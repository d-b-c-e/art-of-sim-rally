# art-of-sim-rally — Codex working notes

> **Resuming a session? Read `docs/FINDINGS.md` first.** It records everything
> verified on disk about the game. Do not re-derive it, and do not describe a
> component as working if the status table below says it has never run.
> Open defects live in `docs/KNOWN-ISSUES.md` — check there before treating a
> symptom as new.

## Repository Purpose

**Current installed candidate: RC13**, clean source
`470e398b89198d60c861ce3ffad800b0ff93854a`, all16 gates passed, installed
2026-09-22 04:15:29UTC (September21 local), game closed. Six payloads and second
native copy verified, six owner/game files retained byte-for-byte. No game launch
or force test. Toolkit358add5/probe0.2.5.4 unchanged. KI-41 rendered acceptance
and the broader KI-39/40 matrix remain pending; exact backup/hashes are in
`docs/LOCAL-DEPLOYMENT.md`.

**Visual grouping follow-up, 2026-09-21:** `codex/simple-visual-grouping` implements
UX-03-G from toolkit `673d451`. See `docs/reviews/2026-09-21-visual-grouping.md`
for actual screenshot/source findings, source review and required rendered checks.
Do not infer in-game layout acceptance from the SettingsUi policy suite; it does
not compile the panel. Keep pre-Bind neutral/release hints visible because the
input layer samples that state at the click. RC12's rendered evidence below is
historical; it does not certify the new layout.

**Previous installed candidate: RC12**, clean source
`cbcf76c5d6401dd8a0c511acfe7ded201df6a99d`, all16 gates passed, installed
2026-09-19 20:35:39UTC. Default4K content fits the actual UMM host; page buttons
wrap and large-scale binding rows stack. Narrow independent source review is
closed. Limited4K smoke passed header/page buttons, all six Simple page tops,
camera nested scrolling, timed Bind→Escape cancellation and native Quit recovery.
Advanced Cameras top was observed; the full Advanced/scale/input matrix remains
pending. Latest normal exit/exact restoration completed21:05:25UTC. RC11 was validated
but not installed.
Exact artifacts/backups/hashes: `docs/LOCAL-DEPLOYMENT.md`. Owner settings,
toolkit358add5 and probe0.2.5.4 retained; no force test or public release.

**Previous RC10**, clean source
`2ab6fb5f27be570693ebe856b48259456f55007f`, all16 gates passed, installed
2026-09-19 20:10:28UTC. It fixes the confirmed RC9 stock-menu dispatch leak and
adds scoped automatic4K text scaling. Live Down/Return/Escape isolation and
native recovery passed; scaled content overflowed the default960px UMM host.
RC12 corrects that width overflow; see `docs/reviews/2026-09-19-rc10-ui-smoke.md`.
The independent source review's missing replay24/25 release case is fixed.
Exact artifacts/backups/hashes: `docs/LOCAL-DEPLOYMENT.md`. Settings, toolkit
358add5 and probe0.2.5.4 retained; no force test/public release. Earlier RC9
receipts and the failed smoke below are historical, not current acceptance.

**Installed UX candidate, 2026-09-19:** `codex/ux-simple-advanced` was integrated
and pushed to main at `2517162d71fc12e030ecda64d8b46d31f0d56b20`.
Simple/Advanced RC8 passed all 16 gates and was installed with an RC6 backup.
**0.2.6-rc.9** (`44e11dd7a7c0e6dd99cef7f2c8be0620e9ce717a.clean`) corrects only
the installer completion text; its complete gate passed and exact package was
installed at 19:31 UTC with the game closed, settings/probe unchanged. Read
`docs/UX-OVERNIGHT-2026-09-16.md` for its inventory, validation and explicit
guideline/runtime gaps and `docs/LOCAL-DEPLOYMENT.md` for exact receipts. Keep
toolkit358add5 and owner crash19.52381/landing20 intact. Offline gates do not
establish in-game UI quality, force feel or complete UX-1 compliance.

**RC9 live smoke failed (KI-40):** Down moves the native main menu behind the
panel; Escape camera-bind cancellation also opens native Quit. Default4K body
text is too small. Game closed normally and exact settings/UMM/payload hashes
were restored at19:52:01UTC. See `docs/reviews/2026-09-19-rc9-ui-smoke.md`.
The successor guards actual native dispatch and held-input handback, with scoped
content scaling; it requires a new serialized runtime slot. Do not mark UX-1 passed.

