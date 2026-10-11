using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using NewbieGift;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(NewbieGiftMod), "良心商人", "1.7.0", "核桃", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: AssemblyFileVersion("1.7.0.0")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("1.7.0.0")]
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
namespace NewbieGift
{
	internal static class AntiCheat
	{
		private const float TickInterval = 1f;

		private static bool _active;

		private static float _nextTickTime;

		private static readonly string[] SystemBonusBlacklist = new string[13]
		{
			"varietyBuyerBonus", "contrabandCriticalMarkUp", "contrabandHighMarkUp", "contrabandMidMarkUp", "contrabandLowMarkUp", "DiscountedCriticalContraband", "DiscountedHighContraband", "DiscountedMidContraband", "DiscountedLowContraband", "StolenBonusBuying",
			"StolenBonusBuyingHigh", "StolenBonusBuyingMid", "StolenBonusBuyingLow"
		};

		private static string StripValueSuffix(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return s;
			}
			int num = s.LastIndexOf(" (", StringComparison.Ordinal);
			if (num < 0)
			{
				return s;
			}
			string text = s.Substring(num);
			if (text.EndsWith("%)") || text.EndsWith("％）"))
			{
				return s.Substring(0, num);
			}
			return s;
		}

		private static bool IsSystemBonus(ItemFeature f)
		{
			try
			{
				string identifier = f.identifier;
				if (string.IsNullOrEmpty(identifier))
				{
					return false;
				}
				for (int i = 0; i < SystemBonusBlacklist.Length; i++)
				{
					if (string.Equals(SystemBonusBlacklist[i], identifier, StringComparison.OrdinalIgnoreCase))
					{
						return true;
					}
				}
				if (identifier.IndexOf("MarkUp", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
				if (identifier.IndexOf("BonusBuying", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
				if (identifier.IndexOf("BuyerBonus", StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			catch
			{
			}
			return false;
		}

		public static void Begin(string clientId)
		{
			if (!_active)
			{
				_active = true;
				_nextTickTime = 0f;
				ModLog.Info("[反作弊] 开始监视（" + clientId + "）");
			}
		}

		public static void End()
		{
			if (_active)
			{
				_active = false;
				ModLog.Info("[反作弊] 停止监视");
			}
		}

		public static void Tick()
		{
			if (!_active)
			{
				return;
			}
			try
			{
				float unscaledTime = Time.unscaledTime;
				if (!(unscaledTime < _nextTickTime))
				{
					_nextTickTime = unscaledTime + 1f;
					ScanOnce();
				}
			}
			catch (Exception ex)
			{
				ModLog.Warn("[反作弊] Tick 异常: " + ex.Message);
			}
		}

		private static void RecursiveScan(GameItem item, HashSet<IntPtr> visited, ref int exposed)
		{
			if (item == null)
			{
				return;
			}
			try
			{
				if (!visited.Add(((Il2CppObjectBase)item).Pointer))
				{
					return;
				}
			}
			catch
			{
				return;
			}
			TryExposeFeatures(item, ref exposed);
			List<GameItem> list = TryGetContainerContents(item);
			if (list != null)
			{
				for (int i = 0; i < list.Count; i++)
				{
					RecursiveScan(list[i], visited, ref exposed);
				}
			}
		}

		private static void ScanOnce()
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Expected Obj, but got Unknown
			List<GameItem> countersItems;
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)instance == (Object)null)
				{
					return;
				}
				countersItems = instance.GetCountersItems();
			}
			catch (Exception ex)
			{
				ModLog.Debug("[反作弊] 取柜台物品失败: " + ex.Message);
				return;
			}
			if (countersItems != null && countersItems.Count != 0)
			{
				ModLog.Debug($"[反作弊] 本轮扫描 {countersItems.Count} 件柜台物品");
				int exposed = 0;
				HashSet<IntPtr> visited = new HashSet<IntPtr>();
				for (int i = 0; i < countersItems.Count; i++)
				{
					RecursiveScan(countersItems[i], visited, ref exposed);
				}
				if (exposed > 0)
				{
					Notify(Lang.T($"哎哟，这小手段我见多了。（识破 {exposed} 件）", $"Nice try, pal. (Busted {exposed} item(s))"));
					ModLog.Info($"[反作弊] 本轮识破 {exposed} 件");
				}
			}
		}

		private static void TryExposeFeatures(GameItem item, ref int exposed)
		{
			List<ItemFeature> itemFeatures;
			try
			{
				itemFeatures = item.itemFeatures;
			}
			catch
			{
				return;
			}
			if (itemFeatures == null)
			{
				return;
			}
			for (int i = 0; i < itemFeatures.Count; i++)
			{
				ItemFeature val = itemFeatures[i];
				if (val == null)
				{
					continue;
				}
				bool flag = false;
				string value = "";
				try
				{
					if (!val.CanExposeFeature() || val.IsExposed() || !val.isExposable || IsSystemBonus(val))
					{
						continue;
					}
					if (!val.useCondition)
					{
						int currentValueMod = val.GetCurrentValueMod();
						int actualValueModifier = val.GetActualValueModifier();
						if (currentValueMod > actualValueModifier)
						{
							flag = true;
							value = $"value {currentValueMod}%→{actualValueModifier}%";
						}
					}
					else
					{
						string text = null;
						string text2 = null;
						try
						{
							text = val.GetPublicDisplay();
						}
						catch
						{
						}
						try
						{
							text2 = val.GetActualDisplay();
						}
						catch
						{
						}
						if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text2) && text != text2 && StripValueSuffix(text) != text2)
						{
							flag = true;
							value = $"display '{text}'→'{text2}'";
						}
					}
					goto IL_0177;
				}
				catch
				{
				}
				continue;
				IL_0177:
				if (flag)
				{
					try
					{
						val.ExposeFeature(false, false);
						exposed++;
						ModLog.Info($"[反作弊] 识破: item={SafeId(item)} feature={SafeFeatureId(val)} {value}");
					}
					catch (Exception ex)
					{
						ModLog.Warn("[反作弊] ExposeFeature 失败: " + ex.Message);
					}
				}
			}
		}

		private static string SafeId(GameItem it)
		{
			try
			{
				return ((it != null) ? it.identifier : null) ?? "?";
			}
			catch
			{
				return "?";
			}
		}

		private static string SafeFeatureId(ItemFeature f)
		{
			try
			{
				return ((f != null) ? f.identifier : null) ?? "?";
			}
			catch
			{
				return "?";
			}
		}

		private static void Notify(string text)
		{
			try
			{
				StoreUIManager instance = StoreUIManager.Instance;
				if (instance != null)
				{
					instance.Notify(text, "default");
				}
			}
			catch
			{
			}
		}

		private static List<GameItem> TryGetContainerContents(GameItem item)
		{
			try
			{
				PixelWindow contentWindow = item.contentWindow;
				if (contentWindow == null)
				{
					return null;
				}
				PixelElement childElement = contentWindow.childElement;
				if (childElement == null)
				{
					return null;
				}
				GameGridInventory val = ((Il2CppObjectBase)childElement).TryCast<GameGridInventory>();
				if (val == null)
				{
					return null;
				}
				List<GameItem> items = val.items;
				if (items == null || items.Count == 0)
				{
					return null;
				}
				List<GameItem> list = new List<GameItem>();
				for (int i = 0; i < items.Count; i++)
				{
					if (items[i] != null)
					{
						list.Add(items[i]);
					}
				}
				return list;
			}
			catch (Exception ex)
			{
				ModLog.Debug("[反作弊] 容器内容读取失败: " + ex.Message);
				return null;
			}
		}
	}
	[HarmonyPatch(typeof(StoreClient), "IsClientBuyingThisItem")]
	internal static class Patch_IsClientBuyingThisItem
	{
		private static float _nextLogTime;

