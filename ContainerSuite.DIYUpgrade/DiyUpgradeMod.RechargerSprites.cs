// 充能器（recharger）贴图钳制 —— 升级后只显示前 4 颗电池。
// 完整取证（反汇编 RVA / 字节码 / 调用点链 / 为什么挂 UpdateInnateModSprite）：
//   docs/diy-recharger-sprite.md
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
