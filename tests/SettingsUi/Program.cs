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
        try{Views();SelectionAndMigration();Runtime();MenuIsolation();Display();Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions,hardwareOutput=false,visualInspection=false}));return 0;}
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
    static void MenuIsolation()
    {
        Host.ResetFixture();Ui.Instance=new();StockUiInput.Reset();Input.Held.Clear();Input.Down.Clear();
        var player=Rewired.ReInput.players.AllPlayers[0];
        StockUiInput.Observe(new Rewired.Integration.UnityUI.RewiredStandaloneInputModule());
        Time.frameCount=100;
        Check(!StockUiInput.Blocked,"unopened menu was blocked");
        Input.Down.Add(Host.Settings.SettingsKey);
        Check(StockUiInput.Blocked,"opening key leaked before watchdog");
        Input.Down.Clear();Ui.Instance.Opened=true;
        player.Down.Add(17);player.Buttons.Add(17);player.Axes[101]=-1;
        Check(StockUiInput.Blocked&&!StockUiInput.ButtonDown(player,17)&&StockUiInput.Axis(player,101)==0,"live native menu inputs leaked");
        var module=new Rewired.Integration.UnityUI.RewiredStandaloneInputModule();
        var pointer=new Rewired.UI.PlayerPointerEventData{eligibleForClick=true,dragging=true,pointerPress=new(),rawPointerPress=new(),pointerDrag=new(),clickCount=2};
        module.AddPointer(pointer);
        var dispatch=typeof(StockUiDispatchGuard).GetMethod("BeforeProcess",BindingFlags.Static|BindingFlags.NonPublic);
        Check(!(bool)dispatch.Invoke(null,new object[]{module}),"native pointer/module dispatch not stopped");
        Check(!pointer.eligibleForClick&&!pointer.dragging&&pointer.pointerPress==null&&pointer.rawPointerPress==null&&pointer.pointerDrag==null&&pointer.clickCount==0,"pre-panel pointer press/drag survived ownership");
        Ui.Instance.Opened=false;
        for(int i=0;i<10;i++){Time.frameCount++;Check(StockUiInput.Blocked,"close handed back held cancel/navigation");}
        player.Buttons.Clear();player.Down.Clear();player.Axes.Clear();
        Input.Held.Add(KeyCode.Escape);Time.frameCount++;
        Check(StockUiInput.Blocked,"keyboard cancel released to native quit");
        Input.Held.Clear();Input.Held.Add(KeyCode.Mouse0);Time.frameCount++;
        Check(StockUiInput.Blocked,"held pointer released to native UI");
        Input.Held.Clear();WheelInput.SettingsHeld=1;Time.frameCount++;
        Check(StockUiInput.Blocked,"held Settings button bypassed close barrier");
        WheelInput.SettingsHeld=0;ArtOfSimRally.Mod.Application.isFocused=false;Time.frameCount++;
        Check(StockUiInput.Blocked,"unfocused input was treated as released");
        ArtOfSimRally.Mod.Application.isFocused=true;Rewired.ReInput.isReady=false;Time.frameCount++;
        Check(StockUiInput.Blocked,"Rewired teardown treated as release");
        Rewired.ReInput.isReady=true;Time.frameCount++;
        Check(StockUiInput.Blocked&&StockUiInput.Blocked,"same-frame multiple dispatch calls drained barrier");
        Time.frameCount++;Check(StockUiInput.Blocked,"release frame leaked a key-up");
        Time.frameCount++;Check(!StockUiInput.Blocked,"neutral controls never returned native input");
        player.Down.Add(17);Check(StockUiInput.ButtonDown(player,17),"fresh cancel failed after neutral handback");player.Down.Clear();
        StockUiInput.Capture();Host.Enabled=false;Check(!StockUiInput.Blocked,"disabled mod trapped native controls");Host.Enabled=true;
        StockUiInput.Capture();player.Negative.Add(102);Time.frameCount++;
        Check(StockUiInput.Blocked,"negative controller navigation bypassed barrier");player.Negative.Clear();
        Ui.Instance.Opened=true;Time.frameCount++;Check(StockUiInput.Blocked,"reopening lost ownership");
        Ui.Instance.Opened=false;StockUiInput.Reset();
        // Build17584229 ReplayManager.Update polls24/25 only inside active
        // playback. Hold each through close; native Axis must remain zero.
        foreach(int action in new[]{24,25})
        {
            StockUiInput.Capture();player.Axes[action]=.05f;
            for(int i=0;i<6;i++){Time.frameCount++;Check(StockUiInput.Axis(player,action)==0&&StockUiInput.Blocked,"held replay scrub resumed at close: "+action);}
            player.Axes.Remove(action);
            for(int i=0;i<3;i++){Time.frameCount++;StockUiInput.Axis(player,action);}
            Check(!StockUiInput.Blocked,"released replay axis never returned input");
        }
        StockUiInput.Capture();player.Axes[24]=1;player.Axes[25]=-1;
        Time.frameCount+=2; // no replay polling in this context
        for(int i=0;i<3;i++){Time.frameCount++;_ = StockUiInput.Blocked;}
        Check(!StockUiInput.Blocked,"inactive replay controls trapped stock menus");
        player.Axes.Clear();StockUiInput.Reset();
    }
    static void Display()
    {
        Check(SettingsDisplayPolicy.Scale(2160,1,false)==2,"4K automatic content remained 1x");
        Check(SettingsDisplayPolicy.Scale(720,1,false)==1,"720p default text shrank");
        Check(SettingsDisplayPolicy.Scale(2160,1.5f,false)==1.5f,"explicit UMM scale was replaced");
        Check(SettingsDisplayPolicy.Scale(2160,1,true)==1,"Use UMM scale ignored explicit 1x");
        foreach(int h in new[]{720,1080,1440,2160})
        {
            float scale=SettingsDisplayPolicy.Scale(h,1,false);int w=h*16/9;
            Check(SettingsDisplayPolicy.Width(w,scale,0)<=w-100,"automatic content escaped screen");
            Check(SettingsDisplayPolicy.PageHeight(h,scale,0)<=h*.45f,"page scroll escaped screen");
        }
        Check(SettingsDisplayPolicy.Width(3840,2,1000)==930,"explicit host width ignored");
        Check(SettingsDisplayPolicy.Width(3840,2,0)==890,"zero UMM preference treated as unlimited 4K viewport");
        Check(SettingsDisplayPolicy.Width(3840,2,960)==890,"actual default UMM host escaped");
        Check(SettingsDisplayPolicy.PageHeight(2160,2,0)<=200,"default UMM height lost page scroll");
        Check(SettingsDisplayPolicy.PageColumns(890,2)==3,"4K default host did not wrap page buttons");
        Check(SettingsDisplayPolicy.PageColumns(890,1)==6,"ordinary host unnecessarily wrapped pages");
        Check(SettingsDisplayPolicy.PageColumns(200,5)==1,"narrow high-scale page grid invalid");
        var c=new Settings{SettingsFollowHostScale=true};Check(Read(Xml(c)).SettingsFollowHostScale,"display preference not persisted");
    }
}
