using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using RealPrinter;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(RealPrinterMod), "RealPrinter", "1.4.12", "hetao", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: MelonColor(76, 175, 160, 255)]
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
namespace RealPrinter
{
	public sealed class RealPrinterMod : MelonMod
	{
		private sealed class ConditionInfo
		{
			public string Id;

			public string Display;

			public string Category;

			public Func<ItemCondition> Make;
		}

		public sealed class Cfg
		{
			public MelonPreferences_Entry<int> FeatureCostEntry;

			public MelonPreferences_Entry<bool> VerboseLogEntry;

			public MelonPreferences_Entry<bool> SupplierStockEntry;

			public MelonPreferences_Entry<bool> EnableF6GiveEntry;

			public MelonPreferences_Entry<int> DealerPriceEntry;

			public MelonPreferences_Entry<int> GiveMachineHotkeyEntry;
		}

		public static class Config
		{
			public const int BaseMachineValue = 50;

			public static int FeatureCost = 20;

			public static bool SupplierStock = true;

			public static bool EnableF6Give = false;

			public const int DealerPrice = 100;

			public static bool VerboseLog = false;
		}

		public static class RLog
		{
			private static readonly string TAG = "[真印] ";

			private static int _swallowedCount;

			private const int SwallowReportEvery = 100;

			internal static int SwallowedCount => _swallowedCount;

			internal static void Detail(string m)
			{
				if (Config.VerboseLog)
				{
					MelonLogger.Msg(TAG + m);
				}
			}

			internal static void Swallowed(string where, Exception e)
			{
				if (e != null)
				{
					_swallowedCount++;
					string name = e.GetType().Name;
					if (name.IndexOf("AccessViolation", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						MelonLogger.Error(TAG + "★★ " + where + " 捕获到 AccessViolation —— MEMORY 记录它不可捕获，说明前提变了，必须查！ 累计吞掉 " + _swallowedCount + " 次");
						_swallowedCount = 0;
					}
					else if (_swallowedCount == 1 || _swallowedCount % 100 == 0)
					{
						MelonLogger.Warning(TAG + "降级：" + where + " — " + name + ": " + e.Message + ((_swallowedCount > 1) ? ("（累计 " + _swallowedCount + " 次）") : ""));
					}
				}
			}

			public static void Msg(string m)
			{
				MelonLogger.Msg(TAG + m);
			}

			public static void Warning(string m)
			{
				MelonLogger.Warning(TAG + m);
			}

			public static void Error(string m)
			{
				MelonLogger.Error(TAG + m);
			}
		}

		public static class Pay
		{
			public static int Query()
			{
				try
				{
					PlayerStore instance = PlayerStore.Instance;
					return (instance != null) ? instance.GetCash() : 0;
				}
				catch
				{
					return 0;
				}
			}

			public static bool TryCharge(int amount, out string failReason)
			{
				failReason = null;
				if (amount <= 0)
				{
					return true;
				}
				try
				{
					PlayerStore instance = PlayerStore.Instance;
					if (instance == null)
					{
						failReason = "PlayerStore 不可用（不在存档内？）";
						return false;
					}
					int cash = instance.GetCash();
					if (cash < amount)
					{
						failReason = "信用点不足：需要 " + amount + "，只有 " + cash;
						return false;
					}
					instance.ModCash(-amount, false);
					int cash2 = instance.GetCash();
					if (cash2 != cash - amount)
					{
						try
						{
							instance.ModCash(amount, false);
						}
						catch (Exception e)
						{
							RLog.Swallowed("TryCharge", e);
						}
						failReason = "扣费未生效（期望 " + (cash - amount) + "，实际 " + cash2 + "），已退回";
						RLog.Error("? 扣费异常已回滚：" + failReason);
						return false;
					}
					return true;
				}
				catch (Exception ex)
				{
					failReason = "扣费异常：" + ex.Message;
					return false;
				}
			}
		}

		public static class License
		{
			private static readonly Dictionary<string, Func<List<ItemCondition>>> _catListAccessors = new Dictionary<string, Func<List<ItemCondition>>>(StringComparer.OrdinalIgnoreCase)
			{
				{
					"香烟",
					() => ItemConditionList.conditionGenuineCigarette
				},
				{
					"化学品",
					() => ItemConditionList.conditionChemicalPurity
				},
				{
					"金属锭",
					() => ItemConditionList.conditionIngotPurity
				},
				{
					"注射器",
					() => ItemConditionList.conditionGenuineInjector
				}
			};

			private static readonly Dictionary<string, string> _catRealNames = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			private static readonly Dictionary<string, string> _catFallback = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "香烟", "CATEGORY_GENUINE_CIGARETTE" },
				{ "化学品", "CATEGORY_CHEMICAL_PURITY" },
				{ "金属锭", "CATEGORY_INGOT_PURITY" },
				{ "注射器", "CATEGORY_GENUINE_INJECTOR" }
			};

			private static bool _catResolved;

			private static readonly Dictionary<string, string> _labelByCat = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
			{
				{ "CATEGORY_GENUINE_STAMP", "印花票" },
				{ "CATEGORY_EXPIRED", "效期" },
				{ "CATEGORY_WATER_PURITY", "水" },
				{ "CATEGORY_WINE_QUALITY", "酒" },
				{ "CATEGORY_EQUIPMENT_CONDITION", "装备成色" },
				{ "CATEGORY_FRESHNESS", "新鲜度" },
				{ "CATEGORY_DENTED", "凹陷" },
				{ "CATEGORY_RUSTED", "生锈" },
				{ "CATEGORY_OPEN_BOX", "已开封" },
				{ "CATEGORY_REPLICA", "仿制品" },
				{ "CATEGORY_FREE", "赠品" },
				{ "CATEGORY_MODULE_STUCK", "模块卡死" }
			};

			private static HashSet<string> _allowed;

			private static void ResolveCategoryNames()
			{
				if (_catResolved)
				{
					return;
				}
				_catResolved = true;
				foreach (KeyValuePair<string, Func<List<ItemCondition>>> catListAccessor in _catListAccessors)
				{
					try
					{
						List<ItemCondition> val = catListAccessor.Value();
						if (val != null && val.Count != 0)
						{
							string value = null;
							try
							{
								value = val[0].category;
							}
							catch (Exception e)
							{
								RLog.Swallowed("ResolveCategoryNames", e);
							}
							if (!string.IsNullOrEmpty(value))
							{
								_catRealNames[catListAccessor.Key] = value;
							}
						}
					}
					catch (Exception e2)
					{
						RLog.Swallowed("ResolveCategoryNames", e2);
					}
				}
				if (_catRealNames.Count == 0)
				{
					foreach (KeyValuePair<string, string> item in _catFallback)
					{
						_catRealNames[item.Key] = item.Value;
					}
					RLog.Warning("? 许可类别名读取失败，已回退到**手写字符串**（若机器开不了窗，多半是这个原因）。");
					return;
				}
				foreach (KeyValuePair<string, string> catRealName in _catRealNames)
				{
					RLog.Detail("许可类别 " + catRealName.Key + " → 游戏真实 category = " + catRealName.Value);
				}
			}

			private static HashSet<string> BuildAllowedSet()
			{
				ResolveCategoryNames();
				HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				foreach (string value in _catRealNames.Values)
				{
					hashSet.Add(value);
				}
				return hashSet;
			}

			internal static List<string> SelfCheckLicense()
			{
				List<string> list = new List<string>();
				string[] array = AllAllowed();
				foreach (string text in array)
				{
					string[] array2 = OfficialIdsForCategory(text);
					if (array2 == null || array2.Length == 0)
					{
						list.Add("许可类别 " + text + " 在游戏官方档位目录里没有对应档位 → 这类商品开窗会是空的。");
					}
				}
				return list;
			}

			public static string CategoryLabel(string cat)
			{
				if (string.IsNullOrEmpty(cat))
				{
					return "未知类别";
				}
				foreach (KeyValuePair<string, string> catRealName in _catRealNames)
				{
					if (string.Equals(catRealName.Value, cat, StringComparison.OrdinalIgnoreCase))
					{
						return catRealName.Key;
					}
				}
				if (!_labelByCat.TryGetValue(cat, out var value))
				{
					return cat;
				}
				return value;
			}

			public static bool IsAllowed(string cat)
			{
				if (string.IsNullOrEmpty(cat))
				{
					return false;
				}
				if (_allowed == null)
				{
					_allowed = BuildAllowedSet();
				}
				return _allowed.Contains(cat);
			}

			public static string[] AllAllowed()
			{
				if (_allowed == null)
				{
					_allowed = BuildAllowedSet();
				}
				string[] array = new string[_allowed.Count];
				_allowed.CopyTo(array);
				return array;
			}

			public static string RejectReason(GameItem target)
			{
				List<string> list = new List<string>();
				int num = 0;
				if (!IsSafeTarget(target))
				{
					return "「该商品」无法处理（对象状态不稳定）。请松手后重新点一次。";
				}
				try
				{
					List<ItemFeature> itemFeatures = target.itemFeatures;
					if (itemFeatures != null)
					{
						num = itemFeatures.Count;
						for (int i = 0; i < itemFeatures.Count; i++)
						{
							string text = null;
							try
							{
								text = itemFeatures[i].category;
							}
							catch
							{
								break;
							}
							if (!string.IsNullOrEmpty(text) && !list.Contains(text))
							{
								list.Add(text);
							}
						}
					}
				}
				catch (Exception e)
				{
					RLog.Swallowed("RejectReason", e);
				}
				if (num == 0)
				{
					return "「" + DisplayNameOf(target) + "」身上没有任何可验证的特性（这台打印机只改真值：验真、纯度、可不可用）。";
				}
				if (list.Count == 0)
				{
					return "「" + DisplayNameOf(target) + "」的真值特性无法识别（类别为空），无法处理。";
				}
				List<string> list2 = new List<string>();
				foreach (string item in list)
				{
					list2.Add(CategoryLabel(item));
				}
				return "「" + DisplayNameOf(target) + "」属于「" + string.Join(" / ", list2.ToArray()) + "」，这台打印机不支持。\n★ 只支持：香烟 / 化学品 / 金属锭 / 注射器。";
			}
		}

		private sealed class EditSession
		{
			public GameItem Tool;

			public GameItem Target;

			public int Cash;

			public int RowSeq;

			public string ExpandedFeatureId;
		}

		private enum EnterVerdict
		{
			Open,
			RejectWithReason,
			RejectSilent
		}

		private sealed class EnterDecision
		{
			public EnterVerdict Verdict;

			public List<FeatureRow> Licensed;

			public int RawCount;

			public string Reason;
		}

		internal sealed class FeatureRow
		{
			public ItemFeature Feature;

			public string Id;

			public string Category;

			public string RealId;

			public string FakeId;

			public string Display;

			public string[] Options;

			public bool CanEdit;
		}

		[HarmonyPatch(typeof(GameItem), "MayTarget")]
		private static class Patch_MayTarget
		{
			[HarmonyPriority(990)]
			private static bool Prefix(GameItem __instance, GameItem targetItem, ref bool __result)
			{
				if (!TryClaim(__instance, targetItem))
				{
					return true;
				}
				__result = true;
				HotLog.Log("MayTarget", __instance, targetItem);
				return false;
			}
		}

		[HarmonyPatch(typeof(GameItem), "CanTarget")]
		private static class Patch_CanTarget
		{
			[HarmonyPriority(990)]
			private static bool Prefix(GameItem __instance, GameItem targetItem, ref bool __result)
			{
				if (!TryClaim(__instance, targetItem))
				{
					return true;
				}
				__result = true;
				HotLog.Log("CanTarget", __instance, targetItem);
				return false;
			}
		}

		[HarmonyPatch(typeof(GameItem), "Target")]
		private static class Patch_Target
		{
			[HarmonyPriority(990)]
			private static bool Prefix(GameItem __instance, GameItem targetItem)
			{
				if (!TryClaim(__instance, targetItem))
				{
					return true;
				}
				if (Config.VerboseLog)
				{
					RLog.Detail("交互被触发：self=" + SafeId(__instance) + " → target=" + SafeId(targetItem));
				}
				OnTargetItemFunc(targetItem);
				return false;
			}
		}

