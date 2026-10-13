# JIT 预热体系（源码注释逐字搬迁）

> 本文件由源码注释逐字搬迁（2026-10-13，注释精简 · 批次1），只做了搬迁、未做任何改写。
> 抽取范围为源码注释行（以 // 或 /// 开头，含全角框线行）；代码行未收录。
> 行号对应搬迁时的源码版本，仅作定位用。

## 文件头说明（WarmupRegistry.cs L7-28）

```
/// <summary>
/// 【2026-10-10 方案 A】补丁方法预热登记表。
///
/// ═══════════════════════════════════════════════════════════════════════
/// 为什么需要它：`WarmupAllPatchMethods` 此前只遍历带 `[HarmonyPatch]`
/// **类特性**的补丁类。但本套件有 4 处补丁是**手动 `harmony.Patch(...)`
/// 注册**的、类上没有该特性：
///   · SkipXiaowoBayRestore  （7 个 Prefix，挂 XIAOWO 的方法）
///   · SkipWageUpgradeRestore（多条 Prefix，挂蛙哥的方法）
///   · ContainerExpandPatch  （2 个 Postfix）
///   · WindowShowHook        （所有 PixelWindow.Show 重载的 Postfix）
/// ⇒ 这些方法**永远不在预热清单里**，首次 JIT 只能等真实调用；
///   一旦真实调用发生在「MonoMod detour 还在栈上」的深栈上
///   （例：SweepZombieHandles 清手柄后 XIAOWO 重走 Attach 路径），
///   old MonoMod(2022) 的 DetourRuntimeNETCore30Platform.CompileMethodHook
///   会读空指针 → CLR fatal 0x80131506（托管 catch 抓不到，日志无堆栈）。
///
/// 本类 = 唯一的登记入口：**凡手动注册的补丁，注册成功时登记一次**。
/// 预热时连同 `[HarmonyPatch]` 枚举结果一起在浅栈 invoke。
/// 这样"注册了却忘了预热"在结构上不可能再发生（注册与预热同源）。
/// ═══════════════════════════════════════════════════════════════════
/// </summary>
```

## _declared 声明清单注释（WarmupRegistry.cs L33-36）

```
    // 【2026-10-10 P2-2】声明清单：记录 ScanAndRegister 实际扫到的 [JitWarmup]（"类型名.方法名"）。
    //   用途：供 DIY 侧做**覆盖率断言** —— MustWarmLeaves ⊆ 本清单。声明式改造后，
    //   "某个叶子被漏打特性" 是唯一的失效模式，而它不会有任何编译期症状；
    //   这里把扫到的东西记成机器可读的账，缺项即报错 + [REG.BP] 留痕。
```

## 声明式预热标记：契约段（WarmupRegistry.cs L42-64）

```
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 P2】声明式预热标记
    //
    // 背景：升级 apply 路径的 8 个「叶子方法」此前靠 WarmupSaveLoadChain 里
    //   8 行**手抄**的 Step(...) 调用来预热。硬编码清单的病灶：
    //     ① 新增叶子时**没有任何东西**提醒作者去补那行 Step —— 漏一个就静默
    //        退化成"在 trampoline 上首编译"，正是我们防的崩溃；
    //     ② 签名一改（加个 out 参数）那行就编译不过或语义漂移；
    //     ③ 清单离叶子定义很远，看代码的人无法确认"这个叶子到底预热了没"。
    //
    // 改法：标记就写在叶子方法自己头上（定义即注册），扫描器统一收集。
    //   ⇒ 与 WarmupRegistry 原本的「注册即登记」哲学一致：
    //     手动补丁由注册处登记，叶子由特性登记，两条路都在启动期浅栈 invoke。
    //
    // ★★ 标记契约（违反 = 启动期异常或静默失效）：
    //   ① 必须是 **static** 方法（扫描器只枚举 Static）；
    //   ② 必须是 **null 安全** 的：扫描器按 WarmupAll 的哑参规则调用
    //      （值类型传 default、引用类型传 null、out/ref 传元素类型默认值），
    //      所以方法必须在首参为 null 时**尽早 return**，不得解引用；
    //   ③ 必须是**叶子**：内部不得再调用我们自己的未预热方法，
    //      否则 invoke 时会嵌套 JIT 未编译方法 → 重入 CompileMethodHook
    //      → CLR fatal 0x80131506（10:37 铁律，大方法绝不进预热）。
    // ═══════════════════════════════════════════════════════════════════
```

