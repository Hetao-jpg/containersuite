using System;
using System.Collections.Generic;
using System.IO;
using ContainerSuite.Shared;   // S2 新增（虽然暂未直接调用 Shared，但为后续准备）
using MelonLoader;
using MelonLoader.Utils;

namespace CustomInfrastructure;

internal static class CustomInfrastructureConfig
{
    internal const int MinSize = 4;
    internal const int MaxSize = 30;

    internal static MelonPreferences_Category Category;

    internal static MelonPreferences_Entry<bool> TrashcanEnable;
    internal static MelonPreferences_Entry<int> TrashcanWidth;
    internal static MelonPreferences_Entry<int> TrashcanHeight;

    internal static MelonPreferences_Entry<bool> DossierEnable;
    internal static MelonPreferences_Entry<int> DossierWidth;
    internal static MelonPreferences_Entry<int> DossierHeight;

    internal static MelonPreferences_Entry<bool> CassettePlayerEnable;
    internal static MelonPreferences_Entry<int> CassettePlayerWidth;
    internal static MelonPreferences_Entry<int> CassettePlayerHeight;
    internal static MelonPreferences_Entry<bool> DiagLog;

    /// <summary>
    /// 【2026-10-10 ★★★ 读档闪退定位】CI 读档链「真实容器」JIT 预热开关。
    /// <para>
    /// **默认 false = 关闭**。理由：这是五次读档崩溃里唯一被推迟到"读档后"执行的
    /// 预热（SBE/DIY 的预热都在启动期主菜单跑完），且崩溃点稳定落在它 1.86 秒后
    /// （五次实测 1.815~1.911s）。它与其它预热的**设计哲学相反** ——
    /// SBE/DIY 一律用 null 安全入口（不碰游戏状态），而它用**真实容器真执行**
    /// 一遍容器扩展（会改动网格形状并重建窗口 UI，详见 ContainerExpander 内的实现），
    /// 这些都是游戏原生操作。在"门刚开、场景仍在重建"时执行风险极高
    /// （CLR fatal，托管 catch 抓不到）。
    /// </para>
    /// <para>
    /// 它的初衷（2026-10-09）是让读档链热路径在浅栈完成 JIT；但实测**引入后读档反而
    /// 每次必崩**，而 10-02 版本（无此预热）无此问题。故默认关闭，回归旧行为。
    /// 排查时置 true 可复现（开=崩、关=不崩 ⇒ 坐实崩点）。
    /// </para>
    /// </summary>
    internal static MelonPreferences_Entry<bool> CIReadLoadWarmup;
    // 【2026-10-10 读档闪退 A/B】z3：诊断开关（默认 false，见 CreateEntry 处注释）
    internal static MelonPreferences_Entry<bool> DisableLoadRestoreA;

    internal static event Action OnConfigChanged;

