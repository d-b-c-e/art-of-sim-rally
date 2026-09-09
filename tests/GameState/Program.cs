using System.Text.Json;
using State = ArtOfSimRally.Mod.GameState;

// Model the verified lazy getter contract, not the game's implementation.
// A failed construction has already started asynchronous work before it throws.
public static class GameEntryPoint
{
    private static EventManager eventManager;
    public static int GetterCalls, ConstructorAttempts, GhostRequests;
    public static EventManager EventManager
    {
        get
        {
            GetterCalls++;
            if (eventManager != null) return eventManager;
            ConstructorAttempts++; GhostRequests++;
            throw new InvalidOperationException("No stage exists; asynchronous work already queued");
        }
    }
    public static void SetForTest(EventManager manager) => eventManager = manager;
}
public static class EventStatusEnums
{
    public enum EventStatus { INTRO_CINEMATIC, IN_PRE_STAGE_SCREEN, WAITING_TO_BEGIN,
        UNDERWAY, PAUSED, FINISHING_STAGE_ANIMATION, FINISHED, REPLAY }
}
public sealed class EventManager
{
    public EventStatusEnums.EventStatus status;
    public bool Restarting, ThrowRestart;
    public bool IsRestartingStage() => ThrowRestart ? throw new InvalidOperationException("transition") : Restarting;
}

static class Program
{
    static int assertions;
    static void Check(bool ok, string message) { assertions++; if (!ok) throw new Exception(message); }
    static void PollAbsent()
    {
        for (int i=0; i<1000; i++)
        {
            _ = State.IsDriving; _ = State.IsEngineLive;
            _ = State.IsPlayerView; _ = State.IsRestarting;
        }
        Check(GameEntryPoint.GetterCalls==0 && GameEntryPoint.ConstructorAttempts==0 && GameEntryPoint.GhostRequests==0,
            $"State polling invoked lazy getter {GameEntryPoint.GetterCalls} times; construction/requests={GameEntryPoint.GhostRequests}");
        Check(!State.IsDriving && !State.IsEngineLive && !State.IsPlayerView && !State.IsRestarting,
            "Absent manager did not park all state");
    }
    static void Main()
    {
        PollAbsent();
        var manager = new EventManager(); GameEntryPoint.SetForTest(manager);
        foreach (var status in Enum.GetValues<EventStatusEnums.EventStatus>())
        {
            manager.status=status;
            bool driving=status==EventStatusEnums.EventStatus.UNDERWAY;
            bool engine=driving || status==EventStatusEnums.EventStatus.WAITING_TO_BEGIN;
            Check(State.IsDriving==driving, "Incorrect driving state: "+status);
            Check(State.IsEngineLive==engine, "Incorrect engine state: "+status);
            Check(State.IsPlayerView==(engine || status==EventStatusEnums.EventStatus.PAUSED), "Incorrect camera ownership: "+status);
            Check(!State.IsRestarting, "Unexpected restart flag: "+status);
        }
        manager.Restarting=true; Check(State.IsRestarting, "Restart flag lost");
        manager.ThrowRestart=true; Check(State.IsRestarting, "Restart exception did not fail closed");
        GameEntryPoint.SetForTest(null); PollAbsent();
        // A new stage must use the new manager, never a cached former instance.
        GameEntryPoint.SetForTest(new EventManager {status=EventStatusEnums.EventStatus.WAITING_TO_BEGIN});
        Check(!State.IsDriving && State.IsEngineLive && State.IsPlayerView && !State.IsRestarting, "Stale manager after stage replacement");
        Check(GameEntryPoint.GetterCalls==0, "Present-manager reads invoked the lazy accessor");
        Console.WriteLine(JsonSerializer.Serialize(new {status="passed", assertions,
            scope="production state predicates; lazy getter side effects, lifecycle and ownership; no game drive"}));
    }
}
