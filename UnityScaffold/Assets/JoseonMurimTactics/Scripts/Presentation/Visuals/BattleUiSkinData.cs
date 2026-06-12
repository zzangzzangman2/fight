using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(fileName = "BattleUiSkinData", menuName = "Joseon Murim Tactics/Visual/Battle UI Skin Data")]
public sealed class BattleUiSkinData : ScriptableObject
{
    [Header("Panels")]
    public Sprite panelDark;
    public Sprite panelLight;
    public Sprite commandButtonNormal;
    public Sprite commandButtonHover;
    public Sprite forecastPanelFrame;

    [Header("Battle Flow")]
    public Sprite phaseBannerPlayer;
    public Sprite phaseBannerEnemy;
    public Sprite hpBarFrame;
    public Sprite innerBarFrame;

    [Header("Icons")]
    public Sprite moraleIcon;
    public Sprite breakIcon;
    public Sprite counterIcon;

    [Header("Palette")]
    public Color allyAccent = new Color(0.35f, 0.72f, 1f, 1f);
    public Color enemyAccent = new Color(0.90f, 0.28f, 0.22f, 1f);
    public Color dangerAccent = new Color(0.95f, 0.32f, 0.16f, 1f);
}
}
