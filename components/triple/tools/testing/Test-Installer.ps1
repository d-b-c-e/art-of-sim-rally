<# Exercise a packaged installer against an isolated fake game; never touch the
   installed game. Evidence remains under ignored artifacts/. #>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$package = [IO.Path]::GetFullPath($PackageDirectory)
. (Join-Path $root 'tools/installer/verify.ps1')
$manifest = Assert-Payload $package
$run = Join-Path $root ('artifacts/installer-' + [Guid]::NewGuid().ToString('N'))
$game = Join-Path $run 'game [triple screen] with spaces'
$mod = Join-Path $game 'Mods/DbceTripleScreenArtOfRally'
$umm = Join-Path $game 'artofrally_Data/Managed/UnityModManager'
$shell = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$checks = 0
New-Item -ItemType Directory -Force -Path $game | Out-Null
Set-Content -LiteralPath (Join-Path $game 'artofrally.exe') -Value 'fixture; never executed'

function Check([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}
function Invoke-Installer([string]$Name, [string]$From = $package,
    [bool]$Uninstall = $false, [bool]$Fails = $false) {
    $arguments = @('-NoProfile', '-ExecutionPolicy', 'Bypass', '-File', (Join-Path $From 'install.ps1'),
        '-GameDir', $game)
    if ($Uninstall) { $arguments += '-Uninstall' }
    # The failure cases deliberately write to stderr. PowerShell 5 promotes
    # native stderr to a terminating error under Stop, so capture it first.
    $previousPreference = $ErrorActionPreference
    try {
        $ErrorActionPreference = 'Continue'
        $output = & $shell @arguments 2>&1
        $code = $LASTEXITCODE
    } finally {
        $ErrorActionPreference = $previousPreference
    }
    $output | Out-File -LiteralPath (Join-Path $run ($Name + '.log')) -Encoding utf8
    Check $(if ($Fails) { $code -ne 0 } else { $code -eq 0 }) "$Name returned $code; see $run"
    return ($output -join "`n")
}

try {
    $log = Invoke-Installer 'missing-umm' -Fails $true
    Check ($log -match 'Unity Mod Manager is not installed') 'Missing-loader guidance was absent.'
    Check (-not (Test-Path -LiteralPath $mod)) 'Missing-loader install wrote files.'
    New-Item -ItemType Directory -Force -Path $umm | Out-Null
    Set-Content -LiteralPath (Join-Path $umm 'UnityModManager.dll') -Value 'fixture; never loaded'

    $null = Invoke-Installer 'fresh-install'
    foreach ($relative in $PayloadFiles | Where-Object { $_.StartsWith('DbceTripleScreenArtOfRally/') }) {
        $target = Join-Path $game ('Mods/' + $relative)
        Check ((Get-FileHash -LiteralPath $target -Algorithm SHA256).Hash -eq $manifest.files.$relative) "Bad installed file: $relative"
    }
    $settings = Join-Path $mod 'Settings.xml'
    $layout = Join-Path $mod 'desired-layout.json'
    Set-Content -LiteralPath $settings -Value '<Settings><ViewWidthScale>1.1</ViewWidthScale></Settings>'
    Set-Content -LiteralPath $layout -Value '{"user":"measurement fixture"}'
    Set-Content -LiteralPath (Join-Path $mod 'notes.txt') -Value 'keep'
    $other = Join-Path $game 'Mods/OtherMod'
    New-Item -ItemType Directory -Force -Path $other | Out-Null
    Set-Content -LiteralPath (Join-Path $other 'Info.json') -Value 'keep other mod'
    $settingsHash = (Get-FileHash -LiteralPath $settings).Hash
    $layoutHash = (Get-FileHash -LiteralPath $layout).Hash
    $null = Invoke-Installer 'upgrade'
    Check ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Upgrade changed settings.'
    Check ((Get-FileHash -LiteralPath $layout).Hash -eq $layoutHash) 'Upgrade changed measurements.'

    $damaged = Join-Path $run 'damaged package'
    New-Item -ItemType Directory -Force -Path $damaged | Out-Null
    Get-ChildItem -LiteralPath $package | Copy-Item -Destination $damaged -Recurse
    Add-Content -LiteralPath (Join-Path $damaged 'DbceTripleScreenArtOfRally/Info.json') -Value 'damage'
    $log = Invoke-Installer 'damaged-package' -From $damaged -Fails $true
    Check ($log -match 'incomplete or changed') 'Damaged-package guidance was absent.'
    Check ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Damaged package changed settings.'

    $null = Invoke-Installer 'uninstall' -Uninstall $true
    foreach ($relative in $PayloadFiles | Where-Object { $_.StartsWith('DbceTripleScreenArtOfRally/') }) {
        Check (-not (Test-Path -LiteralPath (Join-Path $game ('Mods/' + $relative)))) "Uninstall retained $relative"
    }
    Check ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Uninstall changed settings.'
    Check ((Get-FileHash -LiteralPath $layout).Hash -eq $layoutHash) 'Uninstall changed measurements.'
    Check (Test-Path -LiteralPath (Join-Path $mod 'notes.txt')) 'Uninstall removed another user file.'
    Check (Test-Path -LiteralPath (Join-Path $other 'Info.json')) 'Uninstall removed another mod.'
    $null = Invoke-Installer 'repeat-uninstall' -Uninstall $true

    [ordered]@{ status='passed'; assertions=$checks; release=$manifest.release;
        evidence=$run; scope='isolated fake game only' } | ConvertTo-Json -Compress |
        Tee-Object -FilePath (Join-Path $run 'report.json')
} catch {
    [ordered]@{ status='failed'; assertions=$checks; error=$_.Exception.Message;
        evidence=$run } | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'failed.json')
    throw
}
