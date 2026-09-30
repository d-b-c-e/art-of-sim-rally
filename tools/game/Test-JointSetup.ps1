[CmdletBinding()]
param([Parameter(Mandatory)][string]$WheelPackage,[Parameter(Mandatory)][string]$TriplePackage,[Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Use a new output directory'}
$out=New-Item -ItemType Directory -Path $OutputDirectory
$game=Join-Path $out.FullName 'fake game [joint] with spaces'
$wheel=Join-Path $game 'Mods/ArtOfSimRally';$triple=Join-Path $game 'Mods/DbceTripleScreenArtOfRally'
$umm=Join-Path $game 'artofrally_Data/Managed/UnityModManager'
New-Item -ItemType Directory -Path $wheel,$triple,$umm -Force|Out-Null
Set-Content -LiteralPath (Join-Path $game 'artofrally.exe') 'fixture, never executed'
Set-Content -LiteralPath (Join-Path $umm 'UnityModManager.dll') 'fixture'
Set-Content -LiteralPath (Join-Path $wheel 'Settings.xml') 'owner wheel settings'
Set-Content -LiteralPath (Join-Path $triple 'Settings.xml') 'owner triple settings'
Set-Content -LiteralPath (Join-Path $triple 'desired-layout.json') '{"owner":"preserved"}'
$script:checks=0;$commands=@()
function Check([bool]$ok,[string]$why){if(-not $ok){throw $why};$script:checks++}
function Tree(){@(Get-ChildItem -LiteralPath $game -File -Recurse|ForEach-Object { $_.FullName+' '+(Get-FileHash -LiteralPath $_.FullName).Hash }) -join "`n"}
function Run([string]$name,[string[]]$options=@(),[bool]$fails=$false,[string]$tripleFrom=$TriplePackage){
 $shell=Join-Path $env:SystemRoot 'System32/WindowsPowerShell/v1.0/powershell.exe'
 $args=@('-NoProfile','-ExecutionPolicy','Bypass','-File',(Join-Path $PSScriptRoot 'Install-Game.ps1'),'-GameDir',$game,'-WheelPackage',$WheelPackage,'-TriplePackage',$tripleFrom)+$options
 $script:commands+=@{name=$name;executable=$shell;arguments=$args}
 & $shell @args *> (Join-Path $out.FullName "$name.log")
 $code=$LASTEXITCODE
 Check (($code -ne 0) -eq $fails) "$name unexpected exit $code"
}
$before=Tree
Run 'dry-run' @('-DryRun')
Check ((Tree) -eq $before) 'Dry-run changed fake game'
Run 'install-both'
Check (Test-Path -LiteralPath (Join-Path $wheel 'ArtOfSimRally.Mod.dll')) 'Wheel not installed'
Check (Test-Path -LiteralPath (Join-Path $triple 'ArtOfRally.TripleScreen.Mod.dll')) 'Triple not installed'
foreach($path in @((Join-Path $wheel 'Settings.xml'),(Join-Path $triple 'Settings.xml'),(Join-Path $triple 'desired-layout.json'))){Check ((Get-Content -LiteralPath $path -Raw) -match 'owner') 'Owner settings changed'}
$payload=@(Get-FileHash -LiteralPath (Join-Path $wheel 'ArtOfSimRally.Mod.dll'),(Join-Path $triple 'ArtOfRally.TripleScreen.Mod.dll')|ForEach-Object Hash) -join ','
Run 'repeat-install'
Check ((@(Get-FileHash -LiteralPath (Join-Path $wheel 'ArtOfSimRally.Mod.dll'),(Join-Path $triple 'ArtOfRally.TripleScreen.Mod.dll')|ForEach-Object Hash) -join ',') -eq $payload) 'Repeat changed payload'
$bad=Join-Path $out.FullName 'bad triple package'
Copy-Item -LiteralPath $TriplePackage -Destination $bad -Recurse
Add-Content -LiteralPath (Join-Path $bad 'DbceTripleScreenArtOfRally/ArtOfRally.TripleScreen.Mod.dll') 'tamper'
$before=Tree
Run 'tamper-refused-before-install' @() $true $bad
Check ((Tree) -eq $before) 'Failed preflight changed game'
Run 'remove-wheel-only' @('-Uninstall','-Components','wheel')
Check (-not (Test-Path -LiteralPath (Join-Path $wheel 'ArtOfSimRally.Mod.dll'))) 'Wheel removal failed'
Check (Test-Path -LiteralPath (Join-Path $triple 'ArtOfRally.TripleScreen.Mod.dll')) 'Wheel removal affected triple'
Run 'remove-triple-only' @('-Uninstall','-Components','triple')
Check (-not (Test-Path -LiteralPath (Join-Path $triple 'ArtOfRally.TripleScreen.Mod.dll'))) 'Triple removal failed'
foreach($path in @((Join-Path $wheel 'Settings.xml'),(Join-Path $triple 'Settings.xml'),(Join-Path $triple 'desired-layout.json'))){Check ((Get-Content -LiteralPath $path -Raw) -match 'owner') 'Removal changed owner settings'}
$report=@{schema=1;status='PASS';checks=$script:checks;commands=$script:commands;gamePath=$game;scope='Disposable fake game only; no game/UMM assembly executed; actual released installers used';setup_sha256=(Get-FileHash (Join-Path $PSScriptRoot 'Install-Game.ps1')).Hash;manifest_sha256=(Get-FileHash (Join-Path $PSScriptRoot '../../game-release.json')).Hash}
$report|ConvertTo-Json -Depth 8|Set-Content (Join-Path $out.FullName 'result.json')
Write-Output "PASS $($script:checks) joint installer checks"
