# UX-1 adoption audit

**September22 visual follow-up:** owner screenshots rejected RC13's weak card/tab
hierarchy and redundant Setup page. RC14 clean source `8d37db0` applies that
feedback and shared toolkit guidance `be55195`. All16 gates passed and the exact
package was installed with current owner settings retained at2026-09-23
03:59:32UTC. No game launch occurred; rendered/interaction acceptance remains
pending. The [review](reviews/2026-09-21-visual-grouping.md) records the changes
and checks. RC12/RC13 evidence below is historical.

Updated **2026-09-19**. The filename identifies the coordinator's UX rollout,
not the date of a hardware test. This **installed candidate** was developed
on `codex/ux-simple-advanced`, based on consumer `992ed83`. The owner subsequently
authorized finishing, integrating and deploying through the cross-product
coordinator on 2026-09-19. RC6 was backed up before RC8 deployment. Exact final
source/gate/installed identity belongs in the receipt below and LOCAL-DEPLOYMENT.

Guidance: toolkit `a84bebab5ec2abdcd5140b9c63c139ccff86a7d3`, reviewed again at
`e1f0e3f` and then published `483bebd` for the coordinator's
save-before-effective/batch-reset clarifications,
`docs/CONSUMER-UX.md`, `CONSUMER-SETTINGS-VIEWS.md`,
`CONSUMER-CONTROLS-CAMERAS.md`, `CONSUMER-SETUP.md`, and
`CONSUMER-UX-CHECKLIST.md`. The visual reference at toolkit `95cbd89`,
`docs/reference/wheel-settings.html`, is guidance, not a rendered AOSR screen.
The consumer's **native/managed binary pin stays `358add5`** (local toolkit
0.15.0/native 0.8.0); this UI work does not publish or repin the toolkit.

Guidance revision `be5519541820ce3408953588d78f16e78794f1d6` incorporates
the RC13 rendered findings across projects: perceptible surface boundaries,
navigation distinct from commands, Setup only for a distinct guided workflow,
compact right-side actions with named narrow fallback, and stable legacy page
meaning. It changes toolkit documents only; the binary pin above is unchanged.

Coordinator revision `12df6b325d770625baffd75b2d2eb74f1fcd0a8c` clarifies UX-03 stock-menu pointer/submit/navigation
isolation and release of all keyboard/wheel/pad/Settings controls before handoff,
plus UX-04 neutralization of all roles sharing a disconnected primary device.
The shipped UMM builds a full-screen raycast-blocking canvas, which is source
evidence for pointer interception only. This is not a claim that the game's
cached/native keyboard navigation is suppressed or held-input handoff passes.
The RC9 smoke confirmed native-menu leakage; the successor adds guarded dispatch. Repeat native-menu baseline/overlay comparisons; keyboard,
wheel and pad release aggregation remains an explicit acceptance gap until tested.
WheelInput checks each resolved device's read status before contributing values;
existing failed-read/disconnect fixtures cover handbrake and shortcut paths.
The new shared-primary wording is not a substitute for the full physical matrix.

Guidance revision `94dc3a1288359049eb887f6edaabb16005fc999c` additionally makes
the actual host content bounds, reachable Stop/Close, direct shortcut guards,
queued pointer cancellation and first-use screens explicit. RC10 passed narrow
live keyboard isolation/recovery but overflowed the default4K host. RC11 fixes
host-bound widths and page wrapping without changing UMM preferences; all16
offline gates passed, but a later row-overflow finding prevented deployment. See
[RC10 evidence and RC11 correction](reviews/2026-09-19-rc10-ui-smoke.md).
This does not close UX-03's720p/custom-scale, first-use, wheel/pad or held-input
matrix. The binary pin remains358add5, independently of documentation revisions.

Earlier successor **RC12** passed all16 gates and was installed20:35:39UTC.
Independent review closed its aggregate row overflow at clean `cbcf76c5`;126
settings checks cover the default host and large custom scales. RC11 remained
validation-only. Limited default4K runs passed readable header/all six page
buttons, all six Simple page tops, camera nested scrolling, timed Bind→Escape
cancellation and native Quit recovery. Advanced Cameras top was observed;
the full Advanced/scale/input matrix remains pending. Latest normal exit/exact
restoration completed21:05:25UTC. Exact identity and preserved files are in
[LOCAL-DEPLOYMENT](LOCAL-DEPLOYMENT.md); the linked smoke review scopes each
observation. Historical RC8–10 evidence below is not full layout acceptance.

