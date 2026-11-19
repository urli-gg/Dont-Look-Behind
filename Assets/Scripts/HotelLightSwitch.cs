using UnityEngine;

public class HotelLightSwitch : MonoBehaviour
{
    public Light fixture;

    public void Toggle()
    {
        if (fixture != null)
            fixture.enabled = !fixture.enabled;
    }
}