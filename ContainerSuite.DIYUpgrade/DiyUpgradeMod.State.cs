// 由拆解脚本从 自定义集装箱fix/DiyUpgradeMod.cs 机械切分，内容逐字未改。
// 职责：静态字段、各类缓存、诊断开关
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ContainerSuite.Shared;   // S2 新增
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
    private static readonly HashSet<string> XiaowoBayIds = new HashSet<string>(StringComparer.Ordinal) { "machine_bay", "storage_bay", "machine_bay_ext", "storage_bay_large", "smuggler_bay", "smuggler_bay_mod" };
    // S3-1：_log 恢复为本地字段，由 OnInitializeMelon 里赋值。
    // 不再转发到 Shared 的 Logger（多 Mod 会覆盖）。
    private static MelonLogger.Instance _log;
    private static MelonPreferences_Entry<bool> _materialJunk;
    private static MelonPreferences_Entry<bool> _materialScrapMetal;
    private static MelonPreferences_Entry<bool> _materialMetalIngot;
    private static MelonPreferences_Entry<int> _cageHpPerCell;
    // 2026-10-07：原「每类别每槽」的 IncW/IncH 提升为全局两级。
    //   _incW/_incH 管所有原版容器；_modIncW/_modIncH 管所有 Mod 容器（三个 Wage GridDef）。
    //   TargetSlot.IncW/IncH 不再自建条目，统一指向这 4 个共享条目（见 CreateGridDef）。
    private static MelonPreferences_Entry<int> _incW;
    private static MelonPreferences_Entry<int> _incH;
    private static MelonPreferences_Entry<int> _modIncW;
    private static MelonPreferences_Entry<int> _modIncH;
    private static GridDef _box;
    private static GridDef _backpack;
    private static GridDef _cage;
    private static GridDef _multiSlot;
    private static GridDef _multiSlotModule;
    private static MelonPreferences_Entry<string> _upgradeKey;
    private static KeyCode _upgradeKeyCode = (KeyCode)308;
    private static bool _upgradeModCtrl;
    private static bool _upgradeModAlt;
    private static bool _upgradeModShift;
    private static MelonPreferences_Entry<bool> _enableDragResize;
    private static MelonPreferences_Entry<bool> _diagLog;
    /// <summary>读档后是否恢复容器尺寸（2026-10-02 恢复功能时用，出问题可一键关掉）。</summary>
    private static MelonPreferences_Entry<bool> _restoreOnLoad;
    /// <summary>
    /// 批量组拖升级开关（2026-10-11 阶段1）。默认 <b>true</b>。
    /// <para>
    /// 语义：按住升级键 + 框选多堆材料 + 拖到容器上松手 = 连续升级该容器。
    /// 只支持"同容器多材料"，不支持跨容器（见 DiyUpgradeMod.GroupDrag.cs 文件头）。
    /// </para>
    /// <para>
    /// ★ 补丁**始终常驻**，本开关只短路判据第一行（不 Unpatch、不重装）——
    ///   与 S8k 避让开关同一范式。IL2CPP 上动态 Unpatch/重装会重新生成
    ///   detour，是 0x80131506 的已知诱因。
    /// </para>
    /// </summary>
    private static MelonPreferences_Entry<bool> _groupDragEnabled;
    /// <summary>组拖开关的 null 安全读取（未初始化时按"开"处理，避免静默失效）。</summary>
    private static bool IsGroupDragEnabled
    {
        get { try { return _groupDragEnabled == null || _groupDragEnabled.Value; } catch { return true; } }
    }
    /// <summary>R1 实测探针：读档恢复时记录每个已升级容器的尺寸是否被改动。默认关，零噪音。</summary>
    /// <remarks>P1-3：探针仅在 DIAG 构建存在，故字段一并条件编译。</remarks>
#if DIAG
    private static MelonPreferences_Entry<bool> _r1Probe;
    // 2026-10-10 临时：拖拽缩放网格残留取证开关（见 DiyUpgradeMod.ResizeDiag.cs）。定性后删除。
    private static MelonPreferences_Entry<bool> _resizeDiag;
    // T5（2026-10-12，借鉴蛙哥的调用点、只做**实验**）：升级收尾是否补调
    // GameItem.SyncModifiedState()。默认关 —— 该 API 的语义未定论（只知道蛙哥在批量升级
    // 收尾与骰子吸收后调它，而本套件全树零调用），所以先做成可开关的观测，不直接进正式路径。
    private static MelonPreferences_Entry<bool> _syncStateProbe;
