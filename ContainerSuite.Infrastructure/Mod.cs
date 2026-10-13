using System;
using System.Collections.Generic;
using System.Diagnostics;      // ← 新增：Stopwatch
using ContainerSuite.Shared;
using MelonLoader;

namespace CustomInfrastructure;

public sealed class Mod : MelonMod
{
    internal static MelonLogger.Instance Log { get; private set; }
    // ═══════════════════════════════════════════════════════════════════
    // TD-09（2026-10-05）：OnUpdate 三个 Tick 的耗时基线。
    //
    // 只在 ContainerSuiteLog.DiagEnabled=true（cfg [DIYUpgrade] DiagLog）时计时，
    // 关闭时每帧只多 3 次 bool 判断，零额外开销。
    //
    // 为什么用 Stopwatch 而不是 Time.realtimeSinceStartup：
    //   realtimeSinceStartup 是 float，亚毫秒的 Tick 会因精度不足全读成 0。
    //   Stopwatch 走 QPC，单次 Restart/Stop 约 20~50ns，6 次/帧可忽略。
    //
    // 为什么节流到 300 帧（≈ 5 秒 @ 60fps）才报一次：
    //   每帧都打会污染 Latest.log，且单帧抖动太大不好看。取 5 秒窗口内的
    //   均值和峰值，既平滑又不丢尖刺信号。
    // ═══════════════════════════════════════════════════════════════════
    private static readonly Stopwatch _swArbiter = new Stopwatch();
    private static readonly Stopwatch _swSbe = new Stopwatch();
    private static readonly Stopwatch _swDiy = new Stopwatch();

    private static long _nsArbiter, _nsSbe, _nsDiy;       // 本窗口累计纳秒
    private static long _maxArbiter, _maxSbe, _maxDiy;    // 本窗口单帧峰值纳秒
    private static int _perfFrames;
    private const int PerfReportFrames = 300;           // ≈ 5 秒

    // 崩点探针武装标志（见 OnUpdate 头注释）。读档时置位，窗口过后自动解除。
    // P1-3：CrashProbe 的实现在 Release 下被条件编译删除，但 [Conditional] 调用仍需
    //   绑定其实参，故该字段在 Release 里也保留（internal，无未使用告警）。
    internal static bool _crashProbeArmed;
    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        // 统一显示套件版本 + 一行组件版本（组件不再各报一个版本号）
        LoggerInstance.Msg(SuiteVersion.Banner);

