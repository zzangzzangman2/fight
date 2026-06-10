using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Character Visual Data")]
public sealed class CharacterVisualData : ScriptableObject
{
    public string visualId;
    public Sprite fullBodySprite;
    public Sprite bustSprite;
    public Sprite portraitSprite;
    public Sprite faceIconSprite;
    public RuntimeAnimatorController animatorController;

    [Header("Battle Pose Sprites")]
    public Sprite idlePoseSprite;
    public Sprite movePoseSprite;
    public Sprite attackPoseSprite;
    public Sprite skillPoseSprite;
    public Sprite hitPoseSprite;
    public Sprite defeatedPoseSprite;
    public Sprite actedPoseSprite;

    [Header("Outfits")]
    public CharacterOutfitData defaultOutfit;
    public CharacterOutfitData[] outfitOptions;

    public WeaponType defaultWeaponType = WeaponType.Sword;
    public WeaponAnimationSet weaponAnimationSet;

    [Header("Board Fit")]
    public float heightInTiles = 1.18f;
    public Vector2 spriteOffset = new Vector2(0f, 0.12f);
    public int sortingOffset;

    [Header("Presence")]
    public float idleAmplitude = 0.035f;
    public float idleSpeed = 1f;
    public float breathingScale = 0.015f;
    public float shadowWidth = 0.72f;
    public float shadowHeight = 0.18f;

    [Header("State Colors")]
    public Color normalTint = Color.white;
    public Color selectedTint = new Color(1f, 0.92f, 0.62f, 1f);
    public Color actedTint = new Color(0.72f, 0.78f, 0.86f, 0.82f);
    public Color hitTint = new Color(1f, 0.62f, 0.58f, 1f);
    public Color guardTint = new Color(0.72f, 0.96f, 1f, 1f);
    public Color defeatedTint = new Color(0.55f, 0.55f, 0.55f, 0.68f);

    [Header("State Motion")]
    public float moveSecondsPerTile = 0.24f;
    public float moveSettleTime = 0.10f;
    public float moveLeanDegrees = 5f;
    public float attackLunge = 0.13f;
    public float skillPulseScale = 0.09f;
    public float hitRecoil = 0.10f;

    public float WalkSecondsPerTile => weaponAnimationSet == null ? moveSecondsPerTile : weaponAnimationSet.walkSecondsPerTile;
    public float MoveSettleTime => weaponAnimationSet == null ? moveSettleTime : weaponAnimationSet.moveSettleTime;
}
}
