using System;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// A dropdown for choosing a controller, shared by the wheel and shifter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Collapsed it shows one bounded-width choice beside its label. Expanding
    /// shows radio-like choices instead of a wall of full-width command buttons.
    /// The options push content inline rather than floating, because an
    /// overlay popup inside UMM's scrolling settings view lands behind the
    /// controls below it.
    /// </para>
    /// <para>
    /// Selection is returned as a row in the caller's cached snapshot. Callers
    /// persist the corresponding GUID and resolve it back to a display row;
    /// neither a row nor a native enumeration index is a persistent identity.
    /// </para>
    /// </remarks>
    internal static class DeviceDropdown
    {
        // Which dropdown is open, by caller-supplied id. Only one at a time, so
        // opening the shifter list closes the wheel list.
        private static string _openId;

        /// <summary>
        /// Draws the dropdown. Returns the newly chosen index, or -1 if unchanged.
        /// </summary>
        /// <param name="id">Unique id for this dropdown, used to track open state.</param>
        /// <param name="label">Heading shown above it.</param>
        /// <param name="devices">Device names, in enumeration order.</param>
        /// <param name="selected">Currently selected index, or -1 for none.</param>
        /// <param name="emptyText">Shown when there are no devices at all.</param>
        /// <param name="selectableCount">Allows a final saved-but-missing row to be shown but not chosen.</param>
        public static int Draw(string id, string label, string[] devices, int selected, string emptyText,
            int selectableCount = int.MaxValue)
        {
            var wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            if (devices == null || devices.Length == 0)
            {
                GUILayout.Label("<b>" + label + "</b>");
                GUILayout.Label(emptyText, wrap);
                return -1;
            }

            bool open = _openId == id;
            string current = (selected >= 0 && selected < devices.Length)
                ? devices[selected]
                : "(none selected)";

            bool stacked = SettingsPresentation.StackRows;
            if (!stacked) GUILayout.BeginHorizontal(SettingsPresentation.Width(540));
            GUILayout.Label("<b>" + label + "</b>", wrap,
                stacked ? GUILayout.ExpandWidth(false) : SettingsPresentation.Width(105));
            if (GUILayout.Button(current + (open ? "  ▲" : "  ▼"), SettingsPresentation.Width(420)))
                _openId = open ? null : id;
            if (!stacked) GUILayout.EndHorizontal();

            if (!open) return -1;

            int chosen = -1;
            if (!stacked)
            {
                GUILayout.BeginHorizontal(SettingsPresentation.Width(540));
                GUILayout.Space(105 * SettingsPresentation.Scale);
            }
            GUILayout.BeginVertical(SettingsPresentation.Width(420));
            for (int i = 0; i < devices.Length; i++)
            {
                bool isCurrent = i == selected;
                bool wasEnabled = GUI.enabled;
                GUI.enabled = wasEnabled && i < selectableCount;
                bool checkedNow = GUILayout.Toggle(isCurrent, devices[i], GUI.skin.toggle);
                GUI.enabled = wasEnabled;
                if (checkedNow && !isCurrent && i < selectableCount)
                {
                    chosen = i;
                    _openId = null;
                }
            }
            GUILayout.EndVertical();
            if (!stacked) GUILayout.EndHorizontal();
            return chosen;
        }

        /// <summary>Closes any open dropdown, e.g. when the panel is reopened.</summary>
        public static void CloseAll() => _openId = null;
    }
}
