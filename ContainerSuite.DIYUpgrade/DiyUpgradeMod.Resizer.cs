// 职责：拖拽状态机 ResizeState + ResizeEdge 枚举

// 职责：拖拽缩放窗口（ContainerWindowResizer 整体）
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
        // 手柄数量（8 = 与 XIAOWO 手柄组对齐，含独立 Top 边条；补齐 Top 才让"从上压"回到本 mod 链路）。
        // 为什么：见 docs/diy-resizer.md
        internal const int ExpectedHandleCount = 8;

        [Flags]
        internal enum ResizeEdge { None = 0, Left = 1, Right = 2, Top = 4, Bottom = 8 }

        private sealed class ResizeState
        {
            private readonly List<ResizeHandle> _handles = new List<ResizeHandle>();
            private TreeNodeRender _overlay;
            private ResizeEdge _activeEdge;
            private ResizeEdge _hoveredEdge;
            private Vector2 _startPointer;
            private Vector2 _startPosition;
            private Vector2 _startPivot;
            private int _startWidth;
            private int _startHeight;
            private int _startColumns;
            private int _startRows;
            private float _columnPixels;
            private float _rowPixels;
            private float _visibleSince = -1f;
            private bool _restoreDone;

            internal GameItem Item { get; set; }
            internal PixelWindow Window { get; }

            internal bool IsAlive
            {
                get
                {
                    try
                    {
                        if (Window == null || Window.rectTransform == null) return false;
                        // 2026-10-15：加 null 检查。配置未就绪时 _enableDragResize 可能为 null，
                        //   原写法 .Value 抛 NRE 被外层 catch 吞掉 → 返回 false → 被当成"已死"
                        //   → Attach 反复重建手柄。语义上"读不到配置"应视为"不启用拖拽"（返回 true）。
                        if (_enableDragResize == null || !_enableDragResize.Value) return true;
                        return _handles.Count == ExpectedHandleCount && LiveHandleCount == ExpectedHandleCount;
                    }
                    catch { return false; }
                }
            }

            internal int LiveHandleCount
            {
                get
                {
                    int live = 0;
                    foreach (ResizeHandle handle in _handles)
                    {
                        try
                        {
                            TreeNodeRender node = handle.Node;
                            if (node != null && node.rectTransform != null) live++;
                        }
                        catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                    }
                    return live;
                }
            }

            internal bool IsActiveResize => _activeEdge != ResizeEdge.None;

            internal ResizeState(GameItem item) { Item = item; Window = item.contentWindow; }

            // 2026-09-29：拆成主入口 + 若干小方法（IsWindowVisible / TrackVisibility /
            //   TryRestoreOnceOnVisible 等），缩小单个方法体，避免触发 JIT 大方法问题。
            internal ResizeEdge Tick(Vector2 pointer, bool pointerHeld, bool focused, out bool isActive, out int order)
            {
                bool visible = IsWindowVisible();
                TrackVisibility(visible);
                TryRestoreOnceOnVisible(visible);
                order = GetWindowOrder();
                isActive = false;

                if (!visible || !focused || !_enableDragResize.Value)
                {
                    FinishDrag(null);
                    _hoveredEdge = ResizeEdge.None;
                    return ResizeEdge.None;
                }
                if (_activeEdge != ResizeEdge.None)
                {
                    if (pointerHeld) { isActive = true; return _activeEdge; }
                    FinishDrag(null);
                }
                SyncOverlay();
                _hoveredEdge = FindHandleAtPointer(pointer, null);
                return _hoveredEdge;
            }

            private bool IsWindowVisible()
            {
                try
                {
                    PixelWindow w = Window;
                    if (w == null || w.rectTransform == null) return false;
                    return ((Component)w.rectTransform).gameObject.activeInHierarchy;
                }
                catch { return false; }
            }

            private void TrackVisibility(bool visible)
            {
                if (visible && _visibleSince < 0f) _visibleSince = Time.time;
            }

            private void TryRestoreOnceOnVisible(bool visible)
            {
                if (!visible || _restoreDone || _visibleSince <= 0f) return;
                if (Time.time - _visibleSince <= 0.05f) return;
                try
                {
                    TryRestoreRememberedGridShape(Item);
                    if (_enableDragResize.Value) BringHandlesToFront();
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                _restoreDone = true;
            }

            internal void CreateHandles()
            {
                DestroyHandles();
                CreateOverlay();
                AddHandle("Left", ResizeEdge.Left, new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(12f, -32f));
                AddHandle("Right", ResizeEdge.Right, new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(12f, -32f));
                AddHandle("Bottom", ResizeEdge.Bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(-32f, 12f));
                // Top 边手柄：小窝有、我们原先没有 —— 从上压因此漏到小窝链路（见 ExpectedHandleCount 注释）。
                AddHandle("Top", ResizeEdge.Top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(-32f, 12f));
                AddHandle("TopLeft", ResizeEdge.Left | ResizeEdge.Top, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f, 20f));
                AddHandle("TopRight", ResizeEdge.Right | ResizeEdge.Top, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(20f, 20f));
                AddHandle("BottomLeft", ResizeEdge.Left | ResizeEdge.Bottom, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 20f));
                AddHandle("BottomRight", ResizeEdge.Right | ResizeEdge.Bottom, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(20f, 20f));
            }

            internal void BringHandlesToFront()
            {
                SyncOverlay();
                try
                {
                    TreeNodeRender overlay = _overlay;
                    if (overlay != null && overlay.rectTransform != null)
                        ((Transform)overlay.rectTransform).SetAsLastSibling();
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                foreach (ResizeHandle handle in _handles)
                {
                    try
                    {
                        TreeNodeRender node = handle.Node;
                        if (node != null && node.rectTransform != null)
                            ((Transform)node.rectTransform).SetAsLastSibling();
                    }
                    catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                }
                try
                {
                    PixelWindow window = Window;
                    if (window != null)
                    {
                        TreeNodeRender topCloseButton = window.topCloseButton;
                        if (topCloseButton != null && topCloseButton.rectTransform != null)
                            ((Transform)topCloseButton.rectTransform).SetAsLastSibling();
                    }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }

            internal void DestroyHandles()
            {
                foreach (ResizeHandle handle in _handles)
                {
                    try
                    {
                        TreeNodeRender node = handle.Node;
                        if (node != null && node.rectTransform != null)
                            Object.Destroy(((Component)handle.Node.rectTransform).gameObject);
                    }
                    catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                }
                try
                {
                    TreeNodeRender overlay = _overlay;
                    if (overlay != null && overlay.rectTransform != null)
                        Object.Destroy(((Component)_overlay.rectTransform).gameObject);
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                // 若销毁时仍处于拖拽中，归还一次全局拖拽计数。
                // 与 BeginDrag 的 ++ / FinishDrag 的 -- 严格配对：此处 _activeEdge 在同一段里被清零，
                // 之后 FinishDrag 会因 _activeEdge==None 提前返回，不会重复减计数（也不会变负）。
                if (_activeEdge != ResizeEdge.None && _activeDragCount > 0) _activeDragCount--;
                _handles.Clear();
                _overlay = null;
                _activeEdge = ResizeEdge.None;
                _hoveredEdge = ResizeEdge.None;
            }

            private void CreateOverlay()
            {
                _overlay = TreeNodeRender.Instantiate(Window, "ContainerResizeOverlay");
                if (_overlay == null || _overlay.rectTransform == null) { _overlay = null; return; }
                ((Transform)_overlay.rectTransform).SetParent((Transform)(object)Window.rectTransform, false);
                Image image = _overlay.image;
                if (image != null)
                {
                    ((Behaviour)image).enabled = false;
                    ((Graphic)image).raycastTarget = false;
                }
                if (_overlay.mask != null) ((Behaviour)_overlay.mask).enabled = false;
                SyncOverlay();
                ((Transform)_overlay.rectTransform).SetAsLastSibling();
            }

            internal void SyncOverlay()
            {
                PixelWindow window = Window;
                RectTransform borderRt = ((window == null || window.outsideBorder == null) ? null : window.outsideBorder.rectTransform);
                RectTransform overlayRt = ((_overlay == null) ? null : _overlay.rectTransform);
                if (borderRt != null && overlayRt != null)
                {
                    overlayRt.anchorMin = borderRt.anchorMin;
                    overlayRt.anchorMax = borderRt.anchorMax;
                    overlayRt.pivot = borderRt.pivot;
                    overlayRt.anchoredPosition = borderRt.anchoredPosition;
                    overlayRt.sizeDelta = borderRt.sizeDelta;
                    ((Transform)overlayRt).localScale = Vector3.one;
                }
            }

            private void AddHandle(string name, ResizeEdge edge, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
            {
                TreeNodeRender handleNode = TreeNodeRender.Instantiate(Window, "ContainerResize" + name);
                if (handleNode != null && handleNode.rectTransform != null)
                {
                    RectTransform rectTransform = handleNode.rectTransform;
                    RectTransform parentRt = ((_overlay != null) ? _overlay.rectTransform : ((Window.outsideBorder == null) ? Window.rectTransform : Window.outsideBorder.rectTransform));
                    ((Transform)rectTransform).SetParent((Transform)(object)parentRt, false);
                    rectTransform.anchorMin = anchorMin;
                    rectTransform.anchorMax = anchorMax;
                    rectTransform.pivot = new Vector2(0.5f, 0.5f);
                    rectTransform.anchoredPosition = Vector2.zero;
                    rectTransform.sizeDelta = sizeDelta;
                    ((Transform)rectTransform).localScale = Vector3.one;
                    ((Transform)rectTransform).SetAsLastSibling();
                    Image image = handleNode.image;
                    if (image == null) image = ((Component)rectTransform).gameObject.AddComponent<Image>();
                    handleNode.image = image;
                    ((Behaviour)image).enabled = true;
                    ((Graphic)image).color = new Color(1f, 1f, 1f, 0.001f);
                    ((Graphic)image).raycastTarget = true;
                    handleNode.InitEventHandler();
                    Il2CppSystem.Func<PointerEventData, bool> onPotentialDrag = Convert((PointerEventData data) => PrepareDrag(data));
                    Il2CppSystem.Func<PointerEventData, bool> onBeginDrag = Convert((PointerEventData data) => BeginDrag(data, edge));
                    Il2CppSystem.Func<PointerEventData, bool> onDrag = Convert((PointerEventData data) => Drag(data));
                    Il2CppSystem.Func<PointerEventData, bool> onEndDrag = Convert((PointerEventData data) => EndDrag(data));
                    Il2CppSystem.Func<PointerEventData, bool> onEnter = Convert((PointerEventData data) => EnterHandle(data, edge));
                    Il2CppSystem.Func<PointerEventData, bool> onExit = Convert((PointerEventData data) => ExitHandle(data, edge));
                    handleNode.eventHandler.mousePotentialDragFunc.Add(onPotentialDrag);
                    handleNode.eventHandler.mouseBeginDragFunc.Add(onBeginDrag);
                    handleNode.eventHandler.mouseDragFunc.Add(onDrag);
                    handleNode.eventHandler.mouseEndDragFunc.Add(onEndDrag);
                    handleNode.eventHandler.mouseEnterFunc.Add(onEnter);
                    handleNode.eventHandler.mouseExitFunc.Add(onExit);
                    _handles.Add(new ResizeHandle(handleNode, edge, onPotentialDrag, onBeginDrag, onDrag, onEndDrag, onEnter, onExit));
                }
            }

            private static Il2CppSystem.Func<PointerEventData, bool> Convert(System.Func<PointerEventData, bool> action)
            {
                return DelegateSupport.ConvertDelegate<Il2CppSystem.Func<PointerEventData, bool>>(action);
            }

            private bool PrepareDrag(PointerEventData data)
            {
                if (data != null) data.useDragThreshold = false;
                return true;
            }

            private bool BeginDrag(PointerEventData data, ResizeEdge edge)
            {
                if (data != null && Window != null && Window.rectTransform != null && TryPointerInParent(data, out var point))
                {
                    _startPointer = point;
                    GameGridInventory resizableInventory = GetResizableInventory(Item);
                    if (resizableInventory == null || resizableInventory.inventoryShape == null) return false;
                    // 仅在一次全新拖拽时 ++（同状态重复 BeginDrag 不重复计数），
                    // 与 FinishDrag / DestroyHandles 的 -- 严格配对，避免计数泄漏或变负。
                    if (_activeEdge == ResizeEdge.None) _activeDragCount++;
                    _activeEdge = edge;
                    ResizeCursor.Set(edge);
                    _startWidth = Window.widthPixels;
                    _startHeight = Window.heightPixels;

                    // AlignSlots 基准统一：升级路径 PrepareGrid 用「所有启用槽中最大的当前尺寸」为基准，
                    // 拖拽路径也必须用同一基准，否则拖拽会试图把其它槽缩小 → 被 CanContainItems 挡住。
                    GridDef gridDef = GetGridDefForItem(Item);
                    bool alignSlots = (gridDef?.AlignSlots?.Value ?? false);
                    GameGridInventory baseInv = resizableInventory;
                    int baseCols = resizableInventory.inventoryShape.width;
                    int baseRows = resizableInventory.inventoryShape.height;
                    if (alignSlots && gridDef != null)
                    {
                        List<GameGridInventory> allGrids = GetAllGridInventories(Item);
                        foreach (var si in GetEnabledSlotsWithIndex(gridDef))
                        {
                            if (si.Index < 0 || si.Index >= allGrids.Count) continue;
                            GameGridInventory g = allGrids[si.Index];
                            if (g?.inventoryShape == null) continue;
                            if (g.inventoryShape.width > baseCols) { baseCols = g.inventoryShape.width; baseInv = g; }
                            if (g.inventoryShape.height > baseRows) baseRows = g.inventoryShape.height;
                        }
                    }
                    _startColumns = baseCols;
                    _startRows = baseRows;
                    _columnPixels = Math.Max(1f, (float)baseInv.widthPixels / (float)Math.Max(1, baseInv.inventoryShape.width));
                    _rowPixels = Math.Max(1f, (float)baseInv.heightPixels / (float)Math.Max(1, baseInv.inventoryShape.height));
                    _startPosition = Window.rectTransform.anchoredPosition;
                    _startPivot = Window.rectTransform.pivot;
                    Window.ToFront(force: true);
                    ((AbstractEventData)data).Use();
                    BringHandlesToFront();
                    return true;
                }
                return false;
            }

            private bool Drag(PointerEventData data)
            {
                if (_activeEdge == ResizeEdge.None || data == null || !TryPointerInParent(data, out var point)) return false;
                ResizeCursor.Set(_activeEdge);
                Vector2 drag = point - _startPointer;
                int newW = _startWidth;
                int newH = _startHeight;
                if ((_activeEdge & ResizeEdge.Left) != ResizeEdge.None) newW = (int)Math.Round((float)_startWidth - drag.x);
                else if ((_activeEdge & ResizeEdge.Right) != ResizeEdge.None) newW = (int)Math.Round((float)_startWidth + drag.x);
                if ((_activeEdge & ResizeEdge.Bottom) != ResizeEdge.None) newH = (int)Math.Round((float)_startHeight - drag.y);
                else if ((_activeEdge & ResizeEdge.Top) != ResizeEdge.None) newH = (int)Math.Round((float)_startHeight + drag.y);
                int newCols = _startColumns;
                int newRows = _startRows;
                if ((_activeEdge & (ResizeEdge.Left | ResizeEdge.Right)) != ResizeEdge.None) newCols += (int)Math.Round((float)(newW - _startWidth) / _columnPixels);
                if ((_activeEdge & (ResizeEdge.Top | ResizeEdge.Bottom)) != ResizeEdge.None) newRows += (int)Math.Round((float)(newH - _startHeight) / _rowPixels);
                GameGridInventory resizableInventory = GetResizableInventory(Item);
                if (resizableInventory == null || resizableInventory.inventoryShape == null) return false;
                if (!TryGetGridShapeLimits(Item, out var minC, out var minR, out var maxC, out var maxR)) return false;
                newCols = Math.Max(minC, Math.Min(maxC, newCols));
                newRows = Math.Max(minR, Math.Min(maxR, newRows));
                int width = resizableInventory.inventoryShape.width;
                int height = resizableInventory.inventoryShape.height;
                if (newCols != width || newRows != height)
                {
                    bool allowed = true;
                    foreach (GameGridInventory allGridInventory in GetAllGridInventories(Item))
                    {
                        if (!CanContainItems(allGridInventory, newCols, newRows)) { allowed = false; break; }
                    }
                    if (!allowed) { ResizeCursor.SetForbidden(); ((AbstractEventData)data).Use(); return true; }
                    if (!TryResizeGridWindow(Item, newCols, newRows, remember: false, out var actualWidth, out var actualHeight))
                    {
                        ResizeCursor.SetForbidden(); ((AbstractEventData)data).Use(); return true;
                    }
                    int deltaW = actualWidth - _startWidth;
                    int deltaH = actualHeight - _startHeight;
                    Vector2 startPosition = _startPosition;
                    if ((_activeEdge & ResizeEdge.Left) != ResizeEdge.None) startPosition.x -= (1f - _startPivot.x) * (float)deltaW;
                    else if ((_activeEdge & ResizeEdge.Right) != ResizeEdge.None) startPosition.x += _startPivot.x * (float)deltaW;
                    if ((_activeEdge & ResizeEdge.Bottom) != ResizeEdge.None) startPosition.y -= (1f - _startPivot.y) * (float)deltaH;
                    else if ((_activeEdge & ResizeEdge.Top) != ResizeEdge.None) startPosition.y += _startPivot.y * (float)deltaH;
                    Window.rectTransform.anchoredPosition = startPosition;
                }
                ((AbstractEventData)data).Use();
                return true;
            }

            private bool EndDrag(PointerEventData data)
            {
                if (!FinishDrag(data)) return false;
                ResizeEdge hoveredEdge = FindHandleAtPointer(data);
                if (data != null) _hoveredEdge = hoveredEdge;
                if (_hoveredEdge == ResizeEdge.None) ResizeCursor.Reset();
                else ResizeCursor.Set(_hoveredEdge);
                if (data != null) ((AbstractEventData)data).Use();
                return true;
            }

            // 2026-09-29：拆成 6 个小方法。原方法体 45 行 + 3 层 try-catch +
            // 字符串插值 + 迭代器启动，在 MonoMod 首次 JIT 编译时触发
            // CLR 0x80131506 崩溃（与 TryRestoreRememberedGridShape 同源）。
            private bool FinishDrag(PointerEventData data)
            {
                if (_activeEdge == ResizeEdge.None) return false;

                _activeEdge = ResizeEdge.None;
                if (_activeDragCount > 0) _activeDragCount--;
                PersistShapeOnFinish();
                RebuildVisualsOnFinish();
                SchedulePostDragRefresh();
                ResizeCursor.Reset();
                return true;
            }

            private void PersistShapeOnFinish()
            {
                try
                {
                    List<GameGridInventory> grids = GetAllGridInventories(Item);
                    WriteSlotTags(grids);
                    WritePrimaryTags(grids);
                    RememberWindowSize(Item, Window.widthPixels, Window.heightPixels);
                    Window.FixBounds();
                    Item.RememberContentWindowPositionIfMine(Window);
                    SyncOverlay();
                    Attach(Item);
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }

            private void WriteSlotTags(List<GameGridInventory> grids)
            {
                try
                {
                    GridDef def = GetGridDefForItem(Item);
                    foreach (var si in GetEnabledSlotsWithIndex(def))
                    {
                        int idx = si.Index;
                        if (idx < 0 || idx >= grids.Count) continue;
                        GameGridInventory g = grids[idx];
                        if (g?.inventoryShape == null) continue;
                        RememberSlotShape(Item, idx, g.inventoryShape.width, g.inventoryShape.height);
                    }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }

            private void WritePrimaryTags(List<GameGridInventory> grids)
            {
                try
                {
                    // 主槽 = 首个启用槽，与 GetResizableInventory / WriteSlotTags 判定一致。
                    // 不能用 grids[0]：主槽索引非 0 时会把错误的网格尺寸写进主标签。
                    GridDef def = GetGridDefForItem(Item);
                    int primaryIdx = -1;
                    foreach (var si in GetEnabledSlotsWithIndex(def))
                    {
                        if (si.Index >= 0 && si.Index < grids.Count) { primaryIdx = si.Index; break; }
                    }
                    if (primaryIdx < 0 || grids[primaryIdx]?.inventoryShape == null) return;
                    TagUtil.SetItemTag(Item, "DIY_UPGRADE_W_TAG", grids[primaryIdx].inventoryShape.width);
                    TagUtil.SetItemTag(Item, "DIY_UPGRADE_H_TAG", grids[primaryIdx].inventoryShape.height);
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }

            private void RebuildVisualsOnFinish()
            {
                try
                {
                    if (Window == null || Window.rectTransform == null) return;
                    LayoutRebuilder.ForceRebuildLayoutImmediate(Window.rectTransform);
                    if (Window.outsideBorder != null && Window.outsideBorder.rectTransform != null)
                        LayoutRebuilder.ForceRebuildLayoutImmediate(Window.outsideBorder.rectTransform);
                    Canvas.ForceUpdateCanvases();
                    RefreshAllGridsOf(Item);
                    Window.FixBounds();
                }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }

            private void SchedulePostDragRefresh()
            {
                try { MelonCoroutines.Start(ForceRefreshWindowAfterRestore(Item)); }
                catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
            }

            internal void MarkRestoreDone() { _restoreDone = true; }
            internal void ResetRestoreDone() { _restoreDone = false; _visibleSince = -1f; }

            private bool EnterHandle(PointerEventData data, ResizeEdge edge) { _hoveredEdge = edge; ResizeCursor.Set(edge); return true; }

            private bool ExitHandle(PointerEventData data, ResizeEdge edge)
            {
                if (_hoveredEdge == edge) _hoveredEdge = ResizeEdge.None;
                if (_activeEdge == ResizeEdge.None) ResizeCursor.Reset();
                return true;
            }

            private ResizeEdge FindHandleAtPointer(PointerEventData data)
            {
                if (data != null) return FindHandleAtPointer(data.position, data.pressEventCamera);
                return _hoveredEdge;
            }

            private ResizeEdge FindHandleAtPointer(Vector2 pointer, Camera camera)
            {
                if (IsPointerOverCloseButton(pointer, camera) || IsPointerOverTopDragBorder(pointer, camera)) return ResizeEdge.None;
                for (int i = _handles.Count - 1; i >= 0; i--)
                {
                    ResizeHandle handle = _handles[i];
                    try
                    {
                        TreeNodeRender node = handle.Node;
                        RectTransform rt = ((node != null) ? node.rectTransform : null);
                        if (rt != null && RectTransformUtility.RectangleContainsScreenPoint(rt, pointer, camera))
                            return handle.Edge;
                    }
                    catch (System.Exception ex) { CatchLogOnce.Warn(ex); }
                }
                return ResizeEdge.None;
            }

            private bool IsPointerOverCloseButton(Vector2 pointer, Camera camera)
            {
                try
                {
                    PixelWindow window = Window;
                    RectTransform closeRt = ((window != null && window.topCloseButton != null) ? window.topCloseButton.rectTransform : null);
                    if (closeRt == null || !((Component)closeRt).gameObject.activeInHierarchy) return false;
                    Vector2 localPoint;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(closeRt, pointer, camera, out localPoint)) return false;
                    Rect rect = closeRt.rect;
                    return localPoint.x >= rect.xMin - 2f && localPoint.x <= rect.xMax + 2f && localPoint.y >= rect.yMin - 2f && localPoint.y <= rect.yMax + 2f;
                }
                catch { return false; }
            }

            private bool IsPointerOverTopDragBorder(Vector2 pointer, Camera camera)
            {
                try
                {
                    PixelWindow window = Window;
                    RectTransform borderRt = ((window != null && window.topDragBorder != null) ? window.topDragBorder.rectTransform : null);
                    if (borderRt == null || !((Component)borderRt).gameObject.activeInHierarchy) return false;
                    Vector2 localPoint;
                    if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(borderRt, pointer, camera, out localPoint)) return false;
                    Rect rect = borderRt.rect;
                    return localPoint.x >= rect.xMin - 2f && localPoint.x <= rect.xMax + 2f && localPoint.y >= rect.yMin - 2f && localPoint.y <= rect.yMax + 2f;
                }
                catch { return false; }
            }

            private int GetWindowOrder()
            {
                try { return (Window != null && Window.rectTransform != null) ? ((Transform)Window.rectTransform).GetSiblingIndex() : int.MinValue; }
                catch { return int.MinValue; }
            }

            private bool TryPointerInParent(PointerEventData data, out Vector2 point)
            {
                point = Vector2.zero;
                PixelWindow window = Window;
                Transform parent = ((window != null && window.rectTransform != null) ? ((Transform)window.rectTransform).parent : null);
                if (parent == null) return false;
                return RectTransformUtility.ScreenPointToLocalPointInRectangle(new RectTransform(((Il2CppObjectBase)parent).Pointer), data.position, data.pressEventCamera, out point);
            }
        }
    }
}
