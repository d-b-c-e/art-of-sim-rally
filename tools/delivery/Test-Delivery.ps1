#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$PackageDirectory,[Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
. (Join-Path $root 'tools/unified/verify.ps1')
. (Join-Path $root 'tools/ci/Assert-ExpectedNativeRejection.ps1')
$null=Assert-UnifiedPackage $PackageDirectory
New-Item -ItemType Directory -Path $OutputDirectory|Out-Null
$cases=@();$index=0
function Test-Reject([string]$Name,[scriptblock]$Mutation,[switch]$LeaveHashStale,[switch]$CheckInstallBoundary){
 $script:index++;$p=Join-Path $OutputDirectory ('case-'+$script:index);Copy-Item -LiteralPath $PackageDirectory -Destination $p -Recurse
 $file=Join-Path $p 'delivery-manifest.json';$d=Get-Content $file -Raw|ConvertFrom-Json
 & $Mutation $d $p
 if(Test-Path -LiteralPath $file){if($Name -notmatch 'duplicate JSON|invalid JSON'){$d|ConvertTo-Json -Depth 25|Set-Content $file -Encoding utf8};if(-not $LeaveHashStale){$m=Get-Content (Join-Path $p 'package-manifest.json') -Raw|ConvertFrom-Json;$m.files.'delivery-manifest.json'=Hash $file;$m|ConvertTo-Json -Depth 15|Set-Content (Join-Path $p 'package-manifest.json') -Encoding utf8}}
 $rejected=$false;$why='';try{$null=Assert-UnifiedPackage $p}catch{$rejected=$true;$why=$_.Exception.Message}
 if(-not $rejected){throw "Fixture incorrectly accepted: $Name"}
 $installerChecks=0
 if($CheckInstallBoundary){
  $game=Join-Path $OutputDirectory ('target-'+$script:index+' [game]')
  New-Item -ItemType Directory -Force (Join-Path $game 'Mods/ArtOfSimRally')|Out-Null
  [IO.File]::WriteAllText((Join-Path $game 'artofrally.exe'),'INERT game sentinel')
  [IO.File]::WriteAllBytes((Join-Path $game 'Mods/ArtOfSimRally/Settings.xml'),[byte[]](0,255,13,10))
  [IO.File]::WriteAllText((Join-Path $game 'private-capture.jsonl'),'INERT private capture sentinel')
  function Target-Snapshot {(@(Get-ChildItem -LiteralPath $game -Recurse -Force|Sort-Object FullName|ForEach-Object {[ordered]@{path=$_.FullName.Substring($game.Length+1);value=$(if($_.PSIsContainer){'<directory>'}else{Hash $_.FullName})}})|ConvertTo-Json -Depth 5 -Compress)}
  $before=Target-Snapshot
  foreach($operation in @('check','install')){
   [string[]]$flags=if($operation -eq 'check'){@('-DryRun')}else{@()}
   $output=& "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe" -NoProfile -NonInteractive -ExecutionPolicy Bypass -File (Join-Path $p 'install.ps1') -GameDir $game @flags 2>&1
   $code=$LASTEXITCODE;$output|Out-File -LiteralPath (Join-Path $OutputDirectory ('preflight-'+$script:index+'-'+$operation+'.log')) -Encoding utf8
   Assert-ExpectedNativeRejection -ExitCode $code -Output $output -ExpectedReason $why -TargetUnchanged ((Target-Snapshot) -ceq $before)
   $installerChecks++
  }
 }
 $script:cases += [ordered]@{name=$Name;status='PASS';rejection=$why;stockPowerShellInstallerRejections=$installerChecks}
}
Test-Reject 'D04 schema version' {param($d,$p)$d.schemaVersion=2}
Test-Reject 'D04 unknown field' {param($d,$p)$d|Add-Member foo 1}
Test-Reject 'D04 boolean string' {param($d,$p)$d.features[0].implemented='true'}
Test-Reject 'D04 duplicate source role' {param($d,$p)$d.provenance.sources[1].role='runtime'}
Test-Reject 'D04 duplicate JSON key' {param($d,$p)$s=Get-Content (Join-Path $p 'delivery-manifest.json') -Raw;$s=$s.Replace('"schema": "dbce.game-delivery"','"schema": "dbce.game-delivery", "schema": "dbce.game-delivery"');Set-Content (Join-Path $p 'delivery-manifest.json') $s}
Test-Reject 'D04 escaped duplicate JSON key' {param($d,$p)$s=Get-Content (Join-Path $p 'delivery-manifest.json') -Raw;$s=$s.Replace('"schema": "dbce.game-delivery"','"schema": "dbce.game-delivery", "\u0073chema": "dbce.game-delivery"');Set-Content (Join-Path $p 'delivery-manifest.json') $s}
Test-Reject 'D04 invalid JSON' {param($d,$p)$s=Get-Content (Join-Path $p 'delivery-manifest.json') -Raw;Set-Content (Join-Path $p 'delivery-manifest.json') ($s+'{}')}
Test-Reject 'D05 missing feature' {param($d,$p)$d.features=@($d.features|Where-Object featureId -ne playback)}
Test-Reject 'D05 alias feature' {param($d,$p)$d.features[0].featureId='wheel-input'}
Test-Reject 'D05 duplicate feature' {param($d,$p)$d.features[1].featureId='wheel'}
Test-Reject 'D06 packaged unimplemented' {param($d,$p)$d.features[0].implemented=$false}
Test-Reject 'D06 unpackaged enabled' {param($d,$p)$d.features[4].defaultState='on'}
Test-Reject 'D06 unimplemented capability' {param($d,$p)$d.features[4].implemented=$false}
Test-Reject 'D06 accepted no evidence' {param($d,$p)$d.features[0].acceptance.status='accepted'}
Test-Reject 'D07 historical UAT promoted' {param($d,$p)$d.features[0].acceptance.status='accepted';$d.features[0].acceptance.evidence=@([pscustomobject]@{reference='review:historical-anthony';identity='installed-wheel0.2.7-triple0.3.12';scope='Historical installed rig only'})}
Test-Reject 'D08 descriptor stale hash' {param($d,$p)$d.displayName='Changed but not inventoried'} -LeaveHashStale
Test-Reject 'D08 identity conflict' {param($d,$p)$d.version='0.5.0-rc.1'}
Test-Reject 'D08 integrity omission' {param($d,$p)$m=Get-Content (Join-Path $p 'package-manifest.json') -Raw|ConvertFrom-Json;$m.files.PSObject.Properties.Remove('delivery-manifest.json');$m|ConvertTo-Json -Depth 15|Set-Content (Join-Path $p 'package-manifest.json')} -LeaveHashStale
Test-Reject 'D09 no fallback when present unsupported' {param($d,$p)$d.schema='other.schema'}
foreach($path in @('../escape','C:/private','//host/share','/absolute','safe//bad','safe/./bad','safe/../bad','safe/bad:stream','safe\bad')){Test-Reject ('D10 path '+$path) {param($d,$p)$d.setup.entrypoints[0].path=$path}.GetNewClosure()}
Test-Reject 'D10 case-insensitive artifact duplicate' {param($d,$p)$a=$d.provenance.artifacts[0].PSObject.Copy();$a.path=$a.path.ToUpperInvariant();$d.provenance.artifacts+=@($a)}
Test-Reject 'D11 malformed binary hash' {param($d,$p)$d.provenance.artifacts[0].sha256='abc'}
Test-Reject 'D11 short commit' {param($d,$p)$d.provenance.sources[0].commit='123abc'}
Test-Reject 'D11 upstream relabeled fresh' {param($d,$p)($d.provenance.artifacts|Where-Object origin -eq upstream-binary|Select-Object -First 1).origin='fresh-build'}
Test-Reject 'D11 dependency mislabeled override' {param($d,$p)$d.provenance.dependencies[0].status='unpublished-override'}
Test-Reject 'D12 private absolute metadata path' {param($d,$p)$d.displayName='C:\Users\owner\private'}
Test-Reject 'D12 private settings added' {param($d,$p)Set-Content (Join-Path $p 'Settings.xml') 'private sentinel'}
Test-Reject 'D12 excluded recorder added' {param($d,$p)Set-Content (Join-Path $p 'Recorder.dll') 'inert excluded sentinel'}
Test-Reject 'D13 missing entrypoint' {param($d,$p)Remove-Item -LiteralPath (Join-Path $p 'Install.bat')}
Test-Reject 'D13 invented check flag' {param($d,$p)$d.setup.operations.check.arguments=@('-GameDir','<game-directory>')}
Test-Reject 'D13 missing legacy doc' {param($d,$p)$d.compatibility.legacyDescriptors+=@('excluded/source/README.md')}
Test-Reject 'D14 configured mapping promoted verified' {param($d,$p)$d.repository.mappingStatus='verified'}
# Exact independently reported mutations are rehashed, so these exercise semantic
# validation rather than relying on an integrity mismatch to reject the lie.
Test-Reject 'Review unsupported-telemetry-capability' {param($d,$p)($d.features|Where-Object featureId -eq telemetry).capabilities=@('game-input-replay')}
Test-Reject 'Review false-source-tree all roles' {param($d,$p)foreach($source in $d.provenance.sources){$source.tree=('a'*40)}}
Test-Reject 'Review unsupported-engine-operation' {param($d,$p)$d.extensions.'dbce.art'.engineOperations.gameInputReplay='supported'}
foreach($id in @('wheel','ffb','telemetry','triple','recording','playback')){Test-Reject ('Art extra unsupported capability '+$id) {param($d,$p)($d.features|Where-Object featureId -eq $id).capabilities+=@('unsupported-capability')}.GetNewClosure()}
Test-Reject 'Art capability case alias' {param($d,$p)($d.features|Where-Object featureId -eq telemetry).capabilities=@('FORZA-UDP')}
Test-Reject 'Art device playback promotion' {param($d,$p)$d.extensions.'dbce.art'.engineOperations.devicePlayback='supported'}
Test-Reject 'Art renderer qualification promotion' {param($d,$p)$d.extensions.'dbce.art'.engineOperations.rendererQualification='accepted'}
Test-Reject 'Art undeclared engine operation' {param($d,$p)$d.extensions.'dbce.art'.engineOperations|Add-Member autoLaunch 'supported'}
Test-Reject 'Art missing engine operations' {param($d,$p)$d.extensions.'dbce.art'.PSObject.Properties.Remove('engineOperations')}
Test-Reject 'Package tree anchor conflicts build' {param($d,$p)$m=Get-Content (Join-Path $p 'package-manifest.json') -Raw|ConvertFrom-Json;$m.sourceTree=('a'*40);$m|ConvertTo-Json -Depth 15|Set-Content (Join-Path $p 'package-manifest.json')}
Test-Reject 'Build tree anchor conflicts package' {param($d,$p)$file=Join-Path $p 'payload/Mods/ArtOfSimRally/build.json';$build=Get-Content $file -Raw|ConvertFrom-Json;$build.sourceTree=('a'*40);$build|ConvertTo-Json|Set-Content $file;$m=Get-Content (Join-Path $p 'package-manifest.json') -Raw|ConvertFrom-Json;$m.files.'payload/Mods/ArtOfSimRally/build.json'=Hash $file;$m|ConvertTo-Json -Depth 15|Set-Content (Join-Path $p 'package-manifest.json')}
Test-Reject 'Missing package tree anchor' {param($d,$p)$m=Get-Content (Join-Path $p 'package-manifest.json') -Raw|ConvertFrom-Json;$m.PSObject.Properties.Remove('sourceTree');$m|ConvertTo-Json -Depth 15|Set-Content (Join-Path $p 'package-manifest.json')}
Test-Reject 'Missing build tree anchor' {param($d,$p)$file=Join-Path $p 'payload/Mods/ArtOfSimRally/build.json';$build=Get-Content $file -Raw|ConvertFrom-Json;$build.PSObject.Properties.Remove('sourceTree');$build|ConvertTo-Json|Set-Content $file;$m=Get-Content (Join-Path $p 'package-manifest.json') -Raw|ConvertFrom-Json;$m.files.'payload/Mods/ArtOfSimRally/build.json'=Hash $file;$m|ConvertTo-Json -Depth 15|Set-Content (Join-Path $p 'package-manifest.json')}
Test-Reject 'Art recording falsely packaged' {param($d,$p)$d.features[4].packaged=$true;$d.features[4].defaultState='off';$d.features[4].acceptance.status='pending'}
Test-Reject 'Art physical playback advertised' {param($d,$p)$d.recording.physicalOutput='allowed'}
Test-Reject 'Art legacy adapter renamed' {param($d,$p)($d.compatibility.retainedIdentities|Where-Object kind -eq adapter).value='new-adapter'}
Test-Reject 'Self referential integrity hash' {param($d,$p)$a=$d.provenance.artifacts[0].PSObject.Copy();$a.path='delivery-manifest.json';$d.provenance.artifacts+=@($a)}
# Rehash changed legacy bytes and matching package labels to reproduce the reviewed
# production bypass. Labels must never exempt semantic mapping or setup preflight.
function Rewrite-PayloadJson($Package,[string]$Relative,[scriptblock]$Edit){
 $file=Join-Path $Package $Relative;$json=Get-Content -LiteralPath $file -Raw|ConvertFrom-Json;& $Edit $json
 $json|ConvertTo-Json -Depth 15|Set-Content -LiteralPath $file -Encoding utf8
 $inventoryFile=Join-Path $Package 'package-manifest.json';$inventory=Get-Content -LiteralPath $inventoryFile -Raw|ConvertFrom-Json;$inventory.files.$Relative=Hash $file;$inventory|ConvertTo-Json -Depth 15|Set-Content -LiteralPath $inventoryFile -Encoding utf8
}
function Match-FixtureLabels($Delivery,$Package,[bool]$Value){
 $Delivery.extensions.'dbce.art'.fixtureOnly=$Value
 $file=Join-Path $Package 'package-manifest.json';$inventory=Get-Content -LiteralPath $file -Raw|ConvertFrom-Json;$inventory|Add-Member -NotePropertyName fixtureOnly -NotePropertyValue $Value -Force;$inventory|ConvertTo-Json -Depth 15|Set-Content -LiteralPath $file -Encoding utf8
}
foreach($label in @($false,$true)){
 Test-Reject ('Fixture label '+$label+' cannot bypass feature identity') {param($d,$p)Match-FixtureLabels $d $p $label;Rewrite-PayloadJson $p 'payload/Mods/ArtOfSimRally/features.json' {param($f)$f.packageId='wrong-package'}} -CheckInstallBoundary
 Test-Reject ('Fixture label '+$label+' cannot bypass feature availability') {param($d,$p)Match-FixtureLabels $d $p $label;Rewrite-PayloadJson $p 'payload/Mods/ArtOfSimRally/features.json' {param($f)($f.features|Where-Object featureId -eq telemetry).available=$false}}
 Test-Reject ('Fixture label '+$label+' cannot bypass adapter identity') {param($d,$p)Match-FixtureLabels $d $p $label;Rewrite-PayloadJson $p 'payload/Mods/ArtOfSimRally/features.json' {param($f)($f.features|Where-Object featureId -eq triple).adapterId='wrong-adapter'}} -CheckInstallBoundary
 Test-Reject ('Fixture label '+$label+' cannot bypass bridge manifest') {param($d,$p)Match-FixtureLabels $d $p $label;Rewrite-PayloadJson $p 'payload/Mods/DbceTripleScreenArtOfRally/manifest.json' {param($m)$m.adapterId='wrong-adapter'}} -CheckInstallBoundary
 Test-Reject ('Fixture label '+$label+' cannot bypass bridge version') {param($d,$p)Match-FixtureLabels $d $p $label;Rewrite-PayloadJson $p 'payload/Mods/DbceTripleScreenArtOfRally/Info.json' {param($m)$m.Version='99.0.0'}} -CheckInstallBoundary
}
Test-Reject 'Legacy telemetry capabilities conflict' {param($d,$p)Rewrite-PayloadJson $p 'payload/Mods/ArtOfSimRally/features.json' {param($f)($f.features|Where-Object featureId -eq telemetry).capabilities=@('game-input-replay')}}
Test-Reject 'Legacy communication request path conflict' {param($d,$p)Rewrite-PayloadJson $p 'payload/Mods/ArtOfSimRally/features.json' {param($f)($f.features|Where-Object featureId -eq triple).communication.requestPath='wrong.json'}}
Test-Reject 'Adapter game mapping conflict' {param($d,$p)Rewrite-PayloadJson $p 'payload/Mods/DbceTripleScreenArtOfRally/manifest.json' {param($m)$m.gameId='other-game'}}
Test-Reject 'Adapter layout contract conflict' {param($d,$p)Rewrite-PayloadJson $p 'payload/Mods/DbceTripleScreenArtOfRally/manifest.json' {param($m)$m.layoutContractVersions.maximum=2}}
Test-Reject 'Fixture label string is not authority' {param($d,$p)$file=Join-Path $p 'package-manifest.json';$m=Get-Content $file -Raw|ConvertFrom-Json;$m|Add-Member -NotePropertyName fixtureOnly -NotePropertyValue 'true' -Force;$m|ConvertTo-Json -Depth 15|Set-Content $file}
# Honest inherited defaults are verified against source, not invented by metadata adoption.
$settings=Get-Content (Join-Path $root 'components/wheel/src/ArtOfSimRally.Mod/Settings.cs') -Raw
foreach($declaration in @('WheelInputEnabled = false','ForceFeedbackEnabled = true','TelemetryEnabled = false')){if($settings -notmatch [regex]::Escape($declaration)){throw 'Actual default changed: update owner-reviewed mapping'}}
$cases += [ordered]@{name='Art defaults source mapping';status='PASS'}
$owned=($OwnedPaths -join '|');if($owned -match 'delivery-manifest|delivery-validator|delivery-parser|delivery-art'){throw 'Delivery metadata entered installed ownership'}
$cases += [ordered]@{name='Delivery package-only ownership boundary';status='PASS'}
$manifest=Get-Content (Join-Path $PackageDirectory 'delivery-manifest.json') -Raw|ConvertFrom-Json
 Test-Reject 'D11 runtime pin changes on real package' {param($d,$p)$d.provenance.sources[0].commit=('a'*40)}
 Test-Reject 'D08 legacy features contradict' {param($d,$p)$file=Join-Path $p 'payload/Mods/ArtOfSimRally/features.json';$f=Get-Content $file -Raw|ConvertFrom-Json;$f.version='99.0.0';$f|ConvertTo-Json -Depth 10|Set-Content $file;$m=Get-Content (Join-Path $p 'package-manifest.json') -Raw|ConvertFrom-Json;$m.files.'payload/Mods/ArtOfSimRally/features.json'=Hash $file;$m|ConvertTo-Json -Depth 15|Set-Content (Join-Path $p 'package-manifest.json')}
$result=[ordered]@{status='PASS';cases=$cases;assertions=$cases.Count;scope='Package validation and inert metadata mutations plus stock PowerShell installer preflight in disposable targets; no game/device execution';notCovered=@('D02 F-Zero fresh-folder owner and D03 OutRun retained runtime repack require their adoption fixtures','D09 recognized legacy packages retain their existing verifiers; new Art delivery verifier does not replace them','D10 live junction creation not repeated here; existing Art installer linked-target fixture retained','S04/S05 exhaustive transaction-boundary injection, S06 concurrent race and mocked running-game predicate are not newly certified by this metadata pilot','R01-R06 runtime recording/signal fixtures belong to external diagnostic owners; no recording/playback shipped')}
$result|ConvertTo-Json -Depth 8|Set-Content (Join-Path $OutputDirectory 'result.json')
Write-Output "Delivery fixtures PASS: $($cases.Count)"
