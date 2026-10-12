# ============================================================
#  mods-scan.ps1 -- standalone mod detection tool (read-only).
#
#  Does NOT modify any mod. Reads:
#    * <Game>\Mods\*.dll            (what is deployed)
#    * <Game>\MelonLoader\Latest.log (what actually loaded + hashes)
#    * <Game>\Mods\WhoAmI\黑匣子-最新.txt (conflict scan + runtime state)
#    * <Game>\UserData\MelonPreferences.cfg (config sections)
#  Emits: out\mods-report.json + out\mods-report.html
#
#  Usage:
#    powershell -File _regress\mods-scan.ps1
#    powershell -File _regress\mods-scan.ps1 -Root "D:\games\open code work"
#    powershell -File _regress\mods-scan.ps1 -GamePath "D:\...\Probably Stolen Demo"
# ============================================================

param(
    [string]$Root,
    [string]$GamePath,
    [string]$LogPath,
    [switch]$NoOpen,
    [switch]$Quiet
)

$ErrorActionPreference = 'Stop'
. (Join-Path $PSScriptRoot 'lib.ps1')

if (-not $Root) { $Root = Split-Path -Parent $PSScriptRoot }
$outDir = Join-Path $Root '_regress\out'

if (-not $GamePath) { $GamePath = Get-GamePath -Root $Root }
if (-not $GamePath) { throw 'GamePath not found (pass -GamePath, or set it in ContainerSuite.Infrastructure.csproj).' }
if (-not $LogPath)  { $LogPath  = Get-LogPath -GamePath $GamePath }

$modsDir = Join-Path $GamePath 'Mods'
$cfgPath = Join-Path $GamePath 'UserData\MelonPreferences.cfg'
$bbPath  = Join-Path $GamePath 'Mods\WhoAmI\黑匣子-最新.txt'

function Say([string]$m) { if (-not $Quiet) { Write-Host $m } }

Say ('mods-scan: game = ' + $GamePath)
Say ('mods-scan: log  = ' + $LogPath)

$rows = New-Object System.Collections.ArrayList
function Add-Row([string]$status, [string]$id, [string]$desc, [string]$detail) {
    [void]$rows.Add(@{ status = $status; id = $id; desc = $desc; detail = $detail })
}

# ---------- 1. deployed DLLs (active = Mods root) ----------
$activeDlls = @()
if (Test-Path $modsDir) {
    $activeDlls = @(Get-ChildItem -Path $modsDir -Filter '*.dll' -File -ErrorAction SilentlyContinue)
}
$activeByName = @{}
foreach ($f in $activeDlls) { $activeByName[$f.Name] = $f }

# shadow DLLs: anything under Mods but not at root
$shadowDlls = @()
if (Test-Path $modsDir) {
    $shadowDlls = @(Get-ChildItem -Path $modsDir -Filter '*.dll' -File -Recurse -ErrorAction SilentlyContinue |
                    Where-Object { $_.DirectoryName -ne (Resolve-Path $modsDir).Path })
}

# ---------- 2. parse the log ----------
$lines = Read-LogLines -Path $LogPath
$logExists = (Test-Path $LogPath)

$loaded = New-Object System.Collections.ArrayList   # @{ name; hash }
$curName = $null
foreach ($l in $lines) {
    $m = [regex]::Match($l, "Melon Assembly loaded: '\.\\Mods\\(.+?)'")
    if ($m.Success) { $curName = $m.Groups[1].Value; continue }
    $h = [regex]::Match($l, "SHA256 Hash: '([0-9A-Fa-f]+)'")
    if ($h.Success -and $curName) {
        [void]$loaded.Add(@{ name = $curName; hash = $h.Groups[1].Value.ToUpper() })
        $curName = $null
    }
}
$loadedByName = @{}
foreach ($x in $loaded) { $loadedByName[$x.name] = $x.hash }

