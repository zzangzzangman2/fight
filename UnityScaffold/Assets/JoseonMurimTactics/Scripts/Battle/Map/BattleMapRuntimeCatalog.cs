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
        public const string BaekduSnowGateResourcePath = "BattleMaps/baekdu_snow_gate_data";

        private static readonly Dictionary<
            BattleTestMapVariant,
            BattleMapRuntimeSnapshot
        > SnapshotCache = new Dictionary<BattleTestMapVariant, BattleMapRuntimeSnapshot>();

        public static bool TryGetCell(
            BattleTestMapVariant variant,
            Vector2Int cell,
            out BattleMapRuntimeCell data
        )
        {
            if (TryGetSnapshot(variant, out BattleMapRuntimeSnapshot snapshot))
            {
                return snapshot.TryGetCell(cell, out data);
            }

            return TryGetFallbackCell(variant, cell, out data);
        }

        public static IEnumerable<BattleMapRuntimeCell> Cells(BattleTestMapVariant variant)
        {
            if (TryGetSnapshot(variant, out BattleMapRuntimeSnapshot snapshot))
            {
                return snapshot.Cells;
            }

            if (variant == BattleTestMapVariant.BaekduSnowGate)
            {
                return BaekduSnowGateBattleMapData.Cells();
            }

            return Array.Empty<BattleMapRuntimeCell>();
        }

        public static bool TryGetDataAsset(BattleTestMapVariant variant, out BattleMapData mapData)
        {
            if (TryGetSnapshot(variant, out BattleMapRuntimeSnapshot snapshot))
            {
                mapData = snapshot.MapData;
                return mapData != null;
            }

            mapData = null;
            return false;
        }

        public static string SourceName(BattleTestMapVariant variant)
        {
            if (TryGetSnapshot(variant, out BattleMapRuntimeSnapshot snapshot))
            {
                return snapshot.SourceName;
            }

            return variant == BattleTestMapVariant.BaekduSnowGate
                ? "RuntimeCatalogFallback"
                : "GeneratedProfile";
        }

        public static int CellCount(BattleTestMapVariant variant)
        {
            if (TryGetSnapshot(variant, out BattleMapRuntimeSnapshot snapshot))
            {
                return snapshot.Cells.Count;
            }

            int count = 0;
            foreach (BattleMapRuntimeCell ignored in Cells(variant))
            {
                count++;
            }

            return count;
        }

        public static bool ValidateAssetAgainstFallback(
            BattleTestMapVariant variant,
            out string message
        )
        {
            if (
                !TryGetSnapshot(variant, out BattleMapRuntimeSnapshot snapshot)
                || snapshot.MapData == null
            )
            {
                message = "BattleMapData asset was not loaded.";
                return false;
            }

            int compared = 0;
            foreach (BattleMapRuntimeCell assetCell in snapshot.Cells)
            {
                compared++;
                if (
                    !TryGetFallbackCell(
                        variant,
                        assetCell.cell,
                        out BattleMapRuntimeCell fallbackCell
                    )
                )
                {
                    message = $"Fallback runtime data missing cell {assetCell.cell}.";
                    return false;
                }

                if (!RuntimeCellsEqual(assetCell, fallbackCell, out string mismatch))
                {
                    message = $"Cell {assetCell.cell} mismatch: {mismatch}";
                    return false;
                }
            }

            message = $"BattleMapData asset matches fallback cells={compared}.";
            return true;
        }

        private static bool TryGetSnapshot(
            BattleTestMapVariant variant,
            out BattleMapRuntimeSnapshot snapshot
        )
        {
            if (SnapshotCache.TryGetValue(variant, out snapshot))
            {
                return snapshot != null;
            }

            snapshot = LoadSnapshot(variant);
            SnapshotCache[variant] = snapshot;
            return snapshot != null;
        }

        private static BattleMapRuntimeSnapshot LoadSnapshot(BattleTestMapVariant variant)
        {
            if (variant != BattleTestMapVariant.BaekduSnowGate)
            {
                return null;
            }

            BattleMapData mapData = Resources.Load<BattleMapData>(BaekduSnowGateResourcePath);
            if (mapData == null)
            {
                return null;
            }

            return new BattleMapRuntimeSnapshot(mapData, "BattleMapDataAsset");
        }

        private static bool TryGetFallbackCell(
            BattleTestMapVariant variant,
            Vector2Int cell,
            out BattleMapRuntimeCell data
        )
        {
            if (variant == BattleTestMapVariant.BaekduSnowGate)
            {
                return BaekduSnowGateBattleMapData.TryGetCell(cell, out data);
            }

            data = null;
            return false;
        }

        private static bool RuntimeCellsEqual(
            BattleMapRuntimeCell left,
            BattleMapRuntimeCell right,
            out string mismatch
        )
        {
            mismatch = string.Empty;
            if (left.terrainType != right.terrainType)
            {
                mismatch = $"terrainType {left.terrainType} != {right.terrainType}";
                return false;
            }

            if (left.walkable != right.walkable || left.occupyAllowed != right.occupyAllowed)
            {
                mismatch =
                    $"walk/occupy {left.walkable}/{left.occupyAllowed} != {right.walkable}/{right.occupyAllowed}";
                return false;
            }

            if (left.moveCost != right.moveCost || left.elevation != right.elevation)
            {
                mismatch =
                    $"cost/elevation {left.moveCost}/{left.elevation} != {right.moveCost}/{right.elevation}";
                return false;
            }

            if (
                left.blocksLineOfSight != right.blocksLineOfSight
                || left.blocksProjectiles != right.blocksProjectiles
            )
            {
                mismatch =
                    $"los/projectile {left.blocksLineOfSight}/{left.blocksProjectiles} != {right.blocksLineOfSight}/{right.blocksProjectiles}";
                return false;
            }

            if (left.coverBonus != right.coverBonus || left.deployZone != right.deployZone)
            {
                mismatch =
                    $"cover/deploy {left.coverBonus}/{left.deployZone} != {right.coverBonus}/{right.deployZone}";
                return false;
            }

            if (!TagsEqual(left.tags, right.tags))
            {
                mismatch = "tags differ";
                return false;
            }

            return true;
        }

        private static bool TagsEqual(List<string> left, List<string> right)
        {
            if (left.Count != right.Count)
            {
                return false;
            }

            for (int i = 0; i < left.Count; i++)
            {
                if (!string.Equals(left[i], right[i], StringComparison.OrdinalIgnoreCase))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class BattleMapRuntimeSnapshot
    {
        private readonly Dictionary<Vector2Int, BattleMapRuntimeCell> cellsByPosition =
            new Dictionary<Vector2Int, BattleMapRuntimeCell>();

        public BattleMapRuntimeSnapshot(BattleMapData mapData, string sourceName)
        {
            MapData = mapData;
            SourceName = sourceName;

            if (mapData == null || mapData.cells == null)
            {
                return;
            }

            for (int i = 0; i < mapData.cells.Count; i++)
            {
                BattleCellData cellData = mapData.cells[i];
                if (cellData == null)
                {
                    continue;
                }

                cellsByPosition[cellData.cell] = FromBattleCellData(cellData);
            }
        }

        public BattleMapData MapData { get; }
        public string SourceName { get; }
        public IReadOnlyCollection<BattleMapRuntimeCell> Cells => cellsByPosition.Values;

        public bool TryGetCell(Vector2Int cell, out BattleMapRuntimeCell data)
        {
            return cellsByPosition.TryGetValue(cell, out data);
        }

        private static BattleMapRuntimeCell FromBattleCellData(BattleCellData data)
        {
            BattleMapRuntimeCell cell = new BattleMapRuntimeCell
            {
                cell = data.cell,
                terrainType = data.terrainType,
                elevation = data.elevation,
                moveCost = data.blocksMovement ? 99 : Mathf.Max(1, data.moveCost),
                walkable = data.walkable && !data.blocksMovement,
                blocksLineOfSight = data.blocksLineOfSight,
                blocksProjectiles = data.blocksProjectiles,
                occupyAllowed = data.occupyAllowed,
                isChokePoint = data.isChokePoint,
                danger = data.hazardType != HazardType.None,
                coverBonus = data.coverBonus,
                hazardType = data.hazardType,
                deployZone = data.deployZone,
                zoneId = string.IsNullOrEmpty(data.zoneId) ? string.Empty : data.zoneId,
                laneId = string.IsNullOrEmpty(data.laneId) ? string.Empty : data.laneId,
                tacticalNote = string.IsNullOrEmpty(data.decorSetKey)
                    ? data.displayName
                    : data.decorSetKey,
            };

            if (data.tags != null)
            {
                cell.tags.AddRange(data.tags);
            }

            cell.objective = string.Equals(
                cell.zoneId,
                "objective",
                StringComparison.OrdinalIgnoreCase
            );
            return cell;
        }
    }
}