		private static void Postfix(StoreClient __instance, GameItem gameItem, ref bool __result)
		{
			if (!__result || __instance == null || gameItem == null || !Courier.IsCourier(__instance.identifier))
			{
				return;
			}
			try
			{
				if (Courier.IsBlacklisted(gameItem.identifier))
				{
					__result = false;
					float unscaledTime = Time.unscaledTime;
					if (unscaledTime >= _nextLogTime)
					{
						_nextLogTime = unscaledTime + 3f;
						ModLog.Info("[黑名单] 拒收: " + gameItem.identifier);
					}
				}
			}
			catch (Exception ex)
			{
				ModLog.Warn("IsClientBuyingThisItem.Post: " + ex.Message);
			}
		}
	}
	internal static class CatalogIds
	{
		public static readonly string[] All = new string[584]
		{
			"advanced_flux_agent", "aquascan", "aug_id_guide", "be_47_love_cup", "be_brewing_manual", "be_clear_water_tank", "be_compact_smuggler_bay", "be_contraband_perfume", "be_low_blindbox", "be_mid_blindbox",
			"be_multi_storage", "be_ninglu_filter", "be_premium_blindbox", "be_qingyun_filter", "be_wine_blindbox", "be_xuxu_sweet_cup", "be_xuxu_warm_cup", "be_yingfei_filter", "black_lamp", "blank_keycard",
			"blood_bag", "blue_blood_bag", "c4", "c4_set", "card_chem", "card_mining", "card_pharma", "card_rev", "cassette1", "cassette1d",
			"cassette1l", "cassette2", "cassette2d", "cassette2l", "cassette3", "cassette3d", "cassette3l", "cassette4", "cassette4d", "cassette4l",
			"cassette5", "cassette5d", "cassette5l", "cassette6", "cassette6d", "cassette6l", "cassette7", "cassette7d", "cassette7l", "cassette_player",
			"cat_bar", "cert1", "cigarette_color", "cmd_keycard", "common_chemical", "common_electronic", "common_medical", "crowbar", "cup_noodle", "desequencer",
			"dossier", "drain", "dream_dust", "empty_nuclear_waste_barrel", "energy_credit", "energy_credit_breeder", "energy_credit_ext", "energy_drink", "eng_keycard", "exchange_directive",
			"fanny_pack", "fat_meat", "faucet", "fce_chef_cookbook", "fce_cooking_wine", "fce_cuisine_certificate", "fce_cuisine_manual", "fce_dark_soy", "fce_light_soy", "fce_sugar",
			"fce_vinegar", "flashlight", "flux_agent", "furnace", "galaxy_blend", "gene_scanner", "gf_fish_mince_bait", "gf_glass_bait", "gf_rat_dung_bait", "gf_shiny_bait",
			"glass_shard", "glass_shard_shiv", "hand", "handnote", "hatchet", "joe_card", "kitchen_cleaver", "labeler", "labeler_blue", "li_eat_snackbar",
			"magnifier", "mentor_contract", "mirage_projector", "moisture_farm", "mouse_trap", "neuroactive_perfume", "newspaper", "nightmare_dust", "node_medium", "node_small",
			"nuclear_waste", "nudka", "nutrient_tablet", "packet_red_cigarette", "pack_condom", "paper_towel", "pheromone_perfume", "pipe_weapon", "plasma_fuel", "plastic_bag",
			"portable_water_purifier", "postit", "powerblock", "processed_cheese", "processed_juice", "processed_meat", "processed_milk", "rare_electronic", "raw_meat", "recharger",
			"restricted_chemical", "restricted_medical", "rev_codeword", "rubbing_alcohol", "salve", "satchel", "scav_token", "sci_keycard", "screwdriver", "security_alarm",
			"sec_keycard", "ser_keycard", "shampoo", "skincare_cream", "small_raw_meat", "smelling_salt", "soda_red", "stun_baton", "sup_keycard", "tackle_shop_processing_manual",
			"tackle_shop_willy_card", "tazer", "toilet_paper", "toothpaste", "turbo_booster", "turbo_booster_adv", "tutorial_page", "uv_filter", "wanted_paper", "water_filter",
			"water_filter_adv", "water_jug", "water_pitcher", "water_purifier", "water_tablet", "water_test_strip", "welder", "wire", "wire_cutter", "zerochew",
			"printer_plastic", "system_capped_neural_core", "system_module_corrupt", "system_module_eco", "system_module_efficiancy", "system_module_fineness", "system_module_overclock", "system_module_performance", "system_module_quality", "system_module_ruined",
			"system_module_streamlining", "vacuum_robot", "alarm_module_transmitter", "blank_module", "chem_module", "crypto_module_cmd", "crypto_module_eng", "crypto_module_med", "crypto_module_sec", "crypto_module_ser",
			"crypto_module_sup", "custom_storage_box", "furnace_module_blast", "furnace_module_junk", "gene_edit_probe", "gene_edit_probe_box", "keycard_pack", "module_extractor", "module_extractor_advanced", "void_bead_storage",
			"wage_ai_generator", "wage_ai_module", "wage_girl", "wage_gu_machine", "wage_protector_core", "xiaowo_auction_bearing_set", "xiaowo_auction_black_decoder", "xiaowo_auction_black_diamond", "xiaowo_auction_blood_filter", "xiaowo_auction_calibrator",
			"xiaowo_auction_century_vintage", "xiaowo_auction_cipher_ledger", "xiaowo_auction_coilgun", "xiaowo_auction_container", "xiaowo_auction_control_core", "xiaowo_auction_crystal_wine_set", "xiaowo_auction_fabricator_core", "xiaowo_auction_forged_pass", "xiaowo_auction_ghost_identity", "xiaowo_auction_hemostatic_foam",
			"xiaowo_auction_master_key", "xiaowo_auction_microreactor", "xiaowo_auction_micro_engraving", "xiaowo_auction_nano_suture", "xiaowo_auction_neural_repair", "xiaowo_auction_orbital_watch", "xiaowo_auction_organ_case", "xiaowo_auction_organ_matrix", "xiaowo_auction_pocket_watch", "xiaowo_auction_regen_core",
			"xiaowo_auction_silent_bolt", "xiaowo_auction_supercoil", "xiaowo_auction_zero_g_perfume", "xiaowo_auction_zero_point", "xiaowo_kitchen_beverage_machine", "xiaowo_kitchen_black_market_banquet", "xiaowo_kitchen_cheese_noodles", "xiaowo_kitchen_cheese_patty", "xiaowo_kitchen_clear_fruit_drink", "xiaowo_kitchen_cookbook",
			"xiaowo_kitchen_cooking_oil", "xiaowo_kitchen_creamy_stew", "xiaowo_kitchen_dried_vegetables", "xiaowo_kitchen_flowerberry_drink", "xiaowo_kitchen_fridge", "xiaowo_kitchen_fried_fat", "xiaowo_kitchen_fried_steak", "xiaowo_kitchen_kitchen_platter", "xiaowo_kitchen_large_raw_meat", "xiaowo_kitchen_meat_stew",
			"xiaowo_kitchen_milky_coffee", "xiaowo_kitchen_milky_drink", "xiaowo_kitchen_mince_soup", "xiaowo_kitchen_night_shift_coffee", "xiaowo_kitchen_overdrive_tonic", "xiaowo_kitchen_plate", "xiaowo_kitchen_plate_bundle", "xiaowo_kitchen_pot", "xiaowo_kitchen_red_fruit_soda", "xiaowo_kitchen_seasoning_salt",
			"xiaowo_kitchen_small_meat_skewer", "xiaowo_kitchen_spicy_mince", "xiaowo_kitchen_starch", "xiaowo_kitchen_starry_flowerberry_special", "xiaowo_kitchen_stove_double", "xiaowo_kitchen_stove_single", "xiaowo_kitchen_vegetable_noodles", "armor_lining", "flashbang_grenade", "glock_receiver",
			"gun_part", "handmade_pistol", "heavy_handmade_pistol", "heavy_pistol_ammo", "heavy_pistol_ammo_nl", "heavy_pistol_ammo_p", "printed_gun", "revolver", "shotgun", "small_pistol_ammo",
			"small_pistol_ammo_nl", "small_pistol_ammo_p", "smg", "smoke_grenade", "storage_bay_gun", "stun_gun", "aug_scanner", "combat_knife", "fce_cookware_grill", "fce_cookware_pan",
			"fce_cookware_pot", "fce_fishing_kitchen", "gf_carbon_fiber_fishing_rod", "gf_double_hook_fishing_rod", "gf_fishing_net", "gf_makeshift_fishing_rod", "gf_rusty_slicing_knife", "kitchen_knife", "metal_scanner", "skinning_knife",
			"surgery_tool", "backpack_large", "backpack_large_military", "backpack_medium", "backpack_medium_military", "backpack_small", "common_ore", "ingot_book", "metal_ingot", "nuts_metal",
			"rare_ore", "scrap_metal", "sign_material", "bottled_water", "bottled_water_premium", "bottle_hot_sauce", "bottle_printer", "card_brewer", "empty_beer_bottle", "gf_blazing_chili_pepper",
			"gf_chili_pepper", "gf_fish_oil", "gf_salt", "gf_super_salt", "large_bottled_water", "mini_bottle", "red_beer", "small_bottled_water", "wine_berry", "wine_book",
			"wine_bottle", "wine_superyeast", "wine_yeast", "wine_yeast_infinite", "wine_yeast_red", "bandage_item", "birth_control_pill", "black_injector", "caffeine_pill", "fanta_pill",
			"hemostatic_bandage_item", "injector_guide", "large_purple_injector", "med_bottle_blue", "med_bottle_orange", "med_bottle_red", "med_bottle_small_pink", "med_bottle_violet", "med_box", "med_keycard",
			"phagimycin_pill", "pink_alt_injector", "pink_injector", "pure_white_injector", "serum_green", "serum_red", "topical_bandage_item", "unlicensed_phagimycin_pill", "zyanide_pill", "bloomberry_seed",
			"dream_cap", "dream_cap_seed", "fillerweed", "fillerweed_seed", "hydroreed_seed", "kotton_fabric", "kotton_fiber", "kotton_seed", "nightmare_cap", "nutrifruit",
			"nutrifruit_seed", "nutrifruit_seed_packet", "back_inv_save_bag", "beerCase", "box_cutter", "box_dispenser", "box_tampon", "eng_box", "evidence_box", "expedition_box",
			"fridge", "gf_canned_goods_rack", "gf_drying_rack", "gf_food_freezer", "gf_processing_station", "gf_sealed_can", "hidden_save_bag", "machine_bay", "machine_bay_ext", "makeshift_storage_bay",
			"module_bay_expansion_kit", "nutrient_tablet_container", "save_bag", "sec_box", "service_box", "showcase_save_bag", "smuggler_bay", "smuggler_bay_mini", "smuggler_bay_mod", "sold_save_bag",
			"storage_bay", "storage_bay_chem", "storage_bay_large", "test_gizmo", "toolbox", "water_tablet_container", "water_test_container", "wine_rack", "business_permit", "cigarette_guide",
			"expedition_log", "fuel_guide", "hand_guide", "intel_sec", "logo_checker", "procurement_slip", "sec_slip", "sign_household", "sign_medical", "sign_water",
			"slip", "stamp_guide", "tutorial_book", "water_guide", "bayberry_fruit", "bayberry_seed", "beis_icecream", "be_advanced_yeast_reactor", "be_basic_cert", "be_brewery_sign",
			"be_cyber_bag", "be_feimei_wine", "be_forbidden_yeast_reactor", "be_golden_yeast_reactor", "be_high_cert", "be_ideal_end_wine", "be_incubator", "be_laolishi_bag", "be_luofu_dream_wine", "be_magic_pig_wine",
			"be_mid_cert", "be_nanfeng_wine", "be_neon_dream_wine", "be_neon_illusion_wine", "be_nini_card", "be_recipe_1", "be_recipe_2", "be_recipe_3", "be_recipe_4", "be_recipe_5",
			"be_recipe_6", "be_recipe_7", "be_recipe_8", "be_sakura_bag", "be_sea_blue_wine", "be_shuangqing_wine", "be_wine_printer", "be_wuren_mooncake", "be_xuxu_basic_battery", "be_xuxu_high_battery",
			"be_xuxu_mid_battery", "chili_fruit", "chili_seed", "coffee_bean", "cucumber_fruit", "cucumber_seed", "fce_dish_bad_meal", "fce_dish_fish_box_meal", "fce_dish_recipe_boil_clear", "fce_dish_recipe_boil_soy",
			"fce_dish_recipe_fry_oilpacked", "fce_dish_recipe_fry_plain", "fce_dish_recipe_grill_hot", "fce_dish_recipe_grill_plain", "fce_dish_recipe_grill_salted", "fce_dish_recipe_grill_spicy", "fce_dish_recipe_grill_sugar", "fce_dish_recipe_pan_cheese", "fce_dish_recipe_pan_juice", "fce_dish_recipe_raw_berry",
			"fce_dish_recipe_raw_crystal", "fce_dish_recipe_raw_plain", "fce_dish_recipe_steam_berry", "fce_dish_recipe_steam_plain", "fce_dish_recipe_stew_dark", "fce_dish_recipe_stew_nutri", "fce_dish_recipe_stew_plain", "fce_dish_recipe_stew_star", "food_stamp", "gf_blue_crystal_fish",
			"gf_blue_crystal_fish_fillet", "gf_conductive_tendon", "gf_dried_blue_crystal_fish", "gf_dried_electric_eel", "gf_dried_glass_fish", "gf_dried_golden_fish", "gf_dried_horned_swordfish", "gf_dried_large_yellow_croaker", "gf_dried_purifier_fish", "gf_dried_rainbow_fish",
			"gf_dried_ratfish", "gf_dried_redtail_fish", "gf_dried_sardine", "gf_dried_small_yellow_croaker", "gf_dried_volcanic_fish", "gf_electric_eel", "gf_electric_eel_fillet", "gf_glass_fish", "gf_gold", "gf_golden_fish",
			"gf_golden_fish_fillet", "gf_heat_gland", "gf_horned_swordfish", "gf_ice_crystal", "gf_large_yellow_croaker", "gf_oil_packed_fish", "gf_purification_sac", "gf_purifier_fish", "gf_purifier_fish_meat", "gf_purple_fillet",
			"gf_rainbow_fillet", "gf_rainbow_fish", "gf_ratfish", "gf_ratfish_fillet", "gf_redtail_fin", "gf_redtail_fish", "gf_redtail_fish_fillet", "gf_salted_fish", "gf_sardine", "gf_small_yellow_croaker",
			"gf_spicy_pickled_fish", "gf_star_fruit", "gf_swordfish_rostrum", "gf_transparent_membrane", "gf_volcanic_fish", "gf_volcanic_fish_fillet", "gf_yellow_croaker_fillet", "green_grape_fruit", "green_grape_seed", "jasmine_fruit",
			"jasmine_seed", "lemon_fruit", "lemon_seed", "lychee_fruit", "lychee_seed", "meal_cap", "morsel", "osmanthus_fruit", "osmanthus_seed", "ribwich",
			"ribwich_guide", "rice_fruit", "rice_seed", "sichuan_pepper_fruit", "sichuan_pepper_seed", "sign_food", "small_morsel", "stardust_berry_fruit", "stardust_berry_seed", "sugarcane_fruit",
			"sugarcane_seed", "void_plum_fruit", "void_plum_seed", "watertower", "watertower_tank", "wheat_fruit", "wheat_seed", "wormwood_fruit", "wormwood_seed", "yellow_grape_fruit",
			"yellow_grape_seed", "animal_cage", "feces_large", "feces_medium", "feces_small", "livestock_feed_dispenser", "rat", "scratcher", "scratcher_stack", "water_ration",
			"water_ration_small", "junk", "meat_scrap", "trashcan"
		};
	}
	[HarmonyPatch(typeof(SecData), "ApplyCrimeModifier", new Type[]
	{
		typeof(StoreClient),
		typeof(int)
	})]
	internal static class Patch_ApplyCrimeModifier
	{
		private static void Postfix(StoreClient client, int value, ref int __result)
		{
			try
			{
				if (client != null && Courier.IsCourier(client.identifier))
				{
					int num = __result;
					__result = NewbieGiftMod.ApplyContrabandCrimeRate(num);
					ModLog.Debug($"良心商人违禁值修正: {num} -> {__result}（{NewbieGiftMod.ContrabandCrimePercent()}%）");
				}
			}
			catch (Exception ex)
			{
				ModLog.Warn("ApplyCrimeModifier.Post: " + ex.Message);
			}
		}
	}
	internal enum CourierKind
	{
		First,
		Repeat
	}
	internal static class Courier
	{
		public static int DispatchedDay = -1;

