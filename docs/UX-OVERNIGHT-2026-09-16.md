# UX-1 adoption audit

Updated **2026-09-19**. The filename identifies the coordinator's UX rollout,
not the date of a hardware test. This is an **isolated implementation candidate**
on `codex/ux-simple-advanced`, based on consumer `992ed83`. The owner subsequently
authorized finishing, integrating and deploying through the cross-product
coordinator on 2026-09-19. RC6 will be backed up before deployment. Exact final
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

## Implemented scope

- Persisted Simple/Advanced and six pages: Setup, Controls, FFB, Cameras,
  Telemetry, Help. Switching presentation does not reset runtime tuning.
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
| `SettingsView`, `SettingsPage` | Header, both | Simple, Setup; invalid presentation displays these defaults without rewriting tuning |
| `SettingsKey`; F8 Stop FFB; Close/Escape | Controls / header, both | F6 rebindable; F8 fixed; cancel edit before close |
| `SettingsButtonBinding`, `StopFfbButtonBinding` | Controls, both | Empty; strict USB button, press/release then Save binding; no change to FFB preference or driving-input enable |
| `WheelInputEnabled` | Controls, both | Off; Save calibration explicitly enables assigned controls |
| `SteerBinding`, `ThrottleBinding`, `BrakeBinding` | Setup bars; Controls, both | Empty; provisional full-travel calibration, per-channel identity and normalization |
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
| Camera bindings | Keyboard and USB direction/reset/release fixtures, reserved keys/timeout, batch-reset conflict and locked-file rollback; actual editor propagation/UMM Escape ordering pending |
| Connection draft | Production atomic policy fixtures plus existing telemetry loopback tests; actual receiver/UI comparison pending |
| Font scale, 720p/4K, sticky header, keyboard focus | Source inherits UMM styles and uses bounded page scroll; **not rendered or walked in-game** |
| Game/UMM binding integration | Mod/camera USB binding complete in source. Game actions use native ControlsRemapper via guarded panel route. Independent route fixtures cover driving/edit/intro/inactive/missing/ambiguous guards; actual navigation remains untested |
| Calibration ergonomics | **Partial:** captured full-travel transaction and one phase of instructions; no dedicated step-by-step wizard |
| Camera scoped reset | Reset this view and runtime reset share one method; offline scope checks preserve the other mount/shortcuts/tunes |
| Native pin and installed copy | No toolkit binary changes or game launch. Local deployment authorized after full gates; RC6 archived before replacement |

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

Run the local gate from this worktree using the existing two real capture cases:

```powershell
./tools/testing/Test-Rc.ps1 -Version 0.2.6-rc.7 -Corpus E:/Source/art-of-sim-rally/results/regression-corpus/index.json
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

The owner/coordinator authorized replacing RC6 on 2026-09-19; preserve its backup
and force/settings evidence. No live slot is granted. With an allocated slot:

1. Back up installed settings and payload. Use a fresh settings fixture, then a
   copy of legacy settings; verify Simple/Setup defaults, explicit Advanced
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
