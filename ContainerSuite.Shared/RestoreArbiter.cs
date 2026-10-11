// S2 阶段创建。容器尺寸恢复仲裁器，从 DIYUpgrade 搬并解耦。
// 设计文档：ContainerSuite S2 §3.8
//
// 与 DIYUpgrade 原版的差异：
//   · 命名空间统一为 ContainerSuite.Shared
//   · 去掉对 DiyUpgradeMod 的直接依赖 —— 改用 Applier 委托：
//       RegisterApplier(moduleId, applyFunc) 注册
//       Tick 里根据 Request 的 reason 前缀分发到对应 Applier
//   · 日志走 ContainerSuiteLog；诊断受 DiagEnabled 控制
//   · IdOf 用 IdMatch.IdentifierOf（不走 DIY 的诊断语义）
//   · 保留 IsApplying / ApplyingPtr / OnForeignShapeChanged / ClearAll / Cancel
//
// 单写者保证：全进程只有 Tick() 一处允许调 Applier。
// 单物品串行：同一 IntPtr 同时只有一个事务，重入合并。
// 隔离窗口：应用前让 1 帧给别的 mod，应用期间 IsApplying=true 屏蔽所有回环。

using System;
using System.Collections.Generic;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

namespace ContainerSuite.Shared;

/// <summary>
/// 容器尺寸恢复仲裁器。
/// 
/// 要解决的竞争：
///   · 多个恢复触发点同帧写同一 grid
///   · 外部 mod 刚写完我们紧接着写
///   · 我们写 shape 触发游戏再 ValidateShapeState 回环
/// </summary>
public static class RestoreArbiter
{
    private enum Phase { Pending, Cooldown, InFlight }

    private sealed class Entry
    {
        public GameItem Item;
        public IntPtr Ptr;
        public Phase Phase;
        public int RequestedFrame;
        public int CooldownUntilFrame;
        public int Attempts;
        public string Reason;
    }

    private const int MaxAttempts = 3;
    private const int CooldownFrames = 1;    // 应用前让出的帧数
    private const int ForeignCooldown = 2;   // 外部 mod 刚写完后的冷却帧数

    private static readonly Dictionary<IntPtr, Entry> _entries = new Dictionary<IntPtr, Entry>(64);
    private static readonly Queue<IntPtr> _queue = new Queue<IntPtr>(64);

    // 模块 Applier 注册表：moduleId → apply 委托
    private static readonly Dictionary<string, Action<GameItem>> _appliers
        = new Dictionary<string, Action<GameItem>>(StringComparer.Ordinal);

    // ── P3（2026-10-02）：无 Applier 匹配时的告警 ──
    // 单 applier 时所有未匹配 reason 都靠 ResolveApplier 的 Count==1 兜底；
    // 注册第二个 applier 后兜底失效，未注册的 reason 会被静默丢弃。
    // 这里按 reason 去重打 Warning，最多 NoApplierWarnLimit 条，防止刷屏。
    private static readonly HashSet<string> _noApplierWarned = new HashSet<string>(StringComparer.Ordinal);
    private const int NoApplierWarnLimit = 32;

    // 从单 applier 兜底 → 严格匹配的瞬间提醒一次（注册表状态，与场景无关，不随 ClearAll 复位）
    private static bool _multiApplierWarned;

    // 当前正在应用的物品指针。所有 Patch 入口第一件事查这个。
    private static IntPtr _applyingPtr = IntPtr.Zero;

    // 最后一次观察到外部 mod（XIAOWO/Wage）写 shape 的帧号。
    private static int _lastForeignWriteFrame = -1;

    // 日志汇总：每轮队列清空时打一行。
    private static int _processedSinceIdle;
    private static int _busySinceFrame = -1;

    /// <summary>当前是否处于「应用事务」中。</summary>
    public static bool IsApplying => _applyingPtr != IntPtr.Zero;

    /// <summary>正在应用的物品指针（0 表示没有）。</summary>
    public static IntPtr ApplyingPtr => _applyingPtr;

    /// <summary>当前挂起的事务数。</summary>
    public static int PendingCount => _queue.Count;

    /// 注册模块的恢复逻辑。
    /// moduleId 应与 Request 的 reason 前缀一致（如 "DIY"）。
    /// 当前只注册了一个 "DIY"，因此所有无前缀 reason 都靠单 Applier 兜底命中。
    /// 重复注册同一个 moduleId 会覆盖旧委托。
    public static void RegisterApplier(string moduleId, Action<GameItem> apply)
    {
        if (string.IsNullOrEmpty(moduleId) || apply == null) return;

        bool wasPresent = _appliers.ContainsKey(moduleId);
        _appliers[moduleId] = apply;

        // P3：从「单 applier 兜底」切到「多 applier 严格匹配」的瞬间提醒一次。
        // 单 applier 时所有未匹配 reason 走 ResolveApplier 里的 Count==1 兜底；
        // 第二个 applier 注册后兜底失效 —— 提示作者把所有 Request 的 reason 前缀都注册。
        if (!wasPresent && _appliers.Count == 2 && !_multiApplierWarned)
        {
            _multiApplierWarned = true;
            ContainerSuiteLog.Warn("[Arbiter] 已注册第二个 Applier —— 从单 applier 兜底切换到严格匹配。"
                + "请确认所有 Request 的 reason 前缀都已注册，否则会被丢弃。已有：[" + JoinApplierKeys() + "]");
        }
    }

