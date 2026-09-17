using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using System.Xml.Linq;
using ArtOfSimRally.Mod;
using Native=Dbce.Wheel.Ffb.WheelFfbNative;
using Mod=ArtOfSimRally.Mod.Main;

static class Program
{
    static int assertions;
    static void Check(bool ok,string why) { assertions++;if(!ok)throw new Exception(why); }
    static LandingSample Sample(double t,int mask,float vy=-6) => new(){Time=t,Contacts=mask,X=(float)t*10,Y=(float)t*vy,Vx=10,Vy=vy,UpY=1};
    static LandingSignal Armed(double dt=.02)
    {
        var d=new LandingSignal();
        for(double t=0;t<.25;t+=dt) Check(d.Observe(Sample(t,15))==0,"ground triggered");
        return d;
    }
    static void Detector()
    {
        foreach(double dt in new[]{1.0/30,1.0/60,1.0/120})
        {
            var d=new LandingSignal();int events=0;
            for(int i=0;i<Math.Round(2/dt);i++)
            {
                double t=i*dt;int contact=t<.3||t>=1?15:0;
                float result=d.Observe(Sample(t,contact));
                if(result>0){events++;Check(t>=1&&t<1+dt,"delayed landing");Check(result>0&&result<=1,"unbounded cue");}
            }
            Check(events==1,"one jump should produce one event at each sample rate");
        }
        // A roll, spawn in flight, one lifted wheel or a tiny road contact gap
        // must not turn into a jump event.
        foreach(string scenario in new[]{"spawn","jitter","partial","roll","upward","teleport","gap","backwards","invalid","reset"})
        {
            var d=new LandingSignal();int events=0;
            for(int i=0;i<100;i++)
            {
                double t=i*.02;var s=Sample(t,t<.3||t>=1?15:0);
                if(scenario=="spawn"&&t<1)s.Contacts=0;
                if(scenario=="jitter")s.Contacts=i%4==0?0:15;
                if(scenario=="partial")s.Contacts=i%2==0?3:12;
                if(scenario=="roll")s.UpY=-1;
                if(scenario=="upward"){s.Vy=3;s.Y=(float)t*3;}
                if(scenario=="teleport"&&i>=50)s.X+=100;
                if(scenario=="gap"&&i>=50)s.Time+=2;
                if(scenario=="backwards"&&i>=50)s.Time-=1;
                if(scenario=="invalid"&&i==49)s.Vy=float.NaN;
                if(scenario=="reset"&&i==49)d.Reset();
                if(d.Observe(s)>0)events++;
            }
            Check(events==0,"spurious landing: "+scenario);
        }
        // Rear contact starts the cue; delayed front contact is the same landing.
        var split=Armed();for(double t=.26;t<1;t+=.02)split.Observe(Sample(t,0));
        Check(split.Observe(Sample(1,8))>0,"rear-first touchdown missed");
        Check(split.Observe(Sample(1.02,15))==0,"front contact duplicated landing");
        for(int i=0;i<10000;i++)split.Observe(Sample(2+i*.02,15));
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<100000;i++)split.Observe(Sample(202+i*.02,15));
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"detector allocates per sample");
    }
    sealed class Output:ILandingOutput
    {
        public int Creates,Plays,Stops,Releases,Hz,Duration; public bool FailCreate,FailPlay,FailStop;public float Magnitude;
        public int Create(ImpactKind kind,int hz,int duration){Creates++;Hz=hz;Duration=duration;return FailCreate?-1:2;}
        public bool Play(ImpactKind kind,int slot,float magnitude,float hz){Plays++;Magnitude=magnitude;return !FailPlay;}
        public bool Stop(ImpactKind kind,int slot){Stops++;return !FailStop;}
        public void Release(){Releases++;}
    }
    static void Delivery()
    {
        var o=new Output();var f=new LandingFeedback(o);
        f.Prepare(false,true,true);Check(o.Creates==0,"off creates effects");
        f.Prepare(true,true,false);Check(o.Creates==0,"driving creates effects");
        f.Prepare(true,true,true);Check(o.Creates==1&&o.Hz==25&&o.Duration==120,"incorrect finite burst");
        Check(f.Trigger(1,5,1)&&o.Magnitude==.05f,"low strength not forwarded");
        f.Tick(1.10);Check(o.Stops==0,"burst stopped early");f.Tick(1.121);Check(o.Stops==1,"burst did not stop");
        Check(f.Trigger(100,100,2)&&o.Magnitude==.4f,"malformed tune exceeded cap");
        f.Prepare(false,true,false);Check(o.Stops==2&&o.Releases==1,"disable left burst alive");
        f.Prepare(true,false,true);Check(o.Creates==1,"missing device created effect");
        f.Prepare(true,true,true);Check(o.Creates==2,"reinitialisation didn't rebuild slot");
        int count=o.Plays;foreach(float bad in new[]{float.NaN,float.PositiveInfinity,-1,0})Check(!f.Trigger(bad,5,3),"invalid intensity played");
        Check(!f.Trigger(1,float.NaN,3)&&!f.Trigger(1,5,double.NaN)&&o.Plays==count,"invalid tune/time played");
        o.FailPlay=true;Check(!f.Trigger(1,5,3)&&f.Rejected==1&&!f.Available,"failed delivery not surfaced");
        f.Prepare(true,true,true);Check(o.Creates==2,"failed event automatically retried");
        f.Prepare(false,true,true);f.Prepare(true,true,true);Check(o.Creates==3,"explicit retry failed");
        o.FailPlay=false;o.FailStop=true;f.Trigger(1,5,4);f.Stop();Check(!f.Available&&o.Releases>=3,"failed stop didn't release slot");
        o=new Output{FailCreate=true};f=new LandingFeedback(o);
        for(int i=0;i<100;i++)f.Prepare(true,true,true);
        Check(o.Creates==1&&!f.Available,"unsupported wheel retried every frame");

        o=new Output();f=new LandingFeedback(o);f.Prepare(true,true,true);
        foreach(var tune in new[]{(1f,5f,.05f),(1f,20f,.2f),(1f,30f,.3f),(1f,40f,.4f),
            (.5f,40f,.2f),(1f,float.MaxValue,.4f),(float.MaxValue,40f,.4f)})
        {
            Check(f.Trigger(tune.Item1,tune.Item2,10)&&o.Magnitude==tune.Item3,"extended strength or legacy output incorrect");
            Check(f.LastMagnitude==o.Magnitude,"reported and delivered amplitudes differ");
        }
        count=o.Plays;
        foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1,0})
            Check(!f.Trigger(1,bad,11)&&o.Plays==count,"invalid strength sent to wheel");
        int stops=o.Stops;f.Tick(10.121);Check(o.Stops==stops+1,"maximum-strength cue did not stop");
    }
    static void GameIntegration()
    {
        foreach(float strength in new[]{5f,20f,30f,40f})
        foreach(string interrupt in new[]{"none","pause","focus","disable","ffb-off","not-ready","restart","missing-body","missing-wheel","car-change","wall-gap"})
        {
            ImpactController.Shutdown();Mod.Enabled=true;Mod.Settings=new(){LandingStrength=strength};FfbNative.Ready=true;
            UnityEngine.Application.isFocused=true;GameState.IsRestarting=false;GameState.IsDriving=false;
            ImpactController.Tick();GameState.IsDriving=true;
            int before=Native.Plays;var car=new CarDynamics();
            for(int i=0;i<75;i++)
            {
                float t=i*.02f;UnityEngine.Time.fixedTime=t;UnityEngine.Time.realtimeSinceStartup=t;
                if(interrupt=="wall-gap"&&i>=49)UnityEngine.Time.realtimeSinceStartup+=2;
                int mask=t<.3f||t>=1?15:0;
                car.body.position=new(t*10,-t*6,0);car.body.velocity=new(10,-6,0);
                car.axles.frontAxle.leftWheel.onGroundDown=car.axles.frontAxle.rightWheel.onGroundDown=(mask!=0);
                car.axles.rearAxle.leftWheel.onGroundDown=car.axles.rearAxle.rightWheel.onGroundDown=(mask!=0);
                if(i==49)
                {
                    if(interrupt=="pause")GameState.IsDriving=false;
                    if(interrupt=="focus")UnityEngine.Application.isFocused=false;
                    if(interrupt=="disable")Mod.Settings.LandingEffectsEnabled=false;
                    if(interrupt=="ffb-off")Mod.Settings.ForceFeedbackEnabled=false;
                    if(interrupt=="not-ready")FfbNative.Ready=false;
                    if(interrupt=="restart")GameState.IsRestarting=true;
                    if(interrupt=="car-change")car=new CarDynamics();
                    if(interrupt=="missing-body")car=new CarDynamics{body=null};
                    if(interrupt=="missing-wheel")car.axles.rearAxle.rightWheel=null;
                }
                ImpactController.Tick();LandingController.Observe(car);
                if(i==49)
                {
                    GameState.IsDriving=true;GameState.IsRestarting=false;UnityEngine.Application.isFocused=true;
                    Mod.Settings.LandingEffectsEnabled=Mod.Settings.ForceFeedbackEnabled=true;FfbNative.Ready=true;
                    if(car.body==null)car.body=new();
                    car.axles.rearAxle.rightWheel??=new();
                }
            }
            Check(Native.Plays-before==(interrupt=="none"?1:0),"game lifecycle triggered stale/missing cue: "+interrupt);
            if(interrupt=="none")Check(Math.Abs(Native.LastMagnitude-strength/100f*(4.5f/8.5f))<.000001f,"landing controller lost configured strength");
        }
        // Interrupt an already active burst, not just its detector history.
        ImpactController.Shutdown();
        var output=new Output();var feedback=new LandingFeedback(output);feedback.Prepare(true,true,true);feedback.Trigger(1,5,0);
        feedback.Shutdown();Check(output.Stops==1&&output.Releases==1,"shutdown left active effect");
    }
    static string Hash(string p)=>Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(p)));
    static void TimingDiagnostics()
    {
        var t=new LandingTiming(); t.Start(1,8,0);
        Check(t.Pending&&t.CompressionDelayMs<0&&t.AllWheelsDelayMs<0,"ray contact claimed compression/all-wheel timing");
        Check(t.Observe(1.02,15,.3f)&&Math.Abs(t.CompressionDelayMs-20)<.001&&Math.Abs(t.AllWheelsDelayMs-20)<.001,"timing did not observe following sample");
        t.Start(2,15,.2f); Check(!t.Pending&&t.CompressionDelayMs==0&&t.AllWheelsDelayMs==0,"immediate compression should not be delayed");
        t.Start(3,8,0);t.Observe(3.05,8,0);t.Observe(3.1,0,.5f);t.Observe(3.15,8,0);t.Observe(3.21,8,.5f);
        Check(!t.Pending&&t.CompressionDelayMs<0&&t.AllWheelsDelayMs<0,"stale/airborne compression accepted or observation unbounded");
        foreach(double bad in new[]{double.NaN,double.PositiveInfinity,3.9,4.2})
        {t.Start(4,8,0);t.Observe(bad,15,.5f);Check(!t.Pending&&t.CompressionDelayMs<0,"invalid/discontinuous clock fabricated timing");}
        t.Start(5,8,0);t.Cancel();Check(t.Available&&!t.Pending&&t.Status.Contains("Interrupted"),"reset lost distinction between missing and zero delay");
        Check(LandingTiming.Ratio(false,1,1)==0&&LandingTiming.Ratio(true,1,0)==0&&LandingTiming.Ratio(true,float.NaN,1)==0,"invalid compression used");
        for(int i=0;i<1000;i++){t.Start(i,8,0);t.Observe(i+.02,15,.5f);}
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=1000;i<11000;i++){t.Start(i,8,0);t.Observe(i+.02,15,.5f);}
        Check(GC.GetAllocatedBytesForCurrentThread()==before,"landing diagnostic sampling allocates");
    }
    static object Capture(string folder)
    {
        string path=Path.Combine(folder,"signals.csv");var manifest=XDocument.Load(Path.Combine(folder,"manifest.xml"));
        Check(string.Equals(Hash(path),manifest.Root.Element("signals")?.Value,StringComparison.OrdinalIgnoreCase),"capture signals hash changed");
        var rows=File.ReadAllLines(path);string[] header=rows[0].Split(',');
        var d=new LandingSignal();var timing=new LandingTiming();int events=0,first=-1,epoch=-1;float peak=0;
        for(int row=1;row<rows.Length;row++)
        {
            string[] c=rows[row].Split(',');float F(string name)=>float.Parse(c[Array.IndexOf(header,name)],CultureInfo.InvariantCulture);
            int nextEpoch=(int)F("epoch");if(nextEpoch!=epoch){d.Reset();timing.Cancel();epoch=nextEpoch;}
            if(F("valid")==0){d.Reset();timing.Cancel();continue;}
            float qx=F("qx"),qz=F("qz");
            float result=d.Observe(new LandingSample{Time=F("physics_time_s"),Contacts=(int)F("contact_mask"),
                X=F("px_m"),Y=F("py_m"),Z=F("pz_m"),Vx=F("vx_mps"),Vy=F("vy_mps"),Vz=F("vz_mps"),UpY=1-2*(qx*qx+qz*qz)});
            Check(result>=0&&result<=1,"capture produced invalid cue");
            float compression=0;int mask=(int)F("contact_mask");int bit=1;
            foreach(string wheel in new[]{"fl","fr","rl","rr"})
            {compression=Math.Max(compression,LandingTiming.Ratio((mask&bit)!=0,F("compression_"+wheel+"_m"),F("travel_"+wheel+"_m")));bit<<=1;}
            if(d.Discontinuous)timing.Cancel();
            timing.Observe(F("physics_time_s"),mask,compression);
            if(result>0){events++;first=row-1;peak=Math.Max(peak,result);timing.Start(F("physics_time_s"),mask,compression);}
        }
        Check(events==1&&first==4230,"recorded drive must match owner's one landing at row 4230");
        Check(peak==1,"recorded 10m/s descent should reach configured cue maximum");
        Check(timing.ContactMask==8&&timing.CompressionAtContact==0&&timing.CompressionDelayMs>16&&timing.CompressionDelayMs<18&&timing.AllWheelsDelayMs==timing.CompressionDelayMs,"recorded contact/load timing changed");
        return new{events,firstContactRow=first,peakCue=peak,compressionDelayMs=timing.CompressionDelayMs,allWheelsDelayMs=timing.AllWheelsDelayMs,signalsSha256=Hash(path),nativeOutput=false};
    }
    static int Main(string[] args)
    {
        try{ImpactController.MonotonicNow=()=>UnityEngine.Time.realtimeSinceStartup;
            Detector();Delivery();GameIntegration();TimingDiagnostics();assertions+=CrashTests.Run();assertions+=ImpactPlaybackTests.Run();object capture=args.Length==1?Capture(args[0]):null;
            Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions,capture}));return 0;}
        catch(Exception e){Console.Error.WriteLine(e);return 1;}
    }
}
