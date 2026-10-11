# ============================================================
#  verify-s9.ps1 -- S9「读档窗口 / 页归属自愈」实机验收
#
#  与 regress.ps1 的分工：
#    · regress.ps1   Tier-1：只到主菜单（不读档），验启动链路与补丁挂载。
#    · 本脚本        吃【读档态】的日志，验 S9 真正要修的那条路径 ——
#                    "读档后多久能换页" 与 "第二页物品是否还叠在第一页"。
#    ⇒ 用法是：先 regress -Deploy -Launch，**手动读一次档**，再跑本脚本。
#
#  Usage:
#    powershell -File _regress\verify-s9.ps1
#    powershell -File _regress\verify-s9.ps1 -LogPath "<游戏>\MelonLoader\Logs\2026-10-07_12-10-00.log"
#
#  退出码：0 = 无 FAIL；1 = 有 FAIL。
# ============================================================

param(
    [string]$Root,
    [string]$GamePath,
    [string]$LogPath,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib.ps1')

if (-not $Root)     { $Root = Split-Path -Parent $PSScriptRoot }
if (-not $GamePath) { $GamePath = Get-GamePath -Root $Root }
if (-not $LogPath)  { $LogPath = Get-LogPath -GamePath $GamePath }

$rows = New-Object System.Collections.ArrayList
function Add-Row([string]$status, [string]$id, [string]$desc, [string]$detail) {
    [void]$rows.Add(@{ status = $status; id = $id; desc = $desc; detail = $detail })
}
function Say([string]$m) { if (-not $Quiet) { Write-Host $m } }

# 日志行首时间戳 -> TimeSpan（Latest.log 每行自带 [HH:MM:SS.mmm]）
function Get-LineTs([string]$line) {
    $m = [regex]::Match($line, '^\[(\d\d):(\d\d):(\d\d)\.(\d+)\]')
    if (-not $m.Success) { return $null }
    $frac = $m.Groups[4].Value.PadRight(3, '0')
    if ($frac.Length -gt 3) { $frac = $frac.Substring(0, 3) }
    return (New-Object System.TimeSpan(0, [int]$m.Groups[1].Value, [int]$m.Groups[2].Value, [int]$m.Groups[3].Value, [int]$frac))
}

Say ('verify-s9: log = ' + $LogPath)
$lines = Read-LogLines -Path $LogPath
if ($lines.Count -eq 0) { Say 'FAIL: 日志为空或不存在'; exit 1 }

# ────────── 1. 定位最后一次读档 ──────────
$idxLoad = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match 'WorldReadyGate 关门（CI\.LoadGame）') { $idxLoad = $i }
}

if ($idxLoad -lt 0) {
    Add-Row 'FAIL' 's9.loaded' '本次日志没有任何读档记录' '先读一次档，再跑本脚本（regress.ps1 不读档）'
} else {
    $tLoad = Get-LineTs $lines[$idxLoad]
    Add-Row 'PASS' 's9.loaded' '找到读档记录' ('行 ' + ($idxLoad + 1))

    # 读档后首次放行
    $firstOpen = $null
    for ($i = $idxLoad; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match 'WorldReadyGate 开门') { $firstOpen = $lines[$i]; break }
    }
    if ($firstOpen) {
        $tOpen = Get-LineTs $firstOpen
        $d = ''
        if ($tLoad -and $tOpen) { $d = ('LoadGame +' + [Math]::Round(($tOpen - $tLoad).TotalSeconds, 3) + 's') }
        Add-Row 'PASS' 's9.gate' '读档后首次放行' $d
    } else {
        Add-Row 'FAIL' 's9.gate' '读档后没有见到门禁放行' 'WorldReadyGate 未开门 = 后续全都不成立'
    }
}

