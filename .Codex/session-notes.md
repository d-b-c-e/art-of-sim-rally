# Session Notes
<!-- Overwritten each session; previous handoffs remain in git history. -->

- Date: 2026-09-13 UTC
- Branch: main

## Work completed

- Prior task published/installed stable 0.2.5 from c6242a0, official toolkit
  v0.13.0; owner accepted RC8 wheel/shaker testing and default-on landing at 5.
  Saved owner enabled/20 and built-in SimHub 30 Hz profile/gains are retained.
- Current owner request: audit/simplify documentation and installation, then
  commit and push. README is now a short entry point; SETUP, docs index and
  BUILDING separate player setup from development/history.
- Updated stale version/default/toolkit references, clear USB handbrake/logging/
  camera/SimHub help, install/update/remove/custom paths and load verification.
- Standalone ZIP readme is tools/installer/README.txt; no absent local-doc links.
- Fixed KI-33: uninstall without UMM, literal paths, batch arguments/exit codes,
  Windows PowerShell module path; clearer verification/copy recovery messages.
- Test-Installer passes 37 assertions through real batch/Windows PowerShell
  entry points in isolated fake game folders, now included in Test-Rc.

## Decisions and limits

- No game/force code changed. Installed stable 0.2.5 and published ZIP stay intact;
  installer/template changes go into the next package. No deployment is needed
  for source documentation and installer-only work.
- No SimHub helper, new gain, game launch or recorder session. Full hardware
  matrix/third-party wheel and TSS checks remain pending.
- Existing installer isn't transactional; successful full retry is required
  after a mid-copy failure. Recovery guidance is explicit.

## Finishing / evidence

- Full local packaging gate and final link check to be recorded in
  docs/reviews/2026-09-13-docs-install-audit.md before final push.
- Preliminary fixture: results/docs-install-audit-53e42121af294e20ba7ee8e6a7849ac4.
- Installer: results/installer-31d066ecb4b04776bceb5375e3525efe/report.json.
- GitHub has only unchanged issue #1; no open PRs or new replies sent.
- Release/deployment identities: docs/LOCAL-DEPLOYMENT.md and
  docs/reviews/2026-09-13-release-0.2.5.md.
