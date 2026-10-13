# CFG 迁移（迁移链 + 终止线 + 各套迁移）

> 本文件内容由以下源码注释逐字搬迁（2026-10-13，注释精简 · 批次1）。源码头部保留摘要与指针。
> 来源：`DiyUpgrade.CfgMigration.cs` / `DiyUpgrade.CfgMigration.OldKeys.cs` / `DiyUpgrade.CfgMigration.Rename.cs` /
> `DiyUpgrade.CfgMigration.DefaultIds.cs` / `CfgKeys.cs` / `SaveBagExpandConfig.cs` / `SuiteVersion.cs`。
>
> 就地判据保留在源码：`CfgKeys.cs` 的常量表、各迁移函数的幂等/防线注释。

## 1. 基座：迁移链与共用工具（`DiyUpgrade.CfgMigration.cs`）

```
// ══════════════════════════════════════════════════════════════════════════
// CFG 迁移 —— 基座（迁移链说明 + 各套迁移共用的读写工具）
//
// 【这个文件现在做什么】
//   只放 4 套迁移**共用**的工具；各套迁移按主题拆到同目录 3 个 partial 文件：
//     · DiyUpgrade.CfgMigration.OldKeys.cs    旧键合并（升级增量 + 诊断开关）
//     · DiyUpgrade.CfgMigration.Rename.cs     键名重排（域号前缀）+ 已删键清理
//     · DiyUpgrade.CfgMigration.DefaultIds.cs 物品ID白名单补齐
//
// 【迁移终止线：MinSupportedFrom = 1.7.4】（常量见 SuiteVersion.MinSupportedFrom）
//   ★ 早于 1.7.4 的旧格式（4 个 DLL / 三段 cfg：[自定义容器升级] / [DIYUpgrade] /
//     [SaveBagExpand]）**整段不再迁移**。原先的「三段并一段」迁移（1.7.3，标记文件
//     ContainerSuite.merged20261006）连同它的全部快照 / MapLegacyKey /
//     DropLegacyCategoriesFromMelon 逻辑已整体删除。
//     跨过这条线的老用户直接按新键名默认值启动 —— 只丢"历史上改过的值"，不丢存档、
//     不影响功能（面板与各容器都照常工作），这是刻意接受的取舍。
//
// 【迁移链顺序】（DiyUpgradeMod.Init 里逐条调用，顺序不可换）
//     MigrateRenamedKeys → MigrateIncKeys → MigrateDiagKeys → EnsureDefaultIds
//   · MigrateRenamedKeys 必须最先：先把「全局 IncW/IncH」搬到 5D_/5E_ 新键，
//     随后 MigrateIncKeys 的「每槽→全局」合并只会"只升不降"，不会把值压回去。
//
// 【为什么收尾要"三处摘"而不是只删 cfg 文件】
//   MelonPreferences 每次 SaveToFile 都会把它内存里的文档整份写回磁盘；
//   只删文件里的行/段，下一次保存就被写回来（2026-10-06 实测 17ms 即被写回）。
//   所以每套迁移都同时摘：① 内存 cat.Entries ② DefaultFile 文档 ③ 磁盘 cfg 行。
//   （技法与 ModManager\PreferencesStore.RemoveCategoryFromMelonDocument 一致。）
//
// 【静默失效防线（全项目共守）】
//   cfg 里明明有旧键、却一项都没搬动 → Error 级报警且**保留旧键、不写标记**
//   （下次启动重试）。绝不为了 cfg 好看而丢用户的值。
// ══════════════════════════════════════════════════════════════════════════
```

## 2. 键名重排（`DiyUpgrade.CfgMigration.Rename.cs`）

### 2.1 为什么只能改键名 / 改了哪些

