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
· XIAOWO/蛙哥 避让
· cfg TOML 自检

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

▎配置
MelonPreferences.cfg 的
[CustomInfrastructure] 段。

常用：
0_EnableMod 总开关（开）
7_ExpandEnabled 扩容（开）
8_SaveBagPagingEnabled 分页（开）
8A_SaveBagPageCount 页数（2）
9_ScrollEnabled 滚轮（开）
22_EnableDragResize 拖边改尺寸（开）
23_RestoreOnLoad 读档恢复（开）
21_UpgradeHotkey 升级键（LeftAlt）
z1_DiagLog 诊断日志（关）

避让：
3A_SkipXiaoBay XIAOWO（true）
3B_SkipWageBox 蛙哥箱（true）
3C_SkipVoidBead 虚空珠（true=蛙哥）

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
