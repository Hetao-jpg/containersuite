================================================================================
ContainerSuite · mod 维护日志（开发端）
================================================================================

2026-10-07 文档精简：工作区文档固定为三份 ——
  · readme.mod维护日志.md（本文件，开发端）
  · readme.用户须知.md    （用户端完整参考）
  · 安装说明.txt          （随包发布的一页式上手；pack.ps1 会把它复制成 dist\readme.txt）
原 README.md（只做索引）与两份历史稿已归档到 _archive\readme-已精简-20261007\。
本文件收录：四个工程 / 构建与产物 / 三道构建守卫 / 维护日志（倒序）/ 禁止事项。
代码注释里写的「README 禁止事项 #N」→ 本文件第八节；
「README 已知限制 #N」→ readme.用户须知.md 第五节。

================================================================================
一、四个工程
================================================================================

  ★ 2026-10-06：4 个 DLL 合并成 1 个。
    源码仍留在原 4 个目录里（文件一个没动，命名空间也没动），
    由 ContainerSuite.Infrastructure.csproj 的 <Compile Include> 拉进来编译；
    另外 3 个 csproj 已改名 *.csproj.disabled，不再单独产出。

  ContainerSuite.Shared       产物并入 CustomInfrastructure.dll  命名空间 ContainerSuite.Shared   角色 共享库（无 MelonInfo）
  ContainerSuite.Infrastructure 产物 CustomInfrastructure.dll(唯一) 命名空间 CustomInfrastructure  角色 唯一 MelonMod 入口
  ContainerSuite.SaveBag      产物并入 CustomInfrastructure.dll  命名空间 SaveBagExpand           角色 仓库扩容+分页+滚轮
  ContainerSuite.DIYUpgrade  产物并入 CustomInfrastructure.dll  命名空间 DIYUpgrade              角色 拖材料升级+拖边缩放+读档恢复

  为什么合并成 1 个 DLL（2026-10-06）：消除"缺 1 个 DLL→全崩"故障面；消除 4 处
  AssemblyVersion 同步漂移；玩家只覆盖 1 个文件；构建从 4 次收敛成 1 次。
  代价 = 必须连带做完：B3 4 份 AssemblyInfo 合并 1 份（否则 CS0579）；
  B1 按程序集扫补丁改按命名空间过滤（否则 Postfix 跑两遍）；
  B2 3 个 cfg 段并成 [CustomInfrastructure]（否则面板丢 31 项）。

  分工（互不重叠）：trashcan/dossier/cassette_player→基础设施；
  SaveBag/ShowcaseInv/HiddenInv/BackInv/FrontInv→扩容仓库；
  箱子/背包/笼/多槽/机器模组槽/Wage 容器→容器升级。

================================================================================
构建与产物
================================================================================

  构建: cd "D:\games\open code work\ContainerSuite" && dotnet build ContainerSuite.sln -c Release

  ⚠ 三道构建守卫（CommonMod.props 各 Exec 一个，只在编译 Infrastructure 时跑一次，
    任一失败 = MSB3073 构建失败，pack.ps1 走构建时打包前必过）：
    · P4  check-suite-version.ps1    SuiteVersion.cs 与 AssemblyInfo.cs 版本必须一致
                                    （合并前 5 处、合并后 2 处，只改一处编不过）。
    · DOC check-readme-defaults.ps1  README「默认尺寸」表须 == SaveBagExpandConfig.CreateEntry 默认值。
    · FBD check-forbidden.ps1        禁止事项 #21（Validate 实参须走命名常量）
                                    + #22（网格重操作文件须引用 WorldReadyGate 或 $allowGate 登记）
                                    + #25（禁止 RuntimeHelpers.PrepareMethod 做 JIT 预热）
                                    + #26（禁止新挂 IL2CPP 热属性 get_/set_ 访问器，
                                           除 $hotAllow 列出的 4 个布局 getter）。
    ★ 脚本必须纯 ASCII（PS 5.1 按 ANSI 读，中文注释→语法错→整个构建失败）。
    ★ 注册表自检脚本（verify-*.ps1）含中文时须存 UTF-8 with BOM。

  产物（只有 1 个）：ContainerSuite.Infrastructure\bin\Release\CustomInfrastructure.dll

