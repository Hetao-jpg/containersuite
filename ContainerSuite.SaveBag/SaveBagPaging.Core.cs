using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using ContainerSuite.Shared;   // S2 新增
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SaveBagExpand;

public static partial class SaveBagPaging
{
    internal static bool TryCycleNext()
    {
        // ★ 2026-09-30：全部日志改 TabLog，受 _dumpDiagEnabled 控制
        if (_pageCount <= 1)
        {
            TabLog("[TAB 拦截] _pageCount<=1");
            return false;
        }
        if (_mainGrid == null)
        {
            TabLog("[TAB 拦截] _mainGrid=null");
            return false;
        }
        if (_busy)
        {
            TabLog("[TAB 拦截] _busy=true");
            return false;
        }
        if (_mergedForSave)
        {
            TabLog("[TAB 拦截] _mergedForSave=true");
            return false;
        }

        // 2026-10-12：拖拽判定（见 ProbeTabDrag 注释）。
        GameItem held = null;
        TabDragVerdict verdict = ProbeTabDrag(out held, out string why);
        if (verdict == TabDragVerdict.Block)
        {
            TabLog("[TAB 拦截] " + why);
            return false;
        }
        if (verdict != TabDragVerdict.SingleItemDrag && IsMouseButtonHeld())
        {
            // 没在拖单件物品，但左键按着（框选 / 拖窗口 / 拖容器边缘改尺寸）——
            // 维持原拦截：这些场景换页会把网格内容整批搬走，不划算也不安全。
            TabLog(verdict == TabDragVerdict.Unavailable
                ? "[TAB 拦截] 拖拽判定不可用，退回左键判据"
                : "[TAB 拦截] 左键按着但不在拖物品（框选/拖窗口/改尺寸）");
            return false;
        }
        if (verdict == TabDragVerdict.SingleItemDrag)
            TabLog("[TAB 放行] 拖拽中切页，手上=" + DescribeHeld(held));

        int target = (_currentPage + 1) % _pageCount;
        if (target == _currentPage)
        {
            TabLog($"[TAB 拦截] target==_currentPage ({_currentPage})");
            return false;
        }

        int curCount = (_currentPage >= 0 && _currentPage < _storages.Count)
            ? _storages[_currentPage].Count : -1;
        int tgtCount = (target >= 0 && target < _storages.Count)
            ? _storages[target].Count : -1;

        TabLog(
            $"[TAB 进入] 当前页={_currentPage + 1}({curCount}个) → 目标页={target + 1}({tgtCount}个)");

        SwitchPage(target, held);
        return true;
    }

    private static void SwitchPage(int target, GameItem held = null)
    {
        _busy = true;
        try
        {
            IntPtr heldPtr = IntPtr.Zero;
            if (held != null)
            {
                try { heldPtr = held.Pointer; }   // GameItem 继承自 Il2CppObjectBase，无需再转型
                catch (Exception ex) { CatchLogOnce.Warn("paging.SwitchPage.heldPtr", ex); }
            }
            if (heldPtr != IntPtr.Zero)
            {
                // ★ 这一行同时是"游戏用的是哪种拖拽机制"的现场证据：
                //   已不在网格 ⇒ 拿起的瞬间就移出去了（换页天然不碰它）；
                //   仍在网格 ⇒ 幽灵式，靠下面 skipPtr 跳过，不把它搬走。
                TabLog(GridContainsPtr(_mainGrid, heldPtr)
                    ? "[TAB 切换] 手上那件**仍在网格里**（幽灵式）→ 已从快照/清空里跳过"
                    : "[TAB 切换] 手上那件已不在网格里 → 无需跳过，松手后落在新页");
            }
            // S9 安全网：切页前先确保网格只含"当前页"的物品。
            //   若不先校正，读档窗口内（观察窗口还没赶上/被 _busy 挡下）按 TAB 会把
            //   "全页合并"的网格快照进当前页 storage —— 那一页从此永久多出别页的物品。
            //   这也是旧版"换页之后才显示正常"的成因：第一次切页顺带把污染带回正轨。
            //   现在把它前置成无条件动作，任何时刻按 TAB 都是干净的。
            if (EnsureGridOwnershipClean() > 0) SaveIdSets();

            SnapshotCurrentPage(heldPtr);
            ClearGrid(_mainGrid, heldPtr);
            PlaceAll(_mainGrid, _storages[target]);

            // ★ 2026-10-12：上面的 Expel/UncheckedAccept 会让游戏重新 Validate 网格里
            //   每个元素，而 ValidateSelectLocked 会把"跟随鼠标的假体"拆掉（见
            //   ReassertDragGhostIfDragging 注释）—— 拖着物品按 TAB 时表现就是
            //   "绿色落点还在、物品图回到原格不跟手"。这里用游戏自己的
            //   UpdateFakeReal(当前拾取数) 把它复原（含堆叠数量的真/假分配）。
            //   放在 PlaceAll **之后**：拆是发生在 PlaceAll 的调用栈里的。
            if (held != null)
            {
                ReassertDragGhostIfDragging();
                _ghostReassertFrames = 2;   // 保险：网格 Validate 未必只发生在我们的栈内
                TabLog("[TAB 切换] 已复原拖拽假体（真/假数量按当前拾取数重算）");
            }

            _currentPage = target;
            RefreshPageIds();
            SaveIdSets();
            _lastSwitchAt = Time.realtimeSinceStartup;

            SaveBagExpandCore.Diag($"[SaveBagExpand] 主仓库切到第 {_currentPage + 1}/{_pageCount} 页");
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Error($"[SaveBagExpand] 切页失败: {ex.Message}");
        }
        finally
        {
            _busy = false;
        }
    }

