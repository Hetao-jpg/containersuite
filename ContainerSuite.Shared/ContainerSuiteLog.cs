using System;
using System.Collections.Generic;
using MelonLoader;

namespace ContainerSuite.Shared;

/// <summary>
/// 三 Mod 共用的日志辅助。
/// 
/// 不持有 Logger 实例 —— 各模块用自己的 MelonLogger.Instance。
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
    /// 预热期静音 CatchLogOnce —— 哑参 invoke 触发的 NRE 是**预期噪音**
    /// （预热只需"方法体被 JIT 编译"这一步，编译发生在方法体执行前，
    ///   之后抛 NRE 也不影响目标达成），不该进日志。
    /// 由 WarmupSaveLoadChain 在进入/退出时置 true/false。
    /// <para>
    /// ★ 只静音 CatchLogOnce —— ContainerSuiteLog.Msg / Diag / Warn / Error
    ///   走的是不同路径，不受本开关影响（预热期的状态汇报仍会输出）。
    /// </para>
    /// </summary>
    internal static bool SuppressCatchLog;

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

    private static void SafeLog(Action<string> logFn, string msg)
    {
        try { logFn(msg); } catch { }
    }

    public static void Msg(string msg) => SafeLog(MelonLogger.Msg, msg);
    public static void Warn(string msg) => SafeLog(MelonLogger.Warning, msg);
    public static void Error(string msg) => SafeLog(MelonLogger.Error, msg);

    public static void Diag(string msg)
    {
        if (!DiagEnabled) return;
        SafeLog(MelonLogger.Msg, msg);
    }

    /// <summary>诊断日志，按 key 去重。同一个 key 只打印一次。</summary>
    public static void DiagOnce(string key, string msg)
    {
        if (!DiagEnabled) return;
        if (string.IsNullOrEmpty(key)) { Diag(msg); return; }
        if (!_diagSeen.Add(key)) return;
        // ★ C-2：此处不做 CacheLimits.Trim —— 对去重集合整体清空会把已打过的诊断点重打一遍
        //   （去重信息丢失 = 假信号）。有界性交给 CacheRegistry 的场景/读档清空（ClearDiagOnce）；
        //   ★ 新增调用方须保证 key 是**固定诊断点**（拼进物品 id / 坐标 → 键空间单调增长）。
        //   完整理由见 docs/silent-failure.md
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

    // [REG] / [REG.BP] 结构化回归标记（供 _regress\regress.ps1 机器解析）。
    // ★ 格式 [REG] / [REG.BP] 一字不可改（checks.json 22 条断言依赖）；受 DiagLog 总闸控制
    //   （回归前 regress.ps1 会自动把 z1_DiagLog 置 true）。设计原则与理由：见 docs/silent-failure.md

    /// <summary>断言：ok=true → PASS，否则 FAIL。</summary>
    public static void Mark(string id, bool ok, string detail = null)
        => Mark(id, ok ? "PASS" : "FAIL", detail);

    /// <summary>断言：显式给状态（含 NA = 不适用）。</summary>
    public static void Mark(string id, string status, string detail = null)
    {
        if (!DiagEnabled) return;
        try
        {
            string line = "[REG] id=" + id + " status=" + status;
            if (!string.IsNullOrEmpty(detail)) line += " detail=" + detail;
            Msg(line);
        }
        catch { }
    }

    /// <summary>断点：关键状态转换处留痕，供人工 / AI 事后断言。</summary>
    public static void Bp(string name, string detail = null)
    {
        if (!DiagEnabled) return;
        try
        {
            string line = "[REG.BP] name=" + name;
            if (!string.IsNullOrEmpty(detail)) line += " detail=" + detail;
            Msg(line);
        }
        catch { }
    }
}