using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using ContainerSuite.Shared;   // S2 新增
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SaveBagExpand;

/// <summary>
/// 方向一：主仓库分页。
/// 主仓库网格保持游戏原版尺寸，物品按「页」拆开存在内存 + UserData 旁路文件
/// （S8u-4 起不再走 MelonPreferences，见「配置持久化」段取证），
/// TAB 键循环切页时交换网格内容。
/// </summary>
public static class SaveBagPaging
{
    internal sealed class ItemSlot
    {
        public GameItem Item;
        public int X;
        public int Y;
    }

    private static GameGridInventory _mainGrid;
    private static int _currentPage;
    private static int _pageCount = 1;
    private static readonly List<List<ItemSlot>> _storages = new List<List<ItemSlot>>();
    private static readonly List<HashSet<int>> _pageIds = new List<HashSet<int>>();
    private static bool _busy;
    private static bool _mergedForSave;
    private static int _boundSlotId = -1;
    private static float _poll;
    private static bool _idsDirty;          // S8u-4：旁路文件写失败 → Tick 重试
    private static bool _idsWarned;         // 写/读失败每会话只警告一次
    private static float _lastAfterLoadAt = -10f;
    private static float _lastSwitchAt = -10f;

    // ★ S9（2026-10-07）：页归属自愈轮询状态
    private static float _reconcilePoll;                    // 窗口内的轮询累加器
    private static float _reconcileUntil = -1f;              // 自愈观察窗口的截止时刻（realtime）
    private static float _boundAtSec = -1f;                  // 绑定时刻（距首次放行秒数，日志用）

    // ★ 2026-09-30：新增 3 个字段
    // ★ 2026-10-02：诊断日志开关改成读配置；S8g 起已合并为 CI 的统一诊断开关 DiagLog。
    private static bool DiagEnabled
    {
        get
        {
            try
            {
                var e = SaveBagExpandConfig.DiagLog;   // S8g：合并后的统一诊断开关
                return e != null && e.Value;
            }
            catch (Exception ex) { CatchLogOnce.Warn("paging.DiagEnabled", ex); return false; }
        }
    }
    /// <summary>进入「存档合并」状态的时间戳（看门狗用）。</summary>
    private static float _mergedForSaveAt = -10f;
    /// <summary>合并状态卡住超过这么多秒，看门狗强制恢复（TAB 解锁）。</summary>
    private const float MergeStuckTimeout = 10f;
    /// <summary>上次落盘时间（重试节流用）。</summary>
    private static float _lastPrefsSaveAt = -10f;
    /// <summary>落盘重试间隔（秒）。仅旁路文件写失败后才用到。</summary>
    private const float PrefsSaveInterval = 5f;

    /// <summary>
    /// S9（2026-10-07）：Bind 消抖 —— 门禁放行后再等这么久才允许绑定。
    ///
    /// 【旧行为的问题（实机取证）】
    ///   S8u-2 起本值是 3s，且判据是「距【最晚一次】MarkReady」
    ///   （WorldReadyGate.SecondsSinceLastReady）。读档实测时间线
    ///   （Logs\2026-10-07_10-59-07.log）：
    ///     11:00:07.196  CI.LoadGame → AfterLoad（分页重置，_mainGrid=null）
    ///     11:00:07.754  开门（CI.RestoreDone）      ← 门禁其实早就放行了
    ///     11:00:09.483  开门（DIY.RestoreReady）    ← 最晚一次，判据从这起算
    ///     11:00:13.468  Bind（= 09.483 + 3.98s）
    ///   → 09.483 + 3s 稳定期 + 最多 1s 轮询粒度 = 读档后 3.98s 才开始绑定。
    ///   这段时间 _mainGrid 为 null：TAB 被 mod.cs 的 IsBound 判断挡下（无法换页），
    ///   而网格里是存档的"全页合并"内容（第二页物品显示在第一页）。
    ///
    /// 【S9 的两处修改】
    ///   ① 判据从「最晚一次开门」改成「本轮第一次开门」
    ///      （WorldReadyGate.SecondsSinceFirstReady）。理由见 WorldReadyGate 内注释：
    ///      用 LastReady 等于让"恢复最慢的 mod"决定分页可用时间，装一个长恢复 mod
    ///      就会把窗口拖长。用 FirstReady 则上界稳定（本轮最快开门者的时刻）。
    ///      读档场景下 FirstReady = CI.RestoreDone（≈+0.56s），不再等 DIY 的 +2.3s。
    ///   ② 3s → <see cref="BindSettleSeconds"/>。
    ///
    /// 【为什么现在敢缩】旧 3s 是"缩小暴露面"的防御，不是致死点（见维护日志 S8u-2 段）。
    ///   真致死点（0x80131506）是"带参 il2cpp 包装器 localloc 被内联进带 EH 的方法"，
    ///   已由 S8u-3e/3h 的反射化 + 启动期全量预热根治：
    ///     · 预热在主菜单完成（实测 10:59:26.895 完成，读档在 11:00:07 —— 提前 40s），
    ///       本类所有方法（含 Expel 包装器）已被 PREJIT + Mode A 真实执行过，
    ///       读档期不会再触发 MonoMod CompileMethodHook；
    ///     · S9 还把 Bind 里的"ClearGrid + 全量重放"挪走（见 Bind / EnsureGridOwnershipClean），
    ///       绑定本身不再做重网格操作 —— 原来 3s 想规避的那件事在绑定路径上已经不存在了。
    /// </summary>
    private const float BindSettleSeconds = 0.2f;

