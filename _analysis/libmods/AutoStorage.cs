using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using AutoStorage;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Core), "AutoStorage", "2.2.0", "makoto", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
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
}
namespace AutoStorage
{
	internal static class U
	{
		public static IntPtr Ptr(Il2CppObjectBase o)
		{
			if (o == null)
			{
				return IntPtr.Zero;
			}
			try
			{
				return o.Pointer;
			}
			catch
			{
				return IntPtr.Zero;
			}
		}

		public static bool Same(Il2CppObjectBase a, Il2CppObjectBase b)
		{
			IntPtr intPtr = Ptr(a);
			IntPtr intPtr2 = Ptr(b);
			if (intPtr != IntPtr.Zero)
			{
				return intPtr == intPtr2;
			}
			return false;
		}

		public static string SafeId(GameItem it)
		{
			if (it == null)
			{
				return "";
			}
			try
			{
				if (!string.IsNullOrEmpty(it.identifier))
				{
					return it.identifier;
				}
			}
			catch
			{
			}
			try
			{
				return it.name ?? "";
			}
			catch
			{
			}
			return "";
		}

		public static string SafeName(GameItem it)
		{
			if (it == null)
			{
				return "";
			}
			try
			{
				string displayName = it.GetDisplayName(true);
				if (!string.IsNullOrEmpty(displayName))
				{
					return displayName;
				}
			}
			catch
			{
			}
			return SafeId(it);
		}

		public static string DefaultName(GameItem it)
		{
			if (it == null)
			{
				return "";
			}
			try
			{
				string displayName = it.GetDisplayName(false);
				if (!string.IsNullOrEmpty(displayName))
				{
					return displayName;
				}
			}
			catch
			{
			}
			return SafeId(it);
		}

		public static int SafeUid(GameItem it)
		{
			if (it == null)
			{
				return 0;
			}
			try
			{
				return it.uniqueId;
			}
			catch
			{
			}
			try
			{
				return it.GetUniqueID();
			}
			catch
			{
			}
			return 0;
		}

		public static string Join(string[] a)
		{
			if (a != null)
			{
				return string.Join(",", a);
			}
			return "";
		}
	}
	internal sealed class CfgEntry
	{
		public bool Enabled;

		public string White = "";

		public string Black = "";

		public bool AllowStolen = true;

		public bool StolenPriority;

		public bool? TargetRole;

		public bool? SourceRole;

		public CfgEntry Clone()
		{
			return new CfgEntry
			{
				Enabled = Enabled,
				White = White,
				Black = Black,
				AllowStolen = AllowStolen,
				StolenPriority = StolenPriority,
				TargetRole = TargetRole,
				SourceRole = SourceRole
			};
		}

		public string FlagsSegment()
		{
			return "a" + (AllowStolen ? "1" : "0") + "p" + (StolenPriority ? "1" : "0") + (TargetRole.HasValue ? ("t" + (TargetRole.Value ? "1" : "0")) : "") + (SourceRole.HasValue ? ("s" + (SourceRole.Value ? "1" : "0")) : "");
		}

		public void ParseFlags(string seg)
		{
			if (string.IsNullOrEmpty(seg) || seg.Length < 4 || seg[0] != 'a' || seg[2] != 'p')
			{
				return;
			}
			AllowStolen = seg[1] != '0';
			StolenPriority = seg[3] == '1';
			for (int i = 4; i + 1 < seg.Length; i += 2)
			{
				if (seg[i + 1] == '0' || seg[i + 1] == '1')
				{
					if (seg[i] == 't')
					{
						TargetRole = seg[i + 1] == '1';
					}
					if (seg[i] == 's')
					{
						SourceRole = seg[i + 1] == '1';
					}
				}
			}
		}
	}
	internal static class Cfg
	{
		public static bool GlobalEnabled = true;

		private static readonly Dictionary<string, CfgEntry> _mem = new Dictionary<string, CfgEntry>(StringComparer.Ordinal);

		private const string Prefix = "AutoStorage.v2.rule.";

		private const string GlobalKey = "AutoStorage.v2.global";

		private static IntPtr _store;

		private static IntPtr _data;

		private static int _slot = int.MinValue;

		private static Dictionary<string, string> ModData(bool create)
		{
			PlayerStore instance = PlayerStore.instance;
			if (instance == null)
			{
				return null;
			}
			if ((instance.modData == null) & create)
			{
				instance.modData = new Dictionary<string, string>();
			}
			return instance.modData;
		}

		internal static bool LegacyRuleKey(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return false;
			}
			int num = key.IndexOf('#');
			if (num <= 0 || key.IndexOf('#', num + 1) >= 0)
			{
				return false;
			}
			int num2 = key.IndexOf('@', num + 1);
			int result;
			return int.TryParse((num2 < 0) ? key.Substring(num + 1) : key.Substring(num + 1, num2 - num - 1), out result);
		}

		internal static CfgEntry DecodeRule(string value)
		{
			if (value == null)
			{
				return null;
			}
			string[] array = value.Split('|');
			if (array.Length < 3 || array.Length > 4 || (array[0] != "0" && array[0] != "1"))
			{
				return null;
			}
			CfgEntry cfgEntry = new CfgEntry
			{
				Enabled = (array[0] == "1"),
				White = array[1],
				Black = array[2]
			};
			if (array.Length == 4)
			{
				cfgEntry.ParseFlags(array[3]);
			}
			return cfgEntry;
		}

		public static void Load()
		{
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Expected Obj, but got Unknown
			try
			{
				PlayerStore instance = PlayerStore.instance;
				Dictionary<string, string> val = ModData(create: false);
				int num = ((instance != null) ? instance.saveSlotId : int.MinValue);
				if (U.Ptr((Il2CppObjectBase)(object)instance) == _store && U.Ptr((Il2CppObjectBase)(object)val) == _data && num == _slot)
				{
					return;
				}
				_store = U.Ptr((Il2CppObjectBase)(object)instance);
				_data = U.Ptr((Il2CppObjectBase)(object)val);
				_slot = num;
				_mem.Clear();
				GlobalEnabled = true;
				SettingsUI.ResetForSave();
				L.CloseTrackedWindows();
				World.ResetForSave();
				if (val == null)
				{
					return;
				}
				string text = default;
				if (val.TryGetValue("AutoStorage.v2.global", ref text))
				{
					GlobalEnabled = text != "0";
				}
				Il2CppStringArray val2 = new Il2CppStringArray((long)val.Count);
				val.Keys.CopyTo((Il2CppArrayBase<string>)(object)val2, 0);
				for (int i = 0; i < 2; i++)
				{
					for (int j = 0; j < ((Il2CppArrayBase<string>)(object)val2).Length; j++)
					{
						string text2 = ((Il2CppArrayBase<string>)(object)val2)[j];
						bool flag = text2.StartsWith("AutoStorage.v2.rule.", StringComparison.Ordinal);
						if (flag != (i == 0))
						{
							continue;
						}
						string text3 = (flag ? text2.Substring("AutoStorage.v2.rule.".Length) : text2);
						if (!LegacyRuleKey(text3) || _mem.ContainsKey(text3))
						{
							continue;
						}
						CfgEntry cfgEntry = DecodeRule(val[text2]);
						if (cfgEntry != null)
						{
							_mem[text3] = cfgEntry;
							if (!flag)
							{
								val["AutoStorage.v2.rule." + text3] = val[text2];
							}
						}
					}
				}
				Core.Log.Msg("[AutoStorage] Save slot " + num + ": loaded " + _mem.Count + " rules from save data.");
			}
			catch (Exception ex)
			{
				_store = IntPtr.Zero;
				_data = IntPtr.Zero;
				_slot = int.MinValue;
				_mem.Clear();
				GlobalEnabled = true;
				Core.Log.Warning("[AutoStorage] Save configuration unavailable: " + ex.Message);
			}
		}

