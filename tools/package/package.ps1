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
    [string]$Version = "0.2.0"
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
$modDir    = Join-Path $stage 'ArtOfSimRally'
New-Item -ItemType Directory -Force -Path $modDir | Out-Null

$bin = Join-Path $root 'src\ArtOfSimRally.Mod\bin\Release'
Copy-Item (Join-Path $bin 'ArtOfSimRally.Mod.dll')       $modDir
Copy-Item (Join-Path $bin 'ArtOfSimRally.Telemetry.dll') $modDir
Copy-Item (Join-Path $root 'src\ArtOfSimRally.Mod\Info.json') $modDir
Copy-Item (Join-Path $root 'src\UnityForceFeedback\build\UnityForceFeedback.dll') $modDir
Copy-Item (Join-Path $root 'LICENSE') $stage
Copy-Item (Join-Path $root 'tools\installer\Install.bat')   $stage
Copy-Item (Join-Path $root 'tools\installer\Uninstall.bat') $stage
Copy-Item (Join-Path $root 'tools\installer\install.ps1')   $stage

$readme = @'
art of sim rally
================

Turns art of rally into something you can drive on a wheel: real force feedback,
a bonnet camera, Forza-compatible telemetry, and two fixes for steering that the
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

* Force feedback strength is a 0-100 slider. Start around 70 and adjust to
  taste. If the wheel pulls the wrong way, tick "Invert direction".

* Pick your wheel from the Wheel dropdown under Force feedback. If two devices
  share a name, choose one and turn the wheel - if nothing happens, choose the
  other. Switching takes effect immediately.

* Got a separate shifter? Open the Shifter section, tick "Use a separate
  shifter", choose the device, and bind each gear by clicking "set" and moving
  the lever. Both H-pattern and sequential work. The game's own input system
  cannot see most shifters - this reads yours directly, so it works anyway.

* Bonnet and bumper cameras are added to the game's normal view rotation - press
  your change-view button to cycle onto them. Adjust it live on the numpad while you
  are looking through it: 8/2 up-down, 7/9 back-forward, 4/6 left-right,
  1/3 tilt, +/- field of view, 0 resets. Changes save on their own.

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

* The camera can swing about for a moment when the game takes control at the
  end of a stage. Cosmetic, and only during the results cinematic.

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
