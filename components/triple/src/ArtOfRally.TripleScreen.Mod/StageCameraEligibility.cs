namespace ArtOfRally.TripleScreen.Mod;

// CameraManager disables the driving component when Cinemachine takes over
// the same stage camera. Keep rendering only if our matching driver is already
// armed on that exact camera; unrelated menu cameras must not inherit it.
internal static class StageCameraEligibility
{
    internal static bool CanRender(bool isExpectedMainCamera, bool hasDrivingRig,
        bool drivingRigEnabled, bool sameCameraAsArmed, bool matchingDriverArmed) =>
        isExpectedMainCamera && hasDrivingRig &&
        (drivingRigEnabled || (sameCameraAsArmed && matchingDriverArmed));
}
