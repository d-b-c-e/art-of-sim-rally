$OwnedPaths = @(
 'Mods/ArtOfSimRally/ArtOfSimRally.Mod.dll',
 'Mods/ArtOfSimRally/ArtOfRally.TripleScreen.Mod.dll',
 'Mods/ArtOfSimRally/Dbce.Wheel.Ffb.dll',
 'Mods/ArtOfSimRally/Dbce.Wheel.Telemetry.dll',
 'Mods/ArtOfSimRally/Dbce.TripleScreen.Core.dll',
 'Mods/ArtOfSimRally/Dbce.TripleScreen.Protocol.dll',
 'Mods/ArtOfSimRally/UnityForceFeedback.dll',
 'Mods/ArtOfSimRally/Info.json',
 'Mods/ArtOfSimRally/build.json',
 'Mods/ArtOfSimRally/features.json',
 'Mods/DbceTripleScreenArtOfRally/Info.json',
 'Mods/DbceTripleScreenArtOfRally/manifest.json',
 'artofrally_Data/Plugins/x86_64/UnityForceFeedback.dll'
)
$LegacyPaths = @(
 'Mods/DbceTripleScreenArtOfRally/ArtOfRally.TripleScreen.Mod.dll',
 'Mods/DbceTripleScreenArtOfRally/Dbce.TripleScreen.Core.dll',
 'Mods/DbceTripleScreenArtOfRally/Dbce.TripleScreen.Protocol.dll'
)
$PackageExtras = @('Install.bat','Uninstall.bat','Rollback.bat','install.ps1','verify.ps1','README.txt','LICENSE')
function Assert-Path([string]$Root,[string]$Relative) {
 if ($Relative -notmatch '^[A-Za-z0-9_.-]+(/[A-Za-z0-9_.-]+)+$' -or $Relative.Split('/') -contains '..') { throw 'Unsafe relative path' }
 $base=[IO.Path]::GetFullPath($Root).TrimEnd('\','/')
 $target=[IO.Path]::GetFullPath((Join-Path $base $Relative))
 if (-not $target.StartsWith($base+'\',[StringComparison]::OrdinalIgnoreCase)) { throw 'Path escapes root' }
 if (Test-Path -LiteralPath $target -PathType Container) { throw 'Expected a file target, found a directory' }
 $walk=$target
 while ($walk.Length -ge $base.Length) {
  if ((Test-Path -LiteralPath $walk) -and ((Get-Item -LiteralPath $walk -Force).Attributes -band [IO.FileAttributes]::ReparsePoint)) { throw 'Linked path refused' }
  $walk=Split-Path -Parent $walk
 }
 return $target
}
function Hash([string]$Path) { if(Test-Path -LiteralPath $Path -PathType Leaf){return (Get-FileHash -LiteralPath $Path -Algorithm SHA256).Hash}; return $null }
function Assert-UnifiedPackage([string]$Root) {
 $m=Get-Content -LiteralPath (Join-Path $Root 'package-manifest.json') -Raw | ConvertFrom-Json
 if($m.schema -ne 1 -or $m.packageId -ne 'dbce-mods-art-of-rally' -or $m.version -notmatch '^\d+\.\d+\.\d+-rc\.[1-9]\d*$'){throw 'Invalid candidate package identity'}
 $expected=@($OwnedPaths | ForEach-Object {'payload/'+$_})+$PackageExtras
 if(@(Compare-Object ($expected|Sort-Object) (@($m.files.PSObject.Properties.Name)|Sort-Object)).Count){throw 'Unexpected package manifest file list'}
 $items=@(Get-ChildItem -LiteralPath $Root -Recurse -Force)
 if(@($items|Where-Object {$_.Attributes -band [IO.FileAttributes]::ReparsePoint}).Count){throw 'Linked package refused'}
 $actual=@($items|Where-Object {-not $_.PSIsContainer}|ForEach-Object {$_.FullName.Substring(([IO.Path]::GetFullPath($Root).TrimEnd('\','/')).Length+1).Replace('\','/')})
 if(@(Compare-Object (($expected+'package-manifest.json')|Sort-Object) ($actual|Sort-Object)).Count){throw 'Unexpected or missing package files'}
 foreach($p in $expected){if((Hash (Join-Path $Root $p)) -ne $m.files.$p){throw "Package hash mismatch: $p"}}
 foreach($profile in $m.legacyProfiles){
  foreach($p in $profile.files.PSObject.Properties.Name){
   if($p -notin ($OwnedPaths+$LegacyPaths) -or $profile.files.$p -notmatch '^[A-Fa-f0-9]{64}$'){throw 'Unsafe legacy inventory'}
  }
 }
 $info=Get-Content -LiteralPath (Join-Path $Root 'payload/Mods/ArtOfSimRally/Info.json') -Raw|ConvertFrom-Json
 $bridge=Get-Content -LiteralPath (Join-Path $Root 'payload/Mods/DbceTripleScreenArtOfRally/Info.json') -Raw|ConvertFrom-Json
 if($info.Id -ne 'ArtOfSimRally' -or $info.EntryMethod -ne 'ArtOfSimRally.Mod.UnifiedEntry.Load' -or $info.Version -ne ($m.version -split '-')[0]){throw 'Invalid load owner'}
 if($bridge.Id -ne 'DbceTripleScreenArtOfRally' -or $bridge.AssemblyName -or $bridge.EntryMethod){throw 'Bridge must have no load entry point'}
 return $m
}
