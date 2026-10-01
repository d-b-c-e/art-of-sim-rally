#requires -Version 7.0
[CmdletBinding()]
param([string]$RepositoryRoot=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..')))
$ErrorActionPreference='Stop'
function Assert-Documentation([hashtable]$Documents){
 foreach($path in @('README.md','docs/GAME-SETUP.md','docs/RELEASE-STRUCTURE.md')){
  $text=$Documents[$path]
  if(-not $text){throw "Missing authoritative documentation: $path"}
  $parts=$text -split '(?m)^## Legacy component routes \(historical support only\)',2
  if($parts.Count -ne 2){throw "Legacy guidance must be explicitly segregated: $path"}
  $current=$parts[0] -replace '\s+',' '
  if($current -notmatch 'UNIFIED-MIGRATION\.md' -or $current -notmatch 'unreleased') {throw "Missing unified candidate navigation/qualification: $path"}
  if($current -notmatch 'one (installable )?package' -or $current -notmatch 'version'){throw "Missing one-product policy: $path"}
  if($current -match 'Keep wheel and triple versions/pins independent|Future bundles may use|New tags can use|For a normal single-component install|optional `-Components|Install-Game\.ps1|Build and package each component from its own folder') {throw "Superseded recommendation returned: $path"}
  if($text -notmatch 'optimizer' -or $text -notmatch 'settings' -or $text -notmatch '0\.3\.12'){throw "Missing compatibility/installed provenance limits: $path"}
 }
 foreach($target in @('docs/GAME-SETUP.md','docs/RELEASE-STRUCTURE.md')){
  if($Documents['README.md'] -notmatch ([regex]::Escape(']('+ $target + ')'))){throw "Missing authoritative README navigation: $target"}
 }
 if($Documents['docs/GAME-SETUP.md'] -notmatch 'tools/unified/package\.ps1' -or $Documents['docs/GAME-SETUP.md'] -notmatch 'Install\.bat -GameDir' -or $Documents['docs/GAME-SETUP.md'] -notmatch '-DryRun'){throw 'Unified setup route missing'}
 if($Documents['docs/RELEASE-STRUCTURE.md'] -notmatch 'dbce-mods-art-of-rally-0\.4\.0-rc\.2\.zip'){throw 'Unified package naming missing'}
}
$documents=@{}
foreach($path in @('README.md','docs/GAME-SETUP.md','docs/RELEASE-STRUCTURE.md')){$documents[$path]=Get-Content -LiteralPath (Join-Path $RepositoryRoot $path) -Raw}
Assert-Documentation $documents
# Exercise the regression guard against the actual superseded policies and broken navigation.
$mutations=@(
 @{path='docs/RELEASE-STRUCTURE.md';text='Keep wheel and triple versions/pins independent.'},
 @{path='docs/RELEASE-STRUCTURE.md';text='New tags can use wheel/vX.Y.Z, triple/vX.Y.Z, bundle/vX.Y.Z.'},
 @{path='docs/GAME-SETUP.md';text='For a normal single-component install, use the existing component ZIP.'},
 @{path='docs/GAME-SETUP.md';text='Install-Game.ps1 -Components wheel'},
 @{path='README.md';text='Build and package each component from its own folder.'}
)
$rejections=0
foreach($mutation in $mutations){
 $copy=$documents.Clone();$copy[$mutation.path]=$copy[$mutation.path].Replace('## Legacy component routes',$mutation.text+"`n`n## Legacy component routes")
 $rejected=$false;try{Assert-Documentation $copy}catch{$rejected=$true}
 if(-not $rejected){throw "Policy mutation escaped guard: $($mutation.text)"};$rejections++
}
$copy=$documents.Clone();$copy['README.md']=$copy['README.md'].Replace('](docs/GAME-SETUP.md)','](docs/OLD-SETUP.md)')
$rejected=$false;try{Assert-Documentation $copy}catch{$rejected=$true}
if(-not $rejected){throw 'Broken authoritative navigation escaped guard'};$rejections++
[ordered]@{status='PASS';documents=$documents.Count;rejectedRegressions=$rejections}|ConvertTo-Json -Compress
