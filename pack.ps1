# ============================================================================
# ContainerSuite one-click pack script.
#
# Usage:
#   .\pack.ps1                  build + pack into dist/ContainerSuite-v{ver}/ + .zip
#   .\pack.ps1 -SkipBuild       skip dotnet build (reuse current bin/Release DLL)
#   .\pack.ps1 -NoZip           produce the folder only, skip the .zip
#   .\pack.ps1 -Version 1.2.3   override the version (normally read from SuiteVersion.cs)
#
# Build variant:
#   This script builds the LOG-ENABLED (DIAG) variant by default:
#     -p:Diag=true  -> #if DIAG probes compiled in, Banner shows component versions.
#   To produce a clean build without probes, build manually with:
#     dotnet build -c Release -p:Diag=false
#   The DLL filename is the same either way: CustomInfrastructure.dll
#
# Output contract (dist/ContainerSuite-v{ver}/):
#   - CustomInfrastructure.dll   (the single deliverable DLL)
#   - README.md.txt              (player readme, shipped)
#   - README_mobile.txt          (player readme, shipped)
#
# Conventions:
#   - Pure ASCII source (PS 5.1 reads scripts as ANSI; non-ASCII in code
#     would break parsing).
#   - Backslash is NOT an escape char in PowerShell (backtick is); backslashes
#     below are literal path separators.
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

# Single deliverable: one DLL name, one dist folder name.
$DllName   = 'CustomInfrastructure.dll'
$DistLabel = 'ContainerSuite'
$BuiltDll  = Join-Path $Workspace ('ContainerSuite.Infrastructure\bin\Release\' + $DllName)

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

# --- 2. Check the shipped README docs exist (fast-fail, before build) ------
$ShippedDocs = @('README.md.txt', 'README_mobile.txt')

$docOk = $true
foreach ($doc in $ShippedDocs) {
    $path = Join-Path $Workspace $doc
    if (-not (Test-Path $path)) {
        Write-Host ('PACK: shipped doc missing: ' + $doc)
        $docOk = $false
    }
}

if (-not $docOk) {
    Write-Host 'PACK: shipped-doc check FAILED. Nothing was built or packed.'
    exit 1
}
Write-Host 'PACK: shipped-doc check passed.'

# --- 3. Build --------------------------------------------------------------
if (-not $SkipBuild) {
    Write-Host 'PACK: building (Release, Diag=true / log-enabled)...'
    & dotnet build $CfgProj -c Release -v m -p:Diag=true
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
$DistDir = Join-Path $Workspace ('dist\' + $DistLabel + '-v' + $Version)
New-Item -Path $DistDir -ItemType Directory -Force | Out-Null
Write-Host ('PACK: dist dir = ' + $DistDir)

# --- 5. Copy DLL -----------------------------------------------------------
Copy-Item -Path $BuiltDll -Destination $DistDir -Force
Write-Host ('PACK: copied ' + $DllName)

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