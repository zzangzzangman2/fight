using UnityEngine;

namespace JoseonMurimTactics
{
public enum CharacterBattleVisualState
{
    Idle,
    SelectedIdle,
    Move,
    Attack,
    Skill,
    Hit,
    Guard,
    Defeat,
    Victory,
    Wait
}

[DisallowMultipleComponent]
public sealed class CharacterVisualController : MonoBehaviour
{
    public CharacterVisualData visual;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer shadowRenderer;
    public SpriteRenderer selectionRenderer;
    public SpriteRenderer effectRenderer;
    public Animator animator;
    public string sortingLayerName = "Characters";
    public int baseSortingOrder = 1000;

    private Transform bodyTransform;
    private Vector3 baseBodyPosition;
    private Vector3 baseBodyScale = Vector3.one;
    private float phaseSeed;
    private bool selected;
    private bool acted;
    private bool defeated;
    private float facingSign = 1f;
    private CharacterBattleVisualState visualState = CharacterBattleVisualState.Idle;
    private float stateStartedAt;
    private float stateDuration;

    private static Sprite ovalSprite;
    private static Sprite slashSprite;
    private static Sprite skillBurstSprite;
    private static Sprite guardRingSprite;

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

        UpdateStateLifetime();
        ApplyProceduralPose();
        UpdateSorting();
    }

    public void Bind(CombatantData combatant, bool isSelected)
    {
        visual = combatant != null ? combatant.visual : null;
        selected = isSelected;
        acted = false;
        defeated = false;
        visualState = selected ? CharacterBattleVisualState.SelectedIdle : CharacterBattleVisualState.Idle;
        ApplyVisual();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = selected && !defeated;
        }

