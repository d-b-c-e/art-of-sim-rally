# Local deployment

The owner gave standing authorization on 2026-09-09 UTC to keep the installed
copy current. Local deployment is a required checklist item when finishing a
feature or bug fix. After its candidate or final artifact passes the complete
local automated gate, deploy it for testing without asking again. Public release
publication and attended sign-off remain separate.

## Installed stable — 0.2.5

**[0.2.5 is published](https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.5)
and installed as of 2026-09-13 03:57 UTC.** Wheel landing vibration defaults on
at strength 5; existing saved settings, including the owner's enabled/20, remain.
The accepted built-in SimHub profile and gains are unchanged.

- Identity: `0.2.5+c6242a0a163315f7a390d3860c6204b4ca215619.clean`.
- ZIP SHA-256: `8A7F9FC044058B0890F7323138E3DFCBC998B31C67C57E14272D410C6BFCA502`.
- [All 16 final local gates passed](../results/rc-0.2.5-ab78d6a9af8446b9ac562904e7062c6a/automated.json), including actual Unity Mono and the real drive corpus.
- [Installation receipt and RC8 backup](../results/release-025-install-2bd3016bb6334b8ea4effb6142c15c2b/receipt.json): all payloads verified, settings and separate probe 0.2.5.3 preserved. Game closed throughout; Steam/Stream Deck target unchanged.
- [Published download verification](../results/release-025-published-b7832204b25e4f5cb6d1db48bd0adc3f/verification.json): ZIP and checksum match the exact final artifact.
- Official toolkit v0.13.0 replaces the local pin with byte-identical RC8 binaries. No SimHub helper or recorder is shipped.
- [Owner acceptance and release limits](reviews/2026-09-13-release-0.2.5.md): overall RC8 acceptance authorizes publication; full attended matrix and a separate final-artifact drive remain unverified.

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
