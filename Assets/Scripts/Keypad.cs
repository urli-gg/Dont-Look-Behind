using System.Collections;
using TMPro;
using UnityEngine;

public class Keypad : MonoBehaviour
{
    public TMP_Text codeDisplay;
    public int maxDigits = 4;
    public GameCodeManager gameCodeManager;
    public DoorController door;
    public AudioSource response;
    public AudioClip rejected;
    public AudioClip accepted;

    private static readonly Color Ready = new(0.59f, 0.93f, 0.78f);
    private static readonly Color Rejected = new(1f, 0.46f, 0.42f);
    private string input = "";
    private bool locked;
    public bool Accepted { get; private set; }

    private void Start() => Refresh();

    public void PressNumber(string number)
    {
        if (locked || input.Length >= maxDigits || number == null || number.Length != 1 ||
            number[0] < '0' || number[0] > '9')
            return;
        input += number;
        Refresh();
    }

    public void DeleteNumber()
    {
        if (locked || input.Length == 0)
            return;
        input = input.Substring(0, input.Length - 1);
        Refresh();
    }

    public void ClearCode()
    {
        if (locked)
            return;
        input = "";
        Refresh();
    }

    public void SubmitCode()
    {
        if (locked || door == null || gameCodeManager == null || string.IsNullOrEmpty(gameCodeManager.GeneratedCode))
            return;
        if (input.Length == maxDigits && input == gameCodeManager.GeneratedCode)
        {
            Accepted = locked = true;
            SetDisplay("ABIERTO", Ready);
            Play(accepted);
            door.OpenDoor();
        }
        else
        {
            StartCoroutine(Reject());
        }
    }

    private IEnumerator Reject()
    {
        locked = true;
        SetDisplay("DENEGADO", Rejected);
        Play(rejected);
        yield return new WaitForSeconds(1.1f);
        input = "";
        locked = false;
        Refresh();
    }

    private void Play(AudioClip clip)
    {
        if (response != null && clip != null)
            response.PlayOneShot(clip);
    }

    private void Refresh() => SetDisplay(input.PadRight(maxDigits, '·'), Ready);

    private void SetDisplay(string text, Color color)
    {
        if (codeDisplay == null)
            return;
        codeDisplay.text = text;
        codeDisplay.color = color;
    }
}