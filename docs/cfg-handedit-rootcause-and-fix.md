# cfg 手改被「默认值覆盖」——根因分析与优化方案

> 2026-10-14 · 对象：ContainerSuite（分析时 v1.7.9 → **方案已于同日实施为 v1.8.0**，见文末「七、实施记录」）
> 方法：反编译本机 `MelonLoader/net6/MelonLoader.dll`（`MelonPreferences` / `MelonPreferences_Category` /
> `Preferences.IO.File` / `Preferences.IO.Watcher` / `MelonPreferences_Entry<T>`）+ 用本机 Tomlet 跑 14 组
> 手写写法用例（工程在 `_probe/tomltest`）+ 读真实 `UserData\MelonPreferences.cfg` 与启动日志时序。
> **未跑游戏、未改任何 mod 源码（本节撰写时）。**

---

## 0. 结论速览

用户反馈成立。它**不是 ContainerSuite 独有 bug，而是 MelonLoader 0.7.4 的 cfg 读取语义
（读不懂 = 静默回落默认值）与"本套件每次启动全量重写 cfg"叠加的必然结果**。

按命中率排序的三个真实成因：

| # | 成因 | 玩家看到的现象 | 证据 |
|---|---|---|---|
| **1** | 写法不规范但仍是**合法 TOML**：数字键写成 `"30"` / `30.0`、开关写成 `1`、键名或段名大小写不符 | 改完进游戏**没生效**，一点开 cfg **值变回了默认值** | 实测 B/C/D/G/H：`TomlTypeMismatchException` / `TomlNoSuchValueException` 被 empty catch 吞掉 → 条目保持代码默认值 → 启动期 4 次 `SaveToFile` 立刻把默认值写回磁盘 |
| **2** | **非法 TOML**：字符串值漏引号、值里内层双引号未转义、全角标点、同段重复键、只写键不写值 | "我改的东西**全都**没了"；在 ModManager 面板里改的也存不住 | 实测 E/F/K/M/O：`TomlParser.ParseFile` 抛异常 → `MelonPreferences.Load` catch → `WasError=true` → **整份文档为空**（所有条目回落默认）+ `IsSaving=false`（本次运行**所有 mod** 的保存被静默丢弃），日志只有一行 ERROR |
| **3** | 改的是**已改名 / 已删除的旧键**（照旧文档或旧 cfg 抄的） | 改了完全没反应，那几行**还会被删掉** | 仓库 `开发用户须知.md` 里有 **23 个**当前注册表已不存在的键名（`7_ExpandEnabled` / `21_UpgradeHotkey` / `4A_BoxEnable` / `6A_Trashcan_*` / `8A_SaveBagPageCount` …）；注册表 85 键 vs 迁移表 83 条，旧键由 `KeyRenames`/`RemovedKeys`/`CleanupCfgFile` 三处摘 |

次要但真实：
- `EnsureDefaultIds` 的**"只增不减"**会把玩家手删的默认 ID 加回来（已文档化的取舍，但玩家会当 bug 报）。
- 每条 `Description` 会作为注释写回 → 玩家手写的注释也一起被覆盖。

一句话机制：**MelonLoader 读不懂的值 → 静默取代码默认值 → ContainerSuite 每次启动全量重写 cfg → 默认值落盘。**

---

## 1. 游戏侧 cfg 机制（反编译 + 实测结论）

