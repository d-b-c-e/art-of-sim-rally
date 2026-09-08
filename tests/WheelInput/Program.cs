using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;
using ArtOfSimRally.Mod;
using Device = Dbce.Wheel.Ffb.WheelFfbNative;
using Host = ArtOfSimRally.Mod.Main;
using Clock = ArtOfSimRally.Mod.Time;

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void Same(float actual, float expected, string message) => Check(Math.Abs(actual - expected) < .000001f, message);
    static object[] CarInput()
    {
        var args = new object[] { new AxisCarController(), .21f, .32f, -.43f, .54f, .65f, true };
        typeof(WheelInputPatch).GetMethod("Override", BindingFlags.Static | BindingFlags.NonPublic).Invoke(null, args);
        return args;
    }
    static void BoundOnlyHandbrake(float expected)
    {
        var values = CarInput();
        Same((float)values[4], expected, "handbrake did not reach game input as analog float");
        Check((float)values[1] == .21f && (float)values[2] == .32f && (float)values[3] == -.43f &&
            (float)values[5] == .65f && (bool)values[6], "handbrake overwrote unbound channels");
    }
    static void Setup(string binding)
    {
        WheelInput.Close(); Device.ReadOk = true; Device.ThrowRead = false; Device.FailOpen = false;
        Array.Clear(Device.Axes); Array.Clear(Device.Buttons);
        Host.Settings = new Settings { WheelInputEnabled = true, HandbrakeBinding = binding };
        Host.Enabled = true; Clock.realtimeSinceStartup += 20;
        GameEntryPoint.EventManager = new EventManager();
        WheelInput.LoadBindings(); WheelInput.Open();
    }
    static void Travel(int rest, int far)
    {
        Setup($"TSS fixture|0|axis:2|{rest}|{far}");
        foreach (float fraction in new[] { 0f, .25f, .5f, .75f, 1f, .5f, 0f })
        {
            int raw = rest + (int)((far - rest) * fraction); Device.Axes[2] = raw;
            WheelInput.Update(); float expected = (raw - rest) / (float)(far - rest);
            Same(WheelInput.Value(WheelInput.Channel.Handbrake), expected, "partial travel quantized");
            BoundOnlyHandbrake(expected);
        }
    }
    static void Lifecycle()
    {
        Setup("TSS fixture|0|axis:2|0|65535");
        Device.Axes[2] = 65535; WheelInput.Update(); BoundOnlyHandbrake(1);
        Device.ReadOk = false; WheelInput.Update(); BoundOnlyHandbrake(0);
        Device.ThrowRead = true; WheelInput.Update(); BoundOnlyHandbrake(0);
        Device.ThrowRead = false; Device.ReadOk = true; Device.Axes[2] = 32000;
        WheelInput.Update(); BoundOnlyHandbrake(32000f / 65535);
        GameEntryPoint.EventManager.status = EventStatusEnums.EventStatus.FINISHING_STAGE_ANIMATION;
        Same((float)CarInput()[4], .54f, "finish animation handbrake overwritten");
        GameEntryPoint.EventManager = null;
        Same((float)CarInput()[4], .54f, "missing manager handbrake overwritten");
        GameEntryPoint.EventManager = new EventManager();
        Host.Enabled = false; Same((float)CarInput()[4], .54f, "disabled mod overrode game");
        Host.Enabled = true;
        Host.Settings.WheelInputEnabled = false; Same((float)CarInput()[4], .54f, "disabled direct input overrode game");
        Host.Settings.WheelInputEnabled = true;
        WheelInput.Close(); Device.FailOpen = true; Clock.realtimeSinceStartup += 20; WheelInput.Update();
        BoundOnlyHandbrake(0); // Reader loss/reopen must not keep a stale full pull.
    }
    static void RangesAndAssignment()
    {
        Setup("TSS fixture|0|axis:2|500|500"); Device.Axes[2] = 60000; WheelInput.Update(); BoundOnlyHandbrake(0);
        Setup("TSS fixture|0|button:6|0|1"); Device.Buttons[6] = 128; WheelInput.Update(); BoundOnlyHandbrake(1);
        Device.Buttons[6] = 0; WheelInput.Update(); BoundOnlyHandbrake(0);
        Setup(""); Device.Axes[2] = 65535;
        WheelInput.BeginAssign(WheelInput.Channel.Handbrake);
        Device.Axes[2] = 40000; WheelInput.Update();
        Check(Host.Settings.HandbrakeBinding == "TSS fixture|0|axis:2|65535|40000", "reverse-axis assignment wrong");
        int saved = Host.Saves;
        Device.Axes[2] = 0; WheelInput.Update(); BoundOnlyHandbrake(1);
        Check(Host.Settings.HandbrakeBinding.EndsWith("|65535|0") && Host.Saves == saved, "range learning saved during input update");
        Device.Axes[2] = 32768; WheelInput.Update(); BoundOnlyHandbrake(32767f / 65535);
        Device.Axes[2] = 65535; WheelInput.Update(); BoundOnlyHandbrake(0);
        WheelInput.Clear(WheelInput.Channel.Handbrake);
        Same((float)CarInput()[4], .54f, "cleared binding overrode game");
    }
    static void FlipPersistence()
    {
        Setup("TSS fixture|0|axis:2|0|65535");
        WheelInput.Flip(WheelInput.Channel.Handbrake);
        Device.Axes[2] = 0; WheelInput.Update(); BoundOnlyHandbrake(1);
        Device.Axes[2] = 65535; WheelInput.Update(); BoundOnlyHandbrake(0);
        using (var stream = File.OpenRead(Host.Path)) Host.Settings = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
        WheelInput.LoadBindings(); Device.Axes[2] = 32768; WheelInput.Update(); BoundOnlyHandbrake(32767f / 65535);
        WheelInput.Flip(WheelInput.Channel.Handbrake);
        Check(Host.Settings.HandbrakeBinding == "TSS fixture|0|axis:2|0|65535", "double flip changed calibration");
        Host.Settings.SteerBinding = "TSS fixture|0|axis:0|32767|65535";
        WheelInput.LoadBindings(); WheelInput.Flip(WheelInput.Channel.Steer);
        WheelInput.LoadBindings();
        Check(WheelInput.IsBound(WheelInput.Channel.Steer), "flipped steering binding lost on restart");
        Device.Axes[0] = 65535; WheelInput.Update(); Same(WheelInput.Value(WheelInput.Channel.Steer), -1, "steering flip lost sign or calibration");
        WheelInput.Flip(WheelInput.Channel.Steer);
        Check(Host.Settings.SteerBinding == "TSS fixture|0|axis:0|32767|65535", "double steering flip changed range");
        foreach (string invalid in new[] { "TSS fixture|0|axis:2|0|65536", "TSS fixture|0|axis:2|65535|-1",
            "TSS fixture|0|axis:2|0|2147483647", "TSS fixture|0|axis:2|0|-2147483648" })
            Check(WheelInput.Binding.Parse(invalid) == null, "oversized calibration span accepted");
        Setup("TSS fixture|0|axis:2|32767|-1");
        Check(!WheelInput.IsBound(WheelInput.Channel.Handbrake), "virtual steering endpoint accepted as a physical pedal endpoint");
        Setup("TSS fixture|0|axis:2|0|65535"); Device.Axes[2] = 65535; WheelInput.Update();
        Host.Settings.HandbrakeBinding = "TSS fixture|0|axis:1|0|65535"; WheelInput.LoadBindings();
        BoundOnlyHandbrake(0);
    }
    static void AssignmentReadFailure()
    {
        Setup(""); Device.ReadOk = false; WheelInput.BeginAssign(WheelInput.Channel.Handbrake);
        Device.ReadOk = true; Device.Axes[2] = 65535; WheelInput.Update();
        Check(!WheelInput.IsBound(WheelInput.Channel.Handbrake) && WheelInput.Assigning.HasValue,
            "first successful read after failure was mistaken for lever movement");
        Device.Axes[2] = 40000; WheelInput.Update();
        Check(Host.Settings.HandbrakeBinding == "TSS fixture|0|axis:2|65535|40000", "recovered baseline did not bind real movement");
    }
    static int Main(string[] args)
    {
        try
        {
            var directory = Path.GetFullPath(Path.Combine("results", "wheel-input-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory); Host.Path = Path.Combine(directory, "Settings.xml");
            if (args.Contains("--flip-only")) FlipPersistence();
            else if (args.Contains("--assign-only")) AssignmentReadFailure();
            else { Travel(0, 65535); Travel(65535, 0); RangesAndAssignment(); Lifecycle(); FlipPersistence(); AssignmentReadFailure(); }
            Console.WriteLine(JsonSerializer.Serialize(new { status = "passed", assertions })); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
