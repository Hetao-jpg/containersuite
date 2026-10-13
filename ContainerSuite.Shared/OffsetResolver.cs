// S2 阶段创建。运行时偏移解析，从 DIYUpgrade 的 Utils 提取。
// 设计文档：ContainerSuite S2 §3.6
//
// 与 DIYUpgrade 原版的差异：
//   · 命名空间统一为 ContainerSuite.Shared
//   · 去掉 LogOffsetDiag / WarmUpOffsetDiag / MarkOffsetSuspect / VerifyDragOffset
//     这些依赖具体游戏类型（GameItem / Off 常量）和 DIY 特有语义，留在 DIY 模块
//   · 新增 DescribeResolution 方法，返回一段描述文字（不打印），供调用方打诊断日志
//   · _offsetResolveDistrusted 改成 "标记一次就不再用" 的语义 —— 由 DIY 的 IsID 自校验触发
//   · 加 ClearCache 方法，供测试和场景切换用
//
// 核心逻辑：从 Il2CppInterop 生成的 NativeFieldInfoPtr_<字段> 反查真实偏移。
// Il2CppInterop 生成的字段名格式是自动属性 backing field 形式：
//   identifier  ->  NativeFieldInfoPtr__identifier_k__BackingField
// 所以不能直接拼 "NativeFieldInfoPtr_" + 字段名，必须先规范化。

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ContainerSuite.Shared;

/// <summary>
/// 运行时字段偏移解析器。
/// 
/// 从 Il2CppInterop 生成的 NativeFieldInfoPtr_* 反查真实偏移。
/// 任何一步失败（字段找不到 / 导出函数不存在 / 偏移为 0）都回退到 fallback。
/// 
/// 解析机制一旦被证明不可信（调用方自校验失败后 MarkDistrusted），
/// 就永久回退硬编码。
/// </summary>
public static class OffsetResolver
{
    [DllImport("GameAssembly", EntryPoint = "il2cpp_field_get_offset", CallingConvention = CallingConvention.Cdecl)]
    private static extern uint Il2CppFieldGetOffset(IntPtr field);

    private static readonly Dictionary<string, int> _offsetCache = new Dictionary<string, int>(StringComparer.Ordinal);
    private static bool _distrusted;

    /// <summary>解析机制是否已被标记为不可信。</summary>
    public static bool IsDistrusted => _distrusted;

    /// <summary>
    /// 解析字段偏移。
    /// apply=true（默认）：解析成功时采用解析值；
    /// apply=false：只报告，仍然返回 fallback。
    /// </summary>
    public static int FieldOffset(Type type, string[] candidates, int fallback, bool apply = true)
    {
        if (_distrusted || type == null) return fallback;

        // 缓存键必须含 candidates：同一 (type, fallback) 若配不同候选名，
        //   解析结果可能不同（命中哪个字段不同）。旧键只有 type+fallback，
        //   会把先解析到的结果错误地喂给后来的不同候选集。
        string key = type.FullName + "|" + fallback + "|"
            + (candidates == null ? "" : string.Join(",", candidates));
        if (_offsetCache.TryGetValue(key, out int cached))
            return apply ? cached : fallback;

        int resolved = Resolve(type, candidates, fallback, out string how);

        CacheLimits.Trim(_offsetCache, CacheLimits.Small);   // A1（原裸写 >64）
        _offsetCache[key] = resolved;

        // B2（2026-10-07）：每次「首次解析」（cache miss；同一场景每个 key 一次）
        //   无条件留痕一行 [REG.BP]。这是运行时偏移解析的真实结果，供 _regress
        //   事后断言 / 人工核对 —— 此前只有"解析值与硬编码不一致"才打日志，
        //   "命中且一致"和"未命中回退"两种结果都完全静默（= 查不出偏移挂没挂）。
        //   ★ 只加日志：不改 resolved、不改缓存语义、不改任何调用方行为。
        ContainerSuiteLog.Bp("offset." + type.Name + "." + fallback,
            "resolved=" + resolved + " applied=" + (apply ? resolved : fallback) + " how=" + how);

        // 只在「解析成功但值与硬编码不一致」时打一条 —— 这才是真信号：
        // 说明硬编码过期了、解析救了一次。命中且一致 / 未命中都是常态，不吵。
        // ★ 保持无条件（2026-10-09）：这不是诊断，是「硬编码可能已过期」的告警 ——
        //   游戏更新后偏移一旦漂移，这一行是唯一线索，关掉就等于把故障藏起来。
        // ★ B-2（2026-10-12）：去掉条件里的 `&& apply` —— apply=false 的分支同样要报警。
        //   原写法让"只报告、不采用"的调用方（TagSystemFieldNull）在偏移漂移时**完全静默**，
        //   而那种情形恰恰最需要知道"硬编码已经过期"。文案随 apply 分支变化，
        //   让读日志的人一眼看出这个值到底有没有被采用。
        //   ★ 失信路径（本方法开头 return fallback）走不到这里，所以本告警只覆盖
        //     「机制可信、但调用方选择不采用」这一种情形 —— 不冒充全量覆盖。
        if (resolved != fallback)
        {
            ContainerSuiteLog.Msg("[偏移自检] " + type.Name + "#" + fallback
                + "：" + how + "，真实值 " + resolved
                + (apply ? "，已采用" : "，未采用（apply=false）"));
        }

        return apply ? resolved : fallback;
    }

