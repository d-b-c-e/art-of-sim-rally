# Local deployment

The owner gave standing authorization on 2026-09-09 UTC to keep the installed
copy current. Local deployment is a required checklist item when finishing a
feature or bug fix. After its candidate or final artifact passes the complete
local automated gate, deploy it for testing without asking again. Public release
publication and attended sign-off remain separate.

## Installed candidate — 0.2.6-rc.14

Installed **2026-09-23 03:59:32 UTC** (September22 local), game closed, after
all16 local RC gates. This successor responds to the owner's RC13 screenshots
with bordered higher-contrast cards, flat tabs with a selected underline, five
core pages without redundant Setup, smaller header/row commands and guidance to
their left. Old saved page values retain their meaning. No game was launched;
actual rendered fit and interaction remain pending (KI-41 and existing KI-39/40).

- Identity: `0.2.6-rc.14+8d37db001aea2ff21ca30455723f604523d78ac4.clean`.
- ZIP SHA-256: `68E956B06FC421D2A6C0215BF885033A22268A75DB252407D394EC26A35E7EFA`.
- [Complete gate](../results/rc-0.2.6-rc.14-84916e9de11f4b7bac505ba5118892b8/automated.json),
  SHA-256 `E6677D44D96675E5455BF2C6642DD43A839D7ED2214860C88E32285CAB23F548`.
  Includes130 settings policy assertions, both recorded drives and9,507 recorded
  landing/crash assertions. The suite compiles the mod but does not render Unity IMGUI.
- [Install receipt and RC13 backup](../results/ux-0.2.6-rc.14-install-3de661486d8f4ce8981448ab5cd196a8/receipt.json).
  All six mod payloads and second native plugin match. The six current owner/game
  files are byte-identical before/after installation.
- Settings SHA-256 `A9291BDBE10AE1DC667A8CA20F48E0EF8D529F11F9F28500284419090F536536`;
  crash on19.52381 and landing on20. UMM preferences and probe0.2.5.4 retained.
  Toolkit binaries358add5/native0.8.0 remain unchanged in both plugin locations.
- Shared UX guidance `be5519541820ce3408953588d78f16e78794f1d6`; no physical-force
  test or public release.

## Previous candidate — 0.2.6-rc.13

Installed **2026-09-22 04:15:29 UTC** (September21 local), game closed, after
all16 local RC gates and independent source review. Binding groups and local
preconditions, camera keyboard/USB ownership, Show/Hide disclosures and compact
header/Setup address the owner's clutter report. No game was launched; actual
rendered fit and interaction remain pending (KI-41, plus existing KI-39/40).
See the [visual grouping review](reviews/2026-09-21-visual-grouping.md).

- Identity: `0.2.6-rc.13+470e398b89198d60c861ce3ffad800b0ff93854a.clean`.
- ZIP SHA-256: `E9F37818C82D957DE67EC5E039BEF89AE6FDD55B872FFFB4397993E26297A058`.
- [Complete gate](../results/rc-0.2.6-rc.13-56173d7bf1914b7aaa76da395dbc7bbe/automated.json),
  SHA-256 `BC85F5E0561CF7FA3B6C09EEE689BE82BE6C066D3B77BD374DD988FBE5E0E61D`.
  Includes126 settings policy assertions, both recorded drives and9,507 recorded
  landing/crash assertions. The settings suite does not render the panel.
- [Install receipt and RC12 backup](../results/ux-0.2.6-rc.13-install-f013528bf9c24b15bbf08ca587f0264e/receipt.json).
  All six mod payloads and second native plugin match. The six current owner/game
  files are byte-identical before/after installation. This preserves the owner's
  latest settings rather than restoring the older September19 snapshot.
- Settings SHA-256 `A9291BDBE10AE1DC667A8CA20F48E0EF8D529F11F9F28500284419090F536536`;
  crash on19.52381 and landing on20. UMM preferences and probe0.2.5.4 retained.
  Toolkit358add5/native0.8.0 remains unchanged in both plugin locations.
- Shared visual guidance `673d451`; no physical-force test or public release.

## Previous candidate — 0.2.6-rc.12

Installed **2026-09-19 20:35:39 UTC**, game closed, after all16 local gates and
independent source closure of the custom-scale row overflow. RC11 was validated
but not installed. RC12's limited default4K menu check passed; the full rendered
and hardware matrix remains pending. No public release.

