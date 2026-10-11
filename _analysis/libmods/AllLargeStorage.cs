using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Security;
using System.Security.Permissions;
using AllLargeStorage;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(Core), "AllLargeStorage", "1.1.0", "local", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = "")]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
[assembly: AssemblyVersion("0.0.0.0")]
[module: UnverifiableCode]
namespace AllLargeStorage;

public class Core : MelonMod
{
	internal static bool UpgradeExisting = true;

	public override void OnInitializeMelon()
	{
		((MelonBase)this).LoggerInstance.Msg("AllLargeStorage v1.1 — 新建强制大号 + 读档升级已有 + 防还原");
	}

	public override void OnLateInitializeMelon()
	{
		//IL_0005: Unknown result type (might be due to invalid IL or missing references)
		//IL_000b: Expected Obj, but got Unknown
		//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
		//IL_00b3: Expected Obj, but got Unknown
		//IL_0110: Unknown result type (might be due to invalid IL or missing references)
		//IL_011d: Expected Obj, but got Unknown
		//IL_0167: Unknown result type (might be due to invalid IL or missing references)
		//IL_0175: Expected Obj, but got Unknown
		try
		{
			Harmony val = new Harmony("AllLargeStorage");
			int num = 0;
			Type typeFromHandle = typeof(ShipSystemDirectory);
			num += PatchFactory(val, typeFromHandle, "StorageBay");
			num += PatchFactory(val, typeFromHandle, "MakeshiftStorageBay");
			num += PatchFactory(val, typeFromHandle, "GunsmithStorageBay");
			num += PatchFactory(val, typeFromHandle, "ChemistStorageBay");
			MethodInfo methodInfo = AccessTools.Method(typeof(DirectoryMaster), "Item", new Type[2]
			{
				typeof(string),
				typeof(bool)
			}, (Type[])null);
			if (methodInfo != null)
			{
				val.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(Hooks), "DirectoryItemPrefix", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				((MelonBase)this).LoggerInstance.Msg("OK DirectoryMaster.Item → storage_bay_large 重定向");
				num++;
			}
			else
			{
				((MelonBase)this).LoggerInstance.Warning("未找到 DirectoryMaster.Item");
			}
			MethodInfo methodInfo2 = AccessTools.Method(typeof(PlayerStore), "LoadGame", (Type[])null, (Type[])null);
			if (methodInfo2 != null)
			{
				val.Patch((MethodBase)methodInfo2, (HarmonyMethod)null, new HarmonyMethod(typeof(Hooks), "AfterLoadGame", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				((MelonBase)this).LoggerInstance.Msg("OK PlayerStore.LoadGame postfix");
				num++;
			}
			MethodInfo methodInfo3 = AccessTools.Method(typeof(PlayerStore), "SaveGame", (Type[])null, (Type[])null);
			if (methodInfo3 != null)
			{
				val.Patch((MethodBase)methodInfo3, new HarmonyMethod(typeof(Hooks), "BeforeSaveGame", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				((MelonBase)this).LoggerInstance.Msg("OK PlayerStore.SaveGame prefix");
				num++;
			}
			((MelonBase)this).LoggerInstance.Msg($"补丁完成: {num}");
		}
		catch (Exception ex)
		{
			((MelonBase)this).LoggerInstance.Error(ex.ToString());
		}
	}

	private static int PatchFactory(Harmony h, Type type, string methodName)
	{
		//IL_0042: Unknown result type (might be due to invalid IL or missing references)
		//IL_0050: Expected Obj, but got Unknown
		MethodInfo methodInfo = AccessTools.Method(type, methodName, (Type[])null, (Type[])null);
		if (methodInfo == null)
		{
			MelonLogger.Warning("未找到 " + type.Name + "." + methodName);
			return 0;
		}
		h.Patch((MethodBase)methodInfo, new HarmonyMethod(typeof(Hooks), "FactoryToLarge", (Type[])null), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
		MelonLogger.Msg($"OK {type.Name}.{methodName} → StorageBayLarge");
		return 1;
	}

	public override void OnSceneWasLoaded(int buildIndex, string sceneName)
	{
		if (string.IsNullOrEmpty(sceneName) || (sceneName.IndexOf("Emporium", StringComparison.OrdinalIgnoreCase) < 0 && sceneName.IndexOf("Store", StringComparison.OrdinalIgnoreCase) < 0 && sceneName.IndexOf("Game", StringComparison.OrdinalIgnoreCase) < 0))
		{
			return;
		}
		try
		{
			Hooks.UpgradeAllStorageInPlay();
		}
		catch (Exception ex)
		{
			((MelonBase)this).LoggerInstance.Warning("Scene upgrade: " + ex.Message);
		}
	}
}
public static class Hooks
{
	private static MethodInfo _largeFactory;

	private static MethodInfo LargeFactory => _largeFactory ?? (_largeFactory = AccessTools.Method(typeof(ShipSystemDirectory), "StorageBayLarge", (Type[])null, (Type[])null));

	private static bool IsSmallStorageId(string id)
	{
		if (string.IsNullOrEmpty(id))
		{
			return false;
		}
		if (id == "storage_bay_large" || id.StartsWith("storage_bay_large", StringComparison.Ordinal))
		{
			return false;
		}
		if (!(id == "storage_bay"))
		{
			return id.StartsWith("storage_bay", StringComparison.Ordinal);
		}
		return true;
	}

	public static bool FactoryToLarge(ShipSystemDirectory __instance, ref GameItem __result)
	{
		//IL_0027: Unknown result type (might be due to invalid IL or missing references)
		//IL_002d: Expected Obj, but got Unknown
		try
		{
			if ((Object)(object)__instance == (Object)null || LargeFactory == null)
			{
				return true;
			}
			__result = (GameItem)LargeFactory.Invoke(__instance, null);
			return false;
		}
		catch (Exception ex)
		{
			MelonLogger.Error("FactoryToLarge: " + ex);
			return true;
		}
	}

	public static void DirectoryItemPrefix(ref string identifier)
	{
		if (IsSmallStorageId(identifier))
		{
			identifier = "storage_bay_large";
		}
	}

	public static void AfterLoadGame(PlayerStore __instance)
	{
		try
		{
			MelonLogger.Msg("读档完成，开始升级储藏舱…");
			int value = UpgradeAllStorageInPlay();
			MelonLogger.Msg($"读档升级完成，处理 {value} 个储藏舱");
		}
		catch (Exception ex)
		{
			MelonLogger.Error("AfterLoadGame: " + ex);
		}
	}

	public static void BeforeSaveGame(PlayerStore __instance)
	{
		try
		{
			int num = ForceLargeIdentifiersOnly();
			if (num > 0)
			{
				MelonLogger.Msg($"存档前已将 {num} 个储藏舱标记为大号");
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Error("BeforeSaveGame: " + ex);
		}
	}

	public static int ForceLargeIdentifiersOnly()
	{
		int num = 0;
		foreach (GameItem item in EnumerateAllGameItems())
		{
			if (item == null)
			{
				continue;
			}
			string text = null;
			try
			{
				text = item.identifier;
			}
			catch
			{
				continue;
			}
			if (IsSmallStorageId(text))
			{
				try
				{
					item.identifier = "storage_bay_large";
					item.spritePath = "storage_bay_large";
					num++;
				}
				catch
				{
				}
			}
		}
		return num;
	}

	public static int UpgradeAllStorageInPlay()
	{
		if (!Core.UpgradeExisting)
		{
			return 0;
		}
		int num = 0;
		foreach (GameItem item in EnumerateAllGameItems())
		{
			if (item == null)
			{
				continue;
			}
			string text = null;
			try
			{
				text = item.identifier;
			}
			catch
			{
				continue;
			}
			if (!IsSmallStorageId(text))
			{
				continue;
			}
			try
			{
				item.identifier = "storage_bay_large";
				if (!string.IsNullOrEmpty(item.spritePath) && item.spritePath.IndexOf("large", StringComparison.OrdinalIgnoreCase) < 0)
				{
					item.spritePath = "storage_bay_large";
				}
				int childCount = GetChildCount(item);
				num++;
				MelonLogger.Msg($"升级储藏舱 uniqueId={item.uniqueId} childCount={childCount} → storage_bay_large");
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("升级单个储藏舱失败: " + ex.Message);
			}
		}
		return num;
	}

	private static List<GameItem> EnumerateAllGameItems()
	{
		//IL_0046: Unknown result type (might be due to invalid IL or missing references)
		List<GameItem> result = new List<GameItem>();
		HashSet<int> seen = new HashSet<int>();
		try
		{
			Type typeFromHandle = typeof(PlayerStore);
			FieldInfo fieldInfo = AccessTools.Field(typeFromHandle, "instance");
			PlayerStore val = ((!(fieldInfo != null)) ? ((PlayerStore)null) : ((PlayerStore)fieldInfo.GetValue(null)));
			if (val != null)
			{
				FieldInfo fieldInfo2 = AccessTools.Field(typeFromHandle, "gridInv");
				if (fieldInfo2 != null)
				{
					object? value = fieldInfo2.GetValue(val);
					CollectFromInventory((GameInventory)((value is GameInventory) ? value : null), AddItem);
				}
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("Enumerate via PlayerStore: " + ex.Message);
		}
		try
		{
			Object.FindObjectsOfType<MonoBehaviour>();
		}
		catch
		{
		}
		return result;
		void AddItem(GameItem gi)
		{
			if (gi != null)
			{
				int num = 0;
				try
				{
					num = gi.uniqueId;
				}
				catch
				{
				}
				if (num == 0 || seen.Add(num))
				{
					result.Add(gi);
					try
					{
						foreach (GameItem child in GetChildren(gi))
						{
							AddItem(child);
						}
					}
					catch
					{
					}
				}
			}
		}
	}

	private static void CollectFromInventory(GameInventory inv, Action<GameItem> add)
	{
		if (inv == null)
		{
			return;
		}
		try
		{
			foreach (GameItem child in GetChildren(inv))
			{
				add(child);
			}
		}
		catch
		{
		}
	}

	private static int GetChildCount(object obj)
	{
		try
		{
			PropertyInfo propertyInfo = obj.GetType().GetProperty("childItems") ?? obj.GetType().GetProperty("ChildItems");
			if (propertyInfo == null)
			{
				return 0;
			}
			object value = propertyInfo.GetValue(obj);
			if (value == null)
			{
				return 0;
			}
			PropertyInfo property = value.GetType().GetProperty("Count");
			return (property != null) ? ((int)property.GetValue(value)) : 0;
		}
		catch
		{
			return 0;
		}
	}

	private static IEnumerable<GameItem> GetChildren(object obj)
	{
		new List<GameItem>();
		PropertyInfo propertyInfo = obj.GetType().GetProperty("childItems") ?? obj.GetType().GetProperty("ChildItems");
		if (propertyInfo == null)
		{
			yield break;
		}
		object raw = propertyInfo.GetValue(obj);
		if (raw == null)
		{
			yield break;
		}
		PropertyInfo property = raw.GetType().GetProperty("Count");
		PropertyInfo itemP = raw.GetType().GetProperty("Item");
		if (property == null)
		{
			yield break;
		}
		int n = (int)property.GetValue(raw);
		for (int i = 0; i < n; i++)
		{
			object obj2 = null;
			if (itemP != null)
			{
				obj2 = itemP.GetValue(raw, new object[1] { i });
			}
			else
			{
				MethodInfo method = raw.GetType().GetMethod("get_Item");
				if (method != null)
				{
					obj2 = method.Invoke(raw, new object[1] { i });
				}
			}
			GameItem val = (GameItem)((obj2 is GameItem) ? obj2 : null);
			if (val != null)
			{
				yield return val;
			}
		}
	}
}
