# 拖边改尺寸（DIY Resizer）—— 手柄 / 性能 / 门禁 / 上限沿革

> 本文件内容由源码注释逐字搬迁（2026-10-13，注释精简 · 批次1）。源码保留摘要与就地判据。
> 来源：`DiyUpgradeMod.Resizer.cs` / `DiyUpgradeMod.Resizer.Cursor.cs` / `DiyUpgradeMod.GridWindow.cs`。
> 就地保留（不搬）：`[JitWarmup]` 标记、`WorldReadyGate.ShouldDefer` 门禁判据。

## 手柄数量与 Top 边手柄（`DiyUpgradeMod.Resizer.cs`）

```
        // 2026-10-10：手柄数量改为命名常量（原散写 7）。
        // ★ 新增第 8 个 Top 边手柄：小窝（XIAOWO）的手柄组有 8 个、含独立 Top 边条，
        //   我们原来只有 7 个（缺 Top）—— 玩家"从上压"抓到的是小窝的 Top 手柄，
        //   走小窝的缩放链路，本 mod 的网格清理（HideOutOfBoundsNodes / 收尾强制重建）
        //   全都不在链上 ⇒ 从上压必然残留。补齐 Top 手柄让该方向回到本 mod 链路。
        //   几何与 XIAOWO 的 Top 手柄一致（横贯顶边、宽 -32 给两角让位）。
```

## T1（2026-10-12）：手柄自检改单调钟（`DiyUpgradeMod.Resizer.Cursor.cs`）

```
        // T1（2026-10-12）：改单调钟节流 —— 原 DateTime 差值在时钟回拨时会一直判成
        //   "距上次不到 0.5 秒" ⇒ 手柄自检（LiveHandleCount 掉线就重建）**再也不跑**。
```

## S8r（2026-10-05）性能优化：DIY tick 常驻开销 60~80μs → <15μs（`DiyUpgradeMod.Resizer.Cursor.cs`）

```
        // ═══════════════════════════════════════════════════════════════
        // S8r（2026-10-05）性能优化：DIY tick 常驻开销从 60~80μs 降到 <15μs。
        //
        // 背景：TD-09 性能基线显示，玩家静止不动时 DIY tick 依然消耗
        //       60~80μs / 帧（占 60fps 帧预算 0.4~0.5%），主因是每帧对
        //       所有可见 ResizeState 跑 raycast + overlay 同步。
        //
        // 判据：位置 / 左键 / 焦点 三者都没变 && 无正在进行拖拽 && 上一帧无 hover。
        //       任一条不成立 → 走完整 tick（正常逻辑）。
        //       全部成立 → 跳过整帧，但每 ForceTickEvery 帧强制跑一次完整 tick。
        //
        // 为什么每 6 帧要强制跑：ResizeState.Tick 里的 TryRestoreOnceOnVisible
        //       需要 _visibleSince 被记录（窗口可见后 0.05s 才触发恢复）。
        //       若鼠标永远不动、窗口永远"刚可见但还没记录过时间"，恢复就永不触发。
        //       每 6 帧兜底一次（≈0.1s @ 60fps），恢复延迟最多 0.1s，玩家无感。
        //
        // 为什么要求「上一帧无 hover」：鼠标静置期间窗口可能被外部关闭/隐藏，
        //       若直接跳过，光标会停留在 hover 态（视觉残留）。要求上一帧无 hover
        //       让「鼠标第一次移出手柄」这一帧一定被计算到。
        // ═══════════════════════════════════════════════════════════════
```

## 加载期门禁：读档/场景切换完全跳过拖拽手柄 tick（`DiyUpgradeMod.Resizer.Cursor.cs`）

```
            // ═══════════════════════════════════════════════════════════════════════
            // 2026-09-29：门禁关闭时（读档 / 场景切换加载期）完全跳过拖拽手柄 tick。
            //
            // 三个原因：
            //   1. 加载期 State 里的 Window 引用可能已失效（场景卸载），对它做
            //      IsVisible() / RectTransformUtility 判定会触发无效 native 访问。
            //   2. ResizeState.Tick 内的 TryRestoreRememberedGridShape 会与
            //      XIAOWO / Wage 的窗口恢复链竞争同一个 GameGridInventory.SetShape，
            //      这正是 WorldReadyGate 要避免的。
            //   3. 加载期玩家不可能拖拽，全部 interop 调用都是浪费。
            //
            // 由 OnSceneWasLoaded 里的 WorldReadyGate.Invalidate() 关门，
            // 由 LoadGameRestorePatch.DelayedRestore 的 WorldReadyGate.MarkReady() 开门。
            // 门禁打开后下一帧，本方法自然恢复全部逻辑。
            // ═══════════════════════════════════════════════════════════════════════
```

## S8n（2026-10-02）上限与 XIAOWO 对齐 —— 已于 2026-10-04 废弃（`DiyUpgradeMod.GridWindow.cs`）

```
    // ═══════════════════════════════════════════════════════════════════
    //  S8n（2026-10-02）：上限与 XIAOWO 对齐 —— **已于 2026-10-04 废弃**
    // ═══════════════════════════════════════════════════════════════════
    //  原设计：把 DIY 的拖拽上限夹到 XIAOWO 的当前解锁形状，避免两个 mod
    //          各自 clamp 到自己的上限、拖到边界时互相打架（手感抖动）。
    //
    //  废弃原因：
    //    · XIAOWO 的拖拽手柄已被本 mod 避让拦掉：
    //        Prefix_Attach 阻止新建手柄
    //        SweepZombieHandles 清理历史残留
    //      同一个容器上只剩 DIY 一套手柄，不会打架 → 对齐不再必要。
    //    · 保留对齐会造成两个反向 bug：
    //        ① 恢复路径（读档 / Show）经过 TryGetGridShapeLimits 时被夹，
    //           已升级到 10x11 的 smuggler_bay 被夹回 XIAOWO 满级 10x10；
    //        ② 拖拽路径加了 "不缩小已有尺寸" 保护后，一旦形状到了 XIAOWO
    //           上限（如 10x10），上限被夹死，之后拖拽永远出不去。
    //
    //  现状：ClampToXiaowoCeiling 函数、三个反射字段、调用点全部删除。
    //        DIY 拖拽上限只受自己的 cfg（BoxMaxW/MaxH 等）约束。
    //        如将来发现 S8n 的抖动真的回来了，可参考 git 历史还原。
    // ═══════════════════════════════════════════════════════════════════
```
