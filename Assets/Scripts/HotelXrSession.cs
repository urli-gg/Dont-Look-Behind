using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
using UnityEngine.XR.Management;
#if UNITY_EDITOR
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
#endif
using InputDevices = UnityEngine.XR.InputDevices;

[DefaultExecutionOrder(-300)]
[RequireComponent(typeof(XROrigin), typeof(HotelDesktopControls))]
public sealed class HotelXrSession : MonoBehaviour
{
    [SerializeField, Range(0.9f, 2.1f)] private float deviceEyeHeight = 1.62f;
    [SerializeField] private XRBodyTransformer locomotion;
    [SerializeField] private ControllerInputActionManager rightController;
    [SerializeField] private SnapTurnProvider snapTurn;
    [SerializeField] private ContinuousTurnProvider smoothTurn;

    private enum Mode { Starting, Desktop, Simulator, OpenXR }
    private const InputTrackingState HeadPose = InputTrackingState.Position | InputTrackingState.Rotation;

    private XROrigin origin;
    private HotelDesktopControls desktop;
    private TrackedPoseDriver headPose;
    private XRInputModalityManager modality;
    private XRLoader loader;
    private Mode mode;
    private bool originRequested;
    private bool ready;
    private int cameraLayers;
    private Color cameraBackground;
    private float nextDiagnostic;
    private string lastDiagnostic;

    public bool IsXrReady => ready && (mode == Mode.OpenXR || mode == Mode.Simulator);

    private void Awake()
    {
        origin = GetComponent<XROrigin>();
        desktop = GetComponent<HotelDesktopControls>();
        headPose = origin.Camera.GetComponent<TrackedPoseDriver>();
        modality = GetComponent<XRInputModalityManager>();
        cameraLayers = origin.Camera.cullingMask;
        cameraBackground = origin.Camera.backgroundColor;
        origin.CameraYOffset = deviceEyeHeight;
        desktop.enabled = false;
        locomotion.gameObject.SetActive(false);
    }

    private void Update()
    {
        var activeLoader = XRGeneralSettings.Instance?.Manager?.activeLoader;
        var display = activeLoader?.GetLoadedSubsystem<XRDisplaySubsystem>();
        var input = activeLoader?.GetLoadedSubsystem<XRInputSubsystem>();
        // A loader can initialize successfully even if XR session creation fails.
        // Keep ownership during a pause after the subsystems have started.
        bool hardware = display?.running == true || input?.running == true ||
            (mode == Mode.OpenXR && activeLoader != null && activeLoader == loader);
        var desired = hardware ? Mode.OpenXR : Mode.Desktop;
#if UNITY_EDITOR
        if (desired == Mode.OpenXR)
            StopSimulators();
        else if (InputSystem.GetDevice<XRSimulatedHMD>() != null)
            desired = Mode.Simulator;
#endif
        if (mode != desired || loader != activeLoader)
        {
            EnterMode(desired, activeLoader);
            return; // TrackedPoseDriver reads its initial values on the next input update.
        }

        if (!ready && mode == Mode.OpenXR && input?.running == true && !originRequested)
        {
            RequestTrackingOrigin(input);
            return; // Let the input system apply any change of reference space before using the pose.
        }

        var tracking = mode == Mode.OpenXR && input?.running == true
            ? input.GetTrackingOriginMode() : TrackingOriginModeFlags.Unknown;
        if (!ready && (mode == Mode.Simulator || (originRequested && tracking != TrackingOriginModeFlags.Unknown)) &&
            headPose.trackingStateInput.action is { } state &&
            ((InputTrackingState)state.ReadValue<int>() & HeadPose) == HeadPose)
        {
            AlignSpawn(tracking);
            ready = true;
            locomotion.gameObject.SetActive(true);
            ShowScene(true);
        }

        LogStatus(tracking, display?.running == true, input?.running == true);
    }

    private void EnterMode(Mode next, XRLoader activeLoader)
    {
        mode = next;
        loader = activeLoader;
        ready = next == Mode.Desktop;
        originRequested = false;
        locomotion.gameObject.SetActive(false);
        desktop.enabled = false;
        origin.CameraFloorOffsetObject.transform.localPosition =
            next == Mode.OpenXR ? Vector3.zero : Vector3.up * deviceEyeHeight;
        headPose.enabled = next != Mode.Desktop;
        if (modality != null)
            modality.enabled = next != Mode.Desktop;
        if (next == Mode.Desktop)
            desktop.enabled = true;
        ApplyTurnMode(HotelOptions.SmoothTurn);
        ShowScene(ready);
        nextDiagnostic = 0f;
    }

