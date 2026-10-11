using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using KeycardPack;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;
using UnityEngine.UI;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Mod), "KeycardPack", "1.7.1", "local", null)]
[assembly: MelonGame(null, "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("KeycardPack")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("KeycardPack")]
[assembly: AssemblyTitle("KeycardPack")]
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
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Event | AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.GenericParameter, AllowMultiple = false, Inherited = false)]
	internal sealed class NativeIntegerAttribute : Attribute
	{
		public readonly bool[] TransformFlags;

		public NativeIntegerAttribute()
		{
			TransformFlags = new bool[1] { true };
		}

		public NativeIntegerAttribute(bool[] P_0)
		{
			TransformFlags = P_0;
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
namespace KeycardPack
{
	internal static class KeycardPackBatch
	{
		private static readonly List<GameItem> Batch = new List<GameItem>();

		private static GameItem? _targetPack;

		internal static void Apply(Harmony harmony)
		{
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Expected Obj, but got Unknown
			//IL_0065: Expected Obj, but got Unknown
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(ItemMultiSelectHandler), "EndGroupDrag", (Type[])null, (Type[])null);
				if (methodInfo == null)
				{
					Mod.Log.Warning("KeycardPack: ItemMultiSelectHandler.EndGroupDrag 未找到 → 框选批量存入不会生效");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(KeycardPackBatch), "PrefixEndGroupDrag", (Type[])null), new HarmonyMethod(typeof(KeycardPackBatch), "PostfixEndGroupDrag", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack: 已挂钩 ItemMultiSelectHandler.EndGroupDrag（框选批量存入）");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack Batch Apply: " + ex.Message);
			}
		}

		public static void PrefixEndGroupDrag()
		{
			Batch.Clear();
			_targetPack = null;
			try
			{
				if (!KeycardPackConfig.BatchDeposit)
				{
					return;
				}
				ItemMultiSelectHandler current = ItemMultiSelectHandler.current;
				if ((Object)(object)current == (Object)null)
				{
					return;
				}
				GameItem val = FindHoveredPack();
				if (val == null)
				{
					return;
				}
				List<GameItemElement> selectedItems = current.selectedItems;
				if (selectedItems == null)
				{
					return;
				}
				for (int i = 0; i < selectedItems.Count; i++)
				{
					GameItem val2 = null;
					try
					{
						val2 = (GameItem)(object)selectedItems[i];
					}
					catch
					{
						continue;
					}
					if (val2 == null)
					{
						continue;
					}
					try
					{
						if (((Il2CppObjectBase)val2).WasCollected)
						{
							continue;
						}
					}
					catch
					{
					}
					if (!KeycardPackFactory.IsPack(val2) && KeycardPackCounts.IsKnownKeycardId(SafeId(val2)))
					{
						Batch.Add(val2);
					}
				}
				if (Batch.Count > 0)
				{
					_targetPack = val;
				}
				else if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack 群拖：选中的里面没有钥匙卡，批量存入不触发");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack Batch prefix: " + ex.Message);
				Batch.Clear();
				_targetPack = null;
			}
		}

		public static void PostfixEndGroupDrag()
		{
			try
			{
				if (_targetPack != null && Batch.Count != 0)
				{
					GameItem pack = _targetPack;
					List<GameItem> cards = new List<GameItem>(Batch);
					_targetPack = null;
					Batch.Clear();
					KeycardPackPatch.Defer(() =>
					{
						DepositAll(pack, cards);
					});
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack Batch postfix: " + ex.Message);
				_targetPack = null;
				Batch.Clear();
			}
		}

		private static void DepositAll(GameItem? pack, List<GameItem> cards)
		{
			try
			{
				if (pack == null || ((Il2CppObjectBase)pack).WasCollected)
				{
					return;
				}
				try
				{
					KeycardPackFactory.EnsureWindow(pack, "Batch");
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("KeycardPack EnsureWindow(Batch): " + ex.Message);
				}
				int num = 0;
				int num2 = 0;
				foreach (GameItem card in cards)
				{
					try
					{
						if (card == null || ((Il2CppObjectBase)card).WasCollected)
						{
							num2++;
						}
						else if (KeycardPackFactory.TryDeposit(pack, card, syncNow: false))
						{
							num++;
						}
						else
						{
							num2++;
						}
					}
					catch (Exception ex2)
					{
						num2++;
						Mod.Log.Warning("KeycardPack 批量存入单张失败: " + ex2.Message);
					}
				}
				try
				{
					KeycardPackFactory.RefreshButtons(pack);
				}
				catch
				{
				}
				Mod.Log.Msg($"KeycardPack 框选批量存入：成功 {num} 张，跳过 {num2} 张（非卡 / 对象已失效 / 该种已到上限）");
			}
			catch (Exception ex3)
			{
				Mod.Log.Warning("KeycardPack DepositAll: " + ex3.Message);
			}
		}

		internal static GameItem? FindHoveredPack()
		{
			GameItem val = KeycardPackHighlight.FindPackUnderMouse(out var route);
			if (val != null)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack 群拖落点：命中卡包（" + KeycardPackHighlight.RouteName(route) + "）");
				}
				return val;
			}
			if (Mod.Verbose)
			{
				Mod.Log.Msg("KeycardPack 群拖落点：三路都没命中卡包 → 批量存入不触发");
			}
			return null;
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
	}
	internal static class KeycardPackConfig
	{
		private const float DefaultThiefStockChance = 0.25f;

		private static MelonPreferences_Category? _cat;

		private static MelonPreferences_Entry<float>? _thiefStockChance;

		private static MelonPreferences_Entry<bool>? _batchDeposit;

		private static MelonPreferences_Entry<bool>? _dragHighlight;

		public static float ThiefStockChance
		{
			get
			{
				float num = _thiefStockChance?.Value ?? 0.25f;
				if (num < 0f)
				{
					return 0f;
				}
				if (num > 1f)
				{
					return 1f;
				}
				return num;
			}
		}

		public static bool BatchDeposit => _batchDeposit?.Value ?? true;

		public static bool DragHighlight => _dragHighlight?.Value ?? true;

		public static int ToStockPercent(float bmMult)
		{
			if (bmMult < 0f)
			{
				bmMult = 0f;
			}
			int num = (int)Math.Round((double)(ThiefStockChance * bmMult) * 100.0, MidpointRounding.AwayFromZero);
			if (num < 0)
			{
				return 0;
			}
			if (num > 100)
			{
				return 100;
			}
			return num;
		}

		public static void Init()
		{
			try
			{
				_cat = MelonPreferences.CreateCategory("KeycardPack", "钥匙卡包");
				_thiefStockChance = _cat.CreateEntry<float>("thief_stock_chance", 0.25f, "小偷(thief)上架钥匙卡包基础概率，0~1（0.25=25%）。会×黑市库存倍率后换成 0~100 再 RNG.Roll(int)。默认0.25。", (string)null, false, false, (ValueValidator)null, (string)null);
				_batchDeposit = _cat.CreateEntry<bool>("batch_deposit", true, "框选一堆钥匙卡再整组拖到卡包上 → 一次性全部存入。默认true（关掉则只能一张一张拖）。", (string)null, false, false, (ValueValidator)null, (string)null);
				_dragHighlight = _cat.CreateEntry<bool>("drag_highlight", true, "手上拿着钥匙卡（或框选的一批）时，卡包自己那几格亮蓝。默认true。", (string)null, false, false, (ValueValidator)null, (string)null);
				_cat.SaveToFile(false);
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"KeycardPackConfig: thief_stock_chance={ThiefStockChance}");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackConfig.Init: " + ex.Message);
			}
		}
	}
	internal static class KeycardPackCounts
	{
		internal const string TagPrefix = "KEYCARD_PACK_CNT_";

		internal const int Cap = 99;

		internal static string TagFor(string keycardId)
		{
			return "KEYCARD_PACK_CNT_" + keycardId;
		}

		internal static int Get(GameItem pack, string keycardId)
		{
			try
			{
				string text = TagFor(keycardId);
				TagSystem state = pack.state;
				Dictionary<string, TagState> val = ((state != null) ? state.dict : null);
				if (val == null || !val.ContainsKey(text))
				{
					return 0;
				}
				TagState val2 = val[text];
				return (val2 != null) ? val2.valueInt : 0;
			}
			catch
			{
				return 0;
			}
		}

		internal static void Set(GameItem pack, string keycardId, int value)
		{
			try
			{
				value = Math.Clamp(value, 0, 99);
				string text = TagFor(keycardId);
				TagSystem state = pack.state;
				Dictionary<string, TagState> val = ((state != null) ? state.dict : null);
				if (val == null)
				{
					return;
				}
				if (!val.ContainsKey(text))
				{
					try
					{
						pack.EnableTag(text, false);
					}
					catch
					{
					}
				}
				if (!val.ContainsKey(text))
				{
					return;
				}
				TagState val2 = val[text];
				if (val2 != null)
				{
					try
					{
						val2.Enable();
					}
					catch
					{
					}
					val2.valueInt = value;
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackCounts.Set(" + keycardId + "): " + ex.Message);
			}
		}

		internal static int Add(GameItem pack, string keycardId, int delta)
		{
			int num = Math.Clamp(Get(pack, keycardId) + delta, 0, 99);
			Set(pack, keycardId, num);
			return num;
		}

		internal static bool IsKnownKeycardId(string? id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			KeycardPackFactory.SlotSpec[] slots = KeycardPackFactory.Slots;
			foreach (KeycardPackFactory.SlotSpec slotSpec in slots)
			{
				if (string.Equals(slotSpec.KeycardId, id, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		internal static bool CanAccept(GameItem pack, string? keycardId)
		{
			if (pack == null || !IsKnownKeycardId(keycardId))
			{
				return false;
			}
			return Get(pack, keycardId) < 99;
		}
	}
	internal static class KeycardPackFactory
	{
		internal readonly record struct SlotSpec(string KeycardId, string Label, string Atlas, string Sprite, string LocKey);

		internal const string ItemId = "keycard_pack";

		internal const string UiTag = "KEYCARD_PACK_BUTTON_UI_V11";

		private const int BtnW = 64;

		private const int BtnH = 78;

		private const int LabelH = 20;

		private const float IconBaseW = 36f;

		private const float IconBaseH = 44f;

		private const float IconScale = 1.25f;

		internal static readonly SlotSpec[] Slots = new SlotSpec[8]
		{
			new SlotSpec("sec_keycard", "治安部", "Items/items_misc", "sec_keycard", "item_sec_keycard_name"),
			new SlotSpec("sci_keycard", "研发", "Items/items_misc", "sci_card", "item_sci_keycard_name"),
			new SlotSpec("cmd_keycard", "指挥", "Items/items_misc", "command_keycard", "item_cmd_keycard_name"),
			new SlotSpec("med_keycard", "医疗", "Items/items_misc", "med_card", "item_med_keycard_name"),
			new SlotSpec("eng_keycard", "工程", "Items/items_misc", "eng_card", "item_eng_keycard_name"),
			new SlotSpec("sup_keycard", "补给", "Items/items_misc", "sup_card", "item_sup_keycard_name"),
			new SlotSpec("ser_keycard", "服务", "Items/items_misc", "ser_card", "item_ser_keycard_name"),
			new SlotSpec("blank_keycard", "空白", "Items/items_misc", "blank_card", "item_blank_keycard_name")
		};

		private static readonly List<Action> RootedActions = new List<Action>();

		private static readonly Dictionary<nint, Dictionary<string, AdvButtonElement>> ButtonsByPtr = new Dictionary<IntPtr, Dictionary<string, AdvButtonElement>>();

		private static readonly Dictionary<int, Dictionary<string, AdvButtonElement>> ButtonsByUid = new Dictionary<int, Dictionary<string, AdvButtonElement>>();

		private static readonly Dictionary<nint, Dictionary<string, TagElement>> LabelsByPtr = new Dictionary<IntPtr, Dictionary<string, TagElement>>();

		private static readonly Dictionary<int, Dictionary<string, TagElement>> LabelsByUid = new Dictionary<int, Dictionary<string, TagElement>>();

		private static bool _rebuilding;

		internal static bool IsPack(GameItem? item)
		{
			if (item == null)
			{
				return false;
			}
			try
			{
				return string.Equals(item.identifier, "keycard_pack", StringComparison.OrdinalIgnoreCase);
			}
			catch
			{
				return false;
			}
		}

		internal static GameItem Create()
		{
			if (Mod.Verbose)
			{
				Mod.Log.Msg("KeycardPackFactory: Create() start");
			}
			GameItem val = ItemDirectory.CreateEmptyItem("keycard_pack") ?? throw new InvalidOperationException("CreateEmptyItem('keycard_pack') null");
			val.identifier = "keycard_pack";
			try
			{
				val.SetName("钥匙卡包");
			}
			catch
			{
			}
			val.name = "钥匙卡包";
			KeycardPackSprite.ApplyToItem(val);
			val.SetValue(500L);
			try
			{
				val.SetGameItemType("TOOL");
			}
			catch
			{
			}
			try
			{
				ContrabandHelper.InitContrabandItem(val, 3);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack InitContrabandItem: " + ex.Message);
			}
			try
			{
				val.EnableTag("NESTABLE_DEVICE_TAG", false);
			}
			catch
			{
			}
			try
			{
				val.EnableTag("KEYCARD_PACK_BUTTON_UI_V11", false);
			}
			catch
			{
			}
			try
			{
				AudioHelper.InitPlasticCard(val);
			}
			catch
			{
				try
				{
					AudioHelper.InitPouch(val);
				}
				catch
				{
				}
			}
			UpdateTooltip(val);
			EnsureWindow(val, "Create");
			if (Mod.Verbose)
			{
				Mod.Log.Msg("KeycardPackFactory: Create() done");
			}
			return val;
		}

		internal static bool EnsureWindow(GameItem item, string ctx)
		{
			//IL_0142: Unknown result type (might be due to invalid IL or missing references)
			//IL_0149: Expected Obj, but got Unknown
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Expected Obj, but got Unknown
			//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f3: Expected Obj, but got Unknown
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_0279: Expected Obj, but got Unknown
			//IL_0352: Unknown result type (might be due to invalid IL or missing references)
			//IL_0359: Expected Obj, but got Unknown
			if (item == null)
			{
				return false;
			}
			try
			{
				if (!string.Equals(item.identifier, "keycard_pack", StringComparison.OrdinalIgnoreCase))
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
				bool flag = false;
				try
				{
					flag = item.IsTag("KEYCARD_PACK_BUTTON_UI_V11");
				}
				catch
				{
				}
				if (((item.contentWindow != null) & flag) && TryGetButtonMap(item, out Dictionary<string, AdvButtonElement> _))
				{
					RefreshButtons(item);
					KeycardPackHighlight.TrackPack(item);
					return true;
				}
			}
			catch
			{
			}
			try
			{
				item.SetContentWindow((PixelWindow)null);
			}
			catch
			{
				try
				{
					item.contentWindow = null;
				}
				catch
				{
				}
			}
			try
			{
				string text = ((string.IsNullOrEmpty(item.name) || LooksLikeLocError(item.name)) ? "钥匙卡包" : item.name);
				if (LooksLikeLocError(item.name) || string.IsNullOrEmpty(item.name))
				{
					try
					{
						item.SetName(text);
					}
					catch
					{
					}
					item.name = text;
				}
				PixelWindow val = new PixelWindow(true, text);
				GridPixelElement val2 = new GridPixelElement(4, 2, false);
				val2.SetSpacing(8, 6);
				Dictionary<string, AdvButtonElement> dictionary = new Dictionary<string, AdvButtonElement>(StringComparer.OrdinalIgnoreCase);
				Dictionary<string, TagElement> dictionary2 = new Dictionary<string, TagElement>(StringComparer.OrdinalIgnoreCase);
				for (int i = 0; i < Slots.Length; i++)
				{
					SlotSpec spec = Slots[i];
					int num = i % 4;
					int num2 = i / 4;
					int count = KeycardPackCounts.Get(item, spec.KeycardId);
					GameItem packRef = item;
					string keycardId = spec.KeycardId;
					Action val3 = MakeAction(() =>
					{
						KeycardPackPatch.Defer(() =>
						{
							TryWithdraw(packRef, keycardId);
						});
					});
					AdvButtonElement val4;
					try
					{
						val4 = new AdvButtonElement((Sprite)null, "", 64, 78, val3, "interface_click");
					}
					catch (Exception ex)
					{
						Mod.Log.Warning($"KeycardPackFactory[{ctx}] button '{spec.Label}': {ex.Message}");
						continue;
					}
					dictionary[spec.KeycardId] = val4;
					GridPixelElement val5 = new GridPixelElement(1, 2, false);
					val5.SetSpacing(2, 2);
					PixelElement val6 = ((Il2CppObjectBase)val4).TryCast<PixelElement>();
					if (val6 == null)
					{
						Mod.Log.Warning($"KeycardPackFactory[{ctx}] button '{spec.Label}' 非 PixelElement");
						continue;
					}
					if (!val5.Attach(val6, 0, 0, 64, 78))
					{
						Mod.Log.Warning($"KeycardPackFactory[{ctx}] attach btn '{spec.Label}' 失败");
					}
					TagElement val7 = new TagElement(-1, false);
					string label = DisplayName(spec);
					try
					{
						val7.SetText(LabelText(label, count), 100, (ColorPalette)12895408);
					}
					catch
					{
						try
						{
							val7.SetText(LabelText(label, count), 100, (ColorPalette)0);
						}
						catch
						{
						}
					}
					dictionary2[spec.KeycardId] = val7;
					PixelElement val8 = ((Il2CppObjectBase)val7).TryCast<PixelElement>();
					if (val8 != null && !val5.Attach(val8, 0, 1, 64, 24))
					{
						Mod.Log.Warning($"KeycardPackFactory[{ctx}] attach label '{spec.Label}' 失败");
					}
					PixelElement val9 = ((Il2CppObjectBase)val5).TryCast<PixelElement>();
					if (val9 == null)
					{
						Mod.Log.Warning($"KeycardPackFactory[{ctx}] cell '{spec.Label}' 非 PixelElement");
					}
					else if (!val2.Attach(val9, num, num2, 64, 106))
					{
						Mod.Log.Warning($"KeycardPackFactory[{ctx}] attach cell '{spec.Label}' 失败");
					}
				}
				StoreButtonMap(item, dictionary);
				StoreLabelMap(item, dictionary2);
				PixelElement val10 = ((Il2CppObjectBase)val2).TryCast<PixelElement>();
				if (val10 == null)
				{
					Mod.Log.Warning("KeycardPackFactory[" + ctx + "]: carrier 不是 PixelElement");
					return false;
				}
				val.Attach(val10);
				item.SetContentWindow(val);
				try
				{
					item.EnableTag("KEYCARD_PACK_BUTTON_UI_V11", false);
				}
				catch
				{
				}
				try
				{
					val.Validate();
				}
				catch
				{
				}
				SlotSpec[] slots = Slots;
				for (int num3 = 0; num3 < slots.Length; num3++)
				{
					SlotSpec slotSpec = slots[num3];
					if (dictionary.TryGetValue(slotSpec.KeycardId, out var value) && value != null)
					{
						PlaceSingleCardOnButton(value, slotSpec.Atlas, slotSpec.Sprite, ctx);
					}
				}
				RefreshButtons(item);
				KeycardPackHighlight.TrackPack(item);
				KeycardPackPatch.Defer(() =>
				{
					RefreshButtons(item);
				});
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPackFactory[" + ctx + "]: V7 灰按钮无字+单张卡图+下方「标签\\n×N」");
				}
				return true;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning($"KeycardPackFactory[{ctx}] EnsureWindow: {ex2.GetType().Name}: {ex2.Message}");
				return false;
			}
		}

		internal static bool TryDeposit(GameItem pack, GameItem card, bool syncNow = true)
		{
			if (pack == null || card == null)
			{
				return false;
			}
			try
			{
				if (!string.Equals(pack.identifier, "keycard_pack", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
			string text;
			try
			{
				text = card.identifier ?? "";
			}
			catch
			{
				return false;
			}
			if (!KeycardPackCounts.IsKnownKeycardId(text))
			{
				return false;
			}
			int num = 1;
			try
			{
				num = Math.Max(1, card.unitCount);
			}
			catch
			{
			}
			int num2 = KeycardPackCounts.Get(pack, text);
			int num3 = 99 - num2;
			if (num3 <= 0)
			{
				return false;
			}
			int num4 = Math.Min(num3, num);
			if (syncNow)
			{
				try
				{
					EnsureWindow(pack, "Deposit");
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("KeycardPack EnsureWindow deposit: " + ex.Message);
				}
			}
			KeycardPackCounts.Add(pack, text, num4);
			int value = KeycardPackCounts.Get(pack, text);
			ConsumePlayerCard(card, num4, num);
			RefreshButtons(pack);
			UpdateTooltip(pack);
			Mod.Log.Msg($"KeycardPack deposit '{text}' +{num4} → 库存 {value}");
			return true;
		}

		internal static bool TryWithdraw(GameItem pack, string keycardId)
		{
			if (pack == null || string.IsNullOrEmpty(keycardId))
			{
				return false;
			}
			try
			{
				if (!string.Equals(pack.identifier, "keycard_pack", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
			if (!KeycardPackCounts.IsKnownKeycardId(keycardId))
			{
				return false;
			}
			if (KeycardPackCounts.Get(pack, keycardId) <= 0)
			{
				Mod.Log.Msg("KeycardPack withdraw '" + keycardId + "': 库存为 0");
				return false;
			}
			GameGridInventory val = null;
			try
			{
				GameInventory parentInventory = pack.parentInventory;
				val = ((parentInventory != null) ? ((Il2CppObjectBase)parentInventory).TryCast<GameGridInventory>() : null);
			}
			catch
			{
				val = null;
			}
			if (val == null)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack withdraw: 卡包不在网格上，无法同网格发放");
				}
				return false;
			}
			GameItem val2 = null;
			try
			{
				val2 = DirectoryMaster.Item(keycardId, true);
				if (val2 == null)
				{
					Mod.Log.Warning("KeycardPack withdraw: DirectoryMaster.Item('" + keycardId + "') null");
					return false;
				}
				try
				{
					GeneralHelper.SetItemOwned(val2, true);
				}
				catch
				{
				}
				try
				{
					val2.unitCount = 1;
				}
				catch
				{
				}
				int value = 0;
				try
				{
					value = GraphUtils.TryAcceptAllMid(val, val2, -1);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("KeycardPack TryAcceptAllMid: " + ex.Message);
				}
				bool flag = false;
				try
				{
					GameInventory parentInventory2 = val2.parentInventory;
					GameGridInventory val3 = ((parentInventory2 != null) ? ((Il2CppObjectBase)parentInventory2).TryCast<GameGridInventory>() : null);
					if (val3 != null && ((Il2CppObjectBase)val3).Pointer == ((Il2CppObjectBase)val).Pointer)
					{
						flag = true;
					}
				}
				catch
				{
				}
				if (!flag)
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg($"KeycardPack withdraw '{keycardId}': 同网格无空位（TryAcceptAllMid={value}），不扣库存");
					}
					try
					{
						val2.Destroy();
					}
					catch
					{
					}
					return false;
				}
				int value2 = KeycardPackCounts.Add(pack, keycardId, -1);
				RefreshButtons(pack);
				UpdateTooltip(pack);
				Mod.Log.Msg($"KeycardPack withdraw '{keycardId}' OK → 库存 {value2}（同网格发放）");
				return true;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("KeycardPack TryWithdraw: " + ex2.Message);
				if (val2 != null)
				{
					try
					{
						if (val2.parentInventory == null)
						{
							val2.Destroy();
						}
					}
					catch
					{
					}
				}
				return false;
			}
		}

		private static void PlaceSingleCardOnButton(AdvButtonElement btn, string atlas, string spriteName, string ctx)
		{
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Expected Obj, but got Unknown
			//IL_00ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_0183: Unknown result type (might be due to invalid IL or missing references)
			//IL_0199: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01cf: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				TreeNodeRender handler = ((SelectableElement)btn).handler;
				if ((Object)(object)handler == (Object)null)
				{
					Mod.Log.Warning("KeycardPack PlaceCard[" + ctx + "]: handler=null");
					return;
				}
				RectTransform rectTransform = handler.rectTransform;
				if ((Object)(object)rectTransform == (Object)null)
				{
					Mod.Log.Warning("KeycardPack PlaceCard[" + ctx + "]: rectTransform=null");
					return;
				}
				Sprite val = null;
				try
				{
					val = RenderHandler.LoadFromAtlas(atlas, spriteName);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("KeycardPack PlaceCard LoadFromAtlas '" + spriteName + "': " + ex.Message);
					return;
				}
				if ((Object)(object)val == (Object)null)
				{
					Mod.Log.Warning("KeycardPack PlaceCard '" + spriteName + "': sprite=null");
					return;
				}
				GameObject val2 = new GameObject("kcp_card_icon");
				val2.transform.SetParent((Transform)(object)rectTransform, false);
				RectTransform val3 = val2.GetComponent<RectTransform>();
				if ((Object)(object)val3 == (Object)null)
				{
					val3 = val2.AddComponent<RectTransform>();
				}
				Rect rect = rectTransform.rect;
				float num = rect.width;
				rect = rectTransform.rect;
				float num2 = rect.height;
				if (num < 1f)
				{
					num = 64f;
				}
				if (num2 < 1f)
				{
					num2 = 78f;
				}
				val3.anchorMin = new Vector2(0f, 1f);
				val3.anchorMax = new Vector2(0f, 1f);
				val3.pivot = new Vector2(0.5f, 0.5f);
				val3.anchoredPosition = new Vector2(num * 0.5f, (0f - num2) * 0.5f);
				val3.sizeDelta = new Vector2(36f, 44f);
				((Transform)val3).localRotation = Quaternion.Euler(0f, 0f, -90f);
				((Transform)val3).localScale = new Vector3(1.25f, 1.25f, 1f);
				((Transform)val3).SetAsLastSibling();
				Image val4 = val2.AddComponent<Image>();
				val4.sprite = val;
				val4.type = (Type)0;
				val4.preserveAspect = true;
				((Graphic)val4).raycastTarget = false;
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning($"KeycardPack PlaceCard[{ctx}] '{spriteName}': {ex2.Message}");
			}
		}

		private static void ConsumePlayerCard(GameItem card, int take, int add)
		{
			try
			{
				int num = (card.unitCount = Math.Max(0, add - take));
				if (num <= 0)
				{
					try
					{
						card.Destroy();
					}
					catch
					{
						try
						{
							card.unitCount = 0;
						}
						catch
						{
						}
					}
					Mod.Log.Msg("KeycardPack: 消耗卡 unitCount→0 + Destroy");
				}
				else
				{
					Mod.Log.Msg($"KeycardPack: 消耗卡 unitCount {add}→{num}");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack ConsumePlayerCard: " + ex.Message);
			}
		}

		private static string LabelText(string label, int count)
		{
			return $"{label}×{count}";
		}

		internal static string DisplayName(SlotSpec spec)
		{
			try
			{
				if (!string.IsNullOrEmpty(spec.LocKey))
				{
					string text = ExtractDeptShortName(LocHelper.GetLocalizedItem(spec.LocKey, (Il2CppReferenceArray<Object>)null));
					if (!string.IsNullOrEmpty(text))
					{
						if (string.Equals(text, "治安", StringComparison.Ordinal))
						{
							return "治安部";
						}
						return text;
					}
				}
			}
			catch
			{
			}
			return spec.Label;
		}

		private static string? ExtractDeptShortName(string? localized)
		{
			if (string.IsNullOrEmpty(localized))
			{
				return null;
			}
			int num = localized.LastIndexOf('（');
			int num2 = localized.LastIndexOf('）');
			if (num >= 0 && num2 > num)
			{
				return localized.Substring(num + 1, num2 - num - 1).Trim();
			}
			num = localized.LastIndexOf('(');
			num2 = localized.LastIndexOf(')');
			if (num >= 0 && num2 > num)
			{
				return localized.Substring(num + 1, num2 - num - 1).Trim();
			}
			return null;
		}

		internal static void UpdateTooltip(GameItem pack)
		{
			if (pack == null)
			{
				return;
			}
			try
			{
				StringBuilder stringBuilder = new StringBuilder(256);
				stringBuilder.Append("拖钥匙卡到本包存入；点按钮取出（同网格）；拖本包到上锁箱可开锁（优先对应卡，否则指挥卡）。");
				stringBuilder.Append('\n');
				stringBuilder.Append("库存：");
				SlotSpec[] slots = Slots;
				for (int i = 0; i < slots.Length; i++)
				{
					SlotSpec spec = slots[i];
					int value = KeycardPackCounts.Get(pack, spec.KeycardId);
					stringBuilder.Append('\n');
					stringBuilder.Append(DisplayName(spec)).Append('×').Append(value);
				}
				pack.flavorText = stringBuilder.ToString();
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack UpdateTooltip: " + ex.Message);
			}
		}

		internal static void RefreshButtons(GameItem pack)
		{
			if (pack == null)
			{
				return;
			}
			try
			{
				if (!TryGetButtonMap(pack, out Dictionary<string, AdvButtonElement> map) || map == null)
				{
					if (!_rebuilding)
					{
						if (Mod.Verbose)
						{
							Mod.Log.Msg("KeycardPack RefreshButtons: 无按钮引用，重建窗口");
						}
						ForceRebuildWindow(pack, "RefreshMiss");
					}
					return;
				}
				StoreButtonMap(pack, map);
				TryGetLabelMap(pack, out Dictionary<string, TagElement> map2);
				SlotSpec[] slots = Slots;
				for (int i = 0; i < slots.Length; i++)
				{
					SlotSpec spec = slots[i];
					int count = KeycardPackCounts.Get(pack, spec.KeycardId);
					if (map2 == null || !map2.TryGetValue(spec.KeycardId, out var value) || value == null)
					{
						continue;
					}
					string text = LabelText(DisplayName(spec), count);
					try
					{
						value.SetText(text, 100, (ColorPalette)12895408);
					}
					catch
					{
						try
						{
							value.SetText(text, 100, (ColorPalette)0);
						}
						catch
						{
						}
					}
				}
				UpdateTooltip(pack);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack RefreshButtons: " + ex.Message);
			}
		}

		private static void ForceRebuildWindow(GameItem pack, string ctx)
		{
			if (pack == null || _rebuilding)
			{
				return;
			}
			_rebuilding = true;
			try
			{
				ForgetButtonMap(pack);
				ForgetLabelMap(pack);
				try
				{
					pack.SetContentWindow((PixelWindow)null);
				}
				catch
				{
					try
					{
						pack.contentWindow = null;
					}
					catch
					{
					}
				}
				try
				{
					pack.DisableTag("KEYCARD_PACK_BUTTON_UI_V11", true);
				}
				catch
				{
					try
					{
						pack.DisableTag("KEYCARD_PACK_BUTTON_UI_V11", false);
					}
					catch
					{
					}
				}
				EnsureWindow(pack, ctx);
			}
			finally
			{
				_rebuilding = false;
			}
		}

		private static void StoreButtonMap(GameItem pack, Dictionary<string, AdvButtonElement> map)
		{
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer != IntPtr.Zero)
				{
					ButtonsByPtr[pointer] = map;
				}
			}
			catch
			{
			}
			try
			{
				int uniqueId = pack.uniqueId;
				if (uniqueId != 0)
				{
					ButtonsByUid[uniqueId] = map;
				}
			}
			catch
			{
			}
		}

		private static bool TryGetButtonMap(GameItem pack, out Dictionary<string, AdvButtonElement>? map)
		{
			map = null;
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer != IntPtr.Zero && ButtonsByPtr.TryGetValue(pointer, out map) && map != null)
				{
					return true;
				}
			}
			catch
			{
			}
			try
			{
				int uniqueId = pack.uniqueId;
				if (uniqueId != 0 && ButtonsByUid.TryGetValue(uniqueId, out map) && map != null)
				{
					return true;
				}
			}
			catch
			{
			}
			map = null;
			return false;
		}

		private static void StoreLabelMap(GameItem pack, Dictionary<string, TagElement> map)
		{
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer != IntPtr.Zero)
				{
					LabelsByPtr[pointer] = map;
				}
			}
			catch
			{
			}
			try
			{
				int uniqueId = pack.uniqueId;
				if (uniqueId != 0)
				{
					LabelsByUid[uniqueId] = map;
				}
			}
			catch
			{
			}
		}

		private static bool TryGetLabelMap(GameItem pack, out Dictionary<string, TagElement>? map)
		{
			map = null;
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer != IntPtr.Zero && LabelsByPtr.TryGetValue(pointer, out map) && map != null)
				{
					return true;
				}
			}
			catch
			{
			}
			try
			{
				int uniqueId = pack.uniqueId;
				if (uniqueId != 0 && LabelsByUid.TryGetValue(uniqueId, out map) && map != null)
				{
					return true;
				}
			}
			catch
			{
			}
			map = null;
			return false;
		}

		private static void ForgetLabelMap(GameItem pack)
		{
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer != IntPtr.Zero)
				{
					LabelsByPtr.Remove(pointer);
				}
			}
			catch
			{
			}
			try
			{
				int uniqueId = pack.uniqueId;
				if (uniqueId != 0)
				{
					LabelsByUid.Remove(uniqueId);
				}
			}
			catch
			{
			}
		}

		private static void ForgetButtonMap(GameItem pack)
		{
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer != IntPtr.Zero)
				{
					ButtonsByPtr.Remove(pointer);
				}
			}
			catch
			{
			}
			try
			{
				int uniqueId = pack.uniqueId;
				if (uniqueId != 0)
				{
					ButtonsByUid.Remove(uniqueId);
				}
			}
			catch
			{
			}
		}

		private static Action MakeAction(Action action)
		{
			Action val = DelegateSupport.ConvertDelegate<Action>((Delegate)action) ?? Action.op_Implicit(action);
			RootedActions.Add(val);
			return val;
		}

		private static bool LooksLikeLocError(string? s)
		{
			if (!string.IsNullOrEmpty(s))
			{
				if (!s.StartsWith("Translation Error", StringComparison.Ordinal))
				{
					return s.Contains("item_keycard_pack_name", StringComparison.Ordinal);
				}
				return true;
			}
			return false;
		}
	}
	internal static class KeycardPackHighlight
	{
		internal enum HoverRoute
		{
			None,
			Raycast,
			GroupHover,
			DragLast
		}

		internal const BackgroundColor HighlightColor = (BackgroundColor)2;

		private static readonly List<GameItem> KnownPacks = new List<GameItem>();

		private static readonly Dictionary<nint, HighlightManager> Managers = new Dictionary<IntPtr, HighlightManager>();

		private static readonly Dictionary<nint, GameGridInventory> PaintedGrids = new Dictionary<IntPtr, GameGridInventory>();

		private static readonly List<nint> PaintedPackPtrs = new List<IntPtr>();

		private static bool _lit;

		private static string _lastWhy = "";

		private static float _lastWhyAt = -10f;

		internal static GameItem? TrackedPack { get; private set; }

		internal static GameItem? LastDraggedItem { get; set; }

		internal static string RouteName(HoverRoute route)
		{
			return route switch
			{
				HoverRoute.Raycast => "raycast", 
				HoverRoute.GroupHover => "群拖 hoverItem", 
				HoverRoute.DragLast => "单件 lastItem", 
				_ => "没命中", 
			};
		}

		internal static void TrackPack(GameItem pack)
		{
			TrackedPack = pack;
			if (pack == null)
			{
				return;
			}
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer == IntPtr.Zero)
				{
					return;
				}
				for (int i = 0; i < KnownPacks.Count; i++)
				{
					try
					{
						if (KnownPacks[i] != null && ((Il2CppObjectBase)KnownPacks[i]).Pointer == pointer)
						{
							return;
						}
					}
					catch
					{
					}
				}
				KnownPacks.Add(pack);
			}
			catch
			{
			}
		}

		internal static void Clear()
		{
			LastDraggedItem = null;
		}

		internal static void Tick()
		{
			if (!KeycardPackConfig.DragHighlight)
			{
				ClearPaint();
				return;
			}
			try
			{
				List<GameItem> list = FindHighlightTargets(out string why);
				if (list.Count > 0)
				{
					Paint(list, out string how);
					if (!_lit)
					{
						_lit = true;
						_lastWhy = "";
						if (Mod.Verbose)
						{
							Mod.Log.Msg($"KeycardPack 蓝格：点亮 {list.Count} 个包（取槽走 {how}）");
						}
					}
				}
				else
				{
					ClearPaint();
					ReportWhy(why);
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack Highlight Tick: " + ex.Message);
				ClearPaint();
			}
		}

		private static void ReportWhy(string why)
		{
			if (Mod.Verbose && !string.IsNullOrEmpty(why) && !string.Equals(why, _lastWhy, StringComparison.Ordinal))
			{
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (!(realtimeSinceStartup - _lastWhyAt < 1f))
				{
					_lastWhy = why;
					_lastWhyAt = realtimeSinceStartup;
					Mod.Log.Msg("KeycardPack 蓝格：不亮 —— " + why);
				}
			}
		}

		internal static GameItem? FindPackUnderMouse(out HoverRoute route)
		{
			//IL_0003: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			route = HoverRoute.None;
			try
			{
				Vector3 mousePosition = Input.mousePosition;
				GameItem val = RenderHandler.RaycastElement<GameItem>(new Vector2(mousePosition.x, mousePosition.y), (TreeNodeRender)null);
				if (KeycardPackFactory.IsPack(val))
				{
					route = HoverRoute.Raycast;
					return val;
				}
			}
			catch (Exception ex)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack 蓝格 raycast 失败: " + ex.Message);
				}
			}
			try
			{
				ItemMultiSelectHandler current = ItemMultiSelectHandler.current;
				GameItem val2 = ((current != null) ? current.hoverItem : null);
				if (KeycardPackFactory.IsPack(val2))
				{
					route = HoverRoute.GroupHover;
					return val2;
				}
			}
			catch (Exception ex2)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack 蓝格读 hoverItem 失败: " + ex2.Message);
				}
			}
			try
			{
				ItemMouseDragHandler current2 = ItemMouseDragHandler.current;
				if ((Object)(object)current2 != (Object)null && current2.isDragging)
				{
					GameItem lastItem = current2.lastItem;
					if (KeycardPackFactory.IsPack(lastItem))
					{
						route = HoverRoute.DragLast;
						return lastItem;
					}
				}
			}
			catch (Exception ex3)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack 蓝格读 lastItem 失败: " + ex3.Message);
				}
			}
			return null;
		}

		private static List<GameItem> FindHighlightTargets(out string why)
		{
			List<GameItem> list = new List<GameItem>();
			why = "";
			List<string> list2 = new List<string>();
			HeldAcceptableIds(list2, out string how);
			if (list2.Count == 0)
			{
				why = how;
				return list;
			}
			PruneKnownPacks();
			if (KnownPacks.Count == 0)
			{
				GameItem val = FindPackUnderMouse(out var _);
				if (val != null)
				{
					TrackPack(val);
				}
			}
			for (int i = 0; i < KnownPacks.Count; i++)
			{
				GameItem val2 = KnownPacks[i];
				if (val2 == null)
				{
					continue;
				}
				try
				{
					if (((Il2CppObjectBase)val2).WasCollected)
					{
						continue;
					}
				}
				catch
				{
				}
				if (GridOf(val2) == null)
				{
					continue;
				}
				for (int j = 0; j < list2.Count; j++)
				{
					if (KeycardPackCounts.CanAccept(val2, list2[j]))
					{
						list.Add(val2);
						break;
					}
				}
			}
			if (list.Count == 0)
			{
				why = $"手里有 {list2.Count} 种可存卡（{how}），但没有能收的卡包（不在网格上 / 该种已满）";
			}
			return list;
		}

		private static void HeldAcceptableIds(List<string> ids, out string how)
		{
			how = "手上没在拖东西";
			try
			{
				ItemMultiSelectHandler current = ItemMultiSelectHandler.current;
				if ((Object)(object)current != (Object)null)
				{
					List<GameItemElement> selectedItems = current.selectedItems;
					if (selectedItems != null)
					{
						for (int i = 0; i < selectedItems.Count; i++)
						{
							GameItemElement val = null;
							try
							{
								val = selectedItems[i];
							}
							catch
							{
								continue;
							}
							if (val == null)
							{
								continue;
							}
							try
							{
								if (((Il2CppObjectBase)val).WasCollected)
								{
									continue;
								}
							}
							catch
							{
							}
							bool flag = false;
							try
							{
								flag = current.IsGroupDraggingItem((GameItem)(object)val);
							}
							catch
							{
							}
							if (flag)
							{
								string text = SafeId((GameItem?)(object)val);
								if (KeycardPackCounts.IsKnownKeycardId(text) && !ids.Contains(text))
								{
									ids.Add(text);
								}
							}
						}
						if (ids.Count > 0)
						{
							how = "群拖";
							return;
						}
					}
				}
			}
			catch (Exception ex)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack Highlight 读选中列表失败: " + ex.Message);
				}
			}
			try
			{
				ItemMouseDragHandler current2 = ItemMouseDragHandler.current;
				if ((Object)(object)current2 != (Object)null && current2.isDragging)
				{
					string text2 = SafeId(current2.GetCurrentItem());
					if (KeycardPackCounts.IsKnownKeycardId(text2))
					{
						ids.Add(text2);
						how = "单件拖拽";
					}
					else
					{
						how = "单件拖拽，但手上是 '" + text2 + "'（不是能进包的卡）";
					}
				}
			}
			catch (Exception ex2)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPack Highlight 读拖拽物失败: " + ex2.Message);
				}
			}
		}

		private static void PruneKnownPacks()
		{
			for (int num = KnownPacks.Count - 1; num >= 0; num--)
			{
				try
				{
					if (KnownPacks[num] == null || ((Il2CppObjectBase)KnownPacks[num]).WasCollected)
					{
						KnownPacks.RemoveAt(num);
					}
				}
				catch
				{
					KnownPacks.RemoveAt(num);
				}
			}
		}

		private static GameGridInventory? GridOf(GameItem pack)
		{
			try
			{
				object result;
				if (pack == null)
				{
					result = null;
				}
				else
				{
					GameInventory parentInventory = pack.parentInventory;
					result = ((parentInventory != null) ? ((Il2CppObjectBase)parentInventory).TryCast<GameGridInventory>() : null);
				}
				return (GameGridInventory?)result;
			}
			catch
			{
				return null;
			}
		}

		private static void Paint(List<GameItem> packs, out string how)
		{
			how = "";
			if (!SamePackSet(packs))
			{
				ClearPaint();
			}
			PaintedPackPtrs.Clear();
			for (int i = 0; i < packs.Count; i++)
			{
				try
				{
					PaintedPackPtrs.Add(((Il2CppObjectBase)packs[i]).Pointer);
				}
				catch
				{
				}
			}
			for (int j = 0; j < packs.Count; j++)
			{
				GameItem val = packs[j];
				GameGridInventory val2 = GridOf(val);
				if (val2 == null)
				{
					continue;
				}
				SlotMarker val3 = ResolveSlot(val, val2, out string how2);
				if (val3 == null)
				{
					continue;
				}
				HighlightManager val4 = ManagerFor(val);
				try
				{
					((GameInventory)val2).HighlightSlot(val3, (BackgroundColor)2, true);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("KeycardPack HighlightSlot: " + ex.Message);
				}
				if (val4 != null)
				{
					try
					{
						val4.Try(val3, (BackgroundColor)2);
					}
					catch (Exception ex2)
					{
						Mod.Log.Warning("KeycardPack HighlightManager.Try: " + ex2.Message);
					}
				}
				try
				{
					PaintedGrids[((Il2CppObjectBase)val2).Pointer] = val2;
				}
				catch
				{
				}
				if (Mod.Verbose && !_lit)
				{
					how = (string.IsNullOrEmpty(how) ? how2 : how);
					Mod.Log.Msg($"KeycardPack 蓝格定位：走 {how2}；slot[{DescribeSlot(val3)}]；包 modifiedShape={DescribeShape(val.modifiedShape)} shape={DescribeShape(val.shape)}");
				}
				else if (string.IsNullOrEmpty(how))
				{
					how = how2;
				}
			}
		}

		private static bool SamePackSet(List<GameItem> packs)
		{
			if (packs.Count != PaintedPackPtrs.Count)
			{
				return false;
			}
			for (int i = 0; i < packs.Count; i++)
			{
				IntPtr pointer;
				try
				{
					pointer = ((Il2CppObjectBase)packs[i]).Pointer;
				}
				catch
				{
					return false;
				}
				if (!PaintedPackPtrs.Contains(pointer))
				{
					return false;
				}
			}
			return true;
		}

		private static HighlightManager? ManagerFor(GameItem pack)
		{
			//IL_002a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Expected Obj, but got Unknown
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)pack).Pointer;
				if (pointer == IntPtr.Zero)
				{
					return null;
				}
				if (!Managers.TryGetValue(pointer, out HighlightManager value) || value == null)
				{
					value = new HighlightManager();
					Managers[pointer] = value;
				}
				return value;
			}
			catch
			{
				return null;
			}
		}

		private static SlotMarker? ResolveSlot(GameItem pack, GameGridInventory grid, out string how)
		{
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Expected Obj, but got Unknown
			how = "";
			try
			{
				try
				{
					SlotMarker incompleteSlotInInventory = pack.GetIncompleteSlotInInventory();
					if (incompleteSlotInInventory != null && incompleteSlotInInventory.item != null && incompleteSlotInInventory.itemGridShape != null)
					{
						how = "GetIncompleteSlotInInventory";
						return incompleteSlotInInventory;
					}
				}
				catch (Exception ex)
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPack Highlight GetIncompleteSlotInInventory: " + ex.Message);
					}
				}
				try
				{
					SlotMarker incompleteItemInventorySlot = ((GameInventory)grid).GetIncompleteItemInventorySlot(pack);
					if (incompleteItemInventorySlot != null && incompleteItemInventorySlot.item != null && incompleteItemInventorySlot.itemGridShape != null)
					{
						how = "grid.GetIncompleteItemInventorySlot";
						return incompleteItemInventorySlot;
					}
				}
				catch (Exception ex2)
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPack Highlight grid.GetIncompleteItemInventorySlot: " + ex2.Message);
					}
				}
				GridShape val = pack.modifiedShape ?? pack.shape;
				if (val == null)
				{
					return null;
				}
				how = "手工 SlotMarker";
				return new SlotMarker
				{
					item = pack,
					inventory = (GameInventory)(object)grid,
					itemGridShape = val,
					index = 0,
					numTransfer = ((pack.unitCount <= 0) ? 1 : pack.unitCount)
				};
			}
			catch (Exception ex3)
			{
				Mod.Log.Warning("KeycardPack Highlight ResolveSlot: " + ex3.Message);
				return null;
			}
		}

		private static string DescribeShape(GridShape? shape)
		{
			if (shape == null)
			{
				return "null";
			}
			try
			{
				return $"{shape.width}x{shape.height} 全局{shape.globalWidth}x{shape.globalHeight} 原点({shape.minX},{shape.minY})";
			}
			catch
			{
				return "?";
			}
		}

		private static string DescribeSlot(SlotMarker? slot)
		{
			if (slot == null)
			{
				return "null";
			}
			try
			{
				return $"item='{SafeId(slot.item)}' index={slot.index} shape={DescribeShape(slot.itemGridShape)}";
			}
			catch
			{
				return "?";
			}
		}

		private static void ClearPaint()
		{
			if (PaintedGrids.Count == 0 && PaintedPackPtrs.Count == 0)
			{
				return;
			}
			foreach (HighlightManager value in Managers.Values)
			{
				try
				{
					if (value != null)
					{
						value.Clear();
					}
				}
				catch
				{
				}
			}
			foreach (GameGridInventory value2 in PaintedGrids.Values)
			{
				try
				{
					if (value2 != null)
					{
						((GameInventory)value2).ClearHighlight();
					}
				}
				catch
				{
				}
			}
			PaintedGrids.Clear();
			PaintedPackPtrs.Clear();
			_lit = false;
		}

		private static string SafeId(GameItem? item)
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
	}
	internal static class KeycardPackPatch
	{
		private static Func<GameItem>? _factory;

		private static GameItem? _pendingDepositPack;

		private static GameItem? _pendingDepositCard;

		private static GameItem? _pendingUnlockPack;

		private static GameItem? _pendingUnlockBox;

		private static GameGridInventory? _pendingUnlockHome;

		private static readonly Queue<Action> Deferred = new Queue<Action>();

		internal static void Apply(Harmony harmony)
		{
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Expected Obj, but got Unknown
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f9: Expected Obj, but got Unknown
			KeycardPackSprite.Apply(harmony);
			KeycardPackThiefShop.Apply(harmony);
			KeycardPackBatch.Apply(harmony);
			PatchDirectory(harmony, typeof(MiscItemDirectory));
			PatchDirectory(harmony, typeof(ModItemDirectory));
			PatchDirectory(harmony, typeof(ContainerItemDirectory));
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(ItemMouseDragHandler), "EndDrag", (Type[])null, (Type[])null);
				if (methodInfo != null)
				{
					harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(KeycardPackPatch), "PrefixEndDragNotePending", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPack: EndDrag Prefix 登记存入/开箱");
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack EndDrag: " + ex.Message);
			}
			try
			{
				MethodInfo methodInfo2 = AccessTools.Method(typeof(ItemMouseDoubleClickHandler), "OpenContentAction", (Type[])null, (Type[])null);
				if (methodInfo2 != null)
				{
					harmony.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(typeof(KeycardPackPatch), "PostfixOpenContentAction", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPack: 已挂钩 OpenContentAction（开卡包窗口刷新计数）");
					}
				}
				else
				{
					Mod.Log.Warning("KeycardPack: ItemMouseDoubleClickHandler.OpenContentAction 未找到");
				}
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("KeycardPack OpenContentAction: " + ex2.Message);
			}
			if (Mod.Verbose)
			{
				Mod.Log.Msg("KeycardPack hooks: Sprite + EndDrag + OpenContentAction + 框选批量存入 + 小偷随机货架");
			}
		}

		internal static void TickDeferred()
		{
			try
			{
				if (_pendingDepositPack != null && _pendingDepositCard != null)
				{
					GameItem pendingDepositPack = _pendingDepositPack;
					GameItem pendingDepositCard = _pendingDepositCard;
					_pendingDepositPack = null;
					_pendingDepositCard = null;
					try
					{
						if (KeycardPackFactory.TryDeposit(pendingDepositPack, pendingDepositCard))
						{
							Mod.Log.Msg("KeycardPack deferred: 存入成功");
						}
						else if (Mod.Verbose)
						{
							Mod.Log.Msg("KeycardPack deferred: 存入未执行（条件不符）");
						}
					}
					catch (Exception ex)
					{
						Mod.Log.Warning("KeycardPack deferred deposit: " + ex.Message);
					}
					try
					{
						KeycardPackHighlight.Clear();
					}
					catch
					{
					}
				}
				if (_pendingUnlockPack != null && _pendingUnlockBox != null)
				{
					GameItem pendingUnlockPack = _pendingUnlockPack;
					GameItem pendingUnlockBox = _pendingUnlockBox;
					GameGridInventory pendingUnlockHome = _pendingUnlockHome;
					_pendingUnlockPack = null;
					_pendingUnlockBox = null;
					_pendingUnlockHome = null;
					try
					{
						if (KeycardPackUnlock.TryUnlockWithPack(pendingUnlockPack, pendingUnlockBox, pendingUnlockHome))
						{
							Mod.Log.Msg("KeycardPack deferred: 开箱成功（并尝试归位）");
						}
						else if (Mod.Verbose)
						{
							Mod.Log.Msg("KeycardPack deferred: 开箱未执行（仍尝试归位）");
						}
					}
					catch (Exception ex2)
					{
						Mod.Log.Warning("KeycardPack deferred unlock: " + ex2.Message);
						try
						{
							KeycardPackUnlock.RestorePackToHome(pendingUnlockPack, pendingUnlockHome, pendingUnlockBox);
						}
						catch
						{
						}
					}
					try
					{
						KeycardPackHighlight.Clear();
					}
					catch
					{
					}
				}
				while (Deferred.Count > 0)
				{
					Action action = null;
					try
					{
						action = Deferred.Dequeue();
					}
					catch
					{
						break;
					}
					try
					{
						action?.Invoke();
					}
					catch (Exception ex3)
					{
						Mod.Log.Warning("KeycardPack deferred action: " + ex3.Message);
					}
				}
			}
			catch (Exception ex4)
			{
				Mod.Log.Warning("KeycardPack TickDeferred: " + ex4.Message);
			}
		}

		internal static void Defer(Action action)
		{
			if (action != null)
			{
				Deferred.Enqueue(action);
			}
		}

		private static void PatchDirectory(Harmony harmony, Type directoryType)
		{
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Expected Obj, but got Unknown
			MethodInfo methodInfo = AccessTools.Method(directoryType, "InitDirectory", (Type[])null, (Type[])null);
			if (methodInfo == null)
			{
				Mod.Log.Warning("KeycardPack: " + directoryType.Name + ".InitDirectory 未找到");
				return;
			}
			harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(KeycardPackPatch), "PostfixInitDirectory", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			if (Mod.Verbose)
			{
				Mod.Log.Msg("KeycardPack: patched " + directoryType.Name + ".InitDirectory");
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
				Mod.Log.Warning("KeycardPack register on " + ((object)__instance)?.GetType().Name + ": " + ex.Message);
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
				if (((Directory<GameItem>)(object)dir).Has("keycard_pack"))
				{
					return;
				}
			}
			catch
			{
			}
			if ((Delegate)(object)_factory == (Delegate)null)
			{
				Func<GameItem> func = KeycardPackFactory.Create;
				try
				{
					_factory = DelegateSupport.ConvertDelegate<Func<GameItem>>((Delegate)func);
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("KeycardPack ConvertDelegate: " + ex.Message);
					return;
				}
			}
			if (!((Delegate)(object)_factory == (Delegate)null))
			{
				if (!((Directory<GameItem>)(object)dir).Add("keycard_pack", _factory))
				{
					Mod.Log.Warning("KeycardPack " + ((object)dir).GetType().Name + ".Add 返回 false");
				}
				else
				{
					Mod.Log.Msg("KeycardPack registered on " + ((object)dir).GetType().Name);
				}
			}
		}

		public static void PrefixEndDragNotePending(ItemMouseDragHandler __instance)
		{
			try
			{
				GameItem val = TryGetDragItem(__instance, "GetCurrentItem") ?? KeycardPackHighlight.LastDraggedItem;
				GameItem val2 = TryGetDragItem(__instance, "get_lastItem") ?? TryGetDragItem(__instance, "lastItem");
				if (val == null || val2 == null)
				{
					return;
				}
				string text = SafeId(val);
				string text2 = SafeId(val2);
				if (string.Equals(text, "keycard_pack", StringComparison.OrdinalIgnoreCase))
				{
					bool flag = false;
					try
					{
						flag = LockHelper.IsLockedContainer(val2);
					}
					catch
					{
					}
					if (flag)
					{
						GameGridInventory val3 = null;
						try
						{
							GameInventory parentInventory = val.parentInventory;
							val3 = ((parentInventory != null) ? ((Il2CppObjectBase)parentInventory).TryCast<GameGridInventory>() : null);
						}
						catch
						{
							val3 = null;
						}
						_pendingUnlockPack = val;
						_pendingUnlockBox = val2;
						_pendingUnlockHome = val3;
						if (Mod.Verbose)
						{
							Mod.Log.Msg($"KeycardPack EndDrag Prefix: 登记待开箱 包 → '{text2}' home={((val3 == null) ? "null" : "grid")}");
						}
					}
				}
				else if (string.Equals(text2, "keycard_pack", StringComparison.OrdinalIgnoreCase) && KeycardPackCounts.IsKnownKeycardId(text))
				{
					_pendingDepositPack = val2;
					_pendingDepositCard = val;
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPack EndDrag Prefix: 登记待存入 '" + text + "' → 包");
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack PrefixEndDragNotePending: " + ex.Message);
			}
		}

		public static void PostfixOpenContentAction(GameItem __0)
		{
			try
			{
				if (KeycardPackFactory.IsPack(__0))
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPack: 打开卡包窗口 → 登记刷新计数");
					}
					Defer(() =>
					{
						KeycardPackFactory.RefreshButtons(__0);
					});
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPack PostfixOpenContentAction: " + ex.Message);
			}
		}

		private static GameItem? TryGetDragItem(ItemMouseDragHandler handler, string name)
		{
			try
			{
				MethodInfo method = ((object)handler).GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (method != null)
				{
					object? obj = method.Invoke(handler, null);
					return (GameItem?)((obj is GameItem) ? obj : null);
				}
				PropertyInfo property = ((object)handler).GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null)
				{
					object? value = property.GetValue(handler);
					return (GameItem?)((value is GameItem) ? value : null);
				}
				FieldInfo field = ((object)handler).GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null)
				{
					object? value2 = field.GetValue(handler);
					return (GameItem?)((value2 is GameItem) ? value2 : null);
				}
			}
			catch
			{
			}
			return null;
		}

		private static string SafeId(GameItem? item)
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
	}
	internal static class KeycardPackSprite
	{
		internal const string SpriteName = "keycard_pack";

		private const string ResourceName = "keycard_pack.png";

		private static Sprite? _sprite;

		private static bool _hooked;

		internal static void Apply(Harmony harmony)
		{
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Expected Obj, but got Unknown
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Expected Obj, but got Unknown
			if (_hooked)
			{
				return;
			}
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(GameItemElement), "ResolveSpriteByName", new Type[1] { typeof(string) }, (Type[])null);
				if (methodInfo != null)
				{
					harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(KeycardPackSprite), "PrefixResolveSpriteByName", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPackSprite: ResolveSpriteByName 已挂钩");
					}
				}
				else
				{
					Mod.Log.Warning("KeycardPackSprite: ResolveSpriteByName 未找到");
				}
				MethodInfo methodInfo2 = AccessTools.Method(typeof(GameItemElement), "ResolveItemSprite", (Type[])null, (Type[])null);
				if (methodInfo2 != null)
				{
					harmony.Patch((MethodBase)methodInfo2, new HarmonyMethod(typeof(KeycardPackSprite), "PrefixResolveItemSprite", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPackSprite: ResolveItemSprite 已挂钩");
					}
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackSprite Apply: " + ex.Message);
			}
			_hooked = true;
		}

		public static bool PrefixResolveSpriteByName(string __0, ref Sprite __result)
		{
			if (!string.Equals(__0, "keycard_pack", StringComparison.OrdinalIgnoreCase) && !string.Equals(__0, "keycard_pack", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			Sprite orCreate = GetOrCreate();
			if ((Object)(object)orCreate == (Object)null)
			{
				return true;
			}
			__result = orCreate;
			return false;
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
				if (!string.Equals(a, "keycard_pack", StringComparison.OrdinalIgnoreCase) && !string.Equals(a, "keycard_pack", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				Sprite orCreate = GetOrCreate();
				if ((Object)(object)orCreate == (Object)null)
				{
					return true;
				}
				__result = orCreate;
				return false;
			}
			catch
			{
				return true;
			}
		}

		internal static void ApplyToItem(GameItem item)
		{
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			if (item == null)
			{
				return;
			}
			try
			{
				item.spritePath = "keycard_pack";
				item.spriteAtlasPath = "";
				item.spriteChanged = true;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackSprite fields: " + ex.Message);
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
			catch (Exception ex2)
			{
				Mod.Log.Warning("KeycardPackSprite useSpriteFromMod: " + ex2.Message);
			}
			try
			{
				GridShape shape = new GridShapeBuilder(2, 2).SetDataFill(2, 2, (byte)1).Build();
				item.SetShape(shape);
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPackSprite: 已设 2×2 shape + 黑皮卡夹精灵");
				}
			}
			catch (Exception ex3)
			{
				Mod.Log.Warning("KeycardPackSprite SetShape 2x2: " + ex3.Message);
			}
			GetOrCreate();
		}

		private static Sprite? GetOrCreate()
		{
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected Obj, but got Unknown
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_sprite != (Object)null)
			{
				return _sprite;
			}
			try
			{
				byte[] array = LoadPngBytes();
				if (array == null || array.Length == 0)
				{
					Mod.Log.Warning("KeycardPackSprite: 找不到内嵌/旁路 PNG");
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
					Mod.Log.Warning("KeycardPackSprite: ImageConversion.LoadImage 失败");
					return null;
				}
				((Texture)val).filterMode = (FilterMode)0;
				((Texture)val).wrapMode = (TextureWrapMode)1;
				((Object)val).hideFlags = (HideFlags)61;
				if (((Texture)val).width != 32 || ((Texture)val).height != 32)
				{
					Mod.Log.Warning($"KeycardPackSprite: 期望 32×32，实际 {((Texture)val).width}×{((Texture)val).height}（显示会偏大）");
				}
				_sprite = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
				((Object)_sprite).name = "keycard_pack";
				((Object)_sprite).hideFlags = (HideFlags)61;
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"KeycardPackSprite: 已加载黑皮卡夹 {((Texture)val).width}×{((Texture)val).height} ppu=100");
				}
				return _sprite;
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackSprite GetOrCreate: " + ex.Message);
				return null;
			}
		}

		private static byte[]? LoadPngBytes()
		{
			try
			{
				Assembly executingAssembly = Assembly.GetExecutingAssembly();
				string[] manifestResourceNames = executingAssembly.GetManifestResourceNames();
				foreach (string text in manifestResourceNames)
				{
					if (!text.EndsWith("keycard_pack.png", StringComparison.OrdinalIgnoreCase) && !text.Equals("keycard_pack.png", StringComparison.OrdinalIgnoreCase))
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
				Mod.Log.Warning("KeycardPackSprite embed: " + ex.Message);
			}
			try
			{
				string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
				string path2 = Path.Combine(path, "KeycardPack", "keycard_pack.png");
				if (!File.Exists(path2))
				{
					path2 = Path.Combine(path, "keycard_pack.png");
				}
				if (File.Exists(path2))
				{
					return File.ReadAllBytes(path2);
				}
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("KeycardPackSprite file: " + ex2.Message);
			}
			return null;
		}
	}
	internal static class KeycardPackThiefShop
	{
		private const string ThiefId = "thief";

		private static bool _busy;

		private static int _rolledVisitKey = int.MinValue;

		internal static void Apply(Harmony harmony)
		{
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Expected Obj, but got Unknown
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
					Mod.Log.Warning("KeycardPackThiefShop: AddDirectSellingItemToTable 未找到");
					return;
				}
				harmony.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(KeycardPackThiefShop), "PostfixAddDirect", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"KeycardPackThiefShop: 已挂钩 AddDirectSellingItemToTable（base={KeycardPackConfig.ThiefStockChance}→Roll(int)）");
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackThiefShop Apply: " + ex.Message);
			}
		}

		public static void PostfixAddDirect(PlayerStore __instance, GameItem gameItem, bool isOwend, bool isStolen, bool ignorePerk, int forcedHeat)
		{
			if (_busy || __instance == null || gameItem == null)
			{
				return;
			}
			try
			{
				string a = TryGetSellerId(__instance);
				string text = null;
				try
				{
					text = gameItem.identifier;
				}
				catch
				{
				}
				bool flag = string.Equals(a, "thief", StringComparison.OrdinalIgnoreCase);
				bool flag2 = isStolen && IsThiefStockKeycard(text);
				if (!flag && !flag2)
				{
					return;
				}
				int num = TryGetVisitKey(__instance);
				if (num == _rolledVisitKey)
				{
					return;
				}
				_rolledVisitKey = num;
				float num2 = 1f;
				try
				{
					num2 = StoreClientList.GetBMRepStockMult();
				}
				catch
				{
				}
				int num3 = KeycardPackConfig.ToStockPercent(num2);
				string value = (flag ? "seller=thief" : ("stolenKeycard=" + text));
				if (num3 <= 0)
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg($"KeycardPackThiefShop: 跳过 percent=0（base={KeycardPackConfig.ThiefStockChance} mult={num2:0.###} {value}）");
					}
					return;
				}
				if (!RNG.Roll(num3))
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg($"KeycardPackThiefShop: 未掷中 percent={num3}（base={KeycardPackConfig.ThiefStockChance} mult={num2:0.###} {value}）");
					}
					return;
				}
				GameItem val = DirectoryMaster.Item("keycard_pack", true);
				if (val == null)
				{
					Mod.Log.Warning("KeycardPackThiefShop: DirectoryMaster.Item(keycard_pack) null");
					return;
				}
				_busy = true;
				try
				{
					__instance.AddDirectSellingItemToTable(val, false, true, false, 0);
					Mod.Log.Msg($"KeycardPackThiefShop: 已上架钥匙卡包 percent={num3}（base={KeycardPackConfig.ThiefStockChance} mult={num2:0.###} {value}）");
				}
				finally
				{
					_busy = false;
				}
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackThiefShop postfix: " + ex.Message);
				_busy = false;
			}
		}

		private static bool IsThiefStockKeycard(string? id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			if (!string.Equals(id, "sec_keycard", StringComparison.OrdinalIgnoreCase) && !string.Equals(id, "cmd_keycard", StringComparison.OrdinalIgnoreCase) && !string.Equals(id, "med_keycard", StringComparison.OrdinalIgnoreCase) && !string.Equals(id, "eng_keycard", StringComparison.OrdinalIgnoreCase) && !string.Equals(id, "sup_keycard", StringComparison.OrdinalIgnoreCase) && !string.Equals(id, "ser_keycard", StringComparison.OrdinalIgnoreCase) && !string.Equals(id, "sci_keycard", StringComparison.OrdinalIgnoreCase))
			{
				return string.Equals(id, "blank_keycard", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		private static string? TryGetSellerId(PlayerStore store)
		{
			try
			{
				StoreClientInstance currentClientInstance = store.currentClientInstance;
				StoreClient obj = ((currentClientInstance != null) ? currentClientInstance.GetClientBlueprint() : null);
				string text = ((obj != null) ? obj.identifier : null);
				if (!string.IsNullOrEmpty(text))
				{
					return text;
				}
			}
			catch
			{
			}
			try
			{
				StoreClientInstance currentClientInstance2 = store.currentClientInstance;
				if (currentClientInstance2 == null)
				{
					return null;
				}
				Type type = ((object)currentClientInstance2).GetType();
				PropertyInfo propertyInfo = AccessTools.Property(type, "identifier") ?? AccessTools.Property(type, "Identifier");
				if (propertyInfo != null)
				{
					string text2 = propertyInfo.GetValue(currentClientInstance2, null) as string;
					if (!string.IsNullOrEmpty(text2))
					{
						return text2;
					}
				}
				FieldInfo fieldInfo = AccessTools.Field(type, "identifier") ?? AccessTools.Field(type, "Identifier");
				if (fieldInfo != null)
				{
					string text3 = fieldInfo.GetValue(currentClientInstance2) as string;
					if (!string.IsNullOrEmpty(text3))
					{
						return text3;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private static int TryGetVisitKey(PlayerStore store)
		{
			try
			{
				StoreClientInstance currentClientInstance = store.currentClientInstance;
				if (currentClientInstance != null)
				{
					return ((Object)currentClientInstance).GetHashCode();
				}
			}
			catch
			{
			}
			return Time.frameCount;
		}
	}
	internal static class KeycardPackUnlock
	{
		internal const string CmdKeycardId = "cmd_keycard";

		internal const string CmdLockId = "LOCK_ID_COMMAND";

		private static readonly Dictionary<string, string> LockToKeycard = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["LOCK_ID_SEC"] = "sec_keycard",
			["LOCK_ID_SCI"] = "sci_keycard",
			["LOCK_ID_COMMAND"] = "cmd_keycard",
			["LOCK_ID_MED"] = "med_keycard",
			["LOCK_ID_ENG"] = "eng_keycard",
			["LOCK_ID_SER"] = "ser_keycard",
			["LOCK_ID_SUP"] = "sup_keycard"
		};

		internal static bool TryUnlockWithPack(GameItem pack, GameItem box, GameGridInventory? homeGrid)
		{
			if (pack == null || box == null)
			{
				return false;
			}
			try
			{
				if (!string.Equals(pack.identifier, "keycard_pack", StringComparison.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
			bool flag = false;
			try
			{
				return TryUnlockCore(pack, box);
			}
			finally
			{
				RestorePackToHome(pack, homeGrid, box);
			}
		}

		private static bool TryUnlockCore(GameItem pack, GameItem box)
		{
			bool flag;
			try
			{
				flag = LockHelper.IsLockedContainer(box);
			}
			catch (Exception ex)
			{
				Mod.Log.Warning("KeycardPackUnlock IsLockedContainer: " + ex.Message);
				return false;
			}
			if (!flag)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPackUnlock: 目标不是上锁箱");
				}
				return false;
			}
			string text;
			try
			{
				text = LockHelper.GetLock(box) ?? "";
			}
			catch (Exception ex2)
			{
				Mod.Log.Warning("KeycardPackUnlock GetLock: " + ex2.Message);
				return false;
			}
			if (string.IsNullOrEmpty(text))
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPackUnlock: GetLock 为空");
				}
				return false;
			}
			string text2 = null;
			string text3 = text;
			if (LockToKeycard.TryGetValue(text, out string value) && KeycardPackCounts.Get(pack, value) >= 1)
			{
				text2 = value;
				text3 = text;
			}
			else if (KeycardPackCounts.Get(pack, "cmd_keycard") >= 1)
			{
				text2 = "cmd_keycard";
				text3 = "LOCK_ID_COMMAND";
			}
			if (text2 == null)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg("KeycardPackUnlock: 无对应卡且无指挥卡（锁=" + text + "），不开");
				}
				return false;
			}
			bool flag2;
			try
			{
				flag2 = LockHelper.CanOpenLock(box, text3);
			}
			catch (Exception ex3)
			{
				Mod.Log.Warning("KeycardPackUnlock CanOpenLock: " + ex3.Message);
				return false;
			}
			if (!flag2)
			{
				if (Mod.Verbose)
				{
					Mod.Log.Msg($"KeycardPackUnlock: CanOpenLock 拒绝（锁={text}，尝试={text3}）");
				}
				return false;
			}
			try
			{
				LockHelper.UnlockContainer(box, false);
			}
			catch (Exception ex4)
			{
				Mod.Log.Warning("KeycardPackUnlock UnlockContainer: " + ex4.Message);
				return false;
			}
			int value2 = KeycardPackCounts.Add(pack, text2, -1);
			KeycardPackFactory.RefreshButtons(pack);
			KeycardPackFactory.UpdateTooltip(pack);
			string value3 = text2;
			KeycardPackFactory.SlotSpec[] slots = KeycardPackFactory.Slots;
			for (int i = 0; i < slots.Length; i++)
			{
				KeycardPackFactory.SlotSpec spec = slots[i];
				if (string.Equals(spec.KeycardId, text2, StringComparison.OrdinalIgnoreCase))
				{
					value3 = KeycardPackFactory.DisplayName(spec);
					break;
				}
			}
			Mod.Log.Msg($"KeycardPackUnlock: 已开箱 锁={text} 消耗={value3}({text2}) 剩余={value2}" + ((string.Equals(text2, "cmd_keycard", StringComparison.OrdinalIgnoreCase) && !string.Equals(matchedOrEmpty(text), "cmd_keycard", StringComparison.OrdinalIgnoreCase)) ? "（指挥兜底）" : ""));
			return true;
		}

		internal static void RestorePackToHome(GameItem pack, GameGridInventory? homeGrid, GameItem? box)
		{
			if (pack == null || homeGrid == null)
			{
				return;
			}
			try
			{
				GameGridInventory val = null;
				try
				{
					GameInventory parentInventory = pack.parentInventory;
					val = ((parentInventory != null) ? ((Il2CppObjectBase)parentInventory).TryCast<GameGridInventory>() : null);
				}
				catch
				{
				}
				if (val != null && SameInv(val, homeGrid))
				{
					if (Mod.Verbose)
					{
						Mod.Log.Msg("KeycardPackUnlock: 包仍在原网格，无需归位");
					}
					return;
				}
				try
				{
					GameInventory parentInventory2 = pack.parentInventory;
					if (parentInventory2 != null)
					{
						bool value = false;
						try
						{
							value = parentInventory2.Expel(pack);
						}
						catch
						{
						}
						if (Mod.Verbose)
						{
							Mod.Log.Msg($"KeycardPackUnlock: Expel 自当前父库存 ok={value}");
						}
					}
				}
				catch (Exception ex)
				{
					Mod.Log.Warning("KeycardPackUnlock Expel: " + ex.Message);
				}
				bool flag = false;
				try
				{
					GameInventory parentInventory3 = pack.parentInventory;
					if (parentInventory3 != null)
					{
						try
						{
							parentInventory3.Expel(pack);
						}
						catch
						{
						}
					}
					GraphUtils.TryAcceptAllMid(homeGrid, pack, Math.Max(1, pack.unitCount));
					GameInventory parentInventory4 = pack.parentInventory;
					GameGridInventory val2 = ((parentInventory4 != null) ? ((Il2CppObjectBase)parentInventory4).TryCast<GameGridInventory>() : null);
					flag = val2 != null && SameInv(val2, homeGrid);
				}
				catch (Exception ex2)
				{
					Mod.Log.Warning("KeycardPackUnlock TryAcceptAllMid home: " + ex2.Message);
				}
				if (flag)
				{
					Mod.Log.Msg("KeycardPackUnlock: 已归位到原网格");
				}
				else
				{
					Mod.Log.Warning("KeycardPackUnlock: 归位失败——包可能悬空，请手动摆回");
				}
			}
			catch (Exception ex3)
			{
				Mod.Log.Warning("KeycardPackUnlock RestorePackToHome: " + ex3.Message);
			}
		}

		private static bool SameInv(GameGridInventory a, GameGridInventory b)
		{
			try
			{
				return a != null && b != null && ((Il2CppObjectBase)a).Pointer == ((Il2CppObjectBase)b).Pointer;
			}
			catch
			{
				return false;
			}
		}

		private static string matchedOrEmpty(string lockId)
		{
			if (!LockToKeycard.TryGetValue(lockId, out string value))
			{
				return "";
			}
			return value;
		}
	}
	public sealed class Mod : MelonMod
	{
		internal static Instance Log;

		internal static bool Verbose;

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			KeycardPackConfig.Init();
			KeycardPackPatch.Apply(((MelonBase)this).HarmonyInstance);
			Log.Msg("KeycardPack 1.7.1 — 卡包精灵 PNG + 框选批量存入 + 拿起钥匙卡时卡包亮蓝（后两项可在 MelonPreferences 里关）");
		}

		public override void OnUpdate()
		{
			KeycardPackPatch.TickDeferred();
			KeycardPackHighlight.Tick();
		}
	}
}
