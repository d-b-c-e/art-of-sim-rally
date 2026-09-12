# Local deployment

The owner gave standing authorization on 2026-09-09 UTC to keep the installed
copy current. Local deployment is a required checklist item when finishing a
feature or bug fix. After its candidate or final artifact passes the complete
local automated gate, deploy it for testing without asking again. Public release
publication and attended sign-off remain separate.

## Installed candidate — 0.2.5 work

**0.2.5-rc.7** is installed as of 2026-09-12 16:44 UTC. It adds the KI-32 font
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

**0.2.5-rc.6** is installed as of 2026-09-11 03:34 UTC, from the exact package
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
