using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class ShadowBlob : MonoBehaviour
{
    [SerializeField] private SpriteRenderer shadowRenderer;
    [SerializeField] private Vector2 size = new Vector2(0.84f, 0.24f);
    [SerializeField] private Vector3 localOffset = new Vector3(0f, -0.04f, 0.04f);
    [SerializeField] private Color color = new Color(0.04f, 0.035f, 0.025f, 0.28f);
    [SerializeField] private int sortingOrder = 1980;

    private static Sprite sharedShadowSprite;

    public SpriteRenderer Renderer => shadowRenderer;

    private void Awake()
    {
        Refresh();
    }

    private void OnValidate()
    {
        if (shadowRenderer != null)
        {
            ApplyRendererState();
        }
    }

    public void Configure(Vector2 newSize, Color newColor, int newSortingOrder)
    {
        size = newSize;
        color = newColor;
        sortingOrder = newSortingOrder;
        if (Application.isPlaying)
        {
            Refresh();
        }
        else
        {
            ApplyRendererState();
        }
    }

    public void Refresh()
    {
        shadowRenderer = shadowRenderer == null ? FindShadowRenderer() : shadowRenderer;
        if (shadowRenderer == null)
        {
            GameObject shadowObject = new GameObject("Grounding Shadow");
            shadowObject.transform.SetParent(transform, false);
            shadowRenderer = shadowObject.AddComponent<SpriteRenderer>();
        }

        ApplyRendererState();
    }

    private void ApplyRendererState()
    {
        if (shadowRenderer == null)
        {
            return;
        }

        shadowRenderer.sprite = GetShadowSprite();
        shadowRenderer.color = color;
        shadowRenderer.sortingLayerName = "Default";
        shadowRenderer.sortingOrder = sortingOrder;
        shadowRenderer.transform.localPosition = localOffset;
        shadowRenderer.transform.localScale = new Vector3(Mathf.Max(0.01f, size.x), Mathf.Max(0.01f, size.y), 1f);
    }

    private SpriteRenderer FindShadowRenderer()
    {
        Transform child = transform.Find("Grounding Shadow");
        return child == null ? null : child.GetComponent<SpriteRenderer>();
    }

    private static Sprite GetShadowSprite()
    {
        if (sharedShadowSprite != null)
        {
            return sharedShadowSprite;
        }

        const int textureWidth = 96;
        const int textureHeight = 32;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "GroundingShadowBlob";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(textureWidth * 0.5f, textureHeight * 0.5f);
        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = (x + 0.5f - center.x) / center.x;
                float ny = (y + 0.5f - center.y) / center.y;
                float distance = (nx * nx) + (ny * ny * 2.35f);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        sharedShadowSprite = Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight),
                                           new Vector2(0.5f, 0.5f), 96f);
        sharedShadowSprite.name = "GroundingShadowBlob";
        return sharedShadowSprite;
    }
}
}
