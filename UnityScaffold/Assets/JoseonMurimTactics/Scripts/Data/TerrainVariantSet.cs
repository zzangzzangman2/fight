using System;
using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(menuName = "Joseon Murim Tactics/Terrain Variant Set")]
public sealed class TerrainVariantSet : ScriptableObject
{
    public string setId;
    public WeightedTerrainTile[] variants = Array.Empty<WeightedTerrainTile>();

    public TerrainTileData Pick(Vector2Int cell, int salt = 0)
    {
        if (variants == null || variants.Length == 0)
        {
            return null;
        }

        int totalWeight = 0;
        foreach (WeightedTerrainTile variant in variants)
        {
            if (variant != null && variant.tile != null)
            {
                totalWeight += Mathf.Max(1, variant.weight);
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = StableHash(cell, salt) % totalWeight;
        foreach (WeightedTerrainTile variant in variants)
        {
            if (variant == null || variant.tile == null)
            {
                continue;
            }

            roll -= Mathf.Max(1, variant.weight);
            if (roll < 0)
            {
                return variant.tile;
            }
        }

        return variants[0].tile;
    }

    private static int StableHash(Vector2Int cell, int salt)
    {
        unchecked
        {
            int hash = cell.x * 73856093 ^ cell.y * 19349663 ^ salt * 83492791;
            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;
            return hash & 0x7fffffff;
        }
    }
}

[Serializable]
public sealed class WeightedTerrainTile
{
    public TerrainTileData tile;
    public int weight = 1;
}
}
