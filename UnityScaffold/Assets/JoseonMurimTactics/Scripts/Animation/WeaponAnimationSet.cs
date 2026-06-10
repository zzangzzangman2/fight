using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Combat/Weapon Animation Set")]
public sealed class WeaponAnimationSet : ScriptableObject
{
    public WeaponType weaponType = WeaponType.Sword;

    [Header("Animation Clips")]
    public AnimationClip idleClip;
    public AnimationClip selectedIdleClip;
    public AnimationClip walkClip;
    public AnimationClip attackClip;
    public AnimationClip attackAltClip;
    public AnimationClip skillClip;
    public AnimationClip guardClip;
    public AnimationClip evadeClip;
    public AnimationClip hitClip;
    public AnimationClip defeatClip;
    public AnimationClip victoryClip;
    public AnimationClip actedClip;

    [Header("Move Timing")]
    public float walkSecondsPerTile = 0.24f;
    public float moveSettleTime = 0.10f;

    [Header("Attack Timing")]
    public float attackDuration = 0.72f;
    public float skillDuration = 1.12f;
    public float attackHitTime = 0.40f;
    public float skillHitTime = 0.58f;
    public float attackVfxTime = 0.30f;
    public float skillVfxTime = 0.44f;
    public float recoveryTime = 0.18f;

    [Header("Visual Impact")]
    public GameObject attackVfxPrefab;
    public GameObject skillVfxPrefab;
    public GameObject projectilePrefab;
    public GameObject weaponTrailPrefab;
    public GameObject impactVfxPrefab;
    public GameObject guardVfxPrefab;
    public GameObject footstepVfxPrefab;
    public float attackMoveForwardDistance = 0.13f;
    public float skillMoveForwardDistance = 0.20f;
    public float cameraShakeStrength = 0.055f;
    public float cameraShakeDuration = 0.11f;

    public float Duration(bool special)
    {
        return Mathf.Max(0.05f, special ? skillDuration : attackDuration);
    }

    public float HitTime(bool special)
    {
        return Mathf.Clamp(special ? skillHitTime : attackHitTime, 0.01f, Duration(special));
    }

    public float VfxTime(bool special)
    {
        return Mathf.Clamp(special ? skillVfxTime : attackVfxTime, 0.01f, Duration(special));
    }
}
}