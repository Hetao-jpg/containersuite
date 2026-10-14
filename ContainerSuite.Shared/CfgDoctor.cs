// CFG 体检（2026-10-14，方案 P0-1 / P0-2 / P0-3）
//
// 【要治的病】玩家手改 UserData\MelonPreferences.cfg 之后"没生效 / 值变回了默认"。
//   根因（反编译 + 实测，见 docs/cfg-handedit-rootcause-and-fix.md）：
//     MelonLoader 取值走 TomletMain.To<T>，任何类型不匹配 / 写法不规范的异常都被
//     MelonPreferences_Entry<T>.Load 的 3 个空 catch 吞掉 ⇒ 条目静默保持 CreateEntry
//     传入的代码默认值；而本套件每次启动都会 SaveToFile 全量重写 cfg ⇒ 默认值随即
//     落盘，玩家写的原文就此消失。
//
// 【本类做三件事】
//   ① Preflight()：启动最早期扫一遍 cfg 原文，建立"文件里每个键写的是什么"的快照；
//      并反射读 MelonLoader 的 WasError，判断本次启动是否整份解析失败（Fallback）。
//   ② ApplyTo(cat)：各模块 CreateEntry 之后调用 —— 对"文件里有值、条目却停在代码
//      默认值"的条目做**宽容采纳**（去多余引号 / 30.0→30 / 1·0·yes·no→bool），
//      让写得不规范的值也生效，并逐条点名。
//   ③ SaveIfNeeded(cat)：只在"值变了 / 缺键 / 注释变了"时才写盘，把"默认值落盘"
//      的机会从"每次启动必然"降为"仅首次安装与真有变更时"。
//
// 【两条硬边界】
//   · **绝不改玩家的 cfg 文件**。所有采纳都只发生在内存里；文件保持玩家原文，
//     宁可每次启动重新采纳 + 每次都点名，也不替玩家重写他自己的配置。
//     （Fallback 时唯一动文件的动作是**额外复制一份** broken-*.bak 供排查。）
//   · **绝不替玩家猜值**。解析不出来就保持默认值并 Error 点名，不硬套一个数。
//
// 【为什么必须在 CreateEntry 之前 Preflight】
//   MelonLoader 在**所有 mod 之前**就把 cfg 解析成了常驻内存快照，之后磁盘上的
//   改动不会自动生效。所以"文件里到底写了什么"这个信息只能在启动时自己读一遍 ——
//   这正是判断"条目停在默认值是 MelonLoader 读不懂，还是玩家本来就没改"的唯一依据。
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;
using MelonLoader;
using MelonLoader.Utils;

namespace ContainerSuite.Shared;

/// <summary>
/// cfg 体检：把"手改被静默回落默认值"变成"能生效 + 看得见 + 不写坏文件"。
/// </summary>
public static class CfgDoctor
{
    // ══════════════════════════════════════════════════════════════════
    // 状态（全部只在本进程内有效，无持久化）
    // ══════════════════════════════════════════════════════════════════

    /// <summary>cfg 原文按 '\n' 切开（元素可能带尾部 '\r'，拼回去字节不变）。</summary>
    private static string[] _lines;

    /// <summary>键名（忽略大小写）→ 它在文件里的位置与原文。只收本段。</summary>
    private static Dictionary<string, RawLine> _rawByKey;

    /// <summary>本段在文件里是否已存在（= 不是首次安装）。</summary>
    private static bool _sectionExisted;

    /// <summary>段名大小写不符（文件中是别的写法）。</summary>
    private static string _sectionSpellingWrong;

    private static bool _preflightDone;

    /// <summary>本次启动 MelonLoader 整份解析失败（Fallback）。</summary>
    private static bool _fallback;

    /// <summary>本次运行 MelonLoader 已禁止一切保存（Fallback 的后果）。</summary>
    private static bool _savingDisabled;

    private static int _applied;          // 已宽容采纳的条目数
    private static int _unreadable;       // 文件里有值但两种读法都读不出的条目数
    private static int _checked;          // 参与体检的条目数
    /// <summary>已计数的键（三个模块共用同一段，ApplyTo 会被调用三次）。</summary>
    private static readonly HashSet<string> _counted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>本轮"真的写了盘"与"跳过写盘"的段数（收尾报告无条件输出）。</summary>
    private static int _saved;
    private static int _skipped;
    private static readonly List<string> _appliedNotes = new List<string>();
    private static readonly List<string> _unreadableNotes = new List<string>();

