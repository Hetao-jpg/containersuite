using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using AugHuntConfig;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Main), "diy aug", "1.1.0", "核桃", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.1.0.0")]
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
namespace AugHuntConfig
{
	public class Main : MelonMod
	{
		internal static class PresenceGuard
		{
			internal static void Clamp(PlayerStore store, string source)
			{
				if (!IsEnabled() || store == null || !IsPresenceGuardEnabled())
				{
					return;
				}
				try
				{
					int augPresence = store.augPresence;
					int num = augPresence;
					if (store.isHardMode)
					{
						int hardModeMaxPresence = GetHardModeMaxPresence();
						if (num > hardModeMaxPresence)
						{
							num = hardModeMaxPresence;
						}
					}
					if (store.isAugIntroduced && !store.isAugEnded)
					{
						int minimumPresence = GetMinimumPresence();
						if (minimumPresence > 0 && num < minimumPresence)
						{
							num = minimumPresence;
						}
						else if (num < 0)
						{
							num = 0;
						}
					}
					if (num != augPresence)
					{
						store.augPresence = num;
						LogMsg("Presence 保护 after " + source + ": " + augPresence + " -> " + num);
					}
				}
				catch (Exception e)
				{
					LogException("PresenceGuard.Clamp(" + source + ")", e);
				}
			}

			internal static void ClampCurrent(string source)
			{
				if (IsEnabled() && PlayerStore.IsInstanceExist())
				{
					Clamp(PlayerStore.Instance, source);
				}
			}
		}

		internal static class AugHuntContract
		{
			private const string GameOverReason = "aug";

			private static readonly HashSet<long> SpawnedAugs = new HashSet<long>();

			private static readonly HashSet<long> RequiredKillAugs = new HashSet<long>();

			private static readonly HashSet<long> KilledAugs = new HashSet<long>();

			private static readonly HashSet<long> AddAugBaseline = new HashSet<long>();

			private static string _dayKey = string.Empty;

			private static bool _escapedAlive;

			private static bool _sleepResolved;

			private static bool _ignoreLoadedDayForRetaliation;

			private static bool _gameReady;

			private static readonly Dictionary<string, long> _lastAdjustTicks = new Dictionary<string, long>();

			[ThreadStatic]
			private static bool _blockNativeAugAdd;

			internal static int SpawnedCount => SpawnedAugs.Count;

			internal static int RequiredKillCount => RequiredKillAugs.Count;

			internal static int KilledCount => KilledAugs.Count;

			internal static bool EscapedAlive => _escapedAlive;

			internal static string CurrentDayKey => _dayKey;

			internal static bool SleepResolved => _sleepResolved;

			internal static void MarkGameReady()
			{
				_gameReady = true;
			}

			internal static bool IsGameReady()
			{
				return _gameReady;
			}

			internal static void BeforeDailyGeneration(StoreClientManager manager, out bool eligible)
			{
				eligible = false;
				if (IsEnabled() && IsGameReady() && PlayerStore.IsInstanceExist())
				{
					EnsureDay();
					TrackExistingAugs(manager);
					PlayerStore instance = PlayerStore.Instance;
					bool flag = IsContractRunning(instance);
					_blockNativeAugAdd = flag && SpawnedCount > 0;
					eligible = flag && IsNativeAugWindow(manager, instance, StoreStation.instance);
				}
			}

			internal static void AfterDailyGeneration(StoreClientManager manager, bool eligible)
			{
				try
				{
					if (!IsEnabled() || !IsGameReady() || !PlayerStore.IsInstanceExist())
					{
						return;
					}
					EnsureDay();
					TrackExistingAugs(manager);
					if (eligible && IsContractRunning(PlayerStore.Instance))
					{
						int dailyAugLimit = GetDailyAugLimit();
						if (SpawnedCount < dailyAugLimit && RNG.Roll(GetExtraVisitChance()))
						{
							bool blockNativeAugAdd = _blockNativeAugAdd;
							_blockNativeAugAdd = false;
							manager.AddAugClient();
							_blockNativeAugAdd = blockNativeAugAdd;
							LogMsg("Aug 额外刷新成功；今日 Aug 数量=" + SpawnedCount + "。");
						}
					}
				}
				catch (Exception e)
				{
					LogException("AfterDailyGeneration", e);
				}
				finally
				{
					_blockNativeAugAdd = false;
					TrackExistingAugs(manager);
				}
			}

			internal static void AbortDailyGeneration()
			{
				_blockNativeAugAdd = false;
				AddAugBaseline.Clear();
			}

			internal static bool BeforeAddAug(StoreClientManager manager)
			{
				if (!IsEnabled())
				{
					return true;
				}
				if (!IsGameReady())
				{
					return true;
				}
				if (!PlayerStore.IsInstanceExist())
				{
					return true;
				}
				EnsureDay();
				TrackExistingAugs(manager);
				AddAugBaseline.Clear();
				foreach (long spawnedAug in SpawnedAugs)
				{
					AddAugBaseline.Add(spawnedAug);
				}
				return CanAddAug(IsContractRunning(PlayerStore.Instance), SpawnedCount, _blockNativeAugAdd);
			}