# ---------- 3. cross-check deployed vs loaded ----------
$hashMismatch = @()
$notLoaded   = @()
foreach ($f in $activeDlls) {
    $diskHash = Get-FileSha256 -Path $f.FullName
    if (-not $loadedByName.ContainsKey($f.Name)) {
        $notLoaded += $f.Name
        Add-Row 'FAIL' ('mod.loaded:' + $f.Name) 'DLL 在 Mods 目录但日志里没有被加载' ('size=' + $f.Length + 'B  hash=' + $diskHash)
    } elseif ($loadedByName[$f.Name] -ne $diskHash) {
        $hashMismatch += $f.Name
        Add-Row 'WARN' ('mod.hash:' + $f.Name) '日志里的哈希与磁盘不一致（文件已改，但日志来自旧进程）' ('disk=' + $diskHash + '  log=' + $loadedByName[$f.Name])
    } else {
        Add-Row 'PASS' ('mod.loaded:' + $f.Name) '已加载且哈希一致' ($f.Length.ToString() + 'B')
    }
}
$orphanLoaded = @($loaded | Where-Object { -not $activeByName.ContainsKey($_.name) })
foreach ($o in $orphanLoaded) {
    Add-Row 'WARN' ('mod.ghost:' + $o.name) '日志里有加载记录，但 Mods 目录已无此文件' ''
}

# ---------- 4. deploy drift for ContainerSuite ----------
$builtDll = Join-Path $Root 'ContainerSuite.Infrastructure\bin\Release\CustomInfrastructure.dll'
$deployed = Join-Path $modsDir 'CustomInfrastructure.dll'
if ((Test-Path $builtDll) -and (Test-Path $deployed)) {
    $hb = Get-FileSha256 -Path $builtDll
    $hd = Get-FileSha256 -Path $deployed
    if ($hb -eq $hd) {
        Add-Row 'PASS' 'suite.deploy' 'Mods 里的 CustomInfrastructure.dll 与最新 Release 产物一致' $hb.Substring(0,16)
    } else {
        Add-Row 'FAIL' 'suite.deploy' 'Mods 里的 CustomInfrastructure.dll 落后于 Release 产物（未部署）' ('build=' + $hb.Substring(0,16) + ' deployed=' + $hd.Substring(0,16))
    }
} else {
    Add-Row 'INFO' 'suite.deploy' '未能同时找到 Release 产物与已部署 DLL，跳过部署漂移检查' ('built=' + (Test-Path $builtDll) + ' deployed=' + (Test-Path $deployed))
}

# ---------- 5. errors / warnings in the log ----------
$errLines = New-Object System.Collections.ArrayList
$warnLines = New-Object System.Collections.ArrayList
foreach ($l in $lines) {
    if ($l -match '\[ERROR\]|\[ERR\]') { [void]$errLines.Add($l) }
    elseif ($l -match '\[WARNING\]|\[WARN\]') { [void]$warnLines.Add($l) }
}
$errLines = @($errLines | Select-Object -Unique)
$warnLines = @($warnLines | Select-Object -Unique)
$ownTag = 'CustomInfrastructure|SaveBagExpand|DIYUpgrade|ContainerSuite'
$errOwn   = @($errLines | Where-Object { $_ -match $ownTag })
$errOther = @($errLines | Where-Object { $_ -notmatch $ownTag })
if ($errOwn.Count -eq 0) {
    Add-Row 'PASS' 'log.errors.suite' '本套件无 [ERROR]' ''
} else {
    foreach ($e in $errOwn) { Add-Row 'FAIL' 'log.errors.suite' '本套件出现 ERROR' $e.Trim() }
}
foreach ($e in $errOther) { Add-Row 'WARN' 'log.errors.other' '其它 mod 的 ERROR（非本套件）' $e.Trim() }
Add-Row 'INFO' 'log.warnings' ('日志 WARNING 共 ' + $warnLines.Count + ' 条（去重后）') ''

# ---------- 6. WhoAmI black box cross-check ----------
$bbLoaded = $null; $bbPatchTotal = $null; $bbConflicts = $null
if (Test-Path $bbPath) {
    $bb = Read-LogLines -Path $bbPath
    $v = Get-MarkerValue -Lines $bb -Pattern '已装载：\s*(\d+)\s*个'
    if ($v) { $bbLoaded = [int]$v }
    $v2 = Get-MarkerValue -Lines $bb -Pattern '补丁方法\s*(\d+)\s*个'
    if ($v2) { $bbPatchTotal = [int]$v2 }
    $v3 = Get-MarkerValue -Lines $bb -Pattern '其中\s*(\d+)\s*个被多个模组改写'
    if ($v3) { $bbConflicts = [int]$v3 }

    if ($bbConflicts -ne $null) {
        if ($bbConflicts -eq 0) {
            Add-Row 'PASS' 'whoami.conflicts' 'WhoAmI 冲突扫描：未发现多模组改写同一方法' ('补丁方法 ' + $bbPatchTotal + ' 个')
        } else {
            Add-Row 'WARN' 'whoami.conflicts' ('WhoAmI 冲突扫描：发现 ' + $bbConflicts + ' 个方法被多个模组改写') ''
        }
    }
    if ($bbLoaded -ne $null -and $loaded.Count -gt 0 -and $bbLoaded -ne $loaded.Count) {
        Add-Row 'INFO' 'whoami.count' ('WhoAmI 记录装载 ' + $bbLoaded + ' 个，日志加载 ' + $loaded.Count + ' 个') '两者口径可能不同，仅供参考'
    }
} else {
    Add-Row 'INFO' 'whoami.conflicts' '未找到 WhoAmI 黑匣子，跳过冲突扫描交叉验证' $bbPath
}