- Identity: `0.2.6-rc.12+cbcf76c5d6401dd8a0c511acfe7ded201df6a99d.clean`.
- ZIP SHA-256: `88A9B71D43F4A0788112CC987EEEB02407BCB43F9D6AECDA8BCAC93F1927B0C4`.
- [Complete gate](../results/rc-0.2.6-rc.12-bc652082c30b43dba419e09883d67c42/automated.json),
  SHA-256 `9FC72450E3F8306FE9AF44FC52AA112E07A62008D6D70523073B3F5ED3E32856`.
- [Installation receipt and RC10 backup](../results/ux-0.2.6-rc.12-install-787e550c1c6b4fa2bc307e3efb32d5ae/receipt.json).
  All six mod payloads and second native DLL match. Settings, UMM, game assembly
  and probe hashes are unchanged. Crash on19.52381, landing on20; settings SHA
  `C373DA18429788ACDFEF7A032496981BDCC5571D7285543B249769846E5876EF`.
- 126 settings assertions,13 Unity Mono hook/host-size checks, both recorded
  drives and9,507 landing/crash assertions pass. Toolkit358add5/native0.8.0 and
  probe0.2.5.4 unchanged. Exact artifacts/evidence155files hash-verified in main.
- The [smoke/correction review](reviews/2026-09-19-rc10-ui-smoke.md) distinguishes
  RC10's narrow keyboard pass from its failed layout and RC12's limited layout pass.
- [RC12 smoke and restoration receipt](../results/ux-rc12-smoke-d24e3e71d58f498699f4eba1ea8c28f7/receipt.json):
  readable header/all six page buttons and five Simple page tops fit the default
  4K host; Escape closes only the panel and subsequent native Quit works.
  Normal exit and all16 protected hashes restored at20:51:49.521UTC; no output
  or recording. The remaining camera check followed in a second run.
- [RC12 camera cancellation and restoration receipt](../results/ux-rc12-smoke-74c19cbf1bf5462ead1dad05bbdaff67/receipt.json):
  Cameras and nested scrolling fit; Bind→Escape at8.218s canceled the active edit
  without closing the panel or activating native Quit. Advanced Cameras top
  was observed. Normal exit and all16 protected hashes restored at21:05:25.672UTC.
  Comparison of original/after-smoke XML found only the intentional temporary
  FFB/telemetry Off changes; bindings and force strengths were unchanged. Both
  original files are restored byte-for-byte. Full Advanced/scale/input coverage
  remains pending; no output, driving or recording occurred.

Before driving, choose **F6 → FFB → Use steering wheel** or reselect the MOZA;
the saved old name/index lacks an FFB GUID. The installer preserves that choice.

## Previous candidate — 0.2.6-rc.10

Installed **2026-09-19 20:10:28 UTC**, game closed, after all16 local gates.
This replaces RC9's confirmed menu leakage and small4K content with reviewed
dispatch/held-release guards and scoped Auto/UMM text scaling. Live keyboard
isolation/recovery passed, but default4K layout clips controls (KI-40); see
[RC10 smoke](reviews/2026-09-19-rc10-ui-smoke.md). A layout correction/retest is
pending; this is not a public release or complete UX-1 acceptance.

- Identity: `0.2.6-rc.10+2ab6fb5f27be570693ebe856b48259456f55007f.clean`.
- ZIP SHA-256: `F4A46410F5B8F9CD82121C8B183BC161E2788BB47E6F0A1BFF222FC7AD89B9E6`.
- [Complete gate](../results/rc-0.2.6-rc.10-49b815102d9e4f43962f90e1eb0f4d04/automated.json),
  SHA-256 `96B492FFDACB6CCA3BB42B0AE1A404F94ADFFA2AEC8E4B8CD34D490E422B617F`.
- [Install receipt and RC9 backup](../results/ux-0.2.6-rc.10-install-073c05f30a374727b3a29ac3ac50046a/receipt.json).
  All six mod payloads and second native DLL match; settings, UMM preferences,
  game assembly and separate probe are byte-identical. Crash on19.52381,
  landing on20; settings SHA `C373DA18429788ACDFEF7A032496981BDCC5571D7285543B249769846E5876EF`.
- 96 settings checks, actual Unity Mono hook attachment, two real captures and
  9,507 landing/crash assertions pass. Saved landing remains row4230. Toolkit
  358add5/native0.8.0 and probe0.2.5.4 unchanged; no recording or force test.
- Exact artifacts, gate/failed-first-gate and installation evidence are copied
  and hash-verified in main's ignored dist/results. Public stable remains0.2.5.

Before the next drive, explicitly select **F6 → FFB → Use steering wheel** or
reselect the MOZA: the saved old name/index has no FFB GUID. Installation does
not guess an output device. See [RC9 failure and successor review](reviews/2026-09-19-rc9-ui-smoke.md).

## Previous candidate — 0.2.6-rc.9

