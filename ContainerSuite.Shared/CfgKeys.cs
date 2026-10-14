// CFG 键名单一事实源：键名只在这里写一次，其余地方一律引用常量。
// 为什么集中 / 编号方案（由总到分）/ 改名须同步的三件事：见 docs/cfg-migration.md

namespace ContainerSuite.Shared
{
    /// <summary>
    /// 全部 cfg 键名常量。取值之外不要引入逻辑 —— 它是一张表，不是一层抽象。
    /// </summary>
    public static class CfgKeys
    {
        // ══════════════════════════════════════════════════════════════
        // 0_ 套件总开关
        // ══════════════════════════════════════════════════════════════
        /// <summary>套件总开关：false = 整个容器套件的所有功能都不生效。</summary>
        public const string EnableMod = "0_EnableMod";

        // ══════════════════════════════════════════════════════════════
        // 1_ 容器升级 · 全局（材料 → 操作 → 增量）
        // ══════════════════════════════════════════════════════════════
        /// <summary>升级材料：垃圾（优先级最低）。</summary>
        public const string MaterialJunk = "1A_MaterialJunk";
        /// <summary>升级材料：废金属（优先级高于垃圾）。</summary>
        public const string MaterialScrapMetal = "1B_MaterialScrapMetal";
        /// <summary>升级材料：金属锭（优先级最高）。</summary>
        public const string MaterialMetalIngot = "1C_MaterialMetalIngot";
        /// <summary>升级修饰键（面板归入热键块，因为键名仍以 Hotkey 结尾）。</summary>
        public const string UpgradeHotkey = "1D_UpgradeHotkey";
        /// <summary>允许拖窗口边缘改容器尺寸。</summary>
        public const string EnableDragResize = "1E_EnableDragResize";
        /// <summary>读档后恢复已升级容器的尺寸。</summary>
        public const string RestoreOnLoad = "1F_RestoreOnLoad";
        /// <summary>批量框选升级（按住升级键框选多堆材料）。</summary>
        public const string EnableGroupDrag = "1G_EnableGroupDrag";
        /// <summary>笼子每格血量加成。</summary>
        public const string CageHpPerCell = "1H_CageHpPerCell";
        /// <summary>原版容器每次升级增加的列数。</summary>
        public const string IncW = "1I_IncW";
        /// <summary>原版容器每次升级增加的行数。</summary>
        public const string IncH = "1I_IncH";
        /// <summary>Mod 容器每次升级增加的列数。</summary>
        public const string ModIncW = "1J_ModIncW";
        /// <summary>Mod 容器每次升级增加的行数。</summary>
        public const string ModIncH = "1J_ModIncH";

        // ══════════════════════════════════════════════════════════════
        // 2_ 仓库扩容 · 总开关与各仓库宽高
        // ══════════════════════════════════════════════════════════════
        /// <summary>仓库扩容总开关。</summary>
        public const string ExpandEnabled = "2_ExpandEnabled";
        public const string BackInvWidth = "2A_BackInvWidth";
        public const string BackInvHeight = "2A_BackInvHeight";
        public const string FrontInvWidth = "2B_FrontInvWidth";
        public const string FrontInvHeight = "2B_FrontInvHeight";
        public const string HiddenInvWidth = "2C_HiddenInvWidth";
        public const string HiddenInvHeight = "2C_HiddenInvHeight";
        public const string SaveBagWidth = "2D_SaveBagWidth";
        public const string SaveBagHeight = "2D_SaveBagHeight";
        public const string ShowcaseInvWidth = "2E_ShowcaseInvWidth";
        public const string ShowcaseInvHeight = "2E_ShowcaseInvHeight";
        public const string AfterhourWidth = "2F_AfterhourWidth";
        public const string AfterhourHeight = "2F_AfterhourHeight";
        /// <summary>「地面」窗口是否可拖动。</summary>
        public const string GroundDragEnabled = "2G_GroundDragEnabled";
        /// <summary>「地面」窗口拖动后的水平偏移（自动写入，玩家一般不必手改）。</summary>
        public const string GroundOffsetX = "2G_GroundOffsetX";
        /// <summary>「地面」窗口拖动后的垂直偏移（自动写入，玩家一般不必手改）。</summary>
        public const string GroundOffsetY = "2G_GroundOffsetY";

