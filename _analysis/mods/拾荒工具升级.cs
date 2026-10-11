using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using Microsoft.CodeAnalysis;
using ScavToolUpgrade;
using UnityEngine;
using UnityEngine.EventSystems;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Mod), "ScavToolUpgrade", "1.1.3", "local", null)]
[assembly: MelonGame(null, "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("ScavToolUpgrade")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.1.3.0")]
[assembly: AssemblyInformationalVersion("1.1.3")]
[assembly: AssemblyProduct("ScavToolUpgrade")]
[assembly: AssemblyTitle("ScavToolUpgrade")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.1.3.0")]
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
namespace ScavToolUpgrade
{
	public sealed class Mod : MelonMod
	{
		internal static Instance Log;

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			UpgradeDragPatch.Apply(((MelonBase)this).HarmonyInstance);
			ScavLootPatch.Apply(((MelonBase)this).HarmonyInstance);
			WoundPatch.Apply(((MelonBase)this).HarmonyInstance);
			TooltipPatch.Apply(((MelonBase)this).HarmonyInstance);
			Log.Msg("ScavToolUpgrade 1.1.3 ready (拾荒结算改挂 ScavengeDumpingGrounds，与面板 Get* 同源)");
		}
	}
	internal static class ScavLootPatch
	{
		[ThreadStatic]
		private static bool _suppressVanillaLootTools;

		internal static void Apply(Harmony harmony)
		{
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Expected Obj, but got Unknown
			//IL_0064: Expected Obj, but got Unknown
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Expected Obj, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(typeof(ScavHelper), "GetRandomScavengedItem", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Mod.Log.Warning("ScavLootPatch: GetRandomScavengedItem not found");
				return;
			}
			harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(ScavLootPatch), "PrefixLoot", (Type[])null), new HarmonyMethod(typeof(ScavLootPatch), "PostfixLoot", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			MethodInfo methodInfo2 = AccessTools.Method(typeof(EmporiumEntry), "IsAfterhourHaveOwnedItems", new Type[1] { typeof(string) }, (Type[])null);
			if (methodInfo2 != null)
			{
				harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(typeof(ScavLootPatch), "PrefixOwned", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("ScavLootPatch: replaced vanilla fixed-10 tool rolls with tag chance");
			}
			else
			{
				Mod.Log.Warning("ScavLootPatch: IsAfterhourHaveOwnedItems not found");
			}
		}

		public static void PrefixLoot()
		{
			_suppressVanillaLootTools = true;
		}

		public static void PrefixOwned(string identifier, ref bool __result, ref bool __runOriginal)
		{
			if (_suppressVanillaLootTools && ToolDefs.IsLootBonusTool(identifier))
			{
				__runOriginal = false;
				__result = false;
			}
		}

		public static void PostfixLoot(List<GameItem> __result)
		{
			_suppressVanillaLootTools = false;
			try
			{
				TryBonus("screwdriver", "nuts_metal", __result);
				TryBonus("wire_cutter", "wire", __result);
				TryBonus("welder", "scrap_metal", __result);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("ScavLootPatch Postfix: " + ex.Message);
			}
		}

		private static void TryBonus(string toolId, string matId, List<GameItem>? list)
		{
			if (list == null)
			{
				return;
			}
			EmporiumEntry instance = EmporiumEntry.Instance;
			if ((Object)(object)instance == (Object)null)
			{
				return;
			}
			GameItem val = null;
			try
			{
				val = instance.GetAfterhourItemById(toolId);
			}
			catch
			{
			}
			if (val == null || !ToolDefs.TryGetByTool(toolId, out ToolDefs.ToolSpec spec))
			{
				return;
			}
			int value = ToolDefs.GetValue(val, spec);
			if (value <= 0 || !RNG.Roll(value))
			{
				return;
			}
			GameItem val2 = null;
			try
			{
				val2 = ItemSpawner.Spawn(matId);
			}
			catch
			{
			}
			if (val2 == null)
			{
				try
				{
					val2 = DirectoryMaster.Item(matId, true);
				}
				catch
				{
				}
			}
			if (val2 != null)
			{
				try
				{
					GeneralHelper.SetItemOwned(val2, true);
				}
				catch
				{
				}
				list.Add(val2);
			}
		}
	}
	internal static class TagUtil
	{
		internal static bool TryGetEnabledInt(GameItem item, string tagKey, out int value)
		{
			value = 0;
			try
			{
				TagState tagReadonly = item.GetTagReadonly(tagKey);
				if (tagReadonly == null || !tagReadonly.valueEnabled)
				{
					return false;
				}
				value = tagReadonly.valueInt;
				return true;
			}
			catch
			{
				return false;
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
	internal static class ToolDefs
	{
		internal enum Kind
		{
			FlashlightSafety,
			LootChance
		}

		internal sealed class ToolSpec
		{
			public string ToolId { get; }

			public string FuelId { get; }

			public Kind UpgradeKind { get; }

			public string ToolNameCn { get; }

			public string FuelNameCn { get; }

			public string Tag
			{
				get
				{
					if (UpgradeKind != Kind.FlashlightSafety)
					{
						return "SCAV_TOOL_LOOT_CHANCE_INT";
					}
					return "SCAV_FLASHLIGHT_SAFETY_INT";
				}
			}

			public int DefaultValue
			{
				get
				{
					if (UpgradeKind != Kind.FlashlightSafety)
					{
						return 10;
					}
					return 50;
				}
			}

			public ToolSpec(string toolId, string fuelId, Kind kind, string toolCn, string fuelCn)
			{
				ToolId = toolId;
				FuelId = fuelId;
				UpgradeKind = kind;
				ToolNameCn = toolCn;
				FuelNameCn = fuelCn;
			}
		}

		internal const int Step = 5;

		internal const int MaxChance = 100;

		internal const int DefaultLootChance = 10;

		internal const int DefaultFlashlightSafety = 50;

		internal const string FlashlightTag = "SCAV_FLASHLIGHT_SAFETY_INT";

		internal const string LootChanceTag = "SCAV_TOOL_LOOT_CHANCE_INT";

		internal const string Flashlight = "flashlight";

		internal const string Screwdriver = "screwdriver";

		internal const string WireCutter = "wire_cutter";

		internal const string Welder = "welder";

		internal const string Junk = "junk";

		internal const string NutsMetal = "nuts_metal";

		internal const string Wire = "wire";

		internal const string ScrapMetal = "scrap_metal";

		private static readonly Dictionary<string, ToolSpec> ByTool = new Dictionary<string, ToolSpec>(StringComparer.Ordinal)
		{
			["flashlight"] = new ToolSpec("flashlight", "junk", Kind.FlashlightSafety, "手电筒", "垃圾"),
			["screwdriver"] = new ToolSpec("screwdriver", "nuts_metal", Kind.LootChance, "螺丝刀", "金属螺丝"),
			["wire_cutter"] = new ToolSpec("wire_cutter", "wire", Kind.LootChance, "剪线钳", "线缆"),
			["welder"] = new ToolSpec("welder", "scrap_metal", Kind.LootChance, "等离子切割机", "废金属")
		};

		private static readonly Dictionary<string, string> FuelToTool = new Dictionary<string, string>(StringComparer.Ordinal)
		{
			["junk"] = "flashlight",
			["nuts_metal"] = "screwdriver",
			["wire"] = "wire_cutter",
			["scrap_metal"] = "welder"
		};

		internal static bool TryGetByTool(string? toolId, out ToolSpec spec)
		{
			spec = null;
			if (string.IsNullOrEmpty(toolId))
			{
				return false;
			}
			return ByTool.TryGetValue(toolId, out spec);
		}

		internal static bool TryGetByFuel(string? fuelId, out ToolSpec spec)
		{
			spec = null;
			if (string.IsNullOrEmpty(fuelId) || !FuelToTool.TryGetValue(fuelId, out string value))
			{
				return false;
			}
			return ByTool.TryGetValue(value, out spec);
		}

		internal static bool IsLootBonusTool(string? id)
		{
			if (!string.Equals(id, "screwdriver", StringComparison.Ordinal) && !string.Equals(id, "wire_cutter", StringComparison.Ordinal))
			{
				return string.Equals(id, "welder", StringComparison.Ordinal);
			}
			return true;
		}

		internal static int GetValue(GameItem tool, ToolSpec spec)
		{
			return EnsureInitialized(tool, spec);
		}

		internal static int EnsureInitialized(GameItem tool, ToolSpec spec)
		{
			if (TagUtil.TryGetEnabledInt(tool, spec.Tag, out var value))
			{
				if (spec.UpgradeKind == Kind.FlashlightSafety && value == 0)
				{
					TagUtil.SetInt(tool, spec.Tag, 50);
					Mod.Log.Msg($"ScavToolUpgrade: migrate {spec.ToolNameCn} 0→{50}");
					return 50;
				}
				if (value < 0)
				{
					return 0;
				}
				if (value > 100)
				{
					return 100;
				}
				return value;
			}
			int defaultValue = spec.DefaultValue;
			TagUtil.SetInt(tool, spec.Tag, defaultValue);
			Mod.Log.Msg($"ScavToolUpgrade: init {spec.ToolNameCn} {spec.Tag}={defaultValue}");
			return defaultValue;
		}
	}
	internal static class TooltipPatch
	{
		internal static void Apply(Harmony harmony)
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Expected Obj, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(typeof(ScavHelper), "CreateTooltip", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Mod.Log.Warning("TooltipPatch: ScavHelper.CreateTooltip not found");
				return;
			}
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(TooltipPatch), "Postfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			Mod.Log.Msg("ScavToolUpgrade: tooltip patched");
		}

		public static void Postfix(RichTextBuilder builder, GameItem item)
		{
			try
			{
				if (builder != null && item != null && ToolDefs.TryGetByTool(TagUtil.SafeId(item), out ToolDefs.ToolSpec spec))
				{
					int value = ToolDefs.GetValue(item, spec);
					int num = 100;
					string text;
					if (spec.UpgradeKind != ToolDefs.Kind.FlashlightSafety)
					{
						text = ((value >= num) ? $"拾荒额外产物: 已满级 {value}%" : $"拾荒额外产物: {value}/{num}%（拖入{spec.FuelNameCn} +{5}%）");
					}
					else
					{
						text = ((value >= num) ? $"拾荒有{value}%概率不受伤（已满级）" : $"拾荒有{value}%概率不受伤（拖入{spec.FuelNameCn} +{5}%）");
					}
					builder.AddLine(text, true, (ColorPalette)8877056, false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("TooltipPatch: " + ex.Message);
			}
		}
	}
	internal static class UpgradeDragPatch
	{
		private static GameItem? _dragSource;

		private static MethodInfo? _getCurrentItem;

		private static PropertyInfo? _lastItemProp;

		private static FieldInfo? _lastItemField;

		private static bool _resolved;

		internal static void Apply(Harmony harmony)
		{
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Expected Obj, but got Unknown
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Expected Obj, but got Unknown
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_0126: Expected Obj, but got Unknown
			MethodInfo method = typeof(ItemMouseDragHandler).GetMethod("EndDrag", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				harmony.Patch((MethodBase)method, new HarmonyMethod(typeof(UpgradeDragPatch), "PrefixEndDrag", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg($"ScavToolUpgrade: patched {method}");
			}
			else
			{
				Mod.Log.Warning("ScavToolUpgrade: EndDrag not found");
			}
			MethodInfo methodInfo = AccessTools.Method(typeof(TreeNodeRender), "OnBeginDrag", new Type[1] { typeof(PointerEventData) }, (Type[])null);
			if (methodInfo != null)
			{
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(UpgradeDragPatch), "PostfixBeginDrag", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			MethodInfo methodInfo2 = AccessTools.Method(typeof(TreeNodeRender), "OnDrop", new Type[1] { typeof(PointerEventData) }, (Type[])null);
			if (methodInfo2 != null)
			{
				harmony.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(typeof(UpgradeDragPatch), "PostfixDrop", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}

		public static void PrefixEndDrag(ItemMouseDragHandler __instance)
		{
			try
			{
				Resolve();
				object? obj = _getCurrentItem?.Invoke(__instance, null);
				GameItem val = (GameItem)((obj is GameItem) ? obj : null);
				if (val != null && ToolDefs.TryGetByFuel(TagUtil.SafeId(val), out ToolDefs.ToolSpec _))
				{
					GameItem val2 = ReadLastItem(__instance);
					if (val2 != null)
					{
						TryUpgrade(val, val2);
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("UpgradeDrag EndDrag: " + ex.Message);
			}
		}

		public static void PostfixBeginDrag(TreeNodeRender __instance)
		{
			try
			{
				_dragSource = ResolveItem(__instance);
			}
			catch
			{
				_dragSource = null;
			}
		}

		public static void PostfixDrop(TreeNodeRender __instance)
		{
			try
			{
				GameItem dragSource = _dragSource;
				GameItem val = ResolveItem(__instance);
				_dragSource = null;
				if (dragSource != null && val != null && ToolDefs.TryGetByFuel(TagUtil.SafeId(dragSource), out ToolDefs.ToolSpec _))
				{
					TryUpgrade(dragSource, val);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("UpgradeDrag Drop: " + ex.Message);
			}
		}

		private static void TryUpgrade(GameItem fuel, GameItem tool)
		{
			if (ToolDefs.TryGetByFuel(TagUtil.SafeId(fuel), out ToolDefs.ToolSpec spec) && string.Equals(TagUtil.SafeId(tool), spec.ToolId, StringComparison.Ordinal))
			{
				int value = ToolDefs.GetValue(tool, spec);
				if (value >= 100)
				{
					Mod.Log.Msg($"ScavToolUpgrade: '{spec.ToolId}' 已满 {100}");
					return;
				}
				int value2 = Math.Min(100, value + 5);
				TagUtil.SetInt(tool, spec.Tag, value2);
				TagUtil.ConsumeOne(fuel);
				Mod.Log.Msg($"ScavToolUpgrade: {spec.ToolNameCn} {value}→{value2}（耗 1 {spec.FuelNameCn}）");
			}
		}

		private static void Resolve()
		{
			if (!_resolved)
			{
				_resolved = true;
				Type typeFromHandle = typeof(ItemMouseDragHandler);
				_getCurrentItem = typeFromHandle.GetMethod("GetCurrentItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				_lastItemProp = typeFromHandle.GetProperty("lastItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				_lastItemField = typeFromHandle.GetField("lastItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) ?? typeFromHandle.GetField("<lastItem>k__BackingField", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			}
		}

		private static GameItem? ReadLastItem(ItemMouseDragHandler handler)
		{
			try
			{
				if (_lastItemProp != null)
				{
					object? value = _lastItemProp.GetValue(handler);
					return (GameItem?)((value is GameItem) ? value : null);
				}
				if (_lastItemField != null)
				{
					object? value2 = _lastItemField.GetValue(handler);
					return (GameItem?)((value2 is GameItem) ? value2 : null);
				}
			}
			catch
			{
			}
			return null;
		}

		private static GameItem? ResolveItem(TreeNodeRender node)
		{
			try
			{
				PixelElement val = ((node != null) ? node.element : null);
				if (val == null)
				{
					return null;
				}
				return ((Il2CppObjectBase)val).TryCast<GameItem>();
			}
			catch
			{
				return null;
			}
		}
	}
	internal static class WoundPatch
	{
		[ThreadStatic]
		private static bool _suppressVanillaFlashlight;

		[ThreadStatic]
		private static bool _blockVanillaScavReceive;

		internal static void Apply(Harmony harmony)
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected Obj, but got Unknown
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Expected Obj, but got Unknown
			//IL_00cf: Expected Obj, but got Unknown
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Expected Obj, but got Unknown
			//IL_010d: Expected Obj, but got Unknown
			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
			//IL_0155: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Expected Obj, but got Unknown
			//IL_0162: Expected Obj, but got Unknown
			//IL_01d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01de: Expected Obj, but got Unknown
			//IL_020d: Unknown result type (might be due to invalid IL or missing references)
			//IL_021b: Expected Obj, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(typeof(EmporiumEntry), "IsAfterhourHaveOwnedItems", new Type[1] { typeof(string) }, (Type[])null);
			if (methodInfo != null)
			{
				harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(WoundPatch), "PrefixOwned", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			else
			{
				Mod.Log.Warning("WoundPatch: IsAfterhourHaveOwnedItems not found");
			}
			MethodInfo methodInfo2 = AccessTools.Method(typeof(ScavHelper), "GetMinorWoundChance", (Type[])null, (Type[])null);
			MethodInfo methodInfo3 = AccessTools.Method(typeof(ScavHelper), "GetMajorWoundChance", (Type[])null, (Type[])null);
			if (methodInfo2 != null)
			{
				harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(typeof(WoundPatch), "PrefixGetChance", (Type[])null), new HarmonyMethod(typeof(WoundPatch), "PostfixGetChance", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			if (methodInfo3 != null)
			{
				harmony.Patch((MethodBase)methodInfo3, new HarmonyMethod(typeof(WoundPatch), "PrefixGetChance", (Type[])null), new HarmonyMethod(typeof(WoundPatch), "PostfixGetChance", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			MethodInfo methodInfo4 = AccessTools.Method(typeof(ScavHelper), "ScavengeDumpingGrounds", (Type[])null, (Type[])null);
			if (methodInfo4 != null)
			{
				harmony.Patch((MethodBase)methodInfo4, new HarmonyMethod(typeof(WoundPatch), "PrefixScavenge", (Type[])null), new HarmonyMethod(typeof(WoundPatch), "PostfixScavenge", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("WoundPatch: ScavengeDumpingGrounds 已挂钩（结算用 Get*，拦原版 Receive）");
			}
			else
			{
				Mod.Log.Warning("WoundPatch: ScavengeDumpingGrounds not found");
			}
			MethodInfo methodInfo5 = AccessTools.Method(typeof(HealthData), "ReceiveMinorWound", (Type[])null, (Type[])null);
			MethodInfo methodInfo6 = AccessTools.Method(typeof(HealthData), "ReceiveMajorWound", (Type[])null, (Type[])null);
			if (methodInfo5 != null)
			{
				harmony.Patch((MethodBase)methodInfo5, new HarmonyMethod(typeof(WoundPatch), "PrefixReceiveMinor", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			else
			{
				Mod.Log.Warning("WoundPatch: HealthData.ReceiveMinorWound not found");
			}
			if (methodInfo6 != null)
			{
				harmony.Patch((MethodBase)methodInfo6, new HarmonyMethod(typeof(WoundPatch), "PrefixReceiveMajor", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
			else
			{
				Mod.Log.Warning("WoundPatch: HealthData.ReceiveMajorWound not found");
			}
			Mod.Log.Msg($"ScavToolUpgrade: wound UI=Get* + scav结算=ScavengeDumpingGrounds（N% 不受伤, default {50})");
		}

		public static void PrefixScavenge()
		{
			_blockVanillaScavReceive = true;
		}

		public static void PostfixScavenge()
		{
			_blockVanillaScavReceive = false;
			try
			{
				ApplyOurScavWounds();
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("WoundPatch PostfixScavenge: " + ex.Message);
			}
		}

		public static void PrefixReceiveMinor(ref bool __runOriginal)
		{
			if (_blockVanillaScavReceive)
			{
				__runOriginal = false;
			}
		}

		public static void PrefixReceiveMajor(ref bool __runOriginal)
		{
			if (_blockVanillaScavReceive)
			{
				__runOriginal = false;
			}
		}

		private static void ApplyOurScavWounds()
		{
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return;
			}
			int num = 0;
			int num2 = 0;
			try
			{
				num = instance.dumpingGroundMinorWoundImmunitySearchesLeft;
			}
			catch
			{
			}
			try
			{
				num2 = instance.dumpingGroundMajorWoundImmunitySearchesLeft;
			}
			catch
			{
			}
			if (num <= 0)
			{
				float minorWoundChance = ScavHelper.GetMinorWoundChance();
				float num3 = Random.Range(0f, 100f);
				bool flag = minorWoundChance > num3;
				Mod.Log.Msg($"WoundScav minor: chance={minorWoundChance:F4} roll={num3:F4} injure={flag}");
				if (flag)
				{
					HealthData.ReceiveMinorWound();
				}
			}
			else
			{
				Mod.Log.Msg($"WoundScav minor: skipped (immunity={num})");
			}
			if (num2 <= 0)
			{
				float majorWoundChance = ScavHelper.GetMajorWoundChance();
				float num4 = Random.Range(0f, 100f);
				bool flag2 = majorWoundChance > num4;
				Mod.Log.Msg($"WoundScav major: chance={majorWoundChance:F4} roll={num4:F4} injure={flag2}");
				if (flag2)
				{
					HealthData.ReceiveMajorWound();
				}
			}
			else
			{
				Mod.Log.Msg($"WoundScav major: skipped (immunity={num2})");
			}
		}

		public static void PrefixOwned(string identifier, ref bool __result, ref bool __runOriginal)
		{
			if (_suppressVanillaFlashlight && string.Equals(identifier, "flashlight", StringComparison.Ordinal))
			{
				__runOriginal = false;
				__result = false;
			}
		}

		public static void PrefixGetChance()
		{
			_suppressVanillaFlashlight = true;
		}

		public static void PostfixGetChance(ref float __result)
		{
			try
			{
				ApplyFlashlightFactor(ref __result);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("WoundPatch GetChance: " + ex.Message);
			}
			finally
			{
				_suppressVanillaFlashlight = false;
			}
		}

		private static void ApplyFlashlightFactor(ref float chance)
		{
			EmporiumEntry instance = EmporiumEntry.Instance;
			if ((Object)(object)instance == (Object)null)
			{
				return;
			}
			GameItem val = null;
			try
			{
				val = instance.GetAfterhourItemById("flashlight");
			}
			catch
			{
			}
			if (val == null || !ToolDefs.TryGetByTool("flashlight", out ToolDefs.ToolSpec spec))
			{
				return;
			}
			int num = ToolDefs.GetValue(val, spec);
			if (num > 0)
			{
				if (num > 100)
				{
					num = 100;
				}
				chance *= (float)(100 - num) / 100f;
			}
		}
	}
}
