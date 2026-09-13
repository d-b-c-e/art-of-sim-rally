using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using ArtOfSimRally.Haptics;
using ArtOfSimRally.SimHub;
using ArtOfSimRally.Mod;
using UnityEngine;
using ModMain=ArtOfSimRally.Mod.Main;

internal static class Program
{
    private static int _checks;
    private static void Check(bool ok,string name) { _checks++; if(!ok) throw new Exception(name); }
    private static bool Wait(Func<bool> predicate,int ms=700)
    { var clock=Stopwatch.StartNew(); while(clock.ElapsedMilliseconds<ms) { if(predicate()) return true; Thread.Sleep(2); } return predicate(); }
    private static HapticFrame Frame(uint tick=1000,uint sequence=1,uint landing=0,int magnitude=0,bool active=false,ulong session=1)
        => new HapticFrame {Session=session,Sequence=sequence,Tick=tick,Event=landing,EventTick=tick,Magnitude=magnitude,Active=active};
    private static void Main()
    {
        var bytes=new byte[HapticProtocol.Size]; var f=Frame(1000,2,1,10000,true);
        HapticProtocol.Encode(f,bytes); HapticFrame decoded;
        Check(HapticProtocol.Decode(bytes,40,out decoded) && decoded.Magnitude==10000 && decoded.Session==1,"wire roundtrip");
        for(int count=0;count<40;count++) Check(!HapticProtocol.Decode(bytes,count,out decoded),"truncated packet");
        bytes[0]=0; Check(!HapticProtocol.Decode(bytes,40,out decoded),"bad magic");
        HapticProtocol.Encode(f,bytes); bytes[36]=1; Check(!HapticProtocol.Decode(bytes,40,out decoded),"reserved/version rejection");
        f.Magnitude=10001; HapticProtocol.Encode(f,bytes); Check(!HapticProtocol.Decode(bytes,40,out decoded),"over range");
        f=Frame(); var pulse=new LandingPulse(); Check(pulse.Accept(f,1000),"baseline");
        f=Frame(1020,2,1,10000,true); Check(pulse.Accept(f,1020),"event");
        Check(pulse.Value(1020)==0 && pulse.Value(1030)==100 && pulse.Value(1050)==100,"attack and hold");
        Check(pulse.Value(1100)>0 && pulse.Value(1100)<100 && pulse.Value(1200)==0,"decay and expiry");
        for(uint ms=0;ms<500;ms++) Check(pulse.Value(1020+ms)>=0 && pulse.Value(1020+ms)<=100,"bounded output");
        Check(!pulse.Accept(f,1040),"duplicate sequence");
        f.Sequence++; f.Tick=1040; pulse.Accept(f,1040); Check(pulse.Accepted==1,"duplicate event no retrigger");
        f=Frame(1060,4,1,0,false); pulse.Accept(f,1060); Check(pulse.Value(1060)==0,"pause cancels");
        f=Frame(1080,5,1,10000,true); pulse.Accept(f,1080); Check(pulse.Value(1090)==0,"resume no stale event");
        Check(!pulse.Accept(Frame(2000),1000),"future frame");
        Check(!pulse.Accept(Frame(100),1000),"stale frame");
        var joining=new LandingPulse(); joining.Accept(Frame(1000,1,7,10000,true),1000); Check(joining.Value(1010)==0,"join skips already running event");
        joining.Accept(Frame(1020,2,8,10000,true),1020); Check(joining.Value(1030)==100,"new event after join");
        joining.Accept(Frame(1040,1,1,10000,true,2),1040); Check(joining.Value(1050)==0,"session switch drops old output");
        Check(!joining.Accept(Frame(1020,3,9,10000,true,1),1050),"old session frame");
        var wrap=new LandingPulse(); wrap.Accept(Frame(uint.MaxValue-20,uint.MaxValue,0,0,false),uint.MaxValue-20);
        wrap.Accept(Frame(5,0,1,5000,true),5); Check(wrap.Value(15)==50,"tick and sequence wrap");
        using(var receiver=new HapticReceiver(0))
        using(var sender=new HapticSender(receiver.Port))
        {
            sender.Update(false,HapticProtocol.Now); Check(Wait(()=>receiver.Connected),"UDP baseline received");
            sender.Update(true,HapticProtocol.Now,5000); Check(Wait(()=>receiver.Value>0),"UDP pulse delivered");
            Check(receiver.Value<=50,"gain respected");
            sender.Update(false,HapticProtocol.Now); Check(Wait(()=>receiver.Value==0),"UDP park");
            Thread.Sleep(25); sender.Update(true,HapticProtocol.Now,10000); Check(Wait(()=>receiver.Accepted==2),"second event");
            Check(Wait(()=>receiver.Value==0,400),"expiry without more packets");
            Check(Wait(()=>!receiver.Connected,400),"disconnect timeout");
            bool busy=false; try { using(var other=new HapticReceiver(receiver.Port)) {} } catch(SocketException) { busy=true; }
            Check(busy,"exclusive loopback bind");
            using(var bad=new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp))
            { bad.SendTo(new byte[41],new IPEndPoint(IPAddress.Loopback,receiver.Port)); }
            Check(Wait(()=>receiver.Rejected>0),"malformed datagram rejected");
        }
        var timer=Stopwatch.StartNew(); for(int i=0;i<4;i++) new HapticReceiver(0).Dispose();
        Check(timer.ElapsedMilliseconds<2000,"bounded receiver shutdown");
        GameAdapter();
        Console.WriteLine("{\"status\":\"passed\",\"assertions\":"+_checks+",\"audioOutput\":false}");
    }
    private static void GameAdapter()
    {
        using(var receiver=new HapticReceiver(0))
        {
            var sender=new HapticSender(receiver.Port);
            typeof(ShakerLanding).GetField("_sender",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,sender);
            GameState.IsDriving=false; ShakerLanding.Tick(); Check(Wait(()=>receiver.Connected),"game idle baseline");
            GameState.IsDriving=true;
            var car=new CarDynamics(); int frame=0;
            Action<bool> step=ground => {
                frame++; Time.fixedTime=frame/60f; Time.realtimeSinceStartup=frame/60f;
                car.Body.position=new Vector3(frame/6f,0,0); car.Body.velocity=new Vector3(10,-8,0);
                car.axles.frontAxle.leftWheel.onGroundDown=car.axles.frontAxle.rightWheel.onGroundDown=ground;
                car.axles.rearAxle.leftWheel.onGroundDown=car.axles.rearAxle.rightWheel.onGroundDown=ground;
                ShakerLanding.Observe(car);
            };
            for(int i=0;i<15;i++) step(true); for(int i=0;i<20;i++) step(false); step(true);
            Check(Wait(()=>receiver.Accepted==1),"game landing despite wheel FFB disabled");
            Check(receiver.Value<=50,"game independent strength bound");
            GameState.IsDriving=false; ShakerLanding.Tick(); Check(Wait(()=>receiver.Value==0),"game pause clears pulse");
            GameState.IsDriving=true; for(int i=0;i<20;i++) step(false); step(true); Thread.Sleep(15);
            Check(receiver.Accepted==1,"resume in air cannot synthesize landing");
            Action jump=()=> { for(int i=0;i<15;i++) step(true); for(int i=0;i<20;i++) step(false); step(true); };
            ModMain.Settings.ShakerLandingStrength=1000; jump();
            Check(Wait(()=>receiver.Accepted==2) && receiver.Value<=100,"corrupt strength clamped");
            Application.isFocused=false; ShakerLanding.Tick(); Check(Wait(()=>receiver.Value==0),"focus loss clears");
            Application.isFocused=true; jump(); Check(Wait(()=>receiver.Accepted==3),"new jump after focus recovery");
            GameState.IsRestarting=true; ShakerLanding.Tick(); Check(Wait(()=>receiver.Value==0),"restart clears");
            GameState.IsRestarting=false; jump(); Check(Wait(()=>receiver.Accepted==4),"new jump after restart");
            Time.realtimeSinceStartup+=1; ShakerLanding.Tick(); Check(Wait(()=>receiver.Value==0),"physics stall clears");
            jump(); Check(Wait(()=>receiver.Accepted==5),"fresh samples after stall");
            car.axles.frontAxle.leftWheel=null; ShakerLanding.Observe(car);
            Check(Wait(()=>receiver.Value==0),"missing wheel data clears");
            car.axles.frontAxle.leftWheel=new Wheel(); ModMain.Settings.ShakerLandingStrength=50;
            ModMain.Settings.TelemetryEnabled=false; ShakerLanding.Tick(); Check(Wait(()=>!receiver.Connected,400),"telemetry disable closes channel");
            ModMain.Settings.TelemetryEnabled=true; ShakerLanding.Shutdown();
        }
    }
}
