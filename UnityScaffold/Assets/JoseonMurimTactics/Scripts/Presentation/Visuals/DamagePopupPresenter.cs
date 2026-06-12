using System.Collections;
using TMPro;
using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class DamagePopupPresenter : MonoBehaviour
{
    [SerializeField] private TMP_FontAsset font;
    [SerializeField] private float lifetime = 0.70f;

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
        popup.transform.position = worldPosition + new Vector3(0f, 0.68f, -0.08f);
        popup.transform.localScale = Vector3.one * Mathf.Max(0.1f, scale);

        TextMeshPro textMesh = popup.AddComponent<TextMeshPro>();
        textMesh.text = text;
        textMesh.alignment = TextAlignmentOptions.Center;
        textMesh.fontSize = 2.6f;
        textMesh.color = color;
        if (font != null)
        {
            textMesh.font = font;
        }

        MeshRenderer renderer = popup.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 9000;
        }

        StartCoroutine(AnimatePopup(popup.transform, textMesh, color));
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
}
}
