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

