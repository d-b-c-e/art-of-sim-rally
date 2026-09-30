<# Build and package only code owned by this project. The ZIP can be dragged
   into Unity Mod Manager or extracted for the double-click installer. #>
[CmdletBinding()]
param(
    [string]$GameDir = $env:ART_OF_RALLY_DIR,
    [string]$OutputDirectory,
    [switch]$AllowDirty
)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $GameDir -or -not (Test-Path -LiteralPath (Join-Path $GameDir 'artofrally.exe') -PathType Leaf)) {
    throw 'Pass -GameDir with the folder containing artofrally.exe, or set ART_OF_RALLY_DIR.'
}
$gameRoot = [IO.Path]::GetFullPath($GameDir)
$info = Get-Content -LiteralPath (Join-Path $root 'src/ArtOfRally.TripleScreen.Mod/Info.json') -Raw | ConvertFrom-Json
$adapter = Get-Content -LiteralPath (Join-Path $root 'adapter-manifest.json') -Raw | ConvertFrom-Json
$version = [string]$info.Version
if ($version -notmatch '^\d+\.\d+\.\d+$' -or $adapter.adapterVersion -ne $version) {
    throw 'Info.json and adapter-manifest.json must have the same X.Y.Z version.'
}
$revision = (& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source revision.' }
$sourceState = if (& git -C $root status --porcelain --untracked-files=all) { 'dirty' } else { 'clean' }
if ($LASTEXITCODE -ne 0) { throw 'Cannot identify source state.' }
if ($sourceState -ne 'clean' -and -not $AllowDirty) {
    throw 'Release packaging requires a clean source tree. Use -AllowDirty only for local test candidates.'
}
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $root 'dist' }
$output = [IO.Path]::GetFullPath($OutputDirectory)
$stage = Join-Path $output ("stage-$version")
$zip = Join-Path $output ("DbceTripleScreenArtOfRally-$version.zip")
if ((Test-Path -LiteralPath $stage) -or (Test-Path -LiteralPath $zip)) {
    throw 'This candidate already exists. Keep it for comparison or choose a new output folder.'
}

& dotnet build (Join-Path $root 'Dbce.TripleScreen.sln') -c Release "-p:GameDir=$gameRoot" --nologo
if ($LASTEXITCODE -ne 0) { throw 'Release build failed.' }
& dotnet (Join-Path $root 'tests/Dbce.TripleScreen.Core.Tests/bin/Release/net8.0/Dbce.TripleScreen.Core.Tests.dll')
if ($LASTEXITCODE -ne 0) { throw 'Geometry and protocol checks failed.' }

. (Join-Path $root 'tools/installer/verify.ps1')
$modName = 'DbceTripleScreenArtOfRally'
$modDir = Join-Path $stage $modName
New-Item -ItemType Directory -Force -Path $modDir | Out-Null
$buildDir = Join-Path $root 'src/ArtOfRally.TripleScreen.Mod/bin/Release/net48'
foreach ($name in @('ArtOfRally.TripleScreen.Mod.dll', 'Dbce.TripleScreen.Core.dll',
        'Dbce.TripleScreen.Protocol.dll', 'Info.json', 'manifest.json')) {
    Copy-Item -LiteralPath (Join-Path $buildDir $name) -Destination (Join-Path $modDir $name)
}
foreach ($name in @('Install.bat', 'Uninstall.bat', 'install.ps1', 'verify.ps1')) {
    Copy-Item -LiteralPath (Join-Path $root ('tools/installer/' + $name)) -Destination (Join-Path $stage $name)
}
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination (Join-Path $stage 'LICENSE')
$readme = (Get-Content -LiteralPath (Join-Path $root 'tools/installer/README.txt') -Raw).Replace('@VERSION@', $version)
$readme | Set-Content -LiteralPath (Join-Path $stage 'README.txt') -Encoding UTF8

$hashes = [ordered]@{}
foreach ($relative in $PayloadFiles) {
    $hashes[$relative] = (Get-FileHash -LiteralPath (Join-Path $stage $relative) -Algorithm SHA256).Hash
}
[ordered]@{
    schema = 1
    release = $version
    sourceRevision = $revision
    sourceState = $sourceState
    files = $hashes
} | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath (Join-Path $stage 'package-manifest.json') -Encoding UTF8
$null = Assert-Payload $stage

New-Item -ItemType Directory -Force -Path $output | Out-Null
Add-Type -AssemblyName System.IO.Compression.FileSystem
[IO.Compression.ZipFile]::CreateFromDirectory($stage, $zip)
(Get-FileHash -LiteralPath $zip -Algorithm SHA256).Hash |
    Set-Content -LiteralPath ($zip + '.sha256') -Encoding ASCII
Write-Host "Packaged $zip" -ForegroundColor Green
Write-Host "Source $revision ($sourceState); version $version"
