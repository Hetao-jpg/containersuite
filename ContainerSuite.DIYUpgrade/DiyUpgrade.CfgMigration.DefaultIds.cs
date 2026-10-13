// ══════════════════════════════════════════════════════════════════════════
// CFG 迁移 —— 物品ID白名单补齐（主题：代码默认新增了 ID，老玩家 cfg 里存着旧串）
//
// 【为什么需要它】
//   8 个 `*Ids` 是玩家手改的字符串条目，CreateEntry 的默认值**只在条目首次
//   创建时生效**。老玩家 cfg 里早已有 MultiSlotModuleIds = "<旧默认>"，
//   所以把代码默认串加上新 ID 对老玩家是**零效果** —— 白名单认不出新容器，
//   MatchCategory 直接返回 Cat.None，功能静默失效（本项目最忌讳的一类）。
//   （KeyRenames 里那 8 个 *Ids 刻意不改名，正是因为它们靠"玩家手改"工作。）
//
// 【语义：只追加 + 折叠重复，绝不删改玩家已有的 ID】
//   · 追加「代码默认值里有、用户值里没有」的 ID；
//   · 折叠用户值里的重复 token（IsAnyId 命中即返回，重复项是死数据）；
//     仅大小写不同的成对项（历史遗留的 Rat_Cage / rat_cage）折叠成**全小写**那个
//     —— 游戏内 identifier 一律小写，保留大写形式可能匹配不上 item.IsID；
//   · ★ 绝不**删除**用户值里已有的任何 ID —— 玩家可能自己加过自定义容器，
//     也可能主动删掉某个默认容器（不想让别的 mod 抢它的尺寸主权）。做成
//     "默认值覆盖用户值"会把玩家手改过的白名单整条冲掉，是灾难级静默失效。
//   · 代码默认值自身也已去重（CageIds 原本是 animal_cage,Rat_Cage,rat_cage），
//     新装用户不会再拿到这对重复。
//
// 【已知取舍：删掉的默认ID会被加回来】
//   本函数算的是「默认值 ∪ 用户值」，所以玩家**主动删掉**的某个默认容器，
//   下次启动会被重新追加回去（这是"默认值集合只增不减"的必然结果，也让新功能
//   不会被静默漏掉 —— 方向上宁可多给不可少给）。日志会逐项列出补了什么，
//   **不是静默的**；要永久排除某容器，删掉后留意该行日志。
//   反向（连默认值新增的都尊重玩家删除）需要给每个 ID 记"用户已否决"状态，
//   代价与复杂度都不值 —— 记在 TODO 里，需要时再加。
//
// 【幂等】补齐后 added==0 且 collapsed==0 → 不写盘、不打日志（每次启动只扫一次）。
// 【静默失效防线】写入失败 → Error 级报警，绝不静默吞掉。
// ══════════════════════════════════════════════════════════════════════════
using System;
using System.Collections.Generic;
using ContainerSuite.Shared;
using MelonLoader;

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
        // 仅大小写不同、经 IsAnyId 命中同一个物品，属历史冗余。新默认值只留小写规范形式。
        new KeyValuePair<string, string>("CageIds", "animal_cage,rat_cage"),
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
    /// 把「代码默认值里有、用户值里没有」的物品 ID 追加进各 <c>*Ids</c> 白名单，
    /// 并折叠用户值里完全相同的重复 token。<b>只增不减</b>。
    /// <para>
    /// 由 <c>DiyUpgradeMod.Init</c> 在 <c>MigrateDiagKeys</c> 之后调用（配置项都已注册）。
    /// 紧随其后的 <c>cat.SaveToFile(true)</c> 负责落盘，本函数自己不写盘。
    /// </para>
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

                string merged; int added; int collapsed;
                if (!MergeMissingIds(cur, DefaultIdLists[i].Value, out merged, out added, out collapsed)) continue;

                if (added == 0 && collapsed == 0) continue;   // 幂等：绝大多数启动走这里，不写不吵

                try { se.Value = merged; }
                catch (Exception ex) { CatchLogOnce.Warn("EnsureDefaultIds:" + key, ex); failures.Add(key + "（写入失败）"); continue; }

                changes.Add(key
                    + (added > 0 ? $" 追加 {added} 个ID（{DescribeMissing(cur, DefaultIdLists[i].Value)}）" : "")
                    + (collapsed > 0 ? $" 折叠 {collapsed} 个重复ID" : ""));
            }

            if (failures.Count > 0)
                _log?.Error("[配置补齐] ⚠ 以下物品ID白名单未能补齐（新容器可能认不出来）："
                    + string.Join("、", failures));

            if (changes.Count > 0)
                _log?.Msg("[配置补齐] 已把代码默认新增的物品ID追加进 " + changes.Count + " 个白名单："
                    + string.Join("；", changes)
                    + "。只追加、不删除：你原有的ID一个都没动"
                    + "（注意：默认清单里你以前手动删掉的ID，这次会被加回来）。");
        }
        catch (Exception ex)
        {
            _log?.Warning("[配置补齐] 补齐失败（不影响使用，按现有白名单启动）：" + ex.Message);
        }
    }

    /// <summary>
    /// 合并算法：折叠重复 token + 追加缺失的默认 ID。用户值非空时只做这两件事，
    /// **不删除任何用户有意保留的 token**。
    /// </summary>
    private static bool MergeMissingIds(string userValue, string defaultCsv,
        out string merged, out int added, out int collapsed)
    {
        merged = userValue;
        added = 0;
        collapsed = 0;

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