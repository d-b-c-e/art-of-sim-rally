using System;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    // One table over the existing XML fields: no second binding format to migrate.
    internal static class CameraKeys
    {
        internal sealed class Binding
        {
            public readonly string Label;
            public readonly Func<Settings, KeyCode> Get;
            public readonly Action<Settings, KeyCode> Set;
            public Binding(string label, Func<Settings, KeyCode> get, Action<Settings, KeyCode> set)
            { Label = label; Get = get; Set = set; }
        }

        public static readonly Binding[] Bindings = {
            new Binding("Up", s => s.KeyUp, (s,k) => s.KeyUp=k),
            new Binding("Down", s => s.KeyDown, (s,k) => s.KeyDown=k),
            new Binding("Forward", s => s.KeyForward, (s,k) => s.KeyForward=k),
            new Binding("Back", s => s.KeyBack, (s,k) => s.KeyBack=k),
            new Binding("Left", s => s.KeyLeft, (s,k) => s.KeyLeft=k),
            new Binding("Right", s => s.KeyRight, (s,k) => s.KeyRight=k),
            new Binding("Tilt down", s => s.KeyPitchDown, (s,k) => s.KeyPitchDown=k),
            new Binding("Tilt up", s => s.KeyPitchUp, (s,k) => s.KeyPitchUp=k),
            new Binding("Widen field of view", s => s.KeyFovUp, (s,k) => s.KeyFovUp=k),
            new Binding("Narrow field of view", s => s.KeyFovDown, (s,k) => s.KeyFovDown=k),
            new Binding("Reset active mount", s => s.KeyReset, (s,k) => s.KeyReset=k)
        };

        public static int Listening { get; private set; } = -1;
        public static string Status { get; private set; } = "";
        public static bool Available(Settings cfg) => cfg != null && (cfg.BonnetCameraEnabled || cfg.BumperCameraEnabled);
        public static string Name(KeyCode key) => key == KeyCode.None ? "Unbound" : key.ToString().Replace("Keypad", "Numpad ");

        public static void Begin(int index)
        {
            if (index < 0 || index >= Bindings.Length) throw new ArgumentOutOfRangeException(nameof(index));
            Listening = index;
            Status = "Press one keyboard key for " + Bindings[index].Label + ". Escape cancels.";
            CameraTuner.SuppressUntilRelease();
        }

        public static void Cancel() { Listening = -1; Status = ""; }

        public static bool ModifierHeld() =>
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt) || Input.GetKey(KeyCode.AltGr) ||
            Input.GetKey(KeyCode.LeftCommand) || Input.GetKey(KeyCode.RightCommand) ||
            Input.GetKey(KeyCode.LeftWindows) || Input.GetKey(KeyCode.RightWindows);

        private static bool IsKeyboardKey(KeyCode key)
        {
            if (key <= KeyCode.None || key >= KeyCode.Mouse0 || !Enum.IsDefined(typeof(KeyCode), key)) return false;
            switch (key)
            {
                case KeyCode.Escape:
                case KeyCode.LeftShift: case KeyCode.RightShift:
                case KeyCode.LeftControl: case KeyCode.RightControl:
                case KeyCode.LeftAlt: case KeyCode.RightAlt: case KeyCode.AltGr:
                case KeyCode.LeftCommand: case KeyCode.RightCommand:
                case KeyCode.LeftWindows: case KeyCode.RightWindows:
                    return false;
                default: return true;
            }
        }

        // Returns whether the event belongs to a pending capture, even if invalid.
        // Chords aren't represented by the legacy KeyCode fields, so reject them.
        public static bool HandleKey(Settings cfg, KeyCode key, bool modified)
        {
            if (Listening < 0) return false;
            if (key == KeyCode.Escape) { Cancel(); return true; }
            if (cfg == null || modified || !IsKeyboardKey(key))
            {
                Status = "Use one keyboard key without Shift, Ctrl, Alt or Windows/Command. Escape cancels.";
                return true;
            }
            for (int i = 0; i < Bindings.Length; i++)
                if (i != Listening && Bindings[i].Get(cfg) == key)
                {
                    Status = Name(key) + " already controls " + Bindings[i].Label + ". Clear or rebind that action first.";
                    return true;
                }
            var binding = Bindings[Listening];
            binding.Set(cfg, key);
            Listening = -1;
            Status = binding.Label + " = " + Name(key) + ". Saves when idle.";
            CameraTuner.MarkDirty();
            return true;
        }

        public static void Clear(Settings cfg, int index)
        {
            Bindings[index].Set(cfg, KeyCode.None);
            Cancel(); CameraTuner.MarkDirty();
        }

        public static void Reset(Settings cfg)
        {
            var defaults = new Settings();
            foreach (var binding in Bindings) binding.Set(cfg, binding.Get(defaults));
            Cancel(); CameraTuner.MarkDirty();
            Status = "Numpad defaults restored. Saves when idle.";
        }
    }
}
