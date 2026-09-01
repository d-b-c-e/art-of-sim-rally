namespace ArtOfSimRally.Mod
{
    /// <summary>
    /// Whether the player is actually driving, as opposed to watching a cutscene,
    /// sitting in a menu, paused, or watching a replay.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>CarDynamics.FixedUpdate</c> keeps running through the end-of-stage
    /// animation while the game drives the car itself, so anything hooked to it
    /// must ask whether the player is in control. Two bugs come from not asking:
    /// </para>
    /// <list type="bullet">
    /// <item>
    /// Force feedback keeps streaming during the finishing cutscene. The car is
    /// under AI control and steering hard, so the wheel is dragged to full lock
    /// and held there - observed pulling fully left after crossing the line.
    /// </item>
    /// <item>
    /// Telemetry reports <c>IsRaceOn = 1</c> forever, so dashboards never park
    /// between stages and motion rigs keep reacting to a car nobody is driving.
    /// </item>
    /// </list>
    /// <para>
    /// The game's own <c>AxisCarController.GetInput</c> makes the same distinction,
    /// zeroing player input during <c>FINISHING_STAGE_ANIMATION</c>.
    /// </para>
    /// </remarks>
    internal static class GameState
    {
        /// <summary>
        /// True only while the player is driving under their own control.
        /// </summary>
        /// <remarks>
        /// Deliberately just <c>UNDERWAY</c>. <c>WAITING_TO_BEGIN</c> holds the car
        /// on the line, and <c>PAUSED</c>, <c>FINISHING_STAGE_ANIMATION</c>,
        /// <c>FINISHED</c> and <c>REPLAY</c> are all cases where forces should stop.
        /// </remarks>
        public static bool IsDriving
        {
            get
            {
                try
                {
                    var manager = GameEntryPoint.EventManager;
                    if (manager == null) return false;
                    return manager.status == EventStatusEnums.EventStatus.UNDERWAY;
                }
                catch
                {
                    // Before the event manager exists (main menu, loading) treat
                    // the player as not driving rather than throwing every step.
                    return false;
                }
            }
        }

        /// <summary>
        /// True while the player's own camera should be in control of the view.
        /// </summary>
        /// <remarks>
        /// Wider than <see cref="IsDriving"/> on purpose. Sitting on the start line
        /// and pausing are both moments where yanking the view back to a chase
        /// camera would be jarring. The end-of-stage cinematic, replays and the
        /// intro are the opposite: the game is directing, and a mod-mounted camera
        /// fights it - observed as the view ending up underground after crossing
        /// the finish line.
        /// </remarks>
        public static bool IsPlayerView
        {
            get
            {
                try
                {
                    var manager = GameEntryPoint.EventManager;
                    if (manager == null) return false;
                    switch (manager.status)
                    {
                        case EventStatusEnums.EventStatus.UNDERWAY:
                        case EventStatusEnums.EventStatus.WAITING_TO_BEGIN:
                        case EventStatusEnums.EventStatus.PAUSED:
                            return true;
                        default:
                            return false;
                    }
                }
                catch { return false; }
            }
        }
    }
}
