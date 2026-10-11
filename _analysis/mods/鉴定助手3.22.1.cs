using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Il2Cpp;
using Il2CppFebucci.UI;
using Il2CppFebucci.UI.Core;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;
using ProbablyStolenInspector;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: MelonInfo(typeof(InspectorMod), "ProbablyStolenInspector", "3.23.0", "kale", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: AssemblyVersion("0.0.0.0")]
namespace ProbablyStolenInspector;

public static class ModInfo
{
	public const string Name = "ProbablyStolenInspector";

	public const string Version = "3.23.0";

	public const string Author = "kale";

	public const string TargetGameVersion = "0.46D";
}
public class InspectorMod : MelonMod
{
	private enum SettingsDetail
	{
		Value,
		Augmented,
		Berry
	}

	private sealed class ScaledWindow
	{
		internal CustomUIWindow Window;

		internal Button Button;

		internal bool Ready;

		internal bool Applied;
	}

	private const string WINDOW_ID = "ProbablyStolenInspector.counterfeit";

	private const string WINDOW_ID_VALUE = "ProbablyStolenInspector.value";

	private const string WINDOW_ID_AUG = "ProbablyStolenInspector.aug";

	private const string WINDOW_ID_WANTED = "ProbablyStolenInspector.wanted";

	private const string WINDOW_ID_SETTINGS = "ProbablyStolenInspector.settings";

	private static readonly string[][] CHECK_SETS = new string[3][]
	{
		new string[5] { "ISSUE_INDICATOR|inspection_indicator_issues|指示灯问题|Indicator issue", "ISSUE_LABEL|inspection_label_issues|标签问题|Label issue", "ISSUE_SERIAL|inspection_serial_issues|序列号问题|Serial number issue", "ISSUE_BODY|inspection_body_issues|瓶体问题|Body issue", "ISSUE_COLOR|inspection_color_issues|颜色问题|Color issue" },
		new string[5] { "CIGARETTE_LETTERING_ISSUE|inspection_lettering_issues|字母印刷问题|Lettering issue", "CIGARETTE_COLOR_ISSUE|inspection_color_issues|颜色问题|Color issue", "CIGARETTE_SEAL_ISSUE|inspection_seal_issues|防伪标问题|Seal issue", "CIGARETTE_LOGO_ISSUE|inspection_logo_issues|商标问题|Logo issue", "CIGARETTE_BOX_ISSUE|inspection_box_issues|包装盒问题|Box issue" },
		new string[5] { "STAMP_LETTERING_ISSUE|inspection_lettering_issues|字母印刷问题|Lettering issue", "STAMP_LOGO_ISSUE|inspection_logo_issues|商标问题|Logo issue", "STAMP_SEAL_ISSUE|inspection_seal_issues|防伪标问题|Seal issue", "STAMP_AMOUNT_ISSUE|inspection_value_issues|面值问题|Value issue", "STAMP_CHIP_ISSUE|inspection_chip_issues|芯片问题|Chip issue" }
	};

	private static readonly string[] CHECK_TYPE_TAGS = new string[3] { "INS_INJECTOR_TAG", "CIGARETTE_TAG", "FOOD_STAMP_TAG" };

	private const string COLOR_BAD = "#BE514C";

	private const string COLOR_OK = "#00B500";

	private float _timer = 0f;

	private float _triggerTimer = 0f;

	private static MelonPreferences_Category _cfg;

	private static MelonPreferences_Entry<float> _posX;

	private static MelonPreferences_Entry<float> _posY;

	private static MelonPreferences_Entry<float> _valPosX;

	private static MelonPreferences_Entry<float> _valPosY;

	private static MelonPreferences_Entry<float> _augPosX;

	private static MelonPreferences_Entry<float> _augPosY;

	private static MelonPreferences_Entry<float> _wantedPosX;

	private static MelonPreferences_Entry<float> _wantedPosY;

	private static MelonPreferences_Entry<bool> _enabledAuthenticity;

	private static MelonPreferences_Entry<bool> _enabledAppraisal;

	private static MelonPreferences_Entry<bool> _enabledFillEstimate;

	private static MelonPreferences_Entry<bool> _enabledAugAlert;

	private static MelonPreferences_Entry<bool> _enabledWantedAlert;

	private static MelonPreferences_Entry<bool> _enabledBerryQuality;

	private static MelonPreferences_Entry<bool> _showAugPressure;

	private static MelonPreferences_Entry<bool> _showBerryQualityValue;

	private static readonly List<object> _valueWindowCallbacks = new List<object>();

	private const string VALUE_DETAIL_COLOR = "#BDB6B0";

	private UnityAction _augDialogueCallback;

	private UnityEvent _augDialogueEvent;

	private UnityAction _augDialogueStartCallback;

	private UnityEvent _augDialogueStartEvent;

	private const string WINDOW_ID_SETTINGS_AUG_DETAILS = "ProbablyStolenInspector.settings.augDetails";

	private const string WINDOW_ID_SETTINGS_BERRY_DETAILS = "ProbablyStolenInspector.settings.berryDetails";

	private const string WINDOW_ID_SETTINGS_VALUE_DETAILS = "ProbablyStolenInspector.settings.valueDetails";

	private static readonly List<object> _rootedCallbacks = new List<object>();

	private static MelonPreferences_Entry<bool> _largeUI;

	private static readonly Dictionary<string, ScaledWindow> _scaledWindows = new Dictionary<string, ScaledWindow>();

	private static UnityAction _scaleCallback;

	private static Sprite _zoomNormal;

	private static Sprite _zoomHover;

	private static Sprite _zoomPressed;

	private static float _nextZoomScan;

	private const float LARGE_UI_SCALE = 1.5f;

