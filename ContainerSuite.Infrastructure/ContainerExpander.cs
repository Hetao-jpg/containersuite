using System;
using System.Collections.Generic;
using ContainerSuite.Shared;   // S2 新增
using Il2Cpp;
using Il2CppInterop.Runtime;             // P2：GetTagSetter 用 DelegateSupport
using Il2CppInterop.Runtime.InteropTypes;

namespace CustomInfrastructure;

internal static class ContainerExpander
{
    private const string TagAppliedW = "CUSTOMINFRA_APPLIED_W_V1";
    private const string TagAppliedH = "CUSTOMINFRA_APPLIED_H_V1";
    private const int MaxRecursionDepth = 64;

    private static volatile bool _isExpanding;

    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 P1】延迟扩展队列 —— 把 TryExpand 的实质工作搬离 Harmony trampoline
    //
    // 崩溃模型（十一轮实证定稿）：CLR fatal 0x80131506 发生在
    //   「嵌套 JIT」×「栈上有 detour trampoline」的交集处。
    //   GameItem.ValidateShapeState 是外部 mod（XIAOWO / ProbablyOptimized）挂载的
    //   **超热方法** ⇒ 我们的 Postfix 执行时，栈上必然躺着第三方 detour 的 trampoline。
    //   原实现 ValidateShapeStatePostfix 直接在 trampoline 上跑 TryExpand，
    //   而 TryExpand 的 callees（CollectGrids / Walk / SetShape /
    //   ForceRecomputeDynamicBackground / RefreshWindow）是**冷方法**、从未预热
    //   （读档链 JIT 预热默认关闭）⇒ 首次 JIT 恰好落在 trampoline 上 ⇒ 崩。
    //
    // 治本：Postfix 只做「入队」（指针去重 + 存活校验，全是已编译的托管代码，
    //   不触碰任何冷 IL2CPP 路径）；真正的 TryExpand 放到 Mod.OnUpdate 的
    //   **顶层帧**执行 —— 那时栈上没有任何第三方 trampoline，
    //   冷方法即便首次 JIT 也安全。
    //   ⇒ 与 RestoreArbiter「单写者 + 队列 + 每帧预算」同一套成熟范式，不另起炉灶。
    //
    // ★ 跨帧持有 IL2CPP 对象的唯一安全用法 = 照抄 RestoreArbiter 的存活校验
    //   （出队后比对 Pointer 是否漂移 + item.state 是否为空）。
    //   只靠 try/catch 挡不住 AccessViolation（进程直接猝死）。
    // ═══════════════════════════════════════════════════════════════════
    private static readonly Dictionary<IntPtr, GameItem> _pendingExpand
        = new Dictionary<IntPtr, GameItem>(16);
    private static readonly Queue<IntPtr> _pendingExpandOrder = new Queue<IntPtr>(16);

    /// <summary>队列容量上限（安全阀：巨型店铺一次性验证上千物品时不让字典膨胀）。</summary>
    private const int MaxPendingExpand = 256;
    /// <summary>每帧最多处理的物品数（与 RestoreArbiter 的「每帧 1 个」同理，避免单帧尖峰）。</summary>
    private const int ExpandsPerTick = 16;

    /// <summary>
    /// 请求扩展（由 ValidateShapeStatePostfix 在 trampoline 上调用）。
    /// <para>
    /// ★ 本方法**必须保持极小**：它跑在第三方 mod 的 detour 栈上，
    /// 只能调用**已编译**的代码。任何冷 IL2CPP 调用都不得出现在这里
    /// —— 那正是 P1 要消灭的东西。故这里连 identifier 都不读（交给出队后的 TryExpand）。
    /// </para>
    /// </summary>
    internal static void EnqueueExpand(GameItem container)
    {
        if (container == null) return;
        // 加载期不入队：与 TryExpand 的调用方语义一致（defer 期间一律不动网格）。
        try { if (WorldReadyGate.ShouldDefer) return; } catch { return; }

        IntPtr ptr = IntPtr.Zero;
        try { ptr = ((Il2CppObjectBase)container).Pointer; } catch { return; }
        if (ptr == IntPtr.Zero) return;

        if (_pendingExpand.ContainsKey(ptr)) return;        // 同帧/跨帧去重
        if (_pendingExpand.Count >= MaxPendingExpand)
        {
            // 2026-10-15：队列满时留痕。这是安全阀，正常情况不会触发；
            //   一旦真的触发，说明某帧游戏一次性验证了远超 256 个容器 ——
            //   需要知道这件事（否则"某容器没被扩展"会查无实据）。
            //   LogOnce 3 秒去重，不会刷屏。
            ContainerSuite.Shared.ContainerSuiteLog.Diag(
                "[容器扩展] 待处理队列已满（" + MaxPendingExpand + "），本次请求被丢弃。"
                + "若频繁出现请反馈（通常由一次性验证上千容器触发）。");
            return;
        }
        _pendingExpand[ptr] = container;
        _pendingExpandOrder.Enqueue(ptr);
    }

