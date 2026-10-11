// 2026-10-07：充能器（recharger）贴图钳制 —— 升级后只显示前 4 颗电池。
//
// ═══════════════════════════════════════════════════════════════════════════
// 问题
// ═══════════════════════════════════════════════════════════════════════════
// 用升级容器的功能把充能器网格改大之后，塞进超过原版数量（4 颗）的电池，
// 机器图标上的电池贴图会一路排到图标外面去。
//
// 原版：充能器窗口是 2×2 = 4 格，所以最多 4 颗电池，贴图正好铺满机器图标。
//
// ═══════════════════════════════════════════════════════════════════════════
// 原版贴图是怎么画的（IL2CPP 原生反汇编取证，非伪代码）
// ═══════════════════════════════════════════════════════════════════════════
// MachineRecharger.Recharger() @ RVA 0x564CD0 创建机器时会挂一批回调：
//   · 物品 +0x88 = Func<GameItem, List<ItemSpriteModifier>>
//       = <>c__DisplayClass1_0.<Recharger>b__4 @ RVA 0x577AB0
//   · 物品 +0x168 / 窗口 +0x48/+0x50/+0x60/+0x68 = 充放电与刷新回调
//
// <Recharger>b__4 的循环体（0x577C40..0x577DB9）逐条读出来是：
//     var list = new List<ItemSpriteModifier>();
//     int i = 0;
//     foreach (var item in this.itemInventory.Items) {
//         Sprite s = RenderHandler.LoadFromAtlas("Items/items_tool", <item 自己的图标>);
//         list.Add(new ItemSpriteModifier(s, new Vector2(i * 16, 0), ...));   // ← mov eax,r14d; shl eax,4
//         i++;
//     }
//     list.Add(顶部覆盖层 "recharger_top", (0,0));                            // 固定 1 条，0x577E09
//     if (PlayerStore.Instance.<+0xA4>)                                        // 条件：指示灯
//         foreach (...) list.Add("recharger_o"/"recharger_g", (i*16 + 7, 0)); // 0x577F39 起
//
//   ⇒ **每颗电池一条贴图，横向位置 = i × 16**。机器图标宽 4 格 = 64px，
//     所以第 5 颗落在 x=64 —— 正好出界。这就是"贴图超过四个"的根因。
//
// 消费端（数据流已逐层核实）：
//   GameItemElement.HandleInnateModSprite() @ 0x78AFD0
//     → 取 this.<+0x88>() 拿到上面那份 list
//     → SortedByPriority(list)
//     → foreach: UpdateInnateModSprite(i, mod.sprite, mod.position, ...) @ 0x78CE30
//
//   ★ 为什么挂 UpdateInnateModSprite 而不是别的：
//     · 它是**非编译器生成**的普通私有方法（名字可直接从 dump.cs 抄，不用猜闭包名）；
//     · 字节扫确认**全库只有 1 个调用点**（0x78B189，就在 HandleInnateModSprite 里）；
//     · `this` 就是那个 GameItemElement（本身是 GameItem），能判容器 id；
//     · 电池的**位置参数直接传进来**，可以按 x 过滤，不必知道电池总数。
//     · 反汇编确认 `i` 只被用作 List.get_Item(i) 的下标，不做命名/身份用途
//       ⇒ Prefix 返回 false 跳过绘制是安全的（后续下标照常各自建节点）。
//
//   ✗ 另一条 UpdateModSprite（ValidatePartialSprite 内，0x78F14C/0x78F349）走的是
//     **+0x98** parentItemSpriteModifierForItemFunc，与充能器无关，**不碰**。
//
// ═══════════════════════════════════════════════════════════════════════════
// 本补丁做什么
// ═══════════════════════════════════════════════════════════════════════════
// Prefix 返回 false = 跳过这一次贴图绘制。判据（顺序即成本顺序）：
//   ① pos.x < 64  → 放行（纯托管比较，绝大多数调用走这条，零额外开销）
//   ② 不是充能器  → 放行（只有出界贴图才会触发 id 判定）
//   ③ 其余        → 跳过（= 只保留 x<64 的：前 4 颗电池 x=0/16/32/48、
//                          顶部覆盖层 x=0、前 4 个指示灯 x=7/23/39/55）
//
// ★ 只作用于充能器：别的机器（酒架 / 液体容器 / 备菜台）也走这条 innate 通道，
//   它们各自的贴图布局我们没核过，不能顺手一起改。
//   要扩展到别的容器，往 ClampIds 里加 id 即可（但必须先核那个容器的布局）。
//
// ★ 原版（≤4 颗电池）不受影响：所有贴图 x ≤ 55 < 64，一条都不会被跳过。
using Il2Cpp;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    /// <summary>
    /// 充能器贴图钳制。见文件头注释（原生取证过程都写在上面）。
    /// </summary>
    internal static class RechargerSpriteClamp
    {
        /// <summary>充能器机器图标的宽度（px）。4 格 × 16px。电池贴着 x = i*16 排。</summary>
        private const float MachineWidthPx = 64f;

        /// <summary>要钳制的容器 id。加新容器前必须先核它的贴图布局。</summary>
        private static readonly string[] ClampIds = { "recharger" };

        // ── 诊断（有界，避免刷屏）──
        private static readonly System.Collections.Generic.HashSet<float> _seenX = new System.Collections.Generic.HashSet<float>();
        private static int _probeSlots = 14;    // 最多打印 14 个**不同** x，用来把贴图排布画出来
        private static int _skipped;            // 累计跳过条数
        private static bool _skipLogged;

        /// <summary>Prefix：true = 照常绘制；false = 跳过这一次贴图。</summary>
        internal static bool ShouldDraw(GameItemElement el, UnityEngine.Vector2 pos)
        {
            try
            {
                // ① 机器宽度之内 —— 直接放行（纯托管比较，不走任何原生调用）
                if (!(pos.x >= MachineWidthPx)) return true;

                // ★ 刻意**不加** WorldReadyGate.ShouldDefer 门禁（与本文件其它补丁的默认约定不同，理由如下）：
                //   本 Prefix 只决定"少画一条贴图"，**不改任何游戏状态**，所以不存在
                //   "加载深栈里干重活"的问题。
                //   反过来，如果加载期放行：HandleInnateModSprite（由 ValidateSelectLocked 调用，
                //   而我们的读档恢复链会主动触发 validate）会在加载期把**未钳制**的节点
                //   （x=64/80…）建出来；之后钳制生效时，这些节点不会被原版的尾部裁剪逻辑
                //   清掉（它按"修饰器总数"裁，总数没变）→ 残留贴图，正好复现要修的症状。
                //   ⇒ 必须无条件钳制。
                //   读 identifier 的安全性：我们此刻就在该 GameItemElement 自己的方法里，
                //   对象必然是活的；且 IdMatch.IdentifierOf 内部 try/catch，读失败返回 ""，
                //   最坏情况退化成"不钳制"，不可能崩。

                // ② 只有出界的贴图才去判容器身份
                if (el == null) return true;
                if (!ContainerSuite.Shared.IdMatch.IsAnyId(el, ClampIds)) return true;

                // ③ 出界的充能器贴图 → 跳过
                _skipped++;
                if (!_skipLogged)
                {
                    _skipLogged = true;
                    // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 纯图形排查诊断。
                    Diag($"[充能器贴图] 已钳制：只显示前 4 颗电池（跳过 x={pos.x:0.#} 的贴图；"
                        + "超出原版机器宽度 " + MachineWidthPx + "px）。");
                }
                return false;
            }
            catch (System.Exception ex) { ContainerSuite.Shared.CatchLogOnce.Warn(ex); return true; }
        }

        /// <summary>
        /// 主动探针（有界）：**每个不同的 x 各打一行**，用来证明两件事 ——
        ///   ① <c>Vector2</c> 参数在 Harmony 侧没有被编组错（打出荒谬值就说明这个挂点不可用）；
        ///   ② 贴图排布确实是「电池 x = i*16」+「覆盖层 (0,0)」+「指示灯 x = i*16+7」。
        /// 场景预期：充能器 4 颗电池时应看到 x = 0、16、32、48（+ 7、23、39、55、0）；
        /// 放到第 5 颗时多出 x = 64（+ 71）—— 那两条会被 <see cref="ShouldDraw"/> 跳过。
        /// ★ 无条件打日志（只受 _probeSlots 上限约束）：失败也要看得见。
        /// </summary>
        internal static void Probe(GameItemElement el, UnityEngine.Vector2 pos)
        {
            if (_probeSlots <= 0) return;
            if (!_seenX.Add(pos.x)) return;
            _probeSlots--;
            try
            {
                string id = ContainerSuite.Shared.IdMatch.IdentifierOf(el);
                // ★ 2026-10-09：改走 Diag（受 DiagLog 总闸控制）—— 纯图形排查诊断。
                Diag($"[充能器贴图] 探针 新位置 x={pos.x:0.#} y={pos.y:0.#} id='{id}'"
                    + (pos.x >= MachineWidthPx ? " ← 出界，将被跳过" : ""));
            }
            // 纯探针日志：取 id / 打日志失败均无害，吞掉即可。
            catch { }
        }
    }

    // ═══════════════════════════════════════════════════════════════════════
    // 唯一的挂点：GameItemElement.UpdateInnateModSprite(int, Sprite, Vector2, int, bool, ItemSpriteModifier)
    //   数据流与调用点唯一性见文件头。
    //   InstallPatches 会按命名空间 "DIYUpgrade" 收到本类（嵌套类 Namespace 同外层）。
    // ═══════════════════════════════════════════════════════════════════════
    [HarmonyLib.HarmonyPatch(typeof(GameItemElement), "UpdateInnateModSprite")]
    private static class RechargerSpriteClampPatch
    {
        [HarmonyLib.HarmonyPrefix]
        private static bool Prefix(GameItemElement __instance, UnityEngine.Vector2 modPosition)
        {
            RechargerSpriteClamp.Probe(__instance, modPosition);
            return RechargerSpriteClamp.ShouldDraw(__instance, modPosition);
        }
    }
}
