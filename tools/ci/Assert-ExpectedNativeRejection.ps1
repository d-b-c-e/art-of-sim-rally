# Test-only assertion: no runtime/package installer behavior is changed.
function Assert-ExpectedNativeRejection {
 [CmdletBinding()]
 param([Parameter(Mandatory)][int]$ExitCode,[Parameter(Mandatory)][AllowEmptyCollection()][object[]]$Output,[Parameter(Mandatory)][string]$ExpectedReason,[Parameter(Mandatory)][bool]$TargetUnchanged)
 if($ExitCode -ne 1){throw "Expected child rejection exit code 1; observed $ExitCode"}
 if([string]::IsNullOrWhiteSpace($ExpectedReason) -or ($Output -join "`n") -notmatch [regex]::Escape($ExpectedReason)){throw 'Native rejection reason did not match the verified semantic defect'}
 if(-not $TargetUnchanged){throw 'Native rejection changed disposable target bytes'}
 # Contain only this explicitly checked expected failure. Throws above retain failure.
 # GitHub's built-in PowerShell shell propagates this automatic variable at step end.
 $global:LASTEXITCODE=0
}
