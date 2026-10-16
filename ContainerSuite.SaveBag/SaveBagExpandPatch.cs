using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using System;
using ContainerSuite.Shared;   // S2 新增
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace SaveBagExpand;

[HarmonyPatch(typeof(EmporiumEntry), "Start")]
internal static class EmporiumEntryStartPatch
{
    private static void Postfix(EmporiumEntry __instance)
    {
        if (WorldReadyGate.ShouldDefer) return;
        ApplyAllResize(__instance);
        ScheduleLateRefresh(__instance);
    }

    // ★ 2026-09-30 优化：ApplyAllResize 幂等 + 冷却，避免重复 Resize
    private static float _lastApplyAt = -10f;
    private const float ApplyCooldown = 0.5f;

    // ★ 2026-10-09：延迟刷新等待时长（墙钟秒）。原为 20 帧，低帧率下会漂移
    //   （项目约定：等待一律用墙钟秒）。0.33s ≈ 60fps 下的 20 帧，保持原意。
    private const float LateRefreshDelaySeconds = 0.33f;

    // ★ 2026-10-01：本次 ApplyAllResize 里是否有容器被真正改动（用于条件化 Validate）
    private static bool _resizeHappened = false;

    // ★ 2026-10-01：给 OnArriveStore 延迟路径用的入口，绕过冷却
    internal static void ApplyAllResizeForce(EmporiumEntry __instance)
    {
        _lastApplyAt = -10f;
        ApplyAllResize(__instance);
    }