## WarmupAll 方法说明（WarmupRegistry.cs L159-167）

```
    /// <summary>
    /// 用安全哑参把每个登记的方法各 invoke 一次，强制其在当前（浅）栈完成 JIT 编译。
    /// <para>
    /// 哑参规则与 <c>WarmupAllPatchMethods</c> 一致：值类型造默认实例、引用类型传 null、
    /// byref 传其元素类型的默认值。<c>WindowShowHook.ShowPostfix</c> 之类的
    /// **有副作用方法**由调用方在登记前自行排除，本类不替调用方判断。
    /// </para>
    /// </summary>
    /// <returns>成功 invoke 的方法数（含 invoke 抛异常但已达成编译的）。</returns>
```

## 预热时记录实际方法名（WarmupRegistry.cs L171-173）

```
        // 2026-10-10 P2：记录实际预热到的方法名。
        //   声明式扫描后"清单里到底有谁"不再能从源码一眼看出 ——
        //   本套件吃过「静默失效」的亏，这里留一条可核对的账（受 Diag 闸门控制）。
```

## JitWarmupAttribute XML 说明（WarmupRegistry.cs L221-228）

```
/// <summary>
/// 【2026-10-10 P2】声明式 JIT 预热标记：打在方法上 = 声明"启动期请用哑参把我编译掉"。
/// <para>
/// 由 <see cref="WarmupRegistry.ScanAndRegister"/> 在启动期扫描收集。
/// 完整契约（static / null 安全 / 必须是叶子）见 WarmupRegistry 中
/// 「声明式预热标记」段落的注释 —— 违反"叶子"约束会直接触发 CLR fatal。
/// </para>
/// </summary>
```

## WarmupJit 说明（SaveBagPaging.Warmup.cs L14-46）

```
    // ────────── S8u-3：Bind 链 JIT 预热 ──────────
    // S8u-3 一次性诊断标记：预热后若仍崩，控制台最后一行标记直接指认致死调用。
    /// <summary>
    /// S8u-3c（2026-10-06）：Bind 链 JIT 预热 —— 改用「真实调用」而非 PrepareMethod。
    ///
    /// 取证链：
    /// 二期（13:52 读档）：Bind 推迟到 DIY.RestoreReady+3.99s 仍崩在
    ///   ClearGrid → CompileMethodHook → 0x80131506；Snapshot（前驱）正常跑完
    ///   → 时间窗理论证伪，致命点 = 本会话 ClearGrid 链的首次方法编译。
    /// 三期启动（14:19/14:28）：PrepareMethod 预热自己也在启动时致死，
    ///   逐方法打点钉死在 `> SaveBagPaging.ClearGrid`。
    ///
    /// 拼合结论：两起崩溃同一凶手 —— **List&lt;GameItem&gt;.Add 的编译**：
    ///   ClearGrid 是本类唯一用到 List&lt;GameItem&gt;.Add（toExpel.Add）的方法；
    ///   13:52 = 运行时首调触发 JIT，14:28 = PREJIT 内联触发 —— 而 Mode B
    ///   （PrepareMethod/PREJIT）与运行期 Mode A 编译路径不同，13:52 已证明
    ///   ClearGrid 本体 Mode A 可正常编译执行。
    ///
    /// S8u-3c 做法（顺序即安全顺序）：
    ///   组3：集合冒烟 —— List&lt;GameItem&gt;.Add 排最前，Mode A 真实执行
    ///   组4：SaveBagPaging 全部 null 安全入口逐个真实调用（Mode A 首编，
    ///        与运行期同一编译模式；每步一行打点，崩则最后一行=凶手）
    ///
    /// S9x（2026-10-08）：按「编译模式」分治。
    ///   Mode A（真实执行）= 与运行期同路径 ⇒ 常开（组 3/4）；
    ///   Mode B（PrepareMethod）= 与运行期不同路径，已证可致 0x80131506
    ///   （FailFast，try/catch 抓不到）⇒ 组 1/2/5 及探针已全部删除，
    ///   R3 门禁据此零豁免。
    /// </summary>
```