		private static class HotLog
		{
			private sealed class Slot
			{
				public double LastAt;

				public int Skipped;
			}

			private static readonly Dictionary<string, Slot> _slots = new Dictionary<string, Slot>();

			private static readonly DateTime _epoch = DateTime.UtcNow;

			internal static void Log(string kind, GameItem self, GameItem target)
			{
				if (!Config.VerboseLog)
				{
					return;
				}
				try
				{
					lock (_slots)
					{
						double totalSeconds = (DateTime.UtcNow - _epoch).TotalSeconds;
						if (!_slots.TryGetValue(kind, out var value))
						{
							value = new Slot
							{
								LastAt = -999.0
							};
							_slots[kind] = value;
						}
						if (totalSeconds - value.LastAt < 1.0)
						{
							value.Skipped++;
							return;
						}
						int skipped = value.Skipped;
						value.Skipped = 0;
						value.LastAt = totalSeconds;
						RLog.Detail(kind + " 接管：self=" + SafeId(self) + " → target=" + SafeId(target) + ((skipped > 0) ? ("\u3000（另有 " + skipped + " 次同类调用已省略）") : ""));
					}
				}
				catch (Exception e)
				{
					RLog.Swallowed("Log", e);
				}
			}
		}

		[HarmonyPatch(typeof(ModItemDirectory), "InitDirectory")]
		internal static class Patch_ModItemDirectoryInit
		{
			private static void Postfix(ItemDirectory __instance)
			{
				TryRegister(__instance);
			}
		}

		private static Func<ItemCondition>[] _allFactories;

		private static Dictionary<string, ConditionInfo> _byId;

		private static Dictionary<string, List<ConditionInfo>> _byCat;

		private static bool _catalogTried;

		private static int _catalogFail;

