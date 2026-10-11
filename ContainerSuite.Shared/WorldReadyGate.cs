// S2 阶段创建。世界就绪门禁，从 DIYUpgrade 搬并扩展。
// 设计文档：ContainerSuite S2 §3.7
//
// 与 DIYUpgrade 原版的差异：
//   · 命名空间统一为 ContainerSuite.Shared
//   · 日志走 ContainerSuiteLog，不再直接调 MelonLogger
//   · CatchLogOnce 走 Shared 版本
//
// S8e（2026-10-02）：EnqueueRestore / RegisterOnReady 因零调用方被删，
// 随带清理 _pendingRestore / _pendingRestoreSeen / _onReadyCallbacks 三个
// 永远为空的字段 + FlushPending / DoRestore 两个永远不执行的方法。
// 当前 MarkReady：置 _ready = true + 记录 LastReadyAt 时间戳（S8u-2）。

using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace ContainerSuite.Shared;

/// <summary>
/// 世界就绪门禁。
/// 
/// 加载期 / 读档期所有 Hook 通过 ShouldDefer 放行给原版和其它 mod；
/// 读档恢复协程结束时调 MarkReady() 打开门禁，之后 ShouldDefer 返回 false。
/// 
/// 生命周期：
///   · OnSceneWasLoaded  → Invalidate()
///   · PlayerStore.LoadGame Postfix → Invalidate()
///   · 读档恢复协程最后一帧 → MarkReady()
/// </summary>
public static class WorldReadyGate
{
    private static bool _ready;
    private static int _readyFrame = -1;
    private static int _slotAtReady = -1;

    // ═══════════════════════════════════════════════════════════════════
    // S8x（2026-10-05）：新开存档兜底。
    //
    // 背景：新开存档不触发 PlayerStore.LoadGame，MarkReady 永远不会被调，
    //   WorldReadyGate 卡在未就绪 → DIY 所有升级入口 patch 因为 ShouldDefer
    //   而放行给原版 → 玩家按住 LeftAlt 拖金属锭完全没反应、日志一条都没有。
    //
    // 兜底：Invalidate() 之后如果超过 ForceReadyAfterSeconds 秒还没被
    //   MarkReady，且游戏明显已就绪（PlayerStore 存在 + 能读到存档槽），
    //   就主动 MarkReady。
    //
    // 为什么用时间窗口而不是立即：读档流程需要约 1.5 秒才能跑完，
    //   如果提前强制就绪会破坏读档期的避让链。
    //   （A2 2026-10-07：DIY 恢复协程的等待已从「90 帧」改为「墙钟 1.5s」，
    //     与这里的 1.5s 口径一致 —— 此前 90 帧在低帧率机器上会超过 1.5s。）
    // ═══════════════════════════════════════════════════════════════════
    private const float ForceReadyAfterSeconds = 8f;
    private static float _invalidatedAt = -1f;

    /// <summary>门禁是否已打开。</summary>
    public static bool IsReady => _ready;

    /// <summary>门禁打开时的帧号（-1 表示未打开）。</summary>
    public static int ReadyFrame => _readyFrame;

    // ═══════════════════════════════════════════════════════════════════
    // S8u-2（2026-10-06）：开门时间戳 + 稳定期判据。
    //
    // 取证（2026-10-06 Latest.log 时间线）：
    //   03.717 LoadGame → 双 Invalidate（关门）
    //   04.983 CI 30 帧协程跑完 → MarkReady（**开门**）
    //   05.936 SBE 轮询到 → Bind 放行 → ClearGrid/Expel 首次 JIT
    //           → MonoMod CompileMethodHook → 0x80131506
    //   —— 而 DIY 的 90 帧恢复协程此时**还没跑完**（日志无
    //      「读档恢复完成」），模组注册风暴也未结束。
    //
    // 结论：门开得太早（CI 的 30 帧只是它自己的恢复时长，不代表世界就绪）。
    // SBE 分页 Bind 改为「开门 + 距【最晚一次】开门稳定 N 秒」双条件，
    // 由 SaveBagPaging.BindSettleSeconds 执行；本类只负责记录时间戳。
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>最近一次开门的实时时间（realtimeSinceStartup）；-1 = 本会话还没开过门。</summary>
    public static float LastReadyAt { get; private set; } = -1f;

