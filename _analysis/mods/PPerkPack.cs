using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text.RegularExpressions;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using Il2CppTMPro;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using PPerkPack;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Core), "PPerkPack", "1.0.0", "PPerkPack", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: AssemblyCompany("PPerkPack")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("PPerkPack")]
[assembly: AssemblyTitle("PPerkPack")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
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
namespace PPerkPack
{
	public sealed class Core : MelonMod
	{
		public static Instance Log;

		public static MelonPreferences_Entry<int> JunkyardDigBonus;

		public static MelonPreferences_Entry<int> NightVisitBonus;

		public static MelonPreferences_Entry<int> AppraisalClientChance;

		public static MelonPreferences_Entry<float> MajorWoundReduction;

		public static MelonPreferences_Entry<int> AppraisalTotalCustomerBonus;

		public static MelonPreferences_Entry<int> AppraisalExtraClients;

		public static int DigBonusValue => Math.Max(0, JunkyardDigBonus?.Value ?? 5);

		public static int NightBonusValue => Math.Max(0, NightVisitBonus?.Value ?? 2);

		public static int AppraisalChanceValue => Math.Clamp(AppraisalClientChance?.Value ?? 60, 0, 200);

		public static float MajorWoundReductionValue => Math.Max(0f, MajorWoundReduction?.Value ?? 1f);

		public static int AppraisalTotalCustomerBonusValue => Math.Clamp(AppraisalTotalCustomerBonus?.Value ?? 2, 0, 10);

		public static int AppraisalExtraClientsValue => Math.Clamp(AppraisalExtraClients?.Value ?? 2, 0, 100);

		public override void OnInitializeMelon()
		{
			((MelonBase)this).LoggerInstance.Msg("[PPerkPack] v1.0.0 已加载");
			Log = ((MelonBase)this).LoggerInstance;
			MelonPreferences_Category val = MelonPreferences.CreateCategory("PPerkPack", "PPerkPack 设置");
			JunkyardDigBonus = val.CreateEntry<int>("JunkyardDigBonus", 3, "垃圾场额外挖掘次数", "拥有“垃圾场挖掘者”天赋时，在垃圾场（倾倒区）可额外挖掘的次数。", false, false, (ValueValidator)null, (string)null);
			NightVisitBonus = val.CreateEntry<int>("NightVisitBonus", 1, "夜晚额外出门次数", "拥有“夜猫子+”天赋时，打烊后额外增加的出门次数。", false, false, (ValueValidator)null, (string)null);
			AppraisalClientChance = val.CreateEntry<int>("AppraisalClientChance", 30, "鉴定之神：顾客出现概率(%)", "拥有“鉴定之神”天赋时，每位额外鉴定顾客的出现概率（百分比）。范围 0-200：0 为永不出现，100 为必定出现。", false, false, (ValueValidator)null, (string)null);
			MajorWoundReduction = val.CreateEntry<float>("MajorWoundReduction", 1f, "强健体魄：重伤概率减免", "拥有“强健体魄”天赋时，对重伤概率的减免量。1.0=100%（不再受重伤）；填 >1 无额外效果；填 0 等于无减免；负数会被钳制为 0。", false, false, (ValueValidator)null, (string)null);
			AppraisalTotalCustomerBonus = val.CreateEntry<int>("AppraisalTotalCustomerBonus", 1, "鉴定之神：每天顾客总数增加量", "拥有“鉴定之神”天赋时，每天的顾客总数增加的数量。范围 0-10：0 表示不增加。", false, false, (ValueValidator)null, (string)null);
			AppraisalExtraClients = val.CreateEntry<int>("AppraisalExtraClients", 2, "鉴定之神：额外鉴定顾客上限", "拥有“鉴定之神”天赋时，每天最多额外出现的鉴定顾客数量。范围 0-100：0 为不出现额外鉴定顾客。", false, false, (ValueValidator)null, (string)null);
			((MelonBase)this).HarmonyInstance.PatchAll(typeof(Core).Assembly);
			MelonPreferences.Save();
			Log.Msg($"PPerkPack 已加载：挖掘+{DigBonusValue}，夜猫子+{NightBonusValue}，鉴定概率{AppraisalChanceValue}%，重伤减免{MajorWoundReductionValue:0.##}");
		}
	}
	public abstract class CustomPerk
	{
		public abstract string Id { get; }

		public abstract string DisplayName { get; }

		public abstract string Description { get; }

		public virtual int Cost => 1;

		public virtual int MaxSlot => 0;

		public virtual string[] IncompatibleIds => Array.Empty<string>();

		public virtual void OnNewGame()
		{
		}

		public virtual void OnNewDay()
		{
		}

		public StartingPerk Create()
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0011: Unknown result type (might be due to invalid IL or missing references)
			//IL_001d: Unknown result type (might be due to invalid IL or missing references)
			//IL_002a: Expected Obj, but got Unknown
			StartingPerk val = new StartingPerk
			{
				id = Id,
				cost = Cost,
				maxSlot = MaxSlot
			};
			if (val.incompatiblePerks != null)
			{
				string[] incompatibleIds = IncompatibleIds;
				foreach (string text in incompatibleIds)
				{
					if (!string.IsNullOrEmpty(text))
					{
						val.incompatiblePerks.Add(text);
					}
				}
			}
			return val;
		}
	}
	public sealed class JunkyardDiggerPerk : CustomPerk
	{
		public const string PerkId = "custom_junkyard_digger";

		public override string Id => "custom_junkyard_digger";

		public override string DisplayName => "垃圾场挖掘者";

		public override string Description => $"在垃圾场（倾倒区）可额外挖掘 {Core.DigBonusValue} 次。";

		public override int Cost => 1;
	}
	public sealed class NightOwlPlusPerk : CustomPerk
	{
		public const string PerkId = "custom_night_owl_plus";

		private static string _lastNightKey;

		public override string Id => "custom_night_owl_plus";

		public override string DisplayName => "夜猫子+";

		public override string Description => $"打烊后可额外出门 {Core.NightBonusValue} 次。";

		public override int Cost => 1;

		public static void ApplyVisitBonus()
		{
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return;
			}
			string text = $"{instance.runID}|{instance.startingDay}|{instance.dayUntilRent}";
			if (!(text == _lastNightKey))
			{
				_lastNightKey = text;
				instance.visitLeftTonight += Core.NightBonusValue;
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg($"夜猫子+已生效：visitLeftTonight +{Core.NightBonusValue}");
				}
			}
		}
	}
	public sealed class RobustPhysiquePerk : CustomPerk
	{
		public const string PerkId = "custom_robust_physique";

		public override string Id => "custom_robust_physique";

		public override string DisplayName => "强健体魄";

		public override string Description => "挖掘时不再受重伤。";

		public override int Cost => 1;
	}
	public sealed class AppraisalGodPerk : CustomPerk
	{
		public const string PerkId = "custom_appraisal_god";

		public override string Id => "custom_appraisal_god";

		public override string DisplayName => "鉴定之神";

		public override string Description => $"经过多年的鉴定生涯，你对于鉴定就像呼吸一般熟悉。橱窗摆放鉴定证书时，每天最多额外出现{Core.AppraisalExtraClientsValue}位鉴定顾客，每位有{Core.AppraisalChanceValue}%的概率出现；同时每天的顾客总数增加{Core.AppraisalTotalCustomerBonusValue}位。";

		public override int Cost => 3;
	}
	[HarmonyPatch(typeof(StartingPerkList), "InitStartingPerk")]
	internal static class InitStartingPerkPatch
	{
		private static void Postfix()
		{
			PerkRegistry.EnsureRegistered();
		}
	}
	[HarmonyPatch(typeof(StartingPerkContent), "HandleNewGamePerk")]
	internal static class HandleNewGamePerkPatch
	{
		private static void Postfix()
		{
			PerkRegistry.NotifyNewGame();
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "GetLocalizedDisplayName")]
	internal static class PerkDisplayNamePatch
	{
		private static bool Prefix(StartingPerk __instance, ref string __result)
		{
			if (__instance != null && PerkRegistry.TryGetLoc(__instance.id, out var displayName, out var _))
			{
				__result = displayName;
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "GetLocalizedDescription")]
	internal static class PerkDescriptionPatch
	{
		private static bool Prefix(StartingPerk __instance, ref string __result)
		{
			if (__instance != null && PerkRegistry.TryGetLoc(__instance.id, out var _, out var description))
			{
				__result = description;
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(LocHelper), "GetLocalizedPerkTable", new Type[]
	{
		typeof(string),
		typeof(Il2CppReferenceArray<Object>)
	})]
	internal static class PerkTableLocPatch
	{
		private static bool Prefix(string key, ref string __result)
		{
			if (string.IsNullOrEmpty(key) || !key.StartsWith("perk_"))
			{
				return true;
			}
			bool flag = key.EndsWith("_name");
			bool flag2 = key.EndsWith("_desc");
			if (!flag && !flag2)
			{
				return true;
			}
			if (PerkRegistry.TryGetLoc(key.Substring(5, key.Length - 10), out var displayName, out var description))
			{
				__result = (flag ? displayName : description);
				return false;
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "OpenUI")]
	internal static class PerkUiOpenPatch
	{
		private static void Postfix(PerkUIController __instance)
		{
			PerkRegistry.EnsurePickerElements(__instance);
		}
	}
	[HarmonyPatch(typeof(StartingPerkElement), "Start")]
	internal static class StartingPerkElementStartPatch
	{
		private static void Postfix(StartingPerkElement __instance)
		{
			PerkRegistry.EnsureElement(__instance);
		}
	}
	[HarmonyPatch(typeof(ScavHelper), "CanScavenge")]
	internal static class CanScavengePatch
	{
		private static void Postfix(ref bool __result)
		{
			try
			{
				if (!StartingPerk.IsPerkActive("custom_junkyard_digger"))
				{
					return;
				}
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					return;
				}
				int scavengingAttempts = instance.scavengingAttempts;
				int num = UpdateScavUIPatch.NativeMaxScavAttempts;
				if (num <= 0)
				{
					num = 5;
				}
				HealthData healthData = instance.healthData;
				if ((healthData == null || !healthData.isWoundedFresh) && scavengingAttempts >= num && scavengingAttempts < num + Core.DigBonusValue)
				{
					__result = true;
					Instance log = Core.Log;
					if (log != null)
					{
						log.Msg($"垃圾场挖掘者：放开第 {scavengingAttempts + 1} 次挖掘限制");
					}
				}
			}
			catch (Exception value)
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Error($"CanScavenge 补丁失败：{value}");
				}
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "EndDay")]
	internal static class NightOwlEndDayPatch
	{
		private static void Postfix()
		{
			if (StartingPerk.IsPerkActive("custom_night_owl_plus"))
			{
				NightOwlPlusPerk.ApplyVisitBonus();
			}
		}
	}
	[HarmonyPatch(typeof(ScavHelper), "RollMajorWound")]
	internal static class RollMajorWoundPatch
	{
		private static bool Prefix(ref bool __result)
		{
			try
			{
				if (!StartingPerk.IsPerkActive("custom_robust_physique"))
				{
					return true;
				}
				float majorWoundChance = ScavHelper.GetMajorWoundChance();
				float num = Mathf.Max(0f, majorWoundChance - Core.MajorWoundReductionValue);
				__result = Random.value < num;
				return false;
			}
			catch (Exception value)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error($"重伤判定补丁失败：{value}");
				}
			}
			return true;
		}
	}
	[HarmonyPatch(typeof(MapUIManager), "UpdateScavUI")]
	internal static class UpdateScavUIPatch
	{
		private static int _nativeMaxScavAttempts = -1;

		public static int NativeMaxScavAttempts => _nativeMaxScavAttempts;

		public static void ResetNativeMax()
		{
			_nativeMaxScavAttempts = -1;
		}

		private static void Postfix()
		{
			try
			{
				MapUIManager instance = MapUIManager.Instance;
				if ((Object)(object)instance == (Object)null || (Object)(object)instance.UIScavengingPanel == (Object)null)
				{
					return;
				}
				Il2CppArrayBase<TextMeshProUGUI> componentsInChildren = instance.UIScavengingPanel.GetComponentsInChildren<TextMeshProUGUI>(true);
				TextMeshProUGUI val = null;
				foreach (TextMeshProUGUI item in componentsInChildren)
				{
					if ((Object)(object)item != (Object)null && ((Object)item).name == "TimeTMP")
					{
						val = item;
						break;
					}
				}
				if ((Object)(object)val != (Object)null)
				{
					string text = ((TMP_Text)val).text;
					Match match = Regex.Match(text ?? "", "(-?\\d+)");
					if (match.Success)
					{
						int num = int.Parse(match.Groups[1].Value);
						PlayerStore instance2 = PlayerStore.Instance;
						int num2 = ((instance2 != null) ? instance2.scavengingAttempts : 0);
						if (!StartingPerk.IsPerkActive("custom_junkyard_digger"))
						{
							if (num > 0)
							{
								int num3 = num + num2;
								if (_nativeMaxScavAttempts < 0 || num3 < _nativeMaxScavAttempts)
								{
									_nativeMaxScavAttempts = num3;
								}
							}
						}
						else
						{
							if (num > 0)
							{
								int num4 = num + num2;
								if (_nativeMaxScavAttempts < 0 || num4 < _nativeMaxScavAttempts)
								{
									_nativeMaxScavAttempts = num4;
								}
							}
							else if (_nativeMaxScavAttempts < 0)
							{
								_nativeMaxScavAttempts = 5;
							}
							int num5 = _nativeMaxScavAttempts + Core.DigBonusValue - num2;
							if (num5 < 0)
							{
								num5 = 0;
							}
							if (num != num5)
							{
								((TMP_Text)val).text = text.Substring(0, match.Groups[1].Index) + num5 + text.Substring(match.Groups[1].Index + match.Groups[1].Length);
							}
						}
					}
				}
				if (!StartingPerk.IsPerkActive("custom_robust_physique"))
				{
					return;
				}
				float num6 = Core.MajorWoundReductionValue * 100f;
				foreach (TextMeshProUGUI item2 in componentsInChildren)
				{
					if ((Object)(object)item2 == (Object)null || ((Object)item2).name != "injuryMajorTMP")
					{
						continue;
					}
					Match match2 = Regex.Match(((TMP_Text)item2).text ?? "", "(\\d+(?:\\.\\d+)?)");
					if (match2.Success)
					{
						float num7 = float.Parse(match2.Groups[1].Value, CultureInfo.InvariantCulture);
						float num8 = Mathf.Max(0f, num7 - num6);
						if (Mathf.Abs(num7 - num8) >= 0.001f)
						{
							((TMP_Text)item2).text = ((TMP_Text)item2).text.Substring(0, match2.Groups[1].Index) + num8.ToString("0.00", CultureInfo.InvariantCulture) + ((TMP_Text)item2).text.Substring(match2.Groups[1].Index + match2.Groups[1].Length);
						}
						break;
					}
				}
			}
			catch (Exception value)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error($"更新 ScavUI 失败：{value}");
				}
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "HandleNormalClient")]
	internal static class AppraisalGodStartDayPatch
	{
		private static string _lastDayKey;

		private static void Postfix(StoreClientManager __instance)
		{
			try
			{
				if (!StartingPerk.IsPerkActive("custom_appraisal_god"))
				{
					return;
				}
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null || !ShowcaseHelper.IsShowcaseContainItemById("cert1"))
				{
					return;
				}
				bool flag = false;
				try
				{
					flag = instance.isAppraisalIntroduced;
				}
				catch
				{
					return;
				}
				if (!flag)
				{
					return;
				}
				string text = $"{instance.runID}|{instance.startingDay}|{instance.dayUntilRent}";
				if (text == _lastDayKey)
				{
					return;
				}
				_lastDayKey = text;
				if (__instance.clientStack != null)
				{
					Enumerator<StoreClient> enumerator = __instance.clientStack.GetEnumerator();
					while (enumerator.MoveNext())
					{
						StoreClient current = enumerator.Current;
						if (current != null && current.eventSourceId != null && current.eventSourceId.StartsWith("custom_appraisal_god"))
						{
							return;
						}
					}
				}
				int num = 0;
				for (int i = 0; i < Core.AppraisalExtraClientsValue; i++)
				{
					if (PositiveModulo(StableHash($"{instance.runID}|custom_appraisal_god|{instance.startingDay}|{instance.dayUntilRent}|{i}"), 100) < Core.AppraisalChanceValue)
					{
						StoreClient val = ((i != 0) ? StoreClientListAppraisal.CreateAppraisal1() : StoreClientListAppraisal.CreateAppraisalRandom());
						if (val != null)
						{
							val.eventSourceId = $"custom_appraisal_god_{i}";
							__instance.AddClient(val);
							num++;
						}
					}
				}
				Instance log = Core.Log;
				if (log != null)
				{
					log.Msg($"鉴定之神：HandleNormalClient 后 add {num} 位");
				}
			}
			catch (Exception value)
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Error($"鉴定之神生成失败：{value}");
				}
			}
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
			if (num < 0)
			{
				return num + modulo;
			}
			return num;
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "GetNormalCustomerCount")]
	internal static class AppraisalGodCustomerCountPatch
	{
		private static void Postfix(ref int __result)
		{
			try
			{
				if (!StartingPerk.IsPerkActive("custom_appraisal_god"))
				{
					return;
				}
				PlayerStore instance = PlayerStore.Instance;
				if (instance != null && ShowcaseHelper.IsShowcaseContainItemById("cert1"))
				{
					bool flag = false;
					try
					{
						flag = instance.isAppraisalIntroduced;
					}
					catch
					{
						return;
					}
					if (flag)
					{
						__result += Core.AppraisalTotalCustomerBonusValue;
					}
				}
			}
			catch (Exception value)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error($"鉴定之神顾客名额加成失败：{value}");
				}
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class ResetStaticCachePatch
	{
		private static void Postfix()
		{
			UpdateScavUIPatch.ResetNativeMax();
		}
	}
	public static class PerkRegistry
	{
		public static readonly CustomPerk[] All = new CustomPerk[4]
		{
			new JunkyardDiggerPerk(),
			new NightOwlPlusPerk(),
			new RobustPhysiquePerk(),
			new AppraisalGodPerk()
		};

		public static CustomPerk Find(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			CustomPerk[] all = All;
			foreach (CustomPerk customPerk in all)
			{
				if (customPerk.Id == id)
				{
					return customPerk;
				}
			}
			return null;
		}

		public static bool TryGetLoc(string id, out string displayName, out string description)
		{
			displayName = null;
			description = null;
			CustomPerk customPerk = Find(id);
			if (customPerk == null)
			{
				return false;
			}
			displayName = customPerk.DisplayName;
			description = customPerk.Description;
			return true;
		}

		public static void EnsureRegistered()
		{
			try
			{
				List<StartingPerk> perks = StartingPerkList.Perks;
				if (perks == null)
				{
					return;
				}
				CustomPerk[] all = All;
				foreach (CustomPerk customPerk in all)
				{
					if (IndexOf(perks, customPerk.Id) < 0)
					{
						perks.Add(customPerk.Create());
						Instance log = Core.Log;
						if (log != null)
						{
							log.Msg($"注册天赋：{customPerk.Id}（消耗 {customPerk.Cost} 点）");
						}
					}
				}
			}
			catch (Exception value)
			{
				Instance log2 = Core.Log;
				if (log2 != null)
				{
					log2.Error($"注册天赋失败：{value}");
				}
			}
		}

		public static StartingPerk GetOrCreate(CustomPerk perk)
		{
			List<StartingPerk> perks = StartingPerkList.Perks;
			if (perks != null)
			{
				int num = IndexOf(perks, perk.Id);
				if (num >= 0)
				{
					return perks[num];
				}
			}
			return perk.Create();
		}

		private static int IndexOf(List<StartingPerk> list, string id)
		{
			for (int i = 0; i < list.Count; i++)
			{
				StartingPerk val = list[i];
				if (val != null && val.id == id)
				{
					return i;
				}
			}
			return -1;
		}

		public static void EnsurePickerElements(PerkUIController ui)
		{
			try
			{
				if ((Object)(object)ui == (Object)null)
				{
					return;
				}
				EnsureRegistered();
				bool flag = false;
				CustomPerk[] all = All;
				foreach (CustomPerk customPerk in all)
				{
					if ((Object)(object)FindElement(ui.availablePerks, customPerk.Id) == (Object)null)
					{
						AddPickerElement(ui, customPerk);
						flag = true;
					}
				}
				if (flag && (Object)(object)ui.availablePerks != (Object)null)
				{
					ui.SortPerkContainer(ui.availablePerks);
				}
			}
			catch (Exception value)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error($"注入天赋界面失败：{value}");
				}
			}
		}

		public static void AddPickerElement(PerkUIController ui, CustomPerk perk)
		{
			if ((Object)(object)ui == (Object)null || perk == null || (Object)(object)ui.perkElementPrefab == (Object)null || (Object)(object)ui.availablePerks == (Object)null)
			{
				return;
			}
			GameObject val = Object.Instantiate<GameObject>(ui.perkElementPrefab, ui.availablePerks.transform);
			if (!((Object)(object)val == (Object)null))
			{
				StartingPerkElement component = val.GetComponent<StartingPerkElement>();
				if (!((Object)(object)component == (Object)null))
				{
					component.id = perk.Id;
					component.isSelected = false;
					ApplyToElement(component, perk);
					val.SetActive(true);
				}
			}
		}

		public static void ApplyToElement(StartingPerkElement element, CustomPerk perk)
		{
			if (!((Object)(object)element == (Object)null) && perk != null)
			{
				element.perk = GetOrCreate(perk);
				if (element.perk != null)
				{
					element.perk.maxSlot = perk.MaxSlot;
				}
			}
		}

		public static StartingPerkElement FindElement(GameObject parent, string id)
		{
			if ((Object)(object)parent == (Object)null || string.IsNullOrEmpty(id))
			{
				return null;
			}
			foreach (StartingPerkElement componentsInChild in parent.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && componentsInChild.id == id)
				{
					return componentsInChild;
				}
			}
			return null;
		}

		public static void EnsureElement(StartingPerkElement element)
		{
			if (!((Object)(object)element == (Object)null))
			{
				CustomPerk customPerk = Find(element.id);
				if (customPerk != null)
				{
					ApplyToElement(element, customPerk);
				}
			}
		}

		public static void NotifyNewGame()
		{
			try
			{
				CustomPerk[] all = All;
				foreach (CustomPerk customPerk in all)
				{
					if (StartingPerk.IsPerkActive(customPerk.Id))
					{
						customPerk.OnNewGame();
					}
				}
			}
			catch (Exception value)
			{
				Instance log = Core.Log;
				if (log != null)
				{
					log.Error($"处理新游戏天赋失败：{value}");
				}
			}
		}

		public static void NotifyNewDay()
		{
			CustomPerk[] all = All;
			foreach (CustomPerk customPerk in all)
			{
				if (StartingPerk.IsPerkActive(customPerk.Id))
				{
					customPerk.OnNewDay();
				}
			}
		}
	}
}
