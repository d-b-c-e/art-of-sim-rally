# Feature backlog

## Center menus on the middle monitor

**Requested:** 2026-09-24. **Updated:** 2026-09-27. **Status:** partial.

The optional menu-centering setting moves the title prompt and main menu
controls to the middle panel of an exact three-panel-wide output. The title and
main menu were smoke-tested. Pause, settings, dialogs, pointer hit targets,
controller focus, and restoring every position after toggling the feature still
need an attended check. See [experiment 006](experiments/006-centered-menu-and-presentation.md).

Acceptance: interactive content is on the center panel; keyboard, wheel or
controller, and pointer focus follow it; all menu transitions work; and turning
the setting off restores stock placement. Keep gameplay HUD routing separate.

## Surround presentation

**Updated:** 2026-09-27. **Status:** open.

The 0.3.11 direct-viewport renderer restored much of the vegetation and
lighting missing from the older wide-output path, but the attended Surround
drive still showed screen tearing. A conclusive stock Surround comparison is
pending. See [known issues](KNOWN-ISSUES.md) and
[experiment 008](experiments/008-attended-separate-display-check.md).
