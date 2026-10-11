using MelonLoader;
using MelonLoader.Utils;
using System;
using System.IO;
using ContainerSuite.Shared;   // P6：读 SuiteVersion.Suite

namespace SaveBagExpand;

internal static class SaveBagExpandConfig
{
    // ── 全局 ──
    internal static MelonPreferences_Entry<bool> EnableMod;

    // ── 滚动 ──
    internal static MelonPreferences_Entry<bool> ScrollEnabled;
    internal static MelonPreferences_Entry<int> ScrollVisibleRows;
    internal static MelonPreferences_Entry<int> ScrollUpRows;

    // ── 主仓库·分页 ──
    internal static MelonPreferences_Entry<bool> SaveBagPagingEnabled;
    internal static MelonPreferences_Entry<int> SaveBagPageCount;
    internal static MelonPreferences_Entry<float> SaveBagPagingHintDuration;
    internal static MelonPreferences_Entry<bool> DiagLog;   // S8g：合并自 SaveBagPagingDiagLog

    // ── 显化网格 ──
    internal static MelonPreferences_Entry<bool> SaveBagShowGrid;

    // ── 仓库·扩容 ──
    internal static MelonPreferences_Entry<bool> ExpandEnabled;
    internal static MelonPreferences_Entry<int> SaveBagWidth;
    internal static MelonPreferences_Entry<int> SaveBagHeight;
    internal static MelonPreferences_Entry<int> ShowcaseInvWidth;
    internal static MelonPreferences_Entry<int> ShowcaseInvHeight;
    internal static MelonPreferences_Entry<int> HiddenInvWidth;
    internal static MelonPreferences_Entry<int> HiddenInvHeight;
    internal static MelonPreferences_Entry<int> BackInvWidth;
    internal static MelonPreferences_Entry<int> BackInvHeight;

    // ── 客户柜台（S8f：开关合并入【仓库·扩容】，尺寸仍独立配置） ──
    internal static MelonPreferences_Entry<int> FrontInvWidth;
    internal static MelonPreferences_Entry<int> FrontInvHeight;

    // ── 地面/拾荒窗口（2026-10-09 新增，第 9 个容器） ──
    //   「垃圾场拾荒」场景里标题为「地面」的窗口（EmporiumEntry.afterhourWindow /
    //   afterhourInventory）。原版 = 12×9（反汇编 SetupAfterhourInv 的
    //   GameGridInventory.ctor(12,9) 已证实）。挂点 = SetupAfterhourInv Postfix。
    internal static MelonPreferences_Entry<int> AfterhourWidth;
    internal static MelonPreferences_Entry<int> AfterhourHeight;

    // ── 地面/拾荒窗口·鼠标拖动（2026-10-09 新增） ──
    //   地面窗口原版 canMove==false（readonly 字段，构造时定死），游戏自带拖动被禁用。
    //   本 mod 在窗口顶栏自实现拖动（复刻游戏 UIDragHandler.OnDrag 语义：
    //   anchoredPosition += delta / Canvas.scaleFactor），并把相对原版停靠位的
    //   偏移量持久化到下面两个 key —— 下次进场景仍在拖动后的位置。
    //   ★ 偏移单位 = anchoredPosition 像素（Canvas scaleFactor=1 时等同屏幕像素）。
    //   ★ 默认 0 = 与游戏原版位置完全一致；玩家不拖 = 行为等价于未装本功能。
    internal static MelonPreferences_Entry<bool> GroundDragEnabled;
    internal static MelonPreferences_Entry<int> GroundOffsetX;
    internal static MelonPreferences_Entry<int> GroundOffsetY;