Installed **2026-09-19 19:31 UTC** with the game closed after all 16 local gates.
RC9 aligns the installer's completion text with the new F6/FFB/Controls/Help
paths; runtime source is unchanged from RC8. It is not a public release.

- Identity: `0.2.6-rc.9+44e11dd7a7c0e6dd99cef7f2c8be0620e9ce717a.clean`.
- ZIP SHA-256: `8D92D7758AD25F8F62A870A4275B72BE685C533D79FF3976A98FE17EDA3E1199`.
- [Complete gate](../results/rc-0.2.6-rc.9-6506d4fa38bf41eeb011083b62204b39/automated.json)
  and [installation receipt/RC8 backup](../results/ux-0.2.6-rc.9-install-3570eb7cbaf247c4b2c74ee9824ef17a/receipt.json).
  Original RC6 backup remains in the RC8 receipt below. Exact artifacts/evidence
  are hash-verified in both the UX worktree and main checkout.
- All six mod files and second native copy verified. Settings/probe/game/UMM
  files remain byte-identical; crash on/19.52381 and landing on/20, settings SHA
  `C373DA18429788ACDFEF7A032496981BDCC5571D7285543B249769846E5876EF`.
- Owner's old FFB selection has a name/index but no verified GUID. Before the
  next drive, use **F6 → FFB → Use steering wheel**, or explicitly reselect
  the MOZA. Steering already has a saved GUID; the new strict selector never
  guesses an output device from an old name/index. No setting was silently changed.
- The no-output UI smoke found native-menu leakage and small4K text (KI-40).
  Normal Quit and exact settings/UMM/payload restoration completed19:52:01UTC.
  See [the evidence and successor retest](reviews/2026-09-19-rc9-ui-smoke.md).
  KI-38 crash feel and KI-39 full hardware/layout remain open. Toolkit358add5
  and probe0.2.5.4 are unchanged; no recording or SimHub edit.

## Previous candidate — 0.2.6-rc.8

Installed **2026-09-19 19:21 UTC** with the game closed after all 16 local gates.
The new Simple/Advanced settings pages include transactional binding/calibration,
USB shortcuts, persistent Stop FFB, and native keyboard conflict checks. Use
[the candidate guide](UX-SETTINGS-CANDIDATE.md); public 0.2.5 has the older UI.

- Identity: `0.2.6-rc.8+2517162d71fc12e030ecda64d8b46d31f0d56b20.clean`.
- ZIP SHA-256: `0FFB6CEAE47E7268AD2A4815B69F69984D8CEA56C7028A5377E1594B337352C3`.
- [All 16 gates](../results/rc-0.2.6-rc.8-a16bd4f3f545419d91ccc2ca7f774b90/automated.json)
  passed, including both real drives, 9,507 landing/crash assertions, Unity Mono
  hook compatibility and installer entrypoints. No physical wheel output.
- [Installation receipt and RC6 backup](../results/ux-rc8-install-8a331d2029c14571892bb2e2cb0f5b4f/receipt.json):
  fresh ZIP extraction and all six installed payloads plus the second native copy
  verified. Settings, game assembly, UMM parameters and probe files are unchanged.
- Saved **crash on/19.52381, landing on/20**; settings SHA-256
  `C373DA18429788ACDFEF7A032496981BDCC5571D7285543B249769846E5876EF`.
- Toolkit `local+358add53af220eb95bf606e240b230e23a6ea197.clean`, native SHA-256
  `A07DDF7E10ADBD016DB204324D5E035B951E21A7D8DD373405C91257EB7AD288`, unchanged
  from RC6. Probe remains 0.2.5.4; no recorder is packaged. Crash feel KI-38 and
  the settings hardware/layout matrix KI-39 remain open.
- Source fast-forwarded and pushed to main with `[skip ci]`; built locally.
  UMM displays 0.2.6; support/build metadata identifies RC8. No public release.

Built in `E:/Source/art-of-sim-rally-ux`; the exact ZIP/checksum, gate evidence,
RC6 backup and receipt are hash-verified in main's ignored `dist`/`results` too.
RC7 passed offline but was not installed; RC8 adds native keyboard conflict checks.
An allocated, force/telemetry-disabled UI smoke remains pending at installation.

## Previous candidate — 0.2.6-rc.6

Installed **2026-09-17 05:05 UTC** with the game closed after all 16 local
gates. Crashes use method A's finite constant-force push/release, with a
new-settings default of 50 and range 0–100. Landing stays default 5/range 0–40.
The stronger in-game effect still needs an attended test; KI-38 remains open.

