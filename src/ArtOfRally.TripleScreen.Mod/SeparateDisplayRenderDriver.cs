using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AwesomeTechnologies.VegetationSystem;
using Dbce.TripleScreen;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace ArtOfRally.TripleScreen.Mod;

/// <summary>
/// Sends physical off-axis views directly to three independent Unity displays.
/// Unlike the Surround prototype, this path has no wide render target or
/// compositor. Secondary-display activation persists until the game exits.
/// </summary>
[DefaultExecutionOrder(10000)]
internal sealed class SeparateDisplayRenderDriver : MonoBehaviour
{
    private static readonly FieldInfo PostProcessResourcesField = typeof(PostProcessLayer).GetField(
        "m_Resources", BindingFlags.Instance | BindingFlags.NonPublic);
    private readonly Camera[] _sides = new Camera[2];
    private readonly SeparateViewFrameProbe[] _sideFrames = new SeparateViewFrameProbe[2];
    private readonly Dictionary<VegetationSystemPro, CameraCullingMode> _vegetationOriginalCulling = new();
    private readonly Matrix4x4[] _projections = new Matrix4x4[3];
    private readonly Quaternion[] _rotations = new Quaternion[3];
    private Camera _source;
    private GameObject _sideColorVolumeObject;
    private PostProcessProfile _sideColorProfile;
    private ColorGrading _sideColorGrading;
    private readonly List<PostProcessEffectSettings> _sideEffectCopies = new();
    private float _lastColorRefresh;
    private float _lastVegetationScan = -1f;
    private string _colorGradeStep;
    private int _sideVolumeLayer = -1;
    private string _layoutHash;
    private Rect _originalRect;
    private int _originalTargetDisplay;
    private int _leftDisplayIndex;
    private int _rightDisplayIndex;
    private bool _spanMode;
    private bool _configured;
    private bool _wasFocused;
    private int _raiseWindowsAfterFrame = -1;

    internal long SuccessfulCenterFrames { get; private set; }
    internal DateTime LastCenterFrameUtc { get; private set; }
    internal int LastCenterFrame { get; private set; } = -1;
    internal bool AllDisplaysRendered => LastCenterFrame >= 0 &&
        Time.frameCount - LastCenterFrame <= 2 &&
        _sideFrames[0] != null && _sideFrames[0].LastRenderedFrame == LastCenterFrame &&
        _sideFrames[1] != null && _sideFrames[1].LastRenderedFrame == LastCenterFrame;
    internal DateTime LastThreeViewFrameUtc => AllDisplaysRendered
        ? Min(LastCenterFrameUtc, Min(_sideFrames[0].LastFrameUtc, _sideFrames[1].LastFrameUtc))
        : default;
    internal double? LastCameraCallbackSpanMs => AllDisplaysRendered
        ? (Max(LastCenterFrameUtc, Max(_sideFrames[0].LastFrameUtc, _sideFrames[1].LastFrameUtc)) -
           LastThreeViewFrameUtc).TotalMilliseconds
        : (double?)null;
    internal int ActiveSidePostProcessLayers =>
        (_sides[0] != null && _sides[0].GetComponent<PostProcessLayer>()?.enabled == true ? 1 : 0) +
        (_sides[1] != null && _sides[1].GetComponent<PostProcessLayer>()?.enabled == true ? 1 : 0);
    internal bool SideColorGradingEnabled => _sideColorGrading != null && _sideColorGrading.enabled.value;
    internal bool SideColorGradeCreated => _sideColorGrading != null;
    internal string SideColorGradeError { get; private set; }
    internal string SideEffectSummary { get; private set; } = "none";
    internal int ExpandedVegetationSystems => _vegetationOriginalCulling.Count;
    internal int SideOcclusionCount => (_sides[0] != null && _sides[0].GetComponent<AmplifyOcclusionEffect>()?.enabled == true ? 1 : 0) +
        (_sides[1] != null && _sides[1].GetComponent<AmplifyOcclusionEffect>()?.enabled == true ? 1 : 0);
    internal int SideBeautifyCount => (_sides[0] != null && _sides[0].GetComponent<BeautifyEffect.Beautify>()?.enabled == true ? 1 : 0) +
        (_sides[1] != null && _sides[1].GetComponent<BeautifyEffect.Beautify>()?.enabled == true ? 1 : 0);
    internal int SideVolumetricCount => (_sides[0] != null && _sides[0].GetComponent<HxVolumetricCamera>()?.enabled == true &&
                                         _sides[0].GetComponent<HxVolumetricImageEffect>()?.enabled == true ? 1 : 0) +
                                        (_sides[1] != null && _sides[1].GetComponent<HxVolumetricCamera>()?.enabled == true &&
                                         _sides[1].GetComponent<HxVolumetricImageEffect>()?.enabled == true ? 1 : 0);
    internal int LastRaisedSideWindowCount { get; private set; }
    internal string RenderError { get; private set; }
    internal bool SpanMode => _spanMode;

