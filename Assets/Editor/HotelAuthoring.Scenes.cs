using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Inputs.Haptics;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Interactors.Casters;
using UnityEngine.XR.Interaction.Toolkit.Locomotion;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Movement;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

internal sealed partial class HotelAuthoring
{
    private const string Gameplay = "Assets/Scenes/GameScene.unity";

    // Execute on a clean checkout to regenerate the authored version. Edit saved instances for production changes.
    public static void Generate()
    {
        var author = new HotelAuthoring();
        author.SetLayers();
        author.Prepare();
        author.ModularAssets();
        author.Props();
        author.AuthorGame();
        author.AuthorResults("GameOver", false);
        author.AuthorResults("WinScene", true);
        AssetDatabase.SaveAssets();
        Debug.Log("Hotel Umbral generated and saved.");
    }

    private void SetLayers()
    {
        var tagManager = new SerializedObject(AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        var layers = tagManager.FindProperty("layers");
        foreach (var layer in new[] { (DebrisLayer, "Hotel Debris"), (PropsLayer, "Hotel Props"), (ShellLayer, "Hotel Structure") })
        {
            var entry = layers.GetArrayElementAtIndex(layer.Item1);
            if (!string.IsNullOrEmpty(entry.stringValue) && entry.stringValue != layer.Item2)
                throw new InvalidOperationException($"Layer {layer.Item1} already belongs to {entry.stringValue}.");
            entry.stringValue = layer.Item2;
        }
        tagManager.ApplyModifiedPropertiesWithoutUndo();
    }

    private void AuthorGame()
    {
        Scene scene = EditorSceneManager.OpenScene(Gameplay);
        var rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        var creature = UnityEngine.Object.FindFirstObjectByType<Creature>(FindObjectsInactive.Include);
        var code = UnityEngine.Object.FindFirstObjectByType<GameCodeManager>(FindObjectsInactive.Include);
        var start = UnityEngine.Object.FindFirstObjectByType<CorridorStartTrigger>(FindObjectsInactive.Include);
        var apparition = UnityEngine.Object.FindFirstObjectByType<JumpscareTrigger>(FindObjectsInactive.Include);
        if (rig == null || creature == null || code == null || start == null)
            throw new InvalidOperationException("GameScene is missing the XR rig or an original game system.");

        var priorVisual = creature.visual != null ? creature.visual : creature.GetComponentsInChildren<Animator>(true).FirstOrDefault()?.transform;
        var priorApparition = apparition != null ? apparition.jumpscareObject : null;
        creature.transform.SetParent(null, true);
        start.transform.SetParent(null, true);
        if (apparition != null)
            apparition.transform.SetParent(null, true);
        if (priorApparition != null)
            priorApparition.transform.SetParent(null, true);

        var originalRoots = scene.GetRootGameObjects();
        foreach (var item in originalRoots)
        {
            if (item == rig.gameObject || item == creature.gameObject || item == code.gameObject || item == start.gameObject ||
                (apparition != null && item == apparition.gameObject) || item == priorApparition ||
                item.name is "EventSystem" or "Hands Permissions Manager" ||
                item.GetComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>() != null)
                continue;
            UnityEngine.Object.DestroyImmediate(item);
        }

        var player = SetRig(rig, new Vector3(0f, 0.02f, -19.78f));
        var profile = Node("Hotel Umbral");
        var structure = Node("Arquitectura modular", profile.transform).transform;
        var fade = AddFade(rig.Camera);
        SetAtmosphere();

        Corridor(structure);
        ElevatorSequence elevator = BuildElevator(structure, creature);
        ConfigureTurnOption(elevator.menuCanvas.GetComponent<HotelOptions>(), rig.GetComponent<HotelXrSession>());
        Stage(profile.transform);
        RegisterAmbientApparition(apparition, priorApparition);
        ConfigureCreature(creature, priorVisual, rig, fade);
        BuildExit(structure, code, creature, player, fade);
        BuildClues(code, creature, profile.transform);

        var director = Node("Director de atmósfera").AddComponent<HorrorEventDirector>();
        director.playerHead = player;
        director.beats = beats.ToArray();
        start.transform.SetPositionAndRotation(new Vector3(0f, 0f, -13.0f), Quaternion.identity);
        start.transform.localScale = Vector3.one;
        foreach (var oldTrigger in start.GetComponents<Collider>())
            UnityEngine.Object.DestroyImmediate(oldTrigger);
        start.elevatorSequence = elevator;
        start.playerHead = player;
        start.horrorDirector = director;

        BakeNavigation(structure);
        EditorSceneManager.SaveScene(scene);
    }

    private Transform SetRig(XROrigin rig, Vector3 position)
    {
        rig.transform.SetPositionAndRotation(position, Quaternion.identity);
        rig.transform.localScale = Vector3.one;
        rig.gameObject.tag = "Player";
        var view = rig.Camera;
        if (view == null)
            throw new InvalidOperationException("The XR Origin does not have an assigned camera.");
        view.nearClipPlane = 0.07f;
        view.farClipPlane = 55f;
        view.clearFlags = CameraClearFlags.SolidColor;
        view.backgroundColor = HotelMedia.Hex("12161A");
        foreach (var item in rig.GetComponentsInChildren<Transform>(true))
        {
            if (item.name.IndexOf("Callout", StringComparison.OrdinalIgnoreCase) >= 0 ||
                item.name is "Jump" or "Climb" or "Climb Teleport" or "Grab Move" or "Gaze Interactor")
                item.gameObject.SetActive(false);
        }

        ConfigureXrRig(rig);
        teleporter = rig.GetComponentInChildren<TeleportationProvider>(true);
        foreach (var gravity in rig.GetComponentsInChildren<GravityProvider>(true))
        {
            gravity.sphereCastLayerMask |= 1 << ShellLayer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(gravity);
        }
        foreach (var ray in rig.GetComponentsInChildren<XRRayInteractor>(true))
        {
            ray.raycastMask |= (1 << ShellLayer) | (1 << PropsLayer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(ray);
        }
        foreach (var caster in rig.GetComponentsInChildren<CurveInteractionCaster>(true))
        {
            caster.raycastMask |= (1 << ShellLayer) | (1 << PropsLayer);
            PrefabUtility.RecordPrefabInstancePropertyModifications(caster);
        }
        foreach (var caster in rig.GetComponentsInChildren<SphereInteractionCaster>(true))
        {
            caster.physicsLayerMask |= 1 << PropsLayer;
            PrefabUtility.RecordPrefabInstancePropertyModifications(caster);
        }

        if (!UnityEngine.Object.FindFirstObjectByType<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>())
            Node("XR Interaction Manager").AddComponent<UnityEngine.XR.Interaction.Toolkit.XRInteractionManager>();
        return view.transform;
    }

    internal static HotelXrSession ConfigureXrRig(XROrigin rig)
    {
        rig.RequestedTrackingOriginMode = XROrigin.TrackingOriginMode.NotSpecified;
        rig.CameraYOffset = 1.62f;
        rig.CameraFloorOffsetObject.transform.localPosition = Vector3.zero;
        var desktop = rig.GetComponent<HotelDesktopControls>() ?? rig.gameObject.AddComponent<HotelDesktopControls>();
        desktop.view = rig.Camera;
        desktop.body = rig.GetComponent<CharacterController>();
        desktop.enabled = false;

        var body = desktop.body;
        body.height = 1.62f;
        body.center = Vector3.up * 0.81f;
        body.radius = 0.23f;
        body.skinWidth = 0.025f;
        body.stepOffset = 0.24f;
        body.minMoveDistance = 0f;
        var move = rig.GetComponentInChildren<ContinuousMoveProvider>(true);
        move.moveSpeed = 1.75f;
        var snap = rig.GetComponentInChildren<SnapTurnProvider>(true);
        snap.turnAmount = 30f;
        snap.enableTurnAround = false;
        var smooth = rig.GetComponentInChildren<ContinuousTurnProvider>(true);
        smooth.turnSpeed = 55f;
        smooth.enableTurnAround = false;
        smooth.enabled = false;
        var transformer = rig.GetComponentInChildren<XRBodyTransformer>(true);
        transformer.useCharacterControllerIfExists = true;

        var controllers = rig.GetComponentsInChildren<ControllerInputActionManager>(true);
        var right = controllers.Single(controller => controller.name == "Right Controller");
        foreach (var controller in controllers)
        {
            var serialized = new SerializedObject(controller);
            serialized.FindProperty("m_SmoothMotionEnabled").boolValue = controller != right;
            serialized.FindProperty("m_SmoothTurnEnabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            var ray = controller.GetComponentInChildren<NearFarInteractor>(true);
            ray.allowHoveredActivate = true;
            PrefabUtility.RecordPrefabInstancePropertyModifications(ray);
        }

        foreach (var component in new Component[] { rig, rig.CameraFloorOffsetObject.transform, body, move, snap, smooth, transformer })
            PrefabUtility.RecordPrefabInstancePropertyModifications(component);

        var session = rig.GetComponent<HotelXrSession>() ?? rig.gameObject.AddComponent<HotelXrSession>();
        var settings = new SerializedObject(session);
        settings.FindProperty("locomotion").objectReferenceValue = transformer;
        settings.FindProperty("rightController").objectReferenceValue = right;
        settings.FindProperty("snapTurn").objectReferenceValue = snap;
        settings.FindProperty("smoothTurn").objectReferenceValue = smooth;
        settings.ApplyModifiedPropertiesWithoutUndo();
        return session;
    }

    internal static void ConfigureTurnOption(HotelOptions options, HotelXrSession session)
    {
        options.xrSession = session;
        Transform menu = options.transform;
        var existing = menu.Find("Giro VR");
        if (existing == null)
        {
            var template = options.comfortLabel.GetComponentInParent<HotelButton>().gameObject;
            var clone = UnityEngine.Object.Instantiate(template, menu);
            clone.name = "Giro VR";
            var button = clone.GetComponent<HotelButton>();
            button.onPress = new UnityEngine.Events.UnityEvent();
            UnityEventTools.AddPersistentListener(button.onPress, options.ToggleTurn);
            options.turnLabel = clone.GetComponentInChildren<TMP_Text>();
            options.turnLabel.text = "GIRO  POR PASOS";
            existing = clone.transform;
        }

        existing.localPosition = new Vector3(options.difficultyLabel == null ? 0f : 0.327f, -0.444f, -0.054f);
        if (options.difficultyLabel != null)
            options.difficultyLabel.GetComponentInParent<HotelButton>().transform.localPosition = new Vector3(-0.327f, -0.444f, -0.054f);
        var face = menu.Find("Console face");
        face.localPosition = new Vector3(0f, -0.065f, 0f);
        face.localScale = new Vector3(1.43f, 1f, 1f);
    }

    private void SetAtmosphere()
    {
        RenderSettings.skybox = null;
        RenderSettings.sun = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = HotelMedia.Hex("5D615C");
        RenderSettings.reflectionIntensity = 0f;
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.046f;
        RenderSettings.fogColor = HotelMedia.Hex("1D2628");
    }

    private ElevatorSequence BuildElevator(Transform structure, Creature creature)
    {
        var cabin = Node("Ascensor · cabina y motor", structure).transform;
        float middle = (CabinBack + Entrance) / 2f;
        var floor = Shell("Cabin floor", cabin, new Vector3(0f, 0f, middle), nav: false);
        Walkable(floor, null);
        WallRuns(cabin, -HalfWidth, CabinBack, Entrance, 0f, "Lift cabin panel", false);
        WallRuns(cabin, HalfWidth, CabinBack, Entrance, 180f, "Lift cabin panel", false);
        CrossWall(cabin, -HalfWidth, HalfWidth, CabinBack, -90f, false, "Lift cabin panel");
        var ceiling = Shell("Coffered ceiling", cabin, new Vector3(0f, 0f, middle), nav: false);
        ceiling.transform.localScale = new Vector3(1f, 1f, (Entrance - CabinBack) / FloorBay);
        MarkStatic(Place("Ceiling fixture", cabin, new Vector3(0f, 2.72f, middle)));
        Light light = Lamp(cabin, "Plafón de cabina", new Vector3(0f, 2.44f, middle), "F7D0A3", 2.8f, 5.4f);
        Audio(Node("Corriente de cabina", cabin, new Vector3(0f, 2.08f, middle)), "Cabin", 0.29f, 4f, true, true, true);

        var shaft = Node("Hojas independientes", cabin, new Vector3(0f, 0f, Entrance + 0.181f)).transform;
        Transform left = Place("Lift sliding leaf", shaft, new Vector3(-0.377f, 0f, 0f)).transform;
        Transform right = Place("Lift sliding leaf", shaft, new Vector3(0.377f, 0f, 0f)).transform;
        var controller = cabin.gameObject.AddComponent<ElevatorSequence>();
        controller.leftDoor = left;
        controller.rightDoor = right;
        controller.creature = creature.gameObject;
        controller.travelDuration = 4.4f;
        controller.doorDuration = 2.15f;
        controller.doorDistance = 0.87f;
        controller.cabinLight = light;
        controller.corridorTeleport = teleportAreas.ToArray();
        controller.motorSound = Audio(Node("Motor descentrado", cabin, new Vector3(-0.9f, 2.35f, middle)), "Motor", 0.73f, 6f, true);
        controller.dingSound = Audio(Node("Campana", cabin, new Vector3(0.42f, 2.35f, Entrance - 0.26f)), "Ding", 0.78f, 6f);
        controller.doorSound = Audio(Node("Guías de acero", cabin, new Vector3(0f, 1.85f, Entrance)), "Door", 0.82f, 7f);

        var indicator = Node("Indicador del piso", cabin, new Vector3(0f, 2.455f, Entrance - 0.214f)).transform;
        var display = Place("Phosphor display", indicator, Vector3.zero);
        display.transform.localScale = new Vector3(0.73f, 0.22f, 1f);
        controller.floorDisplay = Label(indicator, "Piso", "04  ·  SERVICIO", new Vector3(0f, 0f, -0.016f), 0.7f, 0.20f, 0.44f, "B6F1CB");

        var menu = Node("Menú en cabina", cabin, new Vector3(0f, 1.51f, Entrance - 0.36f)).transform;
        controller.menuCanvas = menu.gameObject;
        var face = Place("Console face", menu, Vector3.zero);
        face.transform.localScale = new Vector3(1.43f, 0.85f, 1f);
        var lip = Place("Phosphor display", menu, new Vector3(0f, 0.35f, -0.035f));
        lip.transform.localScale = new Vector3(1.33f, 0.014f, 1f);
        Label(menu, "Hotel", "H O T E L   U M B R A L", new Vector3(0f, 0.26f, -0.038f), 1.32f, 0.13f, 0.36f, "D3AA6C");
        Label(menu, "Título", "DON’T LOOK\nBEHIND", new Vector3(0f, 0.065f, -0.04f), 1.34f, 0.28f, 0.90f).fontStyle = FontStyles.Bold;
        var play = Button(menu, "Play", "PLAY  /  JUGAR", new Vector3(0f, -0.156f, -0.054f), 0.64f, 0.133f, "Green enamel", 0.40f);
        UnityEventTools.AddPersistentListener(play.onPress, controller.StartElevator);
        var options = menu.gameObject.AddComponent<HotelOptions>();
        var volume = Button(menu, "Audio", "AUDIO  100%", new Vector3(-0.327f, -0.306f, -0.054f), 0.58f, 0.117f, "Brass patina", 0.32f);
        var comfort = Button(menu, "Comodidad VR", "SUSTOS  NORMAL", new Vector3(0.327f, -0.306f, -0.054f), 0.58f, 0.117f, "Brass patina", 0.32f);
        options.volumeLabel = volume.GetComponentInChildren<TMP_Text>();
        options.comfortLabel = comfort.GetComponentInChildren<TMP_Text>();
        UnityEventTools.AddPersistentListener(volume.onPress, options.CycleVolume);
        UnityEventTools.AddPersistentListener(comfort.onPress, options.ToggleComfort);
        return controller;
    }

    private HotelFade AddFade(Camera camera)
    {
        foreach (var previous in camera.GetComponentsInChildren<HotelFade>(true))
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        var shell = Place("Comfort screen", camera.transform, new Vector3(0f, 0f, 0.16f));
        var fade = shell.AddComponent<HotelFade>();
        fade.surface = shell.GetComponent<MeshRenderer>();
        fade.surface.enabled = false;
        return fade;
    }

    private void ConfigureCreature(Creature creature, Transform preservedVisual, XROrigin rig, HotelFade fade)
    {
        creature.gameObject.SetActive(true);
        foreach (var collider in creature.GetComponents<Collider>())
            UnityEngine.Object.DestroyImmediate(collider);
        foreach (var source in creature.GetComponents<AudioSource>())
            UnityEngine.Object.DestroyImmediate(source);
        foreach (var editorProxy in creature.GetComponents<Renderer>())
            UnityEngine.Object.DestroyImmediate(editorProxy);
        foreach (var oldAudio in creature.GetComponentsInChildren<AudioSource>(true))
        {
            if (preservedVisual == null || !oldAudio.transform.IsChildOf(preservedVisual))
                UnityEngine.Object.DestroyImmediate(oldAudio.gameObject);
        }
        foreach (var oldLight in creature.GetComponentsInChildren<Light>(true))
            UnityEngine.Object.DestroyImmediate(oldLight.gameObject);

        creature.transform.SetPositionAndRotation(new Vector3(0.25f, 0.035f, -16.62f), Quaternion.identity);
        if (!creature.TryGetComponent(out NavMeshAgent agent))
            agent = creature.gameObject.AddComponent<NavMeshAgent>();
        agent.radius = 0.28f;
        agent.height = 1.75f;
        agent.speed = 1.05f;
        agent.acceleration = 3.7f;
        agent.stoppingDistance = 0.22f;
        agent.updateRotation = false;

        if (preservedVisual != null)
        {
            preservedVisual.localPosition = new Vector3(0f, 0f, 0f);
            preservedVisual.localScale = Vector3.one * 1.58f;
            preservedVisual.localRotation = Quaternion.identity;
            RestoreGhostCostume(preservedVisual);
        }
        creature.visual = preservedVisual;
        creature.animationBody = preservedVisual != null ? preservedVisual.GetComponentInChildren<Animator>(true) : null;
        if (creature.animationBody != null)
            creature.animationBody.applyRootMotion = false;
        creature.player = rig.transform;
        creature.playerCamera = rig.Camera.transform;
        creature.architecture = 1 << ShellLayer;
        creature.agent = agent;
        creature.speed = 0.97f;
        creature.retreatSpeed = 2.35f;
        creature.gameOverDistance = 0.86f;
        creature.lookThreshold = 0.74f;
        creature.initialGrace = 3.3f;
        creature.maxLookTime = 3f;
        creature.maxTurns = 3;
        creature.footstepAudio = Audio(Node("Pasos de la criatura", creature.transform, new Vector3(0f, 0.11f, 0f)), "Step", 0.9f, 13f);
        creature.breathAudio = Audio(Node("Respiración de la criatura", creature.transform, new Vector3(0f, 1.42f, 0f)), "Breath", 0.55f, 7f, true);

        if (!creature.TryGetComponent(out CreatureAttackSequence capture))
            capture = creature.gameObject.AddComponent<CreatureAttackSequence>();
        capture.fade = fade;
        capture.haptics = rig.GetComponentsInChildren<HapticImpulsePlayer>(true);
        capture.attackSound = Audio(Node("Golpe de captura", creature.transform, new Vector3(0f, 1.15f, 0f)), "Attack", 0.9f, 6f);
        capture.tailSound = Audio(Node("Cola de captura", creature.transform, new Vector3(0f, 0.69f, 0f)), "Aftershock", 0.55f, 0f);
        capture.attackLight = Lamp(creature.transform, "Luz de captura", new Vector3(0f, 1.77f, 0f), "DE503C", 0f, 3f);
        creature.capture = capture;
        creature.gameObject.SetActive(false);
    }

    private void RestoreGhostCostume(Transform model)
    {
        foreach (var renderable in model.GetComponentsInChildren<Renderer>(true))
        {
            var fitted = renderable.sharedMaterials;
            for (int i = 0; i < fitted.Length; i++)
            {
                switch (fitted[i] != null ? fitted[i].name : "")
                {
                    case "Ghost_Dress":
                    case "elbise":
                    case "Waterlogged dress":
                        fitted[i] = materials["Waterlogged dress"];
                        break;
                    case "Ghost_Head":
                    case "Pallid skin":
                        fitted[i] = materials["Pallid skin"];
                        break;
                    case "Ghost_Hair":
                    case "Damp hair":
                        fitted[i] = materials["Damp hair"];
                        break;
                }
            }
            renderable.sharedMaterials = fitted;
            if (renderable.name == "elbise")
                renderable.enabled = true;
        }
    }

    private void RegisterAmbientApparition(JumpscareTrigger trigger, GameObject apparition)
    {
        if (trigger == null || apparition == null)
            return;
        trigger.transform.SetPositionAndRotation(new Vector3(-0.68f, 0.01f, 1.35f), Quaternion.identity);
        trigger.transform.localScale = Vector3.one;
        foreach (var collider in trigger.GetComponents<Collider>())
            UnityEngine.Object.DestroyImmediate(collider);
        foreach (var previous in trigger.GetComponents<AudioSource>())
            UnityEngine.Object.DestroyImmediate(previous);
        apparition.transform.SetPositionAndRotation(new Vector3(-0.72f, 0.05f, 1.27f), Quaternion.identity);
        trigger.jumpscareObject = apparition;
        apparition.SetActive(false);
        trigger.jumpscareAudio = Audio(trigger.gameObject, "Whisper", 0.67f, 8f);
        beats.Add(new HorrorEventDirector.Beat { effect = HorrorEventDirector.Effect.Apparition, marker = trigger.transform, ambientScare = trigger });
    }

    private void BuildExit(Transform structure, GameCodeManager code, Creature creature, Transform player, HotelFade fade)
    {
        var exit = Node("Salida controlada", structure).transform;
        var gate = Place("Fire shutter", exit, new Vector3(0f, 0f, Gate - 0.18f));
        gate.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.On;
        var door = gate.AddComponent<DoorController>();
        door.openHeight = 2.35f;
        door.openSpeed = 1.5f;
        door.motor = Audio(Node("Motor de la persiana", exit, new Vector3(0f, 2.19f, Gate)), "Door", 0.65f, 9f, true);
        var crossing = Node("Umbral de victoria", exit, new Vector3(0f, 0f, Gate + 0.65f));
        var win = crossing.AddComponent<WinTrigger>();
        win.playerHead = player;
        win.creature = creature;
        win.fade = fade;
        win.release = Audio(crossing, "Release", 0.55f, 7f);
        win.exitTeleport = WalkableExit;
        door.winTrigger = crossing;
        WalkableExit.enabled = false;
        crossing.SetActive(false);

        Shell("Coffered ceiling", exit, new Vector3(0f, 0f, Gate + 1.25f), nav: false);
        var emergency = Lamp(exit, "Derivación de emergencia", new Vector3(0f, 2.15f, Gate + 1.35f), "F76D50", 2.4f, 5.2f);
        emergency.shadows = LightShadows.Hard;
        emergency.shadowStrength = 0.65f;

        var console = Node("Teclado de seguridad", exit, new Vector3(1.145f, 1.32f, Gate - 0.37f)).transform;
        var frame = Place("Console face", console, Vector3.zero);
        frame.transform.localScale = new Vector3(0.55f, 0.74f, 1f);
        var keypad = console.gameObject.AddComponent<Keypad>();
        keypad.gameCodeManager = code;
        keypad.door = door;
        keypad.maxDigits = 4;
        keypad.response = Audio(console.gameObject, "Ding", 0.72f, 7f);
        keypad.rejected = sounds["Reject"];
        keypad.accepted = sounds["Ding"];
        Label(console, "Sistema", "U M B R A L  /  S 0 1", new Vector3(0f, 0.314f, -0.037f), 0.49f, 0.05f, 0.19f);
        var display = Place("Phosphor display", console, new Vector3(0f, 0.229f, -0.044f));
        display.transform.localScale = new Vector3(0.465f, 0.125f, 1f);
        keypad.codeDisplay = Label(console, "Pantalla", "····", new Vector3(0f, 0.229f, -0.06f), 0.435f, 0.11f, 0.37f, "B1EFCD");
        for (int i = 0; i < 12; i++)
        {
            int row = i / 3, column = i % 3;
            string digit = i == 10 ? "0" : (i + 1).ToString();
            string caption = i == 9 ? "CLR" : i == 11 ? "OK" : digit;
            var button = Button(console, caption, caption, new Vector3((column - 1) * 0.151f, 0.101f - row * 0.12f, -0.054f),
                0.122f, 0.096f, i == 9 ? "Safety red" : i == 11 ? "Green enamel" : "Brass patina", 0.27f);
            if (i == 9)
                UnityEventTools.AddPersistentListener(button.onPress, keypad.ClearCode);
            else if (i == 11)
                UnityEventTools.AddPersistentListener(button.onPress, keypad.SubmitCode);
            else
                UnityEventTools.AddStringPersistentListener(button.onPress, keypad.PressNumber, digit);
        }
    }

    private void BuildClues(GameCodeManager code, Creature creature, Transform environment)
    {
        var archive = Node("Registro de seguridad · pistas", environment).transform;
        var briefing = WallArt("Bronze plaque", archive, -1f, 1.59f, -15.6f);
        briefing.localScale = new Vector3(0.99f, 0.72f, 1f);
        Label(briefing, "Orden del código", "REGISTRO DE EVACUACIÓN\nLee los sellos del 01 al 04.\nDos están en las habitaciones.", new Vector3(0f, 0.05f, -0.069f), 0.92f, 0.44f, 0.337f);
        Label(briefing, "Advertencia", "Bastan tres miradas. Hazlas contar.", new Vector3(0f, -0.324f, -0.069f), 0.90f, 0.10f, 0.26f, "D69274");

        var clueTemplate = Node("Dígito estampado");
        Label(clueTemplate.transform, "Número", "0", Vector3.zero, 0.38f, 0.34f, 0.90f, "F2BE8E");
        code.bloodNumberPrefab = SavePrefab("Stamped clue digit", clueTemplate);
        code.codeLength = 4;

        var points = new (float x, float z, float yaw, string label)[]
        {
            (1.414f, -13.55f, 90f, "01  ·  RECEPCIÓN"),
            (-4.32f, -8.52f, -90f, "02  ·  HAB. 104"),
            (4.32f, 4.23f, 90f, "03  ·  HAB. 107"),
            (1.405f, 11.79f, 90f, "04  ·  SERVICIO")
        };
        code.numberSpawnPoints = new Transform[points.Length];
        for (int i = 0; i < points.Length; i++)
        {
            var point = points[i];
            var plaqueRoot = Node(point.label, archive, new Vector3(point.x, 1.53f, point.z), point.yaw).transform;
            Place("Bronze plaque", plaqueRoot, Vector3.zero).transform.localScale = new Vector3(0.47f, 0.53f, 1f);
            Label(plaqueRoot, "Identificador", point.label, new Vector3(0f, 0.156f, -0.071f), 0.42f, 0.079f, 0.32f, "DBBC87");
            code.numberSpawnPoints[i] = Node("Anclaje del dígito", plaqueRoot, new Vector3(0f, -0.047f, -0.067f)).transform;
        }

        foreach (float z in new[] { -11.85f, -0.55f, 9.05f, 16.80f })
        {
            var meter = WallArt("Bronze plaque", archive, -1f, 1.51f, z);
            meter.localScale = new Vector3(0.66f, 0.15f, 1f);
            var label = Label(meter, "Presupuesto de miradas", "MIRADAS   3/3", new Vector3(0f, 0f, -0.070f), 0.94f, 0.9f, 0.78f);
            label.transform.localScale = new Vector3(1f, 3.3f, 1f);
            var status = meter.gameObject.AddComponent<LookBudgetDisplay>();
            status.creature = creature;
            status.label = label;
        }
    }

    private void BakeNavigation(Transform layout)
    {
        var navigationComponent = layout.gameObject.AddComponent<HotelNavigation>();
        navigationComponent.staticCollision = navigation.ToArray();
        UpdateNavigation(navigationComponent);
    }

    [MenuItem("Hotel Umbral/Rebake Gallery Navigation")]
    private static void RebakeNavigation()
    {
        var selected = UnityEngine.Object.FindFirstObjectByType<HotelNavigation>();
        if (selected == null)
            throw new InvalidOperationException("Open GameScene before rebaking hotel navigation.");
        UpdateNavigation(selected);
        EditorSceneManager.MarkSceneDirty(selected.gameObject.scene);
        AssetDatabase.SaveAssets();
    }

    private static void UpdateNavigation(HotelNavigation navigationComponent)
    {
        var sources = new List<NavMeshBuildSource>();
        var bounds = new Bounds();
        foreach (var collider in navigationComponent.staticCollision)
        {
            if (collider == null)
                continue;
            Vector3 scale = collider.transform.lossyScale;
            if (sources.Count == 0)
                bounds = collider.bounds;
            else
                bounds.Encapsulate(collider.bounds);
            sources.Add(new NavMeshBuildSource
            {
                shape = NavMeshBuildSourceShape.Box,
                transform = Matrix4x4.TRS(collider.transform.TransformPoint(collider.center), collider.transform.rotation, Vector3.one),
                size = Vector3.Scale(collider.size, scale),
                area = 0
            });
        }
        var settings = NavMesh.GetSettingsByIndex(0);
        settings.agentRadius = 0.30f;
        settings.agentHeight = 1.72f;
        settings.overrideVoxelSize = true;
        settings.voxelSize = 0.075f;
        if (sources.Count == 0)
            throw new InvalidOperationException("Assign the static collision volumes before rebaking navigation.");
        bounds.Expand(new Vector3(2f, 2f, 2f));
        string path = Root + "/Gallery navigation.asset";
        var mesh = AssetDatabase.LoadAssetAtPath<NavMeshData>(path);
        if (mesh == null)
        {
            mesh = UnityEngine.AI.NavMeshBuilder.BuildNavMeshData(settings, sources, bounds, Vector3.zero, Quaternion.identity);
            if (mesh == null)
                throw new InvalidOperationException("NavMesh build failed.");
            AssetDatabase.CreateAsset(mesh, path);
        }
        else if (!UnityEngine.AI.NavMeshBuilder.UpdateNavMeshData(mesh, settings, sources, bounds))
        {
            throw new InvalidOperationException("NavMesh update failed.");
        }
        navigationComponent.layout = mesh;
        EditorUtility.SetDirty(mesh);
    }

    private void AuthorResults(string name, bool win)
    {
        Scene scene = EditorSceneManager.OpenScene($"Assets/Scenes/{name}.unity");
        var rig = UnityEngine.Object.FindFirstObjectByType<XROrigin>(FindObjectsInactive.Include);
        var winMenu = UnityEngine.Object.FindFirstObjectByType<WinMenu>(FindObjectsInactive.Include);
        var overMenu = UnityEngine.Object.FindFirstObjectByType<GameOverMenu>(FindObjectsInactive.Include);
        foreach (var item in scene.GetRootGameObjects())
        {
            if (item == rig.gameObject || (winMenu != null && item == winMenu.gameObject) ||
                (overMenu != null && item == overMenu.gameObject) || item.name == "EventSystem")
                continue;
            UnityEngine.Object.DestroyImmediate(item);
        }

        SetRig(rig, new Vector3(0f, 0.02f, -1.35f));
        SetAtmosphere();
        var environment = Node(win ? "Vestíbulo al amanecer" : "Ascensor detenido").transform;
        Shell("Cabin floor", environment, new Vector3(0f, 0f, -0.65f), nav: false);
        Shell("Coffered ceiling", environment, new Vector3(0f, 0f, -0.58f), nav: false);
        CrossWall(environment, -HalfWidth, HalfWidth, 0.88f, 90f, false);
        CrossWall(environment, -HalfWidth, HalfWidth, -2.29f, -90f, false);
        WallRuns(environment, -HalfWidth, -2.28f, 0.88f, 0f);
        WallRuns(environment, HalfWidth, -2.28f, 0.88f, 180f);
        Place("Ceiling fixture", environment, new Vector3(0f, 2.71f, -0.45f));
        Lamp(environment, "Luz del vestíbulo", new Vector3(0f, 2.36f, -0.45f), win ? "A6CEBC" : "EC8D72", 2.8f, 5f);
        if (!win)
            Audio(Node("Cañerías", environment, new Vector3(1.1f, 2.2f, -0.3f)), "Pipes", 0.16f, 4f, true, true, true);

        var panel = Node("Panel de resultado", environment, new Vector3(0f, 1.50f, 0.49f)).transform;
        Place("Bronze plaque", panel, Vector3.zero).transform.localScale = new Vector3(1.58f, 0.85f, 1f);
        Label(panel, "Resultado", win ? "HAS ESCAPADO" : "TE ENCONTRÓ", new Vector3(0f, 0.131f, -0.062f), 1.48f, 0.19f, 0.69f,
            win ? "BFE8CD" : "FEAC85");
        Label(panel, "Cierre", win ? "La puerta quedó atrás." : "El Hotel Umbral sigue abierto.", new Vector3(0f, -0.014f, -0.064f), 1.35f, 0.12f, 0.34f);
        var retry = Button(panel, "Reiniciar", "VOLVER AL ASCENSOR", new Vector3(0f, -0.225f, -0.088f), 0.86f, 0.15f, "Green enamel", 0.36f);
        if (win)
            UnityEventTools.AddPersistentListener(retry.onPress, winMenu.PlayAgain);
        else
            UnityEventTools.AddPersistentListener(retry.onPress, overMenu.Retry);
        EditorSceneManager.SaveScene(scene);
    }

    public static void BuildWindows()
    {
        const string folder = "Builds/HotelUmbral";
        Directory.CreateDirectory(folder);
        var target = NamedBuildTarget.Standalone;
        ScriptingImplementation previous = PlayerSettings.GetScriptingBackend(target);
        try
        {
            PlayerSettings.SetScriptingBackend(target, ScriptingImplementation.Mono2x);
            var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                target = BuildTarget.StandaloneWindows64,
                locationPathName = folder + "/Dont Look Behind.exe",
                options = BuildOptions.None
            });
            if (result.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Build failed: {result.summary.result}, {result.summary.totalErrors} errors.");
            Debug.Log($"HOTEL WINDOWS BUILD PASS · {result.summary.totalSize} bytes, {result.summary.totalWarnings} warnings.");
        }
        finally
        {
            PlayerSettings.SetScriptingBackend(target, previous);
        }
    }
}
