// 三 Mod 共用的小工具。
//
// 抽取范围（3 项，都是零风险的小函数）：
//   · TrySilent(Action) —— 吞异常 + 转发 CatchLogOnce（按 where 去重）
//   · Ptr(Il2CppObjectBase) —— 指针转字符串（诊断用）
//   · SameObject(a, b) —— 按 native 指针比较两个 Il2Cpp 对象
//
// 与 SBE 原版 TrySilent 的差异：
//   · 支持显式传 where（保留 SBE 的 "savebag-refresh" 诊断标签）
//   · 另提供 [CallerFilePath]/[CallerLineNumber] 自动定位重载（DIY 侧更常用）
//   · 异常统一走 CatchLogOnce，不再各自写一份去重逻辑
//
// 不做（经评估是设计差异，不是重复；详见 README 维护日志 S8s）：
//   · RebuildGridFill / SetShapeKeepTransparency / GridAlphaFor / WindowNeedsSync

using System;
using System.Runtime.CompilerServices;
using Il2CppInterop.Runtime.InteropTypes;

namespace ContainerSuite.Shared;

/// <summary>
/// 通用 Interop / 异常工具。跨 DLL 复用的、不依赖具体游戏类型的小工具归口。
/// </summary>
public static class InteropUtil
{
    // ─────────────────────────────────────────────────────────────
    // 安全调用
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 执行一个可能抛异常的 Action，异常走 CatchLogOnce（按 where 去重）。
    /// where 为空时用 "InteropUtil.TrySilent" 作为去重键。
    /// </summary>
    public static void TrySilent(Action action, string where)
    {
        if (action == null) return;
        try { action(); }
        catch (Exception ex)
        {
            CatchLogOnce.Warn(
                string.IsNullOrEmpty(where) ? "InteropUtil.TrySilent" : where,
                ex);
        }
    }

    /// <summary>
    /// 自动取调用点（文件名 + 行号）作为 where 的安全调用。
    /// 适合 DIY 侧那种"十几处 try-catch 各写各的"场景，省去手写标签。
    /// </summary>
    public static void TrySilent(Action action,
        [CallerFilePath] string file = "",
        [CallerLineNumber] int line = 0)
    {
        if (action == null) return;
        try { action(); }
        catch (Exception ex) { CatchLogOnce.Warn(ex, file, line); }
    }

    // ─────────────────────────────────────────────────────────────
    // 指针工具（诊断用）
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// Il2Cpp 对象指针转字符串（如 "0x7ff8a1b2c3d4"）。
    /// 空对象返回 "null"；取指针异常返回 "?"。
    /// </summary>
    public static string Ptr(Il2CppObjectBase o)
    {
        if (o == null) return "null";
        try { return "0x" + o.Pointer.ToString("X"); }
        catch { return "?"; }
    }

    /// <summary>
    /// 两个 Il2Cpp 对象是否是同一个 native 对象（按指针比较）。
    /// 任一为 null → false；取指针异常 → false。
    /// </summary>
    public static bool SameObject(Il2CppObjectBase a, Il2CppObjectBase b)
    {
        if (a == null || b == null) return false;
        try { return a.Pointer == b.Pointer; }
        catch { return false; }
    }
}