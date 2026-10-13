using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using ContainerSuite.Shared;   // S2 新增
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SaveBagExpand;

public static partial class SaveBagPaging
{
    // ────────── 配置持久化（S8u-4：UserData 私有旁路文件） ──────────
    //
    // 取证（2026-10-06 全会话日志）：每次启动 +10~23s 必现——第三方 mod
    // 「自动化酿酒拓展」写完 MelonPreferences.cfg，3ms 后「良心商人」再写 →
    // IOException「being used by another process」→ ML 设 WasError →
    // File.Save() 此后整个会话直接 return（IsSaving=false），所有 mod 的
    // MelonPreferences 落盘全部静默失效。Bind 时 CreateEntry 虽能从内存
    // 文档回填，但文档里根本没有值（11 个历史槽位 Slot*_Page2Ids 全是 ""，
    // 227 槽行都不存在）→ 读档 FindPage 全返回 0 → 第二页物品全部叠进第一页。
    // 结论：页 ID 不能走 MelonPreferences。格式（每行一个槽，ASCII）：
    //     slotId;pageCount;page2Ids;page3Ids;...
    // ★ T4（2026-10-12）：槽号后面可带一段「局标识」：
    //     slotId#runKey;pageCount;page2Ids;...
    //   带 #runKey 的行只在 runKey 与当前这一局一致时才匹配；**不带**该段的（老文件）
    //   仍按老规则匹配 —— 保证升级套件本身不会让玩家已有的页归属失效。
    // 写入在 SaveIdSets 里同步落盘（存档合并/切页即刻生效），失败才进
    // Tick 的 5 秒重试（_idsDirty）。

