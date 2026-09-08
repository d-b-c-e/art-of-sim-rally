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
            Saves();
            Console.WriteLine(JsonSerializer.Serialize(new { status = "passed", assertions })); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
