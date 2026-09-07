# Release readiness — 2026-09-07

**0.2.3-rc.6 is prepared and passes all 16 automated gates. It is not signed off
for release.** Public mod release remains 0.2.2. No candidate or developer probe
was installed into the Steam game, no stage was driven, and nothing was published.

## Exact candidate

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

## Tomorrow's remaining work

Follow [TEST-DRIVE.md](TEST-DRIVE.md): install RC6 with the game closed, temporarily
install the developer probe, drive cold/repeated/new-stage cases, pause and save,
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
and test cycle. Once the attended gate passes, prepare and verify the exact final
artifact intended for distribution before tagging or publishing it.
