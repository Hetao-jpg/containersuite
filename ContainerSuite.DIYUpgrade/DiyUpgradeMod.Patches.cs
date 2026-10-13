// 职责：全部 Harmony 补丁类
//
// 2026-09-29 改动：
//   1. 去掉 7 处 [HarmonyPriority(800)] —— 与其它 mod 抢占执行顺序是旧档崩溃诱因。
//   2. 所有会读游戏状态的 Prefix/Postfix 加 WorldReadyGate.ShouldDefer 检查，
//      加载期一律放行给原版 / 其它 mod。
//   3. 不改 get_widthPixels / get_heightPixels 这类纯布局 getter —— 它们不读
//      游戏状态，且被 SetShapeKeepTransparency 依赖，加 gate 会破坏布局。
//
// ─────────────────────────────────────────────────────────────────────
// 【2026-10-02 S8i】DIY 入口优先级 —— 只加 Priority，不动 IsUpgradeKeyHeld
// ─────────────────────────────────────────────────────────────────────
// 背景：XIAOWO（精工扩舱）和蛙哥（虚空珠）跟 DIY 抢同一批容器。
//   · DIY  与 XIAOWO 都用 metal_ingot，6 个 bay 又同在 DIY _box 组 IdList 里
//   · DIY  与蛙哥     都用 junk，虚空珠同在 DIY _wageBackpack 组里
// 材料完全重叠 → 同一帧两个 mod 都判定"这块料该给这个箱子"→ 顺序决定结果。
//
// ★ 为什么不改成"无条件抢"（去掉 IsUpgradeKeyHeld）：
//   IsUpgradeKeyHeld 是**交互设计**（玩家主动按键才接管），不是优先级开关。
//   MayTarget / CanTarget / MayHaveValidInventorySlot 的返回值含义是
//   「这东西能不能放进这个箱子」。去掉按键门槛后，材料不够或已满级时
//   PrepareUpgrade 返回 false → CanTarget 返回 false → 玩家连"把金属锭
//   正常放进箱子"都做不了。这是拿一个冲突换三个功能倒退，不做。
//
// ★ 正确做法：让 DIY 的 Prefix 在同目标方法上**先跑**。
//   同一目标方法的所有 Prefix 按 priority 降序执行，任一个返回 false 即短路
//   后续 Prefix。DIY 先跑并短路 → XIAOWO / 蛙哥的 Prefix 根本不会执行。
//   —— 这与"无条件抢"的效果在冲突场景下相同，但不伤正常操作。
//
// ★ 为什么这次写方法级而不是类级：
//   HarmonyPriority 虽声明 AttributeTargets.Class，但 Merge 时 priority 字段
//   有 `if (f != "priority" || value2 != -1)` 特判，类级值只在方法级未设时生效。
//   类级写法在动态 Patch 场景下不可靠（Harmony 只会扫到类上的 attribute，
//   但本工程部分补丁是代码 new 出来的），一律写方法级。
//
// ★ 优先级取值 900 而不是 1000：
//   900 已远高于 XIAOWO / 蛙哥（它们都没设 Priority）。
//
// ★ 2026-10-07 更正（两条都别再用旧说法）：
//   ① HarmonyX 里"没设 Priority"的**有效默认是 Priority.Normal = 400**，
//      不是 -1。-1 只是 attribute 上"未赋值"的哨兵，排序时不会当 -1 用。
//      结论不变（900 > 400 ⇒ DIY 先跑），但旧注释"默认 -1"会误导后人算错账。
//   ② "留 1000+ 给将来"这个预留**已经被用掉了**：
//      RealPrinter（Mods\RealPrinter.dll）对 GameItem.MayTarget / CanTarget /
//      Target 用的是 [HarmonyPriority(990)]，**高于本 mod 的 900**。
//      ⇒ 同一目标方法上，RealPrinter 的 Prefix 跑在 DIY 之前；它若 Claim 成功
//        就 return false 短路，DIY 的 Prefix 根本不执行。
//      两者判据集合只要不相交即安全（各自认各自的物品）。若将来出现
//      "按住升级键拖材料到某容器没反应"且该物品同时是 RealPrinter 的目标，
//      先怀疑这里 —— 排查入口见本文件头与 README 的跨 mod 冲突一节。
//
// ★ Postfix 不加 Priority：
//   Prefix 短路后 Postfix 仍会执行，所以 XIAOWO 的 3 个 Postfix
//   （CanTarget / MayTarget / CanIngotTarget）必须靠 SkipXiaowoBayRestore
//   拦 CanTargetWithIngot 这个 helper —— 那里才是真正的收敛点。
using ContainerSuite.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    /// <summary>
    /// S8i：入口 Prefix 的 Harmony 优先级。比 XIAOWO / 蛙哥（都没设 Priority，
    /// 默认 -1）高得多 → 同目标方法上 DIY 先跑，返回 false 即短路它们。
    /// 留 1000+ 给将来的"必须在 DIY 之前跑"的场景。
    /// </summary>
    internal const int Priority = 900;

    [HarmonyPatch(typeof(GameItem), "MayHaveValidInventorySlot")]
    private static class MayHaveValidInventorySlotPatch
    {
        // S8i：方法级 Priority，让 DIY 先于 XIAOWO / 蛙哥判定（见文件头说明）
        [HarmonyPriority(Priority)]
        private static bool Prefix(GameItem __instance, GameItem item, ref bool __result)
        {
            if (WorldReadyGate.ShouldDefer) return true;
            if (!IsUpgradeKeyHeld()) return true;
            GameItem _m; GameItem _c; Cat _cat;
            if (TryResolve(item, __instance, out _m, out _c, out _cat))
            {
                __result = false;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameItem), "MayTarget")]
    private static class MayTargetPatch
    {
        [HarmonyPriority(Priority)]
        private static bool Prefix(GameItem __instance, GameItem targetItem, ref bool __result)
        {
            if (WorldReadyGate.ShouldDefer) return true;
            if (!IsUpgradeKeyHeld()) return true;
            GameItem _m; GameItem container; Cat cat;
            if (!TryResolve(__instance, targetItem, out _m, out container, out cat)) return true;
            Action _a; string _r;
            __result = PrepareUpgrade(container, cat, out _a, out _r);
            return false;
        }
    }

    [HarmonyPatch(typeof(ItemBehaviourManager), "Target")]
    private static class ManagerTargetPatch
    {
        [HarmonyPriority(Priority)]
        private static bool Prefix(ItemBehaviourManager __instance, GameItem source, GameItem target)
        {
            if (WorldReadyGate.ShouldDefer) return true;
            if (!IsUpgradeKeyHeld()) return true;
            GameItem material; GameItem container; Cat cat;
            return !TryResolve(source, target, out material, out container, out cat);
        }
    }

    [HarmonyPatch(typeof(GameItem), "Target")]
    private static class TargetPatch
    {
        [HarmonyPriority(Priority)]
        private static bool Prefix(GameItem __instance, GameItem targetItem)
        {
            if (WorldReadyGate.ShouldDefer) return true;
            if (!IsUpgradeKeyHeld()) return true;
            GameItem _m; GameItem _c; Cat _cat;
            if (!TryResolve(__instance, targetItem, out _m, out _c, out _cat)) return true;
            return false;
        }
    }

    [HarmonyPatch(typeof(GameItem), "CanTarget")]
    private static class CanTargetPatch
    {
        [HarmonyPriority(Priority)]
        private static bool Prefix(GameItem __instance, GameItem targetItem, ref bool __result)
        {
            if (WorldReadyGate.ShouldDefer) return true;
            if (!IsUpgradeKeyHeld()) return true;
            GameItem _m; GameItem container; Cat cat;
            if (!TryResolve(__instance, targetItem, out _m, out container, out cat)) return true;
            Action _a; string _r;
            __result = PrepareUpgrade(container, cat, out _a, out _r);
            return false;
        }
    }
    [HarmonyPatch(typeof(ItemMouseDragHandler), "EndDrag")]
    private static class EndDragPatch
    {
        // P8（2026-10-02）：Drag 三字段改走运行时反查。
        // 反查失败会静默回退硬编码（行为等价于改动前）；反查命中的话，
        // 游戏更新时字段偏移变了也能自愈。
        // P5 的弱自校验在下游，反查命中错误字段时会告警（见 MarkDragOffsetSuspect）。
        private static int OffDragSource()
        {
            return OffsetResolver.FieldOffset(
                typeof(ItemMouseDragHandler),
                new[] { "source", "dragSource", "sourceItem" },
                Off.DragSource,
                apply: true);
        }

        private static int OffDragTarget()
        {
            return OffsetResolver.FieldOffset(
                typeof(ItemMouseDragHandler),
                new[] { "target", "dragTarget", "targetItem" },
                Off.DragTarget,
                apply: true);
        }

        private static int OffDragTargetInv()
        {
            return OffsetResolver.FieldOffset(
                typeof(ItemMouseDragHandler),
                new[] { "targetInv", "targetInventory", "targetGrid" },
                Off.DragTargetInv,
                apply: true);
        }

        /// <summary>
        /// 阶段0（2026-10-11）：所有权判定，异常/无法判定时**放行**。
        /// <para>
        /// 语义对齐源 mod 的 <c>bool? IsItemOwned</c>：那里返回 null 表示
        /// "PlayerStore 还没就绪 / 查不动"，调用点用 <c>== false</c> 判，
        /// 即 null 不拦截。原生 <c>GeneralHelper.IsItemOwned</c> 只有 bool 且
        /// 异常会抛，所以这里 catch 到异常返回 true（放行）。
        /// </para>
        /// <para>
        /// ★ 宁可放行也不误伤：判定失败时把一个本该正常升级的操作拦掉，
        ///   是玩家看得见的"功能坏了"；而放行的坏处（理论上可能漏拦）极小。
        /// </para>
        /// </summary>
        private static bool IsOwnedSafe(GameItem item)
        {
            if (item == null) return false;   // 空对象视为"非玩家拥有"，不该继续
            try { return GeneralHelper.IsItemOwned(item); }
            catch (System.Exception ex) { CatchLogOnce.Warn("Patches:is-owned", ex); return true; }
        }

        private static void Postfix(ItemMouseDragHandler __instance)
        {
            if (WorldReadyGate.ShouldDefer) return;
            // ★ T1（2026-10-12）：两处节流改单调钟。原 DateTime 差值在系统时钟回拨时
            //   "Elapsed < 0.4" 恒真 ⇒ **单拖升级被永久拒绝**（玩家视角：拖着材料松手没反应）。
            if (!IsUpgradeKeyHeld() || !_endDragGate.Due(400)) return;
            if (PollThrottle.ElapsedMs(_targetConsumeAtMs) < 500) return;
            IntPtr pointer = ((Il2CppObjectBase)__instance).Pointer;
            IntPtr sourcePtr = Marshal.ReadIntPtr(pointer, OffDragSource());
            IntPtr targetPtr = Marshal.ReadIntPtr(pointer, OffDragTarget());
            IntPtr targetInvPtr = Marshal.ReadIntPtr(pointer, OffDragTargetInv());
            if (sourcePtr == IntPtr.Zero) return;
            GameItem dragSourceItem = null;
            try { dragSourceItem = new GameItem(sourcePtr); }
            catch (System.Exception ex) { CatchLogOnce.Warn("Patches:drag-source-ctor", ex); dragSourceItem = null; }
            VerifyDragOffset(dragSourceItem);
            // P5：DragSource 偏移读不出有效 GameItem（new 抛异常 / identifier 字段为空）时
            // 单独计数 —— 与 VerifyDragOffset 里对 Id 偏移的告警区分开。
            if (dragSourceItem == null || IdFieldNull(dragSourceItem))
            {
                MarkDragOffsetSuspect();
                return;
            }
            try { if (!IsAllowedMaterial(dragSourceItem)) return; }
            catch (System.Exception ex) { CatchLogOnce.Warn("Patches:is-allowed-material", ex); return; }
            GameItem dragTargetItem = null;
            if (targetPtr != IntPtr.Zero)
            {
                try { dragTargetItem = new GameItem(targetPtr); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }
            else if (targetInvPtr != IntPtr.Zero)
            {
                dragTargetItem = InventoryOwner(targetInvPtr);
            }
            if (dragTargetItem == null) return;
            // ── 阶段0（2026-10-11）：所有权检查提前到"任何写操作之前" ──────────
            // ★ 为什么必须放在虚空珠移交门之前：
            //   移交门会执行 HandOverVoidBead（打 DIY_VOIDBEAD_HANDOVER tag + 记基准），
            //   且**不可逆**。若先移交再检查所有权，玩家就能给"非自己拥有 / 已上锁"的
            //   虚空珠打上移交 tag —— 污染了别人（或商店）的容器状态。
            //   顺序必须是：先证明"这是我们有权动的容器"，再做任何写入。
            //
            // ★ 为什么可以在 TryResolve 之前判 material/container：
            //   这里判的是 dragSource(材料) 与 dragTarget(容器)，不依赖 MatchCategory
            //   的分类结果 —— 而移交门恰恰会改变分类结果，所以所有权必须先于它。
            //
            // ★ 用游戏原生 GeneralHelper.IsItemOwned（已反汇编取证 0x8B7820）。
            //   不用源 mod 那份手写实现（靠 PlayerStore.FindAllItem() 全量列举
            //   + 指针比对，性能差且重复造轮子）。蛙哥也在用（WagesPerks.cs:2507）。
            //   异常一律放行（对齐源 mod 的 bool? null 语义），见 IsOwnedSafe。
            if (!IsOwnedSafe(dragSourceItem))
            {
                LogOnce("[容器升级] EndDrag拦截: 材料非玩家拥有, 不扣料不升级");
                return;
            }
            if (!IsOwnedSafe(dragTargetItem))
            {
                LogOnce("[容器升级] EndDrag拦截: 容器非玩家拥有, 不升级");
                return;
            }
            // ── S8l 虚空珠移交门 ──────────────────────────────────────
            // 玩家按住升级键 + 拖着可用材料 + 目标是虚空珠 = 表达"这个我要 DIY 升"，
            // 在这里（能拿到 dragSource/dragTarget 的唯一时机）执行一次性移交。
            // 不在这里移交的后果：判据链虽然已返回 WageBackpack，
            // 但基准没记、指针没登记 → 第一次升级会从 1x1 算起，等于吞掉玩家已有的格数。
            if (WageContainerRegistry.IsVoidBeadStorage(dragTargetItem)
                && DiyUpgradeMod.VoidBeadHandoverRequested(dragTargetItem))
            {
                try
                {
                    GameGridInventory beadGrid = GetResizableInventory(dragTargetItem);
                    WageContainerRegistry.HandOverVoidBead(dragTargetItem, beadGrid);
                    // 判据链结果变了，三个缓存都必须失效，否则这一帧还会用旧判定
                    _prepareCache.Clear();
                    _resizableCache.Clear();
                    _metaCache.Clear();
                }
                catch (Exception ex) { _log?.Error("[容器升级] 虚空珠移交异常: " + ex.Message); }
            }
            if (!TryResolve(dragSourceItem, dragTargetItem, out var material, out var container, out var cat)) return;
            try
            {
                if (PrepareUpgradeInternal(container, cat, out var apply, out var reason))
                {
                    // T2：同一容器每帧最多真升一次（闸门放在 prepare 成功之后 ——
                    //     被它拦下的必然是"本该真升的一次"，留痕才是真信号）。
                    if (TryConsumeUpgradeStamp(container))
                    {
                        apply();
                        ConsumeMaterial(material);
                        _targetConsumeAtMs = PollThrottle.NowMs;   // T1：单调钟（原 DateTime.UtcNow）
                    }
                    try { _prepareCache.Clear(); _ggiCache.Clear(); return; } catch { return; }
                }
                LogOnce("[容器升级] 无法升级: " + reason);
            }
            catch (Exception ex) { _log?.Error("[容器升级] EndDrag异常: " + ex.Message); }
        }
    }

    [HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic", new Type[] { })]
    private static class ContainerTooltipPatch
    {
        // 审查 #7：GetTooltipBasic 每帧都会跑，而这个 Postfix 对同一个元素在一帧内
        // 跑多次毫无意义 —— 用「元素指针 + 帧号」去重。
        private static readonly Dictionary<IntPtr, int> _tooltipSeenFrame = new Dictionary<IntPtr, int>();
        private static int _tooltipFrameStamp = -1;

        private static void Postfix(GameItemElement __instance, RichTextBuilder __result)
        {
            try
            {
                if (__result == null || __instance == null) return;
                // 加载期：tooltip 不会显示，直接返回。
                if (WorldReadyGate.ShouldDefer) return;
                IntPtr elementPtr = ((Il2CppObjectBase)__instance).Pointer;
                int frame = Time.frameCount;
                if (_tooltipFrameStamp != frame)
                {
                    _tooltipFrameStamp = frame;
                    _tooltipSeenFrame.Clear();
                }
                if (_tooltipSeenFrame.ContainsKey(elementPtr)) return;
                CacheLimits.Trim(_tooltipSeenFrame, CacheLimits.Medium);   // A1（原裸写 >256）
                _tooltipSeenFrame[elementPtr] = frame;

                if (IdFieldNull(__instance) || TagSystemFieldNull(__instance) || !__instance.IsTag("DIY_UPGRADE_W_TAG") || !__instance.IsTag("DIY_UPGRADE_H_TAG")) return;
                int curW = TagUtil.SafeTagInt(__instance, "DIY_UPGRADE_W_TAG");
                int curH = TagUtil.SafeTagInt(__instance, "DIY_UPGRADE_H_TAG");
                if (curW <= 0 || curH <= 0) return;
                try
                {
                    string nakedText = __result.Build().nakedText;
                    if (!string.IsNullOrEmpty(nakedText) && (nakedText.Contains("| 范围 ") || nakedText.Contains("| 上限 "))) return;
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                int spentTimes = 0, metaBaseW = 0, metaBaseH = 0;
                if (TryGetMeta(__instance, out var baseW, out var baseH, out var spent))
                {
                    spentTimes = ((spent > 0) ? spent : 0);
                    metaBaseW = baseW; metaBaseH = baseH;
                }
                GetMaxSizeForItem(__instance, out var maxW, out var maxH);
                int minW = 1, minH = 1;
                try
                {
                    if (TryGetGridShapeLimits(__instance, out var mc, out var mr, out var xc, out var xr))
                    {
                        minW = Math.Max(1, mc);
                        minH = Math.Max(1, mr);
                        if (xc > 0) maxW = xc;
                        if (xr > 0) maxH = xr;
                    }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                string rangeText = ((maxW > 0 && maxH > 0) ? (maxW + "x" + maxH) : "无");
                __result.Add("升级 " + spentTimes + " 次  ");
                __result.Add("当前 " + curW + "x" + curH + "  ");
                if (metaBaseW > 0 && metaBaseH > 0)
                    __result.Add("(基础 " + metaBaseW + "x" + metaBaseH + ")  ");
                __result.Add("| 范围 " + minW + "x" + minH + " ~ " + rangeText);
                __result.NextLine();
                try
                {
                    GridDef gridDefForTooltip = GetGridDefForItem(__instance);
                    if (gridDefForTooltip != null)
                    {
                        int cfgMaxW = (gridDefForTooltip.MaxW != null && gridDefForTooltip.MaxW.Value > 0) ? gridDefForTooltip.MaxW.Value : 0;
                        int cfgMaxH = (gridDefForTooltip.MaxH != null && gridDefForTooltip.MaxH.Value > 0) ? gridDefForTooltip.MaxH.Value : 0;
                        bool cfgWDifferent = (cfgMaxW > 0 && cfgMaxW != maxW);
                        bool cfgHDifferent = (cfgMaxH > 0 && cfgMaxH != maxH);
                        if (cfgWDifferent || cfgHDifferent)
                        {
                            string cfgWText = (cfgMaxW > 0) ? cfgMaxW.ToString() : "无";
                            string cfgHText = (cfgMaxH > 0) ? cfgMaxH.ToString() : "无";
                            __result.Add("最终上限 " + cfgWText + "x" + cfgHText);
                            __result.NextLine();
                        }
                    }
                }
                catch (Exception ex) { CatchLogOnce.Warn("Patches:tooltip-final", ex); }
                try
                {
                    GridDef gridDefForItem = GetGridDefForItem(__instance);
                    if (gridDefForItem == null) return;
                    TargetSlot primarySlot = GetPrimarySlot(gridDefForItem);
                    int incW = ((primarySlot != null) ? Math.Max(0, primarySlot.IncW.Value) : 0);
                    int incH = ((primarySlot != null) ? Math.Max(0, primarySlot.IncH.Value) : 0);
                    if (incW > 0 || incH > 0)
                    {
                        int capW = ((maxW <= 0) ? 1 : maxW);
                        int capH = ((maxH <= 0) ? 1 : maxH);
                        int nextW = Math.Min(capW, curW + incW);
                        int nextH = Math.Min(capH, curH + incH);
                        if (!(nextW == curW && nextH == curH))
                        {
                            __result.Add("按住 " + GetUpgradeKeyLabel() + " 拖" + GetMaterialLabel() + "可升级至 " + nextW + "x" + nextH);
                            __result.NextLine();
                        }
                    }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        }
    }

    // ─────────────────────────────────────────────────────────────────────
    // 布局 getter 覆写：不读游戏状态，也不应该在加载期被 gate 拦截 ——
    // 它们只在自己 ApplyWindowSize 设置 _layoutWindow 期间生效，其余时刻
    // 直接放行原版逻辑。所以这三个 Patch 不加 WorldReadyGate 检查。
    // ─────────────────────────────────────────────────────────────────────
    [HarmonyPatch(typeof(PixelWindow), "get_widthPixels")]
    internal static class WindowWidthPatch
    {
        private static void Postfix(PixelWindow __instance, ref int __result) { TryOverrideWindowWidth(__instance, ref __result); }
    }
    [HarmonyPatch(typeof(PixelWindow), "get_heightPixels")]
    internal static class WindowHeightPatch
    {
        private static void Postfix(PixelWindow __instance, ref int __result) { TryOverrideWindowHeight(__instance, ref __result); }
    }
    [HarmonyPatch(typeof(GameGridInventory), "get_widthPixels")]
    internal static class GridWidthPatch
    {
        private static void Postfix(GameGridInventory __instance, ref int __result) { TryOverrideChildWidth(__instance, ref __result); }
    }
    [HarmonyPatch(typeof(GameGridInventory), "get_heightPixels")]
    internal static class GridHeightPatch
    {
        private static void Postfix(GameGridInventory __instance, ref int __result) { TryOverrideChildHeight(__instance, ref __result); }
    }

    [HarmonyPatch(typeof(MaterialDirectory), "OnIngotTarget")]
    internal static class MaterialDirectoryOnIngotTargetPatch
    {
        [HarmonyPriority(Priority)]
        private static bool Prefix(GameItem source, GameItem target)
        {
            if (WorldReadyGate.ShouldDefer) return true;
            if (!IsUpgradeKeyHeld()) return true;
            GameItem _m; GameItem _c; Cat _cat;
            if (TryResolve(source, target, out _m, out _c, out _cat))
            {
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(MaterialDirectory), "CanIngotTarget")]
    internal static class MaterialDirectoryCanIngotTargetPatch
    {
        [HarmonyPriority(Priority)]
        private static bool Prefix(GameItem source, GameItem target, ref bool __result)
        {
            if (WorldReadyGate.ShouldDefer) return true;
            // 未按升级键时必须放行给原版逻辑，否则会打断"金属锭→熔炉"的原生操作。
            if (!IsUpgradeKeyHeld()) return true;

            GameItem _m; GameItem container; Cat cat;
            if (TryResolve(source, target, out _m, out container, out cat))
            {
                Action _a; string _r;
                __result = PrepareUpgrade(container, cat, out _a, out _r);
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(PlayerStore), "LoadGame")]
    internal static class LoadGameRestorePatch
    {
        // T1（2026-10-12）：节流改单调钟 —— 原 DateTime 差值在时钟回拨时会让
        //   **读档恢复被永久忽略**（每次 Postfix 都判成"距上次不到 1.5 秒"）。
        private static PollThrottle _loadGate;

        [HarmonyPriority(0)]
        private static void Postfix()
        {
            if (!_loadGate.Due(1500)) return;
            // 再次读档 = 世界重置。先关掉门禁，让恢复流程从干净状态起步。
            WorldReadyGate.Invalidate("DIY.LoadGame");
            try { SaveSlotScope.Invalidate(); } catch (System.Exception ex) { CatchLogOnce.Warn("Patches:save-slot-scope-invalidate", ex); }

            // ★ A-2（2026-10-12）：读档 = 换存档内容，所有以「物品指针 / uniqueId」为键的缓存必须失效。
            //   _metaCache 的 key 是 id#uniqueId，而 uniqueId 跨存档会重复（存档 A 的 uid=5 与
            //   存档 B 的 uid=5 是不同物品）—— 读档若**不**触发场景切换，OnSceneLoaded 的
            //   CacheRegistry.ClearAll() 不会跑，于是 A 档的元数据会污染 B 档同 uid 容器。
            //   症状：tooltip 显示错误的「升级 N 次」、恢复链按错误的 base 算目标尺寸。
            //   ★ 与 OnSceneLoaded 的三处**有意差异**（不要"顺手对齐"）：
            //     ① 不清 _offsetCache —— 类型键（type|fallback|候选名），进程内稳定；
            //        清了只会让恢复深栈上的首次 FieldOffset 白跑一次反射解析。
            //     ② 不 ResetDistrust / 不清 _cachedIdOffset —— distrust 是进程级证据，读档不翻案；
            //        同一进程内偏移不会变。
            //     ③ 不碰 WageContainerRegistry._diyVoidBeadGrids —— 已评估，**有意不清**：
            //        该集合只在 HandOverVoidBead / OnVoidBeadUpgraded / MigrateLegacy* 里登记，
            //        而「已移交」的珠子（tag 持久化）读档后 HandOverVoidBead 会幂等返回、
            //        MigrateLegacy* 又要求 !HasDiyMigrated ⇒ 清了之后整个会话都不会重新登记，
            //        已移交珠子将失去对蛙哥 ApplyLockedShape 的拦截（真实回归）。
            //        不清的代价仅是：地址复用可能对"本就没移交的珠子"误拦一次蛙哥锁形（轻微）。
            //   _restoreMemo 不用显式清：它已登记在 CacheRegistry（Core.cs），本调用覆盖。
            int metaBefore = 0;
            try { metaBefore = _metaCache.Count; } catch { }
            try { CacheRegistry.ClearForLoadGame(); } catch (System.Exception ex) { CatchLogOnce.Warn("Patches:loadgame-clear-caches", ex); }
            // ★ A-2 留痕（无条件，不走 Diag）：这是「uid 跨档污染」前提的**唯一证据来源**；
            //   长期恒为 0 即可据此回退本改动。完整说明见 docs/silent-failure.md
            if (metaBefore > 0)
                ContainerSuiteLog.Msg("[容器升级] 读档清缓存：_metaCache " + metaBefore
                    + " 条 → 0（不处理会让同 uid 容器读到上一存档的升级次数）");

            // ★ A-2 / H7b：换档 = 新会话，**一次性告警的去重集合**也一并重置
            //   （与 OnSceneWasLoaded 里清 CatchLogOnce 同哲学）。不清的后果：存档 A 里
            //   已打印过的 CatchLogOnce.Warn（paging.* / Patches.* 等）在存档 B 不会再出现
            //   —— 正是本项目最忌讳的"静默"。
            try { CatchLogOnce.Clear(); } catch { }
            try { MelonCoroutines.Start(DelayedRestore()); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        }

        private static IEnumerator DelayedRestore()
        {
            HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
            // S8u-2：Collect 原本在 try 外 —— 一旦抛异常协程静默死亡，
            // MarkReady 永远到不了，门禁只能靠 CI/8s 超时兜底。移进 try。
            try
            {
                Collect(_box); Collect(_backpack); Collect(_cage); Collect(_multiSlot); Collect(_multiSlotModule);
                Collect(WageContainerRegistry._wageBox);
                Collect(WageContainerRegistry._wageBackpack);
                Collect(WageContainerRegistry._modContainer);
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            // A2（2026-10-07）：等待从"90 帧"改为"墙钟 1.5s"。
            //   原 for (j<90) yield return null 在 20fps 机器上 = 4.5s、60fps 上 = 1.5s，
            //   行为随帧率漂移（低帧率机器读档恢复明显更慢）。改墙钟后既保留
            //   "给 JIT 更多预热时间"的原始意图，又让等待时长与帧率无关。
            const float RestoreWarmupSeconds = 1.5f;
            float warmupStart = Time.realtimeSinceStartup;
            while (Time.realtimeSinceStartup - warmupStart < RestoreWarmupSeconds) yield return null;
            int processedCount = 0;
            // 此处无外层循环：单次等待已由上面的墙钟 while 完成，下面直接遍历 allIds 发恢复请求。
            try { ContainerWindowResizer.ResetAllRestoreDone(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            int processed = 0;
            bool realChainWarmed = false;
            foreach (string id in allIds)
            {
                try
                {
                    GameItem gameItem = DirectoryMaster.Item(id);
                    if (gameItem != null)
                    {
                        // ★★★【2026-10-10 读档闪退根因修复】首次真实容器前做浅栈预热。
                        //   WarmupSaveLoadChain 用 null 哑参预热，GetGridDefForItem(null)
                        //   首行早退 → 内部 item.identifier/state/IsTag/IsID/GetTagReadonly
                        //   这些 IL2CPP 方法从未 JIT。读档后 RestoreArbiter.Tick 在 OnUpdate
                        //   深栈（Unity→MelonLoader detour→OnUpdate→Tick→applier→…7+ 层）首次
                        //   触发它们 JIT → CompileMethodHook 崩（0x80131506，托管 catch 抓不到）。
                        //   本协程是读档后最早能拿到真实 item 的浅栈上下文（无 Tick/applier
                        //   那 5 层嵌套），在这里用真实 item 只读预热一次，把这些方法的首次
                        //   JIT 提前到浅栈完成，之后 OnUpdate 深栈再走就是编译态、不再崩。
                        if (!realChainWarmed)
                        {
                            realChainWarmed = true;
                            WarmupRestoreChainRealItem(gameItem);
                        }
                        RestoreArbiter.Request(gameItem, "LoadGame"); processed++;
                    }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                yield return null;   // ← 加这一行：每个容器后让出一帧
            }
            processedCount = processed;

            // ═══════════════════════════════════════════════════════════════
            // 2026-09-29：先打开门禁，再补记基准。
            // SeedBaselineIfMissing 里会走 IsResizableContainer，而后者在门禁
            // 关闭时返回 false —— 因此必须先把门禁开起来。
            // MarkReady 会 flush 队列里累积的待恢复请求，与 seed 不冲突。
            // S8u-2：这是读档期【最晚】一次开门（A2 起为 1.5s 墙钟 + 每容器 1 帧）。
            // S9 起 SBE 的绑定不再看这里 —— 它改用 SecondsSinceFirstReady（本轮第一次
            // 开门，读档时即 CI.RestoreDone），否则"最快能用的时刻"会被本协程的
            // 恢复时长绑架。此处仍照常记录 LastReadyAt，供其它判据使用。
            // ═══════════════════════════════════════════════════════════════
            WorldReadyGate.MarkReady("DIY.RestoreReady");
            ContainerSuite.Shared.ContainerSuiteLog.Bp("diy.restore.ready", "containers=" + allIds.Count);

            int seededCount = 0;
            foreach (string id in allIds)
            {
                try
                {
                    GameItem gameItem = DirectoryMaster.Item(id);
                    if (gameItem != null && SeedBaselineIfMissing(gameItem)) seededCount++;
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }
            // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 读档恢复的统计汇报，属诊断。
            Diag($"[容器升级] 读档恢复完成，处理了 {processedCount} 个容器，补记基准 {seededCount} 个");

            void Collect(GridDef d)
            {
                if (d?.IdList != null)
                {
                    string[] idList = d.IdList;
                    foreach (string id in idList)
                        if (!string.IsNullOrEmpty(id) && id.IndexOf('*') < 0 && id.IndexOf('?') < 0) allIds.Add(id);
                }
            }
        }

        /// <summary>
        /// ★★★【2026-10-10 读档闪退根因修复】读档恢复链"真实分支"的浅栈预热。
        /// <para>
        /// 背景：<c>WarmupSaveLoadChain</c> 用 null 哑参预热，<c>GetGridDefForItem(null)</c>
        /// 首行即早退，导致它内部访问的 IL2CPP 方法 —— <c>item.identifier</c>（string 字段，
        /// 走特殊 Il2CppInterop 桥接）、<c>item.state</c>、<c>item.IsTag</c>、
        /// <c>item.state.GetTag</c>、<c>item.GetTagReadonly</c>、<c>item.IsID</c> —— 读档前
        /// 从未被任何托管代码调用过（没 JIT）。读档后 <c>RestoreArbiter.Tick</c> 在 OnUpdate
        /// 深栈（Unity→MelonLoader detour→OnUpdate→Tick→applier→…7+ 层嵌套）上首次触发它们
        /// JIT，old MonoMod(2022) 的 CompileMethodHook 读空指针 → CLR fatal 0x80131506
        /// （托管 catch 抓不到）。探针实测崩点 = <c>GetGridDefForItem</c> 内部
        /// （<c>-> Core.GetGridDef</c> 无配对 <c>&lt;-</c>）。
        /// </para>
        /// <para>
        /// 本方法在 <c>DelayedRestore</c> 协程（读档后最早能拿到真实 item 的上下文）里、
        /// 对第一个真实容器只读调用一遍，把上述方法的首次 JIT 提前到协程浅栈完成。
        /// 之后 OnUpdate 深栈再走就是编译态、不再崩。全部只读、无副作用，任何异常无害。
        /// </para>
        /// </summary>
        private static void WarmupRestoreChainRealItem(GameItem item)
        {
            if (item == null) return;
            try { _ = item.identifier; } catch { }                                   // get_identifier
            try { var st = item.state; if (st != null) { var t = st.GetTag("warmup_probe"); if (t != null) _ = t.GetInt(); } } catch { }  // get_state + TagState.GetTag/GetInt
            try { item.IsTag("warmup_probe"); } catch { }                             // IsTag
            try { item.GetTagReadonly("warmup_probe"); } catch { }                    // GetTagReadonly
            try { item.IsID("warmup_probe"); } catch { }                              // IsID
            try { GetGridDefForItem(item); } catch { }                                // 整条 GetGridDefForItem 链（GetWageGridDef→GetWageCategory→NormalizeId→IsAnyId）
            try { WageContainerRegistry.IsWageBoxAtMaxStage(item); } catch { }        // 满级门（读 wb_stage tag）
            try { DiyUpgradeMod.VoidBeadHandedOver(item); } catch { }                 // 虚空珠移交判据（读 tag）
        }
    }
    // ═══════════════════════════════════════════════════════════════════════
    // S8u（2026-10-05）：从 Patch_EmporiumEntryGuard.cs 合并过来，
    //   类名统一为 <目标类>Patch 风格（原先用 Patch_ 前缀，与其它 patch 不一致）。
    //
    // 读档期「加载顺序竞态」引发的原生空引用兜底（2026-09-29 重写）。
    //
    // 【旧实现的问题】
    //   旧版 Hook 的是 Il2CppException.RaiseExceptionIfNecessary —— 那是所有
    //   IL2CPP 互操作调用的咽喉，加载期每帧被调用几千次。而且它只看托管栈帧，
    //   IL2CPP 内部帧根本不在 Environment.StackTrace 里，所以既慢又拦不住。
    //
    // 【新实现】
    //   直接 Hook 出问题的方法本身。实证（反汇编 + 日志）：
    //     StoreClientManager.HandleJacksonStorage（RVA 0x6864A0）在场景加载早期
    //     调用 EmporiumEntry.Instance.GetItemCountById("storage_bay_large")。
    //     若此时 EmporiumEntry.Instance 还没建好（== null），
    //     GetItemCountById（RVA 0x7CE340，内部直接读 [this+0xb8]）就会抛
    //     NullReferenceException。这里在 native 读字段之前短路，返回 0。
    //
    //   只在确知无害的那一例上闭嘴，并且打印一次留痕。
    // ═══════════════════════════════════════════════════════════════════════
    [HarmonyPatch(typeof(EmporiumEntry), "GetItemCountById")]
    private static class EmporiumEntryGuardPatch
    {
        private static bool _warnedOnce;

        private static bool Prefix(EmporiumEntry __instance, ref int __result)
        {
            if (__instance != null) return true;   // 正常放行

            __result = 0;
            if (!_warnedOnce)
            {
                _warnedOnce = true;
                try
                {
                    MelonLogger.Warning(
                        "[DIYUpgrade] 已抑制一次读档期 EmporiumEntry.Instance 空引用" +
                        "（加载顺序竞态，与本 mod 无关；如后续出现异常行为请检查此项）。");
                }
                catch { }   // 纯无害：日志本身失败无需再报（避免日志系统自吞循环）
            }
            return false;
        }
    }
}