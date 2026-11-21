using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

internal sealed partial class NightmareAuthoring
{
    private GameObject CreateSilhouette()
    {
        var fabric = Glass("Sooted linen", "202B2A", null, 0f);
        var shape = new HotelGeometry()
            .Lathe("Linen", new Vector3(0f, 0f, 0f),
                Profile(.16f, .05f, .30f, .16f, .23f, .45f, .17f, .83f, .23f, 1.26f, .29f, 1.54f, .17f, 1.61f), 12)
            .Lathe("Linen", new Vector3(.045f, 0f, -.02f),
                Profile(.13f, 1.53f, .18f, 1.69f, .18f, 1.81f, .13f, 1.96f, .04f, 2.04f, 0f, 2.05f), 12)
            .Box("Linen", -.264f, 1.12f, -.016f, .085f, .63f, .11f)
            .Box("Linen", .24f, 1.11f, .046f, .115f, .69f, .10f);
        return ModelPrefab("Hall silhouette", shape, new[] { fabric });
    }

    private void DressRooms()
    {
        var stain = StainPrefab();
        var timber = Glass("Sodden furniture", "503930", null, 0f);
        var bench = new HotelGeometry()
            .Box("Wood", 0f, .45f, 0f, .76f, .077f, .34f)
            .Box("Wood", -.28f, .23f, 0f, .075f, .42f, .31f)
            .Box("Wood", .27f, .22f, 0f, .065f, .38f, .32f)
            .Box("Wood", -.19f, .66f, -.147f, .085f, .55f, .065f)
            .Box("Wood", .29f, .53f, -.146f, .072f, .25f, .064f)
            .Box("Wood", -.12f, .80f, -.147f, .31f, .072f, .075f);
        Place(ModelPrefab("Broken bench", bench, new[] { timber }, new Vector3(.73f, .51f, .32f)),
            stage, new Vector3(3.77f, 0f, 5.61f), 15f);

        var marker = ModelPrefab("Pilot below the baseboard", new HotelGeometry().Box("Pilot", 0f, 0f, 0f, .075f, .025f, .015f),
            new[] { guidance });
        foreach (var location in new[] { new Vector3(-1.409f, .27f, -9.38f), new Vector3(1.405f, .27f, 3.02f), new Vector3(-1.407f, .27f, 15.4f) })
            Place(marker, stage, location, location.x < 0f ? -90f : 90f);

        foreach (var entry in new (float x, float y, float z, float width, float height)[]
        {
            (-1.412f, 1.96f, -14.31f, 1.48f, 1.36f), (1.412f, 1.53f, -11.7f, 1.25f, 1.95f),
            (-1.406f, 1.73f, -5.12f, 1.47f, 2.25f), (1.41f, 1.7f, -.41f, 1.11f, 1.6f),
            (-1.404f, 1.63f, 3.09f, 1.45f, 1.85f), (1.409f, 1.59f, 9.36f, 1.18f, 1.51f),
            (-1.404f, 1.77f, 14.73f, 1.30f, 1.95f), (-4.365f, 1.64f, -9.91f, 1.19f, 1.7f),
            (4.369f, 1.51f, 5.24f, 1.05f, 1.45f)
        })
        {
            var overlay = Place(stain, stage, new Vector3(entry.x, entry.y, entry.z), entry.x < 0 ? -90f : 90f);
            overlay.transform.localScale = new Vector3(entry.width, entry.height, 1f);
        }

        var blood = AssetDatabase.LoadAssetAtPath<Material>(Folder + "/Dry streak.mat");
        if (blood == null)
            blood = SaveStainMaterial("Dry streak", new Color(.035f, .008f, .008f, .68f));
        var mark = Place(stain, stage, new Vector3(-1.412f, .73f, 13.30f), -90f);
        mark.GetComponent<MeshRenderer>().sharedMaterial = blood;
        mark.transform.localScale = new Vector3(.46f, .64f, 1f);
    }

    private GameObject StainPrefab()
    {
        const string path = Folder + "/Moisture and lifted paint.png";
        const int size = 384;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (float)x / (size - 1), v = (float)y / (size - 1);
            float flecks = Mathf.PerlinNoise(u * 125f + 2.1f, v * 93f + 13f);
            float mold = Mathf.PerlinNoise(u * 13f + 11f, v * 12f + 1.3f);
            float runs = Mathf.PerlinNoise(u * 33f + 7f, v * 2.1f + 5f);
            float border = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Min(u, 1f - u, v, 1f - v) * 8f));
            float damp = Mathf.SmoothStep(0f, 1f, (mold - 0.36f) * 3f)
                * Mathf.SmoothStep(.23f, .92f, v) + Mathf.Pow(runs, 4) * (0.6f + v);
            float chips = Mathf.Pow(Mathf.Max(0f, (flecks - .61f) * 2.55f), 2f);
            byte opacity = (byte)(Mathf.Clamp01(border * (.67f * damp + .48f * chips)) * 255f);
            pixels[y * size + x] = new Color32(0, 0, 0, opacity);
        }
        texture.SetPixels32(pixels);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.maxTextureSize = 512;
        importer.SaveAndReimport();

        var stain = GameObject.CreatePrimitive(PrimitiveType.Quad);
        UnityEngine.Object.DestroyImmediate(stain.GetComponent<Collider>());
        var renderer = stain.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = SaveStainMaterial("Moisture mask", new Color(.007f, .009f, .009f, .81f));
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        return SavePrefab("Damp wall overlay", stain);
    }

    private Material SaveStainMaterial(string label, Color color)
    {
        string path = Folder + "/" + label + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Hotel/Nightmare Stain")) { name = label };
            AssetDatabase.CreateAsset(material, path);
        }
        material.SetColor("_Shade", color);
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(Folder + "/Moisture and lifted paint.png"));
        EditorUtility.SetDirty(material);
        return material;
    }

    private GameObject ModelPrefab(string name, HotelGeometry design, Material[] materials, Vector3 collision = default)
    {
        string path = Folder + "/" + name + ".asset";
        Mesh generated = design.ToMesh(name);
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
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

        var node = Node(name);
        node.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = node.AddComponent<MeshRenderer>();
        renderer.sharedMaterials = materials;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        if (collision != Vector3.zero)
        {
            node.layer = LayerMask.NameToLayer("Hotel Props");
            var box = node.AddComponent<BoxCollider>();
            box.center = Vector3.up * collision.y / 2f;
            box.size = collision;
        }
        return SavePrefab(name, node);
    }

    private static GameObject SavePrefab(string name, GameObject model)
    {
        model.name = name;
        var prefab = PrefabUtility.SaveAsPrefabAsset(model, Folder + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(model);
        return prefab;
    }

    private static GameObject Place(GameObject prefab, Transform parent, Vector3 position, float yaw)
    {
        var item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        item.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
        return item;
    }

    private static Vector2[] Profile(params float[] values)
    {
        var profile = new Vector2[values.Length / 2];
        for (int i = 0; i < profile.Length; i++)
            profile[i] = new Vector2(values[2 * i], values[2 * i + 1]);
        return profile;
    }
}
