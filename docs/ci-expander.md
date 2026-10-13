# CI 容器扩容：垃圾桶 / 档案箱 / 磁带机（源码注释逐字搬迁）

> 本文件由源码注释逐字搬迁（2026-10-13，注释精简 · 批次1），只做了搬迁、未做任何改写。
> 抽取范围为源码注释行（以 // 或 /// 开头，含全角框线行）；代码行未收录。
> 行号对应搬迁时的源码版本，仅作定位用。

## 延迟扩展队列（ContainerExpander.cs L18-39）

```
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 P1】延迟扩展队列 —— 把 TryExpand 的实质工作搬离 Harmony trampoline
    //
    // 崩溃模型（十一轮实证定稿）：CLR fatal 0x80131506 发生在
    //   「嵌套 JIT」×「栈上有 detour trampoline」的交集处。
    //   GameItem.ValidateShapeState 是外部 mod（XIAOWO / ProbablyOptimized）挂载的
    //   **超热方法** ⇒ 我们的 Postfix 执行时，栈上必然躺着第三方 detour 的 trampoline。
    //   原实现 ValidateShapeStatePostfix 直接在 trampoline 上跑 TryExpand，
    //   而 TryExpand 的 callees（CollectGrids / Walk / SetShape /
    //   ForceRecomputeDynamicBackground / RefreshWindow）是**冷方法**、从未预热
    //   （CIReadLoadWarmup 默认关闭）⇒ 首次 JIT 恰好落在 trampoline 上 ⇒ 崩。
    //
    // 治本：Postfix 只做「入队」（指针去重 + 存活校验，全是已编译的托管代码，
    //   不触碰任何冷 IL2CPP 路径）；真正的 TryExpand 放到 Mod.OnUpdate 的
    //   **顶层帧**执行 —— 那时栈上没有任何第三方 trampoline，
    //   冷方法即便首次 JIT 也安全。
    //   ⇒ 与 RestoreArbiter「单写者 + 队列 + 每帧预算」同一套成熟范式，不另起炉灶。
    //
    // ★ 跨帧持有 IL2CPP 对象的唯一安全用法 = 照抄 RestoreArbiter 的存活校验
    //   （出队后比对 Pointer 是否漂移 + item.state 是否为空）。
    //   只靠 try/catch 挡不住 AccessViolation（进程直接猝死）。
    // ═══════════════════════════════════════════════════════════════════
```

## Walk 止损：物品节点不再下钻（ContainerExpander.cs L272-298）

```
            // ═══════════════════════════════════════════════════════════════
            // 【2026-10-11 修复】止损：遇到**物品**节点（GameItem）不再向下递归。
            //
            // 症状：未升级的容器放进垃圾箱后双击打开 → 被改成 12x12（垃圾箱 cfg 尺寸）。
            //   实测 Latest.log：`trashcan -> 12x12 (grids=2/2)`（正常应为 1/1）。
            //
            // 根因（反汇编 + 现场探针双重确认）：
            //   · GameGridInventory.get_children()（VA 0x18077DDF0）=
            //     `new List<GraphNodeStorage>(this[0x198])`，而 0x198 正是
            //     `private readonly List<GameItem> items` ——
            //     ⇒ **网格的 children 是"网格里装着的物品"**。
            //   · GameItem.get_children()（VA 0x18079DF70）=
            //     [contentWindow, examineWindow] —— 物品自己的两个窗口。
            //   于是原 Walk 链变成：
            //     垃圾箱窗口 → 垃圾箱网格 → 网格里的物品(storage_bay) → 它的窗口
            //        → **它自己的网格** ≠ 垃圾箱的网格！
            //   探针实测（父窗口 parentItems=[storage_bay]）与上述完全吻合。
            //
            // 为什么"物品节点停止递归"是正确的（而不是漏掉合法网格）：
            //   · 容器自身的网格挂在**根容器的窗口树**里（探针 d=0 PixelWindow →
            //     d=1 Grid），不经过任何 GameItem 节点。
            //   · 一个 GameItem 节点永远代表"另一个物品"，它的窗口/网格属于它自己，
            //     不是本次扩展的目标。继续下钻 = 改别人的尺寸 = 本 bug。
            //   · 与参考实现 GridFinder(FullMachineUpgrades) 的差别：那边是给"机器"
            //     扩容且网格里只放产出物（无容器），故不设止损也能用；CI 的垃圾箱
            //     可以装容器，必须止损。
            // ═══════════════════════════════════════════════════════════════
```

## CI 读档链启动期 JIT 预热（ContainerExpander.cs L352-366）

