using ArtOfSimRally.Mod;
using Host=ArtOfSimRally.Mod.Main;
using Device=Dbce.Wheel.Ffb.WheelFfbNative;

static class ShifterIdentityTests
{
    static int count;
    static void Check(bool ok,string message) { count++; if(!ok) throw new Exception(message); }
    internal static int Run()
    {
        count=0; WheelInput.Close(); Shifter.Close(); GameState.IsDriving=false;
        var a=Guid.Parse("0ca32b7b-dada-4a4c-aa78-7563a546f223");
        var b=Guid.Parse("2e3ee382-1080-46e5-a3c7-1462fbc15680");
        Host.Settings=new Settings { ShifterEnabled=true, ShifterDeviceName="shifter", ShifterDeviceIndex=0 };
        Device.Devices=new[] {new Device.DeviceInfo {Index=0,Name="wheel",InstanceGuid=b},new Device.DeviceInfo {Index=4,Name="shifter",InstanceGuid=a}};
        Shifter.ListDevices();
        Check(Shifter.SelectedPosition(Host.Settings)==1,"legacy picker displayed stale USB row");
        Check(Shifter.Open(0) && Device.LastAuxIndex==4,"saved shifter index opened a different USB device");
        int saves=Host.Saves; Time.realtimeSinceStartup+=6; Shifter.FlushSelection();
        Check(Host.Saves==saves+1 && File.ReadAllText(Host.Path).Contains(a.ToString()),"shifter identity not persisted");
        Host.Settings.ShifterDeviceGuid="invalid";
        Check(Shifter.SelectedPosition(Host.Settings)==-1,"invalid GUID displayed a fallback row");
        Check(!Shifter.Open(0) && Device.Aux==null,"invalid identity fell back to a name/index");
        Host.Settings.ShifterDeviceGuid=a.ToString();
        // Reorder again with identical names: migration must retain the chosen identity.
        Device.Devices=new[] {new Device.DeviceInfo {Index=0,Name="shifter",InstanceGuid=b},new Device.DeviceInfo {Index=5,Name="shifter",InstanceGuid=a}};
        Shifter.ListDevices();
        Check(Shifter.SelectedPosition(Host.Settings)==1,"strict picker displayed another same-name device");
        Check(Shifter.Open(0) && Device.LastAuxIndex==5,"shifter lost migrated identity");
        Device.Devices=new[] {Device.Devices[0]};
        Shifter.ListDevices(); Check(Shifter.SelectedPosition(Host.Settings)==-1,"missing shifter displayed another device");
        Check(!Shifter.Open(0) && Device.Aux==null,"missing shifter silently selected another device");
        Host.Settings=new Settings { ShifterEnabled=true, ShifterDeviceName="shifter", ShifterDeviceIndex=0 };
        Device.Devices=new[] {new Device.DeviceInfo {Index=0,Name="shifter",InstanceGuid=b},new Device.DeviceInfo {Index=5,Name="shifter",InstanceGuid=a}};
        Check(!Shifter.Open(0),"ambiguous legacy shifter opened by index");
        Shifter.ListDevices(); Check(Shifter.SelectedPosition(Host.Settings)==-1,"ambiguous legacy picker displayed first device");
        string chosen=Shifter.DeviceGuid(1);
        Check(chosen==a.ToString(),"picker lost its cached device identity");
        Host.Settings.ShifterDeviceGuid=chosen;
        Device.Devices=new[] {Device.Devices[1],Device.Devices[0]};
        Check(Shifter.Open(1) && Device.LastAuxIndex==5,"cached picker used position instead of identity");
        Check(Shifter.SelectedPosition(Host.Settings)==1,"native refresh replaced panel snapshot");
        Shifter.ListDevices(); Check(Shifter.SelectedPosition(Host.Settings)==0,"refreshed picker did not follow GUID");
        Host.Settings.ShifterDeviceGuid="";
        Host.Settings.ShifterDeviceName="";
        Check(!Shifter.Open(0),"unidentified legacy index opened a device");
        Host.Settings.ShifterDeviceName="shifter"; Device.Devices=new[] {Device.Devices[0]};
        Check(Shifter.Open(0),"unambiguous shifter failed to reconnect");
        Host.Settings.ShifterIsHPattern=true; Host.Settings.Gear1Button=2;
        Array.Clear(Device.Buttons); Device.Buttons[2]=128; var car=new Drivetrain();
        Shifter.Update(car); Check(car.Shifts==1,"H-pattern initial gate not applied");
        Shifter.Close(); Shifter.Open(0); Shifter.Update(car);
        Check(car.Shifts==2,"reopen retained previous shifter gate latch");
        Shifter.Close(); GameState.IsDriving=true;
        int enumerations=Device.Enumerations;
        Check(!Shifter.Open(0) && Device.Enumerations==enumerations,"shifter discovery interrupted driving");
        GameState.IsDriving=false; Shifter.Close();
        return count;
    }
}
