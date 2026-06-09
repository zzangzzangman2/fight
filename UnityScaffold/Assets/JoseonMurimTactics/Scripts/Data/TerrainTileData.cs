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
        public int elevation;
        public CoverType coverType;
        public HazardType hazardType;
    }
}
