using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Character Outfit Data")]
public sealed class CharacterOutfitData : ScriptableObject
{
    public string outfitId;
    public string displayName;

    [Header("Full Outfit Sprites")]
    public Sprite fullBodySprite;
    public Sprite bustSprite;
    public Sprite portraitSprite;
    public Sprite faceIconSprite;

    [Header("Battle Pose Sprites")]
    public Sprite idlePoseSprite;
    public Sprite movePoseSprite;
    public Sprite attackPoseSprite;
    public Sprite skillPoseSprite;
    public Sprite hitPoseSprite;
    public Sprite defeatedPoseSprite;
    public Sprite actedPoseSprite;

    [Header("Battle Pose Frames (의상별 오버라이드, 비우면 캐릭터 기본 프레임)")]
    public Sprite[] idleFrames;
    public Sprite[] moveFrames;
    public Sprite[] attackFrames;
    public Sprite[] skillFrames;
    public Sprite[] hitFrames;

    [Header("Future Layered Swap Slots")]
    public bool useLayeredSprites;
    public Sprite baseBodyLayer;
    public Sprite outfitLayer;
    public Sprite hairLayer;
    public Sprite faceLayer;
    public Sprite weaponLayer;
    public Sprite accessoryLayer;
}
}