    private sealed class RawLine
    {
        internal int Line;          // 0-based 行号（用于日志里报 1-based 行号）
        internal string RawKey;     // 文件里的键名原文
        internal string RawValue;   // 等号右侧原文（已去行内注释，未去引号）
    }

    private const string CfgFileName = "MelonPreferences.cfg";

    // ══════════════════════════════════════════════════════════════════
    // ① Preflight —— 启动最早期：读文件快照 + 判断 Fallback
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 在 <c>Mod.OnInitializeMelon</c> 的最开头调用（必须早于任何 CreateEntry）。
    /// 任何异常都只留痕，绝不阻断启动。
    /// </summary>
    public static void Preflight()
    {
        if (_preflightDone) return;
        _preflightDone = true;
        try
        {
            string path = CfgPath();
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;   // 首次安装：无文件

            SnapshotFile(path);
            _fallback = DetectFallback(out _savingDisabled);

            if (_fallback)
            {
                // 整份解析失败 → 本次运行所有 mod 的配置都停在默认值、且保存被禁用。
                // 这是玩家最需要知道的一件事，必须 Error 级、且给出可疑行号。
                ReportFallback(path);
            }
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("CfgDoctor.Preflight", ex);
        }
    }

    private static string CfgPath()
    {
        try { return Path.Combine(MelonUserData(), CfgFileName); }
        catch { return null; }
    }

    private static string MelonUserData()
    {
        return MelonEnvironment.UserDataDirectory;
    }

    /// <summary>把文件切成行 + 抽出本段的键值快照。读不动就保持 <c>_lines = null</c>（= 保守落盘）。</summary>
    private static void SnapshotFile(string path)
    {
        string text = File.ReadAllText(path);
        string[] lines = text.Split('\n');
        _lines = lines;
        _rawByKey = new Dictionary<string, RawLine>(StringComparer.OrdinalIgnoreCase);

        // 段名先按"精确同名"找一遍；只有精确段不存在时才退而认大小写不符的写法，
        // 避免两段并存时把两处同名的键混起来。
        string section = MelonPrefsUtil.SuiteCategoryId;
        bool exact = SectionAt(lines, section, out int first, out int last);

        if (!exact)
        {
            // 大小写/写法不符的段（例如 [custominfrastructure]）：MelonLoader 认不到，
            // 所有条目都会回落默认值 —— 这里仍然把它的键收下来，交给宽容采纳兜住。
            for (int i = 0; i < lines.Length; i++)
            {
                string t = lines[i].Trim();
                if (!IsHeader(t)) continue;
                string name = t.Substring(1, t.Length - 2).Trim();
                if (string.Equals(name, section, StringComparison.OrdinalIgnoreCase))
                {
                    _sectionSpellingWrong = name;
                    first = i;
                    last = NextHeader(lines, i + 1);
                    break;
                }
            }
        }

        if (first < 0) return;
        _sectionExisted = true;

        for (int i = first + 1; i < last && i < lines.Length; i++)
        {
            string t = StripLineComment(lines[i]).Trim();
            if (t.Length == 0) continue;
            int eq = t.IndexOf('=');
            if (eq <= 0) continue;
            string k = Unquote(t.Substring(0, eq).Trim());
            if (k.Length == 0) continue;
            if (_rawByKey.ContainsKey(k)) continue;   // 重复键：第一条为准（重复本身会被 Fallback 抓到）
            _rawByKey[k] = new RawLine
            {
                Line = i,
                RawKey = k,
                // 行内注释（`30  # 备注`）不算值的一部分，先摘掉；引号内的 # 不误伤。
                RawValue = StripInlineComment(t.Substring(eq + 1).Trim()).Trim()
            };
        }
    }

