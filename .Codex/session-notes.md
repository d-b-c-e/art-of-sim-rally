# Session Notes
<!-- Overwritten each session; previous handoffs remain in git history. -->

- Date: 2026-09-13 UTC
- Branch: codex/overnight-0.2.5

## Work completed

- Owner accepted RC8 wheel/shaker testing and explicitly requested 0.2.5 release.
- ButtKicker landings improved with built-in effects at 30 Hz; amplifier CLIP
  seen. Keep tested gain. No SimHub helper; RC9 was withdrawn and removed.
- LandingEffectsEnabled now defaults true at the existing strength 5.
  Settings regression covers new XML, absent fields and explicit saved opt-outs.
- Official toolkit v0.13.0 published upstream from frozen dd0ef20... source.
  Sync-Toolkit pins it here; all five vendored files match tested RC8 exactly.
- Current pre-release installation is restored RC8, wheel landing enabled at
  saved strength 20. Game closed, SimHub running, no recorder active.
- Updated README, defaults/help, release notes, roadmap and evidence limits.

## Decisions

- Shaker tuning uses ordinary Forza telemetry and built-in SimHub effects.
- Keep original steering arithmetic, wheel burst envelope and user settings.
- Publish/build locally; preserve exact archive hashes, tag the recorded source.
- Owner overall acceptance is not a completed per-case hardware matrix.

## Remaining work

- Finish final local gate, install exact package, publish/tag and verify download.
- Record final release/deployment receipts in docs/LOCAL-DEPLOYMENT.md and
  docs/reviews/2026-09-13-release-0.2.5.md after completion.
- Reporter hardware, full lifecycle/gauge and camera combinations remain pending.

## Evidence

- results/release-025-owner-acceptance-fcd2882fda8c4e54883e1c6a49b33884/acceptance.json
- docs/reviews/2026-09-12-builtin-shaker-correction.md
- Toolkit archive SHA256 2F73F5427465CD85E8D969EE7ECAF7D9C2CB08ED923DA80C4AC7C7234AF1B0CC.
