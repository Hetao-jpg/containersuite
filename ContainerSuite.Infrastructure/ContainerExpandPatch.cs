using System;
using System.Collections;
using ContainerSuite.Shared;   // S2 新增
using HarmonyLib;
using Il2Cpp;
using MelonLoader;

namespace CustomInfrastructure;

internal static class ContainerExpandPatch
{
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-09-28 崩溃修复】读档期禁用 ValidateShapeState Postfix
    //
    // 崩溃现象：加载旧存档 → Internal CLR error (0x80131506) 直接闪退。
    // 崩溃栈：PlayerStore.LoadGame → DecodeSaveItem → GameInventory.Expel
    //         → GameItem.ValidateShapeState → 本类 Postfix → TryExpand
    //
    // 根因：ValidateShapeState 在读档时会被原版对**每个物品**调用一次，
    // 此时调用栈已经很深（DecodeSaveItem 之下），而 TryExpand 会走
    // CollectGrids → Walk 递归 + SetShape 等**需要 JIT 现场编译**的路径。
    // Harmony detour 还在栈上时去 JIT 编译新方法，.NET 运行时的
    // CompileMethodHook 会直接抛内部错误 —— 这是 CLR 级 fatal，
    // 托管 try/catch 抓不到，MelonLoader 日志也留不下任何堆栈。
    //
    // 【2026-10-07 更正】原注释称「DIYUpgrade 也 Postfix 了同一个方法
    // （ReapplyFromMeta → TryRestoreRememberedGridShape），两个 Postfix 叠加」
    // —— 该说法在**合并后的当前代码里已不成立**：DIY 的 16 个补丁里没有
    // ValidateShapeState（它的恢复走 Show / LoadGame → RestoreArbiter 那条链）。
    // 更正留痕，避免后人按旧注释去找一个不存在的"第二个 Postfix"。
    //
    // 修复：读档期间（LoadGamePostfix 置位 → 延迟 30 帧）一律跳过。
    // 恢复动作本来就由 LoadGamePostfix 的延迟协程承担（第 3 重保险），
    // 所以跳过后功能不减 —— 只是把重活儿从「读档深栈」搬到「帧末」。
    // ═══════════════════════════════════════════════════════════════════
    // S3-5b：本地闸门删除，改用 Shared 的 WorldReadyGate。
    // 保持 volatile 语义不变 —— WorldReadyGate.ShouldDefer 内部是 thread-safe 的。

    internal static void Apply(HarmonyLib.Harmony harmony)
    {
        // Hook 1: GameItem.ValidateShapeState —— 覆盖「容器创建 / 形状校验 / 存档恢复」
        var validate = AccessTools.Method(typeof(GameItem), "ValidateShapeState");
        if (validate != null)
        {
            harmony.Patch(validate, postfix: new HarmonyMethod(
                typeof(ContainerExpandPatch), nameof(ValidateShapeStatePostfix)));
            // 【2026-10-10 方案 A】手动注册 → 登记进预热表（见 WarmupRegistry 注释）。
            ContainerSuite.Shared.WarmupRegistry.Register(
                typeof(ContainerExpandPatch).GetMethod(nameof(ValidateShapeStatePostfix)));
            // 【2026-10-10 P1】★ 入队方法跑在第三方 mod 的 detour 栈上，
            //   它**自己**必须是已编译的 —— 否则「入队」这步本身就是一次冷 JIT，
            //   治本变徒劳。故连同出队方法一起在启动期浅栈预热。
            //   EnqueueExpand(null) / ProcessExpandQueue() 遇空即早退，无副作用。
            ContainerSuite.Shared.WarmupRegistry.Register(
                typeof(ContainerExpander).GetMethod(nameof(ContainerExpander.EnqueueExpand)));
            ContainerSuite.Shared.WarmupRegistry.Register(
                typeof(ContainerExpander).GetMethod(nameof(ContainerExpander.ProcessExpandQueue)));
            Mod.Log.Msg($"Patched {validate} (ValidateShapeState)");
        }
        else Mod.Log.Warning("GameItem.ValidateShapeState not found");

        // ═════════════════════════════════════════════════════════════════
        // 【2026-10-07 去重】原 Hook 2 / Hook 3（PixelWindow.Show 的两个重载）
        //   已**移除** —— 它们与 DIY 的 WindowShowHook 撞成一对 Postfix：
        //   WindowShowHook 会遍历 PixelWindow 的**所有** Show 重载各挂一次，
        //   所以这两个重载本来就有 2 个 Postfix、每次显示起 2 条协程。
        //   「展示即扩展」的动作已并入 WindowShowHook.DelayedRestore 的**同一条协程**，
        //   顺序（先扩展、再请求恢复）与历史一致。
        //   ⇒ CI 的补丁数由 4 → 2（Mod.ReportPatchSummary 的阈值已同步）。
        //   ★ 为什么并到 DIY 那侧而不是留在这里：WindowShowHook 按**方法名**遍历
        //     所有重载，比这里按显式签名查找更耐游戏更新；留这边反而更脆。
        // ═════════════════════════════════════════════════════════════════

        // Hook 4: PlayerStore.LoadGame —— 读档后主动扫一遍
        var loadGame = AccessTools.Method(typeof(PlayerStore), "LoadGame");
        if (loadGame != null)
        {
            harmony.Patch(loadGame, postfix: new HarmonyMethod(
                typeof(ContainerExpandPatch), nameof(LoadGamePostfix)));
            // ★ 不登记 LoadGamePostfix（有副作用，见其方法头注释）—— 与 LoadGameRestorePatch 同理。
            Mod.Log.Msg($"Patched {loadGame} (LoadGame)");
        }
        else Mod.Log.Warning("PlayerStore.LoadGame not found");
    }