    /// <summary>
    /// S9：绑定后"页归属自愈"的观察窗口（秒）。窗口内以
    /// <see cref="ReconcileInterval"/> 轮询网格，把不属于当前页的物品移回各自页面。
    ///
    /// 为什么需要窗口：读档后游戏把存档里的"全页合并"内容灌进网格，本 mod 要按页 ID
    /// 再把外页物品移出去。灌进来的时刻取决于游戏内部时序（LoadGame 解码 / 进店流程），
    /// 我们不为它猜一个固定延迟，而是"绑定后持续看几秒"——无论灌进来的是早是晚都能自愈。
    /// 窗口设 6s：远超灌入所需时间，且代价极低（见 EnsureGridOwnershipClean 的零成本早退）。
    /// </summary>
    private const float ReconcileWindowSeconds = 6f;

    /// <summary>S9：窗口内自愈轮询间隔（秒）。</summary>
    private const float ReconcileInterval = 0.2f;

    /// <summary>TAB 拦截日志。仅在诊断开关打开时输出。（internal：存档补丁类也要用）</summary>
    internal static void TabLog(string msg)
    {
        if (DiagEnabled) SaveBagExpandCore.Diag(msg);   // TabLog：诊断专用出口，已天然受闸门控制
    }

    // ★ 抄 InventoryPages：用 handler 探测 native 存活
    internal static bool IsBound
    {
        get
        {
            if (_mainGrid == null) return false;
            try
            {
                _ = _mainGrid.handler;
                return true;
            }
            catch
            {
                _mainGrid = null;
                return false;
            }
        }
    }

    internal static int CurrentPage => _currentPage;
    internal static int PageCount => _pageCount;
    internal static float LastSwitchAt => _lastSwitchAt;

    internal static void Initialize(int pageCount)
    {
        // S8u-4：页 ID 已改走私有旁路文件，不再需要 category 参数。
        _pageCount = Math.Clamp(pageCount, 1, 10);

        InitStorages();
        SaveBagExpandCore.Diag($"[SaveBagExpand] 分页已初始化：{_pageCount} 页");
        ContainerSuite.Shared.RegMark.Mark("sbe.paging", _pageCount > 0, "pages=" + _pageCount);
    }