    /// <summary>注销模块的恢复逻辑。</summary>
    public static void UnregisterApplier(string moduleId)
    {
        if (string.IsNullOrEmpty(moduleId)) return;
        _appliers.Remove(moduleId);
    }


    /// <summary>
    /// 请求恢复。
    /// reason 约定：
    ///   · "LoadGame" —— PlayerStore.LoadGame Postfix 的读档恢复
    ///   · "Show"     —— WindowShowHook 的窗口每次 Show 后的恢复
    /// 两个 reason 当前都不带模块前缀（无冒号），因此 ResolveApplier
    /// 走 _appliers.Count == 1 的兜底分支，命中唯一注册的 "DIY"。
    /// 如果将来第二个模块注册 Applier，必须给这两个 reason 补前缀，
    /// 否则会被丢弃并触发 WarnNoApplierOnce。
    /// </summary>
    public static void Request(GameItem item, string reason)
    {
        if (item == null) return;

        IntPtr ptr;
        try { ptr = item.Pointer; }
        catch { return; }
        if (ptr == IntPtr.Zero) return;

        // 正在应用中：合并进当前事务（吞掉）
        if (_applyingPtr == ptr) return;

        if (_entries.TryGetValue(ptr, out Entry existing))
        {
            // 已有事务：刷新时间戳 + 重置冷却，不重复入队。
            // 注意不重置 Attempts —— 防止外部反复请求导致无限重试。
            existing.RequestedFrame = SafeFrameCount();
            existing.CooldownUntilFrame = 0;
            existing.Reason = reason;
            return;
        }

        _entries[ptr] = new Entry
        {
            Item = item,
            Ptr = ptr,
            Phase = Phase.Pending,
            RequestedFrame = SafeFrameCount(),
            CooldownUntilFrame = 0,
            Attempts = 0,
            Reason = reason,
        };
        _queue.Enqueue(ptr);
    }

    /// <summary>挂在 OnUpdate 最前面，每帧最多处理 1 个事务。</summary>
    public static void Tick()
    {
        if (_queue.Count == 0)
        {
            if (_processedSinceIdle > 0)
            {
                if (ContainerSuiteLog.DiagEnabled && _busySinceFrame >= 0)
                {
                    int span = SafeFrameCount() - _busySinceFrame;
                    // ★ 2026-10-09：已在此处判过 DiagEnabled，用 Diag 而非 Msg ——
                    //   避免闸门关闭时仍把字符串拼好再丢弃（这行每轮恢复都走一次）。
                    ContainerSuiteLog.Diag("[Arbiter] 本轮处理 " + _processedSinceIdle + " 个容器，耗时 " + span + " 帧");
                }
                _processedSinceIdle = 0;
                _busySinceFrame = -1;
            }
            return;
        }
        if (_busySinceFrame < 0) _busySinceFrame = SafeFrameCount();

        // 加载期不处理，全部排队
        try { if (WorldReadyGate.ShouldDefer) return; }
        catch { return; }

        // 上一事务还没结束：等下一帧
        if (IsApplying) return;

        CrashProbe.Enter("Arbiter.Peek");
        IntPtr ptr = _queue.Peek();
        if (!_entries.TryGetValue(ptr, out Entry e))
        {
            _queue.Dequeue();
            CrashProbe.Exit("Arbiter.Peek");
            return;
        }
        CrashProbe.Exit("Arbiter.Peek");

        int now = SafeFrameCount();

        // 首次处理：先设冷却，让一帧给别的 mod 写
        if (e.Phase == Phase.Pending)
        {
            e.Phase = Phase.Cooldown;
            e.CooldownUntilFrame = now + CooldownFrames;
            return;
        }

        if (now < e.CooldownUntilFrame) return;

        // 外部 mod 刚写过：再让 ForeignCooldown 帧
        if (_lastForeignWriteFrame >= 0 && now - _lastForeignWriteFrame < ForeignCooldown)
        {
            e.CooldownUntilFrame = now + 1;
            return;
        }

        // 出队 + 存活校验
        _queue.Dequeue();

        CrashProbe.Enter("Arbiter.Liveness");
        GameItem item = e.Item;
        if (item == null) { _entries.Remove(ptr); CrashProbe.Exit("Arbiter.Liveness"); return; }
        try
        {
            if (item.Pointer != ptr) { _entries.Remove(ptr); CrashProbe.Exit("Arbiter.Liveness"); return; }
            if (item.state == null) { _entries.Remove(ptr); CrashProbe.Exit("Arbiter.Liveness"); return; }
        }
        catch { _entries.Remove(ptr); CrashProbe.Exit("Arbiter.Liveness"); return; }
        CrashProbe.Exit("Arbiter.Liveness");

        // 找出对应的 Applier
        CrashProbe.Enter("Arbiter.ResolveApplier");
        Action<GameItem> applier = ResolveApplier(e.Reason);
        CrashProbe.Exit("Arbiter.ResolveApplier");
        if (applier == null)
        {
            // 没有注册的 Applier：直接放弃，不重试（P3 起无条件 Warning）
            WarnNoApplierOnce(e.Reason, item);
            _entries.Remove(ptr);
            return;
        }

        // ═══ 单写者 ═══
        _applyingPtr = ptr;
        bool ok = false;
        CrashProbe.Enter("Arbiter.Apply[" + (e.Reason ?? "?") + "]");
        try
        {
            applier(item);
            ok = true;
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("RestoreArbiter.Apply", ex);
        }
        finally
        {
            _applyingPtr = IntPtr.Zero;
        }
        CrashProbe.Exit("Arbiter.Apply");

        if (ok)
        {
            _entries.Remove(ptr);
            _processedSinceIdle++;
            return;
        }

        // 失败：退避重试，最多 3 次
        e.Attempts++;
        e.Phase = Phase.Pending;
        if (e.Attempts >= MaxAttempts)
        {
            _entries.Remove(ptr);
            string giveUpId = IdMatch.IdentifierOf(item);
            ContainerSuiteLog.Warn("[Arbiter] 放弃恢复 " + giveUpId
                + "（" + MaxAttempts + " 次失败）");
            // B2（2026-10-07）：放弃 = 恢复静默失效的关键状态转换，无条件留痕。
            RegMark.Bp("arbiter.abandon", "why=maxattempts item=" + giveUpId + " attempts=" + MaxAttempts);
        }
        else
        {
            e.CooldownUntilFrame = now + 5;
            _queue.Enqueue(ptr);
        }
    }

