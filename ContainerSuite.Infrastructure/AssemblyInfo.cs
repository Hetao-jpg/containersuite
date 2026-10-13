// ═══════════════════════════════════════════════════════════════════════
// ContainerSuite —— 合并版唯一的程序集声明文件。
//
// 2026-10-06：4 个工程的 AssemblyInfo.cs 合并成这一份。
//   另外三个已删除；若再加回来，编译期直接 CS0579（版本 / 标题等特性重复注册）。
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

// ★★ 必须显式声明 [HarmonyDontPatchAll]，否则每个补丁会被 MelonLoader 自动装两遍。
// 完整机制（反编译 HarmonyInit + CND-PROBE 实机取证 + 修法选择）：
//   docs/harmony-dontpatchall.md
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