#endif
    // ── S8k：避让热开关（2026-10-02）────────────────────────────────────
    // 语义是「关掉 = 把尺寸主权交还给对方 mod」，默认全开（= 全权优先）。
    // ★ 虚空珠（void_bead_storage）单独处理（S8l 起）：
    //   S8k 时它刻意没有开关（与蛙哥材料重叠，主权必须唯一，一旦放开门会被
    //   「吞材料 + 尺寸回弹 + 等级不涨」三重锁死）。核桃实机反馈原版「拖垃圾
    //   逐格解锁」被完全锁死后，S8l 改为给一个**语义相反**的移交门
    //   _skipVoidBead：默认 true = 蛙哥全权（保原版机制），玩家按住升级键拖
    //   材料到虚空珠即永久移交 DIY。详见该字段自身的注释。
    /// <summary>是否避让 XIAOWO 精工扩舱（关掉 = 6 个 bay 的尺寸主权交还 XIAOWO）。</summary>
    private static MelonPreferences_Entry<bool> _skipXiaowoBay;
    /// <summary>是否避让蛙哥妙妙箱（关掉 = 满级妙妙箱的尺寸主权交还蛙哥）。</summary>
    private static MelonPreferences_Entry<bool> _skipWageBox;
    /// <summary>
    /// 虚空珠避让（S8l，2026-10-02）。默认 <b>true</b> = 避让开启 = 蛙哥全权。
    /// <para>
    /// ★ S8k 时这个开关不存在，虚空珠是"无条件接管"。核桃实机反馈原版
    /// "拖垃圾逐格解锁 1~200"的机制完全失效（窗口标题退回本体的"虚空珠 储物区"，
    /// 蛙哥那 4 处 titleString 一次都没跑到），要求改成"原机制先开、
    /// DIY 介入才全面接管"。
    /// </para>
    /// <para>
    /// S8l 起：默认 true = 不接管，蛙哥原机制正常玩。玩家**按住升级键
    /// （默认 LeftAlt）拖材料**的那一刻即视为表达"我要 DIY"，自动永久移交
    /// （打 TagVoidBeadHandedOver），之后才全面接管。
    /// 想彻底断掉移交可能（排查冲突）才把它关掉。
    /// </para>
    /// </summary>
    private static MelonPreferences_Entry<bool> _skipVoidBead;

    /// <summary>
    /// 【2026-10-10】僵尸手柄清扫开关（cfg 3D_SweepZombieHandles）。
    /// ★ 默认值**只在 <c>DiyUpgradeMod.Core.cs</c> 的 CreateEntry 写一次**（当前 false），此处不重复数字。
    /// 清扫实现为「熄火」（读 _handles → 每个手柄节点 SetActive(false)），不反射调 DestroyHandles。
    /// 读不到 / 未注册时返回 **false**（= 不清，fail-safe 方向是不做动作）。
    /// </summary>
    private static MelonPreferences_Entry<bool> _sweepZombieHandles;

    // ═══════════════════════════════════════════════════════════════════
    //  S8k：避让开关的读取入口（给两个避让模块用）
    //  读不到 / 未注册 / 抛异常时一律返回 true（= 保持避让 = 现状行为）。
    //  绝不能因为配置读取失败就静默放行 —— 那等于静默把主权交出去，
    //  是最难查的一类故障。
    // ═══════════════════════════════════════════════════════════════════
    internal static bool SkipXiaowoBayEnabled
    {
        get
        {
            try { return _skipXiaowoBay == null || _skipXiaowoBay.Value; }
            catch (System.Exception ex) { CatchLogOnce.Warn("读 SkipXiaoBay 失败，按开启处理", ex); return true; }
        }
    }

    /// <summary>
    /// 【2026-10-10】僵尸手柄清扫是否启用。
    /// ★ fail-safe 方向仍是「不做动作」：读不到 / 未注册 / 抛异常时返回 **false（不清）**
    /// —— 虽然 17:40 起清扫已改熄火（安全），但保守起见异常时仍不清。
    /// </summary>
    internal static bool SweepZombieHandlesEnabled
    {
        get
        {
            try { return _sweepZombieHandles != null && _sweepZombieHandles.Value; }
            catch (System.Exception ex) { CatchLogOnce.Warn("读 SweepZombieHandles 失败，按关闭处理", ex); return false; }
        }
    }

    internal static bool SkipWageBoxEnabled
    {
        get
        {
            try { return _skipWageBox == null || _skipWageBox.Value; }
            catch (System.Exception ex) { CatchLogOnce.Warn("读 SkipWageBox 失败，按开启处理", ex); return true; }
        }
    }

    /// <summary>
    /// 虚空珠是否仍归蛙哥（S8l）。
    /// <para>
    /// ★ 与另两个开关语义**相反**，这是故意的：另两个是"避让开=我方主权"，
    /// 而虚空珠默认避让开 = **蛙哥主权**（保住原版逐格解锁机制）。
    /// 玩家按住升级键时由 <see cref="VoidBeadHandedOver"/> 触发永久移交。
    /// </para>
    /// </summary>
    internal static bool SkipVoidBeadEnabled
    {
        get
        {
            try { return _skipVoidBead == null || _skipVoidBead.Value; }
            catch (System.Exception ex) { CatchLogOnce.Warn("读 SkipVoidBead 失败，按开启处理", ex); return true; }
        }
    }

    /// <summary>
    /// 虚空珠是否已永久移交给 DIY（S8l）。
    /// 移交后玩家就再也用不回蛙哥的"拖垃圾 +1 格"了 —— 这是**单向门**，
    /// 故意做成不可逆：否则玩家松开升级键就会掉回蛙哥，两边来回抖。
    /// </summary>
    internal static bool VoidBeadHandedOver(GameItem item)
    {
        if (item == null) return false;
        try
        {
            if (item.state == null) return false;   // 加载期 state 未初始化
            return item.IsTag(WageContainerRegistry.TagVoidBeadHandedOver);
        }
        // 逐帧判据：读 state/IsTag 失败按「未移交」处理，仅首次留痕。
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.VoidBeadHandedOver", ex); return false; }
    }

    /// <summary>
    /// 玩家是否正"按住升级键拖着可用材料"对着这个虚空珠 —— 即移交意图（S8l）。
    /// <para>
    /// ★ 本方法**只检测意图，绝不写 tag**。判据链（GetWageCategory）每帧被调几十次，
    /// 在那里写 tag 会导致：判据有副作用 → 无法推理 → 且与 HandOverVoidBead
    /// 两处写同一个 tag 容易打架。真正的移交动作统一在 EndDrag 里执行。
    /// </para>
    /// </summary>
    internal static bool VoidBeadHandoverRequested(GameItem item)
    {
        if (item == null) return false;
        if (!WageContainerRegistry.IsVoidBeadStorage(item)) return false;
        if (VoidBeadHandedOver(item)) return false;      // 已移交过，别再报意图
        if (WorldReadyGate.ShouldDefer) return false;     // 加载期不判意图
        if (RestoreArbiter.IsApplying) return false;     // DIY 正在写，别搅
        try
        {
            if (!DiyUpgradeMod.SkipVoidBeadEnabled) return false;  // 玩家已断掉移交可能
            // ★ 刻意**不要求**"正在拖着材料"：判据函数在拖拽全程都会被调，
            //   这里拿不到可靠的拖拽源。是否真拖着材料由 EndDrag 判定
            //   （那里能拿到 ItemMouseDragHandler 的 dragSource 指针）。
            return IsUpgradeKeyHeld();
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("WageRegistry:HandoverIntent", ex); return false; }
    }
    // ★ T1（2026-10-12，外部 mod 借鉴批次）：下面 3 个节流闸门改用 PollThrottle（单调钟）。
    //   原来是 DateTime.UtcNow 差值：系统时钟一旦回拨，(now - last) 变负 ⇒ "< N 秒" 恒真
    //   ⇒ 闸门卡死（松手不升级 / 配置不落盘），且全程无声。见 Shared/PollThrottle.cs 头注。
    //   ★ 注意与 2026-10-07 那批「A1」区分：那是缓存上限（CacheLimits），本批编号用 T*。
    private static PollThrottle _endDragGate;          // 原 _lastEndDragProcessed（0.4s）
    // _targetConsume 的时间戳要**只在真正扣料时**推进（单拖 / 组拖共用），
    // 所以这里不用 Due()，只存一个单调毫秒时刻。
    private static long _targetConsumeAtMs;
    private static bool _prefsDirty = false;
    private static PollThrottle _prefsFlushGate;      // 原 _lastPrefsFlush（2.0s）
    private static int _offsetSuspectCount = 0;
    private static bool _offsetWarningLogged = false;
    private static int _offsetCheckFails = 0;
    private static bool _offsetChecked = false;
    // P5（2026-10-02）：Drag 偏移的独立告警计数器。
    // VerifyDragOffset 的自校验只覆盖 Id 偏移（IsID 反查），DragSource / DragTarget /
    // DragTargetInv 三个裸偏移挂了没人知道。这里在 EndDrag 路径上单独计数 + 告警，
    // 让"Id 挂了"和"Drag 挂了"在日志里能区分。
    private static int _dragOffsetSuspectCount = 0;
    private static bool _dragOffsetWarningLogged = false;
    // ★ T1（2026-10-12）：下列缓存的时间戳一律改存**单调毫秒**（PollThrottle.NowMs）。
    //   原存 DateTime：时钟回拨时 `Elapsed < TTL` 恒真 ⇒ 缓存命中一个**过期的**判据
    //   （最坏情况：_prepareCache 里存着"不能升"，玩家就会长时间升不动且没有任何报错）。
    private static readonly Dictionary<string, (int BaseW, int BaseH, int Spent)> _metaCache = new Dictionary<string, (int, int, int)>();
    private static readonly Dictionary<IntPtr, long> _lastReapplyFromMetaTime = new Dictionary<IntPtr, long>();
    private static readonly Dictionary<IntPtr, (long Time, List<GameGridInventory> List)> _ggiCache = new Dictionary<IntPtr, (long, List<GameGridInventory>)>();
    private static readonly Dictionary<IntPtr, (long Time, bool Value)> _resizableCache = new Dictionary<IntPtr, (long, bool)>();
    private const int ResizableCacheMillis = 300;    // 原 ResizableCacheSeconds = 0.3
    private static readonly Dictionary<IntPtr, PrepareCacheEntry> _prepareCache = new Dictionary<IntPtr, PrepareCacheEntry>();
    private const int PrepareCacheMillis = 200;      // 原 PrepareCacheSeconds = 0.2
    [ThreadStatic] private static IntPtr _layoutWindow;
    [ThreadStatic] private static IntPtr _layoutInventory;
    [ThreadStatic] private static int _layoutWindowWidth;
    [ThreadStatic] private static int _layoutWindowHeight;
    [ThreadStatic] private static int _layoutChildWidth;
    [ThreadStatic] private static int _layoutChildHeight;
    // ★ T1（2026-10-12）：值由 DateTime 改为单调毫秒（判定见 Utils.LogOnce 的注释）。
    private static readonly Dictionary<string, long> _lastLogTimeByKey = new Dictionary<string, long>();
    // ★ T2（2026-10-12，借鉴蛙哥 ConsumedThisFrame 的**思路**）：同一容器「本帧是否已真升过」。
    //   键是容器裸指针 ⇒ 必须随场景 / 读档清空（已登记 CacheRegistry，见 RegisterSceneCaches）。
    //   ★ 命名以 Cache 结尾是有意的：若将来恢复 _shared 的 R6 门禁（Cache/Seen 结尾的
    //     private static Dictionary 必须登记），本字段天然在其判定范围内 —— 登记了就不会漏。
    private static readonly Dictionary<IntPtr, int> _upgradeStampCache = new Dictionary<IntPtr, int>();
    // P1（2026-10-02）：_lastHideBounds 已搬到 ContainerSuite.Shared.GridFx。
    // peak 内存缓存：tag 读回 0 时兜底，避免探测每秒重复触发。
    private static readonly Dictionary<IntPtr, (int PeakW, int PeakH)> _peakCache
        = new Dictionary<IntPtr, (int, int)>();
    // ═══════════════════════════════════════════════════════════════════════
    // S8u（2026-10-05）/ A1（2026-10-07）：缓存清理上限。
    //
    // 场景切换时这些缓存都会被 OnSceneLoaded 全清（不会跨场景累积），
    // 上限只防"单场景内长时间运行 + 大量容器"导致的无界增长。
    // 超限即整体 Clear —— 简单胜过 LRU，缓存的都是"能重建"的东西，
    // 清空的代价只是下次访问多算一次。
    //
    // A1：上限值不再散落在各文件里裸写（原 512/256/128/64/32 五种并存），
    //     全部统一到 ContainerSuite.Shared.CacheLimits 的 Large/Medium/Small 三级。
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>大缓存（本类）的统一清理入口，等价于 CacheLimits.Large。</summary>
    private static void TrimCache<TKey, TValue>(Dictionary<TKey, TValue> dict)
        => CacheLimits.Trim(dict, CacheLimits.Large);

    // ═══════════════════════════════════════════════════════════════════════
    // A3（2026-10-07）：补丁数量的唯一阈值来源。
    //   此前人类日志用 14、[REG] 断言用 20 —— 实测数量∈[14,19] 时会
    //   "日志说✓、[REG] 报 FAIL"自相矛盾（自检脚本和人的眼睛打架）。
    // ═══════════════════════════════════════════════════════════════════════
    // 阶段1（2026-10-11）：新增 2 个 Patch（GroupDragStartPatch / GroupDragEndPatch）。
    //   实况：installed 16→18，patchedCount 39→41。
    //   阈值从 14/10 上调到 17/15 —— 仍留 1/3 的余量（防止可选 Patch 被 cfg 关掉
    //   导致的正常波动误报），但不再低到"少挂半个模块都不报警"。
    //   ★ 语义不变：低于此值说明"游戏更新后有方法没匹配上"，需要人工排查。
    /// <summary>ReportPatchSummary 判定"挂够了"的下限（人类日志 + [REG] 断言共用）。</summary>
    private const int MinExpectedPatchCount = 17;
    /// <summary>InstallPatches 判定"装上了"的下限。</summary>
    private const int MinInstalledPatchCount = 15;

    // ═══════════════════════════════════════════════════════════════════════
    // 场景代际号（2026-09-29 加入）。
    // 用途：字符串键里嵌一个代际号，避免场景重载后 IL2CPP 复用同一指针，
    // 导致基于裸指针构造的 metaKey 命中上一场景的旧缓存。
    // ═══════════════════════════════════════════════════════════════════════
    private static int _sceneGeneration = 0;
    internal static int SceneGeneration => _sceneGeneration;
    internal static void BumpSceneGeneration() { _sceneGeneration++; }

    private static void RememberPeakCache(GameItem item, int peakW, int peakH)
    {
        try
        {
            IntPtr pk = ((Il2CppObjectBase)item).Pointer;
            if (pk == IntPtr.Zero) return;
            _peakCache.TryGetValue(pk, out var old);
            _peakCache[pk] = (
                PeakW: (peakW > 0) ? peakW : old.PeakW,
                PeakH: (peakH > 0) ? peakH : old.PeakH
            );
            CacheLimits.Trim(_peakCache, CacheLimits.Small);   // A1（原裸写 >128）
        }
        // 指针读取失败：本次不更新缓存即可（纯兜底缓存），仅首次留痕。
        catch (System.Exception ex) { CatchLogOnce.Warn("DiyUpgradeMod.RememberPeakCache", ex); }
    }
    /// <summary>
    /// 是否输出诊断日志。由 cfg [DIYUpgrade] DiagLog 控制。
    /// ★ 必须是 public：CI 所在的 CustomInfrastructure 是独立程序集，
    ///   internal 跨不过去。TD-05d 用它同步 ContainerSuiteLog.DiagEnabled。
    /// </summary>
    public static bool IsDiagEnabled
    {
        // 读配置失败按「关闭」处理：诊断开关的失败只影响日志量，无害。
        get { try { return _diagLog != null && _diagLog.Value; } catch { return false; } }
    }
    internal static void Diag(string msg)
    {
        if (!IsDiagEnabled) return;
        // 日志输出失败无害（例如控制台已不可用），吞掉即可。
        try { MelonLogger.Msg(msg); } catch { }
    }
}