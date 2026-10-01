#requires -Version 7.0
[CmdletBinding()]
param([string]$OutputDirectory)
$ErrorActionPreference='Stop'
if(-not $IsWindows){throw 'The disposable installer gate requires Windows'}
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if(-not $OutputDirectory){$OutputDirectory=Join-Path $root 'artifacts/source-ci'}
$out=[IO.Path]::GetFullPath($OutputDirectory)
if(Test-Path -LiteralPath $out){throw 'Keep previous CI evidence; choose another output directory'}
New-Item -ItemType Directory -Path $out|Out-Null
$watch=[Diagnostics.Stopwatch]::StartNew()
$checks=@()
$documentation=& (Join-Path $PSScriptRoot 'Test-Documentation.ps1') -RepositoryRoot $root
$docResult=$documentation|ConvertFrom-Json
if($docResult.status -ne 'PASS'){throw 'Documentation consistency gate failed'}
$checks += [ordered]@{name='documentation-consistency';status='PASS';documents=$docResult.documents;rejectedRegressions=$docResult.rejectedRegressions}
Write-Output 'Documentation consistency PASS'
function Run-Command([string]$Name,[string[]]$Arguments){
 $output=& dotnet @Arguments 2>&1;$code=$LASTEXITCODE
 $output|Out-File -LiteralPath (Join-Path $out ($Name+'.log')) -Encoding utf8
 if($code -ne 0){$output|Write-Output;throw "$Name failed with exit $code"}
 return $output
}
# Verify the committed, redistributable toolkit inputs without loading native code.
$vendor=Join-Path $root 'components/wheel/lib/toolkit'
$vendorCount=0
foreach($line in Get-Content -LiteralPath (Join-Path $vendor 'MANIFEST.txt')){
 if($line -match '^([A-Fa-f0-9]{64})\s+(.+)$'){
  if((Get-FileHash -LiteralPath (Join-Path $vendor $Matches[2])).Hash -ne $Matches[1]){throw 'Committed toolkit hash mismatch'}
  $vendorCount++
 }
}
if($vendorCount -ne 5){throw 'Unexpected committed toolkit inventory'}
$checks += [ordered]@{name='committed-toolkit-integrity';status='PASS';files=$vendorCount}
$projects=@('tests/UnifiedLifecycle/UnifiedLifecycle.csproj')
foreach($name in @('ForceLifecycle','GameBindings','GameState','Landing','Lifecycle','Support')){$projects+='components/wheel/tests/'+$name+'/'+$name+'.csproj'}
$projects+='tests/SourceCi.Geometry/SourceCi.Geometry.csproj'
foreach($relative in $projects){
 $project=Join-Path $root $relative;$name=[IO.Path]::GetFileNameWithoutExtension($project)
 $config=Join-Path $PSScriptRoot 'NuGet.Config'
 $restore=@('restore',$project,'--configfile',$config,'--verbosity','quiet')
 if($name -eq 'SourceCi.Geometry'){$restore+='--locked-mode'}
 $null=Run-Command ($name+'-restore') $restore
 $null=Run-Command ($name+'-build') @('build',$project,'--no-restore','--configuration','Release','--nologo','--verbosity','quiet','-warnaserror')
 $runtimeConfigs=@(Get-ChildItem -LiteralPath (Join-Path (Split-Path -Parent $project) 'bin/Release/net8.0') -Filter *.runtimeconfig.json)
 if($runtimeConfigs.Count -ne 1){throw 'Expected one offline executable'}
 $dll=$runtimeConfigs[0].FullName.Replace('.runtimeconfig.json','.dll')
 $result=Run-Command $name @($dll)
 $checks += [ordered]@{name=$name;status='PASS';output=($result -join "`n")}
 Write-Output "$name PASS"
}
$fixture=& (Join-Path $PSScriptRoot 'New-InstallerFixture.ps1') -OutputDirectory (Join-Path $out 'fixtures')
& (Join-Path $root 'tools/unified/Test-Installer.ps1') -PackageDirectory $fixture.PackageDirectory -LegacyPackageDirectories $fixture.LegacyPackageDirectories -OutputDirectory (Join-Path $out 'installer')
& (Join-Path $root 'tools/delivery/Test-Delivery.ps1') -PackageDirectory $fixture.PackageDirectory -OutputDirectory (Join-Path $out 'delivery')
$delivery=Get-Content -LiteralPath (Join-Path $out 'delivery/result.json') -Raw|ConvertFrom-Json
$checks += [ordered]@{name='delivery-contract-v1';status=$delivery.status;assertions=$delivery.assertions}
$installer=Get-Content -LiteralPath (Join-Path $out 'installer/result.json') -Raw|ConvertFrom-Json
if($installer.status -ne 'PASS' -or $installer.assertions -le 0){throw 'Installer fixture gate ran no successful assertions'}
$checks += [ordered]@{name='synthetic-installer-policy';status='PASS';assertions=$installer.assertions;cases=$installer.cases}
$watch.Stop()
$result=[ordered]@{status='PASS';elapsedSeconds=[Math]::Round($watch.Elapsed.TotalSeconds,2);checks=$checks;scope='Committed source plus public NuGet dependencies; synthetic installer bytes only';limits=@('No game/Unity/UMM/proprietary assembly builds','Json.NET 13.0.4 is test-only; game-supplied Json.NET compatibility not certified','Synthetic DLL names contain inert text; shipping assembly/package correctness not certified','No private recordings, physical FFB, displayed renderer, loader-version, deployment, or release acceptance')}
$result|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $out 'result.json') -Encoding utf8
Write-Output "Source CI PASS in $($result.elapsedSeconds)s; evidence: $out"
