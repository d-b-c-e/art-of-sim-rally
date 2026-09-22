using System;
using UnityEngine;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>Applies one reversible projection immediately before the camera renders.</summary>
[DefaultExecutionOrder(10000)]
internal sealed class CenterProjectionDriver : MonoBehaviour
{
    private Camera _camera;
    private Rect _originalRect;
    private Matrix4x4 _projection;
    private Rect _targetRect;
    private bool _configured;

    internal long SuccessfulFrames { get; private set; }
    internal DateTime LastSuccessfulFrameUtc { get; private set; }

    private void Awake()
    {
        _camera = GetComponent<Camera>();
        if (_camera != null) _originalRect = _camera.rect;
    }

    internal void Configure(Matrix4x4 projection, Rect targetRect)
    {
        _projection = projection;
        _targetRect = targetRect;
        _configured = true;
        enabled = true;
    }

    internal void Release()
    {
        _configured = false;
        enabled = false;
        Restore();
    }

    private void OnPreCull()
    {
        if (!_configured || _camera == null) return;
        _camera.rect = _targetRect;
        _camera.projectionMatrix = _projection;
        SuccessfulFrames++;
        LastSuccessfulFrameUtc = DateTime.UtcNow;
    }

    private void OnDisable() => Restore();
    private void OnDestroy() => Restore();

    private void Restore()
    {
        if (_camera == null) return;
        _camera.rect = _originalRect;
        _camera.ResetProjectionMatrix();
    }
}
