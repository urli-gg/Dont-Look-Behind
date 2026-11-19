using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;

internal sealed partial class NightmareAuthoring
{
    private const string Folder = "Assets/HotelUmbral/Nightmare";
    private const string Sounds = Folder + "/Sounds";
    private const string MixerPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/MainAudioMixer.mixer";
    private const string Gameplay = "Assets/Scenes/GameScene.unity";

    private Transform stage;
    private HorrorTensionDirector tension;
    private NightmareLighting lighting;
    private NightmareSound soundscape;
    private Transform head;
    private AudioMixerGroup effects;
    private AudioMixerGroup ambience;
    private Material glassOff;
    private Material glassCold;
    private Material glassEmergency;
    private Material guidance;
    private readonly Dictionary<string, AudioClip> clips = new();
    private readonly List<NightmareLighting.Fixture> fixtures = new();

    [MenuItem("Hotel Umbral/Apply Nightmare Update")]
    public static void Upgrade()
    {
        var author = new NightmareAuthoring();
        author.Prepare();
        author.ApplyScene();
        AssetDatabase.SaveAssets();
        Debug.Log("NIGHTMARE AUTHORING PASS · Existing GameScene updated.");
    }

    private void Prepare()
    {
        Directory.CreateDirectory(Folder);
        Directory.CreateDirectory(Sounds);
        AssetDatabase.Refresh();

        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        effects = mixer.FindMatchingGroups("SFX").Single();
        ambience = mixer.FindMatchingGroups("MX").Single();

        var names = new (string name, float duration)[]
        {
            ("EmptyFloor", 8f), ("PursuitPulse", 5.5f), ("WoodUnderfoot", 0.34f), ("DistantTread", 2.2f),
            ("WireFail", 1.10f), ("HushLeft", 1.85f), ("HingeSlam", 1.32f), ("SealTouch", 0.68f),
            ("LastSeal", 1.75f), ("FinalRattle", 1.95f), ("PursuitBreath", 1.25f), ("CaptureImpact", 1.08f),
            ("KeypadRelay", 0.12f)
        };
        for (int i = 0; i < names.Length; i++)
            clips.Add(names[i].name, HotelMedia.Sound(names[i].name, Sounds, names[i].duration, 712 + i * 79));

        glassOff = Glass("Spent glass", "273029", null, 0f);
        glassCold = Glass("Cold glass", "637E77", "80AFB3", 0.49f);
        glassEmergency = Glass("Emergency glass", "723936", "ED4C36", 0.55f);
        guidance = Glass("Low-level seal light", "506D66", "A1C7B8", 0.60f);
    }

    private void ApplyScene()
    {
        var scene = EditorSceneManager.OpenScene(Gameplay);
        var corridor = Find<CorridorStartTrigger>();
        var creature = Find<Creature>();
        var puzzle = Find<GameCodeManager>();
        var keypad = Find<Keypad>();
        var events = Find<HorrorEventDirector>();
        if (corridor == null || creature == null || puzzle == null || keypad == null || events == null)
            throw new InvalidOperationException("Apply Nightmare to the Hotel Umbral GameScene.");

        head = corridor.playerHead;
        var oldStage = scene.GetRootGameObjects().FirstOrDefault(root => root.name == "Nightmare Update");
        if (oldStage != null)
            UnityEngine.Object.DestroyImmediate(oldStage);
        stage = Node("Nightmare Update").transform;
        tension = stage.gameObject.AddComponent<HorrorTensionDirector>();
        tension.profiles = Profiles();
        tension.corridor = corridor;
        tension.playerHead = head;
        tension.creature = creature;
        tension.puzzle = puzzle;
        tension.keypad = keypad;
        corridor.tension = tension;
        corridor.transform.position = new Vector3(0f, 0f, -15.35f);
        creature.transform.position = new Vector3(0.25f, 0.035f, -16.96f);
        creature.tension = tension;
        puzzle.tension = tension;
        puzzle.playerHead = head;
        puzzle.clueOcclusion = creature.architecture;
        Find<WinTrigger>().tension = tension;

        SetDarkness(corridor.elevatorSequence, creature);
        BindSound(creature, keypad);
        BindEvents(events, creature);
        DressRooms();
        AddDifficulty(corridor.elevatorSequence, puzzle);
        EditorSceneManager.SaveScene(scene);
    }

