# 移除的死代码 / 历史叙述归档（2026-10-10，P0-3）

来源：`容器优化方案.md` P0-3「注释历史化 + 清死代码墓地」。

以下内容为 **F11 / F12 诊断台账**的原始实现与「如何恢复」说明，已从源码删除并归档于此。
需要排障时，从本文件取回对应片段即可（源码里只保留一行指向本文件的说明）。

---

## 1. SkipXiaowoBayRestore.cs —— F12 台账移除说明（原 104-116 行）

```
    // ── 诊断台账（F12）已于 S8m 移除 ─────────────────────────────────────
    // 原实现：LogCap 常量 + _recent 环形队列 + _skipCount/_passCount 计数 +
    //         Note() 逐条记录 + TickDiagnostics() 每帧读 F12 打印。
    // 移除原因：S8l 实机验证通过（避让与移交门均正常），台账属排障期脚手架。
    //         每帧读键盘 + 字符串格式化属常驻开销，且 Note() 在拦截路径上
    //         每秒可被调上万次。
    // 重新启用方法：把下方 Note(site, item, skipped) 的调用点恢复为
    //   ① 声明 LogCap / _recent / _skipCount / _passCount 四个成员；
    //   ② 在 Tick() 里调 SkipXiaowoBayRestore.TickDiagnostics()；
    //   ③ 用本文件末尾 TickDiagnostics 注释块里的原始实现。
    // 关键判读信息（台账里最有用的两行，现在直接看启动日志即可）：
    //   拦截次数 > 0  → 避让真的在跑
    //   拦截次数 = 0  → 入口从未被触发 = 多半是判据不匹配或补丁没挂上
```

## 2. SkipXiaowoBayRestore.cs —— F12 台账原始实现（原 648-668 行）

```
    // ═══════════════════════════════════════════════════════════════════
    //  S8m 移除：F12 诊断台账（Note 逐条记录 + TickDiagnostics 打印）
    //  保留原实现全文于下方，需要排障时照抄回 Tick() 即可恢复。
    //  ── 原 Note 实现 ──────────────────────────────────────────────
    //  private static void Note(string site, GameItem item, bool skipped) {
    //      if (skipped) _skipCount++; else _passCount++;
    //      if (_recent.Count >= LogCap) _recent.Dequeue();
    //      _recent.Enqueue($"{(skipped ? "拦截" : "放行")} @{site} → {IdOf(item)}");
    //  }
    //  ── 原 TickDiagnostics 实现（每帧读 F12，注意它本来是性能开销源）──
    //  public static void TickDiagnostics() {
    //      if (!Input.GetKeyDown((KeyCode)293)) return;              // F12
    //      MelonLogger.Msg($"════ XIAOWO 避让台账 ════\n" +
    //          $"拦截 {_skipCount} 次 / 放行 {_passCount} 次\n" +
    //          $"加载期(ShouldDefer)={WorldReadyGate.ShouldDefer}  " +
    //          $"DIY写入中(IsApplying)={RestoreArbiter.IsApplying}\n" +
    //          $"精工扩舱避让(SkipXiaoBay)=" +
    //          $"{(DiyUpgradeMod.SkipXiaowoBayEnabled ? "开" : "关 ← 6 个货舱主权已交还 XIAOWO")}\n" +
    //          $"── 最近记录 ──\n{string.Join("\n", _recent)}");
    //  }
    // ═══════════════════════════════════════════════════════════════════
```

## 3. SkipWageUpgradeRestore.cs —— F11 台账移除说明（原 360-371 行）

```
    // ── 诊断台账（F11）已于 S8m 移除 ─────────────────────────────────────
    // 原实现：LogCap 常量 + _recent 环形队列 + _skipCount/_passCount 计数 +
    //         Note() 逐条记录 + TickDiagnostics() 每帧读 F11 打印。
    // 移除原因：S8l 实机验证通过（避让与虚空珠移交门均正常），台账属排障期
    //         脚手架。每帧读键盘 + 字符串格式化是常驻开销，且 Note() 在拦截
    //         路径上每秒可被调上万次。
    // 重新启用方法：照抄本文件末尾注释块里的 Note / TickDiagnostics 原实现，
    //   并在 Tick() 里调 SkipWageUpgradeRestore.TickDiagnostics()。
    // 关键判读信息（台账里最有用的两行，现在直接看启动日志即可）：
    //   Install 末尾的「Wage 避让：N/N 成功」—— N<N 说明补丁没挂上（静默失效）
    //   移交时 HandOverVoidBead 会打一行「虚空珠已移交给 DIY：基准 20x10」
```

## 4. SkipWageUpgradeRestore.cs —— F11 台账原始实现（原 372-395 行）

```
    // ═══════════════════════════════════════════════════════════════════
    //  S8m 移除：F11 诊断台账（Note 逐条记录 + TickDiagnostics 打印）
    //  保留原实现全文于下方，需要排障时照抄回 Tick() 即可恢复。
    //  ── 原 Note 实现 ──────────────────────────────────────────────
    //  private static void Note(string site, GameItem item, bool skipped) {
    //      if (skipped) _skipCount++; else _passCount++;
    //      if (_recent.Count >= LogCap) _recent.Dequeue();
    //      _recent.Enqueue($"{(skipped ? "拦截" : "放行")} @{site} → {NormalizeId(item)}");
    //  }
    //  ── 原 TickDiagnostics 实现（每帧读 F11，注意它本来是性能开销源）──
    //  public static void TickDiagnostics() {
    //      if (!Input.GetKeyDown((KeyCode)292)) return;              // F11
    //      MelonLogger.Msg($"════ 蛙哥避让台账 ════\n" +
    //          $"拦截 {_skipCount} 次 / 放行 {_passCount} 次\n" +
    //          $"加载期(ShouldDefer)={WorldReadyGate.ShouldDefer}  " +
    //          $"DIY写入中(IsApplying)={RestoreArbiter.IsApplying}\n" +
    //          $"妙妙箱避让(SkipWageBox)=" +
    //          $"{(DiyUpgradeMod.SkipWageBoxEnabled ? "开" : "关 ← 主权已交还蛙哥")}\n" +
    //          $"虚空珠：默认归蛙哥（拖垃圾逐格解锁保留）；按住升级键拖材料 = 永久移交 DIY\n" +
    //          $"── 最近记录 ──\n{string.Join("\n", _recent)}");
    //  }
    // ═══════════════════════════════════════════════════════════════════
```

## 5. DiyUpgradeMod.Patches.cs —— 已压平的裸块注释（原 474 行）

```
    {   // A2：原 for(round) 单次循环已并入上面的墙钟等待；保留块以缩小 diff。
```

该 `{ }` 是 2026-10-07 为「缩小 diff」而保留的空壳块，内部逻辑已并入上方的墙钟等待；
2026-10-10 按当前结构压平，去掉花括号与这条历史叙述。