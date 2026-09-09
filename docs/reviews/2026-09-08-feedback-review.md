# T300/TSS follow-up implementation — 2026-09-08

The owner authorized implementing related roadmap work while away and requested
a [short reply](../replies/2026-09-08-t300-tss.md). Work continues on
`codex/overnight-improvements`. Stable 0.2.3 remains published; the owner requested
local installation of RC4 on 2026-09-08 (2026-09-09 UTC).
**0.2.4-rc.4 passes all 16 automated checks**; attended tests remain pending.
The previous RC2 and failed RC3 run remain immutable historical evidence.

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
passed. The full RC4 gate also passed all 16 checks, including 1,266 telemetry,
450,194 regression and 218 combined lifecycle/camera assertions, package/installer
validation and developer-recorder exclusion.
Offline tests do not establish Unity rendering, physical torque, actual TSS travel
or SimHub motion amplitudes. Use [TEST-DRIVE.md](../TEST-DRIVE.md).

### Exact candidate and handoff

- [ArtOfSimRally-0.2.4-rc.4.zip](../../dist/ArtOfSimRally-0.2.4-rc.4.zip).
- Identity: `0.2.4-rc.4+38c1bff31ff1ca20696c15a8b7de9298ec04dbf3.clean`.
- ZIP SHA-256: `C5284CDF7F0B2991F8E013246857A414CD15FADCCE583AD54F923251B8F47887`;
  independently rehashed after the gate and matched its report.
- [Automated report](../../results/rc-0.2.4-rc.4-744b3572e72e4404a229e5d10fbec92e/automated.json):
  passed 2026-09-08 20:11:12 UTC, clean source, synthetic evidence only, no recorded corpus.
- [Attended checklist](../../results/rc-0.2.4-rc.4-744b3572e72e4404a229e5d10fbec92e/manual.json):
  all seven cases pending. The release gate correctly rejects it with
  `NOT READY: Tester/rig not recorded`; no prior smoke results were copied in.
- Installed 2026-09-09 02:17:23 UTC at the owner's request, with the game closed,
  using this exact archive's installer. All six mod payloads and the second native
  plugin copy match its manifest. Settings.xml is unchanged byte-for-byte;
  the previous 0.2.3 install and settings are backed up.
  [Install receipt and backup location](../../results/rc4-install-7bf2d9692b304b58bbe124f80d43cc4b/install-receipt.json).
- The existing Steam 550320 Stream Deck launcher now opens the RC4 installation.
  No game was launched or driven during installation; RC4 is not published.
  The developer probe remains absent (only its historical cache file remains).

These archive/report links are generated local artifacts outside Git. Later
documentation commits do not rebuild or change RC4. This exact archive is now
installed for attended evidence. Prioritize combined
CameraMod views, corrected telemetry with SimHub/shaker/motion, input Flip/reopen,
camera-key persistence and a paused support bundle after a diagnostic drive.

RC3 stopped at the developer probe's actual Harmony attachment test: putting
Unity timing/focus ECalls directly in the patched watchdog Update broke CLR hook
generation. Moved that sampling behind a non-inlined runtime helper; the actual
14-assertion attach/observe/unpatch test and lifecycle suite now pass. No gate was
skipped or weakened. The failed run remains under
`results/rc-0.2.4-rc.3-66b66f482e2b41de9fc750cdd78b8ff1`; packaging had not started.

FFB `AxleForceCurve@1`, native driver, toolkit pin v0.12.0 and tuning defaults stay
unchanged. FR-2 separate effect gains and KI-6 snapback remain a captured-signal
and attended tuning task; the earlier [effects study](../research/2026-09-08-wheel-signals.md)
found that simply inserting shared mixer headroom changes steering even without
an event. No speculative kick/damper preset or physical force output was added.