# ────────── 2. 绑定时刻（S9 的核心指标） ──────────
$bindLines = @($lines | Where-Object { $_ -match '\[REG\] id=sbe\.bind ' })
$atBind = $null
$slotId = -1
if ($bindLines.Count -eq 0) {
    Add-Row 'FAIL' 'sbe.bind' '没有 sbe.bind 标记（分页未绑定）' '读档后 1 秒内应出现；缺失=绑定链没跑到'
} else {
    $last = $bindLines[$bindLines.Count - 1]
    $mb = [regex]::Match($last, 'at=([0-9\.]+)')
    if ($mb.Success) { $atBind = [double]$mb.Groups[1].Value }
    $ms = [regex]::Match($last, 'slot=(-?\d+)')
    if ($ms.Success) { $slotId = [int]$ms.Groups[1].Value }

    $tBind = Get-LineTs $last
    $winSec = $null
    if ($tLoad -and $tBind) { $winSec = [Math]::Round(($tBind - $tLoad).TotalSeconds, 2) }

    $desc = '绑定距门禁首次放行 ' + $atBind + 's'
    if ($null -ne $winSec) { $desc += ('；读档后 ' + $winSec + 's 即可换页') }
    if ($atBind -le 1.0)      { Add-Row 'PASS' 'sbe.bind' $desc '目标 <=1.0（旧版 3.985）' }
    elseif ($atBind -le 2.5)  { Add-Row 'WARN' 'sbe.bind' $desc '比目标慢，检查是否有别的 mod 拖住门禁' }
    else                      { Add-Row 'FAIL' 'sbe.bind' $desc 'S9 没生效（或回退到了 LastReady 口径）' }
}

# ────────── 3. 页归属自愈 ──────────
$recLines = @($lines | Where-Object { $_ -match '\[REG\] id=sbe\.reconcile ' })
$moved = 0
$atRec = $null
if ($recLines.Count -gt 0) {
    $lastRec = $recLines[$recLines.Count - 1]
    $mm = [regex]::Match($lastRec, 'moved=(\d+)')
    if ($mm.Success) { $moved = [int]$mm.Groups[1].Value }
    $ma = [regex]::Match($lastRec, 'at=([0-9\.]+)')
    if ($ma.Success) { $atRec = [double]$ma.Groups[1].Value }
}

# 旁路文件里的"预期外页件数"（slot;pageCount;页2IDs;页3IDs…）
$expected = -1
$idsPath = Join-Path $GamePath 'UserData\SaveBagPaging.ids'
if (Test-Path $idsPath) {
    $idsTxt = [System.IO.File]::ReadAllText($idsPath, (New-Object System.Text.UTF8Encoding($false)))
    foreach ($ln in ($idsTxt -split "`r?`n")) {
        $ln = $ln.Trim()
        if ($ln -eq '' -or $ln.StartsWith('#')) { continue }
        $f = $ln -split ';'
        if ($slotId -ge 0 -and $f[0] -ne [string]$slotId) { continue }
        $expected = 0
        for ($k = 2; $k -lt $f.Count; $k++) {
            foreach ($idv in ($f[$k] -split ',')) {
                if ($idv.Trim() -ne '') { $expected++ }
            }
        }
        break
    }
}

if ($moved -gt 0) {
    $d = 'moved=' + $moved + (', at=' + $atRec + 's')
    if ($expected -ge 0) { $d += ('；ids 文件里页2+共 ' + $expected + ' 件') }
    if ($expected -ge 0 -and $moved -eq $expected) { Add-Row 'PASS' 'sbe.reconcile' '外页物品已全部归位' $d }
    elseif ($expected -ge 0) { Add-Row 'WARN' 'sbe.reconcile' '归位件数与 ids 文件不一致（可能有物品在存档里丢了）' $d }
    else { Add-Row 'PASS' 'sbe.reconcile' '外页物品已归位（无法比对 ids 文件）' $d }
} else {
    if ($expected -gt 0) {
        Add-Row 'FAIL' 'sbe.reconcile' ('没有自愈记录，但 ids 文件说本槽应外移 ' + $expected + ' 件') '第二页物品仍会叠在第一页'
    } else {
        Add-Row 'INFO' 'sbe.reconcile' '无自愈记录（本槽没有第二页物品 / ids 文件无记录）' '不适用，非失败'
    }
}

# ────────── 4. 硬性健康判据 ──────────
$fatal = @($lines | Where-Object { $_ -match 'Fatal error|0x80131506|Internal CLR error' })
if ($fatal.Count -gt 0) {
    Add-Row 'FAIL' 'no.fatal' ('出现 CRL 级致命错误 ' + $fatal.Count + ' 次') $fatal[0]
} else {
    Add-Row 'PASS' 'no.fatal' '无 0x80131506 / Fatal error' ''
}

