[CmdletBinding()]
param([string]$Version='0.4.0-rc.4',[Parameter(Mandatory)][string]$OutputDirectory,[Parameter(Mandatory)][string[]]$LegacyPackageDirectories,[string]$GameDir='D:\Program Files (x86)\Steam\steamapps\common\artofrally',[string[]]$BuildProperties=@())
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if($Version -notmatch '^\d+\.\d+\.\d+-rc\.[1-9]\d*$'){throw 'This milestone packages local RC candidates only'}
$stage=Join-Path $OutputDirectory ('stage-'+$Version)
if(Test-Path -LiteralPath $stage){throw 'Keep previous candidate evidence; choose a new output directory'}
. (Join-Path $PSScriptRoot 'verify.ps1')
$revision=(& git -c "safe.directory=$root" -C $root rev-parse HEAD).Trim()
if($LASTEXITCODE){throw 'Cannot identify candidate revision'}
$tree=(& git -c "safe.directory=$root" -C $root rev-parse ($revision+'^{tree}')).Trim()
if($LASTEXITCODE -or $tree -cnotmatch '^[a-f0-9]{40}$'){throw 'Cannot identify candidate source tree'}
$sourceState=if(& git -c "safe.directory=$root" -C $root status --porcelain){'dirty'}else{'clean'}
if($LASTEXITCODE){throw 'Cannot identify candidate source state'}
& dotnet build (Join-Path $root 'components/wheel/src/ArtOfSimRally.Mod/ArtOfSimRally.Mod.csproj') -c Release --nologo -warnaserror -v q "-p:GameDir=$GameDir" "-p:ReleaseLabel=$Version" "-p:SourceRevisionId=$revision" "-p:BuildSourceState=$sourceState" @BuildProperties
if($LASTEXITCODE){throw 'Unified build failed'}
$bin=Join-Path $root 'components/wheel/src/ArtOfSimRally.Mod/bin/Release'
$native=Join-Path $root 'components/wheel/lib/toolkit/native/WheelFfb.dll'
$vendor=Join-Path $root 'components/wheel/lib/toolkit'
foreach($line in Get-Content -LiteralPath (Join-Path $vendor 'MANIFEST.txt')){
 if($line -match '^([A-Fa-f0-9]{64})\s+(.+)$'){if((Hash (Join-Path $vendor $Matches[2])) -ne $Matches[1]){throw 'Toolkit pin mismatch'}}
}
New-Item -ItemType Directory -Force $stage|Out-Null
foreach($p in $OwnedPaths){New-Item -ItemType Directory -Force (Split-Path -Parent (Join-Path $stage ('payload/'+$p)))|Out-Null}
$owner=Join-Path $stage 'payload/Mods/ArtOfSimRally'
foreach($dll in @('ArtOfSimRally.Mod.dll','ArtOfRally.TripleScreen.Mod.dll','Dbce.Wheel.Ffb.dll','Dbce.Wheel.Telemetry.dll','Dbce.TripleScreen.Core.dll','Dbce.TripleScreen.Protocol.dll')){Copy-Item -LiteralPath (Join-Path $bin $dll) -Destination $owner}
Copy-Item -LiteralPath $native -Destination (Join-Path $owner 'UnityForceFeedback.dll')
Copy-Item -LiteralPath $native -Destination (Join-Path $stage 'payload/artofrally_Data/Plugins/x86_64/UnityForceFeedback.dll')
$info=Get-Content -LiteralPath (Join-Path $root 'components/wheel/src/ArtOfSimRally.Mod/Info.json') -Raw|ConvertFrom-Json
$info.DisplayName='DBCE mods for art of rally';$info.Version=($Version -split '-')[0];$info.EntryMethod='ArtOfSimRally.Mod.UnifiedEntry.Load';$info.HomePage='https://github.com/d-b-c-e/dbce-mods-art-of-rally';$info.Repository=$info.HomePage
$info|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $owner 'Info.json') -Encoding UTF8
$bridge=Join-Path $stage 'payload/Mods/DbceTripleScreenArtOfRally'
[ordered]@{Id='DbceTripleScreenArtOfRally';DisplayName='Triple optimizer compatibility metadata';Version='0.3.12';AssemblyName='';EntryMethod='';ManagerVersion='0.27.0'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $bridge 'Info.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $root 'components/triple/adapter-manifest.json') -Destination (Join-Path $bridge 'manifest.json')
[ordered]@{schema=1;release=$Version;modVersion='0.2.7';sourceRevision=$revision;sourceTree=$tree;sourceState=$sourceState;wheelBase='d6ed997ceeaf8de88c50677c7d0d860fe134fd09';tripleBase='f8b0f816cd3bdbe9db3c251f31b3e8d59933d37a'}|ConvertTo-Json|Set-Content -LiteralPath (Join-Path $owner 'build.json') -Encoding UTF8
[ordered]@{schemaVersion=1;packageId='dbce-mods-art-of-rally';gameId='art-of-rally';version=$Version;contractStatus='local-candidate-not-adopted-by-optimizer';features=@(
 [ordered]@{featureId='wheel';available=$true;capabilities=@('wheel-input','force-feedback')},
 [ordered]@{featureId='telemetry';available=$true;capabilities=@('forza-udp')},
 [ordered]@{featureId='triple';available=$true;adapterId='dbce-triple-mod-art-of-rally';adapterVersion='0.3.12';capabilities=@('asymmetric-frustum');communication=[ordered]@{layoutContractVersion=1;userRoot='%LOCALAPPDATA%/DBCE/TripleScreen/games/art-of-rally';requestPath='desired-layout.json';statusPath='status.json';supportedTopologies=@('nvidia-surround','borderless-span','separate-displays')}}
)}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $owner 'features.json') -Encoding UTF8
Copy-Item -LiteralPath (Join-Path $root 'LICENSE') -Destination $stage
foreach($name in @('install.ps1','verify.ps1','README.txt','Install.bat','Uninstall.bat','Rollback.bat')){Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $stage}
foreach($name in @('delivery-validator.ps1','delivery-parser.cs')){Copy-Item -LiteralPath (Join-Path $root ('tools/delivery/v1/'+$name)) -Destination $stage}
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'delivery-art.ps1') -Destination $stage
& (Join-Path $PSScriptRoot 'New-DeliveryManifest.ps1') -PackageRoot $stage -Version $Version -SourceCommit $revision -SourceTree $tree -Dirty ($sourceState -eq 'dirty')
$profiles=@()
foreach($dir in $LegacyPackageDirectories){
 $isWheel=Test-Path -LiteralPath (Join-Path $dir 'ArtOfSimRally/Info.json')
 $mod=if($isWheel){'ArtOfSimRally'}else{'DbceTripleScreenArtOfRally'}
 $manifestName=if($isWheel){'manifest.json'}else{'package-manifest.json'}
 $old=Get-Content -LiteralPath (Join-Path $dir $manifestName) -Raw|ConvertFrom-Json
 if($old.schema -ne 1){throw 'Invalid legacy package manifest'}
 $inventory=[ordered]@{}
 foreach($p in $old.files.PSObject.Properties.Name){
  if((Hash (Join-Path $dir $p)) -ne $old.files.$p){throw "Changed legacy package: $p"}
  if($p.StartsWith($mod+'/')){
   $target='Mods/'+$p
   if($target -notin ($OwnedPaths+$LegacyPaths)){throw 'Unexpected legacy owned file'}
   $inventory[$target]=$old.files.$p
  }
 }
 if($isWheel){$inventory['artofrally_Data/Plugins/x86_64/UnityForceFeedback.dll']=$inventory['Mods/ArtOfSimRally/UnityForceFeedback.dll']}
 if($inventory.Count -ne $(if($isWheel){7}else{5})){throw 'Incomplete legacy ownership inventory'}
 $profiles += [ordered]@{component=$(if($isWheel){'wheel'}else{'triple'});version=$old.release;packageManifestSha256=(Hash (Join-Path $dir $manifestName));files=$inventory}
}
$files=[ordered]@{}
foreach($p in @($OwnedPaths|ForEach-Object {'payload/'+$_})+$PackageExtras){$files[$p]=Hash (Join-Path $stage $p)}
[ordered]@{schema=1;packageId='dbce-mods-art-of-rally';version=$Version;sourceRevision=$revision;sourceTree=$tree;sourceState=$sourceState;files=$files;legacyProfiles=$profiles}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $stage 'package-manifest.json') -Encoding UTF8
$null=Assert-UnifiedPackage $stage
$zip=Join-Path $OutputDirectory ('dbce-mods-art-of-rally-'+$Version+'.zip')
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
(Hash $zip)|Set-Content -LiteralPath ($zip+'.sha256') -Encoding ASCII
Write-Output $stage