    private static bool SectionAt(string[] lines, string section, out int first, out int last)
    {
        first = -1; last = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            string t = lines[i].Trim();
            if (!IsHeader(t)) continue;
            string name = t.Substring(1, t.Length - 2).Trim();
            if (!string.Equals(name, section, StringComparison.Ordinal)) continue;
            first = i;
            last = NextHeader(lines, i + 1);
            return true;
        }
        return false;
    }

    private static int NextHeader(string[] lines, int from)
    {
        for (int i = from; i < lines.Length; i++)
            if (IsHeader(lines[i].Trim())) return i;
        return lines.Length;
    }

    private static bool IsHeader(string t)
        => t.Length >= 2 && t[0] == '[' && t[t.Length - 1] == ']';

    // ══════════════════════════════════════════════════════════════════
    // Fallback 探测（反射 MelonLoader 内部状态）
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 反射读 <c>MelonPreferences.DefaultFile</c> 的 <c>WasError</c> / <c>IsSaving</c>。
    /// <para>
    /// 两者都是 MelonLoader 的内部成员（internal 类 <c>Preferences.IO.File</c> 上的
    /// internal 属性），只在 MelonLoader 版本变动时可能失效 —— 读不到就当"没失败"，
    /// 绝不会因为反射失败而误报或阻断启动。
    /// </para>
    /// </summary>
    private static bool DetectFallback(out bool savingDisabled)
    {
        savingDisabled = false;
        try
        {
            const BindingFlags F =
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            FieldInfo fld = typeof(MelonPreferences).GetField("DefaultFile", F);
            object file = fld?.GetValue(null);
            if (file == null) return false;

            Type ft = file.GetType();

            // WasError 是属性，IsSaving 是字段（反编译 0.7.4-ci.2586 确认）——
            // 两个都按"先属性后字段"读，MelonLoader 换实现时也只退化成少一行提示。
            bool wasError = ReadBool(ft, file, "WasError", F);
            bool isSaving = ReadBool(ft, file, "IsSaving", F);

            // WasError = true ⇒ MelonLoader 已进入 Fallback：不再写盘。
            // IsSaving 为 false 是同一个后果的另一种观测。
            if (!isSaving) savingDisabled = true;
            return wasError;
        }
        catch { return false; }
    }

    private static bool ReadBool(Type t, object obj, string name, BindingFlags f)
    {
        try
        {
            PropertyInfo p = t.GetProperty(name, f);
            if (p != null)
            {
                object v = p.GetValue(obj);
                if (v is bool bp) return bp;
            }
            FieldInfo fi = t.GetField(name, f);
            if (fi != null)
            {
                object v = fi.GetValue(obj);
                if (v is bool bf) return bf;
            }
        }
        catch { }
        return false;
    }

    /// <summary>
    /// Fallback 报告：备份一份 broken-*.bak，并**逐行**找出最像"非法 TOML"的那几行。
    /// <para>
    /// 只在确实 Fallback 时才跑（正常文件零开销），所以这些启发式即使不完美也不会误报 ——
    /// 它们只负责把"用文本编辑器逐段自查"缩成"看日志第 N 行"。
    /// </para>
    /// </summary>
    private static void ReportFallback(string path)
    {
        MelonLogger.Error("[cfg体检] ✗ MelonPreferences.cfg 整份解析失败（MelonLoader 已进入 Fallback）。");
        MelonLogger.Error("[cfg体检]   后果：**所有 mod** 的配置都停在代码默认值，"
            + "且本次运行期间任何配置改动（含面板里改的）都不会被保存。");
        if (_savingDisabled)
            MelonLogger.Error("[cfg体检]   已确认 MelonLoader 关闭了写盘（IsSaving=false）。");
        MelonLogger.Error("[cfg体检]   修好后重启才能恢复保存；本套件已尽量按原文把你的值在本局生效。");

        string bak = TryBackupBroken(path);
        if (!string.IsNullOrEmpty(bak))
            MelonLogger.Error("[cfg体检]   已额外备份一份待排查文件：" + bak);

        var suspects = FindSuspectLines(_lines, 8);
        if (suspects.Count == 0)
        {
            MelonLogger.Error("[cfg体检]   没能定位到明显可疑的行（可能是别家 mod 的复杂写法，"
                + "或本启发式未覆盖的语法）。建议对本文件做一次 git/备份对比。");
            return;
        }
        for (int i = 0; i < suspects.Count; i++)
            MelonLogger.Error("[cfg体检]   疑似非法 TOML → " + suspects[i]);
    }

    private static string TryBackupBroken(string path)
    {
        try
        {
            string bak = path + ".broken-" + DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(path, bak, true);
            return bak;
        }
        catch (Exception ex) { CatchLogOnce.Warn("CfgDoctor.BackupBroken", ex); return null; }
    }

    // ══════════════════════════════════════════════════════════════════
    // 逐行可疑性启发式（只在 Fallback 后运行）
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 找出最像"让 Tomlet 解析抛异常"的行。判定范围刻意收窄到 README「已知问题」里
    /// 点名的那几类：漏引号（裸值）、值里未转义的内层引号、全角标点、只写键不写值、
    /// 段内重复键、段头没写完。
    /// </summary>
    private static List<string> FindSuspectLines(string[] lines, int max)
    {
        var hits = new List<string>();
        if (lines == null) return hits;

        string section = "(文档开头)";
        var seen = new Dictionary<string, int>(StringComparer.Ordinal);   // TOML 键名区分大小写

        for (int i = 0; i < lines.Length && hits.Count < max; i++)
        {
            string raw = lines[i];
            string t = StripLineComment(raw).Trim();
            if (t.Length == 0) continue;

            if (IsHeader(t))
            {
                section = t;
                seen.Clear();
                continue;
            }

            int eq = t.IndexOf('=');
            if (eq < 0)
            {
                if (!string.Equals(section, "(文档开头)", StringComparison.Ordinal))
                    Add(hits, i, "既不是注释也不是「键 = 值」：" + Clip(t));
                continue;
            }

            string key = t.Substring(0, eq).Trim();
            string val = t.Substring(eq + 1).Trim();

            if (key.Length == 0) { Add(hits, i, "等号左边没有键名：" + Clip(t)); continue; }
            if (val.Length == 0) { Add(hits, i, "只写了键没写值（TOML 不允许）：" + Clip(t)); continue; }

            if (seen.TryGetValue(key, out int prev))
            {
                Add(hits, i, "同一段里重复定义键「" + key + "」（上一次在第 " + (prev + 1) + " 行）");
                continue;
            }
            seen[key] = i;

            string reason = ValueProblem(val);
            if (reason != null) Add(hits, i, reason + "：" + Clip(t));
        }
        return hits;
    }

    private static void Add(List<string> hits, int lineIndex, string msg)
        => hits.Add("第 " + (lineIndex + 1) + " 行 · " + msg);

    private static string Clip(string s) => s.Length <= 90 ? s : s.Substring(0, 90) + "…";

    /// <summary>值形状检查。返回 null = 看起来合法。</summary>
    private static string ValueProblem(string val)
    {
        char c0 = val[0];

        if (c0 == '"' || c0 == '\'')
        {
            if (val.Length < 2 || val[val.Length - 1] != c0)
                return "引号没闭合";
            if (c0 == '"')
            {
                // 内层未转义的 " ：合法写法必须是 \"，所以未配对的双引号会在这里暴露。
                int count = 0;
                for (int i = 0; i < val.Length; i++)
                {
                    if (val[i] == '\\') { i++; continue; }
                    if (val[i] == '"') count++;
                }
                if (count % 2 != 0) return "字符串里有未转义的内层双引号（要写成 \\\"）";
            }
            return null;
        }

        if (c0 == '[' || c0 == '{') return null;      // 数组 / 内联表：不在这里判
        if (IsBool(val)) return null;
        if (IsNumber(val)) return null;
        if (LooksLikeDate(val)) return null;

        if (HasFullWidth(val))
            return "值里有全角标点（TOML 只认半角，字符串要加引号）";
        return "裸值不被 TOML 接受（字符串必须整体加引号）";
    }

    private static bool IsBool(string v)
    {
        return string.Equals(v, "true", StringComparison.OrdinalIgnoreCase)
            || string.Equals(v, "false", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNumber(string v)
    {
        string s = v.Replace("_", "");
        if (s.Length == 0) return false;
        return double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
    }

    private static bool LooksLikeDate(string v)
        => v.Length >= 8 && v.IndexOf('-') > 0 && v.IndexOf(':') > 0;

    private static bool HasFullWidth(string v)
    {
        for (int i = 0; i < v.Length; i++)
        {
            char c = v[i];
            if (c == '＝' || c == '，' || c == '；' || c == '：' || c == '＂'
                || c == '（' || c == '）' || c == '｛' || c == '｝') return true;
        }
        return false;
    }

    // ══════════════════════════════════════════════════════════════════
    // ② ApplyTo —— 各模块 CreateEntry 之后：宽容采纳
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 对刚注册完的这一批条目做体检。必须在**本模块读快照之前**调用
    /// （例如 DIY 的 RefreshIdLists / LoadPerIdSizeLimits 之前）。
    /// </summary>
    public static void ApplyTo(MelonPreferences_Category category)
    {
        if (category == null) return;
        try
        {
            if (!_preflightDone) Preflight();
            if (_rawByKey == null || _rawByKey.Count == 0) return;

            foreach (MelonPreferences_Entry entry in category.Entries)
            {
                if (entry == null) continue;
                RawLine raw;
                if (!_rawByKey.TryGetValue(entry.Identifier ?? "", out raw)) continue;

                if (_counted.Add(entry.Identifier)) _checked++;

                Type t = entry.GetReflectedType();
                object want;
                string how;
                if (!TryLenient(raw.RawValue, t, out want, out how))
                {
                    // 两种读法都读不出 → 保持默认值（绝不猜），Error 点名。
                    _unreadable++;
                    string note = "第 " + (raw.Line + 1) + " 行 " + raw.RawKey + " = " + Clip(raw.RawValue)
                                + " → 期望 " + Friendly(t) + "，实际按代码默认值 "
                                + entry.GetDefaultValueAsString() + " 生效";
                    _unreadableNotes.Add(note);
                    continue;
                }

                string cur = entry.GetValueAsString() ?? "";
                string def = entry.GetDefaultValueAsString() ?? "";

                // 只在"条目停在代码默认值、而文件里写的是别的"时接管 ——
                // 这正是 MelonLoader 静默回落默认值的唯一指纹，不会误伤迁移写入的值。
                if (!SameValue(cur, def, t)) continue;
                if (SameValue(Str(want, t), cur, t)) continue;

                entry.BoxedValue = want;
                _applied++;
                _appliedNotes.Add("第 " + (raw.Line + 1) + " 行 " + raw.RawKey + " = " + Clip(raw.RawValue)
                    + "（MelonLoader 读不懂）→ 已按 " + Str(want, t) + " 生效"
                    + (how.Length > 0 ? "；" + how : ""));
            }
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("CfgDoctor.ApplyTo", ex);
        }
    }

    /// <summary>宽容采纳的条数（留给日志/自检用）。</summary>
    public static int AppliedCount { get { return _applied; } }

    /// <summary>
    /// 宽容解析：把玩家"意思对但写法不规范"的值读出来。
    /// <para>
    /// ★ 只做**无损**转换：`"30"`→30、`30.0`→30、`1/0/yes/no`→true/false、
    ///   多余引号、`Ctrl+E` 这类裸字符串。**不做**"猜"（`三十` 不会变成 30）。
    /// </para>
    /// </summary>
    private static bool TryLenient(string raw, Type t, out object value, out string how)
    {
        value = null;
        how = "";
        if (raw == null) return false;

        string s = raw.Trim();
        if (s.Length == 0) return false;

        bool wasQuoted = s.Length >= 2 && ((s[0] == '"' && s[s.Length - 1] == '"')
                                        || (s[0] == '\'' && s[s.Length - 1] == '\''));
        string bare = wasQuoted ? Unquote(s) : s;

        if (t == typeof(string))
        {
            value = wasQuoted ? Unescape(bare) : bare;
            // 裸字符串本身在 TOML 里就是非法值（会让整份 cfg 解析失败），所以这里必须给出
            // 可照抄的改法 —— 2026-10-14 故障测试发现原来的措辞（"改成 N 字符的带引号写法"）
            // 玩家看不懂。
            if (!wasQuoted) how = "建议整体加上双引号（内层引号要写成 \\\"）";
            return true;
        }

        if (t == typeof(bool))
        {
            bool b;
            if (TryBool(bare, out b))
            {
                value = b;
                if (wasQuoted) how = "建议去掉引号写成 " + (b ? "true" : "false");
                else if (!string.Equals(bare, b ? "true" : "false", StringComparison.Ordinal))
                    how = "建议写成 " + (b ? "true" : "false");
                return true;
            }
            return false;
        }

        if (t == typeof(int))
        {
            int i;
            if (int.TryParse(bare, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            {
                value = i;
                if (wasQuoted) how = "建议去掉引号写成 " + i.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            double d;
            if (double.TryParse(bare, NumberStyles.Float, CultureInfo.InvariantCulture, out d)
                && Math.Abs(d - Math.Round(d)) < 1e-9
                && Math.Round(d) >= int.MinValue && Math.Round(d) <= int.MaxValue)
            {
                i = (int)Math.Round(d);
                value = i;
                how = "建议写成整数 " + i.ToString(CultureInfo.InvariantCulture);
                return true;
            }
            return false;
        }

        if (t == typeof(float))
        {
            float f;
            if (float.TryParse(bare, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                value = f;
                if (wasQuoted) how = "建议去掉引号";
                return true;
            }
            return false;
        }

        if (t == typeof(double))
        {
            double d;
            if (double.TryParse(bare, NumberStyles.Float, CultureInfo.InvariantCulture, out d))
            {
                value = d;
                if (wasQuoted) how = "建议去掉引号";
                return true;
            }
            return false;
        }

        return false;   // 其它类型不在本套件的配置面内
    }

    private static bool TryBool(string s, out bool v)
    {
        v = false;
        if (string.IsNullOrEmpty(s)) return false;
        switch (s.ToLowerInvariant())
        {
            case "true": case "t": case "yes": case "y": case "on": case "1": case "开": case "是":
                v = true; return true;
            case "false": case "f": case "no": case "n": case "off": case "0": case "关": case "否":
                v = false; return true;
            default: return false;
        }
    }

    private static string Unquote(string s)
    {
        if (s == null) return "";
        s = s.Trim();
        if (s.Length >= 2 && ((s[0] == '"' && s[s.Length - 1] == '"') || (s[0] == '\'' && s[s.Length - 1] == '\'')))
            return s.Substring(1, s.Length - 2);
        return s;
    }

    private static string Unescape(string s) => s.Replace("\\\"", "\"").Replace("\\\\", "\\");

    private static string Str(object v, Type t)
    {
        if (v == null) return "";
        if (t == typeof(float)) return ((float)v).ToString("0.####", CultureInfo.InvariantCulture);
        if (t == typeof(double)) return ((double)v).ToString("0.####", CultureInfo.InvariantCulture);
        if (t == typeof(bool)) return ((bool)v) ? "true" : "false";
        return v.ToString();
    }

    /// <summary>两个值是否同义（数字按数值比、bool 忽略大小写、字符串按原文）。</summary>
    private static bool SameValue(string a, string b, Type t)
    {
        a = a ?? ""; b = b ?? "";
        if (t == typeof(bool))
            return string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
        if (t == typeof(int) || t == typeof(float) || t == typeof(double))
        {
            double da, db;
            string sa = a.Trim().Replace("_", "");
            string sb = b.Trim().Replace("_", "");
            if (double.TryParse(sa, NumberStyles.Float, CultureInfo.InvariantCulture, out da)
                && double.TryParse(sb, NumberStyles.Float, CultureInfo.InvariantCulture, out db))
                return Math.Abs(da - db) < 1e-9;
            return string.Equals(sa, sb, StringComparison.Ordinal);
        }
        return string.Equals(a, b, StringComparison.Ordinal);
    }

    private static string Friendly(Type t)
    {
        if (t == typeof(bool)) return "开关（true/false）";
        if (t == typeof(int)) return "整数";
        if (t == typeof(float) || t == typeof(double)) return "小数";
        if (t == typeof(string)) return "字符串";
        return t.Name;
    }

    // ══════════════════════════════════════════════════════════════════
    // ③ SaveIfNeeded —— 只在真有变化时写盘
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 需要才保存。判据（任一成立即保存）：
    /// <list type="bullet">
    /// <item>拿不到文件快照（首次安装 / 读失败）→ 保守保存；</item>
    /// <item>本段在文件里还不存在（首次安装）；</item>
    /// <item>有注册键在文件里不存在（新版本新增的项）；</item>
    /// <item>某键的值与文件原文不同义（迁移 / 补齐 / 宽容采纳改过内存）；</item>
    /// <item>某键的注释与文件里的注释块不一致（中文显示名首次写入 / 8A 注释重建）。</item>
    /// </list>
    /// <para>
    /// 这是"手改被默认值覆盖"的第二个执行者：原实现每次启动无条件重写整份 cfg，
    /// 于是任何一次静默回落都会立刻落盘。
    /// </para>
    /// <para>
    /// ★ 没有"强制保存"开关：所有真正的改动路径都能被上面五条判据看见 ——
    ///   值改动看得见（值比对）、注释改动也看得见（注释比对）。将来若出现
    ///   "内存改了但值/注释都看不出来"的新情况，再在这里加显式标记，不要图省事
    ///   改回无条件 SaveToFile（2026-10-14 实机印证：只要有一处强制保存，
    ///   每次启动就必然整份重写 cfg）。
    /// </para>
    /// </summary>
    public static void SaveIfNeeded(MelonPreferences_Category category)
    {
        if (category == null) return;
        try
        {
            string why;
            if (!NeedsSave(category, out why))
            {
                _skipped++;
                ContainerSuiteLog.Diag("[cfg体检] " + category.Identifier
                    + " 与文件完全一致，跳过写盘（不再无条件重写 cfg）。");
                return;
            }
            category.SaveToFile();
            _saved++;
            ContainerSuiteLog.Diag("[cfg体检] 写盘：" + category.Identifier + "（" + why + "）");
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("CfgDoctor.SaveIfNeeded", ex);
            try { category.SaveToFile(); } catch { }
        }
    }

    private static bool NeedsSave(MelonPreferences_Category category, out string why)
    {
        why = null;
        if (_lines == null || _rawByKey == null) { why = "无文件快照"; return true; }
        if (!_sectionExisted) { why = "本段首次创建"; return true; }

        foreach (MelonPreferences_Entry entry in category.Entries)
        {
            if (entry == null) continue;
            RawLine raw;
            if (!_rawByKey.TryGetValue(entry.Identifier ?? "", out raw))
            {
                why = "新增键 " + entry.Identifier;
                return true;
            }

            Type t = entry.GetReflectedType();
            if (!SameValue(Unquote(raw.RawValue), entry.GetValueAsString() ?? "", t))
            {
                why = "值变化 " + entry.Identifier;
                return true;
            }

            if (!string.Equals(CommentAbove(raw.Line), NormalizeBlock(entry.Description), StringComparison.Ordinal))
            {
                why = "注释变化 " + entry.Identifier;
                return true;
            }
        }
        return false;
    }

    /// <summary>取某个键正上方连续的注释块（多行注释在 cfg 里是连续的 `# ...` 行）。</summary>
    private static string CommentAbove(int lineIndex)
    {
        var block = new List<string>();
        for (int i = lineIndex - 1; i >= 0; i--)
        {
            string t = (_lines[i] ?? "").Trim();
            if (t.Length == 0) break;
            if (t[0] != '#' && t[0] != ';') break;
            string body = t.Substring(1);
            if (body.StartsWith(" ", StringComparison.Ordinal)) body = body.Substring(1);
            block.Insert(0, body.TrimEnd());
        }
        while (block.Count > 0 && block[block.Count - 1].Length == 0) block.RemoveAt(block.Count - 1);
        return string.Join("\n", block.ToArray());
    }

    private static string NormalizeBlock(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        string[] parts = s.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        for (int i = 0; i < parts.Length; i++) parts[i] = parts[i].TrimEnd();
        int end = parts.Length;
        while (end > 0 && parts[end - 1].Length == 0) end--;
        var sb = new StringBuilder();
        for (int i = 0; i < end; i++)
        {
            if (i > 0) sb.Append('\n');
            sb.Append(parts[i]);
        }
        return sb.ToString();
    }

    // ══════════════════════════════════════════════════════════════════
    // 收尾报告（无条件输出，不进 DiagLog 闸门）
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 在 <c>Mod.OnInitializeMelon</c> 末尾调用。把"这次到底按什么值跑起来了"讲清楚 ——
    /// 这是"我改了配置怎么没生效"的第一现场，默认静默等于把唯一线索藏起来。
    /// </summary>
    public static void Report()
    {
        try
        {
            if (_unreadableNotes.Count > 0)
            {
                MelonLogger.Error("[cfg体检] ✗ " + _unreadableNotes.Count
                    + " 项读不出，已保持代码默认值（下面逐条列出行号，改完重启即可）：");
                for (int i = 0; i < _unreadableNotes.Count; i++)
                    MelonLogger.Error("[cfg体检]   · " + _unreadableNotes[i]);
            }

            if (_appliedNotes.Count > 0)
            {
                MelonLogger.Warning("[cfg体检] " + _appliedNotes.Count
                    + " 项手改写法不规范，已按你的**本意**生效（本套件不改你的文件，"
                    + "想消掉这些提示就按后面的建议写法改一下）：");
                for (int i = 0; i < _appliedNotes.Count; i++)
                    MelonLogger.Warning("[cfg体检]   · " + _appliedNotes[i]);
            }

            if (!string.IsNullOrEmpty(_sectionSpellingWrong))
                MelonLogger.Warning("[cfg体检] 段名大小写不符：文件里是 [" + _sectionSpellingWrong
                    + "]，正确写法 [" + MelonPrefsUtil.SuiteCategoryId
                    + "]。本局已按你的值生效，但面板认不到这个段 —— 请改回正确写法。");

            if (_fallback)
            {
                MelonLogger.Error("[cfg体检] 本次运行不保存任何配置改动（Fallback）："
                    + "修好上面点名的行、重启游戏后一切恢复。");
            }

            // ★ 汇总行**无条件输出且只此一行**：健康、被宽容接纳、Fallback 三种情况都要给出
            //   "这次到底按什么值跑起来了 + 有没有改写玩家的文件"。
            //   2026-10-14 故障测试发现：原先 Fallback 分支直接 return，把这行吞了 ——
            //   而"故障时更该看见落盘统计"恰恰是最需要它的场合。
            MelonLogger.Msg("[cfg体检] 汇总：体检 " + _checked + " 项，宽容采纳 " + _applied
                + " 项，读不出 " + _unreadable + " 项；" + WriteSummary()
                + (string.IsNullOrEmpty(_sectionSpellingWrong) ? "" : "；段名大小写不符"));
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("CfgDoctor.Report", ex);
        }
    }

    /// <summary>
    /// 落盘统计。★ **无条件输出、且单独成句**：不开诊断日志也要能一眼看出
    /// "这次启动到底有没有把 cfg 改写掉" —— 那正是"手改被默认值覆盖"的关键指标。
    /// </summary>
    private static string WriteSummary()
    {
        return "写盘 " + _saved + " 次（跳过 " + _skipped + " 次）"
             + (_saved == 0 ? " — cfg 未被改写" : "");
    }

    // ══════════════════════════════════════════════════════════════════
    // 小工具
    // ══════════════════════════════════════════════════════════════════

    /// <summary>摘掉行内注释（只在引号外遇到的 # 才算注释开头）。</summary>
    private static string StripInlineComment(string v)
    {
        bool inDouble = false, inSingle = false;
        for (int i = 0; i < v.Length; i++)
        {
            char c = v[i];
            if (c == '\\' && inDouble) { i++; continue; }
            if (c == '"' && !inSingle) inDouble = !inDouble;
            else if (c == '\'' && !inDouble) inSingle = !inSingle;
            else if (c == '#' && !inDouble && !inSingle) return v.Substring(0, i);
        }
        return v;
    }

    /// <summary>整行是注释 / 空行 → 返回空串，供需要"只要键值部分"的调用点使用。</summary>
    private static string StripLineComment(string line)
    {
        if (line == null) return "";
        string t = line.TrimStart();
        if (t.Length > 0 && (t[0] == '#' || t[0] == ';')) return "";
        return line;
    }
}