    // ────────── 内部实现 ──────────

    /// <summary>
    /// 按 _pageCount 重建分页容器。
    /// <paramref name="preserve"/> = true（热改页数）时保留已有页的内容，
    /// 被裁掉页里的物品交给 <see cref="RehomeOverflow"/> 安置，不静默丢弃。
    /// </summary>
    private static void InitStorages(bool preserve = false)
    {
        var overflow = new List<ItemSlot>();

        if (preserve)
        {
            while (_storages.Count > _pageCount)
            {
                int last = _storages.Count - 1;
                overflow.InsertRange(0, _storages[last]);
                _storages.RemoveAt(last);
            }
            while (_pageIds.Count > _pageCount) _pageIds.RemoveAt(_pageIds.Count - 1);
        }
        else
        {
            _storages.Clear();
            _pageIds.Clear();
        }

        for (int i = _storages.Count; i < _pageCount; i++) _storages.Add(new List<ItemSlot>());
        for (int i = _pageIds.Count; i < _pageCount; i++) _pageIds.Add(new HashSet<int>());

        if (overflow.Count > 0) RehomeOverflow(overflow);
    }

    /// <summary>
    /// 页数减少时安置被裁掉页里的物品：优先放进「非当前页」的 storage，
    /// 实在没有别的页（缩到 1 页）才塞回主网格。每一步都有日志，不静默丢物品。
    /// </summary>
    private static void RehomeOverflow(List<ItemSlot> overflow)
    {
        int target = -1;
        for (int i = _pageCount - 1; i >= 0; i--)
        {
            if (i != _currentPage) { target = i; break; }
        }

        if (target >= 0)
        {
            _storages[target].AddRange(overflow);
            SaveBagExpandCore.Log.Warning(
                $"[SaveBagExpand] 页数减少：{overflow.Count} 个物品已移到第 {target + 1} 页"
                + "（未直接放回网格，切到该页即可见）");
            return;
        }

        // 只剩 1 页：没有别的页可放，只能塞回主网格
        if (_mainGrid != null)
        {
            foreach (var slot in overflow) Place(_mainGrid, slot.Item, slot.X, slot.Y);
            SaveBagExpandCore.Log.Warning(
                $"[SaveBagExpand] 页数已减到 1 页：{overflow.Count} 个物品尝试放回主网格"
                + "（装不下的可能丢失，建议先扩大仓库再减页）");
        }
        else
        {
            SaveBagExpandCore.Log.Error(
                $"[SaveBagExpand] 页数已减到 1 页且主仓库未绑定：{overflow.Count} 个物品无法安置！"
                + "请把页数改回去再进店");
        }
    }

    private static void DistributeFromGrid()
    {
        var snapshot = Snapshot(_mainGrid);
        ClearGrid(_mainGrid);

        foreach (var slot in snapshot)
        {
            int uid = Uid(slot.Item);
            int page = FindPage(uid);
            if (page == 0 || page == _currentPage)
                Place(_mainGrid, slot.Item, slot.X, slot.Y);
            else if (page < _storages.Count)
                _storages[page].Add(slot);
            else
                Place(_mainGrid, slot.Item, slot.X, slot.Y);
        }

        SnapshotCurrentPage();
    }

