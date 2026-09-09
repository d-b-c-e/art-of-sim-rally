# Local deployment

The owner gave standing authorization on 2026-09-09 UTC to keep the installed
copy current. Local deployment is a required checklist item when finishing a
feature or bug fix. After its candidate or final artifact passes the complete
local automated gate, deploy it for testing without asking again. Public release
publication and attended sign-off remain separate.

## Installed candidate — 0.2.5 work

**0.2.5-rc.3** is installed after passing all 16 local checks, with settings and
the Steam 550320 launch target preserved. [Current candidate evidence and receipt](reviews/2026-09-09-overnight-025.md).
The earlier 0.2.4 installation below is backed up. No candidate hardware result
or public publication is implied.

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
