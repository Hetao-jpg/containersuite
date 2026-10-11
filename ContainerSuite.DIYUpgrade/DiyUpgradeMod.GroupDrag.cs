// 阶段1（2026-10-11）：批量组拖升级。
//
// ═══════════════════════════════════════════════════════════════════════
// 功能：玩家按住升级键 → 框选多堆材料 → 拖到某个容器上松手
//       → 对**同一个容器**连续喂多堆材料，一次拖拽升多级。
//
// 为什么不支持"一次拖拽升级多个容器"：
//   · 落点歧义无法消解 —— 玩家框选一堆材料拖到一片容器区，到底升哪个？
//     源 mod 用 4 层回退（悬停 → 最近单拖 → 材料所在容器 → 悬停库存）去猜，
//     其中「材料当前所在容器」这条会把"从背包选料"误判成"要升背包"。
//   · 收益为零 —— 每个容器升级用各自的材料/槽位，跨容器批量并不省操作。
//   ⇒ 落点 = 唯一悬停容器；悬停落空 = 静默放弃。宁可"没生效再拖一次"，
//     也不能"升错容器"。
//
// ═══════════════════════════════════════════════════════════════════════
// ★★ 为什么在 StartGroupDrag **Prefix** 快照，而不是 EndGroupDrag 时读：
//   反汇编 EndGroupDrag（0x7DBF90）确认其调用序列为
//     GetCommonHomeInventory → IsFormationValid → CommitPrecisePlacement
//     → RestoreGhosts → ClearDropTarget → ClearSelection
//   **ClearSelection() 在内部被调用** ⇒ EndGroupDrag 的 Postfix 跑时
//   `selectedItems` 已经是空列表。Prefix 时它还在。
//   （这也是为什么本补丁挂 Prefix 而不是 Postfix —— 与蛙哥/钥匙卡包
//     的"Prefix 快照 + Postfix 执行"不同：它们挂的是别的目标方法。）
//
// ★★ 为什么结束才执行（而不是 StartGroupDrag 时）：
//   组拖开始时玩家还没选好落点。必须在"松手那一刻"拿到最终 hoverItem。
//
// ★★ 与蛙哥 / 钥匙卡包的共存：
//   已知 3 个 mod 挂 ItemMultiSelectHandler.EndGroupDrag（蛙哥 WagesPerks、
//   小偷钥匙卡包、本 mod）。三家都采用"不动原方法逻辑"的模式 ⇒ Harmony
//   链上互不干扰。容器主权由 WageContainerRegistry.GetWageCategory 统一裁决
//   （妙妙箱满级才移交 / 虚空珠靠 HandOver tag），所以"蛙哥优先、DIY 后接管"
//   不需要在本文件写任何额外判据 —— MatchCategory 的第一跳就是它。
//
// ★★ 分帧执行（用户决策）：
//   不照抄源 mod 的同步 while 循环。改用协程每帧升一个，理由与本项目
//   P1 治本手法一致：把"同一帧 N 次抽奖"摊成"N 帧各一次"，每帧都回到
//   Mod.OnUpdate 顶层帧，脱离第三方 trampoline 上下文。
//   每步**重新解析材料指针**，不跨帧持有 IL2CPP 对象（规避悬垂指针）。
// ═══════════════════════════════════════════════════════════════════════

