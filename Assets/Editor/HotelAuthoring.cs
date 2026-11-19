using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.XR.Interaction.Toolkit.Filtering;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Teleportation;

internal sealed partial class HotelAuthoring
{
    private const string Root = "Assets/HotelUmbral";
    private const string ModelFolder = Root + "/Meshes";
    private const string PrefabFolder = Root + "/Prefabs";
    private const string MaterialFolder = Root + "/Materials";
    private const string SurfaceFolder = Root + "/Surfaces";
    private const string SoundFolder = Root + "/Sounds";
    private const string RigPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/Prefabs/XR Origin Hands (XR Rig).prefab";
    private const string MixerPath = "Assets/Samples/XR Interaction Toolkit/3.5.1/Hands Interaction Demo/DemoAssets/Audio/MainAudioMixer.mixer";
    private const float HalfWidth = 1.64f;
    private const float Ceiling = 2.82f;
    private const float Entrance = -17.6f;
    private const float CabinBack = -20.75f;
    private const float Gate = 17.9f;
    private const float ExitEnd = 20.4f;
    private const int DebrisLayer = 8;
    private const int PropsLayer = 9;
    private const int ShellLayer = 10;

    private readonly Dictionary<string, Material> materials = new();
    private readonly Dictionary<string, AudioClip> sounds = new();
    private readonly Dictionary<string, GameObject> prefabs = new();
    private readonly List<BoxCollider> navigation = new();
    private readonly List<TeleportationArea> teleportAreas = new();
    private readonly List<HorrorEventDirector.Beat> beats = new();
    private TMP_FontAsset font;
    private AudioMixerGroup effects;
    private AudioMixerGroup ambience;
    private TeleportationProvider teleporter;

    private void Prepare()
    {
        foreach (string path in new[] { Root, ModelFolder, PrefabFolder, MaterialFolder, SurfaceFolder, SoundFolder })
            Directory.CreateDirectory(path);
        AssetDatabase.Refresh();

        font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/VRTemplateAssets/Fonts/Inter/Inter-Regular SDF.asset");
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(MixerPath);
        effects = mixer.FindMatchingGroups("SFX").Single();
        ambience = mixer.FindMatchingGroups("MX").Single();

        Surface("Sage wallpaper", "FFFFFF", "Wallpaper", 0f, 0.27f);
        Surface("Dark oak", "FFFFFF", "Oak", 0f, 0.28f);
        Surface("Terrazzo", "FFFFFF", "Terrazzo", 0f, 0.22f);
        Surface("Embroidered wool", "FFFFFF", "Runner", 0f, 0.14f);
        Surface("Oxidized steel", "FFFFFF", "Oxide", 0.40f, 0.42f);
        Surface("Ledger paper", "FFFFFF", "Ledger", 0f, 0.11f);
        Surface("The grounds at dusk", "FFFFFF", "Landscape", 0f, 0.16f);
        Surface("Plaster", "9F9D87", null, 0f, 0.18f);
        Surface("Brass", "AF8A50", null, 0.48f, 0.43f);
        Surface("Black enamel", "242626", null, 0.15f, 0.33f);
        Surface("Green enamel", "33554D", null, 0.32f, 0.43f);
        Surface("Safety red", "772C2C", null, 0.21f, 0.4f);
        Surface("Ceramic", "6E9D92", null, 0.04f, 0.64f);
        Surface("Ivory ceramic", "CCBA94", null, 0f, 0.45f);
        Surface("Bottle glass", "405F52", null, 0.32f, 0.68f);
        Surface("Tumbler glass", "87988D", null, 0.27f, 0.76f);
        Surface("Silvered mirror", "738785", null, 0.77f, 0.72f);
        Surface("Warm glass", "D6A96F", null, 0f, 0.42f, "FFB965", 0.85f);
        Surface("Red glass", "AC4735", null, 0f, 0.42f, "FF542D", 0.7f);
        Surface("Phosphor", "174738", null, 0f, 0.1f, "2F966D", 0.36f);
        Surface("Stain", "332827", null, 0f, 0.16f);
        Surface("Brass patina", "57634B", null, 0.35f, 0.26f);
        SaveMaterial("Comfort fade", Shader.Find("Hotel/Comfort Fade"));
        GhostSurface("Ghost_Dress", "Waterlogged dress", "9DA9A1", 0.11f);
        GhostSurface("Ghost_Head", "Pallid skin", "A7B6B3", 0.19f);
        GhostSurface("Ghost_Hair", "Damp hair", "7E8E89", 0.24f);

        var clips = new (string name, float length)[]
        {
            ("Motor", 3f), ("Cabin", 4f), ("Ding", 1.4f), ("Door", 1.7f), ("Step", 0.42f),
            ("Knock", 0.61f), ("Breath", 4.2f), ("Whisper", 1.65f), ("Pipes", 5f), ("Click", 0.105f),
            ("Reject", 0.49f), ("Glass", 1.1f), ("GlassAlternate", 0.97f), ("Ceramic", 0.86f),
            ("CeramicAlternate", 0.98f), ("Attack", 0.96f), ("Aftershock", 1.31f), ("Release", 1.96f)
        };
        for (int i = 0; i < clips.Length; i++)
            sounds.Add(clips[i].name, HotelMedia.Sound(clips[i].name, SoundFolder, clips[i].length, 117 + i * 37));
    }

