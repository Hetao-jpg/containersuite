# 批量组拖升级（Group Drag Upgrade）——设计与决策记录

> 本文件内容由 `ContainerSuite.DIYUpgrade/DiyUpgradeMod.GroupDrag.cs` 文件头注释逐字搬迁（2026-10-13，T-C4）。
> 源码头部保留摘要与指针；代码里的就地契约注释（指针+uniqueId 双验、Prefix 快照契约）仍留在源码中。

阶段1（2026-10-11）：批量组拖升级。

═══════════════════════════════════════════════════════════════════════
功能：玩家按住升级键 → 框选多堆材料 → 拖到某个容器上松手
      → 对**同一个容器**连续喂多堆材料，一次拖拽升多级。

为什么不支持"一次拖拽升级多个容器"：
  · 落点歧义无法消解 —— 玩家框选一堆材料拖到一片容器区，到底升哪个？
    源 mod 用 4 层回退（悬停 → 最近单拖 → 材料所在容器 → 悬停库存）去猜，
    其中「材料当前所在容器」这条会把"从背包选料"误判成"要升背包"。
  · 收益为零 —— 每个容器升级用各自的材料/槽位，跨容器批量并不省操作。
  ⇒ 落点 = 唯一悬停容器；悬停落空 = 静默放弃。宁可"没生效再拖一次"，
    也不能"升错容器"。

═══════════════════════════════════════════════════════════════════════
★★ 为什么在 StartGroupDrag **Prefix** 快照，而不是 EndGroupDrag 时读：
  反汇编 EndGroupDrag（0x7DBF90）确认其调用序列为
    GetCommonHomeInventory → IsFormationValid → CommitPrecisePlacement
    → RestoreGhosts → ClearDropTarget → ClearSelection
  **ClearSelection() 在内部被调用** ⇒ EndGroupDrag 的 Postfix 跑时
  `selectedItems` 已经是空列表。Prefix 时它还在。
  （这也是为什么本补丁挂 Prefix 而不是 Postfix —— 与蛙哥/钥匙卡包
    的"Prefix 快照 + Postfix 执行"不同：它们挂的是别的目标方法。）

★★ 为什么结束才执行（而不是 StartGroupDrag 时）：
  组拖开始时玩家还没选好落点。必须在"松手那一刻"拿到最终 hoverItem。

★★ 与蛙哥 / 钥匙卡包的共存：
  已知 3 个 mod 挂 ItemMultiSelectHandler.EndGroupDrag（蛙哥 WagesPerks、
  小偷钥匙卡包、本 mod）。三家都采用"不动原方法逻辑"的模式 ⇒ Harmony
  链上互不干扰。容器主权由 WageContainerRegistry.GetWageCategory 统一裁决
  （妙妙箱满级才移交 / 虚空珠靠 HandOver tag），所以"蛙哥优先、DIY 后接管"
  不需要在本文件写任何额外判据 —— MatchCategory 的第一跳就是它。

★★ 分帧执行（用户决策）：
  不照抄源 mod 的同步 while 循环。改用协程每帧升一个，理由与本项目
  P1 治本手法一致：把"同一帧 N 次抽奖"摊成"N 帧各一次"，每帧都回到
  Mod.OnUpdate 顶层帧，脱离第三方 trampoline 上下文。
  每步**重新解析材料指针**，不跨帧持有 IL2CPP 对象（规避悬垂指针）。
═══════════════════════════════════════════════════════════════════════