| 阶段 | 代码位置（v0.7.4-ci.2586） | 行为 |
|---|---|---|
| 启动解析 | `MelonPreferences.Load()` → `File.Load()` → `TomlParser.ParseFile(path)` | 把整份 cfg 解析成一个 **TomlDocument 常驻内存**（一次快照）。**发生在所有 mod 之前** —— 实测 `Latest.log` 只有 1 次 `Preferences Loaded!`（12:25:38.040），mod Init 在 12:25:43.4 之后。此时 `Categories` 为空，所以 Load 阶段不做任何取值 |
| 取值 | `MelonPreferences_Category.CreateEntry` → `File.SetupEntryFromRawValue(entry)` → `document.GetSubTable(段).GetValue(QuoteKey(键))` → `Entry<T>.Load(v)` → `TomletMain.To<T>(v)` | **这才是真正的"读玩家配置"时刻**。`Load` 里 3 个 catch（`TomlTypeMismatch` / `TomlNoSuchValue` / `TomlEnumParse`）**全是空实现** ⇒ 读不懂 = 悄悄保持 `CreateEntry` 传入的代码默认值，零日志 |
| 落盘 | `Category.SaveToFile()` / `MelonPreferences.Save()` → 逐条 `InsertIntoDocument(段, 键, entry.Save())` → `File.Save()` = `File.WriteAllText(路径, document.SerializedValue)` | **整份文档重写**。未注册的键（别家 mod 的段、孤儿键）被保留；已注册的键**无条件覆盖**（`dont_save_default=false`），且条目的 `Description` 会作为 `#` 注释一起重写 |
| 键名匹配 | `TomlTable.Entries` = `new Dictionary<string, TomlValue>()`（默认比较器） | **段名/键名大小写敏感**。写成 `[custominfrastructure]` 或在段里写 `2d_savebagwidth` ⇒ 匹配不到 ⇒ 默认值，且文件里会出现**两个同名段** |
| 解析失败 | `File.WasError = true` 的 setter | 打印 `"Defaulting ... to Fallback Functionality..."` → `IsSaving=false` + `FileWatcher.Destroy()` ⇒ **此后所有 mod 的保存全部丢弃**，且这是 MelonLoader 层面，本套件无法拦截 |
| 运行中手改 | `Preferences.IO.Watcher`（`NotifyFilters.LastWrite\|LastAccess`）→ `LoadFileAndRefreshCategories()` | 外部改动**会被采纳**并刷新已注册条目（`IsSaving` 只挡一次自身写入事件）⇒ README 里"v1.7.8 起不再自己轮询写回"的说法成立 |

> 副产品：这套"文档整份重写"的语义正好解释了 `docs/cfg-migration.md` 里那句
> "只删 cfg 行，下一次 SaveToFile 就被写回来（2026-10-06 实测 17ms）"。

---

## 2. 实测矩阵（14 组手写写法 → 结果）

跑法：`cd _probe/tomltest && dotnet run`（直接引用游戏目录的 `MelonLoader/net6/Tomlet.dll`，
读取口径逐行对齐 `File.SetupEntryFromRawValue` + `Entry<T>.Load`）。

| 用例 | 玩家这么写 | 结果 |
|---|---|---|
| A | `2D_SaveBagWidth = 30` | ✅ 读到 30 |
| **B** | `2D_SaveBagWidth = "30"` | ❌ **静默回落默认 24**（TomlTypeMismatch） |
| **C** | `2D_SaveBagWidth = 30.0` | ❌ **静默回落默认 24**（TomlTypeMismatch） |
| **D** | `z1_DiagLog = 1` | ❌ **静默回落默认 false**（TomlTypeMismatch） |
| **E** | `8A_PerIdSizeLimits = storage_bay=30x20, evidence_box=24x24`（漏引号） | 💀 **整份解析失败**（TomlInvalidValueException）→ Fallback |
| **F** | `1D_UpgradeHotkey = LeftAlt`（漏引号） | 💀 **整份解析失败** → Fallback |
| **G** | 段名写成 `[custominfrastructure]` | ❌ 全部条目**静默回落默认值**，文件里多出一个段 |
| **H** | 键名写成 `2d_savebagwidth` | ❌ 静默回落默认值 |
| I | `2D_SaveBagWidth = 30 # 我改成 30` | ✅ 读到 30 |
| J | 值里用全角逗号（但在引号内） | ✅ 读到原文（本套件自己按 `，` 分隔，OK） |
| **K** | 同段里同一个键写两遍 | 💀 **整份解析失败**（TomlKeyRedefinitionException） |
| L | 键写在旧段 `[SaveBagExpand]` 里 | ⚠ `[CustomInfrastructure]` 里的值胜出；旧段那行会被 ModManager/清理逻辑当孤儿处理 |
| **M** | `2D_SaveBagWidth = "{ x = "F8" }"`（README 点名的那种） | 💀 **整份解析失败** → Fallback |
| N | `1D_UpgradeHotkey = 'Ctrl+E'`（单引号） | ✅ 读到 Ctrl+E |
| **O** | `1D_UpgradeHotkey =`（只写键不写值） | 💀 **整份解析失败** → Fallback |

