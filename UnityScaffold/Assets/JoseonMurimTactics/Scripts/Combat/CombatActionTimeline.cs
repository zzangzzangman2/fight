using UnityEngine;

namespace JoseonMurimTactics
{
public readonly struct CombatActionTimeline
{
    public readonly WeaponType WeaponType;
    public readonly bool Special;
    public readonly float Duration;
    public readonly float VfxTime;
    public readonly float HitTime;
    public readonly float RecoveryTime;
    public readonly float LungeDistance;
    public readonly float CameraShakeStrength;
    public readonly float CameraShakeDuration;

    public CombatActionTimeline(WeaponAnimationSet animationSet, bool special)
    {
        WeaponType = animationSet == null ? WeaponType.Sword : animationSet.weaponType;
        Special = special;
        Duration = animationSet == null ? (special ? 1.12f : 0.72f) : animationSet.Duration(special);
        VfxTime = animationSet == null ? (special ? 0.44f : 0.30f) : animationSet.VfxTime(special);
        HitTime = animationSet == null ? (special ? 0.58f : 0.40f) : animationSet.HitTime(special);
        RecoveryTime = animationSet == null ? 0.18f : Mathf.Max(0f, animationSet.recoveryTime);
        LungeDistance = animationSet == null ? (special ? 0.20f : 0.13f) :
                        (special ? animationSet.skillMoveForwardDistance : animationSet.attackMoveForwardDistance);
        CameraShakeStrength = animationSet == null ? 0.055f : Mathf.Max(0f, animationSet.cameraShakeStrength);
        CameraShakeDuration = animationSet == null ? 0.11f : Mathf.Max(0f, animationSet.cameraShakeDuration);
    }
}
}