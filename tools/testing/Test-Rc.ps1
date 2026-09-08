<# Builds an immutable RC (or final-labelled package with -Final), runs offline gates,
   and creates an attended test checklist. -Final does not provide runtime sign-off.
   Never installs into the real game or sends input/force to hardware. #>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Version, [string]$Corpus, [switch]$Final)
$ErrorActionPreference = 'Stop'
$root = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if ($Final) {
    if ($Version -notmatch '^\d+\.\d+\.\d+$') { throw '-Final requires X.Y.Z' }
} elseif ($Version -notmatch '^\d+\.\d+\.\d+-rc\.[1-9]\d*$') { throw 'Use X.Y.Z-rc.N, or X.Y.Z with -Final' }
$run = Join-Path $root ('results/rc-' + $Version + '-' + [Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $run | Out-Null
$shell = (Get-Process -Id $PID).Path
$checks = @()

function Run([string]$Name, [string]$Exe, [string[]]$Arguments, [bool]$ExpectFailure = $false) {
    $output = & $Exe @Arguments 2>&1
    $exitCode = $LASTEXITCODE
    $output | Out-File -LiteralPath (Join-Path $run "$Name.log") -Encoding utf8
    if (($ExpectFailure -and $exitCode -eq 0) -or (-not $ExpectFailure -and $exitCode -ne 0)) {
        throw "$Name unexpected exit $exitCode; see $run/$Name.log"
    }
    return $output
}
function Assert([bool]$Condition, [string]$Message) { if (-not $Condition) { throw $Message } }
function Checkpoint([string]$Name, [int]$Assertions) { $script:checks += [ordered]@{ name=$Name; status='passed'; assertions=$Assertions } }

try {
    Push-Location $root
    # Includes untracked source changes, but ignores build/results directories.
    $sourceFiles = @(& git ls-files --cached --others --exclude-standard | Sort-Object -Unique)
    if ($LASTEXITCODE -ne 0) { throw 'Cannot enumerate source snapshot' }
    $snapshot = [ordered]@{}
    foreach ($file in $sourceFiles) {
        $snapshot[$file] = if (Test-Path -LiteralPath $file -PathType Leaf) { (Get-FileHash -LiteralPath $file -Algorithm SHA256).Hash } else { '(deleted)' }
    }
    $snapshot | ConvertTo-Json -Depth 3 | Set-Content -LiteralPath (Join-Path $run 'source.json') -Encoding UTF8
    $null = Run 'build' 'dotnet' @('build','ArtOfSimRally.sln','-c','Release','--nologo','-warnaserror')
    Checkpoint 'build' 1
    $null = Run 'telemetry-build' 'dotnet' @('build','tests/Telemetry/Telemetry.csproj','-c','Release','--nologo','-warnaserror')
    $result = Run 'telemetry-loopback' (Join-Path $root 'tests/Telemetry/bin/Release/net48/Telemetry.exe') @($root,'D:/Program Files (x86)/Steam/steamapps/common/artofrally')
    $telemetry = $result | Select-Object -Last 1 | ConvertFrom-Json
    Assert ($telemetry.status -eq 'passed' -and $telemetry.assertions -gt 0) 'Telemetry runner ran no assertions'
    Checkpoint 'telemetry-loopback' $telemetry.assertions
    $result = Run 'regression' 'dotnet' @('run','--project','tests/Regression/Regression.csproj','-c','Release','--','--native','lib/toolkit/native/WheelFfb.dll')
    $regression = $result | Select-Object -Last 1 | ConvertFrom-Json
    Assert ($regression.status -eq 'passed' -and $regression.assertions -gt 0) 'Regression runner ran no assertions'
    Checkpoint 'regression' $regression.assertions
    $result = Run 'lifecycle' 'dotnet' @('run','--project','tests/Lifecycle/Lifecycle.csproj','-c','Release')
    $lifecycle = $result | Select-Object -Last 1 | ConvertFrom-Json
    Assert ($lifecycle.status -eq 'passed' -and $lifecycle.assertions -gt 0) 'Lifecycle runner ran no assertions'
    $result = Run 'camera-tuning' 'dotnet' @('run','--project','tests/CameraTuning/CameraTuning.csproj','-c','Release')
    $cameraTuning = $result | Select-Object -Last 1 | ConvertFrom-Json
    Assert ($cameraTuning.status -eq 'passed' -and $cameraTuning.assertions -gt 0) 'Camera tuning runner ran no assertions'
    Checkpoint 'lifecycle' ($lifecycle.assertions+$cameraTuning.assertions)
    $null = Run 'recorder-build' 'dotnet' @('build','tools/testing/Recorder/Recorder.csproj','-c','Release','--nologo','-warnaserror')
    $result = Run 'recorder-tests' 'dotnet' @('run','--project','tests/Recorder/Recorder.csproj','-c','Release','--',(Join-Path $run 'synthetic'),'src/ArtOfSimRally.Mod/bin/Release/ArtOfSimRally.Mod.dll','D:/Program Files (x86)/Steam/steamapps/common/artofrally')
    $recorder = $result | Select-Object -Last 1 | ConvertFrom-Json
    Assert ($recorder.status -eq 'passed' -and $recorder.assertions -gt 0) 'Recorder runner ran no assertions'
    $null = Run 'probe-hooks-build' 'dotnet' @('build','tests/ProbeHooks/ProbeHooks.csproj','-c','Release','--nologo','-warnaserror')
    $result = Run 'probe-hooks' (Join-Path $root 'tests/ProbeHooks/bin/Release/net48/ProbeHooks.exe') @($root,'D:/Program Files (x86)/Steam/steamapps/common/artofrally')
    $hooks = $result | Select-Object -Last 1 | ConvertFrom-Json
    Assert ($hooks.status -eq 'passed' -and $hooks.assertions -gt 0) 'Probe hooks runner ran no assertions'
    Checkpoint 'dev-recorder' ($recorder.assertions+$hooks.assertions)
    $result = Run 'replay' 'dotnet' @('run','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--replay',$recorder.syntheticCapture)
    $replay = $result | Select-Object -Last 1 | ConvertFrom-Json
    Checkpoint 'replay' $replay.assertions
    # Tampering or missing completion receipts must fail, not become a short pass.
    $badCapture = Join-Path $run 'tampered-capture'
    Copy-Item -LiteralPath $recorder.syntheticCapture -Destination $badCapture -Recurse
    Add-Content -LiteralPath (Join-Path $badCapture 'forces.csv') -Value '0,0'
    $null = Run 'replay-tampered' 'dotnet' @('run','--no-build','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--replay',$badCapture) $true
    Rename-Item -LiteralPath (Join-Path $badCapture 'manifest.xml') -NewName 'manifest.incomplete.xml'
    $null = Run 'replay-incomplete' 'dotnet' @('run','--no-build','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--replay',$badCapture) $true
    $truncated = Join-Path $run 'truncated-capture'
    Copy-Item -LiteralPath $recorder.syntheticCapture -Destination $truncated -Recurse
    $receipt = [xml](Get-Content -LiteralPath (Join-Path $truncated 'manifest.xml') -Raw)
    $receipt.capture.complete = 'false'; $receipt.Save((Join-Path $truncated 'manifest.xml'))
    $null = Run 'replay-truncated' 'dotnet' @('run','--no-build','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--replay',$truncated) $true
    $badHistory = Join-Path $run 'discontinuous-capture'
    Copy-Item -LiteralPath $recorder.syntheticCapture -Destination $badHistory -Recurse
    $forcePath = Join-Path $badHistory 'forces.csv'
    $rows = @(Get-Content -LiteralPath $forcePath)
    $cells = $rows[2].Split(','); $cells[9] = '0.125'; $rows[2] = $cells -join ','
    Set-Content -LiteralPath $forcePath -Value $rows -Encoding utf8
    $receipt = [xml](Get-Content -LiteralPath (Join-Path $badHistory 'manifest.xml') -Raw)
    $receipt.capture.forces.InnerText = (Get-FileHash -LiteralPath $forcePath).Hash
    $receipt.Save((Join-Path $badHistory 'manifest.xml'))
    $historyOutput = Run 'replay-discontinuous' 'dotnet' @('run','--no-build','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--replay',$badHistory) $true
    Assert (($historyOutput -join "`n") -match 'filter history is discontinuous') 'Bad filter history was not detected'
    Checkpoint 'replay-rejection' 4
    $corpusReport = [ordered]@{ status='not supplied'; cases=0 }
    if ($Corpus) {
        $result = Run 'recorded-corpus' 'dotnet' @('run','--no-build','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--corpus',([IO.Path]::GetFullPath($Corpus)))
        $recorded = $result | Select-Object -Last 1 | ConvertFrom-Json
        Assert ($recorded.status -eq 'passed' -and $recorded.detail.caseCount -gt 0) 'Recorded corpus ran no cases'
        $corpusReport = [ordered]@{ status='passed'; cases=$recorded.detail.caseCount; index=([IO.Path]::GetFullPath($Corpus)); sha256=(Get-FileHash -LiteralPath $Corpus).Hash }
    }
    $gateOutput = Run 'gate-tests' 'python' @('-m','unittest','discover','-s','tests/testing','-v')
    Assert (($gateOutput -join "`n") -match 'Ran ([1-9]\d*) tests?') 'Release gate test discovery ran nothing'
    Checkpoint 'release-gate' ([int]$Matches[1])
    $vector = Join-Path $run 'force-curve-vector.csv'
    $null = Run 'vector' 'dotnet' @('run','--project','tools/force-vector/ForceVector.csproj','-c','Release','--',$vector)
    Assert ((Get-Content -LiteralPath $vector -Raw).Replace("`r`n","`n") -eq (Get-Content -LiteralPath 'docs/force-curve-vector.csv' -Raw).Replace("`r`n","`n")) 'Committed vector changed'
    Checkpoint 'vector' 703

    # Prove x64 and every native entry point used by the consumer, without InitDirectInput.
    $native = Join-Path $root 'lib/toolkit/native/WheelFfb.dll'
    $reader = [IO.BinaryReader]::new([IO.File]::OpenRead($native))
    try { $reader.BaseStream.Position=0x3c; $pe=$reader.ReadInt32(); $reader.BaseStream.Position=$pe+4; Assert ($reader.ReadUInt16() -eq 0x8664) 'Native DLL must be x64' }
    finally { $reader.Dispose() }
    $dumpbin = 'C:/Program Files/Microsoft Visual Studio/2022/Community/VC/Tools/MSVC/14.44.35207/bin/Hostx64/x64/dumpbin.exe'
    $exports = (& $dumpbin /exports $native) -join "`n"
    if ($LASTEXITCODE -ne 0) { throw 'dumpbin failed' }
    # The regression runner binds the exact alias and checks every delegate export.
    # No consumer may retain its own toolkit P/Invoke declarations.
    $source = (Get-ChildItem -LiteralPath (Join-Path $root 'src/ArtOfSimRally.Mod') -Filter '*.cs' | Get-Content -Raw) -join "`n"
    Assert ($source -notmatch '\[DllImport\((?:Dll|"UnityForceFeedback"|"WheelFfb")') 'Duplicate native binding returned to the consumer'
    Assert ($exports -match '\bSetStrictDeviceSelection\b') 'Strict GUID selection export missing'
    Checkpoint 'native-exports' 40

    $null = Run 'package' $shell @('-NoProfile','-File','tools/package/package.ps1','-Version',$Version)
    $zip = Join-Path $root "dist/ArtOfSimRally-$Version.zip"
    $extracted = Join-Path $run 'package'
    Expand-Archive -LiteralPath $zip -DestinationPath $extracted
    . (Join-Path $root 'tools/installer/verify.ps1')
    $manifest = Assert-Payload $extracted
    $build = Get-Content -LiteralPath (Join-Path $extracted 'ArtOfSimRally/build.json') -Raw | ConvertFrom-Json
    $versionInfo = [Diagnostics.FileVersionInfo]::GetVersionInfo((Join-Path $extracted 'ArtOfSimRally/ArtOfSimRally.Mod.dll'))
    Assert ($versionInfo.ProductVersion -eq $build.identity) 'Archive build identity differs'
    Checkpoint 'package' $PayloadFiles.Count
    $result = Run 'no-recorder' 'dotnet' @('run','--no-build','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--verify-release',(Join-Path $extracted 'ArtOfSimRally/ArtOfSimRally.Mod.dll'))
    $absence = $result | Select-Object -Last 1 | ConvertFrom-Json
    Assert ($absence.detail.recordingFeatureAbsent) 'Recorder leaked into release'
    $null = Run 'no-recorder-rejection' 'dotnet' @('run','--no-build','--project','tools/testing/Replay/Replay.csproj','-c','Release','--','--verify-release','tools/testing/Recorder/bin/Release/net48/ArtOfSimRally.DevRecorder.dll') $true
    Checkpoint 'no-recorder' ($absence.assertions+1)

    # A synthetic game layout, never the Steam install. No executable is launched.
    $game = Join-Path $run 'fake-game'
    $mod = Join-Path $game 'Mods/ArtOfSimRally'
    $umm = Join-Path $game 'artofrally_Data/Managed/UnityModManager'
    New-Item -ItemType Directory -Path $mod,$umm | Out-Null
    Set-Content -LiteralPath (Join-Path $game 'artofrally.exe') -Value 'test placeholder; never executed'
    Set-Content -LiteralPath (Join-Path $umm 'UnityModManager.dll') -Value 'test placeholder; never loaded'
    $settings = Join-Path $mod 'Settings.xml'
    Set-Content -LiteralPath $settings -Value '<Settings><Strength>15</Strength><Smoothing>0.5</Smoothing></Settings>'
    Set-Content -LiteralPath (Join-Path $mod 'user-notes.txt') -Value 'keep'
    $settingsHash = (Get-FileHash -LiteralPath $settings).Hash
    $installer = Join-Path $extracted 'install.ps1'
    $null = Run 'install' $shell @('-NoProfile','-File',$installer,'-GameDir',$game)
    $null = Run 'upgrade' $shell @('-NoProfile','-File',$installer,'-GameDir',$game)
    $probeInstaller = Join-Path $root 'tools/testing/Install-Recorder.ps1'
    $modHash = (Get-FileHash -LiteralPath (Join-Path $mod 'ArtOfSimRally.Mod.dll')).Hash
    $null = Run 'probe-install' $shell @('-NoProfile','-File',$probeInstaller,'-GameDir',$game,'-SkipBuild')
    $probe = Join-Path $game 'Mods/ArtOfSimRally.DevRecorder'
    Assert (@(Get-ChildItem -LiteralPath $probe -File).Count -eq 2) 'Probe copied unexpected dependencies'
    Set-Content -LiteralPath (Join-Path $probe 'user-notes.txt') -Value 'keep'
    $null = Run 'probe-uninstall' $shell @('-NoProfile','-File',$probeInstaller,'-GameDir',$game,'-Uninstall')
    Assert (-not (Test-Path -LiteralPath (Join-Path $probe 'ArtOfSimRally.DevRecorder.dll'))) 'Probe uninstall left recorder DLL'
    Assert (Test-Path -LiteralPath (Join-Path $probe 'user-notes.txt')) 'Probe uninstall removed user files'
    Assert ((Get-FileHash -LiteralPath (Join-Path $mod 'ArtOfSimRally.Mod.dll')).Hash -eq $modHash) 'Probe changed production mod'
    Checkpoint 'dev-installer' 4
    Assert ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Upgrade changed settings'
    foreach ($name in @('Mods/ArtOfSimRally/UnityForceFeedback.dll','artofrally_Data/Plugins/x86_64/UnityForceFeedback.dll')) {
        Assert ((Get-FileHash -LiteralPath (Join-Path $game $name)).Hash -eq $manifest.files.'ArtOfSimRally/UnityForceFeedback.dll') 'Installed native copy differs'
    }
    $null = Run 'uninstall' $shell @('-NoProfile','-File',$installer,'-GameDir',$game,'-Uninstall')
    Assert ((Get-FileHash -LiteralPath $settings).Hash -eq $settingsHash) 'Uninstall changed settings'
    Assert (Test-Path -LiteralPath (Join-Path $mod 'user-notes.txt')) 'Uninstall removed user files'
    Assert (-not (Test-Path -LiteralPath (Join-Path $mod 'ArtOfSimRally.Mod.dll'))) 'Uninstall left managed payload'
    Checkpoint 'installer' 6
    # Corrupt package must fail before copying anything to the fake game.
    Add-Content -LiteralPath (Join-Path $extracted 'ArtOfSimRally/Info.json') -Value 'tampered'
    $null = Run 'install-reject' $shell @('-NoProfile','-File',$installer,'-GameDir',$game) $true
    Assert (-not (Test-Path -LiteralPath (Join-Path $mod 'ArtOfSimRally.Mod.dll'))) 'Invalid package partially installed'
    Checkpoint 'installer-rejection' 1
    foreach ($file in $sourceFiles) {
        $hash = if (Test-Path -LiteralPath $file -PathType Leaf) { (Get-FileHash -LiteralPath $file).Hash } else { '(deleted)' }
        Assert ($snapshot[$file] -eq $hash) "Source changed during gate: $file"
    }
    $finalFiles = @(& git ls-files --cached --others --exclude-standard | Sort-Object -Unique)
    Assert (@(Compare-Object $sourceFiles $finalFiles).Count -eq 0) 'Source file list changed during gate'
    Checkpoint 'source-stability' $sourceFiles.Count
    $report = [ordered]@{ schema=1; status='passed'; release=$Version; artifact=$zip; artifactSha256=(Get-FileHash -LiteralPath $zip).Hash; identity=$build.identity; sourceState=$build.sourceState; completedUtc=[DateTime]::UtcNow.ToString('o'); checks=$checks; runtime='pending'; syntheticOnly=($corpusReport.status -ne 'passed'); recordedCorpus=$corpusReport }
    $reportPath = Join-Path $run 'automated.json'
    $report | ConvertTo-Json -Depth 5 | Set-Content -LiteralPath $reportPath -Encoding UTF8
    $null = Run 'manual-template' 'python' @('tools/testing/rc_gate.py','init',$reportPath,(Join-Path $run 'manual.json'))
    Write-Host "Offline gates passed. Attended tests remain pending: $run/manual.json" -ForegroundColor Green
    Write-Output $reportPath
}
catch {
    [ordered]@{ schema=1; status='failed'; error=$_.Exception.Message; checks=$checks } | ConvertTo-Json -Depth 5 |
        Set-Content -LiteralPath (Join-Path $run 'failed.json') -Encoding UTF8
    throw
}
finally { Pop-Location }
