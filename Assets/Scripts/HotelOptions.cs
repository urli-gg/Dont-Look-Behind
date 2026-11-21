using TMPro;
using UnityEngine;

public class HotelOptions : MonoBehaviour
{
    public TMP_Text volumeLabel;
    public TMP_Text comfortLabel;
    public TMP_Text difficultyLabel;
    public HorrorTensionDirector tension;
    public TMP_Text turnLabel;
    public HotelXrSession xrSession;

    private static readonly float[] VolumeLevels = { 1f, 0.7f, 0.4f, 0f };
    private const string VolumeKey = "hotel.volume";
    private const string DifficultyKey = "hotel.difficulty";
    private const string ComfortKey = "hotel.reducedScares";
    private const string TurnKey = "hotel.smoothTurn";

    public static NightmareDifficulty Difficulty => (NightmareDifficulty)Mathf.Clamp(PlayerPrefs.GetInt(DifficultyKey, (int)NightmareDifficulty.Nightmare), 0, 2);

    public static bool ReducedScares => PlayerPrefs.GetInt(ComfortKey, 0) == 1;
    public static bool SmoothTurn => PlayerPrefs.GetInt(TurnKey, 0) == 1;

    [RuntimeInitializeOnLoadMethod]
    private static void ApplyVolume()
    {
        AudioListener.volume = VolumeLevels[VolumeIndex];
    }

    private static int VolumeIndex => Mathf.Clamp(PlayerPrefs.GetInt(VolumeKey), 0, VolumeLevels.Length - 1);

    private void Start() => Refresh();

    public void CycleVolume()
    {
        PlayerPrefs.SetInt(VolumeKey, (VolumeIndex + 1) % VolumeLevels.Length);
        PlayerPrefs.Save();
        ApplyVolume();
        Refresh();
    }

    public void ToggleComfort()
    {
        PlayerPrefs.SetInt(ComfortKey, ReducedScares ? 0 : 1);
        PlayerPrefs.Save();
        Refresh();
    }

    public void CycleDifficulty()
    {
        PlayerPrefs.SetInt(DifficultyKey, ((int)Difficulty + 1) % 3);
        PlayerPrefs.Save();
        if (tension != null)
            tension.SetProfile(Difficulty);
        Refresh();
    }

    public void ToggleTurn()
    {
        bool smooth = !SmoothTurn;
        PlayerPrefs.SetInt(TurnKey, smooth ? 1 : 0);
        PlayerPrefs.Save();
        xrSession.ApplyTurnMode(smooth);
        Refresh();
    }

    private void Refresh()
    {
        if (volumeLabel != null)
            volumeLabel.text = $"AUDIO  {AudioListener.volume:P0}";
        if (difficultyLabel != null && tension != null)
            difficultyLabel.text = tension.Profile.displayName;
        if (comfortLabel != null)
            comfortLabel.text = ReducedScares ? "SUSTOS  SUAVES" : "SUSTOS  NORMAL";
        if (turnLabel != null)
            turnLabel.text = SmoothTurn ? "GIRO  SUAVE" : "GIRO  POR PASOS";
    }
}
