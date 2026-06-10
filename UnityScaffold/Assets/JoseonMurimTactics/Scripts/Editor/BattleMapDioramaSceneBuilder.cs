#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
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
    public const int Width = BaekduSnowGateLayout.Width;
    public const int Height = BaekduSnowGateLayout.Height;
    private const float TileWidth = BaekduSnowGateLayout.TileWidth;
    private const float TileHeight = BaekduSnowGateLayout.TileHeight;
    private const string TileAssetFolder = "Assets/JoseonMurimTactics/Art/BattleMaps/Tilesets/DioramaGenerated";

    [MenuItem("Joseon Murim Tactics/Rebuild Baekdu SnowGate v1.9 Diorama")]
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

    public static void RenderBaekduSnowGateScreenshots()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        BattleMapSceneController controller = UnityEngine.Object.FindAnyObjectByType<BattleMapSceneController>();
        if (controller != null)
        {
            controller.InitializeRuntime();
        }

        Camera camera = Camera.main != null ? Camera.main : UnityEngine.Object.FindAnyObjectByType<Camera>();
        if (camera == null)
        {
            throw new InvalidOperationException("Battle map screenshot requires a camera.");
        }

        camera.orthographic = true;
        camera.orthographicSize = 7.25f;
        camera.transform.position = new Vector3(2.40f, 5.95f, -10f);

        const string screenshotFolder = "Assets/JoseonMurimTactics/Art/BattleMaps/Screenshots";
        EnsureFolder(screenshotFolder);
        RenderCameraToPng(camera, screenshotFolder + "/baekdu_snowgate_v1_overview.png", 1600, 1000);
        AssetDatabase.Refresh();
        Debug.Log("[BattleMapDioramaSceneBuilder] Rendered Baekdu screenshot.");
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
        camera.backgroundColor = new Color(0.52f, 0.62f, 0.58f, 1f);
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
                PaintTile(binder.GroundBaseTilemap, cell, VisualTileFor(snowBase, visualTiles),
                          Vary(BaseGroundTint(spec), cell, 0.010f));

                TerrainTileData tacticalTile = ResolveTacticalTile(spec, cell, tiles, variants);
                PaintTile(PrimaryLayerFor(binder, spec), cell, VisualTileFor(tacticalTile, visualTiles),
                          Vary(TacticalTint(spec), cell, 0.012f));
                PaintTransitions(binder, cell, spec, tiles, visualTiles, variants);
                PaintAtmosphere(binder, cell, spec, tiles, visualTiles);
                PaintComfortWash(binder, cell, spec, tiles, visualTiles);
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
                      new Color(0.92f, 0.94f, 0.88f, 0.48f));
        }

        if (spec.terrain == TerrainType.ShallowWater || spec.terrain == TerrainType.DeepWater ||
            spec.terrain == TerrainType.Ice)
        {
            PaintTile(binder.WaterSurfaceTilemap, cell,
                      VisualTileFor(PickPattern(variants["ice_surface"], cell, 7), visualTiles),
                      new Color(0.76f, 0.90f, 0.96f, 0.58f));
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
                              new Color(0.90f, 0.95f, 0.92f, 0.40f));
                }
            }
        }

        if (HasCliffDrop(spec))
        {
            PaintTile(binder.CliffEdgeTilemap, cell, VisualTileFor(tiles["cliff_edge"], visualTiles),
                      new Color(0.82f, 0.86f, 0.80f, 0.74f));
            PaintTile(binder.ShadowAoTilemap, cell, VisualTileFor(tiles["shadow_ao"], visualTiles),
                      new Color(0.10f, 0.09f, 0.08f, 0.18f));
            PaintCliffSkirt(binder, cell, spec, tiles);
        }

        if (spec.coverType != CoverType.None || spec.blocksLineOfSight)
        {
            PaintTile(binder.DecorGrassRockSnowTilemap, cell,
                      VisualTileFor(PickPattern(variants["snow_decor"], cell, 11), visualTiles),
                      new Color(0.76f, 0.86f, 0.72f, 0.34f));
        }
    }

    private static void PaintAtmosphere(BattleMapTilemapBinder binder, Vector2Int cell, CellSpec spec,
                                        Dictionary<string, TerrainTileData> tiles, Dictionary<string, Tile> visualTiles)
    {
        if (cell.y <= 1 || spec.terrain == TerrainType.ShallowWater || spec.terrain == TerrainType.DeepWater)
        {
            PaintTile(binder.FogMistTilemap, cell, VisualTileFor(tiles["fog_mist"], visualTiles),
                      new Color(0.78f, 0.90f, 0.88f, 0.20f));
        }

        if ((cell.x + (cell.y * 2)) % 11 == 0)
        {
            PaintTile(binder.GridSubtleTilemap, cell, VisualTileFor(tiles["grid_subtle"], visualTiles),
                      new Color(0.86f, 0.96f, 0.92f, 0.004f));
        }
    }

    private static void PaintComfortWash(BattleMapTilemapBinder binder, Vector2Int cell, CellSpec spec,
                                         Dictionary<string, TerrainTileData> tiles, Dictionary<string, Tile> visualTiles)
    {
        if (!tiles.TryGetValue("soft_wash", out TerrainTileData wash))
        {
            return;
        }

        Color color = new Color(0.94f, 0.99f, 0.95f, spec.walkable ? 0.075f : 0.035f);
        if (spec.terrain == TerrainType.Road || spec.terrain == TerrainType.Bridge)
        {
            color = new Color(0.98f, 0.92f, 0.78f, 0.090f);
        }
        else if (spec.terrain == TerrainType.Forest || spec.terrain == TerrainType.Bamboo)
        {
            color = new Color(0.78f, 0.94f, 0.72f, 0.070f);
        }
        else if (spec.terrain == TerrainType.ShallowWater || spec.terrain == TerrainType.DeepWater ||
                 spec.terrain == TerrainType.Ice)
        {
            color = new Color(0.78f, 0.94f, 1f, 0.085f);
        }
        else if (spec.zoneId == "objective" || spec.terrain == TerrainType.Gate)
        {
            color = new Color(1f, 0.84f, 0.44f, 0.080f);
        }
        else if (spec.elevation >= 3)
        {
            color = new Color(0.90f, 0.96f, 0.88f, 0.060f);
        }

        PaintTile(binder.FogMistTilemap, cell, VisualTileFor(wash, visualTiles), Vary(color, cell, 0.003f));
    }

    private static void PaintCliffSkirt(BattleMapTilemapBinder binder, Vector2Int cell, CellSpec spec,
                                        Dictionary<string, TerrainTileData> tiles)
    {
        if (binder == null || binder.CliffFaceTilemap == null || !tiles.TryGetValue("cliff_face_b", out TerrainTileData face) ||
            face == null || face.sprite == null)
        {
            return;
        }

        int layers = Mathf.Clamp(spec.elevation, 1, 4);
        for (int i = 0; i < layers; i++)
        {
            GameObject spriteObject = new GameObject($"CliffFaceSkirt_{cell.x}_{cell.y}_{i}");
            spriteObject.transform.SetParent(binder.CliffFaceTilemap.transform, false);
            spriteObject.transform.position = CellToWorld(cell) + new Vector3(0f, -0.20f - (i * 0.17f), 0.04f);
            spriteObject.transform.localScale = new Vector3(1.08f - (i * 0.03f), 0.72f, 1f);

            SpriteRenderer renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = face.sprite;
            renderer.color = new Color(0.50f - (i * 0.030f), 0.56f - (i * 0.025f), 0.55f - (i * 0.020f), 0.78f);
            renderer.sortingLayerName = "Default";
            renderer.sortingOrder = 90 + ((cell.x + cell.y) * 2) - i;
        }
    }

    private static Color BaseGroundTint(CellSpec spec)
    {
        float lift = spec.elevation * 0.012f;
        return new Color(0.94f + lift, 0.98f + lift, 0.95f + lift, 0.18f);
    }

    private static Color TacticalTint(CellSpec spec)
    {
        switch (spec.terrain)
        {
        case TerrainType.Road:
            return new Color(0.98f, 0.95f, 0.84f, 0.82f);
        case TerrainType.Bridge:
            return new Color(0.94f, 0.80f, 0.60f, 0.84f);
        case TerrainType.Forest:
        case TerrainType.Bamboo:
            return new Color(0.84f, 0.96f, 0.78f, 0.80f);
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
        case TerrainType.Ice:
            return new Color(0.84f, 0.98f, 1f, 0.82f);
        case TerrainType.ShrineFloor:
        case TerrainType.Gate:
            return new Color(0.96f, 0.92f, 0.78f, 0.84f);
        case TerrainType.Cliff:
        case TerrainType.Hill:
        case TerrainType.Wall:
        case TerrainType.Rubble:
            return new Color(0.88f, 0.92f, 0.86f, 0.80f);
        default:
            return new Color(0.96f, 1f, 0.96f, 0.78f);
        }
    }

    private static CellSpec ResolveCell(int x, int y)
    {
        BaekduCellSpec layout = BaekduSnowGateLayout.Resolve(new Vector2Int(x, y));
        return new CellSpec
        {
            terrain = layout.terrain,
            tileKey = layout.tileKey,
            variantKey = string.Empty,
            variantSalt = 0,
            elevation = layout.elevation,
            moveCost = layout.moveCost,
            walkable = layout.walkable,
            blocksMovement = layout.blocksMovement,
            blocksLineOfSight = layout.blocksLineOfSight,
            isChokePoint = layout.isChokePoint,
            capacity = layout.capacity,
            coverType = layout.coverType,
            hazardType = layout.hazardType,
            northEdge = layout.northEdge,
            eastEdge = layout.eastEdge,
            southEdge = layout.southEdge,
            westEdge = layout.westEdge,
            zoneId = layout.zoneId,
            laneId = layout.laneId,
            note = layout.note
        };
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
        AddTile(tiles, "soft_wash", TerrainType.Snow, "Tiles/baekdu_snow_plain", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        tiles["soft_wash"].sprite = LoadOrCreateComfortWashSprite();
        EditorUtility.SetDirty(tiles["soft_wash"]);
        AddTile(tiles, "snow_e0_d", TerrainType.Snow, "Tiles/snow_edge", 1, true, false, false, 0, CoverType.None,
                HazardType.None);
        AddTile(tiles, "snow_e0_e", TerrainType.Snow, "Tiles/baekdu_volcanic_snow_rock", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "snow_e0_f", TerrainType.Snow, "Tiles/baekdu_snow_plain", 1, true, false, false, 0,
                CoverType.None, HazardType.None);

        AddTile(tiles, "forest_e0_c", TerrainType.Forest, "Tiles/snow_edge", 2, true, true, false, 0,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e0_d", TerrainType.Forest, "Tiles/baekdu_snow_mountain_pass", 2, true, true, false, 0,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e1_c", TerrainType.Forest, "Tiles/snow_edge", 2, true, true, false, 1,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e1_d", TerrainType.Forest, "Tiles/baekdu_snow_mountain_pass", 2, true, true, false, 1,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e2", TerrainType.Forest, "Tiles/baekdu_snow_pine_floor", 2, true, true, false, 2,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e2_b", TerrainType.Forest, "Tiles/forest_floor", 2, true, true, false, 2,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e2_c", TerrainType.Forest, "Tiles/snow_edge", 2, true, true, false, 2,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "forest_e2_d", TerrainType.Forest, "Tiles/baekdu_snow_mountain_pass", 2, true, true, false, 2,
                CoverType.Light, HazardType.None);

        AddTile(tiles, "bamboo_e0_c", TerrainType.Bamboo, "Tiles/forest_floor", 3, true, true, false, 0,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e0_d", TerrainType.Bamboo, "Tiles/snow_edge", 3, true, true, false, 0,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e1_c", TerrainType.Bamboo, "Tiles/forest_floor", 3, true, true, false, 1,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e1_d", TerrainType.Bamboo, "Tiles/snow_edge", 3, true, true, false, 1,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e2", TerrainType.Bamboo, "Tiles/baekdu_snow_bamboo_floor", 3, true, true, false, 2,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e2_b", TerrainType.Bamboo, "Tiles/bamboo_floor", 3, true, true, false, 2,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e2_c", TerrainType.Bamboo, "Tiles/forest_floor", 3, true, true, false, 2,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "bamboo_e2_d", TerrainType.Bamboo, "Tiles/snow_edge", 3, true, true, false, 2,
                CoverType.Heavy, HazardType.None);

        AddTile(tiles, "road_e0_c", TerrainType.Road, "Tiles/snow_edge", 1, true, false, false, 0, CoverType.None,
                HazardType.None);
        AddTile(tiles, "road_e0_d", TerrainType.Road, "Tiles/baekdu_snow_shrine_floor", 1, true, false, false, 0,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e1_c", TerrainType.Road, "Tiles/snow_edge", 1, true, false, true, 1, CoverType.None,
                HazardType.None);
        AddTile(tiles, "road_e1_d", TerrainType.Road, "Tiles/baekdu_snow_shrine_floor", 1, true, false, true, 1,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e2_c", TerrainType.Road, "Tiles/snow_edge", 1, true, false, true, 2, CoverType.None,
                HazardType.None);
        AddTile(tiles, "road_e2_d", TerrainType.Road, "Tiles/baekdu_snow_shrine_floor", 1, true, false, true, 2,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e3", TerrainType.Road, "Tiles/baekdu_frozen_stair_road", 1, true, false, true, 3,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e3_b", TerrainType.Road, "Tiles/road_stair", 1, true, false, true, 3,
                CoverType.None, HazardType.None);
        AddTile(tiles, "road_e3_c", TerrainType.Road, "Tiles/snow_edge", 1, true, false, true, 3, CoverType.None,
                HazardType.None);
        AddTile(tiles, "road_e3_d", TerrainType.Road, "Tiles/baekdu_snow_shrine_floor", 1, true, false, true, 3,
                CoverType.None, HazardType.None);

        AddTile(tiles, "bridge_e1_b", TerrainType.Bridge, "Tiles/wood_plank", 1, true, false, true, 1,
                CoverType.None, HazardType.Collapse, EdgeType.BridgeRail, EdgeType.BridgeRail, EdgeType.BridgeRail,
                EdgeType.BridgeRail, string.Empty, "central_bridge_choke");
        AddTile(tiles, "bridge_e1_c", TerrainType.Bridge, "Tiles/road_stair", 1, true, false, true, 1,
                CoverType.None, HazardType.Collapse, EdgeType.BridgeRail, EdgeType.BridgeRail, EdgeType.BridgeRail,
                EdgeType.BridgeRail, string.Empty, "central_bridge_choke");
        AddTile(tiles, "bridge_e1_d", TerrainType.Bridge, "Tiles/wood_bridge", 1, true, false, true, 1,
                CoverType.None, HazardType.Collapse, EdgeType.BridgeRail, EdgeType.BridgeRail, EdgeType.BridgeRail,
                EdgeType.BridgeRail, string.Empty, "central_bridge_choke");

        AddTile(tiles, "shrine_e3", TerrainType.ShrineFloor, "Tiles/baekdu_snow_shrine_floor", 1, true, false, false,
                3, CoverType.Light, HazardType.None);
        AddTile(tiles, "shrine_e3_b", TerrainType.ShrineFloor, "Tiles/shrine_floor", 1, true, false, false, 3,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "shrine_e3_c", TerrainType.ShrineFloor, "Tiles/stone_courtyard", 1, true, false, false, 3,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "shrine_e3_d", TerrainType.ShrineFloor, "Tiles/snow_edge", 1, true, false, false, 3,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "gate_e4", TerrainType.Gate, "Tiles/gate_threshold", 1, true, false, false, 4, CoverType.Light,
                HazardType.None, EdgeType.Gate, EdgeType.Gate, EdgeType.None, EdgeType.None, "objective",
                "north_gate_shrine");
        AddTile(tiles, "gate_e4_b", TerrainType.Gate, "Tiles/wall_broken", 1, true, false, false, 4,
                CoverType.Light, HazardType.None, EdgeType.Gate, EdgeType.Gate, EdgeType.None, EdgeType.None,
                "objective", "north_gate_shrine");
        AddTile(tiles, "gate_e4_c", TerrainType.Gate, "Tiles/baekdu_snow_shrine_floor", 1, true, false, false, 4,
                CoverType.Light, HazardType.None, EdgeType.Gate, EdgeType.Gate, EdgeType.None, EdgeType.None,
                "objective", "north_gate_shrine");
        AddTile(tiles, "gate_e4_d", TerrainType.Gate, "Tiles/road_stair", 1, true, false, false, 4,
                CoverType.Light, HazardType.None, EdgeType.Gate, EdgeType.Gate, EdgeType.None, EdgeType.None,
                "objective", "north_gate_shrine");

        AddTile(tiles, "rubble_e1_c", TerrainType.Rubble, "Tiles/wall_broken", 2, true, true, false, 1,
                CoverType.Heavy, HazardType.None);
        AddTile(tiles, "rubble_e1_d", TerrainType.Rubble, "Tiles/snow_edge", 2, true, true, false, 1,
                CoverType.Light, HazardType.None);
        AddTile(tiles, "cliff_face_b", TerrainType.Cliff, "Tiles/cliff_face", 99, false, true, false, 2,
                CoverType.None, HazardType.Fall, EdgeType.CliffDrop, EdgeType.CliffDrop, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "central_cliff_face");
        AddTile(tiles, "cliff_face_c", TerrainType.Cliff, "Tiles/baekdu_volcanic_snow_rock", 99, false, true, false,
                2, CoverType.None, HazardType.Fall, EdgeType.CliffDrop, EdgeType.CliffDrop, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "central_cliff_face");
        AddTile(tiles, "cliff_face_d", TerrainType.Cliff, "Tiles/wall_broken", 99, false, true, false, 2,
                CoverType.None, HazardType.Fall, EdgeType.CliffDrop, EdgeType.CliffDrop, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "central_cliff_face");

        AddTile(tiles, "ridge_e1", TerrainType.Cliff, "Tiles/baekdu_snow_basalt_cliff", 3, true, false, false, 1,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e1_b", TerrainType.Cliff, "Tiles/cliff_face", 3, true, false, false, 1,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e1_c", TerrainType.Cliff, "Tiles/baekdu_volcanic_snow_rock", 3, true, false, false, 1,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e1_d", TerrainType.Cliff, "Tiles/snow_edge", 3, true, false, false, 1,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e2_c", TerrainType.Cliff, "Tiles/baekdu_volcanic_snow_rock", 2, true, false, false, 2,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e2_d", TerrainType.Cliff, "Tiles/snow_edge", 2, true, false, false, 2,
                CoverType.Light, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e3_c", TerrainType.Hill, "Tiles/baekdu_snow_basalt_cliff", 2, true, false, false, 3,
                CoverType.Heavy, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e3_d", TerrainType.Hill, "Tiles/snow_edge", 2, true, false, false, 3,
                CoverType.Heavy, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, string.Empty, "right_cliff_highground");
        AddTile(tiles, "ridge_e4", TerrainType.Hill, "Tiles/baekdu_wind_snow_ridge", 2, true, false, false, 4,
                CoverType.Heavy, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, "beacon", "beacon_peak");
        AddTile(tiles, "ridge_e4_b", TerrainType.Hill, "Tiles/baekdu_volcanic_snow_rock", 2, true, false, false, 4,
                CoverType.Heavy, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, "beacon", "beacon_peak");
        AddTile(tiles, "ridge_e4_c", TerrainType.Hill, "Tiles/baekdu_snow_basalt_cliff", 2, true, false, false, 4,
                CoverType.Heavy, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, "beacon", "beacon_peak");
        AddTile(tiles, "ridge_e4_d", TerrainType.Hill, "Tiles/snow_edge", 2, true, false, false, 4,
                CoverType.Heavy, HazardType.Fall, EdgeType.None, EdgeType.None, EdgeType.CliffDrop,
                EdgeType.CliffDrop, "beacon", "beacon_peak");

        AddTile(tiles, "wall_e3", TerrainType.Wall, "Tiles/wall_broken", 99, false, true, false, 3, CoverType.None,
                HazardType.Fall);
        AddTile(tiles, "wall_e3_b", TerrainType.Wall, "Tiles/gate_threshold", 99, false, true, false, 3,
                CoverType.None, HazardType.Fall);
        AddTile(tiles, "wall_e4", TerrainType.Wall, "Tiles/wall_broken", 99, false, true, false, 4, CoverType.None,
                HazardType.Fall);
        AddTile(tiles, "wall_e4_b", TerrainType.Wall, "Tiles/baekdu_snow_basalt_cliff", 99, false, true, false, 4,
                CoverType.None, HazardType.Fall);
        AddTile(tiles, "ice_slick_b", TerrainType.Ice, "Tiles/ice_slick", 2, true, false, false, 0, CoverType.None,
                HazardType.Ice);
        AddTile(tiles, "ice_slick_c", TerrainType.Ice, "Tiles/baekdu_cracked_ice_hazard", 2, true, false, false, 0,
                CoverType.None, HazardType.Ice);
        AddTile(tiles, "shallow_water_d", TerrainType.ShallowWater, "Tiles/baekdu_ice_slick", 3, true, false, false,
                0, CoverType.None, HazardType.Slippery, EdgeType.WaterBank, EdgeType.WaterBank,
                EdgeType.WaterBank, EdgeType.WaterBank, string.Empty, "frozen_stream");
        AddTile(tiles, "dark_water_b", TerrainType.DeepWater, "Tiles/deep_water", 99, false, false, false, 0,
                CoverType.None, HazardType.DeepWater);
        ApplyEyeComfortFloorSprites(tiles);
        return tiles;
    }

    private static void ApplyEyeComfortFloorSprites(Dictionary<string, TerrainTileData> tiles)
    {
        foreach (KeyValuePair<string, TerrainTileData> entry in tiles)
        {
            TerrainTileData tile = entry.Value;
            if (tile == null || !ShouldUseEyeComfortSprite(entry.Key, tile))
            {
                continue;
            }

            tile.sprite = LoadOrCreateEyeComfortTerrainSprite(entry.Key, tile.terrainType, tile.elevation);
            EditorUtility.SetDirty(tile);
        }
    }

    private static bool ShouldUseEyeComfortSprite(string key, TerrainTileData tile)
    {
        if (key.StartsWith("cliff_face", StringComparison.OrdinalIgnoreCase) ||
            key == "cliff_edge" || key == "shadow_ao" || key == "fog_mist" || key == "grid_subtle" ||
            key == "soft_wash")
        {
            return false;
        }

        if (tile.terrainType == TerrainType.Wall)
        {
            return false;
        }

        return tile.terrainType == TerrainType.Snow || tile.terrainType == TerrainType.Road ||
               tile.terrainType == TerrainType.Bridge || tile.terrainType == TerrainType.Forest ||
               tile.terrainType == TerrainType.Bamboo || tile.terrainType == TerrainType.ShallowWater ||
               tile.terrainType == TerrainType.DeepWater || tile.terrainType == TerrainType.Ice ||
               tile.terrainType == TerrainType.ShrineFloor || tile.terrainType == TerrainType.Gate ||
               tile.terrainType == TerrainType.Cliff || tile.terrainType == TerrainType.Hill ||
               tile.terrainType == TerrainType.Rubble;
    }

    private static Sprite LoadOrCreateEyeComfortTerrainSprite(string key, TerrainType terrain, int elevation)
    {
        string group = EyeComfortGroup(terrain);
        int variant = StableKeyHash(key) % 4;
        string folder = TileAssetFolder + "/ComfortV2";
        EnsureFolder(folder);
        string assetPath = folder + "/" + group + "_" + variant + ".png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        CreateEyeComfortTerrainTexture(assetPath, terrain, elevation, variant);
        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void CreateEyeComfortTerrainTexture(string assetPath, TerrainType terrain, int elevation, int variant)
    {
        const int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = Path.GetFileNameWithoutExtension(assetPath);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Color baseColor = EyeComfortBaseColor(terrain, elevation, variant);
        Color coolLight = Color.Lerp(baseColor, Color.white, 0.22f);
        Color coolShade = Color.Lerp(baseColor, new Color(0.30f, 0.36f, 0.34f, 1f), 0.16f);
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        int salt = StableKeyHash(assetPath) + variant * 97;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x + 0.5f - center.x) / center.x);
                float ny = Mathf.Abs((y + 0.5f - center.y) / center.y);
                float diamond = nx + ny;
                if (diamond > 1.015f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float edgeAlpha = diamond <= 0.992f ? 1f : Mathf.Clamp01((1.006f - diamond) * 82f);
                float edgeShade = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((diamond - 0.62f) * 2.6f));
                float noise = Hash01(x / 13, y / 13, salt);
                float grain = Hash01(x / 37, y / 19, salt + 23);
                Color color = Color.Lerp(coolLight, coolShade, edgeShade * 0.20f + noise * 0.10f);
                color = ApplyTerrainFlecks(color, terrain, grain, noise);
                color.a = edgeAlpha * 0.96f;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 512f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }
    }

    private static Color ApplyTerrainFlecks(Color color, TerrainType terrain, float grain, float noise)
    {
        if (terrain == TerrainType.ShallowWater || terrain == TerrainType.DeepWater || terrain == TerrainType.Ice)
        {
            return grain > 0.86f ? Color.Lerp(color, new Color(0.88f, 1f, 1f, 1f), 0.24f) : color;
        }

        if (terrain == TerrainType.Forest || terrain == TerrainType.Bamboo)
        {
            return grain > 0.80f ? Color.Lerp(color, new Color(0.46f, 0.66f, 0.42f, 1f), 0.18f) : color;
        }

        if (terrain == TerrainType.Road || terrain == TerrainType.ShrineFloor || terrain == TerrainType.Gate)
        {
            return grain > 0.82f ? Color.Lerp(color, new Color(0.74f, 0.68f, 0.56f, 1f), 0.16f) : color;
        }

        if (terrain == TerrainType.Cliff || terrain == TerrainType.Hill || terrain == TerrainType.Rubble)
        {
            return grain > 0.78f ? Color.Lerp(color, new Color(0.58f, 0.62f, 0.58f, 1f), 0.18f) : color;
        }

        return noise > 0.82f ? Color.Lerp(color, Color.white, 0.18f) : color;
    }

    private static string EyeComfortGroup(TerrainType terrain)
    {
        switch (terrain)
        {
        case TerrainType.Road:
        case TerrainType.Bridge:
            return "road";
        case TerrainType.Forest:
        case TerrainType.Bamboo:
            return "forest";
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
        case TerrainType.Ice:
            return "ice";
        case TerrainType.ShrineFloor:
        case TerrainType.Gate:
            return "shrine";
        case TerrainType.Cliff:
        case TerrainType.Hill:
        case TerrainType.Rubble:
            return "ridge";
        default:
            return "snow";
        }
    }

    private static Color EyeComfortBaseColor(TerrainType terrain, int elevation, int variant)
    {
        float lift = elevation * 0.018f + variant * 0.010f;
        switch (terrain)
        {
        case TerrainType.Road:
        case TerrainType.Bridge:
            return new Color(0.74f + lift, 0.70f + lift, 0.58f + lift, 1f);
        case TerrainType.Forest:
        case TerrainType.Bamboo:
            return new Color(0.58f + lift, 0.70f + lift, 0.54f + lift, 1f);
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
        case TerrainType.Ice:
            return new Color(0.48f + lift, 0.72f + lift, 0.78f + lift, 1f);
        case TerrainType.ShrineFloor:
        case TerrainType.Gate:
            return new Color(0.76f + lift, 0.72f + lift, 0.60f + lift, 1f);
        case TerrainType.Cliff:
        case TerrainType.Hill:
        case TerrainType.Rubble:
            return new Color(0.58f + lift, 0.62f + lift, 0.58f + lift, 1f);
        default:
            return new Color(0.72f + lift, 0.80f + lift, 0.74f + lift, 1f);
        }
    }

    private static int StableKeyHash(string key)
    {
        unchecked
        {
            int hash = 17;
            for (int i = 0; i < key.Length; i++)
            {
                hash = hash * 31 + key[i];
            }

            return Mathf.Abs(hash);
        }
    }

    private static float Hash01(int x, int y, int salt)
    {
        unchecked
        {
            int hash = x * 73856093 ^ y * 19349663 ^ salt * 83492791;
            hash ^= hash << 13;
            hash ^= hash >> 17;
            hash ^= hash << 5;
            return (hash & 0x7fffffff) / (float)int.MaxValue;
        }
    }

    private static Dictionary<string, TerrainVariantSet> BuildVariantSets(Dictionary<string, TerrainTileData> tiles)
    {
        Dictionary<string, TerrainVariantSet> variants = new Dictionary<string, TerrainVariantSet>();
        AddVariantSet(variants, "snow_base", tiles["snow_e0"], tiles["snow_e0_b"], tiles["snow_e0_c"],
                      tiles["snow_e0_d"], tiles["snow_e0_e"], tiles["snow_e0_f"]);
        AddVariantSet(variants, "snow_edge", tiles["snow_edge"], tiles["snow_e0_b"], tiles["snow_decor"],
                      tiles["snow_e0_d"]);
        AddVariantSet(variants, "snow_decor", tiles["snow_decor"], tiles["snow_edge"], tiles["snow_e0_c"],
                      tiles["snow_e0_e"]);
        AddVariantSet(variants, "forest_e0", tiles["forest_e0"], tiles["forest_e0_b"], tiles["forest_e0_c"],
                      tiles["forest_e0_d"]);
        AddVariantSet(variants, "forest_e1", tiles["forest_e1"], tiles["forest_e1_b"], tiles["forest_e1_c"],
                      tiles["forest_e1_d"]);
        AddVariantSet(variants, "forest_e2", tiles["forest_e2"], tiles["forest_e2_b"], tiles["forest_e2_c"],
                      tiles["forest_e2_d"]);
        AddVariantSet(variants, "bamboo_e0", tiles["bamboo_e0"], tiles["bamboo_e0_b"], tiles["bamboo_e0_c"],
                      tiles["bamboo_e0_d"]);
        AddVariantSet(variants, "bamboo_e1", tiles["bamboo_e1"], tiles["bamboo_e1_b"], tiles["bamboo_e1_c"],
                      tiles["bamboo_e1_d"]);
        AddVariantSet(variants, "bamboo_e2", tiles["bamboo_e2"], tiles["bamboo_e2_b"], tiles["bamboo_e2_c"],
                      tiles["bamboo_e2_d"]);
        AddVariantSet(variants, "road_e0", tiles["road_e0"], tiles["road_e0_b"], tiles["road_e0_c"],
                      tiles["road_e0_d"]);
        AddVariantSet(variants, "road_e1", tiles["road_e1"], tiles["road_e1_b"], tiles["road_e1_c"],
                      tiles["road_e1_d"]);
        AddVariantSet(variants, "road_e2", tiles["road_e2"], tiles["road_e2_b"], tiles["road_e2_c"],
                      tiles["road_e2_d"]);
        AddVariantSet(variants, "road_e3", tiles["road_e3"], tiles["road_e3_b"], tiles["road_e3_c"],
                      tiles["road_e3_d"]);
        AddVariantSet(variants, "shrine_e2", tiles["shrine_e2"], tiles["shrine_e2_b"]);
        AddVariantSet(variants, "shrine_e3", tiles["shrine_e3"], tiles["shrine_e3_b"], tiles["shrine_e3_c"],
                      tiles["shrine_e3_d"]);
        AddVariantSet(variants, "ridge_e1", tiles["ridge_e1"], tiles["ridge_e1_b"], tiles["ridge_e1_c"],
                      tiles["ridge_e1_d"]);
        AddVariantSet(variants, "ridge_e2", tiles["ridge_e2"], tiles["ridge_e2_b"], tiles["ridge_e2_c"],
                      tiles["ridge_e2_d"]);
        AddVariantSet(variants, "ridge_e3", tiles["ridge_e3"], tiles["ridge_e3_b"], tiles["ridge_e3_c"],
                      tiles["ridge_e3_d"]);
        AddVariantSet(variants, "ridge_e4", tiles["ridge_e4"], tiles["ridge_e4_b"], tiles["ridge_e4_c"],
                      tiles["ridge_e4_d"]);
        AddVariantSet(variants, "rubble_e1", tiles["rubble_e1"], tiles["rubble_e1_b"], tiles["rubble_e1_c"],
                      tiles["rubble_e1_d"]);
        AddVariantSet(variants, "ice_surface", tiles["shallow_water"], tiles["shallow_water_b"],
                      tiles["shallow_water_c"], tiles["shallow_water_d"], tiles["ice_slick"], tiles["ice_slick_b"]);
        AddVariantSet(variants, "dark_water", tiles["deep_water"], tiles["deep_water_b"], tiles["dark_water"],
                      tiles["dark_water_b"]);
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
        CreateBackdropSprite(root, "Backdrop_Canyon_Depth_Shadow", LoadSprite("Tiles/baekdu_snow_mountain_pass"),
                             center + new Vector3(0f, -0.40f, 0.22f), new Vector3(17.0f, 10.2f, 1f), 45f,
                             new Color(0.16f, 0.23f, 0.22f, 0.34f), -235);
        CreateBackdropSprite(root, "Backdrop_Snow_Sky_Wash", LoadSprite("Tiles/baekdu_snow_mountain_pass"),
                             center + new Vector3(0f, 1.2f, 0.20f), new Vector3(16.8f, 9.4f, 1f), 45f,
                             new Color(0.62f, 0.74f, 0.70f, 0.42f), -225);
        CreateBackdropSprite(root, "Backdrop_Valley_Fog_Floor", LoadSprite("Tiles/smoke_veil"),
                             center + new Vector3(0.2f, -1.25f, 0.12f), new Vector3(11.8f, 3.4f, 1f), -8f,
                             new Color(0.78f, 0.86f, 0.82f, 0.44f), -218);
        CreateBackdropSprite(root, "Backdrop_Distant_Baekdu_Ridge", LoadSprite("Tiles/baekdu_wind_snow_ridge"),
                             center + new Vector3(-1.7f, 4.1f, 0.16f), new Vector3(6.4f, 2.2f, 1f), 0f,
                             new Color(0.44f, 0.56f, 0.54f, 0.62f), -210);
        CreateBackdropSprite(root, "Backdrop_Far_Right_Ridge", LoadSprite("Tiles/baekdu_snow_basalt_cliff"),
                             center + new Vector3(3.9f, 3.3f, 0.15f), new Vector3(5.8f, 1.7f, 1f), -7f,
                             new Color(0.32f, 0.44f, 0.42f, 0.42f), -208);
        CreateBackdropSprite(root, "Backdrop_Map_EyeComfort_Wash", LoadOrCreateComfortWashSprite(),
                             center + new Vector3(0.05f, 0.10f, -0.06f), new Vector3(13.8f, 8.8f, 1f), 0f,
                             new Color(0.90f, 0.96f, 0.91f, 0.07f), 119);
        CreateBackdropSprite(root, "Backdrop_Pine_Left", LoadSprite("Objects/baekdu_snow_pine"),
                             CellToWorld(new Vector2Int(0, 10)) + new Vector3(-1.4f, 0.45f, 0.10f),
                             new Vector3(1.45f, 1.45f, 1f), -4f, new Color(0.50f, 0.66f, 0.58f, 0.72f), -120);
        CreateBackdropSprite(root, "Backdrop_Pine_Right", LoadSprite("Objects/baekdu_snow_pine"),
                             CellToWorld(new Vector2Int(19, 6)) + new Vector3(1.1f, 0.35f, 0.10f),
                             new Vector3(-1.30f, 1.30f, 1f), 5f, new Color(0.46f, 0.62f, 0.54f, 0.64f), -118);
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
        CreateProp(binder, "powder_cart", "Powder cart", InteractableKind.WineCart, new Vector2Int(6, 7),
                   "Objects/wine_cart", true, CoverType.Light, false, true, true);
        CreateProp(binder, "beacon_brazier", "Beacon brazier", InteractableKind.Beacon, new Vector2Int(18, 12),
                   "Objects/red_lantern", true, CoverType.Light, false, true, true);
        CreateProp(binder, "cliff_ladder", "Cliff ladder", InteractableKind.Ladder, new Vector2Int(13, 7),
                   "Objects/baekdu_frozen_rope_posts", true, CoverType.Light, false, true, false);
        CreateProp(binder, "fallen_wall", "Fallen shrine wall", InteractableKind.FallenWall, new Vector2Int(11, 8),
                   "Objects/fallen_wall", true, CoverType.Heavy, true, true, false);
        CreateProp(binder, "gate_rope_posts_left", "Broken gate posts", InteractableKind.Ladder, new Vector2Int(8, 12),
                   "Objects/baekdu_frozen_rope_posts", false, CoverType.Light, true, false, false);
        CreateProp(binder, "gate_rope_posts_right", "Broken gate posts", InteractableKind.Ladder, new Vector2Int(11, 12),
                   "Objects/baekdu_frozen_rope_posts", false, CoverType.Light, true, false, false);
        CreateProp(binder, "left_snow_pine_b", "Snow pine", InteractableKind.BambooBundle, new Vector2Int(1, 6),
                   "Objects/baekdu_snow_pine", false, CoverType.Heavy, true, false, false);
        CreateProp(binder, "right_ridge_warning_posts", "Cliff warning posts", InteractableKind.Ladder,
                   new Vector2Int(13, 9), "Objects/baekdu_frozen_rope_posts", true, CoverType.Light, false, true,
                   false);
        CreateProp(binder, "allied_snowdrift_cover", "Allied snowdrift cover", InteractableKind.FallenWall,
                   new Vector2Int(5, 1), "Objects/baekdu_snowdrift_cover", false, CoverType.Heavy, false, false,
                   false);
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
            Unit("shin_seoa", "Shin Seoa", Faction.Ally, "shin_seoa_visual.asset", new Vector2Int(5, 2), 24, 5, 13,
                 14, 5, 3, 5, 13, 4, 7, "Flower Wind", BattleSpecialEffect.Mark),
            Unit("han_biyeon", "Han Biyeon", Faction.Ally, "han_biyeon_visual.asset", new Vector2Int(4, 3), 27, 4,
                 16, 17, 5, 3, 6, 13, 4, 8, "Poison Needle", BattleSpecialEffect.Poison),
            Unit("iron_wolf_guard_1", "Iron Wolf Guard", Faction.Enemy,
                 "do_arin_visual.asset", new Vector2Int(9, 10), 30, 3, 12, 12, 4, 1, 5, 14,
                 5, 8, "Iron Slash", BattleSpecialEffect.Strike),
            Unit("iron_wolf_spear_1", "Iron Wolf Spear", Faction.Enemy,
                 "baek_ryeon_visual.asset", new Vector2Int(11, 11), 32, 3, 11, 12, 4, 2, 5,
                 15, 5, 9, "Wolf Spear", BattleSpecialEffect.BreakGuard),
            Unit("iron_wolf_captain", "Iron Wolf Captain", Faction.Enemy,
                 "jin_seoyul_visual.asset", new Vector2Int(14, 10), 38, 4, 13, 13, 4, 1, 7,
                 16, 6, 11, "Pack Order", BattleSpecialEffect.Mark),
            Unit("ridge_archer", "Ridge Archer", Faction.Enemy, "han_biyeon_visual.asset",
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

    private static void RenderCameraToPng(Camera camera, string assetPath, int width, int height)
    {
        RenderTexture previousTarget = camera.targetTexture;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture texture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D output = new Texture2D(width, height, TextureFormat.RGBA32, false);
        try
        {
            camera.targetTexture = texture;
            RenderTexture.active = texture;
            camera.Render();
            output.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            output.Apply();
            File.WriteAllBytes(assetPath, output.EncodeToPNG());
        }
        finally
        {
            camera.targetTexture = previousTarget;
            RenderTexture.active = previousActive;
            UnityEngine.Object.DestroyImmediate(output);
            texture.Release();
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static InteractableEffectType EffectFor(InteractableKind kind)
    {
        switch (kind)
        {
        case InteractableKind.IncenseBurner:
            return InteractableEffectType.CreateSmoke;
        case InteractableKind.Lantern:
        case InteractableKind.OilJar:
        case InteractableKind.WineCart:
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
        const string characterRoot = "Assets/JoseonMurimTactics/Art/Characters/";
        List<string> candidates = new List<string>();
        string assetName = Path.GetFileName(fileName);

        if (fileName.Contains("/"))
        {
            candidates.Add(characterRoot + fileName);
        }

        if (assetName.EndsWith("_visual.asset", StringComparison.Ordinal))
        {
            string id = Path.GetFileNameWithoutExtension(assetName).Replace("_visual", string.Empty);
            candidates.Add(characterRoot + id + "/VisualData/" + assetName);
        }

        candidates.Add(characterRoot + "VisualData/" + fileName);

        foreach (string path in candidates)
        {
            CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(path);
            if (visual != null)
            {
                return visual;
            }
        }

        Debug.LogWarning("[BattleMapDioramaSceneBuilder] Missing character visual: " + fileName);
        return null;
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

    private static Sprite LoadOrCreateComfortWashSprite()
    {
        const string assetPath = TileAssetFolder + "/soft_wash_sprite.png";
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        const int size = 512;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BaekduSoftWashSprite";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = Mathf.Abs((x + 0.5f - center.x) / center.x);
                float ny = Mathf.Abs((y + 0.5f - center.y) / center.y);
                float diamond = nx + ny;
                float edge = Mathf.Clamp01((1.02f - diamond) * 16f);
                float centerGlow = Mathf.Clamp01(1f - diamond * 0.72f);
                float alpha = edge * (0.66f + centerGlow * 0.24f);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        File.WriteAllBytes(assetPath, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);

        TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 512f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static Vector3 CellToWorld(Vector2Int cell)
    {
        return BaekduSnowGateLayout.CellToWorld(cell);
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