$probe = @($lines | Where-Object { $_ -match '\[SBE-JIT\] Prep ' })
if ($probe.Count -gt 0) {
    Add-Row 'FAIL' 'no.jitprobe' '组 5 被触发（默认应为关闭）' $probe[0]
} else {
    Add-Row 'PASS' 'no.jitprobe' '未触发 PrepareMethod 全量探针' ''
}

$errs = @($lines | Where-Object { $_ -match '\[ERROR\].*(CustomInfrastructure|SaveBagExpand|DIYUpgrade|ContainerSuite)' })
if ($errs.Count -gt 0) {
    Add-Row 'FAIL' 'no.suite.error' ('本套件 ERROR ' + $errs.Count + ' 条') $errs[0]
} else {
    Add-Row 'PASS' 'no.suite.error' '本套件无 [ERROR]' ''
}

$los = @($lines | Where-Object { $_ -match '存档合并丢失|存档警告' })
if ($los.Count -gt 0) {
    Add-Row 'FAIL' 'no.mergeloss' '出现「存档合并丢失」告警' $los[0]
} else {
    Add-Row 'PASS' 'no.mergeloss' '无存档合并丢失告警' ''
}

# SBE 单帧峰值（取读档之后的全部 Perf 行，看最大一次）
$peak = 0
foreach ($l in $lines) {
    $mp = [regex]::Match($l, '峰值：Arb=(\d+) SBE=(\d+)')
    if ($mp.Success) { $v = [int]$mp.Groups[2].Value; if ($v -gt $peak) { $peak = $v } }
}
if ($peak -le 0) {
    Add-Row 'INFO' 'sbe.perf' '日志里没有 Perf 行（DiagLog 关闭时不打）' ''
} elseif ($peak -lt 20000) {
    Add-Row 'PASS' 'sbe.perf' ('SBE 单帧峰值 ' + $peak + 'us（旧版 100019us）') '尖刺已消除'
} elseif ($peak -lt 60000) {
    Add-Row 'WARN' 'sbe.perf' ('SBE 单帧峰值 ' + $peak + 'us') '仍偏高，看 [Perf] 上下文定位'
} else {
    Add-Row 'FAIL' 'sbe.perf' ('SBE 单帧峰值 ' + $peak + 'us，未见改善') '读档全量重放还在跑（Bind 走了 shapeChanged 分支？）'
}

# ────────── 汇总 ──────────
$pass = @($rows | Where-Object { $_.status -eq 'PASS' }).Count
$fail = @($rows | Where-Object { $_.status -eq 'FAIL' }).Count
$warn = @($rows | Where-Object { $_.status -eq 'WARN' }).Count

Say ''
foreach ($r in $rows) {
    Say (('{0,-4} {1,-16} {2} {3}' -f $r.status, $r.id, $r.desc, $r.detail))
}
Say ''
Say ('verify-s9: PASS=' + $pass + ' FAIL=' + $fail + ' WARN=' + $warn)

$outDir = Join-Path $Root '_regress\out'
if (-not (Test-Path $outDir)) { New-Item -ItemType Directory -Path $outDir -Force | Out-Null }
$stamp = (Get-Date).ToString('yyyyMMdd-HHmmss')
$html = New-ReportHtml -Title 'S9 分页读档窗口 · 实机验收' `
    -Subtitle ('日志 ' + $LogPath + ' ｜ ' + (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')) `
    -Stats @{ 'PASS' = $pass; 'FAIL' = $fail; 'WARN' = $warn; 'SBE峰值us' = $peak } `
    -Rows $rows -Footer @('判据：sbe.bind.at <=1.0s；无 0x80131506；sbe.reconcile.moved == ids 文件里页2+件数')
Write-Utf8 -Path (Join-Path $outDir ('s9-' + $stamp + '.html')) -Text $html -Bom
Say ('report: ' + (Join-Path $outDir ('s9-' + $stamp + '.html')))

if ($fail -gt 0) { exit 1 }
exit 0