        if (!defeated && !IsTransientState(visualState))
        {
            visualState = selected ? CharacterBattleVisualState.SelectedIdle :
                          acted ? CharacterBattleVisualState.Wait : CharacterBattleVisualState.Idle;
            stateDuration = 0f;
            stateStartedAt = Time.time;
        }
    }

    public void SetActed(bool value)
    {
        acted = value;
        if (!defeated && !IsTransientState(visualState))
        {
            visualState = acted ? CharacterBattleVisualState.Wait :
                          selected ? CharacterBattleVisualState.SelectedIdle : CharacterBattleVisualState.Idle;
            stateDuration = 0f;
            stateStartedAt = Time.time;
        }
    }

    public void SetDefeated(bool value)
    {
        defeated = value;
        if (selectionRenderer != null && defeated)
        {
            selectionRenderer.enabled = false;
        }

        if (defeated)
        {
            PlayState(CharacterBattleVisualState.Defeat, 0f);
        }
        else
        {
            PlayIdle();
        }
    }

    public void FaceToward(Vector3 worldPosition)
    {
        float dx = worldPosition.x - transform.position.x;
        if (Mathf.Abs(dx) > 0.01f)
        {
            facingSign = dx < 0f ? -1f : 1f;
        }
    }

    public void PlayIdle()
    {
        if (defeated)
        {
            return;
        }

        PlayState(selected ? CharacterBattleVisualState.SelectedIdle :
                  acted ? CharacterBattleVisualState.Wait : CharacterBattleVisualState.Idle, 0f);
    }

    public void PlayMove()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Move, 0f);
        }
    }

    public void PlayAttack()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Attack, 0.34f);
        }
    }

    public void PlaySkill()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Skill, 0.58f);
        }
    }

    public void PlayHit()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Hit, 0.28f);
        }
    }

    public void PlayGuard()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Guard, 0.45f);
        }
    }

    public void PlayWait()
    {
        if (!defeated)
        {
            acted = true;
            PlayState(CharacterBattleVisualState.Wait, 0f);
        }
    }

    public void PlayVictory()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Victory, 0f);
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

            if (effectRenderer != null)
            {
                effectRenderer.sprite = null;
                effectRenderer.enabled = false;
            }

            return;
        }

        bodyRenderer.sprite = visual.fullBodySprite;
        bodyRenderer.color = visual.normalTint;
        bodyRenderer.flipX = false;

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
        bodyTransform.localRotation = Quaternion.identity;
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

        effectRenderer.enabled = false;
        effectRenderer.color = Color.white;
        visualState = defeated ? CharacterBattleVisualState.Defeat :
                      selected ? CharacterBattleVisualState.SelectedIdle :
                      acted ? CharacterBattleVisualState.Wait : CharacterBattleVisualState.Idle;
        stateDuration = 0f;
        stateStartedAt = Time.time;

        ApplyProceduralPose();
        UpdateSorting();
    }

    private void PlayState(CharacterBattleVisualState state, float duration)
    {
        if (defeated && state != CharacterBattleVisualState.Defeat)
        {
            return;
        }

        visualState = state;
        stateDuration = Mathf.Max(0f, duration);
        stateStartedAt = Time.time;
    }

    private void UpdateStateLifetime()
    {
        if (stateDuration <= 0f || visualState == CharacterBattleVisualState.Defeat)
        {
            return;
        }

        if (Time.time - stateStartedAt < stateDuration)
        {
            return;
        }

        visualState = selected ? CharacterBattleVisualState.SelectedIdle :
                      acted ? CharacterBattleVisualState.Wait : CharacterBattleVisualState.Idle;
        stateDuration = 0f;
        stateStartedAt = Time.time;
    }

    private void ApplyProceduralPose()
    {
        float time = Time.time;
        float stateAge = time - stateStartedAt;
        float progress = stateDuration > 0f ? Mathf.Clamp01(stateAge / stateDuration) : 0f;
        float pulse = Mathf.Sin((time * Mathf.Max(0.1f, visual.idleSpeed)) + phaseSeed);
        float idleBob = pulse * visual.idleAmplitude;
        float breath = 1f + (Mathf.Cos((time * visual.idleSpeed * 0.7f) + phaseSeed) * visual.breathingScale);

        Vector3 localPosition = baseBodyPosition + new Vector3(0f, idleBob, 0f);
        Vector3 localScale = new Vector3(baseBodyScale.x * breath, baseBodyScale.y, baseBodyScale.z);
        float rotation = 0f;
        Color tint = visual.normalTint;
        float shadowAlpha = 0.34f;
        bool showEffect = false;
        Sprite effectSprite = null;
        Vector3 effectPosition = new Vector3(0f, 0.55f, -0.02f);
        Vector3 effectScale = Vector3.one;
        float effectRotation = 0f;
        Color effectColor = Color.white;

        bool selectedIdle = visualState == CharacterBattleVisualState.SelectedIdle ||
                            (selected && visualState == CharacterBattleVisualState.Idle);
        if (selectedIdle)
        {
            float selectPulse = Mathf.Abs(Mathf.Sin((time * 4.8f) + phaseSeed));
            localPosition.y += selectPulse * 0.035f;
            localScale.x *= 1f + selectPulse * 0.018f;
            tint = visual.selectedTint;
        }
        else if (acted || visualState == CharacterBattleVisualState.Wait)
        {
            tint = visual.actedTint;
            localPosition.y -= Mathf.Abs(idleBob) * 0.45f;
        }

        switch (visualState)
        {
        case CharacterBattleVisualState.Move:
            float step = Mathf.Sin(stateAge * 22f);
            float hop = Mathf.Abs(step);
            localPosition.x += facingSign * step * 0.025f;
            localPosition.y += hop * 0.035f;
            localScale.x *= 1f - hop * 0.025f;
            localScale.y *= 1f + hop * 0.035f;
            rotation = -facingSign * visual.moveLeanDegrees;
            break;
        case CharacterBattleVisualState.Attack:
            float lunge = Mathf.Sin(progress * Mathf.PI);
            localPosition.x += facingSign * visual.attackLunge * lunge;
            localPosition.y += 0.025f * lunge;
            localScale.x *= 1f + 0.06f * lunge;
            localScale.y *= 1f - 0.025f * lunge;
            rotation = -facingSign * 8f * lunge;
            showEffect = progress > 0.16f && progress < 0.92f;
            effectSprite = GetSlashSprite();
            effectPosition = new Vector3(facingSign * (0.34f + (0.08f * lunge)), 0.56f, -0.02f);
            effectScale = new Vector3(0.72f, 0.52f, 1f);
            effectRotation = facingSign < 0f ? 180f : 0f;
            effectColor = new Color(0.55f, 0.98f, 1f, 0.90f);
            break;
        case CharacterBattleVisualState.Skill:
            float skill = Mathf.Sin(progress * Mathf.PI);
            localPosition.y += 0.06f * skill;
            localScale *= 1f + visual.skillPulseScale * skill;
            rotation = Mathf.Sin(progress * Mathf.PI * 2f) * 3.5f;
            tint = Color.Lerp(visual.normalTint, visual.guardTint, 0.42f * skill);
            showEffect = true;
            effectSprite = GetSkillBurstSprite();
            effectPosition = new Vector3(0f, 0.58f, -0.03f);
            effectScale = Vector3.one * (0.72f + 0.32f * skill);
            effectRotation = time * 18f;
            effectColor = new Color(0.42f, 0.94f, 1f, 0.42f + 0.30f * skill);
            break;
        case CharacterBattleVisualState.Hit:
            float hit = Mathf.Sin(progress * Mathf.PI);
            localPosition.x -= facingSign * visual.hitRecoil * hit;
            localPosition.y += 0.018f * hit;
            rotation = facingSign * 10f * hit;
            tint = Color.Lerp(visual.normalTint, visual.hitTint, hit);
            break;
        case CharacterBattleVisualState.Guard:
            float guard = Mathf.Sin(progress * Mathf.PI);
            localScale.x *= 1f + 0.035f * guard;
            localScale.y *= 1f - 0.015f * guard;
            rotation = -facingSign * 3.5f;
            tint = Color.Lerp(visual.normalTint, visual.guardTint, 0.55f);
            showEffect = true;
            effectSprite = GetGuardRingSprite();
            effectPosition = new Vector3(0f, 0.55f, -0.03f);
            effectScale = Vector3.one * (0.56f + 0.14f * guard);
            effectColor = new Color(0.52f, 0.94f, 1f, 0.34f + 0.30f * guard);
            break;
        case CharacterBattleVisualState.Defeat:
            float fall = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(stateAge / 0.55f));
            localPosition.x -= facingSign * 0.18f * fall;
            localPosition.y -= 0.08f * fall;
            localScale.x *= 0.96f;
            localScale.y *= 0.72f + (0.28f * (1f - fall));
            rotation = facingSign * 72f * fall;
            tint = visual.defeatedTint;
            shadowAlpha = 0.18f;
            break;
        case CharacterBattleVisualState.Victory:
            float cheer = Mathf.Abs(Mathf.Sin((time * 5.6f) + phaseSeed));
            localPosition.y += cheer * 0.060f;
            rotation = Mathf.Sin((time * 5.6f) + phaseSeed) * 4f;
            tint = visual.selectedTint;
            break;
        case CharacterBattleVisualState.Wait:
            tint = visual.actedTint;
            break;
        }

        bodyRenderer.flipX = facingSign < 0f;
        bodyRenderer.color = tint;
        bodyTransform.localPosition = localPosition;
        bodyTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        bodyTransform.localScale = localScale;

        shadowRenderer.color = new Color(0f, 0f, 0f, shadowAlpha);
        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = selected && !defeated;
            float ringPulse = 0.54f + Mathf.Abs(Mathf.Sin((time * 4.3f) + phaseSeed)) * 0.22f;
            selectionRenderer.color = new Color(1f, 0.78f, 0.18f, ringPulse);
        }

        UpdateEffect(showEffect, effectSprite, effectPosition, effectScale, effectRotation, effectColor);
    }

    private void UpdateEffect(bool show, Sprite sprite, Vector3 localPosition, Vector3 localScale, float rotation,
                              Color color)
    {
        if (effectRenderer == null)
        {
            return;
        }

        effectRenderer.enabled = show && sprite != null;
        if (!effectRenderer.enabled)
        {
            return;
        }

        effectRenderer.sprite = sprite;
        effectRenderer.flipX = false;
        effectRenderer.transform.localPosition = localPosition;
        effectRenderer.transform.localScale = localScale;
        effectRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        effectRenderer.color = color;
    }

    private void EnsureRenderers()
    {
        shadowRenderer = shadowRenderer != null ? shadowRenderer : EnsureChildRenderer("Shadow");
        selectionRenderer = selectionRenderer != null ? selectionRenderer : EnsureChildRenderer("SelectionRing");
        bodyRenderer = bodyRenderer != null ? bodyRenderer : EnsureChildRenderer("FullBody");
        effectRenderer = effectRenderer != null ? effectRenderer : EnsureChildRenderer("StateEffect");

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

    private void UpdateSorting()
    {
        int order = baseSortingOrder - Mathf.RoundToInt(transform.position.y * 100f) +
                    (visual != null ? visual.sortingOffset : 0);
        shadowRenderer.sortingLayerName = sortingLayerName;
        bodyRenderer.sortingLayerName = sortingLayerName;
        selectionRenderer.sortingLayerName = sortingLayerName;
        effectRenderer.sortingLayerName = sortingLayerName;
        shadowRenderer.sortingOrder = order - 2;
        selectionRenderer.sortingOrder = order - 1;
        bodyRenderer.sortingOrder = order;
        effectRenderer.sortingOrder = order + 1;
    }

    private static bool IsTransientState(CharacterBattleVisualState state)
    {
        return state == CharacterBattleVisualState.Attack ||
               state == CharacterBattleVisualState.Skill ||
               state == CharacterBattleVisualState.Hit ||
               state == CharacterBattleVisualState.Guard;
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

    private static Sprite GetSlashSprite()
    {
        if (slashSprite != null)
        {
            return slashSprite;
        }

        Texture2D texture = new Texture2D(160, 80, TextureFormat.RGBA32, false);
        texture.name = "GeneratedSlashArc";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                float arc = Mathf.Abs((nx * nx * 0.95f) + ((ny + 0.78f) * (ny + 0.78f) * 2.4f) - 1f);
                float alpha = Mathf.Clamp01((0.16f - arc) * 8f) * Mathf.SmoothStep(-0.9f, 0.15f, nx) *
                              (1f - Mathf.SmoothStep(0.86f, 1f, nx));
                Color color = Color.Lerp(new Color(0.18f, 0.86f, 1f, alpha), new Color(1f, 1f, 1f, alpha),
                                         Mathf.Clamp01(1f - arc * 8f));
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        slashSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 120f);
        slashSprite.name = "GeneratedSlashArc";
        return slashSprite;
    }

    private static Sprite GetSkillBurstSprite()
    {
        if (skillBurstSprite != null)
        {
            return skillBurstSprite;
        }

        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.name = "GeneratedSkillBurst";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float ring = Mathf.Clamp01((0.07f - Mathf.Abs(r - 0.58f)) * 14f);
                float spokes = Mathf.Clamp01(Mathf.Abs(Mathf.Sin(angle * 8f)) * (1f - r));
                float core = Mathf.Clamp01((0.34f - r) * 2.8f);
                float alpha = Mathf.Clamp01((ring * 0.75f) + (spokes * 0.16f) + (core * 0.55f));
                texture.SetPixel(x, y, new Color(0.46f, 0.96f, 1f, alpha));
            }
        }

        texture.Apply();
        skillBurstSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        skillBurstSprite.name = "GeneratedSkillBurst";
        return skillBurstSprite;
    }

    private static Sprite GetGuardRingSprite()
    {
        if (guardRingSprite != null)
        {
            return guardRingSprite;
        }

        Texture2D texture = new Texture2D(128, 128, TextureFormat.RGBA32, false);
        texture.name = "GeneratedGuardRing";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float ring = Mathf.Clamp01((0.06f - Mathf.Abs(r - 0.76f)) * 18f);
                float inner = Mathf.Clamp01((0.05f - Mathf.Abs(r - 0.54f)) * 16f);
                float alpha = Mathf.Clamp01((ring * 0.85f) + (inner * 0.42f));
                texture.SetPixel(x, y, new Color(0.54f, 0.95f, 1f, alpha));
            }
        }

        texture.Apply();
        guardRingSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        guardRingSprite.name = "GeneratedGuardRing";
        return guardRingSprite;
    }
}
}
