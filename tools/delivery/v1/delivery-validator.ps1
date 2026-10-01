# dbce.game-delivery schema v1; reusable package-only validator. Never starts entrypoints.
function Assert-DeliveryManifest {
 [CmdletBinding()]
 param([Parameter(Mandatory)][string]$PackageRoot,[string]$ManifestPath='delivery-manifest.json',[switch]$MetadataOnly)
 $ErrorActionPreference='Stop'
 function Shape($o,[string[]]$required,[string[]]$optional=@()){
  if($null -eq $o -or $o -isnot [pscustomobject]){throw 'Expected JSON object'}
  $names=@($o.PSObject.Properties.Name)
  foreach($k in $required){if($k -cnotin $names){throw "Missing field $k"}}
  foreach($k in $names){if($k -cnotin ($required+$optional)){throw "Unknown field $k"}}
 }
 function Text($v){if($v -isnot [string] -or [string]::IsNullOrWhiteSpace($v)){throw 'Expected nonempty string'}}
 function Bool($v){if($v -isnot [bool]){throw 'Expected JSON boolean'}}
 function List($v){if($v -isnot [array]){throw 'Expected JSON array'}}
 function Strings($v){List $v;foreach($x in $v){Text $x}}
 function Check-Enum($v,[string[]]$allowed){Text $v;if($v -cnotin $allowed){throw "Invalid Check-Enum $v"}}
 function Hex($v,[int]$length){Text $v;if($v -cnotmatch ('^[a-f0-9]{'+$length+'}$')){throw 'Invalid lowercase identity hash'}}
 function Url($v){Text $v;$u=$null;if(-not [Uri]::TryCreate($v,[UriKind]::Absolute,[ref]$u) -or $u.Scheme -ne 'https' -or $u.UserInfo -or -not $u.Host){throw 'Expected safe HTTPS URL'}}
 function Path($v,[switch]$Exists){
  Text $v;if($v -match '[\\:]|^/' -or @($v.Split('/')|Where-Object {$_ -in @('','.','..')}).Count){throw 'Unsafe package path'}
  $base=[IO.Path]::GetFullPath($PackageRoot).TrimEnd('\','/');$p=[IO.Path]::GetFullPath((Join-Path $base $v))
  if(-not $p.StartsWith($base+[IO.Path]::DirectorySeparatorChar,[StringComparison]::OrdinalIgnoreCase)){throw 'Escaping package path'}
  $walk=$p;while($walk.Length -ge $base.Length){if((Test-Path -LiteralPath $walk) -and ((Get-Item -LiteralPath $walk -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)){throw 'Linked package path'};$walk=Split-Path -Parent $walk}
  if($Exists -and -not (Test-Path -LiteralPath $p -PathType Leaf)){throw "Missing package path $v"};return $p
 }
 $mp=Path $ManifestPath -Exists
 if(-not ('DbceDeliveryJsonV1' -as [type])){Add-Type -Path (Join-Path $PSScriptRoot 'delivery-parser.cs')}
 $raw=[IO.File]::ReadAllText($mp);[DbceDeliveryJsonV1]::Check($raw);$m=$raw|ConvertFrom-Json
 # Reject obvious private absolute filesystem paths anywhere in public metadata,
 # including escaped JSON strings and extensions. This is not a blanket secret scanner.
 function PrivatePath($o){
  if($o -is [string]){if($o -match '(?i)(?<![a-z0-9])[a-z]:[\\/]|^\\\\'){throw 'Absolute private filesystem path in delivery metadata'}}
  elseif($o -is [pscustomobject]){foreach($p in $o.PSObject.Properties){PrivatePath $p.Value}}
  elseif($o -is [array]){foreach($v in $o){PrivatePath $v}}
 }
 if($m.repository.visibility -eq 'public'){PrivatePath $m}
 Shape $m @('schema','schemaVersion','kind','packageId','gameId','displayName','version','channel','platform','repository','provenance','integrity','loadOwners','features','setup','compatibility','recording') @('extensions')
 if($m.schema -cne 'dbce.game-delivery' -or $m.kind -cne 'package-delivery' -or ($m.schemaVersion -isnot [int] -and $m.schemaVersion -isnot [long]) -or $m.schemaVersion -ne 1){throw 'Unsupported delivery schema'}
 foreach($v in @($m.packageId,$m.gameId)){Text $v;if($v -cnotmatch '^[a-z0-9]+(?:-[a-z0-9]+)*$'){throw 'Invalid stable ID'}}
 Text $m.displayName;Text $m.version
 $semver='^(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)(-((0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*)(\.(0|[1-9][0-9]*|[0-9]*[A-Za-z-][0-9A-Za-z-]*))*))?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$'
 if($m.version -cnotmatch $semver){throw 'Invalid SemVer'};Check-Enum $m.channel @('candidate','prerelease','stable')
 Shape $m.platform @('os','architecture');Check-Enum $m.platform.os @('windows');Check-Enum $m.platform.architecture @('x86','x64')
 Shape $m.repository @('canonicalUrl','visibility','mappingStatus','previousUrls');Url $m.repository.canonicalUrl;Check-Enum $m.repository.visibility @('public','private');Check-Enum $m.repository.mappingStatus @('proposed','configured','verified');List $m.repository.previousUrls;foreach($u in $m.repository.previousUrls){Url $u}
 Shape $m.provenance @('sources','artifacts','dependencies');List $m.provenance.sources;List $m.provenance.artifacts;List $m.provenance.dependencies
 $roles=@();foreach($s in $m.provenance.sources){Shape $s @('role','repositoryUrl','commit','tree','dirty');Text $s.role;if($s.role -in $roles){throw 'Duplicate source role'};$roles+=$s.role;Url $s.repositoryUrl;Hex $s.commit 40;Hex $s.tree 40;Bool $s.dirty}
 foreach($role in @('runtime','installer','packaging')){if($role -notin $roles){throw "Missing source role $role"}}
 $dependencyIds=@();$dependencyFiles=@{}
 foreach($d in $m.provenance.dependencies){Shape $d @('dependencyId','version','status','sourceCommit','files','overrideOf','distribution') @('reason');Text $d.dependencyId;if($d.dependencyId -in ($dependencyIds+$roles)){throw 'Duplicate dependency ID'};$dependencyIds+=$d.dependencyId;Text $d.version;Check-Enum $d.status @('released','unpublished-override');Check-Enum $d.distribution @('included','external-reference');List $d.files
  if($null -eq $d.sourceCommit -or $null -eq $d.overrideOf){Text $d.reason};if($null -ne $d.sourceCommit){Hex $d.sourceCommit 40};if($null -ne $d.overrideOf){Text $d.overrideOf};if($d.status -eq 'unpublished-override' -and $null -eq $d.overrideOf){throw 'Override must identify base'}
  if(($d.distribution -eq 'external-reference' -and $d.files.Count) -or ($d.distribution -eq 'included' -and -not $d.files.Count)){throw 'Dependency distribution mismatch'}
  foreach($f in $d.files){Shape $f @('path','sha256');$null=Path $f.path;Hex $f.sha256 64;if($dependencyFiles.ContainsKey($f.path)){throw 'Duplicate dependency file'};$dependencyFiles[$f.path]=$f.sha256}
 }
 Shape $m.integrity @('manifestPath','format');$ip=Path $m.integrity.manifestPath -Exists;Text $m.integrity.format
 $artifacts=@{};foreach($a in $m.provenance.artifacts){Shape $a @('path','sha256','origin','sourceRoles');$null=Path $a.path;Hex $a.sha256 64;Check-Enum $a.origin @('fresh-build','retained-binary','upstream-binary');Strings $a.sourceRoles;if(-not $a.sourceRoles.Count){throw 'Missing artifact source'};foreach($role in $a.sourceRoles){if($role -notin ($roles+$dependencyIds)){throw 'Unknown artifact source'}};if($artifacts.ContainsKey($a.path)){throw 'Duplicate artifact path'};if($a.path -in @($ManifestPath,$m.integrity.manifestPath)){throw 'Self-referential delivery hash'};$artifacts[$a.path]=$a}
 List $m.loadOwners;$owners=@();foreach($o in $m.loadOwners){Shape $o @('ownerId','mechanism','role');Text $o.ownerId;Text $o.mechanism;Check-Enum $o.role @('game-host','mod-loader','offline-companion');if($o.ownerId -in $owners){throw 'Duplicate owner'};$owners+=$o.ownerId}
 List $m.features;if($m.features.Count -ne 6){throw 'Exactly six features required'};$ids=@()
 foreach($f in $m.features){Shape $f @('featureId','implemented','packaged','acceptance','defaultState','capabilities','limitations') @('freshInstallDefault');Check-Enum $f.featureId @('wheel','ffb','telemetry','triple','recording','playback');if($f.featureId -in $ids){throw 'Duplicate feature'};$ids+=$f.featureId;Bool $f.implemented;Bool $f.packaged;Strings $f.capabilities;Strings $f.limitations;Check-Enum $f.defaultState @('unavailable','off','on','preserve-existing')
  if($f.packaged -and -not $f.implemented){throw 'Unimplemented feature packaged'};if(-not $f.implemented -and $f.capabilities.Count){throw 'Unimplemented capability'};if(($f.packaged -and $f.defaultState -eq 'unavailable') -or (-not $f.packaged -and $f.defaultState -ne 'unavailable')){throw 'Package/default mismatch'}
  if($f.PSObject.Properties.Name -contains 'freshInstallDefault'){Check-Enum $f.freshInstallDefault @('off','on');if($f.defaultState -ne 'preserve-existing'){throw 'Fresh default requires preservation'}}
  Shape $f.acceptance @('status','scope','evidence');Check-Enum $f.acceptance.status @('not-assessed','pending','partial','accepted','not-applicable');Text $f.acceptance.scope;List $f.acceptance.evidence
  if((-not $f.implemented -or -not $f.packaged) -and $f.acceptance.status -ne 'not-applicable'){throw 'Unshipped package acceptance'}
  if($f.acceptance.status -in @('partial','accepted') -and -not $f.acceptance.evidence.Count){throw 'Acceptance without evidence'}
  foreach($e in $f.acceptance.evidence){Shape $e @('reference','identity','scope');Text $e.reference;Text $e.identity;Text $e.scope;if($e.reference -match '^https:'){Url $e.reference}elseif($e.reference -match '^review:[A-Za-z0-9._-]+$'){}else{$null=Path $e.reference -Exists}}
 }
 Shape $m.setup @('deliveryMode','entrypoints','target','operations','recovery','preservation');Check-Enum $m.setup.deliveryMode @('transactional-install','retained-installer','fresh-folder');Shape $m.setup.target @('kind','legacyRootIdentity');Check-Enum $m.setup.target.kind @('game-directory','per-user-app','new-folder');Text $m.setup.target.legacyRootIdentity
 List $m.setup.entrypoints;$entrypoints=@();foreach($e in $m.setup.entrypoints){Shape $e @('path','purpose','argumentStyle','startsGame','opensDevices');$null=Path $e.path -Exists;if($e.path -in $entrypoints){throw 'Duplicate entrypoint'};$entrypoints+=$e.path;Check-Enum $e.purpose @('primary','legacy-compatible','recovery');Text $e.argumentStyle;Bool $e.startsGame;Bool $e.opensDevices}
 Shape $m.setup.operations @('check','install','update','uninstall','rollback');foreach($n in @('check','install','update','uninstall','rollback')){$o=$m.setup.operations.$n;Shape $o @('support','entrypoint','arguments','notes');Check-Enum $o.support @('automated','manual','unsupported');Strings $o.arguments;Text $o.notes;if($o.support -eq 'automated'){if($o.entrypoint -notin $entrypoints){throw 'Operation entrypoint not declared'}}elseif($null -ne $o.entrypoint){throw 'Manual/unsupported entrypoint must be null'}}
 Shape $m.setup.recovery @('ownershipModel','changedOrUnownedPayload','caughtFailure','interruption','backupVerification');Check-Enum $m.setup.recovery.ownershipModel @('receipt-hashes','recognized-legacy-hashes','runtime-backup','none-fresh-folder');Check-Enum $m.setup.recovery.changedOrUnownedPayload @('refuse','manual-review','not-applicable');foreach($v in @($m.setup.recovery.caughtFailure,$m.setup.recovery.interruption)){Check-Enum $v @('automatic-verified-rollback','explicit-verified-rollback','manual-backup-recovery','not-applicable')};Check-Enum $m.setup.recovery.backupVerification @('hashes','manual','not-applicable')
 Shape $m.setup.preservation @('settings','captures','otherMods','sharedLoader','explicitExceptions');Check-Enum $m.setup.preservation.settings @('byte-preserved-existing');Check-Enum $m.setup.preservation.captures @('retained');Check-Enum $m.setup.preservation.otherMods @('retained');Check-Enum $m.setup.preservation.sharedLoader @('retained-by-default');Strings $m.setup.preservation.explicitExceptions
 Shape $m.compatibility @('legacyDescriptors','retainedIdentities','settingsPolicy','capturePolicy','optimizerDiscovery') @('notes');List $m.compatibility.legacyDescriptors;foreach($v in $m.compatibility.legacyDescriptors){$null=Path $v -Exists};List $m.compatibility.retainedIdentities;foreach($v in $m.compatibility.retainedIdentities){Shape $v @('kind','value');Text $v.kind;Text $v.value};Check-Enum $m.compatibility.settingsPolicy @('preserve-existing');Check-Enum $m.compatibility.capturePolicy @('preserve-existing');Check-Enum $m.compatibility.optimizerDiscovery @('not-adopted');if($m.compatibility.PSObject.Properties.Name -contains 'notes'){Strings $m.compatibility.notes}
 Shape $m.recording @('captureKinds','playbackKinds','formatIds','bounded','completionPolicy','identityPolicy','privacy','physicalOutput') @('limits','notes');List $m.recording.captureKinds;foreach($v in $m.recording.captureKinds){Check-Enum $v @('input','telemetry','configuration','state','force-requests')};List $m.recording.playbackKinds;foreach($v in $m.recording.playbackKinds){Check-Enum $v @('inspection','offline-force-analysis','game-input-replay')};List $m.recording.formatIds;foreach($v in $m.recording.formatIds){Shape $v @('id','version','adapterId');Text $v.id;Text $v.version;Text $v.adapterId};Bool $m.recording.bounded;Text $m.recording.completionPolicy;Text $m.recording.identityPolicy;Check-Enum $m.recording.privacy @('local-private-by-default');Check-Enum $m.recording.physicalOutput @('forbidden-during-offline-playback');foreach($n in @('limits','notes')){if($m.recording.PSObject.Properties.Name -contains $n){Strings $m.recording.$n}}
 if($m.PSObject.Properties.Name -contains 'extensions'){if($m.extensions -isnot [pscustomobject]){throw 'Extensions object required'};foreach($n in $m.extensions.PSObject.Properties.Name){if($n -notmatch '^[a-z][a-z0-9-]*(\.[a-z0-9-]+)+$'){throw 'Extensions must be namespaced'}}}
 if(-not $MetadataOnly){
  # Current v1 implementation supports this integrity adapter only; other owners must add reviewed adapters.
  if($m.integrity.format -ne 'dbce.art-unified-package.v1'){throw 'Unsupported integrity adapter'}
  $iraw=[IO.File]::ReadAllText($ip);[DbceDeliveryJsonV1]::Check($iraw);$inventory=$iraw|ConvertFrom-Json
  if($inventory.packageId -ne $m.packageId -or $inventory.version -ne $m.version){throw 'Conflicting package descriptor identity'}
  $paths=@($inventory.files.PSObject.Properties.Name);if($ManifestPath -notin $paths){throw 'Delivery descriptor omitted from integrity'}
  foreach($p in $paths){$full=Path $p -Exists;Hex ($inventory.files.$p.ToLowerInvariant()) 64;if((Get-FileHash -LiteralPath $full).Hash.ToLowerInvariant() -ne $inventory.files.$p.ToLowerInvariant()){throw "Integrity mismatch $p"}}
  $actual=@(Get-ChildItem -LiteralPath $PackageRoot -Recurse -Force|Where-Object {-not $_.PSIsContainer}|ForEach-Object {$_.FullName.Substring(([IO.Path]::GetFullPath($PackageRoot).TrimEnd('\','/')).Length+1).Replace('\','/')})
  if(@(Compare-Object ($paths+$m.integrity.manifestPath|Sort-Object) ($actual|Sort-Object)).Count){throw 'Uninventoried package bytes'}
  $binaries=@($paths|Where-Object {$_ -match '\.(dll|exe)$'});if(@(Compare-Object ($binaries|Sort-Object) (@($artifacts.Keys)|Sort-Object)).Count){throw 'Executable artifact inventory incomplete'}
  foreach($a in $artifacts.Values){if($a.sha256 -ne $inventory.files.($a.path).ToLowerInvariant()){throw 'Artifact hash conflict'}}
  foreach($p in $dependencyFiles.Keys){if($p -notin $paths -or $dependencyFiles[$p] -ne $inventory.files.$p.ToLowerInvariant()){throw 'Dependency hash conflict'}}
 }
 return $m
}
