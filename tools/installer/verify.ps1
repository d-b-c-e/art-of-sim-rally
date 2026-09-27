# The release allowlist is shared by packaging and installation.
$PayloadFiles = @(
    'DbceTripleScreenArtOfRally/ArtOfRally.TripleScreen.Mod.dll',
    'DbceTripleScreenArtOfRally/Dbce.TripleScreen.Core.dll',
    'DbceTripleScreenArtOfRally/Dbce.TripleScreen.Protocol.dll',
    'DbceTripleScreenArtOfRally/Info.json',
    'DbceTripleScreenArtOfRally/manifest.json',
    'Install.bat', 'Uninstall.bat', 'install.ps1', 'verify.ps1',
    'README.txt', 'LICENSE'
)

function Assert-Payload([string]$Directory) {
    $base = [IO.Path]::GetFullPath($Directory).TrimEnd('\', '/')
    $manifestPath = Join-Path $base 'package-manifest.json'
    if (-not (Test-Path -LiteralPath $manifestPath -PathType Leaf)) { throw 'Package manifest is missing.' }
    $manifest = Get-Content -LiteralPath $manifestPath -Raw | ConvertFrom-Json
    if ($manifest.schema -ne 1 -or $manifest.release -notmatch '^\d+\.\d+\.\d+$') {
        throw 'Unsupported package manifest or release version.'
    }
    $listed = @($manifest.files.PSObject.Properties.Name)
    if (@(Compare-Object ($PayloadFiles | Sort-Object) ($listed | Sort-Object)).Count) {
        throw 'Package manifest does not match the release file list.'
    }
    $items = @(Get-ChildItem -LiteralPath $base -Recurse -Force)
    if (@($items | Where-Object { $_.Attributes -band [IO.FileAttributes]::ReparsePoint }).Count) {
        throw 'The extracted package contains a linked file or folder.'
    }
    $actual = @($items | Where-Object { -not $_.PSIsContainer } | ForEach-Object {
        $_.FullName.Substring($base.Length + 1).Replace('\', '/')
    })
    if (@(Compare-Object (($PayloadFiles + 'package-manifest.json') | Sort-Object) ($actual | Sort-Object)).Count) {
        throw 'The extracted package has missing or unexpected files.'
    }
    foreach ($relative in $PayloadFiles) {
        $path = Join-Path $base $relative
        if ((Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash -ne $manifest.files.$relative) {
            throw "Package hash mismatch: $relative"
        }
    }
    $info = Get-Content -LiteralPath (Join-Path $base 'DbceTripleScreenArtOfRally/Info.json') -Raw | ConvertFrom-Json
    $adapter = Get-Content -LiteralPath (Join-Path $base 'DbceTripleScreenArtOfRally/manifest.json') -Raw | ConvertFrom-Json
    if ($info.Version -ne $manifest.release -or $adapter.adapterVersion -ne $manifest.release) {
        throw 'Package version does not match mod metadata.'
    }
    return $manifest
}
