using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using ContainerSuite.Shared;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SaveBagExpand;

/// <summary>
/// 方向一：主仓库分页。
/// 主仓库网格保持游戏原版尺寸，物品按「页」拆开存在内存 + UserData 旁路文件
/// （不走 MelonPreferences，见「配置持久化」段取证），
/// TAB 键循环切页时交换网格内容。
/// </summary>
public static partial class SaveBagPaging
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

    // ★ 2026-10-12：拖拽中切页后，"跟随鼠标的物品图（假体）"复原重试计数器。
    //   见 ReassertDragGhostIfDragging 的机制注释。
    private static int _ghostReassertFrames;

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
        // 页 ID 走私有旁路文件，不需要 category 参数。
        _pageCount = Math.Clamp(pageCount, 1, 10);

        InitStorages();
        SaveBagExpandCore.Diag($"[SaveBagExpand] 分页已初始化：{_pageCount} 页");
        ContainerSuite.Shared.ContainerSuiteLog.Mark("sbe.paging", _pageCount > 0, "pages=" + _pageCount);
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
                    // 不 CreateEntry（MelonPreferences 在本会话已被
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
        ContainerSuite.Shared.ContainerSuiteLog.Mark("sbe.bind", true,
            "slot=" + slotId + " pages=" + _pageCount + " at=" + _boundAtSec.ToString("0.00") +
            " shapeChanged=" + (shapeChanged ? "1" : "0"));
    }

    // ────────── 存档槽隔离 ──────────
    // 原 EnsureEntries（MelonPreferences CreateEntry 按槽建键）已删除。
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
}