```
// ══════════════════════════════════════════════════════════════════════════
// CFG 迁移 —— 键名重排（主题：面板键名加域号前缀 77→69，并清掉已删键）
//
// 【为什么只能改键名】
//   ModManager 面板顺序 = (段名, 键名) 的字典序，三个注册表都是
//     OrderBy(CategoryId, OrdinalIgnoreCase).ThenBy(EntryId, OrdinalIgnoreCase)
//   （BoolPreferenceRegistry.cs:16 / FloatPreferenceRegistry.cs:16 / HotkeyRegistry.cs:18）。
//   段名动不了 —— ManagerState.CategoryBelongsToMod 用 Normalize()（只留字母数字转小写）
//   比对段名与 DLL 文件名，拆段/改段名会让整段配置从面板里**消失**（静默故障）。
//   ⇒ 想让"同类项相邻 + 开关块与数值块共用同一套域序"，只能给键名加域号前缀。
//
// 【改了哪些】（两代叠加，最终表的 142 条把两代都收了）
//   ① 第 1 代（2026-10-07，标记 renamed20261007）：77 个面板可见键加域号前缀
//      （0_ / 1A_ / 21_ / 4A_ / 7A_ / z1_ …），另删 8 个「每槽开关」⇒ 净剩 69 项。
//   ② 第 2 代（2026-10-13，标记 renamed20261013）：把编号重排为**由总到分**，
//      详见 <see cref="ContainerSuite.Shared.CfgKeys"/> 文件头的方案表。
//      动机是原来那一套里"诊断开关插在功能开关中间、避让开关排在最常用项之前、
//      分类尺寸被切割到两个波段"，玩家按用途找配置项很难找。
//   ★ 8 个 `*Ids`（string）**刻意不改名**：它们不进面板（string 且不以 Hotkey 结尾，
//     三个注册表都不收），却是玩家手改的物品ID列表 —— 改名只有"迁移漏掉就静默
//     丢失自定义ID"的风险，没有任何面板收益。
//   ★ 热键 UpgradeHotkey → 1D_UpgradeHotkey：仍以 "Hotkey" 结尾，
//     照样被 HotkeyRegistry 收进热键块，不影响识别。
//
// 【为什么删 cfg 里的旧键行不够】
//   MelonPreferences.DefaultFile 文档里的旧条目会在下一次 SaveToFile 被原样写回
//   （2026-10-06 实测 17ms）。⇒ 与既有迁移同一套「三处摘」：
//   内存 Entries + DefaultFile 文档 + 磁盘行。
//
// 【静默失效防线】
//   cfg 里明明有旧键、却一项都没搬动 → 报警且**保留旧键不删**、不写标记
//   （下次启动重试）。绝不为了 cfg 好看而丢用户的值。
// ══════════════════════════════════════════════════════════════════════════
```

### 2.2 `KeyRenames` —— 旧名→新名的单一事实源

```
    /// <summary>
    /// 旧键名 → 新键名。**单一事实源** —— 新增/改名配置项时必须同步这里。
    /// <para>
    /// ★ 收录的是**历史上出现过的全部键名**（含两代旧名）+ 上一代的现名，统一指向当前名。
    ///   这样无论是「一路上来的老用户」还是「只差一代的用户」，都能一次搬到位，
    ///   不会出现"旧名字在新表里查不到 ⇒ 一项都没搬动 ⇒ 用户值静默丢失"。
    /// </para>
    /// <para>
    /// ★ 8 个 <c>*Ids</c> 刻意不改名（在表里查得到才怪）—— 它们是玩家手改的物品清单，
    ///   改名只有"迁移漏掉就静默丢自定义ID"的风险，没有任何面板收益。
    ///   8 个「每槽开关」已删除，见 <see cref="RemovedKeys"/>。
    /// </para>
    /// <para>
    /// ★ 新名一侧请以 <see cref="ContainerSuite.Shared.CfgKeys"/> 的常量为准（这里仍写字面量，
    ///   因为历史旧名本来就没有常量）。
    /// </para>
    /// <para>
    /// ★ 2026-10-13：已移除 56 条「第 0 代裸名」映射（EnableMod / UpgradeHotkey / BoxEnable /
    ///   ShowGrid / DiagLog …），本表由 142 条减至 86 条。依据：迁移终止线为 1.7.4
    ///   （SuiteVersion.MinSupportedFrom），且产品确认 2026-10-06~10-07 的
    ///   「单段 [CustomInfrastructure] + 裸键名」窗口未对玩家公开发布 ⇒ 这些映射永不可能命中。
    ///   ⚠ 代价：单段 cfg 里残留的裸孤儿键不再由本表参与「三处摘」，改由 RemovedKeys 清单承担。
    /// </para>
    /// </summary>
```

