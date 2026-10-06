using UnityEngine;
using TMPro;
using System.Collections;

public class Keypad : MonoBehaviour
{
    [Header("Pantalla")]
    public TMP_Text codeDisplay;

    [Header("Configuracion")]
    public int maxDigits = 4;

    [Header("Codigo del nivel")]
    public GameCodeManager gameCodeManager;

    [Header("Puerta")]
    public DoorController door;

    private string currentCode = "";
    private bool locked = false;

    void Start()
    {
        UpdateDisplay();
    }

    public void PressNumber(string number)
    {
        if (locked)
            return;

        if (currentCode.Length >= maxDigits)
            return;

        currentCode += number;

        UpdateDisplay();
    }

    public void DeleteNumber()
    {
        if (locked)
            return;

        if (currentCode.Length == 0)
            return;

        currentCode = currentCode.Substring(
            0,
            currentCode.Length - 1
        );

        UpdateDisplay();
    }

    public void ClearCode()
    {
        if (locked)
            return;

        currentCode = "";

        UpdateDisplay();
    }

    public void SubmitCode()
    {
        if (locked)
            return;

        if (currentCode == gameCodeManager.GeneratedCode)
        {
            CorrectCode();
        }
        else
        {
            StartCoroutine(WrongCode());
        }
    }

    void CorrectCode()
    {
        locked = true;

        codeDisplay.text = "OPEN";

        door.OpenDoor();
    }

    IEnumerator WrongCode()
    {
        locked = true;

        codeDisplay.text = "ERROR";

        yield return new WaitForSeconds(1.5f);

        currentCode = "";

        locked = false;

        UpdateDisplay();
    }

    void UpdateDisplay()
    {
        if (currentCode.Length == 0)
        {
            codeDisplay.text = "----";
        }
        else
        {
            codeDisplay.text = currentCode;
        }
    }
}
