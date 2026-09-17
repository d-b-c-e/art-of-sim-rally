using System.Reflection;
using System.Text;
using ArtOfSimRally.Mod;
using UnityEngine;
using Native=Dbce.Wheel.Ffb.WheelFfbNative;
using Mod=ArtOfSimRally.Mod.Main;

static class CrashTests
{
    static int assertions;
    static void Check(bool ok,string why) { assertions++;if(!ok)throw new Exception(why); }
    static LandingSample Motion(double time,float velocity=20) => new(){Time=time,X=(float)time*velocity,Vx=velocity};
    static CrashSignal Armed()
    {
        var signal=new CrashSignal();for(int i=0;i<=10;i++)signal.Track(Motion(i*.02));return signal;
    }
    static CrashContact Contact(double time=.2,float speed=20) => new(){Time=time,Rvx=speed,Nx=1};
    static void Detection()
    {
        foreach(double dt in new[]{1.0/30,1.0/50,1.0/60,1.0/120})
        foreach(double angle in new[]{0.0,Math.PI/2,3.0})
        {
            var d=new CrashSignal();for(int i=0;i<=20;i++)d.Track(Motion(i*dt));
            var c=Contact(20*dt);c.Nx=(float)Math.Cos(angle);c.Nz=(float)Math.Sin(angle);
            c.Rvx=c.Nx*30;c.Rvz=c.Nz*30;
            Check(d.Observe(c)==1,"head-on hit lost by sample rate/heading");
            Check(d.Observe(c)==0,"duplicate contact retriggered");
        }
        foreach(string scenario in new[]{"road","vertical","glancing","weak","invalid","bad-normal","stale","backwards","spawn","reset","teleport","gap"})
        {
            var d=Armed();var c=Contact();
            if(scenario=="road")c.Road=true;
            if(scenario=="vertical"){c.Nx=0;c.Ny=1;c.Rvy=30;}
            if(scenario=="glancing"){c.Rvx=2;c.Rvz=80;}
            if(scenario=="weak")c.Rvx=3;
            if(scenario=="invalid")c.Rvx=float.NaN;
            if(scenario=="bad-normal")c.Nx=100;
            if(scenario=="stale")c.Time+=.06;
            if(scenario=="backwards")c.Time-=.01;
            if(scenario=="spawn")d=new CrashSignal();
            if(scenario=="reset")d.Reset();
            if(scenario=="teleport"){var m=Motion(.22);m.X+=100;d.Track(m);c.Time=.22;}
            if(scenario=="gap"){d.Track(Motion(2));c.Time=2;}
            Check(d.Observe(c)==0,"false crash: "+scenario);
        }
        var upgrade=Armed();Check(upgrade.Observe(Contact(.2,8))>0,"moderate impact absent");
        Check(upgrade.Observe(Contact(.2,30))==1,"stronger manifold contact did not upgrade");
        for(int i=11;i<=26;i++){upgrade.Track(Motion(i*.02));Check(upgrade.Observe(Contact(i*.02))==0,"repeated collision buzz");}
        upgrade.Track(Motion(.56));Check(upgrade.Observe(Contact(.56))==1,"new impact after cooldown lost");
        var light=Armed().Observe(Contact(.2,6));var hard=Armed().Observe(Contact(.2,15));
        Check(light>0&&light<hard&&hard<1,"impact severity does not scale");
        var invalid=Armed();var bad=Motion(.22);bad.Vx=float.PositiveInfinity;invalid.Track(bad);
        Check(invalid.Observe(Contact(.22))==0,"invalid motion failed to disarm");
        var noAlloc=Armed();for(int i=11;i<1000;i++)noAlloc.Track(Motion(i*.02));
        long allocated=GC.GetAllocatedBytesForCurrentThread();
        for(int i=1000;i<101000;i++){noAlloc.Track(Motion(i*.02));noAlloc.Observe(Contact(i*.02,2));}
        Check(GC.GetAllocatedBytesForCurrentThread()==allocated,"crash detection allocates while driving");
    }
    sealed class Output:ILandingOutput
    {
        public int Creates,Plays,Stops,Releases;public float Magnitude;public bool FailPlay,FailStop;
        public int Create(int hz,int ms){Creates++;Check(hz==25&&ms==120,"shared burst changed waveform");return 4;}
        public bool Play(int slot,float magnitude,float hz){Plays++;Magnitude=magnitude;return !FailPlay;}
        public bool Stop(int slot){Stops++;return !FailStop;}
        public void Release(){Releases++;}
    }
    static void Mixing()
    {
        foreach(bool crashFirst in new[]{false,true})
        {
            var o=new Output();var m=new ImpactMixer(o);m.Prepare(true,true,true,true);
            var first=crashFirst?ImpactKind.Crash:ImpactKind.Landing;
            var second=crashFirst?ImpactKind.Landing:ImpactKind.Crash;
            Check(m.Trigger(first,1,10,1)==ImpactResult.Accepted,"first cue missing");
            var result=m.Trigger(second,1,10,1.01);
            Check(result==(crashFirst?ImpactResult.Suppressed:ImpactResult.Accepted),"crash did not win equal-strength overlap");
            Check(o.Creates==1&&o.Magnitude==.1f,"overlap allocated second effect or summed amplitude");
            m.Prepare(false,true,true,true);Check(o.Stops==0&&o.Releases==0,"disabling landing killed crash");
            m.Stop(ImpactKind.Landing);Check(o.Stops==0,"landing reset killed crash");
            m.Tick(1.14);Check(o.Stops==1,"finite shared effect did not stop");
            int plays=o.Plays;m.Tick(2);Check(o.Plays==plays,"suppressed event replayed later");
            m.Prepare(false,false,true,true);Check(o.Releases==1,"last owner leaked slot");
        }
        var output=new Output();var mixer=new ImpactMixer(output);mixer.Prepare(true,true,true,false);
        Check(output.Creates==0,"shared allocation occurred while driving");mixer.Prepare(true,true,true,true);
        mixer.Trigger(ImpactKind.Landing,1,20,0);
        Check(mixer.Trigger(ImpactKind.Crash,1,5,.01)==ImpactResult.Suppressed,"weak crash replaced stronger landing");
        mixer.Prepare(true,false,true,true);Check(output.Stops==0,"disabling crash killed landing");
        mixer.Prepare(false,false,true,true);Check(output.Stops==1&&output.Releases==1,"disable left periodic output");
        mixer.Prepare(false,true,true,true);mixer.Trigger(ImpactKind.Crash,100,100,1);
        Check(output.Magnitude==.4f,"shared malformed settings exceeded 40% cap");
        mixer.Prepare(false,true,false,true);Check(!mixer.Available(ImpactKind.Crash)&&output.Stops==2,"device loss failed to stop");
        mixer.Prepare(false,true,true,true);output.FailPlay=true;
        Check(mixer.Trigger(ImpactKind.Crash,1,5,2)==ImpactResult.Rejected,"rejection not reported");
        int creates=output.Creates;mixer.Prepare(false,true,true,true);
        Check(output.Creates==creates&&!mixer.Available(ImpactKind.Crash),"rejected event retried automatically");
        mixer.Prepare(false,false,true,true);mixer.Prepare(true,true,true,true);output.FailPlay=false;
        mixer.Trigger(ImpactKind.Crash,1,5,3);output.FailStop=true;mixer.Stop();
        Check(!mixer.Available(ImpactKind.Landing)&&!mixer.Available(ImpactKind.Crash),"failed shared stop left usable slot");
        mixer.Shutdown();

        output=new Output();mixer=new ImpactMixer(output);mixer.Prepare(true,true,true,true);
        Check(mixer.Trigger(ImpactKind.Landing,1,30,10)==ImpactResult.Accepted&&output.Magnitude==.3f,"30% landing unavailable");
        Check(mixer.Trigger(ImpactKind.Crash,1,20,10.01)==ImpactResult.Suppressed,"old cap made a weaker crash win a false tie");
        Check(mixer.Trigger(ImpactKind.Crash,1,40,10.02)==ImpactResult.Accepted&&output.Magnitude==.4f,"40% crash did not replace weaker landing");
        Check(mixer.Trigger(ImpactKind.Landing,1,35,10.03)==ImpactResult.Suppressed,"weaker landing replaced stronger crash");
        Check(mixer.Trigger(ImpactKind.Landing,100,100,10.04)==ImpactResult.Suppressed,"capped landing defeated crash tie priority");
        Check(output.Creates==1&&output.Plays==2&&output.Magnitude==.4f,"strong effects stacked or allocated a second slot");
        Check(mixer.Counts(ImpactKind.Crash).Magnitude==output.Magnitude,"mixer and device strength disagree");
        mixer.Tick(10.141);Check(output.Stops==1,"strong mixed effect did not stop");
        mixer.Tick(11);Check(output.Plays==2,"suppressed strong cue replayed later");mixer.Shutdown();
    }
    static readonly MethodInfo CollisionHook=typeof(CrashController).GetMethod("BeforeCollision",BindingFlags.NonPublic|BindingFlags.Static);
    static Collision Hit(float speed=30) => new(){relativeVelocity=new(speed,0,0),points=new[]{new ContactPoint{normal=new(1,0,0)}}};
    static CarDynamics SetUp()
    {
        ImpactController.Shutdown();Mod.Enabled=true;Mod.Settings=new(){CrashEffectsEnabled=true};
        Application.isFocused=true;FfbNative.Ready=true;GameState.IsDriving=false;GameState.IsRestarting=false;
        Time.realtimeSinceStartup=Time.fixedTime=0;ImpactController.Tick();GameState.IsDriving=true;
        var car=new CarDynamics();GameState.ExistingManager=new();GameState.ExistingManager.playerManager.playerRigidBody=car.body;
        for(int i=0;i<=10;i++)
        {
            float t=i*.02f;Time.realtimeSinceStartup=Time.fixedTime=t;
            car.body.position=new(t*20,0,0);car.body.velocity=new(20,0,0);CrashController.Track(car);
        }
        return car;
    }
    static void Integration()
    {
        foreach(string interruption in new[]{"none","pause","focus","disabled","ffb-off","not-ready","restart","other-car","other-player","missing-contact","road","vertical","weak-scrape","wall-gap","teleport","reset"})
        {
            var car=SetUp();var player=new PlayerCollider{body=car.body};var hit=Hit();int before=Native.Plays;
            if(interruption=="pause")GameState.IsDriving=false;
            if(interruption=="focus")Application.isFocused=false;
            if(interruption=="disabled")Mod.Settings.CrashEffectsEnabled=false;
            if(interruption=="ffb-off")Mod.Settings.ForceFeedbackEnabled=false;
            if(interruption=="not-ready")FfbNative.Ready=false;
            if(interruption=="restart")GameState.IsRestarting=true;
            if(interruption=="other-car")player.body=new();
            if(interruption=="other-player")GameState.ExistingManager.playerManager.playerRigidBody=new();
            if(interruption=="missing-contact")hit.points=Array.Empty<ContactPoint>();
            if(interruption=="road")hit.collider.Road=true;
            if(interruption=="vertical")hit.points[0].normal=new(0,1,0);
            if(interruption=="weak-scrape")hit.relativeVelocity=new(2,0,100);
            if(interruption=="wall-gap")Time.realtimeSinceStartup+=1;
            if(interruption=="teleport"){car.body.position=new(100,0,0);Time.fixedTime+=.02f;CrashController.Track(car);}
            if(interruption=="reset")ImpactController.Reset();
            CollisionHook.Invoke(null,new object[]{player,hit});
            Check(Native.Plays-before==(interruption=="none"?1:0),"collision gate failed: "+interruption);
            if(interruption=="none")
            {
                Check(Native.LastMagnitude==.05f,"configured default crash strength lost");
                int stops=Native.Stops;GameState.IsDriving=false;ImpactController.Tick();
                Check(Native.Stops==stops+1,"pause failed to stop active crash");
            }
        }
        var active=SetUp();var many=Hit();many.points=Enumerable.Repeat(new ContactPoint{normal=new(1,0,0)},100).ToArray();
        CollisionHook.Invoke(null,new object[]{new PlayerCollider{body=active.body},many});
        Check(many.Reads==8,"unbounded contact work");
        foreach(string stop in new[]{"pause","focus","mod-off","ffb-off","device","restart","crash-off","shutdown"})
        {
            var car=SetUp();Mod.Settings.CrashStrength=40;
            CollisionHook.Invoke(null,new object[]{new PlayerCollider{body=car.body},Hit()});int stops=Native.Stops;
            Check(Native.LastMagnitude==.4f,"crash controller lost extended strength");
            if(stop=="pause")GameState.IsDriving=false;
            if(stop=="focus")Application.isFocused=false;
            if(stop=="mod-off")Mod.Enabled=false;
            if(stop=="ffb-off")Mod.Settings.ForceFeedbackEnabled=false;
            if(stop=="device")FfbNative.Ready=false;
            if(stop=="restart")GameState.IsRestarting=true;
            if(stop=="crash-off")Mod.Settings.CrashEffectsEnabled=false;
            if(stop=="shutdown")ImpactController.Shutdown();else ImpactController.Tick();
            Check(Native.Stops==stops+1,"active crash survived "+stop);
        }
        var text=new StringBuilder();ImpactController.AppendSupport(text);
        Check(text.ToString().Contains("=== Crash vibration ===")&&text.ToString().Contains("overlap suppressed"),"support lacks crash delivery/overlap evidence");
        ImpactController.Shutdown();
    }
    public static int Run(){Detection();Mixing();Integration();return assertions;}
}
