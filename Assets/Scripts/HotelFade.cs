using UnityEngine;

public class HotelFade : MonoBehaviour
{
    public Renderer surface;
    private int opacityProperty;
    private MaterialPropertyBlock properties;

    private void Awake()
    {
        opacityProperty = Shader.PropertyToID("_Opacity");
        SetOpacity(0f);
    }

    public void SetOpacity(float opacity)
    {
        if (surface == null)
            return;
        surface.enabled = opacity > 0f;
        properties ??= new MaterialPropertyBlock();
        properties.SetFloat(opacityProperty, Mathf.Clamp01(opacity));
        surface.SetPropertyBlock(properties);
    }
}
