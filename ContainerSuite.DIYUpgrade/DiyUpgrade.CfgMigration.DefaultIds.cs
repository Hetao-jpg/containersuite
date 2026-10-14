// CFG 迁移 —— 物品ID白名单补齐（代码默认值只在条目首建时生效，故须给老玩家追加）。
// 语义（只增不减）/ 已知取舍（删掉的默认ID会被加回）/ 幂等：见 docs/cfg-migration.md
using System;
using System.Collections.Generic;
using ContainerSuite.Shared;
using MelonLoader;
using MelonLoader.Preferences;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    /// <summary>
    /// 8 个 <c>*Ids</c> 键的**代码默认值** —— 单一事实源。
    /// Core.cs 的 CreateGridDef 也从这里取（<see cref="DefaultIdsOf"/>），
    /// 改一处即两处同步，杜绝"默认值 / 补齐逻辑"各自漂移。
    /// </summary>
    internal static readonly KeyValuePair<string, string>[] DefaultIdLists =
    {
        new KeyValuePair<string, string>("BoxIds",
            "sec_box,evidence_box,storage_bay,storage_bay_large,smuggler_bay,smuggler_bay_mod,"
            + "machine_bay,machine_bay_ext,service_box,med_box,eng_box,storage_bay_chem,"
            + "storage_bay_gun,smuggler_bay_mini,makeshift_storage_bay,expedition_box"),
        new KeyValuePair<string, string>("BackpackIds",
            "backpack_small,backpack_medium,backpack_large,satchel,fanny_pack,backpack_medium_military,backpack_large_military"),
        // 查重：原默认串是 "animal_cage,Rat_Cage,rat_cage"，Rat_Cage 与 rat_cage
        // 仅大小写不同、经 IsAnyId 命中同一个物品，属历史冗余；且 rat_cage 并非
        // 游戏内实际存在的容器 id（2026-10-13 确认后一并删除）。新默认值只留 animal_cage。
        // 2026-10-14：新增 ratlin_animal_cage（鼠布林老鼠笼）——鼠布林 mod 添加的笼子，
        //   与原版 animal_cage 同构，按核桃要求纳入同一个 CageIds 白名单。
        new KeyValuePair<string, string>("CageIds", "animal_cage,ratlin_animal_cage"),
        new KeyValuePair<string, string>("MultiSlotIds", "fridge,recharger,vacuum_robot"),
        // 2026-10-08：新增水瓶打印机。它的产出格是 MachineBottlePrinter 机器窗口里的
        // GameGridInventory，与 water_purifier 同构 → 归 4E 模组槽，不归 4D 多槽容器。
        new KeyValuePair<string, string>("MultiSlotModuleIds",
            "furnace,desequencer,security_alarm,mirage_projector,water_purifier,"
            + "moisture_farm,wine_rack,bottle_printer"),
        // Wage 三项直接引用注册表的常量，避免同一串 ID 在两处各写一遍。
        new KeyValuePair<string, string>("WageBoxIds", WageContainerRegistry.IdWageBox),
        new KeyValuePair<string, string>("WageBackpackIds", WageContainerRegistry.IdVoidBead),
        new KeyValuePair<string, string>("ModContainerIds",
            WageContainerRegistry.IdGuMachine + "," + WageContainerRegistry.IdAiGenerator),
    };

    /// <summary>
    /// 取某个 <c>*Ids</c> 键的代码默认值（传前缀，不带 <c>Ids</c> 后缀，
    /// 例如 <c>DefaultIdsOf("MultiSlotModule")</c>）。未登记的前缀返回空串。
    /// </summary>
    internal static string DefaultIdsOf(string prefix)
    {
        string key = (prefix ?? "") + "Ids";
        for (int i = 0; i < DefaultIdLists.Length; i++)
            if (string.Equals(DefaultIdLists[i].Key, key, StringComparison.Ordinal))
                return DefaultIdLists[i].Value;
        return "";
    }

    /// <summary>
    /// 8B 条目 ——「不要再自动补回来」的 id 清单（详见 <see cref="CfgKeys.DefaultIdsVeto"/>）。
    /// </summary>
    private static MelonPreferences_Entry<string> _defaultIdsVeto;

    /// <summary>已否决的 id（忽略大小写）。只影响"自动追加"，不影响你已经写在 *Ids 里的东西。</summary>
    private static readonly HashSet<string> _vetoIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private const string DefaultIdsVetoComment =
        "「不要再自动补回来」的物品id（多个用逗号/分号/空白隔开），例：rat_cage。"
        + "本套件每次启动都会把「代码默认清单里有、而你的 *Ids 里没有」的 id 自动追加回去"
        + "（为的是版本新增的容器不会被静默漏掉）——副作用是你**有意删掉**的 id 也会被加回来。"
        + "把那些 id 写到这里即可永久豁免：它们不会再被自动追加。留空 = 不否决（默认）。"
        + "★ 它只拦「自动追加」，不会删你 *Ids 里已有的东西；改完保存，下次启动生效。";

    /// <summary>
    /// 注册 <c>8B_DefaultIdsVeto</c>。必须在 <c>EnsureDefaultIds</c> 之前调用
    /// （否决清单要参与那一次补齐），且在本文件所有条目之后 —— 让它排 8A 之后。
    /// </summary>
    internal static void RegisterDefaultIdsVeto(MelonPreferences_Category cat)
    {
        if (cat == null) return;
        try
        {
            _defaultIdsVeto = cat.CreateEntry<string>(CfgKeys.DefaultIdsVeto, "",
                (string)null, DefaultIdsVetoComment,
                false, false, (ValueValidator)null, (string)null);

            // 宽容采纳：它也是"最后一个注册的条目"之一，此刻全部条目都已存在。
            CfgDoctor.ApplyTo(cat);

            ((MelonEventBase<LemonAction<string, string>>)(object)_defaultIdsVeto.OnEntryValueChanged)
                .Subscribe((LemonAction<string, string>)delegate { LoadVetoIds(); }, 0, false);

            LoadVetoIds();
            LogVetoSummary();
        }
        catch (Exception ex) { CatchLogOnce.Warn("DefaultIdsVeto:register", ex); }
    }

    /// <summary>
    /// 启动 Summary。★ **无条件输出**（不进 DiagLog 闸门）：与 8A 的
    /// <c>LogPerIdLimitsSummary</c> 同一套路 —— "这份配置到底配了什么"是
    /// "我改了配置怎么没生效"类问题的第一现场，默认静默等于把唯一线索藏起来。
    /// </summary>
    private static void LogVetoSummary()
    {
        try
        {
            if (_vetoIds.Count == 0)
            {
                _log?.Msg("[容器升级] 8B 否决清单为空 → 默认清单里你以前删掉的 id 仍会被自动补回"
                          + "（要永久排除就写进 " + CfgKeys.DefaultIdsVeto + "）");
                return;
            }
            var sb = new System.Text.StringBuilder();
            foreach (string id in _vetoIds)
            {
                if (sb.Length > 0) sb.Append('、');
                sb.Append(id);
            }
            _log?.Msg($"[容器升级] 8B 否决清单（这些 id 不会再被自动补回 *Ids）共 {_vetoIds.Count} 个：{sb}");
        }
        catch (Exception ex) { CatchLogOnce.Warn("DefaultIdsVeto:summary", ex); }
    }

    /// <summary>解析 8B 原文进 <see cref="_vetoIds"/>。幂等，可反复调用。</summary>
    private static void LoadVetoIds()
    {
        _vetoIds.Clear();
        try
        {
            string raw = (_defaultIdsVeto != null) ? _defaultIdsVeto.Value : null;
            if (string.IsNullOrWhiteSpace(raw)) return;
            string[] parts = raw.Split(
                new char[] { ',', '，', ';', '；', ' ', '\t', '\r', '\n' },
                StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string id = parts[i].Trim().ToLowerInvariant();
                if (id.Length > 0) _vetoIds.Add(id);
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("DefaultIdsVeto:parse", ex); }
    }

    /// <summary>
    /// 把「代码默认值里有、用户值里没有」的物品 ID 追加进各 <c>*Ids</c> 白名单，并折叠重复 token。
    /// <b>只增不减</b>；由 <c>DiyUpgradeMod.Init</c> 在 <c>MigrateDiagKeys</c> 之后调用，落盘由紧随的
    /// <c>cat.SaveToFile(true)</c> 负责。详见 docs/cfg-migration.md。
    /// </summary>
    internal static void EnsureDefaultIds(MelonPreferences_Category cat)
    {
        if (cat == null) return;
        try
        {
            var changes = new List<string>();
            var failures = new List<string>();

            for (int i = 0; i < DefaultIdLists.Length; i++)
            {
                string key = DefaultIdLists[i].Key;
                MelonPreferences_Entry entry = FindEntry(cat, key);
                // 条目没注册 = 这类容器在本机不存在（如没装蛙哥时 Wage 三项不建）。
                //   这是正常状态，不是失败。
                if (entry == null) continue;

                var se = entry as MelonPreferences_Entry<string>;
                if (se == null) { failures.Add(key + "（条目类型不是 string）"); continue; }

                string cur;
                try { cur = se.Value; }
                catch (Exception ex) { CatchLogOnce.Warn("EnsureDefaultIds:" + key, ex); failures.Add(key + "（读取失败）"); continue; }

                // 用户主动清空 = 明确表示"这一类我不要"。绝不拿默认值去覆盖它。
                if (string.IsNullOrWhiteSpace(cur)) continue;

                string merged; int added; int collapsed; int vetoed;
                if (!MergeMissingIds(cur, DefaultIdLists[i].Value, out merged, out added, out collapsed, out vetoed)) continue;

                if (added == 0 && collapsed == 0) continue;   // 幂等：绝大多数启动走这里，不写不吵

                try { se.Value = merged; }
                catch (Exception ex) { CatchLogOnce.Warn("EnsureDefaultIds:" + key, ex); failures.Add(key + "（写入失败）"); continue; }

                changes.Add(key
                    + (added > 0 ? $" 追加 {added} 个ID（{DescribeMissing(cur, DefaultIdLists[i].Value)}）" : "")
                    + (collapsed > 0 ? $" 折叠 {collapsed} 个重复ID" : "")
                    + (vetoed > 0 ? $" 按 8B 否决清单跳过 {vetoed} 个" : ""));
            }

            if (failures.Count > 0)
                _log?.Error("[配置补齐] ⚠ 以下物品ID白名单未能补齐（新容器可能认不出来）："
                    + string.Join("、", failures));

            if (changes.Count > 0)
                _log?.Msg("[配置补齐] 已把代码默认新增的物品ID追加进 " + changes.Count + " 个白名单："
                    + string.Join("；", changes)
                    + "。只追加、不删除：你原有的ID一个都没动"
                    + "（注意：默认清单里你以前手动删掉的ID，这次会被加回来 —— "
                    + "不想让它加回来就写进 " + CfgKeys.DefaultIdsVeto + "）。");
        }
        catch (Exception ex)
        {
            _log?.Warning("[配置补齐] 补齐失败（不影响使用，按现有白名单启动）：" + ex.Message);
        }
    }

    /// <summary>
    /// 合并算法：折叠重复 token + 追加缺失的默认 ID。用户值非空时只做这两件事，
    /// **不删除任何用户有意保留的 token**；<paramref name="vetoed"/> 回报被 8B 清单拦下的个数。
    /// </summary>
    private static bool MergeMissingIds(string userValue, string defaultCsv,
        out string merged, out int added, out int collapsed, out int vetoed)
    {
        merged = userValue;
        added = 0;
        collapsed = 0;
        vetoed = 0;

        string[] userIds = IdMatch.SplitIds(userValue ?? "");
        string[] defIds = IdMatch.SplitIds(defaultCsv ?? "");
        if (defIds.Length == 0) return true;

        // ── ① 折叠用户值里的重复 token ──
        //   index 用 OrdinalIgnoreCase 判定"是不是同一个物品"，但 kept 里存的是
        //   **原文**：这样 "Rat_Cage" 与 "rat_cage" 这种仅大小写不同的成对项会被折叠
        //   成一个，且**一律保留全小写的那个** —— 游戏内 identifier 全是小写
        //   （见 WageContainerRegistry.NormalizeId / 蛙哥 IsBottlePrinter 都做
        //   ToLowerInvariant），保留大写形式有可能匹配不上 item.IsID。
        var index = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var kept = new List<string>();
        for (int i = 0; i < userIds.Length; i++)
        {
            string id = userIds[i];
            if (id.Length == 0) continue;
            int at;
            if (index.TryGetValue(id, out at))
            {
                collapsed++;
                if (IsAllLower(id) && !IsAllLower(kept[at])) kept[at] = id;
                continue;
            }
            index[id] = kept.Count;
            kept.Add(id);
        }

        // ── ② 追加代码默认值里有、用户值里没有的 ID ──
        var missing = new List<string>();
        for (int i = 0; i < defIds.Length; i++)
        {
            string id = defIds[i];
            if (id.Length == 0 || index.ContainsKey(id)) continue;
            // ★ 8B 否决清单：玩家**有意删掉**的 id 不再自动加回（2026-10-14）。
            //   语义只有一条："这些 id 不要自动追加"。它绝不删除 *Ids 里已有的东西。
            if (_vetoIds.Contains(id)) { vetoed++; continue; }
            index[id] = kept.Count + missing.Count;
            missing.Add(id);
        }
        added = missing.Count;

        if (added == 0 && collapsed == 0) return true;

        var sb = new List<string>(kept);
        for (int i = 0; i < missing.Count; i++) sb.Add(missing[i]);
        merged = string.Join(",", sb);
        return true;
    }

    /// <summary>是否全为小写 ASCII 字母/数字/下划线（用于挑选大小写成对项里的规范形式）。</summary>
    private static bool IsAllLower(string s)
    {
        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];
            if (c >= 'A' && c <= 'Z') return false;
        }
        return true;
    }

    /// <summary>只用于日志：列出这次追加了哪些 ID（最多 5 个，超出用省略号）。</summary>
    private static string DescribeMissing(string userValue, string defaultCsv)
    {
        var have = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (string id in IdMatch.SplitIds(userValue ?? "")) have.Add(id);

        var added = new List<string>();
        foreach (string id in IdMatch.SplitIds(defaultCsv ?? ""))
        {
            if (id.Length == 0 || have.Contains(id)) continue;
            if (added.Count < 5) added.Add(id);
        }
        return added.Count > 0 ? string.Join("、", added) : "";
    }
}