    internal static void Init()
    {
        // 2026-10-06 合并：三段统一到 CustomInfrastructure，必须复用同一个 category 实例。
        Category = ContainerSuite.Shared.MelonPrefsUtil.GetOrCreate(
            ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId, "自定义基础设施大小");

        TrashcanEnable = Category.CreateEntry(
            "6A_Trashcan_Enable", true,
            "[基建] 垃圾桶·启用",
            "是否扩容垃圾桶。false=保持原版尺寸。");

        TrashcanWidth = Category.CreateEntry(
            "6A_Trashcan_Width", 12,
            $"[基建] 垃圾桶·宽 ({MinSize}-{MaxSize})",
            $"垃圾桶网格的目标列数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        TrashcanHeight = Category.CreateEntry(
            "6A_Trashcan_Height", 12,
            $"[基建] 垃圾桶·高 ({MinSize}-{MaxSize})",
            $"垃圾桶网格的目标行数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        DossierEnable = Category.CreateEntry(
            "6B_Dossier_Enable", true,
            "[基建] 档案箱·启用",
            "是否扩容档案箱。false=保持原版尺寸。");

        DossierWidth = Category.CreateEntry(
            "6B_Dossier_Width", 12,
            $"[基建] 档案箱·宽 ({MinSize}-{MaxSize})",
            $"档案箱网格的目标列数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        DossierHeight = Category.CreateEntry(
            "6B_Dossier_Height", 12,
            $"[基建] 档案箱·高 ({MinSize}-{MaxSize})",
            $"档案箱网格的目标行数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        CassettePlayerEnable = Category.CreateEntry(
            "6C_CassettePlayer_Enable", true,
            "[基建] 磁带·启用",
            "是否扩容磁带播放器。false=保持原版尺寸。");

        CassettePlayerWidth = Category.CreateEntry(
            "6C_CassettePlayer_Width", 8,
            $"[基建] 磁带·宽 ({MinSize}-{MaxSize})",
            $"磁带播放器两个槽各自的目标列数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        CassettePlayerHeight = Category.CreateEntry(
            "6C_CassettePlayer_Height", 8,
            $"[基建] 磁带·高 ({MinSize}-{MaxSize})",
            $"磁带播放器两个槽各自的目标行数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");
        // ═══════════════════════════════════════════════════════════════
        // 2026-10-07：诊断日志开关合并为**一个总开关**。
        //
        // 合并前三个模块各有一个 bool（CI DiagLog / SBE SaveBag_DiagLog /
        //   DIY DIY_DiagLog），面板里三项同名「输出诊断日志」，玩家分不清该开哪个。
        // 合并后只保留本条 —— 它同时管 CI / SBE / DIY 三个模块 + Shared 层
        //   （RestoreArbiter 等）的诊断输出。
        //
        // ★ 实现方式是"别名"而不是"各建一条再 OR"：
        //   SBE 的 SaveBagExpandConfig.DiagLog 与 DIY 的 _diagLog 现在**直接指向
        //   本条 entry 对象**（同一个 MelonPreferences_Entry 实例）。这样：
        //     · 三边所有读取点（`xxx.DiagLog.Value`）一行都不用改，回归面为零；
        //     · 面板里只会出现一项，不可能再出现"开了 A 没开 B"的困惑。
        //   ★ 时序前提：本条的创建必须早于 SBE / DIY 的 Init —— Mod.OnInitializeMelon
        //     里就是 CI.Init → SBE.Init → DIY.Init 的顺序，成立。
        // ═══════════════════════════════════════════════════════════════
        DiagLog = Category.CreateEntry(
    "z1_DiagLog", false,
    "[诊断] 日志(排查时开)",
    "true = 输出【全部模块】的诊断日志（基础设施容器扩展 / 主仓库分页 / 容器升级 / 读档仲裁）。"
    + "一个开关管三家，面板改完立即生效，无需重启。排查问题时才开，正常玩保持 false。");

        // 【2026-10-10 ★★★ 读档闪退定位】CI 读档链「真实容器」JIT 预热。
        //   默认 false = 关（见字段注释的完整推理）。
        CIReadLoadWarmup = Category.CreateEntry(
    "z2_CIReadLoadWarmup", false,
    "[诊断] CI 读档链真实容器预热(⚠仅排查)",
    "true = 读档后对 3 个真实容器重跑一遍扩展（修改网格形状 + 重建窗口 UI），"
    + "以此把读档链热路径的 JIT 提前到浅栈。⚠ 默认 false：实测开启后读档必崩 "
    + "(0x80131506)，仅在排查读档闪退时用于复现，正常玩保持 false。");

        // 【2026-10-10 ★ 读档闪退 A/B 测试】z3：读档恢复总闸（默认 false = 正常恢复）。
        //   true = DelayedRestore 只做 JIT 预热、完全跳过恢复队列（不建手柄 UI、不改网格）。
        //   用途：判断崩溃诱因是否在"恢复链的实际执行"（Attach/网格/UI）——
        //   z3=true 后不崩 ⇒ 诱因在恢复执行；仍崩 ⇒ 诱因在恢复之外（预热本身/其它 mod/游戏）。
        DisableLoadRestoreA = Category.CreateEntry(
    "z3_DisableLoadRestore", false,
    "[诊断] 关读档恢复(⚠仅排查)",
    "true = 读档后不做容器尺寸恢复（只保留 JIT 预热）。仅用于读档闪退 A/B 定位，正常玩保持 false。");

    Subscribe(TrashcanEnable); Subscribe(TrashcanWidth); Subscribe(TrashcanHeight);
        Subscribe(DossierEnable); Subscribe(DossierWidth); Subscribe(DossierHeight);
        Subscribe(CassettePlayerEnable); Subscribe(CassettePlayerWidth); Subscribe(CassettePlayerHeight);

        Category.SaveToFile(true);
    }

    private static void Subscribe<T>(MelonPreferences_Entry<T> entry)
    {
        if (entry == null) return;
        entry.OnEntryValueChanged.Subscribe((_, __) => NotifyChanged());
    }

    private static void NotifyChanged()
    {
        try { OnConfigChanged?.Invoke(); }
        catch (Exception ex) { Mod.Log?.Warning("配置变化回调异常: " + ex.Message); }
    }

    /// <summary>返回当前启用的容器 (id, 目标宽, 目标高) 列表。</summary>
    internal static List<(string Id, int Width, int Height)> GetActiveContainers()
    {
        var list = new List<(string, int, int)>();
        if (TrashcanEnable?.Value == true)
            list.Add(("trashcan", ClampSize(TrashcanWidth.Value), ClampSize(TrashcanHeight.Value)));
        if (DossierEnable?.Value == true)
            list.Add(("dossier", ClampSize(DossierWidth.Value), ClampSize(DossierHeight.Value)));
        if (CassettePlayerEnable?.Value == true)
            list.Add(("cassette_player", ClampSize(CassettePlayerWidth.Value), ClampSize(CassettePlayerHeight.Value)));
        return list;
    }

    internal static int ClampSize(int value)
    {
        if (value < MinSize) return MinSize;
        if (value > MaxSize) return MaxSize;
        return value;
    }
}