<#
.SYNOPSIS
    Builds a distributable art-of-sim-rally release zip.

.DESCRIPTION
    Produces dist/ArtOfSimRally-<version>.zip containing the managed plugin, the
    telemetry encoder and the native force feedback plugin.

    Note the two install locations: the managed DLLs go under BepInEx/plugins,
    but UnityForceFeedback.dll must land in artofrally_Data/Plugins/x86_64 beside
    the game's own native plugins. Putting it in the wrong place fails silently
    with no force feedback and no error message, so the zip mirrors the game's
    folder layout to make the copy unambiguous.

.EXAMPLE
    .\tools\package\package.ps1 -Version 0.1.0
#>
[CmdletBinding()]
param(
    [string]$Version = "0.1.0"
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$dist = Join-Path $root 'dist'
$stage = Join-Path $dist "stage-$Version"

Write-Host "Building..." -ForegroundColor Cyan
& dotnet build (Join-Path $root 'src\ArtOfSimRally.Mod\ArtOfSimRally.Mod.csproj') -c Release -v q --nologo
if ($LASTEXITCODE -ne 0) { throw "Managed build failed" }

& cmd.exe /c (Join-Path $root 'src\UnityForceFeedback\build.bat')
if ($LASTEXITCODE -ne 0) { throw "Native build failed" }

if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
$pluginDir = Join-Path $stage 'BepInEx\plugins\ArtOfSimRally'
$nativeDir = Join-Path $stage 'artofrally_Data\Plugins\x86_64'
New-Item -ItemType Directory -Force -Path $pluginDir, $nativeDir | Out-Null

Copy-Item (Join-Path $root 'src\ArtOfSimRally.Mod\bin\Release\ArtOfSimRally.Mod.dll')       $pluginDir
Copy-Item (Join-Path $root 'src\ArtOfSimRally.Mod\bin\Release\ArtOfSimRally.Telemetry.dll') $pluginDir
Copy-Item (Join-Path $root 'src\UnityForceFeedback\build\UnityForceFeedback.dll')           $nativeDir
Copy-Item (Join-Path $root 'LICENSE') $stage

@"
art of sim rally $Version
=========================

Turns art of rally into something you can drive on a wheel: real force feedback,
a bonnet camera, Forza-compatible telemetry, and two fixes for steering that the
game applies only to wheels it recognises.

INSTALL
-------
1. Install BepInEx 5 (x64) into your art of rally folder, run the game once, quit.
2. Copy BOTH folders from this zip into your art of rally folder, merging them:

     BepInEx\plugins\ArtOfSimRally\   -> the mod
     artofrally_Data\Plugins\x86_64\  -> UnityForceFeedback.dll

   The second one matters. UnityForceFeedback.dll must sit beside the game's own
   native plugins. Anywhere else and force feedback silently does nothing.

3. Run the game once more to generate the config, then tune:
     BepInEx\config\com.dbce.artofsimrally.cfg

FIRST THINGS TO CHECK
---------------------
* Steering should feel direct immediately. If it still feels vague, set
  DiagnosticLogging = true and check BepInEx\LogOutput.log.
* Force feedback strength is set by MzReference - LOWER is STRONGER. The default
  of 150 is a starting guess and will need tuning for your wheel.
* The bonnet camera is added to the normal view rotation - press your change-view
  button to cycle to it. Adjust it live with the numpad while looking through it;
  changes save automatically.
* Telemetry is off by default. Enable it and point SimHub at a Forza Horizon 5
  profile on UDP 8000.

KNOWN LIMITS
------------
* Developed and tested on a MOZA R12 Base only.
* This is a bonnet camera, not a cockpit camera. art of rally's cars have no
  modelled interiors.
* DisableSteerAssist is OFF by default and genuinely changes how the car behaves.
  The other steering fixes only restore what a recognised wheel already gets.
  art of rally has online leaderboards - enable it deliberately.

Source, full technical write-up and issues:
https://github.com/d-b-c-e/art-of-sim-rally
"@ | Set-Content (Join-Path $stage 'README.txt') -Encoding UTF8

$zip = Join-Path $dist "ArtOfSimRally-$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip

Write-Host "Packaged $zip" -ForegroundColor Green
Get-ChildItem $zip | Select-Object Name, Length