- Identity: `0.2.6-rc.6+1cd7814ac20c647f176c57925875e6af47858e91.clean`.
- ZIP SHA-256: `0FDAE9D3822F69F3F83212BD4B1FEFDBE356528C899CD2CC057930B90D352335`.
- [All 16 gates](../results/rc-0.2.6-rc.6-dea9d3926a274f2f923aed528a1140e8/automated.json)
  passed, including both recorded drives, 9,503 landing/crash assertions,
  actual Unity Mono hooks and real installer entrypoints. The saved jump still
  has one landing at row 4230. These checks do not apply physical wheel force.
- [Installation receipt and RC4 backup](../results/constant-crash-rc6-install-4c471c524bbc43d083915f16aee51942/receipt.json):
  all six mod payloads and the second native copy match the package. Settings,
  game assembly, UMM parameters and separate probe files are byte-identical.
- Preserved settings: **crash on/19.52381, landing on/20**. Select crash strength
  **50** manually for comparison; installation does not overwrite a saved tune.
  Settings SHA-256: `C373DA18429788ACDFEF7A032496981BDCC5571D7285543B249769846E5876EF`.
- Toolkit pin: `local+358add53af220eb95bf606e240b230e23a6ea197.clean`, unpublished
  package 0.15.0/native 0.8.0. Native SHA-256:
  `A07DDF7E10ADBD016DB204324D5E035B951E21A7D8DD373405C91257EB7AD288`.
  The native API, tests and reusable findings are pushed upstream. Probe stays
  0.2.5.4; no game launch, recording, Pit House or SimHub change.
- UMM displays **0.2.6**; support/build metadata identifies **0.2.6-rc.6**.
  [Implementation, upstream contribution and testing limits](reviews/2026-09-17-constant-crash.md).
  Public stable is 0.2.5. Official toolkit repin and attended checks remain.

Built in `E:/Source/art-of-sim-rally-timing`; exact ZIP/checksum, gate evidence,
backup and receipt are also retained in the main checkout's ignored `dist`/`results`.
Preliminary RC5 passed all gates but was not installed: its packaged guide still
described the old effect. RC6 corrects those instructions with the same runtime.

## Previous candidate — 0.2.6-rc.4

Installed **2026-09-17 03:57 UTC** (09-16 local) with the game closed after all
16 local gates. Crashes now request a peak-start kick and fading rebound at the
same configured amplitude. Landing requests, steering and telemetry are unchanged.
**Attended follow-up failed:** owner felt no crash effect at about 20 in RC4,
while normal steering worked. Three shape-accepted commands completed without
early managed stops. KI-38 remains open; RC4 is not release-ready.
[Support analysis](reviews/2026-09-17-rc4-crash-feel.md).

- Identity: `0.2.6-rc.4+91cedd8307010c796efe79590661368a52b56d27.clean`.
- ZIP SHA-256: `39643F929A680027937B195F2B18623DC69C289229A65C1AE27AE7AD0078F2BA`.
- [All 16 gates](../results/rc-0.2.6-rc.4-b232db3bfe2346e7b200c2059258a9a3/automated.json)
  passed, including both real corpus cases, 9,342 landing/crash assertions,
  Unity Mono checks and real installer entrypoints. No physical wheel output.
- [Installation receipt and RC3 backup](../results/crash-kick-rc4-install-87c17f07d71242bba8ddc5faf1cbb770/receipt.json):
  all six mod payloads and the second native plugin copy match the exact package.
  Settings, game assembly, UMM parameters and separate probe files are byte-identical.
- Preserved owner settings: **crash on/19.52381, landing on/20**. Settings SHA-256:
  `C373DA18429788ACDFEF7A032496981BDCC5571D7285543B249769846E5876EF`.
  These supersede RC3's earlier installation-time crash off/5 values.
- Toolkit pin: `local+c319b0258b11d1f07492a48dbc110d3e50c14dfd.clean`, unpublished
  package 0.14.0/native 0.7.0. Native SHA-256:
  `BCA81756B5FD920802D51C111AB55D5ED7F7219FCEA40588A65707A89D226EA4`.
  Probe stays 0.2.5.4. No game launch, recording, Pit House or SimHub change.
- UMM displays **0.2.6**; support/build metadata identifies **0.2.6-rc.4**.
  [Candidate behavior, evidence and test plan](reviews/2026-09-17-crash-kick.md).
  Public stable remains 0.2.5; official toolkit repin and attended checks remain.

Built in `E:/Source/art-of-sim-rally-timing`; the exact ZIP/checksum, gate evidence
and installation backup/receipt are also retained in the main checkout's ignored
`dist`/`results`, with copied artifact hashes verified.

## Previous candidate — 0.2.6-rc.3