**读法**：`❌` 是"手改被默认值覆盖"的**逐键**形态；`💀` 是"手改全丢 + 之后什么都存不住"的**整份**形态。

---

## 3. ContainerSuite 侧的放大器（对着源码点名）

| 位置 | 行为 | 后果 |
|---|---|---|
| `CustomInfrastructureConfig.cs:108`、`SaveBagExpandConfig.cs:156`、`DiyUpgradeMod.Core.cs:131` 与 `:145`、`CfgMigration.Rename.cs:219` | **每次启动 4 次无条件全量重写**（实测日志 4 次 `[CustomInfrastructure] MelonPreferences Saved`；全游戏 17 次） | 把"读失败 → 默认值"从"可能"变成"**必然落盘**"；同时把玩家手写的注释一起改掉 |
| `DiyUpgrade.CfgMigration.DefaultIds.cs` `EnsureDefaultIds` | "默认值 ∪ 用户值"，只增不减 | 玩家手删的默认 ID 被加回 → 报成"改完被覆盖" |
| `KeyRenames`(83 条) / `RemovedKeys`(27 个) / `CleanedCfgFile`(13 个 LegacyKeys) | 三处摘旧键 + 直接改玩家文件 | 玩家写旧键名 ⇒ 行被删、值落默认（有备份，但玩家不知道） |
| `DiyUpgradeMod.PerIdLimits.cs` `RefreshPerIdLimitsComment` | 动态重写 `8A_PerIdSizeLimits` 的 `Description`（多行长注释） | 玩家手写的注释/示例被覆盖（值不受影响） |
| `README.md.txt` / `README_mobile.txt` | 键名与代码一致（已抽查，v1.7.9 无漂移） | ✅ |
| **`开发用户须知.md`** | **23 个键名已过期**（`7_ExpandEnabled` 现在叫 `2_ExpandEnabled`、`21_UpgradeHotkey` → `1D_`、`4A_BoxEnable` → `7A_BoxEnable`、`6A_Trashcan_*` → `5A_*`、`8A_SaveBagPageCount` → `3A_`、`3A_SkipXiaoBay` → `6A_` ……） | 谁照这份文档手改，必定全部落默认（成因 3）。**这份文件没进 v1.7.9 的 zip（zip 只有 DLL + 两个 README），但它在仓库里，随时可能被当用户文档发出去** |
| `_shared/check-readme-defaults.ps1` | 只校验 README 的**尺寸数字**与代码默认值一致 | 键名漂移不在守卫范围内 ⇒ 上面那 23 个过期键名能长期存活 |

---

## 4. 优化方案

### P0-1　启动前「配置体检 + 宽容采纳」——新增 `ContainerSuite.Shared/CfgDoctor.cs`

**落点**：`Mod.OnInitializeMelon` 最开头调一次（必须在 `CI.Init` 之前，才赶得上 `CreateEntry` 取值）。

**做什么**（只读扫描 `UserData\MelonPreferences.cfg` 的 `[CustomInfrastructure]` 段）：
1. 段名/键名**大小写归一**后查找；
2. 值做宽容转换：`"30"` → 30；`30.0` → 30（int 键）；`1/0/yes/no/true/false/开/关` → bool；未加引号的字符串值 → 加引号并转义；
3. 与段内**注册键的代码默认值**比对：**"文件里有值、条目却是默认值" = 命中静默回落** → 用宽容值改写条目（复用 `DiyUpgrade.CfgMigration.ApplyRawValue` 那套反射，**不落盘**）；
4. 无条件输出（不进 `z1_DiagLog` 闸门）：
   - 逐项点名：`[cfg体检] 2D_SaveBagWidth：文件里写的是 "30"，已按 30 生效；建议改成 2D_SaveBagWidth = 30`
   - 一行汇总：`[cfg体检] 手改 N 项全部生效，其中 M 项写法不规范（已在上面点名）`

**为什么这样能治本**：把"读不懂就静默变默认"从**游戏行为**变成**本套件可观测、可纠正**的行为。
玩家就算写成 `"30"`，功能也照常生效；下次他想改回来时 cfg 里还是他自己的原文。

