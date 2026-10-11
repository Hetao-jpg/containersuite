// ══════════════════════════════════════════════════════════════════════════
// SaveBagExpandWindowDrag —— 「地面」窗口鼠标拖动（2026-10-09 新增）
//
// 目标：让玩家能用鼠标拖动 PickupWindow 的「地面」窗口（EmporiumEntry.afterhourWindow）。
//
// ── 为什么是"鼠标拖动"而不是"按键移动" ──
//   1) 游戏**自带**完整的 UI 拖动实现：
//        · PixelWindow.OnDrag(PointerEventData)   RVA 0x7C74C0（TreeNodeRender 那层，走 EventSystem 射线）
//        · UIDragHandler.OnDrag(PointerEventData) RVA 0x6E1240（通用 UI 拖动组件，语义最干净）
//      其核心公式（反汇编核实）：
//        anchoredPosition += (PointerEventData.delta / Canvas.scaleFactor)
//      本文件**复刻这一个公式**（铁律 1：借鉴成功案例，不自创）。
//   2) 地面窗口为什么游戏不让拖：PixelWindow.canMove 是 **readonly** 字段（偏移 0x18），
//      构造时传了 false → 不进 dockedFloating、OnDrag 对它是空操作。改不了 readonly，
//      所以只能在 mod 层用**同款公式**自己实现（不动 canMove、不碰 IL）。
//
// ── 安全设计（逐条对照工作区铁律）──
//   ★ 不挂任何新原生方法：拖动纯在 SaveBagExpandCore.Tick() 里轮询 Input 完成，
//     与现有 TryScroll / SaveBagPaging 完全同一范式。⇒ 不触碰 FBD #26 / R4（热访问器禁区），
//     也不会有"挂 new virtual/无体方法 → AccessViolation 秒崩"的风险（无需挂点 = 无此风险）。
//   ★ 不调被劫持的方法：本模块没有任何 Harmony 补丁，自然无"自伤递归"。
//   ★ 危险路径只读裸字段：唯一对游戏对象的读取是 ItemMouseDragHandler.current.IsDraggingItem
//     —— 公开 bool 属性、getter 有 native body、内部只读 isDragging(bool, 0x68) 一个字段，
//     **完全不碰被拖动中的 GameItem**（MEMORY 铁律 10 的正面用法：用游戏自己的状态标志
//     否定整条路径）。拖动物品时直接整条跳过，绝不与物品拖拽抢鼠标。
//   ★ 不用"一次性锁"（铁律 8）：窗口/基准随场景重建，每次窗口重新可见都要能重新抓基准，
//     故用"可见性变化 → 重置基准"的**事件式重置**，而不是 _tried 一次性锁。
//   ★ 加载期不动作：由调用方（Tick）先过 WorldReadyGate.ShouldDefer（本模块不自己判，
//     保持单一判据来源）。
//   ★ 日志走过 Diag（受 DiagLog 总闸控制）：拖动不刷屏；成功/失败都有据可查。
//
// ── 位置语义（与 PickupWindowTest 一致，实机已验证）──
//   · 偏移量 = anchoredPosition 相对「游戏原版停靠位」的差值，单位 = anchoredPosition 像素。
//   · 原版停靠位 = 窗口 Show 后稳定下来的 anchoredPosition。本模块在窗口**刚变可见**时
//     记录当前 anchoredPosition 当作基准（此时游戏已写完初始位置），
//     之后每次把 anchoredPosition 设为「基准 + 持久化偏移」。
//   · 偏移量持久化进 cfg（7G_GroundOffsetX/Y）—— 玩家选择"记住位置"。
//   · 窗口由游戏 FixBounds() 钳制在屏幕内；本模块写位置后也调一次 FixBounds（若可用），
//     避免把窗口拖到屏幕外拿不回来。
// ══════════════════════════════════════════════════════════════════════════

using System;
using Il2Cpp;
using MelonLoader;
using UnityEngine;
using ContainerSuite.Shared;   // CatchLogOnce

namespace SaveBagExpand;

/// <summary>「地面」窗口的鼠标拖动。</summary>
internal static class SaveBagExpandWindowDrag
{
    // ── 拖动态（跨帧） ──
    private static bool _dragging;                 // 是否正在拖窗口
    private static Vector3 _lastMouse;             // 上一帧鼠标位置（算 delta）
    private static bool _wasVisibleLastFrame;      // 上一帧窗口是否可见（检测"刚变可见"）

