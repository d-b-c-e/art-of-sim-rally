using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using SimHub.Plugins.DataPlugins.ShakeItV3.EffectsContainers;
using SimHub.Plugins.DataPlugins.ShakeItV3.Outputs.Audio;

internal static class Program
{
    private static int _checks;
    private static void Check(bool ok,string name) { _checks++; if(!ok) throw new Exception(name); }
    [STAThread]
    private static void Main(string[] args)
    {
        // Resolve locally; never initialize PluginManager, audio, or an output device.
        AppDomain.CurrentDomain.AssemblyResolve+=(s,e)=> {
            string path=Path.Combine(args[0],new AssemblyName(e.Name).Name+".dll");
            return File.Exists(path) ? Assembly.LoadFrom(path) : null;
        };
        string effectPath=Path.GetFullPath(args[1]);
        string scratch=Path.GetFullPath(args[2]); Directory.CreateDirectory(scratch); Directory.SetCurrentDirectory(scratch);
        Inspect(effectPath);
        Console.WriteLine("{\"status\":\"passed\",\"assertions\":"+_checks+",\"audioOutput\":false}");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void Inspect(string path)
    {
        // The standalone host has no SimHub plugin-discovery folder. Register only
        // the two built-in types needed by this profile; do not start any plugins.
        foreach(var pair in new[] { new[]{"EffectsContainerJsonConverter",typeof(CustomEffectContainer).FullName},
            new[]{"OutputsJsonConverter",typeof(ToneOutput).FullName} })
        {
            var assembly=typeof(CustomEffectContainer).Assembly;
            assembly.GetType("SimHub.Plugins.DataPlugins.ShakeItV3.Utilities."+pair[0]).GetMethod("AddType",BindingFlags.NonPublic|BindingFlags.Static)
                .Invoke(null,new object[]{assembly.GetType(pair[1])});
        }
        var effect=JsonConvert.DeserializeObject<EffectsContainerBase>(File.ReadAllText(path)) as CustomEffectContainer;
        Check(effect!=null,"SimHub polymorphic profile import");
        Check(effect.IsEnabled && effect.Gain==100 && !effect.AlwaysExecute && !effect.IsAllowedOutOfGame,"gain and out-of-game gate");
        Check(effect.Output is ToneOutput && ((ToneOutput)effect.Output).Frequency==30,"30Hz tone import");
        Check(effect.FrontLeftFormula.Expression=="isnull([ArtOfSimRallyHaptics.LandingPulse], 0)","formula survives import");
        Check(effect.RearRightFormula.Expression==effect.FrontLeftFormula.Expression,"corner consistency");
        Check(effect.Description=="Art of Sim Rally - landing thud","effect label import");
        Check(effect.AggregationMode=="Mono" && effect.Effects.Count==1,"single aggregate, not four summed tones");
        var copy=JsonConvert.DeserializeObject<EffectsContainerBase>(JsonConvert.SerializeObject(effect)) as CustomEffectContainer;
        Check(copy!=null && ((ToneOutput)copy.Output).Frequency==30 && copy.Gain==100,"SimHub save/reload");
    }
}
