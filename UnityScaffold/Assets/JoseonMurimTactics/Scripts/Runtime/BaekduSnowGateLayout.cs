using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class BaekduCellSpec
{
    public TerrainType terrain = TerrainType.Snow;
    public string tileKey = "snow_e0";
    public int elevation;
    public int moveCost = 1;
    public bool walkable = true;
    public bool blocksMovement;
    public bool blocksLineOfSight;
    public bool isChokePoint;
    public int capacity = 2;
    public CoverType coverType = CoverType.None;
    public HazardType hazardType = HazardType.None;
    public EdgeType northEdge = EdgeType.None;
    public EdgeType eastEdge = EdgeType.None;
    public EdgeType southEdge = EdgeType.None;
    public EdgeType westEdge = EdgeType.None;
    public string zoneId = string.Empty;
    public string laneId = "canyon_floor";
    public string note = "Open snow field between the lower camp and Snow Gate.";
}

public static class BaekduSnowGateLayout
{
    public const int Width = 20;
    public const int Height = 14;
    public const float TileWidth = 1.16f;
    public const float TileHeight = 0.62f;

    public static BaekduCellSpec Resolve(Vector2Int cell)
    {
        int x = cell.x;
        int y = cell.y;
        BaekduCellSpec spec = Snow(cell, y <= 2 ? 0 : y <= 5 ? 1 : y <= 8 ? 2 : y <= 11 ? 3 : 4,
                                   "canyon_floor",
                                   "Snowfield: readable low ground with light exposure.");

        if (y <= 2 && x >= 5 && x <= 13)
        {
            spec = Road(cell, 0, "south_deployment", "Southern deployment road. Allies may reposition here in scout mode.");
            spec.zoneId = "deployment";
            return spec;
        }

        if (IsOuterCanyon(cell))
        {
            return CanyonEdge(cell);
        }

        if (x <= 5 && y >= 2 && y <= 11)
        {
            spec = LeftFlank(cell);
        }

        if (x >= 15 && y <= 3)
        {
            spec = IcyShoal(cell);
        }

        if (y == 4)
        {
            spec = FrozenStream(cell);
        }

        if (x >= 7 && x <= 12 && y >= 0 && y <= 13)
        {
            BaekduCellSpec central = CentralRoute(cell);
            if (central != null)
            {
                spec = central;
            }
        }

        if (x >= 5 && x <= 13 && y >= 6 && y <= 9 && !(x >= 8 && x <= 11))
        {
            spec = RubbleYard(cell);
        }

        if ((x == 6 || x == 12) && y >= 6 && y <= 11)
        {
            spec = CliffFace(cell, y >= 10 ? 3 : 2, "central_cliff_face",
                             "Vertical basalt face: blocks movement and line of sight.");
        }

        if (x >= 13 && y >= 5 && y <= 13)
        {
            spec = RightHighground(cell);
        }

        if (y >= 12 && (x <= 5 || x >= 18))
        {
            spec = Wall(cell, y >= 13 ? 4 : 3, "north_wall",
                        "Snow-buried outer wall. It frames the shrine and creates cliff depth.");
        }

        if ((x == 17 || x == 18) && y >= 12)
        {
            spec = BeaconPeak(cell);
        }

        if ((x == 9 || x == 10) && y >= 12)
        {
            spec.zoneId = "objective";
            spec.note = "Objective: protect the torn sect signboard at the Snow Gate.";
        }

        return spec;
    }

    public static Vector3 CellToWorld(Vector2Int cell)
    {
        float x = (cell.x - cell.y) * TileWidth * 0.5f;
        float y = (cell.x + cell.y) * TileHeight * 0.5f;
        return new Vector3(x, y, 0f);
    }

    private static BaekduCellSpec Snow(Vector2Int cell, int elevation, string lane, string note)
    {
        return new BaekduCellSpec
        {
            terrain = TerrainType.Snow,
            tileKey = Pick(cell, 1, "snow_e0", "snow_e0_b", "snow_e0_c", "snow_e0_d", "snow_e0_e", "snow_e0_f"),
            elevation = elevation,
            laneId = lane,
            note = note
        };
    }

