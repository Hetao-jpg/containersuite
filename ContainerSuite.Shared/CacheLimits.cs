// 缓存清理上限的单一来源。
//
// 背景：套件里 8 处"能重建"的缓存各自裸写 if (Count > N) Clear()，
//   N 出现 512 / 256 / 128 / 64 / 32 五种值 —— 语义模糊，新增缓存者
//   容易忘加清理（历史 S8u 就是后补的）。
//
// 分级原则：**每一级都 >= 它替换掉的原值**，保证没有任何缓存的容量被缩小
//   → 行为非回归（清空频率只会下降或不变，缓存里都是能重建的东西）。
//   Large  512  ← 原 512 / 512 / 512
//   Medium 256  ← 原 256 / 256
//   Small  128  ← 原 128 / 64 / 64 / 32（后三个是抬高）
//
// 用法：CacheLimits.Trim(_cache, CacheLimits.Large);
//   不再出现裸 if (Count > N) Clear()。

using System.Collections.Generic;

namespace ContainerSuite.Shared;

public static class CacheLimits
{
    /// <summary>大缓存：指针/重值类（meta / ggi / prepare / tag setter / diag 去重）。</summary>
    public const int Large = 512;

    /// <summary>中缓存：单场景内可能上千次命中的判据缓存。</summary>
    public const int Medium = 256;

    /// <summary>小缓存：高频、轻量、随时可重建。</summary>
    public const int Small = 128;

    /// <summary>
    /// 超限即整体清空。简单胜过 LRU —— 缓存里都是"能重建"的东西，
    /// 清空的代价只是下次访问多算一次。同时适用于 Dictionary 与 HashSet。
    /// </summary>
    public static void Trim<T>(ICollection<T> cache, int limit)
    {
        if (cache != null && cache.Count > limit) cache.Clear();
    }
}
