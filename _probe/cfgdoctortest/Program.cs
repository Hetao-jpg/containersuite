// CfgDoctor 离线单测：直接反射调用**已编译的** CustomInfrastructure.dll 里的
// ContainerSuite.Shared.CfgDoctor 私有方法，验证「cfg 原文 → 宽容采纳」这条链。
// 目的：把"只能进游戏才知道"缩小到"只剩 MelonLoader 集成部分需要实机"。
// 跑法：dotnet run（见同目录 README）
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

internal static class Program
{
    private static Type _doctor;
    private static MethodInfo _snapshot;
    private static FieldInfo _fLines, _fRaw, _fSectionExisted, _fSectionWrong;
    private static MethodInfo _tryLenient;

    private static int _pass, _fail;

    private static void Main()
    {
        var asmPath = FindAssembly();
        Console.WriteLine("被测程序集: " + asmPath);
        Assembly asm = Assembly.LoadFrom(asmPath);
        _doctor = asm.GetType("ContainerSuite.Shared.CfgDoctor", throwOnError: true);

        const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;
        _snapshot = _doctor.GetMethod("SnapshotFile", S);
        _fLines = _doctor.GetField("_lines", S);
        _fRaw = _doctor.GetField("_rawByKey", S);
        _fSectionExisted = _doctor.GetField("_sectionExisted", S);
        _fSectionWrong = _doctor.GetField("_sectionSpellingWrong", S);
        _tryLenient = _doctor.GetMethod("TryLenient", S);

        // ── 用例：每个用例一份临时 cfg ──
        Case("A 正常值原样读出", "[CustomInfrastructure]\n2D_SaveBagWidth = 30\n", "2D_SaveBagWidth", typeof(int), "30");
        Case("B 数字被引号包住", "[CustomInfrastructure]\n2D_SaveBagWidth = \"30\"\n", "2D_SaveBagWidth", typeof(int), "30");
        Case("C 数字写成小数", "[CustomInfrastructure]\n2D_SaveBagWidth = 30.0\n", "2D_SaveBagWidth", typeof(int), "30");
        Case("D 值后跟行内注释", "[CustomInfrastructure]\n2D_SaveBagWidth = 30 # 我改成 30\n", "2D_SaveBagWidth", typeof(int), "30");
        Case("E 开关写成 1", "[CustomInfrastructure]\nz1_DiagLog = 1\n", "z1_DiagLog", typeof(bool), "true");
        Case("F 开关写中文「开」", "[CustomInfrastructure]\nz1_DiagLog = 开\n", "z1_DiagLog", typeof(bool), "true");
        Case("G 开关被引号包住", "[CustomInfrastructure]\nz1_DiagLog = \"true\"\n", "z1_DiagLog", typeof(bool), "true");
        Case("H 键名大小写不符（应能忽略大小写找到）", "[CustomInfrastructure]\n2d_savebagwidth = 25\n", "2D_SaveBagWidth", typeof(int), "25");
        Case("I 段名大小写不符（应能找到该段并标记）", "[custominfrastructure]\n2D_SaveBagWidth = 27\n", "2D_SaveBagWidth", typeof(int), "27", expectWrongSection: true);
        Case("J 字符串裸值（漏引号）", "[CustomInfrastructure]\n1D_UpgradeHotkey = LeftAlt\n", "1D_UpgradeHotkey", typeof(string), "LeftAlt");
        Case("K 字符串带引号", "[CustomInfrastructure]\n1D_UpgradeHotkey = \"Ctrl+E\"\n", "1D_UpgradeHotkey", typeof(string), "Ctrl+E");
        Case("L 字符串带转义引号", "[CustomInfrastructure]\n8A_PerIdSizeLimits = \"a=\\\"x\\\"\"\n", "8A_PerIdSizeLimits", typeof(string), "a=\"x\"");
        Case("M 全角逗号留在字符串里", "[CustomInfrastructure]\n8A_PerIdSizeLimits = \"a=1x1，b=2x2\"\n", "8A_PerIdSizeLimits", typeof(string), "a=1x1，b=2x2");
        Case("N float 正常", "[CustomInfrastructure]\n3B_SaveBagPagingHintDuration = 1.5\n", "3B_SaveBagPagingHintDuration", typeof(float), "1.5");
        Case("O 引号内的 # 不算注释", "[CustomInfrastructure]\n1D_UpgradeHotkey = \"Ctrl+#\"\n", "1D_UpgradeHotkey", typeof(string), "Ctrl+#");
        Case("P 负数与空格", "[CustomInfrastructure]\n2G_GroundOffsetX =   -48 \n", "2G_GroundOffsetX", typeof(int), "-48");
        Case("Q 中文值给 int 键 → 必须读不出（绝不猜）", "[CustomInfrastructure]\n2D_SaveBagWidth = 三十\n", "2D_SaveBagWidth", typeof(int), null);
        Case("R 小数非整数给 int 键 → 必须读不出", "[CustomInfrastructure]\n2D_SaveBagWidth = 30.5\n", "2D_SaveBagWidth", typeof(int), null);
        Case("S 段不存在 → 无快照键", "[Whatever]\n2D_SaveBagWidth = 30\n", "2D_SaveBagWidth", typeof(int), null, expectNoKey: true);
        Case("T 重复键 → 取第一条", "[CustomInfrastructure]\n2D_SaveBagWidth = 31\n2D_SaveBagWidth = 32\n", "2D_SaveBagWidth", typeof(int), "31");
        Case("U 键前后有空格与引号", "[CustomInfrastructure]\n\"2D_SaveBagWidth\" = 33\n", "2D_SaveBagWidth", typeof(int), "33");
        Case("V 值前后空白（含制表）", "[CustomInfrastructure]\nz1_DiagLog =\t true \t\n", "z1_DiagLog", typeof(bool), "true");

        // ── 注释比对保真度：SaveIfNeeded 的"注释没变就跳过写盘"全靠这条 ──
        //   做法：用 Tomlet **真的写一遍**（模拟 MelonLoader 的落盘），再用 CfgDoctor
        //   的 CommentAbove 从原文读回来，看能不能逐字还原 Description。
        Console.WriteLine();
        Console.WriteLine("── 注释比对保真度（Tomlet 写 → CommentAbove 读回）──");
        CommentRoundTrip("单行注释", "是否扩容垃圾桶。false=保持原版尺寸。");
        CommentRoundTrip("多行 + 空行 + 全角", "每个容器单独设升级上限，格式：容器id=宽x高。\n\n★ 下表 = 当前生效的上限：\n  sec_box=50x30（治安部物资箱）");
        CommentRoundTrip("空注释（应读回空串）", "");

        Console.WriteLine();
        Console.WriteLine("==== 结果: " + _pass + " PASS / " + _fail + " FAIL");
        Environment.Exit(_fail == 0 ? 0 : 1);
    }

