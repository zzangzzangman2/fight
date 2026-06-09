using UnityEngine;

namespace JoseonMurimTactics
{
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
}
