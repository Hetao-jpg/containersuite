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
- 拖边改尺寸：拖窗口边缘调整大小（需先升级过）
- 地面窗口：可改大小（7F）/ 鼠标拖动（7G）
- 读档恢复：已升级容器尺寸自动恢复
- 基础设施扩容：垃圾桶 / 档案箱 / 磁带播放器
- Mod 避让：内置对 XIAOWO / 蛙哥的兼容开关
- TOML 自检：启动后检查 cfg 合法性

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
7_ExpandEnabled          仓库扩容（开）
8_SaveBagPagingEnabled   主仓库分页（开）
8A_SaveBagPageCount      分页页数（2）
9_ScrollEnabled          滚轮（开）
22_EnableDragResize      拖边改尺寸（开）
23_RestoreOnLoad         读档恢复（开）
7F_AfterhourWidth/Height 地面宽高（12×9）
7G_GroundDragEnabled     地面拖动（开）
21_UpgradeHotkey         升级键（LeftAlt）
z1_DiagLog               诊断日志（关）

避让开关（装 XIAOWO / 蛙哥时）：
3A_SkipXiaoBay   XIAOWO 货舱（true = 本 mod 管）
3B_SkipWageBox   蛙哥妙妙箱（true = 本 mod 管）
3C_SkipVoidBead  虚空珠（true = 归蛙哥；false = 本 mod 接管）

改完保存重启生效（避让开关同帧生效）。

容器升级的白名单与尺寸上限
在 UserData\ContainerSuite\containers.txt（首次启动自动生成，文件内带格式说明）。
- [whitelist] 哪些物品算"可升级容器"（按类别，逗号分隔 id）
- [limits]    每个容器 id 单独的宽×高上限（只写要单独设的，其余用类别上限；可增可减）
★ 这个文件**存盘后 2 秒内自动生效，不用重启**；被它声明过的类别以文件为准，
  MelonPreferences.cfg 里对应的 *Ids 不再生效。想恢复默认就删掉那一行（不是留空）。

重要限制
主仓库分页第 2 页及以后，游戏原版检测不到：
- 机器过夜不工作
- 治安官查不出赃物/违禁品
- 博士可能重复购买

规避：需要被识别的物品放第 1 页，或关闭分页 8_SaveBagPagingEnabled = false。
这不是丢档，是原版检测范围限制。

注意事项
- 只有一个 DLL，删了全部失效；想关功能改 cfg，不要删 DLL
- 不要改 DLL 文件名
- 第一次使用前建议备份存档
- 虚空珠移交本 mod 不可逆
- 充能器超过 4 格只显示前 4 颗电池贴图，容量不受影响
- 分页总件数超出单网格容量时，存档会红字警告，明细看 UserData\SaveBagPaging.mergeloss.txt

兼容性
已避让：XIAOWOTradePerks / WagePerks（蛙哥）

请勿同时使用：
- 旧版 DIYUpgrade.dll / SaveBagExpand.dll / ContainerSuite.Shared.dll
- BayExpand.dll / FullMachineUpgrades.dll / AllLargeStorage.dll
- InventoryPages.dll（与分页冲突）

看日志
MelonLoader/Latest.log，搜 [CustomInfrastructure]
