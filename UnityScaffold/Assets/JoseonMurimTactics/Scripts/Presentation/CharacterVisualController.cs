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
public sealed class CharacterVisualController : MonoBehaviour, ICombatAnimationEventReceiver
{
    public CharacterVisualData visual;
    public CharacterOutfitData outfitOverride;
    public SpriteRenderer bodyRenderer;
    public SpriteRenderer baseLayerRenderer;
    public SpriteRenderer outfitLayerRenderer;
    public SpriteRenderer hairLayerRenderer;
    public SpriteRenderer faceLayerRenderer;
    public SpriteRenderer weaponLayerRenderer;
    public SpriteRenderer accessoryLayerRenderer;
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
    private float moveStridePhase = -1f;
    private float moveStridePhaseSetAt = -1f;

    private static Sprite ovalSprite;
    private static Sprite slashSprite;
    private static Sprite skillBurstSprite;
    private static Sprite guardRingSprite;
    private static Sprite impactBurstSprite;
    private static Sprite footstepDustSprite;

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
        FaceDirection(new Vector2(worldPosition.x - transform.position.x, worldPosition.y - transform.position.y));
    }

    public void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            facingSign = direction.x < 0f ? -1f : 1f;
        }
    }

    public void SetOutfit(CharacterOutfitData outfit)
    {
        outfitOverride = outfit;
        ApplyVisual();
    }

    public void SetMoveStridePhase(float phase)
    {
        moveStridePhase = phase;
        moveStridePhaseSetAt = Time.time;
    }

    public CombatActionTimeline CreateTimeline(bool special)
    {
        return new CombatActionTimeline(visual == null ? null : visual.weaponAnimationSet, special);
    }

    public float WalkSecondsPerTile()
    {
        return visual == null ? 0.24f : Mathf.Max(0.05f, visual.WalkSecondsPerTile);
    }

    public float MoveSettleTime()
    {
        return visual == null ? 0.10f : Mathf.Max(0f, visual.MoveSettleTime);
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
            PlayState(CharacterBattleVisualState.Attack, CreateTimeline(false).Duration);
        }
    }

    public void PlaySkill()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Skill, CreateTimeline(true).Duration);
        }
    }

    public void PlayHit()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Hit, 0.30f);
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

            ApplyLayerSprites();
            return;
        }

        bodyRenderer.sprite = SelectStateSprite();
        bodyRenderer.color = visual.normalTint;
        bodyRenderer.flipX = false;
        ApplyLayerSprites();
        ApplyLayerTint(visual.normalTint);

        if (animator != null)
        {
            animator.runtimeAnimatorController = visual.animatorController;
            animator.enabled = visual.animatorController != null;
        }

        Sprite fitSprite = bodyRenderer.sprite != null ? bodyRenderer.sprite : visual.fullBodySprite;
        float scale = 1f;
        if (fitSprite != null && fitSprite.bounds.size.y > 0.01f)
        {
            scale = visual.heightInTiles / fitSprite.bounds.size.y;
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
        if (state != CharacterBattleVisualState.Move)
        {
            moveStridePhase = -1f;
            moveStridePhaseSetAt = -1f;
        }

        if (bodyRenderer != null)
        {
            bodyRenderer.sprite = SelectStateSprite();
            ApplyLayerSprites();
        }
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

        bodyRenderer.sprite = SelectStateSprite();
        ApplyLayerSprites();
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
            float stride01 = MoveStride01(stateAge);
            float step = Mathf.Sin(stride01 * Mathf.PI * 2f);
            float hop = Mathf.Abs(step);
            float planted = 1f - Mathf.SmoothStep(0.08f, 0.32f, Mathf.Abs(step));
            localPosition.x += facingSign * step * 0.030f;
            localPosition.y += hop * 0.040f;
            localScale.x *= 1f - hop * 0.025f;
            localScale.y *= 1f + hop * 0.035f;
            rotation = (-facingSign * visual.moveLeanDegrees) + (facingSign * step * 2.4f);
            if (planted > 0.45f)
            {
                showEffect = true;
                effectSprite = GetFootstepDustSprite();
                effectPosition = new Vector3(-facingSign * 0.18f, 0.10f, -0.02f);
                effectScale = Vector3.one * (0.32f + (0.16f * planted));
                effectColor = Color.Lerp(new Color(0.78f, 0.88f, 0.94f, 0.28f + (0.22f * planted)),
                                         ElementPrimary(0.44f), 0.45f);
            }
            break;
        case CharacterBattleVisualState.Attack:
            float lunge = Mathf.Sin(progress * Mathf.PI);
            localPosition.x += facingSign * AttackLunge(false) * lunge;
            localPosition.y += 0.025f * lunge;
            localScale.x *= 1f + 0.06f * lunge;
            localScale.y *= 1f - 0.025f * lunge;
            rotation = -facingSign * 8f * lunge;
            showEffect = progress > 0.16f && progress < 0.92f;
            effectSprite = GetSlashSprite();
            effectPosition = new Vector3(facingSign * (0.34f + (0.08f * lunge)), 0.56f, -0.02f);
            effectScale = new Vector3(0.72f, 0.52f, 1f);
            effectRotation = facingSign < 0f ? 180f : 0f;
            effectColor = ElementPrimary(0.90f);
            break;
        case CharacterBattleVisualState.Skill:
            float skill = Mathf.Sin(progress * Mathf.PI);
            localPosition.x += facingSign * AttackLunge(true) * 0.55f * skill;
            localPosition.y += 0.06f * skill;
            localScale *= 1f + visual.skillPulseScale * skill;
            rotation = Mathf.Sin(progress * Mathf.PI * 2f) * 3.5f;
            tint = Color.Lerp(visual.normalTint, visual.guardTint, 0.42f * skill);
            showEffect = true;
            effectSprite = GetSkillBurstSprite();
            effectPosition = new Vector3(0f, 0.58f, -0.03f);
            effectScale = Vector3.one * (0.72f + 0.32f * skill);
            effectRotation = time * 18f;
            effectColor = Color.Lerp(ElementPrimary(0.42f + 0.30f * skill), ElementSecondary(0.42f + 0.30f * skill), 0.38f);
            break;
        case CharacterBattleVisualState.Hit:
            float hit = Mathf.Sin(progress * Mathf.PI);
            localPosition.x -= facingSign * visual.hitRecoil * hit;
            localPosition.y += 0.018f * hit;
            rotation = facingSign * 10f * hit;
            tint = Color.Lerp(visual.normalTint, visual.hitTint, hit);
            showEffect = progress < 0.82f;
            effectSprite = GetImpactBurstSprite();
            effectPosition = new Vector3(0f, 0.58f, -0.03f);
            effectScale = Vector3.one * (0.48f + 0.20f * hit);
            effectColor = ElementSecondary(0.62f);
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
            effectColor = ElementPrimary(0.34f + 0.30f * guard);
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
        ApplyLayerFlip(facingSign < 0f);
        ApplyLayerTint(tint);
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
        baseLayerRenderer = baseLayerRenderer != null ? baseLayerRenderer : EnsureChildRenderer("Layer_Base", bodyRenderer.transform);
        outfitLayerRenderer = outfitLayerRenderer != null ? outfitLayerRenderer : EnsureChildRenderer("Layer_Outfit", bodyRenderer.transform);
        hairLayerRenderer = hairLayerRenderer != null ? hairLayerRenderer : EnsureChildRenderer("Layer_Hair", bodyRenderer.transform);
        faceLayerRenderer = faceLayerRenderer != null ? faceLayerRenderer : EnsureChildRenderer("Layer_Face", bodyRenderer.transform);
        weaponLayerRenderer = weaponLayerRenderer != null ? weaponLayerRenderer : EnsureChildRenderer("Layer_Weapon", bodyRenderer.transform);
        accessoryLayerRenderer = accessoryLayerRenderer != null ? accessoryLayerRenderer : EnsureChildRenderer("Layer_Accessory", bodyRenderer.transform);
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
        return EnsureChildRenderer(childName, transform);
    }

    private SpriteRenderer EnsureChildRenderer(string childName, Transform parent)
    {
        Transform child = parent.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(parent, false);
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
        SetLayerSorting(baseLayerRenderer, order);
        SetLayerSorting(outfitLayerRenderer, order + 1);
        SetLayerSorting(hairLayerRenderer, order + 2);
        SetLayerSorting(faceLayerRenderer, order + 3);
        SetLayerSorting(weaponLayerRenderer, order + 4);
        SetLayerSorting(accessoryLayerRenderer, order + 5);
        effectRenderer.sortingOrder = order + 6;
    }

    private Sprite SelectStateSprite()
    {
        if (visual == null)
        {
            return null;
        }

        switch (visualState)
        {
        case CharacterBattleVisualState.Move:
            return SelectMoveCycleSprite();
        case CharacterBattleVisualState.Attack:
            return AttackPoseSprite() != null ? AttackPoseSprite() : SelectIdleFallback();
        case CharacterBattleVisualState.Skill:
            return SkillPoseSprite() != null ? SkillPoseSprite() :
                   AttackPoseSprite() != null ? AttackPoseSprite() : SelectIdleFallback();
        case CharacterBattleVisualState.Hit:
            return HitPoseSprite() != null ? HitPoseSprite() : SelectIdleFallback();
        case CharacterBattleVisualState.Guard:
            return AttackPoseSprite() != null ? AttackPoseSprite() : SelectIdleFallback();
        case CharacterBattleVisualState.Defeat:
            return DefeatedPoseSprite() != null ? DefeatedPoseSprite() : SelectIdleFallback();
        case CharacterBattleVisualState.Victory:
            return SkillPoseSprite() != null ? SkillPoseSprite() : SelectIdleFallback();
        case CharacterBattleVisualState.Wait:
            return ActedPoseSprite() != null ? ActedPoseSprite() : SelectIdleFallback();
        default:
            return SelectIdleFallback();
        }
    }

    private Sprite SelectIdleFallback()
    {
        if (visual == null)
        {
            return null;
        }

        Sprite idle = IdlePoseSprite();
        CharacterOutfitData outfit = ActiveOutfit();
        return idle != null ? idle :
               outfit != null && outfit.fullBodySprite != null ? outfit.fullBodySprite :
               visual.fullBodySprite != null ? visual.fullBodySprite :
               outfit != null && outfit.bustSprite != null ? outfit.bustSprite :
               visual.bustSprite != null ? visual.bustSprite :
               outfit != null && outfit.portraitSprite != null ? outfit.portraitSprite : visual.portraitSprite;
    }

    private Sprite SelectMoveCycleSprite()
    {
        Sprite move = MovePoseSprite();
        Sprite idle = SelectIdleFallback();
        if (move == null)
        {
            return idle;
        }

        if (idle == null || idle == move)
        {
            return move;
        }

        float stride = Mathf.Abs(Mathf.Sin(MoveStride01(Time.time - stateStartedAt) * Mathf.PI * 2f));
        return stride > 0.38f ? move : idle;
    }

    private CharacterOutfitData ActiveOutfit()
    {
        return outfitOverride != null ? outfitOverride : visual != null ? visual.defaultOutfit : null;
    }

    private Sprite IdlePoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        return outfit != null && outfit.idlePoseSprite != null ? outfit.idlePoseSprite : visual.idlePoseSprite;
    }

    private Sprite MovePoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        return outfit != null && outfit.movePoseSprite != null ? outfit.movePoseSprite : visual.movePoseSprite;
    }

    private Sprite AttackPoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        return outfit != null && outfit.attackPoseSprite != null ? outfit.attackPoseSprite : visual.attackPoseSprite;
    }

    private Sprite SkillPoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        return outfit != null && outfit.skillPoseSprite != null ? outfit.skillPoseSprite : visual.skillPoseSprite;
    }

    private Sprite HitPoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        return outfit != null && outfit.hitPoseSprite != null ? outfit.hitPoseSprite : visual.hitPoseSprite;
    }

    private Sprite DefeatedPoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        return outfit != null && outfit.defeatedPoseSprite != null ? outfit.defeatedPoseSprite : visual.defeatedPoseSprite;
    }

    private Sprite ActedPoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        return outfit != null && outfit.actedPoseSprite != null ? outfit.actedPoseSprite : visual.actedPoseSprite;
    }

    private float MoveStride01(float stateAge)
    {
        if (moveStridePhaseSetAt >= 0f && Time.time - moveStridePhaseSetAt <= 0.16f)
        {
            return Mathf.Repeat(moveStridePhase, 1f);
        }

        return Mathf.Repeat(stateAge * 3.75f, 1f);
    }

    private void ApplyLayerSprites()
    {
        if (visual == null)
        {
            SetLayerSprite(baseLayerRenderer, null);
            SetLayerSprite(outfitLayerRenderer, null);
            SetLayerSprite(hairLayerRenderer, null);
            SetLayerSprite(faceLayerRenderer, null);
            SetLayerSprite(weaponLayerRenderer, null);
            SetLayerSprite(accessoryLayerRenderer, null);
            return;
        }

        CharacterOutfitData outfit = ActiveOutfit();
        bool hasLayerSprites = outfit != null && outfit.useLayeredSprites &&
                               (outfit.baseBodyLayer != null || outfit.outfitLayer != null ||
                                outfit.hairLayer != null || outfit.faceLayer != null ||
                                outfit.weaponLayer != null || outfit.accessoryLayer != null);
        if (bodyRenderer != null)
        {
            bodyRenderer.enabled = !hasLayerSprites;
        }

        SetLayerSprite(baseLayerRenderer, hasLayerSprites ? outfit.baseBodyLayer : null);
        SetLayerSprite(outfitLayerRenderer, hasLayerSprites ? outfit.outfitLayer : null);
        SetLayerSprite(hairLayerRenderer, hasLayerSprites ? outfit.hairLayer : null);
        SetLayerSprite(faceLayerRenderer, hasLayerSprites ? outfit.faceLayer : null);
        SetLayerSprite(weaponLayerRenderer, hasLayerSprites ? outfit.weaponLayer : null);
        SetLayerSprite(accessoryLayerRenderer, hasLayerSprites ? outfit.accessoryLayer : null);
    }

    private static void SetLayerSprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sprite = sprite;
        renderer.enabled = sprite != null;
        renderer.transform.localPosition = Vector3.zero;
        renderer.transform.localRotation = Quaternion.identity;
        renderer.transform.localScale = Vector3.one;
    }

    private void ApplyLayerTint(Color tint)
    {
        SetLayerTint(baseLayerRenderer, tint);
        SetLayerTint(outfitLayerRenderer, tint);
        SetLayerTint(hairLayerRenderer, tint);
        SetLayerTint(faceLayerRenderer, tint);
        SetLayerTint(weaponLayerRenderer, tint);
        SetLayerTint(accessoryLayerRenderer, tint);
    }

    private static void SetLayerTint(SpriteRenderer renderer, Color tint)
    {
        if (renderer != null)
        {
            renderer.color = tint;
        }
    }

    private void ApplyLayerFlip(bool flipX)
    {
        SetLayerFlip(baseLayerRenderer, flipX);
        SetLayerFlip(outfitLayerRenderer, flipX);
        SetLayerFlip(hairLayerRenderer, flipX);
        SetLayerFlip(faceLayerRenderer, flipX);
        SetLayerFlip(weaponLayerRenderer, flipX);
        SetLayerFlip(accessoryLayerRenderer, flipX);
    }

    private static void SetLayerFlip(SpriteRenderer renderer, bool flipX)
    {
        if (renderer != null)
        {
            renderer.flipX = flipX;
        }
    }

    private void SetLayerSorting(SpriteRenderer renderer, int order)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sortingLayerName = sortingLayerName;
        renderer.sortingOrder = order;
    }

    private Color ElementPrimary(float alpha)
    {
        Color color = visual != null && visual.weaponAnimationSet != null
                          ? visual.weaponAnimationSet.primaryEffectColor
                          : new Color(0.55f, 0.98f, 1f, 1f);
        color.a = alpha;
        return color;
    }

    private Color ElementSecondary(float alpha)
    {
        Color color = visual != null && visual.weaponAnimationSet != null
                          ? visual.weaponAnimationSet.secondaryEffectColor
                          : Color.white;
        color.a = alpha;
        return color;
    }

    private float AttackLunge(bool special)
    {
        CombatActionTimeline timeline = CreateTimeline(special);
        return Mathf.Max(0f, timeline.LungeDistance);
    }

    public void OnAttackHitFrame()
    {
    }

    public void OnSkillHitFrame()
    {
    }

    public void OnProjectileSpawnFrame()
    {
    }

    public void OnFootstepFrame()
    {
    }

    public void OnAnimationComplete()
    {
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

    private static Sprite GetImpactBurstSprite()
    {
        if (impactBurstSprite != null)
        {
            return impactBurstSprite;
        }

        Texture2D texture = new Texture2D(96, 96, TextureFormat.RGBA32, false);
        texture.name = "GeneratedImpactBurst";
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
                float star = Mathf.Clamp01((0.18f - Mathf.Abs(Mathf.Sin(angle * 5f) * r - 0.22f)) * 4.6f);
                float core = Mathf.Clamp01((0.36f - r) * 3.2f);
                float alpha = Mathf.Clamp01((star * (1f - r)) + (core * 0.72f));
                texture.SetPixel(x, y, new Color(1f, 0.86f, 0.42f, alpha));
            }
        }

        texture.Apply();
        impactBurstSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        impactBurstSprite.name = "GeneratedImpactBurst";
        return impactBurstSprite;
    }

    private static Sprite GetFootstepDustSprite()
    {
        if (footstepDustSprite != null)
        {
            return footstepDustSprite;
        }

        Texture2D texture = new Texture2D(96, 48, TextureFormat.RGBA32, false);
        texture.name = "GeneratedFootstepDust";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                float left = Mathf.Clamp01((0.32f - ((nx + 0.35f) * (nx + 0.35f) + (ny * ny * 2.2f))) * 3.2f);
                float right = Mathf.Clamp01((0.24f - ((nx - 0.22f) * (nx - 0.22f) + ((ny + 0.05f) * (ny + 0.05f) * 2.4f))) * 3.8f);
                float alpha = Mathf.Clamp01((left + right) * Mathf.SmoothStep(-0.88f, 0.35f, ny));
                texture.SetPixel(x, y, new Color(0.82f, 0.88f, 0.92f, alpha * 0.58f));
            }
        }

        texture.Apply();
        footstepDustSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        footstepDustSprite.name = "GeneratedFootstepDust";
        return footstepDustSprite;
    }
}
}