## Implemented scope

- Persisted Simple/Advanced and five pages: Controls, FFB, Cameras, Telemetry,
  Help. The redundant Setup page is removed; its old stored value opens Controls
  and other old stored page values retain their meaning. Switching presentation
  does not reset runtime tuning.
- F6 selects this mod in UMM's existing window; Ctrl+F10 remains available.
  Settings/Stop FFB also accept strict USB button bindings even when assigned
  driving controls are Off. Press edges and held reconnects are covered offline.
  F8/Stop FFB immediately zero outputs and persist Off. Opening settings stops
  steering/impact output and suppresses direct driving inputs. Shifter input
  made while editing/focus-lost must be released before reuse.
- Provisional axis calibration: strict device identity, full steering travel
  on both sides of centre, pedal release/full endpoints, inversion/deadzone,
  explicit Save/Cancel, timeout/disconnect/ambiguous-input handling. Saved
  explicit ranges stay fixed; legacy learned travel is retained until replaced.
- Separate additive analog and button handbrake channels. A legacy button
  handbrake moves to the new field only if it is empty, after a Settings.xml
  backup. An existing axis and button coexist. A backup failure leaves the old
  binding usable. Conflicting manually edited legacy/new fields are retained,
  not guessed away.
- Strict FFB follow-Steering mode or explicit override. Existing GUID choices
  retain precedence; saved Off stays Off. An unidentified old name/index asks
  for reselection rather than silently trying another controller.
- Camera keyboard/USB shortcut rebinding in Simple; numeric pose/lean/rates and
  Reset this view in Advanced. Keyboard defaults are batch-validated against
  reserved bindings before mutation. Bind/Clear/default edits roll back if the
  atomic settings write fails; calibration drafts remain available for retry.
  Existing camera ownership and key storage are retained. Reserved-key,
  duplicate-key, timeout and cancel checks protect editing.
- Atomic host/port drafts, explicit local SimHub preset, truthful one-way UDP
  status, support export in Simple and detailed diagnostics in Advanced.

The [candidate guide](UX-SETTINGS-CANDIDATE.md) describes the actual UI. Published
0.2.5 instructions remain clearly labelled; the candidate ZIP's own README uses
the new paths. Settings are a single XML object, with no duplicated Simple tune.

## Setting and action inventory

Every persisted Settings field is covered below. “Both” means it appears in
Simple and Advanced; expanding a basic group does not select Advanced.

