<# Coordinator for existing component installers. Never launches games/devices.
   All selected packages are preflighted before any installer runs. Component
   installers preserve settings but overwrite payloads without automatic backup
   or restoration; this is not an atomic bundle. #>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$GameDir,
 [Parameter(Mandatory)][string]$WheelPackage,
 [Parameter(Mandatory)][string]$TriplePackage,
 [ValidateSet('wheel','triple')][string[]]$Components=@('wheel','triple'),
 [switch]$DryRun,[switch]$Uninstall)
$ErrorActionPreference='Stop'
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$release=Get-Content -LiteralPath (Join-Path $root 'game-release.json') -Raw | ConvertFrom-Json
if($release.schema -ne 1 -or $release.game -ne 'art-of-rally'){throw 'Unsupported game-release manifest'}
$game=[IO.Path]::GetFullPath($GameDir)
if(-not (Test-Path -LiteralPath (Join-Path $game 'artofrally.exe') -PathType Leaf)){throw 'Select the folder containing artofrally.exe'}
if((Get-Item -LiteralPath $game).Attributes -band [IO.FileAttributes]::ReparsePoint){throw 'Linked game folder refused'}
if(Get-Process artofrally -ErrorAction SilentlyContinue){throw 'Close art of rally normally before setup'}
$selected=@('wheel','triple' | Where-Object {$_ -in $Components})
if(-not $selected.Count){throw 'Select at least one component'}
$packages=@{wheel=[IO.Path]::GetFullPath($WheelPackage);triple=[IO.Path]::GetFullPath($TriplePackage)}
foreach($component in $selected){
 $spec=$release.components.$component
 $manifestPath=Join-Path $packages[$component] $spec.packageManifest
 if((Get-FileHash -LiteralPath $manifestPath -Algorithm SHA256).Hash -ne $spec.packageManifestSha256){throw "$component package manifest differs from selected release"}
 # Verify through our audited source, not a script supplied by the caller's package.
 $validator=if($component -eq 'wheel'){Join-Path $root 'tools/installer/verify.ps1'}else{Join-Path $root 'components/triple/tools/installer/verify.ps1'}
 . $validator
 $verified=Assert-Payload $packages[$component]
 if($verified.release -ne $spec.version){throw "$component version mismatch"}
}
$receipt=[ordered]@{schema=1;game='art-of-rally';gamePath=$game;action=$(if($Uninstall){'uninstall'}else{'install'});dryRun=[bool]$DryRun;status='PREFLIGHT PASS';components=@();limits='No automatic backup or restoration; failed copies may leave partial payloads. Preserve settings and manually reinstall a retained prior package or uninstall each component. No game/device acceptance.'}
foreach($component in $selected){
 if($DryRun){$receipt.components+=@{name=$component;version=$release.components.$component.version;status='NOT RUN';reason='Dry-run: existing installer not invoked'};continue}
 $shell=Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
 $args=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $packages[$component] 'install.ps1'),'-GameDir',$game)
 if($Uninstall){$args+='-Uninstall'}
 & $shell @args
 $code=$LASTEXITCODE
 $receipt.components+=@{name=$component;version=$release.components.$component.version;status=$(if($code -eq 0){'PASS'}else{'FAIL'});exitCode=$code}
 if($code -ne 0){
  foreach($pending in $selected){if($pending -notin @($receipt.components|ForEach-Object name)){$receipt.components+=@{name=$pending;version=$release.components.$pending.version;status='NOT RUN';reason='Stopped after earlier installer failure'}}}
  $receipt.status='PARTIAL OR FAILED';$receipt|ConvertTo-Json -Depth 6
  throw "$component installer failed and may have partially copied files; prior successful components remain installed. No automatic backup or restoration. Retain settings and manually reinstall a known package or use component uninstall."
 }
}
if(-not $DryRun){$receipt.status='PASS'}
$receipt|ConvertTo-Json -Depth 6
