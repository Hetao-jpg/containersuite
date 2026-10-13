# 「地面」窗口鼠标拖动（SaveBagExpandWindowDrag）——设计与取证记录

> 本文件内容由 `ContainerSuite.SaveBag/SaveBagExpandWindowDrag.cs` 文件头注释逐字搬迁（2026-10-13，T-C4）。
> 源码头部保留摘要与指针；代码里的就地契约注释（阈值/缓冲隔离理由）仍留在源码中。

══════════════════════════════════════════════════════════════════════════
SaveBagExpandWindowDrag —— 「地面」窗口鼠标拖动（2026-10-09 新增）

目标：让玩家能用鼠标拖动 PickupWindow 的「地面」窗口（EmporiumEntry.afterhourWindow）。

── 为什么是"鼠标拖动"而不是"按键移动" ──
  1) 游戏**自带**完整的 UI 拖动实现：
       · PixelWindow.OnDrag(PointerEventData)   RVA 0x7C74C0（TreeNodeRender 那层，走 EventSystem 射线）
       · UIDragHandler.OnDrag(PointerEventData) RVA 0x6E1240（通用 UI 拖动组件，语义最干净）
     其核心公式（反汇编核实）：
       anchoredPosition += (PointerEventData.delta / Canvas.scaleFactor)
     本文件**复刻这一个公式**（铁律 1：借鉴成功案例，不自创）。
  2) 地面窗口为什么游戏不让拖：PixelWindow.canMove 是 **readonly** 字段（偏移 0x18），
     构造时传了 false → 不进 dockedFloating、OnDrag 对它是空操作。改不了 readonly，
     所以只能在 mod 层用**同款公式**自己实现（不动 canMove、不碰 IL）。

── 安全设计（逐条对照工作区铁律）──
  ★ 不挂任何新原生方法：拖动纯在 SaveBagExpandCore.Tick() 里轮询 Input 完成，
    与现有 TryScroll / SaveBagPaging 完全同一范式。⇒ 不触碰 FBD #26 / R4（热访问器禁区），
    也不会有"挂 new virtual/无体方法 → AccessViolation 秒崩"的风险（无需挂点 = 无此风险）。
  ★ 不调被劫持的方法：本模块没有任何 Harmony 补丁，自然无"自伤递归"。
  ★ 危险路径只读裸字段：唯一对游戏对象的读取是 ItemMouseDragHandler.current.IsDraggingItem
    —— 公开 bool 属性、getter 有 native body、内部只读 isDragging(bool, 0x68) 一个字段，
    **完全不碰被拖动中的 GameItem**（MEMORY 铁律 10 的正面用法：用游戏自己的状态标志
    否定整条路径）。拖动物品时直接整条跳过，绝不与物品拖拽抢鼠标。
  ★ 不用"一次性锁"（铁律 8）：窗口/基准随场景重建，每次窗口重新可见都要能重新抓基准，
    故用"可见性变化 → 重置基准"的**事件式重置**，而不是 _tried 一次性锁。
  ★ 加载期不动作：由调用方（Tick）先过 WorldReadyGate.ShouldDefer（本模块不自己判，
    保持单一判据来源）。
  ★ 日志走过 Diag（受 DiagLog 总闸控制）：拖动不刷屏；成功/失败都有据可查。

── 位置语义（与 PickupWindowTest 一致，实机已验证）──
  · 偏移量 = anchoredPosition 相对「游戏原版停靠位」的差值，单位 = anchoredPosition 像素。
  · 原版停靠位 = 窗口 Show 后稳定下来的 anchoredPosition。本模块在窗口**刚变可见**时
    记录当前 anchoredPosition 当作基准（此时游戏已写完初始位置），
    之后每次把 anchoredPosition 设为「基准 + 持久化偏移」。
  · 偏移量持久化进 cfg（7G_GroundOffsetX/Y）—— 玩家选择"记住位置"。
  · 窗口由游戏 FixBounds() 钳制在屏幕内；本模块写位置后也调一次 FixBounds（若可用），
    避免把窗口拖到屏幕外拿不回来。
══════════════════════════════════════════════════════════════════════════