Turn art of rally into a sim rig game: force feedback, Forza-compatible UDP
telemetry, bonnet camera. The game's physics are already a real load-sensitive
tire model; this project connects that simulation to a wheel, a dashboard and a
viewpoint.

The founding discovery: **art of rally ships a complete force feedback
implementation that never runs, because `UnityForceFeedback.dll` was left out of
the build.** The managed `ForceFeedback` class P/Invokes seven entry points from
a module that does not exist in the install. The clean-room implementation of
it now lives in dbce-wheel-mod-toolkit as `WheelFfb.dll`, vendored here and
shipped under the name the game's dead code P/Invokes.

Unlike the sibling `dbce-mod-toolkit` (private by design), this is intended to
be **public and shareable**. Keep it that way: no game assemblies committed, no
third-party binaries, nothing that would force the repo private.

## Repository Structure

| Path | Contents |
|---|---|
| `src/ArtOfSimRally.Mod/` | The whole mod. One project, one assembly. `Main.cs` is the only loader-aware file. |
| `lib/toolkit/` | **Vendored** from dbce-wheel-mod-toolkit (pinned by `VERSION`; refresh with `tools/Sync-Toolkit.ps1`): `native/WheelFfb.dll` (shipped as `UnityForceFeedback.dll`, the name the mod P/Invokes) and `dotnet/Dbce.Wheel.Ffb.dll` / `Dbce.Wheel.Telemetry.dll`. The native source and the encoder live in that repo now. **These binaries are committed** — see the gitignore note under Findings. |
| `lib/umm/` | UnityModManager.dll + 0Harmony.dll, extracted locally, **never committed**. |
| `tests/` | Executable consumer regression, CameraTuning, WheelInput/Shifter, ForceLifecycle, GameState, lifecycle, telemetry, Signals, Support, recorder and hook suites; Python replay/evidence tests. Run through `tools/testing/Test-Rc.ps1`. |
| `tools/` | `Sync-Toolkit.ps1` (toolkit pin), `package/` (release zip), `installer/` (the double-click installer), `dinput-enum/` (lists DirectInput devices without launching the game). |
| `docs/OVERNIGHT-QUEUE.md` / `docs/USER-FEEDBACK.md` | Prioritized follow-up work, user reports and unsent support drafts. |
| `docs/KNOWN-ISSUES.md` | **The defect register.** Open, resolved and will-not-fix, with severities. Read before diagnosing anything. |
| `docs/TROUBLESHOOTING.md` | User-facing fixes by symptom; the Fanatec section is the most-needed page. |
| `docs/DEVELOPMENT-CAPTURE.md` | Separate probe schema, motion/contact units, standalone analysis and interpretation limits. |
| `docs/` | FINDINGS, FORCE-FEEDBACK, TELEMETRY, CONTROLS, CAMERA, ROADMAP, RELEASING |
| `docs/SETUP.md` / `docs/README.md` / `docs/BUILDING.md` | Player setup, documentation index and developer prerequisites. README stays a short install/first-drive entry point. |
| `tools/installer/README.txt` | Standalone ZIP guide; packaging replaces `@RELEASE@`. Keep aligned with player docs; link online to files not in the archive. |
| `tools/testing/Test-Installer.ps1` | Isolated real batch/Windows PowerShell installer checks; also included in Test-Rc. No game/hardware output. |

## Status (2026-09-19 UTC) — do not overstate this

The RC12 settings candidate above supersedes RC6 without changing its toolkit,
steering/effect arithmetic or saved tune. RC9 failed KI-40; RC10/RC12 fix and
partially retest its input/layout defects. The full attended matrix remains
pending. Historical evidence follows.
Owner's older FFB name/index lacks a GUID: before driving, explicitly select
Use steering wheel or the MOZA on F6 → FFB. Do not silently choose hardware.

