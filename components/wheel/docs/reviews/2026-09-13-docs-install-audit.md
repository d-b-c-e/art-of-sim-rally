# Documentation and installation audit — 2026-09-13

The owner requested clear, simple documentation and installation, then authorized
committing and pushing the finished audit. The published/installed release is
0.2.5. No gameplay or force changes are part of this work.

## Findings addressed

- README mixed current setup with older release/candidate history and reported
  the old toolkit pin. It now gives four install steps and a short first drive.
- Missing prerequisites, the wrong GitHub download choice, extracting the ZIP,
  custom paths, updating/removing and confirming the loaded version are explicit.
- Setup now has one player guide, a documentation index and separate build
  instructions. Build docs identify local game/UMM references and the full
  runner's remaining owner-machine path assumptions.
- Clarified direct USB handbrake/axis assignment, two-device operation, smoothing,
  wheel landing versus ButtKicker settings, camera key remapping and support logs.
- Removed claims that Vortex installation was validated. Explained UMM's archive
  route and the batch installer's deliberate second native DLL copy.
- Corrected stale 0.2.4/current-candidate/default-off references in user-facing
  pages. Dated investigations remain historical, with current-status pointers.
- The ZIP readme no longer points to absent local docs. Its standalone template
  lives alongside the installer and is versioned during packaging.
- Fixed installer entry-point/removal defects tracked as KI-33. Added version
  reporting and plain recovery instructions for incomplete downloads/copy errors.
- Repaired the known-issues link to the renamed force-curve conformance section.

## Validation

Cross-checked labels/defaults against Settings/SettingsPanel, Info.json, toolkit
VERSION and package/install code. UMM's official download instructions and
SimHub's official connection/UDP troubleshooting were consulted; project-specific
claims use the existing verified game/telemetry evidence.

The standalone installer suite passes 37 assertions through the actual batch
launchers and Windows PowerShell. It covers fresh install, upgrade, saved opt-out
preservation, removal after UMM removal, repeat removal, missing loader, invalid
game folder, corrupt/missing package files, locked-file failure/retry, and literal
paths with spaces/brackets. It also catches inherited PowerShell 7 module paths.
It is included in the full RC gate's existing installer checkpoint.

Local preliminary evidence:
`results/docs-install-audit-53e42121af294e20ba7ee8e6a7849ac4/fixture.json` and
`results/installer-31d066ecb4b04776bceb5375e3525efe/report.json`.
The fixture reused unchanged released game payloads with revised installer/docs;
it is not a distributable replacement for 0.2.5.

Final validation completed at **2026-09-13 05:09 UTC**:

- All **16 local gates passed** for audit package `0.2.5-rc.10`, including
  actual Unity Mono, the unchanged real drive corpus, packaging and **37 new
  installer assertions** (43 in the combined installer checkpoint).
- Identity: `0.2.5-rc.10+5a3c369f90209990e77c38e0ee95455484eda3dd.clean`.
- ZIP SHA-256: `C49E48666C258DB582B8D381F7BE827E24A63145585BEB45E7089DD7FD5BE197`.
- Automated report:
  `results/rc-0.2.5-rc.10-aa83235524f04114a3e1b09dac4614be/automated.json`.
- Actual packaged-installer evidence:
  `results/installer-05bafcfd56ab4b8384a728b66b70de9c/report.json`.
- Link scan found 199 local targets in README/top-level docs: 180 source
  file/heading targets pass; 19 local-only evidence links are explicitly
  classified separately. External links and historical review receipts are not
  claimed as exhaustively checked. Report is `links.json` in the audit directory.
- Packaged README carries the correct version and is covered by manifest checks.
  RC10 is a local validation artifact only, not published or installed. The real
  installation still reports the released `0.2.5+c6242a0...clean` identity.

## Scope and remaining limits

Published release ZIPs/checksums are immutable. Updated GitHub docs are current;
revised installer/readme payloads enter the next package. The installed stable
game files/settings remain current because this audit changes neither.

No actual UMM GUI installation, new game drive or non-Steam install was performed.
The full hardware matrix remains open. The installer is not transactional; a
mid-copy failure requires a successful full retry before launching. Reports now
explain that recovery. No public issue replies were sent.
