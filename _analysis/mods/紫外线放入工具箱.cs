using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Text;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UVToolbox;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.Default | DebuggableAttribute.DebuggingModes.DisableOptimizations | DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints | DebuggableAttribute.DebuggingModes.EnableEditAndContinue)]
[assembly: MelonInfo(typeof(UVToolboxMod), "[HungoPatch]Put UV Back to ToolBox", "1.0.0", "Hungo1998520", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: TargetFramework(".NETCoreApp,Version=v6.0", FrameworkDisplayName = ".NET 6.0")]
[assembly: AssemblyCompany("[HungoPatch]Put UV Back to ToolBox")]
[assembly: AssemblyConfiguration("Debug")]
[assembly: AssemblyFileVersion("0.1.0.0")]
[assembly: AssemblyInformationalVersion("0.1.0")]
[assembly: AssemblyProduct("[HungoPatch]Put UV Back to ToolBox")]
[assembly: AssemblyTitle("[HungoPatch]Put UV Back to ToolBox")]
[assembly: AssemblyVersion("0.1.0.0")]
namespace UVToolbox;

[HarmonyPatch(typeof(FoodItemDirectory), "InitDirectory")]
internal static class FoodItemDirectoryInitDirectoryPatch
{
	private static void Postfix()
	{
		LazyPatches.Ensure();
	}
}
internal static class LazyPatches
{
	private static bool _installed;

	public static void Ensure()
	{
		//IL_00c7: Unknown result type (might be due to invalid IL or missing references)
		//IL_00d4: Expected Obj, but got Unknown
		//IL_0142: Unknown result type (might be due to invalid IL or missing references)
		//IL_014f: Expected Obj, but got Unknown
		if (_installed)
		{
			return;
		}
		_installed = true;
		try
		{
			Harmony val = ((UVToolboxMod.Instance == null) ? null : ((MelonBase)UVToolboxMod.Instance).HarmonyInstance);
			if (val == null)
			{
				_installed = false;
				Log.Warn("惰性补丁：拿不到 Harmony 实例，补丁未安装（本次游戏里紫外线灯仍放不进工具箱）");
				return;
			}
			MethodInfo methodInfo = AccessTools.Method(typeof(DirectoryMaster), "Item", new Type[2]
			{
				typeof(string),
				typeof(bool)
			}, (Type[])null);
			MethodInfo methodInfo2 = AccessTools.Method(typeof(LazyPatches), "ItemPostfix", (Type[])null, (Type[])null);
			if (methodInfo == null || methodInfo2 == null)
			{
				Log.Error("惰性补丁：DirectoryMaster.Item 目标方法找不到（游戏更新可能改了签名），补 tag 未安装 —— 请把这份日志反馈");
			}
			else
			{
				val.Patch((MethodBase)methodInfo, (HarmonyMethod)null, new HarmonyMethod(methodInfo2), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
				Log.Info("补丁已安装：DirectoryMaster.Item → 紫外线灯补 TOOLBOX_ITEM_TAG");
			}
			MethodInfo methodInfo3 = AccessTools.Method(typeof(PlayerStore), "LoadGame", (Type[])null, (Type[])null);
			MethodInfo methodInfo4 = AccessTools.Method(typeof(LazyPatches), "LoadGamePostfix", (Type[])null, (Type[])null);
			if (methodInfo3 == null || methodInfo4 == null)
			{
				Log.Warn("惰性补丁：PlayerStore.LoadGame 目标方法找不到，读档自愈未安装（主路补 tag 不受影响）");
				return;
			}
			val.Patch((MethodBase)methodInfo3, (HarmonyMethod)null, new HarmonyMethod(methodInfo4), (HarmonyMethod)null, (HarmonyMethod)null, (HarmonyMethod)null);
			Log.Info("补丁已安装：PlayerStore.LoadGame → 读档后自愈扫一遍紫外线灯");
		}
		catch (Exception ex)
		{
			_installed = false;
			Log.Exception(ex, "惰性补丁安装失败");
		}
	}

	private static void ItemPostfix(GameItem __result)
	{
		try
		{
			ToolboxCompat.TagIfUvLamp(__result);
		}
		catch (Exception ex)
		{
			Log.Exception(ex, "补 tag 过程中抛异常（已吞，不影响物品创建）");
		}
	}

	private static void LoadGamePostfix()
	{
		try
		{
			ToolboxCompat.RepairExisting();
		}
		catch (Exception ex)
		{
			Log.Exception(ex, "读档自愈抛异常（已吞，主路补 tag 不受影响）");
		}
	}
}
internal static class Log
{
	private const string Prefix = "[UVToolbox]";

	private const string FileName = "UVToolbox.log";

	private const string PrevName = "UVToolbox.prev.log";

	private const int MaxLines = 4000;

	private const int KeepLines = 2000;

	private static readonly object _lock = new object();

	private static readonly List<string> _pending = new List<string>();

	private static string _filePath;

	private static bool _fileReady;

	private static int _lines;

	public static bool Verbose = false;

	public static void Init()
	{
		_filePath = ResolveLogPath();
		try
		{
			string directoryName = Path.GetDirectoryName(_filePath);
			if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
			{
				Directory.CreateDirectory(directoryName);
			}
			try
			{
				if (File.Exists(_filePath))
				{
					string destFileName = Path.Combine(string.IsNullOrEmpty(directoryName) ? "." : directoryName, "UVToolbox.prev.log");
					File.Copy(_filePath, destFileName, overwrite: true);
				}
			}
			catch (Exception ex)
			{
				MelonLogger.Warning("[UVToolbox] 备份上一场日志失败（不影响新日志）: " + ex.Message);
			}
			File.WriteAllText(_filePath, "# UVToolbox 日志（上一场见同目录 UVToolbox.prev.log）" + Environment.NewLine + "# 本 MOD 只做一件事：给紫外线灯补 TOOLBOX_ITEM_TAG，让它能放进工具箱。" + Environment.NewLine + "# 关键行：启动行 / 「补丁已安装」/「紫外线灯已补 tag」。行格式：时间 [级别] 内容" + Environment.NewLine, Encoding.UTF8);
			_lines = 3;
			_fileReady = true;
		}
		catch (Exception ex2)
		{
			_fileReady = false;
			MelonLogger.Warning("[UVToolbox] 无法创建日志文件，将只输出到 MelonLoader 日志: " + ex2.Message);
		}
	}

	private static string ResolveLogPath()
	{
		try
		{
			string dataPath = Application.dataPath;
			if (!string.IsNullOrEmpty(dataPath))
			{
				string directoryName = Path.GetDirectoryName(dataPath);
				if (!string.IsNullOrEmpty(directoryName))
				{
					return Path.Combine(directoryName, "MelonLoader", "UVToolbox.log");
				}
			}
		}
		catch
		{
		}
		try
		{
			return Path.Combine(Path.GetTempPath(), "UVToolbox.log");
		}
		catch
		{
			return "UVToolbox.log";
		}
	}

	private static void Emit(string level, string message)
	{
		string text = DateTime.Now.ToString("HH:mm:ss.fff") + " [" + level + "] " + message;
		lock (_lock)
		{
			if (level == "ERROR")
			{
				MelonLogger.Error("[UVToolbox] " + message);
			}
			else if (level == "WARN")
			{
				MelonLogger.Warning("[UVToolbox] " + message);
			}
			if (!_fileReady)
			{
				_pending.Add(text);
				return;
			}
			try
			{
				File.AppendAllText(_filePath, text + Environment.NewLine, Encoding.UTF8);
				_lines++;
				RollIfNeeded();
			}
			catch
			{
				_pending.Add(text);
			}
		}
	}

	private static void RollIfNeeded()
	{
		if (_lines <= 4000)
		{
			return;
		}
		try
		{
			string[] array = File.ReadAllLines(_filePath, Encoding.UTF8);
			int num = ((array.Length < 2000) ? array.Length : 2000);
			string[] array2 = new string[num + 1];
			array2[0] = "# [日志滚动] 超过 " + 4000 + " 行，只保留最新 " + num + " 行（最新现场优先）";
			Array.Copy(array, array.Length - num, array2, 1, num);
			File.WriteAllLines(_filePath, array2, Encoding.UTF8);
			_lines = array2.Length;
		}
		catch
		{
		}
	}

	public static void Info(string message)
	{
		Emit("INFO", message);
	}

	public static void Warn(string message)
	{
		Emit("WARN", message);
	}

	public static void Error(string message)
	{
		Emit("ERROR", message);
	}

	public static void Debug(string message)
	{
		if (Verbose)
		{
			Emit("DEBUG", message);
		}
	}

	public static void Exception(Exception ex, string context)
	{
		if (ex == null)
		{
			Emit("ERROR", context + " : <null exception>");
			return;
		}
		Emit("ERROR", context + " : " + ex.GetType().Name + " - " + ex.Message);
		Exception innerException = ex.InnerException;
		int num = 1;
		while (innerException != null && num <= 6)
		{
			Emit("ERROR", "   inner[" + num + "] " + innerException.GetType().Name + " - " + innerException.Message);
			innerException = innerException.InnerException;
			num++;
		}
		if (!string.IsNullOrEmpty(ex.StackTrace))
		{
			string[] array = ex.StackTrace.Split('\n');
			int num2 = ((array.Length < 12) ? array.Length : 12);
			for (int i = 0; i < num2; i++)
			{
				Emit("ERROR", "   " + array[i].Trim());
			}
		}
	}

	public static void Flush()
	{
		lock (_lock)
		{
			if (_pending.Count == 0)
			{
				return;
			}
			try
			{
				if (!_fileReady)
				{
					string directoryName = Path.GetDirectoryName(_filePath);
					if (!string.IsNullOrEmpty(directoryName) && !Directory.Exists(directoryName))
					{
						Directory.CreateDirectory(directoryName);
					}
					File.AppendAllText(_filePath, string.Empty, Encoding.UTF8);
					_fileReady = true;
				}
				File.AppendAllLines(_filePath, _pending, Encoding.UTF8);
				_lines += _pending.Count;
				_pending.Clear();
				RollIfNeeded();
			}
			catch
			{
			}
		}
	}
}
internal static class ToolboxCompat
{
	private static int _taggedThisSession;

	private static bool _seenAlreadyTagged;

	public static bool TagIfUvLamp(GameItem item)
	{
		if (item == null)
		{
			return false;
		}
		string identifier;
		try
		{
			identifier = item.identifier;
		}
		catch (Exception ex)
		{
			Log.Debug("读 identifier 失败，跳过补 tag：" + ex.GetType().Name);
			return false;
		}
		if (!string.Equals(identifier, "uv_filter", StringComparison.Ordinal))
		{
			return false;
		}
		bool flag;
		try
		{
			flag = item.IsTag("TOOLBOX_ITEM_TAG");
		}
		catch (Exception ex2)
		{
			Log.Exception(ex2, "读 tag 失败（uv_filter）");
			return false;
		}
		if (flag)
		{
			if (!_seenAlreadyTagged)
			{
				_seenAlreadyTagged = true;
				Log.Debug("紫外线灯已带 TOOLBOX_ITEM_TAG（无需再补）");
			}
			return true;
		}
		bool flag2;
		try
		{
			flag2 = item.EnableTag("TOOLBOX_ITEM_TAG", false);
		}
		catch (Exception ex3)
		{
			Log.Exception(ex3, "补 tag 抛异常（uv_filter ← TOOLBOX_ITEM_TAG）");
			return false;
		}
		if (!flag2)
		{
			Log.Warn("紫外线灯补 TOOLBOX_ITEM_TAG 未成功（EnableTag 返回 false）—— 它可能仍然放不进工具箱，请把这份日志反馈");
			return false;
		}
		_taggedThisSession++;
		bool flag3 = false;
		try
		{
			flag3 = item.IsTag("TOOLBOX_ITEM_TAG");
		}
		catch
		{
		}
		Log.Info("紫外线灯已补 TOOLBOX_ITEM_TAG（本场第 " + _taggedThisSession + " 只；回读 IsTag=" + flag3 + "）→ 现在可以放进工具箱");
		if (!flag3)
		{
			Log.Warn("补 tag 后回读仍是 False，说明补的 tag 没落在这个物品实例上，请把这份日志反馈");
		}
		return true;
	}

	public static void RepairExisting()
	{
		try
		{
			PlayerStore instance = PlayerStore.Instance;
			if (instance == null)
			{
				Log.Debug("读档自愈：拿不到 PlayerStore，跳过（不影响下一次物品创建时补 tag）");
				return;
			}
			List<GameItem> val = instance.FindAllItem(true);
			if (val == null)
			{
				Log.Debug("读档自愈：FindAllItem 返回 null，跳过");
				return;
			}
			int count = val.Count;
			int num = 0;
			for (int i = 0; i < count; i++)
			{
				GameItem val2;
				try
				{
					val2 = val[i];
				}
				catch (Exception ex)
				{
					Log.Debug("读档自愈：取第 " + i + " 件物品失败，跳过：" + ex.GetType().Name);
					continue;
				}
				if (val2 == null)
				{
					continue;
				}
				string identifier;
				try
				{
					identifier = val2.identifier;
				}
				catch
				{
					continue;
				}
				if (string.Equals(identifier, "uv_filter", StringComparison.Ordinal))
				{
					bool flag;
					try
					{
						flag = val2.IsTag("TOOLBOX_ITEM_TAG");
					}
					catch
					{
						continue;
					}
					if (!flag && TagIfUvLamp(val2))
					{
						num++;
					}
				}
			}
			Log.Info("读档自愈：扫描 " + count + " 件物品，补 tag " + num + " 只紫外线灯");
		}
		catch (Exception ex2)
		{
			Log.Warn("读档自愈扫描失败（不影响主路补 tag）：" + ex2.GetType().Name + " - " + ex2.Message);
		}
	}
}
internal static class UVToolboxConfig
{
	public const string ModVersion = "1.0.0";

	public const string UvItemId = "uv_filter";

	public const string ToolboxItemTag = "TOOLBOX_ITEM_TAG";
}
public class UVToolboxMod : MelonMod
{
	public static UVToolboxMod Instance { get; private set; }

	public override void OnInitializeMelon()
	{
		Instance = this;
		Log.Init();
		Log.Info("UVToolbox 启动 v1.0.0 · 构建 " + BuildStamp());
		Log.Info("作用：给「紫外线灯」(uv_filter) 补 TOOLBOX_ITEM_TAG，让它能放进「工具箱」。等下一行「补丁已安装」出现才算武装完成");
		Log.Info("环境：Mods 目录里会被加载的 dll = " + ModsList());
		try
		{
			((MelonBase)this).HarmonyInstance.PatchAll();
		}
		catch (Exception ex)
		{
			MelonLogger.Error("[UVToolbox] 补丁挂载失败：" + ex);
			Log.Exception(ex, "补丁挂载失败");
		}
	}

	private static string BuildStamp()
	{
		try
		{
			return File.GetLastWriteTime(typeof(UVToolboxMod).Assembly.Location).ToString("yyyy-MM-dd HH:mm");
		}
		catch
		{
			return "?";
		}
	}

	private static string ModsList()
	{
		try
		{
			string directoryName = Path.GetDirectoryName(Application.dataPath);
			if (string.IsNullOrEmpty(directoryName))
			{
				return "?";
			}
			string path = Path.Combine(directoryName, "Mods");
			if (!Directory.Exists(path))
			{
				return "?（没有 Mods 目录）";
			}
			string[] files = Directory.GetFiles(path, "*.dll");
			if (files.Length == 0)
			{
				return "（无）";
			}
			for (int i = 0; i < files.Length; i++)
			{
				files[i] = Path.GetFileName(files[i]);
			}
			Array.Sort(files, StringComparer.OrdinalIgnoreCase);
			return string.Join("、", files) + "（共 " + files.Length + " 个）";
		}
		catch
		{
			return "?";
		}
	}

	public override void OnDeinitializeMelon()
	{
		Log.Flush();
		Instance = null;
	}
}
