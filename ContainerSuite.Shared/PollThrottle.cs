// A1（2026-10-12）创建。跨帧节流与"距上次多久"的单一实现。
//
// ★ 思路来源：小窝（XIAOWOTradePerks）的 RuntimePollGate —— 用**单调时钟**判"到期没"，
//   而不是每次现算 DateTime 差值。**只借思路，不照抄命名与结构**（避免与对方同名、
//   也避免把它的字段语义搬进来）。
//
// 为什么不用 DateTime.UtcNow 差值（本文件存在的唯一理由）：
//   ① 系统时钟**回拨**（手动改时间 / 大跨度 NTP 校正 / 虚拟机休眠恢复）会让
//      (now - last) 变成负数，于是所有 `(now - last).TotalSeconds < N` 判据**恒真**
//      ⇒ 闸门卡死：日志静默、同一帧判据被缓存成"不能升"、读档恢复被跳过。
//      这类故障没有任何报错，正是本项目最忌讳的"静默失效"。
//   ② 每个调用点都要手写一遍差值表达式，方向写反（`<` / `>`）不会报错，只会在
//      边缘情况下表现异常。
//
// Environment.TickCount64 = 自开机起的毫秒数：单调递增、与帧率无关、与系统时间无关。
//   · 64 位，不会回绕（32 位 TickCount 会在 49.7 天回绕，故不用）。
//   · 进程内状态，无需清理；进程重启后归零（本结构体只在进程内使用，无影响）。
//   · 语义边界：它衡量的是"真实经过时间"，不受 timeScale 影响 —— 正是墙钟节流想要的。

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
