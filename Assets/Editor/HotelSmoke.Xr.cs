using System.Collections;
using System.Linq;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using UnityEngine.XR.Interaction.Toolkit.Inputs;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Simulation;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.UI;

internal static partial class HotelSmoke
{
    private const string SimulatorAsset = "Assets/Samples/XR Interaction Toolkit/3.5.1/XR Device Simulator/XR Device Simulator.prefab";
    private const InputTrackingState FullPose = InputTrackingState.Position | InputTrackingState.Rotation;

    private static void CheckXrScene()
    {
        var rig = Find<XROrigin>();
        var session = rig.GetComponent<HotelXrSession>();
        var desktop = rig.GetComponent<HotelDesktopControls>();
        var headPose = rig.Camera.GetComponent<TrackedPoseDriver>();
        var manager = rig.GetComponent<InputActionManager>();
        var modality = rig.GetComponent<XRInputModalityManager>();
        var transformer = rig.GetComponentInChildren<XRBodyTransformer>(true);
        Check(session != null && desktop.body != null && desktop.view == rig.Camera && !desktop.enabled &&
            rig.CameraFloorOffsetObject.transform.localPosition == Vector3.zero &&
            rig.Camera.transform.IsChildOf(rig.CameraFloorOffsetObject.transform) &&
            transformer.useCharacterControllerIfExists && manager.actionAssets.Any(asset => asset != null) &&
            headPose.positionInput.action != null && headPose.rotationInput.action != null && headPose.trackingStateInput.action != null,
            "XR scene binds the camera, floor offset, actions, locomotion and collision body.");
        var eventSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        Check(eventSystems.Length == 1 && eventSystems[0].GetComponent<XRUIInputModule>() is { isActiveAndEnabled: true },
            "The scene has one active XR UI input module. The cabin and keypad use physical interactables.");

        foreach (var controller in new[] { modality.leftController, modality.rightController })
        {
            var pose = controller.GetComponent<TrackedPoseDriver>();
            var ray = controller.GetComponentInChildren<NearFarInteractor>(true);
            Check(pose.positionInput.action != null && pose.rotationInput.action != null &&
                pose.trackingStateInput.action != null && ray.allowHoveredActivate &&
                ray.selectInput.inputActionReferencePerformed?.action != null &&
                ray.activateInput.inputActionReferencePerformed?.action != null,
                $"{controller.name} binds pose, grab and trigger to the near/far interactor.");
        }
    }

    private static IEnumerator ExerciseXr()
    {
        var previousInputSettings = InputSystem.settings;
        bool previousRunInBackground = Application.runInBackground;
        var testSettings = Object.Instantiate(previousInputSettings);
        testSettings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings = testSettings;
        Application.runInBackground = true;
        yield return null;
        var session = Find<HotelXrSession>();
        var desktop = Find<HotelDesktopControls>();
        Check(desktop.enabled, "Without running XR subsystems the desktop owns the camera and movement.");
        var keyboard = Keyboard.current == null ? InputSystem.AddDevice<Keyboard>() : null;
        var mouse = Mouse.current == null ? InputSystem.AddDevice<Mouse>() : null;
        var simulator = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(SimulatorAsset));
        yield return Await(() => session.IsXrReady, 5f);
        Check(!desktop.enabled && InputSystem.GetDevice<XRSimulatedHMD>() != null &&
            InputSystem.GetDevice<XRSimulatedController>(UnityEngine.InputSystem.CommonUsages.LeftHand) != null &&
            InputSystem.GetDevice<XRSimulatedController>(UnityEngine.InputSystem.CommonUsages.RightHand) != null,
            "The installed XRI simulator takes over from desktop and creates a headset and two controllers.");
        Object.Destroy(simulator);
        yield return Await(() => desktop.enabled, 5f);
        if (keyboard != null)
            InputSystem.RemoveDevice(keyboard);
        if (mouse != null)
            InputSystem.RemoveDevice(mouse);

        // Drive the same action bindings deterministically after exercising the actual simulator prefab.
        var hmd = InputSystem.AddDevice<XRSimulatedHMD>();
        var left = InputSystem.AddDevice<XRSimulatedController>();
        var right = InputSystem.AddDevice<XRSimulatedController>();
        InputSystem.SetDeviceUsage(left, UnityEngine.InputSystem.CommonUsages.LeftHand);
        InputSystem.SetDeviceUsage(right, UnityEngine.InputSystem.CommonUsages.RightHand);
        var hmdState = new XRSimulatedHMDState();
        hmdState.Reset();
        hmdState.trackingState = (int)FullPose;
        hmdState.isTracked = true;
        hmdState.centerEyePosition = new Vector3(0.44f, 0.13f, -0.38f);
        InputSystem.QueueStateEvent(hmd, hmdState);
        var leftState = ControllerPose(-0.17f, Quaternion.Euler(0f, -21f, 0f));
        var rightState = ControllerPose(0.18f, Quaternion.Euler(0f, 15f, 0f));
        InputSystem.QueueStateEvent(left, leftState);
        InputSystem.QueueStateEvent(right, rightState);

