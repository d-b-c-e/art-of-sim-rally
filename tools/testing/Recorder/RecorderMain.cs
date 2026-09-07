using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityModManagerNet;

namespace ArtOfSimRally.Testing
{
    public static class RecorderMain
    {
        private static readonly CaptureSession Session = new CaptureSession();
        private static SubjectAccess subject;
        private static Harmony patches;
        private static ControlServer server;
        private static bool observing, sent;
        private static int device;
        private const string PatchId = "ArtOfSimRally.DevRecorder";
        private struct Step { public bool Valid; public ForceSample Sample; }

        public static bool Load(UnityModManager.ModEntry entry)
        {
            try
            {
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                Attach(assemblies.Single(a => a.GetName().Name == "ArtOfSimRally.Mod"),
                    assemblies.Single(a => a.GetName().Name == "Dbce.Wheel.Ffb"));
                server = new ControlServer("ArtOfSimRally.DevRecorder." + Process.GetCurrentProcess().Id);
                entry.OnUpdate = (mod, delta) => server?.Pump(Command);
                entry.OnUnload = Unload;
                entry.Logger.Log("Developer capture probe ready. Use tools/testing/Record-Drive.ps1; no recording starts automatically.");
                return true;
            }
            catch (Exception ex)
            {
                patches?.UnpatchAll(PatchId); server?.Dispose(); server = null;
                entry.Logger.Error("Developer probe could not attach: " + ex.Message); return false;
            }
        }
        private static void Attach(System.Reflection.Assembly mod, System.Reflection.Assembly force)
        {
            subject = new SubjectAccess(mod, force);
            patches = new Harmony(PatchId);
            patches.Patch(subject.Drive, prefix: Hook(nameof(BeforeForce)), postfix: Hook(nameof(AfterForce)));
            patches.Patch(subject.Send, prefix: Hook(nameof(BeforeSend)), postfix: Hook(nameof(AfterSend)));
            patches.Patch(subject.Reset, postfix: Hook(nameof(Reset)));
            patches.Patch(subject.Update, postfix: Hook(nameof(Frame)));
            patches.Patch(subject.Shutdown, postfix: Hook(nameof(Shutdown)));
        }
        private static HarmonyMethod Hook(string name) => new HarmonyMethod(typeof(RecorderMain), name);
        private static string Command(string command)
        {
            if (command == "STATUS") return "OK " + Session.Describe;
            if (command == "START")
            {
                if (subject.Driving() || Session.Pending) return "ERROR pause and finish any pending capture before START";
                string root = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ArtOfSimRally", "dev-captures");
                bool ok = Session.Start(false, subject.Identity(Application.version, Application.unityVersion), root);
                return (ok ? "OK " : "ERROR ") + Session.Status;
            }
            bool stopped = Session.Stop(subject.Driving());
            return (stopped ? "OK " : "ERROR ") + Session.Status;
        }
        private static bool Unload(UnityModManager.ModEntry entry)
        {
            if (Session.Pending && !Session.Stop(subject.Driving())) { entry.Logger.Warning(Session.Status); return false; }
            patches?.UnpatchAll(PatchId); server?.Dispose(); server = null; return true;
        }
        private static void BeforeForce(CarDynamics __0, out Step __state)
        {
            __state = default; observing = false; sent = false;
            try
            {
                if (!Session.Active || !subject.Enabled() || !subject.Driving() || !subject.ForceEnabled() || !subject.Ready()) return;
                var front = __0.axles?.frontAxle;
                if (front?.leftWheel == null || front.rightWheel == null) return;
                var left = front.leftWheel; var right = front.rightWheel; var tune = subject.ReadTune();
                var sample = new ForceSample
                {
                    Time = Time.realtimeSinceStartup,
                    Fy = left.Fy + right.Fy,
                    Slip = .5f * (Math.Abs(left.slipAngle) + Math.Abs(right.slipAngle)),
                    Ideal = left.idealSlipAngle,
                    Speed = __0.velo * 3.6f,
                    Reference = tune.Reference,
                    Gain = tune.Gain,
                    Smoothing = tune.Smoothing,
                    Invert = tune.Invert ? 1 : 0,
                    Previous = subject.Smoothed()
                };
                __state = new Step { Valid = true, Sample = sample }; observing = true;
            }
            catch (Exception ex) { Session.AbortSampling(ex.Message); }
        }
        private static void AfterForce(Step __state)
        {
            observing = false;
            if (!__state.Valid) return;
            try
            {
                if (!sent) { Session.Incomplete("Native force observation hook did not execute"); return; }
                var sample = __state.Sample; sample.Output = subject.Smoothed(); sample.Device = device;
                Session.Force(sample);
            }
            catch (Exception ex) { Session.AbortSampling(ex.Message); }
        }
        private static void BeforeSend(int __0) { if (observing) { device = __0; sent = true; } }
        private static void AfterSend(bool __result) { if (observing) Session.Delivery(__result); }
        private static void Reset() => Session.Reset();
        private static void Frame()
        {
            try { if (Session.Active) Session.Frame(subject.Frame(Time.frameCount, Time.realtimeSinceStartup, Time.unscaledDeltaTime)); }
            catch (Exception ex) { Session.AbortSampling(ex.Message); }
        }
        // Runs after the shipping watchdog has released all force/input resources.
        private static void Shutdown() { if (Session.Pending) Session.Stop(false); }
    }
}
