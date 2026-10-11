using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;
using ExtraPerks;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
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
[assembly: MelonInfo(typeof(Core), "ExtraPerks", "0.2.0", "igonzalez", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("ExtraPerks")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("0.2.0.0")]
[assembly: AssemblyInformationalVersion("0.2.0+4fff3766d17c3fea9f3d2aa3ca11fefb94e582e2")]
[assembly: AssemblyProduct("ExtraPerks")]
[assembly: AssemblyTitle("ExtraPerks")]
[assembly: AssemblyVersion("0.2.0.0")]
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
namespace ExtraPerks
{
	internal sealed class BoardInsiderPerk : CustomStartingPerk
	{
		private static readonly string[] StarterItems = new string[5] { "cert1", "hand_guide", "fuel_guide", "ribwich_guide", "magnifier" };

		internal const string VisitId = "appraisalBoardIntro";

		internal const string FollowUpId = "appraisalBoardIntro2";

		internal override string Id => "董事会内线\0\0\0\0\0\0\0";

		internal override string DisplayName => "董事会内线\0\0\0\0\0\0\0\0";

		internal override string Description => "以初级鉴定委员会成员身份开局，自带证书、培训指南和一个放大镜。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string? IconResource => "board_insider.png";

		internal override void OnNewGame()
		{
			PlayerStore instance = PlayerStore.Instance;
			instance.isAppraisalIntroduced = true;
			instance.appraisalIntroducedDay = instance.startingDay;
			string[] starterItems = StarterItems;
			for (int i = 0; i < starterItems.Length; i++)
			{
				InventoryGrant.TryGrant(starterItems[i]);
			}
		}

		internal static bool IsActive()
		{
			return StartingPerk.IsPerkActive("董事会内线\0\0\0\0\0\0\0");
		}

		internal static bool IsBoardVisit(StoreClient? client)
		{
			if (client != null)
			{
				if (!(client.identifier == "appraisalBoardIntro"))
				{
					return client.identifier == "appraisalBoardIntro2";
				}
				return true;
			}
			return false;
		}

		internal static void StripBoardVisits(StoreClientManager? manager)
		{
			List<StoreClient> val = ((manager != null) ? manager.clientStack : null);
			if (val == null)
			{
				return;
			}
			for (int num = val.Count - 1; num >= 0; num--)
			{
				if (IsBoardVisit(val[num]))
				{
					val.RemoveAt(num);
				}
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientListStory), "CreateStoryAppraisalBoard")]
	internal static class BoardInsiderSkipJoinPatch
	{
		private static void Postfix(ref StoreClient __result)
		{
			if (BoardInsiderPerk.IsActive())
			{
				__result = null;
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientListStory), "CreateStoryAppraisalBoard2")]
	internal static class BoardInsiderSkipFollowUpPatch
	{
		private static void Postfix(ref StoreClient __result)
		{
			if (BoardInsiderPerk.IsActive())
			{
				__result = null;
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "HandleSkipClient")]
	[HarmonyPatch(typeof(StoreClientManager), "HandleClientFromIntro")]
	internal static class BoardInsiderStripIntroVisitPatch
	{
		private static void Postfix(StoreClientManager __instance)
		{
			if (BoardInsiderPerk.IsActive())
			{
				BoardInsiderPerk.StripBoardVisits(__instance);
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "TrackClient")]
	internal static class BoardInsiderTrackClientPatch
	{
		private static bool Prefix(StoreClientManager __instance, StoreClient storeClient)
		{
			if (!BoardInsiderPerk.IsActive() || !BoardInsiderPerk.IsBoardVisit(storeClient))
			{
				return true;
			}
			BoardInsiderPerk.StripBoardVisits(__instance);
			return false;
		}
	}
	public class Core : MelonMod
	{
		public static Instance Log { get; private set; }

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			WideConnectionsPerk.EnsurePrefs();
			((MelonBase)this).LoggerInstance.Msg("ExtraPerks 已加载。\0\0\0");
		}
	}
	internal abstract class CustomStartingPerk
	{
		internal abstract string Id { get; }

		internal abstract string DisplayName { get; }

		internal abstract string Description { get; }

		internal virtual int Cost => 1;

		internal virtual int MaxSlot => 0;

		internal virtual StartingPerkType Type => (StartingPerkType)0;

		internal virtual string? IconResource => null;

		internal virtual IReadOnlyList<string> IncompatibleIds => Array.Empty<string>();

		internal abstract void OnNewGame();

		internal virtual void OnNewDay()
		{
		}

		internal StartingPerk Create()
		{
			//IL_0036: Unknown result type (might be due to invalid IL or missing references)
			//IL_003b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0047: Unknown result type (might be due to invalid IL or missing references)
			//IL_0053: Unknown result type (might be due to invalid IL or missing references)
			//IL_005f: Unknown result type (might be due to invalid IL or missing references)
			//IL_0061: Unknown result type (might be due to invalid IL or missing references)
			//IL_006b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0073: Expected Obj, but got Unknown
			List<string> val = new List<string>();
			foreach (string incompatibleId in IncompatibleIds)
			{
				val.Add(incompatibleId);
			}
			return new StartingPerk
			{
				id = Id,
				cost = Cost,
				maxSlot = MaxSlot,
				type = Type,
				incompatiblePerks = val
			};
		}
	}
	internal static class CustomStartingPerks
	{
		internal static readonly CustomStartingPerk[] All = new CustomStartingPerk[10]
		{
			new ScavengersReignPerk(),
			new BoardInsiderPerk(),
			new WideConnectionsPerk(),
			new NocturnalPerk(),
			new DeepSleepPerk(),
			new SnobPerk(),
			new UglyShopPerk(),
			new FellOffTheTruckPerk(),
			new UltraColorfulCharacterPerk(),
			new PissedOffSecurityPerk()
		};

		private static readonly Dictionary<string, StartingPerk> Created = new Dictionary<string, StartingPerk>();

		internal static CustomStartingPerk? Find(string? id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return null;
			}
			CustomStartingPerk[] all = All;
			foreach (CustomStartingPerk customStartingPerk in all)
			{
				if (customStartingPerk.Id == id)
				{
					return customStartingPerk;
				}
			}
			return null;
		}

		internal static bool TryGetLoc(string? id, out string displayName, out string description)
		{
			displayName = "";
			description = "";
			CustomStartingPerk customStartingPerk = Find(id);
			if (customStartingPerk == null)
			{
				return false;
			}
			displayName = customStartingPerk.DisplayName;
			description = customStartingPerk.Description;
			return true;
		}

		internal static bool TryResolvePerkTableKey(string? key, out string text)
		{
			text = "";
			if (string.IsNullOrEmpty(key) || !key.StartsWith("perk_", StringComparison.Ordinal))
			{
				return false;
			}
			string id;
			bool flag;
			if (key.EndsWith("_name", StringComparison.Ordinal))
			{
				string text2 = key;
				id = text2.Substring(5, text2.Length - 5 - 5);
				flag = true;
			}
			else
			{
				if (!key.EndsWith("_desc", StringComparison.Ordinal))
				{
					return false;
				}
				string text2 = key;
				id = text2.Substring(5, text2.Length - 5 - 5);
				flag = false;
			}
			if (!TryGetLoc(id, out string displayName, out string description))
			{
				return false;
			}
			text = (flag ? displayName : description);
			return true;
		}

		internal static StartingPerk GetOrCreate(CustomStartingPerk custom)
		{
			if (Created.TryGetValue(custom.Id, out StartingPerk value) && value != null)
			{
				return value;
			}
			StartingPerk val = custom.Create();
			Created[custom.Id] = val;
			return val;
		}

		internal static void EnsureRegistered()
		{
			List<StartingPerk> perks = StartingPerkList.Perks;
			if (perks == null)
			{
				return;
			}
			CustomStartingPerk[] all = All;
			foreach (CustomStartingPerk customStartingPerk in all)
			{
				if (IndexOf(perks, customStartingPerk.Id) < 0)
				{
					perks.Add(GetOrCreate(customStartingPerk));
				}
			}
			UltraColorfulCharacterPerk.EnsureVanillaIncompatibility();
		}

		internal static void EnsureIcons()
		{
			Dictionary<string, Sprite> perkIcons = StartingPerkIconLoader.perkIcons;
			if (perkIcons == null)
			{
				return;
			}
			CustomStartingPerk[] all = All;
			foreach (CustomStartingPerk customStartingPerk in all)
			{
				if (!string.IsNullOrEmpty(customStartingPerk.IconResource) && !perkIcons.ContainsKey(customStartingPerk.Id))
				{
					Sprite val = PerkIcon.LoadEmbeddedPng(customStartingPerk.IconResource);
					if (!((Object)(object)val == (Object)null))
					{
						perkIcons[customStartingPerk.Id] = val;
					}
				}
			}
		}

		internal static void EnsurePickerElements(PerkUIController ui)
		{
			EnsureRegistered();
			EnsureIcons();
			bool flag = false;
			CustomStartingPerk[] all = All;
			foreach (CustomStartingPerk customStartingPerk in all)
			{
				StartingPerkElement val = FindElement(ui.availablePerks, customStartingPerk.Id);
				if ((Object)(object)val != (Object)null)
				{
					ApplyToElement(val, customStartingPerk);
					continue;
				}
				AddPickerElement(ui, customStartingPerk);
				flag = true;
			}
			if (flag)
			{
				ui.SortPerkContainer(ui.availablePerks);
			}
		}

		internal static void EnsureStatusElements(AdvCalendarUIManager ui)
		{
			EnsureRegistered();
			EnsureIcons();
			if ((Object)(object)((ui != null) ? ui.activePerksContainer : null) == (Object)null)
			{
				return;
			}
			foreach (StartingPerkElement componentsInChild in ui.activePerksContainer.GetComponentsInChildren<StartingPerkElement>(true))
			{
				EnsureElement(componentsInChild);
			}
		}

		internal static void EnsureElement(StartingPerkElement? element)
		{
			if (!((Object)(object)element == (Object)null))
			{
				CustomStartingPerk customStartingPerk = Find(element.id);
				if (customStartingPerk != null)
				{
					ApplyToElement(element, customStartingPerk);
				}
			}
		}

		internal static void NotifyNewGame()
		{
			CustomStartingPerk[] all = All;
			foreach (CustomStartingPerk customStartingPerk in all)
			{
				if (StartingPerk.IsPerkActive(customStartingPerk.Id))
				{
					customStartingPerk.OnNewGame();
				}
			}
		}

		internal static void NotifyNewDay()
		{
			CustomStartingPerk[] all = All;
			foreach (CustomStartingPerk customStartingPerk in all)
			{
				if (StartingPerk.IsPerkActive(customStartingPerk.Id))
				{
					customStartingPerk.OnNewDay();
				}
			}
		}

		private static void AddPickerElement(PerkUIController ui, CustomStartingPerk custom)
		{
			GameObject val = Object.Instantiate<GameObject>(ui.perkElementPrefab, ui.availablePerks.transform);
			StartingPerkElement component = val.GetComponent<StartingPerkElement>();
			component.id = custom.Id;
			component.isSelected = false;
			ApplyToElement(component, custom);
			val.SetActive(true);
		}

		private static void ApplyToElement(StartingPerkElement element, CustomStartingPerk custom)
		{
			StartingPerk orCreate = GetOrCreate(custom);
			orCreate.maxSlot = custom.MaxSlot;
			element.perk = orCreate;
			if (!((Object)(object)element.icon == (Object)null))
			{
				EnsureIcons();
				Dictionary<string, Sprite> perkIcons = StartingPerkIconLoader.perkIcons;
				if (perkIcons != null && perkIcons.ContainsKey(custom.Id))
				{
					element.icon.sprite = perkIcons[custom.Id];
				}
			}
		}

		private static StartingPerkElement? FindElement(GameObject root, string id)
		{
			foreach (StartingPerkElement componentsInChild in root.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && componentsInChild.id == id)
				{
					return componentsInChild;
				}
			}
			return null;
		}

		private static int IndexOf(List<StartingPerk> perks, string id)
		{
			for (int i = 0; i < perks.Count; i++)
			{
				if (perks[i] != null && perks[i].id == id)
				{
					return i;
				}
			}
			return -1;
		}
	}
	[HarmonyPatch(typeof(StartingPerkList), "InitStartingPerk")]
	internal static class InitStartingPerkPatch
	{
		private static void Postfix()
		{
			CustomStartingPerks.EnsureRegistered();
		}
	}
	[HarmonyPatch(typeof(StartingPerkIconLoader), "Start")]
	internal static class StartingPerkIconLoaderStartPatch
	{
		private static void Postfix()
		{
			CustomStartingPerks.EnsureIcons();
		}
	}
	[HarmonyPatch(typeof(StartingPerkElement), "Start")]
	internal static class StartingPerkElementStartPatch
	{
		private static void Postfix(StartingPerkElement __instance)
		{
			CustomStartingPerks.EnsureElement(__instance);
		}
	}
	[HarmonyPatch(typeof(AdvCalendarUIManager), "OnStatusButtonClicked")]
	internal static class StatusButtonClickedPatch
	{
		private static void Postfix(AdvCalendarUIManager __instance)
		{
			CustomStartingPerks.EnsureStatusElements(__instance);
		}
	}
	[HarmonyPatch(typeof(AdvCalendarUIManager), "Init")]
	internal static class AdvCalendarInitPatch
	{
		private static void Postfix(AdvCalendarUIManager __instance)
		{
			CustomStartingPerks.EnsureStatusElements(__instance);
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "OpenUI", new Type[] { })]
	internal static class PerkUiOpenPatch
	{
		private static void Postfix(PerkUIController __instance)
		{
			CustomStartingPerks.EnsurePickerElements(__instance);
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "GetLocalizedDisplayName")]
	internal static class PerkDisplayNamePatch
	{
		private static bool Prefix(StartingPerk __instance, ref string __result)
		{
			if (!CustomStartingPerks.TryGetLoc((__instance != null) ? __instance.id : null, out string displayName, out string _))
			{
				return true;
			}
			__result = displayName;
			return false;
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "GetLocalizedDescription")]
	internal static class PerkDescriptionPatch
	{
		private static bool Prefix(StartingPerk __instance, ref string __result)
		{
			if (!CustomStartingPerks.TryGetLoc((__instance != null) ? __instance.id : null, out string _, out string description))
			{
				return true;
			}
			__result = description;
			return false;
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
			if (!CustomStartingPerks.TryResolvePerkTableKey(key, out string text))
			{
				return true;
			}
			__result = text;
			return false;
		}
	}
	[HarmonyPatch(typeof(StartingPerkElement), "SetTooltipContent")]
	internal static class SetTooltipContentPatch
	{
		private static void Postfix(StartingPerk perk)
		{
			if (!CustomStartingPerks.TryGetLoc((perk != null) ? perk.id : null, out string displayName, out string description))
			{
				return;
			}
			PerkUIController instance = PerkUIController.Instance;
			if (!((Object)(object)instance == (Object)null))
			{
				if ((Object)(object)instance.startingPerkTooltipTitle != (Object)null)
				{
					((TMP_Text)instance.startingPerkTooltipTitle).text = displayName;
				}
				if ((Object)(object)instance.startingPerkTooltipDescription != (Object)null)
				{
					((TMP_Text)instance.startingPerkTooltipDescription).text = description;
				}
			}
		}
	}
	[HarmonyPatch(typeof(StartingPerkContent), "HandleNewGamePerk")]
	internal static class HandleNewGamePerkPatch
	{
		private static void Postfix()
		{
			CustomStartingPerks.NotifyNewGame();
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "BeginDay")]
	internal static class PlayerStoreBeginDayPatch
	{
		private static void Postfix()
		{
			CustomStartingPerks.NotifyNewDay();
		}
	}
	internal sealed class DeepSleepPerk : CustomStartingPerk
	{
		internal const string PerkId = "DeepSleep";

		private const int MissChancePercent = 25;

		internal override string Id => "深度睡眠\0\0\0\0\0";

		internal override string DisplayName => "深度睡眠\0\0\0\0\0\0";

		internal override string Description => "每天有25%概率错过一位顾客。可与夜间活动叠加。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override int Cost => -1;

		internal override StartingPerkType Type => (StartingPerkType)1;

		internal override string? IconResource => "deep_sleep.png";

		internal override IReadOnlyList<string> IncompatibleIds => new <>z__ReadOnlySingleElementList<string>("夜猫子\0\0\0\0\0\0");

		internal override void OnNewGame()
		{
		}

		internal static bool IsActive()
		{
			return StartingPerk.IsPerkActive("深度睡眠\0\0\0\0\0");
		}

		internal static void TryMissCustomer(StoreClientManager manager)
		{
			if (IsActive() && manager != null && Random.Range(0, 100) < 25)
			{
				manager.HandleSkipClient();
				PlayerStore.Instance.AddNightLog("你陷入深度睡眠，错过了一位顾客。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");
			}
		}
	}
	internal sealed class FellOffTheTruckPerk : CustomStartingPerk
	{
		internal const string PerkId = "FellOffTheTruck";

		internal const string ClientId = "robin_banks";

		internal const string ClientName = "Robin Banks";

		private const int CrateHeat = 100;

		private const int DaysPerWeek = 7;

		private const string NoStolenDiscountTag = "fell_off_no_stolen_discount";

		private static readonly string[] Greetings = new string[31]
		{
			"你每周的企业税销账份额到了！\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "这里签字，那里画叉，记住：有人问起就说这箱子是从一辆飞快的卡车上掉下来的。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "恭喜你，订阅者！你已解锁本周的'别问我这是哪来的'等级。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "特快专递：一个箱子，零份文书，还有一个序列号——它本来就长这样，真的。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "运输途中遗失，在你店里找到。宇宙正在自愈。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "这箱子无人认领、未拆封，而且从法律上讲，极度假设性存在。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "货我带来了。合理推诿你负责。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "今日箱子由糟糕的库存管理和精准的时机为您呈现。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我倒是想给你收据，但收据就是文明崩塌的元凶。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "农场直供。有机。散养。查无此货。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0",
			"我把序列号归在'现代艺术'类目下。非常抽象。非常消失。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "这批货经历了三次审计、两次谎言和一辆飞快的卡车，幸存下来。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我不问你卖到哪。你不问从哪辆卡车来的。成交？\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "这不是违禁品，这是惊喜。违禁品是他们意料之中的那种。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "送达完成。如果它哔哔响、冒烟或者报警，那是你的问题。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我走的风景路线。经过摄像头。绕过摄像头。有一次穿过了一个摄像头。不推荐。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "箱子一失踪，股东价值就上涨。不客气，资本主义。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "本周盲盒：可能是医疗品，可能是安保装备，也可能是我的辞职信。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "Keep the lid closed until I'm gone. Schr?dinger's felony works better that way.", "如果它有货单，那货单从另一辆卡车上掉了。这行业真够粗心的。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0",
			"一经开封、检查或在法庭上被想起，保修即失效。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我跟箱子说它要去一个有爱的家。别让我变成骗子。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "就当这是一家不知道自己在关心你的公司寄来的爱心包裹。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我对箱子做了背景调查。它援引第五修正案保持沉默。我表示尊重。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "官方说法这是'物流异常'。非官方说法，嗨。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "小心轻放。戴手套。再准备个不在场证明。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我倒是想留下来喝杯茶，但喝茶会留下时间线。箱子不会。通常不会。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "如果公司有人打电话来，我是你表哥。搞物流的。已经死了。死了两次。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "条形码扫出来是'不关你事'。最先进的标签技术。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我不退款。卡车不倒退，我也不。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0",
			"把热度当成忠诚度计划。非常忠诚。对警用扫描仪忠诚。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"
		};

		private static readonly string[] PurchaseLines = new string[12]
		{
			"现金、箱子、别问。我最爱的三大食物组。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "很高兴和你做这笔无记录交易。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "收据？我还以为我们是朋友呢。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "恭喜，它归你了。对它之前的主人表示哀悼。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "别摇它。别扫它。别给它起名字。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "售出。如果它滴答响，那只是保修在实时过期。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "一个不公平的箱子，卖了个公平的价。宇宙会平衡的。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "你付了钱。我忘了。我们从未见过。完美的制度。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "在公司想起库存是怎么回事之前赶紧拿走。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我接受积分、沉默和合理推诿。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0",
			"在我变成传闻之前别开盖。之后它就是你的传闻了。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "交易完成。这个箱子现在是既存状况了。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"
		};

		private static readonly string[] ClearedStockLines = new string[12]
		{
			"这是最后一箱。卡车空了，我的不在场证明也空了。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我走了。还要的话，自己编第二辆卡车。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "库存清空。我从没来过。箱子有不同意见，但箱子会撒谎。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "全没了。下周同一时间，前提是没人发明文书工作。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "除了指纹什么都没留下，而我不搞指纹那套。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "空口袋，空卡车，满满的推诿能力。趁这一切还没变，我先走了。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "这就是我的全部目录。非常限量。限量到只有一件。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "打烊了。店就是我的外套。外套要走了。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "最后一箱走了。别跟着卡车。根本没有卡车。聊得愉快。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我弹尽粮绝了。连序列号都有别的地方要去。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0",
			"售罄。宇宙的箱子数量恢复正常了。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0", "我掉下来的就这些了。等重力再次发作时再见。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0"
		};

		private static int spawnedDay = int.MinValue;

		private static bool awaitingGreetingEnd;

		private static bool crateStocked;

		private static bool purchaseDialogPlayed;

		internal override string Id => "卡车上掉的货\0\0\0\0\0\0\0\0\0";

		internal override string DisplayName => "卡车上掉的货\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string Description => "一位特殊联系人每周会送来一箱'无人认领'的企业库存。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override int Cost => 2;

		internal override string? IconResource => "fell_off_the_truck.png";

		internal override void OnNewGame()
		{
			spawnedDay = int.MinValue;
			OnNewDay();
		}

		internal override void OnNewDay()
		{
			awaitingGreetingEnd = false;
			crateStocked = false;
			purchaseDialogPlayed = false;
			TryVisit();
		}

		internal static bool IsRobin(StoreClient? client)
		{
			if (client != null)
			{
				return client.identifier == "robin_banks";
			}
			return false;
		}

		internal static bool IsRobinName(string? name)
		{
			if (!(name == "罗宾·班克斯\0\0\0\0\0") && !(name == "robin_banks"))
			{
				return IsRobin(CurrentClient());
			}
			return true;
		}

		internal static bool ShouldStockAfterGreeting()
		{
			if (awaitingGreetingEnd)
			{
				return IsRobin(CurrentClient());
			}
			return false;
		}

		internal static StoreClient? CurrentClient()
		{
			PlayerStore instance = PlayerStore.Instance;
			StoreClientInstance obj = ((instance != null) ? instance.currentClientInstance : null) ?? ((instance != null) ? instance.lastVisitedClient : null);
			if (obj == null)
			{
				return null;
			}
			return obj.storeClient;
		}

		private static void TryVisit()
		{
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return;
			}
			int dayCounter = StoreStation.GetDayCounter();
			if (spawnedDay == dayCounter || (dayCounter - instance.startingDay) % 7 != 0)
			{
				return;
			}
			try
			{
				if (SpawnRobin())
				{
					spawnedDay = dayCounter;
				}
			}
			catch (Exception value)
			{
				Core.Log.Error($"Fell Off The Truck: spawn failed: {value}");
			}
		}

		private static bool SpawnRobin()
		{
			StoreClient val = StoreClientList.CreateStampSeller() ?? StoreClientList.CreateJunkerSellOnly() ?? StoreClientList.CreateShadyMerchant();
			if (val == null)
			{
				Core.Log.Warning("Fell Off The Truck: could not create seller.");
				return false;
			}
			StoreClientManager storeClientManager = PlayerStore.Instance.storeClientManager;
			if (storeClientManager == null)
			{
				Core.Log.Warning("Fell Off The Truck: no StoreClientManager.");
				return false;
			}
			StoreClient look = StoreClientList.CreateShadyMerchant();
			StoreClientFactionSetup.InitBlackMarket(val);
			Identify(val, look);
			val.CompleteClientCreation(true);
			Identify(val, look);
			ApplyDialogues(val);
			storeClientManager.AddClient(val);
			Core.Log.Msg($"Fell Off The Truck: queued Robin Banks ({val.spriteName}, {val.clientFaction}).");
			return true;
		}

		private static void Identify(StoreClient client, StoreClient? look)
		{
			if (look != null)
			{
				client.possibleSprites = look.possibleSprites;
				client.spriteName = look.spriteName;
				client.clientFaction = look.clientFaction;
			}
			client.isRegularSellDisable = false;
			client.useClientBudget = false;
			client.clientCash = 0;
			client.SetClientBudget(0, 0);
			client.OnArrival = null;
			client.clientBuyingIdList?.Clear();
			client.clientBuyingTagList?.Clear();
			client.clientItemFeatureBuying?.Clear();
			client.identifier = "robin_banks";
			client.displayName = "罗宾·班克斯\0\0\0\0\0";
		}

		internal static void StockCrate(StoreClient? client)
		{
			if (crateStocked)
			{
				return;
			}
			if (client == null)
			{
				client = CurrentClient();
			}
			if (client == null || !IsRobin(client))
			{
				return;
			}
			awaitingGreetingEnd = false;
			crateStocked = true;
			GameItem val = CreateRandomCrate();
			if (val == null)
			{
				crateStocked = false;
				awaitingGreetingEnd = true;
				Core.Log.Warning("Fell Off The Truck: crate factory returned null.");
				return;
			}
			val.EnableTag("TAG_NOT_PURCHASED", true);
			val.EnableTag("not_purchased", true);
			val.EnableTag("fell_off_no_stolen_discount", true);
			StolenHelper.InitStolenItem(val, 100);
			StolenHelper.SetHeat(val, 100);
			StripStolenDiscount(val);
			PlayerStore instance = PlayerStore.Instance;
			if (instance != null)
			{
				instance.AddDirectSellingItemToTable(val, false, true, false, 100);
			}
			StripStolenDiscount(val);
			Core.Log.Msg("Fell Off The Truck: stocked " + val.identifier + ".");
			try
			{
				NegociationUIManager instance2 = NegociationUIManager.Instance;
				if (instance2 != null)
				{
					instance2.DispatchUpdateForCurrentIntent();
				}
				NegociationUIManager instance3 = NegociationUIManager.Instance;
				if (instance3 != null)
				{
					instance3.RefreshUI();
				}
			}
			catch (Exception ex)
			{
				Core.Log.Warning("Fell Off The Truck: could not refresh trade UI (" + ex.Message + ").");
			}
		}

		private static GameItem CreateRandomCrate()
		{
			return Random.Range(0, 4) switch
			{
				0 => PreBuiltItemHelper.LootCrateSecurity(), 
				1 => PreBuiltItemHelper.LootCrateMedical(), 
				2 => PreBuiltItemHelper.LootCrateEngineering(), 
				_ => PreBuiltItemHelper.LootCrateService(), 
			};
		}

		private static void ApplyDialogues(StoreClient client)
		{
			Dialogue val = Line(Pick(Greetings));
			val.SetEndAction(DelegateSupport.ConvertDelegate<Action>((Delegate)new Action(OnGreetingDone)));
			client.mainDialogue = val;
			awaitingGreetingEnd = true;
			Dialogue val2 = PurchaseDialogue();
			Dialogue val3 = ClearedStockDialogue();
			client.acceptDealDialogue = val2;
			client.acceptDealDialogueLastLine = val2;
			client.acceptDealDialoguePlayed = false;
			client.allDoneDialogue = val3;
			client.repeatDialogue = val3;
		}

		private static void OnGreetingDone()
		{
			try
			{
				StockCrate(CurrentClient());
			}
			catch (Exception value)
			{
				Core.Log.Error($"Fell Off The Truck: stocking crate failed: {value}");
			}
		}

		internal static Dialogue PurchaseDialogue()
		{
			return Line(Pick(PurchaseLines));
		}

		internal static Dialogue ClearedStockDialogue()
		{
			return Line(Pick(ClearedStockLines));
		}

		internal static string RandomPurchaseLine()
		{
			return Pick(PurchaseLines);
		}

		internal static string RandomClearedStockLine()
		{
			return Pick(ClearedStockLines);
		}

		internal static bool TryRobinDialog(string? clientName, bool purchase, ref Dialogue result)
		{
			if (!IsRobinName(clientName))
			{
				return true;
			}
			result = (purchase ? PurchaseDialogue() : ClearedStockDialogue());
			return false;
		}

		internal static bool TryRobinDialog(bool purchase, ref Dialogue result)
		{
			if (!IsRobin(CurrentClient()))
			{
				return true;
			}
			result = (purchase ? PurchaseDialogue() : ClearedStockDialogue());
			return false;
		}

		internal static void OnCratePurchased()
		{
			StoreClient val = CurrentClient();
			if (val == null || purchaseDialogPlayed || val.identifier != "robin_banks")
			{
				return;
			}
			purchaseDialogPlayed = true;
			Dialogue nextDialogue = (val.mainDialogue = (val.repeatDialogue = (val.allDoneDialogue = val.allDoneDialogue ?? ClearedStockDialogue())));
			val.isBusinessComplete = true;
			if (IsDialoguePlaying())
			{
				return;
			}
			try
			{
				Dialogue val5 = PurchaseDialogue();
				if (!StillHasStock())
				{
					val5.SetNextDialogue(nextDialogue);
				}
				DialogUIManager instance = DialogUIManager.Instance;
				if (instance != null)
				{
					instance.StartDialogue(val5, false);
				}
			}
			catch (Exception ex)
			{
				Core.Log.Warning("Fell Off The Truck: purchase dialogue failed (" + ex.Message + ").");
			}
		}

		private static bool StillHasStock()
		{
			try
			{
				return StoreClientInstance.IsCurrentClientStillHaveThingToSell();
			}
			catch
			{
				return false;
			}
		}

		private static bool IsDialoguePlaying()
		{
			try
			{
				if (BargainUIManager.IsDialoguePlaying())
				{
					return true;
				}
			}
			catch
			{
			}
			try
			{
				DialogUIManager instance = DialogUIManager.Instance;
				return ((instance != null) ? instance.GetCurrentDialog() : null) != null && !instance.IsCurrentDialogEnd();
			}
			catch
			{
				return false;
			}
		}

		private static Dialogue Line(string text)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0005: Unknown result type (might be due to invalid IL or missing references)
			//IL_0013: Expected Obj, but got Unknown
			Dialogue val = new Dialogue();
			val.SetText("罗宾·班克斯\0\0\0\0\0", text);
			return val;
		}

		private static string Pick(string[] lines)
		{
			return lines[Random.Range(0, lines.Length)];
		}

		internal static bool BlocksStolenDiscount(GameItem? item)
		{
			if (item != null)
			{
				if (!item.IsTag("fell_off_no_stolen_discount"))
				{
					return IsRobin(CurrentClient());
				}
				return true;
			}
			return false;
		}

		internal static void StripStolenDiscount(GameItem? crate)
		{
			if (crate == null)
			{
				return;
			}
			ItemFeature[] array = new ItemFeature[5]
			{
				ItemFeatureList.StolenBonusBuying(),
				ItemFeatureList.StolenBonusBuyingHigh(),
				ItemFeatureList.StolenBonusBuyingMid(),
				ItemFeatureList.StolenBonusBuyingLow(),
				ItemFeatureList.DiscountSell50()
			};
			foreach (ItemFeature val in array)
			{
				if (val != null && val.identifier != null)
				{
					crate.RemoveItemFeatureByID(val.identifier);
				}
			}
			List<ItemFeature> itemFeatures = crate.itemFeatures;
			if (itemFeatures == null)
			{
				return;
			}
			for (int num = itemFeatures.Count - 1; num >= 0; num--)
			{
				ItemFeature val2 = itemFeatures[num];
				if (val2 != null && IsStolenDiscountFeature(val2))
				{
					crate.RemoveItemFeatureByID(val2.identifier);
				}
			}
		}

		internal static bool IsStolenDiscountFeature(ItemFeature feature)
		{
			string text = feature.identifier ?? "";
			string text2 = feature.category ?? "";
			string text3 = FeatureDisplay(feature);
			if (text3.Contains("finger", StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
			if (!text.Contains("stolen", StringComparison.OrdinalIgnoreCase) && !text2.Contains("stolen", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}
			if (text3.Length != 0 && !text.Contains("bonus", StringComparison.OrdinalIgnoreCase) && !text.Contains("discount", StringComparison.OrdinalIgnoreCase) && !text2.Contains("bonus", StringComparison.OrdinalIgnoreCase))
			{
				return text2.Contains("discount", StringComparison.OrdinalIgnoreCase);
			}
			return true;
		}

		private static string FeatureDisplay(ItemFeature feature)
		{
			try
			{
				return feature.GetPublicDisplay() ?? feature.GetActualDisplay() ?? "";
			}
			catch
			{
				return "";
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientListDict), "GetClientDisplayName")]
	internal static class RobinBanksDisplayNamePatch
	{
		private static bool Prefix(string __0, ref string __result)
		{
			if (__0 != "robin_banks")
			{
				return true;
			}
			__result = "罗宾·班克斯\0\0\0\0\0";
			return false;
		}
	}
	[HarmonyPatch(typeof(StoreClient), "SetBarterOffer")]
	internal static class RobinBanksNoBarterPatch
	{
		private static bool Prefix(StoreClient __instance)
		{
			return !FellOffTheTruckPerk.IsRobin(__instance);
		}
	}
	[HarmonyPatch(typeof(StoreClient), "IsClientSelling")]
	internal static class RobinBanksIsSellingPatch
	{
		private static void Postfix(StoreClient __instance, ref bool __result)
		{
			if (FellOffTheTruckPerk.IsRobin(__instance))
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(StoreClient), "IsClientBuying")]
	internal static class RobinBanksIsBuyingPatch
	{
		private static void Postfix(StoreClient __instance, ref bool __result)
		{
			if (FellOffTheTruckPerk.IsRobin(__instance))
			{
				__result = false;
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "GetCurrentClientIntent")]
	internal static class RobinBanksSellIntentPatch
	{
		private static void Postfix(ref ClientIntent __result)
		{
			if (FellOffTheTruckPerk.IsRobin(FellOffTheTruckPerk.CurrentClient()))
			{
				__result = (ClientIntent)2;
			}
		}
	}
	[HarmonyPatch(typeof(StolenHelper), "ApplyStolenDiscount")]
	internal static class RobinBanksNoStolenDiscountPatch
	{
		private static bool Prefix(GameItem item)
		{
			if (!FellOffTheTruckPerk.BlocksStolenDiscount(item))
			{
				return true;
			}
			FellOffTheTruckPerk.StripStolenDiscount(item);
			return false;
		}

		private static void Postfix(GameItem item)
		{
			if (FellOffTheTruckPerk.BlocksStolenDiscount(item))
			{
				FellOffTheTruckPerk.StripStolenDiscount(item);
			}
		}
	}
	[HarmonyPatch(typeof(GameItem), "AddItemFeature")]
	internal static class RobinBanksBlockStolenDiscountFeaturePatch
	{
		private static void Postfix(GameItem __instance, ItemFeature itemFeature)
		{
			if (FellOffTheTruckPerk.BlocksStolenDiscount(__instance) && itemFeature != null && FellOffTheTruckPerk.IsStolenDiscountFeature(itemFeature))
			{
				__instance.RemoveItemFeatureByID(itemFeature.identifier);
			}
		}
	}
	[HarmonyPatch(typeof(NegociationUIManager), "UpdateUI", new Type[] { typeof(GameItem) })]
	internal static class RobinBanksStripDiscountOnPricePatch
	{
		private static void Postfix(GameItem gameItem)
		{
			if (FellOffTheTruckPerk.BlocksStolenDiscount(gameItem))
			{
				FellOffTheTruckPerk.StripStolenDiscount(gameItem);
			}
		}
	}
	[HarmonyPatch(typeof(Dialogue), "ExecuteEndAction")]
	internal static class RobinBanksGreetingEndPatch
	{
		private static void Postfix()
		{
			if (!FellOffTheTruckPerk.ShouldStockAfterGreeting())
			{
				return;
			}
			try
			{
				FellOffTheTruckPerk.StockCrate(FellOffTheTruckPerk.CurrentClient());
			}
			catch (Exception value)
			{
				Core.Log.Error($"Fell Off The Truck: stocking after dialogue failed: {value}");
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientDialogList), "CreateAfterBuyingFromClientDialog")]
	internal static class RobinBanksAfterBuyingDialogPatch
	{
		private static bool Prefix(string clientName, ref Dialogue __result)
		{
			return FellOffTheTruckPerk.TryRobinDialog(clientName, purchase: true, ref __result);
		}
	}
	[HarmonyPatch(typeof(StoreClientDialogList), "CreateDefaultAllDoneDialog")]
	internal static class RobinBanksAllDoneDialogPatch
	{
		private static bool Prefix(string clientName, ref Dialogue __result)
		{
			return FellOffTheTruckPerk.TryRobinDialog(clientName, purchase: false, ref __result);
		}
	}
	[HarmonyPatch(typeof(StoreClientDialogList), "CreateAlreadyBoughtDialog")]
	internal static class RobinBanksAlreadyBoughtDialogPatch
	{
		private static bool Prefix(string clientName, ref Dialogue __result)
		{
			return FellOffTheTruckPerk.TryRobinDialog(clientName, purchase: false, ref __result);
		}
	}
	[HarmonyPatch(typeof(StoreClientDialogList), "GetRandomDialogAccepted")]
	internal static class RobinBanksRandomAcceptedDialogPatch
	{
		private static bool Prefix(ref Dialogue __result)
		{
			return FellOffTheTruckPerk.TryRobinDialog(purchase: true, ref __result);
		}
	}
	[HarmonyPatch(typeof(StoreClientDialogList), "GetRandomDialogDone")]
	internal static class RobinBanksRandomDoneDialogPatch
	{
		private static bool Prefix(ref Dialogue __result)
		{
			return FellOffTheTruckPerk.TryRobinDialog(purchase: false, ref __result);
		}
	}
	[HarmonyPatch(typeof(StoreClientDialogList), "GetRandomDialogAcceptedString")]
	internal static class RobinBanksRandomAcceptedStringPatch
	{
		private static void Postfix(ref string __result)
		{
			if (FellOffTheTruckPerk.IsRobin(FellOffTheTruckPerk.CurrentClient()))
			{
				__result = FellOffTheTruckPerk.RandomPurchaseLine();
			}
		}
	}
	[HarmonyPatch(typeof(StoreClientDialogList), "GetRandomDialogDoneString")]
	internal static class RobinBanksRandomDoneStringPatch
	{
		private static void Postfix(ref string __result)
		{
			if (FellOffTheTruckPerk.IsRobin(FellOffTheTruckPerk.CurrentClient()))
			{
				__result = FellOffTheTruckPerk.RandomClearedStockLine();
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "OnItemBought")]
	internal static class RobinBanksPlayerBoughtPatch
	{
		private static void Postfix(GameItem gameItem)
		{
			if (gameItem != null && FellOffTheTruckPerk.IsRobin(FellOffTheTruckPerk.CurrentClient()))
			{
				FellOffTheTruckPerk.OnCratePurchased();
			}
		}
	}
	[HarmonyPatch(typeof(StoreClient), "OnItemSold")]
	internal static class RobinBanksClientSoldPatch
	{
		private static void Postfix(StoreClient __instance)
		{
			if (FellOffTheTruckPerk.IsRobin(__instance))
			{
				FellOffTheTruckPerk.OnCratePurchased();
			}
		}
	}
	internal static class InventoryGrant
	{
		internal static bool TryGrant(string identifier)
		{
			EmporiumEntry instance = EmporiumEntry.Instance;
			if (Contains(instance.GetInvItems(), identifier))
			{
				return true;
			}
			if (TryAdd(MarkOwned(DirectoryMaster.Item(identifier, true))))
			{
				return Contains(instance.GetInvItems(), identifier);
			}
			return false;
		}

		internal static bool TryAdd(GameItem item)
		{
			MarkOwned(item);
			EmporiumEntry instance = EmporiumEntry.Instance;
			instance.backInvinvElement.TryFindOneValidInventorySlot(item, false);
			if (!((GameInventory)instance.backInvinvElement).UncheckedAccept(item))
			{
				return false;
			}
			instance.TransferOwnershipBackInv();
			instance.TransferOwnedItemBackToInv();
			return true;
		}

		internal static bool TryAddOutside(GameItem item)
		{
			GameGridInventory frontInvinvElement = EmporiumEntry.Instance.frontInvinvElement;
			frontInvinvElement.TryFindOneValidInventorySlot(item, false);
			return ((GameInventory)frontInvinvElement).UncheckedAccept(item);
		}

		internal static GameItem MarkOwned(GameItem item)
		{
			item.DisableTag("TAG_NOT_PURCHASED", true);
			item.DisableTag("not_purchased", true);
			return item;
		}

		private static bool Contains(List<GameItem> items, string identifier)
		{
			for (int i = 0; i < items.Count; i++)
			{
				if (items[i].identifier == identifier)
				{
					return true;
				}
			}
			return false;
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "EndDay")]
	internal static class NocturnalEndDayPatch
	{
		private static void Postfix()
		{
			NocturnalPerk.ApplyVisitLimit();
		}
	}
	[HarmonyPatch(typeof(StartingPerk), "IsShortSleeper")]
	internal static class NocturnalShortSleeperPatch
	{
		private static void Postfix(ref bool __result)
		{
			if (!__result && NocturnalPerk.IsActive())
			{
				__result = true;
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "HandleAfterhourDebuff")]
	internal static class NocturnalAfterhourDebuffPatch
	{
		private static bool Prefix()
		{
			return !NocturnalPerk.IsActive();
		}
	}
	[HarmonyPatch(typeof(StoreClientManager), "GenerateClient")]
	internal static class DeepSleepGenerateClientPatch
	{
		private static void Postfix(StoreClientManager __instance)
		{
			DeepSleepPerk.TryMissCustomer(__instance);
		}
	}
	internal sealed class NocturnalPerk : CustomStartingPerk
	{
		internal const string PerkId = "Nocturnal";

		internal override string Id => "夜猫子\0\0\0\0\0\0";

		internal override string DisplayName => "夜猫子\0\0\0\0\0\0";

		internal override string Description => "你可以熬夜到深夜，打烊后还能去2个地方。第二天也不会错过顾客。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override int Cost => 2;

		internal override string? IconResource => "nocturnal.png";

		internal override IReadOnlyList<string> IncompatibleIds => new <>z__ReadOnlySingleElementList<string>("深度睡眠\0\0\0\0\0");

		internal override void OnNewGame()
		{
			ApplyVisitLimit();
		}

		internal static bool IsActive()
		{
			return StartingPerk.IsPerkActive("夜猫子\0\0\0\0\0\0");
		}

		internal static void ApplyVisitLimit()
		{
			if (IsActive())
			{
				PlayerStore.Instance.visitLeftTonight = 2;
			}
		}
	}
	internal static class PerkIcon
	{
		internal static Sprite? LoadEmbeddedPng(string resourceSuffix)
		{
			//IL_009e: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a3: Unknown result type (might be due to invalid IL or missing references)
			//IL_00aa: Unknown result type (might be due to invalid IL or missing references)
			//IL_00b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_00bb: Expected Obj, but got Unknown
			//IL_010c: Unknown result type (might be due to invalid IL or missing references)
			//IL_011b: Unknown result type (might be due to invalid IL or missing references)
			Assembly assembly = typeof(Core).Assembly;
			string text = null;
			string[] manifestResourceNames = assembly.GetManifestResourceNames();
			foreach (string text2 in manifestResourceNames)
			{
				if (text2.EndsWith(resourceSuffix, StringComparison.OrdinalIgnoreCase))
				{
					text = text2;
					break;
				}
			}
			if (text == null)
			{
				Core.Log.Warning("Embedded icon '" + resourceSuffix + "' was not found.");
				return null;
			}
			using Stream stream = assembly.GetManifestResourceStream(text);
			if (stream == null)
			{
				return null;
			}
			byte[] array = new byte[stream.Length];
			if (stream.Read(array, 0, array.Length) <= 0)
			{
				return null;
			}
			Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false, false)
			{
				filterMode = (FilterMode)0,
				wrapMode = (TextureWrapMode)1,
				hideFlags = (HideFlags)61
			};
			if (!ImageConversion.LoadImage(val, Il2CppStructArray<byte>.op_Implicit(array)))
			{
				Object.Destroy((Object)(object)val);
				Core.Log.Warning("Failed to decode icon '" + resourceSuffix + "'.");
				return null;
			}
			Sprite val2 = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
			((Object)val2).hideFlags = (HideFlags)61;
			return val2;
		}
	}
	internal sealed class PissedOffSecurityPerk : CustomStartingPerk
	{
		internal const string PerkId = "PissedOffSecurity";

		internal const double SecurityCap = -45.0;

		internal override string Id => "惹毛安保\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string DisplayName => "惹毛安保\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string Description => "开局安保声望-45，黑市声望40。安保声望无法高于-45。开局拥有扩大的走私者海湾。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override int Cost => -3;

		internal override StartingPerkType Type => (StartingPerkType)1;

		internal override string? IconResource => "pissed_off_security.png";

		internal override void OnNewGame()
		{
			StoreReputation.ModSecReputation(-45);
			StoreReputation.ModBMReputation(40);
			InventoryGrant.TryGrant("smuggler_bay_mod");
		}

		internal static bool IsActive()
		{
			return StartingPerk.IsPerkActive("惹毛安保\0\0\0\0\0\0\0\0\0\0\0\0\0");
		}
	}
	[HarmonyPatch(typeof(StoreReputation), "ModReputation", new Type[] { typeof(double) })]
	internal static class PissedOffSecurityCapPatch
	{
		private static void Prefix(StoreReputation __instance, ref double modValue)
		{
			if (PissedOffSecurityPerk.IsActive() && !(__instance.factionId != StoreClient.FACTION_SECURITY))
			{
				double num = -45.0 - __instance.GetReputationExact();
				if (modValue > num)
				{
					modValue = num;
				}
			}
		}
	}
	[HarmonyPatch(typeof(BargainUIManager), "ComputeRepPer1000Credits")]
	internal static class PissedOffSecurityTradeRepPatch
	{
		private static void Postfix(string factionId, ref double __result)
		{
			if (PissedOffSecurityPerk.IsActive() && !(factionId != StoreClient.FACTION_SECURITY) && __result > 0.0)
			{
				__result = 0.0;
			}
		}
	}
	internal sealed class ScavengersReignPerk : CustomStartingPerk
	{
		private const string TokenId = "scav_token";

		internal override string Id => "拾荒者之王\0\0\0\0\0\0\0\0\0\0";

		internal override string DisplayName => "拾荒者之王\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string Description => "开局获得一个拾荒者代币。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string? IconResource => "scavengers_reign.png";

		internal override void OnNewGame()
		{
			InventoryGrant.TryGrant("scav_token");
		}
	}
	internal sealed class SnobPerk : CustomStartingPerk
	{
		internal const string PerkId = "Snob";

		private const int StartingUlRep = 20;

		private const int StartingLlRep = -20;

		internal const double UlGainMult = 1.2;

		internal const double LlGainMult = 0.8;

		internal override string Id => "势利眼\0";

		internal override string DisplayName => "势利眼\0";

		internal override string Description => "开局底层声望-20，底层声望获取减少20%。作为交换，开局高层声望+20，高层声望获取增加20%。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override int Cost => 0;

		internal override StartingPerkType Type => (StartingPerkType)2;

		internal override string? IconResource => "snob.png";

		internal override void OnNewGame()
		{
			StoreReputation.ModLLReputation(-20);
			StoreReputation.ModULReputation(20);
		}

		internal static bool IsActive()
		{
			return StartingPerk.IsPerkActive("势利眼\0");
		}
	}
	[HarmonyPatch(typeof(BargainUIManager), "ComputeRepPer1000Credits")]
	internal static class SnobTradeRepPatch
	{
		private static void Postfix(string factionId, ref double __result)
		{
			if (SnobPerk.IsActive() && !string.IsNullOrEmpty(factionId))
			{
				if (factionId == StoreClient.FACTION_UPPER)
				{
					__result *= 1.2;
				}
				else if (factionId == StoreClient.FACTION_LOWER)
				{
					__result *= 0.8;
				}
			}
		}
	}
	internal sealed class UglyShopPerk : CustomStartingPerk
	{
		internal const string PerkId = "UglyShop";

		private const int AttractivenessDelta = -150;

		internal override string Id => "丑陋店铺\0\0\0\0";

		internal override string DisplayName => "丑陋店铺\0\0\0\0\0";

		internal override string Description => "开局店铺吸引力-150\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override int Cost => -1;

		internal override StartingPerkType Type => (StartingPerkType)1;

		internal override string? IconResource => "ugly_shop.png";

		internal override void OnNewGame()
		{
			PlayerStore instance = PlayerStore.Instance;
			instance.baseStoreAttractiveness += -150;
		}

		internal static bool IsActive()
		{
			return StartingPerk.IsPerkActive("丑陋店铺\0\0\0\0");
		}
	}
	internal sealed class UltraColorfulCharacterPerk : CustomStartingPerk
	{
		internal const string PerkId = "UltraColorfulCharacter";

		internal const string ColorfulCharacterId = "colorful_character";

		internal const int ExtraSlots = 6;

		private static int extraApplied;

		internal override string Id => "奇葩人物\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string DisplayName => "奇葩人物\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string Description => "允许额外拥有6个初始天赋栏位。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override int Cost => 2;

		internal override int MaxSlot => 0;

		internal override StartingPerkType Type => (StartingPerkType)2;

		internal override string? IconResource => "ultra_colorful_character.png";

		internal override IReadOnlyList<string> IncompatibleIds => new <>z__ReadOnlySingleElementList<string>("colorful_character");

		internal override void OnNewGame()
		{
		}

		internal static bool IsSelectedIn(PerkUIController ui)
		{
			if ((Object)(object)((ui != null) ? ui.selectedPerks : null) == (Object)null)
			{
				return false;
			}
			foreach (StartingPerkElement componentsInChild in ui.selectedPerks.GetComponentsInChildren<StartingPerkElement>(true))
			{
				if ((Object)(object)componentsInChild != (Object)null && componentsInChild.id == "奇葩人物\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0")
				{
					return true;
				}
			}
			return false;
		}

		internal static void EnsureVanillaIncompatibility()
		{
			StartingPerk val = FindListedPerk("colorful_character");
			if (((val != null) ? val.incompatiblePerks : null) == null)
			{
				return;
			}
			for (int i = 0; i < val.incompatiblePerks.Count; i++)
			{
				if (val.incompatiblePerks[i] == "奇葩人物\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0")
				{
					return;
				}
			}
			val.incompatiblePerks.Add("奇葩人物\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0");
		}

		private static StartingPerk? FindListedPerk(string id)
		{
			List<StartingPerk> perks = StartingPerkList.Perks;
			if (perks == null)
			{
				return null;
			}
			for (int i = 0; i < perks.Count; i++)
			{
				StartingPerk val = perks[i];
				if (val != null && val.id == id)
				{
					return val;
				}
			}
			return null;
		}

		internal static void OnPickerOpened(PerkUIController ui)
		{
			extraApplied = 0;
			SyncExtraSlots(ui);
		}

		internal static void SyncExtraSlots(PerkUIController ui)
		{
			if (!((Object)(object)ui == (Object)null))
			{
				int num = (IsSelectedIn(ui) ? 6 : 0);
				int num2 = num - extraApplied;
				if (num2 != 0)
				{
					ui.maxPerkCount += num2;
					extraApplied = num;
				}
				RefreshSlotCounter(ui);
			}
		}

		private static void RefreshSlotCounter(PerkUIController ui)
		{
			TextMeshProUGUI slotCounter = ui.slotCounter;
			if ((Object)(object)slotCounter == (Object)null)
			{
				return;
			}
			string text = ((TMP_Text)slotCounter).text;
			if (!string.IsNullOrEmpty(text))
			{
				Match match = Regex.Match(text, "\\d+/\\d+");
				if (match.Success)
				{
					string text2 = $"{ui.currentPerkCount}/{ui.maxPerkCount}";
					string text3 = text.Substring(0, match.Index);
					string text4 = text;
					int num = match.Index + match.Length;
					((TMP_Text)slotCounter).text = text3 + text2 + text4.Substring(num, text4.Length - num);
				}
			}
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "OnChange")]
	internal static class UltraColorfulOnChangePatch
	{
		private static void Postfix(PerkUIController __instance)
		{
			UltraColorfulCharacterPerk.SyncExtraSlots(__instance);
		}
	}
	[HarmonyPatch(typeof(PerkUIController), "OpenUI", new Type[] { })]
	internal static class UltraColorfulOpenUiPatch
	{
		private static void Postfix(PerkUIController __instance)
		{
			UltraColorfulCharacterPerk.OnPickerOpened(__instance);
		}
	}
	internal sealed class WideConnectionsPerk : CustomStartingPerk
	{
		private const int DaysPerWeek = 7;

		private const int LookaheadDays = 5;

		private static MelonPreferences_Entry<string> lastGrant;

		internal override string Id => "人脉广泛\0\0\0\0\0\0\0\0\0\0\0";

		internal override string DisplayName => "人脉广泛\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string Description => "每周都会有联系人向你透露即将发生事件的情报。\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0\0";

		internal override string? IconResource => "wide_connections.png";

		internal override void OnNewGame()
		{
			GrantIfDue();
		}

		internal override void OnNewDay()
		{
			GrantIfDue();
		}

		internal static void EnsurePrefs()
		{
			lastGrant = MelonPreferences.CreateCategory("ExtraPerks_WideConnections").CreateEntry<string>("LastOutsideGrant", "", (string)null, (string)null, false, false, (ValueValidator)null, (string)null);
		}

		private void GrantIfDue()
		{
			PlayerStore instance = PlayerStore.Instance;
			int dayCounter = StoreStation.GetDayCounter();
			if ((dayCounter - instance.startingDay) % 7 == 0)
			{
				string text = $"{instance.runID}|{dayCounter}";
				if (!(lastGrant.Value == text))
				{
					StoreEvent? obj = FindUpcoming(dayCounter, out var eventDay) ?? StoreStation.Instance.storeEventManager.PickRandomEventByType((EventType)0) ?? EventListNormal.CreatePowerShortage();
					int num = ((eventDay > dayCounter) ? (eventDay - dayCounter) : 7);
					InventoryGrant.TryAddOutside(obj.CreateSlip(num));
					lastGrant.Value = text;
					MelonPreferences.Save();
				}
			}
		}

		private static StoreEvent? FindUpcoming(int today, out int eventDay)
		{
			//IL_003a: Unknown result type (might be due to invalid IL or missing references)
			//IL_0040: Invalid comparison between Unknown and I4
			//IL_0096: Unknown result type (might be due to invalid IL or missing references)
			//IL_009c: Invalid comparison between Unknown and I4
			eventDay = 0;
			StoreEventManager storeEventManager = StoreStation.Instance.storeEventManager;
			for (int i = today + 1; i <= today + 5; i++)
			{
				List<StoreEvent> activeEventOnDay = storeEventManager.GetActiveEventOnDay(i, true, false);
				for (int j = 0; j < activeEventOnDay.Count; j++)
				{
					StoreEvent val = activeEventOnDay[j];
					if (!val.isPermanent && (int)val.eventType != 2)
					{
						eventDay = i;
						return val;
					}
				}
			}
			List<StoreEvent> futurEvents = storeEventManager.futurEvents;
			List<int> futurEventsDay = storeEventManager.futurEventsDay;
			for (int k = 0; k < futurEvents.Count; k++)
			{
				if (futurEventsDay[k] > today)
				{
					StoreEvent val2 = futurEvents[k];
					if (!val2.isPermanent && (int)val2.eventType != 2)
					{
						eventDay = futurEventsDay[k];
						return val2;
					}
				}
			}
			return null;
		}
	}
}
[CompilerGenerated]
internal sealed class <>z__ReadOnlySingleElementList<T> : IEnumerable, ICollection, IList, IEnumerable<T>, IReadOnlyCollection<T>, IReadOnlyList<T>, ICollection<T>, IList<T>
{
	private sealed class Enumerator : IDisposable, IEnumerator, IEnumerator<T>
	{
		[CompilerGenerated]
		private readonly T _item;

		[CompilerGenerated]
		private bool _moveNextCalled;

		object IEnumerator.Current => _item;

		T IEnumerator<T>.Current => _item;

		public Enumerator(T item)
		{
			_item = item;
		}

		bool IEnumerator.MoveNext()
		{
			if (!_moveNextCalled)
			{
				return _moveNextCalled = true;
			}
			return false;
		}

		void IEnumerator.Reset()
		{
			_moveNextCalled = false;
		}

		void IDisposable.Dispose()
		{
		}
	}

	[CompilerGenerated]
	private readonly T _item;

	int ICollection.Count => 1;

	bool ICollection.IsSynchronized => false;

	object ICollection.SyncRoot => this;

	object? IList.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	bool IList.IsFixedSize => true;

	bool IList.IsReadOnly => true;

	int IReadOnlyCollection<T>.Count => 1;

	T IReadOnlyList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
	}

	int ICollection<T>.Count => 1;

	bool ICollection<T>.IsReadOnly => true;

	T IList<T>.this[int index]
	{
		get
		{
			if (index != 0)
			{
				throw new IndexOutOfRangeException();
			}
			return _item;
		}
		set
		{
			throw new NotSupportedException();
		}
	}

	public <>z__ReadOnlySingleElementList(T item)
	{
		_item = item;
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection.CopyTo(Array array, int index)
	{
		array.SetValue(_item, index);
	}

	int IList.Add(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Clear()
	{
		throw new NotSupportedException();
	}

	bool IList.Contains(object? value)
	{
		return EqualityComparer<T>.Default.Equals(_item, (T)value);
	}

	int IList.IndexOf(object? value)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, (T)value))
		{
			return -1;
		}
		return 0;
	}

	void IList.Insert(int index, object? value)
	{
		throw new NotSupportedException();
	}

	void IList.Remove(object? value)
	{
		throw new NotSupportedException();
	}

	void IList.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return new Enumerator(_item);
	}

	void ICollection<T>.Add(T item)
	{
		throw new NotSupportedException();
	}

	void ICollection<T>.Clear()
	{
		throw new NotSupportedException();
	}

	bool ICollection<T>.Contains(T item)
	{
		return EqualityComparer<T>.Default.Equals(_item, item);
	}

	void ICollection<T>.CopyTo(T[] array, int arrayIndex)
	{
		array[arrayIndex] = _item;
	}

	bool ICollection<T>.Remove(T item)
	{
		throw new NotSupportedException();
	}

	int IList<T>.IndexOf(T item)
	{
		if (!EqualityComparer<T>.Default.Equals(_item, item))
		{
			return -1;
		}
		return 0;
	}

	void IList<T>.Insert(int index, T item)
	{
		throw new NotSupportedException();
	}

	void IList<T>.RemoveAt(int index)
	{
		throw new NotSupportedException();
	}
}
