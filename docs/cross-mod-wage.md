# 避让蛙哥妙妙箱（SkipWageUpgradeRestore）——取证与决策记录

> 本文件内容由 `ContainerSuite.DIYUpgrade/SkipWageUpgradeRestore.cs` 类 `<summary>` 注释逐字搬迁（2026-10-13，T-C4）。
> 源码保留摘要与指针；被 patch 方法的精确签名清单等代码级契约仍以源码为准。

拦截蛙哥（WagePerks）对 DIY 已接管容器的干扰逻辑。
使用 AccessTools.TypeByName 反射获取蛙哥类型，动态 Patch。

═══════════════════════════════════════════════════════════════════════
【2026-10-02 S8i】补齐 7 个漏掉的入口 + 判据与 XIAOWO 侧统一
═══════════════════════════════════════════════════════════════════════
上一版只拦了 7 个方法里的"善后层"（恢复形状 / 消耗材料 / tooltip），
漏掉了全部 7 个 **Target 家族入口**。后果：玩家拖着材料对着 DIY 箱子时，
蛙哥的入口 Prefix 照样能跑：
  · ContainerUpgradeV2.PrefixMayTarget_WageBox         → MayTarget 返回 true
  · ContainerUpgradeV2.PrefixCanTarget_WageBox         → CanTarget 返回 true
  · ContainerUpgradeV2.PrefixMayHaveValidInventorySlot_WageBox → 直接吃材料升级
  · LuckScoutBackpackUpgrade.PrefixMayTarget/CanTarget/Target
  · LuckScoutBackpackUpgrade.PrefixMayHaveValidInventorySlot
只拦善后层 = 拦不住"扣材料 + 改尺寸"这个动作本身。

★ 2026-10-12（C-1 复核结论）：上面那段里的「7 个入口层补丁挂钩」已**整段删除**。
  复核确认它们的 Prefix 一律 return true（无副作用空操作）—— 删掉与原方法直通
  完全等价，只是省掉 7 次 Harmony 调度与 7 个预热名额。上面这段历史描述保留
  （记录「为什么曾经挂过」），但**不要再拿它核对当前行为**。

【判据说明 —— 2026-10-09 复核】
  入口层不挂补丁（有意放行）：2026-10-09 复核确认那 7 个入口 Prefix
  （MayTarget / CanTarget / MayHaveValidInventorySlot 家族）是无副作用空操作
  （原 `if(!Owns…) return true; return true;` 判定结果被丢弃），
  2026-10-12（C-1）删除补丁挂钩本身 —— 行为等价：我们的 Prefix 一律 return true
  ⇒ 原方法本来就在跑；删掉后原方法照样跑，只是省掉 7 次 Harmony 调度与预热名额。
  将来需要入口层判据时按签名加回，并同步更新 readme.用户须知.md 的避让计数。
  原私有判据 OwnsWageBox / OwnsVoidBead 仅被这些空操作调用，已于 2026-10-09 删除。
  真正拦蛙哥升级的是善后层：Prefix_WageBox_Target / RestoreWageBoxShape /
  ConsumeNutsInBox / PostfixWageBoxTooltip / ConsumeNutsDirectly /
  Prefix_DoUpgrade（判据 = IsWageBox / IsVoidBeadStorage + HasDiyMigrated，
  **不是** GetWageCategory —— 详见下条 2026-09-29 说明）。

2026-09-29：所有 Prefix 增加 WorldReadyGate.ShouldDefer 检查。
  加载期直接放行给蛙哥 —— 与 GetWageCategory 的「去隐式接管」配合，
  让旧档首次加载时蛙哥链完整跑完，然后 DIY 才通过 ReapplyFromMeta 里
  的显式迁移分支接管。