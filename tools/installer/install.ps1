<#
.SYNOPSIS
    Installs art of sim rally into art of rally.

.DESCRIPTION
    Run from the extracted release folder, normally by double-clicking
    Install.bat next to it.

    Finds the game through the Steam registry rather than guessing at
    "C:\Program Files (x86)\Steam". Steam libraries are routinely on another
    drive, and probing default paths is the single most common way an installer
    reports "game not found" on a perfectly normal machine.

.PARAMETER GameDir
    Skip detection and install here. Use for GOG, Epic, or an unusual layout.

.PARAMETER Uninstall
    Remove the mod instead of installing it.
#>
[CmdletBinding()]
param(
    [string]$GameDir,
    [switch]$Uninstall
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path

function Say($msg, $colour = 'Gray') { Write-Host $msg -ForegroundColor $colour }
function Ok($msg)   { Write-Host "  [ok] $msg"   -ForegroundColor Green }
function Warn($msg) { Write-Host "  [!]  $msg"   -ForegroundColor Yellow }
function Fail($msg) { Write-Host "  [X]  $msg"   -ForegroundColor Red }

Say ""
Say "art of sim rally - installer" Cyan
Say "============================" Cyan
Say ""

# --- find the game ---------------------------------------------------------

function Find-ArtOfRally {
    # Steam records its own location in the registry. Reading it is the only
    # reliable way to find a library that is not on C:.
    $steam = $null
    foreach ($key in 'HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam') {
        try {
            $v = Get-ItemProperty $key -ErrorAction Stop
            if ($v.SteamPath)   { $steam = $v.SteamPath }
            elseif ($v.InstallPath) { $steam = $v.InstallPath }
            if ($steam) { break }
        } catch { }
    }
    if (-not $steam) { return $null }
    $steam = $steam -replace '/', '\'

    # Every library folder, not just the default one.
    $libraries = @($steam)
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    if (Test-Path $vdf) {
        foreach ($line in Get-Content $vdf) {
            if ($line -match '"path"\s+"(.+?)"') {
                $libraries += ($Matches[1] -replace '\\\\', '\')
            }
        }
    }

    # Deduplicate case-insensitively: the registry value and libraryfolders.vdf
    # routinely disagree on casing for the same path, which would otherwise scan
    # the same library twice.
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($lib in $libraries) {
        if (-not $seen.Add($lib.TrimEnd([char]92))) { continue }   # 92 = backslash
        $candidate = Join-Path $lib 'steamapps\common\artofrally'
        if (Test-Path (Join-Path $candidate 'artofrally.exe')) { return $candidate }
    }
    return $null
}

if (-not $GameDir) {
    Say "Looking for art of rally..."
    $GameDir = Find-ArtOfRally
}

if (-not $GameDir -or -not (Test-Path (Join-Path $GameDir 'artofrally.exe'))) {
    Fail "Could not find art of rally."
    Say ""
    Say "  Run this again with the folder containing artofrally.exe, e.g.:"
    Say "    .\install.ps1 -GameDir ""D:\Games\artofrally""" DarkGray
    Say ""
    exit 1
}
Ok "Game found: $GameDir"

# --- check Unity Mod Manager ----------------------------------------------

$ummInstalled = (Test-Path (Join-Path $GameDir 'artofrally_Data\Managed\UnityModManager\UnityModManager.dll'))
if (-not $ummInstalled) {
    Fail "Unity Mod Manager is not installed for this game."
    Say ""
    Say "  Install it first, then run this again:"
    Say "    https://www.nexusmods.com/site/mods/21" DarkGray
    Say ""
    Say "  In its installer, pick 'Art of Rally' and click Install."
    Say ""
    exit 1
}
Ok "Unity Mod Manager present"

$modDir    = Join-Path $GameDir 'Mods\ArtOfSimRally'
$nativeDir = Join-Path $GameDir 'artofrally_Data\Plugins\x86_64'

# --- uninstall -------------------------------------------------------------

if ($Uninstall) {
    Say ""
    Say "Removing..."

    # Settings.xml lives inside the mod folder, so removing the folder would take
    # a user's force feedback and camera tuning with it. Keep it to one side and
    # put it back, so reinstalling later resumes where they left off.
    $settings = Join-Path $modDir 'Settings.xml'
    $keptSettings = $null
    if (Test-Path $settings) {
        $keptSettings = Join-Path $env:TEMP 'ArtOfSimRally.Settings.xml'
        Copy-Item $settings $keptSettings -Force
    }

    if (Test-Path $modDir) { Remove-Item $modDir -Recurse -Force; Ok "Removed $modDir" }
    else { Warn "Nothing at $modDir" }

    $native = Join-Path $nativeDir 'UnityForceFeedback.dll'
    if (Test-Path $native) { Remove-Item $native -Force; Ok "Removed $native" }

    if ($keptSettings) {
        New-Item -ItemType Directory -Force -Path $modDir | Out-Null
        Move-Item $keptSettings $settings -Force
        Ok "Kept your settings at $settings"
    }

    Say ""
    Say "Done. Your key bindings are untouched - they live in the game's own" Green
    Say "settings, not in the mod." Green
    Say ""
    exit 0
}

# --- install ---------------------------------------------------------------

$source = Join-Path $here 'Mods\ArtOfSimRally'
if (-not (Test-Path $source)) { $source = Join-Path $here 'ArtOfSimRally' }
if (-not (Test-Path $source)) {
    Fail "Cannot find the mod files next to this script."
    Say "  Extract the whole zip first, then run Install.bat from inside it."
    exit 1
}

try {
    New-Item -ItemType Directory -Force -Path $modDir | Out-Null
    Copy-Item (Join-Path $source '*') $modDir -Recurse -Force
    Ok "Mod installed to $modDir"

    # The native plugin goes in both places on purpose. The mod loads it by
    # absolute path from its own folder, but the game's own (unused) force
    # feedback code looks in Plugins\x86_64, and a copy there also covers any
    # runtime that will not resolve it from the mod folder. It is 160 KB.
    $native = Join-Path $source 'UnityForceFeedback.dll'
    if (Test-Path $native) {
        New-Item -ItemType Directory -Force -Path $nativeDir | Out-Null
        Copy-Item $native $nativeDir -Force
        Ok "Force feedback plugin installed"
    } else {
        Warn "UnityForceFeedback.dll not found in the package - force feedback will not work"
    }
}
catch [System.UnauthorizedAccessException] {
    Fail "Access denied writing to the game folder."
    Say ""
    Say "  Close the game, then right-click Install.bat and choose"
    Say "  'Run as administrator'." DarkGray
    Say ""
    exit 1
}

# --- verify ----------------------------------------------------------------

Say ""
Say "Verifying..."
$expected = 'ArtOfSimRally.Mod.dll', 'ArtOfSimRally.Telemetry.dll', 'Info.json'
$missing = $expected | Where-Object { -not (Test-Path (Join-Path $modDir $_)) }
if ($missing) {
    Fail "Missing after install: $($missing -join ', ')"
    exit 1
}
Ok "All files in place"

Say ""
Say "Done." Green
Say ""
Say "  1. Launch art of rally"
Say "  2. Press Ctrl+F10 for the mod settings"
Say "  3. Force feedback strength is 'Reference torque' - LOWER IS STRONGER."
Say "     The default is a starting guess; turn on 'Log peak torque', drive a"
Say "     minute, and the log tells you what to set."
Say ""
Say "  Trouble? In the settings panel press 'Create support file on Desktop'"
Say "  and attach that file to a bug report."
Say ""