## 入口/每步打点改走 Diag（SaveBagPaging.Warmup.cs L52-56）

```
        // 入口打点：否则无法区分"没跑"和"跑了但崩"（P3 回归判据也依赖这条进/出痕迹）。
        // ★ 2026-10-09：入口/每步打点改走 Diag（受 DiagLog 总闸控制）。
        //   原因：逐步追踪一次要打 ~28 行，是 Latest.log 的最大单一来源，而它只在
        //   「预热崩了、想知道崩在哪一步」时才有用 —— 那属于排障，不属于日常。
        //   ★ 但"预热完成"这一行仍是无条件（见末尾），因为 regress.ps1 拿它当启动哨兵。
```

## 只保留一个 Postfix + 拆成极薄外壳与真正体（WindowShowHook.cs L93-105）

```
    // 【2026-09-27 P0-6】只保留一个 Postfix —— 所有 Show 重载共用，不依赖参数
    //
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 方案 A】拆成"极薄外壳 + 真正体"：
    //   本 Postfix 挂在 **PixelWindow.Show 的所有重载**上（Show 有 ~72 个调用点，
    //   属超热路径）。此前它无 [HarmonyPatch] 类特性、是手动注册的 →
    //   WarmupAllPatchMethods 看不到它 → 首次 JIT 只能落在真实 Show 的 detour 深栈上
    //   → CompileMethodHook 崩（0x80131506）。
    //   但它**不能**直接拿去预热：原实现首行就 ScheduleDelayedRestore → MelonCoroutines.Start，
    //   用哑参（win=null）invoke 会……其实 win==null 已能早退，但保险起见把"起协程"下沉到
    //   独立方法 ScheduleDelayedRestore —— 外壳只做 null 判断 + 调用，预热时 win=null 即早退，
    //   零副作用，同时外壳本身被编译（detour 目标就是外壳，满足预热目的）。
    // ═══════════════════════════════════════════════════════════════════
```

## DelayedRestore 并入 CI 展示即扩展（WindowShowHook.cs L125-132）

```
        // ── 2026-10-07：并入 CI 过去单独挂的「展示即扩展」（补丁去重）──
        //   过去 CI 在 PixelWindow.Show 的两个重载上各挂一个 Postfix，
        //   与此处的 WindowShowHook 撞成一对 → 每次显示起 2 条协程
        //   （Show() 有 72 个调用点，属热路径）。现在统一由本协程完成：
        //   ① 先扩展全部 parentItems（CI 语义，forceRefresh: true）；
        //   ② 再请求恢复首个可扩容容器（DIY 语义）。
        //   顺序与历史一致（CI 的 Postfix 注册在前、先跑）。
        //   ContainerExpander 属 CI 命名空间，但同属一个已合并的 DLL，直接调。
```

## 启动期预热读档恢复链 JIT（DiyUpgradeMod.Restore.cs L387-416）

