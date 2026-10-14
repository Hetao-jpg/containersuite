// 由拆解脚本从 自定义集装箱fix/DiyUpgradeMod.cs 机械切分，内容逐字未改。
// 职责：升级流程：材料判定 → 准备 → 应用 → 消耗
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
    private static IEnumerator ForceRebuildAfterUpgrade(PixelWindow window, GameGridInventory inventory)
    {
        int frameCount = 5;
        try { if (inventory?.items == null || inventory.items.Count == 0) frameCount = 3; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        for (int i = 0; i < frameCount; i++)
        {
            yield return null;
            try
            {
                if (window == null || window.rectTransform == null) break;
                inventory?.ForceRecomputeDynamicBackground();
                try { inventory?.Validate(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                window.Validate();
                window.FixBounds();
                if (i == 0) Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(window.rectTransform);
                if (window.outsideBorder != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(window.outsideBorder.rectTransform);
                if (i < 2 && inventory != null)
                {
                    Component invComponent = ((Il2CppObjectBase)inventory).TryCast<Component>();
                    if (invComponent != null && invComponent.gameObject != null)
                    {
                        Transform transform = invComponent.gameObject.transform;
                        int childCount = transform.childCount;
                        for (int j = 0; j < childCount; j++)
                        {
                            Transform child = transform.GetChild(j);
                            if (child == null) continue;
                            Graphic g = ((Il2CppObjectBase)child).TryCast<Graphic>();
                            if (g != null) g.SetAllDirty();
                        }
                    }
                }
                continue;
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        }
    }
    private static void GetMaxSizeForItem(GameItem item, out int maxW, out int maxH)
    {
        maxW = 0; maxH = 0;
        GridDef gridDefForItem = GetGridDefForItem(item);
        if (gridDefForItem != null) { maxW = gridDefForItem.MaxW.Value; maxH = gridDefForItem.MaxH.Value; }
        // 8A_PerIdSizeLimits 优先（与 TryGetGridShapeLimits / 升级路径同口径）：
        // 没配 perId 的仍用分类上限。
        if (TryGetPerIdLimit(item, out int idMaxW, out int idMaxH))
        {
            maxW = idMaxW;
            maxH = idMaxH;
        }
    }
    private static bool IsAllowedMaterial(GameItem source)
    {
        if (source == null) return false;
        try
        {
            if (_materialMetalIngot != null && _materialMetalIngot.Value && source.IsID("metal_ingot")) return true;
            if (_materialScrapMetal != null && _materialScrapMetal.Value && source.IsID("scrap_metal")) return true;
            if (_materialJunk != null && _materialJunk.Value && source.IsID("junk")) return true;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        return false;
    }
    private static Cat MatchCategory(GameItem source, GameItem target)
    {
        if (source == null || target == null) return Cat.None;
        if (IdFieldNull(source) || IdFieldNull(target)) return Cat.None;
        if (!IsAllowedMaterial(source)) return Cat.None;
        if (IsAnyId(source, _box.IdList) || IsAnyId(source, _backpack.IdList) || IsAnyId(source, _cage.IdList) || IsAnyId(source, _multiSlot.IdList) || IsAnyId(source, _multiSlotModule.IdList)) return Cat.None;
        Cat wageCat = WageContainerRegistry.GetWageCategory(target);
        if (wageCat != Cat.None) return wageCat;
        if (_multiSlotModule.Enable.Value && IsAnyId(target, _multiSlotModule.IdList)) return Cat.MultiSlotModule;
        if (_multiSlot.Enable.Value && IsAnyId(target, _multiSlot.IdList)) return Cat.MultiSlot;
        if (_box.Enable.Value && IsAnyId(target, _box.IdList)) return Cat.Box;
        if (_backpack.Enable.Value && IsAnyId(target, _backpack.IdList)) return Cat.Backpack;
        if (_cage.Enable.Value && IsAnyId(target, _cage.IdList)) return Cat.Cage;
        return Cat.None;
    }
    private static bool TryResolve(GameItem first, GameItem second, out GameItem material, out GameItem container, out Cat cat)
    {
        cat = MatchCategory(first, second);
        if (cat != Cat.None) { material = first; container = second; return true; }
        cat = MatchCategory(second, first);
        if (cat != Cat.None) { material = second; container = first; return true; }
        material = null; container = null;
        return false;
    }
    private static bool PrepareUpgrade(GameItem container, Cat cat, out Action apply, out string reason)
    {
        apply = null; reason = null;
        if (container == null) return false;
        IntPtr ptr = IntPtr.Zero;
        try { ptr = ((Il2CppObjectBase)container).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        // T1（2026-10-12）：TTL 判定改单调钟 —— 时钟回拨时 `Elapsed < TTL` 会恒真，
        //   于是缓存里那个**过期**的 can/reason 会一直命中（最坏：升级长时间被判"不能升"）。
        if (ptr != IntPtr.Zero && _prepareCache.TryGetValue(ptr, out var cached) && PollThrottle.ElapsedMs(cached.Time) < PrepareCacheMillis)
        {
            reason = cached.Reason;
            return cached.Can;
        }
        bool canPrepare = PrepareUpgradeInternal(container, cat, out apply, out reason);
        if (ptr != IntPtr.Zero)
        {
            CacheLimits.Trim(_prepareCache, CacheLimits.Large);   // S8u
            _prepareCache[ptr] = new PrepareCacheEntry { Time = PollThrottle.NowMs, Can = canPrepare, Reason = reason };
        }
        return canPrepare;
    }
    private static bool PrepareUpgradeInternal(GameItem container, Cat cat, out Action apply, out string reason)
    {
        apply = null; reason = null;
        // ── 阶段0（2026-10-11）：上锁容器拒绝升级 ─────────────────────────
        // 为什么放在这里而不是 EndDragPatch：
        //   ① 一处生效 —— 单拖 / 未来的组拖 / 任何调用 PrepareUpgradeInternal 的入口
        //      自动全覆盖，不会出现"某个入口漏判"。
        //   ② reason 能带出可读文案，EndDragPatch 的 LogOnce("无法升级: " + reason)
        //      直接就有了提示，不需要在调用点再写一遍。
        //   ③ 不动 EndDragPatch 的执行流（脆弱路径少改一处）。
        //
        // ★ 用 IsCurrentlyLocked 而不是 IsLockedContainer（已反汇编取证）：
        //   IsLockedContainer  = "这物品挂过 LOCKED_CONTAINER tag"（静态，开锁后仍为 true）
        //   IsCurrentlyLocked  = "此刻是否锁着"（动态，读 tag 的 0x48 状态位）
        //   用错了会把"已解锁的容器"也误拦。源 mod 用的也是 IsCurrentlyLocked。
        //
        // ★ 异常一律放行（与源 mod 同语义）：IL2CPP 下非法参数会抛 Il2CppException，
        //   拿不到判定结果时宁可放行，也不误伤正常升级。
        if (container != null)
        {
            try
            {
                if (LockHelper.IsCurrentlyLocked(container))
                {
                    reason = "容器已上锁，需先解锁才能升级";
                    return false;
                }
            }
            catch (System.Exception ex) { CatchLogOnce.Warn("Upgrade:locked-check", ex); }
        }
        switch (cat)
        {
            case Cat.Box: return PrepareGrid(container, _box, cat, out apply, out reason);
            case Cat.Backpack: return PrepareGrid(container, _backpack, cat, out apply, out reason);
            case Cat.Cage: return PrepareGrid(container, _cage, cat, out apply, out reason);
            case Cat.MultiSlot: return PrepareGrid(container, _multiSlot, cat, out apply, out reason);
            case Cat.MultiSlotModule: return PrepareGrid(container, _multiSlotModule, cat, out apply, out reason);
            case Cat.ModContainer: return PrepareGrid(container, WageContainerRegistry._modContainer, cat, out apply, out reason);
            case Cat.WageBox: return PrepareGrid(container, WageContainerRegistry._wageBox, cat, out apply, out reason);
            case Cat.WageBackpack: return PrepareGrid(container, WageContainerRegistry._wageBackpack, cat, out apply, out reason);
            default: reason = "未知分类"; return false;
        }
    }
    private static bool PrepareGrid(GameItem container, GridDef d, Cat cat, out Action apply, out string reason)
    {
        apply = null; reason = null;
        List<GameGridInventory> allGridInventories = GetAllGridInventories(container);
        if (allGridInventories.Count == 0) { reason = "找不到网格仓库"; return false; }
        // 懒迁移：第一次对已满级的 Wage 容器执行 DIY 升级时，才做迁移
        if (allGridInventories[0]?.inventoryShape != null)
        {
            if (WageContainerRegistry.IsWageBox(container) && !WageContainerRegistry.HasDiyMigrated(container))
                WageContainerRegistry.MigrateLegacyWageBox(container, allGridInventories[0]);
            else if (WageContainerRegistry.IsVoidBeadStorage(container) && !WageContainerRegistry.HasDiyMigrated(container))
                WageContainerRegistry.MigrateLegacyVoidBead(container, allGridInventories[0]);
        }
        if (TagSystemFieldNull(container)) { reason = "容器标签系统未就绪"; return false; }
        int targetIndex = -1;
        foreach (var slotInfo in GetEnabledSlotsWithIndex(d))
        {
            if (slotInfo.Index >= 0 && slotInfo.Index < allGridInventories.Count) { targetIndex = slotInfo.Index; break; }
        }
        if (targetIndex < 0) { reason = "目标网格索引超出实际网格数量"; return false; }
        GameGridInventory targetGrid = allGridInventories[targetIndex];
        if (targetGrid?.inventoryShape == null) { reason = "读取当前网格形状失败"; return false; }
        var (baseW, baseH, spent) = LoadMeta(container);
        int curW = targetGrid.inventoryShape.width;
        int curH = targetGrid.inventoryShape.height;
        if (baseW <= 0 || baseH <= 0)
        {
            baseW = curW; baseH = curH;
            SaveMeta(container, baseW, baseH, spent);
        }
        List<(int Index, int NewW, int NewH)> plan = new List<(int, int, int)>();
        int maxW = Math.Max(1, d.MaxW.Value);
        int maxH = Math.Max(1, d.MaxH.Value);

        // ★ 2026-10-13：perId 上限必须在升级路径生效 —— 它是比分类上限更细的一层。
        //   原实现只在 TryGetGridShapeLimits（tooltip / 拖拽路径）里读，
        //   升级路径完全绕过它，导致「配了 perId 也能一直升过它」。
        //   语义：perId 写了就完全覆盖分类上限（与 TryGetGridShapeLimits 保持一致）。
        if (TryGetPerIdLimit(container, out int idMaxW, out int idMaxH))
        {
            maxW = Math.Max(1, idMaxW);
            maxH = Math.Max(1, idMaxH);
        }

        // ═══════════════════════════════════════════════════════════════════
        // 【2026-10-11 修复】"已达上限"闸门 —— 与拖拽上限同口径。
        //
        // 症状：容器升到 cfg 上限（如 20×20）后，玩家用手柄把它拖小（如 15×15），
        //   再喂材料 → 又能升级、又能耗材料，可无限重复。
        //
        // 根因：plan 计算用的是**当前实际尺寸** slotW（见下方
        //   `Math.Min(maxW, slotW + incW)`），而"上限"的权威口径在
        //   ComputeMaxSize（GridWindow.cs:263）里是 **baseW + spent*incW**
        //   （升级次数口径）。拖小让 slotW 变小 → slotW+incW 重新 < maxW
        //   → plan 非空 → 又扣料。两处口径不一致 ⇒ 手柄能把尺寸拖回去、
        //   材料却能再吃一遍。
        //
        // 修法：在**算 plan 之前**用拖拽同口径判"是否已达上限"，达上限直接拒绝。
        //   · W 和 H **都**达上限才拦（任一维未满仍允许升那一维 —— 兼容
        //     incW=0 / incH=0 或单维已封顶的配置）。
        //   · 判据只用 baseW/baseH/spent（全是持久化 tag，与当前尺寸无关）
        //     ⇒ AlignSlots 与非 AlignSlots 两条分支**共同覆盖**（alignBaseW/H
        //     也是"当前尺寸"，同样会被拖小污染，不能用来判满级）。
        //   · 刻意**不**把 plan 的 slotW 换成 expectedW：ApplyUpgradePlan 会
        //     写死 plan 的尺寸（SetShapeKeepTransparency），若那样改，玩家
        //     "只想拖小窗口"会被一次升级强行拉回 20 并扣料 —— 比原症状更糟。
        //     加闸门后：满级就拒绝（玩家保留自己拖的 15×15，想恢复用手柄拖回，
        //     不耗材料）；未满级则下方逻辑**一字不改**。
        // ═══════════════════════════════════════════════════════════════════
        {
            TargetSlot primarySlotForCap = GetPrimarySlot(d);
            int capIncW = ((primarySlotForCap != null) ? Math.Max(0, primarySlotForCap.IncW.Value) : 0);
            int capIncH = ((primarySlotForCap != null) ? Math.Max(0, primarySlotForCap.IncH.Value) : 0);
            int expectedW = Math.Min(maxW, baseW + spent * capIncW);
            int expectedH = Math.Min(maxH, baseH + spent * capIncH);
            if (expectedW >= maxW && expectedH >= maxH)
            {
                reason = "已达最大尺寸";
                return false;
            }
        }

        // 多槽容器 AlignSlots：统一以「各槽中最大的当前尺寸」为基准升级。
        bool alignSlots = (d.AlignSlots?.Value ?? false);
        int alignBaseW = 0, alignBaseH = 0;
        if (alignSlots)
        {
            foreach (var si in GetEnabledSlotsWithIndex(d))
            {
                if (si.Index < 0 || si.Index >= allGridInventories.Count) continue;
                GameGridInventory gi = allGridInventories[si.Index];
                if (gi?.inventoryShape == null) continue;
                if (gi.inventoryShape.width > alignBaseW) alignBaseW = gi.inventoryShape.width;
                if (gi.inventoryShape.height > alignBaseH) alignBaseH = gi.inventoryShape.height;
            }
        }
        foreach (var slotInfo in GetEnabledSlotsWithIndex(d))
        {
            int slotIdx = slotInfo.Index;
            TargetSlot targetSlot = slotInfo.Slot;
            if (slotIdx < 0 || slotIdx >= allGridInventories.Count) continue;
            GameGridInventory slotGrid = allGridInventories[slotIdx];
            if (slotGrid?.inventoryShape != null)
            {
                int slotW = slotGrid.inventoryShape.width;
                int slotH = slotGrid.inventoryShape.height;
                int incW = Math.Max(0, targetSlot.IncW.Value);
                int incH = Math.Max(0, targetSlot.IncH.Value);
                int newW, newH;
                if (alignSlots)
                {
                    newW = ((incW > 0) ? Math.Min(maxW, alignBaseW + incW) : alignBaseW);
                    newH = ((incH > 0) ? Math.Min(maxH, alignBaseH + incH) : alignBaseH);
                }
                else
                {
                    newW = ((incW > 0) ? Math.Min(maxW, slotW + incW) : slotW);
                    newH = ((incH > 0) ? Math.Min(maxH, slotH + incH) : slotH);
                }
                if (newW != slotW || newH != slotH) plan.Add((slotIdx, newW, newH));
            }
        }
        if (plan.Count == 0) { reason = "已达最大尺寸或尺寸无变化"; return false; }
        Cat catLocal = cat;
        GameItem containerLocal = container;
        int oldW = curW;
        int oldH = curH;
        int firstNewW = plan[0].NewW;
        int firstNewH = plan[0].NewH;
        apply = delegate
        {
            try
            {
                if (!ApplyUpgradePlan(containerLocal, plan, out var actualWidth, out var actualHeight))
                {
                    if (_log != null) _log.Warning("[容器升级] 尺寸重排失败: " + IdOf(containerLocal));
                    return;
                }
                SaveMeta(containerLocal, baseW, baseH, spent + 1);
                RememberWindowSize(containerLocal, actualWidth, actualHeight);
                _prefsDirty = true;
                // 升级成功日志：让每次成功升级在 Latest.log 里留下可追溯的一行
                _log?.Msg($"[容器升级] {IdOf(containerLocal)} 升级第 {spent + 1} 次：{oldW}x{oldH} → {firstNewW}x{firstNewH}");
                List<GameGridInventory> gridsAfter = GetAllGridInventories(containerLocal);
                int newPeakW = TagUtil.SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_W_TAG", 0);
                int newPeakH = TagUtil.SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_H_TAG", 0);
                foreach (var slot in GetEnabledSlotsWithIndex(d))
                {
                    int slotIdx = slot.Index;
                    if (slotIdx >= 0 && slotIdx < gridsAfter.Count)
                    {
                        GameGridInventory slotGrid = gridsAfter[slotIdx];
                        if (slotGrid?.inventoryShape != null)
                        {
                            RememberSlotShape(containerLocal, slotIdx, slotGrid.inventoryShape.width, slotGrid.inventoryShape.height);
                            if (slotGrid.inventoryShape.width > newPeakW) newPeakW = slotGrid.inventoryShape.width;
                            if (slotGrid.inventoryShape.height > newPeakH) newPeakH = slotGrid.inventoryShape.height;
                        }
                    }
                }
                if (d.AlignSlots != null && newPeakW > 0 && newPeakH > 0)
                {
                    int oldPeakW = TagUtil.SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_W_TAG", 0);
                    int oldPeakH = TagUtil.SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_H_TAG", 0);
                    if (newPeakW != oldPeakW || newPeakH != oldPeakH)
                    {
                        TagUtil.SetItemTag(containerLocal, "DIY_UPGRADE_PEAK_W_TAG", newPeakW);
                        TagUtil.SetItemTag(containerLocal, "DIY_UPGRADE_PEAK_H_TAG", newPeakH);
                    }
                }
                if (gridsAfter.Count > 0 && gridsAfter[0]?.inventoryShape != null)
                {
                    RememberGridShape(containerLocal, gridsAfter[0].inventoryShape.width, gridsAfter[0].inventoryShape.height);
                    SyncXiaowoBayTags(containerLocal, gridsAfter[0]);
                    if (WageContainerRegistry.IsVoidBeadStorage(containerLocal))
                    {
                        WageContainerRegistry.OnVoidBeadUpgraded(
                            containerLocal,
                            gridsAfter[0],
                            gridsAfter[0].inventoryShape.width,
                            gridsAfter[0].inventoryShape.height);
                        SkipWageUpgradeRestore.ClearWageRestoreState();
                    }
                    try { containerLocal.ValidateShapeState(); }
                    catch (Exception ex) { if (_log != null) _log.Warning("[容器升级] ValidateShapeState 失败: " + ex.Message); }
                }
                if (catLocal == Cat.Cage)
                {
                    int hpPerCell = Math.Max(0, _cageHpPerCell.Value);
                    int cellDelta = firstNewW - oldW + (firstNewH - oldH);
                    if (cellDelta <= 0) cellDelta = 1;
                    int hpGain = cellDelta * hpPerCell;
                    int maxHealth = TagUtil.SafeTagInt(containerLocal, "CAGE_MAX_HEALTH_TAG", 100);
                    int curHealth = TagUtil.SafeTagInt(containerLocal, "CAGE_HEALTH_TAG", maxHealth);
                    TagUtil.SetItemTag(containerLocal, "CAGE_MAX_HEALTH_TAG", maxHealth + hpGain);
                    TagUtil.SetItemTag(containerLocal, "CAGE_HEALTH_TAG", Math.Min(curHealth + hpGain, maxHealth + hpGain));
                    containerLocal.unitValue += (long)hpGain * 2L;
                }
                PlayUpgradeSound();
                try
                {
                    PixelWindow contentWindow = containerLocal.contentWindow;
                    GameGridInventory inventory = ((gridsAfter.Count > 0) ? gridsAfter[0] : null);
                    MelonCoroutines.Start(ForceRebuildAfterUpgrade(contentWindow, inventory));
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                ContainerWindowResizer.Attach(containerLocal);
                try { _prepareCache.Clear(); _ggiCache.Clear(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }
            catch (Exception ex2) { if (_log != null) _log.Error("[容器升级] 升级应用异常: " + ex2.Message); }
        };
        return true;
    }
    private static bool ApplyUpgradePlan(GameItem item, List<(int Index, int NewW, int NewH)> plan, out int actualWidth, out int actualHeight)
    {
        actualWidth = 0; actualHeight = 0;
        List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
        if (allGridInventories.Count == 0) return false;
        PixelWindow contentWindow = item.contentWindow;
        int winW = contentWindow?.widthPixels ?? 0;
        int winH = contentWindow?.heightPixels ?? 0;
        int firstIdx = -1;
        foreach (var step in plan)
        {
            if (step.Index >= 0 && step.Index < allGridInventories.Count)
            {
                GameGridInventory grid = allGridInventories[step.Index];
                if (grid?.inventoryShape != null && !CanContainItems(grid, step.NewW, step.NewH)) return false;
            }
        }
        List<(int dW, int dH)> deltas = new List<(int, int)>();
        foreach (var step in plan)
        {
            if (step.Index < 0 || step.Index >= allGridInventories.Count) continue;
            GameGridInventory grid = allGridInventories[step.Index];
            if (grid?.inventoryShape != null)
            {
                int widthPixels = grid.widthPixels;
                int heightPixels = grid.heightPixels;
                SetShapeKeepTransparency(grid, step.NewW, step.NewH);
                grid.ForceRecomputeDynamicBackground();
                deltas.Add((grid.widthPixels - widthPixels, grid.heightPixels - heightPixels));
                if (firstIdx < 0) firstIdx = step.Index;
            }
        }
        if (contentWindow != null && firstIdx >= 0)
        {
            int[] planIndices = new int[plan.Count];
            for (int i = 0; i < plan.Count; i++) planIndices[i] = plan[i].Index;
            bool horizontal = IsHorizontalLayout(allGridInventories, planIndices);

            int newWinW, newWinH;
            ComputeMultiSlotWindowSize(winW, winH, deltas, horizontal, out newWinW, out newWinH);
            GameGridInventory firstGrid = allGridInventories[firstIdx];
            int borderW = Math.Max(0, newWinW - firstGrid.widthPixels);
            int borderH = Math.Max(0, newWinH - firstGrid.heightPixels);
            ApplyWindowSize(contentWindow, firstGrid, newWinW, newWinH, borderW, borderH);
            // 纯布局收尾，失败无害：尺寸已由 ApplyWindowSize 生效，这里只求一次边界修正。
            try { contentWindow.FixBounds(); } catch { }
            actualWidth = newWinW; actualHeight = newWinH;
        }
        return true;
    }
    private static void ConsumeMaterial(GameItem material)
    {
        int remaining = material.unitCount - 1;
        if (remaining <= 0) material.Destroy();
        else material.SetUnitCount(remaining);
    }

    /// <summary>
    /// T2（2026-10-12，借鉴蛙哥 <c>ConsumedThisFrame</c> 的**思路**）：同一容器每帧最多"真升"一次。
    /// <para>
    /// 为什么需要：本 mod 有多个可触发升级的入口（游戏 <c>GameItem.Target</c> /
    /// <c>CanTarget</c> / <c>MaterialDirectory.CanIngotTarget</c> / <c>MouseDragHandler.EndDrag</c>
    /// 的补丁，加组拖协程）。目前只有 EndDrag 与组拖协程会**真的扣料**，理论上不该同帧撞车 ——
    /// 所以这不是"修已知 bug"，而是**廉价保险 + 观测点**：一旦本方法真的拦下第二次，
    /// 说明存在我们没预料到的双重入口（留痕见下），比事后再查"材料为什么少了一堆"便宜得多。
    /// </para>
    /// <para>
    /// ★ 只挂在**单拖**（EndDragPatch）这一处。组拖侧刻意不挂，理由写在
    /// DiyUpgradeMod.GroupDrag.cs 的循环体内（协程以 yield 开头、天然不与单拖同帧；
    /// 真正的跨帧风险由 B-3' 的 uniqueId 双验兜住）。所以本闸门当前是
    /// **纯保险 + 观测点**：今天结构上不该被触发，一旦留痕就是"多了一条我们不知道的入口"。
    /// </para>
    /// <para>
    /// ★ 与蛙哥实现的差别（有意）：① 蛙哥的 <c>_lastUpgradeFrame</c> 永不清理（裸指针键无限增长），
    /// 本实现随场景/读档清空；② 蛙哥把闸门放在扣料**之前**，本实现放在 <c>PrepareUpgradeInternal</c>
    /// 成功**之后** —— 这样被拦下的必然是"本该真升的一次"，留痕是真信号而不是误报。
    /// </para>
    /// </summary>
    /// <returns>true = 放行（已记账）；false = 本帧已被别的入口升过，调用方必须放弃本次 apply/扣料。</returns>
    private static bool TryConsumeUpgradeStamp(GameItem container)
    {
        try
        {
            IntPtr ptr = IntPtr.Zero;
            try { ptr = ((Il2CppObjectBase)container).Pointer; } catch { return true; }
            if (ptr == IntPtr.Zero) return true;   // 拿不到指针 → 不拦（保持旧行为）

            int frame = Time.frameCount;
            if (_upgradeStampCache.TryGetValue(ptr, out int f) && f == frame)
            {
                // 无条件留痕（3 秒去重防刷屏）。这是异常信号，不该静默。
                LogOnce("[容器升级] 同一帧内第二个入口想升级同一容器，已拒绝（防重复扣料）。"
                    + "出现本行说明存在未预料的双重入口，请上报。", warning: true);
                return false;
            }
            CacheLimits.Trim(_upgradeStampCache, CacheLimits.Large);
            _upgradeStampCache[ptr] = frame;
            return true;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.UpgradeStamp", ex); return true; }
    }

    private static void PlayUpgradeSound()
    {
        try
        {
            EmporiumAudioPlayer instance = EmporiumAudioPlayer.Instance;
            if (instance != null && instance.ratchet != null) instance.ratchet.Play();
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
}
