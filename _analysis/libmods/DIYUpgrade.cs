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
using DIYUpgrade;
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
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(DiyUpgradeMod), "自定义容器升级", "1.3.2", "核桃", null)]
[assembly: MelonGame(null, null)]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.3.2.0")]
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
internal static class CatchLogOnce
{
	private static readonly HashSet<string> _seen = new HashSet<string>();

	public static void Warn(string where, Exception ex)
	{
		if (!_seen.Add(where))
		{
			return;
		}
		try
		{
			MelonLogger.Warning("[catch] " + where + ": " + ((ex == null) ? "(no exception)" : (ex.GetType().Name + " - " + ex.Message)));
		}
		catch
		{
		}
	}
}
namespace DIYUpgrade
{
	public class DiyUpgradeMod : MelonMod
	{
		private static class Off
		{
			public const int TagSystem = 440;

			public const int Id = 448;

			public const int DragSource = 80;

			public const int DragTarget = 136;

			public const int DragTargetInv = 144;
		}

		internal enum Cat
		{
			None,
			Box,
			Backpack,
			Cage,
			MultiSlot,
			MultiSlotModule,
			ModContainer,
			WageBox,
			WageBackpack
		}

		internal sealed class TargetSlot
		{
			public MelonPreferences_Entry<bool> Enable;

			public MelonPreferences_Entry<int> IncW;

			public MelonPreferences_Entry<int> IncH;
		}

		internal sealed class GridDef
		{
			public MelonPreferences_Entry<bool> Enable;

			public MelonPreferences_Entry<string> Ids;

			public MelonPreferences_Entry<int> MaxW;

			public MelonPreferences_Entry<int> MaxH;

			public MelonPreferences_Entry<bool> AlignSlots;

			public TargetSlot[] Slots = Array.Empty<TargetSlot>();

			public string[] SlotNames;

			public string[] IdList = Array.Empty<string>();
		}

		[HarmonyPatch(typeof(GameItem), "MayHaveValidInventorySlot")]
		private static class MayHaveValidInventorySlotPatch
		{
			private static bool Prefix(GameItem __instance, GameItem item, ref bool __result)
			{
				if (!IsUpgradeKeyHeld())
				{
					return true;
				}
				if (TryResolve(item, __instance, out var _, out var _, out var _))
				{
					__result = false;
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(GameItem), "MayTarget")]
		private static class MayTargetPatch
		{
			[HarmonyPriority(800)]
			private static bool Prefix(GameItem __instance, GameItem targetItem, ref bool __result)
			{
				if (!IsUpgradeKeyHeld())
				{
					return true;
				}
				if (!TryResolve(__instance, targetItem, out var _, out var container, out var cat))
				{
					return true;
				}
				__result = PrepareUpgrade(container, cat, out var _, out var _);
				return false;
			}
		}

		[HarmonyPatch(typeof(ItemBehaviourManager), "Target")]
		private static class ManagerTargetPatch
		{
			[HarmonyPriority(800)]
			private static bool Prefix(ItemBehaviourManager __instance, GameItem source, GameItem target)
			{
				if (!IsUpgradeKeyHeld())
				{
					return true;
				}
				GameItem material;
				GameItem container;
				Cat cat;
				return !TryResolve(source, target, out material, out container, out cat);
			}
		}

		[HarmonyPatch(typeof(GameItem), "Target")]
		private static class TargetPatch
		{
			[HarmonyPriority(800)]
			private static bool Prefix(GameItem __instance, GameItem targetItem)
			{
				if (!IsUpgradeKeyHeld())
				{
					return true;
				}
				if (!TryResolve(__instance, targetItem, out var _, out var _, out var _))
				{
					return true;
				}
				return false;
			}
		}

		[HarmonyPatch(typeof(GameItem), "CanTarget")]
		private static class CanTargetPatch
		{
			[HarmonyPriority(800)]
			private static bool Prefix(GameItem __instance, GameItem targetItem, ref bool __result)
			{
				if (!IsUpgradeKeyHeld())
				{
					return true;
				}
				if (!TryResolve(__instance, targetItem, out var _, out var container, out var cat))
				{
					return true;
				}
				__result = PrepareUpgrade(container, cat, out var _, out var _);
				return false;
			}
		}

		[HarmonyPatch(typeof(GameItem), "ValidateShapeState")]
		private static class ValidateShapeStatePatch
		{
			private static void Postfix(GameItem __instance)
			{
				if (__instance == null || _isUpgrading)
				{
					return;
				}
				IntPtr intPtr = IntPtr.Zero;
				try
				{
					intPtr = ((Il2CppObjectBase)__instance).Pointer;
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:129", ex);
				}
				if (intPtr != IntPtr.Zero)
				{
					if (_lastReapplyTime.TryGetValue(intPtr, out var value) && (DateTime.UtcNow - value).TotalSeconds < 0.05)
					{
						return;
					}
					_lastReapplyTime[intPtr] = DateTime.UtcNow;
				}
				ReapplyFromMeta(__instance);
			}
		}

		[HarmonyPatch(typeof(ItemMouseDragHandler), "EndDrag")]
		private static class EndDragPatch
		{
			private static void Postfix(ItemMouseDragHandler __instance)
			{
				//IL_0090: Unknown result type (might be due to invalid IL or missing references)
				//IL_0096: Expected Obj, but got Unknown
				//IL_00de: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e5: Expected Obj, but got Unknown
				if (!IsUpgradeKeyHeld() || (DateTime.UtcNow - _lastEndDragProcessed).TotalSeconds < 0.4)
				{
					return;
				}
				_lastEndDragProcessed = DateTime.UtcNow;
				if ((DateTime.UtcNow - _lastTargetConsume).TotalSeconds < 0.5)
				{
					return;
				}
				IntPtr pointer = ((Il2CppObjectBase)__instance).Pointer;
				IntPtr intPtr = Marshal.ReadIntPtr(pointer, 80);
				IntPtr intPtr2 = Marshal.ReadIntPtr(pointer, 136);
				IntPtr intPtr3 = Marshal.ReadIntPtr(pointer, 144);
				if (intPtr == IntPtr.Zero)
				{
					return;
				}
				GameItem val = null;
				try
				{
					val = new GameItem(intPtr);
				}
				catch
				{
					val = null;
				}
				VerifyDragOffset(val);
				if (val == null || IdFieldNull(val))
				{
					if (val == null)
					{
						MarkOffsetSuspect();
					}
					return;
				}
				try
				{
					if (!IsAllowedMaterial(val))
					{
						return;
					}
				}
				catch
				{
					return;
				}
				GameItem val2 = null;
				if (intPtr2 != IntPtr.Zero)
				{
					try
					{
						val2 = new GameItem(intPtr2);
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:163", ex);
					}
				}
				else if (intPtr3 != IntPtr.Zero)
				{
					val2 = InventoryOwner(intPtr3);
				}
				if (val2 == null || !TryResolve(val, val2, out var material, out var container, out var cat))
				{
					return;
				}
				try
				{
					if (PrepareUpgradeInternal(container, cat, out var apply, out var reason))
					{
						apply();
						ConsumeMaterial(material);
						_lastTargetConsume = DateTime.UtcNow;
						try
						{
							_prepareCache.Clear();
							_ggiCache.Clear();
							return;
						}
						catch
						{
							return;
						}
					}
					LogOnce("[容器升级] 无法升级: " + reason);
				}
				catch (Exception ex2)
				{
					_log.Error("[容器升级] EndDrag异常: " + ex2.Message);
				}
			}
		}

		[HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic", new Type[] { })]
		private static class ContainerTooltipPatch
		{
			private static void Postfix(GameItemElement __instance, RichTextBuilder __result)
			{
				try
				{
					if (__result == null || __instance == null || IdFieldNull((GameItem)(object)__instance) || TagSystemFieldNull((GameItem)(object)__instance) || !((GameItem)__instance).IsTag("DIY_UPGRADE_W_TAG") || !((GameItem)__instance).IsTag("DIY_UPGRADE_H_TAG"))
					{
						return;
					}
					int num = SafeTagInt((GameItem)(object)__instance, "DIY_UPGRADE_W_TAG");
					int num2 = SafeTagInt((GameItem)(object)__instance, "DIY_UPGRADE_H_TAG");
					if (num <= 0 || num2 <= 0)
					{
						return;
					}
					try
					{
						string nakedText = __result.Build(-1).nakedText;
						if (!string.IsNullOrEmpty(nakedText) && (nakedText.Contains("| 范围 ") || nakedText.Contains("| 上限 ")))
						{
							return;
						}
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:202", ex);
					}
					int num3 = 0;
					int num4 = 0;
					int num5 = 0;
					if (TryGetMeta((GameItem)(object)__instance, out var baseW, out var baseH, out var spent))
					{
						num3 = ((spent > 0) ? spent : 0);
						num4 = baseW;
						num5 = baseH;
					}
					GetMaxSizeForItem((GameItem)(object)__instance, out var maxW, out var maxH);
					int num6 = 1;
					int num7 = 1;
					try
					{
						if (TryGetGridShapeLimits((GameItem)(object)__instance, out var minC, out var minR, out var maxC, out var maxR))
						{
							num6 = Math.Max(1, minC);
							num7 = Math.Max(1, minR);
							if (maxC > 0)
							{
								maxW = maxC;
							}
							if (maxR > 0)
							{
								maxH = maxR;
							}
						}
					}
					catch (Exception ex2)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:221", ex2);
					}
					string text = ((maxW > 0 && maxH > 0) ? (maxW + "x" + maxH) : "无");
					__result.Add("升级 " + num3 + " 次  ", (ColorPalette)(-1), "", false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
					__result.Add("当前 " + num + "x" + num2 + "  ", (ColorPalette)(-1), "", false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
					if (num4 > 0 && num5 > 0)
					{
						__result.Add("(基础 " + num4 + "x" + num5 + ")  ", (ColorPalette)(-1), "", false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
					}
					__result.Add("| 范围 " + num6 + "x" + num7 + " ~ " + text, (ColorPalette)(-1), "", false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
					__result.NextLine();
					try
					{
						GridDef gridDefForItem = GetGridDefForItem((GameItem)(object)__instance);
						if (gridDefForItem != null)
						{
							int num8 = ((gridDefForItem.MaxW != null && gridDefForItem.MaxW.Value > 0) ? gridDefForItem.MaxW.Value : 0);
							int num9 = ((gridDefForItem.MaxH != null && gridDefForItem.MaxH.Value > 0) ? gridDefForItem.MaxH.Value : 0);
							bool num10 = num8 > 0 && num8 != maxW;
							bool flag = num9 > 0 && num9 != maxH;
							if (num10 | flag)
							{
								string text2 = ((num8 > 0) ? num8.ToString() : "无");
								string text3 = ((num9 > 0) ? num9.ToString() : "无");
								__result.Add("最终上限 " + text2 + "x" + text3, (ColorPalette)(-1), "", false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
								__result.NextLine();
							}
						}
					}
					catch (Exception ex3)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:tooltip-final", ex3);
					}
					try
					{
						GridDef gridDefForItem2 = GetGridDefForItem((GameItem)(object)__instance);
						if (gridDefForItem2 == null)
						{
							return;
						}
						TargetSlot primarySlot = GetPrimarySlot(gridDefForItem2);
						int num11 = ((primarySlot != null) ? Math.Max(0, primarySlot.IncW.Value) : 0);
						int num12 = ((primarySlot != null) ? Math.Max(0, primarySlot.IncH.Value) : 0);
						if (num11 > 0 || num12 > 0)
						{
							int val = ((maxW <= 0) ? 1 : maxW);
							int val2 = ((maxH <= 0) ? 1 : maxH);
							int num13 = Math.Min(val, num + num11);
							int num14 = Math.Min(val2, num2 + num12);
							if (num13 != num || num14 != num2)
							{
								__result.Add("按住 " + GetUpgradeKeyLabel() + " 拖" + GetMaterialLabel() + "可升级至 " + num13 + "x" + num14, (ColorPalette)(-1), "", false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
								__result.NextLine();
							}
						}
					}
					catch (Exception ex4)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:247", ex4);
					}
				}
				catch (Exception ex5)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:249", ex5);
				}
			}
		}

		[HarmonyPatch(typeof(PixelWindow), "get_widthPixels")]
		internal static class WindowWidthPatch
		{
			private static void Postfix(PixelWindow __instance, ref int __result)
			{
				TryOverrideWindowWidth(__instance, ref __result);
			}
		}

		[HarmonyPatch(typeof(PixelWindow), "get_heightPixels")]
		internal static class WindowHeightPatch
		{
			private static void Postfix(PixelWindow __instance, ref int __result)
			{
				TryOverrideWindowHeight(__instance, ref __result);
			}
		}

		[HarmonyPatch(typeof(GameGridInventory), "get_widthPixels")]
		internal static class GridWidthPatch
		{
			private static void Postfix(GameGridInventory __instance, ref int __result)
			{
				TryOverrideChildWidth(__instance, ref __result);
			}
		}

		[HarmonyPatch(typeof(GameGridInventory), "get_heightPixels")]
		internal static class GridHeightPatch
		{
			private static void Postfix(GameGridInventory __instance, ref int __result)
			{
				TryOverrideChildHeight(__instance, ref __result);
			}
		}

		internal static class SaveSlotScope
		{
			private static int _cachedSlotId = -1;

			private static DateTime _cachedAt = DateTime.MinValue;

			private const double CacheSeconds = 1.5;

			internal static int GetCurrent()
			{
				DateTime utcNow = DateTime.UtcNow;
				if ((utcNow - _cachedAt).TotalSeconds < 1.5 && _cachedSlotId >= 0)
				{
					return _cachedSlotId;
				}
				_cachedAt = utcNow;
				try
				{
					if (!PlayerStore.IsInstanceExist() || PlayerStore.Instance == null)
					{
						_cachedSlotId = -1;
						return -1;
					}
					int saveSlotId = PlayerStore.Instance.saveSlotId;
					_cachedSlotId = ((saveSlotId >= 0) ? saveSlotId : (-1));
				}
				catch
				{
					_cachedSlotId = -1;
				}
				return _cachedSlotId;
			}

			internal static string Key(string suffix)
			{
				int current = GetCurrent();
				return ((current >= 0) ? ("slot" + current + ".") : "noslot.") + suffix;
			}

			internal static void Invalidate()
			{
				_cachedSlotId = -1;
				_cachedAt = DateTime.MinValue;
			}
		}

		internal static class ContainerWindowResizer
		{
			[Flags]
			internal enum ResizeEdge
			{
				None = 0,
				Left = 1,
				Right = 2,
				Top = 4,
				Bottom = 8
			}

			private sealed class ResizeState
			{
				private readonly List<ResizeHandle> _handles = new List<ResizeHandle>();

				private TreeNodeRender _overlay;

				private ResizeEdge _activeEdge;

				private ResizeEdge _hoveredEdge;

				private Vector2 _startPointer;

				private Vector2 _startPosition;

				private Vector2 _startPivot;

				private int _startWidth;

				private int _startHeight;

				private int _startColumns;

				private int _startRows;

				private float _columnPixels;

				private float _rowPixels;

				private float _visibleSince = -1f;

				private bool _restoreDone;

				internal GameItem Item { get; set; }

				internal PixelWindow Window { get; }

				internal bool IsAlive
				{
					get
					{
						try
						{
							if (Window == null || (Object)(object)Window.rectTransform == (Object)null)
							{
								return false;
							}
							if (!_enableDragResize.Value)
							{
								return true;
							}
							return _handles.Count == 7 && LiveHandleCount == 7;
						}
						catch
						{
							return false;
						}
					}
				}

				internal int LiveHandleCount
				{
					get
					{
						int num = 0;
						foreach (ResizeHandle handle in _handles)
						{
							try
							{
								TreeNodeRender node = handle.Node;
								if ((Object)(object)node != (Object)null && (Object)(object)node.rectTransform != (Object)null)
								{
									num++;
								}
							}
							catch (Exception ex)
							{
								CatchLogOnce.Warn("DiyUpgradeMod.cs:329", ex);
							}
						}
						return num;
					}
				}

				internal bool IsActiveResize => _activeEdge != ResizeEdge.None;

				internal ResizeState(GameItem item)
				{
					Item = item;
					Window = item.contentWindow;
				}

				internal ResizeEdge Tick(Vector2 pointer, bool pointerHeld, bool focused, out bool isActive, out int order)
				{
					//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
					bool flag = false;
					try
					{
						PixelWindow window = Window;
						if (window != null && (Object)(object)window.rectTransform != (Object)null)
						{
							flag = ((Component)window.rectTransform).gameObject.activeInHierarchy;
						}
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:348", ex);
					}
					if (flag && _visibleSince < 0f)
					{
						_visibleSince = Time.time;
					}
					if (flag && !_restoreDone && _visibleSince > 0f && Time.time - _visibleSince > 0.05f)
					{
						TryRestoreRememberedGridShape(Item);
						if (_enableDragResize.Value)
						{
							BringHandlesToFront();
						}
						_restoreDone = true;
					}
					order = GetWindowOrder();
					isActive = false;
					if (!flag || !focused || !_enableDragResize.Value)
					{
						FinishDrag(null);
						_hoveredEdge = ResizeEdge.None;
						return ResizeEdge.None;
					}
					if (_activeEdge != ResizeEdge.None)
					{
						if (pointerHeld)
						{
							isActive = true;
							return _activeEdge;
						}
						FinishDrag(null);
					}
					SyncOverlay();
					_hoveredEdge = FindHandleAtPointer(pointer, null);
					return _hoveredEdge;
				}

				internal void CreateHandles()
				{
					//IL_001d: Unknown result type (might be due to invalid IL or missing references)
					//IL_002c: Unknown result type (might be due to invalid IL or missing references)
					//IL_003b: Unknown result type (might be due to invalid IL or missing references)
					//IL_0056: Unknown result type (might be due to invalid IL or missing references)
					//IL_0065: Unknown result type (might be due to invalid IL or missing references)
					//IL_0074: Unknown result type (might be due to invalid IL or missing references)
					//IL_008f: Unknown result type (might be due to invalid IL or missing references)
					//IL_009e: Unknown result type (might be due to invalid IL or missing references)
					//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
					//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
					//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
					//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
					//IL_0101: Unknown result type (might be due to invalid IL or missing references)
					//IL_0110: Unknown result type (might be due to invalid IL or missing references)
					//IL_011f: Unknown result type (might be due to invalid IL or missing references)
					//IL_013b: Unknown result type (might be due to invalid IL or missing references)
					//IL_014a: Unknown result type (might be due to invalid IL or missing references)
					//IL_0159: Unknown result type (might be due to invalid IL or missing references)
					//IL_0175: Unknown result type (might be due to invalid IL or missing references)
					//IL_0184: Unknown result type (might be due to invalid IL or missing references)
					//IL_0193: Unknown result type (might be due to invalid IL or missing references)
					DestroyHandles();
					CreateOverlay();
					AddHandle("Left", ResizeEdge.Left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(12f, -32f));
					AddHandle("Right", ResizeEdge.Right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(12f, -32f));
					AddHandle("Bottom", ResizeEdge.Bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 12f));
					AddHandle("TopLeft", ResizeEdge.Left | ResizeEdge.Top, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, 20f));
					AddHandle("TopRight", ResizeEdge.Right | ResizeEdge.Top, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(20f, 20f));
					AddHandle("BottomLeft", ResizeEdge.Left | ResizeEdge.Bottom, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f));
					AddHandle("BottomRight", ResizeEdge.Right | ResizeEdge.Bottom, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(20f, 20f));
				}

				internal void BringHandlesToFront()
				{
					SyncOverlay();
					try
					{
						TreeNodeRender overlay = _overlay;
						if ((Object)(object)overlay != (Object)null && (Object)(object)overlay.rectTransform != (Object)null)
						{
							((Transform)overlay.rectTransform).SetAsLastSibling();
						}
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:396", ex);
					}
					foreach (ResizeHandle handle in _handles)
					{
						try
						{
							TreeNodeRender node = handle.Node;
							if ((Object)(object)node != (Object)null && (Object)(object)node.rectTransform != (Object)null)
							{
								((Transform)node.rectTransform).SetAsLastSibling();
							}
						}
						catch (Exception ex2)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:405", ex2);
						}
					}
					try
					{
						PixelWindow window = Window;
						if (window != null)
						{
							TreeNodeRender topCloseButton = window.topCloseButton;
							if ((Object)(object)topCloseButton != (Object)null && (Object)(object)topCloseButton.rectTransform != (Object)null)
							{
								((Transform)topCloseButton.rectTransform).SetAsLastSibling();
							}
						}
					}
					catch (Exception ex3)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:417", ex3);
					}
				}

				internal void DestroyHandles()
				{
					foreach (ResizeHandle handle in _handles)
					{
						try
						{
							TreeNodeRender node = handle.Node;
							if ((Object)(object)node != (Object)null && (Object)(object)node.rectTransform != (Object)null)
							{
								Object.Destroy((Object)(object)((Component)handle.Node.rectTransform).gameObject);
							}
						}
						catch (Exception ex)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:430", ex);
						}
					}
					try
					{
						TreeNodeRender overlay = _overlay;
						if ((Object)(object)overlay != (Object)null && (Object)(object)overlay.rectTransform != (Object)null)
						{
							Object.Destroy((Object)(object)((Component)_overlay.rectTransform).gameObject);
						}
					}
					catch (Exception ex2)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:438", ex2);
					}
					if (_activeEdge != ResizeEdge.None && _activeDragCount > 0)
					{
						_activeDragCount--;
					}
					_handles.Clear();
					_overlay = null;
					_activeEdge = ResizeEdge.None;
					_hoveredEdge = ResizeEdge.None;
				}

				private void CreateOverlay()
				{
					//IL_0055: Unknown result type (might be due to invalid IL or missing references)
					//IL_0060: Expected Obj, but got Unknown
					_overlay = TreeNodeRender.Instantiate(Window, "ContainerResizeOverlay");
					if ((Object)(object)_overlay == (Object)null || (Object)(object)_overlay.rectTransform == (Object)null)
					{
						_overlay = null;
						return;
					}
					((Transform)_overlay.rectTransform).SetParent((Transform)Window.rectTransform, false);
					Image image = _overlay.image;
					if ((Object)(object)image != (Object)null)
					{
						((Behaviour)image).enabled = false;
						((Graphic)image).raycastTarget = false;
					}
					if ((Object)(object)_overlay.mask != (Object)null)
					{
						((Behaviour)_overlay.mask).enabled = false;
					}
					SyncOverlay();
					((Transform)_overlay.rectTransform).SetAsLastSibling();
				}

				internal void SyncOverlay()
				{
					//IL_0058: Unknown result type (might be due to invalid IL or missing references)
					//IL_0064: Unknown result type (might be due to invalid IL or missing references)
					//IL_0070: Unknown result type (might be due to invalid IL or missing references)
					//IL_007c: Unknown result type (might be due to invalid IL or missing references)
					//IL_0088: Unknown result type (might be due to invalid IL or missing references)
					//IL_0093: Unknown result type (might be due to invalid IL or missing references)
					PixelWindow window = Window;
					RectTransform val = ((window == null || (Object)(object)window.outsideBorder == (Object)null) ? null : window.outsideBorder.rectTransform);
					RectTransform val2 = (((Object)(object)_overlay == (Object)null) ? null : _overlay.rectTransform);
					if ((Object)(object)val != (Object)null && (Object)(object)val2 != (Object)null)
					{
						val2.anchorMin = val.anchorMin;
						val2.anchorMax = val.anchorMax;
						val2.pivot = val.pivot;
						val2.anchoredPosition = val.anchoredPosition;
						val2.sizeDelta = val.sizeDelta;
						((Transform)val2).localScale = Vector3.one;
					}
				}

