using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    public static class BaekduSnowGateBattleMapData
    {
        public const int Width = 16;
        public const int Height = 12;

        public static IEnumerable<BattleMapRuntimeCell> Cells()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (TryGetCell(new Vector2Int(x, y), out BattleMapRuntimeCell cell))
                    {
                        yield return cell;
                    }
                }
            }
        }

        public static bool TryGetCell(Vector2Int cell, out BattleMapRuntimeCell data)
        {
            data = null;
            if (cell.x < 0 || cell.x >= Width || cell.y < 0 || cell.y >= Height)
            {
                return false;
            }

            data = Create(cell.x, cell.y);
            return true;
        }

        private static BattleMapRuntimeCell Create(int x, int y)
        {
            if (IsOuterEdge(x, y))
            {
                return Blocked(
                    x,
                    y,
                    TerrainType.Cliff,
                    3,
                    "painted_cliff_edge",
                    "outer_edge",
                    "cliff",
                    "map_edge"
                );
            }

            if (IsDeploymentCell(x, y))
            {
                return Walkable(
                    x,
                    y,
                    TerrainType.Road,
                    0,
                    1,
                    0,
                    "gate_ascent_deployment",
                    "south_approach",
                    "Player deployment lane: packed snow and visible stone road.",
                    1,
                    "road",
                    "deploy",
                    "visible_path"
                );
            }

            if (IsLowerRoad(x, y))
            {
                bool brazierLight = (x == 5 || x == 6) && y == 2;
                return Walkable(
                    x,
                    y,
                    brazierLight ? TerrainType.Fire : TerrainType.Road,
                    0,
                    brazierLight ? 2 : 1,
                    brazierLight ? 1 : 0,
                    "south_approach",
                    "south_approach",
                    brazierLight
                        ? "Brazier-lit road: passable but slow and dangerous around the prop."
                        : "Lower visible stone road: clear approach toward the snow gate.",
                    0,
                    "road",
                    "visible_path"
                );
            }

            if (IsSnowShoulder(x, y))
            {
                return Walkable(
                    x,
                    y,
                    TerrainType.Snow,
                    0,
                    2,
                    0,
                    "south_snow_shoulder",
                    "south_approach",
                    "Snow shoulder beside the visible road: passable, slower than stone.",
                    0,
                    "snow",
                    "visible_path"
                );
            }

            if (IsStairRamp(x, y))
            {
                int elevation =
                    y <= 4 ? 1
                    : y <= 6 ? 2
                    : 3;
                int moveCost = y >= 7 ? 2 : 1;
                return Walkable(
                    x,
                    y,
                    TerrainType.Stone,
                    elevation,
                    moveCost,
                    0,
                    "gate_stair_ramp",
                    "central_stairs",
                    "Continuous snow-covered stone stairs: legal ramp connection for movement and melee.",
                    0,
                    "stairs",
                    "ramp",
                    "visible_path",
                    "choke"
                );
            }

            if (IsGateLanding(x, y))
            {
                return Walkable(
                    x,
                    y,
                    TerrainType.Gate,
                    3,
                    1,
                    1,
                    "gate_landing",
                    "central_stairs",
                    "Upper gate landing: narrow high ground connected by stairs.",
                    0,
                    "gate",
                    "stairs",
                    "ramp",
                    "visible_path",
                    "cover"
                );
            }

            if (IsRightSnowPath(x, y))
            {
                int elevation =
                    y <= 4 ? 0
                    : y <= 6 ? 1
                    : 2;
                return Walkable(
                    x,
                    y,
                    TerrainType.Snow,
                    elevation,
                    2,
                    0,
                    "right_snow_path",
                    "right_flank",
                    "Right visible snow path beside the palisade.",
                    0,
                    "snow",
                    "visible_path"
                );
            }

            if (IsLeftIceWater(x, y))
            {
                BattleMapRuntimeCell ice = Blocked(
                    x,
                    y,
                    TerrainType.DeepWater,
                    0,
                    "painted_left_ice_water",
                    "left_ice",
                    "ice",
                    "water",
                    "cliff"
                );
                ice.blocksLineOfSight = false;
                ice.blocksProjectiles = false;
                ice.hazardType = HazardType.DeepWater;
                ice.danger = true;
                return ice;
            }

            if (IsRightForestMass(x, y))
            {
                return Blocked(
                    x,
                    y,
                    TerrainType.Forest,
                    1,
                    "painted_right_forest",
                    "right_forest",
                    "forest",
                    "decorativeBlocker"
                );
            }

            if (IsGateWallOrPalisade(x, y))
            {
                return Blocked(
                    x,
                    y,
                    TerrainType.Wall,
                    y >= 6 ? 3 : 2,
                    "painted_gate_wall",
                    "gate_wall",
                    "wall",
                    "palisade",
                    "decorativeBlocker"
                );
            }

            if (IsRubbleBlocker(x, y))
            {
                BattleMapRuntimeCell rubble = Blocked(
                    x,
                    y,
                    TerrainType.Rubble,
                    1,
                    "painted_static_obstacle",
                    "rubble",
                    "rubble",
                    "decorativeBlocker"
                );
                rubble.coverBonus = 2;
                return rubble;
            }

            return Blocked(
                x,
                y,
                TerrainType.Wall,
                2,
                "painted_backdrop_obstacle",
                "backdrop",
                "wall",
                "decorativeBlocker"
            );
        }

        public static bool IsDeploymentCell(int x, int y)
        {
            return (y == 0 && x >= 4 && x <= 7) || (y == 1 && (x == 4 || x == 5));
        }

        public static bool IsWalkableCell(int x, int y)
        {
            return IsDeploymentCell(x, y)
                || IsLowerRoad(x, y)
                || IsSnowShoulder(x, y)
                || IsStairRamp(x, y)
                || IsGateLanding(x, y)
                || IsRightSnowPath(x, y);
        }

        private static bool IsOuterEdge(int x, int y)
        {
            return x == 0 || x == 15 || y == 11;
        }

        private static bool IsLowerRoad(int x, int y)
        {
            switch (y)
            {
                case 0:
                    return x >= 4 && x <= 7;
                case 1:
                    return x >= 4 && x <= 8;
                case 2:
                    return x >= 5 && x <= 9;
                case 3:
                    return x >= 4 && x <= 10;
                default:
                    return false;
            }
        }

        private static bool IsSnowShoulder(int x, int y)
        {
            return (y == 2 && x == 4) || (y == 3 && (x == 3 || x == 11));
        }

        private static bool IsStairRamp(int x, int y)
        {
            switch (y)
            {
                case 4:
                    return x >= 6 && x <= 10;
                case 5:
                    return x >= 6 && x <= 10;
                case 6:
                    return x >= 7 && x <= 10;
                case 7:
                    return x >= 7 && x <= 11;
                default:
                    return false;
            }
        }

        private static bool IsGateLanding(int x, int y)
        {
            return (y == 8 && x >= 8 && x <= 10) || (y == 9 && x >= 9 && x <= 11);
        }

        private static bool IsRightSnowPath(int x, int y)
        {
            return (x == 12 && y >= 3 && y <= 7) || (x == 13 && y >= 4 && y <= 6);
        }

        private static bool IsLeftIceWater(int x, int y)
        {
            return x <= 3 && y >= 4 && y <= 10;
        }

        private static bool IsRightForestMass(int x, int y)
        {
            return x >= 13 && y >= 7 || x >= 14 && y >= 3 || (x == 13 && y <= 3);
        }

        private static bool IsGateWallOrPalisade(int x, int y)
        {
            return (y >= 5 && x >= 4 && x <= 5)
                || (y >= 8 && x >= 4 && x <= 7)
                || (y >= 4 && x >= 11 && x <= 12)
                || (y >= 10 && x >= 8 && x <= 12);
        }

        private static bool IsRubbleBlocker(int x, int y)
        {
            return (x == 5 && y == 4)
                || (x == 6 && y == 8)
                || (x == 12 && y == 8)
                || (x == 3 && y == 3)
                || (x == 11 && y == 2);
        }

        private static BattleMapRuntimeCell Walkable(
            int x,
            int y,
            TerrainType terrain,
            int elevation,
            int moveCost,
            int coverBonus,
            string zoneId,
            string laneId,
            string note,
            int deployZone,
            params string[] tags
        )
        {
            BattleMapRuntimeCell cell = BaseCell(
                x,
                y,
                terrain,
                elevation,
                zoneId,
                laneId,
                note,
                tags
            );
            cell.moveCost = Mathf.Max(1, moveCost);
            cell.coverBonus = Mathf.Max(0, coverBonus);
            cell.walkable = true;
            cell.occupyAllowed = true;
            cell.deployZone = deployZone;
            cell.isChokePoint =
                cell.HasTag("choke")
                || terrain == TerrainType.Gate
                || terrain == TerrainType.Bridge;
            cell.danger = terrain == TerrainType.Fire || cell.HasTag("ice");
            cell.hazardType =
                terrain == TerrainType.Fire ? HazardType.Fire
                : terrain == TerrainType.Ice ? HazardType.Ice
                : HazardType.None;
            return cell;
        }

        private static BattleMapRuntimeCell Blocked(
            int x,
            int y,
            TerrainType terrain,
            int elevation,
            string zoneId,
            string laneId,
            params string[] tags
        )
        {
            BattleMapRuntimeCell cell = BaseCell(
                x,
                y,
                terrain,
                elevation,
                zoneId,
                laneId,
                "Blocked visual mass: impassable tactical cell.",
                tags
            );
            cell.moveCost = 99;
            cell.walkable = false;
            cell.occupyAllowed = false;
            cell.blocksLineOfSight = terrain != TerrainType.DeepWater;
            cell.blocksProjectiles =
                cell.blocksLineOfSight
                || terrain == TerrainType.Wall
                || terrain == TerrainType.Cliff;
            cell.danger = terrain == TerrainType.Cliff || terrain == TerrainType.DeepWater;
            cell.hazardType =
                terrain == TerrainType.Cliff ? HazardType.Fall
                : terrain == TerrainType.DeepWater ? HazardType.DeepWater
                : HazardType.None;
            return cell;
        }

        private static BattleMapRuntimeCell BaseCell(
            int x,
            int y,
            TerrainType terrain,
            int elevation,
            string zoneId,
            string laneId,
            string note,
            params string[] tags
        )
        {
            BattleMapRuntimeCell cell = new BattleMapRuntimeCell
            {
                cell = new Vector2Int(x, y),
                terrainType = terrain,
                elevation = elevation,
                zoneId = zoneId,
                laneId = laneId,
                tacticalNote = note,
            };

            if (tags != null)
            {
                for (int i = 0; i < tags.Length; i++)
                {
                    if (!string.IsNullOrEmpty(tags[i]))
                    {
                        cell.tags.Add(tags[i]);
                    }
                }
            }

            return cell;
        }
    }
}
