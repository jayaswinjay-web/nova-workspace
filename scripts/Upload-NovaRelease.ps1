#Requires -Version 5.1
<#
.SYNOPSIS
    Publish publish\NOVAWorkspace.zip as release assets on the private GitHub repo.

.DESCRIPTION
    GitHub's release-asset upload endpoint rejects requests that take too long, and
    the suite zip is ~154 MB. This script splits the zip into fixed-size byte parts
    (NOVAWorkspace.zip.part01, .part02, ...) and uploads each part as its own asset.
    Concatenated in name order the parts are byte-for-byte the original zip, and the
    Cloudflare Worker at /download streams them back as one file.

    Already-uploaded parts (same name and size) are skipped, so re-running the script
    resumes an interrupted publish.

.PARAMETER PartMB
    Size of each part in mebibytes. Default 16. Keep each part well under GitHub's
    upload window on slow links.

.PARAMETER Parallel
    Number of concurrent uploads. Default 2.

.EXAMPLE
    .\Upload-NovaRelease.ps1
#>
[CmdletBinding()]
param(
    [string]$Repo    = 'jayaswinjay-web/nova-workspace',
    [string]$Tag     = 'v1.0.0',
    [string]$ZipPath = '',
    [int]$PartMB     = 16,
    [int]$Parallel   = 2,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

if (-not $ZipPath) {
    $ZipPath = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) '..\publish\NOVAWorkspace.zip'
}
if (-not (Test-Path $ZipPath)) { throw "Zip not found: $ZipPath" }
$zip = (Resolve-Path $ZipPath).Path
$zipLen = (Get-Item $zip).Length
$partBytes = [int64]$PartMB * 1MB
$partCount = [int][math]::Ceiling($zipLen / $partBytes)
Write-Host ("Source : {0} ({1:N1} MB)" -f $zip, ($zipLen / 1MB))
Write-Host ("Parts  : {0} x {1} MB" -f $partCount, $PartMB)

