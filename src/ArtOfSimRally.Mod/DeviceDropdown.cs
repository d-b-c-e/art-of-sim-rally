using System;
using UnityEngine;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// A dropdown for choosing a controller, shared by the wheel and shifter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Collapsed it shows one line - the current choice - so a panel with two
    /// device pickers does not become a wall of every controller listed twice.
    /// Expanding pushes the options inline rather than floating them, because an
    /// overlay popup inside UMM's scrolling settings view lands behind the
    /// controls below it.
    /// </para>
    /// <para>
    /// Selection is returned by index, and the caller stores the name too. Device
    /// order can change between launches when hardware is plugged in or removed,
    /// so the name is what identifies the choice later; the index only breaks ties
    /// between devices reporting the same name, which Fanatec rigs do.
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
        public static int Draw(string id, string label, string[] devices, int selected, string emptyText)
        {
            var wrap = new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUILayout.Label("<b>" + label + "</b>");

            if (devices == null || devices.Length == 0)
            {
                GUILayout.Label(emptyText, wrap);
                return -1;
            }

            bool open = _openId == id;
            string current = (selected >= 0 && selected < devices.Length)
                ? devices[selected]
                : "(none selected)";

            GUILayout.BeginHorizontal();
            if (GUILayout.Button((open ? "▼  " : "▶  ") + current, GUILayout.Width(320)))
                _openId = open ? null : id;
            GUILayout.Label(open ? "pick one" : "", wrap);
            GUILayout.EndHorizontal();

            if (!open) return -1;

            int chosen = -1;
            for (int i = 0; i < devices.Length; i++)
            {
                bool isCurrent = i == selected;
                string text = (isCurrent ? "•  " : "    ") + devices[i];
                if (GUILayout.Button(text, GUILayout.Width(320)))
                {
                    chosen = i;
                    _openId = null;
                }
            }
            return chosen;
        }

        /// <summary>Closes any open dropdown, e.g. when the panel is reopened.</summary>
        public static void CloseAll() => _openId = null;
    }
}
