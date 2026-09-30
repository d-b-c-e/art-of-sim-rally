# Building from source

From the consolidated repository root, run `cd components/triple` before the commands below. All component-relative paths in this guide are relative to that directory.

Players should use the release ZIP when one is published. Source builds require Windows, .NET 8 SDK, the local 64-bit art of rally game, and Unity Mod Manager (UMM) installed for that game. The adapter targets .NET Framework 4.8; install its developer targeting pack if MSBuild reports missing reference assemblies.

The projects compile against game, Unity, Json.NET, and UMM assemblies in the local installation. Those inputs are read-only and never copied into the release ZIP. The reusable geometry project has no Unity, Windows, UMM, or optimizer dependency. The repository has no private toolkit dependency.

Set the game folder (the one containing `artofrally.exe`) and build:

```powershell
$env:ART_OF_RALLY_DIR = 'D:\Games\artofrally'
dotnet build Dbce.TripleScreen.sln -c Release
dotnet run --project tests/Dbce.TripleScreen.Core.Tests -c Release --no-build
```

Or pass `-p:GameDir="D:\Games\artofrally"` to `dotnet build`. The tests are an executable project; `dotnet test` alone is not the project gate. The built mod DLL and its own dependencies are in `src/ArtOfRally.TripleScreen.Mod/bin/Release/net48/`.

## Package a release candidate

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File tools/package/Package.ps1 -GameDir "D:\Games\artofrally"
powershell -NoProfile -ExecutionPolicy Bypass -File tools/testing/Test-Installer.ps1 -PackageDirectory "dist\stage-0.3.11"
```

`Package.ps1` builds, runs the geometry/protocol assertions, checks version consistency, and writes `dist/DbceTripleScreenArtOfRally-<version>.zip`, its SHA-256 file, and an extracted stage. The ZIP contains only five mod files plus installer, instructions, and license. `Test-Installer.ps1` exercises the package in an isolated fake game folder with spaces in its name; it does not launch or modify the real game. Packaging requires a clean source tree by default. Use `-AllowDirty` only for a local test candidate; its manifest records `sourceState: dirty`.

The ZIP has `DbceTripleScreenArtOfRally/` at its root for UMM's Mods tab. `Install.bat` next to that folder is the double-click route. No `desired-layout.json` or `Settings.xml` is packaged; each player enters measurements in the mod or imports their own optimizer layout.

Read the [release checklist](RELEASING.md) and [known issues](KNOWN-ISSUES.md) before tagging or changing repository visibility.
