using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text.Json.Serialization;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using Microsoft.CodeAnalysis;
using ProbablyStolenItemManager;
using UnityEngine;
using UnityEngine.EventSystems;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(ItemManager), "Probably Stolen Item Manager", "0.4.7", "小行星", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = "")]
[assembly: AssemblyVersion("0.0.0.0")]
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
}
namespace ProbablyStolenItemManager
{
	public sealed class ItemManager : MelonMod
	{
		private enum InspectorFieldKind
		{
			ReadOnly,
			Text,
			Int32,
			Int64,
			Bool
		}

		private sealed class InspectorField
		{
			public string Key { get; }

			public string Label { get; }

			public InspectorFieldKind Kind { get; }

			public string Value { get; }

			public InspectorField(string key, string label, InspectorFieldKind kind, string value)
			{
				Key = key;
				Label = label;
				Kind = kind;
				Value = value ?? string.Empty;
			}
		}

		private sealed class TagEntry
		{
			public string Key { get; }

			public TagState State { get; }

			public bool Modified { get; }

			public TagEntry(string key, TagState state, bool modified)
			{
				Key = key;
				State = state;
				Modified = modified;
			}
		}

		private sealed class TagPreset
		{
			public string Key { get; }

			public string Label { get; }

			public TagPreset(string key, string label)
			{
				Key = key;
				Label = label;
			}
		}

		private sealed class FeaturePreset
		{
			public string Key { get; }

			public string Label { get; }

			public Func<ItemFeature> Factory { get; }

			public FeaturePreset(string key, string label, Func<ItemFeature> factory)
			{
				Key = key;
				Label = label;
				Factory = factory;
			}
		}

		private sealed class GeneratedEntryDefinition
		{
			public string Key { get; }

			public string Chinese { get; }

			public string English { get; }

			public GeneratedEntryDefinition(string key, string chinese, string english)
			{
				Key = key;
				Chinese = chinese;
				English = english;
			}
		}

		private sealed class ItemManagerConfig
		{
			public string ToggleHotkey { get; set; } = "Alt+I";

			public string DeleteHotkey { get; set; } = "Delete";

			public string QuickDeleteHotkey { get; set; } = "Shift+D";

			public string Language { get; set; } = "zh";
		}

		private sealed class ItemEntry
		{
			public string Id { get; set; } = string.Empty;

			public string English { get; set; } = string.Empty;

			public string Chinese { get; set; } = string.Empty;

			public List<string> Aliases { get; set; } = new List<string>();

			public string? ResolvedId { get; set; }

			public bool IsAvailable { get; set; }

			[JsonIgnore]
			public string? GeneratorKey { get; set; }

			[JsonIgnore]
			public bool IsGenerated => !string.IsNullOrWhiteSpace(GeneratorKey);

			[JsonIgnore]
			public string SearchText { get; set; } = string.Empty;
		}

		private const int WindowId = 18427;

		private const int PageSize = 12;

		private const float ValidationInterval = 0.25f;

		private const string ConfigFileName = "item_mod_config.json";

		private const float ListTop = 184f;

		private const float RowHeight = 42f;

		private const float MinWindowWidth = 1040f;

		private const float MinWindowHeight = 820f;

		private const float ResizeGripSize = 26f;

		private const float RowButtonWidth = 62f;

		private const int ImeDuplicateFrameWindow = 3;

		private const int InspectorPageSize = 12;

		private const float InspectorTop = 154f;

		private const float InspectorRowHeight = 44f;

		private const float InspectorListTop = 198f;

		private const float InspectorAddToolbarTop = 132f;

		private static ItemManager? ActiveInstance;

		private static readonly string[] RuntimeDirectoryNames = new string[29]
		{
			"MiscItemDirectory", "ToolDirectory", "MedsItemDirectory", "FoodItemDirectory", "WineDirectory", "HydroponicDirectory", "HusbandryDirectory", "MaterialDirectory", "GunsItemDirectory", "GunModDirectory",
			"MeleeWeaponItemDirectory", "ExplosiveItemDirectory", "ArmorItemDirectory", "ContainerItemDirectory", "FurnitureItemDirectory", "ModuleDirectory", "ModItemDirectory", "StationMachinery", "RuinedMachineDirectory", "KeyItemDirectory",
			"OrganDirectory", "AmenitiesItemDirectory", "ConstructionItemDirectory", "EquipmentDirectory", "ShipItemDirectory", "ShipSystemDirectory", "TechnicianBackpackDirectory", "PlayerAbilityItemDirectory", "UnusedDirectory"
		};

		private static readonly TagPreset[] TagPresets = new TagPreset[20]
		{
			new TagPreset("MATERIAL_TAG", "材料 / Material"),
			new TagPreset("ORE_TAG", "矿石 / Ore"),
			new TagPreset("CONSTRUCTION_MATERIAL_TAG", "建筑材料 / Construction"),
			new TagPreset("FOURNITURE_TAG", "家具 / Furniture"),
			new TagPreset("EQUIPMENT_TAG", "装备 / Equipment"),
			new TagPreset("MELEE_WEAPON_TAG", "近战武器 / Melee"),
			new TagPreset("MELEE_SHARP_WEAPON_TAG", "锐器 / Sharp melee"),
			new TagPreset("MELEE_BLUNT_WEAPON_TAG", "钝器 / Blunt melee"),
			new TagPreset("ORGAN_ITEM_TAG", "器官 / Organ"),
			new TagPreset("HUMAN_ORGAN_TAG", "人体器官 / Human organ"),
			new TagPreset("WATER_TESTING_STRIP_TAG", "水质测试条 / Water test"),
			new TagPreset("WATER_PURIFICATION_TABLET", "净水片 / Purification tablet"),
			new TagPreset("NUTRIENT_TABLET", "营养片 / Nutrient tablet"),
			new TagPreset("WATER_FILTER", "水过滤器 / Water filter"),
			new TagPreset("SURGERY_TOOL_TAG", "手术工具 / Surgery tool"),
			new TagPreset("ANTIQUE_FIREARM_TAG", "古董枪械 / Antique firearm"),
			new TagPreset("EXOTIC_FUEL_TAG", "特殊燃料 / Exotic fuel"),
			new TagPreset("JUNK_MODULE_INPUT", "垃圾模块输入 / Junk module input"),
			new TagPreset("ORGANIC_ORGAN_TAG", "有机器官 / Organic organ"),
			new TagPreset("DOOR_TAG", "门 / Door")
		};

		private static readonly FeaturePreset[] FeaturePresets = new FeaturePreset[12]
		{
			new FeaturePreset("free", "免费 / Free", () => ItemFeatureList.FreeFeature()),
			new FeaturePreset("retail_markup", "零售加价 / Retail markup", () => ItemFeatureList.RetailMarkUp()),
			new FeaturePreset("donation", "捐赠 / Donation", () => ItemFeatureList.Donation()),
			new FeaturePreset("discounted_buy", "折扣购买 / Discounted buy", () => ItemFeatureList.DiscountedBuy()),
			new FeaturePreset("premium_buy", "高级购买 / Premium buy", () => ItemFeatureList.PremiumBuy()),
			new FeaturePreset("cigarette_authenticity", "香烟真伪 / Cigarette authenticity", () => ItemFeatureList.CigaretteAuthenticity()),
			new FeaturePreset("stamp_authenticity", "邮票真伪 / Stamp authenticity", () => ItemFeatureList.StampAuthenticity()),
			new FeaturePreset("module_stuck", "模块卡住 / Module stuck", () => ItemFeatureList.ModuleStuckFeature()),
			new FeaturePreset("discount_25", "25% 折扣 / 25% discount", () => ItemFeatureList.Discount(25)),
			new FeaturePreset("bargain_markup", "议价加价 / Bargain markup", () => ItemFeatureList.BargainMarkup(10)),
			new FeaturePreset("bargain_discount", "议价折扣 / Bargain discount", () => ItemFeatureList.BargainBuyingDiscount(10)),
			new FeaturePreset("equipment_good", "良好装备状态 / Good equipment", () => ItemFeatureList.EquipementConditionFeature((EquipmentCondition)2))
		};

		private static readonly GeneratedEntryDefinition[] GeneratedEntries = new GeneratedEntryDefinition[30]
		{
			new GeneratedEntryDefinition("table:junk", "随机掉落：杂项", "Random drop: Junk"),
			new GeneratedEntryDefinition("table:access_card", "随机掉落：门禁卡", "Random drop: Access card"),
			new GeneratedEntryDefinition("table:all_module", "随机掉落：全部模块", "Random drop: All modules"),
			new GeneratedEntryDefinition("table:makeshift_weapon", "随机掉落：简易武器", "Random drop: Makeshift weapon"),
			new GeneratedEntryDefinition("table:material", "随机掉落：材料", "Random drop: Material"),
			new GeneratedEntryDefinition("table:household", "随机掉落：日用品", "Random drop: Household"),
			new GeneratedEntryDefinition("table:packed_food", "随机掉落：包装食品", "Random drop: Packed food"),
			new GeneratedEntryDefinition("table:t1module", "随机掉落：一级模块", "Random drop: Tier 1 module"),
			new GeneratedEntryDefinition("table:t2module", "随机掉落：二级模块", "Random drop: Tier 2 module"),
			new GeneratedEntryDefinition("table:tool", "随机掉落：工具", "Random drop: Tool"),
			new GeneratedEntryDefinition("table:medical", "随机掉落：医疗品", "Random drop: Medical"),
			new GeneratedEntryDefinition("prebuilt:random_cigarette", "变体：随机香烟", "Variant: Random cigarette"),
			new GeneratedEntryDefinition("prebuilt:fake_cigarette", "变体：假香烟", "Variant: Fake cigarette"),
			new GeneratedEntryDefinition("prebuilt:counterfeit_cigarette", "变体：仿制香烟", "Variant: Counterfeit cigarette"),
			new GeneratedEntryDefinition("prebuilt:random_injector", "变体：随机注射器", "Variant: Random injector"),
			new GeneratedEntryDefinition("prebuilt:genuine_injector", "变体：真注射器", "Variant: Genuine injector"),
			new GeneratedEntryDefinition("prebuilt:expired_injector", "变体：过期注射器", "Variant: Expired injector"),
			new GeneratedEntryDefinition("prebuilt:counterfeit_injector", "变体：仿制注射器", "Variant: Counterfeit injector"),
			new GeneratedEntryDefinition("prebuilt:random_stamp", "变体：随机邮票", "Variant: Random stamp"),
			new GeneratedEntryDefinition("prebuilt:fake_stamp", "变体：假邮票", "Variant: Fake stamp"),
			new GeneratedEntryDefinition("prebuilt:real_stamp", "变体：真邮票", "Variant: Real stamp"),
			new GeneratedEntryDefinition("prebuilt:random_performance_module", "变体：随机性能模块", "Variant: Random performance module"),
			new GeneratedEntryDefinition("prebuilt:random_efficiency_module", "变体：随机效率模块", "Variant: Random efficiency module"),
			new GeneratedEntryDefinition("prebuilt:random_quality_module", "变体：随机质量模块", "Variant: Random quality module"),
			new GeneratedEntryDefinition("prebuilt:random_overclock_module", "变体：随机超频模块", "Variant: Random overclock module"),
			new GeneratedEntryDefinition("prebuilt:random_eco_module", "变体：随机生态模块", "Variant: Random eco module"),
			new GeneratedEntryDefinition("prebuilt:random_fineness_module", "变体：随机精细模块", "Variant: Random fineness module"),
			new GeneratedEntryDefinition("prebuilt:random_node", "变体：随机节点", "Variant: Random node"),
			new GeneratedEntryDefinition("prebuilt:random_ribwich", "变体：随机 Ribwich", "Variant: Random Ribwich"),
			new GeneratedEntryDefinition("prebuilt:random_hand", "变体：随机手部物品", "Variant: Random hand item")
		};

		private readonly List<ItemEntry> _catalog = new List<ItemEntry>();

		private readonly List<ItemEntry> _filtered = new List<ItemEntry>();

		private Rect _windowRect;

		private string _searchText = string.Empty;

		private string _manualId = string.Empty;

		private string _pageJumpText = string.Empty;

		private string _status = "进入存档后按 Alt+I 打开物品面板。物品会放入玩家一侧的柜台区域。";

		private bool _windowOpen;

		private bool _manualIdMode;

		private bool _pageJumpMode;

		private bool _clearInputFocus;

		private bool _focusInputRequested;

		private string _focusControlName = string.Empty;

		private bool _inputFieldFocused;

		private string _inputFieldName = string.Empty;

		private int _inputCaretIndex;

		private string _inputComposition = string.Empty;

		private bool _imeModeEnabled;

		private int _lastInputPollFrame = -1;

		private string _inputPolledText = string.Empty;

		private int _lastPolledImeFrame = -1;

		private string _lastPolledImeText = string.Empty;

		private int _lastEventImeFrame = -1;

		private string _lastEventImeText = string.Empty;

		private bool _englishUi;

		private bool _catalogLoaded;

		private bool _runtimeCatalogMerged;

		private bool _catalogValidationStarted;

		private bool _catalogValidationComplete;

		private int _validationIndex;

		private float _nextValidationTime;

		private int _amount = 1;

		private int _pageIndex;

		private string _lastUiFailure = string.Empty;

		private bool _mouseButtonWasDown;

		private bool _blockGamePointerUntilRelease;

		private int _capturedPointerReleaseFrame = -1;

		private bool _dragging;

		private bool _resizing;

		private float _dragOffsetX;

		private float _dragOffsetY;

		private float _resizeOffsetX;

		private float _resizeOffsetY;

		private GUIStyle? _labelStyle;

		private GUIStyle? _buttonStyle;

		private GUIStyle? _deleteDialogLabelStyle;

		private GUIStyle? _deleteDialogButtonStyle;

		private KeyCode _toggleKey = (KeyCode)105;

		private bool _toggleAlt = true;

		private bool _toggleControl;

		private bool _toggleShift;

		private string _toggleHotkeyLabel = "Alt+I";

		private KeyCode _deleteKey = (KeyCode)127;

		private bool _deleteAlt;

		private bool _deleteControl;

		private bool _deleteShift;

		private string _deleteHotkeyLabel = "Delete";

		private KeyCode _quickDeleteKey = (KeyCode)100;

		private bool _quickDeleteAlt;

		private bool _quickDeleteControl;

		private bool _quickDeleteShift = true;

		private string _quickDeleteHotkeyLabel = "Shift+D";

		private GameItem? _contextItem;

		private GameItem? _selectedItem;

		private GameItem? _pendingDeleteItem;

		private string _pendingDeleteIdentifier = string.Empty;

		private int _pendingDeleteUniqueId;

		private bool _contextMenuVisible;

		private bool _deleteConfirmVisible;

		private int _deleteDialogPressedButton = -1;

		private Rect _deleteDialogConfirmRect;

		private Rect _deleteDialogCancelRect;

		private bool _inspectorVisible;

		private GameItem? _inspectedItem;

		private int _inspectorSection;

		private int _inspectorPage;

		private string _inspectorTagKey = string.Empty;

		private bool _inspectorTagModified;

		private int _tagListPage;

		private int _tagAddPresetIndex;

		private bool _tagAddModified;

		private int _inspectorFeatureIndex = -1;

		private int _inspectorConditionSection;

		private int _featureListPage;

		private int _featureAddPresetIndex;

		private string _inspectorStatus = string.Empty;

		private readonly Dictionary<string, string> _editorInputValues = new Dictionary<string, string>(StringComparer.Ordinal);

		public override void OnInitializeMelon()
		{
			ActiveInstance = this;
			((MelonBase)this).HarmonyInstance.PatchAll(typeof(ItemManager));
			LoadConfig();
			LoadCatalog();
			_status = Ui("进入存档后按 " + _toggleHotkeyLabel + " 打开物品面板。右键物品后按 " + _deleteHotkeyLabel + " 可删除；" + _quickDeleteHotkeyLabel + " 可快速删除。", "Enter a save and press " + _toggleHotkeyLabel + " to open the item panel. Right-click an item, then press " + _deleteHotkeyLabel + " to delete it; " + _quickDeleteHotkeyLabel + " deletes immediately.");
			MelonLogger.Msg("Probably Stolen Item Manager 已加载。进入存档后按 " + _toggleHotkeyLabel + " 打开物品面板。" + (_catalogLoaded ? (" 物品表 " + _catalog.Count + " 条。") : " 未找到物品表，将仅支持手动标识符。"));
		}

		public override void OnUpdate()
		{
			if (!IsGameplayContextReady())
			{
				ClosePanelOutsideGameplay();
			}
			else
			{
				UpdateItemContextState();
			}
			if (_deleteConfirmVisible)
			{
				ProcessDeleteDialogMouse();
			}
			else if (!_inputFieldFocused && IsQuickDeleteHotkeyPressed())
			{
				QuickDeleteSelectedItem();
			}
			else if (_contextMenuVisible && _contextItem != null && !IsDeleteHotkeyBlockedByInput() && IsDeleteHotkeyPressed())
			{
				BeginDeleteConfirmation(_contextItem);
			}
			if (!_deleteConfirmVisible && IsToggleHotkeyPressed())
			{
				_windowOpen = !_windowOpen;
				_dragging = false;
				_resizing = false;
				_mouseButtonWasDown = false;
				_clearInputFocus = true;
				_focusInputRequested = false;
				_focusControlName = string.Empty;
				ClearInputFocusState();
				if (_windowOpen)
				{
					EnsureWindowRect();
				}
			}
			if (_windowOpen && !_deleteConfirmVisible)
			{
				ProcessWindowResize();
				ProcessWindowDrag();
				ProcessMouseClick();
				PollFocusedInput();
			}
			if ((!_catalogValidationComplete || !_runtimeCatalogMerged) && Time.realtimeSinceStartup >= _nextValidationTime)
			{
				ValidateCatalogChunk();
			}
		}

		public override void OnDeinitializeMelon()
		{
			((MelonBase)this).HarmonyInstance.UnpatchSelf();
			if (ActiveInstance == this)
			{
				ActiveInstance = null;
			}
		}

		[HarmonyPatch(typeof(EventSystem), "Update")]
		[HarmonyPrefix]
		private static bool BlockGameEventSystemWhileOverlayCapturesPointer()
		{
			return !InstanceShouldCaptureGamePointer();
		}

		private static bool InstanceShouldCaptureGamePointer()
		{
			if (ActiveInstance != null)
			{
				return ActiveInstance.ShouldCaptureGamePointer();
			}
			return false;
		}

