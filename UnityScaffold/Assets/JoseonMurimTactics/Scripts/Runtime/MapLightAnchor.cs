using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class MapLightAnchor : MonoBehaviour
{
    public Color color = Color.white;
    public float radius = 1.5f;
    public float intensity = 0.5f;
    public Light2D boundLight;

    public void Configure(Color newColor, float newRadius, float newIntensity, Light2D light = null)
    {
        color = newColor;
        radius = Mathf.Max(0.01f, newRadius);
        intensity = Mathf.Max(0f, newIntensity);
        boundLight = light;
    }
}
}