    private void Awake()
    {
        _source = GetComponent<Camera>();
        if (_source == null) return;
        _originalRect = _source.rect;
        _originalTargetDisplay = _source.targetDisplay;
    }

    internal void Configure(string layoutHash, Matrix4x4[] projections, Quaternion[] rotations, int leftDisplayIndex, int rightDisplayIndex)
    {
        if (_source == null) throw new InvalidOperationException("The gameplay camera is unavailable.");
        if (projections == null || projections.Length != 3 || rotations == null || rotations.Length != 3)
            throw new ArgumentException("Exactly three projections and rotations are required.");
        if (leftDisplayIndex < 1 || rightDisplayIndex < 1 || leftDisplayIndex == rightDisplayIndex ||
            leftDisplayIndex >= Display.displays.Length || rightDisplayIndex >= Display.displays.Length)
            throw new ArgumentOutOfRangeException(nameof(leftDisplayIndex), "Two distinct secondary Unity display indices are required.");

        ConfigureCore(layoutHash, projections, rotations, false, leftDisplayIndex, rightDisplayIndex);
    }

    internal void ConfigureSpan(string layoutHash, Matrix4x4[] projections, Quaternion[] rotations)
    {
        if (_source == null) throw new InvalidOperationException("The gameplay camera is unavailable.");
        if (projections == null || projections.Length != 3 || rotations == null || rotations.Length != 3)
            throw new ArgumentException("Exactly three projections and rotations are required.");
        ConfigureCore(layoutHash, projections, rotations, true, 0, 0);
    }

    private void ConfigureCore(string layoutHash, Matrix4x4[] projections, Quaternion[] rotations,
        bool spanMode, int leftDisplayIndex, int rightDisplayIndex)
    {
        // An old frame must never certify a new layout or a remapped display.
        if (!string.Equals(_layoutHash, layoutHash, StringComparison.Ordinal) ||
            _spanMode != spanMode ||
            _leftDisplayIndex != leftDisplayIndex || _rightDisplayIndex != rightDisplayIndex)
        {
            _layoutHash = layoutHash;
            SuccessfulCenterFrames = 0;
            LastCenterFrame = -1;
            LastCenterFrameUtc = default;
            for (var index = 0; index < _sideFrames.Length; index++)
                _sideFrames[index]?.ResetEvidence();
        }

        for (var index = 0; index < 3; index++)
        {
            _projections[index] = projections[index];
            _rotations[index] = rotations[index];
        }

        _leftDisplayIndex = leftDisplayIndex;
        _rightDisplayIndex = rightDisplayIndex;
        _spanMode = spanMode;
        EnsureSideColorVolume();
        EnsureSideCameras();
        ExpandVegetationVisibility();
        if (!spanMode)
        {
            ActivateIfNeeded(_leftDisplayIndex);
            ActivateIfNeeded(_rightDisplayIndex);
        }
        _configured = true;
        enabled = true;
        RenderError = null;
    }

    internal void Release()
    {
        _configured = false;
        _spanMode = false;
        enabled = false;
        _wasFocused = false;
        _raiseWindowsAfterFrame = -1;
        LastRaisedSideWindowCount = 0;
        _layoutHash = null;
        SuccessfulCenterFrames = 0;
        LastCenterFrame = -1;
        LastCenterFrameUtc = default;
        foreach (var registration in _vegetationOriginalCulling)
        {
            if (registration.Key == null) continue;
            var sourceEntry = registration.Key.GetVegetationStudioCamera(_source);
            if (sourceEntry != null) sourceEntry.CameraCullingMode = registration.Value;
        }
        _vegetationOriginalCulling.Clear();
        _lastVegetationScan = -1f;
        RestoreSource();
        for (var index = 0; index < _sides.Length; index++)
        {
            if (_sides[index] == null) continue;
            _sides[index].enabled = false;
            Destroy(_sides[index].gameObject);
            _sides[index] = null;
            _sideFrames[index] = null;
        }
        if (_sideColorVolumeObject != null)
        {
            _sideColorVolumeObject.SetActive(false);
            Destroy(_sideColorVolumeObject);
            _sideColorVolumeObject = null;
        }
        foreach (var effect in _sideEffectCopies)
            if (effect != null) Destroy(effect);
        _sideEffectCopies.Clear();
        if (_sideColorProfile != null) Destroy(_sideColorProfile);
        _sideColorGrading = null;
        _sideColorProfile = null;
        _sideVolumeLayer = -1;
        _lastColorRefresh = 0f;
        SideColorGradeError = null;
        SideEffectSummary = "none";
    }

