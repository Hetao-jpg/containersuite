// ═══════════════════════════════════════════════════════════════════════════
// CfgSelfCheck —— MelonPreferences.cfg 启动期 TOML 终态合法性自检（只读探针）
//
// ── 2026-10-10：本类由原 CfgKeepAlive 瘦身而来，保活部分已整体删除 ──────────
//   原类还带一个「cfg 保活」：每 1.5s 轮询 cfg 的 mtime → 发现外部改动就采纳
//   磁盘值进运行时 entry → 立即 MelonPreferences.Save() 回写。经复核该机制
//   两头都不构成有效增益，故删除：
//     · cfg 有非法 TOML（Fallback）时：File.Save() 自己带
//       `if (!_waserror && ShouldSave) return` 守卫，_waserror=true ⇒ 保活那次
//       MelonPreferences.Save() 同样被静默 return，救不了"退出丢失"；
//     · cfg 没有非法 TOML 时：MelonLoader 自带 FileWatcher
//       （FileWatcher → LoadFileAndRefreshCategories）已经会采纳外部改动。
//   它反而带来"额外的全量落盘"，会把 MelonLoader 内存 document 里残留的
//   孤儿键一起写回文件（document 是启动时 ParseFile 的整份快照，Save 写的是
//   document.SerializedValue，见 MelonLoader Preferences/IO/File.cs）。
//   本类只做"看"，不做"改"。
//
// ── 自检要解决的问题 ──────────────────────────────────────────────────────
//   非法 TOML 会让 MelonLoader 启动 Load() 时 TomlParser.ParseFile 抛异常 →
//   整份 cfg 进入 Fallback（WasError）→ 此后**所有 mod**的 Save() 全部静默
//   return、FileWatcher 被 Destroy，玩家在 ModManager 里改的配置退出后丢失。
//   MelonLoader 对此只在启动日志打一行 ERROR，极易被淹没。本探针主动扫一遍，
//   把"第几行、什么内容、疑似谁写的"打在启动日志里。
//
// ── 为什么延迟到 init 之后才查（DeferCheckSeconds）────────────────────────
//   Tomlet 写盘时非 ASCII 段名走裸形式（[良心商人] / [DIY捕鼠夹]），而这两个
//   中文段由各自的 mod 在**自身初始化时**改写为带引号形式。若在本套件 init 时
//   立刻检查，看到的必然是上一会话遗留的裸段名 → 每次假报警。延迟到所有 mod
//   都初始化完之后再查，看到的才是玩家真正关心的「终态」。
//
// ── 什么算错误（会让 Tomlet 抛异常）───────────────────────────────────────
//   ① 值里内层双引号未转义（例 `A = "{ x = "F8" }"`）→ TomlMissingNewlineException
//   ② 裸值含逗号（例 `A = a,b,c`）→ TomlInvalidValueException
//   ③ 其它裸词（非数字/布尔/数组/内联表）→ 同上
//   ★ 非 ASCII 裸段名/键名**不算错误**：真实 Tomlet 实测可解析（EnsureLegalChar
//     只拒控制字符），只记为提示（Diag 级）。
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using MelonLoader.Utils;

namespace ContainerSuite.Shared;

/// <summary>
/// MelonPreferences.cfg 终态 TOML 合法性自检（只读探针）。全部静态、无实例状态。
/// </summary>
public static class CfgSelfCheck
{
    // ── 自检延迟执行：等到所有 mod 都初始化完之后再看终态（见文件头）。──
    private const float DeferCheckSeconds = 8.0f;
    private static float _checkTimer = DeferCheckSeconds;
    private static bool _checkDone;

    /// <summary>
    /// 由 Mod.OnUpdate 每帧调用。仅在延迟时间到时跑一次只读自检，其余帧只做
    /// 一次浮点减，稳态开销可忽略。
    /// </summary>
    public static void Tick()
    {
        if (_checkDone) return;
        _checkTimer -= UnityEngine.Time.unscaledDeltaTime;
        if (_checkTimer > 0f) return;
        _checkDone = true;
        TomlSanityCheck();
    }