				private void AddHandle(string name, ResizeEdge edge, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
				{
					//IL_009d: Unknown result type (might be due to invalid IL or missing references)
					//IL_00a8: Expected Obj, but got Unknown
					//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
					//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
					//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
					//IL_00cd: Unknown result type (might be due to invalid IL or missing references)
					//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
					//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
					//IL_0135: Unknown result type (might be due to invalid IL or missing references)
					TreeNodeRender val = TreeNodeRender.Instantiate(Window, "ContainerResize" + name);
					if ((Object)(object)val != (Object)null && (Object)(object)val.rectTransform != (Object)null)
					{
						RectTransform rectTransform = val.rectTransform;
						RectTransform val2;
						if ((Object)(object)_overlay != (Object)null)
						{
							val2 = _overlay.rectTransform;
						}
						else
						{
							val2 = (((Object)(object)Window.outsideBorder == (Object)null) ? Window.rectTransform : Window.outsideBorder.rectTransform);
						}
						((Transform)rectTransform).SetParent((Transform)val2, false);
						rectTransform.anchorMin = anchorMin;
						rectTransform.anchorMax = anchorMax;
						rectTransform.pivot = new Vector2(0.5f, 0.5f);
						rectTransform.anchoredPosition = Vector2.zero;
						rectTransform.sizeDelta = sizeDelta;
						((Transform)rectTransform).localScale = Vector3.one;
						((Transform)rectTransform).SetAsLastSibling();
						Image val3 = val.image;
						if ((Object)(object)val3 == (Object)null)
						{
							val3 = ((Component)rectTransform).gameObject.AddComponent<Image>();
						}
						val.image = val3;
						((Behaviour)val3).enabled = true;
						((Graphic)val3).color = new Color(1f, 1f, 1f, 0.001f);
						((Graphic)val3).raycastTarget = true;
						val.InitEventHandler();
						Func<PointerEventData, bool> val4 = Convert((PointerEventData data) => PrepareDrag(data));
						Func<PointerEventData, bool> val5 = Convert((PointerEventData data) => BeginDrag(data, edge));
						Func<PointerEventData, bool> val6 = Convert((PointerEventData data) => Drag(data));
						Func<PointerEventData, bool> val7 = Convert((PointerEventData data) => EndDrag(data));
						Func<PointerEventData, bool> val8 = Convert((PointerEventData data) => EnterHandle(data, edge));
						Func<PointerEventData, bool> val9 = Convert((PointerEventData data) => ExitHandle(data, edge));
						val.eventHandler.mousePotentialDragFunc.Add(val4);
						val.eventHandler.mouseBeginDragFunc.Add(val5);
						val.eventHandler.mouseDragFunc.Add(val6);
						val.eventHandler.mouseEndDragFunc.Add(val7);
						val.eventHandler.mouseEnterFunc.Add(val8);
						val.eventHandler.mouseExitFunc.Add(val9);
						_handles.Add(new ResizeHandle(val, edge, val4, val5, val6, val7, val8, val9));
					}
				}

				private static Func<PointerEventData, bool> Convert(Func<PointerEventData, bool> action)
				{
					return DelegateSupport.ConvertDelegate<Func<PointerEventData, bool>>((Delegate)action);
				}

				private bool PrepareDrag(PointerEventData data)
				{
					if (data != null)
					{
						data.useDragThreshold = false;
					}
					return true;
				}

				private bool BeginDrag(PointerEventData data, ResizeEdge edge)
				{
					//IL_0036: Unknown result type (might be due to invalid IL or missing references)
					//IL_0037: Unknown result type (might be due to invalid IL or missing references)
					//IL_0226: Unknown result type (might be due to invalid IL or missing references)
					//IL_022b: Unknown result type (might be due to invalid IL or missing references)
					//IL_023c: Unknown result type (might be due to invalid IL or missing references)
					//IL_0241: Unknown result type (might be due to invalid IL or missing references)
					if (data != null && Window != null && (Object)(object)Window.rectTransform != (Object)null && TryPointerInParent(data, out var point))
					{
						_startPointer = point;
						GameGridInventory resizableInventory = GetResizableInventory(Item);
						if (resizableInventory == null || resizableInventory.inventoryShape == null)
						{
							return false;
						}
						_activeEdge = edge;
						_activeDragCount++;
						ResizeCursor.Set(edge);
						_startWidth = Window.widthPixels;
						_startHeight = Window.heightPixels;
						GridDef gridDefForItem = GetGridDefForItem(Item);
						bool valueOrDefault = gridDefForItem?.AlignSlots?.Value == true;
						GameGridInventory val = resizableInventory;
						int width = resizableInventory.inventoryShape.width;
						int height = resizableInventory.inventoryShape.height;
						if (valueOrDefault && gridDefForItem != null)
						{
							List<GameGridInventory> allGridInventories = GetAllGridInventories(Item);
							foreach (var item in GetEnabledSlotsWithIndex(gridDefForItem))
							{
								if (item.Index < 0 || item.Index >= allGridInventories.Count)
								{
									continue;
								}
								GameGridInventory val2 = allGridInventories[item.Index];
								if (((val2 != null) ? val2.inventoryShape : null) != null)
								{
									if (val2.inventoryShape.width > width)
									{
										width = val2.inventoryShape.width;
										val = val2;
									}
									if (val2.inventoryShape.height > height)
									{
										height = val2.inventoryShape.height;
									}
								}
							}
						}
						_startColumns = width;
						_startRows = height;
						_columnPixels = Math.Max(1f, (float)val.widthPixels / (float)Math.Max(1, val.inventoryShape.width));
						_rowPixels = Math.Max(1f, (float)val.heightPixels / (float)Math.Max(1, val.inventoryShape.height));
						_startPosition = Window.rectTransform.anchoredPosition;
						_startPivot = Window.rectTransform.pivot;
						Window.ToFront(true);
						((AbstractEventData)data).Use();
						BringHandlesToFront();
						return true;
					}
					return false;
				}

				private bool Drag(PointerEventData data)
				{
					//IL_0023: Unknown result type (might be due to invalid IL or missing references)
					//IL_0025: Unknown result type (might be due to invalid IL or missing references)
					//IL_002a: Unknown result type (might be due to invalid IL or missing references)
					//IL_002f: Unknown result type (might be due to invalid IL or missing references)
					//IL_004f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0071: Unknown result type (might be due to invalid IL or missing references)
					//IL_0091: Unknown result type (might be due to invalid IL or missing references)
					//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
					//IL_0233: Unknown result type (might be due to invalid IL or missing references)
					//IL_0238: Unknown result type (might be due to invalid IL or missing references)
					//IL_02e5: Unknown result type (might be due to invalid IL or missing references)
					if (_activeEdge == ResizeEdge.None || data == null || !TryPointerInParent(data, out var point))
					{
						return false;
					}
					ResizeCursor.Set(_activeEdge);
					Vector2 val = point - _startPointer;
					int num = _startWidth;
					int num2 = _startHeight;
					if ((_activeEdge & ResizeEdge.Left) != 0)
					{
						num = (int)Math.Round((float)_startWidth - val.x);
					}
					else if ((_activeEdge & ResizeEdge.Right) != 0)
					{
						num = (int)Math.Round((float)_startWidth + val.x);
					}
					if ((_activeEdge & ResizeEdge.Bottom) != 0)
					{
						num2 = (int)Math.Round((float)_startHeight - val.y);
					}
					else if ((_activeEdge & ResizeEdge.Top) != 0)
					{
						num2 = (int)Math.Round((float)_startHeight + val.y);
					}
					int num3 = _startColumns;
					int num4 = _startRows;
					if ((_activeEdge & (ResizeEdge.Left | ResizeEdge.Right)) != 0)
					{
						num3 += (int)Math.Round((float)(num - _startWidth) / _columnPixels);
					}
					if ((_activeEdge & (ResizeEdge.Top | ResizeEdge.Bottom)) != 0)
					{
						num4 += (int)Math.Round((float)(num2 - _startHeight) / _rowPixels);
					}
					GameGridInventory resizableInventory = GetResizableInventory(Item);
					if (resizableInventory == null || resizableInventory.inventoryShape == null)
					{
						return false;
					}
					if (!TryGetGridShapeLimits(Item, out var minC, out var minR, out var maxC, out var maxR))
					{
						return false;
					}
					num3 = Math.Max(minC, Math.Min(maxC, num3));
					num4 = Math.Max(minR, Math.Min(maxR, num4));
					int width = resizableInventory.inventoryShape.width;
					int height = resizableInventory.inventoryShape.height;
					if (num3 != width || num4 != height)
					{
						bool flag = true;
						foreach (GameGridInventory allGridInventory in GetAllGridInventories(Item))
						{
							if (!CanContainItems(allGridInventory, num3, num4))
							{
								flag = false;
								break;
							}
						}
						if (!flag)
						{
							ResizeCursor.SetForbidden();
							((AbstractEventData)data).Use();
							return true;
						}
						if (!TryResizeGridWindow(Item, num3, num4, remember: false, out var actualWidth, out var actualHeight))
						{
							ResizeCursor.SetForbidden();
							((AbstractEventData)data).Use();
							return true;
						}
						int num5 = actualWidth - _startWidth;
						int num6 = actualHeight - _startHeight;
						Vector2 startPosition = _startPosition;
						if ((_activeEdge & ResizeEdge.Left) != 0)
						{
							startPosition.x -= (1f - _startPivot.x) * (float)num5;
						}
						else if ((_activeEdge & ResizeEdge.Right) != 0)
						{
							startPosition.x += _startPivot.x * (float)num5;
						}
						if ((_activeEdge & ResizeEdge.Bottom) != 0)
						{
							startPosition.y -= (1f - _startPivot.y) * (float)num6;
						}
						else if ((_activeEdge & ResizeEdge.Top) != 0)
						{
							startPosition.y += _startPivot.y * (float)num6;
						}
						Window.rectTransform.anchoredPosition = startPosition;
					}
					((AbstractEventData)data).Use();
					return true;
				}

				private bool EndDrag(PointerEventData data)
				{
					if (!FinishDrag(data))
					{
						return false;
					}
					ResizeEdge hoveredEdge = FindHandleAtPointer(data);
					if (data != null)
					{
						_hoveredEdge = hoveredEdge;
					}
					if (_hoveredEdge == ResizeEdge.None)
					{
						ResizeCursor.Reset();
					}
					else
					{
						ResizeCursor.Set(_hoveredEdge);
					}
					if (data != null)
					{
						((AbstractEventData)data).Use();
					}
					return true;
				}

				private bool FinishDrag(PointerEventData data)
				{
					if (_activeEdge == ResizeEdge.None)
					{
						return false;
					}
					_activeEdge = ResizeEdge.None;
					if (_activeDragCount > 0)
					{
						_activeDragCount--;
					}
					try
					{
						List<GameGridInventory> allGridInventories = GetAllGridInventories(Item);
						foreach (var item2 in GetEnabledSlotsWithIndex(GetGridDefForItem(Item)))
						{
							int item = item2.Index;
							if (item >= 0 && item < allGridInventories.Count)
							{
								GameGridInventory val = allGridInventories[item];
								if (((val != null) ? val.inventoryShape : null) != null)
								{
									RememberSlotShape(Item, item, allGridInventories[item].inventoryShape.width, allGridInventories[item].inventoryShape.height);
								}
							}
						}
						if (allGridInventories.Count > 0)
						{
							GameGridInventory val2 = allGridInventories[0];
							if (((val2 != null) ? val2.inventoryShape : null) != null)
							{
								SetItemTag(Item, "DIY_UPGRADE_W_TAG", allGridInventories[0].inventoryShape.width);
								SetItemTag(Item, "DIY_UPGRADE_H_TAG", allGridInventories[0].inventoryShape.height);
							}
						}
						RememberWindowSize(Item, Window.widthPixels, Window.heightPixels);
						Window.FixBounds();
						Item.RememberContentWindowPositionIfMine(Window);
						SyncOverlay();
						Attach(Item);
						try
						{
							if (Window != null && (Object)(object)Window.rectTransform != (Object)null)
							{
								LayoutRebuilder.ForceRebuildLayoutImmediate(Window.rectTransform);
								if ((Object)(object)Window.outsideBorder != (Object)null && (Object)(object)Window.outsideBorder.rectTransform != (Object)null)
								{
									LayoutRebuilder.ForceRebuildLayoutImmediate(Window.outsideBorder.rectTransform);
								}
								Canvas.ForceUpdateCanvases();
								RefreshAllGridsOf(Item);
								Window.FixBounds();
							}
						}
						catch (Exception ex)
						{
							_log.Error("[容器升级][调试] FinishDrag 强制重建异常: " + ex.Message);
						}
						try
						{
							MelonCoroutines.Start(ForceRefreshWindowAfterRestore(Item));
						}
						catch (Exception ex2)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:FinishDrag", ex2);
						}
					}
					catch (Exception ex3)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:663", ex3);
					}
					ResizeCursor.Reset();
					return true;
				}

				internal void MarkRestoreDone()
				{
					_restoreDone = true;
				}

				internal void ResetRestoreDone()
				{
					_restoreDone = false;
					_visibleSince = -1f;
				}

				private bool EnterHandle(PointerEventData data, ResizeEdge edge)
				{
					_hoveredEdge = edge;
					ResizeCursor.Set(edge);
					return true;
				}

				private bool ExitHandle(PointerEventData data, ResizeEdge edge)
				{
					if (_hoveredEdge == edge)
					{
						_hoveredEdge = ResizeEdge.None;
					}
					if (_activeEdge == ResizeEdge.None)
					{
						ResizeCursor.Reset();
					}
					return true;
				}

				private ResizeEdge FindHandleAtPointer(PointerEventData data)
				{
					//IL_0005: Unknown result type (might be due to invalid IL or missing references)
					if (data != null)
					{
						return FindHandleAtPointer(data.position, data.pressEventCamera);
					}
					return _hoveredEdge;
				}

				private ResizeEdge FindHandleAtPointer(Vector2 pointer, Camera camera)
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					//IL_000b: Unknown result type (might be due to invalid IL or missing references)
					//IL_0057: Unknown result type (might be due to invalid IL or missing references)
					if (IsPointerOverCloseButton(pointer, camera) || IsPointerOverTopDragBorder(pointer, camera))
					{
						return ResizeEdge.None;
					}
					for (int num = _handles.Count - 1; num >= 0; num--)
					{
						ResizeHandle resizeHandle = _handles[num];
						try
						{
							TreeNodeRender node = resizeHandle.Node;
							RectTransform val = (((Object)(object)node != (Object)null) ? node.rectTransform : null);
							if ((Object)(object)val != (Object)null && RectTransformUtility.RectangleContainsScreenPoint(val, pointer, camera))
							{
								return resizeHandle.Edge;
							}
						}
						catch (Exception ex)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:699", ex);
						}
					}
					return ResizeEdge.None;
				}

				private bool IsPointerOverCloseButton(Vector2 pointer, Camera camera)
				{
					//IL_0043: Unknown result type (might be due to invalid IL or missing references)
					//IL_0054: Unknown result type (might be due to invalid IL or missing references)
					//IL_0059: Unknown result type (might be due to invalid IL or missing references)
					//IL_005a: Unknown result type (might be due to invalid IL or missing references)
					//IL_006f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0084: Unknown result type (might be due to invalid IL or missing references)
					//IL_0099: Unknown result type (might be due to invalid IL or missing references)
					try
					{
						PixelWindow window = Window;
						RectTransform val = ((window != null && (Object)(object)window.topCloseButton != (Object)null) ? window.topCloseButton.rectTransform : null);
						if ((Object)(object)val == (Object)null || !((Component)val).gameObject.activeInHierarchy)
						{
							return false;
						}
						Vector2 val2 = default;
						if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(val, pointer, camera, ref val2))
						{
							return false;
						}
						Rect rect = val.rect;
						return val2.x >= rect.xMin - 2f && val2.x <= rect.xMax + 2f && val2.y >= rect.yMin - 2f && val2.y <= rect.yMax + 2f;
					}
					catch
					{
						return false;
					}
				}

				private bool IsPointerOverTopDragBorder(Vector2 pointer, Camera camera)
				{
					//IL_0043: Unknown result type (might be due to invalid IL or missing references)
					//IL_0054: Unknown result type (might be due to invalid IL or missing references)
					//IL_0059: Unknown result type (might be due to invalid IL or missing references)
					//IL_005a: Unknown result type (might be due to invalid IL or missing references)
					//IL_006f: Unknown result type (might be due to invalid IL or missing references)
					//IL_0084: Unknown result type (might be due to invalid IL or missing references)
					//IL_0099: Unknown result type (might be due to invalid IL or missing references)
					try
					{
						PixelWindow window = Window;
						RectTransform val = ((window != null && (Object)(object)window.topDragBorder != (Object)null) ? window.topDragBorder.rectTransform : null);
						if ((Object)(object)val == (Object)null || !((Component)val).gameObject.activeInHierarchy)
						{
							return false;
						}
						Vector2 val2 = default;
						if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(val, pointer, camera, ref val2))
						{
							return false;
						}
						Rect rect = val.rect;
						return val2.x >= rect.xMin - 2f && val2.x <= rect.xMax + 2f && val2.y >= rect.yMin - 2f && val2.y <= rect.yMax + 2f;
					}
					catch
					{
						return false;
					}
				}

				private int GetWindowOrder()
				{
					try
					{
						return (Window != null && (Object)(object)Window.rectTransform != (Object)null) ? ((Transform)Window.rectTransform).GetSiblingIndex() : int.MinValue;
					}
					catch
					{
						return int.MinValue;
					}
				}

				private bool TryPointerInParent(PointerEventData data, out Vector2 point)
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_0043: Unknown result type (might be due to invalid IL or missing references)
					//IL_0049: Unknown result type (might be due to invalid IL or missing references)
					//IL_005a: Expected Obj, but got Unknown
					point = Vector2.zero;
					PixelWindow window = Window;
					Transform val = ((window != null && (Object)(object)window.rectTransform != (Object)null) ? ((Transform)window.rectTransform).parent : null);
					if ((Object)(object)val == (Object)null)
					{
						return false;
					}
					return RectTransformUtility.ScreenPointToLocalPointInRectangle(new RectTransform(((Il2CppObjectBase)val).Pointer), data.position, data.pressEventCamera, ref point);
				}
			}

			private sealed class ResizeHandle
			{
				internal TreeNodeRender Node { get; }

				internal ResizeEdge Edge { get; }

				internal Func<PointerEventData, bool> Potential { get; }

				internal Func<PointerEventData, bool> Begin { get; }

				internal Func<PointerEventData, bool> Drag { get; }

				internal Func<PointerEventData, bool> End { get; }

				internal Func<PointerEventData, bool> Enter { get; }

				internal Func<PointerEventData, bool> Exit { get; }

				internal ResizeHandle(TreeNodeRender node, ResizeEdge edge, Func<PointerEventData, bool> potential, Func<PointerEventData, bool> begin, Func<PointerEventData, bool> drag, Func<PointerEventData, bool> end, Func<PointerEventData, bool> enter, Func<PointerEventData, bool> exit)
				{
					Node = node;
					Edge = edge;
					Potential = potential;
					Begin = begin;
					Drag = drag;
					End = end;
					Enter = enter;
					Exit = exit;
				}
			}

			internal static class ResizeCursor
			{
				private enum Kind
				{
					Default,
					Horizontal,
					Vertical,
					DiagonalDown,
					DiagonalUp,
					Forbidden
				}

				private static readonly Dictionary<Kind, Texture2D> Textures = new Dictionary<Kind, Texture2D>();

				private static Kind _appliedCursor = Kind.Default;

				internal static void Set(ResizeEdge edge)
				{
					//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
					Kind kind;
					switch (edge)
					{
					case ResizeEdge.Left:
					case ResizeEdge.Right:
						kind = Kind.Horizontal;
						break;
					case ResizeEdge.Top:
					case ResizeEdge.Bottom:
						kind = Kind.Vertical;
						break;
					case ResizeEdge.Left | ResizeEdge.Top:
					case ResizeEdge.Right | ResizeEdge.Bottom:
						kind = Kind.DiagonalDown;
						break;
					default:
						kind = Kind.DiagonalUp;
						break;
					}
					bool flag = _appliedCursor != kind;
					if (!flag && Textures.TryGetValue(kind, out var value) && (Object)(object)value == (Object)null)
					{
						flag = true;
						_appliedCursor = Kind.Default;
					}
					if (flag)
					{
						if (!Textures.TryGetValue(kind, out var value2) || (Object)(object)value2 == (Object)null)
						{
							value2 = Create(kind);
							Textures[kind] = value2;
						}
						Cursor.visible = true;
						Cursor.SetCursor(value2, new Vector2(16f, 16f), (CursorMode)1);
						_appliedCursor = kind;
					}
				}

				internal static void SetForbidden()
				{
					//IL_0044: Unknown result type (might be due to invalid IL or missing references)
					if (_appliedCursor != Kind.Forbidden)
					{
						if (!Textures.TryGetValue(Kind.Forbidden, out var value) || (Object)(object)value == (Object)null)
						{
							value = Create(Kind.Forbidden);
							Textures[Kind.Forbidden] = value;
						}
						Cursor.visible = true;
						Cursor.SetCursor(value, new Vector2(16f, 16f), (CursorMode)1);
						_appliedCursor = Kind.Forbidden;
					}
				}

				internal static void Reset()
				{
					//IL_0008: Unknown result type (might be due to invalid IL or missing references)
					if (_appliedCursor != Kind.Default)
					{
						Cursor.SetCursor((Texture2D)null, Vector2.zero, (CursorMode)0);
						_appliedCursor = Kind.Default;
					}
				}

				internal static void Invalidate()
				{
					//IL_005a: Unknown result type (might be due to invalid IL or missing references)
					foreach (Texture2D value in Textures.Values)
					{
						try
						{
							Object.Destroy((Object)(object)value);
						}
						catch (Exception ex)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:819", ex);
						}
					}
					Textures.Clear();
					_appliedCursor = Kind.Default;
					try
					{
						Cursor.SetCursor((Texture2D)null, Vector2.zero, (CursorMode)0);
					}
					catch (Exception ex2)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:822", ex2);
					}
				}

				internal static void Dispose()
				{
					Reset();
					foreach (Texture2D value in Textures.Values)
					{
						try
						{
							Object.Destroy((Object)(object)value);
						}
						catch (Exception ex)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:828", ex);
						}
					}
					Textures.Clear();
				}

				private static Texture2D Create(Kind kind)
				{
					//IL_0007: Unknown result type (might be due to invalid IL or missing references)
					//IL_000d: Expected Obj, but got Unknown
					//IL_004e: Unknown result type (might be due to invalid IL or missing references)
					//IL_0080: Unknown result type (might be due to invalid IL or missing references)
					//IL_0085: Unknown result type (might be due to invalid IL or missing references)
					//IL_0086: Unknown result type (might be due to invalid IL or missing references)
					//IL_008b: Unknown result type (might be due to invalid IL or missing references)
					//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
					//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
					//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
					//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
					//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
					//IL_010f: Unknown result type (might be due to invalid IL or missing references)
					//IL_011e: Unknown result type (might be due to invalid IL or missing references)
					//IL_012d: Unknown result type (might be due to invalid IL or missing references)
					//IL_013c: Unknown result type (might be due to invalid IL or missing references)
					//IL_014c: Unknown result type (might be due to invalid IL or missing references)
					//IL_015c: Unknown result type (might be due to invalid IL or missing references)
					//IL_0170: Unknown result type (might be due to invalid IL or missing references)
					//IL_017f: Unknown result type (might be due to invalid IL or missing references)
					//IL_018e: Unknown result type (might be due to invalid IL or missing references)
					//IL_019e: Unknown result type (might be due to invalid IL or missing references)
					//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
					//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
					//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
					//IL_01db: Unknown result type (might be due to invalid IL or missing references)
					//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
					//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
					//IL_020e: Unknown result type (might be due to invalid IL or missing references)
					//IL_021b: Unknown result type (might be due to invalid IL or missing references)
					//IL_0228: Unknown result type (might be due to invalid IL or missing references)
					//IL_0238: Unknown result type (might be due to invalid IL or missing references)
					//IL_0248: Unknown result type (might be due to invalid IL or missing references)
					//IL_0256: Unknown result type (might be due to invalid IL or missing references)
					//IL_0263: Unknown result type (might be due to invalid IL or missing references)
					//IL_0270: Unknown result type (might be due to invalid IL or missing references)
					//IL_0280: Unknown result type (might be due to invalid IL or missing references)
					//IL_0290: Unknown result type (might be due to invalid IL or missing references)
					//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
					//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
					//IL_02c0: Unknown result type (might be due to invalid IL or missing references)
					//IL_02ce: Unknown result type (might be due to invalid IL or missing references)
					//IL_02dd: Unknown result type (might be due to invalid IL or missing references)
					//IL_02eb: Unknown result type (might be due to invalid IL or missing references)
					//IL_02fa: Unknown result type (might be due to invalid IL or missing references)
					//IL_0308: Unknown result type (might be due to invalid IL or missing references)
					//IL_0316: Unknown result type (might be due to invalid IL or missing references)
					//IL_0325: Unknown result type (might be due to invalid IL or missing references)
					//IL_0335: Unknown result type (might be due to invalid IL or missing references)
					//IL_0343: Unknown result type (might be due to invalid IL or missing references)
					//IL_0352: Unknown result type (might be due to invalid IL or missing references)
					//IL_0360: Unknown result type (might be due to invalid IL or missing references)
					//IL_0062: Unknown result type (might be due to invalid IL or missing references)
					Texture2D val = new Texture2D(32, 32, (TextureFormat)4, false, false);
					((Object)val).name = "ContainerResizeCursor" + kind;
					((Texture)val).filterMode = (FilterMode)0;
					((Texture)val).wrapMode = (TextureWrapMode)1;
					Color val2 = new Color(0f, 0f, 0f, 0f);
					for (int i = 0; i < 32; i++)
					{
						for (int j = 0; j < 32; j++)
						{
							val.SetPixel(i, j, val2);
						}
					}
					Color white = Color.white;
					Color black = Color.black;
					Color color = new Color(1f, 0.2f, 0.2f, 1f);
					switch (kind)
					{
					case Kind.Horizontal:
						DrawLine(val, 6, 16, 26, 16, 2, black);
						DrawLine(val, 6, 16, 11, 11, 2, black);
						DrawLine(val, 6, 16, 11, 21, 2, black);
						DrawLine(val, 26, 16, 21, 11, 2, black);
						DrawLine(val, 26, 16, 21, 21, 2, black);
						DrawLine(val, 6, 16, 26, 16, 1, white);
						DrawLine(val, 6, 16, 11, 11, 1, white);
						DrawLine(val, 6, 16, 11, 21, 1, white);
						DrawLine(val, 26, 16, 21, 11, 1, white);
						DrawLine(val, 26, 16, 21, 21, 1, white);
						break;
					case Kind.Vertical:
						DrawLine(val, 16, 6, 16, 26, 2, black);
						DrawLine(val, 16, 6, 11, 11, 2, black);
						DrawLine(val, 16, 6, 21, 11, 2, black);
						DrawLine(val, 16, 26, 11, 21, 2, black);
						DrawLine(val, 16, 26, 21, 21, 2, black);
						DrawLine(val, 16, 6, 16, 26, 1, white);
						DrawLine(val, 16, 6, 11, 11, 1, white);
						DrawLine(val, 16, 6, 21, 11, 1, white);
						DrawLine(val, 16, 26, 11, 21, 1, white);
						DrawLine(val, 16, 26, 21, 21, 1, white);
						break;
					case Kind.DiagonalDown:
						DrawLine(val, 6, 6, 26, 26, 2, black);
						DrawLine(val, 6, 6, 6, 14, 2, black);
						DrawLine(val, 6, 6, 14, 6, 2, black);
						DrawLine(val, 26, 26, 26, 18, 2, black);
						DrawLine(val, 26, 26, 18, 26, 2, black);
						DrawLine(val, 6, 6, 26, 26, 1, white);
						DrawLine(val, 6, 6, 6, 14, 1, white);
						DrawLine(val, 6, 6, 14, 6, 1, white);
						DrawLine(val, 26, 26, 26, 18, 1, white);
						DrawLine(val, 26, 26, 18, 26, 1, white);
						break;
					case Kind.DiagonalUp:
						DrawLine(val, 26, 6, 6, 26, 2, black);
						DrawLine(val, 26, 6, 26, 14, 2, black);
						DrawLine(val, 26, 6, 18, 6, 2, black);
						DrawLine(val, 6, 26, 6, 18, 2, black);
						DrawLine(val, 6, 26, 14, 26, 2, black);
						DrawLine(val, 26, 6, 6, 26, 1, white);
						DrawLine(val, 26, 6, 26, 14, 1, white);
						DrawLine(val, 26, 6, 18, 6, 1, white);
						DrawLine(val, 6, 26, 6, 18, 1, white);
						DrawLine(val, 6, 26, 14, 26, 1, white);
						break;
					case Kind.Forbidden:
						DrawCircle(val, 16, 16, 12, 2, black);
						DrawCircle(val, 16, 16, 12, 1, color);
						DrawLine(val, 8, 8, 24, 24, 2, black);
						DrawLine(val, 8, 8, 24, 24, 1, color);
						break;
					}
					val.Apply(false, false);
					return val;
				}

				private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, int radius, Color color)
				{
					//IL_005f: Unknown result type (might be due to invalid IL or missing references)
					int num = Math.Abs(x1 - x0);
					int num2 = ((x0 < x1) ? 1 : (-1));
					int num3 = -Math.Abs(y1 - y0);
					int num4 = ((y0 < y1) ? 1 : (-1));
					int num5 = num + num3;
					while (true)
					{
						for (int i = x0 - radius; i <= x0 + radius; i++)
						{
							for (int j = y0 - radius; j <= y0 + radius; j++)
							{
								if (i >= 0 && i < ((Texture)tex).width && j >= 0 && j < ((Texture)tex).height)
								{
									tex.SetPixel(i, j, color);
								}
							}
						}
						if (x0 != x1 || y0 != y1)
						{
							int num6 = 2 * num5;
							if (num6 >= num3)
							{
								num5 += num3;
								x0 += num2;
							}
							if (num6 <= num)
							{
								num5 += num;
								y0 += num4;
							}
							continue;
						}
						break;
					}
				}

				private static void DrawCircle(Texture2D tex, int cx, int cy, int radius, int thickness, Color color)
				{
					//IL_008c: Unknown result type (might be due to invalid IL or missing references)
					for (int i = 0; i < 64; i++)
					{
						float num = (float)((double)i * 2.0 * Math.PI / 64.0);
						int num2 = cx + (int)Math.Round((double)radius * Math.Cos(num));
						int num3 = cy + (int)Math.Round((double)radius * Math.Sin(num));
						for (int j = -thickness; j <= thickness; j++)
						{
							for (int k = -thickness; k <= thickness; k++)
							{
								int num4 = num2 + j;
								int num5 = num3 + k;
								if (num4 >= 0 && num4 < ((Texture)tex).width && num5 >= 0 && num5 < ((Texture)tex).height)
								{
									tex.SetPixel(num4, num5, color);
								}
							}
						}
					}
				}
			}

			private static readonly Dictionary<IntPtr, ResizeState> States = new Dictionary<IntPtr, ResizeState>();

			private static readonly List<IntPtr> _cleanupBuffer = new List<IntPtr>();

			private static bool _lastEnableDragResizeState = false;

			private static DateTime _lastHandleCheck = DateTime.MinValue;

			private static int _activeDragCount = 0;

			internal static bool IsDragging => _activeDragCount > 0;

			internal static void Attach(GameItem item)
			{
				if (!IsResizableContainer(item) || item.contentWindow == null || (Object)(object)item.contentWindow.rectTransform == (Object)null)
				{
					return;
				}
				IntPtr pointer = ((Il2CppObjectBase)item.contentWindow).Pointer;
				if (!States.TryGetValue(pointer, out var value) || !value.IsAlive)
				{
					if (States.TryGetValue(pointer, out var value2) && value2 != null)
					{
						try
						{
							value2.DestroyHandles();
						}
						catch (Exception ex)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:937", ex);
						}
					}
					value = new ResizeState(item);
					States[pointer] = value;
					if (_enableDragResize.Value)
					{
						value.CreateHandles();
					}
				}
				else
				{
					value.Item = item;
					if (_enableDragResize.Value && value.LiveHandleCount < 7)
					{
						value.CreateHandles();
					}
					if (_enableDragResize.Value)
					{
						value.BringHandlesToFront();
					}
				}
			}

			internal static void MarkRestored(GameItem item)
			{
				if (item == null)
				{
					return;
				}
				try
				{
					PixelWindow contentWindow = item.contentWindow;
					if (contentWindow != null)
					{
						IntPtr pointer = ((Il2CppObjectBase)contentWindow).Pointer;
						if (States.TryGetValue(pointer, out var value))
						{
							value.MarkRestoreDone();
						}
					}
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:962", ex);
				}
			}

			internal static void Tick()
			{
				//IL_02d3: Unknown result type (might be due to invalid IL or missing references)
				//IL_0273: Unknown result type (might be due to invalid IL or missing references)
				//IL_0278: Unknown result type (might be due to invalid IL or missing references)
				//IL_027a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0281: Unknown result type (might be due to invalid IL or missing references)
				//IL_0288: Unknown result type (might be due to invalid IL or missing references)
				//IL_028d: Unknown result type (might be due to invalid IL or missing references)
				bool value = _enableDragResize.Value;
				if (value != _lastEnableDragResizeState)
				{
					_lastEnableDragResizeState = value;
					foreach (ResizeState value3 in States.Values)
					{
						try
						{
							if (value)
							{
								if (value3.LiveHandleCount < 7)
								{
									value3.CreateHandles();
								}
								value3.BringHandlesToFront();
							}
							else
							{
								value3.DestroyHandles();
							}
						}
						catch (Exception ex)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:982", ex);
						}
					}
					if (!value)
					{
						ResizeCursor.Reset();
					}
				}
				if (States.Count > 0)
				{
					_cleanupBuffer.Clear();
					foreach (KeyValuePair<IntPtr, ResizeState> state in States)
					{
						ResizeState value2 = state.Value;
						bool flag = value2.Window == null || (Object)(object)value2.Window.rectTransform == (Object)null || ((Il2CppObjectBase)value2.Window).Pointer == IntPtr.Zero;
						bool flag2 = false;
						try
						{
							flag2 = value2.Item == null || !IsResizableContainer(value2.Item);
						}
						catch (Exception ex2)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:994", ex2);
						}
						if (flag | flag2)
						{
							try
							{
								value2.DestroyHandles();
							}
							catch (Exception ex3)
							{
								CatchLogOnce.Warn("DiyUpgradeMod.cs:997", ex3);
							}
							_cleanupBuffer.Add(state.Key);
						}
					}
					for (int i = 0; i < _cleanupBuffer.Count; i++)
					{
						States.Remove(_cleanupBuffer[i]);
					}
				}
				if (_enableDragResize.Value && States.Count > 0)
				{
					DateTime utcNow = DateTime.UtcNow;
					if ((utcNow - _lastHandleCheck).TotalSeconds >= 0.5)
					{
						_lastHandleCheck = utcNow;
						foreach (ResizeState value4 in States.Values)
						{
							try
							{
								if (value4.LiveHandleCount < 7 && value4.Window != null && (Object)(object)value4.Window.rectTransform != (Object)null)
								{
									value4.CreateHandles();
								}
							}
							catch (Exception ex4)
							{
								CatchLogOnce.Warn("DiyUpgradeMod.cs:1016", ex4);
							}
						}
					}
				}
				Vector2 pointer;
				bool mouseButton;
				bool isFocused;
				try
				{
					Vector3 mousePosition = Input.mousePosition;
					pointer = new Vector2(mousePosition.x, mousePosition.y);
					mouseButton = Input.GetMouseButton(0);
					isFocused = Application.isFocused;
				}
				catch
				{
					ResizeCursor.Reset();
					return;
				}
				ResizeEdge resizeEdge = ResizeEdge.None;
				bool flag3 = false;
				int num = int.MinValue;
				foreach (ResizeState value5 in States.Values)
				{
					try
					{
						ResizeEdge resizeEdge2 = value5.Tick(pointer, mouseButton, isFocused, out var isActive, out var order);
						if (resizeEdge2 != ResizeEdge.None && ((isActive && !flag3) || (isActive == flag3 && order >= num)))
						{
							resizeEdge = resizeEdge2;
							flag3 = isActive;
							num = order;
						}
					}
					catch (Exception ex5)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1048", ex5);
					}
				}
				if (resizeEdge == ResizeEdge.None)
				{
					ResizeCursor.Reset();
				}
				else
				{
					ResizeCursor.Set(resizeEdge);
				}
			}

			internal static void InvalidateCursor()
			{
				try
				{
					ResizeCursor.Invalidate();
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:1054", ex);
				}
			}

			internal static void ResetAllRestoreDone()
			{
				foreach (ResizeState value in States.Values)
				{
					try
					{
						value.ResetRestoreDone();
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1058", ex);
					}
				}
			}

			internal static void Dispose()
			{
				foreach (ResizeState value in States.Values)
				{
					try
					{
						value.DestroyHandles();
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1063", ex);
					}
				}
				States.Clear();
				ResizeCursor.Dispose();
			}
		}

		private sealed class PrepareCacheEntry
		{
			public DateTime Time;

			public bool Can;

			public string Reason;
		}

		[HarmonyPatch(typeof(MaterialDirectory), "OnIngotTarget")]
		internal static class MaterialDirectoryOnIngotTargetPatch
		{
			[HarmonyPriority(800)]
			private static bool Prefix(GameItem source, GameItem target)
			{
				if (!IsUpgradeKeyHeld())
				{
					return true;
				}
				if (TryResolve(source, target, out var _, out var _, out var _))
				{
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(MaterialDirectory), "CanIngotTarget")]
		internal static class MaterialDirectoryCanIngotTargetPatch
		{
			[HarmonyPriority(800)]
			private static bool Prefix(GameItem source, GameItem target, ref bool __result)
			{
				if (!IsUpgradeKeyHeld())
				{
					return true;
				}
				if (TryResolve(source, target, out var _, out var container, out var cat))
				{
					__result = PrepareUpgrade(container, cat, out var _, out var _);
					return false;
				}
				return true;
			}
		}

		[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
		internal static class LoadGameRestorePatch
		{
			private static DateTime _lastLoadGamePostfix = DateTime.MinValue;

			[HarmonyPriority(0)]
			private static void Postfix()
			{
				DateTime utcNow = DateTime.UtcNow;
				if ((utcNow - _lastLoadGamePostfix).TotalSeconds < 1.5)
				{
					return;
				}
				_lastLoadGamePostfix = utcNow;
				try
				{
					SaveSlotScope.Invalidate();
				}
				catch
				{
				}
				try
				{
					MelonCoroutines.Start(DelayedRestore());
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:1120", ex);
				}
			}

			private static IEnumerator DelayedRestore()
			{
				HashSet<string> allIds = new HashSet<string>(StringComparer.Ordinal);
				Collect(_box);
				Collect(_backpack);
				Collect(_cage);
				Collect(_multiSlot);
				Collect(_multiSlotModule);
				Collect(WageContainerRegistry._wageBox);
				Collect(WageContainerRegistry._wageBackpack);
				Collect(WageContainerRegistry._modContainer);
				int[] delays = new int[1] { 30 };
				int value = 0;
				for (int round = 0; round < delays.Length; round++)
				{
					for (int j = 0; j < delays[round]; j++)
					{
						yield return null;
					}
					try
					{
						ContainerWindowResizer.ResetAllRestoreDone();
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1135", ex);
					}
					int num = 0;
					foreach (string item in allIds)
					{
						try
						{
							GameItem val = DirectoryMaster.Item(item, true);
							if (val != null)
							{
								ReapplyFromMeta(val);
								num++;
							}
						}
						catch (Exception ex2)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:1144", ex2);
						}
					}
					value = num;
				}
				int num2 = 0;
				foreach (string item2 in allIds)
				{
					try
					{
						GameItem val2 = DirectoryMaster.Item(item2, true);
						if (val2 != null && SeedBaselineIfMissing(val2))
						{
							num2++;
						}
					}
					catch (Exception ex3)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1156", ex3);
					}
				}
				MelonLogger.Msg($"[容器升级] 读档恢复完成，处理了 {value} 个容器，补记基准 {num2} 个");
				void Collect(GridDef d)
				{
					if (d?.IdList != null)
					{
						string[] idList = d.IdList;
						foreach (string text in idList)
						{
							if (!string.IsNullOrEmpty(text) && text.IndexOf('*') < 0 && text.IndexOf('?') < 0)
							{
								allIds.Add(text);
							}
						}
					}
				}
			}
		}

		private static readonly HashSet<string> XiaowoBayIds = new HashSet<string>(StringComparer.Ordinal) { "machine_bay", "storage_bay", "machine_bay_ext", "storage_bay_large", "smuggler_bay", "smuggler_bay_mod" };

		private static Instance _log;

		private static MelonPreferences_Entry<bool> _materialJunk;

		private static MelonPreferences_Entry<bool> _materialScrapMetal;

		private static MelonPreferences_Entry<bool> _materialMetalIngot;

		private static MelonPreferences_Entry<int> _cageHpPerCell;

		private static GridDef _box;

		private static GridDef _backpack;

		private static GridDef _cage;

		private static GridDef _multiSlot;

		private static GridDef _multiSlotModule;

		private static MelonPreferences_Entry<string> _upgradeKey;

		private static KeyCode _upgradeKeyCode = (KeyCode)308;

		private static bool _upgradeModCtrl;

		private static bool _upgradeModAlt;

		private static bool _upgradeModShift;

		private static MelonPreferences_Entry<bool> _enableDragResize;

		private static MelonPreferences_Entry<bool> _diagLog;

		private static DateTime _lastTargetConsume = DateTime.MinValue;

		private static DateTime _lastEndDragProcessed = DateTime.MinValue;

		private static bool _prefsDirty = false;

		private static DateTime _lastPrefsFlush = DateTime.MinValue;

		private static volatile bool _isUpgrading = false;

		private static int _offsetSuspectCount = 0;

		private static bool _offsetWarningLogged = false;

		private static int _offsetCheckFails = 0;

		private static bool _offsetChecked = false;

		private static readonly Dictionary<string, (int BaseW, int BaseH, int Spent)> _metaCache = new Dictionary<string, (int, int, int)>();

		private static readonly Dictionary<IntPtr, DateTime> _lastReapplyTime = new Dictionary<IntPtr, DateTime>();

		private static readonly Dictionary<IntPtr, DateTime> _lastReapplyFromMetaTime = new Dictionary<IntPtr, DateTime>();

		private static readonly Dictionary<IntPtr, (DateTime Time, List<GameGridInventory> List)> _ggiCache = new Dictionary<IntPtr, (DateTime, List<GameGridInventory>)>();

		private static readonly Dictionary<IntPtr, (DateTime Time, bool Value)> _resizableCache = new Dictionary<IntPtr, (DateTime, bool)>();

		private const double ResizableCacheSeconds = 0.3;

		private static readonly Dictionary<IntPtr, PrepareCacheEntry> _prepareCache = new Dictionary<IntPtr, PrepareCacheEntry>();

		private const double PrepareCacheSeconds = 0.2;

		[ThreadStatic]
		private static IntPtr _layoutWindow;

		[ThreadStatic]
		private static IntPtr _layoutInventory;

		[ThreadStatic]
		private static int _layoutWindowWidth;

		[ThreadStatic]
		private static int _layoutWindowHeight;

		[ThreadStatic]
		private static int _layoutChildWidth;

		[ThreadStatic]
		private static int _layoutChildHeight;

		private static readonly Dictionary<string, DateTime> _lastLogTimeByKey = new Dictionary<string, DateTime>();

		private static readonly Dictionary<IntPtr, (int W, int H, int WinH)> _lastHideBounds = new Dictionary<IntPtr, (int, int, int)>();

		private static readonly Dictionary<IntPtr, (int PeakW, int PeakH)> _peakCache = new Dictionary<IntPtr, (int, int)>();

		private static readonly Dictionary<string, Type> _typeByNameCache = new Dictionary<string, Type>();

		private static readonly HashSet<string> OptionalPatchNames = new HashSet<string>(StringComparer.Ordinal);

		private static readonly HashSet<IntPtr> _redrawPending = new HashSet<IntPtr>();

		internal static bool IsDiagEnabled
		{
			get
			{
				try
				{
					return _diagLog != null && _diagLog.Value;
				}
				catch
				{
					return false;
				}
			}
		}

		private static void RememberPeakCache(GameItem item, int peakW, int peakH)
		{
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)item).Pointer;
				if (!(pointer == IntPtr.Zero))
				{
					_peakCache.TryGetValue(pointer, out (int, int) value);
					Dictionary<IntPtr, (int PeakW, int PeakH)> peakCache = _peakCache;
					int item2;
					if (peakW <= 0)
					{
						(item2, _) = value;
					}
					else
					{
						item2 = peakW;
					}
					peakCache[pointer] = (item2, (peakH > 0) ? peakH : value.Item2);
					if (_peakCache.Count > 128)
					{
						_peakCache.Clear();
					}
				}
			}
			catch
			{
			}
		}

		internal static void Diag(string msg)
		{
			if (!IsDiagEnabled)
			{
				return;
			}
			try
			{
				MelonLogger.Msg(msg);
			}
			catch
			{
			}
		}

		private static IEnumerator ForceRebuildAfterUpgrade(PixelWindow window, GameGridInventory inventory)
		{
			int frameCount = 5;
			try
			{
				if (((inventory != null) ? inventory.items : null) == null || inventory.items.Count == 0)
				{
					frameCount = 3;
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1228", ex);
			}
			for (int i = 0; i < frameCount; i++)
			{
				yield return null;
				try
				{
					if (window == null || (Object)(object)window.rectTransform == (Object)null)
					{
						break;
					}
					if (inventory != null)
					{
						inventory.ForceRecomputeDynamicBackground();
					}
					try
					{
						if (inventory != null)
						{
							((GameInventory)inventory).Validate();
						}
					}
					catch (Exception ex2)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1236", ex2);
					}
					window.Validate();
					window.FixBounds();
					if (i == 0)
					{
						Canvas.ForceUpdateCanvases();
					}
					LayoutRebuilder.ForceRebuildLayoutImmediate(window.rectTransform);
					if ((Object)(object)window.outsideBorder != (Object)null)
					{
						LayoutRebuilder.ForceRebuildLayoutImmediate(window.outsideBorder.rectTransform);
					}
					if (i >= 2 || inventory == null)
					{
						continue;
					}
					Component val = ((Il2CppObjectBase)inventory).TryCast<Component>();
					if (!((Object)(object)val != (Object)null) || !((Object)(object)val.gameObject != (Object)null))
					{
						continue;
					}
					Transform transform = val.gameObject.transform;
					int childCount = transform.childCount;
					for (int j = 0; j < childCount; j++)
					{
						Transform child = transform.GetChild(j);
						if (!((Object)(object)child == (Object)null))
						{
							Graphic val2 = ((Il2CppObjectBase)child).TryCast<Graphic>();
							if ((Object)(object)val2 != (Object)null)
							{
								val2.SetAllDirty();
							}
						}
					}
				}
				catch (Exception ex3)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:1262", ex3);
				}
			}
		}

		private static void GetMaxSizeForItem(GameItem item, out int maxW, out int maxH)
		{
			maxW = 0;
			maxH = 0;
			GridDef gridDefForItem = GetGridDefForItem(item);
			if (gridDefForItem != null)
			{
				maxW = gridDefForItem.MaxW.Value;
				maxH = gridDefForItem.MaxH.Value;
			}
		}

		private static string TagSlotW(int idx)
		{
			return "DIY_UPGRADE_SLOT" + idx + "_W_TAG";
		}

		private static string TagSlotH(int idx)
		{
			return "DIY_UPGRADE_SLOT" + idx + "_H_TAG";
		}

		internal static void SyncXiaowoBayTags(GameItem item, GameGridInventory inv)
		{
			if (item == null || ((inv != null) ? inv.inventoryShape : null) == null)
			{
				return;
			}
			try
			{
				string text = item.identifier?.Replace("\0", "").Trim() ?? "";
				if (XiaowoBayIds.Contains(text))
				{
					int width = inv.inventoryShape.width;
					int height = inv.inventoryShape.height;
					int widthPixels = inv.widthPixels;
					int heightPixels = inv.heightPixels;
					SetItemTag(item, "XIAOWO_BAY_UNLOCKED_COLUMNS", width);
					SetItemTag(item, "XIAOWO_BAY_UNLOCKED_ROWS", height);
					SetItemTag(item, "XIAOWO_BAY_LAYOUT_VERSION", 2);
					int value = Math.Max(0, LoadMeta(item).Spent);
					SetItemTag(item, "XIAOWO_BAY_EXPANSION_LEVEL", value);
					long num = 0L;
					try
					{
						num = item.uniqueId;
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1294", ex);
					}
					string text2 = "XIAOWOTradePerks.ContainerWindow." + text + "." + num + ".";
					PlayerPrefs.SetInt(text2 + "Width", widthPixels);
					PlayerPrefs.SetInt(text2 + "Height", heightPixels);
					PlayerPrefs.SetInt(text2 + "Columns", width);
					PlayerPrefs.SetInt(text2 + "Rows", height);
					_prefsDirty = true;
				}
			}
			catch (Exception ex2)
			{
				if (_log != null)
				{
					_log.Warning("[容器升级] 同步 XIAOWO tag 失败：" + ex2.Message);
				}
			}
		}

		public override void OnInitializeMelon()
		{
			//IL_037f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0384: Unknown result type (might be due to invalid IL or missing references)
			//IL_038a: Expected Obj, but got Unknown
			//IL_038f: Expected Obj, but got Unknown
			//IL_0394: Unknown result type (might be due to invalid IL or missing references)
			//IL_039e: Expected Obj, but got Unknown
			//IL_03a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ad: Expected Obj, but got Unknown
			//IL_03b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bc: Expected Obj, but got Unknown
			string text = "1.3.1";
			try
			{
				if (((MelonBase)this).Info != null && !string.IsNullOrEmpty(((MelonBase)this).Info.Version))
				{
					text = ((MelonBase)this).Info.Version;
				}
			}
			catch
			{
			}
			((MelonBase)this).LoggerInstance.Msg("[自定义容器升级] v" + text + " 已加载");
			_log = ((MelonBase)this).LoggerInstance;
			MelonPreferences_Category val = MelonPreferences.CreateCategory("自定义容器升级");
			_materialJunk = val.CreateEntry<bool>("0A_MaterialJunk", false, (string)null, "升级材料：使用垃圾", false, false, (ValueValidator)null, (string)null);
			_materialScrapMetal = val.CreateEntry<bool>("0B_MaterialScrapMetal", false, (string)null, "升级材料：使用废金属", false, false, (ValueValidator)null, (string)null);
			_materialMetalIngot = val.CreateEntry<bool>("0C_MaterialMetalIngot", true, (string)null, "升级材料：使用金属锭", false, false, (ValueValidator)null, (string)null);
			_upgradeKey = val.CreateEntry<string>("UpgradeHotkey", "LeftAlt", (string)null, "升级修饰键", false, false, (ValueValidator)null, (string)null);
			_enableDragResize = val.CreateEntry<bool>("2_EnableDragResize", true, (string)null, "是否启用拖拽调整窗口大小", false, false, (ValueValidator)null, (string)null);
			_cageHpPerCell = val.CreateEntry<int>("CageHpPerCell", 20, (string)null, "老鼠笼/动物笼 每格血量加成", false, false, (ValueValidator)null, (string)null);
			_diagLog = val.CreateEntry<bool>("DiagLog", false, (string)null, "输出诊断日志（排障用，默认关闭）", false, false, (ValueValidator)null, (string)null);
			_box = CreateGridDef(val, "Box", "箱子类容器", "sec_box,evidence_box,storage_bay,storage_bay_large,smuggler_bay,smuggler_bay_mod,machine_bay,machine_bay_ext,service_box,med_box,eng_box,storage_bay_chem,storage_bay_gun,smuggler_bay_mini,makeshift_storage_bay,expedition_box", 1, 50, 30, 1, null, "箱子类容器物品ID列表(逗号分隔)。");
			_backpack = CreateGridDef(val, "Backpack", "背包类容器", "backpack_small,backpack_medium,backpack_large,satchel,fanny_pack,backpack_medium_military,backpack_large_military", 1, 20, 20, 1, null, "背包类容器物品ID列表(逗号分隔)。");
			_cage = CreateGridDef(val, "Cage", "老鼠笼/动物笼", "animal_cage,Rat_Cage,rat_cage", 1, 20, 20, 1, null, "老鼠笼/动物笼物品ID列表(逗号分隔)。");
			_multiSlot = CreateGridDef(val, "MultiSlot", "多槽容器", "fridge,recharger,vacuum_robot", 4, 20, 20, 2, null, "多槽容器物品ID列表(逗号分隔)。");
			_multiSlotModule = CreateGridDef(val, "MultiSlotModule", "机器模组槽", "furnace,desequencer,security_alarm,mirage_projector,water_purifier,moisture_farm,wine_rack", 4, 10, 10, 3, new string[4] { "模组", "输入", "输出", "待定" }, "机器模组槽物品ID列表(逗号分隔)。");
			WageContainerRegistry.RegisterWageDefinitions(val);
			val.SaveToFile(true);
			RefreshIdLists();
			RefreshUpgradeKey();
			((MelonEventBase<LemonAction<string, string>>)(object)_box.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate
			{
				RefreshIdLists();
			}, 0, false);
			((MelonEventBase<LemonAction<string, string>>)(object)_backpack.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate
			{
				RefreshIdLists();
			}, 0, false);
			((MelonEventBase<LemonAction<string, string>>)(object)_cage.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate
			{
				RefreshIdLists();
			}, 0, false);
			((MelonEventBase<LemonAction<string, string>>)(object)_multiSlot.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate
			{
				RefreshIdLists();
			}, 0, false);
			((MelonEventBase<LemonAction<string, string>>)(object)_multiSlotModule.Ids.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate
			{
				RefreshIdLists();
			}, 0, false);
			((MelonEventBase<LemonAction<string, string>>)(object)_upgradeKey.OnEntryValueChanged).Subscribe((LemonAction<string, string>)delegate
			{
				RefreshUpgradeKey();
			}, 0, false);
			ApplyConfigLabels();
			Harmony val2 = new Harmony("DIYUpgrade");
			InstallPatches(val2);
			ReportPatchSummary(val2);
			SkipXiaowoBayRestore.Install(new Harmony("DIYUpgrade.XiaowoCompat"));
			SkipWageUpgradeRestore.Install(new Harmony("DIYUpgrade.WageCompat"));
			WindowShowHook.Install(new Harmony("DIYUpgrade.WindowShowHook"));
			_log.Msg("[自定义容器升级] 已加载。");
		}

		public override void OnSceneWasLoaded(int index, string sceneName)
		{
			_metaCache.Clear();
			_lastReapplyTime.Clear();
			_lastReapplyFromMetaTime.Clear();
			_ggiCache.Clear();
			_prepareCache.Clear();
			_resizableCache.Clear();
			_lastLogTimeByKey.Clear();
			_redrawPending.Clear();
			_offsetCheckFails = 0;
			_offsetChecked = false;
			_lastHideBounds.Clear();
			_peakCache.Clear();
			ContainerWindowResizer.InvalidateCursor();
			ContainerWindowResizer.ResetAllRestoreDone();
			WageContainerRegistry.ClearVoidBeadGrids();
			SaveSlotScope.Invalidate();
		}

		public override void OnUpdate()
		{
			ContainerWindowResizer.Tick();
			TryFlushPrefs();
		}

		private static void TryFlushPrefs()
		{
			if (!_prefsDirty)
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			if ((utcNow - _lastPrefsFlush).TotalSeconds < 2.0)
			{
				return;
			}
			_lastPrefsFlush = utcNow;
			_prefsDirty = false;
			try
			{
				PlayerPrefs.Save();
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1368", ex);
			}
		}

		public override void OnApplicationQuit()
		{
			try
			{
				if (_prefsDirty)
				{
					PlayerPrefs.Save();
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1373", ex);
			}
			ContainerWindowResizer.Dispose();
		}

		private static string GetUpgradeKeyLabel()
		{
			try
			{
				return (_upgradeKey != null && !string.IsNullOrEmpty(_upgradeKey.Value)) ? _upgradeKey.Value : "LeftAlt";
			}
			catch
			{
				return "LeftAlt";
			}
		}

		private static string GetMaterialLabel()
		{
			if (_materialMetalIngot != null && _materialMetalIngot.Value)
			{
				return "金属锭";
			}
			if (_materialScrapMetal != null && _materialScrapMetal.Value)
			{
				return "废金属";
			}
			if (_materialJunk != null && _materialJunk.Value)
			{
				return "垃圾";
			}
			return "材料";
		}

		private static void InstallPatches(Harmony harmony)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			try
			{
				Type[] array;
				try
				{
					array = typeof(DiyUpgradeMod).Assembly.GetTypes();
				}
				catch (ReflectionTypeLoadException ex)
				{
					array = Array.Empty<Type>();
					if (ex.Types != null)
					{
						int num4 = 0;
						for (int i = 0; i < ex.Types.Length; i++)
						{
							if (ex.Types[i] != null)
							{
								num4++;
							}
						}
						Type[] array2 = new Type[num4];
						int num5 = 0;
						for (int j = 0; j < ex.Types.Length; j++)
						{
							if (ex.Types[j] != null)
							{
								array2[num5++] = ex.Types[j];
							}
						}
						array = array2;
					}
				}
				foreach (Type type in array)
				{
					if (type == null)
					{
						continue;
					}
					try
					{
						if (type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length != 0)
						{
							if (OptionalPatchNames.Count > 0 && OptionalPatchNames.Contains(type.Name))
							{
								num2++;
								continue;
							}
							harmony.CreateClassProcessor(type).Patch();
							num++;
						}
					}
					catch (Exception ex2)
					{
						num3++;
						if (_log != null)
						{
							_log.Error("[容器升级] Patch 安装失败 " + (type?.Name ?? "?") + ": " + ex2.Message);
						}
					}
				}
			}
			catch (Exception ex3)
			{
				if (_log != null)
				{
					_log.Error("[容器升级] InstallPatches 异常: " + ex3.Message);
				}
			}
			if (_log != null)
			{
				_log.Msg($"[容器升级] Patch 安装: 成功 {num} 个" + ((num2 > 0) ? $", 跳过 {num2} 个(可选功能关闭)" : "") + ((num3 > 0) ? $", 失败 {num3} 个(详见上方错误)" : ""));
			}
		}

		private static void ReportPatchSummary(Harmony harmony)
		{
			try
			{
				int num = 0;
				foreach (MethodBase patchedMethod in harmony.GetPatchedMethods())
				{
					_ = patchedMethod;
					num++;
				}
				if (num < 15)
				{
					if (_log != null)
					{
						_log.Warning($"[自定义容器升级] Patch 汇总：{num}/{15}（可能因游戏更新有方法未匹配）");
					}
				}
				else if (_log != null)
				{
					_log.Msg($"[自定义容器升级] Patch 汇总：{num}/{15} ?");
				}
			}
			catch (Exception ex)
			{
				if (_log != null)
				{
					_log.Warning("[自定义容器升级] 汇总 patch 结果失败：" + ex.Message);
				}
			}
		}

		private static void MarkOffsetSuspect()
		{
			_offsetSuspectCount++;
			if (!_offsetWarningLogged && _offsetSuspectCount >= 5)
			{
				_offsetWarningLogged = true;
				if (_log != null)
				{
					_log.Error("[容器升级] 连续多次无法解析拖拽源对象，Off.* 内存偏移可能已失效（游戏更新？）。");
				}
			}
		}

		private static void VerifyDragOffset(GameItem source)
		{
			if (_offsetChecked)
			{
				return;
			}
			if (source == null)
			{
				_offsetCheckFails++;
				if (_offsetCheckFails >= 3)
				{
					_offsetChecked = true;
					MarkOffsetSuspect();
				}
				return;
			}
			try
			{
				string text = IdOf(source);
				if (!string.IsNullOrEmpty(text))
				{
					switch (text)
					{
					case "<null-id>":
					case "<err-id>":
					case "<empty-id>":
						break;
					default:
						_offsetChecked = true;
						return;
					}
				}
				_offsetCheckFails++;
				if (_offsetCheckFails >= 3)
				{
					_offsetChecked = true;
					MarkOffsetSuspect();
				}
			}
			catch
			{
			}
		}

		private static void LogOnce(string msg)
		{
			if (_log == null || string.IsNullOrEmpty(msg))
			{
				return;
			}
			DateTime utcNow = DateTime.UtcNow;
			if (!_lastLogTimeByKey.TryGetValue(msg, out var value) || !((utcNow - value).TotalSeconds < 3.0))
			{
				if (_lastLogTimeByKey.Count > 32)
				{
					_lastLogTimeByKey.Clear();
				}
				_lastLogTimeByKey[msg] = utcNow;
				_log.Msg(msg);
			}
		}

		internal static Type FindTypeCached(string typeName)
		{
			if (string.IsNullOrEmpty(typeName))
			{
				return null;
			}
			if (_typeByNameCache.TryGetValue(typeName, out var value))
			{
				return value;
			}
			Type type = null;
			try
			{
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				foreach (Assembly assembly in assemblies)
				{
					string text = null;
					try
					{
						text = assembly.GetName().Name;
					}
					catch
					{
						continue;
					}
					if (string.IsNullOrEmpty(text) || text.StartsWith("UnityEngine.") || text.StartsWith("Unity.") || text.StartsWith("System.") || text.StartsWith("Microsoft.") || text.StartsWith("Mono."))
					{
						continue;
					}
					switch (text)
					{
					case "mscorlib":
					case "netstandard":
					case "Il2Cppmscorlib":
					case "MelonLoader":
					case "0Harmony":
					case "Il2CppInterop.Runtime":
					case "Il2CppInterop.HarmonySupport":
					case "Il2CppInterop.Common":
						continue;
					}
					try
					{
						Type type2 = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
						if (type2 != null)
						{
							type = type2;
							break;
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.FindTypeCached", ex);
			}
			_typeByNameCache[typeName] = type;
			return type;
		}

		private static void ApplyConfigLabels()
		{
			try
			{
				SetLabel<bool>(_materialJunk, "升级材料：使用垃圾", "使用垃圾作为升级材料（优先级最低）");
				SetLabel<bool>(_materialScrapMetal, "升级材料：使用废金属", "使用废金属作为升级材料（优先级高于垃圾）");
				SetLabel<bool>(_materialMetalIngot, "升级材料：使用金属锭", "使用金属锭作为升级材料（优先级最高）");
				SetLabel<string>(_upgradeKey, "升级修饰键", "按住此键再拖材料到容器上才触发升级。默认 LeftAlt；支持组合键如 Ctrl+E");
				SetLabel<bool>(_enableDragResize, "启用拖拽调整大小", "是否允许通过拖拽窗口边缘调整容器大小。默认开启。");
				SetLabel<int>(_cageHpPerCell, "老鼠笼/动物笼：每格血量加成", "每升级一格增加的血量");
				SetLabel<bool>(_diagLog, "输出诊断日志", "排障用，默认关闭。打开后 Latest.log 会打印详细的判定/尺寸/渲染诊断。");
				ApplyGridLabels(_box, "箱子类容器");
				ApplyGridLabels(_backpack, "背包类容器");
				ApplyGridLabels(_cage, "老鼠笼/动物笼");
				ApplyGridLabels(_multiSlot, "多槽容器");
				ApplyGridLabels(_multiSlotModule, "机器模组槽");
				ApplyGridLabels(WageContainerRegistry._wageBox, "蛙哥妙妙箱");
				ApplyGridLabels(WageContainerRegistry._wageBackpack, "虚空珠储物袋");
				ApplyGridLabels(WageContainerRegistry._modContainer, "Mod容器");
			}
			catch (Exception ex)
			{
				if (_log != null)
				{
					_log.Error("[容器升级] 套用配置标签异常: " + ex.Message);
				}
			}
		}

		private static void ApplyGridLabels(GridDef d, string label)
		{
			if (d == null)
			{
				return;
			}
			bool isSingle = d.Slots.Length == 1;
			SetLabel<bool>(d.Enable, label + "：启用", "是否允许升级");
			SetLabel<string>(d.Ids, label + "：物品ID", "物品ID列表(逗号分隔)");
			SetLabel<int>(d.MaxW, label + "：最大宽度", "所有槽共享的最大列数");
			SetLabel<int>(d.MaxH, label + "：最大高度", "所有槽共享的最大行数");
			for (int i = 0; i < d.Slots.Length; i++)
			{
				TargetSlot targetSlot = d.Slots[i];
				string text = (isSingle ? "：" : (" · " + NameOf(i) + "："));
				if (targetSlot.Enable != null)
				{
					SetLabel<bool>(targetSlot.Enable, label + text + "启用", "是否升级此目标网格");
				}
				SetLabel<int>(targetSlot.IncW, label + text + "宽度增量", "每次升级增加的列数");
				SetLabel<int>(targetSlot.IncH, label + text + "高度增量", "每次升级增加的行数");
			}
			string NameOf(int num)
			{
				if (isSingle)
				{
					return "";
				}
				if (d.SlotNames == null || num >= d.SlotNames.Length || string.IsNullOrEmpty(d.SlotNames[num]))
				{
					return "槽" + num;
				}
				return d.SlotNames[num];
			}
		}

		private static void SetLabel<T>(MelonPreferences_Entry<T> entry, string displayName, string description)
		{
			if (entry == null)
			{
				return;
			}
			try
			{
				((MelonPreferences_Entry)entry).DisplayName = displayName;
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1481", ex);
			}
			try
			{
				((MelonPreferences_Entry)entry).Description = description;
			}
			catch (Exception ex2)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1482", ex2);
			}
		}

		internal static GridDef CreateGridDef(MelonPreferences_Category cat, string prefix, string label, string defaultIds, int slotCount, int defaultMaxW = 20, int defaultMaxH = 20, int defaultEnabledSlots = 1, string[] slotNames = null, string idsComment = null)
		{
			GridDef gridDef = new GridDef
			{
				Enable = cat.CreateEntry<bool>(prefix + "Enable", true, (string)null, "是否允许升级" + label, false, false, (ValueValidator)null, (string)null),
				Ids = cat.CreateEntry<string>(prefix + "Ids", defaultIds, (string)null, idsComment ?? (label + "物品ID列表(逗号分隔)"), false, false, (ValueValidator)null, (string)null),
				MaxW = cat.CreateEntry<int>(prefix + "MaxW", defaultMaxW, (string)null, label + "：所有槽共享的最大宽度", false, false, (ValueValidator)null, (string)null),
				MaxH = cat.CreateEntry<int>(prefix + "MaxH", defaultMaxH, (string)null, label + "：所有槽共享的最大高度", false, false, (ValueValidator)null, (string)null),
				SlotNames = slotNames
			};
			if (slotCount > 1)
			{
				gridDef.AlignSlots = cat.CreateEntry<bool>(prefix + "AlignSlots", true, (string)null, label + "：所有槽位尺寸跟随第一个槽（关=各槽独立）", false, false, (ValueValidator)null, (string)null);
			}
			TargetSlot[] array = new TargetSlot[slotCount];
			for (int i = 0; i < slotCount; i++)
			{
				MelonPreferences_Entry<bool> enable = null;
				if (slotCount > 1)
				{
					string text = ((slotNames != null && i < slotNames.Length && !string.IsNullOrEmpty(slotNames[i])) ? slotNames[i] : ("槽" + i));
					enable = cat.CreateEntry<bool>($"{prefix}T{i}Enable", i < defaultEnabledSlots, (string)null, label + "：" + text + " 是否启用", false, false, (ValueValidator)null, (string)null);
				}
				array[i] = new TargetSlot
				{
					Enable = enable,
					IncW = cat.CreateEntry<int>($"{prefix}T{i}IncW", 1, (string)null, $"{label}：槽{i} 每次升级宽度增量", false, false, (ValueValidator)null, (string)null),
					IncH = cat.CreateEntry<int>($"{prefix}T{i}IncH", 1, (string)null, $"{label}：槽{i} 每次升级高度增量", false, false, (ValueValidator)null, (string)null)
				};
			}
			gridDef.Slots = array;
			return gridDef;
		}

		private static void RefreshIdLists()
		{
			_box.IdList = SplitIds(_box.Ids.Value);
			_backpack.IdList = SplitIds(_backpack.Ids.Value);
			_cage.IdList = SplitIds(_cage.Ids.Value);
			_multiSlot.IdList = SplitIds(_multiSlot.Ids.Value);
			_multiSlotModule.IdList = SplitIds(_multiSlotModule.Ids.Value);
			try
			{
				_resizableCache.Clear();
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1522", ex);
			}
		}

		private static string[] SplitIds(string raw)
		{
			if (string.IsNullOrEmpty(raw))
			{
				return Array.Empty<string>();
			}
			string[] array = raw.Split(new char[4] { ',', '，', ';', '；' }, StringSplitOptions.RemoveEmptyEntries);
			string[] array2 = new string[array.Length];
			for (int i = 0; i < array.Length; i++)
			{
				array2[i] = array[i].Trim();
			}
			return array2;
		}

		private static void RefreshUpgradeKey()
		{
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0107: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			_upgradeModCtrl = false;
			_upgradeModAlt = false;
			_upgradeModShift = false;
			string text = ((_upgradeKey == null || _upgradeKey.Value == null) ? "" : _upgradeKey.Value.Trim());
			if (text.Length == 0 || text.Equals("None", StringComparison.OrdinalIgnoreCase))
			{
				_upgradeKeyCode = (KeyCode)0;
				return;
			}
			string[] array = text.Split('+');
			KeyCode val = (KeyCode)0;
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = array[i].Trim();
				if (text2.Length != 0)
				{
					KeyCode result;
					if (text2.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || text2.Equals("Control", StringComparison.OrdinalIgnoreCase))
					{
						_upgradeModCtrl = true;
					}
					else if (text2.Equals("Alt", StringComparison.OrdinalIgnoreCase))
					{
						_upgradeModAlt = true;
					}
					else if (text2.Equals("Shift", StringComparison.OrdinalIgnoreCase))
					{
						_upgradeModShift = true;
					}
					else if (text2 == "`" || text2 == "~")
					{
						val = (KeyCode)96;
					}
					else if (Enum.TryParse<KeyCode>(text2, true, out result) && (int)result != 0)
					{
						val = result;
					}
				}
			}
			if ((int)val == 0)
			{
				if (_log != null)
				{
					_log.Warning("[容器升级] 无法识别的升级键名 '" + text + "', 回退为 LeftAlt。");
				}
				_upgradeKeyCode = (KeyCode)308;
			}
			else
			{
				_upgradeKeyCode = val;
			}
		}

		private static bool IsUpgradeKeyHeld()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			if ((int)_upgradeKeyCode == 0)
			{
				return false;
			}
			try
			{
				if (!Input.GetKey(_upgradeKeyCode))
				{
					return false;
				}
				if (_upgradeModCtrl && !Input.GetKey((KeyCode)306) && !Input.GetKey((KeyCode)305))
				{
					return false;
				}
				if (_upgradeModAlt && !Input.GetKey((KeyCode)308) && !Input.GetKey((KeyCode)307))
				{
					return false;
				}
				if (_upgradeModShift && !Input.GetKey((KeyCode)304) && !Input.GetKey((KeyCode)303))
				{
					return false;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool PtrFieldNull(GameItem item, int offset)
		{
			if (item == null)
			{
				return true;
			}
			try
			{
				return Marshal.ReadIntPtr(((Il2CppObjectBase)item).Pointer, offset) == IntPtr.Zero;
			}
			catch
			{
				return true;
			}
		}

		private static bool IdFieldNull(GameItem item)
		{
			return PtrFieldNull(item, 448);
		}

		private static bool TagSystemFieldNull(GameItem item)
		{
			return PtrFieldNull(item, 440);
		}

		private static bool IsAnyId(GameItem item, string[] ids)
		{
			if (item == null || IdFieldNull(item))
			{
				return false;
			}
			string text = IdOf(item);
			if (string.IsNullOrEmpty(text))
			{
				return false;
			}
			foreach (string text2 in ids)
			{
				if (string.IsNullOrEmpty(text2))
				{
					continue;
				}
				if (text2.IndexOf('*') >= 0 || text2.IndexOf('?') >= 0)
				{
					if (WildcardMatch(text2, text))
					{
						return true;
					}
				}
				else if (item.IsID(text2))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool WildcardMatch(string pattern, string input)
		{
			if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(input))
			{
				return false;
			}
			int i = 0;
			int num = 0;
			int num2 = -1;
			int num3 = 0;
			while (num < input.Length)
			{
				if (i < pattern.Length && (pattern[i] == input[num] || pattern[i] == '?'))
				{
					i++;
					num++;
					continue;
				}
				if (i < pattern.Length && pattern[i] == '*')
				{
					num2 = i++;
					num3 = num;
					continue;
				}
				if (num2 != -1)
				{
					i = num2 + 1;
					num = ++num3;
					continue;
				}
				return false;
			}
			for (; i < pattern.Length && pattern[i] == '*'; i++)
			{
			}
			return i == pattern.Length;
		}

		internal static string IdOf(GameItem item)
		{
			try
			{
				if (item == null || IdFieldNull(item))
				{
					return "<null-id>";
				}
				string text = item.identifier?.Replace("\0", "").Trim();
				return string.IsNullOrEmpty(text) ? "<empty-id>" : text;
			}
			catch
			{
				return "<err-id>";
			}
		}

		private static GameItem InventoryOwner(IntPtr invPtr)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			if (invPtr == IntPtr.Zero)
			{
				return null;
			}
			try
			{
				return ((GameInventory)new GameGridInventory(invPtr)).GetParentItem();
			}
			catch
			{
				return null;
			}
		}

		private static bool IsAllowedMaterial(GameItem source)
		{
			if (source == null)
			{
				return false;
			}
			try
			{
				if (_materialMetalIngot != null && _materialMetalIngot.Value && source.IsID("metal_ingot"))
				{
					return true;
				}
				if (_materialScrapMetal != null && _materialScrapMetal.Value && source.IsID("scrap_metal"))
				{
					return true;
				}
				if (_materialJunk != null && _materialJunk.Value && source.IsID("junk"))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1652", ex);
			}
			return false;
		}

		private static Cat MatchCategory(GameItem source, GameItem target)
		{
			if (source == null || target == null)
			{
				return Cat.None;
			}
			if (IdFieldNull(source) || IdFieldNull(target))
			{
				return Cat.None;
			}
			if (!IsAllowedMaterial(source))
			{
				return Cat.None;
			}
			if (IsAnyId(source, _box.IdList) || IsAnyId(source, _backpack.IdList) || IsAnyId(source, _cage.IdList) || IsAnyId(source, _multiSlot.IdList) || IsAnyId(source, _multiSlotModule.IdList))
			{
				return Cat.None;
			}
			Cat wageCategory = WageContainerRegistry.GetWageCategory(target);
			if (wageCategory != Cat.None)
			{
				return wageCategory;
			}
			if (_multiSlotModule.Enable.Value && IsAnyId(target, _multiSlotModule.IdList))
			{
				return Cat.MultiSlotModule;
			}
			if (_multiSlot.Enable.Value && IsAnyId(target, _multiSlot.IdList))
			{
				return Cat.MultiSlot;
			}
			if (_box.Enable.Value && IsAnyId(target, _box.IdList))
			{
				return Cat.Box;
			}
			if (_backpack.Enable.Value && IsAnyId(target, _backpack.IdList))
			{
				return Cat.Backpack;
			}
			if (_cage.Enable.Value && IsAnyId(target, _cage.IdList))
			{
				return Cat.Cage;
			}
			return Cat.None;
		}

		private static bool TryResolve(GameItem a, GameItem b, out GameItem material, out GameItem container, out Cat cat)
		{
			cat = MatchCategory(a, b);
			if (cat != Cat.None)
			{
				material = a;
				container = b;
				return true;
			}
			cat = MatchCategory(b, a);
			if (cat != Cat.None)
			{
				material = b;
				container = a;
				return true;
			}
			material = null;
			container = null;
			return false;
		}

		private static bool PrepareUpgrade(GameItem container, Cat cat, out Action apply, out string reason)
		{
			apply = null;
			reason = null;
			if (container == null)
			{
				return false;
			}
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = ((Il2CppObjectBase)container).Pointer;
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1687", ex);
			}
			if (intPtr != IntPtr.Zero && _prepareCache.TryGetValue(intPtr, out var value) && (DateTime.UtcNow - value.Time).TotalSeconds < 0.2)
			{
				reason = value.Reason;
				return value.Can;
			}
			bool flag = PrepareUpgradeInternal(container, cat, out apply, out reason);
			if (intPtr != IntPtr.Zero)
			{
				_prepareCache[intPtr] = new PrepareCacheEntry
				{
					Time = DateTime.UtcNow,
					Can = flag,
					Reason = reason
				};
			}
			return flag;
		}

		private static bool PrepareUpgradeInternal(GameItem container, Cat cat, out Action apply, out string reason)
		{
			apply = null;
			reason = null;
			switch (cat)
			{
			case Cat.Box:
				return PrepareGrid(container, _box, cat, out apply, out reason);
			case Cat.Backpack:
				return PrepareGrid(container, _backpack, cat, out apply, out reason);
			case Cat.Cage:
				return PrepareGrid(container, _cage, cat, out apply, out reason);
			case Cat.MultiSlot:
				return PrepareGrid(container, _multiSlot, cat, out apply, out reason);
			case Cat.MultiSlotModule:
				return PrepareGrid(container, _multiSlotModule, cat, out apply, out reason);
			case Cat.ModContainer:
				return PrepareGrid(container, WageContainerRegistry._modContainer, cat, out apply, out reason);
			case Cat.WageBox:
				return PrepareGrid(container, WageContainerRegistry._wageBox, cat, out apply, out reason);
			case Cat.WageBackpack:
				return PrepareGrid(container, WageContainerRegistry._wageBackpack, cat, out apply, out reason);
			default:
				reason = "未知分类";
				return false;
			}
		}

		private static bool PrepareGrid(GameItem container, GridDef d, Cat cat, out Action apply, out string reason)
		{
			apply = null;
			reason = null;
			List<GameGridInventory> allGridInventories = GetAllGridInventories(container);
			if (allGridInventories.Count == 0)
			{
				reason = "找不到网格仓库";
				return false;
			}
			GameGridInventory val = allGridInventories[0];
			if (((val != null) ? val.inventoryShape : null) != null)
			{
				if (WageContainerRegistry.IsWageBox(container) && !WageContainerRegistry.HasDiyMigrated(container))
				{
					WageContainerRegistry.MigrateLegacyWageBox(container, allGridInventories[0]);
				}
				else if (WageContainerRegistry.IsVoidBeadStorage(container) && !WageContainerRegistry.HasDiyMigrated(container))
				{
					WageContainerRegistry.MigrateLegacyVoidBead(container, allGridInventories[0]);
				}
			}
			if (TagSystemFieldNull(container))
			{
				reason = "容器标签系统未就绪";
				return false;
			}
			int num = -1;
			foreach (var item2 in GetEnabledSlotsWithIndex(d))
			{
				if (item2.Index >= 0 && item2.Index < allGridInventories.Count)
				{
					(num, _) = item2;
					break;
				}
			}
			if (num < 0)
			{
				reason = "目标网格索引超出实际网格数量";
				return false;
			}
			GameGridInventory val2 = allGridInventories[num];
			if (((val2 != null) ? val2.inventoryShape : null) == null)
			{
				reason = "读取当前网格形状失败";
				return false;
			}
			(int, int, int) tuple2 = LoadMeta(container);
			int baseW = tuple2.Item1;
			int baseH = tuple2.Item2;
			int spent = tuple2.Item3;
			int width = val2.inventoryShape.width;
			int height = val2.inventoryShape.height;
			if (baseW <= 0 || baseH <= 0)
			{
				baseW = width;
				baseH = height;
				SaveMeta(container, baseW, baseH, spent);
			}
			List<(int Index, int NewW, int NewH)> plan = new List<(int, int, int)>();
			int width2 = val2.inventoryShape.width;
			int height2 = val2.inventoryShape.height;
			int val3 = Math.Max(1, d.MaxW.Value);
			int val4 = Math.Max(1, d.MaxH.Value);
			bool flag = d.AlignSlots?.Value ?? false;
			int num2 = 0;
			int num3 = 0;
			if (flag)
			{
				foreach (var item3 in GetEnabledSlotsWithIndex(d))
				{
					if (item3.Index < 0 || item3.Index >= allGridInventories.Count)
					{
						continue;
					}
					GameGridInventory val5 = allGridInventories[item3.Index];
					if (((val5 != null) ? val5.inventoryShape : null) != null)
					{
						if (val5.inventoryShape.width > num2)
						{
							num2 = val5.inventoryShape.width;
						}
						if (val5.inventoryShape.height > num3)
						{
							num3 = val5.inventoryShape.height;
						}
					}
				}
			}
			foreach (var (num4, targetSlot) in GetEnabledSlotsWithIndex(d))
			{
				if (num4 < 0 || num4 >= allGridInventories.Count)
				{
					continue;
				}
				GameGridInventory val6 = allGridInventories[num4];
				if (((val6 != null) ? val6.inventoryShape : null) != null)
				{
					int width3 = val6.inventoryShape.width;
					int height3 = val6.inventoryShape.height;
					int num5 = Math.Max(0, targetSlot.IncW.Value);
					int num6 = Math.Max(0, targetSlot.IncH.Value);
					int num7;
					int num8;
					if (flag)
					{
						num7 = ((num5 > 0) ? Math.Min(val3, num2 + num5) : num2);
						num8 = ((num6 > 0) ? Math.Min(val4, num3 + num6) : num3);
					}
					else
					{
						num7 = ((num5 > 0) ? Math.Min(val3, width3 + num5) : width3);
						num8 = ((num6 > 0) ? Math.Min(val4, height3 + num6) : height3);
					}
					if (num7 != width3 || num8 != height3)
					{
						plan.Add((num4, num7, num8));
					}
				}
			}
			if (plan.Count == 0)
			{
				reason = "已达最大尺寸或尺寸无变化";
				return false;
			}
			Cat catLocal = cat;
			GameItem containerLocal = container;
			int oldW = width2;
			int oldH = height2;
			int firstNewW = plan[0].NewW;
			int firstNewH = plan[0].NewH;
			apply = () =>
			{
				_isUpgrading = true;
				try
				{
					if (!ApplyUpgradePlan(containerLocal, plan, out var actualWidth, out var actualHeight))
					{
						if (_log != null)
						{
							_log.Warning("[容器升级] 尺寸重排失败: " + IdOf(containerLocal));
						}
						return;
					}
					SaveMeta(containerLocal, baseW, baseH, spent + 1);
					RememberWindowSize(containerLocal, actualWidth, actualHeight);
					_prefsDirty = true;
					List<GameGridInventory> allGridInventories2 = GetAllGridInventories(containerLocal);
					int num9 = SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_W_TAG", 0);
					int num10 = SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_H_TAG", 0);
					foreach (var item4 in GetEnabledSlotsWithIndex(d))
					{
						int item = item4.Index;
						if (item >= 0 && item < allGridInventories2.Count)
						{
							GameGridInventory val7 = allGridInventories2[item];
							if (((val7 != null) ? val7.inventoryShape : null) != null)
							{
								RememberSlotShape(containerLocal, item, val7.inventoryShape.width, val7.inventoryShape.height);
								if (val7.inventoryShape.width > num9)
								{
									num9 = val7.inventoryShape.width;
								}
								if (val7.inventoryShape.height > num10)
								{
									num10 = val7.inventoryShape.height;
								}
							}
						}
					}
					if (d.AlignSlots != null && num9 > 0 && num10 > 0)
					{
						int num11 = SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_W_TAG", 0);
						int num12 = SafeTagInt(containerLocal, "DIY_UPGRADE_PEAK_H_TAG", 0);
						if (num9 != num11 || num10 != num12)
						{
							SetItemTag(containerLocal, "DIY_UPGRADE_PEAK_W_TAG", num9);
							SetItemTag(containerLocal, "DIY_UPGRADE_PEAK_H_TAG", num10);
						}
					}
					if (allGridInventories2.Count > 0)
					{
						GameGridInventory val8 = allGridInventories2[0];
						if (((val8 != null) ? val8.inventoryShape : null) != null)
						{
							RememberGridShape(containerLocal, allGridInventories2[0].inventoryShape.width, allGridInventories2[0].inventoryShape.height);
							SyncXiaowoBayTags(containerLocal, allGridInventories2[0]);
							if (WageContainerRegistry.IsVoidBeadStorage(containerLocal))
							{
								WageContainerRegistry.OnVoidBeadUpgraded(containerLocal, allGridInventories2[0], allGridInventories2[0].inventoryShape.width, allGridInventories2[0].inventoryShape.height);
								SkipWageUpgradeRestore.ClearWageRestoreState();
							}
							try
							{
								containerLocal.ValidateShapeState();
							}
							catch (Exception ex)
							{
								if (_log != null)
								{
									_log.Warning("[容器升级] ValidateShapeState 失败: " + ex.Message);
								}
							}
						}
					}
					if (catLocal == Cat.Cage)
					{
						int num13 = Math.Max(0, _cageHpPerCell.Value);
						int num14 = firstNewW - oldW + (firstNewH - oldH);
						if (num14 <= 0)
						{
							num14 = 1;
						}
						int num15 = num14 * num13;
						int num16 = SafeTagInt(containerLocal, "CAGE_MAX_HEALTH_TAG", 100);
						int num17 = SafeTagInt(containerLocal, "CAGE_HEALTH_TAG", num16);
						SetItemTag(containerLocal, "CAGE_MAX_HEALTH_TAG", num16 + num15);
						SetItemTag(containerLocal, "CAGE_HEALTH_TAG", Math.Min(num17 + num15, num16 + num15));
						GameItem val9 = containerLocal;
						val9.unitValue += (long)num15 * 2L;
					}
					PlayUpgradeSound();
					try
					{
						PixelWindow contentWindow = containerLocal.contentWindow;
						GameGridInventory inventory = ((allGridInventories2.Count > 0) ? allGridInventories2[0] : null);
						MelonCoroutines.Start(ForceRebuildAfterUpgrade(contentWindow, inventory));
					}
					catch (Exception ex2)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1851", ex2);
					}
					ContainerWindowResizer.Attach(containerLocal);
					try
					{
						_prepareCache.Clear();
						_ggiCache.Clear();
					}
					catch (Exception ex3)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:1853", ex3);
					}
				}
				catch (Exception ex4)
				{
					if (_log != null)
					{
						_log.Error("[容器升级] 升级应用异常: " + ex4.Message);
					}
				}
				finally
				{
					_isUpgrading = false;
				}
			};
			return true;
		}

		private static bool IsHorizontalLayout(List<GameGridInventory> grids, int[] slotIndices)
		{
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			if (grids == null || slotIndices == null || slotIndices.Length < 2)
			{
				return true;
			}
			try
			{
				int num = slotIndices[0];
				int num2 = slotIndices[1];
				if (num < 0 || num >= grids.Count)
				{
					return true;
				}
				if (num2 < 0 || num2 >= grids.Count)
				{
					return true;
				}
				Component val = ((Il2CppObjectBase)grids[num]).TryCast<Component>();
				Component val2 = ((Il2CppObjectBase)grids[num2]).TryCast<Component>();
				if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
				{
					return true;
				}
				Transform transform = val.transform;
				Transform transform2 = val2.transform;
				if ((Object)(object)transform == (Object)null || (Object)(object)transform2 == (Object)null)
				{
					return true;
				}
				RectTransform val3 = ((Il2CppObjectBase)transform).TryCast<RectTransform>();
				RectTransform val4 = ((Il2CppObjectBase)transform2).TryCast<RectTransform>();
				float value;
				float value2;
				if ((Object)(object)val3 != (Object)null && (Object)(object)val4 != (Object)null)
				{
					Vector2 anchoredPosition = val3.anchoredPosition;
					Vector2 anchoredPosition2 = val4.anchoredPosition;
					value = anchoredPosition2.x - anchoredPosition.x;
					value2 = anchoredPosition2.y - anchoredPosition.y;
				}
				else
				{
					Vector3 position = transform.position;
					Vector3 position2 = transform2.position;
					value = position2.x - position.x;
					value2 = position2.y - position.y;
				}
				return Math.Abs(value) >= Math.Abs(value2);
			}
			catch
			{
				return true;
			}
		}

		private static void ComputeMultiSlotWindowSize(int baseWinW, int baseWinH, List<(int dW, int dH)> slotDeltas, bool horizontal, out int newWinW, out int newWinH)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			if (slotDeltas != null)
			{
				for (int i = 0; i < slotDeltas.Count; i++)
				{
					num += slotDeltas[i].dW;
					num2 += slotDeltas[i].dH;
					if (slotDeltas[i].dW > num3)
					{
						num3 = slotDeltas[i].dW;
					}
					if (slotDeltas[i].dH > num4)
					{
						num4 = slotDeltas[i].dH;
					}
				}
			}
			if (horizontal)
			{
				newWinW = baseWinW + num;
				newWinH = baseWinH + num4;
			}
			else
			{
				newWinW = baseWinW + num3;
				newWinH = baseWinH + num2;
			}
		}

		private static bool ApplyUpgradePlan(GameItem item, List<(int Index, int NewW, int NewH)> plan, out int actualWidth, out int actualHeight)
		{
			actualWidth = 0;
			actualHeight = 0;
			List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
			if (allGridInventories.Count == 0)
			{
				return false;
			}
			PixelWindow contentWindow = item.contentWindow;
			int baseWinW = ((contentWindow != null) ? contentWindow.widthPixels : 0);
			int baseWinH = ((contentWindow != null) ? contentWindow.heightPixels : 0);
			int num = -1;
			foreach (var item2 in plan)
			{
				if (item2.Index >= 0 && item2.Index < allGridInventories.Count)
				{
					GameGridInventory val = allGridInventories[item2.Index];
					if (((val != null) ? val.inventoryShape : null) != null && !CanContainItems(val, item2.NewW, item2.NewH))
					{
						return false;
					}
				}
			}
			List<(int, int)> list = new List<(int, int)>();
			foreach (var item3 in plan)
			{
				if (item3.Index < 0 || item3.Index >= allGridInventories.Count)
				{
					continue;
				}
				GameGridInventory val2 = allGridInventories[item3.Index];
				if (((val2 != null) ? val2.inventoryShape : null) != null)
				{
					int widthPixels = val2.widthPixels;
					int heightPixels = val2.heightPixels;
					SetShapeKeepTransparency(val2, item3.NewW, item3.NewH);
					val2.ForceRecomputeDynamicBackground();
					list.Add((val2.widthPixels - widthPixels, val2.heightPixels - heightPixels));
					if (num < 0)
					{
						(num, _, _) = item3;
					}
				}
			}
			if (contentWindow != null && num >= 0)
			{
				int[] array = new int[plan.Count];
				for (int i = 0; i < plan.Count; i++)
				{
					array[i] = plan[i].Index;
				}
				bool horizontal = IsHorizontalLayout(allGridInventories, array);
				ComputeMultiSlotWindowSize(baseWinW, baseWinH, list, horizontal, out var newWinW, out var newWinH);
				GameGridInventory val3 = allGridInventories[num];
				int borderW = Math.Max(0, newWinW - val3.widthPixels);
				int borderH = Math.Max(0, newWinH - val3.heightPixels);
				ApplyWindowSize(contentWindow, val3, newWinW, newWinH, borderW, borderH);
				try
				{
					contentWindow.FixBounds();
				}
				catch
				{
				}
				actualWidth = newWinW;
				actualHeight = newWinH;
			}
			return true;
		}

		internal static bool HasUpgraded(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				return LoadMeta(item).Spent > 0;
			}
			catch
			{
				return false;
			}
		}

		internal static bool IsResizableContainer(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = ((Il2CppObjectBase)item).Pointer;
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:1916", ex);
			}
			if (intPtr != IntPtr.Zero && _resizableCache.TryGetValue(intPtr, out (DateTime, bool) value) && (DateTime.UtcNow - value.Item1).TotalSeconds < 0.3)
			{
				return value.Item2;
			}
			bool flag = (_multiSlotModule.Enable.Value && IsAnyId(item, _multiSlotModule.IdList)) || (_multiSlot.Enable.Value && IsAnyId(item, _multiSlot.IdList)) || (_box.Enable.Value && IsAnyId(item, _box.IdList)) || (_backpack.Enable.Value && IsAnyId(item, _backpack.IdList)) || (_cage.Enable.Value && IsAnyId(item, _cage.IdList));
			if (!flag && WageContainerRegistry.GetWageCategory(item) != Cat.None)
			{
				flag = true;
			}
			if (intPtr != IntPtr.Zero)
			{
				if (_resizableCache.Count > 256)
				{
					_resizableCache.Clear();
				}
				_resizableCache[intPtr] = (DateTime.UtcNow, flag);
			}
			return flag;
		}

		internal static GameGridInventory GetResizableInventory(GameItem item)
		{
			if (item == null)
			{
				return null;
			}
			GridDef gridDefForItem = GetGridDefForItem(item);
			if (gridDefForItem == null)
			{
				return null;
			}
			int num = -1;
			using (IEnumerator<(int, TargetSlot)> enumerator = GetEnabledSlotsWithIndex(gridDefForItem).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					num = enumerator.Current.Item1;
				}
			}
			if (num <= 0)
			{
				try
				{
					PixelWindow contentWindow = item.contentWindow;
					if (contentWindow != null)
					{
						PixelElement childElement = contentWindow.childElement;
						GameGridInventory val = ((childElement != null) ? ((Il2CppObjectBase)childElement).TryCast<GameGridInventory>() : null);
						if (((val != null) ? val.inventoryShape : null) != null)
						{
							return val;
						}
					}
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:1951", ex);
				}
			}
			List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
			if (allGridInventories.Count == 0)
			{
				return null;
			}
			foreach (var item3 in GetEnabledSlotsWithIndex(gridDefForItem))
			{
				int item2 = item3.Index;
				if (item2 >= 0 && item2 < allGridInventories.Count)
				{
					return allGridInventories[item2];
				}
			}
			return allGridInventories[0];
		}

		internal static bool TryGetGridShapeLimits(GameItem item, out int minC, out int minR, out int maxC, out int maxR)
		{
			minC = 1;
			minR = 1;
			(int BaseW, int BaseH, int Spent) tuple = LoadMeta(item);
			int item2 = tuple.BaseW;
			int item3 = tuple.BaseH;
			int num = tuple.Spent;
			int num2 = 20;
			int num3 = 20;
			GridDef gridDefForItem = GetGridDefForItem(item);
			if (gridDefForItem != null)
			{
				if (gridDefForItem.MaxW.Value > 0)
				{
					num2 = gridDefForItem.MaxW.Value;
				}
				if (gridDefForItem.MaxH.Value > 0)
				{
					num3 = gridDefForItem.MaxH.Value;
				}
			}
			if (num2 <= 0)
			{
				num2 = 100;
			}
			if (num3 <= 0)
			{
				num3 = 100;
			}
			if (num < 0)
			{
				num = 0;
			}
			int num4 = 0;
			int num5 = 0;
			bool flag = gridDefForItem != null && gridDefForItem.AlignSlots != null;
			if (flag)
			{
				num4 = SafeTagInt(item, "DIY_UPGRADE_PEAK_W_TAG", 0);
				num5 = SafeTagInt(item, "DIY_UPGRADE_PEAK_H_TAG", 0);
				IntPtr intPtr = IntPtr.Zero;
				try
				{
					intPtr = ((Il2CppObjectBase)item).Pointer;
				}
				catch
				{
				}
				if (intPtr != IntPtr.Zero && _peakCache.TryGetValue(intPtr, out (int, int) value))
				{
					if (num4 <= 0 && value.Item1 > 0)
					{
						(num4, _) = value;
					}
					if (num5 <= 0 && value.Item2 > 0)
					{
						num5 = value.Item2;
					}
				}
				List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
				int num6 = -1;
				foreach (var item5 in GetEnabledSlotsWithIndex(gridDefForItem))
				{
					if (item5.Index >= 0 && item5.Index < allGridInventories.Count)
					{
						(num6, _) = item5;
						break;
					}
				}
				if (num4 <= 0 || num5 <= 0)
				{
					try
					{
						int num7 = SafeTagInt(item, "DIY_UPGRADE_W_TAG", 0);
						int num8 = SafeTagInt(item, "DIY_UPGRADE_H_TAG", 0);
						if (num7 > 0 && num8 > 0)
						{
							SetItemTag(item, "DIY_UPGRADE_PEAK_W_TAG", num7);
							SetItemTag(item, "DIY_UPGRADE_PEAK_H_TAG", num8);
							num4 = num7;
							num5 = num8;
							RememberPeakCache(item, num7, num8);
						}
						else if (num6 >= 0)
						{
							GameGridInventory val = allGridInventories[num6];
							if (((val != null) ? val.inventoryShape : null) != null)
							{
								int width = val.inventoryShape.width;
								int height = val.inventoryShape.height;
								if (width > 0 && height > 0)
								{
									SetItemTag(item, "DIY_UPGRADE_PEAK_W_TAG", width);
									SetItemTag(item, "DIY_UPGRADE_PEAK_H_TAG", height);
									num4 = width;
									num5 = height;
									RememberPeakCache(item, width, height);
								}
							}
						}
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:peak-probe", ex);
					}
				}
			}
			int num9 = num2;
			int num10 = num3;
			int num11 = 1;
			int num12 = 1;
			if (gridDefForItem != null && item2 > 0 && item3 > 0)
			{
				TargetSlot primarySlot = GetPrimarySlot(gridDefForItem);
				int num13 = ((primarySlot != null) ? Math.Max(0, primarySlot.IncW.Value) : 0);
				int num14 = ((primarySlot != null) ? Math.Max(0, primarySlot.IncH.Value) : 0);
				num9 = Math.Min(num2, item2 + num * num13);
				num10 = Math.Min(num3, item3 + num * num14);
				num11 = Math.Max(1, item2);
				num12 = Math.Max(1, item3);
			}
			else
			{
				GameGridInventory resizableInventory = GetResizableInventory(item);
				if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) != null && resizableInventory.inventoryShape.width > 0 && resizableInventory.inventoryShape.height > 0)
				{
					num9 = resizableInventory.inventoryShape.width;
					num10 = resizableInventory.inventoryShape.height;
					num11 = resizableInventory.inventoryShape.width;
					num12 = resizableInventory.inventoryShape.height;
				}
				else
				{
					num9 = 1;
					num10 = 1;
					num11 = 1;
					num12 = 1;
				}
			}
			if (flag)
			{
				if (num4 > 0 && num4 > num9)
				{
					num9 = num4;
				}
				if (num5 > 0 && num5 > num10)
				{
					num10 = num5;
				}
			}
			else
			{
				List<GameGridInventory> allGridInventories2 = GetAllGridInventories(item);
				GridDef gridDefForItem2 = GetGridDefForItem(item);
				if (gridDefForItem2 != null)
				{
					foreach (var item6 in GetEnabledSlotsWithIndex(gridDefForItem2))
					{
						int item4 = item6.Index;
						if (item4 < 0 || item4 >= allGridInventories2.Count)
						{
							continue;
						}
						GameGridInventory val2 = allGridInventories2[item4];
						if (((val2 != null) ? val2.inventoryShape : null) != null)
						{
							if (val2.inventoryShape.width > num9)
							{
								num9 = val2.inventoryShape.width;
							}
							if (val2.inventoryShape.height > num10)
							{
								num10 = val2.inventoryShape.height;
							}
						}
					}
				}
			}
			minC = num11;
			minR = num12;
			maxC = num9;
			maxR = num10;
			if (maxC < minC)
			{
				maxC = minC;
			}
			if (maxR < minR)
			{
				maxR = minR;
			}
			return true;
		}

		internal static bool TryResizeGridWindow(GameItem item, int reqC, int reqR, bool remember, out int actualWidth, out int actualHeight)
		{
			actualWidth = 0;
			actualHeight = 0;
			if (GetGridDefForItem(item)?.AlignSlots?.Value == true && LoadMeta(item).Spent <= 0)
			{
				LogOnce("[容器升级][拖拽拒绝] " + IdOf(item) + " alignSlots=true 且 spent=0，未升级不允许拖拽");
				return false;
			}
			if (!TryGetGridShapeLimits(item, out var minC, out var minR, out var maxC, out var maxR))
			{
				return false;
			}
			List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
			if (allGridInventories.Count == 0)
			{
				return false;
			}
			GridDef gridDefForItem = GetGridDefForItem(item);
			int num = -1;
			using (IEnumerator<(int, TargetSlot)> enumerator = GetEnabledSlotsWithIndex(gridDefForItem).GetEnumerator())
			{
				if (enumerator.MoveNext())
				{
					num = enumerator.Current.Item1;
				}
			}
			bool valueOrDefault = gridDefForItem?.AlignSlots?.Value == true;
			int[] array;
			if (valueOrDefault)
			{
				List<int> list = new List<int>();
				foreach (var item2 in GetEnabledSlotsWithIndex(gridDefForItem))
				{
					if (item2.Index >= 0 && item2.Index < allGridInventories.Count)
					{
						list.Add(item2.Index);
					}
				}
				if (list.Count == 0)
				{
					list.Add(0);
				}
				array = list.ToArray();
			}
			else
			{
				array = ((num < 0) ? new int[1] : new int[1] { num });
			}
			int num2 = Math.Max(minC, Math.Min(maxC, reqC));
			int num3 = Math.Max(minR, Math.Min(maxR, reqR));
			PixelWindow contentWindow = item.contentWindow;
			int baseWinW = ((contentWindow != null) ? contentWindow.widthPixels : 0);
			int baseWinH = ((contentWindow != null) ? contentWindow.heightPixels : 0);
			int index = Math.Max(0, Math.Min(array[0], allGridInventories.Count - 1));
			GameGridInventory val = allGridInventories[index];
			int? num4;
			if (val == null)
			{
				num4 = null;
			}
			else
			{
				GridShape inventoryShape = val.inventoryShape;
				num4 = ((inventoryShape != null) ? new int?(inventoryShape.width) : ((int?)null));
			}
			int num5 = num4 ?? num2;
			GameGridInventory val2 = allGridInventories[index];
			int? num6;
			if (val2 == null)
			{
				num6 = null;
			}
			else
			{
				GridShape inventoryShape2 = val2.inventoryShape;
				num6 = ((inventoryShape2 != null) ? new int?(inventoryShape2.height) : ((int?)null));
			}
			int num7 = num6 ?? num3;
			int num8 = num2 - num5;
			int num9 = num3 - num7;
			List<(int, int)> list2 = new List<(int, int)>();
			int[] array2 = array;
			foreach (int num10 in array2)
			{
				if (num10 < 0 || num10 >= allGridInventories.Count)
				{
					continue;
				}
				GameGridInventory val3 = allGridInventories[num10];
				if (((val3 != null) ? val3.inventoryShape : null) == null)
				{
					continue;
				}
				int width = val3.inventoryShape.width;
				int height = val3.inventoryShape.height;
				int num11 = (valueOrDefault ? num2 : Math.Max(minC, Math.Min(maxC, width + num8)));
				int num12 = (valueOrDefault ? num3 : Math.Max(minR, Math.Min(maxR, height + num9)));
				if (num11 != width || num12 != height)
				{
					if (!CanContainItems(val3, num11, num12))
					{
						return false;
					}
					int widthPixels = val3.widthPixels;
					int heightPixels = val3.heightPixels;
					SetShapeKeepTransparency(val3, num11, num12);
					val3.ForceRecomputeDynamicBackground();
					list2.Add((val3.widthPixels - widthPixels, val3.heightPixels - heightPixels));
				}
			}
			if (contentWindow != null)
			{
				bool horizontal = IsHorizontalLayout(allGridInventories, array);
				ComputeMultiSlotWindowSize(baseWinW, baseWinH, list2, horizontal, out var newWinW, out var newWinH);
				int index2 = Math.Max(0, Math.Min(array[0], allGridInventories.Count - 1));
				GameGridInventory val4 = allGridInventories[index2];
				int borderW = Math.Max(0, newWinW - val4.widthPixels);
				int borderH = Math.Max(0, newWinH - val4.heightPixels);
				ApplyWindowSize(contentWindow, val4, newWinW, newWinH, borderW, borderH);
				actualWidth = newWinW;
				actualHeight = newWinH;
			}
			if (remember)
			{
				RememberGridShape(item, num2, num3);
				RememberWindowSize(item, actualWidth, actualHeight);
				SetItemTag(item, "DIY_UPGRADE_W_TAG", num2);
				SetItemTag(item, "DIY_UPGRADE_H_TAG", num3);
			}
			return true;
		}

		private static GridDef GetGridDefForItem(GameItem item)
		{
			if (item == null)
			{
				return null;
			}
			GridDef wageGridDef = WageContainerRegistry.GetWageGridDef(item);
			if (wageGridDef != null)
			{
				return wageGridDef;
			}
			if (_multiSlotModule.Enable.Value && IsAnyId(item, _multiSlotModule.IdList))
			{
				return _multiSlotModule;
			}
			if (_multiSlot.Enable.Value && IsAnyId(item, _multiSlot.IdList))
			{
				return _multiSlot;
			}
			if (_box.Enable.Value && IsAnyId(item, _box.IdList))
			{
				return _box;
			}
			if (_backpack.Enable.Value && IsAnyId(item, _backpack.IdList))
			{
				return _backpack;
			}
			if (_cage.Enable.Value && IsAnyId(item, _cage.IdList))
			{
				return _cage;
			}
			return null;
		}

		private static IEnumerable<(int Index, TargetSlot Slot)> GetEnabledSlotsWithIndex(GridDef d)
		{
			if (d == null)
			{
				yield break;
			}
			for (int i = 0; i < d.Slots.Length; i++)
			{
				TargetSlot targetSlot = d.Slots[i];
				if (targetSlot != null && (targetSlot.Enable == null || targetSlot.Enable.Value))
				{
					yield return (Index: i, Slot: targetSlot);
				}
			}
		}

		private static TargetSlot GetPrimarySlot(GridDef d)
		{
			if (d == null)
			{
				return null;
			}
			TargetSlot[] slots = d.Slots;
			foreach (TargetSlot targetSlot in slots)
			{
				if (targetSlot != null && (targetSlot.Enable == null || targetSlot.Enable.Value))
				{
					return targetSlot;
				}
			}
			if (d.Slots.Length == 0)
			{
				return null;
			}
			return d.Slots[0];
		}

		private static void ApplyWindowSize(PixelWindow window, GameGridInventory inventory, int width, int height, int borderW, int borderH)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			if (window == null || inventory == null)
			{
				return;
			}
			bool isDragging = ContainerWindowResizer.IsDragging;
			try
			{
				window.ResizePixels(width, height);
				try
				{
					RectTransform rectTransform = window.rectTransform;
					if ((Object)(object)rectTransform != (Object)null)
					{
						rectTransform.sizeDelta = new Vector2((float)width, (float)height);
					}
					TreeNodeRender outsideBorder = window.outsideBorder;
					if ((Object)(object)outsideBorder != (Object)null && (Object)(object)outsideBorder.rectTransform != (Object)null)
					{
						outsideBorder.rectTransform.sizeDelta = new Vector2((float)width, (float)height);
					}
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:resize-force-size", ex);
				}
				try
				{
					((GameInventory)inventory).Validate();
				}
				catch (Exception ex2)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2130", ex2);
				}
				_layoutWindow = ((Il2CppObjectBase)window).Pointer;
				_layoutInventory = ((Il2CppObjectBase)inventory).Pointer;
				_layoutWindowWidth = width;
				_layoutWindowHeight = height;
				_layoutChildWidth = width - borderW;
				_layoutChildHeight = height - borderH;
				window.Validate();
			}
			catch (Exception ex3)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2139", ex3);
			}
			finally
			{
				_layoutWindow = IntPtr.Zero;
				_layoutInventory = IntPtr.Zero;
				_layoutWindowWidth = 0;
				_layoutWindowHeight = 0;
				_layoutChildWidth = 0;
				_layoutChildHeight = 0;
			}
			if (isDragging)
			{
				try
				{
					window.FixBounds();
					return;
				}
				catch
				{
					return;
				}
			}
			try
			{
				window.FixBounds();
				RectTransform rectTransform2 = window.rectTransform;
				if ((Object)(object)rectTransform2 != (Object)null)
				{
					try
					{
						LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform2);
					}
					catch (Exception ex4)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:2164", ex4);
					}
					Graphic component = ((Component)rectTransform2).GetComponent<Graphic>();
					if ((Object)(object)component != (Object)null)
					{
						component.SetAllDirty();
					}
				}
				if (inventory != null)
				{
					Component val = ((Il2CppObjectBase)inventory).TryCast<Component>();
					if ((Object)(object)val != (Object)null && (Object)(object)val.gameObject != (Object)null)
					{
						GameObject gameObject = val.gameObject;
						Graphic component2 = gameObject.GetComponent<Graphic>();
						if ((Object)(object)component2 != (Object)null)
						{
							component2.SetAllDirty();
						}
						Transform transform = gameObject.transform;
						int childCount = transform.childCount;
						for (int i = 0; i < childCount; i++)
						{
							Transform child = transform.GetChild(i);
							if ((Object)(object)child != (Object)null)
							{
								Graphic val2 = ((Il2CppObjectBase)child).TryCast<Graphic>();
								if ((Object)(object)val2 != (Object)null)
								{
									val2.SetAllDirty();
									LayoutRebuilder.MarkLayoutForRebuild(val2.rectTransform);
								}
							}
						}
					}
				}
				if ((Object)(object)window.outsideBorder != (Object)null)
				{
					RectTransform rectTransform3 = window.outsideBorder.rectTransform;
					if ((Object)(object)rectTransform3 != (Object)null)
					{
						LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform3);
					}
				}
				try
				{
					Component val3 = ((Il2CppObjectBase)window).TryCast<Component>();
					if ((Object)(object)((val3 != null) ? val3.gameObject : null) != (Object)null)
					{
						Il2CppArrayBase<Graphic> componentsInChildren = val3.gameObject.GetComponentsInChildren<Graphic>(true);
						if (componentsInChildren != null)
						{
							for (int j = 0; j < componentsInChildren.Count; j++)
							{
								if ((Object)(object)componentsInChildren[j] != (Object)null)
								{
									componentsInChildren[j].SetAllDirty();
								}
							}
						}
					}
				}
				catch (Exception ex5)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:win-dirty-all", ex5);
				}
				ScheduleDelayedRedraw(window);
			}
			catch (Exception ex6)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2205", ex6);
			}
		}

		private static void ScheduleDelayedRedraw(PixelWindow window)
		{
			if (window == null)
			{
				return;
			}
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = ((Il2CppObjectBase)window).Pointer;
			}
			catch
			{
			}
			if (intPtr == IntPtr.Zero)
			{
				try
				{
					MelonCoroutines.Start(DelayedRedraw(window));
					return;
				}
				catch
				{
					return;
				}
			}
			if (!_redrawPending.Add(intPtr))
			{
				return;
			}
			try
			{
				MelonCoroutines.Start(DelayedRedrawTracked(window, intPtr));
			}
			catch
			{
				_redrawPending.Remove(intPtr);
			}
		}

		private static IEnumerator DelayedRedrawTracked(PixelWindow window, IntPtr ptr)
		{
			try
			{
				yield return DelayedRedraw(window);
			}
			finally
			{
				_redrawPending.Remove(ptr);
			}
		}

		private static IEnumerator DelayedRedraw(PixelWindow window)
		{
			yield return null;
			try
			{
				if (window == null || (Object)(object)window.rectTransform == (Object)null)
				{
					yield break;
				}
				RectTransform rectTransform = window.rectTransform;
				if ((Object)(object)rectTransform != (Object)null)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
				}
				if ((Object)(object)window.outsideBorder != (Object)null)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(window.outsideBorder.rectTransform);
				}
				PixelElement childElement = window.childElement;
				GameGridInventory val = ((childElement != null) ? ((Il2CppObjectBase)childElement).TryCast<GameGridInventory>() : null);
				if (val != null)
				{
					Component val2 = ((Il2CppObjectBase)val).TryCast<Component>();
					if ((Object)(object)val2 != (Object)null && (Object)(object)val2.gameObject != (Object)null)
					{
						foreach (Graphic componentsInChild in val2.gameObject.GetComponentsInChildren<Graphic>(true))
						{
							if ((Object)(object)componentsInChild != (Object)null)
							{
								componentsInChild.SetAllDirty();
							}
						}
					}
				}
				try
				{
					Component val3 = ((Il2CppObjectBase)window).TryCast<Component>();
					if (!((Object)(object)((val3 != null) ? val3.gameObject : null) != (Object)null))
					{
						yield break;
					}
					Il2CppArrayBase<Graphic> componentsInChildren = val3.gameObject.GetComponentsInChildren<Graphic>(true);
					if (componentsInChildren == null)
					{
						yield break;
					}
					for (int i = 0; i < componentsInChildren.Count; i++)
					{
						if ((Object)(object)componentsInChildren[i] != (Object)null)
						{
							componentsInChildren[i].SetAllDirty();
						}
					}
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:delayed-dirty-all", ex);
				}
			}
			catch (Exception ex2)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2225", ex2);
			}
		}

		internal static bool TryOverrideWindowWidth(PixelWindow window, ref int value)
		{
			if (_layoutWindow == IntPtr.Zero || window == null || ((Il2CppObjectBase)window).Pointer != _layoutWindow)
			{
				return false;
			}
			value = _layoutWindowWidth;
			return true;
		}

		internal static bool TryOverrideWindowHeight(PixelWindow window, ref int value)
		{
			if (_layoutWindow == IntPtr.Zero || window == null || ((Il2CppObjectBase)window).Pointer != _layoutWindow)
			{
				return false;
			}
			value = _layoutWindowHeight;
			return true;
		}

		internal static bool TryOverrideChildWidth(GameGridInventory inventory, ref int value)
		{
			if (_layoutInventory == IntPtr.Zero || inventory == null || ((Il2CppObjectBase)inventory).Pointer != _layoutInventory)
			{
				return false;
			}
			value = _layoutChildWidth;
			return true;
		}

		internal static bool TryOverrideChildHeight(GameGridInventory inventory, ref int value)
		{
			if (_layoutInventory == IntPtr.Zero || inventory == null || ((Il2CppObjectBase)inventory).Pointer != _layoutInventory)
			{
				return false;
			}
			value = _layoutChildHeight;
			return true;
		}

		internal static bool CanContainItems(GameGridInventory inventory, int columns, int rows)
		{
			if (inventory == null || inventory.items == null)
			{
				return true;
			}
			try
			{
				if (inventory.inventoryShape != null && columns >= inventory.inventoryShape.width && rows >= inventory.inventoryShape.height)
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2267", ex);
			}
			for (int i = 0; i < inventory.items.Count; i++)
			{
				GameItem val = inventory.items[i];
				GridShape val2 = ((val != null) ? val.modifiedShape : null) ?? ((val != null) ? val.shape : null);
				if (val2 != null && (val2.minX < 0 || val2.minY < 0 || val2.maxX >= columns || val2.maxY >= rows))
				{
					return false;
				}
			}
			return true;
		}

		internal static void RememberGridShape(GameItem item, int columns, int rows)
		{
			SetItemTag(item, "DIY_UPGRADE_W_TAG", columns);
			SetItemTag(item, "DIY_UPGRADE_H_TAG", rows);
		}

		internal static void RememberSlotShape(GameItem item, int slotIndex, int columns, int rows)
		{
			SetItemTag(item, TagSlotW(slotIndex), columns);
			SetItemTag(item, TagSlotH(slotIndex), rows);
		}

		internal static void RememberWindowSize(GameItem item, int width, int height)
		{
			SetItemTag(item, "DIY_UPGRADE_WIN_W_TAG", width);
			SetItemTag(item, "DIY_UPGRADE_WIN_H_TAG", height);
		}

		internal static bool TryRestoreRememberedGridShape(GameItem item)
		{
			//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_048b: Unknown result type (might be due to invalid IL or missing references)
			if (item == null)
			{
				return false;
			}
			GridDef gridDefForItem = GetGridDefForItem(item);
			if (gridDefForItem == null)
			{
				if (!WageContainerRegistry.IsWageBox(item) && !WageContainerRegistry.IsVoidBeadStorage(item))
				{
					return false;
				}
				try
				{
					_ggiCache.Clear();
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2304", ex);
				}
				List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
				if (allGridInventories.Count > 0)
				{
					GameGridInventory val = allGridInventories[0];
					if (((val != null) ? val.inventoryShape : null) != null)
					{
						if (WageContainerRegistry.IsWageBox(item))
						{
							WageContainerRegistry.MigrateLegacyWageBox(item, allGridInventories[0]);
						}
						else
						{
							WageContainerRegistry.MigrateLegacyVoidBead(item, allGridInventories[0]);
						}
						try
						{
							_ggiCache.Clear();
						}
						catch (Exception ex2)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:2312", ex2);
						}
					}
				}
				gridDefForItem = GetGridDefForItem(item);
				if (gridDefForItem == null)
				{
					return false;
				}
			}
			List<GameGridInventory> allGridInventories2 = GetAllGridInventories(item);
			if (allGridInventories2.Count == 0)
			{
				return false;
			}
			PixelWindow contentWindow = item.contentWindow;
			List<(int, int, int)> list = new List<(int, int, int)>();
			foreach (var item3 in GetEnabledSlotsWithIndex(gridDefForItem))
			{
				int item2 = item3.Index;
				if (item2 < 0 || item2 >= allGridInventories2.Count)
				{
					continue;
				}
				GameGridInventory val2 = allGridInventories2[item2];
				if (((val2 != null) ? val2.inventoryShape : null) == null)
				{
					continue;
				}
				int num = SafeTagInt(item, TagSlotW(item2), 0);
				int num2 = SafeTagInt(item, TagSlotH(item2), 0);
				if ((num <= 0 || num2 <= 0) && item2 == 0)
				{
					num = SafeTagInt(item, "DIY_UPGRADE_W_TAG", 0);
					num2 = SafeTagInt(item, "DIY_UPGRADE_H_TAG", 0);
				}
				if (num <= 0 || num2 <= 0)
				{
					continue;
				}
				if (TryGetGridShapeLimits(item, out var minC, out var minR, out var maxC, out var maxR))
				{
					if (num < minC)
					{
						num = minC;
					}
					if (num2 < minR)
					{
						num2 = minR;
					}
					if (num > maxC)
					{
						num = maxC;
					}
					if (num2 > maxR)
					{
						num2 = maxR;
					}
				}
				if (CanContainItems(val2, num, num2))
				{
					list.Add((item2, num, num2));
				}
			}
			if (list.Count == 0)
			{
				return false;
			}
			GameGridInventory val3 = allGridInventories2[list[0].Item1];
			if (((val3 != null) ? val3.inventoryShape : null) == null || val3.inventoryShape.width <= 0 || val3.inventoryShape.height <= 0)
			{
				return false;
			}
			bool flag = contentWindow != null;
			float num3 = 0f;
			float num4 = 0f;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			int num8 = 0;
			if (flag)
			{
				num3 = (float)val3.widthPixels / (float)val3.inventoryShape.width;
				num4 = (float)val3.heightPixels / (float)val3.inventoryShape.height;
				num5 = contentWindow.widthPixels;
				num6 = contentWindow.heightPixels;
				num7 = Math.Max(0, num5 - val3.widthPixels);
				num8 = Math.Max(0, num6 - val3.heightPixels);
			}
			foreach (var item4 in list)
			{
				GameGridInventory val4 = allGridInventories2[item4.Item1];
				if (((val4 != null) ? val4.inventoryShape : null) != null && (val4.inventoryShape.width != item4.Item2 || val4.inventoryShape.height != item4.Item3))
				{
					SetShapeKeepTransparency(val4, item4.Item2, item4.Item3);
					val4.ForceRecomputeDynamicBackground();
				}
			}
			if (allGridInventories2.Count > 0)
			{
				GameGridInventory val5 = allGridInventories2[0];
				if (((val5 != null) ? val5.inventoryShape : null) != null)
				{
					SetItemTag(item, "DIY_UPGRADE_W_TAG", allGridInventories2[0].inventoryShape.width);
					SetItemTag(item, "DIY_UPGRADE_H_TAG", allGridInventories2[0].inventoryShape.height);
				}
			}
			if (flag && num3 > 0f && num4 > 0f)
			{
				int num9 = (int)Math.Round((float)list[0].Item2 * num3);
				int num10 = (int)Math.Round((float)list[0].Item3 * num4);
				int num11 = num9 + num7;
				int num12 = num10 + num8;
				bool flag2 = false;
				try
				{
					if ((Object)(object)contentWindow.rectTransform != (Object)null)
					{
						flag2 = ((Component)contentWindow.rectTransform).gameObject.activeInHierarchy;
					}
				}
				catch (Exception ex3)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2394", ex3);
				}
				if (num11 != num5 || num12 != num6)
				{
					ApplyWindowSize(contentWindow, val3, num11, num12, num7, num8);
				}
				else if (flag2)
				{
					try
					{
						val3.ForceRecomputeDynamicBackground();
						contentWindow.FixBounds();
						RectTransform rectTransform = contentWindow.rectTransform;
						if ((Object)(object)rectTransform != (Object)null)
						{
							try
							{
								rectTransform.sizeDelta = new Vector2((float)num11, (float)num12);
							}
							catch (Exception ex4)
							{
								CatchLogOnce.Warn("DiyUpgradeMod.cs:2405", ex4);
							}
							LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
						}
						if ((Object)(object)contentWindow.outsideBorder != (Object)null && (Object)(object)contentWindow.outsideBorder.rectTransform != (Object)null)
						{
							try
							{
								contentWindow.outsideBorder.rectTransform.sizeDelta = new Vector2((float)num11, (float)num12);
							}
							catch (Exception ex5)
							{
								CatchLogOnce.Warn("DiyUpgradeMod.cs:2410", ex5);
							}
							LayoutRebuilder.ForceRebuildLayoutImmediate(contentWindow.outsideBorder.rectTransform);
						}
						contentWindow.Validate();
						((GameInventory)val3).Validate();
					}
					catch (Exception ex6)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:2416", ex6);
					}
				}
				try
				{
					Component val6 = ((Il2CppObjectBase)val3).TryCast<Component>();
					if ((Object)(object)val6 != (Object)null && (Object)(object)val6.gameObject != (Object)null)
					{
						foreach (Graphic componentsInChild in val6.gameObject.GetComponentsInChildren<Graphic>(true))
						{
							if ((Object)(object)componentsInChild != (Object)null)
							{
								componentsInChild.SetAllDirty();
							}
						}
					}
				}
				catch (Exception ex7)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2427", ex7);
				}
			}
			return true;
		}

		private static string MetaKey(GameItem item)
		{
			string text = IdOf(item);
			try
			{
				long num = 0L;
				try
				{
					num = item.uniqueId;
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2438", ex);
				}
				if (num != 0L)
				{
					return text + "#" + num;
				}
				try
				{
					return text + "@" + ((Il2CppObjectBase)item).Pointer.ToInt64().ToString("X");
				}
				catch
				{
					return text + "@unknown";
				}
			}
			catch (Exception ex2)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2441", ex2);
				return text;
			}
		}

		private static (int BaseW, int BaseH, int Spent) LoadMeta(GameItem item)
		{
			string key = MetaKey(item);
			if (_metaCache.TryGetValue(key, out (int, int, int) value))
			{
				return value;
			}
			int num = SafeTagInt(item, "DIY_UPGRADE_BASE_W_TAG", 0);
			int num2 = SafeTagInt(item, "DIY_UPGRADE_BASE_H_TAG", 0);
			int num3 = SafeTagInt(item, "DIY_UPGRADE_SPENT_TAG", 0);
			if ((num <= 0 || num2 <= 0) && num3 > 0)
			{
				int num4 = SafeTagInt(item, "DIY_UPGRADE_W_TAG", 0);
				int num5 = SafeTagInt(item, "DIY_UPGRADE_H_TAG", 0);
				GridDef gridDefForItem = GetGridDefForItem(item);
				int num6 = 1;
				int num7 = 1;
				if (gridDefForItem != null)
				{
					TargetSlot primarySlot = GetPrimarySlot(gridDefForItem);
					if (primarySlot != null)
					{
						num6 = Math.Max(1, primarySlot.IncW.Value);
						num7 = Math.Max(1, primarySlot.IncH.Value);
					}
				}
				if (num4 > 0 && num <= 0)
				{
					num = Math.Max(1, num4 - num3 * num6);
				}
				if (num5 > 0 && num2 <= 0)
				{
					num2 = Math.Max(1, num5 - num3 * num7);
				}
			}
			if (num > 0 && num2 > 0)
			{
				(int, int, int) tuple = (num, num2, num3);
				_metaCache[key] = tuple;
				return tuple;
			}
			return (BaseW: 0, BaseH: 0, Spent: 0);
		}

		internal static void SaveMeta(GameItem item, int baseW, int baseH, int spent)
		{
			string key = MetaKey(item);
			_metaCache[key] = (baseW, baseH, spent);
			SetItemTag(item, "DIY_UPGRADE_SPENT_TAG", spent);
			SetItemTag(item, "DIY_UPGRADE_BASE_W_TAG", baseW);
			SetItemTag(item, "DIY_UPGRADE_BASE_H_TAG", baseH);
		}

		private static bool TryGetMeta(GameItem item, out int baseW, out int baseH, out int spent)
		{
			baseW = 0;
			baseH = 0;
			spent = 0;
			if (item == null)
			{
				return false;
			}
			string key = MetaKey(item);
			if (_metaCache.TryGetValue(key, out (int, int, int) value))
			{
				(baseW, baseH, spent) = value;
				return true;
			}
			baseW = SafeTagInt(item, "DIY_UPGRADE_BASE_W_TAG", 0);
			baseH = SafeTagInt(item, "DIY_UPGRADE_BASE_H_TAG", 0);
			spent = SafeTagInt(item, "DIY_UPGRADE_SPENT_TAG", 0);
			if (baseW <= 0 && baseH <= 0)
			{
				return spent > 0;
			}
			return true;
		}

		internal static bool SeedBaselineIfMissing(GameItem item)
		{
			if (item == null || IdFieldNull(item) || TagSystemFieldNull(item))
			{
				return false;
			}
			if (!IsResizableContainer(item))
			{
				return false;
			}
			try
			{
				(int, int, int) tuple = LoadMeta(item);
				if (tuple.Item1 > 0 && tuple.Item2 > 0)
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
			try
			{
				GridDef gridDefForItem = GetGridDefForItem(item);
				if (gridDefForItem == null)
				{
					return false;
				}
				List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
				if (allGridInventories.Count == 0)
				{
					return false;
				}
				int num = -1;
				foreach (var item3 in GetEnabledSlotsWithIndex(gridDefForItem))
				{
					if (item3.Index >= 0 && item3.Index < allGridInventories.Count)
					{
						(num, _) = item3;
						break;
					}
				}
				if (num < 0)
				{
					return false;
				}
				GameGridInventory val = allGridInventories[num];
				if (((val != null) ? val.inventoryShape : null) == null)
				{
					return false;
				}
				int width = val.inventoryShape.width;
				int height = val.inventoryShape.height;
				if (width <= 0 || height <= 0)
				{
					return false;
				}
				SaveMeta(item, width, height, 0);
				foreach (var item4 in GetEnabledSlotsWithIndex(gridDefForItem))
				{
					int item2 = item4.Index;
					if (item2 != num && item2 >= 0 && item2 < allGridInventories.Count)
					{
						GameGridInventory val2 = allGridInventories[item2];
						if (((val2 != null) ? val2.inventoryShape : null) != null)
						{
							RememberSlotShape(item, item2, val2.inventoryShape.width, val2.inventoryShape.height);
						}
					}
				}
				return true;
			}
			catch (Exception ex)
			{
				if (_log != null)
				{
					_log.Warning("[容器升级] SeedBaselineIfMissing 失败: " + ex.Message);
				}
				return false;
			}
		}

		internal static void ClearReapplyCache(GameItem item)
		{
			if (item != null)
			{
				IntPtr intPtr = IntPtr.Zero;
				try
				{
					intPtr = ((Il2CppObjectBase)item).Pointer;
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2536", ex);
				}
				if (intPtr != IntPtr.Zero)
				{
					_lastReapplyFromMetaTime.Remove(intPtr);
				}
			}
		}

		internal static void ReapplyFromMeta(GameItem item)
		{
			if (item == null || IdFieldNull(item) || TagSystemFieldNull(item))
			{
				return;
			}
			try
			{
				if ((WageContainerRegistry.IsWageBox(item) || WageContainerRegistry.IsVoidBeadStorage(item)) && !WageContainerRegistry.HasDiyMigrated(item))
				{
					try
					{
						_ggiCache.Clear();
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:2548", ex);
					}
					List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
					if (allGridInventories.Count > 0)
					{
						GameGridInventory val = allGridInventories[0];
						if (((val != null) ? val.inventoryShape : null) != null)
						{
							if (WageContainerRegistry.IsWageBox(item))
							{
								WageContainerRegistry.MigrateLegacyWageBox(item, allGridInventories[0]);
							}
							else
							{
								WageContainerRegistry.MigrateLegacyVoidBead(item, allGridInventories[0]);
							}
							try
							{
								_ggiCache.Clear();
							}
							catch (Exception ex2)
							{
								CatchLogOnce.Warn("DiyUpgradeMod.cs:2556", ex2);
							}
						}
					}
				}
			}
			catch (Exception ex3)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2560", ex3);
			}
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = ((Il2CppObjectBase)item).Pointer;
			}
			catch (Exception ex4)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2562", ex4);
			}
			if (intPtr != IntPtr.Zero)
			{
				if (_lastReapplyFromMetaTime.TryGetValue(intPtr, out var value) && (DateTime.UtcNow - value).TotalSeconds < 3.0)
				{
					return;
				}
				_lastReapplyFromMetaTime[intPtr] = DateTime.UtcNow;
			}
			try
			{
				ContainerWindowResizer.Attach(item);
			}
			catch (Exception ex5)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2568", ex5);
			}
			bool flag = false;
			try
			{
				flag = item.contentWindow != null;
			}
			catch (Exception ex6)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2570", ex6);
			}
			bool flag2 = false;
			try
			{
				flag2 = TryRestoreRememberedGridShape(item);
			}
			catch (Exception ex7)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2572", ex7);
			}
			if (!(flag2 & flag))
			{
				return;
			}
			try
			{
				MelonCoroutines.Start(ForceRefreshWindowAfterRestore(item));
			}
			catch (Exception ex8)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2574", ex8);
			}
		}

		private static IEnumerator ForceRefreshWindowAfterRestore(GameItem item)
		{
			yield return null;
			yield return null;
			try
			{
				if (item == null)
				{
					yield break;
				}
				PixelWindow contentWindow = item.contentWindow;
				if (contentWindow == null)
				{
					yield break;
				}
				GameGridInventory resizableInventory = GetResizableInventory(item);
				if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) == null)
				{
					yield break;
				}
				RefreshAllGridsOf(item);
				try
				{
					contentWindow.FixBounds();
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2589", ex);
				}
				try
				{
					contentWindow.Validate();
				}
				catch (Exception ex2)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2590", ex2);
				}
				RectTransform rectTransform = contentWindow.rectTransform;
				if ((Object)(object)rectTransform != (Object)null)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
				}
				if ((Object)(object)contentWindow.outsideBorder != (Object)null && (Object)(object)contentWindow.outsideBorder.rectTransform != (Object)null)
				{
					LayoutRebuilder.ForceRebuildLayoutImmediate(contentWindow.outsideBorder.rectTransform);
				}
				Component val = ((Il2CppObjectBase)resizableInventory).TryCast<Component>();
				if ((Object)(object)val == (Object)null || (Object)(object)val.gameObject == (Object)null)
				{
					yield break;
				}
				foreach (Graphic componentsInChild in val.gameObject.GetComponentsInChildren<Graphic>(true))
				{
					if ((Object)(object)componentsInChild != (Object)null)
					{
						componentsInChild.SetAllDirty();
					}
				}
			}
			catch (Exception ex3)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2600", ex3);
			}
		}

		internal static List<GameGridInventory> GetAllGridInventories(GameItem item)
		{
			if (item == null)
			{
				return new List<GameGridInventory>();
			}
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = ((Il2CppObjectBase)item).Pointer;
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2607", ex);
			}
			if (intPtr != IntPtr.Zero && _ggiCache.TryGetValue(intPtr, out (DateTime, List<GameGridInventory>) value) && (DateTime.UtcNow - value.Item1).TotalSeconds < 2.0)
			{
				return value.Item2;
			}
			List<GameGridInventory> list;
			try
			{
				list = new List<GameGridInventory>();
				CollectGgisFromWindow(item.contentWindow, list);
			}
			catch
			{
				list = new List<GameGridInventory>();
			}
			if (intPtr != IntPtr.Zero)
			{
				_ggiCache[intPtr] = (DateTime.UtcNow, list);
			}
			return list;
		}

		internal static void SetShapeKeepTransparency(GameGridInventory grid, int w, int h)
		{
			if (grid != null)
			{
				float num = 0f;
				try
				{
					num = grid.transparency;
				}
				catch
				{
				}
				grid.SetShape(w, h);
				RestoreTransparency(grid, num);
				RebuildGridFill(grid, num);
			}
		}

		internal static void RestoreTransparency(GameGridInventory grid, float before)
		{
			if (grid == null || before <= 0.001f)
			{
				return;
			}
			try
			{
				if (Math.Abs(grid.transparency - before) > 0.001f)
				{
					grid.SetTransparency(before);
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:restore-transparency", ex);
			}
		}

		internal static void RebuildGridFill(GameGridInventory g, float transparency)
		{
			if (g == null)
			{
				return;
			}
			if (ContainerWindowResizer.IsDragging)
			{
				try
				{
					((GameInventory)g).Validate();
				}
				catch
				{
				}
				try
				{
					HideOutOfBoundsNodes(g);
					return;
				}
				catch
				{
					return;
				}
			}
			try
			{
				DestroyNodeList(g.dynamicBackgrounds);
				DestroyNodeList(g.dynamicR0);
				DestroyNodeList(g.dynamicR1);
				DestroyNodeList(g.dynamicR2);
				DestroyNodeList(g.dynamicR3);
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:clear-nodes", ex);
			}
			try
			{
				g.renderDynamicBackground = true;
			}
			catch (Exception ex2)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:fill-on", ex2);
			}
			try
			{
				((GameInventory)g).Validate();
			}
			catch (Exception ex3)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:fill-v1", ex3);
			}
			try
			{
				g.ValidateBackground();
			}
			catch (Exception ex4)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:fill-vbg", ex4);
			}
			if (!FillHealthy(g))
			{
				try
				{
					g.renderDynamicBackground = false;
				}
				catch
				{
				}
				try
				{
					((GameInventory)g).Validate();
				}
				catch
				{
				}
				try
				{
					g.renderDynamicBackground = true;
				}
				catch
				{
				}
				try
				{
					((GameInventory)g).Validate();
				}
				catch
				{
				}
				try
				{
					g.ValidateBackground();
				}
				catch
				{
				}
			}
			try
			{
				g.ForceRecomputeDynamicBackground();
			}
			catch (Exception ex5)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:fill-fr", ex5);
			}
			ApplyMeshAlpha(g, Mathf.Clamp01(1f - transparency));
		}

		private static void DestroyNodeList(List<TreeNodeRender> list)
		{
			if (list == null)
			{
				return;
			}
			int count = list.Count;
			for (int i = 0; i < count; i++)
			{
				try
				{
					TreeNodeRender val = list[i];
					if (!((Object)(object)val == (Object)null))
					{
						RectTransform rectTransform = val.rectTransform;
						if (!((Object)(object)rectTransform == (Object)null))
						{
							Object.Destroy((Object)(object)((Component)rectTransform).gameObject);
						}
					}
				}
				catch
				{
				}
			}
			try
			{
				list.Clear();
			}
			catch
			{
			}
		}

		private static void HideOutOfBoundsNodes(GameGridInventory g)
		{
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			if (((g != null) ? g.inventoryShape : null) == null)
			{
				return;
			}
			int num = 0;
			try
			{
				num = g.heightPixels;
			}
			catch
			{
			}
			if (num <= 0)
			{
				return;
			}
			IntPtr intPtr = IntPtr.Zero;
			try
			{
				intPtr = ((Il2CppObjectBase)g).Pointer;
			}
			catch
			{
			}
			if (intPtr != IntPtr.Zero)
			{
				(int, int, int) tuple = (g.inventoryShape.width, g.inventoryShape.height, num);
				if (_lastHideBounds.TryGetValue(intPtr, out (int, int, int) value))
				{
					(int, int, int) tuple2 = value;
					(int, int, int) tuple3 = tuple;
					if (tuple2.Item1 == tuple3.Item1 && tuple2.Item2 == tuple3.Item2 && tuple2.Item3 == tuple3.Item3)
					{
						return;
					}
				}
				if (_lastHideBounds.Count > 64)
				{
					_lastHideBounds.Clear();
				}
				_lastHideBounds[intPtr] = tuple;
			}
			float num2 = (float)num * 0.5f + 20f;
			for (int i = 0; i < 5; i++)
			{
				List<TreeNodeRender> val = MeshList(g, i);
				if (val == null)
				{
					continue;
				}
				for (int j = 0; j < val.Count; j++)
				{
					TreeNodeRender val2 = val[j];
					if ((Object)(object)val2 == (Object)null)
					{
						continue;
					}
					RectTransform rectTransform = val2.rectTransform;
					if ((Object)(object)rectTransform == (Object)null)
					{
						continue;
					}
					try
					{
						float y = rectTransform.anchoredPosition.y;
						bool flag = y > num2 || y < 0f - num2;
						GameObject gameObject = ((Component)rectTransform).gameObject;
						if ((Object)(object)gameObject != (Object)null && gameObject.activeSelf == flag)
						{
							gameObject.SetActive(!flag);
						}
					}
					catch
					{
					}
				}
			}
		}

		internal static void ApplyMeshAlpha(GameGridInventory g, float alpha)
		{
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			if (g == null)
			{
				return;
			}
			for (int i = 0; i < 5; i++)
			{
				try
				{
					List<TreeNodeRender> val = MeshList(g, i);
					if (val == null)
					{
						continue;
					}
					for (int j = 0; j < val.Count; j++)
					{
						TreeNodeRender val2 = val[j];
						if (!((Object)(object)val2 == (Object)null))
						{
							Image image = val2.image;
							if (!((Object)(object)image == (Object)null))
							{
								Color color = ((Graphic)image).color;
								((Graphic)image).color = new Color(color.r, color.g, color.b, alpha);
								((Graphic)image).SetAllDirty();
							}
						}
					}
				}
				catch
				{
				}
			}
		}

		private static List<TreeNodeRender> MeshList(GameGridInventory g, int k)
		{
			return k switch
			{
				0 => g.dynamicBackgrounds, 
				1 => g.dynamicR0, 
				2 => g.dynamicR1, 
				3 => g.dynamicR2, 
				_ => g.dynamicR3, 
			};
		}

		internal static bool FillHealthy(GameGridInventory g)
		{
			if (g == null)
			{
				return true;
			}
			if (g.inventoryShape == null)
			{
				return true;
			}
			int width = g.inventoryShape.width;
			int height = g.inventoryShape.height;
			if (width <= 0 || height <= 0)
			{
				return true;
			}
			int num = (width + 2) * (height + 2);
			int num2 = 0;
			try
			{
				num2 = g.dynamicBackgrounds?.Count ?? 0;
			}
			catch
			{
			}
			return Math.Abs(num2 - num) * 20 <= num;
		}

		internal static void RefreshAllGridsOf(GameItem item)
		{
			if (item == null)
			{
				return;
			}
			try
			{
				List<GameGridInventory> allGridInventories = GetAllGridInventories(item);
				if (allGridInventories == null)
				{
					return;
				}
				for (int i = 0; i < allGridInventories.Count; i++)
				{
					GameGridInventory val = allGridInventories[i];
					if (((val != null) ? val.inventoryShape : null) == null)
					{
						continue;
					}
					float num = 0f;
					try
					{
						num = val.transparency;
					}
					catch
					{
					}
					bool flag = false;
					try
					{
						flag = FillHealthy(val);
					}
					catch
					{
					}
					try
					{
						if (!flag)
						{
							RebuildGridFill(val, num);
						}
						else
						{
							ApplyMeshAlpha(val, Mathf.Clamp01(1f - num));
						}
					}
					catch (Exception ex)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:ref-fill", ex);
					}
					if (ContainerWindowResizer.IsDragging)
					{
						continue;
					}
					try
					{
						Component val2 = ((Il2CppObjectBase)val).TryCast<Component>();
						if (!((Object)(object)((val2 != null) ? val2.gameObject : null) != (Object)null))
						{
							continue;
						}
						Il2CppArrayBase<Graphic> componentsInChildren = val2.gameObject.GetComponentsInChildren<Graphic>(true);
						if (componentsInChildren == null)
						{
							continue;
						}
						for (int j = 0; j < componentsInChildren.Count; j++)
						{
							if ((Object)(object)componentsInChildren[j] != (Object)null)
							{
								componentsInChildren[j].SetAllDirty();
							}
						}
					}
					catch (Exception ex2)
					{
						CatchLogOnce.Warn("DiyUpgradeMod.cs:ref-dirty", ex2);
					}
				}
			}
			catch (Exception ex3)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:ref-all", ex3);
			}
		}

		internal static void SetItemTag(GameItem item, string tag, int value)
		{
			try
			{
				if (item != null && (!item.IsTag(tag) || SafeTagInt(item, tag, int.MinValue) != value))
				{
					item.EnableTag(tag, true);
					Action<TagState> val = DelegateSupport.ConvertDelegate<Action<TagState>>((Delegate)(Action<TagState>)((TagState st) =>
					{
						st.SetInt(value);
					}));
					item.ModifyTag(tag, val, true);
				}
			}
			catch (Exception ex)
			{
				if (_log != null)
				{
					_log.Error("[容器升级] SetTag 失败 " + tag + ": " + ex.Message);
				}
			}
		}

		internal static int SafeTagInt(GameItem item, string tag, int fallback)
		{
			if (item == null)
			{
				return fallback;
			}
			try
			{
				if (item.state != null)
				{
					TagState tag2 = item.state.GetTag(tag);
					if (tag2 != null)
					{
						return tag2.GetInt();
					}
				}
			}
			catch
			{
			}
			try
			{
				TagState tagReadonly = item.GetTagReadonly(tag);
				if (tagReadonly != null)
				{
					return tagReadonly.GetInt();
				}
			}
			catch
			{
			}
			return fallback;
		}

		private static int SafeTagInt(GameItem item, string tag)
		{
			return SafeTagInt(item, tag, 0);
		}

		private static void ConsumeMaterial(GameItem material)
		{
			int num = material.unitCount - 1;
			if (num <= 0)
			{
				material.Destroy();
			}
			else
			{
				material.SetUnitCount(num);
			}
		}

		private static void PlayUpgradeSound()
		{
			try
			{
				EmporiumAudioPlayer instance = EmporiumAudioPlayer.Instance;
				if ((Object)(object)instance != (Object)null && (Object)(object)instance.ratchet != (Object)null)
				{
					instance.ratchet.Play();
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2660", ex);
			}
		}

		private static void CollectGgisFromWindow(PixelWindow win, List<GameGridInventory> outList)
		{
			if (win != null)
			{
				CollectWindowTree(win, 0, new HashSet<long>(), new HashSet<long>(), outList);
			}
		}

		private static void CollectWindowTree(object node, int depth, HashSet<long> seenWins, HashSet<long> seenGgis, List<GameGridInventory> outList)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected Obj, but got Unknown
			if (node == null || depth > 3)
			{
				return;
			}
			try
			{
				Il2CppObjectBase val = (Il2CppObjectBase)((node is Il2CppObjectBase) ? node : null);
				if (val == null)
				{
					return;
				}
				PixelWindow val2 = val.TryCast<PixelWindow>();
				if (val2 != null)
				{
					long num = ((Il2CppObjectBase)val2).Pointer.ToInt64();
					if (num == 0L || !seenWins.Add(num))
					{
						return;
					}
					CollectGgiRec(val2.childElement, 0, seenGgis, outList);
					List<GraphNodeStorage> children = val2.children;
					List<GraphNodeStorage> list = new List<GraphNodeStorage>();
					if (children != null)
					{
						for (int i = 0; i < children.Count; i++)
						{
							list.Add(children[i]);
						}
					}
					if (list != null)
					{
						for (int j = 0; j < list.Count; j++)
						{
							CollectWindowTree(list[j], depth + 1, seenWins, seenGgis, outList);
						}
					}
					return;
				}
				GridPixelElement val3 = val.TryCast<GridPixelElement>();
				if (val3 != null)
				{
					CollectGgiRec(val3, 1, seenGgis, outList);
					return;
				}
				GameGridInventory val4 = val.TryCast<GameGridInventory>();
				if (val4 != null)
				{
					CollectGgiRec(val4, 1, seenGgis, outList);
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2699", ex);
			}
		}

		private static void CollectGgiRec(object el, int depth, HashSet<long> seen, List<GameGridInventory> outList)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected Obj, but got Unknown
			if (el == null || depth > 2)
			{
				return;
			}
			try
			{
				Il2CppObjectBase val = (Il2CppObjectBase)((el is Il2CppObjectBase) ? el : null);
				if (val == null)
				{
					return;
				}
				GameGridInventory val2 = val.TryCast<GameGridInventory>();
				if (val2 != null)
				{
					long item = ((Il2CppObjectBase)val2).Pointer.ToInt64();
					if (seen.Add(item))
					{
						outList.Add(val2);
					}
					return;
				}
				GridPixelElement val3 = val.TryCast<GridPixelElement>();
				if (val3 == null)
				{
					return;
				}
				int num = 16;
				int num2 = 16;
				try
				{
					int gridWidth = val3.gridWidth;
					int gridHeight = val3.gridHeight;
					if (gridWidth > 0 && gridWidth <= 128)
					{
						num = gridWidth;
					}
					if (gridHeight > 0 && gridHeight <= 128)
					{
						num2 = gridHeight;
					}
				}
				catch (Exception ex)
				{
					CatchLogOnce.Warn("DiyUpgradeMod.cs:2727", ex);
				}
				for (int i = 0; i < num2; i++)
				{
					for (int j = 0; j < num; j++)
					{
						PixelElement val4 = null;
						try
						{
							val4 = val3.GetElement(j, i);
						}
						catch (Exception ex2)
						{
							CatchLogOnce.Warn("DiyUpgradeMod.cs:2733", ex2);
						}
						if (val4 != null)
						{
							CollectGgiRec(val4, depth + 1, seen, outList);
						}
					}
				}
			}
			catch (Exception ex3)
			{
				CatchLogOnce.Warn("DiyUpgradeMod.cs:2738", ex3);
			}
		}

		static DiyUpgradeMod()
		{
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	internal static class SkipWageUpgradeRestore
	{
		private static bool _installed;

		internal static void Install(Harmony harmony)
		{
			if (_installed)
			{
				return;
			}
			_installed = true;
			Type type = DiyUpgradeMod.FindTypeCached("JacksonPerks.ContainerUpgradeV2");
			Type type2 = DiyUpgradeMod.FindTypeCached("JacksonPerks.LuckScoutBackpackUpgrade");
			int num = 0;
			int num2 = 0;
			if (type != null)
			{
				if (Patch(harmony, type, "PrefixTarget_WageBox", new Type[2]
				{
					typeof(GameItem),
					typeof(GameItem)
				}, "Prefix_WageBox_Target"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (Patch(harmony, type, "RestoreWageBoxShape", new Type[1] { typeof(GameItem) }, "Prefix_RestoreWageBoxShape"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (Patch(harmony, type, "ConsumeNutsInBox", new Type[1] { typeof(GameItem) }, "Prefix_ConsumeNutsInBox"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (Patch(harmony, type, "PostfixWageBoxTooltip", new Type[2]
				{
					typeof(RichTextBuilder),
					typeof(GameItem)
				}, "Prefix_PostfixWageBoxTooltip"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (Patch(harmony, type, "ConsumeNutsDirectly", new Type[2]
				{
					typeof(GameItem),
					typeof(GameItem)
				}, "Prefix_ConsumeNutsDirectly"))
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
			else
			{
				MelonLogger.Msg("[自定义容器升级] 未检测到 JacksonPerks.ContainerUpgradeV2，跳过妙妙箱避让");
			}
			if (type2 != null)
			{
				if (Patch(harmony, type2, "ApplyLockedShape", new Type[2]
				{
					typeof(GameGridInventory),
					typeof(int)
				}, "Prefix_ApplyLockedShape"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (Patch(harmony, type2, "DoUpgrade", new Type[2]
				{
					typeof(GameItem),
					typeof(GameItem)
				}, "Prefix_DoUpgrade"))
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
			else
			{
				MelonLogger.Msg("[自定义容器升级] 未检测到 JacksonPerks.LuckScoutBackpackUpgrade，跳过虚空珠避让");
			}
			if (num + num2 > 0)
			{
				MelonLogger.Msg($"[自定义容器升级] Wage 避让：{num}/{num + num2} 成功");
			}
		}

		private static bool Patch(Harmony harmony, Type type, string methodName, Type[] parameters, string prefixName)
		{
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(type, methodName, parameters, (Type[])null);
				if (methodInfo == null)
				{
					MelonLogger.Warning($"[自定义容器升级] 找不到 {type.Name}.{methodName}，跳过");
					return false;
				}
				MethodInfo method = typeof(SkipWageUpgradeRestore).GetMethod(prefixName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (method == null)
				{
					MelonLogger.Warning("[自定义容器升级] 找不到 Prefix " + prefixName);
					return false;
				}
				harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				return true;
			}
			catch (Exception ex)
			{
				MelonLogger.Warning($"[自定义容器升级] 避让 {type.Name}.{methodName} 失败：{ex.Message}");
				return false;
			}
		}

		public static bool Prefix_WageBox_Target(GameItem __0, GameItem __1)
		{
			GameItem val = null;
			if (WageContainerRegistry.IsWageBox(__1))
			{
				val = __1;
			}
			else if (WageContainerRegistry.IsWageBox(__0))
			{
				val = __0;
			}
			if (val == null)
			{
				return true;
			}
			if (!WageContainerRegistry.HasDiyMigrated(val))
			{
				return true;
			}
			return false;
		}

		public static bool Prefix_RestoreWageBoxShape(GameItem __0)
		{
			if (__0 == null)
			{
				return true;
			}
			if (!WageContainerRegistry.IsWageBox(__0))
			{
				return true;
			}
			if (!WageContainerRegistry.HasDiyMigrated(__0))
			{
				return true;
			}
			return false;
		}

		public static bool Prefix_ConsumeNutsInBox(GameItem __0)
		{
			if (__0 == null)
			{
				return true;
			}
			if (!WageContainerRegistry.IsWageBox(__0))
			{
				return true;
			}
			if (!WageContainerRegistry.HasDiyMigrated(__0))
			{
				return true;
			}
			return false;
		}

		public static bool Prefix_PostfixWageBoxTooltip(RichTextBuilder __0, GameItem __1)
		{
			if (__1 == null)
			{
				return true;
			}
			if (!WageContainerRegistry.IsWageBox(__1))
			{
				return true;
			}
			if (!WageContainerRegistry.HasDiyMigrated(__1))
			{
				return true;
			}
			return false;
		}

		public static bool Prefix_ConsumeNutsDirectly(GameItem __0, GameItem __1)
		{
			GameItem val = null;
			if (WageContainerRegistry.IsWageBox(__0))
			{
				val = __0;
			}
			else if (WageContainerRegistry.IsWageBox(__1))
			{
				val = __1;
			}
			if (val == null)
			{
				return true;
			}
			if (!WageContainerRegistry.HasDiyMigrated(val))
			{
				return true;
			}
			return false;
		}

		public static bool Prefix_ApplyLockedShape(GameGridInventory __0, int __1)
		{
			if (__0 == null)
			{
				return true;
			}
			try
			{
				if (WageContainerRegistry.IsDiyVoidBeadGrid(((Il2CppObjectBase)__0).Pointer))
				{
					return false;
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("SkipWageUpgradeRestore.cs:173", ex);
			}
			return true;
		}

		public static bool Prefix_DoUpgrade(GameItem __0, GameItem __1)
		{
			GameItem val = null;
			if (WageContainerRegistry.IsVoidBeadStorage(__1))
			{
				val = __1;
			}
			else if (WageContainerRegistry.IsVoidBeadStorage(__0))
			{
				val = __0;
			}
			if (val == null)
			{
				return true;
			}
			if (!WageContainerRegistry.HasDiyMigrated(val))
			{
				return true;
			}
			return false;
		}

		public static void ClearWageRestoreState()
		{
			try
			{
				Type type = DiyUpgradeMod.FindTypeCached("JacksonPerks.LuckScoutBackpackUpgrade");
				if (!(type == null))
				{
					FieldInfo fieldInfo = AccessTools.Field(type, "_restoreFramesLeft");
					if (fieldInfo != null)
					{
						fieldInfo.SetValue(null, 0);
					}
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("SkipWageUpgradeRestore.cs:205", ex);
			}
		}
	}
	internal static class SkipXiaowoBayRestore
	{
		private static bool _installed;

		internal static void Install(Harmony harmony)
		{
			if (_installed)
			{
				return;
			}
			_installed = true;
			Type type = DiyUpgradeMod.FindTypeCached("XIAOWOTradePerks.BayExpansionPerk");
			Type type2 = DiyUpgradeMod.FindTypeCached("XIAOWOTradePerks.FridgeExpansion");
			if (type == null && type2 == null)
			{
				MelonLogger.Msg("[自定义容器升级] 未检测到 XIAOWOTradePerks，跳过避让");
				return;
			}
			int num = 0;
			int num2 = 0;
			if (type != null)
			{
				if (PatchMethod(harmony, type, "RestoreExpandedItem", "Prefix_Void_Item"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (PatchMethod(harmony, type, "TryRestoreRememberedGridShape", "Prefix_Bool"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (PatchMethod(harmony, type, "ApplyStoredShape", "Prefix_Bool"))
				{
					num++;
				}
				else
				{
					num2++;
				}
				if (PatchMethod(harmony, type, "IsResizableContainer", "Prefix_IsResizableContainer"))
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
			if (type2 != null)
			{
				if (PatchMethod(harmony, type2, "Restore", "Prefix_Void_Item"))
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
			if (num2 == 0)
			{
				MelonLogger.Msg($"[自定义容器升级] 已避让 XIAOWOTradePerks ({num}/{num})");
			}
			else
			{
				MelonLogger.Warning($"[自定义容器升级] 已避让 XIAOWOTradePerks：成功 {num}，失败 {num2}");
			}
		}

		private static bool PatchMethod(Harmony harmony, Type type, string name, string prefixName)
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(type, name, (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					MelonLogger.Warning($"[自定义容器升级] 找不到 {type.Name}.{name}，跳过");
					return false;
				}
				HarmonyMethod val = new HarmonyMethod(typeof(SkipXiaowoBayRestore).GetMethod(prefixName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic));
				harmony.Patch((MethodBase)methodInfo, val, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				return true;
			}
			catch (Exception ex)
			{
				MelonLogger.Warning($"[自定义容器升级] 避让 {type.Name}.{name} 失败：{ex.Message}");
				return false;
			}
		}

		public static bool Prefix_Void_Item(GameItem __0)
		{
			return !ShouldSkip(__0);
		}

		public static bool Prefix_IsResizableContainer(GameItem __0, ref bool __result)
		{
			if (__0 == null)
			{
				return true;
			}
			try
			{
				if (DiyUpgradeMod.IsResizableContainer(__0))
				{
					__result = false;
					return false;
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("SkipXiaowoBayRestore.cs:125", ex);
			}
			return true;
		}

		public static bool Prefix_Bool(GameItem __0, ref bool __result)
		{
			if (ShouldSkip(__0))
			{
				__result = false;
				return false;
			}
			return true;
		}

		private static bool ShouldSkip(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				if (DiyUpgradeMod.IsResizableContainer(item))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("SkipXiaowoBayRestore.cs:154", ex);
			}
			return false;
		}
	}
	internal static class WageContainerRegistry
	{
		public const string IdWageBox = "custom_storage_box";

		public const string IdVoidBead = "void_bead_storage";

		public const string IdGuMachine = "wage_gu_machine";

		public const string IdAiGenerator = "wage_ai_generator";

		public const string TagDiyMigrated = "DIY_MIGRATED_TAG";

		public const string TagWbStage = "wb_stage";

		public const string TagVoidBeadSlots = "VOID_BEAD_SLOTS_TAG";

		public const int WageBoxMaxStage = 5;

		public const int VoidBeadMaxSlots = 200;

		public static DiyUpgradeMod.GridDef _wageBox;

		public static DiyUpgradeMod.GridDef _wageBackpack;

		public static DiyUpgradeMod.GridDef _modContainer;

		private static readonly HashSet<IntPtr> _diyVoidBeadGrids = new HashSet<IntPtr>();

		internal static string NormalizeId(GameItem item)
		{
			if (item == null)
			{
				return "";
			}
			try
			{
				return (item.identifier ?? "").Replace("\0", "").Trim().ToLowerInvariant();
			}
			catch
			{
				return "";
			}
		}

		public static bool IsDiyVoidBeadGrid(IntPtr ptr)
		{
			if (ptr != IntPtr.Zero)
			{
				return _diyVoidBeadGrids.Contains(ptr);
			}
			return false;
		}

		public static void RegisterVoidBeadGrid(GameGridInventory grid)
		{
			if (grid == null)
			{
				return;
			}
			try
			{
				_diyVoidBeadGrids.Add(((Il2CppObjectBase)grid).Pointer);
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("WageContainerRegistry.cs:46", ex);
			}
		}

		public static void ClearVoidBeadGrids()
		{
			_diyVoidBeadGrids.Clear();
		}

		public static void RegisterWageDefinitions(MelonPreferences_Category cat)
		{
			_wageBox = DiyUpgradeMod.CreateGridDef(cat, "WageBox", "蛙哥妙妙箱", "custom_storage_box", 1, 52, 30, 1, null, "蛙哥妙妙箱（WageMod）");
			_wageBackpack = DiyUpgradeMod.CreateGridDef(cat, "WageBackpack", "虚空珠储物袋", "void_bead_storage", 1, 34, 10, 1, null, "虚空珠储物袋（WageMod）");
			_modContainer = DiyUpgradeMod.CreateGridDef(cat, "ModContainer", "Mod容器", "wage_gu_machine,wage_ai_generator", 1, 20, 20, 1, null, "WageMod 机器容器（养蛊机 / AI 生成器）");
		}

		public static bool IsWageContainer(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			string text = NormalizeId(item);
			switch (text)
			{
			default:
				return text == "wage_ai_generator";
			case "custom_storage_box":
			case "void_bead_storage":
			case "wage_gu_machine":
				return true;
			}
		}

		public static bool IsWageBox(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			return NormalizeId(item) == "custom_storage_box";
		}

		public static bool IsVoidBeadStorage(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			return NormalizeId(item) == "void_bead_storage";
		}

		public static bool HasDiyMigrated(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				return item.IsTag("DIY_MIGRATED_TAG");
			}
			catch
			{
				return false;
			}
		}

		public static DiyUpgradeMod.Cat GetWageCategory(GameItem item)
		{
			if (item == null)
			{
				return DiyUpgradeMod.Cat.None;
			}
			switch (NormalizeId(item))
			{
			case "custom_storage_box":
				if (HasDiyMigrated(item))
				{
					return DiyUpgradeMod.Cat.WageBox;
				}
				if (DiyUpgradeMod.SafeTagInt(item, "wb_stage", 0) < 5)
				{
					return DiyUpgradeMod.Cat.None;
				}
				return DiyUpgradeMod.Cat.WageBox;
			case "void_bead_storage":
				if (HasDiyMigrated(item))
				{
					return DiyUpgradeMod.Cat.WageBackpack;
				}
				if (DiyUpgradeMod.SafeTagInt(item, "VOID_BEAD_SLOTS_TAG", 0) < 200)
				{
					return DiyUpgradeMod.Cat.None;
				}
				return DiyUpgradeMod.Cat.WageBackpack;
			case "wage_gu_machine":
			case "wage_ai_generator":
				return DiyUpgradeMod.Cat.ModContainer;
			default:
				return DiyUpgradeMod.Cat.None;
			}
		}

		public static DiyUpgradeMod.GridDef GetWageGridDef(GameItem item)
		{
			return GetWageCategory(item) switch
			{
				DiyUpgradeMod.Cat.WageBox => _wageBox, 
				DiyUpgradeMod.Cat.WageBackpack => _wageBackpack, 
				DiyUpgradeMod.Cat.ModContainer => _modContainer, 
				_ => null, 
			};
		}

		public static void OnVoidBeadUpgraded(GameItem item, GameGridInventory grid, int newW, int newH)
		{
			if (item == null || grid == null)
			{
				return;
			}
			RegisterVoidBeadGrid(grid);
			try
			{
				DiyUpgradeMod.RememberGridShape(item, newW, newH);
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("WageContainerRegistry.cs:141", ex);
			}
			try
			{
				DiyUpgradeMod.SetItemTag(item, "DIY_MIGRATED_TAG", 1);
			}
			catch (Exception ex2)
			{
				CatchLogOnce.Warn("WageContainerRegistry.cs:142", ex2);
			}
		}

		public static void MigrateLegacyWageBox(GameItem item, GameGridInventory grid)
		{
			if (item == null || grid == null || grid.inventoryShape == null || !IsWageBox(item) || HasDiyMigrated(item))
			{
				return;
			}
			int num = 0;
			try
			{
				num = DiyUpgradeMod.SafeTagInt(item, "wb_stage", 0);
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("WageContainerRegistry.cs:156", ex);
			}
			if (num < 5)
			{
				return;
			}
			int width = grid.inventoryShape.width;
			int height = grid.inventoryShape.height;
			if (width <= 0 || height <= 0)
			{
				return;
			}
			DiyUpgradeMod.SaveMeta(item, width, height, 0);
			DiyUpgradeMod.SetItemTag(item, "DIY_MIGRATED_TAG", 1);
			DiyUpgradeMod.RememberGridShape(item, width, height);
			MelonLogger.Msg($"[自定义容器升级] 蛙哥妙妙箱旧档迁移：基准 {width}x{height}（wb_stage={num}），切换到 DIY 升级");
			try
			{
				DiyUpgradeMod.ClearReapplyCache(item);
			}
			catch (Exception ex2)
			{
				CatchLogOnce.Warn("WageContainerRegistry.cs:167", ex2);
			}
		}

		public static void MigrateLegacyVoidBead(GameItem item, GameGridInventory grid)
		{
			if (item == null || grid == null || grid.inventoryShape == null || !IsVoidBeadStorage(item) || HasDiyMigrated(item))
			{
				return;
			}
			int num = 0;
			try
			{
				num = DiyUpgradeMod.SafeTagInt(item, "VOID_BEAD_SLOTS_TAG", 0);
			}
			catch (Exception ex)
			{
				CatchLogOnce.Warn("WageContainerRegistry.cs:180", ex);
			}
			if (num < 200)
			{
				return;
			}
			int width = grid.inventoryShape.width;
			int height = grid.inventoryShape.height;
			if (width <= 0 || height <= 0)
			{
				return;
			}
			DiyUpgradeMod.SaveMeta(item, width, height, 0);
			DiyUpgradeMod.SetItemTag(item, "DIY_MIGRATED_TAG", 1);
			DiyUpgradeMod.RememberGridShape(item, width, height);
			RegisterVoidBeadGrid(grid);
			MelonLogger.Msg($"[自定义容器升级] 虚空珠旧档迁移：基准 {width}x{height}（slots={num}），切换到 DIY 升级");
			try
			{
				DiyUpgradeMod.ClearReapplyCache(item);
			}
			catch (Exception ex2)
			{
				CatchLogOnce.Warn("WageContainerRegistry.cs:192", ex2);
			}
		}
	}
	internal static class WindowShowHook
	{
		private static bool _installed;

		internal static void Install(Harmony harmony)
		{
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Expected Obj, but got Unknown
			if (_installed)
			{
				return;
			}
			_installed = true;
			try
			{
				Type typeFromHandle = typeof(PixelWindow);
				if (typeFromHandle == null)
				{
					MelonLogger.Warning("[自定义容器升级] 找不到 PixelWindow，跳过 Show Hook");
					return;
				}
				MethodInfo method = typeof(WindowShowHook).GetMethod("ShowPostfix", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				if (method == null)
				{
					MelonLogger.Warning("[自定义容器升级] 找不到 ShowPostfix，跳过 Show Hook");
					return;
				}
				int num = 0;
				List<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(typeFromHandle);
				if (declaredMethods != null)
				{
					foreach (MethodInfo item in declaredMethods)
					{
						if (!(item == null) && !(item.Name != "Show"))
						{
							try
							{
								harmony.Patch((MethodBase)item, (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
								num++;
							}
							catch (Exception ex)
							{
								MelonLogger.Warning("[自定义容器升级] Hook PixelWindow.Show(" + ParamSig(item) + ") 失败：" + ex.Message);
							}
						}
					}
				}
				if (num == 0)
				{
					MelonLogger.Warning("[自定义容器升级] 未找到任何 PixelWindow.Show 重载，恢复会失效");
				}
				else
				{
					MelonLogger.Msg("[自定义容器升级] PixelWindow.Show Hook 完成 (" + num + " 个重载)");
				}
			}
			catch (Exception ex2)
			{
				MelonLogger.Warning("[自定义容器升级] WindowShowHook.Install 异常：" + ex2.Message);
			}
		}

		private static string ParamSig(MethodInfo m)
		{
			try
			{
				ParameterInfo[] parameters = m.GetParameters();
				if (parameters == null || parameters.Length == 0)
				{
					return "";
				}
				string text = "";
				for (int i = 0; i < parameters.Length; i++)
				{
					if (i > 0)
					{
						text += ",";
					}
					text += parameters[i].ParameterType?.Name ?? "?";
				}
				return text;
			}
			catch
			{
				return "?";
			}
		}

		public static void ShowPostfix(PixelWindow __instance)
		{
			ScheduleDelayedRestore(__instance, "Show");
		}

		private static void ScheduleDelayedRestore(PixelWindow win, string src)
		{
			if (win == null)
			{
				return;
			}
			try
			{
				MelonCoroutines.Start(DelayedRestore(win, src));
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("[自定义容器升级] ScheduleDelayedRestore 异常：" + ex.Message);
			}
		}

		private static IEnumerator DelayedRestore(PixelWindow win, string src)
		{
			yield return null;
			yield return null;
			if (win == null)
			{
				yield break;
			}
			GameItem val = null;
			try
			{
				List<GameItem> parentItems = win.parentItems;
				if (parentItems != null && parentItems.Count > 0)
				{
					for (int i = 0; i < parentItems.Count; i++)
					{
						GameItem val2 = parentItems[i];
						if (val2 != null && DiyUpgradeMod.IsResizableContainer(val2))
						{
							val = val2;
							break;
						}
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("[自定义容器升级] 取 parentItems 失败：" + ex.Message);
				yield break;
			}
			if (val == null)
			{
				yield break;
			}
			try
			{
				DiyUpgradeMod.ClearReapplyCache(val);
				DiyUpgradeMod.ReapplyFromMeta(val);
			}
			catch (Exception ex2)
			{
				MelonLogger.Warning("[自定义容器升级] Show 后恢复 " + DiyUpgradeMod.IdOf(val) + " 异常：" + ex2.Message);
			}
		}
	}
}
