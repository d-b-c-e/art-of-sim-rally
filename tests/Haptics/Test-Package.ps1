[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackageDirectory,[Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$count=0
function Assert([bool]$ok,[string]$why) { $script:count++; if(-not $ok) { throw $why } }
$package=[IO.Path]::GetFullPath($PackageDirectory)
$run=[IO.Path]::GetFullPath($OutputDirectory)
$manifest=Get-Content -LiteralPath (Join-Path $package 'manifest.json') -Raw | ConvertFrom-Json
Assert (@(Get-ChildItem -LiteralPath $package -File).Count -eq 5) 'Unexpected payload files'
Assert (@(Get-ChildItem -LiteralPath $package -Filter '*.dll').Count -eq 1) 'Third party dependency packaged'
Assert ([Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $package 'ArtOfSimRally.SimHub.dll')).ProductVersion -eq $manifest.identity) 'Plugin identity differs'
$fake=Join-Path $run 'fake-simhub'
$common=Join-Path $fake 'PluginsData/Common'
New-Item -ItemType Directory -Path $common | Out-Null
Set-Content -LiteralPath (Join-Path $fake 'SimHubWPF.exe') -Value 'test placeholder, never executed'
$bassPath=Join-Path $common 'ShakeITBassShakersSettingsV2.json'
$activationPath=Join-Path $fake 'PluginsData/PluginsActivation.json'
$original=[ordered]@{ProfileId='existing';Name='Original';GameCode=$null;GlobalGain=63.25;EffectsContainers=@(
    @{ContainerType='ImpactEffectContainer';IsEnabled=$true;Gain=99},
    @{ContainerType='JumpContainer';IsEnabled=$true;Gain=80},
    @{ContainerType='WheelsImpactContainer';IsEnabled=$true;Gain=90},
    @{ContainerType='RPMContainer';IsEnabled=$true;Gain=12})}
[ordered]@{Profiles=@($original);activeProfileId='existing';GlobalGain=79.68;LastGameProfiles=@{ets2='existing'}} |
    ConvertTo-Json -Depth 15 | Set-Content -LiteralPath $bassPath -Encoding UTF8
'[{"ClassName":"OtherPlugin","IsEnabled":true}]' | Set-Content -LiteralPath $activationPath
$originalJson=$original | ConvertTo-Json -Depth 15 -Compress
$originalBassHash=(Get-FileHash -LiteralPath $bassPath).Hash
$shell=(Get-Process -Id $PID).Path
$installer=Join-Path $package 'Install-SimHub.ps1'
function Install([string]$scriptPath=$installer) {
    $output=& $shell -NoProfile -File $scriptPath -SimHubDir $fake 2>&1
    if($LASTEXITCODE) { throw ($output -join "`n") }
    $output | Select-Object -Last 1 | ConvertFrom-Json
}
$receipt=Install
$bass=Get-Content -LiteralPath $bassPath -Raw | ConvertFrom-Json
$old=@($bass.Profiles | Where-Object ProfileId -eq 'existing')
Assert ($old.Count -eq 1 -and ($old[0] | ConvertTo-Json -Depth 15 -Compress) -eq $originalJson) 'Original profile changed'
Assert ((Get-FileHash -LiteralPath (Join-Path $receipt.backup 'ShakeITBassShakersSettingsV2.json')).Hash -eq $originalBassHash) 'Original backup differs'
Assert ($bass.GlobalGain -eq 79.68 -and $bass.LastGameProfiles.ets2 -eq 'existing') 'Unrelated settings changed'
$profile=@($bass.Profiles | Where-Object ProfileId -eq $bass.activeProfileId)
Assert ($profile.Count -eq 1 -and $profile[0].GameCode -eq 'FH5' -and $bass.LastGameProfiles.fh5 -eq $profile[0].ProfileId) 'Game profile selection'
$profile=$profile[0]
Assert (@($profile.EffectsContainers | Where-Object { $_.ContainerType -in @('ImpactEffectContainer','JumpContainer','WheelsImpactContainer') -and $_.IsEnabled }).Count -eq 0) 'Duplicate impact effects enabled'
Assert (($profile.EffectsContainers | Where-Object ContainerType -eq 'RPMContainer').Gain -eq 12) 'Engine effect changed'
$effect=@($profile.EffectsContainers | Where-Object ContainerId -eq 'a0561d71-422a-4574-8a73-f99471985fce')
Assert ($effect.Count -eq 1 -and $effect[0].Output.Frequency -eq 30 -and -not $effect[0].AlwaysExecute) 'Landing effect configuration'
foreach($corner in @('FrontLeftFormula','FrontRightFormula','RearLeftFormula','RearRightFormula')) {
    Assert ($effect[0].$corner.Expression -eq 'isnull([ArtOfSimRallyHaptics.LandingPulse], 0)') 'Wrong pulse property'
}
$effect[0].Gain=37
$bass | ConvertTo-Json -Depth 30 | Set-Content -LiteralPath $bassPath -Encoding UTF8
$editedHash=(Get-FileHash -LiteralPath $bassPath).Hash
$receipt=Install
$bass=Get-Content -LiteralPath $bassPath -Raw | ConvertFrom-Json
Assert ($bass.Profiles.Count -eq 2 -and -not $receipt.profileCreated) 'Reinstall duplicated profile'
Assert ((Get-FileHash -LiteralPath $bassPath).Hash -eq $editedHash) 'Reinstall overwrote user tuning'
$activation=@(Get-Content -LiteralPath $activationPath -Raw | ConvertFrom-Json)
Assert ($activation.Count -eq 2 -and $activation[0].ClassName -eq 'OtherPlugin' -and $activation[0].IsEnabled -and $activation[1].IsEnabled) 'Plugin activation preservation'
$tamper=Join-Path $run 'tampered-package'
Copy-Item -LiteralPath $package -Destination $tamper -Recurse
Add-Content -LiteralPath (Join-Path $tamper 'landing-effect.json') -Value 'invalid'
$dllHash=(Get-FileHash -LiteralPath (Join-Path $fake 'ArtOfSimRally.SimHub.dll')).Hash
$output=& $shell -NoProfile -File (Join-Path $tamper 'Install-SimHub.ps1') -SimHubDir $fake 2>&1
Assert ($LASTEXITCODE -ne 0 -and ($output -join "`n") -match 'Invalid haptics payload') 'Tampered package accepted'
Assert ((Get-FileHash -LiteralPath $bassPath).Hash -eq $editedHash -and (Get-FileHash -LiteralPath (Join-Path $fake 'ArtOfSimRally.SimHub.dll')).Hash -eq $dllHash) 'Rejected install changed files'
[ordered]@{status='passed';assertions=$count;audioOutput=$false} | ConvertTo-Json -Compress
