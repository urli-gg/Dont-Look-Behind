using UnityEngine;

public enum NightmareDifficulty { Normal, Nightmare, Extreme }

[CreateAssetMenu(menuName = "Hotel Umbral/Nightmare Profile")]
public sealed class NightmareProfile : ScriptableObject
{
    public NightmareDifficulty difficulty;
    public string displayName;
    [Min(0.1f)] public float pursuitSpeed = 1.32f;
    [Min(0f)] public float pressureSpeed = 0.48f;
    [Min(0f)] public float burstSpeed = 0.42f;
    [Min(0.1f)] public float burstSeconds = 1.65f;
    [Min(1f)] public float burstRest = 7f;
    [Min(0f)] public float grace = 1.7f;
    [Min(0.1f)] public float recovery = 0.46f;
    [Min(0.05f)] public float reaction = 0.17f;
    [Min(0.1f)] public float gazeProtection = 2.55f;
    [Min(1f)] public float eventGap = 8f;
    [Min(0.1f)] public float lightScale = 1f;
    [Range(0f, 1f)] public float audioScale = 0.82f;
    [Min(0.1f)] public float roomPressureAfter = 6f;
    [Min(0.1f)] public float progressPressure = 1f;
    [Min(0.2f)] public float clueDwell = 0.44f;
    [TextArea] public string clueHint;
}
