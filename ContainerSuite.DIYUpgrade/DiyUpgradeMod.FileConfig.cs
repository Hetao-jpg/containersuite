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
        sb.AppendLine(ConfigVersionLine);
        sb.AppendLine("# ContainerSuite 容器升级配置（改完存盘，游戏内 2 秒内自动生效，不用重启）");
        sb.AppendLine("#");
        sb.AppendLine("# [whitelist] 每行 `类别 = 物品id, 物品id, ...`");
        sb.AppendLine("#   类别可用：" + string.Join(" / ", WhitelistKeys));
        sb.AppendLine("#   ★ 这里声明过的类别以本文件为准，MelonPreferences.cfg 里的 *Ids 不再生效；");
        sb.AppendLine("#     没声明的类别仍用 cfg。想「恢复默认」就整行删掉（不是留空）。");
        sb.AppendLine("#   ★ 蛙哥的妙妙箱 / 虚空珠袋、以及 Mod 容器不在这里 —— 它们靠固定 id 匹配，");
        sb.AppendLine("#     归「避让」开关管；要单独给它们改尺寸上限，用下面的 [limits] 写 id。");
        sb.AppendLine("#");
        sb.AppendLine("# [limits] 每行 `物品id = 宽x高`（1~100）");
        sb.AppendLine("#   只对写在这里的 id 生效，**完全覆盖**该类别在 cfg 里的 MaxW/MaxH（可增可减）；");
        sb.AppendLine("#   没写的 id 仍用类别上限。缩小到装不下容器里现有物品时不会生效，日志会提示。");
        sb.AppendLine();
        sb.AppendLine("[whitelist]");
        AppendSeedWhitelist(sb, "box", _box.Ids.Value);
        AppendSeedWhitelist(sb, "backpack", _backpack.Ids.Value);
        AppendSeedWhitelist(sb, "cage", _cage.Ids.Value);
        AppendSeedWhitelist(sb, "multislot", _multiSlot.Ids.Value);
        AppendSeedWhitelist(sb, "multislotmodule", _multiSlotModule.Ids.Value);
        sb.AppendLine();
        sb.AppendLine("[limits]");
        sb.AppendLine("# 例：让某一种箱子单独放宽 / 收紧（其余箱子仍用类别上限）");
        sb.AppendLine("# storage_bay = 30x20");
        sb.AppendLine("# evidence_box = 24x24");
        sb.AppendLine("# 蛙哥容器也可写（固定 id）：custom_storage_box / void_bead_storage / wage_gu_machine / wage_ai_generator");

        WriteConfigFile(path, sb.ToString());
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
