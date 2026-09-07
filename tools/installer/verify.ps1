# Shared by packaging, the installer, and the RC gate. Integrity, not a signature.
$PayloadFiles = @(
    'ArtOfSimRally/ArtOfSimRally.Mod.dll', 'ArtOfSimRally/Dbce.Wheel.Telemetry.dll', 'ArtOfSimRally/Dbce.Wheel.Ffb.dll',
    'ArtOfSimRally/UnityForceFeedback.dll', 'ArtOfSimRally/Info.json', 'ArtOfSimRally/build.json',
    'LICENSE', 'README.txt', 'Install.bat', 'Uninstall.bat', 'install.ps1', 'verify.ps1'
)

function Assert-Payload([string]$Directory) {
    $base = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/')
    $manifest = Get-Content -LiteralPath (Join-Path $base 'manifest.json') -Raw | ConvertFrom-Json
    if ($manifest.schema -ne 1) { throw 'Unsupported package manifest' }
    $listed = @($manifest.files.PSObject.Properties.Name)
    if (@(Compare-Object ($PayloadFiles | Sort-Object) ($listed | Sort-Object)).Count) {
        throw 'Manifest payload list differs from the release allowlist'
    }
    $actual = @(Get-ChildItem -LiteralPath $base -File -Recurse | ForEach-Object {
        $_.FullName.Substring($base.Length + 1).Replace('\', '/')
    })
    if (@(Compare-Object (($PayloadFiles + 'manifest.json') | Sort-Object) ($actual | Sort-Object)).Count) {
        throw 'Package has missing or unexpected files'
    }
    foreach ($relative in $PayloadFiles) {
        $path = Join-Path $base $relative
        if ((Get-Item -LiteralPath $path).Attributes -band [IO.FileAttributes]::ReparsePoint) { throw "Linked payload: $relative" }
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $manifest.files.$relative) {
            throw "Package hash mismatch: $relative"
        }
    }
    $info = Get-Content -LiteralPath (Join-Path $base 'ArtOfSimRally/Info.json') -Raw | ConvertFrom-Json
    $build = Get-Content -LiteralPath (Join-Path $base 'ArtOfSimRally/build.json') -Raw | ConvertFrom-Json
    if ($info.Version -ne $build.modVersion -or $build.release -ne $manifest.release) { throw 'Package version mismatch' }
    return $manifest
}
