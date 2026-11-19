using System;
using UnityEngine;

public static class HotelNoise
{
    public static event Action<Vector3, float> Heard;

    public static void Emit(Vector3 location, float radius)
    {
        if (radius > 0f)
            Heard?.Invoke(location, radius);
    }
}