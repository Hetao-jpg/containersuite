# DCK (2026-10-14): doc-vs-code cfg key-name guard.
# Called by ContainerSuite.Infrastructure.csproj before building.
# Exit 0 = every key name mentioned in the player-facing docs exists in CfgKeys.cs.
#
# WHY THIS EXISTS
#   cfg key names are renumbered whenever the panel ordering changes
#   (2026-10-07 added domain prefixes, 2026-10-13 renumbered "general to
#   specific"). Every renumbering leaves the docs behind. A player who edits
#   cfg following a stale doc writes a key that is not registered any more:
#     1) MelonPreferences never loads it (unregistered key = no entry),
#     2) so the entry keeps its CODE DEFAULT and the edit silently does nothing,
#     3) and the line may even be deleted by the migration / cleanup lists.
#   That is exactly the "my hand edit got overwritten by the default" report
#   (see docs/cfg-handedit-rootcause-and-fix.md).
#   check-readme-defaults.ps1 only compared the SIZE NUMBERS of the
#   warehouse-expand table, so key-name drift was never caught. This closes
#   that hole: a stale key name in a shipped doc now fails the build.
#
# SCOPE
#   Only player-facing docs are scanned (README.md.txt / README_mobile.txt /
#   the dev-and-power-user notice). History files (mod log, refactor plan)
#   intentionally quote retired key names and are NOT scanned.
#
# TODO (known gap, 2026-10-14)
#   This guard checks KEY NAMES only. The old check-readme-defaults.ps1 also
#   compared the doc's default SIZE numbers against CreateEntry defaults, but it
#   parsed the v1.7.6 README table that no longer exists -- it was moved to
#   _archive\<deprecated-guards>\ (CJK folder name; see the csproj comment).
#   If the README regains a machine-readable default table, add a value check.
#
# Style rules (same as the other _shared scripts):
#  - Pure ASCII ONLY. Do NOT add non-ASCII comments: PS 5.1 reads this file
#    without BOM as ANSI and multi-byte chars turn into mojibake. The one
#    non-ASCII filename is built from code points below.
#  - No regex. IndexOf / Split / char loop only.
#  - String output uses '+' concatenation; literals never contain " or [ or ].

param(
    [Parameter(Mandatory = $true)]
    [string]$WorkspaceRoot
)

$ErrorActionPreference = 'Stop'

# --- Doc set ---------------------------------------------------------------
# The notice filename is CJK; build it from code points to keep this file ASCII.
# cp = U+5F00 U+53D1 U+7528 U+6237 U+987B U+77E5  (kai-fa-yong-hu-xu-zhi)
$noticeName = ([string][char]0x5F00 + [string][char]0x53D1 + [string][char]0x7528 +
               [string][char]0x6237 + [string][char]0x987B + [string][char]0x77E5 + '.md')

$docs = @('README.md.txt', 'README_mobile.txt', $noticeName)

# Historical mentions that are intentional (the surrounding text says the key
# was DELETED), plus code identifiers that merely LOOK like cfg keys.
# A listing records a manual review -- it is NOT a blanket licence for stale
# names elsewhere.
$allowStale = @(
    '24_R1Probe',        # header of the deleted one-off probe (the text says so)
    '25_ResizeDiag',     # same line
    'DefaultIds',        # file name  DiyUpgrade.CfgMigration.DefaultIds.cs
    'EnsureDefaultIds'   # method name, quoted when explaining the whitelist top-up
)

# --- 1. Parse CfgKeys.cs --------------------------------------------------
$cfgPath = Join-Path $WorkspaceRoot 'ContainerSuite.Shared\CfgKeys.cs'
if (-not (Test-Path $cfgPath)) {
    Write-Host ('DCK: CfgKeys.cs not found: ' + $cfgPath)
    exit 1
}

$registered = @{}
$cfgLines = [System.IO.File]::ReadAllLines($cfgPath, (New-Object System.Text.UTF8Encoding($false)))

foreach ($ln in $cfgLines) {
    # const string form:  public const string Name = "VALUE";
    $ci = $ln.IndexOf('public const string ')
    if ($ci -ge 0) {
        $q1 = $ln.IndexOf('"', $ci)
        if ($q1 -ge 0) {
            $q2 = $ln.IndexOf('"', $q1 + 1)
            if ($q2 -gt $q1) { $registered[$ln.Substring($q1 + 1, $q2 - $q1 - 1)] = 1 }
        }
        continue
    }

    # grid form:  public static readonly GridKeys X = new GridKeys("7A_", "Box");
    $gi = $ln.IndexOf('new GridKeys(')
    if ($gi -lt 0) { continue }
    $q1 = $ln.IndexOf('"', $gi)
    if ($q1 -lt 0) { continue }
    $q2 = $ln.IndexOf('"', $q1 + 1)
    if ($q2 -lt 0) { continue }
    $q3 = $ln.IndexOf('"', $q2 + 1)
    if ($q3 -lt 0) { continue }
    $q4 = $ln.IndexOf('"', $q3 + 1)
    if ($q4 -lt 0) { continue }
    $sortPrefix = $ln.Substring($q1 + 1, $q2 - $q1 - 1)
    $name = $ln.Substring($q3 + 1, $q4 - $q3 - 1)

    $registered[$sortPrefix + $name + 'Enable'] = 1
    $registered[$sortPrefix + $name + 'MaxW'] = 1
    $registered[$sortPrefix + $name + 'MaxH'] = 1
    $registered[$name + 'Ids'] = 1
    if ($name -eq 'MultiSlot' -or $name -eq 'MultiSlotModule') {
        $registered[$sortPrefix + $name + 'AlignSlots'] = 1
    }
}