    /// <summary>
    /// 处理延迟扩展队列。挂在 Mod.OnUpdate（**顶层帧**，栈上无第三方 trampoline）。
    /// TryExpand 的冷 callees 即便在这里首次 JIT 也是安全的。
    /// </summary>
    internal static void ProcessExpandQueue()
    {
        if (_pendingExpandOrder.Count == 0) return;
        try { if (WorldReadyGate.ShouldDefer) return; } catch { return; }
        if (_isExpanding) return;   // 上一轮尚未收尾，等下一帧

        for (int i = 0; i < ExpandsPerTick; i++)
        {
            if (_pendingExpandOrder.Count == 0) break;

            IntPtr ptr = _pendingExpandOrder.Dequeue();
            GameItem item;
            if (!_pendingExpand.TryGetValue(ptr, out item)) continue;
            _pendingExpand.Remove(ptr);
            if (item == null) continue;

            // ── 存活校验（照抄 RestoreArbiter：跨帧持有的 IL2CPP 对象可能已被回收、
            //    地址还可能被新对象复用 ⇒ 只靠 try/catch 无效，必须比对指针 + state）
            try
            {
                if (((Il2CppObjectBase)item).Pointer != ptr) continue;
                if (item.state == null) continue;
            }
            catch { continue; }

            try { TryExpand(item); }
            catch (Exception ex) { CatchLogOnce.Warn("ContainerExpander.ProcessExpandQueue", ex); }
        }
    }

    /// <summary>场景切换 / 新档加载时清空（旧 GameItem 指针全部失效）。与 RestoreArbiter.ClearAll 同职责。</summary>
    internal static void ClearExpandQueue()
    {
        _pendingExpand.Clear();
        _pendingExpandOrder.Clear();
    }

