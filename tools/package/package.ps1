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
$build = [ordered]@{ schema=1; release=$Version; modVersion=$modVersion; identity=$identity; sourceRevision=$revision; sourceState=$sourceState; toolkitPin=(Get-Content -LiteralPath (Join-Path $toolkit 'VERSION') -First 1); builtUtc=[DateTime]::UtcNow.ToString('o') }
$build | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $modDir 'build.json') -Encoding UTF8
$fileVersion = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $modDir 'ArtOfSimRally.Mod.dll'))
if ($fileVersion.ProductVersion -ne $identity -or $fileVersion.FileVersion -ne "$modVersion.0") { throw 'Built assembly identity does not match candidate' }

$readme = @'
art of sim rally
================

Turns art of rally into something you can drive on a wheel: real force feedback,
bonnet and bumper cameras, Forza-compatible telemetry, and two fixes for steering that the
game only applies to wheels it recognises.

INSTALL
-------
1. Install Unity Mod Manager and point it at art of rally. The game is already
   in its supported list.   https://www.nexusmods.com/site/mods/21

2. Double-click Install.bat.

That is it. It finds your game (including on a non-default Steam library),
checks Unity Mod Manager is present, and puts everything where it belongs.

If you would rather do it by hand, or you use Vortex: drag this zip onto Unity
Mod Manager's Mods tab, which installs the ArtOfSimRally folder correctly.

To remove it later, double-click Uninstall.bat. Your settings are kept.

FIRST THINGS TO CHECK
---------------------
Launch the game and press Ctrl+F10 for the settings panel.

* Steering should feel direct straight away.

* Force feedback strength is a 0-100 slider; 50 is the tuned default. If the
  wheel pulls toward lock instead of back to centre, tick "Invert direction".
  Force fades in between 3 and 12 km/h - that is deliberate.

* Fanatec, or any wheel the game's controls screen ignores: open Wheel input
  (direct) in the mod panel, tick "Read the wheel directly", then Assign
  steering and each pedal by moving it. Flip a row if it runs backwards.
  Menus still use the keyboard or a pad. See docs/TROUBLESHOOTING.md.
* Pick your wheel from the Wheel dropdown under Force feedback. If two devices
  share a name, choose one and turn the wheel - if nothing happens, choose the
  other. Switching takes effect immediately.

* Got a separate shifter? Open the Shifter section, tick "Use a separate
  shifter", choose the device, and bind each gear by clicking "set" and moving
  the lever. Both H-pattern and sequential work. The game's own input system
  cannot see most shifters - this reads yours directly, so it works anyway.

* Bonnet and bumper cameras are added to the game's normal view rotation - press
  your change-view button to cycle onto them. Adjust the active mount on the numpad while you
  are looking through it: 8/2 up-down, 7/9 back-forward, 4/6 left-right,
  1/3 tilt, +/- field of view, 0 resets. In the mod's Camera panel you can
  rebind or clear each key and restore the numpad defaults. Choose single keys
  that do not overlap your game controls. Tuning is suspended while the panel
  is open. Changes save automatically when paused or otherwise idle, including
  after you switch back to a stock view.

* Telemetry is off by default. Switch it on and point SimHub at a Forza
  Horizon 5 profile on UDP port 8000. Host and port can be changed while the
  game runs - handy if something else already owns the port.

SOMETHING NOT WORKING?
----------------------
In the settings panel press "Create support file on Desktop". That collects
your settings, your controllers, what is actually bound, and the logs into one
file. Attach it to a bug report - it usually contains the answer.

KNOWN LIMITS
------------
* Developed and tested on a MOZA R12 Base only. The steering and deadzone fixes
  should apply to any wheel Rewired does not recognise, which is likely most
  modern direct-drive bases, but that is reasoning rather than testing.

* Owner testing of 0.2.3-rc.6 reported working cameras, no stutter and no control
  issues so far. That result applies to the earlier artifact. The new 0.2.4
  camera-key and direct-input changes have automated coverage; attended camera,
  control and hardware checks remain pending. See the release readiness notes
  in the source repository for results tied to each exact artifact.

* This is a bonnet camera, not a cockpit camera. art of rally's cars have no
  modelled interiors, so there is nothing to sit inside of.

* "Disable steering assist" is OFF by default and genuinely changes how the car
  behaves. The other steering options only restore what a recognised wheel
  already gets. art of rally has online leaderboards - enable it deliberately.

Source, the full technical write-up, and issues:
https://github.com/d-b-c-e/art-of-sim-rally
'@

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
