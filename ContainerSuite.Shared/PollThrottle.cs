// A1（2026-10-12）创建。跨帧节流与"距上次多久"的单一实现（Environment.TickCount64 单调钟）。
// 为什么不用 DateTime.UtcNow 差值（时钟回拨 ⇒ 判据恒真 ⇒ 闸门卡死/静默失效）：见 docs/monotonic-clock.md

using System;

namespace ContainerSuite.Shared;

/// <summary>
/// 单调钟：跨帧节流 / 缓存 TTL 统一用它，不要再用 <c>DateTime.UtcNow</c> 差值。
/// </summary>
public struct PollThrottle
{
    private long _nextMs;

    /// <summary>
    /// 当前单调毫秒数（自开机起）。用于缓存 TTL 比较：
    /// <c>PollThrottle.ElapsedMs(stamp) &lt; 200</c>。
    /// </summary>
    public static long NowMs => Environment.TickCount64;

    /// <summary>距 <paramref name="sinceMs"/> 已过多少毫秒（单调钟 ⇒ 永不为负）。</summary>
    public static long ElapsedMs(long sinceMs) => Environment.TickCount64 - sinceMs;

    /// <summary>
    /// 到期放行：返回 true 表示"距上次放行已过 <paramref name="intervalMillis"/>"，
    /// 并把下次到期时间顺延。等价于旧写法
    /// <c>if ((now - last) &lt; interval) return; last = now;</c> 但不会因时钟回拨卡死。
    /// </summary>
    /// <param name="intervalMillis">间隔（毫秒）。≤ 0 视为每次都放行。</param>
    public bool Due(int intervalMillis)
    {
        if (intervalMillis <= 0) return true;
        long now = Environment.TickCount64;
        if (now < _nextMs) return false;
        _nextMs = now + intervalMillis;
        return true;
    }

    /// <summary>复位（下次调用立即可放行）。换档 / 场景切换等需要"立刻可触发"时用。</summary>
    public void Reset() => _nextMs = 0L;
}