		private static bool _delivered;

		private static bool _enqueued;

		private static readonly string[] BlacklistedItemIds = new string[2] { "empty_nuclear_waste_barrel", "nuclear_waste" };

		private static readonly string[] DirectoryClassNames = new string[29]
		{
			"MiscItemDirectory", "ToolDirectory", "MedsItemDirectory", "FoodItemDirectory", "WineDirectory", "HydroponicDirectory", "HusbandryDirectory", "MaterialDirectory", "GunsItemDirectory", "GunModDirectory",
			"MeleeWeaponItemDirectory", "ExplosiveItemDirectory", "ArmorItemDirectory", "ContainerItemDirectory", "FurnitureItemDirectory", "ModuleDirectory", "ModItemDirectory", "StationMachinery", "RuinedMachineDirectory", "KeyItemDirectory",
			"OrganDirectory", "AmenitiesItemDirectory", "ConstructionItemDirectory", "EquipmentDirectory", "ShipItemDirectory", "ShipSystemDirectory", "TechnicianBackpackDirectory", "PlayerAbilityItemDirectory", "UnusedDirectory"
		};

		private static List<string> _cachedAllIds;

		public static void ResetForNewDay()
		{
			_delivered = false;
			_enqueued = false;
		}

		public static bool IsCourier(string id)
		{
			if (!(id == "newbie_gift_courier_first"))
			{
				return id == "newbie_gift_courier_repeat";
			}
			return true;
		}

		public static bool SendFirst(string giftItemId, int cash)
		{
			return SendInternal(cash, CourierKind.First, giftItemId);
		}

		public static bool SendRepeat(int cash)
		{
			return SendInternal(cash, CourierKind.Repeat, null);
		}

		private static bool SendInternal(int cash, CourierKind kind, string itemId)
		{
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return false;
			}
			StoreClientManager storeClientManager = instance.storeClientManager;
			if (storeClientManager == null)
			{
				return false;
			}
			if (_enqueued)
			{
				return true;
			}
			StoreClient val = Build(cash, kind);
			if (val == null)
			{
				return false;
			}
			if (!Enqueue(storeClientManager, val))
			{
				return false;
			}
			_enqueued = true;
			if (kind == CourierKind.First)
			{
				ModLog.Info($"【首访】良心商人已派出，礼品={itemId}，预算={cash}");
			}
			else
			{
				ModLog.Info($"【回头客】良心商人已派出，预算={cash}");
			}
			return true;
		}

		private static bool Enqueue(StoreClientManager manager, StoreClient client)
		{
			try
			{
				manager.AddNextClient(client);
				return true;
			}
			catch (Exception ex)
			{
				ModLog.Debug("AddNextClient 失败，尝试 TryAddClient: " + ex.Message);
			}
			try
			{
				manager.TryAddClient(client, true);
				return true;
			}
			catch (Exception ex2)
			{
				ModLog.Warn("排入顾客失败: " + ex2.Message);
				return false;
			}
		}

		private static StoreClient Build(int cash, CourierKind kind)
		{
			StoreClient template = ResolveTemplate();
			StoreClient val = CreateClient(template, cash, kind);
			ApplyBuyPrice(val);
			ApplyVariety(val);
			ApplyBuyingIds(val);
			EnsureDialogue(val, template, kind);
			return val;
		}

		private static StoreClient ResolveTemplate()
		{
			StoreClient val = null;
			try
			{
				val = StoreClientList.CreateMerchant();
			}
			catch (Exception ex)
			{
				ModLog.Debug("CreateMerchant 失败: " + ex.Message);
			}
			if (val == null)
			{
				try
				{
					val = StoreClientListStory.CreateMentorClient();
				}
				catch (Exception ex2)
				{
					ModLog.Debug("CreateMentorClient 失败: " + ex2.Message);
				}
			}
			if (val == null)
			{
				try
				{
					val = StoreClientManager.PickShopperClient();
				}
				catch (Exception ex3)
				{
					ModLog.Debug("PickShopperClient 失败: " + ex3.Message);
				}
			}
			return val;
		}

		private static string IdentifierOf(CourierKind kind)
		{
			if (kind != CourierKind.First)
			{
				return "newbie_gift_courier_repeat";
			}
			return "newbie_gift_courier_first";
		}

