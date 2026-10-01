using System.Reflection;
using ArtOfSimRally.Mod;
using Mod = ArtOfSimRally.Mod.Main;
using Native = Dbce.Wheel.Ffb.WheelFfbNative;

static class ShiftTests
{
    static int assertions;
    static void Check(bool value, string why) { assertions++; if (!value) throw new Exception(why); }

    sealed class Output : ILandingOutput
    {
        public int Creates, Plays, Stops, Releases, Hz, Duration;
        public ImpactKind? Active;
        public float Magnitude;
        public int Create(ImpactKind kind, int hz, int durationMs)
        { Creates++; Hz = hz; Duration = durationMs; return kind == ImpactKind.Crash ? 2 : 1; }
        public bool Play(ImpactKind kind, int slot, float magnitude, float hz)
        { Check(Active == null, "shift overlapped another effect"); Plays++; Active = kind; Magnitude = magnitude; return true; }
        public bool Stop(ImpactKind kind, int slot)
        { Check(Active == kind, "stopped the wrong effect"); Stops++; Active = null; return true; }
        public void Release() { Releases++; Active = null; }
    }

    static void CompleteShift(Drivetrain drivetrain, int from, int to)
    {
        var after = typeof(ShiftController).GetMethod("After", BindingFlags.Static | BindingFlags.NonPublic);
        Check(after != null, "gear-completion patch missing");
        drivetrain.gear = to;
        after.Invoke(null, new object[] { drivetrain, from });
    }

    public static int Run()
    {
        Check(!ShiftController.Engaged(2, 1, 1), "neutral is not engagement");
        Check(ShiftController.Engaged(1, 3, 1), "neutral-to-gear engagement missed");
        Check(ShiftController.Engaged(2, 3, 1), "immediate shifter engagement missed");
        Check(ShiftController.Engaged(1, 0, 1), "reverse engagement missed");
        Check(!ShiftController.Engaged(3, 3, 1) && !ShiftController.Engaged(3, -1, 1), "unchanged/invalid gear played");

        var output = new Output();
        var mixer = new ImpactMixer(output);
        mixer.Prepare(false, false, true, true, true);
        Check(output.Creates == 1 && output.Hz == 25 && output.Duration == 120, "shift did not prepare a finite sine while idle");
        Check(!mixer.Available(ImpactKind.Landing) && !mixer.Available(ImpactKind.Crash) && mixer.Available(ImpactKind.Shift), "shift-only availability incorrect");
        Check(mixer.Trigger(ImpactKind.Shift, 1, 5, 1) == ImpactResult.Accepted && output.Magnitude == .05f, "shift cue output incorrect");
        Check(mixer.Trigger(ImpactKind.Shift, 1, 20, 1.01) == ImpactResult.Suppressed && output.Plays == 1, "shift retriggered active pulse");
        mixer.Prepare(false, false, false, true, true);
        Check(output.Stops == 1 && output.Releases == 1 && !mixer.Available(ImpactKind.Shift), "disabling shifts retained force");
        mixer.Shutdown();

        output = new Output(); mixer = new ImpactMixer(output);
        mixer.Prepare(true, true, true, true, true);
        Check(output.Creates == 2, "shift allocated another sine slot alongside landing");
        Check(mixer.Trigger(ImpactKind.Shift, 1, 20, 2) == ImpactResult.Accepted && output.Magnitude == .2f, "shift cap incorrect");
        Check(mixer.Trigger(ImpactKind.Landing, 1, 5, 2.01) == ImpactResult.Accepted && output.Stops == 1 && output.Active == ImpactKind.Landing,
            "landing did not replace lower-priority shift");
        Check(mixer.Trigger(ImpactKind.Shift, 1, 20, 2.02) == ImpactResult.Suppressed && output.Active == ImpactKind.Landing,
            "shift stole landing cue");
        Check(mixer.Trigger(ImpactKind.Crash, 1, 50, 2.03) == ImpactResult.Accepted && output.Stops == 2 && output.Active == ImpactKind.Crash,
            "crash could not replace landing");
        Check(mixer.Trigger(ImpactKind.Shift, 1, 20, 2.04) == ImpactResult.Suppressed && output.Active == ImpactKind.Crash,
            "shift stole crash cue");
        Check(LandingFeedback.MagnitudeFor(ImpactKind.Shift, 1, 100) == .2f, "malformed shift tune exceeded cap");
        mixer.Shutdown();

        // The production hook must ignore AI cars, intermediate neutral, pause,
        // focus loss and settings, even when a sine handle already exists.
        ImpactController.Shutdown(); Mod.Settings = new() { LandingEffectsEnabled = false, CrashEffectsEnabled = false,
            ShiftEffectsEnabled = true, ShiftStrength = 5, DiagnosticLogging = false };
        Mod.Enabled = true; Mod.SettingsVisible = false; FfbNative.Ready = true;
        UnityEngine.Application.isFocused = true; GameState.IsRestarting = false; GameState.IsDriving = false;
        ImpactController.Tick();
        var player = new CarDynamics(); GameState.ExistingManager.playerManager.carcontroller = player;
        var ai = new Drivetrain(); int plays = Native.Plays;
        GameState.IsDriving = true; UnityEngine.Time.realtimeSinceStartup = 10;
        CompleteShift(ai, 2, 3);
        CompleteShift(player.drivetrain, 2, 1);
        Check(Native.Plays == plays, "AI or neutral triggered shift rumble");
        CompleteShift(player.drivetrain, 1, 3);
        Check(Native.Plays == plays + 1 && Native.LastMagnitude == .05f, "player gear engagement did not play expected cue");
        ImpactController.Reset("test-reset");
        GameState.IsDriving = false; CompleteShift(player.drivetrain, 3, 4);
        UnityEngine.Application.isFocused = false; GameState.IsDriving = true; CompleteShift(player.drivetrain, 4, 5);
        UnityEngine.Application.isFocused = true; Mod.SettingsVisible = true; CompleteShift(player.drivetrain, 5, 4);
        Mod.SettingsVisible = false; GameState.IsRestarting = true; CompleteShift(player.drivetrain, 4, 3);
        GameState.IsRestarting = false; Mod.Settings.ShiftEffectsEnabled = false; CompleteShift(player.drivetrain, 3, 2);
        Check(Native.Plays == plays + 1, "inactive driving state played shift rumble");
        ImpactController.Shutdown();
        return assertions;
    }
}
