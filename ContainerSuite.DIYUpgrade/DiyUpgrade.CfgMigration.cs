// CFG 迁移 —— 基座（迁移链说明 + 各套迁移共用的读写工具）。
// 这里只放 4 套迁移共用的工具；各套迁移按主题拆在同目录 3 个 partial 文件。
// 背景 / 迁移链顺序 / 终止线 1.7.4 / "三处摘" / 静默失效防线：见 docs/cfg-migration.md
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ContainerSuite.Shared;
using MelonLoader;
using MelonLoader.Utils;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    private static string CfgPath()
    {
        try { return Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg"); }
        catch { return null; }
    }

    /// <summary>旧键名 → 新键名（键名重排后）；不在重排表里就原样返回。</summary>
    private static string NewKeyOf(string oldKey)
    {
        if (string.IsNullOrEmpty(oldKey)) return oldKey;
        return KeyRenames.TryGetValue(oldKey, out string renamed) ? renamed : oldKey;
    }

    private static MelonPreferences_Entry FindEntry(MelonPreferences_Category cat, string id)
    {
        if (cat == null || cat.Entries == null) return null;
        foreach (MelonPreferences_Entry e in cat.Entries)
            if (e != null && string.Equals(e.Identifier, id, StringComparison.Ordinal))
                return e;
        return null;
    }

    private static string StripQuotes(string s)
    {
        if (s == null) return "0";
        s = s.Trim();
        if (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"') s = s.Substring(1, s.Length - 2);
        return s;
    }

    private static Dictionary<string, string> ReadSectionValues(string path, string section)
    {
        var result = new Dictionary<string, string>(StringComparer.Ordinal);
        bool inSection = false;
        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith("#") || line.StartsWith(";")) continue;

            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                inSection = line.Substring(1, line.Length - 2).Trim()
                    .Equals(section, StringComparison.Ordinal);
                continue;
            }
            if (!inSection) continue;

            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string key = line.Substring(0, eq).Trim().Trim('"');
            result[key] = line.Substring(eq + 1).Trim();
        }
        return result;
    }

    /// <summary>把 cfg 里的原始字符串按条目真实类型写回去。</summary>
    private static bool ApplyRawValue(MelonPreferences_Entry entry, string raw)
    {
        if (entry == null || raw == null) return false;
        raw = raw.Trim();
        if (raw.Length >= 2 && raw[0] == '"' && raw[raw.Length - 1] == '"')
            raw = raw.Substring(1, raw.Length - 2);

        try
        {
            PropertyInfo prop = entry.GetType().GetProperty("Value");
            if (prop == null || !prop.CanWrite) return false;

            Type t = prop.PropertyType;
            object value;
            if (t == typeof(bool)) { if (!bool.TryParse(raw, out bool b)) return false; value = b; }
            else if (t == typeof(int)) { if (!int.TryParse(raw, out int i)) return false; value = i; }
            else if (t == typeof(float)) { if (!float.TryParse(raw, out float f)) return false; value = f; }
            else if (t == typeof(double)) { if (!double.TryParse(raw, out double d)) return false; value = d; }
            else if (t == typeof(string)) { value = raw; }
            else return false;

            prop.SetValue(entry, value);
            return true;
        }
        catch { return false; }
    }

    /// <summary>
    /// cfg 里某个段是否已经有某个键（用于判断"这台机器以前装过对应 mod"）。
    /// <para>
    /// ★ 终止线（1.7.4）之后只查**当前段** [CustomInfrastructure]：早于 1.7.4 的三段
    ///   （[自定义容器升级]/[DIYUpgrade]/[SaveBagExpand]）已不再迁移，无需再查。
    /// </para>
    /// </summary>
    internal static bool CfgSectionHasKey(string section, string key)
    {
        try
        {
            string cfgPath = CfgPath();
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
                return ReadSectionValues(cfgPath, section).ContainsKey(key);
        }
        catch (Exception ex) { CatchLogOnce.Warn("CfgMigration:cfg-section-has-key-file", ex); }

        return false;
    }
}