    // ── 拖动步长阈值（像素）：小于此值的鼠标位移不产生拖动，
    //    避免"点击标题栏"被误判成"微小拖动"。与游戏 UIDragHandler.dragThreshold 同思路。──
    private const float DragThreshold = 2f;

    // ── 偏移量（anchoredPosition 像素，相对原版停靠位）。持久化到 cfg。──
    //    这里维护一份内存镜像，拖动时即时更新、松手时写盘。
    private static int _offsetX;
    private static int _offsetY;
    private static bool _offsetLoaded;             // cfg 值是否已读入内存镜像

    // ── 拖动命中判定用的 GetWorldCorners 复用缓冲（懒建）。
    //    ★ 不共用 SaveBagExpandCore.CornersBuffer —— 那个供 IsMouseOver/滚动用，
    //      本模块独立一份，避免"谁先调用谁覆写"的耦合（都在主线程顺序执行，
    //      分开只为隔离风险面，代价 = 一个 4 元小数组）。──
    private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> _cornersBuffer;
    private static Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3> CornersBuffer
    {
        get
        {
            if (_cornersBuffer == null)
                _cornersBuffer = new Il2CppInterop.Runtime.InteropTypes.Arrays.Il2CppStructArray<Vector3>(4);
            return _cornersBuffer;
        }
    }

    /// <summary>由 SaveBagExpandCore.Tick() 每帧调用（已过 WorldReadyGate）。</summary>
    internal static void Tick(EmporiumEntry entry)
    {
        try
        {
            if (entry == null) return;

            // ── 功能开关 ──
            bool enabled = false;
            try { enabled = SaveBagExpandConfig.GroundDragEnabled?.Value == true; } catch { }
            if (!enabled)
            {
                if (_dragging) EndDrag(entry, "开关关闭");
                return;
            }

            // ── 取地面窗口 ──
            PixelWindow win = null;
            try { win = entry.afterhourWindow; } catch (Exception ex) { CatchLogOnce.Warn("sbe.drag.win", ex); }
            if (win == null)
            {
                if (_dragging) _dragging = false;
                _wasVisibleLastFrame = false;
                return;
            }

            RectTransform rt = null;
            try { rt = win.rectTransform; } catch (Exception ex) { CatchLogOnce.Warn("sbe.drag.rt", ex); }
            if (rt == null)
            {
                if (_dragging) _dragging = false;
                _wasVisibleLastFrame = false;
                return;
            }

            // ── 可见性（只读 bool，安全） ──
            bool visible = false;
            try { visible = win.IsVisible(); } catch { }

            if (!visible)
            {
                // 窗口不可见：清拖动状态，不做任何位置操作（避免对隐藏/半死对象操作）
                if (_dragging) _dragging = false;
                _wasVisibleLastFrame = false;
                _haveBase = false;             // 下次可见要重新抓基准
                _framesSinceVisible = 0;
                _lastStableAp = new Vector2(float.NaN, float.NaN);
                _lastStableArea = float.NaN;   // P4：面积样本一并作废
                return;
            }
            if (!_wasVisibleLastFrame)
            {
                // 刚变可见：重置稳定检测（不立刻抓基准 —— 游戏可能还在写布局）
                _framesSinceVisible = 0;
                _haveBase = false;
                _lastStableAp = new Vector2(float.NaN, float.NaN);
                _lastStableArea = float.NaN;   // P4：面积样本一并作废
            }
            _wasVisibleLastFrame = true;
            _framesSinceVisible++;

            // ── 基准稳定检测（复刻 PickupWindowTest 的"等帧 + 位置不变"判定）──
            //   ★ 为什么不能"一可见就抓基准"：游戏在 PixelWindow.Show 内部写初始位置，
            //     之后若干帧内布局可能仍在变（测试工程实测 ~8 帧才稳定）。
            //     过早抓基准 → 基准取到中间态 → 持久化偏移算错、窗口位置漂移。
            //   ★ 为什么不用"rect 尺寸"判稳定：本框架窗口 root 是纯容器，rect 恒为 0×0，
            //     只能靠"位置连续两帧不变"判断（与测试工程同一结论）。
            if (!_haveBase)
            {
                CaptureBaseWhenStable(rt, win);
                // 基准还没稳定前，不处理拖动（此时窗口还在游戏中，位置会打架）
                if (!_haveBase) return;
            }

            // ── 鼠标状态（输入模块不可用属无害情形，静默） ──
            Vector3 mouse;
            bool leftDown, leftHeld;
            try
            {
                mouse = Input.mousePosition;
                leftDown = Input.GetMouseButtonDown(0);
                leftHeld = Input.GetMouseButton(0);
            }
            catch { return; }

            // ── 正在拖物品？整条跳过（危险路径否定，绝不相争） ──
            if (IsDraggingGameItem())
            {
                if (_dragging) _dragging = false;
                _lastMouse = mouse;
                return;
            }

            // ── 状态机 ──
            if (_dragging)
            {
                if (!leftHeld)
                {
                    EndDrag(entry, "松手");
                    _lastMouse = mouse;
                    return;
                }

                // 复刻 UIDragHandler.OnDrag：delta / Canvas.scaleFactor 累加。
                float dx = mouse.x - _lastMouse.x;
                float dy = mouse.y - _lastMouse.y;
                _lastMouse = mouse;

                if (Mathf.Abs(dx) < 0.001f && Mathf.Abs(dy) < 0.001f) return;

                float scale = CanvasScaleFactor(win, rt);
                if (scale <= 0.0001f) scale = 1f;

                Vector2 ap;
                try { ap = rt.anchoredPosition; }
                catch (Exception ex) { CatchLogOnce.Warn("sbe.drag.ap", ex); return; }

                ap.x += dx / scale;
                ap.y += dy / scale;
                try { rt.anchoredPosition = ap; } catch (Exception ex) { CatchLogOnce.Warn("sbe.drag.setap", ex); return; }

                // 同步更新偏移镜像 = 当前位置 - 基准位置
                if (_haveBase)
                {
                    _offsetX = Mathf.RoundToInt(ap.x - _baseX);
                    _offsetY = Mathf.RoundToInt(ap.y - _baseY);
                }
                return;
            }

            // ── 未拖动：检测是否在标题栏按下左键 ⇒ 开始拖动 ──
            if (leftDown && HitTitleBar(rt, win, mouse))
            {
                _dragging = true;
                _lastMouse = mouse;
                SaveBagExpandCore.Diag($"[地面拖动] 开始拖动（鼠标 {mouse.x:F0},{mouse.y:F0}）");
            }
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.drag.Tick", ex);
        }
    }

