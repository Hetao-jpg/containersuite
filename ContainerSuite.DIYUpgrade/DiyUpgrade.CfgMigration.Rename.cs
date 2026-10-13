// CFG 迁移 —— 键名重排（面板键名加域号前缀，并清掉已删键）。
// 为什么只能改键名（段名动不得）/ 改了哪些 / 静默失效防线：见 docs/cfg-migration.md
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
    /// 旧键名 → 新键名。**单一事实源** —— 新增/改名配置项时必须同步这里。
    /// 收录历史上出现过的全部键名（含两代旧名）+ 上一代现名，统一指向当前名；新名一侧以
    /// <see cref="ContainerSuite.Shared.CfgKeys"/> 常量为准。详见 docs/cfg-migration.md。
    /// </summary>
    internal static readonly Dictionary<string, string> KeyRenames = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        // ── 0_ 套件总开关 ──
        // ── 1_ 容器升级·全局（材料 → 操作 → 增量） ──
        { "0A_MaterialJunk", "1A_MaterialJunk" },
        { "0B_MaterialScrapMetal", "1B_MaterialScrapMetal" },
        { "0C_MaterialMetalIngot", "1C_MaterialMetalIngot" },
        { "21_UpgradeHotkey", "1D_UpgradeHotkey" },
        { "2_EnableDragResize", "1E_EnableDragResize" },
        { "22_EnableDragResize", "1E_EnableDragResize" },
        { "23_RestoreOnLoad", "1F_RestoreOnLoad" },
        { "26_EnableGroupDrag", "1G_EnableGroupDrag" },
        { "4C_CageHpPerCell", "1H_CageHpPerCell" },
        { "5D_IncH", "1I_IncH" },
        { "5D_IncW", "1I_IncW" },
        { "5E_ModIncH", "1J_ModIncH" },
        { "5E_ModIncW", "1J_ModIncW" },
        // ── 2_ 仓库扩容·总开关与各仓库宽高 ──
        { "7A_BackInvHeight", "2A_BackInvHeight" },
        { "7A_BackInvWidth", "2A_BackInvWidth" },
        { "7B_FrontInvHeight", "2B_FrontInvHeight" },
        { "7B_FrontInvWidth", "2B_FrontInvWidth" },
        { "7C_HiddenInvHeight", "2C_HiddenInvHeight" },
        { "7C_HiddenInvWidth", "2C_HiddenInvWidth" },
        { "7D_SaveBagHeight", "2D_SaveBagHeight" },
        { "7D_SaveBagWidth", "2D_SaveBagWidth" },
        { "7E_ShowcaseInvHeight", "2E_ShowcaseInvHeight" },
        { "7E_ShowcaseInvWidth", "2E_ShowcaseInvWidth" },
        { "7F_AfterhourHeight", "2F_AfterhourHeight" },
        { "7F_AfterhourWidth", "2F_AfterhourWidth" },
        { "7G_GroundDragEnabled", "2G_GroundDragEnabled" },
        { "7G_GroundOffsetX", "2G_GroundOffsetX" },
        { "7G_GroundOffsetY", "2G_GroundOffsetY" },
        { "7_ExpandEnabled", "2_ExpandEnabled" },
        // ── 3_ 主仓库分页 ──
        { "8A_SaveBagPageCount", "3A_SaveBagPageCount" },
        { "8B_SaveBagPagingHintDuration", "3B_SaveBagPagingHintDuration" },
        { "8_SaveBagPagingEnabled", "3_SaveBagPagingEnabled" },
        // ── 4_ 主仓库滚动 ──
        { "9A_ScrollUpRows", "4A_ScrollUpRows" },
        { "9B_ScrollVisibleRows", "4B_ScrollVisibleRows" },
        { "9_ScrollEnabled", "4_ScrollEnabled" },
        // ── 5_ 基础设施容器 ──
        { "Trashcan_Enable", "5A_Trashcan_Enable" },
        { "6A_Trashcan_Enable", "5A_Trashcan_Enable" },
        { "Trashcan_Height", "5A_Trashcan_Height" },
        { "6A_Trashcan_Height", "5A_Trashcan_Height" },
        { "Trashcan_Width", "5A_Trashcan_Width" },
        { "6A_Trashcan_Width", "5A_Trashcan_Width" },
        { "Dossier_Enable", "5B_Dossier_Enable" },
        { "6B_Dossier_Enable", "5B_Dossier_Enable" },
        { "Dossier_Height", "5B_Dossier_Height" },
        { "6B_Dossier_Height", "5B_Dossier_Height" },
        { "Dossier_Width", "5B_Dossier_Width" },
        { "6B_Dossier_Width", "5B_Dossier_Width" },
        { "CassettePlayer_Enable", "5C_CassettePlayer_Enable" },
        { "6C_CassettePlayer_Enable", "5C_CassettePlayer_Enable" },
        { "CassettePlayer_Height", "5C_CassettePlayer_Height" },
        { "6C_CassettePlayer_Height", "5C_CassettePlayer_Height" },
        { "CassettePlayer_Width", "5C_CassettePlayer_Width" },
        { "6C_CassettePlayer_Width", "5C_CassettePlayer_Width" },
        // ── 6_ 跨 mod 避让 ──
        { "3A_SkipXiaoBay", "6A_SkipXiaoBay" },
        { "3B_SkipWageBox", "6B_SkipWageBox" },
        { "3C_SkipVoidBead", "6C_SkipVoidBead" },
        { "3D_SweepZombieHandles", "6D_SweepZombieHandles" },
        // ── 7_ 容器升级·分类上限 ──
        { "4A_BoxEnable", "7A_BoxEnable" },
        { "4A_BoxMaxH", "7A_BoxMaxH" },
        { "4A_BoxMaxW", "7A_BoxMaxW" },
        { "4B_BackpackEnable", "7B_BackpackEnable" },
        { "4B_BackpackMaxH", "7B_BackpackMaxH" },
        { "4B_BackpackMaxW", "7B_BackpackMaxW" },
        { "4C_CageEnable", "7C_CageEnable" },
        { "4C_CageMaxH", "7C_CageMaxH" },
        { "4C_CageMaxW", "7C_CageMaxW" },
        { "4D_MultiSlotAlignSlots", "7D_MultiSlotAlignSlots" },
        { "4D_MultiSlotEnable", "7D_MultiSlotEnable" },
        { "4D_MultiSlotMaxH", "7D_MultiSlotMaxH" },
        { "4D_MultiSlotMaxW", "7D_MultiSlotMaxW" },
        { "4E_MultiSlotModuleAlignSlots", "7E_MultiSlotModuleAlignSlots" },
        { "4E_MultiSlotModuleEnable", "7E_MultiSlotModuleEnable" },
        { "4E_MultiSlotModuleMaxH", "7E_MultiSlotModuleMaxH" },
        { "4E_MultiSlotModuleMaxW", "7E_MultiSlotModuleMaxW" },
        { "5A_ModContainerEnable", "7F_ModContainerEnable" },
        { "5A_ModContainerMaxH", "7F_ModContainerMaxH" },
        { "5A_ModContainerMaxW", "7F_ModContainerMaxW" },
        { "5B_WageBoxEnable", "7G_WageBoxEnable" },
        { "5B_WageBoxMaxH", "7G_WageBoxMaxH" },
        { "5B_WageBoxMaxW", "7G_WageBoxMaxW" },
        { "5C_WageBackpackEnable", "7H_WageBackpackEnable" },
        { "5C_WageBackpackMaxH", "7H_WageBackpackMaxH" },
        { "5C_WageBackpackMaxW", "7H_WageBackpackMaxW" },
        // ── z_ 诊断与实验 ──
        { "z2_CIReadLoadWarmup", "z3_CIReadLoadWarmup" },
        { "z3_DisableLoadRestore", "z4_DisableLoadRestore" },
        { "27_SyncModifiedStateProbe", "z9_SyncModifiedStateProbe" },
    };

    /// <summary>
    /// **已删除**的配置键（在 cfg 里要清掉、不搬值、不存在于新版本）。
    /// 三类：每槽独立开关（8×2 命名）、诊断期遗留开关、仅 DIAG 构建注册的探针（Release 下为孤儿键）。
    /// 详见 docs/cfg-migration.md。
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
        try { return Path.Combine(MelonEnvironment.UserDataDirectory, "ContainerSuite.renamed20261013"); }
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
