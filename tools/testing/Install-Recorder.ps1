<# Installs/removes a LOCAL developer probe. Never included in release packages.
   Build references use the project's GameDir default; destination can be an isolated test layout. #>
[CmdletBinding()]
param([string]$GameDir='D:/Program Files (x86)/Steam/steamapps/common/artofrally', [switch]$Uninstall, [switch]$SkipBuild)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$GameDir=[IO.Path]::GetFullPath($GameDir)
if (Get-Process -Name artofrally -ErrorAction SilentlyContinue) { throw 'Close art of rally before changing the developer probe.' }
if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'artofrally.exe') -PathType Leaf)) { throw 'Not an art of rally installation.' }
$destination=Join-Path $GameDir 'Mods/ArtOfSimRally.DevRecorder'
# A junction could redirect even these two known files into another installation.
$cursor=$destination
while ($cursor) {
    if ((Test-Path -LiteralPath $cursor) -and ((Get-Item -LiteralPath $cursor -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw "Reparse path is not supported: $cursor" }
    $cursor=Split-Path -Parent $cursor
}
$files=@('ArtOfSimRally.DevRecorder.dll','Info.json')
if ($Uninstall) {
    foreach ($name in $files) {
        $target=Join-Path $destination $name
        if (Test-Path -LiteralPath $target -PathType Leaf) { Remove-Item -LiteralPath $target }
    }
    Write-Output 'Developer probe removed; release mod and evidence preserved.'
    return
}
if (-not (Test-Path -LiteralPath (Join-Path $GameDir 'Mods/ArtOfSimRally/ArtOfSimRally.Mod.dll') -PathType Leaf)) { throw 'Install the packaged candidate before the probe.' }
if (-not $SkipBuild) {
    & dotnet build (Join-Path $PSScriptRoot 'Recorder/Recorder.csproj') -c Release --nologo -warnaserror
    if ($LASTEXITCODE -ne 0) { throw 'Developer probe build failed.' }
}
$sources=@((Join-Path $PSScriptRoot 'Recorder/bin/Release/net48/ArtOfSimRally.DevRecorder.dll'), (Join-Path $PSScriptRoot 'Recorder/Info.json'))
foreach ($source in $sources) { if (-not (Test-Path -LiteralPath $source -PathType Leaf)) { throw "Missing probe file: $source" } }
New-Item -ItemType Directory -Path $destination -Force | Out-Null
for ($i=0; $i -lt $files.Count; $i++) { Copy-Item -LiteralPath $sources[$i] -Destination (Join-Path $destination $files[$i]) -Force }
Write-Output "Developer probe installed in $destination. Pause before using Record-Drive.ps1 -Command Start."
