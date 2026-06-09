using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class MapPropView : MonoBehaviour
{
    public string propId;
    public string displayName;
    public Vector2Int cell;
    public InteractableKind kind;
    public string visualPrefabKey;
    public bool interactive;

    public void Configure(string newPropId, string newDisplayName, Vector2Int newCell, InteractableKind newKind,
                          bool newInteractive)
    {
        propId = newPropId;
        displayName = newDisplayName;
        cell = newCell;
        kind = newKind;
        visualPrefabKey = newPropId;
        interactive = newInteractive;
    }
}

[DisallowMultipleComponent]
public sealed class CoverProvider : MonoBehaviour
{
    public CoverType coverType = CoverType.Light;
    public int coverBonus = 1;
    public int radius = 1;

    public void Configure(CoverType newCoverType, int newCoverBonus, int newRadius = 1)
    {
        coverType = newCoverType;
        coverBonus = Mathf.Max(0, newCoverBonus);
        radius = Mathf.Max(0, newRadius);
    }
}

[DisallowMultipleComponent]
public sealed class LineOfSightBlocker : MonoBehaviour
{
    public bool blocksLineOfSight = true;
    public int height = 1;
    public int radius;

    public void Configure(int newHeight, int newRadius = 0)
    {
        blocksLineOfSight = true;
        height = Mathf.Max(1, newHeight);
        radius = Mathf.Max(0, newRadius);
    }
}

[DisallowMultipleComponent]
public sealed class DestructibleProp : MonoBehaviour
{
    public int maxHp = 12;
    public TerrainType destroyedTerrain = TerrainType.Rubble;
    public HazardType createdHazard = HazardType.None;
    public InteractableEffectType destructionEffect = InteractableEffectType.CreateCover;
    public bool transformTacticalCell = true;

    public void Configure(int hp, TerrainType terrainAfterDestruction, HazardType hazardAfterDestruction,
                          InteractableEffectType effect)
    {
        maxHp = Mathf.Max(1, hp);
        destroyedTerrain = terrainAfterDestruction;
        createdHazard = hazardAfterDestruction;
        destructionEffect = effect;
    }
}

[DisallowMultipleComponent]
public sealed class InteractableProp : MonoBehaviour
{
    public ActionSlot requiredActionSlot = ActionSlot.Main;
    public StatType checkStat = StatType.Strength;
    public int dc = 12;
    public int radius = 1;
    public bool consumedOnUse = true;
    public InteractableEffectType effectType = InteractableEffectType.CreateCover;

    public void Configure(ActionSlot actionSlot, StatType stat, int checkDc, int effectRadius,
                          InteractableEffectType effect, bool consumed)
    {
        requiredActionSlot = actionSlot;
        checkStat = stat;
        dc = Mathf.Max(0, checkDc);
        radius = Mathf.Max(0, effectRadius);
        effectType = effect;
        consumedOnUse = consumed;
    }
}

[DisallowMultipleComponent]
public sealed class MapLightAnchor : MonoBehaviour
{
    public Color color = Color.white;
    public float radius = 1.5f;
    public float intensity = 0.5f;
    public Light2D boundLight;

    public void Configure(Color newColor, float newRadius, float newIntensity, Light2D light = null)
    {
        color = newColor;
        radius = Mathf.Max(0.01f, newRadius);
        intensity = Mathf.Max(0f, newIntensity);
        boundLight = light;
    }
}
}