**Constant crash candidate:** owner preferred standalone method A at 40%, still
too weak, and requested default50/range0–100. Crash now uses a positive-X 120 ms
finite constant pulse; landing stays default5/range0–40, 25 Hz. Crash stays
opt-in and saved strengths are preserved. One logical impact owns separate
cached constant/sine handles: stop the old handle before replacement; never
repurpose ordinary steering. Toolkit source/knowledge contribution is pushed at
`358add53af220eb95bf606e240b230e23a6ea197`, local package0.15.0/native0.8.0.
Consumer pins these exact artifacts. All 16 gates passed for **0.2.6-rc.6**,
source `1cd7814ac20c647f176c57925875e6af47858e91`; exact package installed
2026-09-17 05:05 UTC. Both real captures pass; the single landing stays at row4230.
Settings/probe preserved: crash on/19.52381, landing on/20. Select crash50
manually to compare the new default. Game closed; no physical force test or
public release. See `docs/LOCAL-DEPLOYMENT.md` for hashes and backup receipt.
Preliminary RC5 was not installed; RC6 corrects its stale packaged instructions.
The tester is now `d6fac19`, default50 with manual choices through100. Desktop
shortcut updated, read-only preflight passed; no agent hardware/UI execution.
The runtime creates idle effects then sets magnitude, unlike diagnostic A's
creation at magnitude: physical equivalence still needs an attended check.
See `docs/reviews/2026-09-17-constant-crash.md`. KI-38 remains open.

**RC4 attended crash test failed:** owner felt no effect at roughly 20 while
normal steering/cornering FFB worked. Support file `art-of-sim-rally-support-20260916-230204.txt`
matches the installed RC4/native700 exactly. Three unique .1952 commands passed
shape readback; Play calls took 2.48–4.83 ms and managed expiry stopped them
124.52–131.80 ms after return, zero early stops. First cue steering was -.0071.
Do not blame gain, claim shaped physical output, or treat API echo as a hardware
measurement. Single-axis direction0 is explicitly documented by Microsoft;
no source-level axis/cancellation defect was found. KI-38 remains open. The
first standalone A/B/C comparison below found only a faint constant pulse.
Read `docs/reviews/2026-09-17-rc4-crash-feel.md`. RC4 is the previous waveform.
The preceding developer-only A/B/C diagnostic was built from upstream clean `38bbfa7`;
both architecture builds/fake checks passed. Frozen owner x64 copy is
`results/attended-effects-38bbfa7/attended_effects.exe`, with Desktop shortcut
**Art of Sim Rally - wheel effect test**. The first tester falsely blocked
BorderlessGaming.exe; exact game-name matching fixes that. Read-only preflight
now passes while that utility remains running. The agent has not opened the
UI or acquired/applied force. Owner ran eight A/B/C requests at 5/20%; all were
accepted/PLAYING and C retained PLAYING through six zero updates. Owner only
faintly felt A at 20% and requested more range. That version added manual30/40,
retaining default5, finite120ms and the same waveforms/axes. The later40%
comparison and current50–100 tester are recorded above. API PLAYING does not
establish torque. Logs live under
`%LOCALAPPDATA%\DbceWheel`; exact hashes, receipts and limitations in review.

**Crash kick installed (KI-38):** RC3 owner drive produced 13 accepted
crash cues at up to .1952, but no distinct felt effect. Saved crashes are now
**on/19.52381**, landing on/20; preserve these newer settings. Candidate uses
upstream shaped finite sine: 6.25 Hz, phase 90 degrees, full 120 ms fade, same peak
strength. Landing waveform/detection, steering, physics and telemetry stay fixed.
Driver parameter readback is required for shaping; rejection latches crashes
unavailable until off/on while paused, preserving the slot for legacy landings.
Managed expiry uses monotonic time after native Play returns; support logs
latency, stop reasons and elapsed command time. This is not actuator measurement.
Read `docs/reviews/2026-09-17-crash-kick.md` and the latest deployment receipt
for exact gate/install status. Physical kick acceptance and official repin remain
pending. Earlier paragraphs describe previous artifacts and settings.
All 16 local gates passed for **0.2.6-rc.4**, clean source
`91cedd8307010c796efe79590661368a52b56d27`; exact package installed at
2026-09-17 03:57 UTC with game closed and settings/probe preserved. Two real
corpus cases and 9,342 landing/crash assertions pass. Toolkit local pin is
`c319b0258b11d1f07492a48dbc110d3e50c14dfd.clean` (0.14.0/native 0.7.0),
not a published toolkit release. The installation did not launch the game;
the later owner test failed as recorded above.

