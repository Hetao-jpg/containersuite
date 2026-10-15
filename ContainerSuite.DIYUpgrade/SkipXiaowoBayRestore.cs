using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using ContainerSuite.Shared;
// 2026-10-10：僵尸手柄「熄火」需要 RectTransform/GameObject（SetActive 熄火，不再反射调 DestroyHandles）。
using UnityEngine;
// S8m：System.Text（StringBuilder）与 UnityEngine（KeyCode/Input）随 F12 诊断台账
//       一并移除。需要恢复台账时按文件内注释块补回这两个 using。

namespace DIYUpgrade;

/// <summary>
/// 拦截 XIAOWOTradePerks（精工扩舱）对 DIY 已接管容器的干扰。
/// 完整背景（S8i 根因修复 / S8n 手柄系统本体避让 / 判据统一）：
///   docs/cross-mod-xiaowo.md
/// </summary>
internal static class SkipXiaowoBayRestore
{
    private static bool _installed;

    /// <summary>被 patch 方法的精确参数签名（AccessTools.Method 精确匹配，防止同名重载误挂）。</summary>
    private static readonly Type[] ItemArgs = { typeof(GameItem) };
    private static readonly Type[] ItemPairArgs = { typeof(GameItem), typeof(GameItem) };

    // F12 诊断台账（Note 逐条记录 + TickDiagnostics 打印）已于 S8m 移除：属排障期
    //   脚手架，每帧读键盘 + 字符串格式化是常驻开销。原实现全文与恢复方法见
    //   _archive\removed-code-20261010\F11F12-ledgers.md。

    internal static void Install(HarmonyLib.Harmony harmony)
    {
        if (_installed)
        {
            return;
        }
        _installed = true;

        Type bay    = TypeCache.FindTypeCached("XIAOWOTradePerks.BayExpansionPerk");
        Type fridge = TypeCache.FindTypeCached("XIAOWOTradePerks.FridgeExpansion");
        Type wine   = TypeCache.FindTypeCached("XIAOWOTradePerks.WineRackExpansion");
        // S8n：手柄系统。XIAOWO 没装 / 换了命名空间时为 null，PatchMethod 会记警告。
        Type resizer = TypeCache.FindTypeCached("XIAOWOTradePerks.ContainerWindowResizer");

        if (bay == null && fridge == null && wine == null)
        {
            MelonLogger.Msg("未检测到 XIAOWOTradePerks，跳过避让");
            return;
        }

        int patched = 0;
        int failed = 0;

        if (bay != null)
        {
            // ── 入口层：核心。TryHandleIngot 挡住 3 个 Prefix，
            //    CanTargetWithIngot 挡住 3 个 Postfix。──
            if (PatchMethod(harmony, bay, "TryHandleIngot", ItemPairArgs, nameof(Prefix_TryHandleIngot))) patched++; else failed++;
            if (PatchMethod(harmony, bay, "CanTargetWithIngot", ItemPairArgs, nameof(Prefix_CanTargetWithIngot))) patched++; else failed++;

            // ── 恢复链 ──
            if (PatchMethod(harmony, bay, "RestoreExpandedItem", ItemArgs, nameof(Prefix_Void_Item))) patched++; else failed++;

            // ── 窗口同步（L4353 UI 焦点回调）。不拦不影响 TryUpgradeBay，
            //    但会跟 DIY 的窗口尺寸打架，所以要拦。──
            if (PatchMethod(harmony, bay, "TryRestoreRememberedGridShape", ItemArgs, nameof(Prefix_Bool_WindowSync))) patched++; else failed++;

            // 【S8i 修正】不再拦 ApplyStoredShape。
            // 它的 bool 返回值被 TryUpgradeBay 当作成败契约，拦它 = 让 XIAOWO 自爆。
        }

        if (fridge != null)
        {
            if (PatchMethod(harmony, fridge, "Restore", ItemArgs, nameof(Prefix_Void_Item))) patched++; else failed++;
        }

        if (wine != null)
        {
            if (PatchMethod(harmony, wine, "Restore", ItemArgs, nameof(Prefix_Void_Item))) patched++; else failed++;
        }

        // ── S8n：手柄系统本体。独立于 BayExpansionPerk 存在，故单独挂 ——
        //    原实现嵌在 `if (bay != null)` 内，BayExpansionPerk 缺失时手柄避让会整体失效。
        //    XIAOWO 有一套与本 mod 同名的 ContainerWindowResizer，8 个手柄，
        //    挂在同一个窗口上跟 DIY 的 7 个手柄抢 raycast（详见类注释）。
        //    拦 Attach = 不再产生新手柄；历史僵尸手柄由 ZombieHandleSweeper 清。──
        if (resizer != null)
        {
            if (PatchMethod(harmony, resizer, "Attach", ItemArgs, nameof(Prefix_Attach))) patched++; else failed++;
        }

        if (failed == 0)
        {
            MelonLogger.Msg($"已避让 XIAOWOTradePerks ({patched}/{patched}) —— 入口层已封死，XIAOWO 无法触发升级");
        }
        else
        {
            MelonLogger.Warning($"已避让 XIAOWOTradePerks：成功 {patched}，失败 {failed}" +
                " ← 有方法没挂上，XIAOWO 可能仍会报 rolled back；" +
                "多半是 XIAOWOTradePerks 改了方法名/签名，请更新容器套件");
        }
    }

