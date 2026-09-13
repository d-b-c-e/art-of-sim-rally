<#
.SYNOPSIS
    Builds a distributable art-of-sim-rally release zip for Unity Mod Manager.

.DESCRIPTION
    Produces dist/ArtOfSimRally-<version>.zip.

    Layout is deliberately dual-purpose: ArtOfSimRally/ sits at the zip root so
    dragging the zip onto Unity Mod Manager installs it correctly, while
    Install.bat next to it gives a double-click install for everyone else. An
    earlier layout nested the mod under Mods/, which made UMM produce
    Mods/Mods/ArtOfSimRally and silently fail to load - reported by a user.

    The native plugin ships inside the mod folder; the installer copies it on to
    artofrally_Data/Plugins/x86_64 as well.
.EXAMPLE
    .\tools\package\package.ps1 -Version 0.1.0
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$Version
)

$ErrorActionPreference = 'Stop'
$root  = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
if ($Version -notmatch '^\d+\.\d+\.\d+(-rc\.[1-9]\d*)?$') { throw 'Use X.Y.Z or X.Y.Z-rc.N' }
$props = [xml](Get-Content -LiteralPath (Join-Path $root 'Version.props') -Raw)
$modVersion = [string]$props.Project.PropertyGroup.ModVersion
if (($Version -split '-')[0] -ne $modVersion) { throw "Version must match Version.props ($modVersion)" }
$info = Get-Content -LiteralPath (Join-Path $root 'src/ArtOfSimRally.Mod/Info.json') -Raw | ConvertFrom-Json
if ($info.Version -ne $modVersion) { throw 'Info.json and Version.props disagree' }
$dist  = Join-Path $root 'dist'
$stage = Join-Path $dist "stage-$Version"
$zip = Join-Path $dist "ArtOfSimRally-$Version.zip"
if ((Test-Path -LiteralPath $stage) -or (Test-Path -LiteralPath $zip)) {
    throw 'Candidate already exists. Keep its evidence; choose a new RC number.'
}
$revision = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source revision' }
$sourceState = if (& git -C $root status --porcelain --untracked-files=all) { 'dirty' } else { 'clean' }
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source state' }
if ($Version -notmatch '-rc\.' -and $sourceState -ne 'clean') { throw 'Final release requires a clean source tree' }
$identity = "$Version+$revision.$sourceState"
. (Join-Path $root 'tools/installer/verify.ps1')

