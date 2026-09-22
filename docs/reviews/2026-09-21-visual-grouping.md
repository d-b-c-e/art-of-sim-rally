# Simple settings: visual grouping follow-up

Owner request and implementation dated 2026-09-21. Starting source `1a72fd9`,
installed RC12 runtime `cbcf76c`. Shared UX-03-G guidance commit
`673d451a929807d6256277f4ed69d3de7296fed5` in dbce-wheel-mod-toolkit.

## Evidence and decisions

An independent UX reviewer examined source and retained actual RC12 screenshots
under `results/ux-rc12-smoke-d24e3e71d58f498699f4eba1ea8c28f7/`. The default
960×720 UMM host at Auto2x leaves only190px of page body. The Controls screenshot
shows instructions and header consuming most space, with the first axis heading
at the bottom. It does not show the full binding rows: ambiguous button ownership
is established by the owner's report and source's loose label/bar/action layout.

- Each axis or USB button is a shaded group with a bold heading, assignment,
  device input, and its own actions. Gaps between groups exceed internal padding.
- Bind names its target. Compact Calibrate/Clear share that group's action row;
  stacked actions repeat the target. Width decisions subtract card padding.
- Active calibration replaces the idle actions inside the named group. Save,
  Cancel, inversion and deadzone remain in Simple. Detailed saved calibration
  prose is in Advanced. Essential neutral/release instructions appear **before**
  Bind/Calibrate, because clicking samples the resting axis/button state.
- Each camera adjustment groups its keyboard and USB assignments together.
- Optional clutch, shifter, mod and camera shortcut sections use Show/Hide
  disclosures, disabled during edits so an active editor cannot be hidden.
- Routine save status shares the View row where space permits. FFB's detailed
  state remains on its own page; Stop FFB and Close remain in the header.
  The default4K page budget increases from190 to300px. UMM preferences are read-only.
- Setup keeps three meters, one factual instruction and one next action. It
  does not infer connected/ready hardware from a saved assignment.

No binding storage, input sampling, FFB/physics/telemetry policy, camera behavior,
native dependency, owner tune or probe is changed. This is an RC presentation
change; the public0.2.5 instructions/download remain separate.

## Validation and acceptance

Initial Release build passes with warnings as errors;126 SettingsUi assertions
pass. That suite compiles display/input policies, **not SettingsPanel.cs**; it
does not prove GUILayout interaction or visual fit. Independent source review
checked balanced groups, padding-aware action stacking, edit ownership and
disclosure guards. It found the removed neutral/release precondition; the final
implementation restores it for both axes and buttons.

The immutable candidate must pass all16 local RC gates before installation.
The exact candidate, source/package hashes, gate and installation backup belong
in [Local deployment](../LOCAL-DEPLOYMENT.md). Build/deployment results will be
appended below. Game/desktop automation remains paused after the earlier owner
stop. No new screenshot, game launch, wheel input or force acceptance is claimed.

Remaining rendered checks on the actual installed candidate:

1. Default960×720 host at Auto2x: initial Controls shows the first complete axis
   group **including its actions**; Setup shows meters and next action without
   scrolling. Persistent Stop FFB, Close, View and tabs remain reachable.
2. Scroll halfway through adjacent axes. Bind/Clear ownership stays clear;
   long device names and disconnected/error messages wrap without hiding actions.
3. Check720p,4K and explicit UMM scales in both views. Verify actual shading,
   text contrast, focus, scrolling, and no horizontal escape.
4. Bind steering/pedal/analog handbrake/button from neutral; save and cancel.
   Named draft, inversion/deadzone and Save/Cancel stay understandable. Failed
   saves and timeouts remain visible and previous bindings remain effective.
5. Expand camera adjustments and mod buttons; check keyboard/USB ownership and
   inability to hide the active editor. Confirm Close/Escape cancellation and
   stock-menu isolation still behave correctly.

These remain **Not tested** this turn. KI-39/KI-40 are not closed by source review
or an offline policy assertion. Physical controls and force acceptance remain
independent of this visual improvement.

## Built and installed candidate

RC13 source `470e398b89198d60c861ce3ffad800b0ff93854a` passed all16 local gates,
including both recorded drives, at2026-09-22 04:15:01UTC. Independent closure
confirmed no remaining source blocker from the review; final panel SHA-256
`005DEB0119AA3A820E7D775F5E64D7BA80490B770FDC6B2112D7966C55002F5E`.
The exact package was installed04:15:29UTC with the game closed. Six mod files
and the second native DLL match; six owner/game files retain their immediate
pre-install hashes. Current Settings.xml SHA-256 is
`A9291BDBE10AE1DC667A8CA20F48E0EF8D529F11F9F28500284419090F536536`.
The [deployment entry](../LOCAL-DEPLOYMENT.md) links the gate, RC12 backup,
package hash and installation receipt. No public release, desktop interaction
or physical-force test occurred. KI-41 remains open for the rendered checks above.
