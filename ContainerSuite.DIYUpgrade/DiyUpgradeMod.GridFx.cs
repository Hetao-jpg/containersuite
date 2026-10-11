// 由拆解脚本从 自定义集装箱fix/DiyUpgradeMod.cs 机械切分，内容逐字未改。
// 职责：网格填充底与透明度修复
using ContainerSuite.Shared;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using MelonLoader.Preferences;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    internal static void SetShapeKeepTransparency(GameGridInventory grid, int w, int h)
    {
        if (grid == null) return;
        float before = 0f;
        // 读取原透明度只为原样还原；读失败用 0 继续，无害。
        try { before = grid.transparency; } catch { }
        CrashProbe.Enter("GridFx.SetShape(" + w + "x" + h + ")");
        grid.SetShape(w, h);
        CrashProbe.Exit("GridFx.SetShape");
        CrashProbe.Enter("GridFx.RestoreTransparency");
        GridFx.RestoreTransparency(grid, before);
        CrashProbe.Exit("GridFx.RestoreTransparency");
        CrashProbe.Enter("GridFx.RebuildFill");
        RebuildGridFill(grid, before);
        CrashProbe.Exit("GridFx.RebuildFill");
    }
    internal static void RebuildGridFill(GameGridInventory g, float transparency)
    {
        if (g == null) return;

        // 拖拽中跳过重节点销毁+重建；形状变化由 SetShape + RectTransform 带动。
        // 【2026-10-10 18:05】拖拽中不调 g.Validate() —— 对照小窝源码定案：小窝缩放只有
        //   SetShape + ForceRecomputeDynamicBackground，从不调 Validate，实机无乱格；
        //   而我们多了 g.Validate()，快速拖拽时它逐帧叠加中间宽/高的节点
        //   （diag perRow 实测 210，干净仅 60~80）→ 界内重复 = 乱格。元凶就是它，已移除。
        //   重建交给 TryResizeGridWindow 里的 ForceRecomputeDynamicBackground（小窝同款）。
        //   HideOutOfBoundsNodes 保留作廉价兜底（只 SetActive，不重建，无累积风险）。
        if (ContainerWindowResizer.IsDragging)
        {
            try { GridFx.HideOutOfBoundsNodes(g); } catch { }
            return;
        }

        try
        {
            GridFx.DestroyNodeList(g.dynamicBackgrounds);
            GridFx.DestroyNodeList(g.dynamicR0);
            GridFx.DestroyNodeList(g.dynamicR1);
            GridFx.DestroyNodeList(g.dynamicR2);
            GridFx.DestroyNodeList(g.dynamicR3);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:clear-nodes", ex); }

        try { g.renderDynamicBackground = true; } catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:fill-on", ex); }
        try { g.Validate(); } catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:fill-v1", ex); }
        try { g.ValidateBackground(); } catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:fill-vbg", ex); }

        if (!FillHealthy(g))
        {
            // 兜底「关-开」重刷序列：属尽力而为，任一步失败都继续，无害，故不逐条留痕。
            try { g.renderDynamicBackground = false; } catch { }
            try { g.Validate(); } catch { }
            try { g.renderDynamicBackground = true; } catch { }
            try { g.Validate(); } catch { }
            try { g.ValidateBackground(); } catch { }
        }

        try { g.ForceRecomputeDynamicBackground(); } catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:fill-fr", ex); }

        GridFx.ApplyMeshAlpha(g, Mathf.Clamp01(1f - transparency));
    }
    // P1（2026-10-02）：实现搬到 ContainerSuite.Shared.GridFx.FillHealthyByCount，
    // 这里保留同名薄转发，避免改动其它 partial 文件里的调用点。
    internal static bool FillHealthy(GameGridInventory g) => GridFx.FillHealthyByCount(g);

    internal static void RefreshAllGridsOf(GameItem item)
    {
        if (item == null) return;
        try
        {
            List<GameGridInventory> grids = GetAllGridInventories(item);
            if (grids == null) return;
            for (int i = 0; i < grids.Count; i++)
            {
                GameGridInventory g = grids[i];
                if (g?.inventoryShape == null) continue;
                float trans = 0f;
                // 读透明度只为还原，读失败用 0 继续，无害。
                try { trans = g.transparency; } catch { }
                bool healthy = false;
                // 健康判定失败按「不健康」走重建分支，无害。
                try { healthy = FillHealthy(g); } catch { }
                // 【2026-10-10 17:30 回滚】曾在此加 forceRebuild（收尾强制销毁重建）+
                //   RestoreHiddenNodes（点亮全部隐藏节点）。实机出现「幽灵网格」：
                //   游戏对列表里的节点 active 态有自己的管理（池化/刻意隐藏），
                //   强行点亮/反复全拆全建都会放出自不该显示的格子。
                //   「数量健康即跳过重建」是原经过实机验证的行为，恢复原状。
                try { if (!healthy) RebuildGridFill(g, trans); else GridFx.ApplyMeshAlpha(g, Mathf.Clamp01(1f - trans)); }
                catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:ref-fill", ex); }
                if (!ContainerWindowResizer.IsDragging)
                {
                    try
                    {
                        Component component = ((Il2CppObjectBase)g).TryCast<Component>();
                        if (component?.gameObject != null)
                        {
                            var graphics = component.gameObject.GetComponentsInChildren<Graphic>(true);
                            if (graphics != null)
                                for (int k = 0; k < graphics.Count; k++)
                                    if (graphics[k] != null) graphics[k].SetAllDirty();
                        }
                    }
                    catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:ref-dirty", ex); }
                }
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("GridFx:ref-all", ex); }
    }
}
