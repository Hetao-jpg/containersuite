// 三 Mod 共用的"只打印一次"异常留痕工具。
// 设计文档：ContainerSuite S2 §3.2
//
// 与三 Mod 各自版本的差异：
//   · 命名空间统一为 ContainerSuite.Shared（原来 DIY/CI 都是全局命名空间）
//   · 日志直走 MelonLogger.Warning 静态方法 —— 这是 catch 块的最后一道防线，
//     不能被 ContainerSuiteLog.Logger 未注入的状态影响。
//   · 保留 [CallerFilePath] + [CallerLineNumber]，拆分 partial 后自动取新行号。

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using MelonLoader;

namespace ContainerSuite.Shared;

/// <summary>
/// 「只打印一次」的异常留痕工具。
/// 用途：给原本完全静默的 catch 块留下痕迹，同时避免每帧路径（Tick / OnUpdate）
/// 反复刷屏 —— 同一个 where / file:line 只打印第一次。
/// <para>
/// ★ 2026-10-15：新增 <c>ContainerSuiteLog.SuppressCatchLog</c> 静音开关 ——
/// 预热期（WarmupSaveLoadChain）的哑参 invoke 会产生**预期 NRE**
/// （预热只需"方法体被 JIT 编译"这一步，编译发生在方法体执行前；
/// 之后抛 NRE 不影响目标达成），不该进日志。
/// 实机 2026-10-15 Latest.log 已复现 6 条此类噪音警告。
/// </para>
/// <para>
/// ★ 静音**只针对 CatchLogOnce**：ContainerSuiteLog 的 Msg / Warn / Error / Diag
///   走不同路径，不受本开关影响 —— 预热期的"步骤打点 / 完成汇报"仍照常输出。
/// </para>
/// </summary>
public static class CatchLogOnce
{
    private static readonly HashSet<string> _seen = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>显式指定语义化位置（如 "DiyUpgradeMod.FindTypeCached"）。</summary>
    public static void Warn(string where, Exception ex)
    {
        // 预热期静音：哑参 invoke 的 NRE 是预期噪音，见 ContainerSuiteLog.SuppressCatchLog。
        if (ContainerSuiteLog.SuppressCatchLog) return;
        if (string.IsNullOrEmpty(where)) where = "?";
        if (!_seen.Add(where)) return;
        Emit(where, ex);
    }

    /// <summary>
    /// 自动取调用点：文件名的主干（去掉目录与扩展名）+ 真实行号。
    /// 用于替代硬编码行号 —— 拆分 partial 后那些行号全部失真。
    /// </summary>
    public static void Warn(Exception ex, [CallerFilePath] string file = "", [CallerLineNumber] int line = 0)
    {
        // 预热期静音：同上。
        if (ContainerSuiteLog.SuppressCatchLog) return;
        string where = ShortFile(file) + ":" + line;
        if (!_seen.Add(where)) return;
        Emit(where, ex);
    }

    /// <summary>清空去重集合。场景切换 / 测试可用。</summary>
    public static void Clear()
    {
        _seen.Clear();
    }

    private static string ShortFile(string file)
    {
        if (string.IsNullOrEmpty(file)) return "?";
        try { return Path.GetFileNameWithoutExtension(file) ?? file; }
        catch { return file; }
    }

    private static void Emit(string where, Exception ex)
    {
        try
        {
            MelonLogger.Warning("[catch] " + where + ": " +
                (ex == null ? "(no exception)" : ex.GetType().Name + " - " + ex.Message));
        }
        catch { }
    }
}