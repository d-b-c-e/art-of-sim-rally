# art-of-sim-rally — Claude working notes

> **Resuming a session? Read `docs/FINDINGS.md` first.** It records everything
> verified on disk about the game. Do not re-derive it, and do not describe a
> component as working if the status table below says it has never run.

## Repository Purpose

Turn art of rally into a sim rig game: force feedback, Forza-compatible UDP
telemetry, bonnet camera. The game's physics are already a real load-sensitive
tire model; this project connects that simulation to a wheel, a dashboard and a
viewpoint.

The founding discovery: **art of rally ships a complete force feedback
implementation that never runs, because `UnityForceFeedback.dll` was left out of
the build.** The managed `ForceFeedback` class P/Invokes seven entry points from
a module that does not exist in the install. `src/UnityForceFeedback/` is a
clean-room implementation of it.

Unlike the sibling `dbce-mod-toolkit` (private by design), this is intended to
be **public and shareable**. Keep it that way: no game assemblies committed, no
third-party binaries, nothing that would force the repo private.

## Repository Structure

| Path | Contents |
|---|---|
| `src/UnityForceFeedback` | C++ x64 DirectInput plugin. The DLL the game is missing. |
| `src/ArtOfSimRally.Telemetry` | Forza Data Out encoder + UDP sender. **No Unity, no UMM.** |
| `tests/ArtOfSimRally.Telemetry.Tests` | xUnit, 24 tests pinning the wire format |
| `tools/ArtOfSimRally.Synth` | Synthetic emitter for testing consumers without the game |
| `harness/forza_probe.py` | Listens and prints what is actually on the wire |
| `docs/` | FINDINGS, FORCE-FEEDBACK, TELEMETRY, CAMERA, ROADMAP |

## Status — do not overstate this

| Component | State |
|---|---|
| `UnityForceFeedback.dll` | Built, x64, 7 exports verified. **Never run in the game.** |
| Telemetry encoder | Done. 24 tests pass. Round-tripped C# → UDP → Python 2026-08-31. |
| Synth + probe | Done, verified together. |
| UMM mod | Not started. Blocked on UMM being installed. |
| Bonnet camera | Not started. |

Phase 0 of `docs/ROADMAP.md` — does the game actually call our DLL — is
**unanswered** and blocks the project's direction. It needs a wheel plugged in.

## Conventions

- **`ArtOfSimRally.Telemetry` targets `netstandard2.0`** and must never
  reference Unity or Unity Mod Manager. It is loaded into Unity 2019.4's Mono
  runtime; targeting net8.0 builds fine and then fails to load in game. That
  isolation is also what lets the encoder be unit-tested on modern .NET with no
  game present — which is why it could be finished before UMM was installed.
- **Solution stays classic `.sln`**, not `.slnx`. The .NET 10 SDK emits `.slnx`
  by default and older SDKs cannot open it. Regenerate with
  `dotnet new sln --format sln`.
- The native DLL is **x64 only**. A 32-bit build fails to load with no
  diagnostic beyond force feedback silently not working.
- `BOOL` in the native plugin is the 4-byte Win32 `BOOL`, never C++ `bool` —
  P/Invoke marshals a C# `bool` return as 4 bytes.
- Dates in YYYY-MM-DD.

## Non-negotiable design rules

1. **No physics or assist changes.** art of rally has online leaderboards.
   Force feedback, camera and telemetry are fair-play neutral; grip, assists and
   car behaviour are not. This is what lets the mod be shared without argument.
2. **Never commit game assemblies or `.CT` files.** Reference the local Steam
   install. This repo must stay publishable.
3. **Bonnet camera, not cockpit.** The cars have no modelled interiors. This is
   a settled decision, not a gap — see `docs/CAMERA.md`.
4. **Do not guess wire-format offsets.** The Forza layout is anchored on
   `Speed`@256 and `Gear`@319, both validated against SimHub via the sibling
   cruisn-collection harness. A wrong offset does not throw, it renders a
   plausible and completely wrong dashboard. There are tests; keep them.

## Environment facts

- art of rally: app id **550320**, build **17584229**, installed at
  `D:\Program Files (x86)\Steam\steamapps\common\artofrally`. Steam root on this
  machine is on `D:`, not a default path.
- Engine: **Unity 2019.4.38f1, Mono** — ideal for modding. Not IL2CPP.
- Input: **Rewired**, all four backends. Wheel input already works; only force
  feedback is missing. `xoutput-redux` is very likely unnecessary.
- MSVC 14.44 x64 build tools and Windows SDK 10.0.26100 with `dinput8.lib` are
  installed. `cl.exe` is not on PATH — `src/UnityForceFeedback/build.bat` sets
  up the environment itself.
- `vcvars64.bat` prints `'vswhere.exe' is not recognized` on this machine. That
  comes from inside Microsoft's script and is harmless; only a non-zero exit
  code means a real failure.

## Testing

```powershell
dotnet test ArtOfSimRally.sln
.\src\UnityForceFeedback\build.ps1          # also verifies all 7 exports
```

End-to-end telemetry check, no game required — run the probe in one shell and
the synth in another:

```bash
python harness/forza_probe.py 8123
```
```powershell
dotnet run --project tools/ArtOfSimRally.Synth -- 8123 10
```

The probe's `src` column reads `mod` when byte 323 carries our `'R'` sentinel,
which distinguishes our packets from anything else already on that port.

## Reading the game's assemblies

No decompiler is needed and nothing needs downloading. Type, field and method
names plus the whole P/Invoke table are readable from metadata with
`System.Reflection.Metadata`, which ships in the .NET SDK. `PEReader` →
`GetMetadataReader()` → enumerate `TypeDefinitions`; `MethodDefinition.GetImport()`
gives the `DllImport` module and entry point. That is how the missing DLL was
found. Method *bodies* would need a real decompiler, which has not been needed yet.
