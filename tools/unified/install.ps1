[CmdletBinding()]
param([Parameter(Mandatory)][string]$GameDir,[switch]$DryRun,[switch]$Uninstall,[switch]$Rollback)
$ErrorActionPreference='Stop'
. (Join-Path $PSScriptRoot 'verify.ps1')
$game=[IO.Path]::GetFullPath($GameDir).TrimEnd('\','/')
if(-not(Test-Path -LiteralPath (Join-Path $game 'artofrally.exe') -PathType Leaf)){throw 'Select the Art of Rally game directory'}
if(Get-Process artofrally -ErrorAction SilentlyContinue){throw 'Close Art of Rally before setup'}
if(@($Uninstall,$Rollback|Where-Object {$_}).Count -gt 1){throw 'Choose one operation'}
$null=Assert-Path $game 'Mods/ArtOfSimRally/Info.json'
$state=Assert-Path $game '.dbce-art-unified/receipt.json'
$pending=Assert-Path $game '.dbce-art-unified/pending.json'
$all=@($OwnedPaths+$LegacyPaths)
foreach($p in $all){$null=Assert-Path $game $p}
if(-not $Rollback){
 $mods=Join-Path $game 'Mods'
 if(Test-Path -LiteralPath $mods){
  foreach($dir in Get-ChildItem -LiteralPath $mods -Directory){
   $infoPath=Join-Path $dir.FullName 'Info.json'
   if(-not(Test-Path -LiteralPath $infoPath)){continue}
   if((Get-Item -LiteralPath $infoPath).Length -gt 32768){throw 'Oversized mod metadata'}
   $info=Get-Content -LiteralPath $infoPath -Raw|ConvertFrom-Json
   if(($info.Id -eq 'ArtOfSimRally' -and $dir.Name -ne 'ArtOfSimRally') -or ($info.Id -eq 'DbceTripleScreenArtOfRally' -and $dir.Name -ne 'DbceTripleScreenArtOfRally')){throw 'Duplicate legacy/unified load owner blocks setup'}
  }
 }
}

function Read-Receipt([string]$Path){
 $r=Get-Content -LiteralPath $Path -Raw|ConvertFrom-Json
 if($r.schema -ne 1 -or $r.packageId -ne 'dbce-mods-art-of-rally'){throw 'Invalid installation receipt'}
 if($r.backupId -notmatch '^[a-f0-9]{32}$'){throw 'Invalid backup identity'}
 if(@(Compare-Object ($all|Sort-Object) (@($r.before.PSObject.Properties.Name)|Sort-Object)).Count -or @(Compare-Object ($all|Sort-Object) (@($r.after.PSObject.Properties.Name)|Sort-Object)).Count){throw 'Receipt inventory mismatch'}
 foreach($p in $r.after.PSObject.Properties.Name){if($p -notin $all -or ($r.after.$p -and $r.after.$p -notmatch '^[A-Fa-f0-9]{64}$')){throw 'Unsafe receipt inventory'}}
 foreach($p in $all){if($r.before.$p -and $r.before.$p -notmatch '^[A-Fa-f0-9]{64}$'){throw 'Invalid backup hash'}}
 return $r
}
function Restore-Transaction($r){
 # Verify all targets and backups before the first restore, including interrupted operations.
 foreach($p in $all){
  $target=Assert-Path $game $p; $current=Hash $target
  if($current -ne $r.before.$p -and $current -ne $r.after.$p){throw "Changed file blocks rollback: $p"}
  if($r.before.$p){$backup=Assert-Path $game ('.dbce-art-unified/backups/'+$r.backupId+'/'+$p); if((Hash $backup) -ne $r.before.$p){throw "Damaged backup: $p"}}
 }
 foreach($p in $all){
  $target=Assert-Path $game $p
  if((Hash $target) -eq $r.before.$p){continue}
  if($r.before.$p){
   New-Item -ItemType Directory -Force (Split-Path -Parent $target)|Out-Null
   Copy-Item -LiteralPath (Assert-Path $game ('.dbce-art-unified/backups/'+$r.backupId+'/'+$p)) -Destination $target -Force
  }elseif(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target -Force}
 }
 foreach($p in $all){if((Hash (Join-Path $game $p)) -ne $r.before.$p){throw 'Restore verification failed'}}
}
function Restore-PreviousReceipt($r){
 $prior=Assert-Path $game ('.dbce-art-unified/backups/'+$r.backupId+'/previous-receipt.json')
 if($r.previousReceiptSha256){
  if((Hash $prior) -ne $r.previousReceiptSha256){throw 'Previous receipt backup is damaged'}
  Copy-Item -LiteralPath $prior -Destination $state -Force
  if((Hash $state) -ne $r.previousReceiptSha256){throw 'Previous receipt restoration failed'}
 }elseif(Test-Path -LiteralPath $state){Remove-Item -LiteralPath $state -Force}
}
if($Rollback){
 $path=if(Test-Path -LiteralPath $pending){$pending}else{$state}
 if(-not(Test-Path -LiteralPath $path)){throw 'No transaction to roll back'}
 $r=Read-Receipt $path
 $prior=Assert-Path $game ('.dbce-art-unified/backups/'+$r.backupId+'/previous-receipt.json')
 if($r.previousReceiptSha256){if((Hash $prior) -ne $r.previousReceiptSha256){throw 'Previous receipt backup is damaged'};$null=Read-Receipt $prior}
 elseif(Test-Path -LiteralPath $prior){throw 'Unexpected previous receipt backup'}
 if($DryRun){Write-Output 'Rollback inventory available; no files changed';return}
 Restore-Transaction $r
 Restore-PreviousReceipt $r
 if(Test-Path -LiteralPath $pending){Remove-Item -LiteralPath $pending -Force}
 Write-Output 'Previous owned payload restored; user settings retained';return
}
if(Test-Path -LiteralPath $pending){throw 'Interrupted setup detected. Run Rollback.bat before continuing'}
$m=Assert-UnifiedPackage $PSScriptRoot
$before=[ordered]@{}; $after=[ordered]@{}
foreach($p in $all){$before[$p]=Hash (Join-Path $game $p); $after[$p]=if(-not $Uninstall -and $p -in $OwnedPaths){$m.files.('payload/'+$p)}else{$null}}
if(Test-Path -LiteralPath $state){
 $old=Read-Receipt $state
 foreach($p in $all){if($before[$p] -ne $old.after.$p){throw "Changed or conflicting installed file: $p"}}
}else{
 if($Uninstall){throw 'No unified ownership receipt; use the original installer'}
 foreach($group in @('wheel','triple')){
  $paths=if($group -eq 'wheel'){@($all|Where-Object {$_ -like 'Mods/ArtOfSimRally/*' -or $_ -like 'artofrally_Data/*'})}else{@($all|Where-Object {$_ -like 'Mods/DbceTripleScreenArtOfRally/*'})}
  $present=@($paths|Where-Object {$before[$_]})
  if($present.Count){
   $matches=@($m.legacyProfiles|Where-Object {
    $profile=$_; $profile.component -eq $group -and @($present|Where-Object {$profile.files.$_ -ne $before[$_]}).Count -eq 0
   })
   if(-not $matches.Count){throw "Unowned or changed $group payload blocks migration; retain settings and obtain its exact validated package"}
  }
 }
}
# Neither normal install nor removal changes settings/layouts. Back up their existing bytes too.
$protected=@('Mods/ArtOfSimRally/Settings.xml','Mods/DbceTripleScreenArtOfRally/Settings.xml','Mods/DbceTripleScreenArtOfRally/desired-layout.json')
foreach($p in $protected){$null=Assert-Path $game $p}
if($DryRun){Write-Output "Validated $($m.version): owned payload transaction; settings remain in place";return}
if(-not $Uninstall -and -not(Test-Path -LiteralPath (Join-Path $game 'artofrally_Data/Managed/UnityModManager/UnityModManager.dll'))){throw 'Install Unity Mod Manager first'}
$id=[Guid]::NewGuid().ToString('N')
$r=[ordered]@{schema=1;packageId=$m.packageId;version=$m.version;backupId=$id;before=$before;after=$after;previousReceiptSha256=(Hash $state);operation=$(if($Uninstall){'uninstall'}else{'install'})}
foreach($p in $all+$protected){
 $src=Assert-Path $game $p
 if(Test-Path -LiteralPath $src -PathType Leaf){$dst=Assert-Path $game ('.dbce-art-unified/backups/'+$id+'/'+$p);New-Item -ItemType Directory -Force (Split-Path -Parent $dst)|Out-Null;Copy-Item -LiteralPath $src -Destination $dst}
}
New-Item -ItemType Directory -Force (Split-Path -Parent $pending)|Out-Null
if(Test-Path -LiteralPath $state){Copy-Item -LiteralPath $state -Destination (Assert-Path $game ('.dbce-art-unified/backups/'+$id+'/previous-receipt.json'))}
$r|ConvertTo-Json -Depth 8|Set-Content -LiteralPath $pending -Encoding UTF8
try{
 foreach($p in $all){
  $target=Assert-Path $game $p
  if($after[$p]){New-Item -ItemType Directory -Force (Split-Path -Parent $target)|Out-Null;Copy-Item -LiteralPath (Join-Path $PSScriptRoot ('payload/'+$p)) -Destination $target -Force}
  elseif(Test-Path -LiteralPath $target){Remove-Item -LiteralPath $target -Force}
 }
 foreach($p in $all){if((Hash (Join-Path $game $p)) -ne $after[$p]){throw "Installed hash mismatch: $p"}}
 Copy-Item -LiteralPath $pending -Destination $state -Force
 Remove-Item -LiteralPath $pending -Force
}catch{
 $failure=$_
 Restore-Transaction ([pscustomobject]$r)
 Restore-PreviousReceipt ([pscustomobject]$r)
 Remove-Item -LiteralPath $pending -Force
 throw $failure
}
Write-Output "$($r.operation) completed for unified $($m.version). Settings and measurements retained; backup $id"