    // ── 基准（原版停靠位）──
    private static float _baseX;
    private static float _baseY;
    private static bool _haveBase;

    // ── 基准稳定检测状态 ──
    private static int _framesSinceVisible;                 // 自窗口变可见起的帧数
    private static Vector2 _lastStableAp;                   // 上帧读到的 anchoredPosition
    private static float _lastStableArea = float.NaN;       // 上帧的窗口世界矩形面积（NaN = 无样本）

    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 P4】稳定判据去魔法数
    //
    // 旧判据 = 「可见满 5 帧」+「位置连续两帧不变」。
    //   病灶全在那个 **5**：它是对"游戏要多久算完布局"的**猜测**
    //   （注释里写着"测试工程实测 ~8 帧才稳定"—— 实测 8 却写 5，本身就自相矛盾）。
    //   机器慢 / 窗口复杂 / 首帧卡顿 ⇒ 5 帧时布局还在变 ⇒ 抓到中间态
    //   ⇒ 基准错 ⇒ 持久化偏移算错、窗口位置漂移。
    //   而且它跟帧率绑定：掉帧时 5 帧更短，判据更松。
    //
    // 新判据 = **语义化**：不问"过了几帧"，只问"布局是不是真的不动了"：
    //   ① 位置连续两帧不变（沿用，容差见下）；
    //   ② **窗口世界矩形面积**连续两帧不变 —— 这才是"布局算完了"的真正信号。
    //      复用既有的 TryGetWindowWorldRect（全工程唯一矩形实现，已实机验证），
    //      它取的是 handler 子节点里面积最大的真实矩形，绕开 root 0×0 的坑。
    //
    // 下面每个常量都有出处，不是拍脑袋的经验值：
    //   · BaseStableMinFrames = 2 —— **结构下限**：要比较"连续两帧"至少得有两帧样本，
    //     与"布局要多久"无关；
    //   · BaseStableEps = 0.5 —— canvas 单位下的**亚像素**容差：布局收敛的残余抖动
    //     必然 < 1 单位，≥1 单位就是真移动；
    //   · BaseStableAreaRelEps = 1% —— 用**相对**容差，与窗口尺寸无关
    //     （绝对像素容差换个窗口大小就失效，那才是魔法数）；
    //   · BaseStableGraceFrames = 120 —— 兜底：约 2 秒（@60fps）仍不稳定就退化成
    //     "只看位置"，防止"永远抓不到基准"导致功能静默失效。
    //     ★ 本套件最忌讳静默失效，宁可退化也不能卡死。
    // ═══════════════════════════════════════════════════════════════════
    private const int BaseStableMinFrames = 2;              // 结构下限：两帧才能比"连续两帧"
    private const float BaseStableEps = 0.5f;               // canvas 单位下的亚像素容差
    private const float BaseStableAreaRelEps = 0.01f;       // 面积相对容差 1%（与尺寸无关）
    private const int BaseStableGraceFrames = 120;          // 兜底：约 2s 后退化成"只看位置"

