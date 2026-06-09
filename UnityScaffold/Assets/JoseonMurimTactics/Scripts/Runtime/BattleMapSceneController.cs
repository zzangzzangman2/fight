using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleMapSceneController : MonoBehaviour
{
    [SerializeField] private string mapId = "baekdu_snowgate_v1";
    [SerializeField] private string displayName = "Baekdu Snow Gate";
    [SerializeField] private bool authoredProductionMap = true;
    [SerializeField] private BattleMapTilemapBinder binder;
    [SerializeField] private Vector2Int origin;
    [SerializeField] private Vector2Int size = new Vector2Int(20, 14);
    [SerializeField] private float tileWidth = 1.16f;
    [SerializeField] private float tileHeight = 0.62f;
    [SerializeField] private Color cameraBackground = new Color(0.22f, 0.34f, 0.32f, 1f);
    [SerializeField] private bool generateBaekduOverlayAtRuntime = true;

    public string MapId => mapId;
    public string DisplayName => displayName;
    public bool AuthoredProductionMap => authoredProductionMap;
    public BattleMapTilemapBinder Binder => binder;
    public Vector2Int Origin => origin;
    public Vector2Int Size => size;
    public float TileWidth => tileWidth;
    public float TileHeight => tileHeight;
    public IReadOnlyList<TacticalGridCellData> Cells =>
        binder == null || binder.TacticalOverlay == null ? System.Array.Empty<TacticalGridCellData>() : binder.TacticalOverlay.Cells;

    public void ConfigureAuthoredScene(string newMapId, string newDisplayName, BattleMapTilemapBinder newBinder,
                                       Vector2Int newOrigin, Vector2Int newSize, float newTileWidth,
                                       float newTileHeight)
    {
        mapId = newMapId;
        displayName = newDisplayName;
        authoredProductionMap = true;
        binder = newBinder;
        origin = newOrigin;
        size = newSize;
        tileWidth = newTileWidth;
        tileHeight = newTileHeight;
    }

    private void Awake()
    {
        InitializeRuntime();
    }

    public void InitializeRuntime()
    {
        binder = binder == null ? GetComponentInChildren<BattleMapTilemapBinder>() : binder;
        if (binder == null)
        {
            binder = gameObject.AddComponent<BattleMapTilemapBinder>();
        }

        binder.EnsureStructure(tileWidth, tileHeight);
        if (generateBaekduOverlayAtRuntime && binder.TacticalOverlay != null && binder.TacticalOverlay.Cells.Count == 0)
        {
            GenerateBaekduOverlay();
        }

        if (binder.TacticalOverlay == null || binder.TacticalOverlay.Cells.Count == 0)
        {
            binder.SyncTacticalOverlayFromVisualTilemaps();
        }

        origin = binder.TacticalOverlay == null ? binder.Origin : binder.TacticalOverlay.Origin;
        size = binder.TacticalOverlay == null ? binder.Size : binder.TacticalOverlay.Size;
        if (size.x <= 1 || size.y <= 1)
        {
            origin = Vector2Int.zero;
            size = new Vector2Int(20, 14);
            binder.ConfigureRuntime(origin, size, tileWidth, tileHeight);
        }

        ApplyCameraDefaults();
        EnsureLightRig();
    }

    private void GenerateBaekduOverlay()
    {
        binder.TacticalOverlay.Configure(Vector2Int.zero, new Vector2Int(20, 14));
        for (int y = 0; y < 14; y++)
        {
            for (int x = 0; x < 20; x++)
            {
                binder.TacticalOverlay.SetCell(CreateBaekduCell(new Vector2Int(x, y)));
            }
        }
    }

    private TacticalGridCellData CreateBaekduCell(Vector2Int cell)
    {
        TerrainType terrain = TerrainType.Snow;
        int elevation = cell.y >= 8 ? 1 : 0;
        int moveCost = 1;
        bool walkable = true;
        bool blocksMovement = false;
        bool blocksLineOfSight = false;
        bool choke = false;
        CoverType cover = CoverType.None;
        HazardType hazard = HazardType.None;
        EdgeType north = EdgeType.None;
        EdgeType east = EdgeType.None;
        EdgeType south = EdgeType.None;
        EdgeType west = EdgeType.None;
        string zone = string.Empty;
        string lane = "canyon_floor";
        string note = "Open snow courtyard.";
        int x = cell.x;
        int y = cell.y;

        if (x <= 4 && y >= 3 && y <= 11)
        {
            terrain = x <= 2 || y >= 8 ? TerrainType.Forest : TerrainType.Bamboo;
            elevation = y >= 8 ? 1 : 0;
            moveCost = 2;
            cover = x <= 2 ? CoverType.Light : CoverType.Heavy;
            blocksLineOfSight = true;
            choke = (x == 3 && (y == 6 || y == 7)) || (x == 2 && y == 9);
            lane = "left_forest_flank";
            note = "Left forest flank: slow cover and sight breaks.";
        }
        else if (y == 5)
        {
            terrain = TerrainType.DeepWater;
            moveCost = 99;
            walkable = false;
            blocksMovement = true;
            hazard = HazardType.DeepWater;
            north = east = south = west = EdgeType.WaterBank;
            lane = "frozen_stream";
            note = "Deep frozen stream blocks direct movement.";

            if (x >= 8 && x <= 10)
            {
                terrain = TerrainType.Bridge;
                elevation = 1;
                moveCost = 1;
                walkable = true;
                blocksMovement = false;
                hazard = HazardType.Collapse;
                choke = true;
                north = south = EdgeType.BridgeRail;
                east = west = EdgeType.None;
                lane = "central_bridge_choke";
                note = "Central bridge bottleneck.";
            }
            else if ((x >= 2 && x <= 3) || (x >= 15 && x <= 16))
            {
                terrain = TerrainType.ShallowWater;
                moveCost = 3;
                walkable = true;
                blocksMovement = false;
                hazard = HazardType.Slippery;
                lane = x < 10 ? "left_ford" : "right_ford";
                note = "Frozen ford: slow exposed crossing.";
            }
        }
        else if (x >= 7 && x <= 11 && y <= 4)
        {
            terrain = TerrainType.Road;
            elevation = 0;
            choke = x == 9 && y >= 3;
            lane = "south_approach";
            note = "Southern approach road into the pass.";
        }
        else if (x >= 7 && x <= 11 && y >= 6 && y <= 10)
        {
            terrain = y >= 9 ? TerrainType.ShrineFloor : TerrainType.Road;
            elevation = Mathf.Min(2, y - 5);
            cover = y >= 9 ? CoverType.Light : CoverType.None;
            choke = y <= 8 && x >= 8 && x <= 10;
            lane = "central_stair_choke";
            note = "Raised stair path through the Snow Gate.";
        }
        else if (x >= 7 && x <= 12 && y >= 11)
        {
            terrain = y >= 12 ? TerrainType.Gate : TerrainType.ShrineFloor;
            elevation = 2;
            cover = CoverType.Light;
            zone = x >= 9 && x <= 10 && y >= 12 ? "objective" : string.Empty;
            lane = "north_gate_shrine";
            note = "Gate shrine objective plateau.";
        }
        else if ((x == 6 || x == 12) && y >= 6 && y <= 10)
        {
            terrain = TerrainType.Cliff;
            elevation = 2;
            moveCost = 99;
            walkable = false;
            blocksMovement = true;
            blocksLineOfSight = true;
            hazard = HazardType.Fall;
            north = east = south = west = EdgeType.CliffDrop;
            lane = "central_cliff_face";
            note = "Basalt cliff face divides the central pass.";
        }
        else if (x >= 13 && y >= 6 && y <= 12)
        {
            terrain = y >= 10 ? TerrainType.Hill : TerrainType.Cliff;
            elevation = y >= 10 ? 3 : 2;
            moveCost = 2;
            cover = CoverType.Light;
            lane = "right_cliff_highground";
            note = "Right cliff high ground with fall edges.";
            if (x == 13 || y == 6 || (x >= 17 && y <= 8))
            {
                west = EdgeType.CliffDrop;
                south = EdgeType.CliffDrop;
                hazard = HazardType.Fall;
                choke = x == 13 && y >= 7 && y <= 8;
            }
        }
        else if (x >= 15 && y <= 4)
        {
            terrain = x >= 17 && y <= 2 ? TerrainType.Ice : TerrainType.ShallowWater;
            moveCost = terrain == TerrainType.Ice ? 2 : 3;
            hazard = HazardType.Slippery;
            north = east = south = west = EdgeType.WaterBank;
            lane = "right_icy_shoal";
            note = "Right icy shoal under the cliff.";
        }
        else if (x <= 5 && y <= 2)
        {
            terrain = TerrainType.Forest;
            moveCost = 2;
            cover = CoverType.Light;
            blocksLineOfSight = x <= 2;
            lane = "southwest_pines";
            note = "Pine cover on the southern approach.";
        }
        else if (x >= 5 && x <= 13 && y >= 6 && y <= 9)
        {
            terrain = TerrainType.Rubble;
            elevation = 1;
            moveCost = 2;
            cover = (x + y) % 2 == 0 ? CoverType.Heavy : CoverType.Light;
            blocksLineOfSight = x == 11 && y >= 8;
            lane = "broken_courtyard";
            note = "Broken courtyard cover near the gate.";
        }
        else if (y >= 12 && (x <= 6 || x >= 13))
        {
            terrain = TerrainType.Wall;
            elevation = 2;
            moveCost = 99;
            walkable = false;
            blocksMovement = true;
            blocksLineOfSight = true;
            hazard = HazardType.Fall;
            lane = "north_wall";
            note = "Snow-covered gate wall.";
        }

        return new TacticalGridCellData
        {
            cell = cell,
            displayName = terrain.ToString(),
            worldPosition = CellToWorld(cell),
            terrainType = terrain,
            moveCost = Mathf.Max(1, moveCost),
            walkable = walkable,
            blocksMovement = blocksMovement,
            blocksLineOfSight = blocksLineOfSight,
            isChokePoint = choke,
            capacity = choke ? 1 : 2,
            elevation = elevation,
            coverType = cover,
            hazardType = hazard,
            northEdge = north,
            eastEdge = east,
            southEdge = south,
            westEdge = west,
            zoneId = zone,
            laneId = lane,
            visualTileKey = terrain.ToString(),
            decorSetKey = note,
        };
    }

    public bool TryGetCell(Vector2Int cell, out TacticalGridCellData data)
    {
        if (binder == null || binder.TacticalOverlay == null)
        {
            data = null;
            return false;
        }

        return binder.TacticalOverlay.TryGetCell(cell, out data);
    }

    public IEnumerable<MapPropView> InteractiveProps()
    {
        MapPropView[] props = GetComponentsInChildren<MapPropView>(true);
        foreach (MapPropView prop in props)
        {
            if (prop != null && prop.interactive)
            {
                yield return prop;
            }
        }
    }

    public Vector3 CellToWorld(Vector2Int cell)
    {
        if (binder == null || binder.Grid == null)
        {
            float x = (cell.x - cell.y) * tileWidth * 0.5f;
            float y = (cell.x + cell.y) * tileHeight * 0.5f;
            return new Vector3(x, y, 0f);
        }

        return binder.Grid.CellToWorld(new Vector3Int(cell.x, cell.y, 0));
    }

    private void ApplyCameraDefaults()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return;
        }

        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = cameraBackground;
    }

    private void EnsureLightRig()
    {
        Transform root = binder == null || binder.LightsRoot == null ? transform : binder.LightsRoot;
        if (root.GetComponentInChildren<Light2D>(true) != null)
        {
            return;
        }

        GameObject global = new GameObject("Global Light 2D - Snow Dawn");
        global.transform.SetParent(root, false);
        Light2D globalLight = global.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.78f, 0.86f, 0.94f, 1f);
        globalLight.intensity = 0.78f;

        CreatePointLight(root, "Gate Lantern Warm Pool", new Vector2Int(8, 11), new Color(1f, 0.68f, 0.32f, 1f),
                         2.8f, 0.55f);
        CreatePointLight(root, "Frozen Stream Bounce", new Vector2Int(12, 5), new Color(0.42f, 0.70f, 0.86f, 1f),
                         2.4f, 0.36f);
        CreatePointLight(root, "Right Cliff Rim Light", new Vector2Int(16, 9), new Color(0.88f, 0.82f, 0.56f, 1f),
                         2.2f, 0.32f);
    }

    private void CreatePointLight(Transform root, string lightName, Vector2Int cell, Color color, float radius,
                                  float intensity)
    {
        GameObject lightObject = new GameObject(lightName);
        lightObject.transform.SetParent(root, false);
        lightObject.transform.position = CellToWorld(cell) + new Vector3(0f, 0.22f, -0.20f);

        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.32f;
    }
}
}
