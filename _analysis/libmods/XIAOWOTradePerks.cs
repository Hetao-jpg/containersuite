using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
using MelonLoader.Utils;
using Microsoft.CodeAnalysis;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using XIAOWOTradePerks;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Core), "XIAOWO-TradePerks", "1.9.17", "遗忘的小窝", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: MelonPriority(int.MinValue)]
[assembly: AssemblyFileVersion("1.9.17.0")]
[assembly: InternalsVisibleTo("XIAOWOTradePerks.RuntimeHarness")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyVersion("1.9.17.0")]
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
namespace XIAOWOTradePerks
{
	internal static class PerkSaveState
	{
		internal static bool IsMarked(PlayerStore store, string key, int period)
		{
			if (((store != null) ? store.modData : null) != null && store.modData.ContainsKey(key))
			{
				return string.Equals(store.modData[key], period.ToString(), StringComparison.Ordinal);
			}
			return false;
		}

		internal static void Mark(PlayerStore store, string key, int period)
		{
			if (store != null)
			{
				if (store.modData == null)
				{
					Dictionary<string, string> val = (store.modData = new Dictionary<string, string>());
				}
				store.modData[key] = period.ToString();
			}
		}

		internal static int Day()
		{
			return StoreStation.GetDayCounter();
		}

		internal static int Week(PlayerStore store)
		{
			if (store != null)
			{
				return Math.Max(0, (Day() - store.startingDay) / 7);
			}
			return 0;
		}
	}
	internal static class BlackMarketSample
	{
		internal const string RestrictedNeuralId = "system_capped_neural_core";

		internal const string SaveKey = "xiaowo_trade_black_market_sample_granted";

		private const int RetryFrames = 30;

		private static int _retryCountdown;

		private static string _lastFailure = string.Empty;

		internal static void RequestGrant()
		{
			_retryCountdown = 0;
			Tick();
		}

		internal static void Tick()
		{
			try
			{
				PlayerStore instance = PlayerStore.instance;
				if (instance != null && PerkRegistry.IsActive("xiaowo_trade_black_market_sample") && !PerkSaveState.IsMarked(instance, "xiaowo_trade_black_market_sample_granted", 1) && _retryCountdown-- <= 0)
				{
					_retryCountdown = 30;
					TryGrant();
				}
			}
			catch (Exception ex)
			{
				string text = ex.GetType().Name + ": " + ex.Message;
				if (!string.Equals(text, _lastFailure, StringComparison.Ordinal))
				{
					_lastFailure = text;
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("Black-market sample grant deferred: " + text);
					}
				}
			}
		}

		internal static bool TryGrant()
		{
			PlayerStore instance = PlayerStore.instance;
			EmporiumEntry instance2 = EmporiumEntry.Instance;
			if (instance == null || (Object)(object)instance2 == (Object)null || !PerkRegistry.IsActive("xiaowo_trade_black_market_sample"))
			{
				return false;
			}
			if (PerkSaveState.IsMarked(instance, "xiaowo_trade_black_market_sample_granted", 1))
			{
				return true;
			}
			if (Contains(instance2.GetInvItems(), "system_capped_neural_core"))
			{
				PerkSaveState.Mark(instance, "xiaowo_trade_black_market_sample_granted", 1);
				_lastFailure = string.Empty;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Black-market sample already present; save grant marker repaired.");
				}
				return true;
			}
			GameItem val = DirectoryMaster.Item("system_capped_neural_core", true);
			if (val == null)
			{
				return false;
			}
			val.DisableTag("TAG_NOT_PURCHASED", true);
			val.DisableTag("not_purchased", true);
			GameGridInventory backInvinvElement = instance2.backInvinvElement;
			if (backInvinvElement == null)
			{
				val.Destroy();
				return false;
			}
			backInvinvElement.TryFindOneValidInventorySlot(val, false);
			if (!((GameInventory)backInvinvElement).UncheckedAccept(val))
			{
				val.Destroy();
				return false;
			}
			instance2.TransferOwnershipBackInv();
			instance2.TransferOwnedItemBackToInv();
			if (!Contains(instance2.GetInvItems(), "system_capped_neural_core"))
			{
				return false;
			}
			PerkSaveState.Mark(instance, "xiaowo_trade_black_market_sample_granted", 1);
			_lastFailure = string.Empty;
			Instance log2 = Core.Log;
			if (log2 != null)
			{
				log2.Msg("Black-market sample granted: system_capped_neural_core.");
			}
			return true;
		}

		private static bool Contains(List<GameItem> items, string identifier)
		{
			if (items == null)
			{
				return false;
			}
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i] != null && string.Equals(items[i].identifier, identifier, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(StartingPerkContent), "HandleNewGamePerk")]
	internal static class BlackMarketSampleNewGamePatch
	{
		private static void Postfix()
		{
			BlackMarketSample.RequestGrant();
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class BlackMarketSampleLoadGamePatch
	{
		private static void Postfix()
		{
			BlackMarketSample.RequestGrant();
		}
	}
	internal static class ThirdWatchRoamer
	{
		internal const int NightVisits = 3;

		internal static void Apply(PlayerStore store)
		{
			if (store != null && PerkRegistry.IsActive("xiaowo_trade_third_watch_roamer"))
			{
				store.visitLeftTonight = Math.Max(store.visitLeftTonight, 3);
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "EndDay")]
	internal static class ThirdWatchEndDayPatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(PlayerStore __instance)
		{
			ThirdWatchRoamer.Apply(__instance);
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "IsShortSleeper")]
	internal static class ThirdWatchShortSleeperPatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(ref bool __result)
		{
			if (PerkRegistry.IsActive("xiaowo_trade_third_watch_roamer"))
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "HandleAfterhourDebuff")]
	internal static class ThirdWatchAfterhourDebuffPatch
	{
		[HarmonyPriority(800)]
		private static bool Prefix()
		{
			return !PerkRegistry.IsActive("xiaowo_trade_third_watch_roamer");
		}
	}
	internal static class ShowcaseCurator
	{
		internal static int Adjust(int attractiveness, bool active, bool hasMerchandise)
		{
			if (active && hasMerchandise && attractiveness > 0)
			{
				return (int)Math.Round((double)attractiveness * 1.5, MidpointRounding.AwayFromZero);
			}
			return attractiveness;
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "GetCurrentStoreAttractiveness")]
	internal static class ShowcaseCuratorAttractivenessPatch
	{
		private static void Postfix(ref int __result)
		{
			bool flag = PerkRegistry.IsActive("xiaowo_trade_showcase_curator");
			bool hasMerchandise = flag && ShowcaseHelper.HasMerchandise();
			__result = ShowcaseCurator.Adjust(__result, flag, hasMerchandise);
		}
	}
	internal static class SecondSift
	{
		internal const string SaveKey = "xiaowo_trade_second_sift_day";

		[ThreadStatic]
		private static bool _generatingExtra;

		internal static int DesiredExtraCount(bool active, int nativeCount, bool alreadyGranted)
		{
			if (!active || nativeCount <= 0 || alreadyGranted)
			{
				return 0;
			}
			return 1;
		}

		internal static void AppendExtra(List<GameItem> items)
		{
			PlayerStore instance = PlayerStore.Instance;
			int period = PerkSaveState.Day();
			if (_generatingExtra || items == null || DesiredExtraCount(PerkRegistry.IsActive("xiaowo_trade_second_sift"), items.Count, PerkSaveState.IsMarked(instance, "xiaowo_trade_second_sift_day", period)) == 0)
			{
				return;
			}
			_generatingExtra = true;
			try
			{
				List<GameItem> randomScavengedItem = ScavHelper.GetRandomScavengedItem();
				if (randomScavengedItem == null || randomScavengedItem.Count == 0)
				{
					return;
				}
				items.Add(randomScavengedItem[0]);
				for (int i = 1; i < randomScavengedItem.Count; i++)
				{
					GameItem val = randomScavengedItem[i];
					if (val != null)
					{
						val.Destroy();
					}
				}
				PerkSaveState.Mark(instance, "xiaowo_trade_second_sift_day", period);
			}
			finally
			{
				_generatingExtra = false;
			}
		}
	}
	[HarmonyPatch(typeof(ScavHelper), "GetRandomScavengedItem")]
	internal static class SecondSiftLootPatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(List<GameItem> __result)
		{
			SecondSift.AppendExtra(__result);
		}
	}
	internal static class FamiliarFace
	{
		internal const string SaveKey = "xiaowo_trade_familiar_face_week";

		[ThreadStatic]
		private static bool _ordinaryInspectionContext;

		internal static void BeginOrdinaryInspection()
		{
			_ordinaryInspectionContext = true;
		}

		internal static void EndOrdinaryInspection()
		{
			_ordinaryInspectionContext = false;
		}

		internal static bool IsOrdinaryInspection(StoreClient client)
		{
			if (client != null && client.isSecurity && !client.isWanted && !client.isSentenceOfficer)
			{
				return string.IsNullOrEmpty(client.eventSourceId);
			}
			return false;
		}

		internal static bool ShouldWarn(PlayerStore store, StoreClient client)
		{
			if (!_ordinaryInspectionContext || !PerkRegistry.IsActive("xiaowo_trade_familiar_face") || !IsOrdinaryInspection(client))
			{
				return false;
			}
			int period = PerkSaveState.Week(store);
			if (PerkSaveState.IsMarked(store, "xiaowo_trade_familiar_face_week", period))
			{
				return false;
			}
			PerkSaveState.Mark(store, "xiaowo_trade_familiar_face_week", period);
			return true;
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "HandleInspectionClient")]
	internal static class FamiliarFaceInspectionContextPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			FamiliarFace.BeginOrdinaryInspection();
		}

		[HarmonyPriority(0)]
		private static Exception Finalizer(Exception __exception)
		{
			FamiliarFace.EndOrdinaryInspection();
			return __exception;
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "TryAddClient")]
	internal static class FamiliarFaceInspectionPatch
	{
		[HarmonyPriority(800)]
		private static bool Prefix(StoreClient storeClient)
		{
			PlayerStore instance = PlayerStore.Instance;
			if (!FamiliarFace.ShouldWarn(instance, storeClient))
			{
				return true;
			}
			if (instance != null)
			{
				instance.AddNightLog(ModLocalization.Select("安保人员认出了你：本周第一次普通检查改为口头警告。", "Security recognized you: this week's first ordinary inspection became a warning."));
			}
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Familiar Face converted an ordinary inspection into a warning.");
			}
			return false;
		}
	}
	internal static class ThinSupply
	{
		internal static int RemainingCount(int original)
		{
			if (original > 0)
			{
				return Math.Max(1, (int)Math.Ceiling((double)original * 0.75));
			}
			return 0;
		}

		internal static HashSet<long> Snapshot()
		{
			HashSet<long> hashSet = new HashSet<long>();
			EmporiumEntry instance = EmporiumEntry.Instance;
			List<GameItem> val = ((instance != null) ? instance.GetAllNonOwnedItem() : null);
			if (val == null)
			{
				return hashSet;
			}
			for (int i = 0; i < val.Count; i++)
			{
				if (val[i] != null)
				{
					hashSet.Add(((Il2CppObjectBase)val[i]).Pointer.ToInt64());
				}
			}
			return hashSet;
		}

		internal static void Reduce(StoreClient client, HashSet<long> before)
		{
			if (!PerkRegistry.IsActive("xiaowo_trade_thin_supply") || !TradeEconomyEffects.IsOrdinarySupplier(client))
			{
				return;
			}
			EmporiumEntry instance = EmporiumEntry.Instance;
			List<GameItem> val = ((instance != null) ? instance.GetAllNonOwnedItem() : null);
			if (val == null)
			{
				return;
			}
			List<GameItem> list = new List<GameItem>();
			for (int i = 0; i < val.Count; i++)
			{
				GameItem val2 = val[i];
				if (val2 != null && !before.Contains(((Il2CppObjectBase)val2).Pointer.ToInt64()))
				{
					list.Add(val2);
				}
			}
			int num = RemainingCount(list.Count);
			for (int num2 = list.Count - 1; num2 >= num; num2--)
			{
				GameItem val3 = list[num2];
				GameInventory parentInventory = val3.parentInventory;
				if (parentInventory != null)
				{
					parentInventory.Expel(val3);
				}
				val3.Destroy();
			}
			if (list.Count > num)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Thin Supply reduced ordinary supplier stock from " + list.Count + " to " + num + ".");
				}
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientInstance), "CreateClientInstance")]
	internal static class ThinSupplyClientInventoryPatch
	{
		private static void Prefix(ref HashSet<long> __state)
		{
			__state = ThinSupply.Snapshot();
		}

		private static void Postfix(StoreClient storeClient, HashSet<long> __state)
		{
			ThinSupply.Reduce(storeClient, __state ?? new HashSet<long>());
		}
	}
	internal static class HighRollerMagnet
	{
		internal const string AppliedTag = "xiaowo_trade_high_roller_budget_applied";

		internal static int DoubledBudget(int original)
		{
			if (original > 0)
			{
				if (original <= 1073741823)
				{
					return original * 2;
				}
				return int.MaxValue;
			}
			return original;
		}

		internal static void Apply(StoreClient client)
		{
			if (client != null && PerkRegistry.IsActive("xiaowo_trade_high_roller_magnet") && !client.IsTag("xiaowo_trade_high_roller_budget_applied"))
			{
				int budget = client.GetBudget();
				if (budget > 0)
				{
					client.OverrideBudget(DoubledBudget(budget));
					client.AddTag("xiaowo_trade_high_roller_budget_applied");
				}
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "TrackClient")]
	internal static class HighRollerBudgetPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix(StoreClient storeClient)
		{
			HighRollerMagnet.Apply(storeClient);
		}
	}
	internal static class AugHuntContract
	{
		internal const int ExtraVisitChance = 30;

		internal const int DailyAugLimit = 2;

		internal const string GameOverReason = "aug";

		private static readonly HashSet<long> SpawnedAugs = new HashSet<long>();

		private static readonly HashSet<long> RequiredKillAugs = new HashSet<long>();

		private static readonly HashSet<long> KilledAugs = new HashSet<long>();

		private static readonly HashSet<long> AddAugBaseline = new HashSet<long>();

		private static string _dayKey = string.Empty;

		private static string _extraAttemptKey = string.Empty;

		private static bool _escapedAlive;

		private static bool _sleepResolved;

		private static bool _ignoreLoadedDayForRetaliation;

		[ThreadStatic]
		private static bool _forceNextNativeAugRoll;

		[ThreadStatic]
		private static bool _blockNativeAugAdd;

		internal static int SpawnedCount => SpawnedAugs.Count;

		internal static int RequiredKillCount => RequiredKillAugs.Count;

		internal static int KilledCount => KilledAugs.Count;

		internal static bool EscapedAlive => _escapedAlive;

		internal static bool IgnoringLoadedDayForRetaliation => _ignoreLoadedDayForRetaliation;

		internal static bool ForceNextNativeAugRoll => _forceNextNativeAugRoll;

		internal static void BeforeDailyGeneration(StoreClientManager manager, out bool eligible)
		{
			EnsureDay();
			TrackExistingAugs(manager);
			PlayerStore instance = PlayerStore.Instance;
			bool flag = IsContractRunning(instance);
			_blockNativeAugAdd = flag && SpawnedCount > 0;
			eligible = flag && IsNativeAugWindow(manager, instance, StoreStation.instance);
		}

		internal static void AfterDailyGeneration(StoreClientManager manager, bool eligible)
		{
			try
			{
				EnsureDay();
				TrackExistingAugs(manager);
				if (!eligible || !IsContractRunning(PlayerStore.Instance) || string.Equals(_extraAttemptKey, _dayKey, StringComparison.Ordinal))
				{
					return;
				}
				_extraAttemptKey = _dayKey;
				if (SpawnedCount < 2 && RNG.Roll(30))
				{
					_forceNextNativeAugRoll = true;
					manager.AddAugClient();
					Instance log = Core.Log;
					if (log != null)
					{
						log.Msg("Aug hunt contract extra visit roll succeeded; daily Aug count=" + SpawnedCount + ".");
					}
				}
			}
			finally
			{
				_forceNextNativeAugRoll = false;
				_blockNativeAugAdd = false;
				TrackExistingAugs(manager);
			}
		}

		internal static void AbortDailyGeneration()
		{
			_forceNextNativeAugRoll = false;
			_blockNativeAugAdd = false;
			AddAugBaseline.Clear();
		}

		internal static bool BeforeAddAug(StoreClientManager manager)
		{
			EnsureDay();
			TrackExistingAugs(manager);
			AddAugBaseline.Clear();
			foreach (long spawnedAug in SpawnedAugs)
			{
				AddAugBaseline.Add(spawnedAug);
			}
			return CanAddAug(IsContractRunning(PlayerStore.Instance), SpawnedCount, _forceNextNativeAugRoll, _blockNativeAugAdd);
		}

		internal static void AfterAddAug(StoreClientManager manager)
		{
			EnsureDay();
			TrackExistingAugs(manager);
			if (IsContractRunning(PlayerStore.Instance) && !_ignoreLoadedDayForRetaliation)
			{
				foreach (long spawnedAug in SpawnedAugs)
				{
					if (!AddAugBaseline.Contains(spawnedAug))
					{
						RequiredKillAugs.Add(spawnedAug);
					}
				}
			}
			AddAugBaseline.Clear();
		}

		internal static void ForceNativeRoll(ref int chance)
		{
			if (_forceNextNativeAugRoll && chance == 80)
			{
				chance = 100;
				_forceNextNativeAugRoll = false;
			}
		}

		internal static void MarkCurrentAugKilled()
		{
			EnsureDay();
			PlayerStore instance = PlayerStore.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				StoreClientInstance currentClientInstance = instance.currentClientInstance;
				obj = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
			}
			StoreClient val = (StoreClient)obj;
			if (val == null || !val.isAug)
			{
				return;
			}
			long num = ((Il2CppObjectBase)val).Pointer.ToInt64();
			if (num != 0L)
			{
				SpawnedAugs.Add(num);
				RequiredKillAugs.Add(num);
				KilledAugs.Add(num);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Aug hunt contract recorded a kill; " + KilledCount + "/" + RequiredKillCount + ".");
				}
			}
		}

		internal static void MarkCurrentAugEscaped()
		{
			EnsureDay();
			PlayerStore instance = PlayerStore.Instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				StoreClientInstance currentClientInstance = instance.currentClientInstance;
				obj = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
			}
			StoreClient val = (StoreClient)obj;
			if (val != null && val.isAug)
			{
				long num = ((Il2CppObjectBase)val).Pointer.ToInt64();
				if (num != 0L)
				{
					SpawnedAugs.Add(num);
					RequiredKillAugs.Add(num);
				}
			}
			_escapedAlive = true;
		}

		internal static bool ResolveSleep(Action<string> executeGameOver)
		{
			EnsureDay();
			PlayerStore instance = PlayerStore.Instance;
			TrackExistingAugs((instance != null) ? instance.storeClientManager : null);
			return InvokeGameOverIfNeeded(IsContractRunning(PlayerStore.Instance), RequiredKillCount, KilledCount, _escapedAlive, executeGameOver);
		}

		internal static bool InvokeGameOverIfNeeded(bool active, int spawned, int killed, bool escapedAlive, Action<string> executeGameOver)
		{
			if (!ShouldGameOver(active, spawned, killed, escapedAlive) || executeGameOver == null)
			{
				return false;
			}
			if (_sleepResolved)
			{
				return false;
			}
			_sleepResolved = true;
			executeGameOver("aug");
			return true;
		}

		internal static bool ShouldGameOver(bool active, int spawned, int killed, bool escapedAlive)
		{
			if (active && spawned > 0)
			{
				if (!escapedAlive)
				{
					return killed < spawned;
				}
				return true;
			}
			return false;
		}

		internal static bool ShouldContractRun(bool perkActive, bool shootingUnlocked)
		{
			return perkActive & shootingUnlocked;
		}

		internal static bool IsContractRunning(PlayerStore store)
		{
			return ShouldContractRun(PerkRegistry.IsActive("xiaowo_trade_aug_hunt_contract"), store != null && store.isShootingUnlocked);
		}

		internal static bool CanAddAug(bool contractRunning, int spawned, bool isExtraAdd, bool blockNativeAdd)
		{
			if (!contractRunning)
			{
				return true;
			}
			if (spawned >= 2)
			{
				return false;
			}
			return !blockNativeAdd | isExtraAdd;
		}

		internal static bool IsNativeAugWindow(StoreClientManager manager, PlayerStore store, StoreStation station)
		{
			if (manager == null || store == null || station == null)
			{
				return false;
			}
			int dayCounter = StoreStation.GetDayCounter();
			if (!store.isShootingUnlocked || !store.isAugIntroduced || store.isAugEnded || manager.isAugSpawnHandledToday)
			{
				return false;
			}
			if (dayCounter == 13 || dayCounter == 18)
			{
				return false;
			}
			if (dayCounter >= 62 && store.augPresence <= 0)
			{
				return false;
			}
			if (store.nextAugSpawnDay >= 0)
			{
				return dayCounter >= store.nextAugSpawnDay;
			}
			return false;
		}

		internal static void ResetForTest()
		{
			_dayKey = string.Empty;
			_extraAttemptKey = string.Empty;
			SpawnedAugs.Clear();
			RequiredKillAugs.Clear();
			KilledAugs.Clear();
			AddAugBaseline.Clear();
			_escapedAlive = false;
			_sleepResolved = false;
			_ignoreLoadedDayForRetaliation = false;
			_forceNextNativeAugRoll = false;
			_blockNativeAugAdd = false;
		}

		internal static void OnShotgunStateChanged()
		{
			ClearTracking();
			_ignoreLoadedDayForRetaliation = false;
			_dayKey = GetDayKey();
		}

		internal static void OnGameLoading()
		{
			_dayKey = string.Empty;
			ClearTracking();
			_ignoreLoadedDayForRetaliation = true;
		}

		private static void EnsureDay()
		{
			string dayKey = GetDayKey();
			if (!string.Equals(dayKey, _dayKey, StringComparison.Ordinal))
			{
				bool num = _ignoreLoadedDayForRetaliation && _dayKey.Length == 0;
				_dayKey = dayKey;
				ClearTracking();
				if (!num)
				{
					_ignoreLoadedDayForRetaliation = false;
				}
			}
		}

		private static string GetDayKey()
		{
			PlayerStore instance = PlayerStore.Instance;
			return (((instance != null) ? instance.runID : null) ?? "no-run") + "|" + Math.Max(0, StoreStation.GetDayCounter());
		}

		private static void ClearTracking()
		{
			SpawnedAugs.Clear();
			RequiredKillAugs.Clear();
			KilledAugs.Clear();
			AddAugBaseline.Clear();
			_extraAttemptKey = string.Empty;
			_escapedAlive = false;
			_sleepResolved = false;
			_forceNextNativeAugRoll = false;
			_blockNativeAugAdd = false;
		}

		private static void TrackExistingAugs(StoreClientManager manager)
		{
			if (((manager != null) ? manager.clientStack : null) != null)
			{
				for (int i = 0; i < manager.clientStack.Count; i++)
				{
					TrackAug(manager.clientStack[i]);
				}
			}
			PlayerStore instance = PlayerStore.Instance;
			object client;
			if (instance == null)
			{
				client = null;
			}
			else
			{
				StoreClientInstance currentClientInstance = instance.currentClientInstance;
				client = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
			}
			TrackAug((StoreClient)client);
		}

		private static void TrackAug(StoreClient client)
		{
			if (client != null && client.isAug)
			{
				long num = ((Il2CppObjectBase)client).Pointer.ToInt64();
				if (num != 0L)
				{
					SpawnedAugs.Add(num);
				}
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class AugHuntLoadResetPatch
	{
		private static void Prefix()
		{
			AugHuntContract.OnGameLoading();
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "GenerateClient")]
	internal static class AugHuntDailyGenerationPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix(StoreClientManager __instance, out bool __state)
		{
			AugHuntContract.BeforeDailyGeneration(__instance, out __state);
		}

		[HarmonyPriority(0)]
		private static void Postfix(StoreClientManager __instance, bool __state)
		{
			AugHuntContract.AfterDailyGeneration(__instance, __state);
		}

		[HarmonyPriority(0)]
		private static Exception Finalizer(Exception __exception)
		{
			if (__exception != null)
			{
				AugHuntContract.AbortDailyGeneration();
			}
			return __exception;
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "AddAugClient")]
	internal static class AugHuntDailyCapPatch
	{
		private static bool Prefix(StoreClientManager __instance)
		{
			return AugHuntContract.BeforeAddAug(__instance);
		}

		private static void Postfix(StoreClientManager __instance)
		{
			AugHuntContract.AfterAddAug(__instance);
		}
	}
	[HarmonyPatch(typeof(RNG), "Roll", new Type[] { typeof(int) })]
	internal static class AugHuntNativeRollPatch
	{
		private static void Prefix(ref int chance)
		{
			AugHuntContract.ForceNativeRoll(ref chance);
		}
	}
	[HarmonyPatch(typeof(AugHelper), "OnGoodKill")]
	internal static class AugHuntGoodKillPatch
	{
		private static void Prefix()
		{
			AugHuntContract.MarkCurrentAugKilled();
		}
	}
	[HarmonyPatch(typeof(AugHelper), "CleanupKill")]
	internal static class AugHuntCleanupKillPatch
	{
		private static void Prefix()
		{
			AugHuntContract.MarkCurrentAugKilled();
		}
	}
	[HarmonyPatch(typeof(AugHelper), "OnAugLeftAlive")]
	internal static class AugHuntEscapePatch
	{
		private static void Prefix()
		{
			AugHuntContract.MarkCurrentAugEscaped();
		}
	}
	[HarmonyPatch(typeof(Shotgun), "Unlock")]
	internal static class AugHuntShotgunUnlockPatch
	{
		private static void Postfix()
		{
			AugHuntContract.OnShotgunStateChanged();
		}
	}
	[HarmonyPatch(typeof(Shotgun), "LockShotgun")]
	internal static class AugHuntShotgunLockPatch
	{
		private static void Postfix()
		{
			AugHuntContract.OnShotgunStateChanged();
		}
	}
	[HarmonyPatch(typeof(__c__DisplayClass21_0), "_OnEndDayButtonClick_b__0")]
	internal static class AugHuntSleepPatch
	{
		private static bool Prefix()
		{
			PlayerStore store = PlayerStore.Instance;
			if (store == null)
			{
				return true;
			}
			if (!AugHuntContract.ResolveSleep((string reason) =>
			{
				store.ExecuteGameOver(reason);
			}))
			{
				return true;
			}
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Aug hunt contract retaliation triggered the native 'aug' game over.");
			}
			return false;
		}
	}
	internal static class BayExpansionPerk
	{
		internal readonly struct WindowMetrics
		{
			internal int WindowWidth { get; }

			internal int WindowHeight { get; }

			internal int ChildWidth { get; }

			internal int ChildHeight { get; }

			internal WindowMetrics(int windowWidth, int windowHeight, int childWidth, int childHeight)
			{
				WindowWidth = windowWidth;
				WindowHeight = windowHeight;
				ChildWidth = childWidth;
				ChildHeight = childHeight;
			}
		}

		internal readonly struct WindowLayoutSnapshot
		{
			internal bool Tracked { get; }

			internal int Width { get; }

			internal int Height { get; }

			internal Vector2 Position
			{
				[CompilerGenerated]
				get
				{
					//IL_0001: Unknown result type (might be due to invalid IL or missing references)
					return field;
				}
			}

			internal WindowLayoutSnapshot(bool tracked, int width, int height, Vector2 position)
			{
				//IL_0016: Unknown result type (might be due to invalid IL or missing references)
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				Tracked = tracked;
				Width = width;
				Height = height;
				Position = position;
			}
		}

		internal const string UpgradeLevelTag = "XIAOWO_BAY_EXPANSION_LEVEL";

		internal const string UnlockedColumnsTag = "XIAOWO_BAY_UNLOCKED_COLUMNS";

		internal const string UnlockedRowsTag = "XIAOWO_BAY_UNLOCKED_ROWS";

		internal const string BayLayoutVersionTag = "XIAOWO_BAY_LAYOUT_VERSION";

		private const int BayLayoutVersion = 2;

		internal const string CageSizeTag = "CAGE_SIZE_TAG";

		internal const string CageMaxHealthTag = "CAGE_MAX_HEALTH_TAG";

		internal const string CageHealthTag = "CAGE_HEALTH_TAG";

		private const string CageLayoutVersionTag = "XIAOWO_CAGE_LAYOUT_VERSION";

		private const int CageLayoutVersion = 1;

		private static readonly Dictionary<string, (int Width, int Height)[]> LegacyShapes = new Dictionary<string, (int, int)[]>(StringComparer.Ordinal)
		{
			["machine_bay"] = new (int, int)[4]
			{
				(6, 8),
				(14, 8),
				(22, 8),
				(30, 8)
			},
			["storage_bay"] = new (int, int)[4]
			{
				(10, 10),
				(17, 17),
				(23, 23),
				(30, 30)
			},
			["machine_bay_ext"] = new (int, int)[7]
			{
				(11, 14),
				(18, 16),
				(25, 18),
				(32, 20),
				(40, 23),
				(47, 25),
				(55, 28)
			},
			["storage_bay_large"] = new (int, int)[7]
			{
				(17, 10),
				(23, 13),
				(28, 17),
				(34, 20),
				(40, 23),
				(45, 27),
				(51, 30)
			},
			["smuggler_bay"] = new (int, int)[3]
			{
				(5, 5),
				(8, 8),
				(10, 10)
			},
			["smuggler_bay_mod"] = new (int, int)[6]
			{
				(8, 8),
				(11, 11),
				(14, 14),
				(16, 16),
				(18, 18),
				(20, 20)
			}
		};

		private static readonly int[] CageSizes = new int[8] { 3, 4, 5, 6, 7, 8, 12, 16 };

		private const string CageWindowKey = "__animal_cage__";

		private static readonly Dictionary<string, WindowMetrics> InitialWindowMetrics = new Dictionary<string, WindowMetrics>(StringComparer.Ordinal);

		[ThreadStatic]
		private static bool _resolvingInitialWindow;

		[ThreadStatic]
		private static bool _applyingWindowLayout;

		[ThreadStatic]
		private static IntPtr _layoutInventory;

		[ThreadStatic]
		private static IntPtr _layoutWindow;

		[ThreadStatic]
		private static int _layoutWindowWidth;

		[ThreadStatic]
		private static int _layoutWindowHeight;

		[ThreadStatic]
		private static int _layoutChildWidth;

		[ThreadStatic]
		private static int _layoutChildHeight;

		[ThreadStatic]
		private static bool _restoringVisibleShape;

		[ThreadStatic]
		private static bool _restoringCageWindow;

		[ThreadStatic]
		private static bool _upgradingBay;

		internal static long GridResizeLayoutPassCount { get; private set; }

		internal static bool IsSupportedBay(GameItem item)
		{
			if (item != null && LegacyShapes.ContainsKey(CleanId(item.identifier)))
			{
				return !IsBrewingStorageClone(item);
			}
			return false;
		}

		internal static bool IsBrewingStorageClone(GameItem item)
		{
			if (item == null)
			{
				return false;
			}
			string text = CleanId(item.spritePath);
			if (text == "be_brewing_manual" || text == "be_compact_smuggler_bay" || text.StartsWith("be_multi_storage", StringComparison.Ordinal))
			{
				return true;
			}
			if (item.state == null)
			{
				return false;
			}
			if (!item.IsTag("BE_BREWING_MANUAL") && !item.IsTag("BE_COMPACT_SMUGGLER_BAY"))
			{
				return item.IsTag("BE_MULTI_STORAGE");
			}
			return true;
		}

		internal static bool IsMetalIngot(GameItem item)
		{
			if (item != null)
			{
				return string.Equals(CleanId(item.identifier), "metal_ingot", StringComparison.Ordinal);
			}
			return false;
		}

		internal static int MaximumLevel(string identifier)
		{
			if (!LegacyShapes.TryGetValue(CleanId(identifier), out (int, int)[] value))
			{
				return 0;
			}
			(int, int) tuple = value[0];
			(int, int) tuple2 = value[^1];
			return Math.Max(tuple2.Item1 - tuple.Item1, tuple2.Item2 - tuple.Item2);
		}

		internal static bool TryGetShape(string identifier, int level, out int width, out int height)
		{
			width = 0;
			height = 0;
			if (!LegacyShapes.TryGetValue(CleanId(identifier), out (int, int)[] value) || level < 0 || level > MaximumLevel(identifier))
			{
				return false;
			}
			(int, int) tuple = value[0];
			(int, int) tuple2 = value[^1];
			width = Math.Min(tuple2.Item1, tuple.Item1 + level);
			height = Math.Min(tuple2.Item2, tuple.Item2 + level);
			return true;
		}

		internal static bool TryGetUnlockedShape(GameItem item, out int width, out int height)
		{
			width = 0;
			height = 0;
			if (!IsSupportedBay(item))
			{
				return false;
			}
			int bayLevel = GetBayLevel(item);
			if (!TryGetShape(item.identifier, Math.Min(bayLevel, MaximumLevel(item.identifier)), out width, out height))
			{
				return false;
			}
			if (GetTagInt(item, "XIAOWO_BAY_LAYOUT_VERSION", 0) < 2)
			{
				return true;
			}
			string key = CleanId(item.identifier);
			(int, int) tuple = LegacyShapes[key][0];
			(int, int) tuple2 = LegacyShapes[key][^1];
			width = Math.Max(tuple.Item1, Math.Min(tuple2.Item1, GetTagInt(item, "XIAOWO_BAY_UNLOCKED_COLUMNS", width)));
			height = Math.Max(tuple.Item2, Math.Min(tuple2.Item2, GetTagInt(item, "XIAOWO_BAY_UNLOCKED_ROWS", height)));
			return true;
		}

		internal static int NextCageSize(int currentSize)
		{
			for (int i = 0; i < CageSizes.Length - 1; i++)
			{
				if (CageSizes[i] == currentSize)
				{
					return CageSizes[i + 1];
				}
			}
			return currentSize;
		}

		internal static bool TryGetCageShape(int size, out int width, out int height)
		{
			width = 0;
			height = 0;
			if (size >= 3 && size <= 8)
			{
				width = 10;
				height = size;
				return true;
			}
			switch (size)
			{
			case 12:
				width = 15;
				height = 12;
				return true;
			case 16:
				width = 20;
				height = 16;
				return true;
			default:
				return false;
			}
		}

		internal static int RemainingUpgradeCount(GameItem item)
		{
			if (item == null || !PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion"))
			{
				return -1;
			}
			if (FridgeExpansion.IsFridge(item))
			{
				return FridgeExpansion.Remaining(item);
			}
			if (WineRackExpansion.IsWineRack(item))
			{
				return WineRackExpansion.Remaining(item);
			}
			if (IsSupportedBay(item))
			{
				return Math.Max(0, MaximumLevel(item.identifier) - GetBayLevel(item));
			}
			if (!item.IsTag("CAGE_SIZE_TAG"))
			{
				return -1;
			}
			int tagInt = GetTagInt(item, "CAGE_SIZE_TAG", CageSizes[0]);
			for (int i = 0; i < CageSizes.Length; i++)
			{
				if (CageSizes[i] == tagInt)
				{
					return CageSizes.Length - 1 - i;
				}
			}
			if (tagInt < CageSizes[^1])
			{
				return -1;
			}
			return 0;
		}

		internal static string UpgradeStatusText(GameItem item)
		{
			if (FridgeExpansion.IsFridge(item))
			{
				return FridgeExpansion.Status(item);
			}
			if (WineRackExpansion.IsWineRack(item))
			{
				return WineRackExpansion.Status(item);
			}
			int num = RemainingUpgradeCount(item);
			if (num < 0)
			{
				return string.Empty;
			}
			if (!PrecisionBayMode.IsUpgradeEnabled())
			{
				return ModLocalization.Select("精工扩舱已关闭", "Precision Bay Expansion disabled");
			}
			if (num != 0)
			{
				return ModLocalization.Select("剩余升级次数：", "Upgrades remaining: ") + num;
			}
			return ModLocalization.Select("最大等级", "Maximum level");
		}

		internal static void AppendUpgradeTooltip(GameItem item, RichTextBuilder tooltip)
		{
			string text = UpgradeStatusText(item);
			if (text.Length > 0 && tooltip != null)
			{
				tooltip.AddLine(text, true, (ColorPalette)(-1), false, false, false, false, (ColorPalette)(-1), (ColorPalette)(-1), (ColorPalette)(-1));
			}
		}

		internal static bool CanUseIngot(GameItem target)
		{
			if (target == null || !PrecisionBayMode.IsUpgradeEnabled())
			{
				return false;
			}
			if (IsSupportedBay(target))
			{
				return GetBayLevel(target) < MaximumLevel(target.identifier);
			}
			if (!target.IsTag("CAGE_SIZE_TAG"))
			{
				return false;
			}
			int tagInt = GetTagInt(target, "CAGE_SIZE_TAG", 0);
			if (tagInt >= 8)
			{
				return NextCageSize(tagInt) > tagInt;
			}
			return false;
		}

		internal static bool CanTargetWithIngot(GameItem source, GameItem target)
		{
			if (!IsMetalIngot(source) || target == null || !PrecisionBayMode.IsUpgradeEnabled())
			{
				return false;
			}
			if (FridgeExpansion.IsFridge(target))
			{
				return FridgeExpansion.CanUpgrade(target);
			}
			if (WineRackExpansion.IsWineRack(target))
			{
				return WineRackExpansion.CanUpgrade(target);
			}
			if (!IsSupportedBay(target))
			{
				return CanUseIngot(target);
			}
			if (GetBayLevel(target) < MaximumLevel(target.identifier))
			{
				return true;
			}
			return HasAvailableBaySlot(source, target);
		}

		private static bool HasAvailableBaySlot(GameItem source, GameItem target)
		{
			GameGridInventory bayInventory = GetBayInventory(target);
			if (bayInventory != null)
			{
				return bayInventory.TryFindOneValidInventorySlot(source, false) != null;
			}
			return false;
		}

		internal static bool TryHandleIngot(GameItem source, GameItem target)
		{
			if (target == null || !PrecisionBayMode.IsUpgradeEnabled())
			{
				return false;
			}
			string details;
			if (IsMetalIngot(source) && FridgeExpansion.IsFridge(target))
			{
				if (!FridgeExpansion.CanUpgrade(target))
				{
					return false;
				}
				FridgeExpansion.TryUpgrade(target, source, out details);
				return true;
			}
			if (IsMetalIngot(source) && WineRackExpansion.IsWineRack(target))
			{
				if (WineRackExpansion.CanUpgrade(target))
				{
					WineRackExpansion.TryUpgrade(target, source, out details);
				}
				return true;
			}
			if (IsSupportedBay(target))
			{
				if (GetBayLevel(target) >= MaximumLevel(target.identifier))
				{
					GameGridInventory bayInventory = GetBayInventory(target);
					if (source != null && bayInventory != null && bayInventory.TryFindOneValidInventorySlot(source, false) != null)
					{
						return TryTransferToInventory(source, (GameInventory)(object)bayInventory);
					}
					return true;
				}
				TryUpgradeBay(target, source, out details);
				return true;
			}
			if (!target.IsTag("CAGE_SIZE_TAG"))
			{
				return false;
			}
			int tagInt = GetTagInt(target, "CAGE_SIZE_TAG", 0);
			if (tagInt < 8)
			{
				return false;
			}
			if (NextCageSize(tagInt) <= tagInt)
			{
				return true;
			}
			TryUpgradeCage(target, source, out details);
			return true;
		}

		internal static bool TryUpgradeBay(GameItem target, GameItem ingot, out string details)
		{
			details = string.Empty;
			if (!IsSupportedBay(target))
			{
				details = "unsupported bay";
				return false;
			}
			int bayLevel = GetBayLevel(target);
			int num = MaximumLevel(target.identifier);
			if (bayLevel >= num)
			{
				details = "maximum level";
				return false;
			}
			GameGridInventory bayInventory = GetBayInventory(target);
			if (bayInventory == null || bayInventory.inventoryShape == null)
			{
				details = "inventory unavailable";
				return false;
			}
			if (!TryGetUnlockedShape(target, out var width, out var height))
			{
				details = "unlocked shape unavailable";
				return false;
			}
			(int Width, int Height)[] array = LegacyShapes[CleanId(target.identifier)];
			int num2 = Math.Min(array[^1].Width, width + 1);
			int num3 = Math.Min(array[^1].Height, height + 1);
			int width2 = bayInventory.inventoryShape.width;
			int height2 = bayInventory.inventoryShape.height;
			int level = bayLevel;
			List<(IntPtr, int, int)> list = CaptureItems(bayInventory);
			EnsureInitialWindowMetrics(target, bayInventory, cage: false);
			WindowLayoutSnapshot snapshot = CaptureUpgradeWindowLayout(target);
			try
			{
				_upgradingBay = true;
				SetBayShapeState(target, bayLevel + 1, num2, num3);
				if (!ApplyStoredShape(target) || !ItemsMatch(bayInventory, list))
				{
					throw new InvalidOperationException("inventory contents changed while resizing");
				}
				target.ValidateShapeState();
				ApplyResizableWindow(target, bayInventory, cage: false);
				RestoreUpgradeWindowLayout(target, bayInventory, snapshot);
				if (bayInventory.inventoryShape.width != num2 || bayInventory.inventoryShape.height != num3 || !ItemsMatch(bayInventory, list))
				{
					throw new InvalidOperationException("unlocked shape or inventory contents changed during validation");
				}
				RememberGridShape(target, num2, num3);
				if (ingot != null)
				{
					ingot.Destroy();
				}
				details = CleanId(target.identifier) + " level=" + (bayLevel + 1) + " shape=" + bayInventory.inventoryShape.width + "x" + bayInventory.inventoryShape.height + " items=" + list.Count;
				return true;
			}
			catch (Exception ex)
			{
				SetBayShapeState(target, level, width, height);
				bayInventory.SetShape(width2, height2);
				PixelWindow contentWindow = target.contentWindow;
				if (contentWindow != null)
				{
					contentWindow.Validate();
				}
				ApplyResizableWindow(target, bayInventory, cage: false);
				RestoreUpgradeWindowLayout(target, bayInventory, snapshot);
				details = ex.GetType().Name + ": " + ex.Message;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("Bay expansion rolled back: " + details);
				}
				return false;
			}
			finally
			{
				_upgradingBay = false;
			}
		}

		internal static bool TryUpgradeCage(GameItem cage, GameItem ingot, out string details)
		{
			details = string.Empty;
			GameGridInventory val = ((cage == null) ? null : AnimalCage.GetCageInventory(cage));
			int tagInt = GetTagInt(cage, "CAGE_SIZE_TAG", 0);
			int num = NextCageSize(tagInt);
			if (val == null || num <= tagInt)
			{
				details = "cage cannot be expanded";
				return false;
			}
			List<(IntPtr, int, int)> list = CaptureItems(val);
			int width = val.inventoryShape.width;
			int height = val.inventoryShape.height;
			int tagInt2 = GetTagInt(cage, "CAGE_MAX_HEALTH_TAG", 0);
			int tagInt3 = GetTagInt(cage, "CAGE_HEALTH_TAG", 0);
			int tagInt4 = GetTagInt(cage, "XIAOWO_CAGE_LAYOUT_VERSION", 0);
			long unitValue = cage.unitValue;
			EnsureInitialWindowMetrics(cage, val, cage: true);
			WindowLayoutSnapshot snapshot = CaptureUpgradeWindowLayout(cage);
			try
			{
				SetTagInt(cage, "CAGE_SIZE_TAG", num);
				SetTagInt(cage, "CAGE_MAX_HEALTH_TAG", tagInt2 + 20);
				SetTagInt(cage, "CAGE_HEALTH_TAG", Math.Min(tagInt3 + 20, tagInt2 + 20));
				SetTagInt(cage, "XIAOWO_CAGE_LAYOUT_VERSION", 1);
				cage.unitValue = unitValue + 40;
				if (!TryGetCageShape(num, out var width2, out var height2))
				{
					throw new InvalidOperationException("cage shape unavailable");
				}
				val.SetShape(width2, height2);
				PixelWindow contentWindow = cage.contentWindow;
				if (contentWindow != null)
				{
					contentWindow.Validate();
				}
				if (!ItemsMatch(val, list))
				{
					throw new InvalidOperationException("cage contents changed while resizing");
				}
				cage.ValidateShapeState();
				ApplyResizableWindow(cage, val, cage: true);
				RestoreUpgradeWindowLayout(cage, val, snapshot);
				if (!ItemsMatch(val, list))
				{
					throw new InvalidOperationException("cage contents changed during validation");
				}
				RememberGridShape(cage, val.inventoryShape.width, val.inventoryShape.height);
				if (ingot != null)
				{
					ingot.Destroy();
				}
				details = "cage size=" + width2 + "x" + height2 + " items=" + list.Count;
				return true;
			}
			catch (Exception ex)
			{
				SetTagInt(cage, "CAGE_SIZE_TAG", tagInt);
				SetTagInt(cage, "CAGE_MAX_HEALTH_TAG", tagInt2);
				SetTagInt(cage, "CAGE_HEALTH_TAG", tagInt3);
				SetTagInt(cage, "XIAOWO_CAGE_LAYOUT_VERSION", tagInt4);
				cage.unitValue = unitValue;
				val.SetShape(width, height);
				PixelWindow contentWindow2 = cage.contentWindow;
				if (contentWindow2 != null)
				{
					contentWindow2.Validate();
				}
				ApplyResizableWindow(cage, val, cage: true);
				RestoreUpgradeWindowLayout(cage, val, snapshot);
				details = ex.GetType().Name + ": " + ex.Message;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("Cage expansion rolled back: " + details);
				}
				return false;
			}
		}

		internal static bool ApplyStoredShape(GameItem item)
		{
			if (!IsSupportedBay(item))
			{
				return false;
			}
			if (!TryGetUnlockedShape(item, out var width, out var height))
			{
				return false;
			}
			GameGridInventory bayInventory = GetBayInventory(item);
			if (((bayInventory != null) ? bayInventory.inventoryShape : null) == null)
			{
				return false;
			}
			if (bayInventory.inventoryShape.width != width || bayInventory.inventoryShape.height != height)
			{
				bayInventory.SetShape(width, height);
				if (bayInventory.inventoryShape.width != width || bayInventory.inventoryShape.height != height)
				{
					bayInventory.SetShape(width, height);
				}
			}
			ApplyResizableWindow(item, bayInventory, cage: false);
			return true;
		}

		internal static void RestoreExpandedItem(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null)
			{
				return;
			}
			if (IsSupportedBay(item))
			{
				if (!TryRestoreRememberedGridShape(item))
				{
					ApplyStoredShape(item);
				}
			}
			else if (item != null && item.IsTag("CAGE_SIZE_TAG") && GetTagInt(item, "CAGE_SIZE_TAG", 0) >= 8 && !TryRestoreRememberedGridShape(item))
			{
				GameGridInventory cageInventory = AnimalCage.GetCageInventory(item);
				if (cageInventory != null)
				{
					ApplyResizableWindow(item, cageInventory, cage: true);
				}
			}
		}

		internal static void RestoreLoadedCageWindow(GameItem cage, GameGridInventory inventory)
		{
			if (_restoringCageWindow || cage == null || inventory == null || !PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion") || !cage.IsTag("CAGE_SIZE_TAG") || GetTagInt(cage, "CAGE_SIZE_TAG", 0) < 8)
			{
				return;
			}
			try
			{
				_restoringCageWindow = true;
				NormalizeLoadedCageShape(cage, inventory);
				ApplyResizableWindow(cage, inventory, cage: true);
			}
			finally
			{
				_restoringCageWindow = false;
			}
		}

		internal static bool TryGetInitialWindowSize(GameItem item, out int width, out int height)
		{
			width = 0;
			height = 0;
			bool flag = item != null && item.IsTag("CAGE_SIZE_TAG");
			GameGridInventory val = (flag ? AnimalCage.GetCageInventory(item) : GetBayInventory(item));
			if (val == null || !EnsureInitialWindowMetrics(item, val, flag))
			{
				return false;
			}
			string key = (flag ? "__animal_cage__" : CleanId(item.identifier));
			WindowMetrics windowMetrics = InitialWindowMetrics[key];
			width = windowMetrics.WindowWidth;
			height = windowMetrics.WindowHeight;
			return true;
		}

		internal static bool TryGetInitialWindowMetrics(GameItem item, out WindowMetrics metrics)
		{
			metrics = default;
			bool flag = item != null && item.IsTag("CAGE_SIZE_TAG") && !IsSupportedBay(item);
			GameGridInventory val = (flag ? AnimalCage.GetCageInventory(item) : GetBayInventory(item));
			if (val == null || !EnsureInitialWindowMetrics(item, val, flag))
			{
				return false;
			}
			string key = (flag ? "__animal_cage__" : CleanId(item.identifier));
			metrics = InitialWindowMetrics[key];
			return true;
		}

		internal static bool IsResizableContainer(GameItem item)
		{
			if (item == null || !PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion"))
			{
				return false;
			}
			if (IsSupportedBay(item))
			{
				return true;
			}
			if (item.IsTag("CAGE_SIZE_TAG"))
			{
				return GetTagInt(item, "CAGE_SIZE_TAG", 0) >= 8;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		internal static WindowLayoutSnapshot CaptureWindowLayout(GameItem item)
		{
			//IL_0070: Unknown result type (might be due to invalid IL or missing references)
			PixelWindow val = ((item != null) ? item.contentWindow : null);
			RectTransform val2 = ((val != null) ? val.rectTransform : null);
			bool num;
			if (!IsSupportedBay(item))
			{
				if (item == null || !item.IsTag("CAGE_SIZE_TAG"))
				{
					goto IL_007b;
				}
				num = GetTagInt(item, "CAGE_SIZE_TAG", 0) > 8;
			}
			else
			{
				num = GetBayLevel(item) > 0;
			}
			if (num && IsResizableContainer(item) && !((Object)(object)val2 == (Object)null))
			{
				return new WindowLayoutSnapshot(tracked: true, val.widthPixels, val.heightPixels, val2.anchoredPosition);
			}
			goto IL_007b;
			IL_007b:
			return default;
		}

		internal static WindowLayoutSnapshot CaptureUpgradeWindowLayout(GameItem item)
		{
			//IL_0078: Unknown result type (might be due to invalid IL or missing references)
			PixelWindow val = ((item != null) ? item.contentWindow : null);
			RectTransform val2 = ((val != null) ? val.rectTransform : null);
			if ((IsSupportedBay(item) || (item != null && item.IsTag("CAGE_SIZE_TAG") && GetTagInt(item, "CAGE_SIZE_TAG", 0) >= 8)) && IsResizableContainer(item) && !((Object)(object)val2 == (Object)null) && ((Component)val2).gameObject.activeInHierarchy)
			{
				return new WindowLayoutSnapshot(tracked: true, val.widthPixels, val.heightPixels, val2.anchoredPosition);
			}
			return default;
		}

		internal static void RestoreWindowLayout(GameItem item, WindowLayoutSnapshot snapshot, bool rememberSize = false)
		{
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			if (snapshot.Tracked)
			{
				object obj;
				if (item == null)
				{
					obj = null;
				}
				else
				{
					PixelWindow contentWindow = item.contentWindow;
					obj = ((contentWindow != null) ? contentWindow.rectTransform : null);
				}
				if (!((Object)obj == (Object)null))
				{
					TryResizeWindow(item, snapshot.Width, snapshot.Height, rememberSize, out var _, out var _);
					item.contentWindow.rectTransform.anchoredPosition = snapshot.Position;
					item.RememberContentWindowPositionIfMine(item.contentWindow);
				}
			}
		}

		internal static void RestoreUpgradeWindowLayout(GameItem item, GameGridInventory inventory, WindowLayoutSnapshot snapshot)
		{
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			if (snapshot.Tracked && inventory != null && TryGetInitialWindowMetrics(item, out var metrics))
			{
				int val = inventory.widthPixels + (metrics.WindowWidth - metrics.ChildWidth);
				int val2 = inventory.heightPixels + (metrics.WindowHeight - metrics.ChildHeight);
				int requestedWidth = Math.Max(snapshot.Width, val);
				int requestedHeight = Math.Max(snapshot.Height, val2);
				TryResizeWindow(item, requestedWidth, requestedHeight, remember: true, out var _, out var _);
				item.contentWindow.rectTransform.anchoredPosition = snapshot.Position;
				item.RememberContentWindowPositionIfMine(item.contentWindow);
			}
		}

		internal static bool TryResizeWindow(GameItem item, int requestedWidth, int requestedHeight, bool remember, out int actualWidth, out int actualHeight)
		{
			actualWidth = 0;
			actualHeight = 0;
			if (!IsResizableContainer(item) || !TryGetInitialWindowMetrics(item, out var metrics))
			{
				return false;
			}
			PixelWindow contentWindow = item.contentWindow;
			GameGridInventory val = ((!IsSupportedBay(item)) ? AnimalCage.GetCageInventory(item) : GetBayInventory(item));
			if (contentWindow == null || val == null)
			{
				return false;
			}
			actualWidth = Math.Max(metrics.WindowWidth, requestedWidth);
			actualHeight = Math.Max(metrics.WindowHeight, requestedHeight);
			ApplyWindowSize(contentWindow, val, metrics, actualWidth, actualHeight, validate: false);
			actualWidth = contentWindow.widthPixels;
			actualHeight = contentWindow.heightPixels;
			if (remember)
			{
				RememberWindowSize(item, actualWidth, actualHeight);
			}
			return true;
		}

		internal static bool TryResizeGridWindow(GameItem item, int requestedColumns, int requestedRows, bool remember, out int actualWidth, out int actualHeight)
		{
			actualWidth = 0;
			actualHeight = 0;
			if (!TryGetGridShapeLimits(item, out var minimumColumns, out var minimumRows, out var maximumColumns, out var maximumRows) || !TryGetInitialWindowMetrics(item, out var metrics))
			{
				return false;
			}
			GameGridInventory resizableInventory = GetResizableInventory(item);
			PixelWindow val = ((item != null) ? item.contentWindow : null);
			if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) == null || val == null)
			{
				return false;
			}
			int num = Math.Max(minimumColumns, Math.Min(maximumColumns, requestedColumns));
			int num2 = Math.Max(minimumRows, Math.Min(maximumRows, requestedRows));
			int width = resizableInventory.inventoryShape.width;
			int height = resizableInventory.inventoryShape.height;
			if (num == width && num2 == height)
			{
				if (!InventoryBackgroundMatches(resizableInventory))
				{
					ApplyWindowSize(val, resizableInventory, metrics, val.widthPixels, val.heightPixels, validate: true);
				}
				actualWidth = val.widthPixels;
				actualHeight = val.heightPixels;
				return true;
			}
			GridResizeLayoutPassCount++;
			List<(IntPtr, int, int)> expected = CaptureItems(resizableInventory);
			if ((num < width || num2 < height) && !CanContainItems(resizableInventory, num, num2))
			{
				ApplyWindowToCurrentShape(val, resizableInventory, metrics, out actualWidth, out actualHeight);
				return false;
			}
			try
			{
				if (num != width || num2 != height)
				{
					resizableInventory.SetShape(num, num2);
					if (resizableInventory.inventoryShape.width != num || resizableInventory.inventoryShape.height != num2)
					{
						resizableInventory.SetShape(num, num2);
					}
					if (resizableInventory.inventoryShape.width != num || resizableInventory.inventoryShape.height != num2)
					{
						throw new InvalidOperationException("inventory rejected requested visible grid shape");
					}
				}
				if (!ItemsMatch(resizableInventory, expected))
				{
					throw new InvalidOperationException("inventory contents changed while changing visible grid shape");
				}
				resizableInventory.ForceRecomputeDynamicBackground();
				ApplyWindowToCurrentShape(val, resizableInventory, metrics, out actualWidth, out actualHeight);
				if (remember)
				{
					RememberGridShape(item, num, num2);
					RememberWindowSize(item, actualWidth, actualHeight);
				}
				return true;
			}
			catch (Exception ex)
			{
				if (resizableInventory.inventoryShape.width != width || resizableInventory.inventoryShape.height != height)
				{
					resizableInventory.SetShape(width, height);
				}
				ApplyWindowToCurrentShape(val, resizableInventory, metrics, out actualWidth, out actualHeight);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("Visible container grid resize rolled back: " + ex);
				}
				return false;
			}
		}

		internal static bool NeedsShapeRestore(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion"))
			{
				return false;
			}
			if (IsSupportedBay(item))
			{
				return true;
			}
			if (item.IsTag("CAGE_SIZE_TAG"))
			{
				return GetTagInt(item, "CAGE_SIZE_TAG", 0) >= 8;
			}
			return false;
		}

		internal static bool TryGetGridShapeLimits(GameItem item, out int minimumColumns, out int minimumRows, out int maximumColumns, out int maximumRows)
		{
			minimumColumns = 0;
			minimumRows = 0;
			maximumColumns = 0;
			maximumRows = 0;
			if (!IsResizableContainer(item))
			{
				return false;
			}
			if (IsSupportedBay(item))
			{
				if (!TryGetShape(item.identifier, 0, out minimumColumns, out minimumRows))
				{
					return false;
				}
				return TryGetUnlockedShape(item, out maximumColumns, out maximumRows);
			}
			minimumColumns = 10;
			minimumRows = 8;
			return TryGetCageShape(GetTagInt(item, "CAGE_SIZE_TAG", 0), out maximumColumns, out maximumRows);
		}

		internal static GameGridInventory GetResizableInventory(GameItem item)
		{
			if (item == null || !item.IsTag("CAGE_SIZE_TAG") || IsSupportedBay(item))
			{
				return GetBayInventory(item);
			}
			return AnimalCage.GetCageInventory(item);
		}

		private static void NormalizeLoadedCageShape(GameItem cage, GameGridInventory inventory)
		{
			if (cage == null || ((inventory != null) ? inventory.inventoryShape : null) == null || GetTagInt(cage, "XIAOWO_CAGE_LAYOUT_VERSION", 0) >= 1)
			{
				return;
			}
			int tagInt = GetTagInt(cage, "CAGE_SIZE_TAG", 0);
			if (tagInt == 10 || tagInt == 12 || tagInt == 14 || tagInt == 15 || tagInt == 16)
			{
				tagInt = 16;
				SetTagInt(cage, "CAGE_SIZE_TAG", tagInt);
				SetTagInt(cage, "XIAOWO_CAGE_LAYOUT_VERSION", 1);
				if (TryGetCageShape(tagInt, out var width, out var height) && (inventory.inventoryShape.width != width || inventory.inventoryShape.height != height))
				{
					inventory.SetShape(width, height);
				}
			}
		}

		internal static void RememberGridShape(GameItem item, int columns, int rows)
		{
			if (TryGetGridShapeLimits(item, out var minimumColumns, out var minimumRows, out var maximumColumns, out var maximumRows))
			{
				PlayerPrefs.SetInt(WindowSizeKey(item, "Columns"), Math.Max(minimumColumns, Math.Min(maximumColumns, columns)));
				PlayerPrefs.SetInt(WindowSizeKey(item, "Rows"), Math.Max(minimumRows, Math.Min(maximumRows, rows)));
				PlayerPrefs.Save();
			}
		}

		internal static bool TryRestoreRememberedGridShape(GameItem item)
		{
			if (!_upgradingBay && !_restoringVisibleShape)
			{
				object obj;
				if (item == null)
				{
					obj = null;
				}
				else
				{
					PixelWindow contentWindow = item.contentWindow;
					obj = ((contentWindow != null) ? contentWindow.rectTransform : null);
				}
				if (!((Object)obj == (Object)null) && ((Component)item.contentWindow.rectTransform).gameObject.activeInHierarchy)
				{
					string text = WindowSizeKey(item, "Columns");
					string text2 = WindowSizeKey(item, "Rows");
					if (!PlayerPrefs.HasKey(text) || !PlayerPrefs.HasKey(text2) || !TryGetGridShapeLimits(item, out var minimumColumns, out var minimumRows, out var maximumColumns, out var maximumRows))
					{
						return false;
					}
					int requestedColumns = Math.Max(minimumColumns, Math.Min(maximumColumns, PlayerPrefs.GetInt(text, maximumColumns)));
					int requestedRows = Math.Max(minimumRows, Math.Min(maximumRows, PlayerPrefs.GetInt(text2, maximumRows)));
					try
					{
						_restoringVisibleShape = true;
						if (TryResizeGridWindow(item, requestedColumns, requestedRows, remember: false, out var _, out var _))
						{
							return true;
						}
						GameGridInventory resizableInventory = GetResizableInventory(item);
						if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) != null)
						{
							RememberGridShape(item, resizableInventory.inventoryShape.width, resizableInventory.inventoryShape.height);
						}
						return false;
					}
					finally
					{
						_restoringVisibleShape = false;
					}
				}
			}
			return false;
		}

		internal static void RememberWindowSize(GameItem item, int width, int height)
		{
			if (TryGetInitialWindowMetrics(item, out var metrics))
			{
				PlayerPrefs.SetInt(WindowSizeKey(item, "Width"), Math.Max(metrics.WindowWidth, width));
				PlayerPrefs.SetInt(WindowSizeKey(item, "Height"), Math.Max(metrics.WindowHeight, height));
				PlayerPrefs.Save();
			}
		}

		internal static void ForgetWindowSize(GameItem item)
		{
			if (item != null)
			{
				PlayerPrefs.DeleteKey(WindowSizeKey(item, "Width"));
				PlayerPrefs.DeleteKey(WindowSizeKey(item, "Height"));
				PlayerPrefs.DeleteKey(WindowSizeKey(item, "Columns"));
				PlayerPrefs.DeleteKey(WindowSizeKey(item, "Rows"));
				PlayerPrefs.Save();
			}
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

		private static void ApplyResizableWindow(GameItem item, GameGridInventory inventory, bool cage)
		{
			if (!_applyingWindowLayout && ((item != null) ? item.contentWindow : null) != null && inventory != null && EnsureInitialWindowMetrics(item, inventory, cage))
			{
				string key = (cage ? "__animal_cage__" : CleanId(item.identifier));
				WindowMetrics metrics = InitialWindowMetrics[key];
				int widthPixels = inventory.widthPixels;
				int heightPixels = inventory.heightPixels;
				int val = widthPixels + (metrics.WindowWidth - metrics.ChildWidth);
				int val2 = heightPixels + (metrics.WindowHeight - metrics.ChildHeight);
				int width = Math.Max(val, Math.Max(metrics.WindowWidth, PlayerPrefs.GetInt(WindowSizeKey(item, "Width"), metrics.WindowWidth)));
				int height = Math.Max(val2, Math.Max(metrics.WindowHeight, PlayerPrefs.GetInt(WindowSizeKey(item, "Height"), metrics.WindowHeight)));
				ApplyWindowSize(item.contentWindow, inventory, metrics, width, height, validate: true);
				ContainerWindowResizer.Attach(item);
			}
		}

		private static void ApplyWindowSize(PixelWindow window, GameGridInventory inventory, WindowMetrics metrics, int width, int height, bool validate)
		{
			try
			{
				_applyingWindowLayout = true;
				_layoutInventory = ((Il2CppObjectBase)inventory).Pointer;
				_layoutWindow = ((Il2CppObjectBase)window).Pointer;
				_layoutWindowWidth = width;
				_layoutWindowHeight = height;
				_layoutChildWidth = Math.Max(metrics.ChildWidth, width - (metrics.WindowWidth - metrics.ChildWidth));
				_layoutChildHeight = Math.Max(metrics.ChildHeight, height - (metrics.WindowHeight - metrics.ChildHeight));
				window._widthPixels_k__BackingField = width;
				window._heightPixels_k__BackingField = height;
				window.Validate();
				window._widthPixels_k__BackingField = width;
				window._heightPixels_k__BackingField = height;
				inventory.ForceRecomputeDynamicBackground();
				Canvas.ForceUpdateCanvases();
			}
			finally
			{
				_layoutInventory = IntPtr.Zero;
				_layoutWindow = IntPtr.Zero;
				_layoutWindowWidth = 0;
				_layoutWindowHeight = 0;
				_layoutChildWidth = 0;
				_layoutChildHeight = 0;
				_applyingWindowLayout = false;
			}
		}

		private static bool InventoryBackgroundMatches(GameGridInventory inventory)
		{
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_009d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			if (inventory == null)
			{
				return true;
			}
			try
			{
				TreeNodeRender background = inventory.background;
				RectTransform val = ((background != null) ? background.rectTransform : null);
				Rect rect;
				if ((Object)(object)val != (Object)null)
				{
					rect = val.rect;
					if (Approximately(rect.width, inventory.widthPixels))
					{
						rect = val.rect;
						if (Approximately(rect.height, inventory.heightPixels))
						{
							goto IL_005e;
						}
					}
					return false;
				}
				goto IL_005e;
				IL_005e:
				TreeNodeRender insideBorder = inventory.insideBorder;
				RectTransform val2 = ((insideBorder != null) ? insideBorder.rectTransform : null);
				int result;
				if (!((Object)(object)val2 == (Object)null))
				{
					rect = val2.rect;
					if (Approximately(rect.width, (float)inventory.widthPixels + 2f))
					{
						rect = val2.rect;
						result = (Approximately(rect.height, (float)inventory.heightPixels + 2f) ? 1 : 0);
					}
					else
					{
						result = 0;
					}
				}
				else
				{
					result = 1;
				}
				return (byte)result != 0;
			}
			catch
			{
				return false;
			}
		}

		private static bool Approximately(float left, float right)
		{
			return Math.Abs(left - right) < 0.1f;
		}

		private static void ApplyWindowToCurrentShape(PixelWindow window, GameGridInventory inventory, WindowMetrics metrics, out int actualWidth, out int actualHeight)
		{
			int num = metrics.WindowWidth - metrics.ChildWidth;
			int num2 = metrics.WindowHeight - metrics.ChildHeight;
			actualWidth = Math.Max(metrics.WindowWidth, inventory.widthPixels + num);
			actualHeight = Math.Max(metrics.WindowHeight, inventory.heightPixels + num2);
			ApplyWindowSize(window, inventory, metrics, actualWidth, actualHeight, validate: true);
			actualWidth = window.widthPixels;
			actualHeight = window.heightPixels;
		}

		private static bool EnsureInitialWindowMetrics(GameItem item, GameGridInventory inventory, bool cage)
		{
			string text = (cage ? "__animal_cage__" : CleanId((item != null) ? item.identifier : null));
			if (text.Length == 0)
			{
				return false;
			}
			if (InitialWindowMetrics.ContainsKey(text))
			{
				return true;
			}
			if ((cage ? (GetTagInt(item, "CAGE_SIZE_TAG", 0) <= 8) : (GetBayLevel(item) <= 0)) && TryCaptureWindowMetrics(item, inventory, out var metrics))
			{
				InitialWindowMetrics[text] = metrics;
				return true;
			}
			if (_resolvingInitialWindow)
			{
				return false;
			}
			GameItem val = null;
			try
			{
				_resolvingInitialWindow = true;
				val = (cage ? AnimalCage.CreateAnimalCage() : DirectoryMaster.Item(text, true));
				GameGridInventory inventory2 = (cage ? AnimalCage.GetCageInventory(val) : GetBayInventory(val));
				if (!TryCaptureWindowMetrics(val, inventory2, out var metrics2))
				{
					return false;
				}
				InitialWindowMetrics[text] = metrics2;
				return true;
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("Could not resolve initial container window size for " + text + ": " + ex.Message);
				}
				return false;
			}
			finally
			{
				if (val != null)
				{
					val.Destroy();
				}
				_resolvingInitialWindow = false;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
		private static bool TryCaptureWindowMetrics(GameItem item, GameGridInventory inventory, out WindowMetrics metrics)
		{
			metrics = default;
			PixelWindow val = ((item != null) ? item.contentWindow : null);
			if (val == null || inventory == null)
			{
				return false;
			}
			int widthPixels = val.widthPixels;
			int heightPixels = val.heightPixels;
			int widthPixels2 = inventory.widthPixels;
			int heightPixels2 = inventory.heightPixels;
			if (widthPixels <= 0 || heightPixels <= 0 || widthPixels2 <= 0 || heightPixels2 <= 0)
			{
				return false;
			}
			metrics = new WindowMetrics(widthPixels, heightPixels, widthPixels2, heightPixels2);
			return true;
		}

		internal static GameGridInventory GetBayInventory(GameItem item)
		{
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Expected Obj, but got Unknown
			object obj;
			if (item == null)
			{
				obj = null;
			}
			else
			{
				PixelWindow contentWindow = item.contentWindow;
				obj = ((contentWindow != null) ? contentWindow.child : null);
			}
			GraphNodeStorage val = (GraphNodeStorage)obj;
			if (val != null)
			{
				return new GameGridInventory(((Il2CppObjectBase)val).Pointer);
			}
			return null;
		}

		private static bool TryTransferToInventory(GameItem item, GameInventory target)
		{
			if (item == null || target == null)
			{
				return true;
			}
			GameInventory parentInventory = item.parentInventory;
			if (parentInventory == null)
			{
				target.UncheckedAccept(item);
				return true;
			}
			if (((Il2CppObjectBase)parentInventory).Pointer == ((Il2CppObjectBase)target).Pointer)
			{
				return true;
			}
			if (!parentInventory.Expel(item))
			{
				return true;
			}
			if (target.UncheckedAccept(item))
			{
				return true;
			}
			if (!parentInventory.UncheckedAccept(item))
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("Failed to return a capped upgrade ingot to its source inventory.");
				}
			}
			return true;
		}

		internal static int GetBayLevel(GameItem item)
		{
			int num = Math.Max(0, GetTagInt(item, "XIAOWO_BAY_EXPANSION_LEVEL", 0));
			if (num == 0 || !IsSupportedBay(item) || GetTagInt(item, "XIAOWO_BAY_LAYOUT_VERSION", 0) >= 2)
			{
				return num;
			}
			(int, int)[] array = LegacyShapes[CleanId(item.identifier)];
			(int, int) tuple = array[Math.Min(num, array.Length - 1)];
			GameGridInventory bayInventory = GetBayInventory(item);
			(int, int) tuple2 = array[^1];
			var (val, _) = tuple2;
			var (val2, _) = tuple;
			int? num2;
			if (bayInventory == null)
			{
				num2 = null;
			}
			else
			{
				GridShape inventoryShape = bayInventory.inventoryShape;
				num2 = ((inventoryShape != null) ? new int?(inventoryShape.width) : ((int?)null));
			}
			int? num3 = num2;
			int num4 = Math.Min(val, Math.Max(val2, num3.GetValueOrDefault()));
			int item2 = tuple2.Item2;
			int item3 = tuple.Item2;
			int? num5;
			if (bayInventory == null)
			{
				num5 = null;
			}
			else
			{
				GridShape inventoryShape2 = bayInventory.inventoryShape;
				num5 = ((inventoryShape2 != null) ? new int?(inventoryShape2.height) : ((int?)null));
			}
			num3 = num5;
			int num6 = Math.Min(item2, Math.Max(item3, num3.GetValueOrDefault()));
			int num7 = Math.Max(num4 - array[0].Item1, num6 - array[0].Item2);
			SetBayShapeState(item, num7, num4, num6);
			return num7;
		}

		private static void SetBayShapeState(GameItem item, int level, int width, int height)
		{
			SetTagInt(item, "XIAOWO_BAY_UNLOCKED_COLUMNS", width);
			SetTagInt(item, "XIAOWO_BAY_UNLOCKED_ROWS", height);
			SetTagInt(item, "XIAOWO_BAY_EXPANSION_LEVEL", level);
			SetTagInt(item, "XIAOWO_BAY_LAYOUT_VERSION", 2);
		}

		private static int GetTagInt(GameItem item, string tag, int fallback)
		{
			try
			{
				TagState val = ((item != null) ? item.GetTagReadonly(tag) : null);
				return (val == null) ? fallback : val.GetInt();
			}
			catch
			{
				return fallback;
			}
		}

		private static void SetTagInt(GameItem item, string tag, int value)
		{
			if (((item != null) ? item.state : null) == null)
			{
				throw new InvalidOperationException("item state unavailable");
			}
			if (!item.IsTag(tag))
			{
				item.state.EnableTag(tag);
			}
			(item.state.GetTag(tag) ?? throw new InvalidOperationException("tag unavailable: " + tag)).SetInt(value);
		}

		private static List<(IntPtr Pointer, int UniqueId, int UnitCount)> CaptureItems(GameGridInventory inventory)
		{
			List<(IntPtr, int, int)> list = new List<(IntPtr, int, int)>();
			if (((inventory != null) ? inventory.items : null) == null)
			{
				return list;
			}
			for (int i = 0; i < inventory.items.Count; i++)
			{
				GameItem val = inventory.items[i];
				if (val != null)
				{
					list.Add((((Il2CppObjectBase)val).Pointer, val.uniqueId, val.unitCount));
				}
			}
			return list;
		}

		private static bool ItemsMatch(GameGridInventory inventory, IReadOnlyList<(IntPtr Pointer, int UniqueId, int UnitCount)> expected)
		{
			if (((inventory != null) ? inventory.items : null) == null || inventory.items.Count != expected.Count)
			{
				return false;
			}
			for (int i = 0; i < expected.Count; i++)
			{
				GameItem val = inventory.items[i];
				if (val == null || ((Il2CppObjectBase)val).Pointer != expected[i].Pointer || val.uniqueId != expected[i].UniqueId || val.unitCount != expected[i].UnitCount)
				{
					return false;
				}
			}
			return true;
		}

		private static bool CanContainItems(GameGridInventory inventory, int columns, int rows)
		{
			if (((inventory != null) ? inventory.items : null) == null)
			{
				return true;
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

		private static string CleanId(string id)
		{
			if (!string.IsNullOrEmpty(id))
			{
				return id.Replace("\0", string.Empty).Trim();
			}
			return string.Empty;
		}

		private static string WindowSizeKey(GameItem item, string dimension)
		{
			return "XIAOWOTradePerks.ContainerWindow." + CleanId((item != null) ? item.identifier : null) + "." + ((item != null) ? item.uniqueId : 0) + "." + dimension;
		}
	}
	[HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic")]
	internal static class BayExpansionTooltipPatch
	{
		private static void Postfix(GameItemElement __instance, RichTextBuilder __result)
		{
			BayExpansionPerk.AppendUpgradeTooltip((GameItem)(object)__instance, __result);
		}
	}
	[HarmonyPatch(typeof(AnimalCage), "GetCageInventory")]
	internal static class BayExpansionLoadedCageWindowPatch
	{
		private static void Postfix(GameItem cage, GameGridInventory __result)
		{
			BayExpansionPerk.RestoreLoadedCageWindow(cage, __result);
		}
	}
	[HarmonyPatch(typeof(MaterialDirectory), "CanIngotTarget")]
	internal static class BayExpansionIngotTargetPatch
	{
		private static void Postfix(GameItem source, GameItem target, ref bool __result)
		{
			if (!__result && BayExpansionPerk.IsMetalIngot(source) && BayExpansionPerk.CanTargetWithIngot(source, target))
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(MaterialDirectory), "OnIngotTarget")]
	internal static class BayExpansionIngotUsePatch
	{
		private static bool Prefix(GameItem source, GameItem target)
		{
			return !BayExpansionPerk.TryHandleIngot(source, target);
		}
	}
	[HarmonyPatch(typeof(GameItem), "MayTarget")]
	internal static class BayExpansionMayTargetPatch
	{
		private static void Postfix(GameItem __instance, GameItem targetItem, ref bool __result)
		{
			if (!__result && BayExpansionPerk.IsMetalIngot(__instance) && BayExpansionPerk.CanTargetWithIngot(__instance, targetItem))
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(GameItem), "CanTarget")]
	internal static class BayExpansionCanTargetPatch
	{
		private static void Postfix(GameItem __instance, GameItem targetItem, ref bool __result)
		{
			if (!__result && BayExpansionPerk.IsMetalIngot(__instance) && BayExpansionPerk.CanTargetWithIngot(__instance, targetItem))
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(GameItem), "Target")]
	internal static class BayExpansionTargetPatch
	{
		private static bool Prefix(GameItem __instance, GameItem targetItem)
		{
			if (BayExpansionPerk.IsMetalIngot(__instance))
			{
				return !BayExpansionPerk.TryHandleIngot(__instance, targetItem);
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(GameItem), "ValidateShapeState")]
	internal static class BayExpansionRestorePatch
	{
		private readonly struct RestoreState
		{
			internal bool Required { get; }

			internal BayExpansionPerk.WindowLayoutSnapshot Layout { get; }

			internal RestoreState(bool required, BayExpansionPerk.WindowLayoutSnapshot layout)
			{
				Required = required;
				Layout = layout;
			}
		}

		[ThreadStatic]
		private static bool _restoring;

		private static void Prefix(GameItem __instance, out RestoreState __state)
		{
			bool flag = !_restoring && BayExpansionPerk.NeedsShapeRestore(__instance);
			__state = (flag ? new RestoreState(required: true, BayExpansionPerk.CaptureWindowLayout(__instance)) : default(RestoreState));
		}

		[HarmonyPriority(0)]
		private static void Postfix(GameItem __instance, RestoreState __state)
		{
			if (_restoring || !__state.Required)
			{
				return;
			}
			try
			{
				_restoring = true;
				BayExpansionPerk.RestoreExpandedItem(__instance);
				BayExpansionPerk.RestoreWindowLayout(__instance, __state.Layout);
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("Failed to restore expanded bay shape: " + ex);
				}
			}
			finally
			{
				_restoring = false;
			}
		}
	}
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	internal static class BayExpansionFixedWindowWidthPatch
	{
		private static void Postfix(GameGridInventory __instance, ref int __result)
		{
			BayExpansionPerk.TryOverrideChildWidth(__instance, ref __result);
		}
	}
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	internal static class BayExpansionFixedWindowHeightPatch
	{
		private static void Postfix(GameGridInventory __instance, ref int __result)
		{
			BayExpansionPerk.TryOverrideChildHeight(__instance, ref __result);
		}
	}
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	internal static class BayExpansionResizableWindowWidthPatch
	{
		private static void Postfix(PixelWindow __instance, ref int __result)
		{
			BayExpansionPerk.TryOverrideWindowWidth(__instance, ref __result);
		}
	}
	[HarmonyPatch(/*Could not decode attribute arguments.*/)]
	internal static class BayExpansionResizableWindowHeightPatch
	{
		private static void Postfix(PixelWindow __instance, ref int __result)
		{
			BayExpansionPerk.TryOverrideWindowHeight(__instance, ref __result);
		}
	}
	internal sealed class CashLedger
	{
		public int Version { get; set; } = 1;

		public int StartDay { get; set; }

		public int Day { get; set; }

		public long LastCash { get; set; }

		public long Income { get; set; }

		public long Expense { get; set; }

		public long TotalIncome { get; set; }

		public long TotalExpense { get; set; }

		public int ReportDay { get; set; } = -1;

		public long ReportIncome { get; set; }

		public long ReportExpense { get; set; }

		public bool PartialFirstDay { get; set; }

		internal static CashLedger Create(int day, long cash, bool partial)
		{
			return new CashLedger
			{
				StartDay = day,
				Day = day,
				LastCash = cash,
				PartialFirstDay = partial
			};
		}

		internal bool Observe(int day, long cash)
		{
			bool flag = false;
			if (day != Day)
			{
				ReportDay = ((day > Day) ? (day - 1) : (-1));
				ReportIncome = ((day == Day + 1) ? Income : 0);
				ReportExpense = ((day == Day + 1) ? Expense : 0);
				long income = (Expense = 0L);
				Income = income;
				Day = day;
				flag = true;
			}
			long num2 = cash - LastCash;
			LastCash = cash;
			checked
			{
				if (num2 > 0)
				{
					Income += num2;
					TotalIncome += num2;
				}
				if (num2 < 0)
				{
					Expense -= num2;
					TotalExpense -= num2;
				}
				if (!flag)
				{
					return num2 != 0;
				}
				return true;
			}
		}

		internal string Serialize()
		{
			return JsonSerializer.Serialize(this);
		}

		internal static CashLedger Parse(string text)
		{
			CashLedger cashLedger = JsonSerializer.Deserialize<CashLedger>(text);
			if (cashLedger == null || cashLedger.Version != 1 || cashLedger.StartDay < 0 || cashLedger.Day < 0 || cashLedger.Income < 0 || cashLedger.Expense < 0 || cashLedger.TotalIncome < cashLedger.Income || cashLedger.TotalExpense < cashLedger.Expense || cashLedger.ReportIncome < 0 || cashLedger.ReportExpense < 0)
			{
				throw new FormatException("Unsupported or invalid cash ledger.");
			}
			return cashLedger;
		}

		internal string Yesterday(bool chinese)
		{
			if (ReportDay < StartDay)
			{
				if (!chinese)
				{
					return "Yesterday: no recorded data (tracking starts when enabled)";
				}
				return "昨日收支：尚无记录（从启用时开始统计）";
			}
			bool num = PartialFirstDay && ReportDay == StartDay;
			string text = (chinese ? "昨日收支" : "Yesterday");
			if (num)
			{
				text += (chinese ? "（启用后）" : " (since enabled)");
			}
			return Format(text, ReportIncome, ReportExpense, chinese);
		}

		internal string Cumulative(bool chinese)
		{
			return Format(chinese ? $"累计收支（自第{StartDay}天起，含今日）" : $"Total (since day {StartDay}, including today)", TotalIncome, TotalExpense, chinese);
		}

		private static string Format(string heading, long income, long expense, bool chinese)
		{
			long num = income - expense;
			string value;
			if (num > 0)
			{
				value = "596C43";
			}
			else
			{
				value = ((num < 0) ? "C66D4A" : "625D4B");
			}
			string value2 = (chinese ? $"{heading}：收入 {N(income)} / 支出 {N(expense)} / 净收入 {N(num)}" : $"{heading}: Income {N(income)} / Expenses {N(expense)} / Net {N(num)}");
			return $"<color=#{value}>{value2}</color>";
			static string N(long num2)
			{
				return num2.ToString("N0", CultureInfo.InvariantCulture);
			}
		}
	}
	internal static class CommunityOverlay
	{
		internal const string RootName = "XIAOWOTradePerksVersionOverlay";

		internal const string FollowButtonName = "XIAOWOTradePerksDouyinButton";

		internal const string FollowButtonFillName = "XIAOWOTradePerksDouyinButtonFill";

		internal const string FollowButtonText = "关注遗忘的小窝";

		internal const string DouyinUrl = "https://www.douyin.com/user/MS4wLjABAAAAdx49FthxYEyfr8TYeWa2AhwI2WSm_mLWTTSgpS9dTBQ";

		internal static readonly string VersionText = "v" + (typeof(Core).Assembly.GetName().Version?.ToString(3) ?? "unknown");

		internal const float ContentWidth = 244f;

		private static GameObject _root;

		private static GameObject _followButton;

		private static Sprite _solidSprite;

		private static UnityAction _openDouyinAction;

		private static int _checkCountdown;

		private static string _lastFailure = string.Empty;

		private static readonly Color FollowFillColor = new Color(41f / 255f, 2f / 15f, 52f / 255f, 1f);

		internal static bool IsVisible
		{
			get
			{
				if (IsAlive((Object)(object)_root))
				{
					return _root.activeInHierarchy;
				}
				return false;
			}
		}

		internal static void Tick()
		{
			if (_checkCountdown-- > 0)
			{
				return;
			}
			_checkCountdown = 30;
			try
			{
				RectTransform val = ResolveMainMenuRoot();
				if (!IsAlive((Object)(object)val))
				{
					if (IsAlive((Object)(object)_root))
					{
						Instance log = Core.Log;
						if (log != null)
						{
							log.Msg("Community text hidden outside the main menu.");
						}
					}
					DestroyRoot();
					return;
				}
				if (IsAlive((Object)(object)_root))
				{
					RectTransform component = _root.GetComponent<RectTransform>();
					if (IsAlive((Object)(object)component) && (Object)(object)((Transform)component).parent == (Object)(object)val)
					{
						EnsureFollowButton(MainMenuUIController.Instance);
						return;
					}
					DestroyRoot();
				}
				Build(val);
				EnsureFollowButton(MainMenuUIController.Instance);
				_lastFailure = string.Empty;
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Msg("Community panel attached to the main menu UI layer.");
				}
			}
			catch (Exception ex)
			{
				DestroyRoot();
				string text = ex.GetType().Name + ": " + ex.Message;
				if (!string.Equals(text, _lastFailure, StringComparison.Ordinal))
				{
					_lastFailure = text;
					Instance log3 = Core.Log;
					if (log3 != null)
					{
						log3.Warning("Community panel could not be created: " + text);
					}
				}
			}
		}

		internal static void Dispose()
		{
			DestroyRoot();
			_lastFailure = string.Empty;
			_checkCountdown = 0;
		}

		internal static bool ValidateDefinition(out string details)
		{
			bool result = VersionText == "v1.9.17";
			details = "text=" + result + ", width=" + 244f + ", followButton=关注遗忘的小窝";
			return result;
		}

		internal static string BuildStatus()
		{
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Invalid comparison between Unknown and I4
			//IL_01ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_028b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0290: Unknown result type (might be due to invalid IL or missing references)
			//IL_02fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0301: Unknown result type (might be due to invalid IL or missing references)
			//IL_031f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0341: Unknown result type (might be due to invalid IL or missing references)
			ValidateDefinition(out var details);
			if (!IsAlive((Object)(object)_root))
			{
				return "COMMUNITY\nvisible=False\n" + details;
			}
			RectTransform component = _root.GetComponent<RectTransform>();
			TextMeshProUGUI val = FindLabel("CommunityModTitle");
			bool flag = IsPureWhite(val);
			bool flag2 = IsAlive((Object)(object)val) && (((TMP_Text)val).fontStyle & 1) > 0;
			RectTransform val2 = (IsAlive((Object)(object)_followButton) ? _followButton.GetComponent<RectTransform>() : null);
			TextMeshProUGUI val3 = (IsAlive((Object)(object)_followButton) ? _followButton.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			Image val4 = FindFollowIcon();
			Image image = (IsAlive((Object)(object)_followButton) ? _followButton.GetComponent<Image>() : null);
			Transform val5 = (IsAlive((Object)(object)_followButton) ? _followButton.transform.Find("XIAOWOTradePerksDouyinButtonFill") : null);
			Image image2 = (IsAlive((Object)(object)val5) ? ((Component)val5).gameObject.GetComponent<Image>() : null);
			Button val6 = (IsAlive((Object)(object)_followButton) ? _followButton.GetComponent<Button>() : null);
			string[] array = new string[36]
			{
				"COMMUNITY\nvisible=",
				IsVisible.ToString(),
				"\nmainMenu=",
				IsAlive((Object)(object)MainMenuUIController.Instance).ToString(),
				"\ngameplay=",
				(StoreStation.instance != null).ToString(),
				"\nparent=",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			Transform parent = _root.transform.parent;
			array[7] = ((parent != null) ? ((Object)parent).name : null) ?? "none";
			array[8] = "\nposition=";
			array[9] = (IsAlive((Object)(object)component) ? ((object)component.anchoredPosition/*cast due to constrained. prefix*/).ToString() : "missing");
			array[10] = "\nsize=";
			array[11] = (IsAlive((Object)(object)component) ? ((object)component.sizeDelta/*cast due to constrained. prefix*/).ToString() : "missing");
			array[12] = "\nallTextPureWhite=";
			array[13] = flag.ToString();
			array[14] = "\ntitleBoldAndLarger=";
			array[15] = flag2.ToString();
			array[16] = "\nfollowButtonVisible=";
			array[17] = (IsAlive((Object)(object)_followButton) && _followButton.activeInHierarchy).ToString();
			array[18] = "\nfollowButtonText=";
			array[19] = ((val3 != null) ? ((TMP_Text)val3).text : null) ?? "missing";
			array[20] = "\nfollowButtonSize=";
			array[21] = (IsAlive((Object)(object)val2) ? ((object)val2.sizeDelta/*cast due to constrained. prefix*/).ToString() : "missing");
			array[22] = "\nfollowIcon=";
			object obj;
			if (!IsAlive((Object)(object)val4))
			{
				obj = "missing";
			}
			else
			{
				Sprite sprite = val4.sprite;
				obj = ((sprite != null) ? ((Object)sprite).name : null) ?? "missing";
			}
			array[23] = (string)obj;
			array[24] = "\nfollowIconSize=";
			array[25] = (IsAlive((Object)(object)val4) ? ((object)((Graphic)val4).rectTransform.sizeDelta/*cast due to constrained. prefix*/).ToString() : "missing");
			array[26] = "\nfollowFrameBlack=";
			array[27] = IsColor(image, Color.black).ToString();
			array[28] = "\nfollowFillOriginal=";
			array[29] = IsColor(image2, FollowFillColor).ToString();
			array[30] = "\nfollowTextWhite=";
			array[31] = IsPureWhite(val3).ToString();
			array[32] = "\nfollowPersistentListeners=";
			array[33] = (IsAlive((Object)(object)val6) ? ((UnityEventBase)val6.onClick).GetPersistentEventCount() : (-1)).ToString();
			array[34] = "\nfollowUrl=https://www.douyin.com/user/MS4wLjABAAAAdx49FthxYEyfr8TYeWa2AhwI2WSm_mLWTTSgpS9dTBQ\n";
			array[35] = details;
			return string.Concat(array);
		}

		private static TextMeshProUGUI FindLabel(string name)
		{
			Transform val = _root.transform.Find(name);
			if (!IsAlive((Object)(object)val))
			{
				return null;
			}
			return ((Component)val).gameObject.GetComponentInChildren<TextMeshProUGUI>(true);
		}

		private static bool IsPureWhite(TextMeshProUGUI label)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			if (IsAlive((Object)(object)label) && Math.Abs(((Graphic)label).color.r - 1f) < 0.001f && Math.Abs(((Graphic)label).color.g - 1f) < 0.001f && Math.Abs(((Graphic)label).color.b - 1f) < 0.001f)
			{
				return Math.Abs(((Graphic)label).color.a - 1f) < 0.001f;
			}
			return false;
		}

		private static bool IsColor(Image image, Color expected)
		{
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			if (IsAlive((Object)(object)image) && Math.Abs(((Graphic)image).color.r - expected.r) < 0.001f && Math.Abs(((Graphic)image).color.g - expected.g) < 0.001f && Math.Abs(((Graphic)image).color.b - expected.b) < 0.001f)
			{
				return Math.Abs(((Graphic)image).color.a - expected.a) < 0.001f;
			}
			return false;
		}

		private static RectTransform ResolveMainMenuRoot()
		{
			MainMenuUIController instance = MainMenuUIController.Instance;
			if (!IsAlive((Object)(object)instance) || !((Component)instance).gameObject.activeInHierarchy)
			{
				return null;
			}
			if (!IsAlive((Object)(object)instance.moddingButton) || !IsAlive((Object)(object)instance.moddingButton.transform.parent) || !((Component)instance.moddingButton.transform.parent).gameObject.activeInHierarchy)
			{
				return null;
			}
			if (!IsAlive((Object)(object)CustomUIManager.Instance) || CustomUIManager.anchors == null)
			{
				return null;
			}
			CustomUICanvasAnchor val = default;
			if (!CustomUIManager.anchors.TryGetValue("menu", ref val) || !IsAlive((Object)(object)val))
			{
				return null;
			}
			return val.Root;
		}

		private static void Build(RectTransform parent)
		{
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
			//IL_0041: Expected Obj, but got Unknown
			CustomUIManager instance = CustomUIManager.Instance;
			if (!IsAlive((Object)(object)((instance != null) ? instance.labelPrefab : null)))
			{
				throw new InvalidOperationException("Custom UI label prefab is unavailable.");
			}
			_root = new GameObject("XIAOWOTradePerksVersionOverlay", new Type[1] { Il2CppType.Of<RectTransform>() });
			RectTransform component = _root.GetComponent<RectTransform>();
			((Transform)component).SetParent((Transform)(object)parent, false);
			SetTopLeft(component, 10f, -10f, 244f, 24f);
			CreateLabel(instance, component, "CommunityModTitle", "<b>" + VersionText + "</b>", 17f, 0f, 0f, 244f, 24f);
			((Transform)component).SetAsLastSibling();
			_root.SetActive(true);
		}

		private static void EnsureFollowButton(MainMenuUIController menu)
		{
			GameObject val = ((menu != null) ? menu.discordButton : null);
			Transform val2 = (IsAlive((Object)(object)val) ? val.transform.parent : null);
			if (!IsAlive((Object)(object)val) || !IsAlive((Object)(object)val2))
			{
				throw new InvalidOperationException("The original main-menu social button is unavailable.");
			}
			if (IsAlive((Object)(object)_followButton))
			{
				if ((Object)(object)_followButton.transform.parent == (Object)(object)val2)
				{
					ConfigureFollowButton();
					return;
				}
				Object.Destroy((Object)(object)_followButton);
				_followButton = null;
			}
			_followButton = Object.Instantiate<GameObject>(val, val2, false);
			((Object)_followButton).name = "XIAOWOTradePerksDouyinButton";
			_followButton.transform.SetAsLastSibling();
			ConfigureFollowButton();
			_followButton.SetActive(true);
		}

		private static void ConfigureFollowButton()
		{
			//IL_007e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Expected Obj, but got Unknown
			Button val = _followButton.GetComponent<Button>() ?? _followButton.GetComponentInChildren<Button>(true);
			TextMeshProUGUI componentInChildren = _followButton.GetComponentInChildren<TextMeshProUGUI>(true);
			Image val2 = FindFollowIcon();
			Sprite embeddedSprite = IconLoader.GetEmbeddedSprite("tiktok_logo.png", "ui.tiktok", (FilterMode)1);
			if (!IsAlive((Object)(object)val) || !IsAlive((Object)(object)componentInChildren) || !IsAlive((Object)(object)val2) || !IsAlive((Object)(object)embeddedSprite))
			{
				throw new InvalidOperationException("The cloned social button or TikTok icon is incomplete.");
			}
			((Object)embeddedSprite).name = "tiktok_logo";
			((TMP_Text)componentInChildren).text = "关注遗忘的小窝";
			((Graphic)componentInChildren).color = Color.white;
			((Object)((Component)val2).gameObject).name = "TikTok";
			val2.sprite = embeddedSprite;
			((Graphic)val2).color = Color.white;
			val2.preserveAspect = true;
			ConfigureFollowButtonFrame(val);
			val.onClick = new ButtonClickedEvent();
			if (_openDouyinAction == null)
			{
				_openDouyinAction = UnityAction.op_Implicit((Action)OpenDouyin);
			}
			((UnityEvent)val.onClick).AddListener(_openDouyinAction);
		}

		private static void ConfigureFollowButtonFrame(Button button)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			//IL_0085: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Expected Obj, but got Unknown
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			Image component = _followButton.GetComponent<Image>();
			if (!IsAlive((Object)(object)component))
			{
				throw new InvalidOperationException("The cloned social button frame is missing.");
			}
			Sprite sprite = (component.sprite = GetSolidSprite());
			component.type = (Type)0;
			((Graphic)component).color = Color.black;
			((Selectable)button).targetGraphic = (Graphic)(object)component;
			Transform val = _followButton.transform.Find("XIAOWOTradePerksDouyinButtonFill");
			GameObject val2;
			if (IsAlive((Object)(object)val))
			{
				val2 = ((Component)val).gameObject;
			}
			else
			{
				val2 = new GameObject("XIAOWOTradePerksDouyinButtonFill", new Type[2]
				{
					Il2CppType.Of<RectTransform>(),
					Il2CppType.Of<Image>()
				});
				val2.transform.SetParent(_followButton.transform, false);
			}
			RectTransform component2 = val2.GetComponent<RectTransform>();
			Image component3 = val2.GetComponent<Image>();
			if (!IsAlive((Object)(object)component2) || !IsAlive((Object)(object)component3))
			{
				throw new InvalidOperationException("The follow button fill could not be created.");
			}
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.one;
			component2.pivot = new Vector2(0.5f, 0.5f);
			component2.offsetMin = new Vector2(2f, 2f);
			component2.offsetMax = new Vector2(-2f, -2f);
			((Transform)component2).localScale = Vector3.one;
			component3.sprite = sprite;
			component3.type = (Type)0;
			((Graphic)component3).color = FollowFillColor;
			((Graphic)component3).raycastTarget = false;
			val2.transform.SetAsFirstSibling();
			val2.SetActive(true);
		}

		private static Sprite GetSolidSprite()
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			if (IsAlive((Object)(object)_solidSprite))
			{
				return _solidSprite;
			}
			Texture2D whiteTexture = Texture2D.whiteTexture;
			_solidSprite = Sprite.Create(whiteTexture, new Rect(0f, 0f, (float)((Texture)whiteTexture).width, (float)((Texture)whiteTexture).height), new Vector2(0.5f, 0.5f), 100f);
			((Object)_solidSprite).name = "xiaowo_ui_solid";
			((Object)_solidSprite).hideFlags = (HideFlags)61;
			return _solidSprite;
		}

		private static Image FindFollowIcon()
		{
			if (!IsAlive((Object)(object)_followButton))
			{
				return null;
			}
			Transform val = _followButton.transform.Find("TikTok");
			if (IsAlive((Object)(object)val))
			{
				return ((Component)val).gameObject.GetComponent<Image>();
			}
			foreach (Image componentsInChild in _followButton.GetComponentsInChildren<Image>(true))
			{
				if (IsAlive((Object)(object)componentsInChild) && (Object)(object)((Component)componentsInChild).gameObject != (Object)(object)_followButton && ((Object)((Component)componentsInChild).gameObject).name != "XIAOWOTradePerksDouyinButtonFill")
				{
					return componentsInChild;
				}
			}
			return null;
		}

		private static void OpenDouyin()
		{
			try
			{
				MainMenuUIController instance = MainMenuUIController.Instance;
				if (instance != null)
				{
					instance.OnAnyButtonClick();
				}
				Application.OpenURL("https://www.douyin.com/user/MS4wLjABAAAAdx49FthxYEyfr8TYeWa2AhwI2WSm_mLWTTSgpS9dTBQ");
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Opened the Douyin follow page.");
				}
			}
			catch (Exception ex)
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Warning("Could not open the Douyin follow page: " + ex.Message);
				}
			}
		}

		private static void CreateLabel(CustomUIManager manager, RectTransform parent, string name, string text, float fontSize, float x, float y, float width, float height)
		{
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = Object.Instantiate<GameObject>(manager.labelPrefab, (Transform)(object)parent, false);
			((Object)val).name = name;
			RectTransform component = val.GetComponent<RectTransform>();
			TextMeshProUGUI componentInChildren = val.GetComponentInChildren<TextMeshProUGUI>(true);
			if (!IsAlive((Object)(object)component) || !IsAlive((Object)(object)componentInChildren))
			{
				Object.Destroy((Object)(object)val);
				throw new InvalidOperationException("Custom UI label prefab is incomplete.");
			}
			SetTopLeft(component, x, y, width, height);
			((TMP_Text)componentInChildren).text = text;
			((Graphic)componentInChildren).color = Color.white;
			((TMP_Text)componentInChildren).fontSize = fontSize;
			((TMP_Text)componentInChildren).fontStyle = (FontStyles)(text.StartsWith("<b>", StringComparison.Ordinal) ? 1 : 0);
			((TMP_Text)componentInChildren).alignment = (TextAlignmentOptions)257;
			((TMP_Text)componentInChildren).enableWordWrapping = false;
			((TMP_Text)componentInChildren).richText = true;
			((Graphic)componentInChildren).raycastTarget = false;
			val.SetActive(true);
		}

		private static void SetTopLeft(RectTransform rect, float x, float y, float width, float height)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			rect.anchorMin = new Vector2(0f, 1f);
			rect.anchorMax = new Vector2(0f, 1f);
			rect.pivot = new Vector2(0f, 1f);
			rect.anchoredPosition = new Vector2(x, y);
			rect.sizeDelta = new Vector2(width, height);
			((Transform)rect).localScale = Vector3.one;
		}

		private static void DestroyRoot()
		{
			if (IsAlive((Object)(object)_followButton))
			{
				Object.Destroy((Object)(object)_followButton);
			}
			_followButton = null;
			if (IsAlive((Object)(object)_root))
			{
				Object.Destroy((Object)(object)_root);
			}
			_root = null;
			if (IsAlive((Object)(object)_solidSprite))
			{
				Object.Destroy((Object)(object)_solidSprite);
			}
			_solidSprite = null;
		}

		private static bool IsAlive(Object value)
		{
			if (value != (Object)null)
			{
				return Object.op_Implicit(value);
			}
			return false;
		}

		static CommunityOverlay()
		{
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	public readonly struct CompatibilityBrokerStatus
	{
		public int AliasCount { get; }

		public int ItemConflictCount { get; }

		public int UnresolvedConflictCount { get; }

		public int LoadedModCount { get; }

		public int DeduplicatedPatchCount { get; }

		public int DuplicateRepairFailureCount { get; }

		public CompatibilityBrokerStatus(int aliases, int itemConflicts, int unresolved, int loadedMods)
			: this(aliases, itemConflicts, unresolved, loadedMods, 0, 0)
		{
		}

		public CompatibilityBrokerStatus(int aliases, int itemConflicts, int unresolved, int loadedMods, int deduplicatedPatches, int duplicateRepairFailures)
		{
			AliasCount = aliases;
			ItemConflictCount = itemConflicts;
			UnresolvedConflictCount = unresolved;
			LoadedModCount = loadedMods;
			DeduplicatedPatchCount = deduplicatedPatches;
			DuplicateRepairFailureCount = duplicateRepairFailures;
		}
	}
	public static class XIAOWOCompatibility
	{
		public static string ResolveItemId(string modId, string localId)
		{
			return CompatibilityBroker.Resolve(modId, localId);
		}

		public static bool RegisterItemAlias(string modId, string localId, string resolvedId)
		{
			return CompatibilityBroker.RegisterAlias(modId, localId, resolvedId, automatic: false);
		}

		public static CompatibilityBrokerStatus GetStatus()
		{
			return CompatibilityBroker.Status;
		}
	}
	internal static class CompatibilityBroker
	{
		private readonly struct FactoryOwner
		{
			internal string Owner { get; }

			internal Func<GameItem> Factory { get; }

			internal FactoryOwner(string owner, Func<GameItem> factory)
			{
				Owner = owner;
				Factory = factory;
			}
		}

		private sealed class DuplicateRepairResult
		{
			internal int Removed;

			internal int Failures;

			internal readonly HashSet<string> Owners = new HashSet<string>(StringComparer.Ordinal);
		}

		private readonly struct PatchSnapshot
		{
			internal MethodInfo Method { get; }

			internal int Priority { get; }

			internal string[] Before { get; }

			internal string[] After { get; }

			internal bool Debug { get; }

			internal string DebugEmitPath { get; }

			internal bool WrapTryCatch { get; }

			internal string Identity { get; }

			private PatchSnapshot(MethodInfo method, int priority, string[] before, string[] after, bool debug, string debugEmitPath, bool wrapTryCatch)
			{
				Method = method;
				Priority = priority;
				Before = before ?? Array.Empty<string>();
				After = after ?? Array.Empty<string>();
				Debug = debug;
				DebugEmitPath = debugEmitPath;
				WrapTryCatch = wrapTryCatch;
				Identity = MethodIdentity(method) + "|" + priority + "|" + string.Join("\u001f", Before) + "|" + string.Join("\u001f", After) + "|" + debug + "|" + debugEmitPath + "|" + wrapTryCatch;
			}

			internal static PatchSnapshot From(Patch patch)
			{
				return new PatchSnapshot(patch.PatchMethod, patch.priority, patch.before?.ToArray(), patch.after?.ToArray(), patch.debug, patch.debugEmitPath, patch.wrapTryCatch);
			}

			internal HarmonyMethod ToHarmonyMethod()
			{
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_004a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0056: Unknown result type (might be due to invalid IL or missing references)
				//IL_0068: Expected Obj, but got Unknown
				return new HarmonyMethod(Method)
				{
					priority = Priority,
					before = Before.ToArray(),
					after = After.ToArray(),
					debug = Debug,
					debugEmitPath = DebugEmitPath,
					wrapTryCatch = WrapTryCatch
				};
			}

			private static string MethodIdentity(MethodInfo method)
			{
				try
				{
					return method.Module.ModuleVersionId.ToString() + ":" + method.MetadataToken;
				}
				catch
				{
					return method.DeclaringType?.AssemblyQualifiedName + ":" + method;
				}
			}
		}

		private const string GameOwner = "game";

		private const string SelfAssembly = "XIAOWOTradePerks";

		private static readonly object Sync = new object();

		private static readonly Dictionary<string, string> Routes = new Dictionary<string, string>(StringComparer.Ordinal);

		private static readonly HashSet<string> AmbiguousIds = new HashSet<string>(StringComparer.Ordinal);

		private static readonly HashSet<string> ReportedConflicts = new HashSet<string>(StringComparer.Ordinal);

		private static readonly HashSet<string> UnresolvedConflicts = new HashSet<string>(StringComparer.Ordinal);

		private static int _loadedModCount;

		private static int _deduplicatedPatchCount;

		private static int _duplicateRepairFailureCount;

		private static int _lastPatchEntryCount = -1;

		private static RuntimePollGate _scanPoll;

		internal static CompatibilityBrokerStatus Status
		{
			get
			{
				lock (Sync)
				{
					return new CompatibilityBrokerStatus(Routes.Count, ReportedConflicts.Count, UnresolvedConflicts.Count, _loadedModCount, _deduplicatedPatchCount, _duplicateRepairFailureCount);
				}
			}
		}

		internal static void Initialize()
		{
			lock (Sync)
			{
				Routes.Clear();
				AmbiguousIds.Clear();
				ReportedConflicts.Clear();
				UnresolvedConflicts.Clear();
				_deduplicatedPatchCount = 0;
				_duplicateRepairFailureCount = 0;
				_lastPatchEntryCount = -1;
				_scanPoll.Reset();
				RefreshLoadedModCount();
			}
		}

		internal static void Tick()
		{
			if (!_scanPoll.Due(5000))
			{
				return;
			}
			RefreshLoadedModCount();
			if (CountPatchEntries() == _lastPatchEntryCount)
			{
				return;
			}
			DuplicateRepairResult duplicateRepairResult = RepairExactDuplicatePatches();
			_lastPatchEntryCount = CountPatchEntries();
			if (duplicateRepairResult.Removed > 0)
			{
				lock (Sync)
				{
					_deduplicatedPatchCount += duplicateRepairResult.Removed;
				}
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("MOD 兼容总线已自动去重 " + duplicateRepairResult.Removed + " 个完全相同的 Harmony 补丁，来源：" + string.Join(", ", duplicateRepairResult.Owners.OrderBy((string owner) => owner, StringComparer.Ordinal)) + "。");
				}
			}
			if (duplicateRepairResult.Failures > 0)
			{
				lock (Sync)
				{
					_duplicateRepairFailureCount += duplicateRepairResult.Failures;
				}
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Warning("MOD 兼容总线有 " + duplicateRepairResult.Failures + " 组重复补丁无法安全去重，已保持原样并交由兼容报告显示。");
				}
			}
			PatchCompatibility.InvalidateReport();
		}

		internal static void Dispose()
		{
			lock (Sync)
			{
				Routes.Clear();
				AmbiguousIds.Clear();
				ReportedConflicts.Clear();
				UnresolvedConflicts.Clear();
				_loadedModCount = 0;
				_deduplicatedPatchCount = 0;
				_duplicateRepairFailureCount = 0;
				_lastPatchEntryCount = -1;
				_scanPoll.Reset();
			}
		}

		internal static string Resolve(string modId, string localId)
		{
			string text = NormalizeOwner(modId);
			string text2 = CleanId(localId);
			if (text.Length == 0 || text2.Length == 0)
			{
				return text2;
			}
			lock (Sync)
			{
				string value;
				return Routes.TryGetValue(RouteKey(text, text2), out value) ? value : text2;
			}
		}

		internal static bool RegisterAlias(string modId, string localId, string resolvedId, bool automatic)
		{
			string text = NormalizeOwner(modId);
			string text2 = CleanId(localId);
			string text3 = CleanId(resolvedId);
			if (text.Length == 0 || text2.Length == 0 || text3.Length == 0)
			{
				return false;
			}
			lock (Sync)
			{
				string key = RouteKey(text, text2);
				if (Routes.TryGetValue(key, out var value) && !string.Equals(value, text3, StringComparison.Ordinal))
				{
					UnresolvedConflicts.Add(text + "/" + text2);
					return false;
				}
				Routes[key] = text3;
				if (automatic)
				{
					AmbiguousIds.Add(text2);
				}
				return true;
			}
		}

		internal static void RouteItemRequest(ref string identifier)
		{
			string text = CleanId(identifier);
			if (text.Length == 0)
			{
				return;
			}
			lock (Sync)
			{
				if (!AmbiguousIds.Contains(text))
				{
					return;
				}
			}
			string text2 = Resolve(ResolveCallingOwner(), text);
			if (!string.Equals(text2, text, StringComparison.Ordinal))
			{
				identifier = text2;
			}
		}

		internal static string BuildReport()
		{
			CompatibilityBrokerStatus status = Status;
			return "brokerAliases=" + status.AliasCount + ", itemConflicts=" + status.ItemConflictCount + ", unresolvedItems=" + status.UnresolvedConflictCount + ", deduplicatedPatches=" + status.DeduplicatedPatchCount + ", dedupeFailures=" + status.DuplicateRepairFailureCount + ", loadedMods=" + status.LoadedModCount;
		}

		private static DuplicateRepairResult RepairExactDuplicatePatches()
		{
			DuplicateRepairResult result = new DuplicateRepairResult();
			MethodBase[] array = Harmony.GetAllPatchedMethods().ToArray();
			foreach (MethodBase methodBase in array)
			{
				Patches patchInfo = Harmony.GetPatchInfo(methodBase);
				if (patchInfo != null)
				{
					RepairPatchKind(methodBase, (HarmonyPatchType)1, patchInfo.Prefixes, result);
					RepairPatchKind(methodBase, (HarmonyPatchType)2, patchInfo.Postfixes, result);
					RepairPatchKind(methodBase, (HarmonyPatchType)3, patchInfo.Transpilers, result);
					RepairPatchKind(methodBase, (HarmonyPatchType)4, patchInfo.Finalizers, result);
					RepairPatchKind(methodBase, (HarmonyPatchType)6, patchInfo.ILManipulators, result);
				}
			}
			return result;
		}

		private static void RepairPatchKind(MethodBase original, HarmonyPatchType kind, IEnumerable<Patch> patches, DuplicateRepairResult result)
		{
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fa: Expected Obj, but got Unknown
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			foreach (IGrouping<string, Patch> item in patches.Where((Patch patch) => ((patch != null) ? patch.PatchMethod : null) != null && !string.IsNullOrWhiteSpace(patch.owner) && !PatchCompatibility.IsOwnedByMain(patch.owner)).GroupBy((Patch patch) => patch.owner, StringComparer.Ordinal))
			{
				PatchSnapshot[] array = item.Select(PatchSnapshot.From).ToArray();
				PatchSnapshot[] array2 = (from @group in array.GroupBy((PatchSnapshot snapshot) => snapshot.Identity, StringComparer.Ordinal)
					select @group.First()).ToArray();
				int num = array.Length - array2.Length;
				if (num <= 0)
				{
					continue;
				}
				try
				{
					Harmony val = new Harmony(item.Key);
					val.Unpatch(original, kind, item.Key);
					PatchSnapshot[] array3 = array2;
					foreach (PatchSnapshot patchSnapshot in array3)
					{
						ApplyPatch(val, original, kind, patchSnapshot.ToHarmonyMethod());
					}
					result.Removed += num;
					result.Owners.Add(OwnerLabel(item.Key));
				}
				catch (Exception ex)
				{
					result.Failures++;
					bool flag = RestorePatchGroup(original, kind, item.Key, array);
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("无法自动去重 " + OwnerLabel(item.Key) + " 的 " + MethodLabel(original) + " " + ((object)kind/*cast due to constrained. prefix*/).ToString() + " 补丁，" + (flag ? "已恢复去重前状态：" : "恢复原状态也失败，请停用冲突 MOD：") + ex.Message);
					}
				}
			}
		}

		private static bool RestorePatchGroup(MethodBase original, HarmonyPatchType kind, string owner, IEnumerable<PatchSnapshot> snapshots)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0007: Expected Obj, but got Unknown
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				Harmony val = new Harmony(owner);
				val.Unpatch(original, kind, owner);
				foreach (PatchSnapshot snapshot in snapshots)
				{
					ApplyPatch(val, original, kind, snapshot.ToHarmonyMethod());
				}
				return true;
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("兼容总线恢复 " + OwnerLabel(owner) + " 的 " + MethodLabel(original) + " 补丁失败：" + ex);
				}
				return false;
			}
		}

		private static void ApplyPatch(Harmony harmony, MethodBase original, HarmonyPatchType kind, HarmonyMethod patch)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_0004: Invalid comparison between Unknown and I4
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Invalid comparison between Unknown and I4
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Invalid comparison between Unknown and I4
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Invalid comparison between Unknown and I4
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Invalid comparison between Unknown and I4
			harmony.Patch(original, ((int)kind == 1) ? patch : null, ((int)kind == 2) ? patch : null, ((int)kind == 3) ? patch : null, ((int)kind == 4) ? patch : null, ((int)kind == 6) ? patch : null);
		}

		private static int CountPatchEntries()
		{
			int num = 0;
			foreach (MethodBase allPatchedMethod in Harmony.GetAllPatchedMethods())
			{
				Patches patchInfo = Harmony.GetPatchInfo(allPatchedMethod);
				if (patchInfo != null)
				{
					num += patchInfo.Prefixes.Count + patchInfo.Postfixes.Count + patchInfo.Transpilers.Count + patchInfo.Finalizers.Count + patchInfo.ILManipulators.Count;
				}
			}
			return num;
		}

		private static string OwnerLabel(string owner)
		{
			int num = owner?.IndexOf(',') ?? (-1);
			int num2 = owner?.IndexOf(':') ?? (-1);
			int num3 = ((num >= 0 && num2 >= 0) ? Math.Min(num, num2) : Math.Max(num, num2));
			if (num3 <= 0)
			{
				return owner;
			}
			return owner.Substring(0, num3);
		}

		private static string MethodLabel(MethodBase method)
		{
			return (method?.DeclaringType?.Name ?? "<unknown>") + "." + (method?.Name ?? "<unknown>");
		}

		internal static void AuditRegisteredFactories(ModItemDirectory directory)
		{
			if (((Directory<GameItem>)(object)directory)?.factoryDictionary == null)
			{
				return;
			}
			Dictionary<string, List<FactoryOwner>> dictionary = new Dictionary<string, List<FactoryOwner>>(StringComparer.Ordinal);
			foreach (MelonMod item2 in MelonBase.RegisteredMelons.OfType<MelonMod>())
			{
				MelonAssembly melonAssembly = ((MelonBase)item2).MelonAssembly;
				Assembly assembly = ((melonAssembly != null) ? melonAssembly.Assembly : null);
				if (assembly == null || assembly == typeof(Core).Assembly)
				{
					continue;
				}
				MelonInfoAttribute info = ((MelonBase)item2).Info;
				string owner = NormalizeOwner(((info != null) ? info.Name : null) ?? assembly.GetName().Name);
				foreach (Type item3 in SafeTypes(assembly))
				{
					FieldInfo[] fields;
					try
					{
						fields = item3.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
					}
					catch
					{
						continue;
					}
					FieldInfo[] array = fields;
					foreach (FieldInfo fieldInfo in array)
					{
						object value;
						try
						{
							value = fieldInfo.GetValue(null);
						}
						catch
						{
							continue;
						}
						if (!(value is IDictionary dictionary2))
						{
							continue;
						}
						foreach (DictionaryEntry item4 in dictionary2)
						{
							if (!(item4.Key is string identifier) || !(item4.Value is Func<GameItem> factory))
							{
								continue;
							}
							string text = CleanId(identifier);
							if (text.Length != 0)
							{
								if (!dictionary.TryGetValue(text, out var value2))
								{
									value2 = (dictionary[text] = new List<FactoryOwner>());
								}
								if (!value2.Any((FactoryOwner candidate) => string.Equals(candidate.Owner, owner, StringComparison.Ordinal)))
								{
									value2.Add(new FactoryOwner(owner, factory));
								}
							}
						}
					}
				}
			}
			foreach (KeyValuePair<string, List<FactoryOwner>> item5 in dictionary.Where((KeyValuePair<string, List<FactoryOwner>> pair) => pair.Value.Select((FactoryOwner candidate) => candidate.Owner).Distinct(StringComparer.Ordinal).Count() > 1))
			{
				foreach (FactoryOwner item6 in item5.Value)
				{
					string text2 = AliasId(item6.Owner, item5.Key);
					SetFactory((Directory<GameItem>)(object)directory, text2, item6.Factory);
					RegisterAlias(item6.Owner, item5.Key, text2, automatic: true);
				}
				lock (Sync)
				{
					AmbiguousIds.Add(item5.Key);
					string text3 = string.Join(", ", item5.Value.Select((FactoryOwner candidate) => candidate.Owner).Distinct(StringComparer.Ordinal).OrderBy((string result) => result, StringComparer.Ordinal));
					string item = item5.Key + " / " + text3;
					if (ReportedConflicts.Add(item))
					{
						Instance log = Core.Log;
						if (log != null)
						{
							log.Warning("物品 ID 冲突已自动分流：" + item5.Key + "，来源 " + text3 + "；各 MOD调用会返回自己的物品，未知存档格式不会被自动改写。");
						}
					}
				}
			}
		}

		private static IEnumerable<Type> SafeTypes(Assembly assembly)
		{
			try
			{
				return assembly.GetTypes();
			}
			catch (ReflectionTypeLoadException ex)
			{
				return ex.Types.Where((Type type) => type != null);
			}
			catch
			{
				return Array.Empty<Type>();
			}
		}

		private static void SetFactory(Directory<GameItem> directory, string identifier, Func<GameItem> factory)
		{
			directory.Set(identifier, factory);
		}

		private static string ResolveCallingOwner()
		{
			try
			{
				StackFrame[] frames = new StackTrace(fNeedFileInfo: false).GetFrames();
				if (frames == null)
				{
					return "game";
				}
				StackFrame[] array = frames;
				for (int i = 0; i < array.Length; i++)
				{
					MethodBase method = array[i].GetMethod();
					Assembly assembly = method?.DeclaringType?.Assembly;
					if (assembly == null)
					{
						continue;
					}
					string text = assembly.GetName().Name ?? string.Empty;
					if (text.Length == 0 || string.Equals(text, "XIAOWOTradePerks", StringComparison.Ordinal) || text.StartsWith("0Harmony", StringComparison.Ordinal) || text.StartsWith("Harmony", StringComparison.Ordinal) || text.StartsWith("System", StringComparison.Ordinal) || text.StartsWith("Il2Cpp", StringComparison.Ordinal))
					{
						continue;
					}
					MelonAssembly melonAssemblyOfMember = MelonAssembly.GetMelonAssemblyOfMember((MemberInfo)method, (object)null);
					if (melonAssemblyOfMember != null)
					{
						MelonBase? val = melonAssemblyOfMember.LoadedMelons.FirstOrDefault();
						object obj;
						if (val == null)
						{
							obj = null;
						}
						else
						{
							MelonInfoAttribute info = val.Info;
							obj = ((info != null) ? info.Name : null);
						}
						string text2 = (string)obj;
						return NormalizeOwner(string.IsNullOrWhiteSpace(text2) ? text : text2);
					}
					if (string.Equals(text, "Assembly-CSharp", StringComparison.Ordinal))
					{
						return "game";
					}
					if ((assembly.Location ?? string.Empty).IndexOf("\\Mods\\", StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return NormalizeOwner(text);
					}
				}
			}
			catch
			{
			}
			return "game";
		}

		private static void RefreshLoadedModCount()
		{
			try
			{
				_loadedModCount = MelonBase.RegisteredMelons.OfType<MelonMod>().Count();
			}
			catch
			{
				_loadedModCount = 0;
			}
		}

		private static string AliasId(string owner, string identifier)
		{
			return "__xiaowo__" + SafeToken(owner) + "__" + SafeToken(identifier);
		}

		private static string RouteKey(string owner, string identifier)
		{
			return owner + "\n" + identifier;
		}

		private static string NormalizeOwner(string owner)
		{
			if (!string.IsNullOrWhiteSpace(owner))
			{
				return owner.Replace("\0", string.Empty).Trim();
			}
			return string.Empty;
		}

		private static string CleanId(string identifier)
		{
			if (!string.IsNullOrWhiteSpace(identifier))
			{
				return identifier.Replace("\0", string.Empty).Trim();
			}
			return string.Empty;
		}

		private static string SafeToken(string value)
		{
			return new string(value.Select((char ch) =>
			{
				bool flag = char.IsLetterOrDigit(ch);
				if (!flag)
				{
					bool flag2 = ((ch == '-' || ch == '_') ? true : false);
					flag = flag2;
				}
				return (!flag) ? '_' : ch;
			}).ToArray());
		}
	}
	[HarmonyPatch(typeof(DirectoryMaster), "Item", new Type[]
	{
		typeof(string),
		typeof(bool)
	})]
	internal static class CompatibilityBrokerMasterItemPatch
	{
		[HarmonyPrefix]
		[HarmonyPriority(800)]
		private static void Prefix(ref string identifier)
		{
			CompatibilityBroker.RouteItemRequest(ref identifier);
		}
	}
	[HarmonyPatch]
	internal static class CompatibilityBrokerDirectoryCreatePatch
	{
		private static MethodBase TargetMethod()
		{
			return AccessTools.Method(typeof(Directory<GameItem>), "Create", new Type[1] { typeof(string) }, (Type[])null);
		}

		[HarmonyPrefix]
		[HarmonyPriority(800)]
		private static void Prefix(ref string identifier)
		{
			CompatibilityBroker.RouteItemRequest(ref identifier);
		}
	}
	[HarmonyPatch(typeof(ModItemDirectory), "InitDirectory")]
	internal static class CompatibilityBrokerDirectoryAuditPatch
	{
		[HarmonyPostfix]
		[HarmonyPriority(0)]
		private static void Postfix(ModItemDirectory __instance)
		{
			CompatibilityBroker.AuditRegisteredFactories(__instance);
		}
	}
	internal static class ContainerWindowResizer
	{
		[Flags]
		private enum ResizeEdge
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

			private Func<PointerEventData, bool> _maximizeAction;

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

			private bool _maximized;

			private int _restoreWidth;

			private int _restoreHeight;

			private int _restoreColumns;

			private int _restoreRows;

			private Vector2 _restorePosition;

			private bool _wasVisible;

			private bool _pointerDrag;

			internal ResizeEdge ActiveEdge => _activeEdge;

			internal GameItem Item { get; set; }

			internal PixelWindow Window { get; }

			internal bool IsAlive
			{
				get
				{
					try
					{
						PixelWindow window = Window;
						return (Object)(object)((window != null) ? window.rectTransform : null) != (Object)null && _handles.Count == 8 && LiveHandleCount == 8;
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
							if ((Object)(object)((node != null) ? node.rectTransform : null) != (Object)null)
							{
								num++;
							}
						}
						catch
						{
						}
					}
					return num;
				}
			}

			internal bool HasMaximizeHandler
			{
				get
				{
					try
					{
						int result;
						if ((Delegate)(object)_maximizeAction != (Delegate)null)
						{
							PixelWindow window = Window;
							if (window == null)
							{
								result = 0;
							}
							else
							{
								TreeNodeRender topDragBorder = window.topDragBorder;
								bool? flag;
								if (topDragBorder == null)
								{
									flag = null;
								}
								else
								{
									MultiEventHandler eventHandler = topDragBorder.eventHandler;
									flag = ((eventHandler == null) ? ((bool?)null) : eventHandler.mouseDoubleClickFunc?.Contains(_maximizeAction));
								}
								bool? flag2 = flag;
								result = ((flag2 == true) ? 1 : 0);
							}
						}
						else
						{
							result = 0;
						}
						return (byte)result != 0;
					}
					catch
					{
						return false;
					}
				}
			}

			internal ResizeState(GameItem item)
			{
				Item = item;
				Window = item.contentWindow;
			}

			internal bool ReadVisibility(out bool visible)
			{
				visible = false;
				try
				{
					PixelWindow window = Window;
					RectTransform val = ((window != null) ? window.rectTransform : null);
					if ((Object)(object)val == (Object)null || !Object.op_Implicit((Object)(object)val))
					{
						return false;
					}
					visible = ((Component)val).gameObject.activeInHierarchy;
					return true;
				}
				catch
				{
					return false;
				}
			}

			internal void Deactivate()
			{
				if (_activeEdge != ResizeEdge.None)
				{
					FinishDrag(null);
				}
				_hoveredEdge = ResizeEdge.None;
				_wasVisible = false;
			}

			internal ResizeEdge Tick(Vector2 pointer, bool pointerHeld, bool focused, out bool isActive, out int order)
			{
				//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
				bool flag = false;
				try
				{
					PixelWindow window = Window;
					int num;
					if (window == null)
					{
						num = 0;
					}
					else
					{
						RectTransform rectTransform = window.rectTransform;
						bool? flag2;
						if (rectTransform == null)
						{
							flag2 = null;
						}
						else
						{
							GameObject gameObject = ((Component)rectTransform).gameObject;
							flag2 = ((gameObject != null) ? new bool?(gameObject.activeInHierarchy) : ((bool?)null));
						}
						bool? flag3 = flag2;
						num = ((flag3 == true) ? 1 : 0);
					}
					flag = (byte)num != 0;
				}
				catch
				{
				}
				if (flag && !_wasVisible)
				{
					BayExpansionPerk.TryRestoreRememberedGridShape(Item);
					BringHandlesToFront();
				}
				_wasVisible = flag;
				order = int.MinValue;
				isActive = false;
				if (!flag || !focused)
				{
					FinishDrag(null);
					_hoveredEdge = ResizeEdge.None;
					return ResizeEdge.None;
				}
				SyncOverlay();
				if (_activeEdge != ResizeEdge.None)
				{
					if (pointerHeld)
					{
						if (_pointerDrag)
						{
							DragAtPointer(pointer);
						}
						isActive = true;
						order = GetWindowOrder();
						return _activeEdge;
					}
					FinishDrag(null);
				}
				_hoveredEdge = FindHandleAtPointer(pointer, null);
				if (_hoveredEdge != ResizeEdge.None)
				{
					order = GetWindowOrder();
				}
				return _hoveredEdge;
			}

			internal string TestCloseButtonPriority()
			{
				//IL_0035: Unknown result type (might be due to invalid IL or missing references)
				//IL_003a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				//IL_004a: Unknown result type (might be due to invalid IL or missing references)
				//IL_006b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0070: Unknown result type (might be due to invalid IL or missing references)
				//IL_0091: Unknown result type (might be due to invalid IL or missing references)
				//IL_0096: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b7: Unknown result type (might be due to invalid IL or missing references)
				//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
				//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
				//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
				//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
				//IL_0101: Unknown result type (might be due to invalid IL or missing references)
				//IL_0106: Unknown result type (might be due to invalid IL or missing references)
				//IL_010b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0110: Unknown result type (might be due to invalid IL or missing references)
				//IL_0113: Unknown result type (might be due to invalid IL or missing references)
				//IL_0133: Unknown result type (might be due to invalid IL or missing references)
				//IL_0138: Unknown result type (might be due to invalid IL or missing references)
				//IL_013d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0142: Unknown result type (might be due to invalid IL or missing references)
				//IL_0147: Unknown result type (might be due to invalid IL or missing references)
				//IL_0169: Unknown result type (might be due to invalid IL or missing references)
				//IL_016e: Unknown result type (might be due to invalid IL or missing references)
				//IL_016f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0178: Expected Obj, but got Unknown
				PixelWindow window = Window;
				object obj;
				if (window == null)
				{
					obj = null;
				}
				else
				{
					TreeNodeRender topCloseButton = window.topCloseButton;
					obj = ((topCloseButton != null) ? topCloseButton.rectTransform : null);
				}
				RectTransform val = (RectTransform)obj;
				if ((Object)(object)val == (Object)null)
				{
					return "CONTAINER_CLOSE_PRIORITY FAIL: close button unavailable";
				}
				BringHandlesToFront();
				Rect rect = val.rect;
				Vector2[] array = new Vector2[5]
				{
					rect.center,
					new Vector2(rect.xMin + 1f, rect.yMin + 1f),
					new Vector2(rect.xMin + 1f, rect.yMax - 1f),
					new Vector2(rect.xMax - 1f, rect.yMin + 1f),
					new Vector2(rect.xMax - 1f, rect.yMax - 1f)
				};
				int num = 0;
				Vector2[] array2 = array;
				foreach (Vector2 val2 in array2)
				{
					Vector2 pointer = RectTransformUtility.WorldToScreenPoint((Camera)null, ((Transform)val).TransformPoint(Vector2.op_Implicit(val2)));
					if (FindHandleAtPointer(pointer, null) == ResizeEdge.None)
					{
						num++;
					}
				}
				Vector2 position = RectTransformUtility.WorldToScreenPoint((Camera)null, ((Transform)val).TransformPoint(Vector2.op_Implicit(rect.center)));
				EventSystem current = EventSystem.current;
				bool flag = false;
				string text = "<none>";
				if ((Object)(object)current != (Object)null)
				{
					PointerEventData val3 = new PointerEventData(current)
					{
						position = position
					};
					List<RaycastResult> val4 = new List<RaycastResult>();
					current.RaycastAll(val3, val4);
					if (val4.Count > 0)
					{
						GameObject gameObject = val4[0].gameObject;
						text = ((gameObject != null) ? ((Object)gameObject).name : null) ?? "<null>";
						Transform val5 = ((gameObject != null) ? gameObject.transform : null);
						flag = (Object)(object)val5 != (Object)null && (((Il2CppObjectBase)val5).Pointer == ((Il2CppObjectBase)val).Pointer || val5.IsChildOf((Transform)(object)val));
					}
				}
				TreeNodeRender overlay = _overlay;
				RectTransform val6 = ((overlay != null) ? overlay.rectTransform : null);
				bool flag2 = !((Object)(object)((val6 != null) ? ((Transform)val6).parent : null) != (Object)null) || !((Object)(object)((Transform)val).parent != (Object)null) || !(((Il2CppObjectBase)((Transform)val6).parent).Pointer == ((Il2CppObjectBase)((Transform)val).parent).Pointer) || ((Transform)val).GetSiblingIndex() > ((Transform)val6).GetSiblingIndex();
				PixelWindow window2 = Window;
				int? num2;
				if (window2 == null)
				{
					num2 = null;
				}
				else
				{
					TreeNodeRender topCloseButton2 = window2.topCloseButton;
					if (topCloseButton2 == null)
					{
						num2 = null;
					}
					else
					{
						MultiEventHandler eventHandler = topCloseButton2.eventHandler;
						num2 = ((eventHandler == null) ? ((int?)null) : eventHandler.mouseClickFunc?.Count);
					}
				}
				int? num3 = num2;
				int valueOrDefault = num3.GetValueOrDefault();
				bool flag3 = ((num == array.Length) & flag2) && valueOrDefault > 0;
				return "CONTAINER_CLOSE_PRIORITY " + (flag3 ? "PASS" : "FAIL") + "\nprotectedPoints=" + num + "/" + array.Length + " closeAboveOverlay=" + flag2 + " clickHandlers=" + valueOrDefault + "\ntopHit=" + text + " closeRaycast=" + flag + " active=" + ((Component)val).gameObject.activeInHierarchy + "\nedgeThickness=" + 8f + " cornerSize=" + 16f;
			}

			internal string TestPointerDrag()
			{
				//IL_0083: Unknown result type (might be due to invalid IL or missing references)
				//IL_0088: Unknown result type (might be due to invalid IL or missing references)
				//IL_008d: Unknown result type (might be due to invalid IL or missing references)
				//IL_008f: Unknown result type (might be due to invalid IL or missing references)
				//IL_009f: Unknown result type (might be due to invalid IL or missing references)
				//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
				//IL_00af: Unknown result type (might be due to invalid IL or missing references)
				//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
				//IL_00da: Unknown result type (might be due to invalid IL or missing references)
				//IL_00df: Unknown result type (might be due to invalid IL or missing references)
				GameGridInventory resizableInventory = BayExpansionPerk.GetResizableInventory(Item);
				ResizeHandle resizeHandle = _handles.Find((ResizeHandle handle) => handle.Edge == ResizeEdge.Right);
				if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) != null)
				{
					object obj;
					if (resizeHandle == null)
					{
						obj = null;
					}
					else
					{
						TreeNodeRender node = resizeHandle.Node;
						obj = ((node != null) ? node.rectTransform : null);
					}
					if (!((Object)obj == (Object)null))
					{
						int width = resizableInventory.inventoryShape.width;
						Vector2 val = RectTransformUtility.WorldToScreenPoint((Camera)null, ((Transform)resizeHandle.Node.rectTransform).position);
						if (!BeginPointerDrag(val, ResizeEdge.Right))
						{
							return "CONTAINER_POINTER_DRAG FAIL: pointer press did not reach front window";
						}
						ResizeEdge resizeEdge = Tick(val + new Vector2(100f, 0f), pointerHeld: true, focused: true, out var isActive, out var order);
						int width2 = resizableInventory.inventoryShape.width;
						Tick(val + new Vector2(100f, 0f), pointerHeld: false, focused: true, out var _, out order);
						bool flag = isActive && resizeEdge == ResizeEdge.Right && width2 > width && _activeEdge == ResizeEdge.None && !_pointerDrag;
						return "CONTAINER_POINTER_DRAG " + (flag ? "PASS" : "FAIL") + " initial=" + width + " expanded=" + width2 + " active=" + isActive + " released=" + (_activeEdge == ResizeEdge.None);
					}
				}
				return "CONTAINER_POINTER_DRAG FAIL: inventory or right edge unavailable";
			}

			internal string TestCursorFeedback()
			{
				string text = TestCursor(ResizeEdge.Left);
				string text2 = TestCursor(ResizeEdge.Top);
				string text3 = TestCursor(ResizeEdge.Left | ResizeEdge.Top);
				string text4 = TestCursor(ResizeEdge.Right | ResizeEdge.Top);
				ResizeCursor.Reset();
				string currentName = ResizeCursor.CurrentName;
				bool flag = text == "Horizontal" && text2 == "Vertical" && text3 == "DiagonalDown" && text4 == "DiagonalUp" && currentName == "Default";
				return "CONTAINER_CURSOR_FEEDBACK " + (flag ? "PASS" : "FAIL") + "\nhorizontal=" + text + " vertical=" + text2 + "\ndiagonalDown=" + text3 + " diagonalUp=" + text4 + "\nreset=" + currentName + " visible=" + Cursor.visible + " mode=ForceSoftware";
			}

			internal string TestCursorRelease()
			{
				ResizeHandle resizeHandle = _handles.Find((ResizeHandle handle) => handle.Edge == ResizeEdge.Right);
				if (resizeHandle == null)
				{
					return "CONTAINER_CURSOR_RELEASE FAIL: right handle unavailable";
				}
				ResizeCursor.Reset();
				resizeHandle.Enter.Invoke((PointerEventData)null);
				_activeEdge = ResizeEdge.Right;
				resizeHandle.Exit.Invoke((PointerEventData)null);
				string currentName = ResizeCursor.CurrentName;
				resizeHandle.End.Invoke((PointerEventData)null);
				string currentName2 = ResizeCursor.CurrentName;
				resizeHandle.Enter.Invoke((PointerEventData)null);
				_activeEdge = ResizeEdge.Right;
				resizeHandle.End.Invoke((PointerEventData)null);
				string currentName3 = ResizeCursor.CurrentName;
				resizeHandle.Exit.Invoke((PointerEventData)null);
				string currentName4 = ResizeCursor.CurrentName;
				bool flag = currentName == "Horizontal" && currentName2 == "Default" && currentName3 == "Horizontal" && currentName4 == "Default";
				return "CONTAINER_CURSOR_RELEASE " + (flag ? "PASS" : "FAIL") + "\ndraggingOutside=" + currentName + " releasedOutside=" + currentName2 + "\nreleasedInside=" + currentName3 + " exitedAfterRelease=" + currentName4;
			}

			internal string TestCursorReconciliation()
			{
				//IL_005d: Unknown result type (might be due to invalid IL or missing references)
				//IL_006e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0073: Unknown result type (might be due to invalid IL or missing references)
				//IL_0078: Unknown result type (might be due to invalid IL or missing references)
				//IL_008d: Unknown result type (might be due to invalid IL or missing references)
				//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
				//IL_00f4: Unknown result type (might be due to invalid IL or missing references)
				//IL_0129: Unknown result type (might be due to invalid IL or missing references)
				ResizeHandle resizeHandle = _handles.Find((ResizeHandle handle) => handle.Edge == ResizeEdge.Right);
				object obj;
				if (resizeHandle == null)
				{
					obj = null;
				}
				else
				{
					TreeNodeRender node = resizeHandle.Node;
					obj = ((node != null) ? node.rectTransform : null);
				}
				if ((Object)obj == (Object)null)
				{
					return "CONTAINER_CURSOR_RECONCILIATION FAIL: right handle unavailable";
				}
				Vector2 pointer = new Vector2(-10000f, -10000f);
				Vector2 pointer2 = RectTransformUtility.WorldToScreenPoint((Camera)null, ((Transform)resizeHandle.Node.rectTransform).position);
				_activeEdge = ResizeEdge.None;
				_hoveredEdge = ResizeEdge.None;
				ResizeCursor.Reset();
				ResizeEdge resizeEdge = Tick(pointer2, pointerHeld: false, focused: true, out var isActive, out var order);
				if (resizeEdge == ResizeEdge.None)
				{
					ResizeCursor.Reset();
				}
				else
				{
					ResizeCursor.Set(resizeEdge);
				}
				string currentName = ResizeCursor.CurrentName;
				_hoveredEdge = ResizeEdge.Right;
				ResizeCursor.Set(ResizeEdge.Right);
				ResizeEdge resizeEdge2 = Tick(pointer, pointerHeld: false, focused: true, out isActive, out order);
				if (resizeEdge2 == ResizeEdge.None)
				{
					ResizeCursor.Reset();
				}
				else
				{
					ResizeCursor.Set(resizeEdge2);
				}
				string currentName2 = ResizeCursor.CurrentName;
				_activeEdge = ResizeEdge.Right;
				ResizeCursor.Set(ResizeEdge.Right);
				ResizeEdge resizeEdge3 = Tick(pointer, pointerHeld: false, focused: true, out isActive, out order);
				if (resizeEdge3 == ResizeEdge.None)
				{
					ResizeCursor.Reset();
				}
				else
				{
					ResizeCursor.Set(resizeEdge3);
				}
				string currentName3 = ResizeCursor.CurrentName;
				_hoveredEdge = ResizeEdge.Right;
				ResizeCursor.Set(ResizeEdge.Right);
				ResizeEdge resizeEdge4 = Tick(pointer2, pointerHeld: false, focused: false, out isActive, out order);
				if (resizeEdge4 == ResizeEdge.None)
				{
					ResizeCursor.Reset();
				}
				else
				{
					ResizeCursor.Set(resizeEdge4);
				}
				string currentName4 = ResizeCursor.CurrentName;
				bool flag = currentName == "Horizontal" && currentName2 == "Default" && currentName3 == "Default" && currentName4 == "Default" && _activeEdge == ResizeEdge.None && _hoveredEdge == ResizeEdge.None;
				return "CONTAINER_CURSOR_RECONCILIATION " + (flag ? "PASS" : "FAIL") + "\nentered=" + currentName + " missedExit=" + currentName2 + "\nmissedEnd=" + currentName3 + " focusLoss=" + currentName4 + "\nactive=" + _activeEdge.ToString() + " hover=" + _hoveredEdge;
			}

			private string TestCursor(ResizeEdge edge)
			{
				foreach (ResizeHandle handle in _handles)
				{
					if (handle.Edge == edge)
					{
						handle.Enter.Invoke((PointerEventData)null);
						string currentName = ResizeCursor.CurrentName;
						handle.Exit.Invoke((PointerEventData)null);
						return currentName;
					}
				}
				return "Missing";
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
				//IL_013a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0149: Unknown result type (might be due to invalid IL or missing references)
				//IL_0158: Unknown result type (might be due to invalid IL or missing references)
				//IL_0174: Unknown result type (might be due to invalid IL or missing references)
				//IL_0183: Unknown result type (might be due to invalid IL or missing references)
				//IL_0192: Unknown result type (might be due to invalid IL or missing references)
				//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
				//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
				//IL_01cc: Unknown result type (might be due to invalid IL or missing references)
				DestroyHandles();
				CreateOverlay();
				AddHandle("Left", ResizeEdge.Left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, -32f));
				AddHandle("Right", ResizeEdge.Right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(8f, -32f));
				AddHandle("Top", ResizeEdge.Top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-32f, 8f));
				AddHandle("Bottom", ResizeEdge.Bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 8f));
				AddHandle("TopLeft", ResizeEdge.Left | ResizeEdge.Top, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, 16f));
				AddHandle("TopRight", ResizeEdge.Right | ResizeEdge.Top, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(16f, 16f));
				AddHandle("BottomLeft", ResizeEdge.Left | ResizeEdge.Bottom, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f));
				AddHandle("BottomRight", ResizeEdge.Right | ResizeEdge.Bottom, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f));
				AttachMaximizeHandler();
			}

			internal void BringHandlesToFront()
			{
				SyncOverlay();
				try
				{
					TreeNodeRender overlay = _overlay;
					if (overlay != null)
					{
						RectTransform rectTransform = overlay.rectTransform;
						if (rectTransform != null)
						{
							((Transform)rectTransform).SetAsLastSibling();
						}
					}
				}
				catch
				{
				}
				foreach (ResizeHandle handle in _handles)
				{
					try
					{
						TreeNodeRender node = handle.Node;
						if (node != null)
						{
							RectTransform rectTransform2 = node.rectTransform;
							if (rectTransform2 != null)
							{
								((Transform)rectTransform2).SetAsLastSibling();
							}
						}
					}
					catch
					{
					}
				}
				try
				{
					PixelWindow window = Window;
					if (window == null)
					{
						return;
					}
					TreeNodeRender topCloseButton = window.topCloseButton;
					if (topCloseButton != null)
					{
						RectTransform rectTransform3 = topCloseButton.rectTransform;
						if (rectTransform3 != null)
						{
							((Transform)rectTransform3).SetAsLastSibling();
						}
					}
				}
				catch
				{
				}
			}

			internal void DestroyHandles()
			{
				try
				{
					if ((Delegate)(object)_maximizeAction != (Delegate)null)
					{
						PixelWindow window = Window;
						if (window != null)
						{
							TreeNodeRender topDragBorder = window.topDragBorder;
							if (topDragBorder != null)
							{
								MultiEventHandler eventHandler = topDragBorder.eventHandler;
								if (eventHandler != null)
								{
									eventHandler.mouseDoubleClickFunc?.Remove(_maximizeAction);
								}
							}
						}
					}
				}
				catch
				{
				}
				foreach (ResizeHandle handle in _handles)
				{
					try
					{
						TreeNodeRender node = handle.Node;
						if ((Object)(object)((node != null) ? node.rectTransform : null) != (Object)null)
						{
							Object.Destroy((Object)(object)((Component)handle.Node.rectTransform).gameObject);
						}
					}
					catch
					{
					}
				}
				try
				{
					TreeNodeRender overlay = _overlay;
					if ((Object)(object)((overlay != null) ? overlay.rectTransform : null) != (Object)null)
					{
						Object.Destroy((Object)(object)((Component)_overlay.rectTransform).gameObject);
					}
				}
				catch
				{
				}
				_handles.Clear();
				_overlay = null;
				_maximizeAction = null;
				_activeEdge = ResizeEdge.None;
				_hoveredEdge = ResizeEdge.None;
				ResizeCursor.Reset();
			}

			private void AttachMaximizeHandler()
			{
				TreeNodeRender topDragBorder = Window.topDragBorder;
				if (!((Object)(object)topDragBorder == (Object)null))
				{
					topDragBorder.InitEventHandler();
					_maximizeAction = Convert(ToggleMaximize);
					topDragBorder.eventHandler.mouseDoubleClickFunc.Add(_maximizeAction);
				}
			}

			private void CreateOverlay()
			{
				_overlay = TreeNodeRender.Instantiate(Window, "XIAOWOContainerResizeOverlay");
				TreeNodeRender overlay = _overlay;
				if ((Object)(object)((overlay != null) ? overlay.rectTransform : null) == (Object)null)
				{
					throw new InvalidOperationException("container resize overlay could not be created");
				}
				((Transform)_overlay.rectTransform).SetParent((Transform)(object)Window.rectTransform, false);
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

			private void SyncOverlay()
			{
				//IL_0046: Unknown result type (might be due to invalid IL or missing references)
				//IL_004b: Unknown result type (might be due to invalid IL or missing references)
				//IL_004d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0052: Unknown result type (might be due to invalid IL or missing references)
				//IL_0062: Unknown result type (might be due to invalid IL or missing references)
				//IL_0067: Unknown result type (might be due to invalid IL or missing references)
				//IL_0069: Unknown result type (might be due to invalid IL or missing references)
				//IL_006e: Unknown result type (might be due to invalid IL or missing references)
				//IL_005b: Unknown result type (might be due to invalid IL or missing references)
				//IL_007e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0083: Unknown result type (might be due to invalid IL or missing references)
				//IL_0085: Unknown result type (might be due to invalid IL or missing references)
				//IL_008a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0077: Unknown result type (might be due to invalid IL or missing references)
				//IL_009a: Unknown result type (might be due to invalid IL or missing references)
				//IL_009f: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a6: Unknown result type (might be due to invalid IL or missing references)
				//IL_0093: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
				//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
				//IL_00af: Unknown result type (might be due to invalid IL or missing references)
				//IL_00d2: Unknown result type (might be due to invalid IL or missing references)
				//IL_00d7: Unknown result type (might be due to invalid IL or missing references)
				//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
				PixelWindow window = Window;
				object obj;
				if (window == null)
				{
					obj = null;
				}
				else
				{
					TreeNodeRender outsideBorder = window.outsideBorder;
					obj = ((outsideBorder != null) ? outsideBorder.rectTransform : null);
				}
				RectTransform val = (RectTransform)obj;
				TreeNodeRender overlay = _overlay;
				RectTransform val2 = ((overlay != null) ? overlay.rectTransform : null);
				if (!((Object)(object)val == (Object)null) && !((Object)(object)val2 == (Object)null))
				{
					Vector2 anchorMin = val.anchorMin;
					if (val2.anchorMin != anchorMin)
					{
						val2.anchorMin = anchorMin;
					}
					anchorMin = val.anchorMax;
					if (val2.anchorMax != anchorMin)
					{
						val2.anchorMax = anchorMin;
					}
					anchorMin = val.pivot;
					if (val2.pivot != anchorMin)
					{
						val2.pivot = anchorMin;
					}
					anchorMin = val.anchoredPosition;
					if (val2.anchoredPosition != anchorMin)
					{
						val2.anchoredPosition = anchorMin;
					}
					anchorMin = val.sizeDelta;
					if (val2.sizeDelta != anchorMin)
					{
						val2.sizeDelta = anchorMin;
					}
					if (((Transform)val2).localScale != Vector3.one)
					{
						((Transform)val2).localScale = Vector3.one;
					}
				}
			}

			private void AddHandle(string name, ResizeEdge edge, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
			{
				//IL_0097: Unknown result type (might be due to invalid IL or missing references)
				//IL_009e: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
				//IL_00ce: Unknown result type (might be due to invalid IL or missing references)
				//IL_011b: Unknown result type (might be due to invalid IL or missing references)
				TreeNodeRender val = TreeNodeRender.Instantiate(Window, "XIAOWOContainerResize" + name);
				if ((Object)(object)((val != null) ? val.rectTransform : null) == (Object)null)
				{
					throw new InvalidOperationException("container resize handle could not be created");
				}
				RectTransform rectTransform = val.rectTransform;
				TreeNodeRender overlay = _overlay;
				object obj = ((overlay != null) ? overlay.rectTransform : null);
				if (obj == null)
				{
					TreeNodeRender outsideBorder = Window.outsideBorder;
					obj = ((outsideBorder != null) ? outsideBorder.rectTransform : null) ?? Window.rectTransform;
				}
				RectTransform val2 = (RectTransform)obj;
				((Transform)rectTransform).SetParent((Transform)(object)val2, false);
				rectTransform.anchorMin = anchorMin;
				rectTransform.anchorMax = anchorMax;
				rectTransform.pivot = new Vector2(0.5f, 0.5f);
				rectTransform.anchoredPosition = Vector2.zero;
				rectTransform.sizeDelta = sizeDelta;
				((Transform)rectTransform).localScale = Vector3.one;
				((Transform)rectTransform).SetAsLastSibling();
				Image val3 = (val.image = val.image ?? ((Component)rectTransform).gameObject.AddComponent<Image>());
				((Behaviour)val3).enabled = true;
				((Graphic)val3).color = new Color(1f, 1f, 1f, 0.001f);
				((Graphic)val3).raycastTarget = true;
				val.InitEventHandler();
				Func<PointerEventData, bool> val5 = Convert((PointerEventData data) => PrepareDrag(data));
				Func<PointerEventData, bool> val6 = Convert((PointerEventData data) => BeginDrag(data, edge));
				Func<PointerEventData, bool> val7 = Convert(Drag);
				Func<PointerEventData, bool> val8 = Convert(EndDrag);
				Func<PointerEventData, bool> val9 = Convert((PointerEventData data) => EnterHandle(data, edge));
				Func<PointerEventData, bool> val10 = Convert((PointerEventData data) => ExitHandle(data, edge));
				val.eventHandler.mousePotentialDragFunc.Add(val5);
				val.eventHandler.mouseBeginDragFunc.Add(val6);
				val.eventHandler.mouseDragFunc.Add(val7);
				val.eventHandler.mouseEndDragFunc.Add(val8);
				val.eventHandler.mouseEnterFunc.Add(val9);
				val.eventHandler.mouseExitFunc.Add(val10);
				_handles.Add(new ResizeHandle(val, edge, val5, val6, val7, val8, val9, val10));
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
				if (data != null)
				{
					PixelWindow window = Window;
					if (!((Object)(object)((window != null) ? window.rectTransform : null) == (Object)null) && TryPointerInParent(data, out _startPointer))
					{
						if (_activeEdge != ResizeEdge.None)
						{
							((AbstractEventData)data).Use();
							return true;
						}
						if (!StartDrag(edge))
						{
							return false;
						}
						((AbstractEventData)data).Use();
						return true;
					}
				}
				return false;
			}

			internal bool BeginPointerDrag(Vector2 pointer, ResizeEdge edge)
			{
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0015: Unknown result type (might be due to invalid IL or missing references)
				if (edge == ResizeEdge.None || _activeEdge != ResizeEdge.None || !IsTopmostWindowHit(pointer) || !TryPointerInParent(pointer, null, out _startPointer))
				{
					return false;
				}
				if (!StartDrag(edge))
				{
					return false;
				}
				_pointerDrag = true;
				return true;
			}

			private bool IsTopmostWindowHit(Vector2 pointer)
			{
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_0018: Unknown result type (might be due to invalid IL or missing references)
				//IL_001f: Expected Obj, but got Unknown
				EventSystem current = EventSystem.current;
				if ((Object)(object)current == (Object)null)
				{
					return false;
				}
				PointerEventData val = new PointerEventData(current)
				{
					position = pointer
				};
				List<RaycastResult> val2 = new List<RaycastResult>();
				current.RaycastAll(val, val2);
				object obj;
				if (val2.Count <= 0)
				{
					obj = null;
				}
				else
				{
					GameObject gameObject = val2[0].gameObject;
					obj = ((gameObject != null) ? gameObject.transform : null);
				}
				Transform val3 = (Transform)obj;
				if ((Object)(object)val3 != (Object)null)
				{
					return val3.IsChildOf((Transform)(object)Window.rectTransform);
				}
				return false;
			}

			private bool StartDrag(ResizeEdge edge)
			{
				//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
				//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00e5: Unknown result type (might be due to invalid IL or missing references)
				GameGridInventory resizableInventory = BayExpansionPerk.GetResizableInventory(Item);
				if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) == null)
				{
					return false;
				}
				_activeEdge = edge;
				ResizeCursor.Set(edge);
				_maximized = false;
				_startWidth = Window.widthPixels;
				_startHeight = Window.heightPixels;
				_startColumns = resizableInventory.inventoryShape.width;
				_startRows = resizableInventory.inventoryShape.height;
				_columnPixels = Math.Max(1f, (float)resizableInventory.widthPixels / (float)Math.Max(1, _startColumns));
				_rowPixels = Math.Max(1f, (float)resizableInventory.heightPixels / (float)Math.Max(1, _startRows));
				_startPosition = Window.rectTransform.anchoredPosition;
				_startPivot = Window.rectTransform.pivot;
				Window.ToFront(true);
				return true;
			}

			private bool ToggleMaximize(PointerEventData data)
			{
				//IL_0095: Unknown result type (might be due to invalid IL or missing references)
				//IL_009a: Unknown result type (might be due to invalid IL or missing references)
				//IL_005b: Unknown result type (might be due to invalid IL or missing references)
				PixelWindow window = Window;
				RectTransform val = ((window != null) ? window.rectTransform : null);
				Transform val2 = ((val != null) ? ((Transform)val).parent : null);
				if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
				{
					return false;
				}
				int actualWidth;
				int actualHeight;
				if (_maximized)
				{
					BayExpansionPerk.TryResizeGridWindow(Item, _restoreColumns, _restoreRows, remember: true, out actualWidth, out actualHeight);
					val.anchoredPosition = _restorePosition;
					_maximized = false;
				}
				else
				{
					_restoreWidth = Window.widthPixels;
					_restoreHeight = Window.heightPixels;
					_restorePosition = val.anchoredPosition;
					GameGridInventory resizableInventory = BayExpansionPerk.GetResizableInventory(Item);
					if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) == null || !BayExpansionPerk.TryGetGridShapeLimits(Item, out actualHeight, out actualWidth, out var maximumColumns, out var maximumRows))
					{
						return false;
					}
					_restoreColumns = resizableInventory.inventoryShape.width;
					_restoreRows = resizableInventory.inventoryShape.height;
					BayExpansionPerk.TryResizeGridWindow(Item, maximumColumns, maximumRows, remember: true, out actualWidth, out actualHeight);
					Window.Center();
					_maximized = true;
				}
				Window.FixBounds();
				Item.RememberContentWindowPositionIfMine(Window);
				BringHandlesToFront();
				if (data != null)
				{
					((AbstractEventData)data).Use();
				}
				return true;
			}

			private bool Drag(PointerEventData data)
			{
				//IL_002c: Unknown result type (might be due to invalid IL or missing references)
				if (_pointerDrag)
				{
					if (data != null)
					{
						((AbstractEventData)data).Use();
					}
					return true;
				}
				if (_activeEdge == ResizeEdge.None || data == null || !TryPointerInParent(data, out var point))
				{
					return false;
				}
				if (!DragAt(point))
				{
					return false;
				}
				((AbstractEventData)data).Use();
				return true;
			}

			private bool DragAtPointer(Vector2 pointer)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_000d: Unknown result type (might be due to invalid IL or missing references)
				if (TryPointerInParent(pointer, null, out var point))
				{
					return DragAt(point);
				}
				return false;
			}

			private bool DragAt(Vector2 pointer)
			{
				//IL_000b: Unknown result type (might be due to invalid IL or missing references)
				//IL_000d: Unknown result type (might be due to invalid IL or missing references)
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_0037: Unknown result type (might be due to invalid IL or missing references)
				//IL_0059: Unknown result type (might be due to invalid IL or missing references)
				//IL_0079: Unknown result type (might be due to invalid IL or missing references)
				//IL_009b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0132: Unknown result type (might be due to invalid IL or missing references)
				//IL_0137: Unknown result type (might be due to invalid IL or missing references)
				//IL_01e4: Unknown result type (might be due to invalid IL or missing references)
				ResizeCursor.Set(_activeEdge);
				Vector2 val = pointer - _startPointer;
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
				if (!BayExpansionPerk.TryResizeGridWindow(Item, num3, num4, remember: false, out var actualWidth, out var actualHeight))
				{
					return false;
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
				BringHandlesToFront();
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

			[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
			private bool FinishDrag(PointerEventData data)
			{
				if (_activeEdge == ResizeEdge.None)
				{
					return false;
				}
				_activeEdge = ResizeEdge.None;
				_pointerDrag = false;
				try
				{
					BayExpansionPerk.RememberWindowSize(Item, Window.widthPixels, Window.heightPixels);
					GameGridInventory resizableInventory = BayExpansionPerk.GetResizableInventory(Item);
					if (((resizableInventory != null) ? resizableInventory.inventoryShape : null) != null)
					{
						BayExpansionPerk.RememberGridShape(Item, resizableInventory.inventoryShape.width, resizableInventory.inventoryShape.height);
					}
					Window.FixBounds();
					Item.RememberContentWindowPositionIfMine(Window);
				}
				catch
				{
				}
				return true;
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
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				if (data == null)
				{
					return _hoveredEdge;
				}
				return FindHandleAtPointer(data.position, data.pressEventCamera);
			}

			private ResizeEdge FindHandleAtPointer(Vector2 pointer, Camera camera)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0046: Unknown result type (might be due to invalid IL or missing references)
				if (IsPointerOverCloseButton(pointer, camera))
				{
					return ResizeEdge.None;
				}
				for (int num = _handles.Count - 1; num >= 0; num--)
				{
					ResizeHandle resizeHandle = _handles[num];
					try
					{
						TreeNodeRender node = resizeHandle.Node;
						RectTransform val = ((node != null) ? node.rectTransform : null);
						if ((Object)(object)val != (Object)null && RectTransformUtility.RectangleContainsScreenPoint(val, pointer, camera))
						{
							return resizeHandle.Edge;
						}
					}
					catch
					{
					}
				}
				return ResizeEdge.None;
			}

			private bool IsPointerOverCloseButton(Vector2 pointer, Camera camera)
			{
				//IL_003a: Unknown result type (might be due to invalid IL or missing references)
				//IL_004a: Unknown result type (might be due to invalid IL or missing references)
				//IL_004f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0050: Unknown result type (might be due to invalid IL or missing references)
				//IL_0065: Unknown result type (might be due to invalid IL or missing references)
				//IL_007a: Unknown result type (might be due to invalid IL or missing references)
				//IL_008f: Unknown result type (might be due to invalid IL or missing references)
				try
				{
					PixelWindow window = Window;
					object obj;
					if (window == null)
					{
						obj = null;
					}
					else
					{
						TreeNodeRender topCloseButton = window.topCloseButton;
						obj = ((topCloseButton != null) ? topCloseButton.rectTransform : null);
					}
					RectTransform val = (RectTransform)obj;
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
					PixelWindow window = Window;
					int? num;
					if (window == null)
					{
						num = null;
					}
					else
					{
						RectTransform rectTransform = window.rectTransform;
						num = ((rectTransform != null) ? new int?(((Transform)rectTransform).GetSiblingIndex()) : ((int?)null));
					}
					return num ?? int.MinValue;
				}
				catch
				{
					return int.MinValue;
				}
			}

			private bool TryPointerInParent(PointerEventData data, out Vector2 point)
			{
				//IL_0002: Unknown result type (might be due to invalid IL or missing references)
				return TryPointerInParent(data.position, data.pressEventCamera, out point);
			}

			private bool TryPointerInParent(Vector2 pointer, Camera camera, out Vector2 point)
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				//IL_0006: Unknown result type (might be due to invalid IL or missing references)
				//IL_003b: Unknown result type (might be due to invalid IL or missing references)
				//IL_0040: Unknown result type (might be due to invalid IL or missing references)
				//IL_0048: Expected Obj, but got Unknown
				point = Vector2.zero;
				PixelWindow window = Window;
				object obj;
				if (window == null)
				{
					obj = null;
				}
				else
				{
					RectTransform rectTransform = window.rectTransform;
					obj = ((rectTransform != null) ? ((Transform)rectTransform).parent : null);
				}
				Transform val = (Transform)obj;
				if ((Object)(object)val == (Object)null)
				{
					return false;
				}
				return RectTransformUtility.ScreenPointToLocalPointInRectangle(new RectTransform(((Il2CppObjectBase)val).Pointer), pointer, camera, ref point);
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

		private static class ResizeCursor
		{
			private enum Kind
			{
				Default,
				Horizontal,
				Vertical,
				DiagonalDown,
				DiagonalUp
			}

			private static readonly Dictionary<Kind, Texture2D> Textures = new Dictionary<Kind, Texture2D>();

			private static Kind _current;

			internal static string CurrentName => _current.ToString();

			internal static void Set(ResizeEdge edge)
			{
				//IL_007e: Unknown result type (might be due to invalid IL or missing references)
				Kind kind;
				switch (edge)
				{
				case ResizeEdge.None:
					Reset();
					return;
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
				Kind kind2 = kind;
				if (!Textures.TryGetValue(kind2, out var value))
				{
					value = Create(kind2);
					Textures[kind2] = value;
				}
				Cursor.visible = true;
				Cursor.SetCursor(value, new Vector2(12f, 12f), (CursorMode)1);
				_current = kind2;
			}

			internal static void Maintain()
			{
				//IL_002c: Unknown result type (might be due to invalid IL or missing references)
				if (_current != Kind.Default && Textures.TryGetValue(_current, out var value))
				{
					Cursor.visible = true;
					Cursor.SetCursor(value, new Vector2(12f, 12f), (CursorMode)1);
				}
			}

			internal static void Reset()
			{
				//IL_0009: Unknown result type (might be due to invalid IL or missing references)
				if (_current != Kind.Default)
				{
					Cursor.SetCursor((Texture2D)null, Vector2.zero, (CursorMode)0);
					_current = Kind.Default;
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
					catch
					{
					}
				}
				Textures.Clear();
			}

			private static Texture2D Create(Kind kind)
			{
				//IL_0007: Unknown result type (might be due to invalid IL or missing references)
				//IL_000c: Unknown result type (might be due to invalid IL or missing references)
				//IL_0029: Unknown result type (might be due to invalid IL or missing references)
				//IL_0030: Unknown result type (might be due to invalid IL or missing references)
				//IL_0038: Expected Obj, but got Unknown
				//IL_0043: Unknown result type (might be due to invalid IL or missing references)
				//IL_024a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0293: Unknown result type (might be due to invalid IL or missing references)
				Texture2D val = new Texture2D(24, 24, (TextureFormat)4, false, false)
				{
					name = "XIAOWOContainerResizeCursor" + kind,
					filterMode = (FilterMode)0,
					wrapMode = (TextureWrapMode)1
				};
				for (int i = 0; i < 24; i++)
				{
					for (int j = 0; j < 24; j++)
					{
						val.SetPixel(i, j, Color.clear);
					}
				}
				(int, int, int, int)[] array = kind switch
				{
					Kind.Horizontal => new (int, int, int, int)[5]
					{
						(3, 12, 20, 12),
						(3, 12, 8, 7),
						(3, 12, 8, 17),
						(20, 12, 15, 7),
						(20, 12, 15, 17)
					}, 
					Kind.Vertical => new (int, int, int, int)[5]
					{
						(12, 3, 12, 20),
						(12, 3, 7, 8),
						(12, 3, 17, 8),
						(12, 20, 7, 15),
						(12, 20, 17, 15)
					}, 
					Kind.DiagonalDown => new (int, int, int, int)[5]
					{
						(4, 19, 19, 4),
						(4, 19, 4, 13),
						(4, 19, 10, 19),
						(19, 4, 13, 4),
						(19, 4, 19, 10)
					}, 
					_ => new (int, int, int, int)[5]
					{
						(4, 4, 19, 19),
						(4, 4, 4, 10),
						(4, 4, 10, 4),
						(19, 19, 13, 19),
						(19, 19, 19, 13)
					}, 
				};
				(int, int, int, int)[] array2 = array;
				for (int k = 0; k < array2.Length; k++)
				{
					(int, int, int, int) tuple = array2[k];
					DrawLine(val, tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4, 2, Color.black);
				}
				array2 = array;
				for (int k = 0; k < array2.Length; k++)
				{
					(int, int, int, int) tuple2 = array2[k];
					DrawLine(val, tuple2.Item1, tuple2.Item2, tuple2.Item3, tuple2.Item4, 0, Color.white);
				}
				val.Apply(false, false);
				return val;
			}

			private static void DrawLine(Texture2D texture, int x0, int y0, int x1, int y1, int radius, Color color)
			{
				//IL_0057: Unknown result type (might be due to invalid IL or missing references)
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
							if (i >= 0 && i < 24 && j >= 0 && j < 24)
							{
								texture.SetPixel(i, j, color);
							}
						}
					}
					if (x0 != x1 || y0 != y1)
					{
						int num6 = num5 * 2;
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
		}

		private const float EdgeThickness = 8f;

		private const float CornerSize = 16f;

		private const float CloseButtonPadding = 2f;

		private static readonly Dictionary<IntPtr, ResizeState> States = new Dictionary<IntPtr, ResizeState>();

		private static readonly List<ResizeState> VisibleStates = new List<ResizeState>();

		private static readonly List<IntPtr> StaleStates = new List<IntPtr>();

		private static RuntimePollGate _visibilityPoll;

		private static bool _visibilityDirty = true;

		internal static string CurrentCursorName => ResizeCursor.CurrentName;

		internal static void Attach(GameItem item)
		{
			if (!BayExpansionPerk.IsResizableContainer(item))
			{
				return;
			}
			PixelWindow contentWindow = item.contentWindow;
			if (!((Object)(object)((contentWindow != null) ? contentWindow.rectTransform : null) == (Object)null))
			{
				IntPtr pointer = ((Il2CppObjectBase)item.contentWindow).Pointer;
				if (!States.TryGetValue(pointer, out var value) || !value.IsAlive)
				{
					value = new ResizeState(item);
					States[pointer] = value;
					value.CreateHandles();
				}
				else
				{
					value.Item = item;
					value.BringHandlesToFront();
				}
				_visibilityDirty = true;
			}
		}

		internal static void WindowVisibilityChanged(PixelWindow window)
		{
			if (window != null && States.ContainsKey(((Il2CppObjectBase)window).Pointer))
			{
				_visibilityDirty = true;
			}
		}

		private static void RefreshVisibleStates()
		{
			VisibleStates.Clear();
			StaleStates.Clear();
			foreach (KeyValuePair<IntPtr, ResizeState> state in States)
			{
				if (!state.Value.ReadVisibility(out var visible))
				{
					StaleStates.Add(state.Key);
				}
				else if (visible)
				{
					VisibleStates.Add(state.Value);
				}
				else
				{
					state.Value.Deactivate();
				}
			}
			foreach (IntPtr staleState in StaleStates)
			{
				States[staleState].DestroyHandles();
				States.Remove(staleState);
			}
			_visibilityDirty = false;
		}

		internal static int HandleCount(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return 0;
			}
			return value.LiveHandleCount;
		}

		internal static bool HasMaximizeHandler(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return false;
			}
			return value.HasMaximizeHandler;
		}

		internal static void SetModManagerCursor(int edge)
		{
			ResizeCursor.Set((ResizeEdge)edge);
		}

		internal static void ResetModManagerCursor()
		{
			ResizeCursor.Reset();
		}

		internal static void Tick()
		{
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			if (States.Count == 0)
			{
				return;
			}
			bool flag = _visibilityPoll.Due(250);
			if (_visibilityDirty | flag)
			{
				RefreshVisibleStates();
			}
			if (VisibleStates.Count == 0)
			{
				ResizeCursor.Reset();
				return;
			}
			Vector2 pointer;
			bool mouseButton;
			bool mouseButtonDown;
			bool isFocused;
			try
			{
				pointer = Vector2.op_Implicit(Input.mousePosition);
				mouseButton = Input.GetMouseButton(0);
				mouseButtonDown = Input.GetMouseButtonDown(0);
				isFocused = Application.isFocused;
			}
			catch
			{
				ResizeCursor.Reset();
				return;
			}
			ResizeEdge resizeEdge = ResizeEdge.None;
			bool flag2 = false;
			int num = int.MinValue;
			ResizeState resizeState = null;
			foreach (ResizeState visibleState in VisibleStates)
			{
				try
				{
					ResizeEdge resizeEdge2 = visibleState.Tick(pointer, mouseButton, isFocused, out var isActive, out var order);
					if (resizeEdge2 != ResizeEdge.None && ((isActive && !flag2) || (isActive == flag2 && order >= num)))
					{
						resizeEdge = resizeEdge2;
						flag2 = isActive;
						num = order;
						resizeState = visibleState;
					}
				}
				catch
				{
				}
			}
			if ((mouseButtonDown & isFocused) && !flag2 && resizeState != null && resizeState.BeginPointerDrag(pointer, resizeEdge))
			{
				resizeEdge = resizeState.ActiveEdge;
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

		internal static string TestCursorFeedback(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return "CONTAINER_CURSOR_FEEDBACK FAIL: resize state unavailable";
			}
			return value.TestCursorFeedback();
		}

		internal static string TestCursorRelease(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return "CONTAINER_CURSOR_RELEASE FAIL: resize state unavailable";
			}
			return value.TestCursorRelease();
		}

		internal static string TestCursorReconciliation(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return "CONTAINER_CURSOR_RECONCILIATION FAIL: resize state unavailable";
			}
			return value.TestCursorReconciliation();
		}

		internal static string TestCloseButtonPriority(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return "CONTAINER_CLOSE_PRIORITY FAIL: resize state unavailable";
			}
			return value.TestCloseButtonPriority();
		}

		internal static string TestPointerDrag(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return "CONTAINER_POINTER_DRAG FAIL: resize state unavailable";
			}
			return value.TestPointerDrag();
		}

		internal static string TestVisibilityPolling(GameItem item)
		{
			if (((item != null) ? item.contentWindow : null) == null || !States.TryGetValue(((Il2CppObjectBase)item.contentWindow).Pointer, out var value))
			{
				return "CONTAINER_VISIBILITY_POLLING FAIL: resize state unavailable";
			}
			PixelWindow contentWindow = item.contentWindow;
			bool flag = contentWindow.IsVisible();
			try
			{
				contentWindow.Show();
				Tick();
				bool flag2 = VisibleStates.Contains(value);
				contentWindow.Hide();
				Tick();
				bool flag3 = !VisibleStates.Contains(value);
				contentWindow.Show();
				Tick();
				bool flag4 = VisibleStates.Contains(value);
				return "CONTAINER_VISIBILITY_POLLING " + ((flag2 & flag3 & flag4) ? "PASS" : "FAIL") + "\nshown=" + flag2 + " hidden=" + flag3 + " reopened=" + flag4;
			}
			finally
			{
				if (!flag)
				{
					contentWindow.Hide();
				}
				Tick();
			}
		}

		internal static void Dispose()
		{
			foreach (ResizeState value in States.Values)
			{
				value.DestroyHandles();
			}
			States.Clear();
			VisibleStates.Clear();
			StaleStates.Clear();
			_visibilityDirty = true;
			_visibilityPoll.Reset();
			ResizeCursor.Dispose();
		}
	}
	[HarmonyPatch]
	internal static class BayExpansionWindowVisibilityPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(PixelWindow), "Show", Type.EmptyTypes, (Type[])null) ?? throw new MissingMethodException("PixelWindow", "Show()");
			yield return AccessTools.Method(typeof(PixelWindow), "Show", new Type[3]
			{
				typeof(Vector2),
				typeof(Vector2),
				typeof(bool)
			}, (Type[])null) ?? throw new MissingMethodException("PixelWindow", "Show(Vector2, Vector2, bool)");
			yield return AccessTools.Method(typeof(PixelWindow), "Hide", Type.EmptyTypes, (Type[])null) ?? throw new MissingMethodException("PixelWindow", "Hide()");
		}

		private static void Postfix(PixelWindow __instance)
		{
			ContainerWindowResizer.WindowVisibilityChanged(__instance);
		}
	}
	public sealed class Core : MelonMod
	{
		private string _lastLocaleCode = string.Empty;

		private int _localeCheckCountdown;

		private bool _applicationQuitting;

		private int _patchReportCountdown = 120;

		private readonly Dictionary<string, int> _tickFailures = new Dictionary<string, int>(StringComparer.Ordinal);

		internal static Instance Log { get; private set; }

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			try
			{
				((MelonBase)this).HarmonyInstance.UnpatchSelf();
				PatchCompatibility.Install(((MelonBase)this).HarmonyInstance);
				CompatibilityBroker.Initialize();
			}
			catch (Exception ex)
			{
				Log.Error("Patch registration failed: " + ex);
			}
			TryCleanup("mod manager hotkey registration", ModManagerHotkey.Initialize);
			TryCleanup("custom skins", SkinManager.Initialize);
			_lastLocaleCode = ModLocalization.CurrentLocaleCode;
			Log.Msg("Twenty-three starting perks and two always-on features initialized. Locale: " + ((_lastLocaleCode.Length == 0) ? "default/en" : _lastLocaleCode));
		}

		public override void OnUpdate()
		{
			RunTick("game window", GameWindowResizer.Tick);
			if (PatchCompatibility.Available("perk-picker"))
			{
				RunTick("perk picker scroll", PerkPickerScroll.Tick);
			}
			RunTick("community overlay", CommunityOverlay.Tick);
			RunTick("mod manager", ModManagerUI.Tick);
			RunTick("mod manager capture", ModManagerUI.CaptureTick);
			RunTick("mod manager hotkey", ModManagerHotkey.Tick);
			RunTick("custom skins", SkinManager.Tick);
			RunTick("custom skin UI", SkinUI.Tick);
			RunTick("compatibility broker", CompatibilityBroker.Tick);
			if (PatchCompatibility.Available("starter-gift"))
			{
				RunTick("black-market sample", BlackMarketSample.Tick);
			}
			if (PatchCompatibility.Available("precision-bay"))
			{
				RunTick("precision bay", PrecisionBayMode.Tick);
			}
			if (PatchCompatibility.Available("loading"))
			{
				RunTick("loading progress", LoadingProgressOverlay.Tick);
			}
			if (_patchReportCountdown-- == 0)
			{
				RunTick("patch overlap report", () =>
				{
					PatchCompatibility.ReportSharedMethods();
				});
			}
			if (_localeCheckCountdown-- > 0)
			{
				return;
			}
			_localeCheckCountdown = 30;
			string currentLocaleCode;
			try
			{
				currentLocaleCode = ModLocalization.CurrentLocaleCode;
			}
			catch (Exception ex)
			{
				Instance log = Log;
				if (log != null)
				{
					log.Warning("Language check skipped: " + ex.Message);
				}
				return;
			}
			if (!string.Equals(currentLocaleCode, _lastLocaleCode, StringComparison.OrdinalIgnoreCase))
			{
				_lastLocaleCode = currentLocaleCode;
				RunTick("perk language", PerkRegistry.RefreshLocalizedText);
				RunTick("free-build language", FreeBuildMode.RefreshLocalizedText);
				RunTick("precision bay language", PrecisionBayMode.RefreshLocalizedText);
				RunTick("calendar language", () =>
				{
					StoreStation instance = StoreStation.instance;
					DeterministicSchedule.RefreshLocalizedText((instance != null) ? instance.storeEventManager : null);
				});
				RunTick("owner language", () =>
				{
					PlayerStore instance = PlayerStore.instance;
					OwnerDeal.RefreshLocalizedDialogue((instance != null) ? instance.storeClientManager : null);
				});
				Instance log2 = Log;
				if (log2 != null)
				{
					log2.Msg("Trade perk language changed to: " + ((currentLocaleCode.Length == 0) ? "default/en" : currentLocaleCode));
				}
			}
		}

		public override void OnLateUpdate()
		{
			if (PatchCompatibility.Available("save-manager"))
			{
				RunTick("save backup", SaveManager.Tick);
			}
			if (PatchCompatibility.Available("finance-report"))
			{
				RunTick("finance report", FinanceReport.Tick);
			}
			RunTick("container window", ContainerWindowResizer.Tick);
			RunTick("mod manager resize", ModManagerWindowResizer.Tick);
			if (PatchCompatibility.Available("save-manager"))
			{
				RunTick("save manager UI", SaveManagerUI.Tick);
			}
		}

		public override void OnApplicationQuit()
		{
			_applicationQuitting = true;
			OperatingSurcharge.DiscardTrackedServices();
		}

		public override void OnDeinitializeMelon()
		{
			if (_applicationQuitting)
			{
				OperatingSurcharge.DiscardTrackedServices();
			}
			else
			{
				TryCleanup("restore operating costs", OperatingSurcharge.RestoreKnownServices);
			}
			TryCleanup("game window resizer", GameWindowResizer.Dispose);
			TryCleanup("save manager UI", SaveManagerUI.Dispose);
			TryCleanup("save backups", SaveManager.Dispose);
			TryCleanup("finance report", FinanceReport.Dispose);
			TryCleanup("container window resizer", ContainerWindowResizer.Dispose);
			TryCleanup("free-build mode", FreeBuildMode.Dispose);
			TryCleanup("precision bay mode", PrecisionBayMode.Dispose);
			TryCleanup("mod manager", ModManagerUI.Dispose);
			TryCleanup("custom skin UI", SkinUI.Dispose);
			TryCleanup("custom skins", SkinManager.Dispose);
			TryCleanup("community overlay", CommunityOverlay.Dispose);
			TryCleanup("loading overlay", LoadingProgressOverlay.Dispose);
			TryCleanup("compatibility broker", CompatibilityBroker.Dispose);
			TryCleanup("feature patches", PatchCompatibility.Dispose);
		}

		private void RunTick(string name, Action action)
		{
			if (_tickFailures.TryGetValue(name, out var value) && value >= 3)
			{
				return;
			}
			try
			{
				action();
				if (value != 0)
				{
					_tickFailures.Remove(name);
				}
			}
			catch (Exception ex)
			{
				value++;
				_tickFailures[name] = value;
				if (value == 1 || value == 3)
				{
					Instance log = Log;
					if (log != null)
					{
						log.Warning(name + " failed" + ((value == 3) ? " three times; paused until restart" : "") + ": " + ex);
					}
				}
			}
		}

		private static void TryCleanup(string component, Action action)
		{
			try
			{
				action();
			}
			catch (Exception ex)
			{
				Instance log = Log;
				if (log != null)
				{
					log.Warning("Cleanup skipped for " + component + ": " + ex.Message);
				}
			}
		}
	}
	internal static class TradeEconomyEffects
	{
		private static readonly HashSet<string> SurchargedServiceIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "POWER_SERVICE", "JANITORIAL_SERVICE", "INSURANCE_SERVICE", "BUY_REFERAL", "SELL_REFERAL" };

		internal const int StableSupplyDiscountPercent = 10;

		internal const int ReputationBonusPercent = 20;

		internal const int NegotiationBonusPoints = 10;

		internal const int NegotiationChanceCap = 95;

		internal const int OperatingSurchargePercent = 20;

		[ThreadStatic]
		private static int _playerBuyingDepth;

		internal static bool IsPlayerBuying => _playerBuyingDepth > 0;

		internal static bool IsPurchasePreview(GameItem item)
		{
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Invalid comparison between Unknown and I4
			NegociationUIManager instance = NegociationUIManager.Instance;
			if (item != null && (Object)(object)((instance != null) ? instance.uiObject : null) != (Object)null && instance.uiObject.activeInHierarchy)
			{
				PlayerStore instance2 = PlayerStore.Instance;
				if (instance2 != null)
				{
					StoreClientInstance currentClientInstance = instance2.currentClientInstance;
					ClientIntent? val;
					if (currentClientInstance == null)
					{
						val = null;
					}
					else
					{
						StoreClient storeClient = currentClientInstance.storeClient;
						val = ((storeClient != null) ? new ClientIntent?(storeClient.clientIntent) : ((ClientIntent?)null));
					}
					ClientIntent? val2 = val;
					if ((int)val2.GetValueOrDefault() == 2)
					{
						return !GeneralHelper.IsItemOwned(item);
					}
				}
			}
			return false;
		}

		internal static void BeginPlayerPurchase()
		{
			_playerBuyingDepth++;
		}

		internal static void EndPlayerPurchase()
		{
			if (_playerBuyingDepth > 0)
			{
				_playerBuyingDepth--;
			}
		}

		internal static long StableSupplyPrice(long value)
		{
			if (value > 0)
			{
				return Math.Max(1L, (long)Math.Round((double)value * 0.9, MidpointRounding.AwayFromZero));
			}
			return value;
		}

		internal static double ReputationGain(double value)
		{
			if (!(value > 0.0))
			{
				return value;
			}
			return value * 1.2;
		}

		internal static int NegotiationChance(int value)
		{
			return Math.Min(95, Math.Max(0, value) + 10);
		}

		internal static int ServiceCost(int value)
		{
			if (value > 0)
			{
				return Math.Max(1, (int)Math.Ceiling((double)value * 1.2));
			}
			return value;
		}

		internal static bool IsOrdinarySupplier(StoreClient client)
		{
			//IL_0004: Unknown result type (might be due to invalid IL or missing references)
			//IL_000a: Invalid comparison between Unknown and I4
			if (client == null || (int)client.clientIntent != 2 || !client.CanReturn || client.isSecurity || client.isWanted || !string.IsNullOrEmpty(client.eventSourceId))
			{
				return false;
			}
			string text = client.identifier ?? string.Empty;
			if (!text.StartsWith("xiaowo_", StringComparison.OrdinalIgnoreCase))
			{
				return !text.StartsWith("retired_", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		internal static bool IsOrdinaryTradeClient(StoreClient client)
		{
			if (client == null || !client.CanReturn || client.isSecurity || client.isWanted || !string.IsNullOrEmpty(client.eventSourceId))
			{
				return false;
			}
			string text = client.identifier ?? string.Empty;
			if (!text.StartsWith("xiaowo_", StringComparison.OrdinalIgnoreCase))
			{
				return !text.StartsWith("retired_", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}

		internal static bool IsSurchargedService(StoreService service)
		{
			if (service != null)
			{
				return SurchargedServiceIds.Contains((service.id ?? string.Empty).TrimEnd('\0'));
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "BuyItem")]
	internal static class StableSupplyPurchaseContextPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			TradeEconomyEffects.BeginPlayerPurchase();
		}

		[HarmonyPriority(0)]
		private static Exception Finalizer(Exception __exception)
		{
			TradeEconomyEffects.EndPlayerPurchase();
			return __exception;
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "BuyItemFromTable")]
	internal static class StableSupplyTablePurchaseContextPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			TradeEconomyEffects.BeginPlayerPurchase();
		}

		[HarmonyPriority(0)]
		private static Exception Finalizer(Exception __exception)
		{
			TradeEconomyEffects.EndPlayerPurchase();
			return __exception;
		}
	}
	[HarmonyPatch(typeof(GameItem), "GetNegociatedValue")]
	internal static class StableSupplyPricePatch
	{
		private static void Postfix(GameItem __instance, ref long __result)
		{
			if ((TradeEconomyEffects.IsPlayerBuying || TradeEconomyEffects.IsPurchasePreview(__instance)) && PerkRegistry.IsActive("xiaowo_trade_stable_supply"))
			{
				PlayerStore instance = PlayerStore.Instance;
				object client;
				if (instance == null)
				{
					client = null;
				}
				else
				{
					StoreClientInstance currentClientInstance = instance.currentClientInstance;
					client = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
				}
				if (TradeEconomyEffects.IsOrdinarySupplier((StoreClient)client))
				{
					__result = TradeEconomyEffects.StableSupplyPrice(__result);
				}
			}
		}
	}
	[HarmonyPatch(typeof(StoreClient), "GetTradeRepMultiplier")]
	internal static class ReputationBusinessPatch
	{
		internal static int DiagnosticInvocationCount { get; private set; }

		internal static double DiagnosticOriginalResult { get; private set; }

		internal static double DiagnosticFinalResult { get; private set; }

		internal static void ResetDiagnostic()
		{
			DiagnosticInvocationCount = 0;
			DiagnosticOriginalResult = 0.0;
			DiagnosticFinalResult = 0.0;
		}

		private static void Postfix(StoreClient __instance, ref double __result)
		{
			DiagnosticInvocationCount++;
			DiagnosticOriginalResult = __result;
			if (PerkRegistry.IsActive("xiaowo_trade_reputation_business") && TradeEconomyEffects.IsOrdinaryTradeClient(__instance) && __result > 0.0)
			{
				__result = TradeEconomyEffects.ReputationGain(__result);
			}
			DiagnosticFinalResult = __result;
		}
	}
	[HarmonyPatch(typeof(BargainUIManager), "GetBargainSuccessChance")]
	internal static class NegotiationVeteranPatch
	{
		internal static int DiagnosticInvocationCount { get; private set; }

		internal static int DiagnosticOriginalResult { get; private set; }

		internal static int DiagnosticFinalResult { get; private set; }

		internal static void ResetDiagnostic()
		{
			DiagnosticInvocationCount = 0;
			DiagnosticOriginalResult = 0;
			DiagnosticFinalResult = 0;
		}

		private static void Postfix(ref int __result)
		{
			DiagnosticInvocationCount++;
			DiagnosticOriginalResult = __result;
			if (PerkRegistry.IsActive("xiaowo_trade_negotiation_veteran"))
			{
				__result = TradeEconomyEffects.NegotiationChance(__result);
			}
			DiagnosticFinalResult = __result;
		}
	}
	internal static class OperatingSurcharge
	{
		private readonly struct AppliedCost
		{
			internal int BaseCost { get; }

			internal int AdjustedCost { get; }

			internal AppliedCost(int baseCost, int adjustedCost)
			{
				BaseCost = baseCost;
				AdjustedCost = adjustedCost;
			}
		}

		private const int NativeReferralBaseCost = 10;

		private static readonly Dictionary<long, AppliedCost> AppliedCosts = new Dictionary<long, AppliedCost>();

		private static int _saveDepth;

		internal static void BeforeNativeUpdate(StoreService service)
		{
			if (service != null)
			{
				long key = ((Il2CppObjectBase)service).Pointer.ToInt64();
				if (AppliedCosts.TryGetValue(key, out var value) && service.cost == value.AdjustedCost)
				{
					service.cost = value.BaseCost;
				}
				AppliedCosts.Remove(key);
			}
		}

		internal static void AfterNativeUpdate(StoreService service)
		{
			if (service != null && PerkRegistry.IsActive("xiaowo_trade_operating_surcharge") && TradeEconomyEffects.IsSurchargedService(service))
			{
				ApplyCurrentCost(service);
			}
		}

		internal static void RefreshKnownServices()
		{
			if (PerkRegistry.IsActive("xiaowo_trade_operating_surcharge"))
			{
				ForEachKnownService(ApplyCurrentCost);
			}
		}

		internal static void RecalculateKnownServices()
		{
			AppliedCosts.Clear();
			if (!PerkRegistry.IsActive("xiaowo_trade_operating_surcharge"))
			{
				return;
			}
			ForEachKnownService((StoreService service) =>
			{
				string a = CleanId(service);
				if (string.Equals(a, "BUY_REFERAL", StringComparison.OrdinalIgnoreCase) || string.Equals(a, "SELL_REFERAL", StringComparison.OrdinalIgnoreCase))
				{
					service.cost = 10;
				}
				service.UpdateCost();
			});
		}

		internal static void RestoreKnownServices()
		{
			ForEachKnownService(RestoreCurrentCost);
		}

		internal static void DiscardTrackedServices()
		{
			AppliedCosts.Clear();
			_saveDepth = 0;
		}

		internal static void BeginSave()
		{
			if (_saveDepth++ == 0)
			{
				RestoreKnownServices();
			}
		}

		internal static void EndSave()
		{
			if (_saveDepth > 0)
			{
				_saveDepth--;
				if (_saveDepth == 0)
				{
					RefreshKnownServices();
				}
			}
		}

		private static void ApplyCurrentCost(StoreService service)
		{
			if (service != null)
			{
				long key = ((Il2CppObjectBase)service).Pointer.ToInt64();
				if (!AppliedCosts.TryGetValue(key, out var value) || service.cost != value.AdjustedCost)
				{
					int cost = service.cost;
					int adjustedCost = (service.cost = TradeEconomyEffects.ServiceCost(cost));
					AppliedCosts[key] = new AppliedCost(cost, adjustedCost);
				}
			}
		}

		private static void RestoreCurrentCost(StoreService service)
		{
			if (service != null)
			{
				long key = ((Il2CppObjectBase)service).Pointer.ToInt64();
				if (AppliedCosts.TryGetValue(key, out var value) && service.cost == value.AdjustedCost)
				{
					service.cost = value.BaseCost;
				}
				AppliedCosts.Remove(key);
			}
		}

		private static void ForEachKnownService(Action<StoreService> action)
		{
			HashSet<long> visited = new HashSet<long>();
			PlayerStore instance = PlayerStore.instance;
			List<StoreService> val = ((instance != null) ? instance.storeService : null);
			if (val != null)
			{
				for (int i = 0; i < val.Count; i++)
				{
					Visit(val[i]);
				}
			}
			Visit(StoreService.GetPowerService());
			Visit(StoreService.GetJanitorialService());
			Visit(StoreService.GetInsuranceService());
			Visit(StoreService.GetBuyReferalService());
			Visit(StoreService.GetSellReferalService());
			void Visit(StoreService service)
			{
				if (service != null && TradeEconomyEffects.IsSurchargedService(service))
				{
					long num = ((Il2CppObjectBase)service).Pointer.ToInt64();
					if (num != 0L && visited.Add(num))
					{
						action(service);
					}
				}
			}
		}

		private static string CleanId(StoreService service)
		{
			return (((service != null) ? service.id : null) ?? string.Empty).TrimEnd('\0');
		}
	}
	[HarmonyPatch(typeof(StoreService), "UpdateCost")]
	internal static class OperatingSurchargeCostPatch
	{
		private static void Prefix(StoreService __instance)
		{
			OperatingSurcharge.BeforeNativeUpdate(__instance);
		}

		private static void Postfix(StoreService __instance)
		{
			OperatingSurcharge.AfterNativeUpdate(__instance);
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "WriteSlotFile")]
	internal static class OperatingSurchargeSavePatch
	{
		private static void Prefix()
		{
			OperatingSurcharge.BeginSave();
		}

		private static Exception Finalizer(Exception __exception)
		{
			OperatingSurcharge.EndSave();
			return __exception;
		}
	}
	internal static class DeterministicSchedule
	{
		private const string CashflowPrefix = "xiaowo_trade_cashflow_";

		internal const string AppraisalCertificateId = "cert1";

		internal const string CashflowAllItemsType = "XIAOWO_ALL_ITEMS";

		internal const int CashflowDurationDays = 1;

		private static string _lastDailyGenerationKey = string.Empty;

		internal static int CurrentDay => Math.Max(0, StoreStation.GetDayCounter());

		internal static int CurrentWeek => Math.Max(0, (CurrentDay - 1) / 7);

		internal static bool IsCashflowWindow(out bool boom)
		{
			int currentDay = CurrentDay;
			int currentWeek = CurrentWeek;
			int cashflowStartDay = GetCashflowStartDay(currentWeek);
			boom = GetCashflowBoom(currentWeek);
			return currentDay == cashflowStartDay;
		}

		internal static void HandleDailyClients(StoreClientManager manager)
		{
			if (manager == null || PlayerStore.Instance == null)
			{
				return;
			}
			string text = GetRunKey() + "|" + CurrentDay;
			if (!string.Equals(_lastDailyGenerationKey, text, StringComparison.Ordinal))
			{
				_lastDailyGenerationKey = text;
				if (PerkRegistry.IsActive("xiaowo_trade_appraisal_sign"))
				{
					TryAddAppraisalClient(manager);
				}
				if (PerkRegistry.IsActive("xiaowo_trade_luxury_appointment"))
				{
					TryAddLuxuryClient(manager);
				}
			}
		}

		internal static void EnsureCashflowCalendar(StoreEventManager manager)
		{
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c8: Expected Obj, but got Unknown
			if (manager == null || !PerkRegistry.IsActive("xiaowo_trade_cashflow_gambler"))
			{
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (string.IsNullOrEmpty((instance != null) ? instance.runID : null))
			{
				return;
			}
			int currentWeek = CurrentWeek;
			int cashflowStartDay = GetCashflowStartDay(currentWeek);
			bool cashflowBoom = GetCashflowBoom(currentWeek);
			string id = "xiaowo_trade_cashflow_" + GetRunHash().ToString("x8") + "_" + currentWeek + (cashflowBoom ? "_boom" : "_bust");
			StoreEvent val = FindEvent(manager.activeEvents, id) ?? FindEvent(manager.futurEvents, id);
			if (val != null)
			{
				ConfigureCashflowEvent(val, id, cashflowStartDay, cashflowBoom);
			}
			else if (CurrentDay <= cashflowStartDay)
			{
				StoreEvent val2 = new StoreEvent();
				val2.InitNormalEvent();
				ConfigureCashflowEvent(val2, id, cashflowStartDay, cashflowBoom);
				int num = cashflowStartDay - CurrentDay;
				if (num <= 0)
				{
					manager.ActivateEvent(val2, false, true);
				}
				else
				{
					manager.QueueFuturEvent(val2, num, true);
				}
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Cashflow calendar scheduled: " + val2.displayName + " day " + cashflowStartDay);
				}
			}
		}

		private static void ConfigureCashflowEvent(StoreEvent storeEvent, string id, int startDay, bool boom)
		{
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Expected Obj, but got Unknown
			storeEvent.identifier = id;
			ApplyCashflowText(storeEvent, boom);
			storeEvent.eventType = (EventType)0;
			storeEvent.eventArea = (EventArea)2;
			storeEvent.duration = 1;
			storeEvent.totalDuration = 1;
			storeEvent.startDay = startDay;
			storeEvent.isPermanent = false;
			storeEvent.isHidden = false;
			storeEvent.isDurationVisible = true;
			storeEvent.IsKnown = true;
			storeEvent.isHiddenFromNewsPaper = false;
			storeEvent.negociationDatas.Clear();
			NegociationData val = new NegociationData("XIAOWO_ALL_ITEMS", boom ? 20 : (-20), GetCashflowDescription(boom), storeEvent.displayName);
			val.identifier = id + "_price";
			storeEvent.negociationDatas.Add(val);
		}

		internal static string GetCashflowEventName(string id)
		{
			if (string.IsNullOrEmpty(id) || !id.StartsWith("xiaowo_trade_cashflow_", StringComparison.Ordinal))
			{
				return null;
			}
			return GetCashflowName(IsBoomId(id));
		}

		internal static bool IsCashflowEvent(string id)
		{
			if (!string.IsNullOrEmpty(id))
			{
				return id.StartsWith("xiaowo_trade_cashflow_", StringComparison.Ordinal);
			}
			return false;
		}

		internal static string GetCashflowPriceEffect(string id)
		{
			if (!IsCashflowEvent(id))
			{
				return null;
			}
			if (!IsBoomId(id))
			{
				return ModLocalization.Select("-20% 所有物品", "-20% All Items");
			}
			return ModLocalization.Select("+20% 所有物品", "+20% All Items");
		}

		internal static string GetCashflowFeatureDisplay(string id)
		{
			if (!IsCashflowEvent(id))
			{
				return null;
			}
			bool boom = IsBoomId(id);
			return GetCashflowName(boom) + ModLocalization.Select("：", ": ") + GetCashflowDescription(boom);
		}

		internal static void RefreshLocalizedText(StoreEventManager manager)
		{
			if (manager != null)
			{
				RefreshLocalizedText(manager.activeEvents);
				RefreshLocalizedText(manager.futurEvents);
			}
		}

		private static void RefreshLocalizedText(List<StoreEvent> events)
		{
			if (events == null)
			{
				return;
			}
			for (int i = 0; i < events.Count; i++)
			{
				StoreEvent val = events[i];
				if (IsCashflowEvent((val != null) ? val.identifier : null))
				{
					ApplyCashflowText(val, IsBoomId(val.identifier));
				}
			}
		}

		private static void ApplyCashflowText(StoreEvent storeEvent, bool boom)
		{
			storeEvent.displayName = GetCashflowName(boom);
			storeEvent.newsName = storeEvent.displayName;
			storeEvent.newsDescription = ModLocalization.Select(boom ? "市场繁荣：所有物品价格上涨20%。" : "市场紧缩：所有物品价格下降20%。", boom ? "Market boom: all item prices increase by 20%." : "Market contraction: all item prices decrease by 20%.");
			if (storeEvent.negociationDatas == null)
			{
				return;
			}
			for (int i = 0; i < storeEvent.negociationDatas.Count; i++)
			{
				NegociationData val = storeEvent.negociationDatas[i];
				if (val != null && string.Equals(val.itemType, "XIAOWO_ALL_ITEMS", StringComparison.Ordinal))
				{
					val.publicDisplay = GetCashflowDescription(boom);
					val.reason = storeEvent.displayName;
				}
			}
		}

		private static string GetCashflowName(bool boom)
		{
			return ModLocalization.Select(boom ? "繁荣" : "紧缩", boom ? "Boom" : "Contraction");
		}

		private static string GetCashflowDescription(bool boom)
		{
			return ModLocalization.Select(boom ? "所有物品价格上涨20%。" : "所有物品价格下降20%。", boom ? "All item prices increase by 20%." : "All item prices decrease by 20%.");
		}

		private static bool IsBoomId(string id)
		{
			return id?.Contains("_boom", StringComparison.Ordinal) ?? false;
		}

		private static void TryAddAppraisalClient(StoreClientManager manager)
		{
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return;
			}
			bool certificateDisplayed = ShowcaseHelper.IsShowcaseContainItemById("cert1");
			int roll = PositiveModulo(StableHash(GetRunKey() + "|appraisal|" + CurrentDay), 100);
			if (!CanScheduleAppraisalClient(instance.isAppraisalIntroduced, certificateDisplayed, roll) || ContainsEventSource(manager.clientStack, "xiaowo_trade_appraisal_sign"))
			{
				return;
			}
			StoreClient val = StoreClientListAppraisal.CreateAppraisalRandom();
			if (val != null)
			{
				val.eventSourceId = "xiaowo_trade_appraisal_sign";
				manager.TryAddClient(val, false);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Appraisal sign scheduled an extra appraisal client on day " + CurrentDay + ".");
				}
			}
		}

		internal static bool CanScheduleAppraisalClient(bool appraisalIntroduced, bool certificateDisplayed, int roll)
		{
			if ((appraisalIntroduced & certificateDisplayed) && roll >= 0)
			{
				return roll < 30;
			}
			return false;
		}

		private static void TryAddLuxuryClient(StoreClientManager manager)
		{
			int currentWeek = CurrentWeek;
			int num = PositiveModulo(StableHash(GetRunKey() + "|luxury|" + currentWeek), 7) + 1;
			if ((CurrentDay - 1) % 7 + 1 != num || ContainsIdentifier(manager.clientStack, "xiaowo_trade_luxury_buyer"))
			{
				return;
			}
			StoreClient val = StoreClientList.CreateUpperLowerVisitorLuxury();
			if (val != null)
			{
				val.identifier = "xiaowo_trade_luxury_buyer";
				val.eventSourceId = "xiaowo_trade_luxury_buyer";
				val.CanReturn = false;
				val.CanReturnAsBuyer = false;
				val.OverrideBudget(GetAverageOrdinaryBudget(manager) * 3 / 2);
				manager.TryAddClient(val, true);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Luxury appointment scheduled on day " + CurrentDay + " with budget " + val.GetBudget() + ".");
				}
			}
		}

		private static int GetAverageOrdinaryBudget(StoreClientManager manager)
		{
			long num = 0L;
			int num2 = 0;
			List<StoreClient> clientStack = manager.clientStack;
			if (clientStack != null)
			{
				for (int i = 0; i < clientStack.Count; i++)
				{
					StoreClient val = clientStack[i];
					if (val != null && !val.isSecurity && !val.isWanted && string.IsNullOrEmpty(val.eventSourceId))
					{
						int budget = val.GetBudget();
						if (budget > 0)
						{
							num += budget;
							num2++;
						}
					}
				}
			}
			if (num2 > 0)
			{
				return Math.Max(1, (int)(num / num2));
			}
			StoreClient val2 = StoreClientList.CreateUpperLowerVisitorLuxury();
			if (val2 != null)
			{
				return Math.Max(1, val2.GetBudget());
			}
			return 250;
		}

		private static int GetCashflowStartDay(int week)
		{
			int num = PositiveModulo(StableHash(GetRunKey() + "|cashflow-day|" + week), 7) + 1;
			return week * 7 + num;
		}

		private static bool GetCashflowBoom(int week)
		{
			return (StableHash(GetRunKey() + "|cashflow-kind|" + week) & 1) == 0;
		}

		private static string GetRunKey()
		{
			PlayerStore instance = PlayerStore.Instance;
			string text = ((instance != null) ? instance.runID : null);
			if (!string.IsNullOrEmpty(text))
			{
				return text;
			}
			return "no-run";
		}

		private static int GetRunHash()
		{
			return StableHash(GetRunKey());
		}

		private static int StableHash(string value)
		{
			int num = -2128831035;
			for (int i = 0; i < value.Length; i++)
			{
				num = (num ^ value[i]) * 16777619;
			}
			return num;
		}

		private static int PositiveModulo(int value, int modulo)
		{
			int num = value % modulo;
			if (num >= 0)
			{
				return num;
			}
			return num + modulo;
		}

		private static StoreEvent FindEvent(List<StoreEvent> events, string id)
		{
			if (events == null)
			{
				return null;
			}
			for (int i = 0; i < events.Count; i++)
			{
				if (events[i] != null && string.Equals(events[i].identifier, id, StringComparison.Ordinal))
				{
					return events[i];
				}
			}
			return null;
		}

		private static bool ContainsIdentifier(List<StoreClient> clients, string id)
		{
			if (clients == null)
			{
				return false;
			}
			for (int i = 0; i < clients.Count; i++)
			{
				if (clients[i] != null && string.Equals(clients[i].identifier, id, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private static bool ContainsEventSource(List<StoreClient> clients, string source)
		{
			if (clients == null)
			{
				return false;
			}
			for (int i = 0; i < clients.Count; i++)
			{
				if (clients[i] != null && string.Equals(clients[i].eventSourceId, source, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}
	}
	internal static class TradeEffects
	{
		internal const string LuxuryClientId = "xiaowo_trade_luxury_buyer";

		internal const string StolenHeatTag = "STOLEN_VALUE_INT";

		private static int _playerSellingDepth;

		private static long _insuredVisitPointer;

		private static string _insuredRunWeek = string.Empty;

		internal static bool IsPlayerSelling => _playerSellingDepth > 0;

		internal static void BeginPlayerSale()
		{
			_playerSellingDepth++;
		}

		internal static void EndPlayerSale()
		{
			if (_playerSellingDepth > 0)
			{
				_playerSellingDepth--;
			}
		}

		internal static bool IsSalePreview(GameItem item)
		{
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Invalid comparison between Unknown and I4
			NegociationUIManager instance = NegociationUIManager.Instance;
			if (item != null && (Object)(object)((instance != null) ? instance.uiObject : null) != (Object)null && instance.uiObject.activeInHierarchy)
			{
				PlayerStore instance2 = PlayerStore.Instance;
				if (instance2 != null)
				{
					StoreClientInstance currentClientInstance = instance2.currentClientInstance;
					ClientIntent? val;
					if (currentClientInstance == null)
					{
						val = null;
					}
					else
					{
						StoreClient storeClient = currentClientInstance.storeClient;
						val = ((storeClient != null) ? new ClientIntent?(storeClient.clientIntent) : ((ClientIntent?)null));
					}
					ClientIntent? val2 = val;
					if ((int)val2.GetValueOrDefault() == 1)
					{
						return GeneralHelper.IsItemOwned(item);
					}
				}
			}
			return false;
		}

		internal static long ApplySaleModifiers(long value, bool preview = false)
		{
			if (value <= 0)
			{
				return value;
			}
			double num = 1.0;
			if (IsPlayerSelling | preview)
			{
				StoreStation instance = StoreStation.instance;
				if (PerkRegistry.IsActive("xiaowo_trade_blackout_broker") && ((instance != null) ? instance.storeEventManager : null) != null && instance.storeEventManager.IsBlackoutEventActive())
				{
					num *= 1.1;
				}
				PlayerStore instance2 = PlayerStore.Instance;
				object obj;
				if (instance2 == null)
				{
					obj = null;
				}
				else
				{
					StoreClientInstance currentClientInstance = instance2.currentClientInstance;
					obj = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
				}
				StoreClient val = (StoreClient)obj;
				if (val != null && string.Equals(val.identifier, "xiaowo_trade_luxury_buyer", StringComparison.Ordinal))
				{
					num *= 0.9;
				}
			}
			return Math.Max(1L, (long)Math.Round((double)value * num, MidpointRounding.AwayFromZero));
		}

		internal static long CashflowAdjusted(long value, bool boom)
		{
			if (value > 0)
			{
				return Math.Max(1L, (long)Math.Round((double)value * (boom ? 1.2 : 0.8), MidpointRounding.AwayFromZero));
			}
			return value;
		}

		internal static int AppraisalBonus(int actualFee)
		{
			if (actualFee > 0)
			{
				return Math.Max(1, (int)Math.Round((double)actualFee * 0.2, MidpointRounding.AwayFromZero));
			}
			return 0;
		}

		internal static int AppraisalDisplayedFee(int fee, int baseFee)
		{
			long num = Math.Max(0L, (long)fee + (long)baseFee);
			int num2 = (int)((num > int.MaxValue) ? int.MaxValue : num);
			if (num2 > 0)
			{
				return (int)Math.Min(2147483647L, (long)fee + (long)AppraisalBonus(num2));
			}
			return fee;
		}

		internal static int InsuredHeat(int heat)
		{
			return Math.Max(0, heat / 2);
		}

		internal static int SecurityTargetHeat(int heat)
		{
			if (heat > 0)
			{
				return Math.Max(1, (int)Math.Ceiling((double)heat * 1.25));
			}
			return heat;
		}

		internal static int ShortenedCooldown(int days)
		{
			return Math.Max(1, (int)Math.Floor((double)days * 0.75));
		}

		internal static int RundownAttractiveness(int attractiveness)
		{
			return Math.Max(0, attractiveness - 150);
		}

		internal static int RenovationCost(int cost)
		{
			return Math.Max(0, (int)Math.Ceiling((double)cost * 1.2));
		}

		internal static int ReturningBudget(int budget)
		{
			if (budget > 0)
			{
				return Math.Max(1, (int)Math.Floor((double)budget * 0.75));
			}
			return budget;
		}

		internal static void ApplyGreyInsurance(GameItem item)
		{
			if (item == null || !PerkRegistry.IsActive("xiaowo_trade_grey_insurance") || !StolenHelper.IsStolenItem(item))
			{
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			StoreClientInstance val = ((instance != null) ? instance.currentClientInstance : null);
			StoreClient val2 = ((val != null) ? val.storeClient : null);
			if (instance == null || val == null || val2 == null)
			{
				return;
			}
			string text = (instance.runID ?? "no-run") + "|" + DeterministicSchedule.CurrentWeek;
			if (!string.Equals(_insuredRunWeek, text, StringComparison.Ordinal))
			{
				_insuredRunWeek = text;
				_insuredVisitPointer = 0L;
			}
			string text2 = "XIAOWOTradePerks.GreyInsurance." + text;
			string text3 = PlayerPrefs.GetString(text2, string.Empty);
			if (_insuredVisitPointer == 0L)
			{
				if (!string.IsNullOrEmpty(text3) && !string.Equals(text3, val2.identifier, StringComparison.Ordinal))
				{
					return;
				}
				_insuredVisitPointer = ((Il2CppObjectBase)val).Pointer.ToInt64();
				if (string.IsNullOrEmpty(text3))
				{
					PlayerPrefs.SetString(text2, val2.identifier ?? string.Empty);
					PlayerPrefs.Save();
				}
			}
			if (_insuredVisitPointer != ((Il2CppObjectBase)val).Pointer.ToInt64())
			{
				return;
			}
			TagState tagReadonly = item.GetTagReadonly("STOLEN_VALUE_INT");
			if (tagReadonly == null)
			{
				return;
			}
			int num = tagReadonly.GetInt();
			int num2 = InsuredHeat(num);
			if (num2 != num)
			{
				StolenHelper.SetHeat(item, num2);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Grey insurance reduced stolen heat " + num + " -> " + num2 + ".");
				}
			}
		}

		internal static bool IsOrdinaryReturningClient(StoreClient client)
		{
			if (client == null || string.IsNullOrEmpty(client.identifier))
			{
				return false;
			}
			if (!client.CanReturn || client.isSecurity || client.isWanted)
			{
				return false;
			}
			if (!string.IsNullOrEmpty(client.eventSourceId))
			{
				return false;
			}
			if (client.identifier.StartsWith("retired_", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			return StoreClientManager.GetCustomerTotalVisitedCount(client.identifier) > 0;
		}

		internal static bool IsRenovation(string id)
		{
			if (!string.IsNullOrEmpty(id))
			{
				return id.StartsWith("RENOVATION", StringComparison.OrdinalIgnoreCase);
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "HandleNormalClient")]
	internal static class DailyClientGenerationPatch
	{
		private static void Postfix(StoreClientManager __instance)
		{
			DeterministicSchedule.HandleDailyClients(__instance);
		}
	}
	[HarmonyPatch(typeof(StoreEventManager), "OnDayStart")]
	internal static class CashflowCalendarPatch
	{
		private static void Postfix(StoreEventManager __instance)
		{
			DeterministicSchedule.EnsureCashflowCalendar(__instance);
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "StartNewGame")]
	internal static class NewGameCashflowPatch
	{
		private static void Postfix()
		{
			PerkRegistry.EnsureRegistered();
			OperatingSurcharge.RecalculateKnownServices();
			StoreStation instance = StoreStation.instance;
			StoreEventManager val = ((instance != null) ? instance.storeEventManager : null);
			if (val != null)
			{
				DeterministicSchedule.EnsureCashflowCalendar(val);
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class LoadGameCashflowPatch
	{
		private static void Postfix()
		{
			PerkRegistry.EnsureRegistered();
			OperatingSurcharge.RecalculateKnownServices();
			StoreStation instance = StoreStation.instance;
			StoreEventManager val = ((instance != null) ? instance.storeEventManager : null);
			if (val != null)
			{
				DeterministicSchedule.EnsureCashflowCalendar(val);
			}
		}
	}
	[HarmonyPatch(typeof(StoreEvent), "GetDisplayName")]
	internal static class CashflowEventNamePatch
	{
		private static void Postfix(StoreEvent __instance, ref string __result)
		{
			string cashflowEventName = DeterministicSchedule.GetCashflowEventName((__instance != null) ? __instance.identifier : null);
			if (cashflowEventName != null)
			{
				__result = cashflowEventName;
			}
		}
	}
	[HarmonyPatch(typeof(StoreEvent), "GetEventPriceEffect")]
	internal static class CashflowEventPriceEffectPatch
	{
		private static void Postfix(StoreEvent __instance, ref string __result)
		{
			string cashflowPriceEffect = DeterministicSchedule.GetCashflowPriceEffect((__instance != null) ? __instance.identifier : null);
			if (cashflowPriceEffect != null)
			{
				__result = cashflowPriceEffect;
			}
		}
	}
	[HarmonyPatch(typeof(StoreEvent), "IsItemAffectedByThisEvent")]
	internal static class CashflowAllItemsAffectedPatch
	{
		private static void Postfix(StoreEvent __instance, ref bool __result)
		{
			if (DeterministicSchedule.IsCashflowEvent((__instance != null) ? __instance.identifier : null))
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(ItemFeature), "CreateAndAddFeatureFromEvent")]
	internal static class CashflowAllItemsFeaturePatch
	{
		private static bool Prefix(GameItem gameItem, StoreEvent storeEvent)
		{
			if (!DeterministicSchedule.IsCashflowEvent((storeEvent != null) ? storeEvent.identifier : null))
			{
				return true;
			}
			if (gameItem == null || storeEvent.negociationDatas == null || storeEvent.negociationDatas.Count == 0)
			{
				return false;
			}
			NegociationData val = storeEvent.negociationDatas[0];
			if (val == null || gameItem.IsAlreadyContainFeatureWithId(val.identifier))
			{
				return false;
			}
			ItemFeature val2 = ItemFeature.CreateFeatureFromNegociationData(val);
			if (val2 == null)
			{
				return false;
			}
			string cashflowFeatureDisplay = DeterministicSchedule.GetCashflowFeatureDisplay(storeEvent.identifier);
			val2.SetPublicDisplay(cashflowFeatureDisplay);
			val2.SetActualDisplay(cashflowFeatureDisplay);
			gameItem.AddItemFeature(val2);
			return false;
		}
	}
	[HarmonyPatch(typeof(ItemFeature), "GetPublicDisplay")]
	internal static class CashflowPublicFeatureDisplayPatch
	{
		private static void Postfix(ItemFeature __instance, ref string __result)
		{
			string cashflowFeatureDisplay = DeterministicSchedule.GetCashflowFeatureDisplay((__instance != null) ? __instance.identifier : null);
			if (cashflowFeatureDisplay != null)
			{
				__result = cashflowFeatureDisplay;
			}
		}
	}
	[HarmonyPatch(typeof(ItemFeature), "GetActualDisplay")]
	internal static class CashflowActualFeatureDisplayPatch
	{
		private static void Postfix(ItemFeature __instance, ref string __result)
		{
			string cashflowFeatureDisplay = DeterministicSchedule.GetCashflowFeatureDisplay((__instance != null) ? __instance.identifier : null);
			if (cashflowFeatureDisplay != null)
			{
				__result = cashflowFeatureDisplay;
			}
		}
	}
	[HarmonyPatch(typeof(AppraisalServiceHelper), "GiveAppraisalFee")]
	internal static class AppraisalFeePatch
	{
		private static void Prefix(out int __state)
		{
			__state = ((PlayerStore.Instance != null) ? PlayerStore.Instance.GetCash() : 0);
		}

		private static void Postfix(int __state)
		{
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null || !PerkRegistry.IsActive("xiaowo_trade_gold_appraiser"))
			{
				return;
			}
			int num = instance.GetCash() - __state;
			if (num > 0)
			{
				int num2 = TradeEffects.AppraisalBonus(num);
				instance.ModCash(num2, false);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Gold appraiser bonus: " + num2 + " (base service fee " + num + ").");
				}
			}
		}
	}
	[HarmonyPatch(typeof(AppraisalServiceHelper), "SetString")]
	internal static class AppraisalFeeDisplayPatch
	{
		private static void Prefix(ref int fee, ref int baseFee)
		{
			if (PerkRegistry.IsActive("xiaowo_trade_gold_appraiser"))
			{
				fee = TradeEffects.AppraisalDisplayedFee(fee, baseFee);
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "SellItem")]
	internal static class PlayerSaleContextPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			TradeEffects.BeginPlayerSale();
		}

		[HarmonyPriority(0)]
		private static Exception Finalizer(Exception __exception)
		{
			TradeEffects.EndPlayerSale();
			return __exception;
		}
	}
	[HarmonyPatch(typeof(GameItem), "GetNegociatedValue")]
	internal static class NegotiatedValuePatch
	{
		private static void Postfix(GameItem __instance, ref long __result)
		{
			__result = TradeEffects.ApplySaleModifiers(__result, TradeEffects.IsSalePreview(__instance));
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "OnItemBought")]
	internal static class GreyInsurancePatch
	{
		private static void Postfix(GameItem gameItem)
		{
			TradeEffects.ApplyGreyInsurance(gameItem);
		}
	}
	[HarmonyPatch(typeof(StolenHelper), "InitStolenItem")]
	internal static class SecurityTargetHeatPatch
	{
		private static void Postfix(GameItem item)
		{
			if (item == null || !PerkRegistry.IsActive("xiaowo_trade_security_target"))
			{
				return;
			}
			TagState tagReadonly = item.GetTagReadonly("STOLEN_VALUE_INT");
			if (tagReadonly != null)
			{
				int num = tagReadonly.GetInt();
				if (num > 0)
				{
					StolenHelper.SetHeat(item, TradeEffects.SecurityTargetHeat(num));
				}
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "ComputeInspectionCooldownRange")]
	internal static class SecurityTargetInspectionPatch
	{
		private static void Postfix(ref int min, ref int max)
		{
			if (PerkRegistry.IsActive("xiaowo_trade_security_target"))
			{
				min = TradeEffects.ShortenedCooldown(min);
				max = Math.Max(min, TradeEffects.ShortenedCooldown(max));
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "GetCurrentStoreAttractiveness")]
	internal static class RundownAttractivenessPatch
	{
		private static void Postfix(ref int __result)
		{
			if (PerkRegistry.IsActive("xiaowo_trade_rundown_storefront"))
			{
				__result = TradeEffects.RundownAttractiveness(__result);
			}
		}
	}
	[HarmonyPatch(typeof(NetworkUpgrade), "GetCost")]
	internal static class RenovationCostPatch
	{
		private static void Postfix(NetworkUpgrade __instance, ref int __result)
		{
			if (__instance != null && TradeEffects.IsRenovation(__instance.id) && PerkRegistry.IsActive("xiaowo_trade_rundown_storefront"))
			{
				__result = TradeEffects.RenovationCost(__result);
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "TrackClient")]
	internal static class RegularCreditPatch
	{
		private static readonly HashSet<long> Processed = new HashSet<long>();

		private static void Prefix(StoreClient storeClient)
		{
			if (!PerkRegistry.IsActive("xiaowo_trade_regular_credit") || !TradeEffects.IsOrdinaryReturningClient(storeClient))
			{
				return;
			}
			long item = ((Il2CppObjectBase)storeClient).Pointer.ToInt64();
			if (Processed.Add(item))
			{
				int budget = storeClient.GetBudget();
				if (budget > 0)
				{
					storeClient.OverrideBudget(TradeEffects.ReturningBudget(budget));
				}
			}
		}
	}
	internal static class NightScavengingRisk
	{
		private enum WoundRollPhase
		{
			None,
			Minor,
			Major
		}

		internal const float WoundChanceBonus = 15f;

		[ThreadStatic]
		private static WoundRollPhase _nextRoll;

		[ThreadStatic]
		private static bool _majorRollEligible;

		internal static float AddToChance(float chance, bool active)
		{
			if (!active)
			{
				return chance;
			}
			return Math.Min(100f, chance + 15f);
		}

		internal static void Begin()
		{
			End();
			if (!PerkRegistry.IsActive("xiaowo_trade_reckless_scavenger"))
			{
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (instance != null)
			{
				_majorRollEligible = instance.dumpingGroundMajorWoundImmunitySearchesLeft <= 0;
				if (instance.dumpingGroundMinorWoundImmunitySearchesLeft <= 0)
				{
					_nextRoll = WoundRollPhase.Minor;
				}
				else if (_majorRollEligible)
				{
					_nextRoll = WoundRollPhase.Major;
				}
			}
		}

		internal static void BeginForTest(bool minorRollEligible, bool majorRollEligible)
		{
			End();
			_majorRollEligible = majorRollEligible;
			_nextRoll = (minorRollEligible ? WoundRollPhase.Minor : (majorRollEligible ? WoundRollPhase.Major : WoundRollPhase.None));
		}

		internal static float AdjustNativeWoundRoll(float value, float minimum, float maximum)
		{
			if (_nextRoll == WoundRollPhase.None || Math.Abs(minimum) > 0.001f || Math.Abs(maximum - 100f) > 0.001f)
			{
				return value;
			}
			value -= 15f;
			if (_nextRoll == WoundRollPhase.Minor && _majorRollEligible)
			{
				_nextRoll = WoundRollPhase.Major;
			}
			else
			{
				End();
			}
			return value;
		}

		internal static void End()
		{
			_nextRoll = WoundRollPhase.None;
			_majorRollEligible = false;
		}
	}
	[HarmonyPatch(typeof(ScavHelper), "GetMinorWoundChance")]
	internal static class MinorWoundChancePatch
	{
		private static void Postfix(ref float __result)
		{
			__result = NightScavengingRisk.AddToChance(__result, PerkRegistry.IsActive("xiaowo_trade_reckless_scavenger"));
		}
	}
	[HarmonyPatch(typeof(ScavHelper), "GetMajorWoundChance")]
	internal static class MajorWoundChancePatch
	{
		private static void Postfix(ref float __result)
		{
			__result = NightScavengingRisk.AddToChance(__result, PerkRegistry.IsActive("xiaowo_trade_reckless_scavenger"));
		}
	}
	[HarmonyPatch(typeof(ScavHelper), "ScavengeDumpingGrounds")]
	internal static class NightScavengingContextPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			NightScavengingRisk.Begin();
		}

		[HarmonyPriority(0)]
		private static Exception Finalizer(Exception __exception)
		{
			NightScavengingRisk.End();
			return __exception;
		}
	}
	[HarmonyPatch(typeof(Random), "Range", new Type[]
	{
		typeof(float),
		typeof(float)
	})]
	internal static class NightScavengingRandomPatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(float __0, float __1, ref float __result)
		{
			__result = NightScavengingRisk.AdjustNativeWoundRoll(__result, __0, __1);
		}
	}
	internal static class FinanceReport
	{
		internal const string SaveKey = "xiaowo_trade_cash_ledger_v1";

		private static PlayerStore _store;

		private static CashLedger _ledger;

		private static bool _suspended;

		private static bool _failed;

		private static RuntimePollGate _fallbackPoll;

		internal static CashLedger Ledger => _ledger;

		internal static void Suspend()
		{
			_suspended = true;
			_store = null;
			_ledger = null;
			_failed = false;
			_fallbackPoll.Reset();
		}

		internal static void Resume(bool newGame)
		{
			_suspended = false;
			SafeObserve(newGame);
		}

		internal static void SafeObserve(bool newGame = false)
		{
			if (_suspended)
			{
				return;
			}
			try
			{
				Observe(newGame);
			}
			catch (Exception ex)
			{
				if (!_failed)
				{
					Instance log = Core.Log;
					if (log != null)
					{
						log.Error("收支统计暂停，原有存档记录未覆盖：" + ex.Message);
					}
				}
				_failed = true;
			}
		}

		private static void Observe(bool newGame)
		{
			PlayerStore instance = PlayerStore.instance;
			if (instance == null || StoreStation.instance == null || string.IsNullOrEmpty(instance.runID))
			{
				return;
			}
			if (_store == null || ((Il2CppObjectBase)_store).Pointer != ((Il2CppObjectBase)instance).Pointer)
			{
				_store = instance;
				_ledger = null;
				_failed = false;
				int day = Math.Max(0, StoreStation.GetDayCounter());
				string text = default;
				if (instance.modData != null && instance.modData.TryGetValue("xiaowo_trade_cash_ledger_v1", ref text))
				{
					_ledger = CashLedger.Parse(text);
					_ledger.LastCash = instance.playerCash;
				}
				else
				{
					_ledger = CashLedger.Create(day, instance.playerCash, !newGame);
				}
				Persist();
			}
			if (!_failed && _ledger != null && _ledger.Observe(Math.Max(0, StoreStation.GetDayCounter()), instance.playerCash))
			{
				Persist();
			}
		}

		private static void Persist()
		{
			PlayerStore store = _store;
			if (store.modData == null)
			{
				Dictionary<string, string> val = (store.modData = new Dictionary<string, string>());
			}
			_store.modData["xiaowo_trade_cash_ledger_v1"] = _ledger.Serialize();
		}

		internal static void Tick()
		{
			if (_fallbackPoll.Due(250))
			{
				SafeObserve();
			}
			FinanceReportUI.Tick();
		}

		internal static void Dispose()
		{
			SafeObserve();
			FinanceReportUI.Dispose();
			_store = null;
			_ledger = null;
			_suspended = false;
			_failed = false;
			_fallbackPoll.Reset();
		}
	}
	[HarmonyPatch]
	internal static class FinanceReportCashPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			string[] array = new string[10] { "ModCash", "PayMoney", "BuyItem", "SellItem", "EndNight", "HandleJanitorial", "HandlePower", "HandleInsurance", "HandleOtherServices", "SaveGame" };
			foreach (string text in array)
			{
				yield return AccessTools.Method(typeof(PlayerStore), text, (Type[])null, (Type[])null) ?? throw new MissingMethodException(typeof(PlayerStore).FullName, text);
			}
			yield return AccessTools.Method(typeof(StoreStation), "StartDay", (Type[])null, (Type[])null);
		}

		[HarmonyPriority(800)]
		private static void Prefix()
		{
			FinanceReport.SafeObserve();
		}

		[HarmonyPriority(0)]
		private static void Finalizer()
		{
			FinanceReport.SafeObserve();
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class FinanceReportLoadPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			FinanceReport.Suspend();
		}

		[HarmonyPriority(0)]
		private static void Postfix()
		{
			FinanceReport.Resume(newGame: false);
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "StartNewGame")]
	internal static class FinanceReportNewGamePatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			FinanceReport.Suspend();
		}

		[HarmonyPriority(0)]
		private static void Postfix()
		{
			FinanceReport.Resume(newGame: true);
		}
	}
	internal static class FinanceReportUI
	{
		private static GameObject _root;

		private static RectTransform _content;

		private static RectTransform _viewport;

		private static Transform _originalParent;

		private static Vector2 _anchorMin;

		private static Vector2 _anchorMax;

		private static Vector2 _pivot;

		private static Vector2 _size;

		private static Vector2 _position;

		private static int _sibling;

		private static ScrollRect _scroll;

		private static Scrollbar _bar;

		private static TextMeshProUGUI _yesterday;

		private static TextMeshProUGUI _total;

		private static bool _requestTop;

		private static bool _wasVisible;

		private static int _countdown;

		private static string _signature;

		private static bool _controlHeight;

		private static bool _expandHeight;

		private static bool _controlWidth;

		private static bool _expandWidth;

		internal static ScrollRect Scroll => _scroll;

		internal static Scrollbar Bar => _bar;

		internal static RectTransform Content => _content;

		internal static void RequestTop()
		{
			_requestTop = true;
			_countdown = 0;
		}

		internal static void Rebuilt()
		{
			_signature = null;
			_countdown = 0;
		}

		internal static void Tick()
		{
			//IL_01d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d7: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c8: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0404: Unknown result type (might be due to invalid IL or missing references)
			//IL_0414: Unknown result type (might be due to invalid IL or missing references)
			//IL_036c: Unknown result type (might be due to invalid IL or missing references)
			AdvCalendarUIManager instance = AdvCalendarUIManager.Instance;
			if (!((Object)(object)instance != (Object)null) || !((Object)(object)instance.UIPanel != (Object)null) || !instance.UIPanel.activeInHierarchy || !((Object)(object)instance.nightlyReportSection != (Object)null) || !instance.nightlyReportSection.activeInHierarchy)
			{
				_wasVisible = false;
				return;
			}
			if (!_wasVisible)
			{
				RequestTop();
			}
			_wasVisible = true;
			if (_countdown-- > 0)
			{
				return;
			}
			_countdown = 10;
			StartOfDayUIManager instance2 = StartOfDayUIManager.Instance;
			if ((Object)(object)instance2 == (Object)null || (Object)(object)instance2.contentGroupObject == (Object)null || (Object)(object)instance2.startOfDayTMPPrefab == (Object)null)
			{
				return;
			}
			if ((Object)(object)_root == (Object)null || (Object)(object)_content == (Object)null)
			{
				Create(instance2);
			}
			if ((Object)(object)_root == (Object)null)
			{
				return;
			}
			CashLedger ledger = FinanceReport.Ledger;
			if (ledger == null)
			{
				return;
			}
			TextMeshProUGUI component = instance2.startOfDayTMPPrefab.GetComponent<TextMeshProUGUI>();
			if ((Object)(object)component == (Object)null)
			{
				return;
			}
			_yesterday = EnsureLine(_yesterday, "XIAOWO_YesterdayCash", instance2.startOfDayTMPPrefab);
			_total = EnsureLine(_total, "XIAOWO_CumulativeCash", instance2.startOfDayTMPPrefab);
			bool chinese = ModLocalization.IsChinese(ModLocalization.CurrentLocaleCode);
			SetText(_yesterday, "-" + ledger.Yesterday(chinese));
			SetText(_total, "-" + ledger.Cumulative(chinese));
			Transform transform = ((TMP_Text)_yesterday).transform;
			Transform transform2 = ((TMP_Text)_total).transform;
			if (transform.GetSiblingIndex() != ((Transform)_content).childCount - 2 || transform2.GetSiblingIndex() != ((Transform)_content).childCount - 1)
			{
				transform.SetAsLastSibling();
				transform2.SetAsLastSibling();
			}
			Il2CppArrayBase<TextMeshProUGUI> componentsInChildren = ((Component)_content).GetComponentsInChildren<TextMeshProUGUI>(false);
			StringBuilder stringBuilder = new StringBuilder();
			Rect rect = _viewport.rect;
			StringBuilder stringBuilder2 = stringBuilder.Append(rect.width).Append(':');
			rect = _viewport.rect;
			StringBuilder stringBuilder3 = stringBuilder2.Append(rect.height).Append(':').Append(componentsInChildren.Length);
			foreach (TextMeshProUGUI item in componentsInChildren)
			{
				stringBuilder3.Append('|').Append(((TMP_Text)item).text).Append(':')
					.Append(((TMP_Text)item).fontSize)
					.Append(':')
					.Append(((TMP_Text)item).enableAutoSizing);
			}
			string text = stringBuilder3.ToString();
			if (!(_signature != text) && !_requestTop)
			{
				return;
			}
			float verticalNormalizedPosition = _scroll.verticalNormalizedPosition;
			VerticalLayoutGroup component2 = ((Component)_content).GetComponent<VerticalLayoutGroup>();
			rect = _viewport.rect;
			float num = Math.Max(1f, rect.width - (float)((LayoutGroup)component2).padding.left - (float)((LayoutGroup)component2).padding.right);
			float num2 = ((LayoutGroup)component2).padding.top + ((LayoutGroup)component2).padding.bottom;
			foreach (TextMeshProUGUI item2 in componentsInChildren)
			{
				((TMP_Text)item2).enableAutoSizing = false;
				((TMP_Text)item2).fontSize = (((TMP_Text)component).enableAutoSizing ? ((TMP_Text)component).fontSizeMax : ((TMP_Text)component).fontSize);
				((TMP_Text)item2).enableWordWrapping = true;
				((TMP_Text)item2).overflowMode = (TextOverflowModes)0;
				float num3 = ((TMP_Text)item2).GetPreferredValues(((TMP_Text)item2).text, num, float.PositiveInfinity).y + 4f;
				LayoutElement val = ((Component)item2).GetComponent<LayoutElement>() ?? ((Component)item2).gameObject.AddComponent<LayoutElement>();
				float minHeight = (val.preferredHeight = num3);
				val.minHeight = minHeight;
				val.flexibleHeight = 0f;
				num2 += num3;
			}
			num2 += (float)Math.Max(0, componentsInChildren.Length - 1) * ((HorizontalOrVerticalLayoutGroup)component2).spacing;
			RectTransform content = _content;
			rect = _viewport.rect;
			content.sizeDelta = new Vector2(0f, Math.Max(rect.height, num2));
			LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
			Canvas.ForceUpdateCanvases();
			_scroll.StopMovement();
			_scroll.verticalNormalizedPosition = (_requestTop ? 1f : Mathf.Clamp01(verticalNormalizedPosition));
			_requestTop = false;
			_signature = text;
		}

		private static void SetText(TextMeshProUGUI text, string value)
		{
			if (!string.Equals(((TMP_Text)text).text, value, StringComparison.Ordinal))
			{
				((TMP_Text)text).text = value;
			}
		}

		private static TextMeshProUGUI EnsureLine(TextMeshProUGUI text, string name, GameObject prefab)
		{
			if ((Object)(object)text != (Object)null && (Object)(object)((TMP_Text)text).transform.parent == (Object)(object)_content)
			{
				return text;
			}
			GameObject val = Object.Instantiate<GameObject>(prefab, (Transform)(object)_content, false);
			((Object)val).name = name;
			val.SetActive(true);
			text = val.GetComponent<TextMeshProUGUI>();
			((Graphic)text).raycastTarget = false;
			((TMP_Text)text).richText = true;
			return text;
		}

		private static RectTransform MakeRect(string name, Transform parent)
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[1] { Il2CppType.Of<RectTransform>() });
			val.transform.SetParent(parent, false);
			return val.GetComponent<RectTransform>();
		}

		private static void Fill(RectTransform rect, Vector2 min, Vector2 max)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.offsetMin = min;
			rect.offsetMax = max;
		}

		private static Image ImageOn(RectTransform rect, Color color)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			Image val = ((Component)rect).gameObject.AddComponent<Image>();
			((Graphic)val).color = color;
			return val;
		}

		private static void Create(StartOfDayUIManager manager)
		{
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_006a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_0097: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			//IL_012c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_0156: Unknown result type (might be due to invalid IL or missing references)
			//IL_0165: Unknown result type (might be due to invalid IL or missing references)
			//IL_0174: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01e5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0206: Unknown result type (might be due to invalid IL or missing references)
			//IL_0211: Unknown result type (might be due to invalid IL or missing references)
			//IL_0226: Unknown result type (might be due to invalid IL or missing references)
			//IL_0231: Unknown result type (might be due to invalid IL or missing references)
			//IL_0246: Unknown result type (might be due to invalid IL or missing references)
			//IL_025f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0264: Unknown result type (might be due to invalid IL or missing references)
			//IL_0286: Unknown result type (might be due to invalid IL or missing references)
			//IL_0295: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_02cf: Unknown result type (might be due to invalid IL or missing references)
			_content = manager.contentGroupObject.GetComponent<RectTransform>();
			VerticalLayoutGroup component = manager.contentGroupObject.GetComponent<VerticalLayoutGroup>();
			if (!((Object)(object)_content == (Object)null) && !((Object)(object)component == (Object)null))
			{
				_originalParent = ((Transform)_content).parent;
				_sibling = ((Transform)_content).GetSiblingIndex();
				_anchorMin = _content.anchorMin;
				_anchorMax = _content.anchorMax;
				_pivot = _content.pivot;
				_size = _content.sizeDelta;
				_position = _content.anchoredPosition;
				_controlHeight = ((HorizontalOrVerticalLayoutGroup)component).childControlHeight;
				_expandHeight = ((HorizontalOrVerticalLayoutGroup)component).childForceExpandHeight;
				_controlWidth = ((HorizontalOrVerticalLayoutGroup)component).childControlWidth;
				_expandWidth = ((HorizontalOrVerticalLayoutGroup)component).childForceExpandWidth;
				((HorizontalOrVerticalLayoutGroup)component).childControlHeight = true;
				((HorizontalOrVerticalLayoutGroup)component).childForceExpandHeight = false;
				((HorizontalOrVerticalLayoutGroup)component).childControlWidth = true;
				((HorizontalOrVerticalLayoutGroup)component).childForceExpandWidth = true;
				RectTransform val = MakeRect("XIAOWO_NightlyReportScroll", _originalParent);
				_root = ((Component)val).gameObject;
				((Transform)val).SetSiblingIndex(_sibling);
				val.anchorMin = _anchorMin;
				val.anchorMax = _anchorMax;
				val.pivot = _pivot;
				val.sizeDelta = _size;
				val.anchoredPosition = _position;
				_viewport = MakeRect("Viewport", (Transform)(object)val);
				Fill(_viewport, Vector2.zero, new Vector2(-24f, 0f));
				ImageOn(_viewport, Color.clear);
				((Component)_viewport).gameObject.AddComponent<RectMask2D>();
				((Transform)_content).SetParent((Transform)(object)_viewport, false);
				_content.anchorMin = new Vector2(0f, 1f);
				_content.anchorMax = Vector2.one;
				_content.pivot = new Vector2(0.5f, 1f);
				_content.anchoredPosition = Vector2.zero;
				RectTransform val2 = MakeRect("Scrollbar", (Transform)(object)val);
				val2.anchorMin = new Vector2(1f, 0f);
				val2.anchorMax = Vector2.one;
				val2.pivot = new Vector2(1f, 0.5f);
				val2.anchoredPosition = Vector2.zero;
				val2.sizeDelta = new Vector2(16f, 0f);
				ImageOn(val2, Color32.op_Implicit(new Color32((byte)166, (byte)125, (byte)87, byte.MaxValue)));
				RectTransform val3 = MakeRect("Sliding Area", (Transform)(object)val2);
				Fill(val3, new Vector2(2f, 2f), new Vector2(-2f, -2f));
				RectTransform val4 = MakeRect("Handle", (Transform)(object)val3);
				Fill(val4, Vector2.zero, Vector2.zero);
				Image targetGraphic = ImageOn(val4, Color32.op_Implicit(new Color32((byte)98, (byte)93, (byte)75, byte.MaxValue)));
				_bar = ((Component)val2).gameObject.AddComponent<Scrollbar>();
				_bar.handleRect = val4;
				((Selectable)_bar).targetGraphic = (Graphic)(object)targetGraphic;
				_bar.direction = (Direction)2;
				_bar.numberOfSteps = 0;
				_scroll = ((Component)val).gameObject.AddComponent<ScrollRect>();
				_scroll.viewport = _viewport;
				_scroll.content = _content;
				_scroll.horizontal = false;
				_scroll.vertical = true;
				_scroll.movementType = (MovementType)2;
				_scroll.inertia = false;
				_scroll.scrollSensitivity = 48f;
				_scroll.verticalScrollbar = _bar;
				_scroll.verticalScrollbarVisibility = (ScrollbarVisibility)1;
				_signature = null;
				RequestTop();
			}
		}

		internal static void Dispose()
		{
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_yesterday != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)_yesterday).gameObject);
			}
			if ((Object)(object)_total != (Object)null)
			{
				Object.Destroy((Object)(object)((Component)_total).gameObject);
			}
			if ((Object)(object)_content != (Object)null && (Object)(object)_originalParent != (Object)null)
			{
				((Transform)_content).SetParent(_originalParent, false);
				((Transform)_content).SetSiblingIndex(_sibling);
				_content.anchorMin = _anchorMin;
				_content.anchorMax = _anchorMax;
				_content.pivot = _pivot;
				_content.sizeDelta = _size;
				_content.anchoredPosition = _position;
				VerticalLayoutGroup component = ((Component)_content).GetComponent<VerticalLayoutGroup>();
				if ((Object)(object)component != (Object)null)
				{
					((HorizontalOrVerticalLayoutGroup)component).childControlHeight = _controlHeight;
					((HorizontalOrVerticalLayoutGroup)component).childForceExpandHeight = _expandHeight;
					((HorizontalOrVerticalLayoutGroup)component).childControlWidth = _controlWidth;
					((HorizontalOrVerticalLayoutGroup)component).childForceExpandWidth = _expandWidth;
				}
			}
			if ((Object)(object)_root != (Object)null)
			{
				Object.Destroy((Object)(object)_root);
			}
			_root = null;
			_content = null;
			_viewport = null;
			_originalParent = null;
			_scroll = null;
			_bar = null;
			_yesterday = null;
			_total = null;
			_signature = null;
			_wasVisible = false;
		}
	}
	[HarmonyPatch(typeof(StartOfDayUIManager), "InitPanel")]
	internal static class FinanceReportPanelPatch
	{
		private static void Postfix()
		{
			FinanceReportUI.Rebuilt();
		}
	}
	[HarmonyPatch]
	internal static class FinanceReportOpenPatch
	{
		private static IEnumerable<MethodBase> TargetMethods()
		{
			yield return AccessTools.Method(typeof(AdvCalendarUIManager), "OpenUI", (Type[])null, (Type[])null);
			yield return AccessTools.Method(typeof(AdvCalendarUIManager), "OnNightlyReportButtonClicked", (Type[])null, (Type[])null);
		}

		private static void Postfix()
		{
			FinanceReportUI.RequestTop();
		}
	}
	internal static class FreeBuildMode
	{
		internal const string ToggleName = "XIAOWO_FreeBuildToggle";

		internal const int PointLimit = 500;

		internal const int PerkLimit = 500;

		private const float PickerToggleMinimumWidth = 220f;

		private const float PickerToggleGap = 12f;

		private static GameObject _toggleObject;

		private static Toggle _toggle;

		private static UnityAction<bool> _changedAction;

		private static bool _enabled;

		private static int _nativePointLimit;

		private static int _nativeBasePerkLimit;

		private static bool _nativeLimitsCaptured;

		internal static bool IsEnabled => _enabled;

		private static string LabelText => ModLocalization.Select("无上限特性点数", "Unlimited Perk Points");

		internal static void BeforeOpen(PerkUIController ui)
		{
			if (_enabled)
			{
				ApplyLimits(ui);
			}
		}

		internal static void AfterOpen(PerkUIController ui)
		{
			if (!((Object)(object)ui == (Object)null))
			{
				if (!_enabled)
				{
					CaptureNativeLimits(ui);
				}
				EnsureToggle(ui);
				ApplyLimits(ui);
				ui.OnChange();
				PerkRegistry.EnsurePicker(ui);
			}
		}

		internal static void ApplyLimits(PerkUIController ui)
		{
			if (_enabled && !((Object)(object)ui == (Object)null))
			{
				ui.maxPerkPoint = 500;
				ui.maxPerkCount = 500;
			}
		}

		internal static void RefreshLocalizedText()
		{
			TextMeshProUGUI val = (IsAlive((Object)(object)_toggleObject) ? _toggleObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			if (IsAlive((Object)(object)val))
			{
				((TMP_Text)val).text = LabelText;
			}
		}

		internal static string BuildStatus(PerkUIController ui)
		{
			TextMeshProUGUI val = (IsAlive((Object)(object)_toggleObject) ? _toggleObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			string[] array = new string[18]
			{
				"FREE_BUILD\npresent=",
				IsAlive((Object)(object)_toggleObject).ToString(),
				"\nenabled=",
				_enabled.ToString(),
				"\nlabel=",
				IsAlive((Object)(object)val) ? ((TMP_Text)val).text : "missing",
				"\npoint=",
				((ui != null) ? ui.currentPerkPoint : (-1)).ToString(),
				"/",
				((ui != null) ? ui.maxPerkPoint : (-1)).ToString(),
				"\nperks=",
				((ui != null) ? ui.currentPerkCount : (-1)).ToString(),
				"/",
				((ui != null) ? ui.maxPerkCount : (-1)).ToString(),
				"\npointText=",
				null,
				null,
				null
			};
			object obj;
			if (ui == null)
			{
				obj = null;
			}
			else
			{
				TextMeshProUGUI pointCount = ui.pointCount;
				obj = ((pointCount != null) ? ((TMP_Text)pointCount).text : null);
			}
			if (obj == null)
			{
				obj = "missing";
			}
			array[15] = (string)obj;
			array[16] = "\nperkText=";
			object obj2;
			if (ui == null)
			{
				obj2 = null;
			}
			else
			{
				TextMeshProUGUI slotCounter = ui.slotCounter;
				obj2 = ((slotCounter != null) ? ((TMP_Text)slotCounter).text : null);
			}
			if (obj2 == null)
			{
				obj2 = "missing";
			}
			array[17] = (string)obj2;
			return string.Concat(array);
		}

		internal static void SetEnabledForTest(PerkUIController ui, bool enabled)
		{
			SetEnabled(ui, enabled);
		}

		internal static void Dispose()
		{
			if (IsAlive((Object)(object)_toggleObject))
			{
				Object.Destroy((Object)(object)_toggleObject);
			}
			_toggleObject = null;
			_toggle = null;
			_changedAction = null;
			_enabled = false;
			_nativeLimitsCaptured = false;
		}

		private static void EnsureToggle(PerkUIController ui)
		{
			//IL_017a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0184: Expected Obj, but got Unknown
			object obj;
			if (ui == null)
			{
				obj = null;
			}
			else
			{
				Button startButton = ui.startButton;
				obj = ((startButton != null) ? ((Component)startButton).GetComponent<RectTransform>() : null);
			}
			RectTransform val = (RectTransform)obj;
			Transform val2 = ((val != null) ? ((Transform)val).parent : null);
			if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
			{
				return;
			}
			if (IsAlive((Object)(object)_toggleObject) && (Object)(object)_toggleObject.transform.parent == (Object)(object)val2)
			{
				ApplyPickerToggleLayout(_toggleObject.GetComponent<RectTransform>(), val, placeOnRight: false);
				_toggle.SetIsOnWithoutNotify(_enabled);
				RefreshLocalizedText();
				return;
			}
			if (IsAlive((Object)(object)_toggleObject))
			{
				Object.Destroy((Object)(object)_toggleObject);
			}
			CustomUIManager instance = CustomUIManager.Instance;
			GameObject val3 = ((instance != null) ? instance.togglePrefab : null);
			if (!IsAlive((Object)(object)val3))
			{
				return;
			}
			_toggleObject = Object.Instantiate<GameObject>(val3, val2, false);
			((Object)_toggleObject).name = "XIAOWO_FreeBuildToggle";
			_toggle = _toggleObject.GetComponent<Toggle>() ?? _toggleObject.GetComponentInChildren<Toggle>(true);
			RectTransform component = _toggleObject.GetComponent<RectTransform>();
			TextMeshProUGUI componentInChildren = _toggleObject.GetComponentInChildren<TextMeshProUGUI>(true);
			if (!IsAlive((Object)(object)_toggle) || !IsAlive((Object)(object)component) || !IsAlive((Object)(object)componentInChildren))
			{
				Object.Destroy((Object)(object)_toggleObject);
				_toggleObject = null;
				_toggle = null;
				return;
			}
			ApplyPickerToggleLayout(component, val, placeOnRight: false);
			((TMP_Text)componentInChildren).text = LabelText;
			((TMP_Text)componentInChildren).fontSize = Math.Min(((TMP_Text)componentInChildren).fontSize, 18f);
			((TMP_Text)componentInChildren).enableWordWrapping = false;
			((TMP_Text)componentInChildren).overflowMode = (TextOverflowModes)0;
			_toggle.onValueChanged = new ToggleEvent();
			if (_changedAction == null)
			{
				_changedAction = UnityAction<bool>.op_Implicit((Action<bool>)((bool enabled) =>
				{
					SetEnabled(PerkUIController.Instance, enabled);
				}));
			}
			((UnityEvent<bool>)(object)_toggle.onValueChanged).AddListener(_changedAction);
			_toggle.SetIsOnWithoutNotify(_enabled);
			_toggleObject.SetActive(true);
		}

		private static void SetEnabled(PerkUIController ui, bool enabled)
		{
			if (!((Object)(object)ui == (Object)null))
			{
				if (!_enabled & enabled)
				{
					CaptureNativeLimits(ui);
				}
				_enabled = enabled;
				if (_enabled)
				{
					ApplyLimits(ui);
				}
				else if (_nativeLimitsCaptured)
				{
					ui.maxPerkPoint = _nativePointLimit;
					ui.maxPerkCount = _nativeBasePerkLimit + SelectedExtraSlots(ui);
				}
				if (IsAlive((Object)(object)_toggle))
				{
					_toggle.SetIsOnWithoutNotify(_enabled);
				}
				ui.OnChange();
				PerkRegistry.EnsurePicker(ui);
			}
		}

		private static void CaptureNativeLimits(PerkUIController ui)
		{
			if (!((Object)(object)ui == (Object)null))
			{
				_nativePointLimit = ui.maxPerkPoint;
				_nativeBasePerkLimit = Math.Max(0, ui.maxPerkCount - SelectedExtraSlots(ui));
				_nativeLimitsCaptured = true;
			}
		}

		private static int SelectedExtraSlots(PerkUIController ui)
		{
			if ((Object)(object)((ui != null) ? ui.selectedPerks : null) == (Object)null)
			{
				return 0;
			}
			int num = 0;
			foreach (StartingPerkElement componentsInChild in ui.selectedPerks.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if (((componentsInChild != null) ? componentsInChild.perk : null) != null)
				{
					num += componentsInChild.perk.maxSlot;
				}
			}
			return num;
		}

		internal static void ApplyPickerToggleLayout(RectTransform rect, RectTransform startRect, bool placeOnRight)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_007d: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			if (IsAlive((Object)(object)rect) && IsAlive((Object)(object)startRect))
			{
				Rect rect2 = startRect.rect;
				float num = Math.Max(220f, rect2.width);
				rect2 = startRect.rect;
				float num2 = Math.Max(34f, rect2.height + 8f);
				float num3 = (num + 12f) * 0.5f;
				rect.anchorMin = startRect.anchorMin;
				rect.anchorMax = startRect.anchorMax;
				rect.pivot = startRect.pivot;
				rect.anchoredPosition = startRect.anchoredPosition + new Vector2(placeOnRight ? num3 : (0f - num3), num2);
				rect.sizeDelta = new Vector2(num, 30f);
				((Transform)rect).localScale = Vector3.one;
			}
		}

		private static bool IsAlive(Object value)
		{
			if (value != (Object)null)
			{
				return Object.op_Implicit(value);
			}
			return false;
		}
	}
	internal static class FridgeExpansion
	{
		internal const string LevelTag = "XIAOWO_FRIDGE_EXPANSION_LEVEL";

		internal const string NativeStorageTag = "FRIDGE_STORAGE_UPGRADE_COUNT_TAG";

		internal const string NativePowerTag = "FRIDGE_POWER_UPGRADE_COUNT_TAG";

		internal const int MaximumLevel = 4;

		[ThreadStatic]
		private static bool _applying;

		internal static bool IsFridge(GameItem item)
		{
			if (item != null && string.Equals(item.identifier?.Replace("\0", "").Trim(), "fridge", StringComparison.Ordinal))
			{
				return item.IsTag("FRIDGE_TAG");
			}
			return false;
		}

		internal static int Level(GameItem item)
		{
			if (!IsFridge(item))
			{
				return 0;
			}
			TagSystem state = item.state;
			int? num;
			if (state == null)
			{
				num = null;
			}
			else
			{
				TagState tag = state.GetTag("XIAOWO_FRIDGE_EXPANSION_LEVEL");
				num = ((tag != null) ? new int?(tag.GetInt()) : ((int?)null));
			}
			int? num2 = num;
			return Math.Clamp(num2.GetValueOrDefault(), 0, 4);
		}

		internal static bool NativeStorageComplete(GameItem item)
		{
			if (IsFridge(item))
			{
				TagState tagReadonly = item.GetTagReadonly("FRIDGE_STORAGE_UPGRADE_COUNT_TAG");
				return ((tagReadonly != null) ? tagReadonly.GetInt() : 0) >= 7;
			}
			return false;
		}

		internal static int Remaining(GameItem item)
		{
			if (!NativeStorageComplete(item))
			{
				return 4;
			}
			return 4 - Level(item);
		}

		internal static string Status(GameItem item)
		{
			if (!PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion"))
			{
				return string.Empty;
			}
			if (!PrecisionBayMode.IsUpgradeEnabled())
			{
				return ModLocalization.Select("精工扩舱已关闭", "Precision Bay Expansion disabled");
			}
			if (!NativeStorageComplete(item))
			{
				return ModLocalization.Select("完成冰箱原生存储升级后开放精工扩舱", "Complete native fridge storage upgrades first");
			}
			int num = Level(item);
			return ModLocalization.Select("精工扩舱 ", "Precision expansion ") + num + "/4 | " + ModLocalization.Select("冷藏 ", "Chill ") + (5 + num) + "x" + (9 + num) + " | " + ModLocalization.Select("冷冻 ", "Freeze ") + (3 + num) + "x" + (9 + num);
		}

		internal static bool CanUpgrade(GameItem item)
		{
			if (PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion") && PrecisionBayMode.IsUpgradeEnabled() && NativeStorageComplete(item) && Level(item) < 4)
			{
				GameGridInventory chillZone = MachineFridge.GetChillZone(item);
				if (((chillZone != null) ? chillZone.inventoryShape : null) != null)
				{
					GameGridInventory freezeZone = MachineFridge.GetFreezeZone(item);
					return ((freezeZone != null) ? freezeZone.inventoryShape : null) != null;
				}
			}
			return false;
		}

		internal static bool TryUpgrade(GameItem fridge, GameItem ingot, out string details)
		{
			details = string.Empty;
			if (!CanUpgrade(fridge))
			{
				details = "native fridge storage not ready or capped";
				return false;
			}
			GameGridInventory chillZone = MachineFridge.GetChillZone(fridge);
			GameGridInventory freezeZone = MachineFridge.GetFreezeZone(fridge);
			int num = Level(fridge);
			(int, int) tuple = (chillZone.inventoryShape.width, chillZone.inventoryShape.height);
			(int, int) tuple2 = (freezeZone.inventoryShape.width, freezeZone.inventoryShape.height);
			List<(IntPtr, int, int, int, int)> expected = Snapshot(chillZone);
			List<(IntPtr, int, int, int, int)> expected2 = Snapshot(freezeZone);
			TagState tagReadonly = fridge.GetTagReadonly("FRIDGE_STORAGE_UPGRADE_COUNT_TAG");
			int num2 = ((tagReadonly != null) ? tagReadonly.GetInt() : 0);
			TagState tagReadonly2 = fridge.GetTagReadonly("FRIDGE_POWER_UPGRADE_COUNT_TAG");
			int num3 = ((tagReadonly2 != null) ? tagReadonly2.GetInt() : 0);
			int num4 = num + 1;
			bool result;
			try
			{
				_applying = true;
				SetLevel(fridge, num4);
				Apply(fridge);
				if (!Matches(chillZone, expected) || !Matches(freezeZone, expected2) || chillZone.inventoryShape.width != 5 + num4 || chillZone.inventoryShape.height != 9 + num4 || freezeZone.inventoryShape.width != 3 + num4 || freezeZone.inventoryShape.height != 9 + num4)
				{
					goto IL_0158;
				}
				TagState tagReadonly3 = fridge.GetTagReadonly("FRIDGE_STORAGE_UPGRADE_COUNT_TAG");
				if (((tagReadonly3 != null) ? tagReadonly3.GetInt() : 0) != num2)
				{
					goto IL_0158;
				}
				TagState tagReadonly4 = fridge.GetTagReadonly("FRIDGE_POWER_UPGRADE_COUNT_TAG");
				if (((tagReadonly4 != null) ? tagReadonly4.GetInt() : 0) != num3)
				{
					goto IL_0158;
				}
				if (ingot != null)
				{
					ingot.Destroy();
				}
				details = "fridge expansion=" + num4 + "/4 chill=" + (5 + num4) + "x" + (9 + num4) + " freeze=" + (3 + num4) + "x" + (9 + num4);
				result = true;
				goto end_IL_00ae;
				IL_0158:
				string[] array = new string[16]
				{
					"fridge contents, dimensions or native levels changed: chill=",
					chillZone.inventoryShape.width.ToString(),
					"x",
					chillZone.inventoryShape.height.ToString(),
					" freeze=",
					freezeZone.inventoryShape.width.ToString(),
					"x",
					freezeZone.inventoryShape.height.ToString(),
					" items=",
					null,
					null,
					null,
					null,
					null,
					null,
					null
				};
				result = Matches(chillZone, expected);
				array[9] = result.ToString();
				array[10] = "/";
				result = Matches(freezeZone, expected2);
				array[11] = result.ToString();
				array[12] = " native=";
				TagState tagReadonly5 = fridge.GetTagReadonly("FRIDGE_STORAGE_UPGRADE_COUNT_TAG");
				array[13] = ((tagReadonly5 != null) ? tagReadonly5.GetInt() : 0).ToString();
				array[14] = "/";
				TagState tagReadonly6 = fridge.GetTagReadonly("FRIDGE_POWER_UPGRADE_COUNT_TAG");
				array[15] = ((tagReadonly6 != null) ? tagReadonly6.GetInt() : 0).ToString();
				throw new InvalidOperationException(string.Concat(array));
				end_IL_00ae:;
			}
			catch (Exception ex)
			{
				try
				{
					SetLevel(fridge, num);
					chillZone.SetShape(tuple.Item1, tuple.Item2);
					freezeZone.SetShape(tuple2.Item1, tuple2.Item2);
					PixelWindow contentWindow = fridge.contentWindow;
					if (contentWindow != null)
					{
						contentWindow.Validate();
					}
				}
				catch (Exception ex2)
				{
					Instance log = Core.Log;
					if (log != null)
					{
						log.Error("Fridge expansion rollback failed: " + ex2);
					}
				}
				details = ex.GetType().Name + ": " + ex.Message;
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Error("Fridge expansion rejected: " + details);
				}
				result = false;
			}
			finally
			{
				_applying = false;
			}
			return result;
		}

		internal static void Apply(GameItem fridge)
		{
			if (!NativeStorageComplete(fridge))
			{
				return;
			}
			int num = Level(fridge);
			if (num == 0)
			{
				return;
			}
			GameGridInventory chillZone = MachineFridge.GetChillZone(fridge);
			GameGridInventory freezeZone = MachineFridge.GetFreezeZone(fridge);
			if (((chillZone != null) ? chillZone.inventoryShape : null) == null || ((freezeZone != null) ? freezeZone.inventoryShape : null) == null)
			{
				return;
			}
			bool flag = false;
			if (chillZone.inventoryShape.width != 5 + num || chillZone.inventoryShape.height != 9 + num)
			{
				chillZone.SetShape(5 + num, 9 + num);
				flag = true;
			}
			if (freezeZone.inventoryShape.width != 3 + num || freezeZone.inventoryShape.height != 9 + num)
			{
				freezeZone.SetShape(3 + num, 9 + num);
				flag = true;
			}
			if (flag)
			{
				chillZone.ForceRecomputeDynamicBackground();
				freezeZone.ForceRecomputeDynamicBackground();
				PixelWindow contentWindow = fridge.contentWindow;
				if (contentWindow != null)
				{
					contentWindow.Validate();
				}
				if (chillZone.inventoryShape.width != 5 + num || chillZone.inventoryShape.height != 9 + num)
				{
					chillZone.SetShape(5 + num, 9 + num);
				}
				if (freezeZone.inventoryShape.width != 3 + num || freezeZone.inventoryShape.height != 9 + num)
				{
					freezeZone.SetShape(3 + num, 9 + num);
				}
				MachineFridge.UpdateFridgePowerUsage(fridge);
			}
		}

		private static void SetLevel(GameItem fridge, int level)
		{
			if (!fridge.IsTag("XIAOWO_FRIDGE_EXPANSION_LEVEL"))
			{
				fridge.state.EnableTag("XIAOWO_FRIDGE_EXPANSION_LEVEL");
			}
			fridge.state.GetTag("XIAOWO_FRIDGE_EXPANSION_LEVEL").SetInt(level);
		}

		private static List<(IntPtr Pointer, int Id, int Count, int X, int Y)> Snapshot(GameGridInventory zone)
		{
			List<(IntPtr, int, int, int, int)> list = new List<(IntPtr, int, int, int, int)>();
			for (int i = 0; i < zone.items.Count; i++)
			{
				GameItem val = zone.items[i];
				IntPtr pointer = ((Il2CppObjectBase)val).Pointer;
				int uniqueId = val.uniqueId;
				int unitCount = val.unitCount;
				GridShape modifiedShape = val.modifiedShape;
				int item = ((modifiedShape != null) ? modifiedShape.minX : (-1));
				GridShape modifiedShape2 = val.modifiedShape;
				list.Add((pointer, uniqueId, unitCount, item, (modifiedShape2 != null) ? modifiedShape2.minY : (-1)));
			}
			return list;
		}

		private static bool Matches(GameGridInventory zone, List<(IntPtr Pointer, int Id, int Count, int X, int Y)> expected)
		{
			if (zone.items.Count != expected.Count)
			{
				return false;
			}
			int num = 0;
			while (num < expected.Count)
			{
				GameItem val = zone.items[num];
				IntPtr? intPtr = ((val != null) ? new IntPtr?(((Il2CppObjectBase)val).Pointer) : ((IntPtr?)null));
				IntPtr item = expected[num].Pointer;
				if (intPtr.HasValue && !(intPtr.GetValueOrDefault() != item) && val.uniqueId == expected[num].Id && val.unitCount == expected[num].Count)
				{
					GridShape modifiedShape = val.modifiedShape;
					if (((modifiedShape != null) ? modifiedShape.minX : (-1)) == expected[num].X)
					{
						GridShape modifiedShape2 = val.modifiedShape;
						if (((modifiedShape2 != null) ? modifiedShape2.minY : (-1)) == expected[num].Y)
						{
							num++;
							continue;
						}
					}
				}
				return false;
			}
			return true;
		}

		internal static void Restore(GameItem fridge)
		{
			if (_applying || !IsFridge(fridge) || Level(fridge) == 0)
			{
				return;
			}
			try
			{
				_applying = true;
				Apply(fridge);
			}
			finally
			{
				_applying = false;
			}
		}

		internal static void CorrectPower(GameItem fridge)
		{
			if (NativeStorageComplete(fridge) && Level(fridge) != 0)
			{
				TagState tagReadonly = fridge.GetTagReadonly("FRIDGE_STORAGE_UPGRADE_COUNT_TAG");
				int num = 2 + 3 * ((tagReadonly != null) ? tagReadonly.GetInt() : 0);
				TagState tagReadonly2 = fridge.GetTagReadonly("FRIDGE_POWER_UPGRADE_COUNT_TAG");
				int num2 = Math.Max(2, num - 2 * ((tagReadonly2 != null) ? tagReadonly2.GetInt() : 0));
				TagSystem state = fridge.state;
				TagState obj = ((state != null) ? state.GetTag("machinery_drawn_power") : null);
				if (obj != null)
				{
					obj.SetInt(num2);
				}
			}
		}
	}
	[HarmonyPatch(typeof(MachineFridge), "ApplyStorageSize")]
	internal static class FridgeStorageRestorePatch
	{
		private static void Postfix(GameItem fridge)
		{
			FridgeExpansion.Restore(fridge);
		}
	}
	[HarmonyPatch(typeof(GameItem), "ValidateShapeState")]
	internal static class FridgeValidationRestorePatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(GameItem __instance)
		{
			FridgeExpansion.Restore(__instance);
		}
	}
	[HarmonyPatch(typeof(MachineFridge), "UpdateFridgePowerUsage")]
	internal static class FridgePowerPatch
	{
		private static void Postfix(GameItem fridge)
		{
			FridgeExpansion.CorrectPower(fridge);
		}
	}
	internal static class GameWindowResizer
	{
		private delegate bool EnumWindowsProc(IntPtr window, IntPtr parameter);

		private struct NativeRect
		{
			internal int Left;

			internal int Top;

			internal int Right;

			internal int Bottom;

			internal int Width => Right - Left;

			internal int Height => Bottom - Top;

			internal NativeRect(int left, int top, int right, int bottom)
			{
				Left = left;
				Top = top;
				Right = right;
				Bottom = bottom;
			}
		}

		private const int GwlStyle = -16;

		private const int GwlExStyle = -20;

		private const uint WsThickFrame = 262144u;

		private const uint WsMaximizeBox = 65536u;

		private const uint SwpNoSize = 1u;

		private const uint SwpNoMove = 2u;

		private const uint SwpNoZOrder = 4u;

		private const uint SwpNoActivate = 16u;

		private const uint SwpFrameChanged = 32u;

		private static IntPtr _window;

		private static int _tickCountdown;

		private static bool _styleApplied;

		private static int _testWidth;

		private static int _testHeight;

		internal static void Tick()
		{
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Invalid comparison between Unknown and I4
			if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows) || _tickCountdown-- > 0)
			{
				return;
			}
			_tickCountdown = 30;
			if ((int)Screen.fullScreenMode != 3)
			{
				_styleApplied = false;
				return;
			}
			IntPtr intPtr = FindUnityWindow();
			if (intPtr == IntPtr.Zero)
			{
				return;
			}
			if (_window != intPtr)
			{
				_window = intPtr;
				_styleApplied = false;
			}
			uint windowStyle = GetWindowStyle(intPtr);
			uint num = windowStyle | 0x40000 | 0x10000;
			if (num != windowStyle)
			{
				GetClientRect(intPtr, out var rect);
				SetWindowStyle(intPtr, num);
				NativeRect rect2 = new NativeRect(0, 0, rect.Width, rect.Height);
				if (AdjustWindowRectEx(ref rect2, num, hasMenu: false, GetWindowExStyle(intPtr)))
				{
					SetWindowPos(intPtr, IntPtr.Zero, 0, 0, rect2.Width, rect2.Height, 54u);
				}
				else
				{
					SetWindowPos(intPtr, IntPtr.Zero, 0, 0, 0, 0, 55u);
				}
			}
			_styleApplied = (GetWindowStyle(intPtr) & 0x50000) == 327680;
		}

		internal static string BuildStatus()
		{
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			IntPtr intPtr = FindUnityWindow();
			if (intPtr == IntPtr.Zero)
			{
				return "GAME_WINDOW_RESIZE\nwindow=False mode=" + ((object)Screen.fullScreenMode/*cast due to constrained. prefix*/).ToString();
			}
			GetClientRect(intPtr, out var rect);
			uint windowStyle = GetWindowStyle(intPtr);
			return "GAME_WINDOW_RESIZE\nwindow=True mode=" + ((object)Screen.fullScreenMode/*cast due to constrained. prefix*/).ToString() + "\nstyleApplied=" + _styleApplied + " thickFrame=" + ((windowStyle & 0x40000) != 0) + " maximize=" + ((windowStyle & 0x10000) != 0) + "\nclient=" + rect.Width + "x" + rect.Height + "\nunity=" + Screen.width + "x" + Screen.height;
		}

		internal static string BeginResizeTest()
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Invalid comparison between Unknown and I4
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			TickNow();
			if ((int)Screen.fullScreenMode != 3)
			{
				return "GAME_WINDOW_RESIZE_TEST SKIP: mode=" + ((object)Screen.fullScreenMode/*cast due to constrained. prefix*/).ToString();
			}
			IntPtr intPtr = FindUnityWindow();
			if (intPtr == IntPtr.Zero || !GetClientRect(intPtr, out var rect))
			{
				return "GAME_WINDOW_RESIZE_TEST FAIL: window unavailable";
			}
			_testWidth = rect.Width;
			_testHeight = rect.Height;
			int num = Math.Max(800, rect.Width - 96);
			int num2 = Math.Max(450, rect.Height - 54);
			if (num == rect.Width && num2 == rect.Height)
			{
				num = rect.Width + 96;
				num2 = rect.Height + 54;
			}
			if (!ResizeClient(intPtr, num, num2))
			{
				return "GAME_WINDOW_RESIZE_TEST FAIL: SetWindowPos failed";
			}
			return "GAME_WINDOW_RESIZE_TEST STARTED original=" + _testWidth + "x" + _testHeight + " target=" + num + "x" + num2;
		}

		internal static string RestoreResizeTest()
		{
			IntPtr intPtr = FindUnityWindow();
			if (intPtr == IntPtr.Zero || _testWidth <= 0 || _testHeight <= 0)
			{
				return "GAME_WINDOW_RESIZE_RESTORE SKIP";
			}
			bool flag = ResizeClient(intPtr, _testWidth, _testHeight);
			string result = "GAME_WINDOW_RESIZE_RESTORE " + (flag ? "PASS" : "FAIL") + " target=" + _testWidth + "x" + _testHeight;
			_testWidth = 0;
			_testHeight = 0;
			return result;
		}

		internal static void Dispose()
		{
			_window = IntPtr.Zero;
			_styleApplied = false;
			_testWidth = 0;
			_testHeight = 0;
		}

		private static void TickNow()
		{
			_tickCountdown = 0;
			Tick();
		}

		private static bool ResizeClient(IntPtr window, int width, int height)
		{
			NativeRect rect = new NativeRect(0, 0, width, height);
			if (!AdjustWindowRectEx(ref rect, GetWindowStyle(window), hasMenu: false, GetWindowExStyle(window)))
			{
				return false;
			}
			return SetWindowPos(window, IntPtr.Zero, 0, 0, rect.Width, rect.Height, 22u);
		}

		private static IntPtr FindUnityWindow()
		{
			if (_window != IntPtr.Zero && IsWindow(_window))
			{
				return _window;
			}
			uint processId = (uint)Process.GetCurrentProcess().Id;
			IntPtr fallback = IntPtr.Zero;
			IntPtr unity = IntPtr.Zero;
			EnumWindows((IntPtr window, IntPtr _) =>
			{
				GetWindowThreadProcessId(window, out var processId2);
				if (processId2 != processId || !IsWindowVisible(window))
				{
					return true;
				}
				fallback = window;
				StringBuilder stringBuilder = new StringBuilder(128);
				GetClassName(window, stringBuilder, stringBuilder.Capacity);
				if (!stringBuilder.ToString().Contains("UnityWndClass", StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
				unity = window;
				return false;
			}, IntPtr.Zero);
			_window = ((unity != IntPtr.Zero) ? unity : fallback);
			return _window;
		}

		private static uint GetWindowStyle(IntPtr window)
		{
			return (uint)((IntPtr.Size == 8) ? GetWindowLongPtr64(window, -16).ToInt64() : GetWindowLong32(window, -16));
		}

		private static uint GetWindowExStyle(IntPtr window)
		{
			return (uint)((IntPtr.Size == 8) ? GetWindowLongPtr64(window, -20).ToInt64() : GetWindowLong32(window, -20));
		}

		private static void SetWindowStyle(IntPtr window, uint style)
		{
			if (IntPtr.Size == 8)
			{
				SetWindowLongPtr64(window, -16, new IntPtr(style));
			}
			else
			{
				SetWindowLong32(window, -16, (int)style);
			}
		}

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc callback, IntPtr parameter);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);

		[DllImport("user32.dll", CharSet = CharSet.Unicode)]
		private static extern int GetClassName(IntPtr window, StringBuilder className, int maximumCount);

		[DllImport("user32.dll")]
		private static extern bool IsWindow(IntPtr window);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr window);

		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		private static extern int GetWindowLong32(IntPtr window, int index);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr64(IntPtr window, int index);

		[DllImport("user32.dll", EntryPoint = "SetWindowLong")]
		private static extern int SetWindowLong32(IntPtr window, int index, int value);

		[DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
		private static extern IntPtr SetWindowLongPtr64(IntPtr window, int index, IntPtr value);

		[DllImport("user32.dll")]
		private static extern bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

		[DllImport("user32.dll")]
		private static extern bool AdjustWindowRectEx(ref NativeRect rect, uint style, bool hasMenu, uint extendedStyle);

		[DllImport("user32.dll")]
		private static extern bool GetClientRect(IntPtr window, out NativeRect rect);
	}
	internal static class IconLoader
	{
		private static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

		internal static Sprite Get(PerkDefinition definition)
		{
			return GetEmbeddedSprite(definition.IconFile, definition.Id, (FilterMode)0);
		}

		internal static Sprite GetEmbeddedSprite(string resourceSuffix, string cacheKey, FilterMode filterMode)
		{
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Expected Obj, but got Unknown
			//IL_00e6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			if (Cache.TryGetValue(cacheKey, out var value))
			{
				return value;
			}
			Assembly assembly = typeof(Core).Assembly;
			string text = assembly.GetManifestResourceNames().FirstOrDefault((string name) => name.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase));
			if (text == null)
			{
				return null;
			}
			using Stream stream = assembly.GetManifestResourceStream(text);
			if (stream == null)
			{
				return null;
			}
			byte[] array = new byte[stream.Length];
			if (stream.Read(array, 0, array.Length) != array.Length)
			{
				return null;
			}
			Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false, false)
			{
				filterMode = filterMode,
				wrapMode = (TextureWrapMode)1,
				hideFlags = (HideFlags)61
			};
			if (!ImageConversion.LoadImage(val, new Il2CppStructArray<byte>(array), false))
			{
				Object.Destroy((Object)(object)val);
				return null;
			}
			Sprite val2 = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
			((Object)val2).hideFlags = (HideFlags)61;
			Cache[cacheKey] = val2;
			return val2;
		}

		internal static void InjectAll()
		{
			Dictionary<string, Sprite> perkIcons = StartingPerkIconLoader.perkIcons;
			if (perkIcons == null)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("Perk icon dictionary was not initialized.");
				}
				return;
			}
			int num = 0;
			PerkDefinition[] all = PerkRegistry.All;
			foreach (PerkDefinition perkDefinition in all)
			{
				Sprite val = Get(perkDefinition);
				if (!((Object)(object)val == (Object)null))
				{
					perkIcons[perkDefinition.Id] = val;
					num++;
				}
			}
			if (num == PerkRegistry.All.Length)
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Msg("Embedded perk icons injected: " + num + "/" + PerkRegistry.All.Length + ".");
				}
			}
			else
			{
				Instance log3 = Core.Log;
				if (log3 != null)
				{
					log3.Warning("Embedded perk icons injected: " + num + "/" + PerkRegistry.All.Length + ".");
				}
			}
		}
	}
	[HarmonyPatch(typeof(StartingPerkIconLoader), "Start")]
	internal static class IconInjectionPatch
	{
		private static void Postfix()
		{
			IconLoader.InjectAll();
		}
	}
	internal static class LoadingProgressOverlay
	{
		internal const string RootName = "XIAOWOTradePerksLoadingProgress";

		private const float BarWidth = 680f;

		private const float CompletionHoldSeconds = 1.5f;

		private static readonly Color FrameColor = new Color(144f / 255f, 201f / 255f, 197f / 255f, 1f);

		private static readonly Color TrackColor = new Color(39f / 255f, 31f / 255f, 49f / 255f, 0.96f);

		private static readonly List<string> Trace = new List<string>();

		private static GameObject _root;

		private static RectTransform _fillRect;

		private static TextMeshProUGUI _statusLabel;

		private static Sprite _solidSprite;

		private static bool _active;

		private static bool _completed;

		private static int _slotId = -1;

		private static int _percent;

		private static string _stage = string.Empty;

		private static DateTime _hideAtUtc;

		internal static bool IsActive => _active;

		internal static bool IsVisible
		{
			get
			{
				if (IsAlive((Object)(object)_root))
				{
					return _root.activeInHierarchy;
				}
				return false;
			}
		}

		internal static int ProgressPercent => _percent;

		internal static void Tick()
		{
			if (_active)
			{
				EnsureRoot();
				ApplyVisuals();
			}
			if (_completed && _active && !(DateTime.UtcNow < _hideAtUtc))
			{
				_active = false;
				if (IsAlive((Object)(object)_root))
				{
					_root.SetActive(false);
				}
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Save loading progress hidden after completion.");
				}
			}
		}

		internal static void Begin(int slotId)
		{
			Trace.Clear();
			_slotId = slotId;
			_active = true;
			_completed = false;
			_hideAtUtc = DateTime.MinValue;
			SetStage(10, Select("已选择存档", "Save selected"));
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Save loading progress started for slot " + slotId + ".");
			}
		}

		internal static void Advance(int percent, string chinese, string english)
		{
			if (_active && !_completed)
			{
				SetStage(percent, Select(chinese, english));
			}
		}

		internal static void Complete()
		{
			if (_active)
			{
				SetStage(100, Select("载入完成", "Load complete"));
				_completed = true;
				_hideAtUtc = DateTime.UtcNow.AddSeconds(1.5);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Save loading progress completed for slot " + _slotId + ".");
				}
			}
		}

		internal static string BuildStatus()
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Invalid comparison between Unknown and I4
			float num = (IsAlive((Object)(object)_fillRect) ? _fillRect.anchorMax.x : (-1f));
			Canvas val = (IsAlive((Object)(object)_root) ? _root.GetComponent<Canvas>() : null);
			return "LOADING_PROGRESS\nactive=" + _active + "\nvisible=" + IsVisible + "\ncompleted=" + _completed + "\nslot=" + _slotId + "\npercent=" + _percent + "\nstage=" + _stage + "\nfillRatio=" + num.ToString("0.###") + "\noverlayCanvas=" + (IsAlive((Object)(object)val) && (int)val.renderMode == 0) + "\nraycastBlocked=" + IsRaycastBlocked() + "\ntrace=" + string.Join("|", Trace);
		}

		internal static void Dispose()
		{
			if (IsAlive((Object)(object)_root))
			{
				Object.Destroy((Object)(object)_root);
			}
			_root = null;
			_fillRect = null;
			_statusLabel = null;
			if (IsAlive((Object)(object)_solidSprite))
			{
				Object.Destroy((Object)(object)_solidSprite);
			}
			_solidSprite = null;
			_active = false;
			_completed = false;
			_slotId = -1;
			_percent = 0;
			_stage = string.Empty;
			_hideAtUtc = DateTime.MinValue;
			Trace.Clear();
		}

		private static void SetStage(int percent, string stage)
		{
			_percent = Math.Max(_percent, Math.Clamp(percent, 0, 100));
			_stage = stage ?? string.Empty;
			Trace.Add(_percent + ":" + _stage);
			EnsureRoot();
			ApplyVisuals();
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Save loading progress: " + _percent + "% " + _stage);
			}
		}

		private static void EnsureRoot()
		{
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Expected Obj, but got Unknown
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_0153: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Unknown result type (might be due to invalid IL or missing references)
			//IL_01aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c3: Unknown result type (might be due to invalid IL or missing references)
			//IL_01dc: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0204: Unknown result type (might be due to invalid IL or missing references)
			//IL_0213: Unknown result type (might be due to invalid IL or missing references)
			//IL_0222: Unknown result type (might be due to invalid IL or missing references)
			if (IsAlive((Object)(object)_root))
			{
				if (!_root.activeSelf)
				{
					_root.SetActive(true);
				}
				return;
			}
			CustomUIManager instance = CustomUIManager.Instance;
			if (IsAlive((Object)(object)((instance != null) ? instance.labelPrefab : null)))
			{
				_root = new GameObject("XIAOWOTradePerksLoadingProgress", new Type[3]
				{
					Il2CppType.Of<RectTransform>(),
					Il2CppType.Of<Canvas>(),
					Il2CppType.Of<CanvasScaler>()
				});
				Object.DontDestroyOnLoad((Object)(object)_root);
				Canvas component = _root.GetComponent<Canvas>();
				component.renderMode = (RenderMode)0;
				component.overrideSorting = true;
				component.sortingOrder = 32767;
				CanvasScaler component2 = _root.GetComponent<CanvasScaler>();
				component2.uiScaleMode = (ScaleMode)1;
				component2.referenceResolution = new Vector2(1920f, 1080f);
				component2.matchWidthOrHeight = 0.5f;
				Image val = CreateImage(_root.transform, "LoadingProgressPanel", new Color(0f, 0f, 0f, 0.38f));
				SetBottomCenter(((Graphic)val).rectTransform, 0f, 26f, 712f, 72f);
				_statusLabel = CreateLabel(instance, ((Graphic)val).rectTransform);
				SetBottomCenter(((TMP_Text)_statusLabel).rectTransform, 0f, 34f, 680f, 26f);
				Image val2 = CreateImage(((Component)val).transform, "LoadingProgressFrame", FrameColor);
				SetBottomCenter(((Graphic)val2).rectTransform, 0f, 10f, 684f, 20f);
				Image val3 = CreateImage(((Component)val2).transform, "LoadingProgressTrack", TrackColor);
				Stretch(((Graphic)val3).rectTransform, 2f);
				_fillRect = ((Graphic)CreateImage(((Component)val3).transform, "LoadingProgressFill", FrameColor)).rectTransform;
				_fillRect.anchorMin = Vector2.zero;
				_fillRect.anchorMax = new Vector2(0f, 1f);
				_fillRect.pivot = new Vector2(0f, 0.5f);
				_fillRect.offsetMin = Vector2.zero;
				_fillRect.offsetMax = Vector2.zero;
				((Transform)_fillRect).localScale = Vector3.one;
				_root.SetActive(true);
			}
		}

		private static void ApplyVisuals()
		{
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			if (IsAlive((Object)(object)_root))
			{
				_root.SetActive(_active);
				if (IsAlive((Object)(object)_fillRect))
				{
					_fillRect.anchorMax = new Vector2((float)_percent / 100f, 1f);
					_fillRect.offsetMin = Vector2.zero;
					_fillRect.offsetMax = Vector2.zero;
				}
				if (IsAlive((Object)(object)_statusLabel))
				{
					((TMP_Text)_statusLabel).text = _stage + "  " + _percent + "%";
				}
				Canvas.ForceUpdateCanvases();
			}
		}

		private static Image CreateImage(Transform parent, string name, Color color)
		{
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[3]
			{
				Il2CppType.Of<RectTransform>(),
				Il2CppType.Of<CanvasRenderer>(),
				Il2CppType.Of<Image>()
			});
			val.transform.SetParent(parent, false);
			Image component = val.GetComponent<Image>();
			component.sprite = GetSolidSprite();
			component.type = (Type)0;
			((Graphic)component).color = color;
			((Graphic)component).raycastTarget = false;
			return component;
		}

		private static TextMeshProUGUI CreateLabel(CustomUIManager manager, RectTransform parent)
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = Object.Instantiate<GameObject>(manager.labelPrefab, (Transform)(object)parent, false);
			((Object)val).name = "LoadingProgressStatus";
			TextMeshProUGUI componentInChildren = val.GetComponentInChildren<TextMeshProUGUI>(true);
			if (!IsAlive((Object)(object)componentInChildren))
			{
				throw new InvalidOperationException("Loading progress label prefab is incomplete.");
			}
			((Graphic)componentInChildren).color = Color.white;
			((TMP_Text)componentInChildren).fontSize = 16f;
			((TMP_Text)componentInChildren).fontStyle = (FontStyles)1;
			((TMP_Text)componentInChildren).alignment = (TextAlignmentOptions)514;
			((TMP_Text)componentInChildren).enableWordWrapping = false;
			((Graphic)componentInChildren).raycastTarget = false;
			val.SetActive(true);
			return componentInChildren;
		}

		private static Sprite GetSolidSprite()
		{
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			if (IsAlive((Object)(object)_solidSprite))
			{
				return _solidSprite;
			}
			Texture2D whiteTexture = Texture2D.whiteTexture;
			_solidSprite = Sprite.Create(whiteTexture, new Rect(0f, 0f, (float)((Texture)whiteTexture).width, (float)((Texture)whiteTexture).height), new Vector2(0.5f, 0.5f), 100f);
			((Object)_solidSprite).name = "xiaowo_loading_solid";
			((Object)_solidSprite).hideFlags = (HideFlags)61;
			return _solidSprite;
		}

		private static void SetBottomCenter(RectTransform rect, float x, float y, float width, float height)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			rect.anchorMin = new Vector2(0.5f, 0f);
			rect.anchorMax = new Vector2(0.5f, 0f);
			rect.pivot = new Vector2(0.5f, 0f);
			rect.anchoredPosition = new Vector2(x, y);
			rect.sizeDelta = new Vector2(width, height);
			((Transform)rect).localScale = Vector3.one;
		}

		private static void Stretch(RectTransform rect, float inset)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			rect.anchorMin = Vector2.zero;
			rect.anchorMax = Vector2.one;
			rect.pivot = new Vector2(0.5f, 0.5f);
			rect.offsetMin = new Vector2(inset, inset);
			rect.offsetMax = new Vector2(0f - inset, 0f - inset);
			((Transform)rect).localScale = Vector3.one;
		}

		private static bool IsRaycastBlocked()
		{
			if (!IsAlive((Object)(object)_root))
			{
				return false;
			}
			foreach (Graphic componentsInChild in _root.GetComponentsInChildren<Graphic>(true))
			{
				if (IsAlive((Object)(object)componentsInChild) && componentsInChild.raycastTarget)
				{
					return true;
				}
			}
			return false;
		}

		private static string Select(string chinese, string english)
		{
			return ModLocalization.Select(chinese, english);
		}

		private static bool IsAlive(Object value)
		{
			if (value != (Object)null)
			{
				return Object.op_Implicit(value);
			}
			return false;
		}

		static LoadingProgressOverlay()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	[HarmonyPatch(typeof(SaveSlotElement), "LoadSlot")]
	internal static class LoadingProgressSaveSlotPatch
	{
		private static void Prefix(SaveSlotElement __instance)
		{
			LoadingProgressOverlay.Begin((__instance != null) ? __instance.slotId : (-1));
		}
	}
	[HarmonyPatch(typeof(MainMenuUIController), "StartNewGameClick")]
	internal static class LoadingProgressSceneRequestPatch
	{
		private static void Prefix()
		{
			if (LoadingProgressOverlay.IsActive)
			{
				LoadingProgressOverlay.Advance(25, "正在读取存档", "Reading save data");
			}
		}
	}
	[HarmonyPatch(typeof(EmporiumEntry), "Start")]
	internal static class LoadingProgressSceneReadyPatch
	{
		private static void Prefix()
		{
			LoadingProgressOverlay.Advance(55, "正在载入游戏场景", "Loading game scene");
		}

		private static void Postfix()
		{
			LoadingProgressOverlay.Complete();
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "PreLoadStore")]
	internal static class LoadingProgressStorePreloadPatch
	{
		private static void Prefix()
		{
			LoadingProgressOverlay.Advance(65, "正在准备店铺数据", "Preparing store data");
		}

		private static void Postfix()
		{
			LoadingProgressOverlay.Advance(75, "存档数据读取完成", "Save data read");
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class LoadingProgressStoreRestorePatch
	{
		private static void Prefix()
		{
			LoadingProgressOverlay.Advance(80, "正在恢复店铺、物品和事件", "Restoring store, items and events");
		}

		private static void Postfix()
		{
			LoadingProgressOverlay.Advance(90, "正在初始化 MOD 数据", "Initializing mod data");
		}
	}
	internal static class ModLocalization
	{
		internal static string CurrentLocaleCode
		{
			get
			{
				try
				{
					return LocHelper.GetCurrentLocaleCode() ?? string.Empty;
				}
				catch
				{
					return string.Empty;
				}
			}
		}

		internal static bool IsChinese(string localeCode)
		{
			if (string.IsNullOrWhiteSpace(localeCode))
			{
				return false;
			}
			if (!localeCode.Equals("zh", StringComparison.OrdinalIgnoreCase) && !localeCode.StartsWith("zh-", StringComparison.OrdinalIgnoreCase))
			{
				return localeCode.StartsWith("zh_", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		internal static string Select(string chinese, string english)
		{
			return Select(CurrentLocaleCode, chinese, english);
		}

		internal static string Select(string localeCode, string chinese, string english)
		{
			if (!IsChinese(localeCode))
			{
				return english;
			}
			return chinese;
		}
	}
	internal sealed class ManagedModEntry
	{
		internal string Name { get; set; } = string.Empty;

		internal string Version { get; set; } = string.Empty;

		internal string Author { get; set; } = string.Empty;

		internal string FilePath { get; set; } = string.Empty;

		internal bool LoadedNow { get; set; }

		internal bool EnabledNextLaunch { get; set; }

		internal bool IsSelf { get; set; }

		internal bool CanToggle { get; set; }

		internal bool IsRecognized { get; set; }

		internal string FileName => Path.GetFileName(FilePath);
	}
	internal static class ModManager
	{
		internal const string DisabledDirectoryName = "Disabled";

		internal static string ModsDirectory => MelonEnvironment.ModsDirectory;

		internal static string DisabledDirectory => Path.Combine(ModsDirectory, "Disabled");

		internal static IReadOnlyList<ManagedModEntry> Scan()
		{
			string fullPath = Path.GetFullPath(ModsDirectory);
			string fullPath2 = Path.GetFullPath(DisabledDirectory);
			string selfPath = NormalizePath(typeof(Core).Assembly.Location);
			Dictionary<string, List<MelonMod>> loadedModsByPath = GetLoadedModsByPath();
			List<ManagedModEntry> list = new List<ManagedModEntry>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			AddFiles(list, hashSet, loadedModsByPath, fullPath, enabledNextLaunch: true, selfPath, null);
			AddFiles(list, hashSet, loadedModsByPath, fullPath2, enabledNextLaunch: false, selfPath, fullPath);
			foreach (KeyValuePair<string, List<MelonMod>> item in loadedModsByPath)
			{
				if (!hashSet.Contains(item.Key))
				{
					list.Add(BuildEntry(item.Key, File.Exists(item.Key) && IsImmediateChild(item.Key, fullPath), canToggle: false, item.Value, selfPath));
				}
			}
			return list.OrderByDescending((ManagedModEntry entry) => entry.IsSelf).ThenBy((ManagedModEntry entry) => entry.Name, StringComparer.CurrentCultureIgnoreCase).ThenBy((ManagedModEntry entry) => entry.FileName, StringComparer.CurrentCultureIgnoreCase)
				.ToArray();
		}

		internal static bool TrySetEnabled(ManagedModEntry entry, bool enabled, out string message)
		{
			if (entry == null)
			{
				message = Select("未找到该 MOD。", "The mod entry is unavailable.");
				return false;
			}
			if (entry.IsSelf)
			{
				message = Select("主 MOD 不能停用。", "The core mod cannot be disabled.");
				return false;
			}
			if (!entry.CanToggle)
			{
				message = Select("当前仅支持管理 Mods 根目录中的 DLL。", "Only DLLs in the Mods root are currently manageable.");
				return false;
			}
			if (entry.EnabledNextLaunch == enabled)
			{
				message = Select("状态没有变化。", "The state is unchanged.");
				return true;
			}
			string directory = (enabled ? DisabledDirectory : ModsDirectory);
			string text = (enabled ? ModsDirectory : DisabledDirectory);
			string fullPath = Path.GetFullPath(entry.FilePath);
			if (!IsImmediateChild(fullPath, directory))
			{
				message = Select("拒绝移动不在受管目录中的文件。", "Refused to move a file outside the managed directories.");
				return false;
			}
			if (!File.Exists(fullPath))
			{
				message = Select("DLL 文件已经不存在。", "The DLL file no longer exists.");
				return false;
			}
			string text2 = Path.Combine(text, Path.GetFileName(fullPath));
			if (File.Exists(text2))
			{
				message = Select("目标位置已有同名 DLL，未执行覆盖。", "A DLL with the same name already exists at the destination.");
				return false;
			}
			try
			{
				Directory.CreateDirectory(text);
				File.Move(fullPath, text2);
				entry.FilePath = text2;
				entry.EnabledNextLaunch = enabled;
				message = (enabled ? Select("已设为下次启动时启用。", "Enabled for the next launch.") : Select("已设为下次启动时停用。", "Disabled for the next launch."));
				return true;
			}
			catch (Exception ex)
			{
				message = Select("操作失败：", "Operation failed: ") + ex.Message;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("Mod manager could not change '" + entry.FileName + "': " + ex);
				}
				return false;
			}
		}

		internal static string GetStatus(ManagedModEntry entry)
		{
			if (entry.IsSelf)
			{
				return Select("主 MOD / 已锁定", "Core mod / locked");
			}
			if (entry.LoadedNow && entry.EnabledNextLaunch)
			{
				return Select("本次已加载", "Loaded now");
			}
			if (entry.LoadedNow)
			{
				return Select("本次仍已加载 / 下次停用", "Loaded now / disabled next launch");
			}
			if (entry.EnabledNextLaunch)
			{
				if (!entry.IsRecognized)
				{
					return Select("未注册或加载失败", "Unregistered or failed to load");
				}
				return Select("下次启用", "Enabled next launch");
			}
			return Select("已停用", "Disabled");
		}

		internal static bool IsImmediateChild(string filePath, string directory)
		{
			if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(directory))
			{
				return false;
			}
			string directoryName = Path.GetDirectoryName(Path.GetFullPath(filePath));
			if (string.IsNullOrWhiteSpace(directoryName))
			{
				return false;
			}
			return string.Equals(Path.TrimEndingDirectorySeparator(Path.GetFullPath(directoryName)), Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)), StringComparison.OrdinalIgnoreCase);
		}

		private static void AddFiles(ICollection<ManagedModEntry> entries, ISet<string> seen, IReadOnlyDictionary<string, List<MelonMod>> loadedByPath, string directory, bool enabledNextLaunch, string selfPath, string loadedAliasDirectory)
		{
			if (!Directory.Exists(directory))
			{
				return;
			}
			string[] files = Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly);
			for (int i = 0; i < files.Length; i++)
			{
				string text = NormalizePath(files[i]);
				loadedByPath.TryGetValue(text, out var value);
				if ((value == null || value.Count == 0) && !string.IsNullOrWhiteSpace(loadedAliasDirectory))
				{
					string text2 = NormalizePath(Path.Combine(loadedAliasDirectory, Path.GetFileName(text)));
					if (loadedByPath.TryGetValue(text2, out var value2) && value2.Count > 0)
					{
						value = value2;
						seen.Add(text2);
					}
				}
				entries.Add(BuildEntry(text, enabledNextLaunch, canToggle: true, value, selfPath));
				seen.Add(text);
			}
		}

		private static ManagedModEntry BuildEntry(string path, bool enabledNextLaunch, bool canToggle, List<MelonMod> loaded, string selfPath)
		{
			ManagedModEntry managedModEntry = new ManagedModEntry
			{
				FilePath = path,
				EnabledNextLaunch = enabledNextLaunch,
				CanToggle = canToggle,
				LoadedNow = (loaded != null && loaded.Count > 0),
				IsRecognized = (loaded != null && loaded.Count > 0),
				IsSelf = string.Equals(path, selfPath, StringComparison.OrdinalIgnoreCase)
			};
			if (loaded != null && loaded.Count > 0)
			{
				managedModEntry.Name = string.Join(" + ", from name in loaded.Select((MelonMod mod) =>
					{
						MelonInfoAttribute info = ((MelonBase)mod).Info;
						return (info == null) ? null : info.Name;
					})
					where !string.IsNullOrWhiteSpace(name)
					select name);
				managedModEntry.Version = string.Join(" / ", (from version in loaded.Select((MelonMod mod) =>
					{
						MelonInfoAttribute info = ((MelonBase)mod).Info;
						return (info == null) ? null : info.Version;
					})
					where !string.IsNullOrWhiteSpace(version)
					select version).Distinct());
				managedModEntry.Author = string.Join(" / ", (from author in loaded.Select((MelonMod mod) =>
					{
						MelonInfoAttribute info = ((MelonBase)mod).Info;
						return (info == null) ? null : info.Author;
					})
					where !string.IsNullOrWhiteSpace(author)
					select author).Distinct());
			}
			if (string.IsNullOrWhiteSpace(managedModEntry.Name))
			{
				managedModEntry.Name = Path.GetFileNameWithoutExtension(path);
				TryReadAssemblyIdentity(path, managedModEntry);
			}
			if (managedModEntry.IsSelf)
			{
				managedModEntry.CanToggle = false;
			}
			return managedModEntry;
		}

		private static Dictionary<string, List<MelonMod>> GetLoadedModsByPath()
		{
			Dictionary<string, List<MelonMod>> dictionary = new Dictionary<string, List<MelonMod>>(StringComparer.OrdinalIgnoreCase);
			foreach (MelonBase registeredMelon in MelonBase.RegisteredMelons)
			{
				MelonMod val = (MelonMod)(object)((registeredMelon is MelonMod) ? registeredMelon : null);
				if (val == null)
				{
					continue;
				}
				MelonAssembly melonAssembly = ((MelonBase)val).MelonAssembly;
				if (!string.IsNullOrWhiteSpace((melonAssembly != null) ? melonAssembly.Location : null))
				{
					string key = NormalizePath(((MelonBase)val).MelonAssembly.Location);
					if (!dictionary.TryGetValue(key, out var value))
					{
						value = (dictionary[key] = new List<MelonMod>());
					}
					value.Add(val);
				}
			}
			return dictionary;
		}

		private static void TryReadAssemblyIdentity(string path, ManagedModEntry entry)
		{
			try
			{
				if (File.Exists(path))
				{
					AssemblyName assemblyName = AssemblyName.GetAssemblyName(path);
					if (!string.IsNullOrWhiteSpace(assemblyName.Name))
					{
						entry.Name = assemblyName.Name;
					}
					if (assemblyName.Version != null)
					{
						entry.Version = assemblyName.Version.ToString();
					}
					entry.IsRecognized = true;
				}
			}
			catch
			{
				entry.IsRecognized = false;
			}
		}

		private static string NormalizePath(string path)
		{
			if (string.IsNullOrWhiteSpace(path))
			{
				return string.Empty;
			}
			if (!Path.IsPathRooted(path))
			{
				path = Path.Combine(AppContext.BaseDirectory, path);
			}
			return Path.GetFullPath(path);
		}

		private static string Select(string chinese, string english)
		{
			return ModLocalization.Select(chinese, english);
		}
	}
	internal static class ModManagerHotkey
	{
		private static MelonPreferences_Entry<string> _binding;

		private static string _cachedBinding;

		private static bool _bindingParsed;

		private static KeyCode _key;

		private static bool _validBinding;

		private static bool _requiresCtrl;

		private static bool _requiresShift;

		private static bool _requiresAlt;

		internal static void Initialize()
		{
			_binding = MelonPreferences.CreateCategory("XIAOWOTradePerks_ModManager", "MOD Manager").CreateEntry<string>("ToggleHotkey", "BackQuote", "Toggle window", "Physical key above Tab; combinations such as Ctrl+Shift+M are supported.", false, false, (ValueValidator)null, (string)null);
		}

		internal static void Tick()
		{
			//IL_00a2: Unknown result type (might be due to invalid IL or missing references)
			if (_binding == null || ModManagerUI.IsCapturingHotkey)
			{
				return;
			}
			string value = _binding.Value;
			if (!_bindingParsed || !string.Equals(value, _cachedBinding, StringComparison.Ordinal))
			{
				_bindingParsed = true;
				_cachedBinding = value;
				string text = ModPreferences.NormalizeHotkey(value);
				string[] array = text.Split('+', StringSplitOptions.RemoveEmptyEntries);
				_validBinding = array.Length != 0 && Enum.TryParse<KeyCode>(array[^1], true, out _key);
				_requiresCtrl = text.StartsWith("Ctrl+", StringComparison.Ordinal);
				_requiresShift = text.Contains("Shift+", StringComparison.Ordinal);
				_requiresAlt = text.Contains("Alt+", StringComparison.Ordinal);
			}
			if (_validBinding && Input.GetKeyDown(_key))
			{
				bool num = Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305);
				bool flag = Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
				bool flag2 = Input.GetKey((KeyCode)308) || Input.GetKey((KeyCode)307);
				if (num == _requiresCtrl && flag == _requiresShift && flag2 == _requiresAlt && !IsEditingText())
				{
					ModManagerUI.Toggle();
				}
			}
		}

		private static bool IsEditingText()
		{
			EventSystem current = EventSystem.current;
			GameObject val = ((current != null) ? current.currentSelectedGameObject : null);
			if ((Object)(object)val != (Object)null)
			{
				TMP_InputField component = val.GetComponent<TMP_InputField>();
				if (component == null || !component.isFocused)
				{
					InputField component2 = val.GetComponent<InputField>();
					if (component2 == null)
					{
						return false;
					}
					return component2.isFocused;
				}
				return true;
			}
			return false;
		}
	}
	internal static class ModManagerUI
	{
		internal const string WindowId = "xiaowo_mod_manager";

		internal const string ButtonTextChinese = "MOD 管理";

		internal const string ButtonTextEnglish = "MOD MANAGER";

		internal const string ApplyTextChinese = "应用";

		internal const string ApplyTextEnglish = "APPLY";

		private static GameObject _menuButtonObject;

		private static Button _menuButton;

		private static UnityAction _openAction;

		private static readonly List<Action> ButtonActions = new List<Action>();

		private static readonly List<Action<bool>> ToggleActions = new List<Action<bool>>();

		private static readonly List<Action<string>> InputActions = new List<Action<string>>();

		private static readonly Dictionary<string, bool> PendingStates = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

		private const string ConfirmWindowId = "xiaowo_mod_manager_confirm";

		private static string _tab = "mods";

		private static string _search = string.Empty;

		private static string _selectedCategory = string.Empty;

		private static float _listScrollPosition = 1f;

		private static MelonPreferences_Entry _capturingEntry;

		private static int _captureDelay;

		private static int _checkCountdown;

		private static string _notice = string.Empty;

		private static string _lastFailure = string.Empty;

		internal static bool IsCapturingHotkey => _capturingEntry != null;

		internal static int PendingChangeCount => PendingStates.Count;

		internal static void Tick()
		{
			if (_checkCountdown-- > 0)
			{
				return;
			}
			_checkCountdown = 15;
			try
			{
				MainMenuUIController instance = MainMenuUIController.Instance;
				if (!IsMainMenuVisible(instance))
				{
					ForgetButton();
					return;
				}
				ConfigureMenuButton(instance);
				_lastFailure = string.Empty;
			}
			catch (Exception ex)
			{
				string text = ex.GetType().Name + ": " + ex.Message;
				if (!string.Equals(text, _lastFailure, StringComparison.Ordinal))
				{
					_lastFailure = text;
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("Mod manager button could not be created: " + text);
					}
				}
			}
		}

		internal static void CaptureTick()
		{
			//IL_003d: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Invalid comparison between Unknown and I4
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Invalid comparison between Unknown and I4
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			if (_capturingEntry == null || _captureDelay-- > 0)
			{
				return;
			}
			if (Input.GetKeyDown((KeyCode)27))
			{
				_capturingEntry = null;
				BuildWindow();
				return;
			}
			KeyCode[] values = Enum.GetValues<KeyCode>();
			for (int i = 0; i < values.Length; i++)
			{
				KeyCode val = values[i];
				if ((int)val > 0 && (int)val < 323 && !IsModifier(val) && Input.GetKeyDown(val))
				{
					bool flag = Input.GetKey((KeyCode)306) || Input.GetKey((KeyCode)305);
					bool flag2 = Input.GetKey((KeyCode)304) || Input.GetKey((KeyCode)303);
					bool flag3 = Input.GetKey((KeyCode)308) || Input.GetKey((KeyCode)307);
					string value = ((_capturingEntry.GetReflectedType() == typeof(KeyCode)) ? ((object)val/*cast due to constrained. prefix*/).ToString() : ((flag ? "Ctrl+" : "") + (flag2 ? "Shift+" : "") + (flag3 ? "Alt+" : "") + ((object)val/*cast due to constrained. prefix*/).ToString()));
					StagePreference(_capturingEntry, value);
					_capturingEntry = null;
					BuildWindow();
					break;
				}
			}
		}

		private static bool IsModifier(KeyCode key)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Invalid comparison between Unknown and I4
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Invalid comparison between Unknown and I4
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Invalid comparison between Unknown and I4
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Invalid comparison between Unknown and I4
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Invalid comparison between Unknown and I4
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Invalid comparison between Unknown and I4
			if ((int)key != 306 && (int)key != 305 && (int)key != 304 && (int)key != 303 && (int)key != 308)
			{
				return (int)key == 307;
			}
			return true;
		}

		internal static void Open()
		{
			try
			{
				if (IsMainMenuVisible(MainMenuUIController.Instance))
				{
					MainMenuUIController.Instance.OnAnyButtonClick();
					OpenWindow();
				}
			}
			catch (Exception ex)
			{
				_notice = Select("无法打开 MOD 管理：", "Could not open the mod manager: ") + ex.Message;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("Mod manager window could not be opened: " + ex);
				}
			}
		}

		internal static void OpenInGame()
		{
			if (PlayerStore.instance != null)
			{
				OpenWindow();
			}
		}

		internal static void Toggle()
		{
			CustomUIManager instance = CustomUIManager.Instance;
			if (IsAlive((Object)(object)instance))
			{
				CustomUIWindow window = instance.GetWindow("xiaowo_mod_manager");
				if (window != null && window.IsVisible)
				{
					RequestClose();
					return;
				}
			}
			if (IsMainMenuVisible(MainMenuUIController.Instance))
			{
				Open();
			}
			else
			{
				OpenInGame();
			}
		}

		private static void OpenWindow()
		{
			try
			{
				_notice = string.Empty;
				BuildWindow();
			}
			catch (Exception ex)
			{
				_notice = Select("无法打开 MOD 管理：", "Could not open the mod manager: ") + ex.Message;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("Mod manager window could not be opened: " + ex);
				}
			}
		}

		internal static void Dispose()
		{
			CloseWindow();
			ModManagerWindowResizer.Dispose();
			if (IsAlive((Object)(object)_menuButtonObject))
			{
				_menuButtonObject.SetActive(false);
			}
			ForgetButton();
			ButtonActions.Clear();
			ToggleActions.Clear();
			InputActions.Clear();
			_capturingEntry = null;
			PendingStates.Clear();
			ModPreferences.Revert();
			_notice = string.Empty;
			_lastFailure = string.Empty;
			_checkCountdown = 0;
		}

		internal static string BuildStatus()
		{
			//IL_0237: Unknown result type (might be due to invalid IL or missing references)
			//IL_023c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0269: Unknown result type (might be due to invalid IL or missing references)
			//IL_026e: Unknown result type (might be due to invalid IL or missing references)
			IReadOnlyList<ManagedModEntry> readOnlyList = ModManager.Scan();
			CustomUIManager instance = CustomUIManager.Instance;
			CustomUIWindow val = (IsAlive((Object)(object)instance) ? instance.GetWindow("xiaowo_mod_manager") : null);
			CustomUIElement val2 = (IsAlive((Object)(object)instance) ? instance.GetElement("xiaowo_mod_manager", "summary") : null);
			CustomUIElement val3 = (IsAlive((Object)(object)instance) ? instance.GetElement("xiaowo_mod_manager", "apply") : null);
			CustomUIElement val4 = (IsAlive((Object)(object)instance) ? instance.GetElement("xiaowo_mod_manager", "mod_0") : null);
			TextMeshProUGUI val5 = (IsAlive((Object)(object)_menuButtonObject) ? _menuButtonObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			RectTransform val6 = (IsAlive((Object)(object)_menuButtonObject) ? _menuButtonObject.GetComponent<RectTransform>() : null);
			GameObject val7 = (IsAlive((Object)(object)_menuButtonObject) ? FindExitButton(_menuButtonObject.transform.parent) : null);
			bool flag = IsAlive((Object)(object)val7) && (Object)(object)_menuButtonObject.transform.parent == (Object)(object)val7.transform.parent && _menuButtonObject.transform.GetSiblingIndex() + 1 == val7.transform.GetSiblingIndex();
			int num = (IsAlive((Object)(object)((val != null) ? val.root : null)) ? val.root.GetComponentsInChildren<Toggle>(true).Length : 0);
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			foreach (ManagedModEntry item in readOnlyList)
			{
				if (item.IsSelf)
				{
					num2++;
				}
				if (item.LoadedNow)
				{
					num3++;
				}
				if (!item.EnabledNextLaunch)
				{
					num4++;
				}
			}
			return "MOD_MANAGER\nmainMenu=" + IsAlive((Object)(object)MainMenuUIController.Instance) + "\nbuttonVisible=" + (IsAlive((Object)(object)_menuButtonObject) && _menuButtonObject.activeInHierarchy) + "\nbuttonText=" + (((val5 != null) ? ((TMP_Text)val5).text : null) ?? "missing") + "\nbuttonSize=" + (IsAlive((Object)(object)val6) ? ((object)val6.sizeDelta/*cast due to constrained. prefix*/).ToString() : "missing") + "\nbuttonPosition=" + (IsAlive((Object)(object)val6) ? ((object)val6.anchoredPosition/*cast due to constrained. prefix*/).ToString() : "missing") + "\nbuttonSibling=" + (IsAlive((Object)(object)_menuButtonObject) ? _menuButtonObject.transform.GetSiblingIndex() : (-1)) + "\nexitSibling=" + (IsAlive((Object)(object)val7) ? val7.transform.GetSiblingIndex() : (-1)) + "\nimmediatelyBeforeExit=" + flag + "\nwindowOpen=" + (val != null && val.IsVisible) + "\n" + ModManagerWindowResizer.Status() + "\nsummaryPresent=" + ((Object)(object)((val2 != null) ? val2.text : null) != (Object)null) + "\napplyPresent=" + (val3 != null) + "\npendingChanges=" + PendingStates.Count + "\ntoggleCount=" + num + "\nselfToggleLocked=" + ((Object)(object)((val4 != null) ? val4.toggle : null) != (Object)null && !((Selectable)val4.toggle).interactable) + "\nentries=" + readOnlyList.Count + "\nloadedNow=" + num3 + "\ndisabledNextLaunch=" + num4 + "\nselfLocked=" + (num2 == 1);
		}

		internal static bool ValidateDefinition(out string details)
		{
			details = "window=xiaowo_mod_manager, disabled=Disabled, button=" + Select("MOD 管理", "MOD MANAGER") + ", apply=" + Select("应用", "APPLY");
			return true;
		}

		private static void ConfigureMenuButton(MainMenuUIController menu)
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Expected Obj, but got Unknown
			GameObject moddingButton = menu.moddingButton;
			if (!IsAlive((Object)(object)moddingButton))
			{
				throw new InvalidOperationException("The original modding button is unavailable.");
			}
			Button val = moddingButton.GetComponent<Button>() ?? moddingButton.GetComponentInChildren<Button>(true);
			TextMeshProUGUI componentInChildren = moddingButton.GetComponentInChildren<TextMeshProUGUI>(true);
			if (!IsAlive((Object)(object)val) || !IsAlive((Object)(object)componentInChildren))
			{
				throw new InvalidOperationException("The original modding button is incomplete.");
			}
			if ((Object)(object)_menuButtonObject != (Object)(object)moddingButton || (Object)(object)_menuButton != (Object)(object)val)
			{
				_menuButtonObject = moddingButton;
				_menuButton = val;
				_menuButton.onClick = new ButtonClickedEvent();
				if (_openAction == null)
				{
					_openAction = UnityAction.op_Implicit((Action)Open);
				}
				((UnityEvent)_menuButton.onClick).AddListener(_openAction);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Original main-menu modding button enabled for MelonLoader mod management.");
				}
			}
			((TMP_Text)componentInChildren).text = Select("MOD 管理", "MOD MANAGER");
			moddingButton.SetActive(true);
			((Selectable)val).interactable = true;
			Transform parent = moddingButton.transform.parent;
			GameObject val2 = FindExitButton(parent);
			if (!IsAlive((Object)(object)val2))
			{
				throw new InvalidOperationException("The original exit button is unavailable.");
			}
			Transform val3 = parent.Find("XIAOWOTradePerksSkinButton");
			if (IsAlive((Object)(object)val3))
			{
				PlaceImmediatelyBefore(val3, val2.transform);
				PlaceImmediatelyBefore(moddingButton.transform, val3);
			}
			else
			{
				PlaceImmediatelyBefore(moddingButton.transform, val2.transform);
			}
		}

		private static GameObject FindExitButton(Transform parent)
		{
			if (!IsAlive((Object)(object)parent))
			{
				return null;
			}
			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				Button val = ((child != null) ? ((Component)child).gameObject.GetComponent<Button>() : null);
				if (!IsAlive((Object)(object)val))
				{
					continue;
				}
				for (int j = 0; j < ((UnityEventBase)val.onClick).GetPersistentEventCount(); j++)
				{
					if (string.Equals(((UnityEventBase)val.onClick).GetPersistentMethodName(j), "OnExitClick", StringComparison.Ordinal))
					{
						return ((Component)child).gameObject;
					}
				}
			}
			return null;
		}

		private static void PlaceImmediatelyBefore(Transform source, Transform target)
		{
			if (IsAlive((Object)(object)source) && IsAlive((Object)(object)target) && !((Object)(object)source.parent != (Object)(object)target.parent))
			{
				int siblingIndex = source.GetSiblingIndex();
				int siblingIndex2 = target.GetSiblingIndex();
				if (siblingIndex + 1 != siblingIndex2)
				{
					source.SetSiblingIndex((siblingIndex > siblingIndex2) ? siblingIndex2 : (siblingIndex2 - 1));
				}
			}
		}

		private static void BuildWindow()
		{
			//IL_0696: Unknown result type (might be due to invalid IL or missing references)
			//IL_06a0: Expected Obj, but got Unknown
			CustomUIManager instance = CustomUIManager.Instance;
			if (!IsAlive((Object)(object)instance))
			{
				throw new InvalidOperationException("Custom UI manager is unavailable.");
			}
			ModManagerWindowResizer.BeforeRebuild();
			instance.CloseWindow("xiaowo_mod_manager");
			ButtonActions.Clear();
			ToggleActions.Clear();
			InputActions.Clear();
			IReadOnlyList<ManagedModEntry> readOnlyList = ModManager.Scan();
			string locale = ModLocalization.CurrentLocaleCode;
			CustomUIBuilder val = CustomUIManager.Window("xiaowo_mod_manager", Select("MOD 管理", "MOD MANAGER"), "overlay").SetSize(ModManagerWindowResizer.Width, ModManagerWindowResizer.Height).Center()
				.SetCloseOnEscape(false)
				.SetDraggable(true);
			val.AddLabel(Select("更改先暂存，按“应用”后保存；DLL 启停和部分配置需要重启。", "Changes are staged until APPLY. DLL toggles and some preferences need a restart."), "summary");
			ConfigureLabel(val.Last, 15f, (FontStyles)0, 860f, 28f);
			val.AddLabel(_notice, "notice");
			ConfigureLabel(val.Last, 14f, (FontStyles)1, 860f, 28f);
			val.BeginRow(8f);
			AddTab(val, "mods", Select("MOD 列表", "MODS"));
			AddTab(val, "config", Select("配置", "PREFERENCES"));
			AddTab(val, "hotkeys", Select("热键", "HOTKEYS"));
			val.End();
			val.BeginRow(8f);
			val.BeginColumn(5f);
			Action<string> val2 = ToInputAction((string text2) =>
			{
				_search = text2 ?? string.Empty;
			});
			InputActions.Add(val2);
			val.AddInput(Select("搜索", "Search"), _search, val2, "search");
			val.Last.SetPreferredSize(330f, 38f);
			Action val3 = ToAction(BuildWindow);
			ButtonActions.Add(val3);
			val.AddButton(Select("搜索 / 刷新", "SEARCH / REFRESH"), val3, "refresh");
			val.Last.SetPreferredSize(170f, 32f);
			val.BeginScroll(365f);
			if (_tab == "mods")
			{
				for (int num = 0; num < readOnlyList.Count; num++)
				{
					if (MatchesSearch(readOnlyList[num].Name + " " + readOnlyList[num].FileName))
					{
						AddEntry(val, readOnlyList[num], num);
					}
				}
			}
			else
			{
				foreach (MelonPreferences_Category category in ModPreferences.Categories())
				{
					IReadOnlyList<MelonPreferences_Entry> source = ModPreferences.Entries(category);
					if (!source.Any((MelonPreferences_Entry entry) => _tab == "hotkeys" == ModPreferences.IsHotkey(entry)))
					{
						continue;
					}
					string text = ModPreferenceTranslations.Category(locale, category.Identifier, category.DisplayName);
					if (!MatchesSearch(text + " " + category.DisplayName + " " + category.Identifier) && !source.Any((MelonPreferences_Entry entry) => _tab == "hotkeys" == ModPreferences.IsHotkey(entry) && MatchesSearch(ModPreferenceTranslations.Entry(locale, category.Identifier, entry.Identifier, entry.DisplayName) + " " + entry.DisplayName + " " + entry.Identifier)))
					{
						continue;
					}
					string id = category.Identifier;
					Action val4 = ToAction(() =>
					{
						CustomUIManager instance2 = CustomUIManager.Instance;
						CustomUIWindow obj2 = ((instance2 != null) ? instance2.GetWindow("xiaowo_mod_manager") : null);
						object obj3;
						if (obj2 == null)
						{
							obj3 = null;
						}
						else
						{
							GameObject root2 = obj2.root;
							obj3 = ((root2 != null) ? ((IEnumerable<ScrollRect>)root2.GetComponentsInChildren<ScrollRect>(true)).FirstOrDefault() : null);
						}
						ScrollRect val10 = (ScrollRect)obj3;
						_listScrollPosition = (IsAlive((Object)(object)val10) ? val10.verticalNormalizedPosition : 1f);
						_selectedCategory = id;
						BuildWindow();
					});
					ButtonActions.Add(val4);
					val.AddButton(text + ((id == _selectedCategory) ? "  *" : ""), val4, "category_" + ButtonActions.Count);
					val.Last.SetPreferredSize(330f, 42f);
				}
			}
			val.End();
			val.End();
			val.BeginColumn(5f);
			val.BeginScroll(438f);
			if (_tab == "mods")
			{
				val.AddLabel(Select("选择要启停的 MOD；游戏中已加载的 DLL 不会立即卸载。", "Toggle mods for the next launch; loaded DLLs remain active in this session."), "details");
			}
			else
			{
				AddPreferenceDetails(val);
			}
			val.End();
			val.End();
			val.End();
			val.BeginRow(8f);
			Action val5 = ToAction(RevertChanges);
			ButtonActions.Add(val5);
			val.AddButton(Select("还原", "REVERT"), val5, "revert");
			val.Last.SetPreferredSize(180f, 42f);
			Action val6 = ToAction(ApplyChanges);
			ButtonActions.Add(val6);
			val.AddButton(Select("应用", "APPLY"), val6, "apply");
			val.Last.SetPreferredSize(180f, 42f);
			Action val7 = ToAction(RequestClose);
			ButtonActions.Add(val7);
			val.AddButton(Select("关闭", "CLOSE"), val7, "close");
			val.Last.SetPreferredSize(180f, 42f);
			val.End();
			CustomUIWindow val8 = val.Show();
			object obj;
			if (val8 == null)
			{
				obj = null;
			}
			else
			{
				GameObject root = val8.root;
				obj = ((root != null) ? root.GetComponentsInChildren<ScrollRect>(true) : null);
			}
			Il2CppArrayBase<ScrollRect> val9 = (Il2CppArrayBase<ScrollRect>)obj;
			if (val9 != null && val9.Length >= 2)
			{
				SetScrollWidth(val9[0], 350f);
				SetScrollWidth(val9[1], 505f);
				Canvas.ForceUpdateCanvases();
				val9[0].verticalNormalizedPosition = _listScrollPosition;
			}
			object value;
			if (val8 == null)
			{
				value = null;
			}
			else
			{
				CustomUIWindowFrame frame = val8.frame;
				value = ((frame != null) ? frame.closeButton : null);
			}
			if (IsAlive((Object)value))
			{
				val8.frame.closeButton.onClick = new ButtonClickedEvent();
				((UnityEvent)val8.frame.closeButton.onClick).AddListener(UnityAction.op_Implicit((Action)RequestClose));
			}
			ModManagerWindowResizer.Attach(val8);
		}

		private static void AddEntry(CustomUIBuilder builder, ManagedModEntry entry, int index)
		{
			bool desiredState = GetDesiredState(entry);
			string text = BuildEntryDisplay(entry, desiredState);
			ManagedModEntry captured = entry;
			Action<bool> val = ToAction((bool enabled) =>
			{
				ChangeState(captured, index, enabled);
			});
			ToggleActions.Add(val);
			builder.AddToggle(text, desiredState, val, "mod_" + index);
			CustomUIElement last = builder.Last;
			last.SetPreferredSize(330f, 62f);
			if ((Object)(object)last.text != (Object)null)
			{
				((TMP_Text)last.text).fontSize = 14f;
				((TMP_Text)last.text).enableWordWrapping = false;
				((TMP_Text)last.text).overflowMode = (TextOverflowModes)1;
			}
			if ((Object)(object)last.toggle != (Object)null)
			{
				((Selectable)last.toggle).interactable = entry.CanToggle && !entry.IsSelf;
			}
			last.SetTooltip(entry.FilePath + "\n" + GetDisplayStatus(entry, desiredState));
		}

		private static void SetScrollWidth(ScrollRect scroll, float width)
		{
			if (IsAlive((Object)(object)scroll))
			{
				LayoutElement val = ((Component)scroll).GetComponent<LayoutElement>() ?? ((Component)scroll).gameObject.AddComponent<LayoutElement>();
				val.minWidth = width;
				val.preferredWidth = width;
				val.flexibleWidth = 0f;
			}
		}

		private static void AddTab(CustomUIBuilder builder, string id, string label)
		{
			Action val = ToAction(() =>
			{
				_tab = id;
				_listScrollPosition = 1f;
				BuildWindow();
			});
			ButtonActions.Add(val);
			builder.AddButton(label + ((_tab == id) ? "  *" : ""), val, "tab_" + id);
			builder.Last.SetPreferredSize(160f, 36f);
		}

		private static void AddPreferenceDetails(CustomUIBuilder builder)
		{
			string currentLocaleCode = ModLocalization.CurrentLocaleCode;
			IReadOnlyList<MelonPreferences_Category> source = ModPreferences.Categories();
			MelonPreferences_Category val = source.FirstOrDefault((MelonPreferences_Category item) => item.Identifier == _selectedCategory && HasRows(item)) ?? ((IEnumerable<MelonPreferences_Category>)source).FirstOrDefault((Func<MelonPreferences_Category, bool>)HasRows);
			if (val == null)
			{
				builder.AddLabel(Select("没有可编辑的注册配置。", "No registered editable preferences."), (string)null);
				return;
			}
			_selectedCategory = val.Identifier;
			builder.AddLabel(ModPreferenceTranslations.Category(currentLocaleCode, val.Identifier, val.DisplayName), "selected_category");
			ConfigureLabel(builder.Last, 18f, (FontStyles)1, 480f, 30f);
			foreach (MelonPreferences_Entry item in ModPreferences.Entries(val))
			{
				if (_tab == "hotkeys" != ModPreferences.IsHotkey(item))
				{
					continue;
				}
				MelonPreferences_Entry current2 = item;
				string text = ModPreferenceTranslations.Entry(currentLocaleCode, val.Identifier, item.Identifier, item.DisplayName);
				string tooltip = ModPreferenceTranslations.Description(currentLocaleCode, val.Identifier, item.Identifier, item.Description ?? item.Identifier);
				if (item.GetReflectedType() == typeof(bool))
				{
					Action<bool> val2 = ToAction((bool value) =>
					{
						StagePreference(current2, value.ToString());
					});
					ToggleActions.Add(val2);
					builder.AddToggle(text, bool.TryParse(ModPreferences.Value(item), out var result) & result, val2, "pref_" + ToggleActions.Count);
					builder.Last.SetPreferredSize(480f, 44f);
					builder.Last.SetTooltip(tooltip);
					continue;
				}
				builder.AddLabel(text, (string)null);
				ConfigureLabel(builder.Last, 14f, (FontStyles)0, 480f, 23f);
				Action<string> val3 = ToInputAction((string value) =>
				{
					StagePreference(current2, value);
				});
				InputActions.Add(val3);
				if (ModPreferences.IsHotkey(item))
				{
					builder.BeginRow(5f);
				}
				builder.AddInput(text, ModPreferences.Value(item), val3, "pref_" + InputActions.Count);
				builder.Last.SetPreferredSize(ModPreferences.IsHotkey(item) ? 345f : 480f, 39f);
				builder.Last.SetTooltip(tooltip);
				if (!ModPreferences.IsHotkey(item))
				{
					continue;
				}
				Action val4 = ToAction(() =>
				{
					_capturingEntry = current2;
					_captureDelay = 2;
					_notice = Select("按下新快捷键；Esc 取消。", "Press a shortcut; Esc cancels.");
					CustomUIManager instance = CustomUIManager.Instance;
					CustomUIElement val5 = ((instance != null) ? instance.GetElement("xiaowo_mod_manager", "notice") : null);
					if ((Object)(object)((val5 != null) ? val5.text : null) != (Object)null)
					{
						((TMP_Text)val5.text).text = _notice;
					}
				});
				ButtonActions.Add(val4);
				builder.AddButton(Select("按键绑定", "BIND KEY"), val4, "capture_" + ButtonActions.Count);
				builder.Last.SetPreferredSize(125f, 39f);
				builder.End();
			}
			if (!(_tab == "hotkeys"))
			{
				return;
			}
			foreach (string item2 in ModPreferences.Conflicts())
			{
				builder.AddLabel(Select("冲突：", "Conflict: ") + item2, (string)null);
				ConfigureLabel(builder.Last, 13f, (FontStyles)0, 480f, 36f);
			}
			static bool HasRows(MelonPreferences_Category item)
			{
				return ModPreferences.Entries(item).Any((MelonPreferences_Entry entry) => _tab == "hotkeys" == ModPreferences.IsHotkey(entry));
			}
		}

		private static bool MatchesSearch(string value)
		{
			if (_search.Length != 0)
			{
				return value.Contains(_search, StringComparison.CurrentCultureIgnoreCase);
			}
			return true;
		}

		private static void StagePreference(MelonPreferences_Entry entry, string value)
		{
			bool flag = ModPreferences.Stage(entry, value, out var error);
			_notice = (flag ? (Select("待应用配置：", "Pending preferences: ") + ModPreferences.PendingCount) : (ModPreferenceTranslations.Entry(ModLocalization.CurrentLocaleCode, entry.Category.Identifier, entry.Identifier, entry.DisplayName) + ": " + error));
			if (flag && ModPreferences.IsHotkey(entry))
			{
				string text = ModPreferences.Conflicts().FirstOrDefault((string item) => item.Contains(entry.Category.DisplayName + "/" + entry.DisplayName, StringComparison.Ordinal));
				if (text != null)
				{
					_notice = Select("热键冲突：", "Hotkey conflict: ") + text;
				}
			}
			CustomUIManager instance = CustomUIManager.Instance;
			CustomUIElement val = (IsAlive((Object)(object)instance) ? instance.GetElement("xiaowo_mod_manager", "notice") : null);
			if ((Object)(object)((val != null) ? val.text : null) != (Object)null)
			{
				((TMP_Text)val.text).text = _notice;
			}
		}

		private static void RevertChanges()
		{
			PendingStates.Clear();
			ModPreferences.Revert();
			_notice = Select("已还原未应用的更改。", "Unapplied changes reverted.");
			BuildWindow();
		}

		private static void ChangeState(ManagedModEntry entry, int index, bool enabled)
		{
			StageState(entry, enabled, out _notice);
			CustomUIManager instance = CustomUIManager.Instance;
			if (IsAlive((Object)(object)instance))
			{
				bool desiredState = GetDesiredState(entry);
				CustomUIElement element = instance.GetElement("xiaowo_mod_manager", "mod_" + index);
				if ((Object)(object)((element != null) ? element.text : null) != (Object)null)
				{
					((TMP_Text)element.text).text = BuildEntryDisplay(entry, desiredState);
				}
				if (element != null)
				{
					element.SetTooltip(entry.FilePath + "\n" + GetDisplayStatus(entry, desiredState));
				}
				CustomUIElement element2 = instance.GetElement("xiaowo_mod_manager", "notice");
				if ((Object)(object)((element2 != null) ? element2.text : null) != (Object)null)
				{
					((TMP_Text)element2.text).text = _notice;
				}
			}
		}

		internal static bool StageState(ManagedModEntry entry, bool enabled, out string message)
		{
			if (entry == null)
			{
				message = Select("未找到该 MOD。", "The mod entry is unavailable.");
				return false;
			}
			if (entry.IsSelf || !entry.CanToggle)
			{
				message = (entry.IsSelf ? Select("主 MOD 不能停用。", "The core mod cannot be disabled.") : Select("该 MOD 当前不能管理。", "This mod cannot currently be managed."));
				return false;
			}
			string key = NormalizePath(entry.FilePath);
			if (enabled == entry.EnabledNextLaunch)
			{
				PendingStates.Remove(key);
			}
			else
			{
				PendingStates[key] = enabled;
			}
			message = ((PendingStates.Count == 0) ? Select("没有待应用的更改。", "There are no pending changes.") : (Select("待应用更改：", "Pending changes: ") + PendingStates.Count));
			return true;
		}

		private static void ApplyChanges()
		{
			ApplyPendingChanges(out _notice);
			BuildWindow();
		}

		internal static bool ApplyPendingChanges(out string message)
		{
			if (PendingStates.Count == 0 && ModPreferences.PendingCount == 0)
			{
				message = Select("没有待应用的更改。", "There are no pending changes.");
				return true;
			}
			if (!ModPreferences.Apply(out var message2))
			{
				message = message2;
				return false;
			}
			Dictionary<string, ManagedModEntry> dictionary = ModManager.Scan().ToDictionary((ManagedModEntry entry) => NormalizePath(entry.FilePath), StringComparer.OrdinalIgnoreCase);
			KeyValuePair<string, bool>[] array = PendingStates.ToArray();
			int num = 0;
			List<string> list = new List<string>();
			KeyValuePair<string, bool>[] array2 = array;
			for (int num2 = 0; num2 < array2.Length; num2++)
			{
				KeyValuePair<string, bool> keyValuePair = array2[num2];
				if (!dictionary.TryGetValue(keyValuePair.Key, out var value))
				{
					list.Add(Path.GetFileName(keyValuePair.Key) + ": " + Select("文件状态已改变", "file state changed"));
					continue;
				}
				if (!ModManager.TrySetEnabled(value, keyValuePair.Value, out var message3))
				{
					list.Add(value.FileName + ": " + message3);
					continue;
				}
				PendingStates.Remove(keyValuePair.Key);
				num++;
			}
			if (list.Count == 0)
			{
				message = Select("已应用 ", "Applied ") + num + Select(" 项 DLL 更改；配置已保存。部分修改重启后生效。", " DLL change(s); preferences saved. Some changes require restart.");
				return true;
			}
			message = Select("已应用 ", "Applied ") + num + Select(" 项；失败：", "; failed: ") + string.Join("；", list);
			return false;
		}

		internal static void ClearPendingChanges()
		{
			PendingStates.Clear();
			ModPreferences.Revert();
		}

		private static bool GetDesiredState(ManagedModEntry entry)
		{
			if (!PendingStates.TryGetValue(NormalizePath(entry.FilePath), out var value))
			{
				return entry.EnabledNextLaunch;
			}
			return value;
		}

		private static string GetDisplayStatus(ManagedModEntry entry, bool desiredState)
		{
			if (desiredState != entry.EnabledNextLaunch)
			{
				if (!desiredState)
				{
					return Select("待应用 / 下次停用", "Pending / disable next launch");
				}
				return Select("待应用 / 下次启用", "Pending / enable next launch");
			}
			return ModManager.GetStatus(entry);
		}

		private static string BuildEntryDisplay(ManagedModEntry entry, bool desiredState)
		{
			string text = entry.Name;
			if (!string.IsNullOrWhiteSpace(entry.Version))
			{
				text = text + "  v" + entry.Version;
			}
			if (!string.IsNullOrWhiteSpace(entry.Author))
			{
				text = text + "  |  " + entry.Author;
			}
			return text + "\n" + GetDisplayStatus(entry, desiredState) + "  |  " + entry.FileName;
		}

		private static string NormalizePath(string path)
		{
			return Path.GetFullPath(path ?? string.Empty);
		}

		private static void ConfigureLabel(CustomUIElement element, float size, FontStyles style, float width, float height)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			if (element != null)
			{
				element.SetPreferredSize(width, height);
				if (!((Object)(object)element.text == (Object)null))
				{
					((TMP_Text)element.text).fontSize = size;
					((TMP_Text)element.text).fontStyle = style;
					((TMP_Text)element.text).enableWordWrapping = false;
					((TMP_Text)element.text).overflowMode = (TextOverflowModes)1;
				}
			}
		}

		private static void CloseWindow()
		{
			ModManagerWindowResizer.BeforeRebuild();
			CustomUIManager instance = CustomUIManager.Instance;
			if (IsAlive((Object)(object)instance))
			{
				instance.CloseWindow("xiaowo_mod_manager_confirm");
				instance.CloseWindow("xiaowo_mod_manager");
			}
			PendingStates.Clear();
			ModPreferences.Revert();
			_capturingEntry = null;
			_notice = string.Empty;
		}

		private static void RequestClose()
		{
			if (PendingStates.Count == 0 && ModPreferences.PendingCount == 0)
			{
				CloseWindow();
				return;
			}
			CustomUIManager manager = CustomUIManager.Instance;
			if (!IsAlive((Object)(object)manager))
			{
				return;
			}
			CustomUIWindow window = manager.GetWindow("xiaowo_mod_manager_confirm");
			if (window == null || !window.IsVisible)
			{
				CustomUIBuilder val = CustomUIManager.Window("xiaowo_mod_manager_confirm", Select("未应用的更改", "UNAPPLIED CHANGES"), "overlay").SetSize(440f, 195f).Center()
					.SetCloseOnEscape(true);
				val.AddLabel(Select("离开会放弃尚未应用的修改。", "Leaving will discard unapplied changes."), (string)null);
				val.BeginRow(8f);
				Action val2 = ToAction(() =>
				{
					manager.CloseWindow("xiaowo_mod_manager_confirm");
				});
				Action val3 = ToAction(CloseWindow);
				ButtonActions.Add(val2);
				ButtonActions.Add(val3);
				val.AddButton(Select("继续编辑", "KEEP EDITING"), val2, "keep");
				val.AddButton(Select("放弃更改", "DISCARD"), val3, "discard");
				val.End();
				val.Show();
			}
		}

		private static void ForgetButton()
		{
			_menuButtonObject = null;
			_menuButton = null;
		}

		private static Action ToAction(Action action)
		{
			return DelegateSupport.ConvertDelegate<Action>((Delegate)action);
		}

		private static Action<bool> ToAction(Action<bool> action)
		{
			return DelegateSupport.ConvertDelegate<Action<bool>>((Delegate)action);
		}

		private static Action<string> ToInputAction(Action<string> action)
		{
			return DelegateSupport.ConvertDelegate<Action<string>>((Delegate)action);
		}

		private static string Select(string chinese, string english)
		{
			return ModLocalization.Select(chinese, english);
		}

		private static bool IsAlive(Object value)
		{
			if (value != (Object)null)
			{
				return Object.op_Implicit(value);
			}
			return false;
		}

		private static bool IsMainMenuVisible(MainMenuUIController menu)
		{
			if (!IsAlive((Object)(object)menu) || !((Component)menu).gameObject.activeInHierarchy)
			{
				return false;
			}
			GameObject moddingButton = menu.moddingButton;
			if (IsAlive((Object)(object)moddingButton) && IsAlive((Object)(object)moddingButton.transform.parent))
			{
				return ((Component)moddingButton.transform.parent).gameObject.activeInHierarchy;
			}
			return false;
		}
	}
	internal static class ModManagerWindowResizer
	{
		[Flags]
		private enum Edge
		{
			None = 0,
			Left = 1,
			Right = 2,
			Top = 4,
			Bottom = 8
		}

		private enum WidthKind
		{
			Full,
			Left,
			Right,
			Hotkey
		}

		private const float DefaultWidth = 900f;

		private const float DefaultHeight = 650f;

		private const float MinimumWidth = 720f;

		private const float MinimumHeight = 520f;

		private const float EdgeThickness = 8f;

		private const float CornerSize = 16f;

		private static readonly List<(RectTransform Rect, Edge Edge)> Handles = new List<(RectTransform, Edge)>();

		private static readonly List<(LayoutElement Layout, WidthKind Kind)> Widths = new List<(LayoutElement, WidthKind)>();

		private static CustomUIWindow _window;

		private static RectTransform _rect;

		private static Camera _camera;

		private static ScrollRect _leftScroll;

		private static ScrollRect _rightScroll;

		private static Vector2 _size = new Vector2(900f, 650f);

		private static Vector2 _position;

		private static bool _hasPosition;

		private static Edge _activeEdge;

		private static bool _ownsCursor;

		private static Vector2 _startPointer;

		private static Vector2 _startSize;

		private static Vector2 _startPosition;

		private static Vector2 _startPivot;

		internal static float Width => _size.x;

		internal static float Height => _size.y;

		internal static void Attach(CustomUIWindow window)
		{
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_0058: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			_window = window;
			_rect = ((window != null) ? window.Rect : null);
			if (Alive((Object)(object)_rect))
			{
				Canvas componentInParent = ((Component)_rect).GetComponentInParent<Canvas>();
				_camera = (((Object)(object)componentInParent != (Object)null && (int)componentInParent.renderMode != 0) ? componentInParent.worldCamera : null);
				_size = Clamp(_size);
				_rect.SetSizeWithCurrentAnchors((Axis)0, _size.x);
				_rect.SetSizeWithCurrentAnchors((Axis)1, _size.y);
				if (_hasPosition)
				{
					_rect.anchoredPosition = _position;
				}
				GameObject root = window.root;
				Il2CppArrayBase<ScrollRect> val = ((root != null) ? root.GetComponentsInChildren<ScrollRect>(true) : null);
				if (val != null && val.Length >= 2)
				{
					_leftScroll = val[0];
					_rightScroll = val[1];
				}
				TrackWidths();
				ApplyLayout();
				CreateHandles();
			}
		}

		internal static void BeforeRebuild()
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			//IL_0028: Unknown result type (might be due to invalid IL or missing references)
			//IL_0030: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			if (Alive((Object)(object)_rect))
			{
				Rect rect = _rect.rect;
				float width = rect.width;
				rect = _rect.rect;
				_size = new Vector2(width, rect.height);
				_position = _rect.anchoredPosition;
				_hasPosition = true;
			}
			ClearWindow();
		}

		internal static void Dispose()
		{
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			ClearWindow();
			_size = new Vector2(900f, 650f);
			_position = Vector2.zero;
			_hasPosition = false;
		}

		internal static string Status()
		{
			//IL_007b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			string[] array = new string[10]
			{
				"resize=",
				(Alive((Object)(object)_rect) && Handles.Count == 8).ToString(),
				" size=",
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			string text;
			if (!Alive((Object)(object)_rect))
			{
				text = _size.x + "x" + _size.y;
			}
			else
			{
				Rect rect = _rect.rect;
				string text2 = rect.width.ToString();
				rect = _rect.rect;
				text = text2 + "x" + rect.height;
			}
			array[3] = text;
			array[4] = " handles=";
			array[5] = Handles.Count.ToString();
			array[6] = " leftScroll=";
			ScrollRect leftScroll = _leftScroll;
			float? num;
			if (leftScroll == null)
			{
				num = null;
			}
			else
			{
				LayoutElement component = ((Component)leftScroll).GetComponent<LayoutElement>();
				num = ((component != null) ? new float?(component.preferredHeight) : ((float?)null));
			}
			float? num2 = num;
			array[7] = num2.GetValueOrDefault().ToString();
			array[8] = " rightScroll=";
			ScrollRect rightScroll = _rightScroll;
			float? num3;
			if (rightScroll == null)
			{
				num3 = null;
			}
			else
			{
				LayoutElement component2 = ((Component)rightScroll).GetComponent<LayoutElement>();
				num3 = ((component2 != null) ? new float?(component2.preferredHeight) : ((float?)null));
			}
			num2 = num3;
			array[9] = num2.GetValueOrDefault().ToString();
			return string.Concat(array);
		}

		internal static string TestResize(float width, float height)
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
			if (!Alive((Object)(object)_rect))
			{
				return "MOD_MANAGER_RESIZE FAIL: window unavailable";
			}
			ApplySize(Clamp(new Vector2(width, height)), _rect.anchoredPosition);
			return "MOD_MANAGER_RESIZE " + Status();
		}

		internal static string TestPointerDrag(float deltaX, float deltaY)
		{
			//IL_014f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0154: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_0166: Unknown result type (might be due to invalid IL or missing references)
			//IL_016e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0182: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ba: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ef: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fc: Unknown result type (might be due to invalid IL or missing references)
			//IL_0106: Unknown result type (might be due to invalid IL or missing references)
			//IL_010b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0115: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_011f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0122: Unknown result type (might be due to invalid IL or missing references)
			//IL_0127: Unknown result type (might be due to invalid IL or missing references)
			if (!Alive((Object)(object)_rect))
			{
				return "MOD_MANAGER_POINTER_DRAG FAIL: window unavailable";
			}
			if (!Alive((Object)(object)Handles.Find(((RectTransform Rect, Edge Edge) handle) => handle.Edge == (Edge.Right | Edge.Bottom)).Rect))
			{
				return "MOD_MANAGER_POINTER_DRAG FAIL: corner unavailable";
			}
			Rect rect = _rect.rect;
			Vector2 val = RectTransformUtility.WorldToScreenPoint(_camera, ((Transform)_rect).TransformPoint(Vector2.op_Implicit(new Vector2(rect.xMax - 4f, rect.yMin + 4f))));
			Edge edge = FindEdge(val);
			if (edge != (Edge.Right | Edge.Bottom))
			{
				return "MOD_MANAGER_POINTER_DRAG FAIL: corner not hit (" + edge.ToString() + ")";
			}
			if (!LocalInParent(val, out _startPointer))
			{
				return "MOD_MANAGER_POINTER_DRAG FAIL: pointer unavailable";
			}
			_activeEdge = edge;
			Rect rect2 = _rect.rect;
			float width = rect2.width;
			rect2 = _rect.rect;
			_startSize = new Vector2(width, rect2.height);
			_startPosition = _rect.anchoredPosition;
			_startPivot = _rect.pivot;
			try
			{
				Drag(val + new Vector2(deltaX, deltaY));
				return "MOD_MANAGER_POINTER_DRAG " + Status();
			}
			finally
			{
				_activeEdge = Edge.None;
				rect2 = _rect.rect;
				float width2 = rect2.width;
				rect2 = _rect.rect;
				_size = new Vector2(width2, rect2.height);
				_position = _rect.anchoredPosition;
				_hasPosition = true;
			}
		}

		internal static void Tick()
		{
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0074: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_004f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_010d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0112: Unknown result type (might be due to invalid IL or missing references)
			//IL_011c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0121: Unknown result type (might be due to invalid IL or missing references)
			if (Alive((Object)(object)_rect))
			{
				CustomUIWindow window = _window;
				if (window != null && window.IsVisible && Application.isFocused)
				{
					Vector2 pointer = Vector2.op_Implicit(Input.mousePosition);
					Rect rect;
					if (_activeEdge != Edge.None)
					{
						if (Input.GetMouseButton(0))
						{
							Drag(pointer);
						}
						else
						{
							_activeEdge = Edge.None;
							rect = _rect.rect;
							float width = rect.width;
							rect = _rect.rect;
							_size = new Vector2(width, rect.height);
							_position = _rect.anchoredPosition;
							_hasPosition = true;
						}
					}
					Edge edge = ((_activeEdge != Edge.None) ? _activeEdge : FindEdge(pointer));
					if (_activeEdge == Edge.None && edge != Edge.None && Input.GetMouseButtonDown(0) && LocalInParent(pointer, out _startPointer))
					{
						_activeEdge = edge;
						rect = _rect.rect;
						float width2 = rect.width;
						rect = _rect.rect;
						_startSize = new Vector2(width2, rect.height);
						_startPosition = _rect.anchoredPosition;
						_startPivot = _rect.pivot;
					}
					if (edge != Edge.None)
					{
						ContainerWindowResizer.SetModManagerCursor((int)edge);
						_ownsCursor = true;
					}
					else
					{
						ReleaseCursor();
					}
					return;
				}
			}
			_activeEdge = Edge.None;
			ReleaseCursor();
		}

		private static void Drag(Vector2 pointer)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0089: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0090: Unknown result type (might be due to invalid IL or missing references)
			//IL_0095: Unknown result type (might be due to invalid IL or missing references)
			//IL_009a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0081: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00eb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			if (LocalInParent(pointer, out var local))
			{
				Vector2 val = local - _startPointer;
				Vector2 startSize = _startSize;
				if ((_activeEdge & Edge.Left) != 0)
				{
					startSize.x -= val.x;
				}
				else if ((_activeEdge & Edge.Right) != 0)
				{
					startSize.x += val.x;
				}
				if ((_activeEdge & Edge.Bottom) != 0)
				{
					startSize.y -= val.y;
				}
				else if ((_activeEdge & Edge.Top) != 0)
				{
					startSize.y += val.y;
				}
				Vector2 val2 = Clamp(startSize);
				Vector2 val3 = val2 - _startSize;
				Vector2 startPosition = _startPosition;
				if ((_activeEdge & Edge.Left) != 0)
				{
					startPosition.x -= (1f - _startPivot.x) * val3.x;
				}
				else if ((_activeEdge & Edge.Right) != 0)
				{
					startPosition.x += _startPivot.x * val3.x;
				}
				if ((_activeEdge & Edge.Bottom) != 0)
				{
					startPosition.y -= (1f - _startPivot.y) * val3.y;
				}
				else if ((_activeEdge & Edge.Top) != 0)
				{
					startPosition.y += _startPivot.y * val3.y;
				}
				ApplySize(val2, startPosition);
			}
		}

		private static Vector2 Clamp(Vector2 requested)
		{
			//IL_0044: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			RectTransform rect = _rect;
			object obj;
			if (rect == null)
			{
				obj = null;
			}
			else
			{
				Transform parent = ((Transform)rect).parent;
				obj = ((parent != null) ? ((Component)parent).GetComponent<RectTransform>() : null);
			}
			RectTransform val = (RectTransform)obj;
			float num;
			Rect rect2;
			if (!Alive((Object)(object)val))
			{
				num = Math.Max(900f, (float)Screen.width - 24f);
			}
			else
			{
				rect2 = val.rect;
				num = Math.Max(420f, rect2.width - 24f);
			}
			float num2 = num;
			float num3;
			if (!Alive((Object)(object)val))
			{
				num3 = Math.Max(650f, (float)Screen.height - 24f);
			}
			else
			{
				rect2 = val.rect;
				num3 = Math.Max(360f, rect2.height - 24f);
			}
			float num4 = num3;
			return new Vector2(Mathf.Clamp(requested.x, Math.Min(720f, num2), num2), Mathf.Clamp(requested.y, Math.Min(520f, num4), num4));
		}

		private static void ApplySize(Vector2 size, Vector2 position)
		{
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			if (Alive((Object)(object)_rect))
			{
				_rect.SetSizeWithCurrentAnchors((Axis)0, size.x);
				_rect.SetSizeWithCurrentAnchors((Axis)1, size.y);
				_rect.anchoredPosition = position;
				ApplyLayout();
			}
		}

		private static void ApplyLayout()
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002b: Unknown result type (might be due to invalid IL or missing references)
			if (!Alive((Object)(object)_rect))
			{
				return;
			}
			Rect rect = _rect.rect;
			float width = rect.width;
			rect = _rect.rect;
			float height = rect.height;
			float num = Math.Max(420f, width - 45f);
			float num2 = Mathf.Clamp(num * 350f / 855f, 270f, 550f);
			float num3 = num - num2;
			SetScroll(_leftScroll, num2, Math.Max(210f, height - 285f));
			SetScroll(_rightScroll, num3, Math.Max(260f, height - 212f));
			foreach (var width2 in Widths)
			{
				if (Alive((Object)(object)width2.Layout))
				{
					float num4 = width2.Kind switch
					{
						WidthKind.Full => width - 40f, 
						WidthKind.Left => num2 - 20f, 
						WidthKind.Right => num3 - 25f, 
						_ => num3 - 160f, 
					};
					width2.Layout.minWidth = num4;
					width2.Layout.preferredWidth = num4;
				}
			}
		}

		private static void SetScroll(ScrollRect scroll, float width, float height)
		{
			if (Alive((Object)(object)scroll))
			{
				LayoutElement val = ((Component)scroll).GetComponent<LayoutElement>() ?? ((Component)scroll).gameObject.AddComponent<LayoutElement>();
				val.minWidth = width;
				val.preferredWidth = width;
				val.flexibleWidth = 0f;
				val.minHeight = height;
				val.preferredHeight = height;
				val.flexibleHeight = 0f;
			}
		}

		private static void TrackWidths()
		{
			Widths.Clear();
			CustomUIWindow window = _window;
			if (!Alive((Object)(object)((window != null) ? window.root : null)))
			{
				return;
			}
			foreach (LayoutElement componentsInChild in _window.root.GetComponentsInChildren<LayoutElement>(true))
			{
				if (!Alive((Object)(object)componentsInChild))
				{
					continue;
				}
				GameObject gameObject = ((Component)componentsInChild).gameObject;
				ScrollRect leftScroll = _leftScroll;
				if ((Object)(object)gameObject == (Object)(object)((leftScroll != null) ? ((Component)leftScroll).gameObject : null))
				{
					continue;
				}
				GameObject gameObject2 = ((Component)componentsInChild).gameObject;
				ScrollRect rightScroll = _rightScroll;
				if (!((Object)(object)gameObject2 == (Object)(object)((rightScroll != null) ? ((Component)rightScroll).gameObject : null)))
				{
					float preferredWidth = componentsInChild.preferredWidth;
					WidthKind? widthKind;
					if (Math.Abs(preferredWidth - 860f) < 1f)
					{
						widthKind = WidthKind.Full;
					}
					else if (Math.Abs(preferredWidth - 330f) < 1f)
					{
						widthKind = WidthKind.Left;
					}
					else if (Math.Abs(preferredWidth - 480f) < 1f)
					{
						widthKind = WidthKind.Right;
					}
					else
					{
						widthKind = ((Math.Abs(preferredWidth - 345f) < 1f) ? new WidthKind?(WidthKind.Hotkey) : ((WidthKind?)null));
					}
					if (widthKind.HasValue)
					{
						Widths.Add((componentsInChild, widthKind.Value));
					}
				}
			}
		}

		private static void CreateHandles()
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_008f: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0128: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_0146: Unknown result type (might be due to invalid IL or missing references)
			//IL_0161: Unknown result type (might be due to invalid IL or missing references)
			//IL_0170: Unknown result type (might be due to invalid IL or missing references)
			//IL_017f: Unknown result type (might be due to invalid IL or missing references)
			//IL_019a: Unknown result type (might be due to invalid IL or missing references)
			//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			AddHandle("Left", Edge.Left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(8f, -32f));
			AddHandle("Right", Edge.Right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(8f, -32f));
			AddHandle("Top", Edge.Top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-32f, 8f));
			AddHandle("Bottom", Edge.Bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 8f));
			AddHandle("TopLeft", Edge.Left | Edge.Top, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, 16f));
			AddHandle("TopRight", Edge.Right | Edge.Top, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(16f, 16f));
			AddHandle("BottomLeft", Edge.Left | Edge.Bottom, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 16f));
			AddHandle("BottomRight", Edge.Right | Edge.Bottom, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f));
		}

		private static void AddHandle(string name, Edge edge, Vector2 min, Vector2 max, Vector2 size)
		{
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00af: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d0: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject("XIAOWOModManagerResize" + name, new Type[3]
			{
				Il2CppType.Of<RectTransform>(),
				Il2CppType.Of<CanvasRenderer>(),
				Il2CppType.Of<Image>()
			});
			val.transform.SetParent((Transform)(object)_rect, false);
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchorMin = min;
			component.anchorMax = max;
			component.pivot = new Vector2(0.5f, 0.5f);
			? val2 = component;
			float num;
			if ((edge & Edge.Left) != 0)
			{
				num = -4f;
			}
			else
			{
				num = (((edge & Edge.Right) != 0) ? 4f : 0f);
			}
			? val3 = val2;
			float num2 = num;
			float num3;
			if ((edge & Edge.Top) != 0)
			{
				num3 = 4f;
			}
			else
			{
				num3 = (((edge & Edge.Bottom) != 0) ? (-4f) : 0f);
			}
			((RectTransform)val3).anchoredPosition = new Vector2(num2, num3);
			component.sizeDelta = size;
			Image component2 = val.GetComponent<Image>();
			((Graphic)component2).color = new Color(1f, 1f, 1f, 0.001f);
			((Graphic)component2).raycastTarget = false;
			((Transform)component).SetAsLastSibling();
			Handles.Add((component, edge));
		}

		private static Edge FindEdge(Vector2 pointer)
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Unknown result type (might be due to invalid IL or missing references)
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0063: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_00dd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0178: Unknown result type (might be due to invalid IL or missing references)
			//IL_017d: Unknown result type (might be due to invalid IL or missing references)
			//IL_017e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0186: Expected Obj, but got Unknown
			//IL_022a: Unknown result type (might be due to invalid IL or missing references)
			if (!Alive((Object)(object)_rect) || IsCloseButton(pointer))
			{
				return Edge.None;
			}
			Vector2 val = default;
			if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, pointer, _camera, ref val))
			{
				return Edge.None;
			}
			Rect rect = _rect.rect;
			if (!rect.Contains(val))
			{
				return Edge.None;
			}
			bool flag = val.x - rect.xMin <= 10f;
			bool flag2 = rect.xMax - val.x <= 10f;
			bool flag3 = val.y - rect.yMin <= 10f;
			bool flag4 = rect.yMax - val.y <= 10f;
			bool flag5 = val.x - rect.xMin <= 16f;
			bool flag6 = rect.xMax - val.x <= 16f;
			bool flag7 = val.y - rect.yMin <= 16f;
			bool flag8 = rect.yMax - val.y <= 16f;
			Edge edge;
			if (flag5 & flag8)
			{
				edge = Edge.Left | Edge.Top;
			}
			else if (flag6 & flag8)
			{
				edge = Edge.Right | Edge.Top;
			}
			else if (flag5 & flag7)
			{
				edge = Edge.Left | Edge.Bottom;
			}
			else
			{
				edge = ((flag6 & flag7) ? (Edge.Right | Edge.Bottom) : ((Edge)((flag ? 1 : 0) | (flag2 ? 2 : 0) | (flag4 ? 4 : 0) | (flag3 ? 8 : 0))));
			}
			if (edge == Edge.None)
			{
				return edge;
			}
			EventSystem current = EventSystem.current;
			if ((Object)(object)current != (Object)null)
			{
				PointerEventData val2 = new PointerEventData(current)
				{
					position = pointer
				};
				List<RaycastResult> val3 = new List<RaycastResult>();
				current.RaycastAll(val2, val3);
				if (val3.Count > 0)
				{
					GameObject gameObject = val3[0].gameObject;
					Transform val4 = ((gameObject != null) ? gameObject.transform : null);
					if (!((Object)(object)val4 != (Object)null) || !val4.IsChildOf((Transform)(object)_rect))
					{
						return Edge.None;
					}
					return edge;
				}
			}
			Transform parent = ((Transform)_rect).parent;
			if ((Object)(object)parent == (Object)null)
			{
				return Edge.None;
			}
			for (int i = ((Transform)_rect).GetSiblingIndex() + 1; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				RectTransform component = ((Component)child).GetComponent<RectTransform>();
				if (((Component)child).gameObject.activeInHierarchy && Alive((Object)(object)component) && RectTransformUtility.RectangleContainsScreenPoint(component, pointer, _camera))
				{
					return Edge.None;
				}
			}
			return edge;
		}

		internal static string TestHitRegions()
		{
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_003f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			//IL_0092: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_010a: Unknown result type (might be due to invalid IL or missing references)
			//IL_013b: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_019e: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ec: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_01fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0283: Unknown result type (might be due to invalid IL or missing references)
			//IL_0288: Unknown result type (might be due to invalid IL or missing references)
			//IL_028d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0292: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_02b2: Unknown result type (might be due to invalid IL or missing references)
			//IL_037c: Unknown result type (might be due to invalid IL or missing references)
			//IL_037e: Unknown result type (might be due to invalid IL or missing references)
			//IL_039a: Unknown result type (might be due to invalid IL or missing references)
			//IL_03cd: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_03dc: Expected Obj, but got Unknown
			if (!Alive((Object)(object)_rect))
			{
				return "MOD_MANAGER_HIT_REGIONS FAIL: window unavailable";
			}
			Canvas.ForceUpdateCanvases();
			Rect rect = _rect.rect;
			(string, Vector2, Edge)[] array = new (string, Vector2, Edge)[9]
			{
				("left", new Vector2(rect.xMin + 4f, rect.center.y), Edge.Left),
				("right", new Vector2(rect.xMax - 4f, rect.center.y), Edge.Right),
				("top", new Vector2(rect.center.x, rect.yMax - 4f), Edge.Top),
				("bottom", new Vector2(rect.center.x, rect.yMin + 4f), Edge.Bottom),
				("top-left", new Vector2(rect.xMin + 4f, rect.yMax - 4f), Edge.Left | Edge.Top),
				("top-right", new Vector2(rect.xMax - 4f, rect.yMax - 4f), Edge.Right | Edge.Top),
				("bottom-left", new Vector2(rect.xMin + 4f, rect.yMin + 4f), Edge.Left | Edge.Bottom),
				("bottom-right", new Vector2(rect.xMax - 4f, rect.yMin + 4f), Edge.Right | Edge.Bottom),
				("center", rect.center, Edge.None)
			};
			List<string> list = new List<string>();
			(string, Vector2, Edge)[] array2 = array;
			for (int i = 0; i < array2.Length; i++)
			{
				(string, Vector2, Edge) tuple = array2[i];
				Edge edge = FindEdge(RectTransformUtility.WorldToScreenPoint(_camera, ((Transform)_rect).TransformPoint(Vector2.op_Implicit(tuple.Item2))));
				list.Add(tuple.Item1 + "=" + edge.ToString() + ((edge == tuple.Item3) ? " PASS" : " FAIL"));
			}
			Transform parent = ((Transform)_rect).parent;
			RectTransform value = ((parent != null) ? ((Component)parent).GetComponent<RectTransform>() : null);
			Canvas componentInParent = ((Component)_rect).GetComponentInParent<Canvas>();
			Vector2 val = RectTransformUtility.WorldToScreenPoint(_camera, ((Transform)_rect).TransformPoint(Vector2.op_Implicit(rect.center)));
			string[] array3 = new string[16]
			{
				"canvas=",
				((componentInParent != null) ? ((object)componentInParent.renderMode/*cast due to constrained. prefix*/).ToString() : null) ?? "none",
				" camera=",
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null,
				null
			};
			object obj;
			if (componentInParent == null)
			{
				obj = null;
			}
			else
			{
				Camera worldCamera = componentInParent.worldCamera;
				obj = ((worldCamera != null) ? ((Object)worldCamera).name : null);
			}
			if (obj == null)
			{
				obj = "none";
			}
			array3[3] = (string)obj;
			array3[4] = " parent=";
			Transform parent2 = ((Transform)_rect).parent;
			array3[5] = ((parent2 != null) ? ((Object)parent2).name : null) ?? "none";
			array3[6] = " parentRect=";
			array3[7] = Alive((Object)(object)value).ToString();
			array3[8] = " screen=";
			array3[9] = Screen.width.ToString();
			array3[10] = "x";
			array3[11] = Screen.height.ToString();
			array3[12] = " center=";
			Vector2 local = val;
			array3[13] = ((object)local/*cast due to constrained. prefix*/).ToString();
			array3[14] = " localParent=";
			array3[15] = LocalInParent(val, out local).ToString();
			list.Add(string.Concat(array3));
			EventSystem current = EventSystem.current;
			if ((Object)(object)current != (Object)null)
			{
				PointerEventData val2 = new PointerEventData(current)
				{
					position = val
				};
				List<RaycastResult> val3 = new List<RaycastResult>();
				current.RaycastAll(val2, val3);
				string[] array4 = new string[6] { "centerHit=", null, null, null, null, null };
				object obj2;
				if (val3.Count <= 0)
				{
					obj2 = "none";
				}
				else
				{
					GameObject gameObject = val3[0].gameObject;
					obj2 = ((gameObject != null) ? ((Object)gameObject).name : null);
				}
				array4[1] = (string)obj2;
				array4[2] = " rectName=";
				array4[3] = ((Object)_rect).name;
				array4[4] = " root=";
				CustomUIWindow window = _window;
				object obj3;
				if (window == null)
				{
					obj3 = null;
				}
				else
				{
					GameObject root = window.root;
					obj3 = ((root != null) ? ((Object)root).name : null);
				}
				if (obj3 == null)
				{
					obj3 = "none";
				}
				array4[5] = (string)obj3;
				list.Add(string.Concat(array4));
			}
			return "MOD_MANAGER_HIT_REGIONS " + string.Join("; ", list);
		}

		private static bool IsCloseButton(Vector2 pointer)
		{
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			CustomUIWindow window = _window;
			object obj;
			if (window == null)
			{
				obj = null;
			}
			else
			{
				CustomUIWindowFrame frame = window.frame;
				if (frame == null)
				{
					obj = null;
				}
				else
				{
					Button closeButton = frame.closeButton;
					obj = ((closeButton != null) ? ((Component)closeButton).GetComponent<RectTransform>() : null);
				}
			}
			RectTransform val = (RectTransform)obj;
			if (Alive((Object)(object)val))
			{
				return RectTransformUtility.RectangleContainsScreenPoint(val, pointer, _camera);
			}
			return false;
		}

		private static bool LocalInParent(Vector2 pointer, out Vector2 local)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			local = Vector2.zero;
			RectTransform rect = _rect;
			object obj;
			if (rect == null)
			{
				obj = null;
			}
			else
			{
				Transform parent = ((Transform)rect).parent;
				obj = ((parent != null) ? ((Component)parent).GetComponent<RectTransform>() : null);
			}
			RectTransform val = (RectTransform)obj;
			if (Alive((Object)(object)val))
			{
				return RectTransformUtility.ScreenPointToLocalPointInRectangle(val, pointer, _camera, ref local);
			}
			return false;
		}

		private static void ClearWindow()
		{
			foreach (var handle in Handles)
			{
				if (Alive((Object)(object)handle.Rect))
				{
					Object.Destroy((Object)(object)((Component)handle.Rect).gameObject);
				}
			}
			Handles.Clear();
			Widths.Clear();
			_window = null;
			_rect = null;
			_camera = null;
			_leftScroll = null;
			_rightScroll = null;
			_activeEdge = Edge.None;
			ReleaseCursor();
		}

		private static void ReleaseCursor()
		{
			if (_ownsCursor)
			{
				ContainerWindowResizer.ResetModManagerCursor();
				_ownsCursor = false;
			}
		}

		private static bool Alive(Object value)
		{
			if (value != (Object)null)
			{
				return Object.op_Implicit(value);
			}
			return false;
		}

		static ModManagerWindowResizer()
		{
			//IL_001e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0023: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	internal static class ModPreferences
	{
		private static readonly Dictionary<string, string> Pending = new Dictionary<string, string>(StringComparer.Ordinal);

		internal static int PendingCount => Pending.Count;

		internal static IReadOnlyList<MelonPreferences_Category> Categories()
		{
			return (from category in MelonPreferences.Categories
				where !category.IsHidden && category.Entries.Any(IsSupported)
				orderby category.DisplayName
				select category).ToArray();
		}

		internal static IReadOnlyList<MelonPreferences_Entry> Entries(MelonPreferences_Category category)
		{
			return category?.Entries.Where(IsSupported).OrderBy((MelonPreferences_Entry entry) => entry.DisplayName).ToArray() ?? Array.Empty<MelonPreferences_Entry>();
		}

		internal static bool IsHotkey(MelonPreferences_Entry entry)
		{
			if (entry != null)
			{
				if (!(entry.GetReflectedType() == typeof(KeyCode)))
				{
					if (entry.GetReflectedType() == typeof(string))
					{
						if (!entry.Identifier.Contains("hotkey", StringComparison.OrdinalIgnoreCase) && !entry.Identifier.Contains("shortcut", StringComparison.OrdinalIgnoreCase))
						{
							return entry.Identifier.Contains("keybind", StringComparison.OrdinalIgnoreCase);
						}
						return true;
					}
					return false;
				}
				return true;
			}
			return false;
		}

		internal static string Value(MelonPreferences_Entry entry)
		{
			if (!Pending.TryGetValue(Key(entry), out var value))
			{
				return Format(entry.BoxedValue);
			}
			return value;
		}

		internal static bool Stage(MelonPreferences_Entry entry, string text, out string error)
		{
			error = string.Empty;
			if (!IsSupported(entry))
			{
				error = "Unsupported preference";
				return false;
			}
			if (string.Equals(text, Format(entry.BoxedValue), StringComparison.Ordinal))
			{
				Pending.Remove(Key(entry));
			}
			else
			{
				Pending[Key(entry)] = text;
			}
			object value;
			return TryParse(entry, text, out value, out error);
		}

		internal static void Revert()
		{
			Pending.Clear();
		}

		internal static bool Apply(out string message)
		{
			List<(MelonPreferences_Entry, object)> list = new List<(MelonPreferences_Entry, object)>();
			foreach (MelonPreferences_Category item in Categories())
			{
				foreach (MelonPreferences_Entry item2 in Entries(item))
				{
					if (Pending.TryGetValue(Key(item2), out var value))
					{
						if (!TryParse(item2, value, out var value2, out message))
						{
							message = item.DisplayName + " / " + item2.DisplayName + ": " + message;
							return false;
						}
						list.Add((item2, value2));
					}
				}
			}
			if (list.Count != Pending.Count)
			{
				message = "Registered preferences changed; refresh and try again.";
				return false;
			}
			if (list.Count == 0)
			{
				message = "No pending preferences.";
				return true;
			}
			(MelonPreferences_Entry, object)[] array = list.Select(((MelonPreferences_Entry Entry, object Value) change) => (Entry: change.Entry, Value: change.Entry.BoxedValue)).ToArray();
			try
			{
				string text = Path.Combine(MelonEnvironment.UserDataDirectory, "MelonPreferences.cfg");
				if (File.Exists(text))
				{
					string destFileName = text + ".xiaowo-backup-" + DateTime.UtcNow.ToString("yyyyMMddHHmmssfff", CultureInfo.InvariantCulture);
					File.Copy(text, destFileName);
				}
				foreach (var item3 in list)
				{
					item3.Item1.BoxedEditedValue = item3.Item2;
				}
				foreach (MelonPreferences_Category item4 in list.Select(((MelonPreferences_Entry Entry, object Value) change) => change.Entry.Category).Distinct())
				{
					item4.SaveToFile(false);
				}
				Pending.Clear();
				message = "Saved " + list.Count + " preference(s). Some mods require a restart.";
				return true;
			}
			catch (Exception ex)
			{
				(MelonPreferences_Entry, object)[] array2 = array;
				for (int num = 0; num < array2.Length; num++)
				{
					(MelonPreferences_Entry, object) tuple = array2[num];
					tuple.Item1.BoxedEditedValue = tuple.Item2;
				}
				message = ex.GetType().Name + ": " + ex.Message;
				return false;
			}
		}

		internal static IReadOnlyList<string> Conflicts()
		{
			return (from @group in Categories().SelectMany(Entries).Where(IsHotkey).GroupBy((MelonPreferences_Entry entry) => NormalizeHotkey(Value(entry)), StringComparer.OrdinalIgnoreCase)
				where @group.Key.Length > 0 && @group.Count() > 1
				select @group.Key + ": " + string.Join(", ", @group.Select((MelonPreferences_Entry entry) => entry.Category.DisplayName + "/" + entry.DisplayName))).ToArray();
		}

		internal static string NormalizeHotkey(string text)
		{
			if (string.IsNullOrWhiteSpace(text))
			{
				return string.Empty;
			}
			string[] array = text.Replace(" ", "").Split('+', StringSplitOptions.RemoveEmptyEntries);
			string text2 = ((array[^1].Equals("`", StringComparison.Ordinal) || array[^1] == "·") ? "BackQuote" : array[^1]);
			bool flag = array.Take(array.Length - 1).Any((string part) => part.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || part.Equals("Control", StringComparison.OrdinalIgnoreCase));
			bool flag2 = array.Take(array.Length - 1).Any((string part) => part.Equals("Shift", StringComparison.OrdinalIgnoreCase));
			return string.Concat(str2: array.Take(array.Length - 1).Any((string part) => part.Equals("Alt", StringComparison.OrdinalIgnoreCase)) ? "Alt+" : "", str0: flag ? "Ctrl+" : "", str1: flag2 ? "Shift+" : "", str3: text2);
		}

		private static bool IsSupported(MelonPreferences_Entry entry)
		{
			if (entry == null || entry.IsHidden)
			{
				return false;
			}
			Type reflectedType = entry.GetReflectedType();
			if (!(reflectedType == typeof(bool)) && !(reflectedType == typeof(int)) && !(reflectedType == typeof(float)) && !(reflectedType == typeof(double)))
			{
				return IsHotkey(entry);
			}
			return true;
		}

		private static bool TryParse(MelonPreferences_Entry entry, string text, out object value, out string error)
		{
			//IL_00fb: Unknown result type (might be due to invalid IL or missing references)
			//IL_0105: Unknown result type (might be due to invalid IL or missing references)
			value = null;
			error = string.Empty;
			Type reflectedType = entry.GetReflectedType();
			int result2;
			float result3;
			double result4;
			KeyCode result5;
			if (reflectedType == typeof(bool) && bool.TryParse(text, out var result))
			{
				value = result;
			}
			else if (reflectedType == typeof(int) && int.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out result2))
			{
				value = result2;
			}
			else if (reflectedType == typeof(float) && float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result3) && float.IsFinite(result3))
			{
				value = result3;
			}
			else if (reflectedType == typeof(double) && double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out result4) && double.IsFinite(result4))
			{
				value = result4;
			}
			else if (reflectedType == typeof(KeyCode) && Enum.TryParse<KeyCode>(text, true, out result5) && Enum.IsDefined<KeyCode>(result5))
			{
				value = result5;
			}
			else if (reflectedType == typeof(string) && IsHotkey(entry) && ValidCombo(text))
			{
				value = NormalizeHotkey(text);
			}
			if (value == null)
			{
				error = "Invalid value";
				return false;
			}
			if (entry.Validator != null && !entry.Validator.IsValid(value))
			{
				error = "Value is outside this mod's allowed range";
				return false;
			}
			return true;
		}

		private static bool ValidCombo(string text)
		{
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			if (string.IsNullOrWhiteSpace(text))
			{
				return false;
			}
			string[] array = text.Replace(" ", "").Split('+', StringSplitOptions.RemoveEmptyEntries);
			int num = array.Length;
			if ((num < 1 || num > 4) ? true : false)
			{
				return false;
			}
			for (int i = 0; i < array.Length - 1; i++)
			{
				if (!new string[4] { "Ctrl", "Control", "Shift", "Alt" }.Contains(array[i], StringComparer.OrdinalIgnoreCase))
				{
					return false;
				}
			}
			if (!(array[^1] == "`") && !(array[^1] == "·"))
			{
				if (Enum.TryParse<KeyCode>(array[^1], true, out KeyCode result))
				{
					return Enum.IsDefined<KeyCode>(result);
				}
				return false;
			}
			return true;
		}

		private static string Format(object value)
		{
			string text;
			if (!(value is IFormattable formattable))
			{
				text = value?.ToString();
				if (text == null)
				{
					return string.Empty;
				}
			}
			else
			{
				text = formattable.ToString(null, CultureInfo.InvariantCulture);
			}
			return text;
		}

		private static string Key(MelonPreferences_Entry entry)
		{
			return entry.Category.Identifier + "\0" + entry.Identifier;
		}
	}
	internal static class ModPreferenceTranslations
	{
		private static readonly Dictionary<string, string> Categories = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["XIAOWOTradePerks_ModManager"] = "MOD 管理",
			["XIAOWORatColony"] = "小窝鼠群",
			["General"] = "常规",
			["Settings"] = "设置",
			["Gameplay"] = "玩法",
			["Graphics"] = "画面",
			["Video"] = "视频",
			["Audio"] = "音频",
			["Sound"] = "声音",
			["Controls"] = "操作",
			["Input"] = "输入",
			["Hotkeys"] = "快捷键",
			["Keybinds"] = "按键绑定",
			["Performance"] = "性能",
			["Accessibility"] = "辅助功能",
			["UI"] = "界面",
			["Interface"] = "界面",
			["Notifications"] = "通知",
			["Debug"] = "调试",
			["Economy"] = "经济",
			["Inventory"] = "物品栏",
			["Saving"] = "存档",
			["Save"] = "存档",
			["Network"] = "网络",
			["Items"] = "物品",
			["NPC"] = "NPC",
			["Difficulty"] = "难度"
		};

		private static readonly Dictionary<string, string> ScopedEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			[Key("XIAOWOTradePerks_ModManager", "ToggleHotkey")] = "切换管理窗口的快捷键",
			[Key("XIAOWORatColony", "TargetGrowth")] = "成长 S 级目标",
			[Key("XIAOWORatColony", "TargetHealth")] = "生命 S 级目标",
			[Key("XIAOWORatColony", "TargetHunger")] = "饥饿 S 级目标",
			[Key("XIAOWORatColony", "TargetImmunity")] = "免疫 S 级目标",
			[Key("XIAOWORatColony", "TargetLitter")] = "产仔 S 级目标",
			[Key("XIAOWORatColony", "TargetLongevity")] = "寿命 S 级目标"
		};

		private static readonly Dictionary<string, string> ScopedDescriptions = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase) { [Key("XIAOWOTradePerks_ModManager", "ToggleHotkey")] = "Tab 上方的物理按键；也支持 Ctrl+Shift+M 等组合键。" };

		private static readonly Dictionary<string, string> CommonEntries = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Enabled"] = "启用",
			["Enable"] = "启用",
			["IsEnabled"] = "是否启用",
			["Active"] = "激活",
			["Disabled"] = "禁用",
			["ToggleHotkey"] = "切换快捷键",
			["OpenHotkey"] = "打开快捷键",
			["CloseHotkey"] = "关闭快捷键",
			["MenuHotkey"] = "菜单快捷键",
			["ToggleKey"] = "切换按键",
			["OpenKey"] = "打开按键",
			["Shortcut"] = "快捷键",
			["Keybind"] = "按键绑定",
			["Language"] = "语言",
			["Locale"] = "语言代码",
			["FontSize"] = "字体大小",
			["UIScale"] = "界面缩放",
			["WindowScale"] = "窗口缩放",
			["Opacity"] = "不透明度",
			["Transparency"] = "透明度",
			["PositionX"] = "横向位置",
			["PositionY"] = "纵向位置",
			["Width"] = "宽度",
			["Height"] = "高度",
			["ShowFPS"] = "显示帧率",
			["ShowNotifications"] = "显示通知",
			["ShowTooltips"] = "显示提示",
			["ShowOverlay"] = "显示悬浮层",
			["ShowDebugInfo"] = "显示调试信息",
			["DebugMode"] = "调试模式",
			["DebugLogging"] = "调试日志",
			["VerboseLogging"] = "详细日志",
			["LogLevel"] = "日志级别",
			["AutoSave"] = "自动保存",
			["AutoBackup"] = "自动备份",
			["BackupCount"] = "备份数量",
			["SaveInterval"] = "保存间隔",
			["SaveIntervalSeconds"] = "保存间隔（秒）",
			["Volume"] = "音量",
			["MasterVolume"] = "主音量",
			["MusicVolume"] = "音乐音量",
			["SoundVolume"] = "音效音量",
			["SFXVolume"] = "音效音量",
			["Mute"] = "静音",
			["Fullscreen"] = "全屏",
			["VSync"] = "垂直同步",
			["FrameRateLimit"] = "帧率上限",
			["TargetFPS"] = "目标帧率",
			["SpawnChance"] = "出现概率",
			["SpawnRate"] = "出现频率",
			["DropChance"] = "掉落概率",
			["DropRate"] = "掉落率",
			["PriceMultiplier"] = "价格倍率",
			["CostMultiplier"] = "费用倍率",
			["SpeedMultiplier"] = "速度倍率",
			["DamageMultiplier"] = "伤害倍率",
			["ExperienceMultiplier"] = "经验倍率",
			["XpMultiplier"] = "经验倍率",
			["RewardMultiplier"] = "奖励倍率",
			["ProfitMultiplier"] = "利润倍率",
			["CooldownSeconds"] = "冷却时间（秒）",
			["IntervalSeconds"] = "间隔（秒）",
			["MinItems"] = "最少物品数",
			["MaxItems"] = "最多物品数",
			["MaxStackSize"] = "最大堆叠数量",
			["InventorySize"] = "物品栏容量",
			["MaxClients"] = "最多顾客数",
			["MaxCustomers"] = "最多顾客数",
			["StartingMoney"] = "初始资金",
			["StartingCash"] = "初始现金",
			["Difficulty"] = "难度",
			["Chance"] = "概率",
			["Probability"] = "概率",
			["Multiplier"] = "倍率",
			["Amount"] = "数量",
			["Count"] = "数量",
			["Duration"] = "持续时间",
			["Interval"] = "间隔",
			["Cooldown"] = "冷却时间",
			["MaxRetries"] = "最大重试次数",
			["RetryCount"] = "重试次数"
		};

		private static readonly Dictionary<string, string> Words = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
		{
			["Enable"] = "启用",
			["Disable"] = "禁用",
			["Enabled"] = "启用",
			["Show"] = "显示",
			["Hide"] = "隐藏",
			["Use"] = "使用",
			["Allow"] = "允许",
			["Auto"] = "自动",
			["Save"] = "保存",
			["Backup"] = "备份",
			["Debug"] = "调试",
			["Log"] = "日志",
			["Logging"] = "日志",
			["Notification"] = "通知",
			["Notifications"] = "通知",
			["Tooltip"] = "提示",
			["Tooltips"] = "提示",
			["Overlay"] = "悬浮层",
			["Menu"] = "菜单",
			["Window"] = "窗口",
			["UI"] = "界面",
			["Font"] = "字体",
			["Hotkey"] = "快捷键",
			["Shortcut"] = "快捷键",
			["Key"] = "按键",
			["Keybind"] = "按键绑定",
			["Toggle"] = "切换",
			["Open"] = "打开",
			["Close"] = "关闭",
			["Size"] = "大小",
			["Scale"] = "缩放",
			["Opacity"] = "不透明度",
			["Width"] = "宽度",
			["Height"] = "高度",
			["Position"] = "位置",
			["X"] = "X",
			["Y"] = "Y",
			["Max"] = "最大",
			["Min"] = "最小",
			["Minimum"] = "最小",
			["Maximum"] = "最大",
			["Starting"] = "初始",
			["Default"] = "默认",
			["Client"] = "顾客",
			["Clients"] = "顾客数",
			["Customer"] = "顾客",
			["Customers"] = "顾客数",
			["Item"] = "物品",
			["Items"] = "物品数",
			["Inventory"] = "物品栏",
			["Stack"] = "堆叠",
			["Slot"] = "格子",
			["Slots"] = "格子数",
			["Count"] = "数量",
			["Amount"] = "数量",
			["Money"] = "资金",
			["Cash"] = "现金",
			["Price"] = "价格",
			["Cost"] = "费用",
			["Profit"] = "利润",
			["Reward"] = "奖励",
			["Experience"] = "经验",
			["XP"] = "经验",
			["Damage"] = "伤害",
			["Speed"] = "速度",
			["Multiplier"] = "倍率",
			["Rate"] = "比例",
			["Chance"] = "概率",
			["Probability"] = "概率",
			["Spawn"] = "出现",
			["Drop"] = "掉落",
			["Cooldown"] = "冷却时间",
			["Duration"] = "持续时间",
			["Interval"] = "间隔",
			["Seconds"] = "（秒）",
			["Minutes"] = "（分钟）",
			["Hours"] = "（小时）",
			["Days"] = "（天）",
			["FPS"] = "帧率",
			["Volume"] = "音量",
			["Music"] = "音乐",
			["Sound"] = "音效",
			["SFX"] = "音效",
			["Master"] = "主",
			["Mute"] = "静音",
			["Power"] = "电力",
			["Energy"] = "能量",
			["Health"] = "生命",
			["Hunger"] = "饥饿",
			["Growth"] = "成长",
			["Litter"] = "产仔",
			["Immunity"] = "免疫",
			["Longevity"] = "寿命",
			["Quality"] = "品质",
			["Level"] = "等级",
			["Limit"] = "上限"
		};

		private static readonly Regex IdentifierWords = new Regex("[A-Z]+(?![a-z])|[A-Z]?[a-z]+|[0-9]+", RegexOptions.Compiled | RegexOptions.CultureInvariant);

		internal static string Category(string locale, string categoryId, string original)
		{
			if (!ModLocalization.IsChinese(locale) || !Categories.TryGetValue(categoryId ?? "", out var value))
			{
				return original;
			}
			return value;
		}

		internal static string Entry(string locale, string categoryId, string entryId, string original)
		{
			if (!ModLocalization.IsChinese(locale))
			{
				return original;
			}
			if (ScopedEntries.TryGetValue(Key(categoryId, entryId), out var value))
			{
				return value;
			}
			if (CommonEntries.TryGetValue(entryId ?? "", out var value2))
			{
				return value2;
			}
			if (!TryCompose(entryId, out var translation))
			{
				return original;
			}
			return translation;
		}

		internal static string Description(string locale, string categoryId, string entryId, string original)
		{
			if (!ModLocalization.IsChinese(locale) || !ScopedDescriptions.TryGetValue(Key(categoryId, entryId), out var value))
			{
				return original;
			}
			return value;
		}

		private static string Key(string categoryId, string entryId)
		{
			return categoryId + "\0" + entryId;
		}

		private static bool TryCompose(string identifier, out string translation)
		{
			translation = string.Empty;
			if (string.IsNullOrWhiteSpace(identifier) || identifier.Length > 80)
			{
				return false;
			}
			string text = identifier.Replace('_', ' ').Replace('-', ' ');
			MatchCollection matchCollection = IdentifierWords.Matches(text);
			if (matchCollection.Count < 2)
			{
				return false;
			}
			List<string> list = new List<string>(matchCollection.Count);
			int num = 0;
			foreach (Match item in matchCollection)
			{
				if (text.Substring(num, item.Index - num).Trim().Length != 0 || !Words.TryGetValue(item.Value, out var value))
				{
					return false;
				}
				list.Add(value);
				num = item.Index + item.Length;
			}
			if (text.Substring(num).Trim().Length != 0)
			{
				return false;
			}
			translation = string.Concat(list);
			return true;
		}
	}
	internal static class OwnerDeal
	{
		internal const string ClientId = "landlord";

		internal const string MerchantClientId = "merchant";

		internal const string RestrictedNeuralId = "system_capped_neural_core";

		internal const string EvidenceBoxId = "evidence_box";

		internal const string SecurityBoxId = "sec_box";

		internal const string MedicalBoxId = "med_box";

		internal const string EngineeringBoxId = "eng_box";

		internal const string ServiceBoxId = "service_box";

		internal const int DiscountPercent = 15;

		private const string VisitStatePrefix = "XIAOWOTradePerks.OwnerDeal.PairV3.";

		private const string ExcludedUncappedNeuralId = "system_uncapped_neural_core";

		private static readonly List<string> RegularPool = new List<string>();

		private static string _lastScheduledKey = string.Empty;

		private static bool _poolBuilt;

		private static long _merchantClientPointer;

		private static long _ownerClientPointer;

		private static readonly HashSet<long> CashBuyoutChoiceDialogues = new HashSet<long>();

		private static readonly List<Action> CashBuyoutActions = new List<Action>();

		internal static bool IsEnabledAndOwned(PlayerStore store)
		{
			if (store != null && store.IsPropertyPaid)
			{
				return PerkRegistry.IsActive("xiaowo_trade_owner_deal");
			}
			return false;
		}

		internal static bool ShouldSuppressEnding(PlayerStore store, string ending)
		{
			if (IsEnabledAndOwned(store))
			{
				return string.Equals(ending, "buyout", StringComparison.Ordinal);
			}
			return false;
		}

		internal static bool CanOfferCashBuyout(PlayerStore store)
		{
			if (store == null)
			{
				return false;
			}
			return OwnerDealRules.CanOfferCashBuyout(PerkRegistry.IsActive("xiaowo_trade_owner_deal"), store.IsPropertyPaid, store.IsPayingRent(), store.GetCash(), store.GetStoreBuyPrice(false));
		}

		internal static void ScheduleWeeklyVisitors(StoreClientManager manager)
		{
			if (manager == null)
			{
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (!IsEnabledAndOwned(instance) || StoreStation.instance == null)
			{
				return;
			}
			int currentDay = DeterministicSchedule.CurrentDay;
			if (!OwnerDealRules.IsFormerRentDay(currentDay))
			{
				return;
			}
			string text = (string.IsNullOrEmpty(instance.runID) ? "no-run" : instance.runID);
			string text2 = text + "|" + currentDay;
			if (string.Equals(_lastScheduledKey, text2, StringComparison.Ordinal))
			{
				return;
			}
			int num = OwnerDealRules.DecodeVisitState(PlayerPrefs.GetInt("XIAOWOTradePerks.OwnerDeal.PairV3." + text, -1), currentDay);
			if (num == 3)
			{
				_lastScheduledKey = text2;
				return;
			}
			bool flag = IsClientPresent(instance, manager, "merchant");
			bool flag2 = IsClientPresent(instance, manager, "landlord");
			bool flag3 = false;
			bool flag4 = false;
			try
			{
				if (OwnerDealRules.NeedsVisitor(num, 1, flag))
				{
					StoreClient val = CreateMerchantClient();
					if (val != null)
					{
						manager.TryAddClient(val, true);
						flag3 = ContainsClient(manager.clientStack, "merchant");
					}
				}
				if (OwnerDealRules.NeedsVisitor(num, 2, flag2))
				{
					StoreClient val2 = CreateLandlordClient();
					if (val2 != null)
					{
						manager.TryAddClient(val2, true);
						flag4 = ContainsClient(manager.clientStack, "landlord");
					}
				}
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("Owner deal weekly visitor scheduling failed: " + ex);
				}
				return;
			}
			flag = (flag | flag3) || IsClientPresent(instance, manager, "merchant");
			flag2 = (flag2 | flag4) || IsClientPresent(instance, manager, "landlord");
			bool num2 = ((num & 1) != 0) | flag;
			bool flag5 = ((num & 2) != 0) | flag2;
			if (!num2 || !flag5)
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Warning("Owner deal could not schedule both weekly visitors on day " + currentDay + "; it will retry.");
				}
				return;
			}
			_lastScheduledKey = text2;
			Instance log3 = Core.Log;
			if (log3 != null)
			{
				log3.Msg("Owner deal weekly visitors scheduled on former rent day " + currentDay + " (merchant=" + flag3 + ", landlord=" + flag4 + ", completedMask=" + num + ").");
			}
		}

		internal static StoreClient CreateMerchantClient()
		{
			StoreClient val = StoreClientList.CreateMerchant();
			_merchantClientPointer = ((val != null) ? ((Il2CppObjectBase)val).Pointer.ToInt64() : 0);
			return val;
		}

		internal static StoreClient CreateLandlordClient()
		{
			StoreClient val = StoreClientList.CreateLandlordClient();
			StoreClient val2 = StoreClientUniqueList.LandlordWholesale();
			if (val == null || ((val2 != null) ? val2.mainDialogue : null) == null)
			{
				return null;
			}
			val.mainDialogue = val2.mainDialogue;
			ApplyPostBuyoutVendorState(val);
			ApplyDialogue(val);
			_ownerClientPointer = ((Il2CppObjectBase)val).Pointer.ToInt64();
			return val;
		}

		internal static void ApplyPostBuyoutVendorState(StoreClient client)
		{
			if (client != null)
			{
				client.clientIntent = (ClientIntent)2;
				client.dismissable = true;
				client.arrestable = false;
				client.CanReturn = false;
				client.CanReturnAsBuyer = false;
				client.SetWholesale(15);
			}
		}

		internal static IReadOnlyList<string> BuildStockPlan(string runId, int day)
		{
			EnsureRegularPool();
			Random random = new Random(OwnerDealRules.GetVisitSeed(runId, day));
			OwnerStockRoll ownerStockRoll = OwnerDealRules.Roll(random);
			List<string> list = new List<string>(ownerStockRoll.Count);
			if (ownerStockRoll.RestrictedNeural)
			{
				list.Add("system_capped_neural_core");
			}
			int num = Math.Max(ownerStockRoll.Count, list.Count);
			if (RegularPool.Count == 0)
			{
				return list;
			}
			List<string> list2 = new List<string>(RegularPool);
			while (list.Count < num && list2.Count > 0)
			{
				int index = random.Next(list2.Count);
				list.Add(list2[index]);
				list2.RemoveAt(index);
			}
			return list;
		}

		internal static bool IsEligibleRegularItem(GameItem item)
		{
			if (item != null && item.unitValue > 0)
			{
				if (!item.IsTag("MODULE_TAG") && !item.IsTag("CONTRABAND_ITEM_TAG"))
				{
					return item.IsTag("LUXURY_ITEM");
				}
				return true;
			}
			return false;
		}

		internal static bool IsCurrentOwnerVisit(PlayerStore store)
		{
			object obj;
			if (store == null)
			{
				obj = null;
			}
			else
			{
				StoreClientInstance currentClientInstance = store.currentClientInstance;
				obj = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
			}
			StoreClient val = (StoreClient)obj;
			if (IsEnabledAndOwned(store) && val != null && string.Equals(val.identifier, "landlord", StringComparison.Ordinal))
			{
				if (_ownerClientPointer == 0L || ((Il2CppObjectBase)val).Pointer.ToInt64() != _ownerClientPointer)
				{
					return IsRestoredOwnerVendor(val);
				}
				return true;
			}
			return false;
		}

		internal static bool IsRestoredOwnerVendor(StoreClient client)
		{
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Invalid comparison between Unknown and I4
			if (client != null && string.Equals(client.identifier, "landlord", StringComparison.Ordinal) && (int)client.clientIntent == 2 && client.dismissable)
			{
				return client.isOfferWholesale;
			}
			return false;
		}

		internal static void OnGameLoading()
		{
			_lastScheduledKey = string.Empty;
			_merchantClientPointer = 0L;
			_ownerClientPointer = 0L;
			CashBuyoutChoiceDialogues.Clear();
			CashBuyoutActions.Clear();
		}

		internal static void AttachCashBuyoutChoice(StoreClient client)
		{
			if (!OwnerDealCompatibility.UsesCustomCashBuyoutChoice)
			{
				return;
			}
			StoreClient val = client;
			if (((val != null) ? val.mainDialogue : null) == null)
			{
				return;
			}
			Dialogue mainDialogue = client.mainDialogue;
			Action nativeStartAction = mainDialogue.startAction;
			Action val2 = DelegateSupport.ConvertDelegate<Action>((Delegate)(Action)(() =>
			{
				Action val3 = nativeStartAction;
				if (val3 != null)
				{
					val3.Invoke();
				}
				TryAddCashBuyoutChoice(client);
			}));
			CashBuyoutActions.Add(val2);
			mainDialogue.SetStartAction(val2);
		}

		internal static bool TryAddCashBuyoutChoice(StoreClient client)
		{
			//IL_005c: Unknown result type (might be due to invalid IL or missing references)
			PlayerStore store = PlayerStore.Instance;
			Dialogue val = ((client != null) ? client.mainDialogue : null);
			if (val == null || !CanOfferCashBuyout(store))
			{
				return false;
			}
			long item = ((Il2CppObjectBase)val).Pointer.ToInt64();
			if (!CashBuyoutChoiceDialogues.Add(item))
			{
				return false;
			}
			int storeBuyPrice = store.GetStoreBuyPrice(false);
			Dialogue val2 = new Dialogue().SetText("PLAYER", ModLocalization.Select("我用 " + storeBuyPrice + " 信用点一次买断这间店。", "I will buy the shop outright for " + storeBuyPrice + " Credits."));
			Dialogue val3 = val2.NextDialogue().SetText(client.displayName, ModLocalization.Select("成交。从现在起，这间店归你了。", "Deal. From now on, the shop is yours."));
			Action val4 = DelegateSupport.ConvertDelegate<Action>((Delegate)(Action)(() =>
			{
				DialogUIManager instance = DialogUIManager.Instance;
				if (instance != null)
				{
					instance.CurrentClientLeave(1f);
				}
			}));
			CashBuyoutActions.Add(val4);
			val3.SetEndAction(val4);
			Action val5 = DelegateSupport.ConvertDelegate<Action>((Delegate)(Action)(() =>
			{
				CompleteCashBuyout(store);
			}));
			CashBuyoutActions.Add(val5);
			OwnerDealCompatibility.AddChoice(val, DirectoryMaster.Item("$", true), "xiaowo cash buyout choice", string.Empty, val2, val5);
			return true;
		}

		internal static bool CompleteCashBuyout(PlayerStore store)
		{
			if (!CanOfferCashBuyout(store))
			{
				return false;
			}
			int storeBuyPrice = store.GetStoreBuyPrice(false);
			store.ModCash(-storeBuyPrice, true);
			store.propertyBoughtAt = storeBuyPrice;
			store.mortgageWeekLeft = 0;
			store.mortgageWeeklyPayment = 0;
			store.isPayingMortgage = false;
			store.IsPropertyPaid = true;
			store.OnStoreOwned();
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Owner deal cash buyout completed for " + storeBuyPrice + " Credits; the run continues.");
			}
			return true;
		}

		internal static int CaptureCurrentVisitor(PlayerStore store)
		{
			if (!IsEnabledAndOwned(store) || !OwnerDealRules.IsFormerRentDay(DeterministicSchedule.CurrentDay))
			{
				return 0;
			}
			StoreClientInstance currentClientInstance = store.currentClientInstance;
			StoreClient val = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
			if (val == null)
			{
				return 0;
			}
			long num = ((Il2CppObjectBase)val).Pointer.ToInt64();
			if (string.Equals(val.identifier, "merchant", StringComparison.Ordinal) && (_merchantClientPointer == 0L || num == _merchantClientPointer))
			{
				return 1;
			}
			if (string.Equals(val.identifier, "landlord", StringComparison.Ordinal) && (_ownerClientPointer == 0L || num == _ownerClientPointer))
			{
				return 2;
			}
			return 0;
		}

		internal static void MarkVisitorCompleted(int visitorBit)
		{
			if (visitorBit == 0)
			{
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			int currentDay = DeterministicSchedule.CurrentDay;
			if (!IsEnabledAndOwned(instance) || !OwnerDealRules.IsFormerRentDay(currentDay))
			{
				return;
			}
			string text = (string.IsNullOrEmpty(instance.runID) ? "no-run" : instance.runID);
			string text2 = "XIAOWOTradePerks.OwnerDeal.PairV3." + text;
			int num = OwnerDealRules.DecodeVisitState(PlayerPrefs.GetInt(text2, -1), currentDay);
			int num2 = num | visitorBit;
			if (num2 != num)
			{
				PlayerPrefs.SetInt(text2, OwnerDealRules.EncodeVisitState(currentDay, num2));
				PlayerPrefs.Save();
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Owner deal visitor completed on day " + currentDay + " (mask=" + num2 + ").");
				}
			}
		}

		internal static bool TrySpawnStock(PlayerStore store)
		{
			if (!IsCurrentOwnerVisit(store))
			{
				return false;
			}
			IReadOnlyList<string> readOnlyList = BuildStockPlan(store.runID, DeterministicSchedule.CurrentDay);
			int num = 0;
			foreach (string item in readOnlyList)
			{
				try
				{
					GameItem val = CreateSaleItem(item);
					if (val != null)
					{
						store.AddDirectSellingItemToTable(val, false, false, false, 0);
						num++;
					}
				}
				catch (Exception ex)
				{
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("Owner stock item failed (" + item + "): " + ex.Message);
					}
				}
			}
			Instance log2 = Core.Log;
			if (log2 != null)
			{
				log2.Msg("Owner deal stocked " + num + "/" + readOnlyList.Count + " items.");
			}
			return num > 0;
		}

		internal static GameItem CreateSaleItem(string id)
		{
			return id switch
			{
				"evidence_box" => PreBuiltItemHelper.LootCrateEvidence(), 
				"sec_box" => PreBuiltItemHelper.LootCrateSecurity(), 
				"med_box" => PreBuiltItemHelper.LootCrateMedical(), 
				"eng_box" => PreBuiltItemHelper.LootCrateEngineering(), 
				"service_box" => PreBuiltItemHelper.LootCrateService(), 
				_ => DirectoryMaster.Item(id, false), 
			};
		}

		internal static int GetRegularPoolCount()
		{
			EnsureRegularPool();
			return RegularPool.Count;
		}

		internal static bool RegularPoolContains(string id)
		{
			EnsureRegularPool();
			return RegularPool.Contains(id);
		}

		private static void EnsureRegularPool()
		{
			if (_poolBuilt)
			{
				return;
			}
			_poolBuilt = true;
			RegularPool.Clear();
			List<string> identifierList = DirectoryMaster.GetIdentifierList<GameItem>((string)null);
			if (identifierList == null)
			{
				return;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			for (int i = 0; i < identifierList.Count; i++)
			{
				string text = identifierList[i];
				if (string.IsNullOrEmpty(text) || string.Equals(text, "system_capped_neural_core", StringComparison.Ordinal) || string.Equals(text, "system_uncapped_neural_core", StringComparison.Ordinal) || !hashSet.Add(text))
				{
					continue;
				}
				try
				{
					if (IsEligibleRegularItem(DirectoryMaster.Item(text, false)))
					{
						RegularPool.Add(text);
					}
				}
				catch (Exception ex)
				{
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("Owner stock scan skipped " + text + ": " + ex.Message);
					}
				}
			}
			Instance log2 = Core.Log;
			if (log2 != null)
			{
				log2.Msg("Owner deal regular stock pool: " + RegularPool.Count + " items.");
			}
		}

		private static void ApplyDialogue(StoreClient client)
		{
			string[] array = new string[4]
			{
				ModLocalization.Select("这间店已经归你，今天我不是来收租的。", "The shop is yours now. I am not here to collect rent today."),
				ModLocalization.Select("我从旧渠道里挑了几件货：模组、违禁品，或者真正值钱的奢侈品。", "I picked up a few things through old contacts: modules, contraband, and some genuinely valuable luxuries."),
				ModLocalization.Select("东西来路我不多问，你也别问。看中了就按老熟人的价格拿走。", "I do not ask where they came from, and neither should you. Take what you want at the old-friend price."),
				ModLocalization.Select("货就在桌上。下个租金日，我再看看能替你弄到什么。", "The goods are on the table. On the next old rent day, I will see what else I can find for you.")
			};
			Dialogue val = client.mainDialogue;
			for (int i = 0; i < array.Length; i++)
			{
				if (val == null)
				{
					break;
				}
				val.SetText(client.displayName, array[i]);
				val = val.nextDialogue;
			}
		}

		internal static void RefreshLocalizedDialogue(StoreClientManager manager)
		{
			if (manager == null)
			{
				return;
			}
			PlayerStore instance = PlayerStore.instance;
			object obj;
			if (instance == null)
			{
				obj = null;
			}
			else
			{
				StoreClientInstance currentClientInstance = instance.currentClientInstance;
				obj = ((currentClientInstance != null) ? currentClientInstance.storeClient : null);
			}
			StoreClient val = (StoreClient)obj;
			if (string.Equals((val != null) ? val.identifier : null, "landlord", StringComparison.Ordinal))
			{
				ApplyDialogue(val);
			}
			List<StoreClient> clientStack = manager.clientStack;
			if (clientStack == null)
			{
				return;
			}
			for (int i = 0; i < clientStack.Count; i++)
			{
				StoreClient val2 = clientStack[i];
				if (string.Equals((val2 != null) ? val2.identifier : null, "landlord", StringComparison.Ordinal))
				{
					ApplyDialogue(clientStack[i]);
				}
			}
		}

		private static bool ContainsClient(List<StoreClient> clients, string id)
		{
			if (clients == null)
			{
				return false;
			}
			for (int i = 0; i < clients.Count; i++)
			{
				if (clients[i] != null && string.Equals(clients[i].identifier, id, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsClientPresent(PlayerStore store, StoreClientManager manager, string id)
		{
			object a;
			if (store == null)
			{
				a = null;
			}
			else
			{
				StoreClientInstance currentClientInstance = store.currentClientInstance;
				if (currentClientInstance == null)
				{
					a = null;
				}
				else
				{
					StoreClient storeClient = currentClientInstance.storeClient;
					a = ((storeClient != null) ? storeClient.identifier : null);
				}
			}
			if (!string.Equals((string?)a, id, StringComparison.Ordinal))
			{
				return ContainsClient((manager != null) ? manager.clientStack : null, id);
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class OwnerDealLoadResetPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix()
		{
			OwnerDeal.OnGameLoading();
		}

		[HarmonyPriority(0)]
		private static void Postfix(PlayerStore __instance)
		{
			OwnerDeal.ScheduleWeeklyVisitors((__instance != null) ? __instance.storeClientManager : null);
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "DismissCurrentClient")]
	internal static class OwnerDealVisitorCompletionPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix(PlayerStore __instance, out int __state)
		{
			__state = OwnerDeal.CaptureCurrentVisitor(__instance);
		}

		[HarmonyPriority(0)]
		private static void Postfix(int __state)
		{
			OwnerDeal.MarkVisitorCompleted(__state);
		}
	}
	[HarmonyPatch(typeof(StoreClientList), "CreateLandlordClient")]
	internal static class OwnerDealLandlordDialoguePatch
	{
		private static void Postfix(StoreClient __result)
		{
			OwnerDeal.AttachCashBuyoutChoice(__result);
		}
	}
	internal static class OwnerDealBuyoutChoicePatch
	{
		internal static void Postfix(PlayerStore __instance, ref bool __result)
		{
			if (!__result && OwnerDeal.CanOfferCashBuyout(__instance))
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "ExecuteGameOver")]
	internal static class OwnerDealBuyoutEndingPatch
	{
		private static bool Prefix(PlayerStore __instance, string ending)
		{
			if (!OwnerDeal.ShouldSuppressEnding(__instance, ending))
			{
				return true;
			}
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Owner deal suppressed the cash-buyout ending; the run continues.");
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "CheckRentDay")]
	internal static class OwnerDealRentDayVisitorPatch
	{
		private static void Postfix(StoreClientManager __instance)
		{
			OwnerDeal.ScheduleWeeklyVisitors(__instance);
		}
	}
	internal static class OwnerDealStockPatch
	{
		internal static bool Prefix()
		{
			PlayerStore instance = PlayerStore.Instance;
			if (!OwnerDeal.IsCurrentOwnerVisit(instance))
			{
				return true;
			}
			try
			{
				return !OwnerDeal.TrySpawnStock(instance);
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error("Owner stock generation failed; using native wholesale stock instead: " + ex);
				}
				return true;
			}
		}
	}
	internal static class OwnerDealCompatibility
	{
		private static MethodInfo _cashBuyoutConditionTarget;

		private static MethodInfo _landlordStockTarget;

		internal static MethodInfo CashBuyoutConditionTarget => _cashBuyoutConditionTarget;

		internal static MethodInfo LandlordStockTarget => _landlordStockTarget;

		internal static bool UsesCustomCashBuyoutChoice => _cashBuyoutConditionTarget == null;

		internal static Dialogue AddChoice(Dialogue dialogue, GameItem choiceItem, string name, string description, Dialogue nextDialogue, Action action)
		{
			if (dialogue == null)
			{
				return null;
			}
			MethodInfo methodInfo = (from candidate in typeof(Dialogue).GetMethods(BindingFlags.Instance | BindingFlags.Public)
				where string.Equals(candidate.Name, "AddChoice", StringComparison.Ordinal)
				orderby candidate.GetParameters().Length
				select candidate).FirstOrDefault((MethodInfo candidate) =>
			{
				int num = candidate.GetParameters().Length;
				return (uint)(num - 5) <= 1u;
			});
			if (methodInfo == null)
			{
				throw new MissingMethodException(typeof(Dialogue).FullName, "AddChoice");
			}
			object[] parameters = ((methodInfo.GetParameters().Length != 5) ? new object[6] { choiceItem, name, description, nextDialogue, action, null } : new object[5] { choiceItem, name, description, nextDialogue, action });
			object? obj = methodInfo.Invoke(dialogue, parameters);
			return (Dialogue)((obj is Dialogue) ? obj : null);
		}

		internal static void Install(Harmony harmony)
		{
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_0052: Expected Obj, but got Unknown
			//IL_0134: Unknown result type (might be due to invalid IL or missing references)
			//IL_0142: Expected Obj, but got Unknown
			_cashBuyoutConditionTarget = typeof(PlayerStore).GetMethod("CanBuyOutStore", BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
			if (_cashBuyoutConditionTarget != null)
			{
				harmony.Patch((MethodBase)_cashBuyoutConditionTarget, (HarmonyMethod)null, new HarmonyMethod(AccessTools.Method(typeof(OwnerDealBuyoutChoicePatch), "Postfix", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Owner deal cash-buyout condition patch: " + _cashBuyoutConditionTarget.Name);
				}
			}
			else
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Msg("Owner deal: native CanBuyOutStore is absent; using the current property-purchase flow.");
				}
			}
			_landlordStockTarget = (from method in typeof(__c).GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				where method.Name.StartsWith("_LandlordWholesale_b__", StringComparison.Ordinal) && method.ReturnType == typeof(void) && method.GetParameters().Length == 0
				select method).OrderBy((MethodInfo method) => method.Name, StringComparer.Ordinal).FirstOrDefault();
			if (_landlordStockTarget == null)
			{
				Instance log3 = Core.Log;
				if (log3 != null)
				{
					log3.Warning("Owner deal: landlord wholesale stock callback was not found; native stock will be used.");
				}
				return;
			}
			harmony.Patch((MethodBase)_landlordStockTarget, new HarmonyMethod(AccessTools.Method(typeof(OwnerDealStockPatch), "Prefix", (Type[])null, (Type[])null)), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			Instance log4 = Core.Log;
			if (log4 != null)
			{
				log4.Msg("Owner deal landlord stock patch: " + _landlordStockTarget.Name);
			}
		}
	}
	internal readonly struct OwnerStockRoll
	{
		internal int Count { get; }

		internal bool RestrictedNeural { get; }

		internal OwnerStockRoll(int count, bool restrictedNeural)
		{
			Count = count;
			RestrictedNeural = restrictedNeural;
		}
	}
	internal static class OwnerDealRules
	{
		internal const int VisitIntervalDays = 7;

		internal const int MerchantCompleted = 1;

		internal const int LandlordCompleted = 2;

		internal const int AllVisitorsCompleted = 3;

		internal static bool CanOfferCashBuyout(bool perkActive, bool propertyPaid, bool payingRent, int cash, int price)
		{
			if (((perkActive && !propertyPaid) & payingRent) && price > 0)
			{
				return cash >= price;
			}
			return false;
		}

		internal static bool IsFormerRentDay(int day)
		{
			if (day > 0)
			{
				return day % 7 == 0;
			}
			return false;
		}

		internal static int EncodeVisitState(int day, int completedMask)
		{
			return (Math.Max(0, day) << 2) | (completedMask & 3);
		}

		internal static int DecodeVisitState(int encoded, int day)
		{
			if (encoded >> 2 != Math.Max(0, day))
			{
				return 0;
			}
			return encoded & 3;
		}

		internal static bool NeedsVisitor(int completedMask, int visitorBit, bool present)
		{
			if ((completedMask & visitorBit) == 0)
			{
				return !present;
			}
			return false;
		}

		internal static OwnerStockRoll Roll(Random random)
		{
			bool restrictedNeural = random.Next(100) < 5;
			return new OwnerStockRoll(random.Next(3, 7), restrictedNeural);
		}

		internal static int GetVisitSeed(string runId, int day)
		{
			return StableHash((string.IsNullOrEmpty(runId) ? "no-run" : runId) + "|owner-deal|" + day);
		}

		internal static int StableHash(string value)
		{
			int num = -2128831035;
			for (int i = 0; i < value.Length; i++)
			{
				num = (num ^ value[i]) * 16777619;
			}
			return num;
		}
	}
	internal static class PatchCompatibility
	{
		private static readonly Dictionary<string, Harmony> Active = new Dictionary<string, Harmony>(StringComparer.Ordinal);

		private static readonly HashSet<string> Failed = new HashSet<string>(StringComparer.Ordinal);

		private static string _ownerPrefix = string.Empty;

		private static bool _reported;

		private static string _lastReport = string.Empty;

		internal static int FailedModuleCount => Failed.Count;

		internal static int InstalledModuleCount => Active.Count;

		internal static int PossibleConflictCount { get; private set; }

		internal static int ConfirmedConflictCount { get; private set; }

		internal static int DuplicateOwnPatchCount { get; private set; }

		internal static string LastReport => _lastReport;

		internal static bool Available(string module)
		{
			return !Failed.Contains(module);
		}

		internal static void InvalidateReport()
		{
			_reported = false;
			_lastReport = string.Empty;
		}

		internal static void Install(Harmony root)
		{
			//IL_00bd: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c3: Expected Obj, but got Unknown
			Dispose();
			_ownerPrefix = root.Id + ".feature.";
			foreach (IGrouping<string, Type> item in (from type in typeof(Core).Assembly.GetTypes()
				where type.IsDefined(typeof(HarmonyPatch), inherit: false)
				select type).GroupBy(ModuleFor).OrderBy((IGrouping<string, Type> group) => group.Key, StringComparer.Ordinal))
			{
				Harmony val = new Harmony(_ownerPrefix + item.Key);
				try
				{
					int num = 0;
					foreach (Type item2 in item.OrderBy((Type type) => type.FullName, StringComparer.Ordinal))
					{
						num += val.CreateClassProcessor(item2).Patch().Count;
					}
					if (item.Key == "owner")
					{
						OwnerDealCompatibility.Install(val);
					}
					Active.Add(item.Key, val);
					Instance log = Core.Log;
					if (log != null)
					{
						log.Msg("Patch module " + item.Key + " ready (" + num + " targets).");
					}
				}
				catch (Exception ex)
				{
					val.UnpatchSelf();
					Failed.Add(item.Key);
					Instance log2 = Core.Log;
					if (log2 != null)
					{
						log2.Error("Patch module " + item.Key + " disabled; other modules remain available: " + ex);
					}
				}
			}
		}

		internal static string ReportSharedMethods()
		{
			if (_reported)
			{
				return _lastReport;
			}
			_reported = true;
			List<string> list = new List<string>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> hashSet2 = new HashSet<string>(StringComparer.Ordinal);
			foreach (MethodBase item in Harmony.GetAllPatchedMethods().OrderBy((MethodBase method) => method.DeclaringType?.FullName, StringComparer.Ordinal).ThenBy((MethodBase method) => method.Name, StringComparer.Ordinal))
			{
				Patches patchInfo = Harmony.GetPatchInfo(item);
				if (patchInfo == null || !patchInfo.Owners.Any(IsOurs) || patchInfo.Owners.Where((string owner) => !IsOurs(owner)).Distinct().ToArray()
					.Length == 0)
				{
					continue;
				}
				string target = MethodName(item);
				(string, Patch)[] source = NamedPatches(patchInfo).ToArray();
				string text = string.Join(", ", source.Select(((string Kind, Patch Value) patch) => patch.Kind + ":" + (IsOurs(patch.Value.owner) ? "ours" : patch.Value.owner) + "/" + patch.Value.PatchMethod?.DeclaringType?.Name + "/p" + patch.Value.priority));
				list.Add(target + ": " + text);
				foreach (IGrouping<string, (string, Patch)> item2 in source.GroupBy(((string Kind, Patch Value) patch) => patch.Kind + "|" + patch.Value.owner + "|" + PatchMethodName(patch.Value.PatchMethod), StringComparer.Ordinal).Where((IGrouping<string, (string Kind, Patch Value)> group) => group.Count() > 1))
				{
					(string, Patch) tuple = item2.First();
					if (IsOurs(tuple.Item2.owner))
					{
						DuplicateOwnPatchCount++;
					}
					hashSet2.Add("目标方法 " + target + " 存在重复的" + PatchKindName(tuple.Item1) + "补丁 " + PatchMethodName(tuple.Item2.PatchMethod) + "，来源 MOD：" + OwnerName(tuple.Item2.owner) + "，共注册 " + item2.Count() + " 次");
				}
				(string, Patch)[] source2 = source.Where(((string Kind, Patch Value) patch) => !IsOurs(patch.Value.owner)).ToArray().Where(((string Kind, Patch Value) patch) => !IsKnownCompatible(target, patch))
					.ToArray();
				string text2 = OwnerNames(source2.Select(((string Kind, Patch Value) patch) => patch.Value.owner));
				if (source2.Any(((string Kind, Patch Value) patch) => patch.Kind == "transpile"))
				{
					hashSet.Add("目标方法 " + target + " 与 MOD " + text2 + " 共同使用 Transpiler");
				}
				if (source2.Any(((string Kind, Patch Value) patch) => patch.Kind == "pre" && CanSkipOriginal(patch.Value.PatchMethod)))
				{
					hashSet.Add("目标方法 " + target + " 可能被 MOD " + OwnerNames(source2.Where(((string Kind, Patch Value) patch) => patch.Kind == "pre" && CanSkipOriginal(patch.Value.PatchMethod)).Select(((string Kind, Patch Value) patch) => patch.Value.owner)) + " 的前置补丁跳过原方法");
				}
				if (source.Any(((string Kind, Patch Value) patch) => IsOurs(patch.Value.owner) && WritesResult(patch.Value.PatchMethod)) && source2.Any(((string Kind, Patch Value) patch) => WritesResult(patch.Value.PatchMethod)))
				{
					hashSet.Add("目标方法 " + target + " 的返回值也会被 MOD " + OwnerNames(source2.Where(((string Kind, Patch Value) patch) => WritesResult(patch.Value.PatchMethod)).Select(((string Kind, Patch Value) patch) => patch.Value.owner)) + " 修改");
				}
			}
			foreach (string item3 in Failed)
			{
				hashSet2.Add("主 MOD 功能模块 " + item3 + " 已停用");
			}
			PossibleConflictCount = hashSet.Count;
			ConfirmedConflictCount = hashSet2.Count;
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("MOD 兼容性检测：共享方法=" + list.Count + "，潜在冲突=" + hashSet.Count + "，确认冲突=" + hashSet2.Count + "，停用模块=" + Failed.Count + "；" + CompatibilityBroker.BuildReport() + "。");
			}
			foreach (string item4 in hashSet)
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Warning("可能存在 MOD 冲突：" + item4);
				}
			}
			foreach (string item5 in hashSet2)
			{
				Instance log3 = Core.Log;
				if (log3 != null)
				{
					log3.Error("确认存在 MOD 冲突：" + item5);
				}
			}
			Instance log4 = Core.Log;
			if (log4 != null)
			{
				log4.Msg("遗忘的小窝 QQ 群：611790510");
			}
			_lastReport = "shared=" + list.Count + ", possible=" + hashSet.Count + ", confirmed=" + hashSet2.Count + ", installed=" + Active.Count + ", disabled=" + Failed.Count + ", " + CompatibilityBroker.BuildReport() + "\n" + string.Join("\n", list) + ((hashSet.Count == 0) ? string.Empty : ("\nPOSSIBLE\n" + string.Join("\n", hashSet))) + ((hashSet2.Count == 0) ? string.Empty : ("\nCONFIRMED\n" + string.Join("\n", hashSet2)));
			return _lastReport;
		}

		internal static void Dispose()
		{
			foreach (Harmony value in Active.Values)
			{
				try
				{
					value.UnpatchSelf();
				}
				catch (Exception ex)
				{
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("Patch cleanup failed for " + value.Id + ": " + ex);
					}
				}
			}
			Active.Clear();
			Failed.Clear();
			PossibleConflictCount = 0;
			ConfirmedConflictCount = 0;
			DuplicateOwnPatchCount = 0;
			_reported = false;
			_lastReport = string.Empty;
		}

		private static bool IsOurs(string owner)
		{
			if (!string.IsNullOrEmpty(owner))
			{
				return owner.StartsWith(_ownerPrefix, StringComparison.Ordinal);
			}
			return false;
		}

		internal static bool IsOwnedByMain(string owner)
		{
			return IsOurs(owner);
		}

		private static IEnumerable<(string Kind, Patch Value)> NamedPatches(Patches info)
		{
			return info.Prefixes.Select((Patch patch) => ("pre", patch: patch)).Concat(info.Postfixes.Select((Patch patch) => ("post", patch: patch))).Concat(info.Transpilers.Select((Patch patch) => ("transpile", patch: patch)))
				.Concat(info.Finalizers.Select((Patch patch) => ("final", patch: patch)));
		}

		private static string MethodName(MethodBase method)
		{
			return (method.DeclaringType?.Name ?? "<unknown>") + "." + method.Name;
		}

		private static string PatchMethodName(MethodInfo method)
		{
			if (!(method == null))
			{
				return (method.DeclaringType?.Name ?? "<unknown>") + "." + method.Name;
			}
			return "<unknown>";
		}

		private static bool CanSkipOriginal(MethodInfo method)
		{
			if (method != null)
			{
				return method.ReturnType == typeof(bool);
			}
			return false;
		}

		private static bool WritesResult(MethodInfo method)
		{
			if (method != null)
			{
				return method.GetParameters().Any((ParameterInfo parameter) => parameter.Name == "__result" && parameter.ParameterType.IsByRef);
			}
			return false;
		}

		private static string OwnerNames(IEnumerable<string> owners)
		{
			string[] array = owners.Where((string owner) => !string.IsNullOrWhiteSpace(owner)).Select(OwnerName).Distinct(StringComparer.Ordinal)
				.OrderBy((string name) => name, StringComparer.Ordinal)
				.ToArray();
			if (array.Length != 0)
			{
				return string.Join(", ", array);
			}
			return "未知 MOD";
		}

		private static string OwnerName(string owner)
		{
			if (string.IsNullOrWhiteSpace(owner))
			{
				return "未知 MOD";
			}
			int num = owner.IndexOf(',');
			int num2 = owner.IndexOf(':');
			int num3 = ((num >= 0 && num2 >= 0) ? Math.Min(num, num2) : Math.Max(num, num2));
			if (num3 <= 0)
			{
				return owner;
			}
			return owner.Substring(0, num3);
		}

		private static string PatchKindName(string kind)
		{
			return kind switch
			{
				"pre" => "前置", 
				"post" => "后置", 
				"transpile" => "转译", 
				"final" => "收尾", 
				_ => kind, 
			};
		}

		private static bool IsKnownCompatible(string target, (string Kind, Patch Value) patch)
		{
			string text = OwnerName(patch.Value.owner);
			string text2 = patch.Value.PatchMethod?.DeclaringType?.Name ?? string.Empty;
			switch (target)
			{
			case "GameItem.CanTarget":
				if (!(text == "XIAOWOKitchenBusiness"))
				{
					if (text == "XIAOWORatColony" && text2 == "RatMedicineCanTargetPatch")
					{
						return true;
					}
				}
				else if (text2 == "RottenDishCanTargetPatch")
				{
					return true;
				}
				break;
			case "GameItem.GetNegociatedValue":
				if (text == "XIAOWOKitchenBusiness")
				{
					if (text2 == "FreshnessPricePatch")
					{
						return true;
					}
					if (text2 == "FeaturedDishPricePatch")
					{
						return true;
					}
				}
				break;
			case "GameItem.MayTarget":
				if (!(text == "XIAOWOKitchenBusiness"))
				{
					if (text == "XIAOWORatColony" && text2 == "RatMedicineMayTargetPatch")
					{
						return true;
					}
				}
				else if (text2 == "RottenDishMayTargetPatch")
				{
					return true;
				}
				break;
			case "GameItem.Target":
				if (!(text == "XIAOWOKitchenBusiness"))
				{
					if (text == "XIAOWORatColony" && text2 == "RatMedicineTargetPatch")
					{
						return true;
					}
				}
				else if (text2 == "KitchenDishTargetPatch")
				{
					return true;
				}
				break;
			case "StartingPerk.GetLocalizedDescription":
				if (!(text == "ExtraPerks") || !(text2 == "PerkDescriptionPatch"))
				{
					break;
				}
				return true;
			case "StartingPerk.GetLocalizedDisplayName":
				if (!(text == "ExtraPerks") || !(text2 == "PerkDisplayNamePatch"))
				{
					break;
				}
				return true;
			case "StoreClientManager.TrackClient":
				if (!(text == "ExtraPerks") || !(text2 == "BoardInsiderTrackClientPatch"))
				{
					break;
				}
				return true;
			case "PlayerStore.EndDay":
				if (!(text == "ExtraPerks") || !(text2 == "NocturnalEndDayPatch"))
				{
					break;
				}
				return true;
			case "StartingPerk.IsShortSleeper":
				if (!(text == "ExtraPerks") || !(text2 == "NocturnalShortSleeperPatch"))
				{
					break;
				}
				return true;
			case "PlayerStore.HandleAfterhourDebuff":
				if (!(text == "ExtraPerks") || !(text2 == "NocturnalAfterhourDebuffPatch"))
				{
					break;
				}
				return true;
			}
			return false;
		}

		private static string ModuleFor(Type type)
		{
			string name = type.Name;
			if (name.StartsWith("SaveManager", StringComparison.Ordinal))
			{
				return "save-manager";
			}
			if (name.StartsWith("FinanceReport", StringComparison.Ordinal))
			{
				return "finance-report";
			}
			if (name.StartsWith("OwnerDeal", StringComparison.Ordinal))
			{
				return "owner";
			}
			if (name.StartsWith("AugHunt", StringComparison.Ordinal))
			{
				return "aug-hunt";
			}
			if (name.StartsWith("BayExpansion", StringComparison.Ordinal))
			{
				return "bay-expansion";
			}
			if (name.StartsWith("Fridge", StringComparison.Ordinal))
			{
				return "fridge-expansion";
			}
			if (name.StartsWith("WineRack", StringComparison.Ordinal))
			{
				return "wine-rack-expansion";
			}
			if (name.StartsWith("LoadingProgress", StringComparison.Ordinal))
			{
				return "loading";
			}
			if (name.StartsWith("OperatingSurcharge", StringComparison.Ordinal))
			{
				return "operating-costs";
			}
			bool flag = name.StartsWith("Cashflow", StringComparison.Ordinal);
			if (!flag)
			{
				bool flag2;
				switch (name)
				{
				case "DailyClientGenerationPatch":
				case "NewGameCashflowPatch":
				case "LoadGameCashflowPatch":
					flag2 = true;
					break;
				default:
					flag2 = false;
					break;
				}
				flag = flag2;
			}
			if (flag)
			{
				return "calendar";
			}
			flag = name.StartsWith("NightScavenging", StringComparison.Ordinal);
			if (!flag)
			{
				bool flag2 = ((name == "MinorWoundChancePatch" || name == "MajorWoundChancePatch") ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return "scavenging";
			}
			flag = name.StartsWith("StableSupply", StringComparison.Ordinal);
			if (!flag)
			{
				bool flag2 = ((name == "PlayerSaleContextPatch" || name == "NegotiatedValuePatch") ? true : false);
				flag = flag2;
			}
			if (flag)
			{
				return "trade-pricing";
			}
			if (name.StartsWith("PrecisionBay", StringComparison.Ordinal))
			{
				return "precision-bay";
			}
			if (name.StartsWith("BlackMarketSample", StringComparison.Ordinal))
			{
				return "starter-gift";
			}
			if (name.StartsWith("ThirdWatch", StringComparison.Ordinal))
			{
				return "third-watch";
			}
			if (name.StartsWith("ShowcaseCurator", StringComparison.Ordinal))
			{
				return "showcase-curator";
			}
			if (name.StartsWith("SecondSift", StringComparison.Ordinal))
			{
				return "second-sift";
			}
			if (name.StartsWith("FamiliarFace", StringComparison.Ordinal))
			{
				return "familiar-face";
			}
			if (name.StartsWith("ThinSupply", StringComparison.Ordinal))
			{
				return "thin-supply";
			}
			if (name.StartsWith("HighRoller", StringComparison.Ordinal))
			{
				return "high-roller";
			}
			if (name.StartsWith("CompatibilityBroker", StringComparison.Ordinal))
			{
				return "compatibility-broker";
			}
			if (name.StartsWith("Skin", StringComparison.Ordinal))
			{
				return "skins";
			}
			switch (name)
			{
			case "PerkDescriptionPatch":
			case "PerkElementIconPatch":
			case "PerkPickerPatch":
			case "FreeBuildLimitPatch":
			case "PerkSelectedOrderPatch":
			case "PerkDeselectedOrderPatch":
			case "StartingPerkRegistrationPatch":
			case "PerkNamePatch":
			case "IconInjectionPatch":
				flag = true;
				break;
			default:
				flag = false;
				break;
			}
			if (flag)
			{
				return "perk-picker";
			}
			return "trade-effects";
		}
	}
	internal static class PerkPickerScroll
	{
		private sealed class ScrollArea
		{
			private readonly string _contentName;

			private readonly string _scrollbarName;

			private GameObject _root;

			private GameObject _content;

			private ScrollRect _scroll;

			private Scrollbar _scrollbar;

			private GridLayoutGroup _grid;

			private float _savedPosition = 1f;

			private int _lastPerkCount = -1;

			internal ScrollArea(string contentName, string scrollbarName)
			{
				_contentName = contentName;
				_scrollbarName = scrollbarName;
			}

			internal bool IsAttached(GameObject current)
			{
				if (!string.Equals((current != null) ? ((Object)current).name : null, _contentName, StringComparison.Ordinal))
				{
					return (Object)(object)((current != null) ? current.transform.Find(_contentName) : null) != (Object)null;
				}
				return true;
			}

			internal void RequestTop()
			{
				_savedPosition = 1f;
			}

			internal GameObject Ensure(GameObject current)
			{
				if (!Resolve(current))
				{
					Create(current);
				}
				if (!Resolve(current))
				{
					return null;
				}
				MoveDirectPerksToContent(_root, _content);
				Refresh();
				return _content;
			}

			internal void Capture(GameObject current)
			{
				if (Resolve(current))
				{
					_savedPosition = (HasOverflow() ? _scroll.verticalNormalizedPosition : 1f);
				}
			}

			internal void ForceTop()
			{
				_savedPosition = 1f;
				if ((Object)(object)_scroll != (Object)null)
				{
					_scroll.verticalNormalizedPosition = 1f;
				}
				if ((Object)(object)_scrollbar != (Object)null)
				{
					_scrollbar.value = 1f;
				}
			}

			internal void RefreshIfCountChanged()
			{
				if (!IsAlive((Object)(object)_root) || !IsAlive((Object)(object)_content))
				{
					return;
				}
				MoveDirectPerksToContent(_root, _content);
				if (CountDirectPerks(_content.transform) != _lastPerkCount)
				{
					if (HasOverflow())
					{
						_savedPosition = _scroll.verticalNormalizedPosition;
					}
					Refresh();
				}
			}

			internal void Refresh()
			{
				//IL_0073: Unknown result type (might be due to invalid IL or missing references)
				//IL_0078: Unknown result type (might be due to invalid IL or missing references)
				//IL_008d: Unknown result type (might be due to invalid IL or missing references)
				//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c3: Unknown result type (might be due to invalid IL or missing references)
				//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
				//IL_0142: Unknown result type (might be due to invalid IL or missing references)
				//IL_015f: Unknown result type (might be due to invalid IL or missing references)
				//IL_016e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0173: Unknown result type (might be due to invalid IL or missing references)
				//IL_01a9: Unknown result type (might be due to invalid IL or missing references)
				//IL_01be: Unknown result type (might be due to invalid IL or missing references)
				//IL_01d3: Unknown result type (might be due to invalid IL or missing references)
				//IL_01de: Unknown result type (might be due to invalid IL or missing references)
				//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
				//IL_0208: Unknown result type (might be due to invalid IL or missing references)
				//IL_020d: Unknown result type (might be due to invalid IL or missing references)
				if (!IsAlive((Object)(object)_root) || !IsAlive((Object)(object)_content) || !IsAlive((Object)(object)_scroll) || !IsAlive((Object)(object)_scrollbar) || !IsAlive((Object)(object)_grid))
				{
					return;
				}
				RectTransform component = _root.GetComponent<RectTransform>();
				RectTransform component2 = _content.GetComponent<RectTransform>();
				if (!((Object)(object)component == (Object)null) && !((Object)(object)component2 == (Object)null))
				{
					Rect rect = component.rect;
					float num = Math.Max(1f, rect.width);
					float num2 = Math.Max(_grid.cellSize.x, num - 14f - 6f);
					int num3 = Math.Max(1, Mathf.FloorToInt((num2 + _grid.spacing.x) / (_grid.cellSize.x + _grid.spacing.x)));
					int num4 = Math.Max(1, ((_lastPerkCount = CountDirectPerks(_content.transform)) + num3 - 1) / num3);
					float num5 = (float)(((LayoutGroup)_grid).padding.top + ((LayoutGroup)_grid).padding.bottom) + (float)num4 * _grid.cellSize.y + (float)Math.Max(0, num4 - 1) * _grid.spacing.y;
					rect = component.rect;
					float num6 = Math.Max(rect.height, num5);
					_grid.constraint = (Constraint)1;
					_grid.constraintCount = num3;
					component2.anchorMin = new Vector2(0f, 1f);
					component2.anchorMax = new Vector2(1f, 1f);
					component2.pivot = new Vector2(0f, 1f);
					component2.anchoredPosition = Vector2.zero;
					component2.sizeDelta = new Vector2(0f, num6);
					LayoutRebuilder.ForceRebuildLayoutImmediate(component2);
					Canvas.ForceUpdateCanvases();
					rect = component.rect;
					bool flag = num5 > rect.height + 0.5f;
					if (!flag)
					{
						_savedPosition = 1f;
					}
					((Component)_scrollbar).gameObject.SetActive(flag);
					_scroll.verticalNormalizedPosition = Mathf.Clamp01(_savedPosition);
				}
			}

			internal bool Resolve(GameObject current)
			{
				if (IsAlive((Object)(object)_root) && IsAlive((Object)(object)_content) && IsAlive((Object)(object)_scroll) && IsAlive((Object)(object)_scrollbar) && IsAlive((Object)(object)_grid) && ((Object)(object)current == (Object)(object)_root || (Object)(object)current == (Object)(object)_content))
				{
					return true;
				}
				Clear();
				if ((Object)(object)current == (Object)null)
				{
					return false;
				}
				if (string.Equals(((Object)current).name, _contentName, StringComparison.Ordinal))
				{
					_content = current;
					Transform parent = current.transform.parent;
					_root = ((parent != null) ? ((Component)parent).gameObject : null);
				}
				else
				{
					_root = current;
					Transform val = _root.transform.Find(_contentName);
					_content = ((val != null) ? ((Component)val).gameObject : null);
				}
				GameObject root = _root;
				_scroll = ((root != null) ? root.GetComponent<ScrollRect>() : null);
				GameObject root2 = _root;
				object scrollbar;
				if (root2 == null)
				{
					scrollbar = null;
				}
				else
				{
					Transform val2 = root2.transform.Find(_scrollbarName);
					scrollbar = ((val2 != null) ? ((Component)val2).gameObject.GetComponent<Scrollbar>() : null);
				}
				_scrollbar = (Scrollbar)scrollbar;
				GameObject content = _content;
				_grid = ((content != null) ? content.GetComponent<GridLayoutGroup>() : null);
				if (IsAlive((Object)(object)_root) && IsAlive((Object)(object)_content) && IsAlive((Object)(object)_scroll) && IsAlive((Object)(object)_scrollbar))
				{
					return IsAlive((Object)(object)_grid);
				}
				return false;
			}

			internal string BuildStatus(string prefix)
			{
				//IL_0043: Unknown result type (might be due to invalid IL or missing references)
				//IL_0048: Unknown result type (might be due to invalid IL or missing references)
				//IL_005e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0063: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
				//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
				//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
				//IL_00c0: Unknown result type (might be due to invalid IL or missing references)
				RectTransform component = _root.GetComponent<RectTransform>();
				RectTransform component2 = _content.GetComponent<RectTransform>();
				string[] array = new string[25]
				{
					prefix, ".present=True\n", prefix, ".rootSize=", null, null, null, null, null, null,
					null, null, null, null, null, null, null, null, null, null,
					null, null, null, null, null
				};
				Rect rect;
				string text2;
				if (!((Object)(object)component == (Object)null))
				{
					rect = component.rect;
					string text = rect.width.ToString();
					rect = component.rect;
					text2 = text + "x" + rect.height;
				}
				else
				{
					text2 = "missing";
				}
				array[4] = text2;
				array[5] = "\n";
				array[6] = prefix;
				array[7] = ".contentSize=";
				string text4;
				if (!((Object)(object)component2 == (Object)null))
				{
					rect = component2.rect;
					string text3 = rect.width.ToString();
					rect = component2.rect;
					text4 = text3 + "x" + rect.height;
				}
				else
				{
					text4 = "missing";
				}
				array[8] = text4;
				array[9] = "\n";
				array[10] = prefix;
				array[11] = ".columns=";
				GridLayoutGroup grid = _grid;
				array[12] = ((grid != null) ? grid.constraintCount : 0).ToString();
				array[13] = "\n";
				array[14] = prefix;
				array[15] = ".perkCount=";
				array[16] = CountDirectPerks(_content.transform).ToString();
				array[17] = "\n";
				array[18] = prefix;
				array[19] = ".scrollbarVisible=";
				Scrollbar scrollbar = _scrollbar;
				array[20] = (scrollbar != null && ((Component)scrollbar).gameObject.activeInHierarchy).ToString();
				array[21] = "\n";
				array[22] = prefix;
				array[23] = ".position=";
				array[24] = _scroll.verticalNormalizedPosition.ToString();
				return string.Concat(array);
			}

			internal bool SetPosition(GameObject current, float position)
			{
				if (!Resolve(current))
				{
					return false;
				}
				_savedPosition = Mathf.Clamp01(position);
				_scroll.verticalNormalizedPosition = _savedPosition;
				_forceTopFrames = 0;
				return true;
			}

			internal bool SetRawPosition(GameObject current, float position)
			{
				if (!Resolve(current))
				{
					return false;
				}
				_scrollbar.value = Mathf.Clamp01(position);
				Canvas.ForceUpdateCanvases();
				return true;
			}

			internal float GetPosition(GameObject current)
			{
				if (!Resolve(current))
				{
					return -1f;
				}
				if (!HasOverflow())
				{
					return 1f;
				}
				return _scroll.verticalNormalizedPosition;
			}

			internal bool IsScrollbarVisible(GameObject current)
			{
				if (Resolve(current))
				{
					return ((Component)_scrollbar).gameObject.activeInHierarchy;
				}
				return false;
			}

			internal bool TestScrollbarValue(GameObject current, float position, out float actual)
			{
				actual = -1f;
				if (!Resolve(current))
				{
					return false;
				}
				_scrollbar.value = Mathf.Clamp01(position);
				Canvas.ForceUpdateCanvases();
				actual = _scroll.verticalNormalizedPosition;
				_savedPosition = actual;
				return Math.Abs(actual - Mathf.Clamp01(position)) < 0.01f;
			}

			internal bool TestMouseWheel(GameObject current, float delta, out float before, out float after)
			{
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_003e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0045: Unknown result type (might be due to invalid IL or missing references)
				//IL_0050: Expected Obj, but got Unknown
				before = -1f;
				after = -1f;
				if (!Resolve(current) || (Object)(object)EventSystem.current == (Object)null)
				{
					return false;
				}
				before = _scroll.verticalNormalizedPosition;
				PointerEventData val = new PointerEventData(EventSystem.current)
				{
					scrollDelta = new Vector2(0f, delta)
				};
				_scroll.OnScroll(val);
				Canvas.ForceUpdateCanvases();
				after = _scroll.verticalNormalizedPosition;
				_savedPosition = after;
				return Math.Abs(after - before) > 0.001f;
			}

			private void Create(GameObject root)
			{
				//IL_007a: Unknown result type (might be due to invalid IL or missing references)
				//IL_0084: Expected Obj, but got Unknown
				RectTransform val = ((root != null) ? root.GetComponent<RectTransform>() : null);
				GridLayoutGroup val2 = ((root != null) ? root.GetComponent<GridLayoutGroup>() : null);
				if (!((Object)(object)root == (Object)null) && !((Object)(object)val == (Object)null) && !((Object)(object)val2 == (Object)null))
				{
					_root = root;
					((Behaviour)val2).enabled = false;
					if (_root.GetComponent<RectMask2D>() == null)
					{
						_root.AddComponent<RectMask2D>();
					}
					_content = new GameObject(_contentName, new Type[2]
					{
						Il2CppType.Of<RectTransform>(),
						Il2CppType.Of<GridLayoutGroup>()
					});
					_content.transform.SetParent(_root.transform, false);
					_grid = _content.GetComponent<GridLayoutGroup>();
					CopyGrid(val2, _grid);
					MoveDirectPerksToContent(_root, _content);
					_scrollbar = CreateScrollbar(_root.transform, _scrollbarName);
					_scroll = _root.GetComponent<ScrollRect>() ?? _root.AddComponent<ScrollRect>();
					_scroll.content = _content.GetComponent<RectTransform>();
					_scroll.viewport = val;
					_scroll.horizontal = false;
					_scroll.vertical = true;
					_scroll.movementType = (MovementType)2;
					_scroll.inertia = false;
					_scroll.scrollSensitivity = 48f;
					_scroll.verticalScrollbar = _scrollbar;
					_scroll.verticalScrollbarVisibility = (ScrollbarVisibility)1;
					ForceTop();
					Instance log = Core.Log;
					if (log != null)
					{
						log.Msg("Scrollable starting-perk area attached: " + ((Object)_root).name);
					}
				}
			}

			private void Clear()
			{
				_root = null;
				_content = null;
				_scroll = null;
				_scrollbar = null;
				_grid = null;
				_lastPerkCount = -1;
			}

			private bool HasOverflow()
			{
				//IL_0039: Unknown result type (might be due to invalid IL or missing references)
				//IL_003e: Unknown result type (might be due to invalid IL or missing references)
				//IL_0047: Unknown result type (might be due to invalid IL or missing references)
				//IL_004c: Unknown result type (might be due to invalid IL or missing references)
				GameObject root = _root;
				RectTransform val = ((root != null) ? root.GetComponent<RectTransform>() : null);
				GameObject content = _content;
				RectTransform val2 = ((content != null) ? content.GetComponent<RectTransform>() : null);
				if ((Object)(object)val != (Object)null && (Object)(object)val2 != (Object)null)
				{
					Rect rect = val2.rect;
					float height = rect.height;
					rect = val.rect;
					return height > rect.height + 0.5f;
				}
				return false;
			}
		}

		internal const string AvailableScrollbarName = "XIAOWO_AvailablePerkScrollbar";

		internal const string SelectedScrollbarName = "XIAOWO_SelectedPerkScrollbar";

		private const float ScrollbarWidth = 14f;

		private const float ScrollbarMargin = 3f;

		private static readonly ScrollArea Available = new ScrollArea("XIAOWO_AvailablePerkScrollContent", "XIAOWO_AvailablePerkScrollbar");

		private static readonly ScrollArea Selected = new ScrollArea("XIAOWO_SelectedPerkScrollContent", "XIAOWO_SelectedPerkScrollbar");

		private static int _refreshCountdown;

		private static int _forceTopFrames;

		internal static void BeforeOpen(PerkUIController ui)
		{
			_forceTopFrames = 1;
			Available.RequestTop();
			Selected.RequestTop();
		}

		internal static void Tick()
		{
			PerkUIController instance = PerkUIController.Instance;
			if ((Object)(object)((instance != null) ? instance.ui : null) == (Object)null || !instance.ui.activeInHierarchy)
			{
				return;
			}
			if (_forceTopFrames > 0)
			{
				_forceTopFrames--;
				Available.ForceTop();
				Selected.ForceTop();
			}
			if (_refreshCountdown-- <= 0)
			{
				_refreshCountdown = 10;
				if (!Available.Resolve(instance.availablePerks))
				{
					instance.availablePerks = Available.Ensure(instance.availablePerks) ?? instance.availablePerks;
				}
				ExpandSelectedArea(instance);
				if (!Selected.Resolve(instance.selectedPerks))
				{
					instance.selectedPerks = Selected.Ensure(instance.selectedPerks) ?? instance.selectedPerks;
				}
				Available.RefreshIfCountChanged();
				Selected.RefreshIfCountChanged();
			}
		}

		internal static void Capture(PerkUIController ui)
		{
			if (_forceTopFrames <= 0)
			{
				Available.Capture((ui != null) ? ui.availablePerks : null);
				Selected.Capture((ui != null) ? ui.selectedPerks : null);
			}
		}

		internal static void Ensure(PerkUIController ui)
		{
			if (!((Object)(object)ui == (Object)null))
			{
				ui.availablePerks = Available.Ensure(ui.availablePerks) ?? ui.availablePerks;
				ExpandSelectedArea(ui);
				ui.selectedPerks = Selected.Ensure(ui.selectedPerks) ?? ui.selectedPerks;
				Available.Refresh();
				Selected.Refresh();
				if (_forceTopFrames > 0)
				{
					Available.ForceTop();
					Selected.ForceTop();
				}
			}
		}

		internal static void Refresh(PerkUIController ui)
		{
			if (!((Object)(object)ui == (Object)null))
			{
				Available.Resolve(ui.availablePerks);
				Selected.Resolve(ui.selectedPerks);
				Available.Refresh();
				Selected.Refresh();
			}
		}

		internal static string BuildStatus(PerkUIController ui)
		{
			string text = (Available.Resolve((ui != null) ? ui.availablePerks : null) ? Available.BuildStatus("available") : "available.present=False");
			string text2 = (Selected.Resolve((ui != null) ? ui.selectedPerks : null) ? Selected.BuildStatus("selected") : "selected.present=False");
			return "PERK_SCROLL\n" + text + "\n" + text2;
		}

		internal static bool SetTestPosition(PerkUIController ui, float position)
		{
			return Available.SetPosition((ui != null) ? ui.availablePerks : null, position);
		}

		internal static float GetTestPosition(PerkUIController ui)
		{
			return Available.GetPosition((ui != null) ? ui.availablePerks : null);
		}

		internal static float GetSelectedTestPosition(PerkUIController ui)
		{
			return Selected.GetPosition((ui != null) ? ui.selectedPerks : null);
		}

		internal static bool SetSelectedTestPosition(PerkUIController ui, float position)
		{
			return Selected.SetPosition((ui != null) ? ui.selectedPerks : null, position);
		}

		internal static bool IsSelectedScrollbarVisible(PerkUIController ui)
		{
			return Selected.IsScrollbarVisible((ui != null) ? ui.selectedPerks : null);
		}

		internal static bool TestScrollbarValue(PerkUIController ui, float position, out float actual)
		{
			return Available.TestScrollbarValue((ui != null) ? ui.availablePerks : null, position, out actual);
		}

		internal static bool TestMouseWheel(PerkUIController ui, float delta, out float before, out float after)
		{
			return Available.TestMouseWheel((ui != null) ? ui.availablePerks : null, delta, out before, out after);
		}

		internal static bool SetRawAvailableTestPosition(PerkUIController ui, float position)
		{
			return Available.SetRawPosition((ui != null) ? ui.availablePerks : null, position);
		}

		internal static bool SetRawSelectedTestPosition(PerkUIController ui, float position)
		{
			return Selected.SetRawPosition((ui != null) ? ui.selectedPerks : null, position);
		}

		private static void ExpandSelectedArea(PerkUIController ui)
		{
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = ((ui != null) ? ui.selectedPerks : null);
			if ((Object)(object)val == (Object)null || Selected.IsAttached(val))
			{
				return;
			}
			RectTransform component = val.GetComponent<RectTransform>();
			Transform parent = val.transform.parent;
			if ((Object)(object)component == (Object)null || (Object)(object)parent == (Object)null)
			{
				return;
			}
			Rect rect = component.rect;
			float height = rect.height;
			if (!(height <= 0f))
			{
				ResizeKeepingTop(component, height * 2f);
				Transform val2 = parent.Find("SelectedPerkBG");
				RectTransform val3 = ((val2 != null) ? ((Component)val2).gameObject.GetComponent<RectTransform>() : null);
				if ((Object)(object)val3 != (Object)null)
				{
					rect = val3.rect;
					ResizeKeepingTop(val3, rect.height + height);
				}
				Transform val4 = parent.Find("PerkPoint");
				MoveDown((val4 != null) ? ((Component)val4).gameObject.GetComponent<RectTransform>() : null, height);
				Transform val5 = parent.Find("PerkSlot");
				MoveDown((val5 != null) ? ((Component)val5).gameObject.GetComponent<RectTransform>() : null, height);
			}
		}

		private static void ResizeKeepingTop(RectTransform rect, float newHeight)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_0020: Unknown result type (might be due to invalid IL or missing references)
			//IL_0031: Unknown result type (might be due to invalid IL or missing references)
			//IL_003c: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			Rect rect2 = rect.rect;
			float height = rect2.height;
			float num = rect.anchoredPosition.y + (1f - rect.pivot.y) * height;
			rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y + newHeight - height);
			rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, num - (1f - rect.pivot.y) * newHeight);
		}

		private static void MoveDown(RectTransform rect, float amount)
		{
			//IL_000b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0022: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)rect != (Object)null)
			{
				rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, rect.anchoredPosition.y - amount);
			}
		}

		private static Scrollbar CreateScrollbar(Transform parent, string name)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Expected Obj, but got Unknown
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0075: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00df: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e5: Expected Obj, but got Unknown
			//IL_00fe: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_0176: Unknown result type (might be due to invalid IL or missing references)
			//IL_017e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0189: Unknown result type (might be due to invalid IL or missing references)
			//IL_0194: Unknown result type (might be due to invalid IL or missing references)
			//IL_019f: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c4: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = new GameObject(name, new Type[4]
			{
				Il2CppType.Of<RectTransform>(),
				Il2CppType.Of<CanvasRenderer>(),
				Il2CppType.Of<Image>(),
				Il2CppType.Of<Scrollbar>()
			});
			val.transform.SetParent(parent, false);
			RectTransform component = val.GetComponent<RectTransform>();
			component.anchorMin = new Vector2(1f, 0f);
			component.anchorMax = new Vector2(1f, 1f);
			component.pivot = new Vector2(1f, 0.5f);
			component.anchoredPosition = new Vector2(-3f, 0f);
			component.sizeDelta = new Vector2(14f, -6f);
			((Graphic)val.GetComponent<Image>()).color = new Color(35f / 255f, 30f / 255f, 47f / 255f, 1f);
			GameObject val2 = new GameObject("Sliding Area", new Type[1] { Il2CppType.Of<RectTransform>() });
			val2.transform.SetParent(val.transform, false);
			RectTransform component2 = val2.GetComponent<RectTransform>();
			component2.anchorMin = Vector2.zero;
			component2.anchorMax = Vector2.one;
			component2.offsetMin = new Vector2(2f, 2f);
			component2.offsetMax = new Vector2(-2f, -2f);
			GameObject val3 = new GameObject("Handle", new Type[3]
			{
				Il2CppType.Of<RectTransform>(),
				Il2CppType.Of<CanvasRenderer>(),
				Il2CppType.Of<Image>()
			});
			val3.transform.SetParent(val2.transform, false);
			RectTransform component3 = val3.GetComponent<RectTransform>();
			component3.anchorMin = Vector2.zero;
			component3.anchorMax = Vector2.one;
			component3.offsetMin = Vector2.zero;
			component3.offsetMax = Vector2.zero;
			Image component4 = val3.GetComponent<Image>();
			((Graphic)component4).color = new Color(146f / 255f, 201f / 255f, 190f / 255f, 1f);
			Scrollbar component5 = val.GetComponent<Scrollbar>();
			component5.handleRect = component3;
			((Selectable)component5).targetGraphic = (Graphic)(object)component4;
			component5.direction = (Direction)2;
			component5.numberOfSteps = 0;
			component5.value = 1f;
			return component5;
		}

		private static void CopyGrid(GridLayoutGroup source, GridLayoutGroup target)
		{
			//IL_0002: Unknown result type (might be due to invalid IL or missing references)
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0032: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Expected Obj, but got Unknown
			target.cellSize = source.cellSize;
			target.spacing = source.spacing;
			target.startCorner = source.startCorner;
			target.startAxis = source.startAxis;
			((LayoutGroup)target).childAlignment = ((LayoutGroup)source).childAlignment;
			((LayoutGroup)target).padding = new RectOffset(((LayoutGroup)source).padding.left, ((LayoutGroup)source).padding.right, ((LayoutGroup)source).padding.top, ((LayoutGroup)source).padding.bottom);
		}

		private static void MoveDirectPerksToContent(GameObject root, GameObject content)
		{
			if ((Object)(object)root == (Object)null || (Object)(object)content == (Object)null || (Object)(object)root == (Object)(object)content)
			{
				return;
			}
			List<Transform> list = new List<Transform>();
			for (int i = 0; i < root.transform.childCount; i++)
			{
				Transform child = root.transform.GetChild(i);
				if ((Object)(object)child != (Object)null && (Object)(object)((Component)child).gameObject.GetComponent<StartingPerkElement>() != (Object)null)
				{
					list.Add(child);
				}
			}
			foreach (Transform item in list)
			{
				item.SetParent(content.transform, false);
			}
		}

		private static int CountDirectPerks(Transform content)
		{
			if ((Object)(object)content == (Object)null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < content.childCount; i++)
			{
				Transform child = content.GetChild(i);
				if ((Object)(object)child != (Object)null && (Object)(object)((Component)child).gameObject.GetComponent<StartingPerkElement>() != (Object)null)
				{
					num++;
				}
			}
			return num;
		}

		private static bool IsAlive(Object value)
		{
			if (value != (Object)null)
			{
				return ((Il2CppObjectBase)value).Pointer != IntPtr.Zero;
			}
			return false;
		}
	}
	internal sealed class PerkDefinition
	{
		internal string Id { get; }

		internal string ChineseName { get; }

		internal string EnglishName { get; }

		internal string ChineseDescription { get; }

		internal string EnglishDescription { get; }

		internal string Name => ModLocalization.Select(ChineseName, EnglishName);

		internal string Description => ModLocalization.Select(ChineseDescription, EnglishDescription);

		internal int Cost { get; }

		internal StartingPerkType Type
		{
			[CompilerGenerated]
			get
			{
				//IL_0001: Unknown result type (might be due to invalid IL or missing references)
				return field;
			}
		}

		internal string IconFile { get; }

		internal int MaxSlot { get; }

		internal string[] IncompatibleIds { get; }

		internal PerkDefinition(string id, string chineseName, string englishName, string chineseDescription, string englishDescription, int cost, StartingPerkType type, string iconFile, int maxSlot = 0, params string[] incompatibleIds)
		{
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			Id = id;
			ChineseName = chineseName;
			EnglishName = englishName;
			ChineseDescription = chineseDescription;
			EnglishDescription = englishDescription;
			Cost = cost;
			Type = type;
			IconFile = iconFile;
			MaxSlot = maxSlot;
			IncompatibleIds = incompatibleIds ?? Array.Empty<string>();
		}
	}
	internal static class PerkIds
	{
		internal const string GoldAppraiser = "xiaowo_trade_gold_appraiser";

		internal const string AppraisalSign = "xiaowo_trade_appraisal_sign";

		internal const string GreyInsurance = "xiaowo_trade_grey_insurance";

		internal const string BlackoutBroker = "xiaowo_trade_blackout_broker";

		internal const string LuxuryAppointment = "xiaowo_trade_luxury_appointment";

		internal const string StableSupply = "xiaowo_trade_stable_supply";

		internal const string ReputationBusiness = "xiaowo_trade_reputation_business";

		internal const string NegotiationVeteran = "xiaowo_trade_negotiation_veteran";

		internal const string CashflowGambler = "xiaowo_trade_cashflow_gambler";

		internal const string SecurityTarget = "xiaowo_trade_security_target";

		internal const string RundownStorefront = "xiaowo_trade_rundown_storefront";

		internal const string RegularCredit = "xiaowo_trade_regular_credit";

		internal const string OwnerDeal = "xiaowo_trade_owner_deal";

		internal const string PeerlessProdigy = "xiaowo_trade_peerless_prodigy";

		internal const string RecklessScavenger = "xiaowo_trade_reckless_scavenger";

		internal const string AugHuntContract = "xiaowo_trade_aug_hunt_contract";

		internal const string OperatingSurcharge = "xiaowo_trade_operating_surcharge";

		internal const string PrecisionBayExpansion = "xiaowo_trade_precision_bay_expansion";

		internal const string BlackMarketSample = "xiaowo_trade_black_market_sample";

		internal const string ThirdWatchRoamer = "xiaowo_trade_third_watch_roamer";

		internal const string ShowcaseCurator = "xiaowo_trade_showcase_curator";

		internal const string SecondSift = "xiaowo_trade_second_sift";

		internal const string FamiliarFace = "xiaowo_trade_familiar_face";

		internal const string ThinSupply = "xiaowo_trade_thin_supply";

		internal const string HighRollerMagnet = "xiaowo_trade_high_roller_magnet";
	}
	internal static class PerkRegistry
	{
		internal static readonly PerkDefinition[] All = new PerkDefinition[25]
		{
			new PerkDefinition("xiaowo_trade_gold_appraiser", "金牌鉴定师", "Gold Appraiser", "鉴定服务实际收取的服务费提高20%。", "Appraisal service fees actually collected are increased by 20%.", 2, (StartingPerkType)0, "01_gold_appraiser.png", 0),
			new PerkDefinition("xiaowo_trade_appraisal_sign", "鉴定招牌", "Appraisal Sign", "橱窗摆放鉴定证书时，鉴定服务客人的出现机会提高30%。", "While an appraisal certificate is displayed in the showcase, appraisal customers are 30% more likely to appear.", 2, (StartingPerkType)0, "02_appraisal_sign.png", 0),
			new PerkDefinition("xiaowo_trade_grey_insurance", "灰色保单", "Grey Insurance", "每周首位出售赃物给你的商人，其本次来访中全部赃物热度减半。", "The first merchant each week who sells you stolen goods has the heat of every stolen item in that visit halved.", 1, (StartingPerkType)0, "03_grey_insurance.png", 0),
			new PerkDefinition("xiaowo_trade_blackout_broker", "停电掮客", "Blackout Broker", "停电期间，你向顾客出售物品的成交价提高10%。", "During a blackout, prices you receive when selling items to customers are increased by 10%.", 1, (StartingPerkType)0, "04_blackout_broker.png", 0),
			new PerkDefinition("xiaowo_trade_luxury_appointment", "贵客预约", "Luxury Appointment", "每周额外来一位奢侈品收购商；预算为普通顾客均值的150%，但收购价降低10%。", "One extra luxury-goods buyer visits each week. Their budget is 150% of the average ordinary customer budget, but their purchase prices are 10% lower.", 2, (StartingPerkType)0, "05_luxury_appointment.png", 0),
			new PerkDefinition("xiaowo_trade_stable_supply", "稳定货源", "Stable Supply", "普通供货商向你出售物品的成交价降低10%；剧情与特殊客人不受影响。", "Prices paid when buying from ordinary suppliers are reduced by 10%. Story and special clients are unaffected.", 2, (StartingPerkType)0, "14_stable_supply.png", 0),
			new PerkDefinition("xiaowo_trade_reputation_business", "口碑生意", "Reputation Business", "正常交易获得的阵营声望提高20%；声望损失不受影响。", "Positive faction reputation gained from normal trades is increased by 20%. Reputation losses are unaffected.", 1, (StartingPerkType)0, "15_reputation_business.png", 0),
			new PerkDefinition("xiaowo_trade_negotiation_veteran", "谈判老手", "Negotiation Veteran", "所有主动议价的成功率提高10个百分点，最高不超过95%。", "All active bargaining success chances are increased by 10 percentage points, up to 95%.", 2, (StartingPerkType)0, "16_negotiation_veteran.png", 0),
			new PerkDefinition("xiaowo_trade_cashflow_gambler", "现金流赌徒", "Cashflow Gambler", "每周有一天出现一种行情：红色繁荣使所有价格上涨20%，绿色紧缩使所有价格下降20%。", "One day each week has a market condition: a red Boom raises all prices by 20%, while a green Contraction lowers all prices by 20%.", 0, (StartingPerkType)2, "06_cashflow_gambler.png", 0),
			new PerkDefinition("xiaowo_trade_security_target", "安保眼中钉", "Security Target", "赃物初始热度提高25%，安保检查间隔缩短25%。", "Initial heat on stolen goods is increased by 25%, and the interval between security inspections is reduced by 25%.", -3, (StartingPerkType)1, "07_security_target.png", 0),
			new PerkDefinition("xiaowo_trade_rundown_storefront", "门面失修", "Rundown Storefront", "店铺吸引力降低150，所有店铺翻新费用提高20%。", "Store attractiveness is reduced by 150, and all store renovation costs are increased by 20%.", -2, (StartingPerkType)1, "08_rundown_storefront.png", 0),
			new PerkDefinition("xiaowo_trade_regular_credit", "熟客赊账", "Regulars on Credit", "普通回头客的可用预算降低25%；剧情和特殊客人不受影响。", "Ordinary returning customers have 25% less available budget. Story and special customers are unaffected.", -1, (StartingPerkType)1, "09_regular_credit.png", 0),
			new PerkDefinition("xiaowo_trade_owner_deal", "业主密约", "Owner's Deal", "买断后游戏不会结束；每逢原租金日，房东85折出售稀有货物，同行商人继续上门收货。", "Buying the property no longer ends the run. On each former rent day, the landlord sells rare goods at a 15% discount and the accompanying merchant still visits to buy goods.", 0, (StartingPerkType)2, "10_owner_deal.png", 0),
			new PerkDefinition("xiaowo_trade_peerless_prodigy", "旷世奇才", "Peerless Prodigy", "允许额外拥有12个初始天赋栏位。", "Allows 12 additional starting perk slots.", 3, (StartingPerkType)2, "11_peerless_prodigy.png", 12, "colorful_character"),
			new PerkDefinition("xiaowo_trade_reckless_scavenger", "莽撞拾荒者", "Reckless Scavenger", "夜间前往垃圾堆拾荒时，轻伤和重伤概率各提高15个百分点。", "When scavenging at the dumping grounds at night, minor and major wound chances each increase by 15 percentage points.", -2, (StartingPerkType)1, "12_reckless_scavenger.png", 0),
			new PerkDefinition("xiaowo_trade_aug_hunt_contract", "猎杀契约", "Aug Hunt Contract", "导师交付霰弹枪期间，改造体来访机会额外提高30%，每天最多出现2名；当天若有改造体未被击杀，入睡时你会遭到报复并立即结束游戏。", "While the mentor's shotgun is available, Aug visits gain an extra 30% chance, with at most two per day. If any Aug survives the day, they retaliate when you sleep and the run ends.", -3, (StartingPerkType)1, "13_aug_hunt_contract.png", 0),
			new PerkDefinition("xiaowo_trade_operating_surcharge", "运营附加费", "Operating Surcharge", "电力、清洁、保险以及买卖推荐服务的费用提高20%。", "Power, janitorial, insurance, and buy/sell referral service costs are increased by 20%.", -2, (StartingPerkType)1, "17_operating_surcharge.png", 0),
			new PerkDefinition("xiaowo_trade_precision_bay_expansion", "精工扩舱", "Precision Bay Expansion", "可使用金属锭升级机器区、储藏区及其扩容版本，并将老鼠笼的升级上限与最终容量翻倍；未满级时优先升级，满级后可正常放入金属锭。", "Metal ingots upgrade machine and storage bays, including expanded versions, and double the rat cage upgrade limit and final capacity. Upgrades take priority until maximum level, after which ingots can be stored normally.", 0, (StartingPerkType)0, "18_precision_bay_expansion.png", 0),
			new PerkDefinition("xiaowo_trade_black_market_sample", "灰市样本", "Black-Market Sample", "开局获得1个神经模组（受限）。", "Start with one restricted neural module.", 5, (StartingPerkType)0, "19_black_market_sample.png", 0),
			new PerkDefinition("xiaowo_trade_third_watch_roamer", "三更行者", "Third-Watch Roamer", "每天打烊后最多可前往3个室外地点，并视为短睡眠，不会因熬夜错过第二天顾客。", "Visit up to three outdoor locations after closing each day. This counts as short sleep and does not make you miss the next day's customers.", 3, (StartingPerkType)0, "20_third_watch_roamer.png", 0, "夜猫子", "Nocturnal", "深度睡眠", "DeepSleep"),
			new PerkDefinition("xiaowo_trade_showcase_curator", "橱窗策展人", "Showcase Curator", "橱窗内有商品时，店铺当前吸引力提高50%。", "While merchandise is displayed in the showcase, current store attractiveness is increased by 50%.", 2, (StartingPerkType)0, "21_showcase_curator.png", 0),
			new PerkDefinition("xiaowo_trade_second_sift", "二次淘金", "Second Sift", "每晚第一次成功在垃圾堆拾荒时，从原版掉落池额外获得1件物品；额外物品不会再次判定受伤。", "The first successful dumping-ground search each night grants one extra item from the native loot pool without another injury roll.", 2, (StartingPerkType)0, "22_second_sift.png", 0),
			new PerkDefinition("xiaowo_trade_familiar_face", "安保熟脸", "Familiar Face", "每周第一次普通安保检查只会警告，不搜查或没收物品；剧情与通缉事件不受影响。", "The first ordinary security inspection each week becomes a warning with no search or confiscation. Story and wanted events are unaffected.", 3, (StartingPerkType)0, "23_familiar_face.png", 0),
			new PerkDefinition("xiaowo_trade_thin_supply", "货源缩水", "Thin Supply", "普通供货商携带的商品数量减少25%，至少保留1件；剧情与特殊供货商不受影响。", "Ordinary suppliers carry 25% fewer items, with at least one remaining. Story and special suppliers are unaffected.", -3, (StartingPerkType)1, "24_thin_supply.png", 0),
			new PerkDefinition("xiaowo_trade_high_roller_magnet", "阔客磁场", "High-Roller Magnet", "所有有交易预算的来店NPC，其基础预算提高100%（变为原来的2倍）。", "Every visiting NPC with a trading budget receives a 100% base-budget increase, doubling the original amount.", 5, (StartingPerkType)0, "25_high_roller_magnet.png", 0)
		};

		private static readonly Dictionary<string, StartingPerk> Created = new Dictionary<string, StartingPerk>(StringComparer.Ordinal);

		internal static bool IsActive(string id)
		{
			if (IsAlwaysEnabledFeature(id))
			{
				return true;
			}
			try
			{
				return StartingPerk.IsPerkActive(id);
			}
			catch
			{
				return false;
			}
		}

		internal static PerkDefinition Find(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			for (int i = 0; i < All.Length; i++)
			{
				if (string.Equals(All[i].Id, id, StringComparison.Ordinal))
				{
					return All[i];
				}
			}
			return null;
		}

		internal static void EnsureRegistered()
		{
			List<StartingPerk> perks = StartingPerkList.Perks;
			if (perks == null)
			{
				return;
			}
			PerkDefinition[] all = All;
			foreach (PerkDefinition perkDefinition in all)
			{
				StartingPerk val = FindPerk(perks, perkDefinition.Id);
				if (val != null)
				{
					ApplyDefinition(val, perkDefinition);
					Created[perkDefinition.Id] = val;
					continue;
				}
				perks.Add(GetOrCreate(perkDefinition));
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Registered perk: " + perkDefinition.Name);
				}
			}
			EnsureMutualIncompatibility(perks);
			EnsureDeclaredMutualIncompatibility(perks);
		}

		internal static void RefreshLocalizedText()
		{
			PerkDefinition[] all = All;
			foreach (PerkDefinition perkDefinition in all)
			{
				StartingPerk val = (Created.TryGetValue(perkDefinition.Id, out var value) ? value : FindPerk(StartingPerkList.Perks, perkDefinition.Id));
				if (val != null)
				{
					ApplyDefinition(val, perkDefinition);
				}
			}
		}

		internal static void EnsurePicker(PerkUIController ui)
		{
			if ((Object)(object)ui == (Object)null || (Object)(object)ui.availablePerks == (Object)null || (Object)(object)ui.perkElementPrefab == (Object)null)
			{
				return;
			}
			EnsureRegistered();
			RemoveAlwaysEnabledFeatures(ui);
			bool flag = false;
			PerkDefinition[] all = All;
			foreach (PerkDefinition perkDefinition in all)
			{
				if (IsAlwaysEnabledFeature(perkDefinition.Id))
				{
					continue;
				}
				StartingPerkElement val = FindElement(ui.selectedPerks, perkDefinition.Id) ?? FindElement(ui.availablePerks, perkDefinition.Id);
				if ((Object)(object)val == (Object)null)
				{
					GameObject val2 = Object.Instantiate<GameObject>(ui.perkElementPrefab, ui.availablePerks.transform);
					val = val2.GetComponent<StartingPerkElement>();
					if ((Object)(object)val == (Object)null)
					{
						Object.Destroy((Object)(object)val2);
						continue;
					}
					val.id = perkDefinition.Id;
					val.isSelected = false;
					val2.SetActive(true);
					flag = true;
				}
				val.perk = GetOrCreate(perkDefinition);
				Sprite val3 = IconLoader.Get(perkDefinition);
				if ((Object)(object)val3 != (Object)null && (Object)(object)val.icon != (Object)null)
				{
					val.icon.sprite = val3;
				}
			}
			if (flag)
			{
				ui.SortPerkContainer(ui.availablePerks);
			}
			PlacePeerlessBeforeOddball(ui.availablePerks);
			PerkPickerScroll.Ensure(ui);
		}

		internal static void AfterPickerMove(PerkUIController ui, StartingPerkElement movedPerk, bool selected)
		{
			if ((Object)(object)ui == (Object)null || (Object)(object)movedPerk == (Object)null)
			{
				return;
			}
			RemoveAlwaysEnabledFeatures(ui);
			if (IsAlwaysEnabledFeature(movedPerk.id))
			{
				PerkPickerScroll.Refresh(ui);
				return;
			}
			if (selected && string.Equals(movedPerk.id, "xiaowo_trade_peerless_prodigy", StringComparison.Ordinal))
			{
				MoveToEndOfPerks(ui.selectedPerks, movedPerk);
			}
			PlacePeerlessBeforeOddball(ui.availablePerks);
			PerkPickerScroll.Refresh(ui);
		}

		private static StartingPerk GetOrCreate(PerkDefinition definition)
		{
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Expected Obj, but got Unknown
			if (Created.TryGetValue(definition.Id, out var value) && value != null)
			{
				return value;
			}
			value = new StartingPerk();
			ApplyDefinition(value, definition);
			Created[definition.Id] = value;
			return value;
		}

		private static void ApplyDefinition(StartingPerk perk, PerkDefinition definition)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			perk.id = definition.Id;
			perk.cost = definition.Cost;
			perk.maxSlot = definition.MaxSlot;
			perk.type = definition.Type;
			perk.rawName = definition.Name;
			perk.rawDescription = definition.Description;
			if (perk.incompatiblePerks == null)
			{
				List<string> val = (perk.incompatiblePerks = new List<string>());
			}
			for (int i = 0; i < definition.IncompatibleIds.Length; i++)
			{
				AddIncompatible(perk, definition.IncompatibleIds[i]);
			}
		}

		private static void EnsureMutualIncompatibility(List<StartingPerk> list)
		{
			StartingPerk val = FindPerk(list, "xiaowo_trade_peerless_prodigy");
			if (val == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				StartingPerk val2 = list[i];
				string a = CleanId((val2 != null) ? val2.id : null);
				if (string.Equals(a, "colorful_character", StringComparison.Ordinal) || string.Equals(a, "奇葩人物", StringComparison.Ordinal))
				{
					AddIncompatible(val, val2.id);
					AddIncompatible(val2, "xiaowo_trade_peerless_prodigy");
				}
			}
		}

		private static void EnsureDeclaredMutualIncompatibility(List<StartingPerk> list)
		{
			if (list == null)
			{
				return;
			}
			PerkDefinition[] all = All;
			foreach (PerkDefinition perkDefinition in all)
			{
				if (perkDefinition.IncompatibleIds.Length == 0)
				{
					continue;
				}
				StartingPerk val = FindPerk(list, perkDefinition.Id);
				if (val == null)
				{
					continue;
				}
				for (int j = 0; j < list.Count; j++)
				{
					StartingPerk val2 = list[j];
					if (val2 == null)
					{
						continue;
					}
					string a = CleanId(val2.id);
					for (int k = 0; k < perkDefinition.IncompatibleIds.Length; k++)
					{
						if (string.Equals(a, CleanId(perkDefinition.IncompatibleIds[k]), StringComparison.OrdinalIgnoreCase))
						{
							AddIncompatible(val, val2.id);
							AddIncompatible(val2, perkDefinition.Id);
							break;
						}
					}
				}
			}
		}

		private static void AddIncompatible(StartingPerk perk, string id)
		{
			if (perk == null || string.IsNullOrEmpty(id))
			{
				return;
			}
			if (perk.incompatiblePerks == null)
			{
				List<string> val = (perk.incompatiblePerks = new List<string>());
			}
			for (int i = 0; i < perk.incompatiblePerks.Count; i++)
			{
				if (string.Equals(perk.incompatiblePerks[i], id, StringComparison.Ordinal))
				{
					return;
				}
			}
			perk.incompatiblePerks.Add(id);
		}

		private static string CleanId(string id)
		{
			if (!string.IsNullOrEmpty(id))
			{
				return id.Replace("\0", string.Empty).Trim();
			}
			return string.Empty;
		}

		private static StartingPerk FindPerk(List<StartingPerk> list, string id)
		{
			if (list == null)
			{
				return null;
			}
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i] != null && string.Equals(list[i].id, id, StringComparison.Ordinal))
				{
					return list[i];
				}
			}
			return null;
		}

		private static StartingPerkElement FindElement(GameObject root, string id)
		{
			if ((Object)(object)root == (Object)null)
			{
				return null;
			}
			foreach (StartingPerkElement componentsInChild in root.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && string.Equals(componentsInChild.id, id, StringComparison.Ordinal))
				{
					return componentsInChild;
				}
			}
			return null;
		}

		private static void PlacePeerlessBeforeOddball(GameObject root)
		{
			StartingPerkElement val = FindElement(root, "xiaowo_trade_peerless_prodigy");
			StartingPerkElement val2 = FindElementByCleanId(root, "奇葩人物");
			if ((Object)(object)val == (Object)null || (Object)(object)val2 == (Object)null)
			{
				return;
			}
			Transform transform = ((Component)val).transform;
			Transform transform2 = ((Component)val2).transform;
			if (!((Object)(object)transform == (Object)null) && !((Object)(object)transform2 == (Object)null) && !((Object)(object)transform.parent != (Object)(object)transform2.parent))
			{
				int siblingIndex = transform.GetSiblingIndex();
				int siblingIndex2 = transform2.GetSiblingIndex();
				int num = ((siblingIndex < siblingIndex2) ? (siblingIndex2 - 1) : siblingIndex2);
				if (siblingIndex != num)
				{
					transform.SetSiblingIndex(num);
				}
			}
		}

		internal static bool IsAlwaysEnabledFeature(string id)
		{
			if (!string.Equals(id, "xiaowo_trade_owner_deal", StringComparison.Ordinal))
			{
				return string.Equals(id, "xiaowo_trade_precision_bay_expansion", StringComparison.Ordinal);
			}
			return true;
		}

		internal static bool IsSelectablePerk(string id)
		{
			return !IsAlwaysEnabledFeature(id);
		}

		private static void RemoveAlwaysEnabledFeatures(PerkUIController ui)
		{
			RemoveElements((ui != null) ? ui.availablePerks : null, "xiaowo_trade_owner_deal", "xiaowo_trade_precision_bay_expansion");
			RemoveElements((ui != null) ? ui.selectedPerks : null, "xiaowo_trade_owner_deal", "xiaowo_trade_precision_bay_expansion");
		}

		private static void RemoveElements(GameObject root, params string[] ids)
		{
			if ((Object)(object)root == (Object)null)
			{
				return;
			}
			foreach (StartingPerkElement componentsInChild in root.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if ((Object)(object)componentsInChild == (Object)null)
				{
					continue;
				}
				bool flag = false;
				for (int i = 0; i < ids.Length; i++)
				{
					if (string.Equals(componentsInChild.id, ids[i], StringComparison.Ordinal))
					{
						flag = true;
						break;
					}
				}
				if (flag)
				{
					((Component)componentsInChild).gameObject.SetActive(false);
					((Component)componentsInChild).transform.SetParent((Transform)null, false);
					Object.Destroy((Object)(object)((Component)componentsInChild).gameObject);
				}
			}
		}

		private static void MoveToEndOfPerks(GameObject root, StartingPerkElement movedPerk)
		{
			if ((Object)(object)root == (Object)null || (Object)(object)((movedPerk != null) ? ((Component)movedPerk).transform : null) == (Object)null)
			{
				return;
			}
			Transform parent = ((Component)movedPerk).transform.parent;
			int num = ((Component)movedPerk).transform.GetSiblingIndex();
			foreach (StartingPerkElement componentsInChild in root.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if (!((Object)(object)((componentsInChild != null) ? ((Component)componentsInChild).transform : null) == (Object)null) && !((Object)(object)((Component)componentsInChild).transform.parent != (Object)(object)parent))
				{
					num = Math.Max(num, ((Component)componentsInChild).transform.GetSiblingIndex());
				}
			}
			((Component)movedPerk).transform.SetSiblingIndex(num);
		}

		private static StartingPerkElement FindElementByCleanId(GameObject root, string id)
		{
			if ((Object)(object)root == (Object)null)
			{
				return null;
			}
			foreach (StartingPerkElement componentsInChild in root.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && string.Equals(CleanId(componentsInChild.id), id, StringComparison.Ordinal))
				{
					return componentsInChild;
				}
			}
			return null;
		}
	}
	[HarmonyPatch(typeof(StartingPerkList), "InitStartingPerk")]
	internal static class StartingPerkRegistrationPatch
	{
		private static void Postfix()
		{
			PerkRegistry.EnsureRegistered();
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "OpenUI")]
	internal static class PerkPickerPatch
	{
		private static void Prefix(PerkUIController __instance)
		{
			PerkPickerScroll.BeforeOpen(__instance);
			FreeBuildMode.BeforeOpen(__instance);
		}

		[HarmonyPriority(0)]
		private static void Postfix(PerkUIController __instance)
		{
			PerkRegistry.EnsurePicker(__instance);
			FreeBuildMode.AfterOpen(__instance);
			PrecisionBayMode.AfterPickerOpen(__instance);
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "OnChange")]
	internal static class FreeBuildLimitPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix(PerkUIController __instance)
		{
			FreeBuildMode.ApplyLimits(__instance);
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "SelectPerk")]
	internal static class PerkSelectedOrderPatch
	{
		private static void Prefix(PerkUIController __instance)
		{
			PerkPickerScroll.Capture(__instance);
		}

		[HarmonyPriority(0)]
		private static void Postfix(PerkUIController __instance, StartingPerkElement perk)
		{
			PerkRegistry.AfterPickerMove(__instance, perk, selected: true);
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "DeselectPerk")]
	internal static class PerkDeselectedOrderPatch
	{
		private static void Prefix(PerkUIController __instance)
		{
			PerkPickerScroll.Capture(__instance);
		}

		[HarmonyPriority(0)]
		private static void Postfix(PerkUIController __instance, StartingPerkElement perk)
		{
			PerkRegistry.AfterPickerMove(__instance, perk, selected: false);
		}
	}
	[HarmonyPatch(typeof(StartingPerkElement), "Start")]
	internal static class PerkElementIconPatch
	{
		private static void Postfix(StartingPerkElement __instance)
		{
			PerkDefinition perkDefinition = PerkRegistry.Find((__instance != null) ? __instance.id : null);
			if (perkDefinition != null && !((Object)(object)__instance.icon == (Object)null))
			{
				Sprite val = IconLoader.Get(perkDefinition);
				if ((Object)(object)val != (Object)null)
				{
					__instance.icon.sprite = val;
				}
			}
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "GetLocalizedDisplayName")]
	internal static class PerkNamePatch
	{
		private static void Postfix(StartingPerk __instance, ref string __result)
		{
			PerkDefinition perkDefinition = PerkRegistry.Find((__instance != null) ? __instance.id : null);
			if (perkDefinition != null)
			{
				__result = perkDefinition.Name;
			}
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "GetLocalizedDescription")]
	internal static class PerkDescriptionPatch
	{
		private static void Postfix(StartingPerk __instance, ref string __result)
		{
			PerkDefinition perkDefinition = PerkRegistry.Find((__instance != null) ? __instance.id : null);
			if (perkDefinition != null)
			{
				__result = perkDefinition.Description;
			}
		}
	}
	internal static class PrecisionBayMode
	{
		internal const string SaveKey = "xiaowo_trade_precision_bay_enabled";

		internal const string PickerToggleName = "XIAOWO_PrecisionBayToggle";

		internal const string PauseButtonName = "XIAOWO_ModSettingsButton";

		internal const string WindowId = "xiaowo_save_mod_settings";

		private static GameObject _pickerToggleObject;

		private static Toggle _pickerToggle;

		private static UnityAction<bool> _pickerChangedAction;

		private static bool _newGameEnabled = true;

		private static GameObject _pauseButtonObject;

		private static Button _pauseButton;

		private static UnityAction _openPauseAction;

		private static Action<bool> _saveToggleAction;

		private static Action _manageModsAction;

		private static Action _closeAction;

		private static EscapeUIManager _escapeManager;

		private static int _sceneHandle = int.MinValue;

		private static int _managerSearchCountdown;

		private static int _checkCountdown;

		private static string _lastFailure = string.Empty;

		private static string PickerText => ModLocalization.Select("精工扩舱", "Precision Bay Expansion");

		private static string ModSettingsText => ModLocalization.Select("MOD 设置", "MOD SETTINGS");

		private static string CurrentSaveToggleText => ModLocalization.Select("允许精工扩舱继续升级", "Allow further Precision Bay upgrades");

		private static string DescriptionText => ModLocalization.Select("此开关只影响当前存档。关闭后停止金属锭触发的新升级；已有等级、容量、物品和窗口缩放均会保留。", "This affects only the current save. Disabling stops new metal-ingot upgrades while preserving existing levels, capacity, items, and window resizing.");

		internal static bool IsUpgradeEnabled(PlayerStore store = null)
		{
			if (store == null)
			{
				store = PlayerStore.instance;
			}
			if (store == null || string.IsNullOrEmpty(store.runID))
			{
				return _newGameEnabled;
			}
			Dictionary<string, string> modData = store.modData;
			string a = default;
			if (modData == null || !modData.TryGetValue("xiaowo_trade_precision_bay_enabled", ref a))
			{
				return true;
			}
			return !string.Equals(a, "0", StringComparison.Ordinal);
		}

		internal static void PrepareNewGame()
		{
			_newGameEnabled = true;
			if (IsAlive((Object)(object)_pickerToggle))
			{
				_pickerToggle.SetIsOnWithoutNotify(true);
			}
		}

		internal static void AfterPickerOpen(PerkUIController ui)
		{
			EnsurePickerToggle(ui);
		}

		internal static void ApplyNewGameSelection(PlayerStore store)
		{
			if (store != null)
			{
				SetStoredState(store, _newGameEnabled);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Precision Bay Expansion new-save state: " + (_newGameEnabled ? "enabled" : "disabled"));
				}
			}
		}

		internal static bool SetForCurrentSave(bool enabled, bool saveImmediately)
		{
			PlayerStore instance = PlayerStore.instance;
			if (instance == null || string.IsNullOrEmpty(instance.runID))
			{
				return false;
			}
			SetStoredState(instance, enabled);
			if (saveImmediately)
			{
				instance.SaveGame();
			}
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Precision Bay Expansion for current save: " + (enabled ? "enabled" : "disabled"));
			}
			return true;
		}

		internal static void Tick()
		{
			//IL_0018: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			if (_checkCountdown-- > 0)
			{
				return;
			}
			_checkCountdown = 15;
			try
			{
				Scene activeScene = SceneManager.GetActiveScene();
				int handle = activeScene.handle;
				if (_sceneHandle != handle)
				{
					_sceneHandle = handle;
					_escapeManager = null;
					_managerSearchCountdown = 0;
				}
				if (!IsAlive((Object)(object)_escapeManager))
				{
					if (_managerSearchCountdown-- > 0)
					{
						return;
					}
					_managerSearchCountdown = 120;
					_escapeManager = Object.FindObjectOfType<EscapeUIManager>();
				}
				EscapeUIManager escapeManager = _escapeManager;
				if (IsAlive((Object)(object)((escapeManager != null) ? escapeManager.menuPanel : null)) && escapeManager.menuPanel.activeInHierarchy)
				{
					EnsurePauseButton(escapeManager);
					_lastFailure = string.Empty;
				}
			}
			catch (Exception ex)
			{
				string text = ex.GetType().Name + ": " + ex.Message;
				if (!string.Equals(text, _lastFailure, StringComparison.Ordinal))
				{
					_lastFailure = text;
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("Save MOD settings button could not be created: " + text);
					}
				}
			}
		}

		internal static void EnsurePauseButton(EscapeUIManager manager)
		{
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f8: Expected Obj, but got Unknown
			if (!IsAlive((Object)(object)((manager != null) ? manager.menuPanel : null)))
			{
				return;
			}
			GameObject val = FindNativeButton(manager.menuPanel.transform, "OnOption");
			if (!IsAlive((Object)(object)val))
			{
				throw new InvalidOperationException("The native pause-menu option button is unavailable.");
			}
			Transform parent = val.transform.parent;
			if (!IsAlive((Object)(object)parent))
			{
				throw new InvalidOperationException("The pause-menu button parent is unavailable.");
			}
			if (!IsAlive((Object)(object)_pauseButtonObject) || (Object)(object)_pauseButtonObject.transform.parent != (Object)(object)parent)
			{
				if (IsAlive((Object)(object)_pauseButtonObject))
				{
					Object.Destroy((Object)(object)_pauseButtonObject);
				}
				_pauseButtonObject = Object.Instantiate<GameObject>(val, parent, false);
				((Object)_pauseButtonObject).name = "XIAOWO_ModSettingsButton";
				_pauseButton = _pauseButtonObject.GetComponent<Button>() ?? _pauseButtonObject.GetComponentInChildren<Button>(true);
				if (!IsAlive((Object)(object)_pauseButton))
				{
					throw new InvalidOperationException("The cloned MOD settings button is incomplete.");
				}
				_pauseButton.onClick = new ButtonClickedEvent();
				if (_openPauseAction == null)
				{
					_openPauseAction = UnityAction.op_Implicit((Action)OpenPauseSettings);
				}
				((UnityEvent)_pauseButton.onClick).AddListener(_openPauseAction);
			}
			_escapeManager = manager;
			TextMeshProUGUI componentInChildren = _pauseButtonObject.GetComponentInChildren<TextMeshProUGUI>(true);
			if (IsAlive((Object)(object)componentInChildren))
			{
				((TMP_Text)componentInChildren).text = ModSettingsText;
			}
			_pauseButtonObject.transform.SetSiblingIndex(Math.Min(parent.childCount - 1, val.transform.GetSiblingIndex() + 1));
			((Selectable)_pauseButton).interactable = true;
			_pauseButtonObject.SetActive(true);
		}

		internal static string BuildStatus()
		{
			//IL_016a: Unknown result type (might be due to invalid IL or missing references)
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_0200: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_0240: Unknown result type (might be due to invalid IL or missing references)
			//IL_024e: Unknown result type (might be due to invalid IL or missing references)
			//IL_025a: Unknown result type (might be due to invalid IL or missing references)
			PlayerStore instance = PlayerStore.instance;
			CustomUIManager instance2 = CustomUIManager.Instance;
			CustomUIWindow val = (IsAlive((Object)(object)instance2) ? instance2.GetWindow("xiaowo_save_mod_settings") : null);
			TextMeshProUGUI val2 = (IsAlive((Object)(object)_pickerToggleObject) ? _pickerToggleObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			TextMeshProUGUI val3 = (IsAlive((Object)(object)_pauseButtonObject) ? _pauseButtonObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			RectTransform val4 = (IsAlive((Object)(object)_pickerToggleObject) ? _pickerToggleObject.GetComponent<RectTransform>() : null);
			object obj;
			if (!IsAlive((Object)(object)_pickerToggleObject))
			{
				obj = null;
			}
			else
			{
				Transform parent = _pickerToggleObject.transform.parent;
				obj = ((parent != null) ? parent.Find("XIAOWO_FreeBuildToggle") : null);
			}
			Transform val5 = (Transform)obj;
			RectTransform val6 = (IsAlive((Object)(object)val5) ? ((Component)val5).GetComponent<RectTransform>() : null);
			return "PRECISION_BAY_MODE\nsaveKey=xiaowo_trade_precision_bay_enabled\nrun=" + ((instance != null) ? instance.runID : null) + "\nstored=" + HasStoredState(instance) + "\nenabled=" + IsUpgradeEnabled(instance) + "\npickerPresent=" + IsAlive((Object)(object)_pickerToggleObject) + "\npickerText=" + (((val2 != null) ? ((TMP_Text)val2).text : null) ?? "missing") + "\npickerX=" + (IsAlive((Object)(object)val4) ? val4.anchoredPosition.x : float.NaN) + "\npickerY=" + (IsAlive((Object)(object)val4) ? val4.anchoredPosition.y : float.NaN) + "\nfreeBuildX=" + (IsAlive((Object)(object)val6) ? val6.anchoredPosition.x : float.NaN) + "\nfreeBuildY=" + (IsAlive((Object)(object)val6) ? val6.anchoredPosition.y : float.NaN) + "\npickerRightOfFreeBuild=" + (IsAlive((Object)(object)val4) && IsAlive((Object)(object)val6) && val4.anchoredPosition.x > val6.anchoredPosition.x && Math.Abs(val4.anchoredPosition.y - val6.anchoredPosition.y) < 1f) + "\npauseButtonPresent=" + IsAlive((Object)(object)_pauseButtonObject) + "\npauseButtonText=" + (((val3 != null) ? ((TMP_Text)val3).text : null) ?? "missing") + "\nwindowOpen=" + (val != null && val.IsVisible);
		}

		internal static void RefreshLocalizedText()
		{
			TextMeshProUGUI val = (IsAlive((Object)(object)_pickerToggleObject) ? _pickerToggleObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			if (IsAlive((Object)(object)val))
			{
				((TMP_Text)val).text = PickerText;
			}
			TextMeshProUGUI val2 = (IsAlive((Object)(object)_pauseButtonObject) ? _pauseButtonObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			if (IsAlive((Object)(object)val2))
			{
				((TMP_Text)val2).text = ModSettingsText;
			}
		}

		internal static void OpenPauseSettingsForTest()
		{
			OpenPauseSettings();
		}

		internal static void Dispose()
		{
			CustomUIManager instance = CustomUIManager.Instance;
			if (IsAlive((Object)(object)instance))
			{
				instance.CloseWindow("xiaowo_save_mod_settings");
			}
			if (IsAlive((Object)(object)_pickerToggleObject))
			{
				Object.Destroy((Object)(object)_pickerToggleObject);
			}
			if (IsAlive((Object)(object)_pauseButtonObject))
			{
				Object.Destroy((Object)(object)_pauseButtonObject);
			}
			_pickerToggleObject = null;
			_pickerToggle = null;
			_pickerChangedAction = null;
			_pauseButtonObject = null;
			_pauseButton = null;
			_openPauseAction = null;
			_saveToggleAction = null;
			_closeAction = null;
			_escapeManager = null;
			_sceneHandle = int.MinValue;
			_managerSearchCountdown = 0;
			_newGameEnabled = true;
			_checkCountdown = 0;
			_lastFailure = string.Empty;
		}

		private static void EnsurePickerToggle(PerkUIController ui)
		{
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Expected Obj, but got Unknown
			object obj;
			if (ui == null)
			{
				obj = null;
			}
			else
			{
				Button startButton = ui.startButton;
				obj = ((startButton != null) ? ((Component)startButton).GetComponent<RectTransform>() : null);
			}
			RectTransform val = (RectTransform)obj;
			Transform val2 = ((val != null) ? ((Transform)val).parent : null);
			if (!IsAlive((Object)(object)val) || !IsAlive((Object)(object)val2))
			{
				return;
			}
			if (!IsAlive((Object)(object)_pickerToggleObject) || (Object)(object)_pickerToggleObject.transform.parent != (Object)(object)val2)
			{
				if (IsAlive((Object)(object)_pickerToggleObject))
				{
					Object.Destroy((Object)(object)_pickerToggleObject);
				}
				CustomUIManager instance = CustomUIManager.Instance;
				GameObject val3 = ((instance != null) ? instance.togglePrefab : null);
				if (!IsAlive((Object)(object)val3))
				{
					return;
				}
				_pickerToggleObject = Object.Instantiate<GameObject>(val3, val2, false);
				((Object)_pickerToggleObject).name = "XIAOWO_PrecisionBayToggle";
				_pickerToggle = _pickerToggleObject.GetComponent<Toggle>() ?? _pickerToggleObject.GetComponentInChildren<Toggle>(true);
				RectTransform component = _pickerToggleObject.GetComponent<RectTransform>();
				TextMeshProUGUI componentInChildren = _pickerToggleObject.GetComponentInChildren<TextMeshProUGUI>(true);
				if (!IsAlive((Object)(object)_pickerToggle) || !IsAlive((Object)(object)component) || !IsAlive((Object)(object)componentInChildren))
				{
					Object.Destroy((Object)(object)_pickerToggleObject);
					_pickerToggleObject = null;
					_pickerToggle = null;
					return;
				}
				((TMP_Text)componentInChildren).fontSize = Math.Min(((TMP_Text)componentInChildren).fontSize, 18f);
				((TMP_Text)componentInChildren).enableWordWrapping = false;
				((TMP_Text)componentInChildren).overflowMode = (TextOverflowModes)0;
				_pickerToggle.onValueChanged = new ToggleEvent();
				if (_pickerChangedAction == null)
				{
					_pickerChangedAction = UnityAction<bool>.op_Implicit((Action<bool>)((bool enabled) =>
					{
						_newGameEnabled = enabled;
					}));
				}
				((UnityEvent<bool>)(object)_pickerToggle.onValueChanged).AddListener(_pickerChangedAction);
			}
			FreeBuildMode.ApplyPickerToggleLayout(_pickerToggleObject.GetComponent<RectTransform>(), val, placeOnRight: true);
			TextMeshProUGUI componentInChildren2 = _pickerToggleObject.GetComponentInChildren<TextMeshProUGUI>(true);
			if (IsAlive((Object)(object)componentInChildren2))
			{
				((TMP_Text)componentInChildren2).text = PickerText;
			}
			_pickerToggle.SetIsOnWithoutNotify(_newGameEnabled);
			_pickerToggleObject.SetActive(true);
		}

		private static void OpenPauseSettings()
		{
			try
			{
				EscapeUIManager escapeManager = _escapeManager;
				if (escapeManager != null)
				{
					escapeManager.OnAnyClick();
				}
				CustomUIManager instance = CustomUIManager.Instance;
				if (!IsAlive((Object)(object)instance))
				{
					throw new InvalidOperationException("Custom UI manager is unavailable.");
				}
				instance.CloseWindow("xiaowo_save_mod_settings");
				CustomUIBuilder val = CustomUIManager.Window("xiaowo_save_mod_settings", ModSettingsText, "overlay").SetSize(600f, 260f).Center()
					.SetCloseOnEscape(true)
					.SetDraggable(false);
				val.AddLabel(DescriptionText, "summary");
				val.Last.SetPreferredSize(560f, 72f);
				if ((Object)(object)val.Last.text != (Object)null)
				{
					((TMP_Text)val.Last.text).fontSize = 16f;
					((TMP_Text)val.Last.text).enableWordWrapping = true;
				}
				if (_saveToggleAction == null)
				{
					_saveToggleAction = DelegateSupport.ConvertDelegate<Action<bool>>((Delegate)(Action<bool>)((bool enabled) =>
					{
						SetForCurrentSave(enabled, saveImmediately: true);
					}));
				}
				val.AddToggle(CurrentSaveToggleText, IsUpgradeEnabled(), _saveToggleAction, "precision_bay_enabled");
				val.Last.SetPreferredSize(540f, 42f);
				if (_manageModsAction == null)
				{
					_manageModsAction = DelegateSupport.ConvertDelegate<Action>((Delegate)(Action)(() =>
					{
						CloseWindow();
						ModManagerUI.OpenInGame();
					}));
				}
				val.AddButton(ModLocalization.Select("管理 MOD", "MANAGE MODS"), _manageModsAction, "manage_mods");
				val.Last.SetPreferredSize(220f, 42f);
				if (_closeAction == null)
				{
					_closeAction = DelegateSupport.ConvertDelegate<Action>((Delegate)new Action(CloseWindow));
				}
				val.AddButton(ModLocalization.Select("关闭", "CLOSE"), _closeAction, "close");
				val.Last.SetPreferredSize(180f, 42f);
				val.Show();
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("Save MOD settings window could not be opened: " + ex);
				}
			}
		}

		private static void CloseWindow()
		{
			CustomUIManager instance = CustomUIManager.Instance;
			if (IsAlive((Object)(object)instance))
			{
				instance.CloseWindow("xiaowo_save_mod_settings");
			}
		}

		private static GameObject FindNativeButton(Transform root, string methodName)
		{
			if (!IsAlive((Object)(object)root))
			{
				return null;
			}
			foreach (Button componentsInChild in ((Component)root).gameObject.GetComponentsInChildren<Button>(true))
			{
				if (!IsAlive((Object)(object)componentsInChild) || string.Equals(((Object)((Component)componentsInChild).gameObject).name, "XIAOWO_ModSettingsButton", StringComparison.Ordinal))
				{
					continue;
				}
				for (int i = 0; i < ((UnityEventBase)componentsInChild.onClick).GetPersistentEventCount(); i++)
				{
					if (string.Equals(((UnityEventBase)componentsInChild.onClick).GetPersistentMethodName(i), methodName, StringComparison.Ordinal))
					{
						return ((Component)componentsInChild).gameObject;
					}
				}
			}
			return null;
		}

		private static void SetStoredState(PlayerStore store, bool enabled)
		{
			if (store.modData == null)
			{
				store.modData = new Dictionary<string, string>();
			}
			store.modData["xiaowo_trade_precision_bay_enabled"] = (enabled ? "1" : "0");
		}

		private static bool HasStoredState(PlayerStore store)
		{
			if (((store != null) ? store.modData : null) != null)
			{
				return store.modData.ContainsKey("xiaowo_trade_precision_bay_enabled");
			}
			return false;
		}

		private static bool IsAlive(Object value)
		{
			if (value != (Object)null)
			{
				return Object.op_Implicit(value);
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(MainMenuUIController), "OnNewGameClick")]
	internal static class PrecisionBayPrepareNewGamePatch
	{
		private static void Prefix()
		{
			PrecisionBayMode.PrepareNewGame();
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "InitialSave")]
	internal static class PrecisionBayInitialSavePatch
	{
		private static void Prefix(PlayerStore __instance)
		{
			PrecisionBayMode.ApplyNewGameSelection(__instance);
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "StartNewGame")]
	internal static class PrecisionBayStartNewGamePatch
	{
		private static void Postfix(PlayerStore __instance)
		{
			PrecisionBayMode.ApplyNewGameSelection(__instance);
		}
	}
	[HarmonyPatch(typeof(EscapeUIManager), "OpenEscapeMenu")]
	internal static class PrecisionBayPauseMenuPatch
	{
		private static void Postfix(EscapeUIManager __instance)
		{
			PrecisionBayMode.EnsurePauseButton(__instance);
		}
	}
	[HarmonyPatch(typeof(EscapeUIManager), "ToggleEscapeMenu")]
	internal static class PrecisionBayPauseTogglePatch
	{
		private static void Postfix(EscapeUIManager __instance)
		{
			if (__instance != null)
			{
				GameObject menuPanel = __instance.menuPanel;
				if (((menuPanel != null) ? new bool?(menuPanel.activeInHierarchy) : ((bool?)null)) == true)
				{
					PrecisionBayMode.EnsurePauseButton(__instance);
				}
			}
		}
	}
	internal struct RuntimePollGate
	{
		private long _next;

		internal bool Due(int intervalMilliseconds)
		{
			long tickCount = Environment.TickCount64;
			if (tickCount < _next)
			{
				return false;
			}
			_next = tickCount + intervalMilliseconds;
			return true;
		}

		internal void Reset()
		{
			_next = 0L;
		}
	}
	internal sealed class SaveBackupPolicy
	{
		public bool Enabled { get; set; } = true;

		public int Interval { get; set; } = 7;

		public int Keep { get; set; } = 20;

		public void Validate()
		{
			if (Interval < 1 || Interval > 30 || Keep < 5 || Keep > 100)
			{
				throw new ArgumentException("备份间隔须为 1～30 天，保留数量须为 5～100 份。");
			}
		}
	}
	internal sealed class SaveDescription
	{
		public string RunId { get; set; }

		public int Slot { get; set; }

		public int Day { get; set; }

		public int Cash { get; set; }

		public int StartType { get; set; }

		public string Name { get; set; }

		public string Version { get; set; }

		public string Locale { get; set; }

		internal static SaveDescription Read(byte[] bytes)
		{
			if (bytes.Length == 0 || bytes.Length > 67108864)
			{
				throw new InvalidDataException("存档为空或超过 64 MB。");
			}
			string text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes).TrimStart('\ufeff');
			StringBuilder stringBuilder = new StringBuilder(text.Length);
			bool flag = false;
			bool flag2 = false;
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				if (flag)
				{
					if (!flag2 && c < ' ')
					{
						StringBuilder stringBuilder2 = stringBuilder.Append("\\u");
						int num = c;
						stringBuilder2.Append(num.ToString("x4"));
						continue;
					}
					if (!flag2 && c == '\\' && i + 1 < text.Length && "\"\\/bfnrtu".IndexOf(text[i + 1]) < 0)
					{
						stringBuilder.Append("\\\\");
						continue;
					}
					stringBuilder.Append(c);
					if (flag2)
					{
						flag2 = false;
						continue;
					}
					switch (c)
					{
					case '\\':
						flag2 = true;
						break;
					case '"':
						flag = false;
						break;
					}
					continue;
				}
				if (c == '"')
				{
					flag = true;
					stringBuilder.Append(c);
					continue;
				}
				if (c == '-' || char.IsDigit(c))
				{
					int j;
					for (j = i + 1; j < text.Length && (char.IsDigit(text[j]) || text[j] == '.'); j++)
					{
					}
					int k;
					for (k = j; k < text.Length && char.IsWhiteSpace(text[k]); k++)
					{
					}
					if (k < text.Length && text[k] == ':')
					{
						stringBuilder.Append('"').Append(text, i, j - i).Append('"');
						i = j - 1;
						continue;
					}
				}
				stringBuilder.Append(c);
			}
			using (JsonDocument jsonDocument = JsonDocument.Parse(stringBuilder.ToString(), new JsonDocumentOptions
			{
				MaxDepth = 256
			}))
			{
				JsonElement rootElement = jsonDocument.RootElement;
				JsonElement property = rootElement.GetProperty("playerStore").GetProperty("value");
				JsonElement property2 = rootElement.GetProperty("storeStation").GetProperty("value");
				SaveDescription saveDescription = new SaveDescription
				{
					RunId = S(property, "runID"),
					Slot = property.GetProperty("saveSlotId").GetInt32(),
					Day = property2.GetProperty("dayCounter").GetInt32(),
					Cash = property.GetProperty("playerCash").GetInt32(),
					Version = S(property, "saveVersion"),
					Name = S(property, "startName"),
					Locale = S(property, "startLocale")
				};
				if (property.TryGetProperty("newGameData", out var value))
				{
					if (value.TryGetProperty("startType", out var value2))
					{
						saveDescription.StartType = value2.GetInt32();
					}
					if (saveDescription.Name.Length == 0)
					{
						saveDescription.Name = S(value, "startName");
					}
				}
				if (property.TryGetProperty("startType", out var value3))
				{
					saveDescription.StartType = value3.GetInt32();
				}
				if (string.IsNullOrWhiteSpace(saveDescription.RunId) || saveDescription.Day < 0 || saveDescription.Slot < 0)
				{
					throw new InvalidDataException("存档缺少有效的游戏记录。");
				}
				if (saveDescription.Name.Length == 0)
				{
					saveDescription.Name = "#" + saveDescription.Slot;
				}
				return saveDescription;
			}
			static string S(JsonElement jsonElement, string key)
			{
				if (!jsonElement.TryGetProperty(key, out var value4) || value4.ValueKind != JsonValueKind.String)
				{
					return "";
				}
				return value4.GetString();
			}
		}

		internal static byte[] RebindSlot(byte[] bytes, int slot)
		{
			if (slot < 0)
			{
				throw new ArgumentOutOfRangeException("slot");
			}
			SaveDescription saveDescription = Read(bytes);
			string text = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes);
			int i;
			for (i = 0; i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == '\ufeff'); i++)
			{
			}
			(int, int) tuple = Property(text, Property(text, Property(text, i, "playerStore").start, "value").start, "saveSlotId");
			if (!int.TryParse(text.Substring(tuple.Item1, tuple.Item2 - tuple.Item1), out var result) || result != saveDescription.Slot)
			{
				throw new InvalidDataException("存档槽位字段无效，未导入。");
			}
			int byteCount = Encoding.UTF8.GetByteCount(text.AsSpan(0, tuple.Item1));
			int byteCount2 = Encoding.UTF8.GetByteCount(text.AsSpan(0, tuple.Item2));
			byte[] bytes2 = Encoding.ASCII.GetBytes(slot.ToString(CultureInfo.InvariantCulture));
			byte[] array = new byte[bytes.Length - (byteCount2 - byteCount) + bytes2.Length];
			Buffer.BlockCopy(bytes, 0, array, 0, byteCount);
			Buffer.BlockCopy(bytes2, 0, array, byteCount, bytes2.Length);
			Buffer.BlockCopy(bytes, byteCount2, array, byteCount + bytes2.Length, bytes.Length - byteCount2);
			if (Read(array).Slot != slot)
			{
				throw new InvalidDataException("导入槽位校验失败。");
			}
			return array;
		}

		private static (int start, int end) Property(string text, int start, string name)
		{
			if (text[start] != '{')
			{
				throw new InvalidDataException("无效的存档对象。");
			}
			(int, int) result = (-1, -1);
			int i = start + 1;
			while (i < text.Length)
			{
				for (; i < text.Length && (char.IsWhiteSpace(text[i]) || text[i] == ','); i++)
				{
				}
				if (text[i] == '}')
				{
					break;
				}
				int num = i;
				SkipValue(text, ref i);
				string text2 = JsonSerializer.Deserialize<string>(text.Substring(num, i - num));
				for (; char.IsWhiteSpace(text[i]); i++)
				{
				}
				if (text[i++] != ':')
				{
					throw new InvalidDataException("无效的存档字段。");
				}
				for (; char.IsWhiteSpace(text[i]); i++)
				{
				}
				int item = i;
				SkipValue(text, ref i);
				if (!(text2 != name))
				{
					if (result.Item1 >= 0)
					{
						throw new InvalidDataException("存档包含重复槽位字段。");
					}
					result = (item, i);
				}
			}
			if (result.Item1 < 0)
			{
				throw new InvalidDataException("存档缺少字段：" + name);
			}
			return result;
		}

		private static void SkipValue(string text, ref int i)
		{
			bool flag = false;
			int num = 0;
			while (i < text.Length)
			{
				char c = text[i];
				if (flag)
				{
					switch (c)
					{
					case '\\':
						i++;
						break;
					case '"':
						flag = false;
						if (num == 0)
						{
							i++;
							return;
						}
						break;
					}
				}
				else
				{
					switch (c)
					{
					case '"':
						flag = true;
						break;
					case '[':
					case '{':
						num++;
						break;
					case ']':
					case '}':
						if (num == 0)
						{
							return;
						}
						if (--num == 0)
						{
							i++;
							return;
						}
						break;
					default:
						if (num == 0 && (c == ',' || c == ':' || char.IsWhiteSpace(c)))
						{
							return;
						}
						break;
					}
				}
				i++;
			}
		}
	}
	internal sealed class SaveBackupRecord
	{
		public int Schema { get; set; } = 1;

		public string File { get; set; }

		public string Hash { get; set; }

		public string Group { get; set; }

		public bool Manual { get; set; }

		public DateTime CreatedUtc { get; set; }

		public SaveDescription Save { get; set; }

		public string ModVersion { get; set; }

		public string[] Mods { get; set; } = Array.Empty<string>();
	}
	internal sealed class SaveArchive
	{
		internal string Root { get; }

		internal SaveArchive(string root)
		{
			Root = Path.GetFullPath(root);
		}

		internal static string Hash(byte[] bytes)
		{
			return Convert.ToHexString(SHA256.HashData(bytes));
		}

		internal static byte[] ReadSaveFile(string path)
		{
			using FileStream fileStream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			if (fileStream.Length == 0L || fileStream.Length > 67108864)
			{
				throw new InvalidDataException("存档为空或超过 64 MB。");
			}
			byte[] array = new byte[(int)fileStream.Length];
			int num;
			for (int i = 0; i < array.Length; i += num)
			{
				num = fileStream.Read(array, i, array.Length - i);
				if (num == 0)
				{
					throw new EndOfStreamException("存档读取不完整。");
				}
			}
			return array;
		}

		internal static string Group(int slot, string run)
		{
			return slot + "-" + Hash(Encoding.UTF8.GetBytes(run)).Substring(0, 16);
		}

		internal string PolicyPath(string group)
		{
			return Path.Combine(Root, "settings", group + ".json");
		}

		internal SaveBackupPolicy Policy(string group)
		{
			string path = PolicyPath(group);
			if (!File.Exists(path))
			{
				return new SaveBackupPolicy();
			}
			SaveBackupPolicy? saveBackupPolicy = JsonSerializer.Deserialize<SaveBackupPolicy>(File.ReadAllText(path)) ?? throw new InvalidDataException("备份设置损坏。");
			saveBackupPolicy.Validate();
			return saveBackupPolicy;
		}

		internal void SetPolicy(string group, SaveBackupPolicy policy)
		{
			policy.Validate();
			AtomicWrite(PolicyPath(group), JsonSerializer.SerializeToUtf8Bytes(policy), overwrite: true);
		}

		internal List<SaveBackupRecord> List(string group = null)
		{
			if (!Directory.Exists(Root))
			{
				return new List<SaveBackupRecord>();
			}
			List<SaveBackupRecord> list = new List<SaveBackupRecord>();
			foreach (string item in Directory.EnumerateFiles(Root, "*.zip", SearchOption.TopDirectoryOnly))
			{
				try
				{
					using ZipArchive zip = ZipFile.OpenRead(item);
					SaveBackupRecord saveBackupRecord = ZipRecord(zip);
					saveBackupRecord.File = Path.GetFileName(item);
					if (group == null || saveBackupRecord.Group == group)
					{
						list.Add(saveBackupRecord);
					}
				}
				catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is JsonException)
				{
				}
			}
			foreach (string item2 in Directory.EnumerateFiles(Root, "*.json", SearchOption.TopDirectoryOnly))
			{
				try
				{
					SaveBackupRecord saveBackupRecord2 = JsonSerializer.Deserialize<SaveBackupRecord>(File.ReadAllText(item2));
					if (saveBackupRecord2 != null && saveBackupRecord2.Schema == 1 && saveBackupRecord2.Save != null && saveBackupRecord2.Group != null && !string.IsNullOrEmpty(saveBackupRecord2.File) && Path.GetFileName(saveBackupRecord2.File) == saveBackupRecord2.File && saveBackupRecord2.File.EndsWith(".es3", StringComparison.Ordinal) && Path.GetFileNameWithoutExtension(item2) == Path.GetFileNameWithoutExtension(saveBackupRecord2.File) && (group == null || saveBackupRecord2.Group == group) && File.Exists(Path.Combine(Root, saveBackupRecord2.File)))
					{
						list.Add(saveBackupRecord2);
					}
				}
				catch (IOException)
				{
				}
				catch (JsonException)
				{
				}
			}
			return list.OrderByDescending((SaveBackupRecord r) => r.CreatedUtc).ToList();
		}

		internal bool Due(string group, int day, SaveBackupPolicy policy)
		{
			if (!policy.Enabled)
			{
				return false;
			}
			List<SaveBackupRecord> list = (from r in List(@group)
				where !r.Manual
				select r).ToList();
			if (list.Count != 0)
			{
				return day >= (long)list.Max((SaveBackupRecord r) => r.Save.Day) + (long)policy.Interval;
			}
			return true;
		}

		internal SaveBackupRecord Capture(byte[] bytes, int slot, bool manual, string modVersion, string[] mods, string displayName = null)
		{
			SaveDescription saveDescription = SaveDescription.Read(bytes);
			saveDescription.Slot = slot;
			if (!string.IsNullOrWhiteSpace(displayName))
			{
				saveDescription.Name = displayName;
			}
			string text = Group(slot, saveDescription.RunId);
			SaveBackupPolicy saveBackupPolicy = Policy(text);
			if (!manual && !Due(text, saveDescription.Day, saveBackupPolicy))
			{
				return null;
			}
			Directory.CreateDirectory(Root);
			string text2 = "slot" + slot + "-day" + saveDescription.Day + "-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmssfff") + "-" + Guid.NewGuid().ToString("N").Substring(0, 8);
			SaveBackupRecord saveBackupRecord = new SaveBackupRecord
			{
				Schema = 2,
				File = text2 + ".zip",
				Hash = Hash(bytes),
				Group = text,
				Manual = manual,
				CreatedUtc = DateTime.UtcNow,
				Save = saveDescription,
				ModVersion = modVersion,
				Mods = mods
			};
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (ZipArchive zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
				{
					using (Stream stream = zipArchive.CreateEntry("save.es3", CompressionLevel.Optimal).Open())
					{
						stream.Write(bytes);
					}
					using Stream stream2 = zipArchive.CreateEntry("manifest.json", CompressionLevel.Optimal).Open();
					stream2.Write(JsonSerializer.SerializeToUtf8Bytes(saveBackupRecord));
				}
				AtomicWrite(Path.Combine(Root, saveBackupRecord.File), memoryStream.ToArray(), overwrite: false);
			}
			if (!manual)
			{
				foreach (SaveBackupRecord item in (from r in List(text)
					where !r.Manual
					select r).Skip(saveBackupPolicy.Keep))
				{
					string path = Path.Combine(Root, item.File);
					try
					{
						ReadBackup(item);
					}
					catch (Exception ex) when (ex is IOException || ex is InvalidDataException || ex is JsonException)
					{
						continue;
					}
					File.Delete(path);
					if (item.Schema == 1)
					{
						File.Delete(Path.ChangeExtension(path, ".json"));
					}
				}
			}
			return saveBackupRecord;
		}

		internal byte[] ReadBackup(SaveBackupRecord record)
		{
			if (record == null || string.IsNullOrEmpty(record.File) || Path.GetFileName(record.File) != record.File)
			{
				throw new InvalidDataException("无效的备份路径。");
			}
			string path = Path.Combine(Root, record.File);
			byte[] array = ((record.Schema == 2) ? ReadImport(path, out var _) : ReadSaveFile(path));
			if (Hash(array) != record.Hash)
			{
				throw new InvalidDataException("备份校验失败，文件可能已损坏。");
			}
			SaveDescription.Read(array);
			return array;
		}

		internal void RemoveBackup(SaveBackupRecord record)
		{
			ReadBackup(record);
			string text = Path.Combine(Root, "Deleted", Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(text);
			File.Move(Path.Combine(Root, record.File), Path.Combine(text, record.File));
			if (record.Schema == 1)
			{
				string path = Path.ChangeExtension(record.File, ".json");
				File.Move(Path.Combine(Root, path), Path.Combine(text, path));
			}
		}

		internal static byte[] ReadImport(string path, out SaveBackupRecord record)
		{
			record = null;
			if (!Path.GetExtension(path).Equals(".zip", StringComparison.OrdinalIgnoreCase))
			{
				byte[] array = ReadSaveFile(path);
				string path2 = Path.ChangeExtension(path, ".json");
				if (File.Exists(path2))
				{
					record = JsonSerializer.Deserialize<SaveBackupRecord>(ReadBounded(File.OpenRead(path2), 1048576));
					SaveBackupRecord obj = record;
					if (obj == null || obj.Schema != 1 || record.Save == null || Hash(array) != record.Hash)
					{
						throw new InvalidDataException("旧版备份校验失败。");
					}
				}
				SaveDescription.Read(array);
				return array;
			}
			if (new FileInfo(path).Length > 68157440)
			{
				throw new InvalidDataException("备份包过大。");
			}
			using ZipArchive zipArchive = ZipFile.OpenRead(path);
			record = ZipRecord(zipArchive);
			byte[] array2 = ReadEntry(zipArchive.GetEntry("save.es3"), 67108864);
			if (Hash(array2) != record.Hash)
			{
				throw new InvalidDataException("备份校验失败，文件可能已损坏。");
			}
			SaveDescription saveDescription = SaveDescription.Read(array2);
			if (saveDescription.RunId != record.Save.RunId || saveDescription.Day != record.Save.Day || saveDescription.Cash != record.Save.Cash)
			{
				throw new InvalidDataException("备份信息与存档内容不一致。");
			}
			record.File = Path.GetFileName(path);
			return array2;
		}

		private static SaveBackupRecord ZipRecord(ZipArchive zip)
		{
			if (zip.Entries.Count != 2 || zip.Entries.Count((ZipArchiveEntry e) => e.FullName == "save.es3") != 1 || zip.Entries.Count((ZipArchiveEntry e) => e.FullName == "manifest.json") != 1)
			{
				throw new InvalidDataException("不是有效的特性 MOD 存档备份包。");
			}
			SaveBackupRecord saveBackupRecord = JsonSerializer.Deserialize<SaveBackupRecord>(ReadEntry(zip.GetEntry("manifest.json"), 1048576));
			if (saveBackupRecord == null || saveBackupRecord.Schema != 2 || saveBackupRecord.Save == null || string.IsNullOrEmpty(saveBackupRecord.Group) || string.IsNullOrEmpty(saveBackupRecord.Hash))
			{
				throw new InvalidDataException("备份说明无效。");
			}
			return saveBackupRecord;
		}

		private static byte[] ReadEntry(ZipArchiveEntry entry, int maximum)
		{
			if (entry.Length <= 0 || entry.Length > maximum)
			{
				throw new InvalidDataException("备份内容大小无效。");
			}
			return ReadBounded(entry.Open(), maximum);
		}

		private static byte[] ReadBounded(Stream input, int maximum)
		{
			using (input)
			{
				using MemoryStream memoryStream = new MemoryStream();
				byte[] array = new byte[8192];
				int num;
				while ((num = input.Read(array, 0, array.Length)) > 0)
				{
					if (memoryStream.Length + num > maximum)
					{
						throw new InvalidDataException("备份内容过大。");
					}
					memoryStream.Write(array, 0, num);
				}
				return memoryStream.ToArray();
			}
		}

		internal static void AtomicWrite(string path, byte[] bytes, bool overwrite)
		{
			Directory.CreateDirectory(Path.GetDirectoryName(path));
			string text = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
			try
			{
				using (FileStream fileStream = new FileStream(text, FileMode.CreateNew, FileAccess.Write, FileShare.None))
				{
					fileStream.Write(bytes);
					fileStream.Flush(flushToDisk: true);
				}
				if (Hash(File.ReadAllBytes(text)) != Hash(bytes))
				{
					throw new IOException("文件写入校验失败。");
				}
				File.Move(text, path, overwrite);
			}
			finally
			{
				if (File.Exists(text))
				{
					File.Delete(text);
				}
			}
		}
	}
	internal static class SaveManager
	{
		private static int _pendingSlot = -1;

		private static int _pendingDay;

		private static int _delay;

		private static string _pendingRun;

		private static string _lastError;

		private static bool _manual;

		private static long _savedSequence;

		internal static SaveArchive Archive => new SaveArchive(Path.Combine(Application.persistentDataPath, "XIAOWOTradePerks-Backups"));

		internal static bool InMenu
		{
			get
			{
				//IL_0000: Unknown result type (might be due to invalid IL or missing references)
				//IL_0005: Unknown result type (might be due to invalid IL or missing references)
				Scene activeScene = SceneManager.GetActiveScene();
				return activeScene.name == "EmporiumMenu";
			}
		}

		internal static string[] Mods => (from m in MelonBase.RegisteredMelons
			where m.Info != null
			select m.Info.Name + " " + m.Info.Version into n
			orderby n
			select n).ToArray();

		internal static string PathFor(int slot)
		{
			return Path.Combine(Application.persistentDataPath, SaveFiles.Slot(slot));
		}

		internal static long WriteStamp(PlayerStore store)
		{
			try
			{
				return (store == null) ? 0 : File.GetLastWriteTimeUtc(PathFor(store.saveSlotId)).Ticks;
			}
			catch
			{
				return 0L;
			}
		}

		internal static void OnSaved(PlayerStore store, long before)
		{
			if (store == null || string.IsNullOrEmpty(store.runID))
			{
				return;
			}
			long num = WriteStamp(store);
			if (num != 0L && num != before)
			{
				_savedSequence++;
				if (!_manual)
				{
					_pendingSlot = store.saveSlotId;
					_pendingRun = store.runID;
					_pendingDay = StoreStation.GetDayCounter();
					_delay = 2;
				}
			}
		}

		internal static void Tick()
		{
			if (_pendingSlot < 0 || _manual || _delay-- > 0)
			{
				return;
			}
			int pendingSlot = _pendingSlot;
			_pendingSlot = -1;
			try
			{
				SaveArchive archive = Archive;
				string text = SaveArchive.Group(pendingSlot, _pendingRun);
				SaveBackupPolicy policy = archive.Policy(text);
				if (!archive.Due(text, _pendingDay, policy))
				{
					return;
				}
				byte[] bytes = SaveArchive.ReadSaveFile(PathFor(pendingSlot));
				SaveDescription saveDescription = SaveDescription.Read(bytes);
				if (saveDescription.RunId != _pendingRun || saveDescription.Day != _pendingDay)
				{
					throw new IOException("存档内容尚未与当前日期同步，本次不备份。");
				}
				SaveBackupRecord saveBackupRecord = archive.Capture(bytes, pendingSlot, manual: false, typeof(Core).Assembly.GetName().Version.ToString(3), Mods, DisplayName(saveDescription));
				if (saveBackupRecord != null)
				{
					Instance log = Core.Log;
					if (log != null)
					{
						log.Msg("自动备份已保存：" + saveBackupRecord.File);
					}
				}
				_lastError = null;
			}
			catch (Exception ex)
			{
				if (_lastError != ex.Message)
				{
					Instance log2 = Core.Log;
					if (log2 != null)
					{
						log2.Warning("自动备份未完成，原存档未修改：" + ex.Message);
					}
				}
				_lastError = ex.Message;
			}
		}

		internal static SaveBackupRecord BackupNow(int slot)
		{
			PlayerStore instance = PlayerStore.instance;
			if (!InMenu)
			{
				if (instance == null || instance.saveSlotId != slot)
				{
					throw new InvalidOperationException("游戏中只能备份当前存档。");
				}
				long savedSequence = _savedSequence;
				try
				{
					_manual = true;
					instance.SaveGame();
				}
				finally
				{
					_manual = false;
				}
				if (_savedSequence == savedSequence)
				{
					throw new IOException("游戏未完成新的写盘，未建立手动备份。");
				}
				SaveDescription saveDescription = SaveDescription.Read(SaveArchive.ReadSaveFile(PathFor(slot)));
				if (saveDescription.RunId != instance.runID || saveDescription.Day != StoreStation.GetDayCounter())
				{
					throw new IOException("本次保存未完成，未建立手动备份。");
				}
			}
			byte[] bytes = SaveArchive.ReadSaveFile(PathFor(slot));
			SaveDescription description = SaveDescription.Read(bytes);
			return Archive.Capture(bytes, slot, manual: true, typeof(Core).Assembly.GetName().Version.ToString(3), Mods, DisplayName(description));
		}

		internal static string DisplayName(SaveDescription description, string recorded = null)
		{
			string text = recorded ?? description.Name;
			if (!string.IsNullOrWhiteSpace(text) && !text.StartsWith("#", StringComparison.Ordinal))
			{
				return text;
			}
			return NewGameData.GetStartDisplayName((StartType)description.StartType);
		}

		internal static string DeleteSave(int slot)
		{
			if (!InMenu)
			{
				throw new InvalidOperationException("请先返回主菜单再删除存档。");
			}
			SaveBackupRecord saveBackupRecord = BackupNow(slot);
			SaveFiles.DeleteSlot(slot);
			SaveUIManager.InvalidateCache();
			if (File.Exists(PathFor(slot)))
			{
				throw new IOException("存档未能删除，安全备份已保留。");
			}
			return saveBackupRecord.File;
		}

		internal static List<SaveDescription> Saves()
		{
			List<SaveDescription> list = new List<SaveDescription>();
			foreach (string item in Directory.EnumerateFiles(Application.persistentDataPath, "save_*.es3"))
			{
				if (!int.TryParse(Path.GetFileNameWithoutExtension(item).Substring(5), out var result) || result < 0)
				{
					continue;
				}
				try
				{
					SaveDescription saveDescription = SaveDescription.Read(SaveArchive.ReadSaveFile(item));
					saveDescription.Slot = result;
					SavePreviewData val = SaveFiles.IndexLoad<SavePreviewData>("preview_" + result, (SavePreviewData)null);
					if (val != null && !string.IsNullOrEmpty(val.startName))
					{
						saveDescription.Name = val.startName;
					}
					saveDescription.Name = DisplayName(saveDescription);
					list.Add(saveDescription);
				}
				catch (Exception)
				{
				}
			}
			return list.OrderBy((SaveDescription s) => s.Slot).ToList();
		}

		internal static int Import(byte[] bytes, string displayName = null)
		{
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0084: Unknown result type (might be due to invalid IL or missing references)
			//IL_008e: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c0: Expected Obj, but got Unknown
			if (!InMenu)
			{
				throw new InvalidOperationException("请先返回主菜单，再导入或恢复存档。");
			}
			SaveDescription saveDescription = SaveDescription.Read(bytes);
			List<int> val = SaveFiles.IndexLoad<List<int>>("activeSaveSlotId", new List<int>());
			int num = SaveFiles.NextFreeSlotId(val);
			if (num < 0 || File.Exists(PathFor(num)))
			{
				throw new IOException("无法分配空闲存档槽位。");
			}
			SaveArchive.AtomicWrite(PathFor(num), SaveDescription.RebindSlot(bytes, num), overwrite: false);
			try
			{
				SavePreviewData val2 = new SavePreviewData
				{
					slotId = num,
					credit = saveDescription.Cash,
					dayCount = saveDescription.Day,
					lastSaveDateTime = DateTime.Now,
					startName = DisplayName(saveDescription, displayName),
					version = saveDescription.Version,
					startLocale = saveDescription.Locale,
					startType = (StartType)saveDescription.StartType
				};
				SaveFiles.IndexSave<SavePreviewData>("preview_" + num, val2);
				if (!val.Contains(num))
				{
					val.Add(num);
				}
				SaveFiles.IndexSave<List<int>>("activeSaveSlotId", val);
				SaveUIManager.InvalidateCache();
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("存档导入完成：原槽位 " + saveDescription.Slot + " -> 新槽位 " + num + "，第 " + saveDescription.Day + " 天。");
				}
				return num;
			}
			catch
			{
				SaveUIManager.InvalidateCache();
				throw new IOException("文件已导入槽位 " + num + "，但索引更新失败。请重新打开加载列表；原档未覆盖。");
			}
		}

		internal static void Dispose()
		{
			_pendingSlot = -1;
			_lastError = null;
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "SaveGame")]
	internal static class SaveManagerSavedPatch
	{
		[HarmonyPriority(800)]
		private static void Prefix(PlayerStore __instance, out long __state)
		{
			__state = SaveManager.WriteStamp(__instance);
		}

		[HarmonyPriority(0)]
		private static void Postfix(PlayerStore __instance, long __state)
		{
			SaveManager.OnSaved(__instance, __state);
		}
	}
	[HarmonyPatch(typeof(EscapeUIManager), "OpenEscapeMenu")]
	internal static class SaveManagerPausePatch
	{
		private static void Postfix(EscapeUIManager __instance)
		{
			SaveManagerUI.EnsurePause(__instance);
		}
	}
	internal static class SaveManagerUI
	{
		internal const string WindowId = "xiaowo_save_manager";

		private static GameObject _menuButton;

		private static GameObject _pauseButton;

		private static EscapeUIManager _escape;

		private static RectTransform _pauseFrame;

		private static float _pauseHeight;

		private static float _pauseAppliedHeight;

		private static int _selected = -1;

		private static RuntimePollGate _buttonPoll;

		private static RuntimePollGate _escapeSearchPoll;

		private static List<SaveDescription> _saves = new List<SaveDescription>();

		private static string _interval = "7";

		private static string _keep = "20";

		private static string _notice = "";

		private static bool _enabled;

		private static bool _confirm;

		private static byte[] _importBytes;

		private static string _importHash;

		private static string _importName;

		private static string _deleteLabel;

		private static Action _deleteAction;

		private static string _importWarning = "";

		private static readonly List<Action> Actions = new List<Action>();

		private static readonly List<Action<string>> Inputs = new List<Action<string>>();

		private static readonly List<Action<int>> Choices = new List<Action<int>>();

		private static readonly List<Action<bool>> Toggles = new List<Action<bool>>();

		private static SaveDescription Current
		{
			get
			{
				if (_selected < 0 || _selected >= _saves.Count)
				{
					return null;
				}
				return _saves[_selected];
			}
		}

		private static string Group
		{
			get
			{
				if (Current != null)
				{
					return SaveArchive.Group(Current.Slot, Current.RunId);
				}
				return null;
			}
		}

		internal static string T(string zh, string en)
		{
			return ModLocalization.Select(zh, en);
		}

		internal static void Tick()
		{
			SaveWindowResize.Tick();
			if (!_buttonPoll.Due(500))
			{
				return;
			}
			MainMenuUIController instance = MainMenuUIController.Instance;
			if (SaveManager.InMenu && (Object)(object)instance != (Object)null)
			{
				GameObject moddingButton = instance.moddingButton;
				Transform val = ((moddingButton != null) ? moddingButton.transform.parent : null);
				GameObject val2 = (((Object)(object)val == (Object)null) ? null : FindButton(val, "OnLoadGameClick"));
				if ((Object)(object)val2 != (Object)null)
				{
					EnsureButton(ref _menuButton, val2, "XIAOWOSaveManagerMenu", val2.transform.GetSiblingIndex() + 1);
				}
				return;
			}
			if ((Object)(object)_escape == (Object)null && _escapeSearchPoll.Due(1000))
			{
				_escape = Object.FindObjectOfType<EscapeUIManager>();
			}
			EscapeUIManager escape = _escape;
			if ((Object)(object)((escape != null) ? escape.menuPanel : null) != (Object)null && _escape.menuPanel.activeInHierarchy)
			{
				EnsurePause(_escape);
			}
		}

		internal static void EnsurePause(EscapeUIManager manager)
		{
			//IL_00c4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009b: Unknown result type (might be due to invalid IL or missing references)
			_escape = manager;
			if ((Object)(object)((manager != null) ? manager.menuPanel : null) == (Object)null)
			{
				return;
			}
			GameObject val = FindButton(manager.menuPanel.transform, "OnOption");
			if (!((Object)(object)val != (Object)null))
			{
				return;
			}
			EnsureButton(ref _pauseButton, val, "XIAOWOSaveManagerPause", val.transform.GetSiblingIndex() + 2);
			RectTransform component = ((Component)val.transform.parent).GetComponent<RectTransform>();
			if ((Object)(object)component != (Object)null && (Object)(object)((Component)component).GetComponent<VerticalLayoutGroup>() != (Object)null)
			{
				Rect rect;
				if ((Object)(object)_pauseFrame != (Object)(object)component)
				{
					_pauseFrame = component;
					rect = component.rect;
					_pauseHeight = rect.height;
				}
				LayoutRebuilder.ForceRebuildLayoutImmediate(component);
				_pauseAppliedHeight = Math.Max(_pauseHeight, LayoutUtility.GetPreferredHeight(component));
				rect = component.rect;
				if (Math.Abs(rect.height - _pauseAppliedHeight) > 0.1f)
				{
					component.SetSizeWithCurrentAnchors((Axis)1, _pauseAppliedHeight);
				}
			}
		}

		private static GameObject FindButton(Transform root, string method)
		{
			foreach (Button componentsInChild in ((Component)root).GetComponentsInChildren<Button>(true))
			{
				for (int i = 0; i < ((UnityEventBase)componentsInChild.onClick).GetPersistentEventCount(); i++)
				{
					if (((UnityEventBase)componentsInChild.onClick).GetPersistentMethodName(i) == method)
					{
						return ((Component)componentsInChild).gameObject;
					}
				}
			}
			return null;
		}

		private static void EnsureButton(ref GameObject target, GameObject source, string name, int index)
		{
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Expected Obj, but got Unknown
			if ((Object)(object)target == (Object)null || (Object)(object)target.transform.parent != (Object)(object)source.transform.parent)
			{
				if ((Object)(object)target != (Object)null)
				{
					Object.Destroy((Object)(object)target);
				}
				target = Object.Instantiate<GameObject>(source, source.transform.parent, false);
				((Object)target).name = name;
				Button val = target.GetComponent<Button>() ?? target.GetComponentInChildren<Button>(true);
				val.onClick = new ButtonClickedEvent();
				((UnityEvent)val.onClick).AddListener(UnityAction.op_Implicit((Action)Open));
				((Selectable)val).interactable = true;
			}
			target.SetActive(true);
			target.transform.SetSiblingIndex(Math.Min(index, target.transform.parent.childCount - 1));
			TextMeshProUGUI componentInChildren = target.GetComponentInChildren<TextMeshProUGUI>(true);
			if ((Object)(object)componentInChildren != (Object)null)
			{
				((TMP_Text)componentInChildren).text = T("存档管理", "SAVE MANAGER");
			}
		}

		internal static void Open()
		{
			try
			{
				int num;
				if (SaveManager.InMenu)
				{
					num = Current?.Slot ?? (-1);
				}
				else
				{
					PlayerStore instance = PlayerStore.instance;
					num = ((instance != null) ? instance.saveSlotId : (-1));
				}
				int slot = num;
				_saves = SaveManager.Saves();
				_selected = _saves.FindIndex((SaveDescription s) => s.Slot == slot);
				if (_selected < 0)
				{
					_selected = ((_saves.Count <= 0) ? (-1) : 0);
				}
				_notice = "";
				_importBytes = null;
				_confirm = false;
				_deleteAction = null;
				ReadPolicy();
				Build();
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("存档管理无法打开：" + ex.Message);
				}
			}
		}

		private static void ReadPolicy()
		{
			SaveBackupPolicy saveBackupPolicy = ((Group == null) ? new SaveBackupPolicy() : SaveManager.Archive.Policy(Group));
			_enabled = saveBackupPolicy.Enabled;
			_interval = saveBackupPolicy.Interval.ToString();
			_keep = saveBackupPolicy.Keep.ToString();
		}

		private static void Build()
		{
			//IL_0997: Unknown result type (might be due to invalid IL or missing references)
			//IL_09a1: Expected Obj, but got Unknown
			SaveWindowResize.Remember();
			CustomUIManager instance = CustomUIManager.Instance;
			if ((Object)(object)instance == (Object)null)
			{
				return;
			}
			instance.CloseWindow("xiaowo_save_manager");
			Actions.Clear();
			Inputs.Clear();
			Choices.Clear();
			Toggles.Clear();
			CustomUIBuilder val = CustomUIManager.Window("xiaowo_save_manager", T("存档管理", "SAVE MANAGER"), "overlay").SetSize(SaveWindowResize.Width, SaveWindowResize.Height).Center()
				.SetDraggable(true)
				.SetCloseOnEscape(false);
			val.AddLabel(_notice, "save_notice");
			Size(val, 900f, 58f);
			if (_deleteAction != null)
			{
				val.AddLabel(_deleteLabel, (string)null);
				Size(val, 900f, 90f);
				val.BeginRow(8f);
				Button(val, T("确认删除", "CONFIRM DELETE"), "confirm_delete", () =>
				{
					Action deleteAction = _deleteAction;
					_deleteAction = null;
					deleteAction();
					int selectedSlot = Current?.Slot ?? (-1);
					_saves = SaveManager.Saves();
					_selected = _saves.FindIndex((SaveDescription s) => s.Slot == selectedSlot);
					if (_selected < 0)
					{
						_selected = ((_saves.Count <= 0) ? (-1) : 0);
					}
					ReadPolicy();
					Build();
				});
				Button(val, T("取消", "CANCEL"), "cancel_delete", () =>
				{
					_deleteAction = null;
					Build();
				});
				val.End();
			}
			else if (_confirm)
			{
				val.BeginScroll(420f);
				SaveDescription saveDescription = SaveDescription.Read(_importBytes);
				val.AddLabel(T("导入为新槽位", "IMPORT AS A NEW SLOT"), (string)null);
				val.AddLabel(_importName + "  |  " + T("第 ", "Day ") + saveDescription.Day + "  |  " + saveDescription.Version, (string)null);
				val.AddLabel(T("不会覆盖现有存档。此操作只导入文件，不会立即载入游戏。", "Existing saves stay untouched. This imports a file without loading it."), (string)null);
				val.AddLabel(_importWarning, (string)null);
				val.End();
				val.BeginRow(8f);
				Button(val, T("确认导入", "CONFIRM IMPORT"), "confirm_import", () =>
				{
					if (SaveArchive.Hash(_importBytes) != _importHash)
					{
						throw new IOException("待导入数据发生变化。");
					}
					int slot = SaveManager.Import(_importBytes, _importName);
					_importBytes = null;
					_confirm = false;
					_saves = SaveManager.Saves();
					_selected = _saves.FindIndex((SaveDescription s) => s.Slot == slot);
					ReadPolicy();
					_notice = T("导入完成，新存档槽位：", "Imported to new slot: ") + slot + T("。请关闭此窗口，在加载游戏列表中选择该槽位。", ". Close this window and select this slot in Load Game.");
					Build();
				});
				Button(val, T("取消", "CANCEL"), "cancel_import", () =>
				{
					_confirm = false;
					_importBytes = null;
					_notice = "";
					Build();
				});
				val.End();
			}
			else
			{
				val.BeginRow(8f);
				if (_saves.Count != 0)
				{
					List<string> val2 = new List<string>();
					foreach (SaveDescription safe in _saves)
					{
						val2.Add("#" + safe.Slot + " " + safe.Name + " | " + T("第 ", "Day ") + safe.Day);
					}
					Action<int> val3 = Action<int>.op_Implicit((Action<int>)((int index) =>
					{
						try
						{
							_selected = index;
							_notice = "";
							ReadPolicy();
							Build();
						}
						catch (Exception ex)
						{
							_notice = ex.Message;
							Build();
						}
					}));
					Choices.Add(val3);
					val.AddDropdown(new IList<string>(((Il2CppObjectBase)val2).Pointer), _selected, val3, "save_select");
					Size(val, 470f, 36f);
					if (!SaveManager.InMenu)
					{
						((Selectable)val.Last.dropdown).interactable = false;
					}
				}
				Button(val, T("刷新", "REFRESH"), "refresh_saves", Open, 120f);
				val.End();
				if (Current != null)
				{
					val.BeginRow(8f);
					Action<bool> val4 = Action<bool>.op_Implicit((Action<bool>)((bool value) =>
					{
						_enabled = value;
					}));
					Toggles.Add(val4);
					val.AddToggle(T("自动备份", "AUTO BACKUP"), _enabled, val4, "backup_enabled");
					Size(val, 175f, 36f);
					val.AddLabel(T("间隔天数", "INTERVAL DAYS"), (string)null);
					Size(val, 120f, 36f);
					Input(val, "1 - 30", _interval, (string value) =>
					{
						_interval = value;
					}, "backup_interval");
					val.AddLabel(T("保留份数", "AUTO COPIES"), (string)null);
					Size(val, 120f, 36f);
					Input(val, "5 - 100", _keep, (string value) =>
					{
						_keep = value;
					}, "backup_keep");
					Button(val, T("应用", "APPLY"), "apply_backup", ApplyPolicy, 110f);
					val.End();
				}
				val.BeginRow(8f);
				Button(val, T("立即备份", "BACK UP NOW"), "backup_now", () =>
				{
					if (Current == null)
					{
						throw new InvalidOperationException(T("请先选择存档。", "Select a save first."));
					}
					SaveBackupRecord saveBackupRecord = SaveManager.BackupNow(Current.Slot);
					_notice = T("手动备份完成：", "Backup created: ") + saveBackupRecord.File;
					Build();
				});
				Button(val, T("导入存档", "IMPORT SAVE"), "import_save", () =>
				{
					if (!SaveManager.InMenu)
					{
						throw new InvalidOperationException(T("请返回主菜单后导入。", "Return to the main menu to import."));
					}
					if (SaveFilePicker.Choose(out var path))
					{
						PrepareImport(SaveArchive.ReadImport(path, out var record), record);
					}
				});
				Button(val, T("备份文件夹", "BACKUP FOLDER"), "backup_folder", () =>
				{
					Directory.CreateDirectory(SaveManager.Archive.Root);
					Process.Start(new ProcessStartInfo(SaveManager.Archive.Root)
					{
						UseShellExecute = true
					});
				});
				Button(val, T("删除所选存档", "DELETE SAVE"), "delete_save", () =>
				{
					if (!SaveManager.InMenu)
					{
						throw new InvalidOperationException(T("请返回主菜单再删除存档。", "Return to the main menu to delete a save."));
					}
					SaveDescription selected = Current ?? throw new InvalidOperationException(T("请先选择存档。", "Select a save first."));
					_deleteLabel = T("确认删除存档：", "Delete save: ") + "#" + selected.Slot + " " + selected.Name + T("\n删除前会保留一份手动备份，已有备份不会删除。", "\nA manual safety backup will be kept. Existing backups stay untouched.");
					_deleteAction = () =>
					{
						_notice = T("存档已删除，安全备份：", "Save deleted; safety backup: ") + SaveManager.DeleteSave(selected.Slot);
					};
					Build();
				});
				val.End();
				val.BeginScroll(Math.Max(220f, SaveWindowResize.Height - 330f));
				List<SaveBackupRecord> list = ((SaveManager.InMenu || Group == null) ? SaveManager.Archive.List() : SaveManager.Archive.List(Group));
				if (list.Count == 0)
				{
					val.AddLabel(T("暂无备份", "NO BACKUPS"), (string)null);
				}
				foreach (SaveBackupRecord item in list)
				{
					val.AddLabel("#" + item.Save.Slot + " " + SaveManager.DisplayName(item.Save) + " | " + item.CreatedUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss") + " | " + T("第 ", "Day ") + item.Save.Day + " | " + (item.Manual ? T("手动", "MANUAL") : T("自动", "AUTO")), (string)null);
					Size(val, 900f, 30f);
					val.AddLabel(item.Save.Version + " | MOD " + item.ModVersion, (string)null);
					Size(val, 900f, 26f);
					SaveBackupRecord captured = item;
					val.BeginRow(8f);
					Button(val, T("恢复到新槽位", "RESTORE TO NEW SLOT"), "restore_" + item.File, () =>
					{
						if (!SaveManager.InMenu)
						{
							throw new InvalidOperationException(T("请返回主菜单后恢复。", "Return to the main menu to restore."));
						}
						PrepareImport(SaveManager.Archive.ReadBackup(captured), captured);
					}, 260f);
					Button(val, T("删除备份", "DELETE BACKUP"), "delete_" + item.File, () =>
					{
						_deleteLabel = T("确认删除这份备份？\n", "Delete this backup?\n") + captured.File + T("\n仅移到 Deleted 子文件夹，不删除游戏存档。", "\nMoved to the Deleted subfolder. Game saves stay untouched.");
						_deleteAction = () =>
						{
							SaveManager.Archive.RemoveBackup(captured);
							_notice = T("备份已移到 Deleted，可手动移回。", "Backup moved to Deleted; move it back to restore.");
						};
						Build();
					});
					val.End();
				}
				val.End();
				Button(val, T("关闭", "CLOSE"), "close_saves", Close);
			}
			CustomUIWindow val5 = val.Show();
			foreach (TextMeshProUGUI componentsInChild in val5.root.GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				((TMP_Text)componentsInChild).richText = false;
			}
			CustomUIWindowFrame frame = val5.frame;
			if ((Object)(object)((frame != null) ? frame.closeButton : null) != (Object)null)
			{
				val5.frame.closeButton.onClick = new ButtonClickedEvent();
				((UnityEvent)val5.frame.closeButton.onClick).AddListener(UnityAction.op_Implicit((Action)Close));
			}
			SaveWindowResize.Attach(val5);
		}

		private static void PrepareImport(byte[] bytes, SaveBackupRecord record)
		{
			_importName = SaveManager.DisplayName(SaveDescription.Read(bytes), record?.Save?.Name);
			_importBytes = bytes;
			_importHash = SaveArchive.Hash(bytes);
			_confirm = true;
			_notice = "";
			_importWarning = T("导入前请确认游戏版本和所需 MOD 一致，文件校验不保证跨版本兼容。", "Check the game version and required MODs. File validation does not guarantee compatibility.");
			if (record == null)
			{
				_importWarning += T("\n未记录 MOD 清单，请自行确认依赖。", "\nNo MOD manifest; verify dependencies before loading.");
			}
			else
			{
				string[] array = (record.Mods ?? Array.Empty<string>()).Except(SaveManager.Mods).ToArray();
				if (array.Length != 0)
				{
					_importWarning = _importWarning + T("\n缺少或版本不同：\n", "\nMissing or different versions:\n") + string.Join("\n", array);
				}
			}
			Build();
		}

		private static void ApplyPolicy()
		{
			if (Group == null || !int.TryParse(_interval, out var result) || !int.TryParse(_keep, out var result2))
			{
				throw new ArgumentException(T("请输入有效整数。", "Enter valid integers."));
			}
			SaveManager.Archive.SetPolicy(Group, new SaveBackupPolicy
			{
				Enabled = _enabled,
				Interval = result,
				Keep = result2
			});
			_notice = T("设置已保存，仅影响所选存档。", "Settings saved for the selected save.");
			Build();
		}

		private static void Button(CustomUIBuilder b, string text, string id, Action action, float width = 210f)
		{
			Action val = Action.op_Implicit((Action)(() =>
			{
				try
				{
					action();
				}
				catch (Exception ex)
				{
					_notice = T("操作未完成：", "Operation failed: ") + ex.Message;
					Build();
				}
			}));
			Actions.Add(val);
			b.AddButton(text, val, id);
			Size(b, width, 38f);
		}

		private static void Input(CustomUIBuilder b, string hint, string value, Action<string> action, string id)
		{
			Action<string> val = Action<string>.op_Implicit(action);
			Inputs.Add(val);
			b.AddInput(hint, value, val, id);
			Size(b, 95f, 36f);
		}

		private static void Size(CustomUIBuilder b, float width, float height)
		{
			b.Last.SetPreferredSize(width, height);
		}

		internal static void Close()
		{
			SaveWindowResize.Remember();
			CustomUIManager instance = CustomUIManager.Instance;
			if (instance != null)
			{
				instance.CloseWindow("xiaowo_save_manager");
			}
			_importBytes = null;
			_confirm = false;
			_deleteAction = null;
		}

		internal static void Dispose()
		{
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004a: Unknown result type (might be due to invalid IL or missing references)
			Close();
			if ((Object)(object)_menuButton != (Object)null)
			{
				Object.Destroy((Object)(object)_menuButton);
			}
			if ((Object)(object)_pauseButton != (Object)null)
			{
				Object.Destroy((Object)(object)_pauseButton);
			}
			if ((Object)(object)_pauseFrame != (Object)null)
			{
				Rect rect = _pauseFrame.rect;
				if (Math.Abs(rect.height - _pauseAppliedHeight) < 0.1f)
				{
					_pauseFrame.SetSizeWithCurrentAnchors((Axis)1, _pauseHeight);
				}
			}
			_pauseFrame = null;
			_menuButton = (_pauseButton = null);
			_escape = null;
			Actions.Clear();
			Inputs.Clear();
			Choices.Clear();
			Toggles.Clear();
		}
	}
	internal static class SaveFilePicker
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		private struct OpenFileName
		{
			public int structSize;

			public IntPtr owner;

			public IntPtr instance;

			public string filter;

			public IntPtr customFilter;

			public int maxCustomFilter;

			public int filterIndex;

			public IntPtr file;

			public int maxFile;

			public IntPtr fileTitle;

			public int maxFileTitle;

			public string initialDirectory;

			public string title;

			public int flags;

			public short fileOffset;

			public short fileExtension;

			public IntPtr defaultExtension;

			public IntPtr customData;

			public IntPtr hook;

			public IntPtr templateName;

			public IntPtr reserved;

			public int reserved2;

			public int flagsEx;
		}

		internal static bool Choose(out string path)
		{
			path = "";
			IntPtr intPtr = Marshal.AllocHGlobal(8192);
			try
			{
				Marshal.Copy(new byte[8192], 0, intPtr, 8192);
				OpenFileName data = new OpenFileName
				{
					structSize = Marshal.SizeOf<OpenFileName>(),
					owner = GetActiveWindow(),
					file = intPtr,
					maxFile = 4096,
					filter = "Probably Stolen Save (*.zip;*.es3)\0*.zip;*.es3\0\0",
					title = SaveManagerUI.T("导入存档", "Import save"),
					initialDirectory = Path.GetFullPath(Application.persistentDataPath),
					flags = 530440
				};
				if (!GetOpenFileName(ref data))
				{
					int num = CommDlgExtendedError();
					if (num != 0)
					{
						throw new IOException("文件选择窗口未能打开：" + num);
					}
					return false;
				}
				path = Marshal.PtrToStringUni(intPtr);
				return !string.IsNullOrEmpty(path);
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr);
			}
		}

		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW")]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetOpenFileName(ref OpenFileName data);

		[DllImport("comdlg32.dll")]
		private static extern int CommDlgExtendedError();

		[DllImport("user32.dll")]
		private static extern IntPtr GetActiveWindow();
	}
	internal static class SaveWindowResize
	{
		private static CustomUIWindow _window;

		private static RectTransform _rect;

		private static Camera _camera;

		private static Vector2 _size = new Vector2(1000f, 740f);

		private static Vector2 _position;

		private static Vector2 _startPoint;

		private static Vector2 _startSize;

		private static Vector2 _startPosition;

		private static bool _positionKnown;

		private static bool _cursor;

		private static int _edge;

		internal static float Width => _size.x;

		internal static float Height => _size.y;

		internal static void Attach(CustomUIWindow window)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_0051: Unknown result type (might be due to invalid IL or missing references)
			//IL_005b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			_window = window;
			_rect = window.Rect;
			Canvas componentInParent = ((Component)_rect).GetComponentInParent<Canvas>();
			_camera = (((Object)(object)componentInParent != (Object)null && (int)componentInParent.renderMode != 0) ? componentInParent.worldCamera : null);
			if (_positionKnown)
			{
				_rect.anchoredPosition = _position;
			}
			Apply(_size, _rect.anchoredPosition);
		}

		internal static void Remember()
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001a: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0029: Unknown result type (might be due to invalid IL or missing references)
			//IL_002e: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_rect != (Object)null)
			{
				Rect rect = _rect.rect;
				_size = rect.size;
				_position = _rect.anchoredPosition;
				_positionKnown = true;
			}
			_rect = null;
			_window = null;
			_edge = 0;
			Release();
		}

		internal static void Tick()
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0094: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_019b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_014c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0150: Unknown result type (might be due to invalid IL or missing references)
			//IL_0155: Unknown result type (might be due to invalid IL or missing references)
			//IL_015f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0164: Unknown result type (might be due to invalid IL or missing references)
			//IL_016b: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ee: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_01af: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_01bb: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f1: Unknown result type (might be due to invalid IL or missing references)
			//IL_020c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0232: Unknown result type (might be due to invalid IL or missing references)
			//IL_0234: Unknown result type (might be due to invalid IL or missing references)
			//IL_0239: Unknown result type (might be due to invalid IL or missing references)
			//IL_023a: Unknown result type (might be due to invalid IL or missing references)
			//IL_023f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0244: Unknown result type (might be due to invalid IL or missing references)
			//IL_0246: Unknown result type (might be due to invalid IL or missing references)
			//IL_024b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0229: Unknown result type (might be due to invalid IL or missing references)
			//IL_0269: Unknown result type (might be due to invalid IL or missing references)
			//IL_0274: Unknown result type (might be due to invalid IL or missing references)
			//IL_0297: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a1: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c7: Unknown result type (might be due to invalid IL or missing references)
			//IL_02d2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0309: Unknown result type (might be due to invalid IL or missing references)
			//IL_02f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_02ff: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)_rect == (Object)null))
			{
				CustomUIWindow window = _window;
				if (window != null && window.IsVisible && Application.isFocused)
				{
					Vector2 val = Vector2.op_Implicit(Input.mousePosition);
					Vector2 val2 = default;
					if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_rect, val, _camera, ref val2))
					{
						_edge = 0;
						Release();
						return;
					}
					Rect rect = _rect.rect;
					int num = 0;
					CustomUIWindowFrame frame = _window.frame;
					object obj;
					if (frame == null)
					{
						obj = null;
					}
					else
					{
						Button closeButton = frame.closeButton;
						obj = ((closeButton != null) ? ((Component)closeButton).GetComponent<RectTransform>() : null);
					}
					RectTransform val3 = (RectTransform)obj;
					if (rect.Contains(val2) && (!((Object)(object)val3 != (Object)null) || !RectTransformUtility.RectangleContainsScreenPoint(val3, val, _camera)))
					{
						if (val2.x - rect.xMin <= 8f)
						{
							num |= 1;
						}
						if (rect.xMax - val2.x <= 8f)
						{
							num |= 2;
						}
						if (rect.yMax - val2.y <= 8f)
						{
							num |= 4;
						}
						if (val2.y - rect.yMin <= 8f)
						{
							num |= 8;
						}
					}
					RectTransform component = ((Component)((Transform)_rect).parent).GetComponent<RectTransform>();
					if (_edge == 0 && num != 0 && Input.GetMouseButtonDown(0))
					{
						_edge = num;
						Rect rect2 = _rect.rect;
						_startSize = rect2.size;
						_startPosition = _rect.anchoredPosition;
						RectTransformUtility.ScreenPointToLocalPointInRectangle(component, val, _camera, ref _startPoint);
					}
					if (_edge != 0)
					{
						Vector2 val4 = default;
						if (!Input.GetMouseButton(0))
						{
							_edge = 0;
						}
						else if (RectTransformUtility.ScreenPointToLocalPointInRectangle(component, val, _camera, ref val4))
						{
							Vector2 val5 = val4 - _startPoint;
							Vector2 startSize = _startSize;
							if ((_edge & 1) != 0)
							{
								startSize.x -= val5.x;
							}
							else if ((_edge & 2) != 0)
							{
								startSize.x += val5.x;
							}
							if ((_edge & 8) != 0)
							{
								startSize.y -= val5.y;
							}
							else if ((_edge & 4) != 0)
							{
								startSize.y += val5.y;
							}
							Vector2 val6 = Clamp(startSize);
							Vector2 val7 = val6 - _startSize;
							Vector2 startPosition = _startPosition;
							if ((_edge & 1) != 0)
							{
								startPosition.x -= (1f - _rect.pivot.x) * val7.x;
							}
							else if ((_edge & 2) != 0)
							{
								startPosition.x += _rect.pivot.x * val7.x;
							}
							if ((_edge & 8) != 0)
							{
								startPosition.y -= (1f - _rect.pivot.y) * val7.y;
							}
							else if ((_edge & 4) != 0)
							{
								startPosition.y += _rect.pivot.y * val7.y;
							}
							Apply(val6, startPosition);
						}
					}
					int num2 = ((_edge != 0) ? _edge : num);
					if (num2 != 0)
					{
						ContainerWindowResizer.SetModManagerCursor(num2);
						_cursor = true;
					}
					else
					{
						Release();
					}
					return;
				}
			}
			_edge = 0;
			Release();
		}

		private static Vector2 Clamp(Vector2 size)
		{
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_004d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0059: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0072: Unknown result type (might be due to invalid IL or missing references)
			//IL_0077: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			RectTransform component = ((Component)((Transform)_rect).parent).GetComponent<RectTransform>();
			float x = size.x;
			Rect rect = component.rect;
			float num = Math.Min(940f, rect.width - 24f);
			rect = component.rect;
			float num2 = Mathf.Clamp(x, num, rect.width - 24f);
			float y = size.y;
			rect = component.rect;
			float num3 = Math.Min(650f, rect.height - 24f);
			rect = component.rect;
			return new Vector2(num2, Mathf.Clamp(y, num3, rect.height - 24f));
		}

		private static void Apply(Vector2 size, Vector2 position)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0008: Unknown result type (might be due to invalid IL or missing references)
			//IL_0009: Unknown result type (might be due to invalid IL or missing references)
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0025: Unknown result type (might be due to invalid IL or missing references)
			//IL_0035: Unknown result type (might be due to invalid IL or missing references)
			//IL_006f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			size = Clamp(size);
			_size = size;
			_rect.SetSizeWithCurrentAnchors((Axis)0, size.x);
			_rect.SetSizeWithCurrentAnchors((Axis)1, size.y);
			_rect.anchoredPosition = position;
			foreach (ScrollRect componentsInChild in _window.root.GetComponentsInChildren<ScrollRect>(true))
			{
				LayoutElement component = ((Component)componentsInChild).GetComponent<LayoutElement>();
				if ((Object)(object)component != (Object)null)
				{
					float minHeight = (component.preferredHeight = Math.Max(220f, size.y - 330f));
					component.minHeight = minHeight;
				}
			}
			foreach (TextMeshProUGUI componentsInChild2 in _window.root.GetComponentsInChildren<TextMeshProUGUI>(true))
			{
				LayoutElement component2 = ((Component)componentsInChild2).GetComponent<LayoutElement>();
				if ((Object)(object)component2 != (Object)null && component2.preferredWidth >= 880f)
				{
					float minHeight = (component2.preferredWidth = size.x - 70f);
					component2.minWidth = minHeight;
				}
			}
		}

		private static void Release()
		{
			if (_cursor)
			{
				ContainerWindowResizer.ResetModManagerCursor();
				_cursor = false;
			}
		}

		static SaveWindowResize()
		{
			//IL_000a: Unknown result type (might be due to invalid IL or missing references)
			//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		}
	}
	internal enum SkinPreset
	{
		OriginalEnhanced,
		XiaowoBlueBlack
	}
	internal enum SkinFitMode
	{
		Cover,
		Stretch,
		Tile
	}
	internal enum SkinSlot
	{
		MainMenu,
		Window,
		Shutter
	}
	internal sealed class SkinSettings
	{
		public bool Enabled { get; set; }

		public SkinPreset Preset { get; set; }

		public string MainMenuPath { get; set; } = string.Empty;

		public string WindowPath { get; set; } = string.Empty;

		public string ShutterPath { get; set; } = string.Empty;

		public SkinFitMode MainMenuFit { get; set; }

		public SkinFitMode WindowFit { get; set; } = SkinFitMode.Stretch;

		public SkinFitMode ShutterFit { get; set; } = SkinFitMode.Stretch;

		internal SkinSettings Clone()
		{
			return new SkinSettings
			{
				Enabled = Enabled,
				Preset = Preset,
				MainMenuPath = (MainMenuPath ?? string.Empty),
				WindowPath = (WindowPath ?? string.Empty),
				ShutterPath = (ShutterPath ?? string.Empty),
				MainMenuFit = MainMenuFit,
				WindowFit = WindowFit,
				ShutterFit = ShutterFit
			};
		}

		internal string PathFor(SkinSlot slot)
		{
			return slot switch
			{
				SkinSlot.MainMenu => MainMenuPath ?? string.Empty, 
				SkinSlot.Window => WindowPath ?? string.Empty, 
				_ => ShutterPath ?? string.Empty, 
			};
		}

		internal SkinFitMode FitFor(SkinSlot slot)
		{
			return slot switch
			{
				SkinSlot.MainMenu => MainMenuFit, 
				SkinSlot.Window => WindowFit, 
				_ => ShutterFit, 
			};
		}
	}
	internal static class SkinManager
	{
		private sealed class AssetSet
		{
			internal Sprite MainMenu;

			internal Sprite Window;

			internal Sprite Shutter;
		}

		private sealed record ImageState(Image Image, Sprite Sprite, Type Type, bool PreserveAspect)
		{
			[CompilerGenerated]
			public void Deconstruct(out Image Image, out Sprite Sprite, out Type Type, out bool PreserveAspect)
			{
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				//IL_0018: Expected I4, but got Unknown
				Image = this.Image;
				Sprite = this.Sprite;
				Type = (Type)(int)this.Type;
				PreserveAspect = this.PreserveAspect;
			}
		}

		private sealed record RendererState(SpriteRenderer Renderer, Sprite Sprite, Vector3 Scale, SpriteDrawMode DrawMode, Vector2 Size)
		{
			[CompilerGenerated]
			public void Deconstruct(out SpriteRenderer Renderer, out Sprite Sprite, out Vector3 Scale, out SpriteDrawMode DrawMode, out Vector2 Size)
			{
				//IL_0012: Unknown result type (might be due to invalid IL or missing references)
				//IL_0017: Unknown result type (might be due to invalid IL or missing references)
				//IL_001f: Unknown result type (might be due to invalid IL or missing references)
				//IL_0025: Expected I4, but got Unknown
				//IL_0028: Unknown result type (might be due to invalid IL or missing references)
				//IL_002d: Unknown result type (might be due to invalid IL or missing references)
				Renderer = this.Renderer;
				Sprite = this.Sprite;
				Scale = this.Scale;
				DrawMode = (SpriteDrawMode)(int)this.DrawMode;
				Size = this.Size;
			}
		}

		private struct OpenFileName
		{
			public int structSize;

			public IntPtr dialogOwner;

			public IntPtr instance;

			public IntPtr filter;

			public IntPtr customFilter;

			public int maxCustomFilter;

			public int filterIndex;

			public IntPtr file;

			public int maxFile;

			public IntPtr fileTitle;

			public int maxFileTitle;

			public IntPtr initialDirectory;

			public IntPtr title;

			public int flags;

			public short fileOffset;

			public short fileExtension;

			public IntPtr defaultExtension;

			public IntPtr customData;

			public IntPtr hook;

			public IntPtr templateName;

			public IntPtr reserved;

			public int reserved2;

			public int flagsEx;
		}

		private const long MaximumFileBytes = 16777216L;

		private const int MaximumDimension = 4096;

		private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
		{
			WriteIndented = true
		};

		private static readonly Dictionary<int, ImageState> WindowStates = new Dictionary<int, ImageState>();

		private static readonly Dictionary<int, RendererState> RendererStates = new Dictionary<int, RendererState>();

		private static readonly Dictionary<string, Sprite> FittedSprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);

		private static readonly Dictionary<string, Sprite> ShutterComposites = new Dictionary<string, Sprite>(StringComparer.Ordinal);

		private static readonly List<Sprite> PreviewSprites = new List<Sprite>();

		private static AssetSet _assets;

		private static Harmony _windowHarmony;

		private static SkinSettings _applied = new SkinSettings();

		private static SkinSettings _visible = new SkinSettings();

		private static MainMenuUIController _menu;

		private static SpriteRenderer _menuBackground;

		private static SpriteRenderer _shutterDoor;

		private static ImageState _shutterBlockerState;

		private static int _sceneHandle = int.MinValue;

		private static RuntimePollGate _refreshPoll;

		private static string _lastFailure = string.Empty;

		private static string _lastShutterFailure = string.Empty;

		internal static string RootDirectory => Path.Combine(MelonEnvironment.UserDataDirectory, "XIAOWOTradePerks", "Skins");

		internal static string CustomDirectory => Path.Combine(RootDirectory, "Custom");

		internal static string ConfigPath => Path.Combine(RootDirectory, "skin.json");

		internal static bool IsEnabled => _applied.Enabled;

		internal static SkinSettings Current => _applied.Clone();

		internal static void Initialize()
		{
			Directory.CreateDirectory(CustomDirectory);
			_applied = LoadSettings();
			_visible = _applied.Clone();
			Instance log = Core.Log;
			if (log != null)
			{
				log.Msg("Custom skin configuration loaded: enabled=" + _applied.Enabled + ", preset=" + _applied.Preset.ToString() + ".");
			}
			if (!TryActivate(_applied, out var message))
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Warning("自定义皮肤配置加载失败，已恢复原版：" + message);
				}
				_applied.Enabled = false;
				SaveSettings(_applied, out var _);
				RestoreTargets();
			}
		}

		internal static void Tick()
		{
			if (!_visible.Enabled || _assets == null || !_refreshPoll.Due(1000))
			{
				return;
			}
			ReconcileActiveScene();
			try
			{
				ApplyMainMenu();
				ApplyShutters();
				ReapplyKnownWindows();
				_lastFailure = string.Empty;
			}
			catch (Exception ex)
			{
				string text = ex.GetType().Name + ": " + ex.Message;
				if (!string.Equals(text, _lastFailure, StringComparison.Ordinal))
				{
					_lastFailure = text;
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("自定义皮肤刷新已跳过：" + text);
					}
				}
			}
		}

		internal static bool Preview(SkinSettings draft, out string message)
		{
			SkinSettings skinSettings = draft?.Clone() ?? new SkinSettings();
			skinSettings.Enabled = true;
			if (!TryBuildAssets(skinSettings, out var assets, out message))
			{
				return false;
			}
			if (!TryEnableWindowHook(out message))
			{
				DestroyAssets(assets);
				return false;
			}
			_visible = skinSettings;
			RestoreTargets();
			ReplaceAssets(assets);
			ApplyAllKnownTargets(skinSettings);
			message = Select("预览已应用；关闭窗口会恢复已保存方案。", "Preview applied; closing restores the saved skin.");
			return true;
		}

		internal static bool Apply(SkinSettings draft, out string message)
		{
			if (draft == null)
			{
				message = Select("没有可应用的皮肤配置。", "No skin configuration is available.");
				return false;
			}
			SkinSettings skinSettings = draft.Clone();
			skinSettings.Enabled = true;
			if (!TryValidateAll(skinSettings, out message))
			{
				return false;
			}
			if (!TryImportCustomFiles(skinSettings, out message))
			{
				return false;
			}
			if (!TryBuildAssets(skinSettings, out var assets, out message))
			{
				return false;
			}
			if (!TryEnableWindowHook(out message))
			{
				DestroyAssets(assets);
				return false;
			}
			if (!SaveSettings(skinSettings, out message))
			{
				DestroyAssets(assets);
				return false;
			}
			_applied = skinSettings;
			_visible = skinSettings.Clone();
			RestoreTargets();
			ReplaceAssets(assets);
			ApplyAllKnownTargets(_applied);
			message = Select("皮肤已保存并应用。", "Skin saved and applied.");
			return true;
		}

		internal static bool RestoreOriginal(out string message)
		{
			SkinSettings skinSettings = _applied.Clone();
			skinSettings.Enabled = false;
			if (!SaveSettings(skinSettings, out message))
			{
				return false;
			}
			_applied = skinSettings;
			_visible = skinSettings.Clone();
			RestoreTargets();
			DisableWindowHook();
			ClearShutterComposites();
			DestroyAssets(_assets);
			_assets = null;
			message = Select("已恢复游戏原版外观。", "Original game appearance restored.");
			return true;
		}

		internal static void RevertPreview()
		{
			_visible = _applied.Clone();
			RestoreTargets();
			if (_applied.Enabled)
			{
				TryEnableWindowHook(out var message);
				if (TryBuildAssets(_applied, out var assets, out message))
				{
					ReplaceAssets(assets);
				}
				ApplyAllKnownTargets(_applied);
			}
			else
			{
				DisableWindowHook();
				ClearShutterComposites();
				DestroyAssets(_assets);
				_assets = null;
			}
		}

		internal static Sprite PreviewSprite(SkinSettings draft, SkinSlot slot)
		{
			string text = draft?.PathFor(slot);
			if (!string.IsNullOrWhiteSpace(text) && TryLoadSprite(text, "skin.preview." + slot, out var sprite, out var _))
			{
				PreviewSprites.Add(sprite);
				return sprite;
			}
			Sprite presetSprite = GetPresetSprite(draft?.Preset ?? SkinPreset.OriginalEnhanced, slot);
			if (_assets == null || (Object)(object)presetSprite != (Object)(object)AssetFor(_assets, slot))
			{
				PreviewSprites.Add(presetSprite);
			}
			return presetSprite;
		}

		internal static void ClearPreviewSprites()
		{
			foreach (Sprite previewSprite in PreviewSprites)
			{
				DestroySpriteAndTexture(previewSprite);
			}
			PreviewSprites.Clear();
		}

		internal static void OpenSkinFolder()
		{
			Directory.CreateDirectory(CustomDirectory);
			Process.Start(new ProcessStartInfo
			{
				FileName = CustomDirectory,
				UseShellExecute = true
			});
		}

		internal static bool TryChooseImage(out string path)
		{
			path = string.Empty;
			if (!OperatingSystem.IsWindows())
			{
				return false;
			}
			IntPtr intPtr = Marshal.AllocHGlobal(8192);
			IntPtr intPtr2 = Marshal.StringToHGlobalUni("Image files (*.png;*.jpg;*.jpeg)\0*.png;*.jpg;*.jpeg\0PNG (*.png)\0*.png\0JPEG (*.jpg;*.jpeg)\0*.jpg;*.jpeg\0\0");
			IntPtr intPtr3 = Marshal.StringToHGlobalUni(Directory.Exists(CustomDirectory) ? CustomDirectory : RootDirectory);
			IntPtr intPtr4 = Marshal.StringToHGlobalUni(Select("选择皮肤图片", "Choose a skin image"));
			try
			{
				Marshal.Copy(new byte[8192], 0, intPtr, 8192);
				OpenFileName openFileName = new OpenFileName
				{
					structSize = ((IntPtr.Size == 8) ? 152 : 88),
					filter = intPtr2,
					file = intPtr,
					maxFile = 4096,
					initialDirectory = intPtr3,
					title = intPtr4,
					flags = 530432
				};
				if (!GetOpenFileName(ref openFileName))
				{
					return false;
				}
				path = Marshal.PtrToStringUni(intPtr) ?? string.Empty;
				return path.Length != 0;
			}
			finally
			{
				Marshal.FreeHGlobal(intPtr4);
				Marshal.FreeHGlobal(intPtr3);
				Marshal.FreeHGlobal(intPtr2);
				Marshal.FreeHGlobal(intPtr);
			}
		}

		internal static void ApplyWindow(PixelWindow window)
		{
			try
			{
				if (_visible.Enabled && _assets != null)
				{
					object obj;
					if (window == null)
					{
						obj = null;
					}
					else
					{
						TreeNodeRender background = window.background;
						obj = ((background != null) ? background.image : null);
					}
					if (!((Object)obj == (Object)null))
					{
						ApplyWindowImage(window.background.image, _assets.Window, _visible.WindowFit);
					}
				}
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("窗口皮肤已跳过，不影响窗口打开：" + ex.Message);
				}
			}
		}

		internal static string BuildStatus()
		{
			return "SKINS\nenabled=" + _applied.Enabled + "\npreset=" + _applied.Preset.ToString() + "\nassets=" + (_assets != null) + "\nwindowHook=" + (_windowHarmony != null) + "\nmenuTracked=" + ((Object)(object)_menu != (Object)null && Object.op_Implicit((Object)(object)_menu)) + "\nrenderers=" + RendererStates.Count + "\nwindows=" + WindowStates.Count + "\nroot=" + RootDirectory;
		}

		internal static bool ValidateDefinition(out string details)
		{
			bool result = Enum.GetValues<SkinPreset>().Length == 2 && Enum.GetValues<SkinSlot>().Length == 3;
			details = "presets=2, slots=3, maxBytes=" + 16777216L + ", maxDimension=" + 4096;
			return result;
		}

		internal static void Dispose()
		{
			RestoreTargets();
			DisableWindowHook();
			ClearPreviewSprites();
			ClearShutterComposites();
			DestroyAssets(_assets);
			_assets = null;
			foreach (Sprite value in FittedSprites.Values)
			{
				if ((Object)(object)value != (Object)null && Object.op_Implicit((Object)(object)value))
				{
					Object.Destroy((Object)(object)value);
				}
			}
			FittedSprites.Clear();
			_lastFailure = string.Empty;
			_refreshPoll.Reset();
			_sceneHandle = int.MinValue;
		}

		private static SkinSettings LoadSettings()
		{
			try
			{
				if (!File.Exists(ConfigPath))
				{
					return new SkinSettings();
				}
				return JsonSerializer.Deserialize<SkinSettings>(File.ReadAllText(ConfigPath), JsonOptions) ?? new SkinSettings();
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("自定义皮肤配置无法读取：" + ex.Message);
				}
				return new SkinSettings
				{
					Enabled = false
				};
			}
		}

		private static bool SaveSettings(SkinSettings settings, out string message)
		{
			try
			{
				Directory.CreateDirectory(RootDirectory);
				string text = ConfigPath + ".tmp";
				File.WriteAllText(text, JsonSerializer.Serialize(settings, JsonOptions), new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				File.Move(text, ConfigPath, overwrite: true);
				message = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				message = Select("无法保存皮肤配置：", "Could not save skin configuration: ") + ex.Message;
				return false;
			}
		}

		private static bool TryActivate(SkinSettings settings, out string message)
		{
			if (!settings.Enabled)
			{
				message = string.Empty;
				return true;
			}
			if (!TryBuildAssets(settings, out var assets, out message))
			{
				return false;
			}
			if (!TryEnableWindowHook(out message))
			{
				DestroyAssets(assets);
				return false;
			}
			ReplaceAssets(assets);
			return true;
		}

		private static bool TryBuildAssets(SkinSettings settings, out AssetSet assets, out string message)
		{
			assets = null;
			if (!TryValidateAll(settings, out message))
			{
				return false;
			}
			AssetSet assetSet = new AssetSet();
			try
			{
				assetSet.MainMenu = LoadOrPreset(settings, SkinSlot.MainMenu);
				assetSet.Window = LoadOrPreset(settings, SkinSlot.Window);
				assetSet.Shutter = LoadOrPreset(settings, SkinSlot.Shutter);
				if ((Object)(object)assetSet.MainMenu == (Object)null || (Object)(object)assetSet.Window == (Object)null || (Object)(object)assetSet.Shutter == (Object)null)
				{
					throw new InvalidOperationException(Select("预设资源生成失败。", "Preset resources could not be generated."));
				}
				assets = assetSet;
				message = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				DestroyAssets(assetSet);
				message = ex.Message;
				return false;
			}
		}

		private static Sprite LoadOrPreset(SkinSettings settings, SkinSlot slot)
		{
			string text = settings.PathFor(slot);
			if (string.IsNullOrWhiteSpace(text))
			{
				return CreatePresetSprite(settings.Preset, slot);
			}
			if (!TryLoadSprite(text, "skin.custom." + slot, out var sprite, out var message))
			{
				throw new InvalidOperationException(message);
			}
			return sprite;
		}

		private static bool TryValidateAll(SkinSettings settings, out string message)
		{
			SkinSlot[] values = Enum.GetValues<SkinSlot>();
			foreach (SkinSlot slot in values)
			{
				string text = settings.PathFor(slot);
				if (!string.IsNullOrWhiteSpace(text) && !ValidateImageFile(text, out message))
				{
					message = SlotName(slot) + "：" + message;
					return false;
				}
			}
			message = string.Empty;
			return true;
		}

		private static bool ValidateImageFile(string path, out string message)
		{
			try
			{
				if (!File.Exists(path))
				{
					message = Select("图片文件不存在。", "Image file does not exist.");
					return false;
				}
				string extension = Path.GetExtension(path);
				if (!string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) && !string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase))
				{
					message = Select("只支持 PNG、JPG 和 JPEG。", "Only PNG, JPG, and JPEG are supported.");
					return false;
				}
				if (new FileInfo(path).Length > 16777216)
				{
					message = Select("图片超过 16 MB。", "Image exceeds 16 MB.");
					return false;
				}
				if (!TryLoadSprite(path, "skin.validation", out var sprite, out message))
				{
					return false;
				}
				Texture2D texture = sprite.texture;
				int num;
				if (((Texture)texture).width <= 4096)
				{
					num = ((((Texture)texture).height <= 4096) ? 1 : 0);
					if (num != 0)
					{
						goto IL_00e0;
					}
				}
				else
				{
					num = 0;
				}
				message = Select("图片任一边不能超过 4096 像素。", "Image dimensions cannot exceed 4096 pixels.");
				goto IL_00e0;
				IL_00e0:
				DestroySpriteAndTexture(sprite);
				return (byte)num != 0;
			}
			catch (Exception ex)
			{
				message = Select("图片校验失败：", "Image validation failed: ") + ex.Message;
				return false;
			}
		}

		private static bool TryLoadSprite(string path, string name, out Sprite sprite, out string message)
		{
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0024: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Unknown result type (might be due to invalid IL or missing references)
			//IL_003e: Expected Obj, but got Unknown
			//IL_007c: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			sprite = null;
			Texture2D val = null;
			try
			{
				byte[] array = File.ReadAllBytes(path);
				val = new Texture2D(2, 2, (TextureFormat)4, false, false)
				{
					filterMode = (FilterMode)0,
					wrapMode = (TextureWrapMode)0,
					hideFlags = (HideFlags)61,
					name = name + ".texture"
				};
				if (!ImageConversion.LoadImage(val, new Il2CppStructArray<byte>(array), false))
				{
					throw new InvalidDataException(Select("无法解码图片。", "Image could not be decoded."));
				}
				sprite = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
				((Object)sprite).name = name;
				((Object)sprite).hideFlags = (HideFlags)61;
				message = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				if ((Object)(object)val != (Object)null && Object.op_Implicit((Object)(object)val))
				{
					Object.Destroy((Object)(object)val);
				}
				message = ex.Message;
				return false;
			}
		}

		private static bool TryImportCustomFiles(SkinSettings settings, out string message)
		{
			try
			{
				Directory.CreateDirectory(CustomDirectory);
				settings.MainMenuPath = ImportOne(settings.MainMenuPath, "main_menu");
				settings.WindowPath = ImportOne(settings.WindowPath, "window_background");
				settings.ShutterPath = ImportOne(settings.ShutterPath, "shutter");
				message = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				message = Select("无法导入自定义图片：", "Could not import custom image: ") + ex.Message;
				return false;
			}
		}

		private static string ImportOne(string source, string baseName)
		{
			if (string.IsNullOrWhiteSpace(source))
			{
				return string.Empty;
			}
			string fullPath = Path.GetFullPath(source);
			string text = Path.GetExtension(fullPath).ToLowerInvariant();
			string text2 = Path.Combine(CustomDirectory, baseName + text);
			if (string.Equals(fullPath, Path.GetFullPath(text2), StringComparison.OrdinalIgnoreCase))
			{
				return text2;
			}
			string text3 = text2 + ".tmp";
			File.Copy(fullPath, text3, overwrite: true);
			File.Move(text3, text2, overwrite: true);
			return text2;
		}

		private static Sprite GetPresetSprite(SkinPreset preset, SkinSlot slot)
		{
			if (_assets != null && _visible.Preset == preset && string.IsNullOrWhiteSpace(_visible.PathFor(slot)))
			{
				return AssetFor(_assets, slot);
			}
			return CreatePresetSprite(preset, slot);
		}

		private static Sprite AssetFor(AssetSet assets, SkinSlot slot)
		{
			return slot switch
			{
				SkinSlot.MainMenu => assets.MainMenu, 
				SkinSlot.Window => assets.Window, 
				_ => assets.Shutter, 
			};
		}

		private static Sprite CreatePresetSprite(SkinPreset preset, SkinSlot slot)
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_0069: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Expected Obj, but got Unknown
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Unknown result type (might be due to invalid IL or missing references)
			(int, int) tuple = slot switch
			{
				SkinSlot.MainMenu => (320, 180), 
				SkinSlot.Window => (128, 96), 
				_ => (96, 192), 
			};
			int item = tuple.Item1;
			int item2 = tuple.Item2;
			Texture2D val = new Texture2D(item, item2, (TextureFormat)4, false, false)
			{
				filterMode = (FilterMode)0,
				wrapMode = (TextureWrapMode)0,
				hideFlags = (HideFlags)61,
				name = "skin.preset." + preset.ToString() + "." + slot
			};
			Il2CppStructArray<Color32> val2 = new Il2CppStructArray<Color32>((long)(item * item2));
			for (int i = 0; i < item2; i++)
			{
				for (int j = 0; j < item; j++)
				{
					((Il2CppArrayBase<Color32>)(object)val2)[i * item + j] = PresetPixel(preset, slot, j, i);
				}
			}
			val.SetPixels32(val2);
			val.Apply(false, false);
			Sprite val3 = Sprite.Create(val, new Rect(0f, 0f, (float)item, (float)item2), new Vector2(0.5f, 0.5f), 100f);
			((Object)val3).name = ((Object)val).name;
			((Object)val3).hideFlags = (HideFlags)61;
			return val3;
		}

		private static Color32 PresetPixel(SkinPreset preset, SkinSlot slot, int x, int y)
		{
			//IL_00bc: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
			//IL_001c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d4: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0034: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e2: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e7: Unknown result type (might be due to invalid IL or missing references)
			//IL_0042: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_011a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0100: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0066: Unknown result type (might be due to invalid IL or missing references)
			//IL_0143: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			if (preset == SkinPreset.XiaowoBlueBlack)
			{
				Color32 result = slot switch
				{
					SkinSlot.Window => new Color32((byte)20, (byte)31, (byte)49, byte.MaxValue), 
					SkinSlot.Shutter => new Color32((byte)42, (byte)78, (byte)105, byte.MaxValue), 
					_ => new Color32((byte)12, (byte)18, (byte)31, byte.MaxValue), 
				};
				if (slot == SkinSlot.Shutter && y % 16 < 2)
				{
					return new Color32((byte)96, (byte)170, (byte)185, byte.MaxValue);
				}
				if (x % 24 == 0 || y % 24 == 0)
				{
					return new Color32((byte)27, (byte)48, (byte)71, byte.MaxValue);
				}
				if ((x + y * 2) % 97 < 3)
				{
					return new Color32((byte)35, (byte)73, (byte)91, byte.MaxValue);
				}
				return result;
			}
			Color32 result2 = slot switch
			{
				SkinSlot.Window => new Color32((byte)65, (byte)116, (byte)112, byte.MaxValue), 
				SkinSlot.Shutter => new Color32((byte)55, (byte)67, (byte)70, byte.MaxValue), 
				_ => new Color32((byte)39, (byte)34, (byte)50, byte.MaxValue), 
			};
			if (slot == SkinSlot.Shutter && y % 18 < 3)
			{
				return new Color32((byte)113, (byte)126, (byte)119, byte.MaxValue);
			}
			if ((x + y) % 71 < 2)
			{
				return new Color32((byte)92, (byte)77, (byte)91, byte.MaxValue);
			}
			if ((x / 8 + y / 8) % 11 == 0)
			{
				return new Color32((byte)76, (byte)137, (byte)130, byte.MaxValue);
			}
			return result2;
		}

		private static void ApplyAllKnownTargets(SkinSettings settings)
		{
			if (settings.Enabled && _assets != null)
			{
				ReconcileActiveScene();
				ApplyMainMenu();
				ApplyShutters();
				ReapplyKnownWindows();
			}
		}

		private static void ReconcileActiveScene()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			Scene activeScene = SceneManager.GetActiveScene();
			int handle = activeScene.handle;
			if (handle != _sceneHandle)
			{
				WindowStates.Clear();
				RendererStates.Clear();
				_shutterBlockerState = null;
				_menu = null;
				_menuBackground = null;
				_shutterDoor = null;
				_sceneHandle = handle;
			}
		}

		private static void ApplyMainMenu()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			Scene activeScene = SceneManager.GetActiveScene();
			if (activeScene.name != "EmporiumMenu")
			{
				return;
			}
			MainMenuUIController instance = MainMenuUIController.Instance;
			if ((Object)(object)instance == (Object)null || !Object.op_Implicit((Object)(object)instance))
			{
				return;
			}
			if ((Object)(object)_menu != (Object)(object)instance || (Object)(object)_menuBackground == (Object)null || !Object.op_Implicit((Object)(object)_menuBackground))
			{
				_menu = instance;
				_menuBackground = ResolveMainMenuBackground();
				if ((Object)(object)_menuBackground == (Object)null || !Object.op_Implicit((Object)(object)_menuBackground))
				{
					return;
				}
				TrackRenderer(_menuBackground);
			}
			ApplyRenderer(_menuBackground, _assets.MainMenu, _visible.MainMenuFit);
		}

		private static SpriteRenderer ResolveMainMenuBackground()
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0083: Unknown result type (might be due to invalid IL or missing references)
			//IL_0088: Unknown result type (might be due to invalid IL or missing references)
			//IL_008a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			GameObject val = GameObject.Find("MenuBackgroundNew");
			if ((Object)(object)val == (Object)null || !Object.op_Implicit((Object)(object)val))
			{
				return null;
			}
			SpriteRenderer result = null;
			float num = 0f;
			foreach (SpriteRenderer componentsInChild in val.GetComponentsInChildren<SpriteRenderer>(true))
			{
				if (!((Object)(object)componentsInChild == (Object)null) && Object.op_Implicit((Object)(object)componentsInChild) && !((Object)(object)componentsInChild.sprite == (Object)null) && Object.op_Implicit((Object)(object)componentsInChild.sprite))
				{
					Bounds bounds = componentsInChild.sprite.bounds;
					Vector3 size = bounds.size;
					float num2 = Math.Abs(size.x * ((Component)componentsInChild).transform.lossyScale.x * size.y * ((Component)componentsInChild).transform.lossyScale.y);
					if (!(num2 <= num))
					{
						num = num2;
						result = componentsInChild;
					}
				}
			}
			return result;
		}

		private static void ApplyShutters()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_0212: Unknown result type (might be due to invalid IL or missing references)
			//IL_0218: Unknown result type (might be due to invalid IL or missing references)
			//IL_0203: Unknown result type (might be due to invalid IL or missing references)
			//IL_023e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0244: Unknown result type (might be due to invalid IL or missing references)
			//IL_022a: Unknown result type (might be due to invalid IL or missing references)
			//IL_025b: Unknown result type (might be due to invalid IL or missing references)
			Scene activeScene = SceneManager.GetActiveScene();
			if (activeScene.name == "EmporiumMenu" || (Object)(object)_assets.Shutter == (Object)null || !Object.op_Implicit((Object)(object)_assets.Shutter))
			{
				return;
			}
			ApplyShutterBlocker();
			if ((Object)(object)_shutterDoor == (Object)null || !Object.op_Implicit((Object)(object)_shutterDoor))
			{
				_shutterDoor = FindSceneRenderer("ShutterDoor");
			}
			if ((Object)(object)_shutterDoor == (Object)null || !Object.op_Implicit((Object)(object)_shutterDoor))
			{
				return;
			}
			TrackRenderer(_shutterDoor);
			RendererState rendererState = RendererStates[((Object)_shutterDoor).GetInstanceID()];
			if ((Object)(object)rendererState.Sprite == (Object)null || !Object.op_Implicit((Object)(object)rendererState.Sprite))
			{
				return;
			}
			string key = ((Object)rendererState.Sprite).GetInstanceID() + ":" + ((Object)_assets.Shutter).GetInstanceID() + ":" + _visible.ShutterFit;
			if (!ShutterComposites.TryGetValue(key, out var value) || (Object)(object)value == (Object)null || !Object.op_Implicit((Object)(object)value))
			{
				try
				{
					value = ComposeShutter(rendererState.Sprite, _assets.Shutter, _visible.ShutterFit, cropped: false);
					ShutterComposites[key] = value;
					_lastShutterFailure = string.Empty;
				}
				catch (Exception ex)
				{
					if (_lastShutterFailure != ex.Message)
					{
						Instance log = Core.Log;
						if (log != null)
						{
							log.Warning("卷帘门纹理合成失败，保留原版门板：" + ex.Message);
						}
					}
					_lastShutterFailure = ex.Message;
					return;
				}
			}
			if ((Object)(object)_shutterDoor.sprite != (Object)(object)value)
			{
				_shutterDoor.sprite = value;
			}
			if (_shutterDoor.drawMode != rendererState.DrawMode)
			{
				_shutterDoor.drawMode = rendererState.DrawMode;
			}
			if (_shutterDoor.size != rendererState.Size)
			{
				_shutterDoor.size = rendererState.Size;
			}
			if (((Component)_shutterDoor).transform.localScale != rendererState.Scale)
			{
				((Component)_shutterDoor).transform.localScale = rendererState.Scale;
			}
		}

		private static void ApplyShutterBlocker()
		{
			//IL_0079: Unknown result type (might be due to invalid IL or missing references)
			Image val = _shutterBlockerState?.Image;
			if ((Object)(object)val == (Object)null || !Object.op_Implicit((Object)(object)val))
			{
				val = FindShutterBlocker();
				if ((Object)(object)val == (Object)null || !Object.op_Implicit((Object)(object)val) || (Object)(object)val.sprite == (Object)null || !Object.op_Implicit((Object)(object)val.sprite) || !((Object)val.sprite).name.StartsWith("shutter_door_cropped", StringComparison.Ordinal))
				{
					return;
				}
				_shutterBlockerState = new ImageState(val, val.sprite, val.type, val.preserveAspect);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("卷帘门皮肤已绑定到 UI 门板 " + ((Object)val.sprite).name + "。");
				}
			}
			Sprite sprite = _shutterBlockerState.Sprite;
			if ((Object)(object)sprite == (Object)null || !Object.op_Implicit((Object)(object)sprite))
			{
				return;
			}
			string key = ((Object)sprite).GetInstanceID() + ":" + ((Object)_assets.Shutter).GetInstanceID() + ":" + _visible.ShutterFit;
			if (!ShutterComposites.TryGetValue(key, out var value) || (Object)(object)value == (Object)null || !Object.op_Implicit((Object)(object)value))
			{
				try
				{
					value = ComposeShutter(sprite, _assets.Shutter, _visible.ShutterFit, cropped: true);
					ShutterComposites[key] = value;
					_lastShutterFailure = string.Empty;
				}
				catch (Exception ex)
				{
					if (_lastShutterFailure != ex.Message)
					{
						Instance log2 = Core.Log;
						if (log2 != null)
						{
							log2.Warning("卷帘门 UI 纹理合成失败，保留原版门板：" + ex.Message);
						}
					}
					_lastShutterFailure = ex.Message;
					return;
				}
			}
			if ((!((Object)(object)val.sprite != (Object)(object)sprite) || !((Object)(object)val.sprite != (Object)(object)value)) && (Object)(object)val.sprite != (Object)(object)value)
			{
				val.sprite = value;
			}
		}

		private static Image FindShutterBlocker()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			Scene activeScene = SceneManager.GetActiveScene();
			if (!activeScene.IsValid() || !activeScene.isLoaded)
			{
				return null;
			}
			foreach (GameObject item in (Il2CppArrayBase<GameObject>)(object)activeScene.GetRootGameObjects())
			{
				if (!((Object)(object)item == (Object)null) && Object.op_Implicit((Object)(object)item) && !(((Object)item).name != "WindowUICanvas"))
				{
					Transform val = item.transform.Find("WindowRootObject/Node/BlockerMask/Blocker");
					return ((Object)(object)val == (Object)null || !Object.op_Implicit((Object)(object)val)) ? null : ((Component)val).GetComponent<Image>();
				}
			}
			return null;
		}

		private static Sprite ComposeShutter(Sprite original, Sprite overlay, SkinFitMode mode, bool cropped)
		{
			//IL_0001: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cf: Expected Obj, but got Unknown
			//IL_00de: Unknown result type (might be due to invalid IL or missing references)
			//IL_0109: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			//IL_016f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0478: Unknown result type (might be due to invalid IL or missing references)
			//IL_047d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0484: Unknown result type (might be due to invalid IL or missing references)
			//IL_048b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0493: Unknown result type (might be due to invalid IL or missing references)
			//IL_04a0: Expected Obj, but got Unknown
			//IL_04b5: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c0: Unknown result type (might be due to invalid IL or missing references)
			//IL_04c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_04d0: Unknown result type (might be due to invalid IL or missing references)
			//IL_04db: Unknown result type (might be due to invalid IL or missing references)
			//IL_04e0: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ea: Unknown result type (might be due to invalid IL or missing references)
			//IL_04ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_0504: Unknown result type (might be due to invalid IL or missing references)
			//IL_026d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0272: Unknown result type (might be due to invalid IL or missing references)
			//IL_0274: Unknown result type (might be due to invalid IL or missing references)
			//IL_029f: Unknown result type (might be due to invalid IL or missing references)
			//IL_036a: Unknown result type (might be due to invalid IL or missing references)
			//IL_036f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0376: Unknown result type (might be due to invalid IL or missing references)
			//IL_0384: Unknown result type (might be due to invalid IL or missing references)
			//IL_0393: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_03ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_03db: Unknown result type (might be due to invalid IL or missing references)
			//IL_03f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0406: Unknown result type (might be due to invalid IL or missing references)
			//IL_0420: Unknown result type (might be due to invalid IL or missing references)
			//IL_0431: Unknown result type (might be due to invalid IL or missing references)
			//IL_0446: Unknown result type (might be due to invalid IL or missing references)
			//IL_044d: Unknown result type (might be due to invalid IL or missing references)
			Rect textureRect = original.textureRect;
			int num = (int)textureRect.width;
			int num2 = (int)textureRect.height;
			if (num < 1 || num2 < 1 || num > 4096 || num2 > 4096)
			{
				throw new InvalidOperationException("原版门板纹理尺寸无效。");
			}
			Texture2D val = null;
			Texture2D val2 = null;
			RenderTexture val3 = null;
			RenderTexture active = RenderTexture.active;
			try
			{
				val3 = RenderTexture.GetTemporary(num, num2, 0, (RenderTextureFormat)0);
				Graphics.Blit((Texture)(object)original.texture, val3, new Vector2(textureRect.width / (float)((Texture)original.texture).width, textureRect.height / (float)((Texture)original.texture).height), new Vector2(textureRect.x / (float)((Texture)original.texture).width, textureRect.y / (float)((Texture)original.texture).height));
				RenderTexture.active = val3;
				val = new Texture2D(num, num2, (TextureFormat)4, false, false);
				val.ReadPixels(new Rect(0f, 0f, (float)num, (float)num2), 0, 0, false);
				val.Apply(false, false);
				Il2CppStructArray<Color32> pixels = val.GetPixels32();
				Il2CppStructArray<Color32> pixels2 = overlay.texture.GetPixels32();
				Rect textureRect2 = overlay.textureRect;
				int num3 = (int)textureRect2.width;
				int num4 = (int)textureRect2.height;
				int num5 = (int)textureRect2.x;
				int num6 = (int)textureRect2.y;
				int width = ((Texture)overlay.texture).width;
				Il2CppStructArray<Color32> val4 = new Il2CppStructArray<Color32>((long)(num * num2));
				int num7 = num;
				int num8 = num2;
				int num9 = -1;
				int num10 = -1;
				for (int i = 0; i < num2; i++)
				{
					for (int j = 0; j < num; j++)
					{
						if (((Il2CppArrayBase<Color32>)(object)pixels)[i * num + j].a >= 16)
						{
							num7 = Math.Min(num7, j);
							num9 = Math.Max(num9, j);
							num8 = Math.Min(num8, i);
							num10 = Math.Max(num10, i);
						}
					}
				}
				if (num9 < num7 || num10 < num8)
				{
					throw new InvalidOperationException("原版门板没有可见像素。");
				}
				int num11 = Math.Max(2, (num9 - num7 + 1) / (cropped ? 100 : 12));
				int num12 = Math.Max(2, (num10 - num8 + 1) / (cropped ? 100 : 12));
				int num13 = num7 + num11;
				int num14 = num8 + num12;
				int num15 = Math.Max(1, num9 - num7 + 1 - num11 * 2);
				int num16 = Math.Max(1, num10 - num8 + 1 - num12 * 2);
				float num17 = (float)num15 / (float)num16;
				float num18 = (float)num3 / (float)num4;
				for (int k = 0; k < num2; k++)
				{
					for (int l = 0; l < num; l++)
					{
						int num19 = k * num + l;
						Color32 val5 = ((Il2CppArrayBase<Color32>)(object)pixels)[num19];
						if (val5.a == 0 || l < num13 || l >= num13 + num15 || k < num14 || k >= num14 + num16)
						{
							((Il2CppArrayBase<Color32>)(object)val4)[num19] = val5;
							continue;
						}
						float num20 = ((float)(l - num13) + 0.5f) / (float)num15;
						float num21 = ((float)(k - num14) + 0.5f) / (float)num16;
						switch (mode)
						{
						case SkinFitMode.Cover:
							if (num18 > num17)
							{
								num20 = 0.5f + (num20 - 0.5f) * num17 / num18;
							}
							else
							{
								num21 = 0.5f + (num21 - 0.5f) * num18 / num17;
							}
							break;
						case SkinFitMode.Tile:
							num20 = ((float)((l - num13) % num3) + 0.5f) / (float)num3;
							num21 = ((float)((k - num14) % num4) + 0.5f) / (float)num4;
							break;
						}
						int num22 = num5 + Math.Min(num3 - 1, (int)(num20 * (float)num3));
						int num23 = num6 + Math.Min(num4 - 1, (int)(num21 * (float)num4));
						Color32 val6 = ((Il2CppArrayBase<Color32>)(object)pixels2)[num23 * width + num22];
						float num24 = 0.8f + ((float)(int)val5.r * 0.2126f + (float)(int)val5.g * 0.7152f + (float)(int)val5.b * 0.0722f) / 255f * 0.65f;
						float num25 = (float)(int)val6.a / 255f;
						((Il2CppArrayBase<Color32>)(object)val4)[num19] = new Color32((byte)Math.Min(255f, (float)(int)val5.r * (1f - num25) + (float)(int)val6.r * num24 * num25), (byte)Math.Min(255f, (float)(int)val5.g * (1f - num25) + (float)(int)val6.g * num24 * num25), (byte)Math.Min(255f, (float)(int)val5.b * (1f - num25) + (float)(int)val6.b * num24 * num25), val5.a);
					}
				}
				val2 = new Texture2D(num, num2, (TextureFormat)4, false, false)
				{
					filterMode = (FilterMode)0,
					wrapMode = (TextureWrapMode)1,
					hideFlags = (HideFlags)61,
					name = "skin.shutter.composite.texture"
				};
				val2.SetPixels32(val4);
				val2.Apply(false, false);
				float x = original.pivot.x;
				Rect rect = original.rect;
				float num26 = x / rect.width;
				float y = original.pivot.y;
				rect = original.rect;
				Vector2 val7 = new Vector2(num26, y / rect.height);
				Sprite val8 = Sprite.Create(val2, new Rect(0f, 0f, (float)num, (float)num2), val7, original.pixelsPerUnit);
				((Object)val8).name = "skin.shutter.composite";
				((Object)val8).hideFlags = (HideFlags)61;
				val2 = null;
				return val8;
			}
			finally
			{
				RenderTexture.active = active;
				if ((Object)(object)val3 != (Object)null)
				{
					RenderTexture.ReleaseTemporary(val3);
				}
				if ((Object)(object)val != (Object)null && Object.op_Implicit((Object)(object)val))
				{
					Object.Destroy((Object)(object)val);
				}
				if ((Object)(object)val2 != (Object)null && Object.op_Implicit((Object)(object)val2))
				{
					Object.Destroy((Object)(object)val2);
				}
			}
		}

		private static void ClearShutterComposites()
		{
			foreach (Sprite value in ShutterComposites.Values)
			{
				DestroySpriteAndTexture(value);
			}
			ShutterComposites.Clear();
			_lastShutterFailure = string.Empty;
		}

		private static SpriteRenderer FindSceneRenderer(string objectName)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			Scene activeScene = SceneManager.GetActiveScene();
			if (!activeScene.IsValid() || !activeScene.isLoaded)
			{
				return null;
			}
			Il2CppReferenceArray<GameObject> rootGameObjects = activeScene.GetRootGameObjects();
			if (rootGameObjects == null)
			{
				return null;
			}
			foreach (GameObject item in (Il2CppArrayBase<GameObject>)(object)rootGameObjects)
			{
				if (!((Object)(object)item == (Object)null) && Object.op_Implicit((Object)(object)item))
				{
					SpriteRenderer val = FindRendererRecursive(item.transform, objectName);
					if ((Object)(object)val != (Object)null && Object.op_Implicit((Object)(object)val))
					{
						return val;
					}
				}
			}
			return null;
		}

		private static SpriteRenderer FindRendererRecursive(Transform current, string objectName)
		{
			if ((Object)(object)current == (Object)null || !Object.op_Implicit((Object)(object)current))
			{
				return null;
			}
			if (string.Equals(((Object)((Component)current).gameObject).name, objectName, StringComparison.Ordinal))
			{
				SpriteRenderer component = ((Component)current).gameObject.GetComponent<SpriteRenderer>();
				if ((Object)(object)component != (Object)null && Object.op_Implicit((Object)(object)component))
				{
					return component;
				}
			}
			for (int i = 0; i < current.childCount; i++)
			{
				SpriteRenderer val = FindRendererRecursive(current.GetChild(i), objectName);
				if ((Object)(object)val != (Object)null && Object.op_Implicit((Object)(object)val))
				{
					return val;
				}
			}
			return null;
		}

		private static void TrackRenderer(SpriteRenderer renderer)
		{
			//IL_0027: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0033: Unknown result type (might be due to invalid IL or missing references)
			int instanceID = ((Object)renderer).GetInstanceID();
			if (!RendererStates.ContainsKey(instanceID))
			{
				RendererStates[instanceID] = new RendererState(renderer, renderer.sprite, ((Component)renderer).transform.localScale, renderer.drawMode, renderer.size);
			}
		}

		private static void ApplyRenderer(SpriteRenderer renderer, Sprite source, SkinFitMode mode)
		{
			//IL_0064: Unknown result type (might be due to invalid IL or missing references)
			//IL_007f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0080: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0076: Unknown result type (might be due to invalid IL or missing references)
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
			//IL_008d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0093: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ac: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_0129: Unknown result type (might be due to invalid IL or missing references)
			//IL_012e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0132: Unknown result type (might be due to invalid IL or missing references)
			//IL_0137: Unknown result type (might be due to invalid IL or missing references)
			//IL_0139: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bf: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d1: Invalid comparison between Unknown and I4
			//IL_019c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0147: Unknown result type (might be due to invalid IL or missing references)
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_0162: Unknown result type (might be due to invalid IL or missing references)
			//IL_0169: Unknown result type (might be due to invalid IL or missing references)
			//IL_0172: Unknown result type (might be due to invalid IL or missing references)
			//IL_017c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0183: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0196: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00f5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ca: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0111: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ff: Unknown result type (might be due to invalid IL or missing references)
			//IL_01d9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0120: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)renderer == (Object)null || !Object.op_Implicit((Object)(object)renderer) || (Object)(object)source == (Object)null || !Object.op_Implicit((Object)(object)source))
			{
				return;
			}
			TrackRenderer(renderer);
			RendererState rendererState = RendererStates[((Object)renderer).GetInstanceID()];
			Vector3 val;
			Bounds bounds;
			if (!((Object)(object)rendererState.Sprite != (Object)null) || !Object.op_Implicit((Object)(object)rendererState.Sprite))
			{
				val = new Vector3(1f, 1f, 0f);
			}
			else
			{
				bounds = rendererState.Sprite.bounds;
				val = bounds.size;
			}
			Vector3 val2 = val;
			float targetAspect = ((val2.y <= 0.001f) ? 1f : (val2.x / val2.y));
			Sprite val3 = FitSprite(source, targetAspect, mode);
			Vector3 val4 = rendererState.Scale;
			if (mode == SkinFitMode.Tile)
			{
				Vector2 val5 = new Vector2(val2.x, val2.y);
				if ((int)renderer.drawMode != 2)
				{
					renderer.drawMode = (SpriteDrawMode)2;
				}
				if ((Object)(object)renderer.sprite != (Object)(object)val3)
				{
					renderer.sprite = val3;
				}
				if (renderer.size != val5)
				{
					renderer.size = val5;
				}
				if (((Component)renderer).transform.localScale != val4)
				{
					((Component)renderer).transform.localScale = val4;
				}
				return;
			}
			bounds = val3.bounds;
			Vector3 size = bounds.size;
			if (size.x > 0.001f && size.y > 0.001f)
			{
				val4 = new Vector3(rendererState.Scale.x * val2.x / size.x, rendererState.Scale.y * val2.y / size.y, rendererState.Scale.z);
			}
			if ((int)renderer.drawMode != 0)
			{
				renderer.drawMode = (SpriteDrawMode)0;
			}
			if ((Object)(object)renderer.sprite != (Object)(object)val3)
			{
				renderer.sprite = val3;
			}
			if (((Component)renderer).transform.localScale != val4)
			{
				((Component)renderer).transform.localScale = val4;
			}
		}

		private static void ApplyWindowImage(Image image, Sprite source, SkinFitMode mode)
		{
			//IL_0060: Unknown result type (might be due to invalid IL or missing references)
			//IL_0065: Unknown result type (might be due to invalid IL or missing references)
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b4: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_00be: Unknown result type (might be due to invalid IL or missing references)
			if (!((Object)(object)image == (Object)null) && Object.op_Implicit((Object)(object)image) && !((Object)(object)source == (Object)null) && Object.op_Implicit((Object)(object)source))
			{
				int instanceID = ((Object)image).GetInstanceID();
				if (!WindowStates.ContainsKey(instanceID))
				{
					WindowStates[instanceID] = new ImageState(image, image.sprite, image.type, image.preserveAspect);
				}
				Rect rect = ((Graphic)image).rectTransform.rect;
				float targetAspect = ((rect.height <= 0.001f) ? 1f : (rect.width / rect.height));
				Sprite val = FitSprite(source, targetAspect, mode);
				Type val2 = (Type)((mode == SkinFitMode.Tile) ? 2 : 0);
				if ((Object)(object)image.sprite != (Object)(object)val)
				{
					image.sprite = val;
				}
				if (image.type != val2)
				{
					image.type = val2;
				}
				if (image.preserveAspect)
				{
					image.preserveAspect = false;
				}
			}
		}

		private static Sprite FitSprite(Sprite source, float targetAspect, SkinFitMode mode)
		{
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0071: Unknown result type (might be due to invalid IL or missing references)
			//IL_0103: Unknown result type (might be due to invalid IL or missing references)
			//IL_010e: Unknown result type (might be due to invalid IL or missing references)
			if (mode != SkinFitMode.Cover || (Object)(object)source == (Object)null || !Object.op_Implicit((Object)(object)source) || targetAspect <= 0.001f)
			{
				return source;
			}
			string key = ((Object)source).GetInstanceID() + ":" + Math.Round(targetAspect, 3);
			if (FittedSprites.TryGetValue(key, out var value) && (Object)(object)value != (Object)null && Object.op_Implicit((Object)(object)value))
			{
				return value;
			}
			Rect rect = source.rect;
			float num = rect.width / rect.height;
			if (Math.Abs(num - targetAspect) < 0.002f)
			{
				return source;
			}
			if (num > targetAspect)
			{
				float num2 = rect.height * targetAspect;
				rect.x += (rect.width - num2) * 0.5f;
				rect.width = num2;
			}
			else
			{
				float num3 = rect.width / targetAspect;
				rect.y += (rect.height - num3) * 0.5f;
				rect.height = num3;
			}
			Sprite val = Sprite.Create(source.texture, rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit);
			((Object)val).name = ((Object)source).name + ".cover." + Math.Round(targetAspect, 3);
			((Object)val).hideFlags = (HideFlags)61;
			FittedSprites[key] = val;
			return val;
		}

		private static void ReapplyKnownWindows()
		{
			List<int> list = null;
			foreach (KeyValuePair<int, ImageState> windowState in WindowStates)
			{
				if ((Object)(object)windowState.Value.Image == (Object)null || !Object.op_Implicit((Object)(object)windowState.Value.Image))
				{
					(list ?? (list = new List<int>())).Add(windowState.Key);
				}
				else
				{
					ApplyWindowImage(windowState.Value.Image, _assets.Window, _visible.WindowFit);
				}
			}
			if (list == null)
			{
				return;
			}
			foreach (int item in list)
			{
				WindowStates.Remove(item);
			}
		}

		private static bool TryEnableWindowHook(out string message)
		{
			//IL_0015: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Expected Obj, but got Unknown
			//IL_0045: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Expected Obj, but got Unknown
			if (_windowHarmony != null)
			{
				message = string.Empty;
				return true;
			}
			Harmony val = new Harmony("XIAOWOTradePerks.runtime-skins");
			try
			{
				MethodInfo? method = typeof(SkinManager).GetMethod("OnWindowShown", BindingFlags.Static | BindingFlags.NonPublic);
				if (method == null)
				{
					throw new MissingMethodException("OnWindowShown");
				}
				HarmonyMethod val2 = new HarmonyMethod(method);
				int num = 0;
				MethodInfo[] methods = typeof(PixelWindow).GetMethods();
				foreach (MethodInfo methodInfo in methods)
				{
					if (!(methodInfo.Name != "Show"))
					{
						val.Patch((MethodBase)methodInfo, (HarmonyMethod)null, val2, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
						num++;
					}
				}
				if (num == 0)
				{
					throw new MissingMethodException(typeof(PixelWindow).FullName, "Show");
				}
				_windowHarmony = val;
				message = string.Empty;
				return true;
			}
			catch (Exception ex)
			{
				val.UnpatchSelf();
				message = Select("窗口皮肤挂接失败：", "Window skin hook failed: ") + ex.Message;
				return false;
			}
		}

		private static void DisableWindowHook()
		{
			if (_windowHarmony == null)
			{
				return;
			}
			try
			{
				_windowHarmony.UnpatchSelf();
			}
			catch (Exception ex)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("窗口皮肤补丁清理失败：" + ex.Message);
				}
			}
			_windowHarmony = null;
		}

		private static void OnWindowShown(PixelWindow __result)
		{
			ApplyWindow(__result);
		}

		private static void RestoreTargets()
		{
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b6: Unknown result type (might be due to invalid IL or missing references)
			//IL_014d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0160: Unknown result type (might be due to invalid IL or missing references)
			//IL_0173: Unknown result type (might be due to invalid IL or missing references)
			if ((Object)(object)_shutterBlockerState?.Image != (Object)null && Object.op_Implicit((Object)(object)_shutterBlockerState.Image))
			{
				ImageState shutterBlockerState = _shutterBlockerState;
				shutterBlockerState.Image.sprite = shutterBlockerState.Sprite;
				shutterBlockerState.Image.type = shutterBlockerState.Type;
				shutterBlockerState.Image.preserveAspect = shutterBlockerState.PreserveAspect;
			}
			_shutterBlockerState = null;
			foreach (ImageState value in WindowStates.Values)
			{
				if (!((Object)(object)value.Image == (Object)null) && Object.op_Implicit((Object)(object)value.Image))
				{
					value.Image.sprite = value.Sprite;
					value.Image.type = value.Type;
					value.Image.preserveAspect = value.PreserveAspect;
				}
			}
			WindowStates.Clear();
			foreach (RendererState value2 in RendererStates.Values)
			{
				if (!((Object)(object)value2.Renderer == (Object)null) && Object.op_Implicit((Object)(object)value2.Renderer))
				{
					value2.Renderer.sprite = value2.Sprite;
					((Component)value2.Renderer).transform.localScale = value2.Scale;
					value2.Renderer.drawMode = value2.DrawMode;
					value2.Renderer.size = value2.Size;
				}
			}
			RendererStates.Clear();
			_menu = null;
			_menuBackground = null;
			_shutterDoor = null;
		}

		private static void ReplaceAssets(AssetSet assets)
		{
			ClearShutterComposites();
			foreach (Sprite value in FittedSprites.Values)
			{
				if ((Object)(object)value != (Object)null && Object.op_Implicit((Object)(object)value))
				{
					Object.Destroy((Object)(object)value);
				}
			}
			FittedSprites.Clear();
			DestroyAssets(_assets);
			_assets = assets;
		}

		private static void DestroyAssets(AssetSet assets)
		{
			if (assets != null)
			{
				DestroySpriteAndTexture(assets.MainMenu);
				DestroySpriteAndTexture(assets.Window);
				DestroySpriteAndTexture(assets.Shutter);
			}
		}

		private static void DestroySpriteAndTexture(Sprite sprite)
		{
			if (!((Object)(object)sprite == (Object)null) && Object.op_Implicit((Object)(object)sprite))
			{
				Texture2D texture = sprite.texture;
				Object.Destroy((Object)(object)sprite);
				if ((Object)(object)texture != (Object)null && Object.op_Implicit((Object)(object)texture))
				{
					Object.Destroy((Object)(object)texture);
				}
			}
		}

		private static string SlotName(SkinSlot slot)
		{
			return slot switch
			{
				SkinSlot.MainMenu => Select("主菜单背景", "Main menu background"), 
				SkinSlot.Window => Select("游戏窗口背景", "Game window background"), 
				_ => Select("卷帘门", "Shutter"), 
			};
		}

		private static string Select(string chinese, string english)
		{
			return ModLocalization.Select(chinese, english);
		}

		[DllImport("comdlg32.dll", CharSet = CharSet.Unicode, EntryPoint = "GetOpenFileNameW", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		private static extern bool GetOpenFileName(ref OpenFileName openFileName);
	}
	internal static class SkinUI
	{
		internal const string WindowId = "xiaowo_custom_skins";

		internal const string ButtonName = "XIAOWOTradePerksSkinButton";

		private static GameObject _menuButtonObject;

		private static Button _menuButton;

		private static UnityAction _openAction;

		private static SkinSettings _draft;

		private static string _notice = string.Empty;

		private static int _checkCountdown;

		private static string _lastFailure = string.Empty;

		private static readonly List<Action> ButtonActions = new List<Action>();

		private static readonly List<Action<string>> InputActions = new List<Action<string>>();

		private static readonly List<Action<int>> DropdownActions = new List<Action<int>>();

		internal static void Tick()
		{
			if (_checkCountdown-- > 0)
			{
				return;
			}
			_checkCountdown = 15;
			try
			{
				MainMenuUIController instance = MainMenuUIController.Instance;
				if (!IsMainMenuVisible(instance))
				{
					ForgetButton();
					return;
				}
				ConfigureMenuButton(instance);
				_lastFailure = string.Empty;
			}
			catch (Exception ex)
			{
				string text = ex.GetType().Name + ": " + ex.Message;
				if (!string.Equals(text, _lastFailure, StringComparison.Ordinal))
				{
					_lastFailure = text;
					Instance log = Core.Log;
					if (log != null)
					{
						log.Warning("自定义皮肤按钮无法创建：" + text);
					}
				}
			}
		}

		internal static void Open()
		{
			try
			{
				if (IsMainMenuVisible(MainMenuUIController.Instance))
				{
					MainMenuUIController.Instance.OnAnyButtonClick();
					_draft = SkinManager.Current;
					_draft.Enabled = true;
					_notice = string.Empty;
					BuildWindow();
				}
			}
			catch (Exception ex)
			{
				_notice = Select("无法打开自定义皮肤：", "Could not open custom skins: ") + ex.Message;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Warning("Custom skin window could not be opened: " + ex);
				}
			}
		}

		internal static void Dispose()
		{
			CloseWindow(restoreSaved: false);
			if (IsAlive((Object)(object)_menuButtonObject))
			{
				Object.Destroy((Object)(object)_menuButtonObject);
			}
			ForgetButton();
			ButtonActions.Clear();
			InputActions.Clear();
			DropdownActions.Clear();
			_draft = null;
			_notice = string.Empty;
			_lastFailure = string.Empty;
			_checkCountdown = 0;
		}

		internal static string BuildStatus()
		{
			CustomUIManager instance = CustomUIManager.Instance;
			CustomUIWindow val = (IsAlive((Object)(object)instance) ? instance.GetWindow("xiaowo_custom_skins") : null);
			MainMenuUIController instance2 = MainMenuUIController.Instance;
			GameObject val2 = ((instance2 != null) ? instance2.moddingButton : null);
			if (!IsAlive((Object)(object)_menuButtonObject) && IsAlive((Object)(object)((val2 != null) ? val2.transform.parent : null)))
			{
				Transform val3 = val2.transform.parent.Find("XIAOWOTradePerksSkinButton");
				if (IsAlive((Object)(object)val3))
				{
					_menuButtonObject = ((Component)val3).gameObject;
					_menuButton = _menuButtonObject.GetComponent<Button>() ?? _menuButtonObject.GetComponentInChildren<Button>(true);
				}
			}
			TextMeshProUGUI val4 = (IsAlive((Object)(object)_menuButtonObject) ? _menuButtonObject.GetComponentInChildren<TextMeshProUGUI>(true) : null);
			bool flag = IsAlive((Object)(object)val2) && IsAlive((Object)(object)_menuButtonObject) && (Object)(object)_menuButtonObject.transform.parent == (Object)(object)val2.transform.parent && _menuButtonObject.transform.GetSiblingIndex() == val2.transform.GetSiblingIndex() + 1;
			return "SKIN_UI\nbuttonVisible=" + (IsAlive((Object)(object)_menuButtonObject) && _menuButtonObject.activeInHierarchy) + "\nbuttonText=" + (((val4 != null) ? ((TMP_Text)val4).text : null) ?? "missing") + "\nimmediatelyAfterMod=" + flag + "\nwindowOpen=" + (val != null && val.IsVisible);
		}

		internal static bool ValidateDefinition(out string details)
		{
			details = "window=xiaowo_custom_skins, button=XIAOWOTradePerksSkinButton";
			return true;
		}

		private static void ConfigureMenuButton(MainMenuUIController menu)
		{
			//IL_0114: Unknown result type (might be due to invalid IL or missing references)
			//IL_011e: Expected Obj, but got Unknown
			GameObject moddingButton = menu.moddingButton;
			if (!IsAlive((Object)(object)moddingButton))
			{
				throw new InvalidOperationException("The original modding button is unavailable.");
			}
			Transform parent = moddingButton.transform.parent;
			if (!IsAlive((Object)(object)parent))
			{
				throw new InvalidOperationException("The main-menu button group is unavailable.");
			}
			if (!IsAlive((Object)(object)_menuButtonObject))
			{
				Transform val = parent.Find("XIAOWOTradePerksSkinButton");
				if (IsAlive((Object)(object)val))
				{
					_menuButtonObject = ((Component)val).gameObject;
					_menuButton = _menuButtonObject.GetComponent<Button>() ?? _menuButtonObject.GetComponentInChildren<Button>(true);
				}
			}
			if (!IsAlive((Object)(object)_menuButtonObject) || (Object)(object)_menuButtonObject.transform.parent != (Object)(object)parent)
			{
				if (IsAlive((Object)(object)_menuButtonObject))
				{
					Object.Destroy((Object)(object)_menuButtonObject);
				}
				_menuButtonObject = Object.Instantiate<GameObject>(moddingButton, parent, false);
				((Object)_menuButtonObject).name = "XIAOWOTradePerksSkinButton";
				_menuButton = _menuButtonObject.GetComponent<Button>() ?? _menuButtonObject.GetComponentInChildren<Button>(true);
				if (!IsAlive((Object)(object)_menuButton))
				{
					throw new InvalidOperationException("The cloned skin button is incomplete.");
				}
				_menuButton.onClick = new ButtonClickedEvent();
				if (_openAction == null)
				{
					_openAction = UnityAction.op_Implicit((Action)Open);
				}
				((UnityEvent)_menuButton.onClick).AddListener(_openAction);
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg("Main-menu custom skin button created.");
				}
			}
			TextMeshProUGUI componentInChildren = _menuButtonObject.GetComponentInChildren<TextMeshProUGUI>(true);
			if (!IsAlive((Object)(object)componentInChildren))
			{
				throw new InvalidOperationException("The cloned skin button label is missing.");
			}
			((TMP_Text)componentInChildren).text = Select("自定义皮肤", "CUSTOM SKINS");
			((Selectable)_menuButton).interactable = true;
			_menuButtonObject.SetActive(true);
			GameObject val2 = FindExitButton(parent);
			if (!IsAlive((Object)(object)val2))
			{
				throw new InvalidOperationException("The original exit button is unavailable.");
			}
			PlaceImmediatelyBefore(_menuButtonObject.transform, val2.transform);
			PlaceImmediatelyBefore(moddingButton.transform, _menuButtonObject.transform);
		}

		private static void BuildWindow()
		{
			//IL_0452: Unknown result type (might be due to invalid IL or missing references)
			//IL_045c: Expected Obj, but got Unknown
			CustomUIManager instance = CustomUIManager.Instance;
			if (!IsAlive((Object)(object)instance))
			{
				throw new InvalidOperationException("Custom UI manager is unavailable.");
			}
			instance.CloseWindow("xiaowo_custom_skins");
			SkinManager.ClearPreviewSprites();
			ButtonActions.Clear();
			InputActions.Clear();
			DropdownActions.Clear();
			if (_draft == null)
			{
				_draft = SkinManager.Current;
			}
			CustomUIBuilder val = CustomUIManager.Window("xiaowo_custom_skins", Select("自定义皮肤", "CUSTOM SKINS"), "overlay").SetSize(920f, 700f).Center()
				.SetCloseOnEscape(false)
				.SetDraggable(true);
			val.AddLabel(Select("可混合使用预设和自定义图片。自定义图片优先于预设，按“应用”后保存。", "Mix presets with local images. Local images override presets and save only after APPLY."), "summary");
			ConfigureLabel(val.Last, 15f, (FontStyles)0, 880f, 30f);
			val.AddLabel(_notice, "notice");
			ConfigureLabel(val.Last, 14f, (FontStyles)1, 880f, 28f);
			val.BeginRow(8f);
			val.AddLabel(Select("预设方案", "PRESET"), (string)null);
			ConfigureLabel(val.Last, 16f, (FontStyles)1, 150f, 38f);
			Action<int> val2 = ToAction((int index) =>
			{
				_draft.Preset = (SkinPreset)Math.Clamp(index, 0, 1);
				BuildWindow();
			});
			DropdownActions.Add(val2);
			val.AddDropdown(Options(Select("原版增强", "ORIGINAL ENHANCED"), Select("小窝蓝黑", "XIAOWO BLUE-BLACK")), (int)_draft.Preset, val2, "preset");
			val.Last.SetPreferredSize(310f, 38f);
			Action val3 = ToAction(() =>
			{
				try
				{
					SkinManager.OpenSkinFolder();
				}
				catch (Exception ex)
				{
					_notice = Select("无法打开皮肤文件夹：", "Could not open skin folder: ") + ex.Message;
					BuildWindow();
				}
			});
			ButtonActions.Add(val3);
			val.AddButton(Select("打开皮肤文件夹", "OPEN SKIN FOLDER"), val3, "folder");
			val.Last.SetPreferredSize(220f, 38f);
			val.End();
			val.BeginScroll(465f);
			AddSlot(val, SkinSlot.MainMenu, Select("主菜单背景", "MAIN MENU BACKGROUND"), _draft.MainMenuPath, _draft.MainMenuFit);
			AddSlot(val, SkinSlot.Window, Select("游戏窗口背景", "GAME WINDOW BACKGROUND"), _draft.WindowPath, _draft.WindowFit);
			AddSlot(val, SkinSlot.Shutter, Select("卷帘门", "SHUTTER"), _draft.ShutterPath, _draft.ShutterFit);
			val.End();
			val.BeginRow(8f);
			Action val4 = ToAction(() =>
			{
				SkinManager.Preview(_draft, out _notice);
				BuildWindow();
			});
			Action val5 = ToAction(() =>
			{
				SkinManager.RestoreOriginal(out _notice);
				_draft = SkinManager.Current;
				BuildWindow();
			});
			Action val6 = ToAction(() =>
			{
				if (SkinManager.Apply(_draft, out _notice))
				{
					_draft = SkinManager.Current;
				}
				BuildWindow();
			});
			Action val7 = ToAction(() =>
			{
				CloseWindow(restoreSaved: true);
			});
			ButtonActions.Add(val4);
			ButtonActions.Add(val5);
			ButtonActions.Add(val6);
			ButtonActions.Add(val7);
			val.AddButton(Select("预览", "PREVIEW"), val4, "preview");
			val.AddButton(Select("恢复原版", "RESTORE ORIGINAL"), val5, "restore");
			val.AddButton(Select("应用", "APPLY"), val6, "apply");
			val.AddButton(Select("关闭", "CLOSE"), val7, "close");
			val.Last.SetPreferredSize(170f, 42f);
			val.End();
			CustomUIWindow val8 = val.Show();
			object value;
			if (val8 == null)
			{
				value = null;
			}
			else
			{
				CustomUIWindowFrame frame = val8.frame;
				value = ((frame != null) ? frame.closeButton : null);
			}
			if (IsAlive((Object)value))
			{
				val8.frame.closeButton.onClick = new ButtonClickedEvent();
				((UnityEvent)val8.frame.closeButton.onClick).AddListener(UnityAction.op_Implicit((Action)(() =>
				{
					CloseWindow(restoreSaved: true);
				})));
			}
		}

		private static void AddSlot(CustomUIBuilder builder, SkinSlot slot, string label, string path, SkinFitMode fit)
		{
			builder.AddLabel(label, (string)null);
			ConfigureLabel(builder.Last, 17f, (FontStyles)1, 850f, 27f);
			builder.BeginRow(7f);
			builder.AddImage(SkinManager.PreviewSprite(_draft, slot), 150f, 84f, "preview_" + slot);
			builder.Last.SetPreferredSize(150f, 84f);
			SkinSlot capturedSlot = slot;
			Action<string> val = ToInputAction((string value) =>
			{
				SetPath(capturedSlot, value ?? string.Empty);
			});
			InputActions.Add(val);
			builder.AddInput(Select("留空则使用预设", "Leave empty to use preset"), path ?? string.Empty, val, "path_" + slot);
			builder.Last.SetPreferredSize(430f, 39f);
			Action val2 = ToAction(() =>
			{
				if (SkinManager.TryChooseImage(out var path2))
				{
					SetPath(capturedSlot, path2);
					BuildWindow();
				}
			});
			ButtonActions.Add(val2);
			builder.AddButton(Select("选择", "BROWSE"), val2, "browse_" + slot);
			builder.Last.SetPreferredSize(95f, 39f);
			Action val3 = ToAction(() =>
			{
				SetPath(capturedSlot, string.Empty);
				BuildWindow();
			});
			ButtonActions.Add(val3);
			builder.AddButton(Select("清除", "CLEAR"), val3, "clear_" + slot);
			builder.Last.SetPreferredSize(80f, 39f);
			builder.End();
			builder.BeginRow(7f);
			builder.AddLabel(Select("填充方式", "FIT MODE"), (string)null);
			ConfigureLabel(builder.Last, 14f, (FontStyles)0, 150f, 36f);
			Action<int> val4 = ToAction((int index) =>
			{
				SetFit(capturedSlot, (SkinFitMode)Math.Clamp(index, 0, 2));
			});
			DropdownActions.Add(val4);
			builder.AddDropdown(Options(Select("覆盖", "COVER"), Select("拉伸", "STRETCH"), Select("平铺", "TILE")), (int)fit, val4, "fit_" + slot);
			builder.Last.SetPreferredSize(260f, 36f);
			builder.End();
		}

		private static void SetPath(SkinSlot slot, string value)
		{
			if (_draft != null)
			{
				switch (slot)
				{
				case SkinSlot.MainMenu:
					_draft.MainMenuPath = value;
					break;
				case SkinSlot.Window:
					_draft.WindowPath = value;
					break;
				default:
					_draft.ShutterPath = value;
					break;
				}
			}
		}

		private static void SetFit(SkinSlot slot, SkinFitMode value)
		{
			if (_draft != null)
			{
				switch (slot)
				{
				case SkinSlot.MainMenu:
					_draft.MainMenuFit = value;
					break;
				case SkinSlot.Window:
					_draft.WindowFit = value;
					break;
				default:
					_draft.ShutterFit = value;
					break;
				}
			}
		}

		private static void CloseWindow(bool restoreSaved)
		{
			if (restoreSaved)
			{
				SkinManager.RevertPreview();
			}
			CustomUIManager instance = CustomUIManager.Instance;
			if (IsAlive((Object)(object)instance))
			{
				instance.CloseWindow("xiaowo_custom_skins");
			}
			SkinManager.ClearPreviewSprites();
			_draft = null;
			_notice = string.Empty;
		}

		private static IList<string> Options(params string[] values)
		{
			List<string> val = new List<string>();
			foreach (string text in values)
			{
				val.Add(text);
			}
			return new IList<string>(((Il2CppObjectBase)val).Pointer);
		}

		private static void ConfigureLabel(CustomUIElement element, float size, FontStyles style, float width, float height)
		{
			//IL_002f: Unknown result type (might be due to invalid IL or missing references)
			if (element != null)
			{
				element.SetPreferredSize(width, height);
				if (!((Object)(object)element.text == (Object)null))
				{
					((TMP_Text)element.text).fontSize = size;
					((TMP_Text)element.text).fontStyle = style;
					((TMP_Text)element.text).enableWordWrapping = false;
					((TMP_Text)element.text).overflowMode = (TextOverflowModes)1;
				}
			}
		}

		private static Action ToAction(Action action)
		{
			return DelegateSupport.ConvertDelegate<Action>((Delegate)action);
		}

		private static Action<string> ToInputAction(Action<string> action)
		{
			return DelegateSupport.ConvertDelegate<Action<string>>((Delegate)action);
		}

		private static Action<int> ToAction(Action<int> action)
		{
			return DelegateSupport.ConvertDelegate<Action<int>>((Delegate)action);
		}

		private static string Select(string chinese, string english)
		{
			return ModLocalization.Select(chinese, english);
		}

		private static bool IsAlive(Object value)
		{
			if (value != (Object)null)
			{
				return Object.op_Implicit(value);
			}
			return false;
		}

		private static bool IsMainMenuVisible(MainMenuUIController menu)
		{
			if (IsAlive((Object)(object)menu) && ((Component)menu).gameObject.activeInHierarchy && IsAlive((Object)(object)menu.moddingButton) && IsAlive((Object)(object)menu.moddingButton.transform.parent))
			{
				return ((Component)menu.moddingButton.transform.parent).gameObject.activeInHierarchy;
			}
			return false;
		}

		private static GameObject FindExitButton(Transform parent)
		{
			if (!IsAlive((Object)(object)parent))
			{
				return null;
			}
			for (int i = 0; i < parent.childCount; i++)
			{
				Transform child = parent.GetChild(i);
				Button val = ((child != null) ? ((Component)child).gameObject.GetComponent<Button>() : null);
				if (!IsAlive((Object)(object)val))
				{
					continue;
				}
				for (int j = 0; j < ((UnityEventBase)val.onClick).GetPersistentEventCount(); j++)
				{
					if (string.Equals(((UnityEventBase)val.onClick).GetPersistentMethodName(j), "OnExitClick", StringComparison.Ordinal))
					{
						return ((Component)child).gameObject;
					}
				}
			}
			return null;
		}

		private static void PlaceImmediatelyBefore(Transform source, Transform target)
		{
			if (IsAlive((Object)(object)source) && IsAlive((Object)(object)target) && !((Object)(object)source.parent != (Object)(object)target.parent))
			{
				int siblingIndex = source.GetSiblingIndex();
				int siblingIndex2 = target.GetSiblingIndex();
				if (siblingIndex + 1 != siblingIndex2)
				{
					source.SetSiblingIndex((siblingIndex > siblingIndex2) ? siblingIndex2 : (siblingIndex2 - 1));
				}
			}
		}

		private static void ForgetButton()
		{
			_menuButtonObject = null;
			_menuButton = null;
		}
	}
	internal static class WineRackExpansion
	{
		internal const string LevelTag = "XIAOWO_WINE_RACK_EXPANSION_LEVEL";

		internal const string LayoutTag = "XIAOWO_WINE_RACK_LAYOUT_VERSION";

		internal const int MaximumLevel = 4;

		private static readonly (int Width, int Height)[] Shapes = new (int, int)[5]
		{
			(6, 4),
			(8, 5),
			(9, 6),
			(11, 7),
			(12, 8)
		};

		private static readonly HashSet<IntPtr> PendingModuleRepairs = new HashSet<IntPtr>();

		[ThreadStatic]
		private static bool _applying;

		internal static bool IsWineRack(GameItem item)
		{
			if (item != null)
			{
				return string.Equals(item.identifier?.Replace("\0", "").Trim(), "wine_rack", StringComparison.Ordinal);
			}
			return false;
		}

		internal static GameGridInventory GetWineGrid(GameItem rack)
		{
			if (!IsWineRack(rack))
			{
				return null;
			}
			PixelWindow contentWindow = rack.contentWindow;
			object obj;
			if (contentWindow == null)
			{
				obj = null;
			}
			else
			{
				GraphNodeStorage child = contentWindow.child;
				obj = ((child != null) ? ((Il2CppObjectBase)child).TryCast<GraphNodeStorage>() : null);
			}
			GraphNodeStorage val = (GraphNodeStorage)obj;
			if (((val != null) ? val.children : null) == null || val.children.Count < 3)
			{
				return null;
			}
			GraphNodeStorage val2 = val.children[2];
			if (val2 == null)
			{
				return null;
			}
			return ((Il2CppObjectBase)val2).TryCast<GameGridInventory>();
		}

		internal static GameGridInventory GetModuleGrid(GameItem rack)
		{
			if (IsWineRack(rack))
			{
				PixelWindow contentWindow = rack.contentWindow;
				object obj;
				if (contentWindow == null)
				{
					obj = null;
				}
				else
				{
					GraphNodeStorage child = contentWindow.child;
					obj = ((child != null) ? ((Il2CppObjectBase)child).TryCast<GraphNodeStorage>() : null);
				}
				GraphNodeStorage val = (GraphNodeStorage)obj;
				if (val != null && val.children != null && val.children.Count >= 3)
				{
					GraphNodeStorage val2 = val.children[1];
					if (val2 == null)
					{
						return null;
					}
					return ((Il2CppObjectBase)val2).TryCast<GameGridInventory>();
				}
			}
			return null;
		}

		internal static int Level(GameItem rack)
		{
			if (!IsWineRack(rack))
			{
				return 0;
			}
			TagSystem state = rack.state;
			int? num;
			if (state == null)
			{
				num = null;
			}
			else
			{
				TagState tag = state.GetTag("XIAOWO_WINE_RACK_EXPANSION_LEVEL");
				num = ((tag != null) ? new int?(tag.GetInt()) : ((int?)null));
			}
			int? num2 = num;
			return Math.Clamp(num2.GetValueOrDefault(), 0, 4);
		}

		internal static int Remaining(GameItem rack)
		{
			return 4 - Level(rack);
		}

		internal static string Status(GameItem rack)
		{
			if (!PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion"))
			{
				return string.Empty;
			}
			if (!PrecisionBayMode.IsUpgradeEnabled())
			{
				return ModLocalization.Select("精工扩舱已关闭", "Precision Bay Expansion disabled");
			}
			if (GetWineGrid(rack) == null)
			{
				return string.Empty;
			}
			int num = Level(rack);
			(int, int) tuple = Shapes[num];
			return ModLocalization.Select("精工扩舱 ", "Precision expansion ") + num + "/4 | " + ModLocalization.Select("酒瓶区 ", "Wine storage ") + tuple.Item1 + "x" + tuple.Item2 + (PendingModuleRepairs.Contains(((Il2CppObjectBase)rack).Pointer) ? ModLocalization.Select(" | 请移出模组区原尺寸外的物品", " | Remove modules outside the original grid") : "");
		}

		internal static bool CanUpgrade(GameItem rack)
		{
			if (!PerkRegistry.IsActive("xiaowo_trade_precision_bay_expansion") || !PrecisionBayMode.IsUpgradeEnabled() || Level(rack) >= 4)
			{
				return false;
			}
			GameGridInventory wineGrid = GetWineGrid(rack);
			(int, int) tuple = Shapes[Level(rack) + 1];
			if (((wineGrid != null) ? wineGrid.inventoryShape : null) != null)
			{
				if (wineGrid.inventoryShape.width >= tuple.Item1)
				{
					return wineGrid.inventoryShape.height < tuple.Item2;
				}
				return true;
			}
			return false;
		}

		internal static bool TryUpgrade(GameItem rack, GameItem ingot, out string details)
		{
			details = string.Empty;
			Restore(rack);
			if (!CanUpgrade(rack))
			{
				details = "wine rack unavailable or capped";
				return false;
			}
			GameGridInventory wineGrid = GetWineGrid(rack);
			int num = Level(rack);
			int width = wineGrid.inventoryShape.width;
			int height = wineGrid.inventoryShape.height;
			TagSystem state = rack.state;
			int? num2;
			if (state == null)
			{
				num2 = null;
			}
			else
			{
				TagState tag = state.GetTag("machinery_drawn_power");
				num2 = ((tag != null) ? new int?(tag.GetInt()) : ((int?)null));
			}
			int? num3 = num2;
			List<(IntPtr, int, int, int, int)> expected = Snapshot(wineGrid);
			try
			{
				_applying = true;
				SetLevel(rack, num + 1);
				Apply(rack);
				int num4 = Math.Max(width, Shapes[num + 1].Width);
				int num5 = Math.Max(height, Shapes[num + 1].Height);
				if (wineGrid.inventoryShape.width == num4 && wineGrid.inventoryShape.height == num5 && Matches(wineGrid, expected))
				{
					TagSystem state2 = rack.state;
					bool flag;
					if (state2 == null)
					{
						flag = num3.HasValue;
					}
					else
					{
						TagState tag2 = state2.GetTag("machinery_drawn_power");
						flag = ((tag2 != null) ? new int?(tag2.GetInt()) : ((int?)null)) != num3;
					}
					if (!flag)
					{
						if (ingot != null)
						{
							ingot.Destroy();
						}
						details = "wine rack expansion=" + (num + 1) + "/4 shape=" + num4 + "x" + num5;
						return true;
					}
				}
				throw new InvalidOperationException("wine contents, grid or power changed unexpectedly");
			}
			catch (Exception ex)
			{
				try
				{
					SetLevel(rack, num);
					wineGrid.SetShape(width, height);
					wineGrid.ForceRecomputeDynamicBackground();
					PixelWindow contentWindow = rack.contentWindow;
					if (contentWindow != null)
					{
						contentWindow.Validate();
					}
					if (num3.HasValue)
					{
						TagSystem state3 = rack.state;
						if (state3 != null)
						{
							TagState tag3 = state3.GetTag("machinery_drawn_power");
							if (tag3 != null)
							{
								tag3.SetInt(num3.Value);
							}
						}
					}
					if (!Matches(wineGrid, expected))
					{
						Instance log = Core.Log;
						if (log != null)
						{
							log.Error("Wine rack items changed during rollback");
						}
					}
				}
				catch (Exception ex2)
				{
					Instance log2 = Core.Log;
					if (log2 != null)
					{
						log2.Error("Wine rack rollback failed: " + ex2);
					}
				}
				details = ex.GetType().Name + ": " + ex.Message;
				Instance log3 = Core.Log;
				if (log3 != null)
				{
					log3.Error("Wine rack expansion rejected: " + details);
				}
				return false;
			}
			finally
			{
				_applying = false;
			}
		}

		internal static void Restore(GameItem rack)
		{
			if (_applying || !IsWineRack(rack) || Level(rack) == 0)
			{
				return;
			}
			try
			{
				_applying = true;
				Apply(rack);
			}
			finally
			{
				_applying = false;
			}
		}

		private static void Apply(GameItem rack)
		{
			GameGridInventory wineGrid = GetWineGrid(rack);
			int num = Level(rack);
			if (((wineGrid != null) ? wineGrid.inventoryShape : null) == null || num == 0)
			{
				return;
			}
			RepairLegacyModuleGrid(rack);
			int num2 = Math.Max(wineGrid.inventoryShape.width, Shapes[num].Width);
			int num3 = Math.Max(wineGrid.inventoryShape.height, Shapes[num].Height);
			if (wineGrid.inventoryShape.width != num2 || wineGrid.inventoryShape.height != num3)
			{
				wineGrid.SetShape(num2, num3);
				wineGrid.ForceRecomputeDynamicBackground();
				PixelWindow contentWindow = rack.contentWindow;
				if (contentWindow != null)
				{
					contentWindow.Validate();
				}
				if (wineGrid.inventoryShape.width < num2 || wineGrid.inventoryShape.height < num3)
				{
					wineGrid.SetShape(num2, num3);
				}
			}
		}

		private static void RepairLegacyModuleGrid(GameItem rack)
		{
			TagSystem state = rack.state;
			int? num;
			if (state == null)
			{
				num = null;
			}
			else
			{
				TagState tag = state.GetTag("XIAOWO_WINE_RACK_LAYOUT_VERSION");
				num = ((tag != null) ? new int?(tag.GetInt()) : ((int?)null));
			}
			int? num2 = num;
			if (num2.GetValueOrDefault() >= 1)
			{
				return;
			}
			GameGridInventory moduleGrid = GetModuleGrid(rack);
			if (((moduleGrid != null) ? moduleGrid.inventoryShape : null) == null)
			{
				return;
			}
			int width = moduleGrid.inventoryShape.width;
			int height = moduleGrid.inventoryShape.height;
			if (width > 7 && width <= 11 && height - 5 == width - 7)
			{
				for (int i = 0; i < moduleGrid.items.Count; i++)
				{
					GameItem val = moduleGrid.items[i];
					GridShape val2 = ((val != null) ? val.modifiedShape : null) ?? ((val != null) ? val.shape : null);
					if (val2 != null && val2.minX >= 0 && val2.minY >= 0 && val2.maxX < 7 && val2.maxY < 5)
					{
						continue;
					}
					if (PendingModuleRepairs.Add(((Il2CppObjectBase)rack).Pointer))
					{
						Instance log = Core.Log;
						if (log != null)
						{
							log.Warning("催陈器模组区有物品超出原版 7x5 范围，请先移出；物品已保留，酒瓶区照常扩容。");
						}
					}
					return;
				}
				List<(IntPtr, int, int, int, int)> expected = Snapshot(moduleGrid);
				moduleGrid.SetShape(7, 5);
				if (!Matches(moduleGrid, expected))
				{
					moduleGrid.SetShape(width, height);
					throw new InvalidOperationException("module contents changed while restoring native dimensions");
				}
				bool renderDynamicBackground = moduleGrid.renderDynamicBackground;
				try
				{
					moduleGrid.renderDynamicBackground = false;
					moduleGrid.ForceRecomputeDynamicBackground();
				}
				finally
				{
					moduleGrid.renderDynamicBackground = renderDynamicBackground;
				}
				moduleGrid.ForceRecomputeDynamicBackground();
				PixelWindow contentWindow = rack.contentWindow;
				if (contentWindow != null)
				{
					contentWindow.Validate();
				}
			}
			PendingModuleRepairs.Remove(((Il2CppObjectBase)rack).Pointer);
			if (!rack.IsTag("XIAOWO_WINE_RACK_LAYOUT_VERSION"))
			{
				rack.state.EnableTag("XIAOWO_WINE_RACK_LAYOUT_VERSION");
			}
			rack.state.GetTag("XIAOWO_WINE_RACK_LAYOUT_VERSION").SetInt(1);
		}

		private static void SetLevel(GameItem rack, int level)
		{
			if (!rack.IsTag("XIAOWO_WINE_RACK_EXPANSION_LEVEL"))
			{
				rack.state.EnableTag("XIAOWO_WINE_RACK_EXPANSION_LEVEL");
			}
			rack.state.GetTag("XIAOWO_WINE_RACK_EXPANSION_LEVEL").SetInt(level);
		}

		private static List<(IntPtr Pointer, int Id, int Count, int X, int Y)> Snapshot(GameGridInventory wine)
		{
			List<(IntPtr, int, int, int, int)> list = new List<(IntPtr, int, int, int, int)>();
			for (int i = 0; i < wine.items.Count; i++)
			{
				GameItem val = wine.items[i];
				IntPtr pointer = ((Il2CppObjectBase)val).Pointer;
				int uniqueId = val.uniqueId;
				int unitCount = val.unitCount;
				GridShape modifiedShape = val.modifiedShape;
				int item = ((modifiedShape != null) ? modifiedShape.minX : (-1));
				GridShape modifiedShape2 = val.modifiedShape;
				list.Add((pointer, uniqueId, unitCount, item, (modifiedShape2 != null) ? modifiedShape2.minY : (-1)));
			}
			return list;
		}

		private static bool Matches(GameGridInventory wine, List<(IntPtr Pointer, int Id, int Count, int X, int Y)> expected)
		{
			if (wine.items.Count != expected.Count)
			{
				return false;
			}
			int num = 0;
			while (num < expected.Count)
			{
				GameItem val = wine.items[num];
				IntPtr? intPtr = ((val != null) ? new IntPtr?(((Il2CppObjectBase)val).Pointer) : ((IntPtr?)null));
				IntPtr item = expected[num].Pointer;
				if (intPtr.HasValue && !(intPtr.GetValueOrDefault() != item) && val.uniqueId == expected[num].Id && val.unitCount == expected[num].Count)
				{
					GridShape modifiedShape = val.modifiedShape;
					if (((modifiedShape != null) ? modifiedShape.minX : (-1)) == expected[num].X)
					{
						GridShape modifiedShape2 = val.modifiedShape;
						if (((modifiedShape2 != null) ? modifiedShape2.minY : (-1)) == expected[num].Y)
						{
							num++;
							continue;
						}
					}
				}
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(GameItem), "ValidateShapeState")]
	internal static class WineRackValidationRestorePatch
	{
		[HarmonyPriority(0)]
		private static void Postfix(GameItem __instance)
		{
			WineRackExpansion.Restore(__instance);
		}
	}
}
