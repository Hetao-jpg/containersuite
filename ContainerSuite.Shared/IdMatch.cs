// 容器 ID 匹配工具。
// 设计文档：ContainerSuite S2 §3.4
//
// 与 DIYUpgrade 原版的差异：
//   · 命名空间统一为 ContainerSuite.Shared
//   · 去掉对 Off.Id 的依赖 —— 不走裸偏移，改走 item.identifier 属性
//   · 不提供 IdFieldNull / TagSystemFieldNull / 诊断版 IdOf
//     这三个留在 DIY 模块，它们依赖 Off 常量和特殊诊断语义
//   · IsAnyId 在异常时返回 false，不抛出
//
// 为什么属性安全：
//   item.identifier 是 C# 属性，getter 走 IL2CPP 托管的 native 桥接。
//   它比裸偏移读更慢，但**永远读对字段**。三 Mod 的使用频率不高
//   （升级 / 快照 / 匹配时才调用），性能差异可以忽略。

using System;
using Il2Cpp;

namespace ContainerSuite.Shared;

/// <summary>容器 ID 匹配工具。</summary>
public static class IdMatch
{
    /// <summary>把原始字符串按 , ， ; ； 切分并 trim。空字符串返回空数组。</summary>
    public static string[] SplitIds(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return Array.Empty<string>();
        string[] array = raw.Split(new char[] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
        for (int i = 0; i < array.Length; i++) array[i] = array[i].Trim();
        return array;
    }

    /// <summary>通配符匹配：支持 * 和 ?。空 pattern 或空 input 返回 false。</summary>
    public static bool WildcardMatch(string pattern, string input)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(input)) return false;
        int i = 0, num = 0, num2 = -1, num3 = 0;
        while (num < input.Length)
        {
            if (i < pattern.Length && (pattern[i] == input[num] || pattern[i] == '?')) { i++; num++; continue; }
            if (i < pattern.Length && pattern[i] == '*') { num2 = i++; num3 = num; continue; }
            if (num2 != -1) { i = num2 + 1; num = ++num3; continue; }
            return false;
        }
        for (; i < pattern.Length && pattern[i] == '*'; i++) { }
        return i == pattern.Length;
    }

    /// <summary>
    /// 安全读取 item.identifier。
    /// 返回原始字符串（已清 \0 并 trim）；读取失败或字段为空时返回 ""。
    /// 不使用 "&lt;null-id&gt;" 这类诊断字符串 —— 那是 DIYUpgrade 模块特有的语义。
    /// </summary>
    public static string IdentifierOf(GameItem item)
    {
        if (item == null) return "";
        try
        {
            string raw = item.identifier;
            if (string.IsNullOrEmpty(raw)) return "";
            return raw.Replace("\0", "").Trim();
        }
        catch { return ""; }
    }

    /// <summary>
    /// item 的 identifier 是否匹配 ids 中任意一项。
    /// 含 * 或 ? 的项走 WildcardMatch，其余走 item.IsID。
    /// 异常时返回 false。
    /// </summary>
    public static bool IsAnyId(GameItem item, string[] ids)
    {
        if (item == null || ids == null || ids.Length == 0) return false;
        try
        {
            string text = IdentifierOf(item);
            if (string.IsNullOrEmpty(text)) return false;

            for (int i = 0; i < ids.Length; i++)
            {
                string pattern = ids[i];
                if (string.IsNullOrEmpty(pattern)) continue;

                if (pattern.IndexOf('*') >= 0 || pattern.IndexOf('?') >= 0)
                {
                    if (WildcardMatch(pattern, text)) return true;
                }
                else
                {
                    try { if (item.IsID(pattern)) return true; }
                    catch { /* item 已释放，跳过这一项 */ }
                }
            }
        }
        catch { }
        return false;
    }

    /// <summary>item 的 identifier 是否等于 id（走 item.IsID）。异常返回 false。</summary>
    public static bool IsId(GameItem item, string id)
    {
        if (item == null || string.IsNullOrEmpty(id)) return false;
        try { return item.IsID(id); } catch { return false; }
    }
}