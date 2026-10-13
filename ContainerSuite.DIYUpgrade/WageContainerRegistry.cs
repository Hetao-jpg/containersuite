using ContainerSuite.Shared;
using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;

namespace DIYUpgrade;

/// <summary>
/// WageMod 容器注册与识别层。
/// 集中管理妙妙箱、虚空珠、Mod 容器的 GridDef 与识别逻辑，不涉及 Harmony Patch。
///
/// 2026-09-29 关键改动：
///   旧 GetWageCategory 对满级妙妙箱 / 虚空珠有「隐式接管」逻辑（wb_stage >= 5
///   就直接认作 DIY 容器），但 HasDiyMigrated 此时仍为 false —— 于是旧档加载时
///   蛙哥恢复链和 DIY 恢复链会同时改同一个 GameGridInventory。
///   新逻辑：只有明确迁移过（HasDiyMigrated == true）才接管，迁移只在
///   ReapplyFromMeta / PrepareGrid 的显式迁移分支里触发。
/// </summary>
internal static class WageContainerRegistry
{
    // ============ 容器 ID ============
    public const string IdWageBox = "custom_storage_box";
    public const string IdVoidBead = "void_bead_storage";
    public const string IdGuMachine = "wage_gu_machine";
    public const string IdAiGenerator = "wage_ai_generator";

    // ============ 标签 ============
    public const string TagDiyMigrated = "DIY_MIGRATED_TAG";
    public const string TagWbStage = "wb_stage";
    public const string TagVoidBeadSlots = "VOID_BEAD_SLOTS_TAG";

    /// <summary>
    /// S8l：虚空珠已永久移交给 DIY 的标记。
    /// <para>
    /// 单独一个 tag，不复用 <see cref="TagDiyMigrated"/>：两者语义不同。
    /// TagDiyMigrated 是"基准尺寸已记录"，可能在任何容器上出现；
    /// 本 tag 专表"虚空珠的主权已从蛙哥转到 DIY"，是**单向门**。
    /// </para>
    /// </summary>
    public const string TagVoidBeadHandedOver = "DIY_VOIDBEAD_HANDOVER";

    // ============ 蛙哥链上限 ============
    public const int WageBoxMaxStage = 5;        // 妙妙箱满级段位
    public const int VoidBeadMaxSlots = 200;      // 虚空珠满级格数

    // ============ GridDef ============
    // 注：这三个是 public 可变静态字段（供本文件其它成员及跨文件读取），由
    //     RegisterWageDefinitions 一次性赋值。下划线命名沿用历史习惯，
    //     不改名、不封装为属性，以免牵动其它文件。
    public static DiyUpgradeMod.GridDef _wageBox;
    public static DiyUpgradeMod.GridDef _wageBackpack;
    public static DiyUpgradeMod.GridDef _modContainer;

    // ============ DIY 已接管的虚空珠网格指针（替代 GetParentItem）============
    private static readonly HashSet<IntPtr> _diyVoidBeadGrids = new HashSet<IntPtr>();

    /// <summary>规范化容器 ID：清 \0、去首尾空白、转小写。与 DiyUpgradeMod.IdOf 语义一致。</summary>
    internal static string NormalizeId(GameItem item)
    {
        if (item == null) return "";
        try
        {
            string raw = item.identifier ?? "";
            return raw.Replace("\0", "").Trim().ToLowerInvariant();
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("WageContainerRegistry.NormalizeId", ex);
            return "";
        }
    }

    public static bool IsDiyVoidBeadGrid(IntPtr ptr)
    {
        return ptr != IntPtr.Zero && _diyVoidBeadGrids.Contains(ptr);
    }

