# ============================================================
#  lib.ps1 -- ContainerSuite regress toolchain, shared helpers.
#
#  Deliberately ASCII-only: PowerShell 5.1 mis-decodes non-ASCII
#  .ps1 without BOM, and "remember to add a BOM" is a fragile
#  convention (see verify-log.ps1 header). Chinese text lives in
#  checks.json and in the generated reports (explicit UTF-8).
#
#  Log reading note: Latest.log is UTF-8 without BOM and contains
#  a few NUL bytes, which makes grep/file treat it as binary.
#  .NET ReadAllText is unaffected, but we strip NUL anyway.
# ============================================================

function Get-GamePath {
    param([string]$Root, [string]$Override)
    if ($Override) { return $Override }
    $props = Join-Path $Root '_shared\CommonMod.props'
    if (Test-Path $props) {
        try {
            [xml]$x = Get-Content -Path $props
            foreach ($pg in @($x.Project.PropertyGroup)) {
                if ($pg.GamePath -is [string] -and $pg.GamePath) { return $pg.GamePath }
            }
        } catch { }
    }
    return $null
}

function Get-LogPath {
    param([string]$GamePath, [string]$Override)
    if ($Override) { return $Override }
    return (Join-Path $GamePath 'MelonLoader\Latest.log')
}

function Read-LogLines {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return @() }
    $txt = Read-FileTextShared -Path $Path
    $txt = $txt.Replace([string][char]0, '')
    return @($txt -split "`r?`n")
}

# 共享读：游戏正在写 Latest.log 时 ReadAllText 会抛 IOException（file in use）。
# 2026-10-07 实测：regress -Deploy -Launch 在断言阶段就因为这个崩掉（游戏已成功启动）。
# 用 FileShare.ReadWrite 打开 + 重试，让"游戏还开着"也能断言。
function Read-FileTextShared {
    param([string]$Path, [int]$Retry = 6, [int]$DelayMs = 250)
    $enc = New-Object System.Text.UTF8Encoding($false)
    for ($i = 0; $i -lt $Retry; $i++) {
        try {
            $fs = [System.IO.File]::Open($Path, [System.IO.FileMode]::Open,
                                         [System.IO.FileAccess]::Read,
                                         [System.IO.FileShare]::ReadWrite)
            try {
                $sr = New-Object System.IO.StreamReader($fs, $enc)
                try { return $sr.ReadToEnd() } finally { $sr.Dispose() }
            } finally { $fs.Dispose() }
        } catch {
            if ($i -eq ($Retry - 1)) { throw }
            Start-Sleep -Milliseconds $DelayMs
        }
    }
    return ''
}

# First line matching $Pattern; returns the whole line or $null.
function Get-MarkerLine {
    param([string[]]$Lines, [string]$Pattern)
    foreach ($l in $Lines) {
        if ([regex]::IsMatch($l, $Pattern)) { return $l }
    }
    return $null
}

# First capture group of the first matching line, or $null.
function Get-MarkerValue {
    param([string[]]$Lines, [string]$Pattern, [int]$Group = 1)
    foreach ($l in $Lines) {
        $m = [regex]::Match($l, $Pattern)
        if ($m.Success) {
            if ($m.Groups.Count -gt $Group) { return $m.Groups[$Group].Value }
            return $m.Value
        }
    }
    return $null
}

function Get-MarkerValues {
    param([string[]]$Lines, [string]$Pattern, [int]$Group = 1)
    $res = @()
    foreach ($l in $Lines) {
        $m = [regex]::Match($l, $Pattern)
        if ($m.Success -and $m.Groups.Count -gt $Group) { $res += $m.Groups[$Group].Value }
    }
    return $res
}

function Write-Utf8 {
    param([string]$Path, [string]$Text, [switch]$Bom)
    $dir = Split-Path -Parent $Path
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir -Force | Out-Null }
    $enc = New-Object System.Text.UTF8Encoding([bool]$Bom)
    [System.IO.File]::WriteAllText($Path, $Text, $enc)
}

function Get-FileSha256 {
    param([string]$Path)
    if (-not (Test-Path $Path)) { return $null }
    try { return (Get-FileHash -Path $Path -Algorithm SHA256).Hash } catch { return $null }
}

