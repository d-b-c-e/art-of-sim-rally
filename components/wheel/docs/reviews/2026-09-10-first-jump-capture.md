# First saved jump capture — RC5, 2026-09-10 local time

The owner's third attempt produced a saved schema-3 drive: 11,706 frame rows and
8,974 aligned force/motion rows. Explicit Stop completed while the game was idle;
all three CSV hashes matched the manifest, and an independent local copy was
verified. Recording is stopped and the game subsequently exited. The owner said
there was **one jump** in this stage; analysis found one landing candidate.

This is usable diagnostic evidence. Strict force replay fails at one integer
conversion (KI-31), so it has **not** been promoted to the regression corpus.
The previous RC5 startup-FFB and gauge failures remain open.

## Identity and preservation

- Shipping build: `0.2.5-rc.5+43e0b4a2978121e712d20c8f170567b587554bdf.clean`.
- Probe: 0.2.5.2; toolkit v0.12.0/native 0.5.0, unchanged.
- Stage: `Norway_Stage_5_Reverse_Dry_80s`; car: `Car_M1` (Player.log).
- Start 2026-09-11 02:40:22 UTC; Stop 02:43:40 UTC; about 150 seconds of
  force observations within a 198-second recording including menus.
- Original folder:
  `C:/Users/antho/AppData/Local/ArtOfSimRally/dev-captures/20260911-024022-59d5395fc08e4f2cb2012b4f4c729800`.
- [Saved capture receipt](../../results/attended-rc5-retry-2b067b8ccde040cd87f6274d4b0ea19b/capture-saved.json),
  [verified preservation copy](../../results/attended-rc5-retry-2b067b8ccde040cd87f6274d4b0ea19b/preservation.json),
  [diagnostic analysis](../../results/attended-rc5-retry-2b067b8ccde040cd87f6274d4b0ea19b/diagnostic-audit.json).

The copy is under `capture-diagnostic-only` in the evidence folder, not an
approved corpus case. Original files/receipts were not edited. Analysis scripts
and a local plotting dependency directory also remain in ignored evidence.

## Landing observations

![Landing signals](../../results/attended-rc5-retry-2b067b8ccde040cd87f6274d4b0ea19b/landing-signals.png)

| Observation | Result |
|---|---|
| First contact | Force row 4,230; about 70.50 s after driving begins |
| All-wheel airborne interval | 1.083 s |
| Speed at first contact | About 165 km/h |
| First wheel to regain contact | Rear right (contact mask 8) |
| Steering force at that sample | 0 |
| Front contact / nonzero steering returns | Next physics sample, about 16.7 ms later |
| Steering command at next-sample vertical peak | 6,753 / 10,000 nominal units, about 67.5% |
| Maximum compression / travel in first 400 ms | About 0.997 |

The force response is not absent for the whole landing. Rear contact appears one
sample before the front axle resumes its steering load. There is a strong,
separate vertical-motion/compression response: local vertical acceleration
derived from world velocity peaks around 227 m/s² in the next sample. This is
an uncalibrated game-state derivative, not measured physical rig acceleration or
a prescribed impact gain. Do not transfer that raw peak directly to motion output.

All 8,974 motion rows were available; the current validator found no time/reset/
teleport discontinuities and accepted the quaternion/local-velocity projections.
Physics timestamps advanced coherently. This establishes a useful trace, not all
Unity component ordering or a universal landing detector. Eight additional
slide-recovery candidates describe slip changes only, not confirmed tank-slappers.

The evidence supports investigating a contact-gated, bounded landing envelope
with an independent gain, leaving steering weight unchanged. First model it in
the standalone effects lab against this recording and ordinary-road negatives;
choose gain/duration only after a hardware comparison. The capture did not record
received UDP packets or physical wheel torque, so no SimHub-impact improvement is
claimed. A different stage is optional after this baseline is understood.

## Performance and force delivery

There were 8,973 driving frames: p95 18.72 ms, maximum 27.12 ms, no 100 ms+
intervals. Pause/menu frames are excluded. The native wrapper accepted all 8,974
observed force calls, with zero rejections. Startup initialization also succeeded
in this launch. These facts do not establish torque calibration, clear the
intermittent startup defect, or replace probe-off overhead comparison.

## KI-31: strict replay mismatch

The unchanged .NET 8 strict replay fails `offline device magnitude mismatch`.
A separate diagnostic audit found exactly one integer mismatch out of 8,974:
row 5,892 (CSV line 5,894), time 142.03966 s. Replay expects 4,124; the recorded
native call is 4,123. The toolkit and frozen legacy arithmetic agree exactly in
the offline run; maximum replay-to-recorded float deviation is 1.1920929e-7.

The recorded output parses to the float value 0.4123999774456024. Multiplying
that value by 10,000 at double precision gives 4123.999774456024, which truncates
to 4,123. Rounding the product to single precision first gives 4,124. Across all
rows, double-precision multiplication of the recorded output before truncation
matches every observed integer; single-precision multiplication differs once.
This strongly points to intermediate-precision differences between Unity Mono
and the offline runtime, not toolkit adoption changing the force curve. Confirm
the runtime conversion contract before adjusting replay; no tolerance was widened
and no production force change was made to make this capture pass.

Preserve this exact vector for the conversion regression. Corpus promotion is
pending that reconciliation; signal analysis above is explicitly diagnostic.
