// 由拆解脚本从 自定义集装箱fix/DiyUpgradeMod.cs 机械切分，内容逐字未改。
// 职责：入口 / 生命周期 / 补丁安装与汇总
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ContainerSuite.Shared;   // S2 新增
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
    public static void Init(MelonLogger.Instance logger)
    {
        // S4-2：_log 由 CI 传入（不再从 MelonMod 实例取）
        _log = logger;
        // S5：版本由主入口的统一横幅显示，这里不再各报一个

        // ★ 旧：这里调 SnapshotLegacySection()（原「三段并一段」迁移的第一步）。
        //   该迁移已按终止线 MinSupportedFrom = 1.7.4 整段删除，快照与搬运都不再需要。
        //   详见 DiyUpgrade.CfgMigration.cs 文件头的「迁移终止线」。

        // ★ 2026-10-02：段名从「自定义容器升级」改成「DIYUpgrade」。
        //   ModManager 归属配置项的规则是 CategoryId == DLL 文件名（ManagerState.CategoryBelongsToMod，
        //   只看文件名，不看 MelonInfo 名），中文段名两边都不等 → 面板里一项都不显示。
        // ★ 2026-10-06 合并（B2）：再从「DIYUpgrade」改成「CustomInfrastructure」。
        //   4 个 DLL 合并成 1 个之后文件名恒为 CustomInfrastructure，段名不跟着改的话
        //   本模块 11 个配置项会全部从面板里消失。
        //   旧段 [自定义容器升级] / [DIYUpgrade] 的值由 CfgMigration 一次性搬过来，不丢配置。
        //   必须复用 CI 已创建的同一个 category 实例（否则同名段注册两次、保存互相覆盖）。
        MelonPreferences_Category category = ContainerSuite.Shared.MelonPrefsUtil.GetOrCreate(
            ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId, "自定义基础设施大小");
        _materialJunk = category.CreateEntry<bool>(CfgKeys.MaterialJunk, false, (string)null, "升级材料：使用垃圾", false, false, (ValueValidator)null, (string)null);
        _materialScrapMetal = category.CreateEntry<bool>(CfgKeys.MaterialScrapMetal, false, (string)null, "升级材料：使用废金属", false, false, (ValueValidator)null, (string)null);
        _materialMetalIngot = category.CreateEntry<bool>(CfgKeys.MaterialMetalIngot, true, (string)null, "升级材料：使用金属锭", false, false, (ValueValidator)null, (string)null);
        _upgradeKey = category.CreateEntry<string>(CfgKeys.UpgradeHotkey, "LeftAlt", (string)null, "升级修饰键", false, false, (ValueValidator)null, (string)null);
        _enableDragResize = category.CreateEntry<bool>(CfgKeys.EnableDragResize, true, (string)null, "是否启用拖拽调整窗口大小", false, false, (ValueValidator)null, (string)null);
        _cageHpPerCell = category.CreateEntry<int>(CfgKeys.CageHpPerCell, 20, (string)null, "老鼠笼/动物笼 每格血量加成", false, false, (ValueValidator)null, (string)null);
        // 2026-10-06 合并（B2）：三段并成一段后键名必须互不冲突，只有 DiagLog 撞名
        //   （CI / SBE / DIY 各一个）。CI 保留 DiagLog、SBE 改 SaveBag_DiagLog，
        //   本模块改 DIY_DiagLog。迁移时旧段的 DiagLog 会被重映射到这个新键。
        // ★ 2026-10-07：三个开关再合并成**一个总开关**（CI 的 DiagLog）。
        //   本字段改为它的**别名** —— 同一个 entry 实例，不再自建条目。
        //   所有 `_diagLog.Value` 读取点一行未改；旧键 DIY_DiagLog 由
        //   MigrateDiagKeys 做 OR 迁移 + 三处清理（见下）。
        _diagLog = CustomInfrastructure.CustomInfrastructureConfig.DiagLog;
        _restoreOnLoad = category.CreateEntry<bool>(CfgKeys.RestoreOnLoad, true, (string)null, "读档后恢复已升级容器的尺寸（若读档崩溃请改成 false）", false, false, (ValueValidator)null, (string)null);
        // ── 阶段1（2026-10-11）：批量组拖升级开关，默认开 ──────────────────
        //   补丁常驻，本开关只短路判据（不 Unpatch —— IL2CPP 上动态重装 detour
        //   是崩溃诱因，见 S8k 避让开关注释）。键名 26_ 延续 2x 序。
        _groupDragEnabled = category.CreateEntry<bool>(CfgKeys.EnableGroupDrag, true, (string)null, "批量框选升级：按住升级键框选多堆材料拖到容器上，连续升级（只对同一容器生效）", false, false, (ValueValidator)null, (string)null);
        // P1-3：R1 探针项连实现一起移入 #if DIAG，Release 不再创建该键。
        //
        //   ★ 2026-10-13 删除：R1 探针（`24_R1Probe` + Restore.cs 的 R1ProbeLog 全套）
        //     和拖拽暂存取证探针（`25_ResizeDiag` + DiyUpgradeMod.ResizeDiag.cs 整文件）
        //     的取证目标都已完成：
        //       · ResizeDiag 的假设已被 GridFx.HideOutOfBoundsNodes 的实测数据推翻并修正
        //         （见 GridFx.cs 的「2026-10-10 实测修正」注释块）；
        //       · R1 要回答的"游戏是否持久化升级后尺寸"早已由 _metaCache + tag 持久化解答
        //         （答案：不持久化，本套件靠 DIY_UPGRADE_*_TAG 自己记忆）。
        //     两者都是每会话往 UserData 写文本的实验脚手架，留着只会污染 cfg 与代码。
        //     需要时可从 git 历史找回（commit 4af13c6 之前）。
        // P1-3：同步修饰状态实验开关（T5），保留。
#if DIAG
        //   为什么做成开关而不是直接加：GameItem.SyncModifiedState() 的语义未经核实
        //   （只从蛙哥源码看到它在"批量升级收尾 / 骰子吸收后"调用；本套件此前零调用）。
        //   打开后：每次成功升级的收尾会补调一次，并在异常时无条件留痕 ——
        //   用于回答"我们是不是漏了这一步同步"。定性后再决定转正或删除。
        _syncStateProbe = category.CreateEntry<bool>(CfgKeys.SyncModifiedStateProbe, false, (string)null, "[诊断·实验] 升级收尾补调 GameItem.SyncModifiedState()：验证本套件是否漏了这一步游戏侧同步。默认关。", false, false, (ValueValidator)null, (string)null);
#endif
        // ── S8k：避让热开关（2026-10-02）────────────────────────────────────
        // 语义：「关」= 把尺寸主权交还给对方 mod；默认「开」= 全权优先。
        // 补丁始终常驻，开关只短路判据函数的第一行（不 Unpatch、不重装），
        // 所以同一帧生效，且不会在 IL2CPP 上动态重生成 detour 导致
        // Internal CLR error 0x80131506。
        // ★ 虚空珠（S8k 时刻意不给开关）在 S8l 改为一个**语义相反**的「移交门」，
        //   见下面的 3C_SkipVoidBead。原因与语义详见 State.cs 字段注释。
        _skipXiaowoBay = category.CreateEntry<bool>(CfgKeys.SkipXiaoBay, true, (string)null, "避让·精工扩舱（关掉则 6 个货舱的尺寸主权交还 XIAOWO）", false, false, (ValueValidator)null, (string)null);
        _skipWageBox = category.CreateEntry<bool>(CfgKeys.SkipWageBox, true, (string)null, "避让·蛙哥妙妙箱（关掉则满级妙妙箱的尺寸主权交还蛙哥）", false, false, (ValueValidator)null, (string)null);
        // ── S8l：虚空珠移交门（默认 true = 避让开 = 蛙哥全权，原机制可用）────
        // 语义与上面两个**相反**：这里"开"= 蛙哥全权。玩家按住升级键拖材料
        // 即自动永久移交；关掉本项 = 断掉移交可能，无条件归 DIY（S8j 旧行为）。
        // 键名沿用历史语义（看着像"跳过虚空珠"，实为"移交门"）。
        // 2026-10-07 键名重排：只加域号前缀变成 3C_SkipVoidBead，名字本身不动；
        //   语义以描述文案为准。
        _skipVoidBead = category.CreateEntry<bool>(CfgKeys.SkipVoidBead, true, (string)null, "避让·蛙哥虚空珠。⚠ 语义与其他 Skip* 项相反：true=蛙哥主权（保原版逐格解锁；按住升级键拖材料到虚空珠=永久移交 DIY），false=无条件归 DIY（排查冲突才用）", false, false, (ValueValidator)null, (string)null);
        // 【2026-10-10】僵尸手柄清扫开关。★ **唯一权威默认值写在这里** —— 其余文件只引用本处、
        //   不得各写一份数字（曾经 3 处各写一份且互相矛盾，见 容器优化方案.md P0-1）。
        //   默认 **false（不清扫）**：09:17 实测「门禁 + 2s 消抖都生效、世界就绪后 2.5 秒执行
        //   仍然崩 0x80131506」⇒ 崩点坐实为清扫动作本身，与深栈/时机无关。
        //   17:40 虽把清扫改为「熄火」（SetActive(false)，不再反射调 XIAOWO 的 DestroyHandles），
        //   但该改动**尚未经新一次实机 A/B 证明不崩**，故在拿到新证据前保持 fail-safe：不清扫。
        //   排查/复验时置 true：开=崩、关=不崩 即为坐实崩点的最小实验。
        _sweepZombieHandles = category.CreateEntry<bool>(CfgKeys.SweepZombieHandles, false, (string)null, "清理 XIAOWO 残留拖拽手柄（熄火式：SetActive(false)）。默认关：旧崩溃未复验，置 true 前请先实机确认不崩", false, false, (ValueValidator)null, (string)null);
        // 2026-10-07：原「每类别每槽」升级增量提升为全局两级（详见 DiyUpgradeMod.State.cs 字段注释）。
        //   必须先于任何 CreateGridDef 创建 —— CreateGridDef 内部会把各槽的 IncW/IncH 指向这两个共享条目。
        _incW = category.CreateEntry<int>(CfgKeys.IncW, 1, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
        _incH = category.CreateEntry<int>(CfgKeys.IncH, 1, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
        _modIncW = category.CreateEntry<int>(CfgKeys.ModIncW, 1, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
        _modIncH = category.CreateEntry<int>(CfgKeys.ModIncH, 1, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
        // ★ 2026-10-08：5 个 *Ids 的默认值改从 DefaultIdsOf(...) 取（单一事实源在
        //   DiyUpgrade.CfgMigration.DefaultIdLists）。
        //   为什么不把字面量留在这里：CreateEntry 的默认值**只在条目首次创建时生效**，
        //   老玩家 cfg 里已存着旧串 —— 只改这里的字面量对他们零效果（白名单认不出新容器，
        //   功能静默失效）。因此另配一次性补齐 EnsureDefaultIds，见下面 Init 的调用点。
        _box = CreateGridDef(category, CfgKeys.Box, "箱子类容器", DefaultIdsOf("Box"), 1, 50, 30, null, "箱子类容器物品ID列表(逗号分隔)。");
        _backpack = CreateGridDef(category, CfgKeys.Backpack, "背包类容器", DefaultIdsOf("Backpack"), 1, 20, 20, null, "背包类容器物品ID列表(逗号分隔)。");
        _cage = CreateGridDef(category, CfgKeys.Cage, "老鼠笼/动物笼", DefaultIdsOf("Cage"), 1, 20, 20, null, "老鼠笼/动物笼物品ID列表(逗号分隔)。");
        // 2026-10-07：每槽开关合并到容器 Enable 后，「容器启用 = 启用所有格」⇒ slotCount 维持 4。
        _multiSlot = CreateGridDef(category, CfgKeys.MultiSlot, "多槽容器", DefaultIdsOf("MultiSlot"), 4, 20, 20, null, "多槽容器物品ID列表(逗号分隔)。");
        _multiSlotModule = CreateGridDef(category, CfgKeys.MultiSlotModule, "机器模组槽", DefaultIdsOf("MultiSlotModule"), 4, 10, 10, new string[4] { "模组", "输入", "输出", "待定" }, "机器模组槽物品ID列表(逗号分隔)。");
        // 没装蛙哥 mod 就不创建那 24 项（曾经装过则继续创建，避免把用户调过的值删掉）
        if (WageContainerRegistry.ShouldCreateWageEntries(category.Identifier))
            WageContainerRegistry.RegisterWageDefinitions(category);
        else
            _log?.Msg("[容器升级] 未检测到蛙哥相关 mod，跳过 WageBox / WageBackpack / ModContainer 配置项");
        // S2（2026-10-13 改为 cfg 内的最后一条）：每容器 id 独立尺寸上限。
        //   ★ 刻意放在**所有 GridDef 之后** —— 它在逻辑上是"比分类更细的一层"，
        //     注册顺序决定了它写进 cfg 时也排在末尾（面板里同理），符合由总到分。
        RegisterPerIdSizeLimits(category);
        // 键名迁移（两代叠加，见 CfgMigration.Rename.KeyRenames）：把旧键名
        //   （BoxEnable 等历史名 / 上一代的编号名）的值搬到 CfgKeys 里的当前键名。
        //   ★ 必须排在 MigrateIncKeys 之前 —— 它先把「全局 IncW/IncH/ModIncW/ModIncH」
        //     搬到 IncW/IncH/ModIncW/ModIncH 的新键，随后 MigrateIncKeys 的「每槽→全局」合并只会"只升不降"，
        //     不会把已经正确的全局值压回去。
        //   （原「三段并一段」ApplyLegacySnapshot 已按终止线 MinSupportedFrom=1.7.4 删除，
        //     早于 1.7.4 的旧段格式不再迁移，见 DiyUpgrade.CfgMigration.cs 文件头。）
        MigrateRenamedKeys(category);
        MigrateIncKeys(category);             // 2026-10-07：把旧「每槽」升级增量合并到全局 IncW/IncH / ModIncW/ModIncH
        MigrateDiagKeys(category);            // 2026-10-07：三个模块的诊断开关合并为一个总开关（OR 迁移 + 清旧键）
        // 2026-10-08：把代码默认新增的物品ID追加进各 *Ids（只追加，不动玩家手改的值）。
        EnsureDefaultIds(category);
        category.SaveToFile(true);
        RefreshIdLists();
        RefreshUpgradeKey();
        ((MelonEventBase<LemonAction<string, string>>)(object)_box.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate { RefreshIdLists(); }, 0, false);
        ((MelonEventBase<LemonAction<string, string>>)(object)_backpack.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate { RefreshIdLists(); }, 0, false);
        ((MelonEventBase<LemonAction<string, string>>)(object)_cage.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate { RefreshIdLists(); }, 0, false);
        ((MelonEventBase<LemonAction<string, string>>)(object)_multiSlot.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate { RefreshIdLists(); }, 0, false);
        ((MelonEventBase<LemonAction<string, string>>)(object)_multiSlotModule.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate { RefreshIdLists(); }, 0, false);
        ((MelonEventBase<LemonAction<string, string>>)(object)_upgradeKey.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate { RefreshUpgradeKey(); }, 0, false);
        ApplyConfigLabels();
        WarnAboutUnlabeledEntries(category);   // 自检：漏标中文名会在面板里露出英文键名
        LogPerIdLimitsSummary();               // 每容器上限到底配成了什么样（无条件输出）

        // ═══════════════════════════════════════════════════════════════════
        // 2026-09-29：Harmony 实例列表化，避免 ReportPatchSummary 只统计主
        // 实例、遗漏另外三个子模块的补丁。每个子模块仍保持独立 Harmony ID，
        // 便于在其它 mod 的日志里单独识别。
        // ═══════════════════════════════════════════════════════════════════
        HarmonyLib.Harmony hMain = new HarmonyLib.Harmony("DIYUpgrade");
        InstallPatches(hMain);
        HarmonyLib.Harmony hXiaowo = new HarmonyLib.Harmony("DIYUpgrade.XiaowoCompat");
        SkipXiaowoBayRestore.Install(hXiaowo);
        HarmonyLib.Harmony hWage = new HarmonyLib.Harmony("DIYUpgrade.WageCompat");
        SkipWageUpgradeRestore.Install(hWage);
        HarmonyLib.Harmony hShow = new HarmonyLib.Harmony("DIYUpgrade.WindowShowHook");
        WindowShowHook.Install(hShow);
        ReportPatchSummary(new HarmonyLib.Harmony[] { hMain, hXiaowo, hWage, hShow });
        // S2：把 DIYUpgrade 的恢复逻辑注册到 Shared 的 RestoreArbiter。
        // 之后 RestoreArbiter.Tick() 会按 reason 前缀 "DIY" 分发到这里。
        // 注意：必须在任何 Request(item, "DIY...") 之前注册。
        RestoreArbiter.RegisterApplier("DIY", item => ReapplyFromMeta(item, force: true));
        // 偏移自检预热（审查 #6）：提前解析好，省得首次用到时才做。
        // 默认不输出任何日志；只有 DiagLog=true 时才打印完整解析报告。
        WarmUpOffsetDiag();
        // 2026-10-13：完整偏移解析报告仅 DIAG 构建输出（LogOffsetDiag 已收进 #if DIAG）。
#if DIAG
        if (_diagLog != null && _diagLog.Value) LogOffsetDiag();
#endif
        // P1-1：登记「场景切换必须清空」的可重建缓存（OnSceneLoaded 只调 ClearAll）。
        RegisterSceneCaches();
        _log.Msg($"已加载。读档尺寸恢复={(_restoreOnLoad == null || _restoreOnLoad.Value ? "开" : "关")}");
    }

    /// <summary>
    /// P1-1（2026-10-10）：把本 mod 拥有的「可重建缓存」登记到 <see cref="CacheRegistry"/>。
    /// <para>
    /// ★ 登记顺序 = 旧 OnSceneLoaded 的手写清理顺序（先登记的先清），保证行为等价。
    /// ★ 新增缓存字段必须在此登记 —— 否则 <c>_shared\check-forbidden.ps1</c> 的 R6
    ///   会在编译期失败（字段名以 Cache / Seen 结尾者是 R6 的判定范围）。
    /// </para>
    /// </summary>
    private static void RegisterSceneCaches()
    {
        CacheRegistry.Register("_metaCache", () => _metaCache.Clear());
        CacheRegistry.Register("_lastReapplyFromMetaTime", () => _lastReapplyFromMetaTime.Clear());
        CacheRegistry.Register("_ggiCache", () => _ggiCache.Clear());
        CacheRegistry.Register("_prepareCache", () => _prepareCache.Clear());
        CacheRegistry.Register("_resizableCache", () => _resizableCache.Clear());
        CacheRegistry.Register("_lastLogTimeByKey", () => _lastLogTimeByKey.Clear());
        CacheRegistry.Register("_redrawPending", () => _redrawPending.Clear());
        CacheRegistry.Register("_restoreMemo", ClearRestoreMemo);
        CacheRegistry.Register("_lastHideBounds", GridFx.ClearCaches);
        CacheRegistry.Register("_peakCache", () => _peakCache.Clear());
        // 以下两项原先由 OnSceneLoaded 的其它调用间接清理（_offsetCache 随
        // ResetDistrust；_diagSeen 从不清理）—— 收拢到登记处后语义更明确。
        // ★ A-2（2026-10-12）：_offsetCache 声明为 survive（读档不清）——
        //   键是 type|fallback|候选名，与存档内容无关、进程内恒定有效。
        //   读档时清它零收益，代价是恢复深栈上的首次 FieldOffset 白跑一次反射解析
        //   （2026-10-10 读档闪退修复要避免的正是这类操作）。
        //   其余项（含 _lastLogTimeByKey / _diagSeen）读档时清 —— 换档 = 新会话，
        //   日志去重集合一并重置，与 OnSceneWasLoaded 里 CatchLogOnce.Clear() 同哲学。
        CacheRegistry.Register("_offsetCache", OffsetResolver.ClearCache, survivesLoadGame: true);
        CacheRegistry.Register("_diagSeen", ContainerSuiteLog.ClearDiagOnce);
        // T2（2026-10-12）：键是容器裸指针 ⇒ 场景切换与读档都必须清（复用地址后
        //   旧指针会命中上一个世界的条目，导致"本帧已升过"误判 → 升级被无理由拒绝）。
        CacheRegistry.Register("_upgradeStampCache", () => _upgradeStampCache.Clear());
    }

    public static void OnSceneLoaded(int index, string sceneName)
    {
        // ═══════════════════════════════════════════════════════════════════
        // 2026-09-29：场景切换 = 世界重置。把门禁关掉、代际号自增、所有
        // 以裸指针为键的缓存全部清空 —— 否则 IL2CPP 复用同一地址时，
        // 会命中上一场景已释放对象。
        // ═══════════════════════════════════════════════════════════════════
        RestoreArbiter.ClearAll();
        WorldReadyGate.Invalidate("SceneLoaded");
        BumpSceneGeneration();

        // P1-1（2026-10-10）：可重建缓存统一走 CacheRegistry —— 登记点在
        //   Init() 的 RegisterSceneCaches()，新增缓存若忘登记由门禁 R6 在编译期
        //   拦下（见 _shared\check-forbidden.ps1）。登记顺序与旧手写序列
        //   （_metaCache → ... → _peakCache）一一对应，行为等价。
        CacheRegistry.ClearAll();

        _offsetCheckFails = 0;
        _offsetChecked = false;
        OffsetResolver.ResetDistrust();   // 允许新场景重新探测偏移
        _idOffsetFromResolve = false;
        _cachedIdOffset = -1;             // S8u：清 IdOffset 缓存，下次重新解析

        ContainerWindowResizer.InvalidateCursor();
        ContainerWindowResizer.ResetAllRestoreDone();
        WageContainerRegistry.ClearVoidBeadGrids();
        ContainerSuite.Shared.SaveSlotScope.Invalidate();

        // 阶段1（2026-10-11）：组拖状态（进行中的动作，非"缓存"）。
        //   场景重置时框选材料指针必然失效（IL2CPP 复用地址）⇒ 必须清。
        //   不清的后果：读档后残留的武装标志 + 陈旧指针，让下一次组拖
        //   以错误的材料集启动。注：StartGroupDrag 的 Prefix 也会重置，
        //   这里是双保险（且覆盖"未走 Start 就直接 End"的极端情况）。
        // ★ B-3'（2026-10-12）：快照已改为「指针 + uniqueId」结对，但清理时机不变 ——
        //   跨场景后指针与 uid 都可能对应到别的东西，一起丢。
        ResetGroupDragState();
    }

    /// <summary>
    /// 清空组拖的全部临时状态。场景切换 / 异常路径调用。
    /// 见 DiyUpgradeMod.GroupDrag.cs。
    /// </summary>
    private static void ResetGroupDragState()
    {
        try { _groupDragMaterials.Clear(); } catch { }
        _groupDragArmed = false;
        // ★ 刻意**不**动 _groupUpgradeRunning：协程可能真的还在跑
        //   （读档期间它会在下一步的 ShouldDefer 检查处自行退出并复位）。
        //   这里强行置 false 会让"正在跑的协程"和"新协程"都认为自己独占，
        //   失去防重入保护。让协程自己收尾。
    }

    public static void Tick()
    {
        // （2026-10-13：原「R1 探针 / ResizeDiag 存活标记」两块随两个探针一并删除。）

        // 【2026-10-09 读档崩溃定位】启动期（主菜单、门禁判断之前）一次性预热
        //   读档恢复链的 JIT —— 与 SBE 的 WarmupJit 同理，必须早于任何读档，
        //   否则首次编译正好落在"读档深栈 + MonoMod CompileMethodHook"上而崩。
        ContainerSuite.Shared.CrashProbe.Enter("DIY.WarmupSaveLoadChain");
        WarmupSaveLoadChain();
        ContainerSuite.Shared.CrashProbe.Exit("DIY.WarmupSaveLoadChain");

        // S4-2：RestoreArbiter.Tick 移交给 CI 的 OnUpdate 统一调用。
        // 原因：SBE 也用 RestoreArbiter，若 DIY 和 CI 各 tick 一次，会处理两遍。
        ContainerSuite.Shared.CrashProbe.Enter("DIY.ContainerWindowResizer.Tick");
        ContainerWindowResizer.Tick();
        ContainerSuite.Shared.CrashProbe.Exit("DIY.ContainerWindowResizer.Tick");

        // S8n：清理 XIAOWO 残留拖拽手柄（2 秒节流，开关关着时内部直接返回）。
        // 必须放在 DIY 自己的手柄 tick 之后 —— 先让本 mod 把手柄摆到前面，
        // 再把对面的清掉，避免同一帧内两边互相 BringHandlesToFront 抢 sibling 顺序。
        // 【2026-10-10】外层再包一层兜底：Tick 是每帧热路径，任何未预期异常都不该
        //   从 Tick 里冒出来打断后续逻辑（内部实现虽已全 try/catch，但未来改动可能
        //   引入新裸路径）。这是"防回归"而非"修当前 bug"——当前实现已安全。
        ContainerSuite.Shared.CrashProbe.Enter("DIY.TickZombieHandles");
        try { SkipXiaowoBayRestore.TickZombieHandles(); }
        catch (Exception ex) { CatchLogOnce.Warn("Tick:SkipXiaowoBayRestore", ex); }
        ContainerSuite.Shared.CrashProbe.Exit("DIY.TickZombieHandles");

        ContainerSuite.Shared.CrashProbe.Enter("DIY.TryFlushPrefs");
        TryFlushPrefs();
        ContainerSuite.Shared.CrashProbe.Exit("DIY.TryFlushPrefs");

        // T3（2026-10-12）：组拖"事件丢失"对账（已武装 + 鼠标抬起 N 帧却没等到 EndGroupDrag）。
        //   未武装时方法内第一行就返回，常态零开销；内部全 try/catch，异常不外溢。
        //   ★ 2026-10-13：随 T-A5 收进 #if DIAG（方法体同门控，Release 里整体消失）。
#if DIAG
        ReconcileGroupDragArm();
#endif

        // S8m：原此处每帧调 SkipWageUpgradeRestore.TickDiagnostics()（F11）+
        //       SkipXiaowoBayRestore.TickDiagnostics()（F12），已随诊断台账一并移除
        //       （S8l 实机验证通过，台账属排障期脚手架）。恢复方法见两个文件内的注释块。
        // 基本运行日志仍保留：Install 时的「Wage 避让：N/N 成功」、
        // 虚空珠移交时的「已移交给 DIY：基准 20x10」、DiagLog 开关控制的详细诊断。
    }


    private static void TryFlushPrefs()
    {
        if (!_prefsDirty) return;
        // ★ T1（2026-10-12）：节流改单调钟。原 DateTime 差值在系统时钟回拨时会一直
        //   判成"距上次不到 2 秒" ⇒ **配置永不落盘**（静默）。
        if (!_prefsFlushGate.Due(2000)) return;
        // 审查 #4：必须 Save 成功之后再清 dirty。原写法先清标志再 Save，
        // 一旦 Save 抛异常，本次写盘就永久丢失且不会再重试。
        try
        {
            PlayerPrefs.Save();
            _prefsDirty = false;
        }
        catch (System.Exception ex)
        {
            // 故意保留 _prefsDirty = true：下一帧（或 OnApplicationQuit）再试一次。
            CatchLogOnce.Warn(ex);
        }
    }

    public static void Shutdown()
    {
        try { if (_prefsDirty) PlayerPrefs.Save(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        ContainerWindowResizer.Dispose();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 手动遍历 Assembly 里的 Patch 类，跳过 OptionalPatchNames 里的类。
    // 相比 PatchAll 的好处：可以按配置跳过可选功能的 Patch，做到"功能
    // 关闭 = Patch 完全不装"。
    // ═══════════════════════════════════════════════════════════════════════
    private static void InstallPatches(HarmonyLib.Harmony harmony)
    {
        int installed = 0;
        int skipped = 0;
        int failed = 0;
        try
        {
            Type[] types;
            try { types = typeof(DiyUpgradeMod).Assembly.GetTypes(); }
            catch (ReflectionTypeLoadException rtle)
            {
                types = Array.Empty<Type>();
                if (rtle.Types != null)
                {
                    int validCount = 0;
                    for (int j = 0; j < rtle.Types.Length; j++) if (rtle.Types[j] != null) validCount++;
                    Type[] validTypes = new Type[validCount];
                    int k = 0;
                    for (int j = 0; j < rtle.Types.Length; j++)
                        if (rtle.Types[j] != null) validTypes[k++] = rtle.Types[j];
                    types = validTypes;
                }
            }
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null) continue;
                try
                {
                    // 只处理带 [HarmonyPatch] attribute 的类
                    if (type.GetCustomAttributes(typeof(HarmonyPatch), false).Length == 0) continue;

                    // 【2026-10-06 合并改造】命名空间过滤：4 个 DLL 合并成 1 个之后
                    //   typeof(DiyUpgradeMod).Assembly 就等于整个套件，会把
                    //   SaveBagExpand.* / CustomInfrastructure.* 的补丁类也装到
                    //   DIYUpgrade 这个 Harmony ID 下 → 同一目标被装两遍。
                    //   合并前 Assembly 只含 DIYUpgrade 命名空间，这里补回来等价范围。
                    if (type.Namespace == null || !type.Namespace.StartsWith("DIYUpgrade", StringComparison.Ordinal))
                        continue;

                    // 跳过被配置禁用的可选 Patch
                    if (OptionalPatchNames.Count > 0 && OptionalPatchNames.Contains(type.Name))
                    {
                        skipped++;
                        continue;
                    }

                    harmony.CreateClassProcessor(type).Patch();
                    installed++;
                }
                catch (Exception ex)
                {
                    failed++;
                    if (_log != null) _log.Error("[容器升级] Patch 安装失败 " + (type?.Name ?? "?") + ": " + ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            if (_log != null) _log.Error("[容器升级] InstallPatches 异常: " + ex.Message);
        }
        if (_log != null)
        {
            _log.Msg($"[容器升级] Patch 安装: 成功 {installed} 个"
                    + ((skipped > 0) ? $", 跳过 {skipped} 个(可选功能关闭)" : "")
                    + ((failed > 0) ? $", 失败 {failed} 个(详见上方错误)" : ""));
            ContainerSuite.Shared.ContainerSuiteLog.Mark("diy.patch", installed >= MinInstalledPatchCount && failed == 0,
                "installed=" + installed + " skipped=" + skipped + " failed=" + failed);
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 2026-09-29：改为接收所有本 mod 的 Harmony 实例数组，合并去重统计。
    // 原写法只统计主实例，会遗漏其它三个子模块的补丁数。
    // ═══════════════════════════════════════════════════════════════════════
    private static void ReportPatchSummary(HarmonyLib.Harmony[] instances)
    {
        try
        {
            HashSet<MethodBase> seen = new HashSet<MethodBase>();
            if (instances != null)
            {
                for (int i = 0; i < instances.Length; i++)
                {
                    HarmonyLib.Harmony h = instances[i];
                    if (h == null) continue;
                    try
                    {
                        foreach (MethodBase m in h.GetPatchedMethods())
                            if (m != null) seen.Add(m);
                    }
                    // 单个 Harmony 实例查询失败不影响整体统计，跳过即可。
                    catch { }
                }
            }
            int patchedCount = seen.Count;
            // 只打数量。"/期望值"随版本/新增 patch 变动，容易误导 —— 之前已经踩过两次。
            // A3（2026-10-07）：阈值统一到 MinExpectedPatchCount。
            //   此前人类日志用 14、[REG] 断言用 20 —— patchedCount∈[14,19] 时会
            //   "日志说✓、[REG] 报 FAIL"自相矛盾。取 14：它是历史上真正踩到
            //   "游戏更新导致方法未匹配"时校验过的下限（真实值 31，余量充足）。
            if (patchedCount < MinExpectedPatchCount)
            {
                if (_log != null) _log.Warning($"Patch 汇总：{patchedCount} 个方法已挂补丁（低于预期 {MinExpectedPatchCount}，可能因游戏更新有方法未匹配）");
            }
            else
            {
                if (_log != null) _log.Msg($"Patch 汇总：{patchedCount} 个方法已挂补丁 ✓");
            }
            ContainerSuite.Shared.ContainerSuiteLog.Mark("diy.patch.summary", patchedCount >= MinExpectedPatchCount, "count=" + patchedCount);
        }
        catch (Exception ex) { if (_log != null) _log.Warning("汇总 patch 结果失败：" + ex.Message); }
    }
}