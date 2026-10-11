using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using DiscountBudgetPreview;
using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using MelonLoader.Preferences;
using Microsoft.CodeAnalysis;
using UnityEngine;

[assembly: CompilationRelaxations(8)]
[assembly: RuntimeCompatibility(WrapNonExceptionThrows = true)]
[assembly: Debuggable(DebuggableAttribute.DebuggingModes.IgnoreSymbolStoreSequencePoints)]
[assembly: AssemblyFileVersion("1.1.0.0")]
[assembly: MelonInfo(typeof(Core), "折扣预算预览", "1.1.0", "耄耋", null)]
[assembly: MelonGame("Questing Goose Studio", "Probably Stolen")]
[assembly: AssemblyVersion("1.1.0.0")]
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
namespace DiscountBudgetPreview
{
	[HarmonyPatch(typeof(BargainUIManager), "GetBargainTooltip")]
	internal static class BargainTooltipBudgetPreviewPatch
	{
		private const double DiscountBudgetFactor = 2.0;

		private const double MarkupBudgetFactor = 2.0;

		private const double MarkupBudgetMaxReduction = 0.75;

		private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

		private static void Postfix(BargainUIManager __instance, BargainType __0, int __1, ref string __result)
		{
			//IL_000e: Unknown result type (might be due to invalid IL or missing references)
			//IL_0010: Invalid comparison between Unknown and I4
			//IL_0013: Unknown result type (might be due to invalid IL or missing references)
			//IL_0015: Invalid comparison between Unknown and I4
			try
			{
				if ((Object)(object)__instance == (Object)null)
				{
					return;
				}
				bool flag = (int)__0 == 0;
				bool flag2 = (int)__0 == 1;
				if ((!flag && !flag2) || (flag && __instance.DiscountOffered) || (flag2 && __instance.MarkupOffered))
				{
					return;
				}
				StoreClient trackedClient = GetTrackedClient(__instance);
				if (trackedClient != null && trackedClient.useClientBudget)
				{
					int budget = trackedClient.GetBudget();
					if (budget > 0)
					{
						string text = (flag ? BuildDiscountLine(budget, __1) : BuildMarkupLine(budget, __1));
						__result = (string.IsNullOrEmpty(__result) ? text : (__result + "\n" + text));
						Core.Debug("预算预览：" + (flag ? "折扣 " : "抬高售价 ") + __1 + "% → " + budget);
					}
				}
			}
			catch (Exception ex)
			{
				Core.Debug("预算预览失败：" + ex.Message);
			}
		}

		private static string BuildDiscountLine(int current, int percent)
		{
			long num = Round((double)current * ((double)percent / 100.0) * 2.0);
			long value = current + num;
			MelonPreferences_Entry<bool>? useEnglishText = Core.UseEnglishText;
			if (useEnglishText != null && useEnglishText.Value)
			{
				return $"Discount {percent}%  Budget: {current} -> {value}  (+{num})";
			}
			return $"折扣 {percent}%\u3000预计新预算：{current} → {value}（+{num}）";
		}

		private static string BuildMarkupLine(int current, int percent)
		{
			long val = Round((double)current * ((double)percent / 100.0) * 2.0);
			long val2 = Round((double)current * 0.75);
			long num = Math.Min(val, val2);
			long value = current - num;
			MelonPreferences_Entry<bool>? useEnglishText = Core.UseEnglishText;
			if (useEnglishText != null && useEnglishText.Value)
			{
				return $"Markup {percent}%  Budget: {current} -> {value}  (-{num})";
			}
			return $"抬高售价 {percent}%\u3000预计新预算：{current} → {value}（-{num}）";
		}

		private static long Round(double value)
		{
			return (long)Math.Round(value, MidpointRounding.AwayFromZero);
		}

		private static StoreClient? GetTrackedClient(BargainUIManager manager)
		{
			object? obj = ((object)manager).GetType().GetProperty("trackedClient", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(manager);
			return (StoreClient?)((obj is StoreClient) ? obj : null);
		}
	}
	public sealed class Core : MelonMod
	{
		internal static Instance? Log { get; private set; }

		internal static MelonPreferences_Entry<bool>? UseEnglishText { get; private set; }

		internal static MelonPreferences_Entry<bool>? DebugLogging { get; private set; }

		public override void OnInitializeMelon()
		{
			//IL_00a5: Unknown result type (might be due to invalid IL or missing references)
			Log = ((MelonBase)this).LoggerInstance;
			MelonPreferences_Category val = MelonPreferences.CreateCategory("DiscountBudgetPreview", "折扣预算预览 / Discount Budget Preview");
			UseEnglishText = val.CreateEntry<bool>("UseEnglishText", false, "使用英文提示", "勾选后，折扣预算预览改用英文。若游戏字体不含中文字形、提示显示为方块，请打开此项。", false, false, (ValueValidator)null, (string)null);
			DebugLogging = val.CreateEntry<bool>("DebugLogging", false, "输出调试日志", "输出折扣预算预览的详细调试日志。", false, false, (ValueValidator)null, (string)null);
			((MelonBase)this).HarmonyInstance.UnpatchSelf();
			int num = 0;
			Type[] types = typeof(Core).Assembly.GetTypes();
			foreach (Type type in types)
			{
				if (type.IsClass && type.GetCustomAttributes(typeof(HarmonyPatch), inherit: false).Length != 0)
				{
					try
					{
						new PatchClassProcessor(((MelonBase)this).HarmonyInstance, type).Patch();
						num++;
					}
					catch (Exception ex)
					{
						Log.Error("补丁失败（已隔离）" + type.Name + "：" + ex.Message);
					}
				}
			}
			Log.Msg("折扣预算预览 v1.1.0 已加载，作者：耄耋。已启用补丁模块：" + num + " 个。");
		}

		internal static void Debug(string message)
		{
			MelonPreferences_Entry<bool>? debugLogging = DebugLogging;
			if (debugLogging != null && debugLogging.Value)
			{
				Instance? log = Log;
				if (log != null)
				{
					log.Msg(message);
				}
			}
		}
	}
}
