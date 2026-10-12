// S2 改造：Shared 抽取后，本文件只保留 DIYUpgrade 特有的诊断/工具方法。
// Shared 已覆盖的成员全部删除：
//   · FieldOffset / NormalizeFieldName / FindOffsetField / Il2CppFieldGetOffset
//   · _offsetCache / _offsetResolveDistrusted / _typeByNameCache
//   · FindTypeCached（改走 TypeCache）
//   · IsAnyId / WildcardMatch（保留签名，内部转发到 IdMatch）
//
// 保留：
//   · Off 常量由 Types.cs 提供，本文件不重复定义
//   · WarmUpOffsetDiag / LogOffsetDiag / LogOffsetDiagOne（改走 OffsetResolver）
//   · MarkOffsetSuspect / VerifyDragOffset（改走 OffsetResolver.MarkDistrusted / IsDistrusted）
//   · LogOnce（DIY 特有的 3 秒去重日志）
//   · OptionalPatchNames
//   · PtrFieldNull / IdOffset / IdFieldNull / TagSystemFieldNull
//   · IdOf（保留诊断语义 <null-id>/<empty-id>/<err-id>）
//   · InventoryOwner
//
// 顺手修复 S0 发现的 🔴 问题：TagSystemFieldNull 的候选字段名从 tagSystem 改为 state。