Installed **2026-09-17 03:23 UTC** (2026-09-16 local), with the game closed, after
all 16 local gates. Both wheel-impact sliders now extend to 40%. Existing values
retain their output, so the owner's landing on/20 is unchanged. Crash stays
off/default 5. Telemetry boosting is explicitly deferred; no SimHub gains changed.

- Identity: `0.2.6-rc.3+b85c27a735eac7f55661cd84e9f39b0a20d17f7f.clean`.
- ZIP SHA-256: `E1C784BC889A720D3C621EF88E258ECEE10471D8665A65C4287293277C18BE15`.
- [All 16 gates](../results/rc-0.2.6-rc.3-d692d149e51b4b70bcb0c4bc561298e6/automated.json)
  passed, including both real corpus cases and 9,248 landing/crash assertions.
  Increased output limits, overlap ownership, stop behavior and settings roundtrip
  have offline coverage; actual stronger wheel feel remains untested.
- [Installation receipt and RC2 backup](../results/strength-rc3-install-d28b515512a34942a5c10c168023dc3b/receipt.json):
  six mod payloads and the second native copy match the exact package. Settings,
  game assembly, UMM parameters and separate probe files remain byte-identical.
- Settings SHA-256: `3819C39EABB5B7FE151E08547251F18285812664F5B5256DF4FE512A02E307B1`.
  Toolkit remains the same local/unpublished native 0.6.1 candidate; probe remains
  0.2.5.4. No game launch or recording. Public stable is still 0.2.5.
- UMM shows **0.2.6**; support/build metadata identifies **0.2.6-rc.3**.
  [Change and test notes](reviews/2026-09-16-stronger-impact-effects.md).
  Public release still needs official toolkit repin and attended checks.

Built in `E:/Source/art-of-sim-rally-timing`; exact ZIP/checksum, gate evidence and
deployment backup/receipt were also copied to the main checkout's ignored
`dist`/`results`, with copied artifact hashes verified.

## Previous candidate — 0.2.6-rc.2

Installed **2026-09-16 21:24 UTC**, with the game closed, after all 16 local gates.
Removes default per-force native disk tracing and adds smaller-hitch/last-landing
diagnostics. Landing timing/gain, steering arithmetic, telemetry and crash policy
are unchanged. Settings are byte-identical, landing on/20 and crash off/default 5.

- Identity: `0.2.6-rc.2+6f7302595b25e8196e67af2591cf5a15780c82c0.clean`.
- ZIP SHA-256: `4DC58A75A5417629FF42748AA34C50C3AE7BBEA976DF793F1DF4E5E16EA10AD0`.
- [All 16 local gates](../results/rc-0.2.6-rc.2-272c0b582dc449df8ce4a0bb348b388b/automated.json):
  native binding/version checks, arithmetic/lifecycle, actual Unity Mono hooks,
  both recorded corpus cases, unchanged single landing, support persistence,
  replay protocol, package and installer checks passed.
- [Installation receipt and RC1 backup](../results/timing-rc2-install-a56c9d50a03048b4bd4f72a2d60fdfad/receipt.json):
  all six payloads and the second native copy match the package. Settings, game
  assembly, UMM parameters and every separate probe file are preserved.
- Toolkit is **local/unpublished** `82c789117f115034a074bcb93133fefc8b955e35.clean`,
  managed 0.13.1/native 0.6.1. Final release is blocked until official repin.
  Native SHA-256: `FE85A1ECEC10E84134EA14F8362683293894BE8D485076F8CB38CA22AE9FF2F0`.
- Separate probe remains 0.2.5.4. No game launch, recording, physical force test,
  SimHub/profile adjustment or public publication. UMM shows **0.2.6**;
  support/build metadata identifies **0.2.6-rc.2**.
- [Investigation and attended comparison](reviews/2026-09-16-landing-startup-investigation.md).
  Haapajarvi startup/early landing are not reproduced; normal FFB with the new
  native candidate and full attended matrix remain pending. Public stable is 0.2.5.

Build/evidence originated in isolated `E:/Source/art-of-sim-rally-timing`.
Exact package, gate logs and deployment backup/receipt were also copied into
the main checkout's ignored `dist`/`results`; copied package hash was verified.
The prior failed version-guard run is retained as
`results/rc-0.2.6-rc.2-00bbd090d8b54c91a383120dd60c53c6`.

## Previous candidate — 0.2.6-rc.1

Installed **2026-09-16 20:06 UTC**, with the game closed, after all 16 local gates.
Optional crash vibration defaults **off**, strength 5/cap 20. Owner settings are
byte-identical, including landing enabled/20. Public stable remains 0.2.5.

