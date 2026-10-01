[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackageDirectory,[Parameter(Mandatory)][string]$OutputDirectory,[Parameter(Mandatory)][string[]]$LegacyPackageDirectories)
$ErrorActionPreference='Stop'
. (Join-Path $PackageDirectory 'verify.ps1')
$m=Assert-UnifiedPackage $PackageDirectory
$checks=0;$cases=@()
function Check([bool]$Value,[string]$Message){$script:checks++;if(-not $Value){throw $Message}}
function Snapshot([string]$Root){$map=[ordered]@{};Get-ChildItem -LiteralPath $Root -Recurse -File -Force|ForEach-Object {$map[$_.FullName.Substring($Root.Length+1)]=Hash $_.FullName};return ($map|ConvertTo-Json -Compress)}
function New-Game([string]$Name,[string]$Mode){
 $game=[IO.Path]::GetFullPath((Join-Path $OutputDirectory ($Name+' [game] with spaces')))
 New-Item -ItemType Directory -Path (Join-Path $game 'artofrally_Data/Managed/UnityModManager') -Force|Out-Null
 'fixture'|Set-Content -LiteralPath (Join-Path $game 'artofrally.exe')
 'fixture'|Set-Content -LiteralPath (Join-Path $game 'artofrally_Data/Managed/UnityModManager/UnityModManager.dll')
 if($Mode -ne 'fresh'){
  foreach($dir in $LegacyPackageDirectories){
   $wheel=Test-Path -LiteralPath (Join-Path $dir 'ArtOfSimRally/Info.json')
   $name=if($wheel){'ArtOfSimRally'}else{'DbceTripleScreenArtOfRally'}
   if(($Mode -eq 'both') -or ($Mode -eq 'wheel' -and $wheel) -or ($Mode -eq 'triple' -and -not $wheel)){
    $dest=Join-Path $game ('Mods/'+$name)
    # Use the first inventory for each feature; later inventories are tested separately.
    if(-not(Test-Path -LiteralPath $dest)){
     New-Item -ItemType Directory -Force (Split-Path -Parent $dest)|Out-Null
     Copy-Item -LiteralPath (Join-Path $dir $name) -Destination $dest -Recurse
     if($wheel){$native=Join-Path $game 'artofrally_Data/Plugins/x86_64';New-Item -ItemType Directory -Force $native|Out-Null;Copy-Item -LiteralPath (Join-Path $dest 'UnityForceFeedback.dll') -Destination $native}
    }
   }
  }
 }
 foreach($dir in @('Mods/ArtOfSimRally','Mods/DbceTripleScreenArtOfRally','Mods/UnrelatedRecorder')){New-Item -ItemType Directory -Force (Join-Path $game $dir)|Out-Null}
 [IO.File]::WriteAllBytes((Join-Path $game 'Mods/ArtOfSimRally/Settings.xml'),[byte[]](0,255,13,10,70,70,66))
 [IO.File]::WriteAllBytes((Join-Path $game 'Mods/DbceTripleScreenArtOfRally/Settings.xml'),[byte[]](1,254,13,10,84,82,73))
 'private layout'|Set-Content -LiteralPath (Join-Path $game 'Mods/DbceTripleScreenArtOfRally/desired-layout.json')
 'recorder unchanged'|Set-Content -LiteralPath (Join-Path $game 'Mods/UnrelatedRecorder/private.bin')
 return $game
}
function Invoke-Setup([string]$Game,[string[]]$Flags=@(),[bool]$Failure=$false,[string]$Package=$PackageDirectory){
 $log=& "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -ExecutionPolicy Bypass -File (Join-Path $Package 'install.ps1') -GameDir $Game @Flags 2>&1
 $code=$LASTEXITCODE
 $log|Out-File -LiteralPath (Join-Path $OutputDirectory ('operation-'+[Guid]::NewGuid().ToString('N')+'.log')) -Encoding utf8
 Check (($code -ne 0) -eq $Failure) "Unexpected installer exit $code for $($Flags -join ' '): $log"
}
function Check-Protected([string]$Game,$Hashes){foreach($p in $Hashes.Keys){Check ((Hash (Join-Path $Game $p)) -eq $Hashes[$p]) "Protected bytes changed: $p"}}
function Protected([string]$Game){$h=@{};foreach($p in @('Mods/ArtOfSimRally/Settings.xml','Mods/DbceTripleScreenArtOfRally/Settings.xml','Mods/DbceTripleScreenArtOfRally/desired-layout.json','Mods/UnrelatedRecorder/private.bin')){$h[$p]=Hash (Join-Path $Game $p)};return $h}
New-Item -ItemType Directory -Force $OutputDirectory|Out-Null
foreach($mode in @('fresh','wheel','triple','both')){
 $game=New-Game $mode $mode;$protected=Protected $game;$before=Snapshot $game
 $original=@{};foreach($p in $OwnedPaths+$LegacyPaths){$original[$p]=Hash (Join-Path $game $p)}
 Invoke-Setup $game @('-DryRun');Check ((Snapshot $game) -eq $before) 'DryRun wrote files'
 Invoke-Setup $game
 foreach($p in $OwnedPaths){Check ((Hash (Join-Path $game $p)) -eq $m.files.('payload/'+$p)) "Unified payload mismatch: $p"}
 foreach($p in $LegacyPaths){Check (-not(Test-Path -LiteralPath (Join-Path $game $p))) 'Legacy renderer still present'}
 Check-Protected $game $protected
 Invoke-Setup $game @('-Rollback')
 foreach($p in $OwnedPaths+$LegacyPaths){Check ((Hash (Join-Path $game $p)) -eq $original[$p]) 'Rollback differs from original owned bytes'}
 Check-Protected $game $protected
 Invoke-Setup $game;Invoke-Setup $game;Check-Protected $game $protected
 Invoke-Setup $game @('-Uninstall');foreach($p in $OwnedPaths+$LegacyPaths){Check (-not(Test-Path -LiteralPath (Join-Path $game $p))) 'Uninstall retained payload'}
 Check-Protected $game $protected
 Invoke-Setup $game @('-Rollback');foreach($p in $OwnedPaths){Check ((Hash (Join-Path $game $p)) -eq $m.files.('payload/'+$p)) 'Uninstall rollback failed'}
 $cases += $mode
}
$game=New-Game 'changed-legacy' 'both';'changed'|Set-Content -LiteralPath (Join-Path $game 'Mods/DbceTripleScreenArtOfRally/ArtOfRally.TripleScreen.Mod.dll');$before=Snapshot $game
Invoke-Setup $game @() $true;Check ((Snapshot $game) -eq $before) 'Changed legacy rejection wrote files';$cases+='changed legacy blocked'
$game=New-Game 'changed-unified' 'fresh';Invoke-Setup $game;'user edited payload'|Set-Content -LiteralPath (Join-Path $game 'Mods/ArtOfSimRally/ArtOfSimRally.Mod.dll');$before=Snapshot $game
foreach($flags in @(@('-Uninstall'),@('-Rollback'),@())){Invoke-Setup $game $flags $true;Check ((Snapshot $game) -eq $before) 'Changed unified rejection wrote files'};$cases+='changed unified blocked'
$game=New-Game 'copy-failure' 'both';$before=@{};foreach($p in $OwnedPaths+$LegacyPaths){$before[$p]=Hash (Join-Path $game $p)};$protected=Protected $game
$global:ArtUnifiedTestFault=$false
function Copy-Item {
 [CmdletBinding()]param([string[]]$LiteralPath,[string]$Destination,[switch]$Force,[switch]$Recurse)
 if(-not $global:ArtUnifiedTestFault -and $LiteralPath[0] -like '*payload*ArtOfRally.TripleScreen.Mod.dll'){$global:ArtUnifiedTestFault=$true;throw 'Injected one-time copy failure'}
 Microsoft.PowerShell.Management\Copy-Item @PSBoundParameters
}
try{& (Join-Path $PackageDirectory 'install.ps1') -GameDir $game;throw 'Copy fault did not fail'}catch{Check ($_.Exception.Message -like '*Injected one-time*') 'Wrong failure'}
Remove-Item Function:\Copy-Item
Check $global:ArtUnifiedTestFault 'Fault injection not reached'
Remove-Variable ArtUnifiedTestFault -Scope Global
foreach($p in $before.Keys){Check ((Hash (Join-Path $game $p)) -eq $before[$p]) 'Automatic rollback did not restore original bytes'}
Check-Protected $game $protected;$cases+='copy failure rollback'
$game=New-Game 'interrupted' 'both';Invoke-Setup $game
$receipt=Get-Content -LiteralPath (Join-Path $game '.dbce-art-unified/receipt.json') -Raw|ConvertFrom-Json
Copy-Item -LiteralPath (Join-Path $game '.dbce-art-unified/receipt.json') -Destination (Join-Path $game '.dbce-art-unified/pending.json')
$p='Mods/ArtOfSimRally/ArtOfSimRally.Mod.dll'
Copy-Item -LiteralPath (Join-Path $game ('.dbce-art-unified/backups/'+$receipt.backupId+'/'+$p)) -Destination (Join-Path $game $p) -Force
Invoke-Setup $game @() $true;Invoke-Setup $game @('-Rollback');foreach($p in $OwnedPaths+$LegacyPaths){Check ((Hash (Join-Path $game $p)) -eq $receipt.before.$p) 'Interrupted rollback differs'};$cases+='interrupted recovery'
$bad=Join-Path $OutputDirectory 'corrupt package';Copy-Item -LiteralPath $PackageDirectory -Destination $bad -Recurse
'bad'|Set-Content -LiteralPath (Join-Path $bad 'payload/Mods/ArtOfSimRally/ArtOfSimRally.Mod.dll')
$game=New-Game 'corrupt-package' 'both';$before=Snapshot $game;Invoke-Setup $game @() $true $bad;Check ((Snapshot $game) -eq $before) 'Corrupt package changed target';$cases+='corrupt package blocked'
$game=New-Game 'linked-target' 'fresh';$external=Join-Path $OutputDirectory 'junction destination';New-Item -ItemType Directory -Force $external|Out-Null
New-Item -ItemType Junction -Path (Join-Path $game '.dbce-art-unified') -Target ([IO.Path]::GetFullPath($external))|Out-Null
Invoke-Setup $game @() $true;Check (@(Get-ChildItem -LiteralPath $external -Force).Count -eq 0) 'Junction escaped target';$cases+='linked target blocked'
$game=New-Game 'duplicate-owner' 'both';New-Item -ItemType Directory -Force (Join-Path $game 'Mods/Duplicate')|Out-Null
'{"Id":"ArtOfSimRally"}'|Set-Content -LiteralPath (Join-Path $game 'Mods/Duplicate/Info.json');$before=Snapshot $game
Invoke-Setup $game @() $true;Check ((Snapshot $game) -eq $before) 'Duplicate owner rejection wrote files';$cases+='duplicate owner blocked'
$game=New-Game 'settings-after-install' 'both';Invoke-Setup $game
'new owner settings'|Set-Content -LiteralPath (Join-Path $game 'Mods/ArtOfSimRally/Settings.xml');$protected=Protected $game
Invoke-Setup $game @('-Rollback');Check-Protected $game $protected;$cases+='post-install settings preserved'
$game=New-Game 'dual-install' 'fresh';Invoke-Setup $game
Copy-Item -LiteralPath (Join-Path $LegacyPackageDirectories[1] 'DbceTripleScreenArtOfRally/ArtOfRally.TripleScreen.Mod.dll') -Destination (Join-Path $game 'Mods/DbceTripleScreenArtOfRally/ArtOfRally.TripleScreen.Mod.dll')
$before=Snapshot $game;Invoke-Setup $game @() $true;Check ((Snapshot $game) -eq $before) 'Dual install rejection wrote files';$cases+='dual installed renderer blocked'
[ordered]@{status='PASS';assertions=$checks;cases=$cases;packageVersion=$m.version;scope='Disposable game folders; real Windows PowerShell installer; no game/device execution'}|ConvertTo-Json -Depth 5|Set-Content -LiteralPath (Join-Path $OutputDirectory 'result.json') -Encoding UTF8
Get-Content -LiteralPath (Join-Path $OutputDirectory 'result.json')