Write-Host ('DCK: registered cfg keys = ' + $registered.Count)

# --- 2. Tokenizer (ASCII word chars only) ---------------------------------
function Get-Tokens([string]$line) {
    $out = New-Object System.Collections.Generic.List[string]
    $sb = New-Object System.Text.StringBuilder
    foreach ($ch in $line.ToCharArray()) {
        $c = [int][char]$ch
        $isTok = (($c -ge 48 -and $c -le 57) -or ($c -ge 97 -and $c -le 122) -or
                  ($c -ge 65 -and $c -le 90) -or $c -eq 95)
        if ($isTok) {
            [void]$sb.Append($ch)
        } else {
            if ($sb.Length -gt 0) { $out.Add($sb.ToString()); [void]$sb.Clear() }
        }
    }
    if ($sb.Length -gt 0) { $out.Add($sb.ToString()) }
    return $out
}

# A cfg key name looks like  2D_SaveBagWidth / 7A_BoxMaxW / z1_DiagLog / BoxIds
function Is-KeyCandidate([string]$t) {
    if ($t.Length -lt 4) { return $false }
    if ($t.EndsWith('Ids')) { return $true }
    $c0 = $t[0]
    $isNum = ($c0 -ge '0' -and $c0 -le '9')
    if (-not $isNum -and $c0 -ne 'z' -and $c0 -ne 'Z') { return $false }
    if ($t.IndexOf('_') -lt 0) { return $false }
    return $true
}

# Docs legitimately mention a CATEGORY PREFIX instead of a whole key, e.g.
# "7A_Box / 7B_Backpack" or "6A_Trashcan_*". Accept a token that is a proper
# prefix of some registered key: that keeps the guard strict on renumbered
# prefixes (7_ExpandEnabled is NOT a prefix of 2_ExpandEnabled, so it still
# fails) while not nagging about partial mentions.
function Is-KnownPrefix([string]$t) {
    foreach ($k in $registered.Keys) {
        if ($k.Length -gt $t.Length -and $k.StartsWith($t, [System.StringComparison]::Ordinal)) {
            return $true
        }
    }
    return $false
}

# --- 3. Scan the docs -----------------------------------------------------
$bad = @()
$checked = 0

foreach ($doc in $docs) {
    $path = Join-Path $WorkspaceRoot $doc
    if (-not (Test-Path $path)) {
        Write-Host ('DCK: WARNING doc not found, skipped: ' + $doc)
        continue
    }
    $checked++
    $lines = [System.IO.File]::ReadAllLines($path, (New-Object System.Text.UTF8Encoding($false)))
    $seen = @{}
    for ($i = 0; $i -lt $lines.Length; $i++) {
        foreach ($tok in (Get-Tokens $lines[$i])) {
            if (Is-KeyCandidate $tok) {
                if ($registered.ContainsKey($tok)) { continue }
                if ($allowStale -contains $tok) { continue }
                if (Is-KnownPrefix $tok) { continue }
            } else {
                continue
            }
            $id = $doc + '|' + $tok
            if ($seen.ContainsKey($id)) { continue }
            $seen[$id] = 1
            $bad += ($doc + ':' + ($i + 1) + '  key name "' + $tok +
                     '" is not registered in CfgKeys.cs -- a player following this doc ' +
                     'writes an unregistered key, so the edit silently does nothing and ' +
                     'the entry keeps its code default (the reported "my cfg edit got ' +
                     'overwritten" bug). Fix the doc, or add the token to $allowStale ' +
                     'in _shared\check-doc-keys.ps1 if it is a reviewed historical mention.')
        }
    }
}

if ($bad.Count -gt 0) {
    foreach ($b in $bad) { Write-Host ('DCK: ' + $b) }
    Write-Host ('DCK: doc key-name check FAILED (' + $bad.Count + ' stale name(s)).')
    exit 1
}

Write-Host ('DCK: doc key-name check passed (' + $checked + ' doc(s), ' + $registered.Count + ' registered keys).')
exit 0
