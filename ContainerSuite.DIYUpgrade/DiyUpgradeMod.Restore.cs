// 2026-10-02：读档尺寸恢复功能重建。
//
// 背景：2026-09-29 为了规避 CLR fatal 0x80131506，TryRestoreRememberedGridShape 被整体
//   改成 `return false`，方法体丢失（源码树和参考工程里都只剩空壳）。
// 来源：从 D:\games\mod调试\_backup_deploy\20260926-1625\DIYUpgrade.dll（禁用之前的版本）
//   反编译取回原始逻辑，再按下面的原则重写。
//
// ★★ 为什么必须拆成小方法
//   本方法被 XIAOWO 兼容补丁（SkipXiaowoBayRestore.Prefix_Bool）挂了 Harmony 钩子。
//   被挂钩的方法体过大时，JIT 编译会与 MonoMod 的 CompileMethodHook 冲突 →
//   CLR fatal 0x80131506（托管 catch 抓不到，日志无堆栈）。
//   → 被挂钩的入口方法必须极小，全部逻辑下沉到未被挂钩的小方法里。
//
// ★ 与原始版本的唯一有意差异
//   原版在"窗口尺寸没变"的分支里会手写 rectTransform.sizeDelta / outsideBorder.sizeDelta。
//   按本项目禁止事项 #9（手写 sizeDelta 绕过画布缩放、破坏 GetWorldCorners 判定，已坑两次），
//   这里改为只做 ForceRecomputeDynamicBackground + FixBounds + Validate + 重建布局。
using ContainerSuite.Shared;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Channels;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    /// <summary>
    /// 入口：保持极小（XIAOWO 兼容补丁会挂 Harmony 钩子，方法体大了 JIT 会崩）。
    /// 真正逻辑全在 RestoreShapeCore 及其子方法里。
    /// </summary>
    internal static bool TryRestoreRememberedGridShape(GameItem item)
    {
        if (item == null) return false;
        if (_restoreOnLoad == null || !_restoreOnLoad.Value) return false;
        // 与本项目其它读状态补丁一致：加载期一律让路（RestoreArbiter 也有这道闸，这里是双保险）
        if (WorldReadyGate.ShouldDefer) return false;
        try
        {
            bool ok = RestoreShapeCore(item, out int changed);   // ← 关键：这行必须有
            // S8v：逐条日志收拢到 DiagLog —— 读档时可能 30~40 个容器，
            //   逐条打太吵；有「读档恢复完成，处理了 N 个容器」汇总行就够。
            if (_diagLog != null && _diagLog.Value)
                _log?.Msg($"[容器升级][恢复] {IdOf(item)} 应用了 {changed} 个网格形状（{(ok ? "ok" : "skip")}）");
            return ok;
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); return false; }
    }

    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 P3】恢复链 per-session memo
    //
    // 观察：R1 探针实测显示，同一物品在一次会话里会被反复请求恢复
    //   （Show / LoadGame / Arbiter 多触发点）。首跳 CHANGED=1 之后，
    //   后续每一跳都是 SKIP（changed=0）—— 但"SKIP"只是**没改东西**，
    //   整条链（CollectTargets → ApplyShapes → RememberPrimary → ApplyWindow）
    //   照样每次全跑一遍，含大量 IL2CPP 往返与 PlayerPrefs 写入。
    //
    // 修法：给"已恢复到稳态"的物品记一个状态签名。下次请求时若签名相同，
    //   直接判定"无事可做"，跳过整条链（这正是 SKIP 的语义）。
    //
    // ★★ 安全边界 —— 签名必须覆盖**全部**会让"这次其实有活干"的输入：
    //   ① targets（来自 tag）：玩家 DIY 升级后 tag 变 ⇒ targets 变 ⇒ 签名变 ⇒ 不跳过；
    //   ② 各网格当前形状：外部 mod 改了形状 ⇒ 签名变 ⇒ 不跳过（本来就要修）；
    //   ③ 窗口像素尺寸：外部 mod 只改窗口没改网格 ⇒ 签名变 ⇒ 仍会重跑 ApplyWindow。
    //   三者缺一都会造成"该恢复却跳过"的静默失效 —— 本套件最忌讳的故障形态。
    //
    // ★ 只在上一跳 changed==0 时记忆：changed>0 说明状态仍在向稳态收敛，
    //   不能拿"刚改完之前"的签名当稳态。
    // ★ 记忆的是**跑完之后**的签名（sigAfter），不是跑之前的 —— 否则
    //   "外部改坏 → 我们修好"这一跳会把"坏状态"记成稳态，下次白白重跑。
    // ★ per-session：仅内存字典，绝不落盘；场景切换 / 缓存失效时清空。
    // ═══════════════════════════════════════════════════════════════════
    private struct RestoreMemoEntry
    {
        public string Sig;
        public int Changed;
        public bool Ok;
    }
    private static readonly Dictionary<IntPtr, RestoreMemoEntry> _restoreMemo
        = new Dictionary<IntPtr, RestoreMemoEntry>(32);

    /// <summary>
    /// 计算"这次恢复面对的状态签名"（targets + 各网格当前形状 + 窗口像素）。
    /// 见上方 P3 注释：三项缺一即可能造成漏恢复。
    /// </summary>
    private static string RestoreStateSignature(
        List<(int Index, int W, int H)> targets,
        List<GameGridInventory> invs,
        PixelWindow win)
    {
        var sb = new System.Text.StringBuilder(64);
        try
        {
            sb.Append('T');
            if (targets != null)
                for (int i = 0; i < targets.Count; i++)
                    sb.Append(targets[i].Index).Append(':').Append(targets[i].W)
                      .Append('x').Append(targets[i].H).Append('|');

            sb.Append('G');
            if (invs != null)
                for (int i = 0; i < invs.Count; i++)
                {
                    GameGridInventory inv = invs[i];
                    var sh = inv != null ? inv.inventoryShape : null;
                    sb.Append(sh != null ? sh.width.ToString() : "?")
                      .Append('x')
                      .Append(sh != null ? sh.height.ToString() : "?").Append('|');
                }

            sb.Append('W');
            if (win != null) sb.Append(win.widthPixels).Append('x').Append(win.heightPixels);
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("RestoreStateSignature", ex);
            return null;   // 签名算不出来 = 不敢跳过（下方会当作 sig 为空处理）
        }
        return sb.ToString();
    }

    /// <summary>场景切换 / 新档加载时清空恢复链 memo（旧指针全部失效）。</summary>
    internal static void ClearRestoreMemo()
    {
        _restoreMemo.Clear();
    }

    private static bool RestoreShapeCore(GameItem item, out int changed)
    {
        changed = 0;
        IntPtr memoPtr = IntPtr.Zero;
        try { memoPtr = item != null ? item.Pointer : IntPtr.Zero; } catch { memoPtr = IntPtr.Zero; }

        CrashProbe.Enter("Core.GetGridDef");
        GridDef def = GetGridDefForItem(item);
        CrashProbe.Exit("Core.GetGridDef");
        if (def == null)
        {
            if (!WageContainerRegistry.IsWageBox(item) && !WageContainerRegistry.IsVoidBeadStorage(item))
                return false;

            RestoreMigrateWage(item);
            def = GetGridDefForItem(item);
            if (def == null) return false;
        }

        CrashProbe.Enter("Core.GetAllGrids");
        List<GameGridInventory> invs = GetAllGridInventories(item);
        CrashProbe.Exit("Core.GetAllGrids");
        if (invs.Count == 0) return false;

        CrashProbe.Enter("Core.CollectTargets");
        List<(int Index, int W, int H)> targets = RestoreCollectTargets(item, def, invs);
        CrashProbe.Exit("Core.CollectTargets");
        if (targets.Count == 0) return false;

        GameGridInventory primary = invs[targets[0].Index];
        if (primary?.inventoryShape == null
            || primary.inventoryShape.width <= 0 || primary.inventoryShape.height <= 0)
            return false;

        PixelWindow win = null;
        try { win = item.contentWindow; } catch (Exception ex) { CatchLogOnce.Warn(ex); }

        float cellW = 0f, cellH = 0f;
        int winW = 0, winH = 0, borderW = 0, borderH = 0;
        if (win != null)
        {
            cellW = (float)primary.widthPixels / (float)primary.inventoryShape.width;
            cellH = (float)primary.heightPixels / (float)primary.inventoryShape.height;
            winW = win.widthPixels;
            winH = win.heightPixels;
            borderW = Math.Max(0, winW - primary.widthPixels);
            borderH = Math.Max(0, winH - primary.heightPixels);
        }

        // ── P3 memo：签名未变 且 上次跑完无事可做 ⇒ 整条链等价空转，直接跳过 ──
        //   对外返回值与"跑完整条链且 changed=0"完全一致（ok=true, changed=0），
        //   所以 TryRestoreRememberedGridShape 及上层 Arbiter 无需任何适配。
        string sigBefore = RestoreStateSignature(targets, invs, win);
        if (memoPtr != IntPtr.Zero && sigBefore != null)
        {
            RestoreMemoEntry memo;
            if (_restoreMemo.TryGetValue(memoPtr, out memo)
                && memo.Changed == 0 && memo.Ok
                && string.Equals(memo.Sig, sigBefore, StringComparison.Ordinal))
            {
                // （2026-10-13：R1 探针的 MEMO-SKIP 记录随探针一并删除。）
                return true;
            }
        }

        CrashProbe.Enter("Core.ApplyShapes");
        changed = RestoreApplyShapes(item, invs, targets);
        CrashProbe.Exit("Core.ApplyShapes");
        CrashProbe.Enter("Core.RememberPrimary");
        RestoreRememberPrimary(item, invs);
        CrashProbe.Exit("Core.RememberPrimary");

        if (win != null && cellW > 0f && cellH > 0f)
        {
            CrashProbe.Enter("Core.ApplyWindow");
            RestoreApplyWindow(win, primary, targets[0].W, targets[0].H,
                               cellW, cellH, winW, winH, borderW, borderH);
            CrashProbe.Exit("Core.ApplyWindow");
        }

        // ── P3 memo：记录「跑完之后」的状态签名 ──
        //   用 sigAfter 而不是 sigBefore，否则"外部改坏 → 我们修好"这一跳
        //   会把坏状态记成稳态，下次白白重跑整条链。
        //   changed>0 时也记 —— 下一跳若签名相同（已收敛）即可跳过；
        //   而 changed>0 本身会让**下一跳之前**的任何外部改动改变签名 ⇒ 不会漏恢复。
        if (memoPtr != IntPtr.Zero)
        {
            try
            {
                string sigAfter = RestoreStateSignature(targets, invs, win);
                if (sigAfter != null)
                    _restoreMemo[memoPtr] = new RestoreMemoEntry { Sig = sigAfter, Changed = changed, Ok = true };
            }
            catch (Exception ex) { CatchLogOnce.Warn("RestoreShapeCore.Memo", ex); }
        }

        return true;
    }

    /// <summary>Wage 老容器迁移（原 TryRestoreRememberedGridShape 开头那一段）。</summary>
    private static void RestoreMigrateWage(GameItem item)
    {
        try { _ggiCache.Clear(); }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }

        List<GameGridInventory> invs = GetAllGridInventories(item);
        if (invs.Count > 0 && invs[0]?.inventoryShape != null)
        {
            if (WageContainerRegistry.IsWageBox(item))
                WageContainerRegistry.MigrateLegacyWageBox(item, invs[0]);
            else
                WageContainerRegistry.MigrateLegacyVoidBead(item, invs[0]);

            try { _ggiCache.Clear(); }
            catch (Exception ex) { CatchLogOnce.Warn(ex); }
        }
    }

    /// <summary>收集"需要恢复到什么尺寸"的槽位清单。</summary>
    private static List<(int Index, int W, int H)> RestoreCollectTargets(
        GameItem item, GridDef def, List<GameGridInventory> invs)
    {
        var list = new List<(int, int, int)>();
        foreach (var entry in GetEnabledSlotsWithIndex(def))
        {
            int idx = entry.Index;
            if (idx < 0 || idx >= invs.Count) continue;
            GameGridInventory inv = invs[idx];
            if (inv?.inventoryShape == null) continue;

            int w = idx == 0 ? TagUtil.SafeTagInt(item, "DIY_UPGRADE_W_TAG", 0) : TagUtil.SafeTagInt(item, TagSlotW(idx), 0);
            int h = idx == 0 ? TagUtil.SafeTagInt(item, "DIY_UPGRADE_H_TAG", 0) : TagUtil.SafeTagInt(item, TagSlotH(idx), 0);
            if (w <= 0 || h <= 0) continue;

            if (TryGetGridShapeLimits(item, out int minC, out int minR, out int maxC, out int maxR))
            {
                if (w < minC) w = minC;
                if (h < minR) h = minR;
                if (w > maxC) w = maxC;
                if (h > maxR) h = maxR;
            }

            if (CanContainItems(inv, w, h)) list.Add((idx, w, h));
            else
            {
                // ★ D-1（2026-10-12）：cfg 上限调小后容器内物品越界 → 静默跳过。
                //   玩家视角是"改了 cfg 没反应"，其实是物品挡住了缩小。留一条 **Warning**：
                //   走 LogOnce 的 warning 分支 = **无条件**输出，不受 z1_DiagLog 门控 ——
                //   这是玩家可见的结果类提示，默认静默等于没做（项目教训：
                //   默认静默的日志会被误读成"没执行"）。
                //   3 秒去重防刷屏；msg 含容器 id + 槽号，不同 容器×槽 各留一条。
                //   注意 w/h 在此处已是 clamp 后的值（上方 TryGetGridShapeLimits 夹过
                //   [minC..maxC]×[minR..maxR]），日志打印的是"实际尝试的目标尺寸"。
                LogOnce($"[容器升级] {IdOf(item)} 槽 {idx} 目标尺寸 {w}x{h} 小于容器内物品占用范围，"
                    + "本次未恢复（请先清空容器，或把 cfg 上限调回原值）", warning: true);
            }
        }
        return list;
    }

    /// <summary>应用形状，返回真正改动过的槽位数。</summary>
    private static int RestoreApplyShapes(GameItem item, List<GameGridInventory> invs, List<(int Index, int W, int H)> targets)
    {
        int changed = 0;
        foreach (var t in targets)
        {
            GameGridInventory inv = invs[t.Index];
            if (inv?.inventoryShape == null) continue;
            int beforeW = inv.inventoryShape.width, beforeH = inv.inventoryShape.height;
            if (beforeW == t.W && beforeH == t.H) continue;

            SetShapeKeepTransparency(inv, t.W, t.H);
            try { inv.ForceRecomputeDynamicBackground(); }
            catch (Exception ex) { CatchLogOnce.Warn(ex); }
            changed++;
        }
        return changed;
    }

    private static void RestoreRememberPrimary(GameItem item, List<GameGridInventory> invs)
    {
        if (invs.Count == 0) return;
        GameGridInventory inv = invs[0];
        if (inv?.inventoryShape == null) return;

        TagUtil.SetItemTag(item, "DIY_UPGRADE_W_TAG", inv.inventoryShape.width);
        TagUtil.SetItemTag(item, "DIY_UPGRADE_H_TAG", inv.inventoryShape.height);
    }

    private static void RestoreApplyWindow(PixelWindow win, GameGridInventory primary,
                                           int targetW, int targetH,
                                           float cellW, float cellH,
                                           int winW, int winH, int borderW, int borderH)
    {
        int newW = (int)Math.Round((float)targetW * cellW) + borderW;
        int newH = (int)Math.Round((float)targetH * cellH) + borderH;

        bool active = false;
        try
        {
            RectTransform rt = win.rectTransform;
            if (rt != null) active = ((Component)rt).gameObject.activeInHierarchy;
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }

        if (newW != winW || newH != winH)
        {
            ApplyWindowSize(win, primary, newW, newH, borderW, borderH);
        }
        else if (active)
        {
            RestoreRefreshWindow(win, primary);
        }

        RestoreSetAllDirty(primary);
    }

    /// <summary>尺寸没变但窗口已激活：重整布局（不手写 sizeDelta，见文件头说明）。</summary>
    private static void RestoreRefreshWindow(PixelWindow win, GameGridInventory grid)
    {
        try
        {
            try { grid.ForceRecomputeDynamicBackground(); }
            catch (Exception ex) { CatchLogOnce.Warn(ex); }

            win.FixBounds();

            RectTransform rt = win.rectTransform;
            if (rt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rt);

            if (win.outsideBorder != null && win.outsideBorder.rectTransform != null)
                LayoutRebuilder.ForceRebuildLayoutImmediate(win.outsideBorder.rectTransform);

            win.Validate();
            ((GameInventory)grid).Validate();
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    private static void RestoreSetAllDirty(GameGridInventory grid)
    {
        try
        {
            Component comp = ((Il2CppObjectBase)grid).TryCast<Component>();
            if (comp == null || comp.gameObject == null) return;
            foreach (Graphic g in comp.gameObject.GetComponentsInChildren<Graphic>(true))
                if (g != null) g.SetAllDirty();
        }
        catch (Exception ex) { CatchLogOnce.Warn(ex); }
    }

    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-09 读档崩溃定位】启动期预热"读档恢复链"的 JIT。
    //
    // 为什么加：读档完成后约 0.3s 崩溃在
    //   MonoMod.RuntimeDetour.Platforms.DetourRuntimeNETCore30Platform.CompileMethodHook
    //   （CLR fatal 0x80131506，Windows 事件日志记为 coreclr.dll 0xc0000005）。
    //   SBE 侧有自己的 WarmupJit 且已实机验证有效（读档前 ~40s 在主菜单就把方法
    //   全部编译完），CI / DIY 两侧此前**没有**任何预热。
    //   本方法用与 SBE.WarmupJit 同构的 Mode A（真实调用）把 DIY 读档链在启动期
    //   （主菜单、浅栈、无 Harmony detour 在栈上）点着，读档期不再触发首次编译。
    //
    // 每步一行 `[DIY-JIT] > ...`：若某方法编译本身就崩，控制台**最后一行**即凶手。
    // ★ 2026-10-09：这些逐步打点已改走 Diag（受 DiagLog 总闸控制）——
    //   一次预热要打 ~22 行，是 Latest.log 的最大单一来源之一；
    //   「想知道崩在哪一步」是排障需求，不该常驻。排障时开 DiagLog 即可复原。
    //
    // ★ 只做 Mode A。Mode B（PrepareMethod 预编译）已在 SBE 侧被证明可致
    //   0x80131506，本项目 R3 门禁零豁免 —— 本文件禁止出现该 API。
    // ★ 【2026-10-09 修正】次数由 40 降为 8，**刻意不越过 tier1 阈值（约 30）**。
    //   原写法（40 次）本意是"把 tier1 重编译提前到启动期"，但 CoreCLR 的 tier1
    //   是**后台线程异步**编译的：启动期连调 40 次只是把一批方法**排队**，真正的
    //   tier1 编译仍由后台 JIT 线程在稍后（正好落在读档前后）执行。后台线程崩 =
    //   FailFast，主线程毫不知情地继续跑若干帧 —— 与实机现象（Fatal error 打印后
    //   日志仍续写 ~13ms、栈顶固定为 MonoMod…CompileMethodHook）完全吻合。
    //   改为低于阈值后：首次调用即完成 tier0 编译（足以消除"读档期首次编译"），
    //   且不再排队 tier1 重编译。对照组：SBE 的 WarmupJit 每方法只调 1 次、从不
    //   越过阈值，也是三者中唯一被实机验证"有效"的。
    // ★ 全部以 null 调用 + try/catch：这些方法首行基本都有 `if (item == null) return`；
    //   即便抛异常也已达成"编译"目标（JIT 在方法体执行前完成整段编译）。
    // ═══════════════════════════════════════════════════════════════════
    private static bool _saveLoadChainWarmed;
    private const int WarmupInvokeCount = 1;   // 对齐 SBE 实机验证值；禁用分层 JIT 后单次调用即够

    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-09 第二轮 / 2026-10-10 方案 A】预热「所有补丁方法本体」的 JIT。
    //
    // 根因（Resizer.cs:406 / WorldReadyGate.cs:71 注释同述）：被挂钩的方法体若在
    // 「MonoMod detour 还在栈上」时被首次 JIT 编译，old MonoMod(2022) 的
    // DetourRuntimeNETCore30Platform.CompileMethodHook 会读空指针 → CLR fatal
    // 0x80131506（托管 catch 抓不到，日志无堆栈）。
    //
    // 此前 WarmupSaveLoadChain 只热身了若干 helper，从未预热补丁方法本体 —— 它们
    // 首次编译落在玩家第一次交互（拖拽 / 悬停 / 读档）的 detour 深栈上，正是崩点。
    //
    // 做法：在启动期（Tick 首次调用，Melon 的 OnUpdate 栈，无游戏 detour 在栈上）
    // 用安全哑参把每个 Prefix/Postfix/Transpiler 各 invoke 一次，强制在浅栈完成编译；
    // 真实调用时方法已是编译态，不再触发 CompileMethodHook。
    //   · ShouldDefer 在启动期为 true → 绝大多数补丁首行即早退，无副作用；
    //     无守卫的补丁（如布局 getter）传 null 至多 NRE（托管异常，已 catch）。
    //   · 唯一跳过 LoadGameRestorePatch：它无 ShouldDefer 守卫，invoke 会误触
    //     Invalidate + 起协程，副作用过大。它的崩溃候选 DelayedRestore 协程由
    //     下方 WarmupSaveLoadChain 的 helper 链覆盖不到，但 LoadGame 路径本就晚于
    //     启动期；若仍崩在 LoadGame，再单独处理。
    //
    // ★★★ 【2026-10-10 方案 A 关键修复】原实现只遍历带 `[HarmonyPatch]` **类特性**
    //   的补丁类 ⇒ **4 处手动 `harmony.Patch(...)` 注册的补丁全部漏预热**：
    //     SkipXiaowoBayRestore(7) / SkipWageUpgradeRestore(多条) /
    //     ContainerExpandPatch(2) / WindowShowHook(所有 Show 重载)。
    //   崩溃实例：SweepZombieHandles 首次真清掉手柄后，XIAOWO 下帧重走被挂路径
    //   （Attach 等），我们挂在 XIAOWO 方法上的 Prefix 首次 JIT → CompileMethodHook 崩。
    //   修法：新增 Shared.WarmupRegistry —— 手动注册处"注册成功即登记"，
    //   这里**先预热 Registry 里的**，再跑 `[HarmonyPatch]` 枚举（两路互补、去重）。
    // ═══════════════════════════════════════════════════════════════════
    private static bool _patchMethodsWarmed;
    internal static void WarmupAllPatchMethods()
    {
        if (_patchMethodsWarmed) return;
        _patchMethodsWarmed = true;
        try
        {
            var asm = typeof(DiyUpgradeMod).Assembly;   // == ContainerSuite.Infrastructure 程序集（含 SaveBag/CI/DIY）

            // ── 第零路（2026-10-10 P2）：声明式 [JitWarmup] 叶子 ──
            //   必须在 WarmupAll() **之前**扫描 —— 扫出来的方法要进同一张表，
            //   由下面那一次 WarmupAll() 统一在浅栈 invoke。
            //   取代原先 WarmupSaveLoadChain 里 8 行手抄的 Step(...) 硬编码清单：
            //   漏登记 = 该叶子退回"在 trampoline 上首编译" = 我们正在防的崩溃，
            //   而硬编码清单**没有任何机制**提醒作者补登记。
            int declared = ContainerSuite.Shared.WarmupRegistry.ScanAndRegister(asm);

            // ── 第一路：手动注册的补丁（核心修复）──
            //   这些方法的类不带 [HarmonyPatch] 特性，下面的枚举永远看不到它们，
            //   必须由注册处自己登记（见 WarmupRegistry 类注释）。
            int manual = ContainerSuite.Shared.WarmupRegistry.WarmupAll();

            // ── 第二路：带 [HarmonyPatch] 类特性的补丁（原有逻辑）──
            int n = 0;
            foreach (var t in asm.GetTypes())
            {
                if (!t.IsDefined(typeof(HarmonyPatch), false)) continue;
                if (t.Name == "LoadGameRestorePatch") continue;   // 见上：副作用大，跳过
                foreach (var m in t.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (!(m.IsDefined(typeof(HarmonyPrefix), false)
                       || m.IsDefined(typeof(HarmonyPostfix), false)
                       || m.IsDefined(typeof(HarmonyTranspiler), false)
                       || m.IsDefined(typeof(HarmonyFinalizer), false)))
                        continue;
                    try
                    {
                        var ps = m.GetParameters();
                        var args = new object[ps.Length];
                        for (int i = 0; i < ps.Length; i++)
                        {
                            var p = ps[i];
                            if (p.ParameterType.IsByRef)
                            {
                                var elem = p.ParameterType.GetElementType();
                                args[i] = (elem != null && elem.IsValueType) ? Activator.CreateInstance(elem) : null;
                            }
                            else if (p.ParameterType.IsValueType)
                                args[i] = Activator.CreateInstance(p.ParameterType);
                            else
                                args[i] = null;
                        }
                        m.Invoke(null, args);   // 编译发生在方法体执行前；即便抛 NRE 也已达成预热目标
                        n++;
                    }
                    catch { /* 编译已在执行前完成；任何异常一律忽略 */ }
                }
            }
            // ── P2-2（2026-10-10）：声明式预热的覆盖率断言 ──
            //   声明式改造把"手抄 Step 清单"换成方法头上的 [JitWarmup]，代价是
            //   "某个叶子被漏打特性"不会有任何编译期症状（静默退化成在 trampoline 上首编译
            //   = 我们在防的 0x80131506）。这里用一份"必须预热"清单做集合差自检。
            VerifyJitWarmupCoverage();

            // ★ 无条件打印（不再走 Diag 闸门）：否则"预热到底跑没跑、跑了几个"零线索
            //   —— 本套件吃过「静默失效」的亏，这条是崩溃防线，必须始终可见。
            _log?.Msg($"[DIY-JIT] 补丁方法预热完成：[JitWarmup] 声明 {declared} 个 + 手动注册 {manual} 个 "
                + $"+ [HarmonyPatch] {n} 个 —— 其中手动+声明共 {manual} 个已在浅栈编译"
                + $"（登记表容量 {ContainerSuite.Shared.WarmupRegistry.Count}）");
        }
        catch (Exception ex) { _log?.Warning("[DIY-JIT] 补丁方法预热异常: " + ex.Message); }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // P2-2（2026-10-10）：声明式预热的**覆盖率断言**
    //
    // 病：声明式 [JitWarmup] 把"漏登记"从"忘了抄一行 Step"变成了"忘了打一个特性"，
    //   而后者**编译期完全无症状** —— 结果是该叶子退回"在 trampoline 上首编译"，
    //   正是 0x80131506 的成因（且崩溃无堆栈、不可 catch）。
    //
    // 药：把"必须预热到的叶子"钉成一份**单一事实源清单**（MustWarmLeaves），
    //   启动期与实际扫描结果做集合差：
    //     · 缺项（清单有、扫描没有）→ Error + [REG.BP] name=jitwarmup.missing 留痕；
    //     · 多项（扫描有、清单没有）→ Msg 提示（新增叶子后请同步清单；不报错，
    //       否则每加一个合法叶子都要先改清单，噪音大于收益）。
    //   ⇒ 故意摘掉一个 [JitWarmup] 即触发：回归报告底部"断点留痕"出现缺失告警。
    //
    // 名字格式 = "类型名.方法名"（与 WarmupRegistry.RecordDeclared 一致）。
    //   ★ ContainerWindowResizer 是 DiyUpgradeMod 的**嵌套类**，其反射 Type.Name
    //     就是 "ContainerWindowResizer"（不是 "DiyUpgradeMod+ContainerWindowResizer"）。
    // ★ 新增升级链叶子时，除了在方法头打 [JitWarmup]，务必把它补进本清单。
    // ═══════════════════════════════════════════════════════════════════════
    private static readonly string[] MustWarmLeaves =
    {
        "DiyUpgradeMod.IsHorizontalLayout",
        "DiyUpgradeMod.ComputeMultiSlotWindowSize",
        "DiyUpgradeMod.SyncXiaowoBayTags",
        "DiyUpgradeMod.RememberGridShape",
        "DiyUpgradeMod.RememberSlotShape",
        "DiyUpgradeMod.RememberWindowSize",
        "ContainerWindowResizer.Attach",
        "WageContainerRegistry.OnVoidBeadUpgraded",
    };

    /// <summary>核对"必须预热的叶子"是否都被 [JitWarmup] 声明并已登记。见上方段落注释。</summary>
    private static void VerifyJitWarmupCoverage()
    {
        try
        {
            IReadOnlyList<string> declared = ContainerSuite.Shared.WarmupRegistry.DeclaredNames;
            var have = new HashSet<string>(declared, StringComparer.Ordinal);
            var expected = new HashSet<string>(MustWarmLeaves, StringComparer.Ordinal);

            var missing = new List<string>();
            foreach (string leaf in MustWarmLeaves)
            {
                if (!have.Contains(leaf)) missing.Add(leaf);
            }

            if (missing.Count > 0)
            {
                _log?.Error($"[DIY-JIT] ⚠ 声明式预热清单缺失 {missing.Count} 个叶子：{string.Join("、", missing)}"
                    + " —— 它们会退回「在 trampoline 上首编译」，正是 0x80131506 的成因。"
                    + "请给对应方法补 [JitWarmup]（并确认没有把特性写错类型/漏 static）。");
                ContainerSuite.Shared.ContainerSuiteLog.Bp("jitwarmup.missing", "missing=" + string.Join(",", missing));
            }

            var extra = new List<string>();
            foreach (string d in declared)
            {
                if (!expected.Contains(d)) extra.Add(d);
            }
            if (extra.Count > 0)
            {
                _log?.Msg($"[DIY-JIT] 提示：有 {extra.Count} 个 [JitWarmup] 叶子不在「必须预热」清单里"
                    + $"（{string.Join("、", extra)}）—— 若确为新增叶子，请把它补进 MustWarmLeaves。");
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("VerifyJitWarmupCoverage", ex); }
    }

    internal static void WarmupSaveLoadChain()
    {
        if (_saveLoadChainWarmed) return;
        _saveLoadChainWarmed = true;

        // 第二轮（2026-10-09）：先把所有 [HarmonyPatch] 的 Prefix/Postfix/Transpiler
        // 本体在浅栈一次性编译完，避免它们首次编译落在玩家交互的 detour 深栈上。
        // 见 WarmupAllPatchMethods 注释。
        WarmupAllPatchMethods();

        // 【2026-10-10】XIAOWO 僵尸手柄清扫链的浅栈预热（集合 + 判据 + 反射成员）。
        //   清扫首次真跑发生在"世界就绪后"（TickZombieHandles 门禁 + 2s 消抖），
        //   这里把它的调用链能提前编的都提前编掉，进一步缩小首次 JIT 暴露面。
        try { SkipXiaowoBayRestore.WarmupJit(); }
        catch (Exception ex) { _log?.Warning("[DIY-JIT] XIAOWO 清扫链预热异常: " + ex.Message); }

        Diag("[DIY-JIT] 读档恢复链预热进入");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int steps = 0;

        void Step(string label, Action a)
        {
            try
            {
                Diag("[DIY-JIT] > " + label);
                for (int i = 0; i < WarmupInvokeCount; i++) a();
                steps++;
            }
            // ★【2026-10-10】改为 Diag（静默闸门）：哑参 null 触发的 NRE 是**预期**的
            //   （预热只需"方法体被 JIT 编译"这一步，编译发生在方法体执行前，
            //   之后抛 NRE 也不影响目标达成）。此前用 Warning 级别会让玩家以为出了故障，
            //   实测 Latest.log 里 RestoreApplyShapes/RestoreRememberPrimary 两条就属此类噪音。
            //   真需要看预热细节时开 DiagLog 即可。
            catch (Exception ex) { Diag($"[DIY-JIT] > {label} 异常(哑参预期): {ex.Message}"); }
        }

        // ── 读档入口链：RestoreArbiter → ReapplyFromMeta → TryRestoreRememberedGridShape → RestoreShapeCore ──
        Step("ReapplyFromMeta(null,true)（Arbiter 读档应用入口）", () => ReapplyFromMeta(null, true));
        Step("TryRestoreRememberedGridShape(null)（XIAOWO 挂钩入口，必须极小）", () => TryRestoreRememberedGridShape(null));
        Step("RestoreShapeCore(null,out _)", () => { RestoreShapeCore(null, out int _); });
        Step("RestoreMigrateWage(null)", () => RestoreMigrateWage(null));
        Step("RestoreCollectTargets(null,null,null)", () => RestoreCollectTargets(null, null, null));
        Step("RestoreApplyShapes(null,null,null)", () => RestoreApplyShapes(null, null, null));
        Step("RestoreRememberPrimary(null,null)", () => RestoreRememberPrimary(null, null));
        Step("RestoreApplyWindow(null,...)", () => RestoreApplyWindow(null, null, 0, 0, 0f, 0f, 0, 0, 0, 0));
        Step("RestoreRefreshWindow(null,null)（含 EH + il2cpp 包装器，高风险）", () => RestoreRefreshWindow(null, null));
        Step("RestoreSetAllDirty(null)（含 EH + GetComponentsInChildren）", () => RestoreSetAllDirty(null));
        Step("ApplyWindowSize(null,null,...)", () => ApplyWindowSize(null, null, 0, 0, 0, 0));

        // ── 读档恢复辅助链 ──
        Step("SeedBaselineIfMissing(null)", () => SeedBaselineIfMissing(null));
        Step("ClearReapplyCache(null)", () => ClearReapplyCache(null));
        Step("GetGridDefForItem/GetAllGridInventories/GetEnabledSlotsWithIndex/IsResizableContainer(null)",
            () => { GetGridDefForItem(null); GetAllGridInventories(null); foreach (var _ in GetEnabledSlotsWithIndex(null)) { } IsResizableContainer(null); });
        Step("TryGetGridShapeLimits(null,out...)/CanContainItems(null,0,0)",
            () => { TryGetGridShapeLimits(null, out int _a, out int _b, out int _c, out int _d); CanContainItems(null, 0, 0); });
        Step("SetShapeKeepTransparency(null,0,0)/RefreshAllGridsOf(null)",
            () => { SetShapeKeepTransparency(null, 0, 0); RefreshAllGridsOf(null); });
        Step("ContainerWindowResizer.ResetAllRestoreDone()", () => ContainerWindowResizer.ResetAllRestoreDone());

        // ═══════════════════════════════════════════════════════════════════
        // 【2026-10-10 方案 B · 升级 apply 路径「叶子方法」浅栈预热】
        //   —— 2026-10-10 P2 起改为**声明式**，下面这 8 行硬编码 Step 已删除：
        //        Step("IsHorizontalLayout(null,null)", ...);
        //        Step("ComputeMultiSlotWindowSize(...)", ...);
        //        Step("RememberWindowSize / RememberGridShape / RememberSlotShape", ...);
        //        Step("SyncXiaowoBayTags", ...);
        //        Step("WageContainerRegistry.OnVoidBeadUpgraded", ...);
        //        Step("ContainerWindowResizer.Attach", ...);
        //   现在由方法自己头上的 [JitWarmup] 声明，经 WarmupRegistry.ScanAndRegister
        //   在 WarmupAllPatchMethods 里统一收集（见那里的"第零路"）。
        //   ⇒ 新增叶子时只要加特性就自动进清单，不会再出现"漏登记 → 静默退化成
        //      在 trampoline 上首编译"。
        //
        //   10:37 铁律仍成立：升级链的**大方法**（PrepareUpgradeInternal /
        //   ApplyUpgradePlan / ConsumeMaterial / TryResizeGridWindow）绝不能进预热
        //   —— invoke 时内部会嵌套编译未编译方法 → 重入 CompileMethodHook
        //   → CLR fatal（自爆）。故**只暖叶子**：null 哑参即早退、内部只调已编译的
        //   TagUtil / 游戏原生方法，绝不触发嵌套 JIT。
        //   注：PlayUpgradeSound / ForceRebuildAfterUpgrade 在 ValidateShapeState
        //   返回后（顶层）才调用，运行期自然顶层编译即安全，故不打标记
        //   （前者避免启动期杂声、后者避免启动期起协程）。
        // ═══════════════════════════════════════════════════════════════════

        sw.Stop();
        // ★ 保持无条件（2026-10-09）：与 [SBE-JIT] 预热完成 同理 —— 这是"预热成功"的结论行，
        //   属于加载状态汇报，不是逐步诊断。
        _log?.Msg($"[DIY-JIT] 预热完成：steps={steps} 耗时={sw.ElapsedMilliseconds}ms");
    }
}
