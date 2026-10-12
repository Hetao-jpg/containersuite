// S2 阶段创建。存档槽隔离辅助，从 DIYUpgrade 逐字搬。
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
    }
}