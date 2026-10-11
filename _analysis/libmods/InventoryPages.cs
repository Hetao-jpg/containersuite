using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using InventoryPages;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: MelonInfo(typeof(Core), "InventoryPages", "2.1.0", "maybesad", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
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
namespace InventoryPages
{
	public class Core : MelonMod
	{
		private class Entry
		{
			public GameItem Item;

			public int X;

			public int Y;
		}

		private const int W = 24;

		private const int H = 10;

		private const int CELL_PX = 16;

		private const int MIN_PAGES = 1;

		private const int MAX_PAGES = 3;

		private static Instance _log;

		private static MelonPreferences_Category _cat;

		private static MelonPreferences_Entry<int> _cfgPageCount;

		private static MelonPreferences_Entry<string> _cfgToggleKey;

		private static MelonPreferences_Entry<string> _cfgPage2Ids;

		private static MelonPreferences_Entry<string> _cfgPage3Ids;

		private static readonly List<GameGridInventory> _grids = new List<GameGridInventory>();

		private static readonly List<int> _gridPage = new List<int>();

		private static readonly List<HashSet<int>> _pageIds = new List<HashSet<int>>();

		private static int _pageCount = 2;

		private static int _currentPage = 0;

		private static bool _busy = false;

		private static bool _merged = false;

		private static KeyCode _toggleKey = (KeyCode)9;

		private static int _cfgPageCountLastSeen = -1;

		private static bool _prefsDirty = false;

		private float _poll = 0f;

		public override void OnInitializeMelon()
		{
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Expected Obj, but got Unknown
			//IL_0206: Unknown result type (might be due to invalid IL or missing references)
			//IL_0118: Unknown result type (might be due to invalid IL or missing references)
			//IL_012d: Unknown result type (might be due to invalid IL or missing references)
			//IL_013a: Expected Obj, but got Unknown
			//IL_013a: Expected Obj, but got Unknown
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_0193: Expected Obj, but got Unknown
			_log = ((MelonBase)this).LoggerInstance;
			_cat = MelonPreferences.CreateCategory("InventoryPages");
			_cfgPageCount = _cat.CreateEntry<int>("PageCount", 2, "库存页数（1-3）", "总页数，范围 1-3，超出会被钳制。修改后建议重启游戏。", false, false, (ValueValidator)null, (string)null);
			_cfgToggleKey = _cat.CreateEntry<string>("ToggleKey", "Tab", "翻页按键", "Unity KeyCode 名称，例如 Tab、F1、PageDown、BackQuote。", false, false, (ValueValidator)null, (string)null);
			_cfgPage2Ids = _cat.CreateEntry<string>("Page2Ids", "", "第二页物品ID", "自动生成，请勿手动修改。", false, false, (ValueValidator)null, (string)null);
			_cfgPage3Ids = _cat.CreateEntry<string>("Page3Ids", "", "第三页物品ID", "自动生成，请勿手动修改。", false, false, (ValueValidator)null, (string)null);
			ApplyConfig();
			_cfgPageCount.Value = _pageCount;
			InitPageIdLists();
			_cfgPageCountLastSeen = _pageCount;
			try
			{
				Harmony val = new Harmony("maybesad.InventoryPages");
				MethodInfo method = typeof(PlayerStore).GetMethod("SaveGame");
				if (method != null)
				{
					val.Patch((MethodBase)method, new HarmonyMethod(typeof(Core), "BeforeSave", (Type[])null), new HarmonyMethod(typeof(Core), "AfterSave", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
				else
				{
					_log.Warning("PlayerStore.SaveGame 未找到，存档合并已禁用。");
				}
				MethodInfo method2 = typeof(PlayerStore).GetMethod("LoadGame");
				if (method2 != null)
				{
					val.Patch((MethodBase)method2, (HarmonyMethod)null, new HarmonyMethod(typeof(Core), "AfterLoad", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				}
				else
				{
					_log.Warning("PlayerStore.LoadGame 未找到，读档拆分已禁用。");
				}
			}
			catch (Exception ex)
			{
				_log.Warning("Harmony 补丁失败: " + ex.Message);
			}
			_log.Msg($"loaded (pages={_pageCount}, key={_toggleKey})");
		}

		private static void ApplyConfig()
		{
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			int value = _cfgPageCount.Value;
			_pageCount = Math.Clamp(value, 1, 3);
			if (value != _pageCount)
			{
				_log.Warning($"PageCount={value} 超出范围 [{1},{3}]，已钳制为 {_pageCount}。");
			}
			if (Enum.TryParse<KeyCode>(_cfgToggleKey.Value, true, out KeyCode result))
			{
				_toggleKey = result;
				return;
			}
			_log.Warning("无效的 ToggleKey '" + _cfgToggleKey.Value + "'，回退到 Tab。");
			_toggleKey = (KeyCode)9;
		}

		private static void InitPageIdLists()
		{
			_pageIds.Clear();
			for (int i = 0; i < _pageCount; i++)
			{
				_pageIds.Add(new HashSet<int>());
			}
		}

		public override void OnUpdate()
		{
			//IL_00ec: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				int num = Math.Clamp(_cfgPageCount.Value, 1, 3);
				if (num != _cfgPageCountLastSeen)
				{
					_log.Msg($"PageCount 变化: {_cfgPageCountLastSeen} -> {num}，重建库存");
					_cfgPageCountLastSeen = num;
					_cfgPageCount.Value = num;
					RebuildForNewPageCount(num);
					return;
				}
				_poll += Time.deltaTime;
				if (_poll >= 1f)
				{
					_poll = 0f;
					TryBind();
					if (_prefsDirty)
					{
						MelonPreferences.Save();
						_prefsDirty = false;
					}
				}
				if (!Input.GetKeyDown(_toggleKey))
				{
					return;
				}
				if (!IsBound())
				{
					TryBind();
					if (!IsBound())
					{
						return;
					}
				}
				Flip();
			}
			catch (Exception ex)
			{
				_log.Warning("OnUpdate: " + ex.Message);
			}
		}

		private static bool IsBound()
		{
			if (_grids.Count != _pageCount)
			{
				return false;
			}
			for (int i = 0; i < _grids.Count; i++)
			{
				if (_grids[i] == null)
				{
					return false;
				}
			}
			return true;
		}

		private static void RebuildForNewPageCount(int newCount)
		{
			//IL_01ed: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f4: Expected Obj, but got Unknown
			try
			{
				Dictionary<int, int> dictionary = new Dictionary<int, int>();
				for (int i = 0; i < _grids.Count; i++)
				{
					int num = _gridPage[i];
					if (num <= 0)
					{
						continue;
					}
					foreach (Entry item in Snapshot(_grids[i]))
					{
						int num2 = Uid(item.Item);
						if (num2 != 0)
						{
							dictionary[num2] = num;
						}
					}
				}
				GameGridInventory val = ((_grids.Count > 0) ? _grids[0] : null);
				List<Entry> list = CollectAll();
				ClearAll();
				if (val != null)
				{
					foreach (Entry item2 in list)
					{
						Place(val, item2.Item, item2.X, item2.Y);
					}
				}
				_pageCount = newCount;
				InitPageIdLists();
				foreach (KeyValuePair<int, int> item3 in dictionary)
				{
					if (item3.Value > 0 && item3.Value < _pageCount)
					{
						_pageIds[item3.Value].Add(item3.Key);
					}
				}
				SaveIdSets();
				_grids.Clear();
				_gridPage.Clear();
				if (val != null)
				{
					_grids.Add(val);
					_gridPage.Add(0);
					for (int j = 1; j < _pageCount; j++)
					{
						GameGridInventory val2 = new GameGridInventory(24, 10);
						try
						{
							val2.SetShape(24, 10);
						}
						catch
						{
						}
						_grids.Add(val2);
						_gridPage.Add(j);
					}
				}
				_currentPage = 0;
				_merged = false;
				_busy = false;
				SplitByIds();
				_log.Msg($"重建完成: grids={_grids.Count}, pages={_pageCount}");
			}
			catch (Exception ex)
			{
				_log.Warning("RebuildForNewPageCount: " + ex.Message);
			}
		}

		private static void TryBind()
		{
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance == (Object)null)
				{
					return;
				}
				GameGridInventory invElement = instance.invElement;
				if (invElement == null)
				{
					return;
				}
				bool flag = !IsBound();
				if (!flag)
				{
					try
					{
						_ = _grids[0].handler;
					}
					catch
					{
						flag = true;
					}
				}
				if (flag)
				{
					Rebind(invElement);
					_log.Msg($"bound (grids={_grids.Count}, pages={_pageCount})");
				}
			}
			catch (Exception ex)
			{
				_log.Warning("TryBind: " + ex.Message);
			}
		}

		private static void Rebind(GameGridInventory mainGrid)
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Expected Obj, but got Unknown
			_grids.Clear();
			_gridPage.Clear();
			_grids.Add(mainGrid);
			_gridPage.Add(0);
			for (int i = 1; i < _pageCount; i++)
			{
				GameGridInventory val = new GameGridInventory(24, 10);
				try
				{
					val.SetShape(24, 10);
				}
				catch
				{
				}
				_grids.Add(val);
				_gridPage.Add(i);
			}
			try
			{
				mainGrid.SetShape(24, 10);
			}
			catch
			{
			}
			try
			{
				mainGrid.ResizePixels(384, 160);
			}
			catch
			{
			}
			_currentPage = 0;
			_merged = false;
			_busy = false;
			LoadIdSets();
			SplitByIds();
		}

		private static void Flip()
		{
			if (_busy || _merged || _pageCount < 2)
			{
				return;
			}
			_busy = true;
			try
			{
				int num = (_currentPage + 1) % _pageCount;
				int num2 = _gridPage.IndexOf(num);
				if (num2 >= 0)
				{
					Swap(_grids[0], _grids[num2]);
					int value = _gridPage[0];
					_gridPage[0] = _gridPage[num2];
					_gridPage[num2] = value;
					_currentPage = num;
					RefreshPageIds();
					_log.Msg($"page {_currentPage + 1}/{_pageCount}");
				}
			}
			catch (Exception ex)
			{
				_log.Warning("Flip: " + ex.Message);
			}
			finally
			{
				_busy = false;
			}
		}

		private static void RefreshPageIds()
		{
			for (int i = 0; i < _pageIds.Count; i++)
			{
				_pageIds[i].Clear();
			}
			for (int j = 0; j < _grids.Count; j++)
			{
				int num = _gridPage[j];
				if (num <= 0 || num >= _pageIds.Count)
				{
					continue;
				}
				foreach (Entry item in Snapshot(_grids[j]))
				{
					int num2 = Uid(item.Item);
					if (num2 != 0)
					{
						_pageIds[num].Add(num2);
					}
				}
			}
			SaveIdSets();
		}

		public static void AfterLoad()
		{
			try
			{
				_grids.Clear();
				_gridPage.Clear();
				_currentPage = 0;
				_busy = false;
				_merged = false;
				LoadIdSets();
			}
			catch (Exception ex)
			{
				Instance log = _log;
				if (log != null)
				{
					log.Warning("AfterLoad: " + ex.Message);
				}
			}
		}

		public static void BeforeSave()
		{
			try
			{
				MergeForSave();
			}
			catch (Exception ex)
			{
				Instance log = _log;
				if (log != null)
				{
					log.Warning("BeforeSave: " + ex.Message);
				}
			}
		}

		public static void AfterSave()
		{
			try
			{
				if (_merged)
				{
					SplitAfterSave();
				}
			}
			catch (Exception ex)
			{
				Instance log = _log;
				if (log != null)
				{
					log.Warning("AfterSave: " + ex.Message);
				}
			}
		}

		private static void MergeForSave()
		{
			if (_grids.Count == 0)
			{
				return;
			}
			RefreshPageIds();
			List<Entry> list = CollectAll();
			ClearAll();
			List<Entry> list2 = new List<Entry>();
			foreach (Entry item in list)
			{
				if (FindPage(Uid(item.Item)) == 0)
				{
					Place(_grids[0], item.Item, item.X, item.Y);
				}
				else
				{
					list2.Add(item);
				}
			}
			foreach (Entry item2 in list2)
			{
				Place(_grids[0], item2.Item, item2.X, item2.Y);
			}
			ResetShapes();
			_merged = true;
		}

		private static void SplitAfterSave()
		{
			_currentPage = 0;
			for (int i = 0; i < _gridPage.Count; i++)
			{
				_gridPage[i] = i;
			}
			SplitByIds();
			_merged = false;
			ResetShapes();
		}

		private static void SplitByIds()
		{
			if (_grids.Count == 0)
			{
				return;
			}
			List<Entry> list = CollectAll();
			ClearAll();
			foreach (Entry item in list)
			{
				int num = FindPage(Uid(item.Item));
				int index = 0;
				for (int i = 0; i < _gridPage.Count; i++)
				{
					if (_gridPage[i] == num)
					{
						index = i;
						break;
					}
				}
				Place(_grids[index], item.Item, item.X, item.Y);
			}
			_currentPage = 0;
		}

		private static List<Entry> CollectAll()
		{
			List<Entry> list = new List<Entry>();
			for (int i = 0; i < _grids.Count; i++)
			{
				list.AddRange(Snapshot(_grids[i]));
			}
			return list;
		}

		private static void ClearAll()
		{
			for (int i = 0; i < _grids.Count; i++)
			{
				ClearGrid(_grids[i]);
			}
		}

		private static void ClearGrid(GameGridInventory grid)
		{
			if (grid == null)
			{
				return;
			}
			foreach (Entry item in Snapshot(grid))
			{
				try
				{
					((GameInventory)grid).Expel(item.Item);
				}
				catch
				{
				}
			}
		}

		private static void ResetShapes()
		{
			for (int i = 0; i < _grids.Count; i++)
			{
				try
				{
					_grids[i].SetShape(24, 10);
				}
				catch
				{
				}
			}
			if (_grids.Count > 0)
			{
				try
				{
					_grids[0].ResizePixels(384, 160);
				}
				catch
				{
				}
			}
		}

		private static int FindPage(int uid)
		{
			if (uid == 0)
			{
				return 0;
			}
			for (int i = 1; i < _pageIds.Count; i++)
			{
				if (_pageIds[i].Contains(uid))
				{
					return i;
				}
			}
			return 0;
		}

		private static void Swap(GameGridInventory a, GameGridInventory b)
		{
			List<Entry> list = Snapshot(a);
			List<Entry> list2 = Snapshot(b);
			ClearGrid(a);
			ClearGrid(b);
			foreach (Entry item in list2)
			{
				Place(a, item.Item, item.X, item.Y);
			}
			foreach (Entry item2 in list)
			{
				Place(b, item2.Item, item2.X, item2.Y);
			}
		}

		private static int Uid(GameItem item)
		{
			if (item == null)
			{
				return 0;
			}
			try
			{
				return item.uniqueId;
			}
			catch
			{
				return 0;
			}
		}

		private static List<Entry> Snapshot(GameGridInventory grid)
		{
			List<Entry> list = new List<Entry>();
			if (grid == null)
			{
				return list;
			}
			try
			{
				List<GameItem> childItems = ((GameInventory)grid).childItems;
				if (childItems == null)
				{
					return list;
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
					}
					if (val != null)
					{
						int x = 0;
						int y = 0;
						try
						{
							x = val.modifiedXOrigin;
						}
						catch
						{
						}
						try
						{
							y = val.modifiedYOrigin;
						}
						catch
						{
						}
						list.Add(new Entry
						{
							Item = val,
							X = x,
							Y = y
						});
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static void Place(GameGridInventory grid, GameItem item, int x, int y)
		{
			if (grid == null || item == null)
			{
				return;
			}
			try
			{
				try
				{
					item.modifiedXOrigin = x;
				}
				catch
				{
				}
				try
				{
					item.modifiedYOrigin = y;
				}
				catch
				{
				}
				((GameInventory)grid).UncheckedAccept(item);
			}
			catch
			{
			}
		}

		private static void SaveIdSets()
		{
			if (_pageIds.Count > 1)
			{
				_cfgPage2Ids.Value = FormatIds(_pageIds[1]);
			}
			if (_pageCount >= 3 && _pageIds.Count > 2)
			{
				_cfgPage3Ids.Value = FormatIds(_pageIds[2]);
			}
			_prefsDirty = true;
		}

		private static void LoadIdSets()
		{
			for (int i = 0; i < _pageIds.Count; i++)
			{
				_pageIds[i].Clear();
			}
			if (_pageIds.Count > 1)
			{
				LoadIdSetInto(_cfgPage2Ids.Value, _pageIds[1]);
			}
			if (_pageIds.Count > 2)
			{
				LoadIdSetInto(_cfgPage3Ids.Value, _pageIds[2]);
			}
		}

		private static void LoadIdSetInto(string raw, HashSet<int> target)
		{
			if (string.IsNullOrEmpty(raw))
			{
				return;
			}
			string[] array = raw.Split(',');
			foreach (string text in array)
			{
				if (int.TryParse(text.Trim(), out var result) && result != 0)
				{
					target.Add(result);
				}
			}
		}

		private static string FormatIds(HashSet<int> set)
		{
			if (set == null || set.Count == 0)
			{
				return "";
			}
			StringBuilder stringBuilder = new StringBuilder();
			foreach (int item in set)
			{
				if (stringBuilder.Length > 0)
				{
					stringBuilder.Append(',');
				}
				stringBuilder.Append(item);
			}
			return stringBuilder.ToString();
		}

		static Core()
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
		}
	}
}
