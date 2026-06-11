using UnityEngine;

namespace JoseonMurimTactics
{
public enum CharacterBattleVisualMode
{
    Sprite2D,
    Model3D
}

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

    [Header("Battle Pose Frames (2장 이상이면 프레임 애니메이션, 비우면 단일 포즈)")]
    public Sprite[] idleFrames;
    public Sprite[] moveFrames;
    public Sprite[] attackFrames;
    public Sprite[] skillFrames;
    public Sprite[] hitFrames;
    public float idleFrameRate = 4f;

    [Header("Outfits")]
    public CharacterOutfitData defaultOutfit;
    public CharacterOutfitData[] outfitOptions;

    public WeaponType defaultWeaponType = WeaponType.Sword;
    public WeaponAnimationSet weaponAnimationSet;

    [Header("Board Fit")]
    public float heightInTiles = 1.18f;
    public Vector2 spriteOffset = new Vector2(0f, 0.12f);
    public int sortingOffset;

    [Header("3D Battle Model")]
    public CharacterBattleVisualMode battleVisualMode = CharacterBattleVisualMode.Sprite2D;
    public GameObject modelPrefab;
    public RuntimeAnimatorController modelAnimatorController;
    public Vector3 modelLocalOffset = Vector3.zero;
    public Vector3 modelLocalEuler = new Vector3(0f, 180f, 0f);
    public float modelScale = 1f;
    public float modelGroundY;
    public float modelShadowWidth = 0.72f;
    public float modelShadowHeight = 0.18f;
    public bool modelFaceByYaw = true;
    public bool modelKeepFeetOnGround = true;

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
