using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using FullMachineUpgrades;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Mod), "更全面的机器升级", "0.3.13", "local", null)]
[assembly: MelonGame(null, "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("FullMachineUpgrades")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("0.3.13.0")]
[assembly: AssemblyInformationalVersion("0.3.13")]
[assembly: AssemblyProduct("FullMachineUpgrades")]
[assembly: AssemblyTitle("FullMachineUpgrades")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.3.13.0")]
[module: UnverifiableCode]
[module: RefSafetyRules(11)]
namespace Microsoft.CodeAnalysis
{
	[CompilerGenerated]
	[Embedded]
	internal sealed class EmbeddedAttribute : Attribute
	{
	}
}
namespace System.Runtime.CompilerServices
{
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableAttribute : Attribute
	{
		public readonly byte[] NullableFlags;

		public NullableAttribute(byte P_0)
		{
			NullableFlags = new byte[1] { P_0 };
		}

		public NullableAttribute(byte[] P_0)
		{
			NullableFlags = P_0;
		}
	}
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Method | AttributeTargets.Interface | AttributeTargets.Delegate, AllowMultiple = false, Inherited = false)]
	internal sealed class NullableContextAttribute : Attribute
	{
		public readonly byte Flag;

		public NullableContextAttribute(byte P_0)
		{
			Flag = P_0;
		}
	}
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NativeIntegerAttribute : Attribute
	{
		public readonly bool[] TransformFlags;

		public NativeIntegerAttribute()
		{
			TransformFlags = new bool[1] { true };
		}

		public NativeIntegerAttribute(bool[] P_0)
		{
			TransformFlags = P_0;
		}
	}
	[CompilerGenerated]
	[Embedded]
	[AttributeUsage(AttributeTargets.Module, AllowMultiple = false, Inherited = false)]
	internal sealed class RefSafetyRulesAttribute : Attribute
	{
		public readonly int Version;

		public RefSafetyRulesAttribute(int P_0)
		{
			Version = P_0;
		}
	}
}
namespace FullMachineUpgrades
{
	internal static class BonusLedger
	{
		internal readonly struct Delta
		{
			internal readonly int Quality;

			internal readonly int Performance;

			internal readonly int Power;

			internal Delta(int quality, int performance, int power)
			{
				Quality = quality;
				Performance = performance;
				Power = power;
			}
		}

		internal const int PurifierPartsPerTier = 10;

		internal const int PurifierQualityPerTier = 15;

		internal const int PurifierPerformancePerIngot = 5;

		private const string DrawnBaseTag = "MACHINERY_DRAWN_POWER_BASE";

		private const string DrawnTag = "machinery_drawn_power";

		private const string EffRatioTag = "MACHINE_EFFICIENCY_RATIO_INT";

		private const string PerfRatioTag = "MACHINE_PERFORMANCE_RATIO_INT";

		private const string PerfReducesPowerTag = "MACHINE_PERFORMANCE_REDUCES_POWER_TAG";

		private static readonly Dictionary<string, Delta> PerUnit = new Dictionary<string, Delta>(StringComparer.Ordinal)
		{
			["FMU_FURNACE_INGOT"] = new Delta(5, 0, 10),
			["FMU_FURNACE_CIRCUIT"] = new Delta(5, 0, 0),
			["FMU_MOISTURE_CIRCUIT"] = new Delta(7, 0, 0),
			["FMU_MOISTURE_WIRE"] = new Delta(0, 10, 0),
			["FMU_PROJECTOR_CIRCUIT"] = new Delta(5, 0, -5),
			["FMU_PROJECTOR_PLASTIC"] = new Delta(10, 0, 0),
			["FMU_PROJECTOR_WIRE"] = new Delta(0, 0, 5),
			["FMU_WINERACK_CIRCUIT"] = new Delta(15, 0, -5),
			["FMU_WINERACK_WIRE"] = new Delta(0, 0, 10),
			["FMU_PURIFIER_INGOT"] = new Delta(0, 5, 0)
		};

		internal static void Grant(GameItem machine, string countTag, int units)
		{
			if (units > 0 && PerUnit.TryGetValue(countTag, out var value))
			{
				Grant(machine, value.Quality * units, value.Performance * units, value.Power * units);
			}
		}

		internal static void Grant(GameItem machine, int quality, int performance, int power)
		{
			if (quality != 0)
			{
				TagUtil.AddInt(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT", quality);
			}
			if (performance != 0)
			{
				TagUtil.AddInt(machine, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT", performance);
			}
			if (power != 0)
			{
				TagUtil.AddInt(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT", power);
				RecomputeDrawnPower(machine);
			}
		}

		internal static void Compute(GameItem machine, out int quality, out int performance, out int power)
		{
			quality = 0;
			performance = 0;
			power = 0;
			foreach (KeyValuePair<string, Delta> item in PerUnit)
			{
				int num = TagUtil.GetInt(machine, item.Key);
				if (num > 0)
				{
					quality += item.Value.Quality * num;
					performance += item.Value.Performance * num;
					power += item.Value.Power * num;
				}
			}
			int num2 = TagUtil.GetInt(machine, "FMU_PURIFIER_FILTER_PARTS");
			if (num2 >= 10)
			{
				quality += num2 / 10 * 15;
			}
		}

		internal static bool HasUpgrades(GameItem machine)
		{
			foreach (string key in PerUnit.Keys)
			{
				if (TagUtil.GetInt(machine, key) > 0)
				{
					return true;
				}
			}
			return TagUtil.GetInt(machine, "FMU_PURIFIER_FILTER_PARTS") >= 10;
		}

		internal static void Reapply(GameItem machine)
		{
			try
			{
				if (machine == null || !HasUpgrades(machine))
				{
					return;
				}
				Compute(machine, out var quality, out var performance, out var power);
				if (quality != 0 || performance != 0 || power != 0)
				{
					Grant(machine, quality, performance, power);
					if (power != 0)
					{
						RecomputeDrawnPower(machine);
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("BonusLedger.Reapply: " + ex.Message);
			}
		}

		internal static void RecomputeDrawnPower(GameItem machine)
		{
			try
			{
				TagState tagReadonly = machine.GetTagReadonly("MACHINERY_DRAWN_POWER_BASE");
				if (tagReadonly == null)
				{
					return;
				}
				int valueInt = tagReadonly.valueInt;
				if (valueInt > 0)
				{
					int num = TagUtil.GetInt(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT");
					int num2 = TagUtil.GetInt(machine, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT");
					int num3 = valueInt - valueInt * num / 100;
					int num4 = TagUtil.GetInt(machine, "MACHINE_EFFICIENCY_RATIO_INT");
					if (num4 > 0)
					{
						num3 = valueInt - (int)((float)(valueInt * num / num4) / 100f);
					}
					int num5 = TagUtil.GetInt(machine, "MACHINE_PERFORMANCE_RATIO_INT");
					if (num5 > 0 && num2 != 0 && machine.IsTag("MACHINE_PERFORMANCE_REDUCES_POWER_TAG"))
					{
						num3 += (int)((float)(valueInt * num2 / num5) / 100f);
					}
					if (num3 < 1)
					{
						num3 = 1;
					}
					TagUtil.SetInt(machine, "machinery_drawn_power", num3);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("BonusLedger.RecomputeDrawnPower: " + ex.Message);
			}
		}

		internal static void AppendTooltip(RichTextBuilder b, GameItem machine)
		{
			try
			{
				Compute(machine, out var quality, out var performance, out var power);
				if (quality != 0 || performance != 0 || power != 0)
				{
					StringBuilder stringBuilder = new StringBuilder("加成");
					if (quality != 0)
					{
						stringBuilder.Append("  ·  质量 ").Append(Signed(quality)).Append('%');
					}
					if (performance != 0)
					{
						stringBuilder.Append("  ·  性能 ").Append(Signed(performance)).Append('%');
					}
					if (power != 0)
					{
						stringBuilder.Append("  ·  耗电 ").Append((power > 0) ? "-" : "+").Append(Math.Abs(power))
							.Append('%');
					}
					b.AddLine(stringBuilder.ToString(), true, (ColorPalette)5538048, true, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("BonusLedger.AppendTooltip: " + ex.Message);
			}
		}

		internal static string Describe(string countTag)
		{
			if (!PerUnit.TryGetValue(countTag, out var value))
			{
				return "";
			}
			return Describe(value);
		}

		internal static string Describe(Delta d)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (d.Quality != 0)
			{
				stringBuilder.Append("质量").Append(Signed(d.Quality)).Append('%');
			}
			if (d.Performance != 0)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append("性能").Append(Signed(d.Performance)).Append('%');
			}
			if (d.Power != 0)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(' ');
				}
				stringBuilder.Append("耗电").Append((d.Power > 0) ? "-" : "+").Append(Math.Abs(d.Power))
					.Append('%');
			}
			return stringBuilder.ToString();
		}

		internal static string DescribePurifierFilter()
		{
			return $"每 {10} 点 → 质量+{15}%";
		}

		private static string Signed(int v)
		{
			if (v <= 0)
			{
				return v.ToString();
			}
			return "+" + v;
		}
	}
	internal static class BonusPersistPatch
	{
		internal static void Apply(Harmony harmony)
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = FindTarget();
				if (methodInfo == null)
				{
					Mod.Log.Warning("BonusPersist: ModuleHelper.ComputeModuleEffect(List<GameItem>, GameItem) 未找到");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(BonusPersistPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg($"BonusPersist: 已挂钩 ModuleHelper.{methodInfo.Name}（{methodInfo.GetParameters().Length} 参）—— 加成被模块重算抹掉后自动补回");
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("BonusPersist: " + ex.Message);
			}
		}

		private static MethodInfo? FindTarget()
		{
			MethodInfo[] methods = typeof(ModuleHelper).GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				if (string.Equals(methodInfo.Name, "ComputeModuleEffect", StringComparison.Ordinal))
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (parameters.Length == 2 && parameters[1].ParameterType == typeof(GameItem))
					{
						return methodInfo;
					}
				}
			}
			return null;
		}

		public static void Postfix(GameItem __1)
		{
			BonusLedger.Reapply(__1);
		}
	}
	internal static class Config
	{
		internal static MelonPreferences_Category Prefs = null;

		internal static MelonPreferences_Entry<string> PowerBlocTier1 = null;

		internal static MelonPreferences_Entry<string> PowerBlocTier2 = null;

		internal static MelonPreferences_Entry<string> RechargerExpand = null;

		internal static MelonPreferences_Entry<string> RechargerFast = null;

		internal static MelonPreferences_Entry<int> RechargerVisibleCols = null;

		internal static MelonPreferences_Entry<string> WineWidth = null;

		internal static MelonPreferences_Entry<int> WineWidthMaxLevels = null;

		internal static MelonPreferences_Entry<string> WineBottleCol = null;

		internal static MelonPreferences_Entry<int> WineBottleColMaxLevels = null;

		internal static MelonPreferences_Entry<string> WineCircuit = null;

		internal static MelonPreferences_Entry<string> WineWire = null;

		internal static readonly (string Id, string Label, string Recipe, int Max)[] ModuleBayDefs = new (string, string, string, int)[10]
		{
			("furnace", "熔炉", "wire:2", 6),
			("hydroponic", "水培", "glass_shard:2", 6),
			("moisture_farm", "集水器", "glass_shard:2", 6),
			("water_purifier", "净水器", "glass_shard:2", 6),
			("security_alarm", "警报系统", "wire:2", 6),
			("mirage_projector", "幻影投影仪", "glass_shard:2", 6),
			("projector", "投影仪", "glass_shard:2", 6),
			("desequencer", "解码器", "common_electronic:1", 6),
			("evaporator", "蒸发器", "glass_shard:2", 6),
			("printer", "3D打印机", "glass_shard:2", 6)
		};

		internal static readonly Dictionary<string, MelonPreferences_Entry<string>> ModuleBayRecipe = new Dictionary<string, MelonPreferences_Entry<string>>(StringComparer.Ordinal);

		internal static readonly Dictionary<string, MelonPreferences_Entry<int>> ModuleBayMax = new Dictionary<string, MelonPreferences_Entry<int>>(StringComparer.Ordinal);

		private static readonly HashSet<string> ModuleBaySkipped = new HashSet<string>(StringComparer.Ordinal);

		internal static MelonPreferences_Entry<string> PurifierFilter = null;

		internal static MelonPreferences_Entry<int> PurifierFilterMaxParts = null;

		internal static MelonPreferences_Entry<string> PurifierIngot = null;

		internal static MelonPreferences_Entry<int> PurifierIngotMax = null;

		internal static MelonPreferences_Entry<string> FurnaceIngot = null;

		internal static MelonPreferences_Entry<int> FurnaceIngotMax = null;

		internal static MelonPreferences_Entry<string> FurnaceCircuit = null;

		internal static MelonPreferences_Entry<int> FurnaceCircuitMax = null;

		internal static MelonPreferences_Entry<string> MoistureCircuit = null;

		internal static MelonPreferences_Entry<int> MoistureCircuitMax = null;

		internal static MelonPreferences_Entry<string> MoistureWire = null;

		internal static MelonPreferences_Entry<int> MoistureWireMax = null;

		internal static MelonPreferences_Entry<string> ProjectorCircuit = null;

		internal static MelonPreferences_Entry<string> ProjectorPlastic = null;

		internal static MelonPreferences_Entry<int> ProjectorPlasticMax = null;

		internal static MelonPreferences_Entry<string> ProjectorWire = null;

		internal static MelonPreferences_Entry<string> ProjectorIngot = null;

		internal static MelonPreferences_Entry<string> WineLabelUnlock = null;

		internal static MelonPreferences_Entry<float> WineLabelChance = null;

		internal static MelonPreferences_Entry<string> PurifierClearUnlock = null;

		internal static MelonPreferences_Entry<float> PurifierClearChance = null;

		internal static MelonPreferences_Entry<string> DeseqInstantUnlock = null;

		internal static MelonPreferences_Entry<float> DeseqInstantChance = null;

		internal static string ModuleBayLabel(string id)
		{
			(string, string, string, int)[] moduleBayDefs = ModuleBayDefs;
			for (int i = 0; i < moduleBayDefs.Length; i++)
			{
				(string, string, string, int) tuple = moduleBayDefs[i];
				if (string.Equals(tuple.Item1, id, StringComparison.Ordinal))
				{
					return tuple.Item2;
				}
			}
			return id;
		}

		private static void CreateModuleBayEntries(string id)
		{
			string value = ModuleBayLabel(id);
			string text = "nuts_metal:2";
			int num = 6;
			(string, string, string, int)[] moduleBayDefs = ModuleBayDefs;
			for (int i = 0; i < moduleBayDefs.Length; i++)
			{
				(string, string, string, int) tuple = moduleBayDefs[i];
				if (string.Equals(tuple.Item1, id, StringComparison.Ordinal))
				{
					text = tuple.Item3;
					num = tuple.Item4;
					break;
				}
			}
			ModuleBayRecipe[id] = Prefs.CreateEntry<string>("ModuleBay_" + id, text, $"模组仓加宽·{value}（{id}，每次；改完重启）", (string)null, false, false, (ValueValidator)null, (string)null);
			ModuleBayMax[id] = Prefs.CreateEntry<int>("ModuleBayMax_" + id, num, $"模组仓加宽上限·{value}（{id}；每台机器各自计算）", (string)null, false, false, (ValueValidator)null, (string)null);
		}

		internal static void RecheckOptionalMachines()
		{
			ModPresence.ResetMisses();
			List<string> list = new List<string>();
			foreach (string item in new List<string>(ModuleBaySkipped))
			{
				if (ModPresence.MachineAvailable(item))
				{
					ModuleBaySkipped.Remove(item);
					CreateModuleBayEntries(item);
					list.Add(ModuleBayLabel(item) + "(" + item + ")");
				}
			}
			if (list.Count > 0)
			{
				Mod.Log.Msg($"[FMU] 模组仓加宽：{list.Count} 台已补启用（提供它们的 mod 已在） → " + string.Join(", ", list));
			}
			LogSkippedModuleBay("复核");
		}

		private static void LogSkippedModuleBay(string phase)
		{
			if (ModuleBaySkipped.Count == 0)
			{
				return;
			}
			List<string> list = new List<string>();
			foreach (string item in ModuleBaySkipped)
			{
				list.Add($"{ModuleBayLabel(item)}({item}) 需 {ModPresence.ProviderOf(item) ?? "?"}");
			}
			Mod.Log.Msg($"[FMU] 模组仓加宽：{phase}时 {ModuleBaySkipped.Count} 台机器的提供 mod 未加载，已跳过（不建配置项 / 不出 tooltip / 不接拖放） → {string.Join("、", list)}");
		}

		internal static void Init()
		{
			Prefs = MelonPreferences.CreateCategory("FullMachineUpgrades");
			PowerBlocTier1 = Prefs.CreateEntry<string>("PowerBlocTier1", "printer_plastic:4,nuts_metal:2", "能量阵列→同容器：物品id:数量，逗号分隔（多种=都要凑齐）", (string)null, false, false, (ValueValidator)null, (string)null);
			PowerBlocTier2 = Prefs.CreateEntry<string>("PowerBlocTier2", "common_electronic:2,nuts_metal:3", "能量阵列→全局：物品id:数量，逗号分隔（多种=都要凑齐）", (string)null, false, false, (ValueValidator)null, (string)null);
			RechargerExpand = Prefs.CreateEntry<string>("RechargerExpand", "printer_plastic:2,nuts_metal:2", "充能器扩格", (string)null, false, false, (ValueValidator)null, (string)null);
			RechargerFast = Prefs.CreateEntry<string>("RechargerFast", "electronic:1,wire:2", "充能器快充", (string)null, false, false, (ValueValidator)null, (string)null);
			RechargerVisibleCols = Prefs.CreateEntry<int>("RechargerVisibleCols", 4, "充电器外观：电池图标可见列数（1 列 = 16px；0 = 不裁）", (string)null, false, false, (ValueValidator)null, (string)null);
			WineWidth = Prefs.CreateEntry<string>("WineWidth", "nuts_metal:2", "酒架·模组列加宽（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			WineWidthMaxLevels = Prefs.CreateEntry<int>("WineWidthMaxLevels", 6, "酒架·模组列加宽次数上限", (string)null, false, false, (ValueValidator)null, (string)null);
			WineBottleCol = Prefs.CreateEntry<string>("WineBottleCol", "empty_beer_bottle:1", "酒架·酒瓶列加宽（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			WineBottleColMaxLevels = Prefs.CreateEntry<int>("WineBottleColMaxLevels", 6, "酒架·酒瓶列加宽次数上限", (string)null, false, false, (ValueValidator)null, (string)null);
			WineCircuit = Prefs.CreateEntry<string>("WineCircuit", "electronic:1", "酒架电路（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			WineWire = Prefs.CreateEntry<string>("WineWire", "wire:1", "酒架线缆（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			ModuleBayRecipe.Clear();
			ModuleBayMax.Clear();
			ModuleBaySkipped.Clear();
			(string, string, string, int)[] moduleBayDefs = ModuleBayDefs;
			for (int i = 0; i < moduleBayDefs.Length; i++)
			{
				string item = moduleBayDefs[i].Item1;
				if (!ModPresence.MachineAvailable(item))
				{
					ModuleBaySkipped.Add(item);
				}
				else
				{
					CreateModuleBayEntries(item);
				}
			}
			LogSkippedModuleBay("启动");
			PurifierFilter = Prefs.CreateEntry<string>("PurifierFilter", "water_filter:1", "净水器普通滤水器（每个 +1 滤芯积分）", (string)null, false, false, (ValueValidator)null, (string)null);
			PurifierFilterMaxParts = Prefs.CreateEntry<int>("PurifierFilterMaxParts", 50, "滤芯积分上限（每 10 积分升 1 档）", (string)null, false, false, (ValueValidator)null, (string)null);
			PurifierIngot = Prefs.CreateEntry<string>("PurifierIngot", "metal_ingot:1", "净水器金属锭（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			PurifierIngotMax = Prefs.CreateEntry<int>("PurifierIngotMax", 10, "净水器金属锭次数上限", (string)null, false, false, (ValueValidator)null, (string)null);
			FurnaceIngot = Prefs.CreateEntry<string>("FurnaceIngot", "nuts_metal:2", "熔炉保温（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			FurnaceIngotMax = Prefs.CreateEntry<int>("FurnaceIngotMax", 10, "熔炉保温上限", (string)null, false, false, (ValueValidator)null, (string)null);
			FurnaceCircuit = Prefs.CreateEntry<string>("FurnaceCircuit", "electronic:1", "熔炉传感器（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			FurnaceCircuitMax = Prefs.CreateEntry<int>("FurnaceCircuitMax", 10, "熔炉传感器上限", (string)null, false, false, (ValueValidator)null, (string)null);
			MoistureCircuit = Prefs.CreateEntry<string>("MoistureCircuit", "printer_plastic:3", "集水器探头（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			MoistureCircuitMax = Prefs.CreateEntry<int>("MoistureCircuitMax", 10, "集水器探头上限", (string)null, false, false, (ValueValidator)null, (string)null);
			MoistureWire = Prefs.CreateEntry<string>("MoistureWire", "metal_ingot:1", "集水器铜管（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			MoistureWireMax = Prefs.CreateEntry<int>("MoistureWireMax", 5, "集水器铜管上限", (string)null, false, false, (ValueValidator)null, (string)null);
			ProjectorCircuit = Prefs.CreateEntry<string>("ProjectorCircuit", "electronic:1", "投影成像电路（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			ProjectorPlastic = Prefs.CreateEntry<string>("ProjectorPlastic", "nuts_metal:2", "投影透镜（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			ProjectorPlasticMax = Prefs.CreateEntry<int>("ProjectorPlasticMax", 5, "投影透镜上限", (string)null, false, false, (ValueValidator)null, (string)null);
			ProjectorWire = Prefs.CreateEntry<string>("ProjectorWire", "wire:1", "投影供电线（每次）", (string)null, false, false, (ValueValidator)null, (string)null);
			ProjectorIngot = Prefs.CreateEntry<string>("ProjectorIngot", "metal_ingot:1", "投影散热片（每次，电路+线各等效+1）", (string)null, false, false, (ValueValidator)null, (string)null);
			WineLabelUnlock = Prefs.CreateEntry<string>("WineLabelUnlock", "glass_shard:4", "酒架独特：陈酿时概率挂麻醉品/水展示类型。材料 id:数量，只升一次", (string)null, false, false, (ValueValidator)null, (string)null);
			WineLabelChance = Prefs.CreateEntry<float>("WineLabelChance", 0.25f, "酒架独特触发概率 0~1（默认 0.25）", (string)null, false, false, (ValueValidator)null, (string)null);
			PurifierClearUnlock = Prefs.CreateEntry<string>("PurifierClearUnlock", "water_filter_adv:1", "净水器独特：净化时概率清空一种可提纯杂质。材料 id:数量，只升一次", (string)null, false, false, (ValueValidator)null, (string)null);
			PurifierClearChance = Prefs.CreateEntry<float>("PurifierClearChance", 0.25f, "净水器独特触发概率 0~1（默认 0.25）", (string)null, false, false, (ValueValidator)null, (string)null);
			DeseqInstantUnlock = Prefs.CreateEntry<string>("DeseqInstantUnlock", "common_electronic:1,printer_plastic:2", "解序器独特：过夜进度时概率直接完成。材料 id:数量，只升一次", (string)null, false, false, (ValueValidator)null, (string)null);
			DeseqInstantChance = Prefs.CreateEntry<float>("DeseqInstantChance", 0.25f, "解序器独特触发概率 0~1（默认 0.25）", (string)null, false, false, (ValueValidator)null, (string)null);
		}
	}
	internal static class RecipeUtil
	{
		private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { ["glass"] = "glass_shard" };

		private static readonly HashSet<string> AliasLogged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		internal static List<(string Id, int Count)> Parse(string? raw)
		{
			List<(string, int)> list = new List<(string, int)>();
			if (string.IsNullOrWhiteSpace(raw))
			{
				return list;
			}
			string[] array = raw.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length == 0)
				{
					continue;
				}
				int num = text.IndexOf(':');
				if (num <= 0 || num >= text.Length - 1)
				{
					Mod.Log.Warning("Recipe 无法解析片段 '" + text + "'（需要 id:数量）");
					continue;
				}
				string item = text.Substring(0, num).Trim();
				if (!int.TryParse(text.Substring(num + 1).Trim(), out var result) || result < 1)
				{
					Mod.Log.Warning("Recipe 数量无效 '" + text + "'");
				}
				else
				{
					list.Add((item, result));
				}
			}
			return list;
		}

		internal static bool IdsMatch(string recipeId, string matId)
		{
			if (string.IsNullOrEmpty(recipeId) || string.IsNullOrEmpty(matId))
			{
				return false;
			}
			string text = Canonical(recipeId);
			string text2 = Canonical(matId);
			if (!string.Equals(text, recipeId, StringComparison.OrdinalIgnoreCase))
			{
				LogAliasOnce(recipeId, text);
			}
			if (string.Equals(text, text2, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (string.Equals(text, "electronic", StringComparison.OrdinalIgnoreCase))
			{
				if (!string.Equals(text2, "electronic", StringComparison.OrdinalIgnoreCase))
				{
					return text2.EndsWith("_electronic", StringComparison.OrdinalIgnoreCase);
				}
				return true;
			}
			if ((text.EndsWith("_electronic", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "electronic", StringComparison.OrdinalIgnoreCase)) && (string.Equals(text2, "electronic", StringComparison.OrdinalIgnoreCase) || text2.EndsWith("_electronic", StringComparison.OrdinalIgnoreCase)))
			{
				return true;
			}
			return false;
		}

		private static string Canonical(string id)
		{
			if (!Aliases.TryGetValue(id, out string value))
			{
				return id;
			}
			return value;
		}

		private static void LogAliasOnce(string badId, string goodId)
		{
			if (AliasLogged.Add(badId))
			{
				Mod.Log.Msg($"[FMU] 配方里的 id '{badId}' 在游戏物品里不存在 → 已按 '{goodId}' 对待（两个都能用；cfg 不用改，进度也不会丢）");
			}
		}

		internal static bool ContainsId(string? raw, string matId)
		{
			foreach (var item in Parse(raw))
			{
				if (IdsMatch(item.Id, matId))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool ContainsAnyConfigured(string matId, params MelonPreferences_Entry<string>[] entries)
		{
			foreach (MelonPreferences_Entry<string> val in entries)
			{
				if (val != null && val.Value != null && ContainsId(val.Value, matId))
				{
					return true;
				}
			}
			return false;
		}

		private static string ProgTag(string prefix, string recipeKey)
		{
			return "FMU_RP_" + prefix + "_" + recipeKey;
		}

		internal static int GetProgress(GameItem machine, string prefix, string recipeKey)
		{
			return TagUtil.GetInt(machine, ProgTag(prefix, recipeKey));
		}

		internal static void ClearProgress(GameItem machine, string prefix, string? raw)
		{
			foreach (var item2 in Parse(raw))
			{
				string item = item2.Id;
				TagUtil.SetInt(machine, ProgTag(prefix, item), 0);
			}
		}

		internal static bool IsFilled(GameItem machine, string prefix, string? raw)
		{
			List<(string, int)> list = Parse(raw);
			if (list.Count == 0)
			{
				return false;
			}
			foreach (var (recipeKey, num) in list)
			{
				if (GetProgress(machine, prefix, recipeKey) < num)
				{
					return false;
				}
			}
			return true;
		}

		internal static bool TryDepositFill(GameItem machine, GameItem material, string matId, string prefix, string? raw, out bool justCompleted)
		{
			justCompleted = false;
			List<(string, int)> list = Parse(raw);
			if (list.Count == 0)
			{
				return false;
			}
			string text = null;
			int num = 0;
			foreach (var (text2, num2) in list)
			{
				if (IdsMatch(text2, matId))
				{
					text = text2;
					num = num2;
					break;
				}
			}
			if (text == null || num <= 0)
			{
				return false;
			}
			if (IsFilled(machine, prefix, raw))
			{
				return true;
			}
			int progress = GetProgress(machine, prefix, text);
			if (progress >= num)
			{
				return true;
			}
			TagUtil.SetInt(machine, ProgTag(prefix, text), progress + 1);
			TagUtil.ConsumeOne(material);
			if (IsFilled(machine, prefix, raw))
			{
				justCompleted = true;
				ClearProgress(machine, prefix, raw);
			}
			return true;
		}

		internal static bool MatchesRepeatable(string? raw, string matId)
		{
			return ContainsId(raw, matId);
		}

		internal static string FormatFillProgress(GameItem machine, string prefix, string? raw)
		{
			List<(string, int)> list = Parse(raw);
			if (list.Count == 0)
			{
				return "(未配置配方)";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				var (text, value) = list[i];
				if (i > 0)
				{
					stringBuilder.Append(", ");
				}
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(2, 3, stringBuilder2);
				handler.AppendFormatted(text);
				handler.AppendLiteral(" ");
				handler.AppendFormatted(GetProgress(machine, prefix, text));
				handler.AppendLiteral("/");
				handler.AppendFormatted(value);
				stringBuilder2.Append(ref handler);
			}
			return stringBuilder.ToString();
		}

		internal static string FormatRecipe(string? raw)
		{
			List<(string, int)> list = Parse(raw);
			if (list.Count == 0)
			{
				return "(空)";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(", ");
				}
				StringBuilder stringBuilder2 = stringBuilder;
				StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(1, 2, stringBuilder2);
				handler.AppendFormatted(list[i].Item1);
				handler.AppendLiteral("×");
				handler.AppendFormatted(list[i].Item2);
				stringBuilder2.Append(ref handler);
			}
			return stringBuilder.ToString();
		}
	}
	internal static class DesequencerStructural
	{
		internal static bool AcceptsMaterial(GameItem machine, string matId)
		{
			return UniqueUnlock.Accepts(machine, "FMU_DESEQ_INSTANT_UNLOCK", Config.DeseqInstantUnlock.Value, matId);
		}

		internal static bool TryApply(GameItem machine, GameItem material, string matId)
		{
			if (UniqueUnlock.IsUnlocked(machine, "FMU_DESEQ_INSTANT_UNLOCK"))
			{
				return false;
			}
			return UniqueUnlock.TryDeposit(machine, material, matId, "FMU_DESEQ_INSTANT_UNLOCK", Config.DeseqInstantUnlock.Value, "desequencer 瞬解");
		}

		internal static void AppendTooltip(RichTextBuilder b, GameItem machine)
		{
			TooltipUi.BeginSection(b);
			UniqueUnlock.AppendTooltip(b, machine, "FMU_DESEQ_INSTANT_UNLOCK", "进度中瞬解", Config.DeseqInstantUnlock.Value, Config.DeseqInstantChance.Value, "过夜推进进度时直接完成");
		}
	}
	internal static class DragHighlight
	{
		private static bool _inDragPreview;

		private static bool _inEndDrag;

		internal static void Apply(Harmony harmony)
		{
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Expected Obj, but got Unknown
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Expected Obj, but got Unknown
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Expected Obj, but got Unknown
			//IL_015f: Expected Obj, but got Unknown
			//IL_017b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0190: Unknown result type (might be due to invalid IL or missing references)
			//IL_019d: Expected Obj, but got Unknown
			//IL_019d: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(GameItem), "MayHaveValidInventorySlot", new Type[1] { typeof(GameItem) }, (Type[])null);
				MethodInfo methodInfo2 = AccessTools.Method(typeof(GameItem), "TryFindOneValidInventorySlot", new Type[1] { typeof(GameItem) }, (Type[])null);
				if (methodInfo != null)
				{
					harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(DragHighlight), "PrefixMayHave", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("DragHighlight: MayHaveValidInventorySlot 已挂钩");
					}
				}
				else
				{
					Mod.Log.Warning("DragHighlight: MayHaveValidInventorySlot 未找到");
				}
				if (methodInfo2 != null)
				{
					harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(typeof(DragHighlight), "PrefixTryFind", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("DragHighlight: TryFindOneValidInventorySlot 已挂钩");
					}
				}
				else
				{
					Mod.Log.Warning("DragHighlight: TryFindOneValidInventorySlot 未找到");
				}
				MethodInfo methodInfo3 = AccessTools.Method(typeof(ItemMouseDragHandler), "Drag", (Type[])null, (Type[])null);
				MethodInfo methodInfo4 = AccessTools.Method(typeof(ItemMouseDragHandler), "EndDrag", (Type[])null, (Type[])null);
				if (methodInfo3 != null)
				{
					harmony.Patch((MethodBase)methodInfo3, new HarmonyMethod(typeof(DragHighlight), "PrefixDrag", (Type[])null), new HarmonyMethod(typeof(DragHighlight), "PostfixDrag", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
				if (methodInfo4 != null)
				{
					harmony.Patch((MethodBase)methodInfo4, new HarmonyMethod(typeof(DragHighlight), "PrefixEndDragPhase", (Type[])null), new HarmonyMethod(typeof(DragHighlight), "PostfixEndDragPhase", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("DragHighlight Apply: " + ex.Message);
			}
		}

		public static void PrefixDrag()
		{
			_inDragPreview = true;
		}

		public static void PostfixDrag()
		{
			_inDragPreview = false;
		}

		public static void PrefixEndDragPhase()
		{
			_inEndDrag = true;
		}

		public static void PostfixEndDragPhase()
		{
			_inEndDrag = false;
			_inDragPreview = false;
		}

		public static bool PrefixMayHave(GameItem __instance, GameItem item, ref bool __result)
		{
			if (!_inDragPreview || _inEndDrag)
			{
				return true;
			}
			if (!StructuralRouter.IsUpgradeablePair(__instance, item))
			{
				return true;
			}
			__result = true;
			return false;
		}

		public static bool PrefixTryFind(GameItem __instance, GameItem item, ref SlotMarker __result)
		{
			if (!_inDragPreview || _inEndDrag)
			{
				return true;
			}
			if (!StructuralRouter.IsUpgradeablePair(__instance, item))
			{
				return true;
			}
			try
			{
				SlotMarker incompleteSlotInInventory = __instance.GetIncompleteSlotInInventory();
				if (incompleteSlotInInventory != null)
				{
					try
					{
						incompleteSlotInInventory.numTransfer = 1;
					}
					catch
					{
					}
					__result = incompleteSlotInInventory;
					return false;
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("DragHighlight TryFind: " + ex.Message);
			}
			return true;
		}
	}
	internal static class DragPatch
	{
		private static bool _lastItemResolved;

		private static FieldInfo? _lastItemField;

		private static PropertyInfo? _lastItemProp;

		private static MethodInfo? _lastItemGetter;

		private static bool _highlightResolved;

		private static FieldInfo? _highlightField;

		private static PropertyInfo? _highlightProp;

		internal static void Apply(Harmony harmony)
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected Obj, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(typeof(ItemMouseDragHandler), "EndDrag", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Mod.Log.Warning("DragPatch: ItemMouseDragHandler.EndDrag not found");
				return;
			}
			harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(DragPatch), "PrefixEndDrag", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			if (Mod.Verbose)
			{
				Mod.Log.Msg("DragPatch: EndDrag 已挂钩");
			}
		}

		public static bool PrefixEndDrag(ItemMouseDragHandler __instance)
		{
			try
			{
				GameItem currentItem = GetCurrentItem(__instance);
				if (currentItem == null)
				{
					return true;
				}
				string text = TagUtil.SafeId(currentItem);
				if (!Materials.IsStructuralMaterial(text))
				{
					return true;
				}
				GameItem lastItem = GetLastItem(__instance);
				if (lastItem == null || currentItem == lastItem)
				{
					return true;
				}
				string machineId = TagUtil.SafeId(lastItem);
				if (!StructuralRouter.CanHandle(lastItem, machineId))
				{
					return true;
				}
				if (!StructuralRouter.TryApply(lastItem, currentItem, machineId, text))
				{
					return true;
				}
				try
				{
					__instance.AbortDrag();
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("DragPatch AbortDrag: " + ex.Message);
				}
				ClearDragHighlights(__instance, lastItem);
				return false;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("DragPatch PrefixEndDrag: " + ex2.GetType().Name + ": " + ex2.Message);
				return true;
			}
		}

		private static void ClearDragHighlights(ItemMouseDragHandler handler, GameItem target)
		{
			try
			{
				ResolveHighlight();
				HighlightManager val = null;
				if (_highlightField != null)
				{
					object? value = _highlightField.GetValue(handler);
					val = (HighlightManager)((value is HighlightManager) ? value : null);
				}
				else if (_highlightProp != null)
				{
					object? value2 = _highlightProp.GetValue(handler);
					val = (HighlightManager)((value2 is HighlightManager) ? value2 : null);
				}
				if (val != null)
				{
					val.Clear();
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("DragPatch ClearHighlightManager: " + ex.Message);
			}
			try
			{
				GameInventory parentInventory = target.parentInventory;
				if (parentInventory != null)
				{
					parentInventory.ClearHighlight();
				}
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("DragPatch ClearInventoryHighlight: " + ex2.Message);
			}
		}

		private static void ResolveHighlight()
		{
			if (_highlightResolved)
			{
				return;
			}
			_highlightResolved = true;
			Type typeFromHandle = typeof(ItemMouseDragHandler);
			try
			{
				_highlightField = typeFromHandle.GetField("highlightManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? typeFromHandle.GetField("<highlightManager>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				_highlightProp = typeFromHandle.GetProperty("highlightManager", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("DragPatch ResolveHighlight: " + ex.Message);
			}
		}

		private static GameItem? GetCurrentItem(ItemMouseDragHandler handler)
		{
			try
			{
				return handler.GetCurrentItem();
			}
			catch
			{
				return null;
			}
		}

		private static GameItem? GetLastItem(ItemMouseDragHandler handler)
		{
			ResolveLastItem();
			try
			{
				if (_lastItemField != null)
				{
					object? value = _lastItemField.GetValue(handler);
					return (GameItem?)((value is GameItem) ? value : null);
				}
				if (_lastItemProp != null)
				{
					object? value2 = _lastItemProp.GetValue(handler);
					return (GameItem?)((value2 is GameItem) ? value2 : null);
				}
				if (_lastItemGetter != null)
				{
					object? obj = _lastItemGetter.Invoke(handler, null);
					return (GameItem?)((obj is GameItem) ? obj : null);
				}
			}
			catch
			{
			}
			return null;
		}

		private static void ResolveLastItem()
		{
			if (_lastItemResolved)
			{
				return;
			}
			_lastItemResolved = true;
			Type typeFromHandle = typeof(ItemMouseDragHandler);
			try
			{
				_lastItemField = typeFromHandle.GetField("lastItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? typeFromHandle.GetField("<lastItem>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				_lastItemProp = typeFromHandle.GetProperty("lastItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				_lastItemGetter = typeFromHandle.GetMethod("get_lastItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? typeFromHandle.GetMethod("GetLastItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("DragPatch ResolveLastItem: " + ex.Message);
			}
		}
	}
	internal static class GameTypes
	{
		private const string GameNamespace = "Il2Cpp";

		internal static Type? Find(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			try
			{
				Assembly assembly = typeof(GameItem).Assembly;
				foreach (string item in Candidates(name))
				{
					Type type = assembly.GetType(item, throwOnError: false, ignoreCase: false);
					if (type != null)
					{
						if (Mod.Verbose && item != name)
						{
							Mod.Log.Msg("[FMU] GameTypes: " + name + " → " + item);
						}
						return type;
					}
				}
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"[FMU] GameTypes: 找不到 {name}（试过 {string.Join(" / ", Candidates(name))}）");
				}
				return null;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GameTypes.Find(" + name + "): " + ex.Message);
				return null;
			}
		}

		private static IEnumerable<string> Candidates(string name)
		{
			yield return name;
			if (name.StartsWith("Il2Cpp.", StringComparison.Ordinal))
			{
				yield return name.Substring("Il2Cpp".Length + 1);
			}
			else
			{
				yield return "Il2Cpp." + name;
			}
		}
	}
	internal static class GridFinder
	{
		private static readonly HashSet<string> Logged = new HashSet<string>(StringComparer.Ordinal);

		internal static List<GameGridInventory> CollectFromMachine(GameItem machine)
		{
			List<GameGridInventory> list = new List<GameGridInventory>();
			HashSet<IntPtr> seen = new HashSet<IntPtr>();
			try
			{
				PixelWindow contentWindow = machine.contentWindow;
				if (contentWindow != null)
				{
					CollectFromWindow(contentWindow, list, seen);
				}
			}
			catch
			{
			}
			return list;
		}

		internal static GameGridInventory? FindBestGrid(GameItem machine, int preferWidth, int preferMinHeight)
		{
			List<GameGridInventory> list = CollectFromMachine(machine);
			GameGridInventory val = null;
			int num = int.MinValue;
			foreach (GameGridInventory item in list)
			{
				int num2 = 0;
				int num3 = 0;
				try
				{
					GridShape inventoryShape = item.inventoryShape;
					num2 = ((inventoryShape != null) ? inventoryShape.width : 0);
					num3 = ((inventoryShape != null) ? inventoryShape.height : 0);
				}
				catch
				{
				}
				int num4 = 0;
				if (num2 == preferWidth)
				{
					num4 += 10;
				}
				if (num3 >= preferMinHeight)
				{
					num4 += 5;
				}
				if (num3 == 2 || num3 == 4)
				{
					num4 += 3;
				}
				if (num4 > num)
				{
					num = num4;
					val = item;
				}
			}
			GameGridInventory val2 = val;
			if (val2 == null)
			{
				if (list.Count <= 0)
				{
					return null;
				}
				val2 = list[0];
			}
			return val2;
		}

		internal static GameGridInventory? FindWineGrid(GameItem machine)
		{
			return PickWine(machine, CollectFromMachine(machine));
		}

		internal static GameGridInventory? FindModuleGrid(GameItem machine)
		{
			return PickModule(machine, CollectFromMachine(machine));
		}

		private static GameGridInventory? PickWine(GameItem machine, List<GameGridInventory> grids)
		{
			if (grids.Count == 0)
			{
				return null;
			}
			if (grids.Count == 1)
			{
				return grids[0];
			}
			foreach (GameGridInventory grid in grids)
			{
				if (HasMark(grid, "FMU_GRID_WINE"))
				{
					return grid;
				}
			}
			GameGridInventory val = PickModule(machine, grids);
			if (val != null)
			{
				List<GameGridInventory> list = new List<GameGridInventory>(grids.Count);
				foreach (GameGridInventory grid2 in grids)
				{
					if (!SameGrid(grid2, val))
					{
						list.Add(grid2);
					}
				}
				if (list.Count > 0 && list.Count < grids.Count)
				{
					LogOnce(machine, "wine-fallback", $"[FMU] wine_rack 无酒瓶标记 → 排掉模组格（{SafeWidth(val)}x{SafeHeight(val)}）后取最大：{Describe(list)}");
					return BiggestByArea(list);
				}
			}
			LogOnce(machine, "wine-nomark", "[FMU] wine_rack 认不出模组格，只能按面积挑酒瓶格：" + Describe(grids));
			return BiggestByArea(grids);
		}

		private static GameGridInventory? PickModule(GameItem machine, List<GameGridInventory> grids)
		{
			if (grids.Count == 0)
			{
				return null;
			}
			if (grids.Count == 1)
			{
				return grids[0];
			}
			foreach (GameGridInventory grid in grids)
			{
				if (HasMark(grid, "FMU_GRID_MODULE"))
				{
					return grid;
				}
			}
			GameGridInventory val = TryGetModuleInv(machine);
			if (val != null)
			{
				foreach (GameGridInventory grid2 in grids)
				{
					if (SameGrid(grid2, val))
					{
						LogOnce(machine, "modbay-api", $"[FMU] wine_rack 模组格无标记 → 采纳 GetModuleInv 的结果（{SafeWidth(grid2)}x{SafeHeight(grid2)}）");
						return grid2;
					}
				}
				LogOnce(machine, "modbay-api-miss", "[FMU] wine_rack GetModuleInv 返回的格子不在窗口枚举结果里 → 不采信");
			}
			List<GameGridInventory> list = new List<GameGridInventory>(grids.Count);
			foreach (GameGridInventory grid3 in grids)
			{
				if (!HasMark(grid3, "FMU_GRID_WINE"))
				{
					list.Add(grid3);
				}
			}
			if (list.Count > 0 && list.Count < grids.Count)
			{
				LogOnce(machine, "modbay-nonwine", "[FMU] wine_rack 模组格：靠「不是酒瓶格的那格」认出");
				return list[0];
			}
			GameGridInventory b = BiggestByArea(grids);
			foreach (GameGridInventory grid4 in grids)
			{
				if (!SameGrid(grid4, b))
				{
					LogOnce(machine, "modbay-areafallback", "[FMU] wine_rack 模组格：只能按「非最大」兜底（" + Describe(grids) + "）");
					return grid4;
				}
			}
			return null;
		}

		private static GameGridInventory? TryGetModuleInv(GameItem machine)
		{
			try
			{
				return MachineHelper.GetModuleInv(machine);
			}
			catch (Exception ex)
			{
				LogOnce(machine, "modbay-apierr", "[FMU] GetModuleInv 失败：" + ex.Message);
				return null;
			}
		}

		private static GameGridInventory? BiggestByArea(List<GameGridInventory> grids)
		{
			GameGridInventory result = null;
			int num = -1;
			foreach (GameGridInventory grid in grids)
			{
				int num2 = SafeWidth(grid) * SafeHeight(grid);
				if (num2 > num)
				{
					num = num2;
					result = grid;
				}
			}
			return result;
		}

		private static bool SameGrid(GameGridInventory a, GameGridInventory? b)
		{
			if (b == null)
			{
				return false;
			}
			try
			{
				return ((Il2CppObjectBase)a).Pointer == ((Il2CppObjectBase)b).Pointer;
			}
			catch
			{
				return a == b;
			}
		}

		internal static int SafeWidth(GameGridInventory grid)
		{
			try
			{
				GridShape inventoryShape = grid.inventoryShape;
				return (inventoryShape != null) ? inventoryShape.width : 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static int SafeHeight(GameGridInventory grid)
		{
			try
			{
				GridShape inventoryShape = grid.inventoryShape;
				return (inventoryShape != null) ? inventoryShape.height : 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static bool HasMark(GameGridInventory grid, string mark)
		{
			try
			{
				return ((TagSystem)grid).IsTag(mark);
			}
			catch
			{
				return false;
			}
		}

		private static string Describe(List<GameGridInventory> grids)
		{
			List<string> list = new List<string>();
			foreach (GameGridInventory grid in grids)
			{
				list.Add($"{SafeWidth(grid)}x{SafeHeight(grid)}");
			}
			return string.Join(" / ", list);
		}

		private static void CollectFromWindow(PixelWindow window, List<GameGridInventory> list, HashSet<nint> seen)
		{
			try
			{
				if (window.childElement != null)
				{
					CollectFromNode(((Il2CppObjectBase)window.childElement).TryCast<GraphNodeStorage>(), list, seen);
				}
			}
			catch
			{
			}
			try
			{
				CollectFromNode(window.child, list, seen);
			}
			catch
			{
			}
			try
			{
				List<GraphNodeStorage> children = window.children;
				if (children != null)
				{
					for (int i = 0; i < children.Count; i++)
					{
						CollectFromNode(children[i], list, seen);
					}
				}
			}
			catch
			{
			}
		}

		private static void CollectFromNode(GraphNodeStorage? node, List<GameGridInventory> list, HashSet<nint> seen)
		{
			if (node == null)
			{
				return;
			}
			GameGridInventory val = AsGrid(node);
			if (val != null)
			{
				nint num = 0;
				try
				{
					num = ((Il2CppObjectBase)val).Pointer;
				}
				catch
				{
				}
				if (num == 0 || seen.Add(num))
				{
					list.Add(val);
				}
			}
			try
			{
				List<GraphNodeStorage> children = node.children;
				if (children != null)
				{
					for (int i = 0; i < children.Count; i++)
					{
						CollectFromNode(children[i], list, seen);
					}
				}
			}
			catch
			{
			}
		}

		internal static void LogOnce(GameItem machine, string slot, string message)
		{
			if (Mod.Verbose)
			{
				nint value = 0;
				try
				{
					value = ((Il2CppObjectBase)machine).Pointer;
				}
				catch
				{
				}
				if (Logged.Add($"{value}:{slot}"))
				{
					Mod.Log.Msg(message);
				}
			}
		}

		internal static void ResetLogs()
		{
			Logged.Clear();
		}

		private static GameGridInventory? AsGrid(GraphNodeStorage? node)
		{
			if (node == null)
			{
				return null;
			}
			try
			{
				return ((Il2CppObjectBase)node).TryCast<GameGridInventory>();
			}
			catch
			{
				return null;
			}
		}
	}
	internal static class GridResize
	{
		private static readonly HashSet<string> Attempted = new HashSet<string>(StringComparer.Ordinal);

		internal static void Reset()
		{
			Attempted.Clear();
			GridFinder.ResetLogs();
			ModuleBayStructural.ResetLogs();
		}

		internal static bool EnsureWidth(GameItem machine, GameGridInventory grid, int width, string key)
		{
			try
			{
				if (width <= 0)
				{
					return false;
				}
				int num = GridFinder.SafeWidth(grid);
				int num2 = GridFinder.SafeHeight(grid);
				if (num <= 0 || num2 <= 0)
				{
					return false;
				}
				if (num >= width)
				{
					return true;
				}
				string item = $"{key}|{((Il2CppObjectBase)grid).Pointer:X}|{width}";
				if (!Attempted.Add(item))
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg($"[FMU] GridResize 跳过重复尝试 {key} {num}→{width}");
					}
					return false;
				}
				grid.SetShape(width, num2);
				try
				{
					PixelWindow contentWindow = machine.contentWindow;
					if (contentWindow != null)
					{
						contentWindow.Validate();
					}
				}
				catch
				{
				}
				Mod.Log.Msg($"[FMU] GridResize {key}: {num}x{num2} → {width}x{num2}");
				return true;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GridResize.EnsureWidth(" + key + "): " + ex.Message);
				return false;
			}
		}
	}
	internal static class GridStampPatch
	{
		internal static void Apply(Harmony harmony)
		{
			TryPatchWine(harmony);
			TryPatchModule(harmony);
		}

		private static void TryPatchWine(Harmony harmony)
		{
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Expected Obj, but got Unknown
			try
			{
				Type type = GameTypes.Find("MachineWineRack");
				if (type == null)
				{
					Mod.Log.Warning("GridStampPatch: 类型 Il2Cpp.MachineWineRack 未找到（酒瓶格退回面积兜底）");
					return;
				}
				MethodInfo methodInfo = AccessTools.Method(type, "SetupWineSlot", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning("GridStampPatch: 方法 Il2Cpp.MachineWineRack.SetupWineSlot 未找到（酒瓶格退回面积兜底）");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(GridStampPatch), "PostfixWineSlot", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("GridStampPatch: SetupWineSlot 已挂钩（酒瓶格打标记）");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GridStampPatch wine: " + ex.Message);
			}
		}

		private static void TryPatchModule(Harmony harmony)
		{
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Expected Obj, but got Unknown
			try
			{
				Type type = GameTypes.Find("MachineHelper");
				if (type == null)
				{
					Mod.Log.Warning("GridStampPatch: 类型 Il2Cpp.MachineHelper 未找到（模组列退回「非酒瓶格」兜底）");
					return;
				}
				MethodInfo methodInfo = AccessTools.Method(type, "SetupModuleBay", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning("GridStampPatch: 方法 Il2Cpp.MachineHelper.SetupModuleBay 未找到（模组列退回「非酒瓶格」兜底）");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(GridStampPatch), "PostfixModuleBay", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("GridStampPatch: SetupModuleBay 已挂钩（模组列打标记）");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GridStampPatch module: " + ex.Message);
			}
		}

		public static void PostfixWineSlot(GameGridInventory __0)
		{
			Stamp((GameInventory?)(object)__0, "FMU_GRID_WINE", "酒瓶格");
		}

		public static void PostfixModuleBay(GameInventory __0, GameItem __1)
		{
			string text = TagUtil.SafeId(__1);
			if (Mod.Verbose)
			{
				Mod.Log.Msg($"[FMU] 模组仓机器确认：{text}（加宽名单内={ModuleBayStructural.IsModuleBayMachine(text)}）");
			}
			Stamp(__0, "FMU_GRID_MODULE", "模组列(" + text + ")");
		}

		private static void Stamp(GameInventory? grid, string mark, string what)
		{
			if (grid == null)
			{
				return;
			}
			try
			{
				if (!((TagSystem)grid).IsTag(mark))
				{
					((TagSystem)grid).EnableTag(mark);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("[FMU] 窗口格子标记：" + what + " = " + mark);
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GridStampPatch Stamp " + mark + ": " + ex.Message);
			}
		}
	}
	internal static class Materials
	{
		internal static bool IsStructuralMaterial(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			foreach (var (_, val2) in Config.ModuleBayRecipe)
			{
				if (val2 != null && val2.Value != null && RecipeUtil.ContainsId(val2.Value, id))
				{
					return true;
				}
			}
			return RecipeUtil.ContainsAnyConfigured(id, Config.PowerBlocTier1, Config.PowerBlocTier2, Config.RechargerExpand, Config.RechargerFast, Config.WineWidth, Config.WineBottleCol, Config.WineCircuit, Config.WineWire, Config.PurifierFilter, Config.PurifierIngot, Config.FurnaceIngot, Config.FurnaceCircuit, Config.MoistureCircuit, Config.MoistureWire, Config.ProjectorCircuit, Config.ProjectorPlastic, Config.ProjectorWire, Config.ProjectorIngot, Config.WineLabelUnlock, Config.PurifierClearUnlock, Config.DeseqInstantUnlock);
		}
	}
	public sealed class Mod : MelonMod
	{
		internal static Instance Log;

		internal static bool Verbose;

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			Config.Init();
			DragPatch.Apply(((MelonBase)this).HarmonyInstance);
			DragHighlight.Apply(((MelonBase)this).HarmonyInstance);
			TooltipPatch.Apply(((MelonBase)this).HarmonyInstance);
			RestorePatch.Apply(((MelonBase)this).HarmonyInstance);
			RechargerFastChargePatch.Apply(((MelonBase)this).HarmonyInstance);
			RechargerSpritePatch.Apply(((MelonBase)this).HarmonyInstance);
			UniqueEffectsPatch.Apply(((MelonBase)this).HarmonyInstance);
			BonusPersistPatch.Apply(((MelonBase)this).HarmonyInstance);
			GridStampPatch.Apply(((MelonBase)this).HarmonyInstance);
			Log.Msg("更全面的机器升级 v0.3.13 — 修「充电器快充没生效」：原版过夜充电是 MachineRecharger 的 <Recharger>b__1 遍历电池槽，对每块电池调 PowerHelper.ChargeBattery(电池, 商业用电解锁 ? 6 : 3)（也就是说王尔德人脉网络的「商业用电」本身就是 ×2）；我们挂钩的是这个调用，本该把充入量再翻倍。但找「电池所在的那台充电器」时用了 inv.parent.TryCast<GameItem>() —— GameItem 与 GraphNodeStorage 是两条互不相干的继承链，这个 cast 永远失败 ? 恒返回 null ? ×2 一次都没发生过。现改用游戏自己的 GameInventory.GetParentItem()（取 parents[0] 并 cast 成 GameItem，见 OwnerFinder）。现在只改「传进来的数量」、不碰原版 3/6 的判断 ? 与原版乘法叠加：没买商业用电 3→6、买了 6→12（王尔德那条依然是 ×2，互不吃掉）。另加节流生效日志（同一块电池 30 秒一行），以后不必再靠反推确认。");
		}

		public override void OnLateInitializeMelon()
		{
			try
			{
				Config.RecheckOptionalMachines();
			}
			catch (Exception ex)
			{
				Log.Warning("OnLateInitializeMelon: " + ex.Message);
			}
		}
	}
	internal static class ModPresence
	{
		private static readonly HashSet<string> Loaded = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private static readonly HashSet<string> Missing = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

		private static readonly Dictionary<string, string> Providers = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["hydroponic"] = "FarmerMachines",
			["printer"] = "GunsmithKit",
			["evaporator"] = "ChemistKit",
			["blender"] = "ChemistKit",
			["heating_plate"] = "ChemistKit",
			["chem_finisher"] = "ChemistKit",
			["chem_purifier_diy"] = "ChemistKit"
		};

		internal static string? ProviderOf(string machineId)
		{
			if (!Providers.TryGetValue(machineId, out string value))
			{
				return null;
			}
			return value;
		}

		internal static bool IsLoaded(string? assemblyName)
		{
			if (string.IsNullOrEmpty(assemblyName))
			{
				return true;
			}
			if (Loaded.Contains(assemblyName))
			{
				return true;
			}
			if (Missing.Contains(assemblyName))
			{
				return false;
			}
			try
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				for (int i = 0; i < assemblies.Length; i++)
				{
					string text = assemblies[i]?.GetName()?.Name;
					if (!string.IsNullOrEmpty(text) && string.Equals(text, assemblyName, StringComparison.OrdinalIgnoreCase))
					{
						Loaded.Add(assemblyName);
						Missing.Remove(assemblyName);
						return true;
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("ModPresence.IsLoaded(" + assemblyName + "): " + ex.Message);
			}
			Missing.Add(assemblyName);
			return false;
		}

		internal static bool MachineAvailable(string machineId)
		{
			string text = ProviderOf(machineId);
			if (text != null)
			{
				return IsLoaded(text);
			}
			return true;
		}

		internal static void ResetMisses()
		{
			Missing.Clear();
		}
	}
	internal static class ModuleBayStructural
	{
		private const string GridKey = "modbay";

		private static readonly HashSet<nint> WarnedNotExpanded = new HashSet<IntPtr>();

		internal static void ResetLogs()
		{
			WarnedNotExpanded.Clear();
		}

		internal static bool IsModuleBayMachine(string id)
		{
			if (!string.IsNullOrEmpty(id))
			{
				return Config.ModuleBayRecipe.ContainsKey(id);
			}
			return false;
		}

		private static string? RecipeOf(string id)
		{
			if (!Config.ModuleBayRecipe.TryGetValue(id, out MelonPreferences_Entry<string> value))
			{
				return null;
			}
			return value.Value;
		}

		private static int MaxOf(string id)
		{
			if (!Config.ModuleBayMax.TryGetValue(id, out MelonPreferences_Entry<int> value))
			{
				return 0;
			}
			return Math.Max(0, value.Value);
		}

		internal static bool VanillaModuleBayFull(GameItem machine)
		{
			return TagUtil.GetInt(machine, "MODULE_UPGRADE_STAGE_INT") >= 2;
		}

		internal static bool AcceptsMaterial(GameItem machine, string matId)
		{
			string id = TagUtil.SafeId(machine);
			if (!IsModuleBayMachine(id))
			{
				return false;
			}
			if (!RecipeUtil.MatchesRepeatable(RecipeOf(id), matId))
			{
				return false;
			}
			if (TagUtil.GetInt(machine, "FMU_MODULEBAY_WIDTH_LEVEL") >= MaxOf(id))
			{
				return false;
			}
			return VanillaModuleBayFull(machine);
		}

		internal static bool TryApply(GameItem machine, GameItem material, string matId)
		{
			string text = TagUtil.SafeId(machine);
			if (!IsModuleBayMachine(text))
			{
				return false;
			}
			if (!RecipeUtil.MatchesRepeatable(RecipeOf(text), matId))
			{
				return false;
			}
			int num = MaxOf(text);
			int num2 = TagUtil.GetInt(machine, "FMU_MODULEBAY_WIDTH_LEVEL");
			if (num2 >= num)
			{
				return false;
			}
			if (!VanillaModuleBayFull(machine))
			{
				WarnNotExpanded(machine, text);
				return false;
			}
			GameGridInventory val = FindModuleGrid(machine, out string how);
			if (val == null)
			{
				Mod.Log.Warning("[FMU] " + text + " 模组仓：找不到模组格（窗口没开？），本次不消耗");
				return false;
			}
			int num3 = GridFinder.SafeWidth(val);
			int num4 = TagUtil.GetInt(machine, "FMU_MODULEBAY_WIDTH_BASE");
			if (num4 <= 0)
			{
				num4 = ((num3 <= 0) ? 1 : num3);
				TagUtil.SetInt(machine, "FMU_MODULEBAY_WIDTH_BASE", num4);
			}
			int num5 = num4 + num2 + 1;
			if (!GridResize.EnsureWidth(machine, val, num5, "modbay"))
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"[FMU] {text} 模组仓：{num3}→{num5} 未生效（本次已试过），不消耗");
				}
				return false;
			}
			TagUtil.SetInt(machine, "FMU_MODULEBAY_WIDTH_LEVEL", num2 + 1);
			if (material != null)
			{
				TagUtil.ConsumeOne(material);
			}
			Mod.Log.Msg($"[FMU] {text} 模组仓加宽 → {num5} 列 ({num2 + 1}/{num}) 格子来源={how} 窗口 {WinW(machine)}x{WinH(machine)}px");
			return true;
		}

		internal static void TryRestore(GameItem machine)
		{
			if (!IsModuleBayMachine(TagUtil.SafeId(machine)))
			{
				return;
			}
			int num = TagUtil.GetInt(machine, "FMU_MODULEBAY_WIDTH_LEVEL");
			if (num <= 0)
			{
				return;
			}
			GameGridInventory val = FindModuleGrid(machine, out string _);
			if (val != null)
			{
				int num2 = TagUtil.GetInt(machine, "FMU_MODULEBAY_WIDTH_BASE");
				if (num2 <= 0)
				{
					int num3 = GridFinder.SafeWidth(val);
					num2 = ((num3 - num > 0) ? (num3 - num) : num3);
					TagUtil.SetInt(machine, "FMU_MODULEBAY_WIDTH_BASE", num2);
				}
				GridResize.EnsureWidth(machine, val, num2 + num, "modbay");
			}
		}

		internal static void TryAppendTooltip(RichTextBuilder b, GameItem machine)
		{
			string id = TagUtil.SafeId(machine);
			if (IsModuleBayMachine(id))
			{
				TooltipUi.BeginSection(b);
				TooltipUi.Meter(b, "模组仓加宽·" + Config.ModuleBayLabel(id), TagUtil.GetInt(machine, "FMU_MODULEBAY_WIDTH_LEVEL"), MaxOf(id), TooltipUi.RecipeMatsHint(RecipeOf(id)), "模组格宽度 +1 列");
				if (!VanillaModuleBayFull(machine))
				{
					TooltipUi.Hint(b, "已锁：需先用「模组舱扩建套件」把模组仓扩满" + $"（{TagUtil.GetInt(machine, "MODULE_UPGRADE_STAGE_INT")}/{2}）");
				}
				else if (TagUtil.GetInt(machine, "FMU_MODULEBAY_WIDTH_LEVEL") <= 0)
				{
					TooltipUi.Hint(b, "原版模组仓已扩满，可继续加宽");
				}
			}
		}

		private static GameGridInventory? FindModuleGrid(GameItem machine, out string how)
		{
			how = "";
			foreach (GameGridInventory item in CollectGrids(machine))
			{
				if (GridFinder.HasMark(item, "FMU_GRID_MODULE"))
				{
					how = "标记";
					return item;
				}
			}
			string text = TagUtil.SafeId(machine);
			if (!IsModuleBayMachine(text))
			{
				how = "无标记且不在名单";
				return null;
			}
			try
			{
				GameGridInventory moduleInv = MachineHelper.GetModuleInv(machine);
				if (moduleInv == null)
				{
					how = "GetModuleInv 无结果";
					return null;
				}
				foreach (GameGridInventory item2 in CollectGrids(machine))
				{
					if (SameGrid(item2, moduleInv))
					{
						how = "GetModuleInv";
						return item2;
					}
				}
				GridFinder.LogOnce(machine, "modbay-api", "[FMU] " + text + " 模组仓：格子来自 GetModuleInv（窗口里没枚举到），照用");
				how = "GetModuleInv(未枚举到)";
				return moduleInv;
			}
			catch (Exception ex)
			{
				GridFinder.LogOnce(machine, "modbay-apierr", "[FMU] 模组仓 GetModuleInv 失败：" + ex.Message);
				return null;
			}
		}

		private static List<GameGridInventory> CollectGrids(GameItem machine)
		{
			try
			{
				return GridFinder.CollectFromMachine(machine);
			}
			catch
			{
				return new List<GameGridInventory>();
			}
		}

		private static bool SameGrid(GameGridInventory a, GameGridInventory b)
		{
			try
			{
				return ((Il2CppObjectBase)a).Pointer == ((Il2CppObjectBase)b).Pointer;
			}
			catch
			{
				return a == b;
			}
		}

		private static void WarnNotExpanded(GameItem machine, string id)
		{
			nint item = 0;
			try
			{
				item = ((Il2CppObjectBase)machine).Pointer;
			}
			catch
			{
			}
			if (WarnedNotExpanded.Add(item))
			{
				Mod.Log.Msg($"[FMU] {id} 模组仓加宽：原版模组仓未扩满（{TagUtil.GetInt(machine, "MODULE_UPGRADE_STAGE_INT")}/{2}），需先用「模组舱扩建套件」×2；本次不消耗");
			}
		}

		private static int WinW(GameItem machine)
		{
			try
			{
				return (machine.contentWindow != null) ? machine.contentWindow.widthPixels : 0;
			}
			catch
			{
				return 0;
			}
		}

		private static int WinH(GameItem machine)
		{
			try
			{
				return (machine.contentWindow != null) ? machine.contentWindow.heightPixels : 0;
			}
			catch
			{
				return 0;
			}
		}
	}
	internal static class OwnerFinder
	{
		private static bool _apiWarned;

		internal static GameItem? FindAncestor(GameItem? child, string wantedId, int maxHops = 3)
		{
			if (child == null || string.IsNullOrEmpty(wantedId) || maxHops <= 0)
			{
				return null;
			}
			try
			{
				GameInventory val = ParentInventory(child);
				for (int i = 0; i < maxHops; i++)
				{
					if (val == null)
					{
						break;
					}
					GameItem val2 = ParentItem(val);
					if (val2 == null)
					{
						return null;
					}
					if (string.Equals(TagUtil.SafeId(val2), wantedId, StringComparison.OrdinalIgnoreCase))
					{
						return val2;
					}
					val = ParentInventory(val2);
				}
			}
			catch (Exception ex)
			{
				LogOnce(ex.Message);
			}
			return null;
		}

		private static GameInventory? ParentInventory(GameItem item)
		{
			try
			{
				return item.parentInventory;
			}
			catch
			{
				return null;
			}
		}

		private static GameItem? ParentItem(GameInventory inv)
		{
			try
			{
				return inv.GetParentItem();
			}
			catch (Exception ex)
			{
				LogOnce(ex.Message);
				return null;
			}
		}

		private static void LogOnce(string msg)
		{
			if (!_apiWarned)
			{
				_apiWarned = true;
				Mod.Log.Warning("[FMU] OwnerFinder: GetParentItem 取父失败 → " + msg + "（这项功能会静默失效，请核对游戏是否改了取父 API）");
			}
		}
	}
	internal static class PowerBlocStructural
	{
		internal const int RangeVanilla = 0;

		internal const int RangeContainer = 1;

		internal const int RangeGlobal = 2;

		private const string ProgT1 = "PB_T1";

		private const string ProgT2 = "PB_T2";

		private static Action<GameItem, GameInventory, SlotMarker>? _upgradedCycle;

		private static bool _poweredFieldResolved;

		private static FieldInfo? _poweredField;

		internal static bool AcceptsMaterial(GameItem machine, string matId)
		{
			int range = GetRange(machine);
			if (range >= 2)
			{
				return false;
			}
			if (range >= 1)
			{
				return RecipeUtil.ContainsId(Config.PowerBlocTier2.Value, matId);
			}
			return RecipeUtil.ContainsId(Config.PowerBlocTier1.Value, matId);
		}

		internal static bool TryApply(GameItem machine, GameItem material, string matId)
		{
			int range = GetRange(machine);
			if (range >= 2)
			{
				BindUpgradedCycle(machine);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("[FMU] powerblock 已全局：本次拖放不消耗（交原版收尾）");
				}
				return false;
			}
			if (range >= 1)
			{
				if (!RecipeUtil.ContainsId(Config.PowerBlocTier2.Value, matId))
				{
					return false;
				}
				if (!RecipeUtil.TryDepositFill(machine, material, matId, "PB_T2", Config.PowerBlocTier2.Value, out var justCompleted))
				{
					return false;
				}
				if (justCompleted)
				{
					TagUtil.SetInt(machine, "FMU_POWERBLOC_RANGE", 2);
					BindUpgradedCycle(machine);
					Mod.Log.Msg("[FMU] powerblock 升至全局充电");
				}
				else if (Mod.Verbose)
				{
					Mod.Log.Msg("[FMU] powerblock 全局档 " + RecipeUtil.FormatFillProgress(machine, "PB_T2", Config.PowerBlocTier2.Value));
				}
				return true;
			}
			if (!RecipeUtil.ContainsId(Config.PowerBlocTier1.Value, matId))
			{
				return false;
			}
			if (!RecipeUtil.TryDepositFill(machine, material, matId, "PB_T1", Config.PowerBlocTier1.Value, out var justCompleted2))
			{
				return false;
			}
			if (justCompleted2)
			{
				TagUtil.SetInt(machine, "FMU_POWERBLOC_RANGE", 1);
				BindUpgradedCycle(machine);
				Mod.Log.Msg("[FMU] powerblock 升至同容器充电（排除充能器）");
			}
			else if (Mod.Verbose)
			{
				Mod.Log.Msg("[FMU] powerblock 同容器档 " + RecipeUtil.FormatFillProgress(machine, "PB_T1", Config.PowerBlocTier1.Value));
			}
			return true;
		}

		internal static int GetRange(GameItem machine)
		{
			return TagUtil.GetInt(machine, "FMU_POWERBLOC_RANGE");
		}

		internal static void TryRestore(GameItem machine)
		{
			if (GetRange(machine) >= 1)
			{
				BindUpgradedCycle(machine);
			}
		}

		internal static void AppendTooltip(RichTextBuilder b, GameItem machine)
		{
			TooltipUi.BeginSection(b);
			int range = GetRange(machine);
			if (range < 2)
			{
				if (range == 1)
				{
					TooltipUi.Status(b, "范围", "同容器（排除充能器）", highlight: true);
					TooltipUi.NextTier(b, "全局充电", "整个房间所有机器里的电池");
					TooltipUi.RecipeProgress(b, machine, "PB_T2", Config.PowerBlocTier2.Value);
				}
				else
				{
					TooltipUi.Status(b, "范围", "相邻（原版）");
					TooltipUi.NextTier(b, "同容器充电", "同一格内所有电池");
					TooltipUi.RecipeProgress(b, machine, "PB_T1", Config.PowerBlocTier1.Value);
				}
			}
			else
			{
				TooltipUi.Status(b, "范围", "全局（排除充能器）", highlight: true);
				TooltipUi.Done(b);
			}
		}

		internal static void BindUpgradedCycle(GameItem machine)
		{
			try
			{
				if (_upgradedCycle == null)
				{
					_upgradedCycle = DelegateSupport.ConvertDelegate<Action<GameItem, GameInventory, SlotMarker>>((Delegate)new Action<GameItem, GameInventory, SlotMarker>(OnUpgradedCycle));
				}
				if ((Delegate)(object)_upgradedCycle != (Delegate)null)
				{
					((GameItemFunc)machine).onCycleEndEarlySlotItemFunc = _upgradedCycle;
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("PowerBloc BindUpgradedCycle: " + ex.Message);
			}
		}

		private static void OnUpgradedCycle(GameItem gameItem, GameInventory _, SlotMarker __)
		{
			try
			{
				if (gameItem == null)
				{
					return;
				}
				int range = GetRange(gameItem);
				if (range < 1)
				{
					return;
				}
				if (!IsStorePowered())
				{
					gameItem.SetSprite("Items/items_tool", "powerblock");
					return;
				}
				gameItem.SetSprite("Items/items_tool", "powerblock_on");
				if (range >= 2)
				{
					ChargeGlobal(gameItem);
				}
				else
				{
					ChargeSameContainer(gameItem);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("PowerBloc OnUpgradedCycle: " + ex.Message);
			}
		}

		internal static int ChargeAmount()
		{
			try
			{
				if (NetworkUpgrade.IsCommercialPowerUnlocked())
				{
					return 4;
				}
			}
			catch
			{
			}
			return 2;
		}

		internal static void ChargeSameContainer(GameItem powerblock)
		{
			int num = ChargeAmount();
			int num2 = 0;
			try
			{
				GameInventory parentInventory = powerblock.parentInventory;
				if (parentInventory == null)
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg("[FMU] powerblock 同容器充电：无 parentInventory");
					}
					return;
				}
				List<GameItem> childItems = parentInventory.childItems;
				if (childItems == null)
				{
					return;
				}
				Enumerator<GameItem> enumerator = childItems.GetEnumerator();
				while (enumerator.MoveNext())
				{
					if (TryChargeTarget(enumerator.Current, powerblock, num))
					{
						num2++;
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("PowerBloc ChargeSameContainer: " + ex.Message);
			}
			if (Mod.Verbose)
			{
				Mod.Log.Msg($"[FMU] powerblock 同容器充电 ×{num} → {num2} 个目标");
			}
		}

		internal static void ChargeGlobal(GameItem powerblock)
		{
			int num = ChargeAmount();
			List<GameItem> list = new List<GameItem>();
			bool flag = BatteryMachineSource.TryFill(list, preferCache: true);
			if (!flag)
			{
				FallbackFindAllBatteryMachines(list);
			}
			int num2 = 0;
			foreach (GameItem item in list)
			{
				if (TryChargeTarget(item, powerblock, num))
				{
					num2++;
				}
			}
			if (Mod.Verbose)
			{
				Mod.Log.Msg($"[FMU] powerblock 全局充电 ×{num} → {num2} 台（source={(flag ? "BatteryMonitorLite" : "FindAllItem")}）");
			}
		}

		private static bool TryChargeTarget(GameItem? m, GameItem powerblock, int amount)
		{
			if (m == null || m == powerblock)
			{
				return false;
			}
			try
			{
				if (string.Equals(TagUtil.SafeId(m), "recharger", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
				if (m.IsTag("power_source_item"))
				{
					PowerHelper.ChargeBattery(m, amount);
					return true;
				}
				if (m.IsTag("MACHINE_RECHARGABLE_TAG"))
				{
					MachineHelper.RechargeMachineBattery(m, amount);
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private static bool IsStorePowered()
		{
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					return false;
				}
				ResolvePoweredField();
				if (_poweredField != null)
				{
					return (bool)_poweredField.GetValue(instance);
				}
				return true;
			}
			catch
			{
				return true;
			}
		}

		private static void ResolvePoweredField()
		{
			if (_poweredFieldResolved)
			{
				return;
			}
			_poweredFieldResolved = true;
			try
			{
				FieldInfo[] fields = typeof(PlayerStore).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fields)
				{
					if (fieldInfo.FieldType != typeof(bool))
					{
						continue;
					}
					string name = fieldInfo.Name;
					if (name.IndexOf("ower", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Power", StringComparison.Ordinal) >= 0)
					{
						_poweredField = fieldInfo;
						if (Mod.Verbose)
						{
							Mod.Log.Msg("PowerBloc: PlayerStore 供电标志字段 = " + fieldInfo.Name);
						}
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("PowerBloc ResolvePoweredField: " + ex.Message);
			}
		}

		private static void FallbackFindAllBatteryMachines(List<GameItem> into)
		{
			into.Clear();
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					return;
				}
				List<GameItem> val = instance.FindAllItem(true);
				if (val == null)
				{
					return;
				}
				Enumerator<GameItem> enumerator = val.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					if (current == null)
					{
						continue;
					}
					try
					{
						if (MachineHelper.HasBattery(current))
						{
							into.Add(current);
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("PowerBloc FallbackFindAll: " + ex.Message);
			}
		}
	}
	internal static class BatteryMachineSource
	{
		private static bool _resolved;

		private static MethodInfo? _tryGet;

		internal static bool TryFill(List<GameItem> into, bool preferCache)
		{
			Resolve();
			if (_tryGet == null)
			{
				return false;
			}
			try
			{
				object[] parameters = new object[2] { into, preferCache };
				return (bool)_tryGet.Invoke(null, parameters);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("BatteryMachineSource: " + ex.Message);
				return false;
			}
		}

		private static void Resolve()
		{
			if (_resolved)
			{
				return;
			}
			_resolved = true;
			try
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (Assembly assembly in assemblies)
				{
					if (!string.Equals(assembly.GetName().Name, "BatteryMonitorLite", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					_tryGet = assembly.GetType("BatteryMonitorLite.BatteryMachineShare")?.GetMethod("TryGetBatteryMachines", BindingFlags.Static | BindingFlags.Public);
					if (_tryGet != null)
					{
						if (Mod.Verbose)
						{
							Mod.Log.Msg("BatteryMachineSource: 已接上 BatteryMonitorLite.BatteryMachineShare");
						}
						break;
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("BatteryMachineSource Resolve: " + ex.Message);
			}
		}
	}
	internal static class PurifierStructural
	{
		private const int PartsPerTier = 10;

		private const int QualityPerTier = 15;

		private const int PerfPerIngot = 5;

		internal static bool AcceptsMaterial(GameItem machine, string matId)
		{
			if (UniqueUnlock.Accepts(machine, "FMU_PURIFIER_CLEAR_UNLOCK", Config.PurifierClearUnlock.Value, matId))
			{
				return true;
			}
			int num = Math.Max(1, Config.PurifierFilterMaxParts.Value);
			if (TagUtil.GetInt(machine, "FMU_PURIFIER_FILTER_PARTS") < num && RecipeUtil.MatchesRepeatable(Config.PurifierFilter.Value, matId))
			{
				return true;
			}
			int num2 = Math.Max(0, Config.PurifierIngotMax.Value);
			if (TagUtil.GetInt(machine, "FMU_PURIFIER_INGOT") < num2 && RecipeUtil.MatchesRepeatable(Config.PurifierIngot.Value, matId))
			{
				return true;
			}
			return false;
		}

		internal static bool TryApply(GameItem machine, GameItem material, string matId)
		{
			if (!UniqueUnlock.IsUnlocked(machine, "FMU_PURIFIER_CLEAR_UNLOCK") && RecipeUtil.ContainsId(Config.PurifierClearUnlock.Value, matId) && UniqueUnlock.TryDeposit(machine, material, matId, "FMU_PURIFIER_CLEAR_UNLOCK", Config.PurifierClearUnlock.Value, "water_purifier 清杂质"))
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.PurifierFilter.Value, matId))
			{
				return ApplyFilter(machine, material);
			}
			if (RecipeUtil.MatchesRepeatable(Config.PurifierIngot.Value, matId))
			{
				return ApplyIngot(machine, material);
			}
			return false;
		}

		private static bool ApplyFilter(GameItem machine, GameItem material)
		{
			int num = Math.Max(1, Config.PurifierFilterMaxParts.Value);
			int num2 = TagUtil.GetInt(machine, "FMU_PURIFIER_FILTER_PARTS");
			if (num2 >= num)
			{
				return false;
			}
			int num3 = num2 / 10;
			int num4 = Math.Min(num, num2 + 1);
			TagUtil.SetInt(machine, "FMU_PURIFIER_FILTER_PARTS", num4);
			TagUtil.ConsumeOne(material);
			int num5 = num4 / 10;
			int num6 = num5 - num3;
			if (num6 > 0)
			{
				BonusLedger.Grant(machine, num6 * 15, 0, 0);
			}
			Mod.Log.Msg($"[FMU] water_purifier 滤芯积分 {num4}/{num}（档 {num5}）");
			return true;
		}

		private static bool ApplyIngot(GameItem machine, GameItem material)
		{
			int num = Math.Max(0, Config.PurifierIngotMax.Value);
			int num2 = TagUtil.GetInt(machine, "FMU_PURIFIER_INGOT");
			if (num2 >= num)
			{
				return false;
			}
			TagUtil.SetInt(machine, "FMU_PURIFIER_INGOT", num2 + 1);
			BonusLedger.Grant(machine, "FMU_PURIFIER_INGOT", 1);
			TagUtil.ConsumeOne(material);
			Mod.Log.Msg($"[FMU] water_purifier 加压罐 {num2 + 1}/{num}：性能+{5}%");
			return true;
		}

		internal static void AppendTooltip(RichTextBuilder b, GameItem machine)
		{
			TooltipUi.BeginSection(b);
			BonusLedger.AppendTooltip(b, machine);
			int num = Math.Max(1, Config.PurifierFilterMaxParts.Value);
			int num2 = TagUtil.GetInt(machine, "FMU_PURIFIER_FILTER_PARTS");
			int cur = num2 / 10;
			int value = num2 % 10;
			int num3 = Math.Max(1, num / 10);
			int cur2 = TagUtil.GetInt(machine, "FMU_PURIFIER_INGOT");
			int value2 = Config.PurifierIngotMax.Value;
			TooltipUi.Meter(b, "高效滤芯档", cur, num3, $"滤芯积分 {value}/{10}", BonusLedger.DescribePurifierFilter());
			TooltipUi.Hint(b, $"拖「{TooltipUi.RecipeMatsHint(Config.PurifierFilter.Value)}」每个 +1 积分，每 {10} 积分升 1 档（上限 {num} 积分 = {num3} 档）");
			TooltipUi.Meter(b, "金属加压罐", cur2, value2, TooltipUi.RecipeMatsHint(Config.PurifierIngot.Value), BonusLedger.Describe("FMU_PURIFIER_INGOT"));
			UniqueUnlock.AppendTooltip(b, machine, "FMU_PURIFIER_CLEAR_UNLOCK", "净化清一种杂质", Config.PurifierClearUnlock.Value, Config.PurifierClearChance.Value, "净化时清空一种可提纯杂质");
		}
	}
	internal static class RechargerFastChargePatch
	{
		private static readonly Dictionary<string, DateTime> LastLog = new Dictionary<string, DateTime>();

		private static readonly TimeSpan LogInterval = TimeSpan.FromSeconds(30.0);

		internal static void Apply(Harmony harmony)
		{
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(PowerHelper), "ChargeBattery", new Type[2]
				{
					typeof(GameItem),
					typeof(int)
				}, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning("RechargerFastCharge: ChargeBattery(GameItem,int) not found");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(RechargerFastChargePatch), "PrefixCharge", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("RechargerFastCharge: 已挂钩 PowerHelper.ChargeBattery（prefix 翻倍）");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("RechargerFastCharge Apply: " + ex.Message);
			}
		}

		public static void PrefixCharge(GameItem __0, ref int __1)
		{
			if (__0 == null || __1 <= 0)
			{
				return;
			}
			try
			{
				GameItem val = OwnerFinder.FindAncestor(__0, "recharger");
				if (val != null && TagUtil.GetInt(val, "FMU_RECHARGER_FAST_CHARGE") >= 1)
				{
					int before = __1;
					__1 *= 2;
					LogOnce(__0, before, __1);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("RechargerFastCharge: " + ex.Message);
			}
		}

		private static void LogOnce(GameItem battery, int before, int after)
		{
			try
			{
				string text = TagUtil.SafeId(battery);
				if (string.IsNullOrEmpty(text))
				{
					text = battery.uniqueId.ToString();
				}
				DateTime utcNow = DateTime.UtcNow;
				if (!LastLog.TryGetValue(text, out var value) || !(utcNow - value < LogInterval))
				{
					if (LastLog.Count > 200)
					{
						LastLog.Clear();
					}
					LastLog[text] = utcNow;
					Mod.Log.Msg($"[FMU] recharger 快充生效：{text} 单次充入 {before} → {after}");
				}
			}
			catch
			{
			}
		}
	}
	internal static class RechargerSpritePatch
	{
		private const float CellPixels = 16f;

		private static readonly HashSet<int> Logged = new HashSet<int>();

		internal static void Apply(Harmony harmony)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = FindSpriteFunc();
				if (methodInfo == null)
				{
					Mod.Log.Warning("RechargerSprite: 没找到 MachineRecharger 的贴图回调，电池外观裁切未生效");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(RechargerSpritePatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("RechargerSprite: 已挂钩 " + methodInfo.DeclaringType?.Name + "::" + methodInfo.Name);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("RechargerSprite Apply: " + ex.Message);
			}
		}

		private static MethodInfo? FindSpriteFunc()
		{
			Type type = null;
			try
			{
				type = typeof(MachineRecharger);
			}
			catch
			{
			}
			if ((object)type == null)
			{
				type = GameTypes.Find("MachineRecharger");
			}
			if (type == null)
			{
				return null;
			}
			Type[] nestedTypes = type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic);
			Type[] array = nestedTypes;
			for (int i = 0; i < array.Length; i++)
			{
				MethodInfo method = array[i].GetMethod("_Recharger_b__4", BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					return method;
				}
			}
			Type typeFromHandle = typeof(List<ItemSpriteModifier>);
			array = nestedTypes;
			for (int i = 0; i < array.Length; i++)
			{
				MethodInfo[] methods = array[i].GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					if (methodInfo.ReturnType == typeFromHandle && methodInfo.GetParameters().Length == 1)
					{
						return methodInfo;
					}
				}
			}
			return null;
		}

		public static void Postfix(List<ItemSpriteModifier> __result)
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			if (__result == null)
			{
				return;
			}
			int num = Config.RechargerVisibleCols?.Value ?? 4;
			if (num <= 0)
			{
				return;
			}
			float num2 = (float)num * 16f;
			int num3 = 0;
			try
			{
				for (int num4 = __result.Count - 1; num4 >= 0; num4--)
				{
					if (!(__result[num4].position.x < num2))
					{
						__result.RemoveAt(num4);
						num3++;
					}
				}
			}
			catch (Exception ex)
			{
				if (Logged.Add(-1))
				{
					Mod.Log.Warning("RechargerSprite Postfix: " + ex.Message);
				}
				return;
			}
			if (num3 > 0 && Mod.Verbose && Logged.Add(num3))
			{
				Mod.Log.Msg($"[FMU] recharger 外观：裁掉 {num3} 个排到机身外的电池图标（可见上限 {num} 个）");
			}
		}
	}
	internal static class RechargerStructural
	{
		private const string ProgExpand = "RE_EXPAND";

		private const string ProgFast = "RE_FAST";

		private const int GridCols = 4;

		private const int GridRowsBase = 2;

		private const int GridRowsUp = 4;

		internal static bool AcceptsMaterial(GameItem machine, string matId)
		{
			if (TagUtil.GetInt(machine, "FMU_RECHARGER_CAPACITY") < 1 && RecipeUtil.ContainsId(Config.RechargerExpand.Value, matId))
			{
				return true;
			}
			if (TagUtil.GetInt(machine, "FMU_RECHARGER_FAST_CHARGE") < 1 && RecipeUtil.ContainsId(Config.RechargerFast.Value, matId))
			{
				return true;
			}
			return false;
		}

		internal static bool TryApply(GameItem machine, GameItem material, string matId)
		{
			if (RecipeUtil.ContainsId(Config.RechargerExpand.Value, matId))
			{
				return ApplyExpand(machine, material, matId);
			}
			if (RecipeUtil.ContainsId(Config.RechargerFast.Value, matId))
			{
				return ApplyFast(machine, material, matId);
			}
			return false;
		}

		private static bool ApplyExpand(GameItem machine, GameItem material, string matId)
		{
			if (TagUtil.GetInt(machine, "FMU_RECHARGER_CAPACITY") >= 1)
			{
				return false;
			}
			if (RecipeUtil.IsFilled(machine, "RE_EXPAND", Config.RechargerExpand.Value) || TagUtil.GetInt(machine, "FMU_RECHARGER_3D_PARTS") >= 999)
			{
				if (TryExpandGrid(machine))
				{
					TagUtil.SetInt(machine, "FMU_RECHARGER_CAPACITY", 1);
					RecipeUtil.ClearProgress(machine, "RE_EXPAND", Config.RechargerExpand.Value);
					Mod.Log.Msg("[FMU] recharger 扩容完成(重试): 4x2→4x4");
					return true;
				}
				return false;
			}
			if (!RecipeUtil.TryDepositFill(machine, material, matId, "RE_EXPAND", Config.RechargerExpand.Value, out var justCompleted))
			{
				return false;
			}
			if (justCompleted)
			{
				if (TryExpandGrid(machine))
				{
					TagUtil.SetInt(machine, "FMU_RECHARGER_CAPACITY", 1);
					Mod.Log.Msg("[FMU] recharger 扩容完成: 电池格 4x2→4x4");
				}
				else
				{
					TagUtil.SetInt(machine, "FMU_RECHARGER_3D_PARTS", 999);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("[FMU] recharger 扩容材料已满，窗口未就绪；重开后再拖配方材料可重试");
					}
				}
			}
			else if (Mod.Verbose)
			{
				Mod.Log.Msg("[FMU] recharger 扩容 " + RecipeUtil.FormatFillProgress(machine, "RE_EXPAND", Config.RechargerExpand.Value));
			}
			return true;
		}

		private static bool ApplyFast(GameItem machine, GameItem material, string matId)
		{
			if (TagUtil.GetInt(machine, "FMU_RECHARGER_FAST_CHARGE") >= 1)
			{
				return false;
			}
			if (!RecipeUtil.TryDepositFill(machine, material, matId, "RE_FAST", Config.RechargerFast.Value, out var justCompleted))
			{
				return false;
			}
			if (justCompleted)
			{
				TagUtil.SetInt(machine, "FMU_RECHARGER_FAST_CHARGE", 1);
				Mod.Log.Msg("[FMU] recharger 快充完成：有电时过夜充电翻倍");
			}
			else if (Mod.Verbose)
			{
				Mod.Log.Msg("[FMU] recharger 快充 " + RecipeUtil.FormatFillProgress(machine, "RE_FAST", Config.RechargerFast.Value));
			}
			return true;
		}

		internal static void TryRestore(GameItem machine)
		{
			if ((TagUtil.GetInt(machine, "FMU_RECHARGER_CAPACITY") >= 1 || TagUtil.GetInt(machine, "FMU_RECHARGER_3D_PARTS") >= 999 || RecipeUtil.IsFilled(machine, "RE_EXPAND", Config.RechargerExpand.Value)) && TryExpandGrid(machine))
			{
				TagUtil.SetInt(machine, "FMU_RECHARGER_CAPACITY", 1);
				TagUtil.SetInt(machine, "FMU_RECHARGER_3D_PARTS", 0);
				RecipeUtil.ClearProgress(machine, "RE_EXPAND", Config.RechargerExpand.Value);
			}
		}

		private static bool TryExpandGrid(GameItem machine)
		{
			try
			{
				if (machine.contentWindow == null)
				{
					return false;
				}
				GameGridInventory val = GridFinder.FindBestGrid(machine, 4, 2);
				if (val == null)
				{
					return false;
				}
				int num = 4;
				int num2 = 2;
				try
				{
					GridShape inventoryShape = val.inventoryShape;
					if (inventoryShape != null)
					{
						num = ((inventoryShape.width > 0) ? inventoryShape.width : 4);
						num2 = inventoryShape.height;
					}
				}
				catch
				{
				}
				if (num2 >= 4)
				{
					return true;
				}
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"[FMU] recharger 电池格 {num}x{num2} → 申请 {num}x{4}");
				}
				PixelWindow contentWindow = machine.contentWindow;
				int num3 = 0;
				try
				{
					num3 = contentWindow.heightPixels;
				}
				catch
				{
				}
				val.SetShape((num > 0) ? num : 4, 4);
				try
				{
					contentWindow.Validate();
				}
				catch
				{
				}
				try
				{
					if (contentWindow.heightPixels <= num3 && num3 > 0)
					{
						int num4 = Math.Max(1, val.heightPixels / Math.Max(1, 4));
						contentWindow.ResizePixels(contentWindow.widthPixels, num3 + num4 * (4 - num2));
						contentWindow.Validate();
					}
				}
				catch
				{
				}
				return true;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("RechargerStructural TryExpandGrid: " + ex.Message);
				return false;
			}
		}

		internal static void AppendTooltip(RichTextBuilder b, GameItem machine)
		{
			TooltipUi.BeginSection(b);
			if (TagUtil.GetInt(machine, "FMU_RECHARGER_CAPACITY") >= 1)
			{
				TooltipUi.Status(b, "扩容", "电池格 4×4", highlight: true);
			}
			else
			{
				TooltipUi.NextTier(b, "扩容充电仓", "电池格 4×2 → 4×4");
				TooltipUi.RecipeProgress(b, machine, "RE_EXPAND", Config.RechargerExpand.Value);
			}
			if (TagUtil.GetInt(machine, "FMU_RECHARGER_FAST_CHARGE") >= 1)
			{
				TooltipUi.Status(b, "快充", "过夜充电 ×2（叠加原版）", highlight: true);
				return;
			}
			TooltipUi.NextTier(b, "快充电路", "过夜充电 ×2（叠加原版）");
			TooltipUi.RecipeProgress(b, machine, "RE_FAST", Config.RechargerFast.Value);
		}
	}
	internal static class RestorePatch
	{
		internal static void Apply(Harmony harmony)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(PlayerStore), "EndNight", (Type[])null, (Type[])null);
				if (methodInfo != null)
				{
					harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(RestorePatch), "PostfixEndNight", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("RestorePatch: EndNight 已挂钩");
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("RestorePatch EndNight: " + ex.Message);
			}
		}

		public static void PostfixEndNight()
		{
			GridResize.Reset();
			TryRestoreAll();
		}

		internal static void TryRestoreOnePublic(GameItem item)
		{
			TryRestoreOne(item);
		}

		private static void TryRestoreAll()
		{
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					return;
				}
				List<GameItem> val = instance.FindAllItem(true);
				if (val == null)
				{
					return;
				}
				Enumerator<GameItem> enumerator = val.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					if (current != null)
					{
						TryRestoreOne(current);
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("RestorePatch TryRestoreAll: " + ex.Message);
			}
		}

		private static void TryRestoreOne(GameItem item)
		{
			try
			{
				string text = TagUtil.SafeId(item);
				switch (text)
				{
				case "recharger":
					RechargerStructural.TryRestore(item);
					break;
				case "wine_rack":
					WineRackStructural.TryRestore(item);
					break;
				case "powerblock":
					PowerBlocStructural.TryRestore(item);
					break;
				}
				if (text != "wine_rack")
				{
					ModuleBayStructural.TryRestore(item);
				}
			}
			catch
			{
			}
		}
	}
	internal static class StatStructural
	{
		internal static bool AcceptsFurnace(GameItem machine, string matId)
		{
			if (RecipeUtil.MatchesRepeatable(Config.FurnaceIngot.Value, matId) && TagUtil.GetInt(machine, "FMU_FURNACE_INGOT") < Config.FurnaceIngotMax.Value)
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.FurnaceCircuit.Value, matId) && TagUtil.GetInt(machine, "FMU_FURNACE_CIRCUIT") < Config.FurnaceCircuitMax.Value)
			{
				return true;
			}
			return false;
		}

		internal static bool AcceptsMoisture(GameItem machine, string matId)
		{
			if (RecipeUtil.MatchesRepeatable(Config.MoistureCircuit.Value, matId) && TagUtil.GetInt(machine, "FMU_MOISTURE_CIRCUIT") < Config.MoistureCircuitMax.Value)
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.MoistureWire.Value, matId) && TagUtil.GetInt(machine, "FMU_MOISTURE_WIRE") < Config.MoistureWireMax.Value)
			{
				return true;
			}
			return false;
		}

		internal static bool AcceptsProjector(GameItem machine, string matId)
		{
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorIngot.Value, matId))
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorCircuit.Value, matId))
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorWire.Value, matId))
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorPlastic.Value, matId) && TagUtil.GetInt(machine, "FMU_PROJECTOR_PLASTIC") < Config.ProjectorPlasticMax.Value)
			{
				return true;
			}
			return false;
		}

		internal static bool TryApplyFurnace(GameItem machine, GameItem material, string matId)
		{
			if (RecipeUtil.MatchesRepeatable(Config.FurnaceIngot.Value, matId))
			{
				return Bump(machine, material, "FMU_FURNACE_INGOT", Config.FurnaceIngotMax.Value, "熔炉保温炉衬");
			}
			if (RecipeUtil.MatchesRepeatable(Config.FurnaceCircuit.Value, matId))
			{
				return Bump(machine, material, "FMU_FURNACE_CIRCUIT", Config.FurnaceCircuitMax.Value, "熔炉温度传感器");
			}
			return false;
		}

		internal static bool TryApplyMoisture(GameItem machine, GameItem material, string matId)
		{
			if (RecipeUtil.MatchesRepeatable(Config.MoistureCircuit.Value, matId))
			{
				return Bump(machine, material, "FMU_MOISTURE_CIRCUIT", Config.MoistureCircuitMax.Value, "水质检测探头");
			}
			if (RecipeUtil.MatchesRepeatable(Config.MoistureWire.Value, matId))
			{
				return Bump(machine, material, "FMU_MOISTURE_WIRE", Config.MoistureWireMax.Value, "冷凝铜管");
			}
			return false;
		}

		internal static bool TryApplyProjector(GameItem machine, GameItem material, string matId)
		{
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorIngot.Value, matId))
			{
				return ApplyProjectorIngot(machine, material);
			}
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorCircuit.Value, matId))
			{
				return Bump(machine, material, "FMU_PROJECTOR_CIRCUIT", int.MaxValue, "成像电路");
			}
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorPlastic.Value, matId))
			{
				return Bump(machine, material, "FMU_PROJECTOR_PLASTIC", Config.ProjectorPlasticMax.Value, "聚光透镜");
			}
			if (RecipeUtil.MatchesRepeatable(Config.ProjectorWire.Value, matId))
			{
				return Bump(machine, material, "FMU_PROJECTOR_WIRE", int.MaxValue, "供电线");
			}
			return false;
		}

		private static bool ApplyProjectorIngot(GameItem machine, GameItem material)
		{
			int num = TagUtil.GetInt(machine, "FMU_PROJECTOR_CIRCUIT");
			TagUtil.SetInt(machine, "FMU_PROJECTOR_CIRCUIT", num + 1);
			TagUtil.AddInt(machine, "FMU_PROJECTOR_WIRE", 1);
			BonusLedger.Grant(machine, "FMU_PROJECTOR_CIRCUIT", 1);
			BonusLedger.Grant(machine, "FMU_PROJECTOR_WIRE", 1);
			TagUtil.ConsumeOne(material);
			Mod.Log.Msg("[FMU] projector 散热片：成像电路+1 且供电线+1");
			return true;
		}

		private static bool Bump(GameItem machine, GameItem material, string countTag, int max, string label = "")
		{
			int num = TagUtil.GetInt(machine, countTag);
			if (max != int.MaxValue && num >= max)
			{
				return false;
			}
			TagUtil.SetInt(machine, countTag, num + 1);
			BonusLedger.Grant(machine, countTag, 1);
			TagUtil.ConsumeOne(material);
			string value = ((max == int.MaxValue) ? "∞" : max.ToString());
			Mod.Log.Msg($"[FMU] {label} {num + 1}/{value}");
			return true;
		}

		internal static void AppendTooltipFurnace(RichTextBuilder b, GameItem m)
		{
			TooltipUi.BeginSection(b);
			BonusLedger.AppendTooltip(b, m);
			TooltipUi.Meter(b, "保温炉衬", TagUtil.GetInt(m, "FMU_FURNACE_INGOT"), Config.FurnaceIngotMax.Value, TooltipUi.RecipeMatsHint(Config.FurnaceIngot.Value), BonusLedger.Describe("FMU_FURNACE_INGOT"));
			TooltipUi.Meter(b, "温度传感器", TagUtil.GetInt(m, "FMU_FURNACE_CIRCUIT"), Config.FurnaceCircuitMax.Value, TooltipUi.RecipeMatsHint(Config.FurnaceCircuit.Value), BonusLedger.Describe("FMU_FURNACE_CIRCUIT"));
		}

		internal static void AppendTooltipMoisture(RichTextBuilder b, GameItem m)
		{
			TooltipUi.BeginSection(b);
			BonusLedger.AppendTooltip(b, m);
			TooltipUi.Meter(b, "水质探头", TagUtil.GetInt(m, "FMU_MOISTURE_CIRCUIT"), Config.MoistureCircuitMax.Value, TooltipUi.RecipeMatsHint(Config.MoistureCircuit.Value), BonusLedger.Describe("FMU_MOISTURE_CIRCUIT"));
			TooltipUi.Meter(b, "冷凝铜管", TagUtil.GetInt(m, "FMU_MOISTURE_WIRE"), Config.MoistureWireMax.Value, TooltipUi.RecipeMatsHint(Config.MoistureWire.Value), BonusLedger.Describe("FMU_MOISTURE_WIRE"));
		}

		internal static void AppendTooltipProjector(RichTextBuilder b, GameItem m)
		{
			TooltipUi.BeginSection(b);
			BonusLedger.AppendTooltip(b, m);
			TooltipUi.Meter(b, "成像电路", TagUtil.GetInt(m, "FMU_PROJECTOR_CIRCUIT"), int.MaxValue, TooltipUi.RecipeMatsHint(Config.ProjectorCircuit.Value), BonusLedger.Describe("FMU_PROJECTOR_CIRCUIT"));
			TooltipUi.Meter(b, "聚光透镜", TagUtil.GetInt(m, "FMU_PROJECTOR_PLASTIC"), Config.ProjectorPlasticMax.Value, TooltipUi.RecipeMatsHint(Config.ProjectorPlastic.Value), BonusLedger.Describe("FMU_PROJECTOR_PLASTIC"));
			TooltipUi.Meter(b, "供电线", TagUtil.GetInt(m, "FMU_PROJECTOR_WIRE"), int.MaxValue, TooltipUi.RecipeMatsHint(Config.ProjectorWire.Value), BonusLedger.Describe("FMU_PROJECTOR_WIRE"));
		}
	}
	internal static class StructuralRouter
	{
		internal static bool IsSupportedMachine(string id)
		{
			bool flag;
			switch (id)
			{
			case "projector":
			case "recharger":
			case "wine_rack":
			case "water_purifier":
			case "furnace":
			case "moisture_farm":
			case "mirage_projector":
			case "powerblock":
			case "desequencer":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (!flag)
			{
				return false;
			}
			return ModPresence.MachineAvailable(id);
		}

		internal static bool CanHandle(GameItem machine, string machineId)
		{
			if (!IsSupportedMachine(machineId))
			{
				return ModuleBayStructural.IsModuleBayMachine(machineId);
			}
			return true;
		}

		internal static bool TryApply(GameItem machine, GameItem material, string machineId, string matId)
		{
			try
			{
				bool flag;
				switch (machineId)
				{
				case "recharger":
					flag = RechargerStructural.TryApply(machine, material, matId);
					break;
				case "wine_rack":
					flag = WineRackStructural.TryApply(machine, material, matId);
					break;
				case "water_purifier":
					flag = PurifierStructural.TryApply(machine, material, matId);
					break;
				case "furnace":
					flag = StatStructural.TryApplyFurnace(machine, material, matId);
					break;
				case "moisture_farm":
					flag = StatStructural.TryApplyMoisture(machine, material, matId);
					break;
				case "projector":
				case "mirage_projector":
					flag = StatStructural.TryApplyProjector(machine, material, matId);
					break;
				case "powerblock":
					flag = PowerBlocStructural.TryApply(machine, material, matId);
					break;
				case "desequencer":
					flag = DesequencerStructural.TryApply(machine, material, matId);
					break;
				default:
					flag = false;
					break;
				}
				if (flag)
				{
					return true;
				}
				if (ModuleBayStructural.IsModuleBayMachine(machineId))
				{
					if (IsSupportedMachine(machineId) && Mod.Verbose)
					{
						GridFinder.LogOnce(machine, "modbay-both", "[FMU] " + machineId + " 既在专用配方列表、又在模组仓加宽名单；专用配方优先，加宽走 fallback");
					}
					return ModuleBayStructural.TryApply(machine, material, matId);
				}
				return false;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning($"StructuralRouter [{machineId}/{matId}]: {ex.Message}");
				return false;
			}
		}

		internal static bool IsUpgradeablePair(GameItem? machine, GameItem? material)
		{
			if (machine == null || material == null)
			{
				return false;
			}
			try
			{
				string text = TagUtil.SafeId(machine);
				string text2 = TagUtil.SafeId(material);
				if (string.IsNullOrEmpty(text2) || !CanHandle(machine, text))
				{
					return false;
				}
				if (IsSupportedMachine(text))
				{
					bool result;
					switch (text)
					{
					case "powerblock":
						result = PowerBlocStructural.AcceptsMaterial(machine, text2);
						break;
					case "recharger":
						result = RechargerStructural.AcceptsMaterial(machine, text2);
						break;
					case "wine_rack":
						result = WineRackStructural.AcceptsMaterial(machine, text2);
						break;
					case "water_purifier":
						result = PurifierStructural.AcceptsMaterial(machine, text2);
						break;
					case "furnace":
						result = StatStructural.AcceptsFurnace(machine, text2);
						break;
					case "moisture_farm":
						result = StatStructural.AcceptsMoisture(machine, text2);
						break;
					case "projector":
					case "mirage_projector":
						result = StatStructural.AcceptsProjector(machine, text2);
						break;
					case "desequencer":
						result = DesequencerStructural.AcceptsMaterial(machine, text2);
						break;
					default:
						result = ModuleBayStructural.AcceptsMaterial(machine, text2);
						break;
					}
					return result;
				}
				return ModuleBayStructural.AcceptsMaterial(machine, text2);
			}
			catch
			{
				return false;
			}
		}
	}
	internal static class Tags
	{
		internal const string RechargerCap = "FMU_RECHARGER_CAPACITY";

		internal const string RechargerPlastic = "FMU_RECHARGER_3D_PARTS";

		internal const string RechargerFast = "FMU_RECHARGER_FAST_CHARGE";

		internal const string RechargerCircuits = "FMU_RECHARGER_CIRCUITS";

		internal const string RechargerWires = "FMU_RECHARGER_WIRES";

		internal const string WineWidthLevel = "FMU_WINERACK_WIDTH_LEVEL";

		internal const string WineWidthBase = "FMU_WINERACK_MOD_BASE";

		internal const string WineBottleLevel = "FMU_WINERACK_BOTTLE_LEVEL";

		internal const string WineBottleBase = "FMU_WINERACK_BOTTLE_BASE";

		internal const string WineCircuit = "FMU_WINERACK_CIRCUIT";

		internal const string WineWire = "FMU_WINERACK_WIRE";

		internal const string ModuleBayLevel = "FMU_MODULEBAY_WIDTH_LEVEL";

		internal const string ModuleBayBase = "FMU_MODULEBAY_WIDTH_BASE";

		internal const string GridWineMark = "FMU_GRID_WINE";

		internal const string GridModuleMark = "FMU_GRID_MODULE";

		internal const string PurifierFilterParts = "FMU_PURIFIER_FILTER_PARTS";

		internal const string PurifierIngot = "FMU_PURIFIER_INGOT";

		internal const string FurnaceIngot = "FMU_FURNACE_INGOT";

		internal const string FurnaceCircuit = "FMU_FURNACE_CIRCUIT";

		internal const string MoistureCircuit = "FMU_MOISTURE_CIRCUIT";

		internal const string MoistureWire = "FMU_MOISTURE_WIRE";

		internal const string ProjectorCircuit = "FMU_PROJECTOR_CIRCUIT";

		internal const string ProjectorPlastic = "FMU_PROJECTOR_PLASTIC";

		internal const string ProjectorWire = "FMU_PROJECTOR_WIRE";

		internal const string PowerBlocRange = "FMU_POWERBLOC_RANGE";

		internal const string WineLabelUnlock = "FMU_WINE_LABEL_UNLOCK";

		internal const string PurifierClearUnlock = "FMU_PURIFIER_CLEAR_UNLOCK";

		internal const string DeseqInstantUnlock = "FMU_DESEQ_INSTANT_UNLOCK";

		internal const string TotalQuality = "TOTAL_PERCENTAGE_QUALITY_BONUS_INT";

		internal const string TotalPerformance = "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT";

		internal const string TotalPower = "TOTAL_PERCENTAGE_POWER_BONUS_INT";

		internal const string VanillaModuleStage = "MODULE_UPGRADE_STAGE_INT";

		internal const int VanillaModuleStageMax = 2;
	}
	internal static class TagUtil
	{
		internal static int GetInt(GameItem item, string tagKey, int fallback = 0)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly(tagKey);
				if (tagReadonly == null)
				{
					return fallback;
				}
				return tagReadonly.valueInt;
			}
			catch
			{
				return fallback;
			}
		}

		internal static void SetInt(GameItem item, string tagKey, int value)
		{
			Action<TagState> action = (TagState tag) =>
			{
				tag.valueInt = value;
				tag.valueEnabled = true;
			};
			item.ModifyTag(tagKey, Action<TagState>.op_Implicit(action), false);
		}

		internal static void AddInt(GameItem item, string tagKey, int delta)
		{
			if (delta != 0)
			{
				SetInt(item, tagKey, GetInt(item, tagKey) + delta);
			}
		}

		internal static string SafeId(GameItem? item)
		{
			try
			{
				return ((item != null) ? item.identifier : null) ?? "";
			}
			catch
			{
				return "";
			}
		}

		internal static void ConsumeOne(GameItem material)
		{
			try
			{
				if (material.unitCount > 1)
				{
					material.unitCount -= 1;
				}
				else
				{
					material.Destroy();
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("ConsumeOne: " + ex.Message);
			}
		}
	}
	internal static class TooltipPatch
	{
		internal static void Apply(Harmony harmony)
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(ModHook), "FireOnCreateTooltipLate", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning("TooltipPatch: ModHook.FireOnCreateTooltipLate not found");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(TooltipPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("TooltipPatch: FireOnCreateTooltipLate 已挂钩");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("TooltipPatch: " + ex.Message);
			}
		}

		public static void Postfix(RichTextBuilder builder, GameItem item)
		{
			try
			{
				if (builder != null && item != null)
				{
					RestorePatch.TryRestoreOnePublic(item);
					switch (TagUtil.SafeId(item))
					{
					case "powerblock":
						PowerBlocStructural.AppendTooltip(builder, item);
						break;
					case "recharger":
						RechargerStructural.AppendTooltip(builder, item);
						break;
					case "wine_rack":
						WineRackStructural.AppendTooltip(builder, item);
						break;
					case "water_purifier":
						PurifierStructural.AppendTooltip(builder, item);
						break;
					case "furnace":
						StatStructural.AppendTooltipFurnace(builder, item);
						break;
					case "moisture_farm":
						StatStructural.AppendTooltipMoisture(builder, item);
						break;
					case "projector":
					case "mirage_projector":
						StatStructural.AppendTooltipProjector(builder, item);
						break;
					case "desequencer":
						DesequencerStructural.AppendTooltip(builder, item);
						break;
					}
					ModuleBayStructural.TryAppendTooltip(builder, item);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("TooltipPatch Postfix: " + ex.Message);
			}
		}
	}
	internal static class TooltipUi
	{
		private static readonly Dictionary<string, string> FallbackNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["electronic"] = "电子零件",
			["common_electronic"] = "电子零件（普通）",
			["advanced_electronic"] = "电子零件（高级）",
			["rare_electronic"] = "电子零件（稀有）",
			["wire"] = "线缆",
			["printer_plastic"] = "打印材料",
			["metal_ingot"] = "金属锭",
			["water_filter"] = "滤水器",
			["water_filter_adv"] = "高级滤水器",
			["glass"] = "玻璃"
		};

		internal static void BeginSection(RichTextBuilder b)
		{
			b.AddEmptyLine(6);
			b.AddLine("机器升级", true, (ColorPalette)8877056, true, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
		}

		internal static void Status(RichTextBuilder b, string label, string value, bool highlight = false)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			ColorPalette val = (ColorPalette)(highlight ? 5538048 : 12895408);
			b.AddLine(label + "  ·  " + value, true, val, highlight, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
		}

		internal static void Hint(RichTextBuilder b, string text)
		{
			b.AddLine(text, true, (ColorPalette)11058895, false, true, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
		}

		internal static void NextTier(RichTextBuilder b, string tierName, string? effect = null)
		{
			string text = "下一档  ·  " + tierName;
			if (!string.IsNullOrEmpty(effect))
			{
				text = text + "  ·  " + effect;
			}
			b.AddLine(text, true, (ColorPalette)8877056, false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
		}

		internal static void Done(RichTextBuilder b, string text = "已满级")
		{
			b.AddLine(text, true, (ColorPalette)5538048, false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
		}

		internal static void RecipeProgress(RichTextBuilder b, GameItem machine, string prefix, string? raw)
		{
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			List<(string, int)> list = RecipeUtil.Parse(raw);
			if (list.Count == 0)
			{
				Hint(b, "（未配置配方）");
				return;
			}
			foreach (var item3 in list)
			{
				string item = item3.Item1;
				int item2 = item3.Item2;
				int progress = RecipeUtil.GetProgress(machine, prefix, item);
				string value = MatName(item);
				bool flag = progress >= item2;
				ColorPalette val = (ColorPalette)(flag ? 5538048 : 8877056);
				b.AddLine($"  {value}  {progress}/{item2}", true, val, flag, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
			}
		}

		internal static void Meter(RichTextBuilder b, string label, int cur, int max, string? matHint = null, string? effect = null)
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			bool flag = max > 0 && cur >= max;
			ColorPalette val = (ColorPalette)(flag ? 5538048 : ((cur > 0) ? 8877056 : 12895408));
			string text = ((max <= 0 || max == int.MaxValue) ? $"{label}  ·  {cur}" : $"{label}  ·  {cur}/{max}");
			if (!string.IsNullOrEmpty(effect))
			{
				text = text + "  ·  " + effect;
			}
			if (!string.IsNullOrEmpty(matHint))
			{
				text = text + "  （" + matHint + "）";
			}
			b.AddLine(text, true, val, flag, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
		}

		internal static string MatName(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return "?";
			}
			try
			{
				string text = (id.StartsWith("item_", StringComparison.OrdinalIgnoreCase) ? id : ("item_" + id + "_name"));
				Object[] array = new Object[0];
				string localizedItem = LocHelper.GetLocalizedItem(text, array);
				if (!string.IsNullOrEmpty(localizedItem) && !localizedItem.Contains("Translation Error", StringComparison.OrdinalIgnoreCase) && !string.Equals(localizedItem, text, StringComparison.Ordinal))
				{
					return localizedItem;
				}
			}
			catch
			{
			}
			if (FallbackNames.TryGetValue(id, out string value))
			{
				return value;
			}
			return id;
		}

		internal static string RecipeMatsHint(string? raw)
		{
			List<(string, int)> list = RecipeUtil.Parse(raw);
			if (list.Count == 0)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(" + ");
				}
				var (id, num) = list[i];
				stringBuilder.Append(MatName(id));
				if (num > 1)
				{
					stringBuilder.Append('×').Append(num);
				}
			}
			return stringBuilder.ToString();
		}
	}
	internal static class UniqueEffectsPatch
	{
		internal static void Apply(Harmony harmony)
		{
			TryPatch(harmony, typeof(WineHelper), "OnAgeWine", "PostfixOnAgeWine");
			TryPatch(harmony, typeof(MachinePurifier), "PurifyContainer", "PostfixPurifyContainer");
			TryPatch(harmony, typeof(MachineProgressHelper), "ContinueProgressTypeMachine", "PostfixContinueProgress");
		}

		private static void TryPatch(Harmony harmony, Type type, string method, string postfix)
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(type, method, (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning($"UniqueEffects: {type.Name}.{method} 未找到");
				}
				else
				{
					harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(UniqueEffectsPatch), postfix, (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("UniqueEffects: 已挂钩 " + type.Name + "." + method);
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning($"UniqueEffects patch {type.Name}.{method}: {ex.Message}");
			}
		}

		public static void PostfixOnAgeWine(GameItem item)
		{
			try
			{
				if (item == null)
				{
					return;
				}
				GameItem val = FindParentMachine(item, "wine_rack");
				if (val == null || !UniqueUnlock.IsUnlocked(val, "FMU_WINE_LABEL_UNLOCK") || !UniqueUnlock.Roll(Config.WineLabelChance.Value))
				{
					return;
				}
				try
				{
					if (item.IsGameItemType("MEDICAL") || item.IsGameItemType("WATER"))
					{
						return;
					}
				}
				catch
				{
				}
				bool flag = false;
				try
				{
					flag = RNG.Roll(50);
				}
				catch
				{
					flag = false;
				}
				string text = (flag ? "WATER" : "MEDICAL");
				try
				{
					item.SetGameItemType(text);
					Mod.Log.Msg($"[FMU] wine_rack 陈酿挂签 → {text}（{(flag ? "水" : "麻醉品")}）");
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("UniqueEffects SetGameItemType: " + ex.Message);
				}
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("UniqueEffects OnAgeWine: " + ex2.Message);
			}
		}

		public static void PostfixPurifyContainer(GameItem machine, GameItem waterContainer, bool ignoreBonus)
		{
			try
			{
				if (machine != null && waterContainer != null && string.Equals(TagUtil.SafeId(machine), "water_purifier", StringComparison.OrdinalIgnoreCase) && UniqueUnlock.IsUnlocked(machine, "FMU_PURIFIER_CLEAR_UNLOCK") && UniqueUnlock.Roll(Config.PurifierClearChance.Value) && TryClearOneContaminant(waterContainer, out string clearedId))
				{
					Mod.Log.Msg("[FMU] water_purifier 清空杂质 → " + clearedId);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("UniqueEffects PurifyContainer: " + ex.Message);
			}
		}

		public static void PostfixContinueProgress(GameItem machine)
		{
			try
			{
				if (machine == null || !string.Equals(TagUtil.SafeId(machine), "desequencer", StringComparison.OrdinalIgnoreCase) || !UniqueUnlock.IsUnlocked(machine, "FMU_DESEQ_INSTANT_UNLOCK"))
				{
					return;
				}
				try
				{
					if (MachineProgressHelper.IsProgressTypeMachineFinished(machine))
					{
						return;
					}
				}
				catch
				{
					return;
				}
				try
				{
					ValueTuple<int, int> progress = MachineProgressHelper.GetProgress(machine);
					if (progress.Item2 <= 0 || progress.Item1 <= 0)
					{
						return;
					}
				}
				catch
				{
				}
				if (UniqueUnlock.Roll(Config.DeseqInstantChance.Value))
				{
					if (!TryForceFinishDesequencer(machine))
					{
						Mod.Log.Warning("[FMU] desequencer 瞬解：未能完成（槽位或进度）");
					}
					else
					{
						Mod.Log.Msg("[FMU] desequencer 瞬解成功");
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("UniqueEffects ContinueProgress: " + ex.Message);
			}
		}

		private static bool TryForceFinishDesequencer(GameItem machine)
		{
			try
			{
				MachineProgressHelper.FinishProgressTypeMachine(machine);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("FinishProgressTypeMachine: " + ex.Message);
			}
			if (!TryFindDeseqSlots(machine, out GameSlotInventory input, out GameSlotInventory output))
			{
				return false;
			}
			try
			{
				MachineDesequencer.FinishDesequencer(machine, input, output);
				return true;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("FinishDesequencer: " + ex2.Message);
				return false;
			}
		}

		private static bool TryFindDeseqSlots(GameItem machine, out GameSlotInventory? input, out GameSlotInventory? output)
		{
			input = null;
			output = null;
			List<GameSlotInventory> list = CollectSlots(machine);
			if (list.Count == 0)
			{
				return false;
			}
			GameSlotInventory val = null;
			foreach (GameSlotInventory item in list)
			{
				string text = SafeInvId(item);
				if (text.IndexOf("chip", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					val = item;
				}
				else if (text.IndexOf("output", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					output = item;
				}
				else if (text.IndexOf("input", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					input = item;
				}
			}
			if (input == null || output == null)
			{
				List<GameSlotInventory> list2 = new List<GameSlotInventory>();
				foreach (GameSlotInventory item2 in list)
				{
					if (item2 != val)
					{
						list2.Add(item2);
					}
				}
				if (list2.Count >= 2)
				{
					if (input == null)
					{
						input = list2[0];
					}
					if (output == null)
					{
						output = list2[list2.Count - 1];
					}
				}
				else if (list2.Count == 1 && input == null)
				{
					input = list2[0];
				}
			}
			if (input != null)
			{
				return output != null;
			}
			return false;
		}

		private static List<GameSlotInventory> CollectSlots(GameItem machine)
		{
			List<GameSlotInventory> list = new List<GameSlotInventory>();
			HashSet<IntPtr> seen = new HashSet<IntPtr>();
			try
			{
				PixelWindow contentWindow = machine.contentWindow;
				if (((contentWindow != null) ? contentWindow.childElement : null) != null)
				{
					WalkNode(((Il2CppObjectBase)contentWindow.childElement).TryCast<GraphNodeStorage>(), list, seen);
				}
				if (contentWindow != null)
				{
					WalkNode(contentWindow.child, list, seen);
				}
				List<GraphNodeStorage> val = ((contentWindow != null) ? contentWindow.children : null);
				if (val != null)
				{
					for (int i = 0; i < val.Count; i++)
					{
						WalkNode(val[i], list, seen);
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static void WalkNode(GraphNodeStorage? node, List<GameSlotInventory> list, HashSet<nint> seen)
		{
			if (node == null)
			{
				return;
			}
			try
			{
				GameSlotInventory val = ((Il2CppObjectBase)node).TryCast<GameSlotInventory>();
				if (val != null)
				{
					IntPtr intPtr;
					try
					{
						intPtr = ((Il2CppObjectBase)val).Pointer;
					}
					catch
					{
						intPtr = IntPtr.Zero;
					}
					if (intPtr != IntPtr.Zero && seen.Add(intPtr))
					{
						string text = SafeInvId(val);
						if (text.IndexOf("battery", StringComparison.OrdinalIgnoreCase) < 0 && text.IndexOf("module", StringComparison.OrdinalIgnoreCase) < 0)
						{
							list.Add(val);
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				List<GraphNodeStorage> children = node.children;
				if (children != null)
				{
					for (int i = 0; i < children.Count; i++)
					{
						WalkNode(children[i], list, seen);
					}
				}
			}
			catch
			{
			}
		}

		private static string SafeInvId(GameSlotInventory? slot)
		{
			try
			{
				return ((slot != null) ? ((GameInventory)slot).identifier : null) ?? "";
			}
			catch
			{
				return "";
			}
		}

		private static bool TryClearOneContaminant(GameItem container, out string clearedId)
		{
			clearedId = "";
			try
			{
				List<Liquid> liquids = Liquid.Liquids;
				if (liquids == null)
				{
					return false;
				}
				List<string> list = new List<string>();
				for (int i = 0; i < liquids.Count; i++)
				{
					Liquid val = liquids[i];
					if (val == null)
					{
						continue;
					}
					string text;
					try
					{
						text = val.identifier ?? "";
					}
					catch
					{
						continue;
					}
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					bool flag;
					try
					{
						flag = val.canBeRemovedByPurifier;
					}
					catch
					{
						flag = false;
					}
					if (flag)
					{
						string currentPartFromId = Liquid.GetCurrentPartFromId(text);
						if (!string.IsNullOrEmpty(currentPartFromId) && !(ReadVol(container, currentPartFromId) <= 0.0))
						{
							list.Add(text);
						}
					}
				}
				if (list.Count == 0)
				{
					return false;
				}
				string text2 = list[0];
				if (list.Count > 1)
				{
					try
					{
						text2 = list[Random.Range(0, list.Count)];
					}
					catch
					{
					}
				}
				ChemicalHelper.SetChemContentUL(container, text2, 0);
				try
				{
					if (container.IsTag("LIQUID_CONTAINER_TAG"))
					{
						WaterFeatureHelper.OnLiquidChanged(container, false);
					}
				}
				catch
				{
				}
				clearedId = text2;
				return true;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("TryClearOneContaminant: " + ex.Message);
				return false;
			}
		}

		private static double ReadVol(GameItem item, string tagName)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly(tagName);
				if (tagReadonly == null)
				{
					return 0.0;
				}
				long num = 0L;
				double num2 = 0.0;
				try
				{
					num = tagReadonly.GetInt();
				}
				catch
				{
				}
				try
				{
					num2 = tagReadonly.GetDouble();
				}
				catch
				{
				}
				return ((double)num > num2) ? ((double)num) : num2;
			}
			catch
			{
				return 0.0;
			}
		}

		private static GameItem? FindParentMachine(GameItem item, string machineId)
		{
			try
			{
				GameInventory parentInventory = item.parentInventory;
				int num = 0;
				while (parentInventory != null && num++ < 8)
				{
					GameItem val;
					try
					{
						val = parentInventory.GetParentItem();
					}
					catch
					{
						val = null;
					}
					if (val != null)
					{
						if (string.Equals(TagUtil.SafeId(val), machineId, StringComparison.OrdinalIgnoreCase))
						{
							return val;
						}
						try
						{
							parentInventory = val.parentInventory;
						}
						catch
						{
							break;
						}
						continue;
					}
					break;
				}
			}
			catch
			{
			}
			return null;
		}
	}
	internal static class UniqueUnlock
	{
		private const string ProgPrefix = "UQ";

		internal static bool IsUnlocked(GameItem machine, string unlockTag)
		{
			return TagUtil.GetInt(machine, unlockTag) > 0;
		}

		internal static bool Accepts(GameItem machine, string unlockTag, string? recipe, string matId)
		{
			if (IsUnlocked(machine, unlockTag))
			{
				return false;
			}
			return RecipeUtil.ContainsId(recipe, matId);
		}

		internal static bool TryDeposit(GameItem machine, GameItem material, string matId, string unlockTag, string? recipe, string logName)
		{
			if (IsUnlocked(machine, unlockTag))
			{
				return false;
			}
			if (!RecipeUtil.TryDepositFill(machine, material, matId, "UQ_" + unlockTag, recipe, out var justCompleted))
			{
				return false;
			}
			if (justCompleted)
			{
				TagUtil.SetInt(machine, unlockTag, 1);
				Mod.Log.Msg("[FMU] " + logName + " 独特升级已解锁");
			}
			else if (Mod.Verbose)
			{
				Mod.Log.Msg("[FMU] " + logName + " 独特 " + RecipeUtil.FormatFillProgress(machine, "UQ_" + unlockTag, recipe));
			}
			return true;
		}

		internal static void AppendTooltip(RichTextBuilder b, GameItem machine, string unlockTag, string title, string? recipe, float chance, string? effect = null)
		{
			if (IsUnlocked(machine, unlockTag))
			{
				TooltipUi.Status(b, title, $"已解锁 · 触发 {Clamp01(chance):0%}", highlight: true);
				return;
			}
			TooltipUi.NextTier(b, title, effect);
			TooltipUi.Hint(b, $"触发概率 {Clamp01(chance):0%}（升满后）");
			TooltipUi.RecipeProgress(b, machine, "UQ_" + unlockTag, recipe);
		}

		internal static bool Roll(float chance01)
		{
			int num = (int)Math.Round((double)Clamp01(chance01) * 100.0);
			if (num <= 0)
			{
				return false;
			}
			if (num >= 100)
			{
				return true;
			}
			try
			{
				return RNG.Roll(num);
			}
			catch
			{
				return false;
			}
		}

		internal static float Clamp01(float v)
		{
			if (v < 0f)
			{
				return 0f;
			}
			if (v > 1f)
			{
				return 1f;
			}
			return v;
		}
	}
	internal static class WineRackStructural
	{
		internal static bool AcceptsMaterial(GameItem machine, string matId)
		{
			if (UniqueUnlock.Accepts(machine, "FMU_WINE_LABEL_UNLOCK", Config.WineLabelUnlock.Value, matId))
			{
				return true;
			}
			if (AcceptsColumns(machine, matId, Config.WineWidth.Value, "FMU_WINERACK_WIDTH_LEVEL", Config.WineWidthMaxLevels.Value, requireModuleBay: true))
			{
				return true;
			}
			if (AcceptsColumns(machine, matId, Config.WineBottleCol.Value, "FMU_WINERACK_BOTTLE_LEVEL", Config.WineBottleColMaxLevels.Value, requireModuleBay: false))
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.WineCircuit.Value, matId))
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.WineWire.Value, matId))
			{
				int num = TagUtil.GetInt(machine, "FMU_WINERACK_CIRCUIT") / 2;
				int num2 = TagUtil.GetInt(machine, "FMU_WINERACK_WIRE");
				if (num >= 1 && num2 < num)
				{
					return true;
				}
			}
			return false;
		}

		internal static bool TryApply(GameItem machine, GameItem material, string matId)
		{
			if (!UniqueUnlock.IsUnlocked(machine, "FMU_WINE_LABEL_UNLOCK") && RecipeUtil.ContainsId(Config.WineLabelUnlock.Value, matId) && UniqueUnlock.TryDeposit(machine, material, matId, "FMU_WINE_LABEL_UNLOCK", Config.WineLabelUnlock.Value, "wine_rack 标签"))
			{
				return true;
			}
			if (RecipeUtil.MatchesRepeatable(Config.WineWidth.Value, matId))
			{
				return ApplyColumns(machine, material, moduleCol: true);
			}
			if (RecipeUtil.MatchesRepeatable(Config.WineBottleCol.Value, matId))
			{
				return ApplyColumns(machine, material, moduleCol: false);
			}
			if (RecipeUtil.MatchesRepeatable(Config.WineCircuit.Value, matId))
			{
				return ApplyCircuit(machine, material);
			}
			if (RecipeUtil.MatchesRepeatable(Config.WineWire.Value, matId))
			{
				return ApplyWire(machine, material);
			}
			return false;
		}

		internal static int ModuleBayStage(GameItem machine)
		{
			return TagUtil.GetInt(machine, "MODULE_UPGRADE_STAGE_INT");
		}

		internal static bool ModuleBayExpanded(GameItem machine)
		{
			return ModuleBayStage(machine) >= 2;
		}

		private static bool AcceptsColumns(GameItem machine, string matId, string? recipe, string levelTag, int maxCfg, bool requireModuleBay)
		{
			if (!RecipeUtil.MatchesRepeatable(recipe, matId))
			{
				return false;
			}
			if (requireModuleBay && !ModuleBayExpanded(machine))
			{
				return false;
			}
			return TagUtil.GetInt(machine, levelTag) < Math.Max(0, maxCfg);
		}

		private static bool ApplyColumns(GameItem machine, GameItem material, bool moduleCol)
		{
			string raw = (moduleCol ? Config.WineWidth.Value : Config.WineBottleCol.Value);
			int val = (moduleCol ? Config.WineWidthMaxLevels.Value : Config.WineBottleColMaxLevels.Value);
			string tagKey = (moduleCol ? "FMU_WINERACK_WIDTH_LEVEL" : "FMU_WINERACK_BOTTLE_LEVEL");
			string tagKey2 = (moduleCol ? "FMU_WINERACK_MOD_BASE" : "FMU_WINERACK_BOTTLE_BASE");
			string text = (moduleCol ? "模组列" : "酒瓶列");
			int num = Math.Max(0, val);
			int num2 = TagUtil.GetInt(machine, tagKey);
			if (num2 >= num)
			{
				return false;
			}
			if (moduleCol && !ModuleBayExpanded(machine))
			{
				Mod.Log.Msg($"[FMU] wine_rack 模组列：模组仓未扩满（{ModuleBayStage(machine)}/{2}），" + "需先用「模组舱扩建套件」×2；本次不消耗");
				return false;
			}
			if (!RecipeUtil.MatchesRepeatable(raw, TagUtil.SafeId(material)))
			{
				return false;
			}
			GameGridInventory val2 = (moduleCol ? GridFinder.FindModuleGrid(machine) : GridFinder.FindWineGrid(machine));
			if (val2 == null)
			{
				Mod.Log.Warning("[FMU] wine_rack " + text + "：窗口未开或找不到格子，本次不消耗");
				return false;
			}
			int num3 = GridFinder.SafeWidth(val2);
			int num4 = TagUtil.GetInt(machine, tagKey2);
			if (num4 <= 0)
			{
				num4 = ((num3 <= 0) ? 1 : num3);
				TagUtil.SetInt(machine, tagKey2, num4);
			}
			int num5 = num4 + num2 + 1;
			if (!GridResize.EnsureWidth(machine, val2, num5, moduleCol ? "wine-mod" : "wine-bottle"))
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"[FMU] wine_rack {text}：{num3}→{num5} 未生效（本次已试过），不消耗");
				}
				return false;
			}
			TagUtil.SetInt(machine, tagKey, num2 + 1);
			if (material != null)
			{
				TagUtil.ConsumeOne(material);
			}
			Mod.Log.Msg($"[FMU] wine_rack {text} → {num5} 列 ({num2 + 1}/{num})");
			return true;
		}

		private static bool ApplyCircuit(GameItem machine, GameItem material)
		{
			TagUtil.AddInt(machine, "FMU_WINERACK_CIRCUIT", 1);
			BonusLedger.Grant(machine, "FMU_WINERACK_CIRCUIT", 1);
			TagUtil.ConsumeOne(material);
			int value = TagUtil.GetInt(machine, "FMU_WINERACK_CIRCUIT");
			Mod.Log.Msg($"[FMU] wine_rack 控制电路 #{value}：质量+15% 能耗-5%");
			return true;
		}

		private static bool ApplyWire(GameItem machine, GameItem material)
		{
			int num = TagUtil.GetInt(machine, "FMU_WINERACK_CIRCUIT") / 2;
			int num2 = TagUtil.GetInt(machine, "FMU_WINERACK_WIRE");
			if (num < 1 || num2 >= num)
			{
				return false;
			}
			TagUtil.AddInt(machine, "FMU_WINERACK_WIRE", 1);
			BonusLedger.Grant(machine, "FMU_WINERACK_WIRE", 1);
			TagUtil.ConsumeOne(material);
			Mod.Log.Msg($"[FMU] wine_rack 供电线路 {num2 + 1}/{num}：能耗+10%");
			return true;
		}

		internal static void TryRestore(GameItem machine)
		{
			RestoreColumns(machine, moduleCol: true);
			RestoreColumns(machine, moduleCol: false);
		}

		private static void RestoreColumns(GameItem machine, bool moduleCol)
		{
			string tagKey = (moduleCol ? "FMU_WINERACK_WIDTH_LEVEL" : "FMU_WINERACK_BOTTLE_LEVEL");
			string tagKey2 = (moduleCol ? "FMU_WINERACK_MOD_BASE" : "FMU_WINERACK_BOTTLE_BASE");
			int num = TagUtil.GetInt(machine, tagKey);
			if (num <= 0)
			{
				return;
			}
			GameGridInventory val = (moduleCol ? GridFinder.FindModuleGrid(machine) : GridFinder.FindWineGrid(machine));
			if (val != null)
			{
				int num2 = TagUtil.GetInt(machine, tagKey2);
				if (num2 <= 0)
				{
					int num3 = GridFinder.SafeWidth(val);
					num2 = ((num3 - num > 0) ? (num3 - num) : 6);
					TagUtil.SetInt(machine, tagKey2, num2);
				}
				GridResize.EnsureWidth(machine, val, num2 + num, moduleCol ? "wine-mod" : "wine-bottle");
			}
		}

		internal static void AppendTooltip(RichTextBuilder b, GameItem machine)
		{
			TooltipUi.BeginSection(b);
			BonusLedger.AppendTooltip(b, machine);
			int max = Math.Max(0, Config.WineWidthMaxLevels.Value);
			int max2 = Math.Max(0, Config.WineBottleColMaxLevels.Value);
			int cur = TagUtil.GetInt(machine, "FMU_WINERACK_WIDTH_LEVEL");
			int cur2 = TagUtil.GetInt(machine, "FMU_WINERACK_BOTTLE_LEVEL");
			int num = TagUtil.GetInt(machine, "FMU_WINERACK_CIRCUIT");
			int cur3 = TagUtil.GetInt(machine, "FMU_WINERACK_WIRE");
			int max3 = Math.Max(0, num / 2);
			TooltipUi.Meter(b, "模组列", cur, max, TooltipUi.RecipeMatsHint(Config.WineWidth.Value), "宽度 +1 列");
			if (!ModuleBayExpanded(machine))
			{
				TooltipUi.Hint(b, $"模组列已锁：需先用「模组舱扩建套件」把模组仓扩满（{ModuleBayStage(machine)}/{2}）");
			}
			TooltipUi.Meter(b, "酒瓶列", cur2, max2, TooltipUi.RecipeMatsHint(Config.WineBottleCol.Value), "宽度 +1 列");
			TooltipUi.Meter(b, "控制电路", num, int.MaxValue, TooltipUi.RecipeMatsHint(Config.WineCircuit.Value), BonusLedger.Describe("FMU_WINERACK_CIRCUIT"));
			TooltipUi.Meter(b, "供电线", cur3, max3, TooltipUi.RecipeMatsHint(Config.WineWire.Value), BonusLedger.Describe("FMU_WINERACK_WIRE") + "（每 2 块控制电路解锁 1 根）");
			UniqueUnlock.AppendTooltip(b, machine, "FMU_WINE_LABEL_UNLOCK", "陈酿挂签（麻醉品/水）", Config.WineLabelUnlock.Value, Config.WineLabelChance.Value, "陈酿时挂上 麻醉品/水 展示类型");
		}
	}
}
