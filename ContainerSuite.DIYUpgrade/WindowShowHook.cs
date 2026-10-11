
using ContainerSuite.Shared;
using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using UnityEngine;

namespace DIYUpgrade;

internal static class WindowShowHook
{
    private static bool _installed;

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed) return;
        _installed = true;
        try
        {
            Type type = typeof(PixelWindow);
            if (type == null)
            {
                MelonLogger.Warning("找不到 PixelWindow，跳过 Show Hook");
                return;
            }
            MethodInfo postfix = typeof(WindowShowHook).GetMethod(
                nameof(ShowPostfix),
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (postfix == null)
            {
                MelonLogger.Warning("找不到 ShowPostfix，跳过 Show Hook");
                return;
            }

            // 【2026-09-27 P0-6】动态 Hook 所有 Show 重载。
            int hooked = 0;
            var methods = AccessTools.GetDeclaredMethods(type);
            if (methods != null)
            {
                foreach (MethodInfo m in methods)
                {
                    if (m == null || m.Name != "Show") continue;
                    try
                    {
                        harmony.Patch(m, null, new HarmonyMethod(postfix), null, null, null);
                        hooked++;
                    }
                    catch (Exception ex)
                    {
                        MelonLogger.Warning("Hook PixelWindow.Show(" + ParamSig(m) + ") 失败：" + ex.Message);
                    }
                }
            }

            if (hooked == 0)
                MelonLogger.Warning("未找到任何 PixelWindow.Show 重载，恢复会失效");
            else
            {
                // 【2026-10-10 方案 A】手动注册 → 登记进预热表（见 WarmupRegistry 注释）。
                //   ShowPostfix 已被拆成"极薄外壳"（首行 `if (__instance==null) return`），
                //   预热用 null invoke 会立即早退，不会误触协程。
                ContainerSuite.Shared.WarmupRegistry.Register(postfix);
                MelonLogger.Msg("PixelWindow.Show Hook 完成 (" + hooked + " 个重载)");
            }
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("WindowShowHook.Install 异常：" + ex.Message);
        }
    }

    private static string ParamSig(MethodInfo m)
    {
        try
        {
            var parameters = m.GetParameters();
            if (parameters == null || parameters.Length == 0) return "";
            string sig = "";
            for (int i = 0; i < parameters.Length; i++)
            {
                if (i > 0) sig += ",";
                sig += parameters[i].ParameterType?.Name ?? "?";
            }
            return sig;
        }
        catch (Exception ex) { CatchLogOnce.Warn("WindowShowHook.ParamSig", ex); return "?"; }
    }

    // 【2026-09-27 P0-6】只保留一个 Postfix —— 所有 Show 重载共用，不依赖参数
    //
    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 方案 A】拆成"极薄外壳 + 真正体"：
    //   本 Postfix 挂在 **PixelWindow.Show 的所有重载**上（Show 有 ~72 个调用点，
    //   属超热路径）。此前它无 [HarmonyPatch] 类特性、是手动注册的 →
    //   WarmupAllPatchMethods 看不到它 → 首次 JIT 只能落在真实 Show 的 detour 深栈上
    //   → CompileMethodHook 崩（0x80131506）。
    //   但它**不能**直接拿去预热：原实现首行就 ScheduleDelayedRestore → MelonCoroutines.Start，
    //   用哑参（win=null）invoke 会……其实 win==null 已能早退，但保险起见把"起协程"下沉到
    //   独立方法 ScheduleDelayedRestore —— 外壳只做 null 判断 + 调用，预热时 win=null 即早退，
    //   零副作用，同时外壳本身被编译（detour 目标就是外壳，满足预热目的）。
    // ═══════════════════════════════════════════════════════════════════
    public static void ShowPostfix(PixelWindow __instance)
    {
        if (__instance == null) return;   // 预热（哑参 null）即从此早退，无副作用
        ScheduleDelayedRestore(__instance, "Show");
    }

    private static void ScheduleDelayedRestore(PixelWindow win, string src)
    {
        if (win == null) return;
        try { MelonCoroutines.Start(DelayedRestore(win, src)); }
        catch (Exception ex) { MelonLogger.Warning("ScheduleDelayedRestore 异常：" + ex.Message); }
    }

    private static IEnumerator DelayedRestore(PixelWindow win, string src)
    {
        yield return null;
        yield return null;
        if (win == null) yield break;

        // ── 2026-10-07：并入 CI 过去单独挂的「展示即扩展」（补丁去重）──
        //   过去 CI 在 PixelWindow.Show 的两个重载上各挂一个 Postfix，
        //   与此处的 WindowShowHook 撞成一对 → 每次显示起 2 条协程
        //   （Show() 有 72 个调用点，属热路径）。现在统一由本协程完成：
        //   ① 先扩展全部 parentItems（CI 语义，forceRefresh: true）；
        //   ② 再请求恢复首个可扩容容器（DIY 语义）。
        //   顺序与历史一致（CI 的 Postfix 注册在前、先跑）。
        //   ContainerExpander 属 CI 命名空间，但同属一个已合并的 DLL，直接调。
        Il2CppSystem.Collections.Generic.List<GameItem> parentItems = null;
        try { parentItems = win.parentItems; }
        catch (Exception ex) { MelonLogger.Warning("取 parentItems 失败：" + ex.Message); }

        if (parentItems != null && parentItems.Count > 0)
        {
            for (int k = 0; k < parentItems.Count; k++)
            {
                try
                {
                    GameItem item = parentItems[k];
                    if (item != null) CustomInfrastructure.ContainerExpander.TryExpand(item, forceRefresh: true);
                }
                catch (Exception ex) { MelonLogger.Warning("Show 后扩展异常：" + ex.Message); }
            }
        }

        GameItem targetItem = null;
        if (parentItems != null && parentItems.Count > 0)
        {
            for (int i = 0; i < parentItems.Count; i++)
            {
                GameItem candidate = parentItems[i];
                if (candidate != null && DiyUpgradeMod.IsResizableContainer(candidate))
                {
                    targetItem = candidate;
                    break;
                }
            }
        }
        if (targetItem == null) yield break;
        try
        {
            DiyUpgradeMod.ClearReapplyCache(targetItem);  
// 清缓存，让 Show 时能立即重新恢复
 // ═══════════════════════════════════════════════════════════════
// 2026-09-29：加载期不直接调 ReapplyFromMeta —— 改为入队，等
 // WorldReadyGate 打开时统一 flush。避免与 DelayedRestore 抢同一
 // 个 GameGridInventory 的 SetShape 造成半改状态。
// ═══════════════════════════════════════════════════════════════

            RestoreArbiter.Request(targetItem, "Show");
        }
        catch (Exception ex)
        {
            MelonLogger.Warning("Show 后恢复 " + DiyUpgradeMod.IdOf(targetItem) + " 异常：" + ex.Message);
        }
    }
}