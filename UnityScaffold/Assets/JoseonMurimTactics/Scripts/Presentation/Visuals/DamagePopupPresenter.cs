using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class DamagePopupPresenter : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float lifetime = 0.70f;

    private Canvas screenCanvas;
    private RectTransform screenRoot;
    private Font screenFont;

    public void ShowDamage(Vector3 worldPosition, int amount, bool critical = false)
    {
        Show(worldPosition, amount.ToString(), critical ? new Color(1f, 0.35f, 0.12f, 1f) : Color.white,
             critical ? 1.22f : 1f);
    }

    public void ShowMiss(Vector3 worldPosition)
    {
        Show(worldPosition, "MISS", new Color(0.72f, 0.82f, 0.92f, 1f), 0.9f);
    }

    public void ShowHeal(Vector3 worldPosition, int amount)
    {
        Show(worldPosition, "+" + amount, new Color(0.62f, 1f, 0.70f, 1f), 1f);
    }

    public void ShowCounter(Vector3 worldPosition)
    {
        Show(worldPosition, "COUNTER", new Color(0.95f, 0.86f, 0.36f, 1f), 0.82f);
    }

    public void Show(Vector3 worldPosition, string text, Color color, float scale = 1f)
    {
        GameObject popup = new GameObject("DamagePopup");
        popup.transform.SetParent(transform, false);
        popup.transform.position = worldPosition + new Vector3(0f, 1.02f, -0.14f);
        popup.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale * 1.24f);

        TextMeshPro textMesh = popup.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 3.7f;
        textMesh.fontStyle = FontStyles.Bold;
        textMesh.outlineWidth = 0.18f;
        textMesh.outlineColor = new Color(0.06f, 0.035f, 0.02f, 0.92f);
        textMesh.color = color;
        if (font != null)
        {
            textMesh.font = font;
        }

        MeshRenderer renderer = popup.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Characters";
            renderer.sortingOrder = 12000;
        }

        StartCoroutine(AnimatePopup(popup.transform, textMesh, color));
        ShowScreenPopup(worldPosition, text, color, scale);
    }

    private IEnumerator AnimatePopup(Transform popup, TMP_Text text, Color color)
    {
        Vector3 start = popup.position;
        float elapsed = 0f;
        while (elapsed < lifetime)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, lifetime));
            popup.position = start + new Vector3(0f, Mathf.Lerp(0f, 0.42f, t), 0f);
            color.a = 1f - Mathf.SmoothStep(0.55f, 1f, t);
            text.color = color;
            yield return null;
        }

        Destroy(popup.gameObject);
    }

    private void ShowScreenPopup(Vector3 worldPosition, string text, Color color, float scale)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        EnsureScreenCanvas();
        if (screenRoot == null)
        {
            return;
        }

        Vector3 viewportPosition = camera.WorldToViewportPoint(worldPosition + new Vector3(0f, 1.1f, 0f));
        if (viewportPosition.z < 0f)
        {
            return;
        }

        viewportPosition.x = Mathf.Clamp01(viewportPosition.x);
        viewportPosition.y = Mathf.Clamp01(viewportPosition.y);

        GameObject popupObject = new GameObject("ScreenDamagePopup", typeof(RectTransform), typeof(Text), typeof(Shadow), typeof(Outline));
        popupObject.transform.SetParent(screenRoot, false);
        RectTransform rect = popupObject.GetComponent<RectTransform>();
        Vector2 anchor = new Vector2(viewportPosition.x, viewportPosition.y);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(260f, 108f);
        rect.anchoredPosition = new Vector2(0f, 42f);

        Text uiText = popupObject.GetComponent<Text>();
        uiText.text = text;
        uiText.font = ScreenFont();
        uiText.fontSize = Mathf.RoundToInt(Mathf.Lerp(58f, 84f, Mathf.Clamp01(scale - 0.8f)));
        uiText.fontStyle = FontStyle.Bold;
        uiText.alignment = TextAnchor.MiddleCenter;
        uiText.horizontalOverflow = HorizontalWrapMode.Overflow;
        uiText.verticalOverflow = VerticalWrapMode.Overflow;
        uiText.raycastTarget = false;
        uiText.color = color;

        Shadow shadow = popupObject.GetComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.92f);
        shadow.effectDistance = new Vector2(3f, -3f);

        Outline outline = popupObject.GetComponent<Outline>();
        outline.effectColor = new Color(0.08f, 0.035f, 0.01f, 0.92f);
        outline.effectDistance = new Vector2(2f, -2f);

        StartCoroutine(AnimateScreenPopup(rect, uiText, color));
    }

    private void EnsureScreenCanvas()
    {
        if (screenCanvas != null && screenRoot != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("DamagePopupScreenCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        screenCanvas = canvasObject.GetComponent<Canvas>();
        screenCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        screenCanvas.sortingOrder = 9600;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        screenRoot = canvasObject.GetComponent<RectTransform>();
    }

    private IEnumerator AnimateScreenPopup(RectTransform rect, Text text, Color color)
    {
        Vector2 start = rect.anchoredPosition;
        float elapsed = 0f;
        float duration = Mathf.Max(0.1f, lifetime * 0.95f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float pop = Mathf.Sin(Mathf.Clamp01(t * 2.8f) * Mathf.PI);
            rect.anchoredPosition = start + new Vector2(0f, Mathf.Lerp(0f, 72f, t));
            rect.localScale = Vector3.one * (1f + (0.18f * pop));
            color.a = 1f - Mathf.SmoothStep(0.62f, 1f, t);
            text.color = color;
            yield return null;
        }

        Destroy(rect.gameObject);
    }

    private Font ScreenFont()
    {
        if (screenFont != null)
        {
            return screenFont;
        }

        string[] candidates = { "Maplestory OTF", "Malgun Gothic", "Arial" };
        foreach (string candidate in candidates)
        {
            try
            {
                screenFont = Font.CreateDynamicFontFromOSFont(candidate, 48);
            }
            catch
            {
                screenFont = null;
            }

            if (screenFont != null)
            {
                return screenFont;
            }
        }

        screenFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        return screenFont;
    }
}
}
