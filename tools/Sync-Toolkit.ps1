<#
.SYNOPSIS
    Vendors a pinned dbce-wheel-mod-toolkit release into a consumer repo,
    without overwriting anything that was edited locally.

.DESCRIPTION
    Consumers do not reference this repo's projects; they vendor BUILT artifacts
    pinned by version, so a toolkit change never reaches a game without an
    explicit, diffable bump. Copy this script into the consumer's tools/ folder
    and run it there. It downloads the release zip (or uses a local build),
    extracts the pieces the consumer needs into lib\toolkit\, and writes
    lib\toolkit\VERSION.

    LOCAL EDITS ARE NEVER CLOBBERED. After each sync the script records a
    SHA-256 for every file it wrote, in lib\toolkit\MANIFEST.txt. On the next
    sync a destination file whose hash no longer matches that record was edited
    here, so it is LEFT ALONE and reported, and the script exits non-zero. This
    matters most for profiles\force-profiles.ini: a tune someone arrived at by
    driving is not something a routine version bump should silently delete.

    Resolving one is a choice, not a merge:
      -Force        take the toolkit's copy; the local file is saved first as
                    <name>.local-<timestamp> so nothing is actually lost
      (do nothing)  keep the local file and stay behind on that one file

    Better still, do not edit a vendored file at all. Put a tune in
    force-profiles.user.ini beside the game, which nothing here ever touches,
    and promote it into the toolkit with tools\Import-Profile.ps1 when it is
    worth keeping.

    The native DLL keeps its name (WheelFfb.dll): the managed binding imports
    "WheelFfb", so a consumer that uses Dbce.Wheel.Ffb must ship it under that
    name.

.PARAMETER Version
    Release tag to vendor, e.g. v0.3.0.

.PARAMETER LocalRepo
    Use a local checkout's build output instead of a GitHub release (development).

.PARAMETER Destination
    Folder to populate. Default lib\toolkit under the consumer repo root.

.PARAMETER Parts
    Which artifact groups to copy: native (x64 WheelFfb.dll), native-x86 (the
    32-bit build, for 32-bit games), include (wheelffb.h C ABI + loader,
    forza_packet.h, force_model.h, force_profile.h), profiles
    (force-profiles.ini, which the game DEPLOYS and reads at runtime),
    powershell (DbceWheel.psm1 installer helpers), dotnet, tools, knowledge.
    Default native,dotnet. A native mod that produces force typically takes
    native-x86,include,profiles; an installer takes powershell.

.PARAMETER Force
    Overwrite locally edited files, after saving each one alongside.

.EXAMPLE
    .\tools\Sync-Toolkit.ps1 -Version v0.3.0 -Parts include,native-x86,profiles
    .\tools\Sync-Toolkit.ps1 -LocalRepo E:\Source\dbce-wheel-mod-toolkit -Parts profiles -Force
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$LocalRepo,
    [string]$Destination,
    [string[]]$Parts = @('native', 'dotnet'),
    [switch]$Force,
    [string]$Repo = 'd-b-c-e/dbce-wheel-mod-toolkit'
)

$ErrorActionPreference = 'Stop'
# powershell -File passes "-Parts a,b" as one string; accept both spellings.
$Parts = @($Parts | ForEach-Object { $_ -split ',' } | Where-Object { $_ })
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if (-not $Destination) { $Destination = Join-Path $root 'lib\toolkit' }
New-Item -ItemType Directory -Force -Path $Destination | Out-Null
$Destination = (Resolve-Path $Destination).Path

$manifestPath = Join-Path $Destination 'MANIFEST.txt'
$recorded = @{}
$hadManifest = Test-Path $manifestPath
if ($hadManifest) {
    foreach ($line in Get-Content $manifestPath) {
        $t = $line.Trim()
        if (-not $t -or $t.StartsWith('#')) { continue }
        $i = $t.IndexOf(' ')
        if ($i -gt 0) { $recorded[$t.Substring($i).Trim()] = $t.Substring(0, $i).Trim() }
    }
}

function Get-Sha256($path) { (Get-FileHash -Path $path -Algorithm SHA256).Hash }

