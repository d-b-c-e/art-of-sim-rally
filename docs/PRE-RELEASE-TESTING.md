# Pre-release testing

The release mod contains no recording or playback feature. Development capture
lives in a separate UMM mod under `tools/testing/Recorder`; the standalone
`tools/testing/Replay` runner requires only .NET 8 and the vendored managed force
library. It neither loads Unity nor initializes a wheel.

The regression workflow is **record once, replay on each build**. A small probe
inside the game is necessary to observe private physics signals and the actual
force call; ordinary wheel input cannot supply those signals to an external
recorder. Start/stop control, the corpus and playback remain outside the game.
Remove the probe for the final attended checks of the shipped setup.

## Build and validate a candidate

Full RC checks require Windows x64, PowerShell 7, .NET 8+ SDK/runtime and .NET
Framework 4.8, Python 3, installed game/UMM build references, and the Visual Studio
x64 `dumpbin` path in the script. Close the game for installer guards.

```powershell
./tools/testing/Test-Rc.ps1 -Version 0.2.5-rc.6
# Once real cases exist, include them on every candidate:
./tools/testing/Test-Rc.ps1 -Version 0.2.5-rc.7 -Corpus './results/regression-corpus/index.json'
# Validate the exact final-labelled artifact with the same offline suite:
./tools/testing/Test-Rc.ps1 -Version 0.2.5 -Final
```

See [the current overnight review](reviews/2026-09-09-overnight-025.md) for used
0.2.5 RC numbers and evidence. Choose a new RC number for every rebuild. Existing ZIPs/staging directories are
never overwritten. The script writes `dist/ArtOfSimRally-<version>.zip`, its SHA-256,
and a unique `results/rc-*` folder containing logs, `source.json`, `automated.json`
and `manual.json`. Installer tests use a fake game directory; nothing is installed
in Steam, launched, published or sent to a physical wheel.

`Version.props` and Info.json hold numeric UMM version 0.2.5. The assembly also
embeds the RC label, full Git revision and source state. `build.json` and the
allowlisted package manifest identify its bytes. Hashes detect changes; they are
not signatures. An omitted corpus is reported as `recordedCorpus.status="not supplied"`
and `syntheticOnly=true`, never as recorded game coverage. A supplied empty,
missing or failing corpus fails the run.

## Automated coverage

| Check | Evidence and limits |
|---|---|
| Production build | net48, warnings as errors. |
| Signal sampling | Actual managed inverse rotation, compression units and motion-history guards; distinct wheel corners encoded through the pinned toolkit and received over loopback UDP. Included with telemetry transport. No SimHub/motion hardware claim. |
| Support diagnostics | Real bounded UTF-8 log files, recent errors, corrupt numbers, active writer and truncation; early/later frame aggregates and zero allocation over 100,000 updates. Last-session XML identity/expiry/corruption, atomic locked-file preservation and idle/shutdown retry. Included in regression. |
| Force regression | 125,000 dynamic steps against the original formula using the game's real managed Mathf; also verifies the portable frozen replay baseline over 100,000 steps. Floats and device integers, changing tune, resets, fading, clipping and reversals. |
| Save and binding regression | No learned-range disk writes during driving; locked-file preservation and retry, actual Settings XML roundtrip; malformed bindings and wheel GUID selection. |
| Direct-input integration | Actual assignment/normalization/override with fake transport: analog handbrake travel, unbound controls, failed reads/reopen, reversed axes, Flip roundtrip, valid assignment baseline, zero span and button input. Strict axis/shifter GUIDs, duplicate names/reordered lists, idle reconnect, resumed assignment cancellation and locked-file edits. No TSS hardware claim. Included in regression. |
| Camera/lifecycle | Production camera/watchdog with test doubles: handback, disabled callbacks, feature disable, output release before save, and external CameraMod slot isolation in both initialization orders. Rendering and Unity destroyed-object semantics remain attended. |
| Force lifecycle | Actual consumer callback/shared curve with fake output: missing wheel/axle samples, loss of native readiness, invalid force and pause clear stale output/filter history. No physical force or new tune. Included in lifecycle. |
| Passive game-state access | Production predicates against a lazy factory with request side effects; polling must not initialize managers. Also reads the actual game's private field in net48 without Unity constructors. Stage teardown/replacement and camera/engine/driving ownership covered. |
| Camera tuning and bindings | Actual tuner/writer/Settings plus clock/input doubles: locked-file retry, leaving mounted view, save timing, held-key log volume, all 11 actions, cancel/clear/duplicates/defaults, XML and panel/capture/modifier isolation. Included in the lifecycle checkpoint. UMM rendering/key delivery remain attended. |
| Telemetry loopback | Production connection code plus real pinned sender: failed destination suppression, idle connection setup, driving destination deferral, disabled-output parking, three parked packets, destination switch, restart and idempotent shutdown. No SimHub or game physics. |
| Developer probe | Bounded writer, truncation/failure receipts, no disk writes while sampling, retry and IPC; cached observation getters bound to the actual production assembly. |
| Probe hooks | Actual net48 recorder and installed Harmony: attach all hooks, observe Reset and an uninitialized wrapper's rejected force call, unpatch; external PowerShell Start/Status/Stop through the actual named pipe. Unity ECall callbacks still need a live game. |
| Replay/corpus | Synthetic writer output; separate baseline/toolkit histories, reset continuity, exact force integers; schema-3 motion/contact event analysis with jitter/rotation/discontinuity checks, schema-1/2 compatibility; corrupt/incomplete/misaligned data and altered receipts rejected. Promotion preserves signals; duplicate/empty/synthetic cases and overwrite rejected. Real game cases run only when supplied. |
| Native ABI/vector | Vendor hashes, architecture, wrapper exports and unchanged 703-row reference vector. No DirectInput acquisition in this runner. |
| Package/developer exclusion | Exact payload allowlist and compiled metadata inspection. Packaging fails if recorder types or dependencies enter the release assembly; tests prove a recorder DLL is rejected. |
| Installers | Release install/upgrade/uninstall, both native copies, corrupt payload rejection, settings/user files preserved. Developer probe install/remove copies only two files and leaves the production DLL unchanged. |
| Evidence | Reject pending cases, zero assertions, duplicate cases and changed artifacts/evidence. Source remains stable throughout the gate. |

