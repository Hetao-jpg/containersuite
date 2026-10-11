# P4 (2026-10-02): Suite version consistency check.
# Called by _shared\CommonMod.props before building ContainerSuite.Infrastructure.
# Exit 0 = consistent, exit 1 = inconsistent or error.
#
# Rules to avoid PowerShell parser pitfalls:
#  - Pure ASCII (avoid PS 5.1 encoding issues).
#  - No regex.
#  - No backslash escapes (PowerShell escape char is backtick, not backslash).
#  - String output uses '+' concatenation; literals never contain " or [ or ].

param(
    [Parameter(Mandatory = $true)]
    [string]$WorkspaceRoot
)

$ErrorActionPreference = 'Stop'

# --- 1. Read Suite version from SuiteVersion.cs ---
$suitePath = Join-Path $WorkspaceRoot 'ContainerSuite.Shared\SuiteVersion.cs'
if (-not (Test-Path $suitePath)) {
    Write-Host ('P4: SuiteVersion.cs not found: ' + $suitePath)
    exit 1
}

$suiteText = Get-Content -Path $suitePath -Raw

$needleSuite = 'Suite = "'
$idx = $suiteText.IndexOf($needleSuite)
if ($idx -lt 0) {
    Write-Host ('P4: Marker Suite-assignment not found in ' + $suitePath)
    exit 1
}
$start = $idx + $needleSuite.Length
$end = $suiteText.IndexOf('"', $start)
if ($end -lt 0) {
    Write-Host ('P4: Closing quote not found after Suite = in ' + $suitePath)
    exit 1
}
$suiteVer = $suiteText.Substring($start, $end - $start)
$expected = $suiteVer + '.0'

Write-Host ('P4: Suite version = ' + $suiteVer + ' (expect AssemblyVersion = ' + $expected + ')')

# --- 2. Check each AssemblyInfo.cs ---
# 2026-10-06: 4 DLLs merged into 1. The other three AssemblyInfo.cs were
# removed (they would cause CS0579 now), so only the Infrastructure one
# remains and is the single place AssemblyVersion lives.
$assemblyInfos = @(
    'ContainerSuite.Infrastructure\AssemblyInfo.cs'
)

$needleAsm = 'AssemblyVersion("'
$ok = $true

foreach ($rel in $assemblyInfos) {
    $path = Join-Path $WorkspaceRoot $rel
    if (-not (Test-Path $path)) {
        Write-Host ('P4: AssemblyInfo.cs not found: ' + $path)
        $ok = $false
        continue
    }

    $text = Get-Content -Path $path -Raw
    $pos = 0
    $found = $false

    while (($pos = $text.IndexOf($needleAsm, $pos)) -ge 0) {
        $found = $true
        $s = $pos + $needleAsm.Length
        $e = $text.IndexOf('"', $s)
        if ($e -lt 0) { break }

        $ver = $text.Substring($s, $e - $s)
        if ($ver -ne $expected) {
            Write-Host ('P4: Version mismatch in ' + $path)
            Write-Host ('      found    : ' + $ver)
            Write-Host ('      expected : ' + $expected)
            Write-Host ('      Fix: update SuiteVersion.cs and AssemblyInfo.cs together.')
            $ok = $false
        }
        $pos = $e + 1
    }

    if (-not $found) {
        Write-Host ('P4: No AssemblyVersion found in: ' + $path)
        $ok = $false
    }
}

# --- 3. No hardcoded 1.7.x version literal in source (2026-10-10, P0-2) ---
#   MelonLoader builds the Harmony ID as "<FullName>:<MelonInfo.Name>", and
#   FullName embeds "Version=<AssemblyVersion>". Writing that version as a
#   literal in a comment (e.g. the 2026-10-10 probe-log quotes in
#   AssemblyInfo.cs) makes the comment drift the moment the version bumps.
#   Use the <AssemblyVersion> placeholder instead. Rule: the literal
#   "Version=1.7." must not appear in any .cs of the 4 managed projects.
$scanDirs = @(
    'ContainerSuite.Shared',
    'ContainerSuite.Infrastructure',
    'ContainerSuite.SaveBag',
    'ContainerSuite.DIYUpgrade'
)
$staleNeedle = 'Version=1.7.'
foreach ($dir in $scanDirs) {
    $fullDir = Join-Path $WorkspaceRoot $dir
    if (-not (Test-Path $fullDir)) { continue }
    $csFiles = Get-ChildItem -Path $fullDir -Recurse -Filter '*.cs' -File |
        Where-Object { $_.FullName -notlike '*\bin\*' -and $_.FullName -notlike '*\obj\*' }
    foreach ($cf in $csFiles) {
        $ct = [System.IO.File]::ReadAllText($cf.FullName)
        if ($ct.Contains($staleNeedle)) {
            Write-Host ('P4: hardcoded version literal "' + $staleNeedle + '" found in ' + $cf.FullName)
            Write-Host ('      Fix: replace the version digits with the <AssemblyVersion> placeholder.')
            $ok = $false
        }
    }
}

if (-not $ok) {
    Write-Host 'P4: Version consistency check FAILED.'
    exit 1
}

Write-Host 'P4: Version consistency check passed.'
exit 0