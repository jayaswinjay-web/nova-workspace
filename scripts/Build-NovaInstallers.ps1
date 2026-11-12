# Build the NOVA Workspace Windows installers.
#
# Each package folder under publish\ becomes one NSIS installer:
#
#   publish\NOVAWorkspace              -> NOVAWorkspace-Setup-<version>-x64.exe
#   publish\NOVAWorkspace-win-x86      -> NOVAWorkspace-Setup-<version>-x86.exe
#   publish\NOVAWorkspace-win-arm64    -> NOVAWorkspace-Setup-<version>-arm64.exe
#
# The installer is per-user (no UAC), so the same script works on Windows 7 SP1
# through 11 and for x86 and ARM64 payloads. It creates Start-menu and desktop
# shortcuts, registers an uninstall entry, and can take over Office/PDF file
# types for the current user.
#
# NSIS is not part of the build environment; pass -Makensis or set NOVA_NSIS.
# A portable NSIS 3.10 tree works:
#   https://sourceforge.net/projects/nsis/files/NSIS%203/3.10/nsis-3.10.zip

[CmdletBinding()]
param(
    [string]$Version = '1.0.7',
    [string]$Makensis = $env:NOVA_NSIS,
    [string]$OutDir = '',
    [switch]$SkipMissing
)

$ErrorActionPreference = 'Stop'
$repo = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
if (-not $OutDir) { $OutDir = Join-Path $repo 'publish' }
$script = Join-Path $repo 'packaging\windows\installer.nsi'
$icons = Join-Path $repo 'assets\nova'

if (-not $Makensis) {
    $candidates = @(
        'C:\Program Files (x86)\NSIS\makensis.exe',
        'C:\Program Files\NSIS\makensis.exe',
        'F:\vhtemp\opencode\nsis\nsis-3.10\makensis.exe'
    )
    $Makensis = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $Makensis -or -not (Test-Path $Makensis)) {
    throw "makensis.exe not found. Pass -Makensis <path> or set NOVA_NSIS."
}

$targets = @(
    @{ Arch = 'x64';   Dir = 'NOVAWorkspace';           Desk = 1 },
    @{ Arch = 'x86';   Dir = 'NOVAWorkspace-win-x86';   Desk = 1 },
    @{ Arch = 'arm64'; Dir = 'NOVAWorkspace-win-arm64'; Desk = 0 }
)

foreach ($t in $targets) {
    $src = Join-Path $repo (Join-Path 'publish' $t.Dir)
    if (-not (Test-Path $src)) {
        if ($SkipMissing) { Write-Warning "skipping $($t.Arch): $src is missing"; continue }
        throw "missing package folder: $src"
    }
    $exe = Join-Path $OutDir ("NOVAWorkspace-Setup-{0}.exe" -f $t.Arch)
    Write-Host "== $($t.Arch) <- $src"
    & $Makensis /V3 `
        "/DARCH=$($t.Arch)" `
        "/DSRCDIR=$src" `
        "/DICONDIR=$icons" `
        "/DHASDESK=$($t.Desk)" `
        $script
    if ($LASTEXITCODE -ne 0) { throw "makensis failed for $($t.Arch) (exit $LASTEXITCODE)" }
    $built = Join-Path $repo ("packaging\windows\NOVAWorkspace-Setup-{0}.exe" -f $t.Arch)
    if (-not (Test-Path $built)) { throw "makensis produced no installer at $built" }
    Move-Item $built $exe -Force
    $item = Get-Item $exe
    $hash = (Get-FileHash $exe -Algorithm SHA256).Hash
    Write-Host ("   {0}  {1:N0} bytes  SHA-256 {2}" -f $item.Name, $item.Length, $hash)
}
