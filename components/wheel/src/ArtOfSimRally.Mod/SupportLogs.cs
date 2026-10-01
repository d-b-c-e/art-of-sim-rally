using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ArtOfSimRally.Mod
{
    internal static class SupportLogs
    {
        internal const int MaxBytes = 2 * 1024 * 1024;
        internal const int MaxLines = 4096;
        internal const int MaxLineChars = 2048;
        internal sealed class Snapshot
        {
            internal long FileBytes;
            internal int ReadBytes;
            internal bool Truncated;
            internal string[] Lines;
        }

        internal static Snapshot ReadTail(string path, int maxBytes = MaxBytes, int maxLines = MaxLines)
        {
            if (maxBytes<=0 || maxBytes>MaxBytes || maxLines<=0 || maxLines>MaxLines)
                throw new ArgumentOutOfRangeException();
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
            {
                long length=file.Length, offset=Math.Max(0,length-maxBytes);
                file.Position=offset;
                var bytes=new byte[(int)Math.Min(maxBytes,length)];
                int count=0, read;
                while(count<bytes.Length && (read=file.Read(bytes,count,bytes.Length-count))>0) count+=read;
                bool truncated=offset>0;
                var lines=new Queue<string>();
                using(var reader=new StreamReader(new MemoryStream(bytes,0,count),Encoding.UTF8,true))
                {
                    // Byte offset may start midway through a UTF-8 character or line.
                    if(offset>0) reader.ReadLine();
                    string line;
                    while((line=reader.ReadLine())!=null)
                    {
                        if(line.Length>MaxLineChars) { line=line.Substring(0,MaxLineChars)+" [line truncated]"; truncated=true; }
                        if(lines.Count==maxLines) { lines.Dequeue(); truncated=true; }
                        lines.Enqueue(line);
                    }
                }
                return new Snapshot {FileBytes=length,ReadBytes=count,Truncated=truncated,Lines=lines.ToArray()};
            }
        }

        internal static void AppendWindow(StringBuilder output, Snapshot snapshot)
            => output.AppendLine("Recent log window: " + snapshot.ReadBytes + " bytes read of " + snapshot.FileBytes +
                "; " + snapshot.Lines.Length + " retained lines; truncated=" + snapshot.Truncated + ".");

        internal static void AppendTail(StringBuilder output, string path, int lines)
        {
            output.AppendLine("source: " + path);
            try
            {
                var snapshot=ReadTail(path,maxLines:lines);
                AppendWindow(output,snapshot);
                foreach(var line in snapshot.Lines) output.AppendLine(line);
            }
            catch(Exception ex) { output.AppendLine("log unavailable: " + ex.Message); }
        }

        private static readonly Regex Force = new Regex(@"SetDeviceForcesXY\((-?\d+),\s*(-?\d+)\)");
        internal static void AppendNative(StringBuilder output, Snapshot snapshot)
        {
            AppendWindow(output,snapshot);
            int count=0,min=int.MaxValue,max=int.MinValue,negative=0,positive=0,zero=0;
            var recent=new Queue<string>();
            foreach(var line in snapshot.Lines)
            {
                var match=Force.Match(line);
                if(match.Success && int.TryParse(match.Groups[1].Value,NumberStyles.Integer,CultureInfo.InvariantCulture,out int force))
                {
                    count++; min=Math.Min(min,force); max=Math.Max(max,force);
                    if(force<0) negative++; else if(force>0) positive++; else zero++;
                }
                else { if(recent.Count==400) recent.Dequeue(); recent.Enqueue(line); }
            }
            output.AppendLine("force commands in retained window: " + count);
            if(count>0)
            {
                output.AppendLine("range: " + min + " .. " + max + " (nominal full scale +/-10000)");
                output.AppendLine("sign balance: " + negative + " negative, " + positive + " positive, " + zero + " zero");
            }
            output.AppendLine("Commands are not measured torque or proof of native acceptance. Missing commands in this window do not prove FFB never ran.");
            output.AppendLine("most recent non-force lines (up to 400):");
            foreach(var line in recent) output.AppendLine(line);
        }
    }
}
