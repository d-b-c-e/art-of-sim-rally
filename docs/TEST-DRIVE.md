# Candidate test drive

The release mod has **no recorder or playback feature**. A separate, removable
developer mod observes game signals; a standalone command replays them without
the game or wheel. Record a representative drive once, then reuse that case on
subsequent builds. Allow about 20–30 minutes for attended release checks.

The installed Stream Deck target currently uses published 0.2.3. The overnight
0.2.4 candidate is separate; install it explicitly with the game closed before
testing its camera-key and direct-input changes. Keep the 0.2.3 ZIP/settings backup.

1. **Prepare the candidate.** Close art of rally, retain the previous ZIP and
   Settings.xml, and install the exact ZIP identified by the successful
   `results/rc-*/automated.json`. Keep strength, smoothing, bindings and wheelbase
   settings unchanged. Note wheel, car, stage, game build and attached controllers.
2. **Install the developer probe**, with the game still closed:

   ```powershell
   ./tools/testing/Install-Recorder.ps1
   ```

   This adds only `Mods/ArtOfSimRally.DevRecorder`; it does not replace the release
   mod or its toolkit DLLs. Launch the game normally. UMM's log should say
   `Developer capture probe ready`. FFB must be enabled and connected for a force
   regression capture.
3. **Start recording from PowerShell**, with the game paused or at a menu:

   ```powershell
   ./tools/testing/Record-Drive.ps1 -Command Start
   ./tools/testing/Record-Drive.ps1 -Command Status
   ```

   Resume and drive normally. Include gentle corners, a controlled slide, bumps,
   braking, clutch, handbrake and shifter. Watch the first 15 seconds for stutter.
   Samples stay in memory; no CSV is written while driving. Status reports frame
   and force counts so a missing observation hook is visible.
4. **Exercise transitions.** Cycle stock views, bonnet and bumper, then stock
   views again. Finish the stage and watch a replay. Check pause/resume,
   alt-tab/return and mod disable/re-enable. Restart the same stage, then try a
   different one. Note which starts stutter and whether the wheel releases.
   Pause, then stop from PowerShell:

   ```powershell
   ./tools/testing/Record-Drive.ps1 -Command Stop
   ```

   The reply identifies a folder under
   `%LOCALAPPDATA%/ArtOfSimRally/dev-captures`. Keep its `manifest.xml`,
   `frames.csv` and `forces.csv` together. After a save error, keep the game open,
   remain paused and retry Stop; the probe retains its buffers.
5. **Replay and preserve the case**, outside the game:

   ```powershell
   dotnet run --project tools/testing/Replay -c Release -- --replay 'C:/path/to/capture'
   ./tools/testing/Add-RegressionCapture.ps1 -Capture 'C:/path/to/capture' -Corpus './results/regression-corpus' -Name cold-stage-r12
   dotnet run --project tools/testing/Replay -c Release -- --corpus './results/regression-corpus/index.json'
   ```

   Expected: zero device-force mismatches against the frozen original formula.
   Case names cannot overwrite existing evidence. Frame timing and native-call
   acceptance/rejection counts are reported when available; matching arithmetic
   does not establish physical torque delivery.
6. **Remove the probe and check the shipped setup.** Close the game, then run:

   ```powershell
   ./tools/testing/Install-Recorder.ps1 -Uninstall
   ```

   Relaunch and confirm normal loading, camera transitions and steering with the
   probe absent. Choose your wheel explicitly, exercise learned ranges, pause,
   quit and relaunch to check GUID, binding and tune persistence. If practical,
   test launching without the saved wheel: FFB must not choose another device.

Save a support file and notes. Check SimHub from start line through driving,
pause, finish and exit. Capture screenshots/video for camera problems. If a PS5
pad is available, compare with it attached and absent and inspect ChangeCamera
bindings; that was relevant to issue #1's original report.

If stutter persists, compare capture off and then a mod-disabled run with the same
settings. Different drives supply timing evidence, not deterministic physics
playback. Stop if force behavior is unexpected and record the situation. Keep tune
changes labelled so comparisons use the same settings.

Fill in the exact candidate's `manual.json` with tester/rig, outcomes, notes and
hashed evidence. See [PRE-RELEASE-TESTING.md](PRE-RELEASE-TESTING.md) for gates and
limits. Signal replay does not automate full game input, rendering or wheel feel.

## Extra checks for 0.2.4

- Open Camera in Ctrl+F10; rebind Up/Down and Reset to unused keyboard keys.
  Test Escape/cancel, duplicate rejection, Clear and Restore numpad defaults.
  Try a bumper-only setup. Typing/capturing in the panel and modifier chords
  must not adjust the mount; close the panel, release keys, then use the new keys.
- Adjust a mount, switch straight to stock view, then pause. Quit and relaunch:
  the edit and custom keys should persist even though the mounted view stopped
  updating. Inspect logs for a successful save, without a line for every held-key
  frame. Locked-file retry itself has an offline reproduction/test.
- With the car stationary, check direct Handbrake live values at rest, quarter,
  half and full pull, then release. Use full travel before Flip. Test Flip and
  double Flip, and restart with a deliberately flipped axis. Steering should
  invert around center; pedals invert between endpoints. Restore desired mappings.
- Test a reader disconnect/reconnect or device reopen while parked: stale throttle
  or handbrake must not remain held. When Assign begins during a failed read,
  recovery should establish a rest sample before waiting for deliberate movement.

T300 rotation needs the separate A/B procedure in
[the signal audit](research/2026-09-08-wheel-signals.md), on that hardware. New
effects and the telemetry sampling corrections (KI-16) are not part of this RC.