    /// <summary>
    /// 由 mod.cs 每帧调用。S9 起内部分三条通道：
    ///   ① 未绑定时**每帧**尝试绑定（幂等 + 轻量，见 Bind）；
    ///   ② 绑定后的 6 秒窗口内每 0.2s 做一次页归属自愈（见 EnsureGridOwnershipClean）；
    ///   ③ 1 秒一次的低频通道（热改页数 / 落盘重试）。
    /// </summary>
    internal static void Tick(GameGridInventory maybeGrid)
    {
        // S8u（2026-10-05 补录）：加载期不绑定、不分发（禁止事项 #22）。
        // 看门狗（下方）会调 AfterSave 写网格，所以本闸门必须在它之前。
        // 缺这道闸时：轮询 1 秒后 Bind → DistributeFromGrid → ClearGrid/Expel
        // 在 MonoMod 深栈里触发 Internal CLR error 0x80131506。
        if (WorldReadyGate.ShouldDefer) return;

        try
        {
            // ★ 看门狗（2026-10-02）：Postfix / Finalizer 都没跑时的最后兜底。
            //   存档合并状态卡住超过 10 秒 → 强制恢复，保证 TAB 不会永久锁死。
            //   放在 _busy 判断之前：绝不能被任何中间状态挡住。
            if (_mergedForSave && Time.realtimeSinceStartup - _mergedForSaveAt > MergeStuckTimeout)
            {
                SaveBagExpandCore.Log.Warning(
                    $"[SaveBagExpand] 存档合并状态卡住超过 {MergeStuckTimeout:0} 秒，看门狗强制恢复（TAB 解锁）");
                AfterSave(force: true);
            }

            // S9：消抖闸 —— 判据由 SecondsSinceLastReady(最晚一次) 改为
            // SecondsSinceFirstReady(本轮第一次)，值 3s → 0.2s。
            // 看门狗必须最先跑（绝不能被任何中间状态挡住），消抖闸在它之后。
            if (WorldReadyGate.SecondsSinceFirstReady < BindSettleSeconds) return;

            if (_busy) return;

            // ── 通道 ①：绑定（每帧尝试；Bind 内部幂等，已绑定即刻返回） ──
            //   S9 前这里被压在下方的 1 秒轮询里，等于绑定又额外拖了 0~1 秒。
            if (!IsBound && maybeGrid != null) Bind(maybeGrid);

            // ── 通道 ②：页归属自愈（仅绑定后的观察窗口内） ──
            //   读档后游戏会把自己那份"全页合并"内容灌进网格，灌入时刻我们不去猜，
            //   窗口内持续观察，任何一刻发现外页物品就移回各自页面。
            //   窗口之外这条通道零成本（一个 float 比较）。
            if (_reconcileUntil > 0f && Time.realtimeSinceStartup < _reconcileUntil)
            {
                _reconcilePoll += Time.deltaTime;
                if (_reconcilePoll >= ReconcileInterval)
                {
                    _reconcilePoll = 0f;
                    if (EnsureGridOwnershipClean() > 0) SaveIdSets();
                }
            }

            // ── 通道 ③：1 秒一次的低频维护 ──
            _poll += Time.deltaTime;
            if (_poll < 1f) return;
            _poll = 0f;

            // ★ 热改总页数 → 重建 storages
            int wantPages = Math.Clamp(SaveBagExpandConfig.SaveBagPageCount.Value, 1, 10);
            if (wantPages != _pageCount)
            {
                int oldPages = _pageCount;
                _pageCount = wantPages;
                if (_currentPage >= _pageCount) _currentPage = 0;

                // ★ 2026-10-02：保留已有页内容。原来无脑 InitStorages() 清空，
                //   内存里其它页的物品会直接消失（cfg 里没落盘的那部分彻底找不回来）。
                InitStorages(preserve: true);

                if (_boundSlotId >= 0 && _pageIds.Count > 0)
                {
                    // S8u-4：不再 CreateEntry（MelonPreferences 在本会话已被
                    // 第三方 mod 竞争打死，见「配置持久化」段）——用内存里的
                    // 真实分布重算后直写旁路文件。
                    RefreshPageIds();
                    SaveIdSets();
                }

                SaveBagExpandCore.Diag(
                    $"[SaveBagExpand] 总页数变化 {oldPages} → {wantPages}，重建分页（原有内容已保留）");
            }

            // ★ 旁路文件写失败重试（成功路径在 SaveIdSets 里已同步写过，这里只是兜底）。
            //   S8u-4：原 MelonPreferences.Save() 已弃用——每次启动 +10~23s
            //   第三方 mod 竞争打死后 WasError 会让整个会话的写盘全部静默失效。
            if (_idsDirty)
            {
                if (Time.realtimeSinceStartup - _lastPrefsSaveAt > PrefsSaveInterval)
                {
                    _lastPrefsSaveAt = Time.realtimeSinceStartup;
                    SaveIdSets();
                }
            }
        }
        catch (Exception ex)
        {
            // ★ 保持无条件（2026-10-09）：Tick 是每帧主循环，它抛异常是故障不是诊断。
            SaveBagExpandCore.Log.Msg("[SaveBagExpand] Tick: " + ex.Message);
        }
    }