### 2.3 `RemovedKeys` —— 已删除的配置键（要清掉、不搬值）

```
    /// <summary>
    /// **已删除**的配置键（在 cfg 里要清掉、不搬值、不存在于新版本）。
    /// <para>
    /// ① 2026-10-07："每槽独立开关"这一批 —— 核桃要求「多槽 槽0-槽3」「模组槽 模组-待定」
    /// 合并到容器级 Enable（容器启用 = 启用所有格）。8 个键 × 两种命名（改名前 / 改名后）
    /// = 16 个，都要能清掉：直接跨版本升级的用户 cfg 里是旧名，已升过 v1.7.4 的是新名。
    /// </para>
    /// <para>
    /// ② 2026-10-10："诊断期"用过的开关 —— z4_DisableCfgKeepAlive（cfg 保活整体删除）、
    /// z2_JitPrecompileProbe（项目已明令禁止其全量 PrepareMethod 逻辑）。它们不再是任何构建的
    /// 注册键，却以"活段内孤儿键"的形式留在 [CustomInfrastructure] 里：MelonLoader 的 document
    /// 是启动时整份 ParseFile 的快照，Save 会连孤儿键及其说明注释一并写回。故须在此显式清理 ——
    /// 文档层 RemoveEntryFromDocument 会把 TomlValue（注释挂在它身上）整块摘掉。
    /// </para>
    /// <para>
    /// ③ R1Probe / ResizeDiag 探针：只在 <c>-p:Diag=true</c> 构建里注册（见 DiyUpgradeMod.Core.cs
    /// 的 <c>#if DIAG</c>）。Release 产物不注册 ⇒ 也是孤儿键，需清理；但 DIAG 构建下它们是活键，
    /// **不能**清（否则每次启动都会把玩家的探针开关重置回默认），故用 <c>#if !DIAG</c> 包住。
    /// </para>
    /// </summary>
```

## 3. 旧键合并（`DiyUpgrade.CfgMigration.OldKeys.cs`）

### 3.1 主题

```
// ══════════════════════════════════════════════════════════════════════════
// CFG 迁移 —— 旧键合并（主题：把旧版本的重复/分散键收敛到统一键，并清掉旧键）
//
//   ① MigrateIncKeys  —— 升级增量「每类别每槽」→ 全局两级（IncW/IncH、ModIncW/ModIncH）
//   ② MigrateDiagKeys —— 诊断日志开关「三个 → 一个」（统一到总开关 DiagLog）
//
// 共用工具（CfgPath/FindEntry/ApplyRawValue/NewKeyOf/ReadSectionValues）见基座文件
// DiyUpgrade.CfgMigration.cs。
// ══════════════════════════════════════════════════════════════════════════
```

### 3.2 ① 升级增量「每类别每槽」→ 全局两级

