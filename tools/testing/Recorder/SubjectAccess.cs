using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Globalization;
using System.Xml.Linq;

namespace ArtOfSimRally.Testing
{
    internal struct Tune
    {
        public float Reference { get; set; }
        public float Gain { get; set; }
        public float Smoothing { get; set; }
        public bool Invert { get; set; }
    }
    // Resolve private observation points once; sampling uses cached delegates,
    // with no reflection, boxing, or new collections in the physics callback.
    internal sealed class SubjectAccess
    {
        private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
        public readonly Assembly Mod;
        public readonly MethodInfo Drive, Reset, Update, Shutdown, Send;
        public readonly Func<bool> Enabled, Driving, ForceEnabled, Ready, Direct;
        public readonly Func<float> Smoothed;
        public readonly Func<Tune> ReadTune;
        private readonly Func<float>[] channels = new Func<float>[5];
        private readonly Func<object> settings;
        private readonly Assembly forceAssembly;

        public SubjectAccess(Assembly mod, Assembly force)
        {
            Mod = mod; forceAssembly = force;
            var main = mod.GetType("ArtOfSimRally.Mod.Main", true);
            var controller = mod.GetType("ArtOfSimRally.Mod.FfbController", true);
            var state = mod.GetType("ArtOfSimRally.Mod.GameState", true);
            var input = mod.GetType("ArtOfSimRally.Mod.WheelInput", true);
            var watchdog = mod.GetType("ArtOfSimRally.Mod.ModWatchdog", true);
            var native = force.GetType("Dbce.Wheel.Ffb.WheelFfbNative", true);
            Drive = Method(controller, "DriveWheel"); Reset = Method(controller, "Reset");
            Update = watchdog.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic) ?? throw new MissingMethodException("ModWatchdog.Update");
            Shutdown = Method(watchdog, "Shutdown"); Send = Method(native, "SetForce");
            Enabled = Getter<bool>(main, "Enabled"); Driving = Getter<bool>(state, "IsDriving");
            Ready = Getter<bool>(native, "Ready"); Direct = Getter<bool>(input, "Enabled");
            Smoothed = Expression.Lambda<Func<float>>(Expression.Field(null, controller.GetField("_smoothed", Flags))).Compile();
            var cfg = Expression.Property(null, main.GetProperty("Settings", Flags));
            settings = Expression.Lambda<Func<object>>(Expression.Convert(cfg, typeof(object))).Compile();
            ForceEnabled = Expression.Lambda<Func<bool>>(Expression.Field(cfg, "ForceFeedbackEnabled")).Compile();
            ReadTune = Expression.Lambda<Func<Tune>>(Expression.MemberInit(Expression.New(typeof(Tune)),
                Expression.Bind(typeof(Tune).GetProperty("Reference"), Expression.Field(cfg, "FyReference")),
                Expression.Bind(typeof(Tune).GetProperty("Gain"), Expression.Property(cfg, "GainFromStrength")),
                Expression.Bind(typeof(Tune).GetProperty("Smoothing"), Expression.Field(cfg, "Smoothing")),
                Expression.Bind(typeof(Tune).GetProperty("Invert"), Expression.Field(cfg, "Invert")))).Compile();
            var value = Method(input, "Value"); var channel = value.GetParameters()[0].ParameterType;
            for (int i = 0; i < channels.Length; i++)
                channels[i] = Expression.Lambda<Func<float>>(Expression.Call(value, Expression.Constant(Enum.ToObject(channel, i), channel))).Compile();
        }
        private static MethodInfo Method(Type type, string name) => type.GetMethod(name, Flags) ?? throw new MissingMethodException(type.FullName, name);
        private static Func<T> Getter<T>(Type type, string name) => Expression.Lambda<Func<T>>(Expression.Property(null, type.GetProperty(name, Flags))).Compile();
        public FrameSample Frame(int number, float time, float delta)
            => new FrameSample
            {
                Number = number,
                Time = time,
                Delta = delta,
                Driving = Enabled() && Driving() ? 1 : 0,
                Direct = Direct() ? 1 : 0,
                Steer = channels[0](),
                Throttle = channels[1](),
                Brake = channels[2](),
                Clutch = channels[3](),
                Handbrake = channels[4]()
            };
        public XElement Identity(string game, string unity)
        {
            var identity = (AssemblyInformationalVersionAttribute)Attribute.GetCustomAttribute(Mod, typeof(AssemblyInformationalVersionAttribute));
            var result = new XElement("capture", new XAttribute("origin", "game"),
                new XElement("build", identity?.InformationalVersion ?? Mod.GetName().Version.ToString()),
                new XElement("modSha256", ArtifactHash.FileHash(Mod.Location)),
                new XElement("recorderSha256", ArtifactHash.FileHash(typeof(SubjectAccess).Assembly.Location)),
                new XElement("forceLibrarySha256", ArtifactHash.FileHash(forceAssembly.Location)),
                new XElement("game", game), new XElement("unity", unity),
                new XElement("native", ArtOfSimRally.Mod.NativeDiagnostics.Describe("UnityForceFeedback.dll")));
            var value = settings(); var fields = new XElement("settings");
            foreach (var field in value.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance))
                fields.Add(new XElement("field", new XAttribute("name", field.Name), Convert.ToString(field.GetValue(value), CultureInfo.InvariantCulture)));
            result.Add(fields); return result;
        }
    }
}
