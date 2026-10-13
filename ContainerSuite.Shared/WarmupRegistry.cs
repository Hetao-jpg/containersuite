using System;
using System.Collections.Generic;
using System.Reflection;

namespace ContainerSuite.Shared;

/// <summary>
/// 【2026-10-10 方案 A】补丁方法预热登记表。
///
/// ═══════════════════════════════════════════════════════════════════════
/// 为什么需要它：`WarmupAllPatchMethods` 此前只遍历带 `[HarmonyPatch]`
/// **类特性**的补丁类。但本套件有 4 处补丁是**手动 `harmony.Patch(...)`
/// 注册**的、类上没有该特性：
///   · SkipXiaowoBayRestore  （7 个 Prefix，挂 XIAOWO 的方法）
///   · SkipWageUpgradeRestore（多条 Prefix，挂蛙哥的方法）
///   · ContainerExpandPatch  （2 个 Postfix）
///   · WindowShowHook        （所有 PixelWindow.Show 重载的 Postfix）
/// ⇒ 这些方法**永远不在预热清单里**，首次 JIT 只能等真实调用；
///   一旦真实调用发生在「MonoMod detour 还在栈上」的深栈上
///   （例：SweepZombieHandles 清手柄后 XIAOWO 重走 Attach 路径），
///   old MonoMod(2022) 的 DetourRuntimeNETCore30Platform.CompileMethodHook
///   会读空指针 → CLR fatal 0x80131506（托管 catch 抓不到，日志无堆栈）。
///
/// 本类 = 唯一的登记入口：**凡手动注册的补丁，注册成功时登记一次**。
/// 预热时连同 `[HarmonyPatch]` 枚举结果一起在浅栈 invoke。
/// 这样"注册了却忘了预热"在结构上不可能再发生（注册与预热同源）。
/// ═══════════════════════════════════════════════════════════════════
/// </summary>
internal static class WarmupRegistry
{
    private static readonly List<MethodInfo> _targets = new List<MethodInfo>();

    // 声明清单：记录 ScanAndRegister 实际扫到的 [JitWarmup]，供 DIY 侧做覆盖率断言
    // （MustWarmLeaves ⊆ 本清单，缺项即报错 + [REG.BP]）。理由：见 docs/jit-warmup.md
    private static readonly List<string> _declared = new List<string>();

    /// <summary>本次启动扫描到的 [JitWarmup] 声明清单（"类型名.方法名"，已去重）。</summary>
    internal static IReadOnlyList<string> DeclaredNames { get { return _declared; } }