    /// <summary>
    /// 尝试把容器扩展到配置的宽高。
    /// 只扩大不缩小：实际生效尺寸取 Max(当前值, 配置值)。
    /// forceRefresh=true 时，即使尺寸已达标也会刷新一次窗口 UI（用于 PixelWindow.Show 兜底）。
    /// </summary>
    internal static void TryExpand(GameItem container, bool forceRefresh = false)
    {
        if (container == null) return;
        if (_isExpanding) return;

        string id;
        try { id = container.identifier?.Replace("\0", "").Trim() ?? ""; }
        catch (Exception ex) { CatchLogOnce.Warn("ContainerExpander.TryExpand.identifier", ex); return; }
        if (string.IsNullOrEmpty(id)) return;

        int targetW = -1, targetH = -1;
        foreach (var cfg in CustomInfrastructureConfig.GetActiveContainers())
        {
            if (string.Equals(cfg.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                targetW = cfg.Width;
                targetH = cfg.Height;
                break;
            }
        }
        if (targetW <= 0 || targetH <= 0) return;

        bool tagHit = false;
        try
        {
            tagHit = TagUtil.SafeTagInt(container, TagAppliedW, 0) == targetW
                  && TagUtil.SafeTagInt(container, TagAppliedH, 0) == targetH;
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.TryExpand.tag", ex); }
        if (tagHit && !forceRefresh) return;

        _isExpanding = true;
        try
        {
            var grids = CollectGrids(container);
            if (grids.Count == 0) return;

            int changed = 0;
            foreach (var grid in grids)
            {
                if (grid == null) continue;

                int w = 0, h = 0;
                try
                {
                    var shape = grid.inventoryShape;
                    if (shape != null) { w = shape.width; h = shape.height; }
                }
                catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.TryExpand.readShape", ex); }

                // 只扩不缩
                int newW = Math.Max(w, targetW);
                int newH = Math.Max(h, targetH);
                if (newW == w && newH == h) continue;

                try
                {
                    grid.SetShape(newW, newH);
                    try { grid.ForceRecomputeDynamicBackground(); } catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.TryExpand.recomputeBackground", ex); }
                    changed++;
                }
                catch (Exception ex)
                {
                    Mod.Log.Warning($"SetShape 失败 {id}: {ex.Message}");
                }
            }

            if (changed > 0 || forceRefresh)
            {
                RefreshWindow(container);
            }

            if (changed > 0)
            {
                TagUtil.SetItemTag(container, TagAppliedW, targetW);
                TagUtil.SetItemTag(container, TagAppliedH, targetH);
                Mod.Diag($"{id} -> {targetW}x{targetH} (grids={changed}/{grids.Count})");
            }
        }
        catch (Exception ex)
        {
            Mod.Log.Warning($"TryExpand({id}): {ex.Message}");
        }
        finally
        {
            _isExpanding = false;
        }
    }



    private static void RefreshWindow(GameItem container)
    {
        if (container == null) return;
        try
        {
            var win = container.contentWindow;
            if (win == null) return;
            try { win.FixBounds(); } catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.RefreshWindow.FixBounds", ex); }
            try { win.Validate(); } catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.RefreshWindow.Validate", ex); }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.RefreshWindow", ex); }
    }

    // ============ 网格收集 ============

    private static List<GameGridInventory> CollectGrids(GameItem container)
    {
        var result = new List<GameGridInventory>();
        if (container == null) return result;
        var seen = new HashSet<IntPtr>();
        var visited = new HashSet<IntPtr>();

        try { Walk(container.children, 0, seen, visited, result); }
        catch (Exception ex) { Mod.Log.Warning("Walk: " + ex.Message); }

        if (result.Count == 0)
        {
            try
            {
                var window = container.contentWindow;
                if (window != null)
                {
                    foreach (var g in FindGridsInWindow(window))
                        AddUnique(result, seen, g);
                }
            }
            catch (Exception ex)
            {
                Mod.Log.Warning("contentWindow scan: " + ex.Message);
            }
        }

        return result;
    }

    private static void Walk(Il2CppSystem.Collections.Generic.List<GraphNodeStorage> nodes,
        int depth, HashSet<IntPtr> seen, HashSet<IntPtr> visited, List<GameGridInventory> outList)
    {
        if (nodes == null || depth > MaxRecursionDepth) return;

        for (int i = 0; i < nodes.Count; i++)
        {
            var node = nodes[i];
            if (node == null) continue;

            IntPtr nptr = IntPtr.Zero;
            try { nptr = ((Il2CppObjectBase)node).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.Walk.pointer", ex); }
            if (nptr != IntPtr.Zero && !visited.Add(nptr)) continue;

            var grid = AsGrid(node);
            if (grid != null) AddUnique(outList, seen, grid);

            // ═══════════════════════════════════════════════════════════════
            // 【2026-10-11 修复】止损：遇到**物品**节点（GameItem）不再向下递归。
            //
            // 症状：未升级的容器放进垃圾箱后双击打开 → 被改成 12x12（垃圾箱 cfg 尺寸）。
            //   实测 Latest.log：`trashcan -> 12x12 (grids=2/2)`（正常应为 1/1）。
            //
            // 根因（反汇编 + 现场探针双重确认）：
            //   · GameGridInventory.get_children()（VA 0x18077DDF0）=
            //     `new List<GraphNodeStorage>(this[0x198])`，而 0x198 正是
            //     `private readonly List<GameItem> items` ——
            //     ⇒ **网格的 children 是"网格里装着的物品"**。
            //   · GameItem.get_children()（VA 0x18079DF70）=
            //     [contentWindow, examineWindow] —— 物品自己的两个窗口。
            //   于是原 Walk 链变成：
            //     垃圾箱窗口 → 垃圾箱网格 → 网格里的物品(storage_bay) → 它的窗口
            //        → **它自己的网格** ≠ 垃圾箱的网格！
            //   探针实测（父窗口 parentItems=[storage_bay]）与上述完全吻合。
            //
            // 为什么"物品节点停止递归"是正确的（而不是漏掉合法网格）：
            //   · 容器自身的网格挂在**根容器的窗口树**里（探针 d=0 PixelWindow →
            //     d=1 Grid），不经过任何 GameItem 节点。
            //   · 一个 GameItem 节点永远代表"另一个物品"，它的窗口/网格属于它自己，
            //     不是本次扩展的目标。继续下钻 = 改别人的尺寸 = 本 bug。
            //   · 与参考实现 GridFinder(FullMachineUpgrades) 的差别：那边是给"机器"
            //     扩容且网格里只放产出物（无容器），故不设止损也能用；CI 的垃圾箱
            //     可以装容器，必须止损。
            // ═══════════════════════════════════════════════════════════════
            bool isItem = false;
            try { isItem = (((Il2CppObjectBase)node).TryCast<GameItem>() != null); } catch { }
            if (isItem) continue;

            try { Walk(node.children, depth + 1, seen, visited, outList); } catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.Walk.recurse", ex); }
        }
    }

    private static void AddUnique(List<GameGridInventory> list, HashSet<IntPtr> seen, GameGridInventory grid)
    {
        if (grid == null) return;
        IntPtr p = IntPtr.Zero;
        try { p = ((Il2CppObjectBase)grid).Pointer; } catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.AddUnique.pointer", ex); }
        if (p == IntPtr.Zero || seen.Add(p)) list.Add(grid);
    }

    private static GameGridInventory AsGrid(GraphNodeStorage node)
    {
        if (node == null) return null;
        try { return ((Il2CppObjectBase)node).TryCast<GameGridInventory>(); }
        catch { return null; }   // 非网格节点返回 null 属正常（每节点都会调用），故不记日志避免噪声
    }

    private static List<GameGridInventory> FindGridsInWindow(PixelWindow window)
    {
        var list = new List<GameGridInventory>();
        var seen = new HashSet<IntPtr>();
        if (window == null) return list;

        try
        {
            if (window.childElement != null)
            {
                var g = ((Il2CppObjectBase)window.childElement).TryCast<GraphNodeStorage>();
                AddUnique(list, seen, AsGrid(g));
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.FindGridsInWindow.childElement", ex); }

        try
        {
            var children = window.children;
            if (children != null)
            {
                for (int i = 0; i < children.Count; i++)
                    AddUnique(list, seen, AsGrid(children[i]));
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("ContainerExpander.FindGridsInWindow.children", ex); }

        return list;
    }

}


// ============ Tag ============
// Tag 读写走 ContainerSuite.Shared.TagUtil。