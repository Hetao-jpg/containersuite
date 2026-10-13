// CFG 迁移 —— 旧键合并：① 升级增量「每类别每槽」→ 全局两级；② 诊断日志开关「三个 → 一个」。
// 共用工具见基座文件 DiyUpgrade.CfgMigration.cs；理由见 docs/cfg-migration.md
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ContainerSuite.Shared;
using MelonLoader;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    // ① 升级增量「每类别每槽」→ 全局两级（为什么必须搬 / 取 max 而非 last-wins）：
    //   见 docs/cfg-migration.md
    internal static void MigrateIncKeys(MelonPreferences_Category cat)
    {
        if (cat == null) return;
        try
        {
            var incMax = new Dictionary<string, int>(StringComparer.Ordinal);
            bool any = false;

            // 当前合并段 [CustomInfrastructure]（已在新版但旧键还在的用户）
            string cfgPath = CfgPath();
            if (!string.IsNullOrEmpty(cfgPath) && System.IO.File.Exists(cfgPath))
                CollectInc(ReadSectionValues(cfgPath, ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId), incMax, ref any);

            if (!any) return;

            // 键名重排：这里传的仍是**旧名**（incMax 就是用旧名做键的，见 CollectInc），
            //   由 ApplyInc 内部翻译成新的排序键名去 FindEntry。
            ApplyInc(cat, "IncW", incMax);
            ApplyInc(cat, "IncH", incMax);
            ApplyInc(cat, "ModIncW", incMax);
            ApplyInc(cat, "ModIncH", incMax);

            // 旧值已并入全局键后，把 28 个旧「每槽」键从内存/文档/磁盘三处清掉，
            // 否则 MelonPreferences 按文档重写全文件时会把它们当孤儿键写回来。
            CleanupLegacyIncKeys(cat);

            _log?.Msg("[配置合并] 已将旧版「每类别每槽」的升级增量合并为全局 IncW/IncH（原版容器）与 ModIncW/ModIncH（Mod 容器）。");
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    private static readonly string[] _oldIncPrefixes = { "Box", "Backpack", "Cage", "MultiSlot", "MultiSlotModule", "WageBox", "WageBackpack", "ModContainer" };

    /// <summary>判断一个键是不是旧版「每槽 IncW/IncH」（<前缀>T&lt;槽位&gt;IncW/H 结构）。</summary>
    private static bool IsLegacyIncKey(string key)
    {
        if (string.IsNullOrEmpty(key)) return false;
        if (!key.EndsWith("IncW", StringComparison.Ordinal) && !key.EndsWith("IncH", StringComparison.Ordinal)) return false;
        foreach (var p in _oldIncPrefixes)
        {
            // 前缀后必须紧跟 'T'（槽位分隔），避免误删 "BoxEnable" 等无关键
            if (key.Length > p.Length && key.StartsWith(p, StringComparison.Ordinal) && key[p.Length] == 'T')
                return true;
        }
        return false;
    }

    /// <summary>三处摘旧键：① 内存 cat.Entries（SaveToFile 数据源，主力）② DefaultFile 文档（兜底）③ 磁盘 cfg 文件。</summary>
    private static void CleanupLegacyIncKeys(MelonPreferences_Category cat)
    {
        // ① 内存条目列表
        try
        {
            System.Collections.IList list = cat.Entries as System.Collections.IList;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    MelonPreferences_Entry e = list[i] as MelonPreferences_Entry;
                    if (e != null && IsLegacyIncKey(e.Identifier)) list.RemoveAt(i);
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }

        // ② DefaultFile 文档（MelonLoader 可能另有副本）。反射试 RemoveEntryFromDocument(category, key)，没有该方法就跳过。
        try
        {
            FieldInfo fi = typeof(MelonPreferences).GetField("DefaultFile", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            object doc = fi == null ? null : fi.GetValue(null);
            if (doc != null)
            {
                MethodInfo mi = doc.GetType().GetMethod("RemoveEntryFromDocument",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null,
                    new Type[] { typeof(string), typeof(string) }, null);
                if (mi != null)
                {
                    string sec = ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId;
                    foreach (var p in _oldIncPrefixes)
                        for (int s = 0; s < 8; s++)
                        {
                            try { mi.Invoke(doc, new object[] { sec, p + "T" + s + "IncW" }); } catch { }
                            try { mi.Invoke(doc, new object[] { sec, p + "T" + s + "IncH" }); } catch { }
                        }
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }

        // ③ 磁盘 cfg 文件：直接删掉旧键行（立即干净；即便 ①② 有遗漏也不留孤儿）
        try
        {
            string cfgPath = CfgPath();
            if (!string.IsNullOrEmpty(cfgPath) && System.IO.File.Exists(cfgPath))
                RemoveLegacyIncLines(cfgPath);
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    private static void RemoveLegacyIncLines(string path)
    {
        bool changed = false;
        var output = new System.Collections.Generic.List<string>();
        foreach (string raw in System.IO.File.ReadAllLines(path))
        {
            string line = raw.Trim();
            // 形如  BoxT0IncW = 1  或  "BoxT0IncW" = 1
            int eq = line.IndexOf('=');
            if (eq > 0)
            {
                string key = line.Substring(0, eq).Trim().Trim('"');
                if (IsLegacyIncKey(key)) { changed = true; continue; }
            }
            output.Add(raw);
        }
        if (changed) System.IO.File.WriteAllLines(path, output);
    }

    /// <summary>从一段键值里挑出旧版「每槽 IncW/IncH」键，按目标键累计最大值。</summary>
    private static void CollectInc(Dictionary<string, string> vals, Dictionary<string, int> incMax, ref bool any)
    {
        if (vals == null) return;
        foreach (var kv in vals)
        {
            string key = kv.Key;
            bool isIncW = key.EndsWith("IncW", StringComparison.Ordinal);
            bool isIncH = key.EndsWith("IncH", StringComparison.Ordinal);
            if (!isIncW && !isIncH) continue;
            // 必须是 <前缀>T<槽位>IncW/H 结构，避免误匹配其它 Inc 键
            int tIdx = key.LastIndexOf('T');
            if (tIdx < 1) continue;
            string prefix = key.Substring(0, tIdx);
            bool isMod = prefix == "WageBox" || prefix == "WageBackpack" || prefix == "ModContainer";
            string target = (isMod ? "Mod" : "") + (isIncW ? "IncW" : "IncH");
            if (!int.TryParse(StripQuotes(kv.Value), out int v)) continue;
            if (!incMax.TryGetValue(target, out int cur) || v > cur) incMax[target] = v;
            any = true;
        }
    }

    /// <summary>把累计出的最大值写进全局条目（反射写 Value，绕过泛型）。</summary>
    /// <param name="oldId">incMax 的键 = 旧键名（IncW / IncH / ModIncW / ModIncH）。</param>
    private static void ApplyInc(MelonPreferences_Category cat, string oldId, Dictionary<string, int> incMax)
    {
        if (!incMax.TryGetValue(oldId, out int v)) return;
        // 键名重排：条目已按新键名（5D_IncW 等）注册，FindEntry 要用新名。
        MelonPreferences_Entry e = FindEntry(cat, NewKeyOf(oldId));
        if (e == null) return;
        try
        {
            var prop = e.GetType().GetProperty("Value");
            if (prop != null && prop.CanWrite)
            {
                // ★ 只升不降：MigrateRenamedKeys 可能已经先把「全局旧值」搬到这个新键，
                //   若旧的「每槽」值比它小，绝不能把它压回去（否则等于用户白调）。
                int cur = 0;
                try { cur = (int)prop.GetValue(e); } catch { }
                if (v > cur) { prop.SetValue(e, v); _prefsDirty = true; }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    // ② 诊断日志开关「三个 → 一个」（必须"读一遍再清"取 OR，否则用户诊断输出静默消失）：
    //   见 docs/cfg-migration.md

    /// <summary>诊断开关合并前用过的两个键（都在合并段里）。</summary>
    private static readonly string[] _legacyDiagKeys = { "SaveBag_DiagLog", "DIY_DiagLog" };

    internal static void MigrateDiagKeys(MelonPreferences_Category cat)
    {
        if (cat == null) return;
        try
        {
            bool anyOn = false;
            string cfgPath = CfgPath();
            string merged = ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId;

            // ① 合并段里残留的旧开关
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
            {
                var cur = ReadSectionValues(cfgPath, merged);
                foreach (string k in _legacyDiagKeys)
                    if (RawIsTrue(cur, k)) anyOn = true;
            }

            if (anyOn)
            {
                MelonPreferences_Entry master = FindEntry(cat, NewKeyOf("DiagLog"));
                if (master != null)
                {
                    try
                    {
                        PropertyInfo p = master.GetType().GetProperty("Value");
                        if (p != null && p.CanWrite) { p.SetValue(master, true); _prefsDirty = true; }
                    }
                    catch (Exception ex) { CatchLogOnce.Warn(ex); }
                }
                _log?.Msg("[配置合并] 诊断日志开关已合并为总开关 DiagLog"
                    + "（原有模块的诊断开关处于开启状态 → 总开关已置 true，旧键随后清理）。");
            }

            // 无论是否有 true，旧键都要清掉：留着就是面板外的一堆幽灵配置。
            CleanupLegacyDiagKeys(cat);
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    /// <summary>cfg 原始字符串是否是 true（大小写不敏感，容忍引号）。</summary>
    private static bool RawIsTrue(Dictionary<string, string> vals, string key)
    {
        if (vals == null || key == null) return false;
        if (!vals.TryGetValue(key, out string raw)) return false;
        return string.Equals(StripQuotes(raw), "true", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>三处摘旧键：① 内存 cat.Entries ② DefaultFile 文档 ③ 磁盘 cfg 文件。</summary>
    private static void CleanupLegacyDiagKeys(MelonPreferences_Category cat)
    {
        // ① 内存条目列表（正常情况下这两个键根本没注册，列里不会有；防御式保留）
        try
        {
            System.Collections.IList list = cat.Entries as System.Collections.IList;
            if (list != null)
            {
                for (int i = list.Count - 1; i >= 0; i--)
                {
                    MelonPreferences_Entry e = list[i] as MelonPreferences_Entry;
                    if (e == null) continue;
                    foreach (string k in _legacyDiagKeys)
                        if (string.Equals(e.Identifier, k, StringComparison.Ordinal)) { list.RemoveAt(i); break; }
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }

        // ② DefaultFile 文档（SaveToFile 的数据源；孤儿键就住在这里）
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
                    foreach (string k in _legacyDiagKeys)
                    {
                        try { mi.Invoke(doc, new object[] { sec, k }); } catch { }
                    }
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }

        // ③ 磁盘 cfg 文件：直接删行（即便 ①② 有遗漏也不留孤儿）
        try
        {
            string cfgPath = CfgPath();
            if (!string.IsNullOrEmpty(cfgPath) && File.Exists(cfgPath))
            {
                bool changed = false;
                var output = new List<string>();
                foreach (string raw in File.ReadAllLines(cfgPath))
                {
                    string line = raw.Trim();
                    int eq = line.IndexOf('=');
                    if (eq > 0)
                    {
                        string key = line.Substring(0, eq).Trim().Trim('"');
                        bool hit = false;
                        foreach (string k in _legacyDiagKeys)
                            if (string.Equals(key, k, StringComparison.Ordinal)) { hit = true; break; }
                        if (hit) { changed = true; continue; }
                    }
                    output.Add(raw);
                }
                if (changed) File.WriteAllLines(cfgPath, output);
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }
}