**验收**：用例 B/C/D/G/H 下值仍生效且日志点名；用例 A/I/J/N 不产生任何噪音。

### P0-2　非法 TOML 的定位与显式警告

**落点**：同 `CfgDoctor`。
1. 反射读 `MelonPreferences.DefaultFile` 的 `WasError`（`Preferences.IO.File` 是 internal，私有字段 `_waserror`；可行性高，但**需实机确认字段名**）→ 判断"本次启动是否整份解析失败"；
2. 若失败：先备份 → 用 Tomlet 逐行/二分试解析，定位到**行号 + 原始报错**；
3. Error 级输出，并把最关键那句说出来：
   `本次运行期间，所有 mod 的配置改动（含 ModManager 面板里改的）都不会被保存，请修复后重启。`
4. 可选：对高置信度的非法写法（裸值含逗号、内层引号未转义、全角 `＝`）自动改成合法写法后写回，并提示"已修复，重启后生效"。

**为什么**：README"已知问题"一节目前把责任推给玩家（"逐段自查，重点是别家 mod 写的段"）。
把行号直接打出来，是这里性价比最高的一步。

### P0-3　只在该保存时保存

把 4 次无条件 `SaveToFile` 收敛为"仅当 (a) 段/键首次创建、(b) 本次发生迁移/补齐/宽容修正/注释重建 时保存"。

- 收益：`读失败 → 默认值落盘`的机会从"每次启动必然"降为"仅首次/有变更"；顺带减掉每次启动 17 次全量写盘。
- ⚠ 不能省的：首次安装要生成完整 cfg；`RefreshPerIdLimitsComment()` 之后仍需落盘一次（它改了注释）。
- ⚠ 语义确认：面板改配置时 ModManager 会存、退出时 MelonLoader 自己会存 —— 所以去掉例行保存不会让任何玩家改动丢盘。

### P1-1　键名构建守卫（顺手修掉 `开发用户须知.md`）

把 `_shared/check-readme-defaults.ps1` 扩成 `check-doc-keys.ps1`：抽 `README.md.txt` / `README_mobile.txt` /
`开发用户须知.md` 里所有 `\b[0-9]{1,2}[A-Z]?_[A-Za-z]\w*\b` 形式的键名，与 `CfgKeys.cs` 的注册表比对，
**出现不存在的键名即构建失败**。同时修掉那 23 个过期键名，或直接标注废弃/删掉这份文件。

### P1-2　配置生效摘要（把"静默"彻底关掉）

把 `LogPerIdLimitsSummary` 的模式扩展到全部 85 键：启动无条件输出一行
`[cfg生效] 手改 N 项、不一致 M 项（已点名）`；"读失败清单"单独一行 Warning。

### P1-3　`EnsureDefaultIds` 的否决记忆

在 cfg 里（或旁路文件）记一份"玩家删除过的默认 ID"，让"删掉就是删掉"；或退一步——
只在日志里说"检测到默认 ID 缺失（可能你是有意删的），未自动加回"。

### P2
- **P2-1** 给动态长注释加开关（如 `z3_RefreshCfgComments`），不想被重写注释的人可关。
- **P2-2** 把 `_probe/tomltest` 的 14 组用例固化进 `_regress`，任何未来改动都要过这套矩阵。

---

## 5. 验证方式

**A. 用例矩阵（现在就能跑）**
```bash
cd "D:/games/open code work/ContainerSuite/_probe/tomltest" && dotnet run
```

**B. 手工验收清单（改完代码后）**

| 场景 | 预期 |
|---|---|
| 手改 `2D_SaveBagWidth = "30"` 后启动 | 实际宽 30；日志一行 `[cfg体检]` 点名建议改成 `30`；cfg 里仍是玩家原文 |
| 手改 `2D_SaveBagWidth = ` （留空）后启动 | 日志 Error 点出**行号**+Tomlet 原文；出现"本次运行所有保存被禁用，请重启"提示 |
| 手改 `z1_DiagLog = 1` 后启动 | 诊断日志实际打开；日志点名 |
| 手改一个旧键名（如 `7_ExpandEnabled = false`） | 明确 Warning：`键名已变更，当前叫 2_ExpandEnabled（本次未自动迁移/已迁移）` |
| 全新安装（无 cfg 段） | cfg 完整生成，85 键齐全，无多余保存 |