using ContainerSuite.Shared;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using MelonLoader.Preferences;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    // 当前 Id 偏移是不是运行时解析值（自校验失败时据此判断要不要整机制降级）。
    // 注意：distrust 状态本身托管在 Shared 的 OffsetResolver 里，本字段只记录
    // "本次解析用了什么"，与 distrust 无关。
    private static bool _idOffsetFromResolve;

    // ═══════════════════════════════════════════════════════════════════════
    // 可选 Patch 黑名单：这里列出的 Patch 类名在 PatchAll 时会被跳过。
    // 目前 DIYUpgrade 没有可选功能，集合为空。
    // ═══════════════════════════════════════════════════════════════════════
    private static readonly HashSet<string> OptionalPatchNames = new HashSet<string>(StringComparer.Ordinal);

    private static void MarkOffsetSuspect()
    {
        _offsetSuspectCount++;
        if (!_offsetWarningLogged && _offsetSuspectCount >= 5)
        {
            _offsetWarningLogged = true;
            ContainerSuiteLog.Error("[容器升级] 连续多次无法解析拖拽源对象，Off.* 内存偏移可能已失效（游戏更新？）。");
        }
    }

    // P5（2026-10-02）：Drag 偏移的独立告警。
    // 与 MarkOffsetSuspect 的差异：文案精确指向 DragSource，不混同 Id 偏移。
    // 不自动降级（DragSource 没有可用的反查替代）—— 只告警。
    internal static void MarkDragOffsetSuspect()
    {
        _dragOffsetSuspectCount++;
        if (!_dragOffsetWarningLogged && _dragOffsetSuspectCount >= 5)
        {
            _dragOffsetWarningLogged = true;
            ContainerSuiteLog.Error("[容器升级] 连续多次无法从 DragSource 偏移解析出有效物品，"
                + "Off.DragSource 可能已失效（游戏更新？）。升级功能会静默失效，请检查此项。");
        }
    }

    private static void VerifyDragOffset(GameItem source)
    {
        if (_offsetChecked) return;
        if (source == null)
        {
            _offsetCheckFails++;
            if (_offsetCheckFails >= 3)
            {
                _offsetChecked = true;
                MarkOffsetSuspect();
            }
            return;
        }
        try
        {
            string id = IdOf(source);
            bool bad = string.IsNullOrEmpty(id) || id == "<null-id>" || id == "<err-id>" || id == "<empty-id>";
            if (!bad)
            {
                // 非空只是弱校验——偏移读错也可能读出一串看似合法的垃圾字符。
                // 用游戏自己的 IsID 反查：identifier 字段读出来的必须就是这个 item 的 ID。
                try { if (!source.IsID(id)) bad = true; }
                catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.VerifyDragOffset.IsID", ex); }
            }
            if (bad)
            {
                // 自校验不过，而且当前用的正是解析出来的偏移 —— 说明这次解析不可信，整体降级。
                if (_idOffsetFromResolve && !OffsetResolver.IsDistrusted)
                {
                    OffsetResolver.MarkDistrusted();
                    _cachedIdOffset = -1;   // S8u：清缓存，下次重新解析（会回退 fallback）
                    ContainerSuiteLog.Warn("[容器升级] 运行时解析出的内存偏移未通过 IsID 自校验，已全部回退为硬编码值。");
                }
                _offsetCheckFails++;
                if (_offsetCheckFails >= 3)
                {
                    _offsetChecked = true;
                    MarkOffsetSuspect();
                }
                return;
            }
            _offsetChecked = true;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.VerifyDragOffset", ex); }
    }

    /// <summary>
    /// C-2（2026-10-12）：去重集合的**有界淘汰** —— 只丢最旧的一批，不清空整体。
    /// <para>
    /// ★ 为什么不再用 <c>CacheLimits.Trim</c>：Trim 的语义是「超限即整体 Clear」。
    ///   对**去重集合**用整体 Clear 是错的 —— 清空后旧 key 立刻可以重新打印，
    ///   玩家看到"同一个错又出现了"，反而误判为功能反复出错。
    /// </para>
    /// <para>
    /// ★ 为什么仍要淘汰：key 是消息文本，而部分调用点把物品 id / 槽号 / WxH 拼进了
    ///   消息（见 GridWindow 的拖拽告警），键空间随玩家操作**单调增长** ⇒
    ///   「会话内消息种类天然有限」这个假设不成立，不能完全不设上界。
    ///   按"最后打印时间"淘汰最旧的一批：被淘汰的正好是长期没再出现的消息，
    ///   近期活跃消息的去重信息不丢。
    /// </para>
    /// <para>
    /// ★ 只在这里调用（热路径），超限时才排序，正常情况是 O(1) 判空返回。
    /// </para>
    /// </summary>
    private static void EvictOldestLogKeys(int limit)
    {
        if (_lastLogTimeByKey.Count <= limit) return;
        int target = limit - (limit >> 2);   // 一次淘汰到 3/4，避免"每加一条就淘汰一次"
        var keys = new List<string>(_lastLogTimeByKey.Keys);
        keys.Sort((a, b) => _lastLogTimeByKey[a].CompareTo(_lastLogTimeByKey[b]));
        int remove = keys.Count - target;
        for (int i = 0; i < remove; i++) _lastLogTimeByKey.Remove(keys[i]);
    }

    private static void LogOnce(string msg, bool warning = false)
    {
        if (string.IsNullOrEmpty(msg)) return;
        // ★ T1（2026-10-12）：3 秒窗口改单调钟判定。原用 DateTime.UtcNow 差值 ——
        //   系统时钟一回拨，(now - last) 变负 ⇒ "Elapsed < 3.0" 恒真 ⇒ **所有 LogOnce
        //   输出被永久静音**（连异常告警都看不到），且没有任何报错。见 Shared/PollThrottle.cs。
        //   C-4 的 `!(a < b) → a >= b` 化简已被这个写法吸收。
        long nowMs = PollThrottle.NowMs;
        if (_lastLogTimeByKey.TryGetValue(msg, out var lastMs) && PollThrottle.ElapsedMs(lastMs) < 3000) return;
        // ★ C-2（2026-10-12）：淘汰改为"丢最旧一批"，不再整体 Clear（见 EvictOldestLogKeys）。
        EvictOldestLogKeys(CacheLimits.Small);
        _lastLogTimeByKey[msg] = nowMs;
        // ★ D-1（2026-10-12）：warning=true 走**无条件**输出。
        //   理由：走 Diag 的输出受 cfg z1_DiagLog（默认 false）门控 —— 默认静默的
        //   "玩家可见提示"等于没做（项目教训：默认静默的日志会被误读成"没执行"）。
        //   Diag 语义的调用点（warning 默认 false）行为一行未变。
        if (warning) ContainerSuiteLog.Warn(msg);
        else ContainerSuiteLog.Diag(msg);
    }

    private static bool PtrFieldNull(GameItem item, int offset)
    {
        if (item == null) return true;
        try { return Marshal.ReadIntPtr(((Il2CppObjectBase)item).Pointer, offset) == IntPtr.Zero; }
        // 指针/反射读取失败按「字段为空」处理（既有语义），仅首次留痕。
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.PtrFieldNull", ex); return true; }
    }

    /// <summary>
    /// GameItem.identifier 字段的偏移。首次调用走 OffsetResolver 解析，之后缓存。
    /// <para>
    /// ★ 为什么要缓存：调用链是 IdFieldNull → IdOf → MetaKey → LoadMeta，
    ///   命中每帧路径（tooltip / 拖拽 / IsResizableContainer 都会走）。
    ///   OffsetResolver.FieldOffset 内部虽有 cache，但每次仍要拼 cache key
    ///   字符串 + 走一次字典查找 —— 每帧路径上没必要重复付这个成本。
    /// </para>
    /// <para>
    /// ★ 缓存安全前提：OffsetResolver 一旦 MarkDistrusted 就永久回退硬编码，
    ///   同一场景内 offset 值稳定。distrust 触发时（见 VerifyDragOffset）
    ///   和场景切换时都会主动清此缓存。
    /// </para>
    /// </summary>
    private static int _cachedIdOffset = -1;

    private static int IdOffset()
    {
        if (_cachedIdOffset >= 0) return _cachedIdOffset;
        // 走 Shared 的 OffsetResolver。apply 保持 true（identifier 有 IsID 自校验兜底）。
        int off = OffsetResolver.FieldOffset(typeof(GameItem), new string[] { "identifier", "id" }, Off.Id, apply: true);
        _idOffsetFromResolve = (off != Off.Id);
        _cachedIdOffset = off;
        return off;
    }

    private static bool IdFieldNull(GameItem item) => PtrFieldNull(item, IdOffset());

    // tagSystem 没法像 Id 那样自校验，所以只在 Id 的解析通过校验（机制可信）后才可能被采用。
    // S0 数据校对结果：GameItem 里实际字段名为 state（0x1B0），不是 tagSystem。
    //
    // ★ B-2（2026-10-12）：apply 由「恒 false」改为**跟随 distrust 状态**。
    //   原实现永远用硬编码 432：游戏一旦更新改了 state 的偏移，我们会照旧读旧地址
    //   （读到别的字段），tag 系统判据随即失效 —— 而且这是**静默**的
    //   （OffsetResolver 里 apply=false 的分支原本连告警都不打），正是最忌讳的一类故障。
    //   依据：state 与 identifier 共用同一套 NativeFieldInfoPtr_* 反射机制，
    //   IdOffset 的 IsID 自校验通过 ⇒ 机制可信 ⇒ state 的解析结果也可信。
    //   故：机制未失信时采用解析值；被标失信时回退硬编码 432（与 IdOffset 同一条路）。
    // ★ 候选名收窄为 {"state"} 一项：游戏里字段名就是 state（0x1B0），"tagSystem" 是
    //   S0 之前的误名；留着它会在**精确匹配失败时参与包含匹配**（FindOffsetField 的
    //   loose 分支），有命中到无关字段的风险。
    // ⚠ 当前游戏版本上 resolved == fallback == 432，所以本次改动**不改变任何行为**；
    //   新分支只有等游戏更新导致偏移漂移时才第一次执行 —— 换言之：新逻辑在当前环境
    //   不可验证，"回归全绿"只能证明"没有变化"，**不能**读成"新逻辑验证过了"。
    private static bool TagSystemFieldNull(GameItem item) =>
        PtrFieldNull(item, OffsetResolver.FieldOffset(
            typeof(GameItem),
            new string[] { "state" },
            Off.TagSystem,
            apply: !OffsetResolver.IsDistrusted));

    // ─── 以下三个方法保留原签名，内部转发到 Shared ───

    internal static bool IsAnyId(GameItem item, string[] ids) => IdMatch.IsAnyId(item, ids);

    internal static bool WildcardMatch(string pattern, string input) => IdMatch.WildcardMatch(pattern, input);

    internal static string IdOf(GameItem item)
    {
        try
        {
            if (item == null || IdFieldNull(item)) return "<null-id>";
            string id = IdMatch.IdentifierOf(item);
            return string.IsNullOrEmpty(id) ? "<empty-id>" : id;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.IdOf", ex); return "<err-id>"; }
    }

    private static GameItem InventoryOwner(IntPtr invPtr)
    {
        if (invPtr == IntPtr.Zero) return null;
        try { return new GameGridInventory(invPtr).GetParentItem(); }
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.InventoryOwner", ex); return null; }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DIY 特有诊断入口。实际解析逻辑走 Shared 的 OffsetResolver。
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 启动时预热一次（正常路径是懒解析）。这里不打印任何东西 —— 只有在
    /// DiagLog=true 时才会输出完整的偏移解析报告，供排障用。
    /// </summary>
    internal static void WarmUpOffsetDiag()
    {
        // 预热属尽力而为：这里解析失败只代表「未提前缓存」，留到实际使用时再报，
        // 故静默吞掉（预热逻辑本身不改）。
        try
        {
            OffsetResolver.FieldOffset(typeof(GameItem), new string[] { "identifier", "id" }, Off.Id);
        }
        catch { }

        // TagSystem：GameItem 里字段名为 state（0x1B0）。
        // ★ B-2（2026-10-12）：候选集与 apply 策略**必须与 TagSystemFieldNull 完全一致** ——
        //   否则同一字段会被解析成两个缓存键（OffsetResolver 的键含 candidates）并产出
        //   **两条同名 [REG.BP] offset.GameItem.432**，一条 applied=432、一条 applied=解析值，
        //   让回归断言不知道该信哪一条（审查意见 H4）。这里传同样的候选集与同样的 apply
        //   判据，于是预热与运行时命中**同一个缓存项**、只留一条 [REG.BP]。
        try
        {
            OffsetResolver.FieldOffset(typeof(GameItem), new string[] { "state" }, Off.TagSystem,
                apply: !OffsetResolver.IsDistrusted);
        }
        catch { }
    }

    /// <summary>诊断模式（DiagLog=true）下输出完整偏移解析结果，排障用，默认关闭。</summary>
    internal static void LogOffsetDiag()
    {
        LogOffsetDiagOne(typeof(GameItem), new string[] { "identifier", "id" }, Off.Id);
        // B-2：候选集与上面两处实际使用的保持一致（只是描述文字，不写缓存、不写 RegMark）。
        LogOffsetDiagOne(typeof(GameItem), new string[] { "state" }, Off.TagSystem);
    }

    private static void LogOffsetDiagOne(Type type, string[] candidates, int fallback)
    {
        try
        {
            string how = OffsetResolver.DescribeResolution(type, candidates, fallback);
            // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 纯偏移解析诊断输出。
            ContainerSuiteLog.Diag("[容器升级][诊断] " + type.Name + "#" + fallback + "：" + how);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.LogOffsetDiagOne", ex); }
    }
}