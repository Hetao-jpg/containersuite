// [REG] 结构化回归标记（2026-10-07 建）。
// 一行一个实例，供 _regress\regress.ps1 机器解析；人也能直接读。
//   [REG]    id=<ascii> status=PASS|FAIL|NA detail=<任意文本>
//   [REG.BP] name=<ascii> detail=<任意文本>       ← 断点：关键状态转换处留痕
//
// 设计原则（与 MEMORY 一致）：
//   · id / name 一律 ASCII（避开编码坑），中文只进 detail。
//   · 只加日志，不改任何逻辑、不改任何判据、不改任何写权限。
//
// ★ 2026-10-09 修订：改为**受 DiagLog 总闸控制**（原先无条件输出）。
//   理由：[REG]/[REG.BP] 本质是诊断输出，默认常驻会把 Latest.log 撑成几十倍长度，
//     而"启动成功 / 告警 / 错误"才是玩家需要长期看到的东西。
//   连带影响：回归工具链必须开 DiagLog 才能拿到断言凭据 ——
//     regress.ps1 已在 -Launch 前自动把 cfg 的 z1_DiagLog 置 true（结束后还原），
//     checks.json 里依赖本标记的 22 条断言据此正常工作。

using System;

namespace ContainerSuite.Shared;

public static class RegMark
{
    /// <summary>断言：ok=true → PASS，否则 FAIL。</summary>
    public static void Mark(string id, bool ok, string detail = null)
        => Mark(id, ok ? "PASS" : "FAIL", detail);

    /// <summary>断言：显式给状态（含 NA = 不适用）。</summary>
    public static void Mark(string id, string status, string detail = null)
    {
        if (!ContainerSuiteLog.DiagEnabled) return;
        try
        {
            string line = "[REG] id=" + id + " status=" + status;
            if (!string.IsNullOrEmpty(detail)) line += " detail=" + detail;
            ContainerSuiteLog.Msg(line);
        }
        catch { }
    }

    /// <summary>断点：关键状态转换处留痕，供人工 / AI 事后断言。</summary>
    public static void Bp(string name, string detail = null)
    {
        if (!ContainerSuiteLog.DiagEnabled) return;
        try
        {
            string line = "[REG.BP] name=" + name;
            if (!string.IsNullOrEmpty(detail)) line += " detail=" + detail;
            ContainerSuiteLog.Msg(line);
        }
        catch { }
    }
}
