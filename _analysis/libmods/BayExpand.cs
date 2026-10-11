using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using BayExpand;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Mod), "BayExpand", "1.6.1", "local", null)]
[assembly: MelonGame(null, "Probably Stolen")]
[assembly: AssemblyCompany("BayExpand")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.6.0.0")]
[assembly: AssemblyInformationalVersion("1.6.0")]
[assembly: AssemblyProduct("BayExpand")]
[assembly: AssemblyTitle("BayExpand")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.6.0.0")]
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
namespace BayExpand
{
	internal static class BayExpandConfig
	{
		internal static MelonPreferences_Entry<bool> EnableDossier;

		internal static MelonPreferences_Entry<bool> EnableTrashcan;

		internal static MelonPreferences_Entry<int> DossierTargetSize;

		internal static MelonPreferences_Entry<int> TrashcanTargetSize;

		internal static MelonPreferences_Entry<bool> SyncWithContainerUpgradeTags;

		internal static void Init()
		{
			MelonPreferences_Category val = MelonPreferences.CreateCategory("BayExpand");
			EnableDossier = val.CreateEntry<bool>("EnableDossier", true, (string)null, "【文件柜扩容】是否启用文件柜（Dossier）网格扩展。true=启用；false=保持原版 10x10。", false, false, (ValueValidator)null, (string)null);
			EnableTrashcan = val.CreateEntry<bool>("EnableTrashcan", true, (string)null, "【垃圾桶扩容】是否启用垃圾桶网格扩展。如果你同时装了 ContainerUpgrade 且已把 Trashcan 加入 BoxIds，建议把这里设为 false 避免冲突。", false, false, (ValueValidator)null, (string)null);
			DossierTargetSize = val.CreateEntry<int>("DossierTargetSize", 16, (string)null, "【文件柜目标尺寸】文件柜网格的宽高目标值（原版=10，建议 10-30）。超出范围会被钳制到 10-30。", false, false, (ValueValidator)null, (string)null);
			TrashcanTargetSize = val.CreateEntry<int>("TrashcanTargetSize", 16, (string)null, "【垃圾桶目标尺寸】垃圾桶网格的宽高目标值（原版=10，建议 10-30）。超出范围会被钳制到 10-30。", false, false, (ValueValidator)null, (string)null);
			SyncWithContainerUpgradeTags = val.CreateEntry<bool>("SyncWithContainerUpgradeTags", true, (string)null, "【标签同步】扩容后写入 ContainerUpgrade 能识别的标签，避免两个 mod 反复拉扯。若你没装 ContainerUpgrade，可保持 true（无害）。", false, false, (ValueValidator)null, (string)null);
			val.SaveToFile(true);
		}

		internal static int GetClampedDossierSize()
		{
			int num = ((DossierTargetSize != null) ? DossierTargetSize.Value : 16);
			if (num < 10)
			{
				num = 10;
			}
			if (num > 30)
			{
				num = 30;
			}
			return num;
		}

		internal static int GetClampedTrashcanSize()
		{
			int num = ((TrashcanTargetSize != null) ? TrashcanTargetSize.Value : 16);
			if (num < 10)
			{
				num = 10;
			}
			if (num > 30)
			{
				num = 30;
			}
			return num;
		}
	}
	internal static class DocInvExpandPatch
	{
		private const string TagW = "CONTAINER_UPGRADE_W_TAG";

		private const string TagH = "CONTAINER_UPGRADE_H_TAG";

		private const string TagBaseW = "CONTAINER_UPGRADE_BASE_W_TAG";

		private const string TagBaseH = "CONTAINER_UPGRADE_BASE_H_TAG";

		private static readonly HashSet<IntPtr> Expanded = new HashSet<IntPtr>();

		internal static void Apply(Harmony harmony)
		{
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Expected Obj, but got Unknown
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Expected Obj, but got Unknown
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_014a: Expected Obj, but got Unknown
			//IL_01a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Expected Obj, but got Unknown
			if (!BayExpandConfig.EnableDossier.Value)
			{
				Mod.Log.Msg("Dossier 扩容已禁用（EnableDossier = false），跳过相关 Patch。");
				return;
			}
			MethodInfo methodInfo = AccessTools.Method(typeof(MiscItemDirectory), "Dossier", (Type[])null, (Type[])null);
			if (methodInfo != null)
			{
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(DocInvExpandPatch), "PostfixCreateDossier", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("Patched " + methodInfo?.ToString() + " (dossier create)");
			}
			else
			{
				Mod.Log.Warning("DocInvExpand: MiscItemDirectory.Dossier not found");
			}
			MethodInfo methodInfo2 = AccessTools.Method(typeof(AudioHelper), "InitDossier", (Type[])null, (Type[])null);
			if (methodInfo2 != null)
			{
				harmony.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(typeof(DocInvExpandPatch), "PostfixInitDossier", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("Patched " + methodInfo2?.ToString() + " (dossier init)");
			}
			else
			{
				Mod.Log.Warning("DocInvExpand: AudioHelper.InitDossier not found");
			}
			MethodInfo methodInfo3 = AccessTools.PropertySetter(typeof(EmporiumEntry), "dossier");
			if (methodInfo3 != null)
			{
				harmony.Patch((MethodBase)methodInfo3, (HarmonyMethod)null, new HarmonyMethod(typeof(DocInvExpandPatch), "PostfixSetDossier", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("Patched " + methodInfo3?.ToString() + " (dossier assign)");
			}
			MethodInfo methodInfo4 = AccessTools.Method(typeof(EmporiumEntry), "Start", (Type[])null, (Type[])null);
			if (methodInfo4 != null)
			{
				harmony.Patch((MethodBase)methodInfo4, (HarmonyMethod)null, new HarmonyMethod(typeof(DocInvExpandPatch), "PostfixEmporiumStart", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Mod.Log.Msg("Patched " + methodInfo4?.ToString() + " (emporium start)");
			}
		}

		public static void PostfixCreateDossier(ref GameItem __result)
		{
			TryExpand(__result, "create");
		}

		public static void PostfixInitDossier(GameItem item)
		{
			TryExpand(item, "init");
		}

		public static void PostfixSetDossier(GameItem value)
		{
			TryExpand(value, "assign");
		}

		public static void PostfixEmporiumStart(EmporiumEntry __instance)
		{
			try
			{
				TryExpand(((Object)(object)__instance != (Object)null) ? __instance.dossier : null, "start");
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("DocInvExpand start: " + ex.Message);
			}
		}

		internal static void TryExpand(GameItem dossier, string reason)
		{
			if (dossier == null)
			{
				return;
			}
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = ((Il2CppObjectBase)dossier).Pointer;
			}
			catch
			{
			}
			if (intPtr != IntPtr.Zero && !Expanded.Add(intPtr))
			{
				return;
			}
			try
			{
				List<GameGridInventory> list = CollectGrids(dossier);
				if (list.Count == 0)
				{
					if (intPtr != IntPtr.Zero)
					{
						Expanded.Remove(intPtr);
					}
					return;
				}
				bool flag = reason.Contains("trashcan");
				if (flag && !BayExpandConfig.EnableTrashcan.Value)
				{
					if (intPtr != IntPtr.Zero)
					{
						Expanded.Remove(intPtr);
					}
					return;
				}
				int num = (flag ? BayExpandConfig.GetClampedTrashcanSize() : BayExpandConfig.GetClampedDossierSize());
				int num2 = 0;
				foreach (GameGridInventory item in list)
				{
					if (item == null)
					{
						continue;
					}
					int num3 = 0;
					int num4 = 0;
					try
					{
						GridShape inventoryShape = item.inventoryShape;
						if (inventoryShape != null)
						{
							num3 = inventoryShape.width;
							num4 = inventoryShape.height;
						}
					}
					catch
					{
					}
					if (num3 < num || num4 < num)
					{
						try
						{
							item.SetShape(num, num);
							num2++;
						}
						catch (Exception ex)
						{
							Mod.Log.Warning("DocInvExpand SetShape failed (" + reason + "): " + ex.Message);
						}
					}
				}
				if (num2 > 0)
				{
					SyncTags(dossier, num);
					Mod.Log.Msg("DocInvExpand: -> " + num + "x" + num + " (" + reason + ", grids=" + num2 + "/" + list.Count + ")");
				}
			}
			catch (Exception ex2)
			{
				if (intPtr != IntPtr.Zero)
				{
					Expanded.Remove(intPtr);
				}
				Mod.Log.Warning("DocInvExpand (" + reason + "): " + ex2.Message);
			}
		}

		private static void SyncTags(GameItem item, int size)
		{
			if (item != null && BayExpandConfig.SyncWithContainerUpgradeTags.Value)
			{
				SetItemTag(item, "CONTAINER_UPGRADE_W_TAG", size);
				SetItemTag(item, "CONTAINER_UPGRADE_H_TAG", size);
				SetItemTag(item, "CONTAINER_UPGRADE_BASE_W_TAG", size);
				SetItemTag(item, "CONTAINER_UPGRADE_BASE_H_TAG", size);
			}
		}

		private static void SetItemTag(GameItem item, string tag, int value)
		{
			try
			{
				if (item == null)
				{
					return;
				}
				if (!item.IsTag(tag))
				{
					item.EnableTag(tag, true);
				}
				if (item.state != null)
				{
					TagState tag2 = item.state.GetTag(tag);
					if (tag2 != null)
					{
						tag2.SetInt(value);
					}
				}
			}
			catch
			{
			}
		}

		private static List<GameGridInventory> CollectGrids(GameItem container)
		{
			List<GameGridInventory> list = new List<GameGridInventory>();
			HashSet<IntPtr> seen = new HashSet<IntPtr>();
			try
			{
				Walk(container.children);
			}
			catch
			{
			}
			if (list.Count == 0)
			{
				try
				{
					PixelWindow contentWindow = container.contentWindow;
					if (contentWindow != null)
					{
						GameGridInventory val = FindGridInWindow(contentWindow);
						if (val != null)
						{
							list.Add(val);
						}
					}
				}
				catch
				{
				}
			}
			return list;
			void Walk(List<GraphNodeStorage> nodes)
			{
				if (nodes != null)
				{
					Enumerator<GraphNodeStorage> enumerator = nodes.GetEnumerator();
					while (enumerator.MoveNext())
					{
						GraphNodeStorage current = enumerator.Current;
						if (current != null)
						{
							GameGridInventory val2 = AsGrid(current);
							if (val2 != null)
							{
								IntPtr intPtr = IntPtr.Zero;
								try
								{
									intPtr = ((Il2CppObjectBase)val2).Pointer;
								}
								catch
								{
								}
								if (intPtr == IntPtr.Zero || seen.Add(intPtr))
								{
									list.Add(val2);
								}
							}
							try
							{
								Walk(current.children);
							}
							catch
							{
							}
						}
					}
				}
			}
		}

		private static GameGridInventory AsGrid(GraphNodeStorage node)
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

		private static GameGridInventory FindGridInWindow(PixelWindow window)
		{
			if (window == null)
			{
				return null;
			}
			List<GameGridInventory> list = new List<GameGridInventory>();
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			try
			{
				if (window.childElement != null)
				{
					GameGridInventory val = AsGrid(((Il2CppObjectBase)window.childElement).TryCast<GraphNodeStorage>());
					if (val != null)
					{
						IntPtr intPtr = IntPtr.Zero;
						try
						{
							intPtr = ((Il2CppObjectBase)val).Pointer;
						}
						catch
						{
						}
						if (intPtr == IntPtr.Zero || hashSet.Add(intPtr))
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
				List<GraphNodeStorage> children = window.children;
				if (children != null)
				{
					for (int i = 0; i < children.Count; i++)
					{
						GameGridInventory val2 = AsGrid(children[i]);
						if (val2 != null)
						{
							IntPtr intPtr2 = IntPtr.Zero;
							try
							{
								intPtr2 = ((Il2CppObjectBase)val2).Pointer;
							}
							catch
							{
							}
							if (intPtr2 == IntPtr.Zero || hashSet.Add(intPtr2))
							{
								list.Add(val2);
							}
						}
					}
				}
			}
			catch
			{
			}
			if (list.Count <= 0)
			{
				return null;
			}
			return list[0];
		}
	}
	public sealed class Mod : MelonMod
	{
		internal static Instance Log;

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			BayExpandConfig.Init();
			DocInvExpandPatch.Apply(((MelonBase)this).HarmonyInstance);
			Log.Msg("BayExpand 已加载。Dossier=" + BayExpandConfig.EnableDossier.Value + " Trashcan=" + BayExpandConfig.EnableTrashcan.Value + " DossierSize=" + BayExpandConfig.DossierTargetSize.Value + " TrashcanSize=" + BayExpandConfig.TrashcanTargetSize.Value);
		}
	}
	[HarmonyPatch(typeof(GameItem), "ValidateShapeState")]
	internal static class TrashcanExpandPatch
	{
		private static void Postfix(GameItem __instance)
		{
			if (BayExpandConfig.EnableTrashcan.Value && __instance != null && !string.IsNullOrEmpty(__instance.identifier) && __instance.identifier.Contains("trash"))
			{
				DocInvExpandPatch.TryExpand(__instance, "trashcan_start");
			}
		}
	}
}