```
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-09 读档崩溃定位】启动期预热"读档恢复链"的 JIT。
    //
    // 为什么加：读档完成后约 0.3s 崩溃在
    //   MonoMod.RuntimeDetour.Platforms.DetourRuntimeNETCore30Platform.CompileMethodHook
    //   （CLR fatal 0x80131506，Windows 事件日志记为 coreclr.dll 0xc0000005）。
    //   SBE 侧有自己的 WarmupJit 且已实机验证有效（读档前 ~40s 在主菜单就把方法
    //   全部编译完），CI / DIY 两侧此前**没有**任何预热。
    //   本方法用与 SBE.WarmupJit 同构的 Mode A（真实调用）把 DIY 读档链在启动期
    //   （主菜单、浅栈、无 Harmony detour 在栈上）点着，读档期不再触发首次编译。
    //
    // 每步一行 `[DIY-JIT] > ...`：若某方法编译本身就崩，控制台**最后一行**即凶手。
    // ★ 2026-10-09：这些逐步打点已改走 Diag（受 DiagLog 总闸控制）——
    //   一次预热要打 ~22 行，是 Latest.log 的最大单一来源之一；
    //   「想知道崩在哪一步」是排障需求，不该常驻。排障时开 DiagLog 即可复原。
    //
    // ★ 只做 Mode A。Mode B（PrepareMethod 预编译）已在 SBE 侧被证明可致
    //   0x80131506，本项目 R3 门禁零豁免 —— 本文件禁止出现该 API。
    // ★ 【2026-10-09 修正】次数由 40 降为 8，**刻意不越过 tier1 阈值（约 30）**。
    //   原写法（40 次）本意是"把 tier1 重编译提前到启动期"，但 CoreCLR 的 tier1
    //   是**后台线程异步**编译的：启动期连调 40 次只是把一批方法**排队**，真正的
    //   tier1 编译仍由后台 JIT 线程在稍后（正好落在读档前后）执行。后台线程崩 =
    //   FailFast，主线程毫不知情地继续跑若干帧 —— 与实机现象（Fatal error 打印后
    //   日志仍续写 ~13ms、栈顶固定为 MonoMod…CompileMethodHook）完全吻合。
    //   改为低于阈值后：首次调用即完成 tier0 编译（足以消除"读档期首次编译"），
    //   且不再排队 tier1 重编译。对照组：SBE 的 WarmupJit 每方法只调 1 次、从不
    //   越过阈值，也是三者中唯一被实机验证"有效"的。
    // ★ 全部以 null 调用 + try/catch：这些方法首行基本都有 `if (item == null) return`；
    //   即便抛异常也已达成"编译"目标（JIT 在方法体执行前完成整段编译）。
    // ═══════════════════════════════════════════════════════════════════
```

## 预热所有补丁方法本体（DiyUpgradeMod.Restore.cs L420-449）

```
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-09 第二轮 / 2026-10-10 方案 A】预热「所有补丁方法本体」的 JIT。
    //
    // 根因（Resizer.cs:406 / WorldReadyGate.cs:71 注释同述）：被挂钩的方法体若在
    // 「MonoMod detour 还在栈上」时被首次 JIT 编译，old MonoMod(2022) 的
    // DetourRuntimeNETCore30Platform.CompileMethodHook 会读空指针 → CLR fatal
    // 0x80131506（托管 catch 抓不到，日志无堆栈）。
    //
    // 此前 WarmupSaveLoadChain 只热身了若干 helper，从未预热补丁方法本体 —— 它们
    // 首次编译落在玩家第一次交互（拖拽 / 悬停 / 读档）的 detour 深栈上，正是崩点。
    //
    // 做法：在启动期（Tick 首次调用，Melon 的 OnUpdate 栈，无游戏 detour 在栈上）
    // 用安全哑参把每个 Prefix/Postfix/Transpiler 各 invoke 一次，强制在浅栈完成编译；
    // 真实调用时方法已是编译态，不再触发 CompileMethodHook。
    //   · ShouldDefer 在启动期为 true → 绝大多数补丁首行即早退，无副作用；
    //     无守卫的补丁（如布局 getter）传 null 至多 NRE（托管异常，已 catch）。
    //   · 唯一跳过 LoadGameRestorePatch：它无 ShouldDefer 守卫，invoke 会误触
    //     Invalidate + 起协程，副作用过大。它的崩溃候选 DelayedRestore 协程由
    //     下方 WarmupSaveLoadChain 的 helper 链覆盖不到，但 LoadGame 路径本就晚于
    //     启动期；若仍崩在 LoadGame，再单独处理。
    //
    // ★★★ 【2026-10-10 方案 A 关键修复】原实现只遍历带 `[HarmonyPatch]` **类特性**
    //   的补丁类 ⇒ **4 处手动 `harmony.Patch(...)` 注册的补丁全部漏预热**：
    //     SkipXiaowoBayRestore(7) / SkipWageUpgradeRestore(多条) /
    //     ContainerExpandPatch(2) / WindowShowHook(所有 Show 重载)。
    //   崩溃实例：SweepZombieHandles 首次真清掉手柄后，XIAOWO 下帧重走被挂路径
    //   （Attach 等），我们挂在 XIAOWO 方法上的 Prefix 首次 JIT → CompileMethodHook 崩。
    //   修法：新增 Shared.WarmupRegistry —— 手动注册处"注册成功即登记"，
    //   这里**先预热 Registry 里的**，再跑 `[HarmonyPatch]` 枚举（两路互补、去重）。
    // ═══════════════════════════════════════════════════════════════════
```

