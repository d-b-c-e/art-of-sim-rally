# Art-specific truthfulness mapping; common validator remains game independent.
function Assert-ArtDelivery($Root,$Inventory,$Delivery){
 $d=$Delivery
 if($d.packageId -ne 'dbce-mods-art-of-rally' -or $d.gameId -ne 'art-of-rally' -or $d.channel -ne 'candidate' -or $d.repository.canonicalUrl -ne 'https://github.com/d-b-c-e/dbce-mods-art-of-rally' -or $d.repository.mappingStatus -ne 'configured'){throw 'Art package/repository mapping conflict'}
 if($d.loadOwners.Count -ne 1 -or $d.loadOwners[0].ownerId -ne 'ArtOfSimRally' -or $d.loadOwners[0].role -ne 'mod-loader'){throw 'Art must have one load owner'}
 $fixture=[bool]$Inventory.fixtureOnly
 if($d.extensions.'dbce.art'.fixtureOnly -isnot [bool] -or $d.extensions.'dbce.art'.fixtureOnly -ne $fixture){throw 'Fixture designation conflict'}
 $build=Get-Content -LiteralPath (Join-Path $Root 'payload/Mods/ArtOfSimRally/build.json') -Raw|ConvertFrom-Json
 if(-not $fixture){foreach($source in $d.provenance.sources){if($source.commit -ne $Inventory.sourceRevision -or $source.commit -ne $build.sourceRevision -or $source.dirty -ne ($Inventory.sourceState -eq 'dirty')){throw 'Build/source provenance conflict'}}}
 foreach($f in $d.features){
  $shipping=$f.featureId -in @('wheel','ffb','telemetry','triple')
  if(-not $f.implemented -or $f.packaged -ne $shipping){throw 'Art feature distribution conflict'}
  if($shipping){if($f.acceptance.status -ne 'pending' -or $f.defaultState -ne 'preserve-existing' -or $f.freshInstallDefault -ne $(if($f.featureId -eq 'ffb'){'on'}else{'off'})){throw 'Candidate acceptance/default promotion'};if($f.acceptance.evidence.Count){throw 'No exact unified candidate UAT exists'}}
  elseif($f.acceptance.status -ne 'not-applicable' -or $f.defaultState -ne 'unavailable'){throw 'Excluded diagnostics promoted'}
 }
 if($d.recording.captureKinds.Count -or $d.recording.playbackKinds.Count -or $d.recording.formatIds.Count -or $d.recording.bounded){throw 'Excluded recording/replay advertised as shipped'}
 if(($d.features|Where-Object featureId -eq playback).capabilities -notcontains 'offline-force-analysis'){throw 'Replay capability classification changed'}
 if($d.setup.deliveryMode -ne 'transactional-install' -or $d.setup.recovery.ownershipModel -ne 'receipt-hashes' -or $d.setup.recovery.changedOrUnownedPayload -ne 'refuse' -or $d.setup.recovery.caughtFailure -ne 'automatic-verified-rollback' -or $d.setup.recovery.interruption -ne 'explicit-verified-rollback' -or $d.setup.recovery.backupVerification -ne 'hashes'){throw 'Art recovery declaration conflict'}
 foreach($e in $d.setup.entrypoints){if($e.startsGame -or $e.opensDevices){throw 'Art setup side-effect declaration conflict'}}
 $expected=@{check=@('Install.bat','-GameDir','<game-directory>','-DryRun');install=@('Install.bat','-GameDir','<game-directory>');update=@('Install.bat','-GameDir','<game-directory>');uninstall=@('Uninstall.bat','-GameDir','<game-directory>');rollback=@('Rollback.bat','-GameDir','<game-directory>')}
 foreach($n in $expected.Keys){$o=$d.setup.operations.$n;if($o.support -ne 'automated' -or $o.entrypoint -ne $expected[$n][0] -or ($o.arguments -join '|') -cne ($expected[$n][1..($expected[$n].Count-1)] -join '|')){throw "Invented Art CLI operation $n"}}
 foreach($n in @('loader|ArtOfSimRally','adapter|dbce-triple-mod-art-of-rally','config|Mods/ArtOfSimRally/Settings.xml','config|Mods/DbceTripleScreenArtOfRally/Settings.xml','receipt|.dbce-art-unified/receipt.json')){if($n -notin @($d.compatibility.retainedIdentities|ForEach-Object {$_.kind+'|'+$_.value})){throw 'Legacy identity lost'}}
 if(-not $fixture){
  $features=Get-Content -LiteralPath (Join-Path $Root 'payload/Mods/ArtOfSimRally/features.json') -Raw|ConvertFrom-Json
  if($features.packageId -ne $d.packageId -or $features.gameId -ne $d.gameId -or $features.version -ne $d.version){throw 'Legacy feature identity mismatch'}
  foreach($id in @('wheel','telemetry','triple')){$f=@($features.features|Where-Object featureId -eq $id);if($f.Count -ne 1 -or $f[0].available -isnot [bool] -or -not $f[0].available){throw 'Legacy available mapping conflict'}}
  $triple=$features.features|Where-Object featureId -eq triple
  if($triple.adapterId -ne 'dbce-triple-mod-art-of-rally' -or $triple.adapterVersion -ne '0.3.12'){throw 'Optimizer bridge changed'}
 }
 $dep=@($d.provenance.dependencies|Where-Object dependencyId -eq 'dbce-wheel-mod-toolkit')
 if($dep.Count -ne 1 -or $dep[0].status -ne 'released' -or $dep[0].version -ne '0.15.0' -or $dep[0].files.Count -ne 4){throw 'Toolkit dependency declaration conflict'}
 foreach($a in $d.provenance.artifacts){$upstream=([IO.Path]::GetFileName($a.path) -in @('Dbce.Wheel.Ffb.dll','Dbce.Wheel.Telemetry.dll','UnityForceFeedback.dll'));if($a.origin -ne $(if($upstream){'upstream-binary'}else{'fresh-build'}) -or ($a.sourceRoles -join '|') -ne $(if($upstream){'dbce-wheel-mod-toolkit'}else{'runtime'})){throw 'Artifact origin relabeled'}}
 return $d
}
