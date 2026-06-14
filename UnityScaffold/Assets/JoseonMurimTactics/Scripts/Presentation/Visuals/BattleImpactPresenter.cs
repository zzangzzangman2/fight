using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleImpactPresenter : MonoBehaviour
{
    [SerializeField] private BattleCameraFx cameraFx;
    [SerializeField] private DamagePopupPresenter damagePopups;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.42f, 0.22f, 1f);
    [SerializeField] private Color counterFlashColor = new Color(0.82f, 0.94f, 1f, 1f);

    private static Sprite solidUiSprite;
    private Canvas cinematicCanvas;
    private Image cinematicDim;
    private Image speedLineA;
    private Image speedLineB;
    private Image impactFlash;
    private Coroutine attackCutRoutine;
    private Coroutine impactFlashRoutine;

    private BattleCameraFx CameraFx => cameraFx != null ? cameraFx : GetComponent<BattleCameraFx>();

    private DamagePopupPresenter DamagePopups
    {
        get
        {
            if (damagePopups != null)
            {
                return damagePopups;
            }

            damagePopups = GetComponent<DamagePopupPresenter>();
            if (damagePopups == null)
            {
                damagePopups = FindFirstObjectByType<DamagePopupPresenter>();
            }

            if (damagePopups == null)
            {
                damagePopups = gameObject.AddComponent<DamagePopupPresenter>();
            }

            return damagePopups;
        }
    }

    public Coroutine PlayMoveStepAsync(BattleTestUnit unit)
    {
        return StartCoroutine(MoveStepRoutine(unit));
    }

    public Coroutine PlayAttackStartAsync(BattleTestUnit attacker, BattleTestUnit target, bool special = false)
    {
        return StartCoroutine(AttackStartRoutine(attacker, target, special));
    }

    public Coroutine PlayHitAsync(BattleTestUnit target, int damage, bool isCritical, bool showPopup = true)
    {
        return StartCoroutine(HitRoutine(target, damage, isCritical, showPopup));
    }

    public Coroutine PlayMissAsync(BattleTestUnit target, bool showPopup = true)
    {
        return StartCoroutine(MissRoutine(target, showPopup));
    }

    public Coroutine PlayCounterAsync(BattleTestUnit counterUnit)
    {
        return StartCoroutine(CounterRoutine(counterUnit));
    }

    public Coroutine PlayHealAsync(BattleTestUnit target, int amount)
    {
        return StartCoroutine(HealRoutine(target, amount));
    }

    private IEnumerator MoveStepRoutine(BattleTestUnit unit)
    {
        if (unit != null && unit.view != null)
        {
            CameraFx?.FocusWorldPosition(unit.view.transform.position);
        }

        yield return new WaitForSeconds(0.04f);
    }

    private IEnumerator AttackStartRoutine(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        if (attacker != null && target != null && attacker.view != null && target.view != null)
        {
            Vector3 midpoint = (attacker.view.transform.position + target.view.transform.position) * 0.5f;
            CameraFx?.FocusWorldPosition(midpoint);
            CameraFx?.PulseZoom(special ? -0.20f : -0.12f, special ? 0.24f : 0.16f);
            CameraFx?.Shake(special ? 0.04f : 0.025f, special ? 0.10f : 0.07f);

            if (attackCutRoutine != null)
            {
                StopCoroutine(attackCutRoutine);
                HideAttackCut();
            }

            attackCutRoutine = StartCoroutine(AttackCutRoutine(attacker, target, special));
        }

        yield return new WaitForSeconds(special ? 0.10f : 0.06f);
    }

    private IEnumerator HitRoutine(BattleTestUnit target, int damage, bool isCritical, bool showPopup)
    {
        if (target != null && target.view != null)
        {
            SimpleSpriteFlash flash = target.view.GetComponent<SimpleSpriteFlash>() ??
                                      target.view.gameObject.AddComponent<SimpleSpriteFlash>();
            flash.Flash(hitFlashColor, isCritical ? 0.11f : 0.08f);
            PlayImpactFlash(isCritical);
            if (showPopup)
            {
                DamagePopups?.ShowDamage(target.view.transform.position, damage, isCritical);
            }
            CameraFx?.Shake(isCritical ? 0.18f : 0.10f, isCritical ? 0.20f : 0.13f);
            CameraFx?.PulseZoom(isCritical ? -0.22f : -0.12f, isCritical ? 0.18f : 0.14f);
        }

        yield return new WaitForSeconds(isCritical ? 0.08f : 0.05f);
    }

    private IEnumerator MissRoutine(BattleTestUnit target, bool showPopup)
    {
        if (target != null && target.view != null)
        {
            if (showPopup)
            {
                DamagePopups?.ShowMiss(target.view.transform.position);
            }
            CameraFx?.Shake(0.025f, 0.06f);
        }

        yield return new WaitForSeconds(0.04f);
    }

    private IEnumerator CounterRoutine(BattleTestUnit counterUnit)
    {
        if (counterUnit != null && counterUnit.view != null)
        {
            SimpleSpriteFlash flash = counterUnit.view.GetComponent<SimpleSpriteFlash>() ??
                                      counterUnit.view.gameObject.AddComponent<SimpleSpriteFlash>();
            flash.Flash(counterFlashColor, 0.10f);
            DamagePopups?.ShowCounter(counterUnit.view.transform.position);
            CameraFx?.Shake(0.10f, 0.12f);
        }

        yield return new WaitForSeconds(0.06f);
    }

    private IEnumerator HealRoutine(BattleTestUnit target, int amount)
    {
        if (target != null && target.view != null)
        {
            DamagePopups?.ShowHeal(target.view.transform.position, amount);
            CameraFx?.PulseZoom(-0.06f, 0.12f);
        }

        yield return new WaitForSeconds(0.05f);
    }

    private IEnumerator AttackCutRoutine(BattleTestUnit attacker, BattleTestUnit target, bool special)
    {
        EnsureCinematicCanvas();
        if (cinematicDim == null || speedLineA == null || speedLineB == null)
        {
            yield break;
        }

        Vector3 attackerPosition = attacker.view.transform.position;
        Vector3 targetPosition = target.view.transform.position;
        float direction = targetPosition.x >= attackerPosition.x ? 1f : -1f;
        float seconds = special ? 0.56f : 0.40f;
        float dimPeak = special ? 0.62f : 0.48f;
        float lineAlpha = special ? 0.94f : 0.78f;
        float elapsed = 0f;

        cinematicDim.enabled = true;
        speedLineA.enabled = true;
        speedLineB.enabled = true;
        speedLineA.rectTransform.localRotation = Quaternion.Euler(0f, 0f, direction > 0f ? -16f : 16f);
        speedLineB.rectTransform.localRotation = Quaternion.Euler(0f, 0f, direction > 0f ? -16f : 16f);
        speedLineA.rectTransform.sizeDelta = new Vector2(2800f, special ? 58f : 40f);
        speedLineB.rectTransform.sizeDelta = new Vector2(2500f, special ? 42f : 30f);

        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            float pulse = Mathf.Sin(t * Mathf.PI);
            SetImageAlpha(cinematicDim, pulse * dimPeak);
            SetImageAlpha(speedLineA, pulse * lineAlpha);
            SetImageAlpha(speedLineB, Mathf.Sin(Mathf.Clamp01((t - 0.12f) / 0.88f) * Mathf.PI) * lineAlpha * 0.72f);

            float travelA = Mathf.Lerp(-1120f * direction, 1120f * direction, Mathf.SmoothStep(0f, 1f, t));
            float travelB = Mathf.Lerp(-980f * direction, 980f * direction, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t * 1.18f)));
            speedLineA.rectTransform.anchoredPosition = new Vector2(travelA, 150f);
            speedLineB.rectTransform.anchoredPosition = new Vector2(travelB, -135f);
            yield return null;
        }

        HideAttackCut();
        attackCutRoutine = null;
    }

    private void PlayImpactFlash(bool isCritical)
    {
        EnsureCinematicCanvas();
        if (impactFlash == null)
        {
            return;
        }

        if (impactFlashRoutine != null)
        {
            StopCoroutine(impactFlashRoutine);
        }

        impactFlashRoutine = StartCoroutine(ImpactFlashRoutine(isCritical));
    }

    private IEnumerator ImpactFlashRoutine(bool isCritical)
    {
        impactFlash.enabled = true;
        float seconds = isCritical ? 0.26f : 0.18f;
        float peak = isCritical ? 0.62f : 0.44f;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / seconds);
            SetImageAlpha(impactFlash, (1f - t) * (1f - t) * peak);
            yield return null;
        }

        impactFlash.enabled = false;
        SetImageAlpha(impactFlash, 0f);
        impactFlashRoutine = null;
    }

    private void EnsureCinematicCanvas()
    {
        if (cinematicCanvas != null)
        {
            return;
        }

        GameObject canvasObject = new GameObject("BattleAttackCinematicOverlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasObject.transform.SetParent(transform, false);
        cinematicCanvas = canvasObject.GetComponent<Canvas>();
        cinematicCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        cinematicCanvas.sortingOrder = 9000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        cinematicDim = CreateOverlayImage("Dim", new Color(0f, 0f, 0f, 0f), true);
        impactFlash = CreateOverlayImage("ImpactFlash", new Color(1f, 0.92f, 0.72f, 0f), true);
        speedLineA = CreateOverlayImage("SpeedLineA", new Color(1f, 0.88f, 0.36f, 0f), false);
        speedLineB = CreateOverlayImage("SpeedLineB", new Color(1f, 1f, 1f, 0f), false);
        HideAttackCut();
        impactFlash.enabled = false;
    }

    private Image CreateOverlayImage(string name, Color color, bool fillScreen)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(cinematicCanvas.transform, false);
        Image image = imageObject.GetComponent<Image>();
        image.sprite = SolidUiSprite();
        image.color = color;
        image.raycastTarget = false;

        RectTransform rect = image.rectTransform;
        if (fillScreen)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
        else
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(2200f, 28f);
        }

        return image;
    }

    private void HideAttackCut()
    {
        if (cinematicDim != null)
        {
            cinematicDim.enabled = false;
            SetImageAlpha(cinematicDim, 0f);
        }

        if (speedLineA != null)
        {
            speedLineA.enabled = false;
            SetImageAlpha(speedLineA, 0f);
        }

        if (speedLineB != null)
        {
            speedLineB.enabled = false;
            SetImageAlpha(speedLineB, 0f);
        }
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = Mathf.Clamp01(alpha);
        image.color = color;
    }

    private static Sprite SolidUiSprite()
    {
        if (solidUiSprite != null)
        {
            return solidUiSprite;
        }

        Texture2D texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        texture.name = "GeneratedBattleCinematicSolid";
        for (int y = 0; y < texture.height; y++)
        {
            for (int x = 0; x < texture.width; x++)
            {
                texture.SetPixel(x, y, Color.white);
            }
        }

        texture.Apply();
        solidUiSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 4f);
        solidUiSprite.name = "GeneratedBattleCinematicSolid";
        return solidUiSprite;
    }
}
}
