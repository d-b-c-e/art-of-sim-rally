# Roadmap

Status reviewed 2026-09-09 for 0.2.4 publication.
**0.2.4 is published and installed after RC5 acceptance. The detailed attended matrix remains pending.**
The defect register is [KNOWN-ISSUES.md](KNOWN-ISSUES.md); the implementation review
is [2026-09-06-rc-review.md](reviews/2026-09-06-rc-review.md).

The next implementation session is ordered in [OVERNIGHT-QUEUE.md](OVERNIGHT-QUEUE.md).
The post-0.2.4 queue prioritizes USB identity/reconnect investigations, retaining
diagnostic summaries after exit, a focused lifecycle/performance review, and
development capture of the signals needed for independent steering/impact effects.
These are queued work, not additional verified bugs or completed features.
[USER-FEEDBACK.md](USER-FEEDBACK.md) records the T300/TSS report, camera-key
remapping request and the post-release GitHub audit with unsent support drafts.
Implemented for the **0.2.4 release**: camera-tuner save retry/logging (KI-14),
keyboard remapping (FR-1), and analog-handbrake regression coverage (KI-13) with
cache/Flip/assignment fixes (KI-15). Camera and input harnesses pass offline;
RC4 was installed and driven; overall feedback was good with a strong stutter.
[KI-20 investigation](reviews/2026-09-09-rc4-stutter.md) corrected a lazy-manager
polling defect in RC5. Its owner retest was accepted and the error flood was absent; full attended checks remain pending. Rotation and effect-signal
investigations are documented in [the signal audit](research/2026-09-08-wheel-signals.md).
The [follow-up implementation](reviews/2026-09-08-feedback-review.md) adds
CameraMod isolation, bounded support logs/frame-hitch counts, clearer assist help,
telemetry sampling correction (KI-16) and actual failed-send handling. These have
offline coverage; motion/shaker and combined-camera-mod tests remain pending.
New FFB effects and RWD damping remain a separate captured-signal/attended tuning
cycle. No rotation override is justified by the current T300 evidence.

## Now — follow up on 0.2.4 and capture real signals

Owner RC5 testing was accepted for 0.2.4; earlier 0.2.3 RC6 feedback also reported
working cameras and controls. These are scoped results, not a complete matrix. Implemented;
still requiring the complete attended checks:

- Camera child-transform restoration, including explicit replay/cinematic
  handback, mod/feature disable and restoration of a usable stock angle.
- Deferred learned-axis saves, failed-write retry and output release before
  persistence at shutdown. The reported 10–15 second stutter remains unconfirmed.
- Support build identity, observed native module paths/hashes and read-only
  native version diagnostics. A Plugins/x86_64 copy is an intentional install path.
- Version-consistent packages, full payload integrity checks, and settings-preserving
  install/uninstall. The synthetic installer tests do not exercise UMM's GUI.
- Telemetry recovery after a destination failure; loopback regression covers
  reconnect, parking and shutdown. Live consumer testing remains pending.

Development tooling is separate: bounded capture probe, external Start/Stop,
game-free force replay, a saved regression corpus and the release evidence gate.
No recorder ships in the release mod. A local drive log confirms loading and force
evaluation, but no completed capture was saved. The first real game case needs to be
captured; generated fixtures validate the tooling, not a playthrough.

Run [PRE-RELEASE-TESTING.md](PRE-RELEASE-TESTING.md). The release needs the exact
packaged candidate tested for camera transitions, cold/repeated/new-stage stutter,
FFB lifecycle, binding persistence, telemetry, support identity and the normal UMM
upgrade path. Offline tests are useful evidence, not a replacement for those cases.

Issue #1 is not proven to be the camera code defect: the reporter says removing
a USB PS5 controller resolved it. Their support snapshot also binds ChangeCamera
to `Accelerator -`. Preserve that distinction and test with/without the pad where
available. Fanatec-specific confirmation and RWD wheel snap reports remain open.

## Toolkit adoption: implemented, awaiting attended validation

The pin is **toolkit v0.12.0**, native component **0.5.0**, downloaded from the
[official release](https://github.com/d-b-c-e/dbce-wheel-mod-toolkit/releases/tag/v0.12.0).
All five vendored files match its independently hashed archive.
Production references and packages `Dbce.Wheel.Ffb.dll`: the shared wrapper handles
loading, enumeration, FFB, shifter and direct-input reads. The native alias remains
`UnityForceFeedback.dll`, bound from one exact module handle.

Shared `AxleForceCurve@1` implements the released gain, fade, clamp, then EMA
pipeline. Local ForceCurve is only a forwarding adapter. The original formula is
frozen in consumer regression with the game's actual managed Mathf. Existing
`simlite@2` remains a different tune; it is not the adopted pipeline. No new damper,
periodic effect, assist or physics behavior is enabled.

New explicit FFB picker choices persist an instance GUID. Missing GUIDs report a
setup error and never select another wheel. Existing name/index settings remain
readable. Reader handles are refreshed when switching FFB devices. Transactional
sync resolves KI-9. Schema-2 captures record reset boundaries and the force library
hash. Replay compares original and toolkit outputs with separate filter histories,
checks device integers and reports frame timing. See [TEST-DRIVE.md](TEST-DRIVE.md).

Next after a tested release: investigate RWD snapback (KI-6), then consider bounded
damper/road/impact effects in a separate tune/test window. Generic calibration
abstractions and deterministic Unity input playback remain future work. Game hooks,
UI, camera and telemetry sampling stay here; native lifecycle, encoder and force
arithmetic belong upstream.

## Later

- Fanatec and Thrustmaster follow-up using identified builds and support bundles.
- Per-wheel starting settings once enough reports exist. Keep the successful
  R12 and Reddit-user settings as observations, not universal defaults.
- Consider Logitech-specific effects only after generic condition/periodic effects.
- Nexus distribution after the GitHub release is validated.
- Full game input record/playback only after a separate Unity/Rewired state and
  determinism design. MAME `.inp` playback is not portable to this game.

## Explicitly out of scope

Cockpit view, physics/assist changes, redistributing game assemblies, XInput wheel
workarounds that hide DirectInput, and runtime switching of Rewired's input backend.

## Shipped

Kept for the reasoning, not the status. Dates are when the work landed.

| Phase | What | Landed |
|---|---|---|
| 0 | **Prove the force feedback path.** Answered 2026-08-31: the DLL never loaded because the `ForceFeedback` behaviour is never attached — the feature was built from both ends and never joined in the middle. Route A (supply the missing DLL and let the game drive it) was dead on arrival; route B (compute the force ourselves) is what shipped. | 0.1.0 |
| 1 | **The Unity Mod Manager mod.** `Info.json`, entry point, Ctrl+F10 settings, Harmony hooks on the car's physics update. | 0.1.0 |
| 2 | **Telemetry against the real game.** Forza Horizon 324-byte packet, anchored on `Speed`@256 and `Gear`@319, verified with SimHub and a ButtKicker. | 0.1.0 |
| 3 | **Force feedback worth having.** `Mz` first, then replaced by front-axle lateral force × pneumatic trail after `Mz` proved to reverse sign mid-corner. Low-speed fade, sign settled across three wheels, re-acquire after focus loss. | 0.2.1 |
| 4 | **Bonnet camera**, and a bumper view beside it, appended to the game's own rotation with a numpad tuner. | 0.2.0 |
| 5 | **Make it shareable.** Public repo, MIT, tagged releases with a double-click installer, user-facing README and troubleshooting. | 0.1.0 → 0.2.2 |
