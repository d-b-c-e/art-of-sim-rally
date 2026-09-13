<# Installs the optional haptics plugin and a separate FH5 landing profile. Never emits force/audio. #>
[CmdletBinding()]
param([string]$SimHubDir='C:/Program Files (x86)/SimHub')
$ErrorActionPreference='Stop'
$target=[IO.Path]::GetFullPath($SimHubDir).TrimEnd('\','/')
if (-not (Test-Path -LiteralPath (Join-Path $target 'SimHubWPF.exe'))) { throw 'Choose the SimHub installation directory' }
function Assert-Closed {
    foreach ($process in @(Get-Process SimHubWPF -ErrorAction SilentlyContinue)) {
        if (-not $process.Path -or [IO.Path]::GetDirectoryName($process.Path) -eq $target) { throw 'Exit SimHub before installing; current settings must be saved first' }
    }
}
Assert-Closed
$manifest=Get-Content -LiteralPath (Join-Path $PSScriptRoot 'manifest.json') -Raw | ConvertFrom-Json
$names=@('ArtOfSimRally.SimHub.dll','landing-effect.json','Install-SimHub.ps1','README.md')
if (@(Compare-Object $names @($manifest.files.PSObject.Properties.Name)).Count) { throw 'Unexpected haptics package contents' }
foreach ($name in $names) {
    if ((Get-FileHash -LiteralPath (Join-Path $PSScriptRoot $name)).Hash -ne $manifest.files.$name) { throw "Invalid haptics payload: $name" }
}
$bassPath=Join-Path $target 'PluginsData/Common/ShakeITBassShakersSettingsV2.json'
$activationPath=Join-Path $target 'PluginsData/PluginsActivation.json'
$bass=Get-Content -LiteralPath $bassPath -Raw | ConvertFrom-Json
$activation=@(Get-Content -LiteralPath $activationPath -Raw | ConvertFrom-Json)
$source=@($bass.Profiles | Where-Object ProfileId -eq $bass.activeProfileId)
if ($source.Count -ne 1) { throw 'Cannot identify current ShakeIt profile; no files changed' }
$profileId='a0561d71-59ce-47a4-b1f2-6d11c2affea9'
$existing=@($bass.Profiles | Where-Object ProfileId -eq $profileId)
if ($existing.Count -gt 1) { throw 'Duplicate landing profile identity' }
$created=$existing.Count -eq 0
if ($created) {
    $profile=$source[0] | ConvertTo-Json -Depth 50 | ConvertFrom-Json
    $profile.ProfileId=$profileId; $profile.Name='Art of Sim Rally - landing thud'; $profile.GameCode='FH5'
    # Preserve original profile verbatim. Only the new profile avoids duplicate generic impact cues.
    foreach ($effect in $profile.EffectsContainers) {
        if ($effect.ContainerType -in @('ImpactEffectContainer','JumpContainer','WheelsImpactContainer')) { $effect.IsEnabled=$false }
    }
    $profile.EffectsContainers=@($profile.EffectsContainers)+@(Get-Content -LiteralPath (Join-Path $PSScriptRoot 'landing-effect.json') -Raw | ConvertFrom-Json)
    $bass.Profiles=@($bass.Profiles)+@($profile)
}
$bass.activeProfileId=$profileId
if ($null -eq $bass.LastGameProfiles) { $bass | Add-Member -NotePropertyName LastGameProfiles -NotePropertyValue ([pscustomobject]@{}) -Force }
$bass.LastGameProfiles | Add-Member -NotePropertyName fh5 -NotePropertyValue $profileId -Force
$class='ArtOfSimRally.SimHub.ArtOfSimRallyHaptics'
$entry=@($activation | Where-Object ClassName -eq $class)
if ($entry.Count -gt 1) { throw 'Duplicate plugin activation entry' }
if ($entry.Count) { $entry[0].IsEnabled=$true }
else { $activation+= [pscustomobject]@{ClassName=$class;IsEnabled=$true;ShowInMainMenu=$false;ShowInMainMenuPosition=0} }
$backup=Join-Path $target ('PluginsData/ArtOfSimRally-backups/'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $backup | Out-Null
Copy-Item -LiteralPath $bassPath,$activationPath -Destination $backup
$dll=Join-Path $target 'ArtOfSimRally.SimHub.dll'
if(Test-Path -LiteralPath $dll) { Copy-Item -LiteralPath $dll -Destination $backup }
Assert-Closed
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'ArtOfSimRally.SimHub.dll') -Destination $dll
$bass | ConvertTo-Json -Depth 50 | Set-Content -LiteralPath $bassPath -Encoding UTF8
ConvertTo-Json -InputObject $activation -Depth 30 | Set-Content -LiteralPath $activationPath -Encoding UTF8
if((Get-FileHash -LiteralPath $dll).Hash -ne $manifest.files.'ArtOfSimRally.SimHub.dll') { throw 'Installed plugin hash mismatch' }
$receipt=[ordered]@{status='installed';release=$manifest.release;identity=$manifest.identity;utc=[DateTime]::UtcNow.ToString('O');backup=$backup;
    simHubDirectory=$target;pluginSha256=(Get-FileHash -LiteralPath $dll).Hash;profileId=$profileId;profileCreated=$created;
    originalProfileId=$source[0].ProfileId;globalGainPreserved=$bass.GlobalGain;audioTested=$false;attended='pending'}
$receipt | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $backup 'receipt.json')
$receipt | ConvertTo-Json -Compress
