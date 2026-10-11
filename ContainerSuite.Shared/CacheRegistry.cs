// P1-1（2026-10-10）创建。场景切换必须清空的「可重建缓存」统一登记处。
// 设计文档：容器优化方案.md P1-1。
//
// 疾病它防的是：可重建缓存散落在多个文件，场景切换时靠人工逐个 .Clear()。
//   漏清一处 ⇒ IL2CPP 复用同一地址时，新对象命中上一场景的旧缓存键
//   （静默、极难查；见 DiyUpgradeMod.State.cs 的 _sceneGeneration 注释）。
// 治法：所有「可重建缓存」在 Init() 里登记一次，OnSceneLoaded 只调 ClearAll()。
//   新增缓存若忘了登记，由 _shared\check-forbidden.ps1 的 R6 在编译期拦下
//   （不靠人记得）。
//
// ★ 顺序语义：ClearAll 按登记顺序执行（先登记的先清）。登记顺序在
//   DiyUpgradeMod.RegisterSceneCaches() 里显式写死，与旧 OnSceneLoaded 的
//   手写清理序列一一对应，行为等价。
//
// ★ 只登记「可重建的东西」：清空的代价仅是下次访问多算一次。清空任何
//   承载真实状态（不是缓存）的容器都会丢数据 —— 那不属于本登记处。

using System;
using System.Collections.Generic;

namespace ContainerSuite.Shared;

/// <summary>
/// 场景切换必须清空的「可重建缓存」的运行时登记处。
/// <para>
/// 用法：在 <c>Init()</c> 里对每个缓存调 <see cref="Register"/>；场景切换时调
/// <see cref="ClearAll"/>。字段名只用于日志与 R6 门禁核对，不参与运行时逻辑。
/// </para>
/// </summary>
public static class CacheRegistry
{
    private static readonly List<Action> _clearers = new List<Action>();
    private static readonly HashSet<string> _names = new HashSet<string>(StringComparer.Ordinal);

    /// <summary>
    /// R6 评审例外：经评审确认属于「进程生命周期」、按 int / string 值（而非 IL2CPP
    /// 裸指针）为键、因而**不**在场景切换时清空的缓存字段名。
    /// <para>
    /// 与 _shared\check-forbidden.ps1 的 R6 配套：名字以 Cache / Seen 结尾的
    /// <c>private static readonly Dictionary/HashSet</c> 必须在
    /// <see cref="Register"/> 里登记，或列在本数组里。★ 列入本数组只记录
    /// 2026-10-10 的评审结论，**不是**安全性证明，也**不**授权新增缓存不评审就列入。
    /// </para>
    /// </summary>
    public static readonly string[] KeepUncleared = new string[] {
        // 按 int tag 值缓存 IL2CPP 委托（TagUtil.cs）：键是数值不是地址，
        // 场景切换后依然有效，清空只会白做一次重建。
        "_tagSetterCache"
    };

    /// <summary>
    /// 登记一个「场景切换必须清空」的缓存。
    /// </summary>
    /// <param name="fieldName">缓存字段名（如 "_metaCache"）—— 供 R6 门禁核对。</param>
    /// <param name="clear">清空动作。为 null 时忽略。</param>
    /// <remarks>
    /// 幂等：同一 <paramref name="fieldName"/> 重复登记只生效一次（Init 重复调用安全）。
    /// </remarks>
    public static void Register(string fieldName, Action clear)
    {
        if (clear == null) return;
        if (string.IsNullOrEmpty(fieldName)) fieldName = "(unnamed)";
        if (!_names.Add(fieldName)) return;
        _clearers.Add(clear);
    }

    /// <summary>已登记的缓存数量（诊断用）。</summary>
    public static int Count => _clearers.Count;

    /// <summary>
    /// 清空所有已登记的缓存，按登记顺序执行。场景切换时调用。
    /// 单个清空动作抛异常不影响其余（逐项 try/catch + 首次留痕）。
    /// </summary>
    public static void ClearAll()
    {
        for (int i = 0; i < _clearers.Count; i++)
        {
            try { _clearers[i](); }
            catch (Exception ex) { CatchLogOnce.Warn("CacheRegistry.ClearAll", ex); }
        }
    }
}