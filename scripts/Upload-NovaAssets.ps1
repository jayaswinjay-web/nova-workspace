# Upload one or more files to a GitHub release as single assets (no splitting).
# Used for the installers and the .deb; the big portable zips go through
# Upload-NovaRelease.ps1, which splits them into byte-range parts.

[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Repo,
    [Parameter(Mandatory)][string]$Tag,
    [Parameter(Mandatory, ValueFromRemainingArguments)][string[]]$Files
)

$ErrorActionPreference = 'Stop'
$gh = Get-Command gh -ErrorAction SilentlyContinue
if (-not $gh) { throw "gh CLI not found" }

& gh release view $Tag --repo $Repo *> $null
if ($LASTEXITCODE -ne 0) {
    & gh release create $Tag --repo $Repo --title "NOVA Workspace $Tag" --notes "NOVA Workspace release"
    if ($LASTEXITCODE -ne 0) { throw "could not find or create release $Tag" }
}

foreach ($f in $Files) {
    $item = Get-Item $f
    $hash = (Get-FileHash $f -Algorithm SHA256).Hash
    Write-Host ("upload {0}  {1:N0} bytes  SHA-256 {2}" -f $item.Name, $item.Length, $hash)
    & gh release upload $Tag $item.FullName --repo $Repo --clobber
    if ($LASTEXITCODE -ne 0) { throw "upload failed: $($item.Name)" }
}
Write-Host "done: $($Files.Count) asset(s) on $Tag"