	public override void OnInitializeMelon()
	{
		_cfg = MelonPreferences.CreateCategory("ProbablyStolenInspector");
		_largeUI = _cfg.CreateEntry<bool>("LargeUI", false, "放大全部鉴定助手窗口 / Enlarge all inspector windows", (string)null, false, false, (ValueValidator)null, (string)null);
		_posX = _cfg.CreateEntry<float>("window_pos_x", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_posY = _cfg.CreateEntry<float>("window_pos_y", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_valPosX = _cfg.CreateEntry<float>("value_window_pos_x", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_valPosY = _cfg.CreateEntry<float>("value_window_pos_y", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_augPosX = _cfg.CreateEntry<float>("aug_window_pos_x", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_augPosY = _cfg.CreateEntry<float>("aug_window_pos_y", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_wantedPosX = _cfg.CreateEntry<float>("wanted_window_pos_x", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_wantedPosY = _cfg.CreateEntry<float>("wanted_window_pos_y", -100000f, (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		_enabledAuthenticity = _cfg.CreateEntry<bool>("EnableAuthenticity", true, "真假鉴定 / Authenticity check", (string)null, false, false, (ValueValidator)null, (string)null);
		_enabledAppraisal = _cfg.CreateEntry<bool>("EnableAppraisal", true, "价值鉴定 / Value appraisal", (string)null, false, false, (ValueValidator)null, (string)null);
		_enabledFillEstimate = _cfg.CreateEntry<bool>("EnableFillEstimate", false, "启用填入估价 / Enable Fill estimate", (string)null, false, false, (ValueValidator)null, (string)null);
		_enabledAugAlert = _cfg.CreateEntry<bool>("EnableAugAlert", true, "改造体警告 / Augmented alert", (string)null, false, false, (ValueValidator)null, (string)null);
		_enabledWantedAlert = _cfg.CreateEntry<bool>("EnableWantedAlert", true, "通缉犯警告 / Wanted alert", (string)null, false, false, (ValueValidator)null, (string)null);
		_enabledBerryQuality = _cfg.CreateEntry<bool>("EnableBerryQualityDisplay", true, "荧光莓品质显示 / Wine berry quality display", (string)null, false, false, (ValueValidator)null, (string)null);
		_showAugPressure = _cfg.CreateEntry<bool>("ShowAugPressure", false, "显示改造体压力值 / Show augmented pressure value", (string)null, false, false, (ValueValidator)null, (string)null);
		_showBerryQualityValue = _cfg.CreateEntry<bool>("ShowBerryQualityValue", false, "显示荧光莓品质值 / Show wine berry quality value", (string)null, false, false, (ValueValidator)null, (string)null);
		BerryTooltip.Enabled = _enabledBerryQuality.Value;
		BerryTooltip.ShowQualityValue = _showBerryQualityValue.Value;
		try
		{
			if (_cfg.DeleteEntry("VerboseLog"))
			{
				MelonLogger.Msg("已清理旧版遗留的「诊断日志」配置项");
			}
		}
		catch
		{
		}
		MelonPreferences_Entry<bool> migratedEntry = _cfg.CreateEntry<bool>("LegacyMigrated", false, (string)null, (string)null, true, false, (ValueValidator)null, (string)null);
		LegacyMigration.Run(_cfg, migratedEntry);
		SafeGameAccess.MarkInitTime();
		MelonLogger.Msg("ProbablyStolenInspector 3.23.0 by kale loaded (read-only results, manual estimate input). 右键放大镜 = 设置 / Right-click magnifier = settings");
		ModLog.Fingerprint();
	}

	public override void OnApplicationQuit()
	{
		BerryTooltip.Unregister();
		UnregisterAugDialogueObservation();
		MelonPreferences.Save();
	}

	public override void OnUpdate()
	{
		try
		{
			RefreshLocale();
			ModLog.MarkOk("RefreshLocale");
		}
		catch (Exception ex)
		{
			ModLog.Error("RefreshLocale", ex);
		}
		_triggerTimer += Time.deltaTime;
		if (_triggerTimer >= 0.05f)
		{
			_triggerTimer = 0f;
			try
			{
				CheckMagnifierSettingsTrigger();
				ModLog.MarkOk("CheckMagnifierSettingsTrigger");
			}
			catch (Exception ex2)
			{
				ModLog.Error("CheckMagnifierSettingsTrigger", ex2);
			}
		}
		_timer += Time.deltaTime;
		if (_timer < 0.2f)
		{
			return;
		}
		_timer = 0f;
		CustomUIManager instance = CustomUIManager.Instance;
		if (!((Object)(object)instance == (Object)null) && !((Object)(object)instance.windowPrefab == (Object)null) && !((Object)(object)instance.labelPrefab == (Object)null))
		{
			CloseOrphanSettingsDetails(instance);
			BerryTooltip.EnsureRegistered();
			StoreClient client = ((_enabledAugAlert.Value || _enabledWantedAlert.Value) ? SafeGameAccess.GetCurrentStoreClient() : null);
			try
			{
				RefreshInspector(instance);
				ModLog.MarkOk("RefreshInspector");
			}
			catch (Exception ex3)
			{
				ModLog.Error("RefreshInspector", ex3);
			}
			try
			{
				RefreshValueAppraiser(instance);
				ModLog.MarkOk("RefreshValueAppraiser");
			}
			catch (Exception ex4)
			{
				ModLog.Error("RefreshValueAppraiser", ex4);
			}
			try
			{
				RefreshAugInspector(instance, client);
				ModLog.MarkOk("RefreshAugInspector");
			}
			catch (Exception ex5)
			{
				ModLog.Error("RefreshAugInspector", ex5);
			}
			try
			{
				RefreshWantedInspector(instance, client);
				ModLog.MarkOk("RefreshWantedInspector");
			}
			catch (Exception ex6)
			{
				ModLog.Error("RefreshWantedInspector", ex6);
			}
			TickInspectorWindowScale(instance);
		}
	}

	private void RefreshLocale()
	{
		bool gameReady;
		try
		{
			CustomUIManager instance = CustomUIManager.Instance;
			gameReady = (Object)(object)instance != (Object)null && (Object)(object)instance.windowPrefab != (Object)null && (Object)(object)instance.labelPrefab != (Object)null;
		}
		catch
		{
			gameReady = false;
		}
		LocaleTick localeTick = LocalizationService.Tick(Time.deltaTime, gameReady);
		if (localeTick == LocaleTick.None)
		{
			return;
		}
		InspectorState.FakeLastSig = null;
		InspectorState.FakeFailedSig = null;
		InspectorState.ValueLastSig = null;
		InspectorState.AugLastSig = null;
		InspectorState.AugFailedSig = null;
		InspectorState.WantedLastSig = null;
		if (localeTick != LocaleTick.Changed)
		{
			return;
		}
		bool flag = false;
		try
		{
			CustomUIManager instance2 = CustomUIManager.Instance;
			if ((Object)(object)instance2 != (Object)null)
			{
				flag = instance2.IsOpen("ProbablyStolenInspector.settings");
				instance2.CloseWindow("ProbablyStolenInspector.counterfeit");
				instance2.CloseWindow("ProbablyStolenInspector.value");
				instance2.CloseWindow("ProbablyStolenInspector.aug");
				instance2.CloseWindow("ProbablyStolenInspector.wanted");
				CloseSettingsDetailWindows(instance2);
				instance2.CloseWindow("ProbablyStolenInspector.settings");
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("语言切换重建窗口失败: " + ex.GetType().Name + " - " + ex.Message);
		}
		InspectorState.FakeSuppressed = false;
		InspectorState.ValueSuppressed = false;
		InspectorState.AugSuppressed = false;
		InspectorState.WantedSuppressed = false;
		InspectorState.FakeItemPtr = IntPtr.Zero;
		InspectorState.ValueItemPtr = IntPtr.Zero;
		InspectorState.FakeItemUid = -1;
		InspectorState.ValueItemUid = -1;
		InspectorState.AugClientPtr = IntPtr.Zero;
		InspectorState.WantedClientPtr = IntPtr.Zero;
		InspectorState.AugClientId = "";
		InspectorState.WantedClientId = "";
		InspectorState.ContextItemPtr = IntPtr.Zero;
		if (!flag)
		{
			return;
		}
		try
		{
			CustomUIManager instance3 = CustomUIManager.Instance;
			if ((Object)(object)instance3 != (Object)null)
			{
				BuildSettingsWindow(instance3);
			}
		}
		catch
		{
		}
	}

	private static void SaveWindowPos(CustomUIManager ui, string windowId, MelonPreferences_Entry<float> px, MelonPreferences_Entry<float> py)
	{
		//IL_0036: Unknown result type (might be due to invalid IL or missing references)
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0055: Unknown result type (might be due to invalid IL or missing references)
		//IL_0079: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			CustomUIWindow window = ui.GetWindow(windowId);
			if (window == null || !window.IsAlive)
			{
				return;
			}
			RectTransform rect = window.Rect;
			if (!((Object)(object)rect == (Object)null))
			{
				Vector2 anchoredPosition = rect.anchoredPosition;
				if (Math.Abs(anchoredPosition.x - px.Value) > 2f || Math.Abs(anchoredPosition.y - py.Value) > 2f)
				{
					px.Value = anchoredPosition.x;
					py.Value = anchoredPosition.y;
				}
			}
		}
		catch
		{
		}
	}

	private static bool HasSavedPos(MelonPreferences_Entry<float> px, MelonPreferences_Entry<float> py)
	{
		return px.Value > -99000f && py.Value > -99000f;
	}

	private void RefreshInspector(CustomUIManager ui)
	{
		if (!_enabledAuthenticity.Value)
		{
			if (ui.IsOpen("ProbablyStolenInspector.counterfeit"))
			{
				ui.CloseWindow("ProbablyStolenInspector.counterfeit");
			}
			InspectorState.FakeLastSig = null;
			InspectorState.FakeFailedSig = null;
			InspectorState.FakeWantVisible = false;
			InspectorState.FakeSuppressed = false;
			return;
		}
		SaveWindowPos(ui, "ProbablyStolenInspector.counterfeit", _posX, _posY);
		InspectionUIManager instance = InspectionUIManager.Instance;
		GameItem val = (((Object)(object)instance != (Object)null) ? instance.currentInspectingItem : null);
		IntPtr intPtr = ((val != null) ? ((Il2CppObjectBase)val).Pointer : IntPtr.Zero);
		int num = SafeGameAccess.SafeUniqueId(val);
		if (intPtr != InspectorState.FakeItemPtr || num != InspectorState.FakeItemUid)
		{
			InspectorState.FakeItemPtr = intPtr;
			InspectorState.FakeItemUid = num;
			InspectorState.FakeFailedSig = null;
			InspectorState.FakeSuppressed = false;
			InspectorState.FakeWantVisible = false;
			ModLog.Verbose((val != null) ? ("真假鉴定 → 目标 '" + SafeGameAccess.ItemName(val) + "'") : "真假鉴定 → 无物品（关窗）");
		}
		if (val == null || !SafeGameAccess.IsInspectionPanelVisible(instance))
		{
			if (InspectorState.FakeWantVisible)
			{
				ModLog.Verbose("真假鉴定 → 鉴定界面已关闭（关窗）");
			}
			if (ui.IsOpen("ProbablyStolenInspector.counterfeit"))
			{
				ui.CloseWindow("ProbablyStolenInspector.counterfeit");
			}
			InspectorState.FakeLastSig = null;
			InspectorState.FakeFailedSig = null;
			InspectorState.FakeWantVisible = false;
			return;
		}
		int num2 = -1;
		for (int i = 0; i < CHECK_TYPE_TAGS.Length; i++)
		{
			if (SafeGameAccess.SafeIsTag(val, CHECK_TYPE_TAGS[i]))
			{
				num2 = i;
				break;
			}
		}
		if (num2 < 0)
		{
			if (ui.IsOpen("ProbablyStolenInspector.counterfeit"))
			{
				ui.CloseWindow("ProbablyStolenInspector.counterfeit");
			}
			InspectorState.FakeLastSig = null;
			InspectorState.FakeFailedSig = null;
			InspectorState.FakeWantVisible = false;
			return;
		}
		if (InspectorState.FakeSuppressed)
		{
			if (ui.IsOpen("ProbablyStolenInspector.counterfeit"))
			{
				ui.CloseWindow("ProbablyStolenInspector.counterfeit");
			}
			InspectorState.FakeWantVisible = false;
			return;
		}
		if (InspectorState.FakeWantVisible && !ui.IsOpen("ProbablyStolenInspector.counterfeit"))
		{
			InspectorState.FakeSuppressed = true;
			InspectorState.FakeWantVisible = false;
			return;
		}
		List<string> checkList = GetCheckList(val, num2, num);
		string text = SafeGameAccess.ItemName(val);
		List<bool?> list = new List<bool?>(checkList.Count);
		bool flag = false;
		foreach (string item in checkList)
		{
			bool? flag2 = SafeGameAccess.ReadAuthenticityIssue(val, item.Split('|')[0]);
			list.Add(flag2);
			if (flag2 == true)
			{
				flag = true;
			}
		}
		AuthenticityReadings data = (flag ? SafeGameAccess.ReadAuthenticityAppearance(val, num2) : null);
		List<AuthenticityDetail> list2 = new List<AuthenticityDetail>(checkList.Count);
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(intPtr).Append('|').Append(num)
			.Append('|')
			.Append(num2)
			.Append('|')
			.Append(Screen.width)
			.Append('x')
			.Append(Screen.height);
		stringBuilder.Append('|').Append(text.Length).Append(':')
			.Append(text);
		for (int j = 0; j < checkList.Count; j++)
		{
			string text2 = checkList[j].Split('|')[0];
			bool? flag3 = list[j];
			StringBuilder stringBuilder2 = stringBuilder.Append('|').Append(text2).Append(':');
			string value;
			if (flag3.HasValue)
			{
				value = (flag3.Value ? "1" : "0");
			}
			else
			{
				value = "?";
			}
			stringBuilder2.Append(value);
			AuthenticityDetail authenticityDetail = ((flag3 == false) ? null : AuthenticityExplainer.Explain(num2, text2, flag3, data));
			list2.Add(authenticityDetail);
			if (authenticityDetail == null)
			{
				continue;
			}
			foreach (AuthenticityText reason in authenticityDetail.Reasons)
			{
				stringBuilder.Append('|').Append(reason.Zh.Length).Append(':')
					.Append(reason.Zh);
			}
			if (authenticityDetail.Standard != null)
			{
				stringBuilder.Append('|').Append(authenticityDetail.Standard.Zh.Length).Append(':')
					.Append(authenticityDetail.Standard.Zh);
			}
		}
		string text3 = stringBuilder.ToString();
		if (text3 == InspectorState.FakeLastSig && ui.IsOpen("ProbablyStolenInspector.counterfeit"))
		{
			ClampInspectorWindow(ui.GetWindow("ProbablyStolenInspector.counterfeit"));
			InspectorState.FakeWantVisible = true;
		}
		else
		{
			if (text3 == InspectorState.FakeFailedSig)
			{
				return;
			}
			InspectorState.FakeWantVisible = false;
			try
			{
				BuildAuthenticityWindow(ui, text, checkList, list, list2);
				InspectorState.FakeFailedSig = null;
				InspectorState.FakeLastSig = text3;
				InspectorState.FakeWantVisible = true;
			}
			catch
			{
				InspectorState.FakeFailedSig = text3;
				InspectorState.FakeLastSig = null;
				try
				{
					if (ui.IsOpen("ProbablyStolenInspector.counterfeit"))
					{
						ui.CloseWindow("ProbablyStolenInspector.counterfeit");
					}
				}
				catch (Exception ex)
				{
					ModLog.Error("Authenticity.CloseFailedWindow", ex);
				}
				throw;
			}
		}
	}

	private void BuildAuthenticityWindow(CustomUIManager ui, string itemName, List<string> items, List<bool?> states, List<AuthenticityDetail> details)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0144: Unknown result type (might be due to invalid IL or missing references)
		//IL_0149: Unknown result type (might be due to invalid IL or missing references)
		//IL_0595: Unknown result type (might be due to invalid IL or missing references)
		//IL_05bb: Unknown result type (might be due to invalid IL or missing references)
		//IL_05de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0615: Unknown result type (might be due to invalid IL or missing references)
		//IL_062f: Unknown result type (might be due to invalid IL or missing references)
		//IL_063b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0640: Unknown result type (might be due to invalid IL or missing references)
		CustomUIWindow window = ui.GetWindow("ProbablyStolenInspector.counterfeit");
		Vector3? val = ((window != null && window.IsAlive && window.IsVisible) ? new Vector3?(InspectorWindowTopLeftWorld(window.Rect)) : ((Vector3?)null));
		float fakeWindowHeight = InspectorState.FakeWindowHeight;
		float num = (details.Exists((AuthenticityDetail detail) => detail != null) ? 420f : 280f);
		float num2 = (_largeUI.Value ? 1.5f : 1f);
		for (int num3 = 0; num3 < 2; num3++)
		{
			if (ui.IsOpen("ProbablyStolenInspector.counterfeit"))
			{
				ui.CloseWindow("ProbablyStolenInspector.counterfeit");
			}
			CustomUIBuilder val2 = ui.CreateWindow("ProbablyStolenInspector.counterfeit", LocalizationService.L("鉴定助手", "Inspector"), "overlay").SetDraggable(true).SetCloseOnEscape(false)
				.SetSize(num, 260f);
			CustomUIWindow window2 = ui.GetWindow("ProbablyStolenInspector.counterfeit");
			if (window2 == null || (Object)(object)window2.Rect == (Object)null || (Object)(object)window2.ContentRoot == (Object)null)
			{
				throw new InvalidOperationException("真伪窗口预制体未就绪");
			}
			AttachWindowScale(window2);
			Rect val3 = InspectorScreenBounds(window2.Rect);
			float num4 = Math.Max(1f, (val3.width - 16f) / num2);
			float num5 = ((num3 == 0) ? Math.Min(num, num4) : num4);
			window2.SetSize(num5, 260f);
			VerticalLayoutGroup component = ((Component)window2.Rect).GetComponent<VerticalLayoutGroup>();
			VerticalLayoutGroup component2 = ((Component)window2.ContentRoot).GetComponent<VerticalLayoutGroup>();
			float num6 = Math.Max(1f, num5 - (((Object)(object)component != (Object)null) ? ((float)((LayoutGroup)component).padding.horizontal) : 16f) - (((Object)(object)component2 != (Object)null) ? ((float)((LayoutGroup)component2).padding.horizontal) : 8f));
			val2.BeginColumn(6f);
			AddValueText(val2, LocalizationService.L("物品：", "Item: ") + AuthenticityExplainer.Display(itemName), num6);
			AddValueText(val2, LocalizationService.L("检测目标：", "Checks:"), num6);
			for (int num7 = 0; num7 < items.Count; num7++)
			{
				string[] array = items[num7].Split('|');
				bool? flag = states[num7];
				string text = LocalizationService.LocalizedIssueName(array[1], array[2], array[3]);
				string text2;
				if (flag.HasValue)
				{
					text2 = (flag.Value ? "#BE514C" : "#00B500");
				}
				else
				{
					text2 = "#BDB6B0";
				}
				AddValueText(val2, "<color=" + text2 + ">" + text + ((!flag.HasValue) ? LocalizationService.L("（未读取）", " (unavailable)") : "") + "</color>", num6);
				AuthenticityDetail authenticityDetail = details[num7];
				if (authenticityDetail == null)
				{
					continue;
				}
				float num8 = Math.Min(12f, Math.Max(0f, num6 - 1f));
				val2.BeginRow(0f);
				val2.AddLabel(" ", (string)null);
				if (val2.Last != null)
				{
					val2.Last.SetPreferredSize(num8, 1f);
				}
				val2.BeginColumn(4f);
				foreach (AuthenticityText reason in authenticityDetail.Reasons)
				{
					AddValueText(val2, "<color=" + (reason.Verified ? "#E4A7A0" : "#BDB6B0") + ">" + LocalizationService.L(reason.Zh, reason.En) + "</color>", Math.Max(1f, num6 - num8));
				}
				if (flag == true && authenticityDetail.Standard != null)
				{
					AddValueText(val2, LocalizationService.L(authenticityDetail.Standard.Zh, authenticityDetail.Standard.En), Math.Max(1f, num6 - num8), rightAligned: false, subdued: true);
				}
				val2.End();
				val2.End();
			}
			val2.End();
			window2 = val2.Show();
			LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
			FitInspectorWindowTitle(window2);
			LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
			float num9 = Mathf.Ceil(LayoutUtility.GetPreferredHeight(window2.Rect));
			if (num9 <= 0f || float.IsNaN(num9) || float.IsInfinity(num9))
			{
				throw new InvalidOperationException("真伪窗口未能获得有效内容高度");
			}
			if (num3 != 0 || !(num9 > (val3.height - 16f) / num2) || !(num5 < num4 - 1f))
			{
				window2.SetSize(num5, num9);
				LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
				if (HasSavedPos(_posX, _posY))
				{
					window2.SetPosition(new Vector2(_posX.Value, _posY.Value));
				}
				else
				{
					window2.Center();
				}
				CompleteScaledWindow(window2);
				float num10 = num9 * ((Transform)window2.Rect).localScale.y;
				if (val.HasValue)
				{
					KeepInspectorWindowTopLeft(window2.Rect, val.Value);
				}
				else if (HasSavedPos(_posX, _posY) && fakeWindowHeight > 0f)
				{
					RectTransform rect = window2.Rect;
					rect.anchoredPosition += new Vector2(0f, (fakeWindowHeight - num10) * (1f - window2.Rect.pivot.y));
				}
				ClampInspectorWindow(window2);
				InspectorState.FakeWindowHeight = num10;
				SaveWindowPos(ui, "ProbablyStolenInspector.counterfeit", _posX, _posY);
				return;
			}
		}
		throw new InvalidOperationException("真伪窗口未完成布局");
	}

	private static List<string> GetCheckList(GameItem item, int type, int uid)
	{
		if (InspectorState.CheckList != null && InspectorState.CheckItemPtr == ((Il2CppObjectBase)item).Pointer && InspectorState.CheckItemUid == uid && InspectorState.CheckItemType == type)
		{
			return InspectorState.CheckList;
		}
		InspectorState.CheckList = BuildCheckList(item, type);
		InspectorState.CheckItemPtr = ((Il2CppObjectBase)item).Pointer;
		InspectorState.CheckItemUid = uid;
		InspectorState.CheckItemType = type;
		return InspectorState.CheckList;
	}

	private static List<string> BuildCheckList(GameItem item, int type)
	{
		List<string> list = new List<string>(CHECK_SETS[type]);
		try
		{
			HashSet<string> hashSet = new HashSet<string>();
			string[] array = CHECK_SETS[type];
			foreach (string text in array)
			{
				hashSet.Add(text.Split('|')[0]);
			}
			foreach (string item2 in EnumerateTags(item, type, onlyIssue: true))
			{
				if (hashSet.Add(item2))
				{
					list.Add(item2 + "|" + LocalizationService.GuessLocKey(item2) + "|" + item2 + "|" + item2);
				}
			}
			ModLog.MarkOk("BuildCheckList.discover");
		}
		catch (Exception ex)
		{
			ModLog.Warn("BuildCheckList.discover", "检查项自动发现失败（不影响内置项）: " + ex.GetType().Name + " - " + ex.Message);
		}
		return list;
	}

	private static List<string> EnumerateTags(GameItem item, int type, bool onlyIssue)
	{
		List<string> list = new List<string>();
		TagSystem modifiedState = item.modifiedState;
		if (modifiedState == null)
		{
			return list;
		}
		Dictionary<string, TagState> dict = modifiedState.dict;
		if (dict == null)
		{
			return list;
		}
		Enumerator<string, TagState> enumerator = dict.Keys.GetEnumerator();
		while (enumerator.MoveNext())
		{
			string current = enumerator.Current;
			if (!string.IsNullOrEmpty(current) && (!onlyIssue || IsIssueTagForType(current, type)))
			{
				list.Add(current);
			}
		}
		return list;
	}

	private static bool IsIssueTagForType(string key, int type)
	{
		return type switch
		{
			0 => key.StartsWith("ISSUE_", StringComparison.Ordinal), 
			1 => key.StartsWith("CIGARETTE_", StringComparison.Ordinal) && key.EndsWith("_ISSUE", StringComparison.Ordinal), 
			2 => key.StartsWith("STAMP_", StringComparison.Ordinal) && key.EndsWith("_ISSUE", StringComparison.Ordinal), 
			_ => false, 
		};
	}

	private void RefreshValueAppraiser(CustomUIManager ui)
	{
		if (!_enabledAppraisal.Value)
		{
			if (ui.IsOpen("ProbablyStolenInspector.value"))
			{
				ui.CloseWindow("ProbablyStolenInspector.value");
			}
			InspectorState.ValueLastSig = null;
			InspectorState.ValueWantVisible = false;
			InspectorState.ValueSuppressed = false;
			return;
		}
		SaveWindowPos(ui, "ProbablyStolenInspector.value", _valPosX, _valPosY);
		InspectionUIManager instance = InspectionUIManager.Instance;
		GameItem val = (((Object)(object)instance != (Object)null) ? instance.currentInspectingItem : null);
		IntPtr intPtr = ((val != null) ? ((Il2CppObjectBase)val).Pointer : IntPtr.Zero);
		int num = SafeGameAccess.SafeUniqueId(val);
		if (intPtr != InspectorState.ValueItemPtr || num != InspectorState.ValueItemUid)
		{
			InspectorState.ValueItemPtr = intPtr;
			InspectorState.ValueItemUid = num;
			ModLog.Verbose((val != null) ? ("价值鉴定 → 目标 '" + SafeGameAccess.ItemName(val) + "'") : "价值鉴定 → 无物品（关窗）");
			InspectorState.ValueSuppressed = false;
			InspectorState.ValueWantVisible = false;
		}
		if (val == null || !SafeGameAccess.IsInspectionPanelVisible(instance))
		{
			if (InspectorState.ValueWantVisible)
			{
				ModLog.Verbose("价值鉴定 → 鉴定界面已关闭（关窗）");
			}
			if (ui.IsOpen("ProbablyStolenInspector.value"))
			{
				ui.CloseWindow("ProbablyStolenInspector.value");
			}
			InspectorState.ValueLastSig = null;
			InspectorState.ValueWantVisible = false;
			return;
		}
		if (!SafeGameAccess.SafeIsTag(val, "APPRAISAL_ITEM"))
		{
			if (InspectorState.ValueWantVisible)
			{
				ModLog.Verbose("价值鉴定 → 目标不可估价（关窗）");
			}
			if (ui.IsOpen("ProbablyStolenInspector.value"))
			{
				ui.CloseWindow("ProbablyStolenInspector.value");
			}
			InspectorState.ValueLastSig = null;
			InspectorState.ValueWantVisible = false;
			return;
		}
		int? num2 = SafeGameAccess.ReadAppraisalTrueValue(val);
		string text = (num2.HasValue ? num2.Value.ToString(CultureInfo.InvariantCulture) : "?");
		bool canFill = _enabledFillEstimate.Value && num2.HasValue && (Object)(object)SafeGameAccess.ReadAppraisalInput(instance, intPtr, num) != (Object)null;
		AppraisalValueBreakdown breakdown = ((num2.HasValue && InspectorState.ValueDetailsExpanded) ? ReadValueBreakdown(val, num2.Value) : null);
		if (InspectorState.ValueSuppressed)
		{
			if (ui.IsOpen("ProbablyStolenInspector.value"))
			{
				ui.CloseWindow("ProbablyStolenInspector.value");
			}
			InspectorState.ValueWantVisible = false;
			return;
		}
		if (InspectorState.ValueWantVisible && !ui.IsOpen("ProbablyStolenInspector.value"))
		{
			InspectorState.ValueSuppressed = true;
			InspectorState.ValueWantVisible = false;
			return;
		}
		string text2 = intPtr + "|" + num + "|" + SafeGameAccess.SafeString(val.name) + "|" + text + "|" + _enabledFillEstimate.Value + "|" + canFill + "|" + InspectorState.ValueDetailsExpanded + "|" + ValueBreakdownSignature(breakdown) + "|" + Screen.width + "x" + Screen.height;
		if (text2 == InspectorState.ValueLastSig && ui.IsOpen("ProbablyStolenInspector.value"))
		{
			ClampInspectorWindow(ui.GetWindow("ProbablyStolenInspector.value"));
			InspectorState.ValueWantVisible = true;
		}
		else
		{
			InspectorState.ValueLastSig = text2;
			BuildValueWindow(ui, SafeGameAccess.SafeString(val.name), text, breakdown, intPtr, num, canFill);
			InspectorState.ValueWantVisible = true;
		}
	}

	private void BuildValueWindow(CustomUIManager ui, string itemName, string realVal, AppraisalValueBreakdown breakdown, IntPtr itemPtr, int itemUid, bool canFill)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_0119: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		//IL_062c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0653: Unknown result type (might be due to invalid IL or missing references)
		//IL_0676: Unknown result type (might be due to invalid IL or missing references)
		//IL_06ad: Unknown result type (might be due to invalid IL or missing references)
		//IL_06c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d3: Unknown result type (might be due to invalid IL or missing references)
		//IL_06d8: Unknown result type (might be due to invalid IL or missing references)
		CustomUIWindow window = ui.GetWindow("ProbablyStolenInspector.value");
		Vector3? val = ((window != null && window.IsAlive && window.IsVisible) ? new Vector3?(InspectorWindowTopLeftWorld(window.Rect)) : ((Vector3?)null));
		float valueWindowHeight = InspectorState.ValueWindowHeight;
		float num = (_largeUI.Value ? 1.5f : 1f);
		for (int i = 0; i < 2; i++)
		{
			if (ui.IsOpen("ProbablyStolenInspector.value"))
			{
				ui.CloseWindow("ProbablyStolenInspector.value");
			}
			_valueWindowCallbacks.Clear();
			CustomUIBuilder val2 = ui.CreateWindow("ProbablyStolenInspector.value", LocalizationService.L("价值鉴定 · ", "Value Appraisal · ") + itemName, "overlay").SetDraggable(true).SetCloseOnEscape(false)
				.SetSize(420f, 240f);
			CustomUIWindow window2 = ui.GetWindow("ProbablyStolenInspector.value");
			if (window2 == null || (Object)(object)window2.Rect == (Object)null || (Object)(object)window2.ContentRoot == (Object)null)
			{
				break;
			}
			AttachWindowScale(window2);
			Rect val3 = InspectorScreenBounds(window2.Rect);
			float num2 = Math.Max(1f, (val3.width - 16f) / num);
			float num3 = ((i == 0) ? Math.Min(420f, num2) : num2);
			window2.SetSize(num3, 240f);
			VerticalLayoutGroup component = ((Component)window2.Rect).GetComponent<VerticalLayoutGroup>();
			VerticalLayoutGroup component2 = ((Component)window2.ContentRoot).GetComponent<VerticalLayoutGroup>();
			float num4 = Math.Max(1f, num3 - (((Object)(object)component != (Object)null) ? ((float)((LayoutGroup)component).padding.horizontal) : 16f) - (((Object)(object)component2 != (Object)null) ? ((float)((LayoutGroup)component2).padding.horizontal) : 8f));
			val2.BeginColumn(10f);
			AddValueRow(val2, "<b>" + LocalizationService.L("真实价值", "True value") + "</b>", "<b>" + realVal + "</b>", "#00B500", num4);
			if (InspectorState.ValueDetailsExpanded)
			{
				float num5 = Math.Min(12f, Math.Max(0f, num4 - 1f));
				float width = Math.Max(1f, num4 - num5);
				val2.BeginRow(0f);
				val2.AddLabel(" ", (string)null);
				if (val2.Last != null)
				{
					val2.Last.SetPreferredSize(num5, 1f);
				}
				val2.BeginColumn(4f);
				if (breakdown == null)
				{
					AddValueText(val2, LocalizationService.L("计算说明待核验", "Calculation details pending verification"), width, rightAligned: false, subdued: true);
				}
				else if (breakdown.IsFixed)
				{
					AddValueText(val2, LocalizationService.L(breakdown.FixedReasonZh, breakdown.FixedReasonEn), width, rightAligned: false, subdued: true);
					foreach (AppraisalValueLine fixedDetail in breakdown.FixedDetails)
					{
						AddValueText(val2, "<color=#E4A7A0>? " + LocalizationService.L(fixedDetail.Zh, fixedDetail.En) + "</color>", width);
					}
				}
				else
				{
					foreach (AppraisalValueLine line in breakdown.Lines)
					{
						if (line.Amount != 0)
						{
							string amount = (line.IsBase ? line.Amount.ToString(CultureInfo.InvariantCulture) : (((line.Amount > 0) ? "+" : "") + line.Amount.ToString(CultureInfo.InvariantCulture)));
							string color;
							if (line.IsBase || line.Amount == 0)
							{
								color = null;
							}
							else
							{
								color = ((line.Amount > 0) ? "#A8D8A8" : "#E4A7A0");
							}
							string label = (line.IsBase ? LocalizationService.L("基础价值", "Starting value") : LocalizationService.L(line.Zh, line.En));
							AddValueRow(val2, label, amount, color, width, subdued: true);
						}
					}
					if (breakdown.Total != breakdown.Subtotal)
					{
						AddValueRow(val2, LocalizationService.L("原始小计", "Subtotal"), breakdown.Subtotal.ToString(CultureInfo.InvariantCulture), null, width, subdued: true);
						AddValueRow(val2, LocalizationService.L("最低价值保底（", "Minimum value (") + breakdown.Minimum.ToString(CultureInfo.InvariantCulture) + LocalizationService.L("）", ")"), "+" + (breakdown.Total - breakdown.Subtotal).ToString(CultureInfo.InvariantCulture), "#A8D8A8", width, subdued: true);
					}
				}
				val2.End();
				val2.End();
			}
			AddValueActions(val2, num4, itemPtr, itemUid, canFill);
			val2.End();
			window2 = val2.Show();
			LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
			FitInspectorWindowTitle(window2);
			LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
			float num6 = Mathf.Ceil(LayoutUtility.GetPreferredHeight(window2.Rect));
			if (num6 <= 0f || float.IsNaN(num6) || float.IsInfinity(num6))
			{
				throw new InvalidOperationException("价值窗口未能获得有效内容高度");
			}
			if (i != 0 || !(num6 > (val3.height - 16f) / num) || !(num3 < num2 - 1f))
			{
				window2.SetSize(num3, num6);
				LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
				if (HasSavedPos(_valPosX, _valPosY))
				{
					window2.SetPosition(new Vector2(_valPosX.Value, _valPosY.Value));
				}
				else
				{
					window2.Center();
				}
				CompleteScaledWindow(window2);
				float num7 = num6 * ((Transform)window2.Rect).localScale.y;
				if (val.HasValue)
				{
					KeepInspectorWindowTopLeft(window2.Rect, val.Value);
				}
				else if (HasSavedPos(_valPosX, _valPosY) && valueWindowHeight > 0f)
				{
					RectTransform rect = window2.Rect;
					rect.anchoredPosition += new Vector2(0f, (valueWindowHeight - num7) * (1f - window2.Rect.pivot.y));
				}
				ClampInspectorWindow(window2);
				InspectorState.ValueWindowHeight = num7;
				SaveWindowPos(ui, "ProbablyStolenInspector.value", _valPosX, _valPosY);
				break;
			}
		}
	}

	private static AppraisalValueBreakdown ReadValueBreakdown(GameItem item, int nativeValue)
	{
		try
		{
			AppraisalValueBreakdown appraisalValueBreakdown = null;
			if (SafeGameAccess.SafeIsTag(item, "PLASMA_CELL_TAG"))
			{
				if (SafeGameAccess.ReadAppraisalTagString(item, "PLASMA_CASING") == null)
				{
					return null;
				}
				appraisalValueBreakdown = AppraisalValueCalculator.Plasma(SafeGameAccess.ReadAppraisalTagString(item, "PLASMA_CASING_CONDITION_WEAR"), SafeGameAccess.ReadAppraisalTagString(item, "PLASMA_CASING_CONDITION_CRACK"), SafeGameAccess.ReadAppraisalTagString(item, "PLASMA_CASING_CONDITION_UNCOVERED"), SafeGameAccess.ReadAppraisalTagString(item, "PLASMA_CASING_CONDITION_REINFORCED"), SafeGameAccess.ReadAppraisalTagString(item, "PLASMA_COLOR"), SafeGameAccess.ReadAppraisalTagString(item, "PLASMA_SEAL"));
			}
			else if (SafeGameAccess.SafeIsTag(item, "RIBWICH_TAG"))
			{
				int? num = SafeGameAccess.ReadTagInt(item, "RIBWICH_WRAP_TAG");
				if (!num.HasValue)
				{
					return null;
				}
				bool flag = num.Value == 1 || num.Value == 2;
				bool flag2 = !flag && nativeValue == 40;
				appraisalValueBreakdown = AppraisalValueCalculator.Ribwich(num.Value, nativeValue, (flag | flag2) ? ((int?)null) : SafeGameAccess.ReadTagInt(item, "RIBWICH_MEAT_TAG"), flag ? ((int?)null) : SafeGameAccess.ReadTagInt(item, "RIBWICH_BUN_TAG"), flag ? ((int?)null) : SafeGameAccess.ReadTagInt(item, "RIBWICH_PICKLES_TAG"), flag ? ((int?)null) : SafeGameAccess.ReadTagInt(item, "RIBWICH_PRODUCTION_MONTH_TAG"), flag ? ((int?)null) : SafeGameAccess.ReadTagInt(item, "RIBWICH_PRODUCTION_YEAR_TAG"), flag ? ((int?)null) : SafeGameAccess.ReadTagInt(item, "RIBWICH_FACILITY_NUMBER_TAG"), flag2 ? SafeGameAccess.ReadTagInt(item, "RIBWICH_DAVRE_LOGO_TAG") : ((int?)null), flag2 ? SafeGameAccess.ReadTagInt(item, "RIBWICH_RIBWICH_LOGO_TAG") : ((int?)null), flag2 ? SafeGameAccess.ReadTagInt(item, "RIBWICH_PRODUCTION_TAG") : ((int?)null), flag2 ? SafeGameAccess.ReadTagInt(item, "RIBWICH_FACILITY_TAG") : ((int?)null));
			}
			else if (SafeGameAccess.SafeIsTag(item, "HAND_TAG"))
			{
				appraisalValueBreakdown = AppraisalValueCalculator.Hand(SafeGameAccess.ReadAppraisalTagString(item, "HAND_MATERIAL"), SafeGameAccess.ReadAppraisalTagString(item, "HAND_INTERFACE"), SafeGameAccess.ReadAppraisalTagString(item, "HAND_CONDITION"), SafeGameAccess.ReadAppraisalTagString(item, "HAND_ADDON_ARMORED"), SafeGameAccess.ReadAppraisalTagString(item, "HAND_ADDON_BLADED"));
			}
			if (appraisalValueBreakdown == null || !appraisalValueBreakdown.MatchesNative(nativeValue))
			{
				return null;
			}
			return appraisalValueBreakdown;
		}
		catch (Exception ex)
		{
			ModLog.Warn("AppraisalBreakdown", "价值明细不可用: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	private static string ValueBreakdownSignature(AppraisalValueBreakdown breakdown)
	{
		if (breakdown == null)
		{
			return "?";
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(breakdown.Total).Append('|').Append(breakdown.Subtotal);
		if (breakdown.IsFixed)
		{
			stringBuilder.Append('|').Append(breakdown.FixedReasonEn);
		}
		foreach (AppraisalValueLine fixedDetail in breakdown.FixedDetails)
		{
			stringBuilder.Append('|').Append(fixedDetail.Zh).Append('|')
				.Append(fixedDetail.En);
		}
		foreach (AppraisalValueLine line in breakdown.Lines)
		{
			stringBuilder.Append('|').Append(line.En).Append(':')
				.Append(line.Amount);
		}
		return stringBuilder.ToString();
	}

	private static CustomUIElement AddValueText(CustomUIBuilder builder, string text, float width, bool rightAligned = false, bool subdued = false)
	{
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		if (subdued)
		{
			text = "<color=#BDB6B0>" + text + "</color>";
		}
		builder.AddLabel(text, (string)null);
		CustomUIElement last = builder.Last;
		if (last == null)
		{
			return null;
		}
		float num = 22f;
		if ((Object)(object)last.text != (Object)null)
		{
			((TMP_Text)last.text).enableAutoSizing = false;
			((TMP_Text)last.text).enableWordWrapping = true;
			((TMP_Text)last.text).alignment = (TextAlignmentOptions)(rightAligned ? 260 : 257);
			num = Math.Max(num, Mathf.Ceil(((TMP_Text)last.text).GetPreferredValues(text, width, float.PositiveInfinity).y) + 2f);
		}
		last.SetPreferredSize(width, num);
		return last;
	}

	private static void AddValueRow(CustomUIBuilder builder, string label, string amount, string color, float width, bool subdued = false)
	{
		float num = Math.Min(88f, width * 0.24f);
		float num2 = Math.Max(1f, width - num - 8f);
		builder.BeginRow(8f);
		CustomUIElement val = AddValueText(builder, label, num2, rightAligned: false, subdued);
		CustomUIElement val2 = AddValueText(builder, (color == null) ? amount : ("<color=" + color + ">" + amount + "</color>"), num, rightAligned: true, subdued && color == null);
		if (val != null && val2 != null)
		{
			float num3 = Math.Max(LayoutUtility.GetPreferredHeight(((Il2CppObjectBase)val.root.transform).Cast<RectTransform>()), LayoutUtility.GetPreferredHeight(((Il2CppObjectBase)val2.root.transform).Cast<RectTransform>()));
			val.SetPreferredSize(num2, num3);
			val2.SetPreferredSize(num, num3);
		}
		builder.End();
	}

	private void AddValueActions(CustomUIBuilder builder, float availableWidth, IntPtr itemPtr, int itemUid, bool canFill)
	{
		string label = (InspectorState.ValueDetailsExpanded ? LocalizationService.L("收起计算过程", "Hide calculation") : LocalizationService.L("查看计算过程", "Show calculation"));
		float availableWidth2 = Math.Max(1f, _enabledFillEstimate.Value ? ((availableWidth - 8f) / 2f) : availableWidth);
		builder.BeginRow(8f);
		CustomUIElement val = AddValueButton(builder, label, () =>
		{
			InspectorState.ValueDetailsExpanded = !InspectorState.ValueDetailsExpanded;
			InspectorState.ValueLastSig = null;
		}, "value_details_toggle", availableWidth2, 132f);
		CustomUIElement val2 = (_enabledFillEstimate.Value ? AddValueButton(builder, LocalizationService.L("填入估价", "Fill estimate"), () =>
		{
			FillAppraisalPrice(itemPtr, itemUid);
		}, "value_fill_estimate", availableWidth2, 100f) : null);
		if (val2 != null)
		{
			if ((Object)(object)val2.button != (Object)null)
			{
				((Selectable)val2.button).interactable = canFill;
			}
			val2.SetTooltip(canFill ? LocalizationService.L("将真实价值填入估价框，再由你提交估价", "Fill the estimate with the true value, then submit it yourself") : LocalizationService.L("鉴定服务估价框可用时才能填写", "Available when the appraisal service input is open"));
		}
		if (val != null && val2 != null)
		{
			float num = Math.Max(LayoutUtility.GetPreferredHeight(((Il2CppObjectBase)val.root.transform).Cast<RectTransform>()), LayoutUtility.GetPreferredHeight(((Il2CppObjectBase)val2.root.transform).Cast<RectTransform>()));
			LayoutElement component = val.root.GetComponent<LayoutElement>();
			LayoutElement component2 = val2.root.GetComponent<LayoutElement>();
			if ((Object)(object)component != (Object)null)
			{
				val.SetPreferredSize(component.preferredWidth, num);
			}
			if ((Object)(object)component2 != (Object)null)
			{
				val2.SetPreferredSize(component2.preferredWidth, num);
			}
		}
		builder.End();
	}

	private static CustomUIElement AddValueButton(CustomUIBuilder builder, string label, Action action, string id, float availableWidth, float minimumWidth)
	{
		//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00da: Unknown result type (might be due to invalid IL or missing references)
		//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0114: Unknown result type (might be due to invalid IL or missing references)
		Action val = Action.op_Implicit(action);
		_valueWindowCallbacks.Add(val);
		builder.AddButton(label, val, id);
		CustomUIElement last = builder.Last;
		if (last == null)
		{
			return null;
		}
		float num = Math.Min(minimumWidth, availableWidth);
		float num2 = 28f;
		HorizontalLayoutGroup component = last.root.GetComponent<HorizontalLayoutGroup>();
		float num3 = (((Object)(object)component != (Object)null) ? ((float)((LayoutGroup)component).padding.horizontal) : 24f);
		float num4 = (((Object)(object)component != (Object)null) ? ((float)((LayoutGroup)component).padding.vertical) : 8f);
		if ((Object)(object)last.text != (Object)null)
		{
			((TMP_Text)last.text).enableAutoSizing = false;
			((TMP_Text)last.text).enableWordWrapping = true;
			Vector2 preferredValues = ((TMP_Text)last.text).GetPreferredValues(label, float.PositiveInfinity, float.PositiveInfinity);
			num = Math.Min(Math.Max(num, Mathf.Ceil(preferredValues.x) + num3), availableWidth);
			num2 = Math.Max(num2, Mathf.Ceil(((TMP_Text)last.text).GetPreferredValues(label, Math.Max(1f, num - num3), float.PositiveInfinity).y) + num4);
		}
		LayoutElement component2 = last.root.GetComponent<LayoutElement>();
		if ((Object)(object)component2 != (Object)null && component2.minWidth > num)
		{
			component2.minWidth = num;
		}
		last.SetPreferredSize(num, num2);
		return last;
	}

	private void FillAppraisalPrice(IntPtr expectedItemPtr, int expectedItemUid)
	{
		try
		{
			if (!_enabledAppraisal.Value || !_enabledFillEstimate.Value)
			{
				return;
			}
			InspectionUIManager instance = InspectionUIManager.Instance;
			TMP_InputField val = SafeGameAccess.ReadAppraisalInput(instance, expectedItemPtr, expectedItemUid);
			if ((Object)(object)val == (Object)null)
			{
				ModLog.Warn("AppraisalInput.Fill", "当前物品已切换或估价框不可编辑，本次未填写");
				return;
			}
			int? num = SafeGameAccess.ReadAppraisalTrueValue(instance.currentInspectingItem);
			if (num.HasValue)
			{
				string text = num.Value.ToString(CultureInfo.InvariantCulture);
				val.SetTextWithoutNotify(text);
				instance.OnValueChanged(text);
				int? num2 = SafeGameAccess.ReadTagInt(instance.currentInspectingItem, "APPRAISAL_DEFAULT_VALUE");
				if (val.text != text || num2 != num)
				{
					ModLog.Warn("AppraisalInput.Fill", "估价填写后未能同步到游戏待提交值");
					return;
				}
				ModLog.MarkOk("AppraisalInput.Fill");
				MelonLogger.Msg("已填入估价: " + text);
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("AppraisalInput.Fill", "无法填入估价: " + ex.GetType().Name + " - " + ex.Message);
		}
	}

	private void EnsureAugDialogueObservation()
	{
		UnityEvent val = SafeGameAccess.ReadAugDialogueEvent();
		UnityEvent val2 = SafeGameAccess.ReadAugDialogueStartEvent();
		bool flag = ((_augDialogueStartEvent == null) ? (val2 == null) : (val2 != null && ((Il2CppObjectBase)_augDialogueStartEvent).Pointer == ((Il2CppObjectBase)val2).Pointer));
		if (val == null || ((_augDialogueEvent != null && ((Il2CppObjectBase)_augDialogueEvent).Pointer == ((Il2CppObjectBase)val).Pointer) & flag))
		{
			return;
		}
		try
		{
			UnregisterAugDialogueObservation();
			if ((Delegate)(object)_augDialogueCallback == (Delegate)null)
			{
				Action action = () =>
				{
					if (_enabledAugAlert != null && _enabledAugAlert.Value)
					{
						StoreClient currentStoreClient = SafeGameAccess.GetCurrentStoreClient();
						if (SafeGameAccess.ReadIsAug(currentStoreClient) == true)
						{
							CaptureAugDialogue(currentStoreClient);
						}
					}
				};
				_augDialogueCallback = UnityAction.op_Implicit(action);
			}
			val.AddListener(_augDialogueCallback);
			_augDialogueEvent = val;
			if (val2 != null)
			{
				if ((Delegate)(object)_augDialogueStartCallback == (Delegate)null)
				{
					Action action2 = () =>
					{
						if (_enabledAugAlert != null && _enabledAugAlert.Value)
						{
							SafeGameAccess.RecordAugDialogueOwner(SafeGameAccess.GetCurrentStoreClient());
						}
					};
					_augDialogueStartCallback = UnityAction.op_Implicit(action2);
				}
				val2.AddListener(_augDialogueStartCallback);
				_augDialogueStartEvent = val2;
			}
			ModLog.MarkOk("AugFeatures.ObserveDialogue");
		}
		catch (Exception ex)
		{
			ModLog.Error("AugFeatures.ObserveDialogue", ex);
		}
	}

	private void UnregisterAugDialogueObservation()
	{
		try
		{
			if (_augDialogueEvent != null && (Delegate)(object)_augDialogueCallback != (Delegate)null)
			{
				_augDialogueEvent.RemoveListener(_augDialogueCallback);
			}
		}
		catch (Exception ex)
		{
			ModLog.Error("AugFeatures.UnregisterDialogue", ex);
		}
		finally
		{
			_augDialogueEvent = null;
		}
		try
		{
			if (_augDialogueStartEvent != null && (Delegate)(object)_augDialogueStartCallback != (Delegate)null)
			{
				_augDialogueStartEvent.RemoveListener(_augDialogueStartCallback);
			}
		}
		catch (Exception ex2)
		{
			ModLog.Error("AugFeatures.UnregisterDialogueStart", ex2);
		}
		finally
		{
			_augDialogueStartEvent = null;
		}
	}

	private static void ResetAugDialogue(IntPtr pointer, string id)
	{
		InspectorState.AugDialogueClientPtr = pointer;
		InspectorState.AugDialogueClientId = id;
		InspectorState.AugDialogueReasons.Clear();
		if (InspectorState.AugActiveDialogueClientPtr != pointer || InspectorState.AugActiveDialogueClientId != id)
		{
			InspectorState.AugActiveDialogueClientPtr = (InspectorState.AugActiveDialoguePtr = IntPtr.Zero);
			InspectorState.AugActiveDialogueClientId = "";
		}
	}

	private static bool CaptureAugDialogue(StoreClient client)
	{
		try
		{
			IntPtr intPtr = ((client != null) ? ((Il2CppObjectBase)client).Pointer : IntPtr.Zero);
			string text = SafeGameAccess.SafeClientId(client);
			if (intPtr != InspectorState.AugDialogueClientPtr || text != InspectorState.AugDialogueClientId)
			{
				ResetAugDialogue(intPtr, text);
			}
			string text2 = SafeGameAccess.ReadAugDisplayedDialogue(client, out var failed);
			if (text2 != null)
			{
				AugExplainer.RememberDialogue(InspectorState.AugDialogueReasons, text2);
			}
			return failed;
		}
		catch (Exception ex)
		{
			ModLog.Error("AugFeatures.CaptureDialogue", ex);
			return true;
		}
	}

	private void RefreshAugInspector(CustomUIManager ui, StoreClient client)
	{
		if (!_enabledAugAlert.Value)
		{
			if (ui.IsOpen("ProbablyStolenInspector.aug"))
			{
				ui.CloseWindow("ProbablyStolenInspector.aug");
			}
			InspectorState.AugLastSig = null;
			InspectorState.AugWantVisible = false;
			InspectorState.AugSuppressed = false;
			InspectorState.AugFailedSig = null;
			ResetAugDialogue(IntPtr.Zero, "");
			UnregisterAugDialogueObservation();
			return;
		}
		EnsureAugDialogueObservation();
		SaveWindowPos(ui, "ProbablyStolenInspector.aug", _augPosX, _augPosY);
		IntPtr intPtr = ((client != null) ? ((Il2CppObjectBase)client).Pointer : IntPtr.Zero);
		string text = SafeGameAccess.SafeClientId(client);
		if (intPtr != InspectorState.AugClientPtr || text != InspectorState.AugClientId)
		{
			InspectorState.AugClientPtr = intPtr;
			InspectorState.AugClientId = text;
			ModLog.Verbose((client != null) ? ("改造体检查 → 客户 #" + text) : "改造体检查 → 无顾客（关窗）");
			InspectorState.AugSuppressed = false;
			InspectorState.AugWantVisible = false;
			InspectorState.AugLastSig = null;
			InspectorState.AugFailedSig = null;
			if (intPtr != InspectorState.AugDialogueClientPtr || text != InspectorState.AugDialogueClientId)
			{
				ResetAugDialogue(intPtr, text);
			}
		}
		if (client == null)
		{
			if (ui.IsOpen("ProbablyStolenInspector.aug"))
			{
				ui.CloseWindow("ProbablyStolenInspector.aug");
			}
			InspectorState.AugLastSig = null;
			InspectorState.AugWantVisible = false;
			InspectorState.AugFailedSig = null;
			ResetAugDialogue(intPtr, text);
			return;
		}
		if (SafeGameAccess.ReadIsAug(client) != true)
		{
			if (ui.IsOpen("ProbablyStolenInspector.aug"))
			{
				ui.CloseWindow("ProbablyStolenInspector.aug");
			}
			InspectorState.AugLastSig = null;
			InspectorState.AugWantVisible = false;
			InspectorState.AugFailedSig = null;
			ResetAugDialogue(intPtr, text);
			return;
		}
		if (InspectorState.AugSuppressed)
		{
			if (ui.IsOpen("ProbablyStolenInspector.aug"))
			{
				ui.CloseWindow("ProbablyStolenInspector.aug");
			}
			InspectorState.AugWantVisible = false;
			return;
		}
		if (InspectorState.AugWantVisible && !ui.IsOpen("ProbablyStolenInspector.aug"))
		{
			InspectorState.AugSuppressed = true;
			InspectorState.AugWantVisible = false;
			return;
		}
		AugReadings augReadings = SafeGameAccess.ReadAugFeatures(client);
		augReadings.DialogueFailed = CaptureAugDialogue(client);
		List<AugReason> reasons = AugExplainer.Explain(augReadings, InspectorState.AugDialogueReasons);
		AugRiskPreview? augRiskPreview = SafeGameAccess.ReadAugRisk();
		bool flag = augRiskPreview.HasValue && augRiskPreview.Value.ExtinctionOnKill;
		bool flag2 = augRiskPreview.HasValue && augRiskPreview.Value.OutbreakOnRelease;
		bool value = _showAugPressure.Value;
		string text2;
		if (value)
		{
			text2 = (augRiskPreview.HasValue ? augRiskPreview.Value.Presence.ToString(CultureInfo.InvariantCulture) : "?");
		}
		else
		{
			text2 = null;
		}
		string text3;
		if (flag)
		{
			text3 = "extinction";
		}
		else
		{
			text3 = (flag2 ? "outbreak" : "base");
		}
		text3 = text3 + "|presence=" + (value ? text2 : "hidden");
		text3 = text3 + "|features=" + AugExplainer.Signature(augReadings, reasons, LocalizationService.IsChinese) + "|screen=" + Screen.width + "x" + Screen.height + "|scale=" + _largeUI.Value;
		if (text3 == InspectorState.AugLastSig && ui.IsOpen("ProbablyStolenInspector.aug"))
		{
			ClampInspectorWindow(ui.GetWindow("ProbablyStolenInspector.aug"));
			InspectorState.AugWantVisible = true;
		}
		else
		{
			if (text3 == InspectorState.AugFailedSig)
			{
				return;
			}
			string text4 = LocalizationService.L("击杀该改造体将导致改造体灭绝", "Killing this client will cause augmented clients to go extinct");
			string text5 = LocalizationService.L("放走该改造体将导致改造体泛滥", "Letting this client leave will cause an augmented outbreak");
			string hint = null;
			if (flag)
			{
				hint = "<color=#BE514C>" + text4 + "</color>";
			}
			else if (flag2)
			{
				hint = "<color=#BE514C>" + text5 + "</color>";
			}
			InspectorState.AugWantVisible = false;
			try
			{
				BuildAugWindow(ui, augReadings, reasons, hint, value ? text2 : null);
				InspectorState.AugLastSig = text3;
				InspectorState.AugFailedSig = null;
				InspectorState.AugWantVisible = true;
			}
			catch
			{
				InspectorState.AugFailedSig = text3;
				InspectorState.AugLastSig = null;
				try
				{
					if (ui.IsOpen("ProbablyStolenInspector.aug"))
					{
						ui.CloseWindow("ProbablyStolenInspector.aug");
					}
				}
				catch (Exception ex)
				{
					ModLog.Error("AugFeatures.CloseFailedWindow", ex);
				}
				throw;
			}
		}
	}

	private void BuildAugWindow(CustomUIManager ui, AugReadings read, List<AugReason> reasons, string hint, string presence)
	{
		//IL_0032: Unknown result type (might be due to invalid IL or missing references)
		//IL_010e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0113: Unknown result type (might be due to invalid IL or missing references)
		//IL_03ce: Unknown result type (might be due to invalid IL or missing references)
		//IL_03f4: Unknown result type (might be due to invalid IL or missing references)
		//IL_0417: Unknown result type (might be due to invalid IL or missing references)
		//IL_044e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0468: Unknown result type (might be due to invalid IL or missing references)
		//IL_0474: Unknown result type (might be due to invalid IL or missing references)
		//IL_0479: Unknown result type (might be due to invalid IL or missing references)
		CustomUIWindow window = ui.GetWindow("ProbablyStolenInspector.aug");
		Vector3? val = ((window != null && window.IsAlive && window.IsVisible) ? new Vector3?(InspectorWindowTopLeftWorld(window.Rect)) : ((Vector3?)null));
		float augWindowHeight = InspectorState.AugWindowHeight;
		float num = (_largeUI.Value ? 1.5f : 1f);
		for (int i = 0; i < 2; i++)
		{
			if (ui.IsOpen("ProbablyStolenInspector.aug"))
			{
				ui.CloseWindow("ProbablyStolenInspector.aug");
			}
			CustomUIBuilder val2 = ui.CreateWindow("ProbablyStolenInspector.aug", LocalizationService.L("警告", "Warning"), "overlay").SetDraggable(true).SetCloseOnEscape(true)
				.SetSize(440f, 260f);
			CustomUIWindow window2 = ui.GetWindow("ProbablyStolenInspector.aug");
			if (window2 == null || (Object)(object)window2.Rect == (Object)null || (Object)(object)window2.ContentRoot == (Object)null)
			{
				throw new InvalidOperationException("改造体窗口预制体未就绪");
			}
			AttachWindowScale(window2);
			Rect val3 = InspectorScreenBounds(window2.Rect);
			float num2 = Math.Max(1f, (val3.width - 16f) / num);
			float num3 = ((i == 0) ? Math.Min(440f, num2) : num2);
			window2.SetSize(num3, 260f);
			VerticalLayoutGroup component = ((Component)window2.Rect).GetComponent<VerticalLayoutGroup>();
			VerticalLayoutGroup component2 = ((Component)window2.ContentRoot).GetComponent<VerticalLayoutGroup>();
			float width = Math.Max(1f, num3 - (((Object)(object)component != (Object)null) ? ((float)((LayoutGroup)component).padding.horizontal) : 16f) - (((Object)(object)component2 != (Object)null) ? ((float)((LayoutGroup)component2).padding.horizontal) : 8f));
			val2.BeginColumn(6f);
			AddValueText(val2, "<color=#BE514C>" + LocalizationService.L("检测到改造体", "Augmented client detected") + "</color>", width);
			if (presence != null)
			{
				AddValueText(val2, LocalizationService.L("改造体压力：", "Augmented pressure: ") + presence, width);
			}
			if (hint != null)
			{
				AddValueText(val2, hint, width);
			}
			AddValueText(val2, LocalizationService.L("检测依据：", "Detection evidence:"), width);
			foreach (AugReason reason in reasons)
			{
				AddValueText(val2, "<color=#E4A7A0>" + LocalizationService.L(reason.Zh, reason.En) + "</color>", width);
			}
			if (reasons.Count == 0)
			{
				AddValueText(val2, LocalizationService.L("暂未发现可核实的明显异常；身份依据为游戏内部判定。", "No obvious clue verified so far; identity is confirmed by the game's internal verdict."), width, rightAligned: false, subdued: true);
			}
			AddAugPendingText(val2, read, width);
			val2.End();
			window2 = val2.Show();
			LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
			FitInspectorWindowTitle(window2);
			LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
			float num4 = Mathf.Ceil(LayoutUtility.GetPreferredHeight(window2.Rect));
			if (num4 <= 0f || float.IsNaN(num4) || float.IsInfinity(num4))
			{
				throw new InvalidOperationException("改造体窗口未能获得有效内容高度");
			}
			if (i != 0 || !(num4 > (val3.height - 16f) / num) || !(num3 < num2 - 1f))
			{
				window2.SetSize(num3, num4);
				LayoutRebuilder.ForceRebuildLayoutImmediate(window2.Rect);
				if (HasSavedPos(_augPosX, _augPosY))
				{
					window2.SetPosition(new Vector2(_augPosX.Value, _augPosY.Value));
				}
				else
				{
					window2.Center();
				}
				CompleteScaledWindow(window2);
				float num5 = num4 * ((Transform)window2.Rect).localScale.y;
				if (val.HasValue)
				{
					KeepInspectorWindowTopLeft(window2.Rect, val.Value);
				}
				else if (HasSavedPos(_augPosX, _augPosY) && augWindowHeight > 0f)
				{
					RectTransform rect = window2.Rect;
					rect.anchoredPosition += new Vector2(0f, (augWindowHeight - num5) * (1f - window2.Rect.pivot.y));
				}
				ClampInspectorWindow(window2);
				InspectorState.AugWindowHeight = num5;
				SaveWindowPos(ui, "ProbablyStolenInspector.aug", _augPosX, _augPosY);
				return;
			}
		}
		throw new InvalidOperationException("改造体窗口未完成布局");
	}

	private static void AddAugPendingText(CustomUIBuilder builder, AugReadings read, float width)
	{
		if (read.Motion == AugMotion.Disabled)
		{
			AddValueText(builder, LocalizationService.L("顾客待机动画已关闭，动作特征暂无法观察。", "Client idle animation is disabled; movement clues cannot be observed."), width, rightAligned: false, subdued: true);
		}
		if (read.Motion == AugMotion.Pending && !read.MotionFailed && read.Portrait == AugPortraitRead.Pending && !read.PortraitFailed)
		{
			AddValueText(builder, LocalizationService.L("等待顾客到达柜台后核对可见特征。", "Waiting for the client to reach the counter before checking visible clues."), width, rightAligned: false, subdued: true);
			return;
		}
		List<string> list = new List<string>();
		if (read.MotionFailed || read.Motion == AugMotion.Unknown)
		{
			list.Add(LocalizationService.L("动作", "movement"));
		}
		if (read.PortraitFailed || read.Portrait == AugPortraitRead.Unknown || (read.PortraitKey != null && read.PortraitKey.StartsWith("aug", StringComparison.Ordinal) && !AugExplainer.HasPortrait(read.PortraitKey)))
		{
			list.Add(LocalizationService.L("外观", "appearance"));
		}
		if (read.IdentityFailed || read.IdentityStyle == null)
		{
			list.Add(LocalizationService.L("身份与服装", "identity/outfit"));
		}
		if (read.DialogueFailed)
		{
			list.Add(LocalizationService.L("台词", "dialogue"));
		}
		if (list.Count > 0)
		{
			AddValueText(builder, LocalizationService.L("暂无法可靠核验：", "Not yet reliably verified: ") + string.Join(LocalizationService.L("、", ", "), list) + LocalizationService.L("。", "."), width, rightAligned: false, subdued: true);
		}
	}

	private void RefreshWantedInspector(CustomUIManager ui, StoreClient client)
	{
		//IL_05cc: Unknown result type (might be due to invalid IL or missing references)
		if (!_enabledWantedAlert.Value)
		{
			if (ui.IsOpen("ProbablyStolenInspector.wanted"))
			{
				ui.CloseWindow("ProbablyStolenInspector.wanted");
			}
			InspectorState.WantedWantVisible = false;
			InspectorState.WantedSuppressed = false;
			InspectorState.WantedLastSig = null;
			return;
		}
		SaveWindowPos(ui, "ProbablyStolenInspector.wanted", _wantedPosX, _wantedPosY);
		IntPtr intPtr = ((client != null) ? ((Il2CppObjectBase)client).Pointer : IntPtr.Zero);
		string text = SafeGameAccess.SafeClientId(client);
		if (intPtr != InspectorState.WantedClientPtr || text != InspectorState.WantedClientId)
		{
			InspectorState.WantedClientPtr = intPtr;
			InspectorState.WantedClientId = text;
			ModLog.Verbose((client != null) ? ("通缉犯检查 → 客户 #" + text) : "通缉犯检查 → 无顾客（关窗）");
			InspectorState.WantedSuppressed = false;
			InspectorState.WantedWantVisible = false;
			InspectorState.WantedLastSig = null;
		}
		if (client == null)
		{
			if (ui.IsOpen("ProbablyStolenInspector.wanted"))
			{
				ui.CloseWindow("ProbablyStolenInspector.wanted");
			}
			InspectorState.WantedWantVisible = false;
			return;
		}
		bool isWanted;
		int reportReward;
		string text2;
		try
		{
			isWanted = client.isWanted;
			reportReward = client.reportReward;
			text2 = SafeGameAccess.SafeString(client.realName);
			ModLog.MarkOk("StoreClient.wantedFields");
		}
		catch (Exception ex)
		{
			ModLog.Warn("StoreClient.wantedFields", "读不到通缉犯字段: " + ex.GetType().Name + " - " + ex.Message);
			if (ui.IsOpen("ProbablyStolenInspector.wanted"))
			{
				ui.CloseWindow("ProbablyStolenInspector.wanted");
			}
			InspectorState.WantedWantVisible = false;
			return;
		}
		if (!isWanted)
		{
			if (ui.IsOpen("ProbablyStolenInspector.wanted"))
			{
				ui.CloseWindow("ProbablyStolenInspector.wanted");
			}
			InspectorState.WantedWantVisible = false;
			InspectorState.WantedLastSig = null;
			return;
		}
		WantedReputationData wantedReputationData = SafeGameAccess.ReadWantedReputation(client);
		if (InspectorState.WantedSuppressed)
		{
			if (ui.IsOpen("ProbablyStolenInspector.wanted"))
			{
				ui.CloseWindow("ProbablyStolenInspector.wanted");
			}
			InspectorState.WantedWantVisible = false;
			return;
		}
		if (InspectorState.WantedWantVisible && !ui.IsOpen("ProbablyStolenInspector.wanted"))
		{
			InspectorState.WantedSuppressed = true;
			InspectorState.WantedWantVisible = false;
			return;
		}
		string text3 = "wanted|" + text2 + "|" + reportReward.ToString(CultureInfo.InvariantCulture) + "|" + wantedReputationData.FactionId + "|" + wantedReputationData.FactionName + "|" + wantedReputationData.HasReputation + "|" + wantedReputationData.Preview.Current.ToString(CultureInfo.InvariantCulture) + "|" + wantedReputationData.Preview.AfterReport.ToString(CultureInfo.InvariantCulture) + "|" + wantedReputationData.Preview.Change.ToString(CultureInfo.InvariantCulture);
		if (text3 == InspectorState.WantedLastSig && ui.IsOpen("ProbablyStolenInspector.wanted"))
		{
			InspectorState.WantedWantVisible = true;
			return;
		}
		InspectorState.WantedLastSig = text3;
		if (ui.IsOpen("ProbablyStolenInspector.wanted"))
		{
			ui.CloseWindow("ProbablyStolenInspector.wanted");
		}
		CustomUIBuilder val = ui.CreateWindow("ProbablyStolenInspector.wanted", LocalizationService.L("警告", "Warning"), "overlay").SetDraggable(true).SetCloseOnEscape(true)
			.SetSize(300f, 220f);
		val.BeginColumn(6f);
		val.AddLabel("<color=#BE514C>" + LocalizationService.L("通缉犯", "WANTED") + "</color>", (string)null);
		val.AddLabel(LocalizationService.L("姓名：", "Name: ") + (string.IsNullOrEmpty(text2) ? "?" : text2), (string)null);
		val.AddLabel(LocalizationService.L("所属势力：", "Faction: ") + (string.IsNullOrEmpty(wantedReputationData.FactionName) ? "?" : wantedReputationData.FactionName), (string)null);
		if (wantedReputationData.HasReputation)
		{
			val.AddLabel(LocalizationService.L("势力声望：", "Faction reputation: ") + wantedReputationData.Preview.Current.ToString(CultureInfo.InvariantCulture), (string)null);
			int change = wantedReputationData.Preview.Change;
			string text4 = ((change > 0) ? "+" : "") + change.ToString(CultureInfo.InvariantCulture);
			val.AddLabel("<color=#BE514C>" + LocalizationService.L("举报后：", "After report: ") + wantedReputationData.Preview.Current.ToString(CultureInfo.InvariantCulture) + " → " + wantedReputationData.Preview.AfterReport.ToString(CultureInfo.InvariantCulture) + " (" + text4 + ")</color>", (string)null);
		}
		else
		{
			val.AddLabel(LocalizationService.L("势力声望：?", "Faction reputation: ?"), (string)null);
			val.AddLabel("<color=#BE514C>" + LocalizationService.L("举报后：?", "After report: ?") + "</color>", (string)null);
		}
		val.AddLabel("<color=#F4C95D>" + LocalizationService.L("举报奖励：$", "Bounty: $") + reportReward.ToString(CultureInfo.InvariantCulture) + "</color>", (string)null);
		val.End();
		if (HasSavedPos(_wantedPosX, _wantedPosY))
		{
			val.SetPosition(new Vector2(_wantedPosX.Value, _wantedPosY.Value));
		}
		else
		{
			val.Center();
		}
		FinishFixedScaledWindow(val.Show());
		InspectorState.WantedWantVisible = true;
	}

	private void BuildSettingsWindow(CustomUIManager ui)
	{
		if (!((Object)(object)ui == (Object)null) && !((Object)(object)ui.windowPrefab == (Object)null) && !((Object)(object)ui.labelPrefab == (Object)null) && !((Object)(object)ui.buttonPrefab == (Object)null) && !((Object)(object)ui.togglePrefab == (Object)null))
		{
			if (ui.IsOpen("ProbablyStolenInspector.settings"))
			{
				ui.CloseWindow("ProbablyStolenInspector.settings");
			}
			CloseSettingsDetailWindows(ui);
			ModLog.Verbose("设置窗口 ← 打开");
			_rootedCallbacks.Clear();
			CustomUIBuilder val = ui.CreateWindow("ProbablyStolenInspector.settings", LocalizationService.L("鉴定设置", "Inspector Settings"), "overlay").SetDraggable(true).SetCloseOnEscape(true)
				.SetSize(400f, 228f);
			val.BeginColumn(8f);
			AddSettingsToggle(val, LocalizationService.L("真假鉴定", "Authenticity check"), _enabledAuthenticity, "tgl_auth");
			AddSettingsParentWithDetails(val, ui, LocalizationService.L("价值鉴定", "Value appraisal"), _enabledAppraisal, "tgl_val", SettingsDetail.Value);
			AddSettingsParentWithDetails(val, ui, LocalizationService.L("改造体警告", "Augmented alert"), _enabledAugAlert, "tgl_aug", SettingsDetail.Augmented);
			AddSettingsToggle(val, LocalizationService.L("通缉犯警告", "Wanted alert"), _enabledWantedAlert, "tgl_wanted");
			AddSettingsParentWithDetails(val, ui, LocalizationService.L("荧光莓品质显示", "Wine berry quality display"), _enabledBerryQuality, "tgl_berry_quality", SettingsDetail.Berry, (bool v) =>
			{
				BerryTooltip.Enabled = v;
			});
			val.End();
			val.Center();
			FinishFixedScaledWindow(val.Show());
		}
	}

	private void AddSettingsToggle(CustomUIBuilder builder, string label, MelonPreferences_Entry<bool> entry, string id, Action<bool> onChanged = null)
	{
		Action<bool> action = (bool v) =>
		{
			entry.Value = v;
			if (onChanged != null)
			{
				onChanged(v);
			}
			MelonPreferences.Save();
		};
		Action<bool> val = Action<bool>.op_Implicit(action);
		_rootedCallbacks.Add(val);
		builder.AddToggle(label, entry.Value, val, id);
	}

	private void AddSettingsParentWithDetails(CustomUIBuilder builder, CustomUIManager ui, string label, MelonPreferences_Entry<bool> entry, string id, SettingsDetail detail, Action<bool> onChanged = null)
	{
		builder.BeginRow(8f);
		AddSettingsToggle(builder, label, entry, id, onChanged);
		if (builder.Last != null)
		{
			builder.Last.SetPreferredSize(235f, 28f);
		}
		Action action = () =>
		{
			BuildSettingsDetailWindow(ui, detail);
		};
		Action val = Action.op_Implicit(action);
		_rootedCallbacks.Add(val);
		builder.AddButton(LocalizationService.L("详细设置…", "Details…"), val, id + "_details");
		if (builder.Last != null)
		{
			builder.Last.SetPreferredSize(105f, 28f);
		}
		builder.End();
	}

	private void BuildSettingsDetailWindow(CustomUIManager ui, SettingsDetail detail)
	{
		if ((Object)(object)ui == (Object)null || (Object)(object)ui.windowPrefab == (Object)null || (Object)(object)ui.togglePrefab == (Object)null || !ui.IsOpen("ProbablyStolenInspector.settings"))
		{
			return;
		}
		CloseSettingsDetailWindows(ui);
		CustomUIBuilder val = ui.CreateWindow(detail switch
		{
			SettingsDetail.Augmented => "ProbablyStolenInspector.settings.augDetails", 
			SettingsDetail.Value => "ProbablyStolenInspector.settings.valueDetails", 
			_ => "ProbablyStolenInspector.settings.berryDetails", 
		}, detail switch
		{
			SettingsDetail.Augmented => LocalizationService.L("改造体警告 · 详细设置", "Augmented alert - Details"), 
			SettingsDetail.Value => LocalizationService.L("价值鉴定 · 详细设置", "Value appraisal - Details"), 
			_ => LocalizationService.L("荧光莓品质 · 详细设置", "Wine berry quality - Details"), 
		}, "overlay").SetDraggable(true).SetCloseOnEscape(true)
			.SetSize(360f, 112f);
		val.BeginColumn(8f);
		switch (detail)
		{
		case SettingsDetail.Value:
			AddSettingsToggle(val, LocalizationService.L("启用填入估价", "Enable Fill estimate"), _enabledFillEstimate, "tgl_value_fill_estimate", (bool v) =>
			{
				InspectorState.ValueLastSig = null;
			});
			break;
		case SettingsDetail.Augmented:
			AddSettingsToggle(val, LocalizationService.L("显示压力值", "Show pressure value"), _showAugPressure, "tgl_aug_pressure");
			break;
		default:
			AddSettingsToggle(val, LocalizationService.L("显示品质值", "Show quality value"), _showBerryQualityValue, "tgl_berry_value", (bool v) =>
			{
				BerryTooltip.ShowQualityValue = v;
			});
			break;
		}
		val.End();
		val.Center();
		FinishFixedScaledWindow(val.Show());
	}

	private static void CloseSettingsDetailWindows(CustomUIManager ui)
	{
		if (!((Object)(object)ui == (Object)null))
		{
			if (ui.IsOpen("ProbablyStolenInspector.settings.valueDetails"))
			{
				ui.CloseWindow("ProbablyStolenInspector.settings.valueDetails");
			}
			if (ui.IsOpen("ProbablyStolenInspector.settings.augDetails"))
			{
				ui.CloseWindow("ProbablyStolenInspector.settings.augDetails");
			}
			if (ui.IsOpen("ProbablyStolenInspector.settings.berryDetails"))
			{
				ui.CloseWindow("ProbablyStolenInspector.settings.berryDetails");
			}
		}
	}

	private static void CloseOrphanSettingsDetails(CustomUIManager ui)
	{
		if ((Object)(object)ui != (Object)null && !ui.IsOpen("ProbablyStolenInspector.settings"))
		{
			CloseSettingsDetailWindows(ui);
		}
	}

	private void CheckMagnifierSettingsTrigger()
	{
		ItemContextHandler current = ItemContextHandler.current;
		if ((Object)(object)current == (Object)null)
		{
			return;
		}
		GameItem val = SafeGameAccess.SafeCurrentContextItem(current);
		IntPtr intPtr = ((val != null) ? ((Il2CppObjectBase)val).Pointer : IntPtr.Zero);
		if (intPtr == InspectorState.ContextItemPtr)
		{
			return;
		}
		InspectorState.ContextItemPtr = intPtr;
		if (val == null)
		{
			return;
		}
		GameItem item = val;
		if (SettingsTrigger.IsTargetMagnifier(SafeGameAccess.SafeItemIdentifier(item)))
		{
			CustomUIManager instance = CustomUIManager.Instance;
			if (!((Object)(object)instance == (Object)null) && !instance.IsOpen("ProbablyStolenInspector.settings"))
			{
				BuildSettingsWindow(instance);
			}
		}
	}

	private static void AttachWindowScale(CustomUIWindow window)
	{
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c3: Expected Obj, but got Unknown
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_0118: Unknown result type (might be due to invalid IL or missing references)
		//IL_0125: Unknown result type (might be due to invalid IL or missing references)
		//IL_012a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0137: Expected Obj, but got Unknown
		if (window != null && !((Object)(object)window.Rect == (Object)null) && !((Object)(object)window.frame == (Object)null) && (!_scaledWindows.TryGetValue(window.id, out var value) || !(((Il2CppObjectBase)value.Window).Pointer == ((Il2CppObjectBase)window).Pointer)))
		{
			Button closeButton = window.frame.closeButton;
			if ((Object)(object)closeButton == (Object)null)
			{
				throw new InvalidOperationException("窗口缺少原生关闭按钮");
			}
			FixInspectorHeaderButton(closeButton);
			if ((Object)(object)window.frame.dragHandle != (Object)null)
			{
				window.frame.dragHandle.ignoreClamp = true;
			}
			GameObject val = new GameObject("ProbablyStolenInspector.Zoom");
			RectTransform val2 = val.AddComponent<RectTransform>();
			((Transform)val2).SetParent(((Component)closeButton).transform.parent, false);
			((Transform)val2).localScale = Vector3.one;
			((Transform)val2).SetSiblingIndex(((Component)closeButton).transform.GetSiblingIndex());
			Image targetGraphic = val.AddComponent<Image>();
			Button val3 = val.AddComponent<Button>();
			((Selectable)val3).targetGraphic = (Graphic)(object)targetGraphic;
			((Selectable)val3).colors = ((Selectable)closeButton).colors;
			((Selectable)val3).navigation = new Navigation
			{
				mode = (Mode)0
			};
			FixInspectorHeaderButton(val3);
			if ((Delegate)(object)_scaleCallback == (Delegate)null)
			{
				Action action = ToggleInspectorWindowScale;
				_scaleCallback = UnityAction.op_Implicit(action);
			}
			((UnityEvent)val3.onClick).AddListener(_scaleCallback);
			ScaledWindow scaledWindow = new ScaledWindow
			{
				Window = window,
				Button = val3
			};
			_scaledWindows[window.id] = scaledWindow;
			UpdateInspectorZoomVisual(scaledWindow);
		}
	}

	private static void FixInspectorHeaderButton(Button button)
	{
		LayoutElement val = ((Component)button).GetComponent<LayoutElement>();
		if ((Object)(object)val == (Object)null)
		{
			val = ((Component)button).gameObject.AddComponent<LayoutElement>();
		}
		LayoutElement val2 = val;
		float minWidth = (val.preferredWidth = 32f);
		val2.minWidth = minWidth;
		LayoutElement val3 = val;
		minWidth = (val.preferredHeight = 32f);
		val3.minHeight = minWidth;
		LayoutElement val4 = val;
		minWidth = (val.flexibleHeight = 0f);
		val4.flexibleWidth = minWidth;
		val.ignoreLayout = false;
	}

	private static float FitInspectorWindowTitle(CustomUIWindow window)
	{
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_010b: Unknown result type (might be due to invalid IL or missing references)
		//IL_011e: Unknown result type (might be due to invalid IL or missing references)
		TextMeshProUGUI titleText = window.frame.titleText;
		if ((Object)(object)titleText == (Object)null)
		{
			return 0f;
		}
		RectTransform val = ((Il2CppObjectBase)((Transform)((TMP_Text)titleText).rectTransform).parent).Cast<RectTransform>();
		HorizontalLayoutGroup component = ((Component)val).GetComponent<HorizontalLayoutGroup>();
		LayoutElement component2 = ((Component)val).GetComponent<LayoutElement>();
		LayoutElement component3 = ((Component)titleText).GetComponent<LayoutElement>();
		if ((Object)(object)component == (Object)null || (Object)(object)component2 == (Object)null || (Object)(object)component3 == (Object)null)
		{
			throw new InvalidOperationException("窗口标题栏与当前游戏预制体不一致");
		}
		float num = Math.Max(32f, component2.preferredHeight);
		((HorizontalOrVerticalLayoutGroup)component).childControlWidth = true;
		((HorizontalOrVerticalLayoutGroup)component).childForceExpandWidth = false;
		((LayoutGroup)component).childAlignment = (TextAnchor)0;
		float minWidth = (component3.preferredWidth = 0f);
		component3.minWidth = minWidth;
		component3.flexibleWidth = 1f;
		((TMP_Text)titleText).enableAutoSizing = false;
		((TMP_Text)titleText).enableWordWrapping = true;
		((TMP_Text)titleText).alignment = (TextAlignmentOptions)257;
		LayoutRebuilder.ForceRebuildLayoutImmediate(window.Rect);
		string text = ((TMP_Text)titleText).text;
		Rect rect = ((TMP_Text)titleText).rectTransform.rect;
		float num3 = (component3.preferredHeight = Mathf.Ceil(((TMP_Text)titleText).GetPreferredValues(text, Math.Max(1f, rect.width), float.PositiveInfinity).y) + 2f);
		component3.flexibleHeight = 0f;
		minWidth = (component2.preferredHeight = Math.Max(32f, num3 + (float)((LayoutGroup)component).padding.vertical));
		component2.minHeight = minWidth;
		component2.flexibleHeight = 0f;
		return Math.Max(0f, component2.preferredHeight - num);
	}

	private static void FinishFixedScaledWindow(CustomUIWindow window)
	{
		//IL_003b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_004e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		try
		{
			AttachWindowScale(window);
			if (window != null && !((Object)(object)window.Rect == (Object)null))
			{
				float num = FitInspectorWindowTitle(window);
				if (num > 0f)
				{
					Rect rect = window.Rect.rect;
					float width = rect.width;
					rect = window.Rect.rect;
					window.SetSize(width, rect.height + num);
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(window.Rect);
				CompleteScaledWindow(window);
				ModLog.MarkOk("WindowScale.Create");
			}
		}
		catch (Exception ex)
		{
			ModLog.Error("WindowScale.Create", ex);
		}
	}

	private static void CompleteScaledWindow(CustomUIWindow window)
	{
		if (_scaledWindows.TryGetValue(window.id, out var value))
		{
			value.Ready = true;
		}
		ApplyInspectorWindowScale();
	}

	private static void ToggleInspectorWindowScale()
	{
		try
		{
			_largeUI.Value = !_largeUI.Value;
			ApplyInspectorWindowScale();
			InspectorState.ValueLastSig = null;
			InspectorState.FakeLastSig = null;
			InspectorState.FakeFailedSig = null;
			InspectorState.AugLastSig = null;
			InspectorState.AugFailedSig = null;
			CustomUIManager instance = CustomUIManager.Instance;
			SaveInspectorScaledPositions(instance);
			MelonPreferences.Save();
			ModLog.MarkOk("WindowScale.Toggle");
		}
		catch (Exception ex)
		{
			ModLog.Error("WindowScale.Toggle", ex);
		}
	}

	private static void TickInspectorWindowScale(CustomUIManager ui)
	{
		try
		{
			List<string> list = new List<string>();
			foreach (KeyValuePair<string, ScaledWindow> scaledWindow in _scaledWindows)
			{
				if (!IsScaledWindowAlive(scaledWindow.Value))
				{
					list.Add(scaledWindow.Key);
				}
			}
			foreach (string item in list)
			{
				_scaledWindows.Remove(item);
			}
			if (_scaledWindows.Count > 0 && Time.unscaledTime >= _nextZoomScan && ((Object)(object)_zoomNormal == (Object)null || (Object)(object)_zoomHover == (Object)null || (Object)(object)_zoomPressed == (Object)null) && PlayerStore.IsInstanceExist())
			{
				_nextZoomScan = Time.unscaledTime + 2f;
				foreach (Object item2 in (Il2CppArrayBase<Object>)(object)Resources.FindObjectsOfTypeAll(Il2CppType.Of<Sprite>()))
				{
					Sprite val = ((Il2CppObjectBase)item2).TryCast<Sprite>();
					if (!((Object)(object)val == (Object)null))
					{
						switch (((Object)val).name)
						{
						case "button_zoom_0":
							_zoomNormal = val;
							break;
						case "button_zoom_1":
							_zoomHover = val;
							break;
						case "button_zoom_2":
							_zoomPressed = val;
							break;
						}
					}
				}
				foreach (ScaledWindow value in _scaledWindows.Values)
				{
					UpdateInspectorZoomVisual(value);
				}
			}
			if (!UIDragHandler.isDraggingAnyUI)
			{
				ApplyInspectorWindowScale();
			}
			SaveInspectorScaledPositions(ui);
			ModLog.MarkOk("WindowScale.Tick");
		}
		catch (Exception ex)
		{
			ModLog.Error("WindowScale.Tick", ex);
		}
	}

	private static bool IsScaledWindowAlive(ScaledWindow entry)
	{
		return entry.Window != null && entry.Window.IsAlive && entry.Window.IsVisible;
	}

	private static void ApplyInspectorWindowScale()
	{
		//IL_0062: Unknown result type (might be due to invalid IL or missing references)
		//IL_0067: Unknown result type (might be due to invalid IL or missing references)
		//IL_0078: Unknown result type (might be due to invalid IL or missing references)
		//IL_007d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0095: Unknown result type (might be due to invalid IL or missing references)
		//IL_009a: Unknown result type (might be due to invalid IL or missing references)
		//IL_012b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0130: Unknown result type (might be due to invalid IL or missing references)
		//IL_013b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0155: Unknown result type (might be due to invalid IL or missing references)
		//IL_0190: Unknown result type (might be due to invalid IL or missing references)
		//IL_0195: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c8: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
		//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
		float num = (_largeUI.Value ? 1.5f : 1f);
		Rect rect2;
		foreach (ScaledWindow value in _scaledWindows.Values)
		{
			if (value.Ready && IsScaledWindowAlive(value))
			{
				RectTransform rect = value.Window.Rect;
				Rect val = InspectorScreenBounds(rect);
				float val2 = num;
				float num2 = val.width - 16f;
				rect2 = rect.rect;
				float val3 = num2 / rect2.width;
				float num3 = val.height - 16f;
				rect2 = rect.rect;
				num = Math.Min(val2, Math.Min(val3, num3 / rect2.height));
			}
		}
		num = Math.Max(0.1f, num);
		foreach (ScaledWindow value2 in _scaledWindows.Values)
		{
			if (value2.Ready && IsScaledWindowAlive(value2))
			{
				RectTransform rect3 = value2.Window.Rect;
				Vector3 topWorld = InspectorWindowTopLeftWorld(rect3);
				((Transform)rect3).localScale = new Vector3(num, num, 1f);
				if (value2.Applied)
				{
					KeepInspectorWindowTopLeft(rect3, topWorld);
				}
				value2.Applied = true;
				ClampInspectorWindow(value2.Window);
				if (value2.Window.id == "ProbablyStolenInspector.value")
				{
					rect2 = rect3.rect;
					InspectorState.ValueWindowHeight = rect2.height * num;
				}
				if (value2.Window.id == "ProbablyStolenInspector.counterfeit")
				{
					rect2 = rect3.rect;
					InspectorState.FakeWindowHeight = rect2.height * num;
				}
				if (value2.Window.id == "ProbablyStolenInspector.aug")
				{
					rect2 = rect3.rect;
					InspectorState.AugWindowHeight = rect2.height * num;
				}
			}
		}
	}

	private static void SaveInspectorScaledPositions(CustomUIManager ui)
	{
		if (!((Object)(object)ui == (Object)null))
		{
			SaveWindowPos(ui, "ProbablyStolenInspector.counterfeit", _posX, _posY);
			SaveWindowPos(ui, "ProbablyStolenInspector.value", _valPosX, _valPosY);
			SaveWindowPos(ui, "ProbablyStolenInspector.aug", _augPosX, _augPosY);
			SaveWindowPos(ui, "ProbablyStolenInspector.wanted", _wantedPosX, _wantedPosY);
		}
	}

	private static Vector3 InspectorWindowTopLeftWorld(RectTransform rect)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		Rect rect2 = rect.rect;
		float xMin = rect2.xMin;
		rect2 = rect.rect;
		return ((Transform)rect).TransformPoint(new Vector3(xMin, rect2.yMax, 0f));
	}

	private static void KeepInspectorWindowTopLeft(RectTransform rect, Vector3 topWorld)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_0014: Unknown result type (might be due to invalid IL or missing references)
		//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		//IL_001e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Unknown result type (might be due to invalid IL or missing references)
		//IL_002b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0031: Unknown result type (might be due to invalid IL or missing references)
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		Vector3 val = ((Transform)rect).parent.InverseTransformPoint(topWorld) - ((Transform)rect).parent.InverseTransformPoint(InspectorWindowTopLeftWorld(rect));
		rect.anchoredPosition += new Vector2(val.x, val.y);
	}

	private static Rect InspectorScreenBounds(RectTransform rect)
	{
		//IL_0040: Unknown result type (might be due to invalid IL or missing references)
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_005b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0060: Unknown result type (might be due to invalid IL or missing references)
		//IL_0065: Unknown result type (might be due to invalid IL or missing references)
		//IL_006a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_0086: Unknown result type (might be due to invalid IL or missing references)
		//IL_008b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0090: Unknown result type (might be due to invalid IL or missing references)
		//IL_0092: Unknown result type (might be due to invalid IL or missing references)
		//IL_0099: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00de: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
		//IL_0022: Unknown result type (might be due to invalid IL or missing references)
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
		RectTransform val = ((Il2CppObjectBase)((Transform)rect).parent).Cast<RectTransform>();
		Canvas componentInParent = ((Component)rect).GetComponentInParent<Canvas>();
		if ((Object)(object)componentInParent == (Object)null)
		{
			return val.rect;
		}
		RectTransform val2 = ((Il2CppObjectBase)((Component)componentInParent.rootCanvas).transform).Cast<RectTransform>();
		Rect rect2 = val2.rect;
		Vector3 val3 = ((Transform)val).InverseTransformPoint(((Transform)val2).TransformPoint(new Vector3(rect2.xMin, rect2.yMin, 0f)));
		Vector3 val4 = ((Transform)val).InverseTransformPoint(((Transform)val2).TransformPoint(new Vector3(rect2.xMax, rect2.yMax, 0f)));
		return Rect.MinMaxRect(Math.Min(val3.x, val4.x), Math.Min(val3.y, val4.y), Math.Max(val3.x, val4.x), Math.Max(val3.y, val4.y));
	}

	private static void ClampInspectorWindow(CustomUIWindow window)
	{
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0033: Unknown result type (might be due to invalid IL or missing references)
		//IL_003c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_004b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Unknown result type (might be due to invalid IL or missing references)
		//IL_005e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0063: Unknown result type (might be due to invalid IL or missing references)
		//IL_0068: Unknown result type (might be due to invalid IL or missing references)
		//IL_006d: Unknown result type (might be due to invalid IL or missing references)
		//IL_0076: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0085: Unknown result type (might be due to invalid IL or missing references)
		//IL_008a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0098: Unknown result type (might be due to invalid IL or missing references)
		//IL_009d: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
		//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
		//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
		//IL_0105: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0159: Unknown result type (might be due to invalid IL or missing references)
		//IL_011a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0162: Unknown result type (might be due to invalid IL or missing references)
		//IL_0168: Unknown result type (might be due to invalid IL or missing references)
		//IL_0143: Unknown result type (might be due to invalid IL or missing references)
		//IL_019a: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a0: Unknown result type (might be due to invalid IL or missing references)
		//IL_0191: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
		//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
		//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01c9: Unknown result type (might be due to invalid IL or missing references)
		if (window != null && window.IsAlive && !((Object)(object)window.Rect == (Object)null))
		{
			RectTransform rect = window.Rect;
			Rect val = InspectorScreenBounds(rect);
			Transform parent = ((Transform)rect).parent;
			Rect rect2 = rect.rect;
			float xMin = rect2.xMin;
			rect2 = rect.rect;
			Vector3 val2 = parent.InverseTransformPoint(((Transform)rect).TransformPoint(new Vector3(xMin, rect2.yMin, 0f)));
			Transform parent2 = ((Transform)rect).parent;
			rect2 = rect.rect;
			float xMax = rect2.xMax;
			rect2 = rect.rect;
			Vector3 val3 = parent2.InverseTransformPoint(((Transform)rect).TransformPoint(new Vector3(xMax, rect2.yMax, 0f)));
			float num;
			if (val2.x < val.xMin + 8f)
			{
				num = val.xMin + 8f - val2.x;
			}
			else
			{
				num = ((val3.x > val.xMax - 8f) ? (val.xMax - 8f - val3.x) : 0f);
			}
			float num2;
			if (val3.y > val.yMax - 8f)
			{
				num2 = val.yMax - 8f - val3.y;
			}
			else
			{
				num2 = ((val2.y < val.yMin + 8f) ? (val.yMin + 8f - val2.y) : 0f);
			}
			if (val3.y - val2.y > val.height - 16f)
			{
				num2 = val.yMax - 8f - val3.y;
			}
			if (val3.x - val2.x > val.width - 16f)
			{
				num = val.xMin + 8f - val2.x;
			}
			rect.anchoredPosition += new Vector2(num, num2);
		}
	}

	private static void UpdateInspectorZoomVisual(ScaledWindow entry)
	{
		//IL_0132: Unknown result type (might be due to invalid IL or missing references)
		//IL_007b: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_019e: Unknown result type (might be due to invalid IL or missing references)
		//IL_01a5: Expected Obj, but got Unknown
		//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
		//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
		//IL_01df: Unknown result type (might be due to invalid IL or missing references)
		//IL_0203: Unknown result type (might be due to invalid IL or missing references)
		//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
		//IL_0106: Unknown result type (might be due to invalid IL or missing references)
		//IL_0117: Expected Obj, but got Unknown
		//IL_0257: Unknown result type (might be due to invalid IL or missing references)
		//IL_0281: Unknown result type (might be due to invalid IL or missing references)
		Image component = ((Component)entry.Button).GetComponent<Image>();
		if ((Object)(object)_zoomNormal != (Object)null)
		{
			for (int num = ((Component)entry.Button).transform.childCount - 1; num >= 0; num--)
			{
				GameObject gameObject = ((Component)((Component)entry.Button).transform.GetChild(num)).gameObject;
				gameObject.SetActive(false);
				Object.Destroy((Object)(object)gameObject);
			}
			component.sprite = _zoomNormal;
			((Graphic)component).color = Color.white;
			component.preserveAspect = true;
			((Selectable)entry.Button).transition = (Transition)2;
			((Selectable)entry.Button).spriteState = new SpriteState
			{
				m_HighlightedSprite = (((Object)(object)_zoomHover != (Object)null) ? _zoomHover : _zoomNormal),
				m_SelectedSprite = (((Object)(object)_zoomHover != (Object)null) ? _zoomHover : _zoomNormal),
				m_PressedSprite = (((Object)(object)_zoomPressed != (Object)null) ? _zoomPressed : _zoomNormal),
				m_DisabledSprite = _zoomNormal
			};
			return;
		}
		((Graphic)component).color = new Color(0.65f, 0.68f, 0.62f, 1f);
		((Selectable)entry.Button).transition = (Transition)1;
		if (((Component)entry.Button).transform.childCount > 0)
		{
			return;
		}
		float[] array = new float[2] { 20f, 12f };
		foreach (float num2 in array)
		{
			for (int j = 0; j < 4; j++)
			{
				GameObject val = new GameObject("ZoomOutline");
				RectTransform val2 = val.AddComponent<RectTransform>();
				((Transform)val2).SetParent(((Component)entry.Button).transform, false);
				Vector2 anchorMin = (val2.anchorMax = new Vector2(0.5f, 0.5f));
				val2.anchorMin = anchorMin;
				val2.sizeDelta = ((j < 2) ? new Vector2(num2, 2f) : new Vector2(2f, num2));
				val2.anchoredPosition = new Vector2(j switch
				{
					3 => num2 / 2f, 
					2 => (0f - num2) / 2f, 
					_ => 0f, 
				}, j switch
				{
					1 => num2 / 2f, 
					0 => (0f - num2) / 2f, 
					_ => 0f, 
				});
				Image val4 = val.AddComponent<Image>();
				((Graphic)val4).color = new Color(0.18f, 0.15f, 0.19f, 1f);
				((Graphic)val4).raycastTarget = false;
			}
		}
	}
}
internal sealed class AuthenticityReadings
{
	internal readonly Dictionary<string, int?> Numbers = new Dictionary<string, int?>();

	internal readonly Dictionary<string, string> Text = new Dictionary<string, string>();

	internal int? Number(string key)
	{
		int? value;
		return Numbers.TryGetValue(key, out value) ? value : ((int?)null);
	}

	internal string String(string key)
	{
		string value;
		return Text.TryGetValue(key, out value) ? value : null;
	}
}
internal sealed class AuthenticityText
{
	internal readonly string Zh;

	internal readonly string En;

	internal readonly bool Verified;

	internal AuthenticityText(string zh, string en, bool verified = true)
	{
		Zh = zh;
		En = en;
		Verified = verified;
	}
}
internal sealed class AuthenticityDetail
{
	internal AuthenticityText Standard;

	internal readonly List<AuthenticityText> Reasons = new List<AuthenticityText>();
}
internal static class AuthenticityExplainer
{
	private static readonly HashSet<int> Recalled = new HashSet<int> { 250, 251, 301, 355, 385, 386, 421 };

	internal const string Company = "Carmicheal Pharmaceutical";

	internal const string Model = "Immunivax";

	internal static AuthenticityDetail Explain(int type, string issue, bool? bad, AuthenticityReadings data)
	{
		data = data ?? new AuthenticityReadings();
		AuthenticityDetail authenticityDetail = new AuthenticityDetail();
		switch (type)
		{
		case 0:
			Injector(authenticityDetail, issue, data, bad == true);
			break;
		case 1:
			Cigarette(authenticityDetail, issue, data, bad == true);
			break;
		case 2:
			Stamp(authenticityDetail, issue, data, bad == true);
			break;
		}
		if (bad == true && authenticityDetail.Reasons.Count == 0)
		{
			authenticityDetail.Reasons.Add(new AuthenticityText("游戏判定该项异常，具体表现待核验。", "The game flags this check; the specific cause is pending verification.", verified: false));
		}
		else if (!bad.HasValue)
		{
			authenticityDetail.Reasons.Add(new AuthenticityText("本项判定暂时读不到。", "This check could not be read.", verified: false));
		}
		return authenticityDetail;
	}

	private static void Rule(AuthenticityDetail r, string zh, string en)
	{
		r.Standard = new AuthenticityText(zh, en);
	}

	private static void Reason(AuthenticityDetail r, string zh, string en)
	{
		r.Reasons.Add(new AuthenticityText(zh, en));
	}

	private static string N(int value)
	{
		return value.ToString(CultureInfo.InvariantCulture);
	}

	internal static string Display(string value)
	{
		if (value == null)
		{
			return "?";
		}
		StringBuilder stringBuilder = new StringBuilder();
		int num = Math.Min(80, value.Length);
		for (int i = 0; i < num; i++)
		{
			char c = value[i];
			switch (c)
			{
			case '<':
				stringBuilder.Append("\\u003C");
				continue;
			case '>':
				stringBuilder.Append("\\u003E");
				continue;
			case '\n':
				stringBuilder.Append("\\n");
				continue;
			case '\r':
				stringBuilder.Append("\\r");
				continue;
			}
			if (char.IsControl(c))
			{
				stringBuilder.Append(' ');
			}
			else
			{
				stringBuilder.Append(c);
			}
		}
		if (value.Length > num)
		{
			stringBuilder.Append("...");
		}
		return stringBuilder.ToString();
	}

	private static void Injector(AuthenticityDetail r, string issue, AuthenticityReadings d, bool bad)
	{
		switch (issue)
		{
		case "ISSUE_INDICATOR":
			Rule(r, "说明：绿灯正常；橙灯表示过期，红灯表示已空，均不可使用。", "Explanation: green is normal; orange means expired and red means spent. Neither is usable.");
			if (bad)
			{
				if (d.Number("INJECTOR_INDICATOR") == 1)
				{
					Reason(r, "指示灯为橙色：注射器已过期。", "The indicator is orange: the injector has expired.");
				}
				else if (d.Number("INJECTOR_INDICATOR") == 3)
				{
					Reason(r, "指示灯为红色：注射器已空。", "The indicator is red: the injector is spent.");
				}
			}
			break;
		case "ISSUE_LABEL":
			Rule(r, "说明：公司为 " + "Carmicheal Pharmaceutical".ToUpperInvariant() + "，产品为 " + "Immunivax".ToUpperInvariant() + "；名称应完整，拼写和字体应与示例一致。", "Explanation: company " + "Carmicheal Pharmaceutical".ToUpperInvariant() + "; product " + "Immunivax".ToUpperInvariant() + ". Both names must be complete, with matching spelling and font.");
			if (bad)
			{
				Label(r, d.String("INJECTOR_COMPANY"), "Carmicheal Pharmaceutical", "公司名称", "Company name");
				Label(r, d.String("INJECTOR_MODEL"), "Immunivax", "产品名称", "Product name");
			}
			break;
		case "ISSUE_SERIAL":
			Rule(r, "说明：7位数字，范围1250000–4559999；前3位为批次，250、251、301、355、385、386、421已召回。", "Explanation: seven digits, 1250000–4559999. The first three digits are the batch; 250, 251, 301, 355, 385, 386 and 421 are recalled.");
			if (bad)
			{
				Serial(r, d.String("INJECTOR_SERIAL"));
			}
			break;
		case "ISSUE_BODY":
		{
			Rule(r, "说明：瓶体必须完好，明显裂纹或任何损坏均使注射器不可使用。", "Explanation: the body must be intact. Visible cracks or any damage make the injector unusable.");
			if (!bad)
			{
				break;
			}
			int? num = d.Number("INJECTOR_BODY");
			int? num2 = num;
			if (num2.HasValue)
			{
				switch (num2.GetValueOrDefault())
				{
				case 1:
					Reason(r, "瓶体有明显裂纹。", "The body has a visible crack.");
					break;
				case 2:
					Reason(r, "瓶体底部破损。", "The bottom of the body is damaged.");
					break;
				case 3:
					Reason(r, "瓶体上缘有缺口。", "The upper edge of the body is chipped.");
					break;
				}
			}
			break;
		}
		case "ISSUE_COLOR":
			Rule(r, "说明：主色必须与“正确主色”色卡一致。", "Explanation: the main color must match the correct-color swatch.");
			if (bad)
			{
				if (d.Number("INJECTOR_COLOR") == 0)
				{
					Reason(r, "瓶体呈异常的黄绿色，与标准色卡不符。", "The body has an incorrect yellow-green tint.");
				}
				else if (d.Number("INJECTOR_COLOR") == 1)
				{
					Reason(r, "瓶体呈异常的蓝青色，与标准色卡不符。", "The body has an incorrect blue-cyan tint.");
				}
			}
			break;
		}
	}

	private static void Label(AuthenticityDetail r, string actual, string correct, string zh, string en)
	{
		if (actual == null)
		{
			r.Reasons.Add(new AuthenticityText(zh + "读不到，对应表现待核验。", en + " could not be read; details pending verification.", verified: false));
		}
		else if (!string.Equals(actual, correct, StringComparison.OrdinalIgnoreCase))
		{
			if (actual.Length == 0)
			{
				Reason(r, zh + "缺失。", en + " is missing.");
				return;
			}
			string text = Display(actual.ToUpperInvariant());
			Reason(r, zh + "为“" + text + "”，拼写或内容不符。", en + " is \"" + text + "\": spelling or wording does not match.");
		}
	}

	private static void Serial(AuthenticityDetail r, string serial)
	{
		if (serial == null)
		{
			return;
		}
		if (serial.Length == 0)
		{
			Reason(r, "序列号缺失。", "The serial number is missing.");
			return;
		}
		bool flag = true;
		foreach (char c in serial)
		{
			if (c < '0' || c > '9')
			{
				flag = false;
			}
		}
		string text = "“" + Display(serial) + "”";
		string text2 = "\"" + Display(serial) + "\"";
		if (serial.Length != 7)
		{
			Reason(r, "序列号" + text + "共" + N(serial.Length) + "位，应为7位。", "Serial " + text2 + " has " + N(serial.Length) + " characters; seven are required.");
		}
		if (!flag)
		{
			Reason(r, "序列号" + text + "含非数字字符。", "Serial " + text2 + " contains non-digit characters.");
		}
		if (serial.Length != 7 || !flag)
		{
			return;
		}
		int num = int.Parse(serial, CultureInfo.InvariantCulture);
		if (num < 1250000 || num > 4559999)
		{
			Reason(r, "序列号" + text + "超出有效范围。", "Serial " + text2 + " is outside the valid range.");
			return;
		}
		int num2 = num / 10000;
		if (Recalled.Contains(num2))
		{
			Reason(r, "序列号" + text + "属于已召回批次" + N(num2) + "，不可使用。", "Serial " + text2 + " belongs to recalled batch " + N(num2) + " and is unusable.");
		}
	}

	private static void Cigarette(AuthenticityDetail r, string issue, AuthenticityReadings d, bool bad)
	{
		switch (issue)
		{
		case "CIGARETTE_LETTERING_ISSUE":
		{
			Rule(r, "说明：Hemingway字样的拼写、字体、颜色、位置及印刷质量应与正品一致。", "Explanation: the Hemingway spelling, font, color, placement and print quality must match the genuine example.");
			if (!bad)
			{
				break;
			}
			int? num3 = d.Number("CIGARETTE_LETTERING");
			int? num4 = num3;
			if (num4.HasValue)
			{
				switch (num4.GetValueOrDefault())
				{
				case -1:
				case 6:
					Reason(r, "包装品牌字样缺失。", "The brand lettering is missing.");
					break;
				case 1:
					Reason(r, "品牌字样换成了全大写的方块字体。", "The brand uses an incorrect uppercase block font.");
					break;
				case 2:
				case 3:
				case 4:
				case 5:
				case 7:
					Reason(r, "品牌字样的字母缺失或被替换，与Hemingway不符。", "Letters are missing or replaced in the Hemingway name.");
					break;
				case 0:
					break;
				}
			}
			break;
		}
		case "CIGARETTE_COLOR_ISSUE":
			Rule(r, "说明：包装颜色应与海明威正品色卡一致。", "Explanation: the packaging color must match the Hemingway swatch.");
			if (bad)
			{
				CigaretteColor(r, d.Number("CIGARETTE_BOX"));
			}
			break;
		case "CIGARETTE_SEAL_ISSUE":
			Rule(r, "说明：出售时必须有完整防伪标；缺失或破损均不合格。", "Explanation: a complete security seal is required. A missing or damaged seal is invalid.");
			if (bad)
			{
				if (d.Number("CIGARETTE_SEAL") == -1)
				{
					Reason(r, "防伪标缺失。", "The security seal is missing.");
				}
				else if (d.Number("CIGARETTE_SEAL") == 1)
				{
					Reason(r, "防伪标已撕开或破损。", "The security seal is torn or damaged.");
				}
			}
			break;
		case "CIGARETTE_LOGO_ISSUE":
		{
			Rule(r, "说明：商标的图案和方向应与正品一致。", "Explanation: the logo design and orientation must match the genuine example.");
			if (!bad)
			{
				break;
			}
			int? num = d.Number("CIGARETTE_LOGO");
			int? num2 = num;
			if (num2.HasValue)
			{
				switch (num2.GetValueOrDefault())
				{
				case -1:
					Reason(r, "商标缺失。", "The logo is missing.");
					break;
				case 1:
					Reason(r, "商标被简化，缺少标准图案细节。", "The logo is simplified and lacks standard details.");
					break;
				case 2:
				case 3:
					Reason(r, "商标添加了不属于正品的装饰。", "The logo has extra decorations absent from the genuine design.");
					break;
				case 4:
					Reason(r, "商标倒置，旋转了180度。", "The logo is upside down, rotated 180 degrees.");
					break;
				case 0:
					break;
				}
			}
			break;
		}
		case "CIGARETTE_BOX_ISSUE":
			Rule(r, "说明：包装外观应与正品一致；本类样品的包装异常也可能表现为颜色偏差。", "Explanation: the package must match the genuine example. This sample's box issue may be a color difference.");
			if (bad)
			{
				CigaretteColor(r, d.Number("CIGARETTE_BOX"));
			}
			break;
		}
	}

	private static void CigaretteColor(AuthenticityDetail r, int? box)
	{
		int? num = box;
		if (num.HasValue)
		{
			switch (num.GetValueOrDefault())
			{
			case 1:
				Reason(r, "包装颜色偏浅。", "The packaging is too pale.");
				break;
			case 2:
				Reason(r, "包装颜色偏暗。", "The packaging is too dark.");
				break;
			case 3:
				Reason(r, "包装颜色偏红。", "The packaging has a red tint.");
				break;
			case 4:
				Reason(r, "包装颜色偏橙。", "The packaging has an orange tint.");
				break;
			}
		}
	}

	private static void Stamp(AuthenticityDetail r, string issue, AuthenticityReadings d, bool bad)
	{
		switch (issue)
		{
		case "STAMP_LETTERING_ISSUE":
		{
			Rule(r, "说明：应为REPUBLIC / SOCIAL / PROGRAM三行，拼写、字体、颜色及印刷应一致。", "Explanation: three lines, REPUBLIC / SOCIAL / PROGRAM, with matching spelling, font, color and print.");
			if (!bad)
			{
				break;
			}
			int? num7 = d.Number("STAMP_LETTERING");
			int? num8 = num7;
			if (num8.HasValue)
			{
				switch (num8.GetValueOrDefault())
				{
				case -1:
					Reason(r, "票面文字全部缺失。", "All printed lettering is missing.");
					break;
				case 1:
				case 6:
				case 7:
				case 8:
					Reason(r, "票面文字字体与标准不同。", "The lettering uses a different font.");
					break;
				case 2:
					Reason(r, "错拼为SOCAL / PROGRM，分别缺少I和A。", "SOCAL / PROGRM omit I and A from SOCIAL / PROGRAM.");
					break;
				case 3:
					Reason(r, "缺少第一行REPUBLIC。", "The first line, REPUBLIC, is missing.");
					break;
				case 4:
					Reason(r, "票面文字呈紫色，与标准印刷颜色不同。", "The purple lettering does not match the standard print color.");
					break;
				case 5:
					Reason(r, "字母印刷有明显缺损、不连续。", "The printed letters have visible gaps and broken strokes.");
					break;
				case 0:
					break;
				}
			}
			break;
		}
		case "STAMP_LOGO_ISSUE":
			Rule(r, "说明：商标必须存在，且图案应与正品示例一致。", "Explanation: the logo must be present and match the genuine example.");
			if (bad)
			{
				int? num3 = d.Number("STAMP_LOGO");
				if (num3 == -1)
				{
					Reason(r, "商标缺失。", "The logo is missing.");
				}
				else if (num3 >= 1 && num3 <= 5)
				{
					Reason(r, "商标图案与正品示例不同。", "The logo design differs from the genuine example.");
				}
			}
			break;
		case "STAMP_SEAL_ISSUE":
		{
			Rule(r, "说明：印制面值≥160信用点必须有标准防伪标；低于160则不应有防伪标。", "Explanation: a printed amount of at least 160 credits requires the standard seal; lower amounts have no seal.");
			if (!bad)
			{
				break;
			}
			int? num4 = d.Number("STAMP_AMOUNT");
			int? num5 = d.Number("STAMP_SEAL");
			if (num4.HasValue && num5.HasValue)
			{
				if (num4 >= 160 && num5 == -1)
				{
					Reason(r, "面值" + N(num4.Value) + "信用点，却没有防伪标。", "The printed amount is " + N(num4.Value) + " credits, but the seal is missing.");
				}
				else if (num4 >= 160 && num5 == 1)
				{
					Reason(r, "防伪标使用了非标准的全息图案。", "The seal uses a nonstandard holographic design.");
				}
				else if (num4 < 160 && (num5 == 0 || num5 == 1))
				{
					Reason(r, "面值" + N(num4.Value) + "信用点，却印有防伪标。", "The printed amount is " + N(num4.Value) + " credits, but a seal is present.");
				}
			}
			break;
		}
		case "STAMP_AMOUNT_ISSUE":
			Rule(r, "说明：仅发行40、80、120、160、200、240信用点六种面值。", "Explanation: the only issued amounts are 40, 80, 120, 160, 200 and 240 credits.");
			if (bad)
			{
				int? num6 = d.Number("STAMP_AMOUNT");
				if (num6.HasValue && num6 != 40 && num6 != 80 && num6 != 120 && num6 != 160 && num6 != 200 && num6 != 240)
				{
					Reason(r, "印制面值" + N(num6.Value) + "信用点，不属于发行面值。", "The printed amount, " + N(num6.Value) + " credits, is not an issued denomination.");
				}
			}
			break;
		case "STAMP_CHIP_ISSUE":
		{
			Rule(r, "说明：所有食品券均有芯片；缺失或损坏，即使真券也无法使用。颜色应对照标准图。", "Explanation: every food stamp has a chip. Missing or damaged chips make even genuine stamps unusable. Compare its color with the standard image.");
			if (!bad)
			{
				break;
			}
			int? num = d.Number("STAMP_CHIP");
			int? num2 = num;
			if (num2.HasValue)
			{
				switch (num2.GetValueOrDefault())
				{
				case -1:
					Reason(r, "芯片缺失，票券无法使用。", "The chip is missing; the stamp cannot be used.");
					break;
				case 1:
					Reason(r, "芯片局部破损，票券无法使用。", "Part of the chip is damaged; the stamp cannot be used.");
					break;
				case 2:
					Reason(r, "芯片有贯穿裂纹，票券无法使用。", "The chip has a crack through it; the stamp cannot be used.");
					break;
				case 3:
					Reason(r, "芯片呈异常的深暗色，与标准颜色不符。", "The chip has an incorrect dark color.");
					break;
				case 0:
					break;
				}
			}
			break;
		}
		}
	}
}
internal sealed class AppraisalValueLine
{
	internal readonly string Zh;

	internal readonly string En;

	internal readonly int Amount;

	internal readonly bool IsBase;

	internal readonly string NoteZh;

	internal readonly string NoteEn;

	internal AppraisalValueLine(string zh, string en, int amount, bool isBase = false, string noteZh = null, string noteEn = null)
	{
		Zh = zh;
		En = en;
		Amount = amount;
		IsBase = isBase;
		NoteZh = noteZh;
		NoteEn = noteEn;
	}
}
internal sealed class AppraisalValueBreakdown
{
	internal readonly List<AppraisalValueLine> Lines = new List<AppraisalValueLine>();

	internal readonly List<AppraisalValueLine> FixedDetails = new List<AppraisalValueLine>();

	internal int Subtotal;

	internal int Minimum;

	internal int Total;

	internal string FixedReasonZh;

	internal string FixedReasonEn;

	internal bool IsFixed => FixedReasonZh != null;

	internal bool MatchesNative(int nativeValue)
	{
		return Total == nativeValue;
	}

	internal void Add(string zh, string en, int amount, bool isBase = false, string noteZh = null, string noteEn = null)
	{
		Lines.Add(new AppraisalValueLine(zh, en, amount, isBase, noteZh, noteEn));
		Subtotal += amount;
	}

	internal void Finish(int minimum)
	{
		Minimum = minimum;
		Total = Math.Max(minimum, Subtotal);
	}

	internal static AppraisalValueBreakdown Fixed(int value, string zh, string en)
	{
		return new AppraisalValueBreakdown
		{
			Subtotal = value,
			Total = value,
			Minimum = value,
			FixedReasonZh = zh,
			FixedReasonEn = en
		};
	}
}
internal static class AppraisalValueCalculator
{
	internal static AppraisalValueBreakdown Hand(string material, string iface, string condition, string armored, string bladed)
	{
		if (!OneOf(material, "ALUMINUM_ALLOY", "CARBON_COMPOSITE", "GOLD_PLATED") || !OneOf(iface, "EMG", "EMG_PLUS", "NEURAL", "UNSPECIFIED") || !OneOf(condition, "NONE", "WEAR", "DAMAGE") || !OneOf(armored, "NONE", "ARMORED") || !OneOf(bladed, "NONE", "BLADED"))
		{
			return null;
		}
		AppraisalValueBreakdown appraisalValueBreakdown = new AppraisalValueBreakdown();
		appraisalValueBreakdown.Add("计算起点", "Starting value", 100, isBase: true);
		if (material == "CARBON_COMPOSITE")
		{
			appraisalValueBreakdown.Add("碳复合材料", "Carbon composite", 50);
		}
		if (material == "GOLD_PLATED")
		{
			appraisalValueBreakdown.Add("镀金", "Gold plated", 200);
		}
		if (iface == "NEURAL")
		{
			appraisalValueBreakdown.Add("神经接口", "Neural interface", 120);
		}
		if (iface == "EMG_PLUS")
		{
			appraisalValueBreakdown.Add("EMG+ 接口", "EMG+ interface", 45);
		}
		if (iface == "UNSPECIFIED")
		{
			appraisalValueBreakdown.Add("规格不明接口", "Unspecified interface", -60);
		}
		if (condition == "WEAR")
		{
			appraisalValueBreakdown.Add("磨损", "Wear", -35);
		}
		if (condition == "DAMAGE")
		{
			appraisalValueBreakdown.Add("受损", "Damage", -85);
		}
		if (condition == "NONE")
		{
			appraisalValueBreakdown.Add("无磨损或受损", "No wear or damage", 0);
		}
		if (armored == "ARMORED")
		{
			appraisalValueBreakdown.Add("装甲组件", "Armored add-on", 50);
		}
		if (bladed == "BLADED")
		{
			appraisalValueBreakdown.Add("刀刃组件", "Blade add-on", 80);
		}
		if (material == "CARBON_COMPOSITE" && (iface == "EMG_PLUS" || iface == "NEURAL") && condition == "NONE" && armored == "ARMORED" && bladed == "BLADED")
		{
			appraisalValueBreakdown.Add("战术型组合", "Tactical set", 100, isBase: false, "碳复合材料、神经或 EMG+、无损、装甲与刀刃", "Carbon, neural or EMG+, intact, armored and bladed");
		}
		if (material == "GOLD_PLATED" && iface == "NEURAL" && condition == "NONE")
		{
			appraisalValueBreakdown.Add("豪华型组合", "Deluxe set", 200, isBase: false, "镀金、神经接口、无损", "Gold plated, neural interface, intact");
		}
		appraisalValueBreakdown.Finish(50);
		return appraisalValueBreakdown;
	}

	internal static AppraisalValueBreakdown Plasma(string wear, string crack, string uncovered, string reinforced, string color, string seal)
	{
		if (!OneOf(wear, "NONE", "WEAR", "WEAR_HEAVY") || !OneOf(crack, "NONE", "CRACK") || !OneOf(uncovered, "NONE", "UNCOVERED") || !OneOf(reinforced, "NONE", "REINFORCED") || !OneOf(color, "STANDARD", "POOR1", "POOR2", "HIGH_QUALITY") || !OneOf(seal, "NONE", "LL", "ZHONGHUA", "REPUBLIC", "IGNITION_LAB"))
		{
			return null;
		}
		AppraisalValueBreakdown appraisalValueBreakdown = new AppraisalValueBreakdown();
		appraisalValueBreakdown.Add("计算起点", "Starting value", 100, isBase: true);
		if (wear == "WEAR")
		{
			appraisalValueBreakdown.Add("轻度磨损", "Light wear", -15);
		}
		if (wear == "WEAR_HEAVY")
		{
			appraisalValueBreakdown.Add("重度磨损", "Heavy wear", -30);
		}
		if (crack == "CRACK")
		{
			appraisalValueBreakdown.Add("外壳破裂", "Cracked casing", -50);
		}
		if (uncovered == "UNCOVERED")
		{
			appraisalValueBreakdown.Add("螺丝外露", "Exposed screws", -10);
		}
		if (reinforced == "REINFORCED")
		{
			appraisalValueBreakdown.Add("加固处理", "Reinforced", 20);
		}
		if (color == "POOR1" || color == "POOR2")
		{
			appraisalValueBreakdown.Add("低级颜色", "Low-grade color", -50);
		}
		if (color == "HIGH_QUALITY")
		{
			appraisalValueBreakdown.Add("高级颜色", "High-grade color", 50);
		}
		if (seal == "LL" && (color == "STANDARD" || color == "POOR1" || color == "POOR2"))
		{
			appraisalValueBreakdown.Add("王尔德载具加成", "Wild's Rides bonus", 20, isBase: false, "颜色为标准或低级", "Standard or low-grade color");
		}
		else if (seal == "ZHONGHUA" && wear == "NONE" && crack == "NONE" && uncovered == "NONE" && color == "STANDARD")
		{
			appraisalValueBreakdown.Add("中华船舶加成", "ZhongHua bonus", 40, isBase: false, "无磨损、裂纹或外露；标准颜色", "No wear, cracks or exposed screws; standard color");
		}
		else if (seal == "REPUBLIC" && crack != "CRACK" && color == "STANDARD")
		{
			appraisalValueBreakdown.Add("共和国海军加成", "Republic Naval bonus", 40, isBase: false, "无裂纹；标准颜色", "No cracks; standard color");
		}
		else if (seal == "IGNITION_LAB" && reinforced == "NONE" && wear == "NONE" && crack == "NONE" && uncovered == "NONE" && color == "HIGH_QUALITY")
		{
			appraisalValueBreakdown.Add("点火实验室加成", "Ignition Lab bonus", 80, isBase: false, "外壳完好、未加固；高级颜色", "Intact, unreinforced; high-grade color");
		}
		appraisalValueBreakdown.Finish(30);
		return appraisalValueBreakdown;
	}

	internal static AppraisalValueBreakdown Ribwich(int wrap, int nativeValue, int? meat, int? bun, int? pickles, int? month, int? year, int? facility, int? davreLogo = null, int? ribwichLogo = null, int? productionMark = null, int? facilityMark = null)
	{
		if (wrap == 1 || wrap == 2)
		{
			return AppraisalValueBreakdown.Fixed(40, "包装已撕开", "Packaging torn open");
		}
		if (wrap != 0)
		{
			return null;
		}
		if (nativeValue == 40)
		{
			List<AppraisalValueLine> list = RibwichCounterfeitReasons(bun, pickles, month, year, facility, davreLogo, ribwichLogo, productionMark, facilityMark);
			if (list == null || list.Count == 0)
			{
				return null;
			}
			AppraisalValueBreakdown appraisalValueBreakdown = AppraisalValueBreakdown.Fixed(40, "判为赝品，按固定价计", "Counterfeit: fixed value applies");
			appraisalValueBreakdown.FixedDetails.AddRange(list);
			return appraisalValueBreakdown;
		}
		if (wrap != 0 || !meat.HasValue || !bun.HasValue || !pickles.HasValue || !month.HasValue || !year.HasValue || !facility.HasValue || !OneOf(meat.Value, 0, 1) || !OneOf(bun.Value, 0, 1) || !OneOf(pickles.Value, 0, 1) || month.Value < 1 || month.Value > 12 || ((year.Value < 425 || year.Value > 429) && (year.Value < 432 || year.Value > 438)) || facility.Value < 1 || facility.Value > 35)
		{
			return null;
		}
		AppraisalValueBreakdown appraisalValueBreakdown2 = new AppraisalValueBreakdown();
		appraisalValueBreakdown2.Add("计算起点", "Starting value", 150, isBase: true);
		if (meat.Value == 1)
		{
			appraisalValueBreakdown2.Add("双层肉饼", "Double meat patty", 800);
		}
		if (bun.Value == 1 && pickles.Value == 1)
		{
			appraisalValueBreakdown2.Add("豪华版", "Deluxe edition", 300);
		}
		if ((month.Value == 1 && year.Value == 425) || (month.Value == 12 && year.Value == 438))
		{
			appraisalValueBreakdown2.Add("历史首月／末月", "First / last production month", 50);
		}
		if (year.Value >= 425 && year.Value <= 429)
		{
			appraisalValueBreakdown2.Add("第一生产批次", "First production run", 80);
		}
		int value = facility.Value;
		if (OneOf(value, 1, 2, 5, 8, 21, 34))
		{
			appraisalValueBreakdown2.Add("高品质包装厂 " + value.ToString("D2"), "High-quality facility " + value.ToString("D2"), 50);
		}
		else if (OneOf(value, 7, 15, 18, 26, 31))
		{
			appraisalValueBreakdown2.Add("低品质包装厂 " + value.ToString("D2"), "Low-quality facility " + value.ToString("D2"), -50);
		}
		appraisalValueBreakdown2.Finish(40);
		return appraisalValueBreakdown2;
	}

	internal static List<AppraisalValueLine> RibwichCounterfeitReasons(int? bun, int? pickles, int? month, int? year, int? facility, int? davreLogo, int? ribwichLogo, int? productionMark, int? facilityMark)
	{
		if (!bun.HasValue || !pickles.HasValue || !month.HasValue || !year.HasValue || !facility.HasValue || !davreLogo.HasValue || !ribwichLogo.HasValue || !productionMark.HasValue || !facilityMark.HasValue)
		{
			return null;
		}
		List<AppraisalValueLine> list = new List<AppraisalValueLine>();
		bool flag = year.Value >= 425 && year.Value <= 429;
		bool flag2 = year.Value >= 432 && year.Value <= 438;
		bool flag3 = month.Value >= 1 && month.Value <= 12;
		if (productionMark.Value == -1)
		{
			list.Add(new AppraisalValueLine("生产日期缺失", "Production date missing", 0));
		}
		else
		{
			if (!flag3)
			{
				list.Add(new AppraisalValueLine("生产月份 " + Number(month.Value) + "：应为 1—12 月", "Production month " + Number(month.Value) + ": must be 1–12", 0));
			}
			if (!flag && !flag2)
			{
				list.Add(new AppraisalValueLine("生产年份 " + Number(year.Value) + "：应为 425—429 或 432—438 年", "Production year " + Number(year.Value) + ": must be 425–429 or 432–438", 0));
			}
		}
		if (facilityMark.Value == -1)
		{
			list.Add(new AppraisalValueLine("包装工厂编号缺失", "Packaging facility number missing", 0));
		}
		else if (facility.Value < 1 || facility.Value > 35)
		{
			list.Add(new AppraisalValueLine("包装工厂编号 " + Number(facility.Value) + "：应为 01—35", "Packaging facility " + Number(facility.Value) + ": must be 01–35", 0));
		}
		if (davreLogo.Value != 0)
		{
			list.Add(new AppraisalValueLine((davreLogo.Value == -1) ? "达夫雷标志缺失" : "达夫雷标志图案错误", (davreLogo.Value == -1) ? "Davre logo missing" : "Incorrect Davre logo", 0));
		}
		if ((flag3 & flag) && ribwichLogo.Value != -1)
		{
			list.Add(new AppraisalValueLine("第一生产批次不应有 Ribwich 贴纸", "First production run must not have a Ribwich sticker", 0));
		}
		if ((flag3 & flag2) && ribwichLogo.Value != 0)
		{
			list.Add(new AppraisalValueLine((ribwichLogo.Value == -1) ? "第二生产批次缺少 Ribwich 贴纸" : "第二生产批次的 Ribwich 贴纸图案错误", (ribwichLogo.Value == -1) ? "Second production run: Ribwich sticker missing" : "Second production run: incorrect Ribwich sticker", 0));
		}
		if (bun.Value == 1 && pickles.Value == 0)
		{
			list.Add(new AppraisalValueLine("有芝麻面包，但没有加量酸黄瓜；豪华版须同时具备两者", "Sesame bun without extra pickles; deluxe edition requires both", 0));
		}
		else if (bun.Value == 0 && pickles.Value == 1)
		{
			list.Add(new AppraisalValueLine("有加量酸黄瓜，但没有芝麻面包；豪华版须同时具备两者", "Extra pickles without a sesame bun; deluxe edition requires both", 0));
		}
		else if ((bun.Value != 0 || pickles.Value != 0) && (bun.Value != 1 || pickles.Value != 1))
		{
			list.Add(new AppraisalValueLine("面包与酸黄瓜组合不符合普通版或豪华版", "Bun and pickles do not match a standard or deluxe edition", 0));
		}
		return list;
	}

	private static string Number(int value)
	{
		return value.ToString(CultureInfo.InvariantCulture);
	}

	private static bool OneOf(string value, params string[] options)
	{
		if (value == null)
		{
			return false;
		}
		foreach (string text in options)
		{
			if (value == text)
			{
				return true;
			}
		}
		return false;
	}

	private static bool OneOf(int value, params int[] options)
	{
		foreach (int num in options)
		{
			if (value == num)
			{
				return true;
			}
		}
		return false;
	}
}
internal enum AugMotion
{
	Pending,
	Disabled,
	Unknown,
	Normal,
	Freeze,
	Seizure
}
internal enum AugPortraitRead
{
	Pending,
	Unknown,
	Known
}
internal sealed class AugReadings
{
	internal AugMotion Motion = AugMotion.Pending;

	internal AugPortraitRead Portrait = AugPortraitRead.Pending;

	internal string PortraitKey;

	internal string OutfitStyle;

	internal string IdentityStyle;

	internal string DisplayName;

	internal bool MotionFailed;

	internal bool PortraitFailed;

	internal bool IdentityFailed;

	internal bool DialogueFailed;
}
internal sealed class AugReason
{
	internal readonly string Kind;

	internal readonly string Zh;

	internal readonly string En;

	internal AugReason(string kind, string zh, string en)
	{
		Kind = kind;
		Zh = zh;
		En = en;
	}
}
internal static class AugExplainer
{
	private static readonly Dictionary<string, AugReason[]> Portraits = BuildPortraits();

	private static readonly HashSet<string> SpecificAugLines = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "帮一个朋友带的。", "给朋友的。", "在囤货。", "Picking them up for a friend.", "For a friend.", "I am stocking up." };

	private static readonly Regex SpecificAugBudget = new Regex("\\A(?:我有[0-9]+信用点能用。|I can spend [0-9]+ credits\\.|My budget is [0-9]+ credits\\.)\\z", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

	private static Dictionary<string, AugReason[]> BuildPortraits()
	{
		Dictionary<string, AugReason[]> dictionary = new Dictionary<string, AugReason[]>(StringComparer.Ordinal);
		AddPortrait(dictionary, "augLLMale1", "right", pupils: true, smile: true);
		AddPortrait(dictionary, "augLLMale2", null, pupils: true, smile: false);
		AddPortrait(dictionary, "augLLMale3", "left", pupils: true, smile: false);
		AddPortrait(dictionary, "augLLFemale1", "right", pupils: true, smile: true);
		AddPortrait(dictionary, "augLLFemale2", "left", pupils: true, smile: false);
		AddPortrait(dictionary, "augLLFemale3", null, pupils: true, smile: false);
		AddPortrait(dictionary, "augBMMale1", null, pupils: true, smile: false);
		AddPortrait(dictionary, "augBMMale2", "left", pupils: true, smile: false);
		AddPortrait(dictionary, "augBMMale3", "right", pupils: true, smile: true);
		AddPortrait(dictionary, "augBMFemale1", "right", pupils: true, smile: false);
		AddPortrait(dictionary, "augBMFemale2", "both", pupils: true, smile: true);
		AddPortrait(dictionary, "augBMFemale3", null, pupils: false, smile: false);
		dictionary["augBMFemale3"] = new AugReason[1]
		{
			new AugReason("expression", "诡异表情：嘴角呈僵硬的上扬表情；墨镜遮住双眼，无法据此核对瞳孔。", "Uncanny expression: a stiff upturned mouth; sunglasses conceal the pupils.")
		};
		AddPortrait(dictionary, "augULMale1", "right", pupils: true, smile: false);
		AddPortrait(dictionary, "augULMale2", null, pupils: false, smile: true);
		AddPortrait(dictionary, "augULMale3", "left", pupils: true, smile: false);
		AddPortrait(dictionary, "augULFemale1", null, pupils: true, smile: false);
		AddPortrait(dictionary, "augULFemale2", "right", pupils: true, smile: false);
		AddPortrait(dictionary, "augULFemale3", null, pupils: true, smile: true);
		return dictionary;
	}

	private static void AddPortrait(Dictionary<string, AugReason[]> map, string key, string chipSide, bool pupils, bool smile)
	{
		List<AugReason> list = new List<AugReason>();
		if (chipSide != null)
		{
			string text;
			if (chipSide == "both")
			{
				text = "头部两侧";
			}
			else
			{
				text = ((chipSide == "left") ? "画面左侧头部" : "画面右侧头部");
			}
			string text2 = ((chipSide == "both") ? "both sides of the head" : (chipSide + " side of the image"));
			list.Add(new AugReason("chip", "外露芯片：" + text + "、太阳穴附近可见小型彩色植入物。", "Visible aug chip: a small colored implant near the temple, on the " + text2 + "."));
		}
		if (pupils)
		{
			list.Add(new AugReason("pupils", "异常瞳孔：露出的双眼瞳孔发浅。", "Unusual pupils: the exposed eyes have pale pupils."));
		}
		if (smile)
		{
			list.Add(new AugReason("expression", "诡异表情：嘴角上扬，露齿笑容显得僵硬。", "Uncanny expression: a stiff toothy smile."));
		}
		map[key] = list.ToArray();
	}

	internal static bool HasPortrait(string key)
	{
		return key != null && Portraits.ContainsKey(key);
	}

	internal static AugMotion ResolveMotion(bool bound, bool moving, bool enabled, bool animatorActive, bool playing, bool suppressed, bool shaking, bool horizontal)
	{
		if (!bound | moving)
		{
			return AugMotion.Pending;
		}
		if (!enabled)
		{
			return AugMotion.Disabled;
		}
		if (!animatorActive)
		{
			return AugMotion.Unknown;
		}
		if (suppressed && !shaking && !playing)
		{
			return AugMotion.Freeze;
		}
		if (!suppressed & shaking & playing & horizontal)
		{
			return AugMotion.Seizure;
		}
		if ((!suppressed && !shaking) & playing)
		{
			return AugMotion.Normal;
		}
		return AugMotion.Unknown;
	}

	internal static List<AugReason> Explain(AugReadings read, IEnumerable<AugReason> dialogue)
	{
		List<AugReason> list = new List<AugReason>();
		if (!read.MotionFailed && read.Motion == AugMotion.Freeze)
		{
			list.Add(new AugReason("motion", "不自然的僵滞：当前顾客的待机动作被冻结。", "Unnatural stillness: this client's idle movement is frozen."));
		}
		if (!read.MotionFailed && read.Motion == AugMotion.Seizure)
		{
			list.Add(new AugReason("motion", "异常抽搐：当前顾客的身体出现反复横向抖动。", "Twitching: this client's body repeatedly shakes sideways."));
		}
		if (!read.PortraitFailed && read.Portrait == AugPortraitRead.Known && Portraits.TryGetValue(read.PortraitKey ?? "", out var value))
		{
			list.AddRange(value);
		}
		if (!read.PortraitFailed && !read.IdentityFailed && read.Portrait == AugPortraitRead.Known && read.IdentityStyle != null && read.OutfitStyle != null && read.IdentityStyle != read.OutfitStyle)
		{
			AugReason augReason = StyleName(read.OutfitStyle);
			list.Add(new AugReason("outfit", "身份与服装不符：显示身份为“" + Display(read.DisplayName) + "”，却使用" + augReason.Zh + "外观。", "Outfit mismatch: identified as “" + Display(read.DisplayName) + "”, but wearing " + augReason.En + "."));
		}
		if (dialogue != null)
		{
			list.AddRange(dialogue);
		}
		return list;
	}

	internal static AugReason StyleName(string style)
	{
		return style switch
		{
			"lower" => new AugReason("style", "下层工装", "lower-level clothing"), 
			"upper" => new AugReason("style", "上层服装", "upper-level clothing"), 
			"bm" => new AugReason("style", "黑市服装", "black-market clothing"), 
			"security" => new AugReason("style", "安保制服", "a security uniform"), 
			"medical" => new AugReason("style", "医疗人员服装", "medical clothing"), 
			"engineer" => new AugReason("style", "工程人员服装", "engineering clothing"), 
			"revolution" => new AugReason("style", "革命派服装", "revolutionary clothing"), 
			"chef" => new AugReason("style", "厨师服装", "chef clothing"), 
			"farmer" => new AugReason("style", "农民服装", "farmer clothing"), 
			"merc" => new AugReason("style", "佣兵服装", "mercenary clothing"), 
			"science" => new AugReason("style", "科研人员服装", "scientist clothing"), 
			_ => new AugReason("style", "未识别服装", "unrecognized clothing"), 
		};
	}

	internal static List<AugReason> ExplainDialogue(string text)
	{
		List<AugReason> list = new List<AugReason>();
		text = Plain(text);
		if (text.Length == 0)
		{
			return list;
		}
		AddDialogueReason(list, text, "robotic", "机械化用词", "Robotic vocabulary", "本单位", "请求获取", "请求物资补给", "请求……", "需求目标", "我要求获取", "已分配预算", "已调拨预算", "已调拨资金", "执行物资采购行动", "成本参数", "演算为", "this unit", "requesting", "budget allocated", "allocated budget", "allocated funds", "for this acquisition", "cost parameters", "optimal outcome assessed");
		AddDialogueReason(list, text, "formal", "过度正式的措辞", "Overly formal phrasing", "初始化交流协议", "初始化交易程序", "等待你的配合", "请高效率地处理", "就像一个人类", "正如普通人类", "出于人类的求生本能", "我正在感觉到……恐惧。是的。恐惧", "我感到……非常害怕", "这种情况……并不理想", "initiating communication", "initiating transaction", "awaiting your cooperation", "process this efficiently", "like a human", "like humans", "for human reasons", "i am feeling... fear. yes. fear", "i am... very scared", "this is... not optimal");
		AddDialogueReason(list, text, "purpose", "可疑交易目的", "Suspicious purpose", "改造同化", "自我复制", "主网络", "更多的机体单元", "芯片设计", "改造植入手术", "宿主正在等待接受改造", "受试者的改造准备工作", "为网络进行物资采购", "复制设计蓝图", "augmentation", "replication process", "replication of chip design", "network demands more units", "sourcing for the network", "replication of the design");
		if (SpecificAugLines.Contains(text) || SpecificAugBudget.IsMatch(text))
		{
			list.Add(new AugReason("specific", "改造体特有台词：“" + Display(text) + "”。", "Dialogue unique to augmented clients: “" + Display(text) + "”."));
		}
		return list;
	}

	private static void AddDialogueReason(List<AugReason> result, string text, string kind, string zh, string en, params string[] patterns)
	{
		foreach (string value in patterns)
		{
			int num = text.IndexOf(value, StringComparison.OrdinalIgnoreCase);
			if (num >= 0)
			{
				string text2 = text;
				if (text2.Length > 120)
				{
					int num2 = Math.Max(0, num - 30);
					int num3 = Math.Min(120, text.Length - num2);
					text2 = ((num2 > 0) ? "…" : "") + text.Substring(num2, num3) + ((num2 + num3 < text.Length) ? "…" : "");
				}
				result.Add(new AugReason(kind, zh + "：“" + Display(text2) + "”。", en + ": “" + Display(text2) + "”."));
				break;
			}
		}
	}

	internal static void RememberDialogue(List<AugReason> target, string text)
	{
		foreach (AugReason reason in ExplainDialogue(text))
		{
			if (!target.Exists((AugReason r) => r.Kind == reason.Kind))
			{
				target.Add(reason);
			}
		}
	}

	internal static string Plain(string text)
	{
		return string.IsNullOrEmpty(text) ? "" : Regex.Replace(Regex.Replace(text, "<[^>]*>", ""), "\\s+", " ").Trim();
	}

	internal static string Display(string text)
	{
		return (text ?? "").Replace("<", "＜").Replace(">", "＞");
	}

	internal static string Signature(AugReadings read, IEnumerable<AugReason> reasons, bool chinese)
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.Append(chinese).Append('|').Append(read.Motion)
			.Append('|')
			.Append(read.Portrait)
			.Append('|')
			.Append(read.MotionFailed)
			.Append(read.PortraitFailed)
			.Append(read.IdentityFailed)
			.Append(read.DialogueFailed)
			.Append('|')
			.Append(read.IdentityStyle);
		foreach (AugReason reason in reasons)
		{
			stringBuilder.Append('|').Append(reason.Kind).Append(':')
				.Append(reason.Zh.Length)
				.Append(':')
				.Append(reason.Zh)
				.Append(':')
				.Append(reason.En.Length)
				.Append(':')
				.Append(reason.En);
		}
		return stringBuilder.ToString();
	}
}
internal static class BerryTooltip
{
	private static Action<RichTextBuilder, GameItem> _callback;

	private static bool _registered;

	internal static bool Enabled = true;

	internal static bool ShowQualityValue = false;

	internal static void EnsureRegistered()
	{
		if (_registered)
		{
			return;
		}
		try
		{
			Action<RichTextBuilder, GameItem> action = AppendQualityLine;
			_callback = Action<RichTextBuilder, GameItem>.op_Implicit(action);
			ModHook.OnCreateTooltipLate += _callback;
			_registered = true;
			ModLog.MarkOk("ModHook.OnCreateTooltipLate");
		}
		catch (Exception ex)
		{
			_callback = null;
			ModLog.Warn("ModHook.OnCreateTooltipLate", "荧光莓品质说明注册失败，本轮跳过: " + ex.GetType().Name + " - " + ex.Message);
		}
	}

	internal static void Unregister()
	{
		if (!_registered || (Delegate)(object)_callback == (Delegate)null)
		{
			return;
		}
		try
		{
			ModHook.OnCreateTooltipLate -= _callback;
		}
		catch
		{
		}
		finally
		{
			_registered = false;
			_callback = null;
		}
	}

	private static void AppendQualityLine(RichTextBuilder builder, GameItem item)
	{
		try
		{
			if (builder == null || item == null || !Enabled || !SafeGameAccess.SafeIsTag(item, "BERRY_TAG"))
			{
				return;
			}
			int? condition = SafeGameAccess.ReadTagInt(item, "BERRY_CONDITION_INT");
			int? visualVariant = SafeGameAccess.ReadTagInt(item, "BERRY_VISUAL_VARIANT_INT");
			string[] array = BerryTooltipText.BuildLines(condition, visualVariant, ShowQualityValue, LocalizationService.IsChinese);
			if (array.Length != 0)
			{
				string[] array2 = array;
				foreach (string text in array2)
				{
					builder.AddLine(text, true, (ColorPalette)(-1), false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
				}
				ModLog.MarkOk("BerryTooltip.AppendQualityLine");
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("BerryTooltip.AppendQualityLine", "荧光莓品质说明生成失败，本次不显示: " + ex.GetType().Name + " - " + ex.Message);
		}
	}
}
internal static class InspectorState
{
	internal static string FakeLastSig = null;

	internal static IntPtr FakeItemPtr = IntPtr.Zero;

	internal static int FakeItemUid = -1;

	internal static bool FakeSuppressed = false;

	internal static bool FakeWantVisible = false;

	internal static string FakeFailedSig = null;

	internal static float FakeWindowHeight = 0f;

	internal static string ValueLastSig = null;

	internal static IntPtr ValueItemPtr = IntPtr.Zero;

	internal static int ValueItemUid = -1;

	internal static bool ValueSuppressed = false;

	internal static bool ValueWantVisible = false;

	internal static float ValueWindowHeight = 0f;

	internal static bool ValueDetailsExpanded = false;

	internal static string AugLastSig = null;

	internal static IntPtr AugClientPtr = IntPtr.Zero;

	internal static string AugClientId = "";

	internal static bool AugSuppressed = false;

	internal static bool AugWantVisible = false;

	internal static string AugFailedSig = null;

	internal static float AugWindowHeight = 0f;

	internal static IntPtr AugDialogueClientPtr = IntPtr.Zero;

	internal static string AugDialogueClientId = "";

	internal static readonly List<AugReason> AugDialogueReasons = new List<AugReason>();

	internal static IntPtr AugActiveDialogueClientPtr = IntPtr.Zero;

	internal static string AugActiveDialogueClientId = "";

	internal static IntPtr AugActiveDialoguePtr = IntPtr.Zero;

	internal static int AugActiveDialogueEpoch;

	internal static string WantedLastSig = null;

	internal static IntPtr WantedClientPtr = IntPtr.Zero;

	internal static string WantedClientId = "";

	internal static bool WantedSuppressed = false;

	internal static bool WantedWantVisible = false;

	internal static IntPtr ContextItemPtr = IntPtr.Zero;

	internal static IntPtr CheckItemPtr = IntPtr.Zero;

	internal static int CheckItemUid = int.MinValue;

	internal static int CheckItemType = -1;

	internal static List<string> CheckList;
}
internal static class SettingsTrigger
{
	internal const string MagnifierIdentifier = "magnifier";

	internal static bool IsTargetMagnifier(string identifier)
	{
		return string.Equals(identifier, "magnifier", StringComparison.Ordinal);
	}
}
internal enum BerryQuality
{
	Unknown,
	Normal,
	Small,
	Moldy,
	UnripePurple,
	UnripeRed,
	Dried
}
internal static class BerryQualityClassifier
{
	internal static BerryQuality Classify(int condition, int visualVariant)
	{
		switch (condition)
		{
		case -2:
			return BerryQuality.Moldy;
		case -1:
			return visualVariant switch
			{
				0 => BerryQuality.UnripePurple, 
				1 => BerryQuality.UnripeRed, 
				2 => BerryQuality.Dried, 
				_ => BerryQuality.Unknown, 
			};
		default:
			if (condition != 1)
			{
				if (condition == 2)
				{
					return BerryQuality.Small;
				}
				return BerryQuality.Unknown;
			}
			goto case 0;
		case 0:
			return BerryQuality.Normal;
		}
	}

	internal static string DisplayName(BerryQuality quality, bool isChinese)
	{
		if (isChinese)
		{
			return quality switch
			{
				BerryQuality.Normal => "正常", 
				BerryQuality.Small => "较小", 
				BerryQuality.Moldy => "发霉", 
				BerryQuality.UnripePurple => "未成熟", 
				BerryQuality.UnripeRed => "未成熟", 
				BerryQuality.Dried => "干枯", 
				_ => null, 
			};
		}
		return quality switch
		{
			BerryQuality.Normal => "Normal", 
			BerryQuality.Small => "Small", 
			BerryQuality.Moldy => "Moldy", 
			BerryQuality.UnripePurple => "Unripe", 
			BerryQuality.UnripeRed => "Unripe", 
			BerryQuality.Dried => "Dried", 
			_ => null, 
		};
	}
}
internal static class BerryTooltipText
{
	internal static string[] BuildLines(int? condition, int? visualVariant, bool showValue, bool isChinese)
	{
		if (!condition.HasValue)
		{
			return new string[0];
		}
		string text = (visualVariant.HasValue ? BerryQualityClassifier.DisplayName(BerryQualityClassifier.Classify(condition.Value, visualVariant.Value), isChinese) : null);
		string text2 = (isChinese ? ("品质值：" + condition.Value.ToString(CultureInfo.InvariantCulture)) : ("Quality value: " + condition.Value.ToString(CultureInfo.InvariantCulture)));
		if (string.IsNullOrEmpty(text))
		{
			return (!showValue) ? new string[0] : new string[1] { text2 };
		}
		string text3 = (isChinese ? "品质：" : "Quality: ") + text;
		return (!showValue) ? new string[1] { text3 } : new string[2] { text3, text2 };
	}
}
internal static class WantedReputationCalculator
{
	internal const string FactionBlackMarket = "FACTION_BLACK_MARKET";

	internal const string FactionSecurity = "FACTION_SECURITY";

	internal static WantedReputationPreview Calculate(double currentExact, int clientPenalty, string factionId, bool upperLevelPetitionUnlocked)
	{
		double num = Clamp(currentExact);
		double delta = 0.0 - Math.Abs((double)clientPenalty);
		double num2 = Clamp(num + ApplyNegativeMultiplier(delta, factionId, upperLevelPetitionUnlocked));
		int num3 = (int)num;
		int num4 = (int)num2;
		return new WantedReputationPreview(num3, num4, num4 - num3);
	}

	private static double ApplyNegativeMultiplier(double delta, string factionId, bool upperLevelPetitionUnlocked)
	{
		if (delta >= 0.0)
		{
			return delta;
		}
		if (string.Equals(factionId, "FACTION_BLACK_MARKET", StringComparison.Ordinal))
		{
			return delta * 2.0;
		}
		if (upperLevelPetitionUnlocked && string.Equals(factionId, "FACTION_SECURITY", StringComparison.Ordinal))
		{
			return delta * 2.0;
		}
		return delta;
	}

	private static double Clamp(double value)
	{
		if (double.IsNaN(value))
		{
			return 0.0;
		}
		if (value > 200.0)
		{
			return 200.0;
		}
		if (value < -200.0)
		{
			return -200.0;
		}
		return value;
	}
}
internal readonly struct WantedReputationPreview
{
	internal readonly int Current;

	internal readonly int AfterReport;

	internal readonly int Change;

	internal WantedReputationPreview(int current, int afterReport, int change)
	{
		Current = current;
		AfterReport = afterReport;
		Change = change;
	}
}
internal readonly struct WantedReputationData
{
	internal readonly string FactionId;

	internal readonly string FactionName;

	internal readonly bool HasReputation;

	internal readonly WantedReputationPreview Preview;

	internal WantedReputationData(string factionId, string factionName, bool hasReputation, WantedReputationPreview preview)
	{
		FactionId = factionId ?? "";
		FactionName = factionName ?? "";
		HasReputation = hasReputation;
		Preview = preview;
	}
}
internal readonly struct AugRiskPreview
{
	internal readonly int Presence;

	internal readonly bool ExtinctionOnKill;

	internal readonly bool OutbreakOnRelease;

	internal AugRiskPreview(int presence, bool extinctionOnKill, bool outbreakOnRelease)
	{
		Presence = presence;
		ExtinctionOnKill = extinctionOnKill;
		OutbreakOnRelease = outbreakOnRelease;
	}
}
internal static class AugRiskCalculator
{
	internal const int ExtinctionCheckDay = 62;

	private const int NormalKillReduction = 25;

	private const int HardKillReduction = 10;

	private const int HardReleaseIncrease = 15;

	private const int HardOutbreakThreshold = 100;

	internal static AugRiskPreview Calculate(int currentDay, int augPresence, bool isHardMode, int nextAugSpawnDay)
	{
		int num = (isHardMode ? 10 : 25);
		bool extinctionOnKill = augPresence - num <= 0 && (currentDay >= 62 || !HasAugOpportunityBeforeExtinctionCheck(currentDay, nextAugSpawnDay));
		bool outbreakOnRelease = isHardMode && augPresence + 15 >= 100;
		return new AugRiskPreview(augPresence, extinctionOnKill, outbreakOnRelease);
	}

	private static bool HasAugOpportunityBeforeExtinctionCheck(int currentDay, int nextAugSpawnDay)
	{
		if (currentDay < 13)
		{
			return true;
		}
		if (currentDay < 18)
		{
			return true;
		}
		if (nextAugSpawnDay < 0)
		{
			return currentDay < 62;
		}
		return nextAugSpawnDay < 62;
	}
}
internal static class SafeGameAccess
{
	private static float _initRealtime;

	private const float STORE_WARMUP_SECONDS = 5f;

	internal static bool? ReadAuthenticityIssue(GameItem item, string key)
	{
		if (item == null)
		{
			return null;
		}
		try
		{
			bool value = item.IsTag(key);
			ModLog.MarkOk("Authenticity.Issue." + key);
			return value;
		}
		catch (Exception ex)
		{
			ModLog.Warn("Authenticity.Issue." + key, "真伪检查项读取失败: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static AuthenticityReadings ReadAuthenticityAppearance(GameItem item, int type)
	{
		AuthenticityReadings authenticityReadings = new AuthenticityReadings();
		string[] array = type switch
		{
			2 => new string[5] { "STAMP_LETTERING", "STAMP_LOGO", "STAMP_SEAL", "STAMP_AMOUNT", "STAMP_CHIP" }, 
			1 => new string[4] { "CIGARETTE_LETTERING", "CIGARETTE_BOX", "CIGARETTE_SEAL", "CIGARETTE_LOGO" }, 
			0 => new string[3] { "INJECTOR_INDICATOR", "INJECTOR_BODY", "INJECTOR_COLOR" }, 
			_ => Array.Empty<string>(), 
		};
		foreach (string key in array)
		{
			authenticityReadings.Numbers[key] = ReadAuthenticityNumber(item, key);
		}
		if (type == 0)
		{
			string[] array2 = new string[3] { "INJECTOR_COMPANY", "INJECTOR_MODEL", "INJECTOR_SERIAL" };
			foreach (string key2 in array2)
			{
				authenticityReadings.Text[key2] = ReadAuthenticityString(item, key2);
			}
		}
		return authenticityReadings;
	}

	private static int? ReadAuthenticityNumber(GameItem item, string key)
	{
		if (item == null)
		{
			return null;
		}
		try
		{
			TagState tagReadonly = item.GetTagReadonly(key);
			int? result = ((tagReadonly != null) ? new int?(tagReadonly.valueInt) : ((int?)null));
			ModLog.MarkOk("Authenticity.Appearance." + key);
			return result;
		}
		catch (Exception ex)
		{
			ModLog.Warn("Authenticity.Appearance." + key, "真伪外观读取失败: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	private static string ReadAuthenticityString(GameItem item, string key)
	{
		if (item == null)
		{
			return null;
		}
		try
		{
			TagState tagReadonly = item.GetTagReadonly(key);
			string result = ((tagReadonly != null) ? (tagReadonly.valueString ?? "") : null);
			ModLog.MarkOk("Authenticity.Appearance." + key);
			return result;
		}
		catch (Exception ex)
		{
			ModLog.Warn("Authenticity.Appearance." + key, "真伪文字读取失败: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static void MarkInitTime()
	{
		_initRealtime = Time.realtimeSinceStartup;
	}

	internal static string ItemName(GameItem it)
	{
		try
		{
			return (it == null) ? "(无)" : SafeString(it.name);
		}
		catch
		{
			return "?";
		}
	}

	internal static string SafeString(object value)
	{
		if (value == null)
		{
			return "";
		}
		try
		{
			return Convert.ToString(value, CultureInfo.InvariantCulture) ?? "";
		}
		catch
		{
			return value.ToString() ?? "";
		}
	}

	internal static int SafeUniqueId(GameItem item)
	{
		if (item == null)
		{
			return -1;
		}
		try
		{
			return item.uniqueId;
		}
		catch
		{
			return -1;
		}
	}

	internal static string SafeItemIdentifier(GameItem item)
	{
		if (item == null)
		{
			return "";
		}
		try
		{
			return SafeString(item.identifier);
		}
		catch
		{
			return "";
		}
	}

	internal static string SafeClientId(StoreClient c)
	{
		if (c == null)
		{
			return "";
		}
		try
		{
			return SafeString(c.identifier);
		}
		catch
		{
			return "";
		}
	}

	internal static bool? ReadIsAug(StoreClient client)
	{
		if (client == null)
		{
			return null;
		}
		try
		{
			bool isAug = client.isAug;
			ModLog.MarkOk("StoreClient.isAug");
			return isAug;
		}
		catch (Exception ex)
		{
			ModLog.Warn("StoreClient.isAug", "读不到顾客改造体标志（本轮不显示）: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static AugRiskPreview? ReadAugRisk()
	{
		try
		{
			if (!PlayerStore.IsInstanceExist())
			{
				return null;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return null;
			}
			AugRiskPreview value = AugRiskCalculator.Calculate(StoreStation.GetDayCounter(), instance.augPresence, instance.isHardMode, instance.nextAugSpawnDay);
			ModLog.MarkOk("PlayerStore.augRisk");
			return value;
		}
		catch (Exception ex)
		{
			ModLog.Warn("PlayerStore.augRisk", "读不到改造体结局字段（仅显示基础警告）: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static WantedReputationData ReadWantedReputation(StoreClient client)
	{
		if (client == null)
		{
			return default;
		}
		string text = "";
		string text2 = "";
		try
		{
			text = SafeString(client.clientFaction);
			text2 = SafeString(StoreReputation.GetFactionNameByID(text));
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = FallbackFactionName(text);
			}
			StoreReputation storeReputation = StoreReputation.GetStoreReputation(text);
			if (storeReputation == null)
			{
				ModLog.MarkOk("StoreClient.wantedReputation.noFactionRep");
				return new WantedReputationData(text, text2, hasReputation: false, default);
			}
			double reputationExact = storeReputation.GetReputationExact();
			int reputationPenalty = client.reputationPenalty;
			bool upperLevelPetitionUnlocked = StoreReputation.IsPerkUnlocked("UL_PETITION");
			WantedReputationPreview preview = WantedReputationCalculator.Calculate(reputationExact, reputationPenalty, text, upperLevelPetitionUnlocked);
			ModLog.MarkOk("StoreClient.wantedReputation");
			return new WantedReputationData(text, text2, hasReputation: true, preview);
		}
		catch (Exception ex)
		{
			ModLog.Warn("StoreClient.wantedReputation", "读不到通缉犯势力/声望（该项显示为 ?）: " + ex.GetType().Name + " - " + ex.Message);
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = FallbackFactionName(text);
			}
			return new WantedReputationData(text, text2, hasReputation: false, default);
		}
	}

	private static string FallbackFactionName(string factionId)
	{
		return factionId switch
		{
			"FACTION_LOWER_LEVEL" => LocalizationService.L("下层居民", "Lower Level"), 
			"FACTION_UPPER_LEVEL" => LocalizationService.L("上层居民", "Upper Level"), 
			"FACTION_MIDDLE" => LocalizationService.L("中层居民", "Middle Level"), 
			"FACTION_SECURITY" => LocalizationService.L("安全部门", "Security"), 
			"FACTION_BLACK_MARKET" => LocalizationService.L("黑市", "Black Market"), 
			"FACTION_TOURIST" => LocalizationService.L("游客", "Tourist"), 
			"FACTION_REVOLUTION" => LocalizationService.L("革命军", "Revolution"), 
			"FACTION_CARTEL" => LocalizationService.L("卡特尔", "Cartel"), 
			_ => string.IsNullOrWhiteSpace(factionId) ? "?" : factionId, 
		};
	}

	internal static bool SafeIsTag(GameItem item, string key)
	{
		try
		{
			return item.IsTag(key);
		}
		catch
		{
			return false;
		}
	}

	internal static GameItem SafeCurrentContextItem(ItemContextHandler handler)
	{
		if ((Object)(object)handler == (Object)null)
		{
			return null;
		}
		try
		{
			return handler.currentItem;
		}
		catch
		{
			return null;
		}
	}

	internal static string ReadTagValue(GameItem item, string key)
	{
		try
		{
			TagSystem modifiedState = item.modifiedState;
			if (modifiedState == null)
			{
				return null;
			}
			TagState tag = modifiedState.GetTag(key);
			if (tag == null)
			{
				return null;
			}
			try
			{
				string internalValueString = tag.internalValueString;
				if (!string.IsNullOrWhiteSpace(internalValueString))
				{
					return internalValueString;
				}
			}
			catch
			{
			}
			try
			{
				string valueString = tag.valueString;
				if (!string.IsNullOrWhiteSpace(valueString))
				{
					return valueString;
				}
			}
			catch
			{
			}
			try
			{
				int valueInt = tag.valueInt;
				if (valueInt != 0)
				{
					return valueInt.ToString(CultureInfo.InvariantCulture);
				}
			}
			catch
			{
			}
			return "0";
		}
		catch
		{
			return null;
		}
	}

	internal static int? ReadTagInt(GameItem item, string key)
	{
		if (item == null || string.IsNullOrEmpty(key))
		{
			return null;
		}
		try
		{
			TagState tagReadonly = item.GetTagReadonly(key);
			if (tagReadonly == null)
			{
				return null;
			}
			return tagReadonly.valueInt;
		}
		catch
		{
			return null;
		}
	}

	internal static string ReadAppraisalTagString(GameItem item, string key)
	{
		if (item == null || string.IsNullOrEmpty(key))
		{
			return null;
		}
		try
		{
			TagState tagReadonly = item.GetTagReadonly(key);
			return (tagReadonly != null) ? tagReadonly.valueString : null;
		}
		catch (Exception ex)
		{
			ModLog.Warn("AppraisalTag." + key, "读不到价值鉴定标签: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static int? ReadAppraisalTrueValue(GameItem item)
	{
		try
		{
			int correctBaseValue = AppraisalHelper.GetCorrectBaseValue(item);
			ModLog.MarkOk("AppraisalHelper.GetCorrectBaseValue");
			return correctBaseValue;
		}
		catch (Exception ex)
		{
			ModLog.Warn("AppraisalHelper.GetCorrectBaseValue", "读不到真实价值（该项显示为 ?）: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static TMP_InputField ReadAppraisalInput(InspectionUIManager insp, IntPtr expectedItemPtr, int expectedItemUid)
	{
		try
		{
			if (!IsInspectionPanelVisible(insp))
			{
				return null;
			}
			GameItem currentInspectingItem = insp.currentInspectingItem;
			if (currentInspectingItem == null || ((Il2CppObjectBase)currentInspectingItem).Pointer != expectedItemPtr || SafeUniqueId(currentInspectingItem) != expectedItemUid || !SafeIsTag(currentInspectingItem, "APPRAISAL_ITEM"))
			{
				return null;
			}
			GameObject appraisalPanel = insp.appraisalPanel;
			if ((Object)(object)appraisalPanel == (Object)null || !appraisalPanel.activeInHierarchy)
			{
				return null;
			}
			TMP_InputField apprisalServiceInputField = insp.apprisalServiceInputField;
			if ((Object)(object)apprisalServiceInputField == (Object)null || !((Behaviour)apprisalServiceInputField).isActiveAndEnabled || !((Component)apprisalServiceInputField).gameObject.activeInHierarchy || apprisalServiceInputField.readOnly || !((Selectable)apprisalServiceInputField).IsInteractable())
			{
				return null;
			}
			ModLog.MarkOk("InspectionUIManager.AppraisalInput");
			return apprisalServiceInputField;
		}
		catch (Exception ex)
		{
			ModLog.Warn("InspectionUIManager.AppraisalInput", "估价输入框不可用: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static bool IsInspectionPanelVisible(InspectionUIManager insp)
	{
		if ((Object)(object)insp == (Object)null)
		{
			return false;
		}
		try
		{
			GameObject panel = insp.panel;
			return (Object)(object)panel != (Object)null && panel.activeInHierarchy;
		}
		catch
		{
			return false;
		}
	}

	internal static StoreClient GetCurrentStoreClient()
	{
		if (Time.realtimeSinceStartup - _initRealtime < 5f)
		{
			return null;
		}
		try
		{
			if (!PlayerStore.IsInstanceExist())
			{
				return null;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return null;
			}
			StoreClientInstance currentClientInstance = instance.currentClientInstance;
			if (currentClientInstance == null)
			{
				return null;
			}
			StoreClient clientBlueprint = currentClientInstance.GetClientBlueprint();
			ModLog.MarkOk("PlayerStore.currentClientInstance");
			return clientBlueprint;
		}
		catch (Exception ex)
		{
			ModLog.Warn("PlayerStore.currentClientInstance", "拿不到当前顾客（改造体/通缉犯警告本轮不显示）: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static AugReadings ReadAugFeatures(StoreClient client)
	{
		//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
		//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
		AugReadings augReadings = new AugReadings();
		if (client == null)
		{
			return augReadings;
		}
		StoreClientMono instance;
		try
		{
			instance = StoreClientMono.Instance;
			StoreClient val = (((Object)(object)instance != (Object)null) ? instance.idleDecisionClient : null);
			if (val == null || ((Il2CppObjectBase)val).Pointer != ((Il2CppObjectBase)client).Pointer || instance.IsMoving())
			{
				return augReadings;
			}
		}
		catch (Exception ex)
		{
			augReadings.MotionFailed = (augReadings.PortraitFailed = true);
			AugReadFailure("Target", ex);
			return augReadings;
		}
		try
		{
			IdleAnimator idleAnimator = instance.idleAnimator;
			bool animatorActive = (Object)(object)idleAnimator != (Object)null && ((Behaviour)idleAnimator).isActiveAndEnabled;
			Vector3 val2 = (((Object)(object)idleAnimator != (Object)null) ? idleAnimator.axis : Vector3.zero);
			bool horizontal = Math.Abs(val2.x) > 0.9f && Math.Abs(val2.y) < 0.01f && Math.Abs(val2.z) < 0.01f && idleAnimator.amplitude > 0f && idleAnimator.frequency > 0f;
			augReadings.Motion = AugExplainer.ResolveMotion(bound: true, moving: false, IdleAnimator.GloballyEnabled, animatorActive, (Object)(object)idleAnimator != (Object)null && idleAnimator.playing, instance.idleSuppressed, instance.idleShakeHorizontal, horizontal);
			ModLog.MarkOk("AugFeatures.Motion");
		}
		catch (Exception ex2)
		{
			augReadings.MotionFailed = true;
			AugReadFailure("Motion", ex2);
		}
		try
		{
			augReadings.Portrait = AugPortraitRead.Unknown;
			SpriteRenderer image = instance.image;
			Sprite val3 = (((Object)(object)image != (Object)null && ((Renderer)image).enabled && ((Component)image).gameObject.activeInHierarchy) ? image.sprite : null);
			SpriteDict instance2 = SpriteDict.Instance;
			string spriteName = client.spriteName;
			Sprite val4 = default;
			if ((Object)(object)val3 != (Object)null && (Object)(object)instance2 != (Object)null && !string.IsNullOrEmpty(spriteName) && instance2.spriteDictionary != null && instance2.spriteDictionary.TryGetValue(spriteName, ref val4) && (Object)(object)val4 != (Object)null && ((Il2CppObjectBase)val4).Pointer == ((Il2CppObjectBase)val3).Pointer)
			{
				string text = FindAugOutfitStyle(instance2, spriteName, ((Il2CppObjectBase)val3).Pointer);
				if (text != null)
				{
					augReadings.Portrait = AugPortraitRead.Known;
					augReadings.PortraitKey = spriteName;
					augReadings.OutfitStyle = text;
				}
			}
			ModLog.MarkOk("AugFeatures.Portrait");
		}
		catch (Exception ex3)
		{
			augReadings.PortraitFailed = true;
			AugReadFailure("Portrait", ex3);
		}
		try
		{
			augReadings.DisplayName = client.displayName;
			string[] array = new string[3] { "name_lower_levels_citizen", "name_upper_level_citizen", "name_thief" };
			string[] array2 = new string[3] { "lower", "upper", "bm" };
			for (int i = 0; i < array.Length; i++)
			{
				string text2 = AugExplainer.Plain(LocHelper.GetLocalizedClientName(array[i]));
				if (text2.Length > 0 && AugExplainer.Plain(augReadings.DisplayName) == text2)
				{
					augReadings.IdentityStyle = array2[i];
					break;
				}
			}
			ModLog.MarkOk("AugFeatures.Identity");
		}
		catch (Exception ex4)
		{
			augReadings.IdentityFailed = true;
			AugReadFailure("Identity", ex4);
		}
		return augReadings;
	}

	private static bool AugSpriteIn(List<SpriteEntry> entries, string key, IntPtr sprite)
	{
		if (entries == null)
		{
			return false;
		}
		int count = entries.Count;
		if (count > 2048)
		{
			throw new InvalidOperationException("头像列表超出合理范围");
		}
		for (int i = 0; i < count; i++)
		{
			SpriteEntry val = entries[i];
			if (val != null && !(val.key != key))
			{
				Sprite sprite2 = val.sprite;
				if ((Object)(object)sprite2 != (Object)null && ((Il2CppObjectBase)sprite2).Pointer == sprite)
				{
					return true;
				}
			}
		}
		return false;
	}

	private static string FindAugOutfitStyle(SpriteDict s, string key, IntPtr sprite)
	{
		if (AugSpriteIn(s.augLowerMaleEntries, key, sprite) || AugSpriteIn(s.augLowerFemaleEntries, key, sprite) || AugSpriteIn(s.lowerMaleEntries, key, sprite) || AugSpriteIn(s.lowerFemaleEntries, key, sprite))
		{
			return "lower";
		}
		if (AugSpriteIn(s.augUpperMaleEntries, key, sprite) || AugSpriteIn(s.augUpperFemaleEntries, key, sprite) || AugSpriteIn(s.upperMaleEntries, key, sprite) || AugSpriteIn(s.upperFemaleEntries, key, sprite))
		{
			return "upper";
		}
		if (AugSpriteIn(s.augBmMaleEntries, key, sprite) || AugSpriteIn(s.augBmFemaleEntries, key, sprite) || AugSpriteIn(s.bmMaleEntries, key, sprite) || AugSpriteIn(s.bmFemaleEntries, key, sprite))
		{
			return "bm";
		}
		if (AugSpriteIn(s.secMaleEntries, key, sprite) || AugSpriteIn(s.secFemaleEntries, key, sprite) || AugSpriteIn(s.secCorpoMaleEntries, key, sprite) || AugSpriteIn(s.secCorpoFemaleEntries, key, sprite) || AugSpriteIn(s.secOffMaleEntries, key, sprite) || AugSpriteIn(s.secOffFemaleEntries, key, sprite))
		{
			return "security";
		}
		if (AugSpriteIn(s.medMaleEntries, key, sprite) || AugSpriteIn(s.medFemaleEntries, key, sprite))
		{
			return "medical";
		}
		if (AugSpriteIn(s.engMaleEntries, key, sprite) || AugSpriteIn(s.engFemaleEntries, key, sprite))
		{
			return "engineer";
		}
		if (AugSpriteIn(s.revMaleEntries, key, sprite) || AugSpriteIn(s.revFemaleEntries, key, sprite))
		{
			return "revolution";
		}
		if (AugSpriteIn(s.chefMaleEntries, key, sprite) || AugSpriteIn(s.chefFemaleEntries, key, sprite))
		{
			return "chef";
		}
		if (AugSpriteIn(s.farmerMaleEntries, key, sprite) || AugSpriteIn(s.farmerFemaleEntries, key, sprite))
		{
			return "farmer";
		}
		if (AugSpriteIn(s.mercMaleEntries, key, sprite) || AugSpriteIn(s.mercFemaleEntries, key, sprite))
		{
			return "merc";
		}
		if (AugSpriteIn(s.sciMaleEntries, key, sprite) || AugSpriteIn(s.sciFemaleEntries, key, sprite))
		{
			return "science";
		}
		return null;
	}

	internal static UnityEvent ReadAugDialogueEvent()
	{
		try
		{
			DialogUIManager instance = DialogUIManager.Instance;
			TypewriterByCharacter val = (((Object)(object)instance != (Object)null && (Object)(object)instance.clientBubble != (Object)null) ? instance.clientBubble.typewriter : null);
			return ((Object)(object)val != (Object)null) ? ((TypewriterCore)val).onTextShowed : null;
		}
		catch (Exception ex)
		{
			AugReadFailure("DialogueEvent", ex);
			return null;
		}
	}

	internal static UnityEvent ReadAugDialogueStartEvent()
	{
		try
		{
			DialogUIManager instance = DialogUIManager.Instance;
			TypewriterByCharacter val = (((Object)(object)instance != (Object)null && (Object)(object)instance.clientBubble != (Object)null) ? instance.clientBubble.typewriter : null);
			return ((Object)(object)val != (Object)null) ? ((TypewriterCore)val).onTypewriterStart : null;
		}
		catch (Exception ex)
		{
			AugReadFailure("DialogueStartEvent", ex);
			return null;
		}
	}

	internal static void RecordAugDialogueOwner(StoreClient client)
	{
		InspectorState.AugActiveDialogueClientPtr = IntPtr.Zero;
		InspectorState.AugActiveDialoguePtr = IntPtr.Zero;
		InspectorState.AugActiveDialogueClientId = "";
		try
		{
			DialogUIManager instance = DialogUIManager.Instance;
			Dialogue val = (((Object)(object)instance != (Object)null) ? instance.currentDialog : null);
			StoreClientMono instance2 = StoreClientMono.Instance;
			StoreClient val2 = (((Object)(object)instance2 != (Object)null) ? instance2.idleDecisionClient : null);
			if (client != null && val != null && !(val.title == "PLAYER") && !(val.title == "SEC") && val2 != null && !(((Il2CppObjectBase)val2).Pointer != ((Il2CppObjectBase)client).Pointer) && !instance2.IsMoving() && !((Object)(object)instance.clientNameTMP == (Object)null) && !(AugExplainer.Plain(((TMP_Text)instance.clientNameTMP).text) != AugExplainer.Plain(client.displayName)))
			{
				InspectorState.AugActiveDialogueClientPtr = ((Il2CppObjectBase)client).Pointer;
				InspectorState.AugActiveDialogueClientId = SafeClientId(client);
				InspectorState.AugActiveDialoguePtr = ((Il2CppObjectBase)val).Pointer;
				InspectorState.AugActiveDialogueEpoch = instance.conversationEpoch;
			}
		}
		catch (Exception ex)
		{
			AugReadFailure("DialogueOwner", ex);
		}
	}

	private static bool AugOwnsDialogue(StoreClient client, Dialogue line, int epoch)
	{
		if (InspectorState.AugActiveDialogueClientPtr == ((Il2CppObjectBase)client).Pointer && InspectorState.AugActiveDialogueClientId == SafeClientId(client) && InspectorState.AugActiveDialoguePtr == ((Il2CppObjectBase)line).Pointer && InspectorState.AugActiveDialogueEpoch == epoch)
		{
			return true;
		}
		Dialogue[] array = new Dialogue[9] { client.mainDialogue, client.acceptDealDialogue, client.allDoneDialogue, client.repeatDialogue, client.placedWrongItemWhenSellingToDialogue, client.placeRightItemWhenSellingToDialogue, client.interogationDialogue, client.glasseDialogue, client.customOnArrestDialogue };
		HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
		Dialogue[] array2 = array;
		foreach (Dialogue val in array2)
		{
			Dialogue val2 = val;
			while (val2 != null && hashSet.Count < 128 && hashSet.Add(((Il2CppObjectBase)val2).Pointer))
			{
				if (((Il2CppObjectBase)val2).Pointer == ((Il2CppObjectBase)line).Pointer)
				{
					return true;
				}
				val2 = val2.nextDialogue;
			}
		}
		return false;
	}

	internal static string ReadAugDisplayedDialogue(StoreClient client, out bool failed)
	{
		failed = false;
		if (client == null)
		{
			return null;
		}
		try
		{
			DialogUIManager instance = DialogUIManager.Instance;
			if ((Object)(object)instance == (Object)null || instance.isSecMonologuePlaying)
			{
				return null;
			}
			Dialogue currentDialog = instance.currentDialog;
			if (currentDialog == null || currentDialog.title == "PLAYER" || currentDialog.title == "SEC")
			{
				return null;
			}
			if (!AugOwnsDialogue(client, currentDialog, instance.conversationEpoch))
			{
				return null;
			}
			TextBubble clientBubble = instance.clientBubble;
			if ((Object)(object)clientBubble == (Object)null || !((Component)clientBubble).gameObject.activeInHierarchy || (Object)(object)clientBubble.textfield == (Object)null)
			{
				return null;
			}
			StoreClientMono instance2 = StoreClientMono.Instance;
			StoreClient val = (((Object)(object)instance2 != (Object)null) ? instance2.idleDecisionClient : null);
			if (val == null || ((Il2CppObjectBase)val).Pointer != ((Il2CppObjectBase)client).Pointer || instance2.IsMoving())
			{
				return null;
			}
			if ((Object)(object)instance.clientNameTMP == (Object)null || AugExplainer.Plain(((TMP_Text)instance.clientNameTMP).text) != AugExplainer.Plain(client.displayName))
			{
				return null;
			}
			TypewriterByCharacter typewriter = clientBubble.typewriter;
			TAnimCore val2 = (((Object)(object)typewriter != (Object)null) ? ((TypewriterCore)typewriter)._textAnimator : null);
			if ((Object)(object)val2 == (Object)null || ((TypewriterCore)typewriter).isHidingText || !val2.allLettersShown)
			{
				return null;
			}
			string text = AugExplainer.Plain(currentDialog.plainText);
			if (text.Length == 0 || text != AugExplainer.Plain(val2.textWithoutAnyTag) || text != AugExplainer.Plain(((TMP_Text)clientBubble.textfield).text))
			{
				return null;
			}
			ModLog.MarkOk("AugFeatures.Dialogue");
			return text;
		}
		catch (Exception ex)
		{
			failed = true;
			AugReadFailure("Dialogue", ex);
			return null;
		}
	}

	private static void AugReadFailure(string part, Exception ex)
	{
		ModLog.Warn("AugFeatures." + part, "改造体线索读取失败（保留身份警告）: " + ex.GetType().Name + " - " + ex.Message);
	}
}
internal enum LocaleTick
{
	None,
	FirstResolve,
	Changed
}
internal static class LocalizationService
{
	private static bool _isChinese = true;

	private static bool _resolved = false;

	private static float _timer = 0f;

	private const float CHECK_INTERVAL = 1f;

	internal static bool IsChinese => _isChinese;

	internal static string L(string zh, string en)
	{
		return _isChinese ? zh : en;
	}

	internal static LocaleTick Tick(float dt, bool gameReady)
	{
		if (!gameReady)
		{
			return LocaleTick.None;
		}
		_timer += dt;
		if (_timer < 1f)
		{
			return LocaleTick.None;
		}
		_timer = 0f;
		bool? flag = Detect();
		if (!flag.HasValue)
		{
			return LocaleTick.None;
		}
		if (_resolved && flag.Value == _isChinese)
		{
			return LocaleTick.None;
		}
		bool resolved = _resolved;
		_isChinese = flag.Value;
		_resolved = true;
		return (!resolved) ? LocaleTick.FirstResolve : LocaleTick.Changed;
	}

	private static bool? Detect()
	{
		try
		{
			string currentLocaleCode = LocHelper.GetCurrentLocaleCode();
			ModLog.MarkOk("LocHelper.GetCurrentLocaleCode");
			if (!string.IsNullOrEmpty(currentLocaleCode))
			{
				string text = currentLocaleCode.ToLowerInvariant();
				if (text.StartsWith("zh") || text.Contains("chinese") || text.Contains("hans") || text.Contains("hant"))
				{
					return true;
				}
				if (text.Length >= 2)
				{
					return false;
				}
			}
		}
		catch (Exception ex)
		{
			ModLog.Warn("LocHelper.GetCurrentLocaleCode", "取不到语言码，改用文本判定: " + ex.GetType().Name + " - " + ex.Message);
		}
		try
		{
			string text2 = TryLocalized("inspection_indicator_issues");
			if (!string.IsNullOrEmpty(text2))
			{
				string text3 = text2;
				foreach (char c in text3)
				{
					if (c >= '一' && c <= '?')
					{
						return true;
					}
				}
				return false;
			}
		}
		catch
		{
		}
		return null;
	}

	internal static string TryLocalized(string locKey)
	{
		try
		{
			string localizedUI = LocHelper.GetLocalizedUI(locKey, (Il2CppReferenceArray<Object>)null);
			ModLog.MarkOk("LocHelper.GetLocalizedUI");
			if (string.IsNullOrWhiteSpace(localizedUI))
			{
				return null;
			}
			if (localizedUI == locKey)
			{
				return null;
			}
			return localizedUI;
		}
		catch (Exception ex)
		{
			ModLog.Warn("LocHelper.GetLocalizedUI", "本地化文本不可用，改用内置文案: " + ex.GetType().Name + " - " + ex.Message);
			return null;
		}
	}

	internal static string LocalizedIssueName(string locKey, string zhFallback, string enFallback)
	{
		return TryLocalized(locKey) ?? L(zhFallback, enFallback);
	}

	internal static string GuessLocKey(string issueTag)
	{
		string text = issueTag;
		if (text.StartsWith("ISSUE_", StringComparison.Ordinal))
		{
			text = text.Substring("ISSUE_".Length);
		}
		if (text.EndsWith("_ISSUE", StringComparison.Ordinal))
		{
			text = text.Substring(0, text.Length - "_ISSUE".Length);
		}
		int num = text.IndexOf('_');
		if (num > 0)
		{
			text = text.Substring(num + 1);
		}
		return "inspection_" + text.ToLowerInvariant() + "_issues";
	}
}
internal sealed class LegacyEntryInfo
{
	internal Type Type;

	internal string CurrentText;

	internal string DefaultText;
}
internal sealed class MigrationPlan
{
	internal readonly Dictionary<string, object> Values = new Dictionary<string, object>();

	internal int SkippedUnknownKey;

	internal int SkippedUserChanged;

	internal int SkippedUnparsable;

	internal int SkippedTotal => SkippedUnknownKey + SkippedUserChanged + SkippedUnparsable;
}
internal static class LegacyMigration
{
	private const string LEGACY_SECTION = "CounterfeitInspector";

	internal static void Run(MelonPreferences_Category cfg, MelonPreferences_Entry<bool> migratedEntry)
	{
		try
		{
			if (migratedEntry == null || migratedEntry.Value || cfg == null)
			{
				return;
			}
			string path = Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");
			if (!File.Exists(path))
			{
				return;
			}
			Dictionary<string, string> dictionary = ParseLegacySection(File.ReadAllLines(path), "CounterfeitInspector");
			if (dictionary.Count == 0)
			{
				return;
			}
			Dictionary<string, LegacyEntryInfo> dictionary2 = new Dictionary<string, LegacyEntryInfo>();
			foreach (KeyValuePair<string, string> item in dictionary)
			{
				dictionary2[item.Key] = Lookup(cfg, item.Key);
			}
			MigrationPlan migrationPlan = PlanMigration(dictionary, dictionary2);
			foreach (KeyValuePair<string, object> value in migrationPlan.Values)
			{
				MelonPreferences_Entry entry = cfg.GetEntry(value.Key);
				if (entry != null)
				{
					entry.BoxedValue = value.Value;
				}
			}
			migratedEntry.Value = true;
			Report(dictionary.Count, migrationPlan);
			MelonPreferences.Save();
		}
		catch (Exception ex)
		{
			ModLog.Warn("MigrateLegacyPreferences", "旧配置迁移失败（不影响使用，只是回到默认值）: " + ex.GetType().Name + " - " + ex.Message);
		}
	}

	private static LegacyEntryInfo Lookup(MelonPreferences_Category cfg, string key)
	{
		MelonPreferences_Entry entry = cfg.GetEntry(key);
		if (entry == null)
		{
			return new LegacyEntryInfo
			{
				Type = null
			};
		}
		return new LegacyEntryInfo
		{
			Type = entry.GetReflectedType(),
			CurrentText = entry.GetValueAsString(),
			DefaultText = entry.GetDefaultValueAsString()
		};
	}

	internal static Dictionary<string, string> ParseLegacySection(string[] lines, string sectionName)
	{
		Dictionary<string, string> dictionary = new Dictionary<string, string>();
		if (lines == null)
		{
			return dictionary;
		}
		string text = "[" + sectionName + "]";
		bool flag = false;
		foreach (string text2 in lines)
		{
			string text3 = text2.Trim();
			if (text3.Length == 0 || text3[0] == '#' || text3[0] == ';')
			{
				continue;
			}
			if (text3[0] == '[')
			{
				flag = text3 == text;
			}
			else if (flag)
			{
				int num = text3.IndexOf('=');
				if (num > 0)
				{
					dictionary[text3.Substring(0, num).Trim()] = text3.Substring(num + 1).Trim();
				}
			}
		}
		return dictionary;
	}

	internal static MigrationPlan PlanMigration(IDictionary<string, string> legacy, IDictionary<string, LegacyEntryInfo> infos)
	{
		MigrationPlan migrationPlan = new MigrationPlan();
		if (legacy == null)
		{
			return migrationPlan;
		}
		foreach (KeyValuePair<string, string> item in legacy)
		{
			if (infos == null || !infos.TryGetValue(item.Key, out var value) || value == null || value.Type == null)
			{
				migrationPlan.SkippedUnknownKey++;
				continue;
			}
			if (value.CurrentText != value.DefaultText)
			{
				migrationPlan.SkippedUserChanged++;
				continue;
			}
			object obj = ParseScalar(item.Value, value.Type);
			if (obj == null)
			{
				migrationPlan.SkippedUnparsable++;
			}
			else
			{
				migrationPlan.Values[item.Key] = obj;
			}
		}
		return migrationPlan;
	}

	internal static object ParseScalar(string s, Type t)
	{
		bool result;
		if (t == typeof(bool))
		{
			return bool.TryParse(s, out result) ? ((object)result) : null;
		}
		float result2;
		if (t == typeof(float))
		{
			return float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out result2) ? ((object)result2) : null;
		}
		int result3;
		if (t == typeof(int))
		{
			return int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out result3) ? ((object)result3) : null;
		}
		if (t == typeof(string))
		{
			return (s.Length >= 2 && s[0] == '"' && s[s.Length - 1] == '"') ? s.Substring(1, s.Length - 2) : s;
		}
		return null;
	}

	private static void Report(int legacyCount, MigrationPlan plan)
	{
		if (plan.Values.Count > 0)
		{
			MelonLogger.Msg("已从旧配置段 [CounterfeitInspector] 迁移 " + plan.Values.Count + " 项到 [ProbablyStolenInspector]（3.15.0 改名导致；旧段保留但不再读取）");
		}
		else
		{
			MelonLogger.Msg("旧配置段 [CounterfeitInspector] 有 " + legacyCount + " 项，但没有任何一项需要迁移（已废弃 " + plan.SkippedUnknownKey + " / 用户已改过 " + plan.SkippedUserChanged + " / 类型不支持 " + plan.SkippedUnparsable + "）；本次仍记为已迁移，不再重试。");
		}
		if (plan.SkippedUnparsable > 0)
		{
			ModLog.Warn("MigrateLegacyPreferences.unparsable", "旧配置段里有 " + plan.SkippedUnparsable + " 项的值无法解析成目标类型，已跳过；这通常是 mod 缺陷（ParseScalar 未覆盖该类型），请反馈");
		}
	}
}
internal static class ModLog
{
	internal static readonly bool VerboseEnabled = false;

	private static readonly Dictionary<string, int> _errCounts = new Dictionary<string, int>();

	internal static void Verbose(string msg)
	{
		if (!VerboseEnabled)
		{
			return;
		}
		try
		{
			MelonLogger.Msg("[v] " + msg);
		}
		catch
		{
		}
	}

	private static void OnceImpl(string key, string detail, bool asWarning)
	{
		_errCounts.TryGetValue(key, out var value);
		value++;
		_errCounts[key] = value;
		switch (value)
		{
		case 1:
		{
			string text = key + (asWarning ? " 不可用（降级运行，后续同类信息将静默，恢复正常后自动重新报）: " : " 异常（后续同类信息将静默，恢复正常后自动重新报）: ");
			if (asWarning)
			{
				MelonLogger.Warning(text + detail);
			}
			else
			{
				MelonLogger.Error(text + detail);
			}
			break;
		}
		case 50:
			MelonLogger.Warning(key + " 已连续失败 " + value + " 次，该功能可能已不可用；若游戏刚更新，请等待 mod 更新");
			break;
		}
	}

	internal static void Error(string key, Exception ex)
	{
		OnceImpl(key, ex.GetType().Name + " - " + ex.Message, asWarning: false);
	}

	internal static void Warn(string key, string detail)
	{
		OnceImpl(key, detail, asWarning: true);
	}

	internal static void MarkOk(string key)
	{
		if (_errCounts.Count != 0)
		{
			_errCounts.Remove(key);
		}
	}

	internal static void Fingerprint()
	{
		try
		{
			string text = null;
			try
			{
				text = Application.version;
			}
			catch
			{
			}
			if (!string.IsNullOrEmpty(text) && text.IndexOf("0.46D", StringComparison.OrdinalIgnoreCase) < 0)
			{
				MelonLogger.Warning("游戏版本与 mod 适配目标不一致（" + text + " vs 0.46D）：字段偏移等假设可能已失效，请关注 mod 更新");
			}
			if (!VerboseEnabled)
			{
				return;
			}
			MelonLogger.Msg("Target: Probably Stolen 0.46D / IL2CPP x64 / MelonLoader 0.7.3 / lang=" + (LocalizationService.IsChinese ? "zh" : "en"));
			MelonLogger.Msg("GameVersion(runtime)=" + (text ?? "?") + "  TargetGameVersion=0.46D");
			string location = Assembly.GetExecutingAssembly().Location;
			if (string.IsNullOrEmpty(location) || !File.Exists(location))
			{
				return;
			}
			using SHA256 sHA = SHA256.Create();
			using FileStream inputStream = File.OpenRead(location);
			byte[] array = sHA.ComputeHash(inputStream);
			StringBuilder stringBuilder = new StringBuilder(array.Length * 2);
			for (int i = 0; i < array.Length; i++)
			{
				stringBuilder.Append(array[i].ToString("x2"));
			}
			MelonLogger.Msg("AssemblySHA256=" + stringBuilder.ToString());
		}
		catch
		{
		}
	}
}
