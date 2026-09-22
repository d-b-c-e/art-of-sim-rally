using System;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityModManagerNet;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>
/// Observation-only first probe. It does not create cameras, change resolution,
/// activate displays, or write game settings.
/// </summary>
internal static class Main
{
    private static int _frames;
    private static string _lastSignature = string.Empty;

    private static bool Load(UnityModManager.ModEntry modEntry)
    {
        modEntry.OnUpdate = OnUpdate;
        LogEnvironment(modEntry.Logger);
        modEntry.Logger.Log("Read-only camera/display probe loaded; no rendering state was changed.");
        return true;
    }

    private static void OnUpdate(UnityModManager.ModEntry modEntry, float deltaTime)
    {
        _ = deltaTime;
        _frames++;
        if (_frames < 120 || _frames % 120 != 0)
        {
            return;
        }

        var cameras = Camera.allCameras.OrderBy(camera => camera.GetInstanceID()).ToArray();
        var scene = SceneManager.GetActiveScene();
        var signature = scene.handle + ":" + string.Join(",", cameras.Select(camera => camera.GetInstanceID().ToString()));
        if (string.Equals(signature, _lastSignature, StringComparison.Ordinal))
        {
            return;
        }

        _lastSignature = signature;
        LogCameraInventory(modEntry.Logger, scene.name, cameras);
    }

    private static void LogEnvironment(UnityModManager.ModEntry.ModLogger logger)
    {
        logger.Log($"Unity={Application.unityVersion}; OS={SystemInfo.operatingSystem}; GPU={SystemInfo.graphicsDeviceName}; API={SystemInfo.graphicsDeviceType}");
        logger.Log($"Screen={Screen.width}x{Screen.height}; mode={Screen.fullScreenMode}; displays={Display.displays.Length}");

        for (var index = 0; index < Display.displays.Length; index++)
        {
            var display = Display.displays[index];
            logger.Log($"Display[{index}] system={display.systemWidth}x{display.systemHeight}; render={display.renderingWidth}x{display.renderingHeight}; active={display.active}");
        }
    }

    private static void LogCameraInventory(
        UnityModManager.ModEntry.ModLogger logger,
        string sceneName,
        Camera[] cameras)
    {
        logger.Log($"Scene '{sceneName}' camera inventory: {cameras.Length} active camera(s).");
        foreach (var camera in cameras)
        {
            var components = string.Join(",", camera.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType().FullName)
                .OrderBy(name => name));
            var parent = camera.transform.parent == null ? "(none)" : camera.transform.parent.name;
            var rect = camera.rect;
            var projection = camera.projectionMatrix;
            logger.Log(
                $"Camera id={camera.GetInstanceID()} name='{camera.name}' parent='{parent}' " +
                $"tag='{camera.tag}' targetDisplay={camera.targetDisplay} depth={camera.depth:F2} " +
                $"rect=({rect.x:F4},{rect.y:F4},{rect.width:F4},{rect.height:F4}) " +
                $"fov={camera.fieldOfView:F3} clip={camera.nearClipPlane:F3}..{camera.farClipPlane:F3} " +
                $"path={camera.actualRenderingPath} targetTexture={(camera.targetTexture == null ? "none" : camera.targetTexture.name)} " +
                $"projection=[{projection.m00:F5},{projection.m02:F5};{projection.m11:F5},{projection.m12:F5};{projection.m22:F5},{projection.m23:F5}] " +
                $"components=[{components}]");
        }

        var canvases = Resources.FindObjectsOfTypeAll<Canvas>()
            .Where(canvas => canvas != null && canvas.gameObject.scene.IsValid())
            .OrderBy(canvas => canvas.GetInstanceID())
            .ToArray();
        logger.Log($"Scene '{sceneName}' canvas inventory: {canvases.Length} canvas(es).");
        foreach (var canvas in canvases)
        {
            logger.Log(
                $"Canvas id={canvas.GetInstanceID()} name='{canvas.name}' mode={canvas.renderMode} " +
                $"targetDisplay={canvas.targetDisplay} camera={(canvas.worldCamera == null ? "none" : canvas.worldCamera.name)} " +
                $"sortingOrder={canvas.sortingOrder}");
        }
    }
}
