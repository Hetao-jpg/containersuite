# _probe —— 2026-10-14 的取证与自测目录

本目录**不参与编译、不打包、不参与构建门禁**（csproj 的 `<Compile Include>` 与
`_shared\check-*.ps1` 都只扫 4 个源码目录）。它只是"当时怎么得出结论"的现场，
**可以随时整个删掉**，删掉不影响 mod 的任何功能。

来源与结论见 `docs/cfg-handedit-rootcause-and-fix.md`。

## melon/ —— MelonLoader 0.7.4-ci.2586 反编译源码

用 `ilspycmd -t <类型>` 从 `MelonLoader\net6\MelonLoader.dll` 导出，读的是**本机实际
在跑的那一份**，所以结论不是"某版本大概这样"，而是本机事实：

| 文件 | 用途 |
|---|---|
| `MelonPreferences.cs` | `Load()` / `Save()` / `CreateEntry` 的调用顺序与异常处理 |
| `MelonLoader_MelonPreferences_Category.cs` | `CreateEntry` → `SetupEntryFromRawValue` 的取值时机 |
| `MelonLoader_Preferences_IO_File.cs` | **核心证据**：读值的 `GetValue` + 写盘的整份 `WriteAllText` |
| `MelonLoader_MelonPreferences_Entry.cs` | `Load(TomlValue)` 抽象 |
| `EntryT.cs` | **核心证据**：`Entry<T>.Load` 的 3 个空 catch（静默回落默认值的唯一原因） |
| `Watcher.cs` | FileWatcher 的外部改动采纳逻辑与 `IsSaving` 一次性门闩 |
| `MelonLogger.cs` | 日志为什么在进程外不可用（走 `BootstrapInterop.Library` 原生 host） |

重新生成：
```bash
"D:/games/dnspy/dnSpy.Console.exe" ...          # 不可用：无控制台时会 IOException
"/c/Users/msi/.dotnet/tools/ilspycmd.exe" -t <完整类型名> "<游戏>\MelonLoader\net6\MelonLoader.dll"
```

## tomltest/ —— Tomlet 读取口径实测（14 组手写写法）

直接引用游戏自带的 `Tomlet.dll`，复刻 `File.SetupEntryFromRawValue` + `Entry<T>.Load`
两步，验证"玩家这么写 → MelonLoader 读得到吗"。

```bash
cd _probe/tomltest && dotnet run
```

覆盖：引号包数字 / 小数给 int / 开关写 1 / 段名与键名大小写 / 行内注释 / 漏引号 /
重复键 / 未转义内层引号 / 只写键不写值 / 单引号字符串 / 全角逗号。

## cfgdoctortest/ —— CfgDoctor 离线单测（22 用例）

反射调用**已编译的** `CustomInfrastructure.dll` 里的
`ContainerSuite.Shared.CfgDoctor` 私有方法（`SnapshotFile` / `TryLenient`），
把"只能进游戏才知道"缩小到"只剩 MelonLoader 集成部分需要实机"。

```bash
cd _probe/cfgdoctortest && dotnet run     # 需要先 dotnet build -c Release
```

★ 它加载的是 `bin\Release\CustomInfrastructure.dll`，所以**改完 CfgDoctor 要重新 Release
构建再跑**，否则测的是旧产物。当前结果：22 PASS / 0 FAIL。

## fallbacktest/ —— Fallback 故障路径（12 用例）

把**真实 MelonLoader** 跑在临时沙箱里（`LoaderConfig.Current.Loader.BaseDirectory` 指向沙箱，
并在动手前断言 `MelonEnvironment.UserDataDirectory` 确实落在沙箱内，否则直接退出 —— 绝不碰真实游戏目录）。

```bash
cd _probe/fallbacktest && dotnet run     # 需要先 dotnet build -c Release
```

覆盖三段：① 6 种写法直接问 Tomlet 会不会抛异常（证明故障成因）；
② 强行把私有 `_waserror` / `IsSaving` 置成三种组合，验 `DetectFallback` 如实回报；
③ `FindSuspectLines` 行号定位 + `TryBrokenBackup` 备份内容一致。当前 **12 PASS / 0 FAIL**。

⚠️ **这里测不了"整份 Fallback 的端到端行为"**：MelonLoader 记日志走原生 host
（`BootstrapInterop.Library` 为 null），它在把 `WasError` 置位**之前**就抛 NRE。
那条路径只能在游戏里验 —— 见 `docs/cfg-handedit-rootcause-and-fix.md` 第 9 节
（2026-10-14 故障测试：备份真实 cfg → 注入裸值 → 启动 → 还原）。

## 尚未被覆盖的部分

- `DetectFallback` 的**端到端**行为 —— 只能在游戏里验证（已验：见报告第 9 节）。
- `ApplyTo` 与真实 `MelonPreferences_Category.Entries` 的交互（需要 MelonLoader 运行环境）。
- `SaveIfNeeded` 的实际落盘次数 —— 进游戏看 `[cfg体检] ... 跳过写盘` 的诊断行。
