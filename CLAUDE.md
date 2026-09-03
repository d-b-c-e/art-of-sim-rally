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
| `lib/toolkit/` | **Vendored** from dbce-wheel-mod-toolkit (pinned by `VERSION`; refresh with `tools/Sync-Toolkit.ps1`): `native/WheelFfb.dll` (shipped as `UnityForceFeedback.dll`, the name the mod P/Invokes) and `dotnet/Dbce.Wheel.Telemetry.dll`. The native source and the encoder live in that repo now. |
| `docs/` | FINDINGS, FORCE-FEEDBACK, TELEMETRY, CONTROLS, CAMERA, ROADMAP, RELEASING |

## Status (2026-09-03) — do not overstate this

Released: **0.2.1** (2026-09-02). "Verified" means confirmed on the owner's MOZA
R12 rig unless stated otherwise.

| Component | State |
|---|---|
| Force feedback | Verified. Front-axle lateral force × pneumatic trail, faded out below 12 km/h, re-acquires the wheel after alt-tab. Sign confirmed on a MOZA R12; the MOZA R5 one-sided inversion fixed by user report. |
| Steering fixes, bind-any-device, glyph text fallback | Verified. |
| Shifter (sequential + H-pattern), read directly from the device | Verified by users. |
| Bonnet + bumper cameras | Verified. End-of-stage cinematic wobble is a known cosmetic issue (docs/CAMERA.md). |
| Telemetry (Forza format) | Verified with SimHub + ButtKicker, live from the start line. |
| **Direct wheel input** (`WheelInput`) | Built 2026-09-03; all devices open and read on this rig; **not yet driven**. Unreleased. |
| Crash fix (shifter choice after FFB failure), FFB candidate fallback, capability labels | Built 2026-09-03, init verified here. Unreleased. |
| Rewired DirectInput backend switch (`InputBackend`) | **Abandoned** after four attempts. Settings.xml-only experiment. Do not retry — see below. |

The game's force feedback was half-built: `ForceFeedback` is never attached,
`Wheel.Mz` is computed only `if (cardynamics.enableForceFeedback)`, which
nothing sets, and `CarDynamics.forceFeedback` is never assigned. The mod sets
the flag, computes a force from the steered axle and drives the DLL. The force
is **not** `Mz` any more — see "Findings" below and docs/FORCE-FEEDBACK.md.

## Conventions

- **The telemetry encoder and native FFB layer are not in this repo.** They are
  vendored built artifacts from dbce-wheel-mod-toolkit under `lib/toolkit`. Fix
  FFB lifecycle or packet-layout bugs *there*, release, then bump the pin here
  with `tools/Sync-Toolkit.ps1 -Version vX.Y.Z`. Game-specific code (hooks,
  force signal, cameras, panel) stays here.

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
5. **Never switch Rewired's input source at runtime in shipped code.** The
   setter calls `ResetAll()`; applied at load it killed the keyboard, applied
   after the title screen it killed the menus while every probe said input was
   flowing. Four attempts over two days (docs/CONTROLS.md). Devices Rewired
   cannot read are handled by `WheelInput`, which bypasses it.
6. **One DirectInput instance per session in the native plugin.** Releasing a
   "temporary" instance while the device table stayed populated crashed the
   game for a Fanatec user. `EnsureDirectInput()` at every entry point;
   `FreeDirectInput` releases the instance only when nothing else holds a device.
7. **Deploy only when the game is closed.** The DLLs are locked while it runs;
   a copy that "succeeds" over a running game is the stale build you tested last.

## Environment facts

- art of rally: app id **550320**, build **17584229**, installed at
  `D:\Program Files (x86)\Steam\steamapps\common\artofrally`. Steam root on this
  machine is on `D:`, not a default path.
- Engine: **Unity 2019.4.38f1, Mono** — ideal for modding. Not IL2CPP.
- Input: **Rewired 1.1.55 on the Raw Input backend.** The game has its own
  press-to-bind screen (`ControlsRemapper`) which only ever binds `Joysticks[0]`
  (the mod retargets it to the device you touch). Unrecognised wheels bind but
  get a hidden 10% deadzone (mod removes it). Some devices Raw Input cannot
  read at all — a Fanatec direct-drive base appears twice as `FANATEC Wheel`,
  32 axes / 144 buttons, no element ever moves; DirectInput reads the same two
  as 8/108 and 12/63 and only one has the actuator. For those: direct wheel
  input. **xoutput/XInput must NOT be used** — it hides the wheel from the
  DirectInput API force feedback needs. See docs/CONTROLS.md.