## 预热三路调度与覆盖率断言（DiyUpgradeMod.Restore.cs L459-541）

```
            // ── 第零路（2026-10-10 P2）：声明式 [JitWarmup] 叶子 ──
            //   必须在 WarmupAll() **之前**扫描 —— 扫出来的方法要进同一张表，
            //   由下面那一次 WarmupAll() 统一在浅栈 invoke。
            //   取代原先 WarmupSaveLoadChain 里 8 行手抄的 Step(...) 硬编码清单：
            //   漏登记 = 该叶子退回"在 trampoline 上首编译" = 我们正在防的崩溃，
            //   而硬编码清单**没有任何机制**提醒作者补登记。
            // ── 第一路：手动注册的补丁（核心修复）──
            //   这些方法的类不带 [HarmonyPatch] 特性，下面的枚举永远看不到它们，
            //   必须由注册处自己登记（见 WarmupRegistry 类注释）。
            // ── 第二路：带 [HarmonyPatch] 类特性的补丁（原有逻辑）──
            // ── P2-2（2026-10-10）：声明式预热的覆盖率断言 ──
            //   声明式改造把"手抄 Step 清单"换成方法头上的 [JitWarmup]，代价是
            //   "某个叶子被漏打特性"不会有任何编译期症状（静默退化成在 trampoline 上首编译
            //   = 我们在防的 0x80131506）。这里用一份"必须预热"清单做集合差自检。
            // ★ 无条件打印（不再走 Diag 闸门）：否则"预热到底跑没跑、跑了几个"零线索
            //   —— 本套件吃过「静默失效」的亏，这条是崩溃防线，必须始终可见。
    // ═══════════════════════════════════════════════════════════════════════
    // P2-2（2026-10-10）：声明式预热的**覆盖率断言**
    //
    // 病：声明式 [JitWarmup] 把"漏登记"从"忘了抄一行 Step"变成了"忘了打一个特性"，
    //   而后者**编译期完全无症状** —— 结果是该叶子退回"在 trampoline 上首编译"，
    //   正是 0x80131506 的成因（且崩溃无堆栈、不可 catch）。
    //
    // 药：把"必须预热到的叶子"钉成一份**单一事实源清单**（MustWarmLeaves），
    //   启动期与实际扫描结果做集合差：
    //     · 缺项（清单有、扫描没有）→ Error + [REG.BP] name=jitwarmup.missing 留痕；
    //     · 多项（扫描有、清单没有）→ Msg 提示（新增叶子后请同步清单；不报错，
    //       否则每加一个合法叶子都要先改清单，噪音大于收益）。
    //   ⇒ 故意摘掉一个 [JitWarmup] 即触发：回归报告底部"断点留痕"出现缺失告警。
    //
    // 名字格式 = "类型名.方法名"（与 WarmupRegistry.RecordDeclared 一致）。
    //   ★ ContainerWindowResizer 是 DiyUpgradeMod 的**嵌套类**，其反射 Type.Name
    //     就是 "ContainerWindowResizer"（不是 "DiyUpgradeMod+ContainerWindowResizer"）。
    // ★ 新增升级链叶子时，除了在方法头打 [JitWarmup]，务必把它补进本清单。
    // ═══════════════════════════════════════════════════════════════════════
```

