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
        public int Legacy,Constant,Stops,Releases,Creates;
        public bool RejectConstant,RejectStop;
        public ImpactKind? RejectCreate,Active;
        public float Magnitude,Hz;
        public int Create(ImpactKind kind,int hz,int ms)
        { Creates++;Check(hz==(kind==ImpactKind.Landing?25:0)&&ms==120,"idle finite effect changed");return RejectCreate==kind?-1:kind==ImpactKind.Landing?1:7; }
        public bool Play(ImpactKind kind,int slot,float magnitude,float hz)
        {
            Check(Active==null,"new impact began before old output stopped");
            Check(slot==(kind==ImpactKind.Landing?1:7),"effect used wrong native family/slot");
            if(kind==ImpactKind.Landing)Legacy++;else Constant++;
            Magnitude=magnitude;Hz=hz;Now+=Delay;
            bool accepted=kind!=ImpactKind.Crash||!RejectConstant;
            if(accepted)Active=kind;return accepted;
        }
        public bool Stop(ImpactKind kind,int slot)
        { Check(Active==null||Active==kind,"stopped the other impact family");Stops++;if(!RejectStop)Active=null;return !RejectStop; }
        public void Release() { Releases++;Active=null; }
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
                Check(o.Constant==1&&o.Legacy==0&&o.Hz==0,"crash did not use constant pulse");
            else Check(o.Legacy==1&&o.Constant==0&&o.Hz==25,"landing waveform changed");
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
        // A weaker cue cannot steal a crash. A stronger landing explicitly stops
        // the constant handle before playing the separate sine handle.
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);o.Now+=.02;
            Check(m.Trigger(ImpactKind.Landing,1,10,1.02)==ImpactResult.Suppressed&&o.Legacy==0,"weak landing stole kick");
            Check(m.Trigger(ImpactKind.Landing,1,30,1.02)==ImpactResult.Accepted&&o.Legacy==1&&o.Hz==25&&o.Stops==1,"crash and landing overlapped");
            var c=m.Counts(ImpactKind.Crash);Check(c.Delivery.Reason=="replaced"&&c.EarlyStops==1,"replacement lacks timing evidence");
            m.Shutdown();
        }
        // Unsupported/adjusted driver parameters must disable crash only, without
        // retrying stale events, and leave legacy landings available.
        {
            var o=new Output{RejectConstant=true};var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            Check(m.Trigger(ImpactKind.Crash,1,20,1)==ImpactResult.Rejected,"rejected constant accepted");
            Check(!m.Available(ImpactKind.Crash)&&m.Available(ImpactKind.Landing)&&o.Releases==0,"crash rejection disabled landing");
            for(int i=0;i<1000;i++){m.Prepare(true,true,true,true);m.Trigger(ImpactKind.Crash,1,20,1);}
            Check(o.Constant==1,"failed crash automatically retried");
            Check(m.Trigger(ImpactKind.Landing,1,5,1)==ImpactResult.Accepted&&o.Legacy==1,"landing cannot recover after rejected shape");
            m.Stop();m.Prepare(true,false,true,true);m.Prepare(true,true,true,false);
            Check(!m.Available(ImpactKind.Crash),"crash retried before pause");
            m.Prepare(true,true,true,true);o.RejectConstant=false;
            Check(m.Trigger(ImpactKind.Crash,1,5,1)==ImpactResult.Accepted&&o.Constant==2,"explicit paused retry failed");m.Shutdown();
        }
        foreach(double invalid in new[]{double.NaN,double.PositiveInfinity,99.0})
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);o.Now=invalid;m.Tick(1.01);
            Check(o.Stops==1&&m.Counts(ImpactKind.Crash).Delivery.Reason=="clock-discontinuity","invalid clock retained output");m.Shutdown();
        }
        {
            var o=new Output{RejectConstant=true};var m=new ImpactMixer(o,()=>o.Now);
            m.Prepare(true,false,true,true);m.Prepare(true,true,true,false);
            Check(m.Trigger(ImpactKind.Crash,1,20,1)==ImpactResult.Unavailable&&o.Creates==1,"new constant allocated while driving");
            m.Prepare(true,true,true,true);m.Trigger(ImpactKind.Crash,1,20,1);m.Prepare(true,true,true,true);
            Check(!m.Available(ImpactKind.Crash),"pause alone retried a crash enabled before failure");m.Shutdown();
        }
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);m.Tick(.5);
            Check(o.Stops==1,"backwards game clock retained output");m.Shutdown();
        }
        {
            var o=new Output{RejectConstant=true,RejectStop=true};var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Crash,1,20,1);
            Check(o.Releases==1&&!m.Available(ImpactKind.Landing),"failed rejection cleanup retained unsafe slot");m.Shutdown();
        }
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            foreach(float strength in new[]{5f,20f,40f,50f,75f,100f,float.MaxValue})
            {
                float expected=Math.Min(100f,strength)/100f;
                Check(m.Trigger(ImpactKind.Crash,1,strength,1)==ImpactResult.Accepted&&o.Magnitude==expected,"constant range/clamp mismatch");
                m.Stop();
            }
            Check(m.Trigger(ImpactKind.Crash,.5f,100,1)==ImpactResult.Accepted&&o.Magnitude==.5f,"partial collision lost proportional scale");m.Stop();
            Check(m.Trigger(ImpactKind.Landing,1,100,1)==ImpactResult.Accepted&&o.Magnitude==.4f,"crash range raised landing cap");m.Stop();
            int plays=o.Constant;
            foreach(float bad in new[]{float.NaN,float.PositiveInfinity,float.NegativeInfinity,-1f,0f})
                Check(m.Trigger(ImpactKind.Crash,1,bad,1)==ImpactResult.Unavailable&&o.Constant==plays,"invalid crash gain emitted output");
            m.Trigger(ImpactKind.Crash,1,100,1);o.Now+=.02;
            Check(m.Trigger(ImpactKind.Landing,1,100,1.02)==ImpactResult.Suppressed&&o.Active==ImpactKind.Crash,"landing cap stole full-strength crash");
            m.Shutdown();
        }
        foreach(var failed in new[]{ImpactKind.Landing,ImpactKind.Crash})
        {
            var o=new Output{RejectCreate=failed};var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            var other=failed==ImpactKind.Landing?ImpactKind.Crash:ImpactKind.Landing;
            Check(!m.Available(failed)&&m.Available(other),"unsupported effect disabled the other family");
            Check(m.Trigger(other,1,20,1)==ImpactResult.Accepted,"available effect cannot play independently");
            for(int i=0;i<100;i++)m.Prepare(true,true,true,true);
            Check(o.Creates==2,"failed idle setup retries every frame");m.Shutdown();
        }
        {
            var o=new Output();var m=new ImpactMixer(o,()=>o.Now);m.Prepare(true,true,true,true);
            m.Trigger(ImpactKind.Landing,1,5,1);o.RejectStop=true;
            Check(m.Trigger(ImpactKind.Crash,1,100,1.01)==ImpactResult.Rejected&&o.Constant==0&&o.Releases==1,
                "failed replacement stop started a constant pulse");
            Check(!m.Available(ImpactKind.Landing)&&!m.Available(ImpactKind.Crash),"failed replacement left resources usable");m.Shutdown();
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
