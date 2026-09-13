# Use SimHub's existing impact effects

The owner rejected adding a SimHub helper for landing thuds. That dependency was
introduced before proving a gap in the existing signals/effects. The correct
path is accurate standard Forza telemetry and ordinary SimHub effect tuning.
RC9 is withdrawn; no new shaker protocol/plugin is required.

## Findings

The installed SimHub code confirms:

- Impacts (`ImpactEffect`) reads `FeedbackData.VelocityDistance`; its sensitivity
  maps changes in velocity to intensity and briefly retains the maximum.
- Road impacts (`WheelImpactEffect`) uses calibrated suspension velocity when
  available, with an orientation fallback. It is distinct from road vibration.
- Jump landing is a separate effect requiring the reader's `LandingEffect`
  capability. Its presence in the effects list alone does not prove this reader
  supplies it; no support is claimed here.
- Our existing telemetry exports local velocity/acceleration, actual suspension
  compression in meters and normalized travel. Do not inflate those physical
  values to create extra shaker gain: dashboards/motion consume the same packet.

[Recorded game signals](../../results/builtin-impact-signal-study.json) around
the known landing show a world-velocity change of 4.42 m/s in one physics step,
and corner compression rates around 4.9–5.2 m/s. Compression appears one sample
after first contact. This confirms an available transient, not the actual
SimHub effect level or physical shaker waveform.

[SimHub's effect guide](https://github.com/SHWotever/SimHub/wiki/ShakeIt-V3-Effects-configuration)
documents independent effect gain, frequency, input response and gain modulation
for brief impacts. Current saved original profile had Impacts enabled at gain 99,
50→44 Hz, and Road impacts enabled at gain 100, 44 Hz. Global gain was 79.6818.
Those saved settings do not establish the amplifier's physical headroom.

## Applied correction

- Reverted the helper's game code, UI/settings, protocol, plugin, packaging and
  companion-only tests. Production source/toolkit/installer/package inputs now
  match the validated RC8 source exactly. Existing wheel landing FFB is retained.
- Restored the **exact RC8 ZIP that passed all 16 gates**, including real-drive
  replay, after verifying its hash. No rebuilt or untested payload substituted.
  [Receipt and RC9 backup](../../results/landing-rc8-install-c9b92ca721ed45d892901e8381a45b75/receipt.json).
  Settings, probe and Steam/Stream Deck target preserved.
- SimHub exited normally. Removed only our helper DLL, activation entry and
  helper profile, with backups. Original profile and all existing gains remain.
- Selected a separate **Art of Sim Rally - built-in impacts 30Hz** comparison
  profile. Only Impacts/Road impacts tone frequencies changed to fixed 30 Hz;
  enable state, gain, sensitivity and all other effects are copied unchanged.
  [Settings receipt](../../results/builtin-shaker-install-receipt.json).
- SimHub restarted; its live property server confirms the built-in profile loads
  and the helper profile is absent. Helper DLL and port 20779 listener are absent.
  [Live check](../../results/builtin-simhub-live-check.json). No nonzero hardware
  output was injected and no stage was driven.

## Next attended comparison

Drive the same jump once with **Default profile**, then with **Art of Sim Rally -
built-in impacts 30Hz**. Standard game telemetry stays enabled; no mod-side shaker
switch is needed. Compare thud character, ordinary bumps and pause/finish clearing.
Check the amplifier CLIP light before raising gain. If it remains weak, measure
the built-in effect output during touchdown to separate input calibration from
audio gain/frequency/mounting. This comparison changes frequency only, so it does
not claim a larger signal or a proven physical improvement.
