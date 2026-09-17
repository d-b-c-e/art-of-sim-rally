# Crash kick — 0.2.6 candidate

**Experimental, off by default; not in the published 0.2.5 download.** The saved
owner drive shows head-on deceleration with almost no steering force. The new
wheel cue addresses that missing response without changing the steering curve.
RC3 and shaped RC4 both failed the owner's feel test (KI-38). RC4 accepted three
full-intensity commands with no early managed stops, but no felt crash effect;
ordinary steering worked. The owner preferred the standalone constant pulse
(method A) at 40%, but requested more strength. The next candidate uses that
route. [Current investigation](reviews/2026-09-17-constant-crash.md).

Pause, open Ctrl+F10 → **Force feedback → Crash kick (experimental)**,
and choose **Crash strength**. New settings default to **50**, with a **0–100**
range independent of steering and landing strength. Existing saved strengths
are preserved; select 50 manually when comparing with the new default.
The pulse requires the toolkit's finite constant-force support. Landing settings
and steering strength are preserved. No SimHub helper is needed.

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
- Requests a finite **120 ms constant-force pulse** in the positive X direction,
  followed by release. This matches method A from the owner's standalone test.
  It is a generic jolt, not collision-derived steering torque or a directional
  simulation of the car's impact.
  Landing retains its 25 Hz, 120 ms, phase-zero sine without an envelope.
  Repeated contacts are suppressed for
  350 ms; a stronger contact within the initial 120 ms may replace the first.
  It can restart that finite burst, but cannot create sustained scrape vibration.
- Allows **one active impact** at a time, using separate cached constant and sine
  handles. Strongest magnitude wins; crash wins a tie. The previous effect must
  stop before its replacement starts. A suppressed cue is discarded, never queued for later.
  Disabling/resetting one feature cannot stop a cue currently owned by the other.
  All output stops for pause, focus loss, finish/replay, device loss or shutdown.

Crash strength 100 requests full nominal force for that effect. It does not
guarantee unused headroom: steering and the crash pulse may saturate together,
and the wheelbase's global gain/filters still apply. Existing numbers retain their
nominal amplitude, but the constant waveform differs from the old crash sine.
Landing stays at default 5, range 0–40, with unchanged detection and waveform.
Crashes remain opt-in. Keep other tuning fixed while comparing 50, then higher
values if needed; physical response above the owner's 40% test is unverified.

Finite duration is checked by reading the driver parameter back before starting.
An unsupported, adjusted or failed request disables crash output until crash is
toggled off/on while paused. The separate landing handle remains usable.
There is no silent fallback to the
old vibration and no retry of a stale impact.
The native layer stops the effect after 120 ms even if Unity stalls.

Support retains the last delivery, native call duration, managed stop reason and
elapsed time after playback returned for each effect. Detail logging adds one
start/reject and one stop record per submitted cue, including immediate steering
output. Timing is command evidence, not measured motor motion. The managed
expiry starts after playback returns, avoiding truncation by a slow native call.

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

**Current acceptance limit:** the owner preferred the standalone constant pulse
at 40%, but has not accepted the stronger range or its in-game integration.
The Desktop tester now starts at 50 and offers manual choices through 100.
See [candidate evidence and deployment](reviews/2026-09-17-constant-crash.md).

1. With crash kick **off**, verify ordinary steering and one jump still feel
   as before. Note stage/car. Drive one front impact and one glancing side impact.
2. Pause, enable crash kick and choose a suitable strength; **50** is the new
   default. The front impact should have a distinct short push/release; the side
   hit should be weaker. Increase gradually if needed. Ordinary braking and restarts should not
   trigger it. A landing/body contact should not double the vibration.
   Keep Pit House and other settings fixed. Existing saved strength is preserved,
   not reset to 50 by installation.
3. Check pause/focus loss, finish, disabling either feature, and quit. Save the
   support file while paused; it reports separate event, accepted/rejected and
   overlap-suppressed counters. Confirm settings survive relaunch.
4. For the next separately recorded drive, include front/side impacts, braking,
   landing and restart. **STOP while paused and verify saved collisions.csv before
   quitting.** Record approximate event times. Compare the same events with
   SimHub input/effect output before changing any motion profile.

See [local deployment](LOCAL-DEPLOYMENT.md) for the exact installed candidate and
validation receipt. No public release or attended sign-off is implied.
See [RC3 evidence and shaped-candidate review](reviews/2026-09-17-crash-kick.md).
