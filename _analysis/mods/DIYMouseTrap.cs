using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using MouseTrapTweaks;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(MouseTrapTweaksMod), "DIY 捕鼠夹", "1.0.1", "核桃", null)]
[assembly: MelonGame(null, null)]
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
namespace MouseTrapTweaks
{
	public sealed class MouseTrapTweaksMod : MelonMod
	{
		[HarmonyPatch(typeof(PlayerStore), "EndNight")]
		private static class EndNightPatch
		{
			private static void Postfix()
			{
				if (!_enable.Value || Time.realtimeSinceStartup - _lastAutoCatchTime < 5f)
				{
					return;
				}
				_daysSinceLastAutoCatch++;
				if (_daysSinceLastAutoCatch >= _autoCatchIntervalDays.Value)
				{
					_daysSinceLastAutoCatch = 0;
					_lastAutoCatchTime = Time.realtimeSinceStartup;
					int num = TryAutoCatch();
					if (num > 0)
					{
						DistributeExtraLoot(num);
					}
				}
			}
		}

		private static Instance _log;

		private static MelonPreferences_Category _cfg;

		private static MelonPreferences_Entry<bool> _enable;

		private static MelonPreferences_Entry<int> _autoCatchIntervalDays;

		private static MelonPreferences_Entry<float> _autoCatchChance;

		private static MelonPreferences_Entry<int> _extraLootCount;

		private static MelonPreferences_Entry<float> _extraLootChance;

		private static Harmony _harmony;

		private const string RatItemId = "rat";

		private const string TrapItemId = "mouse_trap";

		private const string ExtraLootItemId = "rat";

		private static readonly bool DebugLog = false;

		private static int _daysSinceLastAutoCatch = 0;

		private static float _lastAutoCatchTime = -999f;

		public override void OnInitializeMelon()
		{
			//IL_0133: Unknown result type (might be due to invalid IL or missing references)
			//IL_013d: Expected Obj, but got Unknown
			_log = ((MelonBase)this).LoggerInstance;
			_cfg = MelonPreferences.CreateCategory("DIY捕鼠夹");
			_enable = _cfg.CreateEntry<bool>("Enable", true, "总开关", (string)null, false, false, (ValueValidator)null, (string)null);
			_autoCatchIntervalDays = _cfg.CreateEntry<int>("AutoCatchIntervalDays", 1, "自动捕捉间隔天数（至少1天）", (string)null, false, false, (ValueValidator)null, (string)null);
			_autoCatchChance = _cfg.CreateEntry<float>("AutoCatchChance", 0.1f, "每个可抓捕鼠夹独立触发概率（0=永不；1=每晚必抓）", (string)null, false, false, (ValueValidator)null, (string)null);
			_extraLootCount = _cfg.CreateEntry<int>("ExtraLootCount", 1, "每只老鼠额外产出数量", (string)null, false, false, (ValueValidator)null, (string)null);
			_extraLootChance = _cfg.CreateEntry<float>("ExtraLootChance", 0.1f, "每只老鼠独立触发额外产出的概率", (string)null, false, false, (ValueValidator)null, (string)null);
			_autoCatchChance.Value = Mathf.Clamp01(_autoCatchChance.Value);
			_extraLootChance.Value = Mathf.Clamp01(_extraLootChance.Value);
			_autoCatchIntervalDays.Value = Math.Max(1, _autoCatchIntervalDays.Value);
			_extraLootCount.Value = Math.Max(0, _extraLootCount.Value);
			_cfg.SaveToFile(true);
			_harmony = new Harmony("MouseTrapTweaks");
			_harmony.PatchAll();
			_log.Msg("捕鼠夹增强 mod 已加载。");
		}

		public override void OnDeinitializeMelon()
		{
			Harmony harmony = _harmony;
			if (harmony != null)
			{
				harmony.UnpatchSelf();
			}
		}