        // ══════════════════════════════════════════════════════════════
        // 3_ 主仓库分页
        // ══════════════════════════════════════════════════════════════
        /// <summary>分页总开关。</summary>
        public const string SaveBagPagingEnabled = "3_SaveBagPagingEnabled";
        /// <summary>页数。</summary>
        public const string SaveBagPageCount = "3A_SaveBagPageCount";
        /// <summary>翻页提示显示时长（秒）。</summary>
        public const string SaveBagPagingHintDuration = "3B_SaveBagPagingHintDuration";

        // ══════════════════════════════════════════════════════════════
        // 4_ 主仓库滚动
        // ══════════════════════════════════════════════════════════════
        /// <summary>滚轮翻 rows 总开关。</summary>
        public const string ScrollEnabled = "4_ScrollEnabled";
        /// <summary>滚轮向上一次滚动的行数。</summary>
        public const string ScrollUpRows = "4A_ScrollUpRows";
        /// <summary>可见行数。</summary>
        public const string ScrollVisibleRows = "4B_ScrollVisibleRows";

        // ══════════════════════════════════════════════════════════════
        // 5_ 基础设施容器
        // ══════════════════════════════════════════════════════════════
        public const string TrashcanEnable = "5A_Trashcan_Enable";
        public const string TrashcanWidth = "5A_Trashcan_Width";
        public const string TrashcanHeight = "5A_Trashcan_Height";
        public const string DossierEnable = "5B_Dossier_Enable";
        public const string DossierWidth = "5B_Dossier_Width";
        public const string DossierHeight = "5B_Dossier_Height";
        public const string CassettePlayerEnable = "5C_CassettePlayer_Enable";
        public const string CassettePlayerWidth = "5C_CassettePlayer_Width";
        public const string CassettePlayerHeight = "5C_CassettePlayer_Height";

        // ══════════════════════════════════════════════════════════════
        // 6_ 跨 mod 避让（冷门：只在装了对面 mod 且排查冲突时才动）
        // ══════════════════════════════════════════════════════════════
        /// <summary>避让 XIAOWO 精工扩舱（关 = 6 个 bay 的尺寸主权交还 XIAOWO）。</summary>
        public const string SkipXiaoBay = "6A_SkipXiaoBay";
        /// <summary>避让蛙哥妙妙箱（关 = 满级妙妙箱主权交还蛙哥）。</summary>
        public const string SkipWageBox = "6B_SkipWageBox";
        /// <summary>虚空珠移交门。⚠ 语义与其它两条相反：true = 蛙哥主权。</summary>
        public const string SkipVoidBead = "6C_SkipVoidBead";
        /// <summary>清扫 XIAOWO 残留拖拽手柄（默认关：旧崩溃未复验）。</summary>
        public const string SweepZombieHandles = "6D_SweepZombieHandles";

        // ══════════════════════════════════════════════════════════════
        // 8_ 容器升级 · 单件上限（比 7x 分类更细的一层）
        // ══════════════════════════════════════════════════════════════
        /// <summary>
        /// 每容器 id 独立尺寸上限。字符串项（面板不显示，只能手改 cfg）。
        /// 写法：<c>storage_bay=30x20, evidence_box=24x24</c>，分隔符认逗号/中文逗号/分号。
        /// <para>★ 只写想单独设的那几个；没写的用 <c>7x</c> 里它所属分类的上限。</para>
        /// </summary>
        public const string PerIdSizeLimits = "8A_PerIdSizeLimits";