- Identity: `0.2.6-rc.1+2e32256f99514db9a01f72888698726d31f4b2b9.clean`.
- ZIP SHA-256: `23131C59FEB11409D42C8379B6B5360FA15FCB36CBFA9C2C239868D2B1660AB3`.
- [All 16 local gates](../results/rc-0.2.6-rc.1-4036497765f24657a723521a0dfe365f/automated.json):
  both real corpus cases, unchanged single landing, crash/overlap lifecycle,
  actual Unity Mono shipping/probe hooks, packaging and installer checks passed.
- [Installation receipt and 0.2.5 backup](../results/crash-rc1-install-12724d6ddcfa4bdb9a13d4cabff440ec/receipt.json):
  six mod payloads and the second native copy match the exact package. Settings,
  game assembly, UMM parameters and all separate probe files are preserved.
- Developer probe remains 0.2.5.4, SHA-256
  `4FBACE0242834376CEA8903AAA0A08C86D3A6EBA83E63AF0659BD92518916EE7`.
  Toolkit remains v0.13.0/native 0.6.0. No game launched, no recording started.
- Steam/Stream Deck target and SimHub profile/gains unchanged. UMM shows numeric
  **0.2.6**; build/support identify **0.2.6-rc.1**.
- [Crash test plan](CRASH-EFFECTS.md); live contact detection, timing, feel and
  SimHub input/output comparison remain pending. No public publication.

## Previous installed stable — 0.2.5

**[0.2.5 is published](https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.5)
and was installed on 2026-09-13 03:57 UTC.** Wheel landing vibration defaults on
at strength 5; existing saved settings, including the owner's enabled/20, remain.
The accepted built-in SimHub profile and gains are unchanged.

- Identity: `0.2.5+c6242a0a163315f7a390d3860c6204b4ca215619.clean`.
- ZIP SHA-256: `8A7F9FC044058B0890F7323138E3DFCBC998B31C67C57E14272D410C6BFCA502`.
- [All 16 final local gates passed](../results/rc-0.2.5-ab78d6a9af8446b9ac562904e7062c6a/automated.json), including actual Unity Mono and the real drive corpus.
- [Installation receipt and RC8 backup](../results/release-025-install-2bd3016bb6334b8ea4effb6142c15c2b/receipt.json): all payloads verified, settings and separate probe 0.2.5.3 preserved. Game closed throughout; Steam/Stream Deck target unchanged.
- [Published download verification](../results/release-025-published-b7832204b25e4f5cb6d1db48bd0adc3f/verification.json): ZIP and checksum match the exact final artifact.
- Official toolkit v0.13.0 replaces the local pin with byte-identical RC8 binaries. No SimHub helper or recorder is shipped.
- [Owner acceptance and release limits](reviews/2026-09-13-release-0.2.5.md): overall RC8 acceptance authorizes publication; full attended matrix and a separate final-artifact drive remain unverified.

## Crash recorder update — 2026-09-15 UTC

The separately installed developer probe is now **0.2.5.4/schema 4**, ready to
observe body collisions during the next explicitly started recording. Stable
0.2.5 and the installed settings remain unchanged. No recorder is shipped in the
release package; nothing is recording and the game is closed.

- Probe SHA-256: `4FBACE0242834376CEA8903AAA0A08C86D3A6EBA83E63AF0659BD92518916EE7`.
- [Install receipt and 0.2.5.3 backup](../results/crash-probe-install-0a257bd79c1e403882130196c3d7421d/receipt.json):
  exact gated DLL/manifest installed, 13 game/mod/settings hashes preserved.
- [All 16 local gates](../results/rc-0.2.5-rc.11-104bedc73e144db98e651c4c22d27263/automated.json),
  including both real drive cases and actual-Mono collision hook attachment.
- The local rc.11 ZIP is packaging validation only, not a changed game build to
  deploy. No public release or SimHub profile change.
- [Crash baseline, fixes and live-test limits](reviews/2026-09-15-crash-capture.md).

## Documentation/installer audit — no game deployment

The 2026-09-13 audit validates revised installer/readme packaging as local
`0.2.5-rc.10`; all 16 gates pass. It changes no game/force code. Installed stable
0.2.5 above remains the current game payload, and published assets are unchanged.
The new installer/template are for the next package; no game redeployment was
needed. [Audit and exact validation evidence](reviews/2026-09-13-docs-install-audit.md).

## Previous candidate — RC8 restored after RC9 withdrawal

