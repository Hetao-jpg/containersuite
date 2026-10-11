using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using SaveBagExpand;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(SaveBagExpandCore), "自定义仓库", "1.0.1", "核桃", null)]
[assembly: MelonGame(null, "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.1.0")]
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
namespace SaveBagExpand
{
	public sealed class SaveBagExpandCore : MelonMod
	{
		internal static Instance Log;

		internal static bool Enabled = true;

		private const float CellPx = 16f;

		private const float RectScale = 2f;

		public override void OnInitializeMelon()
		{
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			Log = ((MelonBase)this).LoggerInstance;
			SaveBagExpandConfig.Init();
			Enabled = SaveBagExpandConfig.EnableMod.Value;
			Log.Msg("================================================");
			Log.Msg("[SaveBagExpand] 存档容器扩容 Mod 已加载");
			Log.Msg("[SaveBagExpand] 当前状态: " + (Enabled ? "已启用" : "已禁用"));
			Log.Msg("================================================");
			try
			{
				new Harmony("SaveBagExpand.Patch").PatchAll();
			}
			catch (Exception ex)
			{
				Log.Error("[SaveBagExpand] 挂载 Harmony 补丁时发生异常: " + ex);
			}
		}

		public override void OnUpdate()
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (!Enabled || !SaveBagExpandConfig.ScrollEnabled.Value)
				{
					return;
				}
				EmporiumEntry instance = EmporiumEntry.Instance;
				if (!((Object)(object)instance == (Object)null))
				{
					float y = Input.mouseScrollDelta.y;
					if (!(Mathf.Abs(y) < 0.01f) && !TryScroll(instance.invWindow, instance.invElement, y, isPersistent: true, 25, 10, 2f, 2f) && !TryScroll(instance.showcaseWindow, instance.showcaseElement, y, isPersistent: true, 0, 0, 2f, 2f) && !TryScroll(instance.backInvWindow, instance.backInvinvElement, y, isPersistent: true, 20, 4, 2f, 2f) && !TryScroll(instance.hiddenWindow, instance.hiddenElement, y, isPersistent: false, 0, 0, 2f, 2f))
					{
						TryScroll(instance.frontInvWindow, instance.frontInvinvElement, y, isPersistent: false, 20, 6, 2f, 2f);
					}
				}
			}
			catch (Exception value)
			{
				Log.Error($"[SaveBagExpand] 滚轮异常: {value}");
			}
		}

		private static bool TryScroll(PixelWindow win, GameGridInventory grid, float scroll, bool isPersistent, int reqCols, int reqRows, float scaleX, float scaleY)
		{
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			if (win == null || grid == null)
			{
				return false;
			}
			if (!isPersistent)
			{
				try
				{
					if (!win.IsVisible())
					{
						return false;
					}
				}
				catch
				{
					return false;
				}
			}
			if (!IsMouseOver(win, grid, reqCols, reqRows, scaleX, scaleY))
			{
				return false;
			}
			TreeNodeRender inner = win.inner;
			if ((Object)(object)inner == (Object)null)
			{
				return false;
			}
			RectTransform rectTransform = inner.rectTransform;
			if ((Object)(object)rectTransform == (Object)null)
			{
				return false;
			}
			int num = 0;
			try
			{
				GridShape inventoryShape = grid.inventoryShape;
				if (inventoryShape != null)
				{
					num = inventoryShape.height;
				}
			}
			catch
			{
			}
			if (num <= 0)
			{
				return false;
			}
			int num2 = SaveBagExpandConfig.ScrollVisibleRows.Value;
			if (num2 < 0)
			{
				num2 = 0;
			}
			if (num2 > 50)
			{
				num2 = 50;
			}
			int num3 = Mathf.Max(0, num - 1);
			if (num2 > num3)
			{
				num2 = num3;
			}
			float num4 = (float)num2 * 16f;
			int num5 = SaveBagExpandConfig.ScrollUpRows.Value;
			if (num5 < 0)
			{
				num5 = 0;
			}
			if (num5 > 50)
			{
				num5 = 50;
			}
			float num6 = (float)num5 * 16f;
			Vector2 anchoredPosition = rectTransform.anchoredPosition;
			anchoredPosition.y += scroll * 16f;
			if (anchoredPosition.y > num6)
			{
				anchoredPosition.y = num6;
			}
			if (anchoredPosition.y < 0f - num4)
			{
				anchoredPosition.y = 0f - num4;
			}
			rectTransform.anchoredPosition = anchoredPosition;
			return true;
		}

		private static bool IsMouseOver(PixelWindow win, GameGridInventory grid, int reqCols, int reqRows, float scaleX, float scaleY)
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				RectTransform rectTransform = win.rectTransform;
				if ((Object)(object)rectTransform == (Object)null)
				{
					return false;
				}
				Il2CppStructArray<Vector3> val = new Il2CppStructArray<Vector3>(4L);
				rectTransform.GetWorldCorners(val);
				float x = ((Il2CppArrayBase<Vector3>)(object)val)[0].x;
				float y = ((Il2CppArrayBase<Vector3>)(object)val)[0].y;
				int num = reqCols;
				int num2 = reqRows;
				if (num <= 0 || num2 <= 0)
				{
					try
					{
						GridShape inventoryShape = grid.inventoryShape;
						if (inventoryShape != null)
						{
							num = inventoryShape.width;
							num2 = inventoryShape.height;
						}
					}
					catch
					{
					}
				}
				if (num <= 0 || num2 <= 0)
				{
					return false;
				}
				float num3 = (float)num * 16f * scaleX;
				float num4 = (float)num2 * 16f * scaleY;
				float num5 = x + num3;
				float num6 = y - num4;
				Vector3 mousePosition = Input.mousePosition;
				return mousePosition.x >= x && mousePosition.x <= num5 && mousePosition.y >= num6 && mousePosition.y <= y;
			}
			catch
			{
				return false;
			}
		}
	}
	internal static class SaveBagExpandConfig
	{
		internal static MelonPreferences_Entry<bool> EnableMod;

		internal static MelonPreferences_Entry<bool> ScrollEnabled;

		internal static MelonPreferences_Entry<int> ScrollVisibleRows;

		internal static MelonPreferences_Entry<int> ScrollUpRows;

		internal static MelonPreferences_Entry<bool> SaveBagEnable;

		internal static MelonPreferences_Entry<int> SaveBagWidth;

		internal static MelonPreferences_Entry<int> SaveBagHeight;

		internal static MelonPreferences_Entry<bool> ShowcaseInvEnable;

		internal static MelonPreferences_Entry<int> ShowcaseInvWidth;

		internal static MelonPreferences_Entry<int> ShowcaseInvHeight;

		internal static MelonPreferences_Entry<bool> HiddenInvEnable;

		internal static MelonPreferences_Entry<int> HiddenInvWidth;

		internal static MelonPreferences_Entry<int> HiddenInvHeight;

		internal static MelonPreferences_Entry<bool> BackInvEnable;

		internal static MelonPreferences_Entry<int> BackInvWidth;

		internal static MelonPreferences_Entry<int> BackInvHeight;

		internal static MelonPreferences_Entry<bool> FrontInvEnable;

		internal static MelonPreferences_Entry<int> FrontInvWidth;

		internal static MelonPreferences_Entry<int> FrontInvHeight;

		internal static void Init()
		{
			MelonPreferences_Category val = MelonPreferences.CreateCategory("SaveBagExpand");
			string text = new string('—', 20);
			EnableMod = val.CreateEntry<bool>("EnableMod", true, text + "【全局】总开关", "【全局】true = 启用本 Mod；false = 完全禁用。", false, false, (ValueValidator)null, (string)null);
			ScrollEnabled = val.CreateEntry<bool>("ScrollEnabled", true, text + "【滚动】滚轮启用", "false = 鼠标指到容器上时可用滚轮上下滚动内容；false = 关闭。", false, false, (ValueValidator)null, (string)null);
			ScrollVisibleRows = val.CreateEntry<int>("ScrollVisibleRows", 10, text + "【滚动】向下滚动行数", "滚轮向下最多能滚几行，可设 0-50。想看完整个大仓库就调大，比如 10-20。", false, false, (ValueValidator)null, (string)null);
			ScrollUpRows = val.CreateEntry<int>("ScrollUpRows", 10, text + "【滚动】向上滚动行数", "滚轮向上最多能滚几行，可设 0-50。0 = 不允许向上滚。", false, false, (ValueValidator)null, (string)null);
			SaveBagEnable = val.CreateEntry<bool>("SaveBagEnable", false, text + "【主仓库】启用", "true = 应用本容器的自定义尺寸；false = 跟随游戏原版（推荐，游戏更新后不受旧值影响）。", false, false, (ValueValidator)null, (string)null);
			SaveBagWidth = val.CreateEntry<int>("SaveBagWidth", 24, text + "【主仓库】宽度", "【主仓库 / SaveBag】宽度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			SaveBagHeight = val.CreateEntry<int>("SaveBagHeight", 10, text + "【主仓库】高度", "【主仓库 / SaveBag】高度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			ShowcaseInvEnable = val.CreateEntry<bool>("ShowcaseInvEnable", false, text + "【展示柜】启用", "true = 应用本容器的自定义尺寸；false = 跟随游戏原版（推荐，游戏更新后不受旧值影响）。", false, false, (ValueValidator)null, (string)null);
			ShowcaseInvWidth = val.CreateEntry<int>("ShowcaseInvWidth", 9, text + "【展示柜】宽度", "【展示柜 / ShowcaseInvSaveBag】宽度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			ShowcaseInvHeight = val.CreateEntry<int>("ShowcaseInvHeight", 7, text + "【展示柜】高度", "【展示柜 / ShowcaseInvSaveBag】高度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			HiddenInvEnable = val.CreateEntry<bool>("HiddenInvEnable", false, text + "【海报暗格】启用", "true = 应用本容器的自定义尺寸；false = 跟随游戏原版（推荐，游戏更新后不受旧值影响）。", false, false, (ValueValidator)null, (string)null);
			HiddenInvWidth = val.CreateEntry<int>("HiddenInvWidth", 2, text + "【海报暗格】宽度", "【海报暗格 / HiddenInvSaveBag】宽度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			HiddenInvHeight = val.CreateEntry<int>("HiddenInvHeight", 2, text + "【海报暗格】高度", "【海报暗格 / HiddenInvSaveBag】高度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			BackInvEnable = val.CreateEntry<bool>("BackInvEnable", false, text + "【柜台】启用", "true = 应用本容器的自定义尺寸；false = 跟随游戏原版（推荐，游戏更新后不受旧值影响）。", false, false, (ValueValidator)null, (string)null);
			BackInvWidth = val.CreateEntry<int>("BackInvWidth", 18, text + "【柜台】宽度", "【柜台 / BackInvSaveBag】宽度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			BackInvHeight = val.CreateEntry<int>("BackInvHeight", 4, text + "【柜台】高度", "【柜台 / BackInvSaveBag】高度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			FrontInvEnable = val.CreateEntry<bool>("FrontInvEnable", false, text + "【客户柜台】启用", "true = 应用本容器的自定义尺寸；false = 跟随游戏原版（推荐，游戏更新后不受旧值影响）。", false, false, (ValueValidator)null, (string)null);
			FrontInvWidth = val.CreateEntry<int>("FrontInvWidth", 18, text + "【客户柜台】宽度", "【客户柜台 / FrontInv】宽度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			FrontInvHeight = val.CreateEntry<int>("FrontInvHeight", 4, text + "【客户柜台】高度", "【客户柜台 / FrontInv】高度。（默认值 = 当前游戏原版尺寸；只有当对应容器的「启用」为 true 时才会应用）", false, false, (ValueValidator)null, (string)null);
			val.SaveToFile(true);
		}

		internal static int Clamp(int value)
		{
			if (value <= 0)
			{
				return 0;
			}
			if (value > 50)
			{
				return 50;
			}
			return value;
		}
	}
	[HarmonyPatch(typeof(EmporiumEntry), "Start")]
	internal static class EmporiumEntryStartPatch
	{
		private static void Postfix(EmporiumEntry __instance)
		{
			ApplyAllResize(__instance);
		}

		private static void Resize(GameGridInventory inv, PixelWindow window, string name, int cfgW, int cfgH)
		{
			if (inv == null)
			{
				return;
			}
			int num = SaveBagExpandConfig.Clamp(cfgW);
			int num2 = SaveBagExpandConfig.Clamp(cfgH);
			if (num == 0 && num2 == 0)
			{
				return;
			}
			try
			{
				inv.SetShape(num, num2);
				((GameInventory)inv).Validate();
				if (window != null)
				{
					window.Validate();
				}
				SaveBagExpandCore.Log.Msg($"[SaveBagExpand] {name} 已改为 {num}x{num2}");
			}
			catch (Exception value)
			{
				SaveBagExpandCore.Log.Error($"[SaveBagExpand] 修改 {name} 失败: {value}");
			}
		}

		internal static void ApplyAllResize(EmporiumEntry __instance)
		{
			if (!SaveBagExpandCore.Enabled || (Object)(object)__instance == (Object)null)
			{
				return;
			}
			if (SaveBagExpandConfig.SaveBagEnable.Value)
			{
				Resize(__instance.invElement, __instance.invWindow, "主仓库", SaveBagExpandConfig.SaveBagWidth.Value, SaveBagExpandConfig.SaveBagHeight.Value);
			}
			if (SaveBagExpandConfig.ShowcaseInvEnable.Value)
			{
				Resize(__instance.showcaseElement, __instance.showcaseWindow, "展示柜", SaveBagExpandConfig.ShowcaseInvWidth.Value, SaveBagExpandConfig.ShowcaseInvHeight.Value);
			}
			if (SaveBagExpandConfig.BackInvEnable.Value)
			{
				Resize(__instance.backInvinvElement, __instance.backInvWindow, "柜台", SaveBagExpandConfig.BackInvWidth.Value, SaveBagExpandConfig.BackInvHeight.Value);
			}
			if (SaveBagExpandConfig.HiddenInvEnable.Value)
			{
				Resize(__instance.hiddenElement, __instance.hiddenWindow, "海报暗格", SaveBagExpandConfig.HiddenInvWidth.Value, SaveBagExpandConfig.HiddenInvHeight.Value);
			}
			if (SaveBagExpandConfig.FrontInvEnable.Value)
			{
				Resize(__instance.frontInvinvElement, __instance.frontInvWindow, "客户柜台", SaveBagExpandConfig.FrontInvWidth.Value, SaveBagExpandConfig.FrontInvHeight.Value);
			}
			try
			{
				__instance.Validate(true);
			}
			catch (Exception value)
			{
				SaveBagExpandCore.Log.Error($"[SaveBagExpand] 调用 EmporiumEntry.Validate 失败: {value}");
			}
		}
	}
	[HarmonyPatch(typeof(StoreShutterButton), "OpenShutter")]
	internal static class StoreShutterOpenPatch
	{
		private static void Postfix()
		{
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if (!((Object)(object)instance == (Object)null))
				{
					EmporiumEntryStartPatch.ApplyAllResize(instance);
				}
			}
			catch (Exception value)
			{
				SaveBagExpandCore.Log.Error($"[SaveBagExpand] 开门后重设尺寸失败: {value}");
			}
		}
	}
	[HarmonyPatch(typeof(EmporiumEntry), "OnArriveStore")]
	internal static class EmporiumEntryArrivePatch
	{
		private static void Postfix(EmporiumEntry __instance)
		{
			EmporiumEntryStartPatch.ApplyAllResize(__instance);
		}
	}
}