| Stored key or action | Page/view | Default / treatment |
|---|---|---|
| `SettingsFollowHostScale` | Help, both (successor) | Off: auto screen scale at default UMM1x; explicit UMM scale takes priority; On follows host exactly |
| `SettingsView`, `SettingsPage` | Header, both | Simple, Controls; old Setup and invalid values display Controls without rewriting tuning; other old page values retain meaning |
| `SettingsKey`; F8 Stop FFB; Close/Escape | Controls / header, both | F6 rebindable; F8 fixed; cancel edit before close |
| `SettingsButtonBinding`, `StopFfbButtonBinding` | Controls, both | Empty; strict USB button, press/release then Save binding; no change to FFB preference or driving-input enable |
| `WheelInputEnabled` | Controls, both | Off; Save calibration explicitly enables assigned controls |
| `SteerBinding`, `ThrottleBinding`, `BrakeBinding` | Controls, both | Empty; provisional full-travel calibration, per-channel identity and normalization |
| `HandbrakeBinding`, `HandbrakeButtonBinding` | Controls, both | Empty; independent analog and button; max with stock game input |
| `ClutchBinding` | Controls, expanded basic group | Empty; same calibration transaction |
| Binding calibration suffix (`Left`, rest/far, inversion, deadzone) | Controls candidate edit, both | No new global defaults; legacy strings preserved; new deadzone 0; UI 0–10%, parser accepts saved 0–25% |
| `DirectSteering`, `ZeroAxisDeadzone`, `BindAnyDevice`, `GlyphTextFallback` | Controls, Advanced | All On; custom summary in Simple |
| `DisableSteerAssist` | Controls, Advanced, explicitly legacy | Off; preserve existing value; no new assist implementation or automatic setting change |
| `UseDirectInputBackend` | Not offered | Abandoned backend switch; existing InputBackend guard retained; do not re-enable |
| `ShifterEnabled`, `ShifterIsHPattern`, `SkipNeutral` | Controls, expanded basic group | Off, sequential, On; retain working game paddles |
| `ShifterDeviceIndex`, `ShifterDeviceName`, `ShifterDeviceGuid` | Controls shifter picker | -1/empty/empty; identity-backed existing reader |
| `ShiftUpButton`, `ShiftDownButton` | Controls shifter, sequential | -1 (unbound); Bind/Cancel/Clear, 10s timeout |
| `GearReverseButton`, `Gear1Button`, `Gear2Button`, `Gear3Button`, `Gear4Button`, `Gear5Button`, `Gear6Button` | Controls shifter, H-pattern | -1; displayed button numbers are one-based |
| Game driving/menu actions | Controls, Open game bindings; Cameras shortcut | Existing ControlsRemapper/PanelManager; no recreated action maps, guarded fallback instruction |
| `ForceFeedbackEnabled` | FFB, both; F8/header | On for new settings; saved Off respected on every selection/restart |
| `FfbDeviceMode`, `PreferredDevice`, `PreferredDeviceIndex`, `PreferredDeviceGuid` | FFB, both | Infer legacy explicit or new follow-Steering; strict GUID, no fallback |
| Refresh/retry connection | FFB, both | Paused; release old device before reacquiring |
| `Strength` | FFB, both | 50, UI 0–100; existing gain Strength/50 |
| `Smoothing`, `Invert` | FFB, Advanced | .2 (display20%), Off; custom summary in Simple |
| `FyReference` | FFB, Advanced | 11500 N; shows retained legacy value and explicit restore-default action |
| `LandingEffectsEnabled`, `LandingStrength` | FFB, Advanced | On, 5%; range0–40, same 25Hz/120ms waveform |
| `CrashEffectsEnabled`, `CrashStrength` | FFB, Advanced | Off, 50%; range0–100, same RC6 finite constant120ms |
| `BonnetCameraEnabled`, `BumperCameraEnabled` | Cameras, both | On; suspended if Nexus CameraMod loaded |
| `CameraTuningKeys` | Cameras, expanded basic group | On; adjustment only when panel closed and keys released |
| `KeyUp`, `KeyDown`, `KeyForward`, `KeyBack`, `KeyLeft`, `KeyRight` | Cameras, expanded basic group | Numpad8/2/9/7/4/6; Bind/Clear |
| `KeyPitchDown`, `KeyPitchUp`, `KeyFovUp`, `KeyFovDown`, `KeyReset` | Cameras, expanded basic group | Numpad1/3/+/−/0; Bind/Clear; restore shortcut defaults |
| `CameraButtonBindings[0..10]` | Cameras, expanded basic group | Empty; matches keyboard action order above; device identities independent, keyboard OR button, reset on press edge |
| `BonnetHeight`, `BonnetForward`, `BonnetSide`, `BonnetPitch`, `BonnetFOV` | Cameras, Advanced bonnet pose | .95m,1m,0m,3°,75°; per-control default |
| `BumperHeight`, `BumperForward`, `BumperSide`, `BumperPitch`, `BumperFOV` | Cameras, Advanced bumper pose | .45m,1.9m,0m,2°,80°; per-control default |
| `BonnetLean` | Cameras, Advanced, shared | .1 (display10%); custom summary in Simple |
| `TuneMoveSpeed`, `TuneAngleSpeed` | Cameras, Advanced | .4m/s,20°/s; retained values and explicit defaults |
| `TelemetryEnabled` | Telemetry, both | Off; receiver destination always visible |
| `TelemetryHost`, `TelemetryPort` | Simple explicit local preset; Advanced connection draft | 127.0.0.1:8000; atomic Apply/Cancel, idle socket change |
| `DiagnosticLogging` | Help, Advanced | Off; one checkbox, no duplicate state |
| Create support file, device diagnostics | Help both / Advanced details | Local export only; identifiers/paths/logs disclosed before action |
| Development recording | Not shipped in UI | Separate probe, per owner's explicit requirement |

## Evidence and gaps

“Offline pass” below means source fixtures exercised production logic or a
real runtime patch attachment. It does not mean a player operated the UI.

