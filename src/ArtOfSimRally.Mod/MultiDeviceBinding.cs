using HarmonyLib;
using Rewired;

namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Lets the controls screen bind whichever device you actually touch, instead
    /// of only the first joystick.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>ControlsRemapper</c> resolves its binding target as:
    /// </para>
    /// <code>
    /// private Joystick joystick {
    ///     get {
    ///         if (player.controllers.joystickCount &lt;= 0) return null;
    ///         return player.controllers.Joysticks[0];   // always index 0
    ///     }
    /// }
    /// </code>
    /// <para>
    /// Everything the remap screen does - polling for input, assigning elements,
    /// listing existing bindings - goes through that one property, so only the
    /// first joystick can ever be configured. Plug in a wheel and a separate
    /// H-pattern shifter and one of them is simply unbindable. Worse, with two
    /// devices sharing a name (a Fanatec rig reports two "FANATEC Wheel" entries)
    /// it is not obvious which one index 0 even is, and binding can silently
    /// target the wrong device - which looks like the wheel not working at all.
    /// </para>
    /// <para>
    /// Nothing about the game requires this. Input at runtime is read with
    /// <c>ReInput.players.GetPlayer(0).GetAxisRaw(...)</c>, a player-level call
    /// that polls every controller assigned to the player, and Rewired stores a
    /// separate map per device. Multi-device rigs work fine once bound; only the
    /// binding UI is the bottleneck.
    /// </para>
    /// <para>
    /// The fix uses an API the game already calls a hundred lines further down -
    /// <c>GetLastActiveController(ControllerType.Joystick)</c>, which it uses to
    /// pick which button glyph to draw. Pointing the binding target at the same
    /// thing means "the device you just moved is the device you are binding".
    /// </para>
    /// </remarks>
    [HarmonyPatch(typeof(ControlsRemapper), "get_joystick")]
    internal static class MultiDeviceBinding
    {
        // Latched rather than read fresh each time. GetLastActiveController only
        // reports a device while it is being moved, and the remapper reads this
        // property repeatedly during and after a poll - including once the stick
        // has recentred. Without latching, the target would flip back to index 0
        // in the instant between moving an axis and the assignment being written.
        private static Joystick _latched;

        [HarmonyPostfix]
        private static void PreferTheDeviceInUse(ref Joystick __result)
        {
            var cfg = Main.Settings;
            if (!Main.Enabled || cfg == null || !cfg.BindAnyDevice) return;

            try
            {
                var player = PadManager.GetPlayer();
                if (player == null || player.controllers.joystickCount <= 1) return;

                var active = player.controllers.GetLastActiveController(ControllerType.Joystick)
                             as Joystick;
                if (active != null && !ReferenceEquals(active, _latched))
                {
                    _latched = active;
                    ModLog.Info("Binding target is now '" + active.name + "'");
                }

                // Only override with a device still assigned to the player, so a
                // unplugged wheel cannot leave the screen bound to a ghost.
                if (_latched != null && player.controllers.ContainsController(_latched))
                    __result = _latched;
            }
            catch
            {
                // Leave the stock behaviour in place rather than break the only
                // screen a player can rebind from.
            }
        }

        /// <summary>Forgets the latched device, e.g. when the screen closes.</summary>
        public static void Reset() => _latched = null;
    }
}
