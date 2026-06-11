using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// Runtime-only anchor correction for CharacterVisualController.
    ///
    /// CharacterVisualController owns animation/pose and rewrites the body transform in LateUpdate.
    /// This component runs after it, then gently pulls the full-body renderer back toward the ground
    /// shadow so units no longer look like they are floating over the battlefield.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(9400)]
    public sealed class BattleUnitGroundingStabilizer : MonoBehaviour
    {
        [Header("Ground Anchor")]
        [SerializeField] private float targetFootLocalY = 0.018f;
        [SerializeField] private float additionalDownBias = 0.035f;
        [SerializeField] private float maxDownCorrection = 0.22f;
        [SerializeField] private float maxUpCorrection = 0.04f;
        [SerializeField] private float smoothing = 1f;

        [Header("Shadow Contact")]
        [SerializeField] private bool reinforceShadow = true;
        [SerializeField] private float shadowY = 0.0f;
        [SerializeField] private float shadowScaleBoost = 1.08f;
        [SerializeField] private float shadowAlpha = 0.40f;

        private CharacterVisualController visual;
        private SpriteRenderer bodyRenderer;
        private SpriteRenderer shadowRenderer;
        private Vector3 lastAppliedOffset;
        private Sprite lastSprite;

        private void Awake()
        {
            CacheVisual();
        }

        private void OnEnable()
        {
            CacheVisual();
            lastAppliedOffset = Vector3.zero;
            lastSprite = null;
        }

        private void LateUpdate()
        {
            CacheVisual();
            if (bodyRenderer == null || bodyRenderer.sprite == null)
            {
                return;
            }

            ApplyGrounding();
            ApplyShadowContact();
        }

        private void CacheVisual()
        {
            if (visual == null)
            {
                visual = GetComponent<CharacterVisualController>();
            }

            if (visual == null)
            {
                return;
            }

            bodyRenderer = visual.bodyRenderer != null ? visual.bodyRenderer : bodyRenderer;
            shadowRenderer = visual.shadowRenderer != null ? visual.shadowRenderer : shadowRenderer;
        }

        private void ApplyGrounding()
        {
            Transform body = bodyRenderer.transform;
            Sprite sprite = bodyRenderer.sprite;
            if (sprite != lastSprite)
            {
                lastAppliedOffset = Vector3.zero;
                lastSprite = sprite;
            }

            Vector3 localPosition = body.localPosition;
            float scaledSpriteBottom = sprite.bounds.min.y * body.localScale.y;
            float currentFootY = localPosition.y + scaledSpriteBottom;
            float desiredCorrectionY = (targetFootLocalY - currentFootY) - additionalDownBias;
            desiredCorrectionY = Mathf.Clamp(desiredCorrectionY, -Mathf.Abs(maxDownCorrection), Mathf.Abs(maxUpCorrection));

            Vector3 desiredOffset = new Vector3(0f, desiredCorrectionY, 0f);
            if (smoothing > 0f && smoothing < 1f)
            {
                desiredOffset = Vector3.Lerp(lastAppliedOffset, desiredOffset, smoothing);
            }

            body.localPosition = localPosition + desiredOffset;
            lastAppliedOffset = desiredOffset;
        }

        private void ApplyShadowContact()
        {
            if (!reinforceShadow || shadowRenderer == null)
            {
                return;
            }

            Transform shadow = shadowRenderer.transform;
            Vector3 localPosition = shadow.localPosition;
            localPosition.y = shadowY;
            shadow.localPosition = localPosition;

            if (visual != null && visual.visual != null)
            {
                float width = Mathf.Max(0.1f, visual.visual.shadowWidth * shadowScaleBoost);
                float height = Mathf.Max(0.04f, visual.visual.shadowHeight * shadowScaleBoost);
                shadow.localScale = new Vector3(width, height, 1f);
            }

            Color color = shadowRenderer.color;
            color.a = Mathf.Max(color.a, shadowAlpha);
            shadowRenderer.color = color;
        }
    }
}
