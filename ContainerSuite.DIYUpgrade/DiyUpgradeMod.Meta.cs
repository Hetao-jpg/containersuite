// 职责：元数据 / 物品 tag 读写 / 读档恢复
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
    private static string TagSlotW(int idx) => "DIY_UPGRADE_SLOT" + idx + "_W_TAG";
    private static string TagSlotH(int idx) => "DIY_UPGRADE_SLOT" + idx + "_H_TAG";

    // 【2026-10-10 P2】声明式预热：首行 `item == null || inv?.inventoryShape == null`
    //   即 return —— null 哑参不会走到下面的 PlayerPrefs / TagUtil 写入。
    [JitWarmup("XIAOWO 泊位 tag 同步：首行判空即返回，哑参安全")]
    internal static void SyncXiaowoBayTags(GameItem item, GameGridInventory inv)
    {
        if (item == null || inv?.inventoryShape == null) return;
        // 2026-09-29：加载期不写 —— 避免把垃圾数据写进 XIAOWO 的私有 PlayerPrefs key。
        // 本方法的调用点只有 PrepareGrid 里升级成功那一刻，正常路径不会在加载期被触发；
        // 这里加 gate 是防御性的。
        if (WorldReadyGate.ShouldDefer) return;
        try
        {
            string id = item.identifier?.Replace("\0", "").Trim() ?? "";
            if (XiaowoBayIds.Contains(id))
            {
                int width = inv.inventoryShape.width;
                int height = inv.inventoryShape.height;
                int widthPixels = inv.widthPixels;
                int heightPixels = inv.heightPixels;
                TagUtil.SetItemTag(item, "XIAOWO_BAY_UNLOCKED_COLUMNS", width);
                TagUtil.SetItemTag(item, "XIAOWO_BAY_UNLOCKED_ROWS", height);
                TagUtil.SetItemTag(item, "XIAOWO_BAY_LAYOUT_VERSION", 2);
                int spent = Math.Max(0, LoadMeta(item).Spent);
                TagUtil.SetItemTag(item, "XIAOWO_BAY_EXPANSION_LEVEL", spent);
                long uniqueId = 0L;
                try { uniqueId = item.uniqueId; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                // 注意：这个 key 是写给 XIAOWO mod 读的，不是我们自己的存储。
                // 千万不要加 SaveSlotScope 前缀，否则 XIAOWO 读不到数据，
                // 它的窗口尺寸同步会失效。
                string keyPrefix = "XIAOWOTradePerks.ContainerWindow." + id + "." + uniqueId + ".";
                PlayerPrefs.SetInt(keyPrefix + "Width", widthPixels);
                PlayerPrefs.SetInt(keyPrefix + "Height", heightPixels);
                PlayerPrefs.SetInt(keyPrefix + "Columns", width);
                PlayerPrefs.SetInt(keyPrefix + "Rows", height);
                _prefsDirty = true;
            }
        }
        catch (Exception ex) { if (_log != null) _log.Warning("[容器升级] 同步 XIAOWO tag 失败：" + ex.Message); }
    }
    // 【2026-10-10 P2】声明式预热：这三个 Remember* 只调 TagUtil.SetItemTag
    //   —— 该层对 null 已安全（内部判空早退），且它自己早已被编译（全套件高频使用）。
    //   ★ 正因只调已编译方法，它们才够格当"叶子"；若哪天在这里加了新调用，
    //     必须重新确认那调用链已预热，否则 invoke 时会嵌套 JIT → 自爆。
    [JitWarmup("记住网格形状：只调已编译的 TagUtil")]
    internal static void RememberGridShape(GameItem item, int columns, int rows)
    {
        TagUtil.SetItemTag(item, "DIY_UPGRADE_W_TAG", columns);
        TagUtil.SetItemTag(item, "DIY_UPGRADE_H_TAG", rows);
    }
    [JitWarmup("记住槽位形状：只调已编译的 TagUtil")]
    internal static void RememberSlotShape(GameItem item, int slotIndex, int columns, int rows)
    {
        TagUtil.SetItemTag(item, TagSlotW(slotIndex), columns);
        TagUtil.SetItemTag(item, TagSlotH(slotIndex), rows);
    }
    [JitWarmup("记住窗口尺寸：只调已编译的 TagUtil")]
    internal static void RememberWindowSize(GameItem item, int width, int height)
    {
        TagUtil.SetItemTag(item, "DIY_UPGRADE_WIN_W_TAG", width);
        TagUtil.SetItemTag(item, "DIY_UPGRADE_WIN_H_TAG", height);
    }
    // TryRestoreRememberedGridShape 已搬到 DiyUpgradeMod.Restore.cs（2026-10-02 重建）。
    // 搬家原因见该文件头：本方法被 XIAOWO 兼容补丁挂了 Harmony 钩子，
    // 方法体必须保持极小，否则 JIT 会触发 CLR fatal 0x80131506。
    // ═══════════════════════════════════════════════════════════════════════
    // 2026-09-29：场景代际号并入 key。
    // 旧档加载会经历：卸载旧场景 → Il2Cpp 释放所有 GameItem → 加载新场景
    // → IL2CPP 分配器很可能复用同一段地址。此时 uniqueId 可能还没恢复，
    // 于是走 @ptr 分支 —— 若 key 只含指针，就会命中上一场景的旧缓存。
    // 加入代际号后，跨场景 key 永不命中。
    // ═══════════════════════════════════════════════════════════════════════
    private static string MetaKey(GameItem item)
    {
     // 2026-10-15：首行判空。
     //   预热（WarmupSaveLoadChain）用 null 哑参 invoke 本方法；原实现
     //   会一路走到 item.uniqueId / (Il2CppObjectBase)item 才抛 NRE，
     //   且抛出的 NRE 经内层 catch → CatchLogOnce.Warn → 污染启动日志。
     //   判空后哑参直接返回占位串，既不抛异常也不进缓存。
     //   ★ 运行期正常调用不会传 null（调用方 IdOf / LoadMeta 均已判空），
     //     所以这条判空对运行期行为**零影响**。
                if (item == null) return "<null>";
        string id = IdOf(item);
        try
        {
            long uniqueId = 0L;
            try { uniqueId = item.uniqueId; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            if (uniqueId != 0L) return id + "#" + uniqueId;

            try { return id + "@g" + _sceneGeneration + "@" + ((Il2CppObjectBase)item).Pointer.ToInt64().ToString("X"); }
            catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.MetaKey", ex); return id + "@g" + _sceneGeneration + "@unknown"; }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        return id;
    }
    private static (int BaseW, int BaseH, int Spent) LoadMeta(GameItem item)
    {
        // 2026-10-15：首行判空。理由同 MetaKey —— 预热哑参不再触发 NRE 噪音。
        if (item == null) return (0, 0, 0);
        string key = MetaKey(item);
        if (_metaCache.TryGetValue(key, out (int, int, int) cached)) return cached;
        int baseWTag = TagUtil.SafeTagInt(item, "DIY_UPGRADE_BASE_W_TAG", 0);
        int baseHTag = TagUtil.SafeTagInt(item, "DIY_UPGRADE_BASE_H_TAG", 0);
        int spentTag = TagUtil.SafeTagInt(item, "DIY_UPGRADE_SPENT_TAG", 0);


        // 兼容老存档：base tag 缺失时，从 W_TAG 反推 base
        if ((baseWTag <= 0 || baseHTag <= 0) && spentTag > 0)
        {
            int tagW = TagUtil.SafeTagInt(item, "DIY_UPGRADE_W_TAG", 0);
            int tagH = TagUtil.SafeTagInt(item, "DIY_UPGRADE_H_TAG", 0);
            GridDef gd = GetGridDefForItem(item);
            int incW = 1, incH = 1;
            if (gd != null)
            {
                TargetSlot ps = GetPrimarySlot(gd);
                if (ps != null)
                {
                    incW = Math.Max(1, ps.IncW.Value);
                    incH = Math.Max(1, ps.IncH.Value);
                }
            }
            if (tagW > 0 && baseWTag <= 0) baseWTag = Math.Max(1, tagW - spentTag * incW);
            if (tagH > 0 && baseHTag <= 0) baseHTag = Math.Max(1, tagH - spentTag * incH);
        }

        if (baseWTag > 0 && baseHTag > 0)
        {
            (int, int, int) meta = (baseWTag, baseHTag, spentTag);
            CacheLimits.Trim(_metaCache, CacheLimits.Large);      // S8u
            _metaCache[key] = meta;
            return meta;
        }
        return (BaseW: 0, BaseH: 0, Spent: 0);
    }
    internal static void SaveMeta(GameItem item, int baseW, int baseH, int spent)
    {
        string key = MetaKey(item);
        CacheLimits.Trim(_metaCache, CacheLimits.Large);                     // S8u
        _metaCache[key] = (baseW, baseH, spent);
        TagUtil.SetItemTag(item, "DIY_UPGRADE_SPENT_TAG", spent);
        TagUtil.SetItemTag(item, "DIY_UPGRADE_BASE_W_TAG", baseW);
        TagUtil.SetItemTag(item, "DIY_UPGRADE_BASE_H_TAG", baseH);
    }
    private static bool TryGetMeta(GameItem item, out int baseW, out int baseH, out int spent)
    {
        baseW = 0; baseH = 0; spent = 0;
        if (item == null) return false;
        string key = MetaKey(item);
        if (_metaCache.TryGetValue(key, out (int, int, int) cached))
        {
            (baseW, baseH, spent) = cached;
            return true;
        }
        baseW = TagUtil.SafeTagInt(item, "DIY_UPGRADE_BASE_W_TAG", 0);
        baseH = TagUtil.SafeTagInt(item, "DIY_UPGRADE_BASE_H_TAG", 0);
        spent = TagUtil.SafeTagInt(item, "DIY_UPGRADE_SPENT_TAG", 0);
        if (baseW <= 0 && baseH <= 0) return spent > 0;
        return true;
    }
    internal static bool SeedBaselineIfMissing(GameItem item)
    {
        if (item == null || IdFieldNull(item) || TagSystemFieldNull(item)) return false;
        if (WorldReadyGate.ShouldDefer) return false;   // 2026-09-29：加载期不 seed
        if (!IsResizableContainer(item)) return false;
        try
        {
            (int BaseW, int BaseH, int Spent) meta = LoadMeta(item);
            if (meta.BaseW > 0 && meta.BaseH > 0) return false;
        }
        // tag 读取失败（Il2Cpp/反射）不能让 seed 静默判负 —— 留痕。
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.SeedBaselineIfMissing", ex); return false; }
        try
        {
            GridDef gridDefForItem = GetGridDefForItem(item);
            if (gridDefForItem == null) return false;
            List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
            if (allGridInventories.Count == 0) return false;
            int targetIndex = -1;
            foreach (var slot in GetEnabledSlotsWithIndex(gridDefForItem))
            {
                if (slot.Index >= 0 && slot.Index < allGridInventories.Count) { targetIndex = slot.Index; break; }
            }
            if (targetIndex < 0) return false;
            GameGridInventory targetGrid = allGridInventories[targetIndex];
            if (targetGrid?.inventoryShape == null) return false;
            int width = targetGrid.inventoryShape.width;
            int height = targetGrid.inventoryShape.height;
            if (width <= 0 || height <= 0) return false;
            SaveMeta(item, width, height, 0);
            foreach (var slot in GetEnabledSlotsWithIndex(gridDefForItem))
            {
                int idx = slot.Index;
                if (idx != targetIndex && idx >= 0 && idx < allGridInventories.Count)
                {
                    GameGridInventory grid = allGridInventories[idx];
                    if (grid?.inventoryShape != null)
                        RememberSlotShape(item, idx, grid.inventoryShape.width, grid.inventoryShape.height);
                }
            }
            return true;
        }
        catch (Exception ex) { if (_log != null) _log.Warning("[容器升级] SeedBaselineIfMissing 失败: " + ex.Message); return false; }
    }
    internal static void ClearReapplyCache(GameItem item)
    {
        if (item == null) return;
        IntPtr ptr = IntPtr.Zero;
        try { ptr = ((Il2CppObjectBase)item).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        if (ptr != IntPtr.Zero)
        {
            // 【2026-10-10 P3】连带作废恢复链 memo。
            //   本方法正是"该物品状态已被外部改写"的统一信号（迁移 / 移交 / 窗口重开
            //   都调它）⇒ memo 必须一起清，否则会拿旧签名跳过本该做的恢复。
            _restoreMemo.Remove(ptr);
        }
    }
    internal static void ReapplyFromMeta(GameItem item, bool force = false)
    {
        if (item == null || IdFieldNull(item) || TagSystemFieldNull(item)) return;

        // 【RestoreArbiter R1】只允许 Arbiter 事务真正执行 shape 应用。
        // 其它路径在应用期重入，直接短路（防 SetShape 递归）。
        if (RestoreArbiter.IsApplying && RestoreArbiter.ApplyingPtr != item.Pointer)
            return;
        try
        {
            if ((WageContainerRegistry.IsWageBox(item) || WageContainerRegistry.IsVoidBeadStorage(item))
                && !WageContainerRegistry.HasDiyMigrated(item))
            {
                try { _ggiCache.Clear(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                var invs0 = GetAllGridInventories(item);
                if (invs0.Count > 0 && invs0[0]?.inventoryShape != null)
                {
                    if (WageContainerRegistry.IsWageBox(item))
                        WageContainerRegistry.MigrateLegacyWageBox(item, invs0[0]);
                    else
                        WageContainerRegistry.MigrateLegacyVoidBead(item, invs0[0]);
                    try { _ggiCache.Clear(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                }
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        // ★ T-C2：原「3 秒 TTL」判据依赖 force==false，而全库 2 处调用点都传 force:true ⇒ 早已是死逻辑。
        //   删除后节流交给 RestoreArbiter + _restoreMemo；force 参数保留（签名不变）。详见 docs/diy-restore.md
        CrashProbe.Enter("Meta.Resizer.Attach");
        try { ContainerWindowResizer.Attach(item); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        CrashProbe.Exit("Meta.Resizer.Attach");
        bool hasWindow = false;
        CrashProbe.Enter("Meta.contentWindow");
        try { hasWindow = item.contentWindow != null; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        CrashProbe.Exit("Meta.contentWindow");
        bool shapeRestored = false;
        CrashProbe.Enter("Meta.TryRestoreRememberedGridShape");
        try { shapeRestored = TryRestoreRememberedGridShape(item); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        CrashProbe.Exit("Meta.TryRestoreRememberedGridShape");
        if (!(shapeRestored && hasWindow)) return;
        CrashProbe.Enter("Meta.StartForceRefresh");
        try { MelonCoroutines.Start(ForceRefreshWindowAfterRestore(item)); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        CrashProbe.Exit("Meta.StartForceRefresh");
    }
    private static IEnumerator ForceRefreshWindowAfterRestore(GameItem item)
    {
        yield return null;
        yield return null;
        try
        {
            if (item == null) yield break;
            PixelWindow contentWindow = item.contentWindow;
            if (contentWindow == null) yield break;
            GameGridInventory resizableInventory = GetResizableInventory(item);
            if (resizableInventory?.inventoryShape == null) yield break;
            RefreshAllGridsOf(item);
            try { contentWindow.FixBounds(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            try { contentWindow.Validate(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            RectTransform rectTransform = contentWindow.rectTransform;
            if (rectTransform != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
            if (contentWindow.outsideBorder != null && contentWindow.outsideBorder.rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(contentWindow.outsideBorder.rectTransform);
            Component invComponent = ((Il2CppObjectBase)resizableInventory).TryCast<Component>();
            if (invComponent == null || invComponent.gameObject == null) yield break;
            foreach (Graphic componentsInChild in invComponent.gameObject.GetComponentsInChildren<Graphic>(true))
                if (componentsInChild != null) componentsInChild.SetAllDirty();
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        // （2026-10-13：ResizeDiag 的 "POST" 采样点随探针一并删除。）
    }
}