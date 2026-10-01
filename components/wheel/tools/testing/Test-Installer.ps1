<# Tests an extracted package in isolated fake game folders using the same
   Windows PowerShell/batch entry points players run. No game or hardware is used.
   Keeps logs and fixtures in results/installer-* for inspection. #>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackageDirectory)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$package = [IO.Path]::GetFullPath($PackageDirectory)
. (Join-Path $root 'tools/installer/verify.ps1')
$manifest = Assert-Payload $package
$run = Join-Path $root ('results/installer-' + [Guid]::NewGuid().ToString('N'))
$game = Join-Path $run 'game [USB] with spaces'
$mod = Join-Path $game 'Mods/ArtOfSimRally'
$umm = Join-Path $game 'artofrally_Data/Managed/UnityModManager'
$native = Join-Path $game 'artofrally_Data/Plugins/x86_64/UnityForceFeedback.dll'
$ps = Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
$checks = 0
New-Item -ItemType Directory -Path $game -Force | Out-Null
Set-Content -LiteralPath (Join-Path $game 'artofrally.exe') -Value 'fixture; never executed'

function Assert([bool]$Condition, [string]$Message) {
    if (-not $Condition) { throw $Message }
    $script:checks++
}
function Invoke-Installer([string]$Name, [string]$Action = 'Install', [string]$From = $package, [bool]$Fails = $false) {
    # No destructive shell operations: the batch launcher calls its own installer.
    # Quoted paths exercise %* forwarding; redirected stdin makes pause nonblocking.
    $launcher = Join-Path $From ($Action + '.bat')
    $line = '""{0}" -GameDir "{1}" <nul"' -f $launcher, $game
    $start = New-Object Diagnostics.ProcessStartInfo
    $start.FileName = $env:ComSpec
    $start.Arguments = '/d /s /c ' + $line
    $start.UseShellExecute = $false
    $start.CreateNoWindow = $true
    $start.RedirectStandardOutput = $true
    $start.RedirectStandardError = $true
    # Deliberately inherit PSModulePath: the batch launchers must work when
    # called from PowerShell 7 as well as Explorer/Windows PowerShell.
    $process = [Diagnostics.Process]::Start($start)
    $stdout = $process.StandardOutput.ReadToEndAsync()
    $stderr = $process.StandardError.ReadToEndAsync()
    if (-not $process.WaitForExit(30000)) {
        # Only this isolated test helper is terminated; never the real game.
        $process.Kill()
        throw "$Name timed out; see $run"
    }
    $output = @($stdout.Result, $stderr.Result)
    $code = $process.ExitCode
    $process.Dispose()
    $output | Out-File -LiteralPath (Join-Path $run ($Name + '.log')) -Encoding utf8
    Assert $(if ($Fails) { $code -ne 0 } else { $code -eq 0 }) "$Name returned $code; see $run"
    return ($output -join "`n")
}

