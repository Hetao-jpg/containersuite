# 为什么必须显式声明 [HarmonyDontPatchAll]

> 本文件内容由 `ContainerSuite.Infrastructure/AssemblyInfo.cs` 注释块逐字搬迁（2026-10-13，T-C4）。
> 源码保留摘要与指针；`[assembly: HarmonyDontPatchAll]` 特性本身留在源码。

═══════════════════════════════════════════════════════════════════════
★★ 2026-10-10：必须显式声明 [HarmonyDontPatchAll]，否则**每个补丁装两遍**。

机制（反编译 MelonLoader 0.7.4 net6 的 MelonBase.HarmonyInit 确认）：
  MelonLoader 给每个 Melon 程序集自动建一个 Harmony ID =
    "<程序集 FullName>:<MelonInfo.Name>"
      ⇒ 本项目 = "CustomInfrastructure, Version=<AssemblyVersion>, Culture=neutral,
                  PublicKeyToken=null:CustomInfrastructure"
        （注释里一律用 <AssemblyVersion> 占位，不写死具体数字 —— 写死的旧版本号
          会与实际 AssemblyVersion 漂移；check-suite-version.ps1 会拒绝硬编码的
          旧 1.7.x 版本号字面量。）
  然后 **除非** 程序集带 [HarmonyDontPatchAll]，它会对本程序集**所有**
  [HarmonyPatch] 类无条件 CreateClassProcessor(...).Patch() 一遍。
    · MelonAssembly.HarmonyDontPatchAll 的默认值 = true，但第 315 行会把它
      覆写成 `harmonyDontPatchAllAttribute != null` —— 即"**缺**该特性 ⇒ false ⇒
      MelonLoader 真的会 PatchAll 整个程序集"。

而本套件**自己**又有三处显式注册（见 Mod.OnInitializeMelon / SaveBagExpandCore
.ApplyPatches / DiyUpgradeMod.InstallPatches，各自用独立 Harmony ID
"CustomInfrastructure.Main" / "SaveBagExpand.Patch" / "DIYUpgrade"）。
  ⇒ 两个 ID 各挂一次 ⇒ **每个 Prefix/Postfix/Transpiler 都跑两遍**。

实机取证（2026-10-10 的 CND-PROBE 补丁归属自检，Harmony.GetPatchInfo）：
  EmporiumEntry.ResetFrontCounterGrid —— Prefix=2 Postfix=4
    · Prefix owner=CustomInfrastructure, Version=<AssemblyVersion>...:CustomInfrastructure  ← MelonLoader 自动
    · Prefix owner=SaveBagExpand.Patch                                            ← 我们自己显式
  两条 owner 一对照即坐实"双注册"。（同帧逐字重复的日志 = 双 Prefix/双 Postfix 夹一具 body。）

★ 为什么以前没炸：多数 Postfix 是幂等的（再设一次同样的形状/位置无害），
  所以这个 latent bug 一直藏着；但探针一挂、日志一开就看出来了。
  ⇒ 它不是"探针的锅"，是套件自合并（2026-10-06，4 DLL → 1 DLL）起就有的老问题。

★ 修法选择：**声明 HarmonyDontPatchAll**（关掉 MelonLoader 的自动扫描），
  保留我们自己的**命名空间过滤**注册（那正是为规避 B1 "同一目标两 ID"而写的，
  见 SaveBagExpand/mod.cs 的 ApplyPatches 注释）。这样：
    · 每个补丁**只装一次**；
    · 跨命名空间的重复（CI/SBE/DIY 三份扫描互相看到对方）**依然被隔离**；
    · 与合并前的行为（4 个独立 DLL、各自 PatchAll 自己程序集）**完全等价**。
  ★ 反过来的方案（删掉我们的显式注册、全靠 MelonLoader 自动）不可取：
    那样会同时失去 OptionalPatchNames 跳过能力与命名空间过滤，风险更大。
═══════════════════════════════════════════════════════════════════════