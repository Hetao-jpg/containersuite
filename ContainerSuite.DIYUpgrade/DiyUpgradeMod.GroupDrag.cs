// 阶段1（2026-10-11）：批量组拖升级 —— 按住升级键框选多堆材料，拖到同一容器连续升级。
// 完整背景（点歧义取舍 / Prefix 快照理由 / 与蛙哥·钥匙卡包共存 / 分帧执行决策）：
//   docs/diy-groupdrag.md

using ContainerSuite.Shared;
using HarmonyLib;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using MelonLoader;
using UnityEngine;   // D-2：组拖进度屏幕提示（GUIStyle / GUI / Screen / Time）

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    // ═══════════════════════════════════════════════════════════════════════
    // 组拖状态（仅存于一次组拖的生命周期内，结束后立即清空）
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// StartGroupDrag 时快照的「被框选材料（指针 + uniqueId）」。
    /// ★ 存指针而非 GameItem 对象：组拖可能持续数秒，跨帧持有 IL2CPP 对象有
    ///   悬垂指针风险（R4）；执行时用指针重新 new GameItem 并做存活校验。
    /// ★ B-3'（2026-10-12）为什么把 uniqueId 与指针结对：裸指针跨帧的有效性
    ///   **无法自证** —— IL2CPP 分配器复用地址时，new GameItem(ptr) 会包装到
    ///   另一个物品上，此时 IdFieldNull 反而能通过（新对象有合法 identifier）
    ///   ⇒ 会吞错材料。uniqueId 是每实例 id：快照时在活对象上读一次（浅栈、安全），
    ///   每步比对即可识别「这个地址已经变成别人的了」。
    ///   ★ 否决「按标识符查表」的方案：identifier 是类型级字符串，
    ///     DirectoryMaster.Item(identifier) 不保证返回玩家拖落的那一个实例
    ///     （场上两个同型容器时可能解析到另一个）—— 指针才是唯一事实。
    ///   ★ H3（2026-10-12）：uid 可能读不到（记为 0）。此时**不丢弃**该项，
    ///     而是降级为「仅指针校验」并留痕 —— 绝不静默少升。
    /// </summary>
    private static readonly List<(IntPtr Ptr, long Uid)> _groupDragMaterials = new List<(IntPtr, long)>();

    /// <summary>最近一次组拖是否已武装（StartGroupDrag 到 EndGroupDrag 之间为 true）。</summary>
    private static bool _groupDragArmed;

    /// <summary>组拖执行协程是否正在跑（防重入）。</summary>
    private static bool _groupUpgradeRunning;

    // ★ T3（2026-10-12，借鉴蛙哥 IsDragRelease 的**反向用法**）：组拖"事件丢失"对账。
    //   蛙哥用它当**触发条件**（"游戏还在拖但鼠标已松 = 该升级了"）；我们反过来用它做
    //   **兜底观测**：已经武装、鼠标也松了，但 EndGroupDrag 迟迟不来 ⇒ 事件被吃掉/时序变了，
    //   玩家视角是"松手没反应"（静默失效）。
    //   ★ 只观测、**不自动复位** —— 擅自把 _groupDragArmed 清掉会与游戏自身状态打架
    //     （也不知道该不该替它把材料放回去）。先拿证据，再决定要不要干预。
    //   ★ 2026-10-13：T3 对账三件套（字段 / ReconcileGroupDragArm / 调用点）整体收进
    //     #if DIAG —— 它是"只观测不干预"的排障脚手架，Release 无需承担（绝不删，见方案 T-A5）。
#if DIAG
    private static int _groupDragMouseUpFrames;
    private static bool _groupDragLostLogged;
    /// <summary>鼠标抬起后连续多少帧仍处于"已武装"才判定事件丢失（毫秒级抖动不算）。</summary>
    private const int GroupDragLostAfterUpFrames = 5;
