// P1（2026-10-02）：从 SaveBagExpand / DIYUpgrade 两边重复的网格处理代码收拢到共享库。
//
// 背景：S4 三合一时只合并了入口，网格处理代码在 SBE 和 DIY 各写了一份，
//   且已经开始分叉（SBE 的 ApplyMeshAlpha 多了个跳过检查，DIY 没有）。
//   跨 DLL 修同一个 bug 要改两处 —— 这是本次抽取的动机。
//
// 抽取范围与取舍：
//   · MeshList / ApplyMeshAlpha / RestoreTransparency / DestroyNodeList /
//     HideOutOfBoundsNodes / RowStats / RowCoverage —— 逐字相同或可安全统一，直接抽
//   · ApplyMeshAlpha —— 采用 DIY 版（无条件写 + 逐节点 SetAllDirty），
//     理由见 README 禁止事项 #10：SBE 版多的 Mathf.Abs(c.a - alpha) < 0.001f
//     跳过属历史遗留偏差，非设计意图
//   · FillHealthy —— 两套策略本质不同，都有实机验证，不强行统一：
//       SBE 用逐行覆盖分析（min*5 >= max*3）→ FillHealthyByRow
//       DIY 用节点数差（diff*20 <= expectedBg）→ FillHealthyByCount
//   · RebuildGridFill / SetShapeKeepTransparency —— 内部依赖各家的节点复用/销毁策略，
//     不抽，留在本地（GridFx 只提供可复用的原子操作）
//
// 命名以 ByRow / ByCount 结尾，明确区分两套判据。
//
// S8s（2026-10-05）二次评估：以下函数**确认不抽**（设计差异，非重复）：
//   · RebuildGridFill —— SBE 是"主动重建"（自己补齐每个节点），
//     DIY 是"委托重建"（清空后交给游戏 renderDynamicBackground）。
//     强行统一 = 一个函数里塞两套 if/else，比现状更难维护。
//   · SetShapeKeepTransparency —— SBE 侧内联在 Resize 里（中间夹着
//     窗口同步、FixBounds），无法提取；DIY 侧只有 4 行，抽出去省不了多少。
//   · GridAlphaFor —— SBE 读【显化网格】配置，DIY 直算，语义不同。
//   · WindowNeedsSync —— SBE 独有（DIY 的窗口同步走 ApplyWindowSize）。
//   本次实际抽取：MarkAllDirty（SBE 的 SyncWindowRect + DIY 的 ApplyWindowSize）。

using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace ContainerSuite.Shared;

/// <summary>
/// 网格填充底 / 透明度 / 节点管理的共享工具。
/// SBE（扩容仓库）和 DIY（容器升级）共用；各家的 RebuildGridFill 保留在本地。
/// </summary>
public static class GridFx
{
    // ─────────────────────────────────────────────────────────────
    // 网格节点列表访问
    // ─────────────────────────────────────────────────────────────

    /// <summary>按索引取 5 个动态填充底列表之一（0=backgrounds, 1~4=R0~R3）。</summary>
    public static Il2CppSystem.Collections.Generic.List<TreeNodeRender> MeshList(GameGridInventory g, int k)
    {
        switch (k)
        {
            case 0: return g.dynamicBackgrounds;
            case 1: return g.dynamicR0;
            case 2: return g.dynamicR1;
            case 3: return g.dynamicR2;
            default: return g.dynamicR3;
        }
    }

