using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;
using XIAOWOTradeOwnerDeal;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: MelonInfo(typeof(Core), "XIAOWO Owner Deal", "1.0.0", "XIAOWO", null)]
[assembly: MelonGame(null, null)]
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
namespace XIAOWOTradeOwnerDeal
{
	public sealed class Core : MelonMod
	{
		private string _lastLocaleCode = string.Empty;

		private int _localeCheckCountdown;

		internal static Instance Log { get; private set; }

		public override void OnInitializeMelon()
		{
			Log = ((MelonBase)this).LoggerInstance;
			try
			{
				((MelonBase)this).HarmonyInstance.UnpatchSelf();
				((MelonBase)this).HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
				OwnerDealCompatibility.Install(((MelonBase)this).HarmonyInstance);
				OwnerDealActivation.IsActive();
				_lastLocaleCode = ModLocalization.CurrentLocaleCode;
				Log.Msg("XIAOWO Owner Deal standalone initialized. Locale: " + ((_lastLocaleCode.Length == 0) ? "default/en" : _lastLocaleCode));
			}
			catch (Exception ex)
			{
				Log.Error("Owner Deal initialization failed: " + ex);
			}
		}

		public override void OnUpdate()
		{
			if (_localeCheckCountdown-- <= 0)
			{
				_localeCheckCountdown = 30;
				string currentLocaleCode = ModLocalization.CurrentLocaleCode;
				if (!string.Equals(currentLocaleCode, _lastLocaleCode, StringComparison.OrdinalIgnoreCase))
				{
					_lastLocaleCode = currentLocaleCode;
					PlayerStore instance = PlayerStore.instance;
					OwnerDeal.RefreshLocalizedDialogue((instance != null) ? instance.storeClientManager : null);
					Log.Msg("Owner Deal language changed to: " + ((currentLocaleCode.Length == 0) ? "default/en" : currentLocaleCode));
				}
			}
		}

		public override void OnDeinitializeMelon()
		{
			OwnerDeal.OnGameLoading();
		}
	}
	internal static class OwnerDealActivation
	{
		private static MelonPreferences_Category _category;

		private static MelonPreferences_Entry<bool> _enabled;

		private static MelonPreferences_Entry<int> _discountPercent;

		private static MelonPreferences_Entry<bool> _enableWeeklyVisitors;

		private static MelonPreferences_Entry<bool> _enableCashBuyout;

		private static MelonPreferences_Entry<int> _stockMin;

		private static MelonPreferences_Entry<int> _stockMax;

		private static MelonPreferences_Entry<int> _restrictedNeuralChance;

		private static MelonPreferences_Entry<int> _unrestrictedNeuralChance;

		internal static int DiscountPercent
		{
			get
			{
				EnsureLoaded();
				return _discountPercent.Value;
			}
		}

		internal static bool WeeklyVisitorsEnabled
		{
			get
			{
				EnsureLoaded();
				return _enableWeeklyVisitors.Value;
			}
		}

		internal static bool CashBuyoutEnabled
		{
			get
			{
				EnsureLoaded();
				return _enableCashBuyout.Value;
			}
		}

		internal static int StockMin
		{
			get
			{
				EnsureLoaded();
				return _stockMin.Value;
			}
		}

		internal static int StockMax
		{
			get
			{
				EnsureLoaded();
				return _stockMax.Value;
			}
		}

		internal static int RestrictedNeuralChance
		{
			get
			{
				EnsureLoaded();
				return _restrictedNeuralChance.Value;
			}
		}

		internal static int UnrestrictedNeuralChance
		{
			get
			{
				EnsureLoaded();
				return _unrestrictedNeuralChance.Value;
			}
		}

		internal static bool IsActive()
		{
			EnsureLoaded();
			return _enabled.Value;
		}