#endif

    // ★ D-2（2026-10-12）：组拖进度提示的状态（100 堆材料 × 6 帧 ≈ 10 秒 @60fps，
    //   中间无任何反馈玩家会以为卡死了）。由 RunGroupUpgrade 每步更新，
    //   由 Infrastructure Mod.OnGUI → DrawGroupDragProgress() 读取并绘制。
    private static int _groupDragProgressCurrent;
    private static int _groupDragProgressTotal;
    private static float _groupDragProgressAt = -10f;
    private static GUIStyle _groupDragProgressStyle;

    /// <summary>
    /// StartGroupDrag Prefix：快照框选材料。
    /// <para>
    /// ★ 只快照 |IsAllowedMaterial| 命中的元素 —— 框选里可能混着非材料物品
    ///   （比如顺手把旁边的箱子也框进来了），那些不该参与升级，也不该
    ///   影响"这是一次升级意图"的判定。
    /// </para>
    /// <para>
    /// ★ 不做任何游戏状态写入，异常一律吞掉（Prefix 必须永远返回 true，
    ///   绝不能因为我们的快照失败而阻断游戏自己的组拖）。
    /// </para>
    /// <para>
    /// ★★ 不做 JIT 预热的理由（见文件末补丁类注释）：本方法跑在
    ///   `StartGroupDrag` 的 detour 栈上，但该方法是**玩家输入触发**的，
    ///   首编译天然在浅栈；而若用 [JitWarmup] 强行在启动期预热，会因
    ///   内部调用 `IsUpgradeKeyHeld()` 等自有方法而**嵌套 JIT** —— 反而危险。
    /// </para>
    /// </summary>
    private static void SnapshotGroupDragMaterials(ItemMultiSelectHandler __instance)
    {
        try
        {
            _groupDragMaterials.Clear();
            _groupDragArmed = false;

            if (!IsUpgradeKeyHeld()) return;                       // 没按升级键 = 普通组拖，不管
            if (WorldReadyGate.ShouldDefer) return;                // 加载期不参与
            if (!IsGroupDragEnabled) return;                       // cfg 总开关（默认开）

            var selected = __instance?.selectedItems;
            if (selected == null) return;

            // H3：读不到 uniqueId 的堆数 —— 这类项**不丢弃**（降级为仅指针校验），
            //   但必须留痕，否则就是"悄悄少了一层保护"。
            int uidMissing = 0;

            for (int i = 0; i < selected.Count; i++)
            {
                var el = selected[i];
                if (el == null) continue;
                GameItem gi = null;
                try { gi = (GameItem)(object)el; } catch { continue; }
                if (gi == null || IdFieldNull(gi)) continue;
                // 只收"能当升级材料"的：非材料留在选择集里不参与，也不阻断
                bool allowed;
                try { allowed = IsAllowedMaterial(gi); } catch { continue; }
                if (!allowed) continue;

                IntPtr p = IntPtr.Zero;
                try { p = ((Il2CppObjectBase)gi).Pointer; } catch { continue; }
                if (p == IntPtr.Zero) continue;
                long uid = 0L;
                // H3：uid 读不到（异常 / 字段未恢复）不算致命 —— 记 0，执行端自动降级为
                //   "仅指针校验"。**不丢弃这一堆**：宁可少一层保护，也不能静默少升级。
                try { uid = gi.uniqueId; } catch { uid = 0L; }
                if (uid == 0L) uidMissing++;
                _groupDragMaterials.Add((p, uid));
            }

            if (uidMissing > 0)
                LogOnce("[容器升级] 组拖：有 " + uidMissing + " 堆材料读不到 uniqueId，"
                    + "本次对它们仅按指针做存活校验（地址复用无法识别）", warning: true);

            if (_groupDragMaterials.Count > 0)
            {
                _groupDragArmed = true;
#if DIAG
                // T3：本次武装从头计数（对账用的"鼠标抬起连续帧数"要干净起步）。
                _groupDragMouseUpFrames = 0;
                _groupDragLostLogged = false;
#endif
                // 首次启用留一条**无条件**日志（项目教训：默认静默的日志会被
                // 误读成"没执行"）。用 LogOnce 的 3 秒去重避免刷屏。
                LogOnce("[容器升级] 批量框选已武装：" + _groupDragMaterials.Count + " 堆材料（松手在容器上即连续升级）");
            }
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("GroupDrag:snapshot", ex); }
    }

    /// <summary>
    /// EndGroupDrag Prefix：解析落点并启动分帧升级协程。
    /// <para>
    /// ★ 必须挂 Prefix —— EndGroupDrag 内部会 ClearSelection()，Postfix 时
    ///   快照虽还在（我们自己存的），但 hoverItem 可能已被清。
    ///   Prefix 是"所有信息都还在"的唯一时机。
    /// </para>
    /// <para>
    /// ★ 不阻断原方法（永远 return true）：游戏自己的组拖逻辑必须正常跑完
    ///   （它会 CommitPrecisePlacement 把材料放进目标容器）。我们在它跑完之后
    ///   的下一帧再开始逐堆消耗 —— 顺序天然正确：材料先进容器，再被升级吃掉。
    /// </para>
    /// </summary>
    private static void ArmGroupUpgrade(ItemMultiSelectHandler __instance)
    {
        try
        {
            if (!_groupDragArmed) return;
            _groupDragArmed = false;   // 无论成败，本次组拖的武装状态到此为止
            if (WorldReadyGate.ShouldDefer) { _groupDragMaterials.Clear(); return; }
            if (RestoreArbiter.IsApplying) { _groupDragMaterials.Clear(); return; }   // 仲裁期不抢
            if (!IsGroupDragEnabled) { _groupDragMaterials.Clear(); return; }
            if (_groupUpgradeRunning) { _groupDragMaterials.Clear(); return; }        // 防重入

            // ── 落点解析（唯一悬停容器，三级判据，无回退猜测） ──────────────
            GameItem container = ResolveGroupDragTarget(__instance);
            if (container == null)
            {
                // 悬停落空 = 玩家没拖到容器上 = 不是升级意图，静默放弃。
                _groupDragMaterials.Clear();
                return;
            }

            // 快照成独立数组交给协程（协程里不再引用这个列表）
            var mats = _groupDragMaterials.ToArray();
            _groupDragMaterials.Clear();
            if (mats.Length == 0) return;

            // ── 虚空珠移交门（与单拖 EndDragPatch 完全同语义） ─────────────
            // ★★ 为什么组拖必须自己走这一步，不能靠 MatchCategory 的判据：
            //   GetWageCategory 里 `VoidBeadHandoverRequested` = 按住升级键即为真，
            //   所以组拖时 MatchCategory 已经返回 WageBackpack —— 但那条路径
            //   **只报意图、不写 tag**（判据函数必须无副作用，见其注释）。
            //   单拖在 EndDragPatch 里紧跟一步 HandOverVoidBead 打 tag + 记基准；
            //   组拖若不补这一步，就会掉进 `PrepareGrid` 的 MigrateLegacyVoidBead
            //   懒迁移分支 —— 那条分支在单拖路径上**永远被短路**，从未经实机验证。
            //   ⇒ 照抄单拖：意图成立就地移交，让后续升级走已验证的 HandOver 基准。
            if (WageContainerRegistry.IsVoidBeadStorage(container)
                && VoidBeadHandoverRequested(container))
            {
                try
                {
                    GameGridInventory beadGrid = GetResizableInventory(container);
                    WageContainerRegistry.HandOverVoidBead(container, beadGrid);
                    // 判据链结果变了，缓存必须失效（照抄 EndDragPatch）
                    _prepareCache.Clear();
                    _resizableCache.Clear();
                    _metaCache.Clear();
                }
                catch (System.Exception ex) { CatchLogOnce.Warn("GroupDrag:handover", ex); }
            }

            // ★ B-3'（2026-10-12）：容器同样按「指针 + uniqueId」交给协程。
            //   指针 = 玩家实际拖落的那一个（唯一事实）；uniqueId = 跨帧存活判据。
            IntPtr containerPtr = IntPtr.Zero;
            try { containerPtr = ((Il2CppObjectBase)container).Pointer; } catch { return; }
            if (containerPtr == IntPtr.Zero) return;
            // H3：容器 uid 读不到（0）**不中止组拖** —— 降级为仅指针校验 + 留痕。
            //   原方案在此直接 return，会让"uid 一时读不到"变成"整次组拖静默失效"。
            long containerUid = 0L;
            try { containerUid = container.uniqueId; } catch { containerUid = 0L; }
            if (containerUid == 0L)
                LogOnce("[容器升级] 组拖：容器读不到 uniqueId，本次仅按指针做存活校验"
                    + "（地址复用无法识别）", warning: true);

            try { MelonCoroutines.Start(RunGroupUpgrade(containerPtr, containerUid, mats)); }
            catch (System.Exception ex) { CatchLogOnce.Warn("GroupDrag:start-coroutine", ex); }
        }
        catch (System.Exception ex)
        {
            CatchLogOnce.Warn("GroupDrag:arm", ex);
            _groupDragMaterials.Clear();
        }
    }

    /// <summary>
    /// 落点解析：唯一的悬停容器。
    /// <para>
    /// 判据链（短路，不猜）：
    ///   ① <c>hoverItem</c> —— 鼠标正悬停的物品（最准）
    ///   ② <c>hoverInventory</c> 反查 owner —— 悬停在容器的网格上时
    ///      hoverItem 可能为空，用库存指针反查。
    /// 都落空 → null（放弃）。
    /// </para>
    /// <para>
    /// ★ 为什么不加"材料当前所在容器"这条回退：见文件头注释（会把
    ///   "从背包选料"误判成"要升背包"）。
    /// </para>
    /// </summary>
    private static GameItem ResolveGroupDragTarget(ItemMultiSelectHandler h)
    {
        GameItem target = null;
        try { target = h?.hoverItem; } catch { target = null; }
        if (target != null) return target;

        IntPtr invPtr = IntPtr.Zero;
        try
        {
            var inv = h?.hoverInventory;
            if (inv != null) invPtr = ((Il2CppObjectBase)inv).Pointer;
        }
        catch { invPtr = IntPtr.Zero; }
        if (invPtr != IntPtr.Zero) return InventoryOwner(invPtr);

        return null;
    }

    /// <summary>
    /// 分帧批量升级协程。
    /// <para>
    /// ★ 每步：重解析材料指针 → 存活校验 → PrepareUpgradeInternal → apply →
    ///   ConsumeMaterial → yield 一帧。**不跨帧持有任何 IL2CPP 对象**。
    /// </para>
    /// <para>
    /// ★ 每步重建容器对象（容器可能已被销毁 —— 极端情况下玩家在升级
    ///   过程中做了别的操作）。存活校验失败就整体停止。
    /// </para>
    /// <para>
    /// ★ 预算：最多处理 materials.Length 堆；另设一个帧数硬上限防止极端情况下
    ///   协程长时间占用（正常不会触及）。
    /// </para>
    /// <para>
    /// ★ B-3'（2026-10-12）：容器与材料都做「指针 + uniqueId」双验 ——
    ///   IL2CPP 复用地址后 new GameItem(ptr) 会包装到别的物品上，此时
    ///   IdFieldNull 能通过（新对象有合法 identifier），只有 uid 比对能识别。
    ///   uid == 0（快照时没读到）的项降级为仅指针校验，**不丢弃**（H3）。
    /// </para>
    /// </summary>
    private static IEnumerator RunGroupUpgrade(IntPtr containerPtr, long containerUid,
        (IntPtr Ptr, long Uid)[] materials)
    {
        _groupUpgradeRunning = true;
        int done = 0;
        try
        {
            // 第一帧先等游戏自己的 EndGroupDrag 跑完（材料落位、幽灵清理）。
            yield return null;

            // D-2：进度提示起算（等上面那一帧之后再置，避免把等待帧算进"升级中"）。
            _groupDragProgressTotal = materials.Length;
            _groupDragProgressCurrent = 0;
            _groupDragProgressAt = Time.realtimeSinceStartup;

            for (int i = 0; i < materials.Length; i++)
            {
                if (WorldReadyGate.ShouldDefer) break;
                if (RestoreArbiter.IsApplying) break;
                if (!IsGroupDragEnabled) break;

                // 重解析容器，存活校验（B-3'：uniqueId 比对识别地址复用；
                //   不按 id 查表 —— DirectoryMaster.Item(identifier) 不保证是这一个实例）
                GameItem container = null;
                try { container = new GameItem(containerPtr); } catch { break; }
                if (container == null) break;
                if (containerUid != 0L)
                {
                    long nowUid = 0L;
                    bool read = true;
                    try { nowUid = container.uniqueId; } catch { read = false; }
                    // 读不到 uid ⇒ 无法自证身份 ⇒ 与"换了对象"同样处理：整体停止。
                    if (!read || nowUid != containerUid) break;
                }
                if (IdFieldNull(container)) break;

                // 重解析材料，存活校验（材料可能已被前面的消耗销毁；
                //   B-3'：指针 + uniqueId 双验，地址被复用成别的物品时 uid 不同 → 跳过）
                var mat = materials[i];
                if (mat.Ptr == IntPtr.Zero) continue;
                GameItem material = null;
                try { material = new GameItem(mat.Ptr); } catch { continue; }
                if (material == null) continue;
                if (mat.Uid != 0L)
                {
                    long nowUid = 0L;
                    bool read = true;
                    try { nowUid = material.uniqueId; } catch { read = false; }
                    if (!read || nowUid != mat.Uid) continue;   // 地址已复用 / 无法自证 → 跳过
                }
                if (IdFieldNull(material)) continue;

                // 材料还在容器里吗？可能已被玩家移走 —— 不追究，直接跳过。
                // ★ 这些 continue 分支**不 yield**：它们无副作用，同帧空转可接受；
                //   加 yield 反而让"整批材料都失效"时白等 N 帧。
                Cat cat = MatchCategory(material, container);
                if (cat == Cat.None) continue;

                bool consumed = false;
                try
                {
                    if (PrepareUpgradeInternal(container, cat, out var apply, out var reason))
                    {
                        // ★ 这里**刻意不**加 T2 的「每帧每容器一次」闸门：
                        //   ① 本协程以 `yield return null` 开头 ⇒ 第一次真升至少比武装晚一帧，
                        //      结构上不可能与单拖（EndDrag）同帧撞车；
                        //   ② 真正需要防的是**跨帧**那种"材料已被别的入口销毁、地址被复用"的
                        //      情形（B-3' 的 uniqueId 双验），帧闸门帮不上忙；
                        //   ③ 批次内两堆之间本来就等 6 帧，加闸门只会带来复杂且无收益的分支。
                        apply();
                        ConsumeMaterial(material);
                        consumed = true;
                        done++;
                        _groupDragProgressCurrent = done;   // D-2：进度 = 已实际升的堆数
#if DIAG
                        ProbeSyncModifiedState(container);   // T5：DIAG 实验（默认关）
#endif
                    }
                    else
                    {
                        // 升不动了（满级 / 材料数不足 / 上锁…）。记一条然后整体停。
                        LogOnce("[容器升级] 批量升级中止: " + reason);
                        break;
                    }
                }
                catch (System.Exception ex) { _log?.Error("[容器升级] 批量升级异常: " + ex.Message); break; }

                // 每步清缓存（照单拖语义：apply 委托内也会清，这里双保险）
                try { _prepareCache.Clear(); _ggiCache.Clear(); } catch { }
                _targetConsumeAtMs = PollThrottle.NowMs;   // 与单拖 EndDragPatch 同语义（T1：单调钟）

                // ★★ 分帧：一堆材料一帧，且**等待上一堆的窗口重建跑完**。
                //   apply() 内部会起 ForceRebuildAfterUpgrade 协程（3~5 帧，
                //   反复调 Validate / FixBounds / ForceRebuildLayoutImmediate）。
                //   若两堆的升级间隔 <重建时长，两个重建协程会并发操作**同一个**
                //   PixelWindow / GameGridInventory —— 中间态被对方读到，正是
                //   ValidateShapeState 链（第三方挂载的超热方法）上最危险的竞态。
                //   ⇒ 等 6 帧（> 最长重建 5 帧）再动下一堆，结构上不可能重叠。
                if (consumed)
                {
                    for (int f = 0; f < 6; f++) yield return null;
                }
            }
        }
        finally
        {
            _groupUpgradeRunning = false;
            try { _prepareCache.Clear(); _ggiCache.Clear(); } catch { }
            if (done > 0)
                LogOnce("[容器升级] 批量升级完成：共 " + done + " 堆材料");
            // D-2：刷新时间戳让"完成/中止"态重新走一遍 3 秒淡出（中止时也刷新 ——
            //   玩家至少能看到"进度停在 3/20"这种有信息量的收尾）。
            _groupDragProgressCurrent = done;
            _groupDragProgressAt = Time.realtimeSinceStartup;
        }
    }

    /// <summary>
    /// D-2（2026-10-12）：组拖进度的屏幕提示。由 Infrastructure 的 <c>Mod.OnGUI</c> 调用
    /// —— **不走** SBE 的 <c>SaveBagExpandCore.DrawGUI</c>：那是分页模块的方法，画 DIY 的
    /// 状态属反向依赖，而且 SBE 的 Enabled / _cfgPagingEnabled 门槛会把这条提示一起吞掉。
    /// <para>
    /// 样式与淡出逻辑抄 <c>SaveBagExpand</c> 的 DrawMergeLossWarning（缓存 GUIStyle、
    /// 阴影 + 淡出、全包 try/catch），y=90 是为了避开它的 y=20 区域（两者可能同屏）。
    /// </para>
    /// </summary>
    internal static void DrawGroupDragProgress()
    {
        try
        {
            if (_groupDragProgressAt < -1f) return;              // 从未跑过组拖
            const float dur = 3f;
            float elapsed = Time.realtimeSinceStartup - _groupDragProgressAt;
            if (elapsed < 0f || elapsed > dur) return;

            if (_groupDragProgressStyle == null)
            {
                _groupDragProgressStyle = new GUIStyle(GUI.skin.label);
                _groupDragProgressStyle.fontSize = 20;
                _groupDragProgressStyle.fontStyle = FontStyle.Bold;
                _groupDragProgressStyle.alignment = TextAnchor.UpperCenter;
                _groupDragProgressStyle.wordWrap = true;
            }

            float alpha = 1f;
            float fadeStart = dur * 0.6f;
            if (elapsed > fadeStart)
            {
                alpha = 1f - (elapsed - fadeStart) / (dur - fadeStart);
                if (alpha < 0f) alpha = 0f;
            }

            string txt = (_groupDragProgressTotal > 0 && _groupDragProgressCurrent >= _groupDragProgressTotal)
                ? $"批量升级完成：{_groupDragProgressTotal} 堆"
                : $"批量升级中 {_groupDragProgressCurrent}/{_groupDragProgressTotal}";

            float w = Mathf.Min(Screen.width - 80f, 420f);
            var rect = new Rect((Screen.width - w) * 0.5f, 90f, w, 30f);
            var shadow = new Rect(rect.x + 2f, rect.y + 2f, rect.width, rect.height);

            _groupDragProgressStyle.normal.textColor = new Color(0f, 0f, 0f, alpha * 0.8f);
            GUI.Label(shadow, txt, _groupDragProgressStyle);
            _groupDragProgressStyle.normal.textColor = new Color(1f, 1f, 1f, alpha);
            GUI.Label(rect, txt, _groupDragProgressStyle);
        }
        catch (Exception ex) { CatchLogOnce.Warn("GroupDrag:draw-progress", ex); }
    }

    /// <summary>
    /// T3（2026-10-12）：组拖「事件丢失」对账。由 <c>DiyUpgradeMod.Tick()</c> 每帧调用。
    /// <para>
    /// 判据：已经武装（StartGroupDrag 记下的材料集还在）**且鼠标已抬起连续 N 帧**，
    /// 说明 EndGroupDrag 没有如期到来 —— 本次升级不会发生，而且是**静默**的。
    /// 只观测、不干预（见字段区注释：擅自复位会与游戏自身状态打架）。
    /// </para>
    /// <para>
    /// ★ 为什么用"连续 N 帧"而不是"当帧鼠标抬起"：MelonLoader 的 OnUpdate 可能排在游戏
    ///   处理鼠标释放 / 派发 EndGroupDrag **之前** —— 那一帧鼠标已是抬起状态而事件还没派发，
    ///   单帧判定会对每一次正常结束的组拖误报。连续 N 帧把这个时序窗口排除掉。
    /// </para>
    /// <para>
    /// ★ 已知误报面：若游戏支持**非鼠标**的框选方式（键盘/手柄），鼠标整场都是抬起的，
    ///   会误报。当前游戏的框选是鼠标框选，故可接受；即使误报也只多一行日志，不改变行为。
    /// </para>
    /// <para>
    /// ★ 2026-10-13：整体收进 <c>#if DIAG</c>（Release 不编译；调用点同门控）。
    ///   它是纯观测脚手架，绝不删——删掉就丢了对"松手没反应"这类静默失效的唯一现场证据。
    /// </para>
    /// </summary>