---

## 6. 本次未做 / 未验证

- **没跑游戏**，没改任何 mod 源码（本文件是分析与方案）。
- `WasError` / `IsSaving` 的反射读取可行性**未实机验证**（字段名/可见性需装机确认）。
- 没验证 ModManager（Nico's Lab）面板是否另存了一份自己的 cfg 快照（只从既有注释与日志看到它会把旧 key 写回）。
- 未确认"多槽/模组槽 `AlignSlots` 只有多槽类注册"这一条在玩家手改时的边界（与本次问题无关，仅记录）。
- 反编译与实测产物在 `_probe/`（`melon/*.cs` 反编译源码、`tomltest/` 用例工程），**可随删**。

---

## 7. 实施记录（2026-10-14 · v1.8.0）

按上面 P0/P1 已落地，**代码与文档都改了**。逐条对照：

| 方案项 | 落点 | 状态 |
|---|---|---|
| P0-1 启动前「配置体检 + 宽容采纳」 | 新增 `ContainerSuite.Shared/CfgDoctor.cs`（`Preflight` / `ApplyTo` / `Report`）；`Mod.cs` 首尾接线；CI / SBE / DIY 三处 `Init` 接线 | ✅ |
| P0-2 非法 TOML 定位 + 显式警告 | `CfgDoctor.DetectFallback`（反射读 `WasError`/`IsSaving`）+ `FindSuspectLines` 逐行启发式（只在 Fallback 时跑）+ `broken-*.bak` 备份；报告"本次运行所有保存被禁用，修复后重启" | ✅ |
| P0-3 只在该保存时保存 | `CfgDoctor.SaveIfNeeded`（值 / 缺键 / 注释三判据）；三模块 4 次无条件 `SaveToFile` 全部替换（`CustomInfrastructureConfig.cs`、`SaveBagExpandConfig.cs`、`DiyUpgradeMod.Core.cs` ×2）。★ 首轮实机后删掉了最初加的 `MarkDirty`（它导致每次启动仍写 1 次盘），详见文末「八、实机验证」 | ✅ |
| P1-1 键名构建守卫 | 新增 `_shared/check-doc-keys.ps1` 并挂进 csproj `BeforeTargets=Build`；修掉 `开发用户须知.md` 的 23 个过期键名（29 处替换） | ✅ |
| P1-2 配置生效摘要 | `CfgDoctor.Report()`：`[cfg体检]` 无条件输出汇总 + 逐项点名；`LogVetoSummary` 同套路 | ✅ |
| P1-3 `EnsureDefaultIds` 否决 | 新增 `CfgKeys.DefaultIdsVeto = 8B_DefaultIdsVeto`；`MergeMissingIds` 跳过否决 id 并回报个数 | ✅ |
| P2-2 用例矩阵固化 | `_probe/tomltest`（14 组 Tomlet 口径）+ `_probe/cfgdoctortest`（**22 条 CfgDoctor 单测，22 PASS / 0 FAIL**） | ✅ |
| P2-1 动态注释开关 | 未做（`RefreshPerIdLimitsComment` 已改为"注释没变就不重写、不标记落盘"，效果等价） | ⏸ |

### 实施时与方案不同的两处决定

1. **P0-1 的落点不是"Mod 开头统一做"，而是"各模块 CreateEntry 之后各做一次"。**
   原因：`ApplyTo` 需要条目的**类型与默认值**才能判断"是不是静默回落"；而 DIY 有
   `LoadPerIdSizeLimits`（读 `8A` 的 Init 期快照）与 `RefreshIdLists()`（读 5 条 `*Ids`），
   所以 DIY 的 `ApplyTo` 必须夹在"最后一个 CreateEntry"与"第一次读快照"之间 ——
   最终放在 `RegisterPerIdSizeLimits` 内部（它注册的是最后一个条目）。
2. **体检不改玩家的 cfg 文件。** 方案里 `[cfg体检]` 建议"建议改成 X"，实施时确认保持
   **只写日志、不写文件**：`NeedsSave` 对"引号包数字"这类只比较语义值，因此不会触发
   规范化写回 —— 也就是说"手改被默认值覆盖"这件事从"文件被改"降级为"每次启动重新采纳 + 每次点名"，
   玩家自己决定要不要按建议改。唯二动文件的地方是 Fallback 时额外复制一份 `broken-*.bak`。

