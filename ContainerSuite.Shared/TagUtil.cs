// 共享的 tag 读写工具。
//
// 背景：CI 和 DIY 各自有一份行为等价的 SetItemTag / SafeTagInt 实现。
// 本次先抽取到 Shared，先让 CI 改用；DIY 保持不动，待后续单独迁移。
//
// 行为选型说明：
//   · SafeTagInt 采用 DIY 版本（先试 item.state.GetTag，再试 GetTagReadonly）。
//     比 CI 原来的单层 GetTagReadonly 多一层兜底，功能等价 —— GetTagReadonly
//     内部也是查 state，但加载早期 state 尚未初始化时多一层判断可避免静默失效。
//   · SetItemTag 采用两家共有的写法（值未变短路 + EnableTag + ModifyTag），
//     走官方 API 确保落盘。
//   · 异常处理走 CatchLogOnce（只打一次）—— 这是高频兜底路径，
//     失败时打一次足够，不必每次刷屏。

using System;
using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;

namespace ContainerSuite.Shared;

/// <summary>
/// GameItem tag 读写工具。
/// </summary>
public static class TagUtil
{
    // 按 int 值缓存 IL2CPP 委托，避免每次写 tag 都新建闭包 + ConvertDelegate。
    private static readonly Dictionary<int, Il2CppSystem.Action<Il2Cpp.TagState>> _tagSetterCache
        = new Dictionary<int, Il2CppSystem.Action<Il2Cpp.TagState>>();

    private static Il2CppSystem.Action<Il2Cpp.TagState> GetTagSetter(int value)
    {
        if (_tagSetterCache.TryGetValue(value, out var cached) && cached != null) return cached;

        System.Action<Il2Cpp.TagState> csAction = delegate (Il2Cpp.TagState st) { st.SetInt(value); };
        Il2CppSystem.Action<Il2Cpp.TagState> il2cppAction =
            DelegateSupport.ConvertDelegate<Il2CppSystem.Action<Il2Cpp.TagState>>(csAction);

        CacheLimits.Trim(_tagSetterCache, CacheLimits.Large);   // A1（原裸写 >512）
        _tagSetterCache[value] = il2cppAction;
        return il2cppAction;
    }

    /// <summary>
    /// 写 GameItem tag（int 值）。值未变则短路。
    /// 走官方 API（EnableTag + ModifyTag），确保落盘。
    /// </summary>
    public static void SetItemTag(GameItem item, string tag, int value)
    {
        if (item == null) return;
        try
        {
            if (item.IsTag(tag) && SafeTagInt(item, tag, int.MinValue) == value) return;

            item.EnableTag(tag, skipValidate: true);
            item.ModifyTag(tag, GetTagSetter(value), skipValidate: true);
        }
        catch (Exception ex)
        {
            CatchLogOnce.Warn("TagUtil.SetItemTag", ex);
        }
    }

    /// <summary>
    /// 读 GameItem tag（int 值）。读不到 / 异常 / item 为 null 都返回 fallback。
    /// 先试 item.state.GetTag，再试 item.GetTagReadonly。
    /// </summary>
    public static int SafeTagInt(GameItem item, string tag, int fallback)
    {
        if (item == null) return fallback;

        try
        {
            if (item.state != null)
            {
                TagState t = item.state.GetTag(tag);
                if (t != null) return t.GetInt();
            }
        }
        catch { }

        try
        {
            TagState ro = item.GetTagReadonly(tag);
            if (ro != null) return ro.GetInt();
        }
        catch { }

        return fallback;
    }

    /// <summary>简化版：fallback = 0。</summary>
    public static int SafeTagInt(GameItem item, string tag) => SafeTagInt(item, tag, 0);
}