    private static T Find<T>() where T : UnityEngine.Object => UnityEngine.Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);

    private NightmareProfile[] Profiles()
    {
        var setups = new (NightmareDifficulty difficulty, string label, float speed, float pressure, float burst, float burstTime,
            float burstRest, float grace, float recovery, float reaction, float gaze, float gap, float light, float audio,
            float roomTime, float progress, float read, string hint)[]
        {
            (NightmareDifficulty.Normal, "NORMAL", 1.16f, 0.37f, 0.32f, 1.45f, 10f, 2.5f, .62f, .15f, 2.95f, 11.5f, 1.22f, .74f, 8.5f, .78f, .36f,
                "REGISTRO DE EVACUACIÓN\nLee los sellos del 01 al 04.\nRecepción · 104 · 107 · Servicio"),
            (NightmareDifficulty.Nightmare, "PESADILLA", 1.38f, .51f, .43f, 1.85f, 7.1f, 1.65f, .43f, .18f, 2.55f, 7.7f, 1f, .83f, 5.8f, 1f, .43f,
                "REGISTRO DE EVACUACIÓN\nLee los sellos del 01 al 04.\nDos están en las habitaciones."),
            (NightmareDifficulty.Extreme, "TERROR EXTREMO", 1.55f, .55f, .52f, 2.10f, 5.7f, 1.15f, .33f, .21f, 2.20f, 6.2f, .78f, .92f, 3.9f, 1.10f, .52f,
                "REGISTRO DE EVACUACIÓN\nBusca los cuatro sellos.\nEl orden está en las placas.")
        };
        var result = new NightmareProfile[setups.Length];
        for (int i = 0; i < setups.Length; i++)
        {
            var s = setups[i];
            string path = Folder + "/" + s.difficulty + ".asset";
            var profile = AssetDatabase.LoadAssetAtPath<NightmareProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<NightmareProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            profile.difficulty = s.difficulty;
            profile.displayName = s.label;
            profile.pursuitSpeed = s.speed;
            profile.pressureSpeed = s.pressure;
            profile.burstSpeed = s.burst;
            profile.burstSeconds = s.burstTime;
            profile.burstRest = s.burstRest;
            profile.grace = s.grace;
            profile.recovery = s.recovery;
            profile.reaction = s.reaction;
            profile.gazeProtection = s.gaze;
            profile.eventGap = s.gap;
            profile.lightScale = s.light;
            profile.audioScale = s.audio;
            profile.roomPressureAfter = s.roomTime;
            profile.progressPressure = s.progress;
            profile.clueDwell = s.read;
            profile.clueHint = s.hint;
            EditorUtility.SetDirty(profile);
            result[i] = profile;
        }
        return result;
    }

    private void SetDarkness(ElevatorSequence elevator, Creature creature)
    {
        RenderSettings.skybox = null;
        RenderSettings.sun = null;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = HotelMedia.Hex("283335");
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = HotelMedia.Hex("0D181B");
        RenderSettings.fogDensity = 0.083f;
        head.GetComponent<Camera>().backgroundColor = RenderSettings.fogColor;

        lighting = stage.gameObject.AddComponent<NightmareLighting>();
        lighting.tension = tension;
        lighting.creature = creature;
        lighting.playerHead = head;
        tension.lighting = lighting;
        elevator.cabinLight.intensity = 1.42f;
        elevator.cabinLight.range = 4.0f;
        elevator.cabinLight.color = HotelMedia.Hex("ADC7B6");

        var all = UnityEngine.Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        float[] levels = { .69f, .26f, .20f, .88f, .29f, .24f, .64f, .70f };
        float[] faults = { .20f, .68f, 0f, .66f, .42f, 0f, .58f, .38f };
        string[] colors = { "ACC2A9", "6C968C", "8AA7B4", "89B8B5", "688F88", "788FAD", "9CBAC3", "FD6752" };
        for (int i = 0; i < levels.Length; i++)
        {
            var light = all.Single(item => item.name == "Luz local " + (i + 1));
            MeshRenderer housing = light.transform.parent.GetComponentsInChildren<MeshRenderer>(true)
                .Where(renderer => renderer.name == "Bronze wall lamp")
                .OrderBy(renderer => Mathf.Abs(renderer.transform.position.z - light.transform.position.z)).First();
            bool broken = i is 2 or 5;
            AddFixture(light, housing, levels[i], 3.8f + (i % 3) * 0.32f, colors[i], faults[i], i is 0 or 3, false, false, broken, i == 7);
        }

        foreach (string room in new[] { "104", "107" })
        {
            var light = all.Single(item => item.name == "Luz habitación " + room);
            var housing = light.transform.parent.Find("Ceiling fixture").GetComponent<MeshRenderer>();
            AddFixture(light, housing, room == "104" ? .62f : .49f, 4.0f,
                room == "104" ? "89ADB2" : "75A58D", .52f, false, true, false, false, false);
        }

        var vestibule = all.Single(item => item.name == "Derivación de emergencia");
        vestibule.intensity = 0.82f;
        vestibule.range = 3.2f;
        lighting.exitPilot = AddPoint(stage, "Piloto de la cerradura", new Vector3(1.05f, 1.59f, 17.08f), "CA5145", 0.26f, 2.30f);

        var emergency = AddPoint(stage, "Baliza de persecución", new Vector3(-1.02f, 2.13f, 13.0f), "EF493C", 0f, 3.8f);
        AddFixture(emergency, null, .94f, 3.8f, "EF493C", 0f, false, false, true, false, true);
        lighting.fixtures = fixtures.ToArray();
    }

    private void AddFixture(Light light, MeshRenderer housing, float level, float range, string color, float fault, bool behind,
        bool room, bool emergency, bool ruined, bool red)
    {
        light.color = HotelMedia.Hex(color);
        light.intensity = ruined || emergency ? 0f : level;
        light.range = range;
        var fixture = new NightmareLighting.Fixture
        {
            light = light, housing = housing, level = level, fault = fault, failBehind = behind, room = room,
            emergency = emergency, ruined = ruined
        };
        if (housing != null)
        {
            fixture.litMaterials = housing.sharedMaterials.Select(material => IsGlass(material) ? (red ? glassEmergency : glassCold) : material).ToArray();
            fixture.deadMaterials = fixture.litMaterials.Select(material => IsGlass(material) ? glassOff : material).ToArray();
            housing.sharedMaterials = ruined ? fixture.deadMaterials : fixture.litMaterials;
        }
        fixtures.Add(fixture);
    }

    private bool IsGlass(Material material) => material != null &&
        (material.name == "Warm glass" || material == glassOff || material == glassCold || material == glassEmergency);

    private void AddDifficulty(ElevatorSequence elevator, GameCodeManager puzzle)
    {
        var menu = elevator.menuCanvas;
        var options = menu.GetComponent<HotelOptions>();
        options.tension = tension;
        Transform previous = menu.transform.Find("Dificultad");
        if (previous != null)
            UnityEngine.Object.DestroyImmediate(previous.gameObject);
        GameObject template = options.comfortLabel.GetComponentInParent<HotelButton>().gameObject;
        var clone = UnityEngine.Object.Instantiate(template, menu.transform);
        clone.name = "Dificultad";
        clone.transform.localPosition = new Vector3(0f, -0.444f, -0.054f);
        var button = clone.GetComponent<HotelButton>();
        button.onPress = new UnityEngine.Events.UnityEvent();
        UnityEventTools.AddPersistentListener(button.onPress, options.CycleDifficulty);
        options.difficultyLabel = clone.GetComponentInChildren<TMP_Text>();
        options.difficultyLabel.text = "PESADILLA";
        options.difficultyLabel.color = HotelMedia.Hex("E4C49A");
        var face = menu.transform.Find("Console face");
        face.localPosition = new Vector3(0f, -0.065f, 0f);
        face.localScale = new Vector3(1.43f, 1.0f, 1f);
        var register = puzzle.numberSpawnPoints[0].parent.parent.GetComponentsInChildren<TMP_Text>(true)
            .Single(label => label.name == "Orden del código");
        tension.clueHint = register;
        register.color = HotelMedia.Hex("CBD3C4");
        register.text = tension.profiles[1].clueHint;
    }

    private void BindSound(Creature creature, Keypad keypad)
    {
        soundscape = stage.gameObject.AddComponent<NightmareSound>();
        tension.soundscape = soundscape;
        soundscape.tension = tension;
        soundscape.roomTone = Audio("Sala vacía", Vector3.zero, "EmptyFloor", 0f, 0f, true, true);
        soundscape.pursuitBed = Audio("Pulso de persecución", Vector3.zero, "PursuitPulse", 0f, 0f, true, true);
        soundscape.footsteps = Audio("Suela sobre parquet", Vector3.zero, "WoodUnderfoot", 0.75f, 6f);
        soundscape.clueEmitter = Audio("Placa de registro", Vector3.zero, "SealTouch", 0.67f, 7f);
        soundscape.finalEmitter = Audio("Golpes tras el cierre", new Vector3(-0.28f, 1.16f, 17.4f), "FinalRattle", 0.78f, 14f);
        soundscape.pipes = UnityEngine.Object.FindObjectsByType<AudioSource>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Where(source => source.name == "Resonancia de cañerías").ToArray();
        foreach (var pipe in soundscape.pipes)
            pipe.volume = 0.14f;

        soundscape.step = clips["WoodUnderfoot"];
        soundscape.landing = clips["HingeSlam"];
        soundscape.seal = clips["SealTouch"];
        soundscape.lastSeal = clips["LastSeal"];
        soundscape.finalKnock = clips["FinalRattle"];
        creature.footstepAudio.clip = clips["WoodUnderfoot"];
        creature.breathAudio.maxDistance = 8.5f;
        creature.rushAudio = Audio("Arranque de persecución", new Vector3(0f, 1.31f, 0.02f), "PursuitBreath", 0.72f, 10f, parent: creature.transform);

        var capture = creature.capture;
        capture.soundscape = soundscape;
        capture.duration = 2.12f;
        capture.lungeDistance = 0.26f;
        capture.attackSound.clip = clips["CaptureImpact"];
        capture.anticipationSound = Audio("Inhalación de captura", new Vector3(0f, 1.39f, 0f), "PursuitBreath", 0.58f, 5f, parent: creature.transform);
        keypad.digitClick = clips["KeypadRelay"];
        keypad.response.maxDistance = 9f;
    }

    private AudioSource Audio(string name, Vector3 at, string clip, float volume, float range,
        bool loop = false, bool bed = false, Transform parent = null)
    {
        Transform owner = parent != null ? parent : stage;
        var old = owner.Find(name);
        if (old != null)
            UnityEngine.Object.DestroyImmediate(old.gameObject);
        var source = Node(name, owner, at).AddComponent<AudioSource>();
        source.clip = clips[clip];
        source.outputAudioMixerGroup = bed ? ambience : effects;
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = range > 0f ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 0.42f;
        source.maxDistance = Mathf.Max(range, 1f);
        source.dopplerLevel = 0f;
        source.volume = volume;
        return source;
    }

    private Material Glass(string name, string body, string emission, float strength)
    {
        string path = Folder + "/" + name + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            var warm = AssetDatabase.LoadAssetAtPath<Material>("Assets/HotelUmbral/Materials/Warm glass.mat");
            material = new Material(warm) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_BaseColor", HotelMedia.Hex(body));
        material.SetColor("_EmissionColor", emission == null ? Color.black : HotelMedia.Hex(emission) * strength);
        material.globalIlluminationFlags = emission == null ? MaterialGlobalIlluminationFlags.EmissiveIsBlack : MaterialGlobalIlluminationFlags.BakedEmissive;
        if (emission == null) material.DisableKeyword("_EMISSION");
        else material.EnableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject Node(string name, Transform parent = null, Vector3 at = default)
    {
        var node = new GameObject(name);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = at;
        return node;
    }

    private static Light AddPoint(Transform parent, string name, Vector3 at, string color, float intensity, float range)
    {
        var light = Node(name, parent, at).AddComponent<Light>();
        light.type = LightType.Point;
        light.shadows = LightShadows.None;
        light.range = range;
        light.intensity = intensity;
        light.color = HotelMedia.Hex(color);
        return light;
    }
}
