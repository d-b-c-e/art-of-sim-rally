# ButtKicker landing thud — RC9

**Withdrawn at the owner's request on 2026-09-12.** The helper was unnecessary
before investigating SimHub's existing effects. Its code/plugin/profile have
been removed and the exact validated RC8 package restored. This document records
the historical experiment only. Current work uses built-in Impacts/Road impacts;
see [the correction and comparison](2026-09-12-builtin-shaker-correction.md).

The owner clarified that wheel FFB was fine and requested a more distinct
ButtKicker impact. Implemented a separate haptic cue and optional SimHub companion;
no wheel retune or physical telemetry amplification. Stable remains 0.2.4.

## Implementation

- Independent game contact/descent detector, reusing `LandingSignal`, works with
  wheel FFB disabled. Default off; Telemetry / ButtKicker landing thud enables it
  while paused. Shaker strength defaults to 50, range 0..100.
- Nonblocking, fixed 40-byte UDP snapshots to loopback 20779. Original event IDs
  and timestamps prevent repeated packets extending/replaying a cue. Idle,
  focus loss, restart, invalid samples and disable clear it; stale input expires.
- SimHub plugin emits a bounded 0..100 property with a 180 ms envelope: 10 ms
  attack, 30 ms hold, 140 ms squared decay. The profile uses a 30 Hz tone and a
  single mono aggregate, avoiding four summed corner tones. Values are authored
  haptic cues, not calibrated impact force. Plugin owns no audio device.
- Installer backs up settings and clones the current profile. Original profile
  and global gain are preserved; only the new profile disables the three generic
  impact effects to avoid overlap. Engine/gear settings remain. Repeat installs
  preserve user edits to the new profile.

## Evidence

Exact game and companion identity:
`0.2.5-rc.9+c4126ba6eba96e0c12440c3442375783b7a08dff.clean`.

- [All 16 local gates passed](../../results/rc-0.2.5-rc.9-a2ff39c577dd4a1592d426b592d5d376/automated.json).
  Haptics: 583 assertions on .NET and 583 on actual Unity Mono, including UDP,
  bounded shutdown, duplicates, stale traffic, pause/focus/restart and missing
  samples. SimHub's actual importer/save/reload passed 8 checks without opening
  audio; installer preservation/idempotence/corruption passed 19 checks.
- Existing steering arithmetic and real captured-drive replay passed unchanged.
  The saved Norway drive still detects exactly one landing. That old recording
  does not contain this new side-channel or measure shaker output.
- [Game install receipt](../../results/landing-rc9-install-aa1e37b690bd4b5dba9ea2e519d12420/receipt.json):
  exact payloads installed with game closed; settings and separate probe 0.2.5.3
  preserved byte-for-byte. Steam/Stream Deck target unchanged.
- SimHub exited using its normal `-exit` path, then the exact companion was
  installed and SimHub restarted. Backup/receipt:
  `C:/Program Files (x86)/SimHub/PluginsData/ArtOfSimRally-backups/3322e8a779d14e2d8fd736b6e71db8b5`.
  Preserved global gain: 79.68176538908256.
- [Installed-host zero-output smoke](../../results/simhub-rc9-idle-smoke.json):
  live property server confirms the new profile/effect, plugin listening,
  Connected false → true → false, LandingPulse 0, accepted/rejected counters 0.
  Only inactive, zero-magnitude datagrams were sent. No hardware thud was tested.

## Remaining attended test

Launch with the usual Stream Deck button. Keep wheel settings unchanged. Enable
Telemetry / ButtKicker landing thud while paused at strength 50. Repeat the same
jump with the toggle off/on; compare a smaller hop and ordinary bumps. Check for
one short thud, no repeating buzz, and clearing on pause/focus/finish/quit. Inspect
the amplifier CLIP indicator before increasing strength. Export support while
paused, and read the SimHub landing counters after the run.

Physical feel, audio amplitude and amplifier headroom are unmeasured. The game is
closed and SimHub is running; no recorder is active. Existing gauge clearing,
FFB startup/recovery and quit checks still apply. The local toolkit pin is
unchanged and needs official publication/repin before a public release.
