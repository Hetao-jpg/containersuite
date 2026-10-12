# Probably Stolen — Mod 开发指南（AI 操作版）

> **本文件的读者是 AI 助手。** 用户会把本文件（或它的路径）交给 AI，
> 由 AI 调用文件工具与 dnSpy MCP 完成分析、编码、构建、部署、验证。
>
> **总纲（来自 `MODS.md` / `mod工作流程清单.docx`，不可推翻）：
> 诊断先于修改。一次只改一件事。AI 说的话，验证后再用。**
>
> ★ 2026-10-07 核桃修订：**「一次只改一件事」改为「按把握度决定是否合批」** ——
> 把握 > 80% 时允许多件事一起改、一起验证（判据：风险面互相独立 + 每件可验/可单独回滚；
> 同处一条脆弱路径的改动仍要拆开）。其余总纲不变。

---

## 0. 开工前 30 秒自检

按顺序确认，任一项不成立就**停下来报告用户**，不要硬做。

| # | 检查项 | 判定方法 |
|---|---|---|
| 1 | dnSpy 在运行 | `curl http://localhost:3000/health` 返回 `{"status":"ok",...}` |
| 2 | 读得到文件 | `D:\games\mod调试\MODS.md` 能 `Read` |
| 3 | 游戏目录存在 | `D:\SteamLibrary\steamapps\common\Probably Stolen Demo\` |
| 4 | 已读本工作区权威清单 | **`D:\games\mod调试\MODS.md`**（受管项目 8 个、命名约定、分发结论都在那） |

> dnSpy 关掉 = MCP 断开 = 反编译能力全失效。
> 开工前打开 `D:\games\dnspyex\dnSpy.exe`，并在
> `View → Options → MCP Server → Enable Server` 勾上。

---

## 1. 环境事实（不要推测，直接用）

### 1.1 游戏与运行时

| 项 | 值 |
|---|---|
| 游戏 | **Probably Stolen**（Demo 与 Playtest 两份） |
| 引擎 | Unity 2021.3.45f2 · **Il2Cpp** · x64 · runtime **net6** |
| 游戏版本 | 0.46D（**改 DLL 前必须确认，游戏一更新签名可能变**） |
| MelonLoader | Demo = **0.7.4-ci.2586** / Playtest = 0.7.3（要求 ≥ 0.7.0） |
| 编译目标 | `net6.0` |

**根路径**

```
游戏(Demo)  : D:\SteamLibrary\steamapps\common\Probably Stolen Demo
游戏(Playtest): D:\SteamLibrary\steamapps\common\Probably Stolen Playtest
  ├─ Mods\                                  ← mod DLL 部署目标
  └─ MelonLoader\
       ├─ Latest.log                        ← 验收必看
       ├─ net6\                             ← MLNet：MelonLoader.dll / 0Harmony.dll / Il2CppInterop.*.dll
       └─ Il2CppAssemblies\                 ← MLIl2Cpp：Assembly-CSharp.dll + 各 UnityEngine.*.dll
