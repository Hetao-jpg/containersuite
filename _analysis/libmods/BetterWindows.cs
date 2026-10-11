using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using BetterWindows;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MelonLoader.Preferences;
using MelonLoader.Utils;
using Microsoft.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: MelonInfo(typeof(PinMod), "更好的窗口-公测版", "1.1.0fix", "ButterLab-AlkaidNG", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("BetterWindows")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("BetterWindows")]
[assembly: AssemblyTitle("BetterWindows")]
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
namespace BetterWindows
{
	internal static class PinConfig
	{
		private const string CategoryId = "BetterWindows";

		private const string CategoryName = "更好的窗口";

		private static MelonPreferences_Entry<bool> _enabled;

		private static MelonPreferences_Entry<bool> _showIcon;

		private static MelonPreferences_Entry<bool> _respectImportant;

		private static MelonPreferences_Entry<bool> _forceToFront;

		private static MelonPreferences_Entry<string> _modifierKey;

		private static MelonPreferences_Entry<string> _actionKey;

		private static MelonPreferences_Entry<bool> _clickToCycle;

		private static MelonPreferences_Entry<int> _maxPriority;

		private static MelonPreferences_Entry<int> _clickPadding;

		private static MelonPreferences_Entry<bool> _clickAnchor;

		private static MelonPreferences_Entry<bool> _suspendMarkImportant;

		private static MelonPreferences_Entry<bool> _suspendLockWindow;

		private static MelonPreferences_Entry<bool> _suspendRaiseWindowCap;

		private static MelonPreferences_Entry<int> _suspendCapHeadroom;

		private static MelonPreferences_Entry<bool> _colorButton;

		private static MelonPreferences_Entry<int> _colorIntensity;

		private static MelonPreferences_Entry<bool> _showOpacitySlider;

		private static MelonPreferences_Entry<int> _opacityMin;

		private static MelonPreferences_Entry<bool> _gridSnap;

		private static MelonPreferences_Entry<int> _gridSize;

		private static MelonPreferences_Entry<bool> _followItem;

		private static MelonPreferences_Entry<bool> _keepWindowInGameArea;

		private static MelonPreferences_Entry<bool> _rememberFollowPositions;

		private static MelonPreferences_Entry<bool> _reopenFollowedWindow;

		private static MelonPreferences_Entry<bool> _closeWhenOwnerGone;

		private static MelonPreferences_Entry<bool> _rememberPinStates;

		private static MelonPreferences_Entry<bool> _verbose;

		internal static bool Enabled = true;

		internal static bool ShowIcon = true;

		internal static bool RespectImportant;

		internal static bool ForceToFront;

		internal static bool ClickToCycle = true;

		internal static int MaxPriority = 5;

		internal static int ClickPadding = 3;

		internal static bool ClickAnchor = true;

		internal static bool SuspendMarkImportant;

		internal static bool SuspendLockWindow;

		internal static bool SuspendRaiseWindowCap = true;

		internal static int SuspendCapHeadroom = 24;

		internal static bool ColorButton = true;

		internal static int ColorIntensity = 100;

		internal static bool ShowOpacitySlider = true;

		internal static int OpacityMin = 20;

		internal static bool GridSnap = true;

		internal static int GridSize = 8;

		internal static bool FollowItem = true;

		internal static bool KeepWindowInGameArea = true;

		internal static bool RememberFollowPositions;

		internal static bool ReopenFollowedWindow = true;

		internal static bool CloseWhenOwnerGone = true;

		internal static bool RememberPinStates = true;

		internal static KeyCode Modifier = (KeyCode)306;

		internal static KeyCode Action = (KeyCode)112;

		internal static string HotkeyText()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			return (((int)Modifier == 0) ? "" : (((object)Modifier/*cast due to constrained. prefix*/).ToString() + " + ")) + ((object)Action/*cast due to constrained. prefix*/).ToString();
		}

		internal static void Init()
		{
			//IL_04f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fa: Unknown result type (might be due to invalid IL or missing references)
			//IL_0511: Unknown result type (might be due to invalid IL or missing references)
			//IL_0516: Unknown result type (might be due to invalid IL or missing references)
			//IL_0522: Unknown result type (might be due to invalid IL or missing references)
			//IL_0528: Invalid comparison between Unknown and I4
			try
			{
				MelonPreferences_Category category = MelonPreferences.CreateCategory("BetterWindows", "更好的窗口");
				_enabled = Entry(category, "Enabled", fallback: true, "启用", "总开关。关闭后不显示图钉图标，快捷键也失效。");
				_showIcon = Entry(category, "ShowIcon", fallback: true, "显示图钉图标", "在所有带标题栏的浮动窗口右上角（关闭按钮左侧）显示一个图钉图标。");
				_modifierKey = Entry(category, "ModifierKey", "LeftControl", "快捷键-修饰键", "KeyCode 名称，例如 LeftControl / LeftShift / LeftAlt / RightControl；填 None 表示不需要修饰键。");
				_actionKey = Entry(category, "ActionKey", "P", "快捷键-主键", "KeyCode 名称，例如 P / F8 / Home。把鼠标移到目标窗口上，按 修饰键+主键 切换置顶。");
				_clickToCycle = Entry(category, "ClickToCycle", fallback: true, "点击图钉切换优先级", "开启后可以直接左键点击窗口标题栏上的图钉图标来循环优先级：0(不固定)→1→2→3→4→5→0。\n图标上的数字就是当前优先级，数字越大越靠上。");
				_maxPriority = Entry(category, "MaxPriority", 5, "最高优先级", "优先级循环的上限，范围 1-9，默认 5。");
				_clickPadding = Entry(category, "ClickPadding", 3, "图钉点击外扩像素", "图钉图标很小，这里把点击判定向外扩几像素，避免点不中。设 0 就是严格贴着图标。");
				_clickAnchor = Entry(category, "ClickAnchor", fallback: true, "点击锚形按钮切换挂起", "开启后可以直接左键点击窗口标题栏上的锚形图标，切换\"挂起\"：挂起后窗口不会被游戏自动关闭/自动隐藏。");
				_suspendMarkImportant = Entry(category, "SuspendMarkImportantOptIn", fallback: false, "挂起时标记为重要 (慎用)", "把窗口的 isImportant 设为 true。这是游戏自己的\"不要自动藏起来\"标记\n（WindowsHandler.HideAllNonImportantMovable 会跳过这类窗口）。\n? 默认关闭。挂起默认只靠\"提高窗口上限\"生效，不需要动窗口本身。\n这个标志会残留在窗口对象上，而机器窗口是复用的单例，\n开启后可能造成\"关掉某个机器窗口后再也无法打开\"。只在上限方案无效时才开。");
				_suspendLockWindow = Entry(category, "SuspendLockWindowOptIn", fallback: false, "挂起时同时上锁 (慎用)", "把窗口的 isLocked 设为 true。游戏的自动关闭筛选条件无法从代码里确认，\n上锁是候选条件之一。\n? 默认关闭。副作用可能是该窗口不能再拖动，且同样会残留、干扰游戏自己的关闭流程。");
				_suspendRaiseWindowCap = Entry(category, "SuspendRaiseWindowCap", fallback: true, "挂起时提高窗口上限", "只要还有窗口处于挂起状态，就把 WindowsHandler._maxVisibleFloatingWindows 临时调大，\n从根源上避免\"窗口太多所以关掉一个\"。取消所有挂起后会恢复原值。");
				_suspendCapHeadroom = Entry(category, "SuspendCapHeadroom", 24, "窗口上限追加数量", "配合上一项使用：在原上限基础上追加这么多，范围 1-200。");
				_colorButton = Entry(category, "ColorButton", fallback: true, "显示配色按钮", "在标题栏再加一个水滴图标：左键点击循环窗口配色（原色→暖棕→冷蓝→青绿→麦黄→淡紫→玫红→灰白→原色）。\n每个窗口的颜色互相独立。");
				_colorIntensity = Entry(category, "ColorIntensity", 100, "配色强度(%)", "着色的浓淡。0 = 完全不上色（等于原色），100 = 预设色的完整效果，范围 0-200。\n如果原版窗口本来就偏亮，觉得颜色不够明显就调大。");
				_showOpacitySlider = Entry(category, "ShowOpacitySlider", fallback: true, "显示透明度滑块", "在标题栏最左边显示一条透明度滑块：按住左右拖动可以调节这个窗口的不透明度。\n透明度会随窗口一起记忆（如果 RememberPinStates 开着）。\n松手后生效，滑块上蓝条的长度就是当前不透明度。");
				_opacityMin = Entry(category, "OpacityMin", 20, "透明度最小值(%)", "滑块能拖到的最小不透明度，范围 5-90。\n这是为了防止不小心把窗口拖到几乎看不见、再也调不回来。");
				_respectImportant = Entry(category, "RespectImportantWindows", fallback: false, "不压住重要窗口", "默认关闭，被固定的窗口一定会压到最前面（符合\"保证在最上面\"的预期）。\n如果发现被固定的窗口挡住了必须点击的弹窗，把它改成 true：此时当最前面的窗口是重要窗口\n（游戏内 isImportant 标记，例如弹窗/日志）时，被固定的窗口不会强行压上去。");
				_forceToFront = Entry(category, "ForceToFront", fallback: false, "强制置顶(ToFront force)", "传给游戏 ToFront(bool force) 的参数。默认 false；如果置顶不生效可以改成 true 试试。");
				_gridSnap = Entry(category, "GridSnap", fallback: true, "窗口对齐到网格", "拖动结束松手时，把窗口位置吸附到整数网格上，让多个窗口排得更整齐。\n游戏本身只吸附到 1 像素，这个选项是更粗的对齐。");
				_gridSize = Entry(category, "GridSize", 8, "网格大小(像素)", "配合上一项使用：吸附的格子边长，范围 1-128。\n1 = 等同游戏原版行为；8 比较自然；16 格子感更强。");
				_followItem = Entry(category, "FollowItemWindow", fallback: true, "窗口跟随所属物品", "让机器窗口跟着它所属的物品一起移动（例如滤水器的滤芯窗口跟着滤水器走）。\n只在游戏确实记录了\"这个窗口属于哪个物品\"时生效，否则该窗口保持原样。\n\n窗口不会被自动摆位：打开时保持在游戏给它的位置，你把它拖到想要的地方一次，\n之后它就一直按这个相对位置跟着物品走（关掉再打开也会回到那个位置）。");
				_reopenFollowedWindow = Entry(category, "ReopenFollowedWindow", fallback: true, "自动重开被关闭的跟随窗口", "拿起物品时，游戏会主动关掉它的容器窗口（这是游戏自己的行为，模组无法阻止）。\n开启这一项后，松手时会把这类窗口重新打开。\n只重开\"本来在跟随、并且在拖动过程中被关掉\"的窗口，\n玩家自己手动关掉的窗口不会被重开。");
				_closeWhenOwnerGone = Entry(category, "CloseWhenOwnerGone", fallback: true, "物品销毁时关闭窗口", "跟随物品的窗口，如果所属物品被销毁了（例如被丢进垃圾桶），这个窗口也会跟着关闭。\n只在窗口确实跟随过一个物品、并且连续约 0.5 秒读不到物品时才会触发，\n短暂读不到（动画中、被隐藏等）不会误关。\n关掉这一项则保持窗口原样。");
				_rememberPinStates = Entry(category, "RememberPinStates", fallback: true, "记住图钉、配色和透明度(跨重启)", "把你设置过的图钉优先级、窗口配色、不透明度记到 UserData/BetterWindows_pins.txt，\n下次开游戏自动恢复 —— 不用每个窗口每次都重新点一遍。\n\n存档以\"窗口标题\"为键：同一个窗口（同名）永远对应同一份设置。\n想清空记忆就把那个文件删掉，或者删掉其中某一行来忘掉那个窗口。");
				_keepWindowInGameArea = Entry(category, "KeepWindowInGameArea", fallback: true, "窗口不越出游戏画面", "跟随写入的位置会被夹在游戏真正画出来的区域里（有黑边时就是黑边以内），\n所以窗口不会跑到画面外面去挡住界面。\n如果你就是想把窗口停在黑边上，把它改成 false：\n此时只保证不越出整个游戏窗口。");
				_rememberFollowPositions = Entry(category, "RememberFollowPositions", fallback: false, "记住跟随位置(跨存档)", "开启后，你拖好的跟随位置会存到 UserData/BetterWindows_follow.txt，\n下次开游戏自动套用 —— 不用每台机器每次开游戏都重拖一遍。\n存档以\"物品的唯一 ID\"为键（游戏自己也是用这个 ID 记窗口位置的），\n所以同一台机器永远对应同一个位置。\n\n? 默认关闭：存下来的是屏幕像素偏移，改了分辨率或 UI 缩放之后可能偏；\n那时候重新拖一次就行，新位置会覆盖旧记录。\n想清空记忆就把那个文件删掉。");
				_verbose = Entry(category, "VerboseLog", fallback: false, "详细日志", "输出调试信息，排查问题时打开。");
			}
			catch (Exception ex)
			{
				PinLog.Warn("读取配置失败，使用默认值运行: " + ex.Message);
			}
			Enabled = Read(_enabled, fallback: true);
			ShowIcon = Read(_showIcon, fallback: true);
			RespectImportant = Read(_respectImportant, fallback: false);
			ForceToFront = Read(_forceToFront, fallback: false);
			ClickToCycle = Read(_clickToCycle, fallback: true);
			MaxPriority = Clamp(Read(_maxPriority, 5), 1, 9);
			ClickPadding = Clamp(Read(_clickPadding, 3), 0, 24);
			ClickAnchor = Read(_clickAnchor, fallback: true);
			SuspendMarkImportant = Read(_suspendMarkImportant, fallback: false);
			SuspendLockWindow = Read(_suspendLockWindow, fallback: false);
			SuspendRaiseWindowCap = Read(_suspendRaiseWindowCap, fallback: true);
			SuspendCapHeadroom = Clamp(Read(_suspendCapHeadroom, 24), 1, 200);
			ColorButton = Read(_colorButton, fallback: true);
			ColorIntensity = Clamp(Read(_colorIntensity, 100), 0, 200);
			ShowOpacitySlider = Read(_showOpacitySlider, fallback: true);
			OpacityMin = Clamp(Read(_opacityMin, 20), 5, 90);
			GridSnap = Read(_gridSnap, fallback: true);
			GridSize = Clamp(Read(_gridSize, 8), 1, 128);
			FollowItem = Read(_followItem, fallback: true);
			KeepWindowInGameArea = Read(_keepWindowInGameArea, fallback: true);
			RememberFollowPositions = Read(_rememberFollowPositions, fallback: false);
			ReopenFollowedWindow = Read(_reopenFollowedWindow, fallback: true);
			CloseWhenOwnerGone = Read(_closeWhenOwnerGone, fallback: true);
			RememberPinStates = Read(_rememberPinStates, fallback: true);
			PinLog.Verbose = Read(_verbose, fallback: false);
			Modifier = ParseKey(Read(_modifierKey, "LeftControl"), (KeyCode)306, allowNone: true);
			Action = ParseKey(Read(_actionKey, "P"), (KeyCode)112, allowNone: false);
			if (Enabled && (int)Action == 0)
			{
				PinLog.Warn("主键解析失败，快捷键已禁用（图钉图标仍会显示）。请检查配置 ActionKey。");
			}
		}

		private static bool Read(MelonPreferences_Entry<bool> entry, bool fallback)
		{
			try
			{
				return entry?.Value ?? fallback;
			}
			catch
			{
				return fallback;
			}
		}

		private static int Read(MelonPreferences_Entry<int> entry, int fallback)
		{
			try
			{
				return entry?.Value ?? fallback;
			}
			catch
			{
				return fallback;
			}
		}

		private static int Clamp(int value, int min, int max)
		{
			if (value < min)
			{
				return min;
			}
			if (value > max)
			{
				return max;
			}
			return value;
		}

