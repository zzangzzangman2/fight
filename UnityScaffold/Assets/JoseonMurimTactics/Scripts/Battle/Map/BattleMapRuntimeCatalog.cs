using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class BattleMapRuntimeCell
{
    public Vector2Int cell;
    public TerrainType terrainType = TerrainType.Snow;
    public int elevation;
    public int moveCost = 1;
    public bool walkable = true;
    public bool blocksLineOfSight;
    public bool blocksProjectiles;
    public bool occupyAllowed = true;
    public bool isChokePoint;
    public bool objective;
    public bool danger;
    public int coverBonus;
    public HazardType hazardType = HazardType.None;
    public int deployZone;
    public string zoneId = string.Empty;
    public string laneId = string.Empty;
    public string tacticalNote = string.Empty;
    public readonly List<string> tags = new List<string>();

    public bool HasTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return false;
        }

        for (int i = 0; i < tags.Count; i++)
        {
            if (string.Equals(tags[i], tag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}

public static class BattleMapRuntimeCatalog
{
    public const string BaekduSnowGateMapId = "baekdu_snow_gate";

    public static bool TryGetCell(BattleTestMapVariant variant, Vector2Int cell, out BattleMapRuntimeCell data)
    {
        if (variant == BattleTestMapVariant.BaekduSnowGate)
        {
            return BaekduSnowGateBattleMapData.TryGetCell(cell, out data);
        }

        data = null;
        return false;
    }

    public static IEnumerable<BattleMapRuntimeCell> Cells(BattleTestMapVariant variant)
    {
        if (variant == BattleTestMapVariant.BaekduSnowGate)
        {
            return BaekduSnowGateBattleMapData.Cells();
        }

        return Array.Empty<BattleMapRuntimeCell>();
    }
}
}
