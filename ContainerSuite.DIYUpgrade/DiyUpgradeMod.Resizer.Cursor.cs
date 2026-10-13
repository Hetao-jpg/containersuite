// 由拆解脚本机械切分，内容逐字未改（唯一改动：ContainerWindowResizer 加 partial）。
// 职责：拖拽手柄 ResizeHandle + 光标资源 ResizeCursor

// 由拆解脚本从 自定义集装箱fix/DiyUpgradeMod.cs 机械切分，内容逐字未改。
// 职责：拖拽缩放窗口（ContainerWindowResizer 整体，待二次拆分）
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
    internal static partial class ContainerWindowResizer
    {
        private sealed class ResizeHandle
        {
            internal TreeNodeRender Node { get; }
            internal ResizeEdge Edge { get; }
            internal Il2CppSystem.Func<PointerEventData, bool> Potential { get; }
            internal Il2CppSystem.Func<PointerEventData, bool> Begin { get; }
            internal Il2CppSystem.Func<PointerEventData, bool> Drag { get; }
            internal Il2CppSystem.Func<PointerEventData, bool> End { get; }
            internal Il2CppSystem.Func<PointerEventData, bool> Enter { get; }
            internal Il2CppSystem.Func<PointerEventData, bool> Exit { get; }

            internal ResizeHandle(TreeNodeRender node, ResizeEdge edge, Il2CppSystem.Func<PointerEventData, bool> potential, Il2CppSystem.Func<PointerEventData, bool> begin, Il2CppSystem.Func<PointerEventData, bool> drag, Il2CppSystem.Func<PointerEventData, bool> end, Il2CppSystem.Func<PointerEventData, bool> enter, Il2CppSystem.Func<PointerEventData, bool> exit)
            {
                Node = node; Edge = edge; Potential = potential; Begin = begin; Drag = drag; End = end; Enter = enter; Exit = exit;
            }
        }

        internal static class ResizeCursor
        {
            private enum Kind { Default, Horizontal, Vertical, DiagonalDown, DiagonalUp, Forbidden }
            private static readonly Dictionary<Kind, Texture2D> Textures = new Dictionary<Kind, Texture2D>();
            private static Kind _appliedCursor = Kind.Default;

            // 具名常量（原为散落的魔法值）：光标纹理热点、Cursor.SetCursor 的 mode 实参。
            private static readonly Vector2 CursorHotspot = new Vector2(16f, 16f);
            private const CursorMode SoftwareCursorMode = CursorMode.ForceSoftware;
            private const CursorMode AutoCursorMode = CursorMode.Auto;

            internal static void Set(ResizeEdge edge)
            {
                Kind kind;
                switch (edge)
                {
                    case ResizeEdge.Left:
                    case ResizeEdge.Right: kind = Kind.Horizontal; break;
                    case ResizeEdge.Top:
                    case ResizeEdge.Bottom: kind = Kind.Vertical; break;
                    case ResizeEdge.Left | ResizeEdge.Top:
                    case ResizeEdge.Right | ResizeEdge.Bottom: kind = Kind.DiagonalDown; break;
                    default: kind = Kind.DiagonalUp; break;
                }
                bool needsApply = _appliedCursor != kind;
                if (!needsApply && Textures.TryGetValue(kind, out var cached) && cached == null) { needsApply = true; _appliedCursor = Kind.Default; }
                if (needsApply)
                {
                    if (!Textures.TryGetValue(kind, out var tex) || tex == null) { tex = Create(kind); Textures[kind] = tex; }
                    Cursor.visible = true;
                    Cursor.SetCursor(tex, CursorHotspot, SoftwareCursorMode);
                    _appliedCursor = kind;
                }
            }

            internal static void SetForbidden()
            {
                if (_appliedCursor != Kind.Forbidden)
                {
                    if (!Textures.TryGetValue(Kind.Forbidden, out var tex) || tex == null) { tex = Create(Kind.Forbidden); Textures[Kind.Forbidden] = tex; }
                    Cursor.visible = true;
                    Cursor.SetCursor(tex, CursorHotspot, SoftwareCursorMode);
                    _appliedCursor = Kind.Forbidden;
                }
            }

            internal static void Reset()
            {
                if (_appliedCursor != Kind.Default)
                {
                    Cursor.SetCursor((Texture2D)null, Vector2.zero, AutoCursorMode);
                    _appliedCursor = Kind.Default;
                }
            }

            internal static void Invalidate()
            {
                foreach (Texture2D tex in Textures.Values) { try { Object.Destroy(tex); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); } }
                Textures.Clear();
                _appliedCursor = Kind.Default;
                try { Cursor.SetCursor((Texture2D)null, Vector2.zero, AutoCursorMode); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }

            internal static void Dispose()
            {
                Reset();
                foreach (Texture2D tex in Textures.Values) { try { Object.Destroy(tex); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); } }
                Textures.Clear();
            }

            // 具名常量（原为散落的魔法值 / 裸枚举强转）。
            private const int CursorTextureSize = 32;
            private const TextureFormat CursorTextureFormat = TextureFormat.RGBA32;
            private const FilterMode CursorFilterMode = FilterMode.Point;
            private const TextureWrapMode CursorWrapMode = TextureWrapMode.Clamp;

            private static Texture2D Create(Kind kind)
            {
                Texture2D tex = new Texture2D(CursorTextureSize, CursorTextureSize, CursorTextureFormat, false, false);
                tex.name = "ContainerResizeCursor" + kind;
                tex.filterMode = CursorFilterMode;
                tex.wrapMode = CursorWrapMode;
                Color transparent = new Color(0f, 0f, 0f, 0f);
                for (int i = 0; i < CursorTextureSize; i++) for (int j = 0; j < CursorTextureSize; j++) tex.SetPixel(i, j, transparent);
                Color white = Color.white;
                Color black = Color.black;
                Color color = new Color(1f, 0.2f, 0.2f, 1f);
                switch (kind)
                {
                    case Kind.Horizontal:
                        DrawLine(tex, 6, 16, 26, 16, 2, black); DrawLine(tex, 6, 16, 11, 11, 2, black);
                        DrawLine(tex, 6, 16, 11, 21, 2, black); DrawLine(tex, 26, 16, 21, 11, 2, black);
                        DrawLine(tex, 26, 16, 21, 21, 2, black); DrawLine(tex, 6, 16, 26, 16, 1, white);
                        DrawLine(tex, 6, 16, 11, 11, 1, white); DrawLine(tex, 6, 16, 11, 21, 1, white);
                        DrawLine(tex, 26, 16, 21, 11, 1, white); DrawLine(tex, 26, 16, 21, 21, 1, white);
                        break;
                    case Kind.Vertical:
                        DrawLine(tex, 16, 6, 16, 26, 2, black); DrawLine(tex, 16, 6, 11, 11, 2, black);
                        DrawLine(tex, 16, 6, 21, 11, 2, black); DrawLine(tex, 16, 26, 11, 21, 2, black);
                        DrawLine(tex, 16, 26, 21, 21, 2, black); DrawLine(tex, 16, 6, 16, 26, 1, white);
                        DrawLine(tex, 16, 6, 11, 11, 1, white); DrawLine(tex, 16, 6, 21, 11, 1, white);
                        DrawLine(tex, 16, 26, 11, 21, 1, white); DrawLine(tex, 16, 26, 21, 21, 1, white);
                        break;
                    case Kind.DiagonalDown:
                        DrawLine(tex, 6, 6, 26, 26, 2, black); DrawLine(tex, 6, 6, 6, 14, 2, black);
                        DrawLine(tex, 6, 6, 14, 6, 2, black); DrawLine(tex, 26, 26, 26, 18, 2, black);
                        DrawLine(tex, 26, 26, 18, 26, 2, black); DrawLine(tex, 6, 6, 26, 26, 1, white);
                        DrawLine(tex, 6, 6, 6, 14, 1, white); DrawLine(tex, 6, 6, 14, 6, 1, white);
                        DrawLine(tex, 26, 26, 26, 18, 1, white); DrawLine(tex, 26, 26, 18, 26, 1, white);
                        break;
                    case Kind.DiagonalUp:
                        DrawLine(tex, 26, 6, 6, 26, 2, black); DrawLine(tex, 26, 6, 26, 14, 2, black);
                        DrawLine(tex, 26, 6, 18, 6, 2, black); DrawLine(tex, 6, 26, 6, 18, 2, black);
                        DrawLine(tex, 6, 26, 14, 26, 2, black); DrawLine(tex, 26, 6, 6, 26, 1, white);
                        DrawLine(tex, 26, 6, 26, 14, 1, white); DrawLine(tex, 26, 6, 18, 6, 1, white);
                        DrawLine(tex, 6, 26, 6, 18, 1, white); DrawLine(tex, 6, 26, 14, 26, 1, white);
                        break;
                    case Kind.Forbidden:
                        DrawCircle(tex, 16, 16, 12, 2, black); DrawCircle(tex, 16, 16, 12, 1, color);
                        DrawLine(tex, 8, 8, 24, 24, 2, black); DrawLine(tex, 8, 8, 24, 24, 1, color);
                        break;
                }
                tex.Apply(false, false);
                return tex;
            }

            private static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, int radius, Color color)
            {
                int dx = Math.Abs(x1 - x0);
                int sx = ((x0 < x1) ? 1 : (-1));
                int dy = -Math.Abs(y1 - y0);
                int sy = ((y0 < y1) ? 1 : (-1));
                int err = dx + dy;
                while (true)
                {
                    for (int i = x0 - radius; i <= x0 + radius; i++)
                        for (int j = y0 - radius; j <= y0 + radius; j++)
                            if (i >= 0 && i < tex.width && j >= 0 && j < tex.height) tex.SetPixel(i, j, color);
                    if (x0 != x1 || y0 != y1)
                    {
                        int e2 = 2 * err;
                        if (e2 >= dy) { err += dy; x0 += sx; }
                        if (e2 <= dx) { err += dx; y0 += sy; }
                        continue;
                    }
                    break;
                }
            }

            private static void DrawCircle(Texture2D tex, int cx, int cy, int radius, int thickness, Color color)
            {
                for (int i = 0; i < 64; i++)
                {
                    float angle = (float)((double)i * 2.0 * Math.PI / 64.0);
                    int px = cx + (int)Math.Round((double)radius * Math.Cos(angle));
                    int py = cy + (int)Math.Round((double)radius * Math.Sin(angle));
                    for (int j = -thickness; j <= thickness; j++)
                        for (int k = -thickness; k <= thickness; k++)
                        {
                            int x = px + j;
                            int y = py + k;
                            if (x >= 0 && x < tex.width && y >= 0 && y < tex.height) tex.SetPixel(x, y, color);
                        }
                }
            }
        }

        private static readonly Dictionary<IntPtr, ResizeState> States = new Dictionary<IntPtr, ResizeState>();
        private static readonly List<IntPtr> _cleanupBuffer = new List<IntPtr>();
        private static bool _lastEnableDragResizeState = false;
        // T1（2026-10-12）：改单调钟节流 —— 原 DateTime 差值在时钟回拨时会一直判成
        //   "距上次不到 0.5 秒" ⇒ 手柄自检（LiveHandleCount 掉线就重建）**再也不跑**。
        private static PollThrottle _handleCheckGate;
        private static int _activeDragCount = 0;

        // （2026-10-13：_lastDragEdge / LastDragEdgeName 原为 ResizeDiag 记录拖拽方向用，
        //   随该探针一并删除。）
        // ═══════════════════════════════════════════════════════════════
        // S8r（2026-10-05）性能优化：DIY tick 常驻开销从 60~80μs 降到 <15μs。
        //
        // 背景：TD-09 性能基线显示，玩家静止不动时 DIY tick 依然消耗
        //       60~80μs / 帧（占 60fps 帧预算 0.4~0.5%），主因是每帧对
        //       所有可见 ResizeState 跑 raycast + overlay 同步。
        //
        // 判据：位置 / 左键 / 焦点 三者都没变 && 无正在进行拖拽 && 上一帧无 hover。
        //       任一条不成立 → 走完整 tick（正常逻辑）。
        //       全部成立 → 跳过整帧，但每 ForceTickEvery 帧强制跑一次完整 tick。
        //
        // 为什么每 6 帧要强制跑：ResizeState.Tick 里的 TryRestoreOnceOnVisible
        //       需要 _visibleSince 被记录（窗口可见后 0.05s 才触发恢复）。
        //       若鼠标永远不动、窗口永远"刚可见但还没记录过时间"，恢复就永不触发。
        //       每 6 帧兜底一次（≈0.1s @ 60fps），恢复延迟最多 0.1s，玩家无感。
        //
        // 为什么要求「上一帧无 hover」：鼠标静置期间窗口可能被外部关闭/隐藏，
        //       若直接跳过，光标会停留在 hover 态（视觉残留）。要求上一帧无 hover
        //       让「鼠标第一次移出手柄」这一帧一定被计算到。
        // ═══════════════════════════════════════════════════════════════
        private static Vector2 _lastPointerPos = new Vector2(-1f, -1f);
        private static bool _lastPointerHeld = false;
        private static bool _lastFocused = true;
        private static bool _lastHadHover = false;
        private static int _forceTickCountdown = 0;
        private const int ForceTickEvery = 6;

        internal static bool IsDragging => _activeDragCount > 0;

        // 【2026-10-10 P2】声明式预热：首行 `!IsResizableContainer(item) || ...`
        //   即 return —— null 哑参在第一个条件就短路，不会进 States 字典操作。
        //   ★ 只管拖拽手柄的挂载，不碰布局（与"改尺寸要 SetShape+Attach"是两回事）。
        [JitWarmup("挂载拖拽手柄：首行 IsResizableContainer(null) 即短路")]
        internal static void Attach(GameItem item)
        {
            if (!IsResizableContainer(item) || item.contentWindow == null || item.contentWindow.rectTransform == null) return;
            IntPtr pointer = ((Il2CppObjectBase)item.contentWindow).Pointer;
            if (!States.TryGetValue(pointer, out var rs) || !rs.IsAlive)
            {
                if (States.TryGetValue(pointer, out var stale) && stale != null) { try { stale.DestroyHandles(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); } }
                rs = new ResizeState(item);
                States[pointer] = rs;
                if (_enableDragResize.Value) rs.CreateHandles();
            }
            else
            {
                rs.Item = item;
                if (_enableDragResize.Value && rs.LiveHandleCount < ExpectedHandleCount) rs.CreateHandles();
                if (_enableDragResize.Value) rs.BringHandlesToFront();
            }
        }

        internal static void MarkRestored(GameItem item)
        {
            if (item == null) return;
            try
            {
                PixelWindow contentWindow = item.contentWindow;
                if (contentWindow != null)
                {
                    IntPtr pointer = ((Il2CppObjectBase)contentWindow).Pointer;
                    if (States.TryGetValue(pointer, out var rs)) rs.MarkRestoreDone();
                }
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
        }

        internal static void Tick()
        {
            // ═══════════════════════════════════════════════════════════════════════
            // 2026-09-29：门禁关闭时（读档 / 场景切换加载期）完全跳过拖拽手柄 tick。
            //
            // 三个原因：
            //   1. 加载期 State 里的 Window 引用可能已失效（场景卸载），对它做
            //      IsVisible() / RectTransformUtility 判定会触发无效 native 访问。
            //   2. ResizeState.Tick 内的 TryRestoreRememberedGridShape 会与
            //      XIAOWO / Wage 的窗口恢复链竞争同一个 GameGridInventory.SetShape，
            //      这正是 WorldReadyGate 要避免的。
            //   3. 加载期玩家不可能拖拽，全部 interop 调用都是浪费。
            //
            // 由 OnSceneWasLoaded 里的 WorldReadyGate.Invalidate() 关门，
            // 由 LoadGameRestorePatch.DelayedRestore 的 WorldReadyGate.MarkReady() 开门。
            // 门禁打开后下一帧，本方法自然恢复全部逻辑。
            // ═══════════════════════════════════════════════════════════════════════
            if (WorldReadyGate.ShouldDefer)
            {
                try { ResizeCursor.Reset(); } catch (System.Exception ex) { CatchLogOnce.Warn("Resizer:tick-gate-reset", ex); }
                return;
            }

            bool enabled = _enableDragResize.Value;
            if (enabled != _lastEnableDragResizeState)
            {
                _lastEnableDragResizeState = enabled;
                foreach (ResizeState rs in States.Values)
                {
                    try
                    {
                        if (enabled)
                        {
                            if (rs.LiveHandleCount < ExpectedHandleCount) rs.CreateHandles();
                            rs.BringHandlesToFront();
                        }
                        else rs.DestroyHandles();
                    }
                    catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                }
                if (!enabled) ResizeCursor.Reset();
            }
            if (States.Count > 0)
            {
                _cleanupBuffer.Clear();
                foreach (KeyValuePair<IntPtr, ResizeState> state in States)
                {
                    ResizeState rs = state.Value;
                    // Window/指针读取包进 try：已销毁的互操作对象访问 .Pointer 可能抛异常，
                    // 若任其逃逸会中断整个 Tick。读取失败即视为该 state 已失效（windowDead）。
                    bool windowDead;
                    try { windowDead = rs.Window == null || rs.Window.rectTransform == null || ((Il2CppObjectBase)rs.Window).Pointer == IntPtr.Zero; }
                    catch (System.Exception ex) { CatchLogOnce.Warn("Resizer:tick-cleanup-window", ex); windowDead = true; }
                    bool itemDead = false;
                    try { itemDead = rs.Item == null || !IsResizableContainer(rs.Item); }
                    catch (System.Exception ex) { CatchLogOnce.Warn("Resizer:tick-cleanup-item", ex); }
                    if (windowDead || itemDead)
                    {
                        try { rs.DestroyHandles(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                        _cleanupBuffer.Add(state.Key);
                    }
                }
                for (int i = 0; i < _cleanupBuffer.Count; i++) States.Remove(_cleanupBuffer[i]);
            }
            if (_enableDragResize.Value && States.Count > 0)
            {
                if (_handleCheckGate.Due(500))
                {
                    foreach (ResizeState rs in States.Values)
                    {
                        try
                        {
                            if (rs.LiveHandleCount < ExpectedHandleCount && rs.Window != null && rs.Window.rectTransform != null)
                                rs.CreateHandles();
                        }
                        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                    }
                }
            }
            Vector2 pointer;
            bool mouseButton;
            bool isFocused;
            try
            {
                Vector3 mousePosition = Input.mousePosition;
                pointer = new Vector2(mousePosition.x, mousePosition.y);
                mouseButton = Input.GetMouseButton(0);
                isFocused = Application.isFocused;
            }
            catch (System.Exception ex) { CatchLogOnce.Warn("Resizer:tick-input", ex); ResizeCursor.Reset(); return; }

            // S8r：鼠标完全没变 && 无拖拽 && 上一帧无 hover → 跳过整帧。
            //      _forceTickCountdown 每 6 帧兜底跑一次，保证 TryRestoreOnceOnVisible 能触发。
            bool pointerChanged = pointer.x != _lastPointerPos.x || pointer.y != _lastPointerPos.y;
            bool buttonChanged = mouseButton != _lastPointerHeld;
            bool focusChanged = isFocused != _lastFocused;
            if (!pointerChanged && !buttonChanged && !focusChanged
                && _activeDragCount == 0 && !_lastHadHover)
            {
                if (_forceTickCountdown > 0)
                {
                    _forceTickCountdown--;
                    return;
                }
            }
            _forceTickCountdown = ForceTickEvery;
            _lastPointerPos = pointer;
            _lastPointerHeld = mouseButton;
            _lastFocused = isFocused;

            ResizeEdge hoverEdge = ResizeEdge.None;
            bool bestIsActive = false;
            int bestOrder = int.MinValue;
            foreach (ResizeState rs in States.Values)
            {
                try
                {
                    bool isActive;
                    int order;
                    ResizeEdge edgeResult = rs.Tick(pointer, mouseButton, isFocused, out isActive, out order);
                    if (edgeResult != ResizeEdge.None && ((isActive && !bestIsActive) || (isActive == bestIsActive && order >= bestOrder)))
                    {
                        hoverEdge = edgeResult;
                        bestIsActive = isActive;
                        bestOrder = order;
                    }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }
            if (hoverEdge == ResizeEdge.None) ResizeCursor.Reset();
            else ResizeCursor.Set(hoverEdge);

            // S8r：记录本帧 hover 状态，供下帧的跳过判据使用。
            _lastHadHover = hoverEdge != ResizeEdge.None;
        }

        internal static void InvalidateCursor() { try { ResizeCursor.Invalidate(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); } }

        internal static void ResetAllRestoreDone()
        {
            foreach (ResizeState rs in States.Values) { try { rs.ResetRestoreDone(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); } }
        }

        internal static void Dispose()
        {
            foreach (ResizeState rs in States.Values) { try { rs.DestroyHandles(); } catch (System.Exception ex) { CatchLogOnce.Warn(ex); } }
            States.Clear();
            ResizeCursor.Dispose();
        }
    }
}