    /// <summary>
    /// 单独对一个容器套用尺寸（供 SetupAfterhourInv Postfix 等场景调用）。
    /// ★ 复用同一个私有 Resize() —— 保证与 ApplyAllResize 里的 8 个容器
    ///   走完全相同的幂等判断 / 边框算法 / 窗口同步 / 填充底修复。
    ///   与 ApplyAllResize 的区别：不动冷却、不重设其它容器、不调 EmporiumEntry.Validate
    ///   （地面刚建好时那些都过重，且地面不属于 Emporium 主布局）。
    /// </summary>
    internal static void ResizeOneContainer(GameGridInventory inv, PixelWindow window, string name,
                                            int cfgW, int cfgH)
    {
        try
        {
            if (!SaveBagExpandCore.Enabled) return;
            Resize(inv, window, name, cfgW, cfgH);
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.patch.ResizeOneContainer", ex);
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ★ 2026-10-10：客户柜台「同帧窗口同步」（RootFix for 半帧滞后）
    //
    // 机制（探针已证实，见 MEMORY 第十一节）：
    //   游戏切商店时**只**调 Reset/ExpandFrontCounterGrid → 只 SetShape(改网格像素)，
    //   **从不碰窗口像素**（探针 #7~#12：SetShape 后 winPx 原封不动）。
    //   本 mod 的窗口同步原本只挂在 Start/OnArriveStore/OpenShutter 的维护块里
    //   ⇒ 网格当帧就变、窗口要等下一个流程点才追上 ⇒ 玩家看到「网格好了窗口没好」
    //   （即"半修"）。
    //
    // 修法：把窗口同步下沉到 Reset/Expand 的 **Postfix** 里，紧跟 SetShape 之后
    //   **同一帧**执行 —— 窗口与网格从此不可能错帧。
    //
    // 与维护块的关系：本方法就是维护块第 1~2 步的**单一实现**，维护块改为调它
    //   （消重，语义完全一致：同样先 PreFillGridLists 裁剪、再 WindowNeedsSync 判据、
    //    同样 4px 边框算法、同样 ResizePixels/Validate/FixBounds/SyncWindowRect）。
    //
    // 安全：
    //   · 只读/写**本 mod 已在维护块里做过**的同一批操作（无新增危险 API）。
    //   · 加载期一律放行（禁止事项 #22：加载深栈 SetShape → 0x80131506，托管 catch 抓不到）
    //     —— 调用方两处 Postfix 各自先判 WorldReadyGate.ShouldDefer。
    //   · TrySilent 包裹全部可能抛的操作，任一失败不影响游戏。
    // ══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// 立即把客户柜台窗口像素同步到网格形状（同帧）。供 Reset/Expand Postfix 调用。
    /// ★ 不判 ShouldDefer / Enabled —— 由调用方判（Postfix 里判，语义更清晰）。
    /// </summary>
    internal static void SyncFrontCounterWindowNow(EmporiumEntry entry, string reason)
    {
        GameGridInventory finv;
        PixelWindow fwin;
        try { finv = entry.frontInvinvElement; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.SyncFrontInv.inv", ex); return; }
        try { fwin = entry.frontInvWindow; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.SyncFrontInv.win", ex); return; }
        if (finv == null || fwin == null) return;

        // 1) 裁剪多余填充底节点（用当前形状算 expected，所以对 14x21 / 18x4 都安全）
        TrySilent(() => PreFillGridLists(finv));

        // 2) 窗口像素残留 → 同步（同帧）
        if (!WindowNeedsSync(fwin, finv)) return;

        int bw = 4, bh = 4;
        try
        {
            int dw = Math.Max(0, fwin.widthPixels - finv.widthPixels);
            int dh = Math.Max(0, fwin.heightPixels - finv.heightPixels);
            if (dw > 64) dw = 4;
            if (dh > 64) dh = 4;
            bw = dw; bh = dh;
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.SyncFrontInv.pixels", ex); }

        int tw = finv.widthPixels + bw;
        int th = finv.heightPixels + bh;

        TrySilent(() => fwin.ResizePixels(tw, th));
        TrySilent(() => fwin.Validate());
        TrySilent(() => fwin.FixBounds());
        SyncWindowRect(fwin);

        try
        {
            SaveBagExpandCore.Diag(
                $"  客户柜台 窗口同步（{reason}）：→ {tw}x{th}px");
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.SyncFrontInv.diag", ex); }
    }

    internal static void ApplyAllResize(EmporiumEntry __instance)
    {
        if (!SaveBagExpandCore.Enabled) return;
        if (__instance == null) return;

        // ★ 冷却：0.5 秒内的重复调用直接跳过（OpenShutter / OnArriveStore / Start 可能同帧触发）
        float now = Time.realtimeSinceStartup;
        if (now - _lastApplyAt < ApplyCooldown) return;
        _lastApplyAt = now;

        // ★ 每次真正进入逻辑时重置标志，避免上次调用残留
        _resizeHappened = false;

        if (SaveBagExpandConfig.ExpandEnabled.Value)
        {
            Resize(__instance.invElement, __instance.invWindow, "主仓库",
                SaveBagExpandConfig.SaveBagWidth.Value,
                SaveBagExpandConfig.SaveBagHeight.Value);

            Resize(__instance.showcaseElement, __instance.showcaseWindow, "展示柜",
                SaveBagExpandConfig.ShowcaseInvWidth.Value,
                SaveBagExpandConfig.ShowcaseInvHeight.Value);

            Resize(__instance.backInvinvElement, __instance.backInvWindow, "柜台",
                SaveBagExpandConfig.BackInvWidth.Value,
                SaveBagExpandConfig.BackInvHeight.Value);

            Resize(__instance.hiddenElement, __instance.hiddenWindow, "海报暗格",
                SaveBagExpandConfig.HiddenInvWidth.Value,
                SaveBagExpandConfig.HiddenInvHeight.Value);

            // 客户柜台开关合并入总控 —— 尺寸仍独立配置，但生效条件跟 ExpandEnabled 一致。
            Resize(__instance.frontInvinvElement, __instance.frontInvWindow, "客户柜台",
                SaveBagExpandConfig.FrontInvWidth.Value,
                SaveBagExpandConfig.FrontInvHeight.Value,
                excludeDoctorShop: true);

            // ── 地面/拾荒窗口（2026-10-09 新增，第 9 个容器）──
            //   ★ 原版 12×9（反汇编 SetupAfterhourInv 的 ctor(12,9) 已证实）。
            //   网格/窗口在 SetupAfterhourInv 里建，本 Postfix 跑在它之后 ⇒ 非 null。
            //   与其它容器同构：交给同一个 Resize()（共用幂等 + 窗口同步 + 填充底修复）。
            //   注意：地面窗口 canMove==false，位置由游戏 Show 内部写入 ——
            //   本 mod 只改尺寸，不碰位置（位置调整是 PickupWindowTest 的实验功能，不并入）。
            Resize(__instance.afterhourInventory, __instance.afterhourWindow, "地面",
                SaveBagExpandConfig.AfterhourWidth.Value,
                SaveBagExpandConfig.AfterhourHeight.Value);
        }

        // ★ 客户柜台维护：不跟随任何"扩容/客户柜台"开关（原 FrontInvExpandEnabled 已删除），无条件执行。
        //   跨商店切换后：形状会恢复（14x21 ↔ 18x4），但
        //     · dynamicBackgrounds 列表里的多余节点不会自己消失
        //     · 窗口像素（widthPixels / heightPixels）会停留在旧形状上
        //   导致可视网格范围溢出到容器之外。
        //   这一段做裁剪 + 窗口同步，形状不变，纯粹清理残留。
        //   ★ 2026-10-10：实现已下沉到 SyncFrontCounterWindowNow（Reset/Expand Postfix 也调它），
        //     这里只做转发 —— 与本方法其它容器一样，是"兜底点"而不是唯一入口。
        try { SyncFrontCounterWindowNow(__instance, "applyresize"); }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.applyresize.frontInvSync", ex); }


        // ★ 2026-10-01：只有真的有容器被改动时才 Validate。
        //   否则博士商店里 DelayedApply 会因为幂等全部跳过 Resize，
        //   但 Validate 仍空转一次（无害但多余）。
        if (_resizeHappened)
        {
            // ★ 2026-10-07：实参**显式化**（原为裸 `Validate()`）。
            //   裸调用 == `Validate(true)`（该方法的元数据默认值就是 true），
            //   旧注释误以为"无参 = 只做轻量校验" —— 见
            //   SaveBagExpandCore.EmporiumForceValidate 处的完整取证。
            //   这里显式写出实参，杜绝"看起来轻量、实际强制重建"的再次误读。
            try { __instance.Validate(SaveBagExpandCore.EmporiumForceValidate); }
            catch (Exception ex) { SaveBagExpandCore.Log.Error($"  EmporiumEntry.Validate 失败: {ex}"); }
        }
        _resizeHappened = false;
    }

    // ★ 2026-10-02（S5-2）：博士商店的客户柜台原版是 14x21。
    //   EmporiumEntry 实例在两个商店间复用（探针已证实指针不变），无法用实例区分。
    //
    //   旧方案只看形状 == 14x21，两个方向都会误判：
    //     · 博士商店柜台被改成别的尺寸（游戏更新 / 其它 mod）→ 认不出来，被误扩容；
    //     · 主商店柜台被玩家配置成 14x21 → 被误认成博士商店，扩容静默失效。
    //
    //   新方案两级判定：
    //     一级 = 游戏自己的信号（EmporiumEntry.ResetFrontCounterGrid / ExpandFrontCounterGrid），
    //            不受尺寸变化影响，最可靠；
    //     二级 = 形状兜底，只在 tracker 还是 Unknown（游戏信号还没到）时沿用旧行为。
    //   只对 FrontInv 生效（excludeDoctorShop = true），其它容器不检查。
    private const int ExcludeW = 14;
    private const int ExcludeH = 21;

    private static void Resize(GameGridInventory inv, PixelWindow window, string name,
                               int cfgW, int cfgH, bool excludeDoctorShop = false)
    {
        if (inv == null) return;

        int w = SaveBagExpandConfig.Clamp(cfgW);
        int h = SaveBagExpandConfig.Clamp(cfgH);
        if (w == 0 && h == 0) return;

        // ── ① 博士商店排除 ──
        if (excludeDoctorShop)
        {
            FrontInvMode mode = FrontInvModeTracker.Current;

            // 一级：游戏信号说在博士商店 → 直接跳过（不管现在是什么尺寸）
            if (mode == FrontInvMode.DoctorShop)
            {
                // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 这些是刷新/延迟应用路径的诊断与容错记录。
                SaveBagExpandCore.Diag(
                    $"  {name} 游戏信号判定为博士商店，跳过扩容（保持原版尺寸）");
                return;
            }

            // 一级：游戏信号说在主商店 → 信任信号，不做形状比对（避免误判成博士商店）
            if (mode == FrontInvMode.MainStore)
            {
                // 落到 ② 继续正常扩容
            }
            // 二级：tracker 未知（游戏信号还没到）→ 沿用旧的形状识别兜底
            else
            {
                try
                {
                    var cur = inv.inventoryShape;
                    if (cur != null && cur.width == ExcludeW && cur.height == ExcludeH)
                    {
                        // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 这些是刷新/延迟应用路径的诊断与容错记录。
                        SaveBagExpandCore.Diag(
                            $"  {name} 模式未知，按原版尺寸（{ExcludeW}x{ExcludeH}）判定为博士商店，跳过");
                        return;
                    }
                }
                catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.Resize.shapeCheck", ex); }
            }
        }

        // ── ② 幂等：形状已经等于配置值 → 不做 Resize 重活，但要裁剪填充底 + 同步窗口 ──
        //   ★ 2026-10-01：跨场景后填充底列表可能残留（博士商店 368 → 主商店 120），
        //   幂等分支也要跑 PreFillGridLists，否则残留节点会溢出显示到网格下方。
        try
        {
            var s = inv.inventoryShape;
            if (s != null && s.width == w && s.height == h)
            {
                PreFillGridLists(inv);
                // 窗口同步已移到 ApplyAllResize 的独立维护块（对 FrontInv 无条件执行）
                return;
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.Resize.idempotentShape", ex); }

        _resizeHappened = true;   // ★ 标记：本次真的改了形状

        try
        {
            int oldInvW = inv.widthPixels;
            int oldInvH = inv.heightPixels;
            int borderW = (window != null) ? Math.Max(0, window.widthPixels - oldInvW) : 0;
            int borderH = (window != null) ? Math.Max(0, window.heightPixels - oldInvH) : 0;

            float transparencyBefore = 0f;
            try { transparencyBefore = inv.transparency; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.transparencyBefore", ex); }

            inv.SetShape(w, h);

            GameGridInventory shown = TryGetAttachedGrid(window);
            bool same = InteropUtil.SameObject(shown, inv);
            if (shown != null && !same) TrySilent(() => shown.SetShape(w, h));

            RefreshGrid(inv, transparencyBefore);
            if (shown != null && !same) RefreshGrid(shown, transparencyBefore);

            if (window != null)
            {
                TrySilent(() => window.ResizePixels(inv.widthPixels + borderW, inv.heightPixels + borderH));
                TrySilent(() => window.Validate());
                TrySilent(() => window.FixBounds());
                SyncWindowRect(window);
            }

            float transparencyAfter = 0f;
            try { transparencyAfter = inv.transparency; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.transparencyAfter", ex); }
            if (transparencyBefore > 0.001f && Math.Abs(transparencyAfter - transparencyBefore) > 0.001f)
            {
                TrySilent(() => inv.SetTransparency(transparencyBefore));
                if (shown != null && !same) TrySilent(() => shown.SetTransparency(transparencyBefore));
            }

            if (!GridFx.FillHealthyByRow(inv)) RebuildGridFill(inv, transparencyBefore);
            else GridFx.ApplyMeshAlpha(inv, GridAlphaFor(transparencyBefore));
            if (shown != null && !same)
            {
                if (!GridFx.FillHealthyByRow(shown)) RebuildGridFill(shown, transparencyBefore);
                else GridFx.ApplyMeshAlpha(shown, GridAlphaFor(transparencyBefore));
            }

            string attachInfo = (shown == null) ? "无"
                              : same ? $"同一个({InteropUtil.Ptr(shown)})"
                              : $"不同！逻辑={InteropUtil.Ptr(inv)} 挂载={InteropUtil.Ptr(shown)}（已一并调整）";
            SaveBagExpandCore.Diag(
                $"  {name} 已改为 {w}x{h}" +
                $"（网格 {inv.widthPixels}x{inv.heightPixels}px" +
                (window != null ? $"，窗口 {window.widthPixels}x{window.heightPixels}px" : "") +
                $"，挂载对象: {attachInfo}）");
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Error($"  修改 {name} 失败: {ex}");
        }
    }

    private static void ScheduleLateRefresh(EmporiumEntry entry)
    {
        if (entry == null) return;
        try { MelonCoroutines.Start(CoLateRefresh(entry)); }
        catch (Exception ex) { SaveBagExpandCore.Diag("  无法安排延迟刷新: " + ex.Message); }
    }

    private static IEnumerator CoLateRefresh(EmporiumEntry entry)
    {
        yield return null;
        yield return null;
        try { RefreshAllVisuals(entry); }
        catch (Exception ex) { SaveBagExpandCore.Diag("  延迟刷新(2帧)失败: " + ex.Message); }

        // ★ 2026-10-09：等 0.33s（墙钟）后仅当布局真的没稳定时才再 Resize。
        //   原为 20 帧 —— 低帧率下 20 帧可能远超预期时长，改用墙钟秒。
        float deadline = Time.realtimeSinceStartup + LateRefreshDelaySeconds;
        while (Time.realtimeSinceStartup < deadline) yield return null;
        try
        {
            if (entry == null) yield break;

            bool needsRedo = false;
            if (entry.invElement != null)
            {
                try
                {
                    var s = entry.invElement.inventoryShape;
                    int wantW, wantH;
                    if (SaveBagExpandConfig.ExpandEnabled.Value)
                    {
                        wantW = SaveBagExpandConfig.Clamp(SaveBagExpandConfig.SaveBagWidth.Value);
                        wantH = SaveBagExpandConfig.Clamp(SaveBagExpandConfig.SaveBagHeight.Value);
                        if (wantW <= 0) wantW = 24;
                        if (wantH <= 0) wantH = 10;
                    }
                    else { wantW = 24; wantH = 10; }   // 扩容关闭 → 期望值就是原版 24x10（与 Bind 口径一致）
                    if (s == null || s.width != wantW || s.height != wantH) needsRedo = true;
                }
                catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.needsRedo", ex); needsRedo = true; }
            }

            if (needsRedo)
            {
                _lastApplyAt = -10f;
                ApplyAllResize(entry);
            }
        }
        catch (Exception ex) { SaveBagExpandCore.Diag("  延迟刷新(延时)失败: " + ex.Message); }
    }

    private static void RefreshAllVisuals(EmporiumEntry e)
    {
        if (e == null) return;
        RefreshOne(e.invElement, e.invWindow);
        RefreshOne(e.showcaseElement, e.showcaseWindow);
        RefreshOne(e.backInvinvElement, e.backInvWindow);
        RefreshOne(e.hiddenElement, e.hiddenWindow);
        RefreshOne(e.frontInvinvElement, e.frontInvWindow);
        // ★ 2026-10-07：实参**显式化**（原为裸 `e.Validate()`）。
        //   裸调用 == `Validate(true)`；旧注释"必须无参"的前提是错的。
        //   完整取证见 SaveBagExpandCore.EmporiumForceValidate。
        try { e.Validate(SaveBagExpandCore.EmporiumForceValidate); }
        catch (Exception ex) { CatchLogOnce.Warn("EmporiumEntry.Validate", ex); }
    }

    private static void RefreshOne(GameGridInventory inv, PixelWindow window)
    {
        if (inv == null) return;
        float t = TransparencyOf(inv);
        RefreshGrid(inv, t);
        GameGridInventory shown = TryGetAttachedGrid(window);
        if (shown != null && !InteropUtil.SameObject(shown, inv)) RefreshGrid(shown, t);
        if (window == null) return;
        TrySilent(() => window.Validate());
        TrySilent(() => window.FixBounds());
        SyncWindowRect(window);
    }

    private static void RefreshGrid(GameGridInventory g, float transparency)
    {
        if (g == null) return;

        TrySilent(() => g.ForceRecomputeDynamicBackground());
        TrySilent(() => g.ValidateBackground());
        TrySilent(() => g.Validate());

        if (!GridFx.FillHealthyByRow(g))
            RebuildGridFill(g, transparency);
        else
            GridFx.ApplyMeshAlpha(g, GridAlphaFor(transparency));

        TrySilent(() => g.ClearHighlight());

        try
        {
            Component c = ((Il2CppObjectBase)g).TryCast<Component>();
            if (c == null || c.gameObject == null) return;
            var gs = c.gameObject.GetComponentsInChildren<Graphic>(true);
            if (gs == null) return;
            for (int i = 0; i < gs.Count; i++) if (gs[i] != null) gs[i].SetAllDirty();
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.RefreshGrid.dirty", ex); }
    }

    private static float TransparencyOf(GameGridInventory g)
    {
        try { return (g == null) ? 0f : g.transparency; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.TransparencyOf", ex); return 0f; }
    }

    // ══════════════════════════════════════════════════════════════════════
    // 填充底修复：预填充 + 硬刷 + 过量裁剪
    // ══════════════════════════════════════════════════════════════════════

    private static void PreFillGridLists(GameGridInventory g)
    {
        if (g == null) return;

        int width = 0, height = 0;
        try
        {
            var s = g.inventoryShape;
            if (s != null) { width = s.width; height = s.height; }
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.PreFillGridLists.shape", ex); }
        if (width <= 0 || height <= 0) return;

        int expected = (width + 2) * (height + 2);

        var dynBg = g.dynamicBackgrounds;
        if (dynBg == null || dynBg.Count == 0) return;

        TreeNodeRender template = dynBg[0];
        if (template == null) return;

        FillListToSize(dynBg, expected, template);
        FillListToSize(g.dynamicR0, expected, template);
        FillListToSize(g.dynamicR1, expected, template);
        FillListToSize(g.dynamicR2, expected, template);
        FillListToSize(g.dynamicR3, expected, template);
    }

    private static void FillListToSize(Il2CppSystem.Collections.Generic.List<TreeNodeRender> list,
                                       int expected, TreeNodeRender template)
    {
        if (list == null || template == null) return;

        // ── ① 过量裁剪：跨场景切换后（主商店 120 ↔ 博士商店 368），
        //   旧形状的节点会残留在列表里，必须裁掉，否则填充底会溢出到网格外。
        //   剪尾：保留前 expected 个，移除后面的（按旧形状布局克隆的多余节点）。
        if (list.Count > expected)
        {
            int removeCount = list.Count - expected;
            for (int i = 0; i < removeCount; i++)
            {
                int lastIdx = list.Count - 1;
                if (lastIdx < 0) break;

                TreeNodeRender node = list[lastIdx];
                try { list.RemoveAt(lastIdx); } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.FillList.remove", ex); break; }

                // 用 SetActive(false) 而不是 Destroy：
                //   ① 不破坏游戏内部可能的对象池；
                //   ② 下次需要时若列表 Count < expected，会走补齐逻辑新建节点，
                //      这些隐藏节点会长期处于隐藏状态（无害）。
                if (node != null)
                {
                    try { node.gameObject?.SetActive(false); } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.FillList.hide", ex); }
                }
            }
        }

        // ── ② 不足补齐 ──
        if (list.Count >= expected) return;

        Transform parentTf = null;
        try { parentTf = template.transform?.parent; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.FillList.parent", ex); }

        while (list.Count < expected)
        {
            try
            {
                UnityEngine.Object raw = UnityEngine.Object.Instantiate(template.gameObject);
                if (raw == null) break;
                GameObject cloneGo = raw.TryCast<GameObject>();
                if (cloneGo == null) { UnityEngine.Object.Destroy(raw); break; }

                TreeNodeRender clone = cloneGo.GetComponent<TreeNodeRender>();
                if (clone == null) { UnityEngine.Object.Destroy(cloneGo); break; }

                if (parentTf != null) clone.transform.SetParent(parentTf, false);
                cloneGo.SetActive(true);

                list.Add(clone);
            }
            catch (Exception ex)
            {
                SaveBagExpandCore.Log.Error($"  补齐填充底节点失败: {ex.Message}");
                break;
            }
        }
    }

    private static void RebuildGridFill(GameGridInventory g, float transparency)
    {
        if (g == null) return;

        PreFillGridLists(g);

        TrySilent(() => g.renderDynamicBackground = true);
        TrySilent(() => g.Validate());
        TrySilent(() => g.ValidateBackground());
        TrySilent(() => g.ForceRecomputeDynamicBackground());

        if (!GridFx.FillHealthyByRow(g))
        {
            TrySilent(() => g.renderDynamicBackground = false);
            TrySilent(() => g.Validate());
            TrySilent(() => g.renderDynamicBackground = true);
            TrySilent(() => g.Validate());
            TrySilent(() => g.ValidateBackground());
            TrySilent(() => g.ForceRecomputeDynamicBackground());
        }

        GridFx.ApplyMeshAlpha(g, GridAlphaFor(transparency));

        // ★ 2026-10-09 优化：末尾这次 FillHealthyByRow **纯为日志**（判据是纯读，
        //   无任何副作用）。原实现无条件跑一次（遍历全部 5 个 mesh 列表的所有节点），
        //   再看 Diag 开关决定丢不丢 —— 关诊断时这一趟就是白跑。
        //   加 CfgDiagEnabled 短路：关诊断时直接跳过，省一次全节点扫描。
        //   语义不变：诊断开启时行为与原来完全一致。
        if (SaveBagExpandCore.CfgDiagEnabled && !GridFx.FillHealthyByRow(g))
            SaveBagExpandCore.Diag($"  网格 {InteropUtil.Ptr(g)} 填充底仍不齐（{GridFx.RowCoverage(g)}）");
    }

    private static float GridAlphaFor(float transparency)
    {
        try
        {
            if (SaveBagExpandConfig.SaveBagShowGrid != null
                && SaveBagExpandConfig.SaveBagShowGrid.Value)
            {
                return 1f;
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.GridAlphaFor", ex); }
        return Mathf.Clamp01(1f - transparency);
    }

    // ★ 2026-10-01：判断窗口是否需要同步。
    //   判断依据：窗口当前像素与网格形状应有的像素明显不符。
    //   正常边框 4~8 像素，差值超过 32 像素就认为窗口没跟上形状。
    private static bool WindowNeedsSync(PixelWindow window, GameGridInventory inv)
    {
        try
        {
            int shapeW = 0, shapeH = 0;
            try
            {
                var s = inv.inventoryShape;
                if (s != null) { shapeW = s.width; shapeH = s.height; }
            }
            catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.WindowNeedsSync.shape", ex); }
            if (shapeW <= 0 || shapeH <= 0) return false;

            // S5-3：每格像素从网格自身反推，不再写死 16
            float cell = SaveBagExpandCore.CellPixelsOf(inv);
            int expectedPixW = Mathf.RoundToInt(shapeW * cell);
            int expectedPixH = Mathf.RoundToInt(shapeH * cell);

            int diffW = Math.Abs(window.widthPixels - expectedPixW);
            int diffH = Math.Abs(window.heightPixels - expectedPixH);

            return diffW > 32 || diffH > 32;
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.WindowNeedsSync", ex); return false; }
    }

    private static void SyncWindowRect(PixelWindow window)
    {
        if (window == null) return;
        RectTransform rt = window.rectTransform;
        if (rt == null) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

        // S8s：MarkAllDirty 与内联写法逐行等价（一个 rt 自标 + 所有子 Graphic），
        //   统一走 GridFx 保证跨 DLL 一处改。
        // ★ S8t 澄清：S8s 时曾误以为 MarkAllDirty 导致主仓库窗口位置异常，
        //   实为 EmporiumEntry.Validate(true) 引发（见禁止事项 #21）。
        //   本条改动与位置问题**无关**，可以放心统一。
        GridFx.MarkAllDirty(rt, "savebag-syncwindow");
    }

    // ────────── 小工具 ──────────

    private static GameGridInventory TryGetAttachedGrid(PixelWindow window)
    {
        if (window == null) return null;
        try
        {
            PixelElement el = window.childElement;
            if (el == null) return null;
            return ((Il2CppObjectBase)el).TryCast<GameGridInventory>();
        }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.TryGetAttachedGrid", ex); return null; }
    }

    // S8s：SameObject 已抽到 ContainerSuite.Shared.InteropUtil。

    // S8s：Ptr 已抽到 ContainerSuite.Shared.InteropUtil。

    // S8s：TrySilent 本体已抽到 ContainerSuite.Shared.InteropUtil。
    //      保留一层 1 行转发 —— 15 处调用点不用改，且 where 键保持
    //      "savebag-refresh" 与历史日志一致（排查时日志格式不变）。
    private static void TrySilent(Action a) => InteropUtil.TrySilent(a, "savebag-refresh");
}

[HarmonyPatch(typeof(StoreShutterButton), "OpenShutter")]
internal static class StoreShutterOpenPatch
{
    private static void Postfix()
    {
        if (WorldReadyGate.ShouldDefer) return;
        try
        {
            EmporiumEntry entry = EmporiumEntry.Instance;
            if (entry == null) return;
            EmporiumEntryStartPatch.ApplyAllResize(entry);
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Error($"  开门后重设尺寸失败: {ex}");
        }
    }
}

[HarmonyPatch(typeof(EmporiumEntry), "OnArriveStore")]
internal static class EmporiumEntryArrivePatch
{
    // ★ 2026-10-01：OnArriveStore 内部游戏还没完成 native 初始化。
    //   延迟 1 秒后再应用，等游戏 native 完成。
    private static int _generation = 0;

    private static void Postfix(EmporiumEntry __instance)
    {
        int myGen = ++_generation;
        try { MelonCoroutines.Start(DelayedApply(__instance, myGen)); }
        catch (Exception ex) { SaveBagExpandCore.Diag("  延迟应用安排失败: " + ex.Message); }
    }

    private static IEnumerator DelayedApply(EmporiumEntry entry, int myGen)
    {
        yield return new WaitForSeconds(1.0f);

        if (_generation != myGen) yield break;
        if (entry == null) yield break;

        try { EmporiumEntryStartPatch.ApplyAllResizeForce(entry); }
        catch (Exception ex) { SaveBagExpandCore.Diag("  延迟应用异常: " + ex.Message); }
    }
}
/// <summary>主商店 / 博士商店 / 未知。</summary>
internal enum FrontInvMode { Unknown, MainStore, DoctorShop }

/// <summary>
/// S5-2：用游戏自己的调用信号判断"当前在主商店还是博士商店"，
/// 取代"形状 == 14x21 就算博士商店"的特征识别。
/// 形状识别降级为 tracker 仍为 Unknown 时的兜底。
/// </summary>
internal static class FrontInvModeTracker
{
    private static FrontInvMode _current = FrontInvMode.Unknown;

    internal static FrontInvMode Current => _current;

    internal static void Set(FrontInvMode mode, string reason)
    {
        // 只在真正变化时才记日志：这两个方法会被游戏反复调用，否则日志刷屏
        if (_current == mode) return;
        _current = mode;
        try { SaveBagExpandCore.Diag($"[S5-2] FrontInv 模式 → {mode}（{reason}）"); }
        catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.FrontInvModeTracker.log", ex); }
    }

    /// <summary>读档 / 换存档槽时调用：回到 Unknown，由形状兜底接管直到游戏再次给出信号。</summary>
    internal static void Reset(string reason) => Set(FrontInvMode.Unknown, reason);
}

/// <summary>主商店：游戏把客户柜台复位成 18x4 时会调这个。</summary>
[HarmonyPatch(typeof(EmporiumEntry), "ResetFrontCounterGrid")]
internal static class ResetFrontCounterGridPatch
{
    [HarmonyPostfix]
    private static void Postfix(EmporiumEntry __instance)
    {
        // ★ 2026-10-10：同帧窗口同步（修复"网格好了窗口没好"的半帧滞后）。
        //   游戏这个方法只 SetShape(改网格像素)、不碰窗口 —— 见 SyncFrontCounterWindowNow 头注释。
        //   加载期一律跳过（禁止事项 #22：加载深栈 SetShape → 0x80131506，托管 catch 抓不到）；
        //   ResizePixels 走的是本 mod 已在维护块跑过的同一路径，无新增危险面。
        if (__instance != null && !WorldReadyGate.ShouldDefer)
        {
            try { EmporiumEntryStartPatch.SyncFrontCounterWindowNow(__instance, "ResetFrontCounterGrid"); }
            catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.ResetFrontCounterGrid.sync", ex); }
        }
        FrontInvModeTracker.Set(FrontInvMode.MainStore, "游戏调 ResetFrontCounterGrid");
    }
}

/// <summary>博士商店：游戏把客户柜台扩成 14x21 时会调这个。</summary>
[HarmonyPatch(typeof(EmporiumEntry), "ExpandFrontCounterGrid")]
internal static class ExpandFrontCounterGridPatch
{
    [HarmonyPostfix]
    private static void Postfix(EmporiumEntry __instance)
    {
        // ★ 2026-10-10：同帧窗口同步（同上）。注意顺序：先同步窗口再置 tracker ——
        //   同步用的是"当前网格形状"，与 tracker 无关，置 tracker 只为 ApplyAllResize 的排除逻辑。
        if (__instance != null && !WorldReadyGate.ShouldDefer)
        {
            try { EmporiumEntryStartPatch.SyncFrontCounterWindowNow(__instance, "ExpandFrontCounterGrid"); }
            catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.ExpandFrontCounterGrid.sync", ex); }
        }
        FrontInvModeTracker.Set(FrontInvMode.DoctorShop, "游戏调 ExpandFrontCounterGrid");
    }
}

// ══════════════════════════════════════════════════════════════════════════
// 地面/拾荒窗口尺寸（2026-10-09 新增，第 9 个容器）
//
// ★ 为什么单独挂 SetupAfterhourInv 而不是只靠 ApplyAllResize(EmporiumEntry.Start)：
//   「地面」的网格/窗口是在 SetupAfterhourInv 内部**当时**创建的
//   （反汇编 0x7D3F10 已证实：GameGridInventory.ctor(12,9) → this+0x148；
//     PixelWindow.ctor → this+0x140）。这个方法由游戏在合适的时机调用
//   （进拾荒场景 / 需要建地面时），**不保证**一定在 EmporiumEntry.Start
//   的那一次 ApplyAllResize 之前 —— 若之后才建，Start 那次就会因
//   afterhourInventory==null 而跳过。挂它自己的 Postfix ⇒ 网格刚建好就改，
//   不依赖 Start 的时序，也不需要像 PickupWindowTest 那样用 Show 兜底。
//
// ★ 安全：SetupAfterhourInv 是 public 实例方法、有 native body（dump.cs 确认），
//   非抽象/非 new-virtual ⇒ 可安全挂载（对照 MEMORY 铁律「挂前确认有 native body」）。
//   它**不是**属性访问器 ⇒ 不触碰 FBD #26 / R4（热 getter/setter 禁区）。
//
// ★ 加载期保护：Postfix 首行判 WorldReadyGate.ShouldDefer（与所有 SBE 补丁一致）。
//   加载期一律放行给原版，等门开后再由后续流程接管（延迟刷新/读档恢复都会重跑）。
// ══════════════════════════════════════════════════════════════════════════
[HarmonyPatch(typeof(EmporiumEntry), "SetupAfterhourInv")]
internal static class SetupAfterhourInvPatch
{
    private static void Postfix(EmporiumEntry __instance)
    {
        try
        {
            if (__instance == null) return;
            // 加载期不做网格重操作（禁止事项 #22：加载深栈 SetShape → 0x80131506，托管 catch 抓不到）。
            if (ContainerSuite.Shared.WorldReadyGate.ShouldDefer) return;
            if (!SaveBagExpandCore.Enabled) return;
            if (!SaveBagExpandConfig.ExpandEnabled.Value) return;

            GameGridInventory inv = null;
            PixelWindow win = null;
            try { inv = __instance.afterhourInventory; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.SetupAfterhourInv.inv", ex); }
            try { win = __instance.afterhourWindow; } catch (Exception ex) { CatchLogOnce.Warn("sbe.patch.SetupAfterhourInv.win", ex); }
            if (inv == null) return;

            // 复用同一个 Resize()（幂等 + 窗口同步 + 填充底修复），与其它 8 个容器同构。
            // 走 ApplyAllResizeForce 会连带重设全部容器 + EmporiumEntry.Validate，
            // 在地面刚建好这一刻过重；这里只针对地面容器做一次 —— 其它容器由
            // Start / OnArriveStore / OpenShutter 的正常流程负责。
            SaveBagExpandCore.Diag(
                "  [地面] SetupAfterhourInv Postfix：网格已建，应用目标尺寸 " +
                SaveBagExpandConfig.Clamp(SaveBagExpandConfig.AfterhourWidth.Value) + "x" +
                SaveBagExpandConfig.Clamp(SaveBagExpandConfig.AfterhourHeight.Value));
            EmporiumEntryStartPatch.ResizeOneContainer(
                inv, win, "地面",
                SaveBagExpandConfig.AfterhourWidth.Value,
                SaveBagExpandConfig.AfterhourHeight.Value);
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("sbe.patch.SetupAfterhourInv", ex);
        }
    }
}