`dotnet test ArtOfSimRally.sln` still runs no tests. The RC script explicitly runs
the executable suites and Python tests. Native/encoder source suites stay upstream
in dbce-wheel-mod-toolkit. No game or UMM assemblies are committed or packaged.

## Capture and reuse a drive

The independent `tools/testing/EffectsLab` study is development-only and synthetic.
Its numerical scenarios investigate future effect mixing and signal quality;
they are not a force-tune change, a release gate or a real capture corpus.

Follow [TEST-DRIVE.md](TEST-DRIVE.md) for the short walkthrough. Install the probe
only with the game closed; it observes the installed candidate without rebuilding
or replacing it. Use `Record-Drive.ps1 -Command Start|Status|Stop` from PowerShell.
Start/Stop require pause or a menu; recording never begins automatically. The
named pipe is restricted to the current Windows user.

Capture records frame timing and cached direct-input channels, plus the physics
inputs, current tune, filter history and observed integer sent to the shared native
wrapper. `direct_input=0` means those cached channels are not the active path:
Rewired keyboard/pad inputs and complete physics state are not recorded. The
receipt snapshots game/Unity identity, installed mod/recorder/force-library hashes,
settings and resident native diagnostics. Force rows carry subsequent tuning.
Schema 3 adds aligned contact, suspension and Rigidbody motion observations;
standalone replay includes bounded landing/slide candidates and road context.
See [DEVELOPMENT-CAPTURE.md](DEVELOPMENT-CAPTURE.md) for units, unknowns and
discontinuities. Schema 1/2 compatibility and corruption tests remain in the gate.

Fixed arrays hold 250,000 frames and 90,000 force steps (about 14 MB). Callbacks
append structs without per-sample reflection or CSV writes. Stop writes only when
not driving; ordinary shutdown saves after production output release. Save failure
keeps buffers, overflow marks the recording incomplete, and an observation exception
halts sampling without propagating into physics. A crash before saving leaves no
completed recording. Probe overhead needs a capture-on/off comparison.

```powershell
dotnet run --project tools/testing/Replay -c Release -- --replay 'C:/path/to/capture'
./tools/testing/Add-RegressionCapture.ps1 -Capture 'C:/path/to/capture' -Corpus './results/regression-corpus' -Name cold-stage-r12
dotnet run --project tools/testing/Replay -c Release -- --corpus './results/regression-corpus/index.json'
```

Promotion validates both source and copied files, saves only the three evidence
files and indexes the manifest hash. Use meaningful case names (car/stage/tune in
accompanying notes) and preserve the corpus across builds. It currently lives under
ignored `results/`; back it up before deleting local results. Review settings and
native paths in receipts before sharing recordings publicly.

Schema 2 maintains separate original/toolkit smoothing histories across resets;
legacy schema 1 is readable for per-row analysis but cannot enter the continuous
regression corpus. A force-disabled run can provide timing CSVs but cannot pass
force replay. Frame statistics separate the first 15 seconds of each driving
segment from later frames, including hitches over 100 ms. There is no automatic
performance threshold or stage identifier; label cold/repeated/new-stage runs.
Native return values are observations of API acceptance, not measured wheel torque.

## Attended release gate

Seven cases remain: cameras, opening stutter, FFB lifecycle, input persistence,
telemetry, support identity and the normal UMM upgrade route. Test the final setup
with the developer probe removed. Fill `manual.json` with `tester`, `rig`, each
case's `status`, notes and one or more evidence entries:

```json
{"path": "C:/path/to/observations.txt", "sha256": "UPPERCASE_SHA256"}
```

Obtain hashes with `Get-FileHash -Algorithm SHA256`. Human observations are
evidence; recordings, support bundles and camera video substantiate them. Do not
mark unavailable hardware tested: a MOZA pass does not certify Fanatec, and missing
PS5-pad coverage belongs in the camera notes.

```powershell
python tools/testing/rc_gate.py check 'results/rc-.../manual.json'
```

Pending/skipped/failed cases, missing evidence and changed reports or archives
fail. `automated.json` retains `runtime=pending`; the manual gate is the separate
sign-off. Nothing publishes automatically. Any rebuild, including changing an RC
label to a final label, needs evidence for the artifact actually distributed.

## Relation to cruisn-collection

That project's MAME `.inp` playback benefits from emulator-controlled frames and
initial machine state. art of rally supplies neither, and injected keyboard input
is known not to work here. We reuse immutable cases, completion receipts, build
identity, bounded capture and offline replay discipline. Full Unity playthrough
automation needs a separate input/state/determinism design. Signal replay does
not automate cameras, regenerate a stage, or validate force feel.
