using ArtOfSimRally.Mod;

// Test the production mixer against deterministic driver latency and failures.
// These assert commands and timing, never claim measured physical wheel output.
static class ImpactPlaybackTests
{
    static int assertions;
    static void Check(bool value,string why) { assertions++;if(!value)throw new Exception(why); }
    sealed class Output:ILandingOutput
    {
        public double Now=100,Delay;
        public int Legacy,Shaped,Stops,Releases;
        public bool RejectShape,RejectStop;
        public float Magnitude,Hz;
        public int Phase,Fade;
        public int Create(int hz,int ms) { Check(hz==25&&ms==120,"idle finite effect changed");return 1; }
        public bool Play(int slot,float magnitude,float hz)
        { Legacy++;Magnitude=magnitude;Hz=hz;Phase=Fade=0;Now+=Delay;return true; }
        public bool PlayShaped(int slot,float magnitude,float hz,int phase,int fadeMs)
        { Shaped++;Magnitude=magnitude;Hz=hz;Phase=phase;Fade=fadeMs;Now+=Delay;return !RejectShape; }
        public bool Stop(int slot) { Stops++;return !RejectStop; }
        public void Release() { Releases++; }
    }
    public static int Run()
    {
        foreach(var kind in new[]{ImpactKind.Landing,ImpactKind.Crash})
        foreach(double latency in new[]{0,.08,.25})
        {
            var o=new Output{Delay=latency};var trace=new List<ImpactDelivery>();
            var m=new ImpactMixer(o,()=>o.Now,trace.Add);m.Prepare(true,true,true,true);
            Check(m.Trigger(kind,1,20,10)==ImpactResult.Accepted&&o.Magnitude==.2f,"same peak comparison changed amplitude");
            Check(Math.Abs(trace[0].PlayLatencyMs-latency*1000)<.001,"native latency not measured");
            if(kind==ImpactKind.Crash)
                Check(o.Shaped==1&&o.Legacy==0&&o.Hz==6.25f&&o.Phase==9000&&o.Fade==120,"crash lost kick/rebound shape");
            else Check(o.Legacy==1&&o.Shaped==0&&o.Hz==25&&o.Phase==0&&o.Fade==0,"landing waveform changed");
            o.Now=100+latency+.119;m.Tick(10+latency+.119);
            Check(o.Stops==0,"slow native call shortened burst");
            Check(m.Trigger(kind,1,10,10+latency+.119)==ImpactResult.Suppressed,"arbitration expired before output");
            o.Now=100+latency+.121;m.Tick(10+latency+.121);
            Check(o.Stops==1&&trace.Count==2&&trace[1].Reason=="duration-elapsed"&&trace[1].ElapsedMs>=120,"full burst duration not retained/reported");
            Check(m.Counts(kind).EarlyStops==0,"expiry misreported as early stop");
            m.Shutdown();
        }
        foreach(string reason in new[]{"focus-lost","not-driving","restart","force-reset","shutdown"})
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,40,1);o.Now+=.025;m.Stop(reason);
            var c=m.Counts(ImpactKind.Crash);
            Check(o.Stops==1&&c.EarlyStops==1&&c.Delivery.Reason==reason&&Math.Abs(c.Delivery.ElapsedMs-25)<.001,"early stop reason/time missing");
            m.Stop(reason);Check(o.Stops==1,"inactive stop sent repeatedly");m.Shutdown();
        }
        // A weaker cue cannot steal a shaped crash. A stronger landing replaces
        // it on the same slot and must request the legacy no-envelope waveform.
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);o.Now+=.02;
            Check(m.Trigger(ImpactKind.Landing,1,10,1.02)==ImpactResult.Suppressed&&o.Legacy==0,"weak landing stole kick");
            Check(m.Trigger(ImpactKind.Landing,1,30,1.02)==ImpactResult.Accepted&&o.Legacy==1&&o.Hz==25&&o.Fade==0,"crash shaping leaked into next landing");
            var c=m.Counts(ImpactKind.Crash);Check(c.Delivery.Reason=="replaced"&&c.EarlyStops==1,"replacement lacks timing evidence");
            m.Shutdown();
        }
        // Unsupported/adjusted driver parameters must disable crash only, without
        // retrying stale events, and leave legacy landings available.
        {
            var o=new Output{RejectShape=true};var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            Check(m.Trigger(ImpactKind.Crash,1,20,1)==ImpactResult.Rejected,"unsupported shape accepted");
            Check(!m.Available(ImpactKind.Crash)&&m.Available(ImpactKind.Landing)&&o.Releases==0,"crash rejection disabled landing");
            for(int i=0;i<1000;i++){m.Prepare(true,true,true,true);m.Trigger(ImpactKind.Crash,1,20,1);}
            Check(o.Shaped==1,"failed crash automatically retried");
            Check(m.Trigger(ImpactKind.Landing,1,5,1)==ImpactResult.Accepted&&o.Legacy==1,"landing cannot recover after rejected shape");
            m.Stop();m.Prepare(true,false,true,true);m.Prepare(true,true,true,false);
            Check(!m.Available(ImpactKind.Crash),"crash retried before pause");
            m.Prepare(true,true,true,true);o.RejectShape=false;
            Check(m.Trigger(ImpactKind.Crash,1,5,1)==ImpactResult.Accepted&&o.Shaped==2,"explicit paused retry failed");m.Shutdown();
        }
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,99.0})
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);o.Now=invalid;m.Tick(1.01);
            Check(o.Stops==1&&m.Counts(ImpactKind.Crash).Delivery.Reason=="clock-discontinuity","invalid clock retained output");m.Shutdown();
        }
        {
            var o=new Output{RejectShape=true};var m=new ImpactMixer(o,()=>o.Now);
            m.Prepare(true,false,true,true);m.Prepare(true,true,true,false);
            m.Trigger(ImpactKind.Crash,1,20,1);m.Prepare(true,true,true,true);
            Check(!m.Available(ImpactKind.Crash),"pause alone retried a crash enabled before failure");m.Shutdown();
        }
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);m.Tick(.5);
            Check(o.Stops==1,"backwards game clock retained output");m.Shutdown();
        }
        {
            var o=new Output{RejectShape=true,RejectStop=true};var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);
            Check(o.Releases==1&&!m.Available(ImpactKind.Landing),"failed rejection cleanup retained unsafe slot");m.Shutdown();
        }
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);for(int i=0;i<10000;i++)m.Tick(1);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100000;i++)m.Tick(1);
            Check(GC.GetAllocatedBytesForCurrentThread()==before,"impact tick allocates per frame");m.Shutdown();
        }
        return assertions;
    }
}
