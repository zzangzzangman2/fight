using System.Collections;
using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleImpactPresenter : MonoBehaviour
{
    [SerializeField] private BattleCameraFx cameraFx;
    [SerializeField] private DamagePopupPresenter damagePopups;
    [SerializeField] private Color hitFlashColor = new Color(1f, 0.42f, 0.22f, 1f);
    [SerializeField] private Color counterFlashColor = new Color(0.82f, 0.94f, 1f, 1f);

    private BattleCameraFx CameraFx => cameraFx != null ? cameraFx : GetComponent<BattleCameraFx>();
    private DamagePopupPresenter DamagePopups => damagePopups != null ? damagePopups : GetComponent<DamagePopupPresenter>();

    public Coroutine PlayMoveStepAsync(BattleTestUnit unit)
    {
        return StartCoroutine(MoveStepRoutine(unit));
    }

    public Coroutine PlayAttackStartAsync(BattleTestUnit attacker, BattleTestUnit target)
    {
        return StartCoroutine(AttackStartRoutine(attacker, target));
    }

    public Coroutine PlayHitAsync(BattleTestUnit target, int damage, bool isCritical)
    {
        return StartCoroutine(HitRoutine(target, damage, isCritical));
    }

    public Coroutine PlayMissAsync(BattleTestUnit target)
    {
        return StartCoroutine(MissRoutine(target));
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

    private IEnumerator AttackStartRoutine(BattleTestUnit attacker, BattleTestUnit target)
    {
        if (attacker != null && target != null && attacker.view != null && target.view != null)
        {
            Vector3 midpoint = (attacker.view.transform.position + target.view.transform.position) * 0.5f;
            CameraFx?.FocusWorldPosition(midpoint);
        }

        yield return new WaitForSeconds(0.06f);
    }

    private IEnumerator HitRoutine(BattleTestUnit target, int damage, bool isCritical)
    {
        if (target != null && target.view != null)
        {
            SimpleSpriteFlash flash = target.view.GetComponent<SimpleSpriteFlash>() ??
                                      target.view.gameObject.AddComponent<SimpleSpriteFlash>();
            flash.Flash(hitFlashColor, isCritical ? 0.11f : 0.08f);
            DamagePopups?.ShowDamage(target.view.transform.position, damage, isCritical);
            CameraFx?.Shake(isCritical ? 0.12f : 0.07f, isCritical ? 0.16f : 0.10f);
            CameraFx?.PulseZoom(isCritical ? -0.16f : -0.08f, 0.14f);
        }

        yield return new WaitForSeconds(isCritical ? 0.08f : 0.05f);
    }

    private IEnumerator MissRoutine(BattleTestUnit target)
    {
        if (target != null && target.view != null)
        {
            DamagePopups?.ShowMiss(target.view.transform.position);
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
}
}
