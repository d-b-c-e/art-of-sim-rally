<# Install or remove only this mod's five packaged files. Saved measurements,
   Settings.xml, other mods, and Unity Mod Manager are never removed. #>
[CmdletBinding()]
param([string]$GameDir, [switch]$Uninstall)

$ErrorActionPreference = 'Stop'
$packageDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$modName = 'DbceTripleScreenArtOfRally'
$modFiles = @('ArtOfRally.TripleScreen.Mod.dll', 'Dbce.TripleScreen.Core.dll',
    'Dbce.TripleScreen.Protocol.dll', 'Info.json', 'manifest.json')

function Find-ArtOfRally {
    $steam = $null
    foreach ($key in @('HKCU:\Software\Valve\Steam', 'HKLM:\SOFTWARE\WOW6432Node\Valve\Steam')) {
        try {
            $value = Get-ItemProperty -LiteralPath $key -ErrorAction Stop
            if ($value.SteamPath) { $steam = $value.SteamPath }
            elseif ($value.InstallPath) { $steam = $value.InstallPath }
            if ($steam) { break }
        } catch { }
    }
    if (-not $steam) { return $null }
    $steam = $steam -replace '/', '\'
    $libraries = @($steam)
    $vdf = Join-Path $steam 'steamapps\libraryfolders.vdf'
    if (Test-Path -LiteralPath $vdf -PathType Leaf) {
        foreach ($line in Get-Content -LiteralPath $vdf) {
            if ($line -match '"path"\s+"(.+?)"') { $libraries += ($Matches[1] -replace '\\\\', '\') }
        }
    }
    $seen = New-Object 'System.Collections.Generic.HashSet[string]' ([StringComparer]::OrdinalIgnoreCase)
    foreach ($library in $libraries) {
        if (-not $seen.Add($library.TrimEnd([char]92))) { continue }
        $candidate = Join-Path $library 'steamapps\common\artofrally'
        if (Test-Path -LiteralPath (Join-Path $candidate 'artofrally.exe') -PathType Leaf) { return $candidate }
    }
    return $null
}

function Assert-OrdinaryTarget([string]$GameRoot, [string]$Target) {
    $path = [IO.Path]::GetFullPath($Target)
    if (-not $path.StartsWith($GameRoot + '\', [StringComparison]::OrdinalIgnoreCase)) {
        throw "Install target is outside the game folder: $path"
    }
    while ($path.Length -ge $GameRoot.Length) {
        if ((Test-Path -LiteralPath $path) -and
            ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint)) {
            throw "Refusing a linked install path: $path"
        }
        $path = Split-Path -Parent $path
    }
}

Write-Host ''
Write-Host 'art of rally triple-screen' -ForegroundColor Cyan
Write-Host '==========================' -ForegroundColor Cyan

if (-not $GameDir) { $GameDir = Find-ArtOfRally }
if (-not $GameDir -or -not (Test-Path -LiteralPath (Join-Path $GameDir 'artofrally.exe') -PathType Leaf)) {
    Write-Error 'Game not found. In Steam choose art of rally > Manage > Browse local files, then run Install.bat -GameDir "D:\Games\artofrally".'
    exit 1
}
$gameRoot = [IO.Path]::GetFullPath($GameDir).TrimEnd('\', '/')
$modDir = Join-Path $gameRoot ('Mods\' + $modName)
Assert-OrdinaryTarget $gameRoot $modDir
foreach ($name in $modFiles) { Assert-OrdinaryTarget $gameRoot (Join-Path $modDir $name) }

if (Get-Process artofrally -ErrorAction SilentlyContinue) {
    Write-Error 'art of rally is running. Close it before installing or removing this mod.'
    exit 1
}

if ($Uninstall) {
    foreach ($name in $modFiles) {
        $target = Join-Path $modDir $name
        if (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target -Force }
    }
    Write-Host 'Mod files removed. Settings, measurements, other mods, and Unity Mod Manager were kept.' -ForegroundColor Green
    exit 0
}

$umm = Join-Path $gameRoot 'artofrally_Data\Managed\UnityModManager\UnityModManager.dll'
if (-not (Test-Path -LiteralPath $umm -PathType Leaf)) {
    Write-Error 'Unity Mod Manager is not installed for art of rally. Install it for this game first: https://www.nexusmods.com/site/mods/21'
    exit 1
}

try {
    . (Join-Path $packageDir 'verify.ps1')
    $manifest = Assert-Payload $packageDir
} catch {
    Write-Error "The extracted download is incomplete or changed: $($_.Exception.Message)"
    exit 1
}

try {
    New-Item -ItemType Directory -Force -Path $modDir | Out-Null
    foreach ($name in $modFiles) {
        Copy-Item -LiteralPath (Join-Path $packageDir ("$modName/$name")) -Destination (Join-Path $modDir $name) -Force
    }
    foreach ($name in $modFiles) {
        $relative = "$modName/$name"
        if ((Get-FileHash -LiteralPath (Join-Path $modDir $name) -Algorithm SHA256).Hash -ne $manifest.files.$relative) {
            throw "Installed file differs from the package: $name"
        }
    }
} catch {
    Write-Error "Install failed: $($_.Exception.Message). Close the game, check folder permissions, and retry the complete extracted download."
    exit 1
}

Write-Host "Installed version $($manifest.release) to $modDir" -ForegroundColor Green
Write-Host 'Existing settings and screen measurements were preserved.'
Write-Host 'Launch through Steam, open Unity Mod Manager with Ctrl+F10, and choose your triple-screen mode.'
Write-Host 'Read README.txt for screen setup and the current tearing limitation.'