| Requirement | Evidence / remaining work |
|---|---|
| View defaults, persistence, no tune reset | Offline XML/view-policy fixtures; legacy crash19.52381/landing20 and saved FFB Off retained |
| Calibration Save/Cancel/timeout/disconnect | Production WheelInput with fake device transport; full asymmetric travel, partial analog, malformed suffix, ambiguity, locked-file failure and successful retry coverage |
| Separate USB axis/button | Independent fake device state/GUID, reorder, independent disconnect and stock-control coexistence; real TSS/Fanatec pending |
| Strict FFB following/override | Pure selection fixtures and existing native identity regressions; actual device acquisition after rebind pending |
| F8 and save failure | Production UI runtime helper with stub host/output: stop before saved Off, no automatic restart, failure shown/backoff/retry |
| Panel/focus suppression | Production force/input/impact/shifter fixtures; physical stop and held-input behavior pending |
| UMM route and cancel-first hook | Route tested against a host fixture; actual UMM ToggleWindow hook attaches/unpatches under game's Unity Mono without executing UI |
| FFB recovery behavior | Existing steering smoothing remains; no dedicated time-based post-panel ramp added. UX-05 says normal suspension *may* resume with a ramp; retain RC6 force arithmetic. At smoothing0 return is immediate after gates, explicitly not a ramp claim |
| Camera bindings | Keyboard and USB direction/reset/release fixtures, reserved keys/timeout, batch-reset conflict and locked-file rollback; RC12 timed Escape canceled the active keyboard bind while keeping the panel open and native menu unchanged; physical USB checks pending |
| Connection draft | Production atomic policy fixtures plus existing telemetry loopback tests; actual receiver/UI comparison pending |
| Font scale, 720p/4K, sticky header, keyboard focus | RC12 limited default4K header/page/scroll checks pass; full Advanced,720p/custom-scale and physical input checks remain pending |
| Game/UMM binding integration | Mod/camera USB binding complete in source. Game actions use native ControlsRemapper via guarded panel route. Independent route fixtures cover driving/edit/intro/inactive/missing/ambiguous guards; actual navigation remains untested |
| Calibration ergonomics | **Partial:** captured full-travel transaction and one phase of instructions; no dedicated step-by-step wizard |
| Camera scoped reset | Reset this view and runtime reset share one method; offline scope checks preserve the other mount/shortcuts/tunes |
| Native pin and installed copy | Toolkit unchanged; RC9 installed and menu-smoked with FFB/telemetry Off, exact settings restored; RC6/RC8 backups retained |

This is not a claim of complete attended UX-1 compliance. Visual/runtime work
remains open. The native game binding exception is grounded in these inspected
build17584229 methods: ControlsRemapper.OnPollForElementAssignment requires its
scene buttons/TimeoutDisplay, disables the UI action map, and later EndPolling
restores the map/back button and saves Rewired data. UIManager.Instance lazily
instantiates a prefab. Calling that capture from a detached UMM widget would
skip its lifecycle; the new route instead uses an existing active ControlsSettings
panel and PanelManager history, never the lazy factory. Raw Input-unreadable
Fanatec devices cannot be repaired by remapping DirectInput indices to Rewired
element IDs without a verified association. The UI explains the main-menu and
keyboard/pad fallback. This retains the game's working binder rather than
claiming an unavailable direct game-button path or changing physics/input backends.
No subjective crash, landing, motion or early-landing issue is closed by UI work.

## Reproducible validation

The first full run at `8807080` stopped at CLR probe-hook attachment, before
packaging. The new direct Unity focus getter in the watchdog exposed an ECall
to the CLR JIT. A non-inlined Main.HasFocus boundary preserves the focus gate
while allowing the standalone probe attachment fixture. Its 18 checks pass
after that fix; the complete run must be repeated. The failed run is retained
as `results/rc-0.2.6-rc.7-44f3c06b9bd94b9a8daaa84794a0c76d/failed.json`.

All 16 RC7 gates subsequently passed at clean `a2e918178aa293e69424f1d1007b852dd2d07f12`;
its ZIP SHA-256 is `B82A39C53E22CD670DBCB7FDB6C2970372F044B605D867FEF25B74865C943CC9`.
That package was not installed: a final review confirmed camera keys could
overlap native keyboard actions. The next scoped change checks player 0's
loaded keyboard maps before camera Bind, batch defaults or Settings-key Bind.
It uses Rewired's `ActionElementMap.keyCode` Unity conversion, not a guessed
enum cast. Read-only queries include inactive contexts and modified chords;
both can conflict with a bare-key camera shortcut after the editor closes.
Unavailable maps reject the edit without writing. No Rewired backend/map is
changed. Later changes through the game's own binder can introduce a conflict
again; existing saved keys are not silently migrated. The separate DirectInput
button identity limitation does not apply to these keyboard checks.

