using HarmonyLib;
using Il2Cpp;
using MelonLoader;
using System;
using ContainerSuite.Shared;   // S2 新增
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SaveBagExpand;

public static partial class SaveBagPaging
{
    // ────────── TAB 循环切页 ──────────

    /// <summary>
    /// 左键是否按着。★ 这**不是**"在拖物品" —— 框选 / 拖窗口 / 拖容器边缘改尺寸
    /// 都会让左键按着。本判据只作为"拖拽判定不可用"时的保守回退（语义与改动前一致）。
    /// </summary>
    private static bool IsMouseButtonHeld()
    {
        // 输入模块不可用属无害情形，静默忽略。
        try { return Input.GetMouseButton(0); } catch { return false; }
    }

    /// <summary>TAB 切页时的拖拽判定结果。</summary>
    private enum TabDragVerdict
    {
        /// <summary>没在拖单件物品（后续交给左键判据决定）。</summary>
        NotDragging,
        /// <summary>在拖单件物品，且可以安全切页（<c>held</c> = 手上那件）。</summary>
        SingleItemDrag,
        /// <summary>必须拦截（原因见 blockReason）。</summary>
        Block,
        /// <summary>游戏信号拿不到（异常）⇒ 调用方退回左键判据。</summary>
        Unavailable,
    }

    /// <summary>
    /// 2026-10-12：TAB 切页的拖拽判定。
    /// <para>
    /// <b>为什么改</b>：原判据是"左键按着"（<see cref="IsMouseButtonHeld"/>），于是
    /// <b>拖着物品时按 TAB 完全没反应</b> —— 玩家要把物品从第 1 页搬到第 2 页只能
    /// "放下 → TAB → 再拿起 → 再放下"。游戏自带
    /// <c>ItemMouseDragHandler.current.IsDraggingItem</c> 与 <c>GetCurrentItem()</c>，
    /// 可以精确判定"是否在拖物品"并拿到手上那件（**不需要**反查偏移）。
    /// </para>
    /// <para>
    /// ★ 保守边界（宁可少放开一个场景，也不动不确定的东西）：
    /// · <b>框选拖拽进行中一律拦截</b>：那一套会把选中物品变成"幽灵"并记下
    ///   <c>selectedHomes</c>，中途换页会把它们连同选中集一起搬走（可能复制/丢失）。
    ///   判据是 <c>ItemMultiSelectHandler.HasSelection</c> —— 它在 <c>EndGroupDrag</c>
    ///   里被 <c>ClearSelection</c> 清掉，所以只在拖拽途中为真。
    /// · <b>拿不到手上那件时拦截</b>（无法保证切页时跳过它）。
    /// · <b>游戏信号异常时返回 Unavailable</b>，由调用方退回左键判据。
    /// </para>
    /// </summary>
    private static TabDragVerdict ProbeTabDrag(out GameItem held, out string blockReason)
    {
        held = null;
        blockReason = null;
        try
        {
            ItemMouseDragHandler drag = ItemMouseDragHandler.current;
            if (drag == null || !drag.IsDraggingItem) return TabDragVerdict.NotDragging;

            ItemMultiSelectHandler multi = ItemMultiSelectHandler.current;
            if (multi != null && multi.HasSelection)
            {
                blockReason = "框选拖拽进行中（选中集会被换页搬走）";
                return TabDragVerdict.Block;
            }

            held = drag.GetCurrentItem();
            if (held == null)
            {
                blockReason = "正在拖物品但拿不到它（无法保证切页时跳过）";
                return TabDragVerdict.Block;
            }
            return TabDragVerdict.SingleItemDrag;
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("paging.tabDragProbe", ex);
            blockReason = "拖拽判定不可用";
            return TabDragVerdict.Unavailable;
        }
    }

    /// <summary>手上那件的可读描述（诊断用；拿不到就返回 "?"）。</summary>
    private static string DescribeHeld(GameItem held)
    {
        try
        {
            if (held == null) return "(null)";
            string id = held.identifier;
            return string.IsNullOrEmpty(id) ? "(空id)" : id;
        }
        catch { return "(读取失败)"; }
    }

