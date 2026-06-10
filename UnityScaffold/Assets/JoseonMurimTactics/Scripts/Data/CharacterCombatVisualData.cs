using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Combat/Character Combat Visual Data")]
public sealed class CharacterCombatVisualData : ScriptableObject
{
    public string characterId;
    public string displayName;
    public GameObject unitSpritePrefab;
    public Sprite bustPortrait;
    public Sprite faceIcon;
    public WeaponType defaultWeaponType = WeaponType.Sword;
    public WeaponAnimationSet weaponAnimationSet;
    public CharacterVisualData boardVisual;
    public GameObject shadowPrefab;
    public GameObject selectionRingPrefab;
    public Color actedTint = new Color(0.72f, 0.78f, 0.86f, 0.82f);
    public Color defeatedTint = new Color(0.55f, 0.55f, 0.55f, 0.68f);
}
}