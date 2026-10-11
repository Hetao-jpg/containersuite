using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using SciRat;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Mod), "科学养鼠", "1.3.12", "local", null)]
[assembly: MelonGame(null, "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("SciRat")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: AssemblyInformationalVersion("1.1.0")]
[assembly: AssemblyProduct("SciRat")]
[assembly: AssemblyTitle("SciRat")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.1.0.0")]
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
namespace SciRat
{
	internal static class GameSession
	{
		private static int _loadDepth;

		private static bool _storeReady;

		internal static bool IsLoading => _loadDepth > 0;

		internal static bool CanTouchGame
		{
			get
			{
				if (IsLoading || !_storeReady)
				{
					return false;
				}
				try
				{
					return (Object)(object)EmporiumEntry.Instance != (Object)null;
				}
				catch
				{
					return false;
				}
			}
		}

		internal static bool TryGetStore(out PlayerStore store)
		{
			store = null;
			if (!CanTouchGame)
			{
				return false;
			}
			try
			{
				if ((Object)(object)EmporiumEntry.Instance == (Object)null)
				{
					return false;
				}
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					return false;
				}
				store = instance;
				return true;
			}
			catch
			{
				return false;
			}
		}

		internal static void Apply(Harmony harmony)
		{
			try
			{
				harmony.PatchAll(typeof(GameSession));
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat load guard: " + ex.Message);
			}
		}

		private static void EnterLoad()
		{
			_loadDepth++;
			_storeReady = false;
		}

		private static void LeaveLoad()
		{
			if (_loadDepth > 0)
			{
				_loadDepth--;
			}
		}

		private static void MarkStoreReady()
		{
			_storeReady = true;
			GeneProbeBoxItem.ResetEnsureFlag();
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
		private static void PreLoadGame()
		{
			EnterLoad();
		}

		[HarmonyFinalizer]
		[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
		private static Exception? FinLoadGame(Exception? __exception)
		{
			LeaveLoad();
			if (__exception == null)
			{
				MarkStoreReady();
			}
			return __exception;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlayerStore), "StartNewGame")]
		private static void PreStartNewGame()
		{
			EnterLoad();
		}

		[HarmonyFinalizer]
		[HarmonyPatch(typeof(PlayerStore), "StartNewGame")]
		private static Exception? FinStartNewGame(Exception? __exception)
		{
			LeaveLoad();
			if (__exception == null)
			{
				MarkStoreReady();
			}
			return __exception;
		}

		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlayerStore), "DecodeSaveItem")]
		private static void PreDecodeSaveItem()
		{
			EnterLoad();
		}

		[HarmonyFinalizer]
		[HarmonyPatch(typeof(PlayerStore), "DecodeSaveItem")]
		private static Exception? FinDecodeSaveItem(Exception? __exception)
		{
			LeaveLoad();
			return __exception;
		}

		[HarmonyPostfix]
		[HarmonyPatch(typeof(EmporiumEntry), "Start")]
		private static void PostEmporiumStart()
		{
			if (!IsLoading)
			{
				MarkStoreReady();
			}
		}
	}
	internal static class GeneEditLimit
	{
		internal const string EditCountTag = "SCI_RAT_PREGNANCY_GENE_EDITS";

		internal static void Apply(Harmony harmony)
		{
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(GeneticHelper), "ClearPartnerGenes", new Type[2]
				{
					typeof(GameItem),
					typeof(AnimalSpecies)
				}, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning("GeneEditLimit: ClearPartnerGenes 未找到");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(GeneEditLimit), "PostClearPartnerGenes", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("GeneEditLimit: 已挂钩 ClearPartnerGenes（清零孕检编辑计数）");
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneEditLimit Apply: " + ex.Message);
			}
		}

		public static void PostClearPartnerGenes(GameItem female)
		{
			if (female != null)
			{
				ClearEditCount(female);
			}
		}

		internal static int ConfiguredMax()
		{
			try
			{
				int num = Mod.MaxGeneEditsPerPregnancy?.Value ?? 0;
				return (num >= 0) ? num : 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static bool IsUnlimited()
		{
			return ConfiguredMax() <= 0;
		}

		internal static int GetEditCount(GameItem? female)
		{
			if (female == null)
			{
				return 0;
			}
			try
			{
				if (!female.IsTag("SCI_RAT_PREGNANCY_GENE_EDITS"))
				{
					return 0;
				}
				TagState tagReadonly = female.GetTagReadonly("SCI_RAT_PREGNANCY_GENE_EDITS");
				if (tagReadonly == null)
				{
					return 0;
				}
				return Math.Max(0, tagReadonly.valueInt);
			}
			catch
			{
				return 0;
			}
		}

		internal static void ClearEditCount(GameItem female)
		{
			try
			{
				female.DisableTag("SCI_RAT_PREGNANCY_GENE_EDITS", true);
			}
			catch
			{
			}
		}

		internal static bool CanEditMore(GameItem female, out string denyMessage)
		{
			denyMessage = "";
			int num = ConfiguredMax();
			if (num <= 0)
			{
				return true;
			}
			int editCount = GetEditCount(female);
			if (editCount < num)
			{
				return true;
			}
			denyMessage = $"本次怀孕已编辑 {editCount}/{num} 个基因，已达上限（UserData MelonPreferences.cfg → SciRat.MaxGeneEditsPerPregnancy）";
			return false;
		}

		internal static void IncrementEditCount(GameItem female)
		{
			int next = GetEditCount(female) + 1;
			try
			{
				female.ModifyTag("SCI_RAT_PREGNANCY_GENE_EDITS", Action<TagState>.op_Implicit((Action<TagState>)((TagState t) =>
				{
					t.SetInt(next);
					try
					{
						t.Enable();
					}
					catch
					{
					}
				})), true);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneEditLimit Increment: " + ex.Message);
			}
		}

		internal static string StatusLine(GameItem female)
		{
			int editCount = GetEditCount(female);
			int num = ConfiguredMax();
			if (num > 0)
			{
				return $"本次怀孕已编辑 {editCount}/{num} 个基因";
			}
			return $"本次怀孕已编辑 {editCount} 个基因（上限：不限制）";
		}
	}
	internal static class GeneProbeBoxItem
	{
		internal const string ItemId = "gene_edit_probe_box";

		internal const string DisplayName = "基因编辑探针盒";

		private const int InvWidth = 8;

		private const int InvHeight = 2;

		private const int Capacity = 8;

		private const int ProbeW = 1;

		private const int ProbeH = 2;

		private static Func<GameItem>? _factory;

		private static bool _ensuredStore;

		internal static void Apply(Harmony harmony)
		{
			PatchDirectory(harmony, typeof(MiscItemDirectory));
			PatchDirectory(harmony, typeof(ModItemDirectory));
		}

		internal static void TickEnsure()
		{
			if (_ensuredStore || !GameSession.CanTouchGame || !GameSession.TryGetStore(out PlayerStore store))
			{
				return;
			}
			_ensuredStore = true;
			try
			{
				List<GameItem> val = store.FindAllItem(true);
				if (val == null)
				{
					return;
				}
				Enumerator<GameItem> enumerator = val.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					if (IsBox(current))
					{
						EnsureWindow(current, "store-ready");
					}
					else if (GeneProbeItem.IsProbe(current))
					{
						GeneProbeSprites.ApplyProbeToItem(current);
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeBox TickEnsure: " + ex.Message);
			}
		}

		internal static void ResetEnsureFlag()
		{
			_ensuredStore = false;
		}

		private static void PatchDirectory(Harmony harmony, Type directoryType)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Expected Obj, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(directoryType, "InitDirectory", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Mod.Log.Warning("GeneProbeBox: " + directoryType.Name + ".InitDirectory 未找到");
			}
			else
			{
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(GeneProbeBoxItem), "PostfixInitDirectory", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}

		public static void PostfixInitDirectory(ItemDirectory __instance)
		{
			try
			{
				Register(__instance);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeBox register on " + ((object)__instance)?.GetType().Name + ": " + ex.Message);
			}
		}

		private static void Register(ItemDirectory dir)
		{
			if ((Object)(object)dir == (Object)null)
			{
				return;
			}
			try
			{
				if (((Directory<GameItem>)(object)dir).Has("gene_edit_probe_box"))
				{
					return;
				}
			}
			catch
			{
			}
			if ((Delegate)(object)_factory == (Delegate)null)
			{
				Func<GameItem> func = Create;
				try
				{
					_factory = DelegateSupport.ConvertDelegate<Func<GameItem>>((Delegate)func);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("GeneProbeBox ConvertDelegate: " + ex.Message);
					return;
				}
			}
			if ((Delegate)(object)_factory == (Delegate)null)
			{
				return;
			}
			try
			{
				if (((Directory<GameItem>)(object)dir).Add("gene_edit_probe_box", _factory))
				{
					Mod.Log.Msg($"GeneProbeBox: 已注册 {"gene_edit_probe_box"}（{8} 格探针）→ {((object)dir).GetType().Name}");
				}
				else
				{
					Mod.Log.Warning($"GeneProbeBox: Add('{"gene_edit_probe_box"}') 返回 false ({((object)dir).GetType().Name})");
				}
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("GeneProbeBox Add: " + ex2.Message);
			}
		}

		internal static bool IsBox(GameItem? item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				return string.Equals(item.identifier, "gene_edit_probe_box", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		internal static GameItem Create()
		{
			GameItem val = CreateBareShell();
			if (!EnsureWindow(val, "Create", out GameInventory _))
			{
				Mod.Log.Warning("GeneProbeBox Create: 内仓未建好");
			}
			ApplyContainerMeta(val);
			return val;
		}

		internal static bool EnsureWindow(GameItem item, string ctx, bool applyFilter = true)
		{
			GameInventory inv;
			return EnsureWindow(item, ctx, out inv, applyFilter);
		}

		internal static bool EnsureWindow(GameItem item, string ctx, out GameInventory? inv, bool applyFilter = true)
		{
			inv = null;
			if (!IsBox(item))
			{
				return false;
			}
			try
			{
				if (item.contentWindow != null)
				{
					inv = ResolveContentInventory(item);
					if (inv != null && !InventoryMatchesLayout(inv) && !TryRebuildInnerWindow(item, ctx, out inv))
					{
						return false;
					}
					GeneProbeSprites.ApplyBoxToItem(item);
					if (applyFilter && inv != null)
					{
						ApplyProbeFilter(inv, item);
					}
					return true;
				}
			}
			catch
			{
			}
			if (!CreateFreshWindow(item, ctx, out inv, applyFilter: false))
			{
				return false;
			}
			GeneProbeSprites.ApplyBoxToItem(item);
			if (applyFilter && inv != null)
			{
				ApplyProbeFilter(inv, item);
			}
			return true;
		}

		private static bool InventoryMatchesLayout(GameInventory inv)
		{
			try
			{
				GameGridInventory val = ((Il2CppObjectBase)inv).TryCast<GameGridInventory>();
				GridShape val2 = ((val != null) ? val.inventoryShape : null);
				if (val2 == null)
				{
					return false;
				}
				return val2.width == 8 && val2.height == 2;
			}
			catch
			{
				return false;
			}
		}

		private static bool TryRebuildInnerWindow(GameItem box, string ctx, out GameInventory? inv)
		{
			inv = ResolveContentInventory(box);
			List<GameItem> list = new List<GameItem>();
			try
			{
				GameInventory? obj = inv;
				List<GameItem> val = ((obj != null) ? obj.childItems : null);
				if (val != null)
				{
					for (int num = val.Count - 1; num >= 0; num--)
					{
						GameItem val2 = val[num];
						if (val2 != null && GeneProbeItem.IsProbe(val2))
						{
							try
							{
								inv.Expel(val2);
							}
							catch
							{
							}
							list.Add(val2);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeBox[" + ctx + "] 取出旧探针: " + ex.Message);
			}
			try
			{
				box.SetContentWindow((PixelWindow)null);
			}
			catch
			{
				try
				{
					box.contentWindow = null;
				}
				catch
				{
				}
			}
			if (!CreateFreshWindow(box, ctx + "-rebuild", out inv, applyFilter: false))
			{
				foreach (GameItem item in list)
				{
					try
					{
						GameInventory? obj5 = inv;
						if (obj5 != null)
						{
							obj5.UncheckedAccept(item);
						}
					}
					catch
					{
						try
						{
							item.Destroy();
						}
						catch
						{
						}
					}
				}
				return false;
			}
			GameGridInventory val3 = ((Il2CppObjectBase)inv).TryCast<GameGridInventory>();
			int num2 = 0;
			if (val3 != null)
			{
				for (int i = 0; i < 2; i += 2)
				{
					if (num2 >= list.Count)
					{
						break;
					}
					for (int j = 0; j < 8; j++)
					{
						if (num2 >= list.Count)
						{
							break;
						}
						GameItem val4 = list[num2];
						if (TryPlaceProbeAt(val3, val4, j, i))
						{
							num2++;
							continue;
						}
						try
						{
							val4.Destroy();
						}
						catch
						{
						}
						num2++;
					}
				}
			}
			for (int k = num2; k < list.Count; k++)
			{
				try
				{
					list[k].Destroy();
				}
				catch
				{
				}
			}
			ApplyProbeFilter(inv, box);
			Mod.Log.Msg($"GeneProbeBox[{ctx}]: 内仓已重建为 {8}×{2}，迁回 {Math.Min(num2, list.Count)} 根");
			return true;
		}

		private static bool CreateFreshWindow(GameItem item, string ctx, out GameInventory? inv, bool applyFilter)
		{
			inv = null;
			try
			{
				ValueTuple<PixelWindow, GameInventory> val = DirectoryUtils.CreateInventoryWindow(8, 2, true);
				PixelWindow val2 = null;
				try
				{
					val2 = val.Item1;
					inv = val.Item2;
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("GeneProbeBox[" + ctx + "] tuple: " + ex.Message);
					return false;
				}
				if (val2 == null || inv == null)
				{
					Mod.Log.Warning("GeneProbeBox[" + ctx + "] CreateInventoryWindow 返回空");
					return false;
				}
				item.SetContentWindow(val2);
				try
				{
					val2.Validate();
				}
				catch
				{
				}
				Mod.Log.Msg($"GeneProbeBox[{ctx}]: 已建内仓 {8}×{2}（可放 {8} 根探针）");
				return true;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("GeneProbeBox[" + ctx + "] EnsureWindow: " + ex2.Message);
				return false;
			}
		}

		private static GameInventory? ResolveContentInventory(GameItem box)
		{
			try
			{
				PixelWindow contentWindow = box.contentWindow;
				if (contentWindow == null)
				{
					return null;
				}
				PixelElement val = null;
				try
				{
					val = contentWindow.childElement;
				}
				catch
				{
				}
				if (val == null)
				{
					try
					{
						GraphNodeStorage child = contentWindow.child;
						val = ((child != null) ? ((Il2CppObjectBase)child).TryCast<PixelElement>() : null);
					}
					catch
					{
					}
				}
				if (val == null)
				{
					return null;
				}
				GameGridInventory val2 = ((Il2CppObjectBase)val).TryCast<GameGridInventory>();
				if (val2 != null)
				{
					return (GameInventory?)(object)val2;
				}
				GameInventory val3 = ((Il2CppObjectBase)val).TryCast<GameInventory>();
				if (val3 != null)
				{
					return val3;
				}
				GraphNodeStorage val4 = ((Il2CppObjectBase)val).TryCast<GraphNodeStorage>();
				if (val4 != null)
				{
					val2 = ((Il2CppObjectBase)val4).TryCast<GameGridInventory>();
					if (val2 != null)
					{
						return (GameInventory?)(object)val2;
					}
					return ((Il2CppObjectBase)val4).TryCast<GameInventory>();
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeBox ResolveContentInventory: " + ex.Message);
			}
			return null;
		}

		private static void ApplyProbeFilter(GameInventory inv, GameItem box)
		{
			try
			{
				ContainerHelper.AllowOnlyTaggedItemOwn(inv, box, "GENE_EDIT_PROBE_TAG", false);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeBox AllowOnlyTaggedItemOwn: " + ex.Message);
			}
		}

		internal static GameItem? CreateFull()
		{
			try
			{
				GameItem val = CreateBareShell();
				if (!EnsureWindow(val, "Full", out GameInventory inv, applyFilter: false))
				{
					return val;
				}
				ApplyContainerMeta(val);
				if (inv == null)
				{
					inv = ResolveContentInventory(val);
				}
				if (inv == null)
				{
					Mod.Log.Warning("GeneProbeBox CreateFull: 无法解析盒内仓位");
					return val;
				}
				GameGridInventory val2 = ((Il2CppObjectBase)inv).TryCast<GameGridInventory>();
				if (val2 == null)
				{
					Mod.Log.Warning("GeneProbeBox CreateFull: 内仓不是 GameGridInventory，无法逐格放置");
					return val;
				}
				int num = 0;
				for (int i = 0; i < 2; i += 2)
				{
					for (int j = 0; j < 8; j++)
					{
						if (num >= 8)
						{
							break;
						}
						GameItem val3 = null;
						try
						{
							val3 = DirectoryMaster.Item("gene_edit_probe", true);
						}
						catch
						{
						}
						if (val3 == null)
						{
							val3 = GeneProbeItem.Create();
						}
						try
						{
							val3.unitCount = 1;
						}
						catch
						{
						}
						if (TryPlaceProbeAt(val2, val3, j, i))
						{
							num++;
							continue;
						}
						try
						{
							val3.Destroy();
						}
						catch
						{
						}
					}
				}
				ApplyProbeFilter(inv, val);
				GeneProbeSprites.ApplyBoxToItem(val);
				Mod.Log.Msg($"GeneProbeBox CreateFull: 已塞入 {num}/{8} 根探针（逐格）");
				return val;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeBox CreateFull: " + ex.Message);
				return null;
			}
		}

		private static bool TryPlaceProbeAt(GameGridInventory grid, GameItem probe, int x, int y)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Expected Obj, but got Unknown
			try
			{
				GeneProbeSprites.ApplyProbeToItem(probe);
				GridShape itemGridShape = new GridShapeBuilder(1, 2).SetDataFill(1, 2, (byte)1).SetTransform(x, y, false, 0).Build();
				SlotMarker val = new SlotMarker
				{
					item = probe,
					inventory = (GameInventory)(object)grid,
					itemGridShape = itemGridShape,
					index = 0,
					numTransfer = 1
				};
				int num = 0;
				try
				{
					num = val.AcceptUnchecked();
				}
				catch (Exception ex)
				{
					Mod.Log.Warning($"GeneProbeBox Place({x},{y}) AcceptUnchecked: {ex.Message}");
				}
				if (num > 0 || IsInInventory((GameInventory)(object)grid, probe))
				{
					return true;
				}
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning($"GeneProbeBox Place({x},{y}): {ex2.Message}");
			}
			return false;
		}

		private static GameItem CreateBareShell()
		{
			GameItem val = ItemDirectory.CreateEmptyItem("gene_edit_probe_box") ?? throw new InvalidOperationException("CreateEmptyItem('gene_edit_probe_box') null");
			val.identifier = "gene_edit_probe_box";
			try
			{
				val.SetName("基因编辑探针盒");
			}
			catch
			{
			}
			val.name = "基因编辑探针盒";
			return val;
		}

		private static void ApplyContainerMeta(GameItem item)
		{
			try
			{
				item.EnableTag("PRE_CONTAINER_TAG", false);
			}
			catch
			{
			}
			try
			{
				item.EnableTag("CONTAINER_TOOLTIP_TAG", false);
			}
			catch
			{
			}
			try
			{
				item.SetValue(1L);
			}
			catch
			{
			}
			try
			{
				AudioHelper.InitPlasticVial(item);
			}
			catch
			{
			}
			try
			{
				AudioHelper.InitPlasticContainer(item);
			}
			catch
			{
				try
				{
					AudioHelper.InitPouch(item);
				}
				catch
				{
				}
			}
		}

		private static bool IsInInventory(GameInventory inv, GameItem probe)
		{
			try
			{
				List<GameItem> childItems = inv.childItems;
				if (childItems == null)
				{
					return false;
				}
				for (int i = 0; i < childItems.Count; i++)
				{
					if (childItems[i] == probe)
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

		internal static GameItem? SpawnIntoStore()
		{
			try
			{
				GameItem val = null;
				try
				{
					val = DirectoryMaster.Item("gene_edit_probe_box", true);
				}
				catch
				{
				}
				if (val == null)
				{
					val = Create();
				}
				else
				{
					EnsureWindow(val, "Spawn");
				}
				try
				{
					GraphUtils.EmporiumTryAdd(val);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("GeneProbeBox EmporiumTryAdd: " + ex.Message);
					try
					{
						val.Destroy();
					}
					catch
					{
					}
					return null;
				}
				return val;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("GeneProbeBox SpawnIntoStore: " + ex2.Message);
				return null;
			}
		}
	}
	internal static class GeneProbeItem
	{
		internal const string ItemId = "gene_edit_probe";

		internal const string DisplayName = "基因编辑探针";

		internal const string ProbeTag = "GENE_EDIT_PROBE_TAG";

		private static Func<GameItem>? _factory;

		internal static void Apply(Harmony harmony)
		{
			PatchDirectory(harmony, typeof(MiscItemDirectory));
			PatchDirectory(harmony, typeof(ModItemDirectory));
		}

		private static void PatchDirectory(Harmony harmony, Type directoryType)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Expected Obj, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(directoryType, "InitDirectory", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Mod.Log.Warning("GeneProbe: " + directoryType.Name + ".InitDirectory 未找到");
			}
			else
			{
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(GeneProbeItem), "PostfixInitDirectory", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			}
		}

		public static void PostfixInitDirectory(ItemDirectory __instance)
		{
			try
			{
				Register(__instance);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbe register on " + ((object)__instance)?.GetType().Name + ": " + ex.Message);
			}
		}

		private static void Register(ItemDirectory dir)
		{
			if ((Object)(object)dir == (Object)null)
			{
				return;
			}
			try
			{
				if (((Directory<GameItem>)(object)dir).Has("gene_edit_probe"))
				{
					return;
				}
			}
			catch
			{
			}
			if ((Delegate)(object)_factory == (Delegate)null)
			{
				Func<GameItem> func = Create;
				try
				{
					_factory = DelegateSupport.ConvertDelegate<Func<GameItem>>((Delegate)func);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("GeneProbe ConvertDelegate: " + ex.Message);
					return;
				}
			}
			if ((Delegate)(object)_factory == (Delegate)null)
			{
				return;
			}
			try
			{
				if (((Directory<GameItem>)(object)dir).Add("gene_edit_probe", _factory))
				{
					Mod.Log.Msg("GeneProbe: 已注册 gene_edit_probe → " + ((object)dir).GetType().Name);
					return;
				}
				Mod.Log.Warning($"GeneProbe: Add('{"gene_edit_probe"}') 返回 false ({((object)dir).GetType().Name})");
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("GeneProbe Add: " + ex2.Message);
			}
		}

		internal static GameItem Create()
		{
			GameItem val = ItemDirectory.CreateEmptyItem("gene_edit_probe") ?? throw new InvalidOperationException("CreateEmptyItem('gene_edit_probe') null");
			val.identifier = "gene_edit_probe";
			try
			{
				val.SetName("基因编辑探针");
			}
			catch
			{
			}
			val.name = "基因编辑探针";
			GeneProbeSprites.ApplyProbeToItem(val);
			try
			{
				val.EnableTag("GENE_EDIT_PROBE_TAG", false);
			}
			catch
			{
			}
			try
			{
				val.SetValue(25L);
			}
			catch
			{
			}
			try
			{
				AudioHelper.InitPlasticVial(val);
			}
			catch
			{
				try
				{
					AudioHelper.InitPlasticCard(val);
				}
				catch
				{
					try
					{
						AudioHelper.InitPouch(val);
						goto end_IL_007b;
					}
					catch
					{
						goto end_IL_007b;
					}
					end_IL_007b:;
				}
			}
			return val;
		}

		internal static bool IsProbe(GameItem? item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				if (string.Equals(item.identifier, "gene_edit_probe", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			catch
			{
			}
			try
			{
				return item.IsTag("GENE_EDIT_PROBE_TAG");
			}
			catch
			{
				return false;
			}
		}

		internal static GameItem? SpawnIntoStore()
		{
			try
			{
				GameItem val = null;
				try
				{
					val = DirectoryMaster.Item("gene_edit_probe", true);
				}
				catch
				{
				}
				if (val == null)
				{
					val = Create();
				}
				try
				{
					GraphUtils.EmporiumTryAdd(val);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("GeneProbe EmporiumTryAdd: " + ex.Message);
					try
					{
						val.Destroy();
					}
					catch
					{
					}
					return null;
				}
				return val;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("GeneProbe SpawnIntoStore: " + ex2.Message);
				return null;
			}
		}
	}
	internal static class GeneProbeRancherShop
	{
		private const string SerumGreenId = "serum_green";

		private const string SerumRedId = "serum_red";

		private static bool _addingBox;

		private static int _addedOnFrame = -1;

		internal static void Apply(Harmony harmony)
		{
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(PlayerStore), "AddDirectSellingItemToTable", new Type[5]
				{
					typeof(GameItem),
					typeof(bool),
					typeof(bool),
					typeof(bool),
					typeof(int)
				}, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning("GeneProbeRancherShop: AddDirectSellingItemToTable 未找到");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(GeneProbeRancherShop), "PostfixAddDirect", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("GeneProbeRancherShop: 已挂钩 AddDirectSellingItemToTable（随牧场主血清上架满装探针盒）");
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeRancherShop Apply: " + ex.Message);
			}
		}

		public static void PostfixAddDirect(PlayerStore __instance, GameItem gameItem)
		{
			if (_addingBox || __instance == null || gameItem == null)
			{
				return;
			}
			string text = null;
			try
			{
				text = gameItem.identifier;
			}
			catch
			{
				return;
			}
			if (!string.Equals(text, "serum_green", StringComparison.OrdinalIgnoreCase) && !string.Equals(text, "serum_red", StringComparison.OrdinalIgnoreCase))
			{
				return;
			}
			int frameCount = Time.frameCount;
			if (frameCount == _addedOnFrame)
			{
				return;
			}
			try
			{
				_addingBox = true;
				_addedOnFrame = frameCount;
				GameItem val = GeneProbeBoxItem.CreateFull();
				if (val == null)
				{
					Mod.Log.Warning("GeneProbeRancherShop: CreateFull 返回 null");
					return;
				}
				__instance.AddDirectSellingItemToTable(val, false, false, false, 0);
				Mod.Log.Msg("GeneProbeRancherShop: 已随 " + text + " 上架满装基因编辑探针盒");
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeRancherShop postfix: " + ex.Message);
			}
			finally
			{
				_addingBox = false;
			}
		}
	}
	internal static class GeneProbeSprites
	{
		internal const string ProbeSpriteName = "gene_edit_probe";

		internal const string BoxSpriteName = "gene_edit_probe_box";

		private static Sprite? _probe;

		private static Sprite? _box;

		private static bool _hooked;

		internal static void Apply(Harmony harmony)
		{
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Expected Obj, but got Unknown
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Expected Obj, but got Unknown
			if (_hooked)
			{
				return;
			}
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(GameItemElement), "ResolveSpriteByName", new Type[1] { typeof(string) }, (Type[])null);
				if (methodInfo != null)
				{
					harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(GeneProbeSprites), "PrefixResolveSpriteByName", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
				MethodInfo methodInfo2 = AccessTools.Method(typeof(GameItemElement), "ResolveItemSprite", (Type[])null, (Type[])null);
				if (methodInfo2 != null)
				{
					harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(typeof(GeneProbeSprites), "PrefixResolveItemSprite", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeSprites Apply: " + ex.Message);
			}
			_hooked = true;
			GetProbe();
			GetBox();
		}

		public static bool PrefixResolveSpriteByName(string __0, ref Sprite __result)
		{
			if (string.Equals(__0, "gene_edit_probe", StringComparison.OrdinalIgnoreCase) || string.Equals(__0, "gene_edit_probe", StringComparison.OrdinalIgnoreCase))
			{
				Sprite probe = GetProbe();
				if ((Object)(object)probe == (Object)null)
				{
					return true;
				}
				__result = probe;
				return false;
			}
			if (string.Equals(__0, "gene_edit_probe_box", StringComparison.OrdinalIgnoreCase) || string.Equals(__0, "gene_edit_probe_box", StringComparison.OrdinalIgnoreCase))
			{
				Sprite box = GetBox();
				if ((Object)(object)box == (Object)null)
				{
					return true;
				}
				__result = box;
				return false;
			}
			return true;
		}

		public static bool PrefixResolveItemSprite(GameItemElement __instance, ref Sprite __result)
		{
			try
			{
				string a = null;
				try
				{
					a = ((__instance != null) ? ((GameItem)__instance).identifier : null);
				}
				catch
				{
				}
				if (string.Equals(a, "gene_edit_probe", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "gene_edit_probe", StringComparison.OrdinalIgnoreCase))
				{
					Sprite probe = GetProbe();
					if ((Object)(object)probe == (Object)null)
					{
						return true;
					}
					__result = probe;
					return false;
				}
				if (string.Equals(a, "gene_edit_probe_box", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "gene_edit_probe_box", StringComparison.OrdinalIgnoreCase))
				{
					Sprite box = GetBox();
					if ((Object)(object)box == (Object)null)
					{
						return true;
					}
					__result = box;
					return false;
				}
			}
			catch
			{
			}
			return true;
		}

		internal static void ApplyProbeToItem(GameItem item)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			if (item != null)
			{
				ApplyModSpriteFields(item, "gene_edit_probe");
				try
				{
					GridShape shape = new GridShapeBuilder(1, 2).SetDataFill(1, 2, (byte)1).Build();
					item.SetShape(shape);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("GeneProbeSprites probe shape: " + ex.Message);
				}
				GetProbe();
			}
		}

		internal static void ApplyBoxToItem(GameItem item)
		{
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			if (item == null)
			{
				return;
			}
			ApplyModSpriteFields(item, "gene_edit_probe_box");
			Sprite box = GetBox();
			try
			{
				int num = 2;
				int num2 = 2;
				if ((Object)(object)box != (Object)null)
				{
					Rect rect = box.rect;
					num = Math.Max(1, (int)(rect.width / 16f));
					num2 = Math.Max(1, (int)(rect.height / 16f));
				}
				GridShape shape = new GridShapeBuilder(num, num2).SetDataFill(num, num2, (byte)1).Build();
				item.SetShape(shape);
				try
				{
					item.ValidateShapeState();
				}
				catch
				{
				}
				int num3 = 0;
				int num4 = 0;
				try
				{
					GridShape shape2 = item.shape;
					if (shape2 != null)
					{
						num3 = shape2.width;
						num4 = shape2.height;
					}
				}
				catch
				{
				}
				if (num3 != num || num4 != num2)
				{
					Mod.Log.Warning($"GeneProbeSprites box SetShape 后仍是 {num3}×{num4}（期望 {num}×{num2}）");
				}
				else
				{
					Mod.Log.Msg($"GeneProbeSprites box: shape OK → {num3}×{num4}");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeSprites box shape: " + ex.Message);
			}
		}

		private static void ApplyModSpriteFields(GameItem item, string spriteName)
		{
			try
			{
				item.spritePath = spriteName;
				item.spriteAtlasPath = "";
				item.spriteChanged = true;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeSprites fields: " + ex.Message);
			}
			try
			{
				PropertyInfo propertyInfo = AccessTools.Property(typeof(GameItem), "useSpriteFromMod");
				if (propertyInfo?.SetMethod != null)
				{
					propertyInfo.SetValue(item, true);
				}
				else
				{
					(AccessTools.Field(typeof(GameItem), "_useSpriteFromMod_k__BackingField") ?? AccessTools.Field(typeof(GameItem), "<useSpriteFromMod>k__BackingField"))?.SetValue(item, true);
				}
			}
			catch
			{
			}
		}

		internal static Sprite? GetProbe()
		{
			return _probe ?? (_probe = LoadSprite("gene_edit_probe", "gene_edit_probe.png", 16, 32));
		}

		internal static Sprite? GetBox()
		{
			return _box ?? (_box = LoadSprite("gene_edit_probe_box", "gene_edit_probe_box.png", 32, 32));
		}

		private static Sprite? LoadSprite(string spriteName, string resourceFile, int expectW, int expectH)
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Expected Obj, but got Unknown
			//IL_014e: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				byte[] array = LoadPngBytes(resourceFile);
				if (array == null || array.Length == 0)
				{
					Mod.Log.Warning("GeneProbeSprites: 找不到 " + resourceFile);
					return null;
				}
				Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false);
				Il2CppStructArray<byte> val2 = new Il2CppStructArray<byte>((long)array.Length);
				for (int i = 0; i < array.Length; i++)
				{
					((Il2CppArrayBase<byte>)(object)val2)[i] = array[i];
				}
				if (!ImageConversion.LoadImage(val, val2))
				{
					Mod.Log.Warning("GeneProbeSprites: LoadImage 失败 " + resourceFile);
					return null;
				}
				((Texture)val).filterMode = (FilterMode)0;
				((Texture)val).wrapMode = (TextureWrapMode)1;
				((Object)val).hideFlags = (HideFlags)61;
				if (((Texture)val).width != expectW || ((Texture)val).height != expectH)
				{
					Mod.Log.Warning($"GeneProbeSprites: {resourceFile} 期望 {expectW}×{expectH}，实际 {((Texture)val).width}×{((Texture)val).height}");
				}
				Sprite val3 = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
				((Object)val3).name = spriteName;
				((Object)val3).hideFlags = (HideFlags)61;
				Mod.Log.Msg($"GeneProbeSprites: 已加载 {spriteName} {((Texture)val).width}×{((Texture)val).height}");
				return val3;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeSprites LoadSprite(" + resourceFile + "): " + ex.Message);
				return null;
			}
		}

		private static byte[]? LoadPngBytes(string fileName)
		{
			try
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				string[] manifestResourceNames = executingAssembly.GetManifestResourceNames();
				foreach (string text in manifestResourceNames)
				{
					if (!text.EndsWith(fileName, StringComparison.OrdinalIgnoreCase) && !string.Equals(text, fileName, StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					using Stream stream = executingAssembly.GetManifestResourceStream(text);
					if (stream == null)
					{
						continue;
					}
					using MemoryStream memoryStream = new MemoryStream();
					stream.CopyTo(memoryStream);
					return memoryStream.ToArray();
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("GeneProbeSprites embed: " + ex.Message);
			}
			try
			{
				string directoryName = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				if (!string.IsNullOrEmpty(directoryName))
				{
					string path = Path.Combine(directoryName, "Sprites", fileName);
					if (File.Exists(path))
					{
						return File.ReadAllBytes(path);
					}
					path = Path.Combine(directoryName, fileName);
					if (File.Exists(path))
					{
						return File.ReadAllBytes(path);
					}
				}
			}
			catch
			{
			}
			return null;
		}
	}
	public sealed class Mod : MelonMod
	{
		internal static Instance Log;

		internal static MelonPreferences_Category Cfg;

		internal static MelonPreferences_Entry<float> NativePosX;

		internal static MelonPreferences_Entry<float> NativePosY;

		internal static MelonPreferences_Entry<float> NativeWidth;

		internal static MelonPreferences_Entry<float> NativeHeight;

		internal static MelonPreferences_Entry<string> ToggleKey;

		internal static MelonPreferences_Entry<int> UiZoomLevel;

		internal static MelonPreferences_Entry<bool> FilterMale;

		internal static MelonPreferences_Entry<bool> FilterFemale;

		internal static MelonPreferences_Entry<bool> FilterPregnant;

		internal static MelonPreferences_Entry<bool> FilterNotPregnant;

		internal static MelonPreferences_Entry<bool> FilterNeutered;

		internal static MelonPreferences_Entry<bool> FilterIntact;

		internal static MelonPreferences_Entry<bool> FilterUnderfed;

		internal static MelonPreferences_Entry<bool> FilterFedOk;

		internal static MelonPreferences_Entry<string> SortGene;

		internal static MelonPreferences_Entry<bool> SortDesc;

		internal static MelonPreferences_Entry<bool> IncludeAlleles;

		internal static MelonPreferences_Entry<int> MaxGeneEditsPerPregnancy;

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			Cfg = MelonPreferences.CreateCategory("SciRat");
			NativePosX = Cfg.CreateEntry<float>("NativePosX", -100000f, (string)null, "Saved window X", false, false, (ValueValidator)null, (string)null);
			NativePosY = Cfg.CreateEntry<float>("NativePosY", -100000f, (string)null, "Saved window Y", false, false, (ValueValidator)null, (string)null);
			NativeWidth = Cfg.CreateEntry<float>("NativeWidth", 720f, (string)null, "Saved window width", false, false, (ValueValidator)null, (string)null);
			NativeHeight = Cfg.CreateEntry<float>("NativeHeight", -1f, (string)null, "Saved window height", false, false, (ValueValidator)null, (string)null);
			ToggleKey = Cfg.CreateEntry<string>("ToggleKey", "F6", (string)null, "Open/close rat monitor", false, false, (ValueValidator)null, (string)null);
			UiZoomLevel = Cfg.CreateEntry<int>("UiZoomLevel", 0, (string)null, "Monitor UI zoom: 0=1x, 1=1.5x, 2=2x", false, false, (ValueValidator)null, (string)null);
			FilterMale = Cfg.CreateEntry<bool>("FilterMale", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			FilterFemale = Cfg.CreateEntry<bool>("FilterFemale", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			FilterPregnant = Cfg.CreateEntry<bool>("FilterPregnant", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			FilterNotPregnant = Cfg.CreateEntry<bool>("FilterNotPregnant", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			FilterNeutered = Cfg.CreateEntry<bool>("FilterNeutered", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			FilterIntact = Cfg.CreateEntry<bool>("FilterIntact", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			FilterUnderfed = Cfg.CreateEntry<bool>("FilterUnderfed", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			FilterFedOk = Cfg.CreateEntry<bool>("FilterFedOk", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			SortGene = Cfg.CreateEntry<string>("SortGene", "ANIMAL_GROWTH_RATE_TAG", (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			SortDesc = Cfg.CreateEntry<bool>("SortDesc", true, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			IncludeAlleles = Cfg.CreateEntry<bool>("IncludeAlleles", false, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
			MaxGeneEditsPerPregnancy = Cfg.CreateEntry<int>("MaxGeneEditsPerPregnancy", 0, (string)null, "一次怀孕最多可编辑几个基因；0=不限制", false, false, (ValueValidator)null, (string)null);
			LoadFiltersFromPrefs();
			try
			{
				GameSession.Apply(((MelonBase)this).HarmonyInstance);
				RatMonitorHooks.Apply(((MelonBase)this).HarmonyInstance);
				GeneProbeSprites.Apply(((MelonBase)this).HarmonyInstance);
				GeneProbeItem.Apply(((MelonBase)this).HarmonyInstance);
				GeneProbeBoxItem.Apply(((MelonBase)this).HarmonyInstance);
				GeneProbeRancherShop.Apply(((MelonBase)this).HarmonyInstance);
				GeneEditLimit.Apply(((MelonBase)this).HarmonyInstance);
			}
			catch (Exception ex)
			{
				Log.Warning("SciRat hooks: " + ex.Message);
			}
			Log.Msg("科学养鼠 1.3.12 — 孕检基因编辑：更优等位绿色高亮");
		}

		internal static void LoadFiltersFromPrefs()
		{
			RatMonitorFilters.FilterMale = FilterMale.Value;
			RatMonitorFilters.FilterFemale = FilterFemale.Value;
			RatMonitorFilters.FilterPregnant = FilterPregnant.Value;
			RatMonitorFilters.FilterNotPregnant = FilterNotPregnant.Value;
			RatMonitorFilters.FilterNeutered = FilterNeutered.Value;
			RatMonitorFilters.FilterIntact = FilterIntact.Value;
			RatMonitorFilters.FilterUnderfed = FilterUnderfed.Value;
			RatMonitorFilters.FilterFedOk = FilterFedOk.Value;
			RatMonitorFilters.SortGeneId = (string.IsNullOrEmpty(SortGene.Value) ? "ANIMAL_GROWTH_RATE_TAG" : SortGene.Value);
			RatMonitorFilters.SortDescending = SortDesc.Value;
			RatMonitorFilters.IncludeAlleles = IncludeAlleles.Value;
			RatMonitorFilters.SyncAlleleGate();
		}

		internal static void SaveFiltersToPrefs()
		{
			FilterMale.Value = RatMonitorFilters.FilterMale;
			FilterFemale.Value = RatMonitorFilters.FilterFemale;
			FilterPregnant.Value = RatMonitorFilters.FilterPregnant;
			FilterNotPregnant.Value = RatMonitorFilters.FilterNotPregnant;
			FilterNeutered.Value = RatMonitorFilters.FilterNeutered;
			FilterIntact.Value = RatMonitorFilters.FilterIntact;
			FilterUnderfed.Value = RatMonitorFilters.FilterUnderfed;
			FilterFedOk.Value = RatMonitorFilters.FilterFedOk;
			SortGene.Value = RatMonitorFilters.SortGeneId;
			SortDesc.Value = RatMonitorFilters.SortDescending;
			IncludeAlleles.Value = RatMonitorFilters.IncludeAlleles && RatData.CanSeeAlleles();
			MelonPreferences.Save();
		}

		public override void OnUpdate()
		{
			RatMonitorUi.Tick();
			RatMonitorRefresh.FlushPending();
			GeneProbeBoxItem.TickEnsure();
		}

		public override void OnApplicationQuit()
		{
			SaveFiltersToPrefs();
			MelonPreferences.Save();
		}

		internal static KeyCode ReadToggle()
		{
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			if (ToggleKey == null || !Enum.TryParse<KeyCode>(ToggleKey.Value, true, out KeyCode result) || (int)result == 0)
			{
				return (KeyCode)287;
			}
			return result;
		}
	}
	internal static class RatData
	{
		private sealed class FeedSource
		{
			internal int Remaining;
		}

		private sealed class CageFeedPool
		{
			internal readonly List<FeedSource> Dispensers = new List<FeedSource>();

			internal readonly List<int> LoosePortions = new List<int>();

			internal int LooseIndex;
		}

		internal readonly struct NextMeatBreakdown
		{
			internal readonly int Final;

			internal readonly int Vanilla;

			internal readonly int Uplift;

			internal readonly float HungerCur;

			internal readonly float HungerProj;

			internal readonly float HungerMax;

			internal readonly float Weight;

			internal readonly float GrowthExpr;

			internal readonly float HungerRateExpr;

			internal readonly bool Neutered;

			internal readonly bool UsedCache;

			internal readonly bool Satiated;

			internal readonly string Path;

			internal readonly int AgeNow;

			internal readonly int AgeOvernight;

			internal NextMeatBreakdown(int final, int vanilla, int uplift, float hungerCur, float hungerProj, float hungerMax, float weight, float growthExpr, float hungerRateExpr, bool neutered, bool usedCache, bool satiated, string path, int ageNow = 0, int ageOvernight = 0)
			{
				Final = final;
				Vanilla = vanilla;
				Uplift = uplift;
				HungerCur = hungerCur;
				HungerProj = hungerProj;
				HungerMax = hungerMax;
				Weight = weight;
				GrowthExpr = growthExpr;
				HungerRateExpr = hungerRateExpr;
				Neutered = neutered;
				UsedCache = usedCache;
				Satiated = satiated;
				Path = path;
				AgeNow = ageNow;
				AgeOvernight = ageOvernight;
			}
		}

		private static readonly Dictionary<int, float> ProjectedHungerByRat = new Dictionary<int, float>();

		private static readonly HashSet<int> UnderfedTonight = new HashSet<int>();

		internal const float FeedSatietyFactor = 0.75f;

		private const float WeightChangeHungerRef = 75f;

		internal const string CannedFoodTag = "CANNED_FOOD_TAG";

		internal const string StarvingTag = "ANIMAL_IS_STARVING_TAG";

		internal const string MaxHungerTag = "ANIMAL_MAX_HUNGER_TAG";

		internal const string AlleleA = "_ALLELE_A";

		internal const string AlleleB = "_ALLELE_B";

		internal const string AnimalItemTag = "ANIMAL_ITEM_TAG";

		internal const string DeadTag = "ANIMAL_IS_DEAD_TAG";

		internal const string PregnantTag = "ANIMAL_PREGNANT_TAG";

		internal const string PregnantProgressTag = "ANIMAL_PREGNANT_PROGRESS_TAG";

		internal const string GestationTag = "ANIMAL_GESTATION_PERIOD_TAG";

		internal const string AgeTag = "ANIMAL_AGE_TAG";

		internal const string WeightTag = "ANIMAL_WEIGHT_TAG";

		internal const string SexTag = "ANIMAL_SEXE_TAG";

		internal const string NeuteredTag = "ANIMAL_NEUTERED_TAG";

		internal const string GrowthGene = "ANIMAL_GROWTH_RATE_TAG";

		internal const string HungerGene = "ANIMAL_HUNGER_RATE_TAG";

		internal const string FertilityGene = "ANIMAL_FERTILITY_TAG";

		internal const string LitterGene = "ANIMAL_LITTER_SIZE_TAG";

		internal const string ImmunityGene = "ANIMAL_IMMUNITY_TAG";

		internal const string MaxHealthGene = "ANIMAL_MAX_HEALTH_TAG";

		internal const string LongevityGene = "ANIMAL_LONGEVITY_TAG";

		internal const string HungerTag = "ANIMAL_HUNGER_TAG";

		internal const string PartnerAlleleA = "_PARTNER_ALLELE_A";

		internal const string PartnerAlleleB = "_PARTNER_ALLELE_B";

		private static readonly Dictionary<string, string> GeneLocKeys = new Dictionary<string, string>
		{
			["ANIMAL_GROWTH_RATE_TAG"] = "husbandry_stat_growth_rate",
			["ANIMAL_HUNGER_RATE_TAG"] = "husbandry_stat_hunger_rate",
			["ANIMAL_FERTILITY_TAG"] = "husbandry_tooltip_fertility",
			["ANIMAL_LITTER_SIZE_TAG"] = "husbandry_stat_litter_size",
			["ANIMAL_IMMUNITY_TAG"] = "husbandry_stat_immunity",
			["ANIMAL_MAX_HEALTH_TAG"] = "husbandry_stat_max_health",
			["ANIMAL_LONGEVITY_TAG"] = "husbandry_stat_longevity"
		};

		private static readonly string[] DefaultGeneIds = new string[7] { "ANIMAL_GROWTH_RATE_TAG", "ANIMAL_HUNGER_RATE_TAG", "ANIMAL_FERTILITY_TAG", "ANIMAL_LITTER_SIZE_TAG", "ANIMAL_IMMUNITY_TAG", "ANIMAL_MAX_HEALTH_TAG", "ANIMAL_LONGEVITY_TAG" };

		internal const string CalorieValueTag = "CALORIE_VALUE_TAG";

		internal const string FeedDispenserTag = "FEED_DISPENSER_TAG";

		internal static void InvalidateFeedProjection()
		{
			ProjectedHungerByRat.Clear();
			UnderfedTonight.Clear();
		}

		internal static void DropRatProjection(GameItem rat)
		{
			if (rat != null)
			{
				ProjectedHungerByRat.Remove(rat.uniqueId);
				UnderfedTonight.Remove(rat.uniqueId);
			}
		}

		internal static void RebuildFeedProjection(IList<GameItem> ratsInStoreOrder)
		{
			ProjectedHungerByRat.Clear();
			UnderfedTonight.Clear();
			Dictionary<int, List<(int, GameItem)>> dictionary = new Dictionary<int, List<(int, GameItem)>>();
			for (int i = 0; i < ratsInStoreOrder.Count; i++)
			{
				GameItem val = ratsInStoreOrder[i];
				int cageId = GetCageId(val);
				if (cageId != 0)
				{
					if (!dictionary.TryGetValue(cageId, out var value))
					{
						value = (dictionary[cageId] = new List<(int, GameItem)>());
					}
					value.Add((i, val));
				}
			}
			foreach (List<(int, GameItem)> value2 in dictionary.Values)
			{
				value2.Sort(((int storeIndex, GameItem rat) a, (int storeIndex, GameItem rat) b) => a.storeIndex.CompareTo(b.storeIndex));
				CageFeedPool pool = BuildFeedPool(value2[0].Item2);
				foreach (var item in value2)
				{
					ApplyFeedProjection(item.Item2, pool);
				}
			}
		}

		internal static void RebuildCageProjection(IList<GameItem> cageRatsInOrder)
		{
			if (cageRatsInOrder == null || cageRatsInOrder.Count == 0)
			{
				return;
			}
			CageFeedPool pool = BuildFeedPool(cageRatsInOrder[0]);
			foreach (GameItem item in cageRatsInOrder)
			{
				DropRatProjection(item);
				ApplyFeedProjection(item, pool);
			}
		}

		private static void ApplyFeedProjection(GameItem rat, CageFeedPool pool)
		{
			try
			{
				float num = HungerCurrent(rat);
				float num2 = HungerMax(rat);
				float num3 = SatietyThreshold(num2);
				float num4 = SimulateRatFeed(rat, num, num2, num3, pool);
				ProjectedHungerByRat[rat.uniqueId] = num4;
				if (num < num3 - 0.5f && num4 < num3 - 0.5f)
				{
					UnderfedTonight.Add(rat.uniqueId);
				}
			}
			catch
			{
			}
		}

		internal static bool WillUnderfeedTonight(GameItem rat)
		{
			if (rat == null)
			{
				return false;
			}
			try
			{
				if (UnderfedTonight.Contains(rat.uniqueId))
				{
					return true;
				}
				if (ProjectedHungerByRat.ContainsKey(rat.uniqueId))
				{
					return false;
				}
				float num = HungerCurrent(rat);
				float num2 = HungerMax(rat);
				float num3 = SatietyThreshold(num2);
				if (num >= num3 - 0.5f)
				{
					return false;
				}
				CageFeedPool pool = BuildFeedPool(rat);
				return SimulateRatFeed(rat, num, num2, num3, pool) < num3 - 0.5f;
			}
			catch
			{
				return false;
			}
		}

		internal static float SatietyThreshold(float maxHunger)
		{
			if (maxHunger <= 0f)
			{
				maxHunger = 1000f;
			}
			return maxHunger * 0.75f;
		}

		internal static int GetCageId(GameItem rat)
		{
			try
			{
				GameInventory parentInventory = rat.parentInventory;
				GameItem val = ((parentInventory != null) ? parentInventory.GetParentItem() : null);
				if (val == null)
				{
					return 0;
				}
				return val.uniqueId;
			}
			catch
			{
				return 0;
			}
		}

		internal static bool IsRefreshTag(string? tagId)
		{
			if (string.IsNullOrEmpty(tagId))
			{
				return false;
			}
			switch (tagId)
			{
			default:
				return tagId == "ANIMAL_SEXE_TAG";
			case "ANIMAL_GROWTH_RATE_TAG":
			case "ANIMAL_HUNGER_RATE_TAG":
			case "ANIMAL_FERTILITY_TAG":
			case "ANIMAL_LITTER_SIZE_TAG":
			case "ANIMAL_IMMUNITY_TAG":
			case "ANIMAL_MAX_HEALTH_TAG":
			case "ANIMAL_LONGEVITY_TAG":
			case "ANIMAL_WEIGHT_TAG":
			case "ANIMAL_HUNGER_TAG":
			case "ANIMAL_AGE_TAG":
			case "ANIMAL_NEUTERED_TAG":
			case "ANIMAL_PREGNANT_TAG":
			case "ANIMAL_PREGNANT_PROGRESS_TAG":
			case "ANIMAL_GESTATION_PERIOD_TAG":
				return true;
			}
		}

		internal static bool IsCageFeedTag(string? tagId)
		{
			if (string.IsNullOrEmpty(tagId))
			{
				return false;
			}
			if (!(tagId == "CALORIE_VALUE_TAG") && !(tagId == "CANNED_FOOD_TAG"))
			{
				return tagId == "FEED_DISPENSER_TAG";
			}
			return true;
		}

		internal static bool AffectsFeedPool(GameItem? item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				if (item.IsTag("FEED_DISPENSER_TAG"))
				{
					return true;
				}
				return IsLooseEdible(item);
			}
			catch
			{
				return false;
			}
		}

		private static bool IsLooseEdible(GameItem item)
		{
			try
			{
				if (item.IsTag("ANIMAL_ITEM_TAG") || item.IsTag("FEED_DISPENSER_TAG") || item.IsTag("CANNED_FOOD_TAG"))
				{
					return false;
				}
				return item.IsTag("CALORIE_VALUE_TAG") && CalorieValueInt(item) > 0;
			}
			catch
			{
				return false;
			}
		}

		private static int CalorieValueInt(GameItem item)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly("CALORIE_VALUE_TAG");
				if (tagReadonly == null)
				{
					return 0;
				}
				int valueInt = tagReadonly.valueInt;
				if (valueInt > 0)
				{
					return valueInt;
				}
				float num = tagReadonly.valueFloat;
				if (num <= 0f)
				{
					num = tagReadonly.GetFloat();
				}
				return (num > 0f) ? ((int)Math.Round(num)) : 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static float HungerCurrent(GameItem item)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly("ANIMAL_HUNGER_TAG");
				if (tagReadonly == null)
				{
					return 0f;
				}
				float num = tagReadonly.valueFloat;
				if (num <= 0f)
				{
					num = tagReadonly.GetFloat();
				}
				return Math.Max(0f, num);
			}
			catch
			{
				return 0f;
			}
		}

		internal static float HungerMax(GameItem item)
		{
			try
			{
				float maxHunger = HusbandryHelper.GetMaxHunger(item);
				if (maxHunger > 0f)
				{
					return maxHunger;
				}
				TagState tagReadonly = item.GetTagReadonly("ANIMAL_MAX_HUNGER_TAG");
				if (tagReadonly != null)
				{
					float num = tagReadonly.valueFloat;
					if (num <= 0f)
					{
						num = tagReadonly.GetFloat();
					}
					if (num > 0f)
					{
						return num;
					}
				}
				return 0f;
			}
			catch
			{
				return 0f;
			}
		}

		internal static bool IsRat(GameItem? item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				if (!item.IsTag("ANIMAL_ITEM_TAG"))
				{
					return false;
				}
				if (item.IsTag("ANIMAL_IS_DEAD_TAG"))
				{
					return false;
				}
				try
				{
					if (HusbandryHelper.IsAnimalDead(item))
					{
						return false;
					}
				}
				catch
				{
				}
				try
				{
					if (!GeneralHelper.IsItemOwned(item))
					{
						return false;
					}
				}
				catch
				{
				}
				AnimalSpecies species = HusbandryHelper.GetSpecies(item);
				if (species == null)
				{
					return false;
				}
				AnimalSpecies rat = AnimalSpecies.Rat;
				if (rat != null && !string.IsNullOrEmpty(rat.speciesID))
				{
					return string.Equals(species.speciesID, rat.speciesID, StringComparison.Ordinal);
				}
				string text = species.speciesID ?? "";
				return text.IndexOf("rat", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("mouse", StringComparison.OrdinalIgnoreCase) >= 0;
			}
			catch
			{
				return false;
			}
		}

		internal static bool IsPregnant(GameItem item)
		{
			try
			{
				return item.IsTag("ANIMAL_PREGNANT_TAG");
			}
			catch
			{
				return false;
			}
		}

		internal static bool IsMale(GameItem item)
		{
			string text = SexRaw(item);
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			if (string.Equals(text, "MALE", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "m", StringComparison.OrdinalIgnoreCase) || text == "1" || text.Contains("公"))
			{
				return true;
			}
			return false;
		}

		internal static bool IsFemale(GameItem item)
		{
			if (IsPregnant(item))
			{
				return true;
			}
			string text = SexRaw(item);
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			if (string.Equals(text, "FEMALE", StringComparison.OrdinalIgnoreCase) || string.Equals(text, "f", StringComparison.OrdinalIgnoreCase) || text == "0" || text.Contains("母"))
			{
				return true;
			}
			return false;
		}

		internal static string SexRaw(GameItem item)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly("ANIMAL_SEXE_TAG");
				if (tagReadonly == null)
				{
					return "";
				}
				string valueString = tagReadonly.valueString;
				if (!string.IsNullOrEmpty(valueString))
				{
					return valueString;
				}
				valueString = tagReadonly.identifierName;
				if (!string.IsNullOrEmpty(valueString))
				{
					return valueString;
				}
				return tagReadonly.identifier ?? "";
			}
			catch
			{
				return "";
			}
		}

		internal static string SexDisplay(GameItem item)
		{
			try
			{
				string sexeDisplay = HusbandryHelper.GetSexeDisplay(SexRaw(item));
				if (!string.IsNullOrEmpty(sexeDisplay) && sexeDisplay != "No Name")
				{
					return sexeDisplay;
				}
			}
			catch
			{
			}
			if (IsPregnant(item) || IsFemale(item))
			{
				return "母";
			}
			if (IsMale(item))
			{
				return "公";
			}
			return "?";
		}

		internal static int AgeDays(GameItem item)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly("ANIMAL_AGE_TAG");
				if (tagReadonly == null)
				{
					return 0;
				}
				return tagReadonly.valueInt;
			}
			catch
			{
				return 0;
			}
		}

		internal static float Weight(GameItem item)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly("ANIMAL_WEIGHT_TAG");
				if (tagReadonly == null)
				{
					return 0f;
				}
				float num = tagReadonly.valueFloat;
				if (num <= 0f)
				{
					num = tagReadonly.GetFloat();
				}
				return num;
			}
			catch
			{
				return 0f;
			}
		}

		internal static int NextMeat(GameItem item)
		{
			return ExplainNextMeat(item).Final;
		}

		internal static NextMeatBreakdown ExplainNextMeat(GameItem item)
		{
			if (item == null)
			{
				return EmptyBreakdown("null-item");
			}
			try
			{
				float num = HungerCurrent(item);
				float num2 = HungerMax(item);
				if (num2 <= 0f)
				{
					num2 = 1000f;
				}
				float num3 = SatietyThreshold(num2);
				float value = num;
				bool usedCache = false;
				try
				{
					if (ProjectedHungerByRat.TryGetValue(item.uniqueId, out value))
					{
						usedCache = true;
					}
					else
					{
						value = SimulateRatFeed(item, num, num2, num3, BuildFeedPool(item));
					}
				}
				catch
				{
					value = SimulateRatFeed(item, num, num2, num3, BuildFeedPool(item));
				}
				float weight = Weight(item);
				bool satiated = num >= num3 - 0.5f;
				float growthExpr = SafeGeneExpr(item, "ANIMAL_GROWTH_RATE_TAG");
				float hungerRateExpr = SafeGeneExpr(item, "ANIMAL_HUNGER_RATE_TAG");
				bool neutered = SafeIsTag(item, "ANIMAL_NEUTERED_TAG");
				bool flag = SafeIsTag(item, "ANIMAL_IS_STARVING_TAG");
				bool starvingProj = flag && value <= num + 0.5f;
				int num4 = AgeDays(item);
				int weightChangeAtOvernightAge = GetWeightChangeAtOvernightAge(item, num4);
				int ageOvernight = num4 + 1;
				if (Math.Abs(value - num) <= 0.5f)
				{
					return new NextMeatBreakdown(weightChangeAtOvernightAge, weightChangeAtOvernightAge, 0, num, value, num2, weight, growthExpr, hungerRateExpr, neutered, usedCache, satiated, "vanilla-age+1", num4, ageOvernight);
				}
				int num5 = PredictWeightChangeFromVanilla(weightChangeAtOvernightAge, num, value, flag, starvingProj);
				return new NextMeatBreakdown(num5, weightChangeAtOvernightAge, num5 - weightChangeAtOvernightAge, num, value, num2, weight, growthExpr, hungerRateExpr, neutered, usedCache, satiated, "hunger-scale-age+1", num4, ageOvernight);
			}
			catch (Exception ex)
			{
				return EmptyBreakdown("error:" + ex.Message);
			}
		}

		private static int GetWeightChangeAtOvernightAge(GameItem item, int ageNow)
		{
			int age = ageNow + 1;
			bool flag = false;
			try
			{
				flag = TrySetAgeDays(item, age);
				return HusbandryHelper.GetAnimalCurrentWeightChange(item);
			}
			catch
			{
				try
				{
					return HusbandryHelper.GetAnimalCurrentWeightChange(item);
				}
				catch
				{
					return 0;
				}
			}
			finally
			{
				if (flag)
				{
					try
					{
						TrySetAgeDays(item, ageNow);
					}
					catch
					{
					}
				}
			}
		}

		private static bool TrySetAgeDays(GameItem item, int age)
		{
			try
			{
				item.ModifyTag("ANIMAL_AGE_TAG", Action<TagState>.op_Implicit((Action<TagState>)((TagState t) =>
				{
					t.SetInt(age);
					try
					{
						t.Enable();
					}
					catch
					{
					}
				})), true);
				return true;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning($"TrySetAgeDays({age}): {ex.Message}");
				return false;
			}
		}

		private static int PredictWeightChangeFromVanilla(int vanilla, float hungerCur, float hungerProj, bool starvingCur, bool starvingProj)
		{
			float num = HungerGrowthScale(hungerCur, starvingCur);
			float num2 = HungerGrowthScale(hungerProj, starvingProj);
			if (Math.Abs(num) < 0.0001f)
			{
				return (int)Math.Round((vanilla == 0) ? num2 : ((float)vanilla));
			}
			return (int)Math.Round((float)vanilla * (num2 / num));
		}

		private static float HungerGrowthScale(float hunger, bool starving)
		{
			if (starving)
			{
				return -0.5f;
			}
			if (hunger >= 75f)
			{
				return 1f;
			}
			return 1f - 0.5f * (75f - hunger) / 75f;
		}

		private static NextMeatBreakdown EmptyBreakdown(string path)
		{
			return new NextMeatBreakdown(0, 0, 0, 0f, 0f, 0f, 0f, 0f, 0f, neutered: false, usedCache: false, satiated: false, path);
		}

		private static float SafeGeneExpr(GameItem item, string geneId)
		{
			try
			{
				return GeneticHelper.GetExpressedValue(item, geneId);
			}
			catch
			{
				return 0f;
			}
		}

		private static bool SafeIsTag(GameItem item, string tagId)
		{
			try
			{
				return item.IsTag(tagId);
			}
			catch
			{
				return false;
			}
		}

		internal static string FormatNextMeatBreakdown(GameItem item, NextMeatBreakdown b)
		{
			string value = DisplayName(item);
			int value2 = 0;
			try
			{
				value2 = item.uniqueId;
			}
			catch
			{
			}
			return string.Join("\n", $"  rat={value} id={value2} neutered={b.Neutered}", $"  growth={b.GrowthExpr:0.###} hungerRate={b.HungerRateExpr:0.###} weight={b.Weight:0.###} age={b.AgeNow}->{b.AgeOvernight}", $"  hunger {b.HungerCur:0.#}/{b.HungerMax:0.#} -> proj {b.HungerProj:0.#} satiated={b.Satiated} cache={b.UsedCache}", $"  vanillaWC={b.Vanilla} uplift={b.Uplift} finalNextMeat={b.Final} path={b.Path}");
		}

		private static CageFeedPool BuildFeedPool(GameItem ratInCage)
		{
			CageFeedPool cageFeedPool = new CageFeedPool();
			try
			{
				string storeTag = "";
				try
				{
					AnimalSpecies species = HusbandryHelper.GetSpecies(ratInCage);
					if (species != null)
					{
						storeTag = MachineFeedDispenser.GetStoredTag(species.speciesID) ?? "";
					}
				}
				catch
				{
				}
				HashSet<int> seenInv = new HashSet<int>();
				GameInventory parentInventory = ratInCage.parentInventory;
				CollectFromInventory(parentInventory, storeTag, cageFeedPool, seenInv);
				GameItem val = ((parentInventory != null) ? parentInventory.GetParentItem() : null);
				if (val != null)
				{
					try
					{
						List<GraphNodeStorage> children = val.children;
						if (children != null)
						{
							for (int i = 0; i < children.Count; i++)
							{
								try
								{
									GraphNodeStorage val2 = children[i];
									GameItem val3 = ((val2 != null) ? ((Il2CppObjectBase)val2).TryCast<GameItem>() : null);
									if (val3 != null)
									{
										CollectDispenserOnItem(val3, storeTag, cageFeedPool);
										continue;
									}
									GameInventory val4 = ((val2 != null) ? ((Il2CppObjectBase)val2).TryCast<GameInventory>() : null);
									if (val4 != null)
									{
										CollectFromInventory(val4, storeTag, cageFeedPool, seenInv);
									}
								}
								catch
								{
								}
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			return cageFeedPool;
		}

		private static void CollectFromInventory(GameInventory? inv, string storeTag, CageFeedPool pool, HashSet<int> seenInv)
		{
			if (inv == null)
			{
				return;
			}
			try
			{
				int num = ((Object)inv).GetHashCode();
				try
				{
					GameItem parentItem = inv.GetParentItem();
					num = ((parentItem != null) ? parentItem.uniqueId : num);
				}
				catch
				{
				}
				if (!seenInv.Add(num))
				{
					return;
				}
				List<GameItem> childItems;
				try
				{
					childItems = inv.childItems;
				}
				catch
				{
					return;
				}
				if (childItems == null)
				{
					return;
				}
				for (int i = 0; i < childItems.Count; i++)
				{
					GameItem val = null;
					try
					{
						val = childItems[i];
					}
					catch
					{
						continue;
					}
					if (val != null)
					{
						CollectDispenserOnItem(val, storeTag, pool);
						AddLoosePortions(val, pool);
					}
				}
			}
			catch
			{
			}
		}

		private static void CollectDispenserOnItem(GameItem item, string storeTag, CageFeedPool pool)
		{
			try
			{
				if (!item.IsTag("FEED_DISPENSER_TAG"))
				{
					return;
				}
				int num = 0;
				if (!string.IsNullOrEmpty(storeTag))
				{
					TagState tagReadonly = item.GetTagReadonly(storeTag);
					if (tagReadonly != null)
					{
						num = Math.Max(0, tagReadonly.valueInt);
					}
				}
				if (num <= 0)
				{
					TagState tagReadonly2 = item.GetTagReadonly("STORED_KCAL_TAG");
					if (tagReadonly2 != null)
					{
						num = Math.Max(0, tagReadonly2.valueInt);
					}
				}
				if (num > 0)
				{
					pool.Dispensers.Add(new FeedSource
					{
						Remaining = num
					});
				}
			}
			catch
			{
			}
		}

		private static void AddLoosePortions(GameItem item, CageFeedPool pool)
		{
			if (!IsLooseEdible(item))
			{
				return;
			}
			int num = CalorieValueInt(item);
			if (num <= 0)
			{
				return;
			}
			int num2 = 1;
			try
			{
				if (item.unitCount > 0)
				{
					num2 = item.unitCount;
				}
			}
			catch
			{
			}
			for (int i = 0; i < num2; i++)
			{
				pool.LoosePortions.Add(num);
			}
		}

		private static float SimulateRatFeed(GameItem rat, float current, float maxH, float threshold, CageFeedPool pool)
		{
			if (current >= threshold - 0.01f)
			{
				return current;
			}
			float num = current;
			int num2 = Math.Max(0, (int)Math.Ceiling(threshold - num));
			if (num2 > 0 && pool.Dispensers.Count > 0)
			{
				int num3 = num2;
				num2 = TakeFromSources(num2, pool.Dispensers);
				num += (float)(num3 - num2);
			}
			if (num >= threshold - 0.01f)
			{
				return num;
			}
			if (pool.LooseIndex < pool.LoosePortions.Count)
			{
				num += (float)pool.LoosePortions[pool.LooseIndex];
				pool.LooseIndex++;
			}
			return num;
		}

		private static int TakeFromSources(int need, List<FeedSource> sources)
		{
			if (need <= 0)
			{
				return 0;
			}
			for (int i = 0; i < sources.Count; i++)
			{
				if (need <= 0)
				{
					break;
				}
				FeedSource feedSource = sources[i];
				if (feedSource.Remaining > 0)
				{
					int num = Math.Min(need, feedSource.Remaining);
					feedSource.Remaining -= num;
					need -= num;
				}
			}
			return need;
		}

		internal static int PregnantDays(GameItem item)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly("ANIMAL_PREGNANT_PROGRESS_TAG");
				if (tagReadonly == null)
				{
					return 0;
				}
				return Math.Max(0, tagReadonly.valueInt);
			}
			catch
			{
				return 0;
			}
		}

		internal static int GestationDays(GameItem item)
		{
			try
			{
				TagState tagReadonly = item.GetTagReadonly("ANIMAL_GESTATION_PERIOD_TAG");
				if (tagReadonly == null)
				{
					return 0;
				}
				int valueInt = tagReadonly.valueInt;
				if (valueInt > 0)
				{
					return valueInt;
				}
				float valueFloat = tagReadonly.valueFloat;
				return (valueFloat > 0f) ? ((int)Math.Round(valueFloat)) : 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static string DisplayName(GameItem item)
		{
			try
			{
				string displayName = item.GetDisplayName(false);
				if (!string.IsNullOrEmpty(displayName) && displayName != "No Name")
				{
					return displayName;
				}
			}
			catch
			{
			}
			try
			{
				string name = item.name;
				if (!string.IsNullOrEmpty(name) && name != "No Name")
				{
					return name;
				}
			}
			catch
			{
			}
			try
			{
				return item.identifier ?? "鼠";
			}
			catch
			{
				return "?";
			}
		}

		internal static bool CanSeeAlleles()
		{
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				return instance != null && instance.isGeneScannerScanning;
			}
			catch
			{
				return false;
			}
		}

		internal static bool TryReadAlleles(GameItem item, string geneId, out float alleleA, out float alleleB)
		{
			alleleA = 0f;
			alleleB = 0f;
			if (item == null || string.IsNullOrEmpty(geneId))
			{
				return false;
			}
			bool flag = false;
			bool flag2 = false;
			try
			{
				TagState tagReadonly = item.GetTagReadonly(geneId + "_ALLELE_A");
				if (tagReadonly != null)
				{
					alleleA = tagReadonly.valueFloat;
					if (alleleA == 0f)
					{
						alleleA = tagReadonly.GetFloat();
					}
					flag = true;
				}
			}
			catch
			{
			}
			try
			{
				TagState tagReadonly2 = item.GetTagReadonly(geneId + "_ALLELE_B");
				if (tagReadonly2 != null)
				{
					alleleB = tagReadonly2.valueFloat;
					if (alleleB == 0f)
					{
						alleleB = tagReadonly2.GetFloat();
					}
					flag2 = true;
				}
			}
			catch
			{
			}
			return flag & flag2;
		}

		internal static float SortKey(GameItem item, string geneId, bool includeAlleles, bool descending)
		{
			if (string.Equals(geneId, "ANIMAL_AGE_TAG", StringComparison.Ordinal))
			{
				return AgeDays(item);
			}
			if (string.Equals(geneId, "ANIMAL_PREGNANT_PROGRESS_TAG", StringComparison.Ordinal))
			{
				if (!IsPregnant(item))
				{
					return -1f;
				}
				return PregnantDays(item);
			}
			if (string.Equals(geneId, "ANIMAL_WEIGHT_TAG", StringComparison.Ordinal))
			{
				return Weight(item);
			}
			if (includeAlleles && CanSeeAlleles() && TryReadAlleles(item, geneId, out var alleleA, out var alleleB))
			{
				if (!descending)
				{
					return Math.Min(alleleA, alleleB);
				}
				return Math.Max(alleleA, alleleB);
			}
			return SafeGeneExpr(item, geneId);
		}

		internal static bool IsGeneSortId(string? id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			if (string.Equals(id, "ANIMAL_AGE_TAG", StringComparison.Ordinal) || string.Equals(id, "ANIMAL_PREGNANT_PROGRESS_TAG", StringComparison.Ordinal) || string.Equals(id, "ANIMAL_WEIGHT_TAG", StringComparison.Ordinal))
			{
				return false;
			}
			return true;
		}

		internal static IReadOnlyList<string> GeneIdsForUi()
		{
			try
			{
				AnimalSpecies rat = AnimalSpecies.Rat;
				if (((rat != null) ? rat.geneticTraitIDs : null) != null && rat.geneticTraitIDs.Count > 0)
				{
					List<string> list = new List<string>();
					Enumerator<string> enumerator = rat.geneticTraitIDs.GetEnumerator();
					while (enumerator.MoveNext())
					{
						string current = enumerator.Current;
						if (!string.IsNullOrEmpty(current))
						{
							list.Add(current);
						}
					}
					if (list.Count > 0)
					{
						return list;
					}
				}
			}
			catch
			{
			}
			return DefaultGeneIds;
		}

		internal static bool TryReadPartnerAlleles(GameItem female, string geneId, out float alleleA, out float alleleB)
		{
			alleleA = 0f;
			alleleB = 0f;
			if (female == null || string.IsNullOrEmpty(geneId))
			{
				return false;
			}
			bool flag = false;
			bool flag2 = false;
			try
			{
				TagState tagReadonly = female.GetTagReadonly(geneId + "_PARTNER_ALLELE_A");
				if (tagReadonly != null)
				{
					alleleA = tagReadonly.valueFloat;
					if (alleleA == 0f)
					{
						alleleA = tagReadonly.GetFloat();
					}
					flag = true;
				}
			}
			catch
			{
			}
			try
			{
				TagState tagReadonly2 = female.GetTagReadonly(geneId + "_PARTNER_ALLELE_B");
				if (tagReadonly2 != null)
				{
					alleleB = tagReadonly2.valueFloat;
					if (alleleB == 0f)
					{
						alleleB = tagReadonly2.GetFloat();
					}
					flag2 = true;
				}
			}
			catch
			{
			}
			return flag | flag2;
		}

		internal static bool HasPartnerGenes(GameItem female)
		{
			if (female == null || !IsPregnant(female))
			{
				return false;
			}
			AnimalSpecies species = null;
			try
			{
				species = HusbandryHelper.GetSpecies(female);
			}
			catch
			{
			}
			foreach (string item in EnumerateGeneIds(species))
			{
				if (TryReadPartnerAlleles(female, item, out var _, out var _))
				{
					return true;
				}
			}
			return false;
		}

		internal static string BuildPartnerGeneReport(GameItem female)
		{
			if (female == null)
			{
				return "无效目标";
			}
			if (!IsPregnant(female))
			{
				return "未怀孕";
			}
			List<string> list = new List<string> { "配对公鼠基因（保存在母鼠身上）", "" };
			AnimalSpecies species = null;
			try
			{
				species = HusbandryHelper.GetSpecies(female);
			}
			catch
			{
			}
			bool flag = false;
			foreach (string item in EnumerateGeneIds(species))
			{
				if (TryReadPartnerAlleles(female, item, out var alleleA, out var alleleB))
				{
					flag = true;
					float value = (alleleA + alleleB) * 0.5f;
					list.Add($"{GeneDisplayName(item)}: A={FormatGeneValue(alleleA)} B={FormatGeneValue(alleleB)}（表达≈{FormatGeneValue(value)}）");
				}
			}
			if (!flag)
			{
				list.Add("无记录（可能在此模组加载前怀孕，或配对基因已被清除）");
			}
			return string.Join("\n", list);
		}

		internal static List<(string geneId, string name, float a, float b)> ListPartnerGenes(GameItem female)
		{
			List<(string, string, float, float)> list = new List<(string, string, float, float)>();
			if (female == null || !IsPregnant(female))
			{
				return list;
			}
			AnimalSpecies species = null;
			try
			{
				species = HusbandryHelper.GetSpecies(female);
			}
			catch
			{
			}
			foreach (string item in EnumerateGeneIds(species))
			{
				if (TryReadPartnerAlleles(female, item, out var alleleA, out var alleleB) && HasBothPartnerAlleles(female, item))
				{
					list.Add((item, GeneDisplayName(item), alleleA, alleleB));
				}
			}
			return list;
		}

		internal static bool HasBothPartnerAlleles(GameItem female, string geneId)
		{
			try
			{
				TagState tagReadonly = female.GetTagReadonly(geneId + "_PARTNER_ALLELE_A");
				TagState tagReadonly2 = female.GetTagReadonly(geneId + "_PARTNER_ALLELE_B");
				return tagReadonly != null && tagReadonly2 != null;
			}
			catch
			{
				return false;
			}
		}

		internal static bool TryLockPartnerAllele(GameItem female, string geneId, bool keepA, out string message)
		{
			message = "";
			if (female == null || string.IsNullOrEmpty(geneId))
			{
				message = "无效目标";
				return false;
			}
			if (!IsPregnant(female))
			{
				message = "未怀孕";
				return false;
			}
			if (!TryReadPartnerAlleles(female, geneId, out var alleleA, out var alleleB) || !HasBothPartnerAlleles(female, geneId))
			{
				message = "缺少配对基因记录";
				return false;
			}
			float value = (keepA ? alleleA : alleleB);
			if (Math.Abs(alleleA - alleleB) < 1E-06f)
			{
				message = "该基因 A/B 已相同，无需选择";
				return false;
			}
			if (!TryWritePartnerFloat(female, geneId + "_PARTNER_ALLELE_A", value) || !TryWritePartnerFloat(female, geneId + "_PARTNER_ALLELE_B", value))
			{
				message = "写入失败";
				return false;
			}
			string value2 = (keepA ? "A" : "B");
			message = $"{GeneDisplayName(geneId)} 已选公鼠{value2}（A=B={FormatGeneValue(value)}）";
			return true;
		}

		private static bool TryWritePartnerFloat(GameItem female, string tagId, float value)
		{
			try
			{
				female.ModifyTag(tagId, Action<TagState>.op_Implicit((Action<TagState>)((TagState t) =>
				{
					t.SetFloat(value);
					try
					{
						t.Enable();
					}
					catch
					{
					}
				})), true);
				return true;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("WritePartner " + tagId + ": " + ex.Message);
				return false;
			}
		}

		internal static int CountGeneProbesInStore()
		{
			if (!GameSession.TryGetStore(out PlayerStore store))
			{
				return 0;
			}
			int num = 0;
			try
			{
				List<GameItem> val = store.FindAllItem(true);
				if (val == null)
				{
					return 0;
				}
				Enumerator<GameItem> enumerator = val.GetEnumerator();
				while (enumerator.MoveNext())
				{
					if (GeneProbeItem.IsProbe(enumerator.Current))
					{
						num++;
					}
				}
				return num;
			}
			catch
			{
				return 0;
			}
		}

		internal static bool TryConsumeOneGeneProbe(out string message)
		{
			message = "";
			if (!GameSession.TryGetStore(out PlayerStore store))
			{
				message = "商店未就绪";
				return false;
			}
			GameItem val = null;
			try
			{
				List<GameItem> val2 = store.FindAllItem(true);
				if (val2 != null)
				{
					Enumerator<GameItem> enumerator = val2.GetEnumerator();
					while (enumerator.MoveNext())
					{
						GameItem current = enumerator.Current;
						if (GeneProbeItem.IsProbe(current))
						{
							val = current;
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				message = "查找探针失败：" + ex.Message;
				return false;
			}
			if (val == null)
			{
				message = "背包中没有基因编辑探针";
				return false;
			}
			try
			{
				GameInventory parentInventory = val.parentInventory;
				if (parentInventory != null)
				{
					try
					{
						parentInventory.Expel(val);
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			try
			{
				val.Destroy();
				message = "已消耗 1 个基因编辑探针";
				return true;
			}
			catch (Exception ex2)
			{
				message = "消耗探针失败：" + ex2.Message;
				return false;
			}
		}

		private static IEnumerable<string> EnumerateGeneIds(AnimalSpecies? species)
		{
			List<string> list = new List<string>();
			try
			{
				List<string> val = ((species != null) ? species.geneticTraitIDs : null);
				if (val != null)
				{
					Enumerator<string> enumerator = val.GetEnumerator();
					while (enumerator.MoveNext())
					{
						string current = enumerator.Current;
						if (!string.IsNullOrEmpty(current))
						{
							list.Add(current);
						}
					}
				}
			}
			catch
			{
			}
			if (list.Count == 0)
			{
				list.AddRange(DefaultGeneIds);
			}
			return list;
		}

		internal static string GeneDisplayName(string geneId)
		{
			if (string.Equals(geneId, "ANIMAL_AGE_TAG", StringComparison.Ordinal))
			{
				return "年龄";
			}
			if (string.Equals(geneId, "ANIMAL_PREGNANT_PROGRESS_TAG", StringComparison.Ordinal))
			{
				return "怀孕天数";
			}
			if (string.Equals(geneId, "ANIMAL_WEIGHT_TAG", StringComparison.Ordinal))
			{
				return "克重";
			}
			if (GeneLocKeys.TryGetValue(geneId, out string value) && !string.IsNullOrEmpty(value))
			{
				try
				{
					string localizedMechanic = LocHelper.GetLocalizedMechanic(value, Array.Empty<Object>());
					localizedMechanic = CleanGeneLabel(localizedMechanic);
					if (!string.IsNullOrEmpty(localizedMechanic))
					{
						return localizedMechanic;
					}
				}
				catch
				{
				}
			}
			if (geneId == "ANIMAL_FERTILITY_TAG")
			{
				return "繁殖力";
			}
			return geneId;
		}

		private static string CleanGeneLabel(string? loc)
		{
			if (string.IsNullOrEmpty(loc))
			{
				return "";
			}
			string text = loc;
			int num = text.IndexOf('{');
			if (num >= 0)
			{
				text = text.Substring(0, num);
			}
			text = text.Trim();
			while (text.Length > 0)
			{
				char c = text[text.Length - 1];
				if (c != ':' && c != '：' && c != '%' && c != ' ')
				{
					break;
				}
				text = text.Substring(0, text.Length - 1).TrimEnd();
			}
			return text;
		}

		private static string FormatGeneValue(float value)
		{
			return value.ToString("0.###");
		}

		internal static string FormatPartnerGeneEditLine(string name, string geneId, float a, float b)
		{
			if (!(Math.Abs(a - b) < 1E-06f))
			{
				bool flag = string.Equals(geneId, "ANIMAL_HUNGER_RATE_TAG", StringComparison.Ordinal);
				bool flag2 = (flag ? (a < b) : (a > b));
				bool flag3 = (flag ? (b < a) : (b > a));
				string value = FormatGeneValue(a);
				string value2 = FormatGeneValue(b);
				if (flag2)
				{
					value = GreenGeneValue(value);
				}
				if (flag3)
				{
					value2 = GreenGeneValue(value2);
				}
				return $"{name}  A={value}  B={value2}";
			}
			return $"{name}  A={FormatGeneValue(a)}  B={FormatGeneValue(b)}  （已相同）";
		}

		private static string GreenGeneValue(string value)
		{
			return "<color=#39D147>" + value + "</color>";
		}

		internal static bool Abort(GameItem female)
		{
			if (female == null || !IsPregnant(female))
			{
				return false;
			}
			try
			{
				female.DisableTag("ANIMAL_PREGNANT_TAG", true);
			}
			catch
			{
				return false;
			}
			try
			{
				female.ModifyTag("ANIMAL_PREGNANT_PROGRESS_TAG", Action<TagState>.op_Implicit((Action<TagState>)((TagState t) =>
				{
					t.SetInt(0);
					t.SetFloat(0f);
					t.Disable();
				})), true);
			}
			catch
			{
				try
				{
					female.DisableTag("ANIMAL_PREGNANT_PROGRESS_TAG", true);
				}
				catch
				{
				}
			}
			try
			{
				AnimalSpecies species = HusbandryHelper.GetSpecies(female);
				if (species != null)
				{
					GeneticHelper.ClearPartnerGenes(female, species);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("ClearPartnerGenes: " + ex.Message);
			}
			Mod.Log.Msg("Aborted pregnancy on " + DisplayName(female));
			return true;
		}
	}
	internal static class RatMonitorFilters
	{
		internal static bool FilterMale;

		internal static bool FilterFemale;

		internal static bool FilterPregnant;

		internal static bool FilterNotPregnant;

		internal static bool FilterNeutered;

		internal static bool FilterIntact;

		internal static bool FilterUnderfed;

		internal static bool FilterFedOk;

		internal static string SortGeneId = "ANIMAL_GROWTH_RATE_TAG";

		internal static bool SortDescending = true;

		internal static bool IncludeAlleles;

		internal static void SyncAlleleGate()
		{
			if (!RatData.CanSeeAlleles())
			{
				IncludeAlleles = false;
			}
		}

		internal static bool Passes(GameItem rat, bool pregnant, bool neutered)
		{
			bool isA = RatData.IsMale(rat);
			bool isB = RatData.IsFemale(rat) | pregnant;
			if (!PassesGroup(FilterMale, FilterFemale, isA, isB))
			{
				return false;
			}
			if (!PassesGroup(FilterPregnant, FilterNotPregnant, pregnant, !pregnant))
			{
				return false;
			}
			if (!PassesGroup(FilterNeutered, FilterIntact, neutered, !neutered))
			{
				return false;
			}
			bool flag = RatData.WillUnderfeedTonight(rat);
			if (!PassesGroup(FilterUnderfed, FilterFedOk, flag, !flag))
			{
				return false;
			}
			return true;
		}

		private static bool PassesGroup(bool optA, bool optB, bool isA, bool isB)
		{
			if (!optA && !optB)
			{
				return true;
			}
			if (optA & isA)
			{
				return true;
			}
			if (optB & isB)
			{
				return true;
			}
			return false;
		}

		internal static int Compare(GameItem a, GameItem b)
		{
			SyncAlleleGate();
			float num = RatData.SortKey(a, SortGeneId, IncludeAlleles, SortDescending);
			float value = RatData.SortKey(b, SortGeneId, IncludeAlleles, SortDescending);
			int num2 = num.CompareTo(value);
			if (!SortDescending)
			{
				return num2;
			}
			return -num2;
		}

		internal static string SortGeneShortLabel()
		{
			try
			{
				return RatData.GeneDisplayName(SortGeneId);
			}
			catch
			{
				return SortGeneId;
			}
		}

		internal static IReadOnlyList<string> SortOptionIds()
		{
			List<string> list = new List<string>();
			foreach (string item in RatData.GeneIdsForUi())
			{
				list.Add(item);
			}
			list.Add("ANIMAL_AGE_TAG");
			list.Add("ANIMAL_PREGNANT_PROGRESS_TAG");
			list.Add("ANIMAL_WEIGHT_TAG");
			return list;
		}

		internal static List<string> SortGeneDropdownLabels()
		{
			List<string> list = new List<string>();
			foreach (string item in SortOptionIds())
			{
				list.Add(RatData.GeneDisplayName(item));
			}
			return list;
		}

		internal static int SortGeneDropdownIndex()
		{
			IReadOnlyList<string> readOnlyList = SortOptionIds();
			for (int i = 0; i < readOnlyList.Count; i++)
			{
				if (string.Equals(readOnlyList[i], SortGeneId, StringComparison.Ordinal))
				{
					return i;
				}
			}
			return 0;
		}

		internal static void SetSortGeneByIndex(int index)
		{
			IReadOnlyList<string> readOnlyList = SortOptionIds();
			if (index >= 0 && index < readOnlyList.Count)
			{
				SortGeneId = readOnlyList[index];
			}
		}

		internal static bool IsGeneSort()
		{
			return RatData.IsGeneSortId(SortGeneId);
		}
	}
	internal static class RatMonitorHooks
	{
		internal static void Apply(Harmony harmony)
		{
			int num = 0;
			num += Patch(harmony, AccessTools.Method(typeof(PlayerStore), "EndNight", (Type[])null, (Type[])null), "PreEndNight", "PostEndNight");
			num += Patch(harmony, AccessTools.Method(typeof(GameItem), "ModifyTag", new Type[3]
			{
				typeof(string),
				typeof(Action<TagState>),
				typeof(bool)
			}, (Type[])null), null, "PostModifyTag");
			num += Patch(harmony, AccessTools.Method(typeof(GameItem), "EnableTag", new Type[2]
			{
				typeof(string),
				typeof(bool)
			}, (Type[])null), null, "PostEnableTag");
			num += Patch(harmony, AccessTools.Method(typeof(GameItem), "DisableTag", new Type[2]
			{
				typeof(string),
				typeof(bool)
			}, (Type[])null), null, "PostDisableTag");
			num += Patch(harmony, AccessTools.Method(typeof(GameGridInventory), "Expel", (Type[])null, (Type[])null), "PreGridExpel", "PostGridExpel");
			num += Patch(harmony, AccessTools.Method(typeof(GameGridInventory), "UncheckedAccept", (Type[])null, (Type[])null), null, "PostGridAccept");
			num += Patch(harmony, AccessTools.Method(typeof(GameSlotInventory), "Expel", (Type[])null, (Type[])null), "PreSlotExpel", "PostSlotExpel");
			num += Patch(harmony, AccessTools.Method(typeof(GameSlotInventory), "UncheckedAccept", (Type[])null, (Type[])null), null, "PostSlotAccept");
			num += Patch(harmony, AccessTools.Method(typeof(GameInventory), "ExpelAll", (Type[])null, (Type[])null), "PreExpelAll", "PostExpelAll");
			num += Patch(harmony, AccessTools.Method(typeof(GameInventory), "UncheckedAcceptAll", (Type[])null, (Type[])null), null, "PostAcceptAll");
			num += Patch(harmony, AccessTools.Method(typeof(CustomUIManager), "NotifyWindowClosed", (Type[])null, (Type[])null), null, "PostWindowClosed");
			if (num > 0)
			{
				Mod.Log.Msg($"SciRat hooks: {num} patch(es) OK — 格子/槽位/批量 Expel·Accept；过夜 EndNight 全刷");
			}
			else
			{
				Mod.Log.Warning("SciRat hooks: no patches applied");
			}
		}

		private static int Patch(Harmony harmony, MethodInfo? method, string? prefix = null, string? postfix = null)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			if (method == null)
			{
				return 0;
			}
			try
			{
				harmony.Patch((MethodBase)method, (prefix == null) ? ((HarmonyMethod)null) : new HarmonyMethod(typeof(RatMonitorHooks), prefix, (Type[])null), (postfix == null) ? ((HarmonyMethod)null) : new HarmonyMethod(typeof(RatMonitorHooks), postfix, (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("SciRat patched " + method.DeclaringType?.Name + "." + method.Name);
				return 1;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat patch fail " + method.Name + ": " + ex.Message);
				return 0;
			}
		}

		private static void PostWindowClosed(CustomUIWindow window)
		{
			RatMonitorUi.OnNativeWindowClosed(window);
		}

		private static void PreEndNight()
		{
			RatMonitorRefresh.BeginNightBatch();
		}

		private static void PostEndNight()
		{
			RatMonitorRefresh.EndNightBatch();
		}

		private static void PostModifyTag(GameItem __instance, string tagName)
		{
			if (__instance == null)
			{
				return;
			}
			if (RatData.IsRat(__instance))
			{
				if (RatData.IsRefreshTag(tagName))
				{
					RatMonitorRefresh.OnRatAttributeChanged(__instance);
				}
			}
			else if (RatData.AffectsFeedPool(__instance) || RatData.IsCageFeedTag(tagName))
			{
				GameItem val = RatMonitorRefresh.ResolveTrackedHousing(__instance);
				if (val != null)
				{
					RatMonitorRefresh.OnHousingFeedChanged(val);
				}
			}
		}

		private static void PostEnableTag(GameItem __instance, string tagName)
		{
			if (__instance == null)
			{
				return;
			}
			if (RatData.IsRat(__instance))
			{
				if (RatData.IsRefreshTag(tagName))
				{
					RatMonitorRefresh.OnRatAttributeChanged(__instance);
				}
			}
			else if (RatData.AffectsFeedPool(__instance))
			{
				GameItem val = RatMonitorRefresh.ResolveTrackedHousing(__instance);
				if (val != null)
				{
					RatMonitorRefresh.OnHousingFeedChanged(val);
				}
			}
		}

		private static void PostDisableTag(GameItem __instance, string tagName)
		{
			if (__instance == null)
			{
				return;
			}
			if (RatData.IsRat(__instance))
			{
				if (RatData.IsRefreshTag(tagName))
				{
					RatMonitorRefresh.OnRatAttributeChanged(__instance);
				}
			}
			else if (RatData.AffectsFeedPool(__instance))
			{
				GameItem val = RatMonitorRefresh.ResolveTrackedHousing(__instance);
				if (val != null)
				{
					RatMonitorRefresh.OnHousingFeedChanged(val);
				}
			}
		}

		private static void PreGridExpel(GameGridInventory __instance, GameItem item)
		{
			NoteRatExpelledIfNeeded(item);
		}

		private static void PostGridExpel(GameGridInventory __instance, GameItem item, bool __result)
		{
			if (__result && item != null)
			{
				AfterInventoryChange((GameInventory)(object)__instance, item);
			}
		}

		private static void PostGridAccept(GameGridInventory __instance, GameItem item, bool __result)
		{
			if (__result && item != null)
			{
				AfterInventoryChange((GameInventory)(object)__instance, item);
			}
		}

		private static void PreSlotExpel(GameSlotInventory __instance, GameItem item)
		{
			NoteRatExpelledIfNeeded(item);
		}

		private static void PostSlotExpel(GameSlotInventory __instance, GameItem item, bool __result)
		{
			if (__result && item != null)
			{
				AfterInventoryChange((GameInventory)(object)__instance, item);
			}
		}

		private static void PostSlotAccept(GameSlotInventory __instance, GameItem item, bool __result)
		{
			if (__result && item != null)
			{
				AfterInventoryChange((GameInventory)(object)__instance, item);
			}
		}

		private static void PreExpelAll(GameInventory __instance, ref List<GameItem>? __state)
		{
			__state = SnapshotChildItems(__instance);
			foreach (GameItem item in __state)
			{
				NoteRatExpelledIfNeeded(item);
			}
		}

		private static void PostExpelAll(GameInventory __instance, GameInventory __result, List<GameItem>? __state)
		{
			if (__state == null || __state.Count == 0)
			{
				return;
			}
			foreach (GameItem item in __state)
			{
				AfterInventoryChange(__instance, item);
			}
		}

		private static void PostAcceptAll(GameInventory __instance, List<GameItem> items, GameInventory __result)
		{
			if (items == null)
			{
				return;
			}
			for (int i = 0; i < items.Count; i++)
			{
				GameItem val = items[i];
				if (val != null)
				{
					AfterInventoryChange(__instance, val);
				}
			}
		}

		private static void NoteRatExpelledIfNeeded(GameItem? item)
		{
			if (item != null && RatData.IsRat(item))
			{
				RatMonitorRefresh.NoteRatExpelled(item);
			}
		}

		private static void AfterInventoryChange(GameInventory inv, GameItem item)
		{
			RatMonitorUi.OnHighlightedItemInventoryChanged(item);
			if (RatData.IsRat(item))
			{
				RatMonitorRefresh.OnRatInventoryChanged(item);
			}
			else if (RatData.AffectsFeedPool(item))
			{
				GameItem val = RatMonitorRefresh.ResolveTrackedHousing(item) ?? RatMonitorRefresh.ResolveTrackedHousing(inv);
				if (val != null)
				{
					RatMonitorRefresh.OnHousingFeedChanged(val);
				}
			}
		}

		private static List<GameItem> SnapshotChildItems(GameInventory? inv)
		{
			List<GameItem> list = new List<GameItem>();
			if (inv == null)
			{
				return list;
			}
			try
			{
				List<GameItem> childItems = inv.childItems;
				if (childItems == null)
				{
					return list;
				}
				for (int i = 0; i < childItems.Count; i++)
				{
					GameItem val = childItems[i];
					if (val != null)
					{
						list.Add(val);
					}
				}
			}
			catch
			{
			}
			return list;
		}
	}
	internal static class RatMonitorRefresh
	{
		private static int _nightBatch;

		private static int _flushDepth;

		private static readonly Dictionary<int, GameItem> _pendingRats = new Dictionary<int, GameItem>();

		private static readonly Dictionary<int, GameItem> _pendingCages = new Dictionary<int, GameItem>();

		private static readonly HashSet<int> _trackedHousingIds = new HashSet<int>();

		private static readonly Dictionary<int, int> _ratOldHousing = new Dictionary<int, int>();

		internal static bool IsNightBatch => _nightBatch > 0;

		internal static void BeginNightBatch()
		{
			_nightBatch++;
			_pendingRats.Clear();
			_pendingCages.Clear();
		}

		internal static void EndNightBatch()
		{
			if (_nightBatch > 0)
			{
				_nightBatch--;
			}
			_pendingRats.Clear();
			_pendingCages.Clear();
			if (CanTouchUi())
			{
				RatMonitorUi.RequestFullRefresh();
			}
		}

		internal static void RebuildTrackedHousing()
		{
			_trackedHousingIds.Clear();
			if (!RatMonitorUi.IsPanelVisible)
			{
				return;
			}
			foreach (GameItem cachedRat in RatMonitorUi.CachedRats)
			{
				int cageId = RatData.GetCageId(cachedRat);
				if (cageId != 0)
				{
					_trackedHousingIds.Add(cageId);
				}
			}
		}

		internal static void ClearTrackedHousing()
		{
			_trackedHousingIds.Clear();
			_ratOldHousing.Clear();
		}

		internal static GameItem? ResolveTrackedHousing(GameItem? item)
		{
			if (item == null || _trackedHousingIds.Count == 0)
			{
				return null;
			}
			try
			{
				GameItem val = item;
				while (val != null)
				{
					int uniqueId = val.uniqueId;
					if (uniqueId != 0 && _trackedHousingIds.Contains(uniqueId))
					{
						return val;
					}
					GameInventory parentInventory = val.parentInventory;
					val = ((parentInventory != null) ? parentInventory.GetParentItem() : null);
				}
			}
			catch
			{
			}
			return null;
		}

		internal static GameItem? ResolveTrackedHousing(GameInventory? inventory)
		{
			if (inventory == null)
			{
				return null;
			}
			try
			{
				return ResolveTrackedHousing(inventory.GetParentItem());
			}
			catch
			{
				return null;
			}
		}

		internal static void NoteRatExpelled(GameItem? rat)
		{
			if (rat == null || !RatData.IsRat(rat))
			{
				return;
			}
			try
			{
				int uniqueId = rat.uniqueId;
				if (uniqueId != 0)
				{
					_ratOldHousing[uniqueId] = RatData.GetCageId(rat);
				}
			}
			catch
			{
			}
		}

		internal static void OnRatInventoryChanged(GameItem? rat)
		{
			if (!CanEnqueue() || rat == null || !RatData.IsRat(rat))
			{
				return;
			}
			try
			{
				int uniqueId = rat.uniqueId;
				if (uniqueId != 0)
				{
					int value = 0;
					if (_ratOldHousing.TryGetValue(uniqueId, out value))
					{
						_ratOldHousing.Remove(uniqueId);
					}
					RebuildTrackedHousing();
					_pendingRats[uniqueId] = rat;
					if (value != 0)
					{
						EnqueueTrackedHousing(value);
					}
					int cageId = RatData.GetCageId(rat);
					if (cageId != 0 && cageId != value)
					{
						EnqueueTrackedHousing(cageId);
					}
				}
			}
			catch
			{
			}
		}

		internal static void OnRatAttributeChanged(GameItem? rat)
		{
			if (!CanEnqueue() || rat == null || !RatData.IsRat(rat))
			{
				return;
			}
			try
			{
				int uniqueId = rat.uniqueId;
				if (uniqueId != 0)
				{
					_pendingRats[uniqueId] = rat;
				}
			}
			catch
			{
			}
		}

		internal static void OnHousingFeedChanged(GameItem? housing)
		{
			if (!CanEnqueue() || housing == null)
			{
				return;
			}
			try
			{
				int uniqueId = housing.uniqueId;
				if (uniqueId != 0 && _trackedHousingIds.Contains(uniqueId))
				{
					_pendingCages[uniqueId] = housing;
				}
			}
			catch
			{
			}
		}

		private static void EnqueueTrackedHousing(int housingId)
		{
			if (housingId == 0 || !_trackedHousingIds.Contains(housingId))
			{
				return;
			}
			foreach (GameItem cachedRat in RatMonitorUi.CachedRats)
			{
				if (RatData.GetCageId(cachedRat) != housingId)
				{
					continue;
				}
				try
				{
					GameInventory parentInventory = cachedRat.parentInventory;
					GameItem val = ((parentInventory != null) ? parentInventory.GetParentItem() : null);
					if (val != null && val.uniqueId == housingId)
					{
						_pendingCages[housingId] = val;
						break;
					}
				}
				catch
				{
				}
			}
		}

		internal static void FlushPending()
		{
			if (IsNightBatch || _flushDepth > 0)
			{
				return;
			}
			if (!CanTouchUi())
			{
				_pendingRats.Clear();
				_pendingCages.Clear();
			}
			else
			{
				if (_pendingRats.Count == 0 && _pendingCages.Count == 0)
				{
					return;
				}
				_flushDepth++;
				try
				{
					List<GameItem> list = new List<GameItem>(_pendingCages.Values);
					_pendingCages.Clear();
					HashSet<int> hashSet = new HashSet<int>();
					foreach (GameItem item in list)
					{
						RatMonitorUi.RefreshCage(item);
						try
						{
							int uniqueId = item.uniqueId;
							if (uniqueId != 0)
							{
								hashSet.Add(uniqueId);
							}
						}
						catch
						{
						}
					}
					List<GameItem> list2 = new List<GameItem>();
					foreach (GameItem value in _pendingRats.Values)
					{
						if (!hashSet.Contains(RatData.GetCageId(value)))
						{
							list2.Add(value);
						}
					}
					_pendingRats.Clear();
					foreach (GameItem item2 in list2)
					{
						RatMonitorUi.RefreshRat(item2);
					}
				}
				finally
				{
					_flushDepth--;
				}
			}
		}

		private static bool CanEnqueue()
		{
			if (CanTouchUi() && !IsNightBatch)
			{
				return _flushDepth == 0;
			}
			return false;
		}

		private static bool CanTouchUi()
		{
			if (GameSession.CanTouchGame && RatMonitorUi.IsPanelVisible)
			{
				return !RatMonitorUi.IsRefreshing;
			}
			return false;
		}
	}
	internal static class RatMonitorUi
	{
		private sealed class Row
		{
			public GameItem Rat;

			public string Name = "";

			public string Sex = "";

			public int Age;

			public float Weight;

			public int NextMeat;

			public bool Pregnant;

			public int PregDays;

			public int Gestation;

			public bool Neutered;

			public bool Underfed;

			public string GeneText = "";
		}

		private const string WindowId = "sci_rat_monitor";

		private const string PrenatalWindowId = "sci_rat_prenatal";

		private const string GripName = "sr_resize_grip";

		private const string ZoomBtnName = "sr_zoom_btn";

		private const float HeaderH = 140f;

		private const float CellH = 40f;

		private const float ColMin = 540f;

		private const float Spacing = 6f;

		private const float GripSize = 16f;

		private const float MinWidth = 480f;

		private const float MinHeight = 200f;

		private static readonly float[] ZoomScales = new float[3] { 1f, 1.5f, 2f };

		private static readonly string[] ZoomSpriteNames = new string[3] { "button_zoom_0", "button_zoom_1", "button_zoom_2" };

		private static readonly List<Action> RootedActions = new List<Action>();

		private static readonly List<Action> PrenatalActions = new List<Action>();

		private static readonly List<Action<bool>> RootedBoolActions = new List<Action<bool>>();

		private static readonly List<Action<int>> RootedIntActions = new List<Action<int>>();

		private static UnityAction? _closeUnity;

		private static UnityAction? _zoomUnity;

		private static readonly Sprite?[] ZoomSprites = new Sprite[3];

		private static bool _zoomSpritesTried;

		private static GameItem? _prenatalFemale;

		private static string _prenatalStatus = "";

		private static bool _visible;

		private static bool _inputOk = true;

		private static bool _inputWarned;

		private static Sprite? _gripSprite;

		private static RectTransform? _gripRt;

		private static bool _resizing;

		private static bool _hiding;

		private static bool _rebuilding;

		private static int _refreshDepth;

		private static readonly List<GameItem> _cachedRats = new List<GameItem>();

		private static HighlightManager? _highlight;

		private static readonly List<GameInventory> _highlightInvs = new List<GameInventory>();

		private static int _highlightUid;

		private static Vector2 _resizeMouse0;

		private static Vector2 _resizeSize0;

		private static Vector2 _resizePos0;

		private static Vector2 _resizePivot;

		internal static bool IsRefreshing => _refreshDepth > 0;

		internal static bool IsPanelVisible => _visible;

		internal static IReadOnlyList<GameItem> CachedRats => _cachedRats;

		internal static void RequestFullRefresh()
		{
			if (GameSession.CanTouchGame && _visible)
			{
				Refresh();
			}
		}

		internal static void RefreshRat(GameItem rat)
		{
			if (!GameSession.CanTouchGame || !_visible || _refreshDepth > 0 || rat == null || !RatData.IsRat(rat))
			{
				return;
			}
			CustomUIManager instance = CustomUIManager.Instance;
			if ((Object)(object)instance == (Object)null || !instance.IsOpen("sci_rat_monitor"))
			{
				return;
			}
			RatData.DropRatProjection(rat);
			int cageId = RatData.GetCageId(rat);
			if (cageId != 0)
			{
				List<GameItem> list = new List<GameItem>();
				foreach (GameItem cachedRat in _cachedRats)
				{
					if (RatData.GetCageId(cachedRat) == cageId)
					{
						list.Add(cachedRat);
					}
				}
				if (list.Count > 0)
				{
					RatData.RebuildCageProjection(list);
				}
			}
			Row row = BuildRow(rat);
			if (NeedsRowLayoutRebuild(instance, row))
			{
				RequestFullRefresh();
			}
			else
			{
				PatchRatRow(instance, row);
			}
		}

		internal static void RefreshCage(GameItem? cage)
		{
			if (!GameSession.CanTouchGame || !_visible || _refreshDepth > 0 || cage == null)
			{
				return;
			}
			CustomUIManager instance = CustomUIManager.Instance;
			if ((Object)(object)instance == (Object)null || !instance.IsOpen("sci_rat_monitor"))
			{
				return;
			}
			int uniqueId;
			try
			{
				uniqueId = cage.uniqueId;
			}
			catch
			{
				return;
			}
			if (uniqueId == 0)
			{
				return;
			}
			List<GameItem> list = new List<GameItem>();
			foreach (GameItem cachedRat in _cachedRats)
			{
				if (RatData.GetCageId(cachedRat) == uniqueId)
				{
					list.Add(cachedRat);
				}
			}
			if (list.Count == 0)
			{
				return;
			}
			RatData.RebuildCageProjection(list);
			foreach (GameItem item in list)
			{
				PatchRatRow(instance, BuildRow(item));
			}
		}

		internal static void Tick()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			if (_inputOk)
			{
				try
				{
					if (Input.GetKeyDown(Mod.ReadToggle()))
					{
						_visible = !_visible;
						if (!_visible)
						{
							HideFromUi();
						}
						else
						{
							RequestFullRefresh();
						}
					}
				}
				catch
				{
					if (!_inputWarned)
					{
						_inputWarned = true;
						_inputOk = false;
						Mod.Log.Warning("SciRat: Input not ready");
					}
				}
			}
			try
			{
				HandleResize();
			}
			catch (Exception ex)
			{
				_resizing = false;
				Mod.Log.Warning("SciRat resize: " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		internal static void OnNativeWindowClosed(CustomUIWindow? window)
		{
			if (_hiding || _rebuilding || _resizing || !_visible)
			{
				return;
			}
			try
			{
				if (window == null || window.id != "sci_rat_monitor")
				{
					return;
				}
			}
			catch
			{
				return;
			}
			HideFromUi();
		}

		private static void Refresh()
		{
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_022d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0266: Unknown result type (might be due to invalid IL or missing references)
			//IL_026b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_0283: Unknown result type (might be due to invalid IL or missing references)
			CustomUIManager instance = CustomUIManager.Instance;
			if ((Object)(object)instance == (Object)null || (Object)(object)instance.buttonPrefab == (Object)null || (Object)(object)instance.windowPrefab == (Object)null)
			{
				return;
			}
			SaveWindowPos(instance);
			if (!_visible || !GameSession.TryGetStore(out PlayerStore _))
			{
				_rebuilding = true;
				try
				{
					if (instance.IsOpen("sci_rat_monitor"))
					{
						instance.CloseWindow("sci_rat_monitor");
					}
				}
				finally
				{
					_rebuilding = false;
				}
				_gripRt = null;
				_resizing = false;
				return;
			}
			if (_resizing)
			{
				EnsureGrip(instance);
				return;
			}
			List<Row> list = CollectRows();
			RatMonitorRefresh.RebuildTrackedHousing();
			float num = CurrentWidth();
			float num2 = CurrentHeight(list.Count);
			_rebuilding = true;
			try
			{
				if (instance.IsOpen("sci_rat_monitor"))
				{
					instance.CloseWindow("sci_rat_monitor");
				}
				_gripRt = null;
				RootedActions.Clear();
				RootedBoolActions.Clear();
				RootedIntActions.Clear();
				RatMonitorFilters.SyncAlleleGate();
				CustomUIBuilder val = instance.CreateWindow("sci_rat_monitor", "科学养鼠", "overlay").SetDraggable(true).SetCloseOnEscape(false)
					.SetSize(num, num2);
				val.AddLabel(((object)Mod.ReadToggle()/*cast due to constrained. prefix*/).ToString() + "  明日增肉=先+1岁再算（对齐过夜）；散装=饥饿鼠各吃一份", "sr_hint");
				AddToolbar(val);
				if (list.Count == 0)
				{
					val.AddLabel("没有符合筛选的老鼠", "sr_empty");
				}
				else
				{
					float num3 = Math.Max(40f, num2 - 140f - 28f);
					bool flag = (float)list.Count * 46f > num3 + 2f;
					if (flag)
					{
						val.BeginScroll(num3);
					}
					val.BeginGrid(1, Math.Max(80f, num - 24f), 40f, 6f);
					foreach (Row item in list)
					{
						AddRow(val, item);
					}
					val.End();
					if (flag)
					{
						val.End();
					}
				}
				if (HasSavedPos())
				{
					val.SetPosition(new Vector2(Mod.NativePosX.Value, Mod.NativePosY.Value));
				}
				else
				{
					val.Center();
				}
				CustomUIWindow val2 = val.Show();
				if (!HasSavedPos())
				{
					try
					{
						if (val2 != null && val2.IsAlive)
						{
							Vector2 anchoredPosition = val2.Rect.anchoredPosition;
							Mod.NativePosX.Value = anchoredPosition.x;
							Mod.NativePosY.Value = anchoredPosition.y;
						}
					}
					catch
					{
					}
				}
				BindClose(val2);
				BindZoom(val2);
				ApplyWindowZoom(val2);
				AttachGrip(val2);
			}
			finally
			{
				_rebuilding = false;
			}
		}

		private static void AddToolbar(CustomUIBuilder builder)
		{
			builder.BeginRow(4f);
			AddFilterToggle(builder, "公", RatMonitorFilters.FilterMale, (bool v) =>
			{
				RatMonitorFilters.FilterMale = v;
			}, "sr_f_male");
			AddFilterToggle(builder, "母", RatMonitorFilters.FilterFemale, (bool v) =>
			{
				RatMonitorFilters.FilterFemale = v;
			}, "sr_f_female");
			AddFilterToggle(builder, "怀孕", RatMonitorFilters.FilterPregnant, (bool v) =>
			{
				RatMonitorFilters.FilterPregnant = v;
			}, "sr_f_preg");
			AddFilterToggle(builder, "未怀孕", RatMonitorFilters.FilterNotPregnant, (bool v) =>
			{
				RatMonitorFilters.FilterNotPregnant = v;
			}, "sr_f_npreg");
			AddFilterToggle(builder, "已绝育", RatMonitorFilters.FilterNeutered, (bool v) =>
			{
				RatMonitorFilters.FilterNeutered = v;
			}, "sr_f_neut");
			AddFilterToggle(builder, "未绝育", RatMonitorFilters.FilterIntact, (bool v) =>
			{
				RatMonitorFilters.FilterIntact = v;
			}, "sr_f_intact");
			builder.End();
			builder.BeginRow(4f);
			AddFilterToggle(builder, "今晚缺粮", RatMonitorFilters.FilterUnderfed, (bool v) =>
			{
				RatMonitorFilters.FilterUnderfed = v;
			}, "sr_f_under");
			AddFilterToggle(builder, "今晚够吃", RatMonitorFilters.FilterFedOk, (bool v) =>
			{
				RatMonitorFilters.FilterFedOk = v;
			}, "sr_f_fed");
			builder.End();
			builder.BeginRow(4f);
			builder.AddLabel("排序", "sr_sort_lbl");
			List<string> options = RatMonitorFilters.SortGeneDropdownLabels();
			AddSortDropdown(builder, options, RatMonitorFilters.SortGeneDropdownIndex());
			AddFilterToggle(builder, "高→低", RatMonitorFilters.SortDescending, (bool v) =>
			{
				RatMonitorFilters.SortDescending = v;
			}, "sr_sort_desc");
			bool flag = RatData.CanSeeAlleles() && RatMonitorFilters.IsGeneSort();
			if (!flag && !RatMonitorFilters.IsGeneSort())
			{
				RatMonitorFilters.IncludeAlleles = false;
			}
			if (!RatData.CanSeeAlleles())
			{
				RatMonitorFilters.IncludeAlleles = false;
			}
			AddFilterToggle(builder, flag ? "含等位基因" : "含等位(基因排序)", RatMonitorFilters.IncludeAlleles & flag, (bool v) =>
			{
				RatMonitorFilters.IncludeAlleles = v && RatData.CanSeeAlleles() && RatMonitorFilters.IsGeneSort();
			}, "sr_sort_allele");
			builder.End();
		}

		private static void AddFilterToggle(CustomUIBuilder builder, string label, bool initial, Action<bool> set, string id)
		{
			Action<bool> action = (bool v) =>
			{
				set(v);
				Mod.SaveFiltersToPrefs();
				RequestFullRefresh();
			};
			Action<bool> val = DelegateSupport.ConvertDelegate<Action<bool>>((Delegate)action);
			if ((Delegate)(object)val == (Delegate)null)
			{
				val = Action<bool>.op_Implicit(action);
			}
			RootedBoolActions.Add(val);
			builder.AddToggle(label, initial, val, id);
		}

		private static void AddSortDropdown(CustomUIBuilder builder, List<string> options, int index)
		{
			Action<int> action = (int i) =>
			{
				RatMonitorFilters.SetSortGeneByIndex(i);
				Mod.SaveFiltersToPrefs();
				RequestFullRefresh();
			};
			Action<int> val = DelegateSupport.ConvertDelegate<Action<int>>((Delegate)action);
			if ((Delegate)(object)val == (Delegate)null)
			{
				val = Action<int>.op_Implicit(action);
			}
			RootedIntActions.Add(val);
			List<string> val2 = new List<string>();
			foreach (string option in options)
			{
				val2.Add(option);
			}
			builder.AddDropdown(((Il2CppObjectBase)val2).Cast<IList<string>>(), index, val, "sr_sort_gene");
		}

		private static void AddRow(CustomUIBuilder builder, Row row)
		{
			builder.BeginRow(6f);
			Sprite val = TryGetSprite(row.Rat);
			if ((Object)(object)val != (Object)null)
			{
				builder.AddImage(val, 28f, 28f, "sr_icon_" + row.Rat.uniqueId);
			}
			builder.AddButton(RowLabel(row), BindClick(() =>
			{
				try
				{
					OpenItem(row.Rat);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("SciRat open: " + ex.Message);
				}
			}), "sr_btn_" + row.Rat.uniqueId);
			ApplyTooltip(builder, Tip(row));
			if (row.Pregnant)
			{
				GameItem target = row.Rat;
				builder.AddButton("孕检", BindClick(() =>
				{
					try
					{
						ShowPrenatalTooltip(target);
					}
					catch (Exception ex)
					{
						Mod.Log.Warning("SciRat prenatal: " + ex.Message);
					}
				}), "sr_prenatal_" + row.Rat.uniqueId);
				ApplyTooltip(builder, RatData.BuildPartnerGeneReport(target));
			}
			if (row.Pregnant)
			{
				GameItem target2 = row.Rat;
				builder.AddButton("流产", BindClick(() =>
				{
					try
					{
						if (RatData.Abort(target2))
						{
							RefreshRat(target2);
						}
					}
					catch (Exception ex)
					{
						Mod.Log.Warning("SciRat abort: " + ex.Message);
					}
				}), "sr_abort_" + row.Rat.uniqueId);
			}
			builder.End();
		}

		private static void ApplyTooltip(CustomUIBuilder builder, string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			try
			{
				builder.WithTooltip(text);
				ApplyTooltipToElement(builder.Last, text);
			}
			catch
			{
			}
		}

		private static void ApplyTooltipToElement(CustomUIElement? el, string text)
		{
			if (el == null || string.IsNullOrEmpty(text))
			{
				return;
			}
			try
			{
				el.SetTooltip(text);
				if ((Object)(object)el.image != (Object)null)
				{
					((Graphic)el.image).raycastTarget = true;
				}
				if ((Object)(object)el.text != (Object)null)
				{
					((Graphic)el.text).raycastTarget = true;
				}
			}
			catch
			{
			}
		}

		private static int SafeId(GameItem item)
		{
			try
			{
				return (item != null) ? item.uniqueId : 0;
			}
			catch
			{
				return 0;
			}
		}

		private static void ShowPrenatalTooltip(GameItem female)
		{
			if (female == null)
			{
				return;
			}
			string text = RatData.BuildPartnerGeneReport(female);
			CustomUIManager instance = CustomUIManager.Instance;
			CustomUIElement el = null;
			try
			{
				object obj;
				if (instance == null)
				{
					obj = null;
				}
				else
				{
					CustomUIWindow window = instance.GetWindow("sci_rat_monitor");
					obj = ((window != null) ? window.Get("sr_prenatal_" + SafeId(female)) : null);
				}
				el = (CustomUIElement)obj;
			}
			catch
			{
			}
			CustomUITooltipTrigger val = FindTooltipTrigger(el);
			if ((Object)(object)instance != (Object)null && (Object)(object)val != (Object)null)
			{
				try
				{
					instance.ShowTooltip(text, val);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("SciRat ShowTooltip: " + ex.Message);
				}
			}
			try
			{
				ShowPrenatalPopup(instance, female);
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("SciRat prenatal popup: " + ex2.Message);
			}
		}

		private static CustomUITooltipTrigger? FindTooltipTrigger(CustomUIElement? el)
		{
			if ((Object)(object)((el != null) ? el.root : null) == (Object)null)
			{
				return null;
			}
			CustomUITooltipTrigger component = el.root.GetComponent<CustomUITooltipTrigger>();
			if ((Object)(object)component != (Object)null)
			{
				return component;
			}
			return el.root.GetComponentInChildren<CustomUITooltipTrigger>(true);
		}

		private static void ShowPrenatalPopup(CustomUIManager? ui, GameItem female)
		{
			if ((Object)(object)ui == (Object)null || female == null)
			{
				return;
			}
			_prenatalFemale = female;
			PrenatalActions.Clear();
			if (ui.IsOpen("sci_rat_prenatal"))
			{
				ui.CloseWindow("sci_rat_prenatal");
			}
			List<(string, string, float, float)> list = RatData.ListPartnerGenes(female);
			int value = RatData.CountGeneProbesInStore();
			float num = Math.Min(560f, 140f + (float)Math.Max(1, list.Count) * 36f + 72f);
			CustomUIBuilder val = ui.CreateWindow("sci_rat_prenatal", "孕检 · 基因编辑", "overlay").SetDraggable(true).SetCloseOnEscape(true)
				.SetSize(560f, num);
			val.AddLabel("母鼠：" + RatData.DisplayName(female), "sr_preg_name");
			val.AddLabel($"基因编辑探针：{value}（选A/选B=把公鼠另一侧改成同值；不改母鼠自身）", "sr_preg_probe");
			val.AddLabel(GeneEditLimit.StatusLine(female), "sr_preg_limit");
			if (!string.IsNullOrEmpty(_prenatalStatus))
			{
				val.AddLabel(_prenatalStatus, "sr_preg_status");
			}
			bool flag = GeneEditLimit.CanEditMore(female, out string _);
			if (list.Count == 0)
			{
				val.AddLabel("无配对公鼠基因记录", "sr_preg_empty");
			}
			else
			{
				for (int i = 0; i < list.Count; i++)
				{
					(string, string, float, float) tuple = list[i];
					string geneId = tuple.Item1;
					bool flag2 = Math.Abs(tuple.Item3 - tuple.Item4) < 1E-06f;
					string text = RatData.FormatPartnerGeneEditLine(tuple.Item2, geneId, tuple.Item3, tuple.Item4);
					val.BeginRow(4f);
					val.AddLabel(text, "sr_preg_g_" + i);
					if (!flag2 & flag)
					{
						val.AddButton("选A", BindPrenatalClick(() =>
						{
							DoLockPartner(geneId, keepA: true);
						}), "sr_preg_lockA_" + i);
						val.AddButton("选B", BindPrenatalClick(() =>
						{
							DoLockPartner(geneId, keepA: false);
						}), "sr_preg_lockB_" + i);
					}
					else if (!flag2 && !flag)
					{
						val.AddLabel("（已达编辑上限）", "sr_preg_cap_" + i);
					}
					val.End();
				}
			}
			val.BeginRow(4f);
			val.AddButton("刷新", BindPrenatalClick(RefreshPrenatal), "sr_preg_refresh");
			val.End();
			val.Center();
			ApplyWindowZoom(val.Show());
		}

		private static Action BindPrenatalClick(Action action)
		{
			Action val = DelegateSupport.ConvertDelegate<Action>((Delegate)action);
			if ((Delegate)(object)val == (Delegate)null)
			{
				val = Action.op_Implicit(action);
			}
			PrenatalActions.Add(val);
			return val;
		}

		private static void DoLockPartner(string geneId, bool keepA)
		{
			GameItem prenatalFemale = _prenatalFemale;
			if (prenatalFemale == null || !RatData.IsPregnant(prenatalFemale))
			{
				_prenatalStatus = "目标无效或已不分娩中";
				RefreshPrenatal();
				return;
			}
			if (!RatData.TryReadPartnerAlleles(prenatalFemale, geneId, out var alleleA, out var alleleB) || !RatData.HasBothPartnerAlleles(prenatalFemale, geneId))
			{
				_prenatalStatus = "缺少配对基因记录";
				RefreshPrenatal();
				return;
			}
			if (Math.Abs(alleleA - alleleB) < 1E-06f)
			{
				_prenatalStatus = "该基因 A/B 已相同，无需选择";
				RefreshPrenatal();
				return;
			}
			if (!GeneEditLimit.CanEditMore(prenatalFemale, out string denyMessage))
			{
				_prenatalStatus = denyMessage;
				RefreshPrenatal();
				return;
			}
			if (!RatData.TryConsumeOneGeneProbe(out string message))
			{
				_prenatalStatus = message;
				RefreshPrenatal();
				return;
			}
			if (!RatData.TryLockPartnerAllele(prenatalFemale, geneId, keepA, out string message2))
			{
				_prenatalStatus = message + "；但选择失败：" + message2;
				RefreshPrenatal();
				return;
			}
			GeneEditLimit.IncrementEditCount(prenatalFemale);
			_prenatalStatus = message2 + "；" + message + "；" + GeneEditLimit.StatusLine(prenatalFemale);
			RefreshPrenatal();
		}

		private static void RefreshPrenatal()
		{
			CustomUIManager instance = CustomUIManager.Instance;
			GameItem prenatalFemale = _prenatalFemale;
			if ((Object)(object)instance == (Object)null || prenatalFemale == null)
			{
				return;
			}
			try
			{
				ShowPrenatalPopup(instance, prenatalFemale);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat prenatal refresh: " + ex.Message);
			}
		}

		private static string Tip(Row row)
		{
			string text = ((row.NextMeat >= 0) ? ("+" + row.NextMeat) : row.NextMeat.ToString());
			List<string> list = new List<string>
			{
				row.Name,
				"性别：" + row.Sex,
				"日龄：" + row.Age,
				"体重：" + row.Weight.ToString("0.##"),
				"明日增肉：" + text + "（过夜先+1岁再长肉）",
				"排序：" + row.GeneText
			};
			if (row.Pregnant)
			{
				string item = ((row.Gestation > 0) ? ("怀孕：" + row.PregDays + "/" + row.Gestation + " 天") : ("怀孕：" + row.PregDays + " 天"));
				list.Add(item);
				list.Add(RatData.HasPartnerGenes(row.Rat) ? "点击「孕检」弹出配对公鼠基因" : "无配对基因记录（点「孕检」看详情）");
			}
			if (row.Underfed)
			{
				list.Add("缺粮：过夜后仍低于饱食线（maxHunger×0.75）；散装按份数抢食");
			}
			if (row.Neutered)
			{
				list.Add("已绝育");
			}
			list.Add("点击名称选中/打开");
			return string.Join("\n", list);
		}

		private static Row BuildRow(GameItem rat)
		{
			Row row = new Row
			{
				Rat = rat,
				Name = RatData.DisplayName(rat),
				Sex = RatData.SexDisplay(rat),
				Age = RatData.AgeDays(rat),
				Weight = RatData.Weight(rat),
				NextMeat = RatData.NextMeat(rat),
				Pregnant = RatData.IsPregnant(rat),
				PregDays = RatData.PregnantDays(rat),
				Gestation = RatData.GestationDays(rat),
				Underfed = RatData.WillUnderfeedTonight(rat)
			};
			try
			{
				row.Neutered = rat.IsTag("ANIMAL_NEUTERED_TAG");
			}
			catch
			{
			}
			RatMonitorFilters.SyncAlleleGate();
			string sortGeneId = RatMonitorFilters.SortGeneId;
			float value = RatData.SortKey(rat, sortGeneId, RatMonitorFilters.IncludeAlleles, RatMonitorFilters.SortDescending);
			string text = RatData.GeneDisplayName(sortGeneId);
			float alleleA;
			float alleleB;
			if (string.Equals(sortGeneId, "ANIMAL_AGE_TAG", StringComparison.Ordinal))
			{
				row.GeneText = $"{text} {row.Age}";
			}
			else if (string.Equals(sortGeneId, "ANIMAL_PREGNANT_PROGRESS_TAG", StringComparison.Ordinal))
			{
				if (row.Pregnant)
				{
					row.GeneText = ((row.Gestation > 0) ? $"{text} {row.PregDays}/{row.Gestation}" : $"{text} {row.PregDays}");
				}
				else
				{
					row.GeneText = text + " —";
				}
			}
			else if (RatMonitorFilters.IncludeAlleles && RatData.CanSeeAlleles() && RatData.TryReadAlleles(rat, sortGeneId, out alleleA, out alleleB))
			{
				row.GeneText = $"{text} {value:0.###} [{alleleA:0.###}/{alleleB:0.###}]";
			}
			else
			{
				row.GeneText = $"{text} {value:0.###}";
			}
			return row;
		}

		private static bool NeedsRowLayoutRebuild(CustomUIManager ui, Row row)
		{
			try
			{
				CustomUIWindow window = ui.GetWindow("sci_rat_monitor");
				if (window == null || !window.IsAlive)
				{
					return true;
				}
				int uniqueId = row.Rat.uniqueId;
				bool flag = window.Get("sr_prenatal_" + uniqueId) != null;
				bool flag2 = window.Get("sr_abort_" + uniqueId) != null;
				bool pregnant = row.Pregnant;
				bool pregnant2 = row.Pregnant;
				return pregnant != flag || pregnant2 != flag2;
			}
			catch
			{
				return true;
			}
		}

		private static string RowLabel(Row row)
		{
			string value = ((row.NextMeat >= 0) ? ("+" + row.NextMeat) : row.NextMeat.ToString());
			string text;
			if (row.Pregnant)
			{
				text = ((row.Gestation > 0) ? $"孕{row.PregDays}/{row.Gestation}" : $"孕{row.PregDays}天");
			}
			else
			{
				text = (row.Neutered ? "绝育" : "");
			}
			string text2 = $"{Trunc(row.Name, 10)} {row.Sex} {row.Age}天 {row.Weight:0.#} {value}";
			if (!string.IsNullOrEmpty(text))
			{
				text2 = text2 + "  " + text;
			}
			if (row.Underfed)
			{
				text2 += "  缺粮";
			}
			if (!string.IsNullOrEmpty(row.GeneText))
			{
				text2 = text2 + "  " + Trunc(row.GeneText, 28);
			}
			return text2;
		}

		private static void PatchRatRow(CustomUIManager ui, Row row)
		{
			try
			{
				CustomUIWindow window = ui.GetWindow("sci_rat_monitor");
				if (window == null || !window.IsAlive)
				{
					return;
				}
				CustomUIElement val = window.Get("sr_btn_" + row.Rat.uniqueId);
				if (val != null)
				{
					val.SetText(RowLabel(row));
					ApplyTooltipToElement(val, Tip(row));
					if (row.Pregnant)
					{
						ApplyTooltipToElement(window.Get("sr_prenatal_" + row.Rat.uniqueId), RatData.BuildPartnerGeneReport(row.Rat));
					}
				}
			}
			catch
			{
			}
		}

		private static List<Row> CollectRows()
		{
			List<Row> list = new List<Row>();
			if (!GameSession.TryGetStore(out PlayerStore store))
			{
				return list;
			}
			_refreshDepth++;
			try
			{
				List<GameItem> val;
				try
				{
					val = store.FindAllItem(true);
				}
				catch
				{
					return list;
				}
				if (val == null)
				{
					return list;
				}
				List<GameItem> list2 = new List<GameItem>();
				Enumerator<GameItem> enumerator = val.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					if (RatData.IsRat(current))
					{
						list2.Add(current);
					}
				}
				_cachedRats.Clear();
				_cachedRats.AddRange(list2);
				RatData.InvalidateFeedProjection();
				RatData.RebuildFeedProjection(list2);
				foreach (GameItem item in list2)
				{
					Row row = BuildRow(item);
					if (RatMonitorFilters.Passes(item, row.Pregnant, row.Neutered))
					{
						list.Add(row);
					}
				}
				list.Sort((Row a, Row b) =>
				{
					int num = RatMonitorFilters.Compare(a.Rat, b.Rat);
					if (num != 0)
					{
						return num;
					}
					num = b.Pregnant.CompareTo(a.Pregnant);
					if (num != 0)
					{
						return num;
					}
					num = b.NextMeat.CompareTo(a.NextMeat);
					return (num != 0) ? num : string.CompareOrdinal(a.Name, b.Name);
				});
				return list;
			}
			finally
			{
				_refreshDepth--;
			}
		}

		private static void OpenItem(GameItem item)
		{
			if (item == null)
			{
				return;
			}
			try
			{
				GameItem val = null;
				try
				{
					GameInventory parentInventory = item.parentInventory;
					if (parentInventory != null)
					{
						val = parentInventory.GetParentItem();
					}
				}
				catch
				{
				}
				ClearHighlight();
				if (val != null)
				{
					BringOrOpen(val.contentWindow, centerIfNew: true);
				}
				HighlightItem(item);
				BringExamine(item);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat OpenItem: " + ex.Message);
			}
		}

		private static void BringOrOpen(PixelWindow? win, bool centerIfNew)
		{
			if (win == null)
			{
				return;
			}
			bool flag = false;
			try
			{
				flag = win.IsVisible();
			}
			catch
			{
			}
			if (flag)
			{
				try
				{
					win.ToFront(true);
					return;
				}
				catch
				{
					return;
				}
			}
			try
			{
				win.Show();
			}
			catch
			{
			}
			if (centerIfNew)
			{
				try
				{
					win.Center();
				}
				catch
				{
				}
			}
			try
			{
				win.ToFront(true);
			}
			catch
			{
			}
			try
			{
				win.FixBounds();
			}
			catch
			{
			}
		}

		private static void BringExamine(GameItem item)
		{
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			PixelWindow val = null;
			try
			{
				val = item.examineWindow;
			}
			catch
			{
			}
			bool flag = false;
			try
			{
				flag = val != null && val.IsVisible();
			}
			catch
			{
			}
			if (flag)
			{
				try
				{
					val.ToFront(true);
					return;
				}
				catch
				{
					return;
				}
			}
			if (val != null)
			{
				BringOrOpen(val, centerIfNew: true);
				return;
			}
			try
			{
				ItemMouseDoubleClickHandler current = ItemMouseDoubleClickHandler.current;
				if ((Object)(object)current != (Object)null)
				{
					current.OpenExamineAction(item, ScreenCenter());
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat OpenExamineAction: " + ex.Message);
			}
			try
			{
				val = item.examineWindow;
			}
			catch
			{
			}
			if (val == null)
			{
				return;
			}
			try
			{
				val.Center();
			}
			catch
			{
			}
			try
			{
				val.ToFront(true);
			}
			catch
			{
			}
			try
			{
				val.FixBounds();
			}
			catch
			{
			}
		}

		private static Vector2 ScreenCenter()
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2((float)Screen.width * 0.5f, (float)Screen.height * 0.5f);
		}

		private static void HighlightItem(GameItem item)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Expected Obj, but got Unknown
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Expected Obj, but got Unknown
			try
			{
				if (_highlight == null)
				{
					_highlight = new HighlightManager();
				}
				GameInventory parentInventory = item.parentInventory;
				GameGridInventory val = ((parentInventory != null) ? ((Il2CppObjectBase)parentInventory).TryCast<GameGridInventory>() : null);
				if (val == null)
				{
					return;
				}
				SlotMarker val2 = null;
				try
				{
					val2 = ((GameInventory)val).GetIncompleteItemInventorySlot(item);
				}
				catch
				{
				}
				if (val2 == null || val2.item == null)
				{
					GridShape val3 = item.modifiedShape ?? item.shape;
					if (val3 == null)
					{
						return;
					}
					val2 = new SlotMarker
					{
						item = item,
						inventory = (GameInventory)(object)val,
						itemGridShape = val3,
						index = 0,
						numTransfer = ((item.unitCount <= 0) ? 1 : item.unitCount)
					};
				}
				((GameInventory)val).HighlightSlot(val2, (BackgroundColor)1, true);
				_highlight.Try(val2, (BackgroundColor)1);
				_highlightInvs.Add((GameInventory)(object)val);
				try
				{
					_highlightUid = item.uniqueId;
				}
				catch
				{
					_highlightUid = 0;
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat highlight: " + ex.Message);
			}
		}

		private static Sprite? TryGetSprite(GameItem item)
		{
			try
			{
				string animalSprite = HusbandryHelper.GetAnimalSprite(item);
				if (!string.IsNullOrEmpty(animalSprite))
				{
					SpriteDict instance = SpriteDict.Instance;
					if ((Object)(object)instance != (Object)null)
					{
						Sprite sprite = instance.GetSprite(animalSprite);
						if ((Object)(object)sprite != (Object)null)
						{
							return sprite;
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				GameItemElement val = ((Il2CppObjectBase)item).TryCast<GameItemElement>();
				if (val != null)
				{
					TreeNodeRender inner = val.inner;
					object obj2 = ((inner != null) ? inner.image : null);
					if (obj2 == null)
					{
						TreeNodeRender middle = val.middle;
						obj2 = ((middle != null) ? middle.image : null);
						if (obj2 == null)
						{
							TreeNodeRender background = val.background;
							obj2 = ((background != null) ? background.image : null);
						}
					}
					Image val2 = (Image)obj2;
					if ((Object)(object)val2 != (Object)null && (Object)(object)val2.sprite != (Object)null)
					{
						return val2.sprite;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private static Action BindClick(Action act)
		{
			Action val = DelegateSupport.ConvertDelegate<Action>((Delegate)act);
			if ((Delegate)(object)val == (Delegate)null)
			{
				val = Action.op_Implicit(act);
			}
			RootedActions.Add(val);
			return val;
		}

		private static string Trunc(string s, int max)
		{
			if (string.IsNullOrEmpty(s) || s.Length <= max)
			{
				return s;
			}
			return s.Substring(0, Math.Max(1, max - 1)) + "…";
		}

		private static void SaveWindowPos(CustomUIManager ui)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				CustomUIWindow window = ui.GetWindow("sci_rat_monitor");
				if (window != null && window.IsAlive)
				{
					Vector2 anchoredPosition = window.Rect.anchoredPosition;
					if (Math.Abs(anchoredPosition.x - Mod.NativePosX.Value) > 0.5f || Math.Abs(anchoredPosition.y - Mod.NativePosY.Value) > 0.5f)
					{
						Mod.NativePosX.Value = anchoredPosition.x;
						Mod.NativePosY.Value = anchoredPosition.y;
					}
				}
			}
			catch
			{
			}
		}

		private static bool HasSavedPos()
		{
			if (Mod.NativePosX.Value > -99000f)
			{
				return Mod.NativePosY.Value > -99000f;
			}
			return false;
		}

		private static float CurrentWidth()
		{
			return Mathf.Max(480f, (Mod.NativeWidth.Value > 40f) ? Mod.NativeWidth.Value : 560f);
		}

		private static float CurrentHeight(int rows)
		{
			if (Mod.NativeHeight.Value > 40f)
			{
				return Mathf.Max(200f, Mod.NativeHeight.Value);
			}
			int num = Math.Max(3, Math.Min(8, (rows <= 0) ? 3 : rows));
			return 168f + (float)num * 40f;
		}

		private static void HideFromUi()
		{
			if (_hiding || _rebuilding)
			{
				return;
			}
			_hiding = true;
			try
			{
				_visible = false;
				_resizing = false;
				_gripRt = null;
				_cachedRats.Clear();
				RatMonitorRefresh.ClearTrackedHousing();
				ClearHighlight();
				try
				{
					CustomUIManager instance = CustomUIManager.Instance;
					if ((Object)(object)instance != (Object)null && instance.IsOpen("sci_rat_monitor"))
					{
						instance.CloseWindow("sci_rat_monitor");
					}
				}
				catch
				{
				}
			}
			finally
			{
				_hiding = false;
			}
		}

		internal static void OnHighlightedItemInventoryChanged(GameItem? item)
		{
			if (_highlightUid == 0 || item == null)
			{
				return;
			}
			try
			{
				if (item.uniqueId == _highlightUid)
				{
					ClearHighlight();
				}
			}
			catch
			{
			}
		}

		private static void ClearHighlight()
		{
			_highlightUid = 0;
			try
			{
				HighlightManager? highlight = _highlight;
				if (highlight != null)
				{
					highlight.Clear();
				}
			}
			catch
			{
			}
			foreach (GameInventory highlightInv in _highlightInvs)
			{
				try
				{
					highlightInv.ClearHighlight();
				}
				catch
				{
				}
			}
			_highlightInvs.Clear();
		}

		private static void BindClose(CustomUIWindow? win)
		{
			if (win == null || !win.IsAlive)
			{
				return;
			}
			Action val = DelegateSupport.ConvertDelegate<Action>((Delegate)new Action(HideFromUi));
			if ((Delegate)(object)val == (Delegate)null)
			{
				val = Action.op_Implicit((Action)HideFromUi);
			}
			RootedActions.Add(val);
			try
			{
				win.onClose = val;
			}
			catch
			{
			}
			try
			{
				CustomUIWindowFrame val2 = win.frame ?? ((Component)win.Rect).GetComponent<CustomUIWindowFrame>() ?? ((Component)win.Rect).GetComponentInChildren<CustomUIWindowFrame>(true);
				Button val3 = (((Object)(object)val2 != (Object)null) ? val2.closeButton : null);
				if (!((Object)(object)val3 == (Object)null))
				{
					if ((Delegate)(object)_closeUnity == (Delegate)null)
					{
						_closeUnity = DelegateSupport.ConvertDelegate<UnityAction>((Delegate)new Action(HideFromUi));
					}
					if ((Delegate)(object)_closeUnity != (Delegate)null)
					{
						((UnityEvent)val3.onClick).AddListener(_closeUnity);
					}
				}
			}
			catch
			{
			}
		}

		private static int ReadZoomLevel()
		{
			try
			{
				int value = Mod.UiZoomLevel.Value;
				if (value < 0)
				{
					return 0;
				}
				if (value > 2)
				{
					return 2;
				}
				return value;
			}
			catch
			{
				return 0;
			}
		}

		private static float CurrentZoomScale()
		{
			return ZoomScales[ReadZoomLevel()];
		}

		private static void CycleZoom()
		{
			try
			{
				int value = (ReadZoomLevel() + 1) % ZoomScales.Length;
				Mod.UiZoomLevel.Value = value;
				MelonPreferences.Save();
				CustomUIManager instance = CustomUIManager.Instance;
				if ((Object)(object)instance == (Object)null)
				{
					return;
				}
				CustomUIWindow win = null;
				try
				{
					win = instance.GetWindow("sci_rat_monitor");
				}
				catch
				{
				}
				ApplyWindowZoom(win);
				RefreshZoomButtonVisual(win);
				try
				{
					ApplyWindowZoom(instance.GetWindow("sci_rat_prenatal"));
				}
				catch
				{
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat zoom: " + ex.Message);
			}
		}

		private static void ApplyWindowZoom(CustomUIWindow? win)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			if (win == null || !win.IsAlive)
			{
				return;
			}
			try
			{
				float num = CurrentZoomScale();
				((Transform)win.Rect).localScale = new Vector3(num, num, 1f);
			}
			catch
			{
			}
		}

		private static void BindZoom(CustomUIWindow? win)
		{
			//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e7: Expected Obj, but got Unknown
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0128: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			//IL_0144: Unknown result type (might be due to invalid IL or missing references)
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
			if (win == null || !win.IsAlive)
			{
				return;
			}
			try
			{
				CustomUIWindowFrame val = win.frame ?? ((Component)win.Rect).GetComponent<CustomUIWindowFrame>() ?? ((Component)win.Rect).GetComponentInChildren<CustomUIWindowFrame>(true);
				Button val2 = (((Object)(object)val != (Object)null) ? val.closeButton : null);
				if ((Object)(object)val2 == (Object)null)
				{
					return;
				}
				GameObject gameObject = ((Component)val2).gameObject;
				Transform parent = gameObject.transform.parent;
				if ((Object)(object)parent == (Object)null)
				{
					return;
				}
				for (int num = parent.childCount - 1; num >= 0; num--)
				{
					Transform child = parent.GetChild(num);
					if ((Object)(object)child != (Object)null && ((Object)child).name == "sr_zoom_btn")
					{
						Object.DestroyImmediate((Object)(object)((Component)child).gameObject);
					}
				}
				GameObject val3 = Object.Instantiate<GameObject>(gameObject, parent);
				((Object)val3).name = "sr_zoom_btn";
				val3.transform.SetSiblingIndex(gameObject.transform.GetSiblingIndex());
				RectTransform component = ((Component)val2).GetComponent<RectTransform>();
				RectTransform component2 = val3.GetComponent<RectTransform>();
				if ((Object)(object)component != (Object)null && (Object)(object)component2 != (Object)null)
				{
					component2.anchorMin = component.anchorMin;
					component2.anchorMax = component.anchorMax;
					component2.pivot = component.pivot;
					component2.sizeDelta = component.sizeDelta;
					((Transform)component2).localScale = Vector3.one;
					Rect rect = component.rect;
					float num2 = Mathf.Abs(rect.width);
					if (num2 < 2f)
					{
						num2 = Mathf.Abs(component.sizeDelta.x);
					}
					if (num2 < 2f)
					{
						num2 = 32f;
					}
					component2.anchoredPosition = component.anchoredPosition + new Vector2(0f - (num2 + 4f), 0f);
				}
				Button component3 = val3.GetComponent<Button>();
				if ((Object)(object)component3 == (Object)null)
				{
					return;
				}
				component3.onClick = new ButtonClickedEvent();
				if ((Delegate)(object)_zoomUnity == (Delegate)null)
				{
					_zoomUnity = DelegateSupport.ConvertDelegate<UnityAction>((Delegate)new Action(CycleZoom));
				}
				if ((Delegate)(object)_zoomUnity != (Delegate)null)
				{
					((UnityEvent)component3.onClick).AddListener(_zoomUnity);
				}
				try
				{
					((Selectable)component3).transition = (Transition)0;
				}
				catch
				{
				}
				ApplyZoomSprites(component3, ReadZoomLevel());
				try
				{
					(val3.GetComponent<CustomUITooltipTrigger>() ?? val3.AddComponent<CustomUITooltipTrigger>()).tooltipText = ZoomTooltip(ReadZoomLevel());
				}
				catch
				{
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat BindZoom: " + ex.Message);
			}
		}

		private static string ZoomTooltip(int level)
		{
			float value = ZoomScales[Math.Clamp(level, 0, ZoomScales.Length - 1)];
			string value2 = ZoomScales[(level + 1) % ZoomScales.Length].ToString("0.##");
			return $"界面缩放 ×{value:0.##}（点击切换到 ×{value2}）";
		}

		private static void RefreshZoomButtonVisual(CustomUIWindow? win)
		{
			if (win == null || !win.IsAlive)
			{
				return;
			}
			try
			{
				Transform val = ((Transform)win.Rect).Find("sr_zoom_btn") ?? FindDeep((Transform)(object)win.Rect, "sr_zoom_btn");
				if ((Object)(object)val == (Object)null)
				{
					return;
				}
				Button component = ((Component)val).GetComponent<Button>();
				if ((Object)(object)component != (Object)null)
				{
					ApplyZoomSprites(component, ReadZoomLevel());
				}
				try
				{
					CustomUITooltipTrigger val2 = ((Component)val).GetComponent<CustomUITooltipTrigger>() ?? ((Component)val).GetComponentInChildren<CustomUITooltipTrigger>(true);
					if ((Object)(object)val2 != (Object)null)
					{
						val2.tooltipText = ZoomTooltip(ReadZoomLevel());
					}
				}
				catch
				{
				}
			}
			catch
			{
			}
		}

		private static Transform? FindDeep(Transform root, string name)
		{
			if ((Object)(object)root == (Object)null)
			{
				return null;
			}
			for (int i = 0; i < root.childCount; i++)
			{
				Transform child = root.GetChild(i);
				if (!((Object)(object)child == (Object)null))
				{
					if (((Object)child).name == name)
					{
						return child;
					}
					Transform val = FindDeep(child, name);
					if ((Object)(object)val != (Object)null)
					{
						return val;
					}
				}
			}
			return null;
		}

		private static void ApplyZoomSprites(Button btn, int level)
		{
			EnsureZoomSprites();
			level = Math.Clamp(level, 0, 2);
			Sprite val = ZoomSprites[level];
			if ((Object)(object)val == (Object)null)
			{
				_zoomSpritesTried = false;
				EnsureZoomSprites();
				val = ZoomSprites[level];
				if ((Object)(object)val == (Object)null)
				{
					return;
				}
			}
			try
			{
				Graphic targetGraphic = ((Selectable)btn).targetGraphic;
				Image val2 = (Image)(object)((targetGraphic is Image) ? targetGraphic : null);
				if (val2 != null)
				{
					val2.sprite = val;
					val2.overrideSprite = null;
					return;
				}
				Image val3 = ((Component)btn).GetComponent<Image>() ?? ((Component)btn).GetComponentInChildren<Image>(true);
				if ((Object)(object)val3 != (Object)null)
				{
					val3.sprite = val;
					val3.overrideSprite = null;
				}
			}
			catch
			{
			}
		}

		private static void EnsureZoomSprites()
		{
			if (_zoomSpritesTried)
			{
				return;
			}
			_zoomSpritesTried = true;
			try
			{
				Il2CppArrayBase<Sprite> val = Resources.FindObjectsOfTypeAll<Sprite>();
				if (val == null)
				{
					return;
				}
				for (int i = 0; i < val.Length; i++)
				{
					Sprite val2 = val[i];
					if ((Object)(object)val2 == (Object)null || string.IsNullOrEmpty(((Object)val2).name))
					{
						continue;
					}
					for (int j = 0; j < ZoomSpriteNames.Length; j++)
					{
						if ((Object)(object)ZoomSprites[j] == (Object)null && ((Object)val2).name == ZoomSpriteNames[j])
						{
							ZoomSprites[j] = val2;
						}
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("SciRat zoom sprites: " + ex.Message);
			}
			if ((Object)(object)ZoomSprites[0] == (Object)null && (Object)(object)ZoomSprites[1] == (Object)null && (Object)(object)ZoomSprites[2] == (Object)null)
			{
				_zoomSpritesTried = false;
			}
		}

		private static void HandleResize()
		{
			//IL_01bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_022c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0216: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			if (!_inputOk)
			{
				return;
			}
			CustomUIManager instance = CustomUIManager.Instance;
			if ((Object)(object)instance == (Object)null || !_visible)
			{
				_resizing = false;
				return;
			}
			CustomUIWindow val = null;
			try
			{
				val = instance.GetWindow("sci_rat_monitor");
			}
			catch
			{
			}
			bool mouseButton = Input.GetMouseButton(0);
			bool mouseButtonDown = Input.GetMouseButtonDown(0);
			if (val == null || !val.IsAlive)
			{
				if (!mouseButton)
				{
					_resizing = false;
				}
				return;
			}
			EnsureGrip(instance);
			if (mouseButtonDown && !_resizing && GripContainsMouse())
			{
				_resizing = true;
				_resizeMouse0 = Vector2.op_Implicit(Input.mousePosition);
				_resizeSize0 = val.Rect.sizeDelta;
				_resizePos0 = val.Rect.anchoredPosition;
				_resizePivot = val.Rect.pivot;
			}
			if (!_resizing)
			{
				return;
			}
			if (!mouseButton)
			{
				_resizing = false;
				SaveWindowPos(instance);
				return;
			}
			float num = 1f;
			try
			{
				Canvas componentInParent = ((Component)val.Rect).GetComponentInParent<Canvas>();
				if ((Object)(object)componentInParent != (Object)null && componentInParent.scaleFactor > 0.01f)
				{
					num = componentInParent.scaleFactor;
				}
			}
			catch
			{
			}
			float num2 = CurrentZoomScale();
			if (num2 > 0.01f)
			{
				num *= num2;
			}
			Vector3 mousePosition = Input.mousePosition;
			float num3 = (mousePosition.x - _resizeMouse0.x) / num;
			float num4 = (_resizeMouse0.y - mousePosition.y) / num;
			float num5 = Mathf.Clamp(_resizeSize0.x + num3, 480f, (float)Screen.width / num * 0.95f);
			float num6 = Mathf.Clamp(_resizeSize0.y + num4, 200f, (float)Screen.height / num * 0.95f);
			try
			{
				val.SetSize(num5, num6);
			}
			catch
			{
				val.Rect.sizeDelta = new Vector2(num5, num6);
			}
			Vector2 val2 = new Vector2(_resizePos0.x + (num5 - _resizeSize0.x) * _resizePivot.x, _resizePos0.y - (num6 - _resizeSize0.y) * (1f - _resizePivot.y));
			try
			{
				val.SetPosition(val2);
			}
			catch
			{
				val.Rect.anchoredPosition = val2;
			}
			Mod.NativeWidth.Value = num5;
			Mod.NativeHeight.Value = num6;
			SaveWindowPos(instance);
		}

		private static bool GripContainsMouse()
		{
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if ((Object)(object)_gripRt == (Object)null || (Object)(object)((Component)_gripRt).gameObject == (Object)null)
				{
					return false;
				}
				Camera val = null;
				Canvas componentInParent = ((Component)_gripRt).GetComponentInParent<Canvas>();
				if ((Object)(object)componentInParent != (Object)null && (int)componentInParent.renderMode != 0)
				{
					val = componentInParent.worldCamera;
				}
				return RectTransformUtility.RectangleContainsScreenPoint(_gripRt, Vector2.op_Implicit(Input.mousePosition), val);
			}
			catch
			{
				_gripRt = null;
				return false;
			}
		}

		private static void EnsureGrip(CustomUIManager ui)
		{
			try
			{
				AttachGrip(ui.GetWindow("sci_rat_monitor"));
			}
			catch
			{
			}
		}

		private static void AttachGrip(CustomUIWindow? win)
		{
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Expected Obj, but got Unknown
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			if (win == null || !win.IsAlive)
			{
				return;
			}
			RectTransform val = null;
			try
			{
				CustomUIWindowFrame val2 = ((Component)win.Rect).GetComponent<CustomUIWindowFrame>() ?? ((Component)win.Rect).GetComponentInParent<CustomUIWindowFrame>() ?? ((Component)win.Rect).GetComponentInChildren<CustomUIWindowFrame>(true);
				if ((Object)(object)val2 != (Object)null)
				{
					val = ((Component)val2).GetComponent<RectTransform>();
				}
				if ((Object)(object)val == (Object)null)
				{
					val = win.Rect;
				}
			}
			catch
			{
				return;
			}
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			try
			{
				Transform val3 = ((Transform)val).Find("sr_resize_grip");
				if ((Object)(object)val3 != (Object)null)
				{
					_gripRt = ((Component)val3).GetComponent<RectTransform>();
					ApplyGrip(_gripRt);
					return;
				}
			}
			catch
			{
			}
			GameObject val4 = new GameObject("sr_resize_grip");
			val4.layer = ((Component)val).gameObject.layer;
			val4.transform.SetParent((Transform)(object)val, false);
			RectTransform val5 = val4.GetComponent<RectTransform>();
			if ((Object)(object)val5 == (Object)null)
			{
				val5 = val4.AddComponent<RectTransform>();
			}
			Image val6 = val4.GetComponent<Image>();
			if ((Object)(object)val6 == (Object)null)
			{
				val6 = val4.AddComponent<Image>();
			}
			val6.sprite = GripSprite();
			((Graphic)val6).color = Color.white;
			((Graphic)val6).raycastTarget = true;
			val6.preserveAspect = true;
			ApplyGrip(val5);
			_gripRt = val5;
		}

		private static void ApplyGrip(RectTransform? grip)
		{
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)grip == (Object)null))
			{
				LayoutElement val = ((Component)grip).GetComponent<LayoutElement>();
				if ((Object)(object)val == (Object)null)
				{
					val = ((Component)grip).gameObject.AddComponent<LayoutElement>();
				}
				val.ignoreLayout = true;
				LayoutElement val2 = val;
				LayoutElement val3 = val;
				LayoutElement val4 = val;
				float num = (val.preferredHeight = 16f);
				float num3 = (val4.preferredWidth = num);
				float minWidth = (val3.minHeight = num3);
				val2.minWidth = minWidth;
				LayoutElement val5 = val;
				minWidth = (val.flexibleHeight = 0f);
				val5.flexibleWidth = minWidth;
				((Transform)grip).localScale = Vector3.one;
				grip.anchorMin = new Vector2(1f, 0f);
				grip.anchorMax = new Vector2(1f, 0f);
				grip.pivot = new Vector2(1f, 0f);
				grip.anchoredPosition = new Vector2(-2f, 2f);
				grip.sizeDelta = new Vector2(16f, 16f);
				((Transform)grip).SetAsLastSibling();
			}
		}

		private static Sprite GripSprite()
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected Obj, but got Unknown
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_gripSprite != (Object)null)
			{
				return _gripSprite;
			}
			Texture2D val = new Texture2D(32, 32, (TextureFormat)4, false);
			((Texture)val).filterMode = (FilterMode)0;
			((Texture)val).wrapMode = (TextureWrapMode)1;
			((Object)val).hideFlags = (HideFlags)61;
			Color[] array = new Color[1024];
			Color val2 = new Color(0.82f, 0.62f, 0.4f, 0.95f);
			for (int i = 0; i < 32; i++)
			{
				for (int j = 0; j < 32; j++)
				{
					float num = (float)j + 0.5f - ((float)i + 0.5f);
					array[i * 32 + j] = (Color)((num > -1f) ? new Color(val2.r, val2.g, val2.b, val2.a * Mathf.Clamp01(num + 0.5f)) : Color.clear);
				}
			}
			val.SetPixels(Il2CppStructArray<Color>.op_Implicit(array));
			val.Apply();
			_gripSprite = Sprite.Create(val, new Rect(0f, 0f, 32f, 32f), new Vector2(1f, 0f), 32f);
			((Object)_gripSprite).hideFlags = (HideFlags)61;
			return _gripSprite;
		}
	}
}