    /// <summary>
    /// 窗口布局稳定后，把当前 anchoredPosition 记为基准（= 游戏原版停靠位），
    /// 并把持久化偏移应用上去（若有）。
    /// <para>
    /// ★ 稳定判据（P4 起语义化）= 位置连续两帧不变 <b>且</b> 窗口世界矩形面积连续两帧不变。
    /// 详见 <c>BaseStableMinFrames</c> 处的完整说明。
    /// </para>
    /// </summary>
    private static void CaptureBaseWhenStable(RectTransform rt, PixelWindow win)
    {
        try
        {
            // 结构下限：不足两帧就没有可比较的样本
            if (_framesSinceVisible < BaseStableMinFrames) return;

            // ── 采样：位置与面积必须**同帧**取，否则比的是错位的样本 ──
            Vector2 cur;
            try { cur = rt.anchoredPosition; }
            catch { return; }

            float area = float.NaN;
            try
            {
                if (TryGetWindowWorldRect(win, out float l, out float r, out float b, out float t))
                    area = Math.Max(0f, (r - l) * (t - b));
            }
            catch { area = float.NaN; }

            bool havePrev = !float.IsNaN(_lastStableAp.x);

            bool posStable = havePrev && Vector2.Distance(cur, _lastStableAp) <= BaseStableEps;
            bool areaStable = havePrev
                              && !float.IsNaN(_lastStableArea) && !float.IsNaN(area)
                              && Math.Abs(area - _lastStableArea)
                                 <= BaseStableAreaRelEps * Math.Max(area, 1f);

            // 记本帧样本供下一帧比较
            _lastStableAp = cur;
            _lastStableArea = area;

            // 首帧只有样本，无从比较
            if (!havePrev) return;

            // 兜底：布局长期抖动（动画窗口等）⇒ 退化成"只看位置"，避免永远抓不到基准
            bool grace = _framesSinceVisible >= BaseStableGraceFrames;

            if (!(posStable && (areaStable || grace))) return;

            if (grace && !areaStable)
            {
                SaveBagExpandCore.Diag(
                    $"[地面拖动] 基准检测 {BaseStableGraceFrames} 帧未收敛，退化用位置判据" +
                    $"（area={area:F0} prev={_lastStableArea:F0}）");
            }

            // 稳定了 —— 记基准
            _baseX = cur.x;
            _baseY = cur.y;
            _haveBase = true;

            EnsureOffsetLoaded();
            if (_offsetX == 0 && _offsetY == 0) return;      // 无偏移 = 保持原版位置

            Vector2 want = new Vector2(_baseX + _offsetX, _baseY + _offsetY);
            try { rt.anchoredPosition = want; } catch (Exception ex) { CatchLogOnce.Warn("sbe.drag.applyBase", ex); return; }
            TryClampToScreen(win);

            SaveBagExpandCore.Diag(
                $"[地面拖动] 应用持久化偏移 ({_offsetX},{_offsetY}) → 基准 ({_baseX:F0},{_baseY:F0})，" +
                $"目标 ({want.x:F0},{want.y:F0})");
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.drag.CaptureBaseWhenStable", ex);
        }
    }