		private static string Read(MelonPreferences_Entry<string> entry, string fallback)
		{
			try
			{
				if (entry == null || string.IsNullOrEmpty(entry.Value))
				{
					return fallback;
				}
				return entry.Value;
			}
			catch
			{
				return fallback;
			}
		}

		private static KeyCode ParseKey(string text, KeyCode fallback, bool allowNone)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			if (string.IsNullOrEmpty(text))
			{
				return fallback;
			}
			string text2 = text.Trim();
			if (allowNone && text2.Equals("None", StringComparison.OrdinalIgnoreCase))
			{
				return (KeyCode)0;
			}
			try
			{
				if (Enum.TryParse<KeyCode>(text2, true, out KeyCode result))
				{
					return result;
				}
			}
			catch
			{
			}
			PinLog.Warn("无法识别的按键 \"" + text + "\"，改用默认值 " + ((object)fallback/*cast due to constrained. prefix*/).ToString() + "。");
			return fallback;
		}

		private static MelonPreferences_Entry<T> Entry<T>(MelonPreferences_Category category, string id, T fallback, string display, string description)
		{
			MelonPreferences_Entry<T> val = category.GetEntry(id) as MelonPreferences_Entry<T>;
			if (val == null)
			{
				val = category.CreateEntry<T>(id, fallback, display, description, false, false, (ValueValidator)null, (string)null);
			}
			return val;
		}

		static PinConfig()
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	internal static class PinLog
	{
		private const int ErrorBudget = 25;

		private static int _errors;

		internal static bool Verbose;

		internal static void Info(string message)
		{
			try
			{
				MelonLogger.Msg("[更好的窗口] " + message);
			}
			catch
			{
			}
		}

		internal static void Warn(string message)
		{
			try
			{
				MelonLogger.Warning("[更好的窗口] " + message);
			}
			catch
			{
			}
		}

		internal static void Error(string message)
		{
			try
			{
				MelonLogger.Error("[更好的窗口] " + message);
			}
			catch
			{
			}
		}

		internal static void ErrorOnce(string message)
		{
			if (_errors < 25)
			{
				_errors++;
				Error(message);
				if (_errors == 25)
				{
					Error("错误过多，后续同类错误不再输出。");
				}
			}
		}

		internal static void Dev(string message)
		{
			if (Verbose)
			{
				Info(message);
			}
		}
	}
	public class PinMod : MelonMod
	{
		private sealed class FollowPlacement
		{
			internal Vector2 Offset;

			internal int ScreenW;

			internal int ScreenH;

			internal string Name;
		}

		private sealed class WindowMemory
		{
			internal int Priority;

			internal int ColorIndex;

			internal int Opacity = 100;
		}

		private const string PinIconName = "BetterWindows_Pin";

		private const string AnchorIconName = "BetterWindows_Anchor";

		private const string PaletteIconName = "BetterWindows_Palette";

		private const string OpacitySliderName = "BetterWindows_OpacitySlider";

		private const int AnchorSpriteKey = 100;

		private const int PaletteSpriteKey = 200;

		private const float StackInterval = 0.15f;

		private const int StackPassWarnAfter = 40;

		private const int MaxStates = 200;

		private const int MaxReopenPending = 8;

		private const int OwnerGoneCloseFrames = 30;

		private const int OwnerGoneMinFrames = 90;

		private const float DoubleClickWindow = 0.35f;

		private const float DragMovePixels = 6f;

		private const int RevealSnapFrames = 10;

		private const float SliderDragPixels = 4f;

		private static readonly Vector2[] HitOffsets = new Vector2[8]
		{
			new Vector2(-1f, -1f),
			new Vector2(0f, -1f),
			new Vector2(1f, -1f),
			new Vector2(-1f, 0f),
			new Vector2(1f, 0f),
			new Vector2(-1f, 1f),
			new Vector2(0f, 1f),
			new Vector2(1f, 1f)
		};

		private static readonly Color[] ColorPresets = new Color[16]
		{
			new Color(1f, 1f, 1f, 1f),
			new Color(2f, 1.9f, 0.1f, 1f),
			new Color(0.1f, 0.7f, 2f, 1f),
			new Color(2f, 0.15f, 0.15f, 1f),
			new Color(0.1f, 1.8f, 0.3f, 1f),
			new Color(1.5f, 0.1f, 1.8f, 1f),
			new Color(2f, 0.7f, 0.05f, 1f),
			new Color(2f, 0.2f, 1f, 1f),
			new Color(1f, 2f, 0.1f, 1f),
			new Color(0.1f, 1.8f, 1.8f, 1f),
			new Color(2f, 0.1f, 0.8f, 1f),
			new Color(0.1f, 0.3f, 1.8f, 1f),
			new Color(2f, 0.6f, 0.4f, 1f),
			new Color(0.5f, 1.4f, 0.2f, 1f),
			new Color(2f, 0.3f, 0.6f, 1f),
			new Color(1.2f, 1.2f, 1.4f, 1f)
		};

		private static readonly string[] ColorPresetNames = new string[16]
		{
			"原色", "明黄", "天蓝", "亮红", "亮绿", "亮紫", "亮橙", "亮粉", "青柠", "青蓝",
			"品红", "深蓝", "珊瑚", "苔绿", "玫红", "银灰"
		};

		private readonly Dictionary<IntPtr, PinState> _states = new Dictionary<IntPtr, PinState>();

		private readonly List<PixelWindow> _wins = new List<PixelWindow>();

		private readonly List<PixelWindow> _allWins = new List<PixelWindow>();

		private HashSet<IntPtr> _seenCurrent = new HashSet<IntPtr>();

		private HashSet<IntPtr> _seenPrevious = new HashSet<IntPtr>();

		private readonly List<PinState> _reopenPending = new List<PinState>();

		private bool _wasDraggingAny;

		private readonly List<PixelWindow> _pinned = new List<PixelWindow>();

		private int _sequenceCounter;

		private float _lastStackPass = -999f;

		private int _stackPasses;

		private bool _stackWarningIssued;

		private int _capTarget = -1;

		private bool _wasDragging;

		private PinState _sliderDragOwner;

		private Vector2 _sliderDragWindowPos;

		private bool _hasSliderDragWindowPos;

		private Vector2 _sliderPressPos;

		private bool _sliderDragging;

		private float _lastMouseDownTime = -999f;

		private float _suppressReopenUntil = -999f;

		private float _mouseDownTime = -999f;

		private Vector2 _pressMousePos;

		private bool _pressMovedFar;

		private bool _pressDraggedUI;

		private readonly Dictionary<int, FollowPlacement> _followPlacements = new Dictionary<int, FollowPlacement>();

		private const int MaxFollowPlacements = 256;

		private bool _placementsDirty;

		private float _lastPlacementFlush = -999f;

		private readonly Dictionary<string, WindowMemory> _windowMemories = new Dictionary<string, WindowMemory>();

		private bool _pinMemoryDirty;

		private float _lastPinMemoryFlush = -999f;

		private const int MaxWindowMemories = 256;

		private const int ExtentSearchDepth = 3;

		private const int MaxExtentNodes = 64;

		private const float ExtentSanityFactor = 4f;

		private const int WindowExtentRefreshFrames = 90;

		private const int AnchorSearchDepth = 3;

		private const int MaxAnchorNodes = 64;

		public override void OnInitializeMelon()
		{
			try
			{
				PinConfig.Init();
				LoadPlacements();
				LoadPinMemory();
				PinSprites.Warm();
				PinLog.Info("更好的窗口 v1.1.0fix 已加载");
				PinLog.Info("用法：标题栏上有三个图标 —— 图钉（优先级 0→1→2→3→4→5→0，数字越大越靠上）、锚（挂起，不会被游戏自动关闭）、水滴（循环窗口配色）；最左边还有一条透明度滑块（按住左右拖）。左键点击即可切换。");
				PinLog.Info("快捷键 " + PinConfig.HotkeyText() + " 等于点图钉（切换优先级）。");
				if (PinConfig.SuspendMarkImportant || PinConfig.SuspendLockWindow)
				{
					PinLog.Warn("挂起的高级选项处于开启状态（SuspendMarkImportantOptIn=" + PinConfig.SuspendMarkImportant + ", SuspendLockWindowOptIn=" + PinConfig.SuspendLockWindow + "）。");
					PinLog.Warn("这两个标志会写在窗口对象上，而机器窗口是复用的单例，可能导致「关掉某个机器窗口后无法再次打开」。");
					PinLog.Warn("如果遇到这个问题，请把 UserData/MelonPreferences.cfg 里 [BetterWindows] 段的 SuspendMarkImportantOptIn 和 SuspendLockWindowOptIn 改成 false（挂起仍然靠窗口上限生效，不需要它们）。");
				}
			}
			catch (Exception ex)
			{
				PinLog.Error("初始化失败: " + ex);
			}
		}

		public override void OnUpdate()
		{
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Unknown result type (might be due to invalid IL or missing references)
			if (!PinConfig.Enabled)
			{
				return;
			}
			try
			{
				float unscaledTime = Time.unscaledTime;
				if (Input.GetMouseButtonDown(0))
				{
					if (_reopenPending.Count > 0)
					{
						_reopenPending.Clear();
					}
					if (unscaledTime - _lastMouseDownTime < 0.35f)
					{
						_suppressReopenUntil = unscaledTime + 0.35f;
					}
					_lastMouseDownTime = unscaledTime;
					_mouseDownTime = unscaledTime;
					_pressMovedFar = false;
					_pressDraggedUI = false;
					try
					{
						_pressMousePos = Vector2.op_Implicit(Input.mousePosition);
					}
					catch
					{
					}
				}
				if (MouseHeld())
				{
					if (DraggingAnyUI())
					{
						_pressDraggedUI = true;
					}
					if (!_pressMovedFar)
					{
						try
						{
							Vector2 val = Vector2.op_Implicit(Input.mousePosition);
							if (Mathf.Abs(val.x - _pressMousePos.x) >= 6f || Mathf.Abs(val.y - _pressMousePos.y) >= 6f)
							{
								_pressMovedFar = true;
							}
						}
						catch
						{
						}
					}
				}
				bool wasDraggingAny = DraggingAnyUI();
				_wasDraggingAny = wasDraggingAny;
				CollectWindows();
				PruneStates();
				if (_reopenPending.Count > 0 && !MouseHeld())
				{
					FlushReopen();
				}
				if (PinConfig.ShowIcon)
				{
					EnsureIcons();
				}
				else
				{
					RemoveAllIcons();
				}
				HandleMouse();
				HandleHotkey();
				ApplyWindowCap();
				ApplyFollow();
				ApplyGridSnap();
				ApplyStacking();
				if (_placementsDirty && (Time.unscaledTime - _lastPlacementFlush >= 1f || !MouseHeld()))
				{
					FlushPlacements();
				}
				if (_pinMemoryDirty && (Time.unscaledTime - _lastPinMemoryFlush >= 1f || !MouseHeld()))
				{
					FlushPinMemory();
				}
			}
			catch (Exception ex)
			{
				PinLog.ErrorOnce("OnUpdate 异常: " + ex);
			}
		}

		public override void OnLateUpdate()
		{
			//IL_0055: Unknown result type (might be due to invalid IL or missing references)
			if (PinConfig.Enabled)
			{
				SnapRevealedFollowedWindows();
			}
			if (_sliderDragOwner != null && _sliderDragging && MouseHeld() && _hasSliderDragWindowPos)
			{
				PinWindowInPlace(_sliderDragOwner, _sliderDragWindowPos);
			}
		}

		private void SnapRevealedFollowedWindows()
		{
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			//IL_0125: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_012f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			if (!PinConfig.FollowItem || MouseHeld() || DraggingAnyUI())
			{
				return;
			}
			for (int i = 0; i < _allWins.Count; i++)
			{
				try
				{
					PixelWindow val = _allWins[i];
					if (!_states.TryGetValue(((Il2CppObjectBase)val).Pointer, out var value) || !value.HasFollowOffset || (value.WasVisibleLastFrame && value.FramesAlive > 10) || !WindowActive(val))
					{
						continue;
					}
					RectTransform rectTransform = val.rectTransform;
					if ((Object)(object)rectTransform == (Object)null)
					{
						continue;
					}
					RectTransform val2 = OwnerRect(val, out var _, out var _, out var _);
					if (!((Object)(object)val2 == (Object)null))
					{
						RectTransform followAnchorNode = value.FollowAnchorNode;
						Vector2 val3;
						try
						{
							val3 = Vector2.op_Implicit(((Object)(object)followAnchorNode != (Object)null) ? ((Transform)followAnchorNode).position : ((Transform)val2).position);
						}
						catch
						{
							goto end_IL_0032;
						}
						Vector2 targetWorld = ClampWindowToScreen(value, rectTransform, val, val3 + value.FollowOffset);
						MoveWindowTowards(rectTransform, targetWorld, Vector2.op_Implicit(((Transform)rectTransform).position));
					}
					end_IL_0032:;
				}
				catch
				{
				}
			}
		}

		private void CollectWindows()
		{
			_wins.Clear();
			_allWins.Clear();
			HashSet<IntPtr> seenPrevious = _seenPrevious;
			_seenPrevious = _seenCurrent;
			_seenCurrent = seenPrevious;
			_seenCurrent.Clear();
			try
			{
				WindowsHandler current = WindowsHandler.current;
				if ((Object)(object)current != (Object)null)
				{
					List<PixelWindow> visibleWindows = current.visibleWindows;
					if (visibleWindows != null)
					{
						for (int i = 0; i < visibleWindows.Count; i++)
						{
							AddCandidate(visibleWindows[i]);
						}
					}
				}
			}
			catch (Exception ex)
			{
				PinLog.Dev("读取 WindowsHandler.visibleWindows 失败: " + ex.Message);
			}
			try
			{
				List<PixelWindow> dockedFloating = PixelWindow.dockedFloating;
				if (dockedFloating != null)
				{
					for (int j = 0; j < dockedFloating.Count; j++)
					{
						AddCandidate(dockedFloating[j]);
					}
				}
			}
			catch (Exception ex2)
			{
				PinLog.Dev("读取 PixelWindow.dockedFloating 失败: " + ex2.Message);
			}
		}

		private void AddCandidate(PixelWindow window)
		{
			if (window == null)
			{
				return;
			}
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)window).Pointer;
				_seenCurrent.Add(pointer);
				bool flag = false;
				for (int i = 0; i < _allWins.Count; i++)
				{
					if (((Il2CppObjectBase)_allWins[i]).Pointer == pointer)
					{
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					_allWins.Add(window);
				}
				if (!window.hasTitle)
				{
					return;
				}
				TreeNodeRender topCloseButton = window.topCloseButton;
				if ((Object)(object)topCloseButton == (Object)null || (Object)(object)((Component)topCloseButton).gameObject == (Object)null)
				{
					return;
				}
				for (int j = 0; j < _wins.Count; j++)
				{
					if (((Il2CppObjectBase)_wins[j]).Pointer == pointer)
					{
						return;
					}
				}
				_wins.Add(window);
			}
			catch (Exception ex)
			{
				PinLog.Dev("检查窗口失败: " + ex.Message);
			}
		}

		private PinState EnsureState(PixelWindow window)
		{
			PinState orCreateState = GetOrCreateState(window);
			if (!HasLiveButtons(orCreateState))
			{
				CreateButtons(orCreateState);
			}
			return orCreateState;
		}

		private PinState GetOrCreateState(PixelWindow window)
		{
			IntPtr pointer = ((Il2CppObjectBase)window).Pointer;
			if (!_states.TryGetValue(pointer, out var value))
			{
				value = new PinState();
				_states[pointer] = value;
				ApplyPinMemory(window, value);
			}
			value.Window = window;
			return value;
		}

		private static bool HasLiveButtons(PinState state)
		{
			return state.Pin != null && state.Pin.Alive && state.Anchor != null && state.Anchor.Alive && (!PinConfig.ColorButton || (state.Palette != null && state.Palette.Alive)) && (!PinConfig.ShowOpacitySlider || (state.Opacity != null && state.Opacity.Alive));
		}

		private void PruneStates()
		{
			if (_states.Count == 0)
			{
				return;
			}
			if (_states.Count > 200)
			{
				PinLog.Warn("窗口状态表异常增长，已重置（图钉和挂起状态会丢失一次）。");
				RemoveAllIcons();
				return;
			}
			List<IntPtr> list = null;
			foreach (KeyValuePair<IntPtr, PinState> state2 in _states)
			{
				bool flag = false;
				PixelWindow val = null;
				for (int i = 0; i < _allWins.Count; i++)
				{
					if (((Il2CppObjectBase)_allWins[i]).Pointer == state2.Key)
					{
						flag = true;
						val = _allWins[i];
						break;
					}
				}
				if (flag && val != null)
				{
					try
					{
						RectTransform rectTransform = val.rectTransform;
						if ((Object)(object)rectTransform != (Object)null && (Object)(object)((Component)rectTransform).gameObject != (Object)null && !((Component)rectTransform).gameObject.activeSelf)
						{
							flag = false;
						}
					}
					catch
					{
					}
				}
				if (!flag && state2.Value.Suspended)
				{
					PinLog.Dev("窗口「" + Title(state2.Value.Window) + "」已不可见，自动解除挂起标志。");
					RestoreSuspend(state2.Value);
				}
				bool wasVisibleLastFrame = state2.Value.WasVisibleLastFrame;
				if (wasVisibleLastFrame && !flag)
				{
					PinLog.Info("「" + Title(state2.Value.Window) + "」从可见变为不可见（HasFollowOffset=" + state2.Value.HasFollowOffset + ", Following=" + state2.Value.Following + ", MouseHeld=" + MouseHeld() + ", 按住时长=" + (Time.unscaledTime - _mouseDownTime).ToString("0.00") + "s）");
				}
				state2.Value.WasVisibleLastFrame = flag;
				if (flag)
				{
					state2.Value.FramesSinceSeen = 0;
				}
				else
				{
					state2.Value.FramesSinceSeen++;
				}
				if (wasVisibleLastFrame && !flag && state2.Value.HasFollowOffset && PinConfig.ReopenFollowedWindow && MouseHeld() && Time.unscaledTime >= _suppressReopenUntil)
				{
					PinLog.Info("跟随中的窗口「" + Title(state2.Value.Window) + "」被游戏关闭，先排队，松手时确认是拖动才重开。");
					QueueReopen(state2.Value);
				}
				if (!flag && !HasLiveButtons(state2.Value))
				{
					if (list == null)
					{
						list = new List<IntPtr>();
					}
					list.Add(state2.Key);
				}
			}
			if (list != null)
			{
				for (int j = 0; j < list.Count; j++)
				{
					PinState state = _states[list[j]];
					RestoreSuspend(state);
					RestoreWindowColor(state);
					_states.Remove(list[j]);
				}
				PinLog.Dev("清理失效窗口状态 " + list.Count + " 条");
			}
		}

		private void QueueReopen(PinState state)
		{
			if (state == null || state.Window == null || _reopenPending.Count >= 8)
			{
				return;
			}
			for (int i = 0; i < _reopenPending.Count; i++)
			{
				if (_reopenPending[i].Window != null && ((Il2CppObjectBase)_reopenPending[i].Window).Pointer == ((Il2CppObjectBase)state.Window).Pointer)
				{
					return;
				}
			}
			_reopenPending.Add(state);
			PinLog.Dev("「" + Title(state.Window) + "」被游戏关闭，已排队等待松手后确认。");
		}

		private void FlushReopen()
		{
			if (!_pressMovedFar && !_pressDraggedUI)
			{
				if (PinLog.Verbose)
				{
					PinLog.Dev("这次按住没有拖动鼠标，取消重开 " + _reopenPending.Count + " 个窗口。");
				}
				_reopenPending.Clear();
			}
			else
			{
				ReopenClosedFollowedWindows();
			}
		}

		private void ReopenClosedFollowedWindows()
		{
			if (_reopenPending.Count == 0)
			{
				return;
			}
			for (int i = 0; i < _reopenPending.Count; i++)
			{
				PinState pinState = _reopenPending[i];
				PixelWindow window = pinState.Window;
				if (window == null)
				{
					continue;
				}
				if (!OwnerStillAlive(window))
				{
					PinLog.Info("「" + Title(window) + "」的所属物品已不存在，不重开。");
					continue;
				}
				if (WindowActive(window))
				{
					PinLog.Dev("「" + Title(window) + "」已经可见，不需要重开。");
					continue;
				}
				try
				{
					window.Show();
					PinLog.Info("已重新打开「" + Title(window) + "」（它在拖动物品时被游戏关闭了）。");
				}
				catch (Exception ex)
				{
					PinLog.Dev("重开窗口失败: " + ex.Message);
				}
			}
			_reopenPending.Clear();
		}

		private void RemoveAllIcons()
		{
			List<IntPtr> list = null;
			foreach (KeyValuePair<IntPtr, PinState> state in _states)
			{
				PinState value = state.Value;
				if (HasLiveButtons(value))
				{
					DestroyButton(value.Pin);
					DestroyButton(value.Anchor);
					DestroyButton(value.Palette);
					DestroySlider(value.Opacity);
					RestoreSuspend(value);
					RestoreWindowColor(value);
					if (list == null)
					{
						list = new List<IntPtr>();
					}
					list.Add(state.Key);
				}
			}
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					_states.Remove(list[i]);
				}
			}
		}

		private static void DestroyButton(WinButton button)
		{
			if (button == null)
			{
				return;
			}
			GameObject go = button.Go;
			if (!((Object)(object)go == (Object)null))
			{
				try
				{
					Object.Destroy((Object)(object)go);
				}
				catch
				{
				}
				button.Go = null;
				button.Image = null;
				button.Rect = null;
			}
		}

		private static void DestroySlider(WinSlider slider)
		{
			if (slider == null)
			{
				return;
			}
			GameObject root = slider.Root;
			if (!((Object)(object)root == (Object)null))
			{
				try
				{
					Object.Destroy((Object)(object)root);
				}
				catch
				{
				}
				slider.Root = null;
				slider.RootRect = null;
				slider.TrackImage = null;
				slider.FillImage = null;
				slider.FillRect = null;
			}
		}

		private void EnsureIcons()
		{
			for (int i = 0; i < _wins.Count; i++)
			{
				PinState state = EnsureState(_wins[i]);
				RefreshIcons(state);
				ApplyWindowColor(state);
			}
		}

		private bool CreateButtons(PinState state)
		{
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			PixelWindow window = state.Window;
			if (window == null)
			{
				return false;
			}
			TreeNodeRender topCloseButton = window.topCloseButton;
			if ((Object)(object)topCloseButton == (Object)null)
			{
				return false;
			}
			GameObject gameObject = ((Component)topCloseButton).gameObject;
			if ((Object)(object)gameObject == (Object)null)
			{
				return false;
			}
			Transform parent = gameObject.transform.parent;
			if ((Object)(object)parent == (Object)null)
			{
				return false;
			}
			RectTransform rectTransform = topCloseButton.rectTransform;
			if ((Object)(object)rectTransform == (Object)null)
			{
				return false;
			}
			Rect rect = rectTransform.rect;
			float num = rect.width;
			rect = rectTransform.rect;
			float num2 = rect.height;
			if (num < 4f || num2 < 4f)
			{
				num = 16f;
				num2 = 16f;
			}
			int pixels = Mathf.Clamp(Mathf.RoundToInt(Mathf.Max(num, num2)), 8, 128);
			float step = num + 3f;
			bool flag = false;
			if (state.Pin == null || !state.Pin.Alive)
			{
				state.Pin = MakeButton(parent, gameObject, rectTransform, pixels, step, 0, PinSprites.GetPin(pixels, state.Priority), "BetterWindows_Pin");
				if (state.Pin != null)
				{
					flag = true;
				}
			}
			if (state.Anchor == null || !state.Anchor.Alive)
			{
				state.Anchor = MakeButton(parent, gameObject, rectTransform, pixels, step, 1, PinSprites.GetAnchor(pixels), "BetterWindows_Anchor");
				if (state.Anchor != null)
				{
					flag = true;
				}
			}
			if (PinConfig.ColorButton && (state.Palette == null || !state.Palette.Alive))
			{
				state.Palette = MakeButton(parent, gameObject, rectTransform, pixels, step, 2, PinSprites.GetPalette(pixels), "BetterWindows_Palette");
				if (state.Palette != null)
				{
					flag = true;
				}
			}
			if (PinConfig.ShowOpacitySlider && (state.Opacity == null || !state.Opacity.Alive))
			{
				state.Opacity = MakeSlider(parent, gameObject, rectTransform, step, 3, "BetterWindows_OpacitySlider");
				if (state.Opacity != null)
				{
					flag = true;
				}
			}
			if (flag)
			{
				PinLog.Dev("已为窗口「" + Title(window) + "」添加标题栏按钮");
			}
			return HasLiveButtons(state);
		}

		private static WinButton MakeButton(Transform bar, GameObject closeObject, RectTransform closeRect, int pixels, float step, int slot, Sprite sprite, string name)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected Obj, but got Unknown
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)sprite == (Object)null)
			{
				return null;
			}
			GameObject val = new GameObject(name);
			val.layer = closeObject.layer;
			RectTransform val2 = val.AddComponent<RectTransform>();
			((Transform)val2).SetParent(bar, false);
			((Transform)val2).SetAsLastSibling();
			val2.anchorMin = closeRect.anchorMin;
			val2.anchorMax = closeRect.anchorMax;
			val2.pivot = closeRect.pivot;
			val2.sizeDelta = closeRect.sizeDelta;
			((Transform)val2).localScale = ((Transform)closeRect).localScale;
			((Transform)val2).localRotation = ((Transform)closeRect).localRotation;
			Vector2 anchoredPosition = closeRect.anchoredPosition;
			val2.anchoredPosition = new Vector2(anchoredPosition.x - step * (float)(slot + 1), anchoredPosition.y);
			Image val3 = val.AddComponent<Image>();
			val3.sprite = sprite;
			val3.type = (Type)0;
			val3.preserveAspect = false;
			((Graphic)val3).color = Color.white;
			((Graphic)val3).raycastTarget = false;
			WinButton winButton = new WinButton();
			winButton.Go = val;
			winButton.Image = val3;
			winButton.Rect = val2;
			winButton.Size = pixels;
			return winButton;
		}

		private static WinSlider MakeSlider(Transform bar, GameObject closeObject, RectTransform closeRect, float step, int slot, string name)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Expected Obj, but got Unknown
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Expected Obj, but got Unknown
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0141: Unknown result type (might be due to invalid IL or missing references)
			//IL_014e: Unknown result type (might be due to invalid IL or missing references)
			//IL_015b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0185: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ac: Expected Obj, but got Unknown
			//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0207: Unknown result type (might be due to invalid IL or missing references)
			//IL_021e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0235: Unknown result type (might be due to invalid IL or missing references)
			//IL_025f: Unknown result type (might be due to invalid IL or missing references)
			float num = step * 3f;
			float num2 = 8f;
			GameObject val = new GameObject(name);
			val.layer = closeObject.layer;
			RectTransform val2 = val.AddComponent<RectTransform>();
			((Transform)val2).SetParent(bar, false);
			((Transform)val2).SetAsLastSibling();
			val2.anchorMin = closeRect.anchorMin;
			val2.anchorMax = closeRect.anchorMax;
			val2.pivot = new Vector2(1f, 0.5f);
			((Transform)val2).localScale = ((Transform)closeRect).localScale;
			((Transform)val2).localRotation = ((Transform)closeRect).localRotation;
			val2.sizeDelta = new Vector2(num, num2);
			float num3 = closeRect.anchoredPosition.x - step * 3f;
			Rect rect = closeRect.rect;
			float num4 = num3 - rect.width * 0.5f;
			val2.anchoredPosition = new Vector2(num4 - 6f, closeRect.anchoredPosition.y - 3f);
			GameObject val3 = new GameObject(name + "_Track");
			val3.layer = closeObject.layer;
			RectTransform val4 = val3.AddComponent<RectTransform>();
			((Transform)val4).SetParent((Transform)(object)val2, false);
			val4.anchorMin = Vector2.zero;
			val4.anchorMax = Vector2.one;
			val4.pivot = new Vector2(0.5f, 0.5f);
			val4.offsetMin = Vector2.zero;
			val4.offsetMax = Vector2.zero;
			Image val5 = val3.AddComponent<Image>();
			((Graphic)val5).color = new Color(0.16f, 0.18f, 0.24f, 0.85f);
			((Graphic)val5).raycastTarget = false;
			GameObject val6 = new GameObject(name + "_Fill");
			val6.layer = closeObject.layer;
			RectTransform val7 = val6.AddComponent<RectTransform>();
			((Transform)val7).SetParent((Transform)(object)val2, false);
			val7.anchorMin = new Vector2(0f, 0f);
			val7.anchorMax = new Vector2(1f, 1f);
			val7.pivot = new Vector2(0.5f, 0.5f);
			val7.offsetMin = new Vector2(1f, 1f);
			val7.offsetMax = new Vector2(-1f, -1f);
			Image val8 = val6.AddComponent<Image>();
			((Graphic)val8).color = new Color(0.72f, 0.85f, 1f, 1f);
			((Graphic)val8).raycastTarget = false;
			WinSlider winSlider = new WinSlider();
			winSlider.Root = val;
			winSlider.RootRect = val2;
			winSlider.TrackImage = val5;
			winSlider.FillImage = val8;
			winSlider.FillRect = val7;
			winSlider.Size = Mathf.RoundToInt(num2);
			return winSlider;
		}

		private static void RefreshIcons(PinState state)
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			if (state.Pin != null)
			{
				RefreshButton(state.Pin, state.Priority, PinSprites.GetPin(state.Pin.Size, state.Priority), PriorityColor(state.Priority));
			}
			if (state.Anchor != null)
			{
				RefreshButton(state.Anchor, 100, PinSprites.GetAnchor(state.Anchor.Size), AnchorColor(state.Suspended));
			}
			if (state.Palette != null)
			{
				RefreshButton(state.Palette, 200, PinSprites.GetPalette(state.Palette.Size), PaletteColor(state.ColorIndex));
			}
			RefreshOpacitySlider(state);
		}

		private static Color PaletteColor(int index)
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			if (index <= 0)
			{
				return new Color(0.8f, 0.83f, 0.88f, 0.9f);
			}
			Color val = ColorPresets[Mathf.Clamp(index, 0, ColorPresets.Length - 1)];
			return new Color(Mathf.Clamp(val.r * 1.05f, 0f, 1f), Mathf.Clamp(val.g * 1.05f, 0f, 1f), Mathf.Clamp(val.b * 1.05f, 0f, 1f), 1f);
		}

		private static void RefreshButton(WinButton button, int spriteKey, Sprite sprite, Color color)
		{
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			if (button == null || (Object)(object)button.Image == (Object)null)
			{
				return;
			}
			if (button.SpriteKey != spriteKey && (Object)(object)sprite != (Object)null)
			{
				try
				{
					button.Image.sprite = sprite;
					button.SpriteKey = spriteKey;
				}
				catch (Exception ex)
				{
					PinLog.ErrorOnce("切换图标失败: " + ex.Message);
				}
			}
			if (button.Hovered)
			{
				color = Brighten(color);
			}
			int num = PackColor(color);
			if (button.ColorKey == num)
			{
				return;
			}
			try
			{
				((Graphic)button.Image).color = color;
				button.ColorKey = num;
			}
			catch (Exception ex2)
			{
				PinLog.ErrorOnce("设置图标颜色失败: " + ex2.Message);
			}
		}

		private static void RefreshOpacitySlider(PinState state)
		{
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			WinSlider opacity = state.Opacity;
			if (opacity == null)
			{
				return;
			}
			int num = Mathf.Clamp(state.OpacityLevel, 0, 100);
			if (opacity.LastShownValue != num)
			{
				opacity.LastShownValue = num;
				try
				{
					if ((Object)(object)opacity.FillRect != (Object)null)
					{
						float num2 = (float)num / 100f;
						opacity.FillRect.anchorMax = new Vector2(num2, 1f);
					}
				}
				catch (Exception ex)
				{
					PinLog.ErrorOnce("更新透明度滑块失败: " + ex.Message);
				}
			}
			Color color = (opacity.Hovered ? new Color(0.9f, 0.97f, 1f, 1f) : new Color(0.72f, 0.85f, 1f, 1f));
			int num3 = PackColor(color);
			if (opacity.ColorKey == num3)
			{
				return;
			}
			try
			{
				((Graphic)opacity.FillImage).color = color;
				opacity.ColorKey = num3;
			}
			catch (Exception ex2)
			{
				PinLog.ErrorOnce("设置滑块颜色失败: " + ex2.Message);
			}
		}

		private static Color Brighten(Color color)
		{
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			return new Color(Mathf.Min(1f, color.r * 1.35f), Mathf.Min(1f, color.g * 1.35f), Mathf.Min(1f, color.b * 1.35f), 1f);
		}

		private static Color PriorityColor(int priority)
		{
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			if (priority >= 5)
			{
				return new Color(1f, 0.35f, 0.35f, 1f);
			}
			return priority switch
			{
				4 => new Color(1f, 0.6f, 0.25f, 1f), 
				3 => new Color(1f, 0.85f, 0.3f, 1f), 
				2 => new Color(0.4f, 0.9f, 0.5f, 1f), 
				1 => new Color(0.45f, 0.75f, 1f, 1f), 
				_ => new Color(0.8f, 0.83f, 0.88f, 0.9f), 
			};
		}

		private static Color AnchorColor(bool suspended)
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			if (suspended)
			{
				return new Color(0.72f, 0.55f, 1f, 1f);
			}
			return new Color(0.8f, 0.83f, 0.88f, 0.9f);
		}

		private static int PackColor(Color color)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			int num = Mathf.Clamp(Mathf.RoundToInt(color.r * 255f), 0, 255);
			int num2 = Mathf.Clamp(Mathf.RoundToInt(color.g * 255f), 0, 255);
			int num3 = Mathf.Clamp(Mathf.RoundToInt(color.b * 255f), 0, 255);
			int num4 = Mathf.Clamp(Mathf.RoundToInt(color.a * 255f), 0, 255);
			return (num << 24) | (num2 << 16) | (num3 << 8) | num4;
		}

		private void HandleMouse()
		{
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_0211: Unknown result type (might be due to invalid IL or missing references)
			//IL_0128: Unknown result type (might be due to invalid IL or missing references)
			//IL_013c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_0354: Unknown result type (might be due to invalid IL or missing references)
			//IL_0355: Unknown result type (might be due to invalid IL or missing references)
			//IL_023b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0119: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0266: Unknown result type (might be due to invalid IL or missing references)
			//IL_0298: Unknown result type (might be due to invalid IL or missing references)
			if (!PinConfig.ClickToCycle && !PinConfig.ClickAnchor && !PinConfig.ColorButton && !PinConfig.ShowOpacitySlider)
			{
				return;
			}
			Vector2 val = new Vector2(Input.mousePosition.x, Input.mousePosition.y);
			bool mouseButtonDown = Input.GetMouseButtonDown(0);
			bool flag = MouseHeld();
			if (_sliderDragOwner != null)
			{
				bool flag2 = ResolveCanvasCamera(_sliderDragOwner);
				Camera cam = (flag2 ? _sliderDragOwner.CanvasCam : null);
				if (flag)
				{
					if (!_sliderDragging)
					{
						Vector2 val2 = val - _sliderPressPos;
						if (!flag2 || val2.sqrMagnitude < 16f)
						{
							TrackHoverSlider(_sliderDragOwner.Opacity, val, cam);
							RefreshIcons(_sliderDragOwner);
							return;
						}
						_sliderDragging = true;
						_hasSliderDragWindowPos = TryCaptureWindowPosition(_sliderDragOwner, out var worldPos);
						if (_hasSliderDragWindowPos)
						{
							_sliderDragWindowPos = worldPos;
						}
					}
					UpdateOpacityFromMouse(_sliderDragOwner, val, cam);
					TrackHoverSlider(_sliderDragOwner.Opacity, val, cam);
					RefreshIcons(_sliderDragOwner);
					return;
				}
				TrackHoverSlider(_sliderDragOwner.Opacity, val, cam);
				RefreshIcons(_sliderDragOwner);
				RememberPinState(_sliderDragOwner, _sliderDragOwner.Window);
				_sliderDragOwner = null;
				_sliderDragging = false;
				_hasSliderDragWindowPos = false;
			}
			PinState pinState = null;
			PinState pinState2 = null;
			PinState pinState3 = null;
			PinState pinState4 = null;
			PixelWindow val3 = null;
			for (int i = 0; i < _wins.Count; i++)
			{
				PixelWindow val4 = _wins[i];
				if (!_states.TryGetValue(((Il2CppObjectBase)val4).Pointer, out var value) || !ResolveCanvasCamera(value))
				{
					continue;
				}
				Camera canvasCam = value.CanvasCam;
				if ((TrackHover(value.Pin, val, canvasCam) & mouseButtonDown) && PinConfig.ClickToCycle)
				{
					pinState = value;
					val3 = val4;
				}
				if ((TrackHover(value.Anchor, val, canvasCam) & mouseButtonDown) && PinConfig.ClickAnchor)
				{
					pinState2 = value;
					val3 = val4;
				}
				if ((TrackHover(value.Palette, val, canvasCam) & mouseButtonDown) && PinConfig.ColorButton)
				{
					pinState3 = value;
					val3 = val4;
				}
				if (((PinConfig.ShowOpacitySlider && TrackHoverSlider(value.Opacity, val, canvasCam)) & mouseButtonDown) && pinState == null && pinState2 == null && pinState3 == null)
				{
					pinState4 = value;
					if (val3 == null)
					{
						val3 = val4;
					}
				}
			}
			if (pinState != null)
			{
				CyclePriority(pinState, val3);
			}
			else if (pinState2 != null)
			{
				ToggleSuspend(pinState2, val3);
			}
			else if (pinState3 != null)
			{
				CycleColor(pinState3, val3);
			}
			else if (pinState4 != null)
			{
				_sliderDragOwner = pinState4;
				_sliderPressPos = val;
				_sliderDragging = false;
				_hasSliderDragWindowPos = false;
				RefreshIcons(pinState4);
			}
		}

		private static bool TrackHover(WinButton button, Vector2 mouse, Camera cam)
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			if (button == null || (Object)(object)button.Rect == (Object)null)
			{
				return false;
			}
			bool flag = HitTest(button, mouse, cam);
			if (flag != button.Hovered)
			{
				button.Hovered = flag;
				button.ColorKey = -1;
			}
			return flag;
		}

		private static bool TrackHoverSlider(WinSlider slider, Vector2 mouse, Camera cam)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			if (slider == null || (Object)(object)slider.RootRect == (Object)null)
			{
				return false;
			}
			bool flag = HitTestRect(slider.RootRect, cam, mouse);
			if (flag != slider.Hovered)
			{
				slider.Hovered = flag;
				slider.ColorKey = -1;
			}
			return flag;
		}

		private static bool HitTest(WinButton button, Vector2 mouse, Camera cam)
		{
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			RectTransform rect = button.Rect;
			if ((Object)(object)rect == (Object)null)
			{
				return false;
			}
			try
			{
				if (RectTransformUtility.RectangleContainsScreenPoint(rect, mouse, cam))
				{
					return true;
				}
				if (PinConfig.ClickPadding <= 0)
				{
					return false;
				}
				float num = PinConfig.ClickPadding;
				for (int i = 0; i < HitOffsets.Length; i++)
				{
					Vector2 val = new Vector2(mouse.x + HitOffsets[i].x * num, mouse.y + HitOffsets[i].y * num);
					if (RectTransformUtility.RectangleContainsScreenPoint(rect, val, cam))
					{
						return true;
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				PinLog.ErrorOnce("图标命中测试失败: " + ex.Message);
				return false;
			}
		}

		private static bool HitTestRect(RectTransform rect, Camera cam, Vector2 mouse)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)rect == (Object)null)
			{
				return false;
			}
			try
			{
				return RectTransformUtility.RectangleContainsScreenPoint(rect, mouse, cam);
			}
			catch (Exception ex)
			{
				PinLog.ErrorOnce("滑块命中测试失败: " + ex.Message);
				return false;
			}
		}

		private static bool TryCaptureWindowPosition(PinState state, out Vector2 worldPos)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			worldPos = default;
			if (state == null || state.Window == null)
			{
				return false;
			}
			try
			{
				RectTransform rectTransform = state.Window.rectTransform;
				if ((Object)(object)rectTransform == (Object)null)
				{
					return false;
				}
				worldPos = Vector2.op_Implicit(((Transform)rectTransform).position);
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static void PinWindowInPlace(PinState state, Vector2 worldPos)
		{
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			if (state == null || state.Window == null)
			{
				return;
			}
			try
			{
				RectTransform rectTransform = state.Window.rectTransform;
				if ((Object)(object)rectTransform != (Object)null)
				{
					((Transform)rectTransform).position = Vector2.op_Implicit(worldPos);
				}
			}
			catch
			{
			}
		}

		private void UpdateOpacityFromMouse(PinState state, Vector2 mouse, Camera cam)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			WinSlider opacity = state.Opacity;
			if (opacity == null || (Object)(object)opacity.RootRect == (Object)null)
			{
				return;
			}
			Vector2 val = default;
			try
			{
				if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(opacity.RootRect, mouse, cam, ref val))
				{
					return;
				}
			}
			catch
			{
				return;
			}
			Rect rect = opacity.RootRect.rect;
			if (!(rect.width <= 0.0001f))
			{
				float num = (val.x - rect.xMin) / rect.width;
				num = Mathf.Clamp01(num);
				int num2 = Mathf.Clamp(PinConfig.OpacityMin, 5, 90);
				int num3 = Mathf.RoundToInt(Mathf.Lerp((float)num2, 100f, num));
				if (num3 != state.OpacityLevel)
				{
					state.OpacityLevel = num3;
					state.HasAppliedColor = false;
					ApplyWindowColor(state);
				}
			}
		}

		private static Camera CanvasCamera(RectTransform rect)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Invalid comparison between Unknown and I4
			try
			{
				Canvas componentInParent = ((Component)rect).GetComponentInParent<Canvas>();
				if ((Object)(object)componentInParent == (Object)null)
				{
					return null;
				}
				if ((int)componentInParent.renderMode == 0)
				{
					return null;
				}
				return componentInParent.worldCamera;
			}
			catch
			{
				return null;
			}
		}

		private static bool TryCanvasCamera(RectTransform rect, out Camera cam, out bool cameraRendered)
		{
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Invalid comparison between Unknown and I4
			cam = null;
			cameraRendered = false;
			if ((Object)(object)rect == (Object)null)
			{
				return false;
			}
			try
			{
				Canvas componentInParent = ((Component)rect).GetComponentInParent<Canvas>();
				if ((Object)(object)componentInParent == (Object)null)
				{
					return false;
				}
				Canvas val = componentInParent.rootCanvas;
				if ((Object)(object)val == (Object)null)
				{
					val = componentInParent;
				}
				if ((int)val.renderMode == 0)
				{
					cameraRendered = false;
					return true;
				}
				cameraRendered = true;
				cam = val.worldCamera;
				return (Object)(object)cam != (Object)null;
			}
			catch
			{
				return false;
			}
		}

		private static bool ResolveCanvasCamera(PinState state)
		{
			if (state == null)
			{
				return false;
			}
			int frameCount = Time.frameCount;
			if (state.CanvasCamFrame == frameCount)
			{
				return state.CanvasCamUsable;
			}
			state.CanvasCamFrame = frameCount;
			state.CanvasCam = null;
			state.CanvasCamUsable = false;
			try
			{
				PixelWindow window = state.Window;
				RectTransform rect = ((window != null) ? window.rectTransform : null);
				if (!TryCanvasCamera(rect, out var cam, out var cameraRendered))
				{
					return false;
				}
				state.CanvasCam = cam;
				state.CanvasCamUsable = !cameraRendered || (Object)(object)cam != (Object)null;
				if (!state.CanvasCamUsable && !state.CanvasCamWarned)
				{
					state.CanvasCamWarned = true;
					PinLog.Warn("「" + Title(window) + "」所在画布是相机渲染，但拿不到相机，已跳过该窗口的图标/滑块命中测试（避免误点）。");
				}
				return state.CanvasCamUsable;
			}
			catch
			{
				return false;
			}
		}

		private void HandleHotkey()
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Invalid comparison between Unknown and I4
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			if ((int)PinConfig.Action != 0 && ((int)PinConfig.Modifier == 0 || Input.GetKey(PinConfig.Modifier)) && Input.GetKeyDown(PinConfig.Action))
			{
				PixelWindow val = PickTarget();
				if (val == null)
				{
					PinLog.Info("鼠标不在任何带标题栏的窗口上，未找到可固定的目标。");
				}
				else
				{
					CyclePriority(EnsureState(val), val);
				}
			}
		}

		private PixelWindow PickTarget()
		{
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			if (_wins.Count == 0)
			{
				return null;
			}
			SortByFocusDescending();
			Vector3 mousePosition = Input.mousePosition;
			Vector2 val = new Vector2(mousePosition.x, mousePosition.y);
			for (int i = 0; i < _wins.Count; i++)
			{
				PixelWindow val2 = _wins[i];
				try
				{
					RectTransform rectTransform = val2.rectTransform;
					if ((Object)(object)rectTransform == (Object)null || !RectTransformUtility.RectangleContainsScreenPoint(rectTransform, val, CanvasCamera(rectTransform)))
					{
						continue;
					}
					return val2;
				}
				catch (Exception ex)
				{
					PinLog.Dev("命中测试失败: " + ex.Message);
				}
			}
			return _wins[0];
		}

		private void SortByFocusDescending()
		{
			for (int i = 1; i < _wins.Count; i++)
			{
				PixelWindow val = _wins[i];
				long num = SafeFocusStamp(val);
				int num2 = i - 1;
				while (num2 >= 0 && SafeFocusStamp(_wins[num2]) < num)
				{
					_wins[num2 + 1] = _wins[num2];
					num2--;
				}
				_wins[num2 + 1] = val;
			}
		}

		private void CyclePriority(PinState state, PixelWindow window)
		{
			int priority = state.Priority;
			if (state.Priority >= PinConfig.MaxPriority)
			{
				state.Priority = 0;
			}
			else
			{
				state.Priority++;
			}
			if (state.Priority > 0)
			{
				if (priority <= 0 || state.Sequence <= 0)
				{
					state.Sequence = ++_sequenceCounter;
				}
			}
			else
			{
				state.Sequence = 0;
			}
			RefreshIcons(state);
			RememberPinState(state, window);
			_lastStackPass = -999f;
			_stackPasses = 0;
			_stackWarningIssued = false;
			string text = Title(window);
			if (state.Priority <= 0)
			{
				PinLog.Info("「" + text + "」已取消固定。");
			}
			else if (state.Priority == 1)
			{
				PinLog.Info("「" + text + "」已固定，优先级 1（最低）。");
			}
			else if (state.Priority == PinConfig.MaxPriority)
			{
				PinLog.Info("「" + text + "」已固定，优先级 " + state.Priority + "（最高）。");
			}
			else
			{
				PinLog.Info("「" + text + "」已固定，优先级 " + state.Priority + "。");
			}
		}

		private void ToggleSuspend(PinState state, PixelWindow window)
		{
			state.Suspended = !state.Suspended;
			ApplySuspend(state, window);
			RefreshIcons(state);
			string text = Title(window);
			if (state.Suspended)
			{
				PinLog.Info("「" + text + "」已挂起：不会再被游戏自动关闭/自动隐藏。");
			}
			else
			{
				PinLog.Info("「" + text + "」已取消挂起，恢复正常。");
			}
		}

		private void ApplySuspend(PinState state, PixelWindow window)
		{
			if (window == null)
			{
				return;
			}
			try
			{
				if (state.Suspended)
				{
					state.SuspendedRestoreImportant = window.isImportant;
					state.SuspendedRestoreLocked = window.isLocked;
					if (PinConfig.SuspendMarkImportant)
					{
						window.isImportant = true;
					}
					if (PinConfig.SuspendLockWindow)
					{
						window.isLocked = true;
					}
				}
				else
				{
					if (PinConfig.SuspendMarkImportant)
					{
						window.isImportant = state.SuspendedRestoreImportant;
					}
					if (PinConfig.SuspendLockWindow)
					{
						window.isLocked = state.SuspendedRestoreLocked;
					}
				}
			}
			catch (Exception ex)
			{
				PinLog.ErrorOnce("设置挂起状态失败: " + ex.Message);
				state.Suspended = false;
			}
		}

		private static void RestoreSuspend(PinState state)
		{
			if (state == null || !state.Suspended)
			{
				return;
			}
			PixelWindow window = state.Window;
			if (window == null)
			{
				return;
			}
			try
			{
				if (PinConfig.SuspendMarkImportant)
				{
					window.isImportant = state.SuspendedRestoreImportant;
				}
				if (PinConfig.SuspendLockWindow)
				{
					window.isLocked = state.SuspendedRestoreLocked;
				}
			}
			catch
			{
			}
			state.Suspended = false;
		}

		private void ApplyWindowCap()
		{
			if (!PinConfig.SuspendRaiseWindowCap)
			{
				return;
			}
			bool flag = false;
			for (int i = 0; i < _wins.Count; i++)
			{
				if (_states.TryGetValue(((Il2CppObjectBase)_wins[i]).Pointer, out var value) && value.Suspended)
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				return;
			}
			try
			{
				WindowsHandler current = WindowsHandler.current;
				if ((Object)(object)current == (Object)null)
				{
					return;
				}
				int maxVisibleFloatingWindows = current._maxVisibleFloatingWindows;
				if (maxVisibleFloatingWindows > 0)
				{
					if (_capTarget <= 0)
					{
						_capTarget = maxVisibleFloatingWindows + PinConfig.SuspendCapHeadroom;
						PinLog.Info("挂起生效：窗口上限 " + maxVisibleFloatingWindows + " → " + _capTarget + "（本次运行不再还原，避免把游戏自己的设置压坏）。");
					}
					if (maxVisibleFloatingWindows < _capTarget)
					{
						current._maxVisibleFloatingWindows = _capTarget;
					}
				}
			}
			catch (Exception ex)
			{
				PinLog.ErrorOnce("调整窗口上限失败: " + ex.Message);
			}
		}

		private void CycleColor(PinState state, PixelWindow window)
		{
			state.ColorIndex++;
			if (state.ColorIndex >= ColorPresets.Length)
			{
				state.ColorIndex = 0;
			}
			state.HasAppliedColor = false;
			ApplyWindowColor(state);
			RefreshIcons(state);
			RememberPinState(state, window);
			string text = ColorPresetNames[Mathf.Clamp(state.ColorIndex, 0, ColorPresetNames.Length - 1)];
			string text2 = Title(window);
			if (state.ColorIndex == 0)
			{
				PinLog.Info("「" + text2 + "」窗口配色已恢复原色。");
				return;
			}
			PinLog.Info("「" + text2 + "」窗口配色：" + text + "。");
		}

		private static void EnsureColorsCaptured(PinState state)
		{
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			if (state.ColorsCaptured)
			{
				return;
			}
			try
			{
				PixelWindow window = state.Window;
				if (window != null)
				{
					bool flag = false;
					Image val = ImageOf(window.background);
					if ((Object)(object)val != (Object)null)
					{
						state.OriginalBackground = ((Graphic)val).color;
						flag = true;
					}
					Image val2 = ImageOf(window.topTitleBackground);
					if ((Object)(object)val2 != (Object)null)
					{
						state.OriginalTitleBackground = ((Graphic)val2).color;
						flag = true;
					}
					Image val3 = ImageOf(window.outsideBorder);
					if ((Object)(object)val3 != (Object)null)
					{
						state.OriginalBorder = ((Graphic)val3).color;
						flag = true;
					}
					state.ColorsCaptured = flag;
					if (flag)
					{
						PinLog.Dev("已记录窗口「" + Title(window) + "」的原始配色");
					}
				}
			}
			catch (Exception ex)
			{
				PinLog.ErrorOnce("读取窗口原始颜色失败: " + ex.Message);
			}
		}

		private void ApplyWindowColor(PinState state)
		{
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			PixelWindow window = state.Window;
			if (window == null)
			{
				return;
			}
			int num = Mathf.Clamp(state.ColorIndex, 0, ColorPresets.Length - 1);
			float num2 = Mathf.Clamp01((float)state.OpacityLevel / 100f);
			if (num == 0 && num2 >= 0.999f && !state.HasAppliedColor)
			{
				return;
			}
			EnsureColorsCaptured(state);
			if (!state.ColorsCaptured)
			{
				return;
			}
			float strength = (float)PinConfig.ColorIntensity / 100f;
			Color preset = ColorPresets[num];
			Color val = Tinted(state.OriginalBackground, preset, strength, num2);
			Color color = Tinted(state.OriginalTitleBackground, preset, strength, num2);
			Color color2 = Tinted(state.OriginalBorder, preset, strength, num2);
			try
			{
				Image val2 = ImageOf(window.background);
				if (!((Object)(object)val2 != (Object)null) || !state.HasAppliedColor || !SameColor(((Graphic)val2).color, state.AppliedBackground))
				{
					SetColor(window.background, val);
					SetColor(window.topTitleBackground, color);
					SetColor(window.outsideBorder, color2);
					state.AppliedBackground = val;
					state.HasAppliedColor = true;
				}
			}
			catch (Exception ex)
			{
				PinLog.ErrorOnce("窗口着色失败: " + ex.Message);
			}
		}

		private static void RestoreWindowColor(PinState state)
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			if (state == null || !state.ColorsCaptured || !state.HasAppliedColor)
			{
				return;
			}
			PixelWindow window = state.Window;
			if (window != null)
			{
				try
				{
					SetColor(window.background, state.OriginalBackground);
					SetColor(window.topTitleBackground, state.OriginalTitleBackground);
					SetColor(window.outsideBorder, state.OriginalBorder);
				}
				catch
				{
				}
				state.HasAppliedColor = false;
				state.ColorIndex = 0;
				state.OpacityLevel = 100;
			}
		}

		private static Image ImageOf(TreeNodeRender node)
		{
			if ((Object)(object)node == (Object)null)
			{
				return null;
			}
			try
			{
				return node.image;
			}
			catch
			{
				return null;
			}
		}

		private static void SetColor(TreeNodeRender node, Color color)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			Image val = ImageOf(node);
			if (!((Object)(object)val == (Object)null))
			{
				((Graphic)val).color = color;
			}
		}

		private static Color Tinted(Color original, Color preset, float strength, float alpha)
		{
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			float num = Mathf.Clamp01(strength);
			Color val = Color.Lerp(original, preset, num);
			float num2 = Mathf.Clamp01(Mathf.Max(0f, strength - 1f));
			if (num2 > 0f)
			{
				val = new Color(Mathf.Lerp(val.r, (val.r > 0.5f) ? 1f : 0f, num2), Mathf.Lerp(val.g, (val.g > 0.5f) ? 1f : 0f, num2), Mathf.Lerp(val.b, (val.b > 0.5f) ? 1f : 0f, num2), val.a);
			}
			val.a = Mathf.Clamp01(original.a) * alpha;
			return val;
		}

		private static bool SameColor(Color a, Color b)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			return Mathf.Abs(a.r - b.r) < 0.004f && Mathf.Abs(a.g - b.g) < 0.004f && Mathf.Abs(a.b - b.b) < 0.004f && Mathf.Abs(a.a - b.a) < 0.004f;
		}

		private void ApplyGridSnap()
		{
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			bool flag = DraggingAnyUI();
			bool flag2 = _wasDragging && !flag;
			_wasDragging = flag;
			if (!PinConfig.GridSnap || PinConfig.GridSize <= 1 || flag || !flag2)
			{
				return;
			}
			float num = PinConfig.GridSize;
			for (int i = 0; i < _wins.Count; i++)
			{
				PixelWindow val = _wins[i];
				if (!_states.TryGetValue(((Il2CppObjectBase)val).Pointer, out var value) || value.Following)
				{
					continue;
				}
				try
				{
					RectTransform rectTransform = val.rectTransform;
					if (!((Object)(object)rectTransform == (Object)null))
					{
						Vector2 anchoredPosition = rectTransform.anchoredPosition;
						Vector2 val2 = new Vector2(Mathf.Round(anchoredPosition.x / num) * num, Mathf.Round(anchoredPosition.y / num) * num);
						if (!Approximately(val2, anchoredPosition))
						{
							rectTransform.anchoredPosition = val2;
							value.LastSnapPosition = val2;
							value.HasSnapPosition = true;
						}
					}
				}
				catch (Exception ex)
				{
					PinLog.Dev("网格吸附失败: " + ex.Message);
				}
			}
		}

		private static bool Approximately(Vector2 a, Vector2 b)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			return Mathf.Abs(a.x - b.x) < 0.01f && Mathf.Abs(a.y - b.y) < 0.01f;
		}

		private static bool DraggingAnyUI()
		{
			try
			{
				return UIDragHandler.isDraggingAnyUI;
			}
			catch
			{
				return false;
			}
		}

		private static bool MouseHeld()
		{
			try
			{
				return Input.GetMouseButton(0);
			}
			catch
			{
				return false;
			}
		}

		private static bool WindowActive(PixelWindow window)
		{
			if (window == null)
			{
				return false;
			}
			try
			{
				RectTransform rectTransform = window.rectTransform;
				if ((Object)(object)rectTransform == (Object)null || (Object)(object)((Component)rectTransform).gameObject == (Object)null)
				{
					return false;
				}
				return ((Component)rectTransform).gameObject.activeSelf;
			}
			catch
			{
				return false;
			}
		}

		private void ApplyFollow()
		{
			//IL_0364: Unknown result type (might be due to invalid IL or missing references)
			//IL_0369: Unknown result type (might be due to invalid IL or missing references)
			//IL_036e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0375: Unknown result type (might be due to invalid IL or missing references)
			//IL_037a: Unknown result type (might be due to invalid IL or missing references)
			//IL_037f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0351: Unknown result type (might be due to invalid IL or missing references)
			//IL_0348: Unknown result type (might be due to invalid IL or missing references)
			//IL_043b: Unknown result type (might be due to invalid IL or missing references)
			//IL_043f: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_03c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_0356: Unknown result type (might be due to invalid IL or missing references)
			//IL_035b: Unknown result type (might be due to invalid IL or missing references)
			//IL_045a: Unknown result type (might be due to invalid IL or missing references)
			//IL_045e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0487: Unknown result type (might be due to invalid IL or missing references)
			//IL_0489: Unknown result type (might be due to invalid IL or missing references)
			//IL_048b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0490: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0502: Unknown result type (might be due to invalid IL or missing references)
			//IL_0504: Unknown result type (might be due to invalid IL or missing references)
			//IL_0513: Unknown result type (might be due to invalid IL or missing references)
			//IL_0515: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a6: Unknown result type (might be due to invalid IL or missing references)
			//IL_06ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_06ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_058b: Unknown result type (might be due to invalid IL or missing references)
			//IL_058f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0594: Unknown result type (might be due to invalid IL or missing references)
			//IL_0599: Unknown result type (might be due to invalid IL or missing references)
			//IL_059e: Unknown result type (might be due to invalid IL or missing references)
			//IL_05a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_05a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b7: Unknown result type (might be due to invalid IL or missing references)
			//IL_05bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_05b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_05c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0697: Unknown result type (might be due to invalid IL or missing references)
			//IL_0699: Unknown result type (might be due to invalid IL or missing references)
			if (!PinConfig.FollowItem)
			{
				return;
			}
			bool flag = DraggingAnyUI();
			bool flag2 = MouseHeld();
			for (int i = 0; i < _allWins.Count; i++)
			{
				PixelWindow val = _allWins[i];
				if (_sliderDragOwner != null && _sliderDragOwner.Window != null && ((Il2CppObjectBase)_sliderDragOwner.Window).Pointer == ((Il2CppObjectBase)val).Pointer)
				{
					continue;
				}
				bool flag3 = WindowActive(val);
				RectTransform val2 = OwnerRect(val, out var ownerItem, out var exactMatch, out var parentCount);
				if ((Object)(object)val2 == (Object)null)
				{
					if (!_states.TryGetValue(((Il2CppObjectBase)val).Pointer, out var value))
					{
						continue;
					}
					value.Following = false;
					ReportNoOwner(val, value);
					if (flag3)
					{
						if (!value.WasActiveLastFrame)
						{
							value.FramesAlive = 0;
						}
						value.FramesAlive++;
					}
					value.WasActiveLastFrame = flag3;
					if ((PinConfig.CloseWhenOwnerGone & flag3) && value.HasFollowOffset && value.FramesAlive >= 90 && !OwnerStillAlive(val))
					{
						value.OwnerGoneFrames++;
						if (value.OwnerGoneFrames >= 30)
						{
							value.OwnerGoneFrames = 0;
							try
							{
								val.Hide();
								PinLog.Info("「" + Title(val) + "」的所属物品已销毁，窗口已关闭。");
							}
							catch (Exception ex)
							{
								PinLog.Dev("关闭无主窗口失败: " + ex.Message);
							}
						}
					}
					else
					{
						value.OwnerGoneFrames = 0;
					}
					continue;
				}
				PinState orCreateState = GetOrCreateState(val);
				if (flag3)
				{
					if (!orCreateState.WasActiveLastFrame)
					{
						orCreateState.FramesAlive = 0;
					}
					orCreateState.FramesAlive++;
				}
				orCreateState.WasActiveLastFrame = flag3;
				orCreateState.OwnerGoneFrames = 0;
				if (PinLog.Verbose && !orCreateState.OwnerReported)
				{
					orCreateState.OwnerReported = true;
					PinLog.Dev("跟随对象: 「" + Title(val) + "」所属物品 " + parentCount + " 个" + (exactMatch ? "，contentWindow 完全匹配" : "，contentWindow 不匹配(疑似取到容器)"));
				}
				try
				{
					RectTransform rectTransform = val.rectTransform;
					if ((Object)(object)rectTransform == (Object)null)
					{
						continue;
					}
					try
					{
						if (((Transform)rectTransform).IsChildOf((Transform)(object)val2))
						{
							orCreateState.Following = false;
							continue;
						}
					}
					catch
					{
					}
					if (!_seenPrevious.Contains(((Il2CppObjectBase)val).Pointer))
					{
						orCreateState.HasFollowObserved = false;
						orCreateState.HasFollowOwnerPos = false;
						orCreateState.HasFollowAnchorNode = false;
						orCreateState.FollowRecallTried = false;
						orCreateState.FollowDragging = false;
					}
					if (!orCreateState.HasFollowAnchorNode)
					{
						orCreateState.FollowAnchorNode = ResolveAnchorNode(ownerItem, val2);
						orCreateState.HasFollowAnchorNode = true;
					}
					RectTransform followAnchorNode = orCreateState.FollowAnchorNode;
					Vector2 val3;
					try
					{
						val3 = Vector2.op_Implicit(((Object)(object)followAnchorNode != (Object)null) ? ((Transform)followAnchorNode).position : ((Transform)val2).position);
					}
					catch
					{
						val3 = Vector2.op_Implicit(((Transform)val2).position);
					}
					Vector2 val4 = Vector2.op_Implicit(((Transform)rectTransform).position);
					orCreateState.Following = true;
					if (!orCreateState.HasFollowOffset && !orCreateState.FollowRecallTried)
					{
						orCreateState.FollowRecallTried = true;
						if (TryRecallOffset(val, ownerItem, out var offset))
						{
							orCreateState.FollowOffset = offset;
							orCreateState.HasFollowOffset = true;
							PinLog.Dev("「" + Title(val) + "」已恢复上次记住的跟随位置 " + offset.x.ToString("0.#") + "," + offset.y.ToString("0.#"));
						}
					}
					bool flag4 = orCreateState.HasFollowOwnerPos && !Approximately(val3, orCreateState.FollowOwnerPos);
					bool flag5 = orCreateState.HasFollowObserved && !Approximately(val4, orCreateState.FollowObserved);
					if ((flag2 & flag5) && !flag4)
					{
						orCreateState.FollowOffset = val4 - val3;
						orCreateState.HasFollowOffset = true;
						RememberOffset(val, ownerItem, orCreateState.FollowOffset);
						if (!orCreateState.FollowDragging)
						{
							orCreateState.FollowDragging = true;
							PinLog.Info("「" + Title(val) + "」已记住跟随位置，之后会一直按这个相对位置跟随。");
						}
					}
					if (!flag2)
					{
						orCreateState.FollowDragging = false;
					}
					if (flag)
					{
						orCreateState.FollowObserved = val4;
						orCreateState.HasFollowObserved = true;
						orCreateState.FollowOwnerPos = val3;
						orCreateState.HasFollowOwnerPos = true;
						continue;
					}
					if (orCreateState.HasFollowOffset)
					{
						if (orCreateState.WindowExtentAge <= 0)
						{
							orCreateState.WindowExtentAge = 90;
							if (!RefreshWindowExtent(orCreateState, rectTransform, val))
							{
								orCreateState.HasWindowExtent = false;
							}
						}
						else
						{
							orCreateState.WindowExtentAge--;
						}
						Vector2 targetWorld = ClampWindowToScreen(orCreateState, rectTransform, val, val3 + orCreateState.FollowOffset);
						bool flag6 = MoveWindowTowards(rectTransform, targetWorld, val4);
						Vector2 followObserved = (flag6 ? Vector2.op_Implicit(((Transform)rectTransform).position) : val4);
						if (flag6 && PinLog.Verbose)
						{
							PinLog.Dev("跟随写入: 目标 " + targetWorld.x.ToString("0.#") + "," + targetWorld.y.ToString("0.#") + " 写前 " + val4.x.ToString("0.#") + "," + val4.y.ToString("0.#") + " 写后 " + followObserved.x.ToString("0.#") + "," + followObserved.y.ToString("0.#"));
						}
						orCreateState.FollowObserved = followObserved;
					}
					else
					{
						orCreateState.FollowObserved = val4;
						if (!orCreateState.CalibrationReported)
						{
							orCreateState.CalibrationReported = true;
							PinLog.Info("「" + Title(val) + "」会跟随所属物品，但还没有记住位置：把它拖到想要的地方一次，之后就会一直跟着。");
						}
					}
					orCreateState.HasFollowObserved = true;
					orCreateState.FollowOwnerPos = val3;
					orCreateState.HasFollowOwnerPos = true;
				}
				catch (Exception ex2)
				{
					PinLog.Dev("窗口跟随失败: " + ex2.Message);
				}
			}
		}

		private static int ItemKey(GameItem item)
		{
			if (item == null)
			{
				return 0;
			}
			try
			{
				int uniqueID = item.GetUniqueID();
				return (uniqueID > 0) ? uniqueID : 0;
			}
			catch
			{
				return 0;
			}
		}

		private static int PlacementKey(PixelWindow window, GameItem ownerItem)
		{
			int num = ItemKey(ownerItem);
			if (num > 0)
			{
				return num;
			}
			int num2 = (int)((Il2CppObjectBase)window).Pointer.ToInt64();
			int result;
			if (num2 != 0)
			{
				result = ((num2 < 0) ? num2 : (-num2));
			}
			else
			{
				result = -1;
			}
			return result;
		}

		private bool TryRecallOffset(PixelWindow window, GameItem ownerItem, out Vector2 offset)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			offset = default;
			int key = PlacementKey(window, ownerItem);
			if (!_followPlacements.TryGetValue(key, out var value) || value == null)
			{
				return false;
			}
			offset = value.Offset;
			return true;
		}

		private void RememberOffset(PixelWindow window, GameItem ownerItem, Vector2 offset)
		{
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			if (_followPlacements.Count >= 256)
			{
				_followPlacements.Clear();
			}
			int num = PlacementKey(window, ownerItem);
			if (num != 0)
			{
				FollowPlacement followPlacement = new FollowPlacement();
				followPlacement.Offset = offset;
				followPlacement.ScreenW = Screen.width;
				followPlacement.ScreenH = Screen.height;
				followPlacement.Name = ItemName(ownerItem);
				_followPlacements[num] = followPlacement;
				_placementsDirty = true;
			}
		}

		private static string ItemName(GameItem item)
		{
			if (item == null)
			{
				return "";
			}
			try
			{
				string displayName = item.GetDisplayName(false);
				return string.IsNullOrEmpty(displayName) ? "" : displayName;
			}
			catch
			{
				return "";
			}
		}

		private static string PlacementFilePath()
		{
			try
			{
				string userDataDirectory = MelonEnvironment.UserDataDirectory;
				if (string.IsNullOrEmpty(userDataDirectory))
				{
					return null;
				}
				return Path.Combine(userDataDirectory, "BetterWindows_follow.txt");
			}
			catch
			{
				return null;
			}
		}

		private void LoadPlacements()
		{
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0140: Unknown result type (might be due to invalid IL or missing references)
			if (!PinConfig.RememberFollowPositions)
			{
				return;
			}
			string text = PlacementFilePath();
			if (text == null)
			{
				return;
			}
			try
			{
				if (!File.Exists(text))
				{
					return;
				}
				string[] array = File.ReadAllLines(text, Encoding.UTF8);
				int num = 0;
				foreach (string text2 in array)
				{
					if (string.IsNullOrEmpty(text2) || text2[0] == '#')
					{
						continue;
					}
					string[] array2 = text2.Split('\t');
					if (array2.Length >= 5 && int.TryParse(array2[0], out var result) && result != 0 && float.TryParse(array2[1], NumberStyles.Float, CultureInfo.InvariantCulture, out var result2) && float.TryParse(array2[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var result3))
					{
						if (!int.TryParse(array2[3], out var result4))
						{
							result4 = 0;
						}
						if (!int.TryParse(array2[4], out var result5))
						{
							result5 = 0;
						}
						FollowPlacement followPlacement = new FollowPlacement();
						followPlacement.Offset = new Vector2(result2, result3);
						followPlacement.ScreenW = result4;
						followPlacement.ScreenH = result5;
						followPlacement.Name = ((array2.Length > 5) ? array2[5] : "");
						_followPlacements[result] = followPlacement;
						num++;
					}
				}
				if (num > 0)
				{
					PinLog.Info("已载入 " + num + " 个记住的跟随位置。");
				}
			}
			catch (Exception ex)
			{
				PinLog.Warn("读取跟随位置存档失败（忽略，按未记录处理）: " + ex.Message);
			}
		}

		private void FlushPlacements()
		{
			_placementsDirty = false;
			_lastPlacementFlush = Time.unscaledTime;
			if (!PinConfig.RememberFollowPositions)
			{
				return;
			}
			string text = PlacementFilePath();
			if (text == null)
			{
				return;
			}
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("# 更好的窗口 —— 记住的跟随位置\n");
				stringBuilder.Append("# 每行：物品唯一ID <TAB> 偏移X <TAB> 偏移Y <TAB> 记录时屏幕宽 <TAB> 记录时屏幕高 <TAB> 名字\n");
				stringBuilder.Append("# 删掉某一行 = 忘掉那台机器的位置；整个文件删掉也没关系。\n");
				foreach (KeyValuePair<int, FollowPlacement> followPlacement in _followPlacements)
				{
					FollowPlacement value = followPlacement.Value;
					if (value != null)
					{
						stringBuilder.Append(followPlacement.Key).Append('\t').Append(value.Offset.x.ToString("0.###", CultureInfo.InvariantCulture))
							.Append('\t')
							.Append(value.Offset.y.ToString("0.###", CultureInfo.InvariantCulture))
							.Append('\t')
							.Append(value.ScreenW)
							.Append('\t')
							.Append(value.ScreenH)
							.Append('\t')
							.Append(Clean(value.Name))
							.Append('\n');
					}
				}
				File.WriteAllText(text, stringBuilder.ToString(), Encoding.UTF8);
			}
			catch (Exception ex)
			{
				PinLog.Warn("写入跟随位置存档失败: " + ex.Message);
			}
		}

		private static string Clean(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			return text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
		}

		private static string MemoryKey(PixelWindow window)
		{
			if (window == null)
			{
				return "";
			}
			try
			{
				RectTransform val = OwnerRect(window, out var ownerItem, out var _, out var _);
				if ((Object)(object)val != (Object)null && ownerItem != null)
				{
					int num = ItemKey(ownerItem);
					if (num > 0)
					{
						return "item:" + num;
					}
				}
			}
			catch
			{
			}
			return "title:" + Clean(Title(window));
		}

		private void ApplyPinMemory(PixelWindow window, PinState state)
		{
			if (!PinConfig.RememberPinStates || window == null || state == null)
			{
				return;
			}
			try
			{
				if (!window.hasTitle)
				{
					return;
				}
			}
			catch
			{
				return;
			}
			string text = MemoryKey(window);
			if (string.IsNullOrEmpty(text) || !_windowMemories.TryGetValue(text, out var value) || value == null)
			{
				return;
			}
			if (value.Priority > 0)
			{
				int num = Mathf.Clamp(value.Priority, 0, PinConfig.MaxPriority);
				if (num > 0)
				{
					state.Priority = num;
					state.Sequence = ++_sequenceCounter;
					PinLog.Dev("「" + text + "」已恢复记住的图钉优先级 " + num);
				}
			}
			if (value.ColorIndex > 0)
			{
				state.ColorIndex = Mathf.Clamp(value.ColorIndex, 0, ColorPresets.Length - 1);
				if (state.ColorIndex > 0)
				{
					PinLog.Dev("「" + text + "」已恢复记住的配色序号 " + state.ColorIndex);
				}
			}
			int num2 = Mathf.Clamp(PinConfig.OpacityMin, 5, 90);
			int num3 = Mathf.Clamp(value.Opacity, num2, 100);
			if (num3 < 100)
			{
				state.OpacityLevel = num3;
				PinLog.Dev("「" + text + "」已恢复记住的不透明度 " + num3 + "%");
			}
		}

		private void RememberPinState(PinState state, PixelWindow window)
		{
			if (!PinConfig.RememberPinStates || state == null || window == null)
			{
				return;
			}
			try
			{
				if (!window.hasTitle)
				{
					return;
				}
			}
			catch
			{
				return;
			}
			string text = MemoryKey(window);
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			if (!_windowMemories.TryGetValue(text, out var value) || value == null)
			{
				if (_windowMemories.Count >= 256)
				{
					_windowMemories.Clear();
				}
				value = new WindowMemory();
				_windowMemories[text] = value;
			}
			value.Priority = state.Priority;
			value.ColorIndex = state.ColorIndex;
			value.Opacity = state.OpacityLevel;
			_pinMemoryDirty = true;
		}

		private static string PinMemoryFilePath()
		{
			try
			{
				string userDataDirectory = MelonEnvironment.UserDataDirectory;
				if (string.IsNullOrEmpty(userDataDirectory))
				{
					return null;
				}
				return Path.Combine(userDataDirectory, "BetterWindows_pins.txt");
			}
			catch
			{
				return null;
			}
		}

		private void LoadPinMemory()
		{
			if (!PinConfig.RememberPinStates)
			{
				return;
			}
			string text = PinMemoryFilePath();
			if (text == null)
			{
				return;
			}
			try
			{
				if (!File.Exists(text))
				{
					return;
				}
				string[] array = File.ReadAllLines(text, Encoding.UTF8);
				int num = 0;
				foreach (string text2 in array)
				{
					if (string.IsNullOrEmpty(text2) || text2[0] == '#')
					{
						continue;
					}
					string[] array2 = text2.Split('\t');
					if (array2.Length < 3)
					{
						continue;
					}
					string text3 = array2[0];
					if (!string.IsNullOrEmpty(text3) && int.TryParse(array2[1], out var result) && int.TryParse(array2[2], out var result2))
					{
						int num2 = 100;
						if (array2.Length >= 4 && int.TryParse(array2[3], out var result3))
						{
							num2 = result3;
						}
						WindowMemory windowMemory = new WindowMemory();
						windowMemory.Priority = Mathf.Clamp(result, 0, 9);
						windowMemory.ColorIndex = Mathf.Clamp(result2, 0, ColorPresets.Length - 1);
						windowMemory.Opacity = Mathf.Clamp(num2, 5, 100);
						_windowMemories[text3] = windowMemory;
						num++;
					}
				}
				if (num > 0)
				{
					PinLog.Info("已载入 " + num + " 个记住的图钉/配色/透明度设置。");
				}
			}
			catch (Exception ex)
			{
				PinLog.Warn("读取图钉/配色/透明度存档失败（忽略，按未记录处理）: " + ex.Message);
			}
		}

		private void FlushPinMemory()
		{
			_pinMemoryDirty = false;
			_lastPinMemoryFlush = Time.unscaledTime;
			if (!PinConfig.RememberPinStates)
			{
				return;
			}
			string text = PinMemoryFilePath();
			if (text == null)
			{
				return;
			}
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("# 更好的窗口 —— 记住的图钉优先级、窗口配色和不透明度\n");
				stringBuilder.Append("# 每行：键 <TAB> 图钉优先级(0=未固定, 1-9) <TAB> 配色序号(0=原色) <TAB> 不透明度(5-100)\n");
				stringBuilder.Append("# 键的格式：item:N = 按物品唯一ID记录（推荐）；title:N = 按窗口标题记录（无物品时兜底）\n");
				stringBuilder.Append("# 删掉某一行 = 忘掉那条设置；整个文件删掉也没关系。\n");
				foreach (KeyValuePair<string, WindowMemory> windowMemory in _windowMemories)
				{
					WindowMemory value = windowMemory.Value;
					if (value != null && (value.Priority != 0 || value.ColorIndex != 0 || value.Opacity < 100))
					{
						stringBuilder.Append(Clean(windowMemory.Key)).Append('\t').Append(value.Priority)
							.Append('\t')
							.Append(value.ColorIndex)
							.Append('\t')
							.Append(value.Opacity)
							.Append('\n');
					}
				}
				File.WriteAllText(text, stringBuilder.ToString(), Encoding.UTF8);
			}
			catch (Exception ex)
			{
				PinLog.Warn("写入图钉/配色/透明度存档失败: " + ex.Message);
			}
		}

		private static void ScreenBounds(out float left, out float bottom, out float right, out float top)
		{
			float num = Screen.width;
			float num2 = Screen.height;
			left = 0f;
			bottom = 0f;
			right = num;
			top = num2;
			if (!PinConfig.KeepWindowInGameArea)
			{
				return;
			}
			try
			{
				int gameAreaX = LetterboxManager.GameAreaX;
				int gameAreaY = LetterboxManager.GameAreaY;
				int gameAreaWidth = LetterboxManager.GameAreaWidth;
				int gameAreaHeight = LetterboxManager.GameAreaHeight;
				if (gameAreaWidth >= 100 && gameAreaHeight >= 100 && !((float)gameAreaWidth > num + 1f) && !((float)gameAreaHeight > num2 + 1f) && gameAreaX >= -1 && gameAreaY >= -1)
				{
					left = gameAreaX;
					bottom = gameAreaY;
					right = gameAreaX + gameAreaWidth;
					top = gameAreaY + gameAreaHeight;
				}
			}
			catch
			{
			}
		}

		private static bool MeasureWindowExtent(RectTransform root, out Vector2 min, out Vector2 max)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0101: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0117: Unknown result type (might be due to invalid IL or missing references)
			min = new Vector2(float.MaxValue, float.MaxValue);
			max = new Vector2(float.MinValue, float.MinValue);
			List<RectTransform> list = new List<RectTransform>();
			list.Add(root);
			int num = 0;
			for (int i = 0; i <= 3; i++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				List<RectTransform> list2 = new List<RectTransform>();
				for (int j = 0; j < list.Count; j++)
				{
					RectTransform val = list[j];
					if ((Object)(object)val == (Object)null)
					{
						continue;
					}
					if (num >= 64)
					{
						break;
					}
					num++;
					if (NodeCorners(val, out var min2, out var max2))
					{
						if (min2.x < min.x)
						{
							min.x = min2.x;
						}
						if (min2.y < min.y)
						{
							min.y = min2.y;
						}
						if (max2.x > max.x)
						{
							max.x = max2.x;
						}
						if (max2.y > max.y)
						{
							max.y = max2.y;
						}
					}
					CollectChildren(val, list2);
				}
				list = list2;
			}
			return max.x > min.x && max.y > min.y;
		}

		private static bool NodeCorners(RectTransform node, out Vector2 min, out Vector2 max)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00db: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
			min = default;
			max = default;
			try
			{
				GameObject gameObject = ((Component)node).gameObject;
				if ((Object)(object)gameObject == (Object)null || !gameObject.activeInHierarchy)
				{
					return false;
				}
				Rect rect = node.rect;
				Vector3 lossyScale = ((Transform)node).lossyScale;
				Vector3 position = ((Transform)node).position;
				float num = position.x + rect.xMin * lossyScale.x;
				float num2 = position.x + rect.xMax * lossyScale.x;
				float num3 = position.y + rect.yMin * lossyScale.y;
				float num4 = position.y + rect.yMax * lossyScale.y;
				min = new Vector2(Mathf.Min(num, num2), Mathf.Min(num3, num4));
				max = new Vector2(Mathf.Max(num, num2), Mathf.Max(num3, num4));
				return true;
			}
			catch
			{
				return false;
			}
		}

		private static bool RefreshWindowExtent(PinState state, RectTransform rect, PixelWindow window)
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0110: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			if (!MeasureWindowExtent(rect, out var min, out var max))
			{
				return false;
			}
			Vector2 val;
			try
			{
				val = Vector2.op_Implicit(((Transform)rect).position);
			}
			catch
			{
				return false;
			}
			Vector2 val2 = min - val;
			Vector2 val3 = max - val;
			if (TryWindowPixels(window, out var width, out var height))
			{
				float num = CanvasScale(rect);
				float num2 = (float)width * 0.5f * num * 4f;
				float num3 = (float)height * 0.5f * num * 4f;
				if (num2 < 1f)
				{
					num2 = 1f;
				}
				if (num3 < 1f)
				{
					num3 = 1f;
				}
				if (Mathf.Abs(val2.x) > num2 || Mathf.Abs(val3.x) > num2 || Mathf.Abs(val2.y) > num3 || Mathf.Abs(val3.y) > num3)
				{
					return false;
				}
			}
			state.WindowExtentMin = val2;
			state.WindowExtentMax = val3;
			state.HasWindowExtent = true;
			if (PinLog.Verbose)
			{
				PinLog.Dev("窗口实际范围: 「" + Title(window) + "」相对 " + val2.x.ToString("0.#") + "," + val2.y.ToString("0.#") + " → " + val3.x.ToString("0.#") + "," + val3.y.ToString("0.#") + "（游戏尺寸 " + width + "x" + height + "）");
			}
			return true;
		}

		private static Vector2 ClampWindowToScreen(PinState state, RectTransform rect, PixelWindow window, Vector2 target)
		{
			//IL_02d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02db: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ab: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0104: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0113: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_013e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0155: Unknown result type (might be due to invalid IL or missing references)
			//IL_015c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_0168: Unknown result type (might be due to invalid IL or missing references)
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0171: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_0175: Unknown result type (might be due to invalid IL or missing references)
			//IL_0177: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_017f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
			//IL_019d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0204: Unknown result type (might be due to invalid IL or missing references)
			//IL_020b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0225: Unknown result type (might be due to invalid IL or missing references)
			//IL_022c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0249: Unknown result type (might be due to invalid IL or missing references)
			//IL_0263: Unknown result type (might be due to invalid IL or missing references)
			//IL_0273: Unknown result type (might be due to invalid IL or missing references)
			//IL_0278: Unknown result type (might be due to invalid IL or missing references)
			//IL_027f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0288: Unknown result type (might be due to invalid IL or missing references)
			//IL_028f: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cb: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				ScreenBounds(out var left, out var bottom, out var right, out var top);
				if (right - left <= 1f || top - bottom <= 1f)
				{
					return target;
				}
				if (!TryCanvasCamera(rect, out var cam, out var cameraRendered))
				{
					return target;
				}
				if (cameraRendered && (Object)(object)cam == (Object)null)
				{
					return target;
				}
				Vector2 val;
				Vector2 val2;
				if (state != null && state.HasWindowExtent)
				{
					val = state.WindowExtentMin;
					val2 = state.WindowExtentMax;
				}
				else
				{
					if (!TryWindowPixels(window, out var width, out var height))
					{
						return target;
					}
					float num = CanvasScale(rect);
					float num2 = (float)width * 0.5f * num;
					float num3 = (float)height * 0.5f * num;
					val = new Vector2(0f - num2, 0f - num3);
					val2 = new Vector2(num2, num3);
				}
				Vector3 position = ((Transform)rect).position;
				Vector2 val3 = RectTransformUtility.WorldToScreenPoint(cam, position);
				Vector2 val4 = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(position.x + val.x, position.y + val.y, position.z));
				Vector2 val5 = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(position.x + val2.x, position.y + val2.y, position.z));
				Vector2 val6 = val4 - val3;
				Vector2 val7 = val5 - val3;
				float num4 = left - Mathf.Min(val6.x, val7.x);
				float num5 = right - Mathf.Max(val6.x, val7.x);
				float num6 = bottom - Mathf.Min(val6.y, val7.y);
				float num7 = top - Mathf.Max(val6.y, val7.y);
				Vector2 val8 = RectTransformUtility.WorldToScreenPoint(cam, new Vector3(target.x, target.y, position.z));
				float num8 = (left + right) * 0.5f - (val6.x + val7.x) * 0.5f;
				float num9 = (bottom + top) * 0.5f - (val6.y + val7.y) * 0.5f;
				Vector2 val9 = new Vector2((num5 >= num4) ? Mathf.Clamp(val8.x, num4, num5) : num8, (num7 >= num6) ? Mathf.Clamp(val8.y, num6, num7) : num9);
				if (val9.x == val8.x && val9.y == val8.y)
				{
					return target;
				}
				Vector3 val10 = default;
				if (RectTransformUtility.ScreenPointToWorldPointInRectangle(rect, val9, cam, ref val10))
				{
					return new Vector2(val10.x, val10.y);
				}
				return target;
			}
			catch
			{
				return target;
			}
		}

		private static bool TryItemPixels(GameItem item, out int width, out int height)
		{
			width = 0;
			height = 0;
			if (item == null)
			{
				return false;
			}
			try
			{
				GameItemElement val = ((Il2CppObjectBase)item).TryCast<GameItemElement>();
				if (val == null)
				{
					return false;
				}
				width = val.widthPixels;
				height = val.heightPixels;
				return width > 0 && height > 0;
			}
			catch
			{
				return false;
			}
		}

		private static RectTransform ResolveAnchorNode(GameItem item, RectTransform owner)
		{
			if (TryItemPixels(item, out var width, out var height))
			{
				try
				{
					RectTransform val = FindNodeBySize(owner, width, height);
					if ((Object)(object)val != (Object)null)
					{
						return val;
					}
				}
				catch
				{
				}
			}
			try
			{
				RectTransform val2 = FindAnySizedNode(owner);
				if ((Object)(object)val2 != (Object)null)
				{
					return val2;
				}
			}
			catch
			{
			}
			return owner;
		}

		private static RectTransform FindNodeBySize(RectTransform root, int wantW, int wantH)
		{
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)root == (Object)null)
			{
				return null;
			}
			List<RectTransform> list = new List<RectTransform>();
			list.Add(root);
			for (int i = 0; i <= 3; i++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				List<RectTransform> list2 = new List<RectTransform>();
				for (int j = 0; j < list.Count; j++)
				{
					RectTransform val = list[j];
					if ((Object)(object)val == (Object)null)
					{
						continue;
					}
					try
					{
						Rect rect = val.rect;
						if (Mathf.Abs(rect.width - (float)wantW) < 1.5f && Mathf.Abs(rect.height - (float)wantH) < 1.5f)
						{
							return val;
						}
					}
					catch
					{
					}
					CollectChildren(val, list2);
				}
				list = list2;
			}
			return null;
		}

		private static RectTransform FindAnySizedNode(RectTransform root)
		{
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)root == (Object)null)
			{
				return null;
			}
			List<RectTransform> list = new List<RectTransform>();
			list.Add(root);
			for (int i = 0; i <= 3; i++)
			{
				if (list.Count <= 0)
				{
					break;
				}
				List<RectTransform> list2 = new List<RectTransform>();
				for (int j = 0; j < list.Count; j++)
				{
					RectTransform val = list[j];
					if ((Object)(object)val == (Object)null)
					{
						continue;
					}
					try
					{
						Rect rect = val.rect;
						if (rect.width > 0f && rect.height > 0f)
						{
							return val;
						}
					}
					catch
					{
					}
					CollectChildren(val, list2);
				}
				list = list2;
			}
			return null;
		}

		private static void CollectChildren(RectTransform node, List<RectTransform> into)
		{
			int childCount;
			try
			{
				childCount = ((Transform)node).childCount;
			}
			catch
			{
				return;
			}
			for (int i = 0; i < childCount; i++)
			{
				if (into.Count >= 64)
				{
					break;
				}
				try
				{
					Transform child = ((Transform)node).GetChild(i);
					if (!((Object)(object)child == (Object)null))
					{
						RectTransform val = ((Il2CppObjectBase)child).TryCast<RectTransform>();
						if ((Object)(object)val != (Object)null)
						{
							into.Add(val);
						}
					}
				}
				catch
				{
				}
			}
		}

		private static bool TryWindowPixels(PixelWindow window, out int width, out int height)
		{
			width = 0;
			height = 0;
			if (window == null)
			{
				return false;
			}
			try
			{
				width = window.widthPixels;
				height = window.heightPixels;
				return width > 0 && height > 0;
			}
			catch
			{
				return false;
			}
		}

		private static bool MoveWindowTowards(RectTransform rect, Vector2 targetWorld, Vector2 currentWorld)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0068: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Unknown result type (might be due to invalid IL or missing references)
			//IL_0087: Unknown result type (might be due to invalid IL or missing references)
			Vector2 val = targetWorld - currentWorld;
			if (Mathf.Abs(val.x) < 0.01f && Mathf.Abs(val.y) < 0.01f)
			{
				return false;
			}
			Vector2 val2 = val;
			try
			{
				Transform parent = ((Transform)rect).parent;
				if ((Object)(object)parent != (Object)null)
				{
					Vector3 val3 = parent.InverseTransformVector(new Vector3(val.x, val.y, 0f));
					val2 = new Vector2(val3.x, val3.y);
				}
			}
			catch
			{
				float num = CanvasScale(rect);
				val2 = new Vector2(val.x / num, val.y / num);
			}
			rect.anchoredPosition += val2;
			return true;
		}

		private static float CanvasScale(RectTransform rect)
		{
			try
			{
				Canvas componentInParent = ((Component)rect).GetComponentInParent<Canvas>();
				if ((Object)(object)componentInParent == (Object)null)
				{
					return 1f;
				}
				float scaleFactor = componentInParent.scaleFactor;
				return (scaleFactor > 0.0001f) ? scaleFactor : 1f;
			}
			catch
			{
				return 1f;
			}
		}

		private static void ReportNoOwner(PixelWindow window, PinState state)
		{
			if (!PinLog.Verbose || state.FollowReported)
			{
				return;
			}
			state.FollowReported = true;
			try
			{
				List<GameItem> parentItems = window.parentItems;
				if (parentItems != null && parentItems.Count != 0)
				{
					PinLog.Dev("「" + Title(window) + "」的所属物品不是可读取位置的界面元素，无法跟随。");
				}
			}
			catch
			{
			}
		}

		private static RectTransform OwnerRect(PixelWindow window, out GameItem ownerItem, out bool exactMatch, out int parentCount)
		{
			ownerItem = null;
			exactMatch = false;
			parentCount = 0;
			try
			{
				List<GameItem> parentItems = window.parentItems;
				if (parentItems != null)
				{
					parentCount = parentItems.Count;
					RectTransform val = null;
					GameItem val2 = null;
					for (int i = 0; i < parentItems.Count; i++)
					{
						GameItem val3 = parentItems[i];
						RectTransform val4 = ItemRect(val3);
						if (!((Object)(object)val4 == (Object)null))
						{
							if (IsContentWindowOf(val3, window))
							{
								exactMatch = true;
								ownerItem = val3;
								return val4;
							}
							if ((Object)(object)val == (Object)null)
							{
								val = val4;
								val2 = val3;
							}
						}
					}
					ownerItem = val2;
					return val;
				}
			}
			catch (Exception ex)
			{
				PinLog.Dev("读取 parentItems 失败: " + ex.Message);
			}
			return null;
		}

		private static bool IsContentWindowOf(GameItem item, PixelWindow window)
		{
			try
			{
				PixelWindow contentWindow = item.contentWindow;
				return contentWindow != null && ((Il2CppObjectBase)contentWindow).Pointer == ((Il2CppObjectBase)window).Pointer;
			}
			catch
			{
				return false;
			}
		}

		private static RectTransform ItemRect(GameItem item)
		{
			if (item == null)
			{
				return null;
			}
			try
			{
				GameItemElement val = ((Il2CppObjectBase)item).TryCast<GameItemElement>();
				if (val == null)
				{
					return null;
				}
				TreeNodeRender handler = val.handler;
				if ((Object)(object)handler == (Object)null)
				{
					return null;
				}
				return handler.rectTransform;
			}
			catch
			{
				return null;
			}
		}

		private static bool IsItemAlive(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				GameItemElement val = ((Il2CppObjectBase)item).TryCast<GameItemElement>();
				if (val == null)
				{
					return false;
				}
				TreeNodeRender handler = val.handler;
				if ((Object)(object)handler == (Object)null)
				{
					return false;
				}
				GameObject gameObject = ((Component)handler).gameObject;
				if ((Object)(object)gameObject == (Object)null)
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

		private static bool OwnerStillAlive(PixelWindow window)
		{
			if (window == null)
			{
				return false;
			}
			try
			{
				List<GameItem> parentItems = window.parentItems;
				if (parentItems == null || parentItems.Count == 0)
				{
					return false;
				}
				for (int i = 0; i < parentItems.Count; i++)
				{
					if (IsItemAlive(parentItems[i]))
					{
						return true;
					}
				}
				return false;
			}
			catch
			{
				return false;
			}
		}

		private void ApplyStacking()
		{
			if (_wins.Count == 0 || DraggingAnyUI())
			{
				return;
			}
			_pinned.Clear();
			for (int i = 0; i < _wins.Count; i++)
			{
				if (_states.TryGetValue(((Il2CppObjectBase)_wins[i]).Pointer, out var value) && value.Priority > 0 && value.Pin != null && value.Pin.Alive)
				{
					_pinned.Add(_wins[i]);
				}
			}
			if (_pinned.Count == 0 || (PinConfig.RespectImportant && FrontmostIsImportant()))
			{
				return;
			}
			if (!HasViolation())
			{
				_stackPasses = 0;
				return;
			}
			float unscaledTime = Time.unscaledTime;
			if (!(unscaledTime - _lastStackPass < 0.15f))
			{
				_lastStackPass = unscaledTime;
				_stackPasses++;
				if (_stackPasses == 40 && !_stackWarningIssued)
				{
					_stackWarningIssued = true;
					PinLog.Warn("已连续多次重排窗口层级但仍未稳定，可能是有窗口在被游戏反复置前。");
				}
				Restack();
			}
		}

		private bool HasViolation()
		{
			for (int i = 0; i < _pinned.Count; i++)
			{
				PixelWindow val = _pinned[i];
				PinState pinState = _states[((Il2CppObjectBase)val).Pointer];
				int num = SafeSiblingIndex(val);
				if (num < 0)
				{
					continue;
				}
				Transform a = SafeParent(val);
				for (int j = 0; j < _wins.Count; j++)
				{
					PixelWindow val2 = _wins[j];
					if (((Il2CppObjectBase)val2).Pointer == ((Il2CppObjectBase)val).Pointer || !SameParent(a, SafeParent(val2)))
					{
						continue;
					}
					int num2 = SafeSiblingIndex(val2);
					if (num2 >= 0 && num2 >= num)
					{
						if (!_states.TryGetValue(((Il2CppObjectBase)val2).Pointer, out var value) || value.Priority <= 0)
						{
							return true;
						}
						if (value.Rank() > pinState.Rank())
						{
							return true;
						}
					}
				}
			}
			return false;
		}

		private void Restack()
		{
			for (int i = 1; i < _pinned.Count; i++)
			{
				PixelWindow val = _pinned[i];
				int num = _states[((Il2CppObjectBase)val).Pointer].Rank();
				int num2 = i - 1;
				while (num2 >= 0 && _states[((Il2CppObjectBase)_pinned[num2]).Pointer].Rank() < num)
				{
					_pinned[num2 + 1] = _pinned[num2];
					num2--;
				}
				_pinned[num2 + 1] = val;
			}
			for (int j = 0; j < _pinned.Count; j++)
			{
				PixelWindow val2 = _pinned[j];
				try
				{
					val2.ToFront(PinConfig.ForceToFront);
				}
				catch (Exception ex)
				{
					PinLog.ErrorOnce("ToFront 失败，已取消该窗口的固定: " + ex.Message);
					if (_states.TryGetValue(((Il2CppObjectBase)val2).Pointer, out var value))
					{
						value.Priority = 0;
						value.Sequence = 0;
						RefreshIcons(value);
					}
				}
			}
			for (int k = 0; k < _pinned.Count; k++)
			{
				PixelWindow val3 = _pinned[k];
				try
				{
					RectTransform rectTransform = val3.rectTransform;
					if (!((Object)(object)rectTransform == (Object)null))
					{
						((Transform)rectTransform).SetAsLastSibling();
					}
				}
				catch (Exception ex2)
				{
					PinLog.ErrorOnce("调整窗口层级失败: " + ex2.Message);
				}
			}
		}

		private bool FrontmostIsImportant()
		{
			long num = long.MinValue;
			bool flag = false;
			for (int i = 0; i < _wins.Count; i++)
			{
				PixelWindow val = _wins[i];
				long num2 = SafeFocusStamp(val);
				if (num2 > num)
				{
					num = num2;
					flag = SafeIsImportant(val);
					if (flag && _states.TryGetValue(((Il2CppObjectBase)val).Pointer, out var value) && value.Suspended)
					{
						flag = false;
					}
				}
			}
			return flag;
		}

		private static long SafeFocusStamp(PixelWindow window)
		{
			try
			{
				return window.focusStamp;
			}
			catch
			{
				return 0L;
			}
		}

		private static int SafeSiblingIndex(PixelWindow window)
		{
			try
			{
				RectTransform rectTransform = window.rectTransform;
				if ((Object)(object)rectTransform == (Object)null)
				{
					return -1;
				}
				return ((Transform)rectTransform).GetSiblingIndex();
			}
			catch
			{
				return -1;
			}
		}

		private static Transform SafeParent(PixelWindow window)
		{
			try
			{
				RectTransform rectTransform = window.rectTransform;
				if ((Object)(object)rectTransform == (Object)null)
				{
					return null;
				}
				return ((Transform)rectTransform).parent;
			}
			catch
			{
				return null;
			}
		}

		private static bool SameParent(Transform a, Transform b)
		{
			if ((Object)(object)a == (Object)null || (Object)(object)b == (Object)null)
			{
				return false;
			}
			return ((Object)a).GetInstanceID() == ((Object)b).GetInstanceID();
		}

		private static bool SafeIsImportant(PixelWindow window)
		{
			try
			{
				return window.isImportant;
			}
			catch
			{
				return false;
			}
		}

		private static string Title(PixelWindow window)
		{
			try
			{
				string titleString = window.titleString;
				if (string.IsNullOrEmpty(titleString))
				{
					return "未命名窗口";
				}
				return titleString;
			}
			catch
			{
				return "未命名窗口";
			}
		}

		static PinMod()
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0054: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_011d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0138: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0198: Unknown result type (might be due to invalid IL or missing references)
			//IL_019d: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_021a: Unknown result type (might be due to invalid IL or missing references)
			//IL_021f: Unknown result type (might be due to invalid IL or missing references)
			//IL_023b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0240: Unknown result type (might be due to invalid IL or missing references)
			//IL_025c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0261: Unknown result type (might be due to invalid IL or missing references)
			//IL_027d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0282: Unknown result type (might be due to invalid IL or missing references)
			//IL_029e: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c4: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	internal static class PinSprites
	{
		private const int ArtSize = 16;

		private static readonly string[] PinArt = new string[16]
		{
			"................", "...##########...", "..#oooooooooo#..", "..#o++ooooooo#..", "..#o++ooooooo#..", "..#oooooooooo#..", "..#oooooooooo#..", "...##########...", "......#oo#......", "....#oooooo#....",
			".....######.....", ".......==.......", ".......==.......", ".......==.......", "......====......", "................"
		};

		private static readonly string[] AnchorArt = new string[16]
		{
			"................", "......oooo......", ".....o....o.....", ".....o....o.....", "......oooo......", ".......oo.......", "...oooooooooo...", ".......oo.......", ".......oo.......", ".......oo.......",
			".......oo.......", "..o....oo....o..", ".oo....oo....oo.", ".oo...oooo...oo.", "..oo..oooo..oo..", "...oooooooooo..."
		};

		private static readonly string[] PaletteArt = new string[16]
		{
			"................", "................", ".......o........", "......ooo.......", "......ooo.......", ".....ooooo......", "....ooooooo.....", "....ooooooo.....", "...ooooooooo....", "...oo+++oooo....",
			"...oo+++oooo....", "...ooooooooo....", "....ooooooo.....", ".....ooooo......", "......ooo.......", "................"
		};

		private static readonly string[][] Digits = new string[10][]
		{
			new string[5] { "###", "#.#", "#.#", "#.#", "###" },
			new string[5] { ".#.", "##.", ".#.", ".#.", "###" },
			new string[5] { "##.", "..#", ".#.", "#..", "###" },
			new string[5] { "##.", "..#", ".##", "..#", "##." },
			new string[5] { "#.#", "#.#", "###", "..#", "..#" },
			new string[5] { "###", "#..", "##.", "..#", "##." },
			new string[5] { ".##", "#..", "###", "#.#", "###" },
			new string[5] { "###", "..#", ".#.", ".#.", ".#." },
			new string[5] { "###", "#.#", "###", "#.#", "###" },
			new string[5] { "###", "#.#", "###", "..#", "##." }
		};

		private const int DigitX = 6;

		private const int DigitY = 2;

		private static readonly Dictionary<int, Sprite> Cache = new Dictionary<int, Sprite>();

		private static readonly List<Texture2D> KeepAlive = new List<Texture2D>();

		private static readonly HashSet<int> Failed = new HashSet<int>();

		internal static Sprite GetPin(int pixels, int priority)
		{
			return Get(Mathf.Clamp(pixels, 8, 128), 0, Mathf.Clamp(priority, 0, 9));
		}

		internal static Sprite GetAnchor(int pixels)
		{
			return Get(Mathf.Clamp(pixels, 8, 128), 1, 0);
		}

		internal static Sprite GetPalette(int pixels)
		{
			return Get(Mathf.Clamp(pixels, 8, 128), 2, 0);
		}

		internal static void Warm()
		{
			GetPin(16, 0);
			GetPin(16, 3);
			GetAnchor(16);
			GetPalette(16);
		}

		private static Sprite Get(int size, int kind, int level)
		{
			int num = kind * 100000 + size * 16 + level;
			if (Cache.TryGetValue(num, out var value) && (Object)(object)value != (Object)null)
			{
				return value;
			}
			if (Failed.Contains(num))
			{
				return null;
			}
			try
			{
				Sprite val = Build(size, GridFor(kind, level));
				if ((Object)(object)val == (Object)null)
				{
					Failed.Add(num);
					PinLog.ErrorOnce("图标生成失败（Sprite.Create 返回 null），该图标不再尝试。");
					return null;
				}
				Cache[num] = val;
				PinLog.Dev("图标已生成: kind=" + kind + " " + size + "x" + size + " level=" + level);
				return val;
			}
			catch (Exception ex)
			{
				Failed.Add(num);
				PinLog.Error("图标生成异常: " + ex);
				return null;
			}
		}

		private static Sprite Build(int size, char[] grid)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Expected Obj, but got Unknown
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			Texture2D val = new Texture2D(size, size, (TextureFormat)4, false);
			Il2CppStructArray<Color32> val2 = new Il2CppStructArray<Color32>((long)(size * size));
			Color32 val3 = new Color32((byte)0, (byte)0, (byte)0, (byte)0);
			for (int i = 0; i < ((Il2CppArrayBase<Color32>)(object)val2).Length; i++)
			{
				((Il2CppArrayBase<Color32>)(object)val2)[i] = val3;
			}
			for (int j = 0; j < size; j++)
			{
				int num = 15 - j * 16 / size;
				for (int k = 0; k < size; k++)
				{
					int num2 = k * 16 / size;
					Color32 val4 = Map(grid[num * 16 + num2]);
					if (val4.a != 0)
					{
						((Il2CppArrayBase<Color32>)(object)val2)[j * size + k] = val4;
					}
				}
			}
			val.SetPixels32(val2);
			((Texture)val).filterMode = (FilterMode)0;
			((Texture)val).wrapMode = (TextureWrapMode)1;
			((Texture)val).anisoLevel = 0;
			((Object)val).hideFlags = (HideFlags)32;
			val.Apply(false, false);
			Sprite val5 = Sprite.Create(val, new Rect(0f, 0f, (float)size, (float)size), new Vector2(0.5f, 0.5f), 100f, 0u, (SpriteMeshType)0);
			if ((Object)(object)val5 != (Object)null)
			{
				((Object)val5).hideFlags = (HideFlags)32;
			}
			KeepAlive.Add(val);
			return val5;
		}

		private static char[] PinGrid(int priority)
		{
			char[] array = ToGrid(PinArt);
			if (priority < 1 || priority > 9)
			{
				return array;
			}
			string[] array2 = Digits[priority];
			for (int i = 0; i < 5; i++)
			{
				for (int j = 0; j < 3; j++)
				{
					if (array2[i][j] == '#')
					{
						int num = 2 + i;
						int num2 = 6 + j;
						if (num >= 0 && num < 16 && num2 >= 0 && num2 < 16)
						{
							array[num * 16 + num2] = 'd';
						}
					}
				}
			}
			return array;
		}

		private static char[] AnchorGrid()
		{
			return ToGrid(AnchorArt);
		}

		private static char[] GridFor(int kind, int level)
		{
			return kind switch
			{
				0 => PinGrid(level), 
				1 => ToGrid(AnchorArt), 
				_ => ToGrid(PaletteArt), 
			};
		}

		private static char[] ToGrid(string[] art)
		{
			char[] array = new char[256];
			for (int i = 0; i < 16; i++)
			{
				string text = art[i];
				for (int j = 0; j < 16; j++)
				{
					array[i * 16 + j] = text[j];
				}
			}
			return array;
		}

		private static Color32 Map(char c)
		{
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			return c switch
			{
				'#' => new Color32((byte)40, (byte)42, (byte)52, byte.MaxValue), 
				'o' => new Color32((byte)196, (byte)202, (byte)214, byte.MaxValue), 
				'+' => new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, byte.MaxValue), 
				'=' => new Color32((byte)150, (byte)156, (byte)168, byte.MaxValue), 
				'd' => new Color32((byte)24, (byte)25, (byte)32, byte.MaxValue), 
				_ => new Color32((byte)0, (byte)0, (byte)0, (byte)0), 
			};
		}
	}
	internal sealed class PinState
	{
		internal PixelWindow Window;

		internal WinButton Pin;

		internal WinButton Anchor;

		internal WinButton Palette;

		internal WinSlider Opacity;

		internal int Priority;

		internal int Sequence;

		internal bool Suspended;

		internal bool SuspendedRestoreImportant;

		internal bool SuspendedRestoreLocked;

		internal int ColorIndex;

		internal int OpacityLevel = 100;

		internal bool ColorsCaptured;

		internal Color OriginalBackground;

		internal Color OriginalTitleBackground;

		internal Color OriginalBorder;

		internal Color AppliedBackground;

		internal bool HasAppliedColor;

		internal Vector2 LastSnapPosition;

		internal bool HasSnapPosition;

		internal bool Following;

		internal Vector2 FollowOffset;

		internal bool HasFollowOffset;

		internal Vector2 FollowObserved;

		internal bool HasFollowObserved;

		internal bool FollowDragging;

		internal bool FollowRecallTried;

		internal Vector2 WindowExtentMin;

		internal Vector2 WindowExtentMax;

		internal bool HasWindowExtent;

		internal int WindowExtentAge;

		internal Vector2 FollowOwnerPos;

		internal bool HasFollowOwnerPos;

		internal RectTransform FollowAnchorNode;

		internal bool HasFollowAnchorNode;

		internal bool FollowReported;

		internal bool CalibrationReported;

		internal bool OwnerReported;

		internal int OwnerGoneFrames;

		internal int FramesAlive;

		internal bool WasActiveLastFrame;

		internal int FramesSinceSeen;

		internal bool WasVisibleLastFrame;

		internal Camera CanvasCam;

		internal int CanvasCamFrame = -1;

		internal bool CanvasCamUsable;

		internal bool CanvasCamWarned;

		internal int Rank()
		{
			return -Priority * 1000000 + Sequence;
		}
	}
	internal sealed class WinButton
	{
		internal GameObject Go;

		internal Image Image;

		internal RectTransform Rect;

		internal int Size;

		internal int SpriteKey = -1;

		internal int ColorKey = -1;

		internal bool Hovered;

		internal bool Alive => (Object)(object)Go != (Object)null;
	}
	internal sealed class WinSlider
	{
		internal GameObject Root;

		internal RectTransform RootRect;

		internal Image TrackImage;

		internal Image FillImage;

		internal RectTransform FillRect;

		internal int Size;

		internal int LastShownValue = -1;

		internal int ColorKey = -1;

		internal bool Hovered;

		internal bool Alive => (Object)(object)Root != (Object)null;
	}
}