    /// <summary>
    /// 把主网格的现状存进"当前页"的虚拟 storage。
    /// <paramref name="skipPtr"/> ≠ 0 时跳过该指针的物品 —— 2026-10-12：TAB 拖拽中切页时，
    /// 手上那件不能被存进"旧页"（否则读档/合并会把它算到旧页，与新页里实际位置矛盾）。
    /// </summary>
    private static void SnapshotCurrentPage(IntPtr skipPtr = default)
    {
        if (_currentPage < 0 || _currentPage >= _storages.Count) return;

        if (_mergedForSave) return;

        _storages[_currentPage].Clear();
        _storages[_currentPage].AddRange(Snapshot(_mainGrid, skipPtr));

        var currentPtrs = new HashSet<IntPtr>();
        foreach (var slot in _storages[_currentPage])
        {
            try { if (slot.Item != null) currentPtrs.Add(slot.Item.Pointer); } catch (Exception ex) { CatchLogOnce.Warn("paging.SnapshotCurrentPage.ptr", ex); }
        }
        for (int i = 0; i < _storages.Count; i++)
        {
            if (i == _currentPage) continue;
            _storages[i].RemoveAll(s => {
                try { return s.Item != null && currentPtrs.Contains(s.Item.Pointer); }
                catch (Exception ex) { CatchLogOnce.Warn("paging.SnapshotCurrentPage.removeAll", ex); return false; }
            });
        }
    }

    private static void PlaceAll(GameGridInventory grid, List<ItemSlot> slots)
    {
        if (grid == null || slots == null) return;

        var onGrid = new HashSet<IntPtr>();
        try
        {
            var items = grid.childItems;
            if (items != null)
                for (int i = 0; i < items.Count; i++)
                {
                    try
                    {
                        var it = items[i];
                        if (it != null) onGrid.Add(it.Pointer);
                    }
                    catch (Exception ex) { CatchLogOnce.Warn("paging.PlaceAll.items", ex); }
                }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.PlaceAll", ex); }

        foreach (var slot in slots)
        {
            if (slot?.Item == null) continue;
            IntPtr ptr = IntPtr.Zero;
            try { ptr = slot.Item.Pointer; } catch (Exception ex) { CatchLogOnce.Warn("paging.PlaceAll.ptr", ex); }
            if (ptr != IntPtr.Zero && onGrid.Contains(ptr)) continue;
            Place(grid, slot.Item, slot.X, slot.Y);
            if (ptr != IntPtr.Zero) onGrid.Add(ptr);
        }
    }

    private static System.Reflection.MethodInfo _uxMi, _uyMi, _uaMi;

    // Place 的反射实参缓冲：复用同一个数组，避免合并/重放时每件物品各分配一次。
    // 每次 Invoke 前都会重新赋值 args[0]。前提假设：反射 setter 不会回调进本类再次调用 Place
    //（否则外层 Invoke 的 args[0] 会被内层覆盖）；当前游戏实现无此回调，故不额外加锁。
    private static readonly object[] _placeArgs = new object[1];

    private static void Place(GameGridInventory grid, GameItem item, int x, int y)
    {
        if (grid == null || item == null) return;
        // S8u-3h（15:28 切页闪退取证）：Place 的调用序列 = 预热 1 + 读档 Bind 14
        // + 第 2 次切页 14 = 29 次，第 4 次切页第 1 件 = 第 30 次 = tier1 重编译阈值。
        // tier1 全优化会把 set_modifiedXOrigin/set_modifiedYOrigin/UncheckedAccept
        // （带参 il2cpp 小包装器，IL 含 localloc）内联进本方法的嵌套 try → JIT 致命
        // （0x80131506@CompileMethodHook）。启动探测器只覆盖 PrepareMethod 级编译，
        // 覆盖不到 tier1，故此处直接反射化三处调用：本方法体内不再有任何游戏
        // 互操作内联候选，任意优化级/tier 都免疫；虚分发语义与原 callvirt 等价。
        if (_uxMi == null)
        {
            const System.Reflection.BindingFlags fi =
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance;
            _uxMi = typeof(GameItem).GetMethod("set_modifiedXOrigin", fi);
            _uyMi = typeof(GameItem).GetMethod("set_modifiedYOrigin", fi);
            _uaMi = typeof(GameInventory).GetMethod("UncheckedAccept", fi);
        }
        try
        {
            int gw = 0, gh = 0;
            try
            {
                var s = grid.inventoryShape;
                if (s != null) { gw = s.width; gh = s.height; }
            }
            catch (Exception ex) { CatchLogOnce.Warn("paging.Place.shape", ex); }
            if (gw > 0) x = Math.Clamp(x, 0, gw - 1);
            if (gh > 0) y = Math.Clamp(y, 0, gh - 1);

            var args = _placeArgs;
            if (_uxMi != null) try { args[0] = x; _uxMi.Invoke(item, args); } catch (Exception ex) { CatchLogOnce.Warn("paging.Place.setX", ex); }
            if (_uyMi != null) try { args[0] = y; _uyMi.Invoke(item, args); } catch (Exception ex) { CatchLogOnce.Warn("paging.Place.setY", ex); }
            if (_uaMi != null) { args[0] = item; _uaMi.Invoke(grid, args); }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.Place", ex); }
    }

}
