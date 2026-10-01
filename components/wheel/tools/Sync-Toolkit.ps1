<# Vendors built toolkit artifacts as a transaction. Every requested part and local
   edit is checked before replacing the destination. Failed commits roll back.
   -Force retains local edits alongside the replacement. Unselected files and their
   manifest entries survive partial syncs. Load supports renamed native DLLs in 0.12+.
   -LocalRepo records its full revision and dirty state; it never claims a release tag. #>
[CmdletBinding()]
param([string]$Version, [string]$LocalRepo, [string]$Destination,
      [string[]]$Parts = @('native','dotnet'), [switch]$Force,
      [string]$Repo = 'd-b-c-e/dbce-wheel-mod-toolkit')
$ErrorActionPreference = 'Stop'
$Parts = @($Parts | ForEach-Object { $_ -split ',' } | Where-Object { $_ } | Select-Object -Unique)
$valid = @('native','native-x86','include','profiles','powershell','dotnet','tools','knowledge')
if ($Parts.Count -eq 0 -or @($Parts | Where-Object { $_ -notin $valid }).Count) { throw 'Unknown or empty Parts selection' }
if ([bool]$Version -eq [bool]$LocalRepo) { throw 'Specify exactly one of -Version or -LocalRepo' }
$root = Split-Path -Parent $PSScriptRoot
if (-not $Destination) { $Destination = Join-Path $root 'lib/toolkit' }
$Destination = [IO.Path]::GetFullPath($Destination).TrimEnd('\','/')
$parent = Split-Path -Parent $Destination
if (-not $parent -or $Destination -eq [IO.Path]::GetPathRoot($Destination).TrimEnd('\','/')) { throw 'Destination must be a directory below a parent' }
function Assert-Unlinked([string]$Path) {
    $cursor = $Path
    while ($cursor) {
        if (Test-Path -LiteralPath $cursor) {
            if ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Linked path refused: $cursor" }
        }
        $cursor = Split-Path -Parent $cursor
    }
}
Assert-Unlinked $Destination
if (Test-Path -LiteralPath $Destination) {
    if (-not (Test-Path -LiteralPath $Destination -PathType Container)) { throw 'Destination is not a directory' }
    foreach ($entry in Get-ChildItem -LiteralPath $Destination -Force -Recurse) {
        if ($entry.Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Linked destination entry: $($entry.FullName)" }
    }
}
$download = Join-Path ([IO.Path]::GetTempPath()) ('toolkit-download-' + [guid]::NewGuid().ToString('N'))
$transaction = Join-Path $parent ('.toolkit-transaction-' + [guid]::NewGuid().ToString('N'))
$staged = Join-Path $transaction 'next'
$backup = Join-Path $transaction 'previous'
$keepRecovery = $false
try {
    if ($LocalRepo) {
        $LocalRepo = [IO.Path]::GetFullPath($LocalRepo)
        $revision = & git -C $LocalRepo rev-parse HEAD
        if ($LASTEXITCODE -ne 0) { throw 'LocalRepo must be a git checkout' }
        $dirty = @(& git -C $LocalRepo status --porcelain)
        if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect LocalRepo state' }
        $ver = 'local+' + $revision + $(if ($dirty.Count) { '.dirty' } else { '.clean' })
        $src = @{
            native = @(Join-Path $LocalRepo 'native/wheelffb/build')
            'native-x86' = @(Join-Path $LocalRepo 'native/wheelffb/build/x86')
            include = @((Join-Path $LocalRepo 'native/wheelffb/include'),(Join-Path $LocalRepo 'native/forza'),(Join-Path $LocalRepo 'native/forcemodel'),(Join-Path $LocalRepo 'native/dinput-proxy'))
            profiles = @(Join-Path $LocalRepo 'profiles')
            powershell = @(Join-Path $LocalRepo 'tools/powershell')
            dotnet = @(Join-Path $LocalRepo 'dotnet/Dbce.Wheel.Ffb/bin/Release/netstandard2.0')
            tools = @(Join-Path $LocalRepo 'tools')
            knowledge = @(Join-Path $LocalRepo 'knowledge')
        }
    } else {
        New-Item -ItemType Directory -Path $download | Out-Null
        & gh release download $Version --repo $Repo --pattern 'dbce-wheel-mod-toolkit-*.zip' --dir $download
        if ($LASTEXITCODE -ne 0) { throw 'Release download failed' }
        $zips = @(Get-ChildItem -LiteralPath $download -Filter '*.zip')
        if ($zips.Count -ne 1) { throw 'Expected exactly one release archive' }
        Expand-Archive -LiteralPath $zips[0].FullName -DestinationPath (Join-Path $download 'expanded')
        $inner = @(Get-ChildItem -LiteralPath (Join-Path $download 'expanded') -Directory)
        if ($inner.Count -ne 1) { throw 'Expected one release root directory' }
        $src = @{}
        foreach ($part in $valid) {
            $rel = switch ($part) { 'native-x86' { 'native/x86' } 'include' { 'native/include' } 'powershell' { 'tools/powershell' } default { $part } }
            $src[$part] = @(Join-Path $inner[0].FullName $rel)
        }
        $ver = $Version
    }
    $recorded = @{}
    $manifestPath = Join-Path $Destination 'MANIFEST.txt'
    if (Test-Path -LiteralPath $manifestPath) {
        foreach ($line in Get-Content -LiteralPath $manifestPath) {
            if (-not $line.Trim() -or $line.TrimStart().StartsWith('#')) { continue }
            if ($line -notmatch '^([A-Fa-f0-9]{64})\s+(.+)$') { throw 'Malformed existing manifest' }
            $relative = $Matches[2].Replace('\','/')
            if ([IO.Path]::IsPathRooted($relative) -or $relative.Split('/') -contains '..') { throw 'Unsafe existing manifest path' }
            $recorded[$relative] = $Matches[1]
        }
    }
    $plan = @{}
    foreach ($part in $Parts) {
        $to = if ($part -eq 'native-x86') { 'native/x86' } else { $part }
        $partCount = 0
        foreach ($from in $src[$part]) {
            if (-not (Test-Path -LiteralPath $from -PathType Container)) { throw "Missing part $part at $from" }
            $files = switch ($part) {
                { $_ -in 'native','native-x86' } { @(Get-Item -LiteralPath (Join-Path $from 'WheelFfb.dll')) }
                'profiles' { @(Get-Item -LiteralPath (Join-Path $from 'force-profiles.ini')) }
                'include' { @(Get-ChildItem -LiteralPath $from -Filter '*.h' -File) }
                'powershell' { @(Get-ChildItem -LiteralPath $from -Filter '*.psm1' -File) }
                'dotnet' { @(Get-ChildItem -LiteralPath $from -File | Where-Object { $_.Name -like 'Dbce.Wheel.*.dll' -or $_.Name -like 'Dbce.Wheel.*.xml' }) }
                default { @(Get-ChildItem -LiteralPath $from -Recurse -File) }
            }
            if ($files.Count -eq 0) { throw "Empty requested part source: $from" }
            if ($part -eq 'dotnet' -and @($files | Where-Object Extension -eq '.dll').Count -eq 0) { throw 'No managed assemblies in dotnet part' }
            foreach ($file in $files) {
                Assert-Unlinked $file.FullName
                $relative = $to + '/' + $file.FullName.Substring($from.TrimEnd('\','/').Length + 1).Replace('\','/')
                if ($plan.ContainsKey($relative)) { throw "Duplicate destination: $relative" }
                $target = Join-Path $Destination $relative
                $hash = (Get-FileHash -LiteralPath $file.FullName).Hash
                $edited = $false
                if (Test-Path -LiteralPath $target) {
                    $oldHash = (Get-FileHash -LiteralPath $target).Hash
                    $edited = $oldHash -ne $hash -and (-not $recorded.ContainsKey($relative) -or $oldHash -ne $recorded[$relative])
                    if ($edited -and -not $Force) { throw "Locally edited or untracked vendor file: $relative. No changes made; use -Force to retain a backup and replace." }
                }
                $plan[$relative] = @{ source=$file.FullName; hash=$hash; edited=$edited }
                $partCount++
            }
        }
        if ($partCount -eq 0) { throw "Empty requested part: $part" }
    }
    # Same-volume staging permits directory renames. All source and conflict
    # checks have passed; the old destination remains untouched while copying.
    New-Item -ItemType Directory -Path $staged -Force | Out-Null
    if (Test-Path -LiteralPath $Destination) {
        foreach ($entry in Get-ChildItem -LiteralPath $Destination -Force) { Copy-Item -LiteralPath $entry.FullName -Destination $staged -Recurse -Force }
    }
    foreach ($relative in $plan.Keys) {
        $item = $plan[$relative]; $target = Join-Path $staged $relative
        New-Item -ItemType Directory -Path (Split-Path -Parent $target) -Force | Out-Null
        if ($item.edited) { Copy-Item -LiteralPath $target -Destination ($target + '.local-' + [guid]::NewGuid().ToString('N')) }
        Copy-Item -LiteralPath $item.source -Destination $target -Force
        if ((Get-FileHash -LiteralPath $target).Hash -ne $item.hash) { throw "Staged copy changed: $relative" }
        $recorded[$relative] = $item.hash
    }
    @("# Sync-Toolkit SHA-256; selected parts: $($Parts -join ','); toolkit $ver") +
        @($recorded.Keys | Sort-Object | ForEach-Object { "$($recorded[$_])  $_" }) |
        Set-Content -LiteralPath (Join-Path $staged 'MANIFEST.txt') -Encoding UTF8
    Set-Content -LiteralPath (Join-Path $staged 'VERSION') -Value @($ver, [DateTime]::UtcNow.ToString('O')) -Encoding UTF8
    $moved = $false
    try {
        if (Test-Path -LiteralPath $Destination) { Move-Item -LiteralPath $Destination -Destination $backup; $moved = $true }
        Move-Item -LiteralPath $staged -Destination $Destination
    } catch {
        if ($moved) {
            try { Move-Item -LiteralPath $backup -Destination $Destination }
            catch { $keepRecovery = $true; throw "Rollback failed; original tree retained at $backup. $($_.Exception.Message)" }
        }
        throw
    }
    Write-Host "Vendored $ver into $Destination ($($plan.Count) files); transaction committed."
} finally {
    # These absolute paths are unique generated children of the named staging
    # parents; never delete the caller's destination during cleanup or rollback.
    foreach ($scratch in @($download, $(if (-not $keepRecovery) { $transaction }))) {
        if ($scratch -and (Test-Path -LiteralPath $scratch)) {
            $absolute = [IO.Path]::GetFullPath($scratch)
            $expectedParent = if ($scratch -eq $download) { [IO.Path]::GetFullPath([IO.Path]::GetTempPath()).TrimEnd('\','/') } else { $parent }
            if ((Split-Path -Parent $absolute) -ne $expectedParent) { throw "Cleanup path escaped staging parent: $absolute" }
            Remove-Item -LiteralPath $absolute -Recurse -Force
        }
    }
}