The camera suite now passes 328 checks, including action-labelled conflicts,
late-default batch rejection, unavailable/throwing maps, unchanged XML and a
successful retry. Production builds against the shipped Rewired assembly.
These are offline checks; live key/UI acceptance remains pending.

Run the local gate from this worktree using the existing two real capture cases:

```powershell
./tools/testing/Test-Rc.ps1 -Version 0.2.6-rc.8 -Corpus E:/Source/art-of-sim-rally/results/regression-corpus/index.json
```

The SettingsUi suite is part of Test-Rc's regression checkpoint; it exercises
production policies and the UI lifecycle helper without hardware. WheelInput,
ForceLifecycle and Landing cover the new suppression/calibration paths. The
ProbeHooks Unity Mono mode checks the UMM close hook as well as collision hooks.
The gate also tests ordinary telemetry, exact capture replay, packaged payload,
installer entry points and exclusion of the recorder. See the final validation
receipt appended after the immutable run; individual passing suites alone are
not the full gate.

## Next attended check

RC8 (`2517162d71fc12e030ecda64d8b46d31f0d56b20.clean`) passed all 16 local gates;
the [gate](../results/rc-0.2.6-rc.8-a16bd4f3f545419d91ccc2ca7f774b90/automated.json)
retains all logs and the untouched pending manual checklist. Both recorded
drives pass, including 9,507 landing/crash assertions and the original row4230
landing. The exact ZIP (`0FFB6CEAE47E7268AD2A4815B69F69984D8CEA56C7028A5377E1594B337352C3`)
was freshly expanded and installed at 2026-09-19 19:21 UTC. All installed payloads
match; settings/probe/game/UMM parameters stayed byte-identical. Source is on
main. [Receipt and RC6 backup](../results/ux-rc8-install-8a331d2029c14571892bb2e2cb0f5b4f/receipt.json).
No live game was launched during deployment. RC9 corrects the old page names
in the installer completion console, with no runtime source changes. All 16
gates passed again for clean `44e11dd7a7c0e6dd99cef7f2c8be0620e9ce717a`; exact
RC9 was installed at 19:31 UTC, preserving the same files and tunes. See
[the current receipt](LOCAL-DEPLOYMENT.md). The allocated RC9 smoke failed native-menu isolation and4K readability (KI-40); see [the exact run and successor](reviews/2026-09-19-rc9-ui-smoke.md).

The owner/coordinator authorized replacing RC6 on 2026-09-19; preserve its backup
and force/settings evidence. No live slot is granted. With an allocated slot:

1. Back up installed settings and payload. Use a fresh settings fixture, then a
   copy of legacy settings; verify old Setup opens Simple/Controls, explicit Advanced
   persistence, owner tune values and FFB Off across restart.
2. Walk all pages at720p, rig resolution and larger UMM scale. Check header,
   Stop/Close, wrapped text, errors, nested scrolling and keyboard focus.
3. Bind then cancel, timeout, disconnect and complete an axis calibration.
   Read centre/full lock and released/partial/full pedal values. Repeat for
   separate USB handbrake axis plus wheel button, then reconnect/reorder.
4. Check FFB follow-Steering and explicit override; unplugged selection stays
   unavailable. Test F8, persisted Off, settings stop and controlled return to
   driving. Compare smoothing0 return separately; no physical ramp claim yet.
5. Rebind settings/camera keys; verify reserved/duplicate errors, modifier
   rejection, Escape cancel-first and no camera movement after editing until
   release. Test shifter held while closing, then release/repress.
6. Enter a bad telemetry port, cancel, then apply valid host/port together.
   Check SimHub reception/parking and create a support bundle from Help.

Record exact source/package hashes and attended observations. Preserve failures;
do not infer a passed hardware matrix from an offline gate or prior RC6 drive.

RC10 successor: reviewed native dispatch/release guards and scoped text scaling
passed all16 local gates at clean2ab6fb5 and were installed20:10:28UTC.
See [current deployment](LOCAL-DEPLOYMENT.md) for exact receipt. The RC9 failure
remains recorded; successor live recheck and the full hardware matrix are pending.
