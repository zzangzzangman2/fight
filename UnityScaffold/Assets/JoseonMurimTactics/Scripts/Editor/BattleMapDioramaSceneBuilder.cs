#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics.Editor
{
public static class BattleMapDioramaSceneBuilder
{
    public const string ScenePath = "Assets/JoseonMurimTactics/Scenes/BattleMap_Baekdu_SnowGate_v1.unity";
    public const int Width = 20;
    public const int Height = 14;
    private const float TileWidth = 1.16f;
    private const float TileHeight = 0.62f;
    private const string TileAssetFolder = "Assets/JoseonMurimTactics/Art/BattleMaps/Tilesets/DioramaGenerated";

    [MenuItem("Joseon Murim Tactics/Rebuild Baekdu SnowGate v1.7 Diorama")]
    public static void RebuildBaekduSnowGateScene()
    {
        EnsureFolder("Assets/JoseonMurimTactics/Scenes");
        EnsureFolder(TileAssetFolder);

        Dictionary<string, TerrainTileData> tiles = BuildTileLibrary();
        Dictionary<string, Tile> visualTiles = BuildVisualTileLibrary(tiles);
        Dictionary<string, TerrainVariantSet> variants = BuildVariantSets(tiles);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BattleMap_Baekdu_SnowGate_v1";

        CreateCamera();
        BattleMapTilemapBinder binder = CreateMapRoot();
        PaintMap(binder, tiles, visualTiles, variants);
        CreateBackdrop(binder);
        CreateProps(binder);
        StripSerializedTilemaps(binder);
        CreateBattleController();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[BattleMapDioramaSceneBuilder] Rebuilt " + ScenePath);
    }

    public static void ValidateBaekduSnowGateScene()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        MapQualityValidator.GenerateMapQualityReport();
    }

    private static BattleMapTilemapBinder CreateMapRoot()
    {
        GameObject root = new GameObject("BattleMap_Baekdu_SnowGate_v1");
        BattleMapTilemapBinder binder = root.AddComponent<BattleMapTilemapBinder>();
        binder.ConfigureRuntime(Vector2Int.zero, new Vector2Int(Width, Height), TileWidth, TileHeight);

        BattleMapSceneController controller = root.AddComponent<BattleMapSceneController>();
        controller.ConfigureAuthoredScene("baekdu_snowgate_v1", "Baekdu Snow Gate", binder, Vector2Int.zero,
                                          new Vector2Int(Width, Height), TileWidth, TileHeight);

        return binder;
    }

    private static void StripSerializedTilemaps(BattleMapTilemapBinder binder)
    {
        Tilemap[] tilemaps = binder.GetComponentsInChildren<Tilemap>(true);
        foreach (Tilemap tilemap in tilemaps)
        {
            TilemapRenderer renderer = tilemap.GetComponent<TilemapRenderer>();
            if (renderer != null)
            {
                UnityEngine.Object.DestroyImmediate(renderer);
            }

            UnityEngine.Object.DestroyImmediate(tilemap);
        }
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Battle Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 7.25f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.22f, 0.34f, 0.32f, 1f);
        cameraObject.transform.position = new Vector3(2.40f, 5.95f, -10f);
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
    }

    private static void PaintMap(BattleMapTilemapBinder binder, Dictionary<string, TerrainTileData> tiles,
                                 Dictionary<string, Tile> visualTiles,
                                 Dictionary<string, TerrainVariantSet> variants)
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                CellSpec spec = ResolveCell(x, y);
                TerrainTileData snowBase = PickPattern(variants["snow_base"], cell, 1);
                PaintTile(binder.GroundBaseTilemap, cell, VisualTileFor(snowBase, visualTiles), Vary(Color.white, cell, 0.035f));

                TerrainTileData tacticalTile = ResolveTacticalTile(spec, cell, tiles, variants);
                PaintTile(PrimaryLayerFor(binder, spec), cell, VisualTileFor(tacticalTile, visualTiles),
                          Vary(Color.white, cell, 0.025f));
                PaintTransitions(binder, cell, spec, tiles, visualTiles, variants);
                PaintAtmosphere(binder, cell, spec, tiles, visualTiles);
            }
        }
    }

    private static TerrainTileData ResolveTacticalTile(CellSpec spec, Vector2Int cell,
                                                       Dictionary<string, TerrainTileData> tiles,
                                                       Dictionary<string, TerrainVariantSet> variants)
    {
        if (!string.IsNullOrEmpty(spec.variantKey) && variants.TryGetValue(spec.variantKey, out TerrainVariantSet set))
        {
            TerrainTileData picked = PickPattern(set, cell, spec.variantSalt);
            if (picked != null)
            {
                return picked;
            }
        }

        return tiles.TryGetValue(spec.tileKey, out TerrainTileData tile) ? tile : tiles["snow_e0"];
    }

    private static Tile VisualTileFor(TerrainTileData terrainTile, Dictionary<string, Tile> visualTiles)
    {
        if (terrainTile != null && visualTiles.TryGetValue(terrainTile.name, out Tile tile))
        {
            return tile;
        }

        return visualTiles.TryGetValue("snow_e0", out Tile fallback) ? fallback : null;
    }

    private static TacticalGridCellData CreateOverlayCell(Vector2Int cell, CellSpec spec)
    {
        return new TacticalGridCellData
        {
            cell = cell,
            displayName = spec.terrain.ToString(),
            worldPosition = CellToWorld(cell),
            terrainType = spec.terrain,
            moveCost = Mathf.Max(1, spec.moveCost),
            walkable = spec.walkable,
            blocksMovement = spec.blocksMovement,
            blocksLineOfSight = spec.blocksLineOfSight,
            isChokePoint = spec.isChokePoint,
            capacity = Mathf.Max(1, spec.capacity),
            elevation = spec.elevation,
            coverType = spec.coverType,
            hazardType = spec.hazardType,
            northEdge = spec.northEdge,
            eastEdge = spec.eastEdge,
            southEdge = spec.southEdge,
            westEdge = spec.westEdge,
            zoneId = spec.zoneId,
            laneId = spec.laneId,
            visualTileKey = spec.tileKey,
            decorSetKey = spec.note,
        };
    }

    private static Tilemap PrimaryLayerFor(BattleMapTilemapBinder binder, CellSpec spec)
    {
        switch (spec.terrain)
        {
        case TerrainType.Road:
        case TerrainType.Bridge:
        case TerrainType.Gate:
        case TerrainType.ShrineFloor:
            return binder.RoadPathTilemap;
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
        case TerrainType.Water:
        case TerrainType.Ice:
            return binder.WaterBaseTilemap;
        case TerrainType.Cliff:
        case TerrainType.Wall:
        case TerrainType.Hill:
            return spec.walkable ? binder.CliffTopTilemap : binder.CliffFaceTilemap;
        case TerrainType.Bamboo:
        case TerrainType.Forest:
        case TerrainType.Rubble:
            return binder.GroundVariationTilemap;
        default:
            return binder.GroundVariationTilemap;
        }
    }

    private static void PaintTransitions(BattleMapTilemapBinder binder, Vector2Int cell, CellSpec spec,
                                         Dictionary<string, TerrainTileData> tiles,
                                         Dictionary<string, Tile> visualTiles,
                                         Dictionary<string, TerrainVariantSet> variants)
    {
        if (spec.terrain == TerrainType.Road || spec.terrain == TerrainType.Bridge ||
            spec.terrain == TerrainType.ShrineFloor || spec.terrain == TerrainType.Gate)
        {
            PaintTile(binder.RoadEdgeTilemap, cell, VisualTileFor(PickPattern(variants["snow_edge"], cell, 5), visualTiles),
                      new Color(1f, 1f, 1f, 0.82f));
        }

        if (spec.terrain == TerrainType.ShallowWater || spec.terrain == TerrainType.DeepWater ||
            spec.terrain == TerrainType.Ice)
        {
            PaintTile(binder.WaterSurfaceTilemap, cell,
                      VisualTileFor(PickPattern(variants["ice_surface"], cell, 7), visualTiles),
                      new Color(0.86f, 0.96f, 1f, 0.72f));
            foreach (Vector2Int next in Neighbors(cell))
            {
                if (!Inside(next))
                {
                    continue;
                }

                CellSpec neighbor = ResolveCell(next.x, next.y);
                if (neighbor.terrain != TerrainType.ShallowWater && neighbor.terrain != TerrainType.DeepWater &&
                    neighbor.terrain != TerrainType.Ice)
                {
                    PaintTile(binder.DecorGroundTilemap, next, VisualTileFor(tiles["water_bank"], visualTiles),
                              new Color(1f, 1f, 1f, 0.62f));
                }
            }
        }

        if (HasCliffDrop(spec))
        {
            PaintTile(binder.CliffEdgeTilemap, cell, VisualTileFor(tiles["cliff_edge"], visualTiles),
                      new Color(1f, 1f, 1f, 0.95f));
            PaintTile(binder.ShadowAoTilemap, cell, VisualTileFor(tiles["shadow_ao"], visualTiles),
                      new Color(0.12f, 0.10f, 0.08f, 0.24f));
        }

        if (spec.coverType != CoverType.None || spec.blocksLineOfSight)
        {
            PaintTile(binder.DecorGrassRockSnowTilemap, cell,
                      VisualTileFor(PickPattern(variants["snow_decor"], cell, 11), visualTiles),
                      new Color(0.92f, 1f, 0.90f, 0.72f));
        }
    }

    private static void PaintAtmosphere(BattleMapTilemapBinder binder, Vector2Int cell, CellSpec spec,
                                        Dictionary<string, TerrainTileData> tiles, Dictionary<string, Tile> visualTiles)
    {
        if (cell.y <= 1 || spec.terrain == TerrainType.ShallowWater || spec.terrain == TerrainType.DeepWater)
        {
            PaintTile(binder.FogMistTilemap, cell, VisualTileFor(tiles["fog_mist"], visualTiles),
                      new Color(0.78f, 0.90f, 0.90f, 0.16f));
        }

        if ((cell.x + cell.y) % 3 == 0)
        {
            PaintTile(binder.GridSubtleTilemap, cell, VisualTileFor(tiles["grid_subtle"], visualTiles),
                      new Color(0.86f, 0.96f, 0.92f, 0.055f));
        }
    }

    private static CellSpec ResolveCell(int x, int y)
    {
        CellSpec spec = new CellSpec
        {
            terrain = TerrainType.Snow,
            tileKey = "snow_e0",
            variantKey = "snow_base",
            variantSalt = 3,
            elevation = y >= 8 ? 1 : 0,
            moveCost = 1,
            walkable = true,
            capacity = 2,
            laneId = "canyon_floor",
            note = "Open snow courtyard."
        };

        if (x <= 4 && y >= 3 && y <= 11)
        {
            spec.terrain = x <= 2 || y >= 8 ? TerrainType.Forest : TerrainType.Bamboo;
            spec.elevation = y >= 8 ? 1 : 0;
            spec.tileKey = spec.terrain == TerrainType.Forest ? "forest_e" + spec.elevation : "bamboo_e" + spec.elevation;
            spec.variantKey = spec.tileKey;
            spec.moveCost = 2;
            spec.coverType = x <= 2 ? CoverType.Light : CoverType.Heavy;
            spec.blocksLineOfSight = true;
            spec.isChokePoint = (x == 3 && (y == 6 || y == 7)) || (x == 2 && y == 9);
            spec.laneId = "left_forest_flank";
            spec.note = "Left forest flank: slow cover and sight breaks.";
            return spec;
        }

        if (y == 5)
        {
            spec.terrain = TerrainType.DeepWater;
            spec.tileKey = "deep_water";
            spec.variantKey = "dark_water";
            spec.moveCost = 99;
            spec.walkable = false;
            spec.blocksMovement = true;
            spec.hazardType = HazardType.DeepWater;
            spec.laneId = "frozen_stream";
            spec.note = "Deep frozen stream blocks direct movement.";

            if (x >= 8 && x <= 10)
            {
                spec.terrain = TerrainType.Bridge;
                spec.tileKey = "bridge_e1";
                spec.variantKey = string.Empty;
                spec.elevation = 1;
                spec.moveCost = 1;
                spec.walkable = true;
                spec.blocksMovement = false;
                spec.hazardType = HazardType.Collapse;
                spec.isChokePoint = true;
                spec.northEdge = EdgeType.BridgeRail;
                spec.southEdge = EdgeType.BridgeRail;
                spec.laneId = "central_bridge_choke";
                spec.note = "Central bridge bottleneck.";
            }
            else if ((x >= 2 && x <= 3) || (x >= 15 && x <= 16))
            {
                spec.terrain = TerrainType.ShallowWater;
                spec.tileKey = "shallow_water";
                spec.variantKey = "ice_surface";
                spec.moveCost = 3;
                spec.walkable = true;
                spec.blocksMovement = false;
                spec.hazardType = HazardType.Slippery;
                spec.laneId = x < 10 ? "left_ford" : "right_ford";
                spec.note = "Frozen ford: slow exposed crossing.";
            }

            return spec;
        }

        if (x >= 7 && x <= 11 && y <= 4)
        {
            spec.terrain = TerrainType.Road;
            spec.tileKey = "road_e0";
            spec.variantKey = "road_e0";
            spec.elevation = 0;
            spec.moveCost = 1;
            spec.isChokePoint = x == 9 && y >= 3;
            spec.laneId = "south_approach";
            spec.note = "Southern approach road into the pass.";
            return spec;
        }

        if (x >= 7 && x <= 11 && y >= 6 && y <= 10)
        {
            spec.terrain = y >= 9 ? TerrainType.ShrineFloor : TerrainType.Road;
            spec.tileKey = y >= 9 ? "shrine_e2" : "road_e" + Mathf.Min(2, y - 5);
            spec.variantKey = y >= 9 ? "shrine_e2" : "road_e" + Mathf.Min(2, y - 5);
            spec.elevation = Mathf.Min(2, y - 5);
            spec.moveCost = 1;
            spec.coverType = y >= 9 ? CoverType.Light : CoverType.None;
            spec.isChokePoint = y <= 8 && x >= 8 && x <= 10;
            spec.laneId = "central_stair_choke";
            spec.note = "Raised stair path through the Snow Gate.";
            return spec;
        }

        if (x >= 7 && x <= 12 && y >= 11)
        {
            spec.terrain = y >= 12 ? TerrainType.Gate : TerrainType.ShrineFloor;
            spec.tileKey = y >= 12 ? "gate_e2" : "shrine_e2";
            spec.variantKey = y >= 12 ? string.Empty : "shrine_e2";
            spec.elevation = 2;
            spec.moveCost = 1;
            spec.coverType = CoverType.Light;
            spec.zoneId = x >= 9 && x <= 10 && y >= 12 ? "objective" : string.Empty;
            spec.laneId = "north_gate_shrine";
            spec.note = "Gate shrine objective plateau.";
            return spec;
        }

        if ((x == 6 || x == 12) && y >= 6 && y <= 10)
        {
            spec.terrain = TerrainType.Cliff;
            spec.tileKey = "cliff_face";
            spec.variantKey = string.Empty;
            spec.elevation = 2;
            spec.moveCost = 99;
            spec.walkable = false;
            spec.blocksMovement = true;
            spec.blocksLineOfSight = true;
            spec.hazardType = HazardType.Fall;
            spec.northEdge = EdgeType.CliffDrop;
            spec.southEdge = EdgeType.CliffDrop;
            spec.laneId = "central_cliff_face";
            spec.note = "Basalt cliff face divides the central pass.";
            return spec;
        }

        if (x >= 13 && y >= 6 && y <= 12)
        {
            spec.terrain = y >= 10 ? TerrainType.Hill : TerrainType.Cliff;
            spec.tileKey = y >= 10 ? "ridge_e3" : "ridge_e2";
            spec.variantKey = y >= 10 ? "ridge_e3" : "ridge_e2";
            spec.elevation = y >= 10 ? 3 : 2;
            spec.moveCost = 2;
            spec.coverType = CoverType.Light;
            spec.laneId = "right_cliff_highground";
            spec.note = "Right cliff high ground with fall edges.";
            if (x == 13 || y == 6 || (x >= 17 && y <= 8))
            {
                spec.westEdge = EdgeType.CliffDrop;
                spec.southEdge = EdgeType.CliffDrop;
                spec.hazardType = HazardType.Fall;
                spec.isChokePoint = x == 13 && y >= 7 && y <= 8;
            }
            return spec;
        }

        if (x >= 15 && y <= 4)
        {
            spec.terrain = x >= 17 && y <= 2 ? TerrainType.Ice : TerrainType.ShallowWater;
            spec.tileKey = spec.terrain == TerrainType.Ice ? "ice_slick" : "shallow_water";
            spec.variantKey = "ice_surface";
            spec.moveCost = spec.terrain == TerrainType.Ice ? 2 : 3;
            spec.hazardType = HazardType.Slippery;
            spec.laneId = "right_icy_shoal";
            spec.note = "Right icy shoal under the cliff.";
            return spec;
        }

        if (x <= 5 && y <= 2)
        {
            spec.terrain = TerrainType.Forest;
            spec.tileKey = "forest_e0";
            spec.variantKey = "forest_e0";
            spec.coverType = CoverType.Light;
            spec.blocksLineOfSight = x <= 2;
            spec.moveCost = 2;
            spec.laneId = "southwest_pines";
            spec.note = "Pine cover on the southern approach.";
            return spec;
        }

        if (x >= 5 && x <= 13 && y >= 6 && y <= 9)
        {
            spec.terrain = TerrainType.Rubble;
            spec.tileKey = "rubble_e1";
            spec.variantKey = "rubble_e1";
            spec.elevation = 1;
            spec.moveCost = 2;
            spec.coverType = (x + y) % 2 == 0 ? CoverType.Heavy : CoverType.Light;
            spec.blocksLineOfSight = x == 11 && y >= 8;
            spec.laneId = "broken_courtyard";
            spec.note = "Broken courtyard cover near the gate.";
            return spec;
        }

        if (y >= 12 && (x <= 6 || x >= 13))
        {
            spec.terrain = TerrainType.Wall;
            spec.tileKey = "wall_e2";
            spec.variantKey = string.Empty;
            spec.elevation = 2;
            spec.walkable = false;
            spec.blocksMovement = true;
            spec.blocksLineOfSight = true;
            spec.hazardType = HazardType.Fall;
            spec.laneId = "north_wall";
            spec.note = "Snow-covered gate wall.";
            return spec;
        }

        return spec;
    }

    private static Dictionary<string, TerrainTileData> BuildTileLibrary()
    {
        Dictionary<string, TerrainTileData> tiles = new Dictionary<string, TerrainTileData>();
        AddTile(tiles, "snow_e0", TerrainType.Snow, "Tiles/baekdu_snow_plain", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "snow_e0_b", TerrainType.Snow, "Tiles/baekdu_deep_snow", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "snow_e0_c", TerrainType.Snow, "Tiles/baekdu_snow_mountain_pass", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "snow_edge", TerrainType.Snow, "Tiles/snow_edge", 1, true, false, false, 0, CoverType.None,
                HazardType.None);
        AddTile(tiles, "forest_e0", TerrainType.Forest, "Tiles/baekdu_snow_pine_floor", 2, true, true, false, 0,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e0_b", TerrainType.Forest, "Tiles/forest_floor", 2, true, true, false, 0,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e1", TerrainType.Forest, "Tiles/baekdu_snow_pine_floor", 2, true, true, false, 1,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e1_b", TerrainType.Forest, "Tiles/forest_floor", 2, true, true, false, 1,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "bamboo_e0", TerrainType.Bamboo, "Tiles/baekdu_snow_bamboo_floor", 2, true, true, false, 0,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e0_b", TerrainType.Bamboo, "Tiles/bamboo_floor", 2, true, true, false, 0,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e1", TerrainType.Bamboo, "Tiles/baekdu_snow_bamboo_floor", 2, true, true, false, 1,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e1_b", TerrainType.Bamboo, "Tiles/bamboo_floor", 2, true, true, false, 1,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "road_e0", TerrainType.Road, "Tiles/baekdu_frozen_stair_road", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e0_b", TerrainType.Road, "Tiles/road_stair", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e1", TerrainType.Road, "Tiles/baekdu_frozen_stair_road", 1, true, false, true, 1,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e1_b", TerrainType.Road, "Tiles/road_stair", 1, true, false, true, 1,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e2", TerrainType.Road, "Tiles/baekdu_frozen_stair_road", 1, true, false, true, 2,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e2_b", TerrainType.Road, "Tiles/road_stair", 1, true, false, true, 2,
                CoverType.None, HazardType.None);
        AddTile(tiles, "bridge_e1", TerrainType.Bridge, "Tiles/wood_bridge", 1, true, false, true, 1,
                CoverType.None, HazardType.Collapse, EdgeType.BridgeRail, EdgeType.BridgeRail, EdgeType.BridgeRail,
                EdgeType.BridgeRail, string.Empty, "central_bridge_choke");
        AddTile(tiles, "shrine_e2", TerrainType.ShrineFloor, "Tiles/baekdu_snow_shrine_floor", 1, true, false, false,
                2, CoverType.Light, HazardType.None);
        AddTile(tiles, "shrine_e2_b", TerrainType.ShrineFloor, "Tiles/shrine_floor", 1, true, false, false, 2,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "gate_e2", TerrainType.Gate, "Tiles/gate_threshold", 1, true, false, false, 2, CoverType.Light,
                HazardType.None, EdgeType.Gate, EdgeType.Gate, EdgeType.None, EdgeType.None, "objective",
                "north_gate_shrine");
        AddTile(tiles, "ridge_e2", TerrainType.Cliff, "Tiles/baekdu_snow_basalt_cliff", 2, true, false, false, 2,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e2_b", TerrainType.Cliff, "Tiles/cliff_face", 2, true, false, false, 2,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e3", TerrainType.Hill, "Tiles/baekdu_wind_snow_ridge", 2, true, false, false, 3,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e3_b", TerrainType.Hill, "Tiles/baekdu_volcanic_snow_rock", 2, true, false, false, 3,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "cliff_face", TerrainType.Cliff, "Tiles/baekdu_snow_basalt_cliff", 99, false, true, false, 2,
                CoverType.None, HazardType.Fall, EdgeType.CliffDrop, EdgeType.CliffDrop, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "central_cliff_face");
        AddTile(tiles, "cliff_edge", TerrainType.Cliff, "Tiles/cliff_face", 99, false, true, false, 2,
                CoverType.None, HazardType.Fall);
        AddTile(tiles, "wall_e2", TerrainType.Wall, "Tiles/wall_broken", 99, false, true, false, 2, CoverType.None,
                HazardType.Fall);
        AddTile(tiles, "rubble_e1", TerrainType.Rubble, "Tiles/rubble", 2, true, true, false, 1, CoverType.Heavy,
                HazardType.None);
        AddTile(tiles, "rubble_e1_b", TerrainType.Rubble, "Tiles/baekdu_volcanic_snow_rock", 2, true, true, false, 1,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "shallow_water", TerrainType.ShallowWater, "Tiles/baekdu_frozen_stream", 3, true, false,
                false, 0, CoverType.None, HazardType.Slippery, EdgeType.WaterBank, EdgeType.WaterBank,
                EdgeType.WaterBank, EdgeType.WaterBank, string.Empty, "frozen_stream");
        AddTile(tiles, "shallow_water_b", TerrainType.ShallowWater, "Tiles/shallow_water", 3, true, false, false, 0,
                CoverType.None, HazardType.Slippery, EdgeType.WaterBank, EdgeType.WaterBank, EdgeType.WaterBank,
                EdgeType.WaterBank, string.Empty, "frozen_stream");
        AddTile(tiles, "shallow_water_c", TerrainType.ShallowWater, "Tiles/baekdu_ice_slick", 3, true, false, false,
                0, CoverType.None, HazardType.Slippery, EdgeType.WaterBank, EdgeType.WaterBank, EdgeType.WaterBank,
                EdgeType.WaterBank, string.Empty, "frozen_stream");
        AddTile(tiles, "deep_water", TerrainType.DeepWater, "Tiles/baekdu_dark_frozen_water", 99, false, false, false,
                0, CoverType.None, HazardType.DeepWater, EdgeType.WaterBank, EdgeType.WaterBank, EdgeType.WaterBank,
                EdgeType.WaterBank, string.Empty, "frozen_stream");
        AddTile(tiles, "deep_water_b", TerrainType.DeepWater, "Tiles/deep_water", 99, false, false, false, 0,
                CoverType.None, HazardType.DeepWater, EdgeType.WaterBank, EdgeType.WaterBank, EdgeType.WaterBank,
                EdgeType.WaterBank, string.Empty, "frozen_stream");
        AddTile(tiles, "ice_slick", TerrainType.Ice, "Tiles/baekdu_ice_slick", 2, true, false, false, 0,
                CoverType.None, HazardType.Slippery);
        AddTile(tiles, "cracked_ice", TerrainType.Ice, "Tiles/baekdu_cracked_ice_hazard", 2, true, false, false, 0,
                CoverType.None, HazardType.Ice);
        AddTile(tiles, "dark_water", TerrainType.DeepWater, "Tiles/baekdu_dark_frozen_water", 99, false, false, false,
                0, CoverType.None, HazardType.DeepWater);
        AddTile(tiles, "water_bank", TerrainType.Snow, "Tiles/snow_edge", 1, true, false, false, 0, CoverType.None,
                HazardType.None);
        AddTile(tiles, "snow_decor", TerrainType.Snow, "Tiles/baekdu_volcanic_snow_rock", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "shadow_ao", TerrainType.Stone, "Tiles/baekdu_volcanic_snow_rock", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "fog_mist", TerrainType.Smoke, "Tiles/smoke_veil", 1, true, true, false, 0, CoverType.None,
                HazardType.Smoke);
        AddTile(tiles, "grid_subtle", TerrainType.Stone, "Tiles/baekdu_snow_plain", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        return tiles;
    }

    private static Dictionary<string, TerrainVariantSet> BuildVariantSets(Dictionary<string, TerrainTileData> tiles)
    {
        Dictionary<string, TerrainVariantSet> variants = new Dictionary<string, TerrainVariantSet>();
        AddVariantSet(variants, "snow_base", tiles["snow_e0"], tiles["snow_e0_b"], tiles["snow_e0_c"]);
        AddVariantSet(variants, "snow_edge", tiles["snow_edge"], tiles["snow_e0_b"], tiles["snow_decor"]);
        AddVariantSet(variants, "snow_decor", tiles["snow_decor"], tiles["snow_edge"], tiles["snow_e0_c"]);
        AddVariantSet(variants, "forest_e0", tiles["forest_e0"], tiles["forest_e0_b"]);
        AddVariantSet(variants, "forest_e1", tiles["forest_e1"], tiles["forest_e1_b"]);
        AddVariantSet(variants, "bamboo_e0", tiles["bamboo_e0"], tiles["bamboo_e0_b"]);
        AddVariantSet(variants, "bamboo_e1", tiles["bamboo_e1"], tiles["bamboo_e1_b"]);
        AddVariantSet(variants, "road_e0", tiles["road_e0"], tiles["road_e0_b"]);
        AddVariantSet(variants, "road_e1", tiles["road_e1"], tiles["road_e1_b"]);
        AddVariantSet(variants, "road_e2", tiles["road_e2"], tiles["road_e2_b"]);
        AddVariantSet(variants, "shrine_e2", tiles["shrine_e2"], tiles["shrine_e2_b"]);
        AddVariantSet(variants, "ridge_e2", tiles["ridge_e2"], tiles["ridge_e2_b"]);
        AddVariantSet(variants, "ridge_e3", tiles["ridge_e3"], tiles["ridge_e3_b"]);
        AddVariantSet(variants, "rubble_e1", tiles["rubble_e1"], tiles["rubble_e1_b"]);
        AddVariantSet(variants, "ice_surface", tiles["shallow_water"], tiles["shallow_water_b"], tiles["shallow_water_c"]);
        AddVariantSet(variants, "dark_water", tiles["deep_water"], tiles["deep_water_b"]);
        return variants;
    }

    private static Dictionary<string, Tile> BuildVisualTileLibrary(Dictionary<string, TerrainTileData> terrainTiles)
    {
        Dictionary<string, Tile> visualTiles = new Dictionary<string, Tile>();
        foreach (KeyValuePair<string, TerrainTileData> entry in terrainTiles)
        {
            string assetPath = TileAssetFolder + "/" + entry.Key + "_visual.asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(assetPath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, assetPath);
            }

            tile.name = entry.Key;
            tile.sprite = entry.Value == null ? null : entry.Value.sprite;
            tile.color = Color.white;
            tile.flags = TileFlags.None;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            visualTiles[entry.Key] = tile;
        }

        return visualTiles;
    }

    private static void AddTile(Dictionary<string, TerrainTileData> tiles, string key, TerrainType terrainType,
                                string spritePath, int moveCost, bool walkable, bool blocksLineOfSight,
                                bool isChokePoint, int elevation, CoverType coverType, HazardType hazardType,
                                EdgeType northEdge = EdgeType.None, EdgeType eastEdge = EdgeType.None,
                                EdgeType southEdge = EdgeType.None, EdgeType westEdge = EdgeType.None,
                                string zoneId = "", string laneId = "")
    {
        string assetPath = TileAssetFolder + "/" + key + ".asset";
        TerrainTileData tile = AssetDatabase.LoadAssetAtPath<TerrainTileData>(assetPath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<TerrainTileData>();
            AssetDatabase.CreateAsset(tile, assetPath);
        }

        tile.name = key;
        tile.sprite = LoadSprite(spritePath);
        tile.color = Color.white;
        tile.flags = TileFlags.None;
        tile.colliderType = Tile.ColliderType.None;
        tile.terrainType = terrainType;
        tile.moveCost = Mathf.Max(1, moveCost);
        tile.walkable = walkable;
        tile.blocksMovement = !walkable;
        tile.blocksLineOfSight = blocksLineOfSight;
        tile.isChokePoint = isChokePoint;
        tile.capacity = isChokePoint ? 1 : 2;
        tile.elevation = elevation;
        tile.coverType = coverType;
        tile.hazardType = hazardType;
        tile.northEdge = northEdge;
        tile.eastEdge = eastEdge;
        tile.southEdge = southEdge;
        tile.westEdge = westEdge;
        tile.zoneId = zoneId;
        tile.laneId = laneId;
        EditorUtility.SetDirty(tile);
        tiles[key] = tile;
    }

    private static void AddVariantSet(Dictionary<string, TerrainVariantSet> variants, string key,
                                      params TerrainTileData[] terrainTiles)
    {
        string assetPath = TileAssetFolder + "/" + key + "_variants.asset";
        TerrainVariantSet set = AssetDatabase.LoadAssetAtPath<TerrainVariantSet>(assetPath);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<TerrainVariantSet>();
            AssetDatabase.CreateAsset(set, assetPath);
        }

        set.name = key + "_variants";
        set.setId = key;
        set.variants = new WeightedTerrainTile[terrainTiles.Length];
        for (int i = 0; i < terrainTiles.Length; i++)
        {
            set.variants[i] = new WeightedTerrainTile { tile = terrainTiles[i], weight = i == 0 ? 5 : 2 };
        }

        EditorUtility.SetDirty(set);
        variants[key] = set;
    }

    private static TerrainTileData PickPattern(TerrainVariantSet set, Vector2Int cell, int salt)
    {
        if (set == null || set.variants == null || set.variants.Length == 0)
        {
            return null;
        }

        List<TerrainTileData> valid = new List<TerrainTileData>();
        foreach (WeightedTerrainTile variant in set.variants)
        {
            if (variant != null && variant.tile != null)
            {
                valid.Add(variant.tile);
            }
        }

        if (valid.Count == 0)
        {
            return null;
        }

        int index = Mathf.Abs((cell.x * 2) + (cell.y * 3) + salt) % valid.Count;
        return valid[index];
    }

    private static void CreateBackdrop(BattleMapTilemapBinder binder)
    {
        Transform root = binder.transform;
        Vector3 center = CellToWorld(new Vector2Int(9, 7));
        CreateBackdropSprite(root, "Backdrop_Snow_Sky_Wash", LoadSprite("Tiles/baekdu_snow_mountain_pass"),
                             center + new Vector3(0f, 1.4f, 0.20f), new Vector3(13.8f, 8.2f, 1f), 45f,
                             new Color(0.34f, 0.52f, 0.54f, 0.42f), -220);
        CreateBackdropSprite(root, "Backdrop_Distant_Baekdu_Ridge", LoadSprite("Tiles/baekdu_wind_snow_ridge"),
                             center + new Vector3(-1.4f, 4.1f, 0.16f), new Vector3(5.8f, 2.0f, 1f), 0f,
                             new Color(0.56f, 0.68f, 0.64f, 0.70f), -210);
        CreateBackdropSprite(root, "Backdrop_Pine_Left", LoadSprite("Objects/baekdu_snow_pine"),
                             CellToWorld(new Vector2Int(0, 10)) + new Vector3(-1.4f, 0.45f, 0.10f),
                             new Vector3(1.25f, 1.25f, 1f), -4f, new Color(0.70f, 0.84f, 0.76f, 0.78f), -120);
        CreateBackdropSprite(root, "Backdrop_Pine_Right", LoadSprite("Objects/baekdu_snow_pine"),
                             CellToWorld(new Vector2Int(19, 6)) + new Vector3(1.1f, 0.35f, 0.10f),
                             new Vector3(-1.05f, 1.05f, 1f), 5f, new Color(0.62f, 0.78f, 0.70f, 0.68f), -118);
    }

    private static void CreateProps(BattleMapTilemapBinder binder)
    {
        CreateProp(binder, "gate_signboard", "Gate signboard", InteractableKind.SectSignboard, new Vector2Int(9, 12),
                   "Objects/sect_signboard", true, CoverType.Light, false, false, true);
        CreateProp(binder, "incense_burner", "Incense burner", InteractableKind.IncenseBurner, new Vector2Int(9, 11),
                   "Objects/incense_burner", true, CoverType.Light, true, false, true);
        CreateProp(binder, "stone_lantern", "Frozen stone lantern", InteractableKind.RockLantern,
                   new Vector2Int(11, 10), "Objects/baekdu_frozen_stone_lantern", true, CoverType.Heavy, true, true,
                   true);
        CreateProp(binder, "bamboo_bundle", "Bamboo bundle", InteractableKind.BambooBundle, new Vector2Int(3, 8),
                   "Objects/bamboo_bundle", true, CoverType.Heavy, true, true, false);
        CreateProp(binder, "snow_pine_a", "Snow pine", InteractableKind.BambooBundle, new Vector2Int(2, 9),
                   "Objects/baekdu_snow_pine", true, CoverType.Heavy, true, true, false);
        CreateProp(binder, "snow_boulder", "Frozen boulder", InteractableKind.RockLantern, new Vector2Int(14, 8),
                   "Objects/baekdu_snow_boulder", true, CoverType.Full, true, true, false);
        CreateProp(binder, "bridge_rope", "Bridge rope", InteractableKind.WoodenBridge, new Vector2Int(10, 5),
                   "Objects/bridge_rope", true, CoverType.None, false, true, false);
        CreateProp(binder, "oil_jar", "Oil jar", InteractableKind.OilJar, new Vector2Int(7, 2), "Objects/oil_jar",
                   true, CoverType.None, false, true, true);
        CreateProp(binder, "red_lantern", "Red lantern", InteractableKind.Lantern, new Vector2Int(8, 3),
                   "Objects/red_lantern", true, CoverType.None, false, true, true);
        CreateProp(binder, "ice_crystal", "Ice crystal", InteractableKind.RockLantern, new Vector2Int(16, 6),
                   "Objects/baekdu_ice_crystal", true, CoverType.Light, true, true, false);
        CreateProp(binder, "broken_gate", "Broken snow gate", InteractableKind.Gate, new Vector2Int(10, 12),
                   "Objects/baekdu_broken_snow_gate", true, CoverType.Heavy, true, true, true);
        CreateProp(binder, "hot_spring_steam", "Hot spring steam", InteractableKind.IncenseBurner,
                   new Vector2Int(16, 2), "Objects/baekdu_hot_spring_steam", true, CoverType.Light, true, false, true);
    }

    private static void CreateProp(BattleMapTilemapBinder binder, string id, string displayName, InteractableKind kind,
                                   Vector2Int cell, string spritePath, bool interactive, CoverType cover,
                                   bool blocksLineOfSight, bool destructible, bool emitsLight)
    {
        GameObject propObject = new GameObject("Prop_" + id);
        propObject.transform.SetParent(binder.PropsRoot, false);
        propObject.transform.position = CellToWorld(cell) + new Vector3(0f, 0.16f, -0.08f);
        propObject.transform.localScale = Vector3.one * 0.72f;

        SpriteRenderer renderer = propObject.AddComponent<SpriteRenderer>();
        renderer.sprite = LoadSprite(spritePath);
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = 2100 + ((cell.x + cell.y) * 6);

        SortingGroup sortingGroup = propObject.AddComponent<SortingGroup>();
        sortingGroup.sortingLayerName = "Default";
        sortingGroup.sortingOrder = renderer.sortingOrder;

        ShadowBlob shadow = propObject.AddComponent<ShadowBlob>();
        shadow.Configure(new Vector2(0.96f, 0.26f), new Color(0.025f, 0.022f, 0.018f, 0.28f),
                         renderer.sortingOrder - 2);

        MapPropView view = propObject.AddComponent<MapPropView>();
        view.Configure(id, displayName, cell, kind, interactive);

        if (interactive)
        {
            InteractableProp interactable = propObject.AddComponent<InteractableProp>();
            interactable.Configure(ActionSlot.Main, kind == InteractableKind.Lantern || kind == InteractableKind.OilJar
                                                       ? StatType.InnerPower
                                                       : StatType.Strength,
                                   kind == InteractableKind.SectSignboard || kind == InteractableKind.Gate ? 0 : 12,
                                   emitsLight ? 2 : 1, EffectFor(kind), true);
        }

        if (cover != CoverType.None)
        {
            propObject.AddComponent<CoverProvider>().Configure(cover, cover == CoverType.Full ? 4 : 2);
        }

        if (blocksLineOfSight)
        {
            propObject.AddComponent<LineOfSightBlocker>().Configure(2, 1);
        }

        if (destructible)
        {
            propObject.AddComponent<DestructibleProp>().Configure(12, TerrainType.Rubble, HazardType.None,
                                                                  EffectFor(kind));
        }

        if (emitsLight)
        {
            Color lightColor = kind == InteractableKind.Lantern || kind == InteractableKind.OilJar
                                   ? new Color(1f, 0.46f, 0.18f, 1f)
                                   : new Color(0.82f, 0.90f, 0.96f, 1f);
            propObject.AddComponent<MapLightAnchor>().Configure(lightColor, 1.55f, 0.48f);
        }
    }

    private static void CreateBattleController()
    {
        GameObject controllerObject = new GameObject("Battle Test Controller");
        BattleTestController controller = controllerObject.AddComponent<BattleTestController>();
        controller.width = Width;
        controller.height = Height;
        controller.tileWidth = TileWidth;
        controller.tileHeight = TileHeight;
        controller.useAuthoredSceneMap = true;
        controller.useTilemapBattlefield = true;
        controller.useCanvasHud = true;
        controller.unitDefinitions = new[] {
            Unit("park_sungjun", "Park Sungjun", Faction.Ally, "park_sungjun_visual.asset", new Vector2Int(8, 1),
                 36, 5, 15, 16, 4, 1, 7, 15, 6, 10, "Baekdu Light Sword", BattleSpecialEffect.Mark),
            Unit("baek_ryeon", "Baek Ryeon", Faction.Ally, "baek_ryeon_visual.asset", new Vector2Int(7, 2), 30, 4,
                 12, 13, 4, 2, 5, 14, 5, 8, "Snow Cliff Palm", BattleSpecialEffect.Freeze),
            Unit("do_arin", "Do Arin", Faction.Ally, "do_arin_visual.asset", new Vector2Int(10, 1), 34, 3, 14, 15,
                 4, 1, 7, 14, 6, 11, "Iron Mountain Palm", BattleSpecialEffect.BreakGuard),
            Unit("jin_seoyul", "Jin Seoyul", Faction.Ally, "jin_seoyul_visual.asset", new Vector2Int(11, 2), 24, 4,
                 18, 19, 5, 2, 6, 12, 4, 7, "Sky Rod Form", BattleSpecialEffect.Strike),
            Unit("han_biyeon", "Han Biyeon", Faction.Ally, "han_biyeon_visual.asset", new Vector2Int(4, 3), 27, 4,
                 16, 17, 5, 3, 6, 13, 4, 8, "Poison Needle", BattleSpecialEffect.Poison),
            Unit("iron_wolf_guard_1", "Iron Wolf Guard", Faction.Enemy,
                 "SchoolCombat/school_combat_04_visual.asset", new Vector2Int(9, 10), 30, 3, 12, 12, 4, 1, 5, 14,
                 5, 8, "Iron Slash", BattleSpecialEffect.Strike),
            Unit("iron_wolf_spear_1", "Iron Wolf Spear", Faction.Enemy,
                 "SchoolCombat/school_combat_05_visual.asset", new Vector2Int(11, 11), 32, 3, 11, 12, 4, 2, 5,
                 15, 5, 9, "Wolf Spear", BattleSpecialEffect.BreakGuard),
            Unit("iron_wolf_captain", "Iron Wolf Captain", Faction.Enemy,
                 "SchoolCombat/school_combat_06_visual.asset", new Vector2Int(14, 10), 38, 4, 13, 13, 4, 1, 7,
                 16, 6, 11, "Pack Order", BattleSpecialEffect.Mark),
            Unit("ridge_archer", "Ridge Archer", Faction.Enemy, "SchoolCombat/school_combat_03_visual.asset",
                 new Vector2Int(16, 9), 26, 3, 15, 16, 4, 3, 5, 13, 4, 7, "Ridge Shot",
                 BattleSpecialEffect.Strike)
        };
    }

    private static BattleTestUnitDefinition Unit(string id, string displayName, Faction faction, string visualFile,
                                                 Vector2Int startCell, int maxHp, int maxInner, int initiative,
                                                 int agility, int moveRange, int attackRange, int attackBonus,
                                                 int defense, int damageMin, int damageMax, string specialName,
                                                 BattleSpecialEffect specialEffect)
    {
        return new BattleTestUnitDefinition { id = id,
                                              displayName = displayName,
                                              faction = faction,
                                              visual = LoadVisual(visualFile),
                                              startCell = startCell,
                                              sectName = faction == Faction.Ally ? "Baekdu Alliance" : "Iron Wolf Sect",
                                              age = faction == Faction.Ally ? 17 : 27,
                                              mbti = faction == Faction.Ally ? "ENTP" : "ISTJ",
                                              elementName = "Snow",
                                              weaponName = attackRange > 1 ? "Spear" : "Sword",
                                              speechTone = "battle test",
                                              maxHp = maxHp,
                                              maxInner = maxInner,
                                              initiative = initiative,
                                              agility = agility,
                                              moveRange = moveRange,
                                              attackRange = attackRange,
                                              attackBonus = attackBonus,
                                              defense = defense,
                                              damageMin = damageMin,
                                              damageMax = damageMax,
                                              specialName = specialName,
                                              specialRange = Mathf.Max(attackRange, 1),
                                              specialCost = specialEffect == BattleSpecialEffect.Mark ? 0 : 1,
                                              specialCooldown = 2,
                                              specialPower = Mathf.Max(4, damageMin),
                                              specialAttackBonus = 1,
                                              specialEffect = specialEffect };
    }

    private static void PaintTile(Tilemap tilemap, Vector2Int cell, TileBase tile, Color color)
    {
        if (tilemap == null || tile == null)
        {
            return;
        }

        Tile visualTile = tile as Tile;
        if (visualTile == null || visualTile.sprite == null)
        {
            return;
        }

        GameObject spriteObject = new GameObject($"PaintedCell_{cell.x}_{cell.y}_{visualTile.name}");
        spriteObject.transform.SetParent(tilemap.transform, false);
        spriteObject.transform.position = CellToWorld(cell);
        spriteObject.transform.localScale = Vector3.one;

        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = visualTile.sprite;
        renderer.color = color;
        renderer.sortingLayerName = "Default";

        TilemapRenderer tilemapRenderer = tilemap.GetComponent<TilemapRenderer>();
        int baseOrder = tilemapRenderer == null ? 0 : tilemapRenderer.sortingOrder;
        renderer.sortingOrder = (baseOrder * 10) + ((cell.x + cell.y) * 2);
    }

    private static SpriteRenderer CreateBackdropSprite(Transform parent, string name, Sprite sprite, Vector3 position,
                                                       Vector3 scale, float rotation, Color color, int sortingOrder)
    {
        GameObject spriteObject = new GameObject(name);
        spriteObject.transform.SetParent(parent, false);
        spriteObject.transform.position = position;
        spriteObject.transform.localScale = scale;
        spriteObject.transform.rotation = Quaternion.Euler(0f, 0f, rotation);
        SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return renderer;
    }

    private static InteractableEffectType EffectFor(InteractableKind kind)
    {
        switch (kind)
        {
        case InteractableKind.IncenseBurner:
            return InteractableEffectType.CreateSmoke;
        case InteractableKind.Lantern:
        case InteractableKind.OilJar:
            return InteractableEffectType.CreateFire;
        case InteractableKind.WoodenBridge:
            return InteractableEffectType.CollapseBridge;
        case InteractableKind.BambooBundle:
            return InteractableEffectType.BlockSight;
        case InteractableKind.RockLantern:
            return InteractableEffectType.Push;
        default:
            return InteractableEffectType.CreateCover;
        }
    }

    private static CharacterVisualData LoadVisual(string fileName)
    {
        string path = "Assets/JoseonMurimTactics/Art/Characters/VisualData/" + fileName;
        return AssetDatabase.LoadAssetAtPath<CharacterVisualData>(path);
    }

    private static Sprite LoadSprite(string relativePath)
    {
        Sprite sprite = Resources.Load<Sprite>("MapAssets/" + relativePath);
        if (sprite == null)
        {
            Debug.LogWarning("[BattleMapDioramaSceneBuilder] Missing sprite: MapAssets/" + relativePath);
        }

        return sprite;
    }

    private static Vector3 CellToWorld(Vector2Int cell)
    {
        float x = (cell.x - cell.y) * TileWidth * 0.5f;
        float y = (cell.x + cell.y) * TileHeight * 0.5f;
        return new Vector3(x, y, 0f);
    }

    private static bool Inside(Vector2Int cell)
    {
        return cell.x >= 0 && cell.y >= 0 && cell.x < Width && cell.y < Height;
    }

    private static IEnumerable<Vector2Int> Neighbors(Vector2Int cell)
    {
        yield return new Vector2Int(cell.x + 1, cell.y);
        yield return new Vector2Int(cell.x - 1, cell.y);
        yield return new Vector2Int(cell.x, cell.y + 1);
        yield return new Vector2Int(cell.x, cell.y - 1);
    }

    private static bool HasCliffDrop(CellSpec spec)
    {
        return spec.northEdge == EdgeType.CliffDrop || spec.eastEdge == EdgeType.CliffDrop ||
               spec.southEdge == EdgeType.CliffDrop || spec.westEdge == EdgeType.CliffDrop;
    }

    private static Color Vary(Color color, Vector2Int cell, float amount)
    {
        float variation = (((cell.x * 37 + cell.y * 19) % 11) - 5) * amount;
        return new Color(Mathf.Clamp01(color.r + variation), Mathf.Clamp01(color.g + variation),
                         Mathf.Clamp01(color.b + variation), color.a);
    }

    private static void EnsureFolder(string folder)
    {
        string[] parts = folder.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
            {
                AssetDatabase.CreateFolder(current, parts[i]);
            }

            current = next;
        }
    }

    private sealed class CellSpec
    {
        public TerrainType terrain;
        public string tileKey;
        public string variantKey;
        public int variantSalt;
        public int elevation;
        public int moveCost = 1;
        public bool walkable = true;
        public bool blocksMovement;
        public bool blocksLineOfSight;
        public bool isChokePoint;
        public int capacity = 1;
        public CoverType coverType;
        public HazardType hazardType;
        public EdgeType northEdge;
        public EdgeType eastEdge;
        public EdgeType southEdge;
        public EdgeType westEdge;
        public string zoneId;
        public string laneId;
        public string note;
    }
}
}
#endif
