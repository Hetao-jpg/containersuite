using System;
using System.Reflection;
using ContainerSuite.Shared;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
// S8m：System.Collections.Generic（Queue）与 UnityEngine（KeyCode/Input）随 F11
//       诊断台账一并移除。需要恢复台账时按文件内注释块补回这两个 using。

namespace DIYUpgrade;

/// <summary>
/// 拦截蛙哥（WagePerks）对 DIY 已接管容器的干扰逻辑。用反射动态 Patch。
/// 完整背景（S8i 入口层历史 / C-1 删除入口层挂钩的复核结论 / 判据说明 / 加载期放行）：
///   docs/cross-mod-wage.md
/// </summary>
internal static class SkipWageUpgradeRestore
{
    private static bool _installed;

    // ═══════════════════════════════════════════════════════════════════
    //  被 patch 方法的精确参数签名（AccessTools.Method 精确匹配，漏一个就找不到）
    //  来源：ilspycmd --ilcode 读 Mods/WagesPerks.dll 的真实 IL 头，2026-10-02
    // ═══════════════════════════════════════════════════════════════════
    /// <summary>无 ref 的两 GameItem 参数（PrefixTarget_WageBox）。</summary>
    private static readonly Type[] PlainArgs = { typeof(GameItem), typeof(GameItem) };

