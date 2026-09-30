# Experiment 005: Surround startup hang investigation

## Incident — 2026-09-24, about 23:02 America/Chicago

The user first launched Art of Rally on three independent displays, exited it,
enabled NVIDIA Surround, and launched it again. They saw a white image on one
panel and green images with a pixelated cursor on the other two. Alt+F4 and
Ctrl+Alt+Del did not recover the computer; the user forced a reboot.

Evidence preserved in ignored `artifacts/incident-20260924-2302/` includes
`Player-prev.log`, `Player.log`, `status.json`, `render-diagnostics.json`, and the
preparation backup `Settings-before-baseline.xml`.

- The first game's `Player-prev.log` ends with normal quit callbacks at 22:58:35.
  Windows recorded display mode enumeration at 22:58:47 and 22:58:50.
- The next game's log reports a 7680x1440 current display resolution. At
  23:02:17 the diagnostic adapter observed a 2560x1440 game output in exclusive
  fullscreen and correctly reported output mismatch. At 23:02:21 status had
  advanced to `STAGE_CAMERA_WAIT` with zero active triple-view cameras; the
  exact resolution guard had therefore cleared. The game log stops at 23:02:23
  without a managed exception or a normal quit message.
- Windows logged an unclean boot at 23:06:29 (Kernel-Power 41, bugcheck code 0,
  power-button timestamp present). Event 6008 followed. No current display
  recovery event was found in the accessible System log. WER replayed older
  WATCHDOG reports; their report timestamps are not proof of a new watchdog
  event from this incident. The protected WATCHDOG dump directory could not
  be inspected with the current permissions.
- ETS2 was also launched briefly and its telemetry plugin
  `SHETS2Telemetry_x64.dll` faulted at 23:01:58. The user recalls closing ETS2
  normally. This is a separate process error and does not establish the cause
  of the subsequent Art of Rally/system hang.

The display/fullscreen transition is the leading investigation target. The
triple compositor had not recorded an active camera or a successful frame when
logging stopped, but the mod cannot be completely excluded from an unflushed
hard-hang sequence. No dump or stack trace identifies the root cause.

## Preparation for the next attended run

The game is closed and Windows currently reports three independent 2560x1440
monitors. The installed adapter remains at 0.2.1 for passive diagnostics, but
its saved `EnableThreeViewPrototype` setting was changed from `true` to `false`
with `EnableCenterPanelPreview=false` and
`AllowOutputResolutionMismatch=false` unchanged. The prior XML is backed up in
the incident artifacts folder. No driver, display topology, or game fullscreen
setting was changed during this preparation.

The next test should start with Surround already stable at 7680x1440, no other
game running, and the experimental renderer off. Verify menu entry and a short
stock-camera drive first. Do not attribute a successful menu launch to the
three-view renderer. If this run is stable, restore the three-view setting as a
separate attended step and compare diagnostics. If it hangs again before
gameplay, investigate Unity exclusive-fullscreen/Surround and driver behavior
before modifying the compositor.