**RC3 stronger impacts:** the 0.2.6 candidate extends both landing and
crash strength from 0–20 to **0–40%** of nominal wheel force. One shared constant
and amplitude mapping serve UI limits, overlap arbitration and final output.
No rescaling/migration: valid existing values keep their force, including the
owner's landing 20; defaults stay 5 and crash stays off. Waveform, detection,
steering and telemetry are unchanged. This implements the reporter's optional
30–40 request; physical stronger-range acceptance remains pending. Read
`docs/reviews/2026-09-16-stronger-impact-effects.md` and the latest deployment
receipt for the validated/installed candidate. Earlier cap-20 entries below
describe prior artifacts, including RC2 and public 0.2.5.
Owner clarified motion telemetry was the priority, then tabled telemetry boosting
and chose wheel-range testing/feedback first. Do not silently extend this work
to motion gain changes; no telemetry multiplier or SimHub profile edit is included.
All 16 local gates passed for **0.2.6-rc.3**, source
`b85c27a735eac7f55661cd84e9f39b0a20d17f7f`; exact package installed
2026-09-17 03:23 UTC (09-16 local) with game closed. Settings/probe preserved,
landing on/20, crash off/default 5, no recording. RC3 supersedes RC2 below.

**Landing/startup investigation:** first-contact raycasts, visual wheel movement
and the game's all-wheel landing cue are distinct. The saved Norway jump has
first contact 16.67 ms before compression/all-wheel contact; no Haapajarvi
support file or clip is available. Landing timing/gain remain unchanged.
KI-37 removes confirmed default per-force synchronous native log writes via
upstream local candidate `82c789117f115034a074bcb93133fefc8b955e35.clean`
(managed 0.13.1/native 0.6.1, unpublished). Full toolkit sources/tests are upstream;
no consumer native fork. Support now records first-5s/next-10s/later interval
counts at 33/50/100 ms and last landing contact/compression timing. No per-frame
disk writes or shipping recorder. See
`docs/reviews/2026-09-16-landing-startup-investigation.md` and the latest deployment
receipt; no reporter-specific diagnosis or attended sign-off is implied.
Prior candidate: all 16 gates passed for **0.2.6-rc.2**, source
`6f7302595b25e8196e67af2591cf5a15780c82c0`; exact package installed 21:24 UTC.
Settings and probe preserved, game closed, no recording or SimHub change.
Final publication requires official toolkit repin plus attended checks.

**0.2.6 crash candidate:** owner authorized implementing a wheel cue using the
saved crash baseline. `CrashController` passively observes active-player body
contacts; `CrashSignal` uses contact-normal speed with provisional thresholds.
`ImpactController`/`ImpactMixer` exclusively own one finite periodic slot shared
with landing (strongest wins; crash wins ties). Never independently release native
periodics from either feature. Crash defaults off/strength 5/cap 20. Landing
detector/strength, steering curve and motion telemetry remain unchanged. Live
schema-4 collision/feel and SimHub input/output comparisons are pending. Read
`docs/CRASH-EFFECTS.md`, `docs/reviews/2026-09-16-feedback-crash-candidate.md` and
the current `docs/LOCAL-DEPLOYMENT.md` receipt for validation/deployment status.
Public stable remains 0.2.5. New reporter landing timing is KI-36; mild Haapajarvi
startup hitch extends KI-5. TSS works per reporter, but proportional axis travel
is not specifically confirmed. No reporter support file received or reply sent.
Prior candidate: all 16 local gates passed for **0.2.6-rc.1**, source
`2e32256f99514db9a01f72888698726d31f4b2b9`; exact package installed 20:06 UTC.
Settings/probe preserved, landing on/20, crash off/5. UMM shows 0.2.6; support
showed rc.1. Superseded by rc.2 above. Attended tests remain pending.

**First crash baseline:** 2,947 owner-driven force/motion rows replay exactly;
multiple sharp decelerations include a ~145→4 km/h event with nearly zero wheel
steering force. Corpus now has landing and crash cases. Probe 0.2.5.4/schema 4
adds bounded passive `PlayerCollider.OnCollisionEnter` observations, not a new
wheel effect. CLR cannot prepare that game's Unity ECalls; gate the actual patch
with `Run-UnityMono.py` and ProbeHooks' `collision` mode, keeping CLR checks for
the other hooks. Live collision callbacks and SimHub input/output comparison
remain pending. KI-34 fixes post-reset false landing analysis; KI-35 fixes corpus
index replacement using `[NullString]::Value`. Full validation/deployment receipts:
`docs/reviews/2026-09-15-crash-capture.md`. Shipping 0.2.5 remains unchanged.
All 16 rc.11 local gates passed; exact probe 0.2.5.4 installed with old-probe
backup and 13 game/mod/settings hashes preserved. Game closed, no recording.
The rc.11 ZIP was packaging validation only and was not deployed or published.

