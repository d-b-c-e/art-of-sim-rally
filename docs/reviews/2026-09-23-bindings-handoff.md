# Native bindings handoff and compact actions

Owner report and implementation dated 2026-09-22 local / 2026-09-23 UTC.
Starting installed candidate RC14, clean source `8d37db0`.

## Confirmed symptom and cause

From Wheel settings, **Open game bindings** closed UMM and displayed the game's
ControlsSettings panel. Its selection continued its normal pulse animation, but
the screen accepted no mouse or keyboard input; the owner had to Alt+F4 and
relaunch. Current logs showed a normal RC14 load and responsive game process.

The route closed UMM and pushed ControlsSettings in the same click frame. UMM's
close guard correctly retained native-menu ownership until held input and two
neutral frames passed, but its general held-input set includes Rewired UI axes.
A wheel or pedal map can rest non-zero indefinitely, so the barrier never released.

RC15 queues the target panel instead. It waits for only controls that can initiate
UMM interaction—keyboard, mouse or the mod's USB Settings button—to release, plus
two neutral focused frames. Unity joystick buttons, Rewired axes and parked
H-pattern buttons do not gate this explicit transition. The route then resets the
old stock barrier and pushes the already-active native panel. Reopening UMM,
disabling/unloading the mod, scene invalidation or lost focus cancels or delays the
handoff rather than opening later unexpectedly.

## UI follow-up

The owner's RC14 Help screenshot also showed that remaining full-width raised
commands read as section dividers. RC15 uses guidance-left/compact-command-right
rows on wide layouts, compact segmented controls for short choices, and flat links
for Advanced/Show navigation. Narrow layouts may stack and expand actions for
reachability. New settings follow UMM's scale; Auto remains an explicit per-mod
alternative. The owner's saved UMM-scale choice was already true and is preserved.

No Rewired maps, bindings, input backend, wheel/FFB arithmetic, telemetry, camera
behavior, owner tune or toolkit binary changes.

Toolkit documentation commit `76cd02ac030c05cf79b7ed1aa83500d7015911c2`
adopts both findings across consumers: bounded compact commands rather than wide
raised action bars, and a scoped handoff barrier based on the actual initiating
control rather than every mapped game input. It changes guidance only.

## Validation and deployment

Focused validation passes a warning-free Release build,134 SettingsUi assertions
and19 GameBindings assertions. The route fixture covers guarded scenes, no native
open in the initiating frame, held input, lost focus, two release frames, explicit
barrier reset, successful push and cancellation when UMM reopens. The settings
suite confirms mouse/USB Settings hold the handoff while a parked joystick button
does not.

RC15 clean source `b0f8d4b2bc59a8080718d089d701f0bbc140201e` passed all16
local gates at2026-09-23 04:29:15UTC. ZIP SHA-256 is
`7E4D7166F43E04D3C409E80F67FA44FDB9BFC368841C15D98F9BC99D78936B07`.
The exact package was installed04:30:35UTC with the game closed. All six payloads
and the second native copy match; six protected files retain their hashes. See
[Local deployment](../LOCAL-DEPLOYMENT.md) for the gate, backup and receipt.

No game launch occurred after installation. Required attended check: open Wheel
settings from the main Options menu, choose **Open game bindings**, release the
click, then verify mouse, arrows/Return and Back all work. Repeat from Cameras and
confirm F6 can reopen Wheel settings without a latent native-panel transition.
Rendered compact-control acceptance and KI-39/40's broader matrix remain separate.
