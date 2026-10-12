# ============================================================================
# ContainerSuite one-click pack script.
#
# Usage:
#   .\pack.ps1                  build + pack into dist/ContainerSuite-v{ver}/ + .zip
#   .\pack.ps1 -SkipBuild       skip msbuild (reuse current bin/Release DLL)
#   .\pack.ps1 -NoZip           produce the folder only, skip the .zip
#   .\pack.ps1 -Version 1.2.3   override the version (must match the doc version
#                               lines, else packing is rejected)
#
# Output contract (dist/ContainerSuite-v{ver}/):
#   - CustomInfrastructure.dll   (the single deliverable DLL)
#   - README.md.txt              (player readme, shipped)
#   - README_mobile.txt          (player readme, shipped)
#
# Version line contract: each shipped README must contain a version line
#   <version-label> + colon (fullwidth or halfwidth) + optional v + x.y.z
# where version-label is the Chinese word for "version" or "latest version".
# and that x.y.z must equal the pack version, otherwise packing is rejected.
# This validation runs BEFORE the build (fast-fail) so a bad doc never wastes
# a compile.
#
# Conventions (the old _shared\check-*.ps1 guards were removed 2026-10-12;
# these rules still apply):
#   - Pure ASCII source (PS 5.1 reads scripts as ANSI; non-ASCII in code
#     would break parsing).
#   - Backslash is NOT an escape char in PowerShell (backtick is); backslashes
#     below are literal path separators.
#   - Regex uses \uXXXX escapes so the source stays pure ASCII.
# ============================================================================

param(
    [switch]$SkipBuild,
    [switch]$NoZip,
    [string]$Version = ''
)

$ErrorActionPreference = 'Stop'

# --- Paths -----------------------------------------------------------------
$Root      = $PSScriptRoot
$Workspace = $Root                                   # ContainerSuite\
$CfgProj   = Join-Path $Workspace 'ContainerSuite.Infrastructure\ContainerSuite.Infrastructure.csproj'
$BuiltDll  = Join-Path $Workspace 'ContainerSuite.Infrastructure\bin\Release\CustomInfrastructure.dll'

# --- 1. Resolve version ----------------------------------------------------
if ([string]::IsNullOrWhiteSpace($Version)) {
    $suiteCs = Join-Path $Workspace 'ContainerSuite.Shared\SuiteVersion.cs'
    if (-not (Test-Path $suiteCs)) {
        Write-Host ('PACK: SuiteVersion.cs not found: ' + $suiteCs)
        exit 1
    }
    $suiteText = Get-Content -Path $suiteCs -Raw
    $needle = 'Suite = "'
    $i = $suiteText.IndexOf($needle)
    if ($i -lt 0) {
        Write-Host 'PACK: could not read Suite version from SuiteVersion.cs'
        exit 1
    }
    $s = $i + $needle.Length
    $e = $suiteText.IndexOf('"', $s)
    if ($e -lt 0) {
        Write-Host 'PACK: malformed Suite version literal in SuiteVersion.cs'
        exit 1
    }
    $Version = $suiteText.Substring($s, $e - $s)
}

Write-Host ('PACK: version = ' + $Version)

# --- 2. Validate the shipped README docs (fast-fail, before build) ---------
# Each doc must (a) mention the marker "ContainerSuite" and (b) contain a
# version line whose number equals the pack version.
$ShippedDocs = @('README.md.txt', 'README_mobile.txt')

# Version-line regex, ASCII-escaped:
#   (?:\u6700\u65B0)?       optional Chinese "zui xin" (latest)
#   \u7248\u672C             Chinese "ban ben" (version)
#   \s*[\uFF1A:]\s*          colon (fullwidth U+FF1A or halfwidth) + optional spaces
#   [vV]?                    optional leading v
#   ([0-9]+\.[0-9]+\.[0-9]+) captured x.y.z
$verPattern = '(?:\u6700\u65B0)?\u7248\u672C\s*[\uFF1A:]\s*[vV]?([0-9]+\.[0-9]+\.[0-9]+)'

$docOk = $true
foreach ($doc in $ShippedDocs) {
    $path = Join-Path $Workspace $doc
    if (-not (Test-Path $path)) {
        Write-Host ('PACK: shipped doc missing: ' + $doc)
        $docOk = $false
        continue
    }
    $text = Get-Content -Path $path -Raw -Encoding UTF8

    if ($text.IndexOf('ContainerSuite') -lt 0) {
        Write-Host ('PACK: ' + $doc + ' does not contain the marker "ContainerSuite"')
        $docOk = $false
    }

    $m = [regex]::Match($text, $verPattern)
    if (-not $m.Success) {
        Write-Host ('PACK: no version line found in ' + $doc)
        Write-Host ('      expected a line like: version label + colon + ' + $Version)
        $docOk = $false
    }
    elseif ($m.Groups[1].Value -ne $Version) {
        Write-Host ('PACK: version mismatch in ' + $doc)
        Write-Host ('      found    : ' + $m.Groups[1].Value)
        Write-Host ('      expected : ' + $Version)
        $docOk = $false
    }
}

if (-not $docOk) {
    Write-Host 'PACK: shipped-doc validation FAILED. Nothing was built or packed.'
    exit 1
}
Write-Host 'PACK: shipped-doc validation passed.'

# --- 3. Build --------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Host 'PACK: building (Release)...'
    & dotnet build $CfgProj -c Release -v m
    if ($LASTEXITCODE -ne 0) {
        Write-Host 'PACK: build FAILED.'
        exit 1
    }
}

if (-not (Test-Path $BuiltDll)) {
    Write-Host ('PACK: built DLL not found: ' + $BuiltDll)
    exit 1
}

# --- 4. Prepare dist folder ------------------------------------------------
# Note: we deliberately do NOT Remove-Item the folder. Some environments'
# safety hooks intercept PowerShell deletes (moving them to a recycle bin) and
# can fail-closed, aborting the pack. Overwriting in place is sufficient: the
# shipped file set is fixed at 3 files, so there is nothing stale to clean.
$DistDir = Join-Path $Workspace ('dist\ContainerSuite-v' + $Version)
New-Item -Path $DistDir -ItemType Directory -Force | Out-Null
Write-Host ('PACK: dist dir = ' + $DistDir)
# --- 5. Copy DLL -----------------------------------------------------------
Copy-Item -Path $BuiltDll -Destination $DistDir -Force
Write-Host 'PACK: copied CustomInfrastructure.dll'

# --- 6. Copy shipped docs --------------------------------------------------
foreach ($doc in $ShippedDocs) {
    Copy-Item -Path (Join-Path $Workspace $doc) -Destination $DistDir -Force
    Write-Host ('PACK: copied ' + $doc)
}

# --- 7. Zip ----------------------------------------------------------------
if (-not $NoZip) {
    $zipPath = $DistDir + '.zip'
    # Do NOT Remove-Item first: some environments' safety hooks move deletes to
    # the recycle bin and can fail-closed. Compress-Archive -Force overwrites in
    # place on its own.
    Compress-Archive -Path (Join-Path $DistDir '*') -DestinationPath $zipPath -CompressionLevel Optimal -Force
    $zipLen = (Get-Item $zipPath).Length
    Write-Host ('PACK: zip = ' + $zipPath + ' (' + $zipLen + ' bytes)')
}

Write-Host 'PACK: done.'
exit 0
