using System.Reflection;
using System.Text.Json;
using ArtOfSimRally.Mod;

static class Program
{
    static int checks;
    static readonly MethodInfo Drive=typeof(FfbController).GetMethod("DriveWheel",BindingFlags.Static|BindingFlags.NonPublic);
    static void Check(bool ok,string message) { checks++; if(!ok) throw new Exception(message); }
    static void Step(CarDynamics car) => Drive.Invoke(null,new object[]{car});
    static CarDynamics Active()
    {
        FfbController.Reset(); FfbNative.Ready=true; GameState.IsDriving=true;
        var car=new CarDynamics(); Step(car);
        Check(FfbNative.Last>0 && car.forceFeedback>0,"fixture never produced an active force"); return car;
    }
    static int Main()
    {
        try
        {
            foreach(int missing in new[]{0,1,2,3})
            {
                var car=Active(); var axles=car.axles;
                if(missing==0) car.axles=null;
                else if(missing==1) axles.frontAxle=null;
                else if(missing==2) axles.frontAxle.leftWheel=null;
                else axles.frontAxle.rightWheel=null;
                Step(car);
                Check(FfbNative.Last==0 && car.forceFeedback==0,"missing axle/wheel left force latched: "+missing);
                car.axles=new Axles(); car.axles.frontAxle.leftWheel.Fy=car.axles.frontAxle.rightWheel.Fy=0;
                Step(car); Check(FfbNative.Last==0,"recovered sample retained stale smoothing");
            }
            var active=Active(); GameState.IsDriving=false; Step(active);
            Check(FfbNative.Last==0 && active.forceFeedback==0,"pause left published game force stale");
            int sends=FfbNative.Sends; Step(active); Check(FfbNative.Sends==sends,"parked force sent repeatedly");
            active=Active(); FfbNative.Ready=false; Step(active);
            active.axles.frontAxle.leftWheel.Fy=active.axles.frontAxle.rightWheel.Fy=0;
            FfbNative.Ready=true; Step(active); Check(FfbNative.Last==0,"not-ready transition retained old force history");
            active=Active(); active.axles.frontAxle.leftWheel.Fy=float.NaN; Step(active);
            Check(FfbNative.Last==0 && active.forceFeedback==0,"invalid sample left game/native force stale");
            Console.WriteLine(JsonSerializer.Serialize(new { status="passed", assertions=checks })); return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