工作区      : D:\games\mod调试               ← 所有 mod 源码
dnSpy       : D:\games\dnspyex\dnSpy.exe
opencode 配置: C:\Users\msi\.config\opencode\opencode.jsonc  (mcp.dnspy → localhost:3000)
```

### 1.2 已装工具链

git 2.55 / node 24.19 LTS / python 3.12 (`py`) / dotnet SDK **6.0.428 · 9.0.318 · 10.0.401** /
dnSpyEx **6.6.0 (64-bit, .NET)** + **dnSpy.Extension.MCP**（32 个工具）/ dnSpy 6.1.8（旧，别用）/
ILSpy 10.0 / Il2CppDumper 6.7.46 / Ghidra 12.1.4 / IDA Free 9.4 /
VS 2022 17.14.41（含 **MelonLoader.VSWizard VSIX 1.4.0**，已最新）

---

## 2. 工具调用规则（**踩过的坑，照做**）

### 2.1 dnSpy MCP —— 一次只发一个调用

> ⚠️ **实测坑**：把多个 `dnspy_*` 调用放进同一条消息里并行发出，
> 序列化层会把它们**拼接成一个残缺 JSON**，报
> `JSON parsing failed: ... Unexpected end of JSON input`，
> 而且会反复复现、越滚越大。
>
> **规则：每条消息最多 1 个 `dnspy_*` 调用。** 其它工具（read / edit / bash / glob）可以正常并行。

可用的 dnSpy 工具（按典型分析顺序）：

| 工具 | 用途 | 关键参数 |
|---|---|---|
| `list_assemblies` | 列出已加载程序集 | `name_filter` |
| `open_files` | 打开 DLL（目录可批量，`pattern`/`recursive`） | `paths` |
| `search_types` | 按名找类型 | `query`（支持 `*Perk*` 通配）、`names_only`、`assembly_name` |
| `get_type_info` | 类型成员总览（token / 签名 / 字段偏移） | `compact=true` 更省；`members_filter` 过滤 |
| `list_methods` | 列方法（带 MethodDef token、可消歧） | 用于拿 token 喂下游 |
| `decompile_type` | 整个类型反编译 | `assembly_name` + `type_full_name` |
| `decompile_method` | 单方法反编译 | 重载用 `parameter_types` 或 `method_token` |
| `get_method_il` | 原始 IL（指令/局部/异常处理） | operand 带标签，可回喂 `patch_method_il` |
| `find_callers` | **谁调用了它**（追调用链主力） | `assembly_name`+`type_full_name`+`method_name`/`method_token` |
| `find_callees` | 它调用了谁 | 同上 |
| `find_overrides` | 虚方法/接口的覆写实现 | `direction` |
| `find_references` | 按 method/field/type/**string** 找引用点 | `target_kind` |
| `search_string_literals` | 字符串反查（`ldstr`）→ 找 PlayerPrefs 键/场景名 | `query` 支持 `*` 通配 |
| `search_constants` | 魔法数字反查 | 建议带 `assembly_name` 收敛 |
| `find_unity_messages` | 列 Unity 生命周期方法（Update/OnGUI…） | 这类**没有 IL 调用点**，只能这么找 |
| `patch_method_il` / `force_return` / `nop_method` | 改 IL | 见 §6 红线 |
| `revert_method_il` / `save_assembly` | 回滚 / 落盘 | `save_assembly` 会自动做时间戳备份 |

**参数约定**

- `assembly_name`：可填简单名 `Assembly-CSharp` 或**完整路径**；同名程序集有多份时**必须用完整路径**。
- `method_token`：**十进制** uint（如 `100681353`）或 hex（`0x06001110`），来自 `get_type_info` / `list_methods`。
  跨程序集引用时要把 `resolved token` 和 `target_assembly` 配对使用。
- 类型全名要含命名空间：`Il2Cpp.SaveManager`，不是 `SaveManager`。
- 嵌套类型用 `Outer+Nested` 或 `.` / `/` 分隔均可。

**启动时若程序集没加载**：先 `open_files` 指向
`...\MelonLoader\Il2CppAssemblies\`（目录形式，`pattern=*.dll`）。

### 2.2 文件工具

- 中文/非 ASCII 路径（`D:\games\mod调试`、`自定义集装箱` 等）：**`glob` 会失效返回空**。
  改用 `bash` + `Get-ChildItem -LiteralPath`。
- 读写文件一律用 `Read` / `Edit` / `Write`，不要用 `Get-Content`/`Set-Content` 猜编码。

### 2.3 PowerShell 5.1（`bash` 工具的 shell）

| 坑 | 正确做法 |
|---|---|
| `Move-Item` **没有 `-Recurse`** | 移动目录用 `Move-Item -LiteralPath a -Destination b`（只对目录本身），或 `Copy-Item -Recurse` + `Remove-Item -Recurse` |
| 控制台默认 GBK，中文乱码 | 命令开头加 `[Console]::OutputEncoding=[Text.Encoding]::UTF8` |
| 命令里别用 `&&` | 用 `cmd1; if ($?) { cmd2 }`，或分开调用 |
| 带空格/中文的可执行路径 | `& "path\to\exe" args` |
| 批量工具调用 | 依赖关系的命令必须串行，独立的才可并行 |

---

## 3. 分析游戏代码的标准 SOP

所有反编译目标默认是：

```
D:\SteamLibrary\steamapps\common\Probably Stolen Demo\MelonLoader\Il2CppAssemblies\Assembly-CSharp.dll
```

**第 1 步 — 找类型**（不知道类名就按关键词搜）
`search_types` → `query=*Save*` / `*Perk*` / `*Inventory*`，拿到 `FullName` + `Token`。

**第 2 步 — 看结构**（先别反编译全文，太费上下文）
`get_type_info` + `compact=true` + `members_filter=*` → 拿到方法签名、token、字段、属性。

**第 3 步 — 追调用链**
- 谁触发它？→ `find_callers`（**每条消息一个调用**）
- 它调了谁？→ `find_callees`
- 字符串键/配置键在哪用？→ `search_string_literals`（例如 `*SAVE*`、PlayerPrefs 键）
- 虚方法落到哪个实现？→ `find_overrides`
- Unity 生命周期入口（`Update`/`OnGUI`/`OnApplicationQuit`）→ `find_unity_messages`（**它们没有 IL 调用点，只能用这个**）

**第 4 步 — 读关键实现**
`decompile_method`（带 token 最准）。**只有真正要改 IL 时**才 `get_method_il`。

**第 5 步 — 记录结论**
把「类型 / token / 关键方法 / 调用方 / 硬编码常量」写进项目 `README.md` 的维护日志，
不要只留在对话里。

### 3.1 Il2Cpp 的两个重要限制（**不知道这两条会得出错误结论**）

1. **反编译出来的多数是 interop 包装层**：方法体只有 `IL2CPP.il2cpp_runtime_invoke(...)`
   这种转发，**真实游戏逻辑在 native 侧，dnSpy 看不到**。
   → 包装层能告诉你：**类型/字段/方法签名、token、谁在调它** —— 这些对写 Harmony 补丁**已经够用**。
   → 要看真实算法：用 Il2CppDumper 出 `DummyDll` + Ghidra/IDA 看 `GameAssembly.dll`
   （**只读分析，别改 native**）。
2. **字段偏移**：`get_type_fields` 可能带 `Offset`（`il2cpp` 来源是 DummyDll 合成偏移），
   一般**不需要**手算偏移 —— 用 wrapper 的属性 getter/setter 即可。

---

## 4. 写 mod 的标准流程

### 4.1 决定改动落在哪

| 情况 | 做法 |
|---|---|
| 改游戏行为（最常见） | **写 Harmony 补丁**（patch 游戏方法）——首选 |
| 只读状态 | 直接访问 `Il2Cpp.*` 的公开静态/实例成员 |
| 改配置 | `MelonPreferences`（段名 = `AssemblyName`，**不能改名**） |
| 改游戏 DLL 本身 | **红线**，见 §6，仅在别无选择时做 |

### 4.2 新建项目（受管项目模板）

目录：`D:\games\mod调试\<中文目录名>\`，里面放 `Main.cs`（**新项目不要用小写 `mod.cs`**）+ `<游戏内名>.csproj` + `README.md`。

**csproj 硬性约束（缺一条就可能踩坑）**

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <!-- 内联公共属性（不再有共享 props，每个目录独立编译） -->
  <PropertyGroup Condition="'$(GamePath)' == ''">
    <GamePath>D:\SteamLibrary\steamapps\common\Probably Stolen Demo</GamePath>
  </PropertyGroup>
  <PropertyGroup>
    <MLNet>$(GamePath)\MelonLoader\net6</MLNet>
    <MLIl2Cpp>$(GamePath)\MelonLoader\Il2CppAssemblies</MLIl2Cpp>
    <TargetFramework>net6.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <AllowUnsafeBlocks>True</AllowUnsafeBlocks>
    <Nullable>disable</Nullable>
    <ImplicitUsings>disable</ImplicitUsings>
    <PlatformTarget>x64</PlatformTarget>
    <Prefer32Bit>false</Prefer32Bit>
    <AppendTargetFrameworkToOutputPath>false</AppendTargetFrameworkToOutputPath>
  </PropertyGroup>

  <PropertyGroup>
    <AssemblyName>MyModName</AssemblyName>          <!-- ★ 冻结不改：= DLL 名 = MelonPreferences 段名 -->
    <RootNamespace>MyModName</RootNamespace>
    <GenerateAssemblyInfo>false</GenerateAssemblyInfo>  <!-- 只有手写 AssemblyInfo.cs 时才需要，否则 CS0579 -->
  </PropertyGroup>

  <ItemGroup>
    <Reference Include="MelonLoader">        <HintPath>$(MLNet)\MelonLoader.dll</HintPath>        <Private>False</Private></Reference>
    <Reference Include="0Harmony">           <HintPath>$(MLNet)\0Harmony.dll</HintPath>           <Private>False</Private></Reference>
    <Reference Include="Il2CppInterop.Runtime"><HintPath>$(MLNet)\Il2CppInterop.Runtime.dll</HintPath><Private>False</Private></Reference>
    <Reference Include="Il2Cppmscorlib">     <HintPath>$(MLIl2Cpp)\Il2Cppmscorlib.dll</HintPath>  <Private>False</Private></Reference>
    <Reference Include="Assembly-CSharp">    <HintPath>$(MLIl2Cpp)\Assembly-CSharp.dll</HintPath> <Private>False</Private></Reference>
    <Reference Include="UnityEngine.CoreModule"><HintPath>$(MLIl2Cpp)\UnityEngine.CoreModule.dll</HintPath><Private>False</Private></Reference>
    <!-- 需要哪个模块就加哪个：UI / UIModule / IMGUIModule / TextRenderingModule / InputLegacyModule … -->
  </ItemGroup>
</Project>
```