		private static void RandomizeRat(GameItem rat)
		{
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			if (rat == null)
			{
				return;
			}
			try
			{
				new __c()._CreateMouseTrap_b__1_4(rat);
				if (DebugLog)
				{
					_log.Msg($"老鼠随机化完成：uniqueId={rat.uniqueId}");
				}
			}
			catch (Exception ex)
			{
				_log.Warning("老鼠随机化失败：" + ex.Message);
			}
		}

		private static int TryAutoCatch()
		{
			int num = 0;
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance == (Object)null)
				{
					return 0;
				}
				List<GameItem> allOwnedItems = instance.GetAllOwnedItems(false, true);
				if (allOwnedItems == null)
				{
					return 0;
				}
				Enumerator<GameItem> enumerator = allOwnedItems.GetEnumerator();
				while (enumerator.MoveNext())
				{
					GameItem current = enumerator.Current;
					if (current == null || !current.IsID("mouse_trap"))
					{
						continue;
					}
					List<GameItem> val = current.FindAllChildItems();
					bool flag = false;
					if (val != null)
					{
						Enumerator<GameItem> enumerator2 = val.GetEnumerator();
						while (enumerator2.MoveNext())
						{
							GameItem current2 = enumerator2.Current;
							if (current2 != null && current2.IsID("rat"))
							{
								flag = true;
								break;
							}
						}
					}
					if (flag || Random.value > _autoCatchChance.Value)
					{
						continue;
					}
					try
					{
						GameItem val2 = DirectoryMaster.Item("rat", true);
						if (val2 == null)
						{
							continue;
						}
						RandomizeRat(val2);
						SlotMarker val3 = current.TryFindOneValidInventorySlot(val2);
						if (val3 == null)
						{
							continue;
						}
						val3.item = val2;
						if (val3.AcceptUnchecked() > 0)
						{
							num++;
							if (DebugLog)
							{
								_log.Msg($"自动捕捉成功：捕鼠夹 uniqueId={current.uniqueId}");
							}
						}
					}
					catch (Exception ex)
					{
						_log.Warning("自动捕捉单个捕鼠夹失败：" + ex.Message);
					}
				}
				if (num > 0)
				{
					_log.Msg($"过夜自动捕捉：{num} 个捕鼠夹抓到了老鼠");
				}
			}
			catch (Exception ex2)
			{
				_log.Warning("自动捕捉异常：" + ex2.Message);
			}
			return num;
		}

		private static void DistributeExtraLoot(int gained)
		{
			if (gained <= 0 || _extraLootChance.Value <= 0f)
			{
				return;
			}
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)(object)instance == (Object)null)
				{
					return;
				}
				int num = 0;
				for (int i = 0; i < gained; i++)
				{
					if (Random.value <= _extraLootChance.Value)
					{
						num += _extraLootCount.Value;
					}
				}
				if (num <= 0)
				{
					return;
				}
				GameItem val = null;
				List<GameItem> allOwnedItemsShallow = instance.GetAllOwnedItemsShallow(false);
				if (allOwnedItemsShallow != null)
				{
					Enumerator<GameItem> enumerator = allOwnedItemsShallow.GetEnumerator();
					while (enumerator.MoveNext())
					{
						GameItem current = enumerator.Current;
						if (current != null && current.IsID("rat") && current.uniqueId > 0)
						{
							val = current;
							break;
						}
					}
				}
				if (val != null)
				{
					val.SetUnitCount(val.unitCount + num);
					_log.Msg($"额外奖励：{"rat"} x {num}");
					return;
				}
				GameItem val2 = DirectoryMaster.Item("rat", true);
				if (val2 == null)
				{
					_log.Warning("额外奖励发放失败：无法创建物品");
					return;
				}
				val2.SetUnitCount(num);
				instance.TryAddToPlayerInv(val2);
				_log.Msg($"额外奖励：{"rat"} x {num}");
			}
			catch (Exception ex)
			{
				_log.Warning("额外奖励发放失败：" + ex.Message);
			}
		}
	}
}