    /// <summary>Tomlet 写盘 → SnapshotFile → CommentAbove，必须还原出原始 Description。</summary>
    private static void CommentRoundTrip(string title, string desc)
    {
        const string key = "2D_SaveBagWidth";
        string path = Path.Combine(Path.GetTempPath(), "cfgdocc_" + Guid.NewGuid().ToString("N") + ".cfg");
        try
        {
            // ① 照 MelonLoader 的写法造文档：往段里放两个键，第一个键带注释，第二个键带被test的注释。
            var doc = Tomlet.Models.TomlDocument.CreateEmpty();
            doc.PutValue("CustomInfrastructure", new Tomlet.Models.TomlTable());
            var table = doc.GetSubTable("CustomInfrastructure");

            var other = Tomlet.TomletMain.ValueFrom(1);
            other.Comments.PrecedingComment = "另一个键的注释（不应被读到）";
            table.PutValue("\"3A_SaveBagPageCount\"", other);

            var v = Tomlet.TomletMain.ValueFrom(30);
            if (!string.IsNullOrEmpty(desc)) v.Comments.PrecedingComment = desc;
            table.PutValue("\"" + key + "\"", v);   // 与 File.InsertIntoDocument 的 QuoteKey 一致

            File.WriteAllText(path, doc.SerializedValue);

            // ② 用被测代码读回来
            ResetStatics();
            _snapshot.Invoke(null, new object[] { path });

            var raw = _fRaw.GetValue(null) as IDictionary;
            if (raw == null || !raw.Contains(key)) { Fail(title, desc, "(快照里找不到键)"); return; }

            object line = raw[key];
            int lineIdx = (int)line.GetType()
                .GetField("Line", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .GetValue(line);

            string got = (string)GetMethod("CommentAbove").Invoke(null, new object[] { lineIdx });

            string expect = desc ?? "";
            if (expect == got) { _pass++; Console.WriteLine("PASS  " + title); }
            else Fail(title, expect, got);
        }
        catch (Exception ex)
        {
            Fail(title, desc, "异常 " + ex.GetType().Name + ": " + ex.Message);
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }
    }

    private static MethodInfo GetMethod(string name)
        => _doctor.GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static);

