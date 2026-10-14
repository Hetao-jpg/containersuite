# DOC (2026-10-06): README default-size table vs SaveBagExpandConfig.CreateEntry defaults.
# Called by _shared\CommonMod.props before building ContainerSuite.Infrastructure.
# Exit 0 = consistent, exit 1 = mismatch or error.
#
# What this checks:
#   The warehouse-expand default-size table in README.md.txt lists the default
#   width/height for the warehouse-expand containers (keys 7A..7F, *Width /
#   *Height). Each of those defaults must match the int passed to
#   CreateEntry<int>(...) in ContainerSuite.SaveBag\SaveBagExpandConfig.cs.
#   If the doc says 24x10 but the code ships 24x16, a user following the doc gets the
#   wrong behaviour. This makes that drift a compile-time failure instead of a
#   silent surprise.
#
# Rules to avoid PowerShell parser pitfalls (same as check-suite-version.ps1):
#  - Pure ASCII. No Chinese literals anywhere (the Chinese in the README is only in the
#    data we READ, never in a string we WRITE).
#  - No regex for matching; use IndexOf / Split / Substring.
#  - String output uses '+' concatenation; literals never contain " or [ or ].

param(
    [Parameter(Mandatory = $true)]
    [string]$WorkspaceRoot
)

$ErrorActionPreference = 'Stop'

# --- Resolve files ---
$readmePath = Join-Path $WorkspaceRoot 'README.md.txt'
$codePath   = Join-Path $WorkspaceRoot 'ContainerSuite.SaveBag\SaveBagExpandConfig.cs'

if (-not (Test-Path $readmePath)) {
    Write-Host ('DOC: README not found: ' + $readmePath)
    exit 1
}
if (-not (Test-Path $codePath)) {
    Write-Host ('DOC: SaveBagExpandConfig.cs not found: ' + $codePath)
    exit 1
}

$readmeText = Get-Content -Path $readmePath -Raw
$codeText   = Get-Content -Path $codePath -Raw

# --- Helpers ---
function Is-Digits([string]$s) {
    if ($s.Length -eq 0) { return $false }
    foreach ($c in $s.ToCharArray()) {
        if ($c -lt '0' -or $c -gt '9') { return $false }
    }
    return $true
}

# Read an integer starting at position $pos, skipping leading spaces/tabs.
function Get-IntAt([string]$s, [int]$pos) {
    $i = $pos
    while ($i -lt $s.Length -and ($s[$i] -eq ' ' -or $s[$i] -eq [char]9)) { $i++ }
    $start = $i
    while ($i -lt $s.Length -and $s[$i] -ge '0' -and $s[$i] -le '9') { $i++ }
    if ($i -eq $start) { return $null }
    return [int]::Parse($s.Substring($start, $i - $start))
}

# --- Parse README: collect expected default sizes from the warehouse-expand table ---
# Lines look like:  7A_BackInvWidth<TAB or spaces>18<TAB or spaces>counter-width
$expected = @{}   # key (string) -> default (int)
$readmeLines = $readmeText -split "`n"
foreach ($line in $readmeLines) {
    # Split on whitespace (tab or space), drop empties. Key is token0, value is token1.
    $parts = $line.Split([char[]]@([char]9, ' '), [System.StringSplitOptions]::RemoveEmptyEntries)
    if ($parts.Length -lt 2) { continue }
    $key = $parts[0].Trim()
    $val = $parts[1].Trim()
    $isSizeKey = ($key.StartsWith('7A_') -or $key.StartsWith('7B_') -or $key.StartsWith('7C_') -or $key.StartsWith('7D_') -or $key.StartsWith('7E_') -or $key.StartsWith('7F_')) -and ($key.EndsWith('Width') -or $key.EndsWith('Height'))
    if ($isSizeKey -and (Is-Digits $val)) {
        $expected[$key] = [int]::Parse($val)
    }
}

if ($expected.Count -eq 0) {
    Write-Host 'DOC: No default-size rows found in README warehouse-expand table.'
    exit 1
}
Write-Host ('DOC: README default-size rows found = ' + $expected.Count)

# --- Parse code: for each expected key, find CreateEntry<int>("KEY", N, ...) default N ---
$codeDefaults = @{}   # key -> int
foreach ($key in $expected.Keys) {
    $marker = '("' + $key + '",'
    $pos = $codeText.IndexOf($marker)
    if ($pos -lt 0) {
        Write-Host ('DOC: CreateEntry not found in code for key: ' + $key)
        exit 1
    }
    $comma = $codeText.IndexOf(',', $pos)
    $n = Get-IntAt $codeText ($comma + 1)
    if ($null -eq $n) {
        Write-Host ('DOC: Cannot parse default int for key: ' + $key)
        exit 1
    }
    $codeDefaults[$key] = $n
}

# --- Compare README vs code ---
$ok = $true
foreach ($key in $expected.Keys) {
    $exp = $expected[$key]
    $act = $codeDefaults[$key]
    if ($exp -ne $act) {
        Write-Host ('DOC: Default-size mismatch for ' + $key)
        Write-Host ('      README (doc)  : ' + $exp)
        Write-Host ('      Code (default): ' + $act)
        Write-Host '      Fix: edit SaveBagExpandConfig.cs CreateEntry default OR the README table so they match.'
        $ok = $false
    }
}

# --- Reverse check: every *Width/*Height CreateEntry<int> in code must be documented ---
# Prevents adding a new container size in code but forgetting the README.
$scan = 0
while (($scan = $codeText.IndexOf('CreateEntry<int>("7', $scan)) -ge 0) {
    $q1 = $codeText.IndexOf('"', $scan)
    $q2 = $codeText.IndexOf('"', $q1 + 1)
    $key = $codeText.Substring($q1 + 1, $q2 - $q1 - 1)
    $isSize = ($key.EndsWith('Width') -or $key.EndsWith('Height'))
    if ($isSize -and -not $expected.ContainsKey($key)) {
        Write-Host ('DOC: Code has size entry not documented in README: ' + $key)
        $ok = $false
    }
    $scan = $q2 + 1
}

if (-not $ok) {
    Write-Host 'DOC: Default-size consistency check FAILED.'
    exit 1
}

Write-Host 'DOC: Default-size consistency check passed.'
exit 0
