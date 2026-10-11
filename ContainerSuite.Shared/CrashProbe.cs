// ═══════════════════════════════════════════════════════════════════════════
// CrashProbe —— 崩溃现场心跳探针（2026-10-10 读档闪退定位）
//
// ── 要解决的取证死角 ──────────────────────────────────────────────────────
//   症状：读档后约 2 秒必崩
//     Fatal error. Internal CLR error. (0x80131506)
//     at MonoMod.RuntimeDetour.Platforms.DetourRuntimeNETCore30Platform.CompileMethodHook
//   —— CLR fatal 的性质：**托管 catch 抓不到、MelonLoader 日志戛然而止**。
//   崩溃前最后一行永远是别的 mod 的噪音（ProbablyOptimized 帧末视觉），
//   无法判断是本套件哪一步崩的。
//
// ── 本探针 ────────────────────────────────────────────────────────────────
//   在 Mod.OnUpdate 的每个子 Tick 前后记录「当前进入的步骤」到一个**极小文本
//   文件**。崩溃后读该文件 = 最后进入但未退出（或最后进入）的那一步 = 凶手。
//
//   ★ 为什么不写 MelonLogger：CLR fatal（0x80131506）发生时进程直接终止，
//     MelonLoader 的日志缓冲/写入器可能已失效 —— 这正是"日志戛然而止"的根因。
//     直接写文件（File.WriteAllText，O(1) 覆盖）不依赖任何托管基础设施，
//     崩溃前最后一次写一定已落盘（大小 < 64 字节，远小于页缓存原子性边界）。
//
//   ★ 为什么只在"读档后窗口"内记录：全程记录 = 每帧一次磁盘写，不可接受。
//     平时零开销（一个 bool 判断）。窗口 = 读档触发后 CrashProbeWindowSeconds 秒，
//     足够覆盖"读档后 2 秒崩"的观察窗口。
//
//   ★ 为什么不用时间戳命名/追加：崩溃是**多次复现**的，每次都覆盖同一个
//     probe 文件即可 —— 我们要的是"最后一次崩溃前在哪一步"，不是历史。
// ═══════════════════════════════════════════════════════════════════════════
//
// ★ P1-3（2026-10-10）：本文件实现已整体移入 `#if DIAG`。
//   Release 产物不含探针实现（也无 "ContainerSuite.crashprobe.txt" 等字符串），
//   只保留文件末尾的**极简空实现**以便调用点零改动。开启见 _shared\CommonMod.props。

#if DIAG
using System;
using System.IO;
using MelonLoader.Utils;

namespace ContainerSuite.Shared;

/// <summary>
/// 崩溃现场心跳探针。全部静态、无实例状态、零常驻开销。
/// </summary>
public static class CrashProbe
{
    /// <summary>武装窗口时长（秒）。读档后此窗口内逐 Tick 记录。</summary>
    public const float CrashProbeWindowSeconds = 15f;

    private static string _probePath;
    private static float _armedUntil = -1f;   // realtimeSinceStartup 截止点
    // ★★【2026-10-10】写盘失败的处理：从「一次性永久放弃（_failed）」改为「冷却重试 + 主备路径切换」。
    //   实测教训（10:03 那轮崩给探针 0 字节）：第一次 Write 失败（大概率 = 杀毒实时扫描
    //   锁住新建文件，本机 360/Defender/卡巴三家）→ 旧代码 _failed=true → 之后 Arm/Enter/Exit
    //   **全程静默短路** → 崩溃后探针文件 0 字节、彻底失明。
    //   —— 这正是"一次性锁在异步资源上是错的"铁律的又一次应验：杀毒锁是**暂时**的，
    //   必须冷却重试；且主路径被长期锁住时应切换备用路径。
    private static float _retryAfter;          // 写盘失败后的冷却截止（realtimeSinceStartup），0 = 可写
    private static bool _useAltPath;           // true = 主路径持续失败，改写备用路径（主路径名 + ".b"）

    /// <summary>探针文件绝对路径（日志里可查）。</summary>
    public static string ProbePath
    {
        get
        {
            if (_probePath == null)
            {
                try { _probePath = Path.Combine(MelonEnvironment.UserDataDirectory, "ContainerSuite.crashprobe.txt"); }
                catch { _probePath = "ContainerSuite.crashprobe.txt"; }
            }
            return _probePath;
        }
    }