    private static void ActivateIfNeeded(int index)
    {
        if (!Display.displays[index].active) Display.displays[index].Activate();
    }

    private void EnsureSideColorVolume()
    {
        if (_sideColorVolumeObject != null) return;
        var sourceEffects = _source.GetComponent<PostProcessLayer>();
        if (sourceEffects == null || !sourceEffects.enabled)
            throw new InvalidOperationException("The gameplay camera has no active color-grading layer to copy.");
        var occupied = UnityEngine.Object.FindObjectsOfType<PostProcessVolume>();
        for (var layer = 31; layer >= 24; layer--)
        {
            var used = false;
            foreach (var existingVolume in occupied)
                if (existingVolume.gameObject.layer == layer) { used = true; break; }
            if (used || (sourceEffects.volumeLayer.value & (1 << layer)) != 0)
                continue;
            _sideVolumeLayer = layer;
            break;
        }
        if (_sideVolumeLayer < 0)
            throw new InvalidOperationException("No unused post-processing layer is available for isolated side color grading.");

        _sideColorProfile = ScriptableObject.CreateInstance<PostProcessProfile>();
        _sideColorProfile.hideFlags = HideFlags.DontSave;
        _sideColorVolumeObject = new GameObject("DBCE Side Color Grade");
        _sideColorVolumeObject.SetActive(false);
        _sideColorVolumeObject.layer = _sideVolumeLayer;
        var volume = _sideColorVolumeObject.AddComponent<PostProcessVolume>();
        volume.isGlobal = true;
        volume.priority = 1000f;
        volume.weight = 1f;
        volume.sharedProfile = _sideColorProfile;
        _sideColorVolumeObject.SetActive(true);
    }

    private void EnsureSideCameras()
    {
        for (var index = 0; index < _sides.Length; index++)
        {
            if (_sides[index] != null) continue;
            var side = new GameObject("DBCE Separate View " + (index == 0 ? "Left" : "Right"));
            side.SetActive(false);
            side.tag = "Untagged";
            side.transform.SetParent(transform, false);
            _sides[index] = side.AddComponent<Camera>();
            _sides[index].enabled = false;
            SyncPostProcessing(_sides[index], _source.GetComponent<PostProcessLayer>());
            MirrorVolumetricEffect(side);
            MirrorCameraEffect(_source.GetComponent<AmplifyOcclusionEffect>(), side);
            MirrorCameraEffect(_source.GetComponent<BeautifyEffect.Beautify>(), side);
            // Unity may invoke camera callbacks in component order even when
            // a script execution order is declared. Add our hook after PP.
            _sideFrames[index] = side.AddComponent<SeparateViewFrameProbe>();
            side.SetActive(true);
        }
    }

