# Session Notes
<!-- Overwritten each session; previous handoffs remain in git history. -->

- Date: 2026-09-13 UTC
- Branch: main (feature branch fast-forwarded and both pushed)

## Work completed

- Owner accepted RC8 wheel/shaker testing and explicitly requested 0.2.5 release.
- ButtKicker landings improved with built-in effects at 30 Hz; amplifier CLIP
  seen. Keep tested gain. No SimHub helper; RC9 was withdrawn and removed.
- LandingEffectsEnabled now defaults true at the existing strength 5.
  Settings regression covers new XML, absent fields and explicit saved opt-outs.
- Official toolkit v0.13.0 published upstream from frozen dd0ef20... source.
  Sync-Toolkit pins it here; all five vendored files match tested RC8 exactly.
- Published stable/latest v0.2.5 from c6242a0a163315f7a390d3860c6204b4ca215619.
  All 16 final local gates passed, including actual Mono and the real corpus.
- Installed the exact final package and verified published ZIP/checksum downloads.
  Saved wheel landing enabled/20, SimHub profile/gains and separate probe retained.
  Game closed, SimHub running, no recorder active; Steam/Stream Deck target unchanged.
- Fixed final runner parsing of Mono's trailing BOM-only output line; earlier
  attempts stopped before packaging. Fresh full final gate passed.
- Updated README, defaults/help, release notes, roadmap and evidence limits.

## Decisions

- Shaker tuning uses ordinary Forza telemetry and built-in SimHub effects.
- Keep original steering arithmetic, wheel burst envelope and user settings.
- Publish/build locally; preserve exact archive hashes, tag the recorded source.
- Owner overall acceptance is not a completed per-case hardware matrix.

## Remaining validation

- Reporter hardware, full lifecycle/gauge and camera combinations remain pending,
  as does a separate final-artifact drive; owner acceptance is recorded separately.
- Post-publication GitHub check: only existing issue #1, unchanged since Sept 6.
  No issue reply sent. Release and deployment records updated.

## Evidence

- results/release-025-owner-acceptance-fcd2882fda8c4e54883e1c6a49b33884/acceptance.json
- results/rc-0.2.5-ab78d6a9af8446b9ac562904e7062c6a/automated.json
- results/release-025-install-2bd3016bb6334b8ea4effb6142c15c2b/receipt.json
- results/release-025-published-b7832204b25e4f5cb6d1db48bd0adc3f/verification.json
- Release ZIP SHA256 8A7F9FC044058B0890F7323138E3DFCBC998B31C67C57E14272D410C6BFCA502.
- docs/reviews/2026-09-12-builtin-shaker-correction.md
- Toolkit archive SHA256 2F73F5427465CD85E8D969EE7ECAF7D9C2CB08ED923DA80C4AC7C7234AF1B0CC.
