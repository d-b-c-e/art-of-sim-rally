// Input observations only. Actual hook installation is checked against the
// installed game's types and Unity Mono in ProbeHooks.
public class PanelManager { private void Update() { } }
public class ModsPanel { private void Update() { } }
public class PauseScreen { public void UpdateMe() { } }
public class ReplayManager { private void Update() { } }
namespace Rewired.Integration.UnityUI
{
    public class RewiredPointerInputModule
    {
        private readonly Dictionary<int, Dictionary<int, Rewired.UI.PlayerPointerEventData>[]> m_PlayerPointerData = new();
        public void AddPointer(Rewired.UI.PlayerPointerEventData data) => m_PlayerPointerData[0] = new[] { null, new Dictionary<int, Rewired.UI.PlayerPointerEventData> { [0] = data } };
    }
    public class RewiredStandaloneInputModule : RewiredPointerInputModule
    {
        public int HorizontalActionId=101,VerticalActionId=102,SubmitActionId=103,CancelActionId=104;
        public void Process() { }
    }
}
namespace Rewired.UI
{
    public class PlayerPointerEventData
    {
        public bool eligibleForClick, dragging;
        public object pointerPress, rawPointerPress, pointerDrag;
        public int clickCount;
    }
}
namespace Rewired
{
    public class Player
    {
        public readonly HashSet<int> Down=new(), Buttons=new(), Negative=new();
        public readonly Dictionary<int,float> Axes=new();
        public bool GetButtonDown(int id)=>Down.Contains(id);
        public bool GetButton(int id)=>Buttons.Contains(id);
        public bool GetNegativeButton(int id)=>Negative.Contains(id);
        public float GetAxis(int id)=>Axes.TryGetValue(id,out var x)?x:0;
    }
    public static class ReInput
    {
        public static bool isReady=true;
        public static Players players=new();
        public class Players { public readonly List<Player> AllPlayers=new(){new Player()}; }
    }
}