**Earlier crash investigation, 2026-09-14:** owner confirmed SimHub for motion. Game
`PlayerCollider.OnCollisionEnter` provides a passive observation candidate;
the wheel does not consume its controller rumble. Signals now passes 2,910
assertions including 48 synthetic collision trajectories through sampler and
encoded UDP. No crash feature/probe extension is implemented; no profile or
installed payload changed. Need labelled body collisions and SimHub input/output
comparison. Coordinate periodic ownership before adding a second wheel effect:
landing currently calls `ReleasePeriodics()` for its sole slot. Read
`docs/research/2026-09-14-crash-feedback.md`; do not treat this as a live crash test.

**0.2.5 is published and installed.** All 16 final local gates passed; the
published ZIP/checksum and installed payloads match. Release source/tag is
`c6242a0a163315f7a390d3860c6204b4ca215619`; ZIP SHA-256 is
`8A7F9FC044058B0890F7323138E3DFCBC998B31C67C57E14272D410C6BFCA502`.
No Actions build or SimHub helper. Game closed, SimHub running, no recording.
Default landing strength is 5; owner's saved enabled/20 and SimHub gains remain.
See `docs/LOCAL-DEPLOYMENT.md` for exact receipts and acceptance limits.

**Owner acceptance 2026-09-13 UTC:** owner accepted RC8 and the built-in
30 Hz shaker profile: landings felt better and amplifier CLIP was observed.
Authorized 0.2.5 release and default-on wheel landing vibration (strength 5).
Existing saved opt-outs/strengths remain intact; the owner's installed strength
20 is retained. Preserve the exact acceptance/log evidence separately from the
incomplete full hardware matrix. Official toolkit v0.13.0 is published and
pinned; all five vendored files are identical to the tested local RC8 artifacts.
See `docs/reviews/2026-09-13-release-0.2.5.md` for the current release record.

**RC9 withdrawn:** owner rejected the extra SimHub helper. Removed its source,
plugin/profile and restored the exact previously validated RC8 artifact on
2026-09-13 01:36 UTC. Keep shaker improvements on standard Forza telemetry and
built-in SimHub effects. A separate **Art of Sim Rally - built-in impacts 30Hz**
profile is selected for comparison; original profile and gains are preserved.
Game closed, SimHub running, no recording. Owner subsequently accepted the physical improvement.
See `docs/reviews/2026-09-12-builtin-shaker-correction.md` and deployment receipts.

Landing vibration was implemented and installed in RC8: initially opt-in,
independent 5% strength default (20% cap), three-cycle 25 Hz/120 ms native sine.
Game detector rejects jitter/reset cases and matches the one real landing.
RC8 used local pin `dd0ef20ad0cdaccc7a67f10a707dbd2a27a6efe9.clean`.
That frozen source/archive is now the official toolkit v0.13.0, native 0.6.0.
The native API now has finite bursts and all-effect focus/watchdog stopping.
The official repin is complete; final packaging still rejects local toolkit
pins. See `docs/LANDING-EFFECTS.md`.

First RC8 owner drive on 2026-09-12: eight nonzero landing requests accepted,
including magnitude .20. **Owner clarified wheel FFB is fine; the weak/buzzy
landing report is about the ButtKicker.** Do not retune steering or wheel bursts
to fix that symptom. Built-in SimHub Impacts/Road impacts tuning was subsequently
accepted; current wheel landing controls do not change telemetry.
The complete hardware gate remains pending. Logs are preserved; game is closed, no recording
running. See `docs/reviews/2026-09-12-landing-wheel-test.md`. Do not redeploy while
the game is running. The toolkit interaction review found no ordinary constant-
force call cancelling the burst; stop timing/driver waveform remain unobserved.

RC8 (`b9598f5c...clean`) was installed after all 16 local gates, including the
real drive corpus and 9,044 landing assertions (one recorded event at row 4230).
It includes RC7's fixed-size settings help/headings correction (KI-32) and
clearer separate USB handbrake, logging and smoothing help. Owner subsequently
accepted landing feel; visual scale verification remains pending. Settings and
optional probe 0.2.5.3 were preserved; RC8 defaulted off/strength 5. Game stayed
closed. Stable was 0.2.4 at that point. Exact evidence is in
`docs/LOCAL-DEPLOYMENT.md`; the latest unsent support draft is in USER-FEEDBACK.

