# 避让精工扩舱（SkipXiaowoBayRestore）——取证与决策记录

> 本文件内容由 `ContainerSuite.DIYUpgrade/SkipXiaowoBayRestore.cs` 类 `<summary>` 注释逐字搬迁（2026-10-13，T-C4）。
> 源码保留摘要与指针；补丁清单/判据等代码级契约仍以源码为准。

拦截 XIAOWOTradePerks（精工扩舱）对 DIY 已接管容器的干扰。

═══════════════════════════════════════════════════════════════════════
【2026-10-02 S8i 重写】根因修复：Bay expansion rolled back
═══════════════════════════════════════════════════════════════════════
上一版（8/8 全装）仍然报 "Bay expansion rolled back: inventory contents
changed while resizing"。根因不是 Harmony 优先级，是**避让挂错了层**：

  XIAOWO.TryUpgradeBay (L1596-1605):
      list = CaptureItems(bayInventory);           ← 快照
      SetBayShapeState(...);
      if (!ApplyStoredShape(target) || !ItemsMatch(...))
          throw new InvalidOperationException("inventory contents changed while resizing");
                   ^^^^^^^^^^^^^^^^^^^^^^^^ 返回 false 就直接抛

  我们把 Prefix_Bool 挂在 ApplyStoredShape 上让它返回 false
  → XIAOWO 抛异常 → 回滚 + 报错。

也就是说：**内部实现层（ApplyStoredShape / TryRestoreRememberedGridShape）
根本不该拦**。拦了会把 XIAOWO 的返回值语义打坏，让它自己在内部抛错。
入口层（TryHandleIngot / CanTargetWithIngot）拦住，它根本进不到这里。

【本版方案：只在入口拦 + 判据唯一】
  入口层（2 个方法，拦了 5 个入口全部失效）：
    · BayExpansionPerk.TryHandleIngot     ← 2 个 Prefix 入口调它
        （L2647 MaterialDirectory.OnIngotTarget / L2679 GameItem.Target）
    · BayExpansionPerk.CanTargetWithIngot ← 3 个 Postfix 入口调它
        （L2636 MaterialDirectory.CanIngotTarget / L2655 GameItem.MayTarget
          / L2666 GameItem.CanTarget）
  恢复链（3 个方法，void 或不影响升级流程，拦了安全）：
    · BayExpansionPerk.RestoreExpandedItem
    · FridgeExpansion.Restore
    · WineRackExpansion.Restore
  窗口同步（1 个方法，L4353 UI 焦点回调调它，拦了安全）：
    · BayExpansionPerk.TryRestoreRememberedGridShape
    ※ 它只在 RestoreExpandedItem 内部和 UI 焦点回调里被调，
      升级流程 TryUpgradeBay **不调**它 → 拦它不会打坏 TryUpgradeBay。
  ✗ 不再拦：ApplyStoredShape（打坏 TryUpgradeBay 的返回值契约 → 报错根因）

【判据统一】全权优先 + 宽判据（核桃 2026-10-02 决策）：
  只要 DIY 的对应组开关开着且容器 ID 在列表里，尺寸主权就归 DIY，
  不等"已经升过级"（tag 证据）。XIAOWO 认的 6 个 bay 全部在 DIY 箱子组里，
  所以这 6 个容器的升级权全部归 DIY。

═══════════════════════════════════════════════════════════════════════
【2026-10-02 S8n 追加】手柄系统本体也要避让
═══════════════════════════════════════════════════════════════════════
上面 6 个补丁只封了「升级入口 + 恢复链」，漏了 XIAOWO 的**拖拽手柄
本体**。反编译取证（XIAOWOTradePerks.dll）：

  XIAOWOTradePerks.ContainerWindowResizer   ← 与本 mod 同名！
    · Attach(GameItem)  L5711
    · Tick()             L5763  OnUpdate 每帧调
    · 私有 States: Dictionary&lt;IntPtr, ResizeState&gt;
    · ResizeState 8 个手柄（边 8px / 角 16px）
        + 私有 BeginPointerDrag（L5032）

它的 Attach 只在 ApplyResizableWindow（L2214）内被调，而
ApplyResizableWindow 只在 TryUpgradeBay / RestoreExpandedItem /
TryUpgradeCage 里调 —— 这三条我们**恰好全拦了**。
于是产生两个后果：

  ① 僵尸手柄：历史上 XIAOWO 成功升级过 → 手柄挂上了 → 之后避让
     生效了，但 States 字典不会被清（只有 mod 卸载的 Dispose 才清）
     → 手柄永久留在窗口上。这就是核桃说的老问题，
       与当前避让开关状态**无关**。

  ② 双通道抢鼠标：XIAOWO 的 Tick 用 Input.GetMouseButtonDown 轮询
     + FindHandleAtPointer 命中就 BeginPointerDrag（L5811），
     **不依赖 Unity EventSystem 的 raycast**。哪怕事件被 DIY 的手柄
     吃掉，它照样开始拖。两套 Drag 同帧各跑一次
     TryResizeGridWindow，目标尺寸还各夹各的上限 → 手柄抖 / 形状跳格。

对策两条：
  · Prefix_Attach：Attach 根本不执行 → 不再产生新手柄（治 ② 的源头）
  · ZombieHandleSweeper：反射清 States 里 DIY 已接管容器的条目（治 ①）
判据复用同一个 ShouldSkipRestore，所以避让开关关掉时两条同时失效，
主权干净地交还 XIAOWO。