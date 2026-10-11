using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using SentinelTracker;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Core), "哨兵综合症监控", "1.0.0", "核桃", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("SentinelTracker")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("SentinelTracker")]
[assembly: AssemblyTitle("SentinelTracker")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.0.0.0")]
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
namespace SentinelTracker
{
	public class Core : MelonMod
	{
		private static MelonPreferences_Category _cfgCategory;

		private static MelonPreferences_Entry<bool> _cfgShowUI;

		private static MelonPreferences_Entry<KeyCode> _cfgToggleKey;

		private static MelonPreferences_Entry<float> _cfgPanelOpacity;

		private const string PP_PANEL_X = "SentinelTracker.PanelX";

		private const string PP_PANEL_Y = "SentinelTracker.PanelY";

		private const float PANEL_DEFAULT_X = 20f;

		private const float PANEL_DEFAULT_Y = 20f;

		private static MelonPreferences_Entry<float> _cfgTextOpacity;

		private static MelonPreferences_Entry<float> _cfgPanelWidth;

		private static MelonPreferences_Entry<float> _cfgPanelHeight;

		private static MelonPreferences_Entry<float> _cfgHandleOpacity;

		private float _readTimer;

		private int _displayD;

		private int _displayPool;

		private bool _dataReady;

		private bool _isUIVisible;

		private Rect _windowRect = new Rect(20f, 20f, 340f, 240f);

		private const float PANEL_MIN_W = 260f;

		private const float PANEL_MIN_H = 180f;

		private const float PANEL_MAX_W = 800f;

		private const float PANEL_MAX_H = 700f;

		private const float HANDLE_SIZE = 22f;

		private const float CONTENT_MAX_W = 300f;

		private const float CONTENT_H = 200f;

		private const float HEADER_H = 34f;

		private const float FOOTER_H = 24f;

		private bool _isResizing;

		private Vector2 _resizeStartMouse;

		private Vector2 _resizeStartSize;

		private GUIStyle _titleStyle;

		private GUIStyle _dataStyle;

		private GUIStyle _tipStyle;

		private GUIStyle _hotkeyStyle;

		private GUIStyle _waitStyle;

		private bool _stylesReady;

		private static GUIStyle _handleStyle;

		public override void OnInitializeMelon()
		{
			//IL_01db: Unknown result type (might be due to invalid IL or missing references)
			_cfgCategory = MelonPreferences.CreateCategory("SentinelTracker", "哨兵综合症监控");
			_cfgShowUI = _cfgCategory.CreateEntry<bool>("ShowUI", true, "显示监控面板", "是否默认显示哨兵监控面板", false, false, (ValueValidator)null, (string)null);
			_cfgToggleKey = _cfgCategory.CreateEntry<KeyCode>("ToggleKey", (KeyCode)284, "面板开关按键", "默认 F3", false, false, (ValueValidator)null, (string)null);
			_cfgPanelOpacity = _cfgCategory.CreateEntry<float>("PanelOpacity", 0.9f, "面板背景透明度", "窗口背景/边框/标题栏的透明度：1.0=不透明，0.3=很透明，0.1=几乎看不见", false, false, (ValueValidator)null, (string)null);
			_cfgTextOpacity = _cfgCategory.CreateEntry<float>("TextOpacity", 1f, "文字透明度", "面板内所有文字的透明度：1.0=完全不透明（推荐），0.5=半透明", false, false, (ValueValidator)null, (string)null);
			_cfgPanelWidth = _cfgCategory.CreateEntry<float>("PanelWidth", 340f, "面板宽度", "面板宽度（拖右下角调整）", false, false, (ValueValidator)null, (string)null);
			_cfgPanelHeight = _cfgCategory.CreateEntry<float>("PanelHeight", 240f, "面板高度", "面板高度（拖右下角调整）", false, false, (ValueValidator)null, (string)null);
			_cfgHandleOpacity = _cfgCategory.CreateEntry<float>("HandleOpacity", 0.6f, "缩放手柄透明度", "右下角缩放手柄的透明度，独立于面板背景。1.0=完全不透明，0.0=完全隐藏（不推荐，会失去视觉提示）", false, false, (ValueValidator)null, (string)null);
			_cfgCategory.SaveToFile(true);
			_isUIVisible = _cfgShowUI.Value;
			_windowRect.x = PlayerPrefs.GetFloat("SentinelTracker.PanelX", 20f);
			_windowRect.y = PlayerPrefs.GetFloat("SentinelTracker.PanelY", 20f);
			_windowRect.width = Mathf.Clamp(_cfgPanelWidth.Value, 260f, 800f);
			_windowRect.height = Mathf.Clamp(_cfgPanelHeight.Value, 180f, 700f);
			((MelonBase)this).LoggerInstance.Msg($"v1.0.0 已加载，按 {_cfgToggleKey.Value} 开关面板");
		}

		private void EnsureStyles()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Expected Obj, but got Unknown
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Expected Obj, but got Unknown
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Expected Obj, but got Unknown
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Expected Obj, but got Unknown
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_0131: Expected Obj, but got Unknown
			if (!_stylesReady)
			{
				_titleStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 16,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4
				};
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 14
				};
				val.normal.textColor = Color.white;
				val.alignment = (TextAnchor)4;
				_dataStyle = val;
				GUIStyle val2 = new GUIStyle(GUI.skin.label)
				{
					fontSize = 13
				};
				val2.normal.textColor = new Color(0.85f, 0.85f, 0.85f);
				val2.alignment = (TextAnchor)4;
				_tipStyle = val2;
				GUIStyle val3 = new GUIStyle(GUI.skin.label)
				{
					fontSize = 11
				};
				val3.normal.textColor = new Color(0.5f, 0.5f, 0.5f);
				val3.alignment = (TextAnchor)5;
				_hotkeyStyle = val3;
				GUIStyle val4 = new GUIStyle(GUI.skin.label)
				{
					fontSize = 14
				};
				val4.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
				val4.alignment = (TextAnchor)4;
				_waitStyle = val4;
				_stylesReady = true;
			}
		}

		public override void OnUpdate()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			if (Input.GetKeyDown(_cfgToggleKey.Value))
			{
				_isUIVisible = !_isUIVisible;
				_cfgShowUI.Value = _isUIVisible;
				MelonPreferences.Save();
			}
			_readTimer += Time.deltaTime;
			if (_readTimer < 0.5f)
			{
				return;
			}
			_readTimer = 0f;
			try
			{
				if (!PlayerStore.IsInstanceExist())
				{
					_dataReady = false;
					return;
				}
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					_dataReady = false;
					return;
				}
				HealthData healthData = instance.healthData;
				if (healthData == null)
				{
					_dataReady = false;
					return;
				}
				_displayD = healthData.daysSinceSentinelInjection;
				_displayPool = healthData.sentinelPool;
				_dataReady = true;
			}
			catch (Exception ex)
			{
				_dataReady = false;
				((MelonBase)this).LoggerInstance.Error("? 读取数据异常: " + ex.Message);
			}
		}

		public override void OnGUI()
		{
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			if (!_isUIVisible)
			{
				return;
			}
			int depth = GUI.depth;
			Color color = GUI.color;
			try
			{
				EnsureStyles();
				if (_stylesReady)
				{
					GUI.depth = 33000;
					float num = Mathf.Clamp(_cfgPanelOpacity?.Value ?? 0.9f, 0.1f, 1f);
					GUI.color = new Color(1f, 1f, 1f, num);
					HandleResizeInput();
					_windowRect = GUI.Window(98765, _windowRect, WindowFunction.op_Implicit((Action<int>)DrawWindow), "哨兵综合症监控");
					DrawResizeHandle();
				}
			}
			catch (Exception value)
			{
				((MelonBase)this).LoggerInstance.Error($"? OnGUI 异常: {value}");
			}
			finally
			{
				GUI.color = color;
				GUI.depth = depth;
			}
		}

		private static Vector2 GetGuiMousePosition()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
		}

		private void HandleResizeInput()
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			float num = _windowRect.x + _windowRect.width - 22f;
			float num2 = _windowRect.y + _windowRect.height - 22f;
			Rect val = new Rect(num, num2, 22f, 22f);
			Vector2 guiMousePosition = GetGuiMousePosition();
			bool flag = val.Contains(guiMousePosition);
			if (Input.GetMouseButtonDown(0) & flag)
			{
				_isResizing = true;
				_resizeStartMouse = guiMousePosition;
				_resizeStartSize = new Vector2(_windowRect.width, _windowRect.height);
			}
			if (_isResizing && Input.GetMouseButton(0))
			{
				float num3 = guiMousePosition.x - _resizeStartMouse.x;
				float num4 = guiMousePosition.y - _resizeStartMouse.y;
				_windowRect.width = Mathf.Clamp(_resizeStartSize.x + num3, 260f, 800f);
				_windowRect.height = Mathf.Clamp(_resizeStartSize.y + num4, 180f, 700f);
			}
			if (_isResizing && Input.GetMouseButtonUp(0))
			{
				_isResizing = false;
				_cfgPanelWidth.Value = _windowRect.width;
				_cfgPanelHeight.Value = _windowRect.height;
				MelonPreferences.Save();
			}
		}

		private void DrawResizeHandle()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0179: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Expected Obj, but got Unknown
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			Color color = GUI.color;
			float num = Mathf.Clamp(_cfgHandleOpacity?.Value ?? 0.6f, 0.05f, 1f);
			GUI.color = new Color(1f, 1f, 1f, num);
			try
			{
				if (_handleStyle == null)
				{
					_handleStyle = new GUIStyle(GUI.skin.button);
					_handleStyle.fontSize = 14;
					_handleStyle.fontStyle = (FontStyle)1;
					_handleStyle.alignment = (TextAnchor)4;
					_handleStyle.normal.textColor = new Color(1f, 0.95f, 0.6f);
					_handleStyle.hover.textColor = new Color(1f, 1f, 1f);
					_handleStyle.active.textColor = new Color(0.6f, 1f, 0.6f);
				}
				float num2 = _windowRect.x + _windowRect.width - 22f;
				float num3 = _windowRect.y + _windowRect.height - 22f;
				GUI.Button(new Rect(num2 + 2f, num3 + 2f, 18f, 18f), "◢", _handleStyle);
			}
			catch (Exception ex)
			{
				((MelonBase)this).LoggerInstance.Error("? DrawResizeHandle 异常: " + ex.Message);
			}
			finally
			{
				GUI.color = color;
			}
		}

		private void DrawWindow(int windowID)
		{
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0216: Unknown result type (might be due to invalid IL or missing references)
			//IL_0243: Unknown result type (might be due to invalid IL or missing references)
			//IL_026c: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0349: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0295: Unknown result type (might be due to invalid IL or missing references)
			//IL_03bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_041c: Unknown result type (might be due to invalid IL or missing references)
			//IL_047a: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0507: Unknown result type (might be due to invalid IL or missing references)
			//IL_0574: Unknown result type (might be due to invalid IL or missing references)
			//IL_0596: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_054a: Unknown result type (might be due to invalid IL or missing references)
			//IL_052d: Unknown result type (might be due to invalid IL or missing references)
			//IL_05c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_05c6: Invalid comparison between Unknown and I4
			try
			{
				GUIStyle val = _titleStyle ?? GUI.skin.label;
				GUIStyle val2 = _dataStyle ?? GUI.skin.label;
				GUIStyle val3 = _tipStyle ?? GUI.skin.label;
				GUIStyle val4 = _hotkeyStyle ?? GUI.skin.label;
				GUIStyle val5 = _waitStyle ?? GUI.skin.label;
				GUI.DragWindow(new Rect(0f, 0f, _windowRect.width - 30f, 30f));
				if (GUI.Button(new Rect(_windowRect.width - 25f, 2f, 20f, 20f), "×"))
				{
					_isUIVisible = false;
					_cfgShowUI.Value = false;
					MelonPreferences.Save();
				}
				Color color = GUI.color;
				float num = Mathf.Clamp(_cfgTextOpacity?.Value ?? 1f, 0.3f, 1f);
				GUI.color = new Color(1f, 1f, 1f, num);
				float width = _windowRect.width;
				float height = _windowRect.height;
				float num2 = Mathf.Min(width - 24f, 300f);
				float num3 = (width - num2) * 0.5f;
				float num4 = height - 34f - 24f;
				float num5 = 34f + Mathf.Max(0f, (num4 - 200f) * 0.5f);
				if (!_dataReady)
				{
					GUI.Label(new Rect(num3, num5 + 40f, num2, 24f), "等待进入游戏或加载存档...", val5);
					GUI.Label(new Rect(num3, _windowRect.height - 24f, num2, 18f), $"快捷键: {_cfgToggleKey.Value} 开关面板", val4);
					GUI.color = color;
					return;
				}
				string text;
				Color textColor;
				if (_displayD < 7)
				{
					text = "【安全期】绝对安全";
					textColor = new Color(0.4f, 1f, 0.4f);
				}
				else if (_displayD < 10)
				{
					text = "【警告期】轻微掉血";
					textColor = new Color(1f, 0.6f, 0f);
				}
				else if (_displayD < 14)
				{
					text = "【重损期】加速死亡";
					textColor = new Color(1f, 0.2f, 0.2f);
				}
				else
				{
					text = "【濒死期】极易暴毙";
					textColor = new Color(1f, 0f, 0f);
				}
				val.normal.textColor = textColor;
				GUI.Label(new Rect(num3, num5, num2, 22f), text, val);
				num5 += 32f;
				GUI.Label(new Rect(num3, num5, num2, 20f), $"距离上次打针: {_displayD} 天", val2);
				num5 += 22f;
				GUI.Label(new Rect(num3, num5, num2, 20f), $"损伤池数值: {_displayPool}", val2);
				num5 += 26f;
				int value = 7 - _displayD;
				int value2 = 10 - _displayD;
				int value3 = 14 - _displayD;
				if (_displayD < 7)
				{
					GUI.Label(new Rect(num3, num5, num2, 20f), $"距离警告期还有 {value} 天", val3);
					num5 += 20f;
				}
				else if (_displayD < 10)
				{
					GUI.Label(new Rect(num3, num5, num2, 20f), $"距离重损期还有 {value2} 天", val3);
					num5 += 20f;
				}
				else if (_displayD < 14)
				{
					GUI.Label(new Rect(num3, num5, num2, 20f), $"距离濒死期还有 {value3} 天", val3);
					num5 += 20f;
				}
				num5 += 6f;
				if (_displayD >= 14)
				{
					GUI.Label(new Rect(num3, num5, num2, 20f), "\ud83d\udea8 立即打真品免疫宁！", val2);
				}
				else if (_displayD >= 10)
				{
					GUI.Label(new Rect(num3, num5, num2, 20f), "?\ufe0f 尽快打药止损！", val2);
				}
				else if (_displayD >= 7)
				{
					GUI.Label(new Rect(num3, num5, num2, 20f), "\ud83d\udca1 准备打药。", val2);
				}
				else
				{
					GUI.Label(new Rect(num3, num5, num2, 20f), "? 当前安全，无需打药。", val3);
				}
				GUI.Label(new Rect(num3, _windowRect.height - 24f, num2, 18f), $"按住标题栏可拖动 · 快捷键: {_cfgToggleKey.Value}", val4);
				GUI.color = color;
				if (Event.current != null && (int)Event.current.type == 1)
				{
					PlayerPrefs.SetFloat("SentinelTracker.PanelX", _windowRect.x);
					PlayerPrefs.SetFloat("SentinelTracker.PanelY", _windowRect.y);
					PlayerPrefs.Save();
				}
			}
			catch (Exception value4)
			{
				((MelonBase)this).LoggerInstance.Error($"? DrawWindow 异常: {value4}");
			}
		}

		public Core()
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
		}
	}
}