    /// <summary>
    /// 返回解析结果的描述文字，不打印。
    /// 供调用方打诊断日志用（受调用方自己的 Diag 开关控制）。
    /// </summary>
    public static string DescribeResolution(Type type, string[] candidates, int fallback)
    {
        if (type == null) return "(type == null)";
        try
        {
            Resolve(type, candidates, fallback, out string how);
            return how;
        }
        catch (Exception ex)
        {
            return "解析异常：" + ex.Message;
        }
    }

    /// <summary>清空偏移缓存。</summary>
    public static void ClearCache()
    {
        _offsetCache.Clear();
    }

    /// <summary>
    /// 标记解析机制不可信 —— 之后所有 FieldOffset 直接返回 fallback。
    /// 由调用方的自校验逻辑（如 DIYUpgrade 的 IsID）在发现解析值读不出正确数据时调用。
    /// </summary>
    public static void MarkDistrusted()
    {
        if (_distrusted) return;
        _distrusted = true;
        _offsetCache.Clear();
    }

    /// <summary>测试用：复位 distrust 状态。</summary>
    public static void ResetDistrust()
    {
        _distrusted = false;
        _offsetCache.Clear();
    }

    // ─────────────── 内部实现 ───────────────

    private static int Resolve(Type type, string[] candidates, int fallback, out string how)
    {
        how = "未命中字段(" + string.Join("/", candidates) + ")";
        int resolved = fallback;

        try
        {
            FieldInfo f = FindOffsetField(type, candidates, out string matched);
            if (f == null) return fallback;

            if (f.GetValue(null) is IntPtr fieldInfo && fieldInfo != IntPtr.Zero)
            {
                uint off = Il2CppFieldGetOffset(fieldInfo);
                if (off != 0u && off != 0xFFFFFFFFu)
                {
                    resolved = (int)off;
                    how = "命中字段 " + matched + "，偏移 " + off
                        + (off == (uint)fallback ? "（与硬编码一致）" : "（≠ 硬编码）");
                }
                else how = "字段偏移无效(0)";
            }
            else how = "字段指针未初始化";
        }
        catch
        {
            // 最常见的失败：il2cpp_field_get_offset 这个导出不存在。
            how = "P/Invoke 不可用(il2cpp_field_get_offset)";
            resolved = fallback;
        }

        return resolved;
    }

    /// <summary>
    /// Il2CppInterop 生成的字段名是自动属性 backing field 形式：
    ///   identifier  ->  NativeFieldInfoPtr__identifier_k__BackingField
    /// 所以不能直接拼 "NativeFieldInfoPtr_" + 字段名（实测这样永远命中不了）。
    /// 这里先把名字规范化（去掉 _k__BackingField 与首尾下划线），再精确匹配；
    /// 精确匹配不到才退化为包含匹配。会沿继承链往上找。
    /// </summary>
    private static string NormalizeFieldName(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return raw;
        const string BF = "_k__BackingField";
        string s = raw;
        if (s.EndsWith(BF, StringComparison.Ordinal)) s = s.Substring(0, s.Length - BF.Length);
        s = s.Trim('_').Replace("<", "").Replace(">", "");
        return s;
    }

    private static FieldInfo FindOffsetField(Type type, string[] candidates, out string matchedName)
    {
        matchedName = null;
        const string P = "NativeFieldInfoPtr_";
        FieldInfo loose = null;
        string looseName = null;

        for (Type t = type; t != null && t != typeof(object); t = t.BaseType)
        {
            FieldInfo[] fields;
            try { fields = t.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic); }
            catch { continue; }

            foreach (FieldInfo f in fields)
            {
                if (f.FieldType != typeof(IntPtr)) continue;
                if (!f.Name.StartsWith(P, StringComparison.Ordinal)) continue;
                string norm = NormalizeFieldName(f.Name.Substring(P.Length));

                foreach (string key in candidates)
                {
                    if (string.Equals(norm, key, StringComparison.OrdinalIgnoreCase))
                    {
                        matchedName = f.Name.Substring(P.Length);
                        return f;
                    }
                }
                if (loose == null)
                {
                    foreach (string key in candidates)
                    {
                        if (norm.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            loose = f;
                            looseName = f.Name.Substring(P.Length);
                            break;
                        }
                    }
                }
            }
        }
        if (loose != null) matchedName = looseName;
        return loose;
    }
}