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
    if (Test-Path -LiteralPath $vdf) {
        foreach ($line in Get-Content -LiteralPath $vdf) {
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
        if (Test-Path -LiteralPath (Join-Path $candidate 'artofrally.exe') -PathType Leaf) { return $candidate }
    }
    return $null
}

if (-not $GameDir) {
    Say "Looking for art of rally..."
    $GameDir = Find-ArtOfRally
}

if (-not $GameDir -or -not (Test-Path -LiteralPath (Join-Path $GameDir 'artofrally.exe') -PathType Leaf)) {
    Fail "Could not find art of rally."
    Say ""
    Say "  In Steam: Library > art of rally > Manage > Browse local files."
    Say "  Open PowerShell in this extracted download and use that folder:"
    $modeArgument = if ($Uninstall) { ' -Uninstall' } else { '' }
    Say "    powershell -NoProfile -ExecutionPolicy Bypass -File .\install.ps1 -GameDir ""D:\Games\artofrally""$modeArgument" DarkGray
    Say ""
    exit 1
}
Ok "Game found: $GameDir"
$GameDir = [IO.Path]::GetFullPath($GameDir).TrimEnd('\', '/')

# --- check Unity Mod Manager ----------------------------------------------

# Installing over a running game leaves half-copied DLLs and a confusing error.
if (Get-Process artofrally -ErrorAction SilentlyContinue) {
    Fail "art of rally is running."
    Say ""
    Say "  Close the game and run this again."
    Say ""
    exit 1
}

# Checked per-game, not globally: Unity Mod Manager is installed into each game
# separately, so having it for another title does not help here.
$ummInstalled = (Test-Path -LiteralPath (Join-Path $GameDir 'artofrally_Data\Managed\UnityModManager\UnityModManager.dll') -PathType Leaf)
if (-not $ummInstalled -and -not $Uninstall) {
    Fail "Unity Mod Manager is not installed for art of rally."
    Say ""
    Say "  This mod runs on top of Unity Mod Manager, so that has to go on first." Yellow
    Say "  It is a one-time setup and takes about a minute:"
    Say ""
    Say "    1. Download Unity Mod Manager:"
    Say "         https://www.nexusmods.com/site/mods/21" DarkGray
    Say "    2. Run UnityModManager.exe"
    Say "    3. In the Game dropdown pick 'Art of Rally'"
    Say "       (it should find your install automatically)"
    Say "    4. Click Install, then close it"
    Say "    5. Double-click Install.bat again - this installer"
    Say ""
    exit 1
}
if ($ummInstalled) { Ok "Unity Mod Manager present" }

$modDir    = Join-Path $GameDir 'Mods\ArtOfSimRally'
$nativeDir = Join-Path $GameDir 'artofrally_Data\Plugins\x86_64'
$modFiles = 'ArtOfSimRally.Mod.dll', 'Dbce.Wheel.Telemetry.dll', 'Dbce.Wheel.Ffb.dll', 'UnityForceFeedback.dll', 'Info.json', 'build.json'

# Do not follow a junction/symlink while replacing or removing installed files.
foreach ($target in @($modDir, $nativeDir)) {
    $path = [IO.Path]::GetFullPath($target)
    if (-not $path.StartsWith($GameDir + '\', [StringComparison]::OrdinalIgnoreCase)) { throw 'Target is outside the game directory' }
    while ($path.Length -ge $GameDir.Length) {
        if ((Test-Path -LiteralPath $path) -and ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing linked install target: $path"
        }
        $path = Split-Path -Parent $path
    }
}
foreach ($targetFile in (@($modFiles | ForEach-Object { Join-Path $modDir $_ }) + (Join-Path $nativeDir 'UnityForceFeedback.dll'))) {
    if ((Test-Path -LiteralPath $targetFile) -and ((Get-Item -LiteralPath $targetFile).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
        throw "Refusing linked install file: $targetFile"
    }
}

# --- uninstall -------------------------------------------------------------

if ($Uninstall) {
    Say ""
    Say "Removing..."

    # Remove only our payload. Settings and unknown user files stay in place;
    # no recursive deletion or shared temporary Settings.xml backup is needed.
    foreach ($name in $modFiles) {
        $file = Join-Path $modDir $name
        if (Test-Path -LiteralPath $file) { Remove-Item -LiteralPath $file -Force }
    }

    $native = Join-Path $nativeDir 'UnityForceFeedback.dll'
    if (Test-Path -LiteralPath $native) { Remove-Item -LiteralPath $native -Force; Ok "Removed $native" }
    Ok 'Kept settings and user files in place'

    Say ""
    Say "Done. Your key bindings are untouched - they live in the game's own" Green
    Say "settings, not in the mod." Green
    Say ""
    exit 0
}

# --- install ---------------------------------------------------------------

$source = Join-Path $here 'ArtOfSimRally'
if (-not (Test-Path -LiteralPath $source)) {
    Fail "Cannot find the mod files next to this script."
    Say "  Extract the whole zip first, then run Install.bat from inside it."
    exit 1
}

# Validate the entire extracted package before touching the game directory.
try {
    . (Join-Path $here 'verify.ps1')
    $manifest = Assert-Payload $here
} catch {
    Fail "The extracted download is incomplete or has changed."
    Say "  $($_.Exception.Message)" DarkGray
    Say "  Download the mod ZIP again (not Source code), use Extract All into a"
    Say "  fresh folder, then run Install.bat. No game files were changed."
    exit 1
}
Ok "Package verified: $($manifest.release)"

try {
    New-Item -ItemType Directory -Force -Path $modDir | Out-Null
    foreach ($name in $modFiles) { Copy-Item -LiteralPath (Join-Path $source $name) -Destination $modDir -Force }
    Ok "Mod installed to $modDir"

    # The native plugin goes in both places on purpose. The mod loads it by
    # absolute path from its own folder, but the game's own (unused) force
    # feedback code looks in Plugins\x86_64, and a copy there also covers any
    # runtime that will not resolve it from the mod folder.
    $native = Join-Path $source 'UnityForceFeedback.dll'
    if (Test-Path -LiteralPath $native) {
        New-Item -ItemType Directory -Force -Path $nativeDir | Out-Null
        Copy-Item -LiteralPath $native -Destination $nativeDir -Force
        Ok "Force feedback plugin installed"
    } else { throw 'Native DLL missing from verified package' }
}
catch [System.UnauthorizedAccessException] {
    Fail "Access denied writing to the game folder."
    Say ""
    Say "  Close the game, then right-click Install.bat and choose"
    Say "  'Run as administrator'." DarkGray
    Say ""
    exit 1
}
catch [System.IO.IOException] {
    Fail "Could not copy the mod files."
    Say "  $($_.Exception.Message)" DarkGray
    Say "  Close the game and anything holding its files, check free disk space,"
    Say "  then run Install.bat again before launching. Some files may already"
    Say "  have been replaced; a successful retry verifies the complete install."
    exit 1
}

# --- verify ----------------------------------------------------------------

Say ""
Say "Verifying..."
$expected = $modFiles
$missing = $expected | Where-Object { -not (Test-Path -LiteralPath (Join-Path $modDir $_)) }
if ($missing) {
    Fail "Missing after install: $($missing -join ', ')"
    exit 1
}
foreach ($name in $modFiles) {
    if ((Get-FileHash -LiteralPath (Join-Path $modDir $name) -Algorithm SHA256).Hash -ne $manifest.files."ArtOfSimRally/$name") {
        throw "Installed file does not match package: $name"
    }
}
if ((Get-FileHash -LiteralPath (Join-Path $nativeDir 'UnityForceFeedback.dll') -Algorithm SHA256).Hash -ne $manifest.files.'ArtOfSimRally/UnityForceFeedback.dll') {
    throw 'Installed plugin copy does not match package'
}
Ok "All installed files match release $($manifest.release)"

Say ""
Say "Done." Green
Say ""
Say "  1. Launch art of rally through Steam"
Say "  2. Press Ctrl+F10 for the mod settings"
Say "  3. While paused, select your wheel under Force feedback > Wheel."
Say "     Keep the game focused while setup completes. Strength defaults to 50;"
Say "     lower it if too heavy. Test while moving: force fades in at 3-12 km/h."
Say "  4. For a separate USB handbrake or unread controls, use Wheel input (direct)."
Say "     Read README.txt for setup, updates and troubleshooting."
Say ""
Say "  Your existing settings are preserved. New settings enable landing"
Say "  vibration at strength 5; saved choices are kept."
Say ""
Say "  Trouble? In the settings panel press 'Create support file on Desktop'"
Say "  and attach that file to a bug report."
Say ""