# --- report html --------------------------------------------------
# $Rows: array of hashtables @{ status='PASS'|'FAIL'|'WARN'|'INFO'; id=..; desc=..; detail=.. }
function New-ReportHtml {
    param(
        [string]$Title,
        [string]$Subtitle,
        [hashtable]$Stats,
        [object[]]$Rows,
        [string[]]$Footer
    )

    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine('<!DOCTYPE html>')
    [void]$sb.AppendLine('<html lang="zh-CN"><head><meta charset="utf-8">')
    [void]$sb.AppendLine('<meta name="viewport" content="width=device-width,initial-scale=1">')
    [void]$sb.AppendLine('<title>' + [System.Net.WebUtility]::HtmlEncode($Title) + '</title>')
    [void]$sb.AppendLine(@'
<style>
:root{--bg:#f7f8fa;--card:#ffffff;--bd:rgba(0,0,0,.10);--tx:#1f2328;--tx2:#656d76;--mono:ui-monospace,Consolas,monospace}
*{box-sizing:border-box}
body{margin:0;background:var(--bg);color:var(--tx);font:13px/1.6 -apple-system,Segoe UI,Roboto,"Microsoft YaHei",sans-serif;padding:24px}
.wrap{max-width:980px;margin:0 auto}
h1{font-size:18px;font-weight:500;margin:0 0 4px}
.sub{color:var(--tx2);font-size:13px;margin-bottom:16px}
.stats{display:grid;grid-template-columns:repeat(auto-fit,minmax(120px,1fr));gap:10px;margin-bottom:16px}
.stat{background:var(--card);border:.5px solid var(--bd);border-radius:10px;padding:12px 14px}
.stat .k{color:var(--tx2);font-size:12px}
.stat .v{font-size:22px;font-weight:500;margin-top:2px}
.card{background:var(--card);border:.5px solid var(--bd);border-radius:12px;overflow:hidden}
.row{display:flex;gap:10px;align-items:flex-start;padding:9px 14px;border-top:.5px solid var(--bd)}
.row:first-child{border-top:0}
.tag{flex:none;width:52px;text-align:center;font-size:11px;font-weight:500;border-radius:4px;padding:2px 0}
.t-PASS{background:#E1F5EE;color:#0F6E56}
.t-FAIL{background:#FCEBEB;color:#A32D2D}
.t-WARN{background:#FAEEDA;color:#854F0B}
.t-INFO{background:#E6F1FB;color:#0C447C}
.rid{flex:none;width:170px;font-family:var(--mono);font-size:12px;color:var(--tx2);word-break:break-all}
.rd{flex:1;min-width:0}
.rd .d{color:var(--tx2);font-family:var(--mono);font-size:12px;word-break:break-all;white-space:pre-wrap;margin-top:2px}
.foot{margin-top:14px;color:var(--tx2);font-size:12px}
</style>
'@)
    [void]$sb.AppendLine('</head><body><div class="wrap">')
    [void]$sb.AppendLine('<h1>' + [System.Net.WebUtility]::HtmlEncode($Title) + '</h1>')
    if ($Subtitle) { [void]$sb.AppendLine('<div class="sub">' + [System.Net.WebUtility]::HtmlEncode($Subtitle) + '</div>') }

    if ($Stats) {
        [void]$sb.AppendLine('<div class="stats">')
        foreach ($k in $Stats.Keys) {
            $v = $Stats[$k]
            [void]$sb.AppendLine('<div class="stat"><div class="k">' + [System.Net.WebUtility]::HtmlEncode([string]$k) + '</div><div class="v">' + [System.Net.WebUtility]::HtmlEncode([string]$v) + '</div></div>')
        }
        [void]$sb.AppendLine('</div>')
    }

    [void]$sb.AppendLine('<div class="card">')
    foreach ($r in $Rows) {
        $st = [string]$r.status
        $cls = 't-' + $st
        [void]$sb.AppendLine('<div class="row"><div class="tag ' + $cls + '">' + [System.Net.WebUtility]::HtmlEncode($st) + '</div>')
        [void]$sb.AppendLine('<div class="rid">' + [System.Net.WebUtility]::HtmlEncode([string]$r.id) + '</div>')
        [void]$sb.AppendLine('<div class="rd">' + [System.Net.WebUtility]::HtmlEncode([string]$r.desc))
        if ($r.detail) { [void]$sb.AppendLine('<div class="d">' + [System.Net.WebUtility]::HtmlEncode([string]$r.detail) + '</div>') }
        [void]$sb.AppendLine('</div></div>')
    }
    [void]$sb.AppendLine('</div>')

    if ($Footer) {
        foreach ($f in $Footer) { [void]$sb.AppendLine('<div class="foot">' + [System.Net.WebUtility]::HtmlEncode($f) + '</div>') }
    }
    [void]$sb.AppendLine('</div></body></html>')
    return $sb.ToString()
}