$written = @{}
$skipped = New-Object System.Collections.Generic.List[string]
$replaced = New-Object System.Collections.Generic.List[string]

function Copy-Vendored {
    param([string]$SourceFile, [string]$TargetDir)

    $name = Split-Path $SourceFile -Leaf
    $dest = Join-Path $TargetDir $name
    $rel = $dest.Substring($Destination.Length).TrimStart('\', '/')
    $srcHash = Get-Sha256 $SourceFile

    if (Test-Path $dest) {
        $destHash = Get-Sha256 $dest
        if ($destHash -eq $srcHash) { $script:written[$rel] = $srcHash; return }   # already current

        # No manifest at all means this is the first sync with edit protection;
        # assume the tree is pristine rather than crying wolf on every file.
        $locallyEdited = $false
        if ($hadManifest) {
            $known = $recorded[$rel]
            $locallyEdited = (-not $known) -or ($destHash -ne $known)
        }

        if ($locallyEdited -and -not $Force) {
            $script:skipped.Add($rel)
            # Deliberately NO manifest entry. Recording the edited hash would make
            # the file look pristine on the next run, so the edit would be found
            # once and then silently overwritten later - with -Force skipping the
            # backup too, because it would no longer believe anything was edited.
            # Leaving it out keeps the file flagged until someone resolves it.
            return
        }
        if ($locallyEdited -and $Force) {
            $backup = "$dest.local-" + (Get-Date -Format 'yyyyMMdd-HHmmss')
            Copy-Item $dest $backup -Force
            $script:replaced.Add("$rel  (yours saved as " + (Split-Path $backup -Leaf) + ")")
        }
    }

    Copy-Item $SourceFile $dest -Force
    $script:written[$rel] = $srcHash
}

$stage = Join-Path $env:TEMP ("wheel-toolkit-" + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Force -Path $stage | Out-Null
try {
    if ($LocalRepo) {
        Write-Host "Using local build at $LocalRepo" -ForegroundColor Cyan
        $src = @{
            native       = Join-Path $LocalRepo 'native\wheelffb\build'
            'native-x86' = Join-Path $LocalRepo 'native\wheelffb\build\x86'
            include      = @((Join-Path $LocalRepo 'native\wheelffb\include'), (Join-Path $LocalRepo 'native\forza'), (Join-Path $LocalRepo 'native\forcemodel'))
            profiles     = Join-Path $LocalRepo 'profiles'
            powershell   = Join-Path $LocalRepo 'tools\powershell'
            dotnet       = Join-Path $LocalRepo 'dotnet\Dbce.Wheel.Ffb\bin\Release\netstandard2.0'
            tools        = Join-Path $LocalRepo 'tools'
            knowledge    = Join-Path $LocalRepo 'knowledge'
        }
        $ver = 'local'
        try { $ErrorActionPreference = 'Continue'; $v = & git -C $LocalRepo describe --tags --always 2>$null; if ($LASTEXITCODE -eq 0 -and $v) { $ver = "$v" } } catch { } finally { $ErrorActionPreference = 'Stop' }
    } else {
        if (-not $Version) { throw "Pass -Version vX.Y.Z or -LocalRepo <path>" }
        Write-Host "Downloading $Repo $Version" -ForegroundColor Cyan
        gh release download $Version --repo $Repo --pattern 'dbce-wheel-mod-toolkit-*.zip' --dir $stage
        $zip = Get-ChildItem $stage -Filter '*.zip' | Select-Object -First 1
        if (-not $zip) { throw "No release zip found for $Version" }
        Expand-Archive $zip.FullName -DestinationPath (Join-Path $stage 'x') -Force
        $inner = Get-ChildItem (Join-Path $stage 'x') -Directory | Select-Object -First 1
        $src = @{
            native       = Join-Path $inner.FullName 'native'
            'native-x86' = Join-Path $inner.FullName 'native\x86'
            include      = @((Join-Path $inner.FullName 'native\include'))
            profiles     = Join-Path $inner.FullName 'profiles'
            powershell   = Join-Path $inner.FullName 'tools\powershell'
            dotnet       = Join-Path $inner.FullName 'dotnet'
            tools        = Join-Path $inner.FullName 'tools'
            knowledge    = Join-Path $inner.FullName 'knowledge'
        }
        $ver = $Version
    }

    foreach ($part in $Parts) {
        $from = $src[$part]
        if (-not $from) { Write-Warning "unknown part '$part' (native, native-x86, include, profiles, powershell, dotnet, tools, knowledge)"; continue }
        $missing = @($from | Where-Object { -not (Test-Path $_) })
        if ($missing) { Write-Warning "part '$part' not found at $($missing -join ', ')"; continue }
        $to = Join-Path $Destination $part
        if ($part -eq 'native-x86') { $to = Join-Path $Destination 'native\x86' }
        New-Item -ItemType Directory -Force -Path $to | Out-Null

        $files = switch ($part) {
            'native'     { @(Join-Path $from 'WheelFfb.dll') }
            'native-x86' { @(Join-Path $from 'WheelFfb.dll') }
            'include'    { @($from | ForEach-Object { Get-ChildItem (Join-Path $_ '*.h') -File -ErrorAction SilentlyContinue | ForEach-Object FullName }) }
            'profiles'   { @(Join-Path $from 'force-profiles.ini') }
            'powershell' { @(Get-ChildItem (Join-Path $from '*.psm1') -File -ErrorAction SilentlyContinue | ForEach-Object FullName) }
            'dotnet'     { @(Get-ChildItem (Join-Path $from 'Dbce.Wheel.*.dll') -File -ErrorAction SilentlyContinue | ForEach-Object FullName) +
                           @(Get-ChildItem (Join-Path $from 'Dbce.Wheel.*.xml') -File -ErrorAction SilentlyContinue | ForEach-Object FullName) }
            default      { @(Get-ChildItem $from -File -Recurse | ForEach-Object FullName) }
        }
        foreach ($f in $files) { if ($f -and (Test-Path $f)) { Copy-Vendored -SourceFile $f -TargetDir $to } }
        Write-Host "  $part -> $to" -ForegroundColor Green
    }

    $lines = @(
        "# Written by Sync-Toolkit.ps1. SHA-256 of every vendored file as it was",
        "# placed here. The next sync compares against this to tell a routine",
        "# update apart from something edited locally, and refuses to overwrite",
        "# the second kind. Do not edit by hand.",
        "# toolkit $ver  $(Get-Date -Format s)"
    ) + ($written.GetEnumerator() | Sort-Object Name | ForEach-Object { "$($_.Value)  $($_.Name)" })
    Set-Content -Path $manifestPath -Value $lines
    Set-Content (Join-Path $Destination 'VERSION') "$ver`n$(Get-Date -Format s)"

    if ($replaced.Count) {
        Write-Host "`nOverwritten (-Force), your copy kept alongside:" -ForegroundColor Yellow
        foreach ($r in $replaced) { Write-Host "  $r" -ForegroundColor Yellow }
    }
    if ($skipped.Count) {
        Write-Host "`nEdited here, so LEFT ALONE and still on the old content:" -ForegroundColor Red
        foreach ($s in $skipped) { Write-Host "  $s" -ForegroundColor Red }
        Write-Host ""
        if ($skipped -contains 'profiles\force-profiles.ini') {
            Write-Host "  Your tune is safe, but it is now diverging from the toolkit. Promote it:" -ForegroundColor Gray
            Write-Host "     Import-Profile.ps1 -From <game folder> -Id <name@version>" -ForegroundColor Gray
            Write-Host "  and keep per-player tweaks in force-profiles.user.ini, never touched by a sync." -ForegroundColor Gray
        }
        Write-Host "  Re-run with -Force to take the toolkit's copy (yours is saved alongside)." -ForegroundColor Gray
        Write-Host "`nVendored $ver into $Destination; $($skipped.Count) file(s) left as they were" -ForegroundColor Yellow
        exit 1
    }

    Write-Host "Vendored $ver into $Destination" -ForegroundColor Green
    if (-not $hadManifest) {
        Write-Host "Wrote MANIFEST.txt; from the next sync on, local edits are detected and kept." -ForegroundColor DarkGray
    }
    exit 0
} finally {
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
