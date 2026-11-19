using System;
using System.IO;
using UnityEditor;
using UnityEngine;

internal static class HotelMedia
{
    public static Texture2D Texture(string name, string folder)
    {
        const int size = 384;
        var texture = new Texture2D(size, size, TextureFormat.RGB24, false);
        var pixels = new Color[size * size];
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float u = (float)x / size, v = (float)y / size;
            float mottling = Mathf.PerlinNoise(u * 11f + 2f, v * 11f + 5f) - 0.5f;
            float grain = Mathf.PerlinNoise(u * 65f + 12f, v * 63f) - 0.5f;
            float fleck = Hash(x, y) - 0.5f;
            Color color;
            switch (name)
            {
                case "Wallpaper":
                    float pane = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 8f));
                    float vine = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 8f + Mathf.Sin(v * Mathf.PI * 6f) * 0.75f));
                    float damask = Smooth(0.85f, 0.99f, vine) * Smooth(0.2f, 0.85f, Mathf.Abs(Mathf.Sin(v * Mathf.PI * 6f)));
                    color = Mix("3D4A44", "738072", pane * 0.2f + damask * 0.42f + mottling * 0.22f);
                    color *= 0.82f + 0.32f * Mathf.PerlinNoise(u * 4f, v * 5f + 3f);
                    break;
                case "Oak":
                    float grainAlong = Mathf.PerlinNoise(u * 78f + 6f, v * 4f + 2f);
                    float streaks = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 90f + grainAlong * 3f));
                    color = Mix("382523", "84634D", grainAlong * 0.64f + streaks * 0.14f);
                    break;
                case "Terrazzo":
                    float vein = Mathf.Abs(Mathf.Sin((u * 2f + v * 3f + mottling * 0.53f) * Mathf.PI));
                    float joint = Mathf.Min(Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f)), Mathf.Abs(Mathf.Sin(v * Mathf.PI * 4f)));
                    color = Mix("464440", "A2937F", mottling * 0.46f + grain * 0.23f + vein * 0.28f + 0.12f);
                    if (joint < 0.038f)
                        color *= 0.56f;
                    break;
                case "Runner":
                    float band = Mathf.Abs(Mathf.Sin(u * Mathf.PI * 4f));
                    float warp = Mathf.Abs(Mathf.Cos(v * Mathf.PI * 8f));
                    float medallion = Smooth(0.6f, 0.92f, 0.68f * band + 0.45f * warp);
                    float wool = 0.64f + grain * 0.34f + fleck * 0.23f;
                    color = Mix("632C34", "C48852", medallion * 0.78f) * wool;
                    break;
                case "Oxide":
                    color = Mix("313F42", "775A43", (mottling + 0.5f) * (grain + 0.55f));
                    break;
                case "Ledger":
                    color = Mix("765F49", "D6B58B", 0.7f + mottling * 0.5f);
                    if (Mathf.Abs(Mathf.Sin(v * Mathf.PI * 19f)) < 0.13f)
                        color *= 0.72f;
                    break;
                default:
                    float horizon = Smooth(0.42f, 0.47f, v + mottling * 0.15f);
                    float moon = Vector2.Distance(new Vector2(u, v), new Vector2(0.64f, 0.72f));
                    float trees = Smooth(0.47f, 0.52f, Mathf.PerlinNoise(14f * u, v * 0.25f));
                    color = Mix("292F30", "9A8166", horizon * (0.3f + mottling * 0.65f));
                    color = Color.Lerp(color, Mix("ADA38C", "CFB386", grain + 0.5f), 1f - Smooth(0.079f, 0.084f, moon));
                    if (v < 0.29f + trees * 0.26f)
                        color = Mix("202627", "414640", mottling + 0.5f);
                    break;
            }
            color.r += grain * 0.05f + fleck * 0.029f;
            color.g += grain * 0.05f + fleck * 0.029f;
            color.b += grain * 0.05f + fleck * 0.029f;
            pixels[y * size + x] = color;
        }

        texture.SetPixels(pixels);
        texture.Apply();
        string path = $"{folder}/{name}.png";
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.maxTextureSize = 512;
        importer.mipmapEnabled = name != "Landscape" && name != "Ledger";
        importer.wrapMode = name is "Landscape" or "Ledger" ? TextureWrapMode.Clamp : TextureWrapMode.Repeat;
        importer.textureCompression = TextureImporterCompression.Compressed;
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    public static AudioClip Sound(string name, string folder, float duration, int seed)
    {
        const int rate = 22050;
        int samples = Mathf.RoundToInt(rate * duration);
        var random = new System.Random(seed);
        string path = $"{folder}/{name}.wav";
        using (var stream = File.Create(path))
        using (var audio = new BinaryWriter(stream))
        {
            audio.Write(0x46464952);
            audio.Write(36 + samples * 2);
            audio.Write(0x45564157);
            audio.Write(0x20746D66);
            audio.Write(16);
            audio.Write((short)1);
            audio.Write((short)1);
            audio.Write(rate);
            audio.Write(rate * 2);
            audio.Write((short)2);
            audio.Write((short)16);
            audio.Write(0x61746164);
            audio.Write(samples * 2);
            float low = 0f, slower = 0f;
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / rate;
                float white = (float)(random.NextDouble() * 2.0 - 1.0);
                low += (white - low) * 0.081f;
                slower += (low - slower) * 0.029f;
                float high = white - low;
                float at = t / duration;
                float envelope = Smooth(0f, 0.012f, t) * (1f - Smooth(0.87f, 1f, at));
                float value = name switch
                {
                    "Motor" => Sin(47f, t) * 0.23f + Sin(94f, t) * 0.11f * Sin(7f, t) + low * (0.15f + 0.14f * Sin(11f, t)),
                    "Cabin" => Sin(52f, t) * 0.063f + Sin(104f, t) * 0.019f + slower * 0.31f,
                    "Ding" or "Release" => (Sin(630f, t) * 0.31f + Sin(837f, t) * 0.19f + Sin(1361f, t) * 0.065f) * Mathf.Exp(-t * 3.7f),
                    "Door" => (low * 0.48f + Sin(72f, t) * 0.15f + Sin(151f + 20f * at, t) * 0.085f) * (0.48f + 0.3f * Sin(8f, t)),
                    "Step" or "Knock" => (Sin(Mathf.Lerp(102f, 64f, at), t) * 0.31f + low * 0.76f) * Mathf.Exp(-at * 7f),
                    "Breath" or "Whisper" => (low * 0.61f + slower * 0.84f + high * 0.043f) * Mathf.Pow(Mathf.Max(0f, Sin(0.48f, t)), 1.1f),
                    "Pipes" => slower * 0.8f + Sin(61f, t) * (0.055f + 0.033f * Sin(0.5f, t)) + Sin(116f, t) * 0.025f,
                    "Click" => (0.32f * low + 0.32f * high + 0.13f * Sin(380f, t)) * Mathf.Exp(-at * 9f),
                    "Reject" => (Sin(146f, t) + Sin(151f, t)) * (t < 0.16f || t > 0.23f ? 0.19f : 0f),
                    "Glass" or "GlassAlternate" => (high * 0.43f + Sin(2500f + seed * 7f, t) * 0.15f + Sin(3401f, t) * 0.11f) * Mathf.Exp(-at * 5f) * (0.65f + 0.32f * Sin(84f, t)),
                    "Ceramic" or "CeramicAlternate" => (low * 0.72f + high * 0.22f + Sin(457f + seed * 7f, t) * 0.26f) * Mathf.Exp(-at * 7f),
                    "Attack" => (low * 0.48f + Sin(91f + 35f * at, t) * 0.24f + Sin(293f - 90f * at, t) * 0.19f + high * 0.16f) * (0.6f + 0.2f * Sin(28f, t)),
                    "Aftershock" => (slower * 0.73f + Sin(48f, t) * 0.30f) * Mathf.Exp(-at * 4f),
                    _ => 0f
                };
                audio.Write((short)(Mathf.Clamp(value * envelope, -0.98f, 0.98f) * short.MaxValue));
            }
        }
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
    }

    public static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color result);
        return result;
    }

    private static float Sin(float frequency, float time) => Mathf.Sin(frequency * time * (2f * Mathf.PI));
    private static float Hash(int x, int y) => (Mathf.Sin(x * 12.9898f + y * 78.233f) * 43758.5453f) % 1f * 0.5f + 0.5f;
    private static float Smooth(float a, float b, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(a, b, value));
    private static Color Mix(string a, string b, float value) => Color.Lerp(Hex(a), Hex(b), value);
}