**RC8 restored on 2026-09-13 01:36 UTC.** The owner rejected RC9's extra SimHub
helper; its game code/plugin/profile were removed. The exact RC8 archive below
was reinstalled, with settings/probe preserved and the Stream Deck target unchanged.
[New receipt and RC9 backup](../results/landing-rc8-install-c9b92ca721ed45d892901e8381a45b75/receipt.json).
SimHub now has a built-in-effects-only 30 Hz comparison profile; the original
profile/gains are preserved. [Correction, evidence and retest](reviews/2026-09-12-builtin-shaker-correction.md).

**0.2.5-rc.8** was first installed on 2026-09-12 23:54 UTC. It adds opt-in landing
vibration with independent strength to RC7. The existing settings were preserved;
landing vibration defaults to off, with strength 5 when enabled.

- Identity: `0.2.5-rc.8+b9598f5cb6b0a48965b37f4f8d27f350fd59be49.clean`.
- ZIP SHA-256: `90F3F0BD942FD6F446702BAF185842196090B54882D821AE737AE1046CDAFADB`.
- [All 16 gates passed, including the real drive corpus and landing detection](../results/rc-0.2.5-rc.8-669ae7f5aae140ca9e793fd9ad0b1781/automated.json).
- [Installation receipt and RC7 backup](../results/landing-rc8-install-a1e5dbdfefd9432d98ebeea43f31b65d/receipt.json).
- Six mod payloads and the native plugin copy match the exact validated package.
  Settings and separate probe 0.2.5.3 were preserved byte-for-byte. The game stayed
  closed; the Steam/Stream Deck installation target is unchanged.
- Toolkit pin is `local+dd0ef20ad0cdaccc7a67f10a707dbd2a27a6efe9.clean`, managed
  0.13.0/native 0.6.0. This is an unpublished development candidate. Final
  packaging rejects local toolkit pins; official publication/repin is required.
- [Landing A/B test](LANDING-EFFECTS.md), FFB startup/recovery, digital gauge
  clearing, quit and font scaling remain attended checks. The first nonzero
  [owner landing test](reviews/2026-09-12-landing-wheel-test.md) logs eight accepted
  wheel cues. Owner says wheel FFB is fine; the desired stronger ButtKicker thud
  is a separate telemetry/haptic-output follow-up.
  No public release; stable remains 0.2.4.

## Previous candidate — RC7

**0.2.5-rc.7** was installed on 2026-09-12 16:44 UTC. It adds the KI-32 font
scaling correction and clearer handbrake/logging/smoothing help to RC6.

- Identity: `0.2.5-rc.7+8dfa61c5fb2212273059ec3714421c9e4fc7debe.clean`.
- ZIP SHA-256: `948D410F34102C7739ECD5148E81EA8EEDE3A29250BEF7C16BFDAE5F9343E0F2`.
- [All 16 gates passed, including the real drive corpus](../results/rc-0.2.5-rc.7-cf98987b0c984f4eb37175090bafa10f/automated.json).
- [Installation receipt and RC6 backup](../results/support-questions-rc7-install-38dee4a087ec466c92ca750c9b2b800b/receipt.json).
- Six mod payloads and the native plugin copy match the exact validated package.
  Settings and the separate 0.2.5.3 probe were preserved byte-for-byte. The game
  stayed closed; the Steam/Stream Deck installation target is unchanged.
- Font scaling still needs an in-game visual check; all prior hardware gates
  remain pending. No public release or support reply was sent. Stable is 0.2.4.

## Previous candidate — RC6

**0.2.5-rc.6** was installed on 2026-09-11 03:34 UTC, from the exact package
that passed all 16 local gates **including one real recorded drive**.

- Identity: `0.2.5-rc.6+8ea54775efa42fe60b367760c72bb0f9668a1c9a.clean`.
- ZIP SHA-256: `A0A99CFA2678142D09C1F63F391D5127706B171C98FC7FF9F84B2B39FD39A4E4`.
- [Gate](../results/rc-0.2.5-rc.6-3166e9988c98438dbf669170f92253c7/automated.json),
  [installation receipt and RC5 backup](../results/bug-investigation-20260911/install-rc6/receipt.json).
- All six installed payloads and the second native plugin copy match the package;
  settings were preserved byte-for-byte. The Steam/Stream Deck game directory
  remains the same. Game stayed closed; no public release or attended pass.
- Optional developer probe **0.2.5.3** is installed, hash
  `F911A673F992963C99E5A10D048C9F849D82A8582BBC87EF9B9D64E815778D49`.
  It remains separate from the release ZIP; previous probe backed up.
- Local DSS speed/RPM dashboard bindings were backed up and repaired. SimHub
  exited normally and restarted on 03:35 UTC; logs confirm both round displays
  reloaded their dashboard. Physical end-of-race clearing remains untested.
