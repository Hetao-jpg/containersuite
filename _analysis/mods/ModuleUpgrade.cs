using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using MelonLoader;
using MelonLoader.Preferences;
using ModuleUpgrade;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: MelonInfo(typeof(MergeMod), "Module upgrade", "1.2.0", "xiaobai233", null)]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = "")]
[assembly: AssemblyCompany("ModuleUpgrade")]
[assembly: AssemblyConfiguration("Release")]
[assembly: AssemblyFileVersion("1.0.0.0")]
[assembly: AssemblyInformationalVersion("1.0.0")]
[assembly: AssemblyProduct("ModuleUpgrade")]
[assembly: AssemblyTitle("ModuleUpgrade")]
[assembly: AssemblyVersion("1.0.0.0")]
namespace ModuleUpgrade;

public class MergeMod : MelonMod
{
	internal const int DefaultMaxLevel = 6;

	internal const int AbsoluteMaxLevel = 16;

	internal static MelonPreferences_Category Category;

	internal static MelonPreferences_Entry<int> MaxLevelEntry;

	internal static string SuccessSoundId;

	internal static bool SoundSearched;

	internal static int MaxLevel
	{
		get
		{
			int num;
			try
			{
				num = ((MaxLevelEntry != null) ? MaxLevelEntry.Value : 6);
			}
			catch
			{
				num = 6;
			}
			if (num < 0)
			{
				num = 0;
			}
			if (num > 16)
			{
				num = 16;
			}
			return num;
		}
	}

	public override void OnInitializeMelon()
	{
		try
		{
			Category = MelonPreferences.CreateCategory("ModuleUpgrade", "Module upgrade");
			MaxLevelEntry = Category.CreateEntry<int>("MaxLevel", 6, "最高合成等级：0=禁用合成，1=2x，2=4x，3=8x … 16=65536x。越界值会自动夹回 0~16。", (string)null, false, false, (ValueValidator)null, (string)null);
			((MelonBase)this).HarmonyInstance.PatchAll(typeof(EndDragPatch));
			int maxLevel = MaxLevel;
			string text = ((maxLevel <= 0) ? "合成已禁用（MaxLevel=0）" : $"最高 {maxLevel} 级（{1 << maxLevel}x，可在 MelonPreferences 里改 MaxLevel，范围 0~{16}）");
			MelonLogger.Msg("已加载 v1.2.0，EndDrag 补丁已挂载。同款同形状【同单价】的模组才能合成：数值与价格 ×2，" + text);
		}
		catch (Exception ex)
		{
			MelonLogger.Error("补丁挂载失败：" + ex);
		}
	}