    /// <summary>场景切换 / 新档加载时清空（旧 GameItem 指针全部失效）。</summary>
    public static void ClearAll()
    {
        _entries.Clear();
        _queue.Clear();
        _applyingPtr = IntPtr.Zero;
        _lastForeignWriteFrame = -1;
        _noApplierWarned.Clear();   // P3（_multiApplierWarned 不复位 —— 注册表状态与场景无关）
    }

    
    /// <summary>
    /// P3：无 Applier 匹配 reason 时打一次 Warning。
    /// 按 reason 去重，最多 NoApplierWarnLimit 条不同 reason；超限后静默。
    /// </summary>
    private static void WarnNoApplierOnce(string reason, GameItem item)
    {
        string key = string.IsNullOrEmpty(reason) ? "(null)" : reason;

        // 已达上限且是新 reason → 不再新增（防集合无界增长）
        if (_noApplierWarned.Count >= NoApplierWarnLimit && !_noApplierWarned.Contains(key)) return;
        if (!_noApplierWarned.Add(key)) return;

        try
        {
            ContainerSuiteLog.Warn("[Arbiter] 无 Applier 匹配 reason=\"" + key
                + "\" item=" + IdMatch.IdentifierOf(item)
                + "（已注册 " + _appliers.Count + " 个：[" + JoinApplierKeys() + "]"
                + (_noApplierWarned.Count == NoApplierWarnLimit ? "，后续新 reason 不再打印" : "") + "）");
        }
        catch { }

        // B2（2026-10-07）：无 Applier = 恢复请求被静默丢弃的关键原因，无条件留痕。
        //   去重上限同上面的 Warning（最多 NoApplierWarnLimit 条，不会刷屏）。
        RegMark.Bp("arbiter.abandon", "why=noapplier reason=" + key);
    }

    private static string JoinApplierKeys()
    {
        if (_appliers.Count == 0) return "(空)";
        var sb = new System.Text.StringBuilder();
        foreach (var k in _appliers.Keys)
        {
            if (sb.Length > 0) sb.Append(", ");
            sb.Append(k);
        }
        return sb.ToString();
    }

    private static Action<GameItem> ResolveApplier(string reason)
    {
        if (string.IsNullOrEmpty(reason)) return null;

        // 完整匹配优先
        if (_appliers.TryGetValue(reason, out var exact)) return exact;

        // 前缀匹配：reason 形如 "DIY:xxx" 时找 "DIY"
        int colon = reason.IndexOf(':');
        if (colon > 0)
        {
            string prefix = reason.Substring(0, colon);
            if (_appliers.TryGetValue(prefix, out var pfx)) return pfx;
        }

        // 单个 Applier 时兜底（常见于 S2/S3 早期只注册一个模块）
        if (_appliers.Count == 1)
        {
            foreach (var kv in _appliers) return kv.Value;
        }

        return null;
    }

    private static int SafeFrameCount()
    {
        try { return Time.frameCount; }
        catch { return Environment.TickCount; }
    }
}