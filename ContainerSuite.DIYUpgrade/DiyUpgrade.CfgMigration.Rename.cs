// ══════════════════════════════════════════════════════════════════════════
// CFG 迁移 —— 键名重排（主题：面板键名加域号前缀 77→69，并清掉已删键）
//
// 【为什么只能改键名】
//   ModManager 面板顺序 = (段名, 键名) 的字典序，三个注册表都是
//     OrderBy(CategoryId, OrdinalIgnoreCase).ThenBy(EntryId, OrdinalIgnoreCase)
//   （BoolPreferenceRegistry.cs:16 / FloatPreferenceRegistry.cs:16 / HotkeyRegistry.cs:18）。
//   段名动不了 —— ManagerState.CategoryBelongsToMod 用 Normalize()（只留字母数字转小写）
//   比对段名与 DLL 文件名，拆段/改段名会让整段配置从面板里**消失**（静默故障）。
//   ⇒ 想让"同类项相邻 + 开关块与数值块共用同一套域序"，只能给键名加域号前缀。
//
// 【改了哪些】
//   77 个「面板可见」的键名加域号前缀（0_ / 1A_ / 4A_ / 7A_ / z1_ …）；
//   另删掉 8 个「每槽开关」（见 RemovedKeys）。净剩 69 个可见条目。
//   ★ 8 个 `*Ids`（string）**刻意不改名**：它们不进面板（string 且不以 Hotkey 结尾，
//     三个注册表都不收），却是玩家手改的物品ID列表 —— 改名只有"迁移漏掉就静默
//     丢失自定义ID"的风险，没有任何面板收益。
//   ★ 热键 UpgradeHotkey → 21_UpgradeHotkey：仍以 "Hotkey" 结尾，
//     照样被 HotkeyRegistry 收进热键块，不影响识别。
//
// 【为什么删 cfg 里的旧键行不够】
//   MelonPreferences.DefaultFile 文档里的旧条目会在下一次 SaveToFile 被原样写回
//   （2026-10-06 实测 17ms）。⇒ 与既有迁移同一套「三处摘」：
//   内存 Entries + DefaultFile 文档 + 磁盘行。
//
// 【静默失效防线】
//   cfg 里明明有旧键、却一项都没搬动 → 报警且**保留旧键不删**、不写标记
//   （下次启动重试）。绝不为了 cfg 好看而丢用户的值。
// ══════════════════════════════════════════════════════════════════════════
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
    /// <summary>
    /// 旧键名 → 新键名（域号前缀）。**单一事实源** —— 新增/改名配置项时同步这里。
    /// 只含「在面板里仍存在、只是改了名」的键（69 项）；8 个 <c>*Ids</c> 不在此表（刻意不改名），
    /// 8 个「每槽开关」已删除、见 <see cref="RemovedKeys"/>。
    /// </summary>
    internal static readonly Dictionary<string, string> KeyRenames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // ── 热键块（1）──
        { "UpgradeHotkey", "21_UpgradeHotkey" },
        // ── 开关块（35）──
        { "EnableMod", "0_EnableMod" },
        { "0A_MaterialJunk", "1A_MaterialJunk" },
        { "0B_MaterialScrapMetal", "1B_MaterialScrapMetal" },
        { "0C_MaterialMetalIngot", "1C_MaterialMetalIngot" },
        { "2_EnableDragResize", "22_EnableDragResize" },
        { "RestoreOnLoad", "23_RestoreOnLoad" },
        { "SkipXiaoBay", "3A_SkipXiaoBay" },
        { "SkipWageBox", "3B_SkipWageBox" },
        { "SkipVoidBead", "3C_SkipVoidBead" },
        { "BoxEnable", "4A_BoxEnable" },
        { "BackpackEnable", "4B_BackpackEnable" },
        { "CageEnable", "4C_CageEnable" },
        { "MultiSlotEnable", "4D_MultiSlotEnable" },
        { "MultiSlotAlignSlots", "4D_MultiSlotAlignSlots" },
        // ★ 2026-10-07：MultiSlotT0..T3Enable 已**删除**（合并到容器级 Enable），
        //   不在本表；清理见 RemovedKeys。
        { "MultiSlotModuleEnable", "4E_MultiSlotModuleEnable" },
        { "MultiSlotModuleAlignSlots", "4E_MultiSlotModuleAlignSlots" },
        // ★ 同上：MultiSlotModuleT0..T3Enable 已删除。清理见 RemovedKeys。
        { "ModContainerEnable", "5A_ModContainerEnable" },
        { "WageBoxEnable", "5B_WageBoxEnable" },
        { "WageBackpackEnable", "5C_WageBackpackEnable" },
        { "Trashcan_Enable", "6A_Trashcan_Enable" },
        { "Dossier_Enable", "6B_Dossier_Enable" },
        { "CassettePlayer_Enable", "6C_CassettePlayer_Enable" },
        { "ExpandEnabled", "7_ExpandEnabled" },
        { "SaveBagPagingEnabled", "8_SaveBagPagingEnabled" },
        { "ScrollEnabled", "9_ScrollEnabled" },
        { "DiagLog", "z1_DiagLog" },
        { "ShowGrid", "z2_ShowGrid" },
        // ── 数值块（41）──
        { "BackInvHeight", "7A_BackInvHeight" },
        { "BackInvWidth", "7A_BackInvWidth" },
        { "FrontInvHeight", "7B_FrontInvHeight" },
        { "FrontInvWidth", "7B_FrontInvWidth" },
        { "HiddenInvHeight", "7C_HiddenInvHeight" },
        { "HiddenInvWidth", "7C_HiddenInvWidth" },
        { "SaveBagHeight", "7D_SaveBagHeight" },
        { "SaveBagWidth", "7D_SaveBagWidth" },
        { "ShowcaseInvHeight", "7E_ShowcaseInvHeight" },
        { "ShowcaseInvWidth", "7E_ShowcaseInvWidth" },
        { "BackpackMaxH", "4B_BackpackMaxH" },
        { "BackpackMaxW", "4B_BackpackMaxW" },
        { "BoxMaxH", "4A_BoxMaxH" },
        { "BoxMaxW", "4A_BoxMaxW" },
        { "CageHpPerCell", "4C_CageHpPerCell" },
        { "CageMaxH", "4C_CageMaxH" },
        { "CageMaxW", "4C_CageMaxW" },
        { "MultiSlotMaxH", "4D_MultiSlotMaxH" },
        { "MultiSlotMaxW", "4D_MultiSlotMaxW" },
        { "MultiSlotModuleMaxH", "4E_MultiSlotModuleMaxH" },
        { "MultiSlotModuleMaxW", "4E_MultiSlotModuleMaxW" },
        { "ModContainerMaxH", "5A_ModContainerMaxH" },
        { "ModContainerMaxW", "5A_ModContainerMaxW" },
        { "WageBoxMaxH", "5B_WageBoxMaxH" },
        { "WageBoxMaxW", "5B_WageBoxMaxW" },
        { "WageBackpackMaxH", "5C_WageBackpackMaxH" },
        { "WageBackpackMaxW", "5C_WageBackpackMaxW" },
        { "IncH", "5D_IncH" },
        { "IncW", "5D_IncW" },
        { "ModIncH", "5E_ModIncH" },
        { "ModIncW", "5E_ModIncW" },
        { "Trashcan_Height", "6A_Trashcan_Height" },
        { "Trashcan_Width", "6A_Trashcan_Width" },
        { "Dossier_Height", "6B_Dossier_Height" },
        { "Dossier_Width", "6B_Dossier_Width" },
        { "CassettePlayer_Height", "6C_CassettePlayer_Height" },
        { "CassettePlayer_Width", "6C_CassettePlayer_Width" },
        { "SaveBagPageCount", "8A_SaveBagPageCount" },
        { "SaveBagPagingHintDuration", "8B_SaveBagPagingHintDuration" },
        { "ScrollUpRows", "9A_ScrollUpRows" },
        { "ScrollVisibleRows", "9B_ScrollVisibleRows" },
    };

    /// <summary>
    /// **已删除**的配置键（在 cfg 里要清掉、不搬值、不存在于新版本）。
    /// <para>
    /// ① 2026-10-07："每槽独立开关"这一批 —— 核桃要求「多槽 槽0-槽3」「模组槽 模组-待定」
    /// 合并到容器级 Enable（容器启用 = 启用所有格）。8 个键 × 两种命名（改名前 / 改名后）
    /// = 16 个，都要能清掉：直接跨版本升级的用户 cfg 里是旧名，已升过 v1.7.4 的是新名。
    /// </para>
    /// <para>
    /// ② 2026-10-10："诊断期"用过的开关 —— z4_DisableCfgKeepAlive（cfg 保活整体删除）、
    /// z2_JitPrecompileProbe（项目已明令禁止其全量 PrepareMethod 逻辑）。它们不再是任何构建的
    /// 注册键，却以"活段内孤儿键"的形式留在 [CustomInfrastructure] 里：MelonLoader 的 document
    /// 是启动时整份 ParseFile 的快照，Save 会连孤儿键及其说明注释一并写回。故须在此显式清理 ——
    /// 文档层 RemoveEntryFromDocument 会把 TomlValue（注释挂在它身上）整块摘掉。
    /// </para>
    /// <para>
    /// ③ R1Probe / ResizeDiag 探针：只在 <c>-p:Diag=true</c> 构建里注册（见 DiyUpgradeMod.Core.cs
    /// 的 <c>#if DIAG</c>）。Release 产物不注册 ⇒ 也是孤儿键，需清理；但 DIAG 构建下它们是活键，
    /// **不能**清（否则每次启动都会把玩家的探针开关重置回默认），故用 <c>#if !DIAG</c> 包住。
    /// </para>
    /// </summary>
    private static readonly HashSet<string> RemovedKeys = new HashSet<string>(StringComparer.Ordinal)
    {
        // 改名前（≤ v1.7.3）
        "MultiSlotT0Enable", "MultiSlotT1Enable", "MultiSlotT2Enable", "MultiSlotT3Enable",
        "MultiSlotModuleT0Enable", "MultiSlotModuleT1Enable",
        "MultiSlotModuleT2Enable", "MultiSlotModuleT3Enable",
        // 改名后（v1.7.4）
        "4D_MultiSlotT0Enable", "4D_MultiSlotT1Enable",
        "4D_MultiSlotT2Enable", "4D_MultiSlotT3Enable",
        "4E_MultiSlotModuleT0Enable", "4E_MultiSlotModuleT1Enable",
        "4E_MultiSlotModuleT2Enable", "4E_MultiSlotModuleT3Enable",
        // 诊断期遗留（不属于任何构建）
        "z4_DisableCfgKeepAlive", "z2_JitPrecompileProbe",
#if !DIAG
        // 仅 DIAG 构建注册的探针；Release 下是孤儿键
        "24_R1Probe", "25_ResizeDiag",
#endif
    };

    /// <summary>这个键是不是"该从 cfg 里摘掉"的（改名的旧名 + 已删除的键）。</summary>
    private static bool IsDroppedKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        return KeyRenames.ContainsKey(key) || RemovedKeys.Contains(key);
    }

    private static string RenameMarkerPath()
    {
        try { return Path.Combine(MelonEnvironment.UserDataDirectory, "ContainerSuite.renamed20261007"); }
        catch { return null; }
    }

    /// <summary>
    /// 2026-10-07 键名重排迁移。<b>必须在所有配置项注册之后、且排在 MigrateIncKeys 之前调用</b>
    /// （先把「全局 IncW/IncH」搬到 5D_/5E_ 新键，随后 MigrateIncKeys 的合并只会"只升不降"）。
    /// <para>幂等：标记文件存在时只做一次防御性清理（旧键仍可能被别的 mod 的 SaveToFile 写回）。</para>
    /// </summary>
    internal static void MigrateRenamedKeys(MelonPreferences_Category cat)
    {
        if (cat == null) return;
        try
        {
            string marker = RenameMarkerPath();
            string cfgPath = CfgPath();
            string sec = ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId;

            // 已迁移过：旧键仍可能被别的 mod 的 SaveToFile 写回 → 每次启动补一次清理。
            if (!string.IsNullOrEmpty(marker) && File.Exists(marker))
            {
                DropRenamedKeysFromMelon(cat);
                // ★ 只在"真的清理掉了残留旧键"时才出声，避免每次启动刷一行噪音。
                if (RemoveRenamedKeyLines(cfgPath))
                    _log?.Msg("[配置重排] 已是新键名；本次清理掉了被其它 mod 写回的残留旧键。");
                return;
            }

            if (string.IsNullOrEmpty(cfgPath) || !File.Exists(cfgPath))
            {
                // cfg 还没生成（首次安装）→ 没有旧值可搬，写标记免得每次扫。
                if (!string.IsNullOrEmpty(marker)) { try { File.WriteAllText(marker, "1"); } catch { } }
                return;
            }

            Dictionary<string, string> cur = ReadSectionValues(cfgPath, sec);
            int candidates = 0, moved = 0, unmatched = 0, unreadable = 0;
            var unmatchedNames = new List<string>();

            foreach (KeyValuePair<string, string> kv in KeyRenames)
            {
                if (!cur.TryGetValue(kv.Key, out string raw)) continue;
                candidates++;
                MelonPreferences_Entry entry = FindEntry(cat, kv.Value);
                if (entry == null)
                {
                    unmatched++;
                    if (unmatchedNames.Count < 12) unmatchedNames.Add(kv.Key + "→" + kv.Value);
                    continue;
                }
                if (ApplyRawValue(entry, raw)) moved++; else unreadable++;
            }

            if (candidates > 0 && moved == 0)
            {
                // 静默失效防线：旧键有值却一项都没搬动 → 明确报警，且保留旧键、不写标记
                //（下次启动会重试）。绝不为了让 cfg 好看而丢用户的值。
                _log?.Error($"[配置重排] cfg 里检测到 {candidates} 个旧键名，但一项都没能迁移！"
                    + "已保留旧键不删（配置没丢）。请把这个日志发给作者。");
                return;
            }

            if (moved > 0)
            {
                // ★ 必须先摘内存/文档再保存：只删 cfg 文件行的旧键会被 SaveToFile 写回。
                DropRenamedKeysFromMelon(cat);
                try { cat.SaveToFile(); } catch (Exception ex) { CatchLogOnce.Warn(ex); }
                RemoveRenamedKeyLines(cfgPath);
                _log?.Msg($"[配置重排] 已把 {moved}/{candidates} 个旧键名的值搬到新键名"
                    + (unreadable > 0 ? $"（{unreadable} 项格式无法解析，保持默认值）" : "")
                    + (unmatched > 0 ? $"（{unmatched} 个新条目未注册：{string.Join("、", unmatchedNames)}）" : ""));
            }
            else
            {
                // 没有旧键：新用户，或已迁移过（标记被删）。只需清一遍可能的孤儿键。
                DropRenamedKeysFromMelon(cat);
                RemoveRenamedKeyLines(cfgPath);
            }

            if (!string.IsNullOrEmpty(marker)) { try { File.WriteAllText(marker, "1"); } catch { } }
        }
        catch (Exception ex) { _log?.Warning("[配置重排] 迁移失败（不影响使用，按默认值启动）：" + ex.Message); }
    }

    /// <summary>三处摘旧键：① 内存 cat.Entries ② DefaultFile 文档 ③ 磁盘 cfg 文件（③ 在 RemoveRenamedKeyLines）。
    /// 摘的是 <see cref="IsDroppedKey"/> 判定的全部键：改名的旧名 + 已删除的键。</summary>
    private static void DropRenamedKeysFromMelon(MelonPreferences_Category cat)
    {
        // ① 内存条目列表（新版本用新键名注册，旧键本来就不在；纯防御）
        try
        {
            System.Collections.IList list = cat.Entries as System.Collections.IList;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    MelonPreferences_Entry e = list[i] as MelonPreferences_Entry;
                    if (e != null && IsDroppedKey(e.Identifier)) list.RemoveAt(i);
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }

        // ② DefaultFile 文档（SaveToFile 的数据源；孤儿旧键就住在这里）
        try
        {
            FieldInfo fi = typeof(MelonPreferences).GetField("DefaultFile",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object doc = fi == null ? null : fi.GetValue(null);
            if (doc != null)
            {
                MethodInfo mi = doc.GetType().GetMethod("RemoveEntryFromDocument",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(string), typeof(string) }, null);
                if (mi != null)
                {
                    string sec = ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId;
                    foreach (string k in KeyRenames.Keys)
                    {
                        try { mi.Invoke(doc, new object[] { sec, k }); } catch { }
                    }
                    foreach (string k in RemovedKeys)
                    {
                        try { mi.Invoke(doc, new object[] { sec, k }); } catch { }
                    }
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    /// <summary>
    /// 从 cfg 文件里删掉旧键行。★ 只在 [CustomInfrastructure] 段内删 ——
    /// 键名如 "EnableMod" / "ShowGrid" 可能被别的 mod 在同名段以外的段里用，
    /// 越段删会误伤别人。
    /// </summary>
    /// <returns>是否真的删掉了至少一行。</returns>
    private static bool RemoveRenamedKeyLines(string path)
    {
        if (string.IsNullOrEmpty(path) || !File.Exists(path)) return false;
        string sec = ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId;
        bool changed = false;
        var output = new List<string>();
        bool inSection = false;

        foreach (string raw in File.ReadAllLines(path))
        {
            string line = raw.Trim();
            if (line.StartsWith("[") && line.EndsWith("]"))
            {
                inSection = line.Substring(1, line.Length - 2).Trim().Equals(sec, StringComparison.Ordinal);
                output.Add(raw);
                continue;
            }
            if (inSection)
            {
                int eq = line.IndexOf('=');
                if (eq > 0)
                {
                    string key = line.Substring(0, eq).Trim().Trim('"');
                    if (IsDroppedKey(key)) { changed = true; continue; }
                }
            }
            output.Add(raw);
        }

        if (changed) File.WriteAllLines(path, output);
        return changed;
    }
}