## WarmupSaveLoadChain 与叶子预热铁律（DiyUpgradeMod.Restore.cs L596-673）

```
        // 第二轮（2026-10-09）：先把所有 [HarmonyPatch] 的 Prefix/Postfix/Transpiler
        // 本体在浅栈一次性编译完，避免它们首次编译落在玩家交互的 detour 深栈上。
        // 见 WarmupAllPatchMethods 注释。
        // 【2026-10-10】XIAOWO 僵尸手柄清扫链的浅栈预热（集合 + 判据 + 反射成员）。
        //   清扫首次真跑发生在"世界就绪后"（TickZombieHandles 门禁 + 2s 消抖），
        //   这里把它的调用链能提前编的都提前编掉，进一步缩小首次 JIT 暴露面。
            // ★【2026-10-10】改为 Diag（静默闸门）：哑参 null 触发的 NRE 是**预期**的
            //   （预热只需"方法体被 JIT 编译"这一步，编译发生在方法体执行前，
            //   之后抛 NRE 也不影响目标达成）。此前用 Warning 级别会让玩家以为出了故障，
            //   实测 Latest.log 里 RestoreApplyShapes/RestoreRememberPrimary 两条就属此类噪音。
            //   真需要看预热细节时开 DiagLog 即可。
        // ── 读档入口链：RestoreArbiter → ReapplyFromMeta → TryRestoreRememberedGridShape → RestoreShapeCore ──
        // ── 读档恢复辅助链 ──
        // ═══════════════════════════════════════════════════════════════════
        // 【2026-10-10 方案 B · 升级 apply 路径「叶子方法」浅栈预热】
        //   —— 2026-10-10 P2 起改为**声明式**，下面这 8 行硬编码 Step 已删除：
        //        Step("IsHorizontalLayout(null,null)", ...);
        //        Step("ComputeMultiSlotWindowSize(...)", ...);
        //        Step("RememberWindowSize / RememberGridShape / RememberSlotShape", ...);
        //        Step("SyncXiaowoBayTags", ...);
        //        Step("WageContainerRegistry.OnVoidBeadUpgraded", ...);
        //        Step("ContainerWindowResizer.Attach", ...);
        //   现在由方法自己头上的 [JitWarmup] 声明，经 WarmupRegistry.ScanAndRegister
        //   在 WarmupAllPatchMethods 里统一收集（见那里的"第零路"）。
        //   ⇒ 新增叶子时只要加特性就自动进清单，不会再出现"漏登记 → 静默退化成
        //      在 trampoline 上首编译"。
        //
        //   10:37 铁律仍成立：升级链的**大方法**（PrepareUpgradeInternal /
        //   ApplyUpgradePlan / ConsumeMaterial / TryResizeGridWindow）绝不能进预热
        //   —— invoke 时内部会嵌套编译未编译方法 → 重入 CompileMethodHook
        //   → CLR fatal（自爆）。故**只暖叶子**：null 哑参即早退、内部只调已编译的
        //   TagUtil / 游戏原生方法，绝不触发嵌套 JIT。
        //   注：PlayUpgradeSound / ForceRebuildAfterUpgrade 在 ValidateShapeState
        //   返回后（顶层）才调用，运行期自然顶层编译即安全，故不打标记
        //   （前者避免启动期杂声、后者避免启动期起协程）。
        // ═══════════════════════════════════════════════════════════════════
```

