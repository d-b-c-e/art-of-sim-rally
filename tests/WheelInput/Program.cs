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
        Device.Devices = new[] { new Device.DeviceInfo() };
        Array.Clear(Device.Axes); Array.Clear(Device.Buttons);
        Host.Settings = new Settings { WheelInputEnabled = true, HandbrakeBinding = binding };
        Host.Enabled = true; Clock.realtimeSinceStartup += 20;
        GameEntryPoint.EventManager = new EventManager();
        GameState.IsDriving=false;
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
    static readonly Guid LeverA = Guid.Parse("df1786e0-bf89-4194-a8c9-a1bddbfb2c90");
    static readonly Guid LeverB = Guid.Parse("ad643763-846c-48cc-b03e-b5c9688f9b60");
    static Device.DeviceInfo Lever(int index, Guid id, int raw, string name = "TSS fixture")
    {
        var axes = new int[8]; axes[2] = raw;
        return new Device.DeviceInfo { Index=index, Name=name, InstanceGuid=id, StateAxes=axes };
    }
    static void Devices(params Device.DeviceInfo[] devices)
    {
        WheelInput.Close(); Device.Devices=devices; GameState.IsDriving=false;
        Clock.realtimeSinceStartup+=20; WheelInput.Open(); WheelInput.Update();
    }
    static void DeviceIdentity()
    {
        Setup("TSS fixture|0|axis:2|0|65535");
        Devices(Lever(0,LeverB,65535),Lever(1,LeverA,16384));
        BoundOnlyHandbrake(0); // Legacy index cannot distinguish identical devices after reordering.
        Setup("TSS fixture|0|axis:2|0|65535|guid:"+LeverA);
        Devices(Lever(0,LeverB,65535),Lever(1,LeverA,16384));
        BoundOnlyHandbrake(16384f/65535); // Index 0 now belongs to another identical lever.
        Devices(Lever(0,LeverB,65535)); BoundOnlyHandbrake(0);
        Devices(Lever(3,LeverA,32768,"renamed lever")); BoundOnlyHandbrake(32768f/65535);
        WheelInput.Flip(WheelInput.Channel.Handbrake); WheelInput.LoadBindings();
        Check(Host.Settings.HandbrakeBinding.EndsWith("|guid:"+LeverA),"Flip/reload lost GUID");
        using (var stream = File.OpenRead(Host.Path)) Host.Settings = (Settings)new XmlSerializer(typeof(Settings)).Deserialize(stream);
        WheelInput.LoadBindings(); WheelInput.Update(); BoundOnlyHandbrake(32767f/65535);
        Setup("TSS fixture|0|axis:2|0|65535");
        Devices(Lever(0,LeverA,65535),Lever(1,LeverB,65535)); BoundOnlyHandbrake(0);
        Check(WheelInput.Describe(WheelInput.Channel.Handbrake).Contains("Assign"),"ambiguous legacy binding not explained");
        Device.Devices[1].CannotOpen=true; Devices(Device.Devices); BoundOnlyHandbrake(0);
        Setup("TSS fixture|0|axis:2|0|65535"); int saves=Host.Saves; Devices(Lever(4,LeverA,32768));
        BoundOnlyHandbrake(32768f/65535);
        Check(Host.Settings.HandbrakeBinding.EndsWith("|guid:"+LeverA),"unique legacy binding not pinned");
        Check(Host.Saves==saves,"legacy migration wrote settings during input update");
        Clock.realtimeSinceStartup+=6; WheelInput.FlushLearnedRanges();
        Check(Host.Saves==saves+1,"legacy migration not persisted while idle");
        Devices(Lever(4,LeverB,65535)); BoundOnlyHandbrake(0);
        foreach (string invalid in new[] {"|guid:bad", "|guid:"+Guid.Empty, "|guid:"+LeverA+"|extra", "|unknown:"+LeverA})
            Check(WheelInput.Binding.Parse("TSS fixture|0|axis:2|0|65535"+invalid)==null,"invalid identity silently became a legacy binding");
        Setup(""); Devices(Lever(0,LeverA,0),Lever(1,LeverB,0));
        WheelInput.BeginAssign(WheelInput.Channel.Handbrake); Device.Devices[1].StateAxes[2]=32000;
        WheelInput.Update();
        Check(Host.Settings.HandbrakeBinding.EndsWith("|guid:"+LeverB),"assignment did not identify moved lever");
        GameState.IsDriving=true;
    }
    static void DeviceRecovery()
    {
        Setup("TSS fixture|1|axis:2|0|65535|guid:"+LeverB);
        var wheel=Lever(0,LeverA,0,"wheel"); Devices(wheel); BoundOnlyHandbrake(0);
        var lever=Lever(1,LeverB,32768); Device.Devices=new[] {wheel,lever};
        int enums=Device.Enumerations; GameState.IsDriving=true; Clock.realtimeSinceStartup+=10;
        for(int i=0;i<100;i++) WheelInput.Update();
        Check(Device.Enumerations==enums,"device discovery occurred while driving"); BoundOnlyHandbrake(0);
        GameState.IsDriving=false; WheelInput.Update(); BoundOnlyHandbrake(32768f/65535);
        lever.Connected=false; WheelInput.Update(); BoundOnlyHandbrake(0);
        enums=Device.Enumerations;
        for(int i=0;i<100;i++) WheelInput.Update();
        Check(Device.Enumerations==enums,"failed device caused an enumeration loop");
        var returned=Lever(7,LeverB,16384); Device.Devices=new[] {wheel,returned};
        Clock.realtimeSinceStartup+=6; WheelInput.Update(); BoundOnlyHandbrake(16384f/65535);
        // Unbound newly attached devices are discovered on explicit Assign.
        Setup(""); Devices(wheel); Device.Devices=new[] {wheel,Lever(2,LeverB,0)};
        WheelInput.BeginAssign(WheelInput.Channel.Handbrake); Device.Devices[1].StateAxes[2]=20000;
        WheelInput.Update(); Check(Host.Settings.HandbrakeBinding.EndsWith("|guid:"+LeverB),"Assign missed late USB device");
        GameState.IsDriving=true; WheelInput.BeginAssign(WheelInput.Channel.Brake);
        Check(!WheelInput.Assigning.HasValue,"assignment refreshed readers during driving");
    }
    static void AssignmentResume()
    {
        Setup(""); WheelInput.BeginAssign(WheelInput.Channel.Handbrake);
        int saves=Host.Saves; GameState.IsDriving=true; Device.Axes[2]=40000; WheelInput.Update();
        Check(!WheelInput.IsBound(WheelInput.Channel.Handbrake) && !WheelInput.Assigning.HasValue && Host.Saves==saves,
            "resuming from pause completed assignment and wrote settings during driving");
    }
    static void BindingSaves()
    {
        Setup("TSS fixture|0|axis:2|0|65535"); Host.SaveSettings();
        int saves=Host.Saves; GameState.IsDriving=true;
        WheelInput.Flip(WheelInput.Channel.Handbrake); WheelInput.Clear(WheelInput.Channel.Handbrake);
        Check(Host.Saves==saves,"Flip/Clear wrote settings during driving");
        GameState.IsDriving=false; Clock.realtimeSinceStartup+=6; WheelInput.FlushLearnedRanges();
        Check(Host.Saves==saves+1 && !File.ReadAllText(Host.Path).Contains("TSS fixture"),"latest binding edit not saved while idle");
        Setup("TSS fixture|0|axis:2|0|65535"); Host.SaveSettings();
        string before=File.ReadAllText(Host.Path);
        using(File.Open(Host.Path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            WheelInput.Flip(WheelInput.Channel.Handbrake);
        Check(File.ReadAllText(Host.Path)==before,"failed binding save damaged previous settings");
        Clock.realtimeSinceStartup+=6; WheelInput.FlushLearnedRanges();
        Check(File.ReadAllText(Host.Path).Contains("|65535|0"),"failed binding edit did not retry");
    }
    static int Main(string[] args)
    {
        try
        {
            var directory = Path.GetFullPath(Path.Combine("results", "wheel-input-" + Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(directory); Host.Path = Path.Combine(directory, "Settings.xml");
            if (args.Contains("--resume-assignment-only")) AssignmentResume();
            else if (args.Contains("--shifter-only")) assertions+=ShifterIdentityTests.Run();
            else if (args.Contains("--identity-only")) DeviceIdentity();
            else if (args.Contains("--reconnect-only")) DeviceRecovery();
            else if (args.Contains("--flip-only")) FlipPersistence();
            else if (args.Contains("--assign-only")) AssignmentReadFailure();
            else { Travel(0, 65535); Travel(65535, 0); RangesAndAssignment(); Lifecycle(); FlipPersistence(); AssignmentReadFailure(); DeviceIdentity(); DeviceRecovery(); AssignmentResume(); BindingSaves(); assertions+=ShifterIdentityTests.Run(); }
            Console.WriteLine(JsonSerializer.Serialize(new { status = "passed", assertions })); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