		private static readonly ConcurrentDictionary<string, string[]> _idArrayCache = new ConcurrentDictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);

		private static readonly ConcurrentDictionary<string, string> _displayCache = new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase);

		private static Cfg _cfg = new Cfg();

		public const string MachineIdentifier = "real_printer";

		public const string OurTag = "REAL_PRINTER_TAG";

		public const string ToolTagLabeler = "LABELER_TAG";

		public const string ImportantTag = "IMPORTANT_TAG";

		public const string MagnifierTag = "MAGNIFYING_GLASS";

		public const string TemplateItemId = "labeler";

		public const string ToolAtlasPath = "Items/items_tool";

		public const string SuiteVersion = "1.4.12";

		private const string ColorDefault = "default";

		internal static RealPrinterMod Self;

		private static bool _patched;

		private static EditSession _session;

		internal const string EditorWindowId = "real_printer_editor";

		private const float FontSizeSmall = 11.5f;

		private const int PatchPriority = 990;

		private static readonly (string Name, Type Type)[] KnownTypes = new (string, Type)[1] { ("GameItem", typeof(GameItem)) };

		private static bool _patchesReady;

		private static readonly ConcurrentDictionary<long, byte> _machinePtrs = new ConcurrentDictionary<long, byte>();

		private const int IdOffset = 448;

		private const int StateOffset = 432;

		private static Func<GameItem> _printerFactory;

		private float _lastRepairTime;

		private float _nextHotkeyTime;

		internal const int ShrinkW = 1;

		internal const int ShrinkH = 2;

		internal const float ShrinkWF = 1f;

		internal const float ShrinkHF = 2f;

		internal static int HotkeyGive => ReadHotkey(_cfg?.GiveMachineHotkeyEntry, 287);

		internal static bool TargetSessionActive => _session != null;

		internal static int MachineRegistryCount
		{
			get
			{
				try
				{
					return _machinePtrs.Count;
				}
				catch
				{
					return 0;
				}
			}
		}

		private static Func<ItemCondition>[] AllOfficialFactories()
		{
			if (_allFactories != null)
			{
				return _allFactories;
			}
			_allFactories = new Func<ItemCondition>[56]
			{
				ItemConditionList.CreateLabPure,
				ItemConditionList.CreatePharmaGrade,
				ItemConditionList.CreateNearPharma,
				ItemConditionList.CreateStreetStandard,
				ItemConditionList.CreateCutProduct,
				ItemConditionList.CreateJunkGrade,
				ItemConditionList.CreateStarchGrade,
				ItemConditionList.CreatePerfectQualityIngot,
				ItemConditionList.CreateVeryHighQualityIngot,
				ItemConditionList.CreateHighQualityIngot,
				ItemConditionList.CreateFairQualityIngot,
				ItemConditionList.CreateLowQualityIngot,
				ItemConditionList.CreateVeryLowQualityIngot,
				ItemConditionList.CreateGenuineCigarette,
				ItemConditionList.CreateCounterfeitCigarette,
				ItemConditionList.CreateGenuineStamp,
				ItemConditionList.CreateCounterfeitStamp,
				ItemConditionList.CreateGenuineInjector,
				ItemConditionList.CreateCounterfeitInjector,
				ItemConditionList.CreateExpiredInjector,
				ItemConditionList.CreateUnusableInjector,
				ItemConditionList.CreatePureWater,
				ItemConditionList.CreateHighQualityWater,
				ItemConditionList.CreateBaseWater,
				ItemConditionList.CreateGhostWater,
				ItemConditionList.CreateRustWater,
				ItemConditionList.CreateGutterflow,
				ItemConditionList.CreateMasterVintage,
				ItemConditionList.CreateReserve,
				ItemConditionList.CreateProductionWine,
				ItemConditionList.CreateHomebrew,
				ItemConditionList.CreateBucketBrew,
				ItemConditionList.CreateUndrinkable,
				ItemConditionList.CreateEquipementConditionNew,
				ItemConditionList.CreateEquipementConditionExcellent,
				ItemConditionList.CreateEquipementConditionGood,
				ItemConditionList.CreateEquipementConditionFair,
				ItemConditionList.CreateEquipementConditionPoor,
				ItemConditionList.CreateFoodVeryFresh,
				ItemConditionList.CreateFoodFresh,
				ItemConditionList.CreateFoodStale,
				ItemConditionList.CreateFoodSpoiled,
				ItemConditionList.CreateExpired,
				ItemConditionList.CreateNonExpired,
				ItemConditionList.CreateDented,
				ItemConditionList.CreateNonDented,
				ItemConditionList.CreateRusted,
				ItemConditionList.CreateRustFree,
				ItemConditionList.CreateOpenBox,
				ItemConditionList.CreateFactorySealed,
				ItemConditionList.CreateReplica,
				ItemConditionList.CreateNonReplica,
				ItemConditionList.CreateModuleStuck,
				ItemConditionList.CreateModuleNormal,
				ItemConditionList.CreateFreeNormal,
				ItemConditionList.CreateFreeFree
			};
			return _allFactories;
		}

		private static void AllowCatalogRetry()
		{
			_catalogFail++;
			if (_catalogFail < 3)
			{
				_catalogTried = false;
			}
		}

		internal static void BuildConditionCatalogOnce()
		{
			EnsureCatalog();
		}

		internal static int ConditionCatalogCount()
		{
			EnsureCatalog();
			if (_byId != null)
			{
				return _byId.Count;
			}
			return 0;
		}

		internal static List<string> SelfCheckConditionCatalog()
		{
			List<string> list = new List<string>();
			EnsureCatalog();
			if (_byId == null || _byId.Count == 0)
			{
				list.Add("官方档位目录为空 —— 56 个 Create* 全失败，真值改写不可用。");
				return list;
			}
			foreach (KeyValuePair<string, ConditionInfo> item in _byId)
			{
				ConditionInfo value = item.Value;
				if (string.IsNullOrEmpty(value.Display))
				{
					list.Add("档位 " + value.Id + " 的 display 为空（写进去界面会空白）。");
				}
			}
			foreach (KeyValuePair<string, List<ConditionInfo>> item2 in _byCat)
			{
				foreach (ConditionInfo item3 in item2.Value)
				{
					ItemCondition probe = ResolveOfficialCondition(item3.Id);
					if (probe == null)
					{
						list.Add("★ category " + item2.Key + " 的候选「" + item3.Id + "」解析不出官方对象 → 玩家能选但写不进去 = 静默失效。");
					}
					else if (!string.Equals(SafeStr(() => probe.identifier), item3.Id, StringComparison.OrdinalIgnoreCase))
					{
						list.Add("★ 档位 " + item3.Id + " 工厂返回的 identifier 是「" + SafeStr(() => probe.identifier) + "」—— 键名与对象不一致。");
					}
				}
			}
			return list;
		}

		private static void EnsureCatalog()
		{
			if (_catalogTried)
			{
				return;
			}
			_catalogTried = true;
			try
			{
				_byId = new Dictionary<string, ConditionInfo>(StringComparer.OrdinalIgnoreCase);
				_byCat = new Dictionary<string, List<ConditionInfo>>(StringComparer.OrdinalIgnoreCase);
				Func<ItemCondition>[] array;
				try
				{
					array = AllOfficialFactories();
				}
				catch (Exception ex)
				{
					MelonLogger.Error("[真印] ★ 官方工厂清单构建失败（真值改写降级，其余功能正常）: " + ex);
					_byId = null;
					_byCat = null;
					AllowCatalogRetry();
					return;
				}
				if (array == null)
				{
					_byId = null;
					_byCat = null;
					AllowCatalogRetry();
					return;
				}
				Func<ItemCondition>[] array2 = array;
				foreach (Func<ItemCondition> func in array2)
				{
					if (func == null)
					{
						continue;
					}
					ItemCondition c = null;
					try
					{
						c = func();
					}
					catch
					{
						continue;
					}
					if (c == null)
					{
						continue;
					}
					string text = SafeStr(() => c.identifier);
					string display = SafeStr(() => c.display);
					string text2 = SafeStr(() => c.category);
					if (string.IsNullOrEmpty(text))
					{
						continue;
					}
					ConditionInfo conditionInfo = new ConditionInfo
					{
						Id = text,
						Display = display,
						Category = text2,
						Make = func
					};
					_byId[text] = conditionInfo;
					if (!string.IsNullOrEmpty(text2))
					{
						if (!_byCat.TryGetValue(text2, out var value))
						{
							value = new List<ConditionInfo>();
							_byCat[text2] = value;
						}
						value.Add(conditionInfo);
					}
				}
				_catalogFail = 0;
				RLog.Msg("官方档位目录就绪：" + _byId.Count + " 档 / " + _byCat.Count + " 类");
			}
			catch (Exception ex2)
			{
				MelonLogger.Error("[真印] ★ 官方档位目录建立失败: " + ex2.Message);
				_byId = null;
				_byCat = null;
				AllowCatalogRetry();
			}
		}

		private static ItemCondition ResolveOfficialCondition(string newId)
		{
			if (string.IsNullOrEmpty(newId))
			{
				return null;
			}
			EnsureCatalog();
			if (_byId == null)
			{
				return null;
			}
			if (!_byId.TryGetValue(newId, out var value))
			{
				return null;
			}
			try
			{
				ItemCondition c = value.Make();
				if (c == null)
				{
					return null;
				}
				string text = SafeStr(() => c.display);
				if (string.IsNullOrEmpty(text))
				{
					MelonLogger.Warning("[真印] ★ 官方档位 " + newId + " 的 display 为空 → 弃用");
					return null;
				}
				if (Config.VerboseLog)
				{
					RLog.Detail("官方档位 " + newId + " → display=" + text + " modValue=" + c.modValue + " modValueDisplay=" + c.modValueDisplay + " compareValue=" + c.compareValue);
				}
				return c;
			}
			catch (Exception ex)
			{
				MelonLogger.Error("[真印] ★ 官方档位 " + newId + " 构造失败: " + ex.Message);
				return null;
			}
		}

		internal static string[] OfficialIdsForCategory(string cat)
		{
			if (string.IsNullOrEmpty(cat))
			{
				return null;
			}
			EnsureCatalog();
			if (_byCat == null)
			{
				return null;
			}
			if (!_byCat.TryGetValue(cat, out var value) || value.Count == 0)
			{
				return null;
			}
			if (_idArrayCache.TryGetValue(cat, out var value2))
			{
				return value2;
			}
			string[] array = new string[value.Count];
			for (int i = 0; i < value.Count; i++)
			{
				array[i] = value[i].Id;
			}
			return _idArrayCache.GetOrAdd(cat, array);
		}

		private static string OfficialDisplay(string tierId)
		{
			if (string.IsNullOrEmpty(tierId))
			{
				return null;
			}
			if (_displayCache.TryGetValue(tierId, out var value))
			{
				return value;
			}
			string text = null;
			try
			{
				EnsureCatalog();
				if (_byId != null && _byId.TryGetValue(tierId, out var value2))
				{
					text = value2.Display;
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("OfficialDisplay", e);
			}
			if (text != null)
			{
				_displayCache.TryAdd(tierId, text);
			}
			return text;
		}

		public void InitPreferences()
		{
			MelonPreferences_Category val = MelonPreferences.CreateCategory("RealPrinter", "真实打印机");
			_cfg.FeatureCostEntry = val.CreateEntry<int>("FeatureCost", Config.FeatureCost, "改一次真值（验真/纯度）消耗多少信用点。", (string)null, false, false, (ValueValidator)null, (string)null);
			_cfg.VerboseLogEntry = val.CreateEntry<bool>("VerboseLog", Config.VerboseLog, "详细日志。默认关；排障时勾上它看流水。", (string)null, false, false, (ValueValidator)null, (string)null);
			_cfg.SupplierStockEntry = val.CreateEntry<bool>("SupplierStock", Config.SupplierStock, "供货商每次摆库存时，追加一台真实打印机（可购买）。", (string)null, false, false, (ValueValidator)null, (string)null);
			_cfg.EnableF6GiveEntry = val.CreateEntry<bool>("EnableF6Give", Config.EnableF6Give, "F6 直领：打开后按 F6 领取一台价值 0 的真实打印机（防刷钱）。关闭时不响应。", (string)null, false, false, (ValueValidator)null, (string)null);
			Config.FeatureCost = _cfg.FeatureCostEntry.Value;
			Config.VerboseLog = _cfg.VerboseLogEntry.Value;
			Config.SupplierStock = ReadFlag(_cfg.SupplierStockEntry, Config.SupplierStock);
			Config.EnableF6Give = ReadFlag(_cfg.EnableF6GiveEntry, Config.EnableF6Give);
			try
			{
				((MelonEventBase<LemonAction<int, int>>)(object)_cfg.FeatureCostEntry.OnEntryValueChanged).Subscribe((LemonAction<int, int>)((int oldV, int newV) =>
				{
					Config.FeatureCost = newV;
				}), 0, false);
				((MelonEventBase<LemonAction<bool, bool>>)(object)_cfg.VerboseLogEntry.OnEntryValueChanged).Subscribe((LemonAction<bool, bool>)((bool oldV, bool newV) =>
				{
					Config.VerboseLog = newV;
				}), 0, false);
				((MelonEventBase<LemonAction<bool, bool>>)(object)_cfg.SupplierStockEntry.OnEntryValueChanged).Subscribe((LemonAction<bool, bool>)((bool oldV, bool newV) =>
				{
					Config.SupplierStock = newV;
				}), 0, false);
				((MelonEventBase<LemonAction<bool, bool>>)(object)_cfg.EnableF6GiveEntry.OnEntryValueChanged).Subscribe((LemonAction<bool, bool>)((bool oldV, bool newV) =>
				{
					Config.EnableF6Give = newV;
				}), 0, false);
			}
			catch (Exception e)
			{
				RLog.Swallowed("CfgHotReload", e);
			}
			try
			{
				MelonPreferences.Save();
			}
			catch (Exception ex)
			{
				RLog.Warning("cfg 写盘失败: " + ex.Message);
			}
			try
			{
				int count = val.Entries.Count;
				if (count < 4)
				{
					RLog.Error("★★ cfg 条目不完整（" + count + "/" + 4 + "）—— 旧 cfg 残留或被面板重写过。已重新写盘；若面板仍缺开关，请把 UserData/MelonPreferences.cfg 的 [RealPrinter] 段删掉后重启游戏一次。");
				}
				else
				{
					RLog.Detail("cfg 自检通过（" + count + " 条全部注册并写盘）。");
				}
			}
			catch (Exception e2)
			{
				RLog.Swallowed("CfgSelfCheck", e2);
			}
		}

		private static bool ReadFlag(MelonPreferences_Entry<bool> entry, bool fallback)
		{
			try
			{
				if (entry != null)
				{
					return entry.Value;
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("ReadFlag", e);
			}
			return fallback;
		}

		private static int ReadHotkey(MelonPreferences_Entry<int> entry, int fallback)
		{
			try
			{
				if (entry != null)
				{
					return entry.Value;
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("ReadHotkey", e);
			}
			return fallback;
		}

		private static string SafeStr(Func<string> get)
		{
			try
			{
				return get();
			}
			catch
			{
				return null;
			}
		}

		private static bool IsPointerLike(string s)
		{
			if (string.IsNullOrEmpty(s) || s.Length < 3)
			{
				return false;
			}
			if (s[0] != '0')
			{
				return false;
			}
			if (s[1] != 'x' && s[1] != 'X')
			{
				return false;
			}
			for (int i = 2; i < s.Length; i++)
			{
				char c = s[i];
				if ((c < '0' || c > '9') && (c < 'a' || c > 'f') && (c < 'A' || c > 'F'))
				{
					return false;
				}
			}
			return true;
		}

		private static string DisplayNameOf(GameItem item)
		{
			string text = SafeId(item);
			if (item != null && !string.IsNullOrEmpty(text) && !IsPointerLike(text))
			{
				return text;
			}
			if (text != null && !IsPointerLike(text) && text[0] != '<')
			{
				return text;
			}
			return "该商品";
		}

		private static void Inform(string text, string color = "default")
		{
			if (string.IsNullOrEmpty(text))
			{
				return;
			}
			try
			{
				StoreUIManager instance = StoreUIManager.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					instance.Notify(text, color);
					return;
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("Inform", e);
			}
			RLog.Msg("提示（无 UI 通道）: " + text);
		}

		public override void OnInitializeMelon()
		{
			//IL_0458: Unknown result type (might be due to invalid IL or missing references)
			//IL_0465: Expected Obj, but got Unknown
			Self = this;
			try
			{
				InitPreferences();
			}
			catch (Exception ex)
			{
				RLog.Error("cfg 挂载异常（用默认值继续）: " + ex.Message);
			}
			try
			{
				BuildConditionCatalogOnce();
				List<string> list = SelfCheckConditionCatalog();
				if (list.Count > 0)
				{
					RLog.Error("★★ 官方档位自检发现 " + list.Count + " 处问题（改真值可能不可用）：\n" + string.Join("\n", list.ConvertAll((string x) => "    · " + x)));
				}
				else
				{
					RLog.Detail("官方档位自检通过（" + ConditionCatalogCount() + " 档全部可解析）。");
				}
			}
			catch (Exception ex2)
			{
				RLog.Error("★★ 官方档位自检异常: " + ex2.Message);
			}
			try
			{
				List<string> list2 = License.SelfCheckLicense();
				if (list2.Count > 0)
				{
					RLog.Error("★★ 许可类别自检发现 " + list2.Count + " 处问题（这些类别可能开不了窗）：\n" + string.Join("\n", list2.ConvertAll((string x) => "    · " + x)));
				}
				else
				{
					RLog.Detail("许可类别自检通过（" + License.AllAllowed().Length + " 类均有官方档位）。");
				}
			}
			catch (Exception ex3)
			{
				RLog.Error("★★ 许可类别自检异常: " + ex3.Message);
			}
			try
			{
				foreach (string item in SelfCheckOffsets())
				{
					RLog.Warning("偏移: " + item);
				}
			}
			catch (Exception ex4)
			{
				RLog.Error("★★ 偏移自检异常: " + ex4.Message);
			}
			try
			{
				SelfCheckInteractPatches();
			}
			catch (Exception ex5)
			{
				RLog.Error("交互补丁自检异常: " + ex5.Message);
			}
			try
			{
				((MelonBase)this).HarmonyInstance.PatchAll();
				_patched = true;
				RLog.Msg("Harmony 补丁已挂 ?");
				try
				{
					MethodInfo methodInfo = AccessTools.Method(typeof(StoreClientList), "PlaceSupplierInventory", (Type[])null, (Type[])null);
					if (methodInfo == null)
					{
						RLog.Error("★★ 供货商上货：找不到 StoreClientList.PlaceSupplierInventory（游戏版本变了？）——**这个版本没法自动获得打印机**。请把游戏退回原版本，或手动改存档放入一台真实打印机。");
					}
					else
					{
						int num = Harmony.GetPatchInfo((MethodBase)methodInfo)?.Postfixes.Count ?? 0;
						if (num > 0)
						{
							RLog.Msg("供货商上货补丁已挂 ?（PlaceSupplierInventory Postfix × " + num + "）—— 供货商每次摆货都会带一台打印机" + (Config.SupplierStock ? "" : "\u3000【当前 cfg 已关】"));
						}
						else
						{
							RLog.Error("★★ 供货商上货补丁没挂上 —— 供货商不会带打印机，也就没有正常获取途径。（多半是补丁类漏了 [HarmonyPatch] 特性）");
						}
					}
				}
				catch (Exception ex6)
				{
					RLog.Warning("供货商上货自检异常: " + ex6.Message);
				}
				try
				{
					MethodInfo methodInfo2 = AccessTools.Method(typeof(ModItemDirectory), "InitDirectory", (Type[])null, (Type[])null);
					if (methodInfo2 == null)
					{
						RLog.Warning("物品池注册：找不到 ModItemDirectory.InitDirectory（游戏版本变了？）—— real_printer 无法注册进物品池。");
					}
					else
					{
						int num2 = Harmony.GetPatchInfo((MethodBase)methodInfo2)?.Postfixes.Count ?? 0;
						if (num2 > 0)
						{
							RLog.Msg("物品池注册补丁已挂 ?（ModItemDirectory.InitDirectory Postfix × " + num2 + "）");
						}
						else
						{
							RLog.Warning("? 物品池注册补丁没挂上 —— 游戏目录里不会有 real_printer。");
						}
					}
				}
				catch (Exception ex7)
				{
					RLog.Warning("物品池注册自检异常: " + ex7.Message);
				}
				try
				{
					MethodInfo methodInfo3 = AccessTools.Method(typeof(GameItemElement), "ResolveSpriteByName", (Type[])null, (Type[])null);
					if (methodInfo3 == null)
					{
						RLog.Error("★★ 精灵缩放：找不到 GameItemElement.ResolveSpriteByName（游戏版本变了？）——机器图会比格子大（显示溢出），摆货本身不受影响。");
					}
					else
					{
						int num3 = Harmony.GetPatchInfo((MethodBase)methodInfo3)?.Postfixes.Count ?? 0;
						if (num3 > 0)
						{
							RLog.Msg("精灵缩放补丁已挂 ?（ResolveSpriteByName Postfix × " + num3 + "）—— 机器图 2×3 → 1×2，与占的格子一致");
						}
						else
						{
							RLog.Error("★★ 精灵缩放补丁没挂上 —— 机器图会比格子大（显示溢出）。（多半是补丁类漏了 [HarmonyPatch] 特性）");
						}
					}
				}
				catch (Exception ex8)
				{
					RLog.Warning("精灵缩放自检异常: " + ex8.Message);
				}
			}
			catch (Exception ex9)
			{
				RLog.Error("Harmony 补丁挂载异常（左键点物品不会有反应）: " + ex9.Message);
			}
			try
			{
				LogBanner();
			}
			catch (Exception ex10)
			{
				RLog.Error("横幅异常: " + ex10.Message);
			}
			try
			{
				MethodInfo methodInfo4 = AccessTools.Method(typeof(PlayerStore), "LoadGame", (Type[])null, (Type[])null);
				if (methodInfo4 != null)
				{
					((MelonBase)this).HarmonyInstance.Patch((MethodBase)methodInfo4, (HarmonyMethod)null, new HarmonyMethod(typeof(RealPrinterMod), "OnStoreLoadedPostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					RLog.Msg("读档重挂钩子已挂。");
				}
				else
				{
					RLog.Warning("? 未找到 PlayerStore.LoadGame —— 读档后交互会失效，请重启游戏。");
				}
			}
			catch (Exception ex11)
			{
				RLog.Warning("读档钩子异常: " + ex11.Message);
			}
		}

		private static void OnStoreLoadedPostfix()
		{
			try
			{
				Self?.RepairMachinesAfterLoad();
			}
			catch (Exception ex)
			{
				RLog.Warning("读档修复异常: " + ex.Message);
			}
		}

		public override void OnUpdate()
		{
			if (!Config.EnableF6Give)
			{
				return;
			}
			try
			{
				PollHotkeys();
			}
			catch (Exception e)
			{
				RLog.Swallowed("OnUpdate", e);
			}
		}

		public override void OnSceneWasLoaded(int buildIndex, string sceneName)
		{
			try
			{
				RepairMachinesAfterLoad();
			}
			catch (Exception e)
			{
				RLog.Swallowed("OnStoreLoadedPostfix", e);
			}
		}

		public override void OnApplicationQuit()
		{
			try
			{
				ClearMachineRegistry();
			}
			catch
			{
			}
		}

		private void LogBanner()
		{
			RLog.Msg("══════ 真实打印机 RealPrinter v1.4.12 ══════");
			List<string> list = new List<string>();
			string[] array = License.AllAllowed();
			foreach (string cat in array)
			{
				list.Add(License.CategoryLabel(cat));
			}
			RLog.Msg("\u3000支持：" + string.Join(" / ", list.ToArray()) + "（毒药按化学品算）");
			RLog.Msg("\u3000获取：" + (Config.SupplierStock ? "供货商上货（开）" : "供货商上货（关）"));
			RLog.Msg("\u3000F6 直领：" + (Config.EnableF6Give ? "开（按 F6 领取价值 0 的机器）" : "关"));
			if (Config.SupplierStock)
			{
				RLog.Msg("\u3000★ 供货商上货：开（供货商每次摆库存都会带一台，售价 " + 100 + "）");
			}
			RLog.Msg("\u3000机器外观：" + RealPrinterSpriteRescale.Report() + "（形状 1×2 格 = 2 格，柜台放得下）");
			RLog.Msg("\u3000用法：手持机器 → 左键点商品 → 选档位");
			if (!_patched)
			{
				RLog.Error("★★ Harmony 补丁未挂 —— 左键点物品不会有任何反应。");
			}
			if (RLog.SwallowedCount >= 100)
			{
				RLog.Warning("运行中已有 " + RLog.SwallowedCount + " 次异常被降级处理 —— 功能可能不完整，勾上 VerboseLog 看详情。");
			}
			if (Config.VerboseLog)
			{
				RLog.Msg("\u3000（详细日志已开，操作流水会一并显示）");
			}
		}

		private static GameInventory TryGetInventory(bool front)
		{
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance == (Object)null)
				{
					return null;
				}
				return (GameInventory)(object)(front ? instance.frontInvinvElement : instance.backInvinvElement);
			}
			catch (Exception e)
			{
				RLog.Swallowed("TryGetInventory", e);
			}
			return null;
		}

		private static int CountChildren(GameInventory inv)
		{
			if (inv == null)
			{
				return -1;
			}
			try
			{
				return inv.childItems?.Count ?? (-1);
			}
			catch (Exception e)
			{
				RLog.Swallowed("CountChildren", e);
			}
			return -1;
		}

		private static bool IsOnFrontCounter(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			IntPtr pointer;
			try
			{
				pointer = ((Il2CppObjectBase)item).Pointer;
			}
			catch (Exception e)
			{
				RLog.Swallowed("IsOnFrontCounter", e);
				return false;
			}
			if (pointer == IntPtr.Zero)
			{
				return false;
			}
			try
			{
				GameInventory val = TryGetInventory(front: true);
				if (val == null)
				{
					return false;
				}
				List<GameItem> childItems = val.childItems;
				if (childItems == null)
				{
					return false;
				}
				for (int i = 0; i < childItems.Count; i++)
				{
					GameItem val2 = childItems[i];
					if (val2 == null)
					{
						continue;
					}
					try
					{
						if (((Il2CppObjectBase)val2).Pointer == pointer)
						{
							return true;
						}
					}
					catch (Exception e2)
					{
						RLog.Swallowed("IsOnFrontCounter", e2);
					}
				}
			}
			catch (Exception e3)
			{
				RLog.Swallowed("IsOnFrontCounter", e3);
			}
			return false;
		}

		internal static void OpenEditor(GameItem tool, GameItem target, string expandedFeature = null)
		{
			if (target == null)
			{
				return;
			}
			EnterDecision enterDecision = TryEnterEditor(target);
			if (enterDecision.Verdict == EnterVerdict.RejectSilent)
			{
				return;
			}
			if (enterDecision.Verdict == EnterVerdict.RejectWithReason)
			{
				Inform(enterDecision.Reason, "yellow");
				if (Config.VerboseLog)
				{
					RLog.Detail("拒绝开窗：" + SafeId(target) + " —— " + (enterDecision.Reason ?? "").Replace('\n', ' '));
				}
			}
			else
			{
				OpenEditorInner(tool, target, enterDecision.Licensed, enterDecision.RawCount, expandedFeature);
			}
		}

		private static EnterDecision TryEnterEditor(GameItem target)
		{
			if (target == null)
			{
				return new EnterDecision
				{
					Verdict = EnterVerdict.RejectSilent,
					Reason = "目标为空"
				};
			}
			if (!IsSafeTarget(target))
			{
				return new EnterDecision
				{
					Verdict = EnterVerdict.RejectSilent,
					Reason = "目标对象状态不稳定（拖动过程中的正常现象）"
				};
			}
			List<FeatureRow> list = DetectFeatures(target);
			int num = 0;
			for (int i = 0; i < list.Count; i++)
			{
				if (License.IsAllowed(list[i].Category))
				{
					num++;
				}
			}
			if (num == 0)
			{
				if (!HasAnyCondition(target, list.Count))
				{
					return new EnterDecision
					{
						Verdict = EnterVerdict.RejectSilent,
						RawCount = 0,
						Reason = "目标没有任何真值特性（拖动/随手点，非许可范围）"
					};
				}
				return new EnterDecision
				{
					Verdict = EnterVerdict.RejectWithReason,
					RawCount = list.Count,
					Reason = License.RejectReason(target)
				};
			}
			List<FeatureRow> list2 = new List<FeatureRow>(num);
			for (int j = 0; j < list.Count; j++)
			{
				if (License.IsAllowed(list[j].Category))
				{
					list2.Add(list[j]);
				}
			}
			return new EnterDecision
			{
				Verdict = EnterVerdict.Open,
				Licensed = list2,
				RawCount = list.Count
			};
		}

		private static bool HasAnyCondition(GameItem item, int detectedCount)
		{
			if (detectedCount > 0)
			{
				return true;
			}
			if (!IsSafeTarget(item))
			{
				return false;
			}
			try
			{
				List<ItemFeature> itemFeatures = item.itemFeatures;
				return itemFeatures != null && itemFeatures.Count > 0;
			}
			catch (Exception e)
			{
				RLog.Swallowed("HasAnyCondition", e);
				return false;
			}
		}

		private static void OpenEditorInner(GameItem tool, GameItem target, List<FeatureRow> licensed, int rawFeatureCount, string expandedFeature)
		{
			try
			{
				CloseEditor();
				EditSession editSession = new EditSession
				{
					Tool = tool,
					Target = target,
					Cash = Pay.Query(),
					ExpandedFeatureId = expandedFeature
				};
				CustomUIBuilder val = CustomUIManager.Window("real_printer_editor", "真实打印机", (string)null);
				if (val == null)
				{
					MelonLogger.Error("★ CustomUIManager.Window 返回 null");
					_session = null;
					return;
				}
				val.SetSize(420f, 320f);
				val.SetDraggable(true);
				val.SetCloseOnEscape(true);
				if (Config.VerboseLog)
				{
					RLog.Detail("编辑窗：" + SafeId(target) + "\u3000原始 feature " + rawFeatureCount + " 条（可改真值 " + licensed.Count + " 条，其余类别不在许可范围）\u3000信用点 " + editSession.Cash);
					foreach (FeatureRow item in licensed)
					{
						RLog.Detail("  ★许可真值项: " + item.Id + "\u3000cat=" + item.Category + "\u3000现值 REAL=" + item.RealId + "\u3000假值 FAKE=" + item.FakeId + "\u3000游戏判可改=" + item.CanEdit);
					}
				}
				val.AddLabel(DisplayNameOf(target) + "\u3000｜" + licensed.Count + " 项真值\u3000｜目前余额 " + editSession.Cash + " 点", (string)null);
				ShrinkFont(val);
				val.BeginScroll(200f);
				foreach (FeatureRow item2 in licensed)
				{
					BuildFeatureRow(val, editSession, item2);
				}
				val.End();
				val.AddLabel("★ 每次修改消耗信用点 " + Config.FeatureCost, (string)null);
				ShrinkFont(val);
				val.AddButton("关闭", Action.op_Implicit((Action)(() =>
				{
					CloseEditor();
				})), (string)null);
				ShrinkFont(val);
				CustomUIWindow val2 = val.Show();
				if (val2 == null)
				{
					MelonLogger.Error("★ Show() 返回 null");
					_session = null;
					return;
				}
				try
				{
					val2.onClose = Action.op_Implicit((Action)(() =>
					{
						_session = null;
					}));
				}
				catch (Exception ex)
				{
					MelonLogger.Warning("挂 onClose 失败: " + ex.Message);
				}
				_session = editSession;
			}
			catch (Exception ex2)
			{
				MelonLogger.Error("★ OpenEditor 异常: " + ex2);
				try
				{
					Inform("开窗失败，详见日志", "yellow");
				}
				catch (Exception e)
				{
					RLog.Swallowed("OpenEditorInner", e);
				}
				_session = null;
			}
		}

		private static void RebuildEditor()
		{
			EditSession session = _session;
			if (session == null || session.Target == null)
			{
				MelonLogger.Warning("RebuildEditor：没有会话可重建（忽略）");
				return;
			}
			try
			{
				OpenEditor(session.Tool, session.Target, session.ExpandedFeatureId);
			}
			catch (Exception ex)
			{
				MelonLogger.Error("★ RebuildEditor 失败: " + ex.Message);
			}
		}

		private static void ShrinkFont(CustomUIBuilder b)
		{
			try
			{
				CustomUIElement last = b.Last;
				if (last != null)
				{
					TextMeshProUGUI text = last.text;
					if (!((Object)(object)text == (Object)null))
					{
						((TMP_Text)text).fontSize = 11.5f;
					}
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("ShrinkFont", e);
			}
		}

		internal static void CloseEditor()
		{
			try
			{
				CustomUIManager instance = CustomUIManager.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					instance.CloseWindow("real_printer_editor");
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("CloseEditor", e);
			}
			_session = null;
		}

		private static void BuildFeatureRow(CustomUIBuilder b, EditSession sess, FeatureRow fr)
		{
			string text = "f" + sess.RowSeq++.ToString("X");
			string text2 = OfficialDisplay(fr.RealId);
			if (string.IsNullOrEmpty(text2) || string.Equals(text2, fr.RealId, StringComparison.Ordinal))
			{
				text2 = fr.Display;
			}
			if (string.IsNullOrEmpty(text2))
			{
				text2 = fr.RealId;
			}
			string text3 = "★ " + text2 + (fr.CanEdit ? "" : "\u3000（不可改）") + ((fr.FakeId != fr.RealId) ? "\u3000假值不同" : "");
			b.AddLabel(text3, text + "L");
			ShrinkFont(b);
			try
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append(fr.Id).Append('\n').Append("类别 = ")
					.Append(License.CategoryLabel(fr.Category));
				stringBuilder.Append('\n').Append("真值 = ").Append(fr.RealId);
				stringBuilder.Append('\n').Append("假值 = ").Append(fr.FakeId);
				stringBuilder.Append('\n').Append(fr.CanEdit ? "游戏判定：可改（CanEditLabel=True）" : "游戏判定：**不可改**（CanEditLabel=False）");
				try
				{
					ItemCondition val = ((fr.Feature == null) ? null : fr.Feature.realCondition);
					if (val != null)
					{
						stringBuilder.Append("\n当前加价 = ").Append(val.modValueDisplay).Append("\u3000（与候选档位的「改后 ±N」同一口径：官方 modValueDisplay）");
					}
				}
				catch (Exception e)
				{
					RLog.Swallowed("BuildFeatureRow", e);
				}
				if (fr.Options != null && fr.Options.Length != 0)
				{
					stringBuilder.Append("\n\n可选档位（中文 ←→ 游戏内部值）：");
					for (int i = 0; i < fr.Options.Length; i++)
					{
						if (i > 0)
						{
							stringBuilder.Append("\n\u3000");
						}
						string text4 = OfficialDisplay(fr.Options[i]);
						stringBuilder.Append((string.IsNullOrEmpty(text4) || string.Equals(text4, fr.Options[i], StringComparison.Ordinal)) ? (fr.Options[i] + "\u3000★中文名取不到") : (text4 + "\u3000← " + fr.Options[i]));
					}
				}
				b.WithTooltip(stringBuilder.ToString());
			}
			catch (Exception e2)
			{
				RLog.Swallowed("BuildFeatureRow", e2);
			}
			b.AddButton("选档位", Action.op_Implicit((Action)(() =>
			{
				sess.ExpandedFeatureId = ((sess.ExpandedFeatureId == fr.Id) ? null : fr.Id);
				RebuildEditor();
			})), text + "B");
			if (sess.ExpandedFeatureId != fr.Id)
			{
				return;
			}
			if (fr.Options != null && fr.Options.Length != 0)
			{
				for (int num = 0; num < fr.Options.Length; num++)
				{
					string text5 = fr.Options[num];
					bool flag = string.Equals(text5, fr.RealId, StringComparison.OrdinalIgnoreCase);
					string text6 = OfficialDisplay(text5);
					bool flag2 = !string.IsNullOrEmpty(text6) && !string.Equals(text6, text5, StringComparison.Ordinal);
					b.AddLabel("\u3000├ " + (flag2 ? text6 : text5) + (flag ? "\u3000← 当前" : "") + (flag2 ? "" : "\u3000（★中文名取不到）"), text + "L" + num);
					ShrinkFont(b);
					string cap = text5;
					b.AddButton(flag ? "? 当前" : "选它", Action.op_Implicit((Action)(() =>
					{
						sess.ExpandedFeatureId = null;
						ApplyFeatureChange(fr, sess, cap);
						RebuildEditor();
					})), text + "O" + num);
				}
			}
			else
			{
				b.AddLabel("\u3000└ ★没有候选档位 —— 用下面的手填框", text + "Ln");
				ShrinkFont(b);
			}
			if (fr.Options == null || fr.Options.Length == 0)
			{
				b.AddLabel("\u3000└ 手填档位名（照抄游戏里真实的值）:", text + "Lh");
				ShrinkFont(b);
				b.AddInput("档位名", "", (Action<string>)null, text + "Ih");
				ShrinkFont(b);
				CustomUIElement inp = b.Last;
				b.AddButton("用它", Action.op_Implicit((Action)(() =>
				{
					try
					{
						TMP_InputField input = inp.input;
						string text7 = (((Object)(object)input == (Object)null) ? null : input.text);
						if (string.IsNullOrEmpty(text7))
						{
							Inform("输入框是空的，没法用", "yellow");
						}
						else
						{
							sess.ExpandedFeatureId = null;
							ApplyFeatureChange(fr, sess, text7.Trim());
							RebuildEditor();
						}
					}
					catch (Exception ex)
					{
						MelonLogger.Warning("读手填输入框失败: " + ex.Message);
					}
				})), text + "Bh");
				ShrinkFont(b);
			}
			b.AddButton("收起", Action.op_Implicit((Action)(() =>
			{
				sess.ExpandedFeatureId = null;
				RebuildEditor();
			})), text + "Bx");
		}

		private static void ApplyFeatureChange(FeatureRow fr, EditSession sess, string input)
		{
			if (sess == null)
			{
				Inform("会话已过期，请重新点物品", "yellow");
				return;
			}
			int featureCost = Config.FeatureCost;
			if (sess.Cash < featureCost)
			{
				Inform("信用点不足：改一次真值要 " + featureCost + "，你只有 " + sess.Cash, "yellow");
				RLog.Detail("真值改写被拒（余额不足）: " + fr.Id);
				return;
			}
			long num = ReadUnitValue(sess.Target);
			RLog.Detail("? 改真值: " + fr.Id + "\u3000" + fr.RealId + " → " + input + "\u3000费用 " + featureCost + "\u3000改前 unitValue=" + num);
			if (!WriteFeature(fr.Feature, input, out var message, sess.Target))
			{
				Inform("★ 没能改成：" + message, "yellow");
				RLog.Detail("★ 改真值失败: " + fr.Id + " → " + message);
				return;
			}
			if (!Pay.TryCharge(featureCost, out var failReason))
			{
				Inform("扣费失败（" + failReason + "）。改动已生效但没扣到钱 —— 请报我。", "yellow");
				RLog.Error("★★ 真值写入成功但扣费失败: " + fr.Id + "\u3000" + failReason);
			}
			else
			{
				sess.Cash = Pay.Query();
			}
			string text = OfficialDisplay(input);
			if (string.IsNullOrEmpty(text) || string.Equals(text, input, StringComparison.Ordinal))
			{
				text = input;
			}
			long num2 = ReadUnitValue(sess.Target);
			string text2 = num + " → " + num2;
			if (num == num2)
			{
				text2 += "\u3000★没变";
			}
			Inform("★ " + text + "\u3000价值 " + text2, "green");
			RLog.Detail("? 真值已改: " + fr.Id + "\u3000" + message + "\u3000（扣费 " + featureCost + "，余额 " + sess.Cash + "）");
			PlayRatchet();
		}

		private static long ReadUnitValue(GameItem item)
		{
			if (item == null)
			{
				return 0L;
			}
			try
			{
				return item.GetNegociatedValue();
			}
			catch (Exception e)
			{
				RLog.Swallowed("ReadUnitValue", e);
			}
			try
			{
				return item.unitValue;
			}
			catch (Exception e2)
			{
				RLog.Swallowed("ReadUnitValue", e2);
				return 0L;
			}
		}

		internal static List<FeatureRow> DetectFeatures(GameItem item)
		{
			List<FeatureRow> list = new List<FeatureRow>();
			if (item == null)
			{
				return list;
			}
			List<ItemFeature> itemFeatures = item.itemFeatures;
			if (itemFeatures == null || itemFeatures.Count == 0)
			{
				return list;
			}
			for (int i = 0; i < itemFeatures.Count; i++)
			{
				ItemFeature f = itemFeatures[i];
				if (f == null)
				{
					continue;
				}
				bool canEdit;
				try
				{
					canEdit = LabelerHelper.CanEditLabel(f);
				}
				catch
				{
					canEdit = false;
				}
				ItemCondition real = f.realCondition;
				ItemCondition fakeCondition = f.fakeCondition;
				if (real != null)
				{
					list.Add(new FeatureRow
					{
						Feature = f,
						Id = SafeStr(() => f.identifier),
						Category = SafeStr(() => f.category),
						RealId = CondId(real),
						FakeId = CondId(fakeCondition),
						Display = SafeStr(() => real.display),
						Options = OptionsFor(real),
						CanEdit = canEdit
					});
				}
			}
			return list;
		}

		private static string[] OptionsFor(ItemCondition c)
		{
			string text;
			try
			{
				text = ((c == null) ? null : c.category);
			}
			catch
			{
				return null;
			}
			if (string.IsNullOrEmpty(text))
			{
				return null;
			}
			return OptionsForCategory(text);
		}

		private static string[] OptionsForCategory(string cat)
		{
			if (string.IsNullOrEmpty(cat))
			{
				return null;
			}
			string[] array = OfficialIdsForCategory(cat);
			if (array != null && array.Length != 0)
			{
				return array;
			}
			EnsureCatalog();
			if (_byId != null && _byId.Count > 0)
			{
				List<string> list = new List<string>(_byId.Keys);
				list.Sort(StringComparer.OrdinalIgnoreCase);
				RLog.Detail("category「" + cat + "」未收录 → 列出全库 " + list.Count + " 个官方档位供手填");
				return list.ToArray();
			}
			return null;
		}

		private static bool WriteFeature(ItemFeature f, string newId, out string message, GameItem item)
		{
			message = "";
			if (f == null)
			{
				message = "feature 为空";
				return false;
			}
			ItemCondition realCondition = f.realCondition;
			ItemCondition fakeCondition = f.fakeCondition;
			if (realCondition == null)
			{
				message = "这件物品的 feature 没有真值（realCondition 为空）";
				return false;
			}
			string text = CondId(realCondition);
			CondId(fakeCondition);
			ItemCondition target = ResolveOfficialCondition(newId);
			if (target == null)
			{
				message = "找不到官方档位对象：" + newId + "\u3000→ 这档没在 ItemConditionList.Create* 映射表里。不做降级处理（降级会造成价格不动 + 英文显示）。把这行字和上面的候选列表给我，我照单补映射。";
				MelonLogger.Warning("[真印] ★ 无法解析官方档位: " + newId);
				return false;
			}
			try
			{
				f.SetConditions(target, target);
			}
			catch (Exception ex)
			{
				message = "SetConditions 调用失败: " + ex.Message;
				MelonLogger.Error("[真印] ★ SetConditions 异常: " + ex);
				return false;
			}
			string text2 = "";
			try
			{
				if (item != null && target != null)
				{
					bool flag = false;
					try
					{
						flag = target.HasOnActivateAction();
					}
					catch (Exception e)
					{
						RLog.Swallowed("WriteFeature", e);
					}
					if (flag)
					{
						target.ExecuteOnActivateAction(item);
						text2 = "\u3000已执行档位动作";
						if (Config.VerboseLog)
						{
							RLog.Detail("? 已执行档位动作: " + newId + "（onActivateActionId=" + SafeStr(() => target.onActivateActionId) + "）");
						}
					}
					else
					{
						RLog.Detail("· 该档位无onActivateAction（纯标签档位）: " + newId);
					}
				}
				else
				{
					MelonLogger.Warning("[真印] ★ 缺 item 引用，无法执行档位动作 —— 价格可能不会变");
					text2 = "\u3000★未执行动作（缺物品引用）";
				}
			}
			catch (Exception ex2)
			{
				MelonLogger.Error("[真印] ★ ExecuteOnActivateAction 异常: " + ex2.Message);
				text2 = "\u3000★动作执行异常：" + ex2.Message;
			}
			if (item != null)
			{
				try
				{
					long num = 0L;
					long num2 = 0L;
					try
					{
						num = item.unitValue;
					}
					catch (Exception e2)
					{
						RLog.Swallowed("WriteFeature", e2);
					}
					try
					{
						num2 = item.GetNegociatedValue();
					}
					catch (Exception e3)
					{
						RLog.Swallowed("WriteFeature", e3);
					}
					if (Config.VerboseLog)
					{
						RLog.Detail("商品侧回读: " + SafeId(item) + "\u3000基础 unitValue=" + num + "\u3000最终 GetNegociatedValue=" + num2 + "\u3000（最终价才是玩家看到的价值；基础值一般不随档位变）");
					}
					text2 = text2 + "｜最终价=" + num2;
				}
				catch (Exception ex3)
				{
					MelonLogger.Warning("[真印] 商品侧回读失败: " + ex3.Message);
				}
			}
			try
			{
				string text3 = CondId(f.realCondition);
				string text4 = CondId(f.fakeCondition);
				if (!string.Equals(text3, newId, StringComparison.OrdinalIgnoreCase))
				{
					message = "写入后回读不一致：期望 " + newId + "，实读 " + text3 + "\u3000→ 这个档位是**派生值**，游戏立刻重算覆盖了它。（老值 " + text + "）";
					MelonLogger.Warning("[真印] ? feature 真值回读不一致: " + f.identifier + " 期望=" + newId + " 实读=" + text3);
					return false;
				}
				string text5 = null;
				int num3 = 0;
				int num4 = 0;
				try
				{
					text5 = ((f.realCondition == null) ? null : f.realCondition.display);
					num3 = ((f.realCondition != null) ? f.realCondition.modValue : 0);
					num4 = ((f.realCondition != null) ? f.realCondition.compareValue : 0);
				}
				catch (Exception e4)
				{
					RLog.Swallowed("WriteFeature", e4);
				}
				string b = SafeStr(() => target.display);
				bool flag2 = num3 == target.modValue && num4 == target.compareValue;
				bool flag3 = string.Equals(text5, b, StringComparison.Ordinal);
				if (Config.VerboseLog)
				{
					RLog.Detail("真值写入回读: " + f.identifier + " id=" + text3 + " display=" + (text5 ?? "<空>") + " modValue=" + num3 + "(官方 " + target.modValue + ") compareValue=" + num4 + "(官方 " + target.compareValue + ")\u3000" + (flag2 ? "价格?" : "★★价格不符") + (flag3 ? " 显示?" : " ★显示与官方不一致"));
				}
				message = "真值 " + text + " → " + text3 + (string.Equals(text4, newId, StringComparison.OrdinalIgnoreCase) ? "（假值已同步）" : ("\u3000★假值未能同步：" + text4)) + "｜显示=" + (text5 ?? "<空>") + "｜加价=" + num3 + (flag2 ? "" : "\u3000★★加价与官方档位不符（售价不会同步）") + text2;
				return true;
			}
			catch (Exception ex4)
			{
				message = "回读异常: " + ex4.Message;
				return false;
			}
		}

		private static string CondId(ItemCondition c)
		{
			if (c == null)
			{
				return "(null)";
			}
			try
			{
				string identifier = c.identifier;
				if (!string.IsNullOrEmpty(identifier))
				{
					return identifier;
				}
				identifier = c.display;
				return string.IsNullOrEmpty(identifier) ? "(空)" : identifier;
			}
			catch
			{
				return "(读取失败)";
			}
		}

		private static bool TryClaim(GameItem self, GameItem target)
		{
			if (self == null || target == null)
			{
				return false;
			}
			if (SamePointer(self, target))
			{
				return false;
			}
			try
			{
				if (!IsOurMachine(self))
				{
					return false;
				}
				if (!IsTargetEditable(target))
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

		private static bool SamePointer(GameItem a, GameItem b)
		{
			if (a == b)
			{
				return true;
			}
			try
			{
				IntPtr pointer = ((Il2CppObjectBase)a).Pointer;
				IntPtr pointer2 = ((Il2CppObjectBase)b).Pointer;
				return pointer != IntPtr.Zero && pointer == pointer2;
			}
			catch
			{
				return false;
			}
		}

		private static Type ResolveType(string name)
		{
			if (string.IsNullOrEmpty(name))
			{
				return null;
			}
			for (int i = 0; i < KnownTypes.Length; i++)
			{
				if (string.Equals(KnownTypes[i].Name, name, StringComparison.Ordinal))
				{
					return KnownTypes[i].Type;
				}
			}
			return null;
		}

		private static MethodInfo FindTargetMethod(Type owner, string name)
		{
			if (owner == null)
			{
				return null;
			}
			List<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(owner);
			for (int i = 0; i < declaredMethods.Count; i++)
			{
				if (declaredMethods[i] != null && declaredMethods[i].Name == name)
				{
					return declaredMethods[i];
				}
			}
			return AccessTools.Method(owner, name, (Type[])null, (Type[])null);
		}

		internal void SelfCheckInteractPatches()
		{
			string[] array = new string[3] { "GameItem.MayTarget", "GameItem.CanTarget", "GameItem.Target" };
			int num = 0;
			StringBuilder stringBuilder = new StringBuilder();
			string[] array2 = array;
			foreach (string text in array2)
			{
				string[] array3 = text.Split('.');
				MethodInfo methodInfo = FindTargetMethod(ResolveType(array3[0]), array3[1]);
				bool flag = false;
				if (methodInfo != null)
				{
					Patches patchInfo = Harmony.GetPatchInfo((MethodBase)methodInfo);
					flag = patchInfo != null && ((patchInfo.Prefixes != null && patchInfo.Prefixes.Count > 0) || (patchInfo.Postfixes != null && patchInfo.Postfixes.Count > 0));
				}
				if (flag)
				{
					num++;
					continue;
				}
				int num2 = 0;
				if (methodInfo != null)
				{
					ParameterInfo[] parameters = methodInfo.GetParameters();
					for (int j = 0; j < parameters.Length; j++)
					{
						if (!parameters[j].Name.StartsWith("__"))
						{
							num2++;
						}
					}
				}
				stringBuilder.AppendLine("      " + text + " → " + ((methodInfo == null) ? "? 找不到目标方法" : "? 未挂上") + "（显式参数 " + num2 + " 个）");
				if (!(methodInfo != null))
				{
					continue;
				}
				stringBuilder.AppendLine("          ? 目标方法: " + methodInfo.DeclaringType?.FullName + "." + methodInfo.Name + "(" + methodInfo.GetParameters().Length + "参)  static=" + methodInfo.IsStatic + "  IsPublic=" + methodInfo.IsPublic + "  声明Asm=" + (methodInfo.DeclaringType?.Assembly?.GetName().Name ?? "?"));
				try
				{
					List<MethodInfo> declaredMethods = AccessTools.GetDeclaredMethods(methodInfo.DeclaringType);
					int num3 = 0;
					for (int k = 0; k < declaredMethods.Count; k++)
					{
						if (declaredMethods[k] != null && declaredMethods[k].Name == methodInfo.Name)
						{
							num3++;
						}
					}
					stringBuilder.AppendLine("          ? 同名方法数（含重载）= " + num3);
				}
				catch (Exception e)
				{
					RLog.Swallowed("SelfCheckInteractPatches", e);
				}
			}
			MarkPatchesReady(num >= 3);
			if (num >= 3)
			{
				RLog.Msg("交互补丁 " + num + "/" + 3 + " 全部就绪 ?（MayTarget / CanTarget / Target）");
			}
			else
			{
				MelonLogger.Error("[真印] ★★ 必需交互补丁缺失（" + num + "/" + 3 + "）—— 左键点物品会没反应。\n" + stringBuilder);
			}
		}

		private static GameItem CreateRealPrinter(int valueOverride = -1)
		{
			try
			{
				GameItem val = null;
				try
				{
					val = DirectoryMaster.Item("labeler", true);
					if (val == null)
					{
						val = ItemDirectory.CreateEmptyItem((string)null);
						MelonLogger.Warning("[真印] 目录里没有 labeler，退回CreateEmptyItem —— sprite 必须手动设，否则显示蓝格子。");
					}
				}
				catch (Exception ex)
				{
					RLog.Detail("取模板物品异常: " + ex.Message);
					try
					{
						val = ItemDirectory.CreateEmptyItem((string)null);
					}
					catch (Exception e)
					{
						RLog.Swallowed("CreateRealPrinter", e);
					}
				}
				if (val == null)
				{
					MelonLogger.Msg("[真印] ? CreateRealPrinter：模板与空壳都拿不到，放弃创建。");
					return null;
				}
				try
				{
					val.identifier = "real_printer";
				}
				catch (Exception e2)
				{
					RLog.Swallowed("CreateRealPrinter", e2);
				}
				try
				{
					val.SetSpriteAndShape("Items/items_tool", "labeler");
					RLog.Detail("sprite 借用原版工具图（Items/items_tool / labeler）。");
				}
				catch (Exception ex2)
				{
					MelonLogger.Msg("[真印] SetSpriteAndShape 异常: " + ex2.Message);
				}
				ShrinkShape(val);
				val.name = "真实打印机";
				val.shortDescription = "用法：装备或使用本机后，左键点目标商品即可（拖动无效）。";
				val.longDescription = "真实打印机：能改写四类商品真实品质的机器 ——香烟的真伪、化学品的纯度、金属锭的纯度、注射器的真伪与可用性。\n★ 正确用法：先「使用 / 装备」本机，再左键点击目标商品 → 选择档位 → 扣费写入。\n★ 直接拖到商品上无效（与原版红机不同，拖动不会触发）。\n所有权 / 任务进度 / 存档相关属性不开放。";
				int num = ((valueOverride < 0) ? 50 : valueOverride);
				val.unitValue = num;
				val.unitBaseValue = num;
				try
				{
					val.EnableTag("IMPORTANT_TAG", true);
				}
				catch (Exception e3)
				{
					RLog.Swallowed("CreateRealPrinter", e3);
				}
				try
				{
					val.EnableTag("LABELER_TAG", true);
				}
				catch (Exception e4)
				{
					RLog.Swallowed("CreateRealPrinter", e4);
				}
				try
				{
					val.EnableTag("REAL_PRINTER_TAG", true);
				}
				catch (Exception e5)
				{
					RLog.Swallowed("CreateRealPrinter", e5);
				}
				try
				{
					val.RemoveAllGameItemType();
				}
				catch (Exception ex3)
				{
					MelonLogger.Msg("[真印] RemoveAllGameItemType 异常: " + ex3.Message);
				}
				try
				{
					GeneralHelper.SetItemOwned(val, true);
				}
				catch (Exception e6)
				{
					RLog.Swallowed("CreateRealPrinter", e6);
				}
				if (!SelfCheckInteractionPatchesReady())
				{
					MelonLogger.Error("[真印] ★★ 建机时挂交互委托失败 —— 这台打印机左键点物品不会有任何反应。");
				}
				RegisterMachine(val);
				RLog.Detail("已创建真实打印机（照抄 CreateLabeler 机制）。");
				return val;
			}
			catch (Exception ex4)
			{
				MelonLogger.Msg("[真印] 创建工具失败: " + ex4.Message);
				return null;
			}
		}

		private static bool SelfCheckInteractionPatchesReady()
		{
			bool flag = HasPatchesReady();
			if (!flag)
			{
				MelonLogger.Error("[真印] ★★ 必需的交互补丁缺失 —— 左键点物品不会有任何反应。");
			}
			return flag;
		}

		private static bool HasPatchesReady()
		{
			return _patchesReady;
		}

		internal static void MarkPatchesReady(bool ready)
		{
			_patchesReady = ready;
		}

		internal static bool IsTargetEditable(GameItem target)
		{
			if (target == null)
			{
				return false;
			}
			if (!IsSafeTarget(target))
			{
				return false;
			}
			try
			{
				if (IsOurMachine(target))
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

		internal static bool IsSafeTarget(GameItem t)
		{
			if (t == null)
			{
				return false;
			}
			try
			{
				if (((Il2CppObjectBase)t).WasCollected)
				{
					return false;
				}
				long num = ((Il2CppObjectBase)t).Pointer.ToInt64();
				if (num == 0L)
				{
					return false;
				}
				return LooksLikeHeapPtr(new IntPtr(num));
			}
			catch
			{
				return false;
			}
		}

		private static bool IsDraggingNow()
		{
			try
			{
				ItemMouseDragHandler current = ItemMouseDragHandler.current;
				if ((Object)(object)current == (Object)null)
				{
					return false;
				}
				return current.IsDraggingItem;
			}
			catch (Exception e)
			{
				RLog.Swallowed("IsDraggingNow", e);
				return false;
			}
		}

		internal static void OnTargetItemFunc(GameItem target)
		{
			if (IsDraggingNow())
			{
				return;
			}
			try
			{
				if (target != null)
				{
					OpenEditor(null, target);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Error("[真印] ★ OnTargetItemFunc 异常: " + ex.Message);
				try
				{
					MelonLogger.Error(ex.StackTrace);
				}
				catch (Exception e)
				{
					RLog.Swallowed("OnTargetItemFunc", e);
				}
			}
		}

		private static void PlayRatchet()
		{
			try
			{
				EmporiumAudioPlayer instance = EmporiumAudioPlayer.Instance;
				if ((Object)(object)instance != (Object)null && (Object)(object)instance.ratchet != (Object)null)
				{
					instance.ratchet.Play();
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("PlayRatchet", e);
			}
		}

		internal static void RegisterMachine(GameItem it)
		{
			try
			{
				if (it != null)
				{
					long num = ((Il2CppObjectBase)it).Pointer.ToInt64();
					if (num != 0L)
					{
						_machinePtrs[num] = 0;
					}
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("RegisterMachine", e);
			}
		}

		internal static void UnregisterMachine(GameItem it)
		{
			try
			{
				if (it != null)
				{
					long num = ((Il2CppObjectBase)it).Pointer.ToInt64();
					if (num != 0L)
					{
						_machinePtrs.TryRemove(num, out var _);
					}
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("UnregisterMachine", e);
			}
		}

		internal static void UnregisterPtr(IntPtr p)
		{
			try
			{
				if (p != IntPtr.Zero)
				{
					_machinePtrs.TryRemove(p.ToInt64(), out var _);
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("UnregisterPtr", e);
			}
		}

		internal static void ClearMachineRegistry()
		{
			try
			{
				_machinePtrs.Clear();
			}
			catch (Exception e)
			{
				RLog.Swallowed("ClearMachineRegistry", e);
			}
		}

		private static bool LooksLikeOurMachineByTag(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			try
			{
				TagSystem state = it.state;
				if (((state != null) ? state.dict : null) == null)
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
				if (it.state.dict.ContainsKey("REAL_PRINTER_TAG"))
				{
					return true;
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("LooksLikeOurMachineByTag", e);
			}
			try
			{
				if (it.state.dict.ContainsKey("LABELER_TAG") && it.identifier == "real_printer")
				{
					return true;
				}
			}
			catch (Exception e2)
			{
				RLog.Swallowed("LooksLikeOurMachineByTag", e2);
			}
			try
			{
				if (it.identifier == "real_printer")
				{
					return true;
				}
			}
			catch (Exception e3)
			{
				RLog.Swallowed("LooksLikeOurMachineByTag", e3);
			}
			return false;
		}

		public static bool IsOurMachine(GameItem it)
		{
			if (it == null)
			{
				return false;
			}
			try
			{
				long num = ((Il2CppObjectBase)it).Pointer.ToInt64();
				if (num == 0L)
				{
					return false;
				}
				return _machinePtrs.ContainsKey(num);
			}
			catch
			{
				return false;
			}
		}

		internal static int BuildMachineRegistryNow()
		{
			try
			{
				FindOurMachines();
			}
			catch (Exception e)
			{
				RLog.Swallowed("BuildMachineRegistryNow", e);
			}
			return MachineRegistryCount;
		}

		private static List<GameItem> FindOurMachines()
		{
			List<GameItem> list = new List<GameItem>();
			HashSet<long> hashSet = new HashSet<long>();
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					return list;
				}
				bool[] array = new bool[2] { true, false };
				foreach (bool flag in array)
				{
					List<GameItem> val = instance.FindAllItem(flag);
					if (val == null)
					{
						continue;
					}
					for (int j = 0; j < val.Count; j++)
					{
						GameItem val2 = val[j];
						if (val2 == null || !LooksLikeOurMachineByTag(val2))
						{
							continue;
						}
						try
						{
							long num = ((Il2CppObjectBase)val2).Pointer.ToInt64();
							if (num == 0L || !hashSet.Add(num))
							{
								continue;
							}
							RegisterMachine(val2);
							goto IL_0091;
						}
						catch
						{
						}
						continue;
						IL_0091:
						list.Add(val2);
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Msg("[真印] FindOurMachines 异常: " + ex.Message);
			}
			return list;
		}

		internal static List<string> SelfCheckOffsets()
		{
			List<string> list = new List<string>();
			try
			{
				List<GameItem> list2 = FindOurMachines();
				GameItem val = ((list2.Count > 0) ? list2[0] : null);
				if (val == null)
				{
					list.Add("偏移自检跳过：存档里还没有真实打印机（按 F6 领一台后会自动复查）。");
					return list;
				}
				string text = null;
				try
				{
					text = val.identifier;
				}
				catch (Exception e)
				{
					RLog.Swallowed("SelfCheckOffsets", e);
				}
				if (string.IsNullOrEmpty(text))
				{
					list.Add("★ 偏移自检无法进行：连托管属性 identifier 都读不到（对象异常）。");
					return list;
				}
				string text2 = ReadIl2CppStringAt(val, 448);
				if (text2 == null)
				{
					list.Add("★★ IdOffset=0x" + 448.ToString("X") + " 读不出字符串 —— **偏移已失效**（游戏更新过？）。 托管属性读到的是「" + text + "」，裸偏移却读不到。 请重新 dump GameItem 确认 <identifier>k__BackingField 的偏移。");
				}
				else if (!string.Equals(text2, text, StringComparison.Ordinal))
				{
					list.Add("★★ IdOffset=0x" + 448.ToString("X") + " **偏移已失效**：裸偏移读到「" + text2 + "」，但托管属性是「" + text + "」。 游戏更新过字段顺序，请重新 dump GameItem 确认偏移。");
				}
				else
				{
					MelonLogger.Msg("[真印] 偏移自检通过：IdOffset=0x" + 448.ToString("X") + " 读到「" + text + "」，与托管属性一致 ?");
				}
				if (ReadFieldPtr(val, 432) == IntPtr.Zero)
				{
					list.Add("★ StateOffset=0x" + 432.ToString("X") + " 读回空 —— 该字段可能已移位（TagSystem 读不到）。 不影响主流程，但相关功能可能失效。");
				}
			}
			catch (Exception ex)
			{
				list.Add("偏移自检异常: " + ex.GetType().Name + ": " + ex.Message);
			}
			return list;
		}

		private unsafe static string ReadIl2CppStringAt(GameItem it, int offset)
		{
			IntPtr intPtr = ReadFieldPtr(it, offset);
			if (intPtr == IntPtr.Zero)
			{
				return null;
			}
			try
			{
				int num = IL2CPP.il2cpp_string_length(intPtr);
				if (num <= 0 || num > 512)
				{
					return null;
				}
				char* ptr = IL2CPP.il2cpp_string_chars(intPtr);
				if (ptr == null)
				{
					return null;
				}
				return Marshal.PtrToStringUni(new IntPtr(ptr), num);
			}
			catch (Exception e)
			{
				RLog.Swallowed("ReadIl2CppStringAt", e);
				return null;
			}
		}

		private static IntPtr ReadFieldPtr(GameItem it, int offset)
		{
			if (it == null)
			{
				return IntPtr.Zero;
			}
			if (offset < 0 || offset > 16384)
			{
				return IntPtr.Zero;
			}
			IntPtr pointer;
			try
			{
				if (((Il2CppObjectBase)it).WasCollected)
				{
					return IntPtr.Zero;
				}
				pointer = ((Il2CppObjectBase)it).Pointer;
			}
			catch
			{
				return IntPtr.Zero;
			}
			if (!LooksLikeHeapPtr(pointer))
			{
				return IntPtr.Zero;
			}
			long num = pointer.ToInt64() + offset + 8;
			if (num <= 0 || num > 8796093022207L)
			{
				return IntPtr.Zero;
			}
			try
			{
				return Marshal.ReadIntPtr(pointer, offset);
			}
			catch
			{
				return IntPtr.Zero;
			}
		}

		private static bool LooksLikeHeapPtr(IntPtr p)
		{
			if (p == IntPtr.Zero)
			{
				return false;
			}
			long num = p.ToInt64();
			if (num < 65536)
			{
				return false;
			}
			if (num > 8796093022207L)
			{
				return false;
			}
			if (num >> 40 == 65535)
			{
				return false;
			}
			return true;
		}

		private unsafe static string SafeId(GameItem it)
		{
			try
			{
				if (it == null)
				{
					return "<null>";
				}
				long num;
				try
				{
					if (((Il2CppObjectBase)it).WasCollected)
					{
						return "<collected>";
					}
					num = ((Il2CppObjectBase)it).Pointer.ToInt64();
				}
				catch
				{
					return "<err>";
				}
				if (num == 0L)
				{
					return "<no-ptr>";
				}
				if (!_machinePtrs.ContainsKey(num))
				{
					return "0x" + num.ToString("X");
				}
				IntPtr intPtr = ReadFieldPtr(it, 448);
				if (intPtr == IntPtr.Zero)
				{
					return "<no-id>";
				}
				int num2 = IL2CPP.il2cpp_string_length(intPtr);
				if (num2 <= 0 || num2 > 512)
				{
					return "<len" + num2 + ">";
				}
				IntPtr source = (IntPtr)IL2CPP.il2cpp_string_chars(intPtr);
				byte[] array = new byte[num2 * 2];
				Marshal.Copy(source, array, 0, num2 * 2);
				return Encoding.Unicode.GetString(array);
			}
			catch
			{
				return "<err>";
			}
		}

		private static string SafePtr(GameItem it)
		{
			try
			{
				return (it == null) ? "-" : ("0x" + ((Il2CppObjectBase)it).Pointer.ToString("X"));
			}
			catch
			{
				return "<err>";
			}
		}

		private static void TryRegister(ItemDirectory dir)
		{
			if ((Object)(object)dir == (Object)null)
			{
				return;
			}
			try
			{
				if (((Directory<GameItem>)(object)dir).Has("real_printer"))
				{
					return;
				}
				if ((Delegate)(object)_printerFactory == (Delegate)null)
				{
					_printerFactory = DelegateSupport.ConvertDelegate<Func<GameItem>>((Delegate)(Func<GameItem>)(() => CreateRealPrinter(100)));
				}
				RLog.Msg((((Directory<GameItem>)(object)dir).Add("real_printer", _printerFactory) ? "★ 已注册 " : "? 注册失败 ") + "real_printer → " + ((object)dir).GetType().Name);
			}
			catch (Exception ex)
			{
				RLog.Warning("注册 real_printer 异常: " + ex.Message);
			}
		}

		private void RepairMachinesAfterLoad()
		{
			try
			{
				if (Time.unscaledTime < _lastRepairTime + 1f)
				{
					return;
				}
				_lastRepairTime = Time.unscaledTime;
				bool flag = false;
				try
				{
					flag = PlayerStore.Instance != null;
				}
				catch
				{
					flag = false;
				}
				if (!flag)
				{
					return;
				}
				List<GameItem> list = FindOurMachines();
				if (list.Count == 0)
				{
					RLog.Detail("读档检查：存档里没有真实打印机（按 F6 领一台）。");
					return;
				}
				int machineRegistryCount = MachineRegistryCount;
				if (machineRegistryCount == 0)
				{
					RLog.Error("★★ 指针白名单重建后仍是空的 —— 交互必然失效（左键点物品没反应）。请重启游戏。");
				}
				int num = 0;
				int num2 = 0;
				for (int i = 0; i < list.Count; i++)
				{
					try
					{
						if (IsOurMachine(list[i]))
						{
							num++;
						}
						else
						{
							num2++;
						}
					}
					catch
					{
						num2++;
					}
				}
				RLog.Detail("读档检查完成：共 " + list.Count + " 台真实打印机（认得出来 " + num + "，认不出 " + num2 + "）· 指针白名单 " + machineRegistryCount + " 条");
				if (num2 > 0)
				{
					RLog.Error("★★ 有 " + num2 + " 台打印机读档后认不出来了，详见上面异常行。");
				}
			}
			catch (Exception ex)
			{
				RLog.Warning("读档重挂异常: " + ex.Message);
			}
		}

		private static void GiveOneManually()
		{
			try
			{
				RLog.Detail("===== F6 领取（价值 0）=====");
				GameItem val = CreateRealPrinter(0);
				if (val == null)
				{
					Inform("创建真实打印机返回 null（详见日志）", "yellow");
				}
				else if (!TryGiveToWarehouse(val, "真实打印机"))
				{
					Inform("真实打印机进主仓库失败，详见日志", "yellow");
				}
			}
			catch (Exception ex)
			{
				RLog.Msg("领取异常: " + ex.Message);
				try
				{
					Inform("领取异常：" + ex.Message, "yellow");
				}
				catch (Exception e)
				{
					RLog.Swallowed("GiveOneManually", e);
				}
			}
		}

		internal static void NoteMachineBought(GameItem item, int cost)
		{
			try
			{
				if (item != null)
				{
					RegisterMachine(item);
					RLog.Msg("? 已买下真实打印机（花了 " + cost + " 信用点）—— 现在归你了。");
					Inform("已买下真实打印机（-" + cost + "）");
				}
			}
			catch (Exception ex)
			{
				RLog.Warning("记录购买结果时异常: " + ex.Message);
			}
		}

		private static bool TryGiveToWarehouse(GameItem item, string what)
		{
			if (item == null)
			{
				return false;
			}
			GameInventory val = null;
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance != (Object)null)
				{
					val = (GameInventory)(object)instance.invElement;
				}
			}
			catch (Exception ex)
			{
				RLog.Msg("取主仓库异常: " + ex.Message);
			}
			if (val == null)
			{
				RLog.Error("? 主仓库不可用（EmporiumEntry.Instance / invElement 为空）—— " + what + " 没有发放，请回店里再试。");
				UnregisterMachine(item);
				try
				{
					item.Destroy();
				}
				catch (Exception e)
				{
					RLog.Swallowed("TryGiveToWarehouse", e);
				}
				return false;
			}
			if (!TryPlace(item, val))
			{
				RLog.Error("? " + what + " 放不进主仓库（格子满？形状不匹配？）");
				UnregisterMachine(item);
				try
				{
					item.Destroy();
				}
				catch (Exception e2)
				{
					RLog.Swallowed("TryGiveToWarehouse", e2);
				}
				return false;
			}
			bool flag = false;
			try
			{
				List<GameItem> childItems = val.childItems;
				if (childItems != null)
				{
					IntPtr pointer = ((Il2CppObjectBase)item).Pointer;
					for (int i = 0; i < childItems.Count; i++)
					{
						try
						{
							if (childItems[i] != null && ((Il2CppObjectBase)childItems[i]).Pointer == pointer)
							{
								flag = true;
								break;
							}
						}
						catch (Exception e3)
						{
							RLog.Swallowed("TryGiveToWarehouse", e3);
						}
					}
				}
			}
			catch (Exception ex2)
			{
				RLog.Msg("回读主仓库异常: " + ex2.Message);
			}
			if (!flag)
			{
				RLog.Error("? " + what + " 放入后回读主仓库没找到它 —— 判定为发放失败。");
				UnregisterMachine(item);
				try
				{
					item.Destroy();
				}
				catch (Exception e4)
				{
					RLog.Swallowed("TryGiveToWarehouse", e4);
				}
				return false;
			}
			RLog.Detail("? 已放主仓库（" + what + "）。");
			Inform("「" + what + "」已放主仓库");
			return true;
		}

		private static bool TryPlace(GameItem item, GameInventory inv)
		{
			if (item == null || inv == null)
			{
				return false;
			}
			try
			{
				SlotMarker val = null;
				try
				{
					GameGridInventory val2 = (GameGridInventory)(object)((inv is GameGridInventory) ? inv : null);
					val = ((val2 == null) ? inv.TryFindOneValidInventorySlot(item) : val2.TryFindOneValidInventorySlot(item, false));
				}
				catch (Exception e)
				{
					RLog.Swallowed("TryPlace", e);
				}
				if (val == null)
				{
					return false;
				}
				try
				{
					val.item = item;
					return val.AcceptUnchecked() > 0;
				}
				catch
				{
					return false;
				}
			}
			catch
			{
				return false;
			}
		}

		private void PollHotkeys()
		{
			try
			{
				if (!(Time.unscaledTime < _nextHotkeyTime) && Input.GetKeyDown((KeyCode)287))
				{
					_nextHotkeyTime = Time.unscaledTime + 0.4f;
					GiveOneManually();
				}
			}
			catch (Exception e)
			{
				RLog.Swallowed("PollHotkeys", e);
			}
		}

		internal static void ShrinkShape(GameItem it)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_000d: Expected Obj, but got Unknown
			if (it == null)
			{
				return;
			}
			try
			{
				GridShapeBuilder val = new GridShapeBuilder(1, 2);
				val.SetDataFill((byte)1);
				it.SetShape(val.shape);
			}
			catch (Exception ex)
			{
				RLog.Warning("[真印] 形状压缩失败（保持原 2×3）: " + ex.GetType().Name + ": " + ex.Message);
			}
		}

		internal static void SupplierStockOne()
		{
			GameItem val = CreateRealPrinter(100);
			if (val == null)
			{
				RLog.Error("★★ 造机器失败 —— 这次供货商没带打印机（详见上面的创建日志）。");
				return;
			}
			PlayerStore val2 = null;
			try
			{
				val2 = PlayerStore.Instance;
			}
			catch (Exception e)
			{
				RLog.Swallowed("SupplierStockOne", e);
			}
			if (val2 == null)
			{
				RLog.Error("★★ PlayerStore 不可用 —— 供货商没带打印机。");
				UnregisterMachine(val);
				try
				{
					val.Destroy();
					return;
				}
				catch (Exception e2)
				{
					RLog.Swallowed("SupplierStockOne", e2);
					return;
				}
			}
			try
			{
				val2.AddDirectSellingItemToTable(val, false, false, false, 0);
			}
			catch (Exception ex)
			{
				RLog.Error("★★ AddDirectSellingItemToTable 异常: " + ex.Message + "\u3000—— 该机器状态未知，已从登记表注销；若它已挂上柜台，下次读档会被自动重新认回。");
				UnregisterMachine(val);
				return;
			}
			int num = CountChildren(TryGetInventory(front: true));
			if (!IsOnFrontCounter(val))
			{
				RLog.Error("★★ 摆货后回读前柜台没找到这台机器（柜台现有 " + num + " 件）—— 被游戏销毁。机器已压到 1×2=2 格仍放不下，说明柜台剩下的空位是 1 格的孤立洞、拼不出 2 格。已从登记表注销。");
				UnregisterMachine(val);
				return;
			}
			long num2 = -1L;
			try
			{
				num2 = val.GetNegociatedValue();
			}
			catch (Exception e3)
			{
				RLog.Swallowed("SupplierStockOne", e3);
			}
			RLog.Detail("[供货商] ? 打印机已上货架 · 售价 " + num2 + "（柜台现有 " + num + " 件）");
			Inform("供货商带来了一台真实打印机（" + ((num2 > 0) ? (num2 + " 信用点") : "待定价") + "）");
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "OnItemBought")]
	internal static class RealPrinter_OnItemBoughtPatch
	{
		private static void Postfix(GameItem gameItem, int cost)
		{
			try
			{
				if (gameItem != null)
				{
					string text = null;
					try
					{
						text = gameItem.identifier;
					}
					catch
					{
						return;
					}
					if (string.Equals(text, "real_printer", StringComparison.Ordinal))
					{
						RealPrinterMod.NoteMachineBought(gameItem, cost);
					}
				}
			}
			catch (Exception ex)
			{
				RealPrinterMod.RLog.Warning("OnItemBought 后置处理异常: " + ex.Message);
			}
		}
	}
	internal static class RealPrinterSpriteRescale
	{
		private const float SrcCellW = 2f;

		private const float SrcCellH = 3f;

		private const float TargetCellW = 1f;

		private const float TargetCellH = 2f;

		private const float PpuScale = 1.5f;

		private static Sprite _scaled;

		private static int _scaledFromId;

		private static bool _reported;

		internal static Sprite GetShrunk(Sprite sourceSprite)
		{
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)sourceSprite == (Object)null)
			{
				return null;
			}
			if ((Object)(object)_scaled != (Object)null && _scaledFromId == ((Object)sourceSprite).GetInstanceID())
			{
				return _scaled;
			}
			try
			{
				Rect rect = sourceSprite.rect;
				Vector2 val = new Vector2((rect.width > 0f) ? (sourceSprite.pivot.x / rect.width) : 0.5f, (rect.height > 0f) ? (sourceSprite.pivot.y / rect.height) : 0.5f);
				Sprite val2 = Sprite.Create(sourceSprite.texture, rect, val, sourceSprite.pixelsPerUnit * 1.5f, 0u, (SpriteMeshType)0, Vector4.zero);
				if ((Object)(object)val2 == (Object)null)
				{
					return null;
				}
				_scaled = val2;
				_scaledFromId = ((Object)sourceSprite).GetInstanceID();
				if (!_reported)
				{
					string text = "?";
					try
					{
						text = ((object)((Texture)sourceSprite.texture).filterMode/*cast due to constrained. prefix*/).ToString();
					}
					catch
					{
					}
					RealPrinterMod.RLog.Msg("[缩图] ? 机器图已按 ppu×" + 1.5f + " 缩放（纹理复用，采样=" + text + "，宽 " + rect.width + "×" + rect.height + " px）");
					_reported = true;
				}
				return _scaled;
			}
			catch (Exception ex)
			{
				RealPrinterMod.RLog.Warning("[缩图] 缩放失败（保持原图）: " + ex.GetType().Name + ": " + ex.Message);
				return null;
			}
		}

		internal static string Report()
		{
			if (!((Object)(object)_scaled != (Object)null))
			{
				return "未生成";
			}
			return "已缩放 ppu×" + 1.5f + "（纹理复用，无重采样）";
		}
	}
	[HarmonyPatch(typeof(GameItemElement), "ResolveSpriteByName")]
	internal static class RealPrinter_RescaleSpritePatch
	{
		private static void Postfix(GameItemElement __instance, string name, ref Sprite __result)
		{
			try
			{
				if ((Object)(object)__result == (Object)null || __instance == null)
				{
					return;
				}
				string text = null;
				try
				{
					text = ((GameItem)__instance).identifier;
				}
				catch
				{
					return;
				}
				if (!string.Equals(text, "real_printer", StringComparison.Ordinal))
				{
					return;
				}
				string text2 = null;
				try
				{
					text2 = ((GameItem)__instance).spritePath;
				}
				catch
				{
					return;
				}
				if (text2 != null && string.Equals(name, text2, StringComparison.Ordinal))
				{
					Sprite shrunk = RealPrinterSpriteRescale.GetShrunk(__result);
					if ((Object)(object)shrunk != (Object)null)
					{
						__result = shrunk;
					}
				}
			}
			catch (Exception ex)
			{
				RealPrinterMod.RLog.Warning("[缩图] Postfix 异常: " + ex.Message);
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientList), "PlaceSupplierInventory")]
	internal static class RealPrinter_SupplierStockPatch
	{
		private static void Postfix()
		{
			try
			{
				if (RealPrinterMod.Config.SupplierStock)
				{
					RealPrinterMod.SupplierStockOne();
				}
			}
			catch (Exception ex)
			{
				RealPrinterMod.RLog.Warning("供货商上货后置处理异常: " + ex.Message);
			}
		}
	}
}
