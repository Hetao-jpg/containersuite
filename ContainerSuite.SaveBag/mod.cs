using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using ContainerSuite.Shared;
using UnityEngine;

namespace SaveBagExpand;

/// <summary>
/// S3-4：SBE 不再是 MelonMod，改为被 ContainerSuite 主 Mod 调用的静态模块。
/// 生命周期方法由主 Mod 转发：
///   OnInitializeMelon → Init(logger)
///   OnUpdate          → Tick()
///   OnGUI             → DrawGUI()
///   OnApplicationQuit → Shutdown()
/// </summary>
public static class SaveBagExpandCore
{
    internal static MelonLogger.Instance Log;
    internal static bool Enabled = true;

    // ★ S5-3（2026-10-02）：每格像素不再硬编码，改为从网格自身反推（抄 DIY 的写法）：
    //     cell = grid.widthPixels / grid.inventoryShape.width
    //   16f 只作为"网格还没初始化 / 读不到形状"时的兜底值。
    private const float CellPxFallback = 16f;

    /// <summary>窗口矩形相对网格像素的放大倍数。</summary>
    /// <remarks>
    /// 仍然硬编码 2.0：这个比例要从 <c>window.rectTransform.sizeDelta</c> 反推，
    /// 但 sizeDelta 里含边框像素（博士商店/主商店边框宽度还不一样），反推值会偏大，
    /// 拿去算鼠标命中范围会让判定范围失真 —— 风险大于收益，保持常量。
    /// </remarks>
    private const float RectScale = 2.0f;

    // ── 屏幕提示的布局常量（自 DrawMergeLossWarning / DrawGUI 提取） ──
    /// <summary>存档合并丢失告警的最大宽度。</summary>
    private const float MergeLossWarnMaxWidthPx = 820f;
    /// <summary>告警与屏幕左右边缘的留白。</summary>
    private const float MergeLossWarnMarginPx = 40f;
    /// <summary>切页提示框宽度。</summary>
    private const float PageHintWidthPx = 260f;
    /// <summary>切页提示框高度。</summary>
    private const float PageHintHeightPx = 36f;
    /// <summary>切页提示整体透明度系数。</summary>
    private const float PageHintAlphaScale = 0.7f;

    // ══════════════════════════════════════════════════════════════════
    // EmporiumEntry.Validate(bool force) 的调用实参（2026-10-07 审计新增）
    //
    // ★ 事实（三重取证，别再凭记忆）：
    //   Il2CppAssemblies/Assembly-CSharp.dll 里的签名是
    //       public void Validate(bool force = TRUE)
    //   —— 默认值**就是 true**。C# 可选参数的默认值由编译器在**调用点**插入，
    //   所以源码写 `Validate()` 与写 `Validate(true)` 生成的 IL 完全相同：
    //       ldarg.0; ldc.i4.1; callvirt EmporiumEntry::Validate(bool)
    //   取证来源：ilspycmd 反编译 + Il2CppDumper 的 dump.cs + 已编译 DLL 的
    //   IL 三处交叉一致；且 10-02 / 10-06 / 当前 三份 DLL 的 IL **全是 true**。
    //
    // ★ 由此推翻 README 禁止事项 #21 的旧前提：
    //   旧文写「Validate() 无参（= Validate(false)），只做轻量校验」——
    //   前提是错的：无参 == force:true == 强制重建布局。
    //   也就是说 2026-10-05 S8t「把 Validate(true) 改回无参」那次修复
    //   **没有改变任何运行时行为**；把窗口位置回归归因到本参数属误判
    //   （这也解释了为何 10-02 的"好"版本与之后的版本 IL 完全相同）。
    //   旧版 check-forbidden.ps1 的 R1「不许带实参」正是建立在这个错误前提上，
    //   反而**封死了唯一能真正拿到非强制行为的写法**。R1 已随之重写。
    //
    // ★ 现状决策：**保持既有运行时行为（= true）**，只把实参显式化。
    //   理由：① 没有任何证据表明 true 有害（旧证据已被推翻）；
    //        ② 游戏自己的默认值就是 true ⇒ true 是游戏的常规调用方式；
    //        ③ 玩家当前对窗口位置是满意的，不为"修一个不存在的 bug"去冒
    //           改变行为的风险。
    //   ★ 若将来实机确认窗口位置确有问题、且确由本参数引起：
    //     把下面这一行改成 false 即可（这是唯一需要动的地方）。
    //     改完同步更新 _shared/check-forbidden.ps1 的 R1b 期望次数说明。
    // ══════════════════════════════════════════════════════════════════
    public const bool EmporiumForceValidate = true;

