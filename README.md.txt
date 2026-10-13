容器套件 · ContainerSuite
作者：核桃
版本：1.7.8
游戏：Probably Stolen 0.46D
环境：MelonLoader 0.7.4+

安装
把 CustomInfrastructure.dll 放进游戏目录 Mods/ 文件夹，启动即可。

⚠️ 从 v1.7.2 或更早升级：先删除旧的 4 个 DLL
（CustomInfrastructure.dll / SaveBagExpand.dll / DIYUpgrade.dll / ContainerSuite.Shared.dll），
只保留新的 CustomInfrastructure.dll。
⚠️ 不要改 DLL 文件名，不要留副本。

功能
- 容器扩容：主仓库 / 展示柜 / 海报暗格 / 柜台 / 客户柜台 / 地面
- 主仓库分页：按 TAB 切页（**拖着物品时也能切**：拿起 → TAB → 松手，物品直接落到新页）
- 滚轮滚动：鼠标悬停容器上滚轮浏览
- 容器升级：按住 LeftAlt 拖金属锭到容器上
- 升级名单/尺寸可配：哪些物品能升级、每个容器最大能多大，都能改（详见下文「配置」）
- 拖边改尺寸：拖窗口边缘调整大小（需先升级过）
- 地面窗口：可改大小（2F）/ 鼠标拖动（2G）
- 读档恢复：已升级容器尺寸自动恢复
- 基础设施扩容：垃圾桶 / 档案箱 / 磁带播放器
- Mod 避让：内置对 XIAOWO / 蛙哥的兼容开关

热键
LeftAlt + 拖材料  升级容器
TAB               主仓库切页（拖着物品时也能按）
鼠标滚轮          容器内滚动
拖窗口边缘        调整大小（需升级过）

把物品搬到第 2 页（不用"放下→切页→再拿起"）
1) 左键按住物品拿起来（堆叠物品可先滚轮调数量）
2) 按 TAB 切页 —— 绿色落点标记照常跟手，物品图跟着鼠标不变
3) 在新页看好位置松手，物品直接落在那一页

堆叠物品：搬走的只是你拿起的数量，剩下的仍留在原格；切页不会打乱
数量或格上的数字。
按 TAB 不会切页的两种情况（有意为之）：
- 框选一批物品正在拖（那批物品的选中集会被换页整批搬走，会出错）
- 只是左键按着（拖窗口 / 拖容器边缘改尺寸），这时并不是在拖物品

分页归属"即时生效、即时记录"，与游戏存档无关
把物品放在第几页，记在 UserData\SaveBagPaging.ids（按存档槽分别记，同一槽开新游戏另起一行）。
它在**切页那一刻就写盘**，不用等你存档。所以：

· 把第 1 页的物品搬到第 2 页之后，**即使不存档**，退出到主菜单再读同一个档，
  读档后那件物品**仍然在第 2 页**（不会回到第 1 页）——这是正常行为，不是丢档。
· 游戏存档里只记"物品本身和它在网格里的位置"；"在第几页"由上面那个文件决定。
· 想让某件东西回到第 1 页：读档后自己搬回去；或关掉游戏 → 先备份 →
  删掉 UserData\SaveBagPaging.ids 里对应存档槽的那一行（形如 270#…;2;19），
  再进游戏它就会归到第 1 页。
· 同一个存档槽**开新游戏**（新的一局）不会继承上一局的页归属。

配置
在 UserData/MelonPreferences.cfg 的 [CustomInfrastructure] 段。

常用：
0_EnableMod              总开关（开）
2_ExpandEnabled          仓库扩容（开）
3_SaveBagPagingEnabled   主仓库分页（开）
3A_SaveBagPageCount      分页页数（2）
4_ScrollEnabled          滚轮（开）
1E_EnableDragResize      拖边改尺寸（开）
1F_RestoreOnLoad         读档恢复（开）
2F_AfterhourWidth/Height 地面宽高（12×9）
2G_GroundDragEnabled     地面拖动（开）
1D_UpgradeHotkey         升级键（LeftAlt）
z1_DiagLog               诊断日志（关）

避让开关（装 XIAOWO / 蛙哥时）：
6A_SkipXiaoBay   XIAOWO 货舱（true = 本 mod 管）
6B_SkipWageBox   蛙哥妙妙箱（true = 本 mod 管）
6C_SkipVoidBead  虚空珠（true = 归蛙哥；false = 本 mod 接管）

改完保存重启生效（避让开关同帧生效）。