# Validate every pinned artifact before building; do not silently package a
# locally edited vendor binary or omit a missing DLL.
$toolkit = Join-Path $root 'lib/toolkit'
$toolkitPin = (Get-Content -LiteralPath (Join-Path $toolkit 'VERSION') -First 1).Trim()
if ($Version -notmatch '-rc\.' -and $toolkitPin -notmatch '^v\d+\.\d+\.\d+$') {
    throw 'Final release requires an official toolkit release pin; local toolkit builds are for development candidates only'
}
$entries = 0
foreach ($line in Get-Content -LiteralPath (Join-Path $toolkit 'MANIFEST.txt')) {
    if ($line.StartsWith('#') -or [string]::IsNullOrWhiteSpace($line)) { continue }
    if ($line -notmatch '^([A-Fa-f0-9]{64})\s+(.+)$') { throw 'Malformed toolkit manifest' }
    $hash = $Matches[1]; $relative = $Matches[2]
    $vendorPath = [IO.Path]::GetFullPath((Join-Path $toolkit $relative))
    if (-not $vendorPath.StartsWith([IO.Path]::GetFullPath($toolkit) + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Invalid toolkit path' }
    if ((Get-FileHash -LiteralPath $vendorPath -Algorithm SHA256).Hash -ne $hash) { throw "Toolkit hash mismatch: $relative" }
    $entries++
}
if ($entries -lt 3) { throw 'Incomplete toolkit manifest' }

Write-Host "Building managed mod..." -ForegroundColor Cyan
& dotnet build (Join-Path $root 'src\ArtOfSimRally.Mod\ArtOfSimRally.Mod.csproj') -c Release -v q --nologo -warnaserror "-p:ReleaseLabel=$Version" "-p:SourceRevisionId=$revision" "-p:BuildSourceState=$sourceState"
if ($LASTEXITCODE -ne 0) { throw "Managed build failed" }
# Inspect the compiled metadata without running game code. A stray recorder
# source file in the production project must fail even if its DLL isn't copied.
& dotnet run --project (Join-Path $root 'tools/testing/Replay/Replay.csproj') -c Release -- --verify-release (Join-Path $root 'src/ArtOfSimRally.Mod/bin/Release/ArtOfSimRally.Mod.dll')
if ($LASTEXITCODE -ne 0) { throw 'Release assembly contains developer recorder code or dependencies.' }

Write-Host "Using verified vendored native plugin..." -ForegroundColor Cyan
# Native FFB layer and telemetry encoder are vendored from dbce-wheel-mod-toolkit (lib\toolkit).
$toolkit = Join-Path $root 'lib\toolkit'
if (-not (Test-Path (Join-Path $toolkit 'native\WheelFfb.dll'))) { throw 'lib\toolkit is missing - run tools\Sync-Toolkit.ps1' }
$modDir    = Join-Path $stage 'ArtOfSimRally'
New-Item -ItemType Directory -Force -Path $modDir | Out-Null

$bin = Join-Path $root 'src\ArtOfSimRally.Mod\bin\Release'
Copy-Item (Join-Path $bin 'ArtOfSimRally.Mod.dll')       $modDir
Copy-Item (Join-Path $bin 'Dbce.Wheel.Telemetry.dll') $modDir
Copy-Item (Join-Path $bin 'Dbce.Wheel.Ffb.dll') $modDir
Copy-Item (Join-Path $root 'src\ArtOfSimRally.Mod\Info.json') $modDir
Copy-Item (Join-Path $toolkit 'native\WheelFfb.dll') (Join-Path $modDir 'UnityForceFeedback.dll')   # the file name the mod P/Invokes
Copy-Item (Join-Path $root 'LICENSE') $stage
Copy-Item (Join-Path $root 'tools\installer\Install.bat')   $stage
Copy-Item (Join-Path $root 'tools\installer\Uninstall.bat') $stage
Copy-Item (Join-Path $root 'tools\installer\install.ps1')   $stage
Copy-Item (Join-Path $root 'tools\installer\verify.ps1')   $stage
$build = [ordered]@{ schema=1; release=$Version; modVersion=$modVersion; identity=$identity; sourceRevision=$revision; sourceState=$sourceState; toolkitPin=$toolkitPin; builtUtc=[DateTime]::UtcNow.ToString('o') }
$build | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $modDir 'build.json') -Encoding UTF8
$fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $modDir 'ArtOfSimRally.Mod.dll'))
if ($fileVersion.ProductVersion -ne $identity -or $fileVersion.FileVersion -ne "$modVersion.0") { throw 'Built assembly identity does not match candidate' }

# Keep the player-facing readme editable beside the installer; ship a standalone
# guide with web links, not paths to source-only documents absent from the ZIP.
$readme = (Get-Content -LiteralPath (Join-Path $root 'tools/installer/README.txt') -Raw).Replace('@RELEASE@', $Version)
$readme | Set-Content (Join-Path $stage 'README.txt') -Encoding UTF8

$files = [ordered]@{}
foreach ($relative in $PayloadFiles) { $files[$relative] = (Get-FileHash -LiteralPath (Join-Path $stage $relative) -Algorithm SHA256).Hash }
[ordered]@{ schema=1; release=$Version; files=$files } | ConvertTo-Json -Depth 4 |
    Set-Content -LiteralPath (Join-Path $stage 'manifest.json') -Encoding UTF8
$null = Assert-Payload $stage
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash | Set-Content -LiteralPath "$zip.sha256" -Encoding ASCII

Write-Host "Packaged $zip" -ForegroundColor Green
Get-ChildItem $zip | Select-Object Name, Length