    private static bool PatchMethod(HarmonyLib.Harmony harmony, Type type, string name,
        Type[] parameters, string prefixName)
    {
        try
        {
            // 按签名精确匹配：仅按名匹配时，对方新增同名重载会挂错方法或抛
            // AmbiguousMatchException；给出参数类型数组即可消歧。
            MethodInfo methodInfo = AccessTools.Method(type, name, parameters, null);
            if (methodInfo == null)
            {
                // CatchLogOnce：同名一次会话只报一次，不刷屏也不静默。
                CatchLogOnce.Warn($"SkipXiaowoBayRestore 找不到 {type.Name}.{name}，跳过", null);
                return false;
            }
            MethodInfo prefix = typeof(SkipXiaowoBayRestore).GetMethod(prefixName,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (prefix == null)
            {
                CatchLogOnce.Warn($"SkipXiaowoBayRestore 找不到 Prefix {prefixName}", null);
                return false;
            }
            harmony.Patch((MethodBase)methodInfo, new HarmonyMethod(prefix), null, null, null, null);
            // 【2026-10-10 方案 A】手动注册的补丁必须登记进预热表 ——
            //   本类不带 [HarmonyPatch] 类特性，WarmupAllPatchMethods 的枚举看不到它，
            //   若漏登记，这些 Prefix 会在 XIAOWO 首次走被挂路径时于 detour 深栈上
            //   首次 JIT → CompileMethodHook 崩（0x80131506）。详见 WarmupRegistry 注释。
            ContainerSuite.Shared.WarmupRegistry.Register(prefix);
            return true;
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn($"SkipXiaowoBayRestore 避让 {type.Name}.{name} 失败", ex);
            return false;
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  判据：尺寸主权归 DIY 吗？——全权优先 + 宽判据（唯一真相）
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// 恢复链专用判据（宽）：只要 DIY 的开关开着且 ID 在列表里就算 DIY 管的。
    /// 恢复链误拦的后果只是"XIAOWO 不恢复尺寸"，比尺寸打架安全得多。
    /// </summary>
    private static bool ShouldSkipRestore(GameItem item)
    {
        if (item == null) return false;
        // S8k：恢复链也必须跟同一个开关。漏了它会出现"避让关了但恢复链还在拦"
        // 的半开状态 —— 玩家看到的是升级不响应、读档尺寸却仍被冻结。
        if (!DiyUpgradeMod.SkipXiaowoBayEnabled) return false;
        // DIY 正在写 shape → 一定是 DIY 的事
        if (RestoreArbiter.IsApplying) return true;
        if (WorldReadyGate.ShouldDefer) return false;
        try { return DiyUpgradeMod.IsResizableContainer(item); }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); return false; }
    }

    /// <summary>
    /// 入口层判据（全权优先 + 宽）：只要 DIY 能管这个容器，主权就归 DIY。
    /// <para>
    /// ★ 与"tag 证据"窄判据的区别（上一版的错误）：
    ///   窄判据要等 DIY 真的升过级（SPENT/W/H tag &gt; 0）才让路，
    ///   于是"从没升过的箱子"两边都抢 → 顺序决定结果 → 时灵时不灵。
    ///   宽判据在入口就把主权判给 DIY，XIAOWO 干净地走不到 TryUpgradeBay。
    /// </para>
    /// </summary>
    private static bool OwnsDimension(GameItem item)
    {
        if (item == null) return false;
        // S8k 避让热开关：关掉 = 尺寸主权交还 XIAOWO，两边都不再拦。
        // 补丁保持常驻（不 Unpatch），所以这一行短路就等价于「避让关闭」，
        // 同一帧生效，且不会在 IL2CPP 上重生成 detour。
        if (!DiyUpgradeMod.SkipXiaowoBayEnabled) return false;
        if (RestoreArbiter.IsApplying) return true;
        if (WorldReadyGate.ShouldDefer) return false;
        try
        {
            // IsResizableContainer = "DIY 的某个组开着 && ID 在它的 IdList 里"
            //                     || 蛙哥容器已迁移
            // 这正是"尺寸主权归 DIY"的完整定义。
            return DiyUpgradeMod.IsResizableContainer(item);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn(ex); return false; }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Prefix 实现
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>恢复链：RestoreExpandedItem / FridgeExpansion.Restore / WineRackExpansion.Restore。</summary>
    public static bool Prefix_Void_Item(GameItem __0)
    {
        bool skip = ShouldSkipRestore(__0);
        return !skip;
    }

    /// <summary>
    /// 窗口同步：TryRestoreRememberedGridShape。
    /// 返回 false = "没恢复"。调用点是 RestoreExpandedItem 内部和 UI 焦点回调，
    /// **都不是 TryUpgradeBay**，所以拦它不会打坏升级流程的返回值契约。
    /// </summary>
    public static bool Prefix_Bool_WindowSync(GameItem __0, ref bool __result)
    {
        if (__0 == null) return true;
        // 加载期 / DIY 没在写 → 放行，保留 XIAOWO 读档期的正常恢复能力
        if (WorldReadyGate.ShouldDefer) return true;
        if (!RestoreArbiter.IsApplying)
        {
            // S8k：同开关。放在 IsResizableContainer 之前，关掉时直接放行给 XIAOWO。
            if (!DiyUpgradeMod.SkipXiaowoBayEnabled) return true;
            try
            {
                if (!DiyUpgradeMod.IsResizableContainer(__0)) return true;
            }
            catch (System.Exception ex) { CatchLogOnce.Warn(ex); return true; }
        }
        __result = false;
        return false;
    }

    /// <summary>
    /// S8n：XIAOWO 手柄系统入口 —— ContainerWindowResizer.Attach(GameItem)。
    ///
    /// ★ 为什么拦这个（而不是拦 Tick）：
    ///   Attach 内部会 CreateHandles() 真的生成 8 个 GameObject 手柄并挂到窗口上。
    ///   拦 Tick 只能让它不动，手柄已经在那里了（视觉上就是两套手柄重叠）。
    ///   拦 Attach = 手柄根本不会被创建出来，治本。
    ///
    /// ★ 判据用 ShouldSkipRestore（恢复链那个宽判据）而不是 OwnsDimension：
    ///   两者判据内容相同，但 ShouldSkipRestore 额外尊重 WorldReadyGate。
    ///   Attach 只在窗口可见时被调（ApplyResizableWindow 链），加载期拦不拦都无所谓，
    ///   用宽判据更安全 —— 宁可多拦一次加载期，不要漏。
    ///
    /// 返回值：void 方法，return false 即跳过原方法（不产生任何副作用）。
    /// </summary>
    public static bool Prefix_Attach(GameItem __0)
    {
        if (__0 == null) return true;
        if (WorldReadyGate.ShouldDefer) return true;   // 加载期不参与
        if (!ShouldSkipRestore(__0)) return true;      // 主权不在 DIY，放行
        // 命中：XIAOWO 想给一个 DIY 已接管的容器挂拖拽手柄 → 拒绝
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  S8n：僵尸手柄清理
    // ═══════════════════════════════════════════════════════════════════
    //  为什么需要：Prefix_Attach 只能阻止**今后**新建手柄。历史上 XIAOWO
    //  成功升级过的容器，8 个手柄已经进了 XIAOWO 的私有 States 字典，
    //  而那个字典只有 XIAOWO 自己 Dispose（mod 卸载）才清 —— 避让开关
    //  后来才打开的话，手柄会永久残留（核桃反馈的老问题）。
    //
    //  做法：反射拿 States，对每个条目的 ResizeState：
    //    1. 读它的 Item 属性
    //    2. 若该容器归 DIY → 熄火其手柄（NeutralizeHandles：SetActive(false)，不销毁）
    //    3. 从字典移除该 key
    //  判据同 Prefix_Attach —— 开关关掉时不动，主权还给 XIAOWO。

    private const int SweepIntervalMillis = 2000;   // T1：原 SweepIntervalSec = 2.0（改 monotonic ms）
    // 【2026-10-10】首次开门后要稳定多久才允许清扫（消抖；见 TickZombieHandles 注释）。
    private const float SweepSettleSeconds = 2.0f;
    // T1（2026-10-12）：改单调钟节流 —— 原 DateTime 差值在系统时钟回拨时会一直判成
    //   "距上次不到 2 秒" ⇒ 僵尸手柄清扫**再也不跑**（静默停摆）。
    private static PollThrottle _sweepGate;
    private static FieldInfo _statesField;
    private static Type _resizeStateType;
    private static PropertyInfo _resizeStateItemProp;
    // 2026-10-10：不再反射调 DestroyHandles（内部摸 Window.topDragBorder + Object.Destroy
    //   + ResizeCursor.Reset 一整串冷游戏方法，detour 深栈首 JIT 必 0x80131506）。
    //   改「熄火」：读 _handles 列表 + 每个 ResizeHandle.Node（TreeNodeRender）→ SetActive(false)。
    private static FieldInfo _resizeStateHandlesField;
    private static Type _resizeHandleType;
    private static PropertyInfo _resizeHandleNodeProp;
    // 【2026-10-09】对齐 XIAOWO 自己 Dispose 所需的两个可选成员（见 SweepZombieHandles 尾段）。
    private static FieldInfo _visibleStatesField;
    private static FieldInfo _visibilityDirtyField;
    private static bool _sweepUnavailableLogged;

    /// <summary>
    /// 由 DiyUpgradeMod.Tick() 每帧调用，内部按 2 秒节流。
    /// 节流理由：反射遍历不便宜，而残留窗口的数量是分钟级变化的，不需要每帧扫。
    ///
    /// 当前实现：读 XIAOWO 的 States 字典 → 对主权归 DIY 的条目调
    /// <see cref="NeutralizeHandles"/>（每个手柄节点 SetActive(false)），再从 States 移除。
    /// **不再**反射调 XIAOWO 的 <c>DestroyHandles</c>（它内部会摸 Window.topDragBorder +
    /// Object.Destroy + ResizeCursor.Reset 一整串冷游戏方法，detour 深栈首 JIT 必 0x80131506）。
    ///
    /// ★ 与禁止事项 #22 同源：Expel/SetShape/ClearGrid/ForceRecomputeDynamicBackground
    ///   这类"网格/窗口级操作"绝不能在加载期跑；清手柄本质是同类操作（停用/销毁 UI GameObject），
    ///   故加载期一律 defer（<see cref="WorldReadyGate.ShouldDefer"/>），并等首次开门后稳定
    ///   <see cref="SweepSettleSeconds"/> 秒再扫。
    ///
    /// 历史（旧实现「反射调 DestroyHandles」的 09:12 时间线记录）见 docs/cross-mod-xiaowo.md。
    /// </summary>
    internal static void TickZombieHandles()
    {
        // 开关关着就不扫 —— 这时 XIAOWO 的手柄是合法的，不该清。
        if (!DiyUpgradeMod.SkipXiaowoBayEnabled) return;
        // ★★★ 【2026-10-10 / 2026-10-15 复验】专用清扫开关（3D_SweepZombieHandles）。
        //   默认值**只在 DiyUpgradeMod.Core.cs 的 CreateEntry 写一次**，此处不重复数字。
        //   2026-10-15 复验：与 XIAOWO 一起运行时开启实测不崩 —— 本项真正解决的场景
        //   就是"两套拖拽手柄重叠、抢 raycast"，cfg 描述已改为「和小窝 mod 一起使用时建议开启」。
        //   默认仍 false：没有 XIAOWO 时用不上，保持 fail-safe（不做动作）方向。
        //   历史依据（保留供后人核对）：2026-10-10 的 09:17 时间线曾实测
        //   「门禁 + 2s 消抖都生效、世界就绪后 2.5 秒执行仍然崩 0x80131506」；
        //   17:40 改为「熄火」后未复验。
        if (!DiyUpgradeMod.SweepZombieHandlesEnabled) return;
        // ★★★ 加载期 / 读档期（深栈）一律不清：清手柄 = 反射调 XIAOWO/游戏方法 + 销毁
        //   GameObject，首次 JIT 落在 detour 深栈上必崩（见方法头 09:12 实测）。
        //   等世界就绪后再扫 —— 残留手柄不是紧急事，晚几秒清掉完全可接受。
        if (WorldReadyGate.ShouldDefer) return;
        // ★★★ 二次消抖（对齐 SBE 的 BindSettleSeconds 思路）：刚开门的那一两帧，
        //   读档协程（CI / DIY / SBE / 各种 mod）往往还没跑完，栈仍可能偏深；
        //   等"首次开门后稳定 2 秒"再清，确保清扫落在游戏完全平稳的空闲帧上。
        //   （残留手柄是历史遗留，多等 2 秒无任何副作用。）
        if (WorldReadyGate.SecondsSinceFirstReady < SweepSettleSeconds) return;
        try
        {
            if (!_sweepGate.Due(SweepIntervalMillis)) return;
            SweepZombieHandles();
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("XiaowoHandleSweep", ex); }
    }

    /// <summary>
    /// 【2026-10-10】清扫链的启动期浅栈 JIT 预热。
    /// <para>
    /// 为什么需要（SBE 同款经验）：<c>SweepZombieHandles</c> 里 <c>new List&lt;IntPtr&gt;</c>、
    /// <c>foreach (DictionaryEntry)</c>、<c>NeutralizeHandles</c> 的 <c>SetActive(false)</c>，以及
    /// 反射 <c>GetStatesDictionary</c> 的成员解析，这些方法**首次 JIT** 若落在读档深栈上就是
    /// 0x80131506。SBE 的 <c>WarmupJit</c> 已证实「<c>List&lt;T&gt;.Add</c> 是两起崩溃的头号凶手」
    /// —— 泛型集合的实例化方法必须提前点着。
    /// </para>
    /// <para>
    /// ★ 边界：本预热**不能**替"真实手柄对象的 SetActive"预热（主菜单没有手柄对象）。
    /// 因此它只是"减少暴露面"，真正的闸门仍是
    /// <see cref="TickZombieHandles"/> 的 WorldReadyGate 门禁 + 2s 消抖 ——
    /// 保证首次真调落在游戏平稳帧上。
    /// </para>
    /// <para>由 <c>DiyUpgradeMod.WarmupSaveLoadChain</c> 在启动期（主菜单浅栈）调用，一次性。</para>
    /// </summary>
    internal static void WarmupJit()
    {
        if (_warmed) return;
        _warmed = true;
        try
        {
            // 集合冒烟：泛型 List<T>/DictionaryEntry 遍历的首次 JIT（SBE 头号凶手同款）。
            var li = new List<IntPtr>();
            li.Add(IntPtr.Zero);
            foreach (var _ in li) { }
            li.Clear();

            var dict = new System.Collections.Hashtable();
            dict[0] = 1;
            foreach (System.Collections.DictionaryEntry kv in dict) { var _ = kv.Key; }
            dict.Clear();

            // 我们自己的判据 helper（首行都有 null 早退，哑参零副作用）。
            ShouldSkipRestore(null);
            OwnsDimension(null);
            // 反射成员：把 GetStatesDictionary 的"首次反射解析"也在浅栈做掉
            //   （它内部 GetField/GetNestedType/GetProperty 首次有开销与首次 JIT）。
            GetStatesDictionary();

            MelonLogger.Msg("[DIY-JIT] XIAOWO 清扫链预热完成（集合 + 判据 + 反射成员）");
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("XiaowoHandleSweep:warmup", ex); }
    }

    private static bool _warmed;

    private static void SweepZombieHandles()
    {
        var states = GetStatesDictionary();
        if (states == null) return;
        if (states.Count == 0) return;

        List<IntPtr> doomedKeys = new List<IntPtr>();
        // 注意：States 声明成 IDictionary 之后，foreach 拿到的是
        // DictionaryEntry 而不是 KeyValuePair<TKey,TValue>，必须用 .Key/.Value。
        foreach (System.Collections.DictionaryEntry kv in states)
        {
            try
            {
                if (kv.Value == null) { doomedKeys.Add((IntPtr)kv.Key); continue; }
                // 读 ResizeState.Item（internal GameItem 属性）
                var item = _resizeStateItemProp?.GetValue(kv.Value) as GameItem;
                if (item == null) { doomedKeys.Add((IntPtr)kv.Key); continue; }
                if (!ShouldSkipRestore(item)) continue;   // 主权不在 DIY → 保留
                // 先熄火手柄（SetActive(false)），再从字典移除。顺序不能反：
                // 字典移除后拿不到 state，就没机会熄火它的手柄了。
                // ★ 不用 XIAOWO 的 DestroyHandles：它内部摸 Window.topDragBorder + Object.Destroy
                //   + ResizeCursor.Reset 一整串冷游戏方法，detour 深栈首 JIT 必 0x80131506。
                //   熄火后节点仍留在场景，但 States 移除 → XIAOWO 不再逐帧 Tick 它 →
                //   不会再 BringHandlesToFront → 我们的手柄 overlay 稳定在最上层、赢 raycast。
                NeutralizeHandles(kv.Value);
                doomedKeys.Add((IntPtr)kv.Key);
            }
            catch (System.Exception ex) { CatchLogOnce.Warn("XiaowoHandleSweep:item", ex); }
        }
        if (doomedKeys.Count == 0) return;

        // 按**实际移除成功**计数：Remove 可能因该 key 已被 XIAOWO 自行移走而返回 false，
        // 此时不应计入清理数量（原实现按"打算移除"计数，会虚高）。
        int removed = 0;
        for (int i = 0; i < doomedKeys.Count; i++)
        {
            // IDictionary.Remove 返回 void（非泛型接口），故先 Contains 判存在再移除。
            try { if (states.Contains(doomedKeys[i])) { states.Remove(doomedKeys[i]); removed++; } }
            catch (System.Exception ex) { CatchLogOnce.Warn("XiaowoHandleSweep:remove", ex); }
        }
        if (removed == 0) return;

        // 【2026-10-09】对齐 XIAOWO 自己的 ContainerWindowResizer.Dispose（它清 States 时
        //   会同时 VisibleStates.Clear() + _visibilityDirty = true，见 XIAOWOTradePerks 反编译）。
        //   只删 States 是不够的：XIAOWO.Tick 遍历的是 VisibleStates 这个缓存列表，里面
        //   仍留着刚被销毁的 ResizeState（其 _handles 已清空、_overlay 已置 null），
        //   会被逐帧继续 Tick。清空缓存 + 置脏后，XIAOWO 下一帧会从存活条目重建它。
        try
        {
            (_visibleStatesField?.GetValue(null) as System.Collections.IList)?.Clear();
            _visibilityDirtyField?.SetValue(null, true);
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("XiaowoHandleSweep:visible", ex); }

        MelonLogger.Msg($"[容器升级] 已清理 XIAOWO 残留拖拽手柄 {removed} 个（避让生效，尺寸主权归 DIY）。" +
            "这是历史遗留：XIAOWO 在避让生效前成功升级过这些容器。");
    }

    /// <summary>
    /// 【2026-10-10】熄火一个 ResizeState 的 8 个手柄：把每个手柄节点 GameObject
    ///   SetActive(false)，使它们退出 raycast / 渲染，而不是销毁。
    /// <para>
    /// ★ 为什么不用 XIAOWO 的 DestroyHandles：它内部摸 Window.topDragBorder + Object.Destroy
    ///   + ResizeCursor.Reset 一整串冷游戏方法，detour 深栈首 JIT 必 0x80131506（09:17 实测）。
    ///   这里只调 SetActive(false)（本 mod 拖拽路径每帧都在用、早已预热）——零冷 JIT、零销毁。
    /// </para>
    /// <para>安全：节点 GameObject 可能已随窗口销毁，逐个 try/catch；单个失败不影响整体。</para>
    /// </summary>
    private static void NeutralizeHandles(object resizeState)
    {
        try
        {
            if (_resizeStateHandlesField == null || _resizeHandleNodeProp == null) return;
            var handles = _resizeStateHandlesField.GetValue(resizeState) as System.Collections.IList;
            if (handles == null) return;
            for (int i = 0; i < handles.Count; i++)
            {
                try
                {
                    var handle = handles[i];
                    if (handle == null) continue;
                    var node = _resizeHandleNodeProp.GetValue(handle) as TreeNodeRender;
                    if (node == null) continue;
                    RectTransform rt = node.rectTransform;
                    if (rt == null) continue;
                    GameObject go = rt.gameObject;
                    if (go != null && go.activeSelf) go.SetActive(false);
                }
                catch { /* 单个手柄熄火失败不影响整体 */ }
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("XiaowoHandleSweep:neutralize", ex); }
    }

    /// <summary>反射拿 XIAOWO 的私有 States 字典（Dictionary&lt;IntPtr, ResizeState&gt;）。</summary>
    private static System.Collections.IDictionary GetStatesDictionary()
    {
        if (_statesField != null && _resizeStateType != null) return _statesField.GetValue(null) as System.Collections.IDictionary;
        try
        {
            Type resizer = TypeCache.FindTypeCached("XIAOWOTradePerks.ContainerWindowResizer");
            if (resizer == null)
            {
                if (!_sweepUnavailableLogged) { _sweepUnavailableLogged = true; MelonLogger.Msg("XIAOWO 手柄类不存在，跳过残留手柄清理（不影响其它避让）。"); }
                return null;
            }
            _statesField = resizer.GetField("States", BindingFlags.NonPublic | BindingFlags.Static);
            _resizeStateType = resizer.GetNestedType("ResizeState", BindingFlags.NonPublic);
            if (_statesField == null || _resizeStateType == null)
            {
                if (!_sweepUnavailableLogged)
                {
                    _sweepUnavailableLogged = true;
                    MelonLogger.Warning("XIAOWO 手柄内部结构已变（找不到 States/ResizeState），残留手柄清理失效。" +
                        "若发现窗口上有两套拖拽手柄，请把 XIAOWO 版本报给容器套件作者。");
                }
                _statesField = null; _resizeStateType = null;
                return null;
            }
            _resizeStateItemProp = _resizeStateType.GetProperty("Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            _resizeStateHandlesField = _resizeStateType.GetField("_handles", BindingFlags.NonPublic | BindingFlags.Instance);
            _resizeHandleType = resizer.GetNestedType("ResizeHandle", BindingFlags.NonPublic);
            _resizeHandleNodeProp = _resizeHandleType?.GetProperty("Node", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // 【2026-10-09】XIAOWO 的 Tick 遍历的是 VisibleStates（States 的缓存，由
            //   RefreshVisibleStates 重建），不是 States 本身。两者都是可选成员：
            //   拿不到只退化为"仅删 States"，不视为结构失效。
            _visibleStatesField = resizer.GetField("VisibleStates", BindingFlags.NonPublic | BindingFlags.Static);
            _visibilityDirtyField = resizer.GetField("_visibilityDirty", BindingFlags.NonPublic | BindingFlags.Static);
            if (_resizeStateItemProp == null || _resizeStateHandlesField == null || _resizeHandleNodeProp == null)
            {
                if (!_sweepUnavailableLogged)
                {
                    _sweepUnavailableLogged = true;
                    MelonLogger.Warning("XIAOWO 手柄成员改名（Item/_handles/Node 找不到），残留手柄清理失效。");
                }
                _statesField = null; _resizeStateType = null;
                return null;
            }
            return _statesField.GetValue(null) as System.Collections.IDictionary;
        }
        catch (System.Exception ex)
        {
            CatchLogOnce.Warn("XiaowoHandleSweep:reflect", ex);
            _statesField = null; _resizeStateType = null;
            return null;
        }
    }

    /// <summary>
    /// 入口 1/2：TryHandleIngot(source, target) —— 返回 true = 我处理了。
    /// 两个 Prefix 入口（MaterialDirectory.OnIngotTarget L2647 /
    /// GameItem.Target L2679）都是 `return !TryHandleIngot(...)`，
    /// 拦它 → 两个入口全部放行原版，XIAOWO 进不到 TryUpgradeBay。
    /// </summary>
    public static bool Prefix_TryHandleIngot(GameItem __0, GameItem __1, ref bool __result)
    {
        // target 才是容器（__1）。__0 是金属锭。
        if (__1 == null) return true;
        if (WorldReadyGate.ShouldDefer) return true;   // 加载期让路
        if (RestoreArbiter.IsApplying) { __result = false; return false; }

        bool owns = OwnsDimension(__1);
        if (!owns) return true;

        __result = false;
        return false;
    }

    /// <summary>
    /// 入口 2/2：CanTargetWithIngot(source, target) —— 返回 true = "能升级"。
    /// 三个 Postfix 入口（GameItem.CanTarget / MayTarget / MaterialDirectory.CanIngotTarget）
    /// 的判定条件都是 `!__result && IsMetalIngot && CanTargetWithIngot(...)`，
    /// 拦它 → 三个 Postfix 全部不改 __result → DIY 的判定不被覆盖。
    /// </summary>
    public static bool Prefix_CanTargetWithIngot(GameItem __0, GameItem __1, ref bool __result)
    {
        if (__1 == null) return true;
        if (WorldReadyGate.ShouldDefer) return true;
        if (RestoreArbiter.IsApplying) { __result = false; return false; }

        bool owns = OwnsDimension(__1);
        if (!owns) return true;

        __result = false;
        return false;
    }

    // ═══════════════════════════════════════════════════════════════════
    //  S8m 移除：F12 诊断台账（Note 逐条记录 + TickDiagnostics 打印）
    //  原实现全文见 _archive\removed-code-20261010\F11F12-ledgers.md。
    // ═══════════════════════════════════════════════════════════════════
}
