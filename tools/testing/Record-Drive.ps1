<# Controls the separately installed developer probe. Never injects game input. #>
[CmdletBinding()]
param([Parameter(Mandatory)][ValidateSet('Start','Stop','Status')][string]$Command, [int]$GameProcessId)
$ErrorActionPreference = 'Stop'
if (-not $GameProcessId) {
    $games = @(Get-Process -Name artofrally -ErrorAction SilentlyContinue)
    if ($games.Count -ne 1) { throw 'Expected one running art of rally process; use -GameProcessId if needed.' }
    $GameProcessId = $games[0].Id
}
$pipe = [IO.Pipes.NamedPipeClientStream]::new('.', "ArtOfSimRally.DevRecorder.$GameProcessId", [IO.Pipes.PipeDirection]::InOut, [IO.Pipes.PipeOptions]::Asynchronous)
$writer = $null; $reader = $null
try {
    $pipe.Connect(3000)
    $writer = [IO.StreamWriter]::new($pipe, [Text.UTF8Encoding]::new($false), 1024, $true)
    $writer.AutoFlush = $true
    $reader = [IO.StreamReader]::new($pipe, [Text.Encoding]::UTF8, $false, 1024, $true)
    $writer.WriteLine($Command.ToUpperInvariant())
    $reply = $reader.ReadLineAsync()
    if (-not $reply.Wait(7000)) { throw 'No reply from probe. Query Status before retrying; the command may have completed.' }
    if (-not $reply.Result.StartsWith('OK ')) { throw $reply.Result }
    Write-Output $reply.Result.Substring(3)
}
finally { if ($reader) { $reader.Dispose() }; if ($writer) { $writer.Dispose() }; $pipe.Dispose() }
