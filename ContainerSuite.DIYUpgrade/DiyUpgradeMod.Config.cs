// 职责：配置项创建 / 中文显示名 / 升级热键解析
using ContainerSuite.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    private static string GetUpgradeKeyLabel()
    {
        try { return (_upgradeKey != null && !string.IsNullOrEmpty(_upgradeKey.Value)) ? _upgradeKey.Value : "LeftAlt"; }
        // 读配置失败回退默认键名，无害。
        catch { return "LeftAlt"; }
    }
    private static string GetMaterialLabel()
    {
        if (_materialMetalIngot != null && _materialMetalIngot.Value) return "金属锭";
        if (_materialScrapMetal != null && _materialScrapMetal.Value) return "废金属";
        if (_materialJunk != null && _materialJunk.Value) return "垃圾";
        return "材料";
    }
    private static void ApplyConfigLabels()
    {
        try
        {
            // ── A-方案（2026-10-07）：标签统一为「[域] 对象·属性」 ──────────
            // 背景：ModManager 面板按 (段名, 键名) 字典序排，且强制分成
            //   「热键 / 开关 / 数值」三块 → 顺序我们改不了；Description 也完全
            //   不显示（三个 Binding 类只有 Label/Value）。
            // ⇒ 把"这是哪个模块的什么"和"范围 / 危险性"全部压进标签文字本身。
            // ★ 只改显示名：不动键名、不动值、不动任何逻辑，cfg 兼容性零影响。
            //   域固定 6 个：[扩容] [分页] [滚动] [基建] [升级] [诊断]
            SetLabel<bool>(_materialJunk, "[升级] 材料·垃圾", "使用垃圾作为升级材料（优先级最低）");
            SetLabel<bool>(_materialScrapMetal, "[升级] 材料·废金属", "使用废金属作为升级材料（优先级高于垃圾）");
            SetLabel<bool>(_materialMetalIngot, "[升级] 材料·金属锭", "使用金属锭作为升级材料（优先级最高）");
            SetLabel<string>(_upgradeKey, "[升级] 升级键(按住拖材料)", "按住此键再拖材料到容器上才触发升级。默认 LeftAlt；支持组合键如 Ctrl+E");
            SetLabel<bool>(_enableDragResize, "[升级] 拖边改尺寸", "是否允许通过拖拽窗口边缘调整容器大小。默认开启。");
            SetLabel<int>(_cageHpPerCell, "[升级] 笼子·每格血量", "每升级一格增加的血量");
            // ★ 2026-10-07：诊断开关已合并为 CI 的 DiagLog 一条总开关，`_diagLog` 是它的别名。
            //   这里**不能**再 SetLabel —— 那会覆盖掉 CI 给总开关写的中文名与说明
            //   （同一个 entry 实例，标签是全局的）。总开关的标签在
            //   CustomInfrastructureConfig.Init 里随 CreateEntry 一起给出。
            SetLabel<bool>(_restoreOnLoad, "[升级] 读档恢复尺寸", "读档后自动把已升级的容器恢复成升级后的尺寸。若读档崩溃（CLR 0x80131506）请关掉本项。");
            SetLabel<bool>(_groupDragEnabled, "[升级] 批量框选升级", "按住升级键框选多堆材料、拖到容器上松手，连续升级该容器（一堆材料一级）。只对同一个容器生效；蛙哥未满级的妙妙箱、未移交的虚空珠仍由蛙哥处理。");
            // ── S8k 避让热开关 ────────────────────────────────────────────
            // 描述要写清「关掉」的后果，否则玩家只会看到"避让/不避让"两个字。
            SetLabel<bool>(_skipXiaowoBay, "[升级] 避让·精工扩舱",
                "开启=6 个货舱的尺寸主权归本 mod，XIAOWO 无法升级它们。关闭=主权交还 XIAOWO，本 mod 也不再改这些货舱的尺寸（两边会各写各的，可能互相覆盖）。仅在排查冲突时关闭。虚空珠不受本项影响。");
            SetLabel<bool>(_skipWageBox, "[升级] 避让·蛙哥妙妙箱",
                "开启=蛙哥升满级（wb_stage 达上限）后的妙妙箱主权归本 mod。关闭=交还蛙哥。关闭无害——妙妙箱两边材料不重叠（蛙哥用坚果金属、本 mod 用金属锭），不存在抢材料。未满级时本项本来就不生效。");
            SetLabel<bool>(_skipVoidBead, "[升级] 避让·虚空珠(不可逆)",
                "默认开启=虚空珠归蛙哥，保留原版「拖垃圾 +1 格」逐格解锁到 200 格。"
                + "一旦你按住升级键（默认 LeftAlt）拖材料到虚空珠上，就等于说「这个我要用 DIY 升」，"
                + "会**永久**移交本 mod（之后蛙哥的逐格解锁对该颗珠子永久失效，无法手动换回）。"
                + "只有在你不想让出主权时才关闭本项——关闭后虚空珠无条件归本 mod，"
                + "蛙哥的逐格解锁立刻完全失效（这是 S8j 的旧行为）。");
            ApplyGridLabels(_box, "箱子");
            ApplyGridLabels(_backpack, "背包");
            ApplyGridLabels(_cage, "笼子");
            ApplyGridLabels(_multiSlot, "多槽");
            ApplyGridLabels(_multiSlotModule, "模组槽");
            ApplyGridLabels(WageContainerRegistry._wageBox, "妙妙箱");
            ApplyGridLabels(WageContainerRegistry._wageBackpack, "虚空珠袋");
            ApplyGridLabels(WageContainerRegistry._modContainer, "Mod容器");
            // ── 2026-10-07：全局升级增量（取代原先「每类别每槽」的 IncW/IncH）──
            SetLabel<int>(_incW, "[升级] 原版·每次+宽", "所有原版容器（箱/背包/笼/多槽/模组槽）每次升级增加的列数。与高度增量相互独立。");
            SetLabel<int>(_incH, "[升级] 原版·每次+高", "所有原版容器每次升级增加的行数。与宽度增量相互独立。");
            SetLabel<int>(_modIncW, "[升级] Mod容器·每次+宽", "所有 Mod 容器（蛙哥妙妙箱/虚空珠/养蛊机/AI生成器）每次升级增加的列数。");
            SetLabel<int>(_modIncH, "[升级] Mod容器·每次+高", "所有 Mod 容器每次升级增加的行数。");
            // 2026-10-13：每容器 id 独立上限（最细的一层，排在最后）。
            //   它是字符串项 ⇒ 面板里看不到，这里给标签只是为了让 cfg 注释与日志文案统一。
            SetLabel<string>(_perIdSizeLimits, "[升级] 单个容器·宽高上限（手改cfg）", DefaultPerIdSizeLimitsComment);
        }
        catch (Exception ex) { if (_log != null) _log.Error("[容器升级] 套用配置标签异常: " + ex.Message); }
    }
    private static void ApplyGridLabels(GridDef d, string label)
    {
        if (d == null) return;
        // A-方案（2026-10-07）：标签统一为 [升级] 容器·属性。
        //   ★ d.Ids 例外：它对应的「物品ID」条目在面板里**根本不出现**
        //     （string 且不以 Hotkey 结尾 → ModManager 三个注册表都不收），
        //     保留旧样式只为让它写进 cfg 注释时仍然可读。
        const string P = "[升级] ";
        SetLabel<bool>(d.Enable, P + label + "·启用", "是否允许升级");
        SetLabel<string>(d.Ids, label + "：物品ID", "物品ID列表(逗号分隔)");
        SetLabel<int>(d.MaxW, P + label + "·最大宽", "所有槽共享的最大列数");
        SetLabel<int>(d.MaxH, P + label + "·最大高", "所有槽共享的最大行数");
        // ★ 2026-10-02 补：之前漏了 AlignSlots，面板里一直显示英文键名 MultiSlotAlignSlots
        if (d.AlignSlots != null)
            SetLabel<bool>(d.AlignSlots, P + label + "·各槽同尺寸", "开 = 各槽共用第一个槽的尺寸；关 = 各槽独立升级");
        // 2026-10-07 已删除两个分支（都随"每槽开关合并到容器 Enable"而去掉）：
        //   · 每槽的「是否升级此目标网格」标签 —— TargetSlot.Enable 现在恒为 null（无条目可贴）
        //   · 每槽的「宽度增量/高度增量」标签 —— 早已提升为全局 IncW/IncH，在 ApplyConfigLabels 贴
    }
    /// <summary>
    /// 自检：面板显示的是 DisplayName，为空就退化成英文键名（Key）。
    /// 以后新增配置项忘了 SetLabel 时这里会点名，避免"面板里冒出英文"这种低级问题。
    /// </summary>
    private static void WarnAboutUnlabeledEntries(MelonPreferences_Category cat)
    {
        try
        {
            if (cat == null) return;
            var missing = new List<string>();
            foreach (MelonPreferences_Entry e in cat.Entries)
            {
                if (e == null) continue;
                string dn = null;
                // 单个条目读不到显示名不影响自检本身，跳过即可。
                try { dn = e.DisplayName; } catch { }
                if (string.IsNullOrWhiteSpace(dn)) missing.Add(e.Identifier);
            }
            if (missing.Count > 0)
                _log?.Warning($"[容器升级] 有 {missing.Count} 个配置项没有中文显示名（面板里会显示英文键名）："
                    + string.Join(", ", missing));
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    private static void SetLabel<T>(MelonPreferences_Entry<T> entry, string displayName, string description)
    {
        if (entry == null) return;
        try { ((MelonPreferences_Entry)entry).DisplayName = displayName; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        try { ((MelonPreferences_Entry)entry).Description = description; } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
    /// <param name="keys">
    /// 这一类的 5 个键名（<see cref="CfgKeys.Box"/> 等）。键名的**唯一事实源**在
    /// <see cref="CfgKeys"/>，这里不拼字符串、不再传 sortPrefix —— 以前这里是
    /// <c>sp + prefix + "Enable"</c> 的拼接，「键名」和「排序前缀」两个概念混在一个
    /// 参数里，改一处容易漏一处（2026-10-13 收敛）。
    /// </param>
    internal static GridDef CreateGridDef(MelonPreferences_Category cat, ContainerSuite.Shared.CfgKeys.GridKeys keys, string label, string defaultIds, int slotCount, int defaultMaxW = 20, int defaultMaxH = 20, string[] slotNames = null, string idsComment = null, bool isMod = false)
    {
        GridDef gridDef = new GridDef
        {
            Enable = cat.CreateEntry<bool>(keys.Enable, true, (string)null, "是否允许升级" + label, false, false, (ValueValidator)null, (string)null),
            // ★ Ids 是裸名（不带排序前缀）：它是玩家手改的物品清单，改名只有静默丢值的风险。
            Ids = cat.CreateEntry<string>(keys.Ids, defaultIds, (string)null, idsComment ?? (label + "物品ID列表(逗号分隔)"), false, false, (ValueValidator)null, (string)null),
            MaxW = cat.CreateEntry<int>(keys.MaxW, defaultMaxW, (string)null, label + "：所有槽共享的最大宽度", false, false, (ValueValidator)null, (string)null),
            MaxH = cat.CreateEntry<int>(keys.MaxH, defaultMaxH, (string)null, label + "：所有槽共享的最大高度", false, false, (ValueValidator)null, (string)null),
            SlotNames = slotNames
        };
        // 多槽容器（冰箱/熔炉）：「所有槽位尺寸跟随第一个槽」开关。默认开。
        if (slotCount > 1)
        {
            gridDef.AlignSlots = cat.CreateEntry<bool>(keys.AlignSlots, true, (string)null,
                label + "：所有槽位尺寸跟随第一个槽（关=各槽独立）", false, false, (ValueValidator)null, (string)null);
        }
        // ═══════════════════════════════════════════════════════════════════════
        // 2026-10-07：每槽独立开关（MultiSlotT0Enable …）**已合并到容器级 Enable**
        //   （核桃要求：减少面板里的冗余开关）。
        //
        //   Enable = null ⇒ `GetEnabledSlotsWithIndex` / `GetPrimarySlot`
        //   一律视为"已启用"（它们的判据就是 `Enable == null || Enable.Value`）
        //   ⇒ **容器启用时，该容器所有存在的网格一起参与升级 / 渲染**。
        //
        //   ★ 核桃定案（2026-10-07）：**「容器启用」= 启用它所有的格**。
        //     所以 slotCount 维持原值（多槽 4、模组槽 4），原先默认关着的第 3/4 格
        //     现在也随容器开关一起启用。要少启用某几格，把调用处的 slotCount 调小即可。
        //
        //   ★ 被删掉的旧键（新旧两套名字，共 16 个）由
        //     `DiyUpgrade.CfgMigration.RemovedKeys` 做三处清理。
        // ═══════════════════════════════════════════════════════════════════════
        TargetSlot[] slots = new TargetSlot[slotCount];
        for (int i = 0; i < slotCount; i++)
        {
            slots[i] = new TargetSlot
            {
                Enable = null,                       // 见上：无独立开关，一律随容器 Enable
                // 2026-10-07：原「每槽自建 IncW/IncH」改为指向全局共享条目。
                //   isMod=false（原版容器）→ _incW/_incH；isMod=true（Mod 容器）→ _modIncW/_modIncH。
                //   读取点（Upgrade/Meta/Patches/GridWindow）的 targetSlot.IncW.Value 无需改动。
                IncW = isMod ? _modIncW : _incW,
                IncH = isMod ? _modIncH : _incH
            };
        }
        gridDef.Slots = slots;
        return gridDef;
    }
    /// <summary>
    /// 把 cfg 的 5 条 <c>*Ids</c> 原文解析成 id 数组，填进各 <see cref="GridDef.IdList"/>。
    /// <para>
    /// ★ 白名单的唯一权威就是 cfg —— 2026-10-13 起取消了 UserData\ContainerSuite\containers.txt
    ///   那套外挂文件（它曾让"改了 cfg 却没用"变成常态歧义）。现在是**单一事实源**。
    /// ★ 5 条 <c>*Ids</c> 都挂了 OnEntryValueChanged ⇒ cfg 一改这里立刻重解析，不用重启。
    /// </para>
    /// </summary>
    private static void RefreshIdLists()
    {
        _box.IdList = IdMatch.SplitIds(_box.Ids.Value);
        _backpack.IdList = IdMatch.SplitIds(_backpack.Ids.Value);
        _cage.IdList = IdMatch.SplitIds(_cage.Ids.Value);
        _multiSlot.IdList = IdMatch.SplitIds(_multiSlot.Ids.Value);
        _multiSlotModule.IdList = IdMatch.SplitIds(_multiSlotModule.Ids.Value);
        try { _resizableCache.Clear(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
    private static void RefreshUpgradeKey()
    {
        _upgradeModCtrl = false;
        _upgradeModAlt = false;
        _upgradeModShift = false;
        string rawKey = ((_upgradeKey == null || _upgradeKey.Value == null) ? "" : _upgradeKey.Value.Trim());
        if (rawKey.Length == 0 || rawKey.Equals("None", StringComparison.OrdinalIgnoreCase))
        {
            _upgradeKeyCode = (KeyCode)0;
            return;
        }
        string[] parts = rawKey.Split('+');
        KeyCode keyCode = (KeyCode)0;
        for (int i = 0; i < parts.Length; i++)
        {
            string part = parts[i].Trim();
            if (part.Length != 0)
            {
                KeyCode parsed;
                if (part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase)) _upgradeModCtrl = true;
                else if (part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) _upgradeModAlt = true;
                else if (part.Equals("Shift", StringComparison.OrdinalIgnoreCase)) _upgradeModShift = true;
                else if (part == "`" || part == "~") keyCode = (KeyCode)96;
                else if (Enum.TryParse<KeyCode>(part, true, out parsed) && (int)parsed != 0) keyCode = parsed;
            }
        }
        if ((int)keyCode == 0)
        {
            if (_log != null) _log.Warning("[容器升级] 无法识别的升级键名 '" + rawKey + "', 回退为 LeftAlt。");
            _upgradeKeyCode = (KeyCode)308;
        }
        else _upgradeKeyCode = keyCode;
    }
    // S8l 改为 internal：虚空珠移交判据（VoidBeadHandoverRequested）要读它。
    internal static bool IsUpgradeKeyHeld()
    {
        if ((int)_upgradeKeyCode == 0) return false;
        try
        {
            if (!Input.GetKey(_upgradeKeyCode)) return false;
            if (_upgradeModCtrl && !Input.GetKey((KeyCode)306) && !Input.GetKey((KeyCode)305)) return false;
            if (_upgradeModAlt && !Input.GetKey((KeyCode)308) && !Input.GetKey((KeyCode)307)) return false;
            if (_upgradeModShift && !Input.GetKey((KeyCode)304) && !Input.GetKey((KeyCode)303)) return false;
            return true;
        }
        // Input 查询失败按「未按下」处理，无害。
        catch { return false; }
    }
}