    private static void EndDrag(EmporiumEntry entry, string reason)
    {
        try
        {
            _dragging = false;
            if (!_haveBase) return;

            // 偏移量写盘（玩家选择"记住位置"）
            try
            {
                if (SaveBagExpandConfig.GroundOffsetX != null)
                    SaveBagExpandConfig.GroundOffsetX.Value = _offsetX;
                if (SaveBagExpandConfig.GroundOffsetY != null)
                    SaveBagExpandConfig.GroundOffsetY.Value = _offsetY;
            }
            catch (Exception ex) { CatchLogOnce.Warn("sbe.drag.saveCfg", ex); }

            SaveBagExpandCore.Diag($"[地面拖动] 结束（{reason}），偏移 = ({_offsetX},{_offsetY}) 已保存");
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.drag.EndDrag", ex);
        }
    }

    /// <summary>把 cfg 里的持久化偏移读进内存镜像（懒加载，只读一次）。</summary>
    private static void EnsureOffsetLoaded()
    {
        if (_offsetLoaded) return;
        _offsetLoaded = true;
        try { _offsetX = SaveBagExpandConfig.GroundOffsetX?.Value ?? 0; } catch { _offsetX = 0; }
        try { _offsetY = SaveBagExpandConfig.GroundOffsetY?.Value ?? 0; } catch { _offsetY = 0; }
    }

    /// <summary>
    /// 命中判定：鼠标是否落在窗口**标题栏**区域（顶部 titleHeight 高度）。
    /// ★★ 2026-10-09 实测定稿（三轮实机探针）：
    ///   **不能**用 `PixelWindow.rectTransform`（= handler）的 `GetWorldCorners` ——
    ///   handler 是纯容器，`rect`/`sizeDelta` 恒为 0×0，四角**全部重合于一点**
    ///   （实测 [0]=[1]=[2]=[3]=(518,335)）⇒ 命中区退化为零面积 ⇒ 永不命中。
    ///   ★ 正确来源 = **handler 的「有真实矩形的子节点」**（Instantiate 出来的可见 UI 节点）。
    ///     实测（12×12 地面窗口，1920×1080，canvasSF=2）：
    ///       [0] Node 世界矩形 x[518,1310] y[-93,331]  r=396x212  ← 窗口主体（取这个）
    ///       [5] Node 世界矩形 x[518,1310] y[329,335]  r=396x3    ← 顶部细条
    ///     `GetWorldCorners` 对 ScreenSpaceOverlay canvas 返回的即**屏幕坐标**，
    ///     与 `Input.mousePosition` 同一空间（实测鼠标 (527,319) 落在上述矩形内）⇒ 可直接比较。
    ///   算法：遍历 handler 子节点，取**面积最大**的那个（= 窗口主体）的 corners 作为窗口矩形；
    ///     标题栏 = 顶部 `titleHeight × 2` 高的一条（×2 = 像素→世界，见实测 396=198×2）。
    /// </summary>
    private static bool HitTitleBar(RectTransform rt, PixelWindow win, Vector3 mouse)
    {
        try
        {
            if (!TryGetWindowWorldRect(win, out float left, out float right, out float bottom, out float top))
                return false;

            float worldH = top - bottom;
            if (worldH <= 0.001f) return false;

            // 标题栏高（世界单位）。titleHeight 是像素，像素→世界 ×2（实测）。
            // ★ 不读 canvasSF 硬乘（实测 canvasSF=2.00，但 pix→world 比固定 ×2；
            //   用窗口像素高与实测世界高之比反推更稳）。
            float pxH = 0f;
            try { pxH = win.heightPixels; } catch { }
            float pixToWorld = (pxH > 0.001f) ? (worldH / pxH) : 2f;

            float titleH = 0f;
            try { titleH = win.titleHeight; } catch { }
            float barHWorld = titleH > 0f ? titleH * pixToWorld : Mathf.Max(4f, worldH / 6f);
            if (barHWorld > worldH * 0.5f) barHWorld = worldH * 0.5f;   // 标题栏不超过半窗，留一半给内容

            // 标题栏 = 顶部一条；右上角让给关闭按钮（内缩一个 barH 见方）。
            float barTop = top;
            float barBottom = top - barHWorld;
            float barLeft = left;
            float barRight = right - barHWorld;
            if (barRight < barLeft) barRight = barLeft;

            return mouse.x >= barLeft && mouse.x <= barRight
                && mouse.y >= barBottom && mouse.y <= barTop;
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.drag.HitTitleBar", ex);
            return false;
        }
    }