### 顺带修掉的两个既有问题

- **`check-readme-defaults.ps1` 其实早已不跑**：它靠 `_shared\CommonMod.props` 挂载，
  而该 props 在"公共属性内联进 csproj"时被弃用；且它解析的 `7A..7F 宽高默认值`表在
  2026-10-13 README 重写后已不存在。已归档到 `_archive\门禁脚本-已废弃\`，
  缺口（文档"值"级漂移无守卫）记在 `check-doc-keys.ps1` 头部 TODO。
- **`开发用户须知.md` 已是"照着改必错"的状态**（23 个过期键名 + 段名前缀全错 + 还在描述
  已删除的「配置保活」与「cfg 自检」探针）。已校正键名并加了文档口径提示。

### 实施后仍然未被验证的部分（写入本节时）

- `CfgDoctor` 的**核心取值逻辑**已用离线单测覆盖（当时 22/22 PASS，反射调用已编译产物）。
- 未实机验证：`DetectFallback`、`ApplyTo` 与真实 `Entries` 的交互、`SaveIfNeeded` 的落盘次数。

---

## 8. 实机验证（2026-10-14 12:54 · 手动启动一轮）

日志：`MelonLoader\Latest.log`（79 KB，14 次全游戏落盘）。加载的 DLL SHA256 = `DF562A4E…`
—— **与本仓库 `dist/ContainerSuite-v1.8.0/CustomInfrastructure.dll` 一致**，验的是真产物。

| 验证点 | 实测 | 判定 |
|---|---|---|
| 版本横幅 | `ContainerSuite v1.8.0` | ✅ |
| 体检跑起来了 | `[cfg体检] cfg 无异常（86 项全部按文件取值）` | ✅ 且**无误报**（本机 cfg 本来就规范，宽容采纳 0、读不出 0） |
| 8B 落盘 | cfg 第 831 行 `8B_DefaultIdsVeto = ""` + 中文注释；日志有 `8B 否决清单为空 → …` | ✅ P1-3 |
| 本套件落盘次数 | **1 次**（改前固定 4 次） | ⚠️ 未达预期 0，见下 |
| 回归标记 | ci.patch=2 / sbe.patch=7 / diy.patch=18 / wage.avoid=7/7 / suite.ready=1.8.0，无本套件 ERROR | ✅ |

### 首轮暴露的问题：仍写 1 次盘

那 1 次写盘的原因就在日志下一行：`写盘：CustomInfrastructure（已标记：8A 注释重建）`。

根因是**我自己引入的**：`RefreshPerIdLimitsComment` 里加了 `MarkDirty`，而它每次启动必被触发 ——
`ApplyConfigLabels()` 会先把 8A 的 `Description` 改回短说明，于是"注释变了"永远成立 ⇒
等价于保留了一次无条件写盘。**这正是"有一个强制保存，就等于每次启动整份重写"的实例。**

修正：删掉 `MarkDirty`（连同 `_dirty/_dirtyReason` 一起删，不留死代码）。注释变更改由
`SaveIfNeeded` 的注释比对触发；并把"写盘 N 次（跳过 M 次）"**无条件**打进收尾报告
（否则"到底有没有改写玩家文件"只有开 DiagLog 才看得见 —— 首轮那行 `跳过写盘` 之所以可见，
是因为 Diag 的 provider 在 `CI.Init` **之后**才注入的既有顺序副作用）。

### 为这条修正补的离线证据

`_probe/cfgdoctortest` 从 22 条扩到 **25 条**，新增的 3 条专门验证"注释比对保真度"：
用 Tomlet **真的序列化一遍**（模拟 MelonLoader 落盘），再用 `CfgDoctor.CommentAbove`
从原文读回，覆盖单行 / 多行含空行+缩进+全角 / 空注释 → **25 PASS / 0 FAIL**。
即：`CommentAbove(file)` 能逐字还原 `Description`，所以"注释没变 ⇒ 跳过写盘"这条判据成立。

### 修正后的第二轮（13:08 · 手动启动）

`[cfg体检] 汇总：体检 86 项，宽容采纳 0 项，读不出 0 项；写盘 0 次（跳过 3 次） — cfg 未被改写`

落盘次数 **4 → 1 → 0**，修正生效。回归 `PASS=34 / FAIL=0 / WARN=0`。

---

## 9. 故障测试（2026-10-14 13:04 · 故意写坏真实 cfg）

CfgDoctor 有三条路径正常启动走不到（Fallback 探测 / 行号定位 / broken 备份）。做法：
把真实 cfg **备份后**注入一个玩家最常见的错法 —— 第 831 行
`8B_DefaultIdsVeto = ""` → `8B_DefaultIdsVeto = rat_cage`（裸值 = 非法 TOML，
正是 `8B` 描述文案里举的那个例子）—— 启动游戏跑一遍，验完还原并逐字节校验。

### 实机结果（全部按设计触发）

```
[ERROR] [cfg体检] ✗ MelonPreferences.cfg 整份解析失败（MelonLoader 已进入 Fallback）。
[ERROR] [cfg体检]   后果：**所有 mod** 的配置都停在代码默认值，且本次运行期间任何配置改动（含面板里改的）都不会被保存。
[ERROR] [cfg体检]   已确认 MelonLoader 关闭了写盘（IsSaving=false）。
[ERROR] [cfg体检]   已额外备份一份待排查文件：...\UserData\MelonPreferences.cfg.broken-20261014-130441
[ERROR] [cfg体检]   疑似非法 TOML → 第 831 行 · 裸值不被 TOML 接受（字符串必须整体加引号）：8B_DefaultIdsVeto = rat_cage
[WARNING] [cfg体检] 12 项手改写法不规范，已按你的**本意**生效 …
[ERROR] [cfg体检] 本次运行不保存任何配置改动（Fallback）：修好上面点名的行、重启游戏后一切恢复。
```

| 验证点 | 结果 |
|---|---|
| 反射读 MelonLoader 私有 `WasError` | ✅ 命中（此前列在"未验证"里） |
| 反射读 `IsSaving`（字段） | ✅ 命中 |
| 行号定位 | ✅ 精确到第 831 行，且未误报前 830 行 |
| broken 备份 | ✅ 生成（45082 字节，与当时 cfg 同内容） |
| "本次运行不保存"告知 | ✅ |
| 宽容采纳在 Fallback 下救值 | ✅ 12 项（含 `z1_DiagLog=true`，否则该轮 Diag 总闸会回落成 false） |
| 本套件段是否被改写 | ✅ **226 行逐行未变**（文件级哈希变化全来自别家 mod 的段名规范化/注释折行） |

### 故障轮暴露的 3 个问题（已修）

1. `Report()` 的 Fallback 分支 `return`，把落盘统计那行吞了 —— 改为**无条件输出一行汇总**。
2. 裸字符串的建议文案（"改成 N 字符的带引号写法"）玩家看不懂 → 改为可照抄的措辞。
3. `no.suite.error` 把"玩家 cfg 非法"判成套件自身出错 → 加负向断言排除 `[cfg体检]` 行；
   cfg 环境健康交给 `cfg.doctor.ok` 专门断言（故障轮里它正确地 FAIL 了）。

### 离线工具改造（`_probe/fallbacktest`）

原想用沙箱里的真实 MelonLoader 复现 Fallback —— **做不到**：它记日志走原生 host
（`BootstrapInterop.Library` 为 null），在**把 `WasError` 置位之前**就抛 NRE。
改造为沙箱真能证的三段，现在 **12 PASS / 0 FAIL**：① 6 种写法直接问 Tomlet 是否抛异常
（证明故障成因）；② 强行置位 `_waserror`/`IsSaving` 验反射管道；③ 行号定位 + 备份。

> 踩坑记录：`DetectFallback(out bool)` 通过 **out 参数**回传，测试读静态字段永远 false —— 误判了一轮。

### 仍未验证

- ModManager（Nico's Lab）面板是否另存了自己的 cfg 快照。
- 「键名写错（成因 3）」在实机上的表现：旧的未注册键会被忽略但**不会被删**（清理清单只覆盖
  已登记的 LegacyKeys/RemovedKeys），与"被删掉"的差异未实机确认。

