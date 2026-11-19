using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.EditorCoroutines.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Gravity;

[InitializeOnLoad]
internal static class HotelSmoke
{
    private const string Enabled = "HotelSmoke.enabled";
    private const string Baseline = "Assets/Scenes/GameScene.unity";

    static HotelSmoke()
    {
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredPlayMode && SessionState.GetBool(Enabled, false))
                EditorCoroutineUtility.StartCoroutineOwnerless(RunSafely());
        };
    }

    public static void Run()
    {
        SessionState.SetBool(Enabled, true);
        EditorSceneManager.OpenScene(Baseline);
        var errors = new List<string>();
        foreach (var scene in EditorBuildSettings.scenes.Where(scene => scene.enabled))
        {
            var loaded = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            foreach (var root in loaded.GetRootGameObjects())
            foreach (var item in root.GetComponentsInChildren<Transform>(true))
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(item.gameObject) != 0)
                    errors.Add($"{scene.path}: {item.name}");
        }
        if (errors.Count != 0)
            throw new InvalidOperationException("Missing scripts: " + string.Join(", ", errors));
        Debug.Log("HOTEL SMOKE · 3 build scenes, no missing MonoBehaviours.");
        EditorSceneManager.OpenScene(Baseline);
        EditorApplication.EnterPlaymode();
    }

    private static IEnumerator RunSafely()
    {
        string runtimeError = null;
        void Watch(string condition, string stack, LogType type)
        {
            if (stack.Contains("UnityEditor.Search.SearchDatabase", StringComparison.Ordinal))
                return;
            if (type is LogType.Exception or LogType.Assert or LogType.Error)
                runtimeError ??= condition;
        }
        Application.logMessageReceived += Watch;
        IEnumerator tests = Exercise();
        while (true)
        {
            object result;
            try
            {
                if (failure != null)
                    throw failure;
                if (runtimeError != null)
                    throw new InvalidOperationException($"Runtime error: {runtimeError}");
                if (!tests.MoveNext())
                    break;
                result = tests.Current;
            }
            catch (Exception error)
            {
                Application.logMessageReceived -= Watch;
                Debug.LogException(error);
                SessionState.EraseBool(Enabled);
                EditorApplication.Exit(1);
                yield break;
            }
            yield return result;
        }
        Application.logMessageReceived -= Watch;
        Debug.Log("HOTEL SMOKE PASS · elevator, rooms, clues, real collision and debris, sound, gaze, keypad, victory, capture, restart.");
        SessionState.EraseBool(Enabled);
        EditorApplication.Exit(0);
    }

    private static IEnumerator Exercise()
    {
        Time.timeScale = 3f;
        yield return null;
        var elevator = Find<ElevatorSequence>();
        var manager = Find<GameCodeManager>();
        var keypad = Find<Keypad>();
        var creature = Find<Creature>(true);
        var desktop = Find<HotelDesktopControls>();
        var head = desktop.view.transform;
        int structureLayer = 1 << LayerMask.NameToLayer("Hotel Structure");
        int propsLayer = 1 << LayerMask.NameToLayer("Hotel Props");
        var gravity = desktop.GetComponentInChildren<GravityProvider>(true);
        var rays = desktop.GetComponentsInChildren<XRRayInteractor>(true);
        Check(gravity != null && (gravity.sphereCastLayerMask.value & structureLayer) != 0 && rays.Length > 0 &&
            rays.All(ray => (ray.raycastMask.value & (structureLayer | propsLayer)) == (structureLayer | propsLayer)),
            "XR gravity and rays include the hotel collision layers.");
        float startZ = head.position.z;
        Vector3 left = elevator.leftDoor.localPosition, right = elevator.rightDoor.localPosition;
        Check(startZ < -18f && !creature.gameObject.activeSelf, "Start in a closed cabin with creature dormant.");
        Check(!elevator.corridorTeleport.Any(area => area.enabled), "Corridor teleport locked.");
        Check(Physics.Linecast(new Vector3(0f, 1.2f, -19f), new Vector3(0f, 1.2f, -16f), creature.architecture), "Lift doors block traversal.");
        Check(manager.GeneratedCode is { Length: 4 } answer && answer.All(value => value >= '1' && value <= '9') &&
            string.Concat(manager.InstalledClues.Select(clue => clue.text)) == answer, "Four ordered clues match a valid answer.");
        Capture("01-Cabina", new Vector3(0f, 1.62f, -19.79f), Quaternion.identity);
        var probe = new GameObject("XR interaction smoke probe");
        probe.transform.position = new Vector3(0f, 1.3f, -18.1f);
        probe.AddComponent<SphereCollider>().isTrigger = true;
        var hand = probe.AddComponent<XRDirectInteractor>();
        yield return null;
        hand.StartManualInteraction((IXRSelectInteractable)GameObject.Find("Play").GetComponent<XRSimpleInteractable>());
        if (hand.isPerformingManualInteraction)
            hand.EndManualInteraction();
        yield return Await(() => !elevator.menuCanvas.activeSelf, 2f);
        Check(true, "XRI selection of Play hides the cabin menu.");
        yield return Await(() => elevator.DoorsOpen, 11f);
        Check(elevator.leftDoor.localPosition.x < left.x - 0.65f && elevator.rightDoor.localPosition.x > right.x + 0.65f &&
            Mathf.Abs(head.position.z - startZ) < 0.02f && !creature.gameObject.activeSelf, "Ride opens both leaves without moving the player.");
        Check(!Physics.Linecast(new Vector3(0f, 1.2f, -19f), new Vector3(0f, 1.2f, -16f), creature.architecture) &&
            elevator.corridorTeleport.All(area => area.enabled), "Open lift leads into the playable gallery.");
        Check(PathExists(new Vector3(0f, 0.04f, -16.6f), new Vector3(-3.55f, 0.04f, -8.6f)) &&
            PathExists(new Vector3(0f, 0.04f, -11f), new Vector3(3.55f, 0.04f, 3.85f)), "Baked navigation reaches both guest rooms.");
        Capture("02-Pasillo", new Vector3(0f, 1.60f, -15.8f), Quaternion.Euler(-1f, 0f, 0f));
        Capture("03-Registro", new Vector3(-2.18f, 1.60f, -8.65f), Quaternion.Euler(8f, -90f, 0f));
        Capture("04-Salida", new Vector3(0.40f, 1.57f, 15.8f), Quaternion.Euler(2f, 12f, 0f));

        var key = GameObject.Find("Service key").GetComponent<XRGrabInteractable>();
        probe.transform.position = key.transform.position;
        hand.StartManualInteraction((IXRSelectInteractable)key);
        Check(key.isSelected, "XRI selects the service key as a grab interactable.");
        hand.EndManualInteraction();
        Check(!key.isSelected, "XRI releases the service key.");
        UnityEngine.Object.Destroy(probe);

        var variants = UnityEngine.Object.FindObjectsByType<BreakableObject>(FindObjectsSortMode.None)
            .GroupBy(prop => prop.fracturePrefab).Select(group => group.First()).ToArray();
        Check(variants.Length >= 5, "All five breakable variants have distinct fracture prefabs.");
        int reported = 0;
        void CountNoise(Vector3 point, float radius) => reported++;
        HotelNoise.Heard += CountNoise;
        foreach (var prop in variants)
        {
            var body = prop.GetComponent<Rigidbody>();
            body.position = new Vector3(0.5f, 1.4f, -12f + reported * 2f);
            body.linearVelocity = Vector3.down * 6f;
            body.WakeUp();
            Debug.Log("HOTEL SMOKE · Impact: " + prop.name);
            yield return Await(() => prop == null, 4f);
        }
        HotelNoise.Heard -= CountNoise;
        Check(reported == variants.Length, "Physical impact broke each prop and emitted one noise.");
        Check(UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Any(body => body.gameObject.layer == 8), "Physical debris spawned.");
        yield return Await(() => !UnityEngine.Object.FindObjectsByType<Rigidbody>(FindObjectsSortMode.None).Any(body => body.gameObject.layer == 8), 11f);
        Check(true, "Debris expired.");

        MovePlayer(desktop, new Vector3(0f, 0.02f, -12.6f), 0f);
        yield return Await(() => creature.gameObject.activeSelf, 1f);
        yield return Await(() => creature.State != Creature.BehaviourState.Waiting, 5f);
        Check(creature.agent.isOnNavMesh && creature.TurnsRemaining == 3, "Corridor activates navigation and exactly three uses.");
        Vector3 hidden = new(-3.47f, 0.02f, -9.65f);
        Vector3 projected = creature.transform.position - hidden;
        MovePlayer(desktop, hidden, Mathf.Atan2(projected.x, projected.z) * Mathf.Rad2Deg);
        Check(Physics.Linecast(head.position, creature.transform.position + Vector3.up * 1.39f, creature.architecture),
            "A wall occludes the creature from the guest room.");
        float hiddenUntil = Time.time + 0.36f;
        yield return Await(() => Time.time >= hiddenUntil, 1.5f);
        Check(creature.TurnsRemaining == 3, "Looking through a wall does not consume a gaze.");
        MovePlayer(desktop, new Vector3(0f, 0.02f, -12.6f), 0f);
        HotelNoise.Emit(new Vector3(0.8f, 0f, -15f), 9f);
        yield return Await(() => creature.State == Creature.BehaviourState.Investigating, 1f);
        Check(true, "Nearby noise produces investigation.");
        for (int count = 0; count < 3; count++)
        {
            MovePlayer(desktop, new Vector3(0f, 0.02f, -12.6f), 180f);
            yield return Await(() => creature.Repelling, 1.6f);
            Check(creature.TurnsRemaining == 2 - count, $"Gaze {count + 1} consumes exactly one use.");
            if (count == 0)
                Capture("05-Criatura", desktop.view.transform.position, desktop.view.transform.rotation);
            float hold = Time.time + 0.32f;
            yield return Await(() => Time.time >= hold, 1f);
            Check(creature.TurnsRemaining == 2 - count, "A continuous gaze does not spend another use.");
            MovePlayer(desktop, new Vector3(0f, 0.02f, -12.6f), 0f);
            float turn = Time.time + 0.33f;
            yield return Await(() => Time.time >= turn, 1f);
        }
        MovePlayer(desktop, new Vector3(0f, 0.02f, -12.6f), 180f);
        float fourth = Time.time + 0.40f;
        yield return Await(() => Time.time >= fourth, 1f);
        Check(!creature.Repelling && creature.TurnsRemaining == 0, "Fourth gaze has no effect.");

        MovePlayer(desktop, new Vector3(0.57f, 0.02f, 16.62f), 0f);
        Vector3 approach = new(0f, 1f, 17f), beyond = new(0f, 1f, 18.7f);
        Check(Physics.Linecast(approach, beyond, creature.architecture), "The locked exit has physical collision.");
        Press(keypad, "1");
        Press(keypad, "OK");
        Check(keypad.codeDisplay.text == "DENEGADO" && !keypad.door.IsOpen, "Wrong entry rejected.");
        yield return Await(() => keypad.codeDisplay.text == "····", 2.8f);
        foreach (char digit in manager.GeneratedCode)
        {
            Press(keypad, digit.ToString());
            float wait = Time.realtimeSinceStartup + 0.17f;
            yield return Await(() => Time.realtimeSinceStartup >= wait, 1f);
        }
        Press(keypad, "OK");
        Check(keypad.Accepted && !keypad.door.winTrigger.activeSelf, "Correct code starts opening before enabling victory.");
        yield return Await(() => keypad.door.IsOpen && keypad.door.winTrigger.activeSelf, 3.5f);
        Check(!Physics.Linecast(approach, beyond, creature.architecture), "The open exit collider clears the threshold.");
        MovePlayer(desktop, new Vector3(0f, 0.02f, 18.75f), 0f);
        yield return Await(() => SceneManager.GetActiveScene().name == "WinScene", 5f);
        yield return null;
        Check(true, "Crossing an open gate loads WinScene.");
        GameObject.Find("Reiniciar").GetComponent<HotelButton>().Press();
        yield return Await(() => SceneManager.GetActiveScene().name == "GameScene", 5f);
        yield return null;

        elevator = Find<ElevatorSequence>();
        creature = Find<Creature>(true);
        desktop = Find<HotelDesktopControls>();
        Check(!creature.gameObject.activeSelf && elevator.menuCanvas.activeSelf, "Restart resets the intro and threat.");
        GameObject.Find("Play").GetComponent<HotelButton>().Press();
        yield return Await(() => elevator.DoorsOpen, 10f);
        MovePlayer(desktop, new Vector3(0f, 0.02f, -12.8f), 0f);
        yield return Await(() => creature.gameObject.activeSelf && creature.agent.isOnNavMesh, 1f);
        creature.agent.Warp(new Vector3(0f, 0.02f, -13.41f));
        yield return Await(() => creature.State == Creature.BehaviourState.Attacking, 5f);
        Check(SceneManager.GetActiveScene().name == "GameScene", "Capture starts inside the gameplay scene.");
        yield return Await(() => creature.capture.fade.surface.enabled, 4f);
        yield return Await(() => SceneManager.GetActiveScene().name == "GameOver", 5f);
        yield return null;
        Check(true, "Capture fades to GameOver.");
        GameObject.Find("Reiniciar").GetComponent<HotelButton>().Press();
        yield return Await(() => SceneManager.GetActiveScene().name == "GameScene", 5f);
        Check(true, "GameOver restarts the elevator.");
        Time.timeScale = 1f;
    }

    private static IEnumerator Await(Func<bool> exit, float limit)
    {
        float until = Time.realtimeSinceStartup + limit;
        while (!exit())
        {
            if (Time.realtimeSinceStartup > until)
            {
                // Resume the runner so that it can report the failure and return a nonzero exit code.
                failure = new TimeoutException("Did not reach the next hotel smoke checkpoint.");
                yield break;
            }
            yield return null;
        }
    }

    private static Exception failure;

    private static T Find<T>(bool inactive = false) where T : UnityEngine.Object =>
        UnityEngine.Object.FindFirstObjectByType<T>(inactive ? FindObjectsInactive.Include : FindObjectsInactive.Exclude);

    private static void MovePlayer(HotelDesktopControls desktop, Vector3 position, float yaw)
    {
        desktop.body.enabled = false;
        desktop.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        desktop.view.transform.localRotation = Quaternion.identity;
        desktop.body.enabled = true;
        Physics.SyncTransforms();
    }

    private static bool PathExists(Vector3 start, Vector3 finish)
    {
        var path = new NavMeshPath();
        return NavMesh.CalculatePath(start, finish, NavMesh.AllAreas, path) && path.status == NavMeshPathStatus.PathComplete;
    }

    private static void Press(Keypad keypad, string button)
    {
        var match = keypad.GetComponentsInChildren<HotelButton>().Single(item => item.gameObject.name == button);
        match.Press();
    }

    private static void Capture(string name, Vector3 position, Quaternion rotation)
    {
        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            return;
        const int width = 1280, height = 720;
        var viewpoint = new GameObject("Temporary preview camera");
        try
        {
            var camera = viewpoint.AddComponent<Camera>();
            camera.CopyFrom(Find<HotelDesktopControls>().view);
            camera.enabled = false;
            camera.transform.SetPositionAndRotation(position, rotation);
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32);
            var request = new UniversalRenderPipeline.SingleCameraRequest { destination = target };
            RenderPipeline.SubmitRenderRequest(camera, request);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var output = new Texture2D(width, height, TextureFormat.RGB24, false);
            output.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            output.Apply();
            Directory.CreateDirectory("Builds/Preview");
            File.WriteAllBytes($"Builds/Preview/{name}.png", output.EncodeToPNG());
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            UnityEngine.Object.Destroy(output);
        }
        finally
        {
            UnityEngine.Object.Destroy(viewpoint);
        }
    }

    private static void Check(bool test, string message)
    {
        if (failure != null)
            throw failure;
        if (!test)
            throw new InvalidOperationException(message);
        Debug.Log("HOTEL SMOKE · " + message);
    }
}
