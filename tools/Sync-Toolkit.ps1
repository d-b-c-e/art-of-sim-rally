<#
.SYNOPSIS
    Vendors a pinned dbce-wheel-mod-toolkit release into a consumer repo.

.DESCRIPTION
    Consumers do not reference this repo's projects; they vendor BUILT artifacts
    pinned by version, so a toolkit change never reaches a game without an
    explicit, diffable bump. Copy this script into the consumer's tools/ folder
    and run it there. It downloads the release zip (or uses a local build),
    extracts the pieces the consumer needs into lib\toolkit\, and writes
    lib\toolkit\VERSION.

    The native DLL keeps its name (WheelFfb.dll): the managed binding imports
    "WheelFfb", so a consumer that uses Dbce.Wheel.Ffb must ship it under that
    name. Only a game that P/Invokes a specific file name itself needs -RenameNative.

.PARAMETER Version
    Release tag to vendor, e.g. v0.1.0.

.PARAMETER LocalRepo
    Use a local checkout's build output instead of a GitHub release (development).

.PARAMETER Destination
    Folder to populate. Default lib\toolkit under the consumer repo root.

.PARAMETER Parts
    Which artifact groups to copy: native, dotnet, tools, knowledge. Default native,dotnet.

.EXAMPLE
    .\tools\Sync-Toolkit.ps1 -Version v0.1.0
    .\tools\Sync-Toolkit.ps1 -LocalRepo E:\Source\dbce-wheel-mod-toolkit -Parts native,dotnet,tools
#>
[CmdletBinding()]
param(
    [string]$Version,
    [string]$LocalRepo,
    [string]$Destination,
    [string[]]$Parts = @('native', 'dotnet'),
    [string]$Repo = 'd-b-c-e/dbce-wheel-mod-toolkit'
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
if (-not $Destination) { $Destination = Join-Path $root 'lib\toolkit' }
New-Item -ItemType Directory -Force -Path $Destination | Out-Null

$stage = Join-Path $env:TEMP ("wheel-toolkit-" + [guid]::NewGuid().ToString('n'))
New-Item -ItemType Directory -Force -Path $stage | Out-Null
try {
    if ($LocalRepo) {
        Write-Host "Using local build at $LocalRepo" -ForegroundColor Cyan
        $src = @{
            native    = Join-Path $LocalRepo 'native\wheelffb\build'
            dotnet    = Join-Path $LocalRepo 'dotnet\Dbce.Wheel.Ffb\bin\Release\netstandard2.0'
            tools     = Join-Path $LocalRepo 'tools'
            knowledge = Join-Path $LocalRepo 'knowledge'
        }
        $ver = 'local'
        try { $ErrorActionPreference = 'Continue'; $v = & git -C $LocalRepo describe --tags --always 2>$null; if ($LASTEXITCODE -eq 0 -and $v) { $ver = "$v" } } catch { } finally { $ErrorActionPreference = 'Stop' }
    } else {
        if (-not $Version) { throw "Pass -Version vX.Y.Z or -LocalRepo <path>" }
        Write-Host "Downloading $Repo $Version" -ForegroundColor Cyan
        gh release download $Version --repo $Repo --pattern 'dbce-wheel-mod-toolkit-*.zip' --dir $stage
        $zip = Get-ChildItem $stage -Filter '*.zip' | Select-Object -First 1
        if (-not $zip) { throw "No release zip found for $Version" }
        Expand-Archive $zip.FullName -DestinationPath (Join-Path $stage 'x') -Force
        $inner = Get-ChildItem (Join-Path $stage 'x') -Directory | Select-Object -First 1
        $src = @{
            native    = Join-Path $inner.FullName 'native'
            dotnet    = Join-Path $inner.FullName 'dotnet'
            tools     = Join-Path $inner.FullName 'tools'
            knowledge = Join-Path $inner.FullName 'knowledge'
        }
        $ver = $Version
    }

    foreach ($part in $Parts) {
        $from = $src[$part]
        if (-not $from -or -not (Test-Path $from)) { Write-Warning "part '$part' not found at $from"; continue }
        $to = Join-Path $Destination $part
        New-Item -ItemType Directory -Force -Path $to | Out-Null
        switch ($part) {
            'native' { Copy-Item (Join-Path $from 'WheelFfb.dll') $to -Force }
            'dotnet' { Copy-Item (Join-Path $from 'Dbce.Wheel.*.dll') $to -Force; Copy-Item (Join-Path $from 'Dbce.Wheel.*.xml') $to -Force -ErrorAction SilentlyContinue }
            default  { Copy-Item (Join-Path $from '*') $to -Recurse -Force }
        }
        Write-Host "  $part -> $to" -ForegroundColor Green
    }
    Set-Content (Join-Path $Destination 'VERSION') "$ver`n$(Get-Date -Format s)"
    Write-Host "Vendored $ver into $Destination" -ForegroundColor Green
} finally {
    Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
}
