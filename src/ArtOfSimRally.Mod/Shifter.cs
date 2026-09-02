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
        /// Polls the device. Returns the gear-ratio index the shifter is asking
        /// for, or -1 when no gate is engaged.
        /// </summary>
        public static int Poll()
        {
            PressedButton = -1;
            if (!_open) return -1;

            var cfg = Main.Settings;
            if (cfg == null) return -1;

            int n;
            try { n = ReadAuxButtons(_buttons, MaxButtons); }
            catch (Exception ex)
            {
                ModLog.Error("Shifter read failed, closing: " + ex.Message);
                Close();
                return -1;
            }
            if (n <= 0) return -1;

            for (int i = 0; i < n; i++)
                if (_buttons[i] != 0) { PressedButton = i; break; }

            // Reverse first: on many H-patterns reverse shares a gate with a
            // forward gear plus a collar, and both buttons report together.
            if (Held(cfg.GearReverseButton, n)) return Reverse;

            for (int gear = 1; gear <= 6; gear++)
                if (Held(cfg.GearButton(gear), n)) return FirstGear + gear - 1;

            return -1;
        }

        private static bool Held(int button, int count)
            => button >= 0 && button < count && _buttons[button] != 0;

        /// <summary>
        /// Applies a polled gear to the car, if it differs from the last one sent.
        /// </summary>
        /// <remarks>
        /// An H-pattern shifter reports its gate continuously, so this would
        /// otherwise call Shift every physics step. Sending only on change also
        /// leaves the player's own sequential paddles working normally in between.
        /// </remarks>
        public static void Apply(Drivetrain drivetrain, int gearIndex)
        {
            if (drivetrain == null) return;

            var cfg = Main.Settings;
            if (cfg == null) return;

            // No gate engaged. On an H-pattern that means the lever is between
            // gears, which is genuinely neutral; a sequential shifter rests
            // between shifts, so it must not be forced to neutral.
            if (gearIndex < 0)
            {
                if (cfg.ShifterIsHPattern && _lastApplied != Neutral)
                {
                    _lastApplied = Neutral;
                    drivetrain.Shift(Neutral, true);
                }
                else if (!cfg.ShifterIsHPattern)
                {
                    _lastApplied = int.MinValue;   // allow the same gate to fire again
                }
                return;
            }

            if (gearIndex == _lastApplied) return;
            _lastApplied = gearIndex;

            int max = drivetrain.gearRatios != null ? drivetrain.gearRatios.Length - 1 : 6;
            if (gearIndex > max)
            {
                // A 6-speed shifter on a 5-speed car. Ignore rather than stall.
                return;
            }

            drivetrain.Shift(gearIndex, true);
        }

        /// <summary>Clears shift state, e.g. between stages.</summary>
        public static void Reset() => _lastApplied = int.MinValue;
    }
}
