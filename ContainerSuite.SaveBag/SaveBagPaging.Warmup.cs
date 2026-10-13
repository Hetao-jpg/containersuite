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
    // ────────── S8u-3：Bind 链 JIT 预热 ──────────

    private static bool _warmedUp;
    // S8u-3 一次性诊断标记：预热后若仍崩，控制台最后一行标记直接指认致死调用。
    private static bool _cgDiag1, _cgDiag2, _cgDiag3;

    /// <summary>
    /// S8u-3c（2026-10-06）：Bind 链 JIT 预热 —— 改用「真实调用」而非 PrepareMethod。
    ///
    /// 取证链：
    /// 二期（13:52 读档）：Bind 推迟到 DIY.RestoreReady+3.99s 仍崩在
    ///   ClearGrid → CompileMethodHook → 0x80131506；Snapshot（前驱）正常跑完
    ///   → 时间窗理论证伪，致命点 = 本会话 ClearGrid 链的首次方法编译。
    /// 三期启动（14:19/14:28）：PrepareMethod 预热自己也在启动时致死，
    ///   逐方法打点钉死在 `> SaveBagPaging.ClearGrid`。
    ///
    /// 拼合结论：两起崩溃同一凶手 —— **List&lt;GameItem&gt;.Add 的编译**：
    ///   ClearGrid 是本类唯一用到 List&lt;GameItem&gt;.Add（toExpel.Add）的方法；
    ///   13:52 = 运行时首调触发 JIT，14:28 = PREJIT 内联触发 —— 而 Mode B
    ///   （PrepareMethod/PREJIT）与运行期 Mode A 编译路径不同，13:52 已证明
    ///   ClearGrid 本体 Mode A 可正常编译执行。
    ///
    /// S8u-3c 做法（顺序即安全顺序）：
    ///   组3：集合冒烟 —— List&lt;GameItem&gt;.Add 排最前，Mode A 真实执行
    ///   组4：SaveBagPaging 全部 null 安全入口逐个真实调用（Mode A 首编，
    ///        与运行期同一编译模式；每步一行打点，崩则最后一行=凶手）
    ///
    /// S9x（2026-10-08）：按「编译模式」分治。
    ///   Mode A（真实执行）= 与运行期同路径 ⇒ 常开（组 3/4）；
    ///   Mode B（PrepareMethod）= 与运行期不同路径，已证可致 0x80131506
    ///   （FailFast，try/catch 抓不到）⇒ 组 1/2/5 及探针已全部删除，
    ///   R3 门禁据此零豁免。
    /// </summary>
    internal static void WarmupJit()
    {
        if (_warmedUp) return;
        _warmedUp = true;

        // 入口打点：否则无法区分"没跑"和"跑了但崩"（P3 回归判据也依赖这条进/出痕迹）。
        // ★ 2026-10-09：入口/每步打点改走 Diag（受 DiagLog 总闸控制）。
        //   原因：逐步追踪一次要打 ~28 行，是 Latest.log 的最大单一来源，而它只在
        //   「预热崩了、想知道崩在哪一步」时才有用 —— 那属于排障，不属于日常。
        //   ★ 但"预热完成"这一行仍是无条件（见末尾），因为 regress.ps1 拿它当启动哨兵。
        SaveBagExpandCore.Diag("[SBE-JIT] WarmupJit 进入");

        // S9a：预热耗时入日志（2026-10-06 实测约 130ms）。若哪天这个数字涨到
        // 秒级，说明预热范围失控，需要收窄到关键方法——数字留在日志里防回归。
        var warmupSw = System.Diagnostics.Stopwatch.StartNew();

        void Group(string label, Action body)
        {
            try { SaveBagExpandCore.Diag("[SBE-JIT] " + label); body(); }
            catch (Exception ex) { SaveBagExpandCore.Log.Warning($"[SBE-JIT] {label} 异常: {ex.Message}"); }
        }

        // Mode A 真实执行的成功步数：Mode B 已删除，这是收尾日志与 ContainerSuiteLog
        //   "预热确实做过事"的唯一证据。
        int modeASteps = 0;
        // 每步打点：崩则控制台最后一行 = 凶手步骤
        void Step(string label, Action a)
        {
            try { SaveBagExpandCore.Diag("[SBE-JIT] > " + label); a(); modeASteps++; }
            catch (Exception ex) { SaveBagExpandCore.Log.Warning($"[SBE-JIT] > {label} 异常: {ex.Message}"); }
        }

        // ── 组 3：集合冒烟（Mode A 真实执行）—— List<GameItem>.Add 是两起
        //    崩溃的头号凶手，必须排在自有方法之前被点着 ──
        Group("集合冒烟（Mode A 真实执行）", () =>
        {
            Step("List<GameItem>: new/Add(null)/AddRange/foreach/Clear", () =>
            {
                var li = new List<GameItem>();
                li.Add(null); li.AddRange(li);
                foreach (var _ in li) { }
                li.Clear();
            });
            Step("List<ItemSlot>: new/Add/AddRange/RemoveAll(lambda)/foreach/Clear", () =>
            {
                var l = new List<ItemSlot>();
                l.Add(new ItemSlot()); l.AddRange(l);
                l.RemoveAll(s => s == null);
                foreach (var _ in l) { }
                l.Clear();
            });
            Step("HashSet<IntPtr>: new/Add/Remove/foreach", () =>
            {
                var hs = new HashSet<IntPtr>();
                hs.Add(IntPtr.Zero); hs.Remove(IntPtr.Zero);
                foreach (var _ in hs) { }
            });
            Step("StringBuilder: new/Append/ToString", () =>
            {
                var sb = new StringBuilder(); sb.Append("x"); sb.ToString();
            });
        });

        // ── 组 4：SaveBagPaging 全部 null 安全入口（Mode A 首编，真实调用） ──
        //    所有调用不碰游戏状态：null 网格 / 空分页 / 未绑定槽（_boundSlotId=-1）。
        Group("SaveBagPaging 执行冒烟（Mode A 首编，逐个打点）", () =>
        {
            Step("get_DiagEnabled/get_IsBound/get_CurrentPage/get_PageCount/get_LastSwitchAt", () =>
            { _ = DiagEnabled; _ = IsBound; _ = CurrentPage; _ = PageCount; _ = LastSwitchAt; });
            Step("TabLog(\"\")", () => TabLog(""));
            Step("ReadSaveSlotId", () => { ReadSaveSlotId(); });
            Step("IsMouseButtonHeld", () => { IsMouseButtonHeld(); });
            // 2026-10-12：TAB 拖拽判定的新入口 —— 现在在 TAB 热路径上，
            //   提前在这里编译（主菜单没有拖拽 ⇒ ItemMouseDragHandler.current 可能为 null，
            //   ProbeTabDrag 走 NotDragging 早退，无副作用）。
            Step("ProbeTabDrag(拖拽判定)", () => { ProbeTabDrag(out _, out _); });
            Step("GridContainsPtr/DescribeHeld", () => { _ = GridContainsPtr(null, IntPtr.Zero); _ = DescribeHeld(null); });
            // 2026-10-12：拖拽假体复原（切页路径的新入口；主菜单没有拖拽 ⇒
            //   ItemMouseDragHandler.current 可能为 null，走首行早退，无副作用）。
            Step("ReassertDragGhostIfDragging（拖拽中切页的假体复原）",
                () => { ReassertDragGhostIfDragging(); TickDragGhostReassert(); });
            Step("Uid(null)/FindPage(0)", () => { Uid(null); FindPage(0); });
            Step("Snapshot(null)", () => { Snapshot(null); });
            // ↓ S9：页归属自愈链（读档主路径）——必须在这里首编，
            //    否则它的首次真实调用就落回"读档期编译 + MonoMod 钩子"的老坑。
            Step("HasForeignPageId/CurrentPageOwns/ForeignPageOf（S9 自愈判据）",
                () => { HasForeignPageId(); CurrentPageOwns(0); ForeignPageOf(0); });
            Step("EnsureGridOwnershipClean（S9 自愈主体，_mainGrid=null → 早退）",
                () => { EnsureGridOwnershipClean(); });
            // ↓ S8u-3d：ClearGrid 已按区域拆 3 段，子方法先编译组装体后编——
            //    若再崩，堆栈方法名直接指认区域（CG_Scan=扫描段 / CG_ExpelLoop=清除段）。
            Step("CG_Scan(null)（拆分·扫描段，14:28/14:38 凶手主体）", () => CG_Scan(null));
            Step("CG_ExpelLoop(null,null)（拆分·Expel 段）", () => CG_ExpelLoop(null, null));
            Step("ClearGrid(null)（拆分·组装体）", () => ClearGrid(null));
            Step("DistributeFromGrid（_mainGrid=null，全链 null 安全）", () => DistributeFromGrid());
            Step("Place(null)/PlaceAll(null)", () => { Place(null, null, 0, 0); PlaceAll(null, null); });
            Step("RefreshPageIds/SaveIdSets/LoadIdSets", () => { RefreshPageIds(); SaveIdSets(); LoadIdSets(); });
            Step("页ID旁路文件自检（写→读→还原，S8u-4）", IdsFileSelfTest);
            Step("InitStorages（与 Initialize 等效重建空分页）", () => InitStorages());
            Step("SnapshotCurrentPage（触发 RemoveAll lambda 首编）", () => SnapshotCurrentPage());
            Step("TryCycleNext（_mainGrid=null → 拦截 return）", () => { TryCycleNext(); });
            Step("BeforeSave/AfterSave（_mainGrid=null/_未合并 → return）", () => { BeforeSave(); AfterSave(); });
        });

        warmupSw.Stop();
        // ★ 这一行保持【无条件】（2026-10-09）：regress.ps1:136 拿它当"启动已完成"的哨兵，
        //   等不到就会一路等到超时判 WARN；checks.json 的 jit.warm.text 也断言它。
        //   它同时是"预热成功"这个结论本身，属于"加载成功"级别，不是逐步诊断。
        SaveBagExpandCore.Log.Msg($"[SBE-JIT] 预热完成：modeA={modeASteps} 耗时={warmupSw.ElapsedMilliseconds}ms");
        // ★ Mode A 真实执行步数即"预热确实做过事"的唯一证据，
        //   否则此处会打成 FAIL（checks.json 的 reg.sbe.jit 要求 status=PASS）。
        ContainerSuite.Shared.ContainerSuiteLog.Mark("sbe.jit", modeASteps > 0,
            $"modeA={modeASteps}");
    }

    // ────────── 存档钩子 ──────────

    public static void BeforeSave()
    {
        if (_busy) return;
        _busy = true;
        try
        {
            if (_mainGrid == null) return;

            if (_mergedForSave)
            {
                SaveBagExpandCore.Diag("[SaveBagExpand] BeforeSave 已在合并状态，跳过重复调用");
                return;
            }

            // S9 安全网：存档前先确保网格只含当前页的物品。
            //   否则紧随其后的 SnapshotCurrentPage + RefreshPageIds 会把"全页合并"态
            //   整份登记成当前页的页 ID —— 页归属被永久抹平（比显示错更严重）。
            if (EnsureGridOwnershipClean() > 0) SaveIdSets();

            SnapshotCurrentPage();
            RefreshPageIds();
            SaveIdSets();

            // ★ 2026-09-30：诊断默认静音
            if (DiagEnabled) DumpDiag("BeforeSave 前");

            // B1：合并前先记账。这份基准用来在合并后按指针做差，
            // 差集就是"没写进存档"的物品（README 限制 #7 的现场取证）。
            List<StoredItem> stored = CollectStored();

            ClearGrid(_mainGrid);
            for (int i = 0; i < _storages.Count; i++)
                foreach (var slot in _storages[i])
                    Place(_mainGrid, slot.Item, slot.X, slot.Y);
            _mergedForSave = true;
            _mergedForSaveAt = Time.realtimeSinceStartup;

            DetectMergeLoss(stored);
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Warning("[SaveBagExpand] BeforeSave 失败: " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    /// <summary>
    /// 存档后恢复当前页。
    /// <paramref name="force"/> = true 时跳过状态检查，供看门狗强制解锁使用。
    /// </summary>
    public static void AfterSave(bool force = false)
    {
        TabLog($"[分页诊断] AfterSave 调用：force={force}，_mergedForSave={_mergedForSave}，网格={(_mainGrid != null ? "有" : "无")}");
        if (_busy) { TabLog("[分页诊断] AfterSave 被 _busy 挡下"); return; }
        _busy = true;
        try
        {
            if (!force && !_mergedForSave) return;

            if (_mainGrid == null)
            {
                // 网格已经没了（读档 / 退店）。状态必须清，否则 TAB 会一直被拦。
                _mergedForSave = false;
                if (force) SaveBagExpandCore.Log.Warning("[SaveBagExpand] 看门狗：主仓库网格为空，仅清理合并状态");
                return;
            }

            ClearGrid(_mainGrid);
            if (_currentPage >= 0 && _currentPage < _storages.Count)
                foreach (var slot in _storages[_currentPage])
                    Place(_mainGrid, slot.Item, slot.X, slot.Y);
            _mergedForSave = false;
            if (force)
                SaveBagExpandCore.Log.Warning($"[SaveBagExpand] 看门狗已恢复分页（回到第 {_currentPage + 1} 页）");
            TabLog($"[分页诊断] AfterSave 完成：已回到第 {_currentPage + 1} 页，_mergedForSave=false");
        }
        catch (Exception ex)
        {
            // B4：存档后没恢复回当前页 = 玩家看到的是空仓库，属数据可见问题，
            // 用 Warning 才会带 [WARNING] 级别（Msg 在日志里跟普通流水一个样）。
            SaveBagExpandCore.Log.Warning("[SaveBagExpand] AfterSave 失败: " + ex.Message);
        }
        finally
        {
            _busy = false;
        }
    }

    public static void AfterLoad()
    {
        try
        {
            // ★ S5-2：读档后不知道会落在哪个商店 → 模式回到 Unknown。
            //   放在去重判断之前，保证每次读档都清掉上一次的残留信号。
            FrontInvModeTracker.Reset("读档，模式待重新判定");

            if (Time.realtimeSinceStartup - _lastAfterLoadAt < 0.5f)
            {
                SaveBagExpandCore.Diag("[SaveBagExpand] AfterLoad 短时间内重复调用，跳过");
                return;
            }
            _lastAfterLoadAt = Time.realtimeSinceStartup;

            _mainGrid = null;
            _currentPage = 0;
            _busy = false;
            _mergedForSave = false;
            _reconcileUntil = -1f;      // S9：观察窗口随读档作废，下次 Bind 重开
            _reconcilePoll = 0f;
            InitStorages();

            SaveBagExpandCore.Diag("[SaveBagExpand] 主仓库分页已重置（等待下次进店重绑定）");
        }
        catch (Exception ex)
        {
            // ★ 保持无条件（2026-10-09）：读档后重建分页失败是数据层面的故障，必须一直可见。
            SaveBagExpandCore.Log.Error("[SaveBagExpand] AfterLoad 失败: " + ex.Message);
        }
    }

}

/// <summary>存档前合并所有页；存档后恢复当前页。</summary>
[HarmonyPatch(typeof(PlayerStore), "SaveGame")]
internal static class SaveBagPagingSavePatch
{
    [HarmonyPrefix]
    private static void Prefix() => SaveBagPaging.BeforeSave();

    [HarmonyPostfix]
    private static void Postfix()
    {
        SaveBagPaging.TabLog("[分页诊断] SaveGame Postfix 执行");
        SaveBagPaging.AfterSave();
    }

    /// <summary>
    /// ★ 2026-10-02：Finalizer 无论 SaveGame 是否抛异常都会执行。
    /// 原来只有 Postfix —— 游戏存档一旦抛异常，Postfix 被跳过，
    /// _mergedForSave 永远卡在 true，TAB 切页静默死锁。
    /// 这里兜底复位；异常原样抛出，不改变游戏行为。
    /// </summary>
    [HarmonyFinalizer]
    private static Exception Finalizer(Exception __exception)
    {
        try
        {
            SaveBagPaging.TabLog("[分页诊断] SaveGame Finalizer 执行，异常=" +
                (__exception == null ? "无" : __exception.GetType().Name));

            if (__exception != null)
                SaveBagExpandCore.Log.Warning(
                    "[SaveBagExpand] 检测到 SaveGame 抛异常（" + __exception.GetType().Name +
                    "），分页状态已兜底恢复");

            SaveBagPaging.AfterSave();
        }
        catch (Exception ex)
        {
            // ★ 保持无条件（2026-10-09）：Finalizer 兜底路径崩了 = 存档状态可能不一致。
            SaveBagExpandCore.Log.Error("[SaveBagExpand] SaveGame Finalizer 异常: " + ex.Message);
        }
        return __exception;   // 原样抛出，不吞异常
    }
}

