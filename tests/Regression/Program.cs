using System.Globalization;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Xml.Linq;
using System.Xml.Serialization;
using ArtOfSimRally.Mod;
using UnityEngine;
using Dbce.Wheel.Ffb;

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static float F(string value) => float.Parse(value, CultureInfo.InvariantCulture);
    static void Same(float actual, float expected, string message)
        => Check(float.IsFinite(actual) && Math.Abs(actual - expected) <= 0.000001f, message + $": {actual:R} != {expected:R}");

    // Frozen from our v0.2.2 inline formula. Crucially this calls the game's actual
    // managed Mathf, rather than using the extracted ForceCurve as its own oracle.
    static float Baseline(float fy, float slip, float ideal, float speed, float reference, float gain, bool invert, float smooth, float previous)
    {
        float trail = Mathf.Lerp(1f, .6f, Mathf.Clamp01(slip / (2f * Mathf.Max(1f, ideal))));
        float n = fy * trail / Mathf.Max(1f, reference);
        n *= gain;
        n *= Mathf.SmoothStep(0f, 1f, (speed - 3f) / 9f);
        if (invert) n = -n;
        n = Mathf.Clamp(n, -1f, 1f);
        return Mathf.Lerp(n, previous, Mathf.Clamp01(smooth));
    }

    static void Curve()
    {
        var random = new System.Random(550320);
        foreach (float smoothing in new[] { 0f, .2f, .5f, .95f, 1f })
        foreach (float gain in new[] { 0f, .3f, .52f, 1f, 2f })
        foreach (bool invert in new[] { false, true })
        {
            float expected = 0, actual = 0, portable = 0;
            for (int i = 0; i < 2000; i++)
            {
                float fy = (float)(random.NextDouble() * 80000 - 40000);
                float speed = i % 5 == 0 ? 0 : (float)(random.NextDouble() * 16);
                float ideal = i % 7 == 0 ? 0 : 8.5f;
                float slip = (float)(random.NextDouble() * 45);
                if (i % 251 == 0) expected = actual = portable = 0; // stage/reset boundary
                expected = Baseline(fy, slip, ideal, speed, 11500, gain, invert, smoothing, expected);
                actual = ForceCurve.Smooth(actual, ForceCurve.Normalised(fy, slip, ideal, speed, 11500, gain, invert), smoothing);
                portable = ArtOfSimRally.Testing.LegacyForceCurve.Evaluate(fy,slip,ideal,speed,11500,gain,invert,smoothing,portable);
                Same(portable,expected,"portable baseline differs from game Mathf");
                Check((int)(portable*10000)==(int)(expected*10000),"portable baseline changed device magnitude");
                Same(actual, expected, $"dynamic curve step {i}");
                Check((int)(actual * 10000) == (int)(expected * 10000), "device magnitude changed");
            }
        }
        // Change user tune and reset state within a sequence too, including the
        // Reddit user's 15/0.50 settings. These remain independent filter states.
        float priorBaseline=0, priorToolkit=0;
        for (int i=0;i<25000;i++)
        {
            float fy=(float)(random.NextDouble()*100000-50000);
            float speed=(float)(random.NextDouble()*60), slip=(float)(random.NextDouble()*50);
            float gain=new[] { .3f, .52f, 1f, 2f }[i%4];
            float smoothing=new[] { 0f,.2f,.5f,1f }[(i/7)%4];
            bool invert=(i/101)%2==1;
            if(i%103==0) priorBaseline=priorToolkit=0;
            priorBaseline=Baseline(fy,slip,8.5f,speed,11500,gain,invert,smoothing,priorBaseline);
            priorToolkit=ForceCurve.Smooth(priorToolkit,ForceCurve.Normalised(fy,slip,8.5f,speed,11500,gain,invert),smoothing);
            Same(priorToolkit,priorBaseline,"changing settings sequence");
            Check((int)(priorToolkit*10000)==(int)(priorBaseline*10000),"changing settings changed device force");
        }
        Check(ForceCurve.Normalised(40000, 0, 8.5f, 3, 11500, 2, false) == 0, "fade start");
        Check(ForceCurve.Normalised(40000, 0, 8.5f, 12, 11500, 2, false) == 1, "saturation");
        Same(ForceCurve.Normalised(1, 0, 0, 12, 0, 1, true), -1, "reference floor/inversion");
    }

    static void WheelIdentity()
    {
        foreach (string invalid in new[] { "wheel|0|axis:8|0|65535", "wheel|0|button:128|0|1", "wheel|x|axis:0|0|1", "wheel|0|unknown:0|0|1", "wheel|0|axis:x|0|1", "wheel|0|axis:0|-2147483648|2147483647" })
            Check(WheelInput.Binding.Parse(invalid)==null,"invalid binding could index device memory");
        string legacy="wheel|0|axis:0|32767|65535";
        Check(WheelInput.Binding.Parse(legacy)?.ToString()==legacy,"legacy input binding changed");
        var first=Guid.NewGuid(); var second=Guid.NewGuid();
        var snapshot=new[] {
            new WheelFfbNative.DeviceInfo { Index=0,Name="same wheel",ForceFeedback=true,InstanceGuid=second },
            new WheelFfbNative.DeviceInfo { Index=1,Name="same wheel",ForceFeedback=true,InstanceGuid=first }
        };
        typeof(FfbNative).GetField("_devices",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Static)!.SetValue(null,snapshot);
        var settings=new Settings { PreferredDeviceIndex=0,PreferredDevice="same wheel",PreferredDeviceGuid=first.ToString() };
        Check(FfbNative.SelectedPosition(settings)==1,"GUID followed stale enumeration index");
        Check(FfbNative.DeviceGuid(1)==first.ToString(),"picker saved wrong GUID");
        settings.PreferredDeviceGuid=Guid.NewGuid().ToString();
        Check(FfbNative.SelectedPosition(settings)==-1,"missing wheel fell back in picker");
        settings.PreferredDeviceGuid="invalid";
        Check(FfbNative.SelectedPosition(settings)==-1,"invalid GUID fell back in picker");
        settings.PreferredDeviceGuid="";
        Check(FfbNative.SelectedPosition(settings)==0,"legacy index no longer readable");
    }

    static void Saves()
    {
        var save = new DeferredSave(); int writes = 0;
        Func<bool> ok = () => { writes++; return true; };
        Check(!save.Flush(0, false, false, ok) && writes == 0, "clean save wrote");
        save.MarkDirty();
        Check(!save.Flush(0, true, false, ok) && writes == 0 && save.Pending, "driving save wrote");
        Check(!save.Flush(0, false, false, () => { writes++; return false; }) && save.Pending, "failure lost dirty state");
        save.MarkDirty(); // another calibration extension must not bypass backoff
        Check(!save.Flush(4.99, false, false, ok) && writes == 1, "failed save retried too soon");
        Check(save.Flush(5, false, false, ok) && !save.Pending && writes == 2, "idle retry failed");
        save.MarkDirty();
        Check(!save.Flush(10, false, false, () => throw new IOException()) && save.Pending, "exception lost dirty state");
        Check(save.Flush(10.1, false, true, ok) && !save.Pending, "shutdown retry skipped");
        Check(!save.Flush(20, false, true, ok) && writes == 3, "duplicate shutdown saved twice");
        // Real disk failure through the production writer, with the actual
        // Settings type and UMM-compatible XML, not a successful-save mock.
        string directory=Path.GetFullPath(Path.Combine("results","settings-"+Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        string path=Path.Combine(directory,"Settings.xml");
        var original=new Settings { Strength=15, Smoothing=.5f, BonnetHeight=1.234f };
        SettingsPersistence.Write(original,path);
        string before=File.ReadAllText(path);
        var changed=new Settings { Strength=26, Smoothing=.2f, BonnetHeight=2.345f };
        var pending=new DeferredSave(); pending.MarkDirty();
        using(var locked=File.Open(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            Check(!pending.Flush(0,false,false,()=>{ SettingsPersistence.Write(changed,path); return true; }) && pending.Pending,"locked file reported success");
        Check(File.ReadAllText(path)==before,"failed save damaged prior settings");
        Check(Directory.GetFiles(directory,"*.tmp").Length==0,"failed save left temporary file");
        Check(pending.Flush(5,false,false,()=>{ SettingsPersistence.Write(changed,path); return true; }) && !pending.Pending,"disk retry did not recover");
        using(var input=File.OpenRead(path))
        {
            var restored=(Settings)new XmlSerializer(typeof(Settings)).Deserialize(input)!;
            Check(restored.Strength==26 && restored.Smoothing==.2f && restored.BonnetHeight==2.345f,"UMM-compatible settings roundtrip changed values");
        }
    }

    static void Native(string path)
    {
        string source = Path.GetFullPath(path);
        string directory = Path.GetFullPath(Path.Combine("results", "native-alias-" + Guid.NewGuid().ToString("N")));
        Directory.CreateDirectory(directory);
        string alias = Path.Combine(directory, "UnityForceFeedback.dll");
        File.Copy(source, alias);
        Check(NativeDiagnostics.Describe("UnityForceFeedback.dll").Contains("not loaded"), "inspection loaded native DLL");
        Check(WheelFfbNative.Load(directory, "UnityForceFeedback.dll"), WheelFfbNative.LastError);
        Check(!WheelFfbNative.Ready, "binding unexpectedly acquired a device");
        Check(WheelFfbNative.Version == 500, "native component changed; review candidate ABI");
        string description = NativeDiagnostics.Describe("UnityForceFeedback.dll");
        Check(description.Contains("0.5.0"), "native version decoding");
        Check(description.Contains(NativeDiagnostics.FileHash(source)), "mapped DLL hash");
        Check(description.Contains(alias), "mapped DLL path");
        Check(NativeDiagnostics.Describe("kernel32.dll").Contains("export missing"), "missing-export fallback");
        IntPtr module = NativeLibrary.Load(alias); // Only a module reference; never InitDirectInput.
        try
        {
            var fields = typeof(WheelFfbNative).GetFields(System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                .Where(f => typeof(Delegate).IsAssignableFrom(f.FieldType)).ToArray();
            Check(fields.Length >= 38, "native binding scan is empty/incomplete");
            foreach (var field in fields) Check(NativeLibrary.TryGetExport(module, field.Name, out _), "Missing export " + field.Name);
        }
        finally { NativeLibrary.Free(module); } // Toolkit keeps its own successful reference.
    }

    static object ToolkitComparison()
    {
        // Exercise the PINNED artifact, not whatever the upstream task is editing.
        // Legacy SimLite remains different; adoption uses explicitly versioned AxleForceCurve.
        var profile = ForceProfile.SimLite();
        profile.Shaper.Invert = true; // Toolkit lateral-force sign is opposite ours.
        var model = new ForceModel(profile.Model);
        var inputs = new ForceInputs { HasFrontLateralForce=true, HasSlip=true,
            FrontLateralForce=23000, FrontSlipAngleDeg=0, IdealSlipAngleDeg=8.5f, SpeedMps=40/3.6f };
        float current = ForceCurve.Smooth(0, ForceCurve.Normalised(23000,0,8.5f,40,11500,1,false),.2f);
        float toolkit = profile.Shaper.Shape(model.Compute(inputs,.02f),40,.02f);
        Same(current,.8f,"current clamp before EMA");
        Same(toolkit,1f,"pinned toolkit clamp after EMA changed; revisit adoption audit");
        inputs.FrontLateralForce=0;
        float nextCurrent=ForceCurve.Smooth(current,0,.2f);
        float nextToolkit=profile.Shaper.Shape(model.Compute(inputs,.02f),40,.02f);
        Same(nextCurrent,.16f,"current release tail"); Same(nextToolkit,.32f,"toolkit release tail");
        return new { pipeline="AxleForceCurve@" + AxleForceCurve.CompatibilityVersion, scenario="23000 N then zero, 40 km/h, strength 50, smoothing 0.2", current, toolkit, nextCurrent, nextToolkit,
            conclusion="adopted compatibility pipeline preserves the baseline; simlite@2 remains a different tune" };
    }

    static int Main(string[] args)
    {
        try
        {
            object? detail = null;
            if (args.Length == 2 && args[0] == "--native") { Curve(); WheelIdentity(); Saves(); Native(args[1]); detail=ToolkitComparison(); }
            else { Curve(); WheelIdentity(); Saves(); detail=ToolkitComparison(); }
            Console.WriteLine(JsonSerializer.Serialize(new { status = "passed", assertions, detail })); return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