    internal static void Bind(GameGridInventory mainGrid)
    {
        if (mainGrid == null) return;
        // S8u（2026-10-05 补录）：加载期不绑定（禁止事项 #22）。
        // S9 起 Bind 已不再做重网格操作（ClearGrid / 全量重放挪到了
        // EnsureGridOwnershipClean，且只在必要时动必要的几件），
        // 这道闸 + 消抖仍是强制点：Bind 会写 _boundSlotId / 读旁路文件，
        // 加载期跑没有意义。
        if (WorldReadyGate.ShouldDefer) return;
        // S9：消抖判据改为「本轮第一次放行」，值 3s → 0.2s（详见 BindSettleSeconds）。
        if (WorldReadyGate.SecondsSinceFirstReady < BindSettleSeconds) return;

        int slotId = ReadSaveSlotId();

        // ★ 槽位变了 → 强制全清（相当于 AfterLoad）
        if (slotId >= 0 && slotId != _boundSlotId)
        {
            _mainGrid = null;
            _currentPage = 0;
            _busy = false;
            _mergedForSave = false;
            _boundSlotId = -1;
            InitStorages();
        }

        // ★ 用 IsBound 探测（native 死后返回 false，允许重绑）
        if (IsBound) return;

        _mainGrid = mainGrid;
        _currentPage = 0;
        _busy = false;
        _mergedForSave = false;

        // S8u-4：原 EnsureEntries 职责（绑定槽位）——页 ID 旁路文件按槽位行存取，
        // 只需要槽号就绪；slotId<0 时保持 -1，LoadIdSets 会跳过。
        if (slotId >= 0) _boundSlotId = slotId;
        InitStorages();
        LoadIdSets();
        if (slotId >= 0)
            SaveBagExpandCore.Diag(
                $"[SaveBagExpand] 已为存档槽 {slotId} 加载分页配置（{_pageCount} 页，旁路文件）");

        bool expand = SaveBagExpandConfig.ExpandEnabled.Value;

        int expectedW, expectedH;
        if (expand)
        {
            expectedW = SaveBagExpandConfig.Clamp(SaveBagExpandConfig.SaveBagWidth.Value);
            expectedH = SaveBagExpandConfig.Clamp(SaveBagExpandConfig.SaveBagHeight.Value);
            if (expectedW <= 0) expectedW = 24;
            if (expectedH <= 0) expectedH = 10;
        }
        else
        {
            expectedW = 24;
            expectedH = 10;
        }

        int curW = 0, curH = 0;
        try
        {
            var s = _mainGrid.inventoryShape;
            if (s != null) { curW = s.width; curH = s.height; }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.Bind.shape", ex); }

        bool shapeChanged = (curW != expectedW || curH != expectedH);
        if (shapeChanged)
        {
            try { _mainGrid.SetShape(expectedW, expectedH); } catch (Exception ex) { CatchLogOnce.Warn("paging.Bind.SetShape", ex); }
            SaveBagExpandCore.Diag(
                $"[SaveBagExpand] Bind 网格调整为 {expectedW}x{expectedH}（之前 {curW}x{curH}，扩容={expand}）");
        }

        // ────────── S9：绑定不再无条件全量重放 ──────────
        //
        // 旧行为：无条件 DistributeFromGrid() —— 读档那一刻对 ~140 件物品做
        //   ClearGrid(140 次反射 Expel) + 重放(~137 次反射 Place)，
        //   实测 Perf 里 SBE 单帧尖刺 100019μs（≈100ms 卡顿），
        //   而且因为只跑这一次，Bind 早于"游戏把合并内容灌进网格"就永远修不回来。
        //
        // 新行为：
        //   · 形状真的变了（curW/curH ≠ cfg）→ 物品坐标可能越界，保留旧的全量重放
        //     （这是唯一需要重排坐标的场景，回归面为零）；
        //   · 形状没变（读档的常规情形）→ 不做任何网格操作，交给
        //     EnsureGridOwnershipClean 在观察窗口里把"外页"物品逐件移走。
        //     读档时通常只有第二页那几件要动 → 2~3 次反射调用，100ms → ~1ms。
        if (shapeChanged)
        {
            _busy = true;
            try
            {
                DistributeFromGrid();
            }
            finally
            {
                _busy = false;
            }
        }
        else
        {
            // 立刻开始观察：绑定后第一帧就试一次自愈（灌入通常早已发生）
            _reconcilePoll = ReconcileInterval;
        }

        // S9：开启自愈观察窗口（无论走哪条分支都要开——读档的合并态两种分支都可能出现）
        _reconcileUntil = Time.realtimeSinceStartup + ReconcileWindowSeconds;
        _boundAtSec = WorldReadyGate.SecondsSinceFirstReady;

        // ★ 2026-09-30：诊断默认静音
        if (DiagEnabled) DumpDiag("Bind 完成");

        SaveBagExpandCore.Diag(
            $"[SaveBagExpand] 主仓库分页已绑定（存档槽 {slotId}，{_pageCount} 页；" +
            $"距门禁首次放行 {_boundAtSec:0.00}s，形状{(shapeChanged ? "有调整" : "未变")}）");
        ContainerSuite.Shared.RegMark.Mark("sbe.bind", true,
            "slot=" + slotId + " pages=" + _pageCount + " at=" + _boundAtSec.ToString("0.00") +
            " shapeChanged=" + (shapeChanged ? "1" : "0"));
    }

    // ────────── 存档槽隔离 ──────────
    // S8u-4：原 EnsureEntries（MelonPreferences CreateEntry 按槽建键）已删除。
    // 取证：ML 在每次启动 +10~23s 被第三方 mod 写竞争打成 WasError，此后
    // 整个会话所有 MelonPreferences 落盘静默失效 —— Slot*_Page2Ids 历史上
    // 从未真正写进过盘（11 个历史槽全为 ""）。页 ID 改走 SaveBagPaging.ids。

    internal static int ReadSaveSlotId()
    {
        try
        {
            PlayerStore ps = PlayerStore.instance;
            if (ps != null) return ps.saveSlotId;
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.ReadSaveSlotId", ex); }
        return -1;
    }

    // ────────── TAB 循环切页 ──────────

    private static bool IsDraggingNow()
    {
        // 输入模块不可用属无害情形，静默忽略。
        try { return Input.GetMouseButton(0); } catch { return false; }
    }

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
        if (IsDraggingNow())
        {
            TabLog("[TAB 拦截] IsDraggingNow=true（鼠标左键按着）");
            return false;
        }

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

        SwitchPage(target);
        return true;
    }

    private static void SwitchPage(int target)
    {
        _busy = true;
        try
        {
            // S9 安全网：切页前先确保网格只含"当前页"的物品。
            //   若不先校正，读档窗口内（观察窗口还没赶上/被 _busy 挡下）按 TAB 会把
            //   "全页合并"的网格快照进当前页 storage —— 那一页从此永久多出别页的物品。
            //   这也是旧版"换页之后才显示正常"的成因：第一次切页顺带把污染带回正轨。
            //   现在把它前置成无条件动作，任何时刻按 TAB 都是干净的。
            if (EnsureGridOwnershipClean() > 0) SaveIdSets();

            SnapshotCurrentPage();
            ClearGrid(_mainGrid);
            PlaceAll(_mainGrid, _storages[target]);

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

    private static void SnapshotCurrentPage()
    {
        if (_currentPage < 0 || _currentPage >= _storages.Count) return;

        if (_mergedForSave) return;

        _storages[_currentPage].Clear();
        _storages[_currentPage].AddRange(Snapshot(_mainGrid));

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
        ContainerSuite.Shared.RegMark.Mark("sbe.reconcile", true,
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

    private static List<ItemSlot> Snapshot(GameGridInventory grid)
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
    private static void ClearGrid(GameGridInventory grid)
    {
        if (grid == null) return;
        // S8u-3：一次性标记 —— 若预热后仍崩，最后一行标记 = 致死调用。
        if (!_cgDiag1) { _cgDiag1 = true; SaveBagExpandCore.Diag("[SBE-JIT] ClearGrid 入口"); }
        if (!_cgDiag2) { _cgDiag2 = true; SaveBagExpandCore.Diag("[SBE-JIT] ClearGrid → childItems/identifier 遍历"); }

        var toExpel = CG_Scan(grid);

        if (!_cgDiag3) { _cgDiag3 = true; SaveBagExpandCore.Diag("[SBE-JIT] ClearGrid → Expel 循环"); }
        CG_ExpelLoop(grid, toExpel);
    }

    private static List<GameItem> CG_Scan(GameGridInventory grid)
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
            foreach (var slot in Snapshot(grid))
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

    // ────────── 配置持久化（S8u-4：UserData 私有旁路文件） ──────────
    //
    // 取证（2026-10-06 全会话日志）：每次启动 +10~23s 必现——第三方 mod
    // 「自动化酿酒拓展」写完 MelonPreferences.cfg，3ms 后「良心商人」再写 →
    // IOException「being used by another process」→ ML 设 WasError →
    // File.Save() 此后整个会话直接 return（IsSaving=false），所有 mod 的
    // MelonPreferences 落盘全部静默失效。Bind 时 CreateEntry 虽能从内存
    // 文档回填，但文档里根本没有值（11 个历史槽位 Slot*_Page2Ids 全是 ""，
    // 227 槽行都不存在）→ 读档 FindPage 全返回 0 → 第二页物品全部叠进第一页。
    // 结论：页 ID 不能走 MelonPreferences。格式（每行一个槽，ASCII）：
    //     slotId;pageCount;page2Ids;page3Ids;...
    // ★ T4（2026-10-12）：槽号后面可带一段「局标识」：
    //     slotId#runKey;pageCount;page2Ids;...
    //   带 #runKey 的行只在 runKey 与当前这一局一致时才匹配；**不带**该段的（老文件）
    //   仍按老规则匹配 —— 保证升级套件本身不会让玩家已有的页归属失效。
    // 写入在 SaveIdSets 里同步落盘（存档合并/切页即刻生效），失败才进
    // Tick 的 5 秒重试（_idsDirty）。

    private static string IdsFilePath =>
        System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "SaveBagPaging.ids");

    private static List<string> ReadIdsLines()
    {
        if (!System.IO.File.Exists(IdsFilePath)) return new List<string>();
        return new List<string>(System.IO.File.ReadAllLines(IdsFilePath));
    }

    private static void WriteIdsLines(List<string> lines)
    {
        string dir = System.IO.Path.GetDirectoryName(IdsFilePath);
        if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        string tmp = IdsFilePath + ".tmp";
        System.IO.File.WriteAllLines(tmp, lines, new UTF8Encoding(false));
        System.IO.File.Move(tmp, IdsFilePath, true);   // 同卷原子替换
    }

    // ── B2：旁路文件版本头 ──────────────────────────────────────────
    //
    // 为什么要有：S8u-4 那次是"读到空值也不吭声" → 第二页全叠进第一页，
    // 静默失效最难查。加一行 #v1 之后，下次改格式时老套件能看见版本不一样，
    // 至少能在日志里说一句"格式不认"，而不是把新格式当旧格式解析。
    //
    // 兼容性：FindIdsLine 已经把 '#' 开头的行整行跳过（读侧零影响），
    //          写侧由 EnsureIdsHeader 在 SaveIdSets 落盘前补齐。
    private const string IdsFileVersion = "#v1";
    private static bool _idsVerWarned;

    private static void EnsureIdsHeader(List<string> lines)
    {
        if (lines == null) return;

        int first = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            string t = lines[i] == null ? "" : lines[i].Trim();
            if (t.Length == 0) continue;
            first = i;
            break;
        }

        if (first < 0) { lines.Insert(0, IdsFileVersion); return; }   // 空文件

        string head = lines[first].Trim();
        if (head[0] != '#') { lines.Insert(first, IdsFileVersion); return; }   // 老文件无头 → 补

        if (!string.Equals(head, IdsFileVersion, StringComparison.Ordinal) && !_idsVerWarned)
        {
            _idsVerWarned = true;
            SaveBagExpandCore.Log.Warning(
                $"[SaveBagExpand] 页ID旁路文件版本不一致（文件 {head}，本套件 {IdsFileVersion}）： " +
                "按现有规则尽力解析，页归属若不对，整理一次并存档即可用本版本覆盖");
            // 注意：此处**不会**改动/删除原文件的版本行（EnsureIdsHeader 只在缺头时插入），
            // 因此万一将来出现 #v2 等新格式，本版本不会把它误标成 #v1 而破坏原文件；
            // 代价是此时按 v1 语义解析可能是错的 —— 这正是上面告警要提示的。
        }
    }

    /// <summary>
    /// T4（2026-10-12）：从一行里解出槽号。表头形如 <c>12</c> 或 <c>12#runKey</c>。
    /// <para>
    /// ★ 必须**只有这一处**做表头解析 —— 读侧（FindIdsLine）与写侧（SaveIdSets 的替换）
    ///   一旦用不同规则，写成 <c>12#run</c> 之后写侧认不出旧行（表头不再能 int.TryParse），
    ///   于是每次存档都**追加**一行、而读侧永远命中靠前的那条旧行 —— 页归属直接冻结。
    ///   这是实现 T4 时实际踩到的坑，故抽成公共实现。
    /// </para>
    /// </summary>
    private static bool TryParseSlotHead(string line, out int slot)
    {
        slot = -1;
        if (string.IsNullOrEmpty(line)) return false;
        string t = line.Trim();
        if (t.Length == 0 || t[0] == '#') return false;   // '#v1' / '#run=' 是头行，不是数据行
        int semi = t.IndexOf(';');
        string head = semi >= 0 ? t.Substring(0, semi) : t;
        int hash = head.IndexOf('#');
        if (hash >= 0) head = head.Substring(0, hash);    // 丢掉可选的局标识段
        return int.TryParse(head, out slot);
    }

    private static string FindIdsLine(List<string> lines, int slot)
    {
        // T4：当前局的标识（拿不到 = 空串 ⇒ 不做局隔离，保持旧行为）。
        string runKey = SaveSlotScope.GetRunKey();
        for (int i = 0; i < lines.Count; i++)
        {
            string t = lines[i] == null ? "" : lines[i].Trim();
            if (!TryParseSlotHead(t, out int s) || s != slot) continue;

            // T4：取出这一段局标识（表头里 '#' 之后的部分）。
            string runPart = null;
            int semi = t.IndexOf(';');
            string head = semi >= 0 ? t.Substring(0, semi) : t;
            int hash = head.IndexOf('#');
            if (hash >= 0) runPart = head.Substring(hash + 1);

            // T4：写了局标识却与本局不符 → 这一行属于**同一存档槽的上一局**（开新游戏），
            //   当作不存在。★ 只跳过这一行，绝不删别的行 —— 文件里还有其它存档槽的行。
            //   ★ 无局标识（老文件 / 自检探针行）一律按老规则匹配。
            if (!string.IsNullOrEmpty(runPart) && !string.IsNullOrEmpty(runKey)
                && !string.Equals(runPart, runKey, StringComparison.Ordinal))
            {
                if (!_idsRunForeignWarned)
                {
                    _idsRunForeignWarned = true;
                    SaveBagExpandCore.Log.Warning(
                        $"[SaveBagExpand] 页ID旁路文件里槽 {slot} 的记录属于**上一局**（runKey 不符），已忽略："
                        + "同槽开新游戏时物品 ID 会重排，沿用旧归属会让第二页物品叠进第一页。"
                        + "本次进游戏后整理一次并存档即可写入本局记录。");
                }
                continue;
            }
            return t;
        }
        return null;
    }

    // T4：局不符只告警一次（每次查询都刷屏没意义）。
    private static bool _idsRunForeignWarned;

    private static string BuildIdsLine()
    {
        var sb = new StringBuilder();
        sb.Append(_boundSlotId);
        // T4（2026-10-12）：把「局标识」拼在槽号后面（`<slot>#<run>`），只借蛙哥
        //   「#runID=」的思路做按局隔离；拿不到 runID 时**不写这一列**（老格式，仍然可用）。
        try
        {
            string runKey = SaveSlotScope.GetRunKey();
            if (!string.IsNullOrEmpty(runKey)) sb.Append('#').Append(runKey);
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.BuildIdsLine.runKey", ex); }
        sb.Append(';').Append(_pageCount);
        for (int i = 1; i < _pageIds.Count; i++)
            sb.Append(';').Append(FormatIds(_pageIds[i]));
        return sb.ToString();
    }

    private static void SaveIdSets()
    {
        if (_boundSlotId < 0 || _pageIds.Count == 0) return;
        try
        {
            var lines = ReadIdsLines();
            string line = BuildIdsLine();
            bool replaced = false;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i] == null ? "" : lines[i].Trim();
                // ★ T4：必须用与读侧**同一个**表头解析（TryParseSlotHead）——
                //   否则写成 `<slot>#<run>;...` 之后这里 int.TryParse 失败 ⇒ 认不出旧行
                //   ⇒ 每次存档追加一行，而读侧永远命中靠前的旧行（页归属冻结）。
                if (TryParseSlotHead(t, out int s) && s == _boundSlotId)
                {
                    lines[i] = line;
                    replaced = true;
                    break;
                }
            }
            if (!replaced) lines.Add(line);
            EnsureIdsHeader(lines);
            WriteIdsLines(lines);
            _idsDirty = false;
            _idsWarned = false;
        }
        catch (Exception ex)
        {
            _idsDirty = true;
            if (!_idsWarned)
            {
                _idsWarned = true;
                SaveBagExpandCore.Log.Warning(
                    "[SaveBagExpand] 页ID旁路文件写入失败（Tick 每 5 秒重试）: " + ex.Message);
            }
        }
    }

    private static void LoadIdSets()
    {
        if (_boundSlotId < 0 || _pageIds.Count == 0) return;
        for (int i = 1; i < _pageIds.Count; i++) _pageIds[i].Clear();
        try
        {
            string line = FindIdsLine(ReadIdsLines(), _boundSlotId);
            if (line == null) return;
            // slot;pageCount;page2;page3;... → _pageIds[1] = 页2 …
            var parts = line.Split(';');
            for (int p = 1; p < _pageIds.Count && p + 1 < parts.Length; p++)
                LoadIdSetInto(parts[p + 1], _pageIds[p]);
        }
        catch (Exception ex)
        {
            if (!_idsWarned)
            {
                _idsWarned = true;
                SaveBagExpandCore.Log.Warning(
                    "[SaveBagExpand] 页ID旁路文件读取失败: " + ex.Message);
            }
        }
    }

    /// <summary>退出时兜底：有未落盘的页 ID 改动就再试一次（Mod.OnApplicationQuit 调）。</summary>
    public static void FlushIdsOnQuit()
    {
        if (_idsDirty) SaveIdSets();
    }

    /// <summary>
    /// S8u-4 启动自检：向旁路文件写入临时槽 -999 → 回读断言 → 还原。
    /// 验证 UserData 可写、格式可解析；已进游戏（已绑定）时跳过。
    /// </summary>
    private static void IdsFileSelfTest()
    {
        if (_boundSlotId >= 0 || _pageIds.Count == 0) return;
        bool existed = System.IO.File.Exists(IdsFilePath);
        var original = ReadIdsLines();
        try
        {
            var probe = new List<string>(original) { "-999;2;7,8;9" };
            WriteIdsLines(probe);
            string back = FindIdsLine(ReadIdsLines(), -999);
            if (back != "-999;2;7,8;9")
                throw new Exception("回读不一致: " + (back ?? "(null)"));
            SaveBagExpandCore.Diag("[SBE-JIT] 页ID旁路文件自检通过: " + IdsFilePath);
        }
        finally
        {
            try
            {
                if (existed) WriteIdsLines(original);
                else if (System.IO.File.Exists(IdsFilePath)) System.IO.File.Delete(IdsFilePath);
            }
            catch (Exception ex)
            {
                SaveBagExpandCore.Log.Warning("[SBE-JIT] 页ID自检还原失败: " + ex.Message);
            }
        }
    }

    private static void LoadIdSetInto(string raw, HashSet<int> target)
    {
        if (string.IsNullOrEmpty(raw)) return;
        foreach (var token in raw.Split(','))
            if (int.TryParse(token.Trim(), out int v) && v != 0)
                target.Add(v);
    }

    private static string FormatIds(HashSet<int> set)
    {
        if (set == null || set.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var id in set)
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append(id);
        }
        return sb.ToString();
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

        // Mode A 真实执行的成功步数：Mode B 已删除，这是收尾日志与 RegMark
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
            Step("IsDraggingNow", () => { IsDraggingNow(); });
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
        ContainerSuite.Shared.RegMark.Mark("sbe.jit", modeASteps > 0,
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

    // ────────── B1：合并丢失检测 + 旁路清单 + 屏幕提示 ──────────
    //
    // README 已知限制 #7：分页 + 扩容组合的容量上限 = 单网格容量。
    // 存档时所有页必须合并回同一个网格，超出容量的物品 UncheckedAccept 直接拒收
    // → 存档里凭空消失，而且**一个字都不提**（静默丢档，最坏的一类 bug）。
    //
    // 这里做三段式：
    //   1) 合并前记账（按指针去重，带页号 / uid / identifier / 坐标）
    //   2) 合并后按指针集合做差，缺谁一目了然
    //   3) Warning 日志 + UserData\SaveBagPaging.mergeloss.txt 旁路清单 + 屏幕红字
    //
    // ★ 只"如实报告"，不自动往网格里塞回去：拒收的原因就是容量不够，
    //   再 Place 一次还是拒；强行改网格尺寸会改掉存档结构，风险比丢档更大。
    //   旁路清单的价值 = 玩家至少知道丢了什么、丢了多少（可按 uid/id 手动补）。

    public static bool HasMergeLoss { get; private set; }
    public static int MergeLossCount { get; private set; }
    public static float MergeLossAt { get; private set; }

    private struct StoredItem
    {
        public IntPtr Ptr;
        public int Page;
        public int Uid;
        public string Id;
        public int X;
        public int Y;
    }

    private static string MergeLossFilePath =>
        System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "SaveBagPaging.mergeloss.txt");

    /// <summary>合并前的记账：所有页里不重复的物品（按指针去重）。</summary>
    private static List<StoredItem> CollectStored()
    {
        var list = new List<StoredItem>();
        var seen = new HashSet<IntPtr>();
        for (int p = 0; p < _storages.Count; p++)
        {
            var slots = _storages[p];
            if (slots == null) continue;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null || s.Item == null) continue;

                IntPtr ptr = IntPtr.Zero;
                try { ptr = s.Item.Pointer; } catch (Exception ex) { CatchLogOnce.Warn("paging.CollectStored.ptr", ex); }
                if (ptr == IntPtr.Zero || !seen.Add(ptr)) continue;

                var rec = new StoredItem { Ptr = ptr, Page = p, X = s.X, Y = s.Y };
                // ★ 与 Snapshot() 的口径必须一致：Snapshot 对 uniqueId 取值抛异常的
                //   物品是 continue 跳过的，这里若照样记账，合并后那边数不到它，
                //   就会被误判成"丢失"（假阳性比漏报更糟，会让人不信这条告警）。
                try { rec.Uid = s.Item.uniqueId; } catch (Exception ex) { CatchLogOnce.Warn("paging.CollectStored.uid", ex); continue; }
                try { rec.Id = s.Item.identifier; } catch (Exception ex) { CatchLogOnce.Warn("paging.CollectStored.id", ex); rec.Id = null; }
                list.Add(rec);
            }
        }
        return list;
    }

