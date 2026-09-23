using System;
using UnityEngine;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>
/// Renders three independent off-axis views into panel-sized targets and
/// composites them before Unity draws screen-space overlay UI.
/// </summary>
[DefaultExecutionOrder(10000)]
internal sealed class ThreeViewRenderDriver : MonoBehaviour
{
    private readonly Camera[] _views = new Camera[3];
    private readonly RenderTexture[] _targets = new RenderTexture[3];
    private readonly Matrix4x4[] _projections = new Matrix4x4[3];
    private readonly Quaternion[] _rotations = new Quaternion[3];
    private Camera _source;
    private string _layoutHash;
    private int _panelWidth;
    private int _panelHeight;
    private int _preparedFrame = -1;
    private bool _configured;

    internal long SuccessfulFrames { get; private set; }
    internal DateTime LastSuccessfulFrameUtc { get; private set; }
    internal string RenderError { get; private set; }

    private void Awake() => _source = GetComponent<Camera>();

    internal void Configure(
        string layoutHash,
        int panelWidth,
        int panelHeight,
        Matrix4x4[] projections,
        Quaternion[] rotations)
    {
        if (projections == null || projections.Length != 3 || rotations == null || rotations.Length != 3)
            throw new ArgumentException("Exactly three projections and rotations are required.");
        if (panelWidth <= 0 || panelHeight <= 0)
            throw new ArgumentOutOfRangeException(nameof(panelWidth), "Panel resolution must be positive.");
        if (_source == null)
            throw new InvalidOperationException("The rendering source camera is unavailable.");

        if (!string.Equals(_layoutHash, layoutHash, StringComparison.Ordinal) ||
            _panelWidth != panelWidth || _panelHeight != panelHeight)
        {
            SuccessfulFrames = 0;
            LastSuccessfulFrameUtc = default;
            _preparedFrame = -1;
            _layoutHash = layoutHash;
            _panelWidth = panelWidth;
            _panelHeight = panelHeight;
            RenderError = null;
            ReleaseTargets();
        }

        for (var index = 0; index < 3; index++)
        {
            _projections[index] = projections[index];
            _rotations[index] = rotations[index];
        }

        _configured = true;
        enabled = true;
    }

    internal void Release()
    {
        _configured = false;
        _preparedFrame = -1;
        enabled = false;
        ReleaseTargets();
    }

    private void OnPreCull()
    {
        if (!_configured || _source == null) return;
        try
        {
            EnsureTargets();
            for (var index = 0; index < 3; index++)
            {
                var view = _views[index];
                view.CopyFrom(_source);
                view.enabled = false;
                view.targetTexture = _targets[index];
                view.rect = new Rect(0f, 0f, 1f, 1f);
                view.stereoTargetEye = StereoTargetEyeMask.None;
                view.transform.position = _source.transform.position;
                view.transform.rotation = _source.transform.rotation * _rotations[index];
                view.projectionMatrix = _projections[index];
                view.Render();
            }

            _preparedFrame = Time.frameCount;
        }
        catch (Exception exception)
        {
            _preparedFrame = -1;
            RenderError = SafeMessage(exception.Message);
        }
    }

    private void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (!_configured || _preparedFrame != Time.frameCount || _targets[0] == null ||
            _targets[1] == null || _targets[2] == null)
        {
            Graphics.Blit(source, destination);
            return;
        }

        try
        {
            var outputWidth = destination == null ? Screen.width : destination.width;
            var outputHeight = destination == null ? Screen.height : destination.height;
            if (outputWidth != _panelWidth * 3 || outputHeight != _panelHeight)
            {
                RenderError = "The compositor target no longer matches three native panel viewports.";
                Graphics.Blit(source, destination);
                return;
            }

            RenderTexture.active = destination;
            GL.Clear(true, true, Color.black);
            GL.PushMatrix();
            try
            {
                GL.LoadPixelMatrix(0, outputWidth, outputHeight, 0);
                for (var index = 0; index < 3; index++)
                    Graphics.DrawTexture(new Rect(index * _panelWidth, 0, _panelWidth, _panelHeight), _targets[index]);
            }
            finally
            {
                GL.PopMatrix();
            }

            SuccessfulFrames++;
            LastSuccessfulFrameUtc = DateTime.UtcNow;
            RenderError = null;
        }
        catch (Exception exception)
        {
            RenderError = SafeMessage(exception.Message);
            Graphics.Blit(source, destination);
        }
    }

    private void OnDisable() => ReleaseTargets();
    private void OnDestroy() => ReleaseTargets();

    private void EnsureTargets()
    {
        for (var index = 0; index < 3; index++)
        {
            if (_targets[index] == null)
            {
                var target = new RenderTexture(_panelWidth, _panelHeight, 24, RenderTextureFormat.ARGB32)
                {
                    name = "DBCE Triple " + index,
                    filterMode = FilterMode.Point,
                    useMipMap = false,
                    autoGenerateMips = false
                };
                if (!target.Create())
                {
                    UnityEngine.Object.Destroy(target);
                    throw new InvalidOperationException("Could not allocate a triple-screen render texture.");
                }

                _targets[index] = target;
            }

            if (_views[index] == null)
            {
                var cameraObject = new GameObject("DBCE Triple View " + index);
                cameraObject.tag = "Untagged";
                cameraObject.transform.SetParent(transform, false);
                var camera = cameraObject.AddComponent<Camera>();
                camera.enabled = false;
                _views[index] = camera;
            }
        }
    }

    private void ReleaseTargets()
    {
        for (var index = 0; index < 3; index++)
        {
            if (_views[index] != null)
            {
                _views[index].targetTexture = null;
                UnityEngine.Object.Destroy(_views[index].gameObject);
                _views[index] = null;
            }

            if (_targets[index] != null)
            {
                _targets[index].Release();
                UnityEngine.Object.Destroy(_targets[index]);
                _targets[index] = null;
            }
        }
    }

    private static string SafeMessage(string message)
    {
        var singleLine = (message ?? "Unknown renderer error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 400 ? singleLine : singleLine.Substring(0, 400);
    }
}