- Logs: UMM `artofrally_Data\Managed\UnityModManager\Log.txt`; native
  `%LOCALAPPDATA%\ArtOfSimRally\ffb.log`; Unity
  `%USERPROFILE%\AppData\LocalLow\Funselektor Labs\art of rally\Player.log`
  — Rewired's own errors appear only there, without stack traces.
- The game runs on the **second monitor**; a primary-screen screenshot will not
  show it.
- Bindings persist in PlayerPrefs at `HKCU\Software\Funselektor Labs\art of rally`.
  That key not existing means the game has never been launched on this machine.
- MSVC 14.44 x64 build tools and Windows SDK 10.0.26100 with `dinput8.lib` are
  installed. `cl.exe` is not on PATH — `src/UnityForceFeedback/build.bat` sets
  up the environment itself.
- `vcvars64.bat` prints `'vswhere.exe' is not recognized` on this machine. That
  comes from inside Microsoft's script and is harmless; only a non-zero exit
  code means a real failure.

## Findings that must not be re-derived

- **`Mz` is unusable as a steering force.** `CalcAligningForce` is a 1989
  Pacejka curve that reverses sign at ~8° slip; this game's front tyres run
  12–29° in ordinary corners, so the wheel flipped from centring to pushing
  outward mid-corner ("there is no centre"). Force = `(FyL + FyR) × trail /
  FyReference`, trail 1.0 → 0.6 at twice the ideal slip angle, faded 3→12 km/h.
  `+Fy` centres on a MOZA R12. Measured 2026-09-02 with the `FFB trace` lines
  (DiagnosticLogging).
- **`0x80040205` is `DIERR_NOTEXCLUSIVEACQUIRED`**, not INCOMPLETEEFFECT or
  EFFECTPLAYING (both were tried). It means focus was lost and the wheel came
  back non-exclusive; the DLL re-acquires and retries. Look up HRESULTs in the
  SDK's `dinput.h` before theorising.
- **Telemetry `IsRaceOn` is true from `WAITING_TO_BEGIN`**, so a shaker follows
  the engine while revving on the line. Forces still wait for `UNDERWAY`.
- **The Fanatec crash chain** (support bundle 2026-09-03): preferred FFB device
  had no actuator → `CreateEffect` 0x80040154 → instance released → device list
  refilled by a temporary instance → shifter chosen → `CreateDevice` on null.
  Fixed by rule 6 above and by trying every FFB candidate.
- **Rewired reset diagnostics**, for the record: after `ResetAll()` the keyboard
  controller, player actions and UI module all reported input; the game logged
  48,216 "object from a previous session" errors from cached Rewired objects in
  `ControllerButtonDisplay` and `Arcader`; refreshing all 84 references brought
  that to zero and the menus stayed dead. Cause not found. Not worth a fifth try.

## Working on this machine

- **`Stop-Process` from a Bash-spawned PowerShell does not stop the game**; the
  native PowerShell tool does. Same for anything that needs the interactive
  desktop.
- art of rally **accepts `SendInput` keyboard events** (unlike iRacing Arcade),
  so unattended tests can drive the title screen. The x64 `INPUT` struct must be
  40 bytes (`FieldOffset(32) long pad`) or `SendInput` fails with error 87.
  `FindWindow` by title fails; use the process's `MainWindowHandle`.
- `ilspycmd` (dotnet tool) is installed: `ilspycmd -t <Type> Assembly-CSharp.dll`
  for one type, `-p -o <dir>` for the whole assembly. `Rewired_Core.dll` is
  obfuscated internally but its public API decompiles fine.
- Long heredocs in the Bash tool get mangled (quotes, backslashes, truncation).
  Write scripts to the scratchpad with the Write tool and run them by path.
- Support bundles from users are the fastest diagnosis: the controllers section
  shows Rewired's view, the ffb.log section shows DirectInput's. Compare them.

## Testing

```powershell
dotnet test ArtOfSimRally.sln
```

End-to-end telemetry check, no game required — run the probe in one shell and
the synth in another:

```bash
python E:\Source\dbce-wheel-mod-toolkit\tools\forza\forza_probe.py 8123
```
```powershell
python E:\Source\dbce-wheel-mod-toolkit\tools\forza\forza_synth.py 8123
```

The probe's `src` column reads `mod` when byte 323 carries our `'R'` sentinel,
which distinguishes our packets from anything else already on that port.

## Reading the game's assemblies

No decompiler is needed and nothing needs downloading. Type, field and method
names plus the whole P/Invoke table are readable from metadata with
`System.Reflection.Metadata`, which ships in the .NET SDK. `PEReader` →
`GetMetadataReader()` → enumerate `TypeDefinitions`; `MethodDefinition.GetImport()`
gives the `DllImport` module and entry point. That is how the missing DLL was
found. Method *bodies* need a decompiler: `ilspycmd` is installed and used for that (see above).