```
    // ═══════════════════════════════════════════════════════════════════════
    // ① 升级增量「每类别每槽」→ 全局两级（IncW/IncH 原版，ModIncW/ModIncH Mod）。
    //
    // ★ 为什么不只靠段改名迁移：旧版这些键就躺在合并段 [CustomInfrastructure] 里
    //   （CreateGridDef 一直往这个段里写 BoxT0IncW…ModContainerT0IncH），
    //   段级迁移抓不到它们。若不搬：MelonPreferences 按内存已注册条目重写全文件时
    //   会把这些未注册的旧键直接抹掉（或留作无害的孤儿键），用户调过的值静默归 1
    //   —— 正是核桃最恨的"静默失效"。
    //
    // ★ 取最大值而非 last-wins：多个旧槽的 Inc 映射到同一个新键，抽到某个槽的默认 1
    //   会反而把用户调大的值压回去；取 max 保证"不比原来小"。
    //
    // 兜底：旧键即使没被 SaveToFile 删干净，也只是无害的孤儿键（新版本不读），
    //   且本函数每次启动都重读并重新 apply（幂等），不会反复刷盘。
    // ═══════════════════════════════════════════════════════════════════════
```

### 3.3 ② 诊断日志开关「三个 → 一个」

```
    // ═══════════════════════════════════════════════════════════════════════
    // ② 诊断日志开关「三个 → 一个」
    //
    // 合并前：CI `DiagLog` / SBE `SaveBag_DiagLog` / DIY `DIY_DiagLog`，
    //   面板里三项同名「输出诊断日志」，玩家分不清。合并后只剩 CI 的 `DiagLog`
    //   （键名重排后 = z1_DiagLog）。
    //
    // ★ 为什么必须"读一遍再清"：用户可能只开了 SBE 那个（`SaveBag_DiagLog = true`），
    //   CI 的一直是 false。如果只是删键，他的诊断输出会**静默消失** ——
    //   正是本项目最忌讳的"没有报错的功能失效"。
    //   所以取 **OR**：任一旧键为 true → 总开关置 true。（反方向无害：
    //   把三个都关着的用户保持关着。）
    //
    // ★ 两个来源都要看：
    //   ① 合并段 [CustomInfrastructure] 里残留的 SaveBag_DiagLog / DIY_DiagLog
    //      —— 10-06~10-07 之间用过一段的用户；
    //   ② 磁盘 cfg 文件本身（与 ① 同源，ReadSectionValues 直读）。
    //
    // 幂等：清干净后下次启动两处都读不到 → any=false → 只做一次空扫描就返回。
    // ═══════════════════════════════════════════════════════════════════════
```

## 4. 物品ID白名单补齐（`DiyUpgrade.CfgMigration.DefaultIds.cs`）

### 4.1 为什么需要它 / 语义 / 已知取舍

```
// ══════════════════════════════════════════════════════════════════════════
// CFG 迁移 —— 物品ID白名单补齐（主题：代码默认新增了 ID，老玩家 cfg 里存着旧串）
//
// 【为什么需要它】
//   8 个 `*Ids` 是玩家手改的字符串条目，CreateEntry 的默认值**只在条目首次
//   创建时生效**。老玩家 cfg 里早已有 MultiSlotModuleIds = "<旧默认>"，
//   所以把代码默认串加上新 ID 对老玩家是**零效果** —— 白名单认不出新容器，
//   MatchCategory 直接返回 Cat.None，功能静默失效（本项目最忌讳的一类）。
//   （KeyRenames 里那 8 个 *Ids 刻意不改名，正是因为它们靠"玩家手改"工作。）
//
// 【语义：只追加 + 折叠重复，绝不删改玩家已有的 ID】
//   · 追加「代码默认值里有、用户值里没有」的 ID；
//   · 折叠用户值里的重复 token（IsAnyId 命中即返回，重复项是死数据）；
//     仅大小写不同的成对项（历史遗留的 Rat_Cage / rat_cage）折叠成**全小写**那个
//     —— 游戏内 identifier 一律小写，保留大写形式可能匹配不上 item.IsID；
//   · ★ 绝不**删除**用户值里已有的任何 ID —— 玩家可能自己加过自定义容器，
//     也可能主动删掉某个默认容器（不想让别的 mod 抢它的尺寸主权）。做成
//     "默认值覆盖用户值"会把玩家手改过的白名单整条冲掉，是灾难级静默失效。
//   · 代码默认值自身也已去重（CageIds 原本是 animal_cage,Rat_Cage,rat_cage），
//     新装用户不会再拿到这对重复。
//
// 【已知取舍：删掉的默认ID会被加回来】
//   本函数算的是「默认值 ∪ 用户值」，所以玩家**主动删掉**的某个默认容器，
//   下次启动会被重新追加回去（这是"默认值集合只增不减"的必然结果，也让新功能
//   不会被静默漏掉 —— 方向上宁可多给不可少给）。日志会逐项列出补了什么，
//   **不是静默的**；要永久排除某容器，删掉后留意该行日志。
//   反向（连默认值新增的都尊重玩家删除）需要给每个 ID 记"用户已否决"状态，
//   代价与复杂度都不值 —— 记在 TODO 里，需要时再加。
//
// 【幂等】补齐后 added==0 且 collapsed==0 → 不写盘、不打日志（每次启动只扫一次）。
// 【静默失效防线】写入失败 → Error 级报警，绝不静默吞掉。
// ══════════════════════════════════════════════════════════════════════════
```

