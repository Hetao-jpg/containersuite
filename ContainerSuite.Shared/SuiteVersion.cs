namespace ContainerSuite.Shared;

/// <summary>
/// S5（2026-10-02）统一版本号。
///
/// 【为什么要有这个文件】
/// 三个组件原本各自维护一个版本字符串（CI 1.2.0 / SBE 1.2.0 / DIY 1.4.0），
/// 合并成 ContainerSuite 之后对外只能有一个版本号，否则用户看到三个数字不知道该信哪个。
///
/// 【约定】
///   · <see cref="Suite"/> 是唯一的对外版本号：MelonInfo、所有 AssemblyInfo、
///     README 标题都用它，改版本只改这一处常量（AssemblyInfo 里仍写字面量，
///     见各文件顶部注释，改的时候记得同步）。
///   · 三个 <c>Component*</c> 常量现在**只在 DIAG 构建的启动横幅里显示**——
///     Release 产物只有 <see cref="Suite"/> 一个版本号对用户可见（见 <see cref="Banner"/>）。
///     它们保留各自的演进历史，用于 DIAG 排障时定位「哪个组件改过什么」。
/// </summary>
public static class SuiteVersion
{
    /// <summary>对外显示的套件版本。</summary>
    public const string Suite = "1.8.0";                  // ← 1.8.0 改（cfg 体检 CfgDoctor + 8B 否决清单 + 文档键名守卫）

    /// <summary>
    /// CFG 迁移的**最低支持来源版本**（迁移终止线）：只把「≥ 本版本」的配置自动迁移到当前格式，
    /// 早于它的旧格式（4 DLL / 三段 cfg）**不再迁移**（只丢历史改过的值，不丢存档、不影响功能）。
    /// 为什么是 1.7.4（键名重排版本）见 docs/cfg-migration.md。
    /// </summary>
    public const string MinSupportedFrom = "1.7.4";

    /// <summary>共享库组件版本。</summary>
    public const string ComponentShared = "1.1.0";        // ← 1.1.0 改（新增 CfgDoctor：cfg 体检/宽容采纳/SaveIfNeeded）

    /// <summary>自定义基础设施大小组件版本。</summary>
    public const string ComponentInfrastructure = "1.2.3";  // ← 1.2.3 改（Init 接线 CfgDoctor + 无条件 SaveToFile 收敛）

    /// <summary>自定义仓库（扩容 / 分页）组件版本。</summary>
    public const string ComponentSaveBag = "1.3.3";         // ← 1.3.3 改（Init 接线 CfgDoctor + 无条件 SaveToFile 收敛）

    /// <summary>自定义容器升级组件版本。</summary>
    /// <remarks>
    /// 1.6.0 = S8k 加避让热开关（SkipXiaoBay / SkipWageBox）
    /// 1.7.0 = S8l 加虚空珠移交门
    /// 1.7.2 = S8n 封 XIAOWO 拖拽手柄 + 上限对齐
    /// 1.7.3 = S8p TagUtil 推广到 DIY + IsDiagEnabled 提升 public
    /// 1.8.0 = cfg 键名重排（域号前缀）+ MigrateRenamedKeys 迁移
    /// 1.8.1 = 每槽独立开关合并到容器级 Enable（删除 8 个键）
    /// 1.8.2 = 白名单新增 bottle_printer（水瓶打印机）+ EnsureDefaultIds 一次性补齐
    ///          + CageIds 默认值去重；*Ids 默认值收敛到 DefaultIdLists 单一事实源
    /// 1.8.3 = 修拖拽缩放网格残留/乱格（对照小窝同款 SetShape+ForceRecompute）+ 清 XIAOWO 残留手柄 + 补 Top 手柄
    /// 1.9.0 = cfg 体检接线（宽容采纳 + 只在真有变化时落盘）+ 8B_DefaultIdsVeto 否决清单
    /// </remarks>
    public const string ComponentDiyUpgrade = "1.9.0";      // ← 1.8.3 改

    /// <summary>启动横幅。Release 只显示套件版本；DIAG 构建额外显示组件版本（排障用）。</summary>
    public static string Banner
    {
        get
        {
#if DIAG
            return $"ContainerSuite v{Suite}" +
                   $"（基础设施 v{ComponentInfrastructure}" +
                   $" / 扩容仓库 v{ComponentSaveBag}" +
                   $" / 容器升级 v{ComponentDiyUpgrade}" +
                   $" / 共享库 v{ComponentShared}）";
#else
            return $"ContainerSuite v{Suite}";
#endif
        }
    }
}