		private static void EnsureLoaded()
		{
			if (_category == null)
			{
				_category = MelonPreferences.CreateCategory("XIAOWOTradeOwnerDeal", "XIAOWO Owner Deal");
				_enabled = _category.CreateEntry<bool>("Enabled", true, "总开关", "Master switch for the whole mod.", false, false, (ValueValidator)null, (string)null);
				_discountPercent = _category.CreateEntry<int>("DiscountPercent", 15, "房东折扣百分比", "Landlord wholesale discount percent.", false, false, (ValueValidator)null, (string)null);
				_enableWeeklyVisitors = _category.CreateEntry<bool>("WeeklyVisitors", true, "原租金日访客", "Spawn landlord + merchant on each former rent day.", false, false, (ValueValidator)null, (string)null);
				_enableCashBuyout = _category.CreateEntry<bool>("CashBuyout", true, "允许现金买断", "Allow the cash buyout dialogue option.", false, false, (ValueValidator)null, (string)null);
				_stockMin = _category.CreateEntry<int>("StockMin", 3, "房东最少上货数", "Minimum items the landlord stocks per visit.", false, false, (ValueValidator)null, (string)null);
				_stockMax = _category.CreateEntry<int>("StockMax", 6, "房东最多上货数", "Maximum items the landlord stocks per visit.", false, false, (ValueValidator)null, (string)null);
				_restrictedNeuralChance = _category.CreateEntry<int>("RestrictedNeuralChancePercent", 5, "受限神经核心概率(%)", "Chance percent to include a restricted neural core.", false, false, (ValueValidator)null, (string)null);
				_unrestrictedNeuralChance = _category.CreateEntry<int>("UnrestrictedNeuralChancePercent", 1, "非受限神经核心概率(%)", "Chance percent to include an unrestricted neural core.", false, false, (ValueValidator)null, (string)null);
			}
		}
	}
	internal static class OwnerDealSchedule
	{
		internal static int CurrentDay => Math.Max(0, StoreStation.GetDayCounter());

