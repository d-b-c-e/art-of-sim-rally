<# Validate and preserve a real capture as a named regression case. No game required. #>
[CmdletBinding()]
param([Parameter(Mandatory)][string]$Capture, [Parameter(Mandatory)][string]$Corpus, [Parameter(Mandatory)][string]$Name)
$ErrorActionPreference='Stop'
if ($Name -notmatch '^[a-z0-9][a-z0-9-]{0,79}$') { throw 'Use a short lowercase case name with letters, digits and hyphens.' }
$Capture=[IO.Path]::GetFullPath($Capture); $Corpus=[IO.Path]::GetFullPath($Corpus)
$receiptPath=Join-Path $Capture 'manifest.xml'
$receipt=[xml](Get-Content -LiteralPath $receiptPath -Raw)
if ($receipt.capture.origin -ne 'game' -or $receipt.capture.schema -notin @('2','3')) { throw 'Only a recorded schema-2/3 game capture can enter the regression corpus.' }
$receiptHash=(Get-FileHash -LiteralPath $receiptPath).Hash
& dotnet run --project (Join-Path $PSScriptRoot 'Replay/Replay.csproj') -c Release -- --replay $Capture
if ($LASTEXITCODE -ne 0) { throw 'Capture failed replay; keep it as diagnostic evidence, not an approved regression case.' }
New-Item -ItemType Directory -Path $Corpus -Force | Out-Null
# Concurrent promotions must fail visibly instead of silently losing an index entry.
$lock=[IO.File]::Open((Join-Path $Corpus 'index.lock'),[IO.FileMode]::OpenOrCreate,[IO.FileAccess]::ReadWrite,[IO.FileShare]::None)
try {
$indexPath=Join-Path $Corpus 'index.json'
$index=if (Test-Path -LiteralPath $indexPath) { Get-Content -LiteralPath $indexPath -Raw | ConvertFrom-Json } else { [pscustomobject]@{schema=1; cases=@()} }
if ($index.schema -ne 1 -or @($index.cases | Where-Object id -eq $Name).Count) { throw 'Unknown corpus schema or duplicate case name.' }
$destination=Join-Path $Corpus "cases/$Name"
if (Test-Path -LiteralPath $destination) { throw 'Case directory already exists; evidence is never overwritten.' }
New-Item -ItemType Directory -Path $destination | Out-Null
$captureFiles=@('frames.csv','forces.csv','manifest.xml')
if ($receipt.capture.schema -eq '3') { $captureFiles += 'signals.csv' }
foreach ($file in $captureFiles) { Copy-Item -LiteralPath (Join-Path $Capture $file) -Destination $destination }
if ((Get-FileHash -LiteralPath (Join-Path $destination 'manifest.xml')).Hash -ne $receiptHash) { throw 'Capture changed while copying; no case was added to the index.' }
# Recheck the copied bytes, not only the source that was valid before copying.
& dotnet run --no-build --project (Join-Path $PSScriptRoot 'Replay/Replay.csproj') -c Release -- --replay $destination
if ($LASTEXITCODE -ne 0) { throw 'Copied capture failed validation; no case was added to the index.' }
$index.cases=@($index.cases)+@([pscustomobject]@{id=$Name;path="cases/$Name";manifestSha256=$receiptHash})
$temporary=Join-Path $Corpus ('index-'+[Guid]::NewGuid().ToString('N')+'.tmp')
$index | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $temporary -Encoding UTF8
if (Test-Path -LiteralPath $indexPath) { [IO.File]::Replace($temporary,$indexPath,$null) } else { [IO.File]::Move($temporary,$indexPath) }
Write-Output "Added $Name. Replay all cases with --corpus $indexPath"
}
finally { $lock.Dispose() }
