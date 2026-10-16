// 场景切换必须清空的「可重建缓存」统一登记处。
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
    /// <summary>
    /// A-2（2026-10-12）：<b>读档</b>时清空的子集 —— 即 <see cref="_clearers"/> 中
    /// 未声明 <c>survivesLoadGame: true</c> 的那部分。
    /// <para>
    /// 与 <see cref="_clearers"/> 一样是**注册表**（进程生命周期，不是缓存），
    /// 不参与任何清空、也不登记（R6 判定范围是 Cache / Seen 结尾的缓存字段）。
    /// </para>
    /// </summary>
    private static readonly List<Action> _loadGameClearers = new List<Action>();
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
    /// <param name="survivesLoadGame">
    /// A-2（2026-10-12）：true = 「读档」时**不清**，只在场景切换（<see cref="ClearAll"/>）时清。
    /// 仅用于「键是类型 / 字段名等进程内稳定值、与存档内容无关」的解析缓存 —— 目前唯一使用者是
    /// <c>_offsetCache</c>（键 = <c>type|fallback|候选名</c>）。
    /// <para>
    /// ⚠ 默认 false。设 true 前先自问：这个缓存的键里**有没有**物品指针 / uniqueId / 场景代际？
    /// 有 ⇒ 必须保持 false（读档换的是世界内容，那些键全部失效）。
    /// </para>
    /// </param>
    /// <remarks>
    /// 幂等：同一 <paramref name="fieldName"/> 重复登记只生效一次（Init 重复调用安全）。
    /// </remarks>
    public static void Register(string fieldName, Action clear, bool survivesLoadGame = false)
    {
        if (clear == null) return;
        if (string.IsNullOrEmpty(fieldName)) fieldName = "(unnamed)";
        if (!_names.Add(fieldName)) return;
        _clearers.Add(clear);
        // A-2（2026-10-12）：survivesLoadGame=true 的项只进场景清空表。
        //   语义：读档 = 换了一套世界内容 ⇒ 以「物品指针 / uniqueId」为键的缓存全部失效；
        //   而「类型 / 字段名」为键的解析缓存在同一进程内仍然有效。
        if (!survivesLoadGame) _loadGameClearers.Add(clear);
    }

    /// <summary>已登记的缓存数量（诊断用）。</summary>
    public static int Count => _clearers.Count;

    /// <summary>
    /// 清空所有已登记的缓存，按登记顺序执行。场景切换时调用。
    /// 单个清空动作抛异常不影响其余（逐项 try/catch + 首次留痕）。
    /// </summary>
    public static void ClearAll() => ClearAllIn(_clearers, "CacheRegistry.ClearAll");

    /// <summary>
    /// A-2（2026-10-12）：<b>读档</b>专用清空 —— 与 <see cref="ClearAll"/> 的唯一差别是
    /// 跳过 <c>survivesLoadGame: true</c> 的登记项（当前只有 <c>_offsetCache</c>）。
    /// <para>
    /// 为什么需要它：读档**不一定**触发场景切换。不触发时 <see cref="ClearAll"/> 不会跑，
    /// 于是以「物品指针 / uniqueId」为键的缓存（如 <c>_metaCache</c>，键 = <c>id#uniqueId</c>）
    /// 会把上一存档的元数据喂给本存档的同 uid 容器。
    /// </para>
    /// <para>
    /// ★ 为什么不用 ClearAll：<c>_offsetCache</c> 的键是 <c>type|fallback|候选名</c>，进程内
    /// 恒定有效；清它零收益，代价是读档后首次 <c>FieldOffset</c> 重新走 FindOffsetField 反射，
    /// 而读档后第一个可能的调用点就在恢复深栈上 —— 正是 2026-10-10 读档闪退修复要避免的
    /// 「深栈上首次执行」类操作（见 DiyUpgradeMod.Patches.cs 的 WarmupRestoreChainRealItem）。
    /// </para>
    /// <para>
    /// ★ 场景切换（OnSceneLoaded）仍走 <see cref="ClearAll"/> 全清，行为一行未变。
    /// </para>
    /// </summary>
    public static void ClearForLoadGame() => ClearAllIn(_loadGameClearers, "CacheRegistry.ClearForLoadGame");

    private static void ClearAllIn(List<Action> list, string tag)
    {
        for (int i = 0; i < list.Count; i++)
        {
            try { list[i](); }
            catch (Exception ex) { CatchLogOnce.Warn(tag, ex); }
        }
    }

    /// <summary>A-2：读档清空会覆盖的登记项数量（诊断用；与 <see cref="Count"/> 的差 = survive 项）。</summary>
    public static int LoadGameCount => _loadGameClearers.Count;
}