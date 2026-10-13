# 读档尺寸恢复 / 重建沿革（源码注释逐字搬迁）

> 本文件由源码注释逐字搬迁（2026-10-13，注释精简 · 批次1），只做了搬迁、未做任何改写。
> 抽取范围为源码注释行（以 // 或 /// 开头，含全角框线行）；代码行未收录。
> 行号对应搬迁时的源码版本，仅作定位用。

## 文件头注释（DiyUpgradeMod.Restore.cs L1-17）

```
// 2026-10-02：读档尺寸恢复功能重建。
//
// 背景：2026-09-29 为了规避 CLR fatal 0x80131506，TryRestoreRememberedGridShape 被整体
//   改成 `return false`，方法体丢失（源码树和参考工程里都只剩空壳）。
// 来源：从 D:\games\mod调试\_backup_deploy\20260926-1625\DIYUpgrade.dll（禁用之前的版本）
//   反编译取回原始逻辑，再按下面的原则重写。
//
// ★★ 为什么必须拆成小方法
//   本方法被 XIAOWO 兼容补丁（SkipXiaowoBayRestore.Prefix_Bool）挂了 Harmony 钩子。
//   被挂钩的方法体过大时，JIT 编译会与 MonoMod 的 CompileMethodHook 冲突 →
//   CLR fatal 0x80131506（托管 catch 抓不到，日志无堆栈）。
//   → 被挂钩的入口方法必须极小，全部逻辑下沉到未被挂钩的小方法里。
//
// ★ 与原始版本的唯一有意差异
//   原版在"窗口尺寸没变"的分支里会手写 rectTransform.sizeDelta / outsideBorder.sizeDelta。
//   按本项目禁止事项 #9（手写 sizeDelta 绕过画布缩放、破坏 GetWorldCorners 判定，已坑两次），
//   这里改为只做 ForceRecomputeDynamicBackground + FixBounds + Validate + 重建布局。
```

## T-C2（2026-10-13）：ReapplyFromMeta 的「3 秒 TTL」删除说明（`DiyUpgradeMod.Meta.cs`）

> 由批次3 从源码搬迁（源码保留关键判据 + 指针）。

```
        // T-C2 阶段二（2026-10-13）：原「3 秒 TTL」节流（_lastReapplyFromMetaTime）已删除。
        //   ★ 真正原因（2026-10-13 复核）：该 TTL 判据 `if (!force && …ElapsedMs<3000) return;`
        //     依赖 force==false，但全库 ReapplyFromMeta 只有 2 个调用点
        //     （Core.cs 的 RestoreArbiter.RegisterApplier 委托；Restore.cs 的读档预热 Step），
        //     两者都传 force:true ⇒ 该分支从未被进入，TTL 早已是死逻辑。
        //   （更正：commit 63b3a61 / 31c3a5e 曾称「阶段一实机观测次数为 0」——
        //     复核确认观测代码从未真正部署（游戏的 -dev 构建不含 ObserveTtlRejectVsMemo），
        //     故该说法不成立；真正判据为上一条「调用点全 force:true」。）
        //   删除后节流职责由 RestoreArbiter（队列 + 冷却帧，全进程）与 _restoreMemo
        //   （状态签名判据，见 RestoreShapeCore）承担；force 参数保留（签名不变，
        //   Arbiter 调用点零改动），现已无事可绕，仅作历史语义占位。
```

