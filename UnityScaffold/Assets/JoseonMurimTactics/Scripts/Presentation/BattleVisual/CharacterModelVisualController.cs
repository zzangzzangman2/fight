using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class CharacterModelVisualController : MonoBehaviour, ICombatAnimationEventReceiver
{
    public CharacterVisualData visual;
    public GameObject modelInstance;
    public Transform modelRoot;
    public Animator animator;
    public SpriteRenderer shadowRenderer;
    public SpriteRenderer selectionRenderer;
    public string sortingLayerName = "Characters";
    public int baseSortingOrder = 1000;

    private Renderer[] modelRenderers;
    private MaterialPropertyBlock propertyBlock;
    private bool selected;
    private bool acted;
    private bool defeated;
    private float facingSign = 1f;
    private int currentSortingOrder = 1000;
    private static Sprite ovalSprite;

    private static readonly int MovingHash = Animator.StringToHash("Moving");
    private static readonly int SelectedHash = Animator.StringToHash("Selected");
    private static readonly int ActedHash = Animator.StringToHash("Acted");
    private static readonly int DefeatedHash = Animator.StringToHash("Defeated");
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int FacingXHash = Animator.StringToHash("FacingX");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int SkillHash = Animator.StringToHash("Skill");
    private static readonly int HitHash = Animator.StringToHash("Hit");
    private static readonly int GuardHash = Animator.StringToHash("Guard");
    private static readonly int VictoryHash = Animator.StringToHash("Victory");
    private static readonly int ResetToIdleHash = Animator.StringToHash("ResetToIdle");

    public int CurrentBodySortingOrder => currentSortingOrder;

    public void SetVisible(bool value)
    {
        enabled = value;
        if (modelRoot != null)
        {
            modelRoot.gameObject.SetActive(value);
        }

        if (shadowRenderer != null)
        {
            shadowRenderer.enabled = value;
        }

        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = value && selected && !defeated;
        }
    }

    private void Awake()
    {
        EnsurePresentationSprites();
    }

    private void LateUpdate()
    {
        UpdateSorting();
        UpdateSelectionPulse();
    }

    public void Bind(CombatantData combatant, bool isSelected)
    {
        Bind(combatant == null ? null : combatant.visual, isSelected);
    }

    public void Bind(CharacterVisualData visualData, bool isSelected)
    {
        visual = visualData;
        selected = isSelected;
        acted = false;
        defeated = false;
        EnsureModelRoot();
        RebuildModel();
        EnsurePresentationSprites();
        ApplyShadowSize();
        SetSelected(selected);
        SetActed(false);
        SetDefeated(false);
        PlayIdle();
    }

    public void SetSelected(bool value)
    {
        selected = value;
        if (selectionRenderer != null)
        {
            selectionRenderer.enabled = selected && !defeated;
        }

        SetAnimatorBool(SelectedHash, selected);
    }

    public void SetActed(bool value)
    {
        acted = value;
        SetAnimatorBool(ActedHash, acted);
        ApplyTint();
    }

    public void SetDefeated(bool value)
    {
        defeated = value;
        SetAnimatorBool(DefeatedHash, defeated);
        if (selectionRenderer != null && defeated)
        {
            selectionRenderer.enabled = false;
        }

        ApplyTint();
    }

    public void FaceToward(Vector3 worldPosition)
    {
        FaceDirection(new Vector2(worldPosition.x - transform.position.x, worldPosition.y - transform.position.y));
    }

    public void FaceDirection(Vector2 direction)
    {
        if (Mathf.Abs(direction.x) <= 0.01f)
        {
            return;
        }

        facingSign = direction.x < 0f ? -1f : 1f;
        SetAnimatorFloat(FacingXHash, facingSign);
        if (visual == null || !visual.modelFaceByYaw || modelRoot == null)
        {
            return;
        }

        Vector3 euler = visual.modelLocalEuler;
        euler.y += facingSign < 0f ? -90f : 90f;
        modelRoot.localEulerAngles = euler;
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
        SetAnimatorBool(MovingHash, false);
        SetAnimatorTrigger(ResetToIdleHash);
    }

    public void PlayMove()
    {
        SetAnimatorFloat(MoveSpeedHash, 1f);
        SetAnimatorBool(MovingHash, true);
    }

    public void PlayAttack()
    {
        SetAnimatorBool(MovingHash, false);
        SetAnimatorTrigger(AttackHash);
    }

    public void PlaySkill()
    {
        SetAnimatorBool(MovingHash, false);
        SetAnimatorTrigger(SkillHash);
    }

    public void PlayHit()
    {
        SetAnimatorBool(MovingHash, false);
        SetAnimatorTrigger(HitHash);
    }

    public void PlayGuard()
    {
        SetAnimatorBool(MovingHash, false);
        SetAnimatorTrigger(GuardHash);
    }

    public void PlayWait()
    {
        SetAnimatorBool(MovingHash, false);
        SetActed(true);
    }

    public void PlayVictory()
    {
        SetAnimatorBool(MovingHash, false);
        SetAnimatorTrigger(VictoryHash);
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
        SetAnimatorBool(MovingHash, false);
    }

    private void EnsureModelRoot()
    {
        if (modelRoot != null)
        {
            return;
        }

        Transform existing = transform.Find("Model3D");
        if (existing != null)
        {
            modelRoot = existing;
            return;
        }

        GameObject root = new GameObject("Model3D");
        root.transform.SetParent(transform, false);
        modelRoot = root.transform;
    }

    private void RebuildModel()
    {
        if (modelInstance != null)
        {
            Destroy(modelInstance);
            modelInstance = null;
        }

        animator = null;
        modelRenderers = null;
        if (visual == null || visual.modelPrefab == null || modelRoot == null)
        {
            return;
        }

        modelRoot.localPosition = visual.modelLocalOffset;
        modelRoot.localEulerAngles = visual.modelLocalEuler;
        modelRoot.localScale = Vector3.one * Mathf.Max(0.001f, visual.modelScale);
        modelInstance = Instantiate(visual.modelPrefab, modelRoot);
        modelInstance.name = visual.modelPrefab.name;
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        animator = modelInstance.GetComponentInChildren<Animator>(true);
        if (animator != null)
        {
            animator.applyRootMotion = false;
            if (visual.modelAnimatorController != null)
            {
                animator.runtimeAnimatorController = visual.modelAnimatorController;
            }
        }

        modelRenderers = modelRoot.GetComponentsInChildren<Renderer>(true);
        if (visual.modelKeepFeetOnGround)
        {
            CharacterModelGroundingSolver.Apply(modelRoot, modelRenderers, transform.position.y + visual.modelGroundY);
        }

        UpdateSorting();
    }

    private void EnsurePresentationSprites()
    {
        if (ovalSprite == null)
        {
            ovalSprite = CreateOvalSprite();
        }

        if (shadowRenderer == null)
        {
            GameObject shadow = new GameObject("Model Shadow");
            shadow.transform.SetParent(transform, false);
            shadow.transform.localPosition = new Vector3(0f, 0f, 0.02f);
            shadowRenderer = shadow.AddComponent<SpriteRenderer>();
            shadowRenderer.sprite = ovalSprite;
            shadowRenderer.color = new Color(0f, 0f, 0f, 0.32f);
            shadowRenderer.sortingLayerName = sortingLayerName;
        }

        if (selectionRenderer == null)
        {
            GameObject selection = new GameObject("Model Selection Ring");
            selection.transform.SetParent(transform, false);
            selection.transform.localPosition = new Vector3(0f, 0.005f, 0.01f);
            selectionRenderer = selection.AddComponent<SpriteRenderer>();
            selectionRenderer.sprite = ovalSprite;
            selectionRenderer.color = new Color(1f, 0.78f, 0.22f, 0.42f);
            selectionRenderer.sortingLayerName = sortingLayerName;
        }
    }

    private void ApplyShadowSize()
    {
        if (shadowRenderer == null || visual == null)
        {
            return;
        }

        shadowRenderer.transform.localScale = new Vector3(Mathf.Max(0.05f, visual.modelShadowWidth),
                                                          Mathf.Max(0.03f, visual.modelShadowHeight), 1f);
        if (selectionRenderer != null)
        {
            selectionRenderer.transform.localScale = shadowRenderer.transform.localScale * 1.18f;
        }
    }

    private void UpdateSorting()
    {
        currentSortingOrder = CharacterModelRendererSorting.CalculateOrder(transform, baseSortingOrder,
                                                                           visual == null ? 0 : visual.sortingOffset);
        CharacterModelRendererSorting.Apply(modelRenderers, sortingLayerName, currentSortingOrder);
        if (shadowRenderer != null)
        {
            shadowRenderer.sortingLayerName = sortingLayerName;
            shadowRenderer.sortingOrder = currentSortingOrder - 2;
        }

        if (selectionRenderer != null)
        {
            selectionRenderer.sortingLayerName = sortingLayerName;
            selectionRenderer.sortingOrder = currentSortingOrder - 1;
        }
    }

    private void UpdateSelectionPulse()
    {
        if (selectionRenderer == null || !selectionRenderer.enabled)
        {
            return;
        }

        Color color = selectionRenderer.color;
        color.a = 0.34f + Mathf.Abs(Mathf.Sin(Time.time * 3.4f)) * 0.22f;
        selectionRenderer.color = color;
    }

    private void ApplyTint()
    {
        if (modelRenderers == null)
        {
            return;
        }

        if (propertyBlock == null)
        {
            propertyBlock = new MaterialPropertyBlock();
        }

        Color tint = defeated ? new Color(0.55f, 0.55f, 0.55f, 0.72f) :
                     acted ? new Color(0.74f, 0.78f, 0.84f, 0.92f) : Color.white;
        for (int i = 0; i < modelRenderers.Length; i++)
        {
            Renderer renderer = modelRenderers[i];
            if (renderer == null)
            {
                continue;
            }

            renderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor("_Color", tint);
            propertyBlock.SetColor("_BaseColor", tint);
            renderer.SetPropertyBlock(propertyBlock);
        }
    }

    private void SetAnimatorBool(int hash, bool value)
    {
        if (animator != null && HasParameter(hash, AnimatorControllerParameterType.Bool))
        {
            animator.SetBool(hash, value);
        }
    }

    private void SetAnimatorFloat(int hash, float value)
    {
        if (animator != null && HasParameter(hash, AnimatorControllerParameterType.Float))
        {
            animator.SetFloat(hash, value);
        }
    }

    private void SetAnimatorTrigger(int hash)
    {
        if (animator != null && HasParameter(hash, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(hash);
        }
    }

    private bool HasParameter(int hash, AnimatorControllerParameterType type)
    {
        if (animator == null)
        {
            return false;
        }

        AnimatorControllerParameter[] parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].nameHash == hash && parameters[i].type == type)
            {
                return true;
            }
        }

        return false;
    }

    private static Sprite CreateOvalSprite()
    {
        const int width = 96;
        const int height = 40;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "RuntimeModelOval";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color fill = new Color(1f, 1f, 1f, 0.78f);
        float rx = width * 0.46f;
        float ry = height * 0.42f;
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float dx = (x - center.x) / rx;
                float dy = (y - center.y) / ry;
                texture.SetPixel(x, y, dx * dx + dy * dy <= 1f ? fill : clear);
            }
        }

        texture.Apply(false, true);
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), width);
    }
}
}