    private static string IdsFilePath =>
        System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "SaveBagPaging.ids");

    private static List<string> ReadIdsLines()
    {
        if (!System.IO.File.Exists(IdsFilePath)) return new List<string>();
        return new List<string>(System.IO.File.ReadAllLines(IdsFilePath));
    }

    private static void WriteIdsLines(List<string> lines)
    {
        string dir = System.IO.Path.GetDirectoryName(IdsFilePath);
        if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
        string tmp = IdsFilePath + ".tmp";
        System.IO.File.WriteAllLines(tmp, lines, new UTF8Encoding(false));
        System.IO.File.Move(tmp, IdsFilePath, true);   // 同卷原子替换
    }

    // ── B2：旁路文件版本头 ──────────────────────────────────────────
    //
    // 为什么要有：S8u-4 那次是"读到空值也不吭声" → 第二页全叠进第一页，
    // 静默失效最难查。加一行 #v1 之后，下次改格式时老套件能看见版本不一样，
    // 至少能在日志里说一句"格式不认"，而不是把新格式当旧格式解析。
    //
    // 兼容性：FindIdsLine 已经把 '#' 开头的行整行跳过（读侧零影响），
    //          写侧由 EnsureIdsHeader 在 SaveIdSets 落盘前补齐。
    private const string IdsFileVersion = "#v1";
    private static bool _idsVerWarned;

    private static void EnsureIdsHeader(List<string> lines)
    {
        if (lines == null) return;

        int first = -1;
        for (int i = 0; i < lines.Count; i++)
        {
            string t = lines[i] == null ? "" : lines[i].Trim();
            if (t.Length == 0) continue;
            first = i;
            break;
        }

        if (first < 0) { lines.Insert(0, IdsFileVersion); return; }   // 空文件

        string head = lines[first].Trim();
        if (head[0] != '#') { lines.Insert(first, IdsFileVersion); return; }   // 老文件无头 → 补

        if (!string.Equals(head, IdsFileVersion, StringComparison.Ordinal) && !_idsVerWarned)
        {
            _idsVerWarned = true;
            SaveBagExpandCore.Log.Warning(
                $"[SaveBagExpand] 页ID旁路文件版本不一致（文件 {head}，本套件 {IdsFileVersion}）： " +
                "按现有规则尽力解析，页归属若不对，整理一次并存档即可用本版本覆盖");
            // 注意：此处**不会**改动/删除原文件的版本行（EnsureIdsHeader 只在缺头时插入），
            // 因此万一将来出现 #v2 等新格式，本版本不会把它误标成 #v1 而破坏原文件；
            // 代价是此时按 v1 语义解析可能是错的 —— 这正是上面告警要提示的。
        }
    }

    /// <summary>
    /// T4（2026-10-12）：从一行里解出槽号。表头形如 <c>12</c> 或 <c>12#runKey</c>。
    /// <para>
    /// ★ 必须**只有这一处**做表头解析 —— 读侧（FindIdsLine）与写侧（SaveIdSets 的替换）
    ///   一旦用不同规则，写成 <c>12#run</c> 之后写侧认不出旧行（表头不再能 int.TryParse），
    ///   于是每次存档都**追加**一行、而读侧永远命中靠前的那条旧行 —— 页归属直接冻结。
    ///   这是实现 T4 时实际踩到的坑，故抽成公共实现。
    /// </para>
    /// </summary>
    private static bool TryParseSlotHead(string line, out int slot)
    {
        slot = -1;
        if (string.IsNullOrEmpty(line)) return false;
        string t = line.Trim();
        if (t.Length == 0 || t[0] == '#') return false;   // '#v1' / '#run=' 是头行，不是数据行
        int semi = t.IndexOf(';');
        string head = semi >= 0 ? t.Substring(0, semi) : t;
        int hash = head.IndexOf('#');
        if (hash >= 0) head = head.Substring(0, hash);    // 丢掉可选的局标识段
        return int.TryParse(head, out slot);
    }

    private static string FindIdsLine(List<string> lines, int slot)
    {
        // T4：当前局的标识（拿不到 = 空串 ⇒ 不做局隔离，保持旧行为）。
        string runKey = SaveSlotScope.GetRunScopeKey();
        for (int i = 0; i < lines.Count; i++)
        {
            string t = lines[i] == null ? "" : lines[i].Trim();
            if (!TryParseSlotHead(t, out int s) || s != slot) continue;

            // T4：取出这一段局标识（表头里 '#' 之后的部分）。
            string runPart = null;
            int semi = t.IndexOf(';');
            string head = semi >= 0 ? t.Substring(0, semi) : t;
            int hash = head.IndexOf('#');
            if (hash >= 0) runPart = head.Substring(hash + 1);

            // T4：写了局标识却与本局不符 → 这一行属于**同一存档槽的上一局**（开新游戏），
            //   当作不存在。★ 只跳过这一行，绝不删别的行 —— 文件里还有其它存档槽的行。
            //   ★ 无局标识（老文件 / 自检探针行）一律按老规则匹配。
            if (!string.IsNullOrEmpty(runPart) && !string.IsNullOrEmpty(runKey)
                && !string.Equals(runPart, runKey, StringComparison.Ordinal))
            {
                if (!_idsRunForeignWarned)
                {
                    _idsRunForeignWarned = true;
                    SaveBagExpandCore.Log.Warning(
                        $"[SaveBagExpand] 页ID旁路文件里槽 {slot} 的记录属于**上一局**（runKey 不符），已忽略："
                        + "同槽开新游戏时物品 ID 会重排，沿用旧归属会让第二页物品叠进第一页。"
                        + "本次进游戏后整理一次并存档即可写入本局记录。");
                }
                continue;
            }
            return t;
        }
        return null;
    }

    // T4：局不符只告警一次（每次查询都刷屏没意义）。
    private static bool _idsRunForeignWarned;

    private static string BuildIdsLine()
    {
        var sb = new StringBuilder();
        sb.Append(_boundSlotId);
        // T4（2026-10-12）：把「局标识」拼在槽号后面（`<slot>#<run>`），只借蛙哥
        //   「#runID=」的思路做按局隔离；拿不到 runID 时**不写这一列**（老格式，仍然可用）。
        try
        {
            string runKey = SaveSlotScope.GetRunScopeKey();
            if (!string.IsNullOrEmpty(runKey)) sb.Append('#').Append(runKey);
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.BuildIdsLine.runKey", ex); }
        sb.Append(';').Append(_pageCount);
        for (int i = 1; i < _pageIds.Count; i++)
            sb.Append(';').Append(FormatIds(_pageIds[i]));
        return sb.ToString();
    }

    private static void SaveIdSets()
    {
        if (_boundSlotId < 0 || _pageIds.Count == 0) return;
        try
        {
            var lines = ReadIdsLines();
            string line = BuildIdsLine();
            bool replaced = false;
            for (int i = 0; i < lines.Count; i++)
            {
                string t = lines[i] == null ? "" : lines[i].Trim();
                // ★ T4：必须用与读侧**同一个**表头解析（TryParseSlotHead）——
                //   否则写成 `<slot>#<run>;...` 之后这里 int.TryParse 失败 ⇒ 认不出旧行
                //   ⇒ 每次存档追加一行，而读侧永远命中靠前的旧行（页归属冻结）。
                if (TryParseSlotHead(t, out int s) && s == _boundSlotId)
                {
                    lines[i] = line;
                    replaced = true;
                    break;
                }
            }
            if (!replaced) lines.Add(line);
            EnsureIdsHeader(lines);
            WriteIdsLines(lines);
            _idsDirty = false;
            _idsWarned = false;
        }
        catch (Exception ex)
        {
            _idsDirty = true;
            if (!_idsWarned)
            {
                _idsWarned = true;
                SaveBagExpandCore.Log.Warning(
                    "[SaveBagExpand] 页ID旁路文件写入失败（Tick 每 5 秒重试）: " + ex.Message);
            }
        }
    }

    private static void LoadIdSets()
    {
        if (_boundSlotId < 0 || _pageIds.Count == 0) return;
        for (int i = 1; i < _pageIds.Count; i++) _pageIds[i].Clear();
        try
        {
            string line = FindIdsLine(ReadIdsLines(), _boundSlotId);
            if (line == null) return;
            // slot;pageCount;page2;page3;... → _pageIds[1] = 页2 …
            var parts = line.Split(';');
            for (int p = 1; p < _pageIds.Count && p + 1 < parts.Length; p++)
                LoadIdSetInto(parts[p + 1], _pageIds[p]);
        }
        catch (Exception ex)
        {
            if (!_idsWarned)
            {
                _idsWarned = true;
                SaveBagExpandCore.Log.Warning(
                    "[SaveBagExpand] 页ID旁路文件读取失败: " + ex.Message);
            }
        }
    }

    /// <summary>退出时兜底：有未落盘的页 ID 改动就再试一次（Mod.OnApplicationQuit 调）。</summary>
    public static void FlushIdsOnQuit()
    {
        if (_idsDirty) SaveIdSets();
    }

    /// <summary>
    /// S8u-4 启动自检：向旁路文件写入临时槽 -999 → 回读断言 → 还原。
    /// 验证 UserData 可写、格式可解析；已进游戏（已绑定）时跳过。
    /// </summary>
    private static void IdsFileSelfTest()
    {
        if (_boundSlotId >= 0 || _pageIds.Count == 0) return;
        bool existed = System.IO.File.Exists(IdsFilePath);
        var original = ReadIdsLines();
        try
        {
            var probe = new List<string>(original) { "-999;2;7,8;9" };
            WriteIdsLines(probe);
            string back = FindIdsLine(ReadIdsLines(), -999);
            if (back != "-999;2;7,8;9")
                throw new Exception("回读不一致: " + (back ?? "(null)"));
            SaveBagExpandCore.Diag("[SBE-JIT] 页ID旁路文件自检通过: " + IdsFilePath);
        }
        finally
        {
            try
            {
                if (existed) WriteIdsLines(original);
                else if (System.IO.File.Exists(IdsFilePath)) System.IO.File.Delete(IdsFilePath);
            }
            catch (Exception ex)
            {
                SaveBagExpandCore.Log.Warning("[SBE-JIT] 页ID自检还原失败: " + ex.Message);
            }
        }
    }

    private static void LoadIdSetInto(string raw, HashSet<int> target)
    {
        if (string.IsNullOrEmpty(raw)) return;
        foreach (var token in raw.Split(','))
            if (int.TryParse(token.Trim(), out int v) && v != 0)
                target.Add(v);
    }

    private static string FormatIds(HashSet<int> set)
    {
        if (set == null || set.Count == 0) return "";
        var sb = new StringBuilder();
        foreach (var id in set)
        {
            if (sb.Length > 0) sb.Append(',');
            sb.Append(id);
        }
        return sb.ToString();
    }

}
