# Crash vibration — 0.2.6 candidate

**Experimental, off by default; not in the published 0.2.5 download.** The saved
owner drive shows head-on deceleration with almost no steering force. The new
wheel cue addresses that missing response without changing the steering curve.
Physical detection, timing and feel still need an attended test.

Pause, open Ctrl+F10 → **Force feedback → Crash vibration (experimental)**,
and start with **Crash strength 5**. Strength is independent of steering and
landing strength, with a hard cap of 20% of nominal wheel force. The effect
requires the same sine support as landing vibration. Existing saved controls,
landing settings and steering strength are preserved. No SimHub helper is needed.

## What the candidate does

- Passively observes the active player's body collisions before the game's
  original callback. No damage, grip, assists or car behavior is changed.
- Uses relative speed into the contact surface, rather than total speed, so a
  shallow scrape is weaker than a head-on hit. The first tuning ignores speeds
  of 3 m/s or less and scales to full configured strength at 20 m/s. These are
  provisional thresholds, not values calibrated from live collision recordings.
- Ignores Road-tagged and predominantly vertical contacts, requires recent
  continuous player motion, and disarms across pause, reset, teleport, stale
  sampling, car change or focus loss. At most eight contacts are inspected.
- Uses a finite 25 Hz, 120 ms vibration. Repeated contacts are suppressed for
  350 ms; a stronger contact within the initial 120 ms may replace the first.
  It can restart that finite burst, but cannot create sustained scrape vibration.
- Shares **one** native effect with landing vibration. Strongest magnitude wins;
  crash wins a tie. A suppressed cue is discarded, never queued for later.
  Disabling/resetting one feature cannot stop a cue currently owned by the other.
  All output stops for pause, focus loss, finish/replay, device loss or shutdown.

The 20% cap bounds periodic output, not combined steering torque plus vibration.
Keep ordinary steering settings unchanged during the comparison. This candidate
does not raise the requested landing cap to 30–40 or change its accepted waveform.

## Evidence and limits

The [owner baseline](reviews/2026-09-15-crash-capture.md) has 2,947 force/motion
rows and owner-labelled front/side impacts. A roughly 145→4 km/h event has nearly
zero steering output. It was recorded with schema 3: it contains **no body-contact
normals or callback events**, so it supports adding a separate cue but cannot
validate this classifier's thresholds or attribute each peak to a collision.
Probe 0.2.5.4/schema 4 is installed for that next check.

Automated coverage exercises multiple headings/rates, contact projection, shallow
scrapes, invalid signals, reset/teleport rejection, bounded callback work,
allocation-free detector arithmetic, shared output, failure recovery and stop
paths. Actual Unity Mono checks production/probe hook coexistence without
executing an in-game collision. The real landing case must remain one event at
row 4230. Driver acceptance is not a measurement of wheel movement.

**Motion telemetry is unchanged.** Existing acceleration survives the synthetic
encoded-UDP crash tests; the real drive contains large deceleration peaks. We
still lack simultaneous SimHub received-input and Surge/Sway output evidence.
Do not inflate physical acceleration or change platform limits to compensate for
an unmeasured receiver/filtering issue. Compare SimHub's built-in data/effect
views first, as described in the [motion investigation](research/2026-09-14-crash-feedback.md).

## Short attended comparison

1. With crash vibration **off**, verify ordinary steering and one jump still feel
   as before. Note stage/car. Drive one front impact and one glancing side impact.
2. Pause, enable crash vibration at **5**, then repeat. The front impact should
   have a brief distinct cue; the side hit should be weaker. Increase gradually
   only after checking the low setting. Ordinary braking and restarts should not
   trigger it. A landing/body contact should not double the vibration.
3. Check pause/focus loss, finish, disabling either feature, and quit. Save the
   support file while paused; it reports separate event, accepted/rejected and
   overlap-suppressed counters. Confirm settings survive relaunch.
4. For the next separately recorded drive, include front/side impacts, braking,
   landing and restart. **STOP while paused and verify saved collisions.csv before
   quitting.** Record approximate event times. Compare the same events with
   SimHub input/effect output before changing any motion profile.

See [local deployment](LOCAL-DEPLOYMENT.md) for the exact installed candidate and
validation receipt. No public release or attended sign-off is implied.