    private void RequestTrackingOrigin(XRInputSubsystem input)
    {
        var supported = input.GetSupportedTrackingOriginModes();
        if (supported == TrackingOriginModeFlags.Unknown)
            return;
        var current = input.GetTrackingOriginMode();
        var preferred = (supported & TrackingOriginModeFlags.Floor) != 0
            ? TrackingOriginModeFlags.Floor : TrackingOriginModeFlags.Device;
        bool accepted = current == preferred ||
            ((supported & preferred) != 0 && input.TrySetTrackingOriginMode(preferred));
        if (!accepted && current != TrackingOriginModeFlags.Device &&
            (supported & TrackingOriginModeFlags.Device) != 0)
            input.TrySetTrackingOriginMode(TrackingOriginModeFlags.Device);
        originRequested = true;
        Debug.Log($"[Hotel XR] Tracking supported={supported}, requested={preferred}, accepted={accepted}.", this);
    }

    private void AlignSpawn(TrackingOriginModeFlags tracking)
    {
        var offset = origin.CameraFloorOffsetObject.transform;
        var localOffset = offset.localPosition;
        if (mode == Mode.OpenXR)
        {
            localOffset.y = (tracking & TrackingOriginModeFlags.Floor) != 0 ? 0f : deviceEyeHeight;
            var forward = Vector3.ProjectOnPlane(origin.Camera.transform.forward, transform.up);
            if (forward.sqrMagnitude > 0.01f)
            {
                float yaw = Vector3.SignedAngle(forward, transform.forward, transform.up);
                transform.rotation = Quaternion.AngleAxis(yaw, transform.up) * transform.rotation;
            }
        }

        // Center the initial head and capsule over the authored spawn. Head and hands share this offset.
        var head = origin.CameraInOriginSpacePos;
        localOffset.x -= head.x;
        localOffset.z -= head.z;
        offset.localPosition = localOffset;
    }

    public void ApplyTurnMode(bool smooth)
    {
        rightController.smoothTurnEnabled = smooth;
        snapTurn.enabled = !smooth;
        smoothTurn.enabled = smooth;
    }

    private void ShowScene(bool visible)
    {
        origin.Camera.cullingMask = visible ? cameraLayers : 0;
        origin.Camera.backgroundColor = visible ? cameraBackground : Color.black;
    }

#if UNITY_EDITOR
    private static void StopSimulators()
    {
#pragma warning disable CS0618 // The project includes the classic XRI simulator sample.
        var classic = XRDeviceSimulator.instance;
#pragma warning restore CS0618
        var current = XRInteractionSimulator.instance;
        if (classic != null && classic.gameObject.activeSelf)
            classic.gameObject.SetActive(false);
        if (current != null && current.gameObject.activeSelf)
            current.gameObject.SetActive(false);
    }
#endif

    private void LogStatus(TrackingOriginModeFlags tracking, bool displayRunning, bool inputRunning)
    {
        if (Time.unscaledTime < nextDiagnostic)
            return;
        nextDiagnostic = Time.unscaledTime + 1f;
        string status = $"mode={mode} loader={loader?.name ?? "none"} " +
            $"display={displayRunning} input={inputRunning} origin={tracking} ready={ready} " +
            $"HMD={Describe(XRNode.Head)} left={Describe(XRNode.LeftHand)} right={Describe(XRNode.RightHand)}";
#if UNITY_EDITOR
        if (mode == Mode.Simulator)
        {
            var state = headPose.trackingStateInput.action;
            status += $" simulatedHmd={InputSystem.GetDevice<XRSimulatedHMD>()?.trackingState.ReadValue()} " +
                $"trackingAction={state?.enabled}/{state?.controls.Count}/{state?.ReadValue<int>()}";
        }
#endif
        if (status == lastDiagnostic)
            return;
        lastDiagnostic = status;
        Debug.Log($"[Hotel XR] {status} eyeHeight={origin.CameraInOriginSpaceHeight:F2}m", this);
    }

    private static string Describe(XRNode node)
    {
        var device = InputDevices.GetDeviceAtXRNode(node);
        if (!device.isValid)
            return "unavailable";
        return device.TryGetFeatureValue(UnityEngine.XR.CommonUsages.isTracked, out bool tracked)
            ? $"{device.name}/{(tracked ? "tracked" : "untracked")}" : device.name;
    }
}
