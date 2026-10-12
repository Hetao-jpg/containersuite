// S2（2026-10-12）创建：把「容器升级」的可配置内容从 MelonPreferences 搬到
// UserData 下的独立文件，并新增「每个容器 id 独立最大尺寸」。
//
// ─────────────────────────────────────────────────────────────────────────
// 为什么搬（两条都是实际痛点，代码级可查）：
//   ① 白名单（BoxIds / BackpackIds / … 共 8 条）是**字符串项**，而 ModManager 面板
//      只显示 bool / 数字 / 以 Hotkey、ToggleKey 结尾的项 ⇒ 白名单在面板里
//      **一项都看不到**，玩家只能手改 UserData\MelonPreferences.cfg 的 TOML
//      （还要小心转义，且面板一"应用配置"就会重写整个 cfg）。
//   ② 尺寸上限是**每类别一条**（CreateGridDef 的 MaxW/MaxH，文案就是"所有槽共享"）
//      ⇒ 16 种箱子共用同一个上限，想让 expedition_box 大一点就得把所有箱子一起放开。
//
// ─────────────────────────────────────────────────────────────────────────
// 文件：UserData\ContainerSuite\containers.txt（行文本，不用 JSON：可写注释、手改不易坏）
//
//   # ContainerSuite 容器升级配置 v1
//   [whitelist]
//   box = sec_box, evidence_box, storage_bay, ...
//   backpack = backpack_small, ...
//   cage = animal_cage, rat_cage
//   multislot = fridge, recharger, vacuum_robot
//   multislotmodule = furnace, ...
//   wagebox = ...
//   wagebackpack = ...
//   modcontainer = ...
//   [limits]
//   storage_bay = 30x20
//   evidence_box = 24x24
//
// ─────────────────────────────────────────────────────────────────────────
// 权威与边界（**不留"两处都能改"的模糊地带**）：
//   · 文件不存在 → 用当前 cfg 现值**生成**一份（含注释说明），此时等于行为不变。
//   · 文件存在 → **[whitelist] 里声明了的类别以文件为准**，cfg 里那些旧值不再生效
//     （加载时会无条件打一行说明谁是权威，避免"我改了 cfg 怎么没用"）；
//     未在文件里声明的类别**沿用 cfg**（不静默清空一个类别）。
//   · [limits] 里写了的 id **完全覆盖**类别上限（可增可减）；没写的用类别上限。
//   · 解析异常一律**点名到行号**告警，不静默忽略；文件读不出来就沿用上一份内存配置
//     （绝不因为配置读不到就把容器升级整体关掉）。
//   · 写盘用 tmp → Move 原子替换（同 SaveBagPaging.ids 的做法）。
// ─────────────────────────────────────────────────────────────────────────

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ContainerSuite.Shared;
using Il2Cpp;
using MelonLoader;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    // ═══════════════════════════════════════════════════════════════════════
    // 路径与状态
    // ═══════════════════════════════════════════════════════════════════════

    private const string ConfigDirName = "ContainerSuite";
    private const string ConfigFileName = "containers.txt";
    private const string ConfigVersionLine = "#v1";

    /// <summary>
    /// 文件注释的模板标记。玩家文件里若**没有**这一行，说明他手上是旧版注释，
    /// 加载时会用新注释把整个文件重写一遍（数据按解析出来的值原样重写）。
    /// 见 <see cref="RefreshTemplateIfOutdated"/>。
    /// </summary>
    private const string ConfigTemplateMarker = "#template=";

    /// <summary>当前注释模板版本（改注释时 +1，老玩家下次启动就会拿到新说明）。</summary>
    private const int ConfigTemplateVersion = 2;

    /// <summary>每 id 尺寸上限的硬边界（与 TryGetGridShapeLimits 的 HardMaxDim 一致）。</summary>
    private const int ConfigLimitMinDim = 1;
    private const int ConfigLimitMaxDim = 100;

    /// <summary>文件里 [whitelist] 声明的白名单原文（键 = 小写类别键，如 "box"）。</summary>
    private static readonly Dictionary<string, string> _fileWhitelist = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    /// <summary>文件里 [limits] 的每 id 上限（键 = 规范化容器 id，小写）。</summary>
    private static readonly Dictionary<string, (int W, int H)> _perIdLimit = new Dictionary<string, (int, int)>(StringComparer.Ordinal);

    private static bool _configFileLoaded;
    private static long _configFileStamp = -1;      // 上次读到的 LastWriteTimeUtc.Ticks
    private static PollThrottle _configFileGate;    // 热重载轮询节流（PollThrottle，单调钟）

    private static string ConfigFilePath
    {
        get
        {
            try
            {
                return Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory,
                    ConfigDirName, ConfigFileName);
            }
            catch { return null; }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 加载
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// S2：加载容器升级配置文件。必须在 EnsureDefaultIds / RefreshIdLists **之前**调用
    /// （它们要读生效后的白名单）。
    /// </summary>
    /// <param name="createIfMissing">文件缺失时是否生成初版（Init 首次调用传 true）。</param>
    internal static void LoadContainerConfigFile(bool createIfMissing)
    {
        if (_configFileLoaded && !createIfMissing) return;   // 已被热重载加载过
        try
        {
            string path = ConfigFilePath;
            if (string.IsNullOrEmpty(path)) return;

            if (!File.Exists(path))
            {
                if (!createIfMissing) return;
                _fileWhitelist.Clear();
                _perIdLimit.Clear();
                WriteConfigSeed(path);
                _configFileLoaded = true;
                TryStampConfigFile(path);
                _log?.Msg("[容器升级] 已生成配置文件：" + path
                    + "（以后改这个文件即可，cfg 里的 *Ids / MaxW / MaxH 不再生效）");
                return;
            }

            string[] lines;
            try { lines = File.ReadAllLines(path); }
            catch (Exception ex)
            {
                // 读不出来 → 保留内存里的上一份配置（**不**清空、**不**关功能）。
                ContainerSuiteLog.Warn("[容器升级] 配置文件读取失败，沿用上一份内存配置：" + ex.Message);
                return;
            }

            _fileWhitelist.Clear();
            _perIdLimit.Clear();

            string section = null;
            int whitelistCount = 0, limitCount = 0, problems = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                string raw = lines[i] ?? "";
                string t = raw.Trim();
                if (t.Length == 0 || t[0] == '#') continue;      // 空行 / 注释（含 #v1）

                if (t[0] == '[')                                 // 段
                {
                    int close = t.IndexOf(']');
                    section = (close > 1) ? t.Substring(1, close - 1).Trim().ToLowerInvariant() : null;
                    if (section != "whitelist" && section != "limits")
                    {
                        problems++;
                        ContainerSuiteLog.Warn($"[容器升级] 配置文件第 {i + 1} 行：未知段 [{section}]，本段内容已忽略");
                        section = null;
                    }
                    else if (section == "whitelist") whitelistCount = 0;
                    continue;
                }

                int eq = t.IndexOf('=');
                if (eq <= 0)
                {
                    problems++;
                    ContainerSuiteLog.Warn($"[容器升级] 配置文件第 {i + 1} 行：不是 `键 = 值` 形式，已跳过：{t}");
                    continue;
                }
                string key = t.Substring(0, eq).Trim();
                string val = t.Substring(eq + 1).Trim();

                if (section == "whitelist")
                {
                    if (!IsKnownWhitelistKey(key))
                    {
                        problems++;
                        ContainerSuiteLog.Warn($"[容器升级] 配置文件第 {i + 1} 行：不认识的白名单类别 `{key}`"
                            + "（可用：" + string.Join(" / ", WhitelistKeys) + "），已忽略");
                        continue;
                    }
                    _fileWhitelist[key] = val;
                    whitelistCount++;
                }
                else if (section == "limits")
                {
                    if (!TryParseLimit(val, out int w, out int h))
                    {
                        problems++;
                        ContainerSuiteLog.Warn($"[容器升级] 配置文件第 {i + 1} 行：尺寸 `{val}` 不是 `宽x高`（"
                            + $"{ConfigLimitMinDim}~{ConfigLimitMaxDim}），已忽略");
                        continue;
                    }
                    if (string.IsNullOrEmpty(key))
                    {
                        problems++;
                        ContainerSuiteLog.Warn($"[容器升级] 配置文件第 {i + 1} 行：id 为空，已忽略");
                        continue;
                    }
                    _perIdLimit[NormalizeConfigId(key)] = (w, h);
                    limitCount++;
                }
                else
                {
                    problems++;
                    ContainerSuiteLog.Warn($"[容器升级] 配置文件第 {i + 1} 行：不在任何段内，已跳过：{t}");
                }
            }

            _configFileLoaded = true;
            TryStampConfigFile(path);

            // ★ 注释是玩家唯一的使用说明，但它只在"生成新文件"时写一次 ⇒ 老玩家会一直
            //   停在他第一次生成那版。这里检查模板标记，旧注释就用新注释重写一遍
            //   （数据按刚解析出来的值原样重写），见该方法注释。
            RefreshTemplateIfOutdated(path, lines);

            // ★ 权威声明（无条件输出）：文件里声明过的类别以文件为准，cfg 里的旧值不生效。
            //   这一行是"我改了 cfg 怎么没用"的唯一解释来源，不能走 Diag 闸门。
            _log?.Msg($"[容器升级] 配置来源：{ConfigFileName}（白名单 {whitelistCount} 类、"
                + $"每 id 上限 {limitCount} 条、问题 {problems} 条）"
                + (whitelistCount > 0
                    ? "。已声明类别的白名单**以文件为准**，cfg 里的 *Ids 不再生效"
                    : "。文件中未声明任何白名单，全部沿用 cfg"));
            if (problems > 0)
                ContainerSuiteLog.Warn($"[容器升级] 配置文件有 {problems} 行未生效（详见上方逐行告警）：{path}");
        }
        catch (Exception ex)
        {
            ContainerSuiteLog.Warn("[容器升级] 配置文件加载异常（沿用上一份内存配置）：" + ex.Message);
        }
    }

    /// <summary>热重载轮询：文件改动 ≤2 秒内生效（不重启）。由 DIY 的 Tick 调用。</summary>
    internal static void TickContainerConfigFile()
    {
        try
        {
            if (!_configFileLoaded) return;
            if (!_configFileGate.Due(2000)) return;             // 单调钟节流，2 秒
            string path = ConfigFilePath;
            if (string.IsNullOrEmpty(path) || !File.Exists(path)) return;

            long stamp;
            try { stamp = File.GetLastWriteTimeUtc(path).Ticks; } catch { return; }
            if (stamp == _configFileStamp) return;

            _configFileLoaded = false;                          // 让 Load 重新读
            LoadContainerConfigFile(createIfMissing: false);
            ApplyConfigFileToRuntime();
            _log?.Msg("[容器升级] 检测到配置文件改动，已热重载（白名单与每 id 上限即时生效）");
        }
        catch (Exception ex) { CatchLogOnce.Warn("FileConfig:tick", ex); }
    }

    /// <summary>把文件中声明的白名单/上限应用到运行时（刷新 id 列表 + 作废受影响的缓存）。</summary>
    private static void ApplyConfigFileToRuntime()
    {
        try { RefreshIdLists(); } catch (System.Exception ex) { CatchLogOnce.Warn("FileConfig:refreshIds", ex); }
        // 白名单影响 IsResizableContainer（_resizableCache）、每 id 上限影响
        // TryGetGridShapeLimits（_prepareCache）—— 两者都必须作废，否则要等 TTL 过期。
        try { _resizableCache.Clear(); } catch (System.Exception ex) { CatchLogOnce.Warn("FileConfig:resizable", ex); }
        try { _prepareCache.Clear(); } catch (System.Exception ex) { CatchLogOnce.Warn("FileConfig:prepare", ex); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 供其它处使用
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 白名单类别键（与 cfg 的 `&lt;键&gt;Ids` 同一套词汇，大小写不敏感）。
    /// <para>
    /// ★ 只有这 5 类**是真正的白名单**（判据链按 <c>GridDef.IdList</c> 逐 id 匹配）。
    /// cfg 里还有 <c>WageBoxIds</c>/<c>WageBackpackIds</c>/<c>ModContainerIds</c> 三条，
    /// 但那三类的匹配走 <c>WageContainerRegistry</c> 的**固定 id**（custom_storage_box /
    /// void_bead_storage / wage_gu_machine / wage_ai_generator），它们的 <c>IdList</c>
    /// 在代码里从未被赋值、也没人读 ⇒ 放进本文件只会造出"配了不生效"的假开关，
    /// 故**不提供**这三个键（它们的尺寸上限仍可用 [limits] 段按 id 单独配）。
    /// </para>
    /// </summary>
    internal static readonly string[] WhitelistKeys =
    {
        "box", "backpack", "cage", "multislot", "multislotmodule",
    };

    private static bool IsKnownWhitelistKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        for (int i = 0; i < WhitelistKeys.Length; i++)
            if (string.Equals(WhitelistKeys[i], key, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    /// <summary>
    /// 生效的白名单原文：文件里声明过 → 用文件；否则 → cfg 值。
    /// （只有**原文**这一层交给文件，解析仍走 SplitIds，逗号/中文逗号/分号都认。）
    /// </summary>
    private static string EffectiveWhitelistRaw(string key, string cfgRaw)
    {
        if (!string.IsNullOrEmpty(key) && _fileWhitelist.TryGetValue(key, out var fromFile))
            return fromFile;
        return cfgRaw;
    }

    /// <summary>该类别是否由文件管理（用于 EnsureDefaultIds 跳过 + 日志说明）。</summary>
    private static bool IsWhitelistFromFile(string key)
        => !string.IsNullOrEmpty(key) && _fileWhitelist.ContainsKey(key);

    /// <summary>
    /// S2：取某个容器 id 的独立上限（文件 [limits] 段）。返回 false = 没配，用类别上限。
    /// ★ 键是**规范化后的容器类型 id**（清 \0、去空白、转小写）—— 与白名单同一套词汇，
    ///   不是 uniqueId（实例身份跨存档会重排，不能当持久化键）。
    /// </summary>
    internal static bool TryGetPerIdLimit(GameItem item, out int w, out int h)
    {
        w = 0; h = 0;
        if (item == null || _perIdLimit.Count == 0) return false;
        string id = WageContainerRegistry.NormalizeId(item);
        if (string.IsNullOrEmpty(id)) return false;
        if (!_perIdLimit.TryGetValue(id, out var lim)) return false;
        w = lim.W; h = lim.H;
        return w > 0 && h > 0;
    }

    private static string NormalizeConfigId(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return raw.Replace("\0", "").Trim().ToLowerInvariant();
    }

    /// <summary>cfg 键（如 "BoxIds"）→ 白名单类别键（"box"）；未登记返回 ""。</summary>
    internal static string WhitelistKeyOf(string cfgEntryKey)
    {
        if (string.IsNullOrEmpty(cfgEntryKey)) return "";
        string k = cfgEntryKey.EndsWith("Ids", StringComparison.Ordinal)
            ? cfgEntryKey.Substring(0, cfgEntryKey.Length - 3)
            : cfgEntryKey;
        return IsKnownWhitelistKey(k) ? k.ToLowerInvariant() : "";
    }

    /// <summary>
    /// S2：把「代码默认里有、文件里没有」的容器 id 追加进**配置文件**的那一行（只增不减，
    /// 与 cfg 版 EnsureDefaultIds 同语义）。<b>就地改写该行</b>，其余行（含玩家的注释与格式）原样保留。
    /// <para>
    /// ★ 为什么必须做：白名单搬到文件后，若仍只往 cfg 里补默认 id，补了也不会被读 ⇒
    ///   "代码新增了容器，老玩家的白名单里没有它" 这个坑会**静默复发** ——
    ///   而 EnsureDefaultIds 当初就是为了堵这个坑才存在的。
    /// </para>
    /// </summary>
    internal static void EnsureFileWhitelistHasDefaults(string whitelistKey, string codeDefaults,
        List<string> changes, List<string> failures)
    {
        if (string.IsNullOrEmpty(whitelistKey)) return;
        try
        {
            if (!_fileWhitelist.TryGetValue(whitelistKey, out string cur)) return;

            // 用户主动清空 = 明确表示"这一类我不要"。绝不拿默认值覆盖它（同 cfg 版语义）。
            if (string.IsNullOrWhiteSpace(cur)) return;

            string merged; int added; int collapsed;
            if (!MergeMissingIds(cur, codeDefaults, out merged, out added, out collapsed)) return;
            if (added == 0 && collapsed == 0) return;   // 幂等：绝大多数启动走这里

            if (ReplaceWhitelistLine(whitelistKey, merged))
            {
                _fileWhitelist[whitelistKey] = merged;
                changes.Add(whitelistKey + "（配置文件）"
                    + (added > 0 ? $" 追加 {added} 个ID（{DescribeMissing(cur, codeDefaults)}）" : "")
                    + (collapsed > 0 ? $" 折叠 {collapsed} 个重复ID" : ""));
            }
            else
            {
                failures.Add(whitelistKey + "（配置文件行替换失败）");
            }
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("FileConfig:ensureDefaults:" + whitelistKey, ex);
            failures?.Add(whitelistKey + "（配置文件补齐异常）");
        }
    }

    /// <summary>把 [whitelist] 段里 `key = ...` 那一行就地替换成新值（其余行不动）。</summary>
    private static bool ReplaceWhitelistLine(string key, string newValue)
    {
        string path = ConfigFilePath;
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        try
        {
            string[] lines = File.ReadAllLines(path);
            string section = null;
            for (int i = 0; i < lines.Length; i++)
            {
                string t = (lines[i] ?? "").Trim();
                if (t.Length == 0) continue;
                if (t[0] == '#') continue;
                if (t[0] == '[')
                {
                    int close = t.IndexOf(']');
                    section = (close > 1) ? t.Substring(1, close - 1).Trim().ToLowerInvariant() : null;
                    continue;
                }
                if (section != "whitelist") continue;
                int eq = t.IndexOf('=');
                if (eq <= 0) continue;
                if (!string.Equals(t.Substring(0, eq).Trim(), key, StringComparison.OrdinalIgnoreCase)) continue;

                lines[i] = key + " = " + newValue;
                WriteConfigFile(path, string.Join(Environment.NewLine, lines) + Environment.NewLine);
                TryStampConfigFile(path);   // 自己改的，别让热重载轮询把它当成"玩家改动"再读一遍
                return true;
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("FileConfig:replaceWhitelistLine", ex); }
        return false;
    }

    private static bool TryParseLimit(string val, out int w, out int h)
    {
        w = 0; h = 0;
        if (string.IsNullOrEmpty(val)) return false;
        string[] parts = val.ToLowerInvariant().Split('x');
        if (parts.Length != 2) return false;
        if (!int.TryParse(parts[0].Trim(), out w)) return false;
        if (!int.TryParse(parts[1].Trim(), out h)) return false;
        if (w < ConfigLimitMinDim || w > ConfigLimitMaxDim) return false;
        if (h < ConfigLimitMinDim || h > ConfigLimitMaxDim) return false;
        return true;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 生成初版 / 追加新默认 id
    // ═══════════════════════════════════════════════════════════════════════

    private static void WriteConfigSeed(string path)
    {
        var sb = new StringBuilder();
        AppendHeader(sb);
        AppendSections(sb,
            k =>
            {
                switch (k)
                {
                    case "box": return _box.Ids.Value ?? "";
                    case "backpack": return _backpack.Ids.Value ?? "";
                    case "cage": return _cage.Ids.Value ?? "";
                    case "multislot": return _multiSlot.Ids.Value ?? "";
                    case "multislotmodule": return _multiSlotModule.Ids.Value ?? "";
                    default: return null;
                }
            },
            null);
        WriteConfigFile(path, sb.ToString());
    }

    /// <summary>
    /// 写出 [whitelist] 与 [limits] 两段（含段头与示例注释）。
    /// <paramref name="whitelistValueOf"/> 返回 null = 这一行不写（玩家把整行删掉 = 恢复出厂名单）。
    /// </summary>
    private static void AppendSections(StringBuilder sb,
        Func<string, string> whitelistValueOf, List<string> limitLines)
    {
        sb.AppendLine();
        sb.AppendLine("[whitelist]");
        for (int i = 0; i < WhitelistKeys.Length; i++)
        {
            string v = whitelistValueOf(WhitelistKeys[i]);
            if (v == null) continue;
            AppendSeedWhitelist(sb, WhitelistKeys[i], v);
        }
        sb.AppendLine();
        sb.AppendLine("[limits]");
        AppendLimitComments(sb);
        if (limitLines != null)
            for (int i = 0; i < limitLines.Count; i++) sb.AppendLine(limitLines[i]);
    }

    /// <summary>[limits] 段里的示例注释（行首都带 #，所以本身不生效）。</summary>
    private static void AppendLimitComments(StringBuilder sb)
    {
        sb.AppendLine("# 下面是示例，行首有 # 表示**没生效**。想让某条生效：删掉它行首的 #，再改成你要的尺寸。");
        sb.AppendLine("# storage_bay = 30x20");
        sb.AppendLine("# evidence_box = 24x24");
        sb.AppendLine("# 蛙哥的容器也能在这里写（它们有固定 id）：");
        sb.AppendLine("# custom_storage_box = 20x20     妙妙箱");
        sb.AppendLine("# void_bead_storage = 20x20      虚空珠袋");
        sb.AppendLine("# wage_gu_machine = 20x20        蛙哥机器");
        sb.AppendLine("# wage_ai_generator = 20x20      蛙哥发电机");
    }

    /// <summary>
    /// 生成文件**头注释**（即 <c>[whitelist]</c> 之前的全部内容）。
    /// <para>
    /// ★ 这段文字是给玩家看的，唯一标准是"照着抄就能改对"：全部用中文、
    ///   全部用「想…→ 就…」的写法、有具体例子、把会踩的坑用 ⚠ 单独标出。
    ///   改这里的时候不要写实现细节（比如"cfg 里的 *Ids"这种词玩家不认，
    ///   要说"另一个配置文件里的旧名单"）。
    /// </para>
    /// </summary>
    private static void AppendHeader(StringBuilder sb)
    {
        sb.AppendLine(ConfigVersionLine);
        sb.AppendLine(ConfigTemplateMarker + ConfigTemplateVersion);
        sb.AppendLine("# ================================================================");
        sb.AppendLine("#  容器套件 · 容器升级名单与尺寸上限  ← 你要改的就是这个文件");
        sb.AppendLine("#  改完按 Ctrl+S 存盘，回到游戏 2 秒内自动生效，不用重启游戏");
        sb.AppendLine("# ================================================================");
        sb.AppendLine("#");
        sb.AppendLine("# 这个文件管两件事：");
        sb.AppendLine("#   ① 哪些物品算「能升级的容器」  → 看下面第一段 [whitelist]");
        sb.AppendLine("#   ② 每种容器最多能升多大        → 看下面第二段 [limits]");
        sb.AppendLine("# （升级方式：按住 LeftAlt，把金属锭拖到容器上；每升一次容器大一圈）");
        sb.AppendLine("#");
        sb.AppendLine("# 只有 3 条规则，记住就够：");
        sb.AppendLine("#   1) 行首带 # 的都是说明文字，不生效。这些说明你想删就删，不影响功能。");
        sb.AppendLine("#   2) 不要删掉 [whitelist] 和 [limits] 这两行 —— 它们用来分段。");
        sb.AppendLine("#   3) 一行只写一个 = 号；物品id 之间用英文逗号隔开。");
        sb.AppendLine("#");
        sb.AppendLine("# ----------------------------------------------------------------");
        sb.AppendLine("#  第一段  [whitelist]  哪些物品能升级");
        sb.AppendLine("# ----------------------------------------------------------------");
        sb.AppendLine("#   写法：   类别 = 物品id, 物品id, ...");
        sb.AppendLine("#");
        sb.AppendLine("#   · 想加一个容器 → 把它的物品id加到对应类别那一行的**末尾**，前面补英文逗号");
        sb.AppendLine("#       例如原本是  box = sec_box,evidence_box");
        sb.AppendLine("#       想再加一个就改成  box = sec_box,evidence_box,新容器的id");
        sb.AppendLine("#   · 想去掉一个容器 → 把它的id从那一行里删掉");
        sb.AppendLine("#   · 想恢复这一类出厂名单 → 把**整行都删掉**");
        sb.AppendLine("#       ⚠ 注意是「整行删掉」，不是「把等号右边留空」——留空表示这类容器全部不能升级");
        sb.AppendLine("#");
        sb.AppendLine("#   5 个类别各管什么（左边的名字不能改，右边的id随便加）：");
        sb.AppendLine("#     box              箱子 / 货舱（主仓库、保险箱、走私舱……）← 最常用");
        sb.AppendLine("#     backpack         背包 / 挎包 / 腰包");
        sb.AppendLine("#     cage             笼子（动物笼、鼠笼）");
        sb.AppendLine("#     multislot        能往里塞东西的机器（冰箱、充电器、扫地机器人）");
        sb.AppendLine("#     multislotmodule  机器上的模组槽（熔炉、拆解机、净水器、打印机……）");
        sb.AppendLine("#");
        sb.AppendLine("#   ⚠ 妙妙箱 / 虚空珠袋 / Mod容器 不在上面这份名单里，写进来也没用");
        sb.AppendLine("#     （它们靠固定id认，归另一个配置文件里的「避让开关」管）；只想给它们");
        sb.AppendLine("#     改尺寸上限的话，写在下面的 [limits] 段即可。");
        sb.AppendLine("#");
        sb.AppendLine("#   ⚠ 这里写过的类别，就「以这个文件为准」了：另一个配置文件里对应的旧名单");
        sb.AppendLine("#     不再生效，不用两边同时改。");
        sb.AppendLine("#");
        sb.AppendLine("# ----------------------------------------------------------------");
        sb.AppendLine("#  第二段  [limits]  每种容器最多能升到多大");
        sb.AppendLine("# ----------------------------------------------------------------");
        sb.AppendLine("#   写法：   物品id = 宽x高      （数字填 1~100，中间的 x 是小写英文字母 x）");
        sb.AppendLine("#");
        sb.AppendLine("#   · 只需要写你想单独设上限的那几种，没写的照样用「这一类容器的默认上限」");
        sb.AppendLine("#   · 示例（行首的 # 去掉才会生效）：");
        sb.AppendLine("#       storage_bay = 30x20     主仓库最多 30 列 20 行");
        sb.AppendLine("#       evidence_box = 24x24    保险箱最多 24×24");
        sb.AppendLine("#");
        sb.AppendLine("#   ⚠ 在这里写了某件容器，就**完全按你写的来**（可以改大，也可以改小），");
        sb.AppendLine("#     不再受这一类容器的默认上限约束。");
        sb.AppendLine("#   ⚠ 改得太小、装不下容器里现有的东西时，这次改动不会生效，游戏日志里会有一条提示。");
        sb.AppendLine("#");
        sb.AppendLine("# 想全部恢复成出厂设置：");
        sb.AppendLine("#   关掉游戏 → 删掉本文件 → 下次进游戏会自动重新生成一份。");
        sb.AppendLine("# ================================================================");
    }

    /// <summary>
    /// 注释模板过期时，用新注释重新落地整个文件 —— **数据按解析出来的值原样重写**。
    /// <para>
    /// 为什么需要它：注释是玩家唯一的使用说明，但它只在"生成新文件"时写一次 ⇒
    /// 老玩家的文件会永远停在他第一次生成那版（也就是我们改进注释前那版）。
    /// 加载时检查模板标记：没有标记 = 旧注释 ⇒ 换新注释 + 留一份 <c>.bak</c>。
    /// </para>
    /// <para>
    /// 语义安全性：重写用的白名单值就是 <c>_fileWhitelist</c> 里刚解析出来的**原文**，
    /// 上限值来自 <c>_perIdLimit</c>（同为刚解析的结果）⇒ 重写前后解析结果完全一致，
    /// 只是注释变新了。顺带保真两件事：① 玩家删掉整行的类别仍然不写（= 继续用出厂名单）；
    /// ② 值留空的类别仍然写成空值（= 继续表示"这一类不能升级"）。
    /// </para>
    /// <para>
    /// 安全边界：① 找不到 <c>[whitelist]</c> 行就**什么都不做**（那是玩家自己写的文件，
    /// 不要碰）；② 写盘走 <c>WriteConfigFile</c> 的 tmp→Move 原子替换；
    /// ③ 只在 <c>.bak</c> 不存在时才备份，保住最早那份原件。
    /// </para>
    /// </summary>
    private static void RefreshTemplateIfOutdated(string path, string[] lines)
    {
        try
        {
            for (int i = 0; i < lines.Length; i++)
                if (lines[i] != null && lines[i].Contains(ConfigTemplateMarker)) return;   // 已是当前模板

            bool hasWhitelistSection = false;
            for (int i = 0; i < lines.Length; i++)
                if (lines[i] != null && lines[i].Trim().ToLowerInvariant().StartsWith("[whitelist"))
                { hasWhitelistSection = true; break; }
            if (!hasWhitelistSection) return;                     // 手工文件：不碰

            var limitLines = new List<string>();
            foreach (var kv in _perIdLimit)
                limitLines.Add(kv.Key + " = " + kv.Value.W + "x" + kv.Value.H);
            limitLines.Sort(StringComparer.Ordinal);

            var sb = new StringBuilder();
            AppendHeader(sb);
            AppendSections(sb,
                k => _fileWhitelist.TryGetValue(k, out var v) ? (v ?? "") : null,
                limitLines);

            string bak = path + ".bak";
            try { if (!File.Exists(bak)) File.Copy(path, bak, false); }
            catch (Exception ex) { CatchLogOnce.Warn("FileConfig:bak", ex); }

            WriteConfigFile(path, sb.ToString());
            TryStampConfigFile(path);                             // 自己写的，别让热重载当成玩家改动
            _log?.Msg("[容器升级] 配置文件的说明文字已更新（你自己的名单和上限一条没动；"
                + "旧文件已备份为 " + Path.GetFileName(bak) + "）");
        }
        catch (Exception ex) { CatchLogOnce.Warn("FileConfig:refreshTemplate", ex); }
    }

    private static void AppendSeedWhitelist(StringBuilder sb, string key, string ids)
    {
        sb.Append(key).Append(" = ").Append(ids ?? "").AppendLine();
    }

    private static void WriteConfigFile(string path, string content)
    {
        string dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);
        string tmp = path + ".tmp";
        File.WriteAllText(tmp, content, new UTF8Encoding(false));
        File.Move(tmp, path, true);   // 同卷原子替换
    }

    private static void TryStampConfigFile(string path)
    {
        try { _configFileStamp = File.GetLastWriteTimeUtc(path).Ticks; } catch { _configFileStamp = -1; }
    }
}