**绝不能违反的**

1. **每个 `<Reference>` 必须 `<Private>False</Private>`** —— 否则游戏 DLL 被拷进 `bin/`，
   实测会多出 **75+ 个 DLL**；分发时更会把游戏 DLL 发出去。
2. **`AssemblyName` 冻结** —— 改它 = 改游戏内 mod 名 + MelonPreferences 配置段 + 跨 mod 按名兼容检测，全断。
3. **公共属性（GamePath / TargetFramework 等）直接内联在各 csproj 里**，
   不引入任何共享 props —— 共享 `_shared/CommonMod.props` 已废除（2026-10-12），
   每个 mod 目录必须能独立编译。
4. **不要引入 `Directory.Build.props`** —— 工作区还有 20+ 未受管项目，会一并改变它们的行为。
   （`AllowUnsafeBlocks` 等**不要**加 `Condition` —— SDK 已给默认值导致兜底静默失效，历史 CS0227 就是这么来的。）
5. 引用别的受管项目用 `<ProjectReference>`，并同样 `Private=false`。

**要挂进批量部署**：在 `deploy-all.ps1` 的 `$PROJECTS` 表里加一行
`[pscustomobject]@{ Dir = '<目录名>'; Dll = '<AssemblyName>'; Probe = $false }`。
`Dir` 必须是**真实目录名**（写错只会「跳过」不报错，很容易误以为成功）。

