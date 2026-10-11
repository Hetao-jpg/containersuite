using MelonLoader;

namespace ContainerSuite.Shared;

/// <summary>
/// 2026-10-06 单 DLL 合并（B2）配套工具。
///
/// 合并前三个模块各占一个 MelonPreferences 段：
///   [CustomInfrastructure] / [SaveBagExpand] / [DIYUpgrade]
/// 合并后只有一个 DLL = 一个文件名 = 一个 ModManager 归属单位
/// （规则见 ModManager\ManagerState.CategoryBelongsToMod：
///   Normalize(CategoryId) == Normalize(DLL 文件名)，只看文件名，不看 MelonInfo 名），
/// 所以三段必须统一到 CustomInfrastructure，否则 SaveBagExpand(20 项) +
/// DIYUpgrade(11 项) 共 31 项配置会从 ModManager 面板里消失（只能手改 cfg）。
///
/// 统一段名之后，CI / SBE / DIY 三处 Init 都会请求同一个段 id。
/// 必须返回**同一个 category 实例**——否则同一个段被注册两次，
/// 保存时两条路径互相覆盖、Entries 分家。
/// </summary>
public static class MelonPrefsUtil
{
    /// <summary>合并后唯一的 MelonPreferences 段名，也是 DLL 文件名 / MelonInfo 名。</summary>
    public const string SuiteCategoryId = "CustomInfrastructure";

    /// <summary>
    /// 取（或创建）段。已有同名段时直接返回它，忽略 displayName ——
    /// 首个创建者的 displayName 生效（CI 先 Init，所以显示名恒为「自定义基础设施大小」）。
    /// </summary>
    public static MelonPreferences_Category GetOrCreate(string identifier, string displayName = null)
    {
        // 已存在同名段：直接复用（同段注册两次会导致 Entries 分家、保存互相覆盖）。
        var existing = TryFind(identifier);
        if (existing != null) return existing;

        try
        {
            return displayName == null
                ? MelonPreferences.CreateCategory(identifier)
                : MelonPreferences.CreateCategory(identifier, displayName);
        }
        catch
        {
            // 兜底：并发/重复创建抛异常时，再找一次。
            var again = TryFind(identifier);
            if (again != null) return again;
            throw;
        }
    }

    /// <summary>在已注册的段里按 identifier 精确查找；读不到（含异常）返回 null。</summary>
    private static MelonPreferences_Category TryFind(string identifier)
    {
        try
        {
            var list = MelonPreferences.Categories;
            if (list != null)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    var c = list[i];
                    if (c != null && string.Equals(c.Identifier, identifier, System.StringComparison.Ordinal))
                        return c;
                }
            }
        }
        catch { /* 读不到就当作未找到，走 CreateCategory */ }
        return null;
    }
}