    private static void DetectMergeLoss(List<StoredItem> before)
    {
        try
        {
            if (_mainGrid == null || before == null || before.Count == 0) return;

            var onGrid = new HashSet<IntPtr>();
            foreach (var slot in Snapshot(_mainGrid))
            {
                try { if (slot != null && slot.Item != null) onGrid.Add(slot.Item.Pointer); } catch (Exception ex) { CatchLogOnce.Warn("paging.DetectMergeLoss.ptr", ex); }
            }

            int gw = 0, gh = 0;
            try
            {
                var shape = _mainGrid.inventoryShape;
                if (shape != null) { gw = shape.width; gh = shape.height; }
            }
            catch (Exception ex) { CatchLogOnce.Warn("paging.DetectMergeLoss.shape", ex); }

            var missing = new List<StoredItem>();
            for (int i = 0; i < before.Count; i++)
                if (!onGrid.Contains(before[i].Ptr)) missing.Add(before[i]);

            if (missing.Count == 0)
            {
                // 这次没丢：上一次留下的屏幕警告可以撤掉
                if (HasMergeLoss) { HasMergeLoss = false; MergeLossCount = 0; }
                return;
            }

            HasMergeLoss = true;
            MergeLossCount = missing.Count;
            MergeLossAt = Time.realtimeSinceStartup;

            SaveBagExpandCore.Log.Warning(
                $"[SaveBagExpand] 存档合并丢失：{missing.Count}/{before.Count} 件没进网格" +
                $"（网格 {gw}x{gh}={gw * gh}，分页总件数超出单网格容量）" +
                $"，清单已写 {MergeLossFilePath}");

            WriteMergeLossFile(before.Count, missing, gw, gh);
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Warning("[SaveBagExpand] 合并丢失检测失败: " + ex.Message);
        }
    }