- [Fixes, signal study and remaining tests](reviews/2026-09-11-bug-follow-up.md).

## Previous candidate — RC5

**0.2.5-rc.5** was installed after passing all 16 local checks, with settings and
the Steam 550320 launch target preserved. [Current candidate evidence and receipt](reviews/2026-09-09-overnight-025.md).
The earlier 0.2.4 installation below is backed up. No candidate hardware result
or public publication was implied by deployment. The subsequent owner drive
reported FFB startup and telemetry gauge failures (KI-28/KI-29); RC5's attended
gate now fails those cases. Do not publish this candidate.

- UMM displays **0.2.5**; support/build.json identify
  `0.2.5-rc.5+43e0b4a2978121e712d20c8f170567b587554bdf.clean`.
- [RC5 install receipt and RC3 backup](../results/overnight-025-install-156deb45275a414bba20b5ee1f2135a5/install-receipt.json).
- Six mod payloads and the native plugin copy match the exact package; settings
  were unchanged at deployment. Game stayed closed, developer probe was absent,
  Stream Deck preserved.
- RC4 passed offline but was superseded before installation by the shifter
  selection-display correction in RC5. Published stable remains 0.2.4.

For the owner's attended drive on 2026-09-10 local time, the separate developer
probe **0.2.5.2** is installed after correcting KI-26 and adding the KI-27 menu-quit
save hook. Explicit menu-only saving and CSV hashes passed in Unity; the automatic
quit-save path still needs a drive. RC5/settings were preserved during replacement. The probe
was present; remove the current probe with the game closed before the comparison drive
without instrumentation. [Capture setup and receipts](reviews/2026-09-10-attended-rc5.md).

## Previous stable installation

- **0.2.4 stable**, installed 2026-09-09 03:09:23 UTC with the game closed, from
  the downloaded [GitHub release](https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.4).
- Identity: `0.2.4+dc14fe70002205e59c8b462c8c8636b72132abdd.clean`.
- UMM displays **0.2.4**; build.json/support now show the final identity without an RC suffix.
- ZIP SHA-256: `08AA70055C4957DA8C96EAB2078BA0F52211E51B7249C38A0B409BBC6AD50942`.
- [Install receipt and RC5 backup location](../results/release-0.2.4-install-3b9ec98a27bf415b807d7974ff3484c3/install-receipt.json).
- All six mod payloads plus the second native plugin copy match the published
  package manifest; Settings.xml preserved byte-for-byte. RC5 install/settings
  backed up before replacement. Published asset digests independently verified.
- Existing Steam 550320 Stream Deck button launches this installation; developer
  recorder remains absent. No game was launched as part of deployment.
- [Final release evidence](reviews/2026-09-09-release-0.2.4.md): all 16 local
  automated checks pass. Owner accepted RC5, which has the same production source
  and toolkit; detailed attended cases and final-labelled drive remain pending.

## Deployment rules

1. Use the newest eligible local artifact on this project's current development
   line. Validate its clean source identity, all required automated checks and
   archive hash against `results/rc-*/automated.json`, using
   `tools/testing/rc_gate.py`'s `verify_automated` where useful. Exclude candidates
   with failed attended cases, withdrawn/superseded builds and version downgrades.
   An unchanged installed identity needs no deployment or rebuild.
2. If `artofrally.exe` is running, defer. Never close the game to perform a routine
   update. Extract the exact validated ZIP, validate its manifest, and back up
   the existing mod, Settings.xml and `Plugins/x86_64/UnityForceFeedback.dll`.
3. Run the extracted package's `install.ps1 -GameDir` against the existing install,
   then match all six mod files, native plugin copy and build identity to the
   manifest/report. Verify the settings hash is unchanged; retain the backup and
   report failures instead of claiming success.
4. Save a local receipt under `results`, update this installed-candidate record
   and session handoff, and report the actual deployed version. Preserve wheel
   tuning, controls, Stream Deck action and optional developer-probe state. Do not
   launch a game or mark attended checks passed as part of deployment.

## Feature completion checklist

- [ ] Build and validate the completed feature/fix in an immutable local artifact.
- [ ] Deploy that exact artifact with the game closed, backing up the previous
  install and preserving settings.
- [ ] Verify all payloads, native plugin copy, identity and unchanged settings;
  save the receipt and report the installed version in the handoff.

If the game is running, leave deployment pending and pick it up at the next active
work session. Do not close the game or set up periodic checks. The owner stopped
scheduled polling on 2026-09-09 UTC; automation
`keep-art-of-sim-rally-installed-build-current` is paused. An unchanged build or
documentation-only work needs no rebuild or redeployment.
