// 反射类型查找缓存。
// 设计文档：ContainerSuite S2 §3.3
//
// 与 DIYUpgrade 原版的差异：
//   · 命名空间统一为 ContainerSuite.Shared
//   · 改成 public static class TypeCache，对外暴露 FindTypeCached / Clear
//   · 内部字典用锁保护（虽然 Mod 主线程单线程，但保险起见）
//   · 找不到时缓存 null，避免重复扫描

using System;
using System.Collections.Generic;
using System.Reflection;

namespace ContainerSuite.Shared;

/// <summary>
/// 反射类型查找缓存。首次调用扫全程序集，结果缓存。
/// 
/// 跳过以下程序集（避免扫描无用或干扰类型）：
///   UnityEngine.* / Unity.* / System.* / Microsoft.* / Mono.*
///   mscorlib / netstandard / Il2Cppmscorlib
///   MelonLoader / 0Harmony / Il2CppInterop.*
/// </summary>
public static class TypeCache
{
    private static readonly Dictionary<string, Type> _cache = new Dictionary<string, Type>(StringComparer.Ordinal);
    private static readonly object _lock = new object();

    /// <summary>按类型名查找类型，结果缓存。找不到返回 null。</summary>
    public static Type FindTypeCached(string typeName)
    {
        if (string.IsNullOrEmpty(typeName)) return null;

        lock (_lock)
        {
            if (_cache.TryGetValue(typeName, out var cached)) return cached;
        }

        Type result = FindTypeSlow(typeName);

        lock (_lock)
        {
            // 缓存 null 也是有效值 —— 避免反复扫描
            _cache[typeName] = result;
        }
        return result;
    }

    /// <summary>清空缓存。</summary>
    public static void Clear()
    {
        lock (_lock) { _cache.Clear(); }
    }

    private static Type FindTypeSlow(string typeName)
    {
        try
        {
            var asms = AppDomain.CurrentDomain.GetAssemblies();
            for (int i = 0; i < asms.Length; i++)
            {
                Assembly asm = asms[i];
                string an = null;
                try { an = asm.GetName().Name; } catch { continue; }
                if (string.IsNullOrEmpty(an)) continue;

                if (ShouldSkipAssembly(an)) continue;

                try
                {
                    Type t = asm.GetType(typeName, false, false);
                    if (t != null) return t;
                }
                catch { }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("TypeCache.FindTypeSlow", ex); }

        return null;
    }

    private static bool ShouldSkipAssembly(string name)
    {
        if (name.StartsWith("UnityEngine.", StringComparison.Ordinal)) return true;
        if (name.StartsWith("Unity.", StringComparison.Ordinal)) return true;
        if (name.StartsWith("System.", StringComparison.Ordinal)) return true;
        if (name.StartsWith("Microsoft.", StringComparison.Ordinal)) return true;
        if (name.StartsWith("Mono.", StringComparison.Ordinal)) return true;

        switch (name)
        {
            case "mscorlib":
            case "netstandard":
            case "Il2Cppmscorlib":
            case "MelonLoader":
            case "0Harmony":
            case "Il2CppInterop.Runtime":
            case "Il2CppInterop.HarmonySupport":
            case "Il2CppInterop.Common":
                return true;
            default:
                return false;
        }
    }
}