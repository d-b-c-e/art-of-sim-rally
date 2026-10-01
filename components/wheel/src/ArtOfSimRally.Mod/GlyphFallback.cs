using HarmonyLib;
using Rewired;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Replaces the blank glyph boxes on the controls screen with the element's
    /// name, for controllers the game has no artwork for.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ControlsRemapper.RefreshDisplayedGlyph</c> looks glyphs up by the
    /// controller's <c>hardwareTypeGuid</c>. Any wheel Rewired does not recognise
    /// has an all-zero GUID, so it matches nothing, falls back to a generic set,
    /// and where that has no entry either assigns a null sprite - drawn as an
    /// empty black box. A binding really did register; it just cannot be read.
    /// </para>
    /// <para>
    /// No new artwork is needed. The same method already drives a text badge for
    /// keyboard bindings:
    /// </para>
    /// <code>
    /// uiData.keyboardGlyph.SetActive(true);
    /// uiData.keyboardText.text = aem.keyboardKeyCode.ToString();
    /// </code>
    /// <para>
    /// So when a joystick glyph comes back empty, this switches that badge on and
    /// writes the element's own name into it. "B12" beats a black square, and it
    /// reuses UI the game already lays out and styles correctly.
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(ControlsRemapper), "RefreshDisplayedGlyph")]
    internal static class GlyphFallback
    {
        [HarmonyPostfix]
        private static void ShowNameWhenNoGlyph(ControlsRemapper.UIActionSet uiData, ActionElementMap aem)
        {
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.GlyphTextFallback) return;
            if (uiData == null || aem == null) return;

            try
            {
                // Only step in where the game drew nothing. A controller with real
                // artwork keeps it.
                if (uiData.glyphImage == null || uiData.glyphImage.sprite != null) return;
                if (uiData.keyboardGlyph == null || uiData.keyboardText == null) return;

                uiData.glyphImage.enabled = false;
                uiData.keyboardGlyph.SetActive(true);
                uiData.keyboardText.text = Shorten(aem.elementIdentifierName);
            }
            catch
            {
                // Cosmetic only - never break the controls screen over a label.
            }
        }

        /// <summary>
        /// Compresses Rewired's element names to fit a badge sized for "F12".
        /// </summary>
        /// <remarks>
        /// "Button 12" becomes "B12" and "X Axis" becomes "X"; anything else is
        /// truncated. A wheel can report over a hundred buttons, so the long form
        /// would overflow the badge on most of them.
        /// </remarks>
        private static string Shorten(string name)
        {
            if (string.IsNullOrEmpty(name)) return "?";

            const string button = "Button ";
            if (name.StartsWith(button)) return "B" + name.Substring(button.Length).Trim();

            // "X Axis", "Y Rotation", "Slider 1" - the leading token identifies it.
            int space = name.IndexOf(' ');
            string head = space > 0 ? name.Substring(0, space) : name;
            return head.Length <= 4 ? head : head.Substring(0, 4);
        }
    }
}
