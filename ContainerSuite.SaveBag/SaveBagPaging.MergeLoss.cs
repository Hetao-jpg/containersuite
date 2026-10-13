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
    // ────────── B1：合并丢失检测 + 旁路清单 + 屏幕提示 ──────────
    //
    // README 已知限制 #7：分页 + 扩容组合的容量上限 = 单网格容量。
    // 存档时所有页必须合并回同一个网格，超出容量的物品 UncheckedAccept 直接拒收
    // → 存档里凭空消失，而且**一个字都不提**（静默丢档，最坏的一类 bug）。
    //
    // 这里做三段式：
    //   1) 合并前记账（按指针去重，带页号 / uid / identifier / 坐标）
    //   2) 合并后按指针集合做差，缺谁一目了然
    //   3) Warning 日志 + UserData\SaveBagPaging.mergeloss.txt 旁路清单 + 屏幕红字
    //
    // ★ 只"如实报告"，不自动往网格里塞回去：拒收的原因就是容量不够，
    //   再 Place 一次还是拒；强行改网格尺寸会改掉存档结构，风险比丢档更大。
    //   旁路清单的价值 = 玩家至少知道丢了什么、丢了多少（可按 uid/id 手动补）。

    public static bool HasMergeLoss { get; private set; }
    public static int MergeLossCount { get; private set; }
    public static float MergeLossAt { get; private set; }

    private struct StoredItem
    {
        public IntPtr Ptr;
        public int Page;
        public int Uid;
        public string Id;
        public int X;
        public int Y;
    }

    private static string MergeLossFilePath =>
        System.IO.Path.Combine(MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "SaveBagPaging.mergeloss.txt");

    /// <summary>合并前的记账：所有页里不重复的物品（按指针去重）。</summary>
    private static List<StoredItem> CollectStored()
    {
        var list = new List<StoredItem>();
        var seen = new HashSet<IntPtr>();
        for (int p = 0; p < _storages.Count; p++)
        {
            var slots = _storages[p];
            if (slots == null) continue;
            for (int i = 0; i < slots.Count; i++)
            {
                var s = slots[i];
                if (s == null || s.Item == null) continue;

                IntPtr ptr = IntPtr.Zero;
                try { ptr = s.Item.Pointer; } catch (Exception ex) { CatchLogOnce.Warn("paging.CollectStored.ptr", ex); }
                if (ptr == IntPtr.Zero || !seen.Add(ptr)) continue;

                var rec = new StoredItem { Ptr = ptr, Page = p, X = s.X, Y = s.Y };
                // ★ 与 Snapshot() 的口径必须一致：Snapshot 对 uniqueId 取值抛异常的
                //   物品是 continue 跳过的，这里若照样记账，合并后那边数不到它，
                //   就会被误判成"丢失"（假阳性比漏报更糟，会让人不信这条告警）。
                try { rec.Uid = s.Item.uniqueId; } catch (Exception ex) { CatchLogOnce.Warn("paging.CollectStored.uid", ex); continue; }
                try { rec.Id = s.Item.identifier; } catch (Exception ex) { CatchLogOnce.Warn("paging.CollectStored.id", ex); rec.Id = null; }
                list.Add(rec);
            }
        }
        return list;
    }

    private static void DetectMergeLoss(List<StoredItem> before)
    {
        try
        {
            if (_mainGrid == null || before == null || before.Count == 0) return;

            var onGrid = new HashSet<IntPtr>();
            foreach (var slot in Snapshot(_mainGrid))
            {
                try { if (slot != null && slot.Item != null) onGrid.Add(slot.Item.Pointer); } catch (Exception ex) { CatchLogOnce.Warn("paging.DetectMergeLoss.ptr", ex); }
            }

            int gw = 0, gh = 0;
            try
            {
                var shape = _mainGrid.inventoryShape;
                if (shape != null) { gw = shape.width; gh = shape.height; }
            }
            catch (Exception ex) { CatchLogOnce.Warn("paging.DetectMergeLoss.shape", ex); }

            var missing = new List<StoredItem>();
            for (int i = 0; i < before.Count; i++)
                if (!onGrid.Contains(before[i].Ptr)) missing.Add(before[i]);

            if (missing.Count == 0)
            {
                // 这次没丢：上一次留下的屏幕警告可以撤掉
                if (HasMergeLoss) { HasMergeLoss = false; MergeLossCount = 0; }
                return;
            }

            HasMergeLoss = true;
            MergeLossCount = missing.Count;
            MergeLossAt = Time.realtimeSinceStartup;

            SaveBagExpandCore.Log.Warning(
                $"[SaveBagExpand] 存档合并丢失：{missing.Count}/{before.Count} 件没进网格" +
                $"（网格 {gw}x{gh}={gw * gh}，分页总件数超出单网格容量）" +
                $"，清单已写 {MergeLossFilePath}");

            WriteMergeLossFile(before.Count, missing, gw, gh);
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Warning("[SaveBagExpand] 合并丢失检测失败: " + ex.Message);
        }
    }

    private static void WriteMergeLossFile(int stored, List<StoredItem> missing, int gw, int gh)
    {
        try
        {
            var lines = new List<string>
            {
                "# SaveBagPaging 合并丢失清单（B1）",
                "# 说明：存档时所有页要合并进同一个网格；超出单网格容量的物品被拒收，",
                "#       没有写进存档文件。本清单只记录丢了什么（物品本体未被抢救）。",
                "# 数据行格式（分号分隔）: 页号;uid;identifier;x;y",
                "# time=" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                $"# grid={gw}x{gh}({gw * gh}) stored={stored} missing={missing.Count}"
            };
            for (int i = 0; i < missing.Count; i++)
            {
                var m = missing[i];
                string id = string.IsNullOrEmpty(m.Id) ? "-" : m.Id;
                lines.Add($"{m.Page};{m.Uid};{id};{m.X};{m.Y}");
            }

            string dir = System.IO.Path.GetDirectoryName(MergeLossFilePath);
            if (!string.IsNullOrEmpty(dir)) System.IO.Directory.CreateDirectory(dir);
            string tmp = MergeLossFilePath + ".tmp";
            System.IO.File.WriteAllLines(tmp, lines, new UTF8Encoding(false));
            System.IO.File.Move(tmp, MergeLossFilePath, true);   // 同卷原子替换
        }
        catch (Exception ex)
        {
            SaveBagExpandCore.Log.Warning("[SaveBagExpand] 合并丢失清单写入失败: " + ex.Message);
        }
    }

}
