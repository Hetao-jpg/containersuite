// ═══════════════════════════════════════════════════════════════════════════
// 【2026-10-10 P2 自检器】验证「声明式 [JitWarmup] 预热清单」在**产物 DLL** 里完整。
//
// 为什么需要它：P2 把 8 行手抄的 Step(...) 硬编码清单换成了方法头上的 [JitWarmup]
//   特性 + 启动期反射扫描（WarmupRegistry.ScanAndRegister）。
//   这次改造的**唯一失效模式**是：扫描器一个都没扫到（反射标志写错 / 特性没进产物）
//   —— 那样会静默退化成"8 个叶子全部不再预热"，而它们恰恰是防 CLR fatal 的那一批。
//   编译通过 ≠ 扫描得到，所以必须在产物上实测。
//
// 做法：用 Mono.Cecil 静态读 DLL（不加载、不需要游戏依赖），
//   按与 ScanAndRegister **完全相同**的筛选条件枚举：
//     Static | Public | NonPublic | DeclaredOnly，且非泛型。
//
// 用法：VerifyJitWarmup <dll路径> [期望的方法名,...]
//   退出码 0 = 清单完整；非 0 = 缺失/多余（CI 或人工卡口用）。
// ═══════════════════════════════════════════════════════════════════════════

using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;

internal static class Program
{
    private const string AttrName = "JitWarmupAttribute";

    private static int Main(string[] args)
    {
        if (args.Length < 1)
        {
            Console.Error.WriteLine("用法: VerifyJitWarmup <dll路径> [期望方法名,逗号分隔]");
            return 2;
        }

        string dll = args[0];
        var expected = new List<string>();
        if (args.Length >= 2)
            expected = args[1].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                              .Select(s => s.Trim()).Where(s => s.Length > 0).ToList();

        AssemblyDefinition asm;
        try
        {
            asm = AssemblyDefinition.ReadAssembly(dll, new ReaderParameters { InMemory = true });
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("读取 DLL 失败: " + ex.Message);
            return 2;
        }

        using (asm)
        {
            var found = new List<string>();

            // ★ 必须递归进 NestedTypes —— Mono.Cecil 的 ModuleDefinition.Types
            //   只给**顶层**类型，而 ContainerWindowResizer 是 DiyUpgradeMod 的嵌套类。
            //   （反射 API 的 Assembly.GetTypes() 会连带返回嵌套类型，
            //    所以产品侧 ScanAndRegister 看不到这个坑 —— 但本自检器必须自己走一遍，
            //    否则会像首版那样把已声明的 Attach 误报成"缺失"。）
            void Walk(IEnumerable<TypeDefinition> types)
            {
                if (types == null) return;
                foreach (var type in types)
                {
                    if (type == null) continue;

                    IList<MethodDefinition> methods = null;
                    try { methods = type.Methods; } catch { methods = null; }
                    if (methods != null)
                    {
                        foreach (var m in methods)
                        {
                            if (m == null) continue;

                            // ── 与 WarmupRegistry.ScanAndRegister 完全一致的筛选 ──
                            if (!m.IsStatic) continue;                  // BindingFlags.Static
                                                                        // Public|NonPublic 在 Cecil 里无需过滤
                            if (m.IsGenericInstance || m.HasGenericParameters) continue;  // 泛型跳过

                            if (!m.HasCustomAttributes) continue;
                            bool marked = false;
                            foreach (var a in m.CustomAttributes)
                            {
                                if (a != null && a.AttributeType != null
                                    && string.Equals(a.AttributeType.Name, AttrName, StringComparison.Ordinal))
                                {
                                    marked = true;
                                    break;
                                }
                            }
                            if (!marked) continue;

                            found.Add(type.Name + "." + m.Name);
                        }
                    }

                    // 递归嵌套类型
                    try { if (type.HasNestedTypes) Walk(type.NestedTypes); } catch { }
                }
            }

            Walk(asm.MainModule.Types);

            found.Sort(StringComparer.Ordinal);

            Console.WriteLine("=== [JitWarmup] 声明清单（产物实测）===");
            foreach (var f in found) Console.WriteLine("  " + f);
            Console.WriteLine("合计: " + found.Count + " 个");

            if (expected.Count == 0)
            {
                Console.WriteLine("(未给出期望清单 —— 仅列举，不判定)");
                return 0;
            }

            var shortNames = found.Select(f => f.Substring(f.IndexOf('.') + 1)).ToList();
            var missing = expected.Where(e => !shortNames.Contains(e)).ToList();
            var extra = shortNames.Where(s => !expected.Contains(s)).ToList();

            bool ok = missing.Count == 0 && extra.Count == 0;

            Console.WriteLine();
            Console.WriteLine("=== 比对（期望 " + expected.Count + " 个）===");
            if (missing.Count > 0)
                Console.WriteLine("  [缺失] " + string.Join(", ", missing));
            if (extra.Count > 0)
                Console.WriteLine("  [多余] " + string.Join(", ", extra));
            if (ok)
                Console.WriteLine("  OK —— 声明式预热清单完整");

            return ok ? 0 : 1;
        }
    }
}