    private static bool IsOuterCanyon(Vector2Int cell)
    {
        int x = cell.x;
        int y = cell.y;
        if (y == 0 && (x <= 3 || x >= 16))
        {
            return true;
        }

        if (y == 1 && (x <= 1 || x >= 18))
        {
            return true;
        }

        if (x == 0 && (y <= 3 || y >= 10))
        {
            return true;
        }

        if (x == 19 && (y <= 4 || y >= 9))
        {
            return true;
        }

        if (y == 13 && (x <= 2 || x >= 18))
        {
            return true;
        }

        return false;
    }

    private static BaekduCellSpec CanyonEdge(Vector2Int cell)
    {
        int elevation = cell.y <= 3 ? 0 : cell.y <= 7 ? 1 : cell.y <= 11 ? 2 : 3;
        return CliffFace(cell, elevation, "outer_canyon_depth",
                         "Outer canyon edge: fog and rock walls keep the battlefield from reading as a flat board.");
    }

    private static BaekduCellSpec Road(Vector2Int cell, int elevation, string lane, string note)
    {
        return new BaekduCellSpec
        {
            terrain = TerrainType.Road,
            tileKey = Pick(cell, 2, RoadKeys(elevation)),
            elevation = elevation,
            laneId = lane,
            note = note
        };
    }

    private static BaekduCellSpec LeftFlank(Vector2Int cell)
    {
        int elevation = cell.y >= 10 ? 2 : cell.y >= 6 ? 1 : 0;
        bool bamboo = cell.x >= 3 && cell.y >= 5 && cell.y <= 10;
        string prefix = bamboo ? "bamboo_e" + elevation : "forest_e" + elevation;
        return new BaekduCellSpec
        {
            terrain = bamboo ? TerrainType.Bamboo : TerrainType.Forest,
            tileKey = Pick(cell, bamboo ? 5 : 4, VariantKeys(prefix)),
            elevation = elevation,
            moveCost = bamboo ? 3 : 2,
            coverType = bamboo ? CoverType.Heavy : CoverType.Light,
            blocksLineOfSight = true,
            isChokePoint = (cell.x == 4 && (cell.y == 6 || cell.y == 7)) || (cell.x == 2 && cell.y == 9),
            capacity = 1,
            laneId = "left_bamboo_flank",
            note = "Left flank: bamboo and pine break sight, slow movement, and create ambush cover."
        };
    }

    private static BaekduCellSpec IcyShoal(Vector2Int cell)
    {
        bool hardIce = cell.x >= 17 || cell.y <= 1;
        return new BaekduCellSpec
        {
            terrain = hardIce ? TerrainType.Ice : TerrainType.ShallowWater,
            tileKey = Pick(cell, 7, hardIce
                                     ? new[] { "ice_slick", "ice_slick_b", "ice_slick_c", "cracked_ice" }
                                     : new[] { "shallow_water", "shallow_water_b", "shallow_water_c", "shallow_water_d" }),
            elevation = 0,
            moveCost = hardIce ? 2 : 3,
            hazardType = hardIce ? HazardType.Ice : HazardType.Slippery,
            northEdge = EdgeType.WaterBank,
            eastEdge = EdgeType.WaterBank,
            southEdge = EdgeType.WaterBank,
            westEdge = EdgeType.WaterBank,
            laneId = "right_icy_shoal",
            note = "Icy ford: ice arts can expand this route; palm strikes can push enemies into hazards."
        };
    }

