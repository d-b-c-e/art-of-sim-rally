using System.Text;
using System.Text.Json;
using ArtOfSimRally.Mod;

static class Program
{
    static int assertions;
    static void Check(bool ok,string message) { assertions++; if(!ok) throw new Exception(message); }
    static void Logs(string root)
    {
        string path=Path.Combine(root,"ffb.log");
        using(var writer=new StreamWriter(path,false,new UTF8Encoding(false)))
        {
            for(int i=0;i<60000;i++) writer.WriteLine("00:00:01.001 SetDeviceForcesXY(1000, 0)");
            for(int i=0;i<500;i++) writer.WriteLine("lifecycle old "+i);
            writer.WriteLine("late SetParameters FAILED 0x80040205");
            writer.WriteLine("SetDeviceForcesXY(-2147483648, 0)");
            writer.WriteLine("SetDeviceForcesXY(9999999999999999999999, 0)");
            writer.WriteLine("unicode café 日本語");
        }
        var snapshot=SupportLogs.ReadTail(path);
        Check(snapshot.FileBytes>SupportLogs.MaxBytes && snapshot.ReadBytes==SupportLogs.MaxBytes,"read entire large log");
        Check(snapshot.Lines.Length==SupportLogs.MaxLines && snapshot.Truncated,"line/byte window not bounded");
        Check(snapshot.Lines.Last()=="unicode café 日本語","UTF8 tail damaged");
        var summary=new StringBuilder(); SupportLogs.AppendNative(summary,snapshot);
        string text=summary.ToString();
        Check(text.Contains("late SetParameters FAILED"),"late error lost behind first 400 lines");
        Check(!text.Contains("lifecycle old 0"+Environment.NewLine),"oldest lifecycle retained");
        Check(text.Contains("-2147483648"),"minimum integer caused failure");
        Check(text.Contains("9999999999999999999999"),"malformed force line discarded");
        Check(text.Contains("not measured torque"),"commands presented as torque proof");
        using(var writer=new FileStream(path,FileMode.Append,FileAccess.Write,FileShare.ReadWrite))
        {
            writer.Write(Encoding.UTF8.GetBytes("concurrent writer marker\n")); writer.Flush();
            Check(SupportLogs.ReadTail(path,maxLines:2).Lines.Last()=="concurrent writer marker","active writer blocked snapshot");
        }
        File.WriteAllText(path,new string('x',SupportLogs.MaxLineChars+5000)+"\nrecent error\n");
        snapshot=SupportLogs.ReadTail(path);
        Check(snapshot.Truncated && snapshot.Lines[0].Length<2100 && snapshot.Lines.Last()=="recent error","oversized line");
        File.WriteAllText(path,"ééééé\nvalid after partial line\n"); snapshot=SupportLogs.ReadTail(path,maxBytes:28);
        Check(snapshot.Truncated && snapshot.Lines.Last()=="valid after partial line","partial UTF8 line");
        File.WriteAllText(path,""); snapshot=SupportLogs.ReadTail(path);
        Check(snapshot.ReadBytes==0 && snapshot.Lines.Length==0,"empty file failed");
        summary.Clear(); SupportLogs.AppendTail(summary,Path.Combine(root,"missing.log"),10);
        Check(summary.ToString().Contains("log unavailable"),"missing log not explained");
        bool rejected=false; try { SupportLogs.ReadTail(path,maxLines:0); } catch(ArgumentOutOfRangeException) { rejected=true; }
        Check(rejected,"invalid limits accepted");
    }
    static void Timing()
    {
        var h=new FrameHealth(); h.Observe(false,true,0); h.Observe(false,true,10);
        Check(h.Frames==0,"diagnostics off counted");
        h.Observe(true,false,20); h.Observe(true,true,50); Check(h.Frames==0,"loading counted");
        h.Observe(true,true,50.2); Check(h.EarlyHitches==1 && h.LaterHitches==0,"early hitch");
        for(int i=1;i<=2000;i++) h.Observe(true,true,50.2+i*.01);
        h.Observe(true,true,70.5); Check(h.LaterHitches==1 && h.MaximumMs>299 && h.MaximumMs<301,"later hitch");
        long frames=h.Frames; h.Observe(true,false,71); h.Observe(true,true,90); Check(h.Frames==frames,"pause/focus transition");
        h.Observe(true,true,90.02); Check(h.Frames==frames+1,"resume failed");
        h.Observe(true,true,double.NaN); h.Observe(true,true,100); Check(h.LaterHitches==1,"invalid clock polluted");
        h.Observe(true,true,1); Check(h.LaterHitches==1,"rollback counted");
        h.Observe(false,false,2); h.Observe(true,true,3); Check(h.Frames==0 && h.EarlyHitches==0 && h.MaximumMs==0,"window reset");
        for(int i=0;i<1000;i++) h.Observe(true,true,3+i*.016);
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=1000;i<101000;i++) h.Observe(true,true,3+i*.016);
        long allocated=GC.GetAllocatedBytesForCurrentThread()-before; Check(allocated==0,"hot path allocated "+allocated);
        var summary=new StringBuilder(); h.Append(summary); Check(summary.ToString().Contains("not stage identifiers"),"segment scope omitted");
        h=new FrameHealth();h.Observe(true,true,0);double now=0;
        for(int i=1;i<=2000;i++)
        {
            now+=.01;
            if(i==100)now+=.03;
            if(i==600)now+=.06;
            if(i==1600)now+=.12;
            h.Observe(true,true,now);
        }
        Check(h.FirstFive.Over33Ms==1&&h.FirstFive.Over50Ms==0&&h.FirstFive.Over100Ms==0,"small first-five-second hitch was invisible");
        Check(h.NextTen.Over33Ms==1&&h.NextTen.Over50Ms==1&&h.NextTen.Over100Ms==0,"middle window threshold counters");
        Check(h.Later.Over33Ms==1&&h.Later.Over50Ms==1&&h.Later.Over100Ms==1,"late window threshold counters");
        Check(h.FirstFive.Frames+h.NextTen.Frames+h.Later.Frames==h.Frames&&h.EarlyHitches==0&&h.LaterHitches==1,"detailed counters disagree with legacy totals");
        h.Observe(true,false,now+1);h.Observe(true,true,now+100);h.Observe(true,true,now+100.07);
        Check(h.FirstFive.Over50Ms==1&&h.FirstFive.Over100Ms==0,"pause gap counted or resume window not restarted");
        h.Observe(false,false,now+101);h.Observe(true,true,now+102);
        Check(h.FirstFive.Frames==0&&h.NextTen.Frames==0&&h.Later.Frames==0,"old detailed counters survive a new diagnostic window");
    }
    static int Main()
    {
        try
        {
            string root=Path.GetFullPath(Path.Combine("results","support-"+Guid.NewGuid().ToString("N")));
            Directory.CreateDirectory(root); Logs(root); Timing(); assertions+=FrameHealthRetentionTests.Run(root);
            Console.WriteLine(JsonSerializer.Serialize(new{status="passed",assertions,scope="bounded real log files; zero-allocation frame aggregates"})); return 0;
        }
        catch(Exception ex) { Console.Error.WriteLine(ex); return 1; }
    }
}