    // ═══════════════════════════════════════════════════════════════════
    // 【2026-10-10 P2】声明式预热标记
    //
    // 背景：升级 apply 路径的 8 个「叶子方法」此前靠 WarmupSaveLoadChain 里
    //   8 行**手抄**的 Step(...) 调用来预热。硬编码清单的病灶：
    //     ① 新增叶子时**没有任何东西**提醒作者去补那行 Step —— 漏一个就静默
    //        退化成"在 trampoline 上首编译"，正是我们防的崩溃；
    //     ② 签名一改（加个 out 参数）那行就编译不过或语义漂移；
    //     ③ 清单离叶子定义很远，看代码的人无法确认"这个叶子到底预热了没"。
    //
    // 改法：标记就写在叶子方法自己头上（定义即注册），扫描器统一收集。
    //   ⇒ 与 WarmupRegistry 原本的「注册即登记」哲学一致：
    //     手动补丁由注册处登记，叶子由特性登记，两条路都在启动期浅栈 invoke。
    //
    // ★★ 标记契约（违反 = 启动期异常或静默失效）：
    //   ① 必须是 **static** 方法（扫描器只枚举 Static）；
    //   ② 必须是 **null 安全** 的：扫描器按 WarmupAll 的哑参规则调用
    //      （值类型传 default、引用类型传 null、out/ref 传元素类型默认值），
    //      所以方法必须在首参为 null 时**尽早 return**，不得解引用；
    //   ③ 必须是**叶子**：内部不得再调用我们自己的未预热方法，
    //      否则 invoke 时会嵌套 JIT 未编译方法 → 重入 CompileMethodHook
    //      → CLR fatal 0x80131506（10:37 铁律，大方法绝不进预热）。
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>扫描整个程序集，把所有带 <see cref="JitWarmupAttribute"/> 的静态方法登记进预热表。</summary>
    /// <returns>本次新登记的方法数（已登记过的会被 Register 去重，不重复计数）。</returns>
    internal static int ScanAndRegister(Assembly asm)
    {
        int found = 0;
        if (asm == null) return 0;

        Type[] types;
        try { types = asm.GetTypes(); }
        catch (ReflectionTypeLoadException ex) { types = ex.Types; }   // 见铁律：必须用 ex.Types 兜底
        catch (Exception ex) { CatchLogOnce.Warn("WarmupRegistry.ScanAndRegister.GetTypes", ex); return 0; }
        if (types == null) return 0;

        for (int i = 0; i < types.Length; i++)
        {
            Type t = types[i];
            if (t == null) continue;

            MethodInfo[] methods;
            try
            {
                methods = t.GetMethods(BindingFlags.Static | BindingFlags.Public
                                     | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            }
            catch { continue; }   // 单个类型反射失败不应连累整张清单
            if (methods == null) continue;

            for (int j = 0; j < methods.Length; j++)
            {
                MethodInfo m = methods[j];
                if (m == null) continue;

                bool marked;
                try { marked = m.IsDefined(typeof(JitWarmupAttribute), false); }
                catch { continue; }
                if (!marked) continue;

                // 泛型方法无法用哑参 invoke（类型实参未知）—— 跳过而不是让它炸。
                if (m.IsGenericMethodDefinition || m.ContainsGenericParameters) continue;

                Register(m);
                RecordDeclared(t.Name + "." + m.Name);
                found++;
            }
        }
        return found;
    }

    /// <summary>登记一个"手动注册的补丁方法本体"（Prefix/Postfix）。</summary>
    private static void RecordDeclared(string fullName)
    {
        if (string.IsNullOrEmpty(fullName)) return;
        try
        {
            for (int i = 0; i < _declared.Count; i++)
            {
                if (string.Equals(_declared[i], fullName, StringComparison.Ordinal)) return;   // 去重
            }
            _declared.Add(fullName);
        }
        catch (Exception ex) { CatchLogOnce.Warn("WarmupRegistry.RecordDeclared", ex); }
    }

    /// <summary>
    /// 登记一个"手动注册的补丁方法本体"（Prefix/Postfix）。
    /// <para>
    /// ★ 必须在 <c>harmony.Patch(...)</c> **成功之后**调用 —— 只有真正挂上的
    /// 补丁才需要预热（没挂上的方法不会被 detour，也就没有 CompileMethodHook 风险）。
    /// </para>
    /// <para>
    /// ★ 幂等：同一方法重复登记会被去重（多 Harmony 实例重装时可能走两次）。
    /// </para>
    /// </summary>
    internal static void Register(MethodInfo patchMethod)
    {
        if (patchMethod == null) return;
        try
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                if (_targets[i] == patchMethod) return;
            }
            _targets.Add(patchMethod);
        }
        catch (Exception ex) { CatchLogOnce.Warn("WarmupRegistry.Register", ex); }
    }

    /// <summary>当前已登记数量（供日志/自检）。</summary>
    internal static int Count
    {
        get { try { return _targets.Count; } catch { return 0; } }
    }

    /// <summary>
    /// 用安全哑参把每个登记的方法各 invoke 一次，强制其在当前（浅）栈完成 JIT 编译。
    /// 有副作用的方法由调用方在登记前自行排除，本类不替调用方判断。哑参规则与理由：见 docs/jit-warmup.md
    /// </summary>
    /// <returns>成功 invoke 的方法数（含 invoke 抛异常但已达成编译的）。</returns>
    internal static int WarmupAll()
    {
        int n = 0;
        // 记录实际预热到的方法名（声明式扫描后清单不再是源码可见，留一条可核对的账，受 Diag 闸门控制）。
        var names = new System.Text.StringBuilder();
        try
        {
            for (int i = 0; i < _targets.Count; i++)
            {
                var m = _targets[i];
                if (m == null) continue;
                try
                {
                    if (ContainerSuiteLog.DiagEnabled)
                    {
                        if (names.Length > 0) names.Append(", ");
                        names.Append(m.DeclaringType != null ? m.DeclaringType.Name + "." : "");
                        names.Append(m.Name);
                    }
                    var ps = m.GetParameters();
                    var args = new object[ps.Length];
                    for (int j = 0; j < ps.Length; j++)
                    {
                        var p = ps[j];
                        if (p.ParameterType.IsByRef)
                        {
                            var elem = p.ParameterType.GetElementType();
                            args[j] = (elem != null && elem.IsValueType) ? Activator.CreateInstance(elem) : null;
                        }
                        else if (p.ParameterType.IsValueType)
                            args[j] = Activator.CreateInstance(p.ParameterType);
                        else
                            args[j] = null;
                    }
                    m.Invoke(null, args);   // 编译发生在方法体执行前；即便抛 NRE 也已达成预热目标
                    n++;
                }
                catch { /* 编译已在执行前完成；任何异常一律忽略 */ }
            }
        }
        catch (Exception ex) { CatchLogOnce.Warn("WarmupRegistry.WarmupAll", ex); }
        try
        {
            if (ContainerSuiteLog.DiagEnabled && names.Length > 0)
                ContainerSuiteLog.Diag("[DIY-JIT] 预热清单：" + names);
        }
        catch { }
        return n;
    }
}

/// <summary>
/// 声明式 JIT 预热标记：打在方法上 = 声明"启动期请用哑参把我编译掉"，由 <see cref="WarmupRegistry.ScanAndRegister"/> 启动期扫描收集。
/// 完整契约（static / null 安全 / 必须是叶子）见 WarmupRegistry 的「声明式预热标记」段。详见 docs/jit-warmup.md
/// </summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
internal sealed class JitWarmupAttribute : Attribute
{
    /// <summary>可选说明（仅供读代码的人理解"为什么这个叶子必须预热"）。</summary>
    internal string Note { get { return _note; } }
    private readonly string _note;

    public JitWarmupAttribute() { _note = string.Empty; }
    public JitWarmupAttribute(string note) { _note = note ?? string.Empty; }
}