    // ─────────────────────────────────────────────────────────────
    // 透明度
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 统一把 5 个动态列表里所有节点的 Image.alpha 写成 alpha。
    /// ★ 无条件写 + 逐节点 SetAllDirty（README 禁止事项 #10）—— 不要加"值相等就跳过"的优化。
    /// </summary>
    public static void ApplyMeshAlpha(GameGridInventory g, float alpha)
    {
        if (g == null) return;
        for (int k = 0; k < 5; k++)
        {
            try
            {
                var list = MeshList(g, k);
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    TreeNodeRender n = list[i];
                    if (n == null) continue;
                    Image img = n.image;
                    if (img == null) continue;
                    Color c = img.color;
                    img.color = new Color(c.r, c.g, c.b, alpha);
                    img.SetAllDirty();
                }
            }
            catch { }
        }
    }

    /// <summary>SetShape 后恢复网格透明度（若变过）。</summary>
    public static void RestoreTransparency(GameGridInventory grid, float before)
    {
        if (grid == null || before <= 0.001f) return;
        try
        {
            if (Math.Abs(grid.transparency - before) > 0.001f) grid.SetTransparency(before);
        }
        catch (Exception ex) { CatchLogOnce.Warn("GridFx:restore-transparency", ex); }
    }
    // ─────────────────────────────────────────────────────────────
    // UI 标脏
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// 从 rt 开始递归标脏所有 Graphic（含 rt 自己）。
    ///
    /// 抽取自 SBE 的 <c>SyncWindowRect</c> 与 DIY 的 <c>ApplyWindowSize</c>
    /// 里两段写法完全一致的代码。语义：不是"重绘"，而是"通知 Unity
    /// 下一帧重绘"—— 布局/形状变化后必须调，否则新形状的网格会显示成
    /// 旧形状（视觉残留）。
    ///
    /// <paramref name="where"/> 用于异常去重（CatchLogOnce 的 key）。
    /// 为空时不记异常 —— 对每秒调用的热路径友好。
    ///
    /// ★ 不要再"顺手统一"其它调用点：DIY 的 DelayedRedraw / RestoreSetAllDirty
    ///   写法与这里**故意不同**（只标特定 gameObject），强行统一会改变
    ///   SetAllDirty 的范围，可能引入视觉闪烁。详见 README S8s。
    /// </summary>
    public static void MarkAllDirty(RectTransform rt, string where = null)
    {
        if (rt == null) return;

        // 标 rt 自己
        try
        {
            var self = rt.GetComponent<Graphic>();
            if (self != null) self.SetAllDirty();
        }
        catch (Exception ex) { if (where != null) CatchLogOnce.Warn(where, ex); }

        // 标所有子 Graphic（GetComponentsInChildren 包含自己，重复 SetAllDirty 无害）
        try
        {
            var gs = rt.GetComponentsInChildren<Graphic>(true);
            if (gs == null) return;
            for (int i = 0; i < gs.Count; i++)
                if (gs[i] != null) gs[i].SetAllDirty();
        }
        catch (Exception ex) { if (where != null) CatchLogOnce.Warn(where, ex); }
    }

    // ─────────────────────────────────────────────────────────────
    // 节点销毁 / 越界隐藏
    // ─────────────────────────────────────────────────────────────

    /// <summary>销毁一个 TreeNodeRender 列表里的所有节点并清空列表（用于 RebuildGridFill 前的清理）。</summary>
    public static void DestroyNodeList(Il2CppSystem.Collections.Generic.List<TreeNodeRender> list)
    {
        if (list == null) return;
        int n = list.Count;
        for (int i = 0; i < n; i++)
        {
            try
            {
                TreeNodeRender node = list[i];
                if (node == null) continue;
                RectTransform rt = node.rectTransform;
                if (rt == null) continue;
                Object.Destroy(((Component)rt).gameObject);
            }
            catch { }
        }
        try { list.Clear(); } catch { }
    }

    // 拖拽中 HideOutOfBoundsNodes 的缓存：同一 grid 在 (shape, winH, winW) 不变时跳过遍历。
    private static readonly Dictionary<IntPtr, (int W, int H, int WinH, int WinW)> _lastHideBounds
        = new Dictionary<IntPtr, (int, int, int, int)>();

    /// <summary>
    /// 拖拽中隐藏超出可视范围的填充底节点（避免拖拽时做重活儿）。
    /// 带 (shape, winH, winW) 缓存 —— 同一形状 + 同一窗口尺寸时跳过遍历。
    /// </summary>
    public static void HideOutOfBoundsNodes(GameGridInventory g)
    {
        if (g?.inventoryShape == null) return;
        int winH = 0;
        try { winH = g.heightPixels; } catch { }
        if (winH <= 0) return;
        int winW = 0;
        try { winW = g.widthPixels; } catch { }
        if (winW <= 0) return;

        IntPtr cachePtr = IntPtr.Zero;
        try { cachePtr = ((Il2CppObjectBase)g).Pointer; } catch { }
        if (cachePtr != IntPtr.Zero)
        {
            var cur = (g.inventoryShape.width, g.inventoryShape.height, winH, winW);
            if (_lastHideBounds.TryGetValue(cachePtr, out var last) && last == cur) return;
            CacheLimits.Trim(_lastHideBounds, CacheLimits.Small);   // A1（原裸写 >64）
            _lastHideBounds[cachePtr] = cur;
        }

        // ═══════════════════════════════════════════════════════════════════
        // ★★★ 2026-10-10 实测修正：原判据是**对称**的 `|y| > winH*0.5f + 20f`，
        //   其前提是「填充底节点围绕网格中心 y=0 分布」。
        //
        //   【实测推翻】UserData/ContainerSuite.resizediag.txt（storage_bay 10x10）：
        //     yMin=-224.0 yMax=16.0  yCenter=-104.0
        //   ⇒ 节点**几乎全在负半轴**：y=0 是网格**顶部**，向下延伸为负。
        //     节点实际范围 = [-(winH + rowH), +rowH]（比格子多一圈 = 上下各多一行）。
        //
        //   后果（这就是"从上压缩 → 网格残留"的全部来源）：
        //     · `y > +halfH` → 永远不可能命中（节点最大 y 才 +16，halfH=100）
        //     · `y < -halfH` → 大量命中（从 -100 一直到 -224）
        //   ⇒ 该判据**只会隐藏网格下半部分，永远不会隐藏上半部分**：
        //      · 从下压（Bottom*）：期望底部收缩，判据恰好只藏底部 → 看起来"正常"；
        //      · 从上压（TopLeft/TopRight）：期望顶部收缩，判据仍只藏底部 → 顶部该消失的还在 → 残留。
        //
        //   【修正】改用**非对称**判据，边界 = 网格实际内容区（含四周各一圈边框）：
        //     top    = +rowH      （顶部边框行中心）
        //     bottom = -winH      （底部边框行中心）
        //   【2026-10-10 17:50 追加 X 轴】快速拖拽缩小暴露了第二个洞：判据原来**只查 Y**。
        //     缩小时陈旧节点在两个轴上都过期 —— 多出的**列**（X 方向）没被藏，
        //     在窗口右侧/下方露出一片乱格（用户实测：快速拖拽出乱网格、松手消失）。
        //     X 与 Y 同一锚定模式（由已实测的 Y 推得）：内容列从 0 向右，
        //     左边框列中心 ≈ -colW，右边框列中心 ≈ +winW ⇒ 合法 x ∈ [-colW, +winW]。
        //   用实测值验证（winH=160, shapeH=10 ⇒ rowH=16）：
        //     top=16, bottom=-160
        //     · 健康网格 y ∈ [-160,+16] ⇒ 全部在界内 ⇒ 隐藏 0 个（原判据误藏 240 个）✔
        //     · 缩小时残留的陈旧行 y ∈ {-176..-224} < -160 ⇒ 正确隐藏 ✔
        // ═══════════════════════════════════════════════════════════════════
        int rows = 1;
        try { rows = Math.Max(1, g.inventoryShape.height); } catch { rows = 1; }
        int cols = 1;
        try { cols = Math.Max(1, g.inventoryShape.width); } catch { cols = 1; }
        float rowH = (float)winH / (float)rows;
        float colW = (float)winW / (float)cols;

        float top = rowH + 1f;              // 顶部边框行中心 ≈ +rowH（+1 抗浮点误差）
        float bottom = -(float)winH - 1f;   // 底部边框行中心 ≈ -winH（-1 抗浮点误差）
        float left = -colW - 1f;            // 左边框列中心 ≈ -colW
        float right = (float)winW + 1f;     // 右边框列中心 ≈ +winW

        for (int k = 0; k < 5; k++)
        {
            var list = MeshList(g, k);
            if (list == null) continue;
            for (int i = 0; i < list.Count; i++)
            {
                var n = list[i];
                if (n == null) continue;
                var rt = n.rectTransform;
                if (rt == null) continue;
                try
                {
                    Vector2 pos = rt.anchoredPosition;
                    bool outOfBounds = (pos.y > top) || (pos.y < bottom)
                                    || (pos.x > right) || (pos.x < left);
                    var go = ((Component)rt).gameObject;
                    if (go != null && go.activeSelf == outOfBounds)
                        go.SetActive(!outOfBounds);
                }
                catch { }
            }
        }
    }

    /// <summary>场景切换时清空内部缓存（旧 grid 指针全部失效）。</summary>
    public static void ClearCaches()
    {
        _lastHideBounds.Clear();
    }

    // ─────────────────────────────────────────────────────────────
    // 填充底健康度判定：两套并存
    // ─────────────────────────────────────────────────────────────

    /// <summary>
    /// SBE 版判据：逐行覆盖分析。所有动态列表里，按 anchoredPosition.y 分行统计，
    /// 行数需 >= shape.height，且每行节点数最小值 * 5 >= 最大值 * 3（即不能有某行严重缺节点）。
    /// 原 SBE 的 FillHealthy。
    /// </summary>
    public static bool FillHealthyByRow(GameGridInventory g)
    {
        if (g == null) return true;
        int rows, min, max;
        if (!RowStats(g, out rows, out min, out max)) return false;
        if (max <= 0) return false;

        int expected = 0;
        try { expected = g.inventoryShape?.height ?? 0; } catch { }
        if (expected > 0 && rows < expected) return false;

        return min * 5 >= max * 3;
    }

    /// <summary>
    /// DIY 版判据：节点数差。dynamicBackgrounds 数量与 (w+2)*(h+2) 的差值
    /// 不超过 expectedBg 的 5%（diff * 20 <= expectedBg）。
    /// 原 DIY 的 FillHealthy。
    /// </summary>
    public static bool FillHealthyByCount(GameGridInventory g)
    {
        if (g == null) return true;
        if (g.inventoryShape == null) return true;
        int w = g.inventoryShape.width;
        int h = g.inventoryShape.height;
        if (w <= 0 || h <= 0) return true;
        int expectedBg = (w + 2) * (h + 2);
        int actualBg = 0;
        try { actualBg = g.dynamicBackgrounds?.Count ?? 0; } catch { }
        int diff = Math.Abs(actualBg - expectedBg);
        return diff * 20 <= expectedBg;
    }

    // ─────────────────────────────────────────────────────────────
    // 行统计（SBE 版判据的辅助）
    // ─────────────────────────────────────────────────────────────

    /// <summary>按 anchoredPosition.y 分行统计节点数；返回 rows / 每行最小 / 每行最大。</summary>
    /// <remarks>
    /// ★ 2026-10-09：行计数字典改为**复用缓冲**（原实现每次调用 new 一个
    ///   Dictionary&lt;int,int&gt;）。RowStats 在一次 Resize 链路里会被调 2~3 次/容器
    ///   （FillHealthyByRow → 判坏 → RebuildGridFill 再判），而每次都要遍历全部 5 个
    ///   mesh 列表的所有节点 —— 每次 new 字典是纯 GC 压力。
    ///   复用安全性：RowStats 仅在 Unity 主线程、且调用链全程顺序（无重入）——
    ///   与 SaveBagExpandCore.CornersBuffer 同一模式。若将来出现重入/多线程调用，
    ///   须改用局部字典或 [ThreadStatic]。
    /// </remarks>
    [ThreadStatic] private static Dictionary<int, int> _rowStatsBuf;

    public static bool RowStats(GameGridInventory g, out int rows, out int min, out int max)
    {
        rows = 0; min = 0; max = 0;
        try
        {
            var dict = _rowStatsBuf;
            if (dict == null) { dict = new Dictionary<int, int>(); _rowStatsBuf = dict; }
            else dict.Clear();
            for (int k = 0; k < 5; k++)
            {
                var list = MeshList(g, k);
                if (list == null) continue;
                for (int i = 0; i < list.Count; i++)
                {
                    TreeNodeRender n = list[i];
                    RectTransform rt = (n == null) ? null : n.rectTransform;
                    if (rt == null) continue;
                    int y = Mathf.RoundToInt(rt.anchoredPosition.y);
                    dict.TryGetValue(y, out int c);
                    dict[y] = c + 1;
                }
            }
            if (dict.Count == 0) return false;
            rows = dict.Count; max = 0; min = int.MaxValue;
            foreach (var kv in dict)
            {
                if (kv.Value > max) max = kv.Value;
                if (kv.Value < min) min = kv.Value;
            }
            return true;
        }
        catch { return false; }
    }

    /// <summary>把 RowStats 结果格式化成一行字符串（诊断用）。</summary>
    public static string RowCoverage(GameGridInventory g)
    {
        int rows, min, max;
        if (!RowStats(g, out rows, out min, out max)) return "无节点";
        return $"{rows} 行，每行 {min}~{max} 个";
    }
}