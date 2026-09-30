using System.Collections.Generic;
using Dbce.TripleScreen.Protocol;
using UnityEngine;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>Repositions only interactive menu elements; never crops the game canvas.</summary>
internal sealed class MenuCenteringController
{
    private readonly Dictionary<RectTransform, Vector2> _original = new();
    private PanelManager _manager;
    private float _offset;

    internal void Update(bool enabled, LayoutLoadResult layout)
    {
        if (!enabled || !MatchesThreePanelOutput(layout))
        {
            Release();
            return;
        }

        var manager = Object.FindObjectOfType<PanelManager>();
        if (manager == null)
        {
            Release();
            return;
        }

        if (_manager != manager)
        {
            Release();
            _manager = manager;
        }

        var canvas = manager.PanelsCanvas;
        if (canvas == null || canvas.scaleFactor <= 0f)
        {
            Release();
            return;
        }

        var offset = Screen.width / 3f / canvas.scaleFactor;
        if (_original.Count > 0 && Mathf.Abs(offset - _offset) > 0.01f)
            Release();
        _manager = manager;
        _offset = offset;

        Move(manager.transform.Find("IntroSplashScreen/Text"), offset);
        Move(manager.transform.Find("Main Menu/Race/VerticalLayoutGroup"), offset);
        Move(manager.transform.Find("Main Menu/Race/GameLogo"), offset);
    }

    internal void Release()
    {
        foreach (var item in _original)
            if (item.Key != null) item.Key.anchoredPosition = item.Value;
        _original.Clear();
        _manager = null;
        _offset = 0f;
    }

    private void Move(Transform candidate, float offset)
    {
        if (candidate == null) return;
        var rect = candidate as RectTransform;
        if (rect == null || _original.ContainsKey(rect)) return;
        var original = rect.anchoredPosition;
        _original.Add(rect, original);
        rect.anchoredPosition = new Vector2(original.x + offset, original.y);
    }

    private static bool MatchesThreePanelOutput(LayoutLoadResult result)
    {
        if (!result.IsSuccess || result.Document?.Output == null || result.Document.Panel == null) return false;
        var output = result.Document.Output;
        var panel = result.Document.Panel;
        return panel.Count == 3 &&
               (output.Mode == "nvidia-surround" || output.Mode == "borderless-span") &&
               output.CombinedWidthPx == Screen.width && output.CombinedHeightPx == Screen.height &&
               (long)panel.NativeWidthPx * 3 == Screen.width && panel.NativeHeightPx == Screen.height;
    }
}