    private static void WriteMergeLossFile(int stored, List<StoredItem> missing, int gw, int gh)
    {
        try
        {
            var lines = new List<string>
            {
                "# SaveBagPaging 合并丢失清单（B1）",
                "# 说明：存档时所有页要合并进同一个网格；超出单网格容量的物品被拒收，",
                "#       没有写进存档文件。本清单只记录丢了什么（物品本体未被抢救）。",
                "# 数据行格式（分号分隔）: 页号;uid;identifier;x;y",
                "# time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                $"# grid={gw}x{gh}({gw * gh}) stored={stored} missing={missing.Count}"
            };
            for (int i = 0; i < missing.Count; i++)
            {
                var m = missing[i];
                string id = string.IsNullOrEmpty(m.Id) ? "-" : m.Id;
                lines.Add($"{m.Page};{m.Uid};{id};{m.X};{m.Y}");
            }

            string dir = System.IO.Path.GetDirectoryName(MergeLossFilePath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            string tmp = MergeLossFilePath + ".tmp";
            System.IO.File.WriteAllLines(tmp, lines, new UTF8Encoding(false));
            System.IO.File.Move(tmp, MergeLossFilePath, true);   // 同卷原子替换
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Warning("[SaveBagExpand] 合并丢失清单写入失败: " + ex.Message);
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