### 4.3 代码约定

```csharp
using HarmonyLib; using Il2Cpp; using MelonLoader; using UnityEngine;

[assembly: MelonInfo(typeof(MyMod.Main), "显示名", "1.0.0", "作者")]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]

namespace MyMod;
public class Main : MelonMod
{
    public override void OnInitializeMelon() { /* 建配置、打日志 */ }
    public override void OnUpdate() { /* 轮询输入、热键 */ }
    public override void OnGUI()   { /* IMGUI 提示，注意 try/catch */ }
}
```

- Harmony 补丁类加 `[HarmonyPatch(typeof(X), "MethodName")]`；同名重载补丁要带 `MethodType`/参数类型数组。
  **让 AI 用 `dnspy_generate_harmony_patch` 生成签名正确的补丁类**，别手猜参数。
- **所有可能失败的边界一律 `try/catch`**：Il2Cpp 对象随时可能为 null/已销毁
  （游戏切场景、UI 关闭）。工作区惯例是静默 `catch {}`，新代码建议 `catch (Exception ex) { Log?.Error(...); }` 或「只打印一次」留痕。
- 日志用 `MelonLogger`，显示名与游戏内对照（见 `MODS.md` 名称三层表）。
- 配置项放 `MelonPreferences`，并**节流缓存**（别每帧读 `.Value`）。
- 输入用 `Input.GetKeyDown` 等前先 `try/catch`（模块可能没引用到）。
- **一次只改一件事**，改完立刻构建验证。

