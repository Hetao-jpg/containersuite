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
    private static int FindPage(int uid)
    {
        if (uid == 0) return 0;
        for (int i = 1; i < _pageIds.Count; i++)
            if (_pageIds[i].Contains(uid)) return i;
        return 0;
    }

    // ══════════════════════════════════════════════════════════════════════
    // S9（2026-10-07）：页归属自愈
    // ══════════════════════════════════════════════════════════════════════
    //
    // 【要解决的问题】读档后"第二页的物品显示在第一页"。
    //   根因不是页 ID 丢失（S8u-4 已修），而是**分发只发生一次、且发生得太晚**：
    //   存档文件本身就是"全页合并"存的（存档前 BeforeSave 会把所有页并进一个网格），
    //   读档后游戏把这份合并内容整份灌进主仓库网格；本 mod 必须按页 ID 再把
    //   外页物品移出去。旧代码只在 Bind 里做一次，而 Bind 被
    //   「最晚一次 MarkReady + 3s」拖到读档后 ~4s，窗口内屏幕上就是合并态。
    //
    // 【为什么改成"看几秒"而不是"猜一个延迟"】灌进来的时刻取决于游戏内部时序，
    //   我们不去猜（猜早了 = 网格还空着，分发等于没做，且再也没机会补）。
    //   改成绑定后开启观察窗口，窗口内每 0.2s 看一次网格；
    //   只要出现"不属于当前页"的物品就移回各自页面。早到晚到都能自愈。
    //
    // 【为什么只移"外页"、不重排当前页】当前页的物品坐标是存档里存好的，
    //   原地不动最省事也最不容易出错。旧代码 ClearGrid + 全量重放
    //   （~140 次反射 Expel + ~137 次反射 Place）实测单帧 100ms；
    //   新逻辑读档时通常只动第二页那几件 → 2~3 次调用 ≈ 1ms。
    //
    // 【归属判据：当前页优先】uid 同时出现在当前页页集里 → 视为当前页物品、不动。
    //   避免"两边都登记过"时来回搬运（抖动）。
    //
    // 【触发点】① Tick 的观察窗口（主路径，覆盖读档）；
    //          ② SwitchPage 切页前（保证 TAB 任何时候按都不会把合并态快照进当前页）；
    //          ③ BeforeSave 存档前（同上，避免合并态被 RefreshPageIds 写进页 ID）。
    //   —— ②③ 是"安全网"：即使观察窗口因为某种原因没赶上，这两个时刻也一定会校正。

    /// <summary>
    /// 把网格上"不属于当前页"的物品移回各自页面；返回移走的件数（幂等、可重入）。
    /// 只更新内存中的 <c>_storages / _pageIds</c> 并置 <see cref="_idsDirty"/>，
    /// 落盘由调用方决定（切页 / 存档路径本来就会 SaveIdSets）。
    /// </summary>
    private static int EnsureGridOwnershipClean()
    {
        if (_mainGrid == null) return 0;
        // ★ 存档合并态：网格里"故意"装着全部页（BeforeSave 干的事），这时候
        //   按页归属去清 = 把刚合并好的内容又拆掉 → 存档会少东西。必须无条件跳过。
        //   （SaveGame 的 Prefix/Postfix 之间 Tick 仍可能跑，这个守卫不是多余的。）
        if (_mergedForSave) return 0;
        if (_pageIds.Count <= 1) return 0;
        if (_currentPage < 0 || _currentPage >= _pageIds.Count) return 0;
        // 零成本早退：除当前页外没有任何页登记过 ID → 不可能命中，直接返回。
        // 老存档（S8u-4 之前页 ID 从未落盘 → 全空）走的就是这条早退，行为与旧版一致。
        if (!HasForeignPageId()) return 0;

        var foreign = new List<ItemSlot>();
        try
        {
            var items = _mainGrid.childItems;
            if (items == null) return 0;
            for (int i = 0; i < items.Count; i++)
            {
                GameItem it = null;
                try { it = items[i]; } catch (Exception ex) { CatchLogOnce.Warn("paging.EnsureOwnership.items", ex); }
                if (it == null) continue;

                int uid = Uid(it);
                if (uid == 0) continue;
                if (CurrentPageOwns(uid)) continue;          // 当前页优先：不动
                int page = ForeignPageOf(uid);
                if (page < 0 || page >= _storages.Count) continue;

                int x = 0, y = 0;
                try { x = it.modifiedXOrigin; } catch (Exception ex) { CatchLogOnce.Warn("paging.EnsureOwnership.x", ex); }
                try { y = it.modifiedYOrigin; } catch (Exception ex) { CatchLogOnce.Warn("paging.EnsureOwnership.y", ex); }
                foreign.Add(new ItemSlot { Item = it, X = x, Y = y });
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.EnsureGridOwnershipClean", ex); }

        if (foreign.Count == 0) return 0;

        // Expel 走反射：与 CG_ExpelLoop 同一条铁律（带参 il2cpp 包装器不得内联进
        // 带 EH 的方法体，见维护日志 S8u-3e）。_expelMi 已由 CG_ExpelLoop 公用。
        if (_expelMi == null) _expelMi = typeof(GameInventory).GetMethod("Expel");
        if (_expelMi == null) return 0;

        var buf = new object[1];
        int moved = 0;
        foreach (var slot in foreign)
        {
            int uid = Uid(slot.Item);
            if (uid == 0) continue;
            // 再判一次归属：Expel 过程中页集理论上不变，但重入安全起见用最新结果
            if (CurrentPageOwns(uid)) continue;
            int page = ForeignPageOf(uid);
            if (page < 0 || page >= _storages.Count) continue;

            bool expelled = false;
            try { buf[0] = slot.Item; _expelMi.Invoke(_mainGrid, buf); expelled = true; }
            catch (Exception ex) { CatchLogOnce.Warn("paging.EnsureOwnership.Expel", ex); }
            if (!expelled) continue;   // A-1：没移走就不登记（否则页表与网格不一致，存档合并会重复 Place）

            _storages[page].Add(slot);
            _pageIds[_currentPage].Remove(uid);   // 清掉可能存在的误登记
            _pageIds[page].Add(uid);
            moved++;
        }

        if (moved <= 0) return 0;

        _idsDirty = true;   // 交给调用方落盘（Tick 直接 SaveIdSets，切页/存档路径本来就会写）

        SaveBagExpandCore.Diag(
            $"[SaveBagExpand] 页归属自愈：{moved} 件物品已从当前页移回各自的页" +
            $"（当前第 {_currentPage + 1}/{_pageCount} 页，距门禁首次放行 " +
            $"{WorldReadyGate.SecondsSinceFirstReady:0.00}s）");
        // 无条件结构化标记（不走 Diag 开关）：这是"读档窗口是否被修好"的唯一机器可读证据。
        ContainerSuite.Shared.ContainerSuiteLog.Mark("sbe.reconcile", true,
            "moved=" + moved + " at=" + WorldReadyGate.SecondsSinceFirstReady.ToString("0.00"));
        return moved;
    }

    /// <summary>除当前页外，是否还有任一页登记过 ID（决定自愈要不要扫）。</summary>
    private static bool HasForeignPageId()
    {
        for (int i = 0; i < _pageIds.Count; i++)
        {
            if (i == _currentPage) continue;
            if (_pageIds[i].Count > 0) return true;
        }
        return false;
    }

    /// <summary>uid 是否已被当前页登记（当前页优先，避免来回搬运）。</summary>
    private static bool CurrentPageOwns(int uid)
    {
        try { return _pageIds[_currentPage].Contains(uid); } catch (Exception ex) { CatchLogOnce.Warn("paging.CurrentPageOwns", ex); return false; }
    }

    /// <summary>uid 归属的「非当前页」索引；-1 = 不属于其它任何页。</summary>
    private static int ForeignPageOf(int uid)
    {
        for (int i = 0; i < _pageIds.Count; i++)
        {
            if (i == _currentPage) continue;
            if (_pageIds[i].Contains(uid)) return i;
        }
        return -1;
    }

    private static void RefreshPageIds()
    {
        for (int i = 0; i < _pageIds.Count; i++) _pageIds[i].Clear();
        for (int i = 0; i < _storages.Count && i < _pageIds.Count; i++)
            foreach (var slot in _storages[i])
            {
                int uid = Uid(slot.Item);
                if (uid != 0) _pageIds[i].Add(uid);
            }

        if (_mainGrid != null && _currentPage >= 0 && _currentPage < _pageIds.Count)
        {
            foreach (var slot in Snapshot(_mainGrid))
            {
                int uid = Uid(slot.Item);
                if (uid != 0) _pageIds[_currentPage].Add(uid);
            }
        }
    }

    private static int Uid(GameItem item)
    {
        if (item == null) return 0;
        try { return item.uniqueId; } catch (Exception ex) { CatchLogOnce.Warn("paging.Uid", ex); return 0; }
    }

    private static List<ItemSlot> Snapshot(GameGridInventory grid, IntPtr skipPtr = default)
    {
        var list = new List<ItemSlot>();
        if (grid == null) return list;

        var seen = new HashSet<IntPtr>();
        try
        {
            var items = grid.childItems;
            if (items == null) return list;
            for (int i = 0; i < items.Count; i++)
            {
                GameItem it = null;
                try { it = items[i]; } catch (Exception ex) { CatchLogOnce.Warn("paging.Snapshot.items", ex); }
                if (it == null) continue;

                IntPtr ptr = IntPtr.Zero;
                try { ptr = it.Pointer; } catch (Exception ex) { CatchLogOnce.Warn("paging.Snapshot.ptr", ex); }
                if (ptr != IntPtr.Zero && !seen.Add(ptr)) continue;
                // 2026-10-12：拖拽中切页时，手上那件不进快照（见 SwitchPage）。
                if (skipPtr != IntPtr.Zero && ptr == skipPtr) continue;

                try { _ = it.uniqueId; } catch (Exception ex) { CatchLogOnce.Warn("paging.Snapshot.uid", ex); continue; }

                int x = 0, y = 0;
                try { x = it.modifiedXOrigin; } catch (Exception ex) { CatchLogOnce.Warn("paging.Snapshot.x", ex); }
                try { y = it.modifiedYOrigin; } catch (Exception ex) { CatchLogOnce.Warn("paging.Snapshot.y", ex); }
                list.Add(new ItemSlot { Item = it, X = x, Y = y });
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.Snapshot", ex); }
        return list;
    }

    // S8u-3d：三起崩溃（13:52 读档 / 14:28+14:38 启动）全部钉死在 ClearGrid 编译，
    // 反编译 IL 为完全标准的 Roslyn 输出（无异常结构）。按区域拆成 3 个子方法：
    //   1) 降低单方法 EH 复杂度，可能直接绕开 JIT 致命点；
    //   2) 下一次若仍崩，堆栈方法名直接指认区域（CG_Scan / CG_ExpelLoop / 组装体）。
    private static void ClearGrid(GameGridInventory grid, IntPtr skipPtr = default)
    {
        if (grid == null) return;
        // S8u-3：一次性标记 —— 若预热后仍崩，最后一行标记 = 致死调用。
        if (!_cgDiag1) { _cgDiag1 = true; SaveBagExpandCore.Diag("[SBE-JIT] ClearGrid 入口"); }
        if (!_cgDiag2) { _cgDiag2 = true; SaveBagExpandCore.Diag("[SBE-JIT] ClearGrid → childItems/identifier 遍历"); }

        var toExpel = CG_Scan(grid, skipPtr);

        if (!_cgDiag3) { _cgDiag3 = true; SaveBagExpandCore.Diag("[SBE-JIT] ClearGrid → Expel 循环"); }
        CG_ExpelLoop(grid, toExpel);
    }

    private static List<GameItem> CG_Scan(GameGridInventory grid, IntPtr skipPtr = default)
    {
        var toExpel = new List<GameItem>();
        if (grid == null) return toExpel;
        var seen = new HashSet<IntPtr>();
        try
        {
            var items = grid.childItems;
            if (items != null)
            {
                for (int i = 0; i < items.Count; i++)
                {
                    GameItem it = null;
                    try { it = items[i]; } catch (Exception ex) { CatchLogOnce.Warn("paging.CG_Scan.items", ex); }
                    if (it == null) continue;

                    IntPtr ptr = IntPtr.Zero;
                    try { ptr = it.Pointer; } catch (Exception ex) { CatchLogOnce.Warn("paging.CG_Scan.ptr", ex); }
                    if (ptr == IntPtr.Zero) continue;
                    if (!seen.Add(ptr)) continue;
                    // 2026-10-12：拖拽中切页时，手上那件不参与清空（见 SwitchPage）。
                    if (skipPtr != IntPtr.Zero && ptr == skipPtr) continue;

                    bool alive = false;
                    try
                    {
                        string probe = it.identifier;
                        alive = !string.IsNullOrEmpty(probe);
                    }
                    catch (Exception ex) { CatchLogOnce.Warn("paging.CG_Scan.alive", ex); alive = false; }
                    if (!alive) continue;

                    toExpel.Add(it);
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.CG_Scan", ex); }

        try
        {
            foreach (var slot in Snapshot(grid, skipPtr))
            {
                if (slot.Item == null) continue;
                IntPtr ptr = IntPtr.Zero;
                try { ptr = slot.Item.Pointer; } catch (Exception ex) { CatchLogOnce.Warn("paging.CG_Scan.snapshotPtr", ex); }
                if (ptr != IntPtr.Zero && !seen.Add(ptr)) continue;
                toExpel.Add(slot.Item);
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.CG_Scan.snapshot", ex); }

        return toExpel;
    }

    private static System.Reflection.MethodInfo _expelMi;

    private static void CG_ExpelLoop(GameGridInventory grid, List<GameItem> toExpel)
    {
        // S8u-3e 取证结论：三起 0x80131506（13:52 读档 / 14:28 / 14:38+15:10 启动）
        // 全部钉死在「编译含 callvirt GameInventory.Expel 的方法」。Expel 是本 mod
        // 唯一的带参 il2cpp 包装器调用，其 IL 含 mul.ovf.un + localloc（参数缓冲栈分配）；
        // 本地反编译证实 get_childItems 等无参 getter（CG_Scan 里在 try 内编译通过）
        // 均无 localloc。致命点 = 把含 localloc 的被调方内联进带 EH 的调用方时
        // CoreCLR JIT 致命错（standalone 编译 Expel 本身无害，组 1 已证明）。
        // 修法：改反射 Invoke —— 本方法体内只剩 BCL 调用，零游戏互操作内联候选，
        // 无论何种机制都绕开；虚分发与原 callvirt 等价（GameGridInventory.Expel 覆写）。
        if (_expelMi == null)
            _expelMi = typeof(GameInventory).GetMethod("Expel");
        if (grid == null || toExpel == null || _expelMi == null) return;
        var buf = new object[1];
        for (int i = 0; i < toExpel.Count; i++)
        {
            var it = toExpel[i];
            if (it == null) continue;
            try { buf[0] = it; _expelMi.Invoke(grid, buf); } catch (Exception ex) { CatchLogOnce.Warn("paging.CG_ExpelLoop", ex); }
        }
    }

    // ────────── 内联诊断 ──────────

    private static void DumpDiag(string tag)
    {
        try
        {
            var snap = Snapshot(_mainGrid);
            SaveBagExpandCore.Diag(
                $"[分页诊断] {tag}：slot={_boundSlotId} 当前页={_currentPage + 1}/{_pageCount} " +
                $"网格物品={snap.Count} 个");

            for (int i = 0; i < snap.Count && i < 8; i++)
            {
                int uid = Uid(snap[i].Item);
                SaveBagExpandCore.Diag(
                    $"[分页诊断]   物品#{i}: uid={uid} @({snap[i].X},{snap[i].Y})");
            }
            if (snap.Count > 8)
                SaveBagExpandCore.Diag($"[分页诊断]   ...（共 {snap.Count} 个，只列前 8）");

            var sb = new StringBuilder("[分页诊断] _pageIds: ");
            for (int i = 0; i < _pageIds.Count; i++)
            {
                sb.Append($"页{i + 1}=").Append(_pageIds[i].Count).Append("个");
                if (i < _pageIds.Count - 1) sb.Append(", ");
            }
            SaveBagExpandCore.Diag(sb.ToString());

            // S8u-4：改为直接显示旁路文件里的本槽行（原 Entry 值已随 MelonPreferences 弃用）
            string raw = null;
            try { raw = FindIdsLine(ReadIdsLines(), _boundSlotId); } catch (Exception ex) { CatchLogOnce.Warn("paging.DumpDiag.readIds", ex); }
            SaveBagExpandCore.Diag($"[分页诊断] 文件行: {raw ?? "(无)"}");
        }
        catch (Exception ex)
        {
            // ★ 保持无条件（2026-10-09）：DumpDiag 自己崩了属于故障，不是诊断。
            SaveBagExpandCore.Log.Msg("[分页诊断] 输出异常: " + ex.Message);
        }
    }

}
