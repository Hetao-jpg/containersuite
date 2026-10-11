// S3-1 改造：去掉全局 Logger 单例，避免多 Mod 覆盖。
// Msg/Warn/Error 改为静态调用 MelonLogger 静态方法（无 Mod 归属）；
// 各模块用自己的 LoggerInstance，日志前缀天然归属自己。

using System;
using System.Collections.Generic;
using MelonLoader;

namespace ContainerSuite.Shared;

/// <summary>
/// 三 Mod 共用的日志辅助。
/// 
/// S3-1 起不再持有 Logger 实例 —— 各模块用自己的 MelonLogger.Instance。
/// 本类只负责：
///   · DiagEnabled 开关（cfg [CustomInfrastructure] 的 DiagLog —— 2026-10-07 起
///     三个模块共用这一条总开关；CI / SBE / DIY 的访问器都是它的别名）
///   · DiagOnce 去重
///   · 给 Shared 内部代码提供一个统一的日志出口
/// </summary>
public static class ContainerSuiteLog
{
    // ═══════════════════════════════════════════════════════════════════
    // S8v+（2026-10-05）：DiagEnabled 从"启动缓存"改为"实时判据"。
    //
    // 背景：原来 CI 在 OnInitializeMelon 里赋值一次，之后 ModManager
    //   面板修改 DiagLog 不会重新计算，必须重启游戏才生效。玩家困惑。
    //
    // 方案：CI 注入一个回调（SetDiagProvider），DiagEnabled 每次读时调用它。
    //
    // ★ 2026-10-07：三个模块的诊断开关合并为一条总开关后，回调只剩一次
    //   .Value 读取（原来要读 3 个再 OR）。
    //
    // 性能：每帧最多读几次（OnUpdate 1 次 + Arbiter 队尾清理 1 次），
    //   每次成本 = 1 次 .Value getter（托管字段读），可忽略。
    // ═══════════════════════════════════════════════════════════════════

    private static Func<bool> _diagProvider;
    private static bool _diagStaticFallback;

    /// <summary>
    /// 注入实时判据。之后 <see cref="DiagEnabled"/> 每次读都会调用它。
    /// 未注入时退回到上次 set 的静态值。
    /// </summary>
    public static void SetDiagProvider(Func<bool> provider)
    {
        _diagProvider = provider;
    }

    /// <summary>
    /// 是否输出诊断日志。
    /// <para>
    /// 有 provider 时：每次实时读（ModManager 改 DiagLog 立即生效）。
    /// 无 provider 时：返回上次 set 的静态值（兼容未注入的旧路径）。
    /// </para>
    /// </summary>
    public static bool DiagEnabled
    {
        get
        {
            var p = _diagProvider;
            if (p != null)
            {
                try { return p(); } catch { return false; }
            }
            return _diagStaticFallback;
        }
        set { _diagStaticFallback = value; }
    }

    private static readonly HashSet<string> _diagSeen = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>Shared 内部用的日志方法（无 Mod 归属前缀）。</summary>
    public static void Msg(string msg)
    {
        try { MelonLogger.Msg(msg); } catch { }
    }

    /// <summary>Shared 内部用的警告方法（无 Mod 归属前缀）。</summary>
    public static void Warn(string msg)
    {
        try { MelonLogger.Warning(msg); } catch { }
    }

    /// <summary>Shared 内部用的错误方法（无 Mod 归属前缀）。</summary>
    public static void Error(string msg)
    {
        try { MelonLogger.Error(msg); } catch { }
    }

    /// <summary>诊断日志。DiagEnabled=false 时静默。</summary>
    public static void Diag(string msg)
    {
        if (!DiagEnabled) return;
        try { MelonLogger.Msg(msg); } catch { }
    }

    /// <summary>诊断日志，按 key 去重。同一个 key 只打印一次。</summary>
    public static void DiagOnce(string key, string msg)
    {
        if (!DiagEnabled) return;
        if (string.IsNullOrEmpty(key)) { Diag(msg); return; }
        if (!_diagSeen.Add(key)) return;
        // 容量上限：防止 Diag 长期开启时集合无限增长。
        // 超过上限就整体清空（而非逐项淘汰）—— 清空后可能重复打印少量旧 key，
        // 但比逐项淘汰的 LRU 实现简单得多，且 DiagOnce 只用于诊断输出，重复打印无害。
        CacheLimits.Trim(_diagSeen, CacheLimits.Large);   // A1（原裸写 >512）
        try { MelonLogger.Msg(msg); } catch { }
    }

    /// <summary>清空 DiagOnce 的去重集合。</summary>
    public static void ClearDiagOnce()
    {
        _diagSeen.Clear();
    }

    /// <summary>复位：DiagEnabled / 去重集合全部清空。</summary>
    public static void Reset()
    {
        DiagEnabled = false;
        _diagSeen.Clear();
    }
}