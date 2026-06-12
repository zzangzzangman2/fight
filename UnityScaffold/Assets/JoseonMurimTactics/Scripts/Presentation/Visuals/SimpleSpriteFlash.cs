using System.Collections;
using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class SimpleSpriteFlash : MonoBehaviour
{
    [SerializeField] private Color flashColor = Color.white;
    [SerializeField] private float flashSeconds = 0.08f;

    private SpriteRenderer[] renderers;
    private Coroutine active;

    private void Awake()
    {
        renderers = GetComponentsInChildren<SpriteRenderer>(true);
    }

    public void Flash()
    {
        if (active != null)
        {
            StopCoroutine(active);
        }

        active = StartCoroutine(FlashRoutine());
    }

    public void Flash(Color color, float seconds)
    {
        flashColor = color;
        flashSeconds = Mathf.Max(0.01f, seconds);
        Flash();
    }

    private IEnumerator FlashRoutine()
    {
        if (renderers == null || renderers.Length == 0)
        {
            renderers = GetComponentsInChildren<SpriteRenderer>(true);
        }

        Color[] original = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            original[i] = renderers[i] == null ? Color.white : renderers[i].color;
            if (renderers[i] != null)
            {
                renderers[i].color = flashColor;
            }
        }

        yield return new WaitForSeconds(flashSeconds);

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].color = original[i];
            }
        }

        active = null;
    }
}
}
