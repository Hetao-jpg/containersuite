# ============================================================
#  regress.ps1 -- ContainerSuite Tier-1 fast regression.
#
#  Flow:  [deploy check] -> [launch game + wait] -> [assert] -> report
#
#  Usage:
#    # assert against the CURRENT Latest.log (game already open)
#    powershell -File _regress\regress.ps1
#
#    # deploy the latest Release build, then assert
#    powershell -File _regress\regress.ps1 -Deploy
#
#    # deploy + launch the game + wait for load, then assert
#    powershell -File _regress\regress.ps1 -Deploy -Launch
#
#    # custom settle / timeout
#    powershell -File _regress\regress.ps1 -Launch -SettleSec 10 -TimeoutSec 120
#
#  Exit code: 0 = all pass, 1 = at least one FAIL.
# ============================================================

param(
    [string]$Root,
    [string]$GamePath,
    [string]$LogPath,
    [string]$Checks,
    [switch]$Deploy,
    [switch]$Launch,
    [int]$SettleSec = 8,
    [int]$TimeoutSec = 120,
    [switch]$NoOpen,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib.ps1')
. (Join-Path $PSScriptRoot 'diaglog.ps1')

if (-not $Root)     { $Root = Split-Path -Parent $PSScriptRoot }
if (-not $Checks)   { $Checks = Join-Path $PSScriptRoot 'checks.json' }
$outDir = Join-Path $Root '_regress\out'
if (-not $GamePath) { $GamePath = Get-GamePath -Root $Root }
if (-not $GamePath) { throw 'GamePath not found (pass -GamePath or set _shared\CommonMod.props).' }
if (-not $LogPath)  { $LogPath = Get-LogPath -GamePath $GamePath }

function Say([string]$m) { if (-not $Quiet) { Write-Host $m } }
Say ('regress: game = ' + $GamePath)

$rows = New-Object System.Collections.ArrayList
function Add-Row([string]$status, [string]$id, [string]$desc, [string]$detail) {
    [void]$rows.Add(@{ status = $status; id = $id; desc = $desc; detail = $detail })
}

# ---------- suite version ----------
$suiteVersion = $null
$svPath = Join-Path $Root 'ContainerSuite.Shared\SuiteVersion.cs'
if (Test-Path $svPath) {
    $svText = [System.IO.File]::ReadAllText($svPath, (New-Object System.Text.UTF8Encoding($false)))
    $m = [regex]::Match($svText, 'Suite\s*=\s*"([^"]+)"')
    if ($m.Success) { $suiteVersion = $m.Groups[1].Value }
}
if (-not $suiteVersion) { $suiteVersion = '(unknown)' }
Add-Row 'INFO' 'suite.version' ('SuiteVersion.cs = ' + $suiteVersion) ''

# ---------- deploy target: resolve the Mods file name MelonLoader actually loads ----------
# ★ MelonLoader 按【文件名】加载，与 AssemblyName 无关；写错名 → 静默不加载（无任何日志）。
#   历史上用过两个文件名：中文「自定义容器套件.dll」(约 2026-10-08 前) 与英文
#   「CustomInfrastructure.dll」(2026-10-09 实测线上加载的就是它，见 Latest.log
#   "Assembly: CustomInfrastructure.dll")。两个文件若同时在 Mods，会被 MelonLoader
#   当成两个 mod 各加载一次 → 重复打补丁；所以只沿用【当前在用的那一个】：
#   已存在的文件优先（顺带避免在 Mods 里新建文件、撞沙箱限制）。
$cnPath = Join-Path $GamePath 'Mods\自定义容器套件.dll'
$enPath = Join-Path $GamePath 'Mods\CustomInfrastructure.dll'
if ((Test-Path $cnPath) -and (Test-Path $enPath)) {
    Add-Row 'WARN' 'deploy.name' 'Mods 下两个候选文件名同时存在，可能被重复加载' '自定义容器套件.dll + CustomInfrastructure.dll'
}
if (Test-Path $cnPath) { $dst = $cnPath } elseif (Test-Path $enPath) { $dst = $enPath } else { $dst = $enPath }

# ---------- optional deploy ----------
$deployOk = $true
if ($Deploy) {
    $src = Join-Path $Root 'ContainerSuite.Infrastructure\bin\Release\CustomInfrastructure.dll'
    if (-not (Test-Path $src)) {
        $deployOk = $false
        Add-Row 'FAIL' 'deploy' '未找到 Release 产物，请先构建' $src
    } else {
        $hs = Get-FileSha256 -Path $src
        Copy-Item -Path $src -Destination $dst -Force
        $hd = Get-FileSha256 -Path $dst
        if ($hs -eq $hd) {
            Add-Row 'PASS' 'deploy' ('已部署 ' + (Split-Path $dst -Leaf) + ' (' + (Get-Item $dst).Length + 'B)') $hs.Substring(0,16)
        } else {
            $deployOk = $false
            Add-Row 'FAIL' 'deploy' '部署后哈希不一致' ('src=' + $hs + ' dst=' + $hd)
        }
    }
} else {
    $src = Join-Path $Root 'ContainerSuite.Infrastructure\bin\Release\CustomInfrastructure.dll'
    if ((Test-Path $src) -and (Test-Path $dst)) {
        if ((Get-FileSha256 $src) -eq (Get-FileSha256 $dst)) {
            Add-Row 'PASS' 'deploy.drift' ('已部署 ' + (Split-Path $dst -Leaf) + ' 与 Release 产物一致') ''
        } else {
            Add-Row 'WARN' 'deploy.drift' '已部署 DLL 落后于 Release 产物（未部署）' '加 -Deploy 可自动部署'
        }
    }
}

# ---------- DiagLog gate ----------
# ★ 2026-10-09：套件已把全部诊断输出收进 DiagLog 总闸（[REG]/[REG.BP]、JIT 逐步
#   打点、分页诊断 dump、偏移解析、WorldReadyGate 事件、cfg 自检提示…）。
#   而 checks.json 里 22 条断言消费的正是这些行 ⇒ 回归必须在开关打开的前提下跑，
#   否则「标记缺失」会被误判成「功能没执行」。
#
#   仅在 -Launch（本次启动由我们负责）时自动开关并在结束时还原 —— 因为游戏要读到
#   磁盘上的值。若游戏已在运行（跳过启动），开关改了对本次日志无效，此时只提示。
$diagRestore = $null
$diagRead = $null
# 本次断言所依据的日志是否在 DiagLog 打开时产生。
# 用于把「诊断标记缺失」从 FAIL 降级为 INFO（否则会把「没开诊断」误报成「功能没执行」）。
$diagGateOn = $false
try { $diagRead = Get-DiagLogState -GamePath $GamePath } catch { }
if ($null -ne $diagRead) { $diagGateOn = ($diagRead.Current -eq $true) }
if ($Launch) {
    try {
        $r = Set-DiagLog -GamePath $GamePath -Value $true
        if ($r.Changed) {
            $diagRestore = $r.Previous
            Add-Row 'INFO' 'diag.gate' ('已临时开启 DiagLog（回归依赖 [REG]/[REG.BP]），跑完自动还原') ('原值=' + $r.Previous)
        } else {
            Add-Row 'PASS' 'diag.gate' 'DiagLog 已为 true，回归断言可消费诊断标记' ''
        }
    } catch {
        Add-Row 'WARN' 'diag.gate' ('无法开启 DiagLog：依赖 [REG]/[REG.BP] 的 22 条断言可能误判 FAIL: ' + $_.Message) ''
    }
} else {
    if ($null -ne $diagRead -and $diagRead.Current -ne $true) {
        Add-Row 'WARN' 'diag.gate' 'DiagLog 当前为 false：本次未带 -Launch，日志里的诊断标记可能缺失，依赖它们的断言会误判 FAIL（建议加 -Launch 重跑）' ('当前值=' + $diagRead.Current)
    }
}

# ---------- optional launch ----------
$logStart = (Get-Date).AddSeconds(-3600)
if ($Launch) {
    $exe = Join-Path $GamePath 'Probably Stolen.exe'
    if (-not (Test-Path $exe)) { throw ('game exe not found: ' + $exe) }
    $running = Get-Process -Name 'Probably Stolen' -ErrorAction SilentlyContinue
    if ($running) {
        Add-Row 'INFO' 'launch' '游戏已在运行，跳过启动，直接对当前日志断言' ''
    } else {
        Add-Row 'INFO' 'launch' '启动游戏，等待加载完成...' $exe
        $logStart = Get-Date
        Start-Process -FilePath $exe -WorkingDirectory $GamePath | Out-Null
        $t0 = Get-Date
        $done = $false
        # 必须先确认读的是【本次会话】的日志：
        #   · MelonLoader 启动时会截断重写 Latest.log，所以拿"修改时间晚于启动时刻"当锚点。
        #     不加这个锚点 → 读到上一次会话的日志 → 里面早就有「预热完成」→ **假 PASS**
        #     （2026-10-07 12:07 那次就是：耗时 0 秒、随后断言阶段才炸）。
        #   · 游戏独占写日志期间 ReadAllText 会抛 IOException；老代码靠 catch/continue 硬等，
        #     结果整段超时（同日 12:18 那次 WARN 的真实原因）。
        #     改用共享读 + 重试，游戏开着也读得到。
        while (((Get-Date) - $t0).TotalSeconds -lt $TimeoutSec) {
            Start-Sleep -Milliseconds 800
            if (-not (Test-Path $LogPath)) { continue }
            $txt = $null
            try {
                if ((Get-Item $LogPath).LastWriteTime -lt $t0) { continue }
                $txt = Read-FileTextShared -Path $LogPath -Retry 3 -DelayMs 120
            } catch { continue }
            if ($txt -and ($txt -match '\[SBE-JIT\] 预热完成')) { $done = $true; break }
        }
        if ($done) {
            $secs = [int](((Get-Date) - $t0).TotalSeconds)
            Add-Row 'PASS' 'launch' ('加载完成，耗时 ' + $secs + ' 秒；再等 ' + $SettleSec + ' 秒稳定期') ''
            Start-Sleep -Seconds $SettleSec
        } else {
            Add-Row 'WARN' 'launch' ('等待超时 ' + $TimeoutSec + ' 秒，未见 JIT 预热完成标记') ''
        }
    }
}

# ---------- assertions ----------
$lines = Read-LogLines -Path $LogPath
if ($lines.Count -eq 0) { Add-Row 'FAIL' 'log.present' ('日志为空或不存在: ' + $LogPath) '' }
else { Add-Row 'PASS' 'log.present' ('日志已读入: ' + $lines.Count + ' 行') ((Get-Item $LogPath).LastWriteTime.ToString('MM-dd HH:mm:ss')) }

$checkDefs = @()
if (Test-Path $Checks) {
    $jsonText = [System.IO.File]::ReadAllText($Checks, (New-Object System.Text.UTF8Encoding($false)))
    $checkDefs = @((ConvertFrom-Json $jsonText).checks)
} else {
    Add-Row 'FAIL' 'checks.file' ('断言表不存在: ' + $Checks) ''
}

foreach ($c in $checkDefs) {
    $pat = [string]$c.pattern
    $pat = $pat.Replace('{suiteVersion}', $suiteVersion)
    $kind = [string]$c.kind
    $opt  = $false
    if ($c.PSObject.Properties.Name -contains 'optional') { $opt = [bool]$c.optional }

    $hit = $null
    foreach ($l in $lines) { if ([regex]::IsMatch($l, $pat)) { $hit = $l; break } }

    switch ($kind) {
        'exists' {
            if ($hit) { Add-Row 'PASS' $c.id $c.desc $hit.Trim() }
            elseif ($opt) { Add-Row 'INFO' $c.id ($c.desc + '（不适用）') '未命中且标记 optional' }
            else { Add-Row 'FAIL' $c.id $c.desc ('未匹配: ' + $pat) }
        }
        'absent' {
            if (-not $hit) { Add-Row 'PASS' $c.id $c.desc '' }
            else { Add-Row 'FAIL' $c.id $c.desc ('命中: ' + $hit.Trim()) }
        }
        'num' {
            if (-not $hit) {
                if ($opt) { Add-Row 'INFO' $c.id ($c.desc + '（不适用）') '' }
                else { Add-Row 'FAIL' $c.id $c.desc ('未匹配: ' + $pat) }
            } else {
                $mm = [regex]::Match($hit, $pat)
                $actual = [double]$mm.Groups[1].Value
                $want = [double]$c.value
                $op = [string]$c.op
                $ok = $false
                switch ($op) {
                    '>=' { $ok = ($actual -ge $want) }
                    '>'  { $ok = ($actual -gt $want) }
                    '==' { $ok = ($actual -eq $want) }
                    '<=' { $ok = ($actual -le $want) }
                    '<'  { $ok = ($actual -lt $want) }
                }
                $det = ('实际 ' + $actual + ' ' + $op + ' ' + $want)
                if ($ok) { Add-Row 'PASS' $c.id $c.desc $det } else { Add-Row 'FAIL' $c.id $c.desc $det }
            }
        }
        'twoEqual' {
            if (-not $hit) {
                if ($opt) { Add-Row 'INFO' $c.id ($c.desc + '（不适用）') '' }
                else { Add-Row 'FAIL' $c.id $c.desc ('未匹配: ' + $pat) }
            } else {
                $mm = [regex]::Match($hit, $pat)
                $a = $mm.Groups[1].Value; $b = $mm.Groups[2].Value
                $det = ($a + '/' + $b)
                if ($a -eq $b) { Add-Row 'PASS' $c.id $c.desc $det }
                else { Add-Row 'FAIL' $c.id $c.desc ($det + '  <- ' + ([int]$b - [int]$a) + ' 个没挂上') }
            }
        }
        'reg' {
            $rid = [string]$c.regId
            $rx = '\[REG\] id=' + [regex]::Escape($rid) + '(?: |$)'
            $rline = $null
            foreach ($l in $lines) { if ([regex]::IsMatch($l, $rx)) { $rline = $l; break } }
            if (-not $rline) {
                # ★ 2026-10-09：[REG] 已归入 DiagLog 总闸。闸门关闭时标记本就不该出现，
                #   此时判 FAIL 是把「没开诊断」误报成「功能没执行」。降级为 INFO 并说明。
                if (-not $diagGateOn) {
                    Add-Row 'INFO' $c.id ($c.desc + '（DiagLog 关闭，标记按设计未产出；加 -Launch 重跑可得断言）') ('expect id=' + $rid)
                }
                elseif ($opt) { Add-Row 'INFO' $c.id ($c.desc + '（未产出标记，不适用）') ('expect id=' + $rid) }
                else { Add-Row 'FAIL' $c.id ($c.desc + '：[REG] 标记缺失') ('expect id=' + $rid) }
            } else {
                $st = [regex]::Match($rline, 'status=(\S+)').Groups[1].Value
                $want = 'PASS'
                if ($c.PSObject.Properties.Name -contains 'expect') { $want = [string]$c.expect }
                if ($st -eq $want) { Add-Row 'PASS' $c.id $c.desc $rline.Trim() }
                elseif ($st -eq 'NA') { Add-Row 'INFO' $c.id $c.desc ('NA: ' + $rline.Trim()) }
                else { Add-Row 'FAIL' $c.id $c.desc $rline.Trim() }
            }
        }
        'bp' {
            $bn = [string]$c.bpName
            $rx = '\[REG\.BP\] name=' + [regex]::Escape($bn) + '(?: |$)'
            $rline = $null
            foreach ($l in $lines) { if ([regex]::IsMatch($l, $rx)) { $rline = $l; break } }
            if ($rline) { Add-Row 'PASS' $c.id $c.desc $rline.Trim() }
            else { Add-Row 'INFO' $c.id ($c.desc + '：断点未触发（交互期才产生）') '' }
        }
        default { Add-Row 'WARN' $c.id $c.desc ('未知 kind: ' + $kind) }
    }
}

# ---------- breakpoints observed ([REG.BP]) ----------
$bpLines = @()
foreach ($l in $lines) { if ($l -match '\[REG\.BP\]') { $bpLines += $l } }
if ($bpLines.Count -gt 0) {
    foreach ($b in $bpLines) { Add-Row 'INFO' 'bp' '断点留痕' $b.Trim() }
} else {
    Add-Row 'INFO' 'bp' '本次日志无 [REG.BP] 断点（断点在交互期触发：读档恢复/切页/升级）' ''
}

# ---------- summary ----------
$pass = @($rows | Where-Object { $_.status -eq 'PASS' }).Count
$fail = @($rows | Where-Object { $_.status -eq 'FAIL' }).Count
$warn = @($rows | Where-Object { $_.status -eq 'WARN' }).Count

$stats = [ordered]@{
    'Suite 版本' = $suiteVersion
    'PASS'      = $pass
    'FAIL'      = $fail
    'WARN'      = $warn
}

$subtitle = 'game = ' + $GamePath + '   |   log = ' + (Get-Item $LogPath).LastWriteTime.ToString('MM-dd HH:mm:ss')

$json = [ordered]@{
    tool = 'regress'; time = (Get-Date).ToString('s')
    gamePath = $GamePath; logPath = $LogPath; suiteVersion = $suiteVersion
    stats = $stats; rows = $rows
}
Write-Utf8 -Path (Join-Path $outDir 'regress-report.json') -Text ($json | ConvertTo-Json -Depth 8) -Bom

$html = New-ReportHtml -Title 'ContainerSuite 快速回归' -Subtitle $subtitle -Stats $stats -Rows $rows -Footer @(
    ('生成时间：' + (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')),
    'PASS=通过  FAIL=回归  WARN=留意  INFO=信息',
    '断言表：' + $Checks
)
$htmlPath = Join-Path $outDir 'regress-report.html'
Write-Utf8 -Path $htmlPath -Text $html -Bom

Say ''
Say ('regress: PASS=' + $pass + ' FAIL=' + $fail + ' WARN=' + $warn)
Say ('report: ' + $htmlPath)

# ---------- restore DiagLog ----------
# 只还原「本次由 regress 改成 true 且原先不是 true」的情况；原本就是 true 的不动。
if ($null -ne $diagRestore) {
    try {
        $null = Set-DiagLog -GamePath $GamePath -Value ([bool]$diagRestore)
        Say ('regress: DiagLog 已还原为 ' + $diagRestore)
    } catch {
        Say ('regress: WARN DiagLog 还原失败（请手动检查 cfg）: ' + $_.Message)
    }
}

if (-not $NoOpen) { try { Start-Process $htmlPath } catch { } }
if ($fail -gt 0) { exit 1 } else { exit 0 }