    /// <summary>主网格里是否物理存在这个指针的物品（用于判定"幽灵式"还是"已离网格"）。</summary>
    private static bool GridContainsPtr(GameGridInventory grid, IntPtr ptr)
    {
        if (grid == null || ptr == IntPtr.Zero) return false;
        try
        {
            var items = grid.childItems;
            if (items == null) return false;
            for (int i = 0; i < items.Count; i++)
            {
                try { if (items[i] != null && items[i].Pointer == ptr) return true; }
                catch { }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.GridContainsPtr", ex); }
        return false;
    }

    // ══════════════════════════════════════════════════════════════════════════
    //  拖拽中 TAB 换页：复原"跟随鼠标的物品图"（2026-10-12）
    // ══════════════════════════════════════════════════════════════════════════
    //
    // 【机制】原生汇编取证（GameAssembly.dll + Il2CppDumper 的 dump.cs/script.json）：
    //   · 拖拽时物品**仍留在网格里**：ItemMouseDragHandler.StartDrag 只做
    //     UpdateFakeReal / ToggleFakeBackground / 隐藏 contentWindow·examineWindow
    //     / 收集子物品，**从不 Expel**；跟着鼠标走的是该元素自己的"假体"节点
    //     （fakeInner/fakeMiddle/fakeRotator/fakeBackground）。
    //   · GameItemElement.ToggleFakeBackground(enabled, color) @0x78C3A0：
    //       enabled=true  → SetParent(假体 → RenderHandler 静态实例的 Overlay 层
    //                        [RenderHandler+0xb8]→[+0x50])；enabled=false → SetParent 回
    //                        元素自己的 handler（就是网格里那一格）。
    //   · ★ GameItemElement.ValidateSelectLocked @0x78E5E0 里有一条
    //     **无条件**的 ToggleFakeBackground(enabled=false)（0x18078E8FC: xor edx,edx）。
    //     ValidateSelectLocked ← ValidateSelect ← GameItemElement.Validate()（虚槽），
    //     由网格在内容/贴图变化时对本格每个元素逐个调用。
    //   · 于是我们拖拽中切页（Expel + UncheckedAccept 整格物品，每一件都会走
    //     GameItem.ValidateShapeState 与 OverlayHandler.UpdateInventoryStateUI）
    //     ⇒ 拖拽元素被 Validate ⇒ 假体被拆回网格那一格并禁用。
    //     表现完全对得上：绿色落点照常跟手（Drag 里的 raycast + RefreshDragHighlight
    //     不受影响），但物品图回到原格、不再跟手 —— 且**静默**（MovePosition /
    //     OnEventUpdate 只判 fakeMiddle 非空，被拆回网格的指针依然有效 ⇒ 不报错、
    //     不中止拖拽）。
    //
    // 【修法】用游戏自己的 GameItemElement.UpdateFakeReal(pickedUp) @0x78CB40 复原：
    //   它内部就是 ToggleFakeBackground(enabled = pickedUp > 0, color) +
    //   按 pickedUp 分配真/假背景与堆叠数字，所以
    //     · 假体重新挂回 Overlay 层、恢复跟手；
    //     · **拖拽堆叠物品时按堆叠语义复原**（真体显示 unitCount-pickedUp，
    //       假体带 ×pickedUp 的数字）。
    //   这是"复用游戏自己的状态机"，不是我们自己摆弄它的节点树。
    //
    // 【不做的】不改 fakeMiddle/fakeBackground 指针，不自己 SetParent，不碰
    //   GridShapeBuilder —— 那些都是内部布局状态，手改容易把网格搞坏。
    // ══════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 若此刻正在拖单件物品，则把它的"假体"（跟随鼠标的物品图）按当前拾取数复原。
    /// 幂等、安全：不在拖拽中时第一行就返回；全程 try/catch 不外溢。
    /// </summary>
    private static void ReassertDragGhostIfDragging()
    {
        try
        {
            ItemMouseDragHandler drag = ItemMouseDragHandler.current;
            if (drag == null || !drag.IsDraggingItem) return;

            GameItemElement el = drag.currentItem;   // 手上那件的元素（拖拽期间它仍在网格里）
            if (el == null) return;

            int picked = drag.currentRemove;         // 游戏自己的"当前拾取数"（滚轮可调）
            if (picked <= 0)
            {
                // 读不到拾取数时退回"整件"：UpdateFakeReal(0) 会把假体拆掉，
                // 那正是我们要修的现象，所以绝不能传 0。
                try { picked = el.unitCount; } catch { picked = 0; }
            }
            if (picked <= 0) return;                 // 两种都读不到 ⇒ 宁可不碰

            el.UpdateFakeReal(picked);

            // 无条件结构化标记：这是"切页后假体确实复原过"的唯一机器可读证据。
            ContainerSuite.Shared.ContainerSuiteLog.Mark("sbe.tab.reassertGhost", true, "picked=" + picked);
            SaveBagExpandCore.Diag("[SaveBagExpand] 拖拽中切页：已复原物品假体（拾取 " + picked + " 件）");
        }
        catch (Exception ex) { CatchLogOnce.Warn("paging.reassertDragGhost", ex); }
    }

    /// <summary>
    /// 切页后若干帧内重试复原假体。网格的 Validate 有可能不发生在我们的调用栈里
    /// （例如紧随其后的窗口刷新），所以多给两帧；计数器归零后常态零开销。
    /// 由 SaveBagExpandCore.Tick 每帧调用。
    /// </summary>
    internal static void TickDragGhostReassert()
    {
        if (_ghostReassertFrames <= 0) return;
        _ghostReassertFrames--;
        ReassertDragGhostIfDragging();
    }

}
