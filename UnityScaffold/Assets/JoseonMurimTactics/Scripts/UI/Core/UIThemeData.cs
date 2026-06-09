using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>ScriptableObject theme target for replacing runtime IMGUI texture generation.</summary>
[CreateAssetMenu(menuName = "Joseon Murim/UI Theme Data")]
public sealed class UIThemeData : ScriptableObject
{
    public Color hanji = new Color(0.91f, 0.84f, 0.70f, 1f);
    public Color hanjiAlt = new Color(0.83f, 0.74f, 0.58f, 1f);
    public Color ink = new Color(0.12f, 0.10f, 0.08f, 1f);
    public Color navy = new Color(0.12f, 0.18f, 0.27f, 1f);
    public Color teal = new Color(0.12f, 0.43f, 0.45f, 1f);
    public Color sealRed = new Color(0.70f, 0.11f, 0.08f, 1f);
    public Color gold = new Color(0.78f, 0.58f, 0.25f, 1f);

    public Sprite panelSprite;
    public Sprite buttonSprite;
    public Sprite primaryButtonSprite;
    public Sprite sealSprite;
    public Sprite dividerSprite;
    public Font fallbackKoreanFont;
}
}
