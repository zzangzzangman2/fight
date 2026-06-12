using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(fileName = "BattleVisualProfile", menuName = "Joseon Murim Tactics/Visual/Battle Visual Profile")]
public sealed class BattleVisualProfile : ScriptableObject
{
    [Header("Identity")]
    public string id = "baekdusan_gate_v1";

    [Header("Terrain Sprites")]
    public Sprite[] groundTiles;
    public Sprite[] roadTiles;
    public Sprite[] waterTiles;
    public Sprite[] cliffTiles;
    public Sprite[] decorTiles;
    public Sprite[] propSprites;

    [Header("Highlights")]
    public Sprite moveHighlightSprite;
    public Sprite attackHighlightSprite;
    public Sprite dangerHighlightSprite;

    [Header("Scene Grade")]
    public Color globalTint = new Color(0.78f, 0.82f, 0.90f, 1f);
    public bool useUrp2DLighting = false;
    public bool usePixelPerfect = false;
}
}