### 4.4 构建 / 部署 / 验收

```powershell
# ① 只编译（先看有没有编译错误，不写游戏目录）
cd 'D:\games\mod调试'
.\deploy-all.ps1 -DryRun

# ② 单个部署
.\deploy-all.ps1 -Only 自定义仓库          # 可写目录名或 AssemblyName

# ③ 或者直接编译单个工程
dotnet build '自定义仓库\SaveBagExpand.csproj' -c Release -nologo
```

**验收（每次必做，不能只看「构建成功」）**

1. 编译 **0 错误**（0 警告为佳）。
2. 产物路径对：`bin\Release\<AssemblyName>.dll`（`AppendTargetFrameworkToOutputPath=false`，**没有** `net6.0` 子目录）。
3. 部署脚本的**重复加载预检**没有报警（同一 AssemblyName 的另一个文件名，如 `哨兵监控F3.dll`）。
4. 启动游戏，看 `MelonLoader\Latest.log` 里**有本 mod 的加载行**。
5. 游戏内实测现象符合预期；不行就回滚，不要叠加改动。

---

## 5. 改动前的备份与回滚

| 动作 | 命令/位置 |
|---|---|
| 改源码前 | 整目录复制到 `_backup_<用途>_<yyyyMMdd-HHmmss>\` |
| 部署旧版自动备份 | `deploy-all.ps1` 会生成 `X.dll.bak-<时间戳>`（同名只留最新一个） |
| 改了 IL 想撤销 | `revert_method_il`（必须在同一次 dnSpy 会话里，先 patch 才有快照） |
| 改了 DLL 想还原 | `save_assembly` 时自动生成 `<path>.<yyyyMMdd-HHmmss>.bak` |

**回滚是常规操作，不是失败。** 出问题先回滚，再重新诊断。

---

## 6. 🔴 红线：直接修改游戏 DLL

`patch_method_il` / `force_return` / `nop_method` + `save_assembly` 能直接改
`Assembly-CSharp.dll`，但：

1. **优先写 Harmony mod**（§4.1），可分发、可卸载、不污染游戏本体。
2. 确要改 DLL 时：先 `revert`/备份可用，改完 `save_assembly`（会自动 `.bak`）。
3. **游戏更新会覆盖/失效**，且无法随 mod 分发给他人 —— 改完必须告诉用户这一点。
4. `Il2CppAssemblies\*.dll` 是 MelonLoader **启动时现场生成的互操作程序集**，
   某些情况下重启游戏会**重新生成并覆盖**你的修改 —— 改之前**先确认它会不会被重建**，
   或者改为在 `Mods/` 里放 Harmony 补丁。
5. **绝不修改 `GameAssembly.dll` / native 二进制**。

---

## 7. 常见坑速查

| 症状 | 原因 → 处理 |
|---|---|
| `JSON parsing failed ... Unexpected end of JSON input` | **一条消息发了多个 dnspy 调用** → 拆成单发 |
| dnspy 报 404 / 连接失败 | dnSpy 没开，或 `Enable Server` 没勾 |
| `glob` 在 `mod调试` 下返回空 | 中文路径不支持 → 用 `bash` + `-LiteralPath` |
| 中文输出乱码 | `[Console]::OutputEncoding=[Text.Encoding]::UTF8` |
| `Move-Item : 参数不可用` | PS 5.1 无 `-Recurse`（§2.3） |
| 编译 `CS0227 不安全代码` | 项目没走受管 props 或曾加了失效 `Condition` |
| 编译 `CS0579 特性重复` | 手写 `AssemblyInfo.cs` 却没 `GenerateAssemblyInfo=false` |
| `bin/` 里几十个 DLL | 缺 `<Private>False</Private>` |
| 部署后 mod 加载两次 | 同 AssemblyName 的旧文件名还在 `Mods\`（预检会报） |
| mod 编译通过但游戏里没反应 | 看 `Latest.log` 是否加载；再查 Harmony 目标方法签名是否被游戏改了 |
| 找不到 `Update`/`OnGUI` 的调用方 | 它们没有调用点 → 用 `find_unity_messages` |
| 反编译只看到 `il2cpp_runtime_invoke` | 这是 interop 包装层，属正常（§3.1） |
| Playtest 版跑不起来 | 两份游戏 ML 版本不同（0.7.3 / 0.7.4），引用路径要指对那份 |

---

## 8. 一次典型任务的执行清单（照着走）

```
□ 1. 读 D:\games\mod调试\MODS.md —— 确认目标 mod 是否受管、AssemblyName、入口类
□ 2. 健康检查 —— /health 返回 ok；不 ok 就让用户开 dnSpy
□ 3. 分析 —— search_types → get_type_info(compact) → find_callers/find_callees → decompile_method
             （每条消息只发一个 dnspy 调用）
