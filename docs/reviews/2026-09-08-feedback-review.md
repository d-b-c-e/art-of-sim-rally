# T300/TSS follow-up implementation — 2026-09-08

The owner authorized implementing related roadmap work while away and requested
a [short reply](../replies/2026-09-08-t300-tss.md). Work continues on
`codex/overnight-improvements`. Stable 0.2.3 and its Stream Deck install remain
intact. The previous RC2 is immutable; new changes require another candidate.

## Findings and changes

| Area | Result | Evidence / limits |
|---|---|---|
| T300 rotation | A later manual 700-degree setting persists; no angle override added. | Reporter observation; physical travel and initial driver response remain unknown. |
| Handbrake | Existing analog path retained. Support includes latest normalized input and binding/device state; prior RC2 Flip/reopen fixes retained. | TSS hardware test still needed. A digital controller binding does not determine the underlying float brake path. |
| Steering assist | Corrected misleading help to describe the legacy spawn-only boolean and Direct-steering dependency. | No saved game setting or assist behavior changed. "20" is not an identified equivalent of this boolean. |
| Rare FPS drops | New opt-in frame interval counts split first 15s/later in each foreground driving segment; max and last-hitch timing. | No timeline/input recording; zero allocation measured across 100,000 counter updates. No culprit or performance fix claimed. |
| Support logs | Bounded recent file reads; recent native lifecycle/errors retained; command counts no longer claim measured/accepted torque or prescribe stronger force. Collect only while not driving. | 2 MiB / 4,096 lines per snapshot, 2,048 chars per line; displayed truncation. Live device inventory replaces reliance on an old log header. |
| CameraMod compatibility | Suspend our mounts/tuning when UMM has loaded CameraMod; explain in panel and preserve saved settings. | Actual consumer harness reproduced displaced slots; both initialization orders now pass. Requires combined-mod screen test. |
| Telemetry units/axes | Actual compression meters and normalized ratio; local motion vectors; reset acceleration across discontinuities. | 1,230 math/encoded-UDP assertions. Corrected units/axes change motion/shaker response; attended comparison mandatory. |
| Toolkit send failure | Observe the shared sender's false return and stop repeated attempts on a dead socket. | Closed an actual UDP transport under the production consumer; 36 transport assertions now exercise failure without manually invoking its error handler. |

## Camera compatibility source

The [Nexus page](https://www.nexusmods.com/artofrally/mods/1) identifies CameraMod
0.3.1 and links its source. Audited upstream commit
`16f7911e6bf6bf66e9a7552846e0eeb28bdf522a`:
[Main.cs](https://github.com/thoxx/aor-camera-mod/blob/16f7911e6bf6bf66e9a7552846e0eeb28bdf522a/Main.cs)
appends two cameras, then reads/writes settings at slots 8/9 and edits by enum
index. [Settings.cs](https://github.com/thoxx/aor-camera-mod/blob/16f7911e6bf6bf66e9a7552846e0eeb28bdf522a/Classes/Settings.cs)
uses numpad keys overlapping ours. Our reference-based camera identification
cannot protect those external index-based writes. Therefore the candidate lets
one camera system own the rotation. It does not claim both editors can coexist.
Loaded state is used deliberately: disabling the other mod need not remove views
it has already added; switch camera systems with a fresh game launch.

This is independent of GitHub #1's reported PS5-pad workaround. Neither the camera
conflict nor the user's unmeasured video proves the cause of rare FPS drops.

## Validation and remaining work

Individual checks: production warnings-as-errors build, 182 camera tuning,
90 direct input, 36 lifecycle/compatibility, 25 support and 1,230 signal assertions
passed. The full candidate report will identify its exact archive and source.
Offline tests do not establish Unity rendering, physical torque, actual TSS travel
or SimHub motion amplitudes. Use [TEST-DRIVE.md](../TEST-DRIVE.md).

FFB `AxleForceCurve@1`, native driver, toolkit pin v0.12.0 and tuning defaults stay
unchanged. FR-2 separate effect gains and KI-6 snapback remain a captured-signal
and attended tuning task; the earlier [effects study](../research/2026-09-08-wheel-signals.md)
found that simply inserting shared mixer headroom changes steering even without
an event. No speculative kick/damper preset or physical force output was added.
