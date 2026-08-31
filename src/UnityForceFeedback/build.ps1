<#
.SYNOPSIS
    Builds UnityForceFeedback.dll (x64), verifies its exports, optionally installs it.

.DESCRIPTION
    art of rally is a 64-bit Unity player, so this must be an x64 DLL — a 32-bit
    build fails to load with no diagnostic beyond the FFB silently not working.

    The actual compile lives in build.bat because setting up the MSVC environment
    and passing quoted paths through nested cmd invocations from PowerShell is a
    quoting minefield. This wrapper adds export verification and install.

    Note: vcvars64.bat prints "'vswhere.exe' is not recognized" on some machines.
    That comes from inside Microsoft's own script and is harmless — the build
    still succeeds. Only a non-zero exit code means a real failure.

.PARAMETER Install
    Copy the built DLL into the game's Plugins\x86_64 folder.

.PARAMETER GameDir
    art of rally install root. Defaults to the Steam library on this machine.

.EXAMPLE
    .\build.ps1 -Install
#>
[CmdletBinding()]
param(
    [switch]$Install,
    [string]$GameDir = "D:\Program Files (x86)\Steam\steamapps\common\artofrally"
)

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$dll  = Join-Path $here 'build\UnityForceFeedback.dll'

Write-Host "Building x64 UnityForceFeedback.dll..." -ForegroundColor Cyan
& cmd.exe /c "$here\build.bat"
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
if (-not (Test-Path $dll)) { throw "Build reported success but $dll is missing" }

Write-Host "Built $dll" -ForegroundColor Green

# A mangled or missing export is the single most likely way this fails silently
# at runtime: the game's P/Invoke would throw EntryPointNotFoundException deep
# inside a MonoBehaviour and art of rally would just carry on with no FFB.
$vswhere = "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
if (Test-Path $vswhere) {
    $vsPath  = & $vswhere -latest -products * -property installationPath
    $dumpbin = Get-ChildItem "$vsPath\VC\Tools\MSVC\*\bin\Hostx64\x64\dumpbin.exe" -ErrorAction SilentlyContinue |
               Select-Object -First 1 -ExpandProperty FullName
    if ($dumpbin) {
        # Join to a single string first: against an array, -notmatch is a filter
        # that returns the non-matching lines, so it is truthy even on success.
        $exports  = (& $dumpbin /exports $dll) -join "`n"
        $required = 'InitDirectInput','Aquire','SetDeviceForcesXY','StartEffect',
                    'StopEffect','SetAutoCenter','FreeDirectInput'
        $missing  = $required | Where-Object { $exports -notmatch "\s$_\s*(`n|$)" }
        if ($missing) { throw "DLL is missing required exports: $($missing -join ', ')" }
        Write-Host "All 7 exports present and undecorated (x64)." -ForegroundColor Green
    }
}

if ($Install) {
    $plugins = Join-Path $GameDir 'artofrally_Data\Plugins\x86_64'
    if (-not (Test-Path $plugins)) { throw "Game plugin folder not found: $plugins" }
    Copy-Item $dll $plugins -Force
    Write-Host "Installed to $plugins" -ForegroundColor Green
    Write-Host ""
    Write-Host "Trace calls by setting AOSR_FFB_LOG=1 before launching, then read" -ForegroundColor Yellow
    Write-Host "  $env:LOCALAPPDATA\ArtOfSimRally\ffb.log" -ForegroundColor Yellow
}
