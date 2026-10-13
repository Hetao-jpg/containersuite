━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
容器套件 · ContainerSuite
作者：核桃 | 版本：1.7.8
游戏：Probably Stolen 0.46D
环境：MelonLoader 0.7.4+
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

▎安装
把 CustomInfrastructure.dll 放进
游戏目录 Mods/ 文件夹，启动即可。

⚠️ 从 v1.7.2 或更早升级：
先删旧的 4 个 DLL，只留新的。
⚠️ 不要改名，不要留副本。

▎功能
· 容器扩容（主仓库/展示柜/
  海报暗格/柜台/地面）
· 主仓库 TAB 分页
  （拖着物品时也能按 TAB）
· 滚轮滚动
· LeftAlt 拖金属锭升级
· 拖边改尺寸
· 地面可改大小/拖动
· 读档恢复
· 垃圾桶/档案箱/磁带播放器扩容
· 容器升级的名单/尺寸可自己配
· XIAOWO/蛙哥 避让

▎热键
LeftAlt+拖材料 升级
TAB 切页（可拖着物品按）
滚轮 滚动
拖边缘 改大小

搬物品到第 2 页：
拿起 → TAB → 松手，直接落新页。
堆叠物品只搬你拿起的那几件，
剩下的留在原格；切页不打乱数量。
按 TAB 不切页：框选一批在拖时 /
只是左键按着（拖窗口、改尺寸）。

▎页归属是即时存的（和存档无关）
物品在第几页记在
UserData\SaveBagPaging.ids，
切页那一刻就写盘，不用存档。
所以搬到第 2 页后即使不存档，
退主菜单再读同一档，物品仍在
第 2 页（正常，不是丢档）。
想让某件回第 1 页：读档后自己搬，
或关游戏→先备份→删掉那文件里
对应存档槽的那一行。
同槽开新游戏不继承上一局页归属。

▎配置
MelonPreferences.cfg 的
[CustomInfrastructure] 段。

常用：
0_EnableMod 总开关（开）
2_ExpandEnabled 扩容（开）
3_SaveBagPagingEnabled 分页（开）
3A_SaveBagPageCount 页数（2）
4_ScrollEnabled 滚轮（开）
1E_EnableDragResize 拖边改尺寸（开）
1F_RestoreOnLoad 读档恢复（开）
1D_UpgradeHotkey 升级键（LeftAlt）
z1_DiagLog 诊断日志（关）

避让：
6A_SkipXiaoBay XIAOWO（true）
6B_SkipWageBox 蛙哥箱（true）
6C_SkipVoidBead 虚空珠（true=蛙哥）

▎容器升级的名单与上限
都在同一个 [CustomInfrastructure]
段里（没有第二个配置文件）。

① 7x_ 分类上限（面板可改）
7A_箱 7B_背包 7C_笼
7D_多槽 7E_模组槽
7F_Mod容器 7G_妙妙箱 7H_虚空珠
每类：Enable / MaxW / MaxH

② <类别>Ids 名单（手改）
逗号分隔的物品 id。
加一个：id 加到行尾前补逗号。
删一个：把 id 从那行删掉。
⚠ 留空 = 这类都不能升级。

③ 8A_PerIdSizeLimits 单件上限
（最细，手改 cfg）
8A_PerIdSizeLimits =
 "storage_bay=30x20,evidence_box=24x24"
· 写了就完全覆盖它的分类上限
· 没写的用分类上限，1~100
· ⚠ 面板里看不到（字符串项）
· ⚠ 改太小装不下现有物品时
  不生效，日志会点名提示

配没配上：看启动日志那行
「单独设了上限的容器共 N 件：…」

▎重要限制
分页第 2 页原版检测不到：
· 机器过夜不工作
· 治安官查不出赃物
· 博士可能重复购买

规避：识别物放第 1 页，
或关分页重启。

▎注意
· 删 DLL = 全部失效
· 不要改文件名
· 先备份存档
· 虚空珠移交不可逆
· 充能器只显示前 4 颗电池
· 名单与上限只在
  [CustomInfrastructure] 一处维护
  （已取消 containers.txt）

▎兼容
已避让：XIAOWO / 蛙哥

勿同用：
旧版 DIYUpgrade.dll /
SaveBagExpand.dll /
ContainerSuite.Shared.dll /
BayExpand.dll /
FullMachineUpgrades.dll /
AllLargeStorage.dll /
InventoryPages.dll

▎日志
MelonLoader/Latest.log
搜 [CustomInfrastructure]

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
反馈：QQ群 @夏月的核桃
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