================================================================================
七、维护日志（倒序）
================================================================================
  ───────────────────────────────────────────────────────────────────────────
  2026-10-13  注释精简 · 批次1：新建 docs 索引 + 8 篇主题 doc（纯新增，逐字搬迁）
  ───────────────────────────────────────────────────────────────────────────
  · 依据《注释精简建议.txt》五问定去留；批次1 只建「迁入地」，不改任何源码。
  · docs/readme.md（索引）+ 8 篇：cfg-migration / jit-warmup / crash-defense /
    monotonic-clock / silent-failure / diy-restore / diy-resizer / ci-expander。
  · 内容为源码注释逐字搬迁（只抽注释行，不含代码行）；含 0x80131506 的行照抄进 docs，
    但源码侧保留（后续批次不搬它们；判据：grep -c 0x80131506 不变）。
  · 用户裁决：D4 只删纯沿革句、保留警告句；D5 自我否定块搬 docs 不删；3 篇可选 doc 本轮做；
    项目铁律 L1~L10 降级（不再当绝对红线，用户仍在持续优化 mod）。
  · 待办：批次2 源码留指针、批次3 执行 D1~D4 删除、批次4 Release+DIAG 双构建 + 门禁 + regress。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-08  物品ID白名单新增水瓶打印机 + 老玩家自动补齐（v1.7.6，已实机验证）
  ───────────────────────────────────────────────────────────────────────────
  · bottle_printer 归 **4E 机器模组槽**（产出格是 MachineBottlePrinter 里的
    GameGridInventory，与已在列的 water_purifier 同构），不是 4D 多槽容器。
  · 坑：*Ids 的 CreateEntry 默认值只在条目首次创建时生效，老玩家 cfg 早有旧串
    → 只改代码字面量对他们零效果（MatchCategory 返回 None，静默失效）。
    ⇒ 新增 EnsureDefaultIds：每次启动把"代码默认有、用户值没有"的 ID **追加**进去，幂等。
  · 三条铁律：只追加从不删除用户 ID；仅大小写不同的成对项保留全小写；写入失败打 ERROR 不静默吞。
  · CageIds 默认串去冗余：animal_cage,Rat_Cage,rat_cage → animal_cage,rat_cage。
  · 实机（蛙哥 1.3.2 + XIAOWO 同时启用）：打印机升级 2×4→3×5→4×6→5×7→6×8 逐次留痕；
    13 条 [REG] 全 PASS；蛙哥"抢金属锭"担心未坐实（Priority=900>蛙哥 400，按住升级键本 mod 先短路）。
  · 残留项定为「已知未知·暂不处理」：蛙哥品质加成是否仍正常涨（入口不可达，未加规避）。
  · 纠错：readme 原文"改名会被 MelonLoader 拒绝加载"是错的——实测中文名长期正常运行；
    改名唯一风险是"双重加载"（同程序集被加载两次），故部署必须覆盖原文件、不能新增。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  补丁稳定性/跨 mod 冲突审计 + Validate 语义更正 + Show 去重 + 守卫 R1 重写（v1.7.5 内）
  ───────────────────────────────────────────────────────────────────────────
  · 方法（可复用）：ilspycmd 11.1 反编译 Il2CppAssemblies\Assembly-CSharp.dll（权威）
    + 已加载的 40 个 mod DLL + Latest.log 实证；25 个挂点全部有效、0 失败。
  · 更正 1 · EmporiumEntry.Validate(bool force=true) 默认值就是 true；源码 Validate() 生成的 IL
    与 Validate(true) 完全一致 ⇒ 旧禁止事项 #21「无参=Validate(false)」是错的；
    S8t「改回无参」那次修复并未改变运行时行为（误判）。决策：保持 true，实参显式化为
    命名常量 EmporiumForceValidate，并加无条件启动日志打实际实参；FBD 的 R1 据此重写为
    R1a（禁裸布尔字面量）+ R1b（两处须走常量）。
  · 更正 2 · PixelWindow.Show 原被 CI 与 DIY 各挂一次（2 Postfix）；现统一由 WindowShowHook
    单点覆盖（按方法名遍历重载），CI 补丁数 4→2；ShowPostfix/DelayedExpandFromWindow 已删。
  · 新增 #24 不可共存清单（旧版独立 DLL / XIAOWO / BayExpand / FullMachineUpgrades / AllLargeStorage）。
  · 踩坑：check-forbidden.ps1 必须纯 ASCII（加中文注释→构建整段失败）。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  充能器升级后贴图溢出——只显示前 4 颗电池（原生取证 + 钳制，v1.7.5 内）
  ───────────────────────────────────────────────────────────────────────────
  · 取证：IL2CPP 无方法体→走 capstone 原生反汇编。MachineRecharger.Recharger 每颗电池一条贴图，
    位置 x=i*16；机器图标宽 4 格=64px ⇒ 第 5 颗 x=64 出界=根因。
  · 挂点：patch GameItemElement.UpdateInnateModSprite 的 Prefix，按 x<64 放行、非充能器放行、其余跳过。
    刻意不加 WorldReadyGate 门禁（只少画一条、不改状态；加门禁会残留贴图=复现症状）。
  · 验证：build 0 错 0 警 + 三守卫；regress 0 FAIL；diy.patch installed=16（15→16）。
  · 未验证：>4 颗电池实际观感（需人往升级过的充能器塞≥5颗后看一眼）。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  每槽独立开关合并到容器级「启用」（面板精简，v1.7.5）
  ───────────────────────────────────────────────────────────────────────────
  · 删 8 个每槽键（4D_MultiSlotT0~T3Enable、4E_MultiSlotModuleT0~T3Enable）；
    CreateGridDef 不再建 {prefix}T{i}Enable（Enable=null=永远启用）；slotCount 维持 4。
  · 迁移：KeyRenames 77→69；新增 RemovedKeys（16=8键×新旧两套命名）；
    IsDroppedKey()+DropRenamedKeysFromMelon+RemoveRenamedKeyLines 按 KeyRenames∪RemovedKeys 摘键。
  · 可见条目 77→69（开关 35→27、数值 41、热键 1），页数 8→7。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  cfg 键名重排（域号前缀）：让 ModManager 面板按功能域聚拢
  ───────────────────────────────────────────────────────────────────────────
  · 依据：2026-10-7modcfg顺序与信息架构分析.md + 逐条回 ModManager 1.2.1 源码复核。
  · 面板顺序=(段名,键名)字典序，强制切成 热键→开关(bool)→数值(float) 三块；
    旧键名按字母排⇒数值块里扩容/升级/基建/分页/滚动互相交错⇒加域号前缀解决。
  · 77 个面板可见键加前缀（0_/1_/2_/3_/4A-E/5A-E/6A-C/7A-E/8_/9_/z1/z2）；
    8 个 *Ids（string）刻意不改名（不进面板、玩家手改、改名只有丢值风险）。
  · 迁移 KeyRenames（单一事实源，77 项）+ MigrateRenamedKeys：读旧键值→按类型写新条目→
    三处摘旧键（内存+DefaultFile文档+磁盘行）→标记文件 ContainerSuite.renamed20261007。
    静默失效防线：有旧键却一项没搬→报警且保留旧键不删。
  · 复核中修正 3 条：CfgMigration.cs 自身硬编码了将被改名的键（DiagLog/IncW 等）→已全部经
    NewKeyOf() 翻译；"76 个旧键残留"实为 35 个 bool 旧键产生幽灵双份；*Ids 改为不改。
  · 迁移顺序：MigrateRenamedKeys 必须排在 MigrateIncKeys 之前。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  配置面板可读性（方案 A：统一 DisplayName）+ 关闭遗留 DiagLog
  ───────────────────────────────────────────────────────────────────────────
  · 硬约束（源码级）：C1 顺序=(段名,键名)字典序；C2 强制 热键→开关→数值 三块（同容器启用于宽度必跨页）；
    C3 Description 完全不显示（面板只 Label/Value）；无任何隐藏机制（页数只能靠删条目降）。
  · 修复（只改显示名，不动键名/值/逻辑）：标签统一「[域] 对象·属性」；
    危险项写「避让·虚空珠(不可逆)」、范围写「[分页] 总页数 (1-10)」、调试项写「[诊断] 日志(排查时开)」。
  · 关闭排查期遗留 DiagLog=true（会刷日志+每帧多 6 次 Stopwatch）。
  · 放弃「DiagLog 自动过期」（自创行为，会在排查中途关掉开关）；用"让人看得懂"替代"替人做决定"。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  诊断开关「三个→一个」总开关（z1_DiagLog）
  ───────────────────────────────────────────────────────────────────────────
  · 键名沿用 CI 的 DiagLog；SBE/DIY 的条目改为直接指向同一 entry 实例（别名），读取点一行未改。
  · 迁移 MigrateDiagKeys：取 **OR** 而非 last-wins（用户可能只开了 SBE 那个）；
    三来源都看（残留键/旧段快照/磁盘直读）；清键走三处摘除；MapLegacyKey 对 DiagLog 返回空串。
  · 实机（故意预置 SaveBag_DiagLog=true 验 OR）：迁移行命中、DiagLog=true、两旧键消失、其余键零误伤。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  S9 分页读档窗口：绑定时机 + 页归属自愈（"第二页物品显示在第一页"）
  ───────────────────────────────────────────────────────────────────────────
  · 根因三独立：① BindSettleSeconds 用 SecondsSinceLastReady（最慢 mod 决定可用时刻）；
    ② 稳定期 3s+1s 轮询过长；③ 分发只跑一次、无自愈（读档灌入合并态后 TAB 把合并态快照进当前页）。
  · 方案：判据改 SecondsSinceFirstReady（新增）；稳定期 3s→0.2s、绑定每帧尝试；
    形状真变才走 DistributeFromGrid，否则交自愈；新增 EnsureGridOwnershipClean()
    （把"不属于当前页"的物品逐件 Expel 回各自页，须跳过 _mergedForSave，触发点三处：
    绑定后 6s 每 0.2s / 切页前 / 存档前）；启动预热补 3 个 Step。
  · 实测：窗口 6.27s→0.814s；moved=3 与旁路文件一致；无 0x80131506 / 无 ERROR / 无合并丢失告警。
  · 未验证：视觉确认、反复读档≥5 次、尖刺直读。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  IncW/IncH 升级增量合并为全局两级（28→4 配置项）
  ───────────────────────────────────────────────────────────────────────────
  · 提升为全局两级：IncW/IncH 管所有原版容器；ModIncW/ModIncH 管所有 Mod 容器（靠 isMod 路由）。
    ★ 仍是两条独立全局键（宽/高各自），未丢失非正方增长自由度。
  · 读取点 4 处统一引用共享条目→读取点一行不动；Enable/Ids/MaxW/MaxH/AlignSlots 仍按类别保留。
  · 迁移旧 28 键按"原版→IncW/IncH、Mod→ModIncW/ModIncH、多键取 max"并入（保证不比原来小），幂等。
  · 唯一 1 FAIL=reg.ci.restore（回归器只拉菜单不读档，与本次无关）。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  文档精简：readme 收敛为三份 + 清除合并前过期事实
  ───────────────────────────────────────────────────────────────────────────
  · 安装说明.txt 大面积过期（还写"4 个 DLL""三段 cfg"）→ 改为 1 个 DLL / 单段；帧预算 0.3%→0.03%。
  · 三份文档分工明确；归档到 _archive（移动非硬删）。_regress/README.md、mod开发指南.md、
    两处 _S?备份 目录里的 README 不动。
  · 遗留：安装说明.txt 是 .txt 不在 DOC 守卫扫描范围，其"24×10"仍可能静默漂移。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  A2b（CI 等待墙钟化）+ B 方案（偏移/仲裁显性化 + TD-09 基线）
  ───────────────────────────────────────────────────────────────────────────
  · A2b：DelayedRestoreFromLoad 的 30 帧循环→墙钟 0.5s；新增 [REG] ci.restore 验证不漏项。
  · B2：OffsetResolver 首次解析无条件留痕 [REG.BP]；RestoreArbiter 两处放弃点留痕；checks.json +4 条。
  · B1：TD-09 基线（DiagLog=true）稳定态 Arbiter 0.5μs/SBE 2.2μs/DIY 1.6μs 合计≈0.03% 帧预算；
    尖峰 SBE 单帧 138ms/100ms（真热点在 SBE 绑定/预热路径，非 Arbiter/DIY）。
  · B3：7 个"纯放行" Prefix 评估结论=不动（收益远小于 SBE 尖刺，且 IL2CPP 无法脚本化拖拽验不了）。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-07  A 方案（缓存上限统一 / DIY 读档等待改墙钟 / 补丁阈值统一）
  ───────────────────────────────────────────────────────────────────────────
  · A1：新增 CacheLimits.cs（Large=512/Medium=256/Small=128，每级>=替换掉的原值），8 处裸写改 Trim()；
    _redrawPending 不加（由 finally Remove 天然以在途协程数为上限）。
  · A2：DIY DelayedRestore 90 帧→墙钟 1.5s（RestoreWarmupSeconds）。
  · A3：MinExpectedPatchCount=14（人类日志+[REG] 共用）/ MinInstalledPatchCount=10，消灭 14/20 双阈值矛盾。
  · 未做：CI 的 30 帧循环本次未动（留作独立项）；交互期行为未人肉验。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-06  S9 实机日志自检 + 存档静默丢档告警 + 三道构建守卫
  ───────────────────────────────────────────────────────────────────────────
  · 起因：日志写"Wage 避让 13/14"而 README 写 14/14——静默失效比崩溃更难查，先把"看日志"变"脚本看日志"。
  · 取证：ilsdump 扒 WagePerks.dll→DoUpgrade 真实签名 (GameItem,GameItem,Boolean)→我们按两参打，
    从未挂上；同份日志反查出分页合并超容量时物品被 UncheckedAccept 拒收=凭空消失。
  · 修复：SkipWageUpgradeRestore 加 PatchAny（候选签名依次试）；
    B1 存档合并丢失三段式（Log.Warning+SaveBagPaging.mergeloss.txt 旁路清单+屏幕红字 15s，只报告不塞回）；
    B3 CleanupCfgFile 改写前先备份 MelonPreferences.cfg（时间戳 .bak，留最近 3 份）；
    新增 verify-log.ps1（含中文须 UTF-8 with BOM）。
  · 三道守卫 P4/DOC/FBD 落地（FBD 抓出文档 24×16/7×5/2×2 vs 代码 24×10/9×7/4×4，一并修正）。
  · 实机 7/7 PASS；B1 三次尝试均未触发（实测重叠 21 格但物品都在，不报）；Bind SetShape 不吃 cfg 尺寸待查。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-06  S8u-2~3 0x80131506 真根因钉死与修复（带参包装器 localloc 内联 + tier1 阈值）
  ───────────────────────────────────────────────────────────────────────────
  · 根因：带参 il2cpp 包装器（Expel 等）含 mul.ovf.un+localloc，被内联进带 EH 的调用方
    （CoreCLR JIT 缺陷）= 致死点（与时间窗/Harmony 无关）。
  · 修复：S8u-2 BindSettleSeconds=3s（缩小暴露面）；WarmupJit 真实调用预热；
    Expel/Place 三调用点全改 MethodInfo.Invoke 反射（虚分发等价、任意 tier 免疫）。
  · 铁律（#22 具体化）：凡"带参 il2cpp 包装器"不得出现在带 EH 方法体，一律缓存后反射 Invoke。
  · 验证：读档/连续切页×10+/重启/启动全过，0x80131506 消失。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-06  S8u-4 读档页归属丢失→页ID改走旁路文件
  ───────────────────────────────────────────────────────────────────────────
  · 根因：cfg 里 11 个 Slot*_Page2Ids 全是""从未落盘；真凶=ML 的 WasError 机制
    （别的 mod 写完 cfg→IOException→WasError=true→本会话后续所有 Save() 静默失效）。
  · 修复：页ID 改走 UserData\SaveBagPaging.ids（ASCII，tmp+Move 原子写，SaveIdSets 同步落盘+5s 重试）；
    删除 EnsureEntries/_pageIdStore 全链；启动自检写临时槽-999回读。
  · 历史数据不可追回（旧机制下页ID从未落盘），整理+存档一次后恢复。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-05  S8u SaveBagExpand 分页与网格操作冲突（CLR 0x80131506）→ 已推翻根因
  ───────────────────────────────────────────────────────────────────────────
  · 现象/修复同上 S8u-2~3（WorldReadyGate 检查 + Show defer）；本条目原"根因=Harmony 叠加/时间窗"
    的判断被推翻，WorldReadyGate 检查保留（缩小暴露面），但它不是致死点修复。
  · 版本：v1.7.3 内同期修复，未对外发过含此 bug 的版本。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-05  S8x 新开存档 WorldReadyGate 卡死修复
  ───────────────────────────────────────────────────────────────────────────
  · 新开存档不走 LoadGame→MarkReady 永不调→ShouldDefer 永 true→DIY 升级入口全放行给原版。
  · 修：Invalidate 后 8 秒还没 MarkReady 且 saveSlotId>=0→主动放行并打日志。版本不升。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-05  S8t 主仓库窗口位置回归 → 见 #21 更正（行为未变）
  ───────────────────────────────────────────────────────────────────────────
  · 现象：主仓库窗口被拉回屏幕内完整显示。原归因 Validate(true)→已推翻：
    源码 Validate() 与 Validate(true) IL 完全一致，那次修复未改变运行时行为（误判）。
  · 判断依据（通用）：SetAllDirty/ForceRebuildLayoutImmediate 只标脏不改位置；
    Validate(true)/带 rebuild 语义的 API 会重排布局改位置。版本不升。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-05  S8r DIY 常驻 tick 优化（空闲 -45%）
  ───────────────────────────────────────────────────────────────────────────
  · 加「鼠标没变 && 无拖拽 && 上帧无 hover」判据跳过整帧，每 6 帧强制完整 tick 兜底。
  · 空闲 60~80μs→40~53μs，峰值 5000μs→385μs；拖拽/升级/分页不变。
  · 未达 <15μs（States 清理遍历未跳过），边际收益不划算，决定不做。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-05  S8q TD-09 性能基线 + 结案（无需优化）
  ───────────────────────────────────────────────────────────────────────────
  · 实测：空闲 70~90μs(0.4~0.5%)、SBE 分页 1250~1370μs(7.5~8.2% 峰值 47ms)、
    恢复路径峰值 1438μs(8.6% 一次性)、拖拽 66~92μs、DIY 升级 90~112μs。
  · 结论：ResizeEdge.Tick 不是瓶颈；除 TAB 分页/读档两个主动事件外全 <2%；两尖刺主观一次性不处理。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-05  S8p TagUtil 推广到 DIY + TD-05d DiagEnabled 接线 + TD-06 孤儿 key
  ───────────────────────────────────────────────────────────────────────────
  · ① DIY 侧 4 项 TagUtil 定义全删、7 文件调用点加 TagUtil. 前缀；IsDiagEnabled 提 public。
  · ② CI Mod.cs 在 DIY Init 之后接 DiagEnabled = IsDiagEnabled（Init 前恒 false=静默失效）；
    首次拿到基线「处理 36 容器，耗时 72 帧」。
  · ③ LegacyKeys 加 FrontInvExpandEnabled，靠版本号变化驱动 P6 清理（发 v1.7.3）。版本 1.7.2→1.7.3。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-04  S8o Wage 命名空间修复 + ClampToXiaowoCeiling 废弃 + 选项1清理 + TagUtil
  ───────────────────────────────────────────────────────────────────────────
  · ① Wage DLL 内部命名空间是 WagePerks 非 JacksonPerks→SkipWageUpgradeRestore.cs 全改，14/14 挂上。
  · ② 升级成功日志：apply 委托加 [容器升级] ... 升级第 N 次 行。
  · ③ ClampToXiaowoCeiling 彻底废弃（XIAOWO 手柄已被避让拦掉，对齐前提不存在→纯负担；
    引入"读档回弹+拖小无法拖大"两反向 bug）。
  · ④ 5 项 L0~L1 死代码清理（含 ClearCache 误删 TryExpand 致 CS0117 教训）。
  · ⑤ TagUtil.cs 新建（SetItemTag/SafeTagInt/GetTagSetter 缓存），本次只迁移 CI 侧。

  ───────────────────────────────────────────────────────────────────────────
  【经验归档】2026-10-02 晚 S8h~S8n：与两个第三方 mod 的冲突治理
  ───────────────────────────────────────────────────────────────────────────
  ★★★ 1. 避让只能拦「入口」和「收敛点」，绝不能拦「内部实现层」
        XIAOWO 写 if(!ApplyStoredShape())throw——拦内部实现=逼它自爆。看调用方有无 if(!X())throw。
  ★★★ 2. "入口封死了" ≠ "对方全套都停了"
        XIAOWO 另有独立拖拽手柄 ContainerWindowResizer(8个)与我们7个叠同窗口抢鼠标，
        日志全绿但功能坏（最费时间）。排查兼容冲突还要 grep 对方有无自己的 Tick/UI 子系统。
  ★★★ 3. 消除冲突 ≠ 剥夺对方全部玩法
        S8j 把虚空珠做成"DIY 无条件接管"→蛙哥逐格解锁全失效。正确(S8l)：默认给对方全权，
        用玩家意图(按住升级键)当移交信号，单向门+幂等移交(不可逆)。
  ★★ 4. 判据函数必须无副作用——移交判据被每帧调，写 tag 统一放 EndDrag(唯一能拿全上下文的时机)。
  ★★ 5. "某 mod 已接管" ≠ "基准已记录"——状态判定优先序：游戏信号 > 运行时反推 > 形状 > 自造 tag。
  ★★ 6. 加避让开关必须覆盖该模块**每一个** Prefix 入口；默认值选"保持现状"侧，读取失败 fallback 到它。
  ★★ 7. 对方私有状态表要"调它的方法"不要抄；逐容器对齐不动玩家全局 cfg；只夹上限不动下限。
  ★ 7b. 怀疑某补丁"可能还有影响"时先验证再动手；HarmonyPatch 目标从 IL 特性字节解出，别按方法名猜；
        每帧路径上的拦截点先加只读日志确认还触不触发。
  ★ 8. 排障脚手架要设"拆除条件"（实机验证通过即删，留注释块）。
  ★ 9. IL2CPP 硬约束：热开关只短路判据第一行绝不 Unpatch/重装(0x80131506)；
        ref 参数签名须 ilspycmd --ilcode 核对；foreach IDictionary 拿到 DictionaryEntry 非 KVP；
        同名类调试极易看错文件。

  ───────────────────────────────────────────────────────────────────────────
  2026-10-02  S8n 封 XIAOWO 拖拽手柄 + 上限对齐（修手柄判定+网格残留）✅ 实机通过
  ───────────────────────────────────────────────────────────────────────────
  · 根因 3 独立：① 同名 ContainerWindowResizer 8手柄叠同窗口抢 sibling+XIAOWO 轮询开拖不依赖 raycast；
    ② 僵尸手柄(历史升级过的容器手柄进私有 States 字典，只有 Dispose 清)；③ 两套上限不一致。
  · 处置：Prefix_Attach 拦 XIAOWO Attach；TickZombieHandles(2s节流)反射清残留；
    TryGetGridShapeLimits 末尾 ClampToXiaowoCeiling(后于 S8o 废弃)。
  · 已作废方案2：当时猜的 4 个 Postfix 真实挂点是 widthPixels/heightPixels(get;set; 真实存储属性)，
    非 childWidth；且只在 ApplyWindowSize 期间生效、finally 清零，手柄封掉后门控不再打开。

  2026-10-02  S8m 移除避让诊断台账（收尾）：删 F11/F12 台账+TickDiagnostics，保留功能日志。1.7.0→1.7.1。
  2026-10-02  S8l 虚空珠移交门（问题1修复）：GetWageCategory 改移交门(默认归蛙哥)、新 tag、
    移交动作统一 EndDrag、HandOverVoidBead 幂等、MigrateLegacyVoidBead 收紧、Prefix_DoUpgrade 判据改。1.6.0→1.7.0。
  2026-10-02  S8k 避让热开关：cfg 新增 SkipXiaoBay/SkipWageBox(默认 true)；补丁常驻+判据第一行短路不 Unpatch；
    覆盖每个 Prefix 入口；读取失败 fallback true。1.5.1→1.6.0。
  2026-10-02  S8h 蛙哥容器冲突首次全面反编译：确认 DLL 内部命名空间 WagePerks、4 处 titleString 格式。
  2026-10-02  S8i/S8j 入口优先级+蛙哥接管：删 ApplyStoredShape 拦截(逼对方自爆=病根)、改拦入口层+7处Priority(900)；
    ref bool 参数致 7补丁只挂1个(须 ilspycmd --ilcode)。1.4.0→1.5.1。
  2026-10-02  S8g 配置默认值+诊断项合并：三总开关默认 false→true(开箱即用)；诊断合并为 SaveBag_DiagLog。1.5.0→1.5.1。
  2026-10-02  S8f SBE 配置调整：展示柜 9×7→7×5；删客户柜台独立开关并入总控。
  2026-10-02  S8e 清理死代码：删 6 处零调用方方法+连锁字段。编译 0 警告。
  2026-10-02  S8d P4 版本同步自动化：check-suite-version.ps1（纯 ASCII+IndexOf）；曾用 RoslynCodeTaskFactory 致 props 全崩→改 PS+Exec。
  2026-10-02  S8c 三项优化：P2 SetItemTag 改官方 API；P5 MarkDragOffsetSuspect；P8 Off.Drag* 走 OffsetResolver(未命中=等价硬编码)。
  2026-10-02  S8b P1 抽 GridFx.cs（SBE/DIY 重复网格处理收拢 Shared）。
  2026-10-02  S8a 低风险优化三连：P3 去重告警；P7 队列溢出 Warning；P6 CleanupCfgFile 加版本标记。
  2026-10-02  S8 立项：优化计划 P1~P8 按性价比排序。
  2026-10-02  S7 ModManager 显示修复：DIY 段名 自定义容器升级→DIYUpgrade；CI MelonInfo 改 CustomInfrastructure；
    诊断项加 DontSaveDefault；未装蛙哥时不生成 24 项。
  2026-10-02  S6 三处🔴全清(均实机验证)：#3 存档异常 TAB 死锁→Finalizer+10s看门狗；#2 热改分页数→保留内容+安置；
    #1 DIY 读档恢复→反编译取回原方法体拆 8 小方法+RestoreOnLoad 开关。
  2026-10-02  S5 收尾：备份 S4 到 _S4备份_20261002；客户柜台改用游戏信号判定；每格像素从网格反推；版本号统一。
  2026-10-01~02  S4 三合一：3 独立 Mod→1 MelonMod 入口+4 DLL。
  2026-10-01  SBE v1.1.2 客户柜台独立化+跨场景残留清理。
  2026-09-28  CI P0 修加载旧档闪退(0x80131506)，加 _suppressUntilFrameEnd 闸门。
  2026-09-28  DIY v1.3.2→1.4.0 审查#1~#8全修；单文件 3548 行→13 partial。
  2026-09-26  SBE 配置系统重构+稳定性修复(v1.1.1)；工程格式统一(GamePath 集中、补 Private)。

