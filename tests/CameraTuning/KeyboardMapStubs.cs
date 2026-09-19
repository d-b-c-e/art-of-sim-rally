using UnityEngine;

namespace Rewired
{
    public enum ControllerType { Keyboard }
    public static class ReInput
    {
        public static bool isReady = true;
        public static readonly Players players = new();
        public static readonly Mapping mapping = new();
    }
    public class Players { public Player Player = new(); public Player GetPlayer(int id) => Player; }
    public class Player { public Controllers controllers = new(); }
    public class Controllers { public Maps maps = new(); }
    public class Maps
    {
        public List<ControllerMap> Items = new() { new() };
        public bool Throw;
        public IEnumerable<ControllerMap> GetAllMaps(ControllerType type) => Throw ? throw new InvalidOperationException("teardown") : Items;
    }
    public class ControllerMap { public bool enabled = true; public List<ActionElementMap> AllMaps = new(); }
    public class ActionElementMap { public KeyCode keyCode; public int actionId; public bool Modified; }
    public class Mapping
    {
        public readonly Dictionary<int, InputAction> Actions = new();
        public InputAction GetAction(int id) => Actions.TryGetValue(id, out var action) ? action : null;
    }
    public class InputAction { public string name; }
}
