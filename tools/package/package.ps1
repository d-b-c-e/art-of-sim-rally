<#
.SYNOPSIS
    Builds a distributable art-of-sim-rally release zip for Unity Mod Manager.

.DESCRIPTION
    Produces dist/ArtOfSimRally-<version>.zip.

    There are two install locations and only one of them is automatic. The mod
    itself goes under Mods/ for Unity Mod Manager, which can install it straight
    from this zip. UnityForceFeedback.dll is a native plugin and must land in
    artofrally_Data/Plugins/x86_64 beside the game's own; UMM will not place it,
    and anywhere else means force feedback silently does nothing with no error.

    The zip therefore mirrors the game's folder layout, so the manual half of the
    copy is unambiguous.

.EXAMPLE
    .\tools\package\package.ps1 -Version 0.1.0
#>
[CmdletBinding()]
param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = 'Stop'
$root  = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dist  = Join-Path $root 'dist'
$stage = Join-Path $dist "stage-$Version"

Write-Host "Building managed mod..." -ForegroundColor Cyan
& dotnet build (Join-Path $root 'src\ArtOfSimRally.Mod\ArtOfSimRally.Mod.csproj') -c Release -v q --nologo
if ($LASTEXITCODE -ne 0) { throw "Managed build failed" }

Write-Host "Building native plugin..." -ForegroundColor Cyan
& cmd.exe /c (Join-Path $root 'src\UnityForceFeedback\build.bat')
if ($LASTEXITCODE -ne 0) { throw "Native build failed" }

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$modDir    = Join-Path $stage 'Mods\ArtOfSimRally'
$nativeDir = Join-Path $stage 'artofrally_Data\Plugins\x86_64'
New-Item -ItemType Directory -Force -Path $modDir, $nativeDir | Out-Null

$bin = Join-Path $root 'src\ArtOfSimRally.Mod\bin\Release'
Copy-Item (Join-Path $bin 'ArtOfSimRally.Mod.dll')       $modDir
Copy-Item (Join-Path $bin 'ArtOfSimRally.Telemetry.dll') $modDir
Copy-Item (Join-Path $root 'src\ArtOfSimRally.Mod\Info.json') $modDir
Copy-Item (Join-Path $root 'src\UnityForceFeedback\build\UnityForceFeedback.dll') $nativeDir
Copy-Item (Join-Path $root 'LICENSE') $stage

$readme = @'
art of sim rally
================

Turns art of rally into something you can drive on a wheel: real force feedback,
a bonnet camera, Forza-compatible telemetry, and two fixes for steering that the
game only applies to wheels it recognises.

INSTALL
-------
1. Install Unity Mod Manager and point it at art of rally. The game is already in
   UMM's supported list.  https://www.nexusmods.com/site/mods/21

2. Install the mod, either way:
     - drag this zip onto Unity Mod Manager's Mods tab, or
     - copy the "Mods" folder from this zip into your art of rally folder.

3. IMPORTANT, and not automatic - copy this by hand into your game folder:

       artofrally_Data\Plugins\x86_64\UnityForceFeedback.dll

   It is a native plugin and has to sit beside the game's own. Unity Mod Manager
   will not place it for you, and anywhere else means force feedback silently
   does nothing, with no error anywhere.

4. Launch the game. Press Ctrl+F10 for the settings panel.

FIRST THINGS TO CHECK
---------------------
* Steering should feel direct straight away.

* Force feedback strength is "Reference torque", and LOWER IS STRONGER. The
  default of 150 is a starting guess that will need tuning for your wheel. Turn
  on "Log peak torque", drive for a minute, and the log reports the number to
  put there.

* The bonnet camera is added to the game's normal view rotation - press your
  change-view button to cycle onto it. Adjust it live on the numpad while you
  are looking through it: 8/2 up-down, 7/9 back-forward, 4/6 left-right,
  1/3 tilt, +/- field of view, 0 resets. Changes save on their own.

* Telemetry is off by default. Switch it on and point SimHub at a Forza
  Horizon 5 profile on UDP port 8000.

KNOWN LIMITS
------------
* Developed and tested on a MOZA R12 Base only. The steering and deadzone fixes
  should apply to any wheel Rewired does not recognise, which is likely most
  modern direct-drive bases, but that is reasoning rather than testing.

* This is a bonnet camera, not a cockpit camera. art of rally's cars have no
  modelled interiors, so there is nothing to sit inside of.

* "Disable steering assist" is OFF by default and genuinely changes how the car
  behaves. The other steering options only restore what a recognised wheel
  already gets. art of rally has online leaderboards - enable it deliberately.

Source, the full technical write-up, and issues:
https://github.com/d-b-c-e/art-of-sim-rally
'@

$readme | Set-Content (Join-Path $stage 'README.txt') -Encoding UTF8

$zip = Join-Path $dist "ArtOfSimRally-$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip

Write-Host "Packaged $zip" -ForegroundColor Green
Get-ChildItem $zip | Select-Object Name, Length
