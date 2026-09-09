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
            ObserveFrameHealth();
            TelemetryPump.StopIfDisabled();
            if (!Main.Enabled)
            {
                WheelInput.FlushLearnedRanges();
                Shifter.FlushSelection();
                CameraTuner.Flush();
                FrameHealthPersistence.Flush();
                return;
            }
            InputBackend.Tick();
            WheelInput.Update();

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
                FfbController.Reset();
                TelemetryPump.Park();

                // The moment the player stops driving is the right one to write
                // anything to disk. WheelInput learns each axis's full range as
                // the control is first used, which is the opening seconds of a
                // stage; saving it there could contribute to the reported KI-5 hitch.
            }
            TelemetryPump.Prepare();
            // Retry failed writes at most once per five seconds, only while idle.
            WheelInput.FlushLearnedRanges();
            Shifter.FlushSelection();
            CameraTuner.Flush();
            FrameHealthPersistence.Flush();
        }

        private void LateUpdate() => BonnetCamera.ReleaseIfInactive();

        // Keep Unity ECalls behind a non-inlined runtime boundary. The separate
        // developer probe must be able to attach its Update hook on the CLR for
        // offline verification without attempting to resolve Unity's native calls.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ObserveFrameHealth()
        {
            FrameHealth.Current.Observe(Main.Enabled && Main.Settings != null && Main.Settings.DiagnosticLogging,
                GameState.IsDriving && Application.isFocused, Time.realtimeSinceStartup);
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
        public static void Shutdown(bool unloading = false)
        {
            // Order matters: park telemetry while the socket is still open, and
            // zero the wheel before releasing the device, or the last non-zero
            // force can remain latched in the driver.
            FfbNative.SetForce(0);
            FfbController.Reset();
            FfbNative.Shutdown();
            TelemetryPump.Park();
            TelemetryPump.Shutdown();
            BonnetCamera.Release(unloading);
            Shifter.Close();
            WheelInput.Close();
            FfbNative.ReleaseInputs();
            // No file writes until force and telemetry outputs are released.
            try { WheelInput.FlushLearnedRanges(shutdown: true); } catch { }
            try { Shifter.FlushSelection(shutdown: true); } catch { }
            try { CameraTuner.Flush(shutdown: true); } catch { }
            try { FrameHealthPersistence.Flush(shutdown: true); } catch { }
        }
    }
}
