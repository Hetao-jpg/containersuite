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
    private static DateTime _cachedAt = DateTime.MinValue;
    private const double CacheSeconds = 1.5;

    /// <summary>获取当前存档槽号。拿不到时返回 -1。缓存 1.5 秒。</summary>
    public static int GetCurrent()
    {
        DateTime utcNow = DateTime.UtcNow;
        if ((utcNow - _cachedAt).TotalSeconds < CacheSeconds && _cachedSlotId >= 0)
            return _cachedSlotId;
        _cachedAt = utcNow;
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
        _cachedAt = DateTime.MinValue;
    }
}