        /// <summary>
        /// 「不要再自动补回来」的物品 id 清单。字符串项（面板不显示，只能手改 cfg）。
        /// <para>
        /// 背景：<c>EnsureDefaultIds</c> 每次启动都会把「代码默认清单里有、你的 *Ids 里没有」
        /// 的 id 追加回去（保证版本新增的容器不会被静默漏掉）—— 副作用是**你有意删掉的**
        /// id 也会被加回来。把那些 id 写到这里即可永久豁免。
        /// </para>
        /// <para>写法：<c>rat_cage, some_custom_box</c>（逗号/分号/空白分隔，忽略大小写）。</para>
        /// </summary>
        public const string DefaultIdsVeto = "8B_DefaultIdsVeto";

        // ══════════════════════════════════════════════════════════════
        // z_ 诊断与实验（永远垫底）
        // ══════════════════════════════════════════════════════════════
        /// <summary>诊断日志总开关（一个开关管 CI / SBE / DIY 三个模块 + Shared 层）。</summary>
        public const string DiagLog = "z1_DiagLog";
        /// <summary>显示仓库网格线。</summary>
        public const string ShowGrid = "z2_ShowGrid";

        // ══════════════════════════════════════════════════════════════
        // 7_ 容器升级 · 分类上限
        //
        //   ★ Ids 一类的字符串项**刻意不带数字前缀**（历史如此，也避免给
        //     「玩家手改的清单」冒改名风险），所以 GridKeys 里只有 Ids 是裸名。
        // ══════════════════════════════════════════════════════════════
        /// <summary>某一类容器的 5 个键名组成（<see cref="GridKeys.Box"/> 等 8 个实例）。</summary>
        public sealed class GridKeys
        {
            private readonly string _sort;   // 数字前缀，如 "7A_"
            private readonly string _name;   // 类别名，如 "Box"

            internal GridKeys(string sortPrefix, string name)
            {
                _sort = sortPrefix;
                _name = name;
            }

            /// <summary>这一类是否允许升级。</summary>
            public string Enable => _sort + _name + "Enable";
            /// <summary>这一类的物品 id 清单（逗号分隔）。不带排序前缀。</summary>
            public string Ids => _name + "Ids";
            /// <summary>这一类所有槽共享的最大列数。</summary>
            public string MaxW => _sort + _name + "MaxW";
            /// <summary>这一类所有槽共享的最大行数。</summary>
            public string MaxH => _sort + _name + "MaxH";
            /// <summary>多槽容器：所有槽位尺寸跟随第一个槽（null = 单槽容器没有这一项）。</summary>
            public string AlignSlots => _sort + _name + "AlignSlots";
            /// <summary>类别名（日志 / 迁移表用）。</summary>
            public string Name => _name;
        }

        // 7x：容器升级的 8 个分类（顺序 = 面板里的排列顺序，也是玩家认知顺序）
        /// <summary>7A 箱子 / 货舱（最常用的一档）。</summary>
        public static readonly GridKeys Box = new GridKeys("7A_", "Box");
        /// <summary>7B 背包 / 挎包 / 腰包。</summary>
        public static readonly GridKeys Backpack = new GridKeys("7B_", "Backpack");
        /// <summary>7C 笼子。</summary>
        public static readonly GridKeys Cage = new GridKeys("7C_", "Cage");
        /// <summary>7D 多槽容器（冰箱 / 充能器 / 扫地机器人）。</summary>
        public static readonly GridKeys MultiSlot = new GridKeys("7D_", "MultiSlot");
        /// <summary>7E 机器模组槽（熔炉 / 净水器 / 解序器 …）。</summary>
        public static readonly GridKeys MultiSlotModule = new GridKeys("7E_", "MultiSlotModule");
        /// <summary>7F Mod 容器（蛙哥养蛊机 / AI 生成器）。</summary>
        public static readonly GridKeys ModContainer = new GridKeys("7F_", "ModContainer");
        /// <summary>7G 蛙哥妙妙箱。</summary>
        public static readonly GridKeys WageBox = new GridKeys("7G_", "WageBox");
        /// <summary>7H 蛙哥虚空珠袋。</summary>
        public static readonly GridKeys WageBackpack = new GridKeys("7H_", "WageBackpack");
    }
}