容器升级的名单与上限（三层，由粗到细）
全部在同一个文件里：UserData/MelonPreferences.cfg 的 [CustomInfrastructure] 段。
★ 没有第二个配置文件 —— 2026-10-13 起取消了 UserData\ContainerSuite\containers.txt，
  改这里就是改全部，不用纠结"到底哪个生效"。

第一层 7x_ 分类上限（最常用，面板里能改）
7A_Box / 7B_Backpack / 7C_Cage / 7D_MultiSlot / 7E_MultiSlotModule
7F_ModContainer / 7G_WageBox / 7H_WageBackpack
每类 3~4 条：Enable（这一类允许升级吗）/ MaxW（最大宽）/ MaxH（最大高）
              / AlignSlots（多槽容器各槽是否同尺寸）

第二层 <类别>Ids 升级名单（手改 cfg）
BoxIds / BackpackIds / CageIds / MultiSlotIds / MultiSlotModuleIds …
· 逗号分隔的物品 id。想加一个容器：把它的 id 加到对应那一行末尾，前面补逗号。
· 想去掉一个：把它的 id 从那一行删掉。⚠ 留空 = 这一类全部不能升级。
· 每条里面出现过的类别，都已由代码自动补齐新容器（只加不删，日志会说补了什么）。

第三层 8A_PerIdSizeLimits 单个容器上限（最细，手改 cfg）
写成一行：8A_PerIdSizeLimits = "storage_bay=30x20, evidence_box=24x24"
· 一个 `容器id=宽x高`，多个用逗号隔开；数字 1~100。
· 写了就**完全覆盖**它那一类的 7x_ 上限（可以改大，也可以改小）。
· 没写的容器照样用第一层。
· ⚠ 改得太小、装不下容器里现有的东西时，这次改动不生效，日志里会有一条点名提示。
· ⚠ 它是字符串项，ModManager 面板里**看不到**（面板只显示开关/数字/热键），只能手改 cfg。

怎么知道我配没配上：启动后看游戏日志（MelonLoader/Latest.log），会有一行
「[容器升级] 单独设了上限的容器共 N 件：storage_bay=30x20（储藏区）…」
一条都没配就是「没有为单个容器单独设上限 → 全部使用「容器升级·分类」里的上限」。

改完生效时机
· 面板（ModManager）里改：点「应用配置」后同帧生效，不用重启。
· 直接编辑 cfg：保存后重启游戏生效（面板方式推荐）。

重要限制
主仓库分页第 2 页及以后，游戏原版检测不到：
- 机器过夜不工作
- 治安官查不出赃物/违禁品
- 博士可能重复购买

规避：需要被识别的物品放第 1 页，或关闭分页 3_SaveBagPagingEnabled = false。
这不是丢档，是原版检测范围限制。

注意事项
- 只有一个 DLL，删了全部失效；想关功能改 cfg，不要删 DLL
- 不要改 DLL 文件名
- 第一次使用前建议备份存档
- 虚空珠移交本 mod 不可逆
- 充能器超过 4 格只显示前 4 颗电池贴图，容量不受影响
- 分页总件数超出单网格容量时，存档会红字警告，明细看 UserData\SaveBagPaging.mergeloss.txt
- 容器升级的名单与上限只在 [CustomInfrastructure] 段一处维护，不要再去找 containers.txt
  （2026-10-13 起该功能已取消；留着的旧文件不生效，可以删掉）

已知问题：改了 cfg 重启后不生效 / 退出后配置丢失
多半是 MelonPreferences.cfg 里有非法 TOML（例：值里内层双引号未转义 `A = "{ x = "F8" }"`、
裸值含逗号 `A = a,b,c`）。MelonLoader 解析失败后整份 cfg 进入 Fallback，此后**所有 mod**
的保存都会被静默丢弃，玩家在 ModManager 里改的配置退出后全部不生效——而启动日志里只有一行
ERROR，极易被淹没。排查：用文本编辑器逐段检查该文件，重点是别家 mod 写的段（本套件 2026-10-13
起不再内置自检探针）。

兼容性
已避让：XIAOWOTradePerks / WagePerks（蛙哥）

请勿同时使用：
- 旧版 DIYUpgrade.dll / SaveBagExpand.dll / ContainerSuite.Shared.dll
- BayExpand.dll / FullMachineUpgrades.dll / AllLargeStorage.dll
- InventoryPages.dll（与分页冲突）

看日志
MelonLoader/Latest.log，搜 [CustomInfrastructure]
