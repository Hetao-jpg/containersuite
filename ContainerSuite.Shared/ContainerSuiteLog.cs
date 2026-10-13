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
        // ★ C-2（2026-10-12）：删除此处的 CacheLimits.Trim。
        //   原注释的理由是"整体清空后重复打印少量旧 key 无害"—— 对去重集合不成立：
        //   一旦整体清空，**已经打过的诊断点会在同一会话里再打一遍**，看起来像
        //   "这个状态又出现了一次"（去重信息丢失 = 制造假信号）。
        //   key 由调用方**显式指定**（诊断点本身，不是消息文本），种类有限；
        //   有界性交给 CacheRegistry 的场景/读档清空（ClearDiagOnce），不依赖这条兜底。
        //   ★ 新增调用方时请确保 key 是固定诊断点 —— 若把物品 id / 坐标拼进 key，
        //     键空间就变成单调增长，需要另行加淘汰（参照 DIY 的 EvictOldestLogKeys）。
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

    // ═══════════════════════════════════════════════════════════════════
    // [REG] 结构化回归标记（原 RegMark 类，2026-10-13 并入本类）。
    // 一行一个实例，供 _regress\regress.ps1 机器解析；人也能直接读。
    //   [REG]    id=<ascii> status=PASS|FAIL|NA detail=<任意文本>
    //   [REG.BP] name=<ascii> detail=<任意文本>       ← 断点：关键状态转换处留痕
    //
    // 设计原则（与 MEMORY 一致）：
    //   · id / name 一律 ASCII（避开编码坑），中文只进 detail。
    //   · 只加日志，不改任何逻辑、不改任何判据、不改任何写权限。
    //
    // ★ 2026-10-09 修订：改为**受 DiagLog 总闸控制**（原先无条件输出）。
    //   理由：[REG]/[REG.BP] 本质是诊断输出，默认常驻会把 Latest.log 撑成几十倍长度，
    //     而"启动成功 / 告警 / 错误"才是玩家需要长期看到的东西。
    //   连带影响：回归工具链必须开 DiagLog 才能拿到断言凭据 ——
    //     regress.ps1 已在 -Launch 前自动把 cfg 的 z1_DiagLog 置 true（结束后还原），
    //     checks.json 里依赖本标记的 22 条断言据此正常工作。
    //   ★ 格式 [REG] / [REG.BP] 一字不可改（checks.json 22 条断言机器解析依赖）。
    // ═══════════════════════════════════════════════════════════════════

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