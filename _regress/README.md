# _regress —— ContainerSuite 快速回归工具链

面向「改完一行 → 30 秒内知道坏没坏」的启动期回归，外加跨 mod 生态的独立体检。
**两个工具都不修改任何 mod**（除 `regress.ps1 -Deploy` 会主动覆盖 `Mods\CustomInfrastructure.dll`）。

```
_regress\
  lib.ps1         公共库（日志读取容忍 NUL、标记抽取、HTML 报告）
  mods-scan.ps1   ① 独立 mod 检测：Mods 目录 + Latest.log + WhoAmI 黑匣子 + cfg
  regress.ps1     ② Tier-1 快速回归：部署校验 → 启动 → 断言 → 报告
  checks.json     ③ 声明式断言表（文本兜底 + [REG] 结构化两层）
  out\            输出：*-report.json（机器读）+ *-report.html（人看）
```

## 用法

```powershell
# ① 体检当前 mod 生态（不启动游戏，秒级）
powershell -File _regress\mods-scan.ps1

# ② 对「当前日志」做回归断言（游戏已开着时用这个，秒级）
powershell -File _regress\regress.ps1

# ③ 一键：部署最新 Release 构建 → 启动游戏 → 等加载 → 断言
powershell -File _regress\regress.ps1 -Deploy -Launch

# 参数
#   -Deploy        把 ContainerSuite.Infrastructure\bin\Release\CustomInfrastructure.dll 拷进 Mods 并校验哈希
#   -Launch        启动游戏并轮询到 [SBE-JIT] 预热完成为止（默认超时 180s，之后再等 -SettleSec 秒）
#   -SettleSec 8   稳定期（默认 8）
#   -TimeoutSec    启动超时（默认 120）
#   -LogPath       指定日志文件（做坏样本自测时用）
#   -NoOpen        不自动打开 HTML 报告
# 退出码：0 = 全过，1 = 有 FAIL
```

## mods-scan 检测什么

| 判据 | 说明 |
|---|---|
| `mod.loaded:*` | Mods 根目录每个 DLL 是否被日志记录为已加载 |
| `mod.hash:*` | **日志里的 SHA256 vs 磁盘** —— 不一致 = 文件换了但游戏还是旧进程 |
| `mod.ghost:*` | 日志有加载记录但目录已无该文件 |
| `suite.deploy` | 已部署的 CustomInfrastructure.dll 是否落后于 Release 产物 |
| `log.errors.suite` | **本套件**的 ERROR（其它 mod 的 ERROR 单独归 `log.errors.other`，不冤枉本套件） |
| `whoami.conflicts` | 交叉验证 WhoAmI 黑匣子的「多模组改写同一方法」扫描 |
| `shadow:*` | 子目录 / 备份里的未加载 DLL（按目录分组，不逐条刷屏） |

## [REG] 结构化标记协议

mod 侧由 `ContainerSuite.Shared.RegMark` 输出，格式固定、id 全 ASCII、中文只进 detail：

```
[REG]    id=<ascii> status=PASS|FAIL|NA detail=<任意文本>
[REG.BP] name=<ascii> detail=<任意文本>          ← 断点：关键状态转换处留痕
```

**为什么不让脚本去猜中文日志**：自由文本匹配分不清「预期内的候选未命中」和「真失败」。
实例：`Could not find method for type WagePerks... DoUpgrade (GameItem,GameItem,bool)` 是一条
**良性**告警（候选签名回退），而旧的 `verify-log.ps1` 的 C6 会把它判成 FAIL —— 实际 14/14 成功。

当前已埋的标记：

| id / name | 位置 | 含义 |
|---|---|---|
| `ci.containers` / `ci.patch` | `Mod.cs` | CI 生效容器数 / 补丁挂载数 |
| `sbe.init` / `sbe.patch` | `Mod.cs` / `SaveBag\mod.cs` | SBE 模块初始化 / 补丁安装数 |
| `sbe.paging` / `sbe.jit` | `SaveBagPaging.cs` | 分页页数 / JIT 预热 prepared |
| `diy.patch` / `diy.patch.summary` / `diy.init` | `DiyUpgradeMod.Core.cs` / `Mod.cs` | DIY 补丁安装数 / 汇总数 / 模块初始化 |
| `wage.avoid` | `SkipWageUpgradeRestore.cs` | Wage 避让 N/N（未装蛙哥时输出 `NA`） |
| `suite.ready` | `Mod.cs` | 套件就绪 + 版本 |
| `diy.restore.ready`（BP） | `DiyUpgradeMod.Patches.cs` | 读档恢复协程就绪（**交互期断点**） |

## 断点（Tier-2）怎么用

IL2CPP 游戏无法脚本化输入，所以交互期只能人玩、工具事后断言：

1. 人：进存档 / TAB 切页 / 拖材料升级。
2. 工具：`powershell -File _regress\regress.ps1`（或把 `Latest.log` 复制出来用 `-LogPath`）。
3. 报告底部「断点留痕」会列出本次触发的所有 `[REG.BP]`，`checks.json` 里的 `kind:"bp"` 断言会核对。

要新增断点：在关键转换处加一行 `ContainerSuite.Shared.RegMark.Bp("名字", "detail")`，
然后在 `checks.json` 加 `{ "id":"bp.x", "kind":"bp", "bpName":"名字" }`。

## 与 verify-log.ps1 的关系

`verify-log.ps1` 是 7 条文本断言的旧脚本，**保留不动**。本工具链是它的进化版：
断言外置成 `checks.json`、增加 [REG] 结构化层、增加独立 mod 检测、容忍日志里的 NUL 字节。

## 注意

- `Latest.log` 是 UTF-8 无 BOM 且**含 NUL 字节**，`grep`/`file` 会误判为二进制。
  .NET 读取不受影响（`lib.ps1` 已顺带剥掉 NUL），但用 shell 工具看日志要加 `-a`。
- 本目录下所有 `.ps1` 均为 **UTF-8 with BOM**（PS 5.1 否则按 ANSI 解码）。改动后请重新补 BOM。
