# Synthetic hash fixtures only: no executable/game/UMM inputs and no release packaging.
[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $root 'tools/unified/verify.ps1')
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $out){throw 'Fixture output already exists; preserve prior evidence'}
New-Item -ItemType Directory -Path $out|Out-Null
function Write-Fixture([string]$Directory,[string]$Relative,[string]$Text){
 $path=Join-Path $Directory $Relative
 New-Item -ItemType Directory -Force (Split-Path -Parent $path)|Out-Null
 [IO.File]::WriteAllText($path,$Text,[Text.UTF8Encoding]::new($false))
}
$stage=Join-Path $out 'synthetic package'
foreach($p in $OwnedPaths){Write-Fixture $stage ('payload/'+$p) ('SYNTHETIC CI BYTES - NOT EXECUTABLE: '+$p)}
$release=Get-Content -LiteralPath (Join-Path $root 'game-release.json') -Raw|ConvertFrom-Json
$version=$release.unifiedCandidate.version
$info=Get-Content -LiteralPath (Join-Path $root 'components/wheel/src/ArtOfSimRally.Mod/Info.json') -Raw|ConvertFrom-Json
$info.Version=($version -split '-')[0];$info.EntryMethod='ArtOfSimRally.Mod.UnifiedEntry.Load'
Write-Fixture $stage 'payload/Mods/ArtOfSimRally/Info.json' ($info|ConvertTo-Json)
$bridge=[ordered]@{Id='DbceTripleScreenArtOfRally';Version='0.3.12';AssemblyName='';EntryMethod='';DisplayName='SYNTHETIC CI compatibility metadata'}
Write-Fixture $stage 'payload/Mods/DbceTripleScreenArtOfRally/Info.json' ($bridge|ConvertTo-Json)
Copy-Item -LiteralPath (Join-Path $root 'components/triple/adapter-manifest.json') -Destination (Join-Path $stage 'payload/Mods/DbceTripleScreenArtOfRally/manifest.json')
Write-Fixture $stage 'payload/Mods/ArtOfSimRally/build.json' ([ordered]@{fixtureOnly=$true;release=$version;modVersion='0.2.7'}|ConvertTo-Json)
Write-Fixture $stage 'payload/Mods/ArtOfSimRally/features.json' '{"fixtureOnly":true}'
foreach($name in $PackageExtras | Where-Object {$_ -ne 'delivery-manifest.json'}){
 $source=if($name -in @('delivery-validator.ps1','delivery-parser.cs')){Join-Path $root ('tools/delivery/v1/'+$name)}elseif($name -eq 'LICENSE'){Join-Path $root 'LICENSE'}else{Join-Path $root ('tools/unified/'+$name)}
 Copy-Item -LiteralPath $source -Destination (Join-Path $stage $name)
}
Write-Fixture $stage 'README.txt' 'SYNTHETIC CI FIXTURE. NOT A MOD. NEVER INSTALL IN A REAL GAME.'
$legacyDirectories=@();$profiles=@()
foreach($component in @('wheel','triple')){
 $mod=if($component -eq 'wheel'){'ArtOfSimRally'}else{'DbceTripleScreenArtOfRally'}
 $dir=Join-Path $out ('synthetic legacy '+$component);$legacyDirectories+=$dir
 $names=if($component -eq 'wheel'){@('ArtOfSimRally.Mod.dll','Dbce.Wheel.Telemetry.dll','Dbce.Wheel.Ffb.dll','UnityForceFeedback.dll','Info.json','build.json')}else{@('ArtOfRally.TripleScreen.Mod.dll','Dbce.TripleScreen.Core.dll','Dbce.TripleScreen.Protocol.dll','Info.json','manifest.json')}
 foreach($name in $names){Write-Fixture $dir ($mod+'/'+$name) ('SYNTHETIC LEGACY '+$component+' - NOT EXECUTABLE: '+$name)}
 $oldInfo=Get-Content -LiteralPath (Join-Path $root ('components/'+$component+'/src/'+$(if($component -eq 'wheel'){'ArtOfSimRally.Mod'}else{'ArtOfRally.TripleScreen.Mod'})+'/Info.json')) -Raw|ConvertFrom-Json
 if($component -eq 'triple'){$oldInfo.Version='0.3.11'}
 Write-Fixture $dir ($mod+'/Info.json') ($oldInfo|ConvertTo-Json)
 $files=[ordered]@{}
 foreach($name in $names){$files['Mods/'+$mod+'/'+$name]=Hash (Join-Path $dir ($mod+'/'+$name))}
 if($component -eq 'wheel'){$files['artofrally_Data/Plugins/x86_64/UnityForceFeedback.dll']=$files['Mods/ArtOfSimRally/UnityForceFeedback.dll']}
 $profiles += [ordered]@{component=$component;version=$oldInfo.Version;fixtureOnly=$true;files=$files}
}
$revision=(& git -c "safe.directory=$root" -C $root rev-parse HEAD).Trim()
$tree=(& git -c "safe.directory=$root" -C $root rev-parse ($revision+'^{tree}')).Trim()
& (Join-Path $root 'tools/unified/New-DeliveryManifest.ps1') -PackageRoot $stage -Version $version -SourceCommit $revision -SourceTree $tree -Dirty $true -FixtureOnly
$files=[ordered]@{}
foreach($p in @($OwnedPaths|ForEach-Object {'payload/'+$_})+$PackageExtras){$files[$p]=Hash (Join-Path $stage $p)}
[ordered]@{schema=1;packageId='dbce-mods-art-of-rally';version=$version;fixtureOnly=$true;files=$files;legacyProfiles=$profiles}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $stage 'package-manifest.json') -Encoding UTF8
$null=Assert-UnifiedPackage $stage
[pscustomobject]@{PackageDirectory=$stage;LegacyPackageDirectories=$legacyDirectories;FixtureOnly=$true}
