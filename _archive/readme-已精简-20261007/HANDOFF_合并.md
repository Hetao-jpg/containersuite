# HANDOFF · ContainerSuite 4 DLL → 1 DLL 合并（跨对话交接）

> 下次对话**开局先读这个文件**，按「起手动作」直接续做，不要重新做可行性分析。
> 本文件由上一段会话自动维护；每完成一步就更新「已完成 / 剩余」两张表。

- 工作区（open，主战场）：`D:\games\open code work`
- 邻居（work，只做备份，别改）：`D:\games\mod调试`
- 目标游戏：Probably Stolen Demo，`D:\SteamLibrary\steamapps\common\Probably Stolen Demo`
- 日期标记：**2026-10-06**；版本 **Suite 1.7.3**
- 用户语言：中文；所有回复和文件用中文

---

## 0. 环境铁律（先看，违反会浪费大量轮次）

1. **每条消息最多 1 个 `dnspy_*` 调用**。多个会 JSON 拼接报错并死循环。
   `read` / `edit` / `bash` / `glob` 可以并行。
2. PowerShell **5.1**：脚本开头先 `[Console]::OutputEncoding=[Text.Encoding]::UTF8`；
   禁用 `&&`（用 `; if ($?) { }` 或 `cmd1; cmd2`）；中文路径用 `-LiteralPath`；
   `Move-Item` 在部分路径下报 DirectoryNotFound，改用 `Rename-Item -NewName`。
3. **编码混杂**：`ContainerSuite.Infrastructure\AssemblyInfo.cs` 原为 GBK（现已重写为 UTF-8）；
   多数 `.cs` 是 UTF-8 无 BOM；文档是 UTF-8（`README.md` 按 GBK 读会乱码）；
   部分文件用 `Get-Content` 读会乱码 → **编辑前必须先用 Read 工具读，oldString 以 Read 输出为准**。
4. .NET SDK：6.0.428 / 9.0.318 / 10.0.401；MelonLoader 0.7.4（Demo）/ 0.7.3（Playtest）。
5. **验证 DLL 内字符串**：.NET 字面量在 US 堆里是 **UTF-16**，标识符才是 UTF-8。
   要同时用 `Unicode.GetString`（且试 offset 0 和 1 两种对齐）+ `UTF8.GetString` 判定，
   否则会误报 MISSING（本会话已踩过）。

### 🔴 既有红线（不可违背，改了就回归）

- `SaveBagPaging.cs` 里 WarmupJit / 反射调用是规避 CoreCLR JIT `0x80131506` 致命错
  （`localloc` 内联进带 EH 方法）的手段，**禁止改成直调**。
- `CG_Scan` 过滤 identifier 为空的物品，**不可用 `ExpelAll()` 替换**。
- `ContainerSuite.Infrastructure\AssemblyInfo.cs` 是唯一允许写 `[assembly: ...]` 的地方，
  再加一份会 CS0579。
- 三道构建守卫（任一失败 = MSB3073 构建失败）：
  `P4 check-suite-version.ps1` / `DOC check-readme-defaults.ps1` / `FBD check-forbidden.ps1`。

---

## 1. 任务背景与用户已拍板的决定

**主任务**：把 4 个 DLL 合并成 1 个（方式 A 源码合并），用户已明确授权开工。
**B2 方案（用户 question 答复选定）**：三个 MelonPreferences 段统一并入
`[CustomInfrastructure]`，`DiagLog` 撞名分别改成
`SaveBag_DiagLog`（SBE）/ `DIY_DiagLog`（DIY），CI 保留 `DiagLog`。

合并必须连带做完的三件事：

| 编号 | 内容 | 不做的后果 |
|---|---|---|
| B3 | 4 份 AssemblyInfo.cs 合成 1 份 | 编译期 CS0579 重复特性 |
| B1 | 按程序集扫补丁 → 按命名空间过滤 | 同一目标被两个 Harmony ID 各装一遍，Postfix 跑两遍，Patch 计数虚高 |
| B2 | 3 个 cfg 段并成 1 个 | ModManager 归属规则只看 DLL 文件名，41 项里 31 项从面板消失 |

