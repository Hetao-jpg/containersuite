// Fallback 探测的离线故障测试：把**真实 MelonLoader** 跑在沙箱目录里，喂给它非法 TOML，
// 验证 CfgDoctor.DetectFallback / FindSuspectLines / TryBackupBroken 这三条"只在故障时
// 才会走到"的路径 —— 它们原先只能靠故意改坏游戏 cfg + 启动游戏才能验。
//
// 安全性：MelonLoader 的 UserDataDirectory = Path.Combine(LoaderConfig.Current.Loader.BaseDirectory, "UserData")，
// 而进程外的 LoaderConfig 是默认构造（BaseDirectory = null）⇒ 相对路径 "UserData" 落在
// **当前工作目录**。所以本程序先把 cwd 换成临时沙箱，并在动手前断言解析结果确实在沙箱内，
// 否则直接退出 —— 绝不碰真实游戏目录。
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using MelonLoader;
using MelonLoader.Utils;

internal static class Program
{
    private static Type _doctor;
    private static MethodInfo _detect;
    private static MethodInfo _suspects;
    private static MethodInfo _backupBroken;
    private static FieldInfo _fSavingDisabled, _fLines;
    private static string _sandbox;
    private static int _pass, _fail;
    private static bool _diagOnce = true;

    private static void Main()
    {
        _sandbox = Path.Combine(Path.GetTempPath(), "mlsandbox_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_sandbox, "UserData"));
        Directory.CreateDirectory(Path.Combine(_sandbox, "MelonLoader", "Logs"));
        Directory.SetCurrentDirectory(_sandbox);

        // ★ MelonEnvironment 的静态构造要用 LoaderConfig.Current.Loader.BaseDirectory，
        //   进程外它是 null ⇒ Path.Combine(null,…) 直接抛 TypeInitializationException。
        //   所以在碰 MelonEnvironment / MelonPreferences **之前**先把 BaseDirectory 指向沙箱。
        const BindingFlags F0 = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        Type tCfg = typeof(MelonLoader.LoaderConfig);
        object cur = tCfg.GetProperty("Current", F0).GetValue(null);
        object loader = tCfg.GetProperty("Loader", F0).GetValue(cur);
        loader.GetType().GetProperty("BaseDirectory", F0).SetValue(loader, _sandbox);

        // ── 安全闸：解析出来的 UserData 必须在沙箱里 ──
        string ud = Path.GetFullPath(MelonEnvironment.UserDataDirectory);
        Console.WriteLine("沙箱:            " + _sandbox);
        Console.WriteLine("UserData 解析为: " + ud);
        if (!ud.StartsWith(Path.GetFullPath(_sandbox), StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine("!! 解析结果不在沙箱内 —— 拒绝继续，绝不碰真实游戏目录。");
            Environment.Exit(2);
        }

        var asm = Assembly.LoadFrom(FindAssembly());
        _doctor = asm.GetType("ContainerSuite.Shared.CfgDoctor", throwOnError: true);
        const BindingFlags S = BindingFlags.NonPublic | BindingFlags.Static;
        _detect = _doctor.GetMethod("DetectFallback", S);
        _suspects = _doctor.GetMethod("FindSuspectLines", S);
        _backupBroken = _doctor.GetMethod("TryBackupBroken", S);
        _fSavingDisabled = _doctor.GetField("_savingDisabled", S);
        _fLines = _doctor.GetField("_lines", S);

        string cfg = Path.Combine(ud, "MelonPreferences.cfg");

        // ── 前提验证：这些写法到底会不会让 Tomlet 真抛异常（离线可证）──
        Console.WriteLine();
        Console.WriteLine("── 前提：Tomlet 对每种写法的真实反应 ──");
        Parses("A 合法 cfg", cfg, "[CustomInfrastructure]\n2D_SaveBagWidth = 30\nz1_DiagLog = true\n", true);
        Parses("B 引号包数字（合法 TOML，只是类型对不上）", cfg,
            "[CustomInfrastructure]\n2D_SaveBagWidth = \"30\"\n", true);
        Parses("C 裸值（漏引号）", cfg,
            "[CustomInfrastructure]\n2D_SaveBagWidth = 30\n8B_DefaultIdsVeto = rat_cage\n", false);
        Parses("D 未转义的内层双引号", cfg,
            "[CustomInfrastructure]\n2D_SaveBagWidth = \"{ x = \"F8\" }\"\n", false);
        Parses("E 段内重复键", cfg,
            "[CustomInfrastructure]\n2D_SaveBagWidth = 30\n2D_SaveBagWidth = 31\n", false);
        Parses("F 只写键不写值", cfg,
            "[CustomInfrastructure]\n1D_UpgradeHotkey =\n", false);

        // ── 反射管道：WasError / IsSaving 的字段名与属性名是否真的读得到 ──
        //   进程外没法让 MelonLoader 真的进入 Fallback（它的日志走原生 host，抛在置位之前），
        //   但"置位之后我们读不读得到"完全可以离线证 —— 名字/可见性错了这里就会 FAIL。
        Console.WriteLine();
        Console.WriteLine("── 反射管道（DetectFallback 读 WasError / IsSaving）──");
        ReflectionCase(cfg, false, true, false);    // 未失败 + 未禁写盘
        ReflectionCase(cfg, true, false, true);     // 失败 + 禁写盘 → 必须双命中
        ReflectionCase(cfg, false, false, false);   // 未失败但 IsSaving=false（只影响提示文案）

        Suspect("G 行号定位：必须点到第 3 行（裸值那行）", cfg,
            "[CustomInfrastructure]\n# 注释\n8B_DefaultIdsVeto = rat_cage\n2D_SaveBagWidth = 30\n",
            "第 3 行");

        Suspect("H 行号定位：重复键必须点到后一次", cfg,
            "[CustomInfrastructure]\n2D_SaveBagWidth = 30\nz1_DiagLog = true\n2D_SaveBagWidth = 31\n",
            "第 4 行");

        Backup("I Fallback 备份：生成的 *.broken-* 与原文件逐字节一致", cfg);

        Console.WriteLine();
        Console.WriteLine("说明：整份 Fallback 的端到端行为（Error 报错 / 行号 / broken 备份 /");
        Console.WriteLine("      「本次运行不保存」提示）已由 2026-10-14 实机故障测试覆盖，");
        Console.WriteLine("      见 docs/cfg-handedit-rootcause-and-fix.md 第 9 节。");
        Console.WriteLine();
        Console.WriteLine("==== 结果: " + _pass + " PASS / " + _fail + " FAIL");
        try { Directory.Delete(_sandbox, true); } catch { }
        Environment.Exit(_fail == 0 ? 0 : 1);
    }

    /// <summary>直接问 Tomlet：这段内容会不会解析失败。</summary>
    private static void Parses(string title, string cfgPath, string content, bool expectOk)
    {
        try
        {
            File.WriteAllText(cfgPath, content);
            bool ok;
            string msg;
            try
            {
                Tomlet.TomlParser.ParseFile(cfgPath);
                ok = true;
                msg = "解析通过";
            }
            catch (Exception ex)
            {
                ok = false;
                msg = "抛 " + ex.GetType().Name;
            }
            Report(ok == expectOk, title, (expectOk ? "期望通过" : "期望失败") + " → " + msg);
        }
        catch (Exception ex)
        {
            Report(false, title, "harness 异常 " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    /// <summary>把 WasError / IsSaving 置成指定值，看 DetectFallback 是否如实回报。</summary>
    private static void ReflectionCase(string cfgPath, bool wasError, bool isSaving,
        bool expectFallback)
    {
        string title = "WasError=" + wasError + " IsSaving=" + isSaving
                     + " → 应报 Fallback=" + expectFallback;
        try
        {
            File.WriteAllText(cfgPath, "[CustomInfrastructure]\n2D_SaveBagWidth = 30\n");
            const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
            object file = typeof(MelonPreferences).GetField("DefaultFile", F).GetValue(null);
            // ★ 直接写私有字段 _waserror，不走属性 setter —— setter 里会先
            //   MelonLogger.Warning("Defaulting ... to Fallback ...")，进程外必然 NRE。
            //   被测代码 DetectFallback 读的是属性 getter，而 getter 返回的就是这个字段。
            file.GetType().GetField("_waserror", F | BindingFlags.Instance).SetValue(file, wasError);
            SetBool(file.GetType(), file, "IsSaving", isSaving);
            _fSavingDisabled.SetValue(null, false);

            object[] args = { null };
            bool got = (bool)_detect.Invoke(null, args);
            // ★ DetectFallback(out bool savingDisabled) 是通过 **out 参数**回传的，
            //   静态字段 _savingDisabled 只在 Preflight 里被赋值。直接反射调用它时
            //   必须读 args[0]，读静态字段永远是 false（这里曾误判过一次）。
            bool gotSavingDisabled = (bool)args[0];

            // IsSaving=false 时 savingDisabled 必须为 true（用于日志里那句"已确认关闭写盘"）
            bool ok = got == expectFallback && (!isSaving ? gotSavingDisabled : true);
            Report(ok, title, "DetectFallback=" + got + " savingDisabled=" + gotSavingDisabled);
        }
        catch (Exception ex)
        {
            Report(false, title, "异常 " + ex.GetType().Name + ": " + ex.Message);
            if (_diagOnce) { _diagOnce = false; Console.WriteLine(ex.ToString()); }
        }
    }

    private static string FindAssembly()
    {
        string dir = AppContext.BaseDirectory;
        string p = Path.Combine(dir, "CustomInfrastructure.dll");
        if (File.Exists(p)) return p;
        throw new FileNotFoundException("找不到 CustomInfrastructure.dll（先 dotnet build -c Release）");
    }

    /// <summary>FindSuspectLines 必须点名出问题那一行。</summary>
    private static void Suspect(string title, string cfgPath, string content, string mustContain)
    {
        try
        {
            File.WriteAllText(cfgPath, content);
            // FindSuspectLines 只看行数组，不需要走 MelonLoader
            var lines = content.Split('\n');
            var hits = (List<string>)_suspects.Invoke(null, new object[] { lines, 8 });
            string all = string.Join(" | ", hits.ToArray());
            Report(hits.Count > 0 && all.Contains(mustContain), title,
                hits.Count == 0 ? "(一条都没报)" : all);
        }
        catch (Exception ex)
        {
            Report(false, title, "异常 " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    private static void Backup(string title, string cfgPath)
    {
        try
        {
            File.WriteAllText(cfgPath, "[CustomInfrastructure]\n8B_DefaultIdsVeto = rat_cage\n");
            string bak = (string)_backupBroken.Invoke(null, new object[] { cfgPath });
            bool ok = !string.IsNullOrEmpty(bak) && File.Exists(bak)
                   && File.ReadAllText(bak) == File.ReadAllText(cfgPath)
                   && Path.GetFileName(bak).Contains(".broken-");
            Report(ok, title, bak == null ? "(没生成)" : Path.GetFileName(bak));
        }
        catch (Exception ex)
        {
            Report(false, title, "异常 " + ex.GetType().Name + ": " + ex.Message);
        }
    }

    /// <summary>MelonLoader 的 WasError 一旦置位就会跳过后续 Load，用例之间必须清干净。</summary>
    private static void ResetErrorState()
    {
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        object file = typeof(MelonPreferences).GetField("DefaultFile", F).GetValue(null);
        SetBool(file.GetType(), file, "WasError", false);
        SetBool(file.GetType(), file, "IsSaving", true);   // IsSaving 是字段，不是属性
        _fSavingDisabled.SetValue(null, false);
    }

    /// <summary>先属性后字段地写一个 bool（MelonLoader 里这两个成员一个是属性一个是字段）。</summary>
    private static void SetBool(Type t, object obj, string name, bool value)
    {
        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        PropertyInfo p = t.GetProperty(name, F);
        if (p != null && p.CanWrite) { p.SetValue(obj, value); return; }
        FieldInfo fi = t.GetField(name, F);
        fi?.SetValue(obj, value);
    }

    private static void Report(bool ok, string title, string detail)
    {
        if (ok) { _pass++; Console.WriteLine("PASS  " + title); }
        else { _fail++; Console.WriteLine("FAIL  " + title); }
        Console.WriteLine("        " + detail);
    }
}
