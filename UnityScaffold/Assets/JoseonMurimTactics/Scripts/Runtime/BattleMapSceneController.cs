using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class BattleMapSceneController : MonoBehaviour
{
    [SerializeField] private string mapId = "authored_battle_map";
    [SerializeField] private string displayName = "Authored Battle Map";
    [SerializeField] private bool authoredProductionMap = true;
    [SerializeField] private BattleMapTilemapBinder binder;
    [SerializeField] private Vector2Int origin;
    [SerializeField] private Vector2Int size = new Vector2Int(20, 14);
    [SerializeField] private float tileWidth = 1.16f;
    [SerializeField] private float tileHeight = 0.62f;
    [SerializeField] private Color cameraBackground = new Color(0.094f, 0.118f, 0.137f, 1f);

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
            float worldX = (cell.x - cell.y) * tileWidth * 0.5f;
            float worldY = (cell.x + cell.y) * tileHeight * 0.5f;
            return transform.position + new Vector3(worldX, worldY, 0f);
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

        GameObject global = new GameObject("Global Light 2D - Battle Map");
        global.transform.SetParent(root, false);
        Light2D globalLight = global.AddComponent<Light2D>();
        globalLight.lightType = Light2D.LightType.Global;
        globalLight.color = new Color(0.78f, 0.86f, 0.94f, 1f);
        globalLight.intensity = 0.78f;

        CreatePointLight(root, "Warm Prop Light", new Vector2Int(8, 11), new Color(1f, 0.68f, 0.32f, 1f),
                         2.8f, 0.55f);
        CreatePointLight(root, "Cool Bounce Light", new Vector2Int(12, 5), new Color(0.42f, 0.70f, 0.86f, 1f),
                         2.4f, 0.36f);
        CreatePointLight(root, "Rim Light", new Vector2Int(16, 9), new Color(0.88f, 0.82f, 0.56f, 1f),
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
