[CmdletBinding()]
param([Parameter(Mandatory)][string]$Version)
$ErrorActionPreference='Stop'
if ($Version -notmatch '^\d+\.\d+\.\d+-rc\.[1-9]\d*$') { throw 'Haptics is experimental; use a fresh X.Y.Z-rc.N candidate' }
$root=[IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$revision=(& git -C $root rev-parse HEAD).Trim()
if ($LASTEXITCODE) { throw 'Cannot identify source' }
$state=if (& git -C $root status --porcelain --untracked-files=all) {'dirty'} else {'clean'}
$zip=Join-Path $root "dist/ArtOfSimRally-SimHub-$Version.zip"
if(Test-Path -LiteralPath $zip) { throw 'Candidate already exists; use a new RC number' }
& dotnet build (Join-Path $PSScriptRoot 'ArtOfSimRally.SimHub.csproj') -c Release --nologo -warnaserror "-p:ReleaseLabel=$Version" "-p:SourceRevisionId=$revision" "-p:BuildSourceState=$state"
if($LASTEXITCODE) { throw 'SimHub plugin build failed' }
$stage=Join-Path $root ('results/simhub-package-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $stage | Out-Null
$dll=Join-Path $PSScriptRoot 'bin/Release/net48/ArtOfSimRally.SimHub.dll'
$identity=[Diagnostics.FileVersionInfo]::GetVersionInfo($dll).ProductVersion
if($identity -ne "$Version+$revision.$state") { throw 'Plugin identity mismatch' }
Copy-Item -LiteralPath $dll -Destination $stage
foreach($name in @('landing-effect.json','Install-SimHub.ps1','README.md')) { Copy-Item -LiteralPath (Join-Path $PSScriptRoot $name) -Destination $stage }
$files=[ordered]@{}
Get-ChildItem -LiteralPath $stage -File | ForEach-Object { $files[$_.Name]=(Get-FileHash -LiteralPath $_.FullName).Hash }
[ordered]@{schema=1;release=$Version;identity=$identity;files=$files} | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $stage 'manifest.json') -Encoding UTF8
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip
[ordered]@{status='passed';artifact=$zip;sha256=(Get-FileHash -LiteralPath $zip).Hash;identity=$identity;staged=$stage} | ConvertTo-Json -Compress