ModManager 归属规则依据：`mod管理器\ModManager\ManagerState.cs:198 CategoryBelongsToMod`
= 段名 normalize 后 == `BuiltInName` 或 == 文件名去扩展；`BuiltInName` 来自
`ModScanner.cs:121-125` = **DLL 文件名**。
`PreferencesStore.IsObsoleteCategory` 认 `MelonPreferences.Categories`
（运行时注册的段）+ `IsReferencedByInstalledMods` 兜底 → 自动清理**不会删**我们的段。

---

## 2. 已完成（✅ 不要重做）

### 备份
- ✅ `D:\games\mod调试\_backup_ContainerSuite_合并前_20261006\`，156 文件 / 3521.9 KB，抽查全 True。

### B3 — AssemblyInfo 合一
- ✅ `ContainerSuite.Infrastructure\AssemblyInfo.cs` 重写为唯一声明
  （MelonInfo `CustomInfrastructure` / 1.7.3 / 核桃、MelonGame、Title/Product=ContainerSuite、
  AssemblyVersion/FileVersion/InformationalVersion=1.7.3(.0)、Guid a1b2c3d4-…），UTF-8。
- ✅ 另 3 份改名 `AssemblyInfo.cs.disabled`。

### B1 — 补丁过滤
- ✅ `ContainerSuite.SaveBag\mod.cs` `ApplyPatches`：按命名空间 `SaveBagExpand` 过滤 +
  `GetCustomAttributes(typeof(HarmonyPatch),false)` + `CreateClassProcessor(type).Patch()`，
  新增日志 `SBE 补丁安装: 成功 N 个`（原 `harmony.PatchAll(...SaveBagExpandCore.Assembly)`）。
- ✅ `DiyUpgradeMod.Core.cs` `InstallPatches`：循环内加 `type.Namespace.StartsWith("DIYUpgrade")`。
- ✅ 核实：**DIY 15 个 / SaveBagExpand 6 个**补丁类；`CustomInfrastructure` 命名空间下**无** `[HarmonyPatch]`。
  CI 侧 `ContainerExpandPatch.Apply` 是显式 `harmony.Patch` ×6，不受影响。

### csproj / sln / 脚本
- ✅ `ContainerSuite.Infrastructure\ContainerSuite.Infrastructure.csproj`：AssemblyName=CustomInfrastructure、
  RootNamespace=CustomInfrastructure、GenerateAssemblyInfo=false、
  `<Compile Include="..\ContainerSuite.Shared|SaveBag|DIYUpgrade\**\*.cs" Exclude="...bin\**;...obj\**">`、
  14 个引用并集全部 `Private=False`、头部 30 行注释记录 B1/B2/B3。
- ✅ 另 3 个 csproj → `*.csproj.disabled`；`ContainerSuite.sln` 重写为单项目。
- ✅ 删除 Shared/SaveBag/DIYUpgrade 的 `bin`/`obj`。
- ✅ `_shared\check-suite-version.ps1`：`$assemblyInfos` 只留 Infrastructure 一行，提示改
  `update SuiteVersion.cs and AssemblyInfo.cs together.`。
- ✅ `pack.ps1`：`$dlls` 只留 `CustomInfrastructure.dll` 一条。

### B2 — 配置段合并（代码已写完）
- ✅ 新建 `ContainerSuite.Shared\MelonPrefsUtil.cs`：
  `SuiteCategoryId="CustomInfrastructure"` + `GetOrCreate(id, displayName)`
  （先遍历 `MelonPreferences.Categories` 复用实例，避免同名段注册两次）。
- ✅ `CustomInfrastructureConfig.cs:34` → `GetOrCreate(SuiteCategoryId, "自定义基础设施大小")`。
- ✅ `SaveBagExpandConfig.cs`：段 → `GetOrCreate(...)`；`DiagLog` → **`SaveBag_DiagLog`**（含注释）。
- ✅ `DiyUpgradeMod.Core.cs`：段 → `GetOrCreate(...)`；`_diagLog` → **`DIY_DiagLog`**。
- ✅ 整体重写 `ContainerSuite.DIYUpgrade\DiyUpgrade.CfgMigration.cs`：
  三段通用迁移（`自定义容器升级` / `DIYUpgrade` / `SaveBagExpand` → `CustomInfrastructure`，
  `LegacySectionsInOrder` 旧→新覆盖）；标记文件 `ContainerSuite.merged20261006`；
  `_legacyValues = Dictionary<section, Dictionary<key,raw>>`；
  `MapLegacyKey`（DiagLog→SaveBag_DiagLog/DIY_DiagLog）；`FindEntry`；
  `SnapshotLegacySection`（`_snapshotTaken` 防重入）；
  `ApplyLegacySnapshot`（moved=0 且有 legacy → **报错不删段不写标记**；逐段 SectionExists→RemoveSection）；
  `CfgSectionHasKey` 增加 合并段+3 旧段+内存快照 多路回退；保留原 `ApplyRawValue/ReadSectionValues/SectionExists/RemoveSection`。
- ✅ `DiyUpgradeMod.Core.cs`：Init 开头 `SnapshotLegacySection()` 调用已删除（改为说明注释）；
  `ApplyLegacySnapshot(val)` 注释改为三段→一段、强调必须在 Wage 注册之后。
- ✅ `ContainerSuite.Infrastructure\Mod.cs` `OnInitializeMelon`：**任何 Init 之前**
  `try { DIYUpgrade.DiyUpgradeMod.SnapshotLegacySection(); } catch ...`
  （原因：CI `CustomInfrastructureConfig.cs:90`、SBE `SaveBagExpandConfig.cs:111`、
  DIY `Core.cs:72` 的 `SaveToFile()` 都会重写整个 cfg 抹掉未注册旧段）。
- ✅ `SaveBagPaging.cs` WarmupJit `mine` 数组 → `{"CustomInfrastructure"}`，
  Group 标题 `预热四程序集…` → `预热全部方法…`，注释补了合并说明。

### 文档
- ✅ **2026-10-06 已按受众拆分**（原 README.md 1714 行 → 三份）：
  - `readme.用户须知.md`（610 行）：概要 / 二、安装与部署 / 三、配置与热键 /
    四、各组件要点（含默认尺寸表 L280）/ 五、已知问题与限制 / 六、排障
  - `readme.mod维护日志.md`（1129 行）：一、四个工程 / 构建与产物 /
    七、维护日志（倒序）/ 八、禁止事项
  - `README.md`（23 行）：只做索引，指向上面两份 + 快速入口
  - 拆分前原文件备份在 `拆分前备份_README_20261006.md`（注意文件名不以
    `readme` 开头，否则会被 DOC 守卫当正文扫到）
  - 章节编号**保持原样**（用户端从「二」开始、开发端有「七」「八」），
    这样所有「见第三节」「README 第五节」「禁止事项 #N」的交叉引用都还成立。
  - `_shared\check-readme-defaults.ps1` 改为扫根目录 `readme*.md`
    （并按文件名含 `mod` 跳过维护日志，它会引用旧尺寸表举例），
    现在报 `DOC: default-size check passed (readme.用户须知.md line 280)`。
- ✅ `README.md` 此前已完成的改动（首页一句话、工程列表 + 「为什么合并成 1 个 DLL」
  完整 B1/B2/B3 说明、P4 守卫对象 5→2 处、AssemblyInfo 编码说明、产物 1 个、部署 + 删残留、
  自查 ④、pack、回滚、打包 zip、残留 DLL 说明、只关一部分功能 + 配置段迁移说明、
  从旧版升级、排障表、`cfg [CustomInfrastructure]` 段名 ×4、构建路径改成本工作区、
  P4 条目补「2026-10-06 合并后 2 处」）——现已随拆分分布到上述两份文件里。

### 构建 / 打包（全绿）
- ✅ `dotnet build ContainerSuite.sln -c Release` → **0 警告 0 错误**，
  P4 / DOC（line 349）/ FBD（40 files）全过，产出唯一
  `ContainerSuite.Infrastructure\bin\Release\CustomInfrastructure.dll`。
- ✅ DLL 内字符串核对通过（UTF-16 双对齐 + UTF-8）：
  `SaveBag_DiagLog`、`DIY_DiagLog`、`ContainerSuite.merged20261006`、`自定义基础设施大小`、
  `MelonPrefsUtil`、`GetOrCreate`、`配置合并`、`SaveBagExpand`、`DIYUpgrade`、
  `Trashcan_Enable`、`WageBoxEnable` 全部在。
- ✅ `pack.ps1 -NoZip` 成功：`dist\ContainerSuite-v1.7.3\` 只有
  `CustomInfrastructure.dll`（198144 字节）+ `install-guide.txt`。

---

## 3. 剩余（按顺序做）—— **N1/N2 已完成，本任务基本收尾**

> 状态（2026-10-06 23:26）：4 DLL → 1 DLL 合并改造**已全绿实机验证**。
> 下面只剩可选项和一次肉眼确认。

### N1 · 实机部署 ✅ 已完成（23:23 二次部署）
- 旧 4 DLL 已备份到 `Mods\_合并前_4DLL_20261006.bak\`（CustomInfrastructure 23552 /
  SaveBagExpand 61440 / DIYUpgrade 111616 / ContainerSuite.Shared 25088）。
- `Mods\` 里套件 DLL **只剩 `CustomInfrastructure.dll`（当前 199168 字节，
  含 `DropLegacyCategoriesFromMelon` 修复）**；3 个旧 DLL 已删，无残留；
  `ModManager-1.2.5.dll` 未动。
- 部署前确认游戏进程未运行。回滚 = 把 `_合并前_4DLL_20261006.bak\` 的 4 个放回去。

### N2 · 实机回归判据 ✅ 已验证（2026-10-06 23:24 第二轮启动）

**结论：全部通过，合并改造可视为完成。**

- `verify-log.ps1` → **ALL PASS（826 行，C1–C7 全绿）**
- `SBE 补丁安装: 成功 6 个` ✓  `Patch 安装: 成功 15 个` ✓（**B1 判据**）
- `[配置合并] 已迁移 101 项到 [CustomInfrastructure]` ✓（**B2 判据**）
- cfg 现状：**只剩 `[CustomInfrastructure] 111 键`**，三个旧段全部消失，标记文件已写 ✓
- 值抽查全对：`SaveBagWidth=24 / SaveBagHeight=16 / BackInvWidth=18 / WageBoxMaxW=52 /
  BoxIds / UpgradeHotkey="LeftAlt" / SaveBag_DiagLog=false / DIY_DiagLog=false /
  SkipWageBox=true / SaveBagPageCount=2`；11 个 `Slot*_Page2Ids`（全为 ""的死键）随旧段清掉
- 之后又发生过一次 SaveToFile（cfg mtime 23:25:06 > 迁移 23:24:57）**旧段没被写回来**
  → `DropLegacyCategoriesFromMelon()` 生效
- XIAOWO 7/7、Wage 14/14、`prepared=1429 skipped=13 耗时=131ms`、无补丁挂载失败、无 ERROR
- 旧一轮（23:03）同样 C1–C7 全 PASS，只是旧段当时被写回（已修）

**唯一没验的**：ModManager 面板肉眼看一眼（工具验不了 UI）——
应只看到 `CustomInfrastructure` 一个套件 mod 下挂 111 项配置，
不再有 `[SaveBagExpand]` / `[DIYUpgrade]` 两行幽灵段，也没有 3 条幽灵 mod。

### 本轮踩到的坑（写代码时照抄）

**只删 cfg 文件里的旧段没用。** MelonLoader 的 `MelonPreferences.DefaultFile`
文档里旧段还在，之后任何一次 `SaveToFile`（自己的、别的 mod 的）都会原样写回
——实测迁移完成 17ms 后就有一条 `MelonPreferences Saved` 把两段写了回来。
正确收尾 = **同时摘两处**（技法抄自 `ModManager\PreferencesStore.
RemoveCategoryFromMelonDocument`）：

1. `MelonPreferences.Categories`（`List<>` 时可 `RemoveAt`；ModManager 面板遍历的就是它）
2. `typeof(MelonPreferences).GetField("DefaultFile", Static|Public|NonPublic)` →
   取文档对象 → 反射调它的 `RemoveCategoryFromDocument(string)`

实现见 `ContainerSuite.DIYUpgrade\DiyUpgrade.CfgMigration.cs` 的
`DropLegacyCategoriesFromMelon()`；标记文件存在时也会每次开机复查摘除。

**另**：dnSpy 工具对 `MelonLoader.dll` 全线报
`'/' is an invalid start of a property name`（decompile_method / decompile_type /
find_callers 都一样），且 `token` / `method_token` 这类整数参数传不进去。
别再在 MelonLoader 程序集上耗轮次，改用反射或读 ModManager 源码。

判据速查（2026-10-06 23:24 实测**全绿**，留作以后回归的基线）：

| 判据 | 实测值 |
|---|---|
| C1 横幅 | `ContainerSuite v1.7.3` |
| C2 分页 | `[SaveBagExpand] 分页已初始化：2 页` |
| B1 补丁数 | `SBE 补丁安装: 成功 6 个` / `Patch 安装: 成功 15 个` |
| B2 迁移 | `[配置合并] 已迁移 101 项`，旧 3 段消失、标记文件已写 |
| C3 WarmupJit | `prepared=1429 skipped=13 耗时=131ms` |
| C4 | `已避让 XIAOWOTradePerks (7/7)` |
| C5 | `Wage 避让：14/14 成功` |
| C6 / C7 | clean / clean |
| cfg | 只剩 `[CustomInfrastructure] 111 键` |

复跑命令：`powershell -NoProfile -ExecutionPolicy Bypass -File .\verify-log.ps1`
（在工作区根目录；退出码 0 = ALL PASS）。

### N3 · 遗留清理（可选）
- `verify-log.ps1`：补 `SBE 补丁安装: 成功 6 个` / `Patch 安装: 成功 15 个` /
  `[配置合并]` 三条新断言（现在只断言 C1–C7）。
- 开发端历史变更日志里 1101 / 1450 行仍写“4 个 AssemblyInfo”——属历史记录，未改
  （现在在 `readme.mod维护日志.md` 里）。
- 3 个 `*.csproj.disabled` / `*.AssemblyInfo.cs.disabled` 保留（比删除安全）。
- 若要回滚整个改造：备份在 `D:\games\mod调试\_backup_ContainerSuite_合并前_20261006\`。

---

## 4. 下次对话怎么接

**合并任务本身已结束**（N1 部署 ✅ / N2 验证 ✅）。下次对话按需要选：

1. 读本文件（`HANDOFF_合并.md`）。
2. **可选 A — 肉眼确认 ModManager 面板**：应只有 `CustomInfrastructure`
   一个套件 mod 下挂 111 项配置，无幽灵段、无 3 条幽灵 mod。
3. **可选 B — 补 verify-log 断言**：把 `SBE 补丁安装: 成功 6 个` /
   `Patch 安装: 成功 15 个` / `[配置合并]` 三条加进 C8。
4. **可选 C — 重新打包发布**：`.\pack.ps1`（会重建 + 产出 dist），
   然后把 dist 里的 DLL 覆盖到 Mods\。
5. 想继续做别的：回到 `D:\games\open code work\优化建议.txt`
   （P0/P1/P2/P3 优化清单），或 `C:\Users\msi\Desktop\mod开发指南.md`。
