using UnityEngine;

public class KeypadEnterButton : MonoBehaviour
{
    public Keypad keypad;

    public void Press()
    {
        keypad.SubmitCode();
    }
}