			internal static void AfterAddAug(StoreClientManager manager)
			{
				if (!IsEnabled() || !IsGameReady() || !PlayerStore.IsInstanceExist())
				{
					return;
				}
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

			internal static void AdjustPresenceByActualDelta(int before, string source)
			{
				if (!IsEnabled() || !IsGameReady() || !PlayerStore.IsInstanceExist())
				{
					return;
				}
				long ticks = DateTime.UtcNow.Ticks;
				if (_lastAdjustTicks.TryGetValue(source, out var value))
				{
					long num = ticks - value;
					if (num >= 0 && num < 500000)
					{
						return;
					}
				}
				_lastAdjustTicks[source] = ticks;
				try
				{
					PlayerStore instance = PlayerStore.Instance;
					if (instance == null || !instance.isShootingUnlocked || instance.isAugEnded)
					{
						return;
					}
					int augPresence = instance.augPresence;
					int num2 = augPresence - before;
					if (num2 == 0)
					{
						return;
					}
					float num3;
					string text;
					if (num2 > 0)
					{
						num3 = GetPresenceGainMultiplier();
						text = "增加";
					}
					else
					{
						num3 = GetPresenceLossMultiplier();
						text = "减少";
					}
					int num4 = (int)((float)num2 * num3);
					int num5 = num4 - num2;
					if (num5 != 0)
					{
						int num6 = augPresence + num5;
						if (num6 < 0)
						{
							num6 = 0;
						}
						instance.augPresence = num6;
						LogMsg("[" + source + "] 存在感" + text + "调整：实际 " + num2 + "，倍率 " + num3 + " -> " + num4 + "，补偿 " + num5 + "，最终=" + instance.augPresence);
					}
				}
				catch (Exception e)
				{
					LogException("AdjustPresenceByActualDelta(" + source + ")", e);
				}
			}

			internal static void MarkCurrentAugKilled()
			{
				if (!IsEnabled() || !IsGameReady())
				{
					return;
				}
				EnsureDay();
				StoreClient currentStoreClient = GetCurrentStoreClient();
				if (currentStoreClient != null && currentStoreClient.isAug)
				{
					long num = ((Il2CppObjectBase)currentStoreClient).Pointer.ToInt64();
					if (num != 0L && !KilledAugs.Contains(num))
					{
						SpawnedAugs.Add(num);
						RequiredKillAugs.Add(num);
						KilledAugs.Add(num);
						LogMsg("Aug 已击杀；" + KilledCount + "/" + RequiredKillCount + "。");
					}
				}
			}

			internal static void MarkCurrentAugEscaped()
			{
				if (!IsEnabled() || !IsGameReady())
				{
					return;
				}
				EnsureDay();
				StoreClient currentStoreClient = GetCurrentStoreClient();
				if (currentStoreClient != null && currentStoreClient.isAug)
				{
					long num = ((Il2CppObjectBase)currentStoreClient).Pointer.ToInt64();
					if (num != 0L)
					{
						SpawnedAugs.Add(num);
						RequiredKillAugs.Add(num);
					}
					_escapedAlive = true;
				}
			}

			internal static bool ResolveSleep(Action<string> executeGameOver)
			{
				if (!IsEnabled())
				{
					return false;
				}
				if (!IsGameReady())
				{
					return false;
				}
				if (!PlayerStore.IsInstanceExist())
				{
					return false;
				}
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
				if (!GetEnableRetaliation())
				{
					return false;
				}
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

			internal static bool IsContractRunning(PlayerStore store)
			{
				if (!IsEnabled())
				{
					return false;
				}
				if (store == null)
				{
					return false;
				}
				if (store.isHandlingShootout)
				{
					return false;
				}
				return store.isShootingUnlocked;
			}

			internal static bool CanAddAug(bool contractRunning, int spawned, bool blockNativeAdd)
			{
				if (!contractRunning)
				{
					return true;
				}
				if (spawned >= GetDailyAugLimit())
				{
					return false;
				}
				return !blockNativeAdd;
			}

			internal static bool IsNativeAugWindow(StoreClientManager manager, PlayerStore store, StoreStation station)
			{
				if (manager == null || store == null || station == null)
				{
					return false;
				}
				if (!store.isShootingUnlocked || !store.isAugIntroduced || store.isAugEnded)
				{
					return false;
				}
				if (manager.isAugSpawnHandledToday)
				{
					return false;
				}
				int dayCounter = StoreStation.GetDayCounter();
				if (store.nextAugSpawnDay >= 0)
				{
					return dayCounter + 1 >= store.nextAugSpawnDay;
				}
				return false;
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
				_gameReady = false;
			}

			internal static StoreClient GetCurrentStoreClient()
			{
				try
				{
					if (!IsGameReady())
					{
						return null;
					}
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
					return currentClientInstance.storeClient;
				}
				catch (Exception e)
				{
					LogException("GetCurrentStoreClient", e);
					return null;
				}
			}

			private static void EnsureDay()
			{
				if (!IsGameReady())
				{
					return;
				}
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
				if (!IsGameReady())
				{
					return "no-run|0";
				}
				if (!PlayerStore.IsInstanceExist())
				{
					return "no-run|0";
				}
				try
				{
					PlayerStore instance = PlayerStore.Instance;
					return (((instance != null) ? instance.runID : null) ?? "no-run") + "|" + Math.Max(0, StoreStation.GetDayCounter());
				}
				catch
				{
					return "no-run|0";
				}
			}

			private static void ClearTracking()
			{
				SpawnedAugs.Clear();
				RequiredKillAugs.Clear();
				KilledAugs.Clear();
				AddAugBaseline.Clear();
				_escapedAlive = false;
				_sleepResolved = false;
				_blockNativeAugAdd = false;
			}

			private static void TrackExistingAugs(StoreClientManager manager)
			{
				if (!IsGameReady())
				{
					return;
				}
				try
				{
					if (manager != null && manager.clientStack != null)
					{
						for (int i = 0; i < manager.clientStack.Count; i++)
						{
							TrackAug(manager.clientStack[i]);
						}
					}
					TrackAug(GetCurrentStoreClient());
				}
				catch (Exception e)
				{
					LogException("TrackExistingAugs", e);
				}
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

		[HarmonyPatch(typeof(AugHelper), "CleanupKill")]
		internal static class AugHuntCleanupKillPatch
		{
			private static void Prefix()
			{
				BeginPresenceAdjust("CleanupKill");
				if (IsEnabled())
				{
					AugHuntContract.MarkCurrentAugKilled();
				}
			}

			private static void Postfix()
			{
				EndPresenceAdjust("CleanupKill", clampAfter: false);
			}
		}

		[HarmonyPatch(typeof(AugHelper), "OnGoodKill")]
		internal static class AugHuntGoodKillPatch
		{
			private static void Prefix()
			{
				BeginPresenceAdjust("OnGoodKill");
				if (IsEnabled())
				{
					AugHuntContract.MarkCurrentAugKilled();
				}
			}

			private static void Postfix()
			{
				EndPresenceAdjust("OnGoodKill", clampAfter: true);
			}
		}

		[HarmonyPatch(typeof(AugHelper), "OnBadKillWithSecGrace")]
		internal static class AugHuntBadKillPatch
		{
			private static void Prefix()
			{
				BeginPresenceAdjust("OnBadKillWithSecGrace");
			}

			private static void Postfix()
			{
				EndPresenceAdjust("OnBadKillWithSecGrace", clampAfter: true);
			}
		}

		[HarmonyPatch(typeof(AugHelper), "OnAugLeftAlive")]
		internal static class AugHuntEscapePatch
		{
			private static void Prefix()
			{
				BeginPresenceAdjust("OnAugLeftAlive");
				if (IsEnabled())
				{
					AugHuntContract.MarkCurrentAugEscaped();
				}
			}

			private static void Postfix()
			{
				EndPresenceAdjust("OnAugLeftAlive", clampAfter: true);
			}
		}

		[HarmonyPatch(typeof(StoreClientManager), "AddAugClient")]
		internal static class AugHuntDailyCapPatch
		{
			private static bool Prefix(StoreClientManager __instance)
			{
				if (!IsEnabled())
				{
					return true;
				}
				return AugHuntContract.BeforeAddAug(__instance);
			}

			private static void Postfix(StoreClientManager __instance)
			{
				if (IsEnabled())
				{
					AugHuntContract.AfterAddAug(__instance);
				}
			}
		}

		[HarmonyPatch(typeof(StoreClientManager), "GenerateClient")]
		internal static class AugHuntDailyGenerationPatch
		{
			private static void Prefix(StoreClientManager __instance, out bool __state)
			{
				__state = false;
				if (IsEnabled())
				{
					AugHuntContract.BeforeDailyGeneration(__instance, out __state);
				}
			}

			private static void Postfix(StoreClientManager __instance, bool __state)
			{
				if (IsEnabled())
				{
					AugHuntContract.AfterDailyGeneration(__instance, __state);
				}
			}

			private static Exception Finalizer(Exception __exception)
			{
				if (__exception != null)
				{
					AugHuntContract.AbortDailyGeneration();
				}
				return __exception;
			}
		}

		[HarmonyPatch(typeof(StoreClientManager), "HandleAugClient")]
		internal static class AugHuntHandleAugClientPatch
		{
			private static void Prefix()
			{
				if (IsEnabled())
				{
					PresenceGuard.ClampCurrent("checking aug visitors");
				}
			}
		}

		[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
		internal static class AugHuntLoadResetPatch
		{
			private static void Prefix()
			{
				if (IsEnabled())
				{
					AugHuntContract.OnGameLoading();
				}
			}

			private static void Postfix(PlayerStore __instance)
			{
				if (IsEnabled())
				{
					PresenceGuard.Clamp(__instance, "loading the save");
					AugHuntContract.MarkGameReady();
				}
			}
		}

		[HarmonyPatch(typeof(PlayerStore), "BeginDay")]
		internal static class AugHuntBeginDayPatch
		{
			private static void Postfix(PlayerStore __instance)
			{
				if (IsEnabled())
				{
					PresenceGuard.Clamp(__instance, "beginning the day");
					ApplyForceSettings(__instance);
				}
			}
		}

		[HarmonyPatch(typeof(Shotgun), "LockShotgun")]
		internal static class AugHuntShotgunLockPatch
		{
			private static void Postfix()
			{
				if (IsEnabled())
				{
					AugHuntContract.OnShotgunStateChanged();
				}
			}
		}

		[HarmonyPatch(typeof(Shotgun), "Unlock")]
		internal static class AugHuntShotgunUnlockPatch
		{
			private static void Postfix()
			{
				if (IsEnabled())
				{
					AugHuntContract.OnShotgunStateChanged();
				}
			}
		}

		[HarmonyPatch(typeof(PlayerStore), "EndNight")]
		internal static class AugHuntSleepPatch
		{
			private static bool Prefix(PlayerStore __instance)
			{
				if (!IsEnabled())
				{
					return true;
				}
				if (__instance == null)
				{
					return true;
				}
				if (!AugHuntContract.ResolveSleep((string reason) =>
				{
					__instance.ExecuteGameOver(reason);
				}))
				{
					return true;
				}
				LogMsg("Aug 狩猎契约未完成 -> 触发 'aug' 游戏结束。");
				return false;
			}
		}

		private static Instance _log;

		private static MelonPreferences_Entry<bool> _enabled;

		private static MelonPreferences_Entry<int> _extraVisitChance;

		private static MelonPreferences_Entry<int> _dailyAugLimit;

		private static MelonPreferences_Entry<bool> _enableRetaliation;

		private static MelonPreferences_Entry<bool> _enablePresenceGuard;

		private static MelonPreferences_Entry<int> _minimumPresence;

		private static MelonPreferences_Entry<int> _hardModeMaxPresence;

		private static MelonPreferences_Entry<float> _presenceGainMultiplier;

		private static MelonPreferences_Entry<float> _presenceLossMultiplier;

		private static Harmony _harmony;

		[ThreadStatic]
		private static int _presenceDepth;

		[ThreadStatic]
		private static int _presenceOuterBefore;

		private const bool FORCE_UNLOCK_SHOTGUN = false;

		private const bool FORCE_AUG_INTRO = false;

		private static MelonPreferences_Entry<bool> _augPanelVisible;

		private static MelonPreferences_Entry<KeyCode> _augPanelKey;

		private static MelonPreferences_Entry<float> _augPanelX;

		private static MelonPreferences_Entry<float> _augPanelY;

		private static MelonPreferences_Entry<float> _augPanelWidth;

		private static MelonPreferences_Entry<float> _augPanelHeight;

		private static bool _augPanelActive;

		private static float _augReadTimer;

		private static Rect _augWindowRect = new Rect(380f, 20f, 280f, 420f);

		private static bool _augResizing;

		private static Vector2 _resizeStartMouse;

		private static Vector2 _resizeStartSize;

		private const float PANEL_MIN_W = 240f;

		private const float PANEL_MIN_H = 340f;

		private const float PANEL_MAX_W = 800f;

		private const float PANEL_MAX_H = 900f;

		private const float HANDLE_SIZE = 22f;

		private static bool _augDataReady;

		private static int _cachedPresence;

		private static bool _cachedHardMode;

		private static bool _cachedShootingUnlocked;

		private static bool _cachedAugIntroduced;

		private static bool _cachedAugEnded;

		private static bool _cachedHandlingShootout;

		private static int _cachedNextAugSpawnDay;

		private static int _cachedDay;

		private static bool _cachedSpawnHandledToday;

		private static bool _cachedCurrentIsAug;

		private static bool _cachedCurrentIsAugScanned;

		private static int _cachedSpawned;

		private static int _cachedRequired;

		private static int _cachedKilled;

		private static bool _cachedEscapedAlive;

		private static string _cachedDayKey = "";

		private static string _cachedLastError = "";

		private static GUIStyle _augTitleStyle;

		private static GUIStyle _augSectionStyle;

		private static GUIStyle _augValueStyle;

		private static GUIStyle _augWarnStyle;

		private static GUIStyle _augTipStyle;

		private static GUIStyle _augHotkeyStyle;

		private static bool _augStylesReady;

		private static long _lastExceptionLogTicks;

		public override void OnInitializeMelon()
		{
			//IL_021f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0229: Expected Obj, but got Unknown
			//IL_031c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0321: Unknown result type (might be due to invalid IL or missing references)
			((MelonBase)this).LoggerInstance.Msg("[diy aug] v1.1.0 已加载");
			_log = ((MelonBase)this).LoggerInstance;
			MelonPreferences_Category val = MelonPreferences.CreateCategory("diy aug");
			_enabled = val.CreateEntry<bool>("Enabled", true, "总开关", "是否启用此 mod。false 时完全恢复原版行为。", false, false, (ValueValidator)null, (string)null);
			_extraVisitChance = val.CreateEntry<int>("ExtraVisitChance", 10, "额外刷新概率", "每日额外刷新 Aug 的概率（0-100）。100 表示每天必定多刷一个 Aug。", false, false, (ValueValidator)null, (string)null);
			_dailyAugLimit = val.CreateEntry<int>("DailyAugLimit", 2, "每日上限", "每天最多生成的 Aug 数量。原版为 2。", false, false, (ValueValidator)null, (string)null);
			_enableRetaliation = val.CreateEntry<bool>("EnableRetaliation", false, "未击杀惩罚", "未击杀的 Aug 在睡觉时是否触发游戏结束。true = 结束游戏（原版）；false = 不结束，仅记录。", false, false, (ValueValidator)null, (string)null);
			_enablePresenceGuard = val.CreateEntry<bool>("EnablePresenceGuard", true, "存在感保护", "是否钳制存在感数值，防止被游戏机制清零或溢出。", false, false, (ValueValidator)null, (string)null);
			_minimumPresence = val.CreateEntry<int>("MinimumPresence", 1, "存在感下限", "Aug 已引入且未结束时，存在感的最低值。0 表示不保护下限。", false, false, (ValueValidator)null, (string)null);
			_hardModeMaxPresence = val.CreateEntry<int>("HardModeMaxPresence", 99, "困难模式存在感上限", "困难模式下的存在感最高值。原版为 99。", false, false, (ValueValidator)null, (string)null);
			_presenceGainMultiplier = val.CreateEntry<float>("PresenceGainMultiplier", 1f, "放走倍率", "放走 Aug 时存在感增加量的倍率。1.0 = 原版（+10/+15）。", false, false, (ValueValidator)null, (string)null);
			_presenceLossMultiplier = val.CreateEntry<float>("PresenceLossMultiplier", 1f, "击杀倍率", "击杀 Aug 时存在感减少量的倍率。1.0 = 原版（-25/-10）。", false, false, (ValueValidator)null, (string)null);
			_augPanelVisible = val.CreateEntry<bool>("AugPanelVisible", false, "显示监控面板", "是否显示 Aug 实时监控面板。", false, false, (ValueValidator)null, (string)null);
			_augPanelKey = val.CreateEntry<KeyCode>("AugPanelKey", (KeyCode)285, "面板快捷键", "开关监控面板的按键。", false, false, (ValueValidator)null, (string)null);
			_augPanelX = val.CreateEntry<float>("AugPanelX", 380f, "面板位置 X", "面板左上角 X 坐标（拖动后自动保存）。", false, false, (ValueValidator)null, (string)null);
			_augPanelY = val.CreateEntry<float>("AugPanelY", 20f, "面板位置 Y", "面板左上角 Y 坐标（拖动后自动保存）。", false, false, (ValueValidator)null, (string)null);
			_augPanelWidth = val.CreateEntry<float>("AugPanelWidth", 280f, "面板宽度", "面板宽度（拖动右下角可调整）。", false, false, (ValueValidator)null, (string)null);
			_augPanelHeight = val.CreateEntry<float>("AugPanelHeight", 420f, "面板高度", "面板高度（拖动右下角可调整）。", false, false, (ValueValidator)null, (string)null);
			val.SaveToFile(true);
			try
			{
				_harmony = new Harmony("diy aug");
				_harmony.PatchAll(typeof(Main).Assembly);
				_log.Msg("[diy aug] Harmony 补丁已应用。");
			}
			catch (Exception ex)
			{
				_log.Error("[diy aug] Harmony 补丁应用失败: " + ex);
			}
			_augPanelActive = _augPanelVisible != null && _augPanelVisible.Value;
			float num = ((_augPanelX != null) ? _augPanelX.Value : 380f);
			float num2 = ((_augPanelY != null) ? _augPanelY.Value : 20f);
			float num3 = ((_augPanelWidth != null) ? _augPanelWidth.Value : 280f);
			float num4 = ((_augPanelHeight != null) ? _augPanelHeight.Value : 420f);
			num3 = Mathf.Clamp(num3, 240f, 800f);
			num4 = Mathf.Clamp(num4, 340f, 900f);
			_augWindowRect = new Rect(num, num2, num3, num4);
			_log.Msg("[diy aug] 已加载。Enabled=" + _enabled.Value + " ExtraVisitChance=" + _extraVisitChance.Value + "% DailyAugLimit=" + _dailyAugLimit.Value + " EnableRetaliation=" + _enableRetaliation.Value + " EnablePresenceGuard=" + _enablePresenceGuard.Value);
		}

		internal static bool IsEnabled()
		{
			if (_enabled != null)
			{
				return _enabled.Value;
			}
			return false;
		}

		internal static bool IsPresenceGuardEnabled()
		{
			if (IsEnabled() && _enablePresenceGuard != null)
			{
				return _enablePresenceGuard.Value;
			}
			return false;
		}

		internal static int GetExtraVisitChance()
		{
			if (_extraVisitChance == null)
			{
				return 10;
			}
			int num = _extraVisitChance.Value;
			if (num < 0)
			{
				num = 0;
			}
			if (num > 100)
			{
				num = 100;
			}
			return num;
		}

		internal static int GetDailyAugLimit()
		{
			if (_dailyAugLimit == null)
			{
				return 2;
			}
			int num = _dailyAugLimit.Value;
			if (num < 0)
			{
				num = 0;
			}
			return num;
		}

		internal static bool GetEnableRetaliation()
		{
			if (_enableRetaliation != null)
			{
				return _enableRetaliation.Value;
			}
			return true;
		}

		internal static int GetMinimumPresence()
		{
			if (_minimumPresence == null)
			{
				return 1;
			}
			return _minimumPresence.Value;
		}

		internal static int GetHardModeMaxPresence()
		{
			if (_hardModeMaxPresence == null)
			{
				return 99;
			}
			return _hardModeMaxPresence.Value;
		}

		internal static float GetPresenceGainMultiplier()
		{
			if (_presenceGainMultiplier == null)
			{
				return 1f;
			}
			float value = _presenceGainMultiplier.Value;
			if (!(value < 0f))
			{
				return value;
			}
			return 0f;
		}

		internal static float GetPresenceLossMultiplier()
		{
			if (_presenceLossMultiplier == null)
			{
				return 1f;
			}
			float value = _presenceLossMultiplier.Value;
			if (!(value < 0f))
			{
				return value;
			}
			return 0f;
		}

		internal static void LogMsg(string msg)
		{
			if (_log != null)
			{
				_log.Msg(msg);
			}
		}

		internal static void LogWarn(string msg)
		{
			if (_log != null)
			{
				_log.Warning(msg);
			}
		}

		internal static void LogException(string source, Exception e)
		{
			long ticks = DateTime.UtcNow.Ticks;
			if (ticks - _lastExceptionLogTicks >= 50000000)
			{
				_lastExceptionLogTicks = ticks;
				if (_log != null)
				{
					_log.Warning("[diy aug] " + source + ": " + e.Message);
				}
			}
		}

		internal unsafe static void SetFieldBool(Il2CppObjectBase obj, string name, bool value)
		{
			if (obj == null)
			{
				return;
			}
			try
			{
				IntPtr il2CppField = IL2CPP.GetIl2CppField(IL2CPP.il2cpp_object_get_class(obj.Pointer), name);
				if (il2CppField == IntPtr.Zero)
				{
					LogWarn("[字段] 未找到: " + name);
					return;
				}
				long num = IL2CPP.il2cpp_field_get_offset(il2CppField);
				((sbyte*)(void*)obj.Pointer)[num] = (value ? ((sbyte)1) : ((sbyte)0));
			}
			catch (Exception e)
			{
				LogException("SetFieldBool(" + name + ")", e);
			}
		}

		internal unsafe static void SetFieldInt(Il2CppObjectBase obj, string name, int value)
		{
			if (obj == null)
			{
				return;
			}
			try
			{
				IntPtr il2CppField = IL2CPP.GetIl2CppField(IL2CPP.il2cpp_object_get_class(obj.Pointer), name);
				if (il2CppField == IntPtr.Zero)
				{
					LogWarn("[字段] 未找到: " + name);
					return;
				}
				long num = IL2CPP.il2cpp_field_get_offset(il2CppField);
				*(int*)((byte*)(void*)obj.Pointer + num) = value;
			}
			catch (Exception e)
			{
				LogException("SetFieldInt(" + name + ")", e);
			}
		}

		private static void BeginPresenceAdjust(string source)
		{
			_presenceDepth++;
			if (_presenceDepth != 1 || !IsEnabled() || !AugHuntContract.IsGameReady() || !PlayerStore.IsInstanceExist())
			{
				return;
			}
			try
			{
				_presenceOuterBefore = PlayerStore.Instance.augPresence;
			}
			catch (Exception e)
			{
				LogException(source + ".Prefix", e);
			}
		}

		private static void EndPresenceAdjust(string source, bool clampAfter)
		{
			_presenceDepth--;
			if (_presenceDepth > 0)
			{
				return;
			}
			if (_presenceDepth < 0)
			{
				_presenceDepth = 0;
			}
			if (IsEnabled())
			{
				AugHuntContract.AdjustPresenceByActualDelta(_presenceOuterBefore, source);
				if (clampAfter)
				{
					PresenceGuard.ClampCurrent(source);
				}
			}
		}

		internal static void ApplyForceSettings(PlayerStore store)
		{
			if (IsEnabled())
			{
			}
		}

		public override void OnGUI()
		{
			//IL_0038: Unknown result type (might be due to invalid IL or missing references)
			//IL_0062: Unknown result type (might be due to invalid IL or missing references)
			//IL_0067: Unknown result type (might be due to invalid IL or missing references)
			if (!IsEnabled() || !_augPanelActive)
			{
				return;
			}
			try
			{
				EnsureAugStyles();
				if (_augStylesReady)
				{
					int depth = GUI.depth;
					GUI.depth = 33100;
					HandleResizeInput();
					_augWindowRect = GUI.Window(98766, _augWindowRect, WindowFunction.op_Implicit((Action<int>)DrawAugPanel), "Aug 狩猎监控");
					DrawResizeHandle();
					GUI.depth = depth;
				}
			}
			catch (Exception e)
			{
				LogException("AugPanel.OnGUI", e);
			}
		}

		private static Vector2 GetGuiMousePosition()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Unknown result type (might be due to invalid IL or missing references)
			//IL_001b: Unknown result type (might be due to invalid IL or missing references)
			return new Vector2(Input.mousePosition.x, (float)Screen.height - Input.mousePosition.y);
		}

		private static void HandleResizeInput()
		{
			//IL_0046: Unknown result type (might be due to invalid IL or missing references)
			//IL_004b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_006c: Unknown result type (might be due to invalid IL or missing references)
			//IL_006d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0086: Unknown result type (might be due to invalid IL or missing references)
			//IL_008b: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b2: Unknown result type (might be due to invalid IL or missing references)
			float num = _augWindowRect.x + _augWindowRect.width - 22f;
			float num2 = _augWindowRect.y + _augWindowRect.height - 22f;
			Rect val = new Rect(num, num2, 22f, 22f);
			Vector2 guiMousePosition = GetGuiMousePosition();
			bool flag = val.Contains(guiMousePosition);
			if (Input.GetMouseButtonDown(0) & flag)
			{
				_augResizing = true;
				_resizeStartMouse = guiMousePosition;
				_resizeStartSize = new Vector2(_augWindowRect.width, _augWindowRect.height);
			}
			if (_augResizing && Input.GetMouseButton(0))
			{
				float num3 = guiMousePosition.x - _resizeStartMouse.x;
				float num4 = guiMousePosition.y - _resizeStartMouse.y;
				_augWindowRect.width = Mathf.Clamp(_resizeStartSize.x + num3, 240f, 800f);
				_augWindowRect.height = Mathf.Clamp(_resizeStartSize.y + num4, 340f, 900f);
			}
			if (_augResizing && Input.GetMouseButtonUp(0))
			{
				_augResizing = false;
				_augPanelWidth.Value = _augWindowRect.width;
				_augPanelHeight.Value = _augWindowRect.height;
				MelonPreferences.Save();
			}
		}

		private static void DrawResizeHandle()
		{
			//IL_004e: Unknown result type (might be due to invalid IL or missing references)
			float num = _augWindowRect.x + _augWindowRect.width - 22f;
			float num2 = _augWindowRect.y + _augWindowRect.height - 22f;
			GUI.Button(new Rect(num + 4f, num2 + 4f, 14f, 14f), "");
		}

		private static void EnsureAugStyles()
		{
			//IL_0012: Unknown result type (might be due to invalid IL or missing references)
			//IL_0017: Unknown result type (might be due to invalid IL or missing references)
			//IL_001f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0039: Expected Obj, but got Unknown
			//IL_0043: Unknown result type (might be due to invalid IL or missing references)
			//IL_0048: Unknown result type (might be due to invalid IL or missing references)
			//IL_0050: Unknown result type (might be due to invalid IL or missing references)
			//IL_0057: Unknown result type (might be due to invalid IL or missing references)
			//IL_005e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Unknown result type (might be due to invalid IL or missing references)
			//IL_0082: Expected Obj, but got Unknown
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a7: Unknown result type (might be due to invalid IL or missing references)
			//IL_00ad: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bc: Expected Obj, but got Unknown
			//IL_00c6: Unknown result type (might be due to invalid IL or missing references)
			//IL_00cb: Unknown result type (might be due to invalid IL or missing references)
			//IL_00d3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00da: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00e8: Unknown result type (might be due to invalid IL or missing references)
			//IL_00fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_010c: Expected Obj, but got Unknown
			//IL_0116: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0123: Unknown result type (might be due to invalid IL or missing references)
			//IL_012a: Unknown result type (might be due to invalid IL or missing references)
			//IL_013f: Unknown result type (might be due to invalid IL or missing references)
			//IL_014e: Expected Obj, but got Unknown
			//IL_0158: Unknown result type (might be due to invalid IL or missing references)
			//IL_015d: Unknown result type (might be due to invalid IL or missing references)
			//IL_0165: Unknown result type (might be due to invalid IL or missing references)
			//IL_016c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0181: Unknown result type (might be due to invalid IL or missing references)
			//IL_0190: Expected Obj, but got Unknown
			if (!_augStylesReady)
			{
				_augTitleStyle = new GUIStyle(GUI.skin.label)
				{
					fontSize = 16,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4,
					wordWrap = true
				};
				GUIStyle val = new GUIStyle(GUI.skin.label)
				{
					fontSize = 13,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4
				};
				val.normal.textColor = new Color(1f, 0.85f, 0.4f);
				_augSectionStyle = val;
				GUIStyle val2 = new GUIStyle(GUI.skin.label)
				{
					fontSize = 14,
					alignment = (TextAnchor)4,
					wordWrap = true
				};
				val2.normal.textColor = Color.white;
				_augValueStyle = val2;
				GUIStyle val3 = new GUIStyle(GUI.skin.label)
				{
					fontSize = 15,
					fontStyle = (FontStyle)1,
					alignment = (TextAnchor)4,
					wordWrap = true
				};
				val3.normal.textColor = new Color(1f, 0.4f, 0.4f);
				_augWarnStyle = val3;
				GUIStyle val4 = new GUIStyle(GUI.skin.label)
				{
					fontSize = 12,
					alignment = (TextAnchor)4
				};
				val4.normal.textColor = new Color(0.7f, 0.7f, 0.7f);
				_augTipStyle = val4;
				GUIStyle val5 = new GUIStyle(GUI.skin.label)
				{
					fontSize = 10,
					alignment = (TextAnchor)5
				};
				val5.normal.textColor = new Color(0.55f, 0.55f, 0.55f);
				_augHotkeyStyle = val5;
				_augStylesReady = true;
			}
		}

		private static void DrawAugPanel(int windowID)
		{
			//IL_0099: Unknown result type (might be due to invalid IL or missing references)
			//IL_00c2: Unknown result type (might be due to invalid IL or missing references)
			//IL_0124: Unknown result type (might be due to invalid IL or missing references)
			//IL_01f6: Unknown result type (might be due to invalid IL or missing references)
			//IL_018c: Unknown result type (might be due to invalid IL or missing references)
			//IL_01ae: Unknown result type (might be due to invalid IL or missing references)
			//IL_0152: Unknown result type (might be due to invalid IL or missing references)
			//IL_021c: Unknown result type (might be due to invalid IL or missing references)
			//IL_026c: Unknown result type (might be due to invalid IL or missing references)
			//IL_027e: Unknown result type (might be due to invalid IL or missing references)
			//IL_02a0: Unknown result type (might be due to invalid IL or missing references)
			//IL_02c5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0261: Unknown result type (might be due to invalid IL or missing references)
			//IL_0242: Unknown result type (might be due to invalid IL or missing references)
			//IL_032b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0357: Unknown result type (might be due to invalid IL or missing references)
			//IL_038b: Unknown result type (might be due to invalid IL or missing references)
			//IL_03b0: Unknown result type (might be due to invalid IL or missing references)
			//IL_03d5: Unknown result type (might be due to invalid IL or missing references)
			//IL_0409: Unknown result type (might be due to invalid IL or missing references)
			//IL_0497: Unknown result type (might be due to invalid IL or missing references)
			//IL_04b9: Unknown result type (might be due to invalid IL or missing references)
			//IL_057c: Unknown result type (might be due to invalid IL or missing references)
			//IL_059e: Unknown result type (might be due to invalid IL or missing references)
			//IL_04fd: Unknown result type (might be due to invalid IL or missing references)
			//IL_05ce: Unknown result type (might be due to invalid IL or missing references)
			//IL_05d4: Invalid comparison between Unknown and I4
			//IL_0537: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				GUIStyle val = _augTitleStyle ?? GUI.skin.label;
				GUIStyle val2 = _augSectionStyle ?? GUI.skin.label;
				GUIStyle val3 = _augValueStyle ?? GUI.skin.label;
				GUIStyle val4 = _augWarnStyle ?? GUI.skin.label;
				GUIStyle val5 = _augTipStyle ?? GUI.skin.label;
				GUIStyle val6 = _augHotkeyStyle ?? GUI.skin.label;
				GUI.DragWindow(new Rect(0f, 0f, _augWindowRect.width - 30f, 30f));
				if (GUI.Button(new Rect(_augWindowRect.width - 25f, 2f, 20f, 20f), "×"))
				{
					_augPanelActive = false;
					_augPanelVisible.Value = false;
					MelonPreferences.Save();
				}
				float num = 8f;
				float num2 = _augWindowRect.width - 16f;
				float num3 = 34f;
				if (!_augDataReady)
				{
					GUI.Label(new Rect(num, num3 + 80f, num2, 30f), "等待进入游戏或加载存档...", val5);
					if (!string.IsNullOrEmpty(_cachedLastError))
					{
						GUI.Label(new Rect(num, num3 + 116f, num2, 30f), "错误: " + _cachedLastError, val5);
					}
					GUI.Label(new Rect(num, _augWindowRect.height - 20f, num2 - 22f, 16f), $"标题栏拖动 · 右下角调整大小 · {_augPanelKey.Value} 开关", val6);
					return;
				}
				string text;
				Color textColor;
				if (!_cachedShootingUnlocked)
				{
					text = "霰弹枪未解锁";
					textColor = new Color(0.6f, 0.6f, 0.6f);
				}
				else if (!_cachedAugIntroduced)
				{
					text = "Aug 尚未引入";
					textColor = new Color(0.6f, 0.8f, 1f);
				}
				else if (_cachedAugEnded)
				{
					text = "Aug 契约已完成";
					textColor = new Color(0.5f, 1f, 0.5f);
				}
				else
				{
					text = "Aug 契约进行中";
					textColor = new Color(1f, 0.75f, 0.3f);
				}
				val.normal.textColor = textColor;
				GUI.Label(new Rect(num, num3, num2, 26f), text, val);
				num3 += 32f;
				GUI.Label(new Rect(num, num3, num2, 20f), "─── 核心数值 ───", val2);
				num3 += 24f;
				GUI.Label(new Rect(num, num3, num2, 22f), "存在感：" + _cachedPresence, val3);
				num3 += 24f;
				string text2 = (_cachedHardMode ? "困难" : "普通") + " · " + (_cachedHandlingShootout ? "对峙中" : "空闲");
				GUI.Label(new Rect(num, num3, num2, 22f), "模式 · 状态：" + text2, val3);
				num3 += 24f;
				GUI.Label(new Rect(num, num3, num2, 22f), "当前天数：" + _cachedDay, val3);
				num3 += 24f;
				GUI.Label(new Rect(num, num3, num2, 22f), BuildNextAugText(), val3);
				num3 += 28f;
				GUI.Label(new Rect(num, num3, num2, 20f), "─── 今日追踪 ───", val2);
				num3 += 24f;
				GUI.Label(new Rect(num, num3, num2, 22f), "生成：" + _cachedSpawned, val3);
				num3 += 24f;
				GUI.Label(new Rect(num, num3, num2, 22f), "需击杀：" + _cachedRequired, val3);
				num3 += 24f;
				string text3 = "已击杀：" + _cachedKilled + "/" + _cachedRequired + "  " + ((_cachedKilled >= _cachedRequired) ? "[已完成]" : "[未完成]");
				GUI.Label(new Rect(num, num3, num2, 22f), text3, val3);
				num3 += 24f;
				GUI.Label(new Rect(num, num3, num2, 22f), "有 Aug 放走：" + (_cachedEscapedAlive ? "[是]" : "[否]"), val3);
				num3 += 28f;
				if (_cachedCurrentIsAug)
				{
					GUI.Label(new Rect(num, num3, num2, 20f), "─── 当前客户 ───", val2);
					num3 += 24f;
					string text4 = (_cachedCurrentIsAugScanned ? "已扫描识别" : "未扫描，可能是伪装");
					GUI.Label(new Rect(num, num3, num2, 24f), "当前客户是 Aug（" + text4 + "）", val4);
					num3 += 28f;
				}
				GUI.Label(new Rect(num, _augWindowRect.height - 20f, num2 - 22f, 16f), $"标题栏拖动 · 右下角调整大小 · {_augPanelKey.Value} 开关", val6);
				if (Event.current != null && (int)Event.current.type == 1)
				{
					_augPanelX.Value = _augWindowRect.x;
					_augPanelY.Value = _augWindowRect.y;
					_augPanelWidth.Value = _augWindowRect.width;
					_augPanelHeight.Value = _augWindowRect.height;
					MelonPreferences.Save();
				}
			}
			catch (Exception e)
			{
				LogException("DrawAugPanel", e);
			}
		}

		private static string BuildNextAugText()
		{
			if (_cachedNextAugSpawnDay < 0)
			{
				return "下次 Aug：未排程";
			}
			int num = _cachedNextAugSpawnDay - _cachedDay;
			if (num <= 0)
			{
				return "下次 Aug：已到生成日";
			}
			if (num == 1)
			{
				return "下次 Aug：预计 1 天后";
			}
			return "下次 Aug：预计 " + num + " 天后";
		}

		public override void OnUpdate()
		{
			//IL_000c: Unknown result type (might be due to invalid IL or missing references)
			if (_augPanelKey != null && Input.GetKeyDown(_augPanelKey.Value))
			{
				_augPanelActive = !_augPanelActive;
				_augPanelVisible.Value = _augPanelActive;
				MelonPreferences.Save();
			}
			_augReadTimer += Time.deltaTime;
			if (!(_augReadTimer < 0.5f))
			{
				_augReadTimer = 0f;
				PollAugData();
			}
		}

		private static void PollAugData()
		{
			try
			{
				_cachedLastError = "";
				_cachedDay = StoreStation.GetDayCounter();
				_cachedSpawned = AugHuntContract.SpawnedCount;
				_cachedRequired = AugHuntContract.RequiredKillCount;
				_cachedKilled = AugHuntContract.KilledCount;
				_cachedEscapedAlive = AugHuntContract.EscapedAlive;
				_cachedDayKey = AugHuntContract.CurrentDayKey ?? "";
				if (!PlayerStore.IsInstanceExist())
				{
					_augDataReady = false;
					return;
				}
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					_augDataReady = false;
					return;
				}
				_cachedPresence = instance.augPresence;
				_cachedHardMode = instance.isHardMode;
				_cachedShootingUnlocked = instance.isShootingUnlocked;
				_cachedAugIntroduced = instance.isAugIntroduced;
				_cachedAugEnded = instance.isAugEnded;
				_cachedHandlingShootout = instance.isHandlingShootout;
				_cachedNextAugSpawnDay = instance.nextAugSpawnDay;
				StoreClientManager storeClientManager = instance.storeClientManager;
				_cachedSpawnHandledToday = storeClientManager != null && storeClientManager.isAugSpawnHandledToday;
				StoreClient currentStoreClient = AugHuntContract.GetCurrentStoreClient();
				if (currentStoreClient != null)
				{
					_cachedCurrentIsAug = currentStoreClient.isAug;
					_cachedCurrentIsAugScanned = currentStoreClient.isAugScanned;
				}
				else
				{
					_cachedCurrentIsAug = false;
					_cachedCurrentIsAugScanned = false;
				}
				_augDataReady = true;
			}
			catch (Exception ex)
			{
				_augDataReady = false;
				_cachedLastError = ex.Message;
			}
		}

		static Main()
		{
			//IL_0014: Unknown result type (might be due to invalid IL or missing references)
			//IL_0019: Unknown result type (might be due to invalid IL or missing references)
		}
	}
}