    /// <summary>
    /// 从网格自身反推每格像素数。读不到时返回 <see cref="CellPxFallback"/>。
    /// 与 DIY 的 <c>_columnPixels / _rowPixels</c> 同一套算法，保证两个组件算出来一致。
    /// </summary>
    internal static float CellPixelsOf(GameGridInventory g)
    {
        try
        {
            if (g == null) return CellPxFallback;
            int cols = 0;
            try { cols = g.inventoryShape?.width ?? 0; }
            catch (Exception ex) { CatchLogOnce.Warn("sbe.CellPixelsOf.shape", ex); }
            if (cols <= 0) return CellPxFallback;
            return Math.Max(1f, (float)g.widthPixels / (float)Math.Max(1, cols));
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.CellPixelsOf", ex); return CellPxFallback; }
    }
    /// <summary>
    /// SBE 诊断开关的对外只读入口（S8v）。
    /// <para>
    /// ★ 2026-10-07 现状：三个模块的诊断开关已合并为 CI 的一条总开关，
    ///   <see cref="SaveBagExpandConfig.DiagLog"/> 是那条 entry 的别名。
    ///   本属性已**不再被 CI 使用**（CI 直接读 CustomInfrastructureConfig.DiagLog，
    ///   不再需要跨程序集绕路 —— 合并成单 DLL 后本来也读得到）。
    ///   保留它是为了对外语义稳定：外部/调试代码问"本模块诊断开了吗"仍可用它。
    /// </para>
    /// </summary>
    public static bool CfgDiagEnabled
    {
        get { try { return SaveBagExpandConfig.DiagLog?.Value == true; } catch (Exception ex) { CatchLogOnce.Warn("sbe.CfgDiagEnabled", ex); return false; } }
    }
    /// <summary>
    /// SBE 模块的诊断日志。受 [SaveBagExpand] DiagLog 控制。
    /// 用于系统内部状态（容器 resize 流程 / 窗口同步 / 网格修复等）。
    /// ★ 与 TabLog 的区别：TabLog 是 TAB 分页专用；
    ///   Diag 是通用内部诊断。两者共用同一个 cfg 开关。
    /// </summary>
    internal static void Diag(string msg)
    {
        try
        {
            if (SaveBagExpandConfig.DiagLog?.Value == true)
                Log?.Msg(msg);
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.Diag", ex); }
    }
    private static bool _cfgPagingEnabled;
    private static bool _cfgScrollEnabled;

    internal static void RefreshConfigCache()
    {
        try
        {
            _cfgPagingEnabled = SaveBagExpandConfig.SaveBagPagingEnabled.Value;
            _cfgScrollEnabled = SaveBagExpandConfig.ScrollEnabled.Value;
            Enabled = SaveBagExpandConfig.EnableMod.Value;
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.RefreshConfigCache", ex); }
    }

    private static float _lastCfgRefreshAt = -10f;
    private const float CfgRefreshInterval = 1f;

    private static void RefreshConfigCacheThrottled()
    {
        float now = Time.realtimeSinceStartup;
        if (now - _lastCfgRefreshAt < CfgRefreshInterval) return;
        _lastCfgRefreshAt = now;
        RefreshConfigCache();
    }

    // ══════════════════════════════════════════════════════════════════
    // 生命周期（由主 Mod 调用）
    // ══════════════════════════════════════════════════════════════════

    /// <summary>由主 Mod 在 OnInitializeMelon 里调用。</summary>
    public static void Init(MelonLogger.Instance logger)
    {
        Log = logger;

        SaveBagExpandConfig.Init();
        Enabled = SaveBagExpandConfig.EnableMod.Value;
        RefreshConfigCache();

        SaveBagPaging.Initialize(SaveBagExpandConfig.SaveBagPageCount.Value);

        Log?.Msg($"已加载，状态: {(Enabled ? "已启用" : "已禁用")}，" +
                $"分页={(SaveBagExpandConfig.SaveBagPagingEnabled.Value ? "开" : "关")}，" +
                $"扩容={(SaveBagExpandConfig.ExpandEnabled.Value ? "开" : "关")}");

        // 2026-10-07：把 EmporiumEntry.Validate 的实际实参打进启动日志。
        // 本参数长期被误读（旧 README #21 以为无参=false，实为 true），
        // 且"静默失效"类故障最怕看不见 —— 无条件打一行，成功/失败都可见。
        Log?.Msg($"[SaveBagExpand] EmporiumEntry.Validate 实参 = " +
                (EmporiumForceValidate ? "true（强制重建 Emporium 布局）" : "false（仅轻量校验，不动位置）"));
    }

    /// <summary>由主 Mod 在自己的 harmony 实例上调用，挂载 SBE 所有补丁类。</summary>
    /// <remarks>
    /// 【2026-10-06 合并改造】原来是 harmony.PatchAll(程序集)——按程序集扫描。
    ///   4 个 DLL 合并成 1 个之后，"本程序集" 就等于整个套件，会把 DIY / CI 的
    ///   [HarmonyPatch] 类也装到 SaveBagExpand.Patch 这个 Harmony ID 下，
    ///   导致同一目标被两个 ID 各注入一次（Postfix 跑两遍、Patch 计数虚高）。
    ///   所以改成按命名空间 SaveBagExpand 过滤，与合并前的可见范围完全等价。
    /// </remarks>
    public static void ApplyPatches(HarmonyLib.Harmony harmony)
    {
        if (harmony == null) return;
        try
        {
            var asm = typeof(SaveBagExpandCore).Assembly;
            int installed = 0, failed = 0;
            Type[] types;
            try { types = asm.GetTypes(); }
            catch (System.Reflection.ReflectionTypeLoadException rtle)
            {
                var list = new System.Collections.Generic.List<Type>();
                if (rtle.Types != null)
                    foreach (var t in rtle.Types) if (t != null) list.Add(t);
                types = list.ToArray();
            }

            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                if (type == null) continue;
                // 命名空间过滤：只装 SaveBagExpand.*（合并前 PatchAll 看到的就是这些）
                if (type.Namespace == null || !type.Namespace.StartsWith("SaveBagExpand", StringComparison.Ordinal))
                    continue;
                if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;
                try
                {
                    harmony.CreateClassProcessor(type).Patch();
                    installed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    Log?.Error("挂载 SBE 补丁失败 " + type.Name + ": " + ex.Message);
                }
            }
            Log?.Msg($"SBE 补丁安装: 成功 {installed} 个" + (failed > 0 ? $", 失败 {failed} 个" : ""));
            ContainerSuite.Shared.ContainerSuiteLog.Mark("sbe.patch", installed > 0 && failed == 0,
                "installed=" + installed + " failed=" + failed);
        }
        catch (Exception ex)
        {
            Log?.Error("挂载 Harmony 补丁时发生异常: " + ex);
        }
    }

    /// <summary>由主 Mod 在 OnUpdate 里调用。</summary>
    public static void Tick()
    {
        try
        {
            // S8u-3（2026-10-06）：主菜单第一帧预热 Bind 链 JIT。
            // 必须放在门禁判断【之前】——加载期门是关的，而预热必须在
            // 任何读档发生前完成（见 SaveBagPaging.WarmupJit 注释）。
            ContainerSuite.Shared.CrashProbe.Enter("SBE.WarmupJit");
            SaveBagPaging.WarmupJit();
            ContainerSuite.Shared.CrashProbe.Exit("SBE.WarmupJit");

            ContainerSuite.Shared.CrashProbe.Enter("SBE.RefreshConfigCacheThrottled");
            RefreshConfigCacheThrottled();
            ContainerSuite.Shared.CrashProbe.Exit("SBE.RefreshConfigCacheThrottled");
            if (!Enabled) return;

            // S8u 双保险（2026-10-05 补录）：加载期整段跳过 ——
            // 分页绑定 / TAB / 滚轮在 WorldReadyGate 放行前都无意义，
            // 且其中 Bind 是 0x80131506 崩溃链的起点（见禁止事项 #22）。
            if (ContainerSuite.Shared.WorldReadyGate.ShouldDefer) return;

            // 2026-10-12：拖拽中切页后的"物品假体"复原重试（未切过页时首行即返回，
            //   常态零开销）。必须在本块之上——它不依赖分页开关/容器窗口实例。
            ContainerSuite.Shared.CrashProbe.Enter("SBE.TickDragGhostReassert");
            SaveBagPaging.TickDragGhostReassert();
            ContainerSuite.Shared.CrashProbe.Exit("SBE.TickDragGhostReassert");

            ContainerSuite.Shared.CrashProbe.Enter("SBE.EmporiumEntry.Instance");
            var entry = EmporiumEntry.Instance;
            ContainerSuite.Shared.CrashProbe.Exit("SBE.EmporiumEntry.Instance");
            if (entry == null) return;

            // 1) 分页：每秒轮询绑定
            if (_cfgPagingEnabled)
            {
                ContainerSuite.Shared.CrashProbe.Enter("SBE.SaveBagPaging.Tick");
                SaveBagPaging.Tick(entry.invElement);
                ContainerSuite.Shared.CrashProbe.Exit("SBE.SaveBagPaging.Tick");

                bool bound = SaveBagPaging.IsBound;
                if (bound)
                {
                    bool tabDown = false;
                    // 输入模块不可用属无害情形（不影响分页本身），静默忽略即可。
                    try { tabDown = Input.GetKeyDown(KeyCode.Tab); } catch { }
                    if (tabDown) SaveBagPaging.TryCycleNext();
                }
            }

            // 2) 「地面」窗口鼠标拖动（独立于滚动/分页；本地块内的早退不影响它，
            //    因为它在本块**之前**调用）。
            ContainerSuite.Shared.CrashProbe.Enter("SBE.TickWindowDrag");
            TickWindowDrag(entry);
            ContainerSuite.Shared.CrashProbe.Exit("SBE.TickWindowDrag");

            // 3) 其它容器滚动
            if (!_cfgScrollEnabled) return;
            ContainerSuite.Shared.CrashProbe.Enter("SBE.Scroll");
            float scroll = Input.mouseScrollDelta.y;
            if (Mathf.Abs(scroll) < 0.01f) return;

            if (TryScroll(entry.showcaseWindow, entry.showcaseElement, scroll, true, 0, 0, RectScale, RectScale)) return;
            if (TryScroll(entry.backInvWindow, entry.backInvinvElement, scroll, true, 0, 0, RectScale, RectScale)) return;
            if (TryScroll(entry.hiddenWindow, entry.hiddenElement, scroll, false, 0, 0, RectScale, RectScale)) return;
            if (TryScroll(entry.frontInvWindow, entry.frontInvinvElement, scroll, false, 0, 0, RectScale, RectScale)) return;
            if (TryScroll(entry.invWindow, entry.invElement, scroll, true, 0, 0, RectScale, RectScale)) return;
        }
        catch (Exception ex)
        {
            Log?.Error($"OnUpdate 异常: {ex}");
        }
    }

    /// <summary>
    /// 「地面」窗口鼠标拖动（2026-10-09 新增）—— 独立于滚动/分页，
    /// 故放在 Tick 的滚动块之前、单独成段（不绑任何 cfg 的"早退"分支）。
    /// </summary>
    private static void TickWindowDrag(EmporiumEntry entry)
    {
        try { SaveBagExpandWindowDrag.Tick(entry); }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.TickWindowDrag", ex); }
    }

    /// <summary>
    /// 场景切换时清空拖动状态（由主 Mod 的 OnSceneWasLoaded 调用）。
    /// ★ 必须清：新场景的窗口是新对象，旧的基准坐标/可见性状态作废；
    ///   若不清，可能出现"新场景首帧 _wasVisibleLastFrame 残留 = true" 而漏抓基准。
    ///   与 PickupWindowTest 的"每次打开窗口从原版位置重来"同思路。
    /// </summary>
    public static void OnSceneLoadedResetDrag()
    {
        try { SaveBagExpandWindowDrag.Reset(); }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.OnSceneLoadedResetDrag", ex); }
    }

    private static GUIStyle _pageHintStyle;
    private static GUIStyle _mergeLossStyle;

    // ★ 2026-10-09：GetWorldCorners 的 4 元数组缓存复用。原 DrawGUI / IsMouseOver
    //   每次调用都 new 一个托管数组（OnGUI / 输入热路径）。两处均在主线程顺序
    //   调用，且 GetWorldCorners 会整体覆写 4 个元素，故可安全复用同一缓冲。
    private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> _cornersBuffer;

    /// <summary>GetWorldCorners 复用缓冲（懒建）；主线程顺序调用，可安全共用。</summary>
    private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> CornersBuffer
    {
        get
        {
            if (_cornersBuffer == null)
                _cornersBuffer = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
            return _cornersBuffer;
        }
    }

    /// <summary>
    /// B1：存档合并丢失的屏幕红字提示（存档时全页合并超出单网格容量 = 物品没写进档）。
    /// ★ 必须放在 DrawGUI 的所有门槛**之前** —— 分页开关、IsBound、翻页时长
    ///   任何一条都不该把"存档里少了东西"级别的告警挡掉。
    /// </summary>
    private static void DrawMergeLossWarning()
    {
        try
        {
            if (!SaveBagPaging.HasMergeLoss) return;

            const float dur = 15f;
            float elapsed = Time.realtimeSinceStartup - SaveBagPaging.MergeLossAt;
            if (elapsed < 0f || elapsed > dur) return;

            if (_mergeLossStyle == null)
            {
                _mergeLossStyle = new GUIStyle(GUI.skin.label);
                _mergeLossStyle.fontSize = 24;
                _mergeLossStyle.fontStyle = FontStyle.Bold;
                _mergeLossStyle.alignment = TextAnchor.UpperCenter;
                _mergeLossStyle.wordWrap = true;
            }

            float alpha = 1f;
            float fadeStart = dur * 0.6f;
            if (elapsed > fadeStart)
            {
                alpha = 1f - (elapsed - fadeStart) / (dur - fadeStart);
                if (alpha < 0f) alpha = 0f;
            }

            float w = Mathf.Min(Screen.width - MergeLossWarnMarginPx, MergeLossWarnMaxWidthPx);
            var rect = new Rect((Screen.width - w) * 0.5f, 20f, w, 70f);
            var shadow = new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height);

            string txt = $"存档警告：{SaveBagPaging.MergeLossCount} 件物品没写进存档\n" +
                         "分页总件数超出单网格上限，明细见 UserData\\SaveBagPaging.mergeloss.txt";

            _mergeLossStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);
            GUI.Label(shadow, txt, _mergeLossStyle);
            _mergeLossStyle.normal.textColor = new Color(1f, 0.25f, 0.2f, alpha);
            GUI.Label(rect, txt, _mergeLossStyle);
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.DrawMergeLossWarning", ex); }
    }

    /// <summary>由主 Mod 在 OnGUI 里调用。</summary>
    public static void DrawGUI()
    {
        try
        {
            DrawMergeLossWarning();

            if (!Enabled) return;
            if (!_cfgPagingEnabled) return;
            if (SaveBagExpandConfig.SaveBagPageCount.Value <= 1) return;
            if (!SaveBagPaging.IsBound) return;

            float dur = SaveBagExpandConfig.SaveBagPagingHintDuration.Value;
            if (dur <= 0.01f) return;

            float elapsed = Time.realtimeSinceStartup - SaveBagPaging.LastSwitchAt;
            if (elapsed < 0f || elapsed > dur) return;

            var entry = EmporiumEntry.Instance;
            if (entry == null) return;

            var win = entry.invWindow;
            if (win == null) return;
            var rt = win.rectTransform;
            if (rt == null) return;

            var corners = CornersBuffer;
            rt.GetWorldCorners(corners);

            float topScreenY = corners[1].y;
            float leftScreenX = corners[0].x;
            float rightScreenX = corners[2].x;

            float guiTopY = Screen.height - topScreenY;
            float guiCenterX = (leftScreenX + rightScreenX) * 0.5f;

            float w = PageHintWidthPx;
            float h = PageHintHeightPx;
            float x = guiCenterX - w * 0.5f;
            float y = guiTopY - h - 6f;

            if (_pageHintStyle == null)
            {
                _pageHintStyle = new GUIStyle(GUI.skin.label);
                _pageHintStyle.fontSize = 22;
                _pageHintStyle.fontStyle = FontStyle.Bold;
                _pageHintStyle.alignment = TextAnchor.MiddleCenter;
            }

            float alpha = 1f;
            float fadeStart = dur * 0.7f;
            if (elapsed > fadeStart)
            {
                alpha = 1f - (elapsed - fadeStart) / (dur - fadeStart);
                if (alpha < 0f) alpha = 0f;
            }
            alpha *= PageHintAlphaScale;

            string txt = $"第 {SaveBagPaging.CurrentPage + 1} 页 / 共 {SaveBagPaging.PageCount} 页";

            var rect = new Rect(x, y, w, h);
            var rectShadow = new Rect(x + 2f, y + 2f, w, h);

            _pageHintStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.75f);
            GUI.Label(rectShadow, txt, _pageHintStyle);

            _pageHintStyle.normal.textColor = new Color(1f, 0.95f, 0.3f, alpha);
            GUI.Label(rect, txt, _pageHintStyle);
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.DrawGUI", ex); }
    }

    // ══════════════════════════════════════════════════════════════════
    // 私有工具（原样保留）
    // ══════════════════════════════════════════════════════════════════

    private static bool TryScroll(PixelWindow win, GameGridInventory grid, float scroll,
                                  bool isPersistent, int reqCols, int reqRows,
                                  float scaleX, float scaleY)
    {
        if (win == null || grid == null) return false;

        if (!isPersistent)
        {
            try { if (!win.IsVisible()) return false; }
            catch (Exception ex) { CatchLogOnce.Warn("sbe.TryScroll.IsVisible", ex); return false; }
        }

        if (!IsMouseOver(win, grid, reqCols, reqRows, scaleX, scaleY)) return false;

        var inner = win.inner;
        if (inner == null) return false;
        var innerRt = inner.rectTransform;
        if (innerRt == null) return false;

        // S5-3：每格像素从该网格自身反推，不再写死 16
        float cell = CellPixelsOf(grid);

        int rows = 0;
        try { rows = grid.inventoryShape?.height ?? 0; }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.TryScroll.shape", ex); }
        if (rows <= 0) return false;

        int downRows = Math.Clamp(SaveBagExpandConfig.ScrollVisibleRows.Value, 0, 50);
        int maxDownable = Mathf.Max(0, rows - 1);
        if (downRows > maxDownable) downRows = maxDownable;
        float maxDown = downRows * cell;

        int upRows = Math.Clamp(SaveBagExpandConfig.ScrollUpRows.Value, 0, 50);
        float maxUp = upRows * cell;

        var pos = innerRt.anchoredPosition;
        pos.y += scroll * cell;

        if (pos.y > maxUp) pos.y = maxUp;
        if (pos.y < -maxDown) pos.y = -maxDown;

        innerRt.anchoredPosition = pos;
        return true;
    }

    /// <summary>
    /// 鼠标是否落在该容器窗口内（滚轮命中判定）。
    /// <para>
    /// ★ 2026-10-09 重写（原实现恒失效）。原代码取自 <c>win.rectTransform.GetWorldCorners()</c>
    /// —— 但 <c>PixelWindow.rectTransform</c>（handler 自身）是**纯容器**，
    /// <c>rect / sizeDelta</c> 恒为 0×0 → GetWorldCorners 返回四点重合的退化矩形 →
    /// 命中区宽度/高度均为 0 → 恒不命中（与「地面拖动」同一根因，实测四点全等）。
    /// </para>
    /// <para>
    /// 现改为复用 <see cref="SaveBagExpandWindowDrag.TryGetWindowWorldRect"/>：
    /// 取 handler 子节点中面积最大的真实矩形（≈ 窗口主体，实测 12×12 地面窗为 396×212）。
    /// <c>GetWorldCorners</c> 对 ScreenSpaceOverlay 画布返回的是**屏幕坐标**，
    /// 与 <c>Input.mousePosition</c> 同空间，可直接比较。
    /// </para>
    /// <para>
    /// ★ <paramref name="scaleX"/>/<paramref name="scaleY"/> 与 <paramref name="reqCols"/>/<paramref name="reqRows"/>
    /// 保留仅为签名兼容（5 个调用点仍按原样传 RectScale/0）——新算法直接用地板实测矩形，
    /// 不再走 <c>cols×cell×scale</c> 那条会失真的推算链。
    /// </para>
    /// </summary>
    private static bool IsMouseOver(PixelWindow win, GameGridInventory grid, int reqCols, int reqRows,
                                    float scaleX, float scaleY)
    {
        try
        {
            if (win == null) return false;

            if (!SaveBagExpandWindowDrag.TryGetWindowWorldRect(win,
                    out float left, out float right, out float bottom, out float top))
                return false;

            // 退化保护：宽或高为 0 时直接判否（避免把边缘剔除成"永远命中"）。
            if (right - left <= 0.5f || top - bottom <= 0.5f) return false;

            Vector3 m = Input.mousePosition;
            return m.x >= left && m.x <= right
                && m.y >= bottom && m.y <= top;
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.IsMouseOver", ex);
            return false;
        }
    }
}