**Attended update 2026-09-10:** installed RC5 failed FFB startup (KI-28) and
telemetry gauge clearing (KI-29); Alt+F4 hang with probe remains unexplained
(KI-30). The exact manual gate records failures. Resolve/retest before release.

Follow-up fixes: focused owned-window FFB acquisition with five idle retries,
production cleanup before process-killing menu Quit, and probe 0.2.5.3 control
thread cancellation. The old probe reproduces a Mono cleanup hang in isolation;
corrected listening/reading/queued-command shutdown passes. Local DSS round-gauge
idle bindings were repaired and SimHub reloaded both displays; physical retest
is pending. RC6 (`8ea54775...clean`) and separate probe 0.2.5.3 were installed after
all 16 local gates, including the real drive corpus. Stable was then 0.2.4.
See `docs/reviews/2026-09-11-bug-follow-up.md` and the deployment receipt.

Published stable is **0.2.5**, including strict USB
axis/shifter identity and idle reader recovery (KI-21/KI-23), last-session
diagnostic retention (KI-19), stale-force release (KI-22), deferred binding edits
(KI-24), and idle telemetry connection/disable handling (KI-25). All have offline
coverage and owner overall RC8 acceptance; full hardware testing remains pending. See
`docs/reviews/2026-09-09-overnight-025.md` and `docs/LOCAL-DEPLOYMENT.md` for the
exact validated/installed artifact, hashes and receipts. The Stream Deck Steam
550320 button targets that installation. Stable 0.2.4 and RC7 use toolkit
**v0.12.0**, native **0.5.0**; 0.2.5 uses official v0.13.0/native 0.6.0.
AxleForceCurve@1 steering arithmetic and telemetry layout are unchanged.
Landing vibration is a separate periodic output; no physics or assist changes.

The separate developer probe now records schema-3 motion/contact/suspension
context, with standalone event/force analysis and schema-1/2 compatibility. It
is not shipped or automatically installed. The owner's 2026-09-10 jump drive
confirmed live observation, but the game's normal Quit killed the process before
saving (KI-27); no usable drive capture was retained. Separate probe 0.2.5.2
introduced the quit guard; explicit menu-only saving and an ordinary Unity shutdown save are
verified. The second run was abandoned with zero force rows, excluded from the
corpus. The 21:40 retry was saved explicitly and hash-verified: 8,974 aligned
motion/force rows, one landing matching the owner's note, no delivery rejections.
KI-31's one-unit difference is reproduced by actual Mono's intermediate numeric
precision. Runtime-aware replay now passes without changing any capture bytes;
`results/regression-corpus/index.json` is the first real regression corpus.
New captures include a force-conversion contract; exact delivery checks remain.
Nothing is recording; the game is closed (2026-09-12 follow-up).
See docs/reviews/2026-09-10-first-jump-capture.md. Pause/Stop and verify files before
quitting. Broader signal-ordering and probe-overhead comparisons remain pending. Read
`docs/DEVELOPMENT-CAPTURE.md` before interpreting landing/slide candidates.

Stable 0.2.4 was released after owner RC5 acceptance. Its preserved drive log
has one normal manager initialization and zero initialization errors, connection
failures or exceptions. KI-20's error flood is resolved on this rig; the T300
user's unrelated slowdown remains unconfirmed. The full attended matrix,
motion/shaker comparison and TSS/Fanatec/combined-camera-mod checks remain pending.
Owner acceptance does not mark those cases passed. Published evidence is in
`docs/reviews/2026-09-09-release-0.2.4.md`. "Verified" means confirmed on the
owner's MOZA R12 rig unless stated otherwise.