### 4.2 `EnsureDefaultIds` 的调用时序

```
    /// <summary>
    /// 把「代码默认值里有、用户值里没有」的物品 ID 追加进各 <c>*Ids</c> 白名单，
    /// 并折叠用户值里完全相同的重复 token。<b>只增不减</b>。
    /// <para>
    /// 由 <c>DiyUpgradeMod.Init</c> 在 <c>MigrateDiagKeys</c> 之后调用（配置项都已注册）。
    /// 紧随其后的 <c>cat.SaveToFile(true)</c> 负责落盘，本函数自己不写盘。
    /// </para>
    /// </summary>
```

## 5. CFG 键名单一事实源（`CfgKeys.cs`）

```
// ══════════════════════════════════════════════════════════════════════════
// CFG 键名单一事实源
//
// 【为什么要有这个文件】
//   [CustomInfrastructure] 段下的键名原先以**字符串字面量**散落在 4 个源码目录里
//   （CI / SBE / DIY / 迁移表），同一件事最多有 4 个副本：
//     · CreateEntry("X")                  —— 注册时的键名
//     · DiyUpgrade.CfgMigration.Rename.KeyRenames—— 迁移表的「新名」一侧
//     · 各种 chronology 注释/日志文案里手抄的键名
//     · 玩家文档 README 里的对照表
//   改一处漏一处是本项目反复踩过的故障模式（「中文显示名漏标」就是同一类）：
//   面板里冒出两个键、或者迁移搬到不存在的键上 —— 都是**没有报错的静默失效**。
//   ⇒ 现在键名只在这里写一次；其余地方一律引用常量。
//
// 【为什么要给键名加数字前缀】
//   ModManager 面板按 (段名, 键名) 的 Ordinal 字典序排，段名动不得（段名必须与
//   DLL 文件名一致，否则整段配置从面板消失）。所以「面板里的排列顺序」完全由
//   **键名前缀**决定 —— 这就是下面这套编号存在的唯一理由。
//
// 【编号方案：由总到分，常用在前】（2026-10-13 定）
//   0_  套件总开关                 ← 玩家最可能动的第一个
//   1_  容器升级·全局             （材料 → 操作 → 增量）
//   2_  仓库扩容·总开关与各仓库宽高
//   3_  主仓库分页
//   4_  主仓库滚动
//   5_  基础设施容器（垃圾桶 / 档案箱 / 磁带机）
//   6_  跨 mod 避让（XIAOWO / 蛙哥 / 虚空珠 / 僵尸手柄）← 冷门，放在功能之后
//   7_  容器升级·分类上限         （箱 / 背包 / 笼 / 多槽 / 模组槽 / 三类 Mod 容器）
//   8_  容器升级·单件上限         （每个容器 id 单独设宽高）← 最细粒度
//   z_  诊断与实验                ← 永远垫底，正常玩家用不到
//
//   子字母按「该组内玩家最常改的在前」排，同组内 Enable 先于 Width/Height。
//
// 【改名必须同步的三件事】（漏一件 = 静默丢配置）
//   ① 本文件的常量值；
//   ② DiyUpgrade.CfgMigration.Rename.KeyRenames —— 旧名 → 新名（含历史旧名与新旧两代）；
//   ③ RenameMarkerPath() 的标记文件名换成新日期，否则迁移会被旧标记跳过。
//   读取点（CreateEntry / FindEntry）不需要改 —— 它们用的是常量，自动跟随。
// ══════════════════════════════════════════════════════════════════════════
```

