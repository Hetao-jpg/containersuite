using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MoreDeviceUpgrades;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: MelonInfo(typeof(global::MoreDeviceUpgrades.MoreDeviceUpgrades), "More Device Upgrades", "0.2.0", "mod-dev", "")]
[assembly: MelonGame(null, null)]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = "")]
[assembly: AssemblyCompany("MoreDeviceUpgrades")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("MoreDeviceUpgrades")]
[assembly: AssemblyTitle("MoreDeviceUpgrades")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.0.0")]
[module: UnverifiableCode]
namespace MoreDeviceUpgrades;

public static class DragHighlight
{
	internal static bool InEndDrag;

	internal static bool InDragPreview;

	private static readonly HashSet<string> Patched = new HashSet<string>();

	public static void Patch(Harmony harmony)
	{
		//IL_021c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0237: Unknown result type (might be due to invalid IL or missing references)
		//IL_0244: Expected Obj, but got Unknown
		//IL_0244: Expected Obj, but got Unknown
		//IL_0288: Unknown result type (might be due to invalid IL or missing references)
		//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
		//IL_02b0: Expected Obj, but got Unknown
		//IL_02b0: Expected Obj, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Expected Obj, but got Unknown
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0157: Expected Obj, but got Unknown
		try
		{
			Type type = FindTypeFull("Il2Cpp.GameItem");
			if (type == null)
			{
				type = FindType("GameItem");
			}
			if (type != null)
			{
				int num = 0;
				MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (MethodInfo methodInfo in methods)
				{
					if (methodInfo.IsAbstract || methodInfo.IsConstructor)
					{
						continue;
					}
					string item = "GameItem." + methodInfo.Name;
					if (!Patched.Contains(item))
					{
						if (methodInfo.Name == "MayHaveValidInventorySlot" && methodInfo.GetParameters().Length == 1)
						{
							MethodInfo method = typeof(DragHighlight).GetMethod("MayHavePrefix", BindingFlags.Static | BindingFlags.NonPublic);
							harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
							Patched.Add(item);
							num++;
						}
						else if (methodInfo.Name == "TryFindOneValidInventorySlot" && methodInfo.GetParameters().Length == 1)
						{
							MethodInfo method2 = typeof(DragHighlight).GetMethod("TryFindPrefix", BindingFlags.Static | BindingFlags.NonPublic);
							harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
							Patched.Add(item);
							num++;
						}
					}
				}
				ModLog.Info($"[DragHl] GameItem patched {num} method(s)");
			}
			else
			{
				ModLog.Warning("[DragHl] GameItem type not found; highlight disabled");
			}
			Type type2 = FindType("ItemMouseDragHandler");
			if (type2 != null)
			{
				MethodInfo methodInfo2 = AccessTools.Method(type2, "EndDrag", (Type[])null, (Type[])null);
				if (methodInfo2 != null)
				{
					harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(typeof(DragHighlight).GetMethod("EndDragPhasePrefix", BindingFlags.Static | BindingFlags.NonPublic)), new HarmonyMethod(typeof(DragHighlight).GetMethod("EndDragPhasePostfix", BindingFlags.Static | BindingFlags.NonPublic)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					ModLog.Info("[DragHl] EndDrag phase hook added");
				}
				MethodInfo methodInfo3 = AccessTools.Method(type2, "Drag", (Type[])null, (Type[])null);
				if (methodInfo3 != null)
				{
					harmony.Patch((MethodBase)methodInfo3, new HarmonyMethod(typeof(DragHighlight).GetMethod("DragPhasePrefix", BindingFlags.Static | BindingFlags.NonPublic)), new HarmonyMethod(typeof(DragHighlight).GetMethod("DragPhasePostfix", BindingFlags.Static | BindingFlags.NonPublic)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					ModLog.Info("[DragHl] Drag phase hook added");
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning("[DragHl] patch err " + ex.Message);
		}
	}

	private static bool EndDragPhasePrefix()
	{
		InEndDrag = true;
		return true;
	}

	private static void EndDragPhasePostfix()
	{
		InEndDrag = false;
		InDragPreview = false;
	}

	private static bool DragPhasePrefix()
	{
		InDragPreview = true;
		return true;
	}

	private static void DragPhasePostfix()
	{
		InDragPreview = false;
	}

	private static bool MayHavePrefix(GameItem __instance, GameItem item, ref bool __result)
	{
		if (!InDragPreview || InEndDrag)
		{
			return true;
		}
		if (MachineUpgrade.IsUpgradeablePair(__instance, item))
		{
			__result = true;
			return false;
		}
		return true;
	}

	private static bool TryFindPrefix(GameItem __instance, GameItem item, ref SlotMarker __result)
	{
		if (!InDragPreview || InEndDrag)
		{
			return true;
		}
		if (MachineUpgrade.IsUpgradeablePair(__instance, item))
		{
			SlotMarker incompleteSlot = GetIncompleteSlot(__instance);
			if (incompleteSlot != null)
			{
				MarkValid(incompleteSlot);
				__result = incompleteSlot;
				return false;
			}
		}
		return true;
	}

	private static SlotMarker GetIncompleteSlot(GameItem machine)
	{
		try
		{
			MethodInfo[] methods = ((object)machine).GetType().GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name.IndexOf("GetIncompleteSlotInInventory", StringComparison.Ordinal) >= 0 && methodInfo.GetParameters().Length == 0)
				{
					object? obj = methodInfo.Invoke(machine, null);
					return (SlotMarker)((obj is SlotMarker) ? obj : null);
				}
			}
			object? obj2 = AccessTools.Method(((object)machine).GetType(), "GetIncompleteSlotInInventory", Type.EmptyTypes, (Type[])null)?.Invoke(machine, null);
			return (SlotMarker)((obj2 is SlotMarker) ? obj2 : null);
		}
		catch (Exception ex)
		{
			ModLog.Info("[DragHl] GetIncompleteSlot err " + ex.Message);
			return null;
		}
	}

	private static void MarkValid(SlotMarker slot)
	{
		try
		{
			FieldInfo field = ((object)slot).GetType().GetField("numTransfer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field == null)
			{
				((object)slot).GetType().GetProperty("numTransfer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.SetValue(slot, 1, null);
			}
			else if (field.FieldType == typeof(int))
			{
				field.SetValue(slot, 1);
			}
			else
			{
				field.SetValue(slot, Convert.ChangeType(1, field.FieldType));
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[DragHl] MarkValid err " + ex.Message);
		}
	}

	private static Type FindType(string name)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetName().Name != "Assembly-CSharp")
			{
				continue;
			}
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.Name == name || type.FullName == name)
					{
						return type;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static Type FindTypeFull(string fullName)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetName().Name != "Assembly-CSharp")
			{
				continue;
			}
			try
			{
				Type type = assembly.GetType(fullName, throwOnError: false);
				if (type != null)
				{
					return type;
				}
			}
			catch
			{
			}
		}
		return null;
	}
}
public static class DropDiagnostics
{
	private static readonly HashSet<string> SeenMachines = new HashSet<string>();

	private static Harmony _harmony;

	private static readonly string[] MachineMarkers = new string[5] { "MODULE_TYPE_FURNACE", "STANDARD_MACHINE_TAG", "MACHINE", "CONTAINER_TAG", "manufacture" };

	private static bool _apiLogged;

	private static readonly HashSet<string> _patched = new HashSet<string>();

	public static void Init(Harmony harmony)
	{
		_harmony = harmony;
		DoPatch();
	}

	private static void DoPatch()
	{
		TryPatchPostInst("ItemMouseDragHandler", "EndDrag", "EndDragPostfix");
		TryPatch("MachineHelper", "OnMachineActioned", "OnMachineActionedLog");
		AutoPatchComputeAggregators();
		TooltipUpgrade.Patch(_harmony);
		DragHighlight.Patch(_harmony);
		MachineUpgrade.PatchRecharger(_harmony);
		MachineUpgrade.PatchRechargerDisplay(_harmony);
	}

	private static void AutoPatchComputeAggregators()
	{
		//IL_0146: Unknown result type (might be due to invalid IL or missing references)
		//IL_0153: Expected Obj, but got Unknown
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetName().Name != "Assembly-CSharp")
			{
				continue;
			}
			Type[] types = assembly.GetTypes();
			foreach (Type type in types)
			{
				MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
				foreach (MethodInfo methodInfo in methods)
				{
					if (methodInfo.Name != "ComputeModuleEffect")
					{
						continue;
					}
					string item = type.Name + "." + methodInfo.Name + "(" + methodInfo.GetParameters().Length + ")#agg";
					if (!_patched.Contains(item))
					{
						MethodInfo methodInfo2 = ((methodInfo.GetParameters().Length == 2) ? typeof(DropDiagnostics).GetMethod("AggPost2", BindingFlags.Static | BindingFlags.NonPublic) : typeof(DropDiagnostics).GetMethod("AggPost3", BindingFlags.Static | BindingFlags.NonPublic));
						if (!(methodInfo2 == null))
						{
							_harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(methodInfo2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
							_patched.Add(item);
							ModLog.Info($"agg-patched {type.Name}.{methodInfo.Name} ({methodInfo.GetParameters().Length})");
						}
					}
				}
			}
		}
	}

	private static void AggPost2(object __0, object __1)
	{
		MachineUpgrade.AddVirtualBonus(__1);
	}

	private static void AggPost3(object __0, object __1, object __2)
	{
		MachineUpgrade.AddVirtualBonus(__2);
	}

	private static void ProbeSig(string type, string method, int paramCount, string preName, string postName)
	{
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_017c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0189: Expected Obj, but got Unknown
		//IL_0189: Expected Obj, but got Unknown
		string item = type + "." + method + $"({paramCount})#sig";
		if (_patched.Contains(item))
		{
			return;
		}
		try
		{
			Type type2 = FindType(type);
			if (type2 == null)
			{
				ModLog.Info("sig-patch deferred: " + type);
				return;
			}
			MethodInfo methodInfo = null;
			MethodInfo[] methods = type2.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodInfo2 in methods)
			{
				if (methodInfo2.Name == method && methodInfo2.GetParameters().Length == paramCount)
				{
					methodInfo = methodInfo2;
					break;
				}
			}
			if (methodInfo == null)
			{
				ModLog.Info($"sig-patch skip: {type}.{method} len{paramCount}");
			}
			else
			{
				MethodInfo method2 = typeof(DropDiagnostics).GetMethod(preName, BindingFlags.Static | BindingFlags.NonPublic);
				MethodInfo method3 = typeof(DropDiagnostics).GetMethod(postName, BindingFlags.Static | BindingFlags.NonPublic);
				_harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method2), new HarmonyMethod(method3), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched.Add(item);
				ModLog.Info($"sig-patched {type}.{method} ({paramCount})");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning($"sig-patch {type}.{method} fail {ex.Message}");
		}
	}

	private static void PApplyP(object a)
	{
		Snap("PRE ApplyModifiedPower", a);
	}

	private static void SApplyP(object a)
	{
		Snap("POST ApplyModifiedPower", a);
	}

	private static void PUpdatePower(object a)
	{
		Snap("PRE UpdatePowerUsage", a);
	}

	private static void SUpdatePower(object a)
	{
		Snap("POST UpdatePowerUsage", a);
	}

	private static void PCompute3(object a, object b, object c)
	{
		Snap("PRE Compute3", c);
	}

	private static void SCompute3(object a, object b, object c)
	{
		Snap("POST Compute3", c);
	}

	private static void PCompute2(object a, object b)
	{
		Snap("PRE Compute2", b);
	}

	private static void SCompute2(object a, object b)
	{
		Snap("POST Compute2", b);
	}

	private static void PApplyBasic(object a, object b, object c)
	{
		Snap("PRE ApplyBasic", c);
	}

	private static void SApplyBasic(object a, object b, object c)
	{
		Snap("POST ApplyBasic", c);
	}

	private static void Snap(string label, object machine)
	{
		try
		{
			string text = GameItemHelper.TryGetItemId(machine);
			if (!(text != "furnace"))
			{
				int? intTag = GameItemHelper.GetIntTag(machine, "MACHINERY_DRAWN_POWER_BASE");
				int? intTag2 = GameItemHelper.GetIntTag(machine, "machinery_drawn_power");
				int? intTag3 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT");
				int? intTag4 = GameItemHelper.GetIntTag(machine, "MOD_FURNACE_INGOT_UPGRADES");
				ModLog.Info($"[SEQ] {label}: base={intTag} draw={intTag2} totalPower={intTag3} upgrades={intTag4}");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning("Snap err " + ex.Message);
		}
	}

	private static void OnMachineActionedLog(object machine, object moduleGrid)
	{
		try
		{
			string text = GameItemHelper.TryGetItemId(machine);
			if (!(text != "furnace") || !(text != "moisture_farm") || !(text != "moisture_farm_w") || !(text != "water_purifier") || !(text != "wine_rack"))
			{
				int? intTag = GameItemHelper.GetIntTag(machine, "machinery_drawn_power");
				int? intTag2 = GameItemHelper.GetIntTag(machine, "MOD_FURNACE_INGOT_UPGRADES");
				int? intTag3 = GameItemHelper.GetIntTag(machine, "MOD_FURNACE_CIRCUIT_UPGRADES");
				int? intTag4 = GameItemHelper.GetIntTag(machine, "MOD_MOISTURE_CIRCUIT_UPGRADES");
				int? intTag5 = GameItemHelper.GetIntTag(machine, "MOD_MOISTURE_WIRE_UPGRADES");
				int? intTag6 = GameItemHelper.GetIntTag(machine, "MOD_PURIFIER_FILTER_PARTS");
				int? intTag7 = GameItemHelper.GetIntTag(machine, "MOD_PURIFIER_INGOT_UPGRADES");
				int? intTag8 = GameItemHelper.GetIntTag(machine, "MOD_WINERACK_CIRCUIT_UPGRADES");
				int? intTag9 = GameItemHelper.GetIntTag(machine, "MOD_WINERACK_WIRE_UPGRADES");
				ModLog.Info($"[Machine] {text} actioned draw={intTag} furnace[ingot={intTag2} circ={intTag3}] moisture[circ={intTag4} wire={intTag5}] purifier[filterParts={intTag6} ingot={intTag7}] winerack[circ={intTag8} wire={intTag9}]");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning("OnMachineActioned err " + ex.Message);
		}
	}

	private static void TryPatchInst(string type, string method, string handlerName, params string[] paramTypeNames)
	{
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_01b2: Expected Obj, but got Unknown
		//IL_01b2: Expected Obj, but got Unknown
		string item = type + "." + method + "()#inst";
		if (_patched.Contains(item))
		{
			return;
		}
		try
		{
			Type type2 = FindType(type);
			if (type2 == null)
			{
				ModLog.Info("inst-patch deferred: type " + type + " not loaded");
				return;
			}
			MethodBase methodBase = null;
			if (paramTypeNames.Length == 0)
			{
				methodBase = AccessTools.Method(type2, method, (Type[])null, (Type[])null);
			}
			else
			{
				Type[] array = new Type[paramTypeNames.Length];
				for (int i = 0; i < paramTypeNames.Length; i++)
				{
					ref Type reference = ref array[i];
					object obj;
					if (paramTypeNames[i] == "int")
					{
						obj = typeof(int);
					}
					else
					{
						obj = ((paramTypeNames[i] == "string") ? typeof(string) : FindType(paramTypeNames[i]));
					}
					reference = (Type)obj;
					if (array[i] == null)
					{
						ModLog.Warning("param type " + paramTypeNames[i] + " unresolved");
						return;
					}
				}
				methodBase = AccessTools.Method(type2, method, array, (Type[])null);
			}
			if (methodBase == null)
			{
				ModLog.Warning($"inst-patch skip: {type}.{method} not found");
			}
			else
			{
				MethodInfo method2 = typeof(DropDiagnostics).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
				_harmony.Patch(methodBase, new HarmonyMethod(method2), new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched.Add(item);
				ModLog.Info("inst-patched " + type + "." + method);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning($"inst-patch {type}.{method} failed: {ex.Message}");
		}
	}

	private static void TryPatchPostInst(string type, string method, string handlerName)
	{
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_00f0: Expected Obj, but got Unknown
		string item = type + "." + method + "()#instpost";
		if (_patched.Contains(item))
		{
			return;
		}
		try
		{
			Type type2 = FindType(type);
			if (type2 == null)
			{
				ModLog.Info("instpost deferred: type " + type + " not loaded");
				return;
			}
			MethodBase methodBase = AccessTools.Method(type2, method, (Type[])null, (Type[])null);
			if (methodBase == null)
			{
				ModLog.Warning($"instpost skip: {type}.{method} not found");
			}
			else
			{
				MethodInfo method2 = typeof(DropDiagnostics).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
				_harmony.Patch(methodBase, (HarmonyMethod)null, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched.Add(item);
				ModLog.Info("instpost-patched " + type + "." + method);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning($"instpost {type}.{method} failed: {ex.Message}");
		}
	}

	private static void EndDragPostfix(object __instance)
	{
		object obj = null;
		object material = null;
		try
		{
			Type type = __instance.GetType();
			MethodInfo method = type.GetMethod("GetCurrentItem", BindingFlags.Instance | BindingFlags.Public);
			if (method != null)
			{
				material = method.Invoke(__instance, null);
			}
			obj = ReadField(__instance, "lastItem");
			MachineUpgrade.HandleDrop(obj, material);
		}
		catch (Exception ex)
		{
			ModLog.Warning("EndDrag probe err " + ex.Message);
		}
	}

	private static void UncheckedAcceptPrefix(object __instance, object item)
	{
		string text = GameItemHelper.TryGetItemId(item);
		string name = __instance.GetType().Name;
		ModLog.Info($"[Accept] {name}.UncheckedAccept item='{text}'");
		if (text != null && text.IndexOf("metal_ingot", StringComparison.OrdinalIgnoreCase) >= 0)
		{
			ModLog.Info("[Accept] !! METAL INGOT accepted into " + name);
		}
	}

	private static object ReadField(object o, string name)
	{
		try
		{
			Type type = o.GetType();
			FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				return field.GetValue(o);
			}
			PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property.GetValue(o, null);
			}
		}
		catch
		{
		}
		return null;
	}

	private static void TryPatchTypedPfx(string type, string method, string handlerName, params string[] paramTypeNames)
	{
		//IL_01fa: Unknown result type (might be due to invalid IL or missing references)
		//IL_0208: Expected Obj, but got Unknown
		string item = type + "." + method + "(" + string.Join(",", paramTypeNames) + ")#pfx";
		if (_patched.Contains(item))
		{
			return;
		}
		try
		{
			Type type2 = FindType(type);
			if (type2 == null)
			{
				ModLog.Info("patch deferred: type " + type + " not loaded");
				return;
			}
			Type[] array = new Type[paramTypeNames.Length];
			for (int i = 0; i < paramTypeNames.Length; i++)
			{
				ref Type reference = ref array[i];
				object obj;
				if (paramTypeNames[i] == "int")
				{
					obj = typeof(int);
				}
				else
				{
					obj = ((paramTypeNames[i] == "string") ? typeof(string) : FindType(paramTypeNames[i]));
				}
				reference = (Type)obj;
				if (array[i] == null)
				{
					ModLog.Warning($"param type {paramTypeNames[i]} unresolved, skip {type}.{method}#pfx");
					return;
				}
			}
			MethodBase methodBase = AccessTools.Method(type2, method, array, (Type[])null);
			if (methodBase == null)
			{
				ModLog.Warning($"patch skip: {type}.{method}#pfx overload not found");
			}
			else
			{
				MethodInfo method2 = typeof(DropDiagnostics).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
				_harmony.Patch(methodBase, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched.Add(item);
				ModLog.Info($"pfx-patched {type}.{method}({string.Join(",", paramTypeNames)})");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning($"pfx-patch {type}.{method} failed: {ex.Message}");
		}
	}

	private static void TryPatchTyped(string type, string method, string handlerName, params string[] paramTypeNames)
	{
		//IL_01ee: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Expected Obj, but got Unknown
		string item = type + "." + method + "(" + string.Join(",", paramTypeNames) + ")";
		if (_patched.Contains(item))
		{
			return;
		}
		try
		{
			Type type2 = FindType(type);
			if (type2 == null)
			{
				ModLog.Info("patch deferred: type " + type + " not loaded");
				return;
			}
			Type[] array = new Type[paramTypeNames.Length];
			for (int i = 0; i < paramTypeNames.Length; i++)
			{
				ref Type reference = ref array[i];
				object obj;
				if (paramTypeNames[i] == "int")
				{
					obj = typeof(int);
				}
				else
				{
					obj = ((paramTypeNames[i] == "string") ? typeof(string) : FindType(paramTypeNames[i]));
				}
				reference = (Type)obj;
				if (array[i] == null)
				{
					ModLog.Warning($"param type {paramTypeNames[i]} unresolved, skip {type}.{method}");
					return;
				}
			}
			MethodBase methodBase = AccessTools.Method(type2, method, array, (Type[])null);
			if (methodBase == null)
			{
				ModLog.Warning($"patch skip: {type}.{method} overload not found");
			}
			else
			{
				MethodInfo method2 = typeof(DropDiagnostics).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
				_harmony.Patch(methodBase, (HarmonyMethod)null, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched.Add(item);
				ModLog.Info($"patched {type}.{method}({string.Join(",", paramTypeNames)})");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning($"patch {type}.{method} failed: {ex.Message}");
		}
	}

	private static void PostfixFeed2(object __0, object __1)
	{
		FeedLog("2arg", __0, __1);
	}

	private static void PostfixFeed3(object __0, object __1)
	{
		FeedLog("3arg", __0, __1);
	}

	private static void PrefixFeed2(object __0, object __1)
	{
		ProbeChain("accept2", __0, __1);
	}

	private static void PrefixFeed3(object __0, object __1)
	{
		ProbeChain("accept3", __0, __1);
	}

	private static void ProbeChain(string kind, object node, object item)
	{
		string value = GameItemHelper.TryGetItemId(item);
		ModLog.Info($"[Diag] {kind}: drop item='{value}'");
		NodeDump(node, kind, 0, 4);
	}

	private static void NodeDump(object node, string kind, int depth, int maxDepth)
	{
		if (node == null || depth >= maxDepth)
		{
			return;
		}
		Type type = node.GetType();
		StringBuilder stringBuilder = new StringBuilder();
		StringBuilder stringBuilder2 = stringBuilder;
		StringBuilder stringBuilder3 = stringBuilder2;
		StringBuilder.AppendInterpolatedStringHandler handler = new StringBuilder.AppendInterpolatedStringHandler(20, 4, stringBuilder2);
		handler.AppendLiteral("[");
		handler.AppendFormatted(kind);
		handler.AppendLiteral("] depth");
		handler.AppendFormatted(depth);
		handler.AppendLiteral(" type=");
		handler.AppendFormatted(type.FullName);
		handler.AppendLiteral(" id='");
		handler.AppendFormatted(GameItemHelper.TryGetItemId(node));
		handler.AppendLiteral("'");
		stringBuilder3.Append(ref handler);
		string[] machineMarkers = MachineMarkers;
		foreach (string text in machineMarkers)
		{
			if (GameItemHelper.HasTag(node, text))
			{
				stringBuilder2 = stringBuilder;
				StringBuilder stringBuilder4 = stringBuilder2;
				handler = new StringBuilder.AppendInterpolatedStringHandler(1, 1, stringBuilder2);
				handler.AppendLiteral(" ");
				handler.AppendFormatted(text);
				stringBuilder4.Append(ref handler);
			}
		}
		ModLog.Info(stringBuilder.ToString());
		if (depth + 1 >= maxDepth)
		{
			return;
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (propertyInfo.GetIndexParameters().Length != 0)
			{
				continue;
			}
			object value;
			try
			{
				value = propertyInfo.GetValue(node, null);
			}
			catch
			{
				continue;
			}
			if (value != null && value != node)
			{
				Type type2 = value.GetType();
				if (!type2.IsPrimitive && !(type2 == typeof(string)) && LooksGraphLike(type2.Name))
				{
					NodeDump(value, kind, depth + 1, maxDepth);
				}
			}
		}
		FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo fieldInfo in fields)
		{
			object value2;
			try
			{
				value2 = fieldInfo.GetValue(node);
			}
			catch
			{
				continue;
			}
			if (value2 != null && value2 != node)
			{
				Type type3 = value2.GetType();
				if (!type3.IsPrimitive && !(type3 == typeof(string)) && LooksGraphLike(type3.Name))
				{
					NodeDump(value2, kind, depth + 1, maxDepth);
				}
			}
		}
	}

	private static bool LooksGraphLike(string name)
	{
		return name.IndexOf("Graph", StringComparison.Ordinal) >= 0 || name.IndexOf("GameItem", StringComparison.Ordinal) >= 0 || name.IndexOf("GameInventory", StringComparison.Ordinal) >= 0 || name.IndexOf("GameSlot", StringComparison.Ordinal) >= 0 || name.IndexOf("GameGrid", StringComparison.Ordinal) >= 0 || name.IndexOf("PixelWindow", StringComparison.Ordinal) >= 0 || name.IndexOf("Machine", StringComparison.Ordinal) >= 0;
	}

	private static void FeedLog(string kind, object node, object item)
	{
		string value = ((node != null) ? GameItemHelper.TryGetItemId(node) : null);
		string value2 = node?.GetType().Name ?? "null";
		string value3 = GameItemHelper.TryGetItemId(item);
		ModLog.Info($"[Diagnostic] feed-{kind}: nodeType='{value2}' nodeId='{value}' item='{value3}'");
	}

	private static void TryPatch(string type, string method, string handlerName)
	{
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00eb: Expected Obj, but got Unknown
		string item = type + "." + method;
		if (_patched.Contains(item))
		{
			return;
		}
		try
		{
			Type type2 = FindType(type);
			if (type2 == null)
			{
				ModLog.Info("patch deferred: type " + type + " not loaded yet");
				return;
			}
			MethodBase methodBase = AccessTools.Method(type2, method, (Type[])null, (Type[])null);
			if (methodBase == null)
			{
				ModLog.Warning($"patch skip: {type}.{method} not found");
			}
			else
			{
				MethodInfo method2 = typeof(DropDiagnostics).GetMethod(handlerName, BindingFlags.Static | BindingFlags.NonPublic);
				_harmony.Patch(methodBase, (HarmonyMethod)null, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				_patched.Add(item);
				ModLog.Info("patched " + type + "." + method);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning($"patch {type}.{method} failed: {ex.Message}");
		}
	}

	private static Type FindType(string name)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetName().Name != "Assembly-CSharp")
			{
				continue;
			}
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.Name == name || type.FullName == name)
					{
						return type;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	private static void PostfixIngot(object __0, object __1)
	{
		LogDrop("ingot", __0, __1);
	}

	private static void PostfixScrap(object __0, object __1)
	{
		LogDrop("scrap", __0, __1);
	}

	private static void PrefixMachine(object __0)
	{
		if (!_apiLogged)
		{
			_apiLogged = true;
			GameItemHelper.LogApis();
			GameItemHelper.LogFeedApis();
			DoPatch();
		}
		string text = GameItemHelper.TryGetItemId(__0);
		if (!string.IsNullOrEmpty(text) && SeenMachines.Add(text))
		{
			ModLog.Info("[Diagnostic] machine actioned: id='" + text + "'");
		}
	}

	private static void PostfixAnyUpgrade(object __0, object __1)
	{
		string value = GameItemHelper.TryGetItemId(__0);
		string value2 = GameItemHelper.TryGetItemId(__1);
		ModLog.Info($"[Diagnostic] toolbox-MayUpgrade source='{value}' target='{value2}'");
	}

	private static void LogDrop(string kind, object src, object dst)
	{
		string text = GameItemHelper.TryGetItemId(src);
		string text2 = GameItemHelper.TryGetItemId(dst);
		ModLog.Info($"[Diagnostic] drop-{kind}: source='{text}' target='{text2}'");
		UpgradeRecipe upgradeRecipe = UpgradeRecipes.Find(text2, text);
		if (upgradeRecipe != null)
		{
			ModLog.Info($"[Diagnostic] MATCH recipe! machine='{upgradeRecipe.Machine}' material='{upgradeRecipe.Material}' stat={upgradeRecipe.Stat} step={upgradeRecipe.StepPerItem} cap={upgradeRecipe.MaxTotal}  (apply logic to be enabled)");
		}
	}
}
public static class GameItemHelper
{
	private struct MEMORY_BASIC_INFORMATION
	{
		public IntPtr BaseAddress;

		public IntPtr AllocationBase;

		public uint AllocationProtect;

		public IntPtr RegionSize;

		public uint State;

		public uint Protect;

		public uint Type;
	}

	private static readonly string[] IdCandidates = new string[10] { "ItemID", "ItemId", "itemID", "Id", "ID", "Identifier", "GameItemId", "GetItemID", "GetItemId", "GetId" };

	private static readonly List<PropertyInfo> IdProps = new List<PropertyInfo>();

	private static readonly List<MethodInfo> IdMethods = new List<MethodInfo>();

	private static bool _scanned;

	private static PropertyInfo _pointerProp;

	public static string TryGetItemId(object item)
	{
		if (item == null)
		{
			return null;
		}
		Scan(item.GetType());
		foreach (PropertyInfo idProp in IdProps)
		{
			try
			{
				if (idProp.PropertyType == typeof(string))
				{
					object value = idProp.GetValue(item, null);
					if (value is string text && !string.IsNullOrEmpty(text))
					{
						return text;
					}
				}
			}
			catch
			{
			}
		}
		foreach (MethodInfo idMethod in IdMethods)
		{
			try
			{
				if (idMethod.ReturnType == typeof(string))
				{
					object obj2 = idMethod.Invoke(item, null);
					if (obj2 is string text2 && !string.IsNullOrEmpty(text2))
					{
						return text2;
					}
				}
			}
			catch
			{
			}
		}
		if (item.GetType().Name.IndexOf("GameItem", StringComparison.Ordinal) >= 0)
		{
			try
			{
				return ReadStringAtOffset(item, 448);
			}
			catch
			{
			}
		}
		return null;
	}

	private static void Scan(Type t)
	{
		if (_scanned)
		{
			return;
		}
		PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (MatchesName(propertyInfo.Name))
			{
				IdProps.Add(propertyInfo);
			}
		}
		MethodInfo[] methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public);
		foreach (MethodInfo methodInfo in methods)
		{
			if (MatchesName(methodInfo.Name))
			{
				IdMethods.Add(methodInfo);
			}
		}
		_scanned = true;
	}

	private static bool MatchesName(string n)
	{
		string[] idCandidates = IdCandidates;
		foreach (string b in idCandidates)
		{
			if (string.Equals(n, b, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return n.IndexOf("itemid", StringComparison.OrdinalIgnoreCase) >= 0 || n.Equals("GetId", StringComparison.OrdinalIgnoreCase);
	}

	private static string ReadStringAtOffset(object item, int offset)
	{
		IntPtr pointer = GetPointer(item);
		if (pointer == IntPtr.Zero)
		{
			return null;
		}
		IntPtr intPtr;
		try
		{
			intPtr = Marshal.ReadIntPtr(pointer, offset);
		}
		catch
		{
			return null;
		}
		if (intPtr == IntPtr.Zero)
		{
			return null;
		}
		string text = null;
		int[] array = new int[6] { 8, 12, 16, 20, 24, 28 };
		foreach (int num in array)
		{
			int[] array2 = new int[4] { 4, 8, 16, 12 };
			foreach (int num2 in array2)
			{
				try
				{
					int num3 = ReadInt32Safe(intPtr, num);
					if (num3 <= 1 || num3 > 4096)
					{
						continue;
					}
					int num4 = num + num2;
					StringBuilder stringBuilder = new StringBuilder(num3);
					for (int k = 0; k < num3 && IsReadable(intPtr + num4 + k * 2, 2); k++)
					{
						char c = (char)Marshal.ReadInt16(intPtr, num4 + k * 2);
						if (c == '\0')
						{
							break;
						}
						stringBuilder.Append(c);
					}
					string text2 = stringBuilder.ToString();
					if (text2.Length >= 2 && IsPlausible(text2) && (text == null || text2.Length > text.Length))
					{
						text = text2;
					}
				}
				catch
				{
				}
			}
		}
		return text;
	}

	private static int ReadInt32Safe(IntPtr p, int offset)
	{
		if (!IsReadable(p + offset, 4))
		{
			throw new InvalidOperationException("unreadable");
		}
		return Marshal.ReadInt32(p, offset);
	}

	private static bool IsReadable(IntPtr addr, int size)
	{
		try
		{
			MEMORY_BASIC_INFORMATION lpBuffer = default;
			IntPtr intPtr = VirtualQuery(addr, ref lpBuffer, (IntPtr)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));
			if (intPtr == IntPtr.Zero)
			{
				return false;
			}
			if (lpBuffer.State != 4096)
			{
				return false;
			}
			if (lpBuffer.Protect == 1 || lpBuffer.Protect == 0)
			{
				return false;
			}
			ulong num = (ulong)((long)lpBuffer.BaseAddress + (long)lpBuffer.RegionSize);
			ulong num2 = (ulong)((long)addr + (uint)size);
			return num2 <= num;
		}
		catch
		{
			return false;
		}
	}

	[DllImport("kernel32.dll")]
	private static extern IntPtr VirtualQuery(IntPtr lpAddress, ref MEMORY_BASIC_INFORMATION lpBuffer, IntPtr dwLength);

	private static bool IsPlausible(string s)
	{
		foreach (char c in s)
		{
			if (c < ' ' || c > '~')
			{
				return false;
			}
		}
		return true;
	}

	public static bool HasTag(object item, string tag)
	{
		if (item == null)
		{
			return false;
		}
		try
		{
			MethodInfo method = item.GetType().GetMethod("IsTag", BindingFlags.Instance | BindingFlags.Public, null, new Type[1] { typeof(string) }, null);
			if (method != null && method.ReturnType == typeof(bool))
			{
				return (bool)method.Invoke(item, new object[1] { tag });
			}
		}
		catch
		{
		}
		return false;
	}

	public static int? GetIntTag(object item, string tag)
	{
		try
		{
			MethodInfo method = item.GetType().GetMethod("GetTagReadonly", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[1] { typeof(string) }, null);
			if (method == null)
			{
				return null;
			}
			object obj = method.Invoke(item, new object[1] { tag });
			if (obj == null)
			{
				return null;
			}
			return ReadTagInt(obj);
		}
		catch
		{
			return null;
		}
	}

	private static int? ReadTagInt(object tagState)
	{
		try
		{
			Type type = tagState.GetType();
			PropertyInfo property = type.GetProperty("valueInt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null && property.PropertyType == typeof(int))
			{
				return (int)property.GetValue(tagState, null);
			}
			FieldInfo field = type.GetField("valueInt", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null && field.FieldType == typeof(int))
			{
				return (int)field.GetValue(tagState);
			}
		}
		catch
		{
		}
		return null;
	}

	private static IntPtr GetPointer(object item)
	{
		if (_pointerProp == null)
		{
			Type type = item.GetType();
			while (type != null && type != typeof(object))
			{
				PropertyInfo property = type.GetProperty("Pointer", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null && property.PropertyType == typeof(IntPtr))
				{
					_pointerProp = property;
					break;
				}
				type = type.BaseType;
			}
		}
		try
		{
			return (_pointerProp == null) ? IntPtr.Zero : ((IntPtr)_pointerProp.GetValue(item, null));
		}
		catch
		{
			return IntPtr.Zero;
		}
	}

	public static void LogApis()
	{
		if (_scanned)
		{
			return;
		}
		Type type = FindType("GameItem");
		if (type == null)
		{
			ModLog.Warning("GameItem type not found in Assembly-CSharp");
			return;
		}
		ModLog.Info("== GameItem members matching Tag/Id/Upgrade ==");
		MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
		foreach (MethodInfo methodInfo in methods)
		{
			string name = methodInfo.Name;
			if (name.IndexOf("Tag", StringComparison.OrdinalIgnoreCase) >= 0 || name.IndexOf("Id", StringComparison.Ordinal) >= 0 || name.IndexOf("Upgrade", StringComparison.Ordinal) >= 0 || name.IndexOf("TagState", StringComparison.Ordinal) >= 0)
			{
				ModLog.Info($"  {methodInfo}");
			}
		}
		_scanned = true;
	}

	private static Type FindType(string name)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetName().Name != "Assembly-CSharp")
			{
				continue;
			}
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.Name == name || type.FullName == name)
					{
						return type;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}

	public static void LogFeedApis()
	{
		string[] array = new string[5] { "GraphUtils", "GameInventory", "GameSlotInventory", "GameGridInventory", "GraphNodeStorage" };
		foreach (string text in array)
		{
			Type type = FindType(text);
			if (type == null)
			{
				ModLog.Info("[API] class " + text + " not found");
				continue;
			}
			ModLog.Info("[API] == " + text + " feed-ish members ==");
			MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				string name = methodInfo.Name;
				if ((name.IndexOf("Add", StringComparison.Ordinal) >= 0 || name.IndexOf("Place", StringComparison.Ordinal) >= 0 || name.IndexOf("Insert", StringComparison.Ordinal) >= 0 || name.IndexOf("Slot", StringComparison.Ordinal) >= 0 || name.IndexOf("Child", StringComparison.Ordinal) >= 0 || name.IndexOf("Accept", StringComparison.Ordinal) >= 0 || name.IndexOf("Transfer", StringComparison.Ordinal) >= 0) && methodInfo.ToString().IndexOf("GameItem", StringComparison.Ordinal) >= 0)
				{
					ModLog.Info($"[API]   {methodInfo}");
				}
			}
		}
	}
}
public static class MachineUpgrade
{
	private sealed class BonusDef
	{
		public string[] Machines;

		public string MaterialFamily;

		public string MaterialKey;

		public string Tag;

		public string NameKey;

		public int QualityPerUse;

		public int PowerPerUse;

		public int PerformancePerUse;

		public int MaxUses = 99;

		public string CapTag;

		public int CapDivisor = 1;
	}

	private struct AggSnap
	{
		public string Id;

		public int Bp;

		public int Bq;

		public int Bf;

		public int Cp;

		public int Cq;

		public int Cf;

		public int AddLogged;

		public int DupLogged;
	}

	private const string TotalPower = "TOTAL_PERCENTAGE_POWER_BONUS_INT";

	private const string TotalQuality = "TOTAL_PERCENTAGE_QUALITY_BONUS_INT";

	private const string TotalPerformance = "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT";

	private const string DrawBase = "MACHINERY_DRAWN_POWER_BASE";

	private const string DrawCurrent = "machinery_drawn_power";

	private const string TagFurnaceIngot = "MOD_FURNACE_INGOT_UPGRADES";

	private const string TagFurnaceCircuit = "MOD_FURNACE_CIRCUIT_UPGRADES";

	private const string TagMoistureCircuit = "MOD_MOISTURE_CIRCUIT_UPGRADES";

	private const string TagMoistureWire = "MOD_MOISTURE_WIRE_UPGRADES";

	private const string TagPurifierIngot = "MOD_PURIFIER_INGOT_UPGRADES";

	private const string TagWineRackCircuit = "MOD_WINERACK_CIRCUIT_UPGRADES";

	private const string TagWineRackWire = "MOD_WINERACK_WIRE_UPGRADES";

	private const string TagWineRackWidth = "MOD_WINERACK_WINE_WIDTH";

	private const int WineRackWidthBase = 6;

	private const int WineRackHeight = 4;

	private const int WineRackWidthMax = 12;

	private const int WineGridX = 2;

	private const int WineGridY = 1;

	private const string TagProjectorCircuit = "MOD_PROJECTOR_CIRCUIT_UPGRADES";

	private const string TagProjector3D = "MOD_PROJECTOR_3D_UPGRADES";

	private const string TagProjectorWire = "MOD_PROJECTOR_WIRE_UPGRADES";

	private const string TagPurifierFilterParts = "MOD_PURIFIER_FILTER_PARTS";

	private const int PurifierFilterMaxParts = 80;

	private const int PurifierFilterStepParts = 10;

	private const string MatWaterFilterAdv = "water_filter_adv";

	private const string MatWaterFilter = "water_filter";

	private const int Unlimited = int.MaxValue;

	private static readonly List<BonusDef> Defs = new List<BonusDef>
	{
		new BonusDef
		{
			Machines = new string[1] { "furnace" },
			MaterialFamily = "metal_ingot",
			MaterialKey = "mat_ingot",
			Tag = "MOD_FURNACE_INGOT_UPGRADES",
			NameKey = "nm_furnace_ingot",
			QualityPerUse = 5,
			PowerPerUse = 10,
			MaxUses = 10
		},
		new BonusDef
		{
			Machines = new string[1] { "furnace" },
			MaterialFamily = "electronic",
			MaterialKey = "mat_circuit",
			Tag = "MOD_FURNACE_CIRCUIT_UPGRADES",
			NameKey = "nm_furnace_circuit",
			QualityPerUse = 5,
			PowerPerUse = 0,
			MaxUses = 10
		},
		new BonusDef
		{
			Machines = new string[2] { "moisture_farm", "moisture_farm_w" },
			MaterialFamily = "electronic",
			MaterialKey = "mat_circuit",
			Tag = "MOD_MOISTURE_CIRCUIT_UPGRADES",
			NameKey = "nm_moisture_circuit",
			QualityPerUse = 7,
			PowerPerUse = 0,
			MaxUses = 10
		},
		new BonusDef
		{
			Machines = new string[2] { "moisture_farm", "moisture_farm_w" },
			MaterialFamily = "wire",
			MaterialKey = "mat_wire",
			Tag = "MOD_MOISTURE_WIRE_UPGRADES",
			NameKey = "nm_moisture_wire",
			QualityPerUse = 0,
			PerformancePerUse = 10,
			MaxUses = 5
		},
		new BonusDef
		{
			Machines = new string[1] { "water_purifier" },
			MaterialFamily = "metal_ingot",
			MaterialKey = "mat_ingot",
			Tag = "MOD_PURIFIER_INGOT_UPGRADES",
			NameKey = "nm_purifier_ingot",
			PerformancePerUse = 5,
			MaxUses = 10
		},
		new BonusDef
		{
			Machines = new string[1] { "wine_rack" },
			MaterialFamily = "electronic",
			MaterialKey = "mat_circuit",
			Tag = "MOD_WINERACK_CIRCUIT_UPGRADES",
			NameKey = "nm_winrack_circuit",
			QualityPerUse = 15,
			PowerPerUse = -5,
			MaxUses = int.MaxValue
		},
		new BonusDef
		{
			Machines = new string[1] { "wine_rack" },
			MaterialFamily = "wire",
			MaterialKey = "mat_wire",
			Tag = "MOD_WINERACK_WIRE_UPGRADES",
			NameKey = "nm_winrack_wire",
			PowerPerUse = 10,
			MaxUses = int.MaxValue,
			CapTag = "MOD_WINERACK_CIRCUIT_UPGRADES",
			CapDivisor = 2
		},
		new BonusDef
		{
			Machines = new string[2] { "projector", "mirage_projector" },
			MaterialFamily = "electronic",
			MaterialKey = "mat_circuit",
			Tag = "MOD_PROJECTOR_CIRCUIT_UPGRADES",
			NameKey = "nm_projector_circuit",
			QualityPerUse = 5,
			PowerPerUse = -5,
			MaxUses = int.MaxValue
		},
		new BonusDef
		{
			Machines = new string[2] { "projector", "mirage_projector" },
			MaterialFamily = "printer_plastic",
			MaterialKey = "mat_plastic",
			Tag = "MOD_PROJECTOR_3D_UPGRADES",
			NameKey = "nm_projector_plastic",
			QualityPerUse = 10,
			MaxUses = 5
		},
		new BonusDef
		{
			Machines = new string[2] { "projector", "mirage_projector" },
			MaterialFamily = "wire",
			MaterialKey = "mat_wire",
			Tag = "MOD_PROJECTOR_WIRE_UPGRADES",
			NameKey = "nm_projector_wire",
			PowerPerUse = 5,
			MaxUses = int.MaxValue
		}
	};

	private static readonly Dictionary<long, DateTime> LastApply = new Dictionary<long, DateTime>();

	private static readonly HashSet<string> LoggedUnhandled = new HashSet<string>();

	private static bool _bonusing;

	private static readonly Dictionary<long, AggSnap> AggSnapStates = new Dictionary<long, AggSnap>();

	private static bool _ownedInited;

	private static MethodInfo _isItemOwned;

	private static bool _methodLogDone;

	private const string RechargerId = "recharger";

	private const string TagRechargerCap = "MOD_RECHARGER_CAPACITY";

	private const string TagRecharger3D = "MOD_RECHARGER_3D_PARTS";

	private const string TagRechargerCircuit = "MOD_RECHARGER_CIRCUITS";

	private const string TagRechargerWire = "MOD_RECHARGER_WIRES";

	private const string TagRechargerFast = "MOD_RECHARGER_FAST_CHARGE";

	private const int Recharger3DNeed = 4;

	private const int RechargerGridCols = 4;

	private const int RechargerGridRowsBase = 2;

	private const int RechargerGridRowsUp = 4;

	private static readonly HashSet<string> RechargerLogged = new HashSet<string>();

	private static bool _rechargerPatched;

	private static bool _powerChargePatched;

	private static bool _powerChargeInDouble;

	private static MethodInfo _rechargerCycleEnd;

	private static Harmony _rechargerHarmony;

	private static readonly Dictionary<long, DateTime> RechargerTickLog = new Dictionary<long, DateTime>();

	private static readonly Dictionary<string, DateTime> PowerChargeLog = new Dictionary<string, DateTime>();

	private static bool _rechargerMethodsLogged;

	private const string TagCurrentCharge = "CURRENT_CHARGE_TAG";

	private const int RechargerDisplayCount = 4;

	private static bool _inRechargerRender;

	private static bool _rechargerDisplayPatched;

	public static void TryRestoreWineRackWidth(object machine)
	{
		if (machine == null)
		{
			return;
		}
		try
		{
			string text = GameItemHelper.TryGetItemId(machine);
			if (text != "wine_rack")
			{
				return;
			}
			int valueOrDefault = GameItemHelper.GetIntTag(machine, "MOD_WINERACK_WINE_WIDTH").GetValueOrDefault();
			if (valueOrDefault > 0)
			{
				int num = 6 + valueOrDefault;
				if (ResizeWineRackGrid(machine, num) && LoggedUnhandled.Add("winerack|restore|" + num))
				{
					ModLog.Info($"[Upgrade] wine_rack restored width to {num} (level {valueOrDefault})");
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] restore wine width err " + ex.Message);
		}
	}

	public static bool HandleDrop(object machine, object material)
	{
		if (machine == null)
		{
			return false;
		}
		long num = Key(machine);
		if (num == 0)
		{
			num = RuntimeHelpers.GetHashCode(machine);
		}
		if (LastApply.TryGetValue(num, out var value) && (DateTime.UtcNow - value).TotalMilliseconds < 500.0)
		{
			return true;
		}
		LastApply[num] = DateTime.UtcNow;
		string text = GameItemHelper.TryGetItemId(material);
		string text2 = GameItemHelper.TryGetItemId(machine);
		if (text == null || text2 == null)
		{
			return false;
		}
		if (HandleRechargerDrop(machine, material, text2, text))
		{
			return true;
		}
		bool flag = text2 == "water_purifier" && (text == "water_filter_adv" || text == "water_filter");
		bool flag2 = text2 == "wine_rack" && MatchesMaterial("metal_ingot", text);
		bool flag3 = (text2 == "projector" || text2 == "mirage_projector") && MatchesMaterial("metal_ingot", text);
		BonusDef bonusDef = FindDef(text2, text);
		if (!flag && !flag2 && !flag3 && bonusDef == null)
		{
			LogUnhandled(text2, text);
			return false;
		}
		if (!IsItemOwned(material) || !IsItemOwned(machine))
		{
			string item = text2 + "|" + text + "#owned";
			if (LoggedUnhandled.Add(item))
			{
				ModLog.Info($"[Upgrade] blocked (item not owned by player) machine='{text2}' material='{text}'");
			}
			return true;
		}
		if (flag)
		{
			ApplyPurifierFilter(machine, material, text);
			return true;
		}
		if (flag2)
		{
			ApplyWineRackWidth(machine, material, text);
			return true;
		}
		if (flag3)
		{
			ApplyProjectorIngot(machine, material, text);
			return true;
		}
		ApplyUpgrade(machine, material, bonusDef, text);
		return true;
	}

	private static void ApplyProjectorIngot(object machine, object material, string matId)
	{
		string machineId = GameItemHelper.TryGetItemId(machine);
		BonusDef bonusDef = FindDefByFamily(machineId, "electronic");
		BonusDef bonusDef2 = FindDefByFamily(machineId, "wire");
		if (bonusDef == null || bonusDef2 == null)
		{
			ModLog.Info("[Upgrade] projector circuit/wire def missing");
			return;
		}
		ApplyUpgrade(machine, material, bonusDef, matId, consume: false, report: false);
		ApplyUpgrade(machine, material, bonusDef2, matId, consume: false, report: false);
		ConsumeOne(material);
		ModLog.Info("[Upgrade] '" + matId + "'->projector 金属锭(电路板x1+线缆x1) 质量+5%");
		PlaySuccessSfx();
	}

	private static BonusDef FindDefByFamily(string machineId, string family)
	{
		foreach (BonusDef def in Defs)
		{
			if (!ArrayContains(def.Machines, machineId) || !string.Equals(def.MaterialFamily, family, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}
			return def;
		}
		return null;
	}

	private static bool IsItemOwned(object item)
	{
		if (item == null)
		{
			return false;
		}
		if (!_ownedInited)
		{
			_ownedInited = true;
			try
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (Assembly assembly in assemblies)
				{
					if (assembly.GetName().Name != "Assembly-CSharp")
					{
						continue;
					}
					Type type = assembly.GetType("GeneralHelper");
					if (type == null)
					{
						Type[] types = assembly.GetTypes();
						foreach (Type type2 in types)
						{
							if (type2.Name == "GeneralHelper")
							{
								type = type2;
								break;
							}
						}
					}
					if (type == null)
					{
						break;
					}
					MethodInfo[] methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public);
					foreach (MethodInfo methodInfo in methods)
					{
						if (methodInfo.Name == "IsItemOwned" && methodInfo.GetParameters().Length == 1)
						{
							_isItemOwned = methodInfo;
							break;
						}
					}
					break;
				}
				if (_isItemOwned == null)
				{
					ModLog.Warning("[Upgrade] GeneralHelper.IsItemOwned not found; ownership check disabled");
				}
			}
			catch (Exception ex)
			{
				ModLog.Warning("[Upgrade] IsItemOwned lookup err " + ex.Message);
			}
		}
		if (_isItemOwned == null)
		{
			return true;
		}
		try
		{
			return (bool)_isItemOwned.Invoke(null, new object[1] { item });
		}
		catch
		{
			return true;
		}
	}

	private static void PlaySuccessSfx()
	{
		try
		{
			AudioManager instance = AudioManager.Instance;
			if ((Object)(object)instance != (Object)null)
			{
				instance.Play2("scrap_metal_drop");
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] sfx err " + ex.Message);
		}
	}

	private static void ApplyPurifierFilter(object machine, object material, string matId)
	{
		int valueOrDefault = GameItemHelper.GetIntTag(machine, "MOD_PURIFIER_FILTER_PARTS").GetValueOrDefault();
		if (valueOrDefault >= 80)
		{
			ModLog.Info("[Upgrade] purifier filter upgrade full (8/8 advanced); nothing applied");
			return;
		}
		int num = ((!(matId == "water_filter_adv")) ? 1 : 10);
		int num2 = valueOrDefault / 10;
		int num3 = Math.Min(80, valueOrDefault + num);
		int num4 = num3 / 10;
		SetIntTag(machine, "MOD_PURIFIER_FILTER_PARTS", num3);
		int num5 = num4 - num2;
		if (num5 > 0)
		{
			RefreshQualityAtDrop(machine, num5 * 15);
		}
		ConsumeOne(material);
		ModLog.Info($"[Upgrade] '{matId}'->water_purifier filter {num3}parts (adv={num4}/8 bank={num3 % 10}/10)");
		PlaySuccessSfx();
	}

	private static void RefreshQualityAtDrop(object machine, int addPoints)
	{
		if (_bonusing)
		{
			return;
		}
		_bonusing = true;
		try
		{
			int valueOrDefault = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetValueOrDefault();
			WriteIntTag(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT", valueOrDefault + addPoints);
		}
		finally
		{
			_bonusing = false;
		}
	}

	private static void ApplyWineRackWidth(object machine, object material, string matId)
	{
		int valueOrDefault = GameItemHelper.GetIntTag(machine, "MOD_WINERACK_WINE_WIDTH").GetValueOrDefault();
		int num = 6 + valueOrDefault;
		if (num >= 12)
		{
			ModLog.Info($"[Upgrade] winerack width already max ({12}); nothing applied");
			return;
		}
		int num2 = num + 1;
		if (!ResizeWineRackGrid(machine, num2))
		{
			ModLog.Info("[Upgrade] winerack grid not found/resized; nothing applied");
			return;
		}
		SetIntTag(machine, "MOD_WINERACK_WINE_WIDTH", valueOrDefault + 1);
		ConsumeOne(material);
		ModLog.Info($"[Upgrade] '{matId}'->wine_rack width {num}x{4}->{num2}x{4} (level {valueOrDefault + 1}/{6})");
		PlaySuccessSfx();
	}

	private static bool ResizeWineRackGrid(object machine, int newCols)
	{
		try
		{
			GameItem val = (GameItem)((machine is GameItem) ? machine : null);
			if (val == null)
			{
				return false;
			}
			PixelWindow contentWindow = val.contentWindow;
			if (contentWindow == null)
			{
				ModLog.Info("[Upgrade] winerack: no contentWindow");
				return false;
			}
			object obj = contentWindow.childElement;
			if (obj == null)
			{
				obj = GetMemberValue(val, "child");
			}
			object obj2 = TryCast(obj, "GridPixelElement");
			if (obj2 == null)
			{
				ModLog.Info("[Upgrade] winerack: childElement cast to GridPixelElement failed");
				return false;
			}
			MethodInfo methodInfo = Find2IntMethod(obj2, "GetElement");
			if (methodInfo == null)
			{
				ModLog.Info("[Upgrade] winerack: GridPixelElement has no GetElement");
				return false;
			}
			List<object> list = new List<object>();
			for (int i = 0; i <= 2; i++)
			{
				for (int j = 0; j <= 4; j++)
				{
					object obj3;
					try
					{
						obj3 = methodInfo.Invoke(obj2, new object[2] { j, i });
					}
					catch
					{
						continue;
					}
					if (obj3 != null)
					{
						object obj5 = TryCast(obj3, "GameGridInventory");
						if (obj5 != null)
						{
							list.Add(obj5);
						}
					}
				}
			}
			if (list.Count == 0)
			{
				ModLog.Info("[Upgrade] winerack: no GameGridInventory under grid layout");
				return false;
			}
			object obj6 = null;
			foreach (object item in list)
			{
				if (ShapeDim(item, wantWidth: false) == 4)
				{
					obj6 = item;
					break;
				}
			}
			if (obj6 == null && list.Count == 1)
			{
				obj6 = list[0];
			}
			if (obj6 == null)
			{
				ModLog.Info($"[Upgrade] winerack: cannot pick wine grid from {list.Count} (heights " + string.Join(",", list.ConvertAll((object x) => ShapeDim(x, wantWidth: false).ToString()).ToArray()) + ")");
				return false;
			}
			if (ShapeDim(obj6, wantWidth: true) >= newCols)
			{
				return true;
			}
			int num = 6 + GameItemHelper.GetIntTag(machine, "MOD_WINERACK_WINE_WIDTH").GetValueOrDefault();
			int intProp = GetIntProp(obj6, "widthPixels");
			InvokeMethod(obj6, "SetShape", newCols, 4);
			InvokeMethod(obj6, "Validate");
			int num2 = GetIntProp(obj6, "widthPixels") - intProp;
			if (num2 <= 0)
			{
				int num3 = ((intProp > 0) ? Math.Max(1, intProp / num) : 20);
				num2 = num3;
				InvokeMethod(obj6, "ResizePixels", GetIntProp(obj6, "widthPixels") + num2, GetIntProp(obj6, "heightPixels"));
				InvokeMethod(obj6, "Validate");
			}
			contentWindow.ResizePixels(contentWindow.widthPixels + num2, contentWindow.heightPixels);
			contentWindow.Validate();
			return true;
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] wine grid resize err " + ex.GetType().Name + ": " + ex.Message);
			return false;
		}
	}

	private static object TryCast(object o, string typeName)
	{
		if (o == null)
		{
			return null;
		}
		Type type = null;
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (!(assembly.GetName().Name != "Assembly-CSharp"))
			{
				type = assembly.GetType("Il2Cpp." + typeName);
				if (type != null)
				{
					break;
				}
			}
		}
		if (type == null)
		{
			return null;
		}
		try
		{
			MethodInfo methodInfo = null;
			MethodInfo[] methods = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo methodInfo2 in methods)
			{
				if (methodInfo2.Name == "TryCast" && methodInfo2.IsGenericMethodDefinition && methodInfo2.GetParameters().Length == 1)
				{
					methodInfo = methodInfo2;
					break;
				}
			}
			if (methodInfo == null)
			{
				MethodInfo[] methods2 = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
				foreach (MethodInfo methodInfo3 in methods2)
				{
					if (methodInfo3.Name == "Cast" && methodInfo3.IsGenericMethodDefinition && methodInfo3.GetParameters().Length == 0)
					{
						methodInfo = methodInfo3;
						break;
					}
				}
			}
			if (methodInfo == null)
			{
				ModLog.Info("[Upgrade] TryCast: no Cast/TryCast generic on " + o.GetType().Name);
				return null;
			}
			MethodInfo methodInfo4 = methodInfo.MakeGenericMethod(type);
			if (methodInfo.Name == "TryCast")
			{
				object[] array = new object[1];
				return ((bool)methodInfo4.Invoke(o, array)) ? array[0] : null;
			}
			return methodInfo4.Invoke(o, null);
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] TryCast " + typeName + " err " + ex.Message);
			return null;
		}
	}

	private static bool HasPropOrMethod(object o, string name)
	{
		try
		{
			return o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public) != null || o.GetType().GetMethod("get_" + name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic) != null;
		}
		catch
		{
			return false;
		}
	}

	private static string GetPropStr(object o, string name)
	{
		try
		{
			object obj = null;
			PropertyInfo property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				obj = property.GetValue(o, null);
			}
			else
			{
				MethodInfo method = o.GetType().GetMethod("get_" + name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					obj = method.Invoke(o, null);
				}
			}
			if (obj == null)
			{
				return "null";
			}
			return obj.GetType().Name;
		}
		catch
		{
			return "err";
		}
	}

	private static object GetMemberValue(object o, string name)
	{
		try
		{
			PropertyInfo property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				return property.GetValue(o, null);
			}
			FieldInfo field = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (field != null)
			{
				return field.GetValue(o);
			}
			MethodInfo method = o.GetType().GetMethod("get_" + name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				return method.Invoke(o, null);
			}
		}
		catch
		{
		}
		return null;
	}

	private static void LogTypeMethodsOnce(object o)
	{
		if (_methodLogDone)
		{
			return;
		}
		_methodLogDone = true;
		try
		{
			StringBuilder stringBuilder = new StringBuilder();
			MethodInfo[] methods = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				stringBuilder.Append(methodInfo.Name).Append('(').Append(methodInfo.GetParameters().Length)
					.Append(") ");
			}
			ModLog.Info("[Upgrade] childElement methods: " + stringBuilder);
		}
		catch
		{
		}
	}

	private static MethodInfo Find2IntMethod(object o, string name)
	{
		try
		{
			MethodInfo[] methods = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				if (!(methodInfo.Name != name))
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					if (parameters.Length == 2 && parameters[0].ParameterType == typeof(int) && parameters[1].ParameterType == typeof(int))
					{
						return methodInfo;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static bool HasMethod(object o, string name)
	{
		try
		{
			MethodInfo[] methods = o.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name == name)
				{
					return true;
				}
			}
		}
		catch
		{
		}
		return false;
	}

	private static void CollectGrids(object root, List<object> grids, int maxDepth = 10)
	{
		if (root == null)
		{
			return;
		}
		HashSet<long> hashSet = new HashSet<long>();
		Queue<object> queue = new Queue<object>();
		queue.Enqueue(root);
		int num = 0;
		int num2 = 0;
		while (queue.Count > 0 && num < maxDepth && num2 < 4000)
		{
			int count = queue.Count;
			for (int i = 0; i < count; i++)
			{
				object obj = queue.Dequeue();
				if (obj == null)
				{
					continue;
				}
				num2++;
				long item = RuntimeHelpers.GetHashCode(obj);
				if (!hashSet.Add(item))
				{
					continue;
				}
				if (obj.GetType().Name == "GameGridInventory")
				{
					grids.Add(obj);
				}
				foreach (object item4 in MemberValues(obj))
				{
					if (item4 == null)
					{
						continue;
					}
					Type type = item4.GetType();
					if (type.IsPrimitive || type == typeof(string) || type.IsEnum)
					{
						continue;
					}
					string name = type.Name;
					if (name.IndexOf("List", StringComparison.Ordinal) >= 0)
					{
						foreach (object item5 in TryEnumerate(item4))
						{
							if (item5 != null)
							{
								long item2 = RuntimeHelpers.GetHashCode(item5);
								if (!hashSet.Contains(item2))
								{
									queue.Enqueue(item5);
								}
							}
						}
					}
					else if (LooksGraphy(name))
					{
						long item3 = RuntimeHelpers.GetHashCode(item4);
						if (!hashSet.Contains(item3))
						{
							queue.Enqueue(item4);
						}
					}
				}
			}
			num++;
		}
	}

	private static IEnumerable<object> TryEnumerate(object o)
	{
		List<object> list = new List<object>();
		if (o is IEnumerable enumerable)
		{
			try
			{
				foreach (object item in enumerable)
				{
					if (item != null)
					{
						list.Add(item);
					}
					if (list.Count > 500)
					{
						break;
					}
				}
				if (list.Count > 0)
				{
					return list;
				}
			}
			catch
			{
			}
		}
		try
		{
			Type type = o.GetType();
			MethodInfo method = type.GetMethod("get_Count", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			MethodInfo method2 = type.GetMethod("get_Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null && method2 != null)
			{
				int num = Convert.ToInt32(method.Invoke(o, null));
				for (int i = 0; i < num; i++)
				{
					if (list.Count >= 500)
					{
						break;
					}
					object obj2 = method2.Invoke(o, new object[1] { i });
					if (obj2 != null)
					{
						list.Add(obj2);
					}
				}
			}
		}
		catch
		{
		}
		return list;
	}

	private static int ShapeDim(object grid, bool wantWidth)
	{
		try
		{
			object obj = null;
			PropertyInfo property = grid.GetType().GetProperty("inventoryShape", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property != null)
			{
				obj = property.GetValue(grid, null);
			}
			if (obj == null)
			{
				obj = grid.GetType().GetMethod("get_inventoryShape", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(grid, null);
			}
			if (obj == null)
			{
				return 0;
			}
			string text = (wantWidth ? "width" : "height");
			PropertyInfo property2 = obj.GetType().GetProperty(text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (property2 != null)
			{
				return Convert.ToInt32(property2.GetValue(obj, null));
			}
			MethodInfo method = obj.GetType().GetMethod("get_" + text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				return Convert.ToInt32(method.Invoke(obj, null));
			}
		}
		catch
		{
		}
		return 0;
	}

	private static List<object> FindDescendantsOfType(object root, string typeName, int maxDepth)
	{
		List<object> list = new List<object>();
		if (root == null)
		{
			return list;
		}
		HashSet<long> hashSet = new HashSet<long>();
		Queue<object> queue = new Queue<object>();
		queue.Enqueue(root);
		int num = 0;
		while (queue.Count > 0 && num < maxDepth)
		{
			int count = queue.Count;
			for (int i = 0; i < count; i++)
			{
				object obj = queue.Dequeue();
				if (obj == null)
				{
					continue;
				}
				long item = RuntimeHelpers.GetHashCode(obj);
				if (!hashSet.Add(item))
				{
					continue;
				}
				Type type = obj.GetType();
				if (type.Name == typeName)
				{
					list.Add(obj);
				}
				foreach (object item3 in MemberValues(obj))
				{
					if (item3 == null)
					{
						continue;
					}
					Type type2 = item3.GetType();
					if (!type2.IsPrimitive && !(type2 == typeof(string)) && !type2.IsEnum && LooksGraphy(type2.Name))
					{
						long item2 = RuntimeHelpers.GetHashCode(item3);
						if (!hashSet.Contains(item2))
						{
							queue.Enqueue(item3);
						}
					}
				}
			}
			num++;
		}
		return list;
	}

	private static IEnumerable<object> MemberValues(object o)
	{
		Type t = o.GetType();
		PropertyInfo[] properties = t.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (PropertyInfo p in properties)
		{
			if (p.GetIndexParameters().Length == 0 && p.Name.IndexOf("childElements", StringComparison.Ordinal) < 0)
			{
				object v;
				try
				{
					v = p.GetValue(o, null);
				}
				catch
				{
					continue;
				}
				if (v != null)
				{
					yield return v;
				}
			}
		}
		FieldInfo[] fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
		foreach (FieldInfo f in fields)
		{
			object v2;
			try
			{
				v2 = f.GetValue(o);
			}
			catch
			{
				continue;
			}
			if (v2 != null)
			{
				yield return v2;
			}
		}
	}

	private static bool LooksGraphy(string name)
	{
		return name.IndexOf("Pixel", StringComparison.Ordinal) >= 0 || name.IndexOf("Grid", StringComparison.Ordinal) >= 0 || name.IndexOf("Slot", StringComparison.Ordinal) >= 0 || name.IndexOf("Window", StringComparison.Ordinal) >= 0 || name.IndexOf("Item", StringComparison.Ordinal) >= 0 || name.IndexOf("Inventory", StringComparison.Ordinal) >= 0 || name.IndexOf("Storage", StringComparison.Ordinal) >= 0 || name.IndexOf("Graph", StringComparison.Ordinal) >= 0;
	}

	private static object InvokeMethod(object instance, string name, params object[] args)
	{
		try
		{
			Type type = instance.GetType();
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name != name)
				{
					continue;
				}
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (parameters.Length != args.Length)
				{
					continue;
				}
				bool flag = true;
				for (int j = 0; j < parameters.Length; j++)
				{
					if (!(parameters[j].ParameterType == typeof(object)) && !(parameters[j].ParameterType == typeof(int)) && (!(args[j] is int) || !(parameters[j].ParameterType == typeof(int))) && !parameters[j].ParameterType.IsInstanceOfType(args[j]))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					return methodInfo.Invoke(instance, args);
				}
			}
			return null;
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] InvokeMethod '" + name + "' err " + ex.Message);
			return null;
		}
	}

	private static int GetIntProp(object instance, string name)
	{
		try
		{
			Type type = instance.GetType();
			PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public);
			if (property != null)
			{
				return Convert.ToInt32(property.GetValue(instance, null));
			}
		}
		catch
		{
		}
		return 0;
	}

	private static bool MatchesMaterial(string family, string id)
	{
		if (string.IsNullOrEmpty(family) || string.IsNullOrEmpty(id))
		{
			return false;
		}
		if (string.Equals(id, family, StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}
		return id.EndsWith("_" + family, StringComparison.OrdinalIgnoreCase);
	}

	private static BonusDef FindDef(string machineId, string materialId)
	{
		foreach (BonusDef def in Defs)
		{
			if (!ArrayContains(def.Machines, machineId) || !MatchesMaterial(def.MaterialFamily, materialId))
			{
				continue;
			}
			return def;
		}
		return null;
	}

	public static bool IsUpgradeablePair(object machine, object material)
	{
		if (machine == null || material == null)
		{
			return false;
		}
		string text = GameItemHelper.TryGetItemId(machine);
		string text2 = GameItemHelper.TryGetItemId(material);
		if (text == null || text2 == null)
		{
			return false;
		}
		if (text == "recharger" && IsRechargerMaterial(text2))
		{
			return true;
		}
		if (text == "water_purifier" && (text2 == "water_filter_adv" || text2 == "water_filter"))
		{
			return true;
		}
		if (text == "wine_rack" && MatchesMaterial("metal_ingot", text2))
		{
			return true;
		}
		if ((text == "projector" || text == "mirage_projector") && MatchesMaterial("metal_ingot", text2))
		{
			return true;
		}
		return FindDef(text, text2) != null;
	}

	private static bool ArrayContains(string[] arr, string v)
	{
		foreach (string text in arr)
		{
			if (text == v)
			{
				return true;
			}
		}
		return false;
	}

	private static void ApplyUpgrade(object machine, object material, BonusDef def, string matId, bool consume = true, bool report = true)
	{
		int valueOrDefault = GameItemHelper.GetIntTag(machine, def.Tag).GetValueOrDefault();
		int num = EffectiveCap(def, machine);
		if (num < 1)
		{
			if (report)
			{
				ModLog.Info($"[Upgrade] {UILoc.Z(def.NameKey)}({UILoc.Z(def.MaterialKey)}) needs prerequisites first (cap {CapText(def)}); nothing applied");
			}
			return;
		}
		if (valueOrDefault >= num)
		{
			if (report)
			{
				ModLog.Info($"[Upgrade] {UILoc.Z(def.NameKey)}({UILoc.Z(def.MaterialKey)}) cap reached ({CapText(def)}); nothing applied");
			}
			return;
		}
		int value = valueOrDefault + 1;
		SetIntTag(machine, def.Tag, value);
		RefreshOnDrop(machine, def);
		if (consume)
		{
			ConsumeOne(material);
		}
		if (report)
		{
			string text = "";
			if (def.PerformancePerUse > 0)
			{
				text += $"performance+{def.PerformancePerUse}% ";
			}
			if (def.PowerPerUse > 0)
			{
				text += $"power+{def.PowerPerUse}% ";
			}
			if (def.PowerPerUse < 0)
			{
				text += $"power{def.PowerPerUse}% ";
			}
			if (def.QualityPerUse > 0)
			{
				text += $"quality+{def.QualityPerUse}%";
			}
			ModLog.Info($"[Upgrade] '{matId}'->{def.Machines[0]} {UILoc.Z(def.NameKey)}({UILoc.Z(def.MaterialKey)}) #{value}/{CapText(def)}: {text.Trim()}");
			PlaySuccessSfx();
		}
	}

	private static int EffectiveCap(BonusDef def, object machine)
	{
		if (def.CapTag != null)
		{
			int valueOrDefault = GameItemHelper.GetIntTag(machine, def.CapTag).GetValueOrDefault();
			return valueOrDefault / def.CapDivisor;
		}
		return def.MaxUses;
	}

	private static string CapText(BonusDef def)
	{
		if (def.CapTag != null)
		{
			return "电路板数/2";
		}
		return (def.MaxUses >= int.MaxValue) ? "∞" : def.MaxUses.ToString();
	}

	private static void RefreshOnDrop(object machine, BonusDef def)
	{
		if (_bonusing)
		{
			return;
		}
		_bonusing = true;
		try
		{
			if (def.PowerPerUse != 0)
			{
				int valueOrDefault = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT").GetValueOrDefault();
				WriteIntTag(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT", valueOrDefault + def.PowerPerUse);
			}
			if (def.QualityPerUse != 0)
			{
				int valueOrDefault2 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetValueOrDefault();
				WriteIntTag(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT", valueOrDefault2 + def.QualityPerUse);
			}
			if (def.PerformancePerUse != 0)
			{
				int valueOrDefault3 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT").GetValueOrDefault();
				WriteIntTag(machine, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT", valueOrDefault3 + def.PerformancePerUse);
			}
		}
		finally
		{
			_bonusing = false;
		}
		if (def.PowerPerUse != 0)
		{
			RecomputeFromGame(machine);
		}
	}

	public static void AddVirtualBonus(object machine)
	{
		if (machine == null || _bonusing)
		{
			return;
		}
		string text = GameItemHelper.TryGetItemId(machine);
		if (text == null)
		{
			return;
		}
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		foreach (BonusDef def in Defs)
		{
			if (ArrayContains(def.Machines, text))
			{
				int valueOrDefault = GameItemHelper.GetIntTag(machine, def.Tag).GetValueOrDefault();
				num += valueOrDefault * def.PowerPerUse;
				num2 += valueOrDefault * def.QualityPerUse;
				num3 += valueOrDefault * def.PerformancePerUse;
			}
		}
		if (text == "water_purifier")
		{
			int valueOrDefault2 = GameItemHelper.GetIntTag(machine, "MOD_PURIFIER_FILTER_PARTS").GetValueOrDefault();
			num2 += valueOrDefault2 / 10 * 15;
		}
		if (num == 0 && num2 == 0 && num3 == 0)
		{
			return;
		}
		int num4 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT").GetValueOrDefault();
		int num5 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetValueOrDefault();
		int num6 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT").GetValueOrDefault();
		long key = AggKey(machine);
		if (AggSnapStates.TryGetValue(key, out var value) && value.Id == text && value.Bp == num && value.Bq == num2 && value.Bf == num3 && value.Cp == num4 && value.Cq == num5 && value.Cf == num6)
		{
			if (value.DupLogged < 10)
			{
				value.DupLogged++;
				AggSnapStates[key] = value;
				ModLog.Info($"[Upgrade] agg dup-skip {text} p+{num} q+{num2} f+{num3} (totalP={num4} q={num5} f={num6})");
			}
			return;
		}
		if (value.Id == null || value.AddLogged == 0)
		{
			ModLog.Info($"[Upgrade] agg first {text} stored p+{num} q+{num2} f+{num3} on totals p={num4} q={num5} f={num6}");
		}
		_bonusing = true;
		try
		{
			if (num != 0)
			{
				int valueOrDefault3 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT").GetValueOrDefault();
				WriteIntTag(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT", valueOrDefault3 + num);
				num4 = valueOrDefault3 + num;
			}
			if (num2 != 0)
			{
				int valueOrDefault4 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT").GetValueOrDefault();
				WriteIntTag(machine, "TOTAL_PERCENTAGE_QUALITY_BONUS_INT", valueOrDefault4 + num2);
				num5 = valueOrDefault4 + num2;
			}
			if (num3 != 0)
			{
				int valueOrDefault5 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT").GetValueOrDefault();
				WriteIntTag(machine, "TOTAL_PERCENTAGE_PERFORMANCE_BONUS_INT", valueOrDefault5 + num3);
				num6 = valueOrDefault5 + num3;
			}
		}
		finally
		{
			_bonusing = false;
		}
		AggSnapStates[key] = new AggSnap
		{
			Id = text,
			Bp = num,
			Bq = num2,
			Bf = num3,
			Cp = num4,
			Cq = num5,
			Cf = num6,
			AddLogged = 1
		};
	}

	private static long AggKey(object machine)
	{
		long num = Key(machine);
		if (num != 0)
		{
			return num;
		}
		return -RuntimeHelpers.GetHashCode(machine);
	}

	private static void RecomputeFromGame(object machine)
	{
		try
		{
			int? intTag = GameItemHelper.GetIntTag(machine, "MACHINERY_DRAWN_POWER_BASE");
			int? intTag2 = GameItemHelper.GetIntTag(machine, "TOTAL_PERCENTAGE_POWER_BONUS_INT");
			if (intTag.HasValue)
			{
				int valueOrDefault = intTag2.GetValueOrDefault();
				int value = Math.Max(1, (int)Math.Round((double)(intTag.Value * (100 - valueOrDefault)) / 100.0));
				WriteIntTag(machine, "machinery_drawn_power", value);
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] recompute-draw err " + ex.Message);
		}
	}

	private static void LogUnhandled(string mId, string matId)
	{
		if (!MatchesMaterial("metal_ingot", matId) && !MatchesMaterial("electronic", matId) && !MatchesMaterial("wire", matId) && matId.IndexOf("chem", StringComparison.OrdinalIgnoreCase) < 0 && matId.IndexOf("water_filter", StringComparison.OrdinalIgnoreCase) < 0)
		{
			switch (mId)
			{
			default:
				if (!(mId == "wine_rack"))
				{
					return;
				}
				break;
			case "furnace":
			case "moisture_farm":
			case "moisture_farm_w":
			case "water_purifier":
				break;
			}
		}
		string item = mId + "|" + matId;
		if (LoggedUnhandled.Add(item))
		{
			ModLog.Info($"[Upgrade] unhandled drop machine='{mId}' material='{matId}' (no recipe)");
		}
	}

	private static long Key(object machine)
	{
		try
		{
			MethodInfo method = machine.GetType().GetMethod("get_uniqueId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method != null)
			{
				object obj = method.Invoke(machine, null);
				if (obj is int num)
				{
					return num;
				}
				if (obj is long result)
				{
					return result;
				}
			}
		}
		catch
		{
		}
		return 0L;
	}

	private static object Invoke(object instance, string method, params object[] args)
	{
		try
		{
			return instance.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(instance, args);
		}
		catch
		{
			return null;
		}
	}

	private static bool WriteIntTag(object machineObj, string name, int value)
	{
		try
		{
			GameItem val = (GameItem)((machineObj is GameItem) ? machineObj : null);
			if (val == null)
			{
				return false;
			}
			Action<TagState> action = (TagState ts) =>
			{
				ts.valueInt = value;
			};
			Action<TagState> val2 = Action<TagState>.op_Implicit(action);
			val.ModifyTag(name, val2, false);
			return true;
		}
		catch (Exception ex)
		{
			ModLog.Info($"[Upgrade] WriteIntTag('{name}') failed: {ex.GetType().Name}: {ex.Message}");
			return false;
		}
	}

	private static void SetIntTag(object machine, string name, int value)
	{
		if (!WriteIntTag(machine, name, value))
		{
			Invoke(machine, "EnableTag", name, true);
			WriteIntTag(machine, name, value);
		}
	}

	private static void ConsumeOne(object material)
	{
		if (material == null)
		{
			return;
		}
		try
		{
			long num = 0L;
			bool flag = false;
			Action<long> action = null;
			Type type = material.GetType();
			PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (PropertyInfo prop in properties)
			{
				Type type2 = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
				if ((type2 != typeof(int) && type2 != typeof(long)) || !IsCountName(prop.Name))
				{
					continue;
				}
				object value = prop.GetValue(material, null);
				num = Convert.ToInt64(value);
				if (num > 0)
				{
					Type pt = type2;
					action = (long v) =>
					{
						prop.SetValue(material, Convert.ChangeType(v, pt), null);
					};
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo f in fields)
				{
					Type type3 = Nullable.GetUnderlyingType(f.FieldType) ?? f.FieldType;
					if ((type3 != typeof(int) && type3 != typeof(long)) || !IsCountName(f.Name))
					{
						continue;
					}
					num = Convert.ToInt64(f.GetValue(material));
					if (num > 0)
					{
						Type ft = type3;
						action = (long v) =>
						{
							f.SetValue(material, Convert.ChangeType(v, ft));
						};
						flag = true;
						break;
					}
				}
			}
			if (flag && num > 1)
			{
				action(num - 1);
			}
			else if (!TryDestroySingle(material))
			{
				ModLog.Info("[Upgrade] material not consumed (could not decrement/destroy)");
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] consume probe failed: " + ex.Message);
		}
	}

	private static bool TryDestroySingle(object material)
	{
		try
		{
			GameItem val = (GameItem)((material is GameItem) ? material : null);
			if (val == null)
			{
				return false;
			}
			List<GameItem> val2 = new List<GameItem>();
			val2.Add(val);
			int num = GraphUtils.TryDestroyAll(val2, 1);
			return num == 0;
		}
		catch (Exception ex)
		{
			ModLog.Info("[Upgrade] destroy-one err " + ex.Message);
			return false;
		}
	}

	private static bool IsCountName(string n)
	{
		string text = n.ToLowerInvariant();
		return text.Contains("count") || text.Contains("amount") || text.Contains("quantity") || text.Contains("stack") || text.Contains("qty");
	}

	public static void AppendTooltipLines(object builderObj, object machineObj)
	{
		if (builderObj == null || machineObj == null)
		{
			return;
		}
		try
		{
			RichTextBuilder val = (RichTextBuilder)((builderObj is RichTextBuilder) ? builderObj : null);
			if (val == null)
			{
				return;
			}
			string text = GameItemHelper.TryGetItemId(machineObj);
			if (text == null)
			{
				return;
			}
			bool flag = false;
			foreach (BonusDef def in Defs)
			{
				if (ArrayContains(def.Machines, text))
				{
					flag = true;
					break;
				}
			}
			if (text == "recharger")
			{
				flag = true;
			}
			if (!flag)
			{
				return;
			}
			val.AddEmptyLine(8);
			foreach (BonusDef def2 in Defs)
			{
				if (ArrayContains(def2.Machines, text))
				{
					int valueOrDefault = GameItemHelper.GetIntTag(machineObj, def2.Tag).GetValueOrDefault();
					int num = EffectiveCap(def2, machineObj);
					bool flag2 = def2.CapTag != null && num < 1;
					bool flag3 = num > 0 && valueOrDefault >= num;
					string text2 = CapTextLocalized(def2, num);
					string progress;
					if (flag2)
					{
						progress = text2;
					}
					else
					{
						progress = (flag3 ? UILoc.KF("maxed", valueOrDefault, text2) : (valueOrDefault + "/" + text2));
					}
					AddUpgLine(val, def2.NameKey, UILoc.KF("req", UILoc.K(def2.MaterialKey), 1), progress, GainText(def2), flag3, valueOrDefault > 0, flag2);
				}
			}
			if (text == "water_purifier")
			{
				int valueOrDefault2 = GameItemHelper.GetIntTag(machineObj, "MOD_PURIFIER_FILTER_PARTS").GetValueOrDefault();
				int num2 = valueOrDefault2 / 10;
				int num3 = valueOrDefault2 % 10;
				bool flag4 = num2 >= 8;
				string gain = UILoc.KF("stat_quality", 15);
				AddUpgLine(val, "nm_purifier_filter", UILoc.KF("req", UILoc.K("mat_filter_adv"), 1), flag4 ? UILoc.KF("maxed", num2, "8") : (num2 + "/8"), gain, flag4, num2 > 0, prereq: false);
				AddUpgLine(val, "nm_purifier_filter", UILoc.KF("req", UILoc.K("mat_filter"), 10), num3 + "/" + 10, gain, maxed: false, num3 > 0, prereq: false);
			}
			if (text == "wine_rack")
			{
				TryRestoreWineRackWidth(machineObj);
				int valueOrDefault3 = GameItemHelper.GetIntTag(machineObj, "MOD_WINERACK_WINE_WIDTH").GetValueOrDefault();
				int num4 = 6;
				bool flag5 = valueOrDefault3 >= num4;
				AddUpgLine(val, "nm_winrack_ingot", UILoc.KF("req", UILoc.K("mat_ingot"), 1), flag5 ? UILoc.KF("maxed", valueOrDefault3, num4.ToString()) : (valueOrDefault3 + "/" + num4), UILoc.K("stat_bottle_width"), flag5, valueOrDefault3 > 0, prereq: false);
			}
			if (text == "projector" || text == "mirage_projector")
			{
				AddUpgLine(val, "nm_projector_ingot", UILoc.KF("req", UILoc.K("mat_ingot"), 1), null, UILoc.KF("stat_quality", 5), maxed: false, started: false, prereq: false);
			}
			if (text == "recharger")
			{
				AppendRechargerTooltipLines(val, machineObj);
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[Tooltip] append err " + ex.Message);
		}
	}

	private static void AddUpgLine(RichTextBuilder b, string nameKey, string req, string progress, string gain, bool maxed, bool started, bool prereq)
	{
		//IL_0077: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		string text = UILoc.K(nameKey);
		string text2 = (string.IsNullOrEmpty(progress) ? UILoc.KF("upg_nop", text, req, gain) : UILoc.KF("upg", text, req, progress, gain));
		ColorPalette val = (ColorPalette)(prereq ? 4365501 : (maxed ? 8950922 : (started ? 4367018 : 4365501)));
		b.AddLine(text2, true, val, false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
	}

	private static string GainText(BonusDef d)
	{
		List<string> list = new List<string>();
		if (d.QualityPerUse > 0)
		{
			list.Add(UILoc.KF("stat_quality", d.QualityPerUse));
		}
		if (d.PerformancePerUse > 0)
		{
			list.Add(UILoc.KF("stat_performance", d.PerformancePerUse));
		}
		if (d.PowerPerUse > 0)
		{
			list.Add(UILoc.KF("stat_eff", d.PowerPerUse));
		}
		if (d.PowerPerUse < 0)
		{
			list.Add(UILoc.KF("stat_powercost", -d.PowerPerUse));
		}
		return string.Join(UILoc.JoinText, list);
	}

	private static string CapTextLocalized(BonusDef def, int cap)
	{
		if (def.CapTag != null)
		{
			return (cap > 0) ? cap.ToString() : UILoc.K("cap_circuit");
		}
		return (def.MaxUses >= int.MaxValue) ? "∞" : def.MaxUses.ToString();
	}

	public static void PatchRecharger(Harmony harmony)
	{
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Expected Obj, but got Unknown
		_rechargerHarmony = harmony;
		if (_rechargerPatched)
		{
			return;
		}
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (assembly.GetName().Name != "Assembly-CSharp")
				{
					continue;
				}
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if ((type.Name.IndexOf("MachineRecharger", StringComparison.Ordinal) < 0 && (type.FullName == null || type.FullName.IndexOf("MachineRecharger", StringComparison.Ordinal) < 0)) || type.Name.IndexOf("c__DisplayClass1_0", StringComparison.Ordinal) < 0)
					{
						continue;
					}
					MethodInfo methodInfo = null;
					MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (MethodInfo methodInfo2 in methods)
					{
						if (methodInfo2.Name.IndexOf("b__1", StringComparison.Ordinal) >= 0)
						{
							methodInfo = methodInfo2;
							break;
						}
					}
					if (methodInfo == null)
					{
						LogRechargerMethodsOnce(type);
						continue;
					}
					MethodInfo method = typeof(MachineUpgrade).GetMethod("RechargerCycleEndPostfix", BindingFlags.Static | BindingFlags.NonPublic);
					harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					_rechargerCycleEnd = methodInfo;
					_rechargerPatched = true;
					ModLog.Info("[Recharger] patched cycle-end charge " + type.Name + "::" + methodInfo.Name);
					break;
				}
				if (_rechargerPatched)
				{
					break;
				}
			}
			if (!_rechargerPatched)
			{
				ModLog.Warning("[Recharger] MachineRecharger cycle-end method not found (diagnostic only)");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warning("[Recharger] patch err " + ex.Message);
		}
		TryPatchPowerCharge(harmony);
	}

	private static void TryPatchPowerCharge(Harmony harmony)
	{
		//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ff: Expected Obj, but got Unknown
		if (_powerChargePatched)
		{
			return;
		}
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (assembly.GetName().Name != "Assembly-CSharp")
				{
					continue;
				}
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.Name != "PowerHelper")
					{
						continue;
					}
					MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Static | BindingFlags.Public);
					foreach (MethodInfo methodInfo in methods)
					{
						if (!(methodInfo.Name != "ChargeBattery"))
						{
							ParameterInfo[] parameters = methodInfo.GetParameters();
							if (parameters.Length == 2)
							{
								MethodInfo method = typeof(MachineUpgrade).GetMethod("PowerChargePostfix", BindingFlags.Static | BindingFlags.NonPublic);
								harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
								_powerChargePatched = true;
								ModLog.Info($"[Recharger] patched PowerHelper.ChargeBattery({parameters[0].ParameterType.Name}, {parameters[1].ParameterType.Name})");
								return;
							}
						}
					}
				}
			}
			ModLog.Warning("[Recharger] PowerHelper.ChargeBattery(GameItem,int) not found; 快充翻倍禁用");
		}
		catch (Exception ex)
		{
			ModLog.Warning("[Recharger] patch power charge err " + ex.Message);
		}
	}

	private static void RechargerCycleEndPostfix(object __instance)
	{
		if (__instance == null)
		{
			return;
		}
		object rechargerField = GetRechargerField(__instance, "machine");
		if (rechargerField == null)
		{
			return;
		}
		try
		{
			string text = GameItemHelper.TryGetItemId(rechargerField);
			if (!(text != "recharger"))
			{
				int valueOrDefault = GameItemHelper.GetIntTag(rechargerField, "MOD_RECHARGER_FAST_CHARGE").GetValueOrDefault();
				LogRechargerTick(rechargerField, valueOrDefault);
			}
		}
		catch
		{
		}
	}

	private static void LogRechargerTick(object machine, int fast)
	{
		try
		{
			long num = Key(machine);
			if (num != 0 && (!RechargerTickLog.TryGetValue(num, out var value) || !((DateTime.UtcNow - value).TotalSeconds < 5.0)))
			{
				RechargerTickLog[num] = DateTime.UtcNow;
				ModLog.Info($"[Recharger] cycle-end tick machine#{num} fast={fast}");
			}
		}
		catch
		{
		}
	}

	private static void PowerChargePostfix(object __0, int __1)
	{
		if (_powerChargeInDouble || __0 == null || __1 <= 0)
		{
			return;
		}
		try
		{
			object obj = FindRechargerContainingBattery(__0);
			string text = GameItemHelper.TryGetItemId(__0);
			if (obj == null)
			{
				string key = text ?? "?";
				bool flag = false;
				if (!PowerChargeLog.TryGetValue(key, out var value) || (DateTime.UtcNow - value).TotalSeconds >= 30.0)
				{
					PowerChargeLog[key] = DateTime.UtcNow;
					ModLog.Info($"[Recharger] PowerCharge battery='{text}' amount={__1} (not in a recharger)");
				}
				return;
			}
			string text2 = GameItemHelper.TryGetItemId(obj);
			if (text2 != "recharger")
			{
				return;
			}
			int valueOrDefault = GameItemHelper.GetIntTag(obj, "MOD_RECHARGER_FAST_CHARGE").GetValueOrDefault();
			if (valueOrDefault < 1)
			{
				string key2 = "inRch|" + (text ?? "?");
				bool flag2 = false;
				if (!PowerChargeLog.TryGetValue(key2, out var value2) || (DateTime.UtcNow - value2).TotalSeconds >= 30.0)
				{
					PowerChargeLog[key2] = DateTime.UtcNow;
					ModLog.Info($"[Recharger] PowerCharge battery='{text}' amount={__1} (in recharger, fast未装)");
				}
				return;
			}
			_powerChargeInDouble = true;
			try
			{
				PowerHelper.ChargeBattery((GameItem)((__0 is GameItem) ? __0 : null), __1);
				ModLog.Info($"[Recharger] 快充生效: battery='{text}' +{__1} 补到 {2 * __1}(机内快充)");
			}
			finally
			{
				_powerChargeInDouble = false;
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[Recharger] power-charge postfix err " + ex.GetType().Name + ": " + ex.Message);
		}
	}

	private static object FindRechargerContainingBattery(object battery)
	{
		try
		{
			object obj = ReadParentInventory(battery);
			if (obj == null)
			{
				return null;
			}
			Type type = obj.GetType();
			return type.GetMethod("GetParentItem", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.Invoke(obj, null);
		}
		catch
		{
			return null;
		}
	}

	private static object ReadParentInventory(object item)
	{
		try
		{
			Type type = item.GetType();
			string[] array = new string[4] { "parentInventory", "_parentInventory", "_parentInventory_k__BackingField", "parent" };
			foreach (string text in array)
			{
				PropertyInfo property = type.GetProperty(text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null && property.GetIndexParameters().Length == 0)
				{
					try
					{
						object value = property.GetValue(item, null);
						if (value != null)
						{
							return value;
						}
					}
					catch
					{
					}
				}
				FieldInfo field = type.GetField(text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null)
				{
					try
					{
						object value2 = field.GetValue(item);
						if (value2 != null)
						{
							return value2;
						}
					}
					catch
					{
					}
				}
				MethodInfo method = type.GetMethod("get_" + text, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (!(method != null) || method.GetParameters().Length != 0)
				{
					continue;
				}
				try
				{
					object obj3 = method.Invoke(item, null);
					if (obj3 != null)
					{
						return obj3;
					}
				}
				catch
				{
				}
			}
			MethodInfo method2 = type.GetMethod("get_parents", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			if (method2 == null)
			{
				return null;
			}
			object obj5 = method2.Invoke(item, null);
			if (obj5 == null)
			{
				return null;
			}
			foreach (object item2 in TryEnumerate(obj5))
			{
				if (item2 != null)
				{
					string name = item2.GetType().Name;
					if (name.IndexOf("Inventory", StringComparison.Ordinal) >= 0)
					{
						return item2;
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}

	private static object GetRechargerField(object closure, string name)
	{
		try
		{
			return closure.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(closure);
		}
		catch
		{
			return null;
		}
	}

	private static void LogRechargerMethodsOnce(Type t)
	{
		if (_rechargerMethodsLogged)
		{
			return;
		}
		_rechargerMethodsLogged = true;
		try
		{
			StringBuilder stringBuilder = new StringBuilder("[Recharger] methods on " + t.FullName + ": ");
			MethodInfo[] methods = t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo in methods)
			{
				stringBuilder.Append(methodInfo.Name).Append('(').Append(methodInfo.GetParameters().Length)
					.Append(") ");
			}
			ModLog.Info(stringBuilder.ToString());
		}
		catch
		{
		}
	}

	private static bool IsRechargerMaterial(string matId)
	{
		return MatchesMaterial("printer_plastic", matId) || MatchesMaterial("electronic", matId) || MatchesMaterial("wire", matId);
	}

	private static bool HandleRechargerDrop(object machine, object material, string mId, string matId)
	{
		if (mId != "recharger")
		{
			return false;
		}
		if (!_rechargerPatched && _rechargerHarmony != null)
		{
			PatchRecharger(_rechargerHarmony);
		}
		bool flag = MatchesMaterial("printer_plastic", matId);
		bool flag2 = MatchesMaterial("electronic", matId);
		bool flag3 = MatchesMaterial("wire", matId);
		if (!flag && !flag2 && !flag3)
		{
			return false;
		}
		if (!IsItemOwned(material) || !IsItemOwned(machine))
		{
			string item = mId + "|" + matId + "#owned";
			if (RechargerLogged.Add(item))
			{
				ModLog.Info($"[Upgrade] blocked (not owned) machine='{mId}' material='{matId}'");
			}
			return true;
		}
		if (flag)
		{
			HandleRechargerCapPart(machine, material, matId);
		}
		else
		{
			HandleRechargerFastPart(machine, material, matId, flag2);
		}
		return true;
	}

	private static void HandleRechargerCapPart(object machine, object material, string matId)
	{
		if (GameItemHelper.GetIntTag(machine, "MOD_RECHARGER_CAPACITY").GetValueOrDefault() >= 1)
		{
			if (RechargerLogged.Add("recharger|3d|full"))
			{
				ModLog.Info("[Upgrade] recharger 扩容已完成; 3D打印材料不再消耗");
			}
			return;
		}
		int valueOrDefault = GameItemHelper.GetIntTag(machine, "MOD_RECHARGER_3D_PARTS").GetValueOrDefault();
		if (valueOrDefault >= 4)
		{
			if (RechargerLogged.Add("recharger|3d|pending"))
			{
				ModLog.Info("[Upgrade] recharger 扩容待执行(见上方 resize 失败), 请重开窗口重试");
			}
			return;
		}
		valueOrDefault++;
		SetIntTag(machine, "MOD_RECHARGER_3D_PARTS", valueOrDefault);
		ConsumeOne(material);
		if (valueOrDefault >= 4)
		{
			if (ResizeRechargerGrid(machine, out var _))
			{
				SetIntTag(machine, "MOD_RECHARGER_CAPACITY", 1);
				SetIntTag(machine, "MOD_RECHARGER_3D_PARTS", 0);
				PlaySuccessSfx();
				ModLog.Info($"[Upgrade] '{matId}'->recharger 扩容完成: 电池格 4x{2}->4x{4}");
			}
			else
			{
				ModLog.Info("[Upgrade] recharger 扩容失败(grid 未找到), 部件保留, 稍后自动重试");
			}
		}
		else
		{
			ModLog.Info($"[Upgrade] '{matId}'->recharger 扩容材料 {valueOrDefault}/{4}");
		}
	}

	private static void HandleRechargerFastPart(object machine, object material, string matId, bool isCircuit)
	{
		if (GameItemHelper.GetIntTag(machine, "MOD_RECHARGER_FAST_CHARGE").GetValueOrDefault() >= 1)
		{
			if (RechargerLogged.Add("recharger|fast|full"))
			{
				ModLog.Info("[Upgrade] recharger 快充已安装; 电路板/线缆不再消耗");
			}
			return;
		}
		int num = GameItemHelper.GetIntTag(machine, "MOD_RECHARGER_CIRCUITS").GetValueOrDefault();
		int num2 = GameItemHelper.GetIntTag(machine, "MOD_RECHARGER_WIRES").GetValueOrDefault();
		if (isCircuit)
		{
			if (num >= 1)
			{
				if (RechargerLogged.Add("recharger|circuit|have"))
				{
					ModLog.Info("[Upgrade] recharger 电路板已够(1); 再拖电路板不消耗");
				}
				return;
			}
			num = 1;
			SetIntTag(machine, "MOD_RECHARGER_CIRCUITS", num);
			ConsumeOne(material);
		}
		else
		{
			if (num2 >= 2)
			{
				if (RechargerLogged.Add("recharger|wire|have"))
				{
					ModLog.Info("[Upgrade] recharger 线缆已够(2); 再拖线缆不消耗");
				}
				return;
			}
			num2++;
			SetIntTag(machine, "MOD_RECHARGER_WIRES", num2);
			ConsumeOne(material);
		}
		if (num >= 1 && num2 >= 2)
		{
			SetIntTag(machine, "MOD_RECHARGER_FAST_CHARGE", 1);
			SetIntTag(machine, "MOD_RECHARGER_CIRCUITS", 0);
			SetIntTag(machine, "MOD_RECHARGER_WIRES", 0);
			PlaySuccessSfx();
			ModLog.Info("[Upgrade] '" + matId + "'->recharger 快充完成(电路板1+线缆2): 有电时过夜充电翻倍");
		}
		else
		{
			ModLog.Info($"[Upgrade] '{matId}'->recharger 快充材料 电路板 {num}/1, 线缆 {num2}/2");
		}
	}

	private static void TryRestoreRechargerGrid(object machine)
	{
		if (machine == null)
		{
			return;
		}
		try
		{
			string text = GameItemHelper.TryGetItemId(machine);
			if (text != "recharger")
			{
				return;
			}
			bool flag = GameItemHelper.GetIntTag(machine, "MOD_RECHARGER_CAPACITY").GetValueOrDefault() >= 1;
			int valueOrDefault = GameItemHelper.GetIntTag(machine, "MOD_RECHARGER_3D_PARTS").GetValueOrDefault();
			if ((flag || valueOrDefault >= 4) && ResizeRechargerGrid(machine, out var changed))
			{
				if (!flag)
				{
					SetIntTag(machine, "MOD_RECHARGER_CAPACITY", 1);
					SetIntTag(machine, "MOD_RECHARGER_3D_PARTS", 0);
				}
				if (changed && RechargerLogged.Add("restore|4x4"))
				{
					ModLog.Info("[Recharger] 电池格已恢复/应用 4x4");
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Info("[Recharger] restore err " + ex.Message);
		}
	}

	private static bool ResizeRechargerGrid(object machine, out bool changed)
	{
		changed = false;
		try
		{
			GameItem val = (GameItem)((machine is GameItem) ? machine : null);
			if (val == null)
			{
				return false;
			}
			PixelWindow contentWindow = val.contentWindow;
			if (contentWindow == null)
			{
				return false;
			}
			object obj = contentWindow.childElement;
			if (obj == null)
			{
				obj = GetMemberValue(contentWindow, "child");
			}
			object obj2 = TryCast(obj, "GridPixelElement");
			if (obj2 == null)
			{
				ModLog.Info("[Recharger] contentWindow child cast to GridPixelElement failed");
				return false;
			}
			MethodInfo methodInfo = Find2IntMethod(obj2, "GetElement");
			if (methodInfo == null)
			{
				ModLog.Info("[Recharger] GridPixelElement has no GetElement");
				return false;
			}
			List<object> list = new List<object>();
			for (int i = 0; i <= 3; i++)
			{
				for (int j = 0; j <= 6; j++)
				{
					object obj3;
					try
					{
						obj3 = methodInfo.Invoke(obj2, new object[2] { j, i });
					}
					catch
					{
						continue;
					}
					if (obj3 != null)
					{
						object obj5 = TryCast(obj3, "GameGridInventory");
						if (obj5 != null)
						{
							list.Add(obj5);
						}
					}
				}
			}
			if (list.Count == 0)
			{
				ModLog.Info("[Recharger] no GameGridInventory found under window");
				return false;
			}
			object obj6 = list[0];
			int num = ShapeDim(obj6, wantWidth: true);
			int num2 = ShapeDim(obj6, wantWidth: false);
			if (num <= 0)
			{
				num = 4;
			}
			if (num2 >= 4)
			{
				return true;
			}
			int num3 = ((num2 >= 2) ? 4 : (num2 + 2));
			int heightPixels = contentWindow.heightPixels;
			InvokeMethod(obj6, "SetShape", num, num3);
			contentWindow.Validate();
			if (contentWindow.heightPixels <= heightPixels)
			{
				int intProp = GetIntProp(obj6, "heightPixels");
				int num4 = ((intProp > 0 && num3 > 0) ? Math.Max(1, intProp / num3) : 20);
				contentWindow.ResizePixels(contentWindow.widthPixels, heightPixels + num4 * (num3 - num2));
				contentWindow.Validate();
			}
			changed = true;
			return true;
		}
		catch (Exception ex)
		{
			ModLog.Info("[Recharger] resize err " + ex.GetType().Name + ": " + ex.Message);
			return false;
		}
	}

	private static void AppendRechargerTooltipLines(RichTextBuilder b, object machineObj)
	{
		TryRestoreRechargerGrid(machineObj);
		bool flag = GameItemHelper.GetIntTag(machineObj, "MOD_RECHARGER_CAPACITY").GetValueOrDefault() >= 1;
		int valueOrDefault = GameItemHelper.GetIntTag(machineObj, "MOD_RECHARGER_3D_PARTS").GetValueOrDefault();
		bool flag2 = GameItemHelper.GetIntTag(machineObj, "MOD_RECHARGER_FAST_CHARGE").GetValueOrDefault() >= 1;
		int valueOrDefault2 = GameItemHelper.GetIntTag(machineObj, "MOD_RECHARGER_CIRCUITS").GetValueOrDefault();
		int valueOrDefault3 = GameItemHelper.GetIntTag(machineObj, "MOD_RECHARGER_WIRES").GetValueOrDefault();
		AddUpgLine(b, "nm_recharger_cap", UILoc.KF("req", UILoc.K("mat_plastic"), 4), flag ? UILoc.KF("maxed", 4, 4.ToString()) : (valueOrDefault + "/" + 4), UILoc.K("stat_battery_bay"), flag, valueOrDefault > 0, prereq: false);
		AddUpgLine(b, "nm_recharger_fast", UILoc.KF("req2", UILoc.K("mat_circuit"), 1, UILoc.K("mat_wire"), 2), flag2 ? UILoc.K("installed") : (valueOrDefault2 + "/1 + " + valueOrDefault3 + "/2"), UILoc.K("stat_fast_charge"), flag2, valueOrDefault2 > 0 || valueOrDefault3 > 0, prereq: false);
	}

	public static void PatchRechargerDisplay(Harmony harmony)
	{
		if (!_rechargerDisplayPatched)
		{
			_rechargerDisplayPatched = true;
			PatchRechargerRenderCallback(harmony);
			PatchGridChildItemsGetter(harmony);
		}
	}

	private static void PatchRechargerRenderCallback(Harmony harmony)
	{
		//IL_016e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Unknown result type (might be due to invalid IL or missing references)
		//IL_0182: Expected Obj, but got Unknown
		//IL_0182: Expected Obj, but got Unknown
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (assembly.GetName().Name != "Assembly-CSharp")
				{
					continue;
				}
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if ((type.Name.IndexOf("MachineRecharger", StringComparison.Ordinal) < 0 && (type.FullName == null || type.FullName.IndexOf("MachineRecharger", StringComparison.Ordinal) < 0)) || (type.Name.IndexOf("c__DisplayClass1_0", StringComparison.Ordinal) < 0 && (type.FullName == null || type.FullName.IndexOf("c__DisplayClass1_0", StringComparison.Ordinal) < 0)))
					{
						continue;
					}
					MethodInfo methodInfo = null;
					MethodInfo[] methods = type.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
					foreach (MethodInfo methodInfo2 in methods)
					{
						if (methodInfo2.Name.IndexOf("b__4", StringComparison.Ordinal) >= 0)
						{
							methodInfo = methodInfo2;
							break;
						}
					}
					if (!(methodInfo == null))
					{
						MethodInfo method = typeof(MachineUpgrade).GetMethod("RechargerRenderPrefix", BindingFlags.Static | BindingFlags.NonPublic);
						MethodInfo method2 = typeof(MachineUpgrade).GetMethod("RechargerRenderPostfix", BindingFlags.Static | BindingFlags.NonPublic);
						harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
						ModLog.Info("[Recharger] patched outside-display render " + type.Name + "::" + methodInfo.Name);
						return;
					}
				}
			}
			ModLog.Warning("[Recharger] outside-display render b__4 not found; display not limited");
		}
		catch (Exception ex)
		{
			ModLog.Warning("[Recharger] display render patch err " + ex.Message);
		}
	}

	private static void PatchGridChildItemsGetter(Harmony harmony)
	{
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Expected Obj, but got Unknown
		try
		{
			Type type = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (!(assembly.GetName().Name != "Assembly-CSharp"))
				{
					type = assembly.GetType("Il2Cpp.GameGridInventory") ?? assembly.GetType("GameGridInventory");
					if (type != null)
					{
						break;
					}
				}
			}
			if (type == null)
			{
				ModLog.Warning("[Recharger] GameGridInventory not found; display not limited");
				return;
			}
			MethodInfo methodInfo = null;
			MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
			foreach (MethodInfo methodInfo2 in methods)
			{
				if (methodInfo2.Name == "get_childItems" && methodInfo2.GetParameters().Length == 0)
				{
					methodInfo = methodInfo2;
					break;
				}
			}
			if (methodInfo == null)
			{
				ModLog.Warning("[Recharger] get_childItems not found; display not limited");
				return;
			}
			MethodInfo method = typeof(MachineUpgrade).GetMethod("FilterChildItemsPostfix", BindingFlags.Static | BindingFlags.NonPublic);
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			ModLog.Info("[Recharger] patched GameGridInventory.get_childItems for outside display");
		}
		catch (Exception ex)
		{
			ModLog.Warning("[Recharger] childItems patch err " + ex.Message);
		}
	}

	private static void RechargerRenderPrefix()
	{
		_inRechargerRender = true;
	}

	private static void RechargerRenderPostfix()
	{
		_inRechargerRender = false;
	}

	private static void FilterChildItemsPostfix(GameGridInventory __instance, ref List<GameItem> __result)
	{
		if (!_inRechargerRender || __result == null)
		{
			return;
		}
		try
		{
			if (__result.Count <= 4)
			{
				return;
			}
			List<GameItem> list = PickDisplayBatteries(__result);
			List<GameItem> val = new List<GameItem>();
			foreach (GameItem item in list)
			{
				val.Add(item);
			}
			__result = val;
		}
		catch (Exception ex)
		{
			ModLog.Info("[Recharger] display filter err " + ex.Message);
		}
	}

	private static List<GameItem> PickDisplayBatteries(List<GameItem> items)
	{
		List<GameItem> list = new List<GameItem>();
		for (int i = 0; i < items.Count; i++)
		{
			list.Add(items[i]);
		}
		list.Sort((GameItem a, GameItem b) => ChargeOf(b).CompareTo(ChargeOf(a)));
		if (list.Count <= 4)
		{
			return list;
		}
		return new List<GameItem>
		{
			list[0],
			list[1],
			list[list.Count - 2],
			list[list.Count - 1]
		};
	}

	private static int ChargeOf(GameItem item)
	{
		try
		{
			return GameItemHelper.GetIntTag(item, "CURRENT_CHARGE_TAG").GetValueOrDefault();
		}
		catch
		{
			return 0;
		}
	}
}
public static class ModLog
{
	public static void Info(string message)
	{
		MelonLogger.Msg(message);
	}

	public static void Warning(string message)
	{
		MelonLogger.Warning(message);
	}
}
public class MoreDeviceUpgrades : MelonMod
{
	public override void OnInitializeMelon()
	{
		//IL_0011: Unknown result type (might be due to invalid IL or missing references)
		//IL_0017: Expected Obj, but got Unknown
		ModLog.Info("Loading More Device Upgrades v0.2.0 (MelonLoader/IL2CPP)");
		Harmony harmony = new Harmony("com.mods.moredeviceupgrades");
		DropDiagnostics.Init(harmony);
		GameItemHelper.LogApis();
		ModLog.Info($"UI language: code='{UILoc.Code}' -> {UILoc.Lang}");
		ModLog.Info("More Device Upgrades loaded!");
	}
}
public static class TooltipUpgrade
{
	public static void Patch(Harmony harmony)
	{
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0077: Expected Obj, but got Unknown
		try
		{
			Type type = FindType("ModHook");
			if (type == null)
			{
				ModLog.Info("[Tooltip] ModHook type not found; tooltip lines disabled");
				return;
			}
			MethodInfo methodInfo = AccessTools.Method(type, "FireOnCreateTooltipLate", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				ModLog.Info("[Tooltip] ModHook.FireOnCreateTooltipLate not found; tooltip lines disabled");
				return;
			}
			MethodInfo method = typeof(TooltipUpgrade).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			ModLog.Info("[Tooltip] patched ModHook.FireOnCreateTooltipLate");
		}
		catch (Exception ex)
		{
			ModLog.Warning("[Tooltip] patch failed: " + ex.Message);
		}
	}

	private static void Postfix(object __0, object __1)
	{
		if (__1 == null)
		{
			return;
		}
		try
		{
			MachineUpgrade.AppendTooltipLines(__0, __1);
		}
		catch (Exception ex)
		{
			ModLog.Warning("[Tooltip] postfix err " + ex.Message);
		}
	}

	private static Type FindType(string name)
	{
		Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
		foreach (Assembly assembly in assemblies)
		{
			if (assembly.GetName().Name != "Assembly-CSharp")
			{
				continue;
			}
			try
			{
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (type.Name == name || type.FullName == name)
					{
						return type;
					}
				}
			}
			catch
			{
			}
		}
		return null;
	}
}
public enum UILanguage
{
	En,
	Zh,
	Fr,
	De,
	It,
	Ja,
	Ko,
	Pt,
	Ru,
	Es
}
public static class UILoc
{
	private static bool _resolved;

	private static UILanguage _lang = UILanguage.En;

	private static DateTime _resolvedAt;

	private static readonly Dictionary<string, string[]> _table = new Dictionary<string, string[]>
	{
		["mat_ingot"] = Row("Metal Ingot", "金属锭", "Metal Ingot", "Metallbarren", "Metal Ingot", "インゴット", "Metal Ingot", "Metal Ingot", "Слиток металла", "Metal Ingot"),
		["mat_circuit"] = Row("Electronic Parts", "电子元件", "Electronic Parts", "Elektronikteile", "Electronic Parts", "電子部品", "Electronic Parts", "Electronic Parts", "Электронные компоненты", "Electronic Parts"),
		["mat_wire"] = Row("Wire", "线缆", "Wire", "Kabel", "Wire", "ワイヤー", "Wire", "Wire", "Провода", "Wire"),
		["mat_plastic"] = Row("3D Printing Filament", "3D打印耗材", "3D Printing Filament", "3D-Druck-Filament", "3D Printing Filament", "3Dプリント用フィラメント", "3D Printing Filament", "3D Printing Filament", "Нить для 3D-принтера", "3D Printing Filament"),
		["mat_filter_adv"] = Row("Advanced Water Filter", "高级滤水器", "Advanced Water Filter", "Fortschrittlicher Wasserfilter", "Advanced Water Filter", "高度なろ過器", "Advanced Water Filter", "Advanced Water Filter", "Продвинутый фильтр для воды", "Advanced Water Filter"),
		["mat_filter"] = Row("Water Filter", "滤水器", "Water Filter", "Wasserfilter", "Water Filter", "ろ過器", "Water Filter", "Water Filter", "Фильтр для воды", "Water Filter"),
		["nm_furnace_ingot"] = Row("Add Refractory Lining", "增加保温炉衬", "Ajouter un revêtement réfractaire", "Feuerfeste Auskleidung anbringen", "Aggiungi rivestimento refrattario", "耐火ライニングを追加", "?? ??? ??", "Adicionar revestimento refratário", "Добавить огнеупорную футеровку", "A?adir revestimiento refractario"),
		["nm_furnace_circuit"] = Row("Add Smelting Temperature Sensor", "增加冶炼温度传感器", "Ajouter un capteur de température de fusion", "Schmelztemperatursensor einbauen", "Aggiungi sensore temperatura di fusione", "溶解温度センサーを追加", "?? ?? ?? ??", "Adicionar sensor de temperatura de fus?o", "Добавить датчик температуры плавки", "A?adir sensor de temperatura de fundición"),
		["nm_moisture_circuit"] = Row("Install Water Quality Probe", "加装水质检测探头", "Installer une sonde de qualité de l'eau", "Wasserqualit?tssonde installieren", "Installa sonda qualità acqua", "水質センサーを設置", "?? ?? ?? ??", "Instalar sonda de qualidade da água", "Установить датчик качества воды", "Instalar sonda de calidad del agua"),
		["nm_moisture_wire"] = Row("Add Condenser Coil", "增加冷凝铜管", "Ajouter un serpentin de condensation", "Kondensatorspule anbringen", "Aggiungi serpentina di condensazione", "凝縮コイルを追加", "?? ?? ??", "Adicionar serpentina de condensa??o", "Добавить конденсаторный змеевик", "A?adir serpentín de condensación"),
		["nm_purifier_ingot"] = Row("Add Metal Pressure Tank", "增加金属加压罐", "Ajouter un réservoir sous pression en métal", "Metall-Drucktank anbringen", "Aggiungi serbatoio in pressione in metallo", "金属加圧タンクを追加", "?? ?? ?? ??", "Adicionar tanque de press?o metálico", "Добавить металлический напорный бак", "A?adir tanque de presión metálico"),
		["nm_winrack_circuit"] = Row("Optimize Control Circuit", "优化控制电路", "Optimiser le circuit de commande", "Steuerungsschaltung optimieren", "Ottimizza circuito di controllo", "制御回路を最適化", "?? ?? ???", "Otimizar circuito de controle", "Оптимизировать схему управления", "Optimizar circuito de control"),
		["nm_winrack_wire"] = Row("Optimize Power Wiring", "优化供电线路", "Optimiser le cablage électrique", "Stromverkabelung optimieren", "Ottimizza cablaggio elettrico", "給電配線を最適化", "?? ?? ???", "Otimizar fia??o elétrica", "Оптимизировать силовую проводку", "Optimizar el cableado eléctrico"),
		["nm_projector_circuit"] = Row("Optimize Imaging Circuit", "优化成像处理电路", "Optimiser le circuit d'imagerie", "Bildverarbeitungsschaltung optimieren", "Ottimizza circuito di imaging", "映像処理回路を最適化", "?? ?? ?? ???", "Otimizar circuito de imagem", "Оптимизировать схему обработки изображения", "Optimizar el circuito de imagen"),
		["nm_projector_plastic"] = Row("Swap Condenser Lens", "更换聚光透镜", "Remplacer la lentille condenseur", "Kondensorlinse austauschen", "Sostituisci lente del condensatore", "集光レンズを交換", "?? ?? ??", "Trocar lente condensadora", "Заменить конденсорную линзу", "Reemplazar la lente condensadora"),
		["nm_projector_wire"] = Row("Upgrade Power Cable", "加粗投影供电线", "Renforcer le cable d'alimentation du projecteur", "Projektor-Stromkabel verst?rken", "Potenziare il cavo di alimentazione del proiettore", "プロジェクターの電源ケーブルを強化", "???? ?? ??? ??", "Refor?ar o cabo de alimenta??o do projetor", "Усилить кабель питания проектора", "Reforzar el cable de alimentación del proyector"),
		["nm_purifier_filter"] = Row("Install High-Capacity Filter", "更换高效滤芯", "Installer un filtre haute capacité", "Hochleistungsfilter einbauen", "Installa filtro ad alta capacità", "大容量フィルターを装着", "??? ?? ??", "Instalar filtro de alta capacidade", "Установить фильтр повышенной ёмкости", "Instalar filtro de alta capacidad"),
		["nm_winrack_ingot"] = Row("Add Metal Bottle Rack", "加装金属置酒架", "Ajouter une étagère à bouteilles en métal", "Metall-Flaschenregal anbringen", "Aggiungi ripiano in metallo per bottiglie", "金属製ボトルラックを追加", "?? ? ?? ??", "Adicionar estante metálica para garrafas", "Добавить металлическую стойку для бутылок", "A?adir estante metálico para botellas"),
		["nm_projector_ingot"] = Row("Add Heat Sink", "加装散热片", "Ajouter un dissipateur thermique", "Kühlk?rper anbringen", "Aggiungi dissipatore di calore", "ヒートシンクを追加", "??? ??", "Adicionar dissipador de calor", "Добавить радиатор охлаждения", "A?adir disipador de calor"),
		["nm_recharger_cap"] = Row("3D-Printed Bay Extension", "打印扩展充电仓", "Extension de chargement imprimée en 3D", "3D-gedruckte Ladebucht-Erweiterung", "Estensione del vano di ricarica stampata in 3D", "3Dプリント拡張充電ベイ", "3D ??? ?? ?? ??", "Extens?o do compartimento impressa em 3D", "Расширение отсека зарядки из 3D-печати", "Extensión de bahía de carga impresa en 3D"),
		["nm_recharger_fast"] = Row("Fast-Charge Circuit", "改装快充电路", "Circuit de charge rapide", "Schnellladeschaltung", "Circuito di ricarica rapida", "急速充電回路", "?? ?? ??", "Circuito de carga rápida", "Цепь быстрой зарядки", "Circuito de carga rápida"),
		["stat_quality"] = Row("Quality +{0}% per use", "质量+{0}%/次", "Qualité +{0}% par utilisation", "Qualit?t +{0}% pro Nutzung", "Qualità +{0}% per uso", "品質 +{0}%/回", "?? +{0}%/?", "Qualidade +{0}% por uso", "Качество +{0}% за раз", "Calidad +{0}% por uso"),
		["stat_performance"] = Row("Performance +{0}% per use", "性能+{0}%/次", "Performance +{0}% par utilisation", "Leistung +{0}% pro Nutzung", "Prestazioni +{0}% per uso", "性能 +{0}%/回", "?? +{0}%/?", "Desempenho +{0}% por uso", "Производительность +{0}% за раз", "Rendimiento +{0}% por uso"),
		["stat_eff"] = Row("Efficiency +{0}% per use", "能效+{0}%/次", "Efficacité +{0}% par utilisation", "Effizienz +{0}% pro Nutzung", "Efficienza +{0}% per uso", "効率 +{0}%/回", "?? +{0}%/?", "Eficiência +{0}% por uso", "Эффективность +{0}% за раз", "Eficiencia +{0}% por uso"),
		["stat_powercost"] = Row("Power Cost +{0}% per use", "能耗+{0}%/次", "Co?t d'énergie +{0}% par utilisation", "Energieverbrauch +{0}% pro Nutzung", "Costo energetico +{0}% per uso", "消費電力 +{0}%/回", "?? ?? +{0}%/?", "Custo de energia +{0}% por uso", "Расход энергии +{0}% за раз", "Coste de energía +{0}% por uso"),
		["drag_num"] = Row("Drag {0}: {1} ({2}, 0/{3})", "拖入 {0}：{1}（{2}，0/{3}）", "Glissez {0} : {1} ({2}, 0/{3})", "Ziehen Sie {0} darauf: {1} ({2}, 0/{3})", "Trascina {0}: {1} ({2}, 0/{3})", "{0}をドラッグ：{1}（{2}、0/{3}）", "{0} ???: {1} ({2}, 0/{3})", "Arraste {0}: {1} ({2}, 0/{3})", "Перетащите {0}: {1} ({2}, 0/{3})", "Arrastra {0}: {1} ({2}, 0/{3})"),
		["drag_cap"] = Row("Drag {0}: {1} ({2}; cap {3})", "拖入 {0}：{1}（{2}；上限 {3}）", "Glissez {0} : {1} ({2} ; plafond {3})", "Ziehen Sie {0} darauf: {1} ({2}; Obergrenze {3})", "Trascina {0}: {1} ({2}; limite {3})", "{0}をドラッグ：{1}（{2}；上限 {3}）", "{0} ???: {1} ({2}; ?? {3})", "Arraste {0}: {1} ({2}; limite {3})", "Перетащите {0}: {1} ({2}; предел {3})", "Arrastra {0}: {1} ({2}; límite {3})"),
		["tpl_count"] = Row("{0}: {1}/{2} ({3})", "{0}：{1}/{2}（{3}）", "{0} : {1}/{2} ({3})", "{0}: {1}/{2} ({3})", "{0}: {1}/{2} ({3})", "{0}：{1}/{2}（{3}）", "{0}: {1}/{2} ({3})", "{0}: {1}/{2} ({3})", "{0}: {1}/{2} ({3})", "{0}: {1}/{2} ({3})"),
		["tpl_max"] = Row("{0}: maxed {1}/{2} ({3})", "{0}：已满 {1}/{2}（{3}）", "{0} : niveau max {1}/{2} ({3})", "{0}: max. {1}/{2} ({3})", "{0}: al massimo {1}/{2} ({3})", "{0}：最大 {1}/{2}（{3}）", "{0}: ?? {1}/{2} ({3})", "{0}: no máximo {1}/{2} ({3})", "{0}: максимум {1}/{2} ({3})", "{0}: máximo {1}/{2} ({3})"),
		["cap_circuit"] = Row("circuits / 2", "电路板数/2", "cartes / 2", "Platinen / 2", "schede / 2", "基板数/2", "?? ?/2", "placas / 2", "платы / 2", "placas / 2"),
		["upg"] = Row("{0} ({1}): {2} ({3})", "{0}（{1}）：{2}（{3}）", "{0} ({1}) : {2} ({3})", "{0} ({1}): {2} ({3})", "{0} ({1}): {2} ({3})", "{0}（{1}）：{2}（{3}）", "{0} ({1}): {2} ({3})", "{0} ({1}): {2} ({3})", "{0} ({1}): {2} ({3})", "{0} ({1}): {2} ({3})"),
		["upg_nop"] = Row("{0} ({1}): {2}", "{0}（{1}）：{2}", "{0} ({1}) : {2}", "{0} ({1}): {2}", "{0} ({1}): {2}", "{0}（{1}）：{2}", "{0} ({1}): {2}", "{0} ({1}): {2}", "{0} ({1}): {2}", "{0} ({1}): {2}"),
		["req"] = Row("uses {0}×{1}", "使用 {0}×{1}", "utilise {0}×{1}", "verwendet {0}×{1}", "usa {0}×{1}", "{0}×{1} を使用", "{0}×{1} ??", "usa {0}×{1}", "использует {0}×{1}", "usa {0}×{1}"),
		["req2"] = Row("uses {0}×{1} + {2}×{3}", "使用 {0}×{1} + {2}×{3}", "utilise {0}×{1} + {2}×{3}", "verwendet {0}×{1} + {2}×{3}", "usa {0}×{1} + {2}×{3}", "{0}×{1} + {2}×{3} を使用", "{0}×{1} + {2}×{3} ??", "usa {0}×{1} + {2}×{3}", "использует {0}×{1} + {2}×{3}", "usa {0}×{1} + {2}×{3}"),
		["maxed"] = Row("maxed {0}/{1}", "已满 {0}/{1}", "niveau max {0}/{1}", "max. {0}/{1}", "al massimo {0}/{1}", "最大 {0}/{1}", "?? {0}/{1}", "no máximo {0}/{1}", "максимум {0}/{1}", "máximo {0}/{1}"),
		["installed"] = Row("installed", "已安装", "installé", "installiert", "installato", "設置済み", "???", "instalado", "установлено", "instalado"),
		["stat_bottle_width"] = Row("bottle row width +1 per use", "酒瓶格宽度+1/次", "largeur du rang de bouteilles +1 par utilisation", "Flaschenreihenbreite +1 pro Nutzung", "larghezza fila bottiglie +1 per uso", "ボトル列幅 +1/回", "? ? ?? +1/?", "largura da fileira de garrafas +1 por uso", "ширина ряда бутылок +1 за раз", "ancho de la fila de botellas +1 por uso"),
		["stat_battery_bay"] = Row("battery grid 4×2→4×4", "电池格 4×2→4×4", "grille de batterie 4×2→4×4", "Batterieraster 4×2→4×4", "griglia batterie 4×2→4×4", "バッテリーグリッド 4×2→4×4", "??? ??? 4×2→4×4", "grade de bateria 4×2→4×4", "сетка батарей 4×2→4×4", "cuadrícula de batería 4×2→4×4"),
		["stat_fast_charge"] = Row("night charge ×2", "过夜充电×2", "charge nocturne ×2", "Nachladen ×2", "ricarica notturna ×2", "夜間充電×2", "?? ??×2", "carga noturna ×2", "ночная зарядка ×2", "carga nocturna ×2"),
		["adv_max"] = Row("Install High-Capacity Filter: maxed {0}/8 (Quality +15% each)", "更换高效滤芯：已满 {0}/8（每个水质+15%）", "Installer un filtre haute capacité : niveau max {0}/8 (Qualité +15% chacun)", "Hochleistungsfilter einbauen: max. {0}/8 (Qualit?t +15% pro Stück)", "Installa filtro ad alta capacità: al massimo {0}/8 (Qualità +15% ciascuno)", "大容量フィルターを装着：最大 {0}/8（各品質+15%）", "??? ?? ??: ?? {0}/8 (?? ?? +15%)", "Instalar filtro de alta capacidade: no máximo {0}/8 (Qualidade +15% cada)", "Установить фильтр повышенной ёмкости: максимум {0}/8 (Качество +15% каждый)", "Instalar filtro de alta capacidad: máximo {0}/8 (+15% de calidad cada uno)"),
		["adv_some"] = Row("Install High-Capacity Filter: {0}/8 (Quality +15% each)", "更换高效滤芯：{0}/8（每个水质+15%）", "Installer un filtre haute capacité : {0}/8 (Qualité +15% chacun)", "Hochleistungsfilter einbauen: {0}/8 (Qualit?t +15% pro Stück)", "Installa filtro ad alta capacità: {0}/8 (Qualità +15% ciascuno)", "大容量フィルターを装着：{0}/8（各品質+15%）", "??? ?? ??: {0}/8 (?? ?? +15%)", "Instalar filtro de alta capacidade: {0}/8 (Qualidade +15% cada)", "Установить фильтр повышенной ёмкости: {0}/8 (Качество +15% каждый)", "Instalar filtro de alta capacidad: {0}/8 (+15% de calidad cada uno)"),
		["adv_none"] = Row("Drag Advanced Water Filter: Install High-Capacity Filter (Quality +15% each, 0/8)", "拖入 高级滤水器：更换高效滤芯（每个水质+15%，0/8）", "Glissez un filtre à eau avancé : installer un filtre haute capacité (Qualité +15% chacun, 0/8)", "Ziehen Sie einen Premium-Wasserfilter hinein: Hochleistungsfilter einbauen (Qualit?t +15% pro Stück, 0/8)", "Trascina un filtro acqua avanzato: installa filtro ad alta capacità (Qualità +15% ciascuno, 0/8)", "高性能浄水フィルターをドラッグ：大容量フィルターを装着（各品質+15%、0/8）", "?? ?? ?? ???: ??? ?? ?? (?? ?? +15%, 0/8)", "Arraste um filtro de água avan?ado: instalar filtro de alta capacidade (Qualidade +15% cada, 0/8)", "Перетащите улучшенный фильтр для воды: установить фильтр повышенной ёмкости (Качество +15% каждый, 0/8)", "Arrastra un filtro de agua avanzado: instalar filtro de alta capacidad (+15% de calidad cada uno, 0/8)"),
		["bank"] = Row("Water Filter: banked {0}/10 (10 = 1 Advanced Filter)", "滤水器：攒 {0}/10（10 个 = 1 个高级滤水器）", "Filtre à eau : {0}/10 en réserve (10 = 1 filtre avancé)", "Wasserfilter: {0}/10 angespart (10 = 1 Premium-Filter)", "Filtro acqua: {0}/10 accumulati (10 = 1 filtro avanzato)", "浄水フィルター：蓄積 {0}/10（10個 = 高性能フィルター1個）", "?? ??: {0}/10 ?? (10 = ?? ?? 1?)", "Filtro de água: {0}/10 acumulados (10 = 1 filtro avan?ado)", "Фильтр для воды: накоплено {0}/10 (10 = 1 улучшенный фильтр)", "Filtro de agua: {0}/10 acumulados (10 = 1 filtro avanzado)"),
		["bank_full"] = Row("Water Filter: high-capacity filter full", "滤水器：高效滤芯已满，不再收取", "Filtre à eau : le filtre haute capacité est plein", "Wasserfilter: Hochleistungsfilter ist voll", "Filtro acqua: filtro ad alta capacità pieno", "浄水フィルター：大容量フィルターは満杯です", "?? ??: ??? ?? ?? ?", "Filtro de água: filtro de alta capacidade cheio", "Фильтр для воды: фильтр повышенной ёмкости заполнен", "Filtro de agua: el filtro de alta capacidad está lleno"),
		["w_max"] = Row("Add Metal Bottle Rack: maxed (bottle row width {0})", "加装金属置酒架：已满（酒瓶格宽度 {0}）", "Ajouter une étagère à bouteilles en métal : max (largeur du rang de bouteilles {0})", "Metall-Flaschenregal anbringen: max. (Flaschenreihenbreite {0})", "Aggiungi ripiano in metallo per bottiglie: al massimo (larghezza fila bottiglie {0})", "金属製ボトルラックを追加：最大（ボトル列幅 {0}）", "?? ? ?? ??: ?? (? ? ?? {0})", "Adicionar estante metálica para garrafas: no máximo (largura da fileira de garrafas {0})", "Добавить металлическую стойку для бутылок: максимум (ширина ряда бутылок {0})", "A?adir estante metálico para botellas: máximo (ancho de la fila de botellas {0})"),
		["w_some"] = Row("Add Metal Bottle Rack: {0}/{1} (bottle row width {2})", "加装金属置酒架：{0}/{1}（酒瓶格宽度 {2}）", "Ajouter une étagère à bouteilles en métal : {0}/{1} (largeur du rang {2})", "Metall-Flaschenregal anbringen: {0}/{1} (Flaschenreihenbreite {2})", "Aggiungi ripiano in metallo per bottiglie: {0}/{1} (larghezza fila {2})", "金属製ボトルラックを追加：{0}/{1}（ボトル列幅 {2}）", "?? ? ?? ??: {0}/{1} (? ? ?? {2})", "Adicionar estante metálica para garrafas: {0}/{1} (largura da fileira {2})", "Добавить металлическую стойку для бутылок: {0}/{1} (ширина ряда {2})", "A?adir estante metálico para botellas: {0}/{1} (ancho de la fila {2})"),
		["w_none"] = Row("Drag Metal Ingot: Add Metal Bottle Rack (+1 bottle column, now {0}×{1})", "拖入 金属锭：加装金属置酒架（酒瓶格宽度+1 列，当前 {0}×{1}）", "Glissez un lingot de métal : ajouter une étagère à bouteilles (+1 colonne, actuellement {0}×{1})", "Ziehen Sie einen Metallbarren hinein: Metall-Flaschenregal anbringen (+1 Flaschenspalte, jetzt {0}×{1})", "Trascina un lingotto di metallo: aggiungi ripiano per bottiglie (+1 colonna, ora {0}×{1})", "金属インゴットをドラッグ：金属製ボトルラックを追加（ボトル列+1、現在 {0}×{1}）", "?? ?? ???: ?? ? ?? ?? (? ? +1, ?? {0}×{1})", "Arraste um lingote de metal: adicionar estante metálica para garrafas (+1 coluna, agora {0}×{1})", "Перетащите металлический слиток: добавить стойку для бутылок (+1 столбец, сейчас {0}×{1})", "Arrastra un lingote de metal: a?adir estante metálico para botellas (+1 columna, ahora {0}×{1})"),
		["combo"] = Row("Metal Ingot: add a heat sink (once each: {0} + {1})", "金属锭：加装散热片（＝「{0}」＋「{1}」各一次）", "Lingot de métal : ajouter un dissipateur thermique (une fois chacun : {0} + {1})", "Metallbarren: Kühlk?rper anbringen (je einmal: {0} + {1})", "Lingotto di metallo: aggiungi dissipatore (una volta ciascuno: {0} + {1})", "金属インゴット：ヒートシンクを追加（各1回：{0}＋{1}）", "?? ??: ??? ?? (? 1?: {0} + {1})", "Lingote de metal: adicionar dissipador de calor (uma vez cada: {0} + {1})", "Металлический слиток: добавить радиатор (по одному разу: {0} + {1})", "Lingote de metal: a?adir disipador de calor (una vez cada uno: {0} + {1})"),
		["rc_cap_max"] = Row("3D-Printed Bay Extension: done (battery grid 4×{0} → 4×{1}, {2} slots)", "打印扩展充电仓：已完成（电池格 4×{0} → 4×{1}，{2} 格）", "Extension de chargement imprimée en 3D : terminée (grille de batterie 4×{0} → 4×{1}, {2} emplacements)", "3D-gedruckte Ladebucht-Erweiterung: fertig (Batterieraster 4×{0} → 4×{1}, {2} F?cher)", "Estensione del vano di ricarica stampata in 3D: completata (griglia batterie 4×{0} → 4×{1}, {2} slot)", "3Dプリント拡張充電ベイ：完了（バッテリーグリッド 4×{0} → 4×{1}、{2} スロット）", "3D ??? ?? ?? ??: ?? (??? ??? 4×{0} → 4×{1}, {2}?)", "Extens?o do compartimento impressa em 3D: concluída (grade de bateria 4×{0} → 4×{1}, {2} espa?os)", "Расширение отсека зарядки из 3D-печати: готово (сетка батарей 4×{0} → 4×{1}, {2} ячеек)", "Extensión de bahía de carga impresa en 3D: completa (cuadrícula de batería 4×{0} → 4×{1}, {2} ranuras)"),
		["rc_cap_part"] = Row("3D-Printed Bay Extension: materials {0}/{1}", "打印扩展充电仓：材料 {0}/{1}", "Extension de chargement imprimée en 3D : matériaux {0}/{1}", "3D-gedruckte Ladebucht-Erweiterung: Material {0}/{1}", "Estensione del vano di ricarica stampata in 3D: materiali {0}/{1}", "3Dプリント拡張充電ベイ：材料 {0}/{1}", "3D ??? ?? ?? ??: ?? {0}/{1}", "Extens?o do compartimento impressa em 3D: materiais {0}/{1}", "Расширение отсека зарядки из 3D-печати: материалы {0}/{1}", "Extensión de bahía de carga impresa en 3D: materiales {0}/{1}"),
		["rc_cap_none"] = Row("Drag Printer Plastic ×{0}: 3D-Printed Bay Extension (battery grid 4×{1} → 4×{2})", "拖入 3D打印材料 ×{0}：打印扩展充电仓（电池格 4×{1} → 4×{2}）", "Glissez du plastique d'impression ×{0} : extension de chargement imprimée en 3D (grille de batterie 4×{1} → 4×{2})", "Ziehen Sie 3D-Druckmaterial ×{0} hinein: 3D-gedruckte Ladebucht-Erweiterung (Batterieraster 4×{1} → 4×{2})", "Trascina plastica per stampa 3D ×{0}: estensione del vano stampata in 3D (griglia batterie 4×{1} → 4×{2})", "3Dプリント素材 ×{0} をドラッグ：3Dプリント拡張充電ベイ（バッテリーグリッド 4×{1} → 4×{2}）", "3D ??? ?? ×{0} ???: 3D ??? ?? ?? ?? (??? ??? 4×{1} → 4×{2})", "Arraste plástico de impressora ×{0}: extens?o do compartimento impressa em 3D (grade de bateria 4×{1} → 4×{2})", "Перетащите пластик для 3D-печати ×{0}: расширение отсека из 3D-печати (сетка батарей 4×{1} → 4×{2})", "Arrastra plástico de impresora ×{0}: extensión de bahía impresa en 3D (cuadrícula de batería 4×{1} → 4×{2})"),
		["rc_fast_max"] = Row("Fast-Charge Circuit: installed (night charge ×2 when powered)", "改装快充电路：已安装（有电时夜间充电翻倍）", "Circuit de charge rapide : installé (charge nocturne ×2 si alimenté)", "Schnellladeschaltung: installiert (nachts ×2 Ladung bei Strom)", "Circuito di ricarica rapida: installato (ricarica notturna ×2 se alimentato)", "急速充電回路：装着済み（電力ありで夜間充電×2）", "?? ?? ??: ??? (?? ?? ? ?? ?? ×2)", "Circuito de carga rápida: instalado (carga noturna ×2 com energia)", "Цепь быстрой зарядки: установлена (ночная зарядка ×2 при питании)", "Circuito de carga rápida: instalado (carga nocturna ×2 con energía)"),
		["rc_fast_part"] = Row("Fast-Charge Circuit: drag 1 {0} + 2 {1} ({0} {2}/1, {1} {3}/2)", "改装快充电路：拖入 {0}×1＋{1}×2（{0} {2}/1，{1} {3}/2）", "Circuit de charge rapide : glissez 1 {0} + 2 {1} ({0} {2}/1, {1} {3}/2)", "Schnellladeschaltung: ziehen Sie 1 {0} + 2 {1} hinein ({0} {2}/1, {1} {3}/2)", "Circuito di ricarica rapida: trascina 1 {0} + 2 {1} ({0} {2}/1, {1} {3}/2)", "急速充電回路：{0}×1＋{1}×2 をドラッグ（{0} {2}/1、{1} {3}/2）", "?? ?? ??: {0}×1 + {1}×2 ??? ({0} {2}/1, {1} {3}/2)", "Circuito de carga rápida: arraste 1 {0} + 2 {1} ({0} {2}/1, {1} {3}/2)", "Цепь быстрой зарядки: перетащите 1 {0} + 2 {1} ({0} {2}/1, {1} {3}/2)", "Circuito de carga rápida: arrastra 1 {0} + 2 {1} ({0} {2}/1, {1} {3}/2)"),
		["rc_fast_none"] = Row("Drag 1 {0} + 2 {1}: Fast-Charge Circuit (night charge ×2 when powered)", "拖入 {0}×1＋{1}×2：改装快充电路（有电时夜间充电翻倍）", "Glissez 1 {0} + 2 {1} : circuit de charge rapide (charge nocturne ×2 si alimenté)", "Ziehen Sie 1 {0} + 2 {1} hinein: Schnellladeschaltung (nachts ×2 Ladung bei Strom)", "Trascina 1 {0} + 2 {1}: circuito di ricarica rapida (ricarica notturna ×2 se alimentato)", "{0}×1＋{1}×2 をドラッグ：急速充電回路（電力ありで夜間充電×2）", "{0}×1 + {1}×2 ???: ?? ?? ?? (?? ?? ? ?? ?? ×2)", "Arraste 1 {0} + 2 {1}: circuito de carga rápida (carga noturna ×2 com energia)", "Перетащите 1 {0} + 2 {1}: цепь быстрой зарядки (ночная зарядка ×2 при питании)", "Arrastra 1 {0} + 2 {1}: circuito de carga rápida (carga nocturna ×2 con energía)")
	};

	public static string Code => GetLocaleCode();

	public static bool IsEnglish => Lang == UILanguage.En;

	public static UILanguage Lang
	{
		get
		{
			if (_resolved && (DateTime.UtcNow - _resolvedAt).TotalSeconds < 5.0)
			{
				return _lang;
			}
			_resolved = true;
			_resolvedAt = DateTime.UtcNow;
			_lang = Detect(GetLocaleCode());
			return _lang;
		}
	}

	public static string JoinText => (Lang == UILanguage.Zh) ? "、" : ", ";

	private static UILanguage Detect(string code)
	{
		if (string.IsNullOrEmpty(code))
		{
			return UILanguage.En;
		}
		string text = code.ToLowerInvariant();
		if (text.IndexOf("zh", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.Zh;
		}
		if (text.IndexOf("en", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.En;
		}
		if (text.IndexOf("fr", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.Fr;
		}
		if (text.IndexOf("de", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.De;
		}
		if (text.IndexOf("it", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.It;
		}
		if (text.IndexOf("ja", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.Ja;
		}
		if (text.IndexOf("ko", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.Ko;
		}
		if (text.IndexOf("pt", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.Pt;
		}
		if (text.IndexOf("ru", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.Ru;
		}
		if (text.IndexOf("es", StringComparison.Ordinal) >= 0)
		{
			return UILanguage.Es;
		}
		return UILanguage.En;
	}

	public static string K(string key)
	{
		return Pick(key, Lang);
	}

	public static string KF(string key, params object[] args)
	{
		string text = Pick(key, Lang);
		try
		{
			return string.Format(text, args);
		}
		catch (Exception)
		{
			return text;
		}
	}

	public static string Z(string key)
	{
		return Pick(key, UILanguage.Zh);
	}

	private static string[] Row(string en, string zh, string fr, string de, string it, string ja, string ko, string pt, string ru, string es)
	{
		return new string[10] { en, zh, fr, de, it, ja, ko, pt, ru, es };
	}

	private static string Pick(string key, UILanguage lang)
	{
		if (!_table.TryGetValue(key, out var value))
		{
			return key;
		}
		string text = value[(int)lang];
		if (string.IsNullOrEmpty(text))
		{
			text = value[0];
		}
		return string.IsNullOrEmpty(text) ? key : text;
	}

	private static string GetLocaleCode()
	{
		try
		{
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			foreach (Assembly assembly in assemblies)
			{
				if (assembly.GetName().Name != "Assembly-CSharp")
				{
					continue;
				}
				Type[] types = assembly.GetTypes();
				foreach (Type type in types)
				{
					if (!(type.Name != "LocHelper"))
					{
						MethodInfo method = type.GetMethod("GetCurrentLocaleCode", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
						if (!(method == null))
						{
							object obj = method.Invoke(null, null);
							return obj as string;
						}
					}
				}
			}
		}
		catch
		{
		}
		return null;
	}
}
public enum UpgradeStat
{
	Quality,
	Efficiency,
	Speed,
	BottleArea,
	ModuleArea
}
public sealed class UpgradeRecipe
{
	public string Machine;

	public string Material;

	public UpgradeStat Stat;

	public float StepPerItem;

	public float MaxTotal;

	public int MaxTimes = 99;
}
public static class UpgradeRecipes
{
	public const string MatCircuit = "electronic";

	public const string MatCopper = "wire";

	public const string Mat3DPrint = "printer_plastic";

	public const string MatChem = "common_chemical_supplies";

	public const string MatMedical = "common_medical_supplies";

	public const string MatIngot = "metal_ingot";

	public const string MatScrap = "scrap_metal";

	public const string MatWaterFilter = "water_filter";

	public const string MatWaterFilterAdv = "water_filter_adv";

	public const string MachineFurnace = "furnace";

	public const string MachinePurifier = "water_purifier";

	public const string MachineMoistureCollector = "moisture_farm";

	public const string MachineAger = "wine_rack";

	public static readonly List<UpgradeRecipe> All = new List<UpgradeRecipe>
	{
		new UpgradeRecipe
		{
			Machine = "furnace",
			Material = "electronic",
			Stat = UpgradeStat.Quality,
			StepPerItem = 0.05f,
			MaxTotal = 0.5f,
			MaxTimes = 10
		},
		new UpgradeRecipe
		{
			Machine = "furnace",
			Material = "wire",
			Stat = UpgradeStat.Efficiency,
			StepPerItem = 0.05f,
			MaxTotal = 0.5f
		},
		new UpgradeRecipe
		{
			Machine = "water_purifier",
			Material = "printer_plastic",
			Stat = UpgradeStat.Speed,
			StepPerItem = 0.1f,
			MaxTotal = 2f
		},
		new UpgradeRecipe
		{
			Machine = "water_purifier",
			Material = "water_filter_adv",
			Stat = UpgradeStat.Quality,
			StepPerItem = 0.15f,
			MaxTotal = 1.2f,
			MaxTimes = 8
		},
		new UpgradeRecipe
		{
			Machine = "water_purifier",
			Material = "water_filter",
			Stat = UpgradeStat.Quality,
			StepPerItem = 0.15f,
			MaxTotal = 1.2f,
			MaxTimes = 0
		},
		new UpgradeRecipe
		{
			Machine = "water_purifier",
			Material = "metal_ingot",
			Stat = UpgradeStat.Speed,
			StepPerItem = 0.05f,
			MaxTotal = 0.5f,
			MaxTimes = 10
		},
		new UpgradeRecipe
		{
			Machine = "water_purifier",
			Material = "common_medical_supplies",
			Stat = UpgradeStat.Quality,
			StepPerItem = 0.2f,
			MaxTotal = 1f
		},
		new UpgradeRecipe
		{
			Machine = "moisture_farm",
			Material = "electronic",
			Stat = UpgradeStat.Quality,
			StepPerItem = 0.07f,
			MaxTotal = 0.7f,
			MaxTimes = 10
		},
		new UpgradeRecipe
		{
			Machine = "moisture_farm",
			Material = "wire",
			Stat = UpgradeStat.Speed,
			StepPerItem = 0.1f,
			MaxTotal = 0.5f,
			MaxTimes = 5
		},
		new UpgradeRecipe
		{
			Machine = "moisture_farm",
			Material = "common_medical_supplies",
			Stat = UpgradeStat.Quality,
			StepPerItem = 0.1f,
			MaxTotal = 1f
		},
		new UpgradeRecipe
		{
			Machine = "wine_rack",
			Material = "electronic",
			Stat = UpgradeStat.Quality,
			StepPerItem = 0.15f,
			MaxTotal = float.MaxValue
		},
		new UpgradeRecipe
		{
			Machine = "wine_rack",
			Material = "wire",
			Stat = UpgradeStat.Efficiency,
			StepPerItem = 0.1f,
			MaxTotal = 0f,
			MaxTimes = 0
		}
	};

	public static UpgradeRecipe Find(string machineId, string materialId)
	{
		foreach (UpgradeRecipe item in All)
		{
			if (item.Machine == machineId && item.Material == materialId)
			{
				return item;
			}
		}
		return null;
	}
}