# ---------- 7. config sections ----------
$cfgSections = @()
if (Test-Path $cfgPath) {
    $cfgLines = Read-LogLines -Path $cfgPath
    foreach ($l in $cfgLines) {
        $m = [regex]::Match($l, '^\[(.+?)\]\s*$')
        if ($m.Success) { $cfgSections += $m.Groups[1].Value }
    }
}

# ---------- 8. shadow mods (grouped, not one row per file) ----------
$modsRootFull = (Resolve-Path $modsDir).Path
$shadowGroups = @{}
foreach ($s in $shadowDlls) {
    $rel = $s.FullName.Substring($modsRootFull.Length).TrimStart('\')
    $top = $rel.Split('\')[0]
    if (-not $shadowGroups.ContainsKey($top)) { $shadowGroups[$top] = @() }
    $shadowGroups[$top] += $rel
}
foreach ($k in ($shadowGroups.Keys | Sort-Object)) {
    $g = @($shadowGroups[$k])
    $preview = ($g | Select-Object -First 5) -join ' | '
    Add-Row 'INFO' ('shadow:' + $k) ('未加载目录「' + $k + '」含 ' + $g.Count + ' 个 DLL（备份/子目录，不参与加载）') $preview
}
foreach ($o in $orphanLoaded) { }

# ---------- summary ----------
$pass = @($rows | Where-Object { $_.status -eq 'PASS' }).Count
$fail = @($rows | Where-Object { $_.status -eq 'FAIL' }).Count
$warn = @($rows | Where-Object { $_.status -eq 'WARN' }).Count

$stats = [ordered]@{
    'Mods 目录 DLL'   = $activeDlls.Count
    '日志已加载'      = $loaded.Count
    'PASS'           = $pass
    'FAIL'           = $fail
    'WARN'           = $warn
    '本套件 ERROR'    = $errOwn.Count
    '其它 mod ERROR'  = $errOther.Count
    '影子 DLL'        = $shadowDlls.Count
    'cfg 段'          = $cfgSections.Count
}

$logName = if ($logExists) { (Get-Item $LogPath).LastWriteTime.ToString('MM-dd HH:mm:ss') } else { '(missing)' }
$subtitle = 'game = ' + $GamePath + '   |   Latest.log = ' + $logName

$json = [ordered]@{
    tool      = 'mods-scan'
    time      = (Get-Date).ToString('s')
    gamePath  = $GamePath
    logPath   = $LogPath
    logTime   = $logName
    stats     = $stats
    loaded    = @($loaded | ForEach-Object { $_.name })
    notLoaded = $notLoaded
    hashMismatch = $hashMismatch
    shadow    = @($shadowDlls | ForEach-Object { $_.Name })
    cfgSections = $cfgSections
    errors    = $errLines
    warnings  = $warnLines
    rows      = $rows
}
Write-Utf8 -Path (Join-Path $outDir 'mods-report.json') -Text ($json | ConvertTo-Json -Depth 8) -Bom

$html = New-ReportHtml -Title 'Mod 检测报告' -Subtitle $subtitle -Stats $stats -Rows $rows -Footer @(
    ('生成时间：' + (Get-Date).ToString('yyyy-MM-dd HH:mm:ss')),
    'PASS=正常  WARN=需留意  FAIL=需处理  INFO=信息',
    '本工具只读，不修改任何 mod 文件。'
)
$htmlPath = Join-Path $outDir 'mods-report.html'
Write-Utf8 -Path $htmlPath -Text $html -Bom

Say ''
Say ('mods-scan: PASS=' + $pass + ' FAIL=' + $fail + ' WARN=' + $warn)
Say ('report: ' + $htmlPath)
Say ('json:   ' + (Join-Path $outDir 'mods-report.json'))

if (-not $NoOpen) { try { Start-Process $htmlPath } catch { } }

if ($fail -gt 0) { exit 1 } else { exit 0 }
