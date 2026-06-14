using UnityEngine;

using System.Collections.Generic;

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
    Wait,
    TurnStart
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
    public SpriteRenderer emotionFaceRenderer;
    public SpriteRenderer weaponLayerRenderer;
    public SpriteRenderer accessoryLayerRenderer;
    public SpriteRenderer shadowRenderer;
    public SpriteRenderer shadowCoreRenderer;
    public SpriteRenderer castShadowRenderer;
    public SpriteRenderer groundBlendRenderer;
    public SpriteRenderer inkRimRenderer;
    public SpriteRenderer footOcclusionRenderer;
    public SpriteRenderer leftFootRenderer;
    public SpriteRenderer rightFootRenderer;
    public SpriteRenderer selectionRenderer;
    public SpriteRenderer motionTrailRenderer;
    public SpriteRenderer effectRenderer;
    public Animator animator;
    public string sortingLayerName = "Characters";
    public int baseSortingOrder = 1000;

    public int CurrentBodySortingOrder => bodyRenderer == null ? baseSortingOrder : bodyRenderer.sortingOrder;

    private Transform bodyTransform;
    private Vector3 baseBodyPosition;
    private Vector3 baseBodyScale = Vector3.one;
    private float bodyFootBoundsMinY;
    private float phaseSeed;
    private bool selected;
    private bool acted;
    private bool defeated;
    private float facingSign = 1f;
    private Vector2 facingVector = Vector2.down;
    private CharacterBattleVisualState visualState = CharacterBattleVisualState.Idle;
    private float stateStartedAt;
    private float stateDuration;
    private float moveStridePhase = -1f;
    private float moveStridePhaseSetAt = -1f;
    private bool lowHpActive;
    private float clickReactionStartedAt = -999f;
    private float assistReactionStartedAt = -999f;
    private CharacterEmotion activeEmotion = CharacterEmotion.Neutral;
    private float emotionExpiresAt = -1f;
    private float nextBlinkAt = -1f;
    private float blinkUntil = -1f;
    private CharacterSpriteAnimationClipData activeCueClip;
    private bool pixelMode;
    private int activeCueLoopIndex = -1;
    private readonly HashSet<string> firedClipCues = new HashSet<string>();
    private Sprite cueEffectSprite;
    private float cueEffectUntil = -1f;
    private Vector3 cueEffectPosition;
    private Vector3 cueEffectScale = Vector3.one;
    private float cueEffectRotation;
    private Color cueEffectColor = Color.white;
    private Sprite motionTrailSprite;
    private float motionTrailStartedAt = -999f;
    private float motionTrailDuration = 0.16f;
    private Vector3 motionTrailPosition;
    private Vector3 motionTrailScale = Vector3.one;
    private float motionTrailRotation;
    private bool motionTrailFlipX;
    private Color motionTrailColor = Color.white;
    private Color sceneAmbientTint = Color.white;
    private Color groundBlendTint = new Color(0.28f, 0.25f, 0.21f, 0.22f);
    private Color inkRimTint = new Color(0.055f, 0.050f, 0.044f, 0.20f);
    private Color footOcclusionTint = new Color(0.30f, 0.26f, 0.20f, 0.30f);
    private float sceneDesaturation = 0.07f;

    private static Sprite ovalSprite;
    private static Sprite groundBlendSprite;
    private static Sprite footOcclusionSprite;
    private static Sprite slashSprite;
    private static Sprite skillBurstSprite;
    private static Sprite guardRingSprite;
    private static Sprite impactBurstSprite;
    private static Sprite footstepDustSprite;
    private static Sprite footContactSprite;
    private static Sprite[] elementSlashSprites;
    private static Sprite[] elementSkillSprites;
    private static Sprite[] elementImpactSprites;
    private const int CombatElementSpriteCount = 7;
    private const int SelectedSortingBoost = 0;

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
        RunAnimationEventCues();
        ApplyProceduralPose();
        UpdateSorting();
    }

    public void Bind(CombatantData combatant, bool isSelected)
    {
        visual = combatant != null ? combatant.visual : null;
        selected = isSelected;
        acted = false;
        defeated = false;
        facingSign = 1f;
        facingVector = Vector2.down;
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
            ResetAnimationEventCues();
        }

        UpdateSorting();
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
            ResetAnimationEventCues();
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
        if (direction.sqrMagnitude <= 0.0001f)
        {
            return;
        }

        facingVector = direction.normalized;
        if (Mathf.Abs(direction.x) > 0.01f)
        {
            facingSign = direction.x < 0f ? -1f : 1f;
        }
    }

    public Vector2 CurrentFacingDirection()
    {
        return FacingVectorOrDefault();
    }

    public void SetOutfit(CharacterOutfitData outfit)
    {
        outfitOverride = outfit;
        ApplyVisual();
    }

    public void SetSceneIntegration(Color ambientTint, Color groundTint, float desaturation = 0.07f,
                                    Color? occlusionTint = null)
    {
        sceneAmbientTint = new Color(Mathf.Max(0f, ambientTint.r),
                                     Mathf.Max(0f, ambientTint.g),
                                     Mathf.Max(0f, ambientTint.b),
                                     1f);
        groundBlendTint = new Color(Mathf.Clamp01(groundTint.r),
                                    Mathf.Clamp01(groundTint.g),
                                    Mathf.Clamp01(groundTint.b),
                                    Mathf.Clamp01(groundTint.a));
        Color footTint = occlusionTint ?? groundTint;
        footOcclusionTint = new Color(Mathf.Clamp01(footTint.r),
                                      Mathf.Clamp01(footTint.g),
                                      Mathf.Clamp01(footTint.b),
                                      Mathf.Clamp01(footTint.a));
        inkRimTint = new Color(Mathf.Clamp01(0.060f * ambientTint.r),
                               Mathf.Clamp01(0.054f * ambientTint.g),
                               Mathf.Clamp01(0.048f * ambientTint.b),
                               0.20f);
        sceneDesaturation = Mathf.Clamp01(desaturation);

        if (visual != null && bodyRenderer != null && bodyRenderer.sprite != null)
        {
            ApplyProceduralPose();
            UpdateSorting();
        }
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
        return visual == null ? 0.44f : Mathf.Max(0.44f, visual.WalkSecondsPerTile);
    }

    public float MoveSettleTime()
    {
        return visual == null ? 0.10f : Mathf.Max(0f, visual.MoveSettleTime);
    }

    private Vector2 FacingVectorOrDefault()
    {
        return facingVector.sqrMagnitude <= 0.0001f ? new Vector2(facingSign, 0f) : facingVector.normalized;
    }

    private float FacingSideAmount()
    {
        Vector2 facing = FacingVectorOrDefault();
        float vertical = Mathf.Abs(facing.y);
        if (vertical > 0.26f)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.Abs(facing.x) / 0.55f);
    }

    private float FacingBackAmount()
    {
        Vector2 facing = FacingVectorOrDefault();
        if (facing.y <= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.InverseLerp(0.22f, 0.44f, facing.y));
    }

    private float FacingFrontAmount()
    {
        Vector2 facing = FacingVectorOrDefault();
        if (facing.y >= 0f)
        {
            return 0f;
        }

        return Mathf.Clamp01(Mathf.InverseLerp(0.22f, 0.44f, -facing.y));
    }

    private bool IsFacingBack()
    {
        return FacingBackAmount() > 0.55f;
    }

    private bool IsFacingSide()
    {
        return FacingSideAmount() > 0.62f;
    }

    private CharacterLivingMotionProfile MotionProfile()
    {
        return visual == null ? null : visual.livingMotion;
    }

    private float IdleBreathAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? visual.breathingScale : profile.idleBreathAmount;
    }

    private float IdleBreathSpeed()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? visual.idleSpeed : profile.idleBreathSpeed;
    }

    private float IdleBobAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? visual.idleAmplitude : profile.idleBobAmount;
    }

    private float IdleBobSpeed()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? visual.idleSpeed : profile.idleBobSpeed;
    }

    private float SelectedPopScale()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 1.06f : Mathf.Max(1f, profile.selectedPopScale);
    }

    private float SelectedPopDuration()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.15f : Mathf.Max(0.04f, profile.selectedPopDuration);
    }

    private float TurnStartHop()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.085f : Mathf.Max(0f, profile.turnStartHop);
    }

    private float TurnStartDuration()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.34f : Mathf.Max(0.10f, profile.turnStartDuration);
    }

    private float WaitSlouchAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.45f : Mathf.Clamp01(profile.waitSlouchAmount);
    }

    private float LowHpShakeAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.012f : Mathf.Max(0f, profile.lowHpShakeAmount);
    }

    private float MoveHopAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.040f : Mathf.Max(0f, profile.moveHopAmount);
    }

    private float MoveLeanMultiplier()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 1f : Mathf.Max(0f, profile.moveLeanAmount);
    }

    private float FootDustAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return visual != null && !visual.enableFootDust ? 0f : profile == null ? 1f : Mathf.Max(0f, profile.footDustAmount);
    }

    private float AttackAnticipationDistance()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.045f : Mathf.Max(0f, profile.attackAnticipationDistance);
    }

    private float AttackLungeMultiplier()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 1f : Mathf.Max(0f, profile.attackLungeMultiplier);
    }

    private float ImpactFreezeSeconds()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        if (visual != null && !visual.enableImpactFreeze)
        {
            return 0f;
        }

        return profile == null ? 0.055f : Mathf.Max(0f, profile.impactFreezeSeconds);
    }

    private float HitShakeAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.025f : Mathf.Max(0f, profile.hitShakeAmount);
    }

    private float VictoryHopAmount()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.060f : Mathf.Max(0f, profile.victoryHopAmount);
    }

    private float StateEnterPopScale()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 1.035f : Mathf.Max(1f, profile.stateEnterPopScale);
    }

    private float StateEnterLift()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.030f : Mathf.Max(0f, profile.stateEnterLift);
    }

    private float StateEnterDuration()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.14f : Mathf.Max(0.04f, profile.stateEnterDuration);
    }

    private float MotionTrailAlpha()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.18f : Mathf.Clamp01(profile.motionTrailAlpha);
    }

    private float MotionTrailDuration()
    {
        CharacterLivingMotionProfile profile = MotionProfile();
        return profile == null ? 0.16f : Mathf.Max(0.04f, profile.motionTrailDuration);
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
            PlayEmotion(CharacterEmotion.Angry, 0.28f);
            PlayState(CharacterBattleVisualState.Attack, ClipDuration(visual == null ? null : visual.attackClip,
                                                                      CreateTimeline(false).Duration));
        }
    }

    public void PlaySkill()
    {
        if (!defeated)
        {
            PlayEmotion(CharacterEmotion.Serious, 0.42f);
            PlayState(CharacterBattleVisualState.Skill, ClipDuration(visual == null ? null : visual.skillClip,
                                                                     CreateTimeline(true).Duration));
        }
    }

    public void PlayHit()
    {
        if (!defeated)
        {
            PlayEmotion(CharacterEmotion.Pain, 0.32f);
            PlayState(CharacterBattleVisualState.Hit, ClipDuration(visual == null ? null : visual.hitClip, 0.30f));
        }
    }

    public void PlayGuard()
    {
        if (!defeated)
        {
            PlayState(CharacterBattleVisualState.Guard, ClipDuration(visual == null ? null : visual.guardClip, 0.45f));
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
            PlayEmotion(CharacterEmotion.Victory, 0.80f);
            PlayState(CharacterBattleVisualState.Victory, 0f);
        }
    }

    public void PlayTurnStart()
    {
        if (!defeated)
        {
            PlayEmotion(CharacterEmotion.Serious, 0.35f);
            PlayState(CharacterBattleVisualState.TurnStart,
                      ClipDuration(visual == null ? null : visual.turnStartClip, TurnStartDuration()));
        }
    }

    public void PlayLowHp(bool value)
    {
        if (lowHpActive == value)
        {
            return;
        }

        lowHpActive = value;
        if (lowHpActive)
        {
            PlayEmotion(CharacterEmotion.LowHp, 0.45f);
        }
        else if (activeEmotion == CharacterEmotion.LowHp)
        {
            activeEmotion = CharacterEmotion.Neutral;
            emotionExpiresAt = -1f;
        }
    }

    public void PlayEmotion(CharacterEmotion emotion, float seconds)
    {
        activeEmotion = emotion;
        emotionExpiresAt = seconds > 0f ? Time.time + seconds : -1f;
    }

    public void PlayClickReaction()
    {
        if (defeated || visual == null || !visual.enableSelectionPop)
        {
            return;
        }

        clickReactionStartedAt = Time.time;
        PlayEmotion(CharacterEmotion.Serious, 0.22f);
    }

    public void PlayAssistReaction()
    {
        if (defeated || visual == null)
        {
            return;
        }

        assistReactionStartedAt = Time.time;
        PlayEmotion(CharacterEmotion.Smile, 0.28f);
    }

    public void ApplyVisual()
    {
        EnsureRenderers();
        pixelMode = visual != null && visual.pixelSpriteMode;

        if (visual == null)
        {
            if (bodyRenderer != null)
            {
                SetBodySprite(null);
            }

            if (effectRenderer != null)
            {
                effectRenderer.sprite = null;
                effectRenderer.enabled = false;
            }

            ClearRenderer(shadowCoreRenderer);
            ClearRenderer(castShadowRenderer);
            ClearRenderer(groundBlendRenderer);
            ClearRenderer(inkRimRenderer);
            ClearRenderer(footOcclusionRenderer);
            ClearMotionTrail();
            ClearEmotionFace();

            ApplyLayerSprites();
            return;
        }

        SetBodySprite(SelectStateSprite());
        bodyRenderer.color = ApplySceneTint(visual.normalTint);
        bodyRenderer.flipX = false;
        ApplyLayerSprites();
        ApplyLayerTint(ApplySceneTint(visual.normalTint));

        if (animator != null)
        {
            animator.runtimeAnimatorController = visual.animatorController;
            animator.enabled = visual.animatorController != null;
        }

        Sprite fitSprite = bodyRenderer.sprite != null ? bodyRenderer.sprite : visual.fullBodySprite;
        float scale = 1f;
        bodyFootBoundsMinY = SpriteMeshMinY(fitSprite);
        float fitHeight = SpriteMeshHeight(fitSprite);
        if (fitHeight > 0.01f)
        {
            scale = visual.heightInTiles / fitHeight;
        }

        bodyTransform = bodyRenderer.transform;
        baseBodyPosition = new Vector3(visual.spriteOffset.x, visual.spriteOffset.y, 0f);
        baseBodyScale = Vector3.one * scale;
        bodyTransform.localPosition = baseBodyPosition;
        bodyTransform.localRotation = Quaternion.identity;
        bodyTransform.localScale = baseBodyScale;

        shadowRenderer.sprite = GetOvalSprite();
        shadowRenderer.transform.localPosition = new Vector3(0f, 0.055f, 0f);
        shadowRenderer.transform.localScale = new Vector3(visual.shadowWidth, visual.shadowHeight, 1f);
        shadowRenderer.color = new Color(0f, 0f, 0f, 0.18f);

        shadowCoreRenderer.sprite = GetOvalSprite();
        shadowCoreRenderer.transform.localPosition = new Vector3(0f, 0.03f, 0f);
        shadowCoreRenderer.transform.localScale = new Vector3(visual.shadowWidth * 0.55f, visual.shadowHeight * 0.52f, 1f);
        shadowCoreRenderer.color = new Color(0f, 0f, 0f, 0.45f);

        castShadowRenderer.enabled = false;
        castShadowRenderer.color = new Color(0f, 0f, 0f, 0.20f);

        inkRimRenderer.enabled = false;
        inkRimRenderer.color = inkRimTint;

        groundBlendRenderer.sprite = GetGroundBlendSprite();
        groundBlendRenderer.color = groundBlendTint;
        groundBlendRenderer.enabled = bodyRenderer.sprite != null;

        footOcclusionRenderer.sprite = GetFootOcclusionSprite();
        footOcclusionRenderer.color = footOcclusionTint;
        footOcclusionRenderer.enabled = bodyRenderer.sprite != null;

        selectionRenderer.sprite = GetOvalSprite();
        selectionRenderer.transform.localPosition = new Vector3(0f, 0.055f, 0f);
        selectionRenderer.transform.localScale =
            new Vector3(visual.shadowWidth * 1.15f, visual.shadowHeight * 1.35f, 1f);
        selectionRenderer.color = new Color(0.28f, 0.76f, 1f, 0.28f);
        selectionRenderer.enabled = selected && !defeated;

        effectRenderer.enabled = false;
        effectRenderer.color = Color.white;
        ClearMotionTrail();
        if (pixelMode)
        {
            ConfigurePixelMode();
        }
        ScheduleNextBlink(Time.time);
        visualState = defeated ? CharacterBattleVisualState.Defeat :
                      selected ? CharacterBattleVisualState.SelectedIdle :
                      acted ? CharacterBattleVisualState.Wait : CharacterBattleVisualState.Idle;
        stateDuration = 0f;
        stateStartedAt = Time.time;
        ResetAnimationEventCues();

        ApplyProceduralPose();
        UpdateSorting();
    }

    private void PlayState(CharacterBattleVisualState state, float duration)
    {
        if (defeated && state != CharacterBattleVisualState.Defeat)
        {
            return;
        }

        if (state != visualState)
        {
            CaptureMotionTrail(state);
        }

        visualState = state;
        stateDuration = Mathf.Max(0f, duration);
        stateStartedAt = Time.time;
        if (state != CharacterBattleVisualState.Move)
        {
            moveStridePhase = -1f;
            moveStridePhaseSetAt = -1f;
        }
        ResetAnimationEventCues();

        if (bodyRenderer != null)
        {
            SetBodySprite(SelectStateSprite());
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
        ResetAnimationEventCues();
    }

    private void ApplyProceduralPose()
    {
        float time = Time.time;
        float stateAge = time - stateStartedAt;
        float progress = stateDuration > 0f ? Mathf.Clamp01(stateAge / stateDuration) : 0f;

        // 살아있는 대기 모션 v2 — "그림이 통째로 위아래로 뜨는" 번역형 바운스 대신,
        // 발 피벗을 붙인 채 가슴이 차오르는 세로 스쿼시&스트레치로 숨을 쉰다.
        // (GroundingStabilizer가 발을 그림자에 고정하므로 Y 이동형 바운스는 보정과 싸워
        //  떠다니는 느낌만 남는다. 스케일 호흡은 발 라인이 고정되어 접지가 유지된다.)
        float pulse = Mathf.Sin((time * Mathf.Max(0.1f, IdleBobSpeed())) + phaseSeed);
        float rawBob = pulse * IdleBobAmount();
        float idleBob = rawBob * 0.25f; // 발을 붙여 보이게 잔향만 남긴다.
        // 전신 스프라이트는 프레임마다 스케일하면 픽셀이 뭉개져 위치/기울기만 쓴다.
        // 저주파 체중 이동: 좌우로 아주 천천히 무게를 옮기며 발 피벗 기준 미세하게 기운다.
        float weightSway = Mathf.Sin((time * 0.52f) + (phaseSeed * 1.7f));

        Sprite stateSprite = SelectStateSprite();
        if (bodyRenderer.sprite != stateSprite)
        {
            SetBodySprite(stateSprite);
            ApplyLayerSprites();
        }
        Vector3 localPosition = baseBodyPosition + new Vector3(weightSway * 0.006f, idleBob, 0f);
        Vector3 localScale = baseBodyScale;
        float rotation = weightSway * 1.15f; // 대기 중 미세 기울기 — 액션 상태는 아래 switch에서 덮어쓴다.
        Color tint = visual.normalTint;
        float shadowAlpha = 0.34f;
        float footStride01 = -1f;
        float facingSideAmount = FacingSideAmount();
        float facingBackAmount = FacingBackAmount();
        float facingFrontAmount = FacingFrontAmount();
        bool showEffect = false;
        Sprite effectSprite = null;
        Vector3 effectPosition = new Vector3(0f, 0.55f, -0.02f);
        Vector3 effectScale = Vector3.one;
        float effectRotation = 0f;
        Color effectColor = Color.white;
        Vector2 facingAxis = FacingVectorOrDefault();

        bool selectedIdle = visualState == CharacterBattleVisualState.SelectedIdle ||
                            (selected && visualState == CharacterBattleVisualState.Idle);
        if (selectedIdle)
        {
            // 선택 강조: 발을 붙인 채 작은 위치 잔향만 준다.
            float selectPulse = Mathf.Abs(Mathf.Sin((time * 4.8f) + phaseSeed));
            localPosition.y += selectPulse * 0.004f;
        }
        else if (acted || visualState == CharacterBattleVisualState.Wait)
        {
            tint = visual.actedTint;
            // 행동 완료: 호흡을 얕게 죽이고 어깨를 떨어뜨려 지친 실루엣을 만든다.
            localPosition.y -= Mathf.Abs(rawBob) * WaitSlouchAmount();
            rotation *= 0.5f;
        }

        switch (visualState)
        {
        case CharacterBattleVisualState.Move:
            float stride01 = MoveStride01(stateAge);
            footStride01 = stride01;
            float step = Mathf.Sin(stride01 * Mathf.PI * 2f);
            float counterStep = Mathf.Cos(stride01 * Mathf.PI * 2f);
            float weightShift = Mathf.Sin(stride01 * Mathf.PI * 4f);
            float hop = Mathf.Abs(step);
            float planted = 1f - Mathf.SmoothStep(0.08f, 0.32f, Mathf.Abs(step));
            // 실제 걷기 프레임이 있으면 보행감은 그림이 담당하므로 절차 변형은 잔향만 남긴다.
            float gait = HasMoveFrames() ? 0.86f : 1f;
            localPosition.x += facingSign * step * 0.038f * gait;
            localPosition.x += facingSign * counterStep * Mathf.Lerp(0.006f, 0.022f, facingSideAmount) * gait;
            localPosition.y += hop * MoveHopAmount() * gait;
            localScale.x *= 1f - hop * 0.030f * gait;
            localScale.y *= 1f + hop * 0.042f * gait;
            localScale.x *= 1f + Mathf.Abs(weightShift) * 0.018f * gait;
            localScale.y *= 1f - Mathf.Abs(weightShift) * 0.014f * gait;
            localScale.x *= Mathf.Lerp(0.88f, 1f, facingSideAmount);
            localScale.y *= Mathf.Lerp(0.94f, 1.03f, facingFrontAmount);
            localPosition.y += (facingBackAmount * 0.035f) - (facingFrontAmount * 0.012f);
            rotation = ((-facingSign * visual.moveLeanDegrees) + (facingSign * step * 2.4f * gait)) *
                       MoveLeanMultiplier();
            rotation += facingSign * counterStep * 2.8f * gait * Mathf.Lerp(0.35f, 1f, facingSideAmount);
            rotation *= Mathf.Lerp(0.22f, 1f, facingSideAmount);
            tint = Color.Lerp(tint, new Color(0.72f, 0.80f, 0.92f, tint.a), facingBackAmount * 0.16f);
            float dustAmount = FootDustAmount();
            if (planted > 0.45f && dustAmount > 0f)
            {
                showEffect = true;
                effectSprite = GetFootstepDustSprite();
                Vector2 dustDirection = FacingVectorOrDefault();
                effectPosition = new Vector3(-dustDirection.x * 0.18f, 0.10f - (dustDirection.y * 0.055f), -0.02f);
                effectScale = Vector3.one * (0.32f + (0.16f * planted)) * dustAmount;
                effectColor = Color.Lerp(new Color(0.78f, 0.88f, 0.94f, (0.28f + (0.22f * planted)) * dustAmount),
                                         ElementPrimary(0.44f), 0.45f);
            }
            break;
        case CharacterBattleVisualState.Attack:
            float anticipation = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.22f, progress)) *
                                 (1f - Mathf.SmoothStep(0.22f, 0.36f, progress));
            float drive = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.18f, 0.52f, progress));
            float recover = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.58f, 1f, progress));
            float impactWindow = Mathf.Clamp01(1f - Mathf.Abs(progress - 0.54f) / Mathf.Max(0.01f, ImpactFreezeSeconds() * 5f));
            float lunge = Mathf.Clamp01(drive - (recover * 0.85f));
            float attackShake = Mathf.Sin((time + phaseSeed) * 72f) * impactWindow * HitShakeAmount();
            float attackAnticipation = AttackAnticipationDistance();
            float attackDrive = AttackLunge(false) * AttackLungeMultiplier();
            localPosition.x -= facingAxis.x * attackAnticipation * anticipation;
            localPosition.y -= facingAxis.y * attackAnticipation * 0.45f * anticipation;
            localPosition.x += facingAxis.x * attackDrive * lunge;
            localPosition.y += facingAxis.y * attackDrive * 0.45f * lunge;
            localPosition.x += attackShake;
            localPosition.y += (0.035f * lunge) - (0.022f * anticipation);
            localScale.x *= 1f + (0.075f * lunge) - (0.030f * anticipation);
            localScale.y *= 1f - (0.032f * lunge) + (0.034f * anticipation);
            rotation = (-facingSign * 9.5f * lunge) + (facingSign * 4.0f * anticipation);
            rotation *= Mathf.Lerp(0.45f, 1f, facingSideAmount);
            showEffect = progress > 0.16f && progress < 0.92f;
            effectSprite = GetElementSlashSprite(ActiveElement());
            effectPosition = new Vector3(facingAxis.x * (0.34f + (0.08f * lunge)),
                                         0.56f + (facingAxis.y * (0.18f + (0.04f * lunge))), -0.02f);
            effectScale = new Vector3(0.72f, 0.52f, 1f);
            effectRotation = Mathf.Atan2(facingAxis.y, facingAxis.x) * Mathf.Rad2Deg;
            effectColor = ElementPrimary(0.90f);
            break;
        case CharacterBattleVisualState.Skill:
            float skill = Mathf.Sin(progress * Mathf.PI);
            float skillWindup = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0f, 0.20f, progress)) *
                                (1f - Mathf.SmoothStep(0.20f, 0.36f, progress));
            localPosition.x -= facingAxis.x * AttackAnticipationDistance() * 0.55f * skillWindup;
            localPosition.y -= facingAxis.y * AttackAnticipationDistance() * 0.24f * skillWindup;
            localPosition.x += facingAxis.x * AttackLunge(true) * AttackLungeMultiplier() * 0.62f * skill;
            localPosition.y += facingAxis.y * AttackLunge(true) * AttackLungeMultiplier() * 0.28f * skill;
            localPosition.y += 0.075f * skill;
            localScale *= 1f + visual.skillPulseScale * 1.12f * skill;
            rotation = Mathf.Sin(progress * Mathf.PI * 2f) * 4.2f;
            rotation *= Mathf.Lerp(0.45f, 1f, facingSideAmount);
            tint = Color.Lerp(visual.normalTint, visual.guardTint, 0.42f * skill);
            showEffect = true;
            effectSprite = GetElementSkillSprite(ActiveElement());
            effectPosition = new Vector3(facingAxis.x * 0.16f, 0.58f + (facingAxis.y * 0.16f), -0.03f);
            effectScale = Vector3.one * (0.72f + 0.32f * skill);
            effectRotation = (Mathf.Atan2(facingAxis.y, facingAxis.x) * Mathf.Rad2Deg) + (time * 18f);
            effectColor = Color.Lerp(ElementPrimary(0.42f + 0.30f * skill), ElementSecondary(0.42f + 0.30f * skill), 0.38f);
            break;
        case CharacterBattleVisualState.Hit:
            float hit = Mathf.Sin(progress * Mathf.PI);
            localPosition.x -= facingAxis.x * visual.hitRecoil * hit;
            localPosition.y -= facingAxis.y * visual.hitRecoil * 0.42f * hit;
            localPosition.x += Mathf.Sin((time + phaseSeed) * 86f) * HitShakeAmount() * hit;
            localPosition.y += 0.018f * hit;
            rotation = facingSign * 10f * hit;
            rotation *= Mathf.Lerp(0.45f, 1f, facingSideAmount);
            tint = Color.Lerp(visual.normalTint, visual.hitTint, hit);
            showEffect = progress < 0.82f;
            effectSprite = GetElementImpactSprite(ActiveElement());
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
            localPosition.y += cheer * VictoryHopAmount();
            rotation = Mathf.Sin((time * 5.6f) + phaseSeed) * 4f;
            break;
        case CharacterBattleVisualState.Wait:
            tint = visual.actedTint;
            break;
        case CharacterBattleVisualState.TurnStart:
            float ready = Mathf.Sin(progress * Mathf.PI);
            localPosition.y += ready * TurnStartHop();
            localScale.x *= 1f + ready * 0.045f;
            localScale.y *= 1f - ready * 0.020f;
            rotation = -facingSign * 4f * ready;
            showEffect = true;
            effectSprite = GetGuardRingSprite();
            effectPosition = new Vector3(0f, 0.18f, -0.03f);
            effectScale = Vector3.one * (0.44f + (0.18f * ready));
            effectColor = ElementPrimary(0.20f + (0.24f * ready));
            break;
        }

        if (lowHpActive && !defeated)
        {
            float weakPulse = Mathf.Sin((time * 16f) + phaseSeed);
            localPosition.x += weakPulse * LowHpShakeAmount();
            localScale.y *= 1f + Mathf.Abs(weakPulse) * 0.012f;
            tint = Color.Lerp(tint, visual.hitTint, 0.16f);
        }

        if (visualState != CharacterBattleVisualState.Move && visualState != CharacterBattleVisualState.Attack &&
            visualState != CharacterBattleVisualState.Skill && visualState != CharacterBattleVisualState.Hit)
        {
            localScale.x *= Mathf.Lerp(0.94f, 1f, facingSideAmount);
            localScale.y *= Mathf.Lerp(0.97f, 1.02f, facingFrontAmount);
            tint = Color.Lerp(tint, new Color(0.76f, 0.82f, 0.92f, tint.a), facingBackAmount * 0.10f);
        }

        ApplyEmotionTint(ref tint);
        ApplyStateEnterMotion(stateAge, ref localPosition, ref localScale, ref rotation);
        ApplyClickAndAssistReactions(time, ref localPosition, ref localScale, ref rotation);
        ApplyRootOffset(ref localPosition);
        ApplyCueEffect(time, ref showEffect, ref effectSprite, ref effectPosition, ref effectScale,
                       ref effectRotation, ref effectColor);
        StabilizeFootAnchor(ref localPosition, localScale);

        Color integratedTint = ApplySceneTint(tint);
        bodyRenderer.flipX = facingSign < 0f;
        bodyRenderer.color = integratedTint;
        if (!pixelMode)
        {
            ApplyLayerFlip(facingSign < 0f);
            ApplyLayerTint(integratedTint);
            UpdateEmotionFace(time, integratedTint);
        }
        bodyTransform.localPosition = localPosition;
        bodyTransform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        bodyTransform.localScale = localScale;
        if (!pixelMode)
        {
            ApplyLayerSway(time, progress, footStride01);
            UpdateFootContacts(visualState == CharacterBattleVisualState.Move, footStride01);
        }

        UpdateShadowPose(localPosition, shadowAlpha);
        if (!pixelMode)
        {
            UpdateCastShadow(shadowAlpha);
            UpdateGroundBlendOverlay();
            UpdateInkRim();
            UpdateFootOcclusion();
        }
        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = selected && !defeated;
            float ringPulse = 0.18f + Mathf.Abs(Mathf.Sin((time * 4.3f) + phaseSeed)) * 0.14f;
            selectionRenderer.color = new Color(0.28f, 0.76f, 1f, ringPulse);
        }

        UpdateEffect(showEffect, effectSprite, effectPosition, effectScale, effectRotation, effectColor);
        if (!pixelMode)
        {
            UpdateMotionTrail(time);
        }
    }

    private void ApplyStateEnterMotion(float stateAge, ref Vector3 localPosition, ref Vector3 localScale,
                                       ref float rotation)
    {
        if (!UsesStateEnterAccent(visualState))
        {
            return;
        }

        float duration = StateEnterDuration();
        if (stateAge >= duration)
        {
            return;
        }

        float t = Mathf.Clamp01(stateAge / duration);
        float pop = Mathf.Sin(t * Mathf.PI);
        float settle = (1f - t) * Mathf.Sin(t * Mathf.PI * 2.4f);
        float strength = StateEnterStrength(visualState);
        float scaleDelta = (StateEnterPopScale() - 1f) * strength;
        localPosition.y += pop * StateEnterLift() * strength;
        localScale.x *= 1f + (scaleDelta * pop);
        localScale.y *= 1f - (scaleDelta * 0.45f * pop);
        rotation += -facingSign * settle * 3.0f * strength;
    }

    private void ApplyRootOffset(ref Vector3 localPosition)
    {
        CharacterSpriteAnimationClipData clip = ActiveStateClip();
        if (clip == null || clip.rootOffset.sqrMagnitude <= 0.000001f)
        {
            return;
        }

        float weight = visualState == CharacterBattleVisualState.Move ? 0.5f : 1f;
        localPosition += new Vector3(clip.rootOffset.x * facingSign * weight, clip.rootOffset.y * weight, 0f);
    }

    private static bool UsesStateEnterAccent(CharacterBattleVisualState state)
    {
        return state == CharacterBattleVisualState.SelectedIdle ||
               state == CharacterBattleVisualState.Move ||
               state == CharacterBattleVisualState.Attack ||
               state == CharacterBattleVisualState.Skill ||
               state == CharacterBattleVisualState.Hit ||
               state == CharacterBattleVisualState.Guard ||
               state == CharacterBattleVisualState.Victory ||
               state == CharacterBattleVisualState.TurnStart;
    }

    private static float StateEnterStrength(CharacterBattleVisualState state)
    {
        switch (state)
        {
        case CharacterBattleVisualState.Attack:
        case CharacterBattleVisualState.Skill:
            return 0.58f;
        case CharacterBattleVisualState.Hit:
            return 0.72f;
        case CharacterBattleVisualState.Move:
            return 0.46f;
        case CharacterBattleVisualState.SelectedIdle:
            return 0f;
        default:
            return 1f;
        }
    }

    private void CaptureMotionTrail(CharacterBattleVisualState nextState)
    {
        if (!UsesMotionTrail(nextState) || visual == null || bodyRenderer == null || bodyTransform == null)
        {
            return;
        }

        float alpha = MotionTrailAlpha();
        if (alpha <= 0.001f || bodyRenderer.sprite == null)
        {
            return;
        }

        motionTrailSprite = bodyRenderer.sprite;
        motionTrailStartedAt = Time.time;
        motionTrailDuration = MotionTrailDuration() * MotionTrailStrength(nextState);
        motionTrailPosition = bodyTransform.localPosition;
        motionTrailScale = bodyTransform.localScale;
        motionTrailRotation = bodyTransform.localEulerAngles.z;
        motionTrailFlipX = bodyRenderer.flipX;
        motionTrailColor = Color.Lerp(visual.normalTint, ElementPrimary(1f), 0.18f);
        motionTrailColor.a = alpha;
    }

    private void UpdateMotionTrail(float time)
    {
        if (motionTrailRenderer == null)
        {
            return;
        }

        if (motionTrailSprite == null || time - motionTrailStartedAt >= motionTrailDuration)
        {
            motionTrailRenderer.enabled = false;
            motionTrailSprite = null;
            return;
        }

        float t = Mathf.Clamp01((time - motionTrailStartedAt) / Mathf.Max(0.01f, motionTrailDuration));
        float fade = (1f - t) * (1f - t);
        Vector3 drift = new Vector3(-facingSign * 0.026f * t, 0.018f * t, 0f);
        motionTrailRenderer.enabled = true;
        motionTrailRenderer.sprite = motionTrailSprite;
        motionTrailRenderer.flipX = motionTrailFlipX;
        motionTrailRenderer.transform.localPosition = motionTrailPosition + drift;
        motionTrailRenderer.transform.localScale = motionTrailScale * (1f + 0.035f * t);
        motionTrailRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, motionTrailRotation - facingSign * 2.0f * t);
        Color color = motionTrailColor;
        color.a *= fade;
        motionTrailRenderer.color = color;
    }

    private void ClearMotionTrail()
    {
        motionTrailSprite = null;
        motionTrailStartedAt = -999f;
        if (motionTrailRenderer != null)
        {
            motionTrailRenderer.sprite = null;
            motionTrailRenderer.enabled = false;
        }
    }

    private static bool UsesMotionTrail(CharacterBattleVisualState state)
    {
        return state == CharacterBattleVisualState.Attack ||
               state == CharacterBattleVisualState.Skill ||
               state == CharacterBattleVisualState.Hit ||
               state == CharacterBattleVisualState.Guard ||
               state == CharacterBattleVisualState.Victory ||
               state == CharacterBattleVisualState.TurnStart;
    }

    private static float MotionTrailStrength(CharacterBattleVisualState state)
    {
        switch (state)
        {
        case CharacterBattleVisualState.Skill:
            return 1.25f;
        case CharacterBattleVisualState.Attack:
            return 1.05f;
        case CharacterBattleVisualState.Hit:
            return 0.75f;
        default:
            return 0.9f;
        }
    }

    private void ResetAnimationEventCues()
    {
        activeCueClip = null;
        activeCueLoopIndex = -1;
        firedClipCues.Clear();
        cueEffectSprite = null;
        cueEffectUntil = -1f;
    }

    private void RunAnimationEventCues()
    {
        CharacterSpriteAnimationClipData clip = ActiveStateClip();
        if (clip == null || clip.eventCues == null || clip.eventCues.Length == 0)
        {
            if (activeCueClip != clip)
            {
                activeCueClip = clip;
                activeCueLoopIndex = -1;
                firedClipCues.Clear();
            }

            return;
        }

        float duration = Mathf.Max(0.01f, clip.Duration);
        float elapsed = Mathf.Max(0f, Time.time - stateStartedAt);
        bool loop = ShouldLoopCueClip(clip);
        int loopIndex = loop ? Mathf.FloorToInt(elapsed / duration) : 0;
        float normalized = loop ? Mathf.Repeat(elapsed / duration, 1f) : Mathf.Clamp01(elapsed / duration);

        if (activeCueClip != clip || activeCueLoopIndex != loopIndex)
        {
            activeCueClip = clip;
            activeCueLoopIndex = loopIndex;
            firedClipCues.Clear();
        }

        for (int i = 0; i < clip.eventCues.Length; i++)
        {
            CharacterSpriteAnimationEventCue cue = clip.eventCues[i];
            if (string.IsNullOrEmpty(cue.cueId) || normalized < Mathf.Clamp01(cue.normalizedTime))
            {
                continue;
            }

            string key = i.ToString() + ":" + cue.cueId;
            if (firedClipCues.Add(key))
            {
                HandleAnimationCue(cue);
            }
        }
    }

    private CharacterSpriteAnimationClipData ActiveStateClip()
    {
        if (visual == null)
        {
            return null;
        }

        switch (visualState)
        {
        case CharacterBattleVisualState.Move:
            return visual.moveClip;
        case CharacterBattleVisualState.Attack:
            return visual.attackClip;
        case CharacterBattleVisualState.Skill:
            return visual.skillClip;
        case CharacterBattleVisualState.Hit:
            return visual.hitClip;
        case CharacterBattleVisualState.Guard:
            return visual.guardClip;
        case CharacterBattleVisualState.Defeat:
            return visual.defeatClip;
        case CharacterBattleVisualState.Victory:
            return visual.victoryClip;
        case CharacterBattleVisualState.Wait:
            return null;
        case CharacterBattleVisualState.TurnStart:
            return null;
        default:
            return null;
        }
    }

    private bool ShouldLoopCueClip(CharacterSpriteAnimationClipData clip)
    {
        return clip != null && (clip.loop ||
                                visualState == CharacterBattleVisualState.Idle ||
                                visualState == CharacterBattleVisualState.SelectedIdle ||
                                visualState == CharacterBattleVisualState.Move ||
                                visualState == CharacterBattleVisualState.Victory ||
                                visualState == CharacterBattleVisualState.Wait);
    }

    private void HandleAnimationCue(CharacterSpriteAnimationEventCue cue)
    {
        string cueId = cue.cueId.ToLowerInvariant();
        if (cueId.Contains("footstep"))
        {
            OnFootstepFrame();
            float dustAmount = FootDustAmount();
            if (dustAmount > 0f)
            {
                Vector2 dustDirection = FacingVectorOrDefault();
                ScheduleCueEffect(GetFootstepDustSprite(),
                                  new Vector3(-dustDirection.x * 0.16f, 0.10f - (dustDirection.y * 0.045f), -0.02f),
                                  Vector3.one * 0.36f * dustAmount, 0f,
                                  new Color(0.78f, 0.88f, 0.94f, 0.32f * dustAmount), 0.12f);
            }

            return;
        }

        if (cueId.Contains("impact") || cueId.Contains("damage") || cueId.Contains("hit_flash"))
        {
            if (visualState == CharacterBattleVisualState.Skill)
            {
                OnSkillHitFrame();
            }
            else if (visualState == CharacterBattleVisualState.Attack)
            {
                OnAttackHitFrame();
            }

            ScheduleCueEffect(GetElementImpactSprite(ActiveElement()), new Vector3(0f, 0.58f, -0.03f),
                              Vector3.one * 0.62f, 0f, ElementSecondary(0.72f), 0.16f);
            return;
        }

        if (cueId.Contains("guard"))
        {
            ScheduleCueEffect(GetGuardRingSprite(), new Vector3(0f, 0.48f, -0.03f),
                              Vector3.one * 0.64f, 0f, ElementPrimary(0.48f), 0.18f);
            return;
        }

        if (cueId.Contains("cast") || cueId.Contains("afterglow"))
        {
            ScheduleCueEffect(GetElementSkillSprite(ActiveElement()), new Vector3(0f, 0.58f, -0.03f),
                              Vector3.one * 0.70f, Time.time * 18f, ElementPrimary(0.56f), 0.16f);
            return;
        }

        if (cueId.Contains("ready"))
        {
            ScheduleCueEffect(GetGuardRingSprite(), new Vector3(0f, 0.18f, -0.03f),
                              Vector3.one * 0.48f, 0f, ElementPrimary(0.42f), 0.14f);
            return;
        }

        if (cueId.Contains("weak"))
        {
            ScheduleCueEffect(GetGuardRingSprite(), new Vector3(0f, 0.16f, -0.03f),
                              Vector3.one * 0.34f, 0f, new Color(1f, 0.52f, 0.48f, 0.24f), 0.10f);
            return;
        }

        if (cueId.Contains("anticipation") || cueId.Contains("recover"))
        {
            ScheduleCueEffect(GetElementSlashSprite(ActiveElement()), new Vector3(facingSign * 0.34f, 0.56f, -0.02f),
                              new Vector3(0.62f, 0.46f, 1f), facingSign < 0f ? 180f : 0f,
                              ElementPrimary(0.55f), 0.12f);
        }
    }

    private void ScheduleCueEffect(Sprite sprite, Vector3 position, Vector3 scale, float rotation, Color color,
                                   float seconds)
    {
        cueEffectSprite = sprite;
        cueEffectPosition = position;
        cueEffectScale = scale;
        cueEffectRotation = rotation;
        cueEffectColor = color;
        cueEffectUntil = Time.time + Mathf.Max(0.02f, seconds);
    }

    private void ApplyCueEffect(float time, ref bool showEffect, ref Sprite effectSprite,
                                ref Vector3 effectPosition, ref Vector3 effectScale,
                                ref float effectRotation, ref Color effectColor)
    {
        if (cueEffectSprite == null)
        {
            return;
        }

        if (time >= cueEffectUntil)
        {
            cueEffectSprite = null;
            return;
        }

        showEffect = true;
        effectSprite = cueEffectSprite;
        effectPosition = cueEffectPosition;
        effectScale = cueEffectScale;
        effectRotation = cueEffectRotation;
        effectColor = cueEffectColor;
    }

    private void ApplyEmotionTint(ref Color tint)
    {
        RefreshEmotion(Time.time);

        switch (activeEmotion)
        {
        case CharacterEmotion.Smile:
            break;
        case CharacterEmotion.Serious:
        case CharacterEmotion.Angry:
            break;
        case CharacterEmotion.Pain:
            tint = Color.Lerp(tint, visual.hitTint, 0.36f);
            break;
        case CharacterEmotion.Victory:
            break;
        case CharacterEmotion.LowHp:
            tint = Color.Lerp(tint, visual.hitTint, 0.18f);
            break;
        }
    }

    private void RefreshEmotion(float time)
    {
        if (emotionExpiresAt > 0f && time > emotionExpiresAt)
        {
            activeEmotion = lowHpActive ? CharacterEmotion.LowHp : CharacterEmotion.Neutral;
            emotionExpiresAt = -1f;
        }
    }

    private void UpdateEmotionFace(float time, Color tint)
    {
        if (emotionFaceRenderer == null)
        {
            return;
        }

        if (!ShouldRenderEmotionFaceOverlay())
        {
            emotionFaceRenderer.enabled = false;
            emotionFaceRenderer.sprite = null;
            return;
        }

        RefreshEmotion(time);
        bool blinking = UpdateBlink(time);
        Sprite sprite = EmotionFaceSprite(activeEmotion);
        if (sprite == null && blinking && CanBlinkDuring(activeEmotion))
        {
            sprite = visual.blinkFaceSprite;
        }

        if (sprite == null)
        {
            emotionFaceRenderer.enabled = false;
            emotionFaceRenderer.sprite = null;
            return;
        }

        emotionFaceRenderer.enabled = true;
        emotionFaceRenderer.sprite = sprite;
        emotionFaceRenderer.flipX = facingSign < 0f;
        emotionFaceRenderer.color = tint;
        Vector2 offset = visual == null ? Vector2.zero : visual.emotionFaceOffset;
        Vector2 scale = visual == null ? Vector2.one : visual.emotionFaceScale;
        emotionFaceRenderer.transform.localPosition = new Vector3(offset.x, offset.y, 0f);
        emotionFaceRenderer.transform.localRotation = Quaternion.identity;
        emotionFaceRenderer.transform.localScale = new Vector3(scale.x, scale.y, 1f);
    }

    private bool ShouldRenderEmotionFaceOverlay()
    {
        return visual != null && visual.fullBodySprite == null && visual.blinkFaceSprite != null;
    }

    private bool UpdateBlink(float time)
    {
        if (visual == null || !visual.enableBlink || visual.blinkFaceSprite == null)
        {
            nextBlinkAt = -1f;
            blinkUntil = -1f;
            return false;
        }

        if (blinkUntil > time)
        {
            return true;
        }

        if (blinkUntil > 0f)
        {
            blinkUntil = -1f;
            ScheduleNextBlink(time);
        }

        if (nextBlinkAt < 0f)
        {
            ScheduleNextBlink(time);
        }

        if (time < nextBlinkAt)
        {
            return false;
        }

        blinkUntil = time + 0.08f;
        nextBlinkAt = -1f;
        return true;
    }

    private void ScheduleNextBlink(float time)
    {
        if (visual == null || !visual.enableBlink || visual.blinkFaceSprite == null)
        {
            nextBlinkAt = -1f;
            blinkUntil = -1f;
            return;
        }

        CharacterLivingMotionProfile profile = MotionProfile();
        float min = profile == null ? 3f : Mathf.Max(0.2f, profile.blinkMinInterval);
        float max = profile == null ? 7f : Mathf.Max(min, profile.blinkMaxInterval);
        nextBlinkAt = time + Random.Range(min, max);
    }

    private Sprite EmotionFaceSprite(CharacterEmotion emotion)
    {
        if (visual == null)
        {
            return null;
        }

        switch (emotion)
        {
        case CharacterEmotion.Blink:
            return visual.blinkFaceSprite;
        case CharacterEmotion.Smile:
        case CharacterEmotion.Victory:
            return visual.happyFaceSprite;
        case CharacterEmotion.Serious:
            return visual.seriousFaceSprite;
        case CharacterEmotion.Angry:
            return visual.angryFaceSprite != null ? visual.angryFaceSprite : visual.seriousFaceSprite;
        case CharacterEmotion.Pain:
            return visual.painFaceSprite;
        case CharacterEmotion.LowHp:
            return visual.painFaceSprite != null ? visual.painFaceSprite : visual.seriousFaceSprite;
        default:
            return null;
        }
    }

    private static bool CanBlinkDuring(CharacterEmotion emotion)
    {
        return emotion == CharacterEmotion.Neutral ||
               emotion == CharacterEmotion.Smile ||
               emotion == CharacterEmotion.Serious ||
               emotion == CharacterEmotion.LowHp;
    }

    private void ClearEmotionFace()
    {
        nextBlinkAt = -1f;
        blinkUntil = -1f;
        if (emotionFaceRenderer != null)
        {
            emotionFaceRenderer.enabled = false;
            emotionFaceRenderer.sprite = null;
        }
    }

    private void ApplyClickAndAssistReactions(float time, ref Vector3 localPosition, ref Vector3 localScale,
                                              ref float rotation)
    {
        if (visual != null && visual.enableSelectionPop)
        {
            float clickT = Mathf.Clamp01((time - clickReactionStartedAt) / SelectedPopDuration());
            if (clickT < 1f)
            {
                float pop = Mathf.Sin(clickT * Mathf.PI);
                float scale = Mathf.Lerp(1f, SelectedPopScale(), pop);
                localScale *= scale;
                localPosition.y += pop * 0.024f;
                rotation += -facingSign * pop * 2.5f;
            }
        }

        float assistT = Mathf.Clamp01((time - assistReactionStartedAt) / 0.24f);
        if (assistT < 1f)
        {
            float pulse = Mathf.Sin(assistT * Mathf.PI);
            localPosition.y += pulse * 0.018f;
            rotation += facingSign * pulse * 2f;
        }
    }

    private void StabilizeFootAnchor(ref Vector3 localPosition, Vector3 localScale)
    {
        if (Mathf.Abs(bodyFootBoundsMinY) <= 0.0001f)
        {
            return;
        }

        localPosition.y += bodyFootBoundsMinY * (baseBodyScale.y - localScale.y);
    }

    private static float SpriteMeshHeight(Sprite sprite)
    {
        if (TryGetSpriteMeshVerticalBounds(sprite, out float minY, out float maxY))
        {
            return maxY - minY;
        }

        return sprite == null ? 0f : sprite.bounds.size.y;
    }

    private static float SpriteMeshMinY(Sprite sprite)
    {
        if (TryGetSpriteMeshVerticalBounds(sprite, out float minY, out _))
        {
            return minY;
        }

        return sprite == null ? 0f : sprite.bounds.min.y;
    }

    private static bool TryGetSpriteMeshVerticalBounds(Sprite sprite, out float minY, out float maxY)
    {
        minY = 0f;
        maxY = 0f;
        if (sprite == null || sprite.vertices == null || sprite.vertices.Length == 0)
        {
            return false;
        }

        minY = sprite.vertices[0].y;
        maxY = minY;
        for (int i = 1; i < sprite.vertices.Length; i++)
        {
            float y = sprite.vertices[i].y;
            if (y < minY)
            {
                minY = y;
            }
            else if (y > maxY)
            {
                maxY = y;
            }
        }

        return maxY > minY + 0.0001f;
    }

    private void UpdateShadowPose(Vector3 localPosition, float stateShadowAlpha)
    {
        if (shadowRenderer == null || visual == null)
        {
            return;
        }

        float lift = Mathf.Max(0f, localPosition.y - baseBodyPosition.y);
        float squash = Mathf.Clamp01(lift / 0.14f);
        float width = visual.shadowWidth * (1f + squash * 0.10f);
        float height = visual.shadowHeight * (1f - squash * 0.18f);
        if (visualState == CharacterBattleVisualState.Wait || lowHpActive)
        {
            width *= 1.04f;
            height *= 0.94f;
        }

        float defeatedScale = visualState == CharacterBattleVisualState.Defeat ? 0.78f : 1f;
        float softAlpha = Mathf.Min(stateShadowAlpha, 0.18f) * defeatedScale;
        float coreAlpha = (visualState == CharacterBattleVisualState.Defeat ? 0.24f : 0.45f) *
                          Mathf.Clamp01(stateShadowAlpha / 0.34f);

        shadowRenderer.transform.localPosition = new Vector3(0f, 0.055f, 0f);
        shadowRenderer.transform.localScale = new Vector3(width * 1.10f, Mathf.Max(0.02f, height * 1.06f), 1f);
        shadowRenderer.color = new Color(0f, 0f, 0f, softAlpha);

        if (shadowCoreRenderer != null)
        {
            shadowCoreRenderer.enabled = true;
            shadowCoreRenderer.sprite = GetOvalSprite();
            shadowCoreRenderer.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            shadowCoreRenderer.transform.localScale =
                new Vector3(width * 0.55f, Mathf.Max(0.018f, height * 0.52f), 1f);
            shadowCoreRenderer.color = new Color(0f, 0f, 0f, coreAlpha);
        }
    }

    private void UpdateCastShadow(float stateShadowAlpha)
    {
        if (castShadowRenderer == null || bodyRenderer == null || bodyRenderer.sprite == null ||
            bodyTransform == null || visual == null)
        {
            ClearRenderer(castShadowRenderer);
            return;
        }

        if (visualState == CharacterBattleVisualState.Move)
        {
            ClearRenderer(castShadowRenderer);
            return;
        }

        float defeatedFade = visualState == CharacterBattleVisualState.Defeat ? 0.55f : 1f;
        float alpha = 0.20f * Mathf.Clamp01(stateShadowAlpha / 0.34f) * defeatedFade;
        castShadowRenderer.enabled = alpha > 0.01f;
        if (!castShadowRenderer.enabled)
        {
            return;
        }

        float xScale = Mathf.Abs(bodyTransform.localScale.x) * 0.92f;
        float yScale = Mathf.Abs(bodyTransform.localScale.y) * 0.23f;
        castShadowRenderer.sprite = bodyRenderer.sprite;
        castShadowRenderer.flipX = false;
        castShadowRenderer.transform.localPosition = new Vector3(0.16f, 0.075f, 0f);
        castShadowRenderer.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
        castShadowRenderer.transform.localScale = new Vector3(xScale, yScale, 1f);
        castShadowRenderer.color = new Color(0f, 0f, 0f, alpha);
    }

    private void UpdateGroundBlendOverlay()
    {
        if (groundBlendRenderer == null || bodyRenderer == null || bodyRenderer.sprite == null)
        {
            ClearRenderer(groundBlendRenderer);
            return;
        }

        Sprite blendSprite = GetGroundBlendSprite();
        Bounds bodyBounds = bodyRenderer.sprite.bounds;
        Bounds blendBounds = blendSprite.bounds;
        float width = Mathf.Max(0.01f, bodyBounds.size.x * 1.08f);
        float height = Mathf.Max(0.01f, bodyBounds.size.y * 0.16f);

        groundBlendRenderer.enabled = true;
        groundBlendRenderer.sprite = blendSprite;
        groundBlendRenderer.transform.localPosition = new Vector3(0f, bodyBounds.min.y - 0.002f, 0f);
        groundBlendRenderer.transform.localRotation = Quaternion.identity;
        groundBlendRenderer.transform.localScale =
            new Vector3(width / Mathf.Max(0.001f, blendBounds.size.x),
                        height / Mathf.Max(0.001f, blendBounds.size.y),
                        1f);
        groundBlendRenderer.color = defeated
                                        ? new Color(groundBlendTint.r, groundBlendTint.g, groundBlendTint.b,
                                                    groundBlendTint.a * 0.70f)
                                        : groundBlendTint;
    }

    private void UpdateInkRim()
    {
        if (inkRimRenderer == null || bodyRenderer == null || bodyRenderer.sprite == null || bodyTransform == null)
        {
            ClearRenderer(inkRimRenderer);
            return;
        }

        if (visualState == CharacterBattleVisualState.Move)
        {
            ClearRenderer(inkRimRenderer);
            return;
        }

        float alpha = visualState == CharacterBattleVisualState.Defeat ? inkRimTint.a * 0.65f : inkRimTint.a;
        inkRimRenderer.enabled = alpha > 0.01f;
        if (!inkRimRenderer.enabled)
        {
            return;
        }

        inkRimRenderer.sprite = bodyRenderer.sprite;
        inkRimRenderer.flipX = bodyRenderer.flipX;
        inkRimRenderer.transform.localPosition = bodyTransform.localPosition + new Vector3(0f, -0.002f, 0f);
        inkRimRenderer.transform.localRotation = bodyTransform.localRotation;
        inkRimRenderer.transform.localScale = bodyTransform.localScale * 1.008f;
        inkRimRenderer.color = new Color(inkRimTint.r, inkRimTint.g, inkRimTint.b, alpha);
    }

    private void UpdateFootOcclusion()
    {
        if (footOcclusionRenderer == null || bodyRenderer == null || bodyRenderer.sprite == null ||
            bodyTransform == null || visual == null)
        {
            ClearRenderer(footOcclusionRenderer);
            return;
        }

        Sprite occlusionSprite = GetFootOcclusionSprite();
        Bounds spriteBounds = occlusionSprite.bounds;
        Bounds bodyBounds = bodyRenderer.sprite.bounds;
        float bodyBottom = bodyTransform.localPosition.y + (bodyBounds.min.y * bodyTransform.localScale.y);
        float width = Mathf.Max(0.18f, visual.shadowWidth * 0.88f);
        float height = Mathf.Max(0.035f, visual.shadowHeight * 0.58f);
        float alpha = visualState == CharacterBattleVisualState.Defeat ? footOcclusionTint.a * 0.65f : footOcclusionTint.a;

        footOcclusionRenderer.enabled = alpha > 0.01f;
        if (!footOcclusionRenderer.enabled)
        {
            return;
        }

        footOcclusionRenderer.sprite = occlusionSprite;
        footOcclusionRenderer.flipX = false;
        footOcclusionRenderer.transform.localPosition = new Vector3(0f, bodyBottom + 0.022f, 0f);
        footOcclusionRenderer.transform.localRotation = Quaternion.identity;
        footOcclusionRenderer.transform.localScale =
            new Vector3(width / Mathf.Max(0.001f, spriteBounds.size.x),
                        height / Mathf.Max(0.001f, spriteBounds.size.y),
                        1f);
        footOcclusionRenderer.color =
            new Color(footOcclusionTint.r, footOcclusionTint.g, footOcclusionTint.b, alpha);
    }

    private Color ApplySceneTint(Color source)
    {
        Color tinted = new Color(source.r * sceneAmbientTint.r,
                                 source.g * sceneAmbientTint.g,
                                 source.b * sceneAmbientTint.b,
                                 source.a);
        float gray = tinted.grayscale;
        tinted.r = Mathf.Lerp(tinted.r, gray, sceneDesaturation);
        tinted.g = Mathf.Lerp(tinted.g, gray, sceneDesaturation);
        tinted.b = Mathf.Lerp(tinted.b, gray, sceneDesaturation);
        tinted.a = 1f;
        return tinted;
    }

    private static Color OpaqueCharacterTint(Color tint)
    {
        tint.a = 1f;
        return tint;
    }

    private static void ClearRenderer(SpriteRenderer renderer)
    {
        if (renderer == null)
        {
            return;
        }

        renderer.sprite = null;
        renderer.enabled = false;
    }

    private void ApplyLayerSway(float time, float progress, float footStride01)
    {
        if (visual == null || !visual.enableLayerSway)
        {
            ResetLayerPose(baseLayerRenderer);
            ResetLayerPose(outfitLayerRenderer);
            ResetLayerPose(hairLayerRenderer);
            ResetLayerPose(faceLayerRenderer);
            ResetLayerPose(weaponLayerRenderer);
            ResetLayerPose(accessoryLayerRenderer);
            return;
        }

        CharacterLivingMotionProfile profile = MotionProfile();
        float hairAmount = profile == null ? 1f : profile.hairSwayAmount;
        float weaponAmount = profile == null ? 1f : profile.weaponSwayAmount;
        float accessoryAmount = profile == null ? 1f : profile.accessorySwayAmount;
        float idle = Mathf.Sin((time * 1.7f) + phaseSeed);
        float move = visualState == CharacterBattleVisualState.Move && footStride01 >= 0f
                         ? Mathf.Sin(footStride01 * Mathf.PI * 2f)
                         : 0f;
        float attack = visualState == CharacterBattleVisualState.Attack || visualState == CharacterBattleVisualState.Skill
                           ? Mathf.Sin(progress * Mathf.PI)
                           : 0f;
        float hit = visualState == CharacterBattleVisualState.Hit ? Mathf.Sin(progress * Mathf.PI) : 0f;
        float wait = visualState == CharacterBattleVisualState.Wait ? 0.45f : 1f;

        SetLayerPose(hairLayerRenderer,
                     new Vector3(facingSign * ((idle * 0.006f) + (move * 0.012f) - (attack * 0.010f) + (hit * 0.014f)) *
                                 hairAmount * wait,
                                 ((Mathf.Abs(idle) * 0.004f) + (Mathf.Abs(move) * 0.006f)) * hairAmount * wait,
                                 0f),
                     ((idle * 1.3f) + (move * 2.0f) - (attack * 3.0f) + (hit * 5f)) * hairAmount * wait);
        SetLayerPose(weaponLayerRenderer,
                     new Vector3(facingSign * ((idle * 0.004f) + (move * 0.010f) - (attack * 0.030f) +
                                               (hit * 0.018f)) * weaponAmount * wait,
                                 Mathf.Abs(move) * 0.004f * weaponAmount * wait,
                                 0f),
                     ((idle * 0.9f) + (move * 2.6f) - (attack * 7.0f) + (hit * 6f)) * weaponAmount * wait);
        SetLayerPose(accessoryLayerRenderer,
                     new Vector3(facingSign * ((idle * 0.005f) + (move * 0.009f) - (attack * 0.012f)) *
                                 accessoryAmount * wait,
                                 Mathf.Abs(idle) * 0.003f * accessoryAmount * wait,
                                 0f),
                     ((idle * 1.2f) + (move * 1.8f) - (attack * 3.5f)) * accessoryAmount * wait);
        SetLayerPose(outfitLayerRenderer,
                     new Vector3(facingSign * ((idle * 0.003f) + (move * 0.006f) - (attack * 0.006f)) * wait,
                                 Mathf.Abs(move) * 0.003f * wait,
                                 0f),
                     ((idle * 0.5f) + (move * 1.0f) - (attack * 1.5f)) * wait);
        SetLayerPose(faceLayerRenderer, Vector3.zero, 0f);
        SetLayerPose(baseLayerRenderer, Vector3.zero, 0f);
    }

    private static void ResetLayerPose(SpriteRenderer renderer)
    {
        SetLayerPose(renderer, Vector3.zero, 0f);
    }

    private static void SetLayerPose(SpriteRenderer renderer, Vector3 localPosition, float rotation)
    {
        if (renderer == null || !renderer.enabled)
        {
            return;
        }

        renderer.transform.localPosition = localPosition;
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, rotation);
        renderer.transform.localScale = Vector3.one;
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
        shadowCoreRenderer = shadowCoreRenderer != null ? shadowCoreRenderer : EnsureChildRenderer("ShadowCore");
        castShadowRenderer = castShadowRenderer != null ? castShadowRenderer : EnsureChildRenderer("CastShadow");
        inkRimRenderer = inkRimRenderer != null ? inkRimRenderer : EnsureChildRenderer("InkRim");
        leftFootRenderer = leftFootRenderer != null ? leftFootRenderer : EnsureChildRenderer("LeftFootContact");
        rightFootRenderer = rightFootRenderer != null ? rightFootRenderer : EnsureChildRenderer("RightFootContact");
        selectionRenderer = selectionRenderer != null ? selectionRenderer : EnsureChildRenderer("SelectionRing");
        bodyRenderer = bodyRenderer != null ? bodyRenderer : EnsureChildRenderer("FullBody");
        groundBlendRenderer = groundBlendRenderer != null ? groundBlendRenderer : EnsureChildRenderer("GroundBlend", bodyRenderer.transform);
        footOcclusionRenderer = footOcclusionRenderer != null ? footOcclusionRenderer : EnsureChildRenderer("FootOcclusion");
        baseLayerRenderer = baseLayerRenderer != null ? baseLayerRenderer : EnsureChildRenderer("Layer_Base", bodyRenderer.transform);
        outfitLayerRenderer = outfitLayerRenderer != null ? outfitLayerRenderer : EnsureChildRenderer("Layer_Outfit", bodyRenderer.transform);
        hairLayerRenderer = hairLayerRenderer != null ? hairLayerRenderer : EnsureChildRenderer("Layer_Hair", bodyRenderer.transform);
        faceLayerRenderer = faceLayerRenderer != null ? faceLayerRenderer : EnsureChildRenderer("Layer_Face", bodyRenderer.transform);
        emotionFaceRenderer = emotionFaceRenderer != null ? emotionFaceRenderer : EnsureChildRenderer("Layer_EmotionFace", bodyRenderer.transform);
        weaponLayerRenderer = weaponLayerRenderer != null ? weaponLayerRenderer : EnsureChildRenderer("Layer_Weapon", bodyRenderer.transform);
        accessoryLayerRenderer = accessoryLayerRenderer != null ? accessoryLayerRenderer : EnsureChildRenderer("Layer_Accessory", bodyRenderer.transform);
        motionTrailRenderer = motionTrailRenderer != null ? motionTrailRenderer : EnsureChildRenderer("MotionTrail");
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
        if (shadowRenderer == null || shadowCoreRenderer == null || castShadowRenderer == null ||
            groundBlendRenderer == null || inkRimRenderer == null || footOcclusionRenderer == null ||
            bodyRenderer == null || selectionRenderer == null || motionTrailRenderer == null || effectRenderer == null ||
            leftFootRenderer == null || rightFootRenderer == null)
        {
            return;
        }

        // Interleave by row while keeping legal painted-map units above the flattened foreground art.
        // World y = (x+y) * tileHeight/2, so one row step = 0.31 world units = 40 orders.
        int selectedBoost = selected && !defeated ? SelectedSortingBoost : 0;
        int order = baseSortingOrder + 350 - Mathf.RoundToInt(transform.position.y * (40f / 0.31f)) +
                    (visual != null ? visual.sortingOffset : 0) + selectedBoost;
        shadowRenderer.sortingLayerName = sortingLayerName;
        shadowCoreRenderer.sortingLayerName = sortingLayerName;
        castShadowRenderer.sortingLayerName = sortingLayerName;
        groundBlendRenderer.sortingLayerName = sortingLayerName;
        inkRimRenderer.sortingLayerName = sortingLayerName;
        footOcclusionRenderer.sortingLayerName = sortingLayerName;
        bodyRenderer.sortingLayerName = sortingLayerName;
        selectionRenderer.sortingLayerName = sortingLayerName;
        motionTrailRenderer.sortingLayerName = sortingLayerName;
        effectRenderer.sortingLayerName = sortingLayerName;
        shadowRenderer.sortingOrder = order - 4;
        shadowCoreRenderer.sortingOrder = order - 3;
        castShadowRenderer.sortingOrder = order - 2;
        inkRimRenderer.sortingOrder = order - 1;
        leftFootRenderer.sortingLayerName = sortingLayerName;
        rightFootRenderer.sortingLayerName = sortingLayerName;
        leftFootRenderer.sortingOrder = order + 8;
        rightFootRenderer.sortingOrder = order + 8;
        selectionRenderer.sortingOrder = order - 1;
        motionTrailRenderer.sortingOrder = order - 1;
        bodyRenderer.sortingOrder = order;
        SetLayerSorting(baseLayerRenderer, order);
        SetLayerSorting(outfitLayerRenderer, order + 1);
        SetLayerSorting(hairLayerRenderer, order + 2);
        SetLayerSorting(faceLayerRenderer, order + 3);
        SetLayerSorting(weaponLayerRenderer, order + 4);
        SetLayerSorting(accessoryLayerRenderer, order + 5);
        SetLayerSorting(groundBlendRenderer, order + 6);
        footOcclusionRenderer.sortingOrder = order + 7;
        int emotionFaceOrderOffset = visual == null ? 6 : visual.emotionFaceSortingOffset;
        SetLayerSorting(emotionFaceRenderer, order + Mathf.Max(8, emotionFaceOrderOffset));
        effectRenderer.sortingOrder = order + Mathf.Max(9, emotionFaceOrderOffset + 1);
    }

    private Sprite SelectStateSprite()
    {
        if (visual == null)
        {
            return null;
        }

        switch (visualState)
        {
        case CharacterBattleVisualState.SelectedIdle:
            return SelectIdleFallback();
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
        case CharacterBattleVisualState.TurnStart:
            return SelectIdleFallback();
        default:
            return SelectIdleFallback();
        }
    }

    private static float ClipDuration(CharacterSpriteAnimationClipData clip, float fallback)
    {
        return clip != null && clip.Duration > 0f ? clip.Duration : fallback;
    }

    private static Sprite ClipSprite(CharacterSpriteAnimationClipData clip, float elapsedSeconds,
                                     float normalizedFallback, bool defaultLoop)
    {
        return clip != null && clip.HasFrames ? clip.Evaluate(elapsedSeconds, normalizedFallback, defaultLoop) : null;
    }

    /// <summary>프레임 배열에서 위상(t01)에 맞는 장을 고른다. 배열이 비면 null을 돌려 단일 포즈 폴백을 쓴다.</summary>
    private static Sprite FrameSprite(Sprite[] primary, Sprite[] fallback, float t01, bool loop)
    {
        Sprite[] frames = primary != null && primary.Length > 0 ? primary : fallback;
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        if (frames.Length == 1)
        {
            return frames[0];
        }

        float phase = loop ? Mathf.Repeat(t01, 1f) : Mathf.Clamp01(t01);
        int index = Mathf.Min(frames.Length - 1, Mathf.FloorToInt(phase * frames.Length));
        return frames[index];
    }

    private Sprite IdleFrameSprite(CharacterOutfitData outfit)
    {
        Sprite[] frames = outfit != null && outfit.idleFrames != null && outfit.idleFrames.Length > 0
                              ? outfit.idleFrames
                              : visual.idleFrames;
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        float rate = Mathf.Max(0.5f, visual.idleFrameRate);
        int index = Mathf.FloorToInt((Time.time + phaseSeed) * rate) % frames.Length;
        return frames[index];
    }

    private bool HasMoveFrames()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        Sprite[] frames = DirectionalMoveFrames(outfit);
        return frames != null && frames.Length > 1;
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

        if (HasMoveFrames())
        {
            return move;
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
        if (visual != null && IsFacingBack() && visual.idleBackPoseSprite != null)
        {
            return visual.idleBackPoseSprite;
        }

        if (visual != null && IsFacingSide() && visual.idleSidePoseSprite != null)
        {
            return visual.idleSidePoseSprite;
        }

        Sprite frame = IdleFrameSprite(outfit);
        if (frame != null)
        {
            return frame;
        }

        return outfit != null && outfit.idlePoseSprite != null ? outfit.idlePoseSprite : visual.idlePoseSprite;
    }

    private Sprite MovePoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        Sprite frame = MoveFrameSprite(outfit);
        if (frame != null)
        {
            return frame;
        }

        if (visual != null && IsFacingBack() && visual.moveBackPoseSprite != null)
        {
            return visual.moveBackPoseSprite;
        }

        if (visual != null && IsFacingSide() && visual.moveSidePoseSprite != null)
        {
            return visual.moveSidePoseSprite;
        }

        return visual != null && visual.movePoseSprite != null
                   ? visual.movePoseSprite
                   : outfit != null ? outfit.movePoseSprite : null;
    }

    private Sprite MoveFrameSprite(CharacterOutfitData outfit)
    {
        Sprite[] frames = DirectionalMoveFrames(outfit);
        if (frames == null || frames.Length == 0)
        {
            return null;
        }

        return FrameSprite(frames, null, MoveStride01(Time.time - stateStartedAt), true);
    }

    private Sprite[] DirectionalMoveFrames(CharacterOutfitData outfit)
    {
        if (visual != null && IsFacingBack() && visual.moveBackFrames != null && visual.moveBackFrames.Length > 0)
        {
            return visual.moveBackFrames;
        }

        if (visual != null && IsFacingSide() && visual.moveSideFrames != null && visual.moveSideFrames.Length > 0)
        {
            return visual.moveSideFrames;
        }

        if (visual != null && visual.moveFrames != null && visual.moveFrames.Length > 0)
        {
            return visual.moveFrames;
        }

        return outfit != null ? outfit.moveFrames : null;
    }

    private Sprite AttackPoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        if (visual != null && IsFacingBack() && visual.moveBackPoseSprite != null)
        {
            return visual.moveBackPoseSprite;
        }

        if (visual != null && IsFacingSide() && visual.moveSidePoseSprite != null)
        {
            return visual.moveSidePoseSprite;
        }

        return outfit != null && outfit.attackPoseSprite != null ? outfit.attackPoseSprite : visual.attackPoseSprite;
    }

    private Sprite SkillPoseSprite()
    {
        CharacterOutfitData outfit = ActiveOutfit();
        if (visual != null && IsFacingBack() && visual.moveBackPoseSprite != null)
        {
            return visual.moveBackPoseSprite;
        }

        if (visual != null && IsFacingSide() && visual.moveSidePoseSprite != null)
        {
            return visual.moveSidePoseSprite;
        }

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
        bool hasLayerSprites = outfit != null && outfit.useLayeredSprites && CanUseLayeredSpritesForState(visualState) &&
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

    private static bool CanUseLayeredSpritesForState(CharacterBattleVisualState state)
    {
        return false;
    }

    private void SetBodySprite(Sprite sprite)
    {
        if (bodyRenderer != null && bodyRenderer.sprite != sprite)
        {
            bodyRenderer.sprite = sprite;
            if (pixelMode && sprite != null && sprite.texture != null)
            {
                // 픽셀 스프라이트는 또렷하게(보간 끔). 시트 텍스처 한 번만 바꿔도 모든 프레임에 적용됨.
                sprite.texture.filterMode = FilterMode.Point;
            }
        }
    }

    // 픽셀 스프라이트 모드: 리빙2D 코스메틱 레이어/이펙트를 끄고 body 프레임만 또렷이 렌더한다.
    // (그림자/선택링/body는 유지. 플래그 기본 false라 기존 캐릭터엔 영향 없음.)
    private void ConfigurePixelMode()
    {
        DisablePixelLayer(baseLayerRenderer);
        DisablePixelLayer(outfitLayerRenderer);
        DisablePixelLayer(hairLayerRenderer);
        DisablePixelLayer(faceLayerRenderer);
        DisablePixelLayer(emotionFaceRenderer);
        DisablePixelLayer(weaponLayerRenderer);
        DisablePixelLayer(accessoryLayerRenderer);
        DisablePixelLayer(inkRimRenderer);
        DisablePixelLayer(groundBlendRenderer);
        DisablePixelLayer(footOcclusionRenderer);
        DisablePixelLayer(castShadowRenderer);
        DisablePixelLayer(motionTrailRenderer);
        DisablePixelLayer(leftFootRenderer);
        DisablePixelLayer(rightFootRenderer);
        if (bodyRenderer != null && bodyRenderer.sprite != null && bodyRenderer.sprite.texture != null)
        {
            bodyRenderer.sprite.texture.filterMode = FilterMode.Point;
        }
    }

    private static void DisablePixelLayer(SpriteRenderer renderer)
    {
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private static void SetLayerSprite(SpriteRenderer renderer, Sprite sprite)
    {
        if (renderer == null)
        {
            return;
        }

        bool changed = renderer.sprite != sprite || renderer.enabled != (sprite != null);
        renderer.sprite = sprite;
        renderer.enabled = sprite != null;
        if (changed)
        {
            renderer.transform.localPosition = Vector3.zero;
            renderer.transform.localRotation = Quaternion.identity;
            renderer.transform.localScale = Vector3.one;
        }
    }

    private void ApplyLayerTint(Color tint)
    {
        tint = OpaqueCharacterTint(tint);
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
            renderer.color = OpaqueCharacterTint(tint);
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

    private void UpdateFootContacts(bool moving, float stride01)
    {
        if (leftFootRenderer == null || rightFootRenderer == null)
        {
            return;
        }

        if (!moving || stride01 < 0f)
        {
            leftFootRenderer.enabled = false;
            rightFootRenderer.enabled = false;
            return;
        }

        SetFootContact(leftFootRenderer, -1f, stride01);
        SetFootContact(rightFootRenderer, 1f, stride01 + 0.5f);
    }

    private void SetFootContact(SpriteRenderer renderer, float side, float phase)
    {
        renderer.sprite = GetFootContactSprite();
        renderer.enabled = renderer.sprite != null;
        if (!renderer.enabled)
        {
            return;
        }

        float cycle = Mathf.Repeat(phase, 1f);
        float plant = FootPlant(cycle, 0f);
        float swing = Mathf.Sin(cycle * Mathf.PI * 2f);
        float lift = Mathf.Pow(Mathf.Clamp01(1f - plant), 0.75f);
        Vector2 forwardAxis = FacingVectorOrDefault();
        Vector2 sideAxis = new Vector2(-forwardAxis.y, forwardAxis.x);
        if (sideAxis.sqrMagnitude <= 0.0001f)
        {
            sideAxis = Vector2.up;
        }

        sideAxis.Normalize();
        float sideSpacing = Mathf.Lerp(0.090f, 0.158f, FacingSideAmount());
        float stride = Mathf.Lerp(0.075f, 0.142f, FacingSideAmount());
        float baseY = BodyBottomY() + 0.024f;
        Vector2 offset = (sideAxis * side * sideSpacing) + (forwardAxis * swing * stride);
        float liftY = lift * Mathf.Lerp(0.020f, 0.038f, FacingSideAmount());

        renderer.transform.localPosition = new Vector3(offset.x, baseY + offset.y + liftY, -0.01f);
        renderer.transform.localScale = new Vector3(0.24f + (plant * 0.16f) - (lift * 0.04f),
                                                    0.13f + (plant * 0.08f) + (lift * 0.02f),
                                                    1f);
        float heading = Mathf.Atan2(forwardAxis.y, forwardAxis.x) * Mathf.Rad2Deg;
        renderer.transform.localRotation = Quaternion.Euler(0f, 0f, heading + (side * 8f) - (swing * 5f));

        Color color = Color.Lerp(new Color(0.68f, 0.76f, 0.82f, 1f), ElementPrimary(1f), 0.18f);
        color.a = 0.16f + (plant * 0.34f) + (lift * 0.06f);
        renderer.color = color;
    }

    private float BodyBottomY()
    {
        if (bodyRenderer == null || bodyRenderer.sprite == null || bodyTransform == null)
        {
            return 0.045f;
        }

        return bodyTransform.localPosition.y + (bodyRenderer.sprite.bounds.min.y * bodyTransform.localScale.y);
    }

    private static float FootPlant(float stride01, float contactPhase)
    {
        float distance = Mathf.Abs(Mathf.Repeat(stride01 - contactPhase + 0.5f, 1f) - 0.5f);
        return 1f - Mathf.SmoothStep(0.12f, 0.36f, distance);
    }

    private CombatElementType ActiveElement()
    {
        return visual != null && visual.weaponAnimationSet != null
                   ? visual.weaponAnimationSet.elementType : CombatElementType.None;
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
               state == CharacterBattleVisualState.Guard ||
               state == CharacterBattleVisualState.TurnStart;
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

    private static Sprite GetGroundBlendSprite()
    {
        if (groundBlendSprite != null)
        {
            return groundBlendSprite;
        }

        Texture2D texture = new Texture2D(128, 48, TextureFormat.RGBA32, false);
        texture.name = "GeneratedCharacterGroundBlend";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            float vertical = 1f - ((y + 0.5f) / texture.height);
            float fadeUp = vertical * vertical;
            for (int x = 0; x < texture.width; x++)
            {
                float nx = Mathf.Abs(((x + 0.5f) / texture.width * 2f) - 1f);
                float sideFade = 1f - Mathf.SmoothStep(0.56f, 1f, nx);
                float alpha = Mathf.Clamp01(fadeUp * sideFade);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        groundBlendSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0f), 128f);
        groundBlendSprite.name = "GeneratedCharacterGroundBlend";
        return groundBlendSprite;
    }

    private static Sprite GetFootOcclusionSprite()
    {
        if (footOcclusionSprite != null)
        {
            return footOcclusionSprite;
        }

        Texture2D texture = new Texture2D(160, 44, TextureFormat.RGBA32, false);
        texture.name = "GeneratedCharacterFootOcclusion";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            float vy = (y + 0.5f) / texture.height;
            for (int x = 0; x < texture.width; x++)
            {
                float u = (x + 0.5f) / texture.width;
                float nx = Mathf.Abs((u * 2f) - 1f);
                float mound = 0.34f + Mathf.PerlinNoise(u * 8.0f + 4.1f, 11.7f) * 0.30f;
                float notches = Mathf.PerlinNoise(u * 22.0f + 0.7f, 3.5f) * 0.15f;
                float edgeFade = 1f - Mathf.SmoothStep(0.68f, 1f, nx);
                float verticalFade = 1f - Mathf.SmoothStep(mound - notches, mound + 0.20f, vy);
                float baseFade = Mathf.SmoothStep(0.02f, 0.18f, vy);
                float alpha = Mathf.Clamp01(edgeFade * verticalFade * baseFade);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        footOcclusionSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.25f), 128f);
        footOcclusionSprite.name = "GeneratedCharacterFootOcclusion";
        return footOcclusionSprite;
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

    private static Sprite GetElementSlashSprite(CombatElementType element)
    {
        if (elementSlashSprites == null)
        {
            elementSlashSprites = new Sprite[CombatElementSpriteCount];
        }

        int index = ElementIndex(element);
        if (elementSlashSprites[index] == null)
        {
            elementSlashSprites[index] = CreateElementSlashSprite(element);
        }

        return elementSlashSprites[index];
    }

    private static Sprite GetElementSkillSprite(CombatElementType element)
    {
        if (elementSkillSprites == null)
        {
            elementSkillSprites = new Sprite[CombatElementSpriteCount];
        }

        int index = ElementIndex(element);
        if (elementSkillSprites[index] == null)
        {
            elementSkillSprites[index] = CreateElementSkillSprite(element);
        }

        return elementSkillSprites[index];
    }

    private static Sprite GetElementImpactSprite(CombatElementType element)
    {
        if (elementImpactSprites == null)
        {
            elementImpactSprites = new Sprite[CombatElementSpriteCount];
        }

        int index = ElementIndex(element);
        if (elementImpactSprites[index] == null)
        {
            elementImpactSprites[index] = CreateElementImpactSprite(element);
        }

        return elementImpactSprites[index];
    }

    private static int ElementIndex(CombatElementType element)
    {
        int value = (int)element;
        return value < 0 || value >= CombatElementSpriteCount ? 0 : value;
    }

    private static Sprite CreateElementSlashSprite(CombatElementType element)
    {
        switch (element)
        {
        case CombatElementType.Fire:
            return CreateGeneratedSprite("GeneratedFireSlash", 180, 96, 120f, (nx, ny) => {
                float arc = Mathf.Abs((nx * nx * 0.78f) + ((ny + 0.72f) * (ny + 0.72f) * 2.15f) - 1f);
                float tongue = 0.58f + (0.42f * Mathf.Sin((nx * 18f) + (ny * 7f)));
                float core = Mathf.Clamp01((0.22f - arc) * 7.8f);
                float tail = Mathf.SmoothStep(-0.95f, 0.05f, nx) * (1f - Mathf.SmoothStep(0.74f, 1f, nx));
                return new Color(1f, 1f, 1f, core * tail * tongue);
            });
        case CombatElementType.Ice:
            return CreateGeneratedSprite("GeneratedIceShardSlash", 180, 96, 120f, (nx, ny) => {
                float diagonal = Mathf.Abs((ny * 0.82f) - ((nx * 0.24f) - 0.04f));
                float shards = Mathf.Abs(Mathf.Sin((nx * 15f) + (ny * 5f)));
                float spine = Mathf.Clamp01((0.075f - diagonal) * 13f);
                float chips = Mathf.Clamp01((0.028f - Mathf.Abs(diagonal - 0.15f * shards)) * 18f);
                float gate = Mathf.SmoothStep(-0.88f, -0.05f, nx) * (1f - Mathf.SmoothStep(0.78f, 1f, nx));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(spine + chips * 0.8f) * gate);
            });
        case CombatElementType.Lightning:
            return CreateGeneratedSprite("GeneratedLightningSlash", 180, 96, 120f, (nx, ny) => {
                float center = 0.12f * Mathf.Sign(Mathf.Sin((nx + 0.08f) * 11f));
                float bolt = Mathf.Clamp01((0.060f - Mathf.Abs(ny - center)) * 17f);
                float branch = Mathf.Clamp01((0.030f - Mathf.Abs((ny + 0.24f) - (nx * -0.34f))) * 18f);
                float gate = Mathf.SmoothStep(-0.92f, -0.04f, nx) * (1f - Mathf.SmoothStep(0.86f, 1f, nx));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(bolt + branch * 0.55f) * gate);
            });
        case CombatElementType.WindFlower:
            return CreateGeneratedSprite("GeneratedWindFlowerSlash", 180, 96, 120f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny * 1.5f));
                float angle = Mathf.Atan2(ny, nx);
                float spiral = Mathf.Clamp01((0.080f - Mathf.Abs(r - (0.50f + (angle * 0.045f)))) * 12f);
                float petals = Mathf.Clamp01((0.15f - Mathf.Abs(Mathf.Sin(angle * 5f) * r - 0.22f)) * 4.2f);
                float gate = Mathf.SmoothStep(-0.92f, -0.10f, nx) * (1f - Mathf.SmoothStep(0.90f, 1f, nx));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(spiral + petals * 0.45f) * gate);
            });
        case CombatElementType.Light:
            return CreateGeneratedSprite("GeneratedLightSlash", 180, 96, 120f, (nx, ny) => {
                float arc = Mathf.Abs((nx * nx * 0.72f) + ((ny + 0.70f) * (ny + 0.70f) * 2.0f) - 1f);
                float ray = Mathf.Clamp01((0.045f - Mathf.Abs(ny - (nx * 0.18f))) * 18f);
                float core = Mathf.Clamp01((0.20f - arc) * 8.5f);
                float gate = Mathf.SmoothStep(-0.92f, -0.04f, nx) * (1f - Mathf.SmoothStep(0.86f, 1f, nx));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(core + ray * 0.55f) * gate);
            });
        case CombatElementType.DarkPoison:
            return CreateGeneratedSprite("GeneratedPoisonSlash", 180, 96, 120f, (nx, ny) => {
                float cloudA = Mathf.Clamp01((0.30f - ((nx + 0.25f) * (nx + 0.25f) + ((ny + 0.04f) * (ny + 0.04f) * 1.8f))) * 3.1f);
                float cloudB = Mathf.Clamp01((0.25f - ((nx - 0.16f) * (nx - 0.16f) + ((ny - 0.06f) * (ny - 0.06f) * 2.2f))) * 3.4f);
                float holes = 0.62f + (0.38f * Mathf.Sin((nx * 12f) - (ny * 16f)));
                float gate = Mathf.SmoothStep(-0.95f, -0.05f, nx) * (1f - Mathf.SmoothStep(0.88f, 1f, nx));
                return new Color(1f, 1f, 1f, Mathf.Clamp01(cloudA + cloudB) * holes * gate);
            });
        default:
            return GetSlashSprite();
        }
    }

    private static Sprite CreateElementSkillSprite(CombatElementType element)
    {
        switch (element)
        {
        case CombatElementType.Fire:
            return CreateGeneratedSprite("GeneratedFireNova", 132, 132, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float flame = Mathf.Clamp01((0.70f - r) * 1.6f) * (0.55f + 0.45f * Mathf.Sin(angle * 9f + r * 10f));
                float core = Mathf.Clamp01((0.28f - r) * 3.2f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(flame + core));
            });
        case CombatElementType.Ice:
            return CreateGeneratedSprite("GeneratedIceCrystalBurst", 132, 132, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float arms = Mathf.Clamp01((0.070f - Mathf.Abs(Mathf.Sin(angle * 6f) * r)) * 12f);
                float ring = Mathf.Clamp01((0.050f - Mathf.Abs(r - 0.54f)) * 18f);
                float core = Mathf.Clamp01((0.18f - r) * 4f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01((arms * (1f - r)) + ring + core));
            });
        case CombatElementType.Lightning:
            return CreateGeneratedSprite("GeneratedLightningField", 132, 132, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float boltA = Mathf.Clamp01((0.050f - Mathf.Abs(ny - (Mathf.Sign(Mathf.Sin(nx * 9f)) * 0.18f))) * 16f);
                float boltB = Mathf.Clamp01((0.040f - Mathf.Abs(nx + (ny * 0.45f))) * 14f);
                float core = Mathf.Clamp01((0.26f - r) * 3.4f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01((boltA + boltB) * (1f - r * 0.45f) + core));
            });
        case CombatElementType.WindFlower:
            return CreateGeneratedSprite("GeneratedPetalCyclone", 132, 132, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float swirl = Mathf.Clamp01((0.060f - Mathf.Abs(r - (0.22f + angle * 0.050f))) * 13f);
                float petals = Mathf.Clamp01(Mathf.Abs(Mathf.Sin(angle * 6f + r * 6f)) * (1f - r));
                float ring = Mathf.Clamp01((0.050f - Mathf.Abs(r - 0.62f)) * 16f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(swirl + petals * 0.38f + ring * 0.55f));
            });
        case CombatElementType.Light:
            return CreateGeneratedSprite("GeneratedHolySigil", 132, 132, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float ring = Mathf.Clamp01((0.055f - Mathf.Abs(r - 0.62f)) * 18f);
                float cross = Mathf.Clamp01((0.045f - Mathf.Min(Mathf.Abs(nx), Mathf.Abs(ny))) * 12f) *
                              Mathf.Clamp01(0.78f - r);
                float rays = Mathf.Clamp01(Mathf.Abs(Mathf.Cos(angle * 8f)) * (0.88f - r)) * 0.32f;
                float core = Mathf.Clamp01((0.24f - r) * 3.5f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(ring + cross + rays + core));
            });
        case CombatElementType.DarkPoison:
            return CreateGeneratedSprite("GeneratedPoisonCloudBurst", 132, 132, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float blob = Mathf.Clamp01((0.72f - r) * 1.35f);
                float mottled = 0.58f + 0.42f * Mathf.Sin(nx * 15f + ny * 11f);
                float core = Mathf.Clamp01((0.30f - r) * 3f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(blob * mottled + core));
            });
        default:
            return GetSkillBurstSprite();
        }
    }

    private static Sprite CreateElementImpactSprite(CombatElementType element)
    {
        switch (element)
        {
        case CombatElementType.Fire:
            return CreateGeneratedSprite("GeneratedFireImpact", 96, 96, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float spark = Mathf.Clamp01((0.16f - Mathf.Abs(Mathf.Sin(angle * 7f) * r - 0.18f)) * 5f);
                float core = Mathf.Clamp01((0.34f - r) * 3.4f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(spark + core));
            });
        case CombatElementType.Ice:
            return CreateGeneratedSprite("GeneratedIceImpact", 96, 96, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float chips = Mathf.Clamp01((0.080f - Mathf.Abs(Mathf.Sin(angle * 5f) * r)) * 10f);
                float core = Mathf.Clamp01((0.26f - r) * 3.8f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(chips * (1f - r) + core));
            });
        case CombatElementType.Lightning:
            return CreateGeneratedSprite("GeneratedLightningImpact", 96, 96, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float bolt = Mathf.Clamp01((0.055f - Mathf.Abs(nx - Mathf.Sign(Mathf.Sin(ny * 10f)) * 0.12f)) * 14f);
                float flash = Mathf.Clamp01((0.32f - r) * 3.2f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(bolt * (1f - r * 0.35f) + flash));
            });
        case CombatElementType.WindFlower:
            return CreateGeneratedSprite("GeneratedPetalImpact", 96, 96, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float petal = Mathf.Clamp01((0.13f - Mathf.Abs(Mathf.Sin(angle * 5f) * r - 0.16f)) * 4.8f);
                float core = Mathf.Clamp01((0.22f - r) * 3f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(petal + core));
            });
        case CombatElementType.Light:
            return CreateGeneratedSprite("GeneratedLightImpact", 96, 96, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float angle = Mathf.Atan2(ny, nx);
                float star = Mathf.Clamp01((0.13f - Mathf.Abs(Mathf.Sin(angle * 6f) * r - 0.18f)) * 5f);
                float pillar = Mathf.Clamp01((0.050f - Mathf.Abs(nx)) * 13f) * Mathf.Clamp01(0.88f - Mathf.Abs(ny));
                float core = Mathf.Clamp01((0.30f - r) * 3.4f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(star + pillar + core));
            });
        case CombatElementType.DarkPoison:
            return CreateGeneratedSprite("GeneratedPoisonImpact", 96, 96, 96f, (nx, ny) => {
                float r = Mathf.Sqrt((nx * nx) + (ny * ny));
                float cloud = Mathf.Clamp01((0.58f - r) * 1.8f);
                float mottled = 0.5f + 0.5f * Mathf.Sin(nx * 16f - ny * 13f);
                float core = Mathf.Clamp01((0.24f - r) * 3f);
                return new Color(1f, 1f, 1f, Mathf.Clamp01(cloud * mottled + core));
            });
        default:
            return GetImpactBurstSprite();
        }
    }

    private static Sprite CreateGeneratedSprite(string name, int width, int height, float pixelsPerUnit,
                                                System.Func<float, float, Color> sample)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = name;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                texture.SetPixel(x, y, sample(nx, ny));
            }
        }

        texture.Apply();
        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height),
                                      new Vector2(0.5f, 0.5f), pixelsPerUnit);
        sprite.name = name;
        return sprite;
    }

    private static Sprite GetFootstepDustSprite()
    {
        if (footstepDustSprite != null)
        {
            return footstepDustSprite;
        }

        footstepDustSprite = Resources.Load<Sprite>("UI/BattleHUD/Tactical/vfx_foot_dust_01");
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

    private static Sprite GetFootContactSprite()
    {
        if (footContactSprite != null)
        {
            return footContactSprite;
        }

        footContactSprite = Resources.Load<Sprite>("UI/BattleHUD/Tactical/vfx_step_flash");
        if (footContactSprite != null)
        {
            return footContactSprite;
        }

        Texture2D texture = new Texture2D(56, 24, TextureFormat.RGBA32, false);
        texture.name = "GeneratedFootContact";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                float nx = ((x + 0.5f) / texture.width * 2f) - 1f;
                float ny = ((y + 0.5f) / texture.height * 2f) - 1f;
                float heel = Mathf.Clamp01((0.52f - (((nx + 0.22f) * (nx + 0.22f) * 1.45f) + (ny * ny * 2.9f))) * 2.8f);
                float toe = Mathf.Clamp01((0.38f - (((nx - 0.34f) * (nx - 0.34f) * 1.9f) + ((ny + 0.04f) * (ny + 0.04f) * 3.2f))) * 3.2f);
                float arch = Mathf.Clamp01((0.12f - Mathf.Abs(ny + 0.08f)) * 4.8f) *
                             Mathf.Clamp01((0.60f - Mathf.Abs(nx * 0.95f)) * 1.6f);
                float alpha = Mathf.Clamp01((heel * 0.72f) + (toe * 0.88f) + (arch * 0.22f));
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha * 0.78f));
            }
        }

        texture.Apply();
        footContactSprite =
            Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 96f);
        footContactSprite.name = "GeneratedFootContact";
        return footContactSprite;
    }
}
}
