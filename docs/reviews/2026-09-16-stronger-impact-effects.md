# Stronger landing and crash wheel effects — 2026-09-16

Owner requested greater effect headroom and allowed rescaling their saved 20 if
needed. The existing controls already express percent of nominal wheel force;
extending their range preserves that meaning and avoids a settings migration.
The owner then clarified their primary concern was telemetry for motion rigs,
but explicitly tabled that work and chose to put the wheel range out for feedback.
This change is wheel-only; motion telemetry and receiver settings remain unchanged.

Both landing and crash strength now allow **0–40%**, up from 0–20%. At the same
event intensity, 20 requests the same amplitude as before and 40 requests twice
the old maximum. Defaults stay 5, saved opt-outs remain, and experimental crashes
stay off by default. The owner's installed landing 20 is retained. Actual feel
depends on the device/driver and concurrent steering output; this is a requested
periodic-amplitude limit, not a measured combined wheel-torque guarantee.

`LandingFeedback.MaximumStrengthPercent` and `MagnitudeFor` are shared by the
panel, mixer and final submission. This prevents a raised slider being silently
clamped lower by the mixer, or a false tie letting a weaker crash replace a
stronger landing. Malformed finite values are clamped to 40%; invalid/nonpositive
inputs remain rejected. The shared finite 25 Hz/120 ms effect still uses the
strongest cue, crash wins a tie, and suppressed events are discarded.

Detector thresholds, first-contact timing, cooldowns, stop/recovery behavior,
steering, game physics, telemetry and SimHub settings are unchanged. The toolkit
pin remains the RC2 logging candidate (native 0.6.1); no native change was needed.
The owner's ButtKicker clipping does not determine another rig's wheel headroom.

Targeted checks pass 9,248 assertions with the original Norway capture, including
unchanged single-event detection, legacy 5/20 delivery, new 30/40 and half-intensity
40 delivery, invalid values, above-cap clamping, stronger overlap replacement,
non-stacking, no delayed replay, and lifecycle stopping at maximum strength.
Controller tests exercise the game landing path at 5/20/30/40 and crash path at40.
Settings persistence tests now include landing40/crash30 while retaining legacy
defaults/opt-out checks. Offline output is fake-device evidence, not wheel feel.

See [LOCAL-DEPLOYMENT](../LOCAL-DEPLOYMENT.md) for the full candidate gate and exact
installation receipt. No public release or attended acceptance is implied.

All 16 full local gates passed for clean source
`b85c27a735eac7f55661cd84e9f39b0a20d17f7f`. Exact RC3 installed with the game closed
at **2026-09-17 03:23 UTC** (09-16 local); six mod payloads and the second native
copy match. Settings, game assembly, UMM parameters and probe files are preserved.
ZIP SHA-256: `E1C784BC889A720D3C621EF88E258ECEE10471D8665A65C4287293277C18BE15`.
Previous RC2 is backed up. No physical wheel test or recording started.

For an attended test, leave the owner's landing20 initially and confirm familiar
feel. A wheel with spare output range can compare 25/30/40 on the same jump.
For crashes, enable while paused, begin at5 and increase gradually as needed;
compare head-on and glancing impacts. Confirm pause/focus loss stops each cue,
and regular braking/restarts do not trigger it. Save a support file while paused.
Retain the separate Haapajarvi timing/cold-start comparison; this change does not
claim to resolve those reports or increase the ButtKicker's output.
