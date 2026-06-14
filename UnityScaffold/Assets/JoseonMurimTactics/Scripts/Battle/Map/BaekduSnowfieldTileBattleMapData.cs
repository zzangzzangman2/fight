using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    public static class BaekduSnowfieldTileBattleMapData
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
            if (IsAllyDeployment(x, y))
            {
                return Walkable(
                    x,
                    y,
                    TerrainType.Ice,
                    0,
                    1,
                    0,
                    "south_deploy",
                    "south_deploy",
                    "Tile-aligned ally deployment ice plaza.",
                    1,
                    "deploy",
                    "ally_start",
                    "visible_path"
                );
            }

            if (IsEnemySpawn(x, y))
            {
                return Walkable(
                    x,
                    y,
                    TerrainType.Gate,
                    3,
                    1,
                    1,
                    "north_gate",
                    "enemy_gate",
                    "Upper gate platform enemy start tile.",
                    0,
                    BattleMapRuntimeEditStore.EnemySpawnTag,
                    "enemy_start",
                    "high_ground",
                    "visible_path"
                );
            }

            if (IsCentralStair(x, y))
            {
                int elevation = y <= 3 ? 0 : y <= 5 ? 1 : y <= 7 ? 2 : 3;
                return Walkable(
                    x,
                    y,
                    y <= 3 ? TerrainType.Stone : TerrainType.Road,
                    elevation,
                    y >= 6 ? 2 : 1,
                    0,
                    "central_stairs",
                    "central_stairs",
                    "Wide central stone stair route connecting lower plaza to the upper gate.",
                    0,
                    "stairs",
                    "ramp",
                    "visible_path",
                    "central_route"
                );
            }

            if (IsLeftConnector(x, y))
            {
                int elevation = y <= 4 ? 0 : y <= 6 ? 1 : y <= 8 ? 2 : 3;
                TerrainType terrain = y <= 4 ? TerrainType.Bridge : TerrainType.Road;
                return Walkable(
                    x,
                    y,
                    terrain,
                    elevation,
                    2,
                    y == 8 ? 1 : 0,
                    "left_ice_stairs",
                    "left_flank",
                    "Left ice bridge and side stair route; connects the lower field to the high gate.",
                    0,
                    "stairs",
                    "bridge",
                    "ramp",
                    "visible_path",
                    "left_route"
                );
            }

            if (IsRightConnector(x, y))
            {
                int elevation = y <= 3 ? 0 : y <= 5 ? 1 : y <= 7 ? 2 : 3;
                return Walkable(
                    x,
                    y,
                    TerrainType.Snow,
                    elevation,
                    2,
                    y == 5 || y == 8 ? 1 : 0,
                    "right_snow_ramp",
                    "right_flank",
                    "Right snow ramp route; wide enough for a visible alternate approach.",
                    0,
                    "ramp",
                    "snow",
                    "visible_path",
                    "right_route"
                );
            }

            if (IsUpperPlatform(x, y))
            {
                return Walkable(
                    x,
                    y,
                    TerrainType.ShrineFloor,
                    3,
                    1,
                    IsCoverSnowdrift(x, y) ? 1 : 0,
                    "north_platform",
                    "enemy_gate",
                    "Upper snow-covered stone platform with clean tile boundaries.",
                    0,
                    "high_ground",
                    "visible_path"
                );
            }

            if (IsLowerField(x, y))
            {
                return Walkable(
                    x,
                    y,
                    IsCoverSnowdrift(x, y) ? TerrainType.Rubble : TerrainType.Snow,
                    0,
                    IsCoverSnowdrift(x, y) ? 2 : 1,
                    IsCoverSnowdrift(x, y) ? 2 : 0,
                    "lower_field",
                    "south_approach",
                    "Open lower snowfield; cover pieces occupy whole tiles.",
                    0,
                    "snow",
                    "visible_path"
                );
            }

            if (IsLeftIceRavine(x, y))
            {
                BattleMapRuntimeCell ravine = Blocked(
                    x,
                    y,
                    TerrainType.DeepWater,
                    0,
                    "left_ice_ravine",
                    "left_ravine",
                    "ice",
                    "ravine",
                    "map_edge"
                );
                ravine.blocksLineOfSight = false;
                ravine.blocksProjectiles = false;
                ravine.hazardType = HazardType.DeepWater;
                ravine.danger = true;
                return ravine;
            }

            if (IsWholeTileWall(x, y))
            {
                return Blocked(
                    x,
                    y,
                    TerrainType.Cliff,
                    y >= 8 ? 3 : y >= 5 ? 2 : 1,
                    "basalt_wall",
                    "wall_mass",
                    "wall",
                    "cliff",
                    "full_tile_obstacle"
                );
            }

            if (IsWholeTilePineOrBoulder(x, y))
            {
                BattleMapRuntimeCell blocker = Blocked(
                    x,
                    y,
                    IsPineTile(x, y) ? TerrainType.Forest : TerrainType.Rubble,
                    y >= 7 ? 2 : 1,
                    "snow_obstacle",
                    "obstacle_mass",
                    "full_tile_obstacle",
                    IsPineTile(x, y) ? "pine" : "boulder"
                );
                blocker.coverBonus = 2;
                return blocker;
            }

            int defaultElevation = y >= 9 ? 3 : y >= 7 ? 2 : y >= 4 ? 1 : 0;
            return Walkable(
                x,
                y,
                y >= 7 ? TerrainType.Hill : TerrainType.Snow,
                defaultElevation,
                y >= 7 ? 2 : 1,
                IsCoverSnowdrift(x, y) ? 1 : 0,
                "walkable_snow",
                "open_snow",
                "Default tile-aligned walkable snow. Mark red in the editor only where movement should be blocked.",
                0,
                "snow",
                "visible_path"
            );
        }

        private static bool IsAllyDeployment(int x, int y)
        {
            return (y == 0 && x >= 5 && x <= 10) || (y == 1 && x >= 5 && x <= 10);
        }

        private static bool IsEnemySpawn(int x, int y)
        {
            return (y == 10 && x >= 6 && x <= 9) || (y == 9 && x >= 6 && x <= 9);
        }

        private static bool IsCentralStair(int x, int y)
        {
            switch (y)
            {
                case 2:
                case 3:
                    return x >= 6 && x <= 9;
                case 4:
                case 5:
                    return x >= 6 && x <= 9;
                case 6:
                case 7:
                    return x >= 7 && x <= 9;
                case 8:
                    return x >= 7 && x <= 8;
                default:
                    return false;
            }
        }

        private static bool IsLeftConnector(int x, int y)
        {
            return (y == 2 && x == 5)
                || (y == 3 && x == 4)
                || (y == 4 && x >= 2 && x <= 4)
                || (y == 5 && x == 4)
                || (y == 6 && x == 4)
                || (y == 7 && x == 5)
                || (y == 8 && x == 5)
                || (y == 9 && x == 5);
        }

        private static bool IsRightConnector(int x, int y)
        {
            return (y == 2 && x == 10)
                || (y == 3 && x == 11)
                || (y == 4 && x == 12)
                || (y == 5 && x == 12)
                || (y == 6 && x == 11)
                || (y == 7 && x == 10)
                || (y == 8 && x == 10)
                || (y == 9 && x == 10);
        }

        private static bool IsUpperPlatform(int x, int y)
        {
            return (y == 9 && x >= 5 && x <= 10) || (y == 10 && x >= 5 && x <= 10);
        }

        private static bool IsLowerField(int x, int y)
        {
            return y <= 3 && x >= 3 && x <= 12;
        }

        private static bool IsLeftIceRavine(int x, int y)
        {
            return x <= 1 && y >= 2 && y <= 10;
        }

        private static bool IsWholeTileWall(int x, int y)
        {
            return (y == 5 && (x == 2 || x == 3 || x == 5 || x == 13))
                || (y == 6 && (x == 2 || x == 3 || x == 5 || x == 12 || x == 13))
                || (y == 7 && (x == 3 || x == 4 || x == 6 || x == 11 || x == 12))
                || (y == 8 && (x == 3 || x == 4 || x == 6 || x == 11 || x == 12))
                || (y == 11)
                || (x == 0 && y >= 0)
                || (x == 15 && y >= 0);
        }

        private static bool IsWholeTilePineOrBoulder(int x, int y)
        {
            return IsPineTile(x, y)
                || (x == 4 && y == 2)
                || (x == 11 && y == 2)
                || (x == 5 && y == 6)
                || (x == 10 && y == 6)
                || (x == 3 && y == 9)
                || (x == 12 && y == 9);
        }

        private static bool IsPineTile(int x, int y)
        {
            return (x == 13 && y >= 7 && y <= 10)
                || (x == 14 && y >= 2 && y <= 10)
                || (x == 2 && y == 8)
                || (x == 2 && y == 9);
        }

        private static bool IsCoverSnowdrift(int x, int y)
        {
            return (x == 5 && y == 3)
                || (x == 10 && y == 3)
                || (x == 6 && y == 6)
                || (x == 9 && y == 6)
                || (x == 6 && y == 9)
                || (x == 9 && y == 9);
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
            BattleMapRuntimeCell cell = BaseCell(x, y, terrain, elevation, zoneId, laneId, note, tags);
            cell.moveCost = Mathf.Max(1, moveCost);
            cell.coverBonus = Mathf.Max(0, coverBonus);
            cell.walkable = true;
            cell.occupyAllowed = true;
            cell.deployZone = deployZone;
            cell.isChokePoint =
                cell.HasTag("choke") || cell.HasTag("bridge") || cell.HasTag("stairs");
            cell.danger = terrain == TerrainType.Ice;
            cell.hazardType = terrain == TerrainType.Ice ? HazardType.Ice : HazardType.None;
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
                "Whole-tile obstacle: impassable tactical cell.",
                tags
            );
            cell.moveCost = 99;
            cell.walkable = false;
            cell.occupyAllowed = false;
            cell.blocksLineOfSight = terrain != TerrainType.DeepWater;
            cell.blocksProjectiles =
                cell.blocksLineOfSight || terrain == TerrainType.Wall || terrain == TerrainType.Cliff;
            cell.danger = terrain == TerrainType.Cliff || terrain == TerrainType.DeepWater;
            cell.hazardType =
                terrain == TerrainType.Cliff ? HazardType.Fall :
                terrain == TerrainType.DeepWater ? HazardType.DeepWater :
                HazardType.None;
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
                    string tag = tags[i];
                    if (!string.IsNullOrEmpty(tag) && !cell.tags.Contains(tag))
                    {
                        cell.tags.Add(tag);
                    }
                }
            }

            return cell;
        }
    }
}
