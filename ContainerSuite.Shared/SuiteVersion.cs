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
///   · 三个 <c>Component*</c> 常量只用于启动日志里"一行显示组件版本"，
///     不对外冒充套件版本，保留各自的演进历史。
/// </summary>
public static class SuiteVersion
{
    /// <summary>对外显示的套件版本。</summary>
    public const string Suite = "1.7.8";                  // ← 1.7.7 改（修拖拽缩放网格残留/乱格 + 清 XIAOWO 残留手柄 + 补顶边 Top 手柄）

    /// <summary>
    /// CFG 迁移的**最低支持来源版本**（迁移终止线）。
    /// <para>
    /// 约定：只保证把「≥ 本版本」的配置自动迁移到当前格式；早于本版本的旧格式
    /// （4 个 DLL / 三段 cfg：<c>[自定义容器升级]</c> / <c>[DIYUpgrade]</c> /
    /// <c>[SaveBagExpand]</c>）**不再迁移** —— 老用户跨过这条线直接按新键名默认值启动
    /// （只丢"历史改过的值"，不丢存档、不影响功能，可接受）。
    /// </para>
    /// <para>
    /// 为什么是 1.7.4：该版本做了 cfg 键名重排（域号前缀），之后所有面板可见键都是新键名；
    /// 早于它的「三段并一段」合并（1.7.3）逻辑已整段删除，见
    /// <c>DiyUpgrade.CfgMigration.cs</c> 文件头的「迁移终止线」说明。
    /// </para>
    /// </summary>
    public const string MinSupportedFrom = "1.7.4";

    /// <summary>共享库组件版本。</summary>
    public const string ComponentShared = "1.0.0";        // ← 不动（本轮 Shared 未改）

    /// <summary>自定义基础设施大小组件版本。</summary>
    public const string ComponentInfrastructure = "1.2.2";  // ← 1.2.1 改（键名加 6A/6B/6C/z1 前缀）

    /// <summary>自定义仓库（扩容 / 分页）组件版本。</summary>
    public const string ComponentSaveBag = "1.3.2";         // ← 1.3.1 改（键名加域号前缀）

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
    /// </remarks>
    public const string ComponentDiyUpgrade = "1.8.3";      // ← 1.8.2 改

    /// <summary>启动横幅：套件版本 + 一行组件版本。</summary>
    public static string Banner =>
        $"ContainerSuite v{Suite}" +
        $"（基础设施 v{ComponentInfrastructure}" +
        $" / 扩容仓库 v{ComponentSaveBag}" +
        $" / 容器升级 v{ComponentDiyUpgrade}" +
        $" / 共享库 v{ComponentShared}）";
}
