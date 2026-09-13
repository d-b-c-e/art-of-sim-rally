# Art of Sim Rally — SimHub landing thud

Experimental companion for the matching 0.2.5 candidate. It receives a separate
landing cue from the game; it never changes Forza motion or wheel feedback.

1. Exit SimHub. Run `Install-SimHub.ps1` (PowerShell). It verifies the package,
   backs up settings/plugin files, enables the plugin, and creates/selects a
   separate **Art of Sim Rally - landing thud** FH5 profile. Your original
   profile and global gain are preserved. Reinstallation preserves edits to the
   existing landing profile. Custom output mappings may need manual routing.
2. Start SimHub and select Forza Horizon 5, as for ordinary mod telemetry.
3. In the game's mod panel, open **Telemetry**, enable telemetry and
   **ButtKicker landing thud (SimHub)** while paused. Start at **Shaker landing
   strength 50**; its maximum is 100. The wheel landing slider is independent.
4. Drive a known jump. Expect a short low-frequency pulse. Compare with the
   shaker landing toggle off, preserving all other settings. Check amplifier
   clipping before increasing gain; do not maximize every effect together.

The custom effect uses 30 Hz. The property supplies a 180 ms envelope: 10 ms
attack, 30 ms hold, 140 ms squared decay. Adjust frequency/effect gain in SimHub
to suit the mount. These are experimental starting values, not calibrated impact
force. No physical waveform is measured or guaranteed.

Available properties:

- `ArtOfSimRallyHaptics.LandingPulse`: 0..100; expires independently of game ticks.
- `ArtOfSimRallyHaptics.Connected`: fresh side-channel frames received.
- `ArtOfSimRallyHaptics.AcceptedLandings`: new events accepted this plugin session.
- `ArtOfSimRallyHaptics.RejectedPackets`, `Status`: diagnostic state.

UDP is fixed to loopback port 20779. Packets carry an event ID and original
timestamp so repeated frames do not retrigger or extend a pulse. Pause, disable,
quit, focus loss, invalid data and stale traffic clear output. Joining/restarting
the receiver establishes a baseline without replaying an existing event. The
plugin owns no sound device; only the enabled ShakeIt custom effect produces sound.

Retest: large/small jump, ordinary bumps, pause, restart, finish, alt-tab and quit.
There must be no repeating thud or latched buzz. Check regular speed/RPM gauges
and wheel feedback too. Send a support file while paused and the SimHub counters.

For a short isolated comparison, disable other effects in the new profile;
the original profile remains available. The installer disables general Impacts,
Jump landing and Road impacts only in its new profile to avoid duplicate cues.

No SimHub/game assemblies are included. The plugin uses SimHub's installed SDK.
Licensed under the repository's MIT license. Do not publish this local candidate
as a stable release; its attended checks and toolkit publication are pending.