    private static void Fail(string title, string expect, string got)
    {
        _fail++;
        Console.WriteLine("FAIL  " + title);
        Console.WriteLine("        期望 " + Show(expect));
        Console.WriteLine("        实得 " + Show(got));
    }

    private static string Show(string s)
    {
        if (s == null) return "(null)";
        return "\"" + s.Replace("\n", "\\n") + "\"";
    }

    private static string FindAssembly()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8 && dir != null; i++)
        {
            string p = Path.Combine(dir, "CustomInfrastructure.dll");
            if (File.Exists(p)) return p;
            string p2 = Path.Combine(dir, "ContainerSuite.Infrastructure", "bin", "Release", "CustomInfrastructure.dll");
            if (File.Exists(p2)) return p2;
            dir = Path.GetDirectoryName(dir);
        }
        throw new FileNotFoundException("找不到 CustomInfrastructure.dll：先 dotnet build -c Release");
    }

    private static void Case(string title, string cfg, string key, Type type,
        string expect, bool expectNoKey = false, bool expectWrongSection = false)
    {
        string path = Path.Combine(Path.GetTempPath(), "cfgdoctor_" + Guid.NewGuid().ToString("N") + ".cfg");
        File.WriteAllText(path, cfg);

        string got;
        string note = "";
        try
        {
            ResetStatics();
            _snapshot.Invoke(null, new object[] { path });

            var raw = _fRaw.GetValue(null) as IDictionary;
            bool wrong = (string)_fSectionWrong.GetValue(null) != null;

            if (expectWrongSection && !wrong) note += "[应标记段名不符但未标记] ";
            if (!expectWrongSection && wrong) note += "[不该标记段名不符却标记了] ";

            if (raw == null || !raw.Contains(key))
            {
                got = null;
                if (!expectNoKey) note += "[快照里找不到键] ";
            }
            else
            {
                object line = raw[key];
                string rawValue = (string)line.GetType()
                    .GetField("RawValue", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    .GetValue(line);

                object[] args = { rawValue, type, null, null };
                bool ok = (bool)_tryLenient.Invoke(null, args);
                got = ok ? Str(args[2], type) : null;
                if (!ok)
                {
                    // 宽容解析失败时，ApplyTo 的语义是"保持代码默认值" → 期望 null
                }
            }
        }
        finally
        {
            try { File.Delete(path); } catch { }
        }

        bool pass = expectNoKey ? (got == null && note.Length == 0)
                                : (expect == got && note.Length == 0);
        if (pass) _pass++; else _fail++;
        Console.WriteLine((pass ? "PASS  " : "FAIL  ") + title);
        if (!pass)
            Console.WriteLine("        期望 " + (expect ?? "(读不出)") + " / 实得 " + (got ?? "(读不出)") + " " + note);
    }

    /// <summary>CfgDoctor 全是静态状态（进程内只跑一次），用例之间必须清干净。</summary>
    private static void ResetStatics()
    {
        const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;
        Set("_lines", null); Set("_rawByKey", null);
        Set("_sectionExisted", false); Set("_preflightDone", false);
        Set("_sectionSpellingWrong", null);
        Set("_fallback", false); Set("_savingDisabled", false);
        Set("_applied", 0); Set("_unreadable", 0); Set("_checked", 0);
        Set("_saved", 0); Set("_skipped", 0);
        Set("_dirty", false); Set("_dirtyReason", null);
        foreach (string list in new[] { "_appliedNotes", "_unreadableNotes", "_counted" })
        {
            FieldInfo f = _doctor.GetField(list, S);
            object v = f.GetValue(null);
            if (v != null) v.GetType().GetMethod("Clear").Invoke(v, null);
        }
    }

    private static void Set(string name, object value)
    {
        FieldInfo f = _doctor.GetField(name, BindingFlags.NonPublic | BindingFlags.Static);
        if (f != null) f.SetValue(null, value);
    }

    private static string Str(object v, Type t)
    {
        if (v == null) return null;
        if (t == typeof(float)) return ((float)v).ToString("0.####", CultureInfo.InvariantCulture);
        if (t == typeof(double)) return ((double)v).ToString("0.####", CultureInfo.InvariantCulture);
        if (t == typeof(bool)) return ((bool)v) ? "true" : "false";
        return v.ToString();
    }
}
