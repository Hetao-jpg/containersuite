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

    private static void LogOnce(string msg)
    {
        if (string.IsNullOrEmpty(msg)) return;
        DateTime utcNow = DateTime.UtcNow;
        if (!_lastLogTimeByKey.TryGetValue(msg, out var lastTime) || !((utcNow - lastTime).TotalSeconds < 3.0))
        {
            CacheLimits.Trim(_lastLogTimeByKey, CacheLimits.Small);   // A1（原裸写 >32）
            _lastLogTimeByKey[msg] = utcNow;
            ContainerSuiteLog.Diag(msg);
        }
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
    // 保持 apply: false —— 只报告，不采用。
    private static bool TagSystemFieldNull(GameItem item) =>
        PtrFieldNull(item, OffsetResolver.FieldOffset(
            typeof(GameItem),
            new string[] { "state", "tagSystem" },
            Off.TagSystem,
            apply: false));

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

        // TagSystem：GameItem 里字段名为 state。它没有像 Id 那样的自校验手段，
        // 所以只报告、不采用 —— 避免解析错导致整片容器静默失效。
        try
        {
            OffsetResolver.FieldOffset(typeof(GameItem), new string[] { "state", "tagSystem" }, Off.TagSystem, apply: false);
        }
        catch { }
    }

    /// <summary>诊断模式（DiagLog=true）下输出完整偏移解析结果，排障用，默认关闭。</summary>
    internal static void LogOffsetDiag()
    {
        LogOffsetDiagOne(typeof(GameItem), new string[] { "identifier", "id" }, Off.Id);
        LogOffsetDiagOne(typeof(GameItem), new string[] { "state", "tagSystem" }, Off.TagSystem);
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