    private void Surface(string name, string hex, string texture, float metallic, float smoothness, string emission = null, float strength = 0f)
    {
        Material material = SaveMaterial(name, Shader.Find("Universal Render Pipeline/Lit"));
        material.SetColor("_BaseColor", HotelMedia.Hex(hex));
        material.SetFloat("_Metallic", metallic);
        material.SetFloat("_Smoothness", smoothness);
        if (texture != null)
            material.SetTexture("_BaseMap", HotelMedia.Texture(texture, SurfaceFolder));
        if (emission != null)
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", HotelMedia.Hex(emission) * strength);
        }
        EditorUtility.SetDirty(material);
    }

    private Material SaveMaterial(string name, Shader shader)
    {
        if (shader == null)
            throw new InvalidOperationException($"Shader unavailable for {name}.");
        string path = $"{MaterialFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }
        materials.Add(name, material);
        return material;
    }

    private void GhostSurface(string original, string name, string tint, float smoothness)
    {
        var source = AssetDatabase.LoadAssetAtPath<Material>($"Assets/assets/Models/GhostGirl/Materials/{original}.mat");
        if (source == null)
            throw new InvalidOperationException($"GhostGirl material unavailable: {original}.");
        Material surface = SaveMaterial(name, source.shader);
        surface.CopyPropertiesFromMaterial(source);
        surface.SetColor("_BaseColor", HotelMedia.Hex(tint));
        surface.SetFloat("_Smoothness", smoothness);
        surface.SetFloat("_Metallic", 0f);
        EditorUtility.SetDirty(surface);
    }

    private GameObject Model(string name, HotelGeometry geometry, Transform parent = null)
    {
        string path = $"{ModelFolder}/{name}.asset";
        Mesh generated = geometry.ToMesh(name);
        Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        if (mesh == null)
        {
            mesh = generated;
            AssetDatabase.CreateAsset(mesh, path);
        }
        else
        {
            EditorUtility.CopySerialized(generated, mesh);
            EditorUtility.SetDirty(mesh);
            UnityEngine.Object.DestroyImmediate(generated);
        }

        var target = Node(name, parent);
        target.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = target.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = geometry.Surfaces.Select(surface => materials[surface]).ToArray();
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.lightProbeUsage = LightProbeUsage.Off;
        renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
        return target;
    }

    private GameObject SavePrefab(string name, GameObject template)
    {
        template.name = name;
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(template, $"{PrefabFolder}/{name}.prefab");
        UnityEngine.Object.DestroyImmediate(template);
        prefabs.Add(name, asset);
        return asset;
    }

    private GameObject Place(string model, Transform parent, Vector3 position, float yaw = 0f)
    {
        var target = (GameObject)PrefabUtility.InstantiatePrefab(prefabs[model], parent);
        target.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return target;
    }

    private static GameObject Node(string name, Transform parent = null, Vector3 position = default, float yaw = 0f)
    {
        var target = new GameObject(name);
        target.transform.SetParent(parent, false);
        target.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return target;
    }

    private static BoxCollider Box(GameObject target, Vector3 center, Vector3 size)
    {
        var collider = target.AddComponent<BoxCollider>();
        collider.center = center;
        collider.size = size;
        return collider;
    }

    private static void SetLayer(GameObject root, int layer)
    {
        foreach (var item in root.GetComponentsInChildren<Transform>(true))
            item.gameObject.layer = layer;
    }

    private void Walkable(GameObject segment, List<TeleportationArea> areas)
    {
        var collider = segment.GetComponent<BoxCollider>();
        var area = segment.AddComponent<TeleportationArea>();
        area.teleportationProvider = teleporter;
        area.interactionLayers = UnityEngine.XR.Interaction.Toolkit.InteractionLayerMask.GetMask("Teleport");
        area.colliders.Add(collider);
        areas?.Add(area);
    }

    private TMP_Text Label(Transform parent, string name, string caption, Vector3 position, float width, float height, float size,
        string color = "EBDBB7", TextAlignmentOptions alignment = TextAlignmentOptions.Center)
    {
        var target = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)target.transform;
        rect.SetParent(parent, false);
        rect.localPosition = position;
        rect.sizeDelta = new Vector2(width, height);
        var text = target.AddComponent<TextMeshPro>();
        text.font = font;
        text.text = caption;
        text.fontSize = size;
        text.color = HotelMedia.Hex(color);
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    private AudioSource Audio(GameObject root, string clip, float volume, float range, bool loop = false, bool autoplay = false, bool bed = false)
    {
        var source = root.AddComponent<AudioSource>();
        source.outputAudioMixerGroup = bed ? ambience : effects;
        source.clip = sounds[clip];
        source.playOnAwake = autoplay;
        source.loop = loop;
        source.spatialBlend = range > 0f ? 1f : 0f;
        source.minDistance = 0.4f;
        source.maxDistance = Mathf.Max(1f, range);
        source.rolloffMode = AudioRolloffMode.Linear;
        source.dopplerLevel = 0f;
        source.volume = volume;
        return source;
    }

    private HotelButton Button(Transform parent, string name, string caption, Vector3 position, float width, float height,
        string capMaterial = "Green enamel", float size = 0.22f)
    {
        var target = Node(name, parent, position);
        var cap = Place("Button cap", target.transform, Vector3.zero);
        cap.transform.localScale = new Vector3(width, height, 1f);
        var box = Box(target, Vector3.zero, new Vector3(width, height, 0.036f));
        cap.GetComponent<MeshRenderer>().sharedMaterial = materials[capMaterial];
        var label = Label(target.transform, caption, caption, new Vector3(0f, 0f, -0.02f), width, height, size);

        var interactable = target.AddComponent<XRSimpleInteractable>();
        interactable.colliders.Add(box);
        var poke = target.AddComponent<XRPokeFilter>();
        poke.pokeInteractable = interactable;
        poke.pokeCollider = box;
        HotelButton button = target.AddComponent<HotelButton>();
        Transform movingCap = Node("Button movement", target.transform).transform;
        cap.transform.SetParent(movingCap, true);
        label.transform.SetParent(movingCap, false);
        button.cap = movingCap;
        button.click = Audio(target, "Click", 0.63f, 4f);
        return button;
    }

    private Light Lamp(Transform parent, string name, Vector3 position, string color, float intensity, float range)
    {
        var light = Node(name, parent, position).AddComponent<Light>();
        light.type = LightType.Point;
        light.color = HotelMedia.Hex(color);
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
        return light;
    }

    private static void MarkStatic(GameObject segment)
    {
        foreach (var renderer in segment.GetComponentsInChildren<MeshRenderer>())
            GameObjectUtility.SetStaticEditorFlags(renderer.gameObject, StaticEditorFlags.BatchingStatic);
    }
}
