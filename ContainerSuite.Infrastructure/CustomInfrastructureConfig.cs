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

    internal static event Action OnConfigChanged;

    internal static void Init()
    {
        // 2026-10-06 合并：三段统一到 CustomInfrastructure，必须复用同一个 category 实例。
        Category = ContainerSuite.Shared.MelonPrefsUtil.GetOrCreate(
            ContainerSuite.Shared.MelonPrefsUtil.SuiteCategoryId, "自定义基础设施大小");

        TrashcanEnable = Category.CreateEntry(
            CfgKeys.TrashcanEnable, true,
            "[基建] 垃圾桶·启用",
            "是否扩容垃圾桶。false=保持原版尺寸。");

        TrashcanWidth = Category.CreateEntry(
            CfgKeys.TrashcanWidth, 12,
            $"[基建] 垃圾桶·宽 ({MinSize}-{MaxSize})",
            $"垃圾桶网格的目标列数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        TrashcanHeight = Category.CreateEntry(
            CfgKeys.TrashcanHeight, 12,
            $"[基建] 垃圾桶·高 ({MinSize}-{MaxSize})",
            $"垃圾桶网格的目标行数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        DossierEnable = Category.CreateEntry(
            CfgKeys.DossierEnable, true,
            "[基建] 档案箱·启用",
            "是否扩容档案箱。false=保持原版尺寸。");

        DossierWidth = Category.CreateEntry(
            CfgKeys.DossierWidth, 12,
            $"[基建] 档案箱·宽 ({MinSize}-{MaxSize})",
            $"档案箱网格的目标列数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        DossierHeight = Category.CreateEntry(
            CfgKeys.DossierHeight, 12,
            $"[基建] 档案箱·高 ({MinSize}-{MaxSize})",
            $"档案箱网格的目标行数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        CassettePlayerEnable = Category.CreateEntry(
            CfgKeys.CassettePlayerEnable, true,
            "[基建] 磁带·启用",
            "是否扩容磁带播放器。false=保持原版尺寸。");

        CassettePlayerWidth = Category.CreateEntry(
            CfgKeys.CassettePlayerWidth, 8,
            $"[基建] 磁带·宽 ({MinSize}-{MaxSize})",
            $"磁带播放器两个槽各自的目标列数（{MinSize}-{MaxSize}，超出会被钳制）。只扩大不缩小。");

        CassettePlayerHeight = Category.CreateEntry(
            CfgKeys.CassettePlayerHeight, 8,
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
    CfgKeys.DiagLog, false,
    "[诊断] 日志(排查时开)",
    "true = 输出【全部模块】的诊断日志（基础设施容器扩展 / 主仓库分页 / 容器升级 / 读档仲裁）。"
    + "一个开关管三家，面板改完立即生效，无需重启。排查问题时才开，正常玩保持 false。");

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