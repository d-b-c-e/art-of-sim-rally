using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Persistent component that releases the wheel and parks telemetry whenever
    /// the game is not being driven, and cleans both up on exit.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Everything else in this mod hangs off Harmony patches, which only run while
    /// the thing they patched is running. That is a real gap for anything holding
    /// external state:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Force feedback is zeroed in the <c>CarDynamics.FixedUpdate</c> postfix when
    /// the player is not driving. But if the car stops ticking at all - destroyed
    /// on stage end, or the physics loop stopping during a cutscene - that postfix
    /// never runs again, so the last force applied just stays applied and the wheel
    /// keeps pulling.
    /// </item>
    /// <item>
    /// Telemetry consumers hold the last packet they received. Simply stopping
    /// leaves a dashboard frozen at whatever speed and RPM the game quit at.
    /// </item>
    /// </list>
    /// <para>
    /// A component that ticks independently of the game's own objects closes both.
    /// It survives scene loads, so it is still there when everything else is gone.
    /// </para>
    /// </remarks>
    internal sealed class ModWatchdog : MonoBehaviour
    {
        private static ModWatchdog _instance;
        private bool _wheelReleased;

        /// <summary>Creates the watchdog once, outside the scene hierarchy.</summary>
        public static void Install()
        {
            if (_instance != null) return;

            var host = new GameObject("ArtOfSimRally.Watchdog");
            DontDestroyOnLoad(host);
            host.hideFlags = HideFlags.HideAndDontSave;
            _instance = host.AddComponent<ModWatchdog>();

            ModLog.Info("Watchdog installed.");
        }

        private void Update()
        {
            if (!Main.Enabled) return;

            // Independent of whether any game object is still ticking. The
            // FixedUpdate postfix normally gets here first; this exists for when
            // it cannot.
            if (GameState.IsDriving)
            {
                _wheelReleased = false;
                return;
            }

            if (!_wheelReleased)
            {
                _wheelReleased = true;
                FfbNative.SetForce(0);
                TelemetryPump.Park();
            }
        }

        private void OnApplicationQuit()
        {
            Shutdown();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        /// <summary>Zeroes the wheel, parks telemetry, and releases both.</summary>
        public static void Shutdown()
        {
            // Order matters: park telemetry while the socket is still open, and
            // zero the wheel before releasing the device, or the last non-zero
            // force can remain latched in the driver.
            TelemetryPump.Park();
            TelemetryPump.Shutdown();

            FfbNative.SetForce(0);
            FfbNative.Shutdown();

            Shifter.Close();
        }
    }
}
