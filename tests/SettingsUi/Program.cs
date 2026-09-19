using System.Reflection;
using System.Text.Json;
using System.Xml.Serialization;
using ArtOfSimRally.Mod;
using UnityEngine;
using Host=ArtOfSimRally.Mod.Main;
using Ui=UnityModManagerNet.UnityModManager.UI;
using Time=ArtOfSimRally.Mod.Time;

static class Program
{
    static int assertions;
    static void Check(bool ok,string reason){assertions++;if(!ok)throw new Exception(reason);}
    static string Xml(Settings c){using var w=new StringWriter();new XmlSerializer(typeof(Settings)).Serialize(w,c);return w.ToString();}
    static Settings Read(string xml)=> (Settings)new XmlSerializer(typeof(Settings)).Deserialize(new StringReader(xml));
    static readonly string Wheel="a5444f71-6773-43d7-b43f-3b83492d6f55";
    static void Views()
    {
        var c=Read("<Settings><ForceFeedbackEnabled>false</ForceFeedbackEnabled><Smoothing>0.61</Smoothing><CrashStrength>19.52381</CrashStrength><LandingStrength>20</LandingStrength><BonnetHeight>1.4</BonnetHeight><PreferredDeviceGuid>"+Wheel+"</PreferredDeviceGuid></Settings>");
        string before=Xml(c);
        Check(!SettingsViewPolicy.Advanced(c)&&SettingsViewPolicy.Page(c)==0,"legacy config not Simple Setup");
        Check(SettingsViewPolicy.CustomFfb(c)&&SettingsViewPolicy.CustomCamera(c),"custom tune hidden without summary");
        Check(!SettingsViewPolicy.CustomControls(c),"default controls labelled custom");
        c.DirectSteering=false;Check(SettingsViewPolicy.CustomControls(c),"custom controls hidden");c.DirectSteering=true;
        Check(SettingsViewPolicy.Select(c,true,3,false),"Advanced selection failed");
        var again=Read(Xml(c));Check(SettingsViewPolicy.Advanced(again)&&again.SettingsPage==3,"view/page not persistent");
        Check(!SettingsViewPolicy.Select(c,false,0,true)&&c.SettingsPage==3&&SettingsViewPolicy.Advanced(c),"edit guard discarded current page");
        SettingsViewPolicy.Select(c,false,0,false);Check(Xml(c)==before,"view round trip changed runtime settings");
        c.SettingsView="unknown";c.SettingsPage=999;
        Check(!SettingsViewPolicy.Advanced(c)&&SettingsViewPolicy.Page(c)==0&&!c.ForceFeedbackEnabled,"unknown presentation reset tune");
        var network=new ConnectionEdit();string oldHost=c.TelemetryHost;int oldPort=c.TelemetryPort;
        network.Begin(c);network.Host="127.0.0.2";network.Port="invalid";
        Check(!network.Apply(c)&&network.Editing&&c.TelemetryHost==oldHost&&c.TelemetryPort==oldPort,"partial connection committed");
        network.Cancel();Check(c.TelemetryHost==oldHost&&c.TelemetryPort==oldPort,"cancel changed connection");
        network.Begin(c);network.Host="127.0.0.2";network.Port="9001";
        Check(network.Apply(c)&&!network.Editing&&c.TelemetryHost=="127.0.0.2"&&c.TelemetryPort==9001,"connection not atomic");
    }
    static void SelectionAndMigration()
    {
        var c=new Settings{ForceFeedbackEnabled=false};
        Check(FfbSelection.FollowsSteering(c)&&!FfbSelection.TryTarget(c,out _,out _,out _,out _),"new unbound config chose first wheel");
        c.SteerBinding=$"Wheel|9|axis:0|32767|65535|guid:{Wheel}";
        Check(FfbSelection.TryTarget(c,out var name,out _,out var guid,out _)&&name=="Wheel"&&guid==Wheel&&!c.ForceFeedbackEnabled,"follow lost identity or enabled FFB");
        c.PreferredDeviceGuid=Guid.NewGuid().ToString();string explicitGuid=c.PreferredDeviceGuid;
        Check(!FfbSelection.FollowsSteering(c)&&FfbSelection.TryTarget(c,out _,out _,out guid,out _)&&guid==explicitGuid,"legacy explicit target changed");
        c.SteerBinding=$"Other|0|axis:0|20000|60000|guid:{Guid.NewGuid()}";
        Check(FfbSelection.TryTarget(c,out _,out _,out guid,out _)&&guid==explicitGuid,"steering rebind stole override");
        c.FfbDeviceMode="steering";Check(FfbSelection.TryTarget(c,out name,out _,out guid,out _)&&name=="Other"&&guid!=explicitGuid,"follow did not follow rebind");
        c.FfbDeviceMode="explicit";c.PreferredDeviceGuid="";c.PreferredDevice="Wheel";c.PreferredDeviceIndex=0;
        Check(!FfbSelection.TryTarget(c,out _,out _,out _,out _),"unverified legacy index accepted");
        c.HandbrakeBinding=$"TSS|4|button:2|0|1|guid:{Wheel}";string legacy=c.HandbrakeBinding;
        Check(SettingsMigration.NeedsHandbrakeSplit(c),"legacy button not identified");SettingsMigration.SplitHandbrake(c);
        Check(c.HandbrakeButtonBinding==legacy&&c.HandbrakeBinding==""&&!c.ForceFeedbackEnabled,"legacy split lost binding or tune");
        c.HandbrakeBinding=$"TSS|4|axis:2|65535|0|guid:{Wheel}";string axis=c.HandbrakeBinding;SettingsMigration.SplitHandbrake(c);
        Check(c.HandbrakeBinding==axis&&c.HandbrakeButtonBinding==legacy,"axis/button coexistence migrated incorrectly");
        Check(Read(Xml(c)).HandbrakeButtonBinding==legacy,"button XML persistence failed");
        c.BonnetHeight=1.9f;c.BumperHeight=1.2f;c.LandingStrength=20;c.KeyUp=KeyCode.U;
        c.ResetCameraMount(false);
        Check(c.BonnetHeight==.95f&&c.BumperHeight==1.2f&&c.LandingStrength==20&&c.KeyUp==KeyCode.U,"camera reset escaped selected pose");
        c.ResetFfbTuning();
        Check(!c.ForceFeedbackEnabled&&c.PreferredDevice=="Wheel"&&c.HandbrakeBinding==axis&&c.BumperHeight==1.2f&&c.KeyUp==KeyCode.U,
            "FFB reset changed preference/device/controls/camera");
        Check(c.Strength==50&&c.Smoothing==.2f&&c.LandingStrength==5&&c.CrashStrength==50&&!c.CrashEffectsEnabled,"FFB reset missed tuning");
    }
    static void Runtime()
    {
        Host.ResetFixture();Ui.Instance=new();FfbNative.Ready=true;FfbNative.Stops=FfbNative.Shutdowns=0;
        ArtOfSimRally.Mod.Application.isFocused=true;GameState.IsDriving=true;Input.Down.Clear();
        Input.Down.Add(KeyCode.F8);Host.TickSettingsUi();Input.Down.Clear();
        Check(!Host.Settings.ForceFeedbackEnabled&&FfbNative.Stops==1&&Host.Saves==1&&Host.Cancels==1,"F8 did not stop before saved Off");
        for(int i=0;i<10;i++)Host.TickSettingsUi();Check(!Host.Settings.ForceFeedbackEnabled&&Host.Requests==0,"panic auto-resumed");
        SettingsViewPolicy.Select(Host.Settings,true,2,false);Host.TickSettingsUi();Check(FfbNative.Stops==1&&!Host.Settings.ForceFeedbackEnabled,"view switch affected FFB");
        FfbNative.Ready=false;Host.SelectForceDevice();Check(Host.Requests==0&&!Host.Settings.ForceFeedbackEnabled,"picker cleared Off");
        Host.SetFeedbackEnabled(true);Check(Host.Requests==1&&Host.Settings.ForceFeedbackEnabled,"explicit On cannot resume");
        WheelInput.Pressed.Add(WheelInput.Channel.StopFfbButton);Host.TickSettingsUi();Check(!Host.Settings.ForceFeedbackEnabled,"USB stop failed to save Off");
        WheelInput.Pressed.Add(WheelInput.Channel.SettingsButton);Host.TickSettingsUi();Check(Ui.Instance.Opened,"USB Settings did not open same UI");Host.CloseSettings();
        Host.ToggleSettings();Check(Ui.Instance.Opened&&Ui.Instance.Selected==0&&Ui.Instance.Filter=="fixture","F6 route did not select same mod panel");
        SettingsPanel.Editing=true;Host.CloseSettings();Check(Ui.Instance.Opened&&!SettingsPanel.Editing,"close did not cancel first");
        Host.CloseSettings();Check(!Ui.Instance.Opened,"second close failed");
        Host.WriteSucceeds=false;Host.MarkSettingsDirty();GameState.IsDriving=false;Time.realtimeSinceStartup+=1;
        int saves=Host.Saves;Host.TickSettingsUi();Check(Host.Saves==saves+1&&Host.SettingsSaveStatus=="Write failed","failed save hidden");
        Host.TickSettingsUi();Check(Host.Saves==saves+1,"failed write retried each frame");
        Host.WriteSucceeds=true;Time.realtimeSinceStartup+=6;Host.TickSettingsUi();Check(Host.SettingsSaveStatus=="Saved","failed write not retried");
        Host.SuppressHostClose=true;
        var guard=typeof(SettingsCloseGuard).GetMethod("BeforeClose",BindingFlags.Static|BindingFlags.NonPublic);
        Check(!(bool)guard.Invoke(null,new object[]{false})&&!Host.SuppressHostClose,"UMM key-up ignored cancel guard");
        Check((bool)guard.Invoke(null,new object[]{false}),"UMM close permanently blocked");
    }
    static int Main()
    {
        try{Views();SelectionAndMigration();Runtime();Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions,hardwareOutput=false,visualInspection=false}));return 0;}
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