| Component | State |
|---|---|
| Force feedback | Verified. Front-axle lateral force × pneumatic trail (reference 11,500 N after two retunes), faded out below 12 km/h, re-acquires the wheel after alt-tab. Sign confirmed on a MOZA R12; the MOZA R5 one-sided inversion fixed by user report. |
| Steering fixes, bind-any-device, glyph text fallback | Verified. |
| Shifter (sequential + H-pattern), read directly from the device | Verified by users. |
| Bonnet + bumper cameras | Owner RC6 camera smoke passed; offline handback tests pass. The complete stock/replay/finish transition matrix remains pending. Issue #1 reporter separately says unplugging a PS5 pad resolved their symptom (KI-1/KI-2). |
| Telemetry (Forza format) | Previously verified live with SimHub + ButtKicker. KI-16 units/local-axis corrections now pass offline/encoded UDP tests; changed motion/shaker response requires an attended comparison. |
| **Direct wheel input** (`WheelInput`) | Verified driving on the owner's rig 2026-09-03 after the steering-sign fix (assignment is direction-independent; Flip per channel). Released in 0.2.2. Fanatec user pending. |
| Crash fix (shifter choice after FFB failure), FFB candidate fallback, capability labels | Released in 0.2.2; init verified here, Fanatec user pending. |
| Rewired DirectInput backend switch (`InputBackend`) | **Abandoned** after four attempts. Settings.xml-only experiment. Do not retry — see below. |
| Toolkit adoption | Managed wrapper and AxleForceCurve@1 adopted; explicit FFB selections persist strict GUIDs. Landing uses the finite periodic API. Offline tests and local Mono loading/drive pass; full hardware lifecycle matrix pending. Damper remains unused. |

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
   plausible and completely wrong dashboard. The tests that lock this down now
   live in dbce-wheel-mod-toolkit with the encoder; keep them there.
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
8. **The vendored toolkit binaries are committed, and must stay committed.**
   They are our own MIT artifacts, not third-party ones, so they do not
   compromise rule 2. Without them a clone cannot package. Verify with
   `git check-ignore -v lib/toolkit/native/WheelFfb.dll` returning nothing.

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
- **There is no native build in this repo any more.** The native source moved to
  dbce-wheel-mod-toolkit; build it there and re-pin here. MSVC 14.44 x64 build
  tools and Windows SDK 10.0.26100 with `dinput8.lib` are installed for that
  repo's sake, and `dumpbin.exe` (useful for checking a new pin's exports) is at
  `C:\Program Files\Microsoft Visual Studio\2022\Community\VC\Tools\MSVC\14.44.35207\bin\Hostx64\x64\`.
  `cl.exe` is not on PATH.
- The native DLL logs to `%LOCALAPPDATA%\ArtOfSimRally\ffb.log` **by default** —
  startup/lifecycle/errors remain on unless `DBCE_FFB_LOG=0`. The current local
  toolkit candidate requires `DBCE_FFB_TRACE_FORCE=1` inherited before game launch
  for per-update force samples (cached once). Published 0.2.5 still logs those
  samples by default. This is separate from the mod's DiagnosticLogging checkbox.
  The old `AOSR_FFB_LOG=1` variable no
  longer exists; anything still telling you to set it is stale.
- `vcvars64.bat` prints `'vswhere.exe' is not recognized` on this machine. That
  comes from inside Microsoft's script and is harmless; only a non-zero exit
  code means a real failure.

## Findings that must not be re-derived

- **The game's normal Quit forcibly kills the process.** `ExitGame.Exit` calls
  `Process.GetCurrentProcess().Kill()` in build 17584229. Unity quit callbacks
  cannot be assumed to run. Probe 0.2.5.2 intercepts this menu action before the
  kill; its automatic quit-save path still needs an attended check. For captures,
  pause, STOP and verify saved files before permitting quit (KI-27). Never call
  the game's Exit method in an in-process test; it kills the test host too.
- **`GameEntryPoint.EventManager` is a lazy factory.** Do not call it from mod
  state polling, input or support. Failed menu construction queues ghost replay
  downloads before throwing; catching does not undo the work. RC4 produced
  43,806 failures and a download backlog continuing into driving. Read the existing
  private `eventManager` field via `GameState.ExistingManager`; cache metadata,
  never a manager across scene teardown. KI-20 fixes the code; owner RC5 retest accepted and error flood absent.
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
- **A `.gitignore` rule excluding a directory cannot be undone below it.**
  `lib/` plus `!lib/toolkit/**` silently kept the vendored toolkit untracked
  through all of 0.2.2 — git never descends into an excluded directory, so the
  re-include never matched. `lib/*` is the fix. Check with `git check-ignore -v`,
  not by reading the file.
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
- **art of rally no longer accepts injected keyboard input** (retested
  2026-09-04, build 17584229 with UMM). Virtual-key `SendInput`, scan-code
  `SendInput` (`KEYEVENTF_SCANCODE`) and `PostMessage(WM_KEYDOWN)` all leave the
  title screen sitting on "press any button to start", with the game confirmed
  foreground. An earlier note here said the opposite; it no longer holds, so
  **anything needing a stage driven has to be driven by a person.** Plan for
  that: ship a diagnostic behind `DiagnosticLogging` and read the log, rather
  than trying to automate the UI. If retrying anyway: the x64 `INPUT` struct
  must be 40 bytes (`FieldOffset(32) long pad`) or `SendInput` fails with error
  87, `FindWindow` by title fails so use `MainWindowHandle`, and UMM's panel
  opens over the game at startup (`ShowOnStart` in
  `artofrally_Data\Managed\UnityModManager\Params.xml`).
- **The stage camera is a two-object rig.** `CarCameras` is on the GameObject
  "Stage Camera"; the camera that renders is "Camera Main", **its child**, which
  the game pins at local identity (`CameraManager`'s constructor zeroes
  `localPosition`/`localRotation`). Writing a world-space transform to
  `Camera.main` therefore leaves a local offset on the child that the stock rig
  never clears — a code defect relevant to KI-1, not a confirmed diagnosis of its reporter. Anything touching the camera must restore
  that invariant when it lets go.
- `ilspycmd` (dotnet tool) is installed: `ilspycmd -t <Type> Assembly-CSharp.dll`
  for one type, `-p -o <dir>` for the whole assembly. `Rewired_Core.dll` is
  obfuscated internally but its public API decompiles fine.
- Long heredocs in the Bash tool get mangled (quotes, backslashes, truncation).
  Write scripts to the scratchpad with the Write tool and run them by path.
- Support bundles from users are the fastest diagnosis: the controllers section
  shows Rewired's view, the ffb.log section shows DirectInput's. Compare them.

## Testing

Documentation/installer audit 2026-09-13: current user instructions live in
README/SETUP/TROUBLESHOOTING; dated reviews remain history. The revised installer
accepts literal custom paths, forwards batch arguments/exit codes, initializes
Windows PowerShell's own module path, and can uninstall after UMM removal.
These installer changes are unreleased; the published 0.2.5 ZIP and installed
game payload stay immutable. Docs-only and installer-only work does not require
replacing the already-current game DLLs. Record package validation separately.

**Keep the installed copy current without asking again** (standing owner request,
2026-09-09 UTC). Local deployment is a checklist item when finishing a feature or
bug fix. After its artifact passes all local automated gates, deploy the exact
package if the game is closed, preserving settings and a backup. If the game is
running, record deployment as pending and resume at the next active work session;
never close it or schedule polling. Follow docs/LOCAL-DEPLOYMENT.md. Scheduled
deployment checks are disabled at the owner's request. This authorization does
not grant public publication or attended sign-off.

**Release builds run locally** (owner preference, 2026-09-08). Run the local RC
or final gate, then upload the exact validated ZIP and checksum using `gh release`
when publication is authorized. Do not add or dispatch GitHub Actions builds
unless the owner changes this preference. No Actions workflows were present when
checked. Tag the artifact's recorded source commit and preserve its hashes;
local publishing does not waive attended checks. See docs/RELEASING.md.

Use `tools/testing/Test-Rc.ps1 -Version 0.2.5-rc.N` with a new RC number, or
`-Version X.Y.Z -Final` for a final-labelled artifact; neither grants runtime sign-off.
It explicitly runs consumer arithmetic, save, camera/lifecycle and capture tests,
package/installer checks, and creates an attended checklist. `dotnet test
ArtOfSimRally.sln` still runs nothing and is not evidence. Game/UMM references
are local and never packaged. Native and encoder source tests stay upstream.

Recorder/playback are **development-only**, never shipping features. Capture is
the separately installed `tools/testing/Recorder` UMM probe; external commands
control it and `tools/testing/Replay` works without game/Unity/wheel dependencies.
Packaging rejects recorder types/dependencies. Use `-Corpus <index.json>` on the
RC runner once real captures exist. Synthetic fixtures are not playthrough evidence. Schema 3 adds aligned motion/contact
context; schemas 1/2 remain readable. No physical wheel-motion or calibrated
impact signal is implied by an offline event candidate.

The release requires the **exact packaged artifact** installed with the game
closed and tested by a person. Automated capture replay evaluates force arithmetic
without native hardware output; it does not recreate Unity or drive a stage.
Do not mark camera, stutter or hardware fixes verified based on offline tests.

```powershell
dotnet build ArtOfSimRally.sln -c Release -warnaserror
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