    /// <summary>
    /// 全文件扫描 TOML 合法性，把问题（含行号/内容/疑似来源）打进启动日志。
    /// 只读，绝不改文件。
    /// </summary>
    public static void TomlSanityCheck()
    {
        try
        {
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");
            if (!File.Exists(path))
            {
                ContainerSuiteLog.Diag("[cfg 自检] 未找到 MelonPreferences.cfg，跳过。");
                return;
            }

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            var problems = new List<string>();     // 真会让 Tomlet 抛异常 → 触发 Fallback
            var notices = new List<string>();      // 仅"不够规范"，Tomlet 能接受

            string currentSection = "(文件开头)";

            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i];
                string s = raw.Trim();
                if (s.Length == 0 || s[0] == '#') continue;

                if (s[0] == '[')
                {
                    currentSection = s;
                    // ★ 非 ASCII 裸段名：实测 Tomlet（MelonLoader 0.7.4 随附版）
                    //   **能正常解析**（`EnsureLegalChar` 只拒控制字符，不拒非 ASCII），
                    //   不会触发 Fallback。故这里只记为"提示"，不算错误。
                    //   （早先据 Python tomllib 的严格判定误报过，已修正。）
                    int close = s.IndexOf(']');
                    string inner = close > 0 ? s.Substring(1, close - 1).Trim() : s.Substring(1);
                    if (!inner.StartsWith("\"") && !inner.StartsWith("'") && HasNonAscii(inner))
                        notices.Add($"  L{i + 1} 段名为非 ASCII 裸名（Tomlet 可解析，但写法不规范）: {Trunc(s)}");
                    continue;
                }

                int eq = s.IndexOf('=');
                if (eq <= 0) continue;

                string key = s.Substring(0, eq).Trim();
                string val = s.Substring(eq + 1).Trim();

                // 非 ASCII 裸键名：同上，Tomlet 能解析，只提示。
                if (!key.StartsWith("\"") && !key.StartsWith("'") && HasNonAscii(key))
                    notices.Add($"  L{i + 1} 键名为非 ASCII 裸名（Tomlet 可解析，但写法不规范）: {Trunc(s)}  段: {currentSection}");

                // ★ 值非法：这类才会让 TomlParser 抛异常 → WasError → Fallback。
                if (!IsValidTomlValue(val))
                    problems.Add($"  L{i + 1} 值非法: {Trunc(s)}  段: {currentSection}  → 疑似来源 mod: {GuessOwner(currentSection)}");
            }

            if (problems.Count == 0)
            {
                string extra = notices.Count > 0 ? $"（另有 {notices.Count} 处非 ASCII 裸名，Tomlet 可解析，仅记录）" : "";
                ContainerSuiteLog.Msg($"[cfg 自检] MelonPreferences.cfg TOML 终态合法性检查通过（{lines.Length} 行）{extra}。");
                // 提示项用 Diag 输出，避免刷屏；仅在诊断开关开启时可见。
                foreach (string n in notices) ContainerSuiteLog.Diag("[cfg 自检][提示]" + n);
            }
            else
            {
                // ★ 用 Error 级：这类值非法会让 TomlParser 抛异常 → WasError → Fallback，
                //   届时【所有 mod】的配置都无法写盘。必须醒目。
                var sb = new StringBuilder();
                sb.AppendLine($"[cfg 自检] ⚠ 启动后终态检查：MelonPreferences.cfg 有 {problems.Count} 处会使 Tomlet 解析抛异常的值 —— " +
                              "若不修，MelonLoader 下次启动会整份载入失败（Fallback），届时【所有 mod】的配置都无法写盘！");
                foreach (string p in problems) sb.AppendLine(p);
                sb.AppendLine("  提示：非法行不都属于本套件；但一个文件里任意一处非法会让所有 mod 一起遭殃。");
                sb.AppendLine("  修法：值整体加引号并转义内层引号；含逗号的裸值必须加引号。");
                foreach (string n in notices) sb.AppendLine(n);
                ContainerSuiteLog.Error(sb.ToString().TrimEnd());
            }
        }
        catch (Exception ex)
        {
            ContainerSuiteLog.Warn("[cfg 自检] 检查失败（不影响启动）：" + ex.Message);
        }
    }

    private static bool HasNonAscii(string s)
    {
        for (int i = 0; i < s.Length; i++)
            if (s[i] > 0x7F) return true;
        return false;
    }

    private static string Trunc(string s) => s.Length <= 110 ? s : s.Substring(0, 110) + "…";

    /// <summary>
    /// 判断 '=' 右侧是否是一个合法 TOML 值（只覆盖本文件实际会用到的类型）。
    /// 关键是字符串分支：引号必须能**完整闭合且闭合后无残留**——
    /// 这正是 ILoveGenshin 那行踩的坑（内层引号未转义 → 提前闭合 → 残留）。
    /// </summary>
    private static bool IsValidTomlValue(string val)
    {
        string v = val.Trim();
        if (v.Length == 0) return false;

        char c = v[0];
        if (c == '[' || c == '{') return true;      // 数组 / 内联表

        if (c == '"' || c == '\'')
        {
            char q = c;
            int i = 1;
            while (i < v.Length)
            {
                if (q == '"' && v[i] == '\\') { i += 2; continue; }
                if (v[i] == q) return v.Substring(i + 1).Trim().Length == 0;   // 闭合后不允许有尾巴
                i++;
            }
            return false;   // 没找到闭合引号
        }

        if (v == "true" || v == "false") return true;
        if (Regex.IsMatch(v, @"^[+-]?\d[\d_]*$")) return true;
        if (Regex.IsMatch(v, @"^[+-]?\d[\d_]*\.\d[\d_]*([eE][+-]?\d+)?$")) return true;   // 浮点

        return false;   // 裸词（含逗号的 a,b,c 等）非法
    }

    /// <summary>
    /// 由段名猜测写入者。段名通常就是 mod 的 MelonPreferences 段（多数等于 DLL 名）。
    /// 只为日志提示用，猜不准也无害。
    /// </summary>
    private static string GuessOwner(string sectionLine)
    {
        try
        {
            int close = sectionLine.IndexOf(']');
            string inner = close > 0 ? sectionLine.Substring(1, close - 1).Trim().Trim('"', '\'') : sectionLine;
            return string.IsNullOrEmpty(inner) ? "(未知)" : inner;
        }
        catch { return "(未知)"; }
    }
}