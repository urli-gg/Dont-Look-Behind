using UnityEngine;
using TMPro;

public class Keypad : MonoBehaviour
{
    public TMP_Text codeDisplay;
    public int maxDigits = 4;

    private string currentCode = "";

    public void PressNumber(string number)
    {
        if (currentCode.Length >= maxDigits)
            return;

        currentCode += number;
        UpdateDisplay();
    }

    public void DeleteNumber()
    {
        if (currentCode.Length == 0)
            return;

        currentCode = currentCode.Substring(0, currentCode.Length - 1);
        UpdateDisplay();
    }

    public void ClearCode()
    {
        currentCode = "";
        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        if (codeDisplay == null)
            return;

        if (currentCode.Length == 0)
            codeDisplay.text = "----";
        else
            codeDisplay.text = currentCode;
    }
}
