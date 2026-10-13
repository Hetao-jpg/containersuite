// 存档槽隔离辅助。
// 设计文档：ContainerSuite S2 §3.5
//
// 与 DIYUpgrade 原版的差异：
//   · 命名空间统一为 ContainerSuite.Shared
//   · 改成 public static class SaveSlotScope

using System;
using Il2Cpp;

namespace ContainerSuite.Shared;

/// <summary>
/// 存档槽隔离辅助：任何外部存储（PlayerPrefs / 文件）的 key 都应该
/// 带上当前存档槽号，避免多存档切换时数据串。
/// 
/// 用 PlayerStore.Instance.saveSlotId 作为槽标识。
/// </summary>
public static class SaveSlotScope
{
    private static int _cachedSlotId = -1;
    // A1（2026-10-12）：节流改单调钟（PollThrottle）。原为 DateTime 差值 ——
    //   系统时钟回拨时 (now - cachedAt) 变负 ⇒ "< 1.5 秒" 恒真 ⇒ 槽号缓存永久不刷新。
    private static PollThrottle _cacheGate;
    private const int CacheMillis = 1500;   // 原 CacheSeconds = 1.5

    /// <summary>获取当前存档槽号。拿不到时返回 -1。缓存 1.5 秒。</summary>
    public static int GetCurrent()
    {
        // ★ 判据顺序保持原语义：拿不到槽号（<0）时**不消耗闸门**，仍然每次调用都重试 ——
        //   这样 PlayerStore 一旦可用就能立刻取到，不必白等 1.5 秒。
        if (_cachedSlotId >= 0 && !_cacheGate.Due(CacheMillis))
            return _cachedSlotId;
        try
        {
            if (!PlayerStore.IsInstanceExist() || PlayerStore.Instance == null)
            {
                _cachedSlotId = -1;
                return -1;
            }
            int slot = PlayerStore.Instance.saveSlotId;
            _cachedSlotId = (slot >= 0) ? slot : -1;
        }
        catch { _cachedSlotId = -1; }
        return _cachedSlotId;
    }


    /// <summary>强制清缓存（读档/换档时调用）。</summary>
    public static void Invalidate()
    {
        _cachedSlotId = -1;
        _cacheGate.Reset();
        _cachedRunKey = null;
        _runKeyGate.Reset();
    }

    // ═══════════════════════════════════════════════════════════════════════
    // T4（2026-10-12，借鉴蛙哥 WageSaveStore 的「#runID=」做法 —— **只借思路**，
    //     命名/存储实现都不照抄；蛙哥是自建文件库，我们只是给现有旁路文件加一列）
    //
    // 为什么需要：saveSlotId 只是**存档槽号**。同一个槽开新游戏时槽号不变、但物品的
    //   uniqueId 会重新分配 ⇒ 上一局的页归属会被当成这一局的用，第二页物品可能整片
    //   叠进第一页。这与"跨档 uid 复用污染内存缓存"（A-2 修的那类）是**同一类**问题，
    //   只是这份数据在磁盘上，不会因为读档清缓存而消失。
    // ═══════════════════════════════════════════════════════════════════════

    private static string _cachedRunKey;
    private static PollThrottle _runKeyGate;
    private const int RunKeyCacheMillis = 1500;

    /// <summary>
    /// 当前这一局的标识（<c>PlayerStore.runID</c>），供外部存储做**按局隔离**。
    /// <para>
    /// ★ 拿不到时返回**空串**，调用方必须把空串当作"不做局隔离"（保持旧行为）——
    ///   绝不能用空串去匹配，那会把玩家已有的数据整片判成"外来局"。
    /// </para>
    /// <para>
    /// 字符集：runID 会被写进以 ';' 与 '#' 分隔的旁路文件行，故只保留
    /// [0-9A-Za-z_-]，其余替换为 '_'，并截断到 24 字符（够区分，也不会把行撑长）。
    /// </para>
    /// <para>缓存 1.5 秒；拿不到时不消耗闸门、每次重试（与 <see cref="GetCurrent"/> 同策略）。</para>
    /// </summary>
    public static string GetRunScopeKey()
    {
        if (!string.IsNullOrEmpty(_cachedRunKey) && !_runKeyGate.Due(RunKeyCacheMillis))
            return _cachedRunKey;
        string raw = null;
        try
        {
            if (PlayerStore.IsInstanceExist() && PlayerStore.Instance != null)
                raw = PlayerStore.Instance.runID;
        }
        catch { raw = null; }
        _cachedRunKey = SanitizeRunScopeKey(raw);
        return _cachedRunKey;
    }

    private static string SanitizeRunScopeKey(string raw)
    {
        if (string.IsNullOrEmpty(raw)) return "";
        var sb = new System.Text.StringBuilder(24);
        for (int i = 0; i < raw.Length && sb.Length < 24; i++)
        {
            char c = raw[i];
            bool ok = (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')
                      || (c >= 'a' && c <= 'z') || c == '_' || c == '-';
            sb.Append(ok ? c : '_');
        }
        return sb.ToString();
    }
}