    private static BaekduCellSpec FrozenStream(Vector2Int cell)
    {
        if (cell.x >= 9 && cell.x <= 10)
        {
            return new BaekduCellSpec
            {
                terrain = TerrainType.Bridge,
                tileKey = Pick(cell, 3, "bridge_e1", "bridge_e1_b", "bridge_e1_c", "bridge_e1_d"),
                elevation = 1,
                hazardType = HazardType.Collapse,
                isChokePoint = true,
                capacity = 1,
                northEdge = EdgeType.BridgeRail,
                southEdge = EdgeType.BridgeRail,
                laneId = "central_bridge_choke",
                note = "Central 1-2 tile wooden bridge bottleneck. The ropes can be cut."
            };
        }

        if ((cell.x >= 2 && cell.x <= 3) || (cell.x >= 15 && cell.x <= 16))
        {
            return new BaekduCellSpec
            {
                terrain = TerrainType.ShallowWater,
                tileKey = Pick(cell, 6, "shallow_water", "shallow_water_b", "shallow_water_c", "shallow_water_d"),
                elevation = 0,
                moveCost = 3,
                hazardType = HazardType.Slippery,
                northEdge = EdgeType.WaterBank,
                eastEdge = EdgeType.WaterBank,
                southEdge = EdgeType.WaterBank,
                westEdge = EdgeType.WaterBank,
                laneId = cell.x < 10 ? "left_ford" : "right_ford",
                note = "Frozen ford: slow exposed crossing below the ridge."
            };
        }

        return new BaekduCellSpec
        {
            terrain = TerrainType.DeepWater,
            tileKey = Pick(cell, 6, "deep_water", "deep_water_b", "dark_water", "dark_water_b"),
            elevation = 0,
            moveCost = 99,
            walkable = false,
            blocksMovement = true,
            hazardType = HazardType.DeepWater,
            northEdge = EdgeType.WaterBank,
            eastEdge = EdgeType.WaterBank,
            southEdge = EdgeType.WaterBank,
            westEdge = EdgeType.WaterBank,
            laneId = "frozen_stream",
            note = "Deep frozen stream blocks direct movement."
        };
    }

    private static BaekduCellSpec CentralRoute(Vector2Int cell)
    {
        int x = cell.x;
        int y = cell.y;
        if (y <= 3 && x >= 7 && x <= 12)
        {
            return Road(cell, 0, "south_approach", "Lower snow road from the allied deployment.");
        }

        if (y >= 5 && y <= 6 && x >= 8 && x <= 11)
        {
            BaekduCellSpec spec = Road(cell, 1, "central_bridge_choke", "Bridge landing and first stair.");
            spec.isChokePoint = x >= 9 && x <= 10;
            spec.capacity = spec.isChokePoint ? 1 : 2;
            return spec;
        }

        if (y >= 7 && y <= 8 && x >= 8 && x <= 11)
        {
            BaekduCellSpec spec = Road(cell, 2, "central_stair_choke", "Narrow stair under the shrine wall.");
            spec.isChokePoint = x >= 9 && x <= 10;
            spec.capacity = spec.isChokePoint ? 1 : 2;
            spec.northEdge = EdgeType.SlopeUp;
            spec.southEdge = EdgeType.SlopeDown;
            return spec;
        }

        if (y >= 9 && y <= 11 && x >= 7 && x <= 12)
        {
            return new BaekduCellSpec
            {
                terrain = TerrainType.ShrineFloor,
                tileKey = Pick(cell, 8, "shrine_e3", "shrine_e3_b", "shrine_e3_c", "shrine_e3_d"),
                elevation = 3,
                moveCost = 1,
                coverType = CoverType.Light,
                laneId = "north_gate_shrine",
                note = "Ruined shrine courtyard: objective pressure and light cover."
            };
        }

        if (y >= 12 && x >= 7 && x <= 12)
        {
            return new BaekduCellSpec
            {
                terrain = TerrainType.Gate,
                tileKey = Pick(cell, 9, "gate_e4", "gate_e4_b", "gate_e4_c", "gate_e4_d"),
                elevation = 4,
                moveCost = 1,
                coverType = CoverType.Light,
                laneId = "north_gate_shrine",
                zoneId = x >= 9 && x <= 10 ? "objective" : string.Empty,
                note = "Snow Gate summit: protect the sect signboard."
            };
        }

        return null;
    }

    private static BaekduCellSpec RubbleYard(Vector2Int cell)
    {
        int elevation = cell.y >= 8 ? 2 : 1;
        return new BaekduCellSpec
        {
            terrain = TerrainType.Rubble,
            tileKey = Pick(cell, 10, "rubble_e1", "rubble_e1_b", "rubble_e1_c", "rubble_e1_d"),
            elevation = elevation,
            moveCost = 2,
            coverType = (cell.x + cell.y) % 2 == 0 ? CoverType.Heavy : CoverType.Light,
            blocksLineOfSight = cell.x == 11 && cell.y >= 8,
            laneId = "broken_courtyard",
            note = "Broken courtyard: cover, debris, and knockback collision points."
        };
    }