try {
    # Absent loader must fail through the actual Install.bat, before any payload.
    $log = Invoke-Installer 'missing-umm' -Fails $true
    Assert ($log -match 'Unity Mod Manager is not installed') 'Missing loader guidance absent'
    Assert (-not (Test-Path -LiteralPath $mod)) 'Missing-loader install wrote a mod'
    New-Item -ItemType Directory -Path $umm -Force | Out-Null
    Set-Content -LiteralPath (Join-Path $umm 'UnityModManager.dll') -Value 'fixture; never loaded'

    $log = Invoke-Installer 'fresh-install'
    Assert ($log.Contains("All installed files match release $($manifest.release)")) 'Installed release was not reported'
    foreach ($relative in $PayloadFiles | Where-Object { $_.StartsWith('ArtOfSimRally/') }) {
        $file = Join-Path $game ('Mods/' + $relative)
        Assert ((Get-FileHash -LiteralPath $file).Hash -eq $manifest.files.$relative) "Payload mismatch: $relative"
    }
    Assert ((Get-FileHash -LiteralPath $native).Hash -eq $manifest.files.'ArtOfSimRally/UnityForceFeedback.dll') 'Native plugin copy mismatch'

    $settings = Join-Path $mod 'Settings.xml'
    Set-Content -LiteralPath $settings -Value '<Settings><LandingEffectsEnabled>false</LandingEffectsEnabled><Strength>15</Strength></Settings>'
    $settingsHash = (Get-FileHash -LiteralPath $settings).Hash
    Set-Content -LiteralPath (Join-Path $mod 'user-notes.txt') -Value 'keep'
    $other = Join-Path $game 'Mods/OtherMod'
    New-Item -ItemType Directory -Path $other | Out-Null
    Set-Content -LiteralPath (Join-Path $other 'Info.json') -Value 'keep other mod'
    $null = Invoke-Installer 'upgrade'
    Assert ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Upgrade changed saved preferences'

    # Real file lock causes a failed upgrade, and must survive the batch pause
    # as a nonzero exit. Successful retry restores the complete payload.
    $locked = [IO.File]::Open((Join-Path $mod 'ArtOfSimRally.Mod.dll'), 'Open', 'Read', 'None')
    try { $log = Invoke-Installer 'locked-file' -Fails $true } finally { $locked.Dispose() }
    Assert ($log -match 'successful retry verifies the complete install') 'Locked-file retry guidance absent'
    Assert ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Failed upgrade changed settings'
    $null = Invoke-Installer 'retry-after-lock'

    # Uninstall must not depend on UMM remaining installed.
    Remove-Item -LiteralPath (Join-Path $umm 'UnityModManager.dll')
    $null = Invoke-Installer 'uninstall-without-umm' -Action 'Uninstall'
    Assert ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Uninstall changed settings'
    Assert (Test-Path -LiteralPath (Join-Path $mod 'user-notes.txt')) 'Uninstall removed user notes'
    Assert (Test-Path -LiteralPath (Join-Path $other 'Info.json')) 'Uninstall removed another mod'
    foreach ($relative in $PayloadFiles | Where-Object { $_.StartsWith('ArtOfSimRally/') }) {
        Assert (-not (Test-Path -LiteralPath (Join-Path $game ('Mods/' + $relative)))) "Uninstall retained $relative"
    }
    Assert (-not (Test-Path -LiteralPath $native)) 'Uninstall retained native copy'
    $null = Invoke-Installer 'repeat-uninstall' -Action 'Uninstall'

    # Test rejection using copies, never mutate the candidate under evaluation.
    Set-Content -LiteralPath (Join-Path $umm 'UnityModManager.dll') -Value 'fixture; never loaded'
    $bad = Join-Path $run 'damaged download'
    New-Item -ItemType Directory -Path $bad | Out-Null
    Get-ChildItem -LiteralPath $package | Copy-Item -Destination $bad -Recurse
    Add-Content -LiteralPath (Join-Path $bad 'ArtOfSimRally/Info.json') -Value 'damaged'
    $log = Invoke-Installer 'damaged-download' -From $bad -Fails $true
    Assert ($log -match 'incomplete or has changed') 'Damaged-download guidance absent'
    Assert (-not (Test-Path -LiteralPath (Join-Path $mod 'ArtOfSimRally.Mod.dll'))) 'Damaged package wrote a payload'
    Assert ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Damaged package changed settings'
    Remove-Item -LiteralPath (Join-Path $bad 'verify.ps1')
    $log = Invoke-Installer 'missing-verifier' -From $bad -Fails $true
    Assert ($log -match 'incomplete or has changed') 'Missing verifier guidance absent'

    $output = & $ps -NoProfile -ExecutionPolicy Bypass -File (Join-Path $package 'install.ps1') -GameDir (Join-Path $run 'absent game') 2>&1
    $code = $LASTEXITCODE
    $output | Out-File -LiteralPath (Join-Path $run 'invalid-game.log') -Encoding utf8
    Assert ($code -ne 0 -and ($output -join "`n") -match 'Browse local files') 'Invalid game guidance absent'
    [ordered]@{ status='passed'; assertions=$checks; package=$package; release=$manifest.release; evidence=$run; scope='isolated installer only; no game/UMM GUI or hardware executed' } |
        ConvertTo-Json -Compress | Tee-Object -FilePath (Join-Path $run 'report.json')
} catch {
    [ordered]@{ status='failed'; assertions=$checks; error=$_.Exception.Message; evidence=$run } |
        ConvertTo-Json | Set-Content -LiteralPath (Join-Path $run 'failed.json')
    throw
}
