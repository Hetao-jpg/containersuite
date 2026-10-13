// 由拆解脚本从 自定义集装箱fix/DiyUpgradeMod.cs 机械切分，内容逐字未改。
// 职责：网格与窗口尺寸计算、限制判定、重绘与网格枚举
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
    // 【2026-10-10 P2】声明式预热：升级 apply 路径的叶子。
    //   null 安全（首行 grids/slotIndices 判空即 return true），内部只调已编译的
    //   TryCast / RectTransform 访问器 —— 满足"叶子"契约，不会嵌套 JIT。
    [JitWarmup("升级窗口布局判定：多槽容器决定横排还是竖排")]
    private static bool IsHorizontalLayout(List<GameGridInventory> grids, int[] slotIndices)
    {
        if (grids == null || slotIndices == null || slotIndices.Length < 2) return true;
        try
        {
            int i0 = slotIndices[0], i1 = slotIndices[1];
            if (i0 < 0 || i0 >= grids.Count) return true;
            if (i1 < 0 || i1 >= grids.Count) return true;

            Component c0 = ((Il2CppObjectBase)grids[i0]).TryCast<Component>();
            Component c1 = ((Il2CppObjectBase)grids[i1]).TryCast<Component>();
            if (c0 == null || c1 == null) return true;

            Transform t0 = c0.transform;
            Transform t1 = c1.transform;
            if (t0 == null || t1 == null) return true;

            float dx, dy;

            RectTransform rt0 = t0.TryCast<RectTransform>();
            RectTransform rt1 = t1.TryCast<RectTransform>();
            if (rt0 != null && rt1 != null)
            {
                Vector2 p0 = rt0.anchoredPosition;
                Vector2 p1 = rt1.anchoredPosition;
                dx = p1.x - p0.x;
                dy = p1.y - p0.y;
            }
            else
            {
                Vector3 p0 = t0.position;
                Vector3 p1 = t1.position;
                dx = p1.x - p0.x;
                dy = p1.y - p0.y;
            }

            return Math.Abs(dx) >= Math.Abs(dy);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:horizontal-layout", ex); return true; }
    }
    // 【2026-10-10 P2】声明式预热：纯算术叶子，无 IL2CPP 调用，绝对安全。
    [JitWarmup("多槽容器窗口尺寸计算：纯算术，无外部调用")]
    private static void ComputeMultiSlotWindowSize(
        int baseWinW, int baseWinH,
        List<(int dW, int dH)> slotDeltas,
        bool horizontal,
        out int newWinW, out int newWinH)
    {
        int sumDW = 0, sumDH = 0, maxDW = 0, maxDH = 0;
        if (slotDeltas != null)
        {
            for (int i = 0; i < slotDeltas.Count; i++)
            {
                sumDW += slotDeltas[i].dW;
                sumDH += slotDeltas[i].dH;
                if (slotDeltas[i].dW > maxDW) maxDW = slotDeltas[i].dW;
                if (slotDeltas[i].dH > maxDH) maxDH = slotDeltas[i].dH;
            }
        }
        if (horizontal)
        {
            // 水平并排：宽度求和、高度取最大
            newWinW = baseWinW + sumDW;
            newWinH = baseWinH + maxDH;
        }
        else
        {
            // 垂直堆叠：高度求和、宽度取最大
            newWinW = baseWinW + maxDW;
            newWinH = baseWinH + sumDH;
        }
    }
    internal static bool HasUpgraded(GameItem item)
    {
        if (item == null) return false;
        try { return LoadMeta(item).Spent > 0; } catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:has-upgraded", ex); return false; }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2026-09-29：加载期直接返回 false —— 让其它 mod（XIAOWO / WageMod）的
    // 恢复链在加载期完整运行，本 mod 的恢复统一推迟到 WorldReadyGate 打开。
    // 若不加此检查，两边的恢复会抢同一个 GameGridInventory.SetShape。
    // ═══════════════════════════════════════════════════════════════════════
    internal static bool IsResizableContainer(GameItem item)
    {
        if (item == null) return false;
        if (WorldReadyGate.ShouldDefer) return false;
        IntPtr intPtr = IntPtr.Zero;
        try { intPtr = ((Il2CppObjectBase)item).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        // T1（2026-10-12）：TTL 改单调钟（时钟回拨时原写法会让过期判定被长期误当有效）。
        if (intPtr != IntPtr.Zero && _resizableCache.TryGetValue(intPtr, out (long, bool) cached) && PollThrottle.ElapsedMs(cached.Item1) < ResizableCacheMillis)
            return cached.Item2;
        // 判定链：任一类别启用清单命中即为可调整容器（拆行以便阅读与逐项排查）。
        bool resizable =
            (_multiSlotModule.Enable.Value && IsAnyId(item, _multiSlotModule.IdList))
            || (_multiSlot.Enable.Value && IsAnyId(item, _multiSlot.IdList))
            || (_box.Enable.Value && IsAnyId(item, _box.IdList))
            || (_backpack.Enable.Value && IsAnyId(item, _backpack.IdList))
            || (_cage.Enable.Value && IsAnyId(item, _cage.IdList));
        if (!resizable && WageContainerRegistry.GetWageCategory(item) != Cat.None) resizable = true;
        if (intPtr != IntPtr.Zero)
        {
            CacheLimits.Trim(_resizableCache, CacheLimits.Medium);   // A1（原裸写 >256）
            _resizableCache[intPtr] = (PollThrottle.NowMs, resizable);   // T1：单调毫秒
        }
        return resizable;
    }

    internal static GameGridInventory GetResizableInventory(GameItem item)
    {
        if (item == null) return null;
        GridDef gridDefForItem = GetGridDefForItem(item);
        if (gridDefForItem == null) return null;
        int primaryIdx = -1;
        using (IEnumerator<(int, TargetSlot)> enumerator = GetEnabledSlotsWithIndex(gridDefForItem).GetEnumerator())
        {
            if (enumerator.MoveNext()) primaryIdx = enumerator.Current.Item1;
        }
        // 审查 #3：槽 0 是合法索引，原写法 primaryIdx <= 0 会让主槽索引为 0 的容器误走兜底分支。
        if (primaryIdx < 0)
        {
            try
            {
                PixelWindow contentWindow = item.contentWindow;
                if (contentWindow != null)
                {
                    PixelElement childElement = contentWindow.childElement;
                    GameGridInventory gameGridInventory = ((childElement != null) ? ((Il2CppObjectBase)childElement).TryCast<GameGridInventory>() : null);
                    if (gameGridInventory?.inventoryShape != null) return gameGridInventory;
                }
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        }
        List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
        if (allGridInventories.Count == 0) return null;
        foreach (var si in GetEnabledSlotsWithIndex(gridDefForItem))
        {
            int idx = si.Index;
            if (idx >= 0 && idx < allGridInventories.Count) return allGridInventories[idx];
        }
        return allGridInventories[0];
    }
    // DelayedRedraw 去重：同一窗口同一时间只保留一个待执行协程。
    // A1 复核（2026-10-07）：此集合**不需要容量上限** —— Add 后由
    //   DelayedRedrawTracked 的 finally 里 Remove，天然以"在途协程数"为上限，
    //   会自己回收。硬加 if(Count>N)Clear() 反而会破坏去重（清掉在途 ptr →
    //   同一窗口被重复调度）。故保持无上限。
    private static readonly HashSet<IntPtr> _redrawPending = new HashSet<IntPtr>();
    internal static bool TryGetGridShapeLimits(GameItem item, out int minC, out int minR, out int maxC, out int maxR)
    {
        minC = 1; minR = 1;
        const int FallbackMaxDim = 20;   // 配置缺失时的兜底上限
        const int HardMaxDim = 100;      // 配置非法（<=0）时的硬上限
        (int BaseW, int BaseH, int Spent) meta = LoadMeta(item);
        int baseW = meta.BaseW;
        int baseH = meta.BaseH;
        int spent = meta.Spent;
        int cfgMaxW = FallbackMaxDim; int cfgMaxH = FallbackMaxDim;
        GridDef gridDefForItem = GetGridDefForItem(item);
        if (gridDefForItem != null)
        {
            if (gridDefForItem.MaxW.Value > 0) cfgMaxW = gridDefForItem.MaxW.Value;
            if (gridDefForItem.MaxH.Value > 0) cfgMaxH = gridDefForItem.MaxH.Value;
        }
        if (cfgMaxW <= 0) cfgMaxW = HardMaxDim;
        if (cfgMaxH <= 0) cfgMaxH = HardMaxDim;

        // 每容器 id 独立上限（cfg 的 8A_PerIdSizeLimits，见 DiyUpgradeMod.PerIdLimits.cs）。
        //   ★ 语义：写进去的 id **完全覆盖**分类上限（可增可减）；没写的仍用分类上限。
        //   这里只是"把天花板换掉"，后面 spent/peak 的夹取逻辑一行不动 ——
        //   "升级进度决定实际可用尺寸"的既有语义不受影响。
        if (TryGetPerIdLimit(item, out int idMaxW, out int idMaxH))
        {
            cfgMaxW = idMaxW;
            cfgMaxH = idMaxH;
        }
        if (spent < 0) spent = 0;

        // peak 上限：多槽容器专属。tag 缺失时用 W_TAG（= 升级解锁的最大尺寸）推导。
        int peakW = 0, peakH = 0;
        bool isMultiSlot = (gridDefForItem != null && gridDefForItem.AlignSlots != null);

        if (isMultiSlot)
        {
            peakW = TagUtil.SafeTagInt(item, "DIY_UPGRADE_PEAK_W_TAG", 0);
            peakH = TagUtil.SafeTagInt(item, "DIY_UPGRADE_PEAK_H_TAG", 0);

            {
                IntPtr pk = IntPtr.Zero;
                try { pk = ((Il2CppObjectBase)item).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:peak-ptr", ex); }
                if (pk != IntPtr.Zero && _peakCache.TryGetValue(pk, out var cached))
                {
                    if (peakW <= 0 && cached.PeakW > 0) peakW = cached.PeakW;
                    if (peakH <= 0 && cached.PeakH > 0) peakH = cached.PeakH;
                }
            }

            List<GameGridInventory> allGridsFloor = GetAllGridInventories(item);
            int primaryIdxFloor = -1;
            foreach (var si in GetEnabledSlotsWithIndex(gridDefForItem))
            {
                if (si.Index >= 0 && si.Index < allGridsFloor.Count) { primaryIdxFloor = si.Index; break; }
            }

            if (peakW <= 0 || peakH <= 0)
            {
                try
                {
                    int tagW = TagUtil.SafeTagInt(item, "DIY_UPGRADE_W_TAG", 0);
                    int tagH = TagUtil.SafeTagInt(item, "DIY_UPGRADE_H_TAG", 0);
                    if (tagW > 0 && tagH > 0)
                    {
                        TagUtil.SetItemTag(item, "DIY_UPGRADE_PEAK_W_TAG", tagW);
                        TagUtil.SetItemTag(item, "DIY_UPGRADE_PEAK_H_TAG", tagH);
                        peakW = tagW;
                        peakH = tagH;
                        RememberPeakCache(item, tagW, tagH);
                    }
                    else if (primaryIdxFloor >= 0)
                    {
                        GameGridInventory pg = allGridsFloor[primaryIdxFloor];
                        if (pg?.inventoryShape != null)
                        {
                            int curW = pg.inventoryShape.width;
                            int curH = pg.inventoryShape.height;
                            if (curW > 0 && curH > 0)
                            {
                                TagUtil.SetItemTag(item, "DIY_UPGRADE_PEAK_W_TAG", curW);
                                TagUtil.SetItemTag(item, "DIY_UPGRADE_PEAK_H_TAG", curH);
                                peakW = curW;
                                peakH = curH;
                                RememberPeakCache(item, curW, curH);
                            }
                        }
                    }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:peak-probe", ex); }
            }
        }

        int maxW = cfgMaxW, maxH = cfgMaxH, minW = 1, minH = 1;
        if (gridDefForItem != null && baseW > 0 && baseH > 0)
        {
            TargetSlot primarySlot = GetPrimarySlot(gridDefForItem);
            int incW = ((primarySlot != null) ? Math.Max(0, primarySlot.IncW.Value) : 0);
            int incH = ((primarySlot != null) ? Math.Max(0, primarySlot.IncH.Value) : 0);
            maxW = Math.Min(cfgMaxW, baseW + spent * incW);
            maxH = Math.Min(cfgMaxH, baseH + spent * incH);
            minW = Math.Max(1, baseW);
            minH = Math.Max(1, baseH);
        }
        else
        {
            GameGridInventory resizableInventory = GetResizableInventory(item);
            if (resizableInventory?.inventoryShape != null && resizableInventory.inventoryShape.width > 0 && resizableInventory.inventoryShape.height > 0)
            {
                maxW = resizableInventory.inventoryShape.width;
                maxH = resizableInventory.inventoryShape.height;
                minW = resizableInventory.inventoryShape.width;
                minH = resizableInventory.inventoryShape.height;
            }
            else { maxW = 1; maxH = 1; minW = 1; minH = 1; }
        }
        // 多槽容器用 peak 作上限（避免拖小后上限缩水）；单槽容器用主槽当前尺寸兜底。
        if (isMultiSlot)
        {
            if (peakW > 0 && peakW > maxW) maxW = peakW;
            if (peakH > 0 && peakH > maxH) maxH = peakH;
        }
        else
        {
            List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
            GridDef gridDefForBounds = GetGridDefForItem(item);
            if (gridDefForBounds != null)
            {
                foreach (var si in GetEnabledSlotsWithIndex(gridDefForBounds))
                {
                    int idx = si.Index;
                    if (idx < 0 || idx >= allGridInventories.Count) continue;
                    GameGridInventory gameGridInventory = allGridInventories[idx];
                    if (gameGridInventory?.inventoryShape != null)
                    {
                        if (gameGridInventory.inventoryShape.width > maxW) maxW = gameGridInventory.inventoryShape.width;
                        if (gameGridInventory.inventoryShape.height > maxH) maxH = gameGridInventory.inventoryShape.height;
                    }
                }
            }
        }
        minC = minW; minR = minH; maxC = maxW; maxR = maxH;
        if (maxC < minC) maxC = minC;
        if (maxR < minR) maxR = minR;
        return true;
    }
    // S8n（2026-10-02）上限与 XIAOWO 对齐，已于 2026-10-04 废弃；原因见 docs/diy-resizer.md
    internal static bool TryResizeGridWindow(GameItem item, int reqC, int reqR, bool remember, out int actualWidth, out int actualHeight)
    {
        actualWidth = 0; actualHeight = 0;

        // 多槽容器未升级时禁止拖拽（防止绕过 min/max 判定）。
        GridDef gridDefPre = GetGridDefForItem(item);
        bool alignSlotsPre = (gridDefPre?.AlignSlots?.Value ?? false);
        if (alignSlotsPre)
        {
            (int _, int _, int spentPre) = LoadMeta(item);
            if (spentPre <= 0)
            {
                LogOnce($"[容器升级][拖拽拒绝] {IdOf(item)} alignSlots=true 且 spent=0，未升级不允许拖拽");
                return false;
            }
        }

        if (!TryGetGridShapeLimits(item, out var minC, out var minR, out var maxC, out var maxR)) return false;
        List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
        if (allGridInventories.Count == 0) return false;
        GridDef gridDefForItem = GetGridDefForItem(item);
        int primaryIdx = -1;
        using (IEnumerator<(int, TargetSlot)> enumerator = GetEnabledSlotsWithIndex(gridDefForItem).GetEnumerator())
        {
            if (enumerator.MoveNext()) primaryIdx = enumerator.Current.Item1;
        }
        bool alignSlots = (gridDefForItem?.AlignSlots?.Value ?? false);
        int[] slotIndices;
        if (alignSlots)
        {
            List<int> slotIdx = new List<int>();
            foreach (var si in GetEnabledSlotsWithIndex(gridDefForItem))
                if (si.Index >= 0 && si.Index < allGridInventories.Count) slotIdx.Add(si.Index);
            if (slotIdx.Count == 0) slotIdx.Add(0);
            slotIndices = slotIdx.ToArray();
        }
        else
        {
            slotIndices = ((primaryIdx < 0) ? new int[1] : new int[1] { primaryIdx });
        }
        int reqClampedC = Math.Max(minC, Math.Min(maxC, reqC));
        int reqClampedR = Math.Max(minR, Math.Min(maxR, reqR));
        PixelWindow contentWindow = item.contentWindow;
        int winW = contentWindow?.widthPixels ?? 0;
        int winH = contentWindow?.heightPixels ?? 0;
        int primaryArrIdx = Math.Max(0, Math.Min(slotIndices[0], allGridInventories.Count - 1));
        int curW = allGridInventories[primaryArrIdx]?.inventoryShape?.width ?? reqClampedC;
        int curH = allGridInventories[primaryArrIdx]?.inventoryShape?.height ?? reqClampedR;
        int deltaC = reqClampedC - curW;
        int deltaR = reqClampedR - curH;
        // 审查 #1：改成两阶段（仿 Upgrade.cs 的 ApplyUpgradePlan）。
        // 原写法是「边校验边落地」：第 k 个槽 CanContainItems 失败就 return false，
        // 而前 k-1 个槽已经 SetShape 改掉了，窗口尺寸调整又在循环之后——于是留下
        // 「网格已改、窗口没动」的半改状态。现在要么全改，要么一个都不改。
        //
        // 阶段一：只收集目标尺寸并预校验，不碰任何数据。
        // 各槽 inventoryShape 相互独立，缩 A 槽不影响 B 槽的物品分布，
        // 所以「提前批量预校验」与「逐个改边校验」的结果等价。
        List<(int Slot, GameGridInventory Inv, int W, int H)> plan = new List<(int, GameGridInventory, int, int)>();
        foreach (int slotIdx in slotIndices)
        {
            if (slotIdx < 0 || slotIdx >= allGridInventories.Count) continue;
            GameGridInventory gameGridInventory = allGridInventories[slotIdx];
            if (gameGridInventory?.inventoryShape == null) continue;
            int width = gameGridInventory.inventoryShape.width;
            int height = gameGridInventory.inventoryShape.height;
            // AlignSlots：所有槽直接用「第一个槽算出的目标尺寸」（绝对值），而不是各自加增量
            int targetC = alignSlots ? reqClampedC : Math.Max(minC, Math.Min(maxC, width + deltaC));
            int targetR = alignSlots ? reqClampedR : Math.Max(minR, Math.Min(maxR, height + deltaR));
            if (targetC == width && targetR == height) continue;
            if (!CanContainItems(gameGridInventory, targetC, targetR)) return false;
            plan.Add((slotIdx, gameGridInventory, targetC, targetR));
        }

        // 阶段二：全部校验通过后才统一落地。
        List<(int dW, int dH)> deltas = new List<(int, int)>();
        foreach (var entry in plan)
        {
            int widthPixels = entry.Inv.widthPixels;
            int heightPixels = entry.Inv.heightPixels;
            SetShapeKeepTransparency(entry.Inv, entry.W, entry.H);
            // 【2026-10-10 18:05】恢复无条件调用（小窝同款）。小窝缩放 = SetShape +
            //   ForceRecomputeDynamicBackground，从不自管网格、从不调 g.Validate()，实机无乱格。
            //   17:59 曾在拖拽中连它一起跳过 → 拖拽中零重建 → 网格线(R0~R3)呈旧形状残段（用户截图断线）。
            //   真正的累积元凶是 g.Validate()（小窝从不调），已在 RebuildGridFill 轻量路径移除。
            entry.Inv.ForceRecomputeDynamicBackground();
            deltas.Add((entry.Inv.widthPixels - widthPixels, entry.Inv.heightPixels - heightPixels));
        }

        // 落地后复核：SetShapeKeepTransparency 内部会吞掉异常，这里把「没改成」显式暴露出来，
        // 避免又变成那种没有任何报错的静默失效。
        foreach (var entry in plan)
        {
            try
            {
                if (entry.Inv?.inventoryShape == null) continue;
                int aw = entry.Inv.inventoryShape.width;
                int ah = entry.Inv.inventoryShape.height;
                if (aw != entry.W || ah != entry.H)
                    LogOnce($"[容器升级][拖拽告警] {IdOf(item)} 槽 {entry.Slot} 未达到目标尺寸 {entry.W}x{entry.H}（实际 {aw}x{ah}），SetShape 可能被拒绝");
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        }
        if (contentWindow != null)
        {
            bool horizontal = IsHorizontalLayout(allGridInventories, slotIndices);
            int newWinW, newWinH;
            ComputeMultiSlotWindowSize(winW, winH, deltas, horizontal, out newWinW, out newWinH);
            int primaryArrIdx2 = Math.Max(0, Math.Min(slotIndices[0], allGridInventories.Count - 1));
            GameGridInventory primaryInv = allGridInventories[primaryArrIdx2];
            int borderW = Math.Max(0, newWinW - primaryInv.widthPixels);
            int borderH = Math.Max(0, newWinH - primaryInv.heightPixels);
            ApplyWindowSize(contentWindow, primaryInv, newWinW, newWinH, borderW, borderH);
            actualWidth = newWinW;
            actualHeight = newWinH;
        }
        if (remember)
        {
            RememberGridShape(item, reqClampedC, reqClampedR);
            RememberWindowSize(item, actualWidth, actualHeight);
            TagUtil.SetItemTag(item, "DIY_UPGRADE_W_TAG", reqClampedC);
            TagUtil.SetItemTag(item, "DIY_UPGRADE_H_TAG", reqClampedR);
        }
        return true;
    }
    private static GridDef GetGridDefForItem(GameItem item)
    {
        if (item == null) return null;
        CrashProbe.Enter("GDI.GetWageGridDef");
        GridDef wageDef = WageContainerRegistry.GetWageGridDef(item);
        CrashProbe.Exit("GDI.GetWageGridDef");
        if (wageDef != null) return wageDef;
        CrashProbe.Enter("GDI.IsAnyId");
        if (_multiSlotModule.Enable.Value && IsAnyId(item, _multiSlotModule.IdList)) { CrashProbe.Exit("GDI.IsAnyId"); return _multiSlotModule; }
        if (_multiSlot.Enable.Value && IsAnyId(item, _multiSlot.IdList)) { CrashProbe.Exit("GDI.IsAnyId"); return _multiSlot; }
        if (_box.Enable.Value && IsAnyId(item, _box.IdList)) { CrashProbe.Exit("GDI.IsAnyId"); return _box; }
        if (_backpack.Enable.Value && IsAnyId(item, _backpack.IdList)) { CrashProbe.Exit("GDI.IsAnyId"); return _backpack; }
        if (_cage.Enable.Value && IsAnyId(item, _cage.IdList)) { CrashProbe.Exit("GDI.IsAnyId"); return _cage; }
        CrashProbe.Exit("GDI.IsAnyId");
        return null;
    }
    private static IEnumerable<(int Index, TargetSlot Slot)> GetEnabledSlotsWithIndex(GridDef d)
    {
        if (d == null) yield break;
        for (int i = 0; i < d.Slots.Length; i++)
        {
            TargetSlot targetSlot = d.Slots[i];
            if (targetSlot != null && (targetSlot.Enable == null || targetSlot.Enable.Value))
                yield return (Index: i, Slot: targetSlot);
        }
    }
    private static TargetSlot GetPrimarySlot(GridDef d)
    {
        if (d == null) return null;
        foreach (TargetSlot targetSlot in d.Slots)
            if (targetSlot != null && (targetSlot.Enable == null || targetSlot.Enable.Value)) return targetSlot;
        if (d.Slots.Length == 0) return null;
        return d.Slots[0];
    }
    private static void ApplyWindowSize(PixelWindow window, GameGridInventory inventory, int width, int height, int borderW, int borderH)
    {
        if (window == null || inventory == null) return;
        // 拖拽中只做 ResizePixels + FixBounds，重操作推迟到 FinishDrag → RefreshAllGridsOf。
        bool dragging = ContainerWindowResizer.IsDragging;
        try
        {
            window.ResizePixels(width, height);

            // ResizePixels 内部可能 clamp 尺寸，直接写 sizeDelta 强制落地。
            try
            {
                RectTransform rtSelf = window.rectTransform;
                if (rtSelf != null)
                    rtSelf.sizeDelta = new Vector2((float)width, (float)height);

                TreeNodeRender ob = window.outsideBorder;
                if (ob != null && ob.rectTransform != null)
                    ob.rectTransform.sizeDelta = new Vector2((float)width, (float)height);
            }
            catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:resize-force-size", ex); }

            try { inventory.Validate(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }

            _layoutWindow = ((Il2CppObjectBase)window).Pointer;
            _layoutInventory = ((Il2CppObjectBase)inventory).Pointer;
            _layoutWindowWidth = width;
            _layoutWindowHeight = height;
            _layoutChildWidth = width - borderW;
            _layoutChildHeight = height - borderH;
            window.Validate();
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        finally
        {
            _layoutWindow = IntPtr.Zero;
            _layoutInventory = IntPtr.Zero;
            _layoutWindowWidth = 0;
            _layoutWindowHeight = 0;
            _layoutChildWidth = 0;
            _layoutChildHeight = 0;
        }

        if (dragging)
        {
            try { window.FixBounds(); } catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:fix-bounds-dragging", ex); }
            return;
        }

        try
        {
            window.FixBounds();
            RectTransform rectTransform = window.rectTransform;
            if (rectTransform != null)
            {
                try { LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform); }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                Graphic component = ((Component)rectTransform).GetComponent<Graphic>();
                if (component != null) component.SetAllDirty();
            }
            if (inventory != null)
            {
                Component invComponent = ((Il2CppObjectBase)inventory).TryCast<Component>();
                if (invComponent != null && invComponent.gameObject != null)
                {
                    GameObject gameObject = invComponent.gameObject;
                    Graphic component2 = gameObject.GetComponent<Graphic>();
                    if (component2 != null) component2.SetAllDirty();
                    Transform transform = gameObject.transform;
                    int childCount = transform.childCount;
                    for (int i = 0; i < childCount; i++)
                    {
                        Transform child = transform.GetChild(i);
                        if (child != null)
                        {
                            Graphic childGraphic = ((Il2CppObjectBase)child).TryCast<Graphic>();
                            if (childGraphic != null)
                            {
                                childGraphic.SetAllDirty();
                                LayoutRebuilder.MarkLayoutForRebuild(childGraphic.rectTransform);
                            }
                        }
                    }
                }
            }
            if (window.outsideBorder != null)
            {
                RectTransform rectTransform2 = window.outsideBorder.rectTransform;
                if (rectTransform2 != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform2);
            }

            // 整窗 SetAllDirty，覆盖顶部/底部空白区。
            // S8s：本体已抽到 GridFx.MarkAllDirty（行为等价）。
            //      MarkAllDirty 内部有 try-catch，外部不用再包。
            GridFx.MarkAllDirty(window.rectTransform, "GridWindow:win-dirty-all");

            ScheduleDelayedRedraw(window);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
    private static void ScheduleDelayedRedraw(PixelWindow window)
    {
        if (window == null) return;
        IntPtr ptr = IntPtr.Zero;
        try { ptr = ((Il2CppObjectBase)window).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:schedule-redraw-ptr", ex); }
        if (ptr == IntPtr.Zero)
        {
            try { MelonCoroutines.Start(DelayedRedraw(window)); } catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:schedule-redraw-start", ex); }
            return;
        }
        if (!_redrawPending.Add(ptr)) return;
        try { MelonCoroutines.Start(DelayedRedrawTracked(window, ptr)); }
        catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:schedule-redraw-tracked", ex); _redrawPending.Remove(ptr); }
    }
    private static IEnumerator DelayedRedrawTracked(PixelWindow window, IntPtr ptr)
    {
        try { yield return DelayedRedraw(window); }
        finally { _redrawPending.Remove(ptr); }
    }
    private static IEnumerator DelayedRedraw(PixelWindow window)
    {
        yield return null;
        try
        {
            if (window == null || window.rectTransform == null) yield break;
            RectTransform rectTransform = window.rectTransform;
            if (rectTransform != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            if (window.outsideBorder != null) LayoutRebuilder.ForceRebuildLayoutImmediate(window.outsideBorder.rectTransform);
            PixelElement childElement = window.childElement;
            GameGridInventory gameGridInventory = ((childElement != null) ? ((Il2CppObjectBase)childElement).TryCast<GameGridInventory>() : null);
            if (gameGridInventory != null)
            {
                Component invComponent = ((Il2CppObjectBase)gameGridInventory).TryCast<Component>();
                if (invComponent != null && invComponent.gameObject != null)
                {
                    foreach (Graphic componentsInChild in invComponent.gameObject.GetComponentsInChildren<Graphic>(true))
                        if (componentsInChild != null) componentsInChild.SetAllDirty();
                }
            }

            try
            {
                Component wc = ((Il2CppObjectBase)window).TryCast<Component>();
                if (wc?.gameObject != null)
                {
                    var graphics = wc.gameObject.GetComponentsInChildren<Graphic>(true);
                    if (graphics != null)
                        for (int i = 0; i < graphics.Count; i++)
                            if (graphics[i] != null) graphics[i].SetAllDirty();
                }
            }
            catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:delayed-dirty-all", ex); }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
    internal static bool TryOverrideWindowWidth(PixelWindow window, ref int value)
    {
        if (_layoutWindow == IntPtr.Zero || window == null || ((Il2CppObjectBase)window).Pointer != _layoutWindow) return false;
        value = _layoutWindowWidth;
        return true;
    }
    internal static bool TryOverrideWindowHeight(PixelWindow window, ref int value)
    {
        if (_layoutWindow == IntPtr.Zero || window == null || ((Il2CppObjectBase)window).Pointer != _layoutWindow) return false;
        value = _layoutWindowHeight;
        return true;
    }
    internal static bool TryOverrideChildWidth(GameGridInventory inventory, ref int value)
    {
        if (_layoutInventory == IntPtr.Zero || inventory == null || ((Il2CppObjectBase)inventory).Pointer != _layoutInventory) return false;
        value = _layoutChildWidth;
        return true;
    }
    internal static bool TryOverrideChildHeight(GameGridInventory inventory, ref int value)
    {
        if (_layoutInventory == IntPtr.Zero || inventory == null || ((Il2CppObjectBase)inventory).Pointer != _layoutInventory) return false;
        value = _layoutChildHeight;
        return true;
    }
    internal static bool CanContainItems(GameGridInventory inventory, int columns, int rows)
    {
        if (inventory == null || inventory.items == null) return true;
        try
        {
            if (inventory.inventoryShape != null
                && columns >= inventory.inventoryShape.width
                && rows >= inventory.inventoryShape.height)
                return true;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        for (int i = 0; i < inventory.items.Count; i++)
        {
            GameItem gameItem = inventory.items[i];
            GridShape gridShape = gameItem?.modifiedShape ?? gameItem?.shape;
            if (gridShape != null && (gridShape.minX < 0 || gridShape.minY < 0 || gridShape.maxX >= columns || gridShape.maxY >= rows)) return false;
        }
        return true;
    }
    internal static List<GameGridInventory> GetAllGridInventories(GameItem item)
    {
        if (item == null) return new List<GameGridInventory>();
        IntPtr intPtr = IntPtr.Zero;
        try { intPtr = ((Il2CppObjectBase)item).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        if (intPtr != IntPtr.Zero && _ggiCache.TryGetValue(intPtr, out (long, List<GameGridInventory>) cached) && PollThrottle.ElapsedMs(cached.Item1) < 2000)
            return cached.Item2;
        List<GameGridInventory> list;
        try
        {
            list = new List<GameGridInventory>();
            CollectGgisFromWindow(item.contentWindow, list);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("GridWindow:collect-ggi", ex); list = new List<GameGridInventory>(); }
        if (intPtr != IntPtr.Zero) { CacheLimits.Trim(_ggiCache, CacheLimits.Large); _ggiCache[intPtr] = (PollThrottle.NowMs, list); }   // T1：单调毫秒
        return list;
    }
    private static void CollectGgisFromWindow(PixelWindow win, List<GameGridInventory> outList)
    {
        if (win != null) CollectWindowTree(win, 0, new HashSet<long>(), new HashSet<long>(), outList);
    }
    private static void CollectWindowTree(object node, int depth, HashSet<long> seenWins, HashSet<long> seenGgis, List<GameGridInventory> outList)
    {
        if (node == null || depth > 3) return;
        try
        {
            Il2CppObjectBase il2cpp = (Il2CppObjectBase)((node is Il2CppObjectBase) ? node : null);
            if (il2cpp == null) return;
            PixelWindow pixelWindow = il2cpp.TryCast<PixelWindow>();
            if (pixelWindow != null)
            {
                long ptr = ((Il2CppObjectBase)pixelWindow).Pointer.ToInt64();
                if (ptr == 0L || !seenWins.Add(ptr)) return;
                CollectGgiRec(pixelWindow.childElement, 0, seenGgis, outList);
                var il2cppChildren = pixelWindow.children;
                List<GraphNodeStorage> children = new List<GraphNodeStorage>();
                if (il2cppChildren != null)
                {
                    for (int i = 0; i < il2cppChildren.Count; i++) children.Add(il2cppChildren[i]);
                }
                if (children != null)
                {
                    for (int i = 0; i < children.Count; i++)
                        CollectWindowTree(children[i], depth + 1, seenWins, seenGgis, outList);
                }
                return;
            }
            GridPixelElement gridPixelElement = il2cpp.TryCast<GridPixelElement>();
            if (gridPixelElement != null) { CollectGgiRec(gridPixelElement, 1, seenGgis, outList); return; }
            GameGridInventory gameGridInventory = il2cpp.TryCast<GameGridInventory>();
            if (gameGridInventory != null) CollectGgiRec(gameGridInventory, 1, seenGgis, outList);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
    private static void CollectGgiRec(object el, int depth, HashSet<long> seen, List<GameGridInventory> outList)
    {
        if (el == null || depth > 2) return;
        try
        {
            Il2CppObjectBase il2cpp = (Il2CppObjectBase)((el is Il2CppObjectBase) ? el : null);
            if (il2cpp == null) return;
            GameGridInventory gameGridInventory = il2cpp.TryCast<GameGridInventory>();
            if (gameGridInventory != null)
            {
                long ptr = ((Il2CppObjectBase)gameGridInventory).Pointer.ToInt64();
                if (seen.Add(ptr)) outList.Add(gameGridInventory);
                return;
            }
            GridPixelElement gridPixelElement = il2cpp.TryCast<GridPixelElement>();
            if (gridPixelElement == null) return;
            const int DefaultProbeDim = 16;   // 读不到 grid 尺寸时的探测兜底
            const int MaxProbeDim = 128;      // 探测上限，避免超大网格拖慢枚举
            int gridW = DefaultProbeDim;
            int gridH = DefaultProbeDim;
            try
            {
                int gridWidth = gridPixelElement.gridWidth;
                int gridHeight = gridPixelElement.gridHeight;
                if (gridWidth > 0 && gridWidth <= MaxProbeDim) gridW = gridWidth;
                if (gridHeight > 0 && gridHeight <= MaxProbeDim) gridH = gridHeight;
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            for (int i = 0; i < gridH; i++)
            {
                for (int j = 0; j < gridW; j++)
                {
                    PixelElement pixelElement = null;
                    try { pixelElement = gridPixelElement.GetElement(j, i); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                    if (pixelElement != null) CollectGgiRec(pixelElement, depth + 1, seen, outList);
                }
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
}