        var preferences = new[] { "hotel.volume", "hotel.difficulty", "hotel.smoothTurn" }
            .ToDictionary(key => key, key => PlayerPrefs.HasKey(key) ? (int?)PlayerPrefs.GetInt(key) : null);
        float originalVolume = AudioListener.volume;
        try
        {
            yield return Await(() => session.IsXrReady, 5f);
            var rig = session.GetComponent<XROrigin>();
            var snapped = rig.GetComponentInChildren<SnapTurnProvider>();
            var turning = rig.GetComponentInChildren<ContinuousTurnProvider>();
            var locomotion = rig.GetComponentInChildren<ContinuousMoveProvider>();
            var modality = rig.GetComponent<XRInputModalityManager>();
            var head = rig.Camera.transform;
            var localHead = rig.CameraInOriginSpacePos;
            Check(Mathf.Abs(localHead.y - 1.75f) < 0.04f &&
                new Vector2(localHead.x, localHead.z).magnitude < 0.025f,
                "A displaced simulated headset spawns centered with one eye-height offset.");
            yield return Await(() => modality.leftController.activeInHierarchy && modality.rightController.activeInHierarchy, 5f);
            Check(Quaternion.Angle(modality.leftController.transform.localRotation, leftState.deviceRotation) < 1f &&
                Quaternion.Angle(modality.rightController.transform.localRotation, rightState.deviceRotation) < 1f &&
                Vector3.Distance(modality.leftController.transform.localPosition, leftState.devicePosition) < 0.01f &&
                Vector3.Distance(modality.rightController.transform.localPosition, rightState.devicePosition) < 0.01f,
                "Both controllers follow position and rotation from their own Input System actions.");

            var options = Find<HotelOptions>();
            var elevator = Find<ElevatorSequence>();
            var ray = modality.rightController.GetComponentInChildren<NearFarInteractor>();
            var leftRay = modality.leftController.GetComponentInChildren<NearFarInteractor>();
            yield return PressWithTrigger(left, leftState, leftRay, options.volumeLabel.GetComponentInParent<HotelButton>());
            Check(!Mathf.Approximately(AudioListener.volume, originalVolume), "The left trigger changes cabin audio.");
            var originalDifficulty = HotelOptions.Difficulty;
            yield return PressWithTrigger(right, rightState, ray, options.difficultyLabel.GetComponentInParent<HotelButton>());
            Check(HotelOptions.Difficulty != originalDifficulty && options.tension.Profile.difficulty == HotelOptions.Difficulty,
                "The right trigger changes the active Nightmare difficulty.");

            var turnButton = options.turnLabel.GetComponentInParent<HotelButton>();
            bool originalTurn = HotelOptions.SmoothTurn;
            yield return PressWithTrigger(right, rightState, ray, turnButton);
            Check(turning.enabled != originalTurn && snapped.enabled != turning.enabled,
                "The trigger switches the cabin's turn setting and only one provider is active.");
            session.ApplyTurnMode(false);

            Vector3 facing = rig.transform.forward;
            rightState.primary2DAxis = Vector2.right;
            InputSystem.QueueStateEvent(right, rightState);
            yield return Await(() => Vector3.Angle(facing, rig.transform.forward) > 20f, 4f);
            rightState.primary2DAxis = Vector2.zero;
            InputSystem.QueueStateEvent(right, rightState);
            Check(Mathf.Abs(Vector3.Angle(facing, rig.transform.forward) - 30f) < 1f, "The right stick applies a 30-degree snap turn.");
            rig.transform.rotation = Quaternion.identity;
            yield return null;

            var play = GameObject.Find("Play").GetComponent<HotelButton>();
            yield return PressWithTrigger(right, rightState, ray, play);
            yield return Await(() => !elevator.menuCanvas.activeSelf, 3f);
            leftState.primary2DAxis = Vector2.up;
            InputSystem.QueueStateEvent(left, leftState);
            yield return Await(() => head.position.z > -18.31f, 4f);
            Check(!elevator.DoorsOpen && head.position.z < -17.55f && locomotion.enabled,
                "The left stick moves the player inside the cabin and the closed doors block the collision body.");
            yield return Await(() => elevator.DoorsOpen && head.position.z > -16.75f, 12f);
            leftState.primary2DAxis = Vector2.zero;
            InputSystem.QueueStateEvent(left, leftState);
            Check(head.position.y > 1.6f, "The trigger starts the ride and the left stick crosses the open doorway at standing height.");
        }
        finally
        {
            InputSystem.RemoveDevice(right);
            InputSystem.RemoveDevice(left);
            InputSystem.RemoveDevice(hmd);
            InputSystem.settings = previousInputSettings;
            Application.runInBackground = previousRunInBackground;
            Object.Destroy(testSettings);
            foreach (var preference in preferences)
            {
                if (preference.Value is int value)
                    PlayerPrefs.SetInt(preference.Key, value);
                else
                    PlayerPrefs.DeleteKey(preference.Key);
            }
            PlayerPrefs.Save();
            AudioListener.volume = originalVolume;
        }
        SceneManager.LoadScene("GameScene");
        yield return Await(() => Find<HotelDesktopControls>().enabled, 5f);
    }

    private static XRSimulatedControllerState ControllerPose(float x, Quaternion rotation)
    {
        var state = new XRSimulatedControllerState();
        state.Reset();
        state.trackingState = (int)FullPose;
        state.isTracked = true;
        state.devicePosition = new Vector3(0.44f + x, -0.29f, -0.27f);
        state.deviceRotation = rotation;
        return state;
    }

    private static IEnumerator PressWithTrigger(XRSimulatedController device, XRSimulatedControllerState state, NearFarInteractor ray, HotelButton button)
    {
        var interactable = button.GetComponent<XRSimpleInteractable>();
        var target = interactable.colliders[0].bounds.center;
        state.deviceRotation = Quaternion.LookRotation(target - ray.transform.position);
        InputSystem.QueueStateEvent(device, state);
        yield return Await(() => ray.IsHovering(interactable), 3f);
        state.trigger = 1f;
        state.buttons = (ushort)(1 << (int)ControllerButton.TriggerButton);
        InputSystem.QueueStateEvent(device, state);
        yield return null;
        yield return null;
        state.trigger = 0f;
        state.buttons = 0;
        InputSystem.QueueStateEvent(device, state);
        yield return null;
    }
}