    /// <summary>
    /// 【2026-10-10 P1】本方法**只入队，不干活**。
    /// <para>
    /// 原实现直接 <c>TryExpand(__instance)</c>：那是把冷方法链
    /// （CollectGrids / Walk / SetShape / ForceRecomputeDynamicBackground / RefreshWindow，
    /// 全部未预热）的首次 JIT 放在了 **GameItem.ValidateShapeState 的 detour 栈**上
    /// —— 而该方法必然被 XIAOWO / ProbablyOptimized 等外部 mod 挂载（超热），
    /// 栈上已有 trampoline ⇒ 命中「嵌套 JIT × trampoline」交集 ⇒ CLR fatal 0x80131506。
    /// </para>
    /// <para>
    /// 改为入队后，真正的 TryExpand 在 <c>Mod.OnUpdate</c> 的顶层帧执行，
    /// 栈上无第三方 trampoline，冷方法首次 JIT 亦安全。
    /// ★ 延迟量 = 最多 1 帧（队列通常当帧排空），与 RestoreArbiter / DIY 恢复协程
    ///   的既有延迟语义一致，不引入新的时序依赖。
    /// </para>
    /// </summary>
    public static void ValidateShapeStatePostfix(GameItem __instance)
    {
        if (ContainerSuite.Shared.WorldReadyGate.ShouldDefer) return;
        try { ContainerExpander.EnqueueExpand(__instance); }
        catch (Exception ex) { CatchLogOnce.Warn("ContainerExpandPatch.ValidateShapeStatePostfix", ex); }
    }

    // 2026-10-07：原 ShowPostfix / DelayedExpandFromWindow 已**删除** ——
    //   它们只服务于被移除的 PixelWindow.Show 挂点。
    //   "展示即扩展"的动作已并入 DIY 的 WindowShowHook.DelayedRestore（同一条协程）。
    //   ★ 删而不是留注释：常驻却没人调的方法，以后会被误认为"还在生效"
    //     （与"只占几行但会误导"的死代码同源，必须清掉）。

    // T1（2026-10-12）：节流改单调钟 —— 原 DateTime 差值在系统时钟回拨时会让读档流程
    //   被永久忽略（"距上次不到 1.5 秒"恒真）。见 Shared/PollThrottle.cs。
    private static PollThrottle _loadGate;