# ── split ────────────────────────────────────────────────────────────────────
$baseName = [System.IO.Path]::GetFileName($zip) # e.g. NOVAWorkspace.zip or NOVAWorkspace-win-x86.zip
$partDir = Join-Path $env:TEMP ("nova-parts-" + [guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Force -Path $partDir | Out-Null
Write-Host "Splitting into $partDir"

$in = [System.IO.File]::OpenRead($zip)
try {
    $buffer = New-Object byte[] (4MB)
    for ($i = 1; $i -le $partCount; $i++) {
        $name = '{0}.part{1:D2}' -f $baseName, $i
        $path = Join-Path $partDir $name
        $out = [System.IO.File]::Create($path)
        try {
            $remaining = [math]::Min($partBytes, $zipLen - $in.Position)
            while ($remaining -gt 0) {
                $want = [int][math]::Min([int64]$buffer.Length, $remaining)
                $read = $in.Read($buffer, 0, $want)
                if ($read -le 0) { throw "Unexpected end of $zip" }
                $out.Write($buffer, 0, $read)
                $remaining -= $read
            }
        } finally { $out.Close() }
        Write-Host ("  {0}  {1:N1} MB" -f $name, ((Get-Item $path).Length / 1MB))
    }
} finally { $in.Close() }

# ── what is already on the release? ──────────────────────────────────────────
function Get-Release {
    $all = gh api "repos/$Repo/releases?per_page=100" | ConvertFrom-Json
    $rel = $all | Where-Object { $_.tag_name -eq $Tag } | Select-Object -First 1
    if (-not $rel) {
        Write-Host ("Creating release {0}..." -f $Tag)
        $notes = @(
            "NOVA Workspace - the office suite that is not Microsoft.",
            "",
            "Free for 30 days, then 6 rupees per month for one person.",
            "Unzip anywhere and double-click Start NOVA Desk.vbs (Windows packages).",
            "",
            "Downloads may be published as platform zips and/or byte-range parts (*.zip.partNN).",
            "Concatenated in name order, parts are the original zip; the website Worker streams them as one file."
        ) -join "`n"
        $notesFile = Join-Path $env:TEMP ("nova-notes-" + [guid]::NewGuid().ToString('N') + ".md")
        [System.IO.File]::WriteAllText($notesFile, $notes, (New-Object System.Text.UTF8Encoding($false)))
        # Created as a draft: the website reads /releases/latest, so the release
        # must not go live until every part is uploaded and verified.
        gh release create $Tag --repo $Repo --draft --title "NOVA Workspace $Tag" --notes-file $notesFile | Out-Null
        Remove-Item $notesFile -Force -ErrorAction SilentlyContinue
        # The releases list can lag a moment behind the create call (it did for v1.0.5:
        # the draft existed but the next GET did not list it yet), so poll briefly
        # instead of failing a publish that actually succeeded.
        for ($i = 0; $i -lt 10 -and -not $rel; $i++) {
            if ($i -gt 0) { Start-Sleep -Seconds 2 }
            $all = gh api "repos/$Repo/releases?per_page=100" | ConvertFrom-Json
            $rel = $all | Where-Object { $_.tag_name -eq $Tag } | Select-Object -First 1
        }
    }
    if (-not $rel) { throw "Release $Tag not found in $Repo" }
    return $rel
}

$release = Get-Release
$releaseId = $release.id
$existing = @{}
foreach ($a in $release.assets) { $existing[$a.name] = [int64]$a.size }

# A part already on the release is skipped by name and size, which is enough to resume an
# upload of the same zip that was interrupted. It is NOT enough when the zip has changed: a
# rebuild of the same size still differs (a zip stores its files' timestamps), and GitHub keeps
# no content hash of an asset to compare against. A changed zip therefore needs -Force, which
# re-uploads every part and costs the same 25 seconds as the first upload did.
$parts = Get-ChildItem $partDir -File | Sort-Object Name
$todo = @()
foreach ($p in $parts) {
    if (-not $Force -and $existing.ContainsKey($p.Name) -and $existing[$p.Name] -eq $p.Length) {
        Write-Host ("skip   {0} (already uploaded)" -f $p.Name)
    } else {
        $todo += $p
    }
}
Write-Host ("To upload: {0} part(s)" -f $todo.Count)

# ── upload with limited parallelism ──────────────────────────────────────────
$sw = [System.Diagnostics.Stopwatch]::StartNew()
$queue = [System.Collections.Queue]::new()
foreach ($t in $todo) { $queue.Enqueue($t) }
$running = @()
$failed = @()

while ($queue.Count -gt 0 -or $running.Count -gt 0) {
    while ($queue.Count -gt 0 -and $running.Count -lt $Parallel) {
        $file = $queue.Dequeue()
        Write-Host ("start  {0}" -f $file.Name)
        $running += Start-Job -ScriptBlock {
            param($repo, $tag, $path, $name)
            $out = gh release upload $tag $path --repo $repo --clobber 2>&1 | Out-String
            [pscustomobject]@{ Name = $name; Exit = $LASTEXITCODE; Output = $out }
        } -ArgumentList $Repo, $Tag, $file.FullName, $file.Name
    }

    $done = $running | Wait-Job -Any
    $result = $done | Receive-Job
    $done | Remove-Job
    $running = @($running | Where-Object { $_.State -ne 'Completed' })

    if ($result.Exit -ne 0) {
        $failed += $result.Name
        Write-Host ("FAIL   {0}" -f $result.Name) -ForegroundColor Red
        Write-Host $result.Output
    } else {
        Write-Host ("ok     {0}  (elapsed {1:N0}s)" -f $result.Name, $sw.Elapsed.TotalSeconds)
    }
}
$sw.Stop()

Write-Host ''
if ($failed.Count -gt 0) {
    Write-Host ("Uploaded with failures: {0}" -f ($failed -join ', ')) -ForegroundColor Red
    Write-Host 'Re-run the script to retry only the missing parts.'
    exit 1
}

# ── verify sizes on the release ──────────────────────────────────────────────
$release = gh api "repos/$Repo/releases/$releaseId" | ConvertFrom-Json
$total = 0
foreach ($p in $parts) {
    $asset = $release.assets | Where-Object { $_.name -eq $p.Name }
    if (-not $asset) { throw "Missing asset after upload: $($p.Name)" }
    if ([int64]$asset.size -ne $p.Length) { throw "Size mismatch for $($p.Name)" }
    $total += [int64]$asset.size
}
if ($total -ne $zipLen) { throw "Part total $total != zip size $zipLen" }

Write-Host ("All {0} parts uploaded and verified ({1:N1} MB total) in {2:N0}s." -f $parts.Count, ($total / 1MB), $sw.Elapsed.TotalSeconds) -ForegroundColor Green

# Publish the release so /releases/latest on the website picks it up.
$isDraft = (gh api "repos/$Repo/releases/$releaseId" | ConvertFrom-Json).draft
if ($isDraft) {
    Write-Host 'Publishing the release...'
    gh api -X PATCH "repos/$Repo/releases/$releaseId" -F draft=false | Out-Null
}

Remove-Item $partDir -Recurse -Force -ErrorAction SilentlyContinue