#if DIAG
    internal static void ReconcileGroupDragArm()
    {
        try
        {
            if (!_groupDragArmed)                 // 常态：一次 bool 判断，零开销
            {
                _groupDragMouseUpFrames = 0;
                _groupDragLostLogged = false;
                return;
            }
            bool held;
            try { held = Input.GetMouseButton(0); } catch { return; }
            if (held) { _groupDragMouseUpFrames = 0; return; }

            _groupDragMouseUpFrames++;
            if (_groupDragMouseUpFrames < GroupDragLostAfterUpFrames || _groupDragLostLogged) return;
            _groupDragLostLogged = true;
            // 无条件留痕：这不是诊断细节，是"玩家松手没反应"的成因证据。
            ContainerSuiteLog.Warn("[容器升级] 框选升级：已武装且鼠标抬起 "
                + _groupDragMouseUpFrames + " 帧，却始终没收到 EndGroupDrag —— 事件可能被其它 mod"
                + "吃掉或时序变了，本次批量升级不会发生。本行只观测、不自动复位。");
        }
        catch (System.Exception ex) { CatchLogOnce.Warn("GroupDrag:reconcile", ex); }
    }
#endif

    // ═══════════════════════════════════════════════════════════════════════
    // Harmony 补丁类
    // ═══════════════════════════════════════════════════════════════════════
    //
    // ★ 挂 Prefix 而非 Postfix：见 ArmGroupUpgrade 注释（ClearSelection 时机）。
    // ★ 不改原方法逻辑（Prefix 永远返回 true），与蛙哥/钥匙卡包同模式。
    // ★★ 刻意**不**打 [HarmonyPrefix] —— 让这两个 Prefix 不进预热清单。
    //   理由：WarmupAllPatchMethods 的第二路会 `m.Invoke(null, 哑参)` 真调一次
    //   （`__instance` 是引用类型 ⇒ 传 null）。若打了特性，启动期就会在浅栈上
    //   真调一次 `SnapshotGroupDragMaterials(null)` → 连带嵌套 JIT 它内部的
    //   `IsUpgradeKeyHeld()` 等自有方法，**反而制造**嵌套 JIT 的 0x80131506 风险。
    //   而 `StartGroupDrag` / `EndGroupDrag` 只在玩家组拖时被调用（浅栈、非加载期），
    //   首编译天然安全 —— 预热对它们是负收益。既有的 `TargetPatch` / `EndDragPatch`
    //   同样采用"方法名叫 Prefix 的隐式识别"写法，本文件与其保持一致。
    // ═══════════════════════════════════════════════════════════════════════

    [HarmonyPatch(typeof(ItemMultiSelectHandler), "StartGroupDrag")]
    private static class GroupDragStartPatch
    {
        // ★ 无 [HarmonyPriority]：本项目刻意不与其他 mod 抢执行顺序
        //   （见 DiyUpgradeMod.Patches.cs 文件头 2026-09-29 改动说明）。
        //   快照是只读操作，谁先谁后都不影响结果。
        private static void Prefix(ItemMultiSelectHandler __instance)
        {
            SnapshotGroupDragMaterials(__instance);
        }
    }

    [HarmonyPatch(typeof(ItemMultiSelectHandler), "EndGroupDrag")]
    private static class GroupDragEndPatch
    {
        private static void Prefix(ItemMultiSelectHandler __instance)
        {
            ArmGroupUpgrade(__instance);
        }
    }
}
