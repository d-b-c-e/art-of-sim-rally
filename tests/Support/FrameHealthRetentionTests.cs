using System.Text;
using System.Xml.Linq;
using ArtOfSimRally.Mod;

static class FrameHealthRetentionTests
{
    static int count;
    static void Check(bool value,string message) { count++; if(!value) throw new Exception(message); }
    static string Summary(FrameHealthStore store) { var sb=new StringBuilder(); store.AppendPrevious(sb); return sb.ToString(); }
    internal static int Run(string root)
    {
        count=0;
        var start=new DateTime(2026,9,9,7,0,0,DateTimeKind.Utc);
        string path=Path.Combine(root,"retention","last-session-frame-health.xml");
        var empty=new FrameHealth(); var store=new FrameHealthStore(path,"0.2.5-test+fixture",start);
        Check(!store.Flush(empty,false,true,1,start.AddSeconds(1)) && !Directory.Exists(Path.GetDirectoryName(path)),"disabled diagnostics wrote a file/directory");
        var health=new FrameHealth(); health.Observe(true,true,0); health.Observe(true,true,.2);
        Check(!store.Flush(health,true,false,1,start.AddSeconds(1)) && !File.Exists(path),"driving snapshot wrote to disk");
        Check(store.Flush(health,false,false,2,start.AddSeconds(2)),"idle summary not retained");
        Check(new FileInfo(path).Length<FrameHealthStore.MaxBytes,"unbounded summary");
        string first=File.ReadAllText(path);
        Check(XElement.Parse(first).Attribute("build").Value=="0.2.5-test+fixture","build identity missing");
        Check(!store.Flush(health,false,false,3,start.AddSeconds(3)) && store.Writes==1,"unchanged idle rewrote summary");
        health.Observe(true,true,.5);
        using (File.Open(path,FileMode.Open,FileAccess.ReadWrite,FileShare.None))
            Check(!store.Flush(health,false,false,8,start.AddSeconds(8)) && store.LastError!=null,"locked snapshot was reported saved");
        Check(File.ReadAllText(path)==first,"failed write damaged previous snapshot");
        Check(!Directory.EnumerateFiles(Path.GetDirectoryName(path),"*.tmp").Any(),"failed write leaked temporary file");
        Check(!store.Flush(health,false,false,9,start.AddSeconds(9)),"failed write retried before backoff");
        Check(store.Flush(health,false,false,14,start.AddSeconds(14)) && store.LastError==null,"failed write did not recover");
        Check(store.Flush(health,false,true,15,start.AddSeconds(15)),"normal exit boundary was not retained");
        Check(!store.Flush(health,false,true,16,start.AddSeconds(16)) && store.Writes==3,"duplicate shutdown rewrote summary");
        string valid=File.ReadAllText(path);
        Check(XElement.Parse(valid).Attribute("boundary").Value=="exit","snapshot not labelled exit");
        var next=new FrameHealthStore(path,"new-build",start.AddHours(1));
        string previous=Summary(next);
        Check(previous.Contains("previous session") && previous.Contains("0.2.5-test+fixture") && previous.Contains("capturedUtc"),"previous-session evidence mislabelled");
        health.Reset();
        Check(health.Frames==0 && !next.Flush(health,false,true,20,start.AddHours(1)),"new disabled session overwrote previous evidence");
        Check(File.ReadAllText(path)==valid,"disabled session changed retained snapshot");
        Check(Summary(new FrameHealthStore(path,"later-build",start.AddDays(31))).Contains("unavailable"),"stale snapshot accepted");
        foreach(string corrupt in new[] {"broken XML",new string('x',FrameHealthStore.MaxBytes+1),
            valid.Replace("schema=\"1\"","schema=\"99\""), valid.Replace("frames=\"2\"","frames=\"-1\""),
            valid.Replace("maximumMs=\"300\"","maximumMs=\"NaN\""),
            "<!DOCTYPE frameHealth [<!ENTITY x SYSTEM 'file:///must-not-be-read'>]><frameHealth>&x;</frameHealth>"})
        {
            File.WriteAllText(path,corrupt);
            Check(Summary(new FrameHealthStore(path,"new-build",start.AddHours(1))).Contains("unavailable"),"corrupt snapshot accepted: "+corrupt[..Math.Min(70,corrupt.Length)]);
        }
        File.WriteAllText(path,valid);
        var fresh=new FrameHealthStore(path,"next-build",start.AddHours(1));
        health.Observe(true,true,0); health.Observe(true,true,.1);
        Check(fresh.Flush(health,false,false,1,start.AddHours(1).AddSeconds(1)),"replacement session write failed");
        Check(Summary(fresh).Contains("0.2.5-test+fixture") && !Summary(fresh).Contains("build: next-build"),"current snapshot relabelled as previous");
        return count;
    }
}
