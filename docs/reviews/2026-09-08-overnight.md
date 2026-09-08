# Overnight implementation and candidate handoff — 2026-09-08

**0.2.4-rc.2 passes all 16 automated checks and is ready for attended testing.**
It has not been installed into Steam, driven or published. Stable 0.2.3 remains
installed behind the existing Stream Deck key. Work is on
`codex/overnight-improvements`; [the queue](../OVERNIGHT-QUEUE.md) records scope.

## Implemented and reproduced

| Change | Evidence | Still requires a person |
|---|---|---|
| Camera save recovery, KI-14 | Reproduced 0.2.3 falsely logging success after a locked-file failure. Deferred saves now retry while idle across view changes and shutdown; removed per-frame tuning logs. | Adjust a mount, change to a stock view, pause, quit and check persistence on relaunch. |
| Camera keyboard remapping, FR-1 | All 11 existing actions can be rebound or cleared; cancel, duplicate/chord rejection and numpad defaults. Existing XML fields retained. Tuning is suppressed in the panel and until capture keys are released. | Actual UMM key capture, bumper-only configuration, stock/replay/finish cameras and other camera mods. |
| Direct-input recovery and Flip, KI-15 | Reproduced stale pedal values after failed reopen, invalid endpoint inversion and assignment from a failed baseline. Fixes retain neutral output until valid input, invert pedal endpoints, preserve flipped steering on restart and require a valid assignment baseline. | Pedal/steering Flip and double Flip, restart, reader reconnect and normal controls on the rig. |
| Handbrake support, KI-13 | New live normalized values. Followed the game's float handbrake through to proportional wheel friction torque; no physics patch. Actual consumer tests cover partial pulls and release. | TSS rest/quarter/half/full/release and gameplay behavior; synthetic inputs do not certify that hardware. |

## Exact artifact and automated evidence

- [Candidate ZIP](../../dist/ArtOfSimRally-0.2.4-rc.2.zip).
- Source: `43334e2ad627674efc71ee4e6a18d65d207619a3`, clean when packaged.
- Identity: `0.2.4-rc.2+43334e2ad627674efc71ee4e6a18d65d207619a3.clean`.
- ZIP SHA-256: `D2ADF06B614E0925B8C5AA04B54B418713EF7DDD1AE05AB03F4401A40B760CF6`.
- Completed 2026-09-08 06:10:25 UTC using `Test-Rc.ps1 -Version 0.2.4-rc.2`.
- [Automated report](../../results/rc-0.2.4-rc.2-ee7891e97a444aa08e82bffd07746eb7/automated.json).
- [Attended checklist](../../results/rc-0.2.4-rc.2-ee7891e97a444aa08e82bffd07746eb7/manual.json).

| Automated coverage | Result / limits |
|---|---|
| Production build | Warnings-as-errors build passed. |
| Consumer regression | 450,169 assertions, including 90 new actual WheelInput assertions against fake transport/game boundaries. |
| Camera/lifecycle | 209 assertions: 182 actual camera tuning/key/save checks plus 27 lifecycle checks. Locked-file tests exercise the real settings writer. Unity rendering is not simulated. |
| Force reference | All 703 reference rows unchanged; pinned toolkit and production force controller/curve are unchanged from 0.2.3. |
| Telemetry transport | 34 loopback/recovery assertions. This tests connection lifecycle, not correctness of every sampled physics field; see KI-16. |
| Development capture/replay | Probe, standalone replay, rejection and evidence gates passed. No real recorded corpus supplied; `syntheticOnly=true`, `runtime=pending`. |
| Package/installers | Native exports, payload identity, compiled recorder exclusion, fake-install upgrade/removal and corrupt-package rejection passed. Source remained stable throughout. |

The actual attended gate correctly rejects this blank checklist with
`NOT READY: Tester/rig not recorded`. Owner smoke results for **0.2.3-rc.6** are
historical and are not copied onto this candidate.

RC1 also passed; RC2 only corrects the packaged camera guide and identifies the
older smoke-test artifact explicitly. Both archives and their evidence remain
immutable. Later documentation commits do not rebuild RC2. Local ZIP/results
links are generated evidence outside Git and need retaining on this machine.

## Research completed and next work

The [wheel signal audit](../research/2026-09-08-wheel-signals.md) contains source
references, limitations and the T300 A/B procedure.

- **T300 rotation, KI-12:** no explicit physical-angle request in the pinned
  native initialization/read paths. A driver/profile response is still possible;
  cause remains unconfirmed without that hardware. No upstream change justified.
- **Light steering plus impacts, FR-2:** standalone managed EffectsLab passed
  10,242 assertions over 4,848 synthetic rows, with no native output. Shared mixer
  headroom reduces steering even without an event (0.8 becomes 0.6 with 0.25
  headroom). Integration needs explicit tuning and captured event signals.
- **Telemetry sampling, KI-16:** found maximum travel sent as actual travel,
  meter-valued compression sent as normalized travel, and world motion sent where
  local vehicle axes are expected. These consumer defects remain open. Next code
  work should add focused travel/projection/discontinuity tests, then correct
  sampling and compare recorded SimHub/motion output before release. Encoder
  offsets and the current production telemetry path were not changed here.
- **RWD snapback, KI-6:** needs a real force/slip/wheel-motion capture before
  changing damping or smoothing. No force tune changed overnight.
- **GitHub:** refreshed issue and comment audit; still only
  [issue #1](https://github.com/d-b-c-e/art-of-sim-rally/issues/1) and its existing
  PS5-unplug follow-up. Support drafts remain in [USER-FEEDBACK.md](../USER-FEEDBACK.md);
  no replies posted or issue state changed.

## Next attended session

Follow [TEST-DRIVE.md](../TEST-DRIVE.md), including its extra 0.2.4 checks. Install
this exact ZIP with the game closed before testing it. Prioritize camera key
capture/persistence, pedal and steering Flip across restart, and neutral input
after reconnect. Then exercise the existing camera, stutter, FFB lifecycle,
telemetry, support identity and upgrade checks. Capture once with the separate
probe; remove it for final checks and retain the capture for future RC runs.

The stable install was rechecked against its release receipt: six mod payload
files, the native plugin copy and Settings.xml match. Stream Deck still launches
Steam app 550320 into **0.2.3**. The developer probe's loadable DLL and Info.json
are absent; only a historical cache file remains. [Verification receipt](../../results/overnight-2026-09-08/stable-install-verification.json).
