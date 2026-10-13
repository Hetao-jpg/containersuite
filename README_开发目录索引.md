# ContainerSuite 开发目录索引

> 给维护者（未来的我 / 接手的 AI）看的目录地图。玩家说明在 `README.md.txt` / `README_mobile.txt`，不要看这里。

## 一、这个目录里有什么

```
ContainerSuite/
├── ContainerSuite.Infrastructure/   ← 唯一编译入口（csproj 在这）
├── ContainerSuite.Shared/           ← 地基（日志/节流/缓存/门禁，无独立功能）
├── ContainerSuite.SaveBag/          ← 主仓库分页/滚动/扩容/窗口拖动
├── ContainerSuite.DIYUpgrade/       ← 容器升级/批量升级/拖边改尺寸/跨mod避让
│
├── _regress/                        ← 回归测试脚本（regress.ps1 等，验收依赖）
├── dist/                            ← 打包产物（发布版 + 开发版 zip）
├── _archive/                        ← 历史存档（精简过的 README / 删除的代码 / 分析文档）
│
├── README.md.txt                    ← 玩家说明（打包时随 DLL 一起发）
├── README_mobile.txt                ← 移动端玩家说明
├── 开发用户须知.md                   ← 开发/高级用户须知
├── 物品id参考.md                     ← 物品图鉴（开发参考）
├── mod维护日志.md                    ← 开发维护日志（每次改动都记）
├── README_开发目录索引.md            ← 本文件
│
├── ContainerSuite 瘦身方案（AI 执行版）.txt  ← 待执行的重构计划
├── ContainerSuite.sln               ← 解决方案（只含一个编译入口）
├── pack.ps1 / pack.bat              ← 一键打包脚本
└── _verify_cfgkeys.py               ← cfg 键名一致性校验脚本
```

## 二、四个源码目录的分工

| 目录 | 角色 | 关键文件（举几例） |
|---|---|---|
| `ContainerSuite.Infrastructure/` | 编译入口 + CI（垃圾桶/档案箱/磁带机扩容） | `ContainerSuite.Infrastructure.csproj`、`Mod.cs`、`ContainerExpander.cs` |
| `ContainerSuite.Shared/` | 地基，无独立功能 | `ContainerSuiteLog.cs`、`PollThrottle.cs`、`CacheLimits.cs`、`WorldReadyGate.cs`、`SuiteVersion.cs` |
| `ContainerSuite.SaveBag/` | SBE：主仓库分页/滚动/扩容/地面窗口拖动 | `SaveBagPaging.cs`、`SaveBagExpandWindowDrag.cs` |
| `ContainerSuite.DIYUpgrade/` | DIY：容器升级/批量升级/拖边改尺寸/跨mod避让 | `DiyUpgradeMod.Core.cs`、`DiyUpgradeMod.Restore.cs`、`SkipXiaowoBayRestore.cs` |

**关键机制**：4 个 DLL 已于 2026-10-06 合并成 1 个 `CustomInfrastructure.dll`。源码仍留在 4 个目录（git 历史保留），由 `ContainerSuite.Infrastructure.csproj` 用 `<Compile Include="..\X\**\*.cs">` 拉进来编译。**所以 4 个源码目录的相对路径是写死的，不要移动。**

## 三、开发版 vs 发布版（2026-10-13 起）

项目用 `DIAG` 编译宏区分两个产物，**文件名一眼可辨**：

| 命令 | 产物 | 用途 |
|---|---|---|
| `dotnet build -c Release` | `CustomInfrastructure.dll` | 发布版（给玩家），DIAG 诊断代码被编译期裁掉 |
| `dotnet build -c Release -p:Diag=true` | `CustomInfrastructure-dev.dll` | 开发版（调试），含 `#if DIAG` 诊断探针 |

一键打包：

```powershell
.\pack.ps1          # 打包发布版 → dist/ContainerSuite-v1.7.8/
.\pack.ps1 -Dev     # 打包开发版 → dist/ContainerSuite-dev-v1.7.8/
```

**诊断代码都写在 `#if DIAG` 块里**（源码里有 10+ 处），Release 编译时连 IL 都不生成，所以「精简代码量」靠编译期裁剪，不靠手工删文件。

## 四、维护时的关键规则

1. **改版本号**：只改 `ContainerSuite.Shared/SuiteVersion.cs` 的 `Suite` 常量；README 标题和 AssemblyInfo 要同步（`pack.ps1` 会校验版本一致性，不一致拒绝打包）。
2. **改 cfg 键名**：只从 `ContainerSuite.Shared/CfgKeys.cs` 取常量，禁止写字符串字面量（改名漏一处 = 静默丢配置）。
3. **节流**：必须用 `PollThrottle`（单调钟），禁用 `DateTime.UtcNow` 差值。
4. **缓存上限**：用 `CacheLimits.Trim(x, ...)`，禁止裸写 `if (Count > N) Clear()`。
5. **每次改动记 `mod维护日志.md`**，异常必须留痕（`CatchLogOnce` / `LogOnce`）。
6. **回归**：改完跑 `_regress/regress.ps1`（需要游戏环境）。
7. **不要移动 4 个源码目录**，也不要改 csproj 里的相对路径 —— 会破坏编译和 git 历史。

## 五、待办：瘦身重构

`ContainerSuite 瘦身方案（AI 执行版）.txt` 里有 14 张任务卡（T-A1 ~ T-C4），目标在不减功能的前提下砍代码。执行前先读它末尾的「执行偏差」一节（2026-10-13 审查记录，含门禁脚本缺失、行数基线过时、T-B1 已完成等修正）。
