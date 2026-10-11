# ============================================================
#  Set-DiagLog -- flip the suite's DiagLog cfg flag in MelonPreferences.cfg.
#
#  ★ 2026-10-09 新增。背景：套件把「诊断日志」收成全局总闸（CI / SBE / DIY 三处
#    的逐步打点、[REG]/[REG.BP] 标记、分页诊断 dump、偏移解析、WorldReadyGate
#    事件、cfg 自检提示… 全部归入 DiagLog 之下）。回归断言消费的正是这些行 ——
#    checks.json 有 22 条断言依赖 [REG]/[REG.BP]，还有 4 条依赖中文文本行。
#    ⇒ 回归跑之前必须先把开关打开，否则断言会误判为「功能没执行」。
#
#  语义：幂等；返回 @{ Path, Changed, WasSet } 便于调用方决定是否还原。
#  不做任何「聪明」判断 —— cfg 里可能有多处同名键，一律走 MelonLoader 的段解析。
# ============================================================

# cfg key lives in section [CustomInfrastructure], name z1_DiagLog
# (see CustomInfrastructureConfig.cs). Kept as constants so a rename is loud here.
$script:DiagSection = 'CustomInfrastructure'
$script:DiagKey     = 'z1_DiagLog'

function Get-DiagCfgPath {
    param([string]$GamePath)
    return (Join-Path $GamePath 'UserData\MelonPreferences.cfg')
}

function Get-DiagLogState {
    <#
      .SYNOPSIS
        Read the suite DiagLog flag without modifying anything.
      .OUTPUTS
        @{ Path; Changed=$false; Previous; Current }  (Current = $null when the key is absent)
    #>
    param([Parameter(Mandatory = $true)][string]$GamePath)
    return (Set-DiagLog -GamePath $GamePath)   # no -Value => read-only path
}

function Set-DiagLog {
    <#
      .SYNOPSIS
        Set (or read) the suite DiagLog flag in MelonPreferences.cfg.
      .PARAMETER Value
        $true/$false to set; omit to only read.
      .OUTPUTS
        @{ Path; Changed; Previous; Current }
    #>
    param(
        [Parameter(Mandatory = $true)][string]$GamePath,
        [Nullable[bool]]$Value
    )

    # ★ 不要用 `$Value.HasValue` 判断「是否要写」：PowerShell 5.1 会把
    #   [Nullable[bool]] 拆箱成 [bool]，此时 .HasValue 求值为 $null，
    #   -not $null 为 $true ⇒ 永远走「只读」分支，设置被静默忽略
    #   （2026-10-09 实测：Changed=False 但调用方以为写成功了）。
    #   改用 PSBoundParameters 判定「参数是否被显式传入」。
    $doWrite = $PSBoundParameters.ContainsKey('Value')

    $path = Get-DiagCfgPath -GamePath $GamePath
    if (-not (Test-Path $path)) {
        throw ("MelonPreferences.cfg not found: " + $path)
    }

    # 读：必须用 UTF-8 无 BOM（MelonPreferences 写盘就是这个编码），
    #     且游戏正在写时 ReadAllText 会抛 IOException -> 共享读 + 重试。
    $text = Read-FileTextShared -Path $path -Retry 4 -DelayMs 200

    $lines = $text -split "`r?`n"
    $sectionStart = -1
    $sectionEnd = $lines.Count
    for ($i = 0; $i -lt $lines.Count; $i++) {
        $t = $lines[$i].Trim()
        if (-not $t.StartsWith('[')) { continue }
        $name = $t.Trim('[', ']', '"').Trim()
        if ($sectionStart -lt 0) {
            if ($name -eq $script:DiagSection) { $sectionStart = $i }
        } else {
            $sectionEnd = $i
            break
        }
    }

    $current = $null
    $keyIdx = -1
    if ($sectionStart -ge 0) {
        for ($i = $sectionStart + 1; $i -lt $sectionEnd; $i++) {
            $t = $lines[$i].Trim()
            if (-not $t -or $t.StartsWith('#') -or $t.StartsWith('[')) { continue }
            $eq = $t.IndexOf('=')
            if ($eq -le 0) { continue }
            if ($t.Substring(0, $eq).Trim().Trim('"') -eq $script:DiagKey) {
                $keyIdx = $i
                $v = $t.Substring($eq + 1).Trim().Trim('"')
                $cur = $false
                if ([bool]::TryParse($v, [ref]$cur)) { $current = $cur }
                break
            }
        }
    }

    if (-not $doWrite) {
        return @{ Path = $path; Changed = $false; Previous = $current; Current = $current }
    }

    $want = [bool]$Value
    if ($null -ne $current -and $current -eq $want) {
        return @{ Path = $path; Changed = $false; Previous = $current; Current = $current }
    }

    $newLine = $script:DiagKey + ' = ' + $(if ($want) { 'true' } else { 'false' })
    if ($keyIdx -ge 0) {
        $lines[$keyIdx] = $newLine
    } elseif ($sectionStart -ge 0) {
        # 段存在但没有该键：插在段末尾（保持 MelonPreferences 的键序风格）
        $ins = @()
        $ins += $lines[0..($sectionEnd - 1)]
        $ins += $newLine
        if ($sectionEnd -lt $lines.Count) { $ins += $lines[$sectionEnd..($lines.Count - 1)] }
        $lines = $ins
    } else {
        throw ("section [" + $script:DiagSection + "] not found in " + $path)
    }

    $out = ($lines -join "`r`n")
    [System.IO.File]::WriteAllText($path, $out, (New-Object System.Text.UTF8Encoding($false)))
    return @{ Path = $path; Changed = $true; Previous = $current; Current = $want }
}