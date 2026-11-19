using TMPro;
using UnityEngine;

public class HotelOptions : MonoBehaviour
{
    public TMP_Text volumeLabel;
    public TMP_Text comfortLabel;

    private static readonly float[] VolumeLevels = { 1f, 0.7f, 0.4f, 0f };
    private const string VolumeKey = "hotel.volume";
    private const string ComfortKey = "hotel.reducedScares";

    public static bool ReducedScares => PlayerPrefs.GetInt(ComfortKey, 0) == 1;

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

    private void Refresh()
    {
        if (volumeLabel != null)
            volumeLabel.text = $"AUDIO  {AudioListener.volume:P0}";
        if (comfortLabel != null)
            comfortLabel.text = ReducedScares ? "SUSTOS  SUAVES" : "SUSTOS  NORMAL";
    }
}
