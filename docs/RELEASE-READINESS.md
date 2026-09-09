# Release readiness — 2026-09-08

**RC4 attended update (2026-09-09 UTC): strong stutter reported.** Overall drive
feedback was good, but its stutter case is failed. [Log investigation and KI-20
fix](reviews/2026-09-09-rc4-stutter.md) found lazy-manager polling with a large
ghost-download backlog. Successor source passes targeted tests; a new candidate
and attended comparison are required. RC4 remains installed.

**0.2.4-rc.4 passes all 16 automated checks.** The
[feedback follow-up and exact artifact](reviews/2026-09-08-feedback-review.md)
include CameraMod compatibility isolation, diagnostics, telemetry sampling and
send-failure handling after RC2. The stutter case is failed; six cases remain pending,
especially motion/shaker response. RC4 is now installed locally at the owner's
request (2026-09-09 02:17 UTC), with payload hashes verified and settings preserved.
The existing Stream Deck button launches it. It was driven but is not published.
[Install receipt](../results/rc4-install-7bf2d9692b304b58bbe124f80d43cc4b/install-receipt.json).
RC2 evidence below is historical; RC3 stopped before packaging and was superseded.

**0.2.4-rc.2 passes all 16 automated checks** with camera key/persistence and
direct-input fixes. It has not been installed, driven or published. Its
[exact artifact, results and handoff](reviews/2026-09-08-overnight.md) are separate
from the published/installed 0.2.3 evidence below. [Overnight status](OVERNIGHT-QUEUE.md) and
[extra drive checks](TEST-DRIVE.md#extra-checks-for-024) describe the new scope.

**[0.2.3 is published](https://github.com/d-b-c-e/art-of-sim-rally/releases/tag/v0.2.3)
and was previously installed locally; RC4 now replaces that local install.** RC6 passes
all 16 automated gates and was driven after local installation. Owner feedback:
"no stutter, camera worked great, and no issues with the controls so far."
The full attended matrix remains pending. Publication is a scoped release decision,
not a passed manual gate. The final-labelled artifact receives its own automated
run and checklist; it has not been driven yet.

The RC6 UMM log confirms toolkit/Mono loading and live force evaluation. Local
log and exact owner report are preserved under `results/release-0.2.3-preparation`.
No real capture was completed. RC6 evidence below remains historical and immutable.

## Published final artifact

- Published 2026-09-08 05:08:05 UTC; stable/latest GitHub release.
- Tag `v0.2.3`, source `1a9df539bad2f3bddfc80adc36ec9e4ef3b1c2a8`.
- Identity `0.2.3+1a9df539bad2f3bddfc80adc36ec9e4ef3b1c2a8.clean`.
- ZIP SHA-256 `0C82AC79A91093C82732EDAA2878597D429AB4A480A61EADD657C655BDFE9F3F`.
- [Final automated report](../results/rc-0.2.3-5898189c119e4483af6d7a4a0110ff5b/automated.json):
  all 16 checks pass, including 450,079 consumer assertions and 703 reference rows.
- [Final attended checklist](../results/rc-0.2.3-5898189c119e4483af6d7a4a0110ff5b/manual.json):
  pending; no real corpus. RC6 smoke results are not copied as final-artifact passes.
- Downloaded the published ZIP and checksum and matched local SHA-256 plus GitHub's
  asset digest. Production source, Version.props and toolkit are unchanged from RC6.
- Installed that downloaded archive with the game closed. All six mod files and
  the second native plugin copy match its manifest. Settings.xml was preserved
  byte-for-byte, including the owner's current Strength 50 and Smoothing 0.2.
- Removed the separately installed developer probe for the shipping setup; prior
  mod, settings and probe are backed up locally. No game was launched after this
  final installation. [Install receipt](../results/release-0.2.3-preparation/final-install-receipt.json).
- Existing Stream Deck key still launches Steam app 550320 into this installation.
  No button/profile change was needed. The RC6 install description below is history.

The owner explicitly requested publication and supplied the scoped RC6 smoke
result. That decision leaves the full manual gate incomplete; it does not change
the gate's rejection of missing evidence. Follow-up work and a new source finding
in camera tuning persistence (KI-14) are in [OVERNIGHT-QUEUE.md](OVERNIGHT-QUEUE.md).

## Historical 0.2.3-rc.6 candidate

- ZIP: [ArtOfSimRally-0.2.3-rc.6.zip](../dist/ArtOfSimRally-0.2.3-rc.6.zip)
- Source commit: `cf4ceb2263fcb09f44de6ef12c3a436bcde71d81`, clean when packaged.
- Identity: `0.2.3-rc.6+cf4ceb2263fcb09f44de6ef12c3a436bcde71d81.clean`.
- ZIP SHA-256: `68AA854C1080A067D866CA447F34271FB0E2A6DBC3E5AD640F539435DB044222`.
- [Automated report](../results/rc-0.2.3-rc.6-5bc7ee50847748b59f4309b4cac7ad92/automated.json).
- [Attended checklist](../results/rc-0.2.3-rc.6-5bc7ee50847748b59f4309b4cac7ad92/manual.json).

The ZIP and evidence links are local generated artifacts, intentionally outside
Git. Keep RC5 and earlier evidence as history; RC6 contains the recorder separation
and telemetry recovery correction. Later documentation commits do not rebuild RC6.

## Completed

- Prior camera ownership/handback, deferred settings persistence, support identity,
  GUID selection and native lifecycle fixes remain in the candidate.
- Toolkit v0.12.0 is consumed in production: native driver, managed device wrapper,
  `AxleForceCurve@1` and telemetry encoder. Native component is 0.5.0. The official
  release artifacts were verified; no local copy of shared force arithmetic or
  toolkit P/Invoke bindings remains in production.
- Recorder and replay are development tools. The release mod has no recorder UI,
  hooks or dependency. A separately installed UMM probe observes the installed
  candidate; external PowerShell commands control it. Standalone replay needs no
  game assemblies or wheel. Packaging rejects recorder leakage.
- Recorded cases can be validated, preserved with immutable names and reused
  through `--corpus` or `Test-Rc.ps1 -Corpus`. Overflow, incomplete/corrupt files,
  discontinuous history and synthetic corpus entries fail. Concurrent promotion
  cannot silently replace another writer's index.
- Review found and fixed telemetry getting stuck after a connection error (KI-11).
  Correcting the destination or explicitly restarting now recovers; unchanged
  failures do not cause repeated connection attempts. Failed sockets are disposed.
- Release/test instructions, troubleshooting and the known-issue register reflect
  the current implementation and the remaining uncertainty.

## Evidence obtained without a drive

| Coverage | Result |
|---|---|
| Production build | Zero warnings/errors. |
| Consumer regression | 450,079 assertions; 125,000 dynamic force steps against real managed Unity Mathf, plus 100,000 checks of the portable baseline sequence. No changed device magnitudes. |
| Reference vector | 703 rows unchanged. |
| Camera/watchdog | 25 assertions against test doubles; no rendering claim. |
| Telemetry | 34 assertions exercising production connection code and real loopback UDP, including destination recovery, parking, socket failure and restart. |
| Developer probe | 52 assertions across buffers/receipts/retry, actual observation bindings, real net48 Harmony attach-observe-unpatch, and external PowerShell IPC. |
| Replay | Synthetic capture passes with zero float/device difference; one reset boundary. 63 assertions plus rejection checks. |
| Adversarial/evidence tests | 19 Python tests, including corpus promotion and overwrite rejection. Synthetic protocol fixtures are explicitly identified as such. |
| Packaging/installers | Exact payload, compiled recorder exclusion, release install/upgrade/uninstall, developer probe install/remove and corrupt-package rejection. All installs were in a fake game layout. |
| Source/artifact integrity | Source stable during the gate; vendored hashes, native architecture/exports, package identity and archive hash checked. |

The complete check list is in automated.json. The report explicitly records
`recordedCorpus.status="not supplied"`, `syntheticOnly=true`, and `runtime=pending`.
The real gate returns `NOT READY: Tester/rig not recorded`, as it should.

## Local test preparation — 2026-09-07

RC6 was installed from the verified ZIP with the game closed. Every installed mod
payload and both native copies match the package; the assembly identity matches
the automated report. Settings.xml is byte-for-byte preserved, including Strength
26 and Smoothing 0.2. The previous installation/settings were backed up beside the
[installation receipt](../results/rc-0.2.3-rc.6-5bc7ee50847748b59f4309b4cac7ad92/local-install-20260907-195440/receipt.json).

The existing art of rally Stream Deck key already launches Steam app 550320,
whose installed directory is this game folder. Profile 1, page UUID
`C881702A-9A40-47FD-A514-0A9E9CA40A8C`, Keypad coordinate `6,3` (bottom row,
seventh key). Its action/layout/artwork were left intact; launching it now uses
the installed RC6 at that time. The separate developer probe was installed for
capture. The later owner drive and final stable installation are recorded above;
the probe has now been removed.

## Remaining attended work

Follow [TEST-DRIVE.md](TEST-DRIVE.md): launch using the existing Stream Deck key,
drive cold/repeated/new-stage cases with the developer probe, pause and save,
then replay and preserve the real capture. Remove the probe and verify the shipped
setup. Allow about 20–30 minutes, longer if reproducing a problem.

All seven attended cases are pending: camera transitions/replay/finish; opening
stutter; FFB feel and pause/focus/disable/quit lifecycle; binding/GUID/settings
persistence; SimHub telemetry; support identity; and the normal UMM upgrade route.
Unity's frame/physics capture callbacks and the probe's runtime overhead also need
that first real session. CLR hook tests cannot execute Unity's native ECalls.

Matching offline force arithmetic does not prove delivered torque, camera pixels,
deterministic game input playback or the absence of the reported stutter. Do not
close KI-1/KI-2/KI-5 or mark Fanatec/PS5/T300 reports tested from these offline runs.

Keep RWD snapback tuning, damper/road/impact effects, new wheel defaults and full
Unity input playback after the maintenance release. They need a separate tuning
and test cycle. Use new candidate identities for future runtime changes; do not
replace the published artifact or silently mark these pending checks passed.
