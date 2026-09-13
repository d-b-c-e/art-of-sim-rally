# Building from source

For development only. Players should use the [release ZIP](../README.md#install).

## Prerequisites

- Windows x64, a local art of rally installation and UMM installed for that game.
- .NET SDK 8 or later for the executable test tools; the mod targets .NET
  Framework 4.8. Install the .NET Framework 4.8 developer/targeting pack if MSBuild
  reports missing reference assemblies.
- Python 3 and PowerShell for the release test runner. Git is required to record
  the packaged build's source identity.

The toolkit's native DLL, managed wrapper and telemetry encoder are committed
under `lib/toolkit`. The pin is **v0.13.0**; no native compiler or sibling checkout
is needed to build this mod. Changes to those components belong upstream.

## Local references and build

From the repository root in PowerShell, set your game path and copy UMM's two
reference assemblies into the ignored `lib/umm` directory:

```powershell
$gameDir = 'D:\Program Files (x86)\Steam\steamapps\common\artofrally'
$ummDir = Join-Path $gameDir 'artofrally_Data\Managed\UnityModManager'
New-Item -ItemType Directory -Force lib/umm | Out-Null
Copy-Item -LiteralPath (Join-Path $ummDir 'UnityModManager.dll') -Destination lib/umm
Copy-Item -LiteralPath (Join-Path $ummDir '0Harmony.dll') -Destination lib/umm
dotnet build ArtOfSimRally.sln -c Release -warnaserror "-p:GameDir=$gameDir"
```

The project references game assemblies directly from that folder. Do not commit
or redistribute them or UMM's assemblies. The mod output is
`src/ArtOfSimRally.Mod/bin/Release/ArtOfSimRally.Mod.dll`.

## Testing and packaging

`dotnet test ArtOfSimRally.sln` does not execute these regression suites. Use
`tools/testing/Test-Rc.ps1` as described in [pre-release testing](PRE-RELEASE-TESTING.md).
It runs the executable suites and installer checks and produces a versioned ZIP,
checksum, automated report and a separate attended checklist.

Read `Version.props` and choose an **unused** `X.Y.Z-rc.N` matching its numeric
version. Published versions and existing candidates are immutable. Pass
`-Corpus <index.json>` when a real local drive corpus is available; captures in
the owner's ignored `results/` directory are not part of a fresh clone.

The complete RC runner currently uses owner-machine paths for game/Mono/native
inspection; adjust those paths before running it on another PC. The `GameDir`
build override above does not reconfigure that runner. Installer-only checks
can also run with `tools/testing/Test-Installer.ps1 -PackageDirectory <extracted-package>`.

Build releases locally. Publishing requires the separate
[release procedure](RELEASING.md) and attended acceptance; a passing offline run
does not prove wheel feel, camera rendering or physical gauge behavior.
