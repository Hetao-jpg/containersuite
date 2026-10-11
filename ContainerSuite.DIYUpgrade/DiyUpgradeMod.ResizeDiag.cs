// ═══════════════════════════════════════════════════════════════════════════
// 【2026-10-10 临时诊断 · 定性后整体删除本文件】
//
// 取证目标：拖拽手柄缩放容器时，**从上（TopLeft / TopRight）压缩**会出现网格残留，
//   而从左 / 右 / 左下 / 右下压缩正常。
//
// 待验证的核心假设：
//   GridFx.HideOutOfBoundsNodes 用**对称**判据 `|y| > winH*0.5f + 20f`
//   隐藏越界填充节点，其前提是「填充底节点围绕网格中心 y=0 分布」
//   （(w+2)*(h+2) 比格子多一圈、+20 对应多出的半格，可印证该设计意图）。
//   但游戏原生 SetShape 是 native 方法，缩小时**从固定一端销毁节点**；
//   销毁后剩余节点整体偏移、不再围绕 0 中心 ⇒ 对称判据失效：
//     · 与销毁端一致的 Bottom 方向（含 BottomLeft/BottomRight）→ 判据碰巧成立 → 正常
//     · 方向相反的 Top（TopLeft/TopRight）→ 误判 → 残留
//
// 判据：从上压一次、从下压一次，对比两份日志里的 yMin / yMax / yCenter：
//   · yCenter 明显偏离 0        ⇒ 假设成立（节点未围绕中心分布）
//   · 两个方向的 yCenter 偏向相反 ⇒ 进一步坐实"销毁固定一端"是方向性来源
//
// ★ 临时代码：定性后删除本文件 + Core.cs 的 `25_ResizeDiag` 条目 + State.cs 的
//   `_resizeDiag` 字段 + Resizer.cs 里的三处调用。全部以 "ResizeDiag" 检索即可。
//
// ★ P1-3（2026-10-10）：本文件已整体移入 `#if DIAG`，Release 产物不含
//   （DLL 里搜不到 "ResizeDiag" 字符串）。取证时以 -p:Diag=true 构建即恢复。
// ═══════════════════════════════════════════════════════════════════════════

#if DIAG
using System;
using System.Collections.Generic;
using System.Text;
using ContainerSuite.Shared;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using MelonLoader;
using UnityEngine;

namespace DIYUpgrade;

public static partial class DiyUpgradeMod
{
    private static bool _resizeDiagSessionLogged;
    private static bool _resizeDiagUseAltPath;
    private static DateTime _resizeDiagRetryAfter = DateTime.MinValue;

    /// <summary>会话首行标记：开开关 + 进游戏必有文件，避免"到底跑没跑"没线索。</summary>
    internal static void ResizeDiagSessionStart()
    {
        if (_resizeDiag == null || !_resizeDiag.Value) return;
        if (_resizeDiagSessionLogged) return;
        _resizeDiagSessionLogged = true;
        ResizeDiagLog($"=== ResizeDiag ACTIVE {DateTime.Now:yyyy-MM-dd HH:mm:ss} "
            + "(从上压一次 / 从下压一次，对比 yCenter 是否偏移 0) ===");
    }

    /// <summary>
    /// 写盘：路径 UserData/ContainerSuite.resizediag.txt；
    /// 主路径被锁（杀毒/瞬时占用）时切 .b，并冷却 1 秒后重试 —— 与 R1Probe 同一套兜底。
    /// </summary>
    private static void ResizeDiagLog(string line)
    {
        try
        {
            if (DateTime.UtcNow < _resizeDiagRetryAfter) return;
            string basePath = System.IO.Path.Combine(
                MelonLoader.Utils.MelonEnvironment.UserDataDirectory, "ContainerSuite.resizediag.txt");
            string path = _resizeDiagUseAltPath ? basePath + ".b" : basePath;
            try
            {
                System.IO.File.AppendAllText(path, line + "\n");
            }
            catch
            {
                if (!_resizeDiagUseAltPath)
                {
                    _resizeDiagUseAltPath = true;
                    System.IO.File.AppendAllText(basePath + ".b", line + "\n");
                }
                else throw;
            }
        }
        catch { _resizeDiagRetryAfter = DateTime.UtcNow.AddSeconds(1); }
    }