        try
        {
            // ─── CI 自身初始化 ───
            CustomInfrastructureConfig.Init();
            // ★ 2026-10-09 修正（顺序 bug）：诊断开关的 provider 必须在这里就注入。
            //   背景：ContainerSuiteLog 从"无条件输出"改成了"受 DiagLog 闸门控制"，而
            //   DiagEnabled 在 provider 未注入时退回静态值 false —— 若把注入点留在
            //   原来的位置（DIY Init 之后，约本方法第 100 行），那么它之前的
            //   ContainerSuiteLog.Mark("ci.containers") / ContainerSuiteLog.Mark("sbe.init") 会读到 false
            //   而**被静默丢弃** ⇒ 回归的 reg.ci.containers / reg.sbe.init 断言
            //   即使 DiagLog=true 也拿不到标记，误判为"功能没执行"。
            //   cfg 项此刻已由上面的 Init 建好，provider 是懒执行，随时可注入。
            ContainerSuiteLog.SetDiagProvider(() =>
                CustomInfrastructureConfig.DiagLog?.Value ?? false);
            // S3-5b：初始化前先 Invalidate WorldReadyGate，防止上一个场景的状态残留
            ContainerSuite.Shared.WorldReadyGate.Invalidate("CI.Init");

            var harmony = new HarmonyLib.Harmony("CustomInfrastructure.Main");
            ContainerExpandPatch.Apply(harmony);

            CustomInfrastructureConfig.OnConfigChanged += OnConfigChanged;

            var active = CustomInfrastructureConfig.GetActiveContainers();
            var parts = new List<string>();
            foreach (var c in active) parts.Add($"{c.Id}={c.Width}x{c.Height}");
            string summary = parts.Count > 0 ? string.Join(", ", parts) : "(无)";
            Log.Msg($"已加载。生效容器：{summary}");

            ReportPatchSummary(harmony);
            ContainerSuite.Shared.ContainerSuiteLog.Mark("ci.containers", active.Count > 0, "count=" + active.Count);

            // ─── S3-4：初始化 SBE 模块 ───
            try
            {
                SaveBagExpand.SaveBagExpandCore.Init(LoggerInstance);

                var sbeHarmony = new HarmonyLib.Harmony("SaveBagExpand.Patch");
                SaveBagExpand.SaveBagExpandCore.ApplyPatches(sbeHarmony);
                ContainerSuite.Shared.ContainerSuiteLog.Mark("sbe.init", true, null);
            }
            catch (Exception ex)
            {
                Log.Error("初始化 SBE 模块失败：" + ex);
            }

            // ─── S4：初始化 DIY 模块 ───
            try
            {
                DIYUpgrade.DiyUpgradeMod.Init(LoggerInstance);
                // TD-05d（2026-10-04）：把 Shared 层的诊断开关接到 DIY 的 cfg DiagLog 上。
                // 背景：ContainerSuiteLog.DiagEnabled 这个属性从创建起就从未被赋值过，
                //   所以 RestoreArbiter.cs 里那段「本轮处理 N 个容器，耗时 M 帧」是死代码，
                //   永远不执行。排障"读档慢"时那段日志正好最有用。
                // 放在 Init 之后：Init 里会读 cfg 并把 _diagLog 填好，IsDiagEnabled 才能取到真实值。
                // 说明：这只是"启动时同步一次"——用户热改 cfg 需要重启游戏才生效，
                //   与 ModManager 里其它 cfg 项的行为一致。
                // S8v：Shared 层的 Diag 开关跟随模块的 DiagLog。
                // S8v+：注入实时判据 —— ModManager 面板改 DiagLog 立即生效，
                //   不再需要重启游戏。判据本身每次读时执行，代价 = 1 次 .Value getter。
                //   ★ 必须放在 DIY Init 之后 —— 那时 cfg 项才建好；
                //     但 Provider 是懒执行，即使现在 cfg 项未建好也不影响，
                //     后续 ModManager 修改后下一次读就能拿到新值。
                // ★ 2026-10-09：provider 的注入点已上移到本方法开头（紧跟
                //   CustomInfrastructureConfig.Init）—— ContainerSuiteLog 从第 67 行起就依赖它，
                //   在这里才注入会让前面那些 [REG] 标记被静默丢弃。
                ContainerSuite.Shared.ContainerSuiteLog.Mark("diy.init", true, null);
            }
            catch (Exception ex)
            {
                Log.Error("初始化 DIY 模块失败：" + ex);
            }
            ContainerSuite.Shared.ContainerSuiteLog.Mark("suite.ready", true, "version=" + SuiteVersion.Suite);
        }
        catch (Exception ex)
        {
            Log.Error($"初始化失败: {ex}");
        }
    }

    public override void OnUpdate()
    {
        // ═══════════════════════════════════════════════════════════════════
        // 【2026-10-10 读档闪退 · 崩点探针】
        //
        // 背景：读档后约 2 秒必崩（Fatal error 0x80131506，CLR fatal，托管 catch
        //   抓不到、MelonLoader 日志戛然而止 —— 崩溃前最后一行永远是
        //   ProbablyOptimized 的「帧末视觉 记录 450 个」，那是"UI 全量销毁/重建"
        //   那一帧，是**现象**不是原因）。
        //   已排除：清扫手柄（A3 关掉仍崩）、预热时机（门禁+消抖生效仍崩）。
        //
        // 做法：崩点只可能在本方法的三个 Tick 里。用**独立心跳文件**逐 Tick 记录
        //   进入/退出 —— 不走 MelonLogger（CLR fatal 时它可能已失效），改为直接
        //   写一个极小文本文件（只写当前步骤名，O(1) 覆盖写，不追历史）。
        //   崩溃后读该文件即知"最后进入但未退出"的那一步 = 凶手。
        //   ★ 只在读档后 CrashProbeWindowSeconds 秒内记录（避免全程刷盘）；
        //     平时零开销（一个 bool 判断）。
        // ═══════════════════════════════════════════════════════════════════
        CrashProbe.Enter("FrameStart");
        CrashProbe.TickFrame(ref _crashProbeArmed);
        CrashProbe.Exit("FrameStart");

        // ═══════════════════════════════════════════════════════════════════
        // 【2026-10-10 P1】CI 容器扩展的**唯一安全执行点**。
        //
        // ValidateShapeStatePostfix 只把物品放进队列（跑在第三方 mod 的 detour 栈上，
        // 必须极小）；真正的 TryExpand 在这里跑 —— 本帧栈顶是 Unity 的 Update，
        // 没有任何第三方 detour trampoline，冷方法首次 JIT 不会触发 CLR fatal。
        //
        // 顺序：放在 Arbiter 之前 —— 先扩尺寸、再让 DIY 恢复链基于最终尺寸做恢复，
        //   与历史顺序（Postfix 同步执行 → 恢复链随后）一致。
        // ═══════════════════════════════════════════════════════════════════
        CrashProbe.Enter("CIExpandQueue");
        try { ContainerExpander.ProcessExpandQueue(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnUpdate.CIExpandQueue", ex); }
        CrashProbe.Exit("CIExpandQueue");

        // TD-09：只在 DiagLog 开启时计时，关闭时零开销
        bool perf = ContainerSuiteLog.DiagEnabled;

        CrashProbe.Enter("Arbiter");
        if (perf) _swArbiter.Restart();
        try { ContainerSuite.Shared.RestoreArbiter.Tick(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnUpdate.Arbiter", ex); }
        if (perf) _swArbiter.Stop();
        CrashProbe.Exit("Arbiter");

        CrashProbe.Enter("SBE");
        if (perf) _swSbe.Restart();
        try { SaveBagExpand.SaveBagExpandCore.Tick(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnUpdate.SBE", ex); }
        if (perf) _swSbe.Stop();
        CrashProbe.Exit("SBE");

        CrashProbe.Enter("DIY");
        if (perf) _swDiy.Restart();
        try { DIYUpgrade.DiyUpgradeMod.Tick(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnUpdate.DIY", ex); }
        if (perf) _swDiy.Stop();
        CrashProbe.Exit("DIY");

        CrashProbe.Enter("Perf");
        if (perf) AccumulateTickPerf();
        CrashProbe.Exit("Perf");

        // 帧结束标记：本行之后再无本 mod 代码。若探针停在 `-> ONUPDATE-END`
        // 而下一帧的 `-> FrameStart` 从未出现 ⇒ 崩溃在 OnUpdate 之外（游戏帧逻辑）。
        CrashProbe.Enter("ONUPDATE-END");
    }
    public override void OnSceneWasLoaded(int index, string sceneName)
    {
        // S4：DIY 的 OnSceneWasLoaded 里做缓存清理 + 门禁重置
        try { DIYUpgrade.DiyUpgradeMod.OnSceneLoaded(index, sceneName); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnSceneWasLoaded.DIY", ex); }

        // 2026-10-09：SBE 的「地面」窗口拖动状态在场景切换时清空
        //   （新场景窗口是新对象，旧基准/可见性状态作废）。
        try { SaveBagExpand.SaveBagExpandCore.OnSceneLoadedResetDrag(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnSceneWasLoaded.SBEDrag", ex); }

        // 2026-10-10 P1：CI 延迟扩展队列里跨帧持有的 GameItem 指针在场景切换后全部失效
        //   （与 RestoreArbiter.ClearAll 同理）。不清理 ⇒ 存活校验会误判地址复用后的新对象。
        try { ContainerExpander.ClearExpandQueue(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnSceneWasLoaded.CIExpandQueue", ex); }

        // 2026-10-07（审计）：场景切换时清空「只打印一次」去重集合。
        //   理由：全局去重会让"场景 A 里的偶发异常"把"场景 B 里真正的同类异常"
        //   一起吞掉（同一 where 只打第一次）—— 恰恰是"静默"最典型的成因。
        //   按场景重置后，每个场景都有一次重新留痕的机会；场景切换本身低频，
        //   不会造成刷屏。（CatchLogOnce 的类注释本就写明"场景切换 / 测试可用"）
        try { CatchLogOnce.Clear(); } catch { /* 清空去重集合不会失败，忽略 */ }
    }
    public override void OnGUI()
    {
        try { SaveBagExpand.SaveBagExpandCore.DrawGUI(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnGUI.SBE", ex); }
        // D-2（2026-10-12）：组拖进度提示。放在 SBE 之后：两者屏幕区域不重叠
        // （SBE 的提示在 y=20，组拖进度在 y=90），互不影响。
        // ★ 刻意不走 SBE 的 DrawGUI —— 分页开关关闭时那条路径会被门槛挡掉，
        //   而"批量升级进行中"与分页开关毫无关系。
        try { DIYUpgrade.DiyUpgradeMod.DrawGroupDragProgress(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnGUI.DIYGroupDrag", ex); }
    }

    public override void OnApplicationQuit()
    {
        // S8v+：把 MelonPreferences 的当前内存值统一写盘一次。
        //   原因：ModManager（Nico's Lab）修改 DiagLog 等配置时，
        //   Entry.Value 是内存值（我们的 provider 立即生效，所以热改有效），
        //   但 ModManager 自身不一定调 MelonPreferences.Save() 写盘 ——
        //   结果重启后配置丢失。
        //   退出时主动 Save 一次，保证内存里的最新值持久化。
        //   注：Save() 会写所有 category（含其它 mod 的），但都基于各自当前
        //      内存值，与其它 mod 各自保存的效果一致，不会互相破坏。
        try { MelonPreferences.Save(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnApplicationQuit.SavePrefs", ex); }

        // S8u-4：页 ID 旁路文件兜底（正常路径在 SaveIdSets 已同步写，这里只补写失败重试）。
        try { SaveBagExpand.SaveBagPaging.FlushIdsOnQuit(); }
        catch (Exception ex) { CatchLogOnce.Warn("Mod.OnApplicationQuit.FlushIds", ex); }

        try
        {
            DIYUpgrade.DiyUpgradeMod.Shutdown();
            CustomInfrastructureConfig.OnConfigChanged -= OnConfigChanged;
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("Mod.OnApplicationQuit", ex);
        }
    }
    /// <summary>
    /// TD-09：累计本帧 3 个 Tick 的耗时，满 300 帧打一次报告。
    /// 报告只在 DiagEnabled=true 时输出（由 ContainerSuiteLog.Diag 内部判断）。
    /// </summary>
    private static void AccumulateTickPerf()
    {
        long arbNs = TicksToNs(_swArbiter.ElapsedTicks);
        long sbeNs = TicksToNs(_swSbe.ElapsedTicks);
        long diyNs = TicksToNs(_swDiy.ElapsedTicks);

        _nsArbiter += arbNs; _nsSbe += sbeNs; _nsDiy += diyNs;
        if (arbNs > _maxArbiter) _maxArbiter = arbNs;
        if (sbeNs > _maxSbe) _maxSbe = sbeNs;
        if (diyNs > _maxDiy) _maxDiy = diyNs;

        if (++_perfFrames < PerfReportFrames) return;

        double avgArb = _nsArbiter / (double)_perfFrames / 1000.0;   // → μs
        double avgSbe = _nsSbe / (double)_perfFrames / 1000.0;
        double avgDiy = _nsDiy / (double)_perfFrames / 1000.0;
        double avgSum = avgArb + avgSbe + avgDiy;
        double maxArb = _maxArbiter / 1000.0;
        double maxSbe = _maxSbe / 1000.0;
        double maxDiy = _maxDiy / 1000.0;

        // 16670μs = 60fps 一帧的预算
        ContainerSuiteLog.Diag(
            $"[Perf] 最近 {_perfFrames} 帧均值：" +
            $"Arbiter={avgArb:F1}μs  SBE={avgSbe:F1}μs  DIY={avgDiy:F1}μs  " +
            $"合计={avgSum:F1}μs（占 60fps 帧预算 {avgSum / 16670.0 * 100.0:F2}%）  " +
            $"峰值：Arb={maxArb:F0} SBE={maxSbe:F0} DIY={maxDiy:F0}μs");

        _nsArbiter = _nsSbe = _nsDiy = 0;
        _maxArbiter = _maxSbe = _maxDiy = 0;
        _perfFrames = 0;
    }

    /// <summary>Stopwatch tick 数 → 纳秒。整数运算，避免 double 精度损失。</summary>
    private static long TicksToNs(long ticks)
        => ticks * 1_000_000_000L / Stopwatch.Frequency;
    private static void OnConfigChanged()
    {
        // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 配置变更属状态汇报，不是加载/告警。
        Diag("配置已更新。新尺寸将在容器下次打开 / 下次校验形状时生效。");
    }

    private static int ReportPatchSummary(HarmonyLib.Harmony harmony)
    {
        try
        {
            int count = 0;
            foreach (var _ in harmony.GetPatchedMethods()) count++;
            Log.Msg($"Patch 汇总：{count} 个方法已挂钩。");
            // 2026-10-07：阈值 4 → 2 —— CI 原有 4 个挂点，其中 PixelWindow.Show 的
            // 两个重载已摘除（与 DIY 的 WindowShowHook 重复），只剩
            // GameItem.ValidateShapeState + PlayerStore.LoadGame。
            ContainerSuite.Shared.ContainerSuiteLog.Mark("ci.patch", count >= 2, "count=" + count);
            return count;
        }
        catch (Exception ex)
        {
            Log.Warning("汇总 patch 结果失败：" + ex.Message);
            ContainerSuite.Shared.ContainerSuiteLog.Mark("ci.patch", false, ex.Message);
            return -1;
        }
    }
    /// <summary>
    /// CI 模块的诊断日志。受 [CustomInfrastructure] DiagLog 控制。
    /// 用于系统内部状态（容器扩展流程等），玩家正常玩不需要看。
    /// </summary>
    internal static void Diag(string msg)
    {
        try
        {
            if (CustomInfrastructureConfig.DiagLog?.Value == true)
                Log?.Msg(msg);
        }
        catch { }
    }
}