param(
    [Parameter(Mandatory = $true)]
    [string] $ModDirectory,

    [string] $CanonicalLayoutPath = (Join-Path $env:LOCALAPPDATA 'DBCE\TripleScreen\games\art-of-rally\desired-layout.json'),

    [switch] $Apply
)

$ErrorActionPreference = 'Stop'

$modPath = [System.IO.Path]::GetFullPath($ModDirectory)
$stagedPath = Join-Path $modPath 'desired-layout.json'
if (-not (Test-Path -LiteralPath $stagedPath -PathType Leaf)) {
    throw "The installed adapter layout was not found: $stagedPath"
}

$layout = Get-Content -LiteralPath $stagedPath -Raw | ConvertFrom-Json -AsHashtable
if ($layout.schemaVersion -ne 1 -or $layout.panel.count -ne 3 -or
    $layout.panel.nativeWidthPx -le 0 -or $layout.panel.nativeHeightPx -le 0 -or
    $layout.output.mode -notin @('nvidia-surround', 'borderless-span', 'separate-displays')) {
    throw 'The staged layout is not a supported three-panel contract.'
}

$layout.output = @{ mode = 'separate-displays' }
$payload = $layout | ConvertTo-Json -Depth 12
Write-Output "Separate-display candidate: three $($layout.panel.nativeWidthPx)x$($layout.panel.nativeHeightPx) panels; center must be Windows primary."
Write-Output "Staged: $stagedPath"
Write-Output "Canonical: $CanonicalLayoutPath"
if (-not $Apply) {
    Write-Output 'Dry run only. Close the game, switch to separate displays, then rerun with -Apply.'
    return
}

if (Get-Process -Name 'artofrally', 'art of rally' -ErrorAction SilentlyContinue) {
    throw 'Close Art of Rally before changing its layout contract.'
}

$canonicalPath = [System.IO.Path]::GetFullPath($CanonicalLayoutPath)
$timestamp = Get-Date -Format 'yyyyMMdd-HHmmss'
foreach ($target in @($stagedPath, $canonicalPath)) {
    $parent = Split-Path -Parent $target
    if (-not (Test-Path -LiteralPath $parent -PathType Container)) {
        New-Item -ItemType Directory -Path $parent -Force | Out-Null
    }
    if (Test-Path -LiteralPath $target -PathType Leaf) {
        Copy-Item -LiteralPath $target -Destination "$target.before-separate-$timestamp.bak"
    }
}

$encoding = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($stagedPath, $payload + [Environment]::NewLine, $encoding)
[System.IO.File]::WriteAllText($canonicalPath, $payload + [Environment]::NewLine, $encoding)
Write-Output 'Separate-display contracts staged with timestamped backups. Do not run Apply in Triple Screen Optimizer until it supports this output mode; its current Art of Rally action would restore Surround mode.'
