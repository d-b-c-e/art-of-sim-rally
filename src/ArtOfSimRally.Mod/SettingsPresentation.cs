using System;
using System.Reflection;
using UnityEngine;
using UnityModManagerNet;

namespace ArtOfSimRally.Mod
{
    // Scoped to our content. Never change UMM's Params.xml, global styles or its
    // scale/window preferences. UMM retains its own chrome and outer scrolling.
    internal sealed class SettingsPresentation : IDisposable
    {
        private static GUISkin _skin, _source;
        private static Font _font;
        private static float _lastScale;
        private readonly GUISkin _previous;
        private static readonly PropertyInfo HostParams = typeof(UnityModManager).GetProperty("Params", BindingFlags.Static | BindingFlags.NonPublic);
        private static UnityModManager.Param Preferences => (UnityModManager.Param)HostParams?.GetValue(null, null);
        private static readonly FieldInfo HostSize = typeof(UnityModManager.UI).GetField("mWindowSize", BindingFlags.Instance | BindingFlags.NonPublic);
        private static Vector2 WindowSize => HostSize != null && UnityModManager.UI.Instance != null
            ? (Vector2)HostSize.GetValue(UnityModManager.UI.Instance)
            : new Vector2(Preferences?.WindowWidth ?? 0, Preferences?.WindowHeight ?? 0);
        internal static float Scale { get; private set; } = 1;
        internal static float ContentWidth { get; private set; } = 890;
        internal static float BodyWidth => Math.Max(160, ContentWidth - 30 * Scale);
        internal static GUILayoutOption Width(float value) => GUILayout.Width(Math.Min(value * Scale, BodyWidth));
        internal static float PageHeight => SettingsDisplayPolicy.PageHeight(Screen.height, Scale, WindowSize.y);
        internal SettingsPresentation()
        {
            _previous = GUI.skin;
            Scale = SettingsDisplayPolicy.Scale(Screen.height, UnityModManager.UI.Scale(1f), Main.Settings.SettingsFollowHostScale);
            if (_skin == null || _source != _previous || _font != _previous.font || Math.Abs(_lastScale - Scale) > .01f)
            {
                if (_skin != null) UnityEngine.Object.Destroy(_skin);
                _skin = UnityEngine.Object.Instantiate(_previous);
                _source = _previous; _font = _previous.font; _lastScale = Scale;
                foreach (var style in new[] { _skin.label, _skin.button, _skin.toggle, _skin.textField, _skin.textArea, _skin.box })
                {
                    int inherited = style.fontSize > 0 ? style.fontSize : _font == null ? 13 : _font.fontSize;
                    style.fontSize = Math.Max(inherited, (int)Math.Round(14 * Scale));
                    style.wordWrap = true;
                    style.fixedHeight = 0;
                    style.padding = new RectOffset((int)(8 * Scale), (int)(8 * Scale), (int)(4 * Scale), (int)(4 * Scale));
                }
                _skin.verticalScrollbar.fixedWidth = 18 * Scale;
                _skin.verticalScrollbarThumb.fixedWidth = 18 * Scale;
                _skin.horizontalSlider.fixedHeight = 18 * Scale;
                _skin.horizontalSliderThumb.fixedHeight = 18 * Scale;
            }
            GUI.skin = _skin;
            ContentWidth = SettingsDisplayPolicy.Width(Screen.width, Scale, WindowSize.x);
            // A MinWidth expands the scroll content rather than the UMM host.
            // Bound both the outer group and the nested scroll body explicitly.
            GUILayout.BeginVertical(GUILayout.Width(ContentWidth));
        }
        public void Dispose() { GUILayout.EndVertical(); GUI.skin = _previous; }
    }
}