    /// <summary>
    /// 由 Mod.OnSceneWasLoaded / LoadGame 之后调用，武装探针窗口。
    /// 便宜（只是设一个截止时间），可在任意时机调。
    /// </summary>
    public static void Arm(string reason)
    {
        try
        {
            _armedUntil = UnityEngine.Time.realtimeSinceStartup + CrashProbeWindowSeconds;
            // 新一轮读档：清空上一轮的历史，避免旧序列混淆判读。
            _histCount = 0; _histNext = 0;
            Write("ARM(" + reason + ")");
        }
        catch { /* 武装失败不影响主流程 */ }
    }

    /// <summary>每帧由 OnUpdate 开头调用：窗口过期则解除武装。</summary>
    public static void TickFrame(ref bool armed)
    {
        if (!armed) return;
        try
        {
            if (UnityEngine.Time.realtimeSinceStartup > _armedUntil)
            {
                Write("DISARM(窗口结束)");
                armed = false;
                _armedUntil = -1f;   // 复位 → Enter/Exit 立即短路，不再逐帧刷盘
            }
        }
        catch { armed = false; _armedUntil = -1f; }
    }

    /// <summary>记录"即将进入某步骤"。崩溃后最后一条进入即凶手。</summary>
    public static void Enter(string step) { if (_armedUntil < 0f) return; Write("-> " + step); }

    /// <summary>记录"已离开某步骤"。</summary>
    public static void Exit(string step) { if (_armedUntil < 0f) return; Write("<- " + step); }

    // ── 2026-10-10：从「单行覆盖」升级为「最近 N 步滚动快照」──
    //   原因：单行覆盖只能看到"最后进入的步骤"，看不出它是"第几个容器/哪一轮"，
    //   也看不出紧邻的前后步骤。改为在内存里保留最近 N 条，每次覆盖写整块。
    //   仍是 O(1) 覆盖写（块大小 < 2KB，远小于页缓存原子边界），崩溃前最后一次
    //   写必定已落盘；且不追加历史、不增长文件，避免长期刷盘。
    private const int MaxHistory = 16;
    private static readonly string[] _hist = new string[MaxHistory];
    private static int _histCount;
    private static int _histNext;
    private static int _seq;   // 递增序号（判读"最后一步离崩溃多远"）

    private static void Write(string text)
    {
        float now = UnityEngine.Time.realtimeSinceStartup;
        if (now < _retryAfter) return;   // 冷却期内：静默跳过（**不放弃**，冷却结束自动恢复）
        try
        {
            int frame = 0;
            try { frame = UnityEngine.Time.frameCount; } catch { }
            _hist[_histNext] = "#" + (_seq++) + " f" + frame + " " + text;
            _histNext = (_histNext + 1) % MaxHistory;
            if (_histCount < MaxHistory) _histCount++;

            // 按时间顺序拼出最近 _histCount 条
            var sb = new System.Text.StringBuilder(512);
            int start = (_histCount < MaxHistory) ? 0 : _histNext;
            for (int i = 0; i < _histCount; i++)
            {
                int idx = (start + i) % MaxHistory;
                if (_hist[idx] != null) sb.Append(_hist[idx]).Append('\n');
            }
            // 主路径持续失败时切备用路径（杀毒对同一文件的锁往往比对"另一个新文件"松）
            string path = _useAltPath ? ProbePath + ".b" : ProbePath;
            try
            {
                File.WriteAllText(path, sb.ToString());
            }
            catch
            {
                if (!_useAltPath) { _useAltPath = true; File.WriteAllText(ProbePath + ".b", sb.ToString()); }
                else throw;   // 备用也失败 → 走外层冷却
            }
        }
        catch { _retryAfter = now + 1f; }   // 失败冷却 1 秒后重试（杀毒/瞬时锁会消失）
    }
}
#else
// ── Release 极简入口（P1-3）────────────────────────────────────────────────
// 上面的探针实现整段移出产物；这里只留同名同签名的空实现，且每个方法都标
// [Conditional("DIAG")] —— 编译器在未定义 DIAG 时会把**整条调用（含实参表达式）**
// 删除，所以各调用点一行都不用改，Release 里也没有探针的步骤名字符串、零运行时开销。
// 需要取证时按 _shared\CommonMod.props 的说明以 -p:Diag=true 构建，本文件即切回上面的实现。
namespace ContainerSuite.Shared;

public static class CrashProbe
{
    [System.Diagnostics.Conditional("DIAG")] public static void Arm(string reason) { }
    [System.Diagnostics.Conditional("DIAG")] public static void TickFrame(ref bool armed) { }
    [System.Diagnostics.Conditional("DIAG")] public static void Enter(string step) { }
    [System.Diagnostics.Conditional("DIAG")] public static void Exit(string step) { }
}
#endif