```
    // ═══════════════════════════════════════════════════════════════════
    // 【读档崩溃防护】CI 读档链启动期 JIT 预热。
    //
    // 读档链：PlayerStore.LoadGame → LoadGamePostfix → DelayedRestoreFromLoad
    //         → TryExpand（+ CollectGrids / RefreshWindow）。
    // 这些方法的 IL2CPP 热路径（SetShape / ForceRecomputeDynamicBackground /
    // FixBounds / Validate / Walk 递归 / TryCast）若在「读档深栈」上首次被 JIT
    // 编译，会触发 Internal CLR error 0x80131506（CLR fatal，托管 catch 抓不到）。
    //
    // 修复方式：等 WorldReadyGate 开门（容器已加载）后，用**真实容器**跑一遍
    // TryExpand —— 让上述热路径在浅栈上完成 JIT。
    // ★ 禁止传 null 预热：各方法首行 container==null 早退，传 null 只编译到早退前，
    //   热路径根本没被编译；旧写法正是这个无效预热 + 一堆 NRE 噪声的根源。
    // ★ 禁止 Mode B（PrepareMethod 预编译）—— R3 门禁零豁免（实测会自己崩）。
    // ═══════════════════════════════════════════════════════════════════
```

## WarmupJit 实现内注释（ContainerExpander.cs L373-438）

```
        // 【2026-10-10 ★★★ 读档闪退定位】默认关闭！
        //   本预热是五次读档崩溃里唯一被推迟到"读档后"执行的（SBE/DIY 的都在启动期
        //   跑完），且崩点稳定落在它 1.86 秒后（五次实测 1.815~1.911s）。
        //   它与其它预热哲学相反：用**真实容器真执行** TryExpand（改网格形状 + 重建 UI），
        //   而 SBE/DIY 一律用 null 安全入口。门刚开、场景仍在重建时执行这些操作
        //   风险极高（0x80131506，托管 catch 抓不到）。
        //   保留代码 + cfg 开关（z2_CIReadLoadWarmup，默认 false），便于复现排查。
        // 等世界就绪（容器已加载）再预热：用真实容器跑一遍才能真正 JIT 热路径。
        // 门没开时预热只会拿到 null 兜底、什么都没编译到（且旧写法传 null 会触发 NRE 噪声）。
        // ★ 2026-10-09：入口 / 每步打点改走 Diag（受 DiagLog 总闸控制）。
        //   「想知道崩在哪一步」属排障；日常不开闸则这串追踪完全静默。
        // Mode A 真实调用：用当前已加载的容器实际跑一遍 TryExpand，
        // 让 CollectGrids / Walk / SetShape / ForceRecomputeDynamicBackground /
        // FixBounds / Validate 等 IL2CPP 热路径在浅栈上完成 JIT。
        // 读档深栈上做这些事会触发 0x80131506（CLR fatal，托管 catch 抓不到）。
        // TryExpand 只扩不缩、且已达标即早退，对真实容器是幂等操作，无副作用。
            // 罕见：门已开但容器仍未就绪。退而求其次，确保辅助方法桩被 JIT
            // （它们对 null 已安全，不会抛）。真正的热路径仍依赖 WorldReadyGate 读档期关门。
        // ★ 保持无条件（2026-10-09）：与 [SBE-JIT] / [DIY-JIT] 的"预热完成"同理 ——
        //   这是「预热确实跑过、且用的是真实容器」这个结论本身，属于加载状态汇报。
```

## 读档期禁用 ValidateShapeState Postfix 说明（ContainerExpandPatch.cs L12-35）

```
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-09-28 崩溃修复】读档期禁用 ValidateShapeState Postfix
    //
    // 崩溃现象：加载旧存档 → Internal CLR error (0x80131506) 直接闪退。
    // 崩溃栈：PlayerStore.LoadGame → DecodeSaveItem → GameInventory.Expel
    //         → GameItem.ValidateShapeState → 本类 Postfix → TryExpand
    //
    // 根因：ValidateShapeState 在读档时会被原版对**每个物品**调用一次，
    // 此时调用栈已经很深（DecodeSaveItem 之下），而 TryExpand 会走
    // CollectGrids → Walk 递归 + SetShape 等**需要 JIT 现场编译**的路径。
    // Harmony detour 还在栈上时去 JIT 编译新方法，.NET 运行时的
    // CompileMethodHook 会直接抛内部错误 —— 这是 CLR 级 fatal，
    // 托管 try/catch 抓不到，MelonLoader 日志也留不下任何堆栈。
    //
    // 【2026-10-07 更正】原注释称「DIYUpgrade 也 Postfix 了同一个方法
    // （ReapplyFromMeta → TryRestoreRememberedGridShape），两个 Postfix 叠加」
    // —— 该说法在**合并后的当前代码里已不成立**：DIY 的 16 个补丁里没有
    // ValidateShapeState（它的恢复走 Show / LoadGame → RestoreArbiter 那条链）。
    // 更正留痕，避免后人按旧注释去找一个不存在的"第二个 Postfix"。
    //
    // 修复：读档期间（LoadGamePostfix 置位 → 延迟 30 帧）一律跳过。
    // 恢复动作本来就由 LoadGamePostfix 的延迟协程承担（第 3 重保险），
    // 所以跳过后功能不减 —— 只是把重活儿从「读档深栈」搬到「帧末」。
    // ═══════════════════════════════════════════════════════════════════
```

