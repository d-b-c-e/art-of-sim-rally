using System.Text.Json;
using System.Xml.Serialization;
using ArtOfSimRally.Mod;
using UnityEngine;
using Host = ArtOfSimRally.Mod.Main;
using Clock = ArtOfSimRally.Mod.Time;
using Keys = ArtOfSimRally.Mod.Input;

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static Settings Saved()
    {
        using var input = File.OpenRead(Host.Path);
        return (Settings)new XmlSerializer(typeof(Settings)).Deserialize(input);
    }
    static void Tick(float time) { Clock.unscaledTime = time; CameraTuner.Flush(); }
    static readonly KeyCode[] CustomKeys = { KeyCode.Q, KeyCode.W, KeyCode.E, KeyCode.R, KeyCode.T,
        KeyCode.Y, KeyCode.U, KeyCode.I, KeyCode.O, KeyCode.P, KeyCode.H };
    static void Bindings()
    {
        Host.Settings = new Settings(); var cfg = Host.Settings;
        var defaults = new[] { KeyCode.Keypad8, KeyCode.Keypad2, KeyCode.Keypad9, KeyCode.Keypad7,
            KeyCode.Keypad4, KeyCode.Keypad6, KeyCode.Keypad1, KeyCode.Keypad3, KeyCode.KeypadPlus,
            KeyCode.KeypadMinus, KeyCode.Keypad0 };
        Check(CameraKeys.Bindings.Select(b => b.Get(cfg)).SequenceEqual(defaults), "legacy defaults changed");
        using (var xml = new StringReader("<Settings><BonnetHeight>1.25</BonnetHeight></Settings>"))
        {
            var legacy = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(xml);
            Check(legacy.BonnetHeight == 1.25f && CameraKeys.Bindings.Select(b => b.Get(legacy)).SequenceEqual(defaults),
                "legacy settings without key fields lost defaults");
        }
        Check(!CameraKeys.HandleKey(cfg, KeyCode.Q, false), "capture consumed a key without listening");
        CameraKeys.Begin(0);
        Check(CameraKeys.HandleKey(cfg, KeyCode.Escape, false) && CameraKeys.Listening == -1 && cfg.KeyUp == defaults[0], "escape changed binding");
        CameraKeys.Begin(0); CameraKeys.Cancel();
        Check(CameraKeys.Listening == -1 && cfg.KeyUp == defaults[0], "cancel changed binding");
        CameraKeys.Begin(0);
        foreach (var key in new[] { KeyCode.None, KeyCode.LeftShift, KeyCode.RightControl, KeyCode.LeftAlt,
            KeyCode.AltGr, KeyCode.LeftCommand, KeyCode.RightWindows, KeyCode.Mouse0, KeyCode.JoystickButton0, (KeyCode)9999 })
            Check(CameraKeys.HandleKey(cfg, key, false) && CameraKeys.Listening == 0 && cfg.KeyUp == defaults[0], "invalid/modifier/device key accepted: " + key);
        Check(CameraKeys.HandleKey(cfg, KeyCode.F10, true) && CameraKeys.Listening == 0 && cfg.KeyUp == defaults[0], "chord stored as bare key");
        Check(CameraKeys.HandleKey(cfg, KeyCode.Keypad2, false) && CameraKeys.Status.Contains("Down") && cfg.KeyUp == defaults[0], "duplicate silently replaced mapping");
        for (int i = 0; i < CustomKeys.Length; i++)
        {
            CameraKeys.Begin(i);
            Check(CameraKeys.HandleKey(cfg, CustomKeys[i], false) && CameraKeys.Listening == -1, "keyboard rebind failed");
        }
        Check(new[] { cfg.KeyUp, cfg.KeyDown, cfg.KeyForward, cfg.KeyBack, cfg.KeyLeft, cfg.KeyRight,
            cfg.KeyPitchDown, cfg.KeyPitchUp, cfg.KeyFovUp, cfg.KeyFovDown, cfg.KeyReset }.SequenceEqual(CustomKeys), "binding table writes wrong XML field");
        CameraTuner.Flush(shutdown: true);
        Check(CameraKeys.Bindings.Select(b => b.Get(Saved())).SequenceEqual(CustomKeys), "custom keys lost in XML roundtrip");
        CameraKeys.Clear(cfg, 0); CameraTuner.Flush(shutdown: true);
        Check(Saved().KeyUp == KeyCode.None && cfg.KeyDown == KeyCode.W && CameraKeys.Name(cfg.KeyUp) == "Unbound", "clear changed other key or failed persistence");
        cfg.BonnetHeight = 9; CameraKeys.Reset(cfg);
        Check(CameraKeys.Bindings.Select(b => b.Get(cfg)).SequenceEqual(defaults) && cfg.BonnetHeight == 9, "reset keys also reset mount or missed binding");
        for (int i = 0; i < CustomKeys.Length; i++) { CameraKeys.Begin(i); CameraKeys.HandleKey(cfg, CustomKeys[i], false); }
        cfg.BonnetCameraEnabled = false; cfg.BumperCameraEnabled = true;
        Check(CameraKeys.Available(cfg), "bumper-only setup cannot rebind");
        cfg.BumperCameraEnabled = false; Check(!CameraKeys.Available(cfg), "disabled mounts expose active tuner");
        cfg.BonnetCameraEnabled = cfg.BumperCameraEnabled = true;
    }
    static float[] MountValues(Settings s, bool bumper) => bumper
        ? new[] { s.BumperHeight, s.BumperForward, s.BumperSide, s.BumperPitch, s.BumperFOV }
        : new[] { s.BonnetHeight, s.BonnetForward, s.BonnetSide, s.BonnetPitch, s.BonnetFOV };
    static void KeyEffects()
    {
        var cfg = Host.Settings;
        Keys.Release(); CameraTuner.Update(BonnetCamera.View.Bonnet); // release the capture key latch
        int[] targets = { 0, 0, 1, 1, 2, 2, 3, 3, 4, 4 };
        int[] directions = { 1, -1, 1, -1, -1, 1, 1, -1, 1, -1 };
        foreach (bool bumper in new[] { false, true })
        {
            var view = bumper ? BonnetCamera.View.Bumper : BonnetCamera.View.Bonnet;
            for (int i = 0; i < 10; i++)
            {
                var before = MountValues(cfg, bumper); var other = MountValues(cfg, !bumper);
                Keys.Held.Add(CustomKeys[i]); CameraTuner.Update(view); Keys.Release();
                var after = MountValues(cfg, bumper);
                for (int field = 0; field < before.Length; field++)
                {
                    float delta = field == targets[i] ? directions[i] * Clock.unscaledDeltaTime * (i < 6 ? cfg.TuneMoveSpeed : cfg.TuneAngleSpeed) : 0;
                    Check(Math.Abs(after[field] - before[field] - delta) < .00001f, "mapped key changed wrong mount field");
                }
                Check(MountValues(cfg, !bumper).SequenceEqual(other), "key affected inactive mount");
            }
            var inactive = MountValues(cfg, !bumper);
            Keys.Pressed.Add(KeyCode.H); CameraTuner.Update(view); Keys.Release();
            Check(MountValues(cfg, bumper).SequenceEqual(MountValues(new Settings(), bumper)) &&
                MountValues(cfg, !bumper).SequenceEqual(inactive), "reset affected wrong mount");
        }
        float height = cfg.BonnetHeight;
        Host.SettingsVisible = true; Keys.Held.Add(KeyCode.Q);
        CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "panel editing nudged mount");
        Host.SettingsVisible = false; CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "panel close key leaked into tuner");
        Keys.Release(); CameraTuner.Update(BonnetCamera.View.Bonnet);
        CameraKeys.Begin(0); Keys.Held.Add(KeyCode.Z); Keys.Pressed.Add(KeyCode.H);
        CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "binding capture reset mount");
        Keys.Pressed.Clear(); CameraKeys.HandleKey(cfg, KeyCode.Z, false);
        CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "captured held key moved mount");
        Keys.Release(); CameraTuner.Update(BonnetCamera.View.Bonnet); Keys.Held.Add(KeyCode.Z);
        CameraTuner.Update(BonnetCamera.View.None); Check(cfg.BonnetHeight == height, "key moved stock view");
        cfg.CameraTuningKeys = false; CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "disabled tuner moved mount");
        cfg.CameraTuningKeys = true; Host.Enabled = false; CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "disabled mod moved mount");
        Host.Enabled = true; CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight > height, "released then re-pressed key never resumed tuning");
        height = cfg.BonnetHeight;
        Keys.Held.Add(KeyCode.LeftControl); CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "Ctrl chord activated a single-key binding");
        Keys.Held.Remove(KeyCode.LeftControl); CameraTuner.Update(BonnetCamera.View.Bonnet);
        Check(cfg.BonnetHeight == height, "releasing modifier leaked held key into tuner");
        Keys.Release(); CameraTuner.Flush(shutdown: true);
    }
    static void Saves()
    {
        SettingsPersistence.Write(Host.Settings, Host.Path);
        string before = File.ReadAllText(Host.Path);
        GameState.IsDriving = true; Keys.Held.Add(KeyCode.Keypad8);
        for (int i = 0; i < 100; i++)
        {
            Clock.unscaledTime = i * .02f;
            CameraTuner.Update(BonnetCamera.View.Bonnet); CameraTuner.Flush();
        }
        Check(Host.Saves == 0 && File.ReadAllText(Host.Path) == before, "held adjustment saved during driving");
        Check(ModLog.Messages.Count == 0, "held adjustment flooded log");
        Check(Host.Settings.BonnetHeight > Saved().BonnetHeight, "held key never adjusted camera");
        Keys.Release();
        // Leave the mounted view: only the persistent callback runs now.
        Tick(3.1f); Check(Host.Saves == 0, "debounced save still wrote while driving");
        GameState.IsDriving = false;
        using (var locked = File.Open(Host.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            Tick(3.1f);
        Check(Host.Saves == 1 && ModLog.Messages.Count == 0, "locked write reported success");
        Check(File.ReadAllText(Host.Path) == before, "failed save damaged settings");
        Check(Directory.GetFiles(Path.GetDirectoryName(Host.Path), "*.tmp").Length == 0, "failed save left temp file");
        Host.Settings.BumperHeight = 2.25f; CameraTuner.MarkDirty();
        Tick(8.09f); Check(Host.Saves == 1, "new edit bypassed retry backoff");
        Tick(8.1f);
        Check(Host.Saves == 2 && ModLog.Messages.SequenceEqual(new[] { "Camera settings saved." }), "retry failed or logged false success");
        Check(Saved().BonnetHeight == Host.Settings.BonnetHeight && Saved().BumperHeight == 2.25f, "retry lost latest edit");
        Tick(20); Check(Host.Saves == 2, "clean tuner saved twice");

        Host.Settings.BonnetHeight = 3; CameraTuner.MarkDirty();
        Tick(20.99f); Check(Host.Saves == 2, "debounce saved early");
        Host.Enabled = false; GameState.IsDriving = true; Tick(21);
        Check(Host.Saves == 3 && Saved().BonnetHeight == 3, "mod disable lost pending edit");
        Host.Enabled = true; GameState.IsDriving = false;
        Host.Settings.BonnetHeight = 4; CameraTuner.MarkDirty();
        using (var locked = File.Open(Host.Path, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
            CameraTuner.Flush(shutdown: true);
        Check(Host.Saves == 4 && ModLog.Messages.Count == 2, "forced failure reported success");
        GameState.IsDriving = true; CameraTuner.Flush(shutdown: true);
        Check(Host.Saves == 5 && Saved().BonnetHeight == 4, "shutdown did not bypass debounce/backoff");
        CameraTuner.Flush(shutdown: true); Check(Host.Saves == 5, "double shutdown saved twice");
        GameState.IsDriving = false;
    }
    static int Main()
    {
        try
        {
            var directory = Path.GetFullPath(Path.Combine("results", "camera-tuning-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory); Host.Path = Path.Combine(directory, "Settings.xml");
            Saves(); Bindings(); KeyEffects();
            Console.WriteLine(JsonSerializer.Serialize(new { status = "passed", assertions })); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
