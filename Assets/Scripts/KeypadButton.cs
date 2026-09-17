using UnityEngine;

public class KeypadButton : MonoBehaviour
{
    public Keypad keypad;
    public string number;

    public void Press()
    {
        keypad.PressNumber(number);
    }
}