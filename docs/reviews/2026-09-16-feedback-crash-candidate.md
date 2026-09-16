# 0.2.5 feedback and crash candidate — 2026-09-16

The owner supplied a further T300/TSS reporter message and authorized progressing
crash response using the saved evidence. Source work targets 0.2.6-rc.1; public
stable remains 0.2.5. Exact gate/deployment receipts follow validation.

## Feedback findings

- Strong positive steering report on 0.2.5, distinct car feel, and handbrake
  reported working perfectly. The message does not identify axis versus button
  assignment or demonstrate partial travel. Direct axes feed 0..1 into the
  game's existing proportional handbrake torque; buttons remain digital.
- Landing vibration at strength 20 reportedly precedes visible touchdown on
  Finland Haapajarvi. Track as **KI-36**, unconfirmed. The reporter requests
  optional 30–40 strength. Keep the current default/cap while timing is assessed;
  this wheel request is separate from the owner's already-clipping ButtKicker.
- Small hitch in that stage's first five seconds extends **KI-5**. No reporter
  support file received, so no cause or fix is established. Need the original
  file, car/stage/direction and cold-start versus restart comparison.
- A support reply is drafted in USER-FEEDBACK; no reply or GitHub comment sent.
  No payment details supplied or invented.

Landing source still triggers on the first nonzero wheel-contact mask. Local
previously inspected Wheel code uses a downward raycast to suspension travel plus
loaded tyre radius. In the unchanged Norway corpus, row 4230 has rear-right
contact, world descent -9.1866 m/s and zero recorded compression. Row 4231,
16.671 ms later in physics time, has all-wheel contact and 0.08209 m rear-right
compression; row 4232 has compression at all four wheels. Component ordering,
rendering and driver output were not captured together. This supports investigating
first-contact versus load timing; it does **not** prove a 17 ms bug or justify
blindly delaying every landing. Detector timing/waveform is unchanged.

## Crash implementation and limits

[CRASH-EFFECTS](../CRASH-EFFECTS.md) documents provisional detection, one shared
finite output, overlap rules, defaults and the attended test. Production native
and telemetry APIs remain toolkit-owned; the existing v0.13.0 API suffices, so
there is no native fork or repin. No physical telemetry, force curve, SimHub
profile, steering preference or physics change.

The old recording establishes a missing wheel cue; it cannot supply contact
geometry absent from schema 3. The schema-4 callback drive remains pending.
Production and probe collision patches coexist in actual Unity Mono tests;
these tests do not execute a real collision or output hardware force.

Shared-effect lifecycle and synthetic collision cases are part of the existing
Landing executable and full RC gate. The attended checklist now explicitly
covers crash off/on, negative controls, overlap, saved options and the reported
Finland startup hitch. New/legacy settings default crashes off at strength 5;
landing saved values remain unchanged.