    // ★ C-1（2026-10-12）：原 RefBoolArgs 字段（带 ref bool 的三参数签名）已删除 ——
    //   它只被那 7 个入口层 Patch 调用使用，随之一并移除。
    //   PlainArgs（两个 GameItem）保留：善后层 PrefixTarget_WageBox 的签名是
    //   (GameItem, GameItem)，无 ref，仍在用。
    //   若将来加回入口层补丁，按 ilspycmd --ilcode 从 WagesPerks.dll 读出的签名重建
    //   （妙妙箱 3 个入口 + 虚空珠 3 个入口都带 ref bool，PrefixTarget_Bead 不带）。

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed) return;
        _installed = true;

        // 【2026-09-27 P0-3】走 FindTypeCached，避免启动时反复扫全程序集
        Type typeUpgrade = TypeCache.FindTypeCached("WagePerks.ContainerUpgradeV2");
        Type typeLuckScout = TypeCache.FindTypeCached("WagePerks.LuckScoutBackpackUpgrade");

        int patched = 0, failed = 0;

        if (typeUpgrade != null)
        {
            // ── 【S8i】Target 家族入口 —— ★ 2026-10-12（C-1）已整段删除 ──────────
            //   历史：S8i 曾在这里 patch 蛙哥自己的 3 个入口 Prefix
            //     PrefixMayTarget_WageBox / PrefixCanTarget_WageBox /
            //     PrefixMayHaveValidInventorySlot_WageBox（签名均带 ref bool），
            //   但我们的 Prefix 一律 return true（无副作用空操作），挂着只是让
            //   Harmony 多跑一次调度。2026-10-12 删除，行为与原方法直通完全等价。
            //   ★★ 若将来要加回：参数必须带 ref bool —— AccessTools.Method 参数匹配
            //      是精确的，漏掉第三个参数就找不到方法（2026-10-02 实测：6 个补丁全
            //      挂不上、日志刷"找不到 ... 跳过"，只有恰好无 ref 的 PrefixTarget_WageBox
            //      挂上了）。签名经 ilspycmd --ilcode 从 WagesPerks.dll 读出，
            //      加回时请按同一来源重建，不要凭方法名猜。

            // ── 原有的善后层拦截 ──
            if (Patch(harmony, typeUpgrade, "PrefixTarget_WageBox", PlainArgs,
                nameof(Prefix_WageBox_Target))) patched++;
            else failed++;

            if (Patch(harmony, typeUpgrade, "RestoreWageBoxShape",
                new[] { typeof(GameItem) },
                nameof(Prefix_RestoreWageBoxShape))) patched++;
            else failed++;

            if (Patch(harmony, typeUpgrade, "ConsumeNutsInBox",
                new[] { typeof(GameItem) },
                nameof(Prefix_ConsumeNutsInBox))) patched++;
            else failed++;

            if (Patch(harmony, typeUpgrade, "PostfixWageBoxTooltip",
                new[] { typeof(RichTextBuilder), typeof(GameItem) },
                nameof(Prefix_PostfixWageBoxTooltip))) patched++;
            else failed++;

            // 【新增】拦截 1.3.0 批量升级中的妙妙箱直接消耗逻辑
            if (Patch(harmony, typeUpgrade, "ConsumeNutsDirectly",
                new[] { typeof(GameItem), typeof(GameItem) },
                nameof(Prefix_ConsumeNutsDirectly))) patched++;
            else failed++;
        }
        else
        {
            MelonLogger.Msg("未检测到 WagePerks.ContainerUpgradeV2，跳过妙妙箱避让");
        }

        if (typeLuckScout != null)
        {
            // ── 【S8i】虚空珠的 4 个入口 —— ★ 2026-10-12（C-1）已整段删除 ──────
            //   历史：PrefixMayTarget / PrefixCanTarget / PrefixMayHaveValidInventorySlot
            //   （带 ref bool）与 PrefixTarget（无 ref，用 PlainArgs）—— 同样一律
            //   return true（无副作用空操作），删掉后蛙哥原方法照常执行，行为等价。
            //   ★ 加回时按 ilspycmd --ilcode 从 WagesPerks.dll 读出的签名重建。

            // ── 原有的虚空珠拦截 ──
            if (Patch(harmony, typeLuckScout, "ApplyLockedShape",
                new[] { typeof(GameGridInventory), typeof(int) },
                nameof(Prefix_ApplyLockedShape))) patched++;
            else failed++;

            // 【新增】拦截 1.3.0 批量升级中的虚空珠直接升级逻辑
            // ★★ 2026-10-06 实机发现这条**从来没挂上过**：
            //   用 ilsdump 读当前 Mods\WagePerks.dll，真实签名是
            //   DoUpgrade(GameItem, GameItem, Boolean) -> Boolean，
            //   而我们按 (GameItem, GameItem) 打 —— AccessTools 精确匹配不到。
            //   改成候选签名依次试：新版 3 参在前，旧版 2 参兜底。
            // ★ 2026-10-12（C-1）：当时的日志表现是 "Wage 避让：13/14 成功"。
            //   入口层 7 个挂钩删除后总数回到 7，"13/14" 与旧注释里的
            //   "README 有 5 处写 14/14" 都已过时（那是 _archive 旧 README 的情况，
            //   当前 README.md.txt 里没有 14/14；需要同步的只有
            //   readme.用户须知.md 的示例日志，已改为 7/7）。
            if (PatchAny(harmony, typeLuckScout, "DoUpgrade", nameof(Prefix_DoUpgrade),
                new[]
                {
                    new[] { typeof(GameItem), typeof(GameItem), typeof(bool) },
                    new[] { typeof(GameItem), typeof(GameItem) },
                })) patched++;
            else failed++;
        }
        else
        {
            MelonLogger.Msg("未检测到 WagePerks.LuckScoutBackpackUpgrade，跳过虚空珠避让");
        }

        int totalWage = patched + failed;
        if (totalWage > 0)
            MelonLogger.Msg($"Wage 避让：{patched}/{totalWage} 成功" +
                (failed > 0 ? " ← 有方法没挂上，蛙哥可能仍能抢 DIY 容器；" +
                    "多半是 WagePerks 改了方法名/签名，请更新容器套件" : ""));
        ContainerSuite.Shared.ContainerSuiteLog.Mark("wage.avoid",
            totalWage > 0 ? (failed == 0 ? "PASS" : "FAIL") : "NA",
            totalWage > 0 ? (patched + "/" + totalWage) : "WagePerks not installed");
    }

    private static bool Patch(HarmonyLib.Harmony harmony, Type type, string methodName,
        Type[] parameters, string prefixName)
    {
        try
        {
            MethodInfo target = AccessTools.Method(type, methodName, parameters);
            if (target == null)
            {
                MelonLogger.Warning($"找不到 {type.Name}.{methodName}，跳过");
                return false;
            }
            MethodInfo prefix = typeof(SkipWageUpgradeRestore).GetMethod(prefixName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prefix == null)
            {
                MelonLogger.Warning($"找不到 Prefix {prefixName}");
                return false;
            }
            harmony.Patch(target, new HarmonyMethod(prefix), null, null, null, null);
            // 【2026-10-10 方案 A】手动注册 → 登记进预热表（见 WarmupRegistry 注释）。
            ContainerSuite.Shared.WarmupRegistry.Register(prefix);
            return true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"避让 {type.Name}.{methodName} 失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 同名方法按**多个候选签名**依次试，命中一个即返回 true。
    ///
    /// 为什么需要它：<see cref="Patch"/> 的 AccessTools.Method 是精确参数匹配，
    /// 对方 mod 一改签名（WagePerks 给 DoUpgrade 多加了个 bool），整条拦截就
    /// **静默失效** —— 只在日志里留一行 "13/14"，没人看就以为一直正常。
    /// 2026-10-06 实机日志确认过一次，故补此入口。
    ///
    /// 不复用 Patch：Patch 找不到会先刷一条 Warning，候选链里第一个签名片段
    /// 不匹配是正常流程，用 Patch 会误报。
    /// </summary>
    private static bool PatchAny(HarmonyLib.Harmony harmony, Type type, string methodName,
        string prefixName, Type[][] candidates)
    {
        try
        {
            MethodInfo prefix = typeof(SkipWageUpgradeRestore).GetMethod(prefixName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prefix == null)
            {
                MelonLogger.Warning($"找不到 Prefix {prefixName}");
                return false;
            }

            MethodInfo target = null;
            string tried = null;
            foreach (Type[] ps in candidates)
            {
                target = AccessTools.Method(type, methodName, ps);
                if (target != null) break;
                tried = (tried == null ? "" : tried + ", ") +
                        "(" + string.Join(",", Array.ConvertAll(ps, t => t.Name + (t.IsByRef ? "&" : ""))) + ")";
            }

            if (target == null)
            {
                MelonLogger.Warning($"找不到 {type.Name}.{methodName}（已试签名 {tried}），跳过");
                return false;
            }

            // 位置绑定校验：Prefix 的 __0/__1… 是**按位置**注入的，必须与目标方法前几个
            // 参数类型一致。候选签名是我们显式列出的（AccessTools 精确匹配，不会挂错重载），
            // 但若将来新增的候选项把参数顺序改掉（如 (bool, GameItem, GameItem)），Prefix
            // 会静默绑错 —— 这里显式校验，不匹配则告警并跳过。
            if (!LeadingArgsMatch(prefix, target))
            {
                MelonLogger.Warning($"避让 {type.Name}.{methodName}：已匹配到方法，但其前导参数与 " +
                    $"Prefix {prefixName} 位置不匹配，跳过（对方可能改了签名）。");
                return false;
            }

            harmony.Patch(target, new HarmonyMethod(prefix), null, null, null, null);
            // 【2026-10-10 方案 A】手动注册 → 登记进预热表（见 WarmupRegistry 注释）。
            ContainerSuite.Shared.WarmupRegistry.Register(prefix);
            return true;
        }
        catch (Exception ex)
        {
            MelonLogger.Warning($"避让 {type.Name}.{methodName} 失败：{ex.Message}");
            return false;
        }
    }

    /// <summary>
    /// 校验 Prefix 的前导位置参数（命名为 __0 / __1 … 的参数）与目标方法的对应位置
    /// 类型一致。只校验连续编号的位置参数；遇到其它参数（如 __result）即停止。
    /// </summary>
    private static bool LeadingArgsMatch(MethodInfo prefix, MethodInfo target)
    {
        ParameterInfo[] pp = prefix.GetParameters();
        ParameterInfo[] tp = target.GetParameters();
        int idx = 0;
        foreach (ParameterInfo p in pp)
        {
            if (p.Name != "__" + idx) break;      // 只认 __0/__1… 形式的位置参数
            if (idx >= tp.Length) return false;
            Type want = p.ParameterType;
            if (want.IsByRef) want = want.GetElementType();
            if (tp[idx].ParameterType != want) return false;
            idx++;
        }
        return true;
    }

    // ============== 妙妙箱 / 虚空珠入口拦截（真直通，占位） ==============

    // ─────────────────────────────────────────────────────────────────
    // 【S8i 原为拦截；2026-10-09 复核改为「真直通」】
    //
    // 原实现形如 `if (!OwnsWageBox(...)) return true; return true;` —— 两个分支
    // 返回同一值，判定结果被直接丢弃，是纯空操作。故改为一行 return true，
    // 并去掉对应的判据函数 OwnsWageBox / OwnsVoidBead（仅被此处调用，已删除）。
    //
    // ★ 设计上必须放行、**不要改成 return false**：蛙哥这些方法都是"强制改判 +
    //   吞材料"（PrefixMayTarget → __result = true；PrefixMayHaveValidInventorySlot
    //   → 吞材料）。我们也去写 __result / 返回 false，会连带堵死「把材料正常放进
    //   容器」这种最普通的操作 —— 比"蛙哥抢升级"更糟。
    //   详细禁止事项见 README #13。真正拦住蛙哥升级的是善后层
    //   Prefix_WageBox_Target / Prefix_DoUpgrade 等。
    // ─────────────────────────────────────────────────────────────────

    // ★ 2026-10-12（C-1）：上面那一节列出的 7 个入口 Prefix **已全部删除** ——
    //   · 妙妙箱：PrefixMayTarget_WageBox / PrefixCanTarget_WageBox /
    //             PrefixMayHaveValidInventorySlot_WageBox
    //   · 虚空珠：PrefixMayTarget_Bead / PrefixCanTarget_Bead /
    //             PrefixMayHaveValidInventorySlot_Bead / PrefixTarget_Bead
    //   它们的实现体都是 `return true;`（一行空操作），挂着只多付一次 Harmony 调度；
    //   删除后蛙哥原方法照常执行 —— 行为等价，不是功能变更。
    //   ★ 上面「不要改成 return false」的警告**将来加回时仍然有效**：我们的入口
    //     Prefix 必须放行，否则会连带堵死「把材料正常放进容器」这条最普通的操作。
    //   ★ 上面提到的「README #13」在 README.md.txt 里并不存在（只存在于 _archive
    //     的旧 README），不要再去找它。

    // ═══════════════════════════════════════════════════════════════════
    //  （原判据 OwnsWageBox / OwnsVoidBead 已于 2026-10-09 删除：它们仅被上面
    //    7 个已改为「真直通」的入口 Prefix 调用，删除后无引用。）
    //  ★ 2026-10-12（C-1）：那 7 个入口 Prefix 本身也已删除（见上一段），
    //    故本段与上面提到的判据一起成为纯历史记录 —— 当前不存在任何入口层补丁。
    // ═══════════════════════════════════════════════════════════════════

    // F11 诊断台账（Note 逐条记录 + TickDiagnostics 打印）已于 S8m 移除：属排障期
    //   脚手架，每帧读键盘 + 字符串格式化是常驻开销。原实现全文与恢复方法见
    //   _archive\removed-code-20261010\F11F12-ledgers.md。

    public static bool Prefix_WageBox_Target(GameItem __0, GameItem __1)
    {
        if (WorldReadyGate.ShouldDefer) return true;   // 加载期不干涉
        if (RestoreArbiter.IsApplying) return false;   // 【新增】
        if (!DiyUpgradeMod.SkipWageBoxEnabled) return true;   // S8k
        GameItem box = null;
        if (WageContainerRegistry.IsWageBox(__1)) box = __1;
        else if (WageContainerRegistry.IsWageBox(__0)) box = __0;
        if (box == null) return true;
        if (!WageContainerRegistry.HasDiyMigrated(box)) return true;
        return false; // DIY 接管，跳过蛙哥升级逻辑
    }

    public static bool Prefix_RestoreWageBoxShape(GameItem __0)
    {
        if (__0 == null) return true;
        if (WorldReadyGate.ShouldDefer) return true;
        if (RestoreArbiter.IsApplying) return false;   // 【新增】
        if (!DiyUpgradeMod.SkipWageBoxEnabled) return true;   // S8k
        if (!WageContainerRegistry.IsWageBox(__0)) return true;
        if (!WageContainerRegistry.HasDiyMigrated(__0)) return true;
        return false;
    }

    public static bool Prefix_ConsumeNutsInBox(GameItem __0)
    {
        if (__0 == null) return true;
        if (WorldReadyGate.ShouldDefer) return true;
        if (RestoreArbiter.IsApplying) return false;   // 【新增】
        if (!DiyUpgradeMod.SkipWageBoxEnabled) return true;   // S8k
        if (!WageContainerRegistry.IsWageBox(__0)) return true;
        if (!WageContainerRegistry.HasDiyMigrated(__0)) return true;
        return false;
    }

    public static bool Prefix_PostfixWageBoxTooltip(RichTextBuilder __0, GameItem __1)
    {
        if (__1 == null) return true;
        if (WorldReadyGate.ShouldDefer) return true;
        if (RestoreArbiter.IsApplying) return false;   // 【新增】
        if (!DiyUpgradeMod.SkipWageBoxEnabled) return true;   // S8k
        if (!WageContainerRegistry.IsWageBox(__1)) return true;
        if (!WageContainerRegistry.HasDiyMigrated(__1)) return true;
        return false;
    }

    // 【新增】批量升级拦截：防止框选拖拽绕过 DIY 接管
    public static bool Prefix_ConsumeNutsDirectly(GameItem __0, GameItem __1)
    {
        if (WorldReadyGate.ShouldDefer) return true;
        if (RestoreArbiter.IsApplying) return false;   // 【新增】
        if (!DiyUpgradeMod.SkipWageBoxEnabled) return true;   // S8k
        // __0 = box, __1 = nuts
        GameItem box = null;
        if (WageContainerRegistry.IsWageBox(__0)) box = __0;
        else if (WageContainerRegistry.IsWageBox(__1)) box = __1;

        if (box == null) return true;
        if (!WageContainerRegistry.HasDiyMigrated(box)) return true;
        return false; // DIY 已接管，阻止蛙哥批量消耗
    }

    // ============== 虚空珠拦截（用指针集合，不依赖 GetParentItem） ==============

    public static bool Prefix_ApplyLockedShape(GameGridInventory __0, int __1)
    {
        // __1 = 目标形状 id，本拦截不需要它，保留以对应目标签名。
        if (__0 == null) return true;
        if (WorldReadyGate.ShouldDefer) return true;
        if (RestoreArbiter.IsApplying) return false;   // 【新增】
        try
        {
            IntPtr ptr = ((Il2CppObjectBase)__0).Pointer;
            if (WageContainerRegistry.IsDiyVoidBeadGrid(ptr)) return false;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        return true;
    }

    // 【新增】批量升级拦截：防止框选拖拽绕过 DIY 接管
    public static bool Prefix_DoUpgrade(GameItem __0, GameItem __1)
    {
        if (WorldReadyGate.ShouldDefer) return true;
        if (RestoreArbiter.IsApplying) return false;   // 【新增】
        // __0 = junk, __1 = bead
        GameItem bead = null;
        if (WageContainerRegistry.IsVoidBeadStorage(__1)) bead = __1;
        else if (WageContainerRegistry.IsVoidBeadStorage(__0)) bead = __0;

        if (bead == null) return true;
        // S8l：判据从 HasDiyMigrated 换成 VoidBeadHandedOver（单一真相）。
        // 两者不等价：HasDiyMigrated 只表示"基准记过"，可能出现在任何容器上；
        // 而"是否该拦蛙哥"真正要看的是**主权有没有移交**。
        // 用旧判据会导致一种坏情况：移交前的虚空珠若因任何原因带了
        // DIY_MIGRATED_TAG，蛙哥原版逐格解锁被无端掐死 —— 正是 S8l 要修的病。
        if (!DiyUpgradeMod.VoidBeadHandedOver(bead)) return true;
        return false; // DIY 已接管，阻止蛙哥批量消耗
    }

    // ============== 兜底：清空蛙哥静态恢复状态 ==============

    /// <summary>
    /// 把 LuckScoutBackpackUpgrade._restoreFramesLeft 归零，让恢复循环停止。
    /// DIY 升级虚空珠成功后调用，作为对 ApplyLockedShape 拦截的双保险。
    /// </summary>
    public static void ClearWageRestoreState()
    {
        try
        {
            // 【2026-09-27 P0-3】复用缓存：Install 阶段已经查过一次，这里直接命中
            Type type = TypeCache.FindTypeCached("WagePerks.LuckScoutBackpackUpgrade");
            if (type == null) return;
            FieldInfo field = AccessTools.Field(type, "_restoreFramesLeft");
            if (field != null) field.SetValue(null, 0);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
    }
}