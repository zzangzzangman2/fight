using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class CharacterVisualController : MonoBehaviour
{
    public CharacterVisualData visual;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer shadowRenderer;
    public SpriteRenderer selectionRenderer;
    public Animator animator;
    public string sortingLayerName = "Characters";
    public int baseSortingOrder = 1000;

    private Transform bodyTransform;
    private Vector3 baseBodyPosition;
    private Vector3 baseBodyScale = Vector3.one;
    private float phaseSeed;
    private bool selected;
    private bool defeated;

    private static Sprite ovalSprite;

    private void Awake()
    {
        EnsureRenderers();
        phaseSeed = Random.value * Mathf.PI * 2f;
        ApplyVisual();
    }

    private void LateUpdate()
    {
        if (bodyTransform == null || visual == null)
        {
            return;
        }

        float pulse = Mathf.Sin((Time.time * Mathf.Max(0.1f, visual.idleSpeed)) + phaseSeed);
        bodyTransform.localPosition = baseBodyPosition + new Vector3(0f, pulse * visual.idleAmplitude, 0f);
        float breath = 1f + (Mathf.Cos((Time.time * visual.idleSpeed * 0.7f) + phaseSeed) * visual.breathingScale);
        bodyTransform.localScale = new Vector3(baseBodyScale.x * breath, baseBodyScale.y, baseBodyScale.z);

        UpdateStateTint();
        UpdateSorting();
    }

    public void Bind(CombatantData combatant, bool isSelected)
    {
        visual = combatant != null ? combatant.visual : null;
        selected = isSelected;
        defeated = false;
        ApplyVisual();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = selected && !defeated;
        }
    }

    public void SetDefeated(bool value)
    {
        defeated = value;
        if (selectionRenderer != null && defeated)
        {
            selectionRenderer.enabled = false;
        }
    }

    public void ApplyVisual()
    {
        EnsureRenderers();

        if (visual == null)
        {
            if (bodyRenderer != null)
            {
                bodyRenderer.sprite = null;
            }
            return;
        }

        bodyRenderer.sprite = visual.fullBodySprite;
        bodyRenderer.color = visual.normalTint;

        if (animator != null)
        {
            animator.runtimeAnimatorController = visual.animatorController;
            animator.enabled = visual.animatorController != null;
        }

        float scale = 1f;
        if (visual.fullBodySprite != null && visual.fullBodySprite.bounds.size.y > 0.01f)
        {
            scale = visual.heightInTiles / visual.fullBodySprite.bounds.size.y;
        }

        bodyTransform = bodyRenderer.transform;
        baseBodyPosition = new Vector3(visual.spriteOffset.x, visual.spriteOffset.y, 0f);
        baseBodyScale = Vector3.one * scale;
        bodyTransform.localPosition = baseBodyPosition;
        bodyTransform.localScale = baseBodyScale;

        shadowRenderer.sprite = GetOvalSprite();
        shadowRenderer.transform.localPosition = Vector3.zero;
        shadowRenderer.transform.localScale = new Vector3(visual.shadowWidth, visual.shadowHeight, 1f);
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.34f);

        selectionRenderer.sprite = GetOvalSprite();
        selectionRenderer.transform.localPosition = new Vector3(0f, 0.02f, 0f);
        selectionRenderer.transform.localScale =
            new Vector3(visual.shadowWidth * 1.15f, visual.shadowHeight * 1.35f, 1f);
        selectionRenderer.color = new Color(1f, 0.78f, 0.18f, 0.62f);
        selectionRenderer.enabled = selected && !defeated;

        UpdateStateTint();
        UpdateSorting();
    }

    private void EnsureRenderers()
    {
        shadowRenderer = shadowRenderer != null ? shadowRenderer : EnsureChildRenderer("Shadow");
        bodyRenderer = bodyRenderer != null ? bodyRenderer : EnsureChildRenderer("FullBody");
        selectionRenderer = selectionRenderer != null ? selectionRenderer : EnsureChildRenderer("SelectionRing");

        bodyTransform = bodyRenderer.transform;
        animator = animator != null ? animator : bodyRenderer.GetComponent<Animator>();
        if (animator == null)
        {
            animator = bodyRenderer.gameObject.AddComponent<Animator>();
            animator.enabled = false;
        }
    }

    private SpriteRenderer EnsureChildRenderer(string childName)
    {
        Transform child = transform.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(transform, false);
        }

        SpriteRenderer renderer = child.GetComponent<SpriteRenderer>();
        if (renderer == null)
        {
            renderer = child.gameObject.AddComponent<SpriteRenderer>();
        }

        renderer.sortingLayerName = sortingLayerName;
        return renderer;
    }

    private void UpdateStateTint()
    {
        if (bodyRenderer == null || visual == null)
        {
            return;
        }

        bodyRenderer.color = defeated ? visual.defeatedTint : selected ? visual.selectedTint : visual.normalTint;
    }

    private void UpdateSorting()
    {
        int order = baseSortingOrder - Mathf.RoundToInt(transform.position.y * 100f) +
                    (visual != null ? visual.sortingOffset : 0);
        shadowRenderer.sortingLayerName = sortingLayerName;
        bodyRenderer.sortingLayerName = sortingLayerName;
        selectionRenderer.sortingLayerName = sortingLayerName;
        shadowRenderer.sortingOrder = order - 2;
        selectionRenderer.sortingOrder = order - 1;
        bodyRenderer.sortingOrder = order;
    }

    private static Sprite GetOvalSprite()
    {
        if (ovalSprite != null)
        {
            return ovalSprite;
        }

        Texture2D texture = new Texture2D(64, 16, TextureFormat.RGBA32, false);
        texture.name = "GeneratedCharacterOval";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                float d = (nx * nx) + (ny * ny);
                float alpha = Mathf.Clamp01((1f - d) * 2.6f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        ovalSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 64f);
        ovalSprite.name = "GeneratedCharacterOval";
        return ovalSprite;
    }
}
}