## 6. 段名合并 / DiagLog 别名 / 已删开关（`SaveBagExpandConfig.cs` 节选）

```
        // 2026-10-06 合并（B2）：段名 SaveBagExpand → CustomInfrastructure。
        //   ModManager 归属规则只看 DLL 文件名（ManagerState.CategoryBelongsToMod），
        //   单 DLL 之后文件名恒为 CustomInfrastructure，段名不跟着改的话
        //   本模块 19 个配置项会全部从面板里消失。
        //   旧段 [SaveBagExpand] 的值由 DiyUpgrade.CfgMigration 一次性搬过来，不丢配置。
        //   必须复用 CI 已创建的那个 category 实例（否则同名段注册两次、保存互相覆盖）。
```

```
        // 2026-10-06 合并（B2）：三段并成一段后，三个模块的键名必须互不冲突，
        //   只有 DiagLog 撞名（CI / SBE / DIY 各有一个）。当时把本模块改名成
        //   SaveBag_DiagLog、DIY 改成 DIY_DiagLog。
        //
        // ★ 2026-10-07 再改：三个开关**合并成一个总开关**（面板里三项同名，
        //   玩家根本分不清该开哪个）。现在本字段是 CI 那条 `DiagLog` 的**别名**
        //   —— 同一个 MelonPreferences_Entry 实例，不再是本模块自己的条目。
        //   所以：
        //     · 所有 `SaveBagExpandConfig.DiagLog?.Value` 读取点一行未改；
        //     · 旧键 SaveBag_DiagLog / DIY_DiagLog 由 DiyUpgrade.CfgMigration
        //       的 MigrateDiagKeys 做 OR 迁移 + 三处清理（见那里）。
        //   ★ 时序前提：CI.Init 早于 SBE.Init（Mod.OnInitializeMelon 的顺序），
        //     所以这里取到的一定是已创建好的 entry。
```

```
        // ── 客户柜台（S8f：开关合并入【仓库·扩容】总控，尺寸仍独立配置） ──
        //   ⚠️ 原 FrontInvExpandEnabled 独立开关已删除。老的 cfg 里如果还留着
        //      这个 key，MelonPreferences 会忽略（未注册 = 不激活），无害。
```

## 7. 迁移终止线常量（`SuiteVersion.cs`）

```
    /// <summary>
    /// CFG 迁移的**最低支持来源版本**（迁移终止线）。
    /// <para>
    /// 约定：只保证把「≥ 本版本」的配置自动迁移到当前格式；早于本版本的旧格式
    /// （4 个 DLL / 三段 cfg：<c>[自定义容器升级]</c> / <c>[DIYUpgrade]</c> /
    /// <c>[SaveBagExpand]</c>）**不再迁移** —— 老用户跨过这条线直接按新键名默认值启动
    /// （只丢"历史改过的值"，不丢存档、不影响功能，可接受）。
    /// </para>
    /// <para>
    /// 为什么是 1.7.4：该版本做了 cfg 键名重排（域号前缀），之后所有面板可见键都是新键名；
    /// 早于它的「三段并一段」合并（1.7.3）逻辑已整段删除，见
    /// <c>DiyUpgrade.CfgMigration.cs</c> 文件头的「迁移终止线」说明。
    /// </para>
    /// </summary>
```
