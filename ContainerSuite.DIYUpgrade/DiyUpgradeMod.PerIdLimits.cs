// ══════════════════════════════════════════════════════════════════════════
// 「每个容器 id 独立尺寸上限」—— 唯一事实源是 cfg 的 <c>8A_PerIdSizeLimits</c>
//
// 【它的位置为什么在分层的最底下】
//   容器升级的上限一共三层，越往下越具体、越往下覆盖力越强：
//     ① 7x_<类别>（7A_BoxMaxW/MaxH …）      —— 一类容器共享，玩家最常调
//     ② 8A_PerIdSizeLimits                  —— 单个容器 id，写了就**完全覆盖** ①
//     ③ 硬边界 1~100（TryGetGridShapeLimits 的 HardMaxDim）—— 写不进去
//   ② 排在配置面板的最末尾（z_ 诊断之前），因为它 90% 的人用不到；
//     真要用的人通常只为一两个容器改，属于"精细调整"。
//
// 【为什么写在一条字符串里，而不是每个 id 一条 cfg 项】
//   · 容器 id 是**开放集合** —— 玩家随时能往 *Ids 里加自定义容器 id，
//     cfg 条目却必须在启动时静态注册，动态 id 建不出条目；
//   · 反过来"给 200 个已知 id 各建两条"会把 cfg 变成 400 行的噪音，
//     而其中 99% 的人一个都不改。
//   ⇒ 一条 `id=宽x高` 列表是这里唯一既能覆盖任意 id、又不制造冗余条目的写法。
//
// 【为什么改完不需要重启】
//   OnEntryValueChanged 里重解析 + 作废两张受影响的缓存（IsResizable / 形状上限），
//   与 *Ids 清单的热更新同一套机制。
//
// 【为什么不另外出一份文件】（2026-10-13 决定）
//   2026-10-12 曾把白名单与上限搬到 UserData\ContainerSuite\containers.txt，
//   代价是"改了 cfg 没生效"成了常态疑问（两套配置、谁是谁的权威必须靠日志解释）。
//   现在白名单回 cfg、上限也进 cfg —— **单一事实源**，不用再解释。
// ══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Text;
using ContainerSuite.Shared;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Preferences;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    /// <summary>每 id 上限的硬边界（与 TryGetGridShapeLimits 的 <c>HardMaxDim</c> 保持一致）。</summary>
    private const int PerIdLimitMinDim = 1;
    private const int PerIdLimitMaxDim = 100;

    /// <summary>
    /// cfg 字符串条目：<c>storage_bay=30x20, evidence_box=24x24</c>。
    /// <para>★ 它是 string ⇒ ModManager 面板**不显示**（面板只收 bool / 数字 / Hotkey 键名），
    /// 只能手改 cfg —— 这是刻意的取舍，详见本文件头注。</para>
    /// </summary>
    private static MelonPreferences_Entry<string> _perIdSizeLimits;

    /// <summary>解析结果：规范化容器 id →（宽, 高）。空表 = 玩家一条都没配 ⇒ 热路径零开销。</summary>
    private static readonly Dictionary<string, (int W, int H)> _perIdLimit
        = new Dictionary<string, (int, int)>(StringComparer.Ordinal);

    /// <summary>
    /// 容器 id → 中文名（日志用，让玩家看得出自己配了哪几件）。
    /// 取自本仓库 <c>物品id参考.md</c>；查不到就只写 id（宁可不写，也不猜一个错名字）。
    /// </summary>
    private const string ContainerNameTable =
        "sec_box=治安部物资箱;evidence_box=治安部物证柜;storage_bay=储藏区;storage_bay_large=储藏区（扩建）;"
        + "smuggler_bay=走私者暗格;smuggler_bay_mod=走私者暗格（改进）;machine_bay=机器区;"
        + "machine_bay_ext=机器区（扩建）;service_box=后勤服务部物资箱;med_box=医疗舱物资箱;"
        + "eng_box=工程铸造厂物资箱;storage_bay_chem=化学家储物舱;storage_bay_gun=枪匠储物舱;"
        + "smuggler_bay_mini=迷你走私舱;makeshift_storage_bay=简易储物舱;expedition_box=远征补给箱;"
        + "backpack_small=背包（小）;backpack_medium=背包（基础）;backpack_large=背包（大）;"
        + "satchel=挎包（小）;fanny_pack=腰包;backpack_medium_military=背包（军用）;"
        + "backpack_large_military=大型军用背包;animal_cage=动物笼;"
        + "ratlin_animal_cage=鼠布林老鼠笼;"
        + "fridge=冰箱;recharger=充能器;vacuum_robot=清洁机器人;"
        + "furnace=熔炉;desequencer=密码解序器;security_alarm=警报系统;mirage_projector=幻影投影仪;"
        + "water_purifier=净水器;moisture_farm=水分收集器;wine_rack=催陈器;bottle_printer=水瓶打印机;"
        + "custom_storage_box=蛙哥妙妙箱;void_bead_storage=虚空珠;wage_gu_machine=蛙哥养蛊机;"
        + "wage_ai_generator=蛙哥不稳定AI生成器";

    // ═══════════════════════════════════════════════════════════════════════
    // 注册（放在 Init 的最后一项条目，让它在 cfg 里也排在末尾）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 注册 <c>8A_PerIdSizeLimits</c> 并挂上热更新回调。
    /// <para>★ 必须在所有 <c>CreateGridDef</c> 之后调用 —— 一是键名顺序（它在最后），
    /// 二是解析完成后的生效 Summary 要能读到分类上限。</para>
    /// </summary>
    private static void RegisterPerIdSizeLimits(MelonPreferences_Category cat)
    {
        if (cat == null) return;
        _perIdSizeLimits = cat.CreateEntry<string>(CfgKeys.PerIdSizeLimits, "",
            (string)null, DefaultPerIdSizeLimitsComment,
            false, false, (ValueValidator)null, (string)null);

        // ★ 2026-10-14：cfg 体检的宽容采纳必须在这里做（且只在这里）。
        //   理由：它是**最后一个**注册的条目 —— 此刻全部 85 个条目都已存在（类型与默认值
        //   都已知，够 CfgDoctor 判断"文件里有值、条目却停在默认值"），而它又早于
        //   下面的 LoadPerIdSizeLimits()（本文件唯一的"Init 期读快照"）以及 Core.Init 里的
        //   RefreshIdLists()。位置放错就等于白采纳：值救回来了，可解析结果仍是旧默认值。
        CfgDoctor.ApplyTo(cat);

        ((MelonEventBase<LemonAction<string, string>>)(object)_perIdSizeLimits.OnEntryValueChanged)
            .Subscribe((LemonAction<string, string>)delegate { LoadPerIdSizeLimits(); }, 0, false);

        LoadPerIdSizeLimits();
    }

    /// <summary>
    /// cfg 里这条的**注释**。它是 string 项、面板看不到 ⇒ 这段注释（连同 CreateEntry 的
    /// Description）就是玩家唯一能看到的使用说明，必须照着抄就能改对。
    /// </summary>
    private const string DefaultPerIdSizeLimitsComment =
        "每个容器单独设升级上限，格式：容器id=宽x高，多个用逗号隔开（例 storage_bay=30x20,evidence_box=24x24）。"
        + "数字 1~100；只写想单独限制的容器，没写的用面板里「容器升级·分类」那几个上限。"
        + "留空 = 全部按分类上限。改完保存，面板「应用配置」后立刻生效，不用重启。";

    // ═══════════════════════════════════════════════════════════════════════
    // 解析
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 解析 cfg 原文进 <see cref="_perIdLimit"/>。幂等，可反复调用（面板改一次调一次）。
    /// <para>
    /// ★ **绝不因为解析失败就把旧表清空**：解析异常时保留上一份结果，只点名告警。
    ///   "上一次还能用的那套继续生效"比"配置表干净"重要得多 —— 这里如果贸然清空，
    ///   玩家写错一个字符就会看到所有容器的上限同时失效，且没有任何报错。
    /// </para>
    /// </summary>
    private static void LoadPerIdSizeLimits()
    {
        var next = new Dictionary<string, (int W, int H)>(StringComparer.Ordinal);
        var problems = new List<string>();
        try
        {
            string raw = (_perIdSizeLimits != null) ? _perIdSizeLimits.Value : null;
            if (!string.IsNullOrWhiteSpace(raw))
            {
                string[] parts = raw.Split(new char[4] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < parts.Length; i++)
                {
                    string token = parts[i].Trim();
                    if (token.Length == 0) continue;

                    int eq = token.IndexOf('=');
                    if (eq <= 0)
                    {
                        problems.Add($"第 {i + 1} 项 `{token}` 不是 `容器id=宽x高` 的形式，已忽略");
                        continue;
                    }
                    string id = NormalizeContainerId(token.Substring(0, eq));
                    string val = token.Substring(eq + 1).Trim();
                    if (string.IsNullOrEmpty(id))
                    {
                        problems.Add($"第 {i + 1} 项 `{token}` 的容器id为空，已忽略");
                        continue;
                    }
                    if (!TryParsePerIdSize(val, out int w, out int h))
                    {
                        problems.Add($"`{id}` 的尺寸 `{val}` 不合法（要写成 宽x高，数字 "
                                     + $"{PerIdLimitMinDim}~{PerIdLimitMaxDim}），已忽略");
                        continue;
                    }
                    next[id] = (w, h);     // 后写的覆盖先写的（同一 id 写了两次时以最后一次为准）
                }
            }
        }
        catch (Exception ex)
        {
            // ★ 解析整体失败：保留上一份，不用半成品覆盖。
            CatchLogOnce.Warn("PerIdLimits:parse", ex);
            _log?.Error("[容器升级] 每容器上限cfg解析失败，此处**沿用上一次生效的设置**：" + ex.Message);
            return;
        }

        _perIdLimit.Clear();
        foreach (var kv in next) _perIdLimit[kv.Key] = kv.Value;

        // 白名单本身改动也会影响这张表（id 换了），两张缓存都要作废。
        try { _resizableCache.Clear(); } catch (Exception ex) { CatchLogOnce.Warn("PerIdLimits:resizable", ex); }
        try { _prepareCache.Clear(); } catch (Exception ex) { CatchLogOnce.Warn("PerIdLimits:prepare", ex); }

        if (problems.Count > 0)
            _log?.Warning("[容器升级] 每容器上限cfg有 " + problems.Count + " 处没生效（其余正常）："
                          + string.Join("；", problems));
    }

    /// <summary>把玩家写的 id 规范化（清 \0、去空白、转小写）—— 与白名单同一套词汇。</summary>
    private static string NormalizeContainerId(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        return raw.Replace("\0", "").Trim().ToLowerInvariant();
    }

    private static bool TryParsePerIdSize(string val, out int w, out int h)
    {
        w = 0; h = 0;
        if (string.IsNullOrEmpty(val)) return false;
        string[] p = val.ToLowerInvariant().Split('x');
        if (p.Length != 2) return false;
        if (!int.TryParse(p[0].Trim(), out w)) return false;
        if (!int.TryParse(p[1].Trim(), out h)) return false;
        return w >= PerIdLimitMinDim && w <= PerIdLimitMaxDim
            && h >= PerIdLimitMinDim && h <= PerIdLimitMaxDim;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 读取（GridWindow.TryGetGridShapeLimits 的唯一外部入口）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 取某个容器的独立上限。返回 false = 玩家没为它单独配 ⇒ 用分类上限（7x_）。
    /// <para>★ 键是**规范化后的容器类型 id**，不是 uniqueId（实例身份跨存档会重排，不能当持久化键）。</para>
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

    // ═══════════════════════════════════════════════════════════════════════
    // 可观测性：把「这份配置到底配了什么」说清楚
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 启动 Summary。★ **无条件输出**（不进 DiagLog 闸门）：
    /// 玩家有没有配、配了哪几件 —— 这是"我改了配置怎么没生效"类问题的第一现场，
    /// 默认静默的话等于把唯一线索藏起来（本项目反复踩过这条）。
    /// </summary>
    private static void LogPerIdLimitsSummary()
    {
        try
        {
            if (_perIdLimit.Count == 0)
            {
                _log?.Msg("[容器升级] 没有为单个容器单独设上限 → 全部使用「容器升级·分类」里的上限"
                          + "（要单独设：改 cfg 的 " + CfgKeys.PerIdSizeLimits + "）");
                return;
            }
            var sb = new StringBuilder();
            foreach (var kv in _perIdLimit)
            {
                if (sb.Length > 0) sb.Append("、");
                sb.Append(kv.Key).Append('=').Append(kv.Value.W).Append('x').Append(kv.Value.H);
                string cn = ContainerNameOf(kv.Key);
                if (cn.Length > 0) sb.Append('（').Append(cn).Append('）');
            }
            _log?.Msg($"[容器升级] 单独设了上限的容器共 {_perIdLimit.Count} 件：{sb}"
                      + "；其它容器仍用「容器升级·分类」里的上限");
        }
        catch (Exception ex) { CatchLogOnce.Warn("PerIdLimits:summary", ex); }
    }

    /// <summary>容器 id 的中文名（查不到返回空串 —— 玩家自定义 id 不猜名字）。</summary>
    private static string ContainerNameOf(string id)
    {
        if (string.IsNullOrEmpty(id)) return "";
        string[] pairs = ContainerNameTable.Split(';');
        for (int i = 0; i < pairs.Length; i++)
        {
            int eq = pairs[i].IndexOf('=');
            if (eq <= 0) continue;
            if (string.Equals(pairs[i].Substring(0, eq), id, StringComparison.OrdinalIgnoreCase))
                return pairs[i].Substring(eq + 1);
        }
        return "";
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 动态注释：把「每个 id 当前生效的上限」写进 cfg（2026-10-13）
    //
    // 玩家要手改 8A_PerIdSizeLimits 时，得先知道「这个 id 现在算多大的上限」——
    // 那要么翻分类上限、要么记住 id 的中文名。这里把两者都摊在条目正上方的注释里：
    // id=宽x高（中文名），已单独配的加 ★。
    // ★ 依赖白名单就绪 ⇒ 必须在 RefreshIdLists() 之后调用（见 Core.Init）。
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 重建 <c>8A_PerIdSizeLimits</c> 的 cfg 注释（条目上方的 <c>#</c> 注释即 entry.Description）。
    /// <para>数值口径与 <see cref="TryGetPerIdLimit"/> 一致：配了 perId 用 perId，否则用分类上限。</para>
    /// </summary>
    private static void RefreshPerIdLimitsComment()
    {
        try
        {
            if (_perIdSizeLimits == null) return;
            var entry = (MelonPreferences_Entry)_perIdSizeLimits;
            string next = BuildPerIdLimitsCommentBody();
            // 2026-10-14：注释没变就不重写 Description。
            //   ★ 这里**刻意不标记"必须落盘"**：本方法每次启动都会走到（因为上面的
            //     ApplyConfigLabels 会先把 8A 的 Description 改回短说明），若在这里
            //     MarkDirty 就等于"每次启动必然写一次盘"——实机 2026-10-14 12:54 印证了
            //     这一点（日志 [cfg体检] 写盘：...（已标记：8A 注释重建））。
            //   注释变更由 CfgDoctor.SaveIfNeeded 的**注释比对**负责触发写盘：它拿
            //   文件里该键上方的注释块与这里的 Description 逐字比，不一致才写。所以
            //   "玩家改了白名单/单件上限 → 注释真的变了" 仍然会落盘，无需在此强制。
            if (string.Equals(entry.Description, next, StringComparison.Ordinal)) return;
            entry.Description = next;
        }
        catch (Exception ex) { CatchLogOnce.Warn("PerIdLimits:comment", ex); }
    }

    /// <summary>拼出 8A 的完整注释正文（用法说明 + 按 8 个分类列出全部白名单 id 及其生效上限）。</summary>
    private static string BuildPerIdLimitsCommentBody()
    {
        var sb = new StringBuilder();
        sb.Append(DefaultPerIdSizeLimitsComment);
        sb.Append("\n");
        sb.Append("\n★ 下表 = 白名单里每个容器「当前确认生效」的上限（id=宽x高（中文名））。");
        sb.Append("\n· 数值口径：你单独配了的按你配的（行尾标 ★）；没配的用它所属分类的上限（面板「容器升级·分类」里那几项）。");
        sb.Append("\n· 想单独改某件：对照下表的中文名找到容器id，在本行引号里写成 `容器id=宽x高`（多个用逗号隔开）保存即生效。");

        AppendPerIdCommentGroup(sb, "箱子", _box);
        AppendPerIdCommentGroup(sb, "背包", _backpack);
        AppendPerIdCommentGroup(sb, "笼子", _cage);
        AppendPerIdCommentGroup(sb, "多槽容器", _multiSlot);
        AppendPerIdCommentGroup(sb, "机器模组槽", _multiSlotModule);
        AppendPerIdCommentGroup(sb, "Mod容器", WageContainerRegistry._modContainer);
        AppendPerIdCommentGroup(sb, "蛙哥妙妙箱", WageContainerRegistry._wageBox);
        AppendPerIdCommentGroup(sb, "蛙哥虚空珠袋", WageContainerRegistry._wageBackpack);
        return sb.ToString();
    }

    /// <summary>追加一个分类：标题行 + 每个 id 一行 <c>id=宽x高（中文名）</c>。</summary>
    private static void AppendPerIdCommentGroup(StringBuilder sb, string label, GridDef d)
    {
        if (d == null) return;
        string[] ids;
        try { ids = IdMatch.SplitIds(d.Ids.Value); }
        catch (Exception ex) { CatchLogOnce.Warn("PerIdLimits:comment-ids", ex); return; }
        if (ids == null || ids.Length == 0) return;

        int catMaxW = (d.MaxW != null && d.MaxW.Value > 0) ? d.MaxW.Value : PerIdLimitMaxDim;
        int catMaxH = (d.MaxH != null && d.MaxH.Value > 0) ? d.MaxH.Value : PerIdLimitMaxDim;

        sb.Append("\n[").Append(label).Append("]");
        for (int i = 0; i < ids.Length; i++)
        {
            string id = NormalizeContainerId(ids[i]);
            if (id.Length == 0) continue;

            int w = catMaxW, h = catMaxH;
            bool isPerId = false;
            if (_perIdLimit.TryGetValue(id, out var lim)) { w = lim.W; h = lim.H; isPerId = true; }

            string cn = ContainerNameOf(id);
            sb.Append("\n  ").Append(id).Append('=').Append(w).Append('x').Append(h);
            if (cn.Length > 0) sb.Append("（").Append(cn).Append('）');
            if (isPerId) sb.Append("  ★");
        }
    }
}