    private void MirrorVolumetricEffect(GameObject destination)
    {
        var sourceVolume = _source.GetComponent<HxVolumetricCamera>();
        var sourceImage = _source.GetComponent<HxVolumetricImageEffect>();
        if (sourceVolume == null || !sourceVolume.enabled || !sourceVolume.volumetricEnabled ||
            sourceImage == null || !sourceImage.enabled) return;

        var volume = destination.AddComponent<HxVolumetricCamera>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(sourceVolume), volume);
        // The callbacks are camera-specific references. A serialized copy
        // must never toggle or draw through the center camera's image effect.
        volume.callBackImageEffect = null;
        volume.callBackImageEffectOpaque = null;
        volume.enabled = true;
        var image = destination.AddComponent<HxVolumetricImageEffect>();
        image.enabled = true;
    }

    private static void MirrorCameraEffect<T>(T source, GameObject destination) where T : Behaviour
    {
        if (source == null || !source.enabled) return;
        var copy = destination.AddComponent<T>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), copy);
        copy.enabled = true;
    }

    private void ExpandVegetationVisibility()
    {
        if (_lastVegetationScan >= 0f && Time.realtimeSinceStartup - _lastVegetationScan < 1f) return;
        _lastVegetationScan = Time.realtimeSinceStartup;
        foreach (var system in FindObjectsOfType<VegetationSystemPro>())
        {
            if (system == null || !system.InitDone || _vegetationOriginalCulling.ContainsKey(system)) continue;
            var sourceEntry = system.GetVegetationStudioCamera(_source);
            if (sourceEntry == null) continue;
            // The game queues vegetation draws without a target camera, so
            // one broad culling pass makes the same objects available to all
            // three display cameras. Direct side-camera draws did not appear
            // on Unity secondary displays in the attended 0.3.7 test.
            _vegetationOriginalCulling.Add(system, sourceEntry.CameraCullingMode);
            sourceEntry.CameraCullingMode = CameraCullingMode.Complete360;
        }
    }

    private void LateUpdate()
    {
        if (!_configured || _source == null) return;
        try
        {
            var focused = Application.isFocused;
            if (focused && !_wasFocused) _raiseWindowsAfterFrame = Time.frameCount + 1;
            _wasFocused = focused;
            var sourceEffects = _source.GetComponent<PostProcessLayer>();
            for (var index = 0; index < 2; index++)
            {
                var view = _sides[index];
                if (view == null) throw new InvalidOperationException("A side camera was destroyed.");
                view.CopyFrom(_source);
                SyncPostProcessing(view, sourceEffects);
                // PostProcessLayer.OnPreCull resets custom matrices for a
                // non-physical camera. A custom matrix remains authoritative
                // when physical properties are enabled.
                view.usePhysicalProperties = true;
                view.targetTexture = null;
                view.targetDisplay = _spanMode ? 0 : index == 0 ? _leftDisplayIndex : _rightDisplayIndex;
                view.rect = _spanMode ? SpanRect(index == 0 ? 0 : 2) : new Rect(0f, 0f, 1f, 1f);
                view.depth = _source.depth + index + 1f;
                view.stereoTargetEye = StereoTargetEyeMask.None;
                view.transform.position = _source.transform.position;
                view.transform.rotation = _source.transform.rotation * _rotations[index == 0 ? 0 : 2];
                view.projectionMatrix = _projections[index == 0 ? 0 : 2];
                _sideFrames[index].Projection = _projections[index == 0 ? 0 : 2];
                view.enabled = true;
            }
            if (focused && _raiseWindowsAfterFrame >= 0 && Time.frameCount >= _raiseWindowsAfterFrame)
            {
                try { LastRaisedSideWindowCount = SideWindowFocusRestorer.Raise(); }
                catch { LastRaisedSideWindowCount = 0; }
                _raiseWindowsAfterFrame = -1;
            }
            RenderError = null;
        }
        catch (Exception exception)
        {
            RenderError = SafeMessage(exception.Message);
            for (var index = 0; index < _sides.Length; index++)
                if (_sides[index] != null) _sides[index].enabled = false;
        }
    }

    private void OnPreCull()
    {
        if (!_configured || _source == null) return;
        _source.targetDisplay = 0;
        _source.rect = _spanMode ? SpanRect(1) : new Rect(0f, 0f, 1f, 1f);
        _source.projectionMatrix = _projections[1];
    }

    private void OnPostRender()
    {
        if (!_configured) return;
        SuccessfulCenterFrames++;
        LastCenterFrame = Time.frameCount;
        LastCenterFrameUtc = DateTime.UtcNow;
        try
        {
            RefreshSideColorGrading();
        }
        catch (Exception exception)
        {
            var message = _colorGradeStep + ": " + exception.GetType().Name + ": " + SafeMessage(exception.Message);
            if (SideColorGradeError != message) Debug.LogException(exception);
            SideColorGradeError = message;
        }
    }

    private void OnDisable()
    {
        RestoreSource();
        for (var index = 0; index < _sides.Length; index++)
            if (_sides[index] != null) _sides[index].enabled = false;
    }
    private void OnDestroy() => Release();

    private void RestoreSource()
    {
        if (_source == null) return;
        _source.targetDisplay = _originalTargetDisplay;
        _source.rect = _originalRect;
        _source.ResetProjectionMatrix();
    }

    private void SyncPostProcessing(Camera view, PostProcessLayer source)
    {
        var side = view.GetComponent<PostProcessLayer>();
        if (source == null || !source.enabled)
        {
            if (side != null) side.enabled = false;
            return;
        }

        if (side == null)
        {
            var resources = PostProcessResourcesField?.GetValue(source) as PostProcessResources;
            if (resources == null)
                throw new InvalidOperationException("The game's post-processing resources are unavailable for side views.");
            side = view.gameObject.AddComponent<PostProcessLayer>();
            side.Init(resources);
        }

        // Camera.CopyFrom does not copy image-effect components. Each side
        // camera gets an isolated volume containing the source's active effects
        // except MotionBlur. The 0.3.1 full-volume attempt blurred side views.
        side.volumeLayer = 1 << _sideVolumeLayer;
        side.volumeTrigger = view.transform;
        side.stopNaNPropagation = source.stopNaNPropagation;
        side.finalBlitToCameraTarget = source.finalBlitToCameraTarget;
        side.antialiasingMode = source.antialiasingMode;
        side.breakBeforeColorGrading = source.breakBeforeColorGrading;
        if (source.fog != null && side.fog != null)
        {
            side.fog.enabled = source.fog.enabled;
            side.fog.excludeSkybox = source.fog.excludeSkybox;
        }
        if (source.fastApproximateAntialiasing != null && side.fastApproximateAntialiasing != null)
        {
            side.fastApproximateAntialiasing.fastMode = source.fastApproximateAntialiasing.fastMode;
            side.fastApproximateAntialiasing.keepAlpha = source.fastApproximateAntialiasing.keepAlpha;
        }
        if (source.subpixelMorphologicalAntialiasing != null && side.subpixelMorphologicalAntialiasing != null)
            side.subpixelMorphologicalAntialiasing.quality = source.subpixelMorphologicalAntialiasing.quality;
        side.enabled = true;
    }

    private void RefreshSideColorGrading()
    {
        var sourceLayer = _source.GetComponent<PostProcessLayer>();
        if (sourceLayer == null || !sourceLayer.enabled || _sideColorProfile == null) return;
        if (_sideEffectCopies.Count > 0 && Time.realtimeSinceStartup - _lastColorRefresh < 0.25f) return;

        var replacements = new List<PostProcessEffectSettings>();
        var names = new List<string>();
        try
        {
            foreach (var type in PostProcessManager.instance.settingsTypes.Keys)
            {
                if (type == typeof(MotionBlur)) continue;
                _colorGradeStep = "Read " + type.Name;
                var current = sourceLayer.GetBundle(type)?.settings;
                if (current == null || !current.active || !current.enabled.value) continue;
                _colorGradeStep = "Clone " + type.Name;
                var replacement = Instantiate(current);
                replacement.hideFlags = HideFlags.DontSave;
                replacement.SetAllOverridesTo(true, false);
                replacement.enabled.value = true;
                replacements.Add(replacement);
                names.Add(type.Name);
            }
        }
        catch
        {
            foreach (var effect in replacements) if (effect != null) Destroy(effect);
            throw;
        }

        _colorGradeStep = "Install side effects";
        _sideColorProfile.settings.Clear();
        _sideColorProfile.settings.AddRange(replacements);
        _sideColorProfile.isDirty = true;
        foreach (var effect in _sideEffectCopies) if (effect != null) Destroy(effect);
        _sideEffectCopies.Clear();
        _sideEffectCopies.AddRange(replacements);
        _sideColorGrading = replacements.OfType<ColorGrading>().FirstOrDefault();
        SideEffectSummary = names.Count == 0 ? "none" : string.Join(", ", names);
        _lastColorRefresh = Time.realtimeSinceStartup;
        SideColorGradeError = null;
    }

    private static string SafeMessage(string message)
    {
        var singleLine = (message ?? "Unknown separate-display error.").Replace('\r', ' ').Replace('\n', ' ');
        return singleLine.Length <= 400 ? singleLine : singleLine.Substring(0, 400);
    }

    private static Rect SpanRect(int panelIndex)
    {
        var viewport = TripleViewportLayout.ForPanelIndex(panelIndex);
        return new Rect((float)viewport.X, (float)viewport.Y,
            (float)viewport.Width, (float)viewport.Height);
    }

    private static DateTime Min(DateTime left, DateTime right) => left <= right ? left : right;
    private static DateTime Max(DateTime left, DateTime right) => left >= right ? left : right;
}

[DefaultExecutionOrder(10000)]
internal sealed class SeparateViewFrameProbe : MonoBehaviour
{
    private Camera _camera;
    internal Matrix4x4 Projection { get; set; }
    internal long RenderedFrames { get; private set; }
    internal int LastRenderedFrame { get; private set; } = -1;
    internal DateTime LastFrameUtc { get; private set; }

    internal void ResetEvidence()
    {
        RenderedFrames = 0;
        LastRenderedFrame = -1;
        LastFrameUtc = default;
    }

    private void Awake() => _camera = GetComponent<Camera>();

    private void OnPreCull()
    {
        // PostProcessLayer.OnPreCull resets custom projection matrices. Run
        // afterward on each side camera, as the source driver does on center.
        if (_camera != null) _camera.projectionMatrix = Projection;
    }

    private void OnPostRender()
    {
        RenderedFrames++;
        LastRenderedFrame = Time.frameCount;
        LastFrameUtc = DateTime.UtcNow;
    }
}
