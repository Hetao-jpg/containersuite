# docs 索引（设计/取证文档）

> 本目录收录从源码搬迁而来的**背景 / 取证 / 决策理由**类注释。源码里对应位置只留一行指针。
> 目的：让源码重新成为"可读的代码"，而把"为什么这么做"集中到一处可检索的地方。
> （注释精简工程，2026-10-13 起；方法论见仓库根目录 `注释精简建议.txt`。）

## 一、本次搬迁新增（2026-10-13 注释精简 · 批次1）

| 文档 | 主题 | 主要来源 |
|---|---|---|
| [cfg-migration.md](cfg-migration.md) | CFG 迁移链、终止线 1.7.4、键名重排、旧键合并、ID 白名单补齐 | `DiyUpgrade.CfgMigration*.cs`、`CfgKeys.cs`、`SuiteVersion.cs` |
| [jit-warmup.md](jit-warmup.md) | JIT 预热体系：登记表、声明式 `[JitWarmup]` 契约、Bind 链取证 | `WarmupRegistry.cs`、`SaveBagPaging.Warmup.cs`、`WindowShowHook.cs` |
| [crash-defense.md](crash-defense.md) | 0x80131506 崩溃模型、CrashProbe 心跳探针 | `CrashProbe.cs` |
| [monotonic-clock.md](monotonic-clock.md) | 为什么用单调钟节流（`PollThrottle` 的存在理由） | `PollThrottle.cs` |
| [silent-failure.md](silent-failure.md) | "绝不静默失效"判据：[REG] 标记、恢复放弃留痕 | `ContainerSuiteLog.cs`、`RestoreArbiter.cs` |
| [diy-restore.md](diy-restore.md) | 读档尺寸恢复/重建沿革 | `DiyUpgradeMod.Restore.cs` |
| [diy-resizer.md](diy-resizer.md) | 拖边改尺寸：手柄、性能优化、加载期门禁、上限沿革 | `DiyUpgradeMod.Resizer*.cs`、`DiyUpgradeMod.GridWindow.cs` |
| [ci-expander.md](ci-expander.md) | CI 容器扩容（垃圾桶/档案箱/磁带机）沿革 | `ContainerExpander.cs`、`ContainerExpandPatch.cs` |

## 二、既有文档（早于本次搬迁）

| 文档 | 主题 |
|---|---|
| [diy-groupdrag.md](diy-groupdrag.md) | DIY 组拖对账 |
| [diy-recharger-sprite.md](diy-recharger-sprite.md) | 充能器贴图钳制（反汇编取证） |
| [sbe-window-drag.md](sbe-window-drag.md) | SBE 地面窗口拖动 |
| [cross-mod-wage.md](cross-mod-wage.md) | 跨 mod 兼容：蛙哥（Wage） |
| [cross-mod-xiaowo.md](cross-mod-xiaowo.md) | 跨 mod 兼容：小窝（XIAOWO） |
| [harmony-dontpatchall.md](harmony-dontpatchall.md) | 为什么不能 PatchAll（Harmony 手动注册） |

## 三、约定

- 源码指针形态：`// 背景见 docs/xxx.md`（1~3 行），不再复述长段落。
- **就地保留**（不进本目录）：不变量/契约判据、玩家可见告警、跨 mod 外部契约（`CfgKeys` / `#runKey` 等）、
  含 `0x80131506` 的运行时事实说明、`★ 保持无条件` 的 regress 哨兵行。
- 新决策写进 commit message，不写回代码注释。
