using System.Reflection;
using ArtOfSimRally.Mod;
using Device = Dbce.Wheel.Ffb.WheelFfbNative;

static class CalibrationTests
{
    static int count;
    static readonly Guid Lever = Guid.Parse("a5444f71-6773-43d7-b43f-3b83492d6f55");
    static void Check(bool value, string message) { count++; if (!value) throw new Exception(message); }
    static void Same(float a, float b, string message) => Check(Math.Abs(a-b) < .0001f, message+": "+a+" != "+b);
    static void Start()
    {
        WheelInput.Close(); Device.ReadOk=true; Device.ThrowRead=Device.FailOpen=false;
        Array.Clear(Device.Axes); Array.Clear(Device.Buttons);
        Device.Devices = new[] { new Device.DeviceInfo { InstanceGuid=Lever } };
        Main.Enabled=true; Main.SettingsVisible=false; Application.isFocused=true; GameState.IsDriving=false;
        Main.Settings=new Settings { WheelInputEnabled=true, HandbrakeBinding=$"TSS fixture|0|axis:2|0|65535|guid:{Lever}" };
        Time.realtimeSinceStartup += 60; WheelInput.LoadBindings(); WheelInput.Open(); WheelInput.Update();
    }
    static float Handbrake(float stock=0)
    {
        var args=new object[]{new AxisCarController(),.2f,.3f,.4f,stock,.6f,true};
        typeof(WheelInputPatch).GetMethod("Override",BindingFlags.Static|BindingFlags.NonPublic)!.Invoke(null,args);
        return (float)args[4];
    }
    public static int Run()
    {
        Start(); string previous=Main.Settings.HandbrakeBinding;
        WheelInput.BeginCalibration(WheelInput.Channel.Handbrake);
        Device.Axes[2]=33000; WheelInput.Update();
        Check(WheelInput.PendingCalibration!=null && Main.Settings.HandbrakeBinding==previous,"candidate committed before save");
        Check(!WheelInput.CanSaveCalibration,"held candidate can be saved");
        WheelInput.CancelAssign(); Check(Main.Settings.HandbrakeBinding==previous,"cancel changed prior calibration");
        Device.Axes[2]=0; WheelInput.Update(); WheelInput.BeginCalibration(WheelInput.Channel.Handbrake);
        Device.Axes[2]=60000; WheelInput.Update(); Device.Axes[2]=0; WheelInput.Update();
        Check(WheelInput.CanSaveCalibration && WheelInput.SaveCalibration(),"full-release calibration did not save");
        var saved=WheelInput.Binding.Parse(Main.Settings.HandbrakeBinding);
        Check(saved.Calibrated && saved.Far==60000 && saved.InstanceGuid==Lever,"calibration/identity not atomic");
        Device.Axes[2]=30000; WheelInput.Update(); Same(Handbrake(),.5f,"partial handbrake reduced to button");
        Device.Axes[2]=65000; WheelInput.Update(); Check(Main.Settings.HandbrakeBinding==saved.ToString(),"explicit range changed while driving");
        Device.Axes[2]=0; WheelInput.BeginCalibration(WheelInput.Channel.HandbrakeButton);
        Device.Buttons[4]=128; WheelInput.Update(); Device.Buttons[4]=0; WheelInput.Update();
        Check(WheelInput.SaveCalibration(),"button capture did not commit after release");
        Device.Axes[2]=30000; Device.Buttons[4]=128; WheelInput.Update(); Same(Handbrake(),1,"button not additive with axis");
        Device.Buttons[4]=0; WheelInput.Update(); Same(Handbrake(),.5f,"button release cancelled axis");
        Same(Handbrake(.8f),.8f,"stock handbrake cancelled");
        Main.SettingsVisible=true; Same(Handbrake(1),0,"settings allowed handbrake output"); Main.SettingsVisible=false;
        Application.isFocused=false; Same(Handbrake(1),0,"focus loss allowed handbrake output"); Application.isFocused=true;
        previous=Main.Settings.HandbrakeBinding; WheelInput.BeginCalibration(WheelInput.Channel.Handbrake);
        Time.realtimeSinceStartup+=46; WheelInput.Update(); Check(!WheelInput.Assigning.HasValue && previous==Main.Settings.HandbrakeBinding,"timeout changed saved assignment");
        // Centre and both ends are independent, including asymmetric travel.
        Start(); Device.Axes[0]=30000; WheelInput.BeginCalibration(WheelInput.Channel.Steer);
        Device.Axes[0]=0; WheelInput.Update(); Device.Axes[0]=30000; WheelInput.Update();
        Check(!WheelInput.CanSaveCalibration,"one-sided steering accepted");
        Device.Axes[0]=65000; WheelInput.Update(); Device.Axes[0]=30000; WheelInput.Update();
        Check(WheelInput.SaveCalibration(),"two-sided steering not accepted");
        foreach(var pair in new[]{(0,-1f),(15000,-.5f),(30000,0f),(47500,.5f),(65000,1f)})
        { Device.Axes[0]=pair.Item1; WheelInput.Update(); Same(WheelInput.Value(WheelInput.Channel.Steer),pair.Item2,"asymmetric steering range"); }
        var steer=WheelInput.Binding.Parse(Main.Settings.SteerBinding); steer.Inverted=true; steer.Deadzone=.1f;
        Main.Settings.SteerBinding=steer.ToString(); WheelInput.LoadBindings(); Device.Axes[0]=65000; WheelInput.Update();
        Same(WheelInput.Value(WheelInput.Channel.Steer),-1,"saved inversion not applied");
        Device.Axes[0]=31000; WheelInput.Update(); Same(WheelInput.Value(WheelInput.Channel.Steer),0,"saved deadzone not applied");
        // Cancel after an axis and unrelated axis move together never changes XML.
        Start(); previous=Main.Settings.HandbrakeBinding; WheelInput.BeginCalibration(WheelInput.Channel.Handbrake);
        Device.Axes[1]=30000; Device.Axes[2]=30000; WheelInput.Update();
        Check(WheelInput.PendingCalibration==null && Main.Settings.HandbrakeBinding==previous,"ambiguous capture accepted");
        WheelInput.CancelAssign();
        // Axis and digital fallback on independent USB devices survive reorder
        // and disconnect separately; neither may cancel the other or stock input.
        Start(); WheelInput.Close();
        var buttonId=Guid.Parse("bd80fb76-f8f3-46c6-a5c7-cc5af9727d49");
        var axisDevice=new Device.DeviceInfo {Index=7,InstanceGuid=Lever,StateAxes=new int[8],StateButtons=new byte[128]};
        var buttonDevice=new Device.DeviceInfo {Index=2,InstanceGuid=buttonId,StateAxes=new int[8],StateButtons=new byte[128]};
        axisDevice.StateAxes[2]=32767;
        Main.Settings.HandbrakeButtonBinding=$"TSS fixture|99|button:4|0|1|guid:{buttonId}";
        Device.Devices=new[]{buttonDevice,axisDevice}; WheelInput.LoadBindings(); WheelInput.Open(); WheelInput.Update();
        Same(Handbrake(),32767f/65535,"reordered separate USB axis not read");
        buttonDevice.StateButtons[4]=128; WheelInput.Update(); Same(Handbrake(),1,"second USB button not additive");
        buttonDevice.Connected=false; WheelInput.Update(); Same(Handbrake(),32767f/65535,"missing button cancelled independent axis");
        buttonDevice.Connected=true; axisDevice.Connected=false; WheelInput.Update(); Same(Handbrake(),1,"missing axis cancelled independent button");
        buttonDevice.Connected=false; WheelInput.Update(); Same(Handbrake(),0,"disconnected devices left stale handbrake");
        Same(Handbrake(.8f),.8f,"disconnected direct devices cancelled stock control");
        Start(); previous=Main.Settings.HandbrakeBinding; WheelInput.BeginCalibration(WheelInput.Channel.Handbrake);
        Device.Axes[2]=50000; WheelInput.Update(); Device.Devices[0].Connected=false; WheelInput.Update();
        Check(!WheelInput.Assigning.HasValue&&Main.Settings.HandbrakeBinding==previous,"disconnect committed pending calibration");
        foreach(string bad in new[]{"cal:0:NaN:0","cal:0:Infinity:0","cal:0:0.3:0","cal:0:0:2","cal:-1:0:0"})
            Check(WheelInput.Binding.Parse($"TSS fixture|0|axis:2|0|65535|guid:{Lever}|{bad}")==null,"invalid calibration accepted: "+bad);
        Start(); Main.SaveSettings(); previous=Main.Settings.HandbrakeBinding;
        WheelInput.BeginCalibration(WheelInput.Channel.Handbrake);Device.Axes[2]=50000;WheelInput.Update();Device.Axes[2]=0;WheelInput.Update();
        using(File.Open(Main.Path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
        {
            Check(!WheelInput.SaveCalibration()&&WheelInput.CanSaveCalibration&&Main.Settings.HandbrakeBinding==previous,"failed save ended draft or changed config");
            Device.Axes[2]=25000;WheelInput.Update();Same(Handbrake(),25000f/65535,"failed save changed effective calibration");
            Check(!WheelInput.Clear(WheelInput.Channel.Handbrake)&&Main.Settings.HandbrakeBinding==previous&&WheelInput.IsBound(WheelInput.Channel.Handbrake),"failed clear lost binding");
        }
        Device.Axes[2]=0;WheelInput.Update();Check(WheelInput.SaveCalibration(),"calibration could not retry after write failure");
        Device.Axes[2]=25000;WheelInput.Update();Same(Handbrake(),.5f,"successful retry did not swap calibration");
        Check(WheelInput.Clear(WheelInput.Channel.Handbrake)&&!WheelInput.IsBound(WheelInput.Channel.Handbrake),"clear retry failed");
        // Mod buttons remain usable independently of direct driving controls.
        Start();Main.Settings.WheelInputEnabled=false;Main.Settings.ForceFeedbackEnabled=false;
        WheelInput.BeginCalibration(WheelInput.Channel.SettingsButton);
        Device.Buttons[7]=128;WheelInput.Update();Device.Buttons[7]=0;WheelInput.Update();
        Check(WheelInput.SaveCalibration()&&!Main.Settings.WheelInputEnabled&&!Main.Settings.ForceFeedbackEnabled,"mod binding enabled driving controls or FFB");
        WheelInput.Update();Check(!WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton),"save generated a settings press");
        Device.Buttons[7]=128;WheelInput.Update();Check(WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton),"settings button unavailable with driving input Off");
        Check(!WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton),"held settings button repeats");
        Device.Devices[0].Connected=false;WheelInput.Update();Check(!WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton),"disconnect generated shortcut");
        Device.Devices[0].Connected=true;WheelInput.Update();Check(!WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton),"held reconnect generated shortcut");
        Device.Buttons[7]=0;WheelInput.Update();WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton);
        Device.Buttons[7]=128;WheelInput.Update();Check(WheelInput.ShortcutPressed(WheelInput.Channel.SettingsButton),"released/repressed shortcut failed");
        Device.Buttons[7]=0;WheelInput.Update();WheelInput.BeginCalibration(WheelInput.Channel.StopFfbButton);
        Device.Buttons[7]=128;WheelInput.Update();
        Check(WheelInput.PendingCalibration==null&&Main.Settings.StopFfbButtonBinding=="","reserved button conflict accepted");
        WheelInput.CancelAssign();
        for(int i=0;i<11;i++)
        {
            Array.Clear(Device.Buttons);WheelInput.Update();var channel=WheelInput.CameraChannel(i);
            WheelInput.BeginCalibration(channel);Device.Buttons[20+i]=128;WheelInput.Update();Device.Buttons[20+i]=0;WheelInput.Update();
            Check(WheelInput.SaveCalibration()&&!Main.Settings.WheelInputEnabled,"camera button save enabled driving input");
            WheelInput.LoadBindings();WheelInput.Update();WheelInput.ShortcutPressed(channel);
            Device.Buttons[20+i]=128;WheelInput.Update();Same(WheelInput.Value(channel),1,"camera binding lost after reload");
            Check(WheelInput.ShortcutPressed(channel)&&!WheelInput.ShortcutPressed(channel),"camera edge repeated");
        }
        return count;
    }
}
