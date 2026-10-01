#requires -Version 7.0
[CmdletBinding()]
param([Parameter(Mandatory)][string]$OutputDirectory)
$ErrorActionPreference='Stop'
if(Test-Path -LiteralPath $OutputDirectory){throw 'Preserve previous wrapper-test evidence'}
New-Item -ItemType Directory -Path $OutputDirectory|Out-Null
function Literal([string]$Text){return "'"+$Text.Replace("'","''")+"'"}
$helper=Literal (Join-Path $PSScriptRoot 'Assert-ExpectedNativeRejection.ps1')
$native=Literal "$env:WINDIR\System32\WindowsPowerShell\v1.0\powershell.exe"
$cases=@(
 @{name='expected-rejection-success';childExit=1;reason='expected semantic rejection';unchanged=$true;after='';expectedExit=0;expectedText='WRAPPER-SUCCESS'},
 @{name='unexpected-child-failure';childExit=7;reason='expected semantic rejection';unchanged=$true;after='';expectedExit=1;expectedText='observed 7'},
 @{name='unexpected-child-success';childExit=0;reason='expected semantic rejection';unchanged=$true;after='';expectedExit=1;expectedText='observed 0'},
 @{name='wrong-rejection-reason';childExit=1;reason='unrelated failure';unchanged=$true;after='';expectedExit=1;expectedText='reason did not match'},
 @{name='rejection-mutated-target';childExit=1;reason='expected semantic rejection';unchanged=$false;after='';expectedExit=1;expectedText='changed disposable target'},
 @{name='later-unexpected-native-failure';childExit=1;reason='expected semantic rejection';unchanged=$true;after='native';expectedExit=1;expectedText='UNEXPECTED-NATIVE-EXIT=9'},
 @{name='later-terminating-validation-failure';childExit=1;reason='expected semantic rejection';unchanged=$true;after='throw';expectedExit=1;expectedText='Later genuine validation failure'}
)
$results=@()
foreach($case in $cases){
 $body=@('$ErrorActionPreference=''Stop''',('. '+$helper),('$childOutput=& '+$native+' -NoProfile -NonInteractive -Command '+(Literal ("Write-Output '"+$case.reason+"'; exit "+$case.childExit))+' 2>&1'),'$childExit=$LASTEXITCODE',('Assert-ExpectedNativeRejection -ExitCode $childExit -Output $childOutput -ExpectedReason ''expected semantic rejection'' -TargetUnchanged $'+$case.unchanged.ToString().ToLowerInvariant()))
 if($case.after -eq 'native'){$body+=('& '+$native+' -NoProfile -NonInteractive -Command ''exit 9''');$body+='Write-Output ("UNEXPECTED-NATIVE-EXIT="+$LASTEXITCODE)'}
 if($case.after -eq 'throw'){$body+='throw ''Later genuine validation failure'''}
 $body+='Write-Output ''WRAPPER-SUCCESS'''
 # Exact documented GitHub built-in pwsh behavior: Stop preference, dot-sourced
 # temporary step script, and propagation of LASTEXITCODE at the end.
 $body+='if ((Test-Path -LiteralPath variable:\LASTEXITCODE)) { exit $LASTEXITCODE }'
 $file=Join-Path $OutputDirectory ($case.name+'.ps1');$body -join "`n"|Set-Content -LiteralPath $file -Encoding utf8
 $output=& (Get-Command pwsh).Source -NoProfile -NonInteractive -Command ('. '+(Literal $file)) 2>&1
 $exitCode=$LASTEXITCODE;$text=$output -join "`n";$output|Out-File -LiteralPath (Join-Path $OutputDirectory ($case.name+'.log')) -Encoding utf8
 if($exitCode -ne $case.expectedExit -or $text -notmatch [regex]::Escape($case.expectedText)){throw "Wrapper regression failed: $($case.name), exit $exitCode, $text"}
 $results += [ordered]@{name=$case.name;childExit=$case.childExit;wrapperExit=$exitCode;expectedWrapperExit=$case.expectedExit;status='PASS'}
}
# Each negative wrapper control has been explicitly checked above. Contain this
# test runner's verified expected failures without altering a genuine failure path.
$global:LASTEXITCODE=0
[ordered]@{status='PASS';cases=$results;assertions=$results.Count;scope='Child-shell exit controls only; GitHub built-in pwsh wrapper semantics; no game/package/device execution'}|ConvertTo-Json -Depth 8|Set-Content -LiteralPath (Join-Path $OutputDirectory 'result.json') -Encoding utf8
Write-Output "PowerShell wrapper regression PASS: $($results.Count)"