    /// <summary>
    /// 距最近一次开门经过的秒数；本会话还没开过门时返回 float.MaxValue。
    /// 每次 MarkReady 都会刷新 —— 所以它总是从「最晚」那次开门起算。
    /// </summary>
    public static float SecondsSinceLastReady
    {
        get
        {
            if (LastReadyAt < 0f) return float.MaxValue;
            try { return Time.realtimeSinceStartup - LastReadyAt; }
            catch { return float.MaxValue; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    // S9（2026-10-07）：本轮【第一次】开门的时间戳。
    //
    // 为什么要和 LastReadyAt 分开：
    //   SecondsSinceLastReady 取「最晚一次」MarkReady —— 这是 DIY 恢复协程
    //   跑完的时刻，也就是**最慢的那个 mod** 决定的。任何新装的长恢复 mod
    //   都会把依赖它的判据一起拖长（症状：明明世界早就能用了，功能却还在等）。
    //   SecondsSinceFirstReady 只认本轮（自上次 Invalidate）第一次放行，
    //   时间上界因此只由"最快开门者"决定，不再被别人的恢复时长绑架。
    //
    // 用途：SaveBagPaging 的绑定消抖 —— 见 SaveBagPaging.BindSettleSeconds。
    //   旧行为用 LastReadyAt + 3s，实测让分页绑定一直拖到读档后 ~4.0s。
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>本轮第一次开门的实时时间；-1 = 本轮（自上次 Invalidate）还没开过门。</summary>
    public static float FirstReadyAt { get; private set; } = -1f;

    /// <summary>
    /// 距本轮【第一次】开门经过的秒数；本轮还没开过门时返回 float.MaxValue。
    /// 与 <see cref="SecondsSinceLastReady"/> 的区别：本属性不会被后续的 MarkReady
    /// 刷新，因此不会被"恢复很慢的那个 mod"拖住。
    /// </summary>
    public static float SecondsSinceFirstReady
    {
        get
        {
            if (FirstReadyAt < 0f) return float.MaxValue;
            try { return Time.realtimeSinceStartup - FirstReadyAt; }
            catch { return float.MaxValue; }
        }
    }

    /// <summary>
    /// 门禁关闭 → true（应当放行给原版 / 其它 mod）。
    /// 除了「未就绪」之外，还检测存档槽变更（玩家切档）。
    /// </summary>
    public static bool ShouldDefer
    {
        get
        {
            if (!_ready)
            {
                // S8x：超时兜底 —— 新开存档不走 LoadGame，必须靠时间窗口放行。
                TryForceReadyOnTimeout();
                if (!_ready) return true;
            }
            try
            {
                int cur = SaveSlotScope.GetCurrent();
                if (cur >= 0 && _slotAtReady >= 0 && cur != _slotAtReady) return true;
            }
            catch { }
            return false;
        }
    }

    /// <summary>
    /// S8x：如果 Invalidate 之后超过 ForceReadyAfterSeconds 秒还没被 MarkReady，
    /// 且游戏明显已就绪（PlayerStore 存在 + 能读到非负存档槽），主动放行。
    /// </summary>
    private static void TryForceReadyOnTimeout()
    {
        try
        {
            if (_invalidatedAt < 0f) return;
            if (Time.realtimeSinceStartup - _invalidatedAt < ForceReadyAfterSeconds) return;

            // 游戏就绪信号：PlayerStore 存在 + saveSlotId 有效
            if (!PlayerStore.IsInstanceExist() || PlayerStore.Instance == null) return;
            int slot = PlayerStore.Instance.saveSlotId;
            if (slot < 0) return;

            MarkReady("Timeout8s");
            try
            {
                ContainerSuiteLog.Diag(
                    "[ContainerSuite] WorldReadyGate 超时兜底放行（新开存档路径，slot=" + slot + "）");
            }
            catch { }
        }
        catch { }
    }

    /// <summary>由读档恢复协程的最后一帧调用。</summary>
    public static void MarkReady() => MarkReady(null);

    /// <param name="source">开门来源（日志用，如 "CI.RestoreDone" / "DIY.RestoreReady" / "Timeout8s"）。</param>
    public static void MarkReady(string source)
    {
        // 先算「距关门多久」再复位 _invalidatedAt（日志要用）
        float sinceInvalidate = -1f;
        if (_invalidatedAt >= 0f)
        {
            try { sinceInvalidate = Time.realtimeSinceStartup - _invalidatedAt; } catch { sinceInvalidate = -1f; }
        }

        _ready = true;
        _invalidatedAt = -1f;   // S8x：防止后续再被超时兜底重复触发
        try { _readyFrame = Time.frameCount; } catch { _readyFrame = 0; }
        try { LastReadyAt = Time.realtimeSinceStartup; } catch { LastReadyAt = -1f; }
        // S9：只记本轮第一次开门，之后的 MarkReady 不再覆盖
        if (FirstReadyAt < 0f) FirstReadyAt = LastReadyAt;
        try { _slotAtReady = SaveSlotScope.GetCurrent(); } catch { _slotAtReady = -1; }

        LogGateEvent("开门", source, sinceInvalidate);
    }

    /// <summary>场景切换 / 再次 LoadGame 时调用。</summary>
    public static void Invalidate() => Invalidate(null);

    /// <param name="source">关门来源（日志用，如 "CI.LoadGame" / "SceneLoaded"）。</param>
    public static void Invalidate(string source)
    {
        _ready = false;
        _readyFrame = -1;
        _slotAtReady = -1;
        FirstReadyAt = -1f;     // S9：本轮作废，下次 MarkReady 重新记"第一次"
        try { _invalidatedAt = Time.realtimeSinceStartup; } catch { _invalidatedAt = -1f; }

        LogGateEvent("关门", source, -1f);
    }

    // S8u-2：门禁事件只在加载期发生（每次读档/切场景 2~4 行），零常驻开销。
    // 用途：核对「开门 → SBE Bind → 读档恢复完成」的时序 ——
    // 2026-10-06 的崩溃取证只能靠人工拼散落日志，以后直接看这几行。
    // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 上述时序核对能力保留在
    //   DiagLog=true 下；默认关闭时不再每次读档/切场景都打这 2~4 行。
    private static void LogGateEvent(string what, string source, float sinceInvalidate)
    {
        try
        {
            string src = string.IsNullOrEmpty(source) ? "未标注来源" : source;
            string delta = sinceInvalidate >= 0f ? $"，距上次关门 {sinceInvalidate:0.00}s" : "";
            ContainerSuiteLog.Diag(
                $"[ContainerSuite] WorldReadyGate {what}（{src}{delta}）");
        }
        catch { }
    }
}