		private bool IsToggleHotkeyPressed()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				return IsHotkeyPressed(_toggleKey, _toggleAlt, _toggleControl, _toggleShift);
			}
			catch (Exception ex)
			{
				LogUiFailure("hotkey", ex);
				return false;
			}
		}

		private bool IsDeleteHotkeyPressed()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				return IsHotkeyPressed(_deleteKey, _deleteAlt, _deleteControl, _deleteShift);
			}
			catch (Exception ex)
			{
				LogUiFailure("hotkey", ex);
				return false;
			}
		}

		private bool IsQuickDeleteHotkeyPressed()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				return IsHotkeyPressed(_quickDeleteKey, _quickDeleteAlt, _quickDeleteControl, _quickDeleteShift);
			}
			catch (Exception ex)
			{
				LogUiFailure("hotkey", ex);
				return false;
			}
		}

		private bool IsDeleteHotkeyBlockedByInput()
		{
			if (!_windowOpen || !_inputFieldFocused)
			{
				return false;
			}
			return IsMouseInsideWindow();
		}

		private static bool IsHotkeyPressed(KeyCode key, bool alt, bool control, bool shift)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			if (!Input.GetKeyDown(key))
			{
				return false;
			}
			if (alt && !IsAnyKeyHeld((KeyCode)308, (KeyCode)307))
			{
				return false;
			}
			if (control && !IsAnyKeyHeld((KeyCode)306, (KeyCode)305))
			{
				return false;
			}
			if (shift && !IsAnyKeyHeld((KeyCode)304, (KeyCode)303))
			{
				return false;
			}
			return true;
		}

		private static bool IsAnyKeyHeld(KeyCode first, KeyCode second)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			if (!Input.GetKey(first))
			{
				return Input.GetKey(second);
			}
			return true;
		}

		private void LoadConfig()
		{
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_0145: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			string path = Path.Combine(Path.GetDirectoryName(typeof(ItemManager).Assembly.Location) ?? AppContext.BaseDirectory, "item_mod_config.json");
			if (!File.Exists(path))
			{
				return;
			}
			try
			{
				JsonSerializerOptions options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};
				ItemManagerConfig itemManagerConfig = JsonSerializer.Deserialize<ItemManagerConfig>(File.ReadAllText(path), options);
				if (itemManagerConfig == null)
				{
					MelonLogger.Warning("Probably Stolen Item Manager: 配置中的 toggleHotkey 无法识别，将使用默认快捷键 Alt+I。支持例如 Alt+I、F6、Ctrl+Shift+I。");
					return;
				}
				ApplyLanguage(itemManagerConfig.Language);
				if (!TryParseHotkey(itemManagerConfig.ToggleHotkey, out KeyCode key, out bool alt, out bool control, out bool shift, out string label))
				{
					MelonLogger.Warning("Probably Stolen Item Manager: 配置中的 toggleHotkey 无法识别，将使用默认快捷键 Alt+I。支持例如 Alt+I、F6、Ctrl+Shift+I。");
					return;
				}
				_toggleKey = key;
				_toggleAlt = alt;
				_toggleControl = control;
				_toggleShift = shift;
				_toggleHotkeyLabel = label;
				if (!string.IsNullOrWhiteSpace(itemManagerConfig.DeleteHotkey))
				{
					if (!TryParseHotkey(itemManagerConfig.DeleteHotkey, out KeyCode key2, out bool alt2, out bool control2, out bool shift2, out string label2))
					{
						MelonLogger.Warning("Probably Stolen Item Manager: 配置中的 deleteHotkey 无法识别，将使用默认删除快捷键 Delete。支持例如 Delete、Alt+Delete、Ctrl+Shift+F8。");
					}
					else
					{
						_deleteKey = key2;
						_deleteAlt = alt2;
						_deleteControl = control2;
						_deleteShift = shift2;
						_deleteHotkeyLabel = label2;
					}
				}
				if (!string.IsNullOrWhiteSpace(itemManagerConfig.QuickDeleteHotkey))
				{
					if (!TryParseHotkey(itemManagerConfig.QuickDeleteHotkey, out KeyCode key3, out bool alt3, out bool control3, out bool shift3, out string label3))
					{
						MelonLogger.Warning("Probably Stolen Item Manager: 配置中的 quickDeleteHotkey 无法识别，将使用默认快捷键 Shift+D。支持例如 Shift+D、Alt+Delete、Ctrl+Shift+F8。");
						return;
					}
					_quickDeleteKey = key3;
					_quickDeleteAlt = alt3;
					_quickDeleteControl = control3;
					_quickDeleteShift = shift3;
					_quickDeleteHotkeyLabel = label3;
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("Probably Stolen Item Manager: 读取 item_mod_config.json 失败，将使用默认快捷键 Alt+I：" + ex.Message);
			}
		}

		private void ApplyLanguage(string? value)
		{
			string text = (value ?? "zh").Trim().ToLowerInvariant();
			int englishUi;
			switch (text)
			{
			default:
				englishUi = ((text == "英语") ? 1 : 0);
				break;
			case "en":
			case "en-us":
			case "english":
			case "英文":
				englishUi = 1;
				break;
			}
			_englishUi = (byte)englishUi != 0;
		}

		private string Ui(string chinese, string english)
		{
			if (!_englishUi)
			{
				return chinese;
			}
			return english;
		}

		private static bool TryParseHotkey(string? value, out KeyCode key, out bool alt, out bool control, out bool shift, out string label)
		{
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0102: Invalid comparison between Unknown and I4
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Expected I4, but got Unknown
			key = (KeyCode)105;
			alt = false;
			control = false;
			shift = false;
			label = "Alt+I";
			if (string.IsNullOrWhiteSpace(value))
			{
				return false;
			}
			bool flag = false;
			string[] array = value.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				switch (text.ToLowerInvariant())
				{
				case "alt":
				case "option":
				case "leftalt":
				case "rightalt":
					alt = true;
					continue;
				case "ctrl":
				case "control":
				case "leftcontrol":
				case "rightcontrol":
					control = true;
					continue;
				case "shift":
				case "leftshift":
				case "rightshift":
					shift = true;
					continue;
				}
				if ((!Enum.TryParse<KeyCode>(text, true, out KeyCode result) || (int)result == 0) | flag)
				{
					return false;
				}
				key = (KeyCode)(int)result;
				flag = true;
			}
			if (!flag)
			{
				return false;
			}
			List<string> list = new List<string>();
			if (control)
			{
				list.Add("Ctrl");
			}
			if (alt)
			{
				list.Add("Alt");
			}
			if (shift)
			{
				list.Add("Shift");
			}
			list.Add(((object)key/*cast due to constrained. prefix*/).ToString());
			label = string.Join("+", list);
			return true;
		}

		public override void OnGUI()
		{
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			if (!IsGameplayContextReady())
			{
				ClosePanelOutsideGameplay();
				return;
			}
			if (_windowOpen && !_deleteConfirmVisible)
			{
				EnsureGuiStyles();
				EnsureWindowRect();
				if (_clearInputFocus)
				{
					GUIUtility.keyboardControl = 0;
					ClearInputFocusState();
					_clearInputFocus = false;
				}
				WindowFunction val = WindowFunction.op_Implicit((Action<int>)DrawWindow);
				int fontSize = 22;
				int fontSize2 = 20;
				int fontSize3 = 23;
				GUIStyle label = GUI.skin.label;
				GUIStyle button = GUI.skin.button;
				GUIStyle window = GUI.skin.window;
				int fontSize4 = label.fontSize;
				int fontSize5 = button.fontSize;
				int fontSize6 = window.fontSize;
				try
				{
					label.fontSize = fontSize;
					button.fontSize = fontSize2;
					window.fontSize = fontSize3;
					_windowRect = GUI.Window(18427, _windowRect, val, "Probably Stolen Item Manager");
					ConsumeOverlayEvent();
				}
				finally
				{
					label.fontSize = fontSize4;
					button.fontSize = fontSize5;
					window.fontSize = fontSize6;
				}
			}
			DrawDeleteConfirmationPrompt();
		}

		private static bool IsGameplayContextReady()
		{
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance == (Object)null)
				{
					return false;
				}
				if (instance.invElement == null && instance.frontInvinvElement == null && instance.backInvinvElement == null && instance.backInvinvElementCounter == null)
				{
					return false;
				}
				PlayerStore instance2 = PlayerStore.Instance;
				if (instance2 == null || instance2.gridInv == null)
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

		private void ClosePanelOutsideGameplay()
		{
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			if (_windowOpen || _deleteConfirmVisible || _inspectorVisible)
			{
				_windowOpen = false;
				_deleteConfirmVisible = false;
				_pendingDeleteItem = null;
				_pendingDeleteIdentifier = string.Empty;
				_pendingDeleteUniqueId = 0;
				_deleteDialogPressedButton = -1;
				_deleteDialogConfirmRect = default;
				_deleteDialogCancelRect = default;
				CloseInspector();
				ClearContextDeleteState();
				_dragging = false;
				_resizing = false;
				_mouseButtonWasDown = false;
				_pageJumpMode = false;
				ClearInputFocusState();
				_clearInputFocus = true;
				_focusInputRequested = false;
				_focusControlName = string.Empty;
			}
		}

		private void UpdateItemContextState()
		{
			try
			{
				if (_deleteConfirmVisible)
				{
					return;
				}
				ItemContextHandler current = ItemContextHandler.current;
				if ((Object)(object)current == (Object)null)
				{
					ClearContextDeleteState();
					return;
				}
				GameItem currentItem = current.currentItem;
				if (currentItem == null)
				{
					ClearContextDeleteState();
					return;
				}
				bool flag = !IsSameItem(_selectedItem, currentItem);
				_selectedItem = currentItem;
				if ((_inspectorVisible & flag) && !IsSameItem(_inspectedItem, currentItem))
				{
					_inspectedItem = currentItem;
					_inspectorPage = 0;
					_inspectorTagKey = string.Empty;
					_inspectorFeatureIndex = -1;
					_inspectorConditionSection = 0;
					_tagListPage = 0;
					_featureListPage = 0;
					_editorInputValues.Clear();
					ClearInputFocusState();
					_clearInputFocus = true;
					_inspectorStatus = Ui("已切换当前物品，属性已刷新。", "Selected item changed; attributes refreshed.");
				}
				PixelWindow contextWindow = current.contextWindow;
				if (contextWindow != null && !contextWindow.IsVisible())
				{
					ClearContextDeleteState();
					return;
				}
				if (!_contextMenuVisible || !IsSameItem(_contextItem, currentItem))
				{
					_contextItem = currentItem;
				}
				_contextMenuVisible = true;
			}
			catch (Exception ex)
			{
				ClearContextDeleteState();
				LogUiFailure("context-state", ex);
			}
		}

		private void ClearContextDeleteState()
		{
			_contextMenuVisible = false;
			_contextItem = null;
		}

		private void DrawDeleteConfirmationPrompt()
		{
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0157: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0165: Unknown result type (might be due to invalid IL or missing references)
			//IL_0167: Unknown result type (might be due to invalid IL or missing references)
			//IL_016d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0185: Unknown result type (might be due to invalid IL or missing references)
			GameItem pendingDeleteItem = _pendingDeleteItem;
			if (_deleteConfirmVisible && pendingDeleteItem != null)
			{
				string itemLabel = GetItemLabel(pendingDeleteItem);
				float num = Math.Min(680f, Math.Max(480f, (float)Screen.width - 80f));
				string text = Ui("确认删除当前整组物品？\n物品：" + itemLabel + "\n确认后将从当前库存中移除。", "Delete this entire item stack?\nItem: " + itemLabel + "\nThe item will be removed from its inventory.");
				float num2 = num - 40f;
				float num3 = MeasureDeleteDialogText(text, num2);
				float num4 = Math.Clamp(num3 + 96f, 210f, Math.Max(210f, (float)Screen.height - 80f));
				float num5 = ((float)Screen.width - num) / 2f;
				float num6 = ((float)Screen.height - num4) / 2f;
				GUI.Box(new Rect(num5, num6, num, num4), string.Empty);
				DrawDeleteDialogLabel(new Rect(num5 + 20f, num6 + 16f, num2, num3), text);
				float num7 = 180f;
				float num8 = 44f;
				float num9 = 24f;
				float num10 = num7 * 2f + num9;
				float num11 = num5 + (num - num10) / 2f;
				float num12 = num6 + num4 - num8 - 22f;
				Rect val = new Rect(num11, num12, num7, num8);
				Rect val2 = new Rect(num11 + num7 + num9, num12, num7, num8);
				_deleteDialogConfirmRect = val;
				_deleteDialogCancelRect = val2;
				DrawDeleteDialogButton(val, Ui("确认删除", "Confirm deletion"));
				DrawDeleteDialogButton(val2, Ui("取消", "Cancel"));
				ConsumeDeleteDialogInput();
			}
		}

		private float MeasureDeleteDialogText(string text, float width)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected Obj, but got Unknown
			try
			{
				if (_deleteDialogLabelStyle != null)
				{
					return Math.Max(76f, _deleteDialogLabelStyle.CalcHeight(new GUIContent(text), width));
				}
			}
			catch (Exception ex)
			{
				LogUiFailure("delete-dialog-measure", ex);
			}
			return 96f;
		}

		private void DrawDeleteDialogLabel(Rect rect, string text)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (_deleteDialogLabelStyle != null)
				{
					GUI.Label(rect, text, _deleteDialogLabelStyle);
				}
				else
				{
					GUI.Label(rect, text);
				}
			}
			catch (Exception ex)
			{
				LogUiFailure("delete-dialog-label", ex);
			}
		}

		private void DrawDeleteDialogButton(Rect rect, string text)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (_deleteDialogButtonStyle != null)
				{
					GUI.Button(rect, text, _deleteDialogButtonStyle);
				}
				else
				{
					GUI.Button(rect, text);
				}
			}
			catch (Exception ex)
			{
				LogUiFailure("delete-dialog-button", ex);
			}
		}

		private void ProcessDeleteDialogMouse()
		{
			try
			{
				if (Input.GetMouseButtonDown(0))
				{
					_deleteDialogPressedButton = GetDeleteDialogButtonAtMouse();
				}
				if (Input.GetMouseButtonUp(0))
				{
					int deleteDialogPressedButton = _deleteDialogPressedButton;
					int deleteDialogButtonAtMouse = GetDeleteDialogButtonAtMouse();
					_deleteDialogPressedButton = -1;
					if (deleteDialogPressedButton == 1 && deleteDialogButtonAtMouse == 1)
					{
						ConfirmDelete();
					}
					else if (deleteDialogPressedButton == 2 && deleteDialogButtonAtMouse == 2)
					{
						CancelDeleteConfirmation();
					}
				}
			}
			catch (Exception ex)
			{
				_deleteDialogPressedButton = -1;
				LogUiFailure("delete-dialog-input", ex);
			}
		}

		private int GetDeleteDialogButtonAtMouse()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			Vector3 mousePosition = Input.mousePosition;
			Vector2 val = new Vector2(mousePosition.x, (float)Screen.height - mousePosition.y);
			if (_deleteDialogConfirmRect.Contains(val))
			{
				return 1;
			}
			if (_deleteDialogCancelRect.Contains(val))
			{
				return 2;
			}
			return -1;
		}

		private static void ConsumeDeleteDialogInput()
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Expected I4, but got Unknown
			Event current = Event.current;
			if (current != null)
			{
				EventType type = current.type;
				switch ((int)type)
				{
				case 0:
				case 1:
				case 2:
				case 3:
				case 4:
				case 5:
				case 6:
				case 13:
				case 14:
				case 16:
					current.Use();
					break;
				case 7:
				case 8:
				case 9:
				case 10:
				case 11:
				case 12:
				case 15:
					break;
				}
			}
		}

		private void BeginDeleteConfirmation(GameItem item)
		{
			_pendingDeleteItem = item;
			_pendingDeleteIdentifier = ReadItemIdentifier(item);
			_pendingDeleteUniqueId = ReadItemUniqueId(item);
			_deleteConfirmVisible = true;
			_deleteDialogPressedButton = -1;
			_contextMenuVisible = false;
			_status = Ui("请确认删除：", "Confirm deletion: ") + GetItemLabel(item);
		}

		private void CancelDeleteConfirmation()
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			_pendingDeleteItem = null;
			_pendingDeleteIdentifier = string.Empty;
			_pendingDeleteUniqueId = 0;
			_deleteConfirmVisible = false;
			_deleteDialogPressedButton = -1;
			_deleteDialogConfirmRect = default;
			_deleteDialogCancelRect = default;
			_status = Ui("已取消删除。", "Deletion cancelled.");
		}

		private void ConfirmDelete()
		{
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			GameItem pendingDeleteItem = _pendingDeleteItem;
			string pendingDeleteIdentifier = _pendingDeleteIdentifier;
			if (pendingDeleteItem == null)
			{
				CancelDeleteConfirmation();
				return;
			}
			if (_pendingDeleteUniqueId != 0 && ReadItemUniqueId(pendingDeleteItem) != _pendingDeleteUniqueId)
			{
				CancelDeleteConfirmation();
				_status = Ui("物品已变化，未执行删除。", "The item changed; nothing was deleted.");
				return;
			}
			string itemLabel = GetItemLabel(pendingDeleteItem);
			if (TryDeleteItem(pendingDeleteItem, out string detail))
			{
				_status = Ui("已删除：", "Deleted: ") + itemLabel;
				MelonLogger.Msg("Probably Stolen Item Manager: removed item " + pendingDeleteIdentifier);
				_contextItem = null;
				if (IsSameItem(_selectedItem, pendingDeleteItem))
				{
					_selectedItem = null;
				}
				if (IsSameItem(_inspectedItem, pendingDeleteItem))
				{
					CloseInspector();
				}
				_contextMenuVisible = false;
			}
			else
			{
				_status = Ui("删除失败：", "Could not delete: ") + itemLabel + (string.IsNullOrWhiteSpace(detail) ? string.Empty : Ui("（" + detail + "）", " (" + detail + ")"));
				MelonLogger.Warning("Probably Stolen Item Manager: remove failed for " + pendingDeleteIdentifier + (string.IsNullOrWhiteSpace(detail) ? string.Empty : (": " + detail)));
			}
			_pendingDeleteItem = null;
			_pendingDeleteIdentifier = string.Empty;
			_pendingDeleteUniqueId = 0;
			_deleteConfirmVisible = false;
			_deleteDialogPressedButton = -1;
			_deleteDialogConfirmRect = default;
			_deleteDialogCancelRect = default;
		}

		private void QuickDeleteSelectedItem()
		{
			GameItem val = _selectedItem ?? _contextItem;
			if (val == null)
			{
				_status = Ui("没有已选中的物品可快速删除。", "No selected item to delete immediately.");
				return;
			}
			string text = ReadItemIdentifier(val);
			string itemLabel = GetItemLabel(val);
			if (TryDeleteItem(val, out string detail))
			{
				_status = Ui("已快速删除：", "Deleted immediately: ") + itemLabel;
				MelonLogger.Msg("Probably Stolen Item Manager: quick-removed item " + text);
				_contextItem = null;
				if (IsSameItem(_selectedItem, val))
				{
					_selectedItem = null;
				}
				if (IsSameItem(_inspectedItem, val))
				{
					CloseInspector();
				}
				_contextMenuVisible = false;
			}
			else
			{
				_status = Ui("快速删除失败：", "Immediate delete failed: ") + itemLabel + (string.IsNullOrWhiteSpace(detail) ? string.Empty : Ui("（" + detail + "）", " (" + detail + ")"));
				MelonLogger.Warning("Probably Stolen Item Manager: quick removal failed for " + text + (string.IsNullOrWhiteSpace(detail) ? string.Empty : (": " + detail)));
			}
		}

		private bool TryDeleteItem(GameItem item, out string detail)
		{
			detail = string.Empty;
			GameInventory val = null;
			try
			{
				val = item.parentInventory;
			}
			catch (Exception ex)
			{
				detail = ex.Message;
			}
			if (val != null && TryExpelAndDestroy(val, item, out detail))
			{
				RefreshItemAreas();
				return true;
			}
			foreach (GameInventory knownInventory in GetKnownInventories())
			{
				if (InventoryContains(knownInventory, item) && TryExpelAndDestroy(knownInventory, item, out detail))
				{
					RefreshItemAreas();
					return true;
				}
			}
			if (TryDeleteKnownFixture(item, out detail))
			{
				RefreshItemAreas();
				return true;
			}
			if (string.IsNullOrWhiteSpace(detail))
			{
				detail = Ui("物品不在可移除的库存中", "item is not in a removable inventory");
			}
			return false;
		}

		private static bool TryDeleteKnownFixture(GameItem item, out string detail)
		{
			detail = string.Empty;
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance == (Object)null)
				{
					return false;
				}
				if (IsSameItem(instance.trashcan, item) && instance.trashcanInvElement != null)
				{
					return TryExpelAndDestroy((GameInventory)(object)instance.trashcanInvElement, item, out detail);
				}
				if (IsSameItem(instance.drain, item) && instance.drainInvElement != null)
				{
					return TryExpelAndDestroy((GameInventory)(object)instance.drainInvElement, item, out detail);
				}
			}
			catch (Exception ex)
			{
				detail = ex.Message;
			}
			return false;
		}

		private static bool TryExpelAndDestroy(GameInventory inventory, GameItem item, out string detail)
		{
			detail = string.Empty;
			bool flag = false;
			bool overrideLockRemove = false;
			try
			{
				try
				{
					overrideLockRemove = inventory.overrideLockRemove;
					inventory.overrideLockRemove = true;
					flag = true;
				}
				catch
				{
				}
				if (!inventory.Expel(item))
				{
					detail = "inventory rejected removal";
					return false;
				}
				try
				{
					item.Destroy();
				}
				catch (Exception ex)
				{
					detail = "item destroy failed: " + ex.Message;
				}
				return true;
			}
			catch (Exception ex2)
			{
				detail = ex2.Message;
				return false;
			}
			finally
			{
				if (flag)
				{
					try
					{
						inventory.overrideLockRemove = overrideLockRemove;
					}
					catch
					{
					}
				}
			}
		}

		private static void RefreshItemAreas()
		{
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance != null)
				{
					instance.RefreshCounterItem();
				}
			}
			catch
			{
			}
			try
			{
				EmporiumEntry instance2 = EmporiumEntry.Instance;
				if (instance2 != null)
				{
					instance2.Validate(false);
				}
			}
			catch
			{
			}
		}

		private static List<GameInventory> GetKnownInventories()
		{
			List<GameInventory> list = new List<GameInventory>();
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance != null)
				{
					AddKnownInventory(list, (GameInventory?)(object)instance.gridInv);
				}
			}
			catch
			{
			}
			try
			{
				EmporiumEntry instance2 = EmporiumEntry.Instance;
				if ((Object)(object)instance2 == (Object)null)
				{
					return list;
				}
				AddKnownInventory(list, (GameInventory?)(object)instance2.invElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.showcaseElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.docInvElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.trashInvElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.swapBufferElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.drainInvElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.frontInvinvElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.backInvinvElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.backInvinvElementCounter);
				AddKnownInventory(list, (GameInventory?)(object)instance2.bazarLeftinvElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.hiddenElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.faucetElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.cassettePlayerElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.vendingMachineElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.vendingFountainElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.soldElement);
				AddKnownInventory(list, (GameInventory?)(object)instance2.responseInventory);
				AddKnownInventory(list, (GameInventory?)(object)instance2.responseInventoryClosable);
				AddKnownInventory(list, (GameInventory?)(object)instance2.afterhourInventory);
				AddKnownInventory(list, (GameInventory?)(object)instance2.afterhourPocketSlotInvLeft);
				AddKnownInventory(list, (GameInventory?)(object)instance2.afterhourPocketSlotInvBackpack);
				AddKnownInventory(list, (GameInventory?)(object)instance2.afterhourPocketSlotInvRight);
				AddKnownInventory(list, (GameInventory?)(object)instance2.hirelingInv);
				AddKnownInventory(list, (GameInventory?)(object)instance2.trashcanInvElement);
			}
			catch
			{
			}
			return list;
		}

		private static void AddKnownInventory(List<GameInventory> inventories, GameInventory? inventory)
		{
			if (inventory == null)
			{
				return;
			}
			foreach (GameInventory inventory2 in inventories)
			{
				if (inventory2 == inventory)
				{
					return;
				}
			}
			inventories.Add(inventory);
		}

		private static bool InventoryContains(GameInventory inventory, GameItem item)
		{
			try
			{
				GameGridInventory val = (GameGridInventory)(object)((inventory is GameGridInventory) ? inventory : null);
				if (val != null)
				{
					List<GameItem> items = val.items;
					if (items != null)
					{
						for (int i = 0; i < items.Count; i++)
						{
							if (IsSameItem(items[i], item))
							{
								return true;
							}
						}
					}
				}
				GameSlotInventory val2 = (GameSlotInventory)(object)((inventory is GameSlotInventory) ? inventory : null);
				if (val2 != null && IsSameItem(val2.currentItem, item))
				{
					return true;
				}
				List<GameItem> childItems = inventory.childItems;
				if (childItems != null)
				{
					for (int j = 0; j < childItems.Count; j++)
					{
						if (IsSameItem(childItems[j], item))
						{
							return true;
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}

		private static bool IsSameItem(GameItem? first, GameItem? second)
		{
			if (first == null || second == null)
			{
				return false;
			}
			if (first == second)
			{
				return true;
			}
			try
			{
				int uniqueId = first.uniqueId;
				int uniqueId2 = second.uniqueId;
				if (uniqueId != 0 && uniqueId == uniqueId2)
				{
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private static string ReadItemIdentifier(GameItem item)
		{
			try
			{
				return item.identifier ?? string.Empty;
			}
			catch
			{
				return string.Empty;
			}
		}

		private static int ReadItemUniqueId(GameItem item)
		{
			try
			{
				return item.uniqueId;
			}
			catch
			{
				return 0;
			}
		}

		private static string GetItemLabel(GameItem item)
		{
			try
			{
				string displayName = item.GetDisplayName(false);
				if (!string.IsNullOrWhiteSpace(displayName))
				{
					return displayName;
				}
			}
			catch
			{
			}
			string text = ReadItemIdentifier(item);
			if (!string.IsNullOrWhiteSpace(text))
			{
				return text;
			}
			return "(unknown item)";
		}

		private void ConsumeOverlayEvent()
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Invalid comparison between Unknown and I4
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Invalid comparison between Unknown and I4
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Invalid comparison between Unknown and I4
			Event current = Event.current;
			if (current != null && IsMouseInsideWindow())
			{
				EventType type = current.type;
				if ((int)type <= 3 || (int)type == 6 || (int)type == 16)
				{
					current.Use();
				}
			}
		}

		private bool IsMouseInsideWindow()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Vector3 mousePosition = Input.mousePosition;
				float num = mousePosition.x - _windowRect.x;
				float num2 = (float)Screen.height - mousePosition.y - _windowRect.y;
				return num >= 0f && num <= _windowRect.width && num2 >= 0f && num2 <= _windowRect.height;
			}
			catch (Exception ex)
			{
				LogUiFailure("mouse-position", ex);
				return false;
			}
		}

		private bool ShouldCaptureGamePointer()
		{
			if (_deleteConfirmVisible)
			{
				return true;
			}
			bool flag = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
			if (_windowOpen)
			{
				bool flag2 = IsMouseInsideWindow();
				if (flag && (flag2 || _blockGamePointerUntilRelease))
				{
					_blockGamePointerUntilRelease = true;
					return true;
				}
				if (flag2)
				{
					return true;
				}
			}
			if (!_blockGamePointerUntilRelease)
			{
				return _capturedPointerReleaseFrame == Time.frameCount;
			}
			if (flag)
			{
				return true;
			}
			_blockGamePointerUntilRelease = false;
			_capturedPointerReleaseFrame = Time.frameCount;
			return true;
		}

		private void EnsureGuiStyles()
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Expected Obj, but got Unknown
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Expected Obj, but got Unknown
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Expected Obj, but got Unknown
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Expected Obj, but got Unknown
			if (_labelStyle != null && _buttonStyle != null && _deleteDialogLabelStyle != null && _deleteDialogButtonStyle != null)
			{
				return;
			}
			try
			{
				_labelStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 22,
					alignment = (TextAnchor)3,
					wordWrap = false,
					clipping = (TextClipping)1
				};
				_buttonStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 20,
					alignment = (TextAnchor)4,
					wordWrap = false,
					clipping = (TextClipping)1
				};
				_deleteDialogLabelStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 22,
					alignment = (TextAnchor)3,
					wordWrap = true,
					clipping = (TextClipping)1
				};
				_deleteDialogButtonStyle = new GUIStyle(GUI.skin.button)
				{
					fontSize = 20,
					alignment = (TextAnchor)4,
					wordWrap = false,
					clipping = (TextClipping)1
				};
			}
			catch (Exception ex)
			{
				_labelStyle = null;
				_buttonStyle = null;
				_deleteDialogLabelStyle = null;
				_deleteDialogButtonStyle = null;
				LogUiFailure("styles", ex);
			}
		}

		private void DrawWindow(int windowId)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_0214: Unknown result type (might be due to invalid IL or missing references)
			//IL_021a: Unknown result type (might be due to invalid IL or missing references)
			//IL_025a: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0318: Unknown result type (might be due to invalid IL or missing references)
			//IL_034c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0385: Unknown result type (might be due to invalid IL or missing references)
			//IL_03be: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0429: Unknown result type (might be due to invalid IL or missing references)
			//IL_045b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0490: Unknown result type (might be due to invalid IL or missing references)
			//IL_0583: Unknown result type (might be due to invalid IL or missing references)
			//IL_0545: Unknown result type (might be due to invalid IL or missing references)
			//IL_062d: Unknown result type (might be due to invalid IL or missing references)
			//IL_06d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_070e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0755: Unknown result type (might be due to invalid IL or missing references)
			//IL_079f: Unknown result type (might be due to invalid IL or missing references)
			//IL_07dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_085b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0894: Unknown result type (might be due to invalid IL or missing references)
			//IL_089a: Unknown result type (might be due to invalid IL or missing references)
			//IL_08db: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a44: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a8a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0a0e: Unknown result type (might be due to invalid IL or missing references)
			float width = _windowRect.width;
			float height = _windowRect.height;
			SafeLabel(new Rect(12f, 26f, width - 248f, 30f), FitText(Ui("按 " + _toggleHotkeyLabel + " 隐藏；右键物品后按 " + _deleteHotkeyLabel + " 删除。", "Press " + _toggleHotkeyLabel + " to hide; right-click an item and press " + _deleteHotkeyLabel + " to delete."), width - 248f), "label-1");
			string text = (_inspectorVisible ? Ui("返回", "Back") : Ui("属性", "Properties"));
			SafeButton(new Rect(width - 224f, 26f, 112f, 32f), text, "button-inspector");
			SafeButton(new Rect(width - 100f, 26f, 88f, 32f), Ui("关闭", "Close"), "button-close");
			if (_inspectorVisible)
			{
				DrawInspectorWindow(width, height);
				return;
			}
			string text2 = (_manualIdMode ? Ui("直接 ID", "ID") : Ui("搜索", "Find"));
			string value = (_manualIdMode ? _manualId : _searchText);
			SafeLabel(new Rect(12f, 60f, 58f, 32f), text2 + (_englishUi ? ":" : "："), "label-input");
			string controlName = (_manualIdMode ? "item-manager-manual-id" : "item-manager-search");
			Rect rect = new Rect(72f, 60f, Math.Max(220f, width - 72f - 190f), 32f);
			DrawCustomInputField(rect, value, controlName, Ui("点击此处输入中文、英文或 ID", "Click here to enter Chinese, English, or an ID"), "text-input");
			ApplyFocusRequest(controlName);
			if (SafeButton(new Rect(width - 174f, 60f, 76f, 32f), Ui("清空", "Clear"), "button-clear"))
			{
				if (_manualIdMode)
				{
					_manualId = string.Empty;
				}
				else
				{
					_searchText = string.Empty;
					_pageIndex = 0;
				}
			}
			if (SafeButton(new Rect(width - 90f, 60f, 78f, 32f), _manualIdMode ? Ui("搜索模式", "Search") : Ui("手动 ID", "ID"), "button-mode"))
			{
				_manualIdMode = !_manualIdMode;
			}
			SafeLabel(new Rect(12f, 98f, 44f, 32f), Ui("数量", "Qty"), "label-amount");
			if (SafeButton(new Rect(58f, 98f, 32f, 32f), "?", "button-amount-minus"))
			{
				SetAmountValue(_amount - 1);
			}
			SafeLabel(new Rect(94f, 98f, 72f, 32f), "× " + _amount, "label-amount-value");
			if (SafeButton(new Rect(168f, 98f, 32f, 32f), "+", "button-amount-plus"))
			{
				SetAmountValue(_amount + 1);
			}
			if (SafeButton(new Rect(206f, 98f, 44f, 32f), "×1", "button-amount-1"))
			{
				SetAmountValue(1);
			}
			if (SafeButton(new Rect(254f, 98f, 44f, 32f), "×5", "button-amount-5"))
			{
				SetAmountValue(5);
			}
			if (SafeButton(new Rect(302f, 98f, 52f, 32f), "×10", "button-amount-10"))
			{
				SetAmountValue(10);
			}
			SafeLabel(new Rect(365f, 98f, width - 377f, 32f), FitText(_catalogValidationComplete ? (Ui("可用标识/生成：", "Available/generators: ") + _catalog.Count((ItemEntry x) => x.IsAvailable) + "/" + _catalog.Count) : Ui("正在读取游戏物品目录…", "Reading game item directory…"), width - 377f), "label-availability");
			if (_manualIdMode && SafeButton(new Rect(width - 86f, 98f, 74f, 32f), Ui("添加 ID", "Add ID"), "button-add-manual"))
			{
				AddManualItem();
			}
			SafeLabel(new Rect(12f, 136f, width - 24f, 30f), FitText(_pageJumpMode ? Ui("跳转页码：先点击下方页码框，再输入数字；Enter 确认，Esc 取消。", "Page jump: click the page field below, enter a number, then press Enter. Esc cancels.") : Ui("先点击上方输入框，再输入中文、英文或物品 ID；Backspace 删除。", "Click the input field above before typing Chinese, English, or an item ID. Backspace deletes."), width - 24f), "label-help");
			RebuildFiltered();
			int num = Math.Max(1, (_filtered.Count + 12 - 1) / 12);
			_pageIndex = Math.Clamp(_pageIndex, 0, num - 1);
			float num2 = width - 24f;
			if (_filtered.Count == 0)
			{
				SafeLabel(new Rect(18f, 184f, num2 - 12f, 30f), FitText(Ui("没有匹配项。可点击右侧“手动 ID”输入实际标识符。", "No matches. Switch to ID mode to enter an exact identifier."), num2 - 12f), "label-empty");
			}
			else
			{
				int num3 = _pageIndex * 12;
				int num4 = Math.Min(num3 + 12, _filtered.Count);
				for (int num5 = num3; num5 < num4; num5++)
				{
					DrawEntryRow(_filtered[num5], 12f, 184f + (float)(num5 - num3) * 42f, num2);
				}
			}
			float num6 = 696f;
			if (SafeButton(new Rect(12f, num6, 82f, 32f), Ui("首页", "Home"), "button-first"))
			{
				_pageIndex = 0;
			}
			if (SafeButton(new Rect(102f, num6, 80f, 32f), Ui("上一页", "Prev"), "button-prev"))
			{
				_pageIndex = Math.Max(0, _pageIndex - 1);
			}
			if (SafeButton(new Rect(190f, num6, 80f, 32f), Ui("下一页", "Next"), "button-next"))
			{
				_pageIndex = Math.Min(num - 1, _pageIndex + 1);
			}
			if (SafeButton(new Rect(278f, num6, 74f, 32f), Ui("尾页", "End"), "button-last"))
			{
				_pageIndex = num - 1;
			}
			if (SafeButton(new Rect(360f, num6, 72f, 32f), Ui("跳转", "Go"), "button-jump"))
			{
				_pageJumpMode = true;
				_pageJumpText = string.Empty;
				_clearInputFocus = true;
				_focusInputRequested = false;
				_focusControlName = string.Empty;
				_status = Ui("请输入页码后按 Enter 确认。", "Enter a page number, then press Enter to confirm.");
			}
			if (_pageJumpMode)
			{
				SafeLabel(new Rect(444f, num6, 54f, 32f), Ui("页码：", "Pg:"), "label-page");
				string controlName2 = "item-manager-page";
				Rect rect2 = new Rect(498f, num6, 100f, 32f);
				DrawCustomInputField(rect2, _pageJumpText, controlName2, Ui("输入页码", "Page"), "text-page", digitsOnly: true);
				ApplyFocusRequest(controlName2);
				SafeLabel(new Rect(606f, num6, width - 618f, 32f), " / " + num, "label-page-total");
			}
			else
			{
				string text3 = (_englishUi ? ("Page " + (_pageIndex + 1) + "/" + num + ", " + _filtered.Count + " items (" + 12 + " per page)") : ("第 " + (_pageIndex + 1) + "/" + num + " 页，共 " + _filtered.Count + " 项（每页 " + 12 + " 项）"));
				SafeLabel(new Rect(444f, num6, width - 456f, 32f), FitText(text3, width - 456f), "label-page");
			}
			SafeLabel(new Rect(12f, height - 42f, width - 24f, 28f), FitText(_status, width - 24f), "label-status");
			SafeLabel(new Rect(width - 26f - 2f, height - 26f - 2f, 26f, 26f), "↘", "resize-grip");
		}

		private void DrawInspectorWindow(float width, float height)
		{
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0148: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			GameItem inspectedItem = _inspectedItem;
			if (inspectedItem == null)
			{
				SafeLabel(new Rect(12f, 66f, width - 24f, 34f), Ui("没有可查看的物品。请先在游戏中右键一个物品，再打开属性。", "No item is selected. Right-click an item in the game, then open Properties."), "inspector-empty");
				DrawInspectorFooter(width, height);
				return;
			}
			string itemLabel = GetItemLabel(inspectedItem);
			SafeLabel(new Rect(12f, 62f, width - 24f, 30f), FitText(Ui("当前物品：", "Selected item: ") + itemLabel, width - 24f), "inspector-item");
			if (_inspectorTagKey.Length > 0)
			{
				SafeButton(new Rect(12f, 96f, 150f, 32f), Ui("返回标签", "Back to tags"), "inspector-back-tags");
				DrawTagEditor(inspectedItem, width);
			}
			else if (_inspectorFeatureIndex >= 0)
			{
				SafeButton(new Rect(12f, 96f, 180f, 32f), Ui("返回特性", "Back to features"), "inspector-back-features");
				DrawFeatureEditor(inspectedItem, width);
			}
			else
			{
				SafeButton(new Rect(12f, 96f, 92f, 32f), Ui("基础", "Base"), "inspector-tab-base");
				SafeButton(new Rect(112f, 96f, 92f, 32f), Ui("标签", "Tags"), "inspector-tab-tags");
				SafeButton(new Rect(212f, 96f, 112f, 32f), Ui("特性", "Features"), "inspector-tab-features");
				SafeButton(new Rect(332f, 96f, 94f, 32f), Ui("刷新", "Refresh"), "inspector-refresh");
				switch (_inspectorSection)
				{
				case 1:
					DrawTagList(inspectedItem, width);
					break;
				case 2:
					DrawFeatureList(inspectedItem, width);
					break;
				default:
					DrawBaseInspector(inspectedItem, width);
					break;
				}
			}
			DrawInspectorFooter(width, height);
		}

		private void DrawInspectorFooter(float width, float height)
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			string text = (string.IsNullOrWhiteSpace(_inspectorStatus) ? _status : _inspectorStatus);
			SafeLabel(new Rect(12f, height - 42f, width - 24f, 28f), FitText(text, width - 24f), "inspector-status");
			SafeLabel(new Rect(width - 26f - 2f, height - 26f - 2f, 26f, 26f), "↘", "resize-grip-inspector");
		}

		private void OpenInspector(GameItem? item)
		{
			if (item == null)
			{
				_status = Ui("请先在游戏中右键选中一个物品。", "Right-click an item in the game first.");
				return;
			}
			_inspectedItem = item;
			_inspectorVisible = true;
			_inspectorSection = 0;
			_inspectorPage = 0;
			_inspectorTagKey = string.Empty;
			_inspectorFeatureIndex = -1;
			_inspectorConditionSection = 0;
			_tagListPage = 0;
			_featureListPage = 0;
			_inspectorStatus = Ui("可编辑字段点击右侧“应用”；标签和特性会直接作用于当前物品。", "Edit a field and click Apply; tag and feature changes affect the selected item immediately.");
			_editorInputValues.Clear();
			ClearInputFocusState();
			_clearInputFocus = true;
			_pageJumpMode = false;
		}

		private void CloseInspector()
		{
			_inspectorVisible = false;
			_inspectedItem = null;
			_inspectorTagKey = string.Empty;
			_inspectorFeatureIndex = -1;
			_inspectorConditionSection = 0;
			_inspectorPage = 0;
			_inspectorStatus = string.Empty;
			_editorInputValues.Clear();
			ClearInputFocusState();
			_clearInputFocus = true;
		}

		private void SetInspectorSection(int section)
		{
			_inspectorSection = Math.Clamp(section, 0, 2);
			_inspectorPage = 0;
			_inspectorTagKey = string.Empty;
			_inspectorFeatureIndex = -1;
			_inspectorConditionSection = 0;
			_editorInputValues.Clear();
			ClearInputFocusState();
			_clearInputFocus = true;
		}

		private void DrawBaseInspector(GameItem item, float width)
		{
			List<InspectorField> list = BuildBaseFields(item);
			int num = Math.Max(1, (list.Count + 12 - 1) / 12);
			_inspectorPage = Math.Clamp(_inspectorPage, 0, num - 1);
			int num2 = _inspectorPage * 12;
			int num3 = Math.Min(num2 + 12, list.Count);
			for (int i = num2; i < num3; i++)
			{
				DrawInspectorFieldRow(list[i], i - num2, width);
			}
			DrawInspectorNavigation(num, list.Count, width);
		}

		private List<InspectorField> BuildBaseFields(GameItem item)
		{
			return new List<InspectorField>
			{
				new InspectorField("identifier", Ui("物品 ID", "Identifier"), InspectorFieldKind.ReadOnly, SafeRead(() => item.identifier, string.Empty)),
				new InspectorField("name", Ui("显示名称", "Display name"), InspectorFieldKind.Text, SafeRead(() => item.name, string.Empty)),
				new InspectorField("shortDescription", Ui("简短描述", "Short description"), InspectorFieldKind.Text, SafeRead(() => item.shortDescription, string.Empty)),
				new InspectorField("longDescription", Ui("详细描述", "Long description"), InspectorFieldKind.Text, SafeRead(() => item.longDescription, string.Empty)),
				new InspectorField("flavorText", Ui("风味文本", "Flavor text"), InspectorFieldKind.Text, SafeRead(() => item.flavorText, string.Empty)),
				new InspectorField("customText", Ui("自定义文本", "Custom text"), InspectorFieldKind.Text, SafeRead(() => item.customText, string.Empty)),
				new InspectorField("unitCount", Ui("数量", "Unit count"), InspectorFieldKind.Int32, SafeRead(() => item.unitCount.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("unitBaseValue", Ui("基础价值", "Base value"), InspectorFieldKind.Int64, SafeRead(() => item.unitBaseValue.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("unitValue", Ui("当前价值", "Unit value"), InspectorFieldKind.Int64, SafeRead(() => item.unitValue.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("lateUnitValue", Ui("后期价值", "Late value"), InspectorFieldKind.Int64, SafeRead(() => item.lateUnitValue.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("backupUnitValue", Ui("备用价值", "Backup value"), InspectorFieldKind.Int64, SafeRead(() => item.backupUnitValue.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("bonusAccuracy", Ui("额外精度", "Bonus accuracy"), InspectorFieldKind.Int32, SafeRead(() => item.bonusAccuracy.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("activateDefault", Ui("默认可激活", "Activate by default"), InspectorFieldKind.Bool, SafeRead(() => (!item.activateDefault) ? "false" : "true", "false")),
				new InspectorField("forceDisableActivate", Ui("禁用激活", "Disable activate"), InspectorFieldKind.Bool, SafeRead(() => (!item.forceDisableActivate) ? "false" : "true", "false")),
				new InspectorField("forceDisableUse", Ui("禁用使用", "Disable use"), InspectorFieldKind.Bool, SafeRead(() => (!item.forceDisableUse) ? "false" : "true", "false")),
				new InspectorField("canUseOutsideCombat", Ui("战斗外可用", "Use outside combat"), InspectorFieldKind.Bool, SafeRead(() => (!item.canUseOutsideCombat) ? "false" : "true", "false")),
				new InspectorField("triggerOverwatch", Ui("触发监视", "Trigger overwatch"), InspectorFieldKind.Bool, SafeRead(() => (!item.triggerOverwatch) ? "false" : "true", "false")),
				new InspectorField("isCombatBackpack", Ui("战斗背包", "Combat backpack"), InspectorFieldKind.Bool, SafeRead(() => (!item.isCombatBackpack) ? "false" : "true", "false")),
				new InspectorField("isDebugMenu", Ui("调试物品", "Debug item"), InspectorFieldKind.Bool, SafeRead(() => (!item.isDebugMenu) ? "false" : "true", "false")),
				new InspectorField("modifiedXOrigin", Ui("修改后 X 原点", "Modified X origin"), InspectorFieldKind.Int32, SafeRead(() => item.modifiedXOrigin.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("modifiedYOrigin", Ui("修改后 Y 原点", "Modified Y origin"), InspectorFieldKind.Int32, SafeRead(() => item.modifiedYOrigin.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("spritePath", Ui("精灵路径", "Sprite path"), InspectorFieldKind.Text, SafeRead(() => item.spritePath, string.Empty)),
				new InspectorField("spriteAtlasPath", Ui("精灵图集路径", "Sprite atlas path"), InspectorFieldKind.Text, SafeRead(() => item.spriteAtlasPath, string.Empty)),
				new InspectorField("spriteChanged", Ui("精灵已改变", "Sprite changed"), InspectorFieldKind.Bool, SafeRead(() => (!item.spriteChanged) ? "false" : "true", "false")),
				new InspectorField("useSpriteFromMod", Ui("使用 Mod 精灵", "Use mod sprite"), InspectorFieldKind.ReadOnly, SafeRead(() => (!item.useSpriteFromMod) ? "false" : "true", "false")),
				new InspectorField("uniqueId", Ui("实例 ID", "Unique ID"), InspectorFieldKind.ReadOnly, SafeRead(() => item.uniqueId.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("itemTypes", Ui("物品类型", "Item types"), InspectorFieldKind.ReadOnly, ReadItemTypes(item)),
				new InspectorField("shape", Ui("形状", "Shape"), InspectorFieldKind.ReadOnly, DescribeShape(SafeRead(() => item.shape, null))),
				new InspectorField("modifiedShape", Ui("修改后形状", "Modified shape"), InspectorFieldKind.ReadOnly, DescribeShape(SafeRead(() => item.modifiedShape, null))),
				new InspectorField("parentInventory", Ui("所属库存", "Parent inventory"), InspectorFieldKind.ReadOnly, ReadParentInventory(item)),
				new InspectorField("parents", Ui("父物品", "Parents"), InspectorFieldKind.ReadOnly, DescribeObjectCollection(() => item.parents)),
				new InspectorField("children", Ui("子物品", "Children"), InspectorFieldKind.ReadOnly, DescribeObjectCollection(() => item.children)),
				new InspectorField("itemFeatures", Ui("特性集合", "Item features"), InspectorFieldKind.ReadOnly, DescribeObjectCollection(() => item.itemFeatures)),
				new InspectorField("effects", Ui("效果集合", "Effects"), InspectorFieldKind.ReadOnly, DescribeObjectCollection(() => item.effects)),
				new InspectorField("ability", Ui("能力", "Ability"), InspectorFieldKind.ReadOnly, DescribeObject(() => item.ability)),
				new InspectorField("recipeManager", Ui("配方管理器", "Recipe manager"), InspectorFieldKind.ReadOnly, DescribeObject(() => item.recipeManager)),
				new InspectorField("contentWindow", Ui("内容窗口", "Content window"), InspectorFieldKind.ReadOnly, DescribeObject(() => item.contentWindow)),
				new InspectorField("examineWindow", Ui("检查窗口", "Examine window"), InspectorFieldKind.ReadOnly, DescribeObject(() => item.examineWindow))
			};
		}

		private void DrawInspectorFieldRow(InspectorField field, int row, float width)
		{
			DrawInspectorFieldRowAt(field, row, width, 154f);
		}

		private void DrawInspectorFieldRowAt(InspectorField field, int row, float width, float top, string inputPrefix = "base:")
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0136: Unknown result type (might be due to invalid IL or missing references)
			//IL_017e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			float num = top + (float)row * 44f;
			SafeLabel(new Rect(12f, num + 3f, 224f, 34f), FitText(field.Label, 220f), "inspector-field-label");
			if (field.Kind == InspectorFieldKind.ReadOnly)
			{
				SafeLabel(new Rect(244f, num + 3f, width - 256f, 34f), FitText(field.Value, width - 264f), "inspector-field-value");
				return;
			}
			string text = inputPrefix + field.Key;
			EnsureEditorInput(text, field.Value);
			if (field.Kind == InspectorFieldKind.Bool)
			{
				SafeLabel(new Rect(244f, num + 3f, width - 350f, 34f), field.Value, "inspector-field-bool");
				SafeButton(new Rect(width - 96f, num + 3f, 82f, 34f), Ui("切换", "Toggle"), "inspector-field-toggle");
				return;
			}
			float num2 = Math.Max(260f, width - 356f);
			DrawCustomInputField(new Rect(244f, num + 3f, num2, 34f), GetEditorInput(text), EditorControlName(text), field.Label, "inspector-field-input");
			ApplyFocusRequest(EditorControlName(text));
			SafeButton(new Rect(width - 96f, num + 3f, 82f, 34f), Ui("应用", "Apply"), "inspector-field-apply");
		}

		private void DrawInspectorNavigation(int totalPages, int totalItems, float width)
		{
			DrawInspectorNavigationAt(totalPages, totalItems, width, 690f);
		}

		private void DrawInspectorNavigationAt(int totalPages, int totalItems, float width, float navY)
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_018b: Unknown result type (might be due to invalid IL or missing references)
			SafeButton(new Rect(12f, navY, 82f, 32f), Ui("首页", "Home"), "inspector-first");
			SafeButton(new Rect(102f, navY, 80f, 32f), Ui("上一页", "Prev"), "inspector-prev");
			SafeButton(new Rect(190f, navY, 80f, 32f), Ui("下一页", "Next"), "inspector-next");
			SafeButton(new Rect(278f, navY, 74f, 32f), Ui("尾页", "End"), "inspector-last");
			string text = (_englishUi ? ("Page " + (_inspectorPage + 1) + "/" + totalPages + ", " + totalItems + " fields") : ("第 " + (_inspectorPage + 1) + "/" + totalPages + " 页，共 " + totalItems + " 项"));
			SafeLabel(new Rect(364f, navY, width - 376f, 32f), FitText(text, width - 376f), "inspector-page");
		}

		private void DrawTagList(GameItem item, float width)
		{
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			DrawTagAddToolbar(width);
			List<TagEntry> list = ReadTagEntries(item);
			int num = Math.Max(1, (list.Count + 12 - 1) / 12);
			_inspectorPage = Math.Clamp(_inspectorPage, 0, num - 1);
			if (list.Count == 0)
			{
				SafeLabel(new Rect(18f, 198f, width - 30f, 34f), Ui("当前物品没有标签状态。", "This item has no tag states."), "tag-empty");
			}
			else
			{
				int num2 = _inspectorPage * 12;
				int num3 = Math.Min(num2 + 12, list.Count);
				for (int i = num2; i < num3; i++)
				{
					DrawTagSummaryRow(list[i], i - num2, width);
				}
			}
			DrawInspectorNavigationAt(num, list.Count, width, 734f);
		}

		private void DrawTagAddToolbar(float width)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Unknown result type (might be due to invalid IL or missing references)
			TagPreset tagPreset = TagPresets[Math.Clamp(_tagAddPresetIndex, 0, TagPresets.Length - 1)];
			SafeLabel(new Rect(12f, 132f, 82f, 30f), Ui("新增标签", "Add tag"), "tag-add-label");
			EnsureEditorInput("tag-add-key", tagPreset.Key);
			float num = Math.Max(260f, width - 660f);
			DrawCustomInputField(new Rect(98f, 132f, num, 30f), GetEditorInput("tag-add-key"), EditorControlName("tag-add-key"), Ui("标签 ID", "Tag ID"), "tag-add-input");
			ApplyFocusRequest(EditorControlName("tag-add-key"));
			float num2 = 106f + num;
			float num3 = Math.Max(210f, width - num2 - 280f);
			SafeButton(new Rect(num2, 132f, num3, 30f), FitText(Ui("预设：", "Preset: ") + tagPreset.Label, num3 - 8f), "tag-add-preset");
			SafeButton(new Rect(num2 + num3 + 8f, 132f, 94f, 30f), _tagAddModified ? Ui("修改态", "Modified") : Ui("基础态", "Base"), "tag-add-state");
			SafeButton(new Rect(width - 96f, 132f, 82f, 30f), Ui("添加", "Add"), "tag-add-button");
		}

		private void DrawTagSummaryRow(TagEntry entry, int row, float width)
		{
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_012b: Unknown result type (might be due to invalid IL or missing references)
			float num = 198f + (float)row * 44f;
			string text = (entry.Modified ? Ui("修改态", "modified") : Ui("基础态", "base"));
			string text2 = "[" + text + "] " + entry.Key;
			SafeLabel(new Rect(12f, num + 2f, width * 0.34f, 36f), FitText(text2, width * 0.34f), "tag-key");
			string text3 = DescribeTagState(entry.State);
			SafeLabel(new Rect(width * 0.35f, num + 2f, width - width * 0.35f - 200f, 36f), FitText(text3, width - width * 0.35f - 200f), "tag-value");
			SafeButton(new Rect(width - 180f, num + 3f, 76f, 34f), Ui("删除", "Delete"), "tag-delete");
			SafeButton(new Rect(width - 96f, num + 3f, 82f, 34f), Ui("编辑", "Edit"), "tag-edit");
		}

		private void DrawTagEditor(GameItem item, float width)
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			TagState val = FindTagState(item, _inspectorTagKey, _inspectorTagModified);
			if (val == null)
			{
				SafeLabel(new Rect(12f, 154f, width - 24f, 34f), Ui("标签已不存在。", "The tag no longer exists."), "tag-missing");
				return;
			}
			SafeLabel(new Rect(12f, 132f, width - 24f, 30f), FitText("[" + (_inspectorTagModified ? Ui("修改态", "modified") : Ui("基础态", "base")) + "] " + _inspectorTagKey, width - 24f), "tag-title");
			List<InspectorField> list = BuildTagFields(val);
			string inputPrefix = "tag:" + (_inspectorTagModified ? "modified:" : "base:") + _inspectorTagKey + ":";
			for (int i = 0; i < list.Count; i++)
			{
				DrawInspectorFieldRowAt(list[i], i, width, 154f, inputPrefix);
			}
		}

		private List<InspectorField> BuildTagFields(TagState? tag)
		{
			if (tag == null)
			{
				return new List<InspectorField>();
			}
			return new List<InspectorField>
			{
				new InspectorField("identifier", Ui("标签 ID", "Tag ID"), InspectorFieldKind.ReadOnly, SafeRead(() => tag.identifier, _inspectorTagKey)),
				new InspectorField("identifierName", Ui("标签名称", "Tag name"), InspectorFieldKind.Text, SafeRead(() => tag.identifierName, string.Empty)),
				new InspectorField("valueEnabled", Ui("启用状态", "Enabled"), InspectorFieldKind.Bool, SafeRead(() => (!tag.valueEnabled) ? "false" : "true", "false")),
				new InspectorField("valueString", Ui("字符串值", "String value"), InspectorFieldKind.Text, SafeRead(() => tag.valueString, string.Empty)),
				new InspectorField("valueInt", Ui("整数值", "Integer value"), InspectorFieldKind.Int32, SafeRead(() => tag.valueInt.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("valueLong", Ui("长整数值", "Long value"), InspectorFieldKind.Int64, SafeRead(() => tag.valueLong.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("valueFloat", Ui("浮点值", "Float value"), InspectorFieldKind.Text, SafeRead(() => tag.valueFloat.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("valueDouble", Ui("双精度值", "Double value"), InspectorFieldKind.Text, SafeRead(() => tag.valueDouble.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("valueBool", Ui("布尔值", "Boolean value"), InspectorFieldKind.Bool, SafeRead(() => (!tag.valueBool) ? "false" : "true", "false")),
				new InspectorField("internalValueString", Ui("内部值文本", "Internal value"), InspectorFieldKind.ReadOnly, SafeRead(() => tag.internalValueString, string.Empty))
			};
		}

		private void DrawFeatureList(GameItem item, float width)
		{
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			DrawFeatureAddToolbar(width);
			List<ItemFeature> list = ReadFeatures(item);
			int num = Math.Max(1, (list.Count + 12 - 1) / 12);
			_inspectorPage = Math.Clamp(_inspectorPage, 0, num - 1);
			if (list.Count == 0)
			{
				SafeLabel(new Rect(18f, 198f, width - 30f, 34f), Ui("当前物品没有 ItemFeature。", "This item has no ItemFeature entries."), "feature-empty");
			}
			else
			{
				int num2 = _inspectorPage * 12;
				int num3 = Math.Min(num2 + 12, list.Count);
				for (int i = num2; i < num3; i++)
				{
					DrawFeatureSummaryRow(list[i], i, i - num2, width);
				}
			}
			DrawInspectorNavigationAt(num, list.Count, width, 734f);
		}

		private void DrawFeatureAddToolbar(float width)
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
			FeaturePreset featurePreset = FeaturePresets[Math.Clamp(_featureAddPresetIndex, 0, FeaturePresets.Length - 1)];
			SafeLabel(new Rect(12f, 132f, 82f, 30f), Ui("新增特性", "Add feature"), "feature-add-label");
			float num = 300f;
			SafeButton(new Rect(98f, 132f, num, 30f), FitText(Ui("预设：", "Preset: ") + featurePreset.Label, num - 8f), "feature-add-preset");
			SafeButton(new Rect(406f, 132f, 94f, 30f), Ui("添加预设", "Add preset"), "feature-add-preset-button");
			SafeLabel(new Rect(510f, 132f, 76f, 30f), Ui("自定义 ID", "Custom ID"), "feature-add-custom-label");
			EnsureEditorInput("feature-add-id", string.Empty);
			float num2 = Math.Max(220f, width - 700f);
			DrawCustomInputField(new Rect(588f, 132f, num2, 30f), GetEditorInput("feature-add-id"), EditorControlName("feature-add-id"), Ui("特性 ID", "Feature ID"), "feature-add-input");
			ApplyFocusRequest(EditorControlName("feature-add-id"));
			SafeButton(new Rect(width - 96f, 132f, 82f, 30f), Ui("添加自定义", "Add custom"), "feature-add-custom-button");
		}

		private void DrawFeatureSummaryRow(ItemFeature feature, int featureIndex, int row, float width)
		{
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0151: Unknown result type (might be due to invalid IL or missing references)
			//IL_019d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
			float num = 198f + (float)row * 44f;
			string text = "[" + featureIndex.ToString(CultureInfo.InvariantCulture) + "] " + SafeRead(() => feature.category, string.Empty) + " / " + SafeRead(() => feature.identifier, string.Empty);
			SafeLabel(new Rect(12f, num + 2f, width * 0.43f, 36f), FitText(text, width * 0.43f), "feature-key");
			string text2 = SafeRead(() => feature.actualDisplay, string.Empty);
			if (string.IsNullOrWhiteSpace(text2))
			{
				text2 = SafeRead(() => feature.publicDisplay, string.Empty);
			}
			string text3 = "stage=" + SafeRead(() =>
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				return ((object)feature.valueStage/*cast due to constrained. prefix*/).ToString();
			}, string.Empty) + ", exposed=" + SafeRead(() => (!feature.isFeatureExposed) ? "false" : "true", "false");
			SafeLabel(new Rect(width * 0.44f, num + 2f, width - width * 0.44f - 200f, 36f), FitText(text2 + "  " + text3, width - width * 0.44f - 200f), "feature-value");
			SafeButton(new Rect(width - 180f, num + 3f, 76f, 34f), Ui("删除", "Delete"), "feature-delete");
			SafeButton(new Rect(width - 96f, num + 3f, 82f, 34f), Ui("编辑", "Edit"), "feature-edit");
		}

		private void DrawFeatureEditor(GameItem item, float width)
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			List<ItemFeature> list = ReadFeatures(item);
			if (_inspectorFeatureIndex < 0 || _inspectorFeatureIndex >= list.Count)
			{
				SafeLabel(new Rect(12f, 154f, width - 24f, 34f), Ui("特性已不存在。", "The feature no longer exists."), "feature-missing");
				return;
			}
			ItemFeature feature = list[_inspectorFeatureIndex];
			SafeLabel(new Rect(12f, 132f, width - 24f, 30f), FitText("[" + _inspectorFeatureIndex.ToString(CultureInfo.InvariantCulture) + "] " + SafeRead(() => feature.identifier, string.Empty), width - 24f), "feature-title");
			SafeButton(new Rect(12f, 164f, 86f, 32f), Ui("常规", "General"), "feature-tab-general");
			SafeButton(new Rect(106f, 164f, 86f, 32f), Ui("假条件", "Fake"), "feature-tab-fake");
			SafeButton(new Rect(200f, 164f, 86f, 32f), Ui("真条件", "Real"), "feature-tab-real");
			List<InspectorField> list2;
			if (_inspectorConditionSection == 1)
			{
				list2 = BuildConditionFields(feature.fakeCondition, "fake");
			}
			else
			{
				list2 = ((_inspectorConditionSection == 2) ? BuildConditionFields(feature.realCondition, "real") : BuildFeatureFields(feature));
			}
			float num = 204f;
			int num2 = Math.Max(1, (list2.Count + 12 - 1) / 12);
			_inspectorPage = Math.Clamp(_inspectorPage, 0, num2 - 1);
			int num3 = _inspectorPage * 12;
			int num4 = Math.Min(num3 + 12, list2.Count);
			string text;
			if (_inspectorConditionSection == 1)
			{
				text = "condition:fake:";
			}
			else
			{
				text = ((_inspectorConditionSection == 2) ? "condition:real:" : "feature:");
			}
			string inputPrefix = text + _inspectorFeatureIndex.ToString(CultureInfo.InvariantCulture) + ":";
			for (int num5 = num3; num5 < num4; num5++)
			{
				DrawInspectorFieldRowAt(list2[num5], num5 - num3, width, num, inputPrefix);
			}
			DrawInspectorNavigationAt(num2, list2.Count, width, num + 528f + 8f);
		}

		private List<InspectorField> BuildFeatureFields(ItemFeature feature)
		{
			return new List<InspectorField>
			{
				new InspectorField("identifier", Ui("特性 ID", "Feature ID"), InspectorFieldKind.Text, SafeRead(() => feature.identifier, string.Empty)),
				new InspectorField("category", Ui("类别", "Category"), InspectorFieldKind.Text, SafeRead(() => feature.category, string.Empty)),
				new InspectorField("featureType", Ui("特性类型", "Feature type"), InspectorFieldKind.ReadOnly, SafeRead(() =>
				{
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_000b: Unknown result type (might be due to invalid IL or missing references)
					return ((object)feature.featureType/*cast due to constrained. prefix*/).ToString();
				}, string.Empty)),
				new InspectorField("valueStage", Ui("值阶段", "Value stage"), InspectorFieldKind.ReadOnly, SafeRead(() =>
				{
					//IL_0006: Unknown result type (might be due to invalid IL or missing references)
					//IL_000b: Unknown result type (might be due to invalid IL or missing references)
					return ((object)feature.valueStage/*cast due to constrained. prefix*/).ToString();
				}, string.Empty)),
				new InspectorField("actualDisplay", Ui("实际显示", "Actual display"), InspectorFieldKind.Text, SafeRead(() => feature.actualDisplay, string.Empty)),
				new InspectorField("publicDisplay", Ui("公开显示", "Public display"), InspectorFieldKind.Text, SafeRead(() => feature.publicDisplay, string.Empty)),
				new InspectorField("removedByTool", Ui("移除工具", "Removed by tool"), InspectorFieldKind.Text, SafeRead(() => feature.removedByTool, string.Empty)),
				new InspectorField("NEGO_TOOL_SCALE", Ui("协商倍率", "Negotiation scale"), InspectorFieldKind.ReadOnly, SafeRead(() => ItemFeature.NEGO_TOOL_SCALE, string.Empty)),
				new InspectorField("customIntValue1", Ui("自定义整数", "Custom integer"), InspectorFieldKind.Int32, SafeRead(() => feature.customIntValue1.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("parentItemUniqueId", Ui("父物品实例 ID", "Parent item ID"), InspectorFieldKind.Int32, SafeRead(() => feature.parentItemUniqueId.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("preExposeValueModifier", Ui("预公开修正", "Pre-expose modifier"), InspectorFieldKind.Int32, SafeRead(() => feature.preExposeValueModifier.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("valueModifier", Ui("价值修正", "Value modifier"), InspectorFieldKind.Int32, SafeRead(() => feature.valueModifier.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("initiallyShown", Ui("初始显示", "Initially shown"), InspectorFieldKind.Bool, SafeRead(() => (!feature.initiallyShown) ? "false" : "true", "false")),
				new InspectorField("isDisabled", Ui("已禁用", "Disabled"), InspectorFieldKind.Bool, SafeRead(() => (!feature.isDisabled) ? "false" : "true", "false")),
				new InspectorField("isExposable", Ui("可揭示", "Exposable"), InspectorFieldKind.Bool, SafeRead(() => (!feature.isExposable) ? "false" : "true", "false")),
				new InspectorField("isExposing", Ui("揭示中", "Exposing"), InspectorFieldKind.Bool, SafeRead(() => (!feature.isExposing) ? "false" : "true", "false")),
				new InspectorField("isFeatureDiscovered", Ui("已发现", "Discovered"), InspectorFieldKind.Bool, SafeRead(() => (!feature.isFeatureDiscovered) ? "false" : "true", "false")),
				new InspectorField("isFeatureExposed", Ui("已揭示", "Exposed"), InspectorFieldKind.Bool, SafeRead(() => (!feature.isFeatureExposed) ? "false" : "true", "false")),
				new InspectorField("isFeatureMatch", Ui("特性匹配", "Feature match"), InspectorFieldKind.Bool, SafeRead(() => (!feature.isFeatureMatch) ? "false" : "true", "false")),
				new InspectorField("isPublicHidden", Ui("公开隐藏", "Public hidden"), InspectorFieldKind.Bool, SafeRead(() => (!feature.isPublicHidden) ? "false" : "true", "false")),
				new InspectorField("useCondition", Ui("使用条件", "Use condition"), InspectorFieldKind.Bool, SafeRead(() => (!feature.useCondition) ? "false" : "true", "false")),
				new InspectorField("usePreExposeValue", Ui("使用预公开值", "Use pre-expose value"), InspectorFieldKind.Bool, SafeRead(() => (!feature.usePreExposeValue) ? "false" : "true", "false"))
			};
		}

		private List<InspectorField> BuildConditionFields(ItemCondition? condition, string source)
		{
			if (condition == null)
			{
				return new List<InspectorField>
				{
					new InspectorField("missing-" + source, Ui("条件", "Condition"), InspectorFieldKind.ReadOnly, Ui("未设置（游戏未创建此条件）", "Not set (the game did not create this condition)"))
				};
			}
			return new List<InspectorField>
			{
				new InspectorField("identifier", Ui("条件 ID", "Condition ID"), InspectorFieldKind.Text, SafeRead(() => condition.identifier, string.Empty)),
				new InspectorField("category", Ui("类别", "Category"), InspectorFieldKind.Text, SafeRead(() => condition.category, string.Empty)),
				new InspectorField("display", Ui("显示文本", "Display"), InspectorFieldKind.Text, SafeRead(() => condition.display, string.Empty)),
				new InspectorField("compareValue", Ui("比较值", "Compare value"), InspectorFieldKind.Int32, SafeRead(() => condition.compareValue.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("customValue", Ui("自定义值", "Custom value"), InspectorFieldKind.Int32, SafeRead(() => condition.customValue.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("modValue", Ui("修正值", "Modifier value"), InspectorFieldKind.Int32, SafeRead(() => condition.modValue.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("modValueDisplay", Ui("显示修正值", "Display modifier"), InspectorFieldKind.Int32, SafeRead(() => condition.modValueDisplay.ToString(CultureInfo.InvariantCulture), "0")),
				new InspectorField("hiddenAsPublic", Ui("公开时隐藏", "Hidden as public"), InspectorFieldKind.Bool, SafeRead(() => (!condition.hiddenAsPublic) ? "false" : "true", "false")),
				new InspectorField("isTransformative", Ui("转换条件", "Transformative"), InspectorFieldKind.Bool, SafeRead(() => (!condition.isTransformative) ? "false" : "true", "false")),
				new InspectorField("onActivateActionId", Ui("激活动作 ID", "Activate action ID"), InspectorFieldKind.Text, SafeRead(() => condition.onActivateActionId, string.Empty))
			};
		}

		private string GetInspectorInputControlAt(float localX, float localY)
		{
			GameItem inspectedItem = _inspectedItem;
			if (!_inspectorVisible || inspectedItem == null)
			{
				return string.Empty;
			}
			if (_inspectorTagKey.Length == 0 && _inspectorSection == 1 && Hit(localX, localY, 98f, 132f, Math.Max(260f, _windowRect.width - 660f), 30f))
			{
				return EditorControlName("tag-add-key");
			}
			if (_inspectorFeatureIndex < 0 && _inspectorSection == 2 && Hit(localX, localY, 588f, 132f, Math.Max(220f, _windowRect.width - 700f), 30f))
			{
				return EditorControlName("feature-add-id");
			}
			float num = 154f;
			List<InspectorField> list;
			string text;
			if (_inspectorTagKey.Length > 0)
			{
				list = BuildTagFields(FindTagState(inspectedItem, _inspectorTagKey, _inspectorTagModified));
				text = "tag:" + (_inspectorTagModified ? "modified:" : "base:") + _inspectorTagKey + ":";
			}
			else if (_inspectorFeatureIndex >= 0)
			{
				List<ItemFeature> list2 = ReadFeatures(inspectedItem);
				if (_inspectorFeatureIndex >= list2.Count)
				{
					return string.Empty;
				}
				ItemFeature val = list2[_inspectorFeatureIndex];
				if (_inspectorConditionSection == 1)
				{
					list = BuildConditionFields(val.fakeCondition, "fake");
				}
				else
				{
					list = ((_inspectorConditionSection == 2) ? BuildConditionFields(val.realCondition, "real") : BuildFeatureFields(val));
				}
				num = 204f;
				string text2;
				if (_inspectorConditionSection == 1)
				{
					text2 = "condition:fake:";
				}
				else
				{
					text2 = ((_inspectorConditionSection == 2) ? "condition:real:" : "feature:");
				}
				text = text2 + _inspectorFeatureIndex.ToString(CultureInfo.InvariantCulture) + ":";
			}
			else
			{
				if (_inspectorSection != 0)
				{
					return string.Empty;
				}
				list = BuildBaseFields(inspectedItem);
				text = "base:";
			}
			int num2 = _inspectorPage * 12;
			if (num2 < 0 || num2 >= list.Count)
			{
				return string.Empty;
			}
			int num3 = (int)Math.Floor((localY - num) / 44f);
			if (num3 < 0 || num3 >= 12 || num2 + num3 >= list.Count)
			{
				return string.Empty;
			}
			InspectorField inspectorField = list[num2 + num3];
			InspectorFieldKind kind = inspectorField.Kind;
			if (kind == InspectorFieldKind.ReadOnly || kind == InspectorFieldKind.Bool)
			{
				return string.Empty;
			}
			float width = Math.Max(260f, _windowRect.width - 356f);
			float top = num + (float)num3 * 44f + 3f;
			if (!Hit(localX, localY, 244f, top, width, 34f))
			{
				return string.Empty;
			}
			return EditorControlName(text + inspectorField.Key);
		}

		private void ProcessInspectorMouseClick(float localX, float localY)
		{
			float width = _windowRect.width;
			if (_inspectedItem == null)
			{
				return;
			}
			if (_inspectorTagKey.Length == 0 && _inspectorFeatureIndex < 0)
			{
				if (Hit(localX, localY, 12f, 96f, 92f, 32f))
				{
					SetInspectorSection(0);
					return;
				}
				if (Hit(localX, localY, 112f, 96f, 92f, 32f))
				{
					SetInspectorSection(1);
					return;
				}
				if (Hit(localX, localY, 212f, 96f, 112f, 32f))
				{
					SetInspectorSection(2);
					return;
				}
				if (Hit(localX, localY, 332f, 96f, 94f, 32f))
				{
					_editorInputValues.Clear();
					_inspectorStatus = Ui("已刷新当前物品属性。", "Item attributes refreshed.");
					return;
				}
			}
			if (_inspectorTagKey.Length > 0)
			{
				if (Hit(localX, localY, 12f, 96f, 150f, 32f))
				{
					_inspectorTagKey = string.Empty;
					_inspectorPage = Math.Max(0, _tagListPage);
					_editorInputValues.Clear();
					ClearInputFocusState();
					_clearInputFocus = true;
				}
				else
				{
					List<InspectorField> fields = BuildTagFields(FindTagState(_inspectedItem, _inspectorTagKey, _inspectorTagModified));
					ProcessInspectorFieldClick(fields, 154f, localX, localY, width, (InspectorField field) =>
					{
						ApplyTagField(_inspectedItem, field);
					});
				}
			}
			else if (_inspectorSection == 1)
			{
				float num = Math.Max(260f, width - 660f);
				float num2 = 106f + num;
				float num3 = Math.Max(210f, width - num2 - 280f);
				if (Hit(localX, localY, num2, 132f, num3, 30f))
				{
					_tagAddPresetIndex = (_tagAddPresetIndex + 1) % TagPresets.Length;
					SetInputValue("item-editor:tag-add-key", TagPresets[_tagAddPresetIndex].Key);
					ClearInputFocusState();
					_clearInputFocus = true;
					return;
				}
				if (Hit(localX, localY, num2 + num3 + 8f, 132f, 94f, 30f))
				{
					_tagAddModified = !_tagAddModified;
					return;
				}
				if (Hit(localX, localY, width - 96f, 132f, 82f, 30f))
				{
					AddTagToItem(_inspectedItem);
					return;
				}
				List<TagEntry> list = ReadTagEntries(_inspectedItem);
				int num4 = _inspectorPage * 12;
				int num5 = Math.Min(num4 + 12, list.Count);
				if (localY >= 198f && localY < 726f)
				{
					int num6 = (int)Math.Floor((localY - 198f) / 44f);
					if (num6 >= 0 && num4 + num6 < num5)
					{
						TagEntry tagEntry = list[num4 + num6];
						if (Hit(localX, localY, width - 180f, 198f + (float)num6 * 44f, 76f, 34f))
						{
							RemoveTagFromItem(_inspectedItem, tagEntry.Key, tagEntry.Modified);
							return;
						}
						if (Hit(localX, localY, width - 96f, 198f + (float)num6 * 44f, 82f, 34f))
						{
							_inspectorTagKey = tagEntry.Key;
							_inspectorTagModified = tagEntry.Modified;
							_tagListPage = _inspectorPage;
							_inspectorPage = 0;
							_editorInputValues.Clear();
							ClearInputFocusState();
							_clearInputFocus = true;
							return;
						}
					}
				}
				ProcessInspectorNavigation(Math.Max(1, (list.Count + 12 - 1) / 12), 734f, localX, localY);
			}
			else if (_inspectorFeatureIndex >= 0)
			{
				if (Hit(localX, localY, 12f, 96f, 180f, 32f))
				{
					_inspectorFeatureIndex = -1;
					_inspectorConditionSection = 0;
					_inspectorPage = Math.Max(0, _featureListPage);
					_editorInputValues.Clear();
					ClearInputFocusState();
					_clearInputFocus = true;
					return;
				}
				if (Hit(localX, localY, 12f, 164f, 86f, 32f))
				{
					_inspectorConditionSection = 0;
					_inspectorPage = 0;
					_editorInputValues.Clear();
					return;
				}
				if (Hit(localX, localY, 106f, 164f, 86f, 32f))
				{
					_inspectorConditionSection = 1;
					_inspectorPage = 0;
					_editorInputValues.Clear();
					return;
				}
				if (Hit(localX, localY, 200f, 164f, 86f, 32f))
				{
					_inspectorConditionSection = 2;
					_inspectorPage = 0;
					_editorInputValues.Clear();
					return;
				}
				List<ItemFeature> list2 = ReadFeatures(_inspectedItem);
				if (_inspectorFeatureIndex >= 0 && _inspectorFeatureIndex < list2.Count)
				{
					ItemFeature feature = list2[_inspectorFeatureIndex];
					List<InspectorField> list3;
					if (_inspectorConditionSection == 1)
					{
						list3 = BuildConditionFields(feature.fakeCondition, "fake");
					}
					else
					{
						list3 = ((_inspectorConditionSection == 2) ? BuildConditionFields(feature.realCondition, "real") : BuildFeatureFields(feature));
					}
					float num7 = 204f;
					int num8 = Math.Max(1, (list3.Count + 12 - 1) / 12);
					_inspectorPage = Math.Clamp(_inspectorPage, 0, num8 - 1);
					int num9 = _inspectorPage * 12;
					int num10 = Math.Min(num9 + 12, list3.Count);
					List<InspectorField> fields2 = list3.Skip(num9).Take(num10 - num9).ToList();
					ProcessInspectorFieldClick(fields2, num7, localX, localY, width, (InspectorField field) =>
					{
						ApplyFeatureField(_inspectedItem, feature, field, _inspectorConditionSection);
					});
					float navY = num7 + 528f + 8f;
					ProcessInspectorNavigation(num8, navY, localX, localY);
				}
			}
			else if (_inspectorSection == 2)
			{
				FeaturePreset preset = FeaturePresets[Math.Clamp(_featureAddPresetIndex, 0, FeaturePresets.Length - 1)];
				if (Hit(localX, localY, 98f, 132f, 300f, 30f))
				{
					_featureAddPresetIndex = (_featureAddPresetIndex + 1) % FeaturePresets.Length;
					ClearInputFocusState();
					_clearInputFocus = true;
					return;
				}
				if (Hit(localX, localY, 406f, 132f, 94f, 30f))
				{
					AddPresetFeature(_inspectedItem, preset);
					return;
				}
				if (Hit(localX, localY, width - 96f, 132f, 82f, 30f))
				{
					AddCustomFeature(_inspectedItem);
					return;
				}
				List<ItemFeature> list4 = ReadFeatures(_inspectedItem);
				int num11 = _inspectorPage * 12;
				int num12 = Math.Min(num11 + 12, list4.Count);
				if (localY >= 198f && localY < 726f)
				{
					int num13 = (int)Math.Floor((localY - 198f) / 44f);
					if (num13 >= 0 && num11 + num13 < num12)
					{
						int num14 = num11 + num13;
						if (Hit(localX, localY, width - 180f, 198f + (float)num13 * 44f, 76f, 34f))
						{
							RemoveFeatureFromItem(_inspectedItem, num14);
							return;
						}
						if (Hit(localX, localY, width - 96f, 198f + (float)num13 * 44f, 82f, 34f))
						{
							_featureListPage = _inspectorPage;
							_inspectorFeatureIndex = num14;
							_inspectorConditionSection = 0;
							_inspectorPage = 0;
							_editorInputValues.Clear();
							ClearInputFocusState();
							_clearInputFocus = true;
							return;
						}
					}
				}
				ProcessInspectorNavigation(Math.Max(1, (list4.Count + 12 - 1) / 12), 734f, localX, localY);
			}
			else if (_inspectorSection == 0)
			{
				List<InspectorField> list5 = BuildBaseFields(_inspectedItem);
				ProcessInspectorFieldClick(list5.Skip(_inspectorPage * 12).Take(12).ToList(), 154f, localX, localY, width, (InspectorField field) =>
				{
					ApplyBaseField(_inspectedItem, field);
				});
				int totalPages = Math.Max(1, (list5.Count + 12 - 1) / 12);
				ProcessInspectorNavigation(totalPages, 690f, localX, localY);
			}
		}

		private void ProcessInspectorFieldClick(List<InspectorField> fields, float top, float localX, float localY, float width, Action<InspectorField> apply)
		{
			if (localY < top || localY >= top + (float)fields.Count * 44f)
			{
				return;
			}
			int num = (int)Math.Floor((localY - top) / 44f);
			if (num < 0 || num >= fields.Count)
			{
				return;
			}
			InspectorField inspectorField = fields[num];
			if (inspectorField.Kind == InspectorFieldKind.ReadOnly)
			{
				return;
			}
			if (inspectorField.Kind == InspectorFieldKind.Bool)
			{
				if (Hit(localX, localY, width - 96f, top + (float)num * 44f, 82f, 34f))
				{
					apply(inspectorField);
				}
			}
			else if (Hit(localX, localY, width - 96f, top + (float)num * 44f, 82f, 34f))
			{
				apply(inspectorField);
			}
		}

		private void ProcessInspectorNavigation(int totalPages, float navY, float localX, float localY)
		{
			if (Hit(localX, localY, 12f, navY, 82f, 32f))
			{
				_inspectorPage = 0;
			}
			else if (Hit(localX, localY, 102f, navY, 80f, 32f))
			{
				_inspectorPage = Math.Max(0, _inspectorPage - 1);
			}
			else if (Hit(localX, localY, 190f, navY, 80f, 32f))
			{
				_inspectorPage = Math.Min(totalPages - 1, _inspectorPage + 1);
			}
			else if (Hit(localX, localY, 278f, navY, 74f, 32f))
			{
				_inspectorPage = totalPages - 1;
			}
		}

		private static List<TagEntry> ReadTagEntries(GameItem item)
		{
			List<TagEntry> list = new List<TagEntry>();
			try
			{
				AppendTagEntries(list, item.state, modified: false);
			}
			catch
			{
			}
			try
			{
				AppendTagEntries(list, item.modifiedState, modified: true);
			}
			catch
			{
			}
			list.Sort((TagEntry left, TagEntry right) =>
			{
				int num = left.Modified.CompareTo(right.Modified);
				return (num == 0) ? string.Compare(left.Key, right.Key, StringComparison.OrdinalIgnoreCase) : num;
			});
			return list;
		}

		private static void AppendTagEntries(List<TagEntry> entries, TagSystem? system, bool modified)
		{
			if (system == null || system.dict == null)
			{
				return;
			}
			KeyCollection<string, TagState> keys = system.dict.Keys;
			if (keys == null)
			{
				return;
			}
			Enumerator<string, TagState> enumerator = keys.GetEnumerator();
			while (enumerator.MoveNext())
			{
				string current = enumerator.Current;
				if (!string.IsNullOrWhiteSpace(current))
				{
					TagState val = null;
					try
					{
						val = system.dict[current];
					}
					catch
					{
					}
					if (val != null)
					{
						entries.Add(new TagEntry(current, val, modified));
					}
				}
			}
		}

		private static TagState? FindTagState(GameItem item, string key, bool modified)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return null;
			}
			try
			{
				TagSystem val = (modified ? item.modifiedState : item.state);
				return (val != null) ? val.GetTag(key) : null;
			}
			catch
			{
				return null;
			}
		}

		private static string DescribeTagState(TagState state)
		{
			string text = SafeRead(() => (!state.valueEnabled) ? "off" : "on", "?");
			string text2 = SafeRead(() => state.identifierName, string.Empty);
			string text3 = SafeRead(() => state.internalValueString, string.Empty);
			string text4 = (string.IsNullOrWhiteSpace(text3) ? SafeRead(() => state.valueString, string.Empty) : text3);
			if (string.IsNullOrWhiteSpace(text4))
			{
				text4 = SafeRead(() => state.valueInt.ToString(CultureInfo.InvariantCulture), "0");
			}
			return text + "  " + text2 + "  " + text4;
		}

		private static List<ItemFeature> ReadFeatures(GameItem item)
		{
			List<ItemFeature> list = new List<ItemFeature>();
			try
			{
				List<ItemFeature> itemFeatures = item.itemFeatures;
				if (itemFeatures == null)
				{
					return list;
				}
				for (int i = 0; i < itemFeatures.Count; i++)
				{
					ItemFeature val = itemFeatures[i];
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

		private void AddTagToItem(GameItem item)
		{
			string text = GetEditorInput("tag-add-key").Trim();
			if (text.Length == 0)
			{
				_inspectorStatus = Ui("请输入标签 ID。", "Enter a tag ID.");
				return;
			}
			try
			{
				TagSystem val = (_tagAddModified ? item.modifiedState : item.state);
				if (val == null)
				{
					_inspectorStatus = Ui("目标物品没有可用的标签系统。", "The selected item has no usable tag system.");
					return;
				}
				if (val.GetTag(text) != null)
				{
					_inspectorStatus = Ui("标签已存在：", "Tag already exists: ") + text;
					return;
				}
				if (!val.InitTagString(text) && val.GetTag(text) == null)
				{
					_inspectorStatus = Ui("无法创建标签：", "Could not create tag: ") + text;
					return;
				}
				TagState tag = val.GetTag(text);
				if (tag == null)
				{
					_inspectorStatus = Ui("标签创建后无法读取：", "The created tag could not be read: ") + text;
					return;
				}
				TagPreset tagPreset = TagPresets[Math.Clamp(_tagAddPresetIndex, 0, TagPresets.Length - 1)];
				string text2 = (string.Equals(text, tagPreset.Key, StringComparison.Ordinal) ? tagPreset.Label.Split(" / ")[0] : text);
				tag.UpdateReadableName(text2);
				tag.SetEnabled(true);
				ValidateItemAfterEdit(item);
				_inspectorPage = 0;
				_inspectorStatus = Ui("已添加标签：", "Tag added: ") + text;
				RefreshInspectedItem(item);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("添加标签失败：", "Tag add failed: ") + ex.Message;
			}
		}

		private void RemoveTagFromItem(GameItem item, string key, bool modified)
		{
			if (string.IsNullOrWhiteSpace(key))
			{
				return;
			}
			try
			{
				TagSystem val = (modified ? item.modifiedState : item.state);
				Dictionary<string, TagState> val2 = ((val != null) ? val.dict : null);
				if (val2 == null || !val2.Remove(key))
				{
					_inspectorStatus = Ui("未找到标签：", "Tag not found: ") + key;
					return;
				}
				ValidateItemAfterEdit(item);
				_inspectorPage = 0;
				_inspectorStatus = Ui("已删除标签：", "Tag deleted: ") + key;
				RefreshInspectedItem(item);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("删除标签失败：", "Tag delete failed: ") + ex.Message;
			}
		}

		private void AddPresetFeature(GameItem item, FeaturePreset preset)
		{
			try
			{
				ItemFeature feature = preset.Factory();
				AddFeatureObject(item, feature, preset.Label);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("添加特性失败：", "Feature add failed: ") + ex.Message;
			}
		}

		private void AddCustomFeature(GameItem item)
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Expected Obj, but got Unknown
			string text = GetEditorInput("feature-add-id").Trim();
			if (text.Length == 0)
			{
				_inspectorStatus = Ui("请输入特性 ID。", "Enter a feature ID.");
				return;
			}
			try
			{
				AddFeatureObject(item, new ItemFeature(text), text);
				SetInputValue("item-editor:feature-add-id", string.Empty);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("添加特性失败：", "Feature add failed: ") + ex.Message;
			}
		}

		private void AddFeatureObject(GameItem item, ItemFeature? feature, string displayName)
		{
			if (feature == null)
			{
				_inspectorStatus = Ui("游戏没有返回特性对象。", "The game returned no feature object.");
				return;
			}
			string text = SafeRead(() => feature.identifier, string.Empty);
			if (!string.IsNullOrWhiteSpace(text) && item.FindItemFeatureByID(text) != null)
			{
				_inspectorStatus = Ui("特性已存在：", "Feature already exists: ") + text;
				return;
			}
			try
			{
				feature.parentItemUniqueId = item.uniqueId;
			}
			catch
			{
			}
			item.AddItemFeature(feature);
			ValidateItemAfterEdit(item);
			_inspectorPage = 0;
			_inspectorStatus = Ui("已添加特性：", "Feature added: ") + displayName;
			RefreshInspectedItem(item);
		}

		private void RemoveFeatureFromItem(GameItem item, int featureIndex)
		{
			List<ItemFeature> list = ReadFeatures(item);
			if (featureIndex < 0 || featureIndex >= list.Count)
			{
				return;
			}
			ItemFeature feature = list[featureIndex];
			string text = SafeRead(() => feature.identifier, string.Empty);
			try
			{
				if (!string.IsNullOrWhiteSpace(text))
				{
					item.RemoveItemFeatureByID(text);
				}
				else
				{
					item.itemFeatures?.RemoveAt(featureIndex);
				}
				ValidateItemAfterEdit(item);
				_inspectorPage = 0;
				_inspectorStatus = Ui("已删除特性：", "Feature deleted: ") + (string.IsNullOrWhiteSpace(text) ? featureIndex.ToString(CultureInfo.InvariantCulture) : text);
				RefreshInspectedItem(item);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("删除特性失败：", "Feature delete failed: ") + ex.Message;
			}
		}

		private void ApplyBaseField(GameItem item, InspectorField field)
		{
			try
			{
				if (field.Kind == InspectorFieldKind.Bool)
				{
					ToggleBaseBool(item, field.Key);
					_inspectorStatus = Ui("已切换：", "Toggled: ") + field.Label;
					RefreshInspectedItem(item);
					return;
				}
				string editorInput = GetEditorInput("base:" + field.Key);
				switch (field.Key)
				{
				case "name":
					item.SetName(editorInput);
					break;
				case "shortDescription":
					item.shortDescription = editorInput;
					break;
				case "longDescription":
					item.longDescription = editorInput;
					break;
				case "flavorText":
					item.flavorText = editorInput;
					break;
				case "customText":
					item.customText = editorInput;
					break;
				case "unitCount":
				{
					if (!int.TryParse(editorInput, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result) || result < 0 || result > 999999)
					{
						throw new FormatException("unit count must be an integer from 0 to 999999");
					}
					item.SetUnitCount(result);
					break;
				}
				case "unitBaseValue":
					item.unitBaseValue = ParseInt64(editorInput);
					break;
				case "unitValue":
					item.unitValue = ParseInt64(editorInput);
					break;
				case "lateUnitValue":
					item.lateUnitValue = ParseInt64(editorInput);
					break;
				case "backupUnitValue":
					item.backupUnitValue = ParseInt64(editorInput);
					break;
				case "bonusAccuracy":
					item.bonusAccuracy = ParseInt32(editorInput);
					break;
				case "modifiedXOrigin":
					item.modifiedXOrigin = ParseInt32(editorInput);
					break;
				case "modifiedYOrigin":
					item.modifiedYOrigin = ParseInt32(editorInput);
					break;
				case "spritePath":
					item.spritePath = editorInput;
					break;
				case "spriteAtlasPath":
					item.spriteAtlasPath = editorInput;
					break;
				default:
					_inspectorStatus = Ui("该字段为只读字段。", "This field is read-only.");
					return;
				}
				ValidateItemAfterEdit(item);
				_inspectorStatus = Ui("已应用：", "Applied: ") + field.Label;
				RefreshInspectedItem(item);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("应用失败：", "Apply failed: ") + ex.Message;
			}
		}

		private static void ToggleBaseBool(GameItem item, string key)
		{
			switch (key)
			{
			case "activateDefault":
				item.activateDefault = !item.activateDefault;
				break;
			case "forceDisableActivate":
				item.forceDisableActivate = !item.forceDisableActivate;
				break;
			case "forceDisableUse":
				item.forceDisableUse = !item.forceDisableUse;
				break;
			case "canUseOutsideCombat":
				item.canUseOutsideCombat = !item.canUseOutsideCombat;
				break;
			case "triggerOverwatch":
				item.triggerOverwatch = !item.triggerOverwatch;
				break;
			case "isCombatBackpack":
				item.isCombatBackpack = !item.isCombatBackpack;
				break;
			case "isDebugMenu":
				item.isDebugMenu = !item.isDebugMenu;
				break;
			case "spriteChanged":
				item.spriteChanged = !item.spriteChanged;
				break;
			default:
				throw new InvalidOperationException("unsupported boolean field");
			}
		}

		private void ApplyTagField(GameItem item, InspectorField field)
		{
			TagState val = FindTagState(item, _inspectorTagKey, _inspectorTagModified);
			if (val == null)
			{
				_inspectorStatus = Ui("标签已不存在。", "The tag no longer exists.");
				return;
			}
			try
			{
				if (field.Key == "valueEnabled")
				{
					val.SetEnabled(!val.valueEnabled);
				}
				else
				{
					string editorInput = GetEditorInput("tag:" + (_inspectorTagModified ? "modified:" : "base:") + _inspectorTagKey + ":" + field.Key);
					switch (field.Key)
					{
					default:
						return;
					case "identifierName":
						val.UpdateReadableName(editorInput);
						break;
					case "valueString":
						val.SetString(editorInput);
						break;
					case "valueInt":
						val.SetInt(ParseInt32(editorInput));
						break;
					case "valueLong":
						val.valueLong = ParseInt64(editorInput);
						break;
					case "valueFloat":
						val.SetFloat(ParseFloat(editorInput));
						break;
					case "valueDouble":
						val.SetDouble(ParseDouble(editorInput));
						break;
					case "valueBool":
						val.SetBool(ParseBool(editorInput));
						break;
					}
				}
				ValidateItemAfterEdit(item);
				_inspectorStatus = Ui("已应用标签：", "Tag applied: ") + _inspectorTagKey;
				RefreshInspectedItem(item);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("应用标签失败：", "Tag apply failed: ") + ex.Message;
			}
		}

		private void ApplyFeatureField(GameItem item, ItemFeature feature, InspectorField field, int conditionSection)
		{
			try
			{
				if (conditionSection == 1 || conditionSection == 2)
				{
					ItemCondition condition = ((conditionSection == 1) ? feature.fakeCondition : feature.realCondition);
					ApplyConditionField(condition, field, (conditionSection == 1) ? "fake" : "real");
				}
				else if (field.Kind == InspectorFieldKind.Bool)
				{
					ToggleFeatureBool(feature, field.Key);
				}
				else
				{
					string editorInput = GetEditorInput("feature:" + _inspectorFeatureIndex.ToString(CultureInfo.InvariantCulture) + ":" + field.Key);
					switch (field.Key)
					{
					default:
						return;
					case "identifier":
						feature.identifier = editorInput;
						break;
					case "category":
						feature.category = editorInput;
						break;
					case "actualDisplay":
						feature.actualDisplay = editorInput;
						break;
					case "publicDisplay":
						feature.publicDisplay = editorInput;
						break;
					case "removedByTool":
						feature.removedByTool = editorInput;
						break;
					case "customIntValue1":
						feature.customIntValue1 = ParseInt32(editorInput);
						break;
					case "parentItemUniqueId":
						feature.parentItemUniqueId = ParseInt32(editorInput);
						break;
					case "preExposeValueModifier":
						feature.preExposeValueModifier = ParseInt32(editorInput);
						break;
					case "valueModifier":
						feature.valueModifier = ParseInt32(editorInput);
						break;
					}
				}
				ValidateItemAfterEdit(item);
				_inspectorStatus = Ui("已应用特性字段。", "Feature field applied.");
				RefreshInspectedItem(item);
			}
			catch (Exception ex)
			{
				_inspectorStatus = Ui("应用特性失败：", "Feature apply failed: ") + ex.Message;
			}
		}

		private static void ToggleFeatureBool(ItemFeature feature, string key)
		{
			switch (key)
			{
			case "initiallyShown":
				feature.initiallyShown = !feature.initiallyShown;
				break;
			case "isDisabled":
				feature.isDisabled = !feature.isDisabled;
				break;
			case "isExposable":
				feature.isExposable = !feature.isExposable;
				break;
			case "isExposing":
				feature.isExposing = !feature.isExposing;
				break;
			case "isFeatureDiscovered":
				feature.isFeatureDiscovered = !feature.isFeatureDiscovered;
				break;
			case "isFeatureExposed":
				feature.isFeatureExposed = !feature.isFeatureExposed;
				break;
			case "isFeatureMatch":
				feature.isFeatureMatch = !feature.isFeatureMatch;
				break;
			case "isPublicHidden":
				feature.isPublicHidden = !feature.isPublicHidden;
				break;
			case "useCondition":
				feature.useCondition = !feature.useCondition;
				break;
			case "usePreExposeValue":
				feature.usePreExposeValue = !feature.usePreExposeValue;
				break;
			default:
				throw new InvalidOperationException("unsupported feature flag");
			}
		}

		private void ApplyConditionField(ItemCondition? condition, InspectorField field, string source)
		{
			if (condition == null)
			{
				_inspectorStatus = Ui("该条件未设置，无法编辑。", "This condition is not set and cannot be edited.");
				return;
			}
			string editorInput = GetEditorInput("condition:" + source + ":" + _inspectorFeatureIndex.ToString(CultureInfo.InvariantCulture) + ":" + field.Key);
			switch (field.Key)
			{
			case "identifier":
				condition.identifier = editorInput;
				break;
			case "category":
				condition.category = editorInput;
				break;
			case "display":
				condition.display = editorInput;
				break;
			case "compareValue":
				condition.compareValue = ParseInt32(editorInput);
				break;
			case "customValue":
				condition.customValue = ParseInt32(editorInput);
				break;
			case "modValue":
				condition.modValue = ParseInt32(editorInput);
				break;
			case "modValueDisplay":
				condition.modValueDisplay = ParseInt32(editorInput);
				break;
			case "hiddenAsPublic":
				condition.hiddenAsPublic = !condition.hiddenAsPublic;
				break;
			case "isTransformative":
				condition.isTransformative = !condition.isTransformative;
				break;
			case "onActivateActionId":
				condition.onActivateActionId = editorInput;
				break;
			}
		}

		private void ValidateItemAfterEdit(GameItem item)
		{
			try
			{
				item.Validate();
			}
			catch
			{
			}
			try
			{
				RefreshItemAreas();
			}
			catch
			{
			}
		}

		private void RefreshInspectedItem(GameItem item)
		{
			_inspectedItem = item;
			_selectedItem = item;
		}

		private static int ParseInt32(string value)
		{
			if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				throw new FormatException("value must be a 32-bit integer");
			}
			return result;
		}

		private static long ParseInt64(string value)
		{
			if (!long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				throw new FormatException("value must be a 64-bit integer");
			}
			return result;
		}

		private static float ParseFloat(string value)
		{
			if (!float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				throw new FormatException("value must be a number");
			}
			return result;
		}

		private static double ParseDouble(string value)
		{
			if (!double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				throw new FormatException("value must be a number");
			}
			return result;
		}

		private static bool ParseBool(string value)
		{
			if (bool.TryParse(value, out var result))
			{
				return result;
			}
			if (value == "1")
			{
				return true;
			}
			if (value == "0")
			{
				return false;
			}
			throw new FormatException("value must be true/false or 1/0");
		}

		private static T SafeRead<T>(Func<T> reader, T fallback)
		{
			try
			{
				T val = reader();
				return (val == null) ? fallback : val;
			}
			catch
			{
				return fallback;
			}
		}

		private static string ReadItemTypes(GameItem item)
		{
			try
			{
				List<string> itemTypes = item.itemTypes;
				if (itemTypes == null || itemTypes.Count == 0)
				{
					return string.Empty;
				}
				List<string> list = new List<string>();
				for (int i = 0; i < itemTypes.Count; i++)
				{
					string text = itemTypes[i];
					if (!string.IsNullOrWhiteSpace(text))
					{
						list.Add(text);
					}
				}
				return string.Join(", ", list);
			}
			catch
			{
				return string.Empty;
			}
		}

		private static string ReadParentInventory(GameItem item)
		{
			try
			{
				GameInventory parentInventory = item.parentInventory;
				return (parentInventory == null) ? string.Empty : ((object)parentInventory).GetType().Name;
			}
			catch
			{
				return string.Empty;
			}
		}

		private static string DescribeObject<T>(Func<T> reader)
		{
			try
			{
				T val = reader();
				if (val == null)
				{
					return "(null)";
				}
				string name = val.GetType().Name;
				T val2 = val;
				return name + ": " + val2;
			}
			catch
			{
				return "(unavailable)";
			}
		}

		private static string DescribeObjectCollection<T>(Func<T> reader)
		{
			try
			{
				T value = reader();
				if (value == null)
				{
					return "(null)";
				}
				if (value is ICollection collection)
				{
					return value.GetType().Name + ": count=" + collection.Count.ToString(CultureInfo.InvariantCulture);
				}
				return DescribeObject(() => value);
			}
			catch
			{
				return "(unavailable)";
			}
		}

		private static string DescribeShape(GridShape? shape)
		{
			if (shape == null)
			{
				return string.Empty;
			}
			return SafeRead(() => shape.width + "×" + shape.height + "  bounds=" + shape.minX + "," + shape.minY + ".." + shape.maxX + "," + shape.maxY + "  orientation=" + shape.orientation + "  flipped=" + shape.flipped + "  outside=" + shape.GetOutsideBounds(), string.Empty);
		}

		private void DrawEntryRow(ItemEntry entry, float x, float y, float width)
		{
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_014a: Unknown result type (might be due to invalid IL or missing references)
			string text = entry.Chinese;
			if (string.IsNullOrWhiteSpace(text))
			{
				text = entry.English;
			}
			if (string.IsNullOrWhiteSpace(text))
			{
				text = entry.Id;
			}
			string text2 = ((_catalogValidationComplete && !entry.IsAvailable && !entry.IsGenerated) ? "  ?" : string.Empty);
			float num = Math.Max(360f, width - 62f - 24f);
			float num2 = num * 0.34f;
			float num3 = num * 0.38f;
			float num4 = num - num2 - num3;
			float num5 = x + 6f;
			float num6 = num5 + num2 + 8f;
			float num7 = num6 + num3 + 8f;
			float num8 = 38f;
			SafeLabel(new Rect(num5, y + 2f, num2, num8), FitText(text + text2, num2), "row-title");
			SafeLabel(new Rect(num6, y + 2f, num3, num8), FitText(entry.English ?? string.Empty, num3), "row-english");
			SafeLabel(new Rect(num7, y + 2f, num4, num8), FitText(entry.ResolvedId ?? entry.Id, num4), "row-id");
			if (SafeButton(new Rect(x + width - 62f, y + 2f, 62f, 34f), Ui("添加", "Add"), "row-button"))
			{
				AddItem(entry);
			}
		}

		private void AddManualItem()
		{
			string text = (_manualId ?? string.Empty).Trim();
			if (text.Length == 0)
			{
				_status = Ui("请输入物品标识符。", "Enter an item identifier.");
				return;
			}
			AddItem(new ItemEntry
			{
				Id = text,
				English = text,
				Chinese = string.Empty,
				Aliases = new List<string> { text }
			});
		}

		private void AddItem(ItemEntry entry)
		{
			if (!TryReadAmount(out var amount))
			{
				_status = Ui("数量必须是 1 到 999 的整数。", "Quantity must be an integer from 1 to 999.");
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				_status = Ui("请先进入一个存档；主菜单没有可用的玩家柜台。", "Enter a save first; the main menu has no player counter.");
				return;
			}
			GameItem item = null;
			string text = entry.ResolvedId ?? entry.Id;
			Exception ex = null;
			if (!string.IsNullOrWhiteSpace(entry.GeneratorKey))
			{
				if (!TryCreateGeneratedItem(entry.GeneratorKey, out item, out string detail))
				{
					_status = Ui("无法生成：", "Could not generate: ") + entry.Chinese + (string.IsNullOrWhiteSpace(detail) ? string.Empty : Ui("（" + detail + "）", " (" + detail + ")"));
					return;
				}
			}
			else
			{
				if (TryCreateNativeLootCrate(entry.ResolvedId ?? entry.Id, out item))
				{
					text = entry.ResolvedId ?? entry.Id;
				}
				List<string> list = new List<string>();
				if (item == null && !string.IsNullOrWhiteSpace(entry.ResolvedId))
				{
					list.Add(entry.ResolvedId);
				}
				if (item == null && !string.IsNullOrWhiteSpace(entry.Id) && !list.Contains(entry.Id, StringComparer.Ordinal))
				{
					list.Add(entry.Id);
				}
				if (item == null && entry.Aliases != null)
				{
					foreach (string alias in entry.Aliases)
					{
						if (!string.IsNullOrWhiteSpace(alias) && !list.Contains(alias, StringComparer.Ordinal))
						{
							list.Add(alias);
						}
					}
				}
				foreach (string item2 in list)
				{
					try
					{
						GameItem val = ItemSpawner.Spawn(item2);
						if (val == null)
						{
							continue;
						}
						item = val;
						text = item2;
						break;
					}
					catch (Exception ex2)
					{
						ex = ex2;
					}
				}
			}
			if (item != null)
			{
				try
				{
					item.SetAmount(amount);
					instance.AddDirectToWeightedTable(item, true);
					try
					{
						instance.RefreshCounterItem();
					}
					catch
					{
					}
					entry.ResolvedId = text;
					entry.IsAvailable = true;
					string text2 = entry.Chinese;
					if (string.IsNullOrWhiteSpace(text2))
					{
						text2 = entry.English;
					}
					if (string.IsNullOrWhiteSpace(text2))
					{
						text2 = text;
					}
					_status = Ui("已添加到玩家侧柜台：", "Added to the player's counter: ") + text2 + " × " + amount;
					MelonLogger.Msg("Probably Stolen Item Manager: added to player weighted/counter area " + text + " x" + amount);
					return;
				}
				catch (Exception ex3)
				{
					ex = ex3;
				}
			}
			_status = Ui("无法生成：", "Could not spawn: ") + (entry.Id ?? Ui("（空标识符）", "(empty identifier)")) + Ui("。可尝试使用物品的实际 ID。", ". Try the item's actual ID.");
			if (ex != null)
			{
				MelonLogger.Warning("Probably Stolen Item Manager: spawn failed for " + entry.Id + ": " + ex.Message);
			}
		}

		private static bool TryCreateGeneratedItem(string generatorKey, out GameItem? item, out string detail)
		{
			item = null;
			detail = string.Empty;
			try
			{
				if (generatorKey.StartsWith("table:", StringComparison.Ordinal))
				{
					int length = "table:".Length;
					string text = ResolveNamedTableId(generatorKey.Substring(length, generatorKey.Length - length));
					if (string.IsNullOrWhiteSpace(text))
					{
						detail = "the table is not initialized";
						return false;
					}
					item = ItemSpawner.SpawnFromTable(text);
				}
				else
				{
					item = generatorKey switch
					{
						"prebuilt:random_cigarette" => PreBuiltItemHelper.RandomCigarette(), 
						"prebuilt:fake_cigarette" => PreBuiltItemHelper.FakeCigarette(), 
						"prebuilt:counterfeit_cigarette" => PreBuiltItemHelper.CounterfeitCigarette(), 
						"prebuilt:random_injector" => PreBuiltItemHelper.CreateRandomInjector(), 
						"prebuilt:genuine_injector" => PreBuiltItemHelper.CreateRealGenuineInjector(), 
						"prebuilt:expired_injector" => PreBuiltItemHelper.CreateRealExpiredInjector(), 
						"prebuilt:counterfeit_injector" => PreBuiltItemHelper.CreateRealCounterfeitInjector(), 
						"prebuilt:random_stamp" => PreBuiltItemHelper.CreateRandomStamp(), 
						"prebuilt:fake_stamp" => PreBuiltItemHelper.CreateFakeStamp(), 
						"prebuilt:real_stamp" => PreBuiltItemHelper.CreateRealStamp(), 
						"prebuilt:random_performance_module" => PreBuiltItemHelper.RandomPerformanceModule(), 
						"prebuilt:random_efficiency_module" => PreBuiltItemHelper.RandomEfficiencyModule(), 
						"prebuilt:random_quality_module" => PreBuiltItemHelper.RandomQualityModule(), 
						"prebuilt:random_overclock_module" => PreBuiltItemHelper.RandomOverclockModule(), 
						"prebuilt:random_eco_module" => PreBuiltItemHelper.RandomEcoModule(), 
						"prebuilt:random_fineness_module" => PreBuiltItemHelper.RandomFinenessModule(), 
						"prebuilt:random_node" => PreBuiltItemHelper.RandomNode(), 
						"prebuilt:random_ribwich" => PreBuiltItemHelper.CreateRibwichRandom(), 
						"prebuilt:random_hand" => PreBuiltItemHelper.CreateHandRandom(), 
						_ => (GameItem)null, 
					};
				}
				if (item == null)
				{
					detail = "the game returned no item";
					return false;
				}
				return true;
			}
			catch (Exception ex)
			{
				detail = ex.Message;
				return false;
			}
		}

		private static bool TryCreateNativeLootCrate(string identifier, out GameItem? item)
		{
			item = identifier switch
			{
				"evidence_box" => PreBuiltItemHelper.LootCrateEvidence(), 
				"med_box" => PreBuiltItemHelper.LootCrateMedical(), 
				"sec_box" => PreBuiltItemHelper.LootCrateSecurity(), 
				"service_box" => PreBuiltItemHelper.LootCrateService(), 
				"eng_box" => PreBuiltItemHelper.LootCrateEngineering(), 
				_ => (GameItem)null, 
			};
			if (item == null)
			{
				switch (identifier)
				{
				case "sci_box":
				case "research_box":
				case "sup_box":
				case "supply_box":
				{
					string[] array = ((!(identifier == "sci_box") && !(identifier == "research_box")) ? new string[1] { "LootCrateSupply" } : new string[2] { "LootCrateResearch", "LootCrateScience" });
					foreach (string name in array)
					{
						try
						{
							object? obj = typeof(PreBuiltItemHelper).GetMethod(name, BindingFlags.Static | BindingFlags.Public, Type.EmptyTypes)?.Invoke(null, null);
							item = (GameItem?)((obj is GameItem) ? obj : null);
							if (item != null)
							{
								break;
							}
						}
						catch
						{
						}
					}
					break;
				}
				}
			}
			return item != null;
		}

		private static string ResolveNamedTableId(string tableKey)
		{
			try
			{
				if ((Object)(object)TableMaster.Instance != (Object)null)
				{
					string text = tableKey switch
					{
						"junk" => TableMaster.junkTable, 
						"access_card" => TableMaster.accessCardTable, 
						"all_module" => TableMaster.allModuleTable, 
						"makeshift_weapon" => TableMaster.makeshiftWeaponTable, 
						"material" => TableMaster.materialTable, 
						"household" => TableMaster.householdTable, 
						"packed_food" => TableMaster.packedFoodTable, 
						"t1module" => TableMaster.t1moduleTable, 
						"t2module" => TableMaster.t2moduleTable, 
						"tool" => TableMaster.toolTable, 
						"medical" => TableMaster.medicalTable, 
						_ => string.Empty, 
					};
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text;
					}
				}
			}
			catch
			{
			}
			return tableKey switch
			{
				"junk" => "junk", 
				"access_card" => "access_card", 
				"all_module" => "all_module", 
				"makeshift_weapon" => "makeshift_weapon", 
				"material" => "material", 
				"household" => "household", 
				"packed_food" => "packed_food", 
				"t1module" => "t1module", 
				"t2module" => "t2module", 
				"tool" => "tool", 
				"medical" => "medical", 
				_ => string.Empty, 
			};
		}

		private bool TryReadAmount(out int amount)
		{
			amount = Math.Clamp(_amount, 0, 999);
			return amount >= 1;
		}

		private void SetAmountValue(int value)
		{
			_amount = Math.Clamp(value, 1, 999);
		}

		private void RebuildFiltered()
		{
			_filtered.Clear();
			string text = (_searchText ?? string.Empty).Trim();
			foreach (ItemEntry item in _catalog)
			{
				if (text.Length == 0 || item.SearchText.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					_filtered.Add(item);
				}
			}
			int num = Math.Max(1, (_filtered.Count + 12 - 1) / 12);
			_pageIndex = Math.Clamp(_pageIndex, 0, num - 1);
		}

		private void ProcessMouseClick()
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (!Input.GetMouseButton(0))
				{
					_mouseButtonWasDown = false;
				}
				else
				{
					if (_mouseButtonWasDown)
					{
						return;
					}
					_mouseButtonWasDown = true;
					Vector3 mousePosition = Input.mousePosition;
					float num = mousePosition.x - _windowRect.x;
					float num2 = (float)Screen.height - mousePosition.y - _windowRect.y;
					float width = _windowRect.width;
					float height = _windowRect.height;
					if (num < 0f || num > width || num2 < 0f || num2 > height)
					{
						ClearInputFocusState();
						_clearInputFocus = true;
						_focusInputRequested = false;
						_focusControlName = string.Empty;
						return;
					}
					float width2 = Math.Max(220f, width - 72f - 190f);
					string text = (_inspectorVisible ? GetInspectorInputControlAt(num, num2) : string.Empty);
					bool num3 = !_inspectorVisible && Hit(num, num2, 72f, 60f, width2, 32f);
					bool flag = !_inspectorVisible && _pageJumpMode && Hit(num, num2, 498f, 696f, 100f, 32f);
					if (num3)
					{
						_focusInputRequested = true;
						_focusControlName = (_manualIdMode ? "item-manager-manual-id" : "item-manager-search");
						FocusInputField(_focusControlName);
					}
					else if (flag)
					{
						_focusInputRequested = true;
						_focusControlName = "item-manager-page";
						FocusInputField(_focusControlName);
					}
					else if (!string.IsNullOrEmpty(text))
					{
						_focusInputRequested = true;
						_focusControlName = text;
						FocusInputField(text);
					}
					else
					{
						ClearInputFocusState();
						_clearInputFocus = true;
						_focusInputRequested = false;
						_focusControlName = string.Empty;
					}
					if (Hit(num, num2, width - 100f, 26f, 88f, 32f))
					{
						_windowOpen = false;
						CloseInspector();
						_pageJumpMode = false;
						_dragging = false;
						_resizing = false;
						_mouseButtonWasDown = true;
						_clearInputFocus = true;
						_focusInputRequested = false;
						_focusControlName = string.Empty;
						ClearInputFocusState();
					}
					else if (Hit(num, num2, width - 224f, 26f, 112f, 32f))
					{
						if (_inspectorVisible)
						{
							CloseInspector();
						}
						else
						{
							OpenInspector(_selectedItem ?? _contextItem);
						}
					}
					else
					{
						if (_resizing)
						{
							return;
						}
						if (_inspectorVisible)
						{
							ProcessInspectorMouseClick(num, num2);
							return;
						}
						if (Hit(num, num2, width - 174f, 60f, 76f, 32f))
						{
							if (_manualIdMode)
							{
								_manualId = string.Empty;
								return;
							}
							_searchText = string.Empty;
							_pageIndex = 0;
							return;
						}
						if (Hit(num, num2, width - 90f, 60f, 78f, 32f))
						{
							_manualIdMode = !_manualIdMode;
							_clearInputFocus = true;
							_focusInputRequested = false;
							_focusControlName = string.Empty;
							ClearInputFocusState();
							_status = (_manualIdMode ? Ui("已切换到手动 ID 模式。", "Switched to ID mode.") : Ui("已切换到搜索模式。", "Switched to search mode."));
							return;
						}
						if (Hit(num, num2, 58f, 98f, 32f, 32f))
						{
							SetAmountValue(_amount - 1);
							return;
						}
						if (Hit(num, num2, 168f, 98f, 32f, 32f))
						{
							SetAmountValue(_amount + 1);
							return;
						}
						if (Hit(num, num2, 206f, 98f, 44f, 32f))
						{
							SetAmountValue(1);
							return;
						}
						if (Hit(num, num2, 254f, 98f, 44f, 32f))
						{
							SetAmountValue(5);
							return;
						}
						if (Hit(num, num2, 302f, 98f, 52f, 32f))
						{
							SetAmountValue(10);
							return;
						}
						if (_manualIdMode && Hit(num, num2, width - 86f, 98f, 74f, 32f))
						{
							AddManualItem();
							return;
						}
						int num4 = Math.Max(1, (_filtered.Count + 12 - 1) / 12);
						float top = 696f;
						if (Hit(num, num2, 12f, top, 82f, 32f))
						{
							_pageIndex = 0;
						}
						else if (Hit(num, num2, 102f, top, 80f, 32f))
						{
							_pageIndex = Math.Max(0, _pageIndex - 1);
						}
						else if (Hit(num, num2, 190f, top, 80f, 32f))
						{
							_pageIndex = Math.Min(num4 - 1, _pageIndex + 1);
						}
						else if (Hit(num, num2, 278f, top, 74f, 32f))
						{
							_pageIndex = num4 - 1;
						}
						else if (Hit(num, num2, 360f, top, 72f, 32f))
						{
							_pageJumpMode = true;
							_pageJumpText = string.Empty;
							_clearInputFocus = true;
							_focusInputRequested = false;
							_focusControlName = string.Empty;
							ClearInputFocusState();
							_status = Ui("请输入页码后按 Enter 确认。", "Enter a page number, then press Enter to confirm.");
						}
						else
						{
							int num5 = _pageIndex * 12;
							int num6 = (int)Math.Floor((num2 - 184f) / 42f);
							if (num6 >= 0 && num6 < 12 && num5 + num6 < _filtered.Count && Hit(num, num2, width - 62f, 184f + (float)num6 * 42f, 62f, 34f))
							{
								AddItem(_filtered[num5 + num6]);
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				LogUiFailure("mouse-input", ex);
			}
		}

		private void ProcessWindowDrag()
		{
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (_resizing)
				{
					_dragging = false;
					return;
				}
				if (!Input.GetMouseButton(0))
				{
					_dragging = false;
					return;
				}
				Vector3 mousePosition = Input.mousePosition;
				float num = mousePosition.x - _windowRect.x;
				float num2 = (float)Screen.height - mousePosition.y - _windowRect.y;
				if (!_dragging)
				{
					bool num3 = num >= 0f && num <= _windowRect.width - 12f && num2 >= 0f && num2 <= 25f;
					bool flag = num >= 0f && num <= _windowRect.width - 236f && num2 >= 28f && num2 <= 54f;
					if (!num3 && !flag)
					{
						return;
					}
					_dragging = true;
					_dragOffsetX = num;
					_dragOffsetY = num2;
				}
				_windowRect.x = mousePosition.x - _dragOffsetX;
				_windowRect.y = (float)Screen.height - mousePosition.y - _dragOffsetY;
				_windowRect.x = Math.Clamp(_windowRect.x, 0f, Math.Max(0f, (float)Screen.width - _windowRect.width));
				_windowRect.y = Math.Clamp(_windowRect.y, 0f, Math.Max(0f, (float)Screen.height - _windowRect.height));
			}
			catch (Exception ex)
			{
				LogUiFailure("window-drag", ex);
			}
		}

		private void ProcessWindowResize()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (!Input.GetMouseButton(0))
				{
					_resizing = false;
					return;
				}
				Vector3 mousePosition = Input.mousePosition;
				float num = mousePosition.x - _windowRect.x;
				float num2 = (float)Screen.height - mousePosition.y - _windowRect.y;
				float width = _windowRect.width;
				float height = _windowRect.height;
				if (!_resizing)
				{
					if (!Hit(num, num2, width - 26f, height - 26f, 26f, 26f))
					{
						return;
					}
					_resizing = true;
					_dragging = false;
					_resizeOffsetX = width - num;
					_resizeOffsetY = height - num2;
				}
				float max = Math.Max(1040f, (float)Screen.width - _windowRect.x - 12f);
				float max2 = Math.Max(820f, (float)Screen.height - _windowRect.y - 12f);
				_windowRect.width = Math.Clamp(num + _resizeOffsetX, 1040f, max);
				_windowRect.height = Math.Clamp(num2 + _resizeOffsetY, 820f, max2);
			}
			catch (Exception ex)
			{
				LogUiFailure("window-resize", ex);
			}
		}

		private static string FitText(string? text, float width)
		{
			if (string.IsNullOrEmpty(text) || width <= 8f)
			{
				return string.Empty;
			}
			float num = Math.Max(1f, width - 6f);
			float num2 = 0f;
			for (int i = 0; i < text.Length; i++)
			{
				float num3 = ((text[i] >= '?') ? 22f : 11f);
				if (num2 + num3 > num)
				{
					if (i == 0)
					{
						return "…";
					}
					if (num2 + 20f <= num)
					{
						return text.Substring(0, i) + "…";
					}
					return text.Substring(0, Math.Max(1, i - 1)) + "…";
				}
				num2 += num3;
			}
			return text;
		}

		private static bool Hit(float x, float y, float left, float top, float width, float height)
		{
			if (x >= left && x <= left + width && y >= top)
			{
				return y <= top + height;
			}
			return false;
		}

		private void ApplyPageJump()
		{
			int num = Math.Max(1, (_filtered.Count + 12 - 1) / 12);
			if (!int.TryParse(_pageJumpText, NumberStyles.None, CultureInfo.InvariantCulture, out var result) || result < 1 || result > num)
			{
				_status = Ui("页码范围为 1 到 " + num + "。", "Page must be between 1 and " + num + ".");
				return;
			}
			_pageIndex = result - 1;
			_pageJumpMode = false;
			_pageJumpText = string.Empty;
			ClearInputFocusState();
			_clearInputFocus = true;
			_status = Ui("已跳转到第 " + result + " 页。", "Moved to page " + result + ".");
		}

		private void PollFocusedInput()
		{
			try
			{
				int frameCount = Time.frameCount;
				if (_lastInputPollFrame == frameCount)
				{
					return;
				}
				_lastInputPollFrame = frameCount;
				_inputComposition = string.Empty;
				_inputPolledText = string.Empty;
				if (!_windowOpen || !_inputFieldFocused || string.IsNullOrEmpty(_inputFieldName))
				{
					return;
				}
				SetImeMode(enabled: true);
				_inputPolledText = Input.inputString ?? string.Empty;
				_inputComposition = Input.compositionString ?? string.Empty;
				bool flag = !string.IsNullOrEmpty(_inputComposition);
				string inputPolledText = _inputPolledText;
				for (int i = 0; i < inputPolledText.Length; i++)
				{
					char c = inputPolledText[i];
					if (char.IsControl(c) || (flag && c < '\u0080'))
					{
						continue;
					}
					if (c >= '\u0080')
					{
						if (!IsRecentImeDuplicate(c, fromPoll: true))
						{
							AppendInputText(c.ToString());
							RememberImeCharacter(c, fromPoll: true);
						}
					}
					else
					{
						AppendInputText(c.ToString());
					}
				}
			}
			catch (Exception ex)
			{
				LogUiFailure("text-input", ex);
			}
		}

		private void DrawCustomInputField(Rect rect, string value, string controlName, string placeholder, string stage, bool digitsOnly = false)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				HandleCustomInputEvent(rect, controlName, digitsOnly);
				string text = GetInputValue(controlName);
				if (string.IsNullOrEmpty(text) && !_inputFieldFocused)
				{
					text = placeholder;
				}
				else if (_inputFieldFocused && string.Equals(_inputFieldName, controlName, StringComparison.Ordinal))
				{
					string text2 = (digitsOnly ? new string(_inputComposition.Where(IsAsciiDigit).ToArray()) : _inputComposition);
					text = text + text2 + "|";
				}
				GUI.Box(rect, string.Empty, GUI.skin.box);
				SafeLabel(new Rect(rect.x + 8f, rect.y, rect.width - 12f, rect.height), FitText(text, rect.width - 12f), stage + "-label");
			}
			catch (Exception ex)
			{
				LogUiFailure(stage, ex);
			}
		}

		private void HandleCustomInputEvent(Rect rect, string controlName, bool digitsOnly)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0055: Invalid comparison between Unknown and I4
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Invalid comparison between Unknown and I4
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Invalid comparison between Unknown and I4
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Invalid comparison between Unknown and I4
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Invalid comparison between Unknown and I4
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Invalid comparison between Unknown and I4
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Invalid comparison between Unknown and I4
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Invalid comparison between Unknown and I4
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Expected I4, but got Unknown
			Event current = Event.current;
			if (current == null)
			{
				return;
			}
			if ((int)current.type == 0 && current.button == 0 && rect.Contains(current.mousePosition))
			{
				FocusInputField(controlName);
				current.Use();
			}
			else
			{
				if (!_inputFieldFocused || !string.Equals(_inputFieldName, controlName, StringComparison.Ordinal) || (int)current.type != 4)
				{
					return;
				}
				if (current.control && (int)current.keyCode == 97)
				{
					SetInputValue(controlName, string.Empty);
					_inputCaretIndex = 0;
					current.Use();
					return;
				}
				if (current.control && (int)current.keyCode == 118)
				{
					try
					{
						AppendInputText(GUIUtility.systemCopyBuffer);
					}
					catch (Exception ex)
					{
						LogUiFailure("clipboard", ex);
					}
					current.Use();
					return;
				}
				KeyCode keyCode = current.keyCode;
				if ((int)keyCode <= 13)
				{
					if ((int)keyCode == 8)
					{
						RemovePreviousInputCharacter();
						current.Use();
						return;
					}
					if ((int)keyCode != 13)
					{
						goto IL_0238;
					}
				}
				else
				{
					if ((int)keyCode == 27)
					{
						if (_pageJumpMode && string.Equals(controlName, "item-manager-page", StringComparison.Ordinal))
						{
							_pageJumpMode = false;
							_pageJumpText = string.Empty;
							_status = Ui("已取消页码跳转。", "Page jump cancelled.");
						}
						else
						{
							SetInputValue(controlName, string.Empty);
						}
						ClearInputFocusState();
						_clearInputFocus = true;
						current.Use();
						return;
					}
					if ((int)keyCode == 127)
					{
						RemoveNextInputCharacter();
						current.Use();
						return;
					}
					switch (keyCode - 271)
					{
					case 5:
						_inputCaretIndex = Math.Max(0, _inputCaretIndex - 1);
						current.Use();
						return;
					case 4:
						_inputCaretIndex = Math.Min(GetInputValue(controlName).Length, _inputCaretIndex + 1);
						current.Use();
						return;
					case 7:
						_inputCaretIndex = 0;
						current.Use();
						return;
					case 8:
						_inputCaretIndex = GetInputValue(controlName).Length;
						current.Use();
						return;
					case 0:
						break;
					default:
						goto IL_0238;
					}
				}
				if (string.Equals(controlName, "item-manager-page", StringComparison.Ordinal))
				{
					ApplyPageJump();
				}
				else if (string.Equals(controlName, "item-manager-manual-id", StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(_manualId))
				{
					AddManualItem();
				}
				current.Use();
			}
			return;
			IL_0238:
			char character = current.character;
			if (character < '\u0080' || char.IsControl(character))
			{
				current.Use();
				return;
			}
			if (IsRecentImeDuplicate(character, fromPoll: false))
			{
				current.Use();
				return;
			}
			AppendInputText(character.ToString());
			RememberImeCharacter(character, fromPoll: false);
			current.Use();
		}

		private void AppendInputText(string? text)
		{
			if (!_inputFieldFocused || string.IsNullOrEmpty(_inputFieldName) || string.IsNullOrEmpty(text))
			{
				return;
			}
			string text2 = GetInputValue(_inputFieldName);
			int num = (string.Equals(_inputFieldName, "item-manager-page", StringComparison.Ordinal) ? 6 : 80);
			int num2 = Math.Clamp(_inputCaretIndex, 0, text2.Length);
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (!char.IsControl(c) && (!string.Equals(_inputFieldName, "item-manager-page", StringComparison.Ordinal) || IsAsciiDigit(c)))
				{
					if (text2.Length >= num)
					{
						break;
					}
					text2 = text2.Insert(num2, c.ToString());
					num2++;
				}
			}
			SetInputValue(_inputFieldName, text2);
			_inputCaretIndex = Math.Clamp(num2, 0, GetInputValue(_inputFieldName).Length);
		}

		private void RemovePreviousInputCharacter()
		{
			string inputValue = GetInputValue(_inputFieldName);
			int num = Math.Clamp(_inputCaretIndex, 0, inputValue.Length);
			if (num > 0)
			{
				int num2 = num - 1;
				if (char.IsLowSurrogate(inputValue[num2]) && num2 > 0 && char.IsHighSurrogate(inputValue[num2 - 1]))
				{
					num2--;
				}
				SetInputValue(_inputFieldName, inputValue.Remove(num2, num - num2));
				_inputCaretIndex = num2;
			}
		}

		private void RemoveNextInputCharacter()
		{
			string inputValue = GetInputValue(_inputFieldName);
			int num = Math.Clamp(_inputCaretIndex, 0, inputValue.Length);
			if (num < inputValue.Length)
			{
				int count = 1;
				if (char.IsHighSurrogate(inputValue[num]) && num + 1 < inputValue.Length && char.IsLowSurrogate(inputValue[num + 1]))
				{
					count = 2;
				}
				SetInputValue(_inputFieldName, inputValue.Remove(num, count));
				_inputCaretIndex = Math.Min(num, GetInputValue(_inputFieldName).Length);
			}
		}

		private string GetInputValue(string controlName)
		{
			return controlName switch
			{
				"item-manager-search" => _searchText, 
				"item-manager-manual-id" => _manualId, 
				"item-manager-page" => _pageJumpText, 
				_ => GetEditorInput(EditorInputKey(controlName)), 
			};
		}

		private void SetInputValue(string controlName, string? value)
		{
			string text = value ?? string.Empty;
			text = ((!string.Equals(controlName, "item-manager-page", StringComparison.Ordinal)) ? LimitText(text, 80) : new string(text.Where(IsAsciiDigit).Take(6).ToArray()));
			switch (controlName)
			{
			case "item-manager-search":
				if (!string.Equals(_searchText, text, StringComparison.Ordinal))
				{
					_pageIndex = 0;
				}
				_searchText = text;
				break;
			case "item-manager-manual-id":
				_manualId = text;
				break;
			case "item-manager-page":
				_pageJumpText = text;
				break;
			default:
			{
				string text2 = EditorInputKey(controlName);
				if (!string.IsNullOrEmpty(text2))
				{
					_editorInputValues[text2] = text;
				}
				break;
			}
			}
			_inputCaretIndex = Math.Clamp(_inputCaretIndex, 0, GetInputValue(controlName).Length);
		}

		private static bool IsAsciiDigit(char character)
		{
			if (character >= '0')
			{
				return character <= '9';
			}
			return false;
		}

		private bool IsRecentImeDuplicate(char character, bool fromPoll)
		{
			int num = (fromPoll ? _lastEventImeFrame : _lastPolledImeFrame);
			string text = (fromPoll ? _lastEventImeText : _lastPolledImeText);
			int num2 = Time.frameCount - num;
			if (num >= 0 && num2 >= 0 && num2 <= 3)
			{
				return text.IndexOf(character) >= 0;
			}
			return false;
		}

		private void RememberImeCharacter(char character, bool fromPoll)
		{
			int frameCount = Time.frameCount;
			if (fromPoll)
			{
				if (_lastPolledImeFrame != frameCount)
				{
					_lastPolledImeText = string.Empty;
				}
				_lastPolledImeFrame = frameCount;
				_lastPolledImeText += character;
				if (_lastPolledImeText.Length > 64)
				{
					string lastPolledImeText = _lastPolledImeText;
					int length = lastPolledImeText.Length;
					int num = length - 64;
					_lastPolledImeText = lastPolledImeText.Substring(num, length - num);
				}
			}
			else
			{
				if (_lastEventImeFrame != frameCount)
				{
					_lastEventImeText = string.Empty;
				}
				_lastEventImeFrame = frameCount;
				_lastEventImeText += character;
				if (_lastEventImeText.Length > 64)
				{
					string lastEventImeText = _lastEventImeText;
					int num = lastEventImeText.Length;
					int length = num - 64;
					_lastEventImeText = lastEventImeText.Substring(length, num - length);
				}
			}
		}

		private void FocusInputField(string controlName)
		{
			if (!(controlName != "item-manager-search") || !(controlName != "item-manager-manual-id") || !(controlName != "item-manager-page") || controlName.StartsWith("item-editor:", StringComparison.Ordinal))
			{
				_inputFieldFocused = true;
				_inputFieldName = controlName;
				_inputCaretIndex = GetInputValue(controlName).Length;
				SetImeMode(enabled: true);
			}
		}

		private void ClearInputFocusState()
		{
			_inputFieldFocused = false;
			_inputFieldName = string.Empty;
			_inputCaretIndex = 0;
			_inputComposition = string.Empty;
			_inputPolledText = string.Empty;
			_lastPolledImeFrame = -1;
			_lastPolledImeText = string.Empty;
			_lastEventImeFrame = -1;
			_lastEventImeText = string.Empty;
			SetImeMode(enabled: false);
		}

		private void SetImeMode(bool enabled)
		{
			if (_imeModeEnabled == enabled)
			{
				return;
			}
			try
			{
				Input.imeCompositionMode = (IMECompositionMode)(enabled ? 1 : 0);
				_imeModeEnabled = enabled;
			}
			catch (Exception ex)
			{
				LogUiFailure("ime", ex);
			}
		}

		private static string LimitText(string? value, int maxLength)
		{
			string text = value ?? string.Empty;
			if (text.Length > maxLength)
			{
				return text.Substring(0, maxLength);
			}
			return text;
		}

		private void ApplyFocusRequest(string controlName)
		{
			if (!_focusInputRequested || !string.Equals(_focusControlName, controlName, StringComparison.Ordinal))
			{
				return;
			}
			try
			{
				FocusInputField(controlName);
			}
			catch (Exception ex)
			{
				LogUiFailure("text-focus", ex);
			}
			finally
			{
				_focusInputRequested = false;
				_focusControlName = string.Empty;
			}
		}

		private void EnsureEditorInput(string key, string? value)
		{
			if (!_editorInputValues.ContainsKey(key))
			{
				_editorInputValues[key] = value ?? string.Empty;
			}
		}

		private string GetEditorInput(string? key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return string.Empty;
			}
			if (!_editorInputValues.TryGetValue(key, out string value))
			{
				return string.Empty;
			}
			return value;
		}

		private static string EditorControlName(string inputKey)
		{
			return "item-editor:" + inputKey;
		}

		private static string EditorInputKey(string controlName)
		{
			if (!controlName.StartsWith("item-editor:", StringComparison.Ordinal))
			{
				return controlName;
			}
			int length = "item-editor:".Length;
			return controlName.Substring(length, controlName.Length - length);
		}

		private void LoadCatalog()
		{
			try
			{
				string path = Path.Combine(Path.GetDirectoryName(typeof(ItemManager).Assembly.Location) ?? AppContext.BaseDirectory, "item_catalog.json");
				if (File.Exists(path))
				{
					JsonSerializerOptions options = new JsonSerializerOptions
					{
						PropertyNameCaseInsensitive = true
					};
					List<ItemEntry> list = JsonSerializer.Deserialize<List<ItemEntry>>(File.ReadAllText(path), options);
					if (list != null)
					{
						foreach (ItemEntry item in list)
						{
							if (item != null && !string.IsNullOrWhiteSpace(item.Id))
							{
								ItemEntry itemEntry = item;
								if (itemEntry.Aliases == null)
								{
									List<string> list2 = (itemEntry.Aliases = new List<string>());
								}
								if (!item.Aliases.Contains(item.Id, StringComparer.Ordinal))
								{
									item.Aliases.Insert(0, item.Id);
								}
								PrepareEntry(item);
								_catalog.Add(item);
							}
						}
					}
				}
				AddGeneratedEntries();
				SortCatalog();
				_catalogLoaded = _catalog.Count > 0;
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("Probably Stolen Item Manager: 读取 item_catalog.json 失败：" + ex.Message);
			}
		}

		private void AddGeneratedEntries()
		{
			GeneratedEntryDefinition[] generatedEntries = GeneratedEntries;
			foreach (GeneratedEntryDefinition definition in generatedEntries)
			{
				if (!_catalog.Any((ItemEntry entry) => string.Equals(entry.GeneratorKey, definition.Key, StringComparison.Ordinal)))
				{
					ItemEntry itemEntry = new ItemEntry
					{
						Id = definition.Key,
						English = definition.English,
						Chinese = definition.Chinese,
						Aliases = new List<string> { definition.Key },
						GeneratorKey = definition.Key,
						IsAvailable = true
					};
					PrepareEntry(itemEntry);
					_catalog.Add(itemEntry);
				}
			}
		}

		private void PrepareEntry(ItemEntry entry)
		{
			List<string> list = new List<string> { entry.Id, entry.English, entry.Chinese };
			if (entry.Aliases != null)
			{
				list.AddRange(entry.Aliases);
			}
			entry.SearchText = string.Join(" ", list.Where((string x) => !string.IsNullOrWhiteSpace(x)));
		}

		private ItemEntry? FindEntryByIdentifier(string identifier)
		{
			foreach (ItemEntry item in _catalog)
			{
				if (string.Equals(item.Id, identifier, StringComparison.Ordinal) || item.Aliases.Any((string alias) => string.Equals(alias, identifier, StringComparison.Ordinal)))
				{
					return item;
				}
			}
			return null;
		}

		private void SortCatalog()
		{
			foreach (ItemEntry item in _catalog)
			{
				PrepareEntry(item);
			}
			_catalog.Sort((ItemEntry a, ItemEntry b) =>
			{
				string strA = (string.IsNullOrWhiteSpace(a.Chinese) ? a.English : a.Chinese);
				string strB = (string.IsNullOrWhiteSpace(b.Chinese) ? b.English : b.Chinese);
				int num = string.Compare(strA, strB, StringComparison.CurrentCultureIgnoreCase);
				return (num == 0) ? string.Compare(a.Id, b.Id, StringComparison.OrdinalIgnoreCase) : num;
			});
		}

		private void MergeRuntimeCatalog()
		{
			if (_runtimeCatalogMerged)
			{
				return;
			}
			try
			{
				try
				{
					if (PlayerStore.Instance == null)
					{
						return;
					}
				}
				catch
				{
					return;
				}
				HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
				int num = 0;
				int num2 = 0;
				int num3 = 0;
				string[] runtimeDirectoryNames = RuntimeDirectoryNames;
				foreach (string text in runtimeDirectoryNames)
				{
					try
					{
						List<string> identifierList = DirectoryMaster.GetIdentifierList<GameItem>(text);
						if (identifierList == null)
						{
							continue;
						}
						num3++;
						for (int j = 0; j < identifierList.Count; j++)
						{
							string text2 = identifierList[j];
							if (!string.IsNullOrWhiteSpace(text2) && hashSet.Add(text2))
							{
								num++;
								ItemEntry itemEntry = FindEntryByIdentifier(text2);
								if (itemEntry == null)
								{
									itemEntry = new ItemEntry
									{
										Id = text2,
										English = text2,
										Chinese = string.Empty,
										Aliases = new List<string> { text2 }
									};
									_catalog.Add(itemEntry);
									num2++;
								}
								itemEntry.ResolvedId = text2;
								itemEntry.IsAvailable = true;
								PrepareEntry(itemEntry);
							}
						}
					}
					catch (Exception ex)
					{
						MelonLogger.Warning("Probably Stolen Item Manager: 读取运行时目录 " + text + " 失败：" + ex.Message);
					}
				}
				if (num == 0)
				{
					Dictionary<Type, List<Object>> directories = DirectoryMaster.directories;
					if (directories != null && directories.Count > 0)
					{
						Enumerator<Type, List<Object>> enumerator = directories.Keys.GetEnumerator();
						while (enumerator.MoveNext())
						{
							Type current = enumerator.Current;
							List<Object> val = directories[current];
							if (val == null)
							{
								continue;
							}
							num3++;
							for (int k = 0; k < val.Count; k++)
							{
								Object val2 = val[k];
								GameItem val3 = ((val2 != null) ? ((Il2CppObjectBase)val2).TryCast<GameItem>() : null);
								if (val3 == null)
								{
									continue;
								}
								string identifier = val3.identifier;
								if (!string.IsNullOrWhiteSpace(identifier) && hashSet.Add(identifier))
								{
									num++;
									ItemEntry itemEntry2 = FindEntryByIdentifier(identifier);
									if (itemEntry2 == null)
									{
										itemEntry2 = new ItemEntry
										{
											Id = identifier,
											English = identifier,
											Chinese = string.Empty,
											Aliases = new List<string> { identifier }
										};
										_catalog.Add(itemEntry2);
										num2++;
									}
									itemEntry2.ResolvedId = identifier;
									itemEntry2.IsAvailable = true;
									PrepareEntry(itemEntry2);
								}
							}
						}
					}
				}
				if (num != 0)
				{
					_runtimeCatalogMerged = true;
					_catalogLoaded = _catalog.Count > 0;
					SortCatalog();
					MelonLogger.Msg("Probably Stolen Item Manager: 已从 " + num3 + " 个运行时目录读取 " + num + " 个物品，补充 " + num2 + " 条未收录标识。");
				}
			}
			catch (Exception ex2)
			{
				LogUiFailure("runtime-catalog", ex2);
			}
		}

		private void ValidateCatalogChunk()
		{
			_nextValidationTime = Time.realtimeSinceStartup + 0.25f;
			if (!IsGameplayContextReady())
			{
				return;
			}
			MergeRuntimeCatalog();
			if (_catalogValidationComplete || _catalog.Count == 0 || (!_runtimeCatalogMerged && !HasAnyKnownCatalogEntry()))
			{
				return;
			}
			_catalogValidationStarted = true;
			int num = Math.Min(_validationIndex + 32, _catalog.Count);
			while (_validationIndex < num)
			{
				ItemEntry itemEntry = _catalog[_validationIndex];
				if (itemEntry.IsGenerated)
				{
					itemEntry.IsAvailable = true;
				}
				else
				{
					foreach (string item in itemEntry.Aliases ?? new List<string> { itemEntry.Id })
					{
						try
						{
							if (DirectoryMaster.Has<GameItem>(item))
							{
								itemEntry.ResolvedId = item;
								itemEntry.IsAvailable = true;
								break;
							}
						}
						catch
						{
						}
					}
				}
				_validationIndex++;
			}
			if (_validationIndex >= _catalog.Count)
			{
				_catalogValidationComplete = true;
				MelonLogger.Msg("Probably Stolen Item Manager: 已验证可用物品 " + _catalog.Count((ItemEntry x) => x.IsAvailable) + "/" + _catalog.Count + "。" + (_catalogValidationStarted ? string.Empty : "目录尚未初始化。"));
			}
		}

		private bool HasAnyKnownCatalogEntry()
		{
			int num = Math.Min(_catalog.Count, 32);
			for (int i = 0; i < num; i++)
			{
				ItemEntry itemEntry = _catalog[i];
				if (itemEntry.IsGenerated)
				{
					continue;
				}
				foreach (string item in itemEntry.Aliases ?? new List<string> { itemEntry.Id })
				{
					try
					{
						if (DirectoryMaster.Has<GameItem>(item))
						{
							return true;
						}
					}
					catch
					{
					}
				}
			}
			return false;
		}

		private void EnsureWindowRect()
		{
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			float num = Math.Min(1200f, Math.Max(1040f, (float)Screen.width * 0.8f));
			float num2 = Math.Min(840f, Math.Max(820f, (float)Screen.height * 0.8f));
			float max = Math.Max(1040f, (float)Screen.width - 24f);
			float max2 = Math.Max(820f, (float)Screen.height - 24f);
			if (_windowRect.width <= 0f || _windowRect.height <= 0f)
			{
				_windowRect = new Rect(((float)Screen.width - num) / 2f, ((float)Screen.height - num2) / 2f, num, num2);
				return;
			}
			_windowRect.width = Math.Clamp(_windowRect.width, 1040f, max);
			_windowRect.height = Math.Clamp(_windowRect.height, 820f, max2);
			_windowRect.x = Math.Clamp(_windowRect.x, 0f, Math.Max(0f, (float)Screen.width - _windowRect.width));
			_windowRect.y = Math.Clamp(_windowRect.y, 0f, Math.Max(0f, (float)Screen.height - _windowRect.height));
		}

		private void SafeLabel(Rect rect, string text, string stage)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (_labelStyle != null)
				{
					GUI.Label(rect, text ?? string.Empty, _labelStyle);
				}
				else
				{
					GUI.Label(rect, text ?? string.Empty);
				}
			}
			catch (Exception ex)
			{
				LogUiFailure(stage, ex);
			}
		}

		private bool SafeButton(Rect rect, string text, string stage)
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (_buttonStyle != null)
				{
					GUI.Button(rect, text ?? string.Empty, _buttonStyle);
				}
				else
				{
					GUI.Button(rect, text ?? string.Empty);
				}
				return false;
			}
			catch (Exception ex)
			{
				LogUiFailure(stage, ex);
				return false;
			}
		}

		private void LogUiFailure(string stage, Exception ex)
		{
			if (!(_lastUiFailure == stage))
			{
				_lastUiFailure = stage;
				MelonLogger.Error("Probably Stolen Item Manager UI " + stage + " 失败：" + ex);
			}
		}

		public ItemManager()
		{
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
		}
	}
}