    // 【2026-10-10 方案 A】★ 本方法**刻意不登记**进 WarmupRegistry：
    //   它的执行流会 Invalidate 世界门禁 + 起协程 + 调 SBE.AfterLoad（有真实副作用），
    //   哑参预热会把副作用提前到启动期 —— 与 LoadGameRestorePatch 同一性质，必须跳过。
    //   它的首次 JIT 时机 = PlayerStore.LoadGame（读档），此刻启动期浅栈预热早已完成；
    //   且读档路径本就由 WorldReadyGate 深栈保护兜底。与历史处理一致。
    public static void LoadGamePostfix()
    {
        if (!_loadGate.Due(1500)) return;

        // S3-5b：读档 = 世界重置，先关闸门
        ContainerSuite.Shared.WorldReadyGate.Invalidate("CI.LoadGame");

        // 【2026-10-10 崩点探针】读档开始 → 武装探针窗口（15s）。
        //   读档后约 2 秒是已知崩溃窗口；这 15 秒内 Mod.OnUpdate 的每个子 Tick
        //   都会把"当前进入的步骤"写进 ContainerSuite.crashprobe.txt。
        //   崩溃后读该文件 = 最后进入未退出的那一步 = 凶手。
        try { ContainerSuite.Shared.CrashProbe.Arm("LoadGame"); } catch { }

        try { MelonCoroutines.Start(DelayedRestoreFromLoad()); }
        catch (System.Exception ex)
        {
            CatchLogOnce.Warn("ContainerExpandPatch.LoadGamePostfix", ex);
            // 协程没跑起来 → finally 不会执行 → 手动开门，否则世界永久 defer
            ContainerSuite.Shared.WorldReadyGate.MarkReady("CI.CoroutineStartFailed");
        }

        // S3-5a：SBE 的分页 AfterLoad 也在这个 Postfix 里调度，
        // 避免两个 Postfix 抢 PlayerStore.LoadGame 执行顺序。
        try
        {
            SaveBagExpand.SaveBagPaging.AfterLoad();
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("ContainerExpandPatch.LoadGamePostfix.SBE", ex);
        }
    }

    private static System.Collections.IEnumerator DelayedRestoreFromLoad()
    {
        // A2b（2026-10-07）：等待从"30 帧"改为"墙钟 0.5s"（0.5s = 30 帧 @60fps）。
        //   原 for (i<30) 在 20fps 机器上 = 1.5s、60fps 上 = 0.5s，行为随帧率漂移。
        //   这里挡的是「离开 LoadGame 深栈 + 给 JIT 预热」，不是"等世界就绪"
        //   （开门由本协程 finally 里的 MarkReady 负责）—— 离开深栈只需 1 帧，
        //   0.5s 的余量在低帧率下也仍有约 10 帧。
        //   ★ 兜底：若容器此刻还没被创建，本次会跳过（见下方 item==null 分支），
        //     但 WindowShowHook 在窗口每次 Show 时会再扩一次，功能不会丢。
        const float CiRestoreWarmupSeconds = 0.5f;
        float ciWarmupStart = UnityEngine.Time.realtimeSinceStartup;
        while (UnityEngine.Time.realtimeSinceStartup - ciWarmupStart < CiRestoreWarmupSeconds)
            yield return null;

        int ciExpanded = 0;
        int ciMissed = 0;
        try
        {
            foreach (var cfg in CustomInfrastructureConfig.GetActiveContainers())
            {
                try
                {
                    var item = DirectoryMaster.Item(cfg.Id, true);
                    if (item != null) { ContainerExpander.TryExpand(item); ciExpanded++; }
                    else ciMissed++;
                }
                catch (Exception ex)
                {
                    Mod.Log.Warning($"LoadGame 恢复 {cfg.Id}: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            Mod.Log.Warning("LoadGame 恢复: " + ex.Message);
        }
        finally
        {
            // S3-5b：改用 Shared 的 WorldReadyGate。
            // 走完延迟恢复 → 世界就绪。
            // S8u-2 注：这是 CI 自身恢复时长，不是「读档结束」——
            // SBE 的 Bind 另有 BindSettleSeconds 稳定期，等 DIY 的恢复协程
            // MarkReady 后再起算（见 2026-10-06 取证）。
            // A2 注（2026-10-07）：CI 由「30 帧」、DIY 由「90 帧」均改为墙钟。
            // B2 注（2026-10-07）：漏项（item==null）会让本次恢复静默失效 ——
            //   用 [REG] 显式断言，missed>0 即 FAIL，不再靠人翻日志。
            ContainerSuite.Shared.RegMark.Mark("ci.restore",
                ciMissed == 0, "expanded=" + ciExpanded + " missed=" + ciMissed);
            ContainerSuite.Shared.WorldReadyGate.MarkReady("CI.RestoreDone");
        }
    }
}