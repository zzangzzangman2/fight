using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleTilemapBattlefield : MonoBehaviour
{
    // Floor sprites use (rows - (x+y)) * RowStride + slot. Floor slots stay below 26,
    // props use 28, units 30. Highlights sit on 26: above the floor of
    // their own row, clipped by nearer rows, under standing units and props.
    private const int RowStride = 40;
    private const int HighlightSlot = 26;

    private readonly Dictionary<string, TerrainTileData> terrainTiles = new Dictionary<string, TerrainTileData>();
    private readonly Dictionary<TerrainType, Tilemap> terrainLayerLookup = new Dictionary<TerrainType, Tilemap>();
    private readonly Dictionary<Vector2Int, SpriteRenderer> highlightCells =
        new Dictionary<Vector2Int, SpriteRenderer>();

    private Grid grid;
    private Tile overlayTile;
    private Sprite fallbackDiamond;
    private Sprite softDiamond;
    private Sprite detailSprite;
    private Sprite dotSprite;
    private Sprite highlightCellSprite;
    private Transform highlightRoot;
    private int highlightRowCount = 33;
    private BattleMapTilemapBinder binder;

    public BattleMapTilemapBinder Binder => binder;
    public Tilemap GroundTilemap { get; private set; }
    public Tilemap RoadTilemap { get; private set; }
    public Tilemap CliffTilemap { get; private set; }
    public Tilemap WaterTilemap { get; private set; }
    public Tilemap DecorTilemap { get; private set; }
    public Tilemap PropsTilemap { get; private set; }
    public Tilemap OverlayTilemap { get; private set; }
    public Tilemap HighlightTilemap => HighlightMoveTilemap;
    public Tilemap HighlightMoveTilemap { get; private set; }
    public Tilemap HighlightAttackTilemap { get; private set; }
    public Tilemap HighlightDangerTilemap { get; private set; }
    public TacticalGridOverlay TacticalOverlay { get; private set; }
    public Transform CellColliderRoot { get; private set; }
    public bool UsePaintedGroundBackdrop { get; set; }

    public void Initialize(int width, int height, float tileWidth, float tileHeight, Sprite fallbackSprite,
                           Sprite softSprite, Sprite detail, Sprite dot)
    {
        fallbackDiamond = fallbackSprite;
        softDiamond = softSprite == null ? fallbackSprite : softSprite;
        detailSprite = detail;
        dotSprite = dot;

        binder = gameObject.GetComponent<BattleMapTilemapBinder>();
        if (binder == null)
        {
            binder = gameObject.AddComponent<BattleMapTilemapBinder>();
        }

        binder.ConfigureRuntime(Vector2Int.zero, new Vector2Int(width, height), tileWidth, tileHeight);
        grid = binder.Grid;
        GroundTilemap = binder.GroundTilemap;
        RoadTilemap = binder.RoadTilemap;
        CliffTilemap = binder.CliffTilemap;
        WaterTilemap = binder.WaterTilemap;
        DecorTilemap = binder.DecorTilemap;
        PropsTilemap = binder.PropsTilemap;
        OverlayTilemap = binder.OverlayTilemap;
        HighlightMoveTilemap = binder.HighlightMoveTilemap;
        HighlightAttackTilemap = binder.HighlightAttackTilemap;
        HighlightDangerTilemap = binder.HighlightDangerTilemap;

        overlayTile = CreateUtilityTile("Runtime Tactical Overlay", softDiamond, Color.white);
        highlightRowCount = width + height - 1;

        TacticalOverlay = binder.TacticalOverlay;
        TacticalOverlay.Configure(Vector2Int.zero, new Vector2Int(width, height));

        CellColliderRoot = new GameObject("TacticalGridOverlay_Colliders").transform;
        CellColliderRoot.SetParent(transform, false);

        BuildTerrainLayerLookup();
        CreateLightingRig(width, height);
        EnsureCinemachineIntroCamera();
    }

    public void BindAuthored(BattleMapTilemapBinder authoredBinder, float tileWidth, float tileHeight, Sprite fallbackSprite,
                             Sprite softSprite, Sprite detail, Sprite dot)
    {
        fallbackDiamond = fallbackSprite;
        softDiamond = softSprite == null ? fallbackSprite : softSprite;
        detailSprite = detail;
        dotSprite = dot;
        binder = authoredBinder == null ? gameObject.GetComponent<BattleMapTilemapBinder>() : authoredBinder;
        if (binder == null)
        {
            binder = gameObject.AddComponent<BattleMapTilemapBinder>();
        }

        binder.EnsureStructure(tileWidth, tileHeight);
        grid = binder.Grid;
        GroundTilemap = binder.GroundTilemap;
        RoadTilemap = binder.RoadTilemap;
        CliffTilemap = binder.CliffTilemap;
        WaterTilemap = binder.WaterTilemap;
        DecorTilemap = binder.DecorTilemap;
        PropsTilemap = binder.PropsTilemap;
        OverlayTilemap = binder.OverlayTilemap;
        HighlightMoveTilemap = binder.HighlightMoveTilemap;
        HighlightAttackTilemap = binder.HighlightAttackTilemap;
        HighlightDangerTilemap = binder.HighlightDangerTilemap;
        TacticalOverlay = binder.TacticalOverlay;
        overlayTile = CreateUtilityTile("Authored Tactical Overlay", softDiamond, Color.white);
        highlightRowCount = Mathf.Max(1, binder.Size.x + binder.Size.y - 1);

        Transform colliderRoot = transform.Find("AuthoredTacticalGridOverlay_Colliders");
        if (colliderRoot == null)
        {
            colliderRoot = new GameObject("AuthoredTacticalGridOverlay_Colliders").transform;
            colliderRoot.SetParent(transform, false);
        }

        CellColliderRoot = colliderRoot;
        BuildTerrainLayerLookup();
    }

    public void SetTerrainCell(Vector2Int cell, TerrainType terrainType, Sprite sprite, Color baseColor, int moveCost,
                               bool walkable, bool blocksLineOfSight, bool isChokePoint, int elevation,
                               int coverBonus, bool objective, bool danger, string laneId, string tacticalNote,
                               Vector3 worldPosition)
    {
        Tilemap layer = LayerForTerrain(terrainType);
        TerrainTileData tile = GetOrCreateTerrainTile(terrainType, sprite, moveCost, walkable, blocksLineOfSight,
                                                      isChokePoint, elevation, coverBonus, danger);
        Vector3Int tileCell = ToTilemapCell(cell);
        if (UsePaintedGroundBackdrop)
        {
            layer.SetTile(tileCell, null);
        }
        else
        {
            layer.SetTile(tileCell, tile);
            layer.SetTileFlags(tileCell, TileFlags.None);
            layer.SetColor(tileCell, tile.sprite == fallbackDiamond ? baseColor : Color.white);
        }

        SetSubtleOverlay(cell, terrainType, elevation, coverBonus, objective, danger);
        TacticalOverlay.SetCell(new TacticalGridCellData
        {
            cell = cell,
            displayName = terrainType.ToString(),
            worldPosition = worldPosition,
            terrainType = terrainType,
            moveCost = Mathf.Max(1, moveCost),
            walkable = walkable,
            blocksMovement = !walkable,
            blocksLineOfSight = blocksLineOfSight,
            isChokePoint = isChokePoint,
            capacity = isChokePoint ? 1 : 2,
            elevation = elevation,
            coverType = CoverFromBonus(coverBonus),
            hazardType = HazardForTerrain(terrainType, danger, walkable),
            northEdge = EdgeForTerrain(terrainType, danger),
            eastEdge = EdgeForTerrain(terrainType, danger),
            southEdge = EdgeForTerrain(terrainType, danger),
            westEdge = EdgeForTerrain(terrainType, danger),
            zoneId = objective ? "objective" : danger ? "danger" : string.Empty,
            laneId = laneId,
            visualTileKey = terrainType.ToString(),
            decorSetKey = tacticalNote,
        });
    }

    public void SetPropCell(Vector2Int cell, Sprite sprite, Color color)
    {
        if (sprite == null)
        {
            return;
        }

        Tile propTile = CreateUtilityTile("Runtime Prop Tile", sprite, Color.white);
        Vector3Int tileCell = ToTilemapCell(cell);
        Tilemap target = DecorTilemap == null ? PropsTilemap : DecorTilemap;
        target.SetTile(tileCell, propTile);
        target.SetTileFlags(tileCell, TileFlags.None);
        target.SetColor(tileCell, color);
    }

    public void SetHighlight(Vector2Int cell, Color color)
    {
        if (color.a <= 0.01f)
        {
            if (highlightCells.TryGetValue(cell, out SpriteRenderer existing) && existing != null)
            {
                existing.gameObject.SetActive(false);
            }

            return;
        }

        SpriteRenderer renderer = EnsureHighlightCell(cell);
        renderer.gameObject.SetActive(true);
        renderer.color = color;
    }

    public void ClearHighlights()
    {
        foreach (KeyValuePair<Vector2Int, SpriteRenderer> entry in highlightCells)
        {
            if (entry.Value != null)
            {
                entry.Value.gameObject.SetActive(false);
            }
        }

        if (HighlightMoveTilemap != null)
        {
            HighlightMoveTilemap.ClearAllTiles();
        }

        if (HighlightAttackTilemap != null)
        {
            HighlightAttackTilemap.ClearAllTiles();
        }

        if (HighlightDangerTilemap != null)
        {
            HighlightDangerTilemap.ClearAllTiles();
        }

        if (binder != null && binder.HighlightPathArrowTilemap != null)
        {
            binder.HighlightPathArrowTilemap.ClearAllTiles();
        }
    }

    private SpriteRenderer EnsureHighlightCell(Vector2Int cell)
    {
        if (highlightCells.TryGetValue(cell, out SpriteRenderer renderer) && renderer != null)
        {
            return renderer;
        }

        if (highlightRoot == null)
        {
            GameObject rootObject = new GameObject("Highlight_Cells");
            rootObject.transform.SetParent(transform, false);
            highlightRoot = rootObject.transform;
        }

        if (highlightCellSprite == null)
        {
            highlightCellSprite = CreateHighlightDiamondSprite();
        }

        GameObject cellObject = new GameObject($"Highlight_{cell.x}_{cell.y}");
        cellObject.transform.SetParent(highlightRoot, false);
        cellObject.transform.position = CellToWorld(cell) + new Vector3(0f, 0f, -0.01f);

        renderer = cellObject.AddComponent<SpriteRenderer>();
        renderer.sprite = highlightCellSprite;
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = ((highlightRowCount - (cell.x + cell.y)) * RowStride) + HighlightSlot;
        highlightCells[cell] = renderer;
        return renderer;
    }

    public void SetTerrainTint(Vector2Int cell, TerrainType terrainType, Color color)
    {
        if (UsePaintedGroundBackdrop)
        {
            Vector3Int overlayCell = ToTilemapCell(cell);
            if (color.a <= 0.01f)
            {
                OverlayTilemap.SetTile(overlayCell, null);
                return;
            }

            OverlayTilemap.SetTile(overlayCell, overlayTile);
            OverlayTilemap.SetTileFlags(overlayCell, TileFlags.None);
            OverlayTilemap.SetColor(overlayCell, color);
            return;
        }

        Tilemap layer = LayerForTerrain(terrainType);
        Vector3Int tileCell = ToTilemapCell(cell);
        if (!layer.HasTile(tileCell))
        {
            return;
        }

        layer.SetTileFlags(tileCell, TileFlags.None);
        layer.SetColor(tileCell, color);
    }

    public void RegisterPropRenderer(SpriteRenderer renderer, Vector2Int cell, BattleTestInteractableKind kind)
    {
        if (renderer == null)
        {
            return;
        }

        if (renderer.GetComponent<ShadowCaster2D>() == null)
        {
            renderer.gameObject.AddComponent<ShadowCaster2D>();
        }

        switch (kind)
        {
        case BattleTestInteractableKind.Fire:
            CreatePointLight("Lantern Light", renderer.transform, new Color(1f, 0.46f, 0.18f, 1f), 1.45f, 0.92f);
            break;
        case BattleTestInteractableKind.Objective:
            CreatePointLight("Objective Warm Light", renderer.transform, new Color(1f, 0.74f, 0.34f, 1f), 1.70f,
                             0.62f);
            break;
        case BattleTestInteractableKind.Smoke:
            CreatePointLight("Incense Haze Light", renderer.transform, new Color(0.70f, 0.78f, 0.72f, 1f), 1.20f,
                             0.32f);
            break;
        }
    }

    private Tilemap CreateLayer(string layerName, int sortingOrder)
    {
        GameObject layerObject = new GameObject(layerName);
        layerObject.transform.SetParent(transform, false);

        Tilemap tilemap = layerObject.AddComponent<Tilemap>();
        tilemap.tileAnchor = Vector3.zero;

        TilemapRenderer renderer = layerObject.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = "Default";
        renderer.sortingOrder = sortingOrder;
        renderer.sortOrder = TilemapRenderer.SortOrder.BottomLeft;
        return tilemap;
    }

    private TerrainTileData GetOrCreateTerrainTile(TerrainType terrainType, Sprite sprite, int moveCost, bool walkable,
                                                   bool blocksLineOfSight, bool isChokePoint, int elevation,
                                                   int coverBonus, bool danger)
    {
        string key = string.Join("|", terrainType, moveCost, walkable, blocksLineOfSight, isChokePoint, elevation,
                                 coverBonus, danger);
        if (terrainTiles.TryGetValue(key, out TerrainTileData tile))
        {
            return tile;
        }

        tile = ScriptableObject.CreateInstance<TerrainTileData>();
        tile.name = "Runtime_" + key.Replace("|", "_");
        tile.sprite = sprite == null ? fallbackDiamond : sprite;
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
        tile.coverType = CoverFromBonus(coverBonus);
        tile.hazardType = HazardForTerrain(terrainType, danger, walkable);
        tile.northEdge = EdgeForTerrain(terrainType, danger);
        tile.eastEdge = EdgeForTerrain(terrainType, danger);
        tile.southEdge = EdgeForTerrain(terrainType, danger);
        tile.westEdge = EdgeForTerrain(terrainType, danger);
        terrainTiles[key] = tile;
        return tile;
    }

    private void SetSubtleOverlay(Vector2Int cell, TerrainType terrainType, int elevation, int coverBonus,
                                  bool objective, bool danger)
    {
        if (UsePaintedGroundBackdrop)
        {
            return;
        }

        Color color = Color.clear;
        if (objective)
        {
            color = new Color(1f, 0.78f, 0.28f, 0.13f);
        }
        else if (danger || terrainType == TerrainType.Cliff || terrainType == TerrainType.DeepWater)
        {
            color = new Color(0.72f, 0.16f, 0.10f, 0.07f);
        }
        else if (coverBonus >= 2)
        {
            color = new Color(0.18f, 0.48f, 0.26f, 0.06f);
        }
        else if (elevation > 0)
        {
            color = new Color(0.95f, 0.76f, 0.34f, Mathf.Clamp01(0.035f + elevation * 0.018f));
        }

        if (color.a <= 0.01f)
        {
            return;
        }

        Vector3Int tileCell = ToTilemapCell(cell);
        OverlayTilemap.SetTile(tileCell, overlayTile);
        OverlayTilemap.SetTileFlags(tileCell, TileFlags.None);
        OverlayTilemap.SetColor(tileCell, color);
    }

    private Color PaintedGroundTileColor(TerrainType terrainType, bool objective, bool danger, int coverBonus)
    {
        if (UsePaintedGroundBackdrop)
        {
            return Color.clear;
        }

        float alpha = 0.035f;
        switch (terrainType)
        {
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
        case TerrainType.Water:
        case TerrainType.Ice:
            alpha = 0.075f;
            break;
        case TerrainType.Cliff:
        case TerrainType.Wall:
        case TerrainType.Rubble:
            alpha = 0.070f;
            break;
        case TerrainType.Road:
        case TerrainType.Bridge:
        case TerrainType.Gate:
        case TerrainType.ShrineFloor:
            alpha = 0.050f;
            break;
        case TerrainType.Fire:
        case TerrainType.Trap:
            alpha = 0.145f;
            break;
        case TerrainType.Smoke:
            alpha = 0.105f;
            break;
        }

        if (objective)
        {
            alpha = Mathf.Max(alpha, 0.085f);
        }
        else if (danger)
        {
            alpha = Mathf.Max(alpha, 0.095f);
        }
        else if (coverBonus >= 3)
        {
            alpha = Mathf.Max(alpha, 0.050f);
        }

        return new Color(1f, 1f, 1f, alpha);
    }

    private void CreateLightingRig(int width, int height)
    {
        Transform lightRoot = binder == null || binder.LightsRoot == null ? transform : binder.LightsRoot;
        GameObject global = new GameObject("Global Light 2D - Dawn Mist");
        global.transform.SetParent(lightRoot, false);
        Light2D globalLight = global.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.intensity = 0.72f;
        globalLight.color = new Color(0.78f, 0.84f, 0.92f, 1f);

        CreateSceneLight("Bridge Cold Bounce", new Vector2Int(width / 2, height / 2), new Color(0.42f, 0.68f, 0.80f, 1f),
                         2.35f, 0.34f);
        CreateSceneLight("Shrine Warm Pool", new Vector2Int(width / 2, height - 3), new Color(1f, 0.68f, 0.32f, 1f),
                         2.75f, 0.44f);
        CreateSceneLight("Right Ridge Rim", new Vector2Int(width - 4, height - 4),
                         new Color(0.82f, 0.78f, 0.58f, 1f), 2.10f, 0.30f);
    }

    private void CreateSceneLight(string name, Vector2Int cell, Color color, float radius, float intensity)
    {
        Transform lightRoot = binder == null || binder.LightsRoot == null ? transform : binder.LightsRoot;
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(lightRoot, false);
        lightObject.transform.position = CellToWorld(cell) + new Vector3(0f, 0.20f, -0.20f);
        CreatePointLight("Point Light 2D", lightObject.transform, color, radius, intensity);
    }

    private void CreatePointLight(string name, Transform parent, Color color, float radius, float intensity)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, false);
        lightObject.transform.localPosition = new Vector3(0f, 0.16f, -0.12f);
        Light2D light = lightObject.AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.pointLightOuterRadius = radius;
        light.pointLightInnerRadius = radius * 0.34f;
    }

    private void EnsureCinemachineIntroCamera()
    {
        Type cameraType = Type.GetType("Unity.Cinemachine.CinemachineCamera, Unity.Cinemachine");
        if (cameraType == null || !typeof(Component).IsAssignableFrom(cameraType))
        {
            return;
        }

        GameObject cameraObject = new GameObject("Cinemachine_BattleIntroCamera");
        cameraObject.transform.SetParent(transform, false);
        cameraObject.SetActive(false);
        cameraObject.AddComponent(cameraType);
    }

    private void BuildTerrainLayerLookup()
    {
        terrainLayerLookup.Clear();
        foreach (TerrainType terrainType in Enum.GetValues(typeof(TerrainType)))
        {
            terrainLayerLookup[terrainType] = LayerForTerrainInternal(terrainType);
        }
    }

    private Tilemap LayerForTerrain(TerrainType terrainType)
    {
        return terrainLayerLookup.TryGetValue(terrainType, out Tilemap layer) && layer != null
                   ? layer
                   : LayerForTerrainInternal(terrainType);
    }

    private Tilemap LayerForTerrainInternal(TerrainType terrainType)
    {
        switch (terrainType)
        {
        case TerrainType.Road:
        case TerrainType.Bridge:
        case TerrainType.Wood:
        case TerrainType.Gate:
            return RoadTilemap;
        case TerrainType.Cliff:
        case TerrainType.Wall:
        case TerrainType.Rubble:
        case TerrainType.Roof:
            return CliffTilemap;
        case TerrainType.Water:
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
        case TerrainType.Ice:
            return WaterTilemap;
        case TerrainType.Fire:
        case TerrainType.Smoke:
        case TerrainType.Trap:
            return OverlayTilemap;
        default:
            return GroundTilemap;
        }
    }

    private static CoverType CoverFromBonus(int coverBonus)
    {
        if (coverBonus >= 4)
        {
            return CoverType.Full;
        }

        if (coverBonus >= 2)
        {
            return CoverType.Heavy;
        }

        return coverBonus > 0 ? CoverType.Light : CoverType.None;
    }

    private static HazardType HazardForTerrain(TerrainType terrainType, bool danger, bool walkable)
    {
        switch (terrainType)
        {
        case TerrainType.Fire:
            return HazardType.Fire;
        case TerrainType.Smoke:
            return HazardType.Smoke;
        case TerrainType.Ice:
            return HazardType.Ice;
        case TerrainType.Trap:
            return HazardType.Trap;
        case TerrainType.DeepWater:
        case TerrainType.Water:
            return HazardType.DeepWater;
        case TerrainType.Cliff:
            return walkable ? HazardType.None : HazardType.Fall;
        default:
            return danger && !walkable ? HazardType.Fall : HazardType.None;
        }
    }

    private static EdgeType EdgeForTerrain(TerrainType terrainType, bool danger)
    {
        switch (terrainType)
        {
        case TerrainType.Cliff:
            return EdgeType.CliffDrop;
        case TerrainType.Wall:
            return EdgeType.HighWall;
        case TerrainType.Bridge:
            return EdgeType.BridgeRail;
        case TerrainType.Water:
        case TerrainType.ShallowWater:
        case TerrainType.DeepWater:
            return EdgeType.WaterBank;
        default:
            return danger ? EdgeType.CliffDrop : EdgeType.None;
        }
    }

    private Tile CreateUtilityTile(string tileName, Sprite sprite, Color color)
    {
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.name = tileName;
        tile.sprite = sprite == null ? fallbackDiamond : sprite;
        tile.color = color;
        tile.flags = TileFlags.None;
        tile.colliderType = Tile.ColliderType.None;
        return tile;
    }

    private static Sprite CreateHighlightDiamondSprite()
    {
        // Movement tiles need readable cell separation on painted backgrounds:
        // a low-alpha fill prevents wide bands, while the rim keeps each tile clear.
        const int textureWidth = 232;
        const int textureHeight = 124;
        const float inset = 0.88f;
        const float worldWidth = 0.92f;
        Texture2D texture = new Texture2D(textureWidth, textureHeight, TextureFormat.RGBA32, false);
        texture.name = "BattleHighlightCell";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < textureHeight; y++)
        {
            for (int x = 0; x < textureWidth; x++)
            {
                float nx = Mathf.Abs(((x + 0.5f) / textureWidth * 2f) - 1f) / inset;
                float ny = Mathf.Abs(((y + 0.5f) / textureHeight * 2f) - 1f) / inset;
                float d = nx + ny;
                if (d > 1f)
                {
                    texture.SetPixel(x, y, Color.clear);
                    continue;
                }

                float edge = Mathf.Clamp01((1f - d) * 26f);
                float border = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((1f - d) * 12f));
                float alpha = Mathf.Lerp(0.10f, 0.78f, border) * edge;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, textureWidth, textureHeight), new Vector2(0.5f, 0.5f),
                             textureWidth / worldWidth);
    }

    private Vector3 CellToWorld(Vector2Int cell)
    {
        return grid == null ? Vector3.zero : grid.CellToWorld(ToTilemapCell(cell));
    }

    private static Vector3Int ToTilemapCell(Vector2Int cell)
    {
        return new Vector3Int(cell.x, cell.y, 0);
    }
}
}
