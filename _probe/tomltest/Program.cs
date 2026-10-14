// 复现 MelonLoader 的 cfg 读取口径（Tomlet），逐条验证「手改的值能不能被读回来」。
// 口径完全对照 MelonLoader 反编译源码：
//   File.Load()                -> TomlParser.ParseFile(path)
//   File.SetupEntryFromRawValue-> document.GetSubTable(cat).GetValue(QuoteKey(key)) -> entry.Load(v)
//   MelonPreferences_Entry<T>.Load -> TomletMain.To<T>(v)  ，异常（TomlTypeMismatch/NoSuchValue）被吞
using System;
using System.IO;
using Tomlet;
using Tomlet.Models;

internal static class Program
{
    private static string QuoteKey(string key) =>
        !key.Contains('"') ? "\"" + key + "\"" : "'" + key + "'";

    // 完全复刻 MelonPreferences_Entry<T>.Load 的行为：异常=静默回落默认值
    private static string ReadAs<T>(TomlDocument doc, string cat, string key, T def)
    {
        try
        {
            var sub = doc.GetSubTable(cat);
            var v = sub.GetValue(QuoteKey(key));
            return "读到 " + TomletMain.To<T>(v);
        }
        catch (Exception ex)
        {
            return "!! 静默回落默认值 " + def + "  <" + ex.GetType().Name + ">";
        }
    }

    private static void Case(string title, string body, params (string key, string kind)[] probes)
    {
        Console.WriteLine("\n=== " + title + " ===");
        string path = Path.Combine(Path.GetTempPath(), "tomltest_" + Guid.NewGuid().ToString("N") + ".cfg");
        File.WriteAllText(path, body);
        TomlDocument doc = null;
        try { doc = TomlParser.ParseFile(path); }
        catch (Exception ex) { Console.WriteLine("  ★整份解析失败: " + ex.GetType().Name + " " + ex.Message); }

        if (doc == null) { Console.WriteLine("  -> 文档为空：MelonLoader 会把 WasError 置位，所有条目回落默认值"); return; }

        foreach (var (key, kind) in probes)
        {
            string r = kind switch
            {
                "int" => ReadAs<int>(doc, "CustomInfrastructure", key, 24),
                "bool" => ReadAs<bool>(doc, "CustomInfrastructure", key, false),
                "str" => ReadAs<string>(doc, "CustomInfrastructure", key, "(空)"),
                "float" => ReadAs<float>(doc, "CustomInfrastructure", key, 1.0f),
                _ => "?"
            };
            Console.WriteLine("  " + key.PadRight(28) + " 期望" + kind.PadRight(6) + " " + r);
        }

        // 原样回写（MelonPreferences Save 的落盘动作），看手改内容是否被保留
        File.WriteAllText(path + ".out", doc.SerializedValue);
        Console.WriteLine("  -- 回写后仍包含 key 的行：" + (File.ReadAllText(path + ".out").Contains(probes.Length > 0 ? probes[0].key : "x") ? "是" : "否"));
    }

    private static void Main()
    {
        Console.WriteLine("Tomlet " + typeof(TomlDocument).Assembly.GetName().Version);

        Case("A 正常：类型正确（基准）",
@"[CustomInfrastructure]
2D_SaveBagWidth = 30
z1_DiagLog = true
1D_UpgradeHotkey = ""Ctrl+E""
8A_PerIdSizeLimits = ""storage_bay=30x20, evidence_box=24x24""
",
            ("2D_SaveBagWidth", "int"), ("z1_DiagLog", "bool"),
            ("1D_UpgradeHotkey", "str"), ("8A_PerIdSizeLimits", "str"));

        Case("B 字符串给数字键加了引号",
@"[CustomInfrastructure]
2D_SaveBagWidth = ""30""
",
            ("2D_SaveBagWidth", "int"));

        Case("C 数字键写成小数",
@"[CustomInfrastructure]
2D_SaveBagWidth = 30.0
",
            ("2D_SaveBagWidth", "int"));

        Case("D 开关写成 1/0（或 yes/no）",
@"[CustomInfrastructure]
z1_DiagLog = 1
",
            ("z1_DiagLog", "bool"));

        Case("E 字符串键漏了引号（含逗号）",
@"[CustomInfrastructure]
8A_PerIdSizeLimits = storage_bay=30x20, evidence_box=24x24
",
            ("8A_PerIdSizeLimits", "str"));

        Case("F 字符串键漏了引号（无逗号，纯单词）",
@"[CustomInfrastructure]
1D_UpgradeHotkey = LeftAlt
",
            ("1D_UpgradeHotkey", "str"));

        Case("G 段名大小写不一致",
@"[custominfrastructure]
2D_SaveBagWidth = 30
",
            ("2D_SaveBagWidth", "int"));

        Case("H 键名大小写不一致",
@"[CustomInfrastructure]
2d_savebagwidth = 30
",
            ("2D_SaveBagWidth", "int"));

        Case("I 值后面跟行内中文注释",
@"[CustomInfrastructure]
2D_SaveBagWidth = 30 # 我改成 30
",
            ("2D_SaveBagWidth", "int"));

        Case("J 全角逗号/中文标点留在字符串里",
@"[CustomInfrastructure]
8A_PerIdSizeLimits = ""storage_bay=30x20，evidence_box=24x24""
",
            ("8A_PerIdSizeLimits", "str"));

        Case("K 同一段里同一个键写了两遍",
@"[CustomInfrastructure]
2D_SaveBagWidth = 30
2D_SaveBagWidth = 40
",
            ("2D_SaveBagWidth", "int"));

        Case("L 键写在了别的段（旧段名）",
@"[SaveBagExpand]
2A_BackInvWidth = 30
[CustomInfrastructure]
2A_BackInvWidth = 18
",
            ("2A_BackInvWidth", "int"));

        Case("M 值里有未转义的内部双引号（README 点名的非法 TOML）",
"[CustomInfrastructure]\n2D_SaveBagWidth = \"{ x = \"F8\" }\"\n",
            ("2D_SaveBagWidth", "int"));

        Case("N 单引号包裹的字符串（TOML 字面量字符串）",
@"[CustomInfrastructure]
1D_UpgradeHotkey = 'Ctrl+E'
",
            ("1D_UpgradeHotkey", "str"));

        Case("O 值为空 / 只写键不写值",
@"[CustomInfrastructure]
1D_UpgradeHotkey =
",
            ("1D_UpgradeHotkey", "str"));
    }
}