		internal static int CurrentWeek => Math.Max(0, (CurrentDay - 1) / 7);
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
			return IsChinese(localeCode) ? chinese : english;
		}
	}
	internal static class OwnerDealRules
	{
		internal const int VisitIntervalDays = 7;

		internal const int MerchantCompleted = 1;

		internal const int LandlordCompleted = 2;

		internal const int AllVisitorsCompleted = 3;

		internal static bool CanOfferCashBuyout(bool active, bool propertyPaid, bool payingRent, int cash, int price)
		{
			return ((active && !propertyPaid) & payingRent) && price > 0 && cash >= price;
		}

		internal static bool IsFormerRentDay(int day)
		{
			return day > 0 && day % 7 == 0;
		}

		internal static int EncodeVisitState(int day, int mask)
		{
			return (Math.Max(0, day) << 2) | (mask & 3);
		}

		internal static int DecodeVisitState(int encoded, int day)
		{
			if (encoded >> 2 != Math.Max(0, day))
			{
				return 0;
			}
			return encoded & 3;
		}

		internal static bool NeedsVisitor(int mask, int bit, bool present)
		{
			return (mask & bit) == 0 && !present;
		}

		internal static OwnerStockRoll Roll(Random random)
		{
			int num = OwnerDealActivation.StockMin;
			int num2 = OwnerDealActivation.StockMax;
			if (num < 0)
			{
				num = 0;
			}
			if (num2 < num)
			{
				num2 = num;
			}
			int count = ((num2 > num) ? random.Next(num, num2 + 1) : num);
			int num3 = Clamp01to100(OwnerDealActivation.RestrictedNeuralChance);
			int num4 = Clamp01to100(OwnerDealActivation.UnrestrictedNeuralChance);
			bool restrictedNeural = random.Next(100) < num3;
			bool unrestrictedNeural = random.Next(100) < num4;
			return new OwnerStockRoll(count, restrictedNeural, unrestrictedNeural);
		}

		private static int Clamp01to100(int value)
		{
			if (value < 0)
			{
				return 0;
			}
			if (value > 100)
			{
				return 100;
			}
			return value;
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
	internal readonly struct OwnerStockRoll(int count, bool restrictedNeural, bool unrestrictedNeural)
	{
		internal int Count { get; } = count;

		internal bool RestrictedNeural { get; } = restrictedNeural;

		internal bool UnrestrictedNeural { get; } = unrestrictedNeural;
	}
	internal static class OwnerDeal
	{
		internal const string ClientId = "landlord";

		internal const string MerchantClientId = "merchant";

		internal const string RestrictedNeuralId = "system_capped_neural_core";

		internal const string UnrestrictedNeuralId = "system_uncapped_neural_core";

		internal const string EvidenceBoxId = "evidence_box";

		internal const int DiscountPercent = 15;

		private const string VisitStatePrefix = "XIAOWOTradePerks.OwnerDeal.PairV3.";

		private static readonly List<string> RegularPool = new List<string>();

		private static string _lastScheduledKey = string.Empty;

		private static bool _poolBuilt;

		private static long _merchantClientPointer;

		private static long _ownerClientPointer;

		private static readonly HashSet<long> CashBuyoutChoiceDialogues = new HashSet<long>();

		private static readonly List<Action> CashBuyoutActions = new List<Action>();

		internal static bool IsEnabledAndOwned(PlayerStore store)
		{
			return store != null && store.IsPropertyPaid && OwnerDealActivation.IsActive();
		}

		internal static bool ShouldSuppressEnding(PlayerStore store, string ending)
		{
			return IsEnabledAndOwned(store) && string.Equals(ending, "buyout", StringComparison.Ordinal);
		}

		internal static bool CanOfferCashBuyout(PlayerStore store)
		{
			if (store == null)
			{
				return false;
			}
			if (!OwnerDealActivation.CashBuyoutEnabled)
			{
				return false;
			}
			return OwnerDealRules.CanOfferCashBuyout(OwnerDealActivation.IsActive(), store.IsPropertyPaid, store.IsPayingRent(), store.GetCash(), store.GetStoreBuyPrice(false));
		}

		internal static void ScheduleWeeklyVisitors(StoreClientManager manager)
		{
			if (manager == null || !OwnerDealActivation.WeeklyVisitorsEnabled)
			{
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (!IsEnabledAndOwned(instance) || StoreStation.instance == null)
			{
				return;
			}
			int currentDay = OwnerDealSchedule.CurrentDay;
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
			bool flag5 = ((num & 1) != 0) | flag;
			bool flag6 = ((num & 2) != 0) | flag2;
			if (!flag5 || !flag6)
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
				log3.Msg("Owner deal weekly visitors scheduled on former rent day " + currentDay + " (merchant=" + flag3 + ", landlord=" + flag4 + ", mask=" + num + ").");
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
				client.SetWholesale(OwnerDealActivation.DiscountPercent);
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
			if (ownerStockRoll.UnrestrictedNeural)
			{
				list.Add("system_uncapped_neural_core");
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
			if (item == null || item.unitValue <= 0)
			{
				return false;
			}
			if (!item.IsTag("MODULE_TAG") && !item.IsTag("CONTRABAND_ITEM_TAG"))
			{
				return item.IsTag("LUXURY_ITEM");
			}
			return true;
		}

		internal static bool IsCurrentOwnerVisit(PlayerStore store)
		{
			//IL_0026: Unknown result type (might be due to invalid IL or missing references)
			//IL_002c: Expected Obj, but got Unknown
			object obj = null;
			if (store != null)
			{
				StoreClientInstance currentClientInstance = store.currentClientInstance;
				if (currentClientInstance != null)
				{
					obj = currentClientInstance.storeClient;
				}
			}
			StoreClient val = (StoreClient)obj;
			if (IsEnabledAndOwned(store) && val != null && _ownerClientPointer != 0)
			{
				return ((Il2CppObjectBase)val).Pointer.ToInt64() == _ownerClientPointer;
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
			Action action = () =>
			{
				Action val3 = nativeStartAction;
				if (val3 != null)
				{
					val3.Invoke();
				}
				TryAddCashBuyoutChoice(client);
			};
			Action val2 = DelegateSupport.ConvertDelegate<Action>((Delegate)action);
			CashBuyoutActions.Add(val2);
			mainDialogue.SetStartAction(val2);
		}

		internal static bool TryAddCashBuyoutChoice(StoreClient client)
		{
			//IL_007a: Unknown result type (might be due to invalid IL or missing references)
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
			val2.NextDialogue().SetText(client.displayName, ModLocalization.Select("成交。从现在起，这间店归你了。", "Deal. From now on, the shop is yours."));
			Action action = () =>
			{
				CompleteCashBuyout(store);
			};
			Action val3 = DelegateSupport.ConvertDelegate<Action>((Delegate)action);
			CashBuyoutActions.Add(val3);
			OwnerDealCompatibility.AddChoice(val, DirectoryMaster.Item("$", true), "xiaowo cash buyout choice", string.Empty, val2, val3);
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
			if (!IsEnabledAndOwned(store) || !OwnerDealRules.IsFormerRentDay(OwnerDealSchedule.CurrentDay))
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
			int currentDay = OwnerDealSchedule.CurrentDay;
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
			IReadOnlyList<string> readOnlyList = BuildStockPlan(store.runID, OwnerDealSchedule.CurrentDay);
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
			return string.Equals(id, "evidence_box", StringComparison.Ordinal) ? PreBuiltItemHelper.LootCrateEvidence() : DirectoryMaster.Item(id, false);
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
			//IL_0040: Unknown result type (might be due to invalid IL or missing references)
			//IL_0046: Expected Obj, but got Unknown
			if (manager == null)
			{
				return;
			}
			PlayerStore instance = PlayerStore.instance;
			object obj = null;
			if (instance != null)
			{
				StoreClientInstance currentClientInstance = instance.currentClientInstance;
				if (currentClientInstance != null)
				{
					obj = currentClientInstance.storeClient;
				}
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
				StoreClient val = clients[i];
				if (val != null && string.Equals(val.identifier, id, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsClientPresent(PlayerStore store, StoreClientManager manager, string id)
		{
			string a = null;
			if (store != null)
			{
				StoreClientInstance currentClientInstance = store.currentClientInstance;
				if (((currentClientInstance != null) ? currentClientInstance.storeClient : null) != null)
				{
					a = currentClientInstance.storeClient.identifier;
				}
			}
			if (!string.Equals(a, id, StringComparison.Ordinal))
			{
				return ContainsClient((manager != null) ? manager.clientStack : null, id);
			}
			return true;
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
			MethodInfo methodInfo = (from c in typeof(Dialogue).GetMethods(BindingFlags.Instance | BindingFlags.Public)
				where string.Equals(c.Name, "AddChoice", StringComparison.Ordinal)
				orderby c.GetParameters().Length
				select c).FirstOrDefault((MethodInfo c) =>
			{
				int num = c.GetParameters().Length;
				return (uint)(num - 5) <= 1u;
			});
			if (methodInfo == null)
			{
				throw new MissingMethodException(typeof(Dialogue).FullName, "AddChoice");
			}
			object[] parameters = ((methodInfo.GetParameters().Length == 5) ? new object[5] { choiceItem, name, description, nextDialogue, action } : new object[6] { choiceItem, name, description, nextDialogue, action, null });
			object obj = methodInfo.Invoke(dialogue, parameters);
			return (Dialogue)((obj is Dialogue) ? obj : null);
		}

		internal static void Install(Harmony harmony)
		{
			//IL_0049: Unknown result type (might be due to invalid IL or missing references)
			//IL_0056: Expected Obj, but got Unknown
			//IL_01a8: Unknown result type (might be due to invalid IL or missing references)
			//IL_01b6: Expected Obj, but got Unknown
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
					log2.Msg("Owner deal: native CanBuyOutStore is absent; using custom dialogue choice.");
				}
			}
			_landlordStockTarget = (from t in typeof(StoreClientUniqueList).Assembly.GetTypes()
				from m in t.GetMethods(BindingFlags.DeclaredOnly | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				where m.Name.StartsWith("_LandlordWholesale_b__", StringComparison.Ordinal) && m.ReturnType == typeof(void) && m.GetParameters().Length == 0
				orderby m.Name
				select m).FirstOrDefault();
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
	[HarmonyPatch]
	internal static class OwnerDealLandlordDialoguePatch
	{
		private static MethodBase TargetMethod()
		{
			return typeof(StoreClientList).GetMethod("CreateLandlordClient", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
		}

		private static void Postfix(StoreClient __result)
		{
			OwnerDeal.AttachCashBuyoutChoice(__result);
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "LoadGame")]
	internal static class OwnerDealLoadResetPatch
	{
		private static void Prefix()
		{
			OwnerDeal.OnGameLoading();
		}

		private static void Postfix(PlayerStore __instance)
		{
			OwnerDeal.ScheduleWeeklyVisitors((__instance != null) ? __instance.storeClientManager : null);
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
	[HarmonyPatch(typeof(PlayerStore), "DismissCurrentClient")]
	internal static class OwnerDealVisitorCompletionPatch
	{
		private static void Prefix(PlayerStore __instance, out int __state)
		{
			__state = OwnerDeal.CaptureCurrentVisitor(__instance);
		}

		private static void Postfix(int __state)
		{
			OwnerDeal.MarkVisitorCompleted(__state);
		}
	}
}