		private static StoreClient CreateClient(StoreClient template, int cash, CourierKind kind)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			//IL_0006: Expected Obj, but got Unknown
			StoreClient val = new StoreClient();
			val.identifier = IdentifierOf(kind);
			val.displayName = Lang.T("良心商人", "The Honest Merchant");
			val.spriteName = ((template != null) ? template.spriteName : null);
			val.possibleSprites = ((template != null) ? template.possibleSprites : null);
			val.clientFaction = ((template != null) ? template.clientFaction : null);
			val.bubbleColor = ((template != null) ? template.bubbleColor : null);
			val.realName = ((template != null) ? template.realName : null);
			val.dismissable = true;
			val.arrestable = false;
			val.nonDismissableArrestable = false;
			val.nonShootable = true;
			val.isIntroduced = true;
			val.clientIntent = (ClientIntent)3;
			val.clientCash = cash;
			val.clientBudget = cash;
			val.useClientBudget = true;
			val.eventSourceId = "newbie_gift";
			val.clientBuyingTagList = new List<string>();
			val.clientBuyingIdList = new List<string>();
			val.clientBlackTagList = new List<string>();
			val.clientBlackIdList = new List<string>();
			val.clientNegociationCheck = new List<string>();
			val.tag = new List<string>();
			val.clientItemFeatureBuying = new List<string>();
			val.varietyTrackingList = new List<string>();
			val.CompleteClientCreation(true);
			try
			{
				val.canClientExposeFeature = ((template != null) ? template.canClientExposeFeature : null);
				val.CanExposeChem = template != null && template.CanExposeChem;
				val.isLazyInspector = false;
			}
			catch (Exception ex)
			{
				ModLog.Warn("挂载鉴定策略失败: " + ex.Message);
			}
			return val;
		}

		private static void ApplyBuyPrice(StoreClient c)
		{
			try
			{
				c.buyPriceModifier = NewbieGiftMod.BuyPriceBonus();
			}
			catch (Exception ex)
			{
				ModLog.Warn("设置收购价加成失败: " + ex.Message);
			}
		}

		private static void ApplyVariety(StoreClient c)
		{
			if (!NewbieGiftMod.VarietyBuy())
			{
				return;
			}
			try
			{
				c.SetVariety();
			}
			catch (Exception ex)
			{
				ModLog.Warn("SetVariety 失败: " + ex.Message);
			}
		}

		internal static bool IsBlacklisted(string id)
		{
			if (string.IsNullOrEmpty(id))
			{
				return false;
			}
			for (int i = 0; i < BlacklistedItemIds.Length; i++)
			{
				if (string.Equals(BlacklistedItemIds[i], id, StringComparison.OrdinalIgnoreCase))
				{
					return true;
				}
			}
			return false;
		}

		private static void ApplyBuyingIds(StoreClient c)
		{
			try
			{
				List<string> list = CollectAllItemIds();
				if (list == null || list.Count == 0)
				{
					return;
				}
				c.clientBuyingIdList.Clear();
				int num = 0;
				for (int i = 0; i < list.Count; i++)
				{
					string text = list[i];
					if (IsBlacklisted(text))
					{
						num++;
					}
					else
					{
						c.clientBuyingIdList.Add(text);
					}
				}
				if (c.clientBlackIdList != null)
				{
					c.clientBlackIdList.Clear();
					for (int j = 0; j < BlacklistedItemIds.Length; j++)
					{
						c.clientBlackIdList.Add(BlacklistedItemIds[j]);
					}
				}
				ModLog.Info($"良心商人收货 ID 已装填：{c.clientBuyingIdList.Count} 个（黑名单 {BlacklistedItemIds.Length} 个，已剔除 {num}）");
			}
			catch (Exception ex)
			{
				ModLog.Warn("装填收货 ID 失败: " + ex.Message);
			}
		}

		private static void EnsureDialogue(StoreClient c, StoreClient template, CourierKind kind)
		{
			try
			{
				string title = (string.IsNullOrEmpty(c.displayName) ? "???" : c.displayName);
				Dialogue mainDialogue = ((kind == CourierKind.First) ? BuildFirstDialogue(title) : BuildRepeatDialogue(title));
				c.mainDialogue = mainDialogue;
				c.isMainDialogueStarted = false;
				return;
			}
			catch (Exception ex)
			{
				ModLog.Warn("自建对话失败: " + ex.Message);
			}
			try
			{
				c.AddBasicDialog();
				if (c.mainDialogue != null)
				{
					c.isMainDialogueStarted = false;
					return;
				}
			}
			catch (Exception ex2)
			{
				ModLog.Debug("AddBasicDialog 回退失败: " + ex2.Message);
			}
			try
			{
				if (template != null && template.mainDialogue != null)
				{
					c.mainDialogue = template.mainDialogue;
					try
					{
						c.mainDialogue.title = c.displayName;
					}
					catch
					{
					}
					c.isMainDialogueStarted = false;
				}
			}
			catch (Exception ex3)
			{
				ModLog.Debug("模板对话回退失败: " + ex3.Message);
			}
		}

		private static Dialogue BuildFirstDialogue(string title)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			Dialogue val;
			Dialogue result = (val = new Dialogue().SetText(title, Lang.T("你好啊，同行。欢迎来到下层区。", "Hey there, pal. Welcome to the Lower District.")));
			val = val.NextDialogue();
			val.SetText(title, Lang.T("听说你今天刚开张，我特地带了份见面礼。", "Heard it's your opening day, so I brought you a housewarming gift."));
			val = val.NextDialogue();
			val.SetText(title, Lang.T("柜台上的这个储藏区是白送的，直接搬走就行。", "The storage bay on the counter is free - just take it."));
			int giftCash = NewbieGiftMod.Defaults.GiftCash;
			if (giftCash > 0)
			{
				val = val.NextDialogue();
				val.SetText(title, Lang.T($"另外，还有 {giftCash} 现金已经打进了你的账上——小小意思，不成敬意。", $"Also, {giftCash} cash has been wired to you - a small token, please accept it."));
			}
			val = val.NextDialogue();
			val.SetText(title, Lang.T("我这人什么都收，不过每种只收一件，价格上我会给你额外优惠。", "I buy just about anything, but only one of each kind. You'll get a better price from me."));
			return result;
		}

		private static Dialogue BuildRepeatDialogue(string title)
		{
			//IL_0000: Unknown result type (might be due to invalid IL or missing references)
			Dialogue val = new Dialogue().SetText(title, Lang.T("又见面了，最近生意怎么样？", "Good to see you again. How's business?"));
			val.NextDialogue().SetText(title, Lang.T("有什么好东西要出手的吗？我出价比别人高。", "Got anything good to sell? I pay better than the others."));
			return val;
		}

		public static void OnClientArrived(StoreClient client)
		{
			if (client == null)
			{
				return;
			}
			if (client.identifier == "newbie_gift_courier_first" || client.identifier == "newbie_gift_courier_repeat")
			{
				AntiCheat.Begin(client.identifier);
			}
			if (!_delivered)
			{
				if (client.identifier == "newbie_gift_courier_first")
				{
					HandleFirstArrival();
				}
				else if (client.identifier == "newbie_gift_courier_repeat")
				{
					HandleRepeatArrival();
				}
			}
		}

		public static void OnClientArrivedFromCurrent()
		{
			if (_delivered)
			{
				return;
			}
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				return;
			}
			StoreClientInstance currentClientInstance = instance.currentClientInstance;
			if (currentClientInstance != null)
			{
				StoreClient clientFromInstance = GetClientFromInstance(currentClientInstance);
				if (clientFromInstance != null)
				{
					OnClientArrived(clientFromInstance);
				}
			}
		}

		internal static StoreClient GetClientFromInstance(StoreClientInstance instance)
		{
			try
			{
				return instance.GetClientBlueprint();
			}
			catch (Exception ex)
			{
				ModLog.Debug("GetClientBlueprint 失败: " + ex.Message);
			}
			try
			{
				return instance.storeClient;
			}
			catch (Exception ex2)
			{
				ModLog.Debug("读取 storeClient 失败: " + ex2.Message);
			}
			return null;
		}

		private static void HandleFirstArrival()
		{
			if (!PutOnCounter("storage_bay"))
			{
				return;
			}
			_delivered = true;
			int currentDay = GetCurrentDay();
			NewbieGiftMod.SetFirstVisitDay(currentDay);
			NewbieGiftMod.SetLastVisitDay(currentDay);
			NewbieGiftMod.Instance?.TryGrantGiftCash();
			try
			{
				StoreUIManager instance = StoreUIManager.Instance;
				if (instance != null)
				{
					instance.Notify(Lang.T("良心商人把储藏箱放上了柜台", "The honest merchant dropped a storage bay on the counter"), "white");
				}
			}
			catch
			{
			}
			try
			{
				NewbieGiftMod.Instance?.Save?.Flush();
			}
			catch
			{
			}
			ModLog.Info($"【首访】完成：日={currentDay}");
		}

		private static void HandleRepeatArrival()
		{
			_delivered = true;
			int currentDay = GetCurrentDay();
			NewbieGiftMod.SetLastVisitDay(currentDay);
			ModLog.Info($"【回头客】到访完成：日={currentDay}");
		}

		private static int GetCurrentDay()
		{
			try
			{
				return StoreStation.GetDayCounter();
			}
			catch
			{
				return 0;
			}
		}

		public static bool PutOnCounter(string itemId)
		{
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance == null)
				{
					return false;
				}
				if (AlreadyOnCounter(itemId))
				{
					ModLog.Debug("柜台已有 mod 送出的该物品，跳过上架");
					return true;
				}
				GameItem val = ResolveGiftItem(itemId);
				if (val == null)
				{
					return false;
				}
				bool flag = NewbieGiftMod.FreeGift();
				ClearBadTags(val);
				if (flag)
				{
					MarkOwned(val);
				}
				instance.AddDirectSellingItemToTable(val, flag, false, false, 0);
				TagCounterItems(itemId);
				ClearBadTags(val);
				if (flag)
				{
					MarkOwned(val);
				}
				ModLog.Info($"已上架到柜台: {itemId}（{(flag ? "白送" : "需付款")}，tag={"NEWBIE_GIFT_DELIVERED"}）");
				return true;
			}
			catch (Exception ex)
			{
				ModLog.Warn("上架柜台失败: " + ex.Message);
				return false;
			}
		}

		private static void TagCounterItems(string itemId)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Expected Obj, but got Unknown
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)instance == (Object)null)
				{
					return;
				}
				List<GameItem> countersItems = instance.GetCountersItems();
				if (countersItems == null)
				{
					return;
				}
				for (int i = 0; i < countersItems.Count; i++)
				{
					GameItem val = countersItems[i];
					if (val != null)
					{
						string a = null;
						try
						{
							a = val.identifier;
						}
						catch
						{
						}
						if (string.Equals(a, itemId, StringComparison.OrdinalIgnoreCase))
						{
							TryEnableTag(val, "NEWBIE_GIFT_DELIVERED");
						}
					}
				}
			}
			catch (Exception ex)
			{
				ModLog.Debug("TagCounterItems 失败: " + ex.Message);
			}
		}

		private static GameItem ResolveGiftItem(string itemId)
		{
			if (!ItemExists(itemId))
			{
				ModLog.Warn("物品 id 无效: " + itemId);
				return null;
			}
			GameItem val = DirectoryMaster.Item(itemId, true);
			if (val == null)
			{
				ModLog.Warn("创建物品失败: " + itemId);
				return null;
			}
			return val;
		}

		private static void ClearBadTags(GameItem item)
		{
			if (item != null)
			{
				TryDisableTag(item, "stolen");
				TryDisableTag(item, "TAG_STOLEN");
				TryDisableTag(item, "STOLEN_VALUE_INT");
			}
		}

		private static void MarkOwned(GameItem item)
		{
			if (item != null)
			{
				TryDisableTag(item, "not_purchased");
				TryDisableTag(item, "TAG_NOT_PURCHASED");
				TryEnableTag(item, "IS_OWNED_TAG");
			}
		}

		private static void TryDisableTag(GameItem item, string tag)
		{
			try
			{
				item.DisableTag(tag, true);
			}
			catch (Exception ex)
			{
				ModLog.Debug("DisableTag(" + tag + ") 失败: " + ex.Message);
			}
		}

		private static void TryEnableTag(GameItem item, string tag)
		{
			try
			{
				item.EnableTag(tag, true);
			}
			catch (Exception ex)
			{
				ModLog.Debug("EnableTag(" + tag + ") 失败: " + ex.Message);
			}
		}

		private static bool HasTag(GameItem item, string tag)
		{
			try
			{
				return item.IsTag(tag);
			}
			catch (Exception ex)
			{
				ModLog.Debug("IsTag(" + tag + ") 失败: " + ex.Message);
				return false;
			}
		}

		public static bool AlreadyOnCounter(string itemId)
		{
			//IL_0016: Unknown result type (might be due to invalid IL or missing references)
			//IL_0021: Expected Obj, but got Unknown
			try
			{
				if (string.IsNullOrEmpty(itemId))
				{
					return false;
				}
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)instance == (Object)null)
				{
					return false;
				}
				List<GameItem> countersItems = instance.GetCountersItems();
				if (countersItems == null)
				{
					return false;
				}
				for (int i = 0; i < countersItems.Count; i++)
				{
					GameItem val = countersItems[i];
					if (val != null)
					{
						string a = null;
						try
						{
							a = val.identifier;
						}
						catch
						{
						}
						if (string.Equals(a, itemId, StringComparison.OrdinalIgnoreCase) && HasTag(val, "NEWBIE_GIFT_DELIVERED"))
						{
							return true;
						}
					}
				}
				return false;
			}
			catch (Exception ex)
			{
				ModLog.Debug("检查柜台物品失败: " + ex.Message);
				return false;
			}
		}

		private static bool ItemExists(string itemId)
		{
			try
			{
				if (DirectoryMaster.Has<GameItem>(itemId))
				{
					return true;
				}
			}
			catch (Exception ex)
			{
				ModLog.Debug("DirectoryMaster.Has 失败: " + ex.Message);
			}
			try
			{
				return DirectoryMaster.Item(itemId, true) != null;
			}
			catch (Exception ex2)
			{
				ModLog.Debug("DirectoryMaster.Item 失败: " + ex2.Message);
				return false;
			}
		}

		public static bool HandToPlayer(string itemId)
		{
			//IL_0007: Unknown result type (might be due to invalid IL or missing references)
			//IL_0012: Expected Obj, but got Unknown
			try
			{
				EmporiumEntry instance = EmporiumEntry.Instance;
				if ((Object)instance == (Object)null || instance.invElement == null)
				{
					return false;
				}
				GameItem val = DirectoryMaster.Item(itemId, true);
				if (val == null)
				{
					return false;
				}
				SlotMarker val2 = instance.invElement.TryFindOneValidInventorySlot(val, false);
				if (val2 != null)
				{
					val2.AcceptUnchecked();
					TagCounterItems(itemId);
					return true;
				}
				if (((GameInventory)instance.invElement).UncheckedAccept(val))
				{
					TagCounterItems(itemId);
					return true;
				}
				return false;
			}
			catch (Exception ex)
			{
				ModLog.Warn("直接入库失败: " + ex.Message);
				return false;
			}
		}

		private static List<string> CollectAllItemIds()
		{
			if (_cachedAllIds != null)
			{
				return _cachedAllIds;
			}
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			if (CatalogIds.All != null)
			{
				for (int i = 0; i < CatalogIds.All.Length; i++)
				{
					string text = CatalogIds.All[i];
					if (!string.IsNullOrWhiteSpace(text))
					{
						hashSet.Add(text);
					}
				}
			}
			int num = 0;
			int num2 = 0;
			for (int j = 0; j < DirectoryClassNames.Length; j++)
			{
				string text2 = DirectoryClassNames[j];
				try
				{
					List<string> identifierList = DirectoryMaster.GetIdentifierList<GameItem>(text2);
					if (identifierList == null)
					{
						num2++;
						continue;
					}
					for (int k = 0; k < identifierList.Count; k++)
					{
						string text3 = identifierList[k];
						if (!string.IsNullOrWhiteSpace(text3))
						{
							hashSet.Add(text3);
						}
					}
					num++;
				}
				catch (Exception ex)
				{
					num2++;
					ModLog.Debug("枚举目录 " + text2 + " 失败: " + ex.Message);
				}
			}
			List<string> list = new List<string>(hashSet.Count);
			foreach (string item in hashSet)
			{
				list.Add(item);
			}
			if (list.Count > 0)
			{
				if (num >= 20)
				{
					_cachedAllIds = list;
					ModLog.Info($"良心商人：运行时枚举共 {list.Count} 个物品 ID（目录 {num} 成功 / {num2} 失败）");
				}
				else
				{
					_cachedAllIds = new List<string>(CatalogIds.All);
					ModLog.Warn($"良心商人：运行时枚举不完整（{num}/{num2}），使用 CatalogIds 兜底 {_cachedAllIds.Count} 项");
				}
			}
			return _cachedAllIds ?? list;
		}
	}
	[HarmonyPatch(typeof(StoreClient), "StartMainDialogue")]
	internal static class Patch_StartMainDialogue
	{
		private static void Postfix(StoreClient __instance)
		{
			try
			{
				if (__instance != null && Courier.IsCourier(__instance.identifier))
				{
					Courier.OnClientArrived(__instance);
				}
			}
			catch (Exception ex)
			{
				ModLog.Warn("StartMainDialogue.Post: " + ex.Message);
			}
		}
	}
	[HarmonyPatch(typeof(PlayerStore), "DismissCurrentClient")]
	internal static class Patch_DismissCurrentClient
	{
		private static void Postfix()
		{
			try
			{
				AntiCheat.End();
			}
			catch (Exception ex)
			{
				ModLog.Warn("DismissCurrentClient.Post: " + ex.Message);
			}
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
	internal static class Embedded
	{
		public static Sprite LoadSprite(string resourceSuffix)
		{
			return LoadSprite(Assembly.GetExecutingAssembly(), resourceSuffix);
		}

		public static Sprite LoadSprite(Assembly asm, string resourceSuffix)
		{
			//IL_008c: Unknown result type (might be due to invalid IL or missing references)
			//IL_0091: Unknown result type (might be due to invalid IL or missing references)
			//IL_0098: Unknown result type (might be due to invalid IL or missing references)
			//IL_009f: Unknown result type (might be due to invalid IL or missing references)
			//IL_00a9: Expected Obj, but got Unknown
			//IL_00f9: Unknown result type (might be due to invalid IL or missing references)
			//IL_0108: Unknown result type (might be due to invalid IL or missing references)
			try
			{
				if (asm == null)
				{
					return null;
				}
				string text = FindResource(asm, resourceSuffix);
				if (text == null)
				{
					MelonLogger.Warning("[Embedded] 找不到嵌入资源 “" + resourceSuffix + "”");
					return null;
				}
				using Stream stream = asm.GetManifestResourceStream(text);
				if (stream == null)
				{
					return null;
				}
				byte[] array = new byte[stream.Length];
				int i;
				int num;
				for (i = 0; i < array.Length; i += num)
				{
					num = stream.Read(array, i, array.Length - i);
					if (num <= 0)
					{
						break;
					}
				}
				if (i == 0)
				{
					return null;
				}
				Texture2D val = new Texture2D(2, 2, (TextureFormat)4, false, false)
				{
					filterMode = (FilterMode)0,
					wrapMode = (TextureWrapMode)1,
					hideFlags = (HideFlags)61
				};
				Il2CppStructArray<byte> val2 = Il2CppStructArray<byte>.op_Implicit(array);
				if (!ImageConversion.LoadImage(val, val2))
				{
					Object.Destroy((Object)(object)val);
					MelonLogger.Warning("[Embedded] 解码失败 “" + resourceSuffix + "”");
					return null;
				}
				Sprite val3 = Sprite.Create(val, new Rect(0f, 0f, (float)((Texture)val).width, (float)((Texture)val).height), new Vector2(0.5f, 0.5f), 100f);
				((Object)val3).hideFlags = (HideFlags)61;
				return val3;
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("[Embedded] 加载 “" + resourceSuffix + "” 异常: " + ex.Message);
				return null;
			}
		}

		private static string FindResource(Assembly asm, string suffix)
		{
			if (asm == null || string.IsNullOrEmpty(suffix))
			{
				return null;
			}
			string[] manifestResourceNames = asm.GetManifestResourceNames();
			foreach (string text in manifestResourceNames)
			{
				if (text.EndsWith(suffix, StringComparison.OrdinalIgnoreCase))
				{
					return text;
				}
			}
			return null;
		}
	}
	public sealed class HonestMerchantPerk : CustomPerk
	{
		public const string PerkId = "newbie_gift";

		public override string Id => "newbie_gift";

		public override string DisplayName => Lang.T("良心商人", "The Honest Merchant");

		public override string Description => Lang.T("下层区多了一位偶尔来光顾你生意的良心商人。", "A rare honest merchant has appeared in the Lower District and occasionally drops by your shop.");

		public override int Cost => 1;

		public override int MaxSlot => 0;

		public static bool IsActive
		{
			get
			{
				try
				{
					return StartingPerk.IsPerkActive("newbie_gift");
				}
				catch
				{
					return false;
				}
			}
		}
	}
	internal static class Ids
	{
		public const string PerkId = "newbie_gift";

		public const string CourierFirstClientId = "newbie_gift_courier_first";

		public const string CourierRepeatClientId = "newbie_gift_courier_repeat";

		public const string EventSourceId = "newbie_gift";

		public const string PerkLocKeyPrefix = "perk_newbie_gift_";
	}
	internal static class Lang
	{
		internal const string CategoryId = "良心商人";

		internal const string DefaultLang = "zh";

		private static string _current;

		internal static void Refresh()
		{
			try
			{
				MelonPreferences_Category category = MelonPreferences.GetCategory("良心商人");
				_current = (string.Equals(((category != null) ? category.GetEntry<string>("Language") : null)?.Value ?? "zh", "en", StringComparison.OrdinalIgnoreCase) ? "en" : "zh");
			}
			catch
			{
				_current = "zh";
			}
		}

		public static string T(string zh, string en)
		{
			if (string.IsNullOrEmpty(_current))
			{
				Refresh();
			}
			if (_current == "en" && !string.IsNullOrWhiteSpace(en))
			{
				return en;
			}
			return zh ?? "";
		}
	}
	internal static class ModLog
	{
		public const string Prefix = "[良心商人] ";

		public static bool DebugEnabled;

		public static void Info(string message)
		{
			MelonLogger.Msg("[良心商人] " + message);
		}

		public static void Warn(string message)
		{
			MelonLogger.Warning("[良心商人] " + message);
		}

		public static void Error(string message)
		{
			MelonLogger.Error("[良心商人] " + message);
		}

		public static void Debug(string message)
		{
			if (DebugEnabled)
			{
				MelonLogger.Msg("[良心商人] [debug] " + message);
			}
		}
	}
	public sealed class NewbieGiftMod : MelonMod
	{
		public static class Defaults
		{
			public const int FirstDay = 1;

			public static readonly int GiftCash = 100;

			public const string GiftItemId = "storage_bay";

			public const int ContrabandCrimePercentDefault = 50;
		}

		private MelonPreferences_Category _cat;

		internal MelonPreferences_Entry<bool> CfgEnabled;

		internal MelonPreferences_Entry<bool> CfgFreeGift;

		internal MelonPreferences_Entry<bool> CfgDirectToInventory;

		internal MelonPreferences_Entry<bool> CfgVarietyBuy;

		internal MelonPreferences_Entry<int> CfgBuyPriceBonus;

		internal MelonPreferences_Entry<int> CfgContrabandCrimePercent;

		internal MelonPreferences_Entry<string> CfgLanguage;

		internal SaveStore Save;

		private static int _lastHandledDay = -1;

		private static string _lastRunId = "";

		public static NewbieGiftMod Instance { get; private set; }

		public override void OnInitializeMelon()
		{
			//IL_01b1: Unknown result type (might be due to invalid IL or missing references)
			//IL_01be: Expected Obj, but got Unknown
			//IL_022b: Unknown result type (might be due to invalid IL or missing references)
			//IL_0238: Expected Obj, but got Unknown
			Instance = this;
			_cat = MelonPreferences.CreateCategory("良心商人", "良心商人");
			CfgEnabled = _cat.CreateEntry<bool>("Enabled", true, "启用 mod / Enable mod", (string)null, false, false, (ValueValidator)null, (string)null);
			CfgFreeGift = _cat.CreateEntry<bool>("FreeGift", true, "白送=true / 要付款=false | Free=true / Paid=false", (string)null, false, false, (ValueValidator)null, (string)null);
			CfgDirectToInventory = _cat.CreateEntry<bool>("DirectToInventory", false, "直接入库（排障用）/ Direct to inventory (debug)", (string)null, false, false, (ValueValidator)null, (string)null);
			CfgVarietyBuy = _cat.CreateEntry<bool>("VarietyBuy", true, "每种只收 1 件 / One per kind", (string)null, false, false, (ValueValidator)null, (string)null);
			CfgBuyPriceBonus = _cat.CreateEntry<int>("BuyPriceBonus", 15, "收购价加成 % / Buy price bonus %", (string)null, false, false, (ValueValidator)null, (string)null);
			CfgContrabandCrimePercent = _cat.CreateEntry<int>("ContrabandCrimePercent", 50, "违禁值 % / Contraband crime %", (string)null, false, false, (ValueValidator)null, (string)null);
			CfgLanguage = _cat.CreateEntry<string>("Language", "zh", "语言 / Language (zh / en)", (string)null, false, false, (ValueValidator)null, (string)null);
			_cat.SaveToFile(false);
			Lang.Refresh();
			Save = new SaveStore("NewbieGift");
			try
			{
				((MelonBase)this).HarmonyInstance.PatchAll();
				ModLog.Info("Harmony 补丁已挂（特性 / 良心商人 / 违禁值）");
			}
			catch (Exception ex)
			{
				ModLog.Error("Harmony 挂载异常: " + ex.Message);
			}
			try
			{
				PerkRegistry.SetupIcon();
			}
			catch (Exception ex2)
			{
				ModLog.Warn("SetupIcon: " + ex2.Message);
			}
			try
			{
				MethodInfo methodInfo = AccessTools.Method(typeof(PlayerStore), "LoadGame", (Type[])null, (Type[])null);
				if (methodInfo != null)
				{
					((MelonBase)this).HarmonyInstance.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(typeof(NewbieGiftMod), "OnLoadGamePostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					ModLog.Info("读档钩子已挂（PlayerStore.LoadGame）");
				}
				else
				{
					ModLog.Warn("未找到 PlayerStore.LoadGame");
				}
			}
			catch (Exception ex3)
			{
				ModLog.Warn("读档钩子异常: " + ex3.Message);
			}
			try
			{
				MethodInfo methodInfo2 = AccessTools.Method(typeof(PlayerStore), "OpenShutter", (Type[])null, (Type[])null);
				if (methodInfo2 != null)
				{
					((MelonBase)this).HarmonyInstance.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(typeof(NewbieGiftMod), "OnOpenShutterPostfix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
					ModLog.Info("开门钩子已挂（PlayerStore.OpenShutter）");
				}
				else
				{
					ModLog.Warn("未找到 PlayerStore.OpenShutter");
				}
			}
			catch (Exception ex4)
			{
				ModLog.Warn("开门钩子异常: " + ex4.Message);
			}
			ModLog.Info("良心商人已加载（v1.7.0，反作弊 + 容器递归）");
		}

		public override void OnUpdate()
		{
			try
			{
				AntiCheat.Tick();
			}
			catch
			{
			}
		}

		public override void OnApplicationQuit()
		{
			try
			{
				Save?.Flush(force: true);
			}
			catch
			{
			}
		}

		private static void OnLoadGamePostfix()
		{
			try
			{
				NewbieGiftMod instance = Instance;
				if (instance != null)
				{
					_lastHandledDay = -1;
					Courier.DispatchedDay = -1;
					Courier.ResetForNewDay();
					ResetStaticsOnRunChange();
					instance.Save?.ReloadNow();
					ModLog.Info("读档完成，存档已同步加载");
				}
			}
			catch (Exception ex)
			{
				ModLog.Warn("OnLoadGamePostfix: " + ex.Message);
			}
		}

		private static void OnOpenShutterPostfix()
		{
			try
			{
				NewbieGiftMod instance = Instance;
				if (instance == null)
				{
					return;
				}
				ResetStaticsOnRunChange();
				int dayCounter;
				try
				{
					dayCounter = StoreStation.GetDayCounter();
				}
				catch
				{
					return;
				}
				if (dayCounter > 0 && _lastHandledDay != dayCounter)
				{
					_lastHandledDay = dayCounter;
					if (instance.Save != null && !instance.Save.Loaded)
					{
						instance.Save.ReloadNow();
					}
					instance.OnDayStarted(dayCounter);
				}
			}
			catch (Exception ex)
			{
				ModLog.Error("OnOpenShutterPostfix: " + ex.Message);
			}
		}

		private static void ResetStaticsOnRunChange()
		{
			try
			{
				string text = "";
				try
				{
					PlayerStore instance = PlayerStore.Instance;
					text = ((instance != null) ? instance.runID : null) ?? "";
				}
				catch
				{
				}
				if (text != _lastRunId)
				{
					_lastRunId = text;
					_lastHandledDay = -1;
					Courier.DispatchedDay = -1;
					Courier.ResetForNewDay();
					ModLog.Info("runID 变化：'" + text + "'，static 状态已重置");
				}
			}
			catch
			{
			}
		}

		private void OnDayStarted(int day)
		{
			try
			{
				Courier.ResetForNewDay();
				if (!CfgEnabled.Value || !HonestMerchantPerk.IsActive || !IsVisitDay(day) || Courier.DispatchedDay == day)
				{
					return;
				}
				int num = CurrentSlotId();
				if (num < 0)
				{
					ModLog.Warn("无法获取存档槽位，跳过本日派发");
					return;
				}
				string text = SaveKeys.GrantedDay(num);
				if (Save.GetInt(text) != day)
				{
					int cash = CalcCourierCash(day);
					if (GetFirstVisitDay() == -1)
					{
						DispatchFirstMerchant(day, text, cash);
					}
					else
					{
						DispatchRepeatMerchant(day, text, cash);
					}
				}
			}
			catch (Exception ex)
			{
				ModLog.Error("OnDayStarted: " + ex.Message);
			}
			finally
			{
				try
				{
					Save?.Flush();
				}
				catch (Exception ex2)
				{
					ModLog.Warn("Save.Flush: " + ex2.Message);
				}
			}
		}

		private void DispatchFirstMerchant(int day, string grantedKey, int cash)
		{
			if (CfgDirectToInventory.Value)
			{
				if (!Courier.HandToPlayer("storage_bay"))
				{
					ModLog.Warn("直接入库失败，下个周期重试");
					return;
				}
				Courier.DispatchedDay = day;
				Save.Set(grantedKey, day);
				SetFirstVisitDay(day);
				SetLastVisitDay(day);
				TryGrantGiftCash();
				Notify(Lang.T("储藏箱已直接塞进仓库", "Storage bay placed into your inventory"));
				ModLog.Info($"【首访·直接入库】完成：日={day}");
			}
			else if (Courier.SendFirst("storage_bay", cash))
			{
				Courier.DispatchedDay = day;
				Save.Set(grantedKey, day);
				ModLog.Info($"第 {day} 天，【首访】良心商人已派出（预算 {cash}）");
			}
		}

		private void DispatchRepeatMerchant(int day, string grantedKey, int cash)
		{
			if (Courier.SendRepeat(cash))
			{
				Courier.DispatchedDay = day;
				Save.Set(grantedKey, day);
				ModLog.Info($"第 {day} 天，【回头客】良心商人已派出（预算 {cash}）");
			}
		}

		internal void TryGrantGiftCash()
		{
			if (Defaults.GiftCash <= 0)
			{
				return;
			}
			if (Save == null || !Save.Loaded)
			{
				ModLog.Debug("TryGrantGiftCash: 存档未就绪，跳过");
				return;
			}
			int num = CurrentSlotId();
			if (num < 0)
			{
				return;
			}
			string key = SaveKeys.CashGranted(num);
			if (Save.GetInt(key) == 1)
			{
				ModLog.Debug($"TryGrantGiftCash: slot {num} 礼金已发过，跳过");
				return;
			}
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance != null)
				{
					instance.ModCash(Defaults.GiftCash, true);
				}
			}
			catch (Exception ex)
			{
				ModLog.Warn("加现金失败: " + ex.Message);
				return;
			}
			Save.Set(key, 1);
			Save.Flush();
			ModLog.Info($"礼金已发：+{Defaults.GiftCash}（slot {num}）");
		}

		private void Notify(string text)
		{
			try
			{
				StoreUIManager instance = StoreUIManager.Instance;
				if (instance != null)
				{
					instance.Notify(text, "white");
				}
			}
			catch
			{
			}
		}

		private static bool IsVisitDay(int day)
		{
			if (day != 1)
			{
				if (day > 1)
				{
					return day % 7 == 0;
				}
				return false;
			}
			return true;
		}

		private static int CalcCourierCash(int day)
		{
			return 15 * (10 + day);
		}

		private static int CurrentSlotId()
		{
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance != null)
				{
					return instance.saveSlotId;
				}
			}
			catch
			{
			}
			return -1;
		}

		internal static int GetFirstVisitDay()
		{
			return Instance?.Save?.GetInt(SaveKeys.FirstVisitDay(CurrentSlotId()), -1) ?? (-1);
		}

		internal static void SetFirstVisitDay(int day)
		{
			Instance?.Save?.Set(SaveKeys.FirstVisitDay(CurrentSlotId()), day);
		}

		internal static void SetLastVisitDay(int day)
		{
			Instance?.Save?.Set(SaveKeys.LastVisitDay(CurrentSlotId()), day);
		}

		internal static bool FreeGift()
		{
			return Instance?.CfgFreeGift?.Value ?? true;
		}

		internal static bool VarietyBuy()
		{
			return Instance?.CfgVarietyBuy?.Value ?? true;
		}

		internal static int BuyPriceBonus()
		{
			return Instance?.CfgBuyPriceBonus?.Value ?? 15;
		}

		internal static int ContrabandCrimePercent()
		{
			int num = Instance?.CfgContrabandCrimePercent?.Value ?? 50;
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

		internal static int ApplyContrabandCrimeRate(int baseValue)
		{
			if (baseValue <= 0)
			{
				return baseValue;
			}
			int num = ContrabandCrimePercent();
			long num2 = (long)baseValue * (long)num / 100;
			if (num2 < 0)
			{
				num2 = 0L;
			}
			if (num2 > int.MaxValue)
			{
				num2 = 2147483647L;
			}
			return (int)num2;
		}
	}
	[HarmonyPatch(typeof(StartingPerkList), "InitStartingPerk")]
	internal static class InitStartingPerkPatch
	{
		private static void Postfix()
		{
			PerkRegistry.EnsureRegistered();
			PerkRegistry.SetupIcon();
		}
	}
	[HarmonyPatch(typeof(StartingPerkIconLoader), "Start")]
	internal static class StartingPerkIconLoaderStartPatch
	{
		private static void Postfix()
		{
			try
			{
				PerkRegistry.SetupIcon();
			}
			catch (Exception ex)
			{
				ModLog.Warn("IconLoader.Start.Post: " + ex.Message);
			}
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
	[HarmonyPatch(typeof(StoreUIManager), "OnNextClientArrived")]
	internal static class Patch_OnNextClientArrived
	{
		private static void Postfix()
		{
			try
			{
				Courier.OnClientArrivedFromCurrent();
			}
			catch (Exception ex)
			{
				ModLog.Warn("OnNextClientArrived.Post: " + ex.Message);
			}
		}
	}
	public static class PerkRegistry
	{
		public static readonly CustomPerk[] All = new CustomPerk[1]
		{
			new HonestMerchantPerk()
		};

		private static Sprite _cachedIcon;

		private static bool _iconLoaded;

		private static Sprite GetIcon()
		{
			if (!_iconLoaded)
			{
				_iconLoaded = true;
				try
				{
					_cachedIcon = Embedded.LoadSprite(typeof(NewbieGiftMod).Assembly, "newbie_gift.png");
				}
				catch (Exception ex)
				{
					ModLog.Warn("加载图标失败: " + ex.Message);
				}
			}
			return _cachedIcon;
		}

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
					bool flag = false;
					for (int j = 0; j < perks.Count; j++)
					{
						if (perks[j] != null && perks[j].id == customPerk.Id)
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						perks.Add(customPerk.Create());
						ModLog.Info($"注册自定义特性：{customPerk.Id}（消耗 {customPerk.Cost} 点）");
					}
				}
			}
			catch (Exception ex)
			{
				ModLog.Error("注册特性失败：" + ex.Message);
			}
		}

		public static StartingPerk GetOrCreate(CustomPerk perk)
		{
			List<StartingPerk> perks = StartingPerkList.Perks;
			if (perks != null)
			{
				for (int i = 0; i < perks.Count; i++)
				{
					if (perks[i] != null && perks[i].id == perk.Id)
					{
						return perks[i];
					}
				}
			}
			return perk.Create();
		}

		public static void EnsurePickerElements(PerkUIController ui)
		{
			try
			{
				if ((Object)(object)ui == (Object)null || (Object)(object)ui.availablePerks == (Object)null)
				{
					return;
				}
				EnsureRegistered();
				Sprite icon = GetIcon();
				CustomPerk[] all = All;
				foreach (CustomPerk customPerk in all)
				{
					StartingPerkElement val = FindElement(ui.availablePerks, customPerk.Id);
					if ((Object)(object)val != (Object)null)
					{
						if (val.perk == null)
						{
							val.perk = GetOrCreate(customPerk);
						}
						if ((Object)(object)val.icon != (Object)null && (Object)(object)icon != (Object)null)
						{
							val.icon.sprite = icon;
						}
					}
					else
					{
						if ((Object)(object)ui.perkElementPrefab == (Object)null)
						{
							continue;
						}
						GameObject val2 = Object.Instantiate<GameObject>(ui.perkElementPrefab, ui.availablePerks.transform);
						StartingPerkElement component = val2.GetComponent<StartingPerkElement>();
						if ((Object)(object)component == (Object)null)
						{
							Object.Destroy((Object)(object)val2);
							continue;
						}
						component.id = customPerk.Id;
						component.isSelected = false;
						component.perk = GetOrCreate(customPerk);
						if ((Object)(object)component.icon != (Object)null && (Object)(object)icon != (Object)null)
						{
							component.icon.sprite = icon;
						}
						val2.SetActive(true);
						ModLog.Info("UI 元素已注入：" + customPerk.Id);
					}
				}
				ui.SortPerkContainer(ui.availablePerks);
			}
			catch (Exception ex)
			{
				ModLog.Error("注入特性界面失败：" + ex.Message);
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
			if ((Object)(object)element == (Object)null)
			{
				return;
			}
			CustomPerk customPerk = Find(element.id);
			if (customPerk != null)
			{
				if (element.perk == null)
				{
					element.perk = GetOrCreate(customPerk);
				}
				if ((Object)(object)element.icon != (Object)null && (Object)(object)element.icon.sprite == (Object)null)
				{
					element.icon.sprite = GetIcon();
				}
			}
		}

		public static void SetupIcon()
		{
			if ((Object)(object)GetIcon() == (Object)null)
			{
				ModLog.Warn("自定义图标为空，跳过图标注入");
			}
			else
			{
				InjectIcon();
			}
		}

		public static void InjectIcon()
		{
			try
			{
				Dictionary<string, Sprite> iconDict = GetIconDict();
				if (iconDict == null)
				{
					ModLog.Warn("拿不到 perkIcons 字典");
					return;
				}
				Sprite val = GetIcon();
				CustomPerk[] all = All;
				foreach (CustomPerk customPerk in all)
				{
					if ((Object)(object)val == (Object)null)
					{
						Enumerator<string, Sprite> enumerator = iconDict.GetEnumerator();
						while (enumerator.MoveNext())
						{
							KeyValuePair<string, Sprite> current = enumerator.Current;
							if ((Object)(object)current.Value != (Object)null)
							{
								val = current.Value;
								break;
							}
						}
						if ((Object)(object)val == (Object)null)
						{
							ModLog.Warn("无可用图标，跳过");
							continue;
						}
						ModLog.Warn("自定义图标为空，用原版兜底");
					}
					iconDict[customPerk.Id] = val;
					ModLog.Info("图标已注入：" + customPerk.Id);
				}
			}
			catch (Exception ex)
			{
				ModLog.Error("图标注入失败：" + ex.Message);
			}
		}

		private static Dictionary<string, Sprite> GetIconDict()
		{
			try
			{
				object obj = null;
				PropertyInfo propertyInfo = AccessTools.Property(typeof(StartingPerkIconLoader), "perkIcons");
				if (propertyInfo != null)
				{
					try
					{
						obj = propertyInfo.GetValue(null);
					}
					catch
					{
					}
				}
				if (obj == null)
				{
					FieldInfo fieldInfo = AccessTools.Field(typeof(StartingPerkIconLoader), "_perkIcons_k__BackingField") ?? AccessTools.Field(typeof(StartingPerkIconLoader), "<perkIcons>k__BackingField");
					if (fieldInfo != null)
					{
						try
						{
							obj = fieldInfo.GetValue(null);
						}
						catch
						{
						}
					}
				}
				return obj as Dictionary<string, Sprite>;
			}
			catch
			{
				return null;
			}
		}
	}
	internal static class SaveKeys
	{
		public static string FirstVisitDay(int slot)
		{
			return $"first_visit_day_slot{slot}";
		}

		public static string LastVisitDay(int slot)
		{
			return $"last_visit_day_slot{slot}";
		}

		public static string CashGranted(int slot)
		{
			return $"gift_cash_granted_slot{slot}";
		}

		public static string GrantedDay(int slot)
		{
			return $"granted_day_slot{slot}";
		}
	}
	internal sealed class SaveStore
	{
		private readonly string _modId;

		private readonly Dictionary<string, string> _mem = new Dictionary<string, string>(StringComparer.Ordinal);

		private string _currentRun = "";

		private bool _loadPending;

		private int _pendingFrames;

		private bool _dirty;

		public int LoadDelayFrames { get; set; } = 30;

		public bool Loaded => !_loadPending;

		private string UserDataDir
		{
			get
			{
				try
				{
					return Path.Combine(Directory.GetParent(Application.dataPath)?.FullName ?? "", "UserData", _modId);
				}
				catch
				{
					return Path.Combine("UserData", _modId);
				}
			}
		}

		public SaveStore(string modId)
		{
			_modId = (string.IsNullOrEmpty(modId) ? "NewbieGiftMod" : modId);
			TryMigrateLegacyDir();
			MarkLoadPending();
		}

		private void TryMigrateLegacyDir()
		{
			try
			{
				string text = Directory.GetParent(Application.dataPath)?.FullName ?? "";
				if (!string.IsNullOrEmpty(text))
				{
					string text2 = Path.Combine(text, "UserData", "良心商人");
					string text3 = Path.Combine(text, "UserData", _modId);
					if (Directory.Exists(text2) && !Directory.Exists(text3))
					{
						Directory.Move(text2, text3);
						MelonLogger.Msg("[SaveStore] 旧目录已迁移：良心商人 → " + _modId);
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("[SaveStore] 旧目录迁移失败（不影响新数据）: " + ex.Message);
			}
		}

		private string PathForRun(string runId)
		{
			return Path.Combine(UserDataDir, "save_" + (string.IsNullOrEmpty(runId) ? "_pending" : runId) + ".txt");
		}

		private string ResolveRunId()
		{
			try
			{
				PlayerStore instance = PlayerStore.Instance;
				if (instance != null)
				{
					string runID = instance.runID;
					if (!string.IsNullOrEmpty(runID))
					{
						return runID;
					}
				}
			}
			catch
			{
			}
			return _currentRun ?? "";
		}

		public void Set(string key, string value)
		{
			if (!string.IsNullOrEmpty(key))
			{
				_mem[key] = value ?? "";
				_dirty = true;
			}
		}

		public void Set(string key, int v)
		{
			Set(key, v.ToString(CultureInfo.InvariantCulture));
		}

		public void Set(string key, long v)
		{
			Set(key, v.ToString(CultureInfo.InvariantCulture));
		}

		public void Set(string key, float v)
		{
			Set(key, v.ToString("R", CultureInfo.InvariantCulture));
		}

		public void Set(string key, double v)
		{
			Set(key, v.ToString("R", CultureInfo.InvariantCulture));
		}

		public void Set(string key, bool v)
		{
			Set(key, v ? "1" : "0");
		}

		public string GetString(string key, string fallback = "")
		{
			if (!_mem.TryGetValue(key, out var value))
			{
				return fallback;
			}
			return value;
		}

		public int GetInt(string key, int fallback = 0)
		{
			if (!int.TryParse(GetString(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				return fallback;
			}
			return result;
		}

		public long GetLong(string key, long fallback = 0L)
		{
			if (!long.TryParse(GetString(key), NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
			{
				return fallback;
			}
			return result;
		}

		public float GetFloat(string key, float fallback = 0f)
		{
			if (!float.TryParse(GetString(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return fallback;
			}
			return result;
		}

		public double GetDouble(string key, double fallback = 0.0)
		{
			if (!double.TryParse(GetString(key), NumberStyles.Float, CultureInfo.InvariantCulture, out var result))
			{
				return fallback;
			}
			return result;
		}

		public bool GetBool(string key, bool fallback = false)
		{
			string text = GetString(key);
			if (text == "1")
			{
				return true;
			}
			if (text == "0")
			{
				return false;
			}
			if (!bool.TryParse(text, out var result))
			{
				return fallback;
			}
			return result;
		}

		public bool Has(string key)
		{
			return _mem.ContainsKey(key);
		}

		public void Remove(string key)
		{
			if (!string.IsNullOrEmpty(key) && _mem.Remove(key))
			{
				_dirty = true;
			}
		}

		public void MarkLoadPending()
		{
			_mem.Clear();
			_loadPending = true;
			_pendingFrames = Math.Max(1, LoadDelayFrames);
		}

		public void ReloadNow()
		{
			_loadPending = false;
			string text = ResolveRunId();
			if (string.IsNullOrEmpty(text))
			{
				LoadFromDisk(PathForRun(""));
				return;
			}
			if (string.IsNullOrEmpty(_currentRun))
			{
				string text2 = PathForRun("");
				string text3 = PathForRun(text);
				try
				{
					if (File.Exists(text2) && !File.Exists(text3))
					{
						Directory.CreateDirectory(UserDataDir);
						File.Move(text2, text3);
					}
				}
				catch
				{
				}
			}
			_currentRun = text;
			LoadFromDisk(PathForRun(_currentRun));
		}

		public void Tick()
		{
			if (_loadPending)
			{
				if (_pendingFrames > 0)
				{
					_pendingFrames--;
				}
				else
				{
					ReloadNow();
				}
			}
		}

		public void Flush(bool force = false)
		{
			if (!_dirty && !force)
			{
				return;
			}
			try
			{
				Directory.CreateDirectory(UserDataDir);
				using StreamWriter streamWriter = new StreamWriter(PathForRun(_currentRun), append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
				streamWriter.WriteLine("#runID=" + _currentRun);
				streamWriter.WriteLine("#saved=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
				foreach (KeyValuePair<string, string> item in _mem)
				{
					streamWriter.WriteLine(item.Key + "=" + item.Value);
				}
				_dirty = false;
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("[SaveStore] Flush 失败: " + ex.Message);
			}
		}

		private void LoadFromDisk(string path)
		{
			try
			{
				_mem.Clear();
				if (!File.Exists(path))
				{
					return;
				}
				string[] array = File.ReadAllLines(path);
				foreach (string text in array)
				{
					if (!string.IsNullOrEmpty(text) && text[0] != '#')
					{
						int num = text.IndexOf('=');
						if (num > 0)
						{
							string key = text.Substring(0, num);
							string value = text.Substring(num + 1);
							_mem[key] = value;
						}
					}
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("[SaveStore] Load 失败: " + ex.Message);
			}
		}
	}
	internal static class Tags
	{
		public const string Stolen = "stolen";

		public const string TagStolen = "TAG_STOLEN";

		public const string StolenValueInt = "STOLEN_VALUE_INT";

		public const string NotPurchased = "not_purchased";

		public const string TagNotPurchased = "TAG_NOT_PURCHASED";

		public const string IsOwned = "IS_OWNED_TAG";

		public const string NewbieGiftDelivered = "NEWBIE_GIFT_DELIVERED";
	}
}