    /// <summary>
    /// 采集一次网格状态快照。
    /// </summary>
    /// <param name="stage">阶段标记：DRAGGING（收尾前，IsDragging=true）/ FINISH（收尾后）/ POST（延迟协程后）。</param>
    /// <param name="edge">本次拖拽的边（判定方向性用）。</param>
    internal static void ResizeDiagDump(GameItem item, string stage, string edge)
    {
        if (_resizeDiag == null || !_resizeDiag.Value) return;
        try
        {
            List<GameGridInventory> grids = GetAllGridInventories(item);
            if (grids == null || grids.Count == 0) return;

            var sb = new StringBuilder();
            sb.Append($"--- {stage} edge={edge} t={DateTime.Now:HH:mm:ss.fff} "
                + $"item={(item?.identifier ?? "?")} dragging={ContainerWindowResizer.IsDragging}\n");

            for (int gi = 0; gi < grids.Count; gi++)
            {
                GameGridInventory g = grids[gi];
                if (g?.inventoryShape == null) continue;

                int w = 0, h = 0;
                try { w = g.inventoryShape.width; h = g.inventoryShape.height; } catch { }
                int winH = 0;
                try { winH = g.heightPixels; } catch { }
                int winW = 0;
                try { winW = g.widthPixels; } catch { }

                // ── 采集填充底节点的 y 分布（核心判据）──
                float yMin = float.MaxValue, yMax = float.MinValue;
                // 【2026-10-10】x 分布：核验 X 轴判据约定（内容向右、左边框 -colW、右边框 +winW）
                float xMin = float.MaxValue, xMax = float.MinValue;
                int total = 0, hiddenBySym = 0;
                var rows = new Dictionary<int, int>();
                for (int k = 0; k < 5; k++)
                {
                    var list = GridFx.MeshList(g, k);
                    if (list == null) continue;
                    for (int i = 0; i < list.Count; i++)
                    {
                        TreeNodeRender n = list[i];
                        RectTransform rt = (n == null) ? null : n.rectTransform;
                        if (rt == null) continue;
                        float y = rt.anchoredPosition.y;
                        float x = rt.anchoredPosition.x;
                        if (y < yMin) yMin = y;
                        if (y > yMax) yMax = y;
                        if (x < xMin) xMin = x;
                        if (x > xMax) xMax = x;
                        total++;
                        int yi = Mathf.RoundToInt(y);
                        rows.TryGetValue(yi, out int c);
                        rows[yi] = c + 1;
                    }
                }

                float halfH = winH * 0.5f + 20f;
                foreach (var kv in rows)
                    if (kv.Key > halfH || kv.Key < -halfH) hiddenBySym += kv.Value;

                float yCenter = (total > 0) ? (yMin + yMax) * 0.5f : 0f;

                int rowMin = int.MaxValue, rowMax = 0;
                foreach (var kv in rows)
                {
                    if (kv.Value < rowMin) rowMin = kv.Value;
                    if (kv.Value > rowMax) rowMax = kv.Value;
                }
                if (rows.Count == 0) rowMin = 0;

                // ── 填充底健康判定（决定收尾走不走销毁重建）──
                int expectedBg = (w + 2) * (h + 2);
                int actualBg = 0;
                try { actualBg = g.dynamicBackgrounds?.Count ?? 0; } catch { }
                bool healthy = GridFx.FillHealthyByCount(g);

                sb.Append($"  grid[{gi}] shape={w}x{h} heightPixels={winH}\n");
                sb.Append($"    nodes={total} rows={rows.Count} perRow={rowMin}~{rowMax}\n");
                // ★★ 核心：yCenter 应≈0（节点围绕网格中心）。明显偏离即对称判据前提失效。
                sb.Append($"    yMin={yMin:F1} yMax={yMax:F1} yCenter={yCenter:F1}  (对称判据 halfH={halfH:F1})\n");
                sb.Append($"    xMin={xMin:F1} xMax={xMax:F1}  (X 判据: 合法 [-colW, +winW], winW={winW})\n");
                sb.Append($"    按对称判据会隐藏 {hiddenBySym} 个节点\n");
                sb.Append($"    FillHealthyByCount: expectedBg={expectedBg} actualBg={actualBg} "
                    + $"diff={Math.Abs(actualBg - expectedBg)} healthy={healthy}"
                    + (healthy ? "  ← 跳过销毁重建，只 ApplyMeshAlpha" : "  ← 走销毁重建") + "\n");
            }

            ResizeDiagLog(sb.ToString());
        }
        catch { /* 诊断绝不能影响功能 */ }
    }
}
#endif
