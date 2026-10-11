// ═══════════════════════════════════════════════════════════════════════
// ContainerSuite —— 合并版唯一的程序集声明文件。
//
// 2026-10-06：原本 4 个工程各有一份 AssemblyInfo.cs（Shared / Infrastructure /
//   SaveBag / DIYUpgrade），合并成单 DLL 后 4 份会互相冲突 CS0579
//   （AssemblyVersion / FileVersion / InformationalVersion / Title / Product /
//    Company 各重复 3 次），所以收敛成这一份。
//   另外三个 AssemblyInfo.cs 已删除；若再加回来，编译期直接 CS0579。
//   本文件是全工作区**唯一**允许写 [assembly: ...] 特性的地方。
// ═══════════════════════════════════════════════════════════════════════
//
// S5：对外只有一个 Mod —— CustomInfrastructure。
//     MelonInfo 改名不会丢配置：MelonPreferences 的分类名是各 Config 里硬编码的字符串
//     （"CustomInfrastructure" / "SaveBagExpand" / "DIYUpgrade"），与 AssemblyName 无关。
// ★ 2026-10-02：名字从 "ContainerSuite" 改成 "CustomInfrastructure"。
//   ModManager 面板的显示名 = 站点(nicokobo)按这个名字精确匹配到的中文名；
//   站点收录的 built-in name 是 "CustomInfrastructure"（中文名「自定义基础设施大小」），
//   叫 ContainerSuite 时匹配不到 → 面板里显示英文。
//
// 合并后注意：AssemblyName 也叫 CustomInfrastructure，所以
//   "DLL 文件名" 与 "MelonInfo 名" 现在是同一个字符串 CustomInfrastructure。
//   另外两个 cfg 段名 SaveBagExpand / DIYUpgrade 不再等于 DLL 文件名，
//   ModManager 的段落归属需要靠已知 CategoryId 白名单兜底（见 DiyUpgrade.CfgMigration.cs）。
using System.Reflection;
using System.Runtime.InteropServices;
using CustomInfrastructure;
using MelonLoader;

[assembly: MelonInfo(typeof(Mod), "CustomInfrastructure", "1.7.8", "核桃", null)]
[assembly: MelonGame(null, "Probably Stolen")]

// ═══════════════════════════════════════════════════════════════════════
// ★★ 2026-10-10：必须显式声明 [HarmonyDontPatchAll]，否则**每个补丁装两遍**。
//
// 机制（反编译 MelonLoader 0.7.4 net6 的 MelonBase.HarmonyInit 确认）：
//   MelonLoader 给每个 Melon 程序集自动建一个 Harmony ID =
//     "<程序集 FullName>:<MelonInfo.Name>"
//       ⇒ 本项目 = "CustomInfrastructure, Version=<AssemblyVersion>, Culture=neutral,
//                   PublicKeyToken=null:CustomInfrastructure"
//         （注释里一律用 <AssemblyVersion> 占位，不写死具体数字 —— 写死的旧版本号
//           会与实际 AssemblyVersion 漂移；check-suite-version.ps1 会拒绝硬编码的
//           旧 1.7.x 版本号字面量。）
//   然后 **除非** 程序集带 [HarmonyDontPatchAll]，它会对本程序集**所有**
//   [HarmonyPatch] 类无条件 CreateClassProcessor(...).Patch() 一遍。
//     · MelonAssembly.HarmonyDontPatchAll 的默认值 = true，但第 315 行会把它
//       覆写成 `harmonyDontPatchAllAttribute != null` —— 即"**缺**该特性 ⇒ false ⇒
//       MelonLoader 真的会 PatchAll 整个程序集"。
//
// 而本套件**自己**又有三处显式注册（见 Mod.OnInitializeMelon / SaveBagExpandCore
// .ApplyPatches / DiyUpgradeMod.InstallPatches，各自用独立 Harmony ID
// "CustomInfrastructure.Main" / "SaveBagExpand.Patch" / "DIYUpgrade"）。
//   ⇒ 两个 ID 各挂一次 ⇒ **每个 Prefix/Postfix/Transpiler 都跑两遍**。
//
// 实机取证（2026-10-10 的 CND-PROBE 补丁归属自检，Harmony.GetPatchInfo）：
//   EmporiumEntry.ResetFrontCounterGrid —— Prefix=2 Postfix=4
//     · Prefix owner=CustomInfrastructure, Version=<AssemblyVersion>...:CustomInfrastructure  ← MelonLoader 自动
//     · Prefix owner=SaveBagExpand.Patch                                            ← 我们自己显式
//   两条 owner 一对照即坐实"双注册"。（同帧逐字重复的日志 = 双 Prefix/双 Postfix 夹一具 body。）
//
// ★ 为什么以前没炸：多数 Postfix 是幂等的（再设一次同样的形状/位置无害），
//   所以这个 latent bug 一直藏着；但探针一挂、日志一开就看出来了。
//   ⇒ 它不是"探针的锅"，是套件自合并（2026-10-06，4 DLL → 1 DLL）起就有的老问题。
//
// ★ 修法选择：**声明 HarmonyDontPatchAll**（关掉 MelonLoader 的自动扫描），
//   保留我们自己的**命名空间过滤**注册（那正是为规避 B1 "同一目标两 ID"而写的，
//   见 SaveBagExpand/mod.cs 的 ApplyPatches 注释）。这样：
//     · 每个补丁**只装一次**；
//     · 跨命名空间的重复（CI/SBE/DIY 三份扫描互相看到对方）**依然被隔离**；
//     · 与合并前的行为（4 个独立 DLL、各自 PatchAll 自己程序集）**完全等价**。
//   ★ 反过来的方案（删掉我们的显式注册、全靠 MelonLoader 自动）不可取：
//     那样会同时失去 OptionalPatchNames 跳过能力与命名空间过滤，风险更大。
// ═══════════════════════════════════════════════════════════════════════
[assembly: HarmonyDontPatchAll]

[assembly: AssemblyTitle("ContainerSuite")]
[assembly: AssemblyProduct("ContainerSuite")]
[assembly: AssemblyCompany("核桃")]
[assembly: AssemblyConfiguration("Release")]
[assembly: ComVisible(false)]
[assembly: Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890")]

// S5：程序集版本统一跟随 SuiteVersion.Suite（1.7.8）。
//     改了 SuiteVersion.Suite 必须同步这里，否则 _shared\check-suite-version.ps1 编译期报错。
[assembly: AssemblyVersion("1.7.8.0")]
[assembly: AssemblyFileVersion("1.7.8.0")]
[assembly: AssemblyInformationalVersion("1.7.8")]