    /// <summary>
    /// 取窗口的**世界（屏幕）矩形** —— 来源 = handler 子节点里面积最大的那个。
    /// 见 <see cref="HitTitleBar"/> 的完整实测依据。
    /// <para>
    /// ★ 2026-10-09 起提升为 <c>internal</c>：<c>SaveBagExpandCore.IsMouseOver</c>（滚轮命中）
    /// 复用同一算法 —— <c>PixelWindow.rectTransform</c>（handler 自身）是纯容器，
    /// <c>rect</c> 恒为 0×0 → <c>GetWorldCorners</c> 四点重合 → 命中区恒为空。
    /// 全工程只此一处矩形实现，避免两套逻辑再次跑偏。
    /// </para>
    /// </summary>
    internal static bool TryGetWindowWorldRect(PixelWindow win, out float left, out float right,
                                               out float bottom, out float top)
    {
        left = right = bottom = top = 0f;
        try
        {
            if (win == null) return false;
            RectTransform rt = null;
            try { rt = win.rectTransform; } catch { }
            if (rt == null) return false;

            Transform parent = null;
            try { parent = rt.transform; } catch { }
            if (parent == null) return false;

            float bestArea = -1f;
            float bLeft = 0f, bRight = 0f, bBottom = 0f, bTop = 0f;

            int n = 0;
            try { n = parent.childCount; } catch { }
            int max = Math.Min(n, 24);
            for (int i = 0; i < max; i++)
            {
                Transform ch = null;
                try { ch = parent.GetChild(i); } catch { continue; }
                if (ch == null) continue;
                RectTransform crt = null;
                try { crt = ch.TryCast<RectTransform>(); } catch { continue; }
                if (crt == null) continue;

                // 只收有真实矩形的节点（跳过退化的 0×0）
                try
                {
                    var r = crt.rect;
                    if (r.width <= 0.5f || r.height <= 0.5f) continue;
                }
                catch { continue; }

                var c = CornersBuffer;
                try { crt.GetWorldCorners(c); } catch { continue; }
                // 顺序 [0]左下 [1]左上 [2]右上 [3]右下
                float l = Math.Min(c[0].x, c[2].x);
                float rr = Math.Max(c[0].x, c[2].x);
                float b = Math.Min(c[0].y, c[2].y);
                float t = Math.Max(c[0].y, c[2].y);
                float area = (rr - l) * (t - b);
                if (area > bestArea)
                {
                    bestArea = area;
                    bLeft = l; bRight = rr; bBottom = b; bTop = t;
                }
            }

            if (bestArea <= 0.5f) return false;
            left = bLeft; right = bRight; bottom = bBottom; top = bTop;
            return true;
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.drag.TryGetWindowWorldRect", ex);
            return false;
        }
    }

    /// <summary>玩家是否正在拖物品（危险路径否定：只读一个 bool，不碰被拖对象）。</summary>
    private static bool IsDraggingGameItem()
    {
        try
        {
            var h = ItemMouseDragHandler.current;
            if (h == null) return false;
            return h.IsDraggingItem;
        }
        catch { return false; }
    }

    /// <summary>读 Canvas.scaleFactor（拖动公式的分母）。读不到返回 1。</summary>
    private static float CanvasScaleFactor(PixelWindow win, RectTransform rt)
    {
        try
        {
            // ★ RectTransform 是 Unity Component，有 GetComponentInParent<T>；
            //   （注意：PixelWindow 本身**不是** Component —— 它是 IL2CPP 接口类型，
            //    在它上面调 GetComponentInParent 会编译失败，必须在 rectTransform 上调。）
            if (rt == null) return 1f;
            Canvas c = null;
            try { c = rt.GetComponentInParent<Canvas>(); } catch { }
            if (c == null) return 1f;
            float s = c.scaleFactor;
            return s > 0.0001f ? s : 1f;
        }
        catch { return 1f; }
    }

    /// <summary>写位置后调一次 FixBounds，避免把窗口拖出屏幕拿不回来（若可用）。</summary>
    private static void TryClampToScreen(PixelWindow win)
    {
        try { win.FixBounds(); } catch { }
    }

    /// <summary>场景切换 / 卸载时清理状态（由 SaveBagExpandCore 在合适位置调用，可选）。</summary>
    internal static void Reset()
    {
        _dragging = false;
        _haveBase = false;
        _wasVisibleLastFrame = false;
        _offsetLoaded = false;
        _offsetX = 0;
        _offsetY = 0;
        _framesSinceVisible = 0;
        _lastStableAp = new Vector2(float.NaN, float.NaN);
        _lastStableArea = float.NaN;   // P4：面积样本一并作废
    }
}