	internal static void PlaySuccessSound()
	{
		try
		{
			if (!SoundSearched)
			{
				SoundSearched = true;
				SuccessSoundId = FindSuccessSound();
				if (SuccessSoundId != null)
				{
					MelonLogger.Msg("已选定合成音效：" + SuccessSoundId);
				}
				else
				{
					MelonLogger.Warning("未找到合适的合成音效，声音提示将静默跳过。");
				}
			}
			if (SuccessSoundId != null && (Object)(object)AudioManager.Instance != (Object)null)
			{
				AudioManager.Instance.Play(SuccessSoundId);
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("播放合成音效失败：" + ex.Message);
		}
	}

	private static string FindSuccessSound()
	{
		try
		{
			AudioDirectory activeDirectory = AudioDirectory.activeDirectory;
			if ((Object)(object)activeDirectory == (Object)null)
			{
				return null;
			}
			string[] keywords = new string[10] { "coin", "cash", "bell", "success", "upgrade", "money", "chime", "ding", "buy", "sell" };
			if (SearchArray(activeDirectory.audioAsset, keywords) != null)
			{
				return SearchArray(activeDirectory.audioAsset, keywords);
			}
			if (SearchArray(activeDirectory.audioAsset2, keywords) != null)
			{
				return SearchArray(activeDirectory.audioAsset2, keywords);
			}
			if (SearchArray(activeDirectory.audioAsset3, keywords) != null)
			{
				return SearchArray(activeDirectory.audioAsset3, keywords);
			}
			if (SearchArray(activeDirectory.music, keywords) != null)
			{
				return SearchArray(activeDirectory.music, keywords);
			}
			if (SearchArray(activeDirectory.item1, keywords) != null)
			{
				return SearchArray(activeDirectory.item1, keywords);
			}
			if (SearchArray(activeDirectory.item2, keywords) != null)
			{
				return SearchArray(activeDirectory.item2, keywords);
			}
		}
		catch (Exception ex)
		{
			MelonLogger.Warning("查找合成音效失败：" + ex.Message);
		}
		return null;
	}

	private static string SearchArray(Il2CppReferenceArray<AudioAsset> arr, string[] keywords)
	{
		if (arr == null)
		{
			return null;
		}
		foreach (AudioAsset item in (Il2CppArrayBase<AudioAsset>)(object)arr)
		{
			if (item == null)
			{
				continue;
			}
			string identifier = item.identifier;
			if (string.IsNullOrEmpty(identifier))
			{
				continue;
			}
			string text = identifier.ToLowerInvariant();
			foreach (string value in keywords)
			{
				if (text.Contains(value))
				{
					return identifier;
				}
			}
		}
		return null;
	}
}
[HarmonyPatch(typeof(ItemMouseDragHandler), "EndDrag")]
internal static class EndDragPatch
{
	private static bool Prefix(ItemMouseDragHandler __instance)
	{
		try
		{
			int maxLevel = MergeMod.MaxLevel;
			if (maxLevel <= 0)
			{
				return true;
			}
			GameItem currentItem = __instance.GetCurrentItem();
			GameItem lastItem = __instance.lastItem;
			if (currentItem == null || lastItem == null)
			{
				return true;
			}
			if (currentItem == lastItem)
			{
				return true;
			}
			if (!IsModule(currentItem) || !IsModule(lastItem))
			{
				return true;
			}
			if (!IsSameModule(currentItem, lastItem))
			{
				return true;
			}
			if (currentItem.unitValue != lastItem.unitValue)
			{
				return true;
			}
			int mergeLevel = GetMergeLevel(currentItem);
			if (mergeLevel >= maxLevel)
			{
				MelonLogger.Warning("「" + currentItem.identifier + "」已达设置的最高 " + maxLevel + " 级（" + (1 << maxLevel) + "x），不能再合成。");
				return true;
			}
			long unitValue = currentItem.unitValue;
			float num = 1 << mergeLevel + 1;
			ModuleHelper.SetBaseStatMult(currentItem, num, num);
			long unitValue2 = currentItem.unitValue;
			MelonLogger.Msg("合成成功：" + currentItem.identifier + " 等级 " + mergeLevel + "→" + (mergeLevel + 1) + "，单价 " + unitValue + "→" + unitValue2 + "（基础单价 " + currentItem.unitBaseValue + "）。");
			lastItem.Destroy();
			MergeMod.PlaySuccessSound();
			return false;
		}
		catch (Exception ex)
		{
			MelonLogger.Error("合成异常：" + ex);
			return true;
		}
	}

	private static bool IsModule(GameItem item)
	{
		string identifier = item.identifier;
		if (!string.IsNullOrEmpty(identifier))
		{
			return identifier.ToLowerInvariant().Contains("module");
		}
		return false;
	}

	private static bool IsSameModule(GameItem a, GameItem b)
	{
		if (a.identifier != b.identifier)
		{
			return false;
		}
		GridShape val = a.modifiedShape ?? a.shape;
		GridShape val2 = b.modifiedShape ?? b.shape;
		if (val == null || val2 == null)
		{
			return false;
		}
		if (val.width != val2.width)
		{
			return false;
		}
		if (val.height != val2.height)
		{
			return false;
		}
		if (val.flipped != val2.flipped)
		{
			return false;
		}
		if (val.orientation != val2.orientation)
		{
			return false;
		}
		return true;
	}

	private static int GetMergeLevel(GameItem item)
	{
		long unitBaseValue = item.unitBaseValue;
		long unitValue = item.unitValue;
		if (unitBaseValue <= 0 || unitValue <= 0)
		{
			return 0;
		}
		int num = (int)Math.Round(Math.Log((double)unitValue / (double)unitBaseValue) / Math.Log(2.0));
		if (num < 0)
		{
			num = 0;
		}
		return num;
	}
}
