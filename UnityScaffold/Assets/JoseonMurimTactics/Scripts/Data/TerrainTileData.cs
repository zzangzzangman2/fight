using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Terrain Tile Data")]
public sealed class TerrainTileData : Tile
{
    public TerrainType terrainType = TerrainType.Stone;
    public int moveCost = 1;
    public bool walkable = true;
    public bool blocksMovement;
    public bool blocksLineOfSight;
    public bool isChokePoint;
    public int capacity = 1;
    public int elevation;
    public CoverType coverType;
    public HazardType hazardType;
    public EdgeType northEdge;
    public EdgeType eastEdge;
    public EdgeType southEdge;
    public EdgeType westEdge;
    public string zoneId;
    public string laneId;
}
}