================================================================================
八、禁止事项（踩过坑，别回退）
================================================================================

   1. 不要删 SkipWageUpgradeRestore.cs 的 Prefix_ConsumeNutsDirectly / Prefix_DoUpgrade

   2. 不要把 tag 写入改回 item.state.GetTag(tag).SetInt()

   3. ModifyTag 的 skipValidate: true 不能去掉

   4. 不要恢复 DIY 的 base 重置段 / MetaSanitize / floor 探测

   5. 不要撤销 CanContainItems 的放大提前退出、ForceRebuildAfterUpgrade 的 5 帧

   6. 不要给带 ref bool __result 的方法打 Prefix（native crash）

   7. 不要在 DelayedRestore 里遍历玩家库存

   8. 不要改回"优先级"材料模式（已定案为集合模式）

   9. 不要手写 rectTransform.sizeDelta（绕过画布缩放，破坏 GetWorldCorners 判定，已坑两次）

  10. 不要撤销 ApplyMeshAlpha 的"无条件写 + 逐节点 SetAllDirty"

  11. 不要在日志里再手写一遍 Mod 名（MelonLoader 会自动加前缀，否则双前缀）

  12. 不要让"清理残留状态"受用户开关控制（维护与配置必须分离）

  13. 不要相信 "HarmonyPriority 高就能拦住别的 Mod 的 Prefix"
        S8n 实测：DIY 的 MayTarget Prefix 加 [HarmonyPriority(900)]，蛙哥 Prefix 仍照常执行。
        → 要拦别的 Mod，只能 patch 它自己的方法，或给它加前置补丁，不能指望 Priority 顺序。

  14. 不要用"入口层已封"推断"对方全套都停了"
        S8n：XIAOWO 另有独立拖拽手柄叠同窗口抢鼠标，日志全绿但功能坏。
        → 排查兼容冲突还要 grep 对方有无自己的 Tick()/Update()/UI 子系统。

  15. 不要拦对方方法的"内部实现层"
        XIAOWO.TryUpgradeBay 写 if(!ApplyStoredShape())throw——拦内部实现=逼它自爆。
        → 拦的层次：入口(TryHandleIngot)→收敛点(TryRestoreRememberedGridShape/Attach)→内部实现层(别碰)。

  16. 不要把对方私有状态表抄一份
        S8n：抄 XIAOWO LegacyShapes 会静默漂移。正确做法反射调它自己的 TryGetUnlockedShape；
        优先逐容器对齐，不动玩家全局 cfg。

  17. 不要在"每帧被调几万次的 getter"上挂方向未验证的拦截
        S8n 方案2(拦 widthPixels 的 4 个 Postfix)作废：真实挂点是 GameGridInventory/PixelWindow
        的 widthPixels/heightPixels(get;set; 真实存储属性)；先加只读日志确认还触不触发再动手。
        ⚠ 2026-10-09 更正（本条后句已被证伪）：**"先加只读日志"本身就危险**——只读探针挂了
        set_widthPixels/set_heightPixels（属性访问器）直接硬崩 0x80131506（详见 #26）。
        → 现在的判据不是"先加只读日志试试"，而是：**不要新挂任何 get_/set_ 访问器**（守卫 R4 强制）；
          要观测就改在自有调用点加日志。现存 4 个布局 getter 挂点（widthPixels/heightPixels）
          属历史 load-bearing（被 SetShapeKeepTransparency 依赖、不能加 gate），保留不动。

  18. HarmonyPatch 的目标方法要从 IL 里解出来，不要按方法名猜
        ilspycmd 显示 /*Could not decode attribute arguments.*/ 时，导出 --ilcode 从
        .custom HarmonyPatch::.ctor(...) 的 hex 字节解 UTF-8 字符串。

  19. 不要按源码目录名推断对方 DLL 的内部命名空间
        S8o：WagesPerks.dll 源码目录 JacksonPerks、namespace 也是 JacksonPerks，
        但部署 DLL 内部命名空间是 WagePerks。核对方类型一律以 ilspycmd -l class <dll> 为准。
        源码目录名/源码 namespace/MelonInfo 名/DLL 文件名，四个可以两两不同。

  20. 不要在只剩一套手柄时还保留"与对方对齐上限"的逻辑
        S8o：XIAOWO 手柄被封后，ClampToXiaowoCeiling 变成纯负担(读档回弹+拖小无法拖大)。
        → 引入"与对方对齐"前先问：对齐前提(双方都有手柄)是否还成立？

  21. EmporiumEntry.Validate 必须传命名常量 EmporiumForceValidate（2026-10-07 修订；旧版前提被推翻）
        · 旧版#21「无参=Validate(false)」是错的：签名 public void Validate(bool force=TRUE)，
          默认值即 true，源码 Validate() 的 IL 与 Validate(true) 完全相同。
        · 规则(R1a/R1b)：实参必须是命名常量 EmporiumForceValidate；禁裸布尔字面量；禁裸 Validate()
          （等于 true，易被误读成"轻量"）。两个调用点：ApplyAllResize 末尾、RefreshAllVisuals 末尾。
        · 现状值=true（保持既有行为）；若将来确由本参数引起窗口问题，改常量一处即可，启动日志打实际实参。
        · 通用：调 API 前先明确它改什么不改什么；不要把结论建立在"可选参数默认值"的猜测上。

  22. 不要在加载期（WorldReadyGate.ShouldDefer == true）对 GameGridInventory 执行
        SetShape / Expel / UncheckedAccept / ForceRecomputeDynamicBackground 等操作
        S8u：加载期走 ClearGrid→Expel→UncheckedAccept 全链路 + MonoMod 深栈 CompileMethodHook
        → Internal CLR error 0x80131506（托管 catch 抓不到）。
        → 所有增删物品/改形状/强制重算背景的代码必须先检查 ShouldDefer，加载期 return 或入队。
        → 分页/升级/恢复/拖拽缩放四类全适用。与 #6 同源（IL2CPP+MonoMod 加载深栈 JIT 冲突）。

  23. 改配置键名时，必须**连同迁移一起改**（不能只加前缀了事）
        2026-10-07 复核：CfgMigration.cs 自身硬编码了将被改名的键(DiagLog/IncW 等)，只改 CreateEntry
        而不改这些助手→旧版用户的合并静默停止生效。
        → 改 CreateEntry 键名一个都不能漏：KeyRenames(唯一事实源)+经 NewKeyOf() 翻译的 FindEntry/
          ApplyInc/MapLegacyKey+ShouldCreateWageEntries 新旧兼容+check-readme-defaults.ps1 硬编码
          W/H 键名+8 个 *Ids 不改名。与 #12 同源：改名的值搬运+三处摘旧键是维护，不受用户开关控制。

  24. 不要与 ContainerSuite 同时加载旧版独立 DLL / 同类扩容 mod（2026-10-07 补丁审计新增）
        MelonLoader 0.7.4 只加载 Mods\ 根目录的 DLL（不递归子目录）。
        · 旧版独立版 DIYUpgrade.dll/SaveBagExpand.dll（4-DLL 合并前产物）——与本套件同源同目标，最危险。
        · XIAOWOTradePerks.dll——与大量挂点重叠；套件有专用避让层但启用后必须重新实机验证。
        · BayExpand.dll（垃圾桶/文件箱）/FullMachineUpgrades.dll（机器升级）/AllLargeStorage.dll（箱子扩容）
          ——分别与 ValidateShapeState/EmporiumEntry.Start、EndDrag、LoadGame·SaveGame 撞。
        → 分发唯一 DLL=CustomInfrastructure.dll；旧版 4 个 DLL 必须删净。新增同类 mod 先跑冲突比对。

  25. 不要用 RuntimeHelpers.PrepareMethod 做 JIT 预热（Mode B 编译）
        0.46D 实机：WarmupJit 组5 对全部自有程序集 PrepareMethod→8ms 后 0x80131506（栈顶 CompileMethodHook），
        FailFast 托管 catch 抓不到。
        → 只允 Mode A（真实调用）预热；check-forbidden.ps1 的 R3 默认禁用一切 PrepareMethod，
          $prepAllow 仅记录人工复核。2026-10-09 组1/2/5 及取证开关已整块删，R3 现零豁免。

  26. 不要为了观测（哪怕是"只读取证"）去 Harmony 挂 IL2CPP 的热属性 getter/setter
        2026-10-09 实机：为量尺寸链铺只读探针，挂了 PixelWindow / GameGridInventory 的
        set_widthPixels / set_heightPixels（属性访问器）→ 所有 mod 加载完后进程硬崩
        （日志戛然而止、无 OnApplicationQuit），栈顶 CompileMethodHook → 0x80131506（托管 catch 抓不到）。
        · 与 #25 同源：都是 IL2CPP + MonoMod 的 CompileMethodHook FailFast。属"挂载点本身"的
          结构性风险，跟"读还是写"无关——只读探针照样崩。
        · 为什么热：widthPixels/heightPixels 是 auto-property（<widthPixels>k__BackingField），
          getter 是 `return this[+0x1C]` 的平凡体，游戏每帧读它数千次；IL2CPP 对这类平凡访问器
          detour 危险。对比：拾荒测试工程 TestResize.cs 只**读**这些属性、从不 Harmony 挂 → 从未崩。
        · 判据（避免重蹈）：要观测 hot 路径，只在**本 mod 自己的调用点**里加日志
          （如 ApplyWindowSize / Restore 内部），**不要**为了观察去挂游戏的属性访问器。
        · 守卫 R4：check-forbidden.ps1 默认拒绝一切按名挂 get_/set_ 的新 `[HarmonyPatch(typeof(X),"get_Y")]`，
          $hotAllow 精确列出已复核的 4 个布局 getter（PixelWindow / GameGridInventory 的
          widthPixels/heightPixels）——它们是 load-bearing（被 SetShapeKeepTransparency 依赖，
          见 #17），多个版本实机稳定，故保留；$hotAllow 仅记录复核状态，**不等于**放行新增热挂点。
        · 崩溃是潜伏型：注册成功、之后 JIT 才崩 → 编译通过 + 反编译核对都不能证明不崩，必须实机跑。

  27. 不要在"每帧超热的游戏方法"上挂 Harmony 补丁（含被第三方 mod 挂的——本 mod 自身要避开同罪）
        2026-10-10 十一轮读档/升级闪退定案：ProbablyOptimized 挂了 GameItem.ValidateShapeState
        （每帧被调的超热方法），导致全环境调用栈上几乎总有它的 trampoline 在活动；此时本 mod 的
        读档恢复链/升级链在启动后**首次执行**（嵌套 JIT：执行 A 时 A 内部首次编译 B）撞上 trampoline
        → CompileMethodHook 读空指针 → 0x80131506（托管 catch 抓不到、不写日志，但黑窗口控制台
        能打出完整托管栈——取证务必截图）。
        · 模型 = 「嵌套 JIT」×「栈上有 trampoline」的交集：本 mod 的嵌套 JIT 是"导火索"，
          外部超热补丁是"氧气"，缺一不崩（10-08 前无 ProbablyOptimized 不崩、移走后也不崩）。
        · 本 mod 自守两条：① 预热 Step 只允许"叶子方法"（null 早退且内部不再触发未编译方法）——
          invoke 大方法 = 嵌套自爆（10-37 实测 5 个升级链预热 Step 把启动搞崩）；大方法没有安全预热
          手段（PrepareMethod 已禁 #25），只能拆方法叶子化（TryRestoreRememberedGridShape 先例）或
          运行期自然编译。② 绝不为了优化/观测去挂 ValidateShapeState / GetCurrentValue 这类超热方法。
        · 玩家侧：若同时装本 mod 与"每帧视觉优化类"mod 出现读档/升级闪退，优先停用后者并向其作者
          反馈（超热方法不该挂 Harmony 补丁）。诊断开关 z2/z3/z4（[CustomInfrastructure] 段，默认 false）。

================================================================================
                                  文档结束
================================================================================
