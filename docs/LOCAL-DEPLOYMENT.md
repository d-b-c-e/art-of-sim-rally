# Local deployment

The owner gave standing authorization on 2026-09-09 UTC to keep the installed
copy current. After a new candidate or final artifact passes the complete local
automated gate, deploy it for testing without asking again. This is part of
finishing the build, not an optional follow-up. Public release publication and
attended sign-off remain separate.

## Installed candidate

- **0.2.4-rc.5**, installed 2026-09-09 02:47:18 UTC with the game closed.
- Identity: `0.2.4-rc.5+5701ebb69adcd17fe4a4122806b77b88594155a8.clean`.
- UMM displays the numeric mod version **0.2.4** for this candidate. The RC suffix
  is in build.json and support build identity; the UMM list alone cannot distinguish RCs.
- ZIP SHA-256: `00451924A77E2BF6E8E0CEDBF3D2EE14868B5491F7536B524CF060A8399D2F63`.
- [Install receipt and backup location](../results/rc5-install-8be3a84a9e254c9eb1b2d1e68490aeef/install-receipt.json).
- Six mod payloads and the second native plugin copy match the package; current
  Settings.xml was preserved byte-for-byte. Previous RC4 install/settings backed up.
- Existing Steam 550320 Stream Deck launcher opens this installation. Developer
  recorder remains absent. No game was launched as part of deployment.
- [Automated results and attended checklist](reviews/2026-09-09-rc4-stutter.md#exact-successor-candidate).
  All 16 automated checks pass; all seven attended cases remain pending. RC4's
  failed stutter evidence is historical and is not transferred to RC5.

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

## Follow-up when a build is waiting

An hourly heartbeat attached to this task checks for eligible artifacts that were
not installed immediately, including builds deferred while the game was running.
Automation ID: `keep-art-of-sim-rally-installed-build-current`. It stays quiet when
unchanged or waiting for the game to close, and reports completed updates or actual
failures. It does not build new versions just because the schedule fired.
The local computer and app need to be running for this check.