using ContainerSuite.Shared;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MelonLoader;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    // ═══════════════════════════════════════════════════════════════════════
    // 组拖状态（仅存于一次组拖的生命周期内，结束后立即清空）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// StartGroupDrag 时快照的"被框选材料的指针"。★ 存指针而非 GameItem 对象：
    /// 组拖可能持续数秒，跨帧持有 IL2CPP 对象有悬垂指针风险（R4）；
    /// 执行时用指针重新 new GameItem 并做存活校验。
    /// </summary>
    private static readonly List<IntPtr> _groupDragMaterialPtrs = new List<IntPtr>();

    /// <summary>最近一次组拖是否已武装（StartGroupDrag 到 EndGroupDrag 之间为 true）。</summary>
    private static bool _groupDragArmed;

    /// <summary>组拖执行协程是否正在跑（防重入）。</summary>
    private static bool _groupUpgradeRunning;

    /// <summary>
    /// StartGroupDrag Prefix：快照框选材料。
    /// <para>
    /// ★ 只快照 |IsAllowedMaterial| 命中的元素 —— 框选里可能混着非材料物品
    ///   （比如顺手把旁边的箱子也框进来了），那些不该参与升级，也不该
    ///   影响"这是一次升级意图"的判定。
    /// </para>
    /// <para>
    /// ★ 不做任何游戏状态写入，异常一律吞掉（Prefix 必须永远返回 true，
    ///   绝不能因为我们的快照失败而阻断游戏自己的组拖）。
    /// </para>
    /// <para>
    /// ★★ 不做 JIT 预热的理由（见文件末补丁类注释）：本方法跑在
    ///   `StartGroupDrag` 的 detour 栈上，但该方法是**玩家输入触发**的，
    ///   首编译天然在浅栈；而若用 [JitWarmup] 强行在启动期预热，会因
    ///   内部调用 `IsUpgradeKeyHeld()` 等自有方法而**嵌套 JIT** —— 反而危险。
    /// </para>
    /// </summary>
    private static void SnapshotGroupDragMaterials(ItemMultiSelectHandler __instance)
    {
        try
        {
            _groupDragMaterialPtrs.Clear();
            _groupDragArmed = false;

            if (!IsUpgradeKeyHeld()) return;                       // 没按升级键 = 普通组拖，不管
            if (WorldReadyGate.ShouldDefer) return;                // 加载期不参与
            if (!IsGroupDragEnabled) return;                       // cfg 总开关（默认开）

            var selected = __instance?.selectedItems;
            if (selected == null) return;

            for (int i = 0; i < selected.Count; i++)
            {
                var el = selected[i];
                if (el == null) continue;
                GameItem gi = null;
                try { gi = (GameItem)(object)el; } catch { continue; }
                if (gi == null || IdFieldNull(gi)) continue;
                // 只收"能当升级材料"的：非材料留在选择集里不参与，也不阻断
                bool allowed;
                try { allowed = IsAllowedMaterial(gi); } catch { continue; }
                if (!allowed) continue;

                IntPtr p = IntPtr.Zero;
                try { p = ((Il2CppObjectBase)gi).Pointer; } catch { continue; }
                if (p != IntPtr.Zero) _groupDragMaterialPtrs.Add(p);
            }

            if (_groupDragMaterialPtrs.Count > 0)
            {
                _groupDragArmed = true;
                // 首次启用留一条**无条件**日志（项目教训：默认静默的日志会被
                // 误读成"没执行"）。用 LogOnce 的 3 秒去重避免刷屏。
                LogOnce("[容器升级] 批量框选已武装：" + _groupDragMaterialPtrs.Count + " 堆材料（松手在容器上即连续升级）");
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("GroupDrag:snapshot", ex); }
    }

    /// <summary>
    /// EndGroupDrag Prefix：解析落点并启动分帧升级协程。
    /// <para>
    /// ★ 必须挂 Prefix —— EndGroupDrag 内部会 ClearSelection()，Postfix 时
    ///   快照虽还在（我们自己存的），但 hoverItem 可能已被清。
    ///   Prefix 是"所有信息都还在"的唯一时机。
    /// </para>
    /// <para>
    /// ★ 不阻断原方法（永远 return true）：游戏自己的组拖逻辑必须正常跑完
    ///   （它会 CommitPrecisePlacement 把材料放进目标容器）。我们在它跑完之后
    ///   的下一帧再开始逐堆消耗 —— 顺序天然正确：材料先进容器，再被升级吃掉。
    /// </para>
    /// </summary>
    private static void ArmGroupUpgrade(ItemMultiSelectHandler __instance)
    {
        try
        {
            if (!_groupDragArmed) return;
            _groupDragArmed = false;   // 无论成败，本次组拖的武装状态到此为止
            if (WorldReadyGate.ShouldDefer) { _groupDragMaterialPtrs.Clear(); return; }
            if (RestoreArbiter.IsApplying) { _groupDragMaterialPtrs.Clear(); return; }   // 仲裁期不抢
            if (!IsGroupDragEnabled) { _groupDragMaterialPtrs.Clear(); return; }
            if (_groupUpgradeRunning) { _groupDragMaterialPtrs.Clear(); return; }        // 防重入

            // ── 落点解析（唯一悬停容器，三级判据，无回退猜测） ──────────────
            GameItem container = ResolveGroupDragTarget(__instance);
            if (container == null)
            {
                // 悬停落空 = 玩家没拖到容器上 = 不是升级意图，静默放弃。
                _groupDragMaterialPtrs.Clear();
                return;
            }

            // 快照成独立数组交给协程（协程里不再引用这两个列表）
            IntPtr[] ptrs = _groupDragMaterialPtrs.ToArray();
            _groupDragMaterialPtrs.Clear();
            if (ptrs.Length == 0) return;

            // ── 虚空珠移交门（与单拖 EndDragPatch 完全同语义） ─────────────
            // ★★ 为什么组拖必须自己走这一步，不能靠 MatchCategory 的判据：
            //   GetWageCategory 里 `VoidBeadHandoverRequested` = 按住升级键即为真，
            //   所以组拖时 MatchCategory 已经返回 WageBackpack —— 但那条路径
            //   **只报意图、不写 tag**（判据函数必须无副作用，见其注释）。
            //   单拖在 EndDragPatch 里紧跟一步 HandOverVoidBead 打 tag + 记基准；
            //   组拖若不补这一步，就会掉进 `PrepareGrid` 的 MigrateLegacyVoidBead
            //   懒迁移分支 —— 那条分支在单拖路径上**永远被短路**，从未经实机验证。
            //   ⇒ 照抄单拖：意图成立就地移交，让后续升级走已验证的 HandOver 基准。
            if (WageContainerRegistry.IsVoidBeadStorage(container)
                && VoidBeadHandoverRequested(container))
            {
                try
                {
                    GameGridInventory beadGrid = GetResizableInventory(container);
                    WageContainerRegistry.HandOverVoidBead(container, beadGrid);
                    // 判据链结果变了，缓存必须失效（照抄 EndDragPatch）
                    _prepareCache.Clear();
                    _resizableCache.Clear();
                    _metaCache.Clear();
                }
                catch (System.Exception ex) { CatchLogOnce.Warn("GroupDrag:handover", ex); }
            }

            IntPtr containerPtr = IntPtr.Zero;
            try { containerPtr = ((Il2CppObjectBase)container).Pointer; } catch { return; }
            if (containerPtr == IntPtr.Zero) return;

            try { MelonCoroutines.Start(RunGroupUpgrade(containerPtr, ptrs)); }
            catch (System.Exception ex) { CatchLogOnce.Warn("GroupDrag:start-coroutine", ex); }
        }
        catch (System.Exception ex)
        {
            CatchLogOnce.Warn("GroupDrag:arm", ex);
            _groupDragMaterialPtrs.Clear();
        }
    }

    /// <summary>
    /// 落点解析：唯一的悬停容器。
    /// <para>
    /// 判据链（短路，不猜）：
    ///   ① <c>hoverItem</c> —— 鼠标正悬停的物品（最准）
    ///   ② <c>hoverInventory</c> 反查 owner —— 悬停在容器的网格上时
    ///      hoverItem 可能为空，用库存指针反查。
    /// 都落空 → null（放弃）。
    /// </para>
    /// <para>
    /// ★ 为什么不加"材料当前所在容器"这条回退：见文件头注释（会把
    ///   "从背包选料"误判成"要升背包"）。
    /// </para>
    /// </summary>
    private static GameItem ResolveGroupDragTarget(ItemMultiSelectHandler h)
    {
        GameItem target = null;
        try { target = h?.hoverItem; } catch { target = null; }
        if (target != null) return target;

        IntPtr invPtr = IntPtr.Zero;
        try
        {
            var inv = h?.hoverInventory;
            if (inv != null) invPtr = ((Il2CppObjectBase)inv).Pointer;
        }
        catch { invPtr = IntPtr.Zero; }
        if (invPtr != IntPtr.Zero) return InventoryOwner(invPtr);

        return null;
    }

    /// <summary>
    /// 分帧批量升级协程。
    /// <para>
    /// ★ 每步：重解析材料指针 → 存活校验 → PrepareUpgradeInternal → apply →
    ///   ConsumeMaterial → yield 一帧。**不跨帧持有任何 IL2CPP 对象**。
    /// </para>
    /// <para>
    /// ★ 每步重建容器对象（容器可能已被销毁 —— 极端情况下玩家在升级
    ///   过程中做了别的操作）。存活校验失败就整体停止。
    /// </para>
    /// <para>
    /// ★ 预算：最多处理 ptrs.Length 堆；另设一个帧数硬上限防止极端情况下
    ///   协程长时间占用（正常不会触及）。
    /// </para>
    /// </summary>
    private static IEnumerator RunGroupUpgrade(IntPtr containerPtr, IntPtr[] materialPtrs)
    {
        _groupUpgradeRunning = true;
        int done = 0;
        try
        {
            // 第一帧先等游戏自己的 EndGroupDrag 跑完（材料落位、幽灵清理）。
            yield return null;

            for (int i = 0; i < materialPtrs.Length; i++)
            {
                if (WorldReadyGate.ShouldDefer) break;
                if (RestoreArbiter.IsApplying) break;
                if (!IsGroupDragEnabled) break;

                // 重解析容器，存活校验
                GameItem container = null;
                try { container = new GameItem(containerPtr); } catch { break; }
                if (container == null || IdFieldNull(container)) break;

                // 重解析材料，存活校验（材料可能已被前面的消耗销毁）
                IntPtr mptr = materialPtrs[i];
                if (mptr == IntPtr.Zero) continue;
                GameItem material = null;
                try { material = new GameItem(mptr); } catch { continue; }
                if (material == null || IdFieldNull(material)) continue;

                // 材料还在容器里吗？可能已被玩家移走 —— 不追究，直接跳过。
                // ★ 这些 continue 分支**不 yield**：它们无副作用，同帧空转可接受；
                //   加 yield 反而让"整批材料都失效"时白等 N 帧。
                Cat cat = MatchCategory(material, container);
                if (cat == Cat.None) continue;

                bool consumed = false;
                try
                {
                    if (PrepareUpgradeInternal(container, cat, out var apply, out var reason))
                    {
                        apply();
                        ConsumeMaterial(material);
                        consumed = true;
                        done++;
                    }
                    else
                    {
                        // 升不动了（满级 / 材料数不足 / 上锁…）。记一条然后整体停。
                        LogOnce("[容器升级] 批量升级中止: " + reason);
                        break;
                    }
                }
                catch (System.Exception ex) { _log?.Error("[容器升级] 批量升级异常: " + ex.Message); break; }

                // 每步清缓存（照单拖语义：apply 委托内也会清，这里双保险）
                try { _prepareCache.Clear(); _ggiCache.Clear(); } catch { }
                _lastTargetConsume = DateTime.UtcNow;   // 与单拖 EndDragPatch 同语义

                // ★★ 分帧：一堆材料一帧，且**等待上一堆的窗口重建跑完**。
                //   apply() 内部会起 ForceRebuildAfterUpgrade 协程（3~5 帧，
                //   反复调 Validate / FixBounds / ForceRebuildLayoutImmediate）。
                //   若两堆的升级间隔 <重建时长，两个重建协程会并发操作**同一个**
                //   PixelWindow / GameGridInventory —— 中间态被对方读到，正是
                //   ValidateShapeState 链（第三方挂载的超热方法）上最危险的竞态。
                //   ⇒ 等 6 帧（> 最长重建 5 帧）再动下一堆，结构上不可能重叠。
                if (consumed)
                {
                    for (int f = 0; f < 6; f++) yield return null;
                }
            }
        }
        finally
        {
            _groupUpgradeRunning = false;
            try { _prepareCache.Clear(); _ggiCache.Clear(); } catch { }
            if (done > 0)
                LogOnce("[容器升级] 批量升级完成：共 " + done + " 堆材料");
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // Harmony 补丁类
    // ═══════════════════════════════════════════════════════════════════════
    //
    // ★ 挂 Prefix 而非 Postfix：见 ArmGroupUpgrade 注释（ClearSelection 时机）。
    // ★ 不改原方法逻辑（Prefix 永远返回 true），与蛙哥/钥匙卡包同模式。
    // ★★ 刻意**不**打 [HarmonyPrefix] —— 让这两个 Prefix 不进预热清单。
    //   理由：WarmupAllPatchMethods 的第二路会 `m.Invoke(null, 哑参)` 真调一次
    //   （`__instance` 是引用类型 ⇒ 传 null）。若打了特性，启动期就会在浅栈上
    //   真调一次 `SnapshotGroupDragMaterials(null)` → 连带嵌套 JIT 它内部的
    //   `IsUpgradeKeyHeld()` 等自有方法，**反而制造**嵌套 JIT 的 0x80131506 风险。
    //   而 `StartGroupDrag` / `EndGroupDrag` 只在玩家组拖时被调用（浅栈、非加载期），
    //   首编译天然安全 —— 预热对它们是负收益。既有的 `TargetPatch` / `EndDragPatch`
    //   同样采用"方法名叫 Prefix 的隐式识别"写法，本文件与其保持一致。
    // ═══════════════════════════════════════════════════════════════════════

    [HarmonyPatch(typeof(ItemMultiSelectHandler), "StartGroupDrag")]
    private static class GroupDragStartPatch
    {
        // ★ 无 [HarmonyPriority]：本项目刻意不与其他 mod 抢执行顺序
        //   （见 DiyUpgradeMod.Patches.cs 文件头 2026-09-29 改动说明）。
        //   快照是只读操作，谁先谁后都不影响结果。
        private static void Prefix(ItemMultiSelectHandler __instance)
        {
            SnapshotGroupDragMaterials(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemMultiSelectHandler), "EndGroupDrag")]
    private static class GroupDragEndPatch
    {
        private static void Prefix(ItemMultiSelectHandler __instance)
        {
            ArmGroupUpgrade(__instance);
        }
    }
}
