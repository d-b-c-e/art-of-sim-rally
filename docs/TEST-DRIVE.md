# Candidate test drive

Use the exact ZIP named by the latest successful `results/rc-*/automated.json`.
Do not install an intermediate build from `bin/`. Estimated hands-on time:
20–30 minutes, longer if reproducing a problem.

1. **Prepare.** Close art of rally, keep a copy of the old ZIP and Settings.xml,
   then install the candidate using your normal route. Leave strength, smoothing,
   wheelbase settings and bindings unchanged. Note wheel, car, stage, controller
   attachments and game build. Confirm the mod version in its support file.
2. **Record.** Before driving (or while paused), open Ctrl+F10 → Devices and
   troubleshooting → Start drive capture. The recorder keeps samples in memory;
   it writes the files after you pause and stop the capture.
3. **Cold stage.** Drive a familiar stage. Pay attention to the first 15 seconds,
   then try gentle corners, a controlled slide, bumps, braking, clutch, handbrake
   and shifter. Note stutters and any change in steering feel at the same tune.
4. **Cameras and lifecycle.** Cycle the stock cameras, bonnet and bumper; repeat
   the stock views after using a mounted view. Finish the stage and watch a replay.
   Check pause/resume, alt-tab/return and disabling/re-enabling the mod. The wheel
   should release when driving stops and resume without a stale force.
5. **Repeat/new stage.** Restart the same stage, then load a different one. Note
   which starts stutter. Pause and choose Stop and save drive capture. Keep the
   complete folder and its manifest, frames.csv and forces.csv. The panel shows
   its location under `%LOCALAPPDATA%\ArtOfSimRally\captures`.
6. **Persistence.** Choose your wheel explicitly in the picker to save its GUID.
   Exercise the full learned steering/pedal ranges, pause, quit, and relaunch.
   Confirm the wheel selection, ranges and tune survived. If practical, test a
   launch with that saved wheel disconnected: FFB should report it missing.
7. **Evidence.** Save a support bundle and short notes. Screenshots/video are
   useful for replay and finish-camera problems. If a PS5 pad is available, repeat
   the camera check with it attached and absent, and inspect ChangeCamera bindings.
   Confirm SimHub/your normal telemetry consumers work from the start line through
   pause, finish and exit.

Start with the same settings used before adoption. If you reduce strength for an
initial check, record that value and compare using the same value. Stop driving if
force behavior is unexpected; note the situation rather than repeatedly testing it.

## What the recording tells us

Offline replay runs each recorded signal through the original 0.2.2 formula and
`AxleForceCurve@1` from the pinned toolkit. It compares normalized outputs and the
exact integers sent to the native API, carrying separate smoothing histories and
honoring recorded resets. Expected result: zero device-force mismatches. Existing
schema-1 captures remain usable for per-row comparisons; schema 2 checks continuous
filter history as well. Neither path sends force to the wheel.

```powershell
dotnet run --project tests/Regression/Regression.csproj -c Release -- --replay "C:/path/to/capture-folder"
```

Timing analysis reports the first 15 seconds of each driving segment separately
from later frames. If stutter persists, compare a similar run with capture off,
then with the mod disabled. Different drives are useful timing evidence, but are
not deterministic physics replays.

Calculated force is not a measurement of physical torque. A matching replay does
not validate camera pixels, successful Unity/Mono loading, force delivery, or the
recorder's in-game performance. Those need your observations and support log.

Fill in the candidate's `manual.json` with tester/rig, a result and observations
for every case, plus paths and SHA-256 hashes for its evidence. An untested or failed
case keeps the release gate closed. See [PRE-RELEASE-TESTING.md](PRE-RELEASE-TESTING.md)
for the complete evidence workflow.
