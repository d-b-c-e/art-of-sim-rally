using System;
using System.Runtime.InteropServices;
using System.Text;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Reads a shifter directly through DirectInput and selects gears itself.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The game only has ShiftUp and ShiftDown actions, so even a bound H-pattern
    /// shifter would act as paddles - selecting 3rd would mean "one gear up from
    /// wherever you are". And Rewired's Raw Input backend does not enumerate most
    /// shifters at all, so usually they cannot even be bound.
    /// </para>
    /// <para>
    /// Both problems disappear by not involving Rewired. The native plugin already
    /// enumerates every DirectInput controller, so the shifter is read there,
    /// non-exclusively, and gears are applied with <c>Drivetrain.Shift</c> - the
    /// same call the game's own sequential path makes. No backend change, no
    /// rebinding, and nothing that can affect keyboard input.
    /// </para>
    /// <para>
    /// Gear numbers are indices into the game's <c>gearRatios</c> array:
    /// <c>{ -2.66 reverse, 0 neutral, 2.66 1st, 1.91, 1.39, 1.0, 0.71 }</c> - so
    /// reverse is 0, neutral 1, and first gear 2. Five forward gears exist for
    /// every car, which is why a 6-speed shifter has one unused gate.
    /// </para>
    /// </remarks>
    internal static class Shifter
    {
        private const string Dll = "UnityForceFeedback";
        private const int MaxButtons = 128;

        [DllImport(Dll)] private static extern int  EnumerateAllDevices();
        [DllImport(Dll, CharSet = CharSet.Ansi)]
        private static extern int  GetAnyDeviceName(int index, StringBuilder buffer, int size);
        [DllImport(Dll)] private static extern int  GetAnyDeviceInfo(int index, out int axes, out int buttons, out int ffb);
        [DllImport(Dll)] private static extern int  OpenAuxDevice(int index);
        [DllImport(Dll)] private static extern int  ReadAuxButtons(byte[] buffer, int length);
        [DllImport(Dll)] private static extern void CloseAuxDevice();

        /// <summary>Gear-ratio index for neutral.</summary>
        public const int Neutral = 1;

        /// <summary>Gear-ratio index for reverse.</summary>
        public const int Reverse = 0;

        /// <summary>Gear-ratio index of first gear; add one per gear above it.</summary>
        public const int FirstGear = 2;

        private static readonly byte[] _buttons = new byte[MaxButtons];
        private static bool _open;
        private static int  _lastApplied = int.MinValue;
        private static bool _lastUp;
        private static bool _lastDown;

        /// <summary>True while a shifter device is open and being read.</summary>
        public static bool IsOpen => _open;

        /// <summary>Button index currently held, or -1. Used by the binding UI.</summary>
        public static int PressedButton { get; private set; } = -1;

        /// <summary>Every attached controller, force feedback or not.</summary>
        public static string[] ListDevices()
        {
            try
            {
                int count = EnumerateAllDevices();
                if (count <= 0) return new string[0];

                var names = new string[count];
                var buf = new StringBuilder(260);
                for (int i = 0; i < count; i++)
                {
                    buf.Length = 0;
                    names[i] = GetAnyDeviceName(i, buf, buf.Capacity) != 0
                        ? buf.ToString() : "(device " + i + ")";
                }
                return names;
            }
            catch (Exception ex)
            {
                ModLog.Warning("Could not list controllers: " + ex.Message);
                return new string[0];
            }
        }

        /// <summary>Display labels for <see cref="ListDevices"/> - name plus capabilities. Not for storing.</summary>
        public static string[] ListDeviceLabels(string[] names)
        {
            var labels = new string[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                labels[i] = names[i];
                try
                {
                    if (GetAnyDeviceInfo(i, out int axes, out int buttons, out int ffb) != 0 && (axes > 0 || buttons > 0))
                        labels[i] = names[i] + "  (" + axes + " axes, " + buttons + " buttons" + (ffb != 0 ? ", force feedback" : "") + ")";
                }
                catch { }
            }
            return labels;
        }

        /// <summary>Opens the chosen device for reading. Safe to call repeatedly.</summary>
        public static bool Open(int index)
        {
            if (index < 0) { Close(); return false; }
            try
            {
                _open = OpenAuxDevice(index) != 0;
                if (!_open) ModLog.Warning("Could not open shifter device " + index);
                return _open;
            }
            catch (Exception ex)
            {
                ModLog.Error("Shifter open failed: " + ex.Message);
                _open = false;
                return false;
            }
        }

        /// <summary>Stops reading and releases the device.</summary>
        public static void Close()
        {
            if (!_open) return;
            try { CloseAuxDevice(); } catch { }
            _open = false;
            PressedButton = -1;
        }

        /// <summary>
        /// Reads the shifter and applies any gear change to the car.
        /// </summary>
        /// <remarks>
        /// The two modes are genuinely different mechanisms, not one with a flag.
        /// An H-pattern reports an absolute gate and holds it, so the gear follows
        /// the lever and letting go means neutral. A sequential lever is two
        /// momentary switches, so it must be edge-triggered and moves relative to
        /// whatever gear the car is already in.
        /// </remarks>
        public static void Update(Drivetrain drivetrain)
        {
            PressedButton = -1;
            if (!_open || drivetrain == null) return;

            var cfg = Main.Settings;
            if (cfg == null) return;

            int n;
            try { n = ReadAuxButtons(_buttons, MaxButtons); }
            catch (Exception ex)
            {
                ModLog.Error("Shifter read failed, closing: " + ex.Message);
                Close();
                return;
            }
            if (n <= 0) return;

            for (int i = 0; i < n; i++)
                if (_buttons[i] != 0) { PressedButton = i; break; }

            if (cfg.ShifterIsHPattern) UpdateHPattern(cfg, drivetrain, n);
            else UpdateSequential(cfg, drivetrain, n);
        }

        private static void UpdateHPattern(Settings cfg, Drivetrain drivetrain, int n)
        {
            // With nothing bound, every frame looks like "no gate held", which
            // below means neutral - so an enabled but unconfigured H-pattern would
            // hold the car in neutral forever and look exactly like the shifter
            // having broken the game. Do nothing until at least one gate is bound.
            if (!AnyGearBound(cfg)) return;

            int gear = -1;

            // Reverse first: on many shifters reverse shares a gate with a forward
            // gear plus a collar, and both report together.
            if (Held(cfg.GearReverseButton, n)) gear = Reverse;
            else
                for (int g = 1; g <= 6; g++)
                    if (Held(cfg.GearButton(g), n)) { gear = FirstGear + g - 1; break; }

            // No gate held means the lever is between gears, which really is
            // neutral on an H-pattern.
            if (gear < 0) gear = Neutral;

            if (gear == _lastApplied) return;
            _lastApplied = gear;

            if (!GearExists(drivetrain, gear)) return;
            drivetrain.Shift(gear, true);
        }

        private static void UpdateSequential(Settings cfg, Drivetrain drivetrain, int n)
        {
            bool up = Held(cfg.ShiftUpButton, n);
            bool down = Held(cfg.ShiftDownButton, n);

            // Edge-triggered: holding the lever must not shift repeatedly.
            bool upEdge = up && !_lastUp;
            bool downEdge = down && !_lastDown;
            _lastUp = up;
            _lastDown = down;

            if (!upEdge && !downEdge) return;

            int step = upEdge ? 1 : -1;
            int target = drivetrain.gear + step;

            // Step over neutral rather than stopping on it, so reverse to first is
            // one press instead of two.
            if (cfg.SkipNeutral && target == Neutral) target += step;

            if (!GearExists(drivetrain, target)) return;
            drivetrain.Shift(target, true);
        }

        private static bool AnyGearBound(Settings cfg)
        {
            if (cfg.GearReverseButton >= 0) return true;
            for (int g = 1; g <= 6; g++) if (cfg.GearButton(g) >= 0) return true;
            return false;
        }

        private static bool GearExists(Drivetrain drivetrain, int gear)
        {
            if (gear < 0) return false;
            int max = drivetrain.gearRatios != null ? drivetrain.gearRatios.Length - 1 : 6;
            return gear <= max;
        }

        private static bool Held(int button, int count)
            => button >= 0 && button < count && _buttons[button] != 0;

        /// <summary>
        /// Reads buttons without changing gear, for the binding UI.
        /// </summary>
        public static void PollForBinding()
        {
            PressedButton = -1;
            if (!_open) return;
            try
            {
                int n = ReadAuxButtons(_buttons, MaxButtons);
                for (int i = 0; i < n; i++)
                    if (_buttons[i] != 0) { PressedButton = i; return; }
            }
            catch { }
        }

        /// <summary>Clears shift state, e.g. between stages.</summary>
        public static void Reset()
        {
            _lastApplied = int.MinValue;
            _lastUp = false;
            _lastDown = false;
        }
    }
}
