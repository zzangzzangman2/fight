using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics
{
public enum BattleMapHighlightLayer
{
    Move,
    Attack,
    Danger,
    PathArrow
}

[DisallowMultipleComponent]
public sealed class BattleMapTilemapBinder : MonoBehaviour
{
    [SerializeField] private Grid grid;
    [Header("Authored Diorama Layers")]
    [SerializeField] private Tilemap backdropBaseTilemap;
    [SerializeField] private Tilemap backdropDistantTilemap;
    [SerializeField] private Tilemap groundBaseTilemap;
    [SerializeField] private Tilemap groundVariationTilemap;
    [SerializeField] private Tilemap roadPathTilemap;
    [SerializeField] private Tilemap roadEdgeTilemap;
    [SerializeField] private Tilemap cliffTopTilemap;
    [SerializeField] private Tilemap cliffFaceTilemap;
    [SerializeField] private Tilemap cliffEdgeTilemap;
    [SerializeField] private Tilemap waterBaseTilemap;
    [SerializeField] private Tilemap waterSurfaceTilemap;
    [SerializeField] private Tilemap decorGroundTilemap;
    [SerializeField] private Tilemap decorGrassRockSnowTilemap;
    [SerializeField] private Tilemap propsBehindUnitsTilemap;
    [SerializeField] private Tilemap propsFrontOfUnitsTilemap;
    [SerializeField] private Tilemap shadowAoTilemap;
    [SerializeField] private Tilemap fogMistTilemap;
    [SerializeField] private Tilemap gridSubtleTilemap;

    [Header("Legacy Runtime Aliases")]
    [SerializeField] private Tilemap groundTilemap;
    [SerializeField] private Tilemap roadTilemap;
    [SerializeField] private Tilemap waterTilemap;
    [SerializeField] private Tilemap cliffTilemap;
    [SerializeField] private Tilemap decorTilemap;
    [SerializeField] private Tilemap propsTilemap;
    [SerializeField] private Tilemap overlayTilemap;
    [SerializeField] private Tilemap highlightMoveTilemap;
    [SerializeField] private Tilemap highlightAttackTilemap;
    [SerializeField] private Tilemap highlightDangerTilemap;
    [SerializeField] private Tilemap highlightPathArrowTilemap;
    [SerializeField] private Transform propsRoot;
    [SerializeField] private Transform lightsRoot;
    [SerializeField] private TacticalGridOverlay tacticalGridOverlay;
    [SerializeField] private BattleMapData battleMapData;
    [SerializeField] private Vector2Int origin;
    [SerializeField] private Vector2Int size = new Vector2Int(16, 12);

    public Grid Grid => grid;
    public Tilemap BackdropBaseTilemap => backdropBaseTilemap;
    public Tilemap BackdropDistantTilemap => backdropDistantTilemap;
    public Tilemap GroundBaseTilemap => groundBaseTilemap;
    public Tilemap GroundVariationTilemap => groundVariationTilemap;
    public Tilemap RoadPathTilemap => roadPathTilemap;
    public Tilemap RoadEdgeTilemap => roadEdgeTilemap;
    public Tilemap CliffTopTilemap => cliffTopTilemap;
    public Tilemap CliffFaceTilemap => cliffFaceTilemap;
    public Tilemap CliffEdgeTilemap => cliffEdgeTilemap;
    public Tilemap WaterBaseTilemap => waterBaseTilemap;
    public Tilemap WaterSurfaceTilemap => waterSurfaceTilemap;
    public Tilemap DecorGroundTilemap => decorGroundTilemap;
    public Tilemap DecorGrassRockSnowTilemap => decorGrassRockSnowTilemap;
    public Tilemap PropsBehindUnitsTilemap => propsBehindUnitsTilemap;
    public Tilemap PropsFrontOfUnitsTilemap => propsFrontOfUnitsTilemap;
    public Tilemap ShadowAoTilemap => shadowAoTilemap;
    public Tilemap FogMistTilemap => fogMistTilemap;
    public Tilemap GridSubtleTilemap => gridSubtleTilemap;
    public Tilemap GroundTilemap => groundBaseTilemap == null ? groundTilemap : groundBaseTilemap;
    public Tilemap RoadTilemap => roadPathTilemap == null ? roadTilemap : roadPathTilemap;
    public Tilemap WaterTilemap => waterBaseTilemap == null ? waterTilemap : waterBaseTilemap;
    public Tilemap CliffTilemap => cliffTopTilemap == null ? cliffTilemap : cliffTopTilemap;
    public Tilemap DecorTilemap => decorGrassRockSnowTilemap == null ? decorTilemap : decorGrassRockSnowTilemap;
    public Tilemap PropsTilemap => propsFrontOfUnitsTilemap == null ? propsTilemap : propsFrontOfUnitsTilemap;
    public Tilemap OverlayTilemap => gridSubtleTilemap == null ? overlayTilemap : gridSubtleTilemap;
    public Tilemap HighlightMoveTilemap => highlightMoveTilemap;
    public Tilemap HighlightAttackTilemap => highlightAttackTilemap;
    public Tilemap HighlightDangerTilemap => highlightDangerTilemap;
    public Tilemap HighlightPathArrowTilemap => highlightPathArrowTilemap;
    public Transform PropsRoot => propsRoot;
    public Transform LightsRoot => lightsRoot;
    public TacticalGridOverlay TacticalOverlay => tacticalGridOverlay;
    public BattleMapData BattleMapData => battleMapData;
    public Vector2Int Origin => origin;
    public Vector2Int Size => size;
    public bool HasAuthoredProductionLayers => groundBaseTilemap != null && groundVariationTilemap != null &&
                                               roadPathTilemap != null && cliffFaceTilemap != null &&
                                               waterBaseTilemap != null && propsFrontOfUnitsTilemap != null;

    public void ConfigureRuntime(Vector2Int newOrigin, Vector2Int newSize, float tileWidth, float tileHeight)
    {
        origin = newOrigin;
        size = newSize;
        EnsureStructure(tileWidth, tileHeight);
        tacticalGridOverlay.Configure(origin, size);
    }

    public void EnsureStructure(float tileWidth = -1f, float tileHeight = -1f)
    {
        grid = grid == null ? GetComponent<Grid>() : grid;
        if (grid == null)
        {
            grid = gameObject.AddComponent<Grid>();
        }

        grid.cellLayout = GridLayout.CellLayout.Isometric;
        if (tileWidth > 0f && tileHeight > 0f)
        {
            grid.cellSize = new Vector3(tileWidth, tileHeight, 1f);
        }
        else if (grid.cellSize == Vector3.zero)
        {
            grid.cellSize = new Vector3(1f, 0.5f, 1f);
        }

        backdropBaseTilemap = EnsureTilemap("Tilemap_Backdrop_Base", backdropBaseTilemap, -140);
        backdropDistantTilemap = EnsureTilemap("Tilemap_Backdrop_Distant", backdropDistantTilemap, -130);
        groundBaseTilemap = EnsureTilemap("Tilemap_Ground_Base", groundBaseTilemap == null ? groundTilemap : groundBaseTilemap, 0);
        groundVariationTilemap = EnsureTilemap("Tilemap_Ground_Variation", groundVariationTilemap, 4);
        roadPathTilemap = EnsureTilemap("Tilemap_Road_Path", roadPathTilemap == null ? roadTilemap : roadPathTilemap, 18);
        roadEdgeTilemap = EnsureTilemap("Tilemap_Road_Edge", roadEdgeTilemap, 19);
        waterBaseTilemap = EnsureTilemap("Tilemap_Water_Base", waterBaseTilemap == null ? waterTilemap : waterBaseTilemap, 10);
        waterSurfaceTilemap = EnsureTilemap("Tilemap_Water_Surface", waterSurfaceTilemap, 11);
        cliffFaceTilemap = EnsureTilemap("Tilemap_Cliff_Face", cliffFaceTilemap, 28);
        cliffTopTilemap = EnsureTilemap("Tilemap_Cliff_Top", cliffTopTilemap == null ? cliffTilemap : cliffTopTilemap, 40);
        cliffEdgeTilemap = EnsureTilemap("Tilemap_Cliff_Edge", cliffEdgeTilemap, 44);
        decorGroundTilemap = EnsureTilemap("Tilemap_Decor_Ground", decorGroundTilemap == null ? decorTilemap : decorGroundTilemap, 60);
        decorGrassRockSnowTilemap = EnsureTilemap("Tilemap_Decor_GrassRockSnow", decorGrassRockSnowTilemap, 70);
        propsBehindUnitsTilemap = EnsureTilemap("Tilemap_Props_BehindUnits", propsBehindUnitsTilemap, 82);
        propsFrontOfUnitsTilemap = EnsureTilemap("Tilemap_Props_FrontOfUnits", propsFrontOfUnitsTilemap == null ? propsTilemap : propsFrontOfUnitsTilemap, 96);
        shadowAoTilemap = EnsureTilemap("Tilemap_Shadow_AO", shadowAoTilemap, 104);
        fogMistTilemap = EnsureTilemap("Tilemap_Fog_Mist", fogMistTilemap, 118);
        gridSubtleTilemap = EnsureTilemap("Tilemap_Grid_Subtle", gridSubtleTilemap == null ? overlayTilemap : gridSubtleTilemap, 120);
        highlightMoveTilemap = EnsureTilemap("Tilemap_Highlight_Move", highlightMoveTilemap, 160);
        highlightAttackTilemap = EnsureTilemap("Tilemap_Highlight_Attack", highlightAttackTilemap, 161);
        highlightDangerTilemap = EnsureTilemap("Tilemap_Highlight_Danger", highlightDangerTilemap, 162);
        highlightPathArrowTilemap = EnsureTilemap("Tilemap_Highlight_PathArrow", highlightPathArrowTilemap, 163);

        groundTilemap = groundBaseTilemap;
        roadTilemap = roadPathTilemap;
        waterTilemap = waterBaseTilemap;
        cliffTilemap = cliffTopTilemap;
        decorTilemap = decorGrassRockSnowTilemap;
        propsTilemap = propsFrontOfUnitsTilemap;
        overlayTilemap = gridSubtleTilemap;

        propsRoot = EnsureChild("PropsRoot", propsRoot);
        lightsRoot = EnsureChild("LightsRoot", lightsRoot);
        tacticalGridOverlay = EnsureTacticalOverlay();
    }

    public void SyncTacticalOverlayFromVisualTilemaps()
    {
        EnsureStructure();
        BoundsInt bounds = ResolveTacticalBounds();
        origin = new Vector2Int(bounds.xMin, bounds.yMin);
        size = new Vector2Int(bounds.size.x, bounds.size.y);
        tacticalGridOverlay.Configure(origin, size);

        for (int y = bounds.yMin; y < bounds.yMax; y++)
        {
            for (int x = bounds.xMin; x < bounds.xMax; x++)
            {
                Vector3Int tileCell = new Vector3Int(x, y, 0);
                TerrainTileData terrainTile = ResolveTerrainTile(tileCell);
                tacticalGridOverlay.SetCell(CreateCellData(tileCell, terrainTile));
            }
        }

        if (battleMapData != null)
        {
            tacticalGridOverlay.CopyTo(battleMapData);
        }
    }

    public Tilemap TilemapForHighlight(BattleMapHighlightLayer layer)
    {
        switch (layer)
        {
        case BattleMapHighlightLayer.Attack:
            return highlightAttackTilemap;
        case BattleMapHighlightLayer.Danger:
            return highlightDangerTilemap;
        case BattleMapHighlightLayer.PathArrow:
            return highlightPathArrowTilemap;
        default:
            return highlightMoveTilemap;
        }
    }

    public Tilemap[] PaintedTilemaps()
    {
        return new[] {
            backdropBaseTilemap,
            backdropDistantTilemap,
            groundBaseTilemap,
            groundVariationTilemap,
            roadPathTilemap,
            roadEdgeTilemap,
            waterBaseTilemap,
            waterSurfaceTilemap,
            cliffFaceTilemap,
            cliffTopTilemap,
            cliffEdgeTilemap,
            decorGroundTilemap,
            decorGrassRockSnowTilemap,
            propsBehindUnitsTilemap,
            propsFrontOfUnitsTilemap,
            shadowAoTilemap,
            fogMistTilemap,
            gridSubtleTilemap,
        };
    }

    private Tilemap EnsureTilemap(string layerName, Tilemap existing, int sortingOrder)
    {
        if (existing == null)
        {
            Transform child = transform.Find(layerName);
            existing = child == null ? null : child.GetComponent<Tilemap>();
            if (existing == null && child != null)
            {
                existing = child.gameObject.AddComponent<Tilemap>();
            }
        }

        if (existing == null)
        {
            GameObject layerObject = new GameObject(layerName);
            layerObject.transform.SetParent(transform, false);
            existing = layerObject.AddComponent<Tilemap>();
        }

        existing.tileAnchor = Vector3.zero;
        TilemapRenderer renderer = existing.GetComponent<TilemapRenderer>();
        if (renderer == null)
        {
            renderer = existing.gameObject.AddComponent<TilemapRenderer>();
        }

        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
        return existing;
    }

    private Transform EnsureChild(string childName, Transform existing)
    {
        if (existing != null)
        {
            return existing;
        }

        Transform child = transform.Find(childName);
        if (child != null)
        {
            return child;
        }

        GameObject childObject = new GameObject(childName);
        childObject.transform.SetParent(transform, false);
        return childObject.transform;
    }

    private TacticalGridOverlay EnsureTacticalOverlay()
    {
        if (tacticalGridOverlay != null)
        {
            return tacticalGridOverlay;
        }

        Transform overlayTransform = transform.Find("TacticalGridOverlay");
        if (overlayTransform == null)
        {
            GameObject overlayObject = new GameObject("TacticalGridOverlay");
            overlayObject.transform.SetParent(transform, false);
            overlayTransform = overlayObject.transform;
        }

        TacticalGridOverlay overlay = overlayTransform.GetComponent<TacticalGridOverlay>();
        return overlay == null ? overlayTransform.gameObject.AddComponent<TacticalGridOverlay>() : overlay;
    }

    private BoundsInt ResolveTacticalBounds()
    {
        int minX = origin.x;
        int minY = origin.y;
        int maxX = origin.x + Mathf.Max(1, size.x);
        int maxY = origin.y + Mathf.Max(1, size.y);
        bool foundTile = false;

        ExpandBounds(groundBaseTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(groundVariationTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(roadPathTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(roadEdgeTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(waterBaseTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(waterSurfaceTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(cliffTopTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(cliffFaceTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(cliffEdgeTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(decorGroundTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);
        ExpandBounds(decorGrassRockSnowTilemap, ref minX, ref minY, ref maxX, ref maxY, ref foundTile);

        if (!foundTile)
        {
            return new BoundsInt(origin.x, origin.y, 0, Mathf.Max(1, size.x), Mathf.Max(1, size.y), 1);
        }

        return new BoundsInt(minX, minY, 0, Mathf.Max(1, maxX - minX), Mathf.Max(1, maxY - minY), 1);
    }

    private static void ExpandBounds(Tilemap tilemap, ref int minX, ref int minY, ref int maxX, ref int maxY,
                                     ref bool foundTile)
    {
        if (tilemap == null || tilemap.GetUsedTilesCount() == 0)
        {
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        if (!foundTile)
        {
            minX = bounds.xMin;
            minY = bounds.yMin;
            maxX = bounds.xMax;
            maxY = bounds.yMax;
            foundTile = true;
            return;
        }

        minX = Mathf.Min(minX, bounds.xMin);
        minY = Mathf.Min(minY, bounds.yMin);
        maxX = Mathf.Max(maxX, bounds.xMax);
        maxY = Mathf.Max(maxY, bounds.yMax);
    }

    private TerrainTileData ResolveTerrainTile(Vector3Int cell)
    {
        TerrainTileData terrainTile = TileDataAt(roadPathTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(cliffTopTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(cliffEdgeTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(cliffFaceTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(waterSurfaceTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(waterBaseTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(groundVariationTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(groundBaseTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(roadTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(cliffTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(waterTilemap, cell);
        if (terrainTile != null)
        {
            return terrainTile;
        }

        terrainTile = TileDataAt(groundTilemap, cell);
        return terrainTile == null ? TileDataAt(decorTilemap, cell) : terrainTile;
    }

    private static TerrainTileData TileDataAt(Tilemap tilemap, Vector3Int cell)
    {
        return tilemap == null ? null : tilemap.GetTile<TerrainTileData>(cell);
    }

    private TacticalGridCellData CreateCellData(Vector3Int tileCell, TerrainTileData terrainTile)
    {
        Vector2Int cell = new Vector2Int(tileCell.x, tileCell.y);
        TerrainType terrainType = terrainTile == null ? TerrainType.Stone : terrainTile.terrainType;
        bool walkable = terrainTile == null || terrainTile.walkable;
        return new TacticalGridCellData
        {
            cell = cell,
            displayName = terrainType.ToString(),
            worldPosition = grid == null ? Vector3.zero : grid.CellToWorld(tileCell),
            terrainType = terrainType,
            moveCost = terrainTile == null ? 1 : Mathf.Max(1, terrainTile.moveCost),
            walkable = walkable,
            blocksMovement = terrainTile != null && terrainTile.blocksMovement,
            blocksLineOfSight = terrainTile != null && terrainTile.blocksLineOfSight,
            isChokePoint = terrainTile != null && terrainTile.isChokePoint,
            capacity = terrainTile == null ? 1 : Mathf.Max(1, terrainTile.capacity),
            elevation = terrainTile == null ? 0 : terrainTile.elevation,
            coverType = terrainTile == null ? CoverType.None : terrainTile.coverType,
            hazardType = terrainTile == null ? HazardType.None : terrainTile.hazardType,
            northEdge = terrainTile == null ? EdgeType.None : terrainTile.northEdge,
            eastEdge = terrainTile == null ? EdgeType.None : terrainTile.eastEdge,
            southEdge = terrainTile == null ? EdgeType.None : terrainTile.southEdge,
            westEdge = terrainTile == null ? EdgeType.None : terrainTile.westEdge,
            zoneId = terrainTile == null ? string.Empty : terrainTile.zoneId,
            laneId = terrainTile == null ? string.Empty : terrainTile.laneId,
            visualTileKey = terrainType.ToString(),
            decorSetKey = string.Empty,
        };
    }
}
}