    public static void RegisterVoidBeadGrid(GameGridInventory grid)
    {
        if (grid == null) return;
        try { _diyVoidBeadGrids.Add(((Il2CppObjectBase)grid).Pointer); } catch (System.Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.RegisterVoidBeadGrid", ex); }
    }

    public static void ClearVoidBeadGrids()
    {
        _diyVoidBeadGrids.Clear();
    }

    // ============ 注册 ============

    /// <summary>
    /// 是否要为蛙哥相关容器创建配置项（2026-10-02）。
    /// 没装蛙哥 mod 的玩家不该在 cfg 里多出 24 行永远用不上的配置。
    /// <para>
    /// ★ 但要防"配置丢失"：MelonPreferences 保存时按内存里已注册的条目重写整个文件，
    /// 一旦这里返回 false 而用户 cfg 里已有这些键，用户调过的值会被连键一起删掉。
    /// 所以只要 cfg 里已经存在这些键（说明以前装过）就继续创建。
    /// </para>
    /// </summary>
    public static bool ShouldCreateWageEntries(string categoryId)
    {
        if (IsWageModPresent()) return true;
        // 键名历经三代，三个名字都要查 —— 用户此刻很可能还没走迁移
        //   （cfg 里仍是上一代的键名），漏查旧名会让"装了蛙哥 mod 又卸掉"的玩家
        //   直接丢掉整个妙妙箱配置段（没有报错的那种丢）。
        //   ★ 这些是**历史名**，故意留字面量：它们本来就不该出现在 CfgKeys 里。
        return DiyUpgradeMod.CfgSectionHasKey(categoryId, "WageBoxEnable")            // 第 1 代
            || DiyUpgradeMod.CfgSectionHasKey(categoryId, "5B_WageBoxEnable")         // 第 2 代
            || DiyUpgradeMod.CfgSectionHasKey(categoryId, CfgKeys.WageBox.Enable);    // 当前
    }

    // 会话内结果稳定（Mods 目录不会在运行中变化），缓存一次，避免每次配置枚举都扫盘。
    private static bool _wageModPresentChecked;
    private static bool _wageModPresent;

    /// <summary>扫描 Mods 目录（含 Disabled）看有没有蛙哥 mod 的 DLL。</summary>
    private static bool IsWageModPresent()
    {
        if (_wageModPresentChecked) return _wageModPresent;
        _wageModPresentChecked = true;
        try
        {
            foreach (string dir in new[]
            {
                MelonLoader.Utils.MelonEnvironment.ModsDirectory,
                System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.ModsDirectory, "Disabled"),
            })
            {
                if (string.IsNullOrEmpty(dir) || !System.IO.Directory.Exists(dir)) continue;
                foreach (string file in System.IO.Directory.GetFiles(dir, "*.dll"))
                {
                    string name = System.IO.Path.GetFileNameWithoutExtension(file);
                    if (name.IndexOf("Wage", StringComparison.OrdinalIgnoreCase) >= 0)
                        return _wageModPresent = true;
                }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.IsWageModPresent", ex); }
        return _wageModPresent = false;
    }

    public static void RegisterWageDefinitions(MelonPreferences_Category cat)
    {
        // 2026-10-08：默认值改从 DiyUpgradeMod.DefaultIdLists 取（单一事实源），
        //   否则这三条 ID 串与 EnsureDefaultIds 的补齐逻辑各写一遍、必然漂移。
        _wageBox = DiyUpgradeMod.CreateGridDef(
            cat, CfgKeys.WageBox, "蛙哥妙妙箱",
            DiyUpgradeMod.DefaultIdsOf("WageBox"), 1, 52, 30, null, "蛙哥妙妙箱（WageMod）", true);

        _wageBackpack = DiyUpgradeMod.CreateGridDef(
            cat, CfgKeys.WageBackpack, "虚空珠储物袋",
            DiyUpgradeMod.DefaultIdsOf("WageBackpack"), 1, 34, 10, null, "虚空珠储物袋（WageMod）", true);

        _modContainer = DiyUpgradeMod.CreateGridDef(
            cat, CfgKeys.ModContainer, "Mod容器",
            DiyUpgradeMod.DefaultIdsOf("ModContainer"), 1, 20, 20, null,
            "WageMod 机器容器（养蛊机 / AI 生成器）", true);
    }

    // ============ 识别（全部走 NormalizeId）============
    public static bool IsWageContainer(GameItem item)
    {
        if (item == null) return false;
        string id = NormalizeId(item);
        return id == IdWageBox || id == IdVoidBead || id == IdGuMachine || id == IdAiGenerator;
    }

    public static bool IsWageBox(GameItem item)
    {
        if (item == null) return false;
        return NormalizeId(item) == IdWageBox;
    }

    public static bool IsVoidBeadStorage(GameItem item)
    {
        if (item == null) return false;
        return NormalizeId(item) == IdVoidBead;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2026-09-29：加 item.state 检查。
    // 加载早期 item.state 尚未初始化，IsTag 会抛 native 异常。
    // ═══════════════════════════════════════════════════════════════════════
    public static bool HasDiyMigrated(GameItem item)
    {
        if (item == null) return false;
        try
        {
            if (item.state == null) return false;
            return item.IsTag(TagDiyMigrated);
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("WageContainerRegistry.HasDiyMigrated", ex);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 【2026-10-02 S8j】接管条件放宽（核桃决策）
    // ═══════════════════════════════════════════════════════════════════════
    // 【旧逻辑的问题】只有 HasDiyMigrated（DIY_MIGRATED_TAG）才认领。实测后果：
    //   NEI 刷出来的 void_bead_storage / custom_storage_box 都是全新的、没 tag 的
    //   → GetWageCategory 返回 None → MatchCategory 走到底 → 四个组 IdList
    //     都不含这两个 ID → TryResolve 返回 false → **DIY 压根不认它是容器**。
    //   玩家视角：普通容器能升级，蛙哥的容器完全没反应，日志一条都没有。
    //   （2026-09-29 去掉"隐式接管"是为了防旧档加载时两边一起改网格，
    //     那个目的仍然有效 —— 改用"满级门"实现，见下。）
    //
    // 【新逻辑】
    //   · 虚空珠 void_bead_storage：**默认不接管**（S8l，核桃实机反馈后改）。
    //     S8j 时是无条件接管 → 蛙哥原版「拖垃圾 +1 格」逐格解锁到 200 格的
    //     机制**完全失效**，窗口标题退回游戏本体的"虚空珠 储物区"
    //     （蛙哥那 4 处 titleString 一次都没跑到），实机截图确认。
    //     S8l 起改为「移交门」：
    //       · 默认（SkipVoidBead=true）→ 归蛙哥，原机制正常玩
    //       · 已打 TagVoidBeadHandedOver → 归 DIY
    //       · 玩家正按住升级键拖材料 → 立即视为移交意图（由 HandOverVoidBead 执行）
    //     移交**不可逆**：否则松开键就掉回蛙哥，两边来回抖。
    //   · 妙妙箱 custom_storage_box：**蛙哥升满级后**才交给 DIY。
    //     判据 wb_stage >= WageBoxMaxStage。理由：未满级时让蛙哥继续用
    //     nuts_metal 升（那是它的正常玩法，玩家不该被剥夺）；满级后它自己
    //     就不再升了，正好移交。移交后 DIY 用 metal_ingot 升 ——
    //     **材料跟蛙哥不重叠，冲突从根上消失**。
    //   · Mod 容器（养蛊机 / AI 生成器）：一直是无条件接管，行为不变。
    //
    // 【为什么妙妙箱用"满级门"而不是 tag】
    //   MigrateLegacyWageBox 只在第一次 DIY 升级时才打 tag，那时已经晚于
    //   "要不要接管"的判断 —— 循环依赖。满级门是纯读 tag，无副作用。
    // ═══════════════════════════════════════════════════════════════════════
    public static DiyUpgradeMod.Cat GetWageCategory(GameItem item)
    {
        if (item == null) return DiyUpgradeMod.Cat.None;
        string id = NormalizeId(item);
        // 妙妙箱：满级（wb_stage 达到蛙哥上限）后才移交 DIY
        if (id == IdWageBox)
        {
            return IsWageBoxAtMaxStage(item) ? DiyUpgradeMod.Cat.WageBox : DiyUpgradeMod.Cat.None;
        }
        // 虚空珠（S8l）：默认归蛙哥（保住原版逐格解锁）。已移交 或 正按住升级键 → 归 DIY。
        if (id == IdVoidBead)
        {
            // 玩家**关掉**本项（SkipVoidBeadEnabled=false）= 放弃"蛙哥逐格解锁"、全程归 DIY
            // 的无条件接管（S8j 旧行为，保留作逃生口）。注意语义：开关值为 false 时才接管。
            if (!DiyUpgradeMod.SkipVoidBeadEnabled) return DiyUpgradeMod.Cat.WageBackpack;
            if (DiyUpgradeMod.VoidBeadHandedOver(item)) return DiyUpgradeMod.Cat.WageBackpack;
            // ★ 移交意图：玩家按住升级键 + 另一只手拖着可用材料。
            //   判据函数必须无副作用，所以这里**不写 tag**，只把"有意图"暴露出来，
            //   由 HandOverVoidBead 统一执行打 tag + 记基准，避免两处写 tag 打架。
            if (DiyUpgradeMod.VoidBeadHandoverRequested(item)) return DiyUpgradeMod.Cat.WageBackpack;
            return DiyUpgradeMod.Cat.None;
        }
        // Mod 容器直接接管
        if (id == IdGuMachine || id == IdAiGenerator) return DiyUpgradeMod.Cat.ModContainer;
        return DiyUpgradeMod.Cat.None;
    }

    /// <summary>
    /// 执行虚空珠移交（S8l）：打单向门 tag + 记录当前尺寸为 DIY 基准。
    /// <para>
    /// ★ 幂等：已打过 tag 就直接返回。**不能重复记基准** —— 否则玩家第二次
    /// 按住升级键时，会把已经升到 16x16 的尺寸当成"当前基准"，等于白升一级。
    /// </para>
    /// <para>
    /// 基准取当前实际尺寸、不缩回：玩家从蛙哥升到的格数接着往上加。
    /// </para>
    /// </summary>
    public static void HandOverVoidBead(GameItem item, GameGridInventory grid)
    {
        if (item == null) return;
        if (DiyUpgradeMod.VoidBeadHandedOver(item)) return;   // 幂等

        try { TagUtil.SetItemTag(item, TagVoidBeadHandedOver, 1); }
        catch (System.Exception ex) { CatchLogOnce.Warn("WageRegistry:HandOver", ex); }

        if (grid != null && grid.inventoryShape != null)
        {
            int curW = grid.inventoryShape.width;
            int curH = grid.inventoryShape.height;
            if (curW > 0 && curH > 0)
            {
                try
                {
                    DiyUpgradeMod.SaveMeta(item, curW, curH, 0);
                    TagUtil.SetItemTag(item, TagDiyMigrated, 1);
                    DiyUpgradeMod.RememberGridShape(item, curW, curH);
                    RegisterVoidBeadGrid(grid);
                    int slots = TagUtil.SafeTagInt(item, TagVoidBeadSlots, 0);
                    // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 每次读档移交都会触发，属诊断。
                    DiyUpgradeMod.Diag($"[容器升级] 工资盒已移交 DIY，基准 {curW}x{curH}（空珠位={slots}）。"
                        + "蛙哥的「拖垃圾逐格解锁」对这颗珠子永久失效。");
                }
                catch (System.Exception ex) { CatchLogOnce.Warn("WageRegistry:HandOverMeta", ex); }
            }
        }
        else
        {
            // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）。
            DiyUpgradeMod.Diag("[容器升级] 工资盒已移交 DIY，但未读到基准（首次进游戏时可能发生）。");
        }
        try { DiyUpgradeMod.ClearReapplyCache(item); } catch (System.Exception ex) { CatchLogOnce.Warn("WageRegistry:HandOverCache", ex); }
    }

    /// <summary>
    /// 妙妙箱是否已被蛙哥升到满级。读 wb_stage tag（蛙哥自己写的，我们只读不写）。
    /// <para>
    /// 拿不到 tag / tag 为 0 时返回 false —— 宁可"暂不接管"也不要误判成满级，
    /// 因为误判会让 DIY 把一个 1x1 的新箱子当成满级箱子接管。
    /// </para>
    /// </summary>
    public static bool IsWageBoxAtMaxStage(GameItem item)
    {
        if (item == null) return false;
        try
        {
            if (item.state == null) return false;   // 加载期 state 未初始化
            if (!item.IsTag(TagWbStage)) return false;
            // 复用 Shared 的 TagUtil.SafeTagInt（语义与已删除的私有 GetTagIntSafe 一致：
            // 读不到 / 异常一律返回 fallback 0，绝不抛）。
            return TagUtil.SafeTagInt(item, TagWbStage, 0) >= WageBoxMaxStage;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("WageRegistry:MaxStage", ex); return false; }
    }

    public static DiyUpgradeMod.GridDef GetWageGridDef(GameItem item)
    {
        switch (GetWageCategory(item))
        {
            case DiyUpgradeMod.Cat.WageBox: return _wageBox;
            case DiyUpgradeMod.Cat.WageBackpack: return _wageBackpack;
            case DiyUpgradeMod.Cat.ModContainer: return _modContainer;
            default: return null;
        }
    }

    // ============ 虚空珠 DIY 升级成功后的处理 ============
    /// <summary>
    /// 虚空珠 DIY 升级成功后调用：登记指针 + 同步标签（仅显示用）+ 打迁移标记。
    /// </summary>
    // 【2026-10-10 P2】声明式预热：首行 `item == null || grid == null` 即 return。
    //   内部只调 RegisterVoidBeadGrid / RememberGridShape / TagUtil —— 均为已编译叶子。
    [JitWarmup("虚空珠升级后登记：首行判空即返回")]
    public static void OnVoidBeadUpgraded(GameItem item, GameGridInventory grid, int newW, int newH)
    {
        if (item == null || grid == null) return;
        RegisterVoidBeadGrid(grid);
        // 【说明】不再写 TagVoidBeadSlots，避免蛙哥的 UI 显示超出 200 的怪数字。
        // 尺寸由 DIY 自己的 DIY_UPGRADE_W/H_TAG 控制。
        try { DiyUpgradeMod.RememberGridShape(item, newW, newH); } catch (System.Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.OnVoidBeadUpgraded.RememberShape", ex); }
        try { TagUtil.SetItemTag(item, TagDiyMigrated, 1); } catch (System.Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.OnVoidBeadUpgraded.Tag", ex); }
    }

    // ============ 旧档迁移 ============
    /// <summary>
    /// 妙妙箱旧档迁移：仅在 wb_stage >= 5（蛙哥满级）时触发。
    /// </summary>
    public static void MigrateLegacyWageBox(GameItem item, GameGridInventory grid)
    {
        if (item == null || grid == null || grid.inventoryShape == null) return;
        if (!IsWageBox(item)) return;
        if (HasDiyMigrated(item)) return;

        int stage = 0;
        try { stage = TagUtil.SafeTagInt(item, TagWbStage, 0); } catch (System.Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.MigrateLegacyWageBox.Stage", ex); }
        if (stage < WageBoxMaxStage) return;

        int curW = grid.inventoryShape.width;
        int curH = grid.inventoryShape.height;
        if (curW <= 0 || curH <= 0) return;

        DiyUpgradeMod.SaveMeta(item, curW, curH, 0);
        TagUtil.SetItemTag(item, TagDiyMigrated, 1);
        DiyUpgradeMod.RememberGridShape(item, curW, curH);
        // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 迁移诊断。
        DiyUpgradeMod.Diag($"工资盒已生成迁移基准：{curW}x{curH}（wb_stage={stage}），交由 DIY 接管。");
        try { DiyUpgradeMod.ClearReapplyCache(item); } catch (System.Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.MigrateLegacyWageBox.Cache", ex); }
    }

    /// <summary>
    /// 虚空珠旧档迁移（S8l 语义已变）：**仅在玩家已表达移交意图后才触发**。
    /// <para>
    /// S8j 时是"无条件迁移"（因为 GetWageCategory 也无条件接管，两者配套）。
    /// S8l 起 GetWageCategory 默认归蛙哥，所以这里必须同步收紧 ——
    /// 否则玩家还没移交，迁移函数就把基准打了、RegisterVoidBeadGrid 也调了，
    /// 而 GetWageCategory 仍返回 None → DIY 认都不认它，却已经动过它的数据。
    /// </para>
    /// <para>
    /// 移交的正式入口是 <see cref="HandOverVoidBead"/>；这里只负责补记
    /// 「已经移交但基准没写成」的情况（比如移交那一刻网格还没就绪）。
    /// </para>
    /// </summary>
    public static void MigrateLegacyVoidBead(GameItem item, GameGridInventory grid)
    {
        if (item == null || grid == null || grid.inventoryShape == null) return;
        if (!IsVoidBeadStorage(item)) return;
        if (HasDiyMigrated(item)) return;
        // S8l：没移交就完全不碰。基准由 HandOverVoidBead 记。
        if (!DiyUpgradeMod.VoidBeadHandedOver(item)) return;

        // 基准取当前实际尺寸，不缩回；玩家从当前尺寸继续往上加。
        int slots = 0;
        try { slots = TagUtil.SafeTagInt(item, TagVoidBeadSlots, 0); } catch (System.Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.MigrateLegacyVoidBead.Slots", ex); }

        int curW = grid.inventoryShape.width;
        int curH = grid.inventoryShape.height;
        if (curW <= 0 || curH <= 0) return;

        DiyUpgradeMod.SaveMeta(item, curW, curH, 0);
        TagUtil.SetItemTag(item, TagDiyMigrated, 1);
        DiyUpgradeMod.RememberGridShape(item, curW, curH);
        RegisterVoidBeadGrid(grid);
        // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 迁移诊断。
        DiyUpgradeMod.Diag($"空珠已生成基准：{curW}x{curH}（空珠位={slots}），交由 DIY 接管。");
        try { DiyUpgradeMod.ClearReapplyCache(item); } catch (System.Exception ex) { CatchLogOnce.Warn("WageContainerRegistry.MigrateLegacyVoidBead.Cache", ex); }
    }
}