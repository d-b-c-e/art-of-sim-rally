# Pre-release testing

This framework produces an identified candidate, offline evidence and an attended
checklist. A successful build or offline replay does **not** mean the release is
ready. The game still needs a person at the rig.

## Build a candidate

Requires Windows x64, .NET 8+ SDK/runtime, Python 3, the installed game/UMM build
references, and the Visual Studio x64 `dumpbin` path used in `Test-Rc.ps1`.
The game should be closed: the installer smoke test also checks that guard.

```powershell
./tools/testing/Test-Rc.ps1 -Version 0.2.3-rc.5
```

Use a new RC number for each rebuild. The script refuses to overwrite a candidate.
It writes the zip and `.sha256` under `dist/`, and logs, `source.json`,
`automated.json` and `manual.json` under a unique `results/rc-*` directory.
Nothing is installed in Steam; installer tests use an isolated fake game layout.
No test calls DirectInput initialization, drives a wheel, injects input or publishes.

`Version.props` and `Info.json` carry the numeric UMM version, currently `0.2.3`.
The DLL's informational version carries the RC label, full Git revision and
clean/dirty source state. Its SHA-256 distinguishes dirty builds at the same
revision. `build.json` and the package's allowlisted `manifest.json` preserve that
identity. Hashes detect accidental changes; they are not a signature.

## What runs offline

| Gate | Evidence and limits |
|---|---|
| Release build | Warnings treated as errors; builds the production net48 assembly. |
| Force regression | 125,000 dynamic steps compared with our v0.2.2 formula using the installed game's real managed `Mathf`; compares floats and device magnitudes across fades, clipping, reversals, strength, smoothing and resets. |
| Toolkit comparison | Compares pinned AxleForceCurve@1 with the original formula. Keeps the old SimLite counterexample to prevent substituting that different tune. |
| Save policy | No learned-range writes while driving; failed writes and exceptions stay pending; five-second retry backoff; forced final retry and duplicate-shutdown handling. A real locked-file test preserves the prior XML, retries successfully and roundtrips the actual Settings type. |
| Camera/lifecycle | Compiles the production camera, watchdog and capture files against minimal test doubles. Checks ownership, stock handback, disabled callbacks, feature disable, and release-before-save ordering. Does not simulate Unity rendering, destroyed objects or Harmony patch installation. |
| Capture/replay | Produces a synthetic capture with the production writer, checks row counts/hashes/arithmetic, rejects altered, unfinished and truncated captures. Synthetic output is never evidence of a real playthrough. |
| Vendor/ABI | Verifies vendor manifest hashes, native x64 architecture and all shared wrapper delegate exports; version/path inspection only maps the native DLL without acquiring a device. |
| Package/installer | Exact payload allowlist, matching versions/hashes, no game or UMM assemblies; install, upgrade, both native copies, uninstall preserving settings/user files, and corrupt-package rejection before writes. UMM's actual GUI route remains attended. |
| Evidence gate | Tests rejection of pending cases, missing/duplicate cases, zero assertions, changed artifacts and changed evidence. Source files must not change during the gate. |

`dotnet test ArtOfSimRally.sln` still runs no tests. Use the script above; it calls
the executable regression runners explicitly and rejects empty results. These
are consumer tests; the telemetry encoder and native lifecycle suites belong in
dbce-wheel-mod-toolkit.

For the hands-on walkthrough, see [TEST-DRIVE.md](TEST-DRIVE.md).

## Record an attended drive

1. Close the game and install the **packaged** candidate. Keep your existing
   settings. Record wheel/base software settings, car, stage, game build and
   whether any controller (especially a PS5 pad) is connected.
2. At a menu or while paused, open Ctrl+F10 → **Devices and troubleshooting** →
   **Start drive capture**. Resume and drive. Capture is off by default.
3. Pause or return to a menu and press **Stop and save drive capture**. The panel
   shows the directory under `%LOCALAPPDATA%\ArtOfSimRally\captures`.
4. Copy that entire directory into your RC evidence folder. Generate a support
   file too. Add observations and screenshots/video for camera and rig behaviour.

The capture records frame timing, whether the player was driving, cached direct
wheel input channels, and the inputs/previous state/result of every evaluated
force step. `direct_input=0` means those channel values are not the active input
path; Rewired pad/keyboard inputs and game physics state are **not** recorded.
Metadata snapshots settings at capture start, game/Unity/build identity and
resident native DLL diagnostics; force rows also carry the current tuning.
Settings changed later outside those force fields belong in the final support file.

Callbacks append structs to fixed arrays (250,000 frames and 90,000 force steps,
about 14 MB total). There is no CSV formatting or disk IO in those callbacks.
Stop saves only when not driving; normal shutdown releases outputs first, then
saves. A save failure keeps the buffers for a retry in a new directory. Overflow
is marked incomplete; a crash before save leaves no completed recording. Capture
is diagnostic overhead, so compare stutter runs with recording both on and off.

## Replay and inspect evidence

```powershell
dotnet run --project tests/Regression/Regression.csproj -c Release -- --replay "C:\path\to\capture"
```

This compares **force arithmetic only** between the original formula and pinned
toolkit. Schema 2 carries separate filter histories across reset epochs; schema 1
remains a per-row comparison using its recorded previous state. It emits frame-time statistics (including the first 15 seconds of each
driving segment), device-magnitude comparison and row counts. It rejects missing
receipts, truncated captures, hash/count/schema mismatches, nonfinite rows,
missing frames and captures with no drive/force samples. A force-disabled run
can still provide a timing CSV for inspection, but cannot pass force replay.

It does not recreate the game, verify native delivery to a wheel, establish
deterministic Unity physics, or judge camera pixels. There is no automatic timing
pass threshold: compare cold launch, same-stage restart and a different stage,
and investigate visible lockups and frames over 100 ms. Each pause/resume creates
a new driving segment. Capture lacks a stage ID, so label those runs in notes.

## Attended release gate

`manual.json` contains seven required cases: camera transitions, opening stutter,
FFB lifecycle, input/settings persistence, telemetry, support identity and UMM
upgrade. Fill in `tester`, `rig`, each case's `status` (`passed` only after testing),
`notes`, and one or more evidence entries:

```json
{"path": "C:/path/to/observations.txt", "sha256": "UPPERCASE_SHA256"}
```

Use `Get-FileHash -Algorithm SHA256` to obtain an evidence hash. Written human
observations are evidence; attach capture/support files and camera video where
they substantiate the result. Keep untested hardware reports open. A MOZA pass
does not certify Fanatec or Thrustmaster, and missing optional PS5 hardware must
be stated in the camera notes rather than marked tested.

```powershell
python tools/testing/rc_gate.py check "results/rc-.../manual.json"
```

Pending/skipped/failed cases, missing evidence, altered reports or a different zip
return nonzero. `automated.json` keeps `runtime=pending` as the historical offline
result; the separate manual gate is the sign-off. Passing never publishes anything.
Test the final artifact you intend to distribute. A new build, including a change
from an RC label to a final label, requires a new evidence set.

## What transfers from cruisn-collection

Reviewed its replay/testing review and `record_drive.py`, `session_case.py`,
`replay.py` and `run_replay_smoke.py`. MAME supplies frame-based `.inp` recording,
playback, initial machine-state control and a clean-playback exit. Those facilities
do not exist here; this game's injected keyboard input is known not to work.

We reuse the discipline: identified builds, immutable cases, settings snapshots,
completion receipts, separate evidence per run, bounded capture and no hardware
output during offline replay. Full game input replay would require a separate,
explicit design for Rewired/direct-input hooks, scene/random state, frame timing
and observable checkpoints. It is future work, not an RC feature.