□ 4. 汇报发现 —— 类型/token/调用链/可行注入点，**先给用户看，等确认再动手**
□ 5. 最小改动 —— 只改一个行为；受管项目改源码，新能力才新建项目
□ 6. 构建 —— deploy-all.ps1 -DryRun（或 dotnet build），确认 0 错误
□ 7. 部署 —— deploy-all.ps1 -Only <目录名>
□ 8. 验收 —— Latest.log 有加载行 + 游戏内实测
□ 9. 记录 —— 把新发现的 API/token/坑写进该项目 README.md 维护日志
□ 10. 失败则回滚 —— 不叠加改动（§5）
```

### 汇报格式建议

```
[发现]  Il2Cpp.SaveManager::Save  token 100681353，静态方法
        上游调用方：X（token ...）、Y（token ...）
[判断]  可在 X 之前 Harmony Prefix 拦截，注入点为 <方法名>
[风险]  游戏 0.46D 下验证；未验证 0.47
[计划]  最小改动：新增一个 Harmony 补丁，编译部署后看 Latest.log
```

**先给结论和计划，等用户点头再写代码。**

---

## 9. 权威文档索引（冲突时以它们为准）

| 文件 | 内容 |
|---|---|
| `D:\games\mod调试\MODS.md` | **受管项目清单、csproj 规范、构建部署、兼容分发结论** |
| `D:\games\mod调试\MODS-优化清单.md` | 后续批次进展 |
| `D:\games\mod调试\MODS-合并分析.md` | 「各 mod 独立不合并」的决策依据 |
| `D:\games\mod调试\mod工作流程清单.docx` | 症状收集 → 诊断 → 最小改动 → 验收 + AI 常见陷阱 |
| `D:\games\mod调试\_shared\CommonMod.props` | ~~公共构建属性~~ 已废除：属性内联进各 csproj（2026-10-12） |
| `D:\games\mod调试\deploy-all.ps1` | 统一构建部署（表驱动，含重复加载预检） |
| 各项目 `README.md` | 用途 / 构建 / 部署 / 热键 / 维护日志 |
| `D:\games\mod调试\_archive\` | 已归档的探针结论、旧备份 |

> 本指南描述的是**工具怎么用**；**mod 该做成什么样**，以 `MODS.md` 为准。
