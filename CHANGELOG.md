# Changelog

## Unreleased — 0.3.12 candidate

- Added **Override field of view**. With it off, all three projections follow
  the game's live camera FOV; with it on, the existing slider and saved value
  remain in control. Existing installations keep override on until changed.
- Offline geometry checks cover the observed 75° game FOV with the current 70°
  side-screen example. In-game visual validation is still pending.

## 0.3.11 — 2026-09-27

First packaged release candidate for art of rally triple-screen support.

- Added independent setup in the mod: Off, Single wide display, Three separate
  displays, screen measurements, and one field-of-view control for all views.
- Added direct side-display and wide-viewport rendering, including camera effect
  matching, shared vegetation visibility, and finish-camera handoff.
- Added a standalone installer, update and uninstall paths, package integrity
  checks, and player setup and troubleshooting guides.
- Confirmed aligned seams and stable vegetation in the attended separate-display
  drive. The attended Surround drive had improved lighting and vegetation, but
  **visible tearing remains unresolved**. See [known issues](docs/KNOWN-ISSUES.md).