		public static void Save()
		{
			try
			{
				Dictionary<string, string> val = ModData(create: true);
				if (val == null)
				{
					return;
				}
				val["AutoStorage.v2.global"] = (GlobalEnabled ? "1" : "0");
				foreach (KeyValuePair<string, CfgEntry> item in _mem)
				{
					WriteRule(val, item.Key, item.Value);
				}
				_data = U.Ptr((Il2CppObjectBase)(object)val);
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] Save rules: " + ex.Message);
			}
		}

		private static void WriteRule(Dictionary<string, string> data, string key, CfgEntry entry)
		{
			data["AutoStorage.v2.rule." + key] = (entry.Enabled ? "1" : "0") + "|" + entry.White + "|" + entry.Black + "|" + entry.FlagsSegment();
		}

		private static void MirrorToModData(string key)
		{
		}

		public static string Key(GameItem container)
		{
			string text = U.SafeId(container);
			if (string.IsNullOrEmpty(text))
			{
				text = "container";
			}
			return text + "#" + U.SafeUid(container);
		}

		public static string Key(GameItem container, string slot)
		{
			string text = Key(container);
			if (!string.IsNullOrEmpty(slot))
			{
				return text + "@" + slot;
			}
			return text;
		}

		public static CfgEntry Get(GameItem container)
		{
			return Get(container, null);
		}

		public static CfgEntry Get(GameItem container, string slot)
		{
			if (container == null)
			{
				return new CfgEntry();
			}
			Load();
			string key = Key(container, slot);
			if (_mem.TryGetValue(key, out var value))
			{
				return value.Clone();
			}
			if (slot == "input" && _mem.TryGetValue(Key(container, "container"), out value))
			{
				return value.Clone();
			}
			return new CfgEntry();
		}

		public static void Save(GameItem container, bool enabled, string white, string black)
		{
			Save(container, null, enabled, white, black);
		}

		public static void Save(GameItem container, string slot, bool enabled, string white, string black)
		{
			CfgEntry cfgEntry = Get(container, slot);
			Save(container, slot, enabled, white, black, cfgEntry.AllowStolen, cfgEntry.StolenPriority);
		}

		public static void Save(GameItem container, bool enabled, string white, string black, bool allowStolen, bool stolenPriority)
		{
			Save(container, null, enabled, white, black, allowStolen, stolenPriority);
		}

		public static void Save(GameItem container, string slot, bool enabled, string white, string black, bool allowStolen, bool stolenPriority)
		{
			if (container != null)
			{
				Load();
				string text = Key(container, slot);
				CfgEntry cfgEntry = Get(container, slot);
				_mem[text] = new CfgEntry
				{
					Enabled = enabled,
					White = (white ?? ""),
					Black = (black ?? ""),
					AllowStolen = allowStolen,
					StolenPriority = stolenPriority,
					TargetRole = cfgEntry.TargetRole,
					SourceRole = cfgEntry.SourceRole
				};
				MirrorToModData(text);
				Save();
				Core.Log.Msg("[AutoStorage] 保存 " + text + "  启用=" + enabled + "  白=[" + white + "]  黑=[" + black + "]  收赃物=" + allowStolen + "  赃物专用=" + stolenPriority);
			}
		}

		public static void SaveRoles(GameItem item, string slot, bool target, bool source)
		{
			CfgEntry cfgEntry = Get(item, slot);
			cfgEntry.TargetRole = target;
			cfgEntry.SourceRole = source;
			string key = Key(item, slot);
			_mem[key] = cfgEntry;
			MirrorToModData(key);
			Save();
		}

		public static List<string> SplitList(string csv)
		{
			List<string> list = new List<string>();
			if (string.IsNullOrEmpty(csv))
			{
				return list;
			}
			string[] array = csv.Split(',');
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				if (text.Length > 0)
				{
					list.Add(text);
				}
			}
			return list;
		}
	}
	internal static class Tags
	{
		public sealed class TagRow
		{
			public string ItemName;

			public string ItemId;

			public string Key;

			public string Display;

			public bool On;

			public string Note = "";

			public string Label
			{
				get
				{
					string display = Display;
					if (string.IsNullOrEmpty(display))
					{
						return Key;
					}
					if (string.Equals(display, Key, StringComparison.OrdinalIgnoreCase))
					{
						return display;
					}
					return display + "  (" + Key + ")";
				}
			}
		}

		public static int StatNoState;

		public static int StatNoDict;

		public static int StatEmptyDict;

		public static int StatOk;

		public static int StatReflFail;

		public static int StatEnabled;

		public static int StatDisabled;

		public static int StatRepaired;

		private static readonly Dictionary<string, string> _itemLabelCache = new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly Dictionary<string, string> _labelCache = new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly List<TagRow> _rows = new List<TagRow>();

		public static bool CategoryOnly = true;

		public static GameItem Scope;

		public static List<TagRow> Rows => _rows;

		public static bool Has(GameItem it, string tag)
		{
			if (it == null || string.IsNullOrEmpty(tag))
			{
				return false;
			}
			try
			{
				if (it.IsTag(tag))
				{
					return true;
				}
			}
			catch
			{
			}
			try
			{
				List<string> itemTypes = it.itemTypes;
				if (itemTypes != null)
				{
					int count = itemTypes.Count;
					if (count > 0 && count < 64)
					{
						for (int i = 0; i < count; i++)
						{
							if (string.Equals(itemTypes[i], tag, StringComparison.OrdinalIgnoreCase))
							{
								return true;
							}
						}
					}
				}
			}
			catch
			{
			}
			return false;
		}

		public static bool HasAny(GameItem it, List<string> list)
		{
			if (it == null || list == null)
			{
				return false;
			}
			for (int i = 0; i < list.Count; i++)
			{
				string text = list[i];
				if (string.IsNullOrEmpty(text))
				{
					continue;
				}
				if (text.StartsWith("id:", StringComparison.OrdinalIgnoreCase))
				{
					string text2 = text.Substring(3).Trim();
					string text3 = U.SafeId(it);
					if (text3.Length > 0 && text2.Length > 0 && text3.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				else if (Has(it, text))
				{
					return true;
				}
			}
			return false;
		}

		public static void ResetStats()
		{
			StatNoState = (StatNoDict = (StatEmptyDict = (StatOk = (StatReflFail = 0))));
			StatEnabled = (StatDisabled = (StatRepaired = 0));
		}

		public static string StatLine()
		{
			return "state==null:" + StatNoState + "  dict==null:" + StatNoDict + "  dict空:" + StatEmptyDict + "  读到tag:" + StatOk + "  反射失败:" + StatReflFail + "  启用:" + StatEnabled + "  禁用:" + StatDisabled + "  修复:" + StatRepaired;
		}

		private static bool TryTyped(GameItem it, HashSet<string> outTags)
		{
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Expected Obj, but got Unknown
			try
			{
				TagSystem state = it.state;
				if (state == null)
				{
					StatNoState++;
					return true;
				}
				Dictionary<string, TagState> dict = state.dict;
				if (dict == null)
				{
					StatNoDict++;
					return true;
				}
				KeyCollection<string, TagState> keys = dict.Keys;
				if (keys == null)
				{
					StatNoDict++;
					return true;
				}
				int count = keys.Count;
				if (count <= 0)
				{
					StatEmptyDict++;
					return true;
				}
				if (count > 4096)
				{
					StatEmptyDict++;
					return true;
				}
				Il2CppStringArray val = new Il2CppStringArray((long)count);
				keys.CopyTo((Il2CppArrayBase<string>)(object)val, 0);
				for (int i = 0; i < count; i++)
				{
					string text = ((Il2CppArrayBase<string>)(object)val)[i];
					if (!string.IsNullOrEmpty(text))
					{
						if (!IsTagEnabled(dict, text, out var enabled))
						{
							StatReflFail++;
							continue;
						}
						if (!enabled)
						{
							StatDisabled++;
							continue;
						}
						StatEnabled++;
						outTags.Add(text);
					}
				}
				StatOk++;
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static string Display(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return "";
			}
			string text = TypeLabel(key);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			text = ItemNameLabel(key);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return key;
		}

		public static string DisplayBoth(string key)
		{
			string text = Display(key);
			if (string.IsNullOrEmpty(text) || string.Equals(text, key, StringComparison.OrdinalIgnoreCase))
			{
				return key;
			}
			return text + " (" + key + ")";
		}

		public static string DisplayFor(GameItem it, string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return "";
			}
			return TypeLabel(key);
		}

		public static string TypeLabel(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return "";
			}
			if (_labelCache.TryGetValue(key, out var value))
			{
				return value;
			}
			string text = "";
			try
			{
				string text2 = "type_" + key.ToLowerInvariant();
				string localizedMechanic = LocHelper.GetLocalizedMechanic(text2, (Il2CppReferenceArray<Object>)null);
				if (!string.IsNullOrEmpty(localizedMechanic) && !string.Equals(localizedMechanic, text2, StringComparison.OrdinalIgnoreCase) && !localizedMechanic.StartsWith("Translation Error", StringComparison.OrdinalIgnoreCase))
				{
					text = localizedMechanic.Trim();
				}
			}
			catch
			{
			}
			if (text.Length > 0)
			{
				_labelCache[key] = text;
			}
			return text;
		}

		public static string ItemNameLabel(string key)
		{
			if (string.IsNullOrEmpty(key))
			{
				return "";
			}
			if (key.IndexOf(' ') >= 0)
			{
				return "";
			}
			if (_itemLabelCache.TryGetValue(key, out var value))
			{
				return value;
			}
			string text = "";
			try
			{
				string text2 = "item_" + key + "_name";
				string localizedItem = LocHelper.GetLocalizedItem(text2, (Il2CppReferenceArray<Object>)null);
				if (!string.IsNullOrEmpty(localizedItem) && !string.Equals(localizedItem, text2, StringComparison.OrdinalIgnoreCase) && !localizedItem.StartsWith("Translation Error", StringComparison.OrdinalIgnoreCase))
				{
					text = localizedItem.Trim();
				}
			}
			catch
			{
			}
			if (text.Length > 0)
			{
				_itemLabelCache[key] = text;
			}
			return text;
		}

		public static void ClearLabelCache()
		{
			_labelCache.Clear();
			_itemLabelCache.Clear();
		}

		public static string RenderTypeTooltip(GameItem it)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Expected Obj, but got Unknown
			if (it == null)
			{
				return null;
			}
			try
			{
				RichTextBuilder val = new RichTextBuilder(200);
				CommonTooltipHelper.CreateItemTypeTooltip(val, it);
				RichText val2 = val.Build(-1);
				if (val2 == null)
				{
					return null;
				}
				return val2.nakedText;
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] RenderTypeTooltip: " + ex.Message);
				return null;
			}
		}

		private static bool IsTagEnabled(Dictionary<string, TagState> d, string key, out bool enabled)
		{
			enabled = false;
			if (d == null || string.IsNullOrEmpty(key))
			{
				return true;
			}
			try
			{
				TagState val = default;
				if (!d.TryGetValue(key, ref val) || val == null)
				{
					return true;
				}
				enabled = val.valueEnabled;
				return true;
			}
			catch
			{
			}
			try
			{
				TagState val2 = d[key];
				if (val2 != null)
				{
					enabled = val2.valueEnabled;
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		internal static bool ShowCategory(string categoryLabel, bool categoryOnly)
		{
			if (categoryOnly)
			{
				return !string.IsNullOrWhiteSpace(categoryLabel);
			}
			return true;
		}

		public static string Scan(GameItem container)
		{
			return ScanItems(World.ItemsIn(container), "容器 '" + U.SafeName(container) + "'");
		}

		public static string ScanInventory(GameInventory inv, string what)
		{
			List<GameItem> list = new List<GameItem>();
			if (inv != null)
			{
				try
				{
					List<GameItem> childItems = inv.childItems;
					if (childItems != null)
					{
						for (int i = 0; i < childItems.Count; i++)
						{
							if (childItems[i] != null)
							{
								list.Add(childItems[i]);
							}
						}
					}
				}
				catch
				{
				}
			}
			return ScanItems(list, "'" + what + "'");
		}

		public static string ScanMachine(GameItem item, string onlySlot)
		{
			List<GameItem> list = new List<GameItem>();
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			foreach (MachineSlot item2 in World.AllMachineSlots(item))
			{
				if ((onlySlot != null && item2.SlotId != onlySlot) || !World.IsTarget(item, item2))
				{
					continue;
				}
				try
				{
					List<GameItem> childItems = item2.Inv.childItems;
					if (childItems == null)
					{
						continue;
					}
					for (int i = 0; i < childItems.Count; i++)
					{
						if (childItems[i] != null && hashSet.Add(U.Ptr((Il2CppObjectBase)(object)childItems[i])))
						{
							list.Add(childItems[i]);
						}
					}
				}
				catch
				{
				}
			}
			return ScanItems(list, U.SafeName(item) + " 选中的 tag 搜索/放置槽");
		}

		private static string ScanItems(List<GameItem> items, string what)
		{
			_rows.Clear();
			ResetStats();
			ClearLabelCache();
			Scope = null;
			for (int i = 0; i < items.Count; i++)
			{
				ScanItem(items[i]);
				DumpTags(items[i]);
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			StringBuilder stringBuilder = new StringBuilder();
			string b = null;
			for (int j = 0; j < _rows.Count; j++)
			{
				TagRow tagRow = _rows[j];
				if (!string.IsNullOrEmpty(tagRow.Key))
				{
					if (!dictionary.ContainsKey(tagRow.Key))
					{
						dictionary[tagRow.Key] = tagRow.Display;
					}
					if (!string.Equals(tagRow.ItemName, b, StringComparison.Ordinal))
					{
						stringBuilder.Append("\n    ").Append(tagRow.ItemName).Append(" [")
							.Append(tagRow.ItemId)
							.Append("]")
							.Append(string.IsNullOrEmpty(tagRow.Note) ? "" : ("  " + tagRow.Note));
						b = tagRow.ItemName;
					}
					stringBuilder.Append("\n        ").Append(tagRow.Label);
				}
			}
			List<string> list = new List<string>(dictionary.Keys);
			list.Sort(StringComparer.OrdinalIgnoreCase);
			StringBuilder stringBuilder2 = new StringBuilder();
			for (int k = 0; k < list.Count; k++)
			{
				string text = dictionary[list[k]];
				if (string.IsNullOrEmpty(text) || string.Equals(text, list[k], StringComparison.OrdinalIgnoreCase))
				{
					stringBuilder2.Append(list[k]);
				}
				else
				{
					stringBuilder2.Append(text).Append("|").Append(list[k]);
				}
				if (k < list.Count - 1)
				{
					stringBuilder2.Append(",");
				}
			}
			Core.Log.Msg("[AutoStorage] 扫描" + what + " 内 " + items.Count + " 个物品 → " + _rows.Count + " 条记录 / " + list.Count + " 个不同 tag   [" + StatLine() + "]");
			if (stringBuilder.Length > 0)
			{
				Core.Log.Msg("[AutoStorage] 逐物品 tag:" + stringBuilder);
			}
			return stringBuilder2.ToString();
		}

		public static string Scan()
		{
			_rows.Clear();
			ResetStats();
			Scope = null;
			List<Container> list = World.Containers(force: true);
			for (int i = 0; i < list.Count; i++)
			{
				ScanItem(list[i].Item);
			}
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					ScanList(instance.GetAllItems());
					ScanList(instance.GetAllOwnedItems(false, true));
				}
			}
			catch
			{
			}
			try
			{
				PlayerStore instance2 = PlayerStore.instance;
				if (instance2 != null)
				{
					ScanList(instance2.FindAllItem(true));
				}
			}
			catch
			{
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			StringBuilder stringBuilder = new StringBuilder();
			string b = null;
			for (int j = 0; j < _rows.Count; j++)
			{
				TagRow tagRow = _rows[j];
				if (!string.IsNullOrEmpty(tagRow.Key))
				{
					if (!dictionary.ContainsKey(tagRow.Key))
					{
						dictionary[tagRow.Key] = tagRow.Display;
					}
					if (!string.Equals(tagRow.ItemName, b, StringComparison.Ordinal))
					{
						stringBuilder.Append("\n    ").Append(tagRow.ItemName).Append(" [")
							.Append(tagRow.ItemId)
							.Append("]")
							.Append(string.IsNullOrEmpty(tagRow.Note) ? "" : ("  " + tagRow.Note));
						b = tagRow.ItemName;
					}
					stringBuilder.Append("\n        ").Append(tagRow.Label);
				}
			}
			List<string> list2 = new List<string>(dictionary.Keys);
			list2.Sort(StringComparer.OrdinalIgnoreCase);
			StringBuilder stringBuilder2 = new StringBuilder();
			for (int k = 0; k < list2.Count; k++)
			{
				string text = dictionary[list2[k]];
				if (string.IsNullOrEmpty(text) || string.Equals(text, list2[k], StringComparison.OrdinalIgnoreCase))
				{
					stringBuilder2.Append(list2[k]);
				}
				else
				{
					stringBuilder2.Append(text).Append("|").Append(list2[k]);
				}
				if (k < list2.Count - 1)
				{
					stringBuilder2.Append(",");
				}
			}
			Core.Log.Msg("[AutoStorage] 全存档扫描 → " + _rows.Count + " 条 tag 记录 / " + list2.Count + " 个不同 tag   [" + StatLine() + "]");
			if (stringBuilder.Length > 0)
			{
				Core.Log.Msg("[AutoStorage] 逐物品 tag:" + stringBuilder);
			}
			return stringBuilder2.ToString();
		}

		private static void ScanList(List<GameItem> items)
		{
			if (items == null)
			{
				return;
			}
			int num = 0;
			try
			{
				num = items.Count;
			}
			catch
			{
			}
			if (num <= 0 || num > 4096)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				GameItem val = null;
				try
				{
					val = items[i];
				}
				catch
				{
				}
				if (val != null)
				{
					ScanItem(val);
				}
			}
		}

		private static int RepairItem(GameItem it)
		{
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Expected Obj, but got Unknown
			try
			{
				TagSystem state = it.state;
				if (state == null)
				{
					return 0;
				}
				Dictionary<string, TagState> dict = state.dict;
				if (dict == null)
				{
					return 0;
				}
				KeyCollection<string, TagState> keys = dict.Keys;
				if (keys == null)
				{
					return 0;
				}
				int count = keys.Count;
				if (count <= 0 || count > 128)
				{
					return 0;
				}
				Il2CppStringArray val = new Il2CppStringArray((long)count);
				keys.CopyTo((Il2CppArrayBase<string>)(object)val, 0);
				bool flag = false;
				int num = 0;
				for (int i = 0; i < count; i++)
				{
					string text = ((Il2CppArrayBase<string>)(object)val)[i];
					if (!string.IsNullOrEmpty(text))
					{
						if (!IsTagEnabled(dict, text, out var enabled))
						{
							return 0;
						}
						if (enabled)
						{
							num++;
						}
						else if (!string.IsNullOrEmpty(TypeLabel(text)))
						{
							flag = true;
						}
					}
				}
				if (!flag || num >= count)
				{
					return 0;
				}
				int num2 = 0;
				for (int j = 0; j < count; j++)
				{
					string text2 = ((Il2CppArrayBase<string>)(object)val)[j];
					if (string.IsNullOrEmpty(text2))
					{
						continue;
					}
					TagState val2 = null;
					try
					{
						dict.TryGetValue(text2, ref val2);
					}
					catch
					{
					}
					if (val2 == null)
					{
						continue;
					}
					bool flag2 = false;
					try
					{
						flag2 = val2.valueEnabled;
					}
					catch
					{
					}
					if (!flag2)
					{
						try
						{
							val2.valueEnabled = true;
							num2++;
						}
						catch
						{
						}
					}
				}
				return num2;
			}
			catch
			{
				return 0;
			}
		}

		private static void ScanItem(GameItem it)
		{
			if (it == null)
			{
				return;
			}
			int num = RepairItem(it);
			if (num > 0)
			{
				StatRepaired += num;
				Core.Log.Warning("[AutoStorage] 修复 '" + U.SafeName(it) + "'：重新启用 " + num + " 个被旧版本扫描禁用的 tag");
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			Collect(it, hashSet);
			if (hashSet.Count == 0)
			{
				return;
			}
			string text = "";
			try
			{
				text = it.GetDisplayName(false);
			}
			catch
			{
			}
			if (string.IsNullOrEmpty(text))
			{
				try
				{
					text = U.SafeName(it);
				}
				catch
				{
				}
			}
			string itemId = U.SafeId(it);
			string note = (Stolen.Is(it) ? Stolen.Tooltip(it) : "");
			List<string> list = new List<string>(hashSet);
			list.Sort(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < list.Count; i++)
			{
				string key = list[i];
				if (ShowCategory(DisplayFor(it, key), CategoryOnly))
				{
					_rows.Add(new TagRow
					{
						ItemName = text,
						ItemId = itemId,
						Key = key,
						Display = Display(key),
						On = true,
						Note = note
					});
				}
			}
		}

		public static string ScanTags()
		{
			return Scan();
		}

		public static void DumpTags(GameItem it)
		{
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Expected Obj, but got Unknown
			if (it == null)
			{
				Core.Log.Msg("[AutoStorage] DumpTags: item == null");
				return;
			}
			string value = "";
			try
			{
				value = it.GetDisplayName(false);
			}
			catch
			{
			}
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append("\n  === ").Append(value).Append("  [")
				.Append(U.SafeId(it))
				.Append("] ===");
			try
			{
				string value2 = RenderTypeTooltip(it);
				stringBuilder.Append("\n  tooltip.nakedText = '").Append(value2).Append("'");
			}
			catch (Exception ex)
			{
				stringBuilder.Append("\n  tooltip 异常: ").Append(ex.Message);
			}
			try
			{
				TagSystem state = it.state;
				if (state == null)
				{
					Core.Log.Msg("[AutoStorage] DumpTags: state == null");
					return;
				}
				Dictionary<string, TagState> dict = state.dict;
				if (dict == null)
				{
					Core.Log.Msg("[AutoStorage] DumpTags: dict == null");
					return;
				}
				KeyCollection<string, TagState> keys = dict.Keys;
				int count = keys.Count;
				Il2CppStringArray val = new Il2CppStringArray((long)count);
				keys.CopyTo((Il2CppArrayBase<string>)(object)val, 0);
				stringBuilder.Append("\n  总共 ").Append(count).Append(" 个 key");
				int num = 0;
				for (int i = 0; i < count; i++)
				{
					string text = ((Il2CppArrayBase<string>)(object)val)[i];
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					TagState val2 = null;
					try
					{
						dict.TryGetValue(text, ref val2);
					}
					catch
					{
					}
					if (val2 != null && val2.valueEnabled)
					{
						num++;
						string value3 = "";
						string value4 = "";
						try
						{
							value3 = val2.identifierName;
						}
						catch
						{
						}
						try
						{
							_ = val2.identifier;
						}
						catch
						{
						}
						try
						{
							value4 = val2.valueString;
						}
						catch
						{
						}
						stringBuilder.Append("\n    ").Append(text).Append("   name='")
							.Append(value3)
							.Append("'")
							.Append("   label='")
							.Append(Display(text))
							.Append("'")
							.Append("   valueString='")
							.Append(value4)
							.Append("'");
					}
				}
				stringBuilder.Append("\n  启用 ").Append(num).Append(" / ")
					.Append(count);
			}
			catch (Exception ex2)
			{
				stringBuilder.Append("\n  异常: ").Append(ex2.Message);
			}
			Core.Log.Msg("[AutoStorage] DumpTags:" + stringBuilder);
		}

		private static void CollectItemTypes(GameItem it, HashSet<string> outTags)
		{
			try
			{
				List<string> itemTypes = it.itemTypes;
				if (itemTypes == null)
				{
					return;
				}
				int count = itemTypes.Count;
				if (count <= 0 || count > 64)
				{
					return;
				}
				for (int i = 0; i < count; i++)
				{
					string text = itemTypes[i];
					if (!string.IsNullOrEmpty(text))
					{
						outTags.Add(text);
					}
				}
			}
			catch
			{
			}
		}

		public static void Collect(GameItem it, HashSet<string> outTags)
		{
			if (it == null || outTags == null)
			{
				return;
			}
			CollectItemTypes(it, outTags);
			if (TryTyped(it, outTags))
			{
				return;
			}
			StatReflFail++;
			object obj = null;
			try
			{
				TagSystem state = it.state;
				if (state == null)
				{
					return;
				}
				obj = state.dict;
			}
			catch
			{
			}
			if (obj == null)
			{
				return;
			}
			try
			{
				Type type = obj.GetType();
				object obj3 = null;
				PropertyInfo property = type.GetProperty("Keys");
				if (property != null)
				{
					obj3 = property.GetValue(obj, null);
				}
				int num = 0;
				if (obj3 != null)
				{
					PropertyInfo property2 = obj3.GetType().GetProperty("Count");
					if (property2 != null)
					{
						num = (int)property2.GetValue(obj3, null);
					}
				}
				if (num <= 0 || num > 512 || obj3 == null)
				{
					return;
				}
				Type type2 = obj3.GetType();
				PropertyInfo property3 = type2.GetProperty("Item", new Type[1] { typeof(int) });
				if (property3 != null)
				{
					for (int i = 0; i < num; i++)
					{
						object obj4 = null;
						try
						{
							obj4 = property3.GetValue(obj3, new object[1] { i });
						}
						catch
						{
						}
						string text = obj4 as string;
						if (!string.IsNullOrEmpty(text) && ReflectIsEnabled(type, obj, text))
						{
							outTags.Add(text);
						}
					}
					return;
				}
				MethodInfo methodInfo = null;
				MethodInfo[] methods = type2.GetMethods();
				foreach (MethodInfo methodInfo2 in methods)
				{
					if (methodInfo2.Name == "CopyTo" && methodInfo2.GetParameters().Length == 2)
					{
						methodInfo = methodInfo2;
						break;
					}
				}
				if (!(methodInfo != null))
				{
					return;
				}
				Type parameterType = methodInfo.GetParameters()[0].ParameterType;
				object obj6 = null;
				if (parameterType == typeof(string[]))
				{
					obj6 = new string[num];
				}
				else
				{
					try
					{
						obj6 = Activator.CreateInstance(parameterType, (long)num);
					}
					catch
					{
					}
				}
				if (obj6 == null)
				{
					return;
				}
				methodInfo.Invoke(obj3, new object[2] { obj6, 0 });
				if (!(obj6 is IList list))
				{
					return;
				}
				for (int k = 0; k < num; k++)
				{
					string text2 = null;
					try
					{
						text2 = list[k] as string;
					}
					catch
					{
					}
					if (text2 != null)
					{
						if (text2.Length > 0 && ReflectIsEnabled(type, obj, text2))
						{
							outTags.Add(text2);
						}
						continue;
					}
					break;
				}
			}
			catch
			{
			}
		}

		private static bool ReflectIsEnabled(Type dictType, object dict, string key)
		{
			object obj = null;
			try
			{
				PropertyInfo property = dictType.GetProperty("Item");
				if (property != null)
				{
					obj = property.GetValue(dict, new object[1] { key });
				}
			}
			catch
			{
			}
			if (obj == null)
			{
				try
				{
					MethodInfo method = dictType.GetMethod("get_Item");
					if (method != null)
					{
						obj = method.Invoke(dict, new object[1] { key });
					}
				}
				catch
				{
				}
			}
			if (obj == null)
			{
				return false;
			}
			try
			{
				FieldInfo field = obj.GetType().GetField("valueEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (field != null)
				{
					return (bool)field.GetValue(obj);
				}
			}
			catch
			{
			}
			try
			{
				PropertyInfo property2 = obj.GetType().GetProperty("valueEnabled");
				if (property2 != null)
				{
					return (bool)property2.GetValue(obj, null);
				}
			}
			catch
			{
			}
			return false;
		}
	}
	internal static class Stolen
	{
		public static bool Is(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			try
			{
				return StolenHelper.IsStolenItem(it);
			}
			catch
			{
				return false;
			}
		}

		public static string Tooltip(GameItem it)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Expected Obj, but got Unknown
			if (it == null)
			{
				return "";
			}
			try
			{
				RichTextBuilder val = new RichTextBuilder(100);
				StolenHelper.CreateStolenItemTooltip(val, it);
				RichText val2 = val.Build(-1);
				if (val2 == null)
				{
					return "";
				}
				string nakedText = val2.nakedText;
				return string.IsNullOrEmpty(nakedText) ? "" : nakedText.Trim();
			}
			catch
			{
				return "";
			}
		}
	}
	internal static class Owned
	{
		private static bool _warned;

		public static bool Is(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			try
			{
				return GeneralHelper.IsItemEffectivelyOwned(it);
			}
			catch (Exception ex)
			{
				if (!_warned)
				{
					_warned = true;
					Core.Log.Warning("[AutoStorage] IsItemEffectivelyOwned 调用失败，跳过无法确认归属的物品: " + ex.Message);
				}
				return false;
			}
		}
	}
	internal sealed class Container
	{
		public GameItem Item;

		public GameInventory Inv;

		public string Slot;

		public string SlotLabel;

		public string Id => U.SafeId(Item);

		public string Name
		{
			get
			{
				string text = U.SafeName(Item);
				if (Slot != null)
				{
					return text + "·" + L.Slot(SlotLabel);
				}
				return text;
			}
		}

		public string Key => Cfg.Key(Item, Slot);
	}
	internal sealed class MachineSlot
	{
		public string SlotId;

		public string Label;

		public GameInventory Inv;

		public bool DefaultTarget;

		public bool DefaultSource;
	}
	internal static class World
	{
		private sealed class MachineLayout
		{
			public string[] Roles;

			public string TargetLabel;
		}

		private static readonly List<IntPtr> _protected = new List<IntPtr>();

		private static readonly string[] BlockWords = new string[20]
		{
			"display_case", "displaycase", "showcase", "counter", "sold", "trashcan", "trash", "drain", "faucet", "vending",
			"cassette", "swapbuffer", "swap_buffer", "response", "afterhour", "docinv", "bazar", "hidden", "dossier", "toolbox"
		};

		public static bool IncludeAll = false;

		private static readonly List<Container> _containers = new List<Container>();

		private static float _lastScan = -1000f;

		private static readonly Dictionary<string, MachineLayout> MachineLayouts = new Dictionary<string, MachineLayout>(StringComparer.OrdinalIgnoreCase)
		{
			{
				"furnace",
				new MachineLayout
				{
					Roles = new string[5] { "battery", "module", "input", "output", "note" },
					TargetLabel = "输入"
				}
			},
			{
				"be_incubator",
				new MachineLayout
				{
					Roles = new string[5] { "battery", "module", "input", "output", "note" },
					TargetLabel = "输入"
				}
			},
			{
				"desequencer",
				new MachineLayout
				{
					Roles = new string[5] { "battery", "module", "chip", "input", "output" },
					TargetLabel = "输入"
				}
			},
			{
				"wine_rack",
				new MachineLayout
				{
					Roles = new string[4] { "battery", "module", "container", "note" },
					TargetLabel = "容器"
				}
			},
			{
				"moisture_farm",
				new MachineLayout
				{
					Roles = new string[4] { "battery", "module", "container", "note" },
					TargetLabel = "储水容器"
				}
			},
			{
				"water_purifier",
				new MachineLayout
				{
					Roles = new string[4] { "battery", "module", "container", "note" },
					TargetLabel = "储水容器"
				}
			},
			{
				"mirage_projector",
				new MachineLayout
				{
					Roles = new string[3] { "battery", "module", "note" },
					TargetLabel = null
				}
			},
			{
				"security_alarm",
				new MachineLayout
				{
					Roles = new string[3] { "battery", "module", "note" },
					TargetLabel = null
				}
			},
			{
				"recharger",
				new MachineLayout
				{
					Roles = new string[1] { "container" },
					TargetLabel = "能量电池"
				}
			}
		};

		private static readonly Dictionary<string, string> MachineSlotLabels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			{ "input", "输入" },
			{ "output", "输出" },
			{ "container", "容器" },
			{ "chill", "冰镇区" },
			{ "freeze", "冷冻区" }
		};

		private static readonly HashSet<IntPtr> _slotDumped = new HashSet<IntPtr>();

		private static readonly Dictionary<string, string> _headerCache = new Dictionary<string, string>();

		public static void ResetForSave()
		{
			_containers.Clear();
			_protected.Clear();
			_slotDumped.Clear();
			_headerCache.Clear();
			_lastScan = -1000f;
			Tags.Rows.Clear();
			Tags.ClearLabelCache();
		}

		private static void Protect(GameInventory inv)
		{
			IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)inv);
			if (intPtr != IntPtr.Zero && !_protected.Contains(intPtr))
			{
				_protected.Add(intPtr);
			}
		}

		public static bool IsProtected(GameInventory inv)
		{
			IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)inv);
			if (intPtr != IntPtr.Zero)
			{
				return _protected.Contains(intPtr);
			}
			return false;
		}

		private static void RebuildProtected()
		{
			_protected.Clear();
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					Protect((GameInventory)(object)instance.invElement);
					Protect((GameInventory)(object)instance.frontInvinvElement);
					Protect((GameInventory)(object)instance.backInvinvElement);
					Protect((GameInventory)(object)instance.backInvinvElementCounter);
					Protect((GameInventory)(object)instance.showcaseElement);
					Protect((GameInventory)(object)instance.soldElement);
					Protect((GameInventory)(object)instance.hiddenElement);
					Protect((GameInventory)(object)instance.bazarLeftinvElement);
					Protect((GameInventory)(object)instance.trashcanInvElement);
					Protect((GameInventory)(object)instance.afterhourInventory);
					Protect((GameInventory)(object)instance.hirelingInv);
					Protect((GameInventory)(object)instance.responseInventory);
				}
			}
			catch
			{
			}
			try
			{
				PlayerStore instance2 = PlayerStore.instance;
				if (instance2 != null)
				{
					Protect((GameInventory)(object)instance2.gridInv);
				}
			}
			catch
			{
			}
		}

		public static bool BlockedId(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			string text = id.ToLowerInvariant();
			if (text == "save_bag" || text.EndsWith("_inv_save_bag", StringComparison.Ordinal))
			{
				return true;
			}
			for (int i = 0; i < BlockWords.Length; i++)
			{
				if (text.Contains(BlockWords[i]))
				{
					return true;
				}
			}
			return false;
		}

		public static bool IsInventoryItem(GameItem it, out GameInventory inv)
		{
			inv = null;
			if (it == null)
			{
				return false;
			}
			if (BlockedId(U.SafeId(it)))
			{
				return false;
			}
			inv = ResolveInventory(it);
			if (inv == null)
			{
				return false;
			}
			if (IsProtected(inv))
			{
				return false;
			}
			GameItem val = null;
			try
			{
				val = inv.GetParentItem();
			}
			catch
			{
			}
			if (val != null && !U.Same((Il2CppObjectBase)(object)val, (Il2CppObjectBase)(object)it))
			{
				return false;
			}
			if (val == null && !OwnsInventory(it, inv))
			{
				return false;
			}
			try
			{
				if (inv.childItems == null)
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
			return true;
		}

		private static bool OwnsInventory(GameItem it, GameInventory inv)
		{
			try
			{
				List<GraphNodeStorage> children = it.children;
				if (children == null)
				{
					return false;
				}
				int num = 0;
				try
				{
					num = children.Count;
				}
				catch
				{
				}
				if (num <= 0 || num > 256)
				{
					return false;
				}
				IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)inv);
				for (int i = 0; i < num; i++)
				{
					GraphNodeStorage val = null;
					try
					{
						val = children[i];
					}
					catch
					{
					}
					if (val != null)
					{
						GameInventory val2 = ((Il2CppObjectBase)val).TryCast<GameInventory>();
						if (val2 != null && U.Ptr((Il2CppObjectBase)(object)val2) == intPtr)
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

		public static bool IdHasStorage(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			return U.SafeId(it).IndexOf("storage", StringComparison.OrdinalIgnoreCase) >= 0;
		}

		public static bool IsCage(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			if (string.Equals(U.SafeId(it), "animal_cage", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			try
			{
				if (Tags.Has(it, "CAGE_TAG"))
				{
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		public static bool IsContainerItem(GameItem it, out GameInventory inv)
		{
			inv = null;
			if (!Owned.Is(it))
			{
				return false;
			}
			if (!IncludeAll && !Tags.Has(it, "STORAGE") && !IdHasStorage(it) && !IsCage(it))
			{
				return false;
			}
			return IsInventoryItem(it, out inv);
		}

		public static GameInventory ResolveInventory(GameItem it)
		{
			if (it == null)
			{
				return null;
			}
			try
			{
				PixelWindow contentWindow = it.contentWindow;
				if (contentWindow != null)
				{
					GameInventory val = FromWindow(contentWindow);
					if (val != null)
					{
						return val;
					}
				}
			}
			catch
			{
			}
			try
			{
				List<GraphNodeStorage> children = it.children;
				if (children != null)
				{
					int num = 0;
					try
					{
						num = children.Count;
					}
					catch
					{
					}
					if (num > 0 && num <= 256)
					{
						for (int i = 0; i < num; i++)
						{
							GraphNodeStorage val2 = null;
							try
							{
								val2 = children[i];
							}
							catch
							{
							}
							if (val2 != null)
							{
								GameInventory val3 = ((Il2CppObjectBase)val2).TryCast<GameInventory>();
								if (val3 != null && U.Ptr((Il2CppObjectBase)(object)val3) != IntPtr.Zero)
								{
									return val3;
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				GraphNodeWindow child = it.child;
				if (child != null)
				{
					GraphNodeStorage node = null;
					try
					{
						node = child.child;
					}
					catch
					{
					}
					GameInventory val4 = DigInventory(node, 0);
					if (val4 != null)
					{
						return val4;
					}
				}
			}
			catch
			{
			}
			GameInventory val5 = ResolveInventoryByReflection(it);
			if (val5 != null)
			{
				return val5;
			}
			return null;
		}

		private static GameInventory DigInventory(GraphNodeStorage node, int depth)
		{
			if (node == null || depth > 4)
			{
				return null;
			}
			try
			{
				GameInventory val = ((Il2CppObjectBase)node).TryCast<GameInventory>();
				if (val != null && U.Ptr((Il2CppObjectBase)(object)val) != IntPtr.Zero)
				{
					return val;
				}
			}
			catch
			{
			}
			try
			{
				List<GraphNodeStorage> children = node.children;
				if (children != null)
				{
					int num = 0;
					try
					{
						num = children.Count;
					}
					catch
					{
					}
					if (num > 0 && num <= 256)
					{
						for (int i = 0; i < num; i++)
						{
							GraphNodeStorage node2 = null;
							try
							{
								node2 = children[i];
							}
							catch
							{
							}
							GameInventory val2 = DigInventory(node2, depth + 1);
							if (val2 != null)
							{
								return val2;
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

		private static GameInventory ResolveInventoryByReflection(GameItem it)
		{
			try
			{
				Type type = ((object)it).GetType();
				PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (PropertyInfo propertyInfo in properties)
				{
					Type propertyType;
					try
					{
						propertyType = propertyInfo.PropertyType;
					}
					catch
					{
						continue;
					}
					if (propertyType != typeof(GameInventory) && !propertyType.IsSubclassOf(typeof(GameInventory)))
					{
						continue;
					}
					try
					{
						object? value = propertyInfo.GetValue(it, null);
						GameInventory val = (GameInventory)((value is GameInventory) ? value : null);
						if (val != null && U.Ptr((Il2CppObjectBase)(object)val) != IntPtr.Zero)
						{
							return val;
						}
					}
					catch
					{
					}
				}
				FieldInfo[] fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (FieldInfo fieldInfo in fields)
				{
					Type fieldType;
					try
					{
						fieldType = fieldInfo.FieldType;
					}
					catch
					{
						continue;
					}
					if (fieldType != typeof(GameInventory) && !fieldType.IsSubclassOf(typeof(GameInventory)))
					{
						continue;
					}
					try
					{
						object? value2 = fieldInfo.GetValue(it);
						GameInventory val2 = (GameInventory)((value2 is GameInventory) ? value2 : null);
						if (val2 != null && U.Ptr((Il2CppObjectBase)(object)val2) != IntPtr.Zero)
						{
							return val2;
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
			return null;
		}

		private static GameInventory FromWindow(PixelWindow w)
		{
			if (w == null)
			{
				return null;
			}
			try
			{
				PixelElement childElement = w.childElement;
				if (childElement != null)
				{
					GameInventory val = ((Il2CppObjectBase)childElement).TryCast<GameInventory>();
					if (val != null)
					{
						return val;
					}
				}
			}
			catch
			{
			}
			try
			{
				GraphNodeStorage child = w.child;
				if (child != null)
				{
					GameInventory val2 = ((Il2CppObjectBase)child).TryCast<GameInventory>();
					if (val2 != null)
					{
						return val2;
					}
				}
			}
			catch
			{
			}
			try
			{
				PixelElement childElement2 = w.childElement;
				GridPixelElement val3 = ((childElement2 != null) ? ((Il2CppObjectBase)childElement2).TryCast<GridPixelElement>() : null);
				if (val3 != null)
				{
					int num = 0;
					int num2 = 0;
					try
					{
						num = val3.gridWidth;
					}
					catch
					{
					}
					try
					{
						num2 = val3.gridHeight;
					}
					catch
					{
					}
					if (num > 0 && num2 > 0 && num <= 64 && num2 <= 64)
					{
						for (int i = 0; i < num2; i++)
						{
							for (int j = 0; j < num; j++)
							{
								PixelElement element = val3.GetElement(j, i);
								if (element != null)
								{
									GameInventory val4 = ((Il2CppObjectBase)element).TryCast<GameInventory>();
									if (val4 != null)
									{
										return val4;
									}
								}
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

		public static List<GameItem> ItemsIn(GameItem container)
		{
			List<GameItem> list = new List<GameItem>();
			if (container == null)
			{
				return list;
			}
			HashSet<IntPtr> seenInv = new HashSet<IntPtr>();
			HashSet<IntPtr> seenItem = new HashSet<IntPtr>();
			try
			{
				PixelWindow contentWindow = container.contentWindow;
				if (contentWindow != null)
				{
					GameInventory val = FromWindow(contentWindow);
					if (val != null)
					{
						PullInv(val, list, seenInv, seenItem, 0);
					}
				}
			}
			catch
			{
			}
			try
			{
				GraphNodeWindow child = container.child;
				if (child != null)
				{
					GraphNodeStorage child2 = child.child;
					if (child2 != null)
					{
						PullNode(child2, list, seenInv, seenItem, 0);
					}
				}
			}
			catch
			{
			}
			try
			{
				GameInventory val2 = ResolveInventory(container);
				if (val2 != null)
				{
					PullInv(val2, list, seenInv, seenItem, 0);
				}
			}
			catch
			{
			}
			return list;
		}

		private static void PullInv(GameInventory inv, List<GameItem> res, HashSet<IntPtr> seenInv, HashSet<IntPtr> seenItem, int depth)
		{
			if (inv == null || depth > 3)
			{
				return;
			}
			IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)inv);
			if (intPtr == IntPtr.Zero || !seenInv.Add(intPtr))
			{
				return;
			}
			List<GameItem> val = null;
			try
			{
				val = inv.childItems;
			}
			catch
			{
			}
			if (val == null)
			{
				return;
			}
			int num = 0;
			try
			{
				num = val.Count;
			}
			catch
			{
			}
			if (num <= 0 || num > 4000)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				GameItem val2 = null;
				try
				{
					val2 = val[i];
				}
				catch
				{
				}
				if (val2 == null)
				{
					continue;
				}
				IntPtr intPtr2 = U.Ptr((Il2CppObjectBase)(object)val2);
				if (intPtr2 != IntPtr.Zero && seenItem.Add(intPtr2))
				{
					res.Add(val2);
				}
				try
				{
					GameInventory val3 = ResolveInventory(val2);
					if (val3 != null)
					{
						PullInv(val3, res, seenInv, seenItem, depth + 1);
					}
				}
				catch
				{
				}
			}
		}

		private static void PullNode(GraphNodeStorage node, List<GameItem> res, HashSet<IntPtr> seenInv, HashSet<IntPtr> seenItem, int depth)
		{
			if (node == null || depth > 3)
			{
				return;
			}
			try
			{
				List<GraphNodeStorage> children = node.children;
				if (children == null)
				{
					return;
				}
				int count = children.Count;
				if (count > 0 && count <= 400)
				{
					for (int i = 0; i < count; i++)
					{
						PullNode(children[i], res, seenInv, seenItem, depth + 1);
					}
				}
			}
			catch
			{
			}
		}

		public static List<Container> Containers(bool force)
		{
			if (!force && Time.realtimeSinceStartup - _lastScan < 3f)
			{
				return _containers;
			}
			_lastScan = Time.realtimeSinceStartup;
			_containers.Clear();
			RebuildProtected();
			HashSet<IntPtr> seenItems = new HashSet<IntPtr>();
			HashSet<IntPtr> seenInvs = new HashSet<IntPtr>();
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					AddInventory((GameInventory)(object)instance.invElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.frontInvinvElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.backInvinvElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.backInvinvElementCounter, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.showcaseElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.soldElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.hiddenElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.bazarLeftinvElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.trashcanInvElement, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.afterhourInventory, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.hirelingInv, seenInvs, seenItems);
					AddInventory((GameInventory)(object)instance.responseInventory, seenInvs, seenItems);
					AddItem(instance.selectedBackpack, seenItems);
				}
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] EmporiumEntry 扫描: " + ex.Message);
			}
			try
			{
				EmporiumEntry instance2 = EmporiumEntry.Instance;
				if ((Object)(object)instance2 != (Object)null)
				{
					List<GameItem> allItems = instance2.GetAllItems();
					if (allItems != null)
					{
						for (int i = 0; i < allItems.Count; i++)
						{
							AddItem(allItems[i], seenItems);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				Core.Log.Warning("[AutoStorage] GetAllItems: " + ex2.Message);
			}
			try
			{
				PlayerStore instance3 = PlayerStore.instance;
				if (instance3 != null)
				{
					List<GameItem> val = instance3.FindAllItem(true);
					if (val != null)
					{
						for (int j = 0; j < val.Count; j++)
						{
							AddItem(val[j], seenItems);
						}
					}
				}
			}
			catch (Exception ex3)
			{
				Core.Log.Warning("[AutoStorage] FindAllItem: " + ex3.Message);
			}
			try
			{
				EmporiumEntry instance4 = EmporiumEntry.Instance;
				if ((Object)(object)instance4 != (Object)null)
				{
					List<GameItem> allOwnedItems = instance4.GetAllOwnedItems(false, true);
					if (allOwnedItems != null)
					{
						for (int k = 0; k < allOwnedItems.Count; k++)
						{
							AddItem(allOwnedItems[k], seenItems);
						}
					}
				}
			}
			catch (Exception ex4)
			{
				Core.Log.Warning("[AutoStorage] GetAllOwnedItems: " + ex4.Message);
			}
			try
			{
				PlayerStore instance5 = PlayerStore.instance;
				if (instance5 != null)
				{
					AddInventory((GameInventory)(object)instance5.gridInv, seenInvs, seenItems);
				}
			}
			catch (Exception ex5)
			{
				Core.Log.Warning("[AutoStorage] gridInv: " + ex5.Message);
			}
			try
			{
				Il2CppReferenceArray<Object> val2 = Resources.FindObjectsOfTypeAll(Il2CppType.Of<TreeNodeRender>());
				if (val2 != null)
				{
					for (int l = 0; l < ((Il2CppArrayBase<Object>)(object)val2).Length; l++)
					{
						Object val3 = ((Il2CppArrayBase<Object>)(object)val2)[l];
						if (val3 == (Object)null)
						{
							continue;
						}
						TreeNodeRender val4 = ((Il2CppObjectBase)val3).TryCast<TreeNodeRender>();
						if ((Object)(object)val4 == (Object)null)
						{
							continue;
						}
						try
						{
							PixelElement element = val4.element;
							if (element != null)
							{
								GameInventory val5 = ((Il2CppObjectBase)element).TryCast<GameInventory>();
								if (val5 != null)
								{
									AddInventory(val5, seenInvs, seenItems);
								}
								else
								{
									GameItem val6 = ((Il2CppObjectBase)element).TryCast<GameItem>();
									if (val6 != null)
									{
										AddItem(val6, seenItems);
									}
								}
							}
						}
						catch
						{
						}
						try
						{
							PixelWindow window = val4.window;
							if (window != null)
							{
								AddInventory(FromWindow(window), seenInvs, seenItems);
							}
						}
						catch
						{
						}
					}
				}
			}
			catch (Exception ex6)
			{
				Core.Log.Warning("[AutoStorage] 场景扫描: " + ex6.Message);
			}
			return _containers;
		}

		private static void AddInventory(GameInventory inv, HashSet<IntPtr> seenInvs, HashSet<IntPtr> seenItems)
		{
			IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)inv);
			if (intPtr == IntPtr.Zero || !seenInvs.Add(intPtr))
			{
				return;
			}
			Register(inv);
			try
			{
				List<GameItem> childItems = inv.childItems;
				if (childItems == null)
				{
					return;
				}
				int count = childItems.Count;
				if (count <= 4000)
				{
					for (int i = 0; i < count; i++)
					{
						AddItem(childItems[i], seenItems);
					}
				}
			}
			catch
			{
			}
		}

		private static void AddItem(GameItem it, HashSet<IntPtr> seenItems)
		{
			IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)it);
			if (intPtr == IntPtr.Zero || !seenItems.Add(intPtr) || BlockedId(U.SafeId(it)))
			{
				return;
			}
			TryRegister(it);
			try
			{
				PixelWindow contentWindow = it.contentWindow;
				if (contentWindow != null)
				{
					GameInventory val = FromWindow(contentWindow);
					if (val != null)
					{
						Register(val);
					}
				}
			}
			catch
			{
			}
		}

		private static void TryRegister(GameItem it)
		{
			if (it != null && !BlockedId(U.SafeId(it)) && Owned.Is(it))
			{
				GameInventory inv;
				if (IsMachineItem(it))
				{
					List<MachineSlot> slots = MachineSlotsOf(it, includeOutput: false);
					PairSlots(it, slots);
				}
				else if (IsContainerItem(it, out inv))
				{
					Pair(it, inv);
				}
			}
		}

		private static void Register(GameInventory inv)
		{
			if (inv != null)
			{
				GameItem val = null;
				try
				{
					val = inv.GetParentItem();
				}
				catch
				{
				}
				if (val != null)
				{
					TryRegister(val);
				}
			}
		}

		private static void Pair(GameItem it, GameInventory inv)
		{
			for (int i = 0; i < _containers.Count; i++)
			{
				if (U.Ptr((Il2CppObjectBase)(object)_containers[i].Item) == U.Ptr((Il2CppObjectBase)(object)it))
				{
					if (U.Ptr((Il2CppObjectBase)(object)_containers[i].Inv) == IntPtr.Zero)
					{
						_containers[i].Inv = inv;
					}
					return;
				}
			}
			_containers.Add(new Container
			{
				Item = it,
				Inv = inv
			});
		}

		private static void PairSlots(GameItem it, List<MachineSlot> slots)
		{
			for (int i = 0; i < slots.Count; i++)
			{
				MachineSlot machineSlot = slots[i];
				bool flag = false;
				for (int j = 0; j < _containers.Count; j++)
				{
					if (U.Ptr((Il2CppObjectBase)(object)_containers[j].Item) == U.Ptr((Il2CppObjectBase)(object)it) && string.Equals(_containers[j].Slot, machineSlot.SlotId, StringComparison.OrdinalIgnoreCase))
					{
						if (U.Ptr((Il2CppObjectBase)(object)_containers[j].Inv) == IntPtr.Zero)
						{
							_containers[j].Inv = machineSlot.Inv;
						}
						flag = true;
						break;
					}
				}
				if (!flag)
				{
					_containers.Add(new Container
					{
						Item = it,
						Inv = machineSlot.Inv,
						Slot = machineSlot.SlotId,
						SlotLabel = machineSlot.Label
					});
				}
			}
		}

		public static bool IsMachineItem(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			try
			{
				if (Tags.Has(it, "MACHINE"))
				{
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private static List<GameInventory> CollectMachineInventories(GameItem item)
		{
			List<GameInventory> list = new List<GameInventory>();
			HashSet<IntPtr> seen = new HashSet<IntPtr>();
			try
			{
				PixelWindow contentWindow = item.contentWindow;
				if (contentWindow != null)
				{
					CollectFromWindow(contentWindow, list, seen);
				}
			}
			catch
			{
			}
			try
			{
				List<GraphNodeStorage> children = item.children;
				if (children != null)
				{
					int num = 0;
					try
					{
						num = children.Count;
					}
					catch
					{
					}
					if (num > 0 && num <= 64)
					{
						for (int i = 0; i < num; i++)
						{
							GraphNodeStorage node = null;
							try
							{
								node = children[i];
							}
							catch
							{
							}
							CollectFromNode(node, list, seen, 0);
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				GraphNodeWindow child = item.child;
				if (child != null)
				{
					GraphNodeStorage node2 = null;
					try
					{
						node2 = child.child;
					}
					catch
					{
					}
					CollectFromNode(node2, list, seen, 0);
				}
			}
			catch
			{
			}
			return list;
		}

		private static void CollectFromNode(GraphNodeStorage node, List<GameInventory> res, HashSet<IntPtr> seen, int depth)
		{
			if (node == null || depth > 6)
			{
				return;
			}
			try
			{
				GameInventory val = ((Il2CppObjectBase)node).TryCast<GameInventory>();
				if (val != null)
				{
					IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)val);
					if (intPtr != IntPtr.Zero && seen.Add(intPtr))
					{
						res.Add(val);
					}
					return;
				}
			}
			catch
			{
			}
			try
			{
				List<GraphNodeStorage> children = node.children;
				if (children == null)
				{
					return;
				}
				int num = 0;
				try
				{
					num = children.Count;
				}
				catch
				{
				}
				if (num <= 0 || num > 64)
				{
					return;
				}
				for (int i = 0; i < num; i++)
				{
					GraphNodeStorage node2 = null;
					try
					{
						node2 = children[i];
					}
					catch
					{
					}
					CollectFromNode(node2, res, seen, depth + 1);
				}
			}
			catch
			{
			}
		}

		private static void CollectFromWindow(PixelWindow w, List<GameInventory> res, HashSet<IntPtr> seen)
		{
			if (w == null)
			{
				return;
			}
			PixelElement val = null;
			try
			{
				val = w.childElement;
			}
			catch
			{
			}
			if (val != null)
			{
				try
				{
					GameInventory val2 = ((Il2CppObjectBase)val).TryCast<GameInventory>();
					if (val2 != null)
					{
						IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)val2);
						if (intPtr != IntPtr.Zero && seen.Add(intPtr))
						{
							res.Add(val2);
						}
					}
				}
				catch
				{
				}
				try
				{
					GridPixelElement val3 = ((Il2CppObjectBase)val).TryCast<GridPixelElement>();
					if (val3 != null)
					{
						int num = 0;
						int num2 = 0;
						try
						{
							num = val3.gridWidth;
						}
						catch
						{
						}
						try
						{
							num2 = val3.gridHeight;
						}
						catch
						{
						}
						if (num > 0 && num2 > 0 && num <= 64 && num2 <= 64)
						{
							for (int i = 0; i < num2; i++)
							{
								for (int j = 0; j < num; j++)
								{
									PixelElement val4 = null;
									try
									{
										val4 = val3.GetElement(j, i);
									}
									catch
									{
									}
									if (val4 == null)
									{
										continue;
									}
									GameInventory val5 = ((Il2CppObjectBase)val4).TryCast<GameInventory>();
									if (val5 != null)
									{
										IntPtr intPtr2 = U.Ptr((Il2CppObjectBase)(object)val5);
										if (intPtr2 != IntPtr.Zero && seen.Add(intPtr2))
										{
											res.Add(val5);
										}
									}
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			try
			{
				GraphNodeStorage child = w.child;
				if (child != null)
				{
					CollectFromNode(child, res, seen, 0);
				}
			}
			catch
			{
			}
		}

		public static bool IsFridge(GameItem item)
		{
			return string.Equals(U.SafeId(item), "fridge", StringComparison.OrdinalIgnoreCase);
		}

		public static string RuleSlot(GameItem item, string slot)
		{
			return slot;
		}

		private static string SlotRole(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return null;
			}
			text = Regex.Replace(text, "<[^>]*>", "").Trim().ToLowerInvariant();
			switch (text)
			{
			case "input":
			case "输入":
			case "輸入":
			case "be_slot_input":
				return "input";
			case "output":
			case "输出":
			case "輸出":
			case "be_slot_output":
				return "output";
			case "battery":
			case "power":
			case "电池":
			case "电源":
			case "電源":
			case "be_slot_battery":
				return "battery";
			case "module":
			case "modules":
			case "模组":
			case "模組":
			case "be_slot_module":
			case "be_slot_incubator_module":
				return "module";
			case "note":
			case "notes":
			case "备注":
			case "備註":
			case "be_slot_note":
				return "note";
			case "container":
			case "容器":
				return "container";
			case "chip":
			case "芯片":
				return "chip";
			default:
				return null;
			}
		}

		private static string InventoryRole(GameInventory inv)
		{
			return null;
		}

		private static string ReadHeaderText(PixelElement element)
		{
			if (element == null)
			{
				return null;
			}
			try
			{
				TagElement val = ((Il2CppObjectBase)element).TryCast<TagElement>();
				if (val != null)
				{
					string text = val.GetText();
					if (!string.IsNullOrWhiteSpace(text))
					{
						return text;
					}
				}
			}
			catch
			{
			}
			try
			{
				RichTextElement val2 = ((Il2CppObjectBase)element).TryCast<RichTextElement>();
				if (val2 != null && (Object)(object)val2.textNode != (Object)null && (Object)(object)val2.textNode.text != (Object)null)
				{
					string text2 = ((TMP_Text)val2.textNode.text).text;
					if (!string.IsNullOrWhiteSpace(text2))
					{
						return text2;
					}
				}
			}
			catch
			{
			}
			return null;
		}

		private static void ReadSlotHeaders(PixelElement element, Dictionary<IntPtr, string> headers, int depth)
		{
			if (element == null || depth > 6)
			{
				return;
			}
			GridPixelElement val = ((Il2CppObjectBase)element).TryCast<GridPixelElement>();
			if (val == null)
			{
				return;
			}
			int gridWidth = val.gridWidth;
			int gridHeight = val.gridHeight;
			if (gridWidth <= 0 || gridHeight <= 0 || gridWidth > 64 || gridHeight > 64)
			{
				return;
			}
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			for (int i = 0; i < gridHeight; i++)
			{
				for (int j = 0; j < gridWidth; j++)
				{
					PixelElement element2 = val.GetElement(j, i);
					if (element2 == null || !hashSet.Add(U.Ptr((Il2CppObjectBase)(object)element2)))
					{
						continue;
					}
					GameInventory val2 = ((Il2CppObjectBase)element2).TryCast<GameInventory>();
					if (val2 == null)
					{
						ReadSlotHeaders(element2, headers, depth + 1);
						continue;
					}
					for (int num = i - 1; num >= 0; num--)
					{
						PixelElement element3 = val.GetElement(j, num);
						if (element3 != null && !U.Same((Il2CppObjectBase)(object)element3, (Il2CppObjectBase)(object)element2))
						{
							string value = ReadHeaderText(element3);
							if (!string.IsNullOrWhiteSpace(value))
							{
								headers[U.Ptr((Il2CppObjectBase)(object)val2)] = value;
							}
							break;
						}
					}
				}
			}
		}

		public static List<MachineSlot> AllMachineSlots(GameItem item)
		{
			List<MachineSlot> list = new List<MachineSlot>();
			if (item == null || BlockedId(U.SafeId(item)))
			{
				return list;
			}
			if (IsFridge(item))
			{
				try
				{
					AddMachineSlot(list, "chill", "冰镇区", (GameInventory)(object)MachineFridge.GetChillZone(item), target: true, source: false);
				}
				catch
				{
				}
				try
				{
					AddMachineSlot(list, "freeze", "冷冻区", (GameInventory)(object)MachineFridge.GetFreezeZone(item), target: true, source: false);
				}
				catch
				{
				}
				return list;
			}
			List<GameInventory> list2 = CollectMachineInventories(item);
			Dictionary<IntPtr, string> dictionary = new Dictionary<IntPtr, string>();
			try
			{
				if (item.contentWindow != null)
				{
					ReadSlotHeaders(item.contentWindow.childElement, dictionary, 0);
				}
			}
			catch
			{
			}
			bool flag = MachineLayouts.TryGetValue(U.SafeId(item), out var value) && value.Roles.Length == list2.Count;
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list2.Count; i++)
			{
				GameInventory val = list2[i];
				if (val == null || IsProtected(val))
				{
					continue;
				}
				dictionary.TryGetValue(U.Ptr((Il2CppObjectBase)(object)val), out var value2);
				string key = Cfg.Key(item) + ":" + U.Ptr((Il2CppObjectBase)(object)val);
				if (!string.IsNullOrWhiteSpace(value2))
				{
					_headerCache[key] = value2;
				}
				else
				{
					_headerCache.TryGetValue(key, out value2);
				}
				string text = InventoryRole(val) ?? SlotRole(value2);
				switch (text)
				{
				case "battery":
				case "module":
				case "note":
					continue;
				}
				if ((text == null) & flag)
				{
					switch (value.Roles[i])
					{
					case "battery":
					case "module":
					case "note":
						continue;
					}
				}
				string text2 = text ?? ("slot" + i);
				if (HasSlot(list, text2))
				{
					text2 = text2 + "_" + i;
				}
				string value3 = value2;
				if (string.IsNullOrWhiteSpace(value3) && text != null)
				{
					MachineSlotLabels.TryGetValue(text, out value3);
				}
				if (string.IsNullOrWhiteSpace(value3))
				{
					value3 = ((text == "chip") ? "芯片" : ("未识别槽位 " + (i + 1)));
				}
				bool target = text == "input" || text == "container";
				AddMachineSlot(list, text2, value3, val, target, text == "output");
				stringBuilder.Append("\n    ").Append(text2).Append(" = ")
					.Append(value3);
			}
			list.Sort((MachineSlot a, MachineSlot b) => SlotRank(a.SlotId).CompareTo(SlotRank(b.SlotId)));
			if (_slotDumped.Add(U.Ptr((Il2CppObjectBase)(object)item)))
			{
				Core.Log.Msg("[AutoStorage] 机器槽位 '" + U.SafeId(item) + "':" + stringBuilder);
			}
			return list;
		}

		private static void AddMachineSlot(List<MachineSlot> slots, string id, string label, GameInventory inv, bool target, bool source)
		{
			if (inv != null && !(U.Ptr((Il2CppObjectBase)(object)inv) == IntPtr.Zero) && !IsProtected(inv))
			{
				slots.Add(new MachineSlot
				{
					SlotId = id,
					Label = label,
					Inv = inv,
					DefaultTarget = target,
					DefaultSource = source
				});
			}
		}

		public static bool IsTarget(GameItem item, MachineSlot slot)
		{
			if (!slot.DefaultSource)
			{
				return Cfg.Get(item, slot.SlotId).TargetRole ?? slot.DefaultTarget;
			}
			return false;
		}

		public static List<MachineSlot> TargetPages(GameItem item)
		{
			List<MachineSlot> list = AllMachineSlots(item);
			list.RemoveAll((MachineSlot slot) => !IsTarget(item, slot));
			return list;
		}

		public static MachineSlot SelectPage(GameItem item, MachineSlot requested)
		{
			List<MachineSlot> list = TargetPages(item);
			foreach (MachineSlot item2 in list)
			{
				if (requested != null && item2.SlotId == requested.SlotId)
				{
					return item2;
				}
			}
			if (list.Count <= 0)
			{
				return null;
			}
			return list[0];
		}

		public static bool IsSource(GameItem item, MachineSlot slot)
		{
			return Cfg.Get(item, slot.SlotId).SourceRole ?? slot.DefaultSource;
		}

		public static List<MachineSlot> MachineSlotsOf(GameItem item, bool includeOutput)
		{
			List<MachineSlot> list = AllMachineSlots(item);
			list.RemoveAll((MachineSlot slot) =>
			{
				try
				{
					return (!includeOutput) ? (!IsTarget(item, slot) || slot.Inv.overrideLockInsert) : (!IsSource(item, slot) || slot.Inv.overrideLockRemove);
				}
				catch
				{
					return true;
				}
			});
			return list;
		}

		private static bool HasSlot(List<MachineSlot> list, string slotId)
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].SlotId == slotId)
				{
					return true;
				}
			}
			return false;
		}

		private static int SlotRank(string slotId)
		{
			return slotId switch
			{
				"input" => 0, 
				"container" => 1, 
				"chill" => 2, 
				"freeze" => 3, 
				_ => 4, 
			};
		}

		private static string SlotNames(List<MachineSlot> list)
		{
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < list.Count; i++)
			{
				if (i > 0)
				{
					stringBuilder.Append(", ");
				}
				stringBuilder.Append(list[i].SlotId);
			}
			return stringBuilder.ToString();
		}

		public static bool IsBag(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			string text = (U.SafeId(it) ?? "").ToLowerInvariant();
			if (text.Length == 0 || BlockedId(text))
			{
				return false;
			}
			if (text.Contains("backpack") || text.Contains("satchel") || text.Contains("fanny_pack") || text.Contains("fannypack"))
			{
				return true;
			}
			try
			{
				if (Tags.Has(it, "backpack"))
				{
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		private static void MineBag(GameItem bag, List<GameItem> res, HashSet<IntPtr> seenItems, HashSet<IntPtr> seenBags, int depth)
		{
			if (bag == null || depth > 2)
			{
				return;
			}
			IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)bag);
			if (intPtr == IntPtr.Zero || !seenBags.Add(intPtr) || !IsBag(bag))
			{
				return;
			}
			GameInventory val = ResolveInventory(bag);
			if (val == null || IsProtected(val))
			{
				return;
			}
			List<GameItem> val2 = Child(val);
			if (val2 == null)
			{
				return;
			}
			int num = 0;
			try
			{
				num = val2.Count;
			}
			catch
			{
			}
			if (num <= 0 || num > 2000)
			{
				return;
			}
			for (int i = 0; i < num; i++)
			{
				GameItem val3 = null;
				try
				{
					val3 = val2[i];
				}
				catch
				{
				}
				if (val3 == null)
				{
					continue;
				}
				IntPtr intPtr2 = U.Ptr((Il2CppObjectBase)(object)val3);
				if (!(intPtr2 == IntPtr.Zero) && seenItems.Add(intPtr2))
				{
					res.Add(val3);
					if (IsBag(val3))
					{
						MineBag(val3, res, seenItems, seenBags, depth + 1);
					}
				}
			}
		}

		public static List<GameItem> SourceItems(out string report)
		{
			List<GameItem> res = new List<GameItem>();
			HashSet<IntPtr> seen = new HashSet<IntPtr>();
			StringBuilder sb = new StringBuilder();
			Action<string, List<GameItem>> action = (string label, List<GameItem> val9) =>
			{
				int num12 = 0;
				if (val9 != null)
				{
					for (int i = 0; i < val9.Count; i++)
					{
						GameItem val10 = val9[i];
						IntPtr intPtr4 = U.Ptr((Il2CppObjectBase)(object)val10);
						if (!(intPtr4 == IntPtr.Zero) && seen.Add(intPtr4))
						{
							res.Add(val10);
							num12++;
						}
					}
				}
				if (num12 > 0)
				{
					sb.Append("\n    物品栏/柜台[").Append(label).Append("] = ")
						.Append(num12);
				}
			};
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					action("invElement", Child((GameInventory)(object)instance.invElement));
					action("frontInv", Child((GameInventory)(object)instance.frontInvinvElement));
					action("backInv", Child((GameInventory)(object)instance.backInvinvElement));
					action("hidden", Child((GameInventory)(object)instance.hiddenElement));
				}
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] 物品栏: " + ex.Message);
			}
			try
			{
				EmporiumEntry instance2 = EmporiumEntry.Instance;
				if ((Object)(object)instance2 != (Object)null)
				{
					action("backInvCounter", Child((GameInventory)(object)instance2.backInvinvElementCounter));
					List<GameItem> countersItems = instance2.GetCountersItems();
					if (countersItems != null)
					{
						int num = 0;
						for (int num2 = 0; num2 < countersItems.Count; num2++)
						{
							GameItem val = countersItems[num2];
							IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)val);
							if (!(intPtr == IntPtr.Zero) && seen.Add(intPtr))
							{
								res.Add(val);
								num++;
							}
						}
						if (num > 0)
						{
							sb.Append("\n    谈判柜台[GetCountersItems] = ").Append(num);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				Core.Log.Warning("[AutoStorage] 谈判柜台: " + ex2.Message);
			}
			try
			{
				EmporiumEntry instance3 = EmporiumEntry.Instance;
				if ((Object)(object)instance3 != (Object)null)
				{
					int count = res.Count;
					List<GameItem> list = new List<GameItem>();
					GameItem val2 = null;
					try
					{
						val2 = instance3.selectedBackpack;
					}
					catch
					{
					}
					if (val2 != null)
					{
						list.Add(val2);
					}
					try
					{
						GameSlotInventory afterhourPocketSlotInvLeft = instance3.afterhourPocketSlotInvLeft;
						if (afterhourPocketSlotInvLeft != null && afterhourPocketSlotInvLeft.currentItem != null)
						{
							list.Add(afterhourPocketSlotInvLeft.currentItem);
						}
					}
					catch
					{
					}
					try
					{
						GameSlotInventory afterhourPocketSlotInvBackpack = instance3.afterhourPocketSlotInvBackpack;
						if (afterhourPocketSlotInvBackpack != null && afterhourPocketSlotInvBackpack.currentItem != null)
						{
							list.Add(afterhourPocketSlotInvBackpack.currentItem);
						}
					}
					catch
					{
					}
					try
					{
						GameSlotInventory afterhourPocketSlotInvRight = instance3.afterhourPocketSlotInvRight;
						if (afterhourPocketSlotInvRight != null && afterhourPocketSlotInvRight.currentItem != null)
						{
							list.Add(afterhourPocketSlotInvRight.currentItem);
						}
					}
					catch
					{
					}
					int count2 = res.Count;
					for (int num3 = 0; num3 < count2; num3++)
					{
						if (IsBag(res[num3]))
						{
							list.Add(res[num3]);
						}
					}
					HashSet<IntPtr> seenBags = new HashSet<IntPtr>();
					for (int num4 = 0; num4 < list.Count; num4++)
					{
						MineBag(list[num4], res, seen, seenBags, 0);
					}
					int num5 = res.Count - count;
					if (num5 > 0)
					{
						sb.Append("\n    背包内容物 = ").Append(num5);
					}
				}
			}
			catch (Exception ex3)
			{
				Core.Log.Warning("[AutoStorage] 背包扫描: " + ex3.Message);
			}
			try
			{
				PlayerStore instance4 = PlayerStore.instance;
				List<GameItem> val3 = ((instance4 != null) ? instance4.FindAllItem(true) : null);
				if (val3 != null)
				{
					int count3 = res.Count;
					for (int num6 = 0; num6 < val3.Count; num6++)
					{
						GameItem val4 = val3[num6];
						if (val4 == null || !IsMachineItem(val4) || !Owned.Is(val4))
						{
							continue;
						}
						List<MachineSlot> list2 = MachineSlotsOf(val4, includeOutput: true);
						for (int num7 = 0; num7 < list2.Count; num7++)
						{
							List<GameItem> val5 = ((list2[num7] != null) ? Child(list2[num7].Inv) : null);
							if (val5 == null)
							{
								continue;
							}
							for (int num8 = 0; num8 < val5.Count; num8++)
							{
								GameItem val6 = null;
								try
								{
									val6 = val5[num8];
								}
								catch
								{
								}
								if (val6 != null)
								{
									IntPtr intPtr2 = U.Ptr((Il2CppObjectBase)(object)val6);
									if (!(intPtr2 == IntPtr.Zero) && seen.Add(intPtr2))
									{
										res.Add(val6);
									}
								}
							}
						}
					}
					int num9 = res.Count - count3;
					if (num9 > 0)
					{
						sb.Append("\n    机器输出 = ").Append(num9);
					}
				}
			}
			catch (Exception ex4)
			{
				Core.Log.Warning("[AutoStorage] 机器输出扫描: " + ex4.Message);
			}
			foreach (Container item in Containers(force: false))
			{
				if (item.Slot != null || (!Tags.Has(item.Item, "STORAGE") && !IdHasStorage(item.Item) && !IsCage(item.Item)) || !Owned.Is(item.Item) || Cfg.Get(item.Item).SourceRole != true)
				{
					continue;
				}
				try
				{
					if (item.Inv != null && !item.Inv.overrideLockRemove)
					{
						action("storage:" + U.SafeName(item.Item), Child(item.Inv));
					}
				}
				catch (Exception ex5)
				{
					Core.Log.Warning("[AutoStorage] Storage source: " + ex5.Message);
				}
			}
			foreach (Container item2 in Containers(force: false))
			{
				if (item2.Slot != null || !Owned.Is(item2.Item) || !Cfg.Get(item2.Item).StolenPriority)
				{
					continue;
				}
				try
				{
					if (item2.Inv == null || item2.Inv.overrideLockRemove)
					{
						continue;
					}
					List<GameItem> val7 = Child(item2.Inv);
					if (val7 == null)
					{
						continue;
					}
					int num10 = 0;
					for (int num11 = 0; num11 < val7.Count; num11++)
					{
						GameItem val8 = null;
						try
						{
							val8 = val7[num11];
						}
						catch
						{
						}
						if (val8 != null && !Stolen.Is(val8))
						{
							IntPtr intPtr3 = U.Ptr((Il2CppObjectBase)(object)val8);
							if (!(intPtr3 == IntPtr.Zero) && seen.Add(intPtr3))
							{
								res.Add(val8);
								num10++;
							}
						}
					}
					if (num10 > 0)
					{
						sb.Append("\n    赃物箱冷却[").Append(U.SafeName(item2.Item)).Append("] = ")
							.Append(num10);
					}
				}
				catch (Exception ex6)
				{
					Core.Log.Warning("[AutoStorage] 赃物箱冷却扫描: " + ex6.Message);
				}
			}
			report = sb.ToString();
			return res;
		}

		private static List<GameItem> Child(GameInventory inv)
		{
			if (inv == null)
			{
				return null;
			}
			try
			{
				return inv.childItems;
			}
			catch
			{
				return null;
			}
		}
	}
	internal static class Mover
	{
		private static bool _scanWarned;

		public static bool CanAccept(GameInventory inv, GameItem item)
		{
			if (inv == null || item == null)
			{
				return false;
			}
			SlotMarker val = null;
			try
			{
				val = inv.TryFindOneValidInventorySlot(item);
			}
			catch (Exception ex)
			{
				if (!_scanWarned)
				{
					_scanWarned = true;
					Core.Log.Warning("[AutoStorage] 容量扫描异常（按容器已满处理）: " + ex.Message);
				}
				return false;
			}
			return val != null;
		}

		public static bool Move(GameItem item, GameInventory target)
		{
			if (item == null || target == null)
			{
				return false;
			}
			GameInventory val = null;
			try
			{
				val = item.parentInventory;
			}
			catch
			{
			}
			if (val == null)
			{
				return false;
			}
			if (U.Same((Il2CppObjectBase)(object)val, (Il2CppObjectBase)(object)target))
			{
				return false;
			}
			try
			{
				if (val.overrideLockRemove || target.overrideLockInsert)
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
				flag = val.Expel(item);
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] Expel: " + ex.Message);
			}
			if (!flag)
			{
				return false;
			}
			try
			{
				if (item.parentInventory != null)
				{
					return false;
				}
			}
			catch
			{
			}
			if (Place(item, target))
			{
				return true;
			}
			if (!Place(item, val))
			{
				Core.Log.Warning("[AutoStorage] !! " + U.SafeName(item) + " 未能放回原处，请检查物品栏。");
			}
			return false;
		}

		private static bool Place(GameItem item, GameInventory inv)
		{
			if (item == null || inv == null)
			{
				return false;
			}
			SlotMarker val = null;
			try
			{
				val = inv.TryFindOneValidInventorySlot(item);
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] TryFindOneValidInventorySlot: " + ex.Message);
			}
			if (val == null)
			{
				return false;
			}
			try
			{
				val.AcceptUnchecked();
			}
			catch (Exception ex2)
			{
				Core.Log.Warning("[AutoStorage] AcceptUnchecked: " + ex2.Message);
				return false;
			}
			try
			{
				return U.Same((Il2CppObjectBase)(object)item.parentInventory, (Il2CppObjectBase)(object)inv);
			}
			catch
			{
				return false;
			}
		}
	}
	internal static class AutoStore
	{
		public static bool PickSawFull;

		public static void Run(bool verbose)
		{
			Cfg.Load();
			if (!Cfg.GlobalEnabled)
			{
				Core.Log.Warning("[AutoStorage] 功能已被全局关闭（Ctrl+Shift+O 切换）。");
				return;
			}
			List<Container> list = World.Containers(force: true);
			List<Container> list2 = new List<Container>();
			for (int i = 0; i < list.Count; i++)
			{
				Container container = list[i];
				if (Cfg.Get(container.Item, World.RuleSlot(container.Item, container.Slot)).Enabled)
				{
					list2.Add(container);
				}
			}
			List<Container> active = list2.FindAll((Container c) => c.Slot != null);
			HashSet<IntPtr> hashSet = new HashSet<IntPtr>();
			foreach (Container item in list)
			{
				if (item.Slot == null && (Tags.Has(item.Item, "STORAGE") || World.IdHasStorage(item.Item)) && Cfg.Get(item.Item).SourceRole == true)
				{
					hashSet.Add(U.Ptr((Il2CppObjectBase)(object)item.Inv));
				}
			}
			HashSet<IntPtr> hashSet2 = new HashSet<IntPtr>();
			foreach (Container item2 in list)
			{
				if (item2.Slot == null && World.IsCage(item2.Item) && Cfg.Get(item2.Item).SourceRole == true)
				{
					hashSet2.Add(U.Ptr((Il2CppObjectBase)(object)item2.Inv));
				}
			}
			HashSet<IntPtr> hashSet3 = new HashSet<IntPtr>();
			foreach (Container item3 in list)
			{
				if (item3.Slot == null && Cfg.Get(item3.Item).StolenPriority)
				{
					hashSet3.Add(U.Ptr((Il2CppObjectBase)(object)item3.Inv));
				}
			}
			Core.Log.Msg($"[AutoStorage] 发现 {list.Count} 个储藏容器，其中 {list2.Count} 个已启用自动入库。");
			if (list2.Count == 0)
			{
				Core.Log.Warning("[AutoStorage] 没有启用自动入库的容器。 右键储藏容器 -> 在弹出的面板里点「自动入库设置」 -> 勾选「启用自动入库」 -> 保存。");
				if (list.Count > 0)
				{
					StringBuilder stringBuilder = new StringBuilder("[AutoStorage] 已识别到的容器：");
					for (int num = 0; num < list.Count && num < 30; num++)
					{
						stringBuilder.Append("\n    ").Append(U.SafeId(list[num].Item)).Append("  uid=")
							.Append(U.SafeUid(list[num].Item))
							.Append("  ")
							.Append(U.SafeName(list[num].Item));
					}
					Core.Log.Msg(stringBuilder.ToString());
				}
				return;
			}
			List<GameItem> list3 = World.SourceItems(out var report);
			Core.Log.Msg("[AutoStorage] 待整理物品 " + list3.Count + " 件：" + report);
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			int num6 = 0;
			int num7 = 0;
			for (int num8 = 0; num8 < list3.Count; num8++)
			{
				GameItem val = list3[num8];
				if (val == null)
				{
					continue;
				}
				if (World.IsMachineItem(val) || World.IsContainerItem(val, out var _))
				{
					num5++;
					continue;
				}
				if (!Owned.Is(val))
				{
					num6++;
					if (verbose)
					{
						Core.Log.Msg("    xx 未拥有，跳过: " + U.SafeName(val) + "  [" + U.SafeId(val) + "]");
					}
					continue;
				}
				bool flag = Stolen.Is(val);
				bool flag2 = hashSet.Contains(U.Ptr((Il2CppObjectBase)(object)val.parentInventory));
				bool flag3 = hashSet2.Contains(U.Ptr((Il2CppObjectBase)(object)val.parentInventory));
				if (flag2 && !flag && hashSet3.Contains(U.Ptr((Il2CppObjectBase)(object)val.parentInventory)))
				{
					flag2 = false;
				}
				Container container2 = (flag3 ? null : Pick(active, val, flag));
				if (container2 != null && Mover.Move(val, container2.Inv))
				{
					num2++;
					if (verbose)
					{
						Core.Log.Msg("    -> " + U.SafeName(val) + " => " + container2.Name);
					}
					continue;
				}
				if (flag2)
				{
					if (container2 != null)
					{
						num4++;
					}
					else if (PickSawFull)
					{
						num7++;
					}
					else
					{
						num3++;
					}
					continue;
				}
				if (flag)
				{
					Container container3 = PickStolenPriority(list2, val);
					if (container3 != null && Mover.Move(val, container3.Inv))
					{
						num2++;
						if (verbose)
						{
							Core.Log.Msg("    -> " + U.SafeName(val) + "  =>  " + container3.Name + "   [赃物专用 " + Stolen.Tooltip(val) + "]");
						}
						continue;
					}
				}
				Container container4 = Pick(list2, val, flag);
				if (container4 == null)
				{
					if (PickSawFull)
					{
						num7++;
					}
					else
					{
						num3++;
					}
				}
				else if (Mover.Move(val, container4.Inv))
				{
					num2++;
					if (verbose)
					{
						Core.Log.Msg("    -> " + U.SafeName(val) + "  =>  " + container4.Name + (flag ? ("   [" + Stolen.Tooltip(val) + "]") : ""));
					}
				}
				else
				{
					num4++;
				}
			}
			try
			{
				for (int num9 = 0; num9 < list2.Count; num9++)
				{
					if (list2[num9].Inv != null)
					{
						list2[num9].Inv.Validate();
					}
				}
			}
			catch
			{
			}
			Core.Log.Msg($"[AutoStorage] 完成：移动 {num2} 件，无匹配容器 {num3} 件，移动失败 {num4} 件，跳过容器 {num5} 个，未拥有跳过 {num6} 件，容器已满留下 {num7} 件。");
		}

		private static Container PickStolenPriority(List<Container> active, GameItem item)
		{
			for (int i = 0; i < active.Count; i++)
			{
				Container container = active[i];
				if (container.Inv != null && !U.Same((Il2CppObjectBase)(object)container.Inv, (Il2CppObjectBase)(object)item.parentInventory) && Cfg.Get(container.Item, World.RuleSlot(container.Item, container.Slot)).StolenPriority && Mover.CanAccept(container.Inv, item))
				{
					return container;
				}
			}
			return null;
		}

		private static Container Pick(List<Container> active, GameItem item, bool stolen)
		{
			PickSawFull = false;
			for (int i = 0; i < active.Count; i++)
			{
				Container container = active[i];
				if (container.Inv == null || U.Same((Il2CppObjectBase)(object)container.Inv, (Il2CppObjectBase)(object)item.parentInventory))
				{
					continue;
				}
				CfgEntry cfgEntry = Cfg.Get(container.Item, World.RuleSlot(container.Item, container.Slot));
				if (stolen && !cfgEntry.AllowStolen)
				{
					continue;
				}
				List<string> list = Cfg.SplitList(cfgEntry.Black);
				if ((list.Count <= 0 || !Tags.HasAny(item, list)) && Tags.HasAny(item, Cfg.SplitList(cfgEntry.White)))
				{
					if (Mover.CanAccept(container.Inv, item))
					{
						return container;
					}
					PickSawFull = true;
				}
			}
			return null;
		}
	}
	internal static class SettingsUI
	{
		private static readonly List<object> _alive = new List<object>();

		private static readonly Dictionary<string, string> _draftW = new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly Dictionary<string, string> _draftB = new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly Dictionary<string, bool> _draftOn = new Dictionary<string, bool>(StringComparer.Ordinal);

		private static readonly Dictionary<string, bool> _draftAS = new Dictionary<string, bool>(StringComparer.Ordinal);

		private static readonly Dictionary<string, bool> _draftSP = new Dictionary<string, bool>(StringComparer.Ordinal);

		private static readonly Dictionary<string, string> _tagCache = new Dictionary<string, string>(StringComparer.Ordinal);

		public static void ResetForSave()
		{
			_draftW.Clear();
			_draftB.Clear();
			_draftOn.Clear();
			_draftAS.Clear();
			_draftSP.Clear();
			_tagCache.Clear();
		}

		private static Action Act(Action a)
		{
			if (a == null)
			{
				return null;
			}
			Action val = Action.op_Implicit(a);
			_alive.Add(a);
			_alive.Add(val);
			return val;
		}

		private static Action<bool> ActB(Action<bool> a)
		{
			if (a == null)
			{
				return null;
			}
			Action<bool> val = Action<bool>.op_Implicit(a);
			_alive.Add(a);
			_alive.Add(val);
			return val;
		}

		private static Action<string> ActS(Action<string> a)
		{
			if (a == null)
			{
				return null;
			}
			Action<string> val = Action<string>.op_Implicit(a);
			_alive.Add(a);
			_alive.Add(val);
			return val;
		}

		public static string WindowId(GameItem c, string slot)
		{
			return "AutoStorage.cfg." + Cfg.Key(c, slot).Replace('#', '_').Replace('.', '_')
				.Replace('@', '_');
		}

		public static void Close(GameItem c)
		{
			Close(c, null);
		}

		public static void Close(GameItem c, MachineSlot slot)
		{
			string slot2 = slot?.SlotId;
			try
			{
				CustomUIManager.Instance.CloseWindow(WindowId(c, slot2));
			}
			catch
			{
			}
			string key = Cfg.Key(c, slot2);
			_draftOn.Remove(key);
			_draftW.Remove(key);
			_draftB.Remove(key);
			_draftAS.Remove(key);
			_draftSP.Remove(key);
			_tagCache.Remove(key);
		}

		private static void CacheDraft(GameItem container, MachineSlot slot)
		{
			string key = Cfg.Key(container, slot?.SlotId);
			CfgEntry cfgEntry = Cfg.Get(container, slot?.SlotId);
			_draftOn[key] = cfgEntry.Enabled;
			_draftW[key] = cfgEntry.White;
			_draftB[key] = cfgEntry.Black;
			_draftAS[key] = cfgEntry.AllowStolen;
			_draftSP[key] = cfgEntry.StolenPriority;
		}

		public static void Open(GameItem container)
		{
			Open(container, rebuildDraft: true, null);
		}

		public static void Open(GameItem container, MachineSlot slot)
		{
			Open(container, rebuildDraft: true, slot);
		}

		private static void AddSlotControls(CustomUIBuilder b, GameItem item, MachineSlot page)
		{
			if (!World.IsMachineItem(item))
			{
				if (Tags.Has(item, "STORAGE") || World.IdHasStorage(item) || World.IsCage(item))
				{
					bool flag = World.IsCage(item);
					b.AddToggle(flag ? L.T("作为整理来源（笼内物品只供给名单匹配的储藏箱；可同时接收入库）", "Sorting source (cage contents feed list-matching storage boxes; can also receive items)") : L.T("作为整理来源（供料给机器；可同时接收入库）", "Sorting source (feeds machines; can also receive items)"), Cfg.Get(item).SourceRole == true, ActB((bool value) =>
					{
						Cfg.SaveRoles(item, null, target: true, value);
						Open(item, rebuildDraft: false, page);
					}), "storage_source");
					b.AddLabel(flag ? L.T("笼内物品只供给名单匹配的储藏箱；无匹配或容器已满则留下。启用自动入库后仍按名单接收外部物品。", "Cage contents feed list-matching storage boxes only; otherwise stay in the cage. Enable auto-storage to also receive matching items.") : L.T("容器内材料只供给机器；无匹配或机器已满则留下。启用自动入库后仍按名单接收外部物品。", "Stored materials feed matching machines only; otherwise stay here. Enable auto-storage to also receive matching items."), (string)null);
				}
				return;
			}
			b.AddLabel(L.T("槽位用途（勾选后立即保存；同一槽位只能选择一种用途）", "Slot roles (saved immediately; one role per slot)"), (string)null);
			b.BeginScroll(140f);
			b.BeginColumn(4f);
			foreach (MachineSlot item2 in World.AllMachineSlots(item))
			{
				MachineSlot current2 = item2;
				b.BeginRow(6f);
				b.AddLabel(L.Slot(current2.Label), (string)null);
				if (current2.DefaultSource)
				{
					b.AddLabel(L.T("输出槽：不可用于类别搜索 / 物品放置", "Output: cannot scan types or receive items"), (string)null);
				}
				else
				{
					b.AddToggle(L.T("类别搜索 / 物品放置", "Scan types / Receive items"), World.IsTarget(item, current2), ActB((bool value) =>
					{
						Cfg.SaveRoles(item, current2.SlotId, value, !value && World.IsSource(item, current2));
						_tagCache.Remove(Cfg.Key(item, (page != null) ? page.SlotId : null));
						Open(item, rebuildDraft: false, page);
					}), "target_" + current2.SlotId);
				}
				b.AddToggle(L.T("整理来源", "Sorting source"), World.IsSource(item, current2), ActB((bool value) =>
				{
					Cfg.SaveRoles(item, current2.SlotId, !value && World.IsTarget(item, current2), value);
					_tagCache.Remove(Cfg.Key(item, (page != null) ? page.SlotId : null));
					Open(item, rebuildDraft: false, page);
				}), "source_" + current2.SlotId);
				b.End();
			}
			b.End();
			b.End();
		}

		public static void Open(GameItem container, bool rebuildDraft, MachineSlot slot)
		{
			if (container == null || !Owned.Is(container))
			{
				return;
			}
			if (World.IsMachineItem(container))
			{
				MachineSlot machineSlot = World.SelectPage(container, slot);
				if (((slot != null) ? slot.SlotId : null) != machineSlot?.SlotId)
				{
					try
					{
						CustomUIManager.Instance.CloseWindow(WindowId(container, (slot != null) ? slot.SlotId : null));
					}
					catch
					{
					}
				}
				slot = machineSlot;
			}
			string slotId = ((slot != null) ? slot.SlotId : null);
			CustomUIManager mgr = null;
			try
			{
				mgr = CustomUIManager.Instance;
			}
			catch
			{
			}
			if ((Object)(object)mgr == (Object)null)
			{
				Core.Log.Warning("[AutoStorage] CustomUIManager 不可用。");
				return;
			}
			string text = WindowId(container, slotId);
			string key = Cfg.Key(container, slotId);
			if (rebuildDraft || !_draftOn.ContainsKey(key))
			{
				CacheDraft(container, slot);
			}
			L.Track(text, () =>
			{
				_tagCache.Remove(key);
				Open(container, rebuildDraft: false, slot);
			});
			try
			{
				if (mgr.IsOpen(text))
				{
					mgr.CloseWindow(text);
				}
			}
			catch
			{
			}
			string text2 = U.SafeName(container);
			if (slot != null)
			{
				text2 = text2 + "·" + L.Slot(slot.Label);
			}
			if (string.IsNullOrEmpty(text2))
			{
				text2 = L.T("容器", "Container");
			}
			try
			{
				CustomUIBuilder val = mgr.CreateWindow(text, L.T("自动入库设置 — ", "AutoStorage settings — ") + text2, CustomUIManager.LAYER_OVERLAY);
				val.SetDraggable(true);
				val.SetCloseOnEscape(true);
				val.SetSize(720f, 720f);
				val.BeginColumn(6f);
				val.AddLabel("<b>" + text2 + "</b>    id=" + U.SafeId(container) + "    uid=" + U.SafeUid(container), (string)null);
				AddSlotControls(val, container, slot);
				if (World.IsMachineItem(container) && slot == null)
				{
					val.AddLabel(L.T("尚未选择类别搜索 / 物品放置槽；可在上方勾选。", "No destination selected. Select a slot above."), (string)null);
					val.AddButton(L.T("关闭", "Close"), Act(() =>
					{
						Close(container, slot);
					}), "close");
					val.End();
					val.Center();
					val.Show();
					return;
				}
				val.AddToggle(L.T("启用自动入库 (Ctrl+R)", "Enable auto-storage (Ctrl+R)"), _draftOn[key], ActB((bool v) =>
				{
					_draftOn[key] = v;
				}), "on");
				val.AddToggle(L.T("整理时接收赃物（带 [赃物(热度)] 标签的物品）", "Accept stolen items"), _draftAS[key], ActB((bool v) =>
				{
					_draftAS[key] = v;
				}), "as");
				val.AddToggle(L.T("赃物专用容器（赃物无视名单优先放这里；热度消失后下次整理自动移出）", "Prioritize stolen items (ignore lists; items auto-leave once heat is gone)"), _draftSP[key], ActB((bool v) =>
				{
					_draftSP[key] = v;
				}), "sp");
				val.AddLabel(L.T("白名单类别（逗号分隔；留空 = 接收所有未被黑名单排除的物品）", "Allowed types (comma-separated; empty = allow all except blocked)"), (string)null);
				val.AddInput(L.T("例如 food,medical,weapon", "e.g. food,medical,weapon"), _draftW[key], ActS((string s) =>
				{
					_draftW[key] = s ?? "";
				}), "wl");
				val.AddLabel(L.T("黑名单类别（逗号分隔；优先级高于白名单）", "Blocked types (comma-separated; override allowed types)"), (string)null);
				val.AddInput(L.T("例如 contraband,fuel", "e.g. contraband,fuel"), _draftB[key], ActS((string s) =>
				{
					_draftB[key] = s ?? "";
				}), "bl");
				val.AddLabel(L.T("也支持 id: 前缀，按物品内部 id 前缀匹配，例如 id:storage_bay", "Use id: to match item ID prefixes, e.g. id:storage_bay"), (string)null);
				val.BeginRow(6f);
				val.AddButton(L.T("保存", "Save"), Act(() =>
				{
					Cfg.Save(container, slotId, _draftOn[key], _draftW[key], _draftB[key], _draftAS[key], _draftSP[key]);
					Close(container, slot);
				}), "save");
				val.AddButton(L.T("清空白/黑名单", "Clear lists"), Act(() =>
				{
					_draftW[key] = "";
					_draftB[key] = "";
					Cfg.Save(container, slotId, _draftOn[key], "", "", _draftAS[key], _draftSP[key]);
					Open(container, rebuildDraft: false, slot);
				}), "clear");
				val.AddButton(L.T("扫描可用类别", "Scan available types"), Act(() =>
				{
					_tagCache[key] = (World.IsMachineItem(container) ? Tags.ScanMachine(container, slotId) : Tags.Scan(container));
					Open(container, rebuildDraft: false, slot);
				}), "scan");
				val.AddButton(L.T("关闭", "Close"), Act(() =>
				{
					Close(container, slot);
				}), "close");
				val.End();
				if (slot != null && World.IsMachineItem(container))
				{
					List<MachineSlot> list = World.TargetPages(container);
					if (list != null && list.Count > 1)
					{
						int num = 0;
						for (int num2 = 0; num2 < list.Count; num2++)
						{
							if (list[num2].SlotId == slot.SlotId)
							{
								num = num2;
								break;
							}
						}
						MachineSlot next = list[(num + 1) % list.Count];
						val.BeginRow(6f);
						val.AddButton(L.T("切换槽位：", "Switch slot: ") + L.Slot(slot.Label) + "  →  " + L.Slot(next.Label) + L.T("（各页分别保存）", " (save each page separately)"), Act(() =>
						{
							try
							{
								mgr.CloseWindow(WindowId(container, slot.SlotId));
							}
							catch
							{
							}
							Open(container, rebuildDraft: false, next);
						}), "nextslot");
						val.End();
					}
				}
				List<Tags.TagRow> rows = Tags.Rows;
				if (_tagCache.ContainsKey(key) && rows.Count > 0)
				{
					Dictionary<string, Tags.TagRow> dictionary = new Dictionary<string, Tags.TagRow>(StringComparer.OrdinalIgnoreCase);
					List<string> list2 = new List<string>();
					List<Tags.TagRow> list3 = new List<Tags.TagRow>();
					HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
					for (int num3 = 0; num3 < rows.Count; num3++)
					{
						Tags.TagRow tagRow = rows[num3];
						if (!string.IsNullOrEmpty(tagRow.Key))
						{
							if (!dictionary.ContainsKey(tagRow.Key))
							{
								dictionary[tagRow.Key] = tagRow;
								list2.Add(tagRow.Key);
							}
							string item = tagRow.ItemName + "\u0001" + tagRow.ItemId + "\u0001" + tagRow.Note;
							if (hashSet.Add(item))
							{
								list3.Add(tagRow);
							}
						}
					}
					list2.Sort(StringComparer.OrdinalIgnoreCase);
					val.AddLabel(L.T("已发现的类别（", "Discovered types (") + list2.Count + L.T(" 种，已去重；点 +白 / +黑 按类别加入名单）：", "; use +Allow / +Block to add types):"), (string)null);
					val.BeginScroll(170f);
					for (int num4 = 0; num4 < list2.Count && num4 < 300; num4++)
					{
						string tt = list2[num4];
						Tags.TagRow tagRow2 = dictionary[tt];
						val.BeginRow(4f);
						val.AddLabel(tagRow2.Label, (string)null);
						val.AddButton(L.T("+白", "+Allow"), Act(() =>
						{
							_draftW[key] = AddPart(_draftW[key], tt);
							Cfg.Save(container, slotId, _draftOn[key], _draftW[key], _draftB[key], _draftAS[key], _draftSP[key]);
							Open(container, rebuildDraft: false, slot);
						}), "w_" + tt);
						val.AddButton(L.T("+黑", "+Block"), Act(() =>
						{
							_draftB[key] = AddPart(_draftB[key], tt);
							Cfg.Save(container, slotId, _draftOn[key], _draftW[key], _draftB[key], _draftAS[key], _draftSP[key]);
							Open(container, rebuildDraft: false, slot);
						}), "b_" + tt);
						val.End();
					}
					val.End();
					val.AddLabel(L.T("物品（", "Items (") + list3.Count + L.T(" 种；+白id / +黑id 按物品 id 加入名单）：", "; use +Allow ID / +Block ID):"), (string)null);
					val.BeginScroll(110f);
					for (int num5 = 0; num5 < list3.Count && num5 < 100; num5++)
					{
						Tags.TagRow tagRow3 = list3[num5];
						string text3 = tagRow3.ItemName + "  [" + tagRow3.ItemId + "]" + (string.IsNullOrEmpty(tagRow3.Note) ? "" : ("  " + tagRow3.Note));
						if (!string.IsNullOrEmpty(tagRow3.ItemId))
						{
							string gid = "id:" + tagRow3.ItemId;
							val.BeginRow(4f);
							val.AddLabel("<b>" + text3 + "</b>", (string)null);
							val.AddButton(L.T("+白id", "+Allow ID"), Act(() =>
							{
								_draftW[key] = AddPart(_draftW[key], gid);
								Cfg.Save(container, slotId, _draftOn[key], _draftW[key], _draftB[key], _draftAS[key], _draftSP[key]);
								Open(container, rebuildDraft: false, slot);
							}), "gw_" + tagRow3.ItemId);
							val.AddButton(L.T("+黑id", "+Block ID"), Act(() =>
							{
								_draftB[key] = AddPart(_draftB[key], gid);
								Cfg.Save(container, slotId, _draftOn[key], _draftW[key], _draftB[key], _draftAS[key], _draftSP[key]);
								Open(container, rebuildDraft: false, slot);
							}), "gb_" + tagRow3.ItemId);
							val.End();
						}
						else
						{
							val.AddLabel("<b>" + text3 + "</b>", (string)null);
						}
					}
					val.End();
				}
				else
				{
					val.AddLabel(L.T("提示：点「扫描可用类别」可从当前存档的物品上收集类别名称。", "Click Scan available types to inspect items in the selected slot."), (string)null);
				}
				val.End();
				val.Center();
				val.Show();
			}
			catch (Exception ex)
			{
				Core.Log.Error("[AutoStorage] 设置窗口创建失败: " + ex);
			}
		}

		private static string AddPart(string csv, string part)
		{
			List<string> list = Cfg.SplitList(csv);
			for (int i = 0; i < list.Count; i++)
			{
				if (string.Equals(list[i], part, StringComparison.OrdinalIgnoreCase))
				{
					return csv;
				}
			}
			list.Add(part);
			return string.Join(",", list.ToArray());
		}
	}
	internal static class ContainerList
	{
		private const string PanelId = "AutoStorage.list";

		private static readonly List<object> _alive = new List<object>();

		private static Action Act(Action a)
		{
			if (a == null)
			{
				return null;
			}
			Action val = Action.op_Implicit(a);
			_alive.Add(a);
			_alive.Add(val);
			return val;
		}

		private static Action<bool> ActB(Action<bool> a)
		{
			if (a == null)
			{
				return null;
			}
			Action<bool> val = Action<bool>.op_Implicit(a);
			_alive.Add(a);
			_alive.Add(val);
			return val;
		}

		public static void Toggle()
		{
			CustomUIManager val = null;
			bool flag = false;
			try
			{
				val = CustomUIManager.Instance;
				if ((Object)(object)val != (Object)null)
				{
					flag = val.IsOpen("AutoStorage.list");
				}
			}
			catch
			{
			}
			if (flag)
			{
				try
				{
					val.CloseWindow("AutoStorage.list");
					return;
				}
				catch
				{
					return;
				}
			}
			Show();
		}

		public static void Show()
		{
			CustomUIManager val = null;
			try
			{
				val = CustomUIManager.Instance;
			}
			catch
			{
			}
			if ((Object)(object)val == (Object)null)
			{
				Core.Log.Warning("[AutoStorage] CustomUIManager 不可用，无法显示容器总览。");
				return;
			}
			try
			{
				val.CloseWindow("AutoStorage.list");
			}
			catch
			{
			}
			try
			{
				List<Container> list = World.Containers(force: true);
				CustomUIBuilder val2 = val.CreateWindow("AutoStorage.list", L.T("AutoStorage — 容器总览", "AutoStorage — Containers"), CustomUIManager.LAYER_OVERLAY);
				L.Track("AutoStorage.list", () =>
				{
					Show();
				});
				val2.SetDraggable(true);
				val2.SetCloseOnEscape(true);
				val2.SetSize(660f, 560f);
				val2.BeginColumn(6f);
				val2.AddLabel(L.T("<b>检测到 ", "<b>Found ") + list.Count + L.T(" 个储藏容器</b>    (Ctrl+L 关闭)", " containers</b>    (Ctrl+L to close)"), (string)null);
				val2.BeginRow(6f);
				val2.AddButton(L.T("刷新", "Refresh"), Act(() =>
				{
					Show();
				}), "refresh");
				val2.AddToggle(L.T("包含所有带背包的物品", "Include all inventory-bearing items"), World.IncludeAll, ActB((bool v) =>
				{
					World.IncludeAll = v;
					Show();
				}), "all");
				val2.AddButton(L.T("立即整理 (Ctrl+R)", "Sort now (Ctrl+R)"), Act(() =>
				{
					AutoStore.Run(verbose: true);
					Show();
				}), "run");
				val2.End();
				if (list.Count == 0)
				{
					val2.AddLabel(L.T("没有检测到容器，请检查物品类型、归属及目标槽位勾选。", "No containers found. Check item types, ownership and selected destination slots."), (string)null);
				}
				else
				{
					val2.BeginScroll(370f);
					for (int num = 0; num < list.Count; num++)
					{
						Container cc = list[num];
						GameItem c = cc.Item;
						CfgEntry cfgEntry = Cfg.Get(c, World.RuleSlot(c, cc.Slot));
						string text = ((cc.Slot == null && cfgEntry.SourceRole == true) ? L.T("[整理来源] ", "[Sorting source] ") : "") + (cfgEntry.Enabled ? L.T("[已启用] ", "[Enabled] ") : L.T("[未启用] ", "[Disabled] ")) + (cfgEntry.StolenPriority ? L.T("[赃物专用] ", "[Stolen priority] ") : "") + ((!cfgEntry.AllowStolen) ? L.T("[不收赃物] ", "[No stolen items] ") : "") + cc.Name + "    id=" + U.SafeId(c) + "    uid=" + U.SafeUid(c) + ((cc.Slot != null) ? ("    slot=" + cc.Slot) : "");
						val2.BeginRow(4f);
						val2.AddLabel(text, (string)null);
						val2.AddButton(L.T("设置", "Settings"), Act(() =>
						{
							SettingsUI.Open(c, (cc.Slot == null) ? null : new MachineSlot
							{
								SlotId = cc.Slot,
								Label = cc.SlotLabel,
								Inv = cc.Inv
							});
						}), "s" + num);
						val2.AddButton(cfgEntry.Enabled ? L.T("停用", "Disable") : L.T("启用", "Enable"), Act(() =>
						{
							CfgEntry cfgEntry2 = Cfg.Get(c, World.RuleSlot(c, cc.Slot));
							Cfg.Save(c, World.RuleSlot(c, cc.Slot), !cfgEntry2.Enabled, cfgEntry2.White, cfgEntry2.Black, cfgEntry2.AllowStolen, cfgEntry2.StolenPriority);
							Show();
						}), "t" + num);
						val2.End();
					}
					val2.End();
				}
				val2.AddLabel(L.T("白名单留空 = 接收除黑名单外的全部物品；黑名单优先。均支持 id: 前缀匹配。", "Empty allow list accepts all except blocked items. Both lists support id: prefixes."), (string)null);
				val2.End();
				val2.Center();
				val2.Show();
				Core.Log.Msg("[AutoStorage] 容器总览已打开：" + list.Count + " 个。");
			}
			catch (Exception ex)
			{
				Core.Log.Error("[AutoStorage] 容器总览创建失败: " + ex);
			}
		}
	}
	internal static class MenuButton
	{
		private const string PanelId = "AutoStorage.menu";

		private static readonly List<object> _alive = new List<object>();

		private static IntPtr _panelFor = IntPtr.Zero;

		private static float _shownAt = -1000f;

		private static bool _dirty = true;

		private static IntPtr _lastSeenItem = IntPtr.Zero;

		private static float _lastMenuFrame = -1000f;

		private static ItemContextHandler _src;

		private static Action Act(Action a)
		{
			if (a == null)
			{
				return null;
			}
			Action val = Action.op_Implicit(a);
			_alive.Add(a);
			_alive.Add(val);
			return val;
		}

		private static Action<bool> ActB(Action<bool> a)
		{
			if (a == null)
			{
				return null;
			}
			Action<bool> val = Action<bool>.op_Implicit(a);
			_alive.Add(a);
			_alive.Add(val);
			return val;
		}

		public static void ClosePanel()
		{
			try
			{
				CustomUIManager.Instance.CloseWindow("AutoStorage.menu");
			}
			catch
			{
			}
			_panelFor = IntPtr.Zero;
		}

		public static void Inject(ItemContextHandler h)
		{
			if (!((Object)(object)h == (Object)null))
			{
				_src = h;
				_dirty = true;
				Core.Log.Msg("[AutoStorage] OnEventInit 触发（handler=" + U.Ptr((Il2CppObjectBase)(object)h) + "）。");
			}
		}

		public static void OnMenuFrame(ItemContextHandler h)
		{
			if (!((Object)(object)h == (Object)null))
			{
				_src = h;
				float realtimeSinceStartup = Time.realtimeSinceStartup;
				if (realtimeSinceStartup - _lastMenuFrame > 0.5f)
				{
					_dirty = true;
					Core.Log.Msg("[AutoStorage] 检测到右键菜单活动（OnEventUpdate 沿）。");
				}
				_lastMenuFrame = realtimeSinceStartup;
			}
		}

		public static void OnRightClick()
		{
			_dirty = true;
			Core.Log.Msg("[AutoStorage] 检测到鼠标右键按下。");
		}

		public static void Tick()
		{
			ItemContextHandler val = _src;
			if ((Object)(object)val == (Object)null)
			{
				try
				{
					val = ItemContextHandler.current;
				}
				catch
				{
				}
			}
			if ((Object)(object)val == (Object)null)
			{
				return;
			}
			GameItem val2 = null;
			try
			{
				val2 = val.currentItem;
			}
			catch
			{
			}
			if (val2 == null)
			{
				return;
			}
			IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)val2);
			if (intPtr == IntPtr.Zero || (intPtr == _lastSeenItem && !_dirty))
			{
				return;
			}
			bool flag = false;
			try
			{
				PixelWindow contextWindow = val.contextWindow;
				if (contextWindow != null)
				{
					flag = contextWindow.IsVisible();
				}
			}
			catch
			{
			}
			if (!flag)
			{
				return;
			}
			_lastSeenItem = intPtr;
			_dirty = false;
			if (intPtr == _panelFor)
			{
				bool flag2 = false;
				try
				{
					flag2 = CustomUIManager.Instance.IsOpen("AutoStorage.menu");
				}
				catch
				{
				}
				if (flag2 || Time.realtimeSinceStartup - _shownAt < 1f)
				{
					return;
				}
			}
			Handle(val2);
		}

		private static void Handle(GameItem item)
		{
			if (item == null || World.BlockedId(U.SafeId(item)) || !Owned.Is(item) || (!World.IsMachineItem(item) && !Tags.Has(item, "STORAGE") && !World.IdHasStorage(item) && !World.IsCage(item)))
			{
				return;
			}
			GameInventory val = null;
			try
			{
				val = (GameInventory)(object)ItemContextHandler.current.currentInventory;
			}
			catch
			{
			}
			GameInventory inv = null;
			GameInventory inv2 = null;
			bool flag = World.IsInventoryItem(item, out inv);
			bool flag2 = World.IsContainerItem(item, out inv2);
			bool flag3 = World.IsMachineItem(item);
			List<MachineSlot> list = (flag3 ? World.AllMachineSlots(item) : null);
			bool flag4 = flag3 && list != null && list.Count > 0;
			Core.Log.Msg("[AutoStorage] 右键命中 → item='" + U.SafeId(item) + "' 名称=" + U.SafeName(item) + "  currentInventory=" + ((val != null) ? "有" : "无") + "  自带背包=" + flag + "  inventory=" + ((inv != null) ? "有" : "无") + "  判定为储藏容器=" + flag2 + "  机器槽位=" + (list?.Count ?? 0) + "  (includeAll=" + World.IncludeAll + ")");
			if (flag4)
			{
				if (World.TargetPages(item).Count > 1)
				{
					Core.Log.Msg("[AutoStorage] 该机器有 " + list.Count + " 个可配置槽位，面板打开「" + list[0].Label + "」，面板内可切换槽位。");
				}
				ShowPanelSlots(item, list, 0);
			}
			else if (!flag3)
			{
				if (!flag2)
				{
					Core.Log.Warning("[AutoStorage] 该物品不算储藏容器，未弹面板。 若要强制允许所有带背包的物品，按 Ctrl+L 打开容器总览并勾选「包含所有带背包的物品」。");
				}
				else
				{
					ShowPanel(item, null);
				}
			}
		}

		public static void ShowPanel(GameItem container)
		{
			ShowPanel(container, null, null, 0);
		}

		public static void ShowPanelSlots(GameItem container, List<MachineSlot> slots, int idx)
		{
			if (slots == null || slots.Count == 0)
			{
				ShowPanel(container, null, null, 0);
				return;
			}
			if (idx < 0 || idx >= slots.Count)
			{
				idx = 0;
			}
			ShowPanel(container, slots[idx], slots, idx);
		}

		public static void ShowPanel(GameItem container, MachineSlot slot)
		{
			ShowPanel(container, slot, null, 0);
		}

		private static void ShowPanel(GameItem container, MachineSlot slot, List<MachineSlot> slots, int slotIdx)
		{
			if (!Owned.Is(container))
			{
				return;
			}
			CustomUIManager val = null;
			try
			{
				val = CustomUIManager.Instance;
			}
			catch
			{
			}
			if ((Object)(object)val == (Object)null)
			{
				Core.Log.Warning("[AutoStorage] CustomUIManager.Instance 为 null，右键面板无法创建。请改用 Ctrl+L 容器总览（若它也打不开，说明本作没有可用的 CustomUI 框架）。");
				return;
			}
			if (World.IsMachineItem(container))
			{
				slots = World.TargetPages(container);
				slot = World.SelectPage(container, slot);
				slotIdx = 0;
				for (int i = 0; i < slots.Count; i++)
				{
					if (slot != null && slots[i].SlotId == slot.SlotId)
					{
						slotIdx = i;
					}
				}
			}
			try
			{
				val.CloseWindow("AutoStorage.menu");
			}
			catch
			{
			}
			try
			{
				string text = U.SafeName(container);
				if (slot != null)
				{
					text = text + "·" + L.Slot(slot.Label);
				}
				if (string.IsNullOrEmpty(text))
				{
					text = L.T("储藏容器", "Storage container");
				}
				bool enabled = Cfg.Get(container, (slot != null) ? slot.SlotId : null).Enabled;
				CustomUIBuilder val2 = val.CreateWindow("AutoStorage.menu", L.T("储藏容器 — ", "Storage — ") + text, CustomUIManager.LAYER_OVERLAY);
				L.Track("AutoStorage.menu", () =>
				{
					ShowPanel(container, slot);
				});
				val2.SetDraggable(true);
				val2.SetCloseOnEscape(true);
				val2.SetSize(420f, 210f);
				val2.BeginColumn(6f);
				val2.AddLabel("<b>" + text + "</b>  id=" + U.SafeId(container), (string)null);
				if (!World.IsMachineItem(container) || slot != null)
				{
					val2.AddToggle(L.T("启用自动入库", "Enable auto-storage"), enabled, ActB((bool v) =>
					{
						CfgEntry cfgEntry = Cfg.Get(container, (slot != null) ? slot.SlotId : null);
						Cfg.Save(container, (slot != null) ? slot.SlotId : null, v, cfgEntry.White, cfgEntry.Black);
					}), "on");
				}
				val2.AddLabel(L.T("白名单 = ", "Allowed = ") + ShowList(Cfg.Get(container, (slot != null) ? slot.SlotId : null).White) + L.T("    黑名单 = ", "    Blocked = ") + ShowList(Cfg.Get(container, (slot != null) ? slot.SlotId : null).Black), (string)null);
				val2.BeginRow(6f);
				val2.AddButton(L.T("设置白名单/黑名单类别", "Configure slots / types"), Act(() =>
				{
					SettingsUI.Open(container, slot);
					ClosePanel();
				}), "open");
				val2.AddButton(L.T("立即整理 (Ctrl+R)", "Sort now (Ctrl+R)"), Act(() =>
				{
					AutoStore.Run(verbose: true);
					ClosePanel();
				}), "run");
				val2.AddButton(L.T("关闭", "Close"), Act(() =>
				{
					ClosePanel();
				}), "close");
				val2.End();
				if (slots != null && slots.Count > 1)
				{
					MachineSlot machineSlot = slots[(slotIdx + 1) % slots.Count];
					val2.BeginRow(6f);
					val2.AddButton(L.T("切换槽位：", "Switch slot: ") + ((slot != null) ? L.Slot(slot.Label) : "?") + "  →  " + L.Slot(machineSlot.Label), Act(() =>
					{
						ClosePanel();
						ShowPanelSlots(container, slots, (slotIdx + 1) % slots.Count);
					}), "nextslot");
					val2.End();
				}
				val2.End();
				val2.Center();
				val2.Show();
				_panelFor = U.Ptr((Il2CppObjectBase)(object)container);
				_shownAt = Time.realtimeSinceStartup;
				Core.Log.Msg("[AutoStorage] 右键面板已显示：'" + U.SafeId(container) + "'");
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] 右键面板创建失败: " + ex.Message);
			}
		}

		private static string ShowList(string csv)
		{
			if (!string.IsNullOrEmpty(csv))
			{
				return csv;
			}
			return L.T("(空)", "(empty)");
		}
	}
	[HarmonyPatch]
	internal static class Patch_ContextMenu
	{
		private static MethodBase TargetMethod()
		{
			try
			{
				return AccessTools.Method(typeof(ItemContextHandler), "OnEventInit", new Type[0], (Type[])null);
			}
			catch
			{
				return null;
			}
		}

		private static void Postfix(ItemContextHandler __instance)
		{
			try
			{
				MenuButton.Inject(__instance);
			}
			catch (Exception ex)
			{
				Core.Log.Warning("[AutoStorage] OnEventInit: " + ex.Message);
			}
		}
	}
	[HarmonyPatch]
	internal static class Patch_ContextUpdate
	{
		private static MethodBase TargetMethod()
		{
			try
			{
				return AccessTools.Method(typeof(ItemContextHandler), "OnEventUpdate", new Type[0], (Type[])null);
			}
			catch
			{
				return null;
			}
		}

		private static void Postfix(ItemContextHandler __instance)
		{
			try
			{
				MenuButton.OnMenuFrame(__instance);
			}
			catch
			{
			}
		}
	}
	public class Core : MelonMod
	{
		public static Instance Log;

		private IntPtr _lastCtx = IntPtr.Zero;

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			Hotkeys.Init();
			Log.Msg("=========================================================");
			Log.Msg(" AutoStorage 2.2.0  —  Probably Stolen");
			Log.Msg("  Ctrl+R        把 物品栏 / 谈判柜台 里符合 tag 规则的物品自动放进储藏容器");
			Log.Msg("  Ctrl+L        容器总览：列出所有检测到的容器，逐个打开设置（右键失效时用这个）");
			Log.Msg("  右键储藏容器  弹出面板 -> 点「自动入库设置」配置白名单/黑名单 tag");
			Log.Msg("  右键机器      熔炉(输入)/催陈器(容器)/冰箱(冰镇区/冷冻区)等也可配置入库");
			Log.Msg("  右键动物笼    可勾「整理来源」，笼内物品只供给名单匹配的储藏箱");
			Log.Msg("  Ctrl+Shift+O  临时启用/禁用整个功能");
			Log.Msg("=========================================================");
		}

		public override void OnLateInitializeMelon()
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Expected Obj, but got Unknown
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_006e: Expected Obj, but got Unknown
			//IL_00d1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00de: Expected Obj, but got Unknown
			try
			{
				Cfg.Load();
				Harmony val = new Harmony("workbuddy.autostorage");
				MethodInfo methodInfo = AccessTools.Method(typeof(ItemContextHandler), "OnEventInit", new Type[0], (Type[])null);
				if (methodInfo == null)
				{
					Log.Warning("[AutoStorage] 未找到 ItemContextHandler.OnEventInit，右键按钮改用轮询兜底。");
				}
				else
				{
					MethodInfo method = typeof(Patch_ContextMenu).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
					val.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(method), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					Log.Msg("[AutoStorage] 已挂钩 ItemContextHandler.OnEventInit。");
				}
				MethodInfo methodInfo2 = AccessTools.Method(typeof(ItemContextHandler), "OnEventUpdate", new Type[0], (Type[])null);
				if (methodInfo2 == null)
				{
					Log.Warning("[AutoStorage] 未找到 ItemContextHandler.OnEventUpdate，右键二次打开可能失效。");
					return;
				}
				MethodInfo method2 = typeof(Patch_ContextUpdate).GetMethod("Postfix", BindingFlags.Static | BindingFlags.NonPublic);
				val.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(method2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Log.Msg("[AutoStorage] 已挂钩 ItemContextHandler.OnEventUpdate。");
			}
			catch (Exception ex)
			{
				Log.Error("[AutoStorage] Harmony 挂钩失败: " + ex);
			}
		}

		public override void OnUpdate()
		{
			try
			{
				Cfg.Load();
				L.Tick();
				if (Input.GetMouseButtonDown(1))
				{
					MenuButton.OnRightClick();
				}
				MenuButton.Tick();
				if (Hotkeys.Down(Hotkeys.Sort))
				{
					AutoStore.Run(verbose: true);
					return;
				}
				if (Hotkeys.Down(Hotkeys.Overview))
				{
					ContainerList.Toggle();
					return;
				}
				if (Hotkeys.Down(Hotkeys.Toggle))
				{
					Cfg.GlobalEnabled = !Cfg.GlobalEnabled;
					Cfg.Save();
					Log.Msg("[AutoStorage] 全局开关 = " + Cfg.GlobalEnabled);
					return;
				}
				ItemContextHandler val = null;
				try
				{
					val = ItemContextHandler.current;
				}
				catch
				{
				}
				if ((Object)(object)val != (Object)null)
				{
					IntPtr intPtr = U.Ptr((Il2CppObjectBase)(object)val);
					if (intPtr != IntPtr.Zero && intPtr != _lastCtx)
					{
						_lastCtx = intPtr;
						MenuButton.Inject(val);
					}
				}
				else
				{
					_lastCtx = IntPtr.Zero;
				}
			}
			catch (Exception ex)
			{
				Log.Error("[AutoStorage] OnUpdate: " + ex);
			}
		}

		private static void Dump()
		{
			try
			{
				List<Container> list = World.Containers(force: true);
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("[AutoStorage] 诊断 — 容器数=").Append(list.Count);
				string value;
				try
				{
					value = (((Object)(object)CustomUIManager.Instance != (Object)null) ? "可用" : "null");
				}
				catch (Exception ex)
				{
					value = "异常:" + ex.Message;
				}
				stringBuilder.Append("  CustomUIManager=").Append(value).Append("  includeAll=")
					.Append(World.IncludeAll)
					.Append("  globalOn=")
					.Append(Cfg.GlobalEnabled);
				Tags.ResetStats();
				for (int i = 0; i < list.Count; i++)
				{
					Container container = list[i];
					CfgEntry cfgEntry = Cfg.Get(container.Item, World.RuleSlot(container.Item, container.Slot));
					HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					Tags.Collect(container.Item, hashSet);
					stringBuilder.Append("\n  #").Append(i).Append("  id=")
						.Append(U.SafeId(container.Item))
						.Append("  uid=")
						.Append(U.SafeUid(container.Item))
						.Append("  name=")
						.Append(container.Name)
						.Append("  slot=")
						.Append(container.Slot ?? "-")
						.Append("  inv=")
						.Append((container.Inv != null) ? "yes" : "NO")
						.Append("  invId=")
						.Append(InvId(container.Inv))
						.Append("  items=")
						.Append(InvCount(container.Inv))
						.Append("  on=")
						.Append(cfgEntry.Enabled)
						.Append("  wl=[")
						.Append(cfgEntry.White)
						.Append("]")
						.Append("  bl=[")
						.Append(cfgEntry.Black)
						.Append("]")
						.Append("  tags=[")
						.Append(U.Join(new List<string>(hashSet).ToArray()))
						.Append("]");
				}
				List<GameItem> list2 = World.SourceItems(out var report);
				stringBuilder.Append("\n  来源物品=").Append(list2.Count).Append(report);
				stringBuilder.Append("\n  tag 统计 = ").Append(Tags.StatLine());
				for (int j = 0; j < list2.Count && j < 40; j++)
				{
					GameItem val = list2[j];
					HashSet<string> hashSet2 = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					Tags.Collect(val, hashSet2);
					int value2 = 0;
					try
					{
						value2 = val.unitCount;
					}
					catch
					{
					}
					stringBuilder.Append("\n    ").Append(U.SafeId(val)).Append("  x")
						.Append(value2)
						.Append(Owned.Is(val) ? "  拥有" : "  未拥有")
						.Append("  tags=[")
						.Append(U.Join(new List<string>(hashSet2).ToArray()))
						.Append("]");
				}
				Log.Msg(stringBuilder.ToString());
			}
			catch (Exception ex2)
			{
				Log.Error("[AutoStorage] Dump: " + ex2);
			}
		}

		private static string InvId(GameInventory inv)
		{
			if (inv == null)
			{
				return "-";
			}
			try
			{
				return inv.identifier ?? "-";
			}
			catch
			{
				return "-";
			}
		}

		private static int InvCount(GameInventory inv)
		{
			if (inv == null)
			{
				return -1;
			}
			try
			{
				return (inv.childItems != null) ? inv.childItems.Count : (-1);
			}
			catch
			{
				return -1;
			}
		}
	}
	internal static class Hotkeys
	{
		public static MelonPreferences_Entry<string> Sort;

		public static MelonPreferences_Entry<string> Overview;

		public static MelonPreferences_Entry<string> Toggle;

		public static MelonPreferences_Entry<string> Diagnostic;

		public static MelonPreferences_Entry<string> Scan;

		public static void Init()
		{
			MelonPreferences_Category val = MelonPreferences.CreateCategory("AutoStorage", "AutoStorage");
			Sort = val.CreateEntry<string>("SortHotkey", "Ctrl+R", "Sort items / 整理物品", (string)null, false, false, (ValueValidator)null, (string)null);
			Overview = val.CreateEntry<string>("OverviewHotkey", "Ctrl+L", "Container overview / 容器总览", (string)null, false, false, (ValueValidator)null, (string)null);
			Toggle = val.CreateEntry<string>("ToggleHotkey", "Ctrl+Shift+O", "Toggle AutoStorage / 全局开关", (string)null, false, false, (ValueValidator)null, (string)null);
			val.SaveToFile(false);
		}

		public static bool Parse(string raw, out KeyCode key, out bool ctrl, out bool alt, out bool shift)
		{
			//IL_01eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0200: Unknown result type (might be due to invalid IL or missing references)
			//IL_0203: Expected I4, but got Unknown
			key = (KeyCode)0;
			ctrl = (alt = (shift = false));
			if (string.IsNullOrWhiteSpace(raw) || raw.Equals("None", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			string[] array = raw.Split('+');
			for (int i = 0; i < array.Length; i++)
			{
				string text = array[i].Trim();
				switch (text.ToLowerInvariant())
				{
				case "ctrl":
				case "control":
				case "leftcontrol":
				case "rightcontrol":
					ctrl = true;
					continue;
				case "alt":
				case "leftalt":
				case "rightalt":
					alt = true;
					continue;
				case "shift":
				case "leftshift":
				case "rightshift":
					shift = true;
					continue;
				}
				if ((int)key != 0 || int.TryParse(text, out var _) || !Enum.TryParse<KeyCode>((text == "`") ? "BackQuote" : text, true, out KeyCode result2) || !Enum.IsDefined(typeof(KeyCode), result2) || (int)result2 == 0)
				{
					return false;
				}
				key = (KeyCode)(int)result2;
			}
			return (int)key != 0;
		}

		public static bool Down(MelonPreferences_Entry<string> entry)
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if (entry == null || !Parse(entry.Value, out var key, out var ctrl, out var alt, out var shift) || (int)key == 0)
			{
				return false;
			}
			if (Input.GetKeyDown(key) && ctrl == (Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305)) && alt == (Input.GetKey((KeyCode)308) || Input.GetKey((KeyCode)307)))
			{
				return shift == (Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303));
			}
			return false;
		}

		public static string Hints(string text)
		{
			return Regex.Replace(text, "Ctrl\\+Shift\\+[DTO]|Ctrl\\+[RL]", (Match match) =>
			{
				MelonPreferences_Entry<string> val = null;
				switch (match.Value)
				{
				case "Ctrl+R":
					val = Sort;
					break;
				case "Ctrl+L":
					val = Overview;
					break;
				case "Ctrl+Shift+O":
					val = Toggle;
					break;
				case "Ctrl+Shift+D":
					val = Diagnostic;
					break;
				case "Ctrl+Shift+T":
					val = Scan;
					break;
				}
				string result = ((val != null) ? ((!string.IsNullOrWhiteSpace(val.Value)) ? val.Value : "None") : match.Value);
				return result;
			});
		}
	}
	internal static class L
	{
		private static string _state;

		private static float _nextCheck;

		private static readonly Dictionary<string, Action> _windows = new Dictionary<string, Action>();

		public static bool Chinese
		{
			get
			{
				try
				{
					return IsChinese(LocHelper.GetCurrentLocaleCode());
				}
				catch
				{
					return false;
				}
			}
		}

		public static void Track(string id, Action refresh)
		{
			_windows[id] = refresh;
		}

		public static void CloseTrackedWindows()
		{
			foreach (string key in _windows.Keys)
			{
				try
				{
					if ((Object)(object)CustomUIManager.Instance != (Object)null)
					{
						CustomUIManager.Instance.CloseWindow(key);
					}
				}
				catch
				{
				}
			}
			_windows.Clear();
			MenuButton.ClosePanel();
		}

		public static void Tick()
		{
			if (Time.realtimeSinceStartup < _nextCheck)
			{
				return;
			}
			_nextCheck = Time.realtimeSinceStartup + 0.5f;
			string text;
			try
			{
				text = LocHelper.GetCurrentLocaleCode() ?? "";
			}
			catch
			{
				return;
			}
			string text2 = text + "|" + Hotkeys.Hints("Ctrl+R|Ctrl+L|Ctrl+Shift+O");
			if (text2 == _state)
			{
				return;
			}
			_state = text2;
			Tags.ClearLabelCache();
			foreach (KeyValuePair<string, Action> item in new Dictionary<string, Action>(_windows))
			{
				try
				{
					if ((Object)(object)CustomUIManager.Instance != (Object)null && CustomUIManager.Instance.IsOpen(item.Key))
					{
						item.Value();
					}
					else
					{
						_windows.Remove(item.Key);
					}
				}
				catch (Exception ex)
				{
					Core.Log.Warning("[AutoStorage] Refresh UI: " + ex.Message);
				}
			}
		}

		public static bool IsChinese(string code)
		{
			if (code != null)
			{
				if (!code.Equals("zh", StringComparison.OrdinalIgnoreCase) && !code.StartsWith("zh-", StringComparison.OrdinalIgnoreCase))
				{
					return code.StartsWith("zh_", StringComparison.OrdinalIgnoreCase);
				}
				return true;
			}
			return false;
		}

		public static string T(string zh, string en)
		{
			return Hotkeys.Hints(Chinese ? zh : en);
		}

		public static string Slot(string label)
		{
			if (label == null)
			{
				return label;
			}
			switch (label.Trim().ToLowerInvariant())
			{
			case "input":
			case "输入":
			case "輸入":
				return T("输入", "Input");
			case "output":
			case "输出":
			case "輸出":
				return T("输出", "Output");
			case "container":
			case "容器":
				return T("容器", "Container");
			case "chill zone":
			case "冰镇区":
				return T("冰镇区", "Chill zone");
			case "freeze zone":
			case "冷冻区":
				return T("冷冻区", "Freeze zone");
			case "储水容器":
				return T("储水容器", "Water storage");
			case "能量电池":
				return T("能量电池", "Energy cells");
			case "chip":
			case "芯片":
				return T("芯片", "Chip");
			default:
				if (!Chinese)
				{
					return label.Replace("未识别槽位", "Unidentified slot");
				}
				return label;
			}
		}
	}
}