    internal static void Init()
    {
        // 2026-10-06 合并（B2）：段名 SaveBagExpand → CustomInfrastructure。
        //   ModManager 归属规则只看 DLL 文件名（ManagerState.CategoryBelongsToMod），
        //   单 DLL 之后文件名恒为 CustomInfrastructure，段名不跟着改的话
        //   本模块 19 个配置项会全部从面板里消失。
        //   旧段 [SaveBagExpand] 的值由 DiyUpgrade.CfgMigration 一次性搬过来，不丢配置。
        //   必须复用 CI 已创建的那个 category 实例（否则同名段注册两次、保存互相覆盖）。
        MelonPreferences_Category cat = ContainerSuite.Shared.MelonPrefsUtil.GetOrCreate(
            ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId, "自定义基础设施大小");

        const string sizeDesc = "（默认值 = 当前游戏原版尺寸；[扩容] 启用为 true 时才会应用）";
        const string hiddenDesc = "（默认值 4x4；游戏原版 2x2）";

        // ── 全局 ──
        EnableMod = cat.CreateEntry<bool>("0_EnableMod", true,
            "[全局] 总开关",
            "true = 启用本 Mod；false = 完全禁用。");

        // ── 滚动 ──
        ScrollEnabled = cat.CreateEntry<bool>("9_ScrollEnabled", true,
    "[滚动] 滚轮启用",
    "true = 鼠标指到容器上时可用滚轮上下滚动内容；false = 关闭。默认 true。");
        ScrollVisibleRows = cat.CreateEntry<int>("9B_ScrollVisibleRows", 0,
            "[滚动] 向下行数 (0-50)",
            "滚轮向下最多能滚几行，可设 0-50。");
        ScrollUpRows = cat.CreateEntry<int>("9A_ScrollUpRows", 10,
            "[滚动] 向上行数 (0-50)",
            "滚轮向上最多能滚几行，可设 0-50。0 = 不允许向上滚。");

        // ── 主仓库·分页 ──
        SaveBagPagingEnabled = cat.CreateEntry<bool>("8_SaveBagPagingEnabled", true,
    "[分页] 启用",
    "true = 主仓库保持游戏原版 24x10 网格，通过分页提供多倍存储空间。默认 true。");
        SaveBagPageCount = cat.CreateEntry<int>("8A_SaveBagPageCount", 2,
            "[分页] 总页数 (1-10)",
            "主仓库的总页数，范围 1-10。每页 24x10 = 240 格。");
        SaveBagPagingHintDuration = cat.CreateEntry<float>("8B_SaveBagPagingHintDuration", 1.0f,
            "[分页] 提示时长 (0-5 秒)",
            "TAB 切页成功后，屏幕提示显示多少秒。默认 1.0，范围 0.1 ~ 5.0。0 = 不显示。");
        // 2026-10-06 合并（B2）：三段并成一段后，三个模块的键名必须互不冲突，
        //   只有 DiagLog 撞名（CI / SBE / DIY 各有一个）。当时把本模块改名成
        //   SaveBag_DiagLog、DIY 改成 DIY_DiagLog。
        //
        // ★ 2026-10-07 再改：三个开关**合并成一个总开关**（面板里三项同名，
        //   玩家根本分不清该开哪个）。现在本字段是 CI 那条 `DiagLog` 的**别名**
        //   —— 同一个 MelonPreferences_Entry 实例，不再是本模块自己的条目。
        //   所以：
        //     · 所有 `SaveBagExpandConfig.DiagLog?.Value` 读取点一行未改；
        //     · 旧键 SaveBag_DiagLog / DIY_DiagLog 由 DiyUpgrade.CfgMigration
        //       的 MigrateDiagKeys 做 OR 迁移 + 三处清理（见那里）。
        //   ★ 时序前提：CI.Init 早于 SBE.Init（Mod.OnInitializeMelon 的顺序），
        //     所以这里取到的一定是已创建好的 entry。
        DiagLog = CustomInfrastructure.CustomInfrastructureConfig.DiagLog;


        // ── 显化网格 ──
        SaveBagShowGrid = cat.CreateEntry<bool>("z2_ShowGrid", false,
            "[诊断] 网格强制不透明",
            "true = 强制把所有网格填充底画成不透明（黑色格子）。默认 false。");

        // ── 仓库·扩容 ──
        ExpandEnabled = cat.CreateEntry<bool>("7_ExpandEnabled", true,
    "[扩容] 启用",
    "true = 应用下方所有容器的自定义尺寸；false = 所有容器按游戏原版尺寸。");
        SaveBagWidth = cat.CreateEntry<int>("7D_SaveBagWidth", 24, "[扩容] 主仓库·宽", sizeDesc);
        SaveBagHeight = cat.CreateEntry<int>("7D_SaveBagHeight", 10, "[扩容] 主仓库·高", sizeDesc);
        ShowcaseInvWidth = cat.CreateEntry<int>("7E_ShowcaseInvWidth", 9, "[扩容] 展示柜·宽", sizeDesc);
        ShowcaseInvHeight = cat.CreateEntry<int>("7E_ShowcaseInvHeight", 7, "[扩容] 展示柜·高", sizeDesc);
        HiddenInvWidth = cat.CreateEntry<int>("7C_HiddenInvWidth", 4, "[扩容] 海报暗格·宽", hiddenDesc);
        HiddenInvHeight = cat.CreateEntry<int>("7C_HiddenInvHeight", 4, "[扩容] 海报暗格·高", hiddenDesc);
        BackInvWidth = cat.CreateEntry<int>("7A_BackInvWidth", 18, "[扩容] 柜台·宽", sizeDesc);
        BackInvHeight = cat.CreateEntry<int>("7A_BackInvHeight", 4, "[扩容] 柜台·高", sizeDesc);

        // ── 客户柜台（S8f：开关合并入【仓库·扩容】总控，尺寸仍独立配置） ──
        //   ⚠️ 原 FrontInvExpandEnabled 独立开关已删除。老的 cfg 里如果还留着
        //      这个 key，MelonPreferences 会忽略（未注册 = 不激活），无害。
        FrontInvWidth = cat.CreateEntry<int>("7B_FrontInvWidth", 18, "[扩容] 客户柜台·宽", sizeDesc);
        FrontInvHeight = cat.CreateEntry<int>("7B_FrontInvHeight", 4, "[扩容] 客户柜台·高", sizeDesc);

        // ── 地面/拾荒窗口（2026-10-09 新增）──
        //   ★ 默认值 = 游戏原版 12×9（与「默认值 = 当前游戏原版尺寸」约定一致）。
        //     改了这两个值 = 真正改尺寸；保持原版值 = 行为等价于未装本功能。
        AfterhourWidth = cat.CreateEntry<int>("7F_AfterhourWidth", 12, "[扩容] 地面·宽",
            "「垃圾场拾荒」场景里「地面」窗口的列数（★ 游戏原版 = 12）。" +
            "（默认值 = 当前游戏原版尺寸；[扩容] 启用为 true 时才会应用）");
        AfterhourHeight = cat.CreateEntry<int>("7F_AfterhourHeight", 9, "[扩容] 地面·高",
            "「垃圾场拾荒」场景里「地面」窗口的行数（★ 游戏原版 = 9）。" +
            "（默认值 = 当前游戏原版尺寸；[扩容] 启用为 true 时才会应用）");

        // ── 地面/拾荒窗口·鼠标拖动（7G 域，2026-10-09 新增）──
        GroundDragEnabled = cat.CreateEntry<bool>("7G_GroundDragEnabled", true,
            "[地面] 允许鼠标拖动",
            "true = 可以用鼠标按住「地面」窗口的**标题栏**拖动它（与拖动窗口的通用习惯一致）。" +
            "false = 地面窗口固定不动。★ 拖动窗口时若正按着鼠标拖物品，本功能会自动跳过，不抢物品拖拽。");

        GroundOffsetX = cat.CreateEntry<int>("7G_GroundOffsetX", 0,
            "[地面] 拖动位置·X（自动写入）",
            "「地面」窗口相对游戏原版停靠位的水平偏移（anchoredPosition 像素，正=右）。" +
            "★ 由拖动自动写入，一般不用手改；改为 0 可复位到原版位置。窗口会先被钳制在屏幕内。");

        GroundOffsetY = cat.CreateEntry<int>("7G_GroundOffsetY", 0,
            "[地面] 拖动位置·Y（自动写入）",
            "「地面」窗口相对游戏原版停靠位的垂直偏移（anchoredPosition 像素，正=上）。" +
            "★ 由拖动自动写入，一般不用手改；改为 0 可复位到原版位置。窗口会先被钳制在屏幕内。");

        // ── 迁移（一次性）──
        try { RunMigrationOnce(); } catch (Exception ex) { CatchLogOnce.Warn("cfg.RunMigrationOnce", ex); }

        cat.SaveToFile();

        // ★ 直接编辑 cfg 文件，删除残留旧 key（幂等）。
        //   P6：内部用 SaveBagExpand.cfgcleaned 标记版本，同版本启动不再重跑。
        try { CleanupCfgFile(); } catch (Exception ex) { CatchLogOnce.Warn("cfg.CleanupCfgFile", ex); }
    }