    private static BaekduCellSpec CliffFace(Vector2Int cell, int elevation, string lane, string note)
    {
        return new BaekduCellSpec
        {
            terrain = TerrainType.Cliff,
            tileKey = Pick(cell, 11, "cliff_face", "cliff_face_b", "cliff_face_c", "cliff_face_d"),
            elevation = elevation,
            moveCost = 99,
            walkable = false,
            blocksMovement = true,
            blocksLineOfSight = true,
            hazardType = HazardType.Fall,
            northEdge = EdgeType.CliffDrop,
            eastEdge = EdgeType.CliffDrop,
            southEdge = EdgeType.CliffDrop,
            westEdge = EdgeType.CliffDrop,
            capacity = 1,
            laneId = lane,
            note = note
        };
    }

    private static BaekduCellSpec RightHighground(Vector2Int cell)
    {
        int elevation = cell.y >= 12 ? 4 : cell.y >= 10 ? 3 : cell.y >= 7 ? 2 : 1;
        string prefix = "ridge_e" + elevation;
        return new BaekduCellSpec
        {
            terrain = elevation >= 3 ? TerrainType.Hill : TerrainType.Cliff,
            tileKey = Pick(cell, 12, VariantKeys(prefix)),
            elevation = elevation,
            moveCost = elevation >= 3 ? 2 : 3,
            coverType = elevation >= 3 ? CoverType.Heavy : CoverType.Light,
            hazardType = HazardType.Fall,
            westEdge = cell.x == 13 ? EdgeType.CliffDrop : EdgeType.None,
            southEdge = cell.y == 5 || (cell.x >= 17 && cell.y <= 8) ? EdgeType.CliffDrop : EdgeType.None,
            isChokePoint = cell.x == 13 && cell.y >= 7 && cell.y <= 8,
            capacity = cell.x == 13 && cell.y >= 7 && cell.y <= 8 ? 1 : 2,
            laneId = "right_cliff_highground",
            note = "Right high ground: archers and palm users gain range, sight, and fall pressure."
        };
    }

    private static BaekduCellSpec BeaconPeak(Vector2Int cell)
    {
        return new BaekduCellSpec
        {
            terrain = TerrainType.Hill,
            tileKey = Pick(cell, 13, "ridge_e4", "ridge_e4_b", "ridge_e4_c", "ridge_e4_d"),
            elevation = 4,
            moveCost = 2,
            coverType = CoverType.Heavy,
            hazardType = HazardType.Fall,
            westEdge = EdgeType.CliffDrop,
            southEdge = EdgeType.CliffDrop,
            laneId = "beacon_peak",
            zoneId = "beacon",
            note = "Beacon peak: high-risk high-ground objective overlooking the whole map."
        };
    }

    private static BaekduCellSpec Wall(Vector2Int cell, int elevation, string lane, string note)
    {
        return new BaekduCellSpec
        {
            terrain = TerrainType.Wall,
            tileKey = Pick(cell, 14, "wall_e3", "wall_e3_b", "wall_e4", "wall_e4_b"),
            elevation = elevation,
            moveCost = 99,
            walkable = false,
            blocksMovement = true,
            blocksLineOfSight = true,
            hazardType = HazardType.Fall,
            laneId = lane,
            note = note
        };
    }

    private static string[] RoadKeys(int elevation)
    {
        switch (Mathf.Clamp(elevation, 0, 3))
        {
        case 0:
            return new[] { "road_e0", "road_e0_b", "road_e0_c", "road_e0_d" };
        case 1:
            return new[] { "road_e1", "road_e1_b", "road_e1_c", "road_e1_d" };
        case 2:
            return new[] { "road_e2", "road_e2_b", "road_e2_c", "road_e2_d" };
        default:
            return new[] { "road_e3", "road_e3_b", "road_e3_c", "road_e3_d" };
        }
    }

    private static string[] VariantKeys(string prefix)
    {
        return new[] { prefix, prefix + "_b", prefix + "_c", prefix + "_d" };
    }

    private static string Pick(Vector2Int cell, int salt, params string[] keys)
    {
        if (keys == null || keys.Length == 0)
        {
            return string.Empty;
        }

        int checker = (cell.x & 1) + ((cell.y & 1) * 2);
        int band = keys.Length > 4 ? (((cell.x + cell.y + salt) & 1) * 4) : 0;
        int index = (checker + band + salt) % keys.Length;
        return keys[Mathf.Abs(index)];
    }
}
}