    // ══════════════════════════════════════════════════════════════════
    // 迁移逻辑：把旧版本 cfg 里的旧 key 值迁移到新 key
    // ══════════════════════════════════════════════════════════════════

    private static string MarkerPath()
    {
        try { return Path.Combine(MelonEnvironment.UserDataDirectory, "SaveBagExpand.migrated"); }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.MarkerPath", ex); return null; }
    }

    private static bool ShouldRunMigration()
    {
        try
        {
            string p = MarkerPath();
            if (string.IsNullOrEmpty(p)) return true;
            return !File.Exists(p);
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.ShouldRunMigration", ex); return true; }
    }

    private static void MarkMigrated()
    {
        try
        {
            string p = MarkerPath();
            if (!string.IsNullOrEmpty(p)) File.WriteAllText(p, "1");
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.MarkMigrated", ex); }
    }

    private static bool ReadLegacyBool(string key)
    {
        try
        {
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");
            if (!File.Exists(path)) return false;

            string[] lines = File.ReadAllLines(path);
            bool inSection = false;
            foreach (var raw in lines)
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("["))
                {
                    inSection = line.Equals("[SaveBagExpand]", StringComparison.OrdinalIgnoreCase);
                    continue;
                }
                if (!inSection) continue;

                int eq = line.IndexOf('=');
                if (eq < 0) continue;
                string k = line.Substring(0, eq).Trim();
                if (!k.Equals(key, StringComparison.Ordinal)) continue;

                string v = line.Substring(eq + 1).Trim().ToLowerInvariant();
                return v == "true";
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.ReadLegacyBool", ex); }
        return false;
    }

    private static void RunMigrationOnce()
    {
        // ★ 关键修复：迁移只跑一次。标记文件存在 → 直接返回，整段迁移都跳过
        if (!ShouldRunMigration()) return;

        bool didMigrate = false;

        // ── 迁移 1：旧容器开关 → ExpandEnabled ──
        bool legacyExpand = false;

        try
        {
            bool agg = ReadLegacyBool("SaveBagAggressive");
            bool en = ReadLegacyBool("SaveBagEnable");
            if (agg && en) legacyExpand = true;
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.RunMigrationOnce.legacyExpand", ex); }

        if (ReadLegacyBool("ShowcaseInvEnable")) legacyExpand = true;
        if (ReadLegacyBool("HiddenInvEnable")) legacyExpand = true;
        if (ReadLegacyBool("BackInvEnable")) legacyExpand = true;
        if (ReadLegacyBool("FrontInvEnable")) legacyExpand = true;

        if (legacyExpand && !ExpandEnabled.Value)
        {
            ExpandEnabled.Value = true;
            didMigrate = true;
            SaveBagExpandCore.Log?.Msg("[SaveBagExpand] 迁移：旧容器开关为 true → 已自动开启 [扩容]");
        }

        // ── 迁移 2：SaveBagPagingSwitchEnabled=false → SaveBagPagingEnabled=false ──
        try
        {
            string cfgPath = Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");
            if (File.Exists(cfgPath))
            {
                string all = File.ReadAllText(cfgPath);
                if (all.Contains("SaveBagPagingSwitchEnabled = false"))
                {
                    SaveBagPagingEnabled.Value = false;
                    didMigrate = true;
                    SaveBagExpandCore.Log?.Msg("[SaveBagExpand] 迁移：旧换页开关为 false → 已关闭 [分页]");
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.RunMigrationOnce.pagingSwitch", ex); }

        MarkMigrated();
        if (didMigrate) SaveBagExpandCore.Log?.Msg("[SaveBagExpand] 配置迁移完成");
    }

    // ══════════════════════════════════════════════════════════════════
    // 直接编辑 cfg 文件：删除旧版本残留的 key
    // ══════════════════════════════════════════════════════════════════

    private static readonly string[] LegacyKeys = new[]
    {
        "SaveBagAggressive",
        "SaveBagEnable",
        "ShowcaseInvEnable",
        "HiddenInvEnable",
        "BackInvEnable",
        "FrontInvEnable",
        "SaveBagPagingSwitchEnabled",
        "Page2Ids",
        "Page3Ids",
        // S8g：诊断项合并后遗留的孤儿 key
        "SaveBagPagingDiagLog",
        "SaveBagPagingSimulateSaveFailure",
        // S8p：S8f 删掉的独立开关，cfg 里一直没清（S8f 时决定"留着无害"，
        //       S8p 发 v1.7.3 时顺带加进清理列表）。
        "FrontInvExpandEnabled",
    };

    // ══════════════════════════════════════════════════════════════════
    // P6（2026-10-02）：旧 key 清理的版本标记。
    //
    // 之前每次启动都读整个 cfg + 构建 output 列表 —— ModManager（Nico's Lab）
    // 每次启动会把旧 key 写回，我们下次启动又清一遍，循环重写同一个文件。
    // 改为标记「上次清理时的套件版本」：
    //   · 同版本反复启动          → 直接 return，零 IO
    //   · 套件版本升级后首次启动  → 清理一次，写标记
    //   · 首次安装（无标记文件）  → 清理一次，写标记
    //   · cfg 文件不存在          → 不写标记，下次再来
    //
    // 副作用（有意接受）：同版本内 ModManager 写回的旧 key 会残留（无害，
    // 不再是持久战）。README #10 已同步说明。
    // ══════════════════════════════════════════════════════════════════

    private static string CleanupMarkerPath()
    {
        try { return Path.Combine(MelonEnvironment.UserDataDirectory, "SaveBagExpand.cfgcleaned"); }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.CleanupMarkerPath", ex); return null; }
    }

    private static bool ShouldRunCleanup()
    {
        try
        {
            string p = CleanupMarkerPath();
            if (string.IsNullOrEmpty(p)) return true;      // 定位不到标记 → 保守清理
            if (!File.Exists(p)) return true;              // 首次安装
            string last = File.ReadAllText(p).Trim();
            return !string.Equals(last, SuiteVersion.Suite, StringComparison.Ordinal);
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.ShouldRunCleanup", ex); return true; }
    }

    private static void MarkCleaned()
    {
        try
        {
            string p = CleanupMarkerPath();
            if (!string.IsNullOrEmpty(p)) File.WriteAllText(p, SuiteVersion.Suite);
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.MarkCleaned", ex); }
    }

    private static void CleanupCfgFile()
    {
        // P6：同版本已清过 → 直接返回（不读 cfg、不分配列表、不写文件）
        if (!ShouldRunCleanup()) return;

        try
        {
            string path = Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");
            if (!File.Exists(path))
            {
                // cfg 还没生成（首次启动 / 用户删过）→ 不写标记，下次再来
                return;
            }

            string[] lines = File.ReadAllLines(path);
            var output = new System.Collections.Generic.List<string>();
            bool inSection = false;
            int removed = 0;

            foreach (var raw in lines)
            {
                string trimmed = raw.Trim();

                if (trimmed.StartsWith("[") && trimmed.EndsWith("]"))
                {
                    inSection = trimmed.Equals("[SaveBagExpand]", StringComparison.OrdinalIgnoreCase);
                    output.Add(raw);
                    continue;
                }

                if (!inSection) { output.Add(raw); continue; }

                if (trimmed.Length == 0 || trimmed.StartsWith("#") || trimmed.StartsWith(";"))
                {
                    output.Add(raw);
                    continue;
                }

                int eq = trimmed.IndexOf('=');
                if (eq < 0) { output.Add(raw); continue; }

                string key = trimmed.Substring(0, eq).Trim();
                if (Array.IndexOf(LegacyKeys, key) >= 0)
                {
                    removed++;
                    continue;
                }

                output.Add(raw);
            }

            if (removed > 0)
            {
                // B3：改写**全游戏 mod 共用**的 MelonPreferences.cfg 之前先备份，
                // 只留最近 3 份（时间戳命名，按修改时间裁剪）。
                // 备份失败就不改 —— 宁可留几个旧 key（无害），也不能把 cfg 改坏；
                // 同时不写 MarkCleaned，下次启动会再试一次。
                string bakErr;
                if (!TryBackupCfg(path, out bakErr))
                {
                    SaveBagExpandCore.Log?.Warning(
                        "[SaveBagExpand] cfg 备份失败，本次跳过清理: " + bakErr);
                    return;
                }

                File.WriteAllLines(path, output);
                SaveBagExpandCore.Log?.Msg($"[SaveBagExpand] 已从 cfg 清理 {removed} 个旧配置项");
            }

            // P6：扫过一遍（无论是否真有 removal）就算「本版本已清理」
            MarkCleaned();
        }
        catch (Exception ex)
        {
            // ★ 2026-10-09：升级为 Warning（原先是 Msg）。
            //   cfg 清理失败会留下本该消失的旧版本 key，MelonLoader 下次可能又把它们
            //   显示回配置界面 —— 这属于故障，不是状态汇报，不该随 DiagLog 一起静默。
            SaveBagExpandCore.Log?.Warning("[SaveBagExpand] cfg 清理异常: " + ex.Message);
        }
    }

    // ── B3：cfg 写前备份（保留最近 CfgBackupKeep 份） ──────────────────

    private const int CfgBackupKeep = 3;

    private static bool TryBackupCfg(string path, out string error)
    {
        error = null;
        try
        {
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            string bak = path + "." + stamp + ".bak";
            File.Copy(path, bak, true);
            PruneCfgBackups(path);
            return true;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }

    private static void PruneCfgBackups(string path)
    {
        try
        {
            string dir = Path.GetDirectoryName(path);
            if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) return;

            string prefix = Path.GetFileName(path) + ".";   // "MelonPreferences.cfg."
            var found = new System.Collections.Generic.List<string>();
            foreach (string f in Directory.GetFiles(dir, "*.bak"))
                if (Path.GetFileName(f).StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                    found.Add(f);

            if (found.Count <= CfgBackupKeep) return;

            // 时间戳在文件名里，但按修改时间排更稳（手动拷贝过的文件也认）
            found.Sort((a, b) =>
                File.GetLastWriteTimeUtc(b).CompareTo(File.GetLastWriteTimeUtc(a)));

            for (int i = CfgBackupKeep; i < found.Count; i++)
                File.Delete(found[i]);
        }
        catch (Exception ex) { CatchLogOnce.Warn("cfg.PruneCfgBackups", ex); }
    }


    internal static int Clamp(int value)
    {
        if (value <= 0) return 0;
        if (value > 50) return 50;
        return value;
    }
}