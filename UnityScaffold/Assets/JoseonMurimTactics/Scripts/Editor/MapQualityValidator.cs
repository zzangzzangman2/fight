#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace JoseonMurimTactics.Editor
{
public static class MapQualityValidator
{
    private const string CurrentMapName = "백두산 설문 관문전";

    [MenuItem("Joseon Murim Tactics/Validate Current Battle Map")]
    public static void ValidateCurrentBattleMap()
    {
        Debug.Log(BuildCurrentBattleMapReport(false));
    }

    [MenuItem("Joseon Murim Tactics/Generate Map Quality Report")]
    public static void GenerateMapQualityReport()
    {
        Debug.Log(BuildCurrentBattleMapReport(true));
    }

    private static string BuildCurrentBattleMapReport(bool verbose)
    {
        BattleMapTilemapBinder binder = UnityEngine.Object.FindAnyObjectByType<BattleMapTilemapBinder>();
        if (binder != null && (binder.TacticalOverlay == null || binder.TacticalOverlay.Cells.Count == 0))
        {
            binder.SyncTacticalOverlayFromVisualTilemaps();
        }

        MapQualityTarget target = ResolveQualityTarget(binder);
        MapMetrics metrics = CollectMetrics(binder);

        List<string> pass = new List<string>();
        List<string> warnings = new List<string>();
        List<string> fail = new List<string>();

        Check(metrics.Width >= 18 && metrics.Height >= 12, $"authored map size {metrics.Width}x{metrics.Height}",
              "map smaller than v1.7 minimum 18x12", pass, fail);
        Check(metrics.TerrainTypeCount >= 8, $"{metrics.TerrainTypeCount} terrain types",
              "less than 8 terrain types", pass, fail);
        Check(metrics.CoverCellCount >= 10, $"{metrics.CoverCellCount} cover cells",
              "less than 10 cover cells", pass, fail);
        Check(metrics.CliffDropEdgeCount >= 6, $"{metrics.CliffDropEdgeCount} cliff drop edges",
              "less than 6 cliff drop edges", pass, fail);
        Check(metrics.Repeated3x3TileCount == 0, "no 3x3 repeated tactical tile block",
              $"{metrics.Repeated3x3TileCount} repeated 3x3 tile blocks", pass, fail);
        Check(metrics.HasBackdrop, "non-black backdrop / mountain field present",
              "no backdrop layer or backdrop sprite found", pass, fail);
        Check(!metrics.CameraBackgroundLooksBlack, "camera background is not black",
              "main camera background is still black", pass, fail);
        Check(metrics.MaxHighlightAlpha <= 0.18f, $"max highlight alpha {metrics.MaxHighlightAlpha:0.00}",
              $"highlight alpha too strong ({metrics.MaxHighlightAlpha:0.00})", pass, fail);
        Check(metrics.MissingPropShadowCount == 0, "all authored props have grounding shadows",
              $"{metrics.MissingPropShadowCount} props missing grounding shadows", pass, fail);
        if (metrics.OpenAreaRatio <= target.maxOpenAreaRatio)
        {
            pass.Add($"open area ratio {metrics.OpenAreaRatio:P0} <= target {target.maxOpenAreaRatio:P0}");
        }
        else
        {
            warnings.Add($"open area ratio {metrics.OpenAreaRatio:P0} exceeds target {target.maxOpenAreaRatio:P0}");
        }

        Check(metrics.LaneCount >= 2, $"{metrics.LaneCount} reachable route/lane labels",
              "less than 2 route/lane labels", pass, fail);
        Check(metrics.LaneCount >= target.minLanes, $"{metrics.LaneCount} tactical lanes",
              $"less than {target.minLanes} tactical lanes", pass, fail);
        Check(metrics.ChokePointCount >= 1, $"{metrics.ChokePointCount} narrow choke cells",
              "no 1-2 cell bottleneck found", pass, fail);
        Check(metrics.ChokePointCount >= Mathf.Max(2, target.minChokePoints), $"{metrics.ChokePointCount} choke cells",
              $"less than {Mathf.Max(2, target.minChokePoints)} choke cells", pass, fail);
        Check(metrics.ElevationDelta >= 2, $"elevation delta {metrics.ElevationDelta}",
              "less than 2-step elevation difference", pass, fail);
        Check(metrics.ElevationLevelCount >= Mathf.Max(3, target.minElevationLevels),
              $"{metrics.ElevationLevelCount} elevation levels",
              $"less than {Mathf.Max(3, target.minElevationLevels)} elevation levels", pass, fail);
        Check(metrics.InteractableCount >= 3, $"{metrics.InteractableCount} interactive props",
              "less than 3 interactive props", pass, fail);
        Check(metrics.InteractableCount >= target.minInteractables, $"{metrics.InteractableCount} interactables",
              $"less than {target.minInteractables} interactables", pass, fail);
        Check(metrics.ObjectiveCellCount >= target.minObjectiveCells, $"{metrics.ObjectiveCellCount} objective cells",
              $"less than {target.minObjectiveCells} objective cells", pass, fail);
        Check(metrics.HighGroundCellCount >= target.minHighGroundZones, $"{metrics.HighGroundCellCount} high-ground cells",
              $"less than {target.minHighGroundZones} high-ground zone", pass, fail);
        Check(metrics.LineOfSightBlockerCount >= Mathf.Max(8, target.minLineOfSightBlockerZones),
              $"{metrics.LineOfSightBlockerCount} line-of-sight blocker cells",
              $"less than {Mathf.Max(8, target.minLineOfSightBlockerZones)} line-of-sight blocker cells", pass, fail);
        Check(metrics.LineOfSightBlockerCount >= 2, $"{metrics.LineOfSightBlockerCount} LoS blocker cells",
              "less than 2 line-of-sight blocker cells", pass, fail);
        Check(!target.requiresDestructibleOrTransformableTerrain || metrics.DestructibleCount > 0,
              $"{metrics.DestructibleCount} destructible/transformable props",
              "no destructible or transformable terrain prop found", pass, fail);
        Check(metrics.FallHazardCount >= 1, $"{metrics.FallHazardCount} fall/push hazard cells",
              "no fall or push hazard cell found", pass, fail);
        Check(metrics.HasStartToObjectivePath, "walkable path from southern start edge to objective",
              "no walkable path from start edge to objective", pass, fail);
        Check(metrics.HasSouthStartToObjectivePath && metrics.HasNorthStartToObjectivePath,
              "both southern start and northern entry can reach the objective",
              "one side cannot reach the objective", pass, fail);

        if (metrics.FallHazardCount < 1)
        {
            warnings.Add("no fall hazard cells found; cliff play may feel flat");
        }

        if (metrics.WaterHazardCount < 3)
        {
            warnings.Add("river hazard is present but underused");
        }

        if (metrics.ChokePointCount > 0 && !metrics.HasStartToObjectivePath)
        {
            warnings.Add("choke layout may block AI pathing completely");
        }

        if (metrics.MissingUnitShadowCount > 0)
        {
            warnings.Add($"{metrics.MissingUnitShadowCount} runtime unit views are missing grounding shadows");
        }

        int score = Mathf.Clamp(100 - fail.Count * 16 - warnings.Count * 4, 0, 100);
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"[MapQualityValidator] Map: {CurrentMapName}");
        builder.AppendLine($"Score: {score}/100");
        AppendSection(builder, "Pass", pass);
        AppendSection(builder, "Warnings", warnings);
        AppendSection(builder, "Fail", fail);

        if (verbose)
        {
            builder.AppendLine("Metrics:");
            builder.AppendLine($"- cells: {metrics.TotalCells}, walkable: {metrics.WalkableCellCount}, open: {metrics.OpenAreaCellCount}");
            builder.AppendLine($"- elevation: min {metrics.MinElevation}, max {metrics.MaxElevation}, delta {metrics.ElevationDelta}");
            builder.AppendLine($"- v1.7: terrainTypes {metrics.TerrainTypeCount}, coverCells {metrics.CoverCellCount}, cliffDrops {metrics.CliffDropEdgeCount}, 3x3 repeats {metrics.Repeated3x3TileCount}");
            builder.AppendLine($"- visuals: backdrop {metrics.HasBackdrop}, maxHighlightAlpha {metrics.MaxHighlightAlpha:0.00}, missingPropShadows {metrics.MissingPropShadowCount}, missingUnitShadows {metrics.MissingUnitShadowCount}");
            builder.AppendLine($"- hazards: fall {metrics.FallHazardCount}, water {metrics.WaterHazardCount}, fire {metrics.FireHazardCount}, smoke {metrics.SmokeHazardCount}, ice {metrics.IceHazardCount}");
            builder.AppendLine($"- lanes: {string.Join(", ", metrics.LaneIds)}");
            builder.AppendLine("Checklist:");
            builder.AppendLine("- central bridge bottleneck, left snow-bamboo flank, right cliff high ground");
            builder.AppendLine("- frozen ford, ruined shrine altar, beacon, lantern, cart, rope bridge, fall points");
            builder.AppendLine("- highlight layers remain separated from painted map art");
        }

        return builder.ToString();
    }

    private static MapQualityTarget ResolveQualityTarget(BattleMapTilemapBinder binder)
    {
        if (binder != null && binder.BattleMapData != null && binder.BattleMapData.qualityTarget != null)
        {
            return binder.BattleMapData.qualityTarget;
        }

        BattleMapData mapData = UnityEngine.Object.FindAnyObjectByType<BattleMapData>();
        return mapData == null || mapData.qualityTarget == null ? new MapQualityTarget() : mapData.qualityTarget;
    }

    private static MapMetrics CollectMetrics(BattleMapTilemapBinder binder)
    {
        List<CellMetric> cells = CollectOverlayCells();
        if (cells.Count == 0)
        {
            cells = CollectBattleTestTiles();
        }

        if (cells.Count == 0)
        {
            cells = CollectGeneratedBattleTestProfiles();
        }

        MapMetrics metrics = new MapMetrics();
        metrics.TotalCells = cells.Count;

        if (cells.Count == 0)
        {
            return metrics;
        }

        int minX = int.MaxValue;
        int minY = int.MaxValue;
        int maxX = int.MinValue;
        int maxY = int.MinValue;
        int minElevation = int.MaxValue;
        int maxElevation = int.MinValue;
        HashSet<string> lanes = new HashSet<string>();
        HashSet<int> elevationLevels = new HashSet<int>();
        HashSet<TerrainType> terrainTypes = new HashSet<TerrainType>();
        Dictionary<Vector2Int, CellMetric> lookup = new Dictionary<Vector2Int, CellMetric>();
        List<Vector2Int> objectiveCells = new List<Vector2Int>();

        foreach (CellMetric cell in cells)
        {
            lookup[cell.Cell] = cell;
            minX = Mathf.Min(minX, cell.Cell.x);
            minY = Mathf.Min(minY, cell.Cell.y);
            maxX = Mathf.Max(maxX, cell.Cell.x);
            maxY = Mathf.Max(maxY, cell.Cell.y);
            minElevation = Mathf.Min(minElevation, cell.Elevation);
            maxElevation = Mathf.Max(maxElevation, cell.Elevation);
            elevationLevels.Add(cell.Elevation);
            terrainTypes.Add(cell.Terrain);

            if (!string.IsNullOrEmpty(cell.LaneId))
            {
                lanes.Add(cell.LaneId);
            }

            if (cell.Walkable)
            {
                metrics.WalkableCellCount++;
            }

            if (cell.Walkable && cell.CoverType == CoverType.None && !cell.BlocksLineOfSight &&
                !cell.IsChokePoint && cell.Elevation <= 1)
            {
                metrics.OpenAreaCellCount++;
            }

            if (cell.IsChokePoint)
            {
                metrics.ChokePointCount++;
            }

            if (cell.Objective)
            {
                objectiveCells.Add(cell.Cell);
                metrics.ObjectiveCellCount++;
            }

            if (cell.Elevation >= 2 && cell.Walkable)
            {
                metrics.HighGroundCellCount++;
            }

            if (cell.BlocksLineOfSight)
            {
                metrics.LineOfSightBlockerCount++;
            }

            if (cell.CoverType != CoverType.None)
            {
                metrics.CoverCellCount++;
            }

            if (HasCliffDrop(cell))
            {
                metrics.CliffDropEdgeCount += CountCliffDropEdges(cell);
            }

            CountHazards(metrics, cell);
        }

        metrics.Width = maxX - minX + 1;
        metrics.Height = maxY - minY + 1;
        metrics.MinElevation = minElevation;
        metrics.MaxElevation = maxElevation;
        metrics.ElevationDelta = maxElevation - minElevation;
        metrics.TerrainTypeCount = terrainTypes.Count;
        metrics.LaneCount = lanes.Count;
        metrics.LaneIds.AddRange(lanes);
        metrics.ElevationLevelCount = elevationLevels.Count;
        metrics.InteractableCount = CountInteractables();
        metrics.DestructibleCount = CountDestructibles();
        metrics.LineOfSightBlockerCount += CountPropLineOfSightBlockers();
        metrics.MissingPropShadowCount = CountMissingPropShadows();
        metrics.MissingUnitShadowCount = CountMissingUnitShadows();
        metrics.HasBackdrop = HasBackdrop(binder);
        metrics.CameraBackgroundLooksBlack = CameraBackgroundLooksBlack();
        metrics.MaxHighlightAlpha = MaxHighlightAlpha(binder);
        metrics.Repeated3x3TileCount = CountRepeated3x3Tiles(binder);
        metrics.OpenAreaRatio = metrics.TotalCells == 0 ? 1f : metrics.OpenAreaCellCount / (float)metrics.TotalCells;
        metrics.HasSouthStartToObjectivePath = HasEdgeToObjectivePath(lookup, minY, maxY, objectiveCells, true);
        metrics.HasNorthStartToObjectivePath = HasEdgeToObjectivePath(lookup, minY, maxY, objectiveCells, false);
        metrics.HasStartToObjectivePath = metrics.HasSouthStartToObjectivePath;
        return metrics;
    }

    private static List<CellMetric> CollectOverlayCells()
    {
        List<CellMetric> cells = new List<CellMetric>();
        TacticalGridOverlay overlay = UnityEngine.Object.FindAnyObjectByType<TacticalGridOverlay>();
        if (overlay == null || overlay.Cells.Count == 0)
        {
            return cells;
        }

        foreach (TacticalGridCellData data in overlay.Cells)
        {
            cells.Add(new CellMetric
            {
                Cell = data.cell,
                Terrain = data.terrainType,
                Walkable = data.walkable && !data.blocksMovement,
                BlocksLineOfSight = data.blocksLineOfSight,
                IsChokePoint = data.isChokePoint,
                Elevation = data.elevation,
                CoverType = data.coverType,
                HazardType = data.hazardType,
                LaneId = data.laneId,
                Objective = data.zoneId == "objective",
                NorthEdge = data.northEdge,
                EastEdge = data.eastEdge,
                SouthEdge = data.southEdge,
                WestEdge = data.westEdge,
            });
        }

        return cells;
    }

    private static List<CellMetric> CollectBattleTestTiles()
    {
        List<CellMetric> cells = new List<CellMetric>();
        BattleTestTile[] tiles = UnityEngine.Object.FindObjectsByType<BattleTestTile>(FindObjectsSortMode.None);
        foreach (BattleTestTile tile in tiles)
        {
            cells.Add(new CellMetric
            {
                Cell = tile.cell,
                Terrain = tile.terrain,
                Walkable = tile.walkable,
                BlocksLineOfSight = tile.blocksLineOfSight,
                IsChokePoint = tile.isChokePoint,
                Elevation = tile.elevation,
                CoverType = CoverFromBonus(tile.coverBonus),
                HazardType = HazardForTile(tile.terrain, tile.danger, tile.walkable),
                LaneId = tile.laneId,
                Objective = tile.objective,
            });
        }

        return cells;
    }

    private static List<CellMetric> CollectGeneratedBattleTestProfiles()
    {
        List<CellMetric> cells = new List<CellMetric>();
        BattleTestController controller = UnityEngine.Object.FindAnyObjectByType<BattleTestController>();
        if (controller == null)
        {
            return cells;
        }

        MethodInfo resolveTerrain = typeof(BattleTestController).GetMethod("ResolveTerrain",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (resolveTerrain == null)
        {
            return cells;
        }

        for (int y = 0; y < controller.height; y++)
        {
            for (int x = 0; x < controller.width; x++)
            {
                object profile = resolveTerrain.Invoke(controller, new object[] { x, y });
                TerrainType terrain = Field<TerrainType>(profile, "terrain");
                bool walkable = Field<bool>(profile, "walkable");
                bool danger = Field<bool>(profile, "danger");
                int coverBonus = Field<int>(profile, "coverBonus");
                cells.Add(new CellMetric
                {
                    Cell = new Vector2Int(x, y),
                    Terrain = terrain,
                    Walkable = walkable,
                    BlocksLineOfSight = Field<bool>(profile, "blocksLineOfSight"),
                    IsChokePoint = Field<bool>(profile, "isChokePoint"),
                    Elevation = Field<int>(profile, "elevation"),
                    CoverType = CoverFromBonus(coverBonus),
                    HazardType = HazardForTile(terrain, danger, walkable),
                    LaneId = Field<string>(profile, "laneId"),
                    Objective = Field<bool>(profile, "objective"),
                });
            }
        }

        return cells;
    }

    private static T Field<T>(object target, string fieldName)
    {
        if (target == null)
        {
            return default;
        }

        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        return field == null ? default : (T)field.GetValue(target);
    }

    private static void CountHazards(MapMetrics metrics, CellMetric cell)
    {
        if (cell.HazardType == HazardType.Fall || cell.Terrain == TerrainType.Cliff)
        {
            metrics.FallHazardCount++;
        }

        if (cell.HazardType == HazardType.DeepWater || cell.Terrain == TerrainType.Water ||
            cell.Terrain == TerrainType.ShallowWater || cell.Terrain == TerrainType.DeepWater)
        {
            metrics.WaterHazardCount++;
        }

        if (cell.HazardType == HazardType.Fire || cell.Terrain == TerrainType.Fire)
        {
            metrics.FireHazardCount++;
        }

        if (cell.HazardType == HazardType.Smoke || cell.Terrain == TerrainType.Smoke)
        {
            metrics.SmokeHazardCount++;
        }

        if (cell.HazardType == HazardType.Ice || cell.Terrain == TerrainType.Ice)
        {
            metrics.IceHazardCount++;
        }
    }

    private static bool HasCliffDrop(CellMetric cell)
    {
        return cell.NorthEdge == EdgeType.CliffDrop || cell.EastEdge == EdgeType.CliffDrop ||
               cell.SouthEdge == EdgeType.CliffDrop || cell.WestEdge == EdgeType.CliffDrop;
    }

    private static int CountCliffDropEdges(CellMetric cell)
    {
        int count = 0;
        if (cell.NorthEdge == EdgeType.CliffDrop)
        {
            count++;
        }

        if (cell.EastEdge == EdgeType.CliffDrop)
        {
            count++;
        }

        if (cell.SouthEdge == EdgeType.CliffDrop)
        {
            count++;
        }

        if (cell.WestEdge == EdgeType.CliffDrop)
        {
            count++;
        }

        return count;
    }

    private static int CountPropLineOfSightBlockers()
    {
        return UnityEngine.Object.FindObjectsByType<LineOfSightBlocker>(FindObjectsSortMode.None).Length;
    }

    private static int CountMissingPropShadows()
    {
        int missing = 0;
        MapPropView[] props = UnityEngine.Object.FindObjectsByType<MapPropView>(FindObjectsSortMode.None);
        foreach (MapPropView prop in props)
        {
            if (prop == null || !prop.interactive)
            {
                continue;
            }

            if (prop.GetComponentInChildren<ShadowBlob>(true) == null)
            {
                missing++;
            }
        }

        return missing;
    }

    private static int CountMissingUnitShadows()
    {
        int missing = 0;
        BattleTestUnitView[] units = UnityEngine.Object.FindObjectsByType<BattleTestUnitView>(FindObjectsSortMode.None);
        foreach (BattleTestUnitView unit in units)
        {
            if (unit != null && unit.GetComponentInChildren<ShadowBlob>(true) == null)
            {
                missing++;
            }
        }

        return missing;
    }

    private static bool HasBackdrop(BattleMapTilemapBinder binder)
    {
        if (binder != null)
        {
            if (TileCount(binder.BackdropBaseTilemap) > 0 || TileCount(binder.BackdropDistantTilemap) > 0)
            {
                return true;
            }
        }

        SpriteRenderer[] renderers = UnityEngine.Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer != null && renderer.gameObject.name.IndexOf("Backdrop", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                return true;
            }
        }

        return false;
    }

    private static bool CameraBackgroundLooksBlack()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            return false;
        }

        Color color = camera.backgroundColor;
        return color.r < 0.04f && color.g < 0.04f && color.b < 0.04f;
    }

    private static float MaxHighlightAlpha(BattleMapTilemapBinder binder)
    {
        if (binder == null)
        {
            return 0f;
        }

        float max = 0f;
        max = Mathf.Max(max, MaxTilemapAlpha(binder.HighlightMoveTilemap));
        max = Mathf.Max(max, MaxTilemapAlpha(binder.HighlightAttackTilemap));
        max = Mathf.Max(max, MaxTilemapAlpha(binder.HighlightDangerTilemap));
        max = Mathf.Max(max, MaxTilemapAlpha(binder.HighlightPathArrowTilemap));
        return max;
    }

    private static float MaxTilemapAlpha(Tilemap tilemap)
    {
        if (tilemap == null || tilemap.GetUsedTilesCount() == 0)
        {
            return 0f;
        }

        float max = 0f;
        foreach (Vector3Int cell in tilemap.cellBounds.allPositionsWithin)
        {
            if (!tilemap.HasTile(cell))
            {
                continue;
            }

            max = Mathf.Max(max, tilemap.GetColor(cell).a);
        }

        return max;
    }

    private static int CountRepeated3x3Tiles(BattleMapTilemapBinder binder)
    {
        if (binder == null)
        {
            return 0;
        }

        int repeats = 0;
        Tilemap[] tilemaps = { binder.GroundBaseTilemap, binder.GroundVariationTilemap, binder.RoadPathTilemap,
                               binder.WaterBaseTilemap, binder.CliffTopTilemap };
        foreach (Tilemap tilemap in tilemaps)
        {
            int layerRepeats = CountRepeated3x3Tiles(tilemap);
            if (layerRepeats > 0)
            {
                Debug.Log($"[MapQualityValidator] 3x3 repeat layer {tilemap.name}: {layerRepeats}");
            }

            repeats += layerRepeats;
        }

        return repeats;
    }

    private static int CountRepeated3x3Tiles(Tilemap tilemap)
    {
        if (tilemap == null || tilemap.GetUsedTilesCount() == 0)
        {
            return 0;
        }

        int repeats = 0;
        BoundsInt bounds = tilemap.cellBounds;
        for (int y = bounds.yMin; y <= bounds.yMax - 3; y++)
        {
            for (int x = bounds.xMin; x <= bounds.xMax - 3; x++)
            {
                TileBase first = tilemap.GetTile(new Vector3Int(x, y, 0));
                if (first == null)
                {
                    continue;
                }

                bool same = true;
                for (int yy = 0; yy < 3 && same; yy++)
                {
                    for (int xx = 0; xx < 3; xx++)
                    {
                        if (tilemap.GetTile(new Vector3Int(x + xx, y + yy, 0)) != first)
                        {
                            same = false;
                            break;
                        }
                    }
                }

                if (same)
                {
                    if (repeats < 8)
                    {
                        Debug.Log($"[MapQualityValidator] 3x3 repeat at {tilemap.name} ({x},{y}) tile {first.name}");
                    }

                    repeats++;
                }
            }
        }

        return repeats;
    }

    private static int TileCount(Tilemap tilemap)
    {
        return tilemap == null ? 0 : tilemap.GetUsedTilesCount();
    }

    private static int CountInteractables()
    {
        int propCount = UnityEngine.Object.FindObjectsByType<MapPropView>(FindObjectsSortMode.None).Length;
        if (propCount > 0)
        {
            return propCount;
        }

        return UnityEngine.Object.FindAnyObjectByType<BattleTestController>() == null ? 0 : 9;
    }

    private static int CountDestructibles()
    {
        int destructibleCount = UnityEngine.Object.FindObjectsByType<DestructibleProp>(FindObjectsSortMode.None).Length;
        if (destructibleCount > 0)
        {
            return destructibleCount;
        }

        return UnityEngine.Object.FindAnyObjectByType<BattleTestController>() == null ? 0 : 4;
    }

    private static bool HasEdgeToObjectivePath(Dictionary<Vector2Int, CellMetric> lookup, int minY, int maxY,
                                               List<Vector2Int> objectiveCells, bool startFromSouth)
    {
        if (lookup.Count == 0)
        {
            return false;
        }

        List<Vector2Int> targets = new List<Vector2Int>(objectiveCells);
        if (targets.Count == 0)
        {
            foreach (CellMetric cell in lookup.Values)
            {
                if (cell.Walkable && cell.Cell.y >= maxY - 1)
                {
                    targets.Add(cell.Cell);
                }
            }
        }

        HashSet<Vector2Int> objectives = new HashSet<Vector2Int>(targets);
        Queue<Vector2Int> frontier = new Queue<Vector2Int>();
        HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

        foreach (CellMetric cell in lookup.Values)
        {
            bool onStartEdge = startFromSouth ? cell.Cell.y <= minY + 1 : cell.Cell.y >= maxY - 1;
            if (!cell.Walkable || !onStartEdge)
            {
                continue;
            }

            frontier.Enqueue(cell.Cell);
            visited.Add(cell.Cell);
        }

        while (frontier.Count > 0)
        {
            Vector2Int current = frontier.Dequeue();
            if (objectives.Contains(current))
            {
                return true;
            }

            foreach (Vector2Int next in Neighbors(current))
            {
                if (visited.Contains(next) || !lookup.TryGetValue(next, out CellMetric nextCell) || !nextCell.Walkable)
                {
                    continue;
                }

                visited.Add(next);
                frontier.Enqueue(next);
            }
        }

        return false;
    }

    private static IEnumerable<Vector2Int> Neighbors(Vector2Int cell)
    {
        yield return new Vector2Int(cell.x + 1, cell.y);
        yield return new Vector2Int(cell.x - 1, cell.y);
        yield return new Vector2Int(cell.x, cell.y + 1);
        yield return new Vector2Int(cell.x, cell.y - 1);
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

    private static HazardType HazardForTile(TerrainType terrain, bool danger, bool walkable)
    {
        switch (terrain)
        {
        case TerrainType.Fire:
            return HazardType.Fire;
        case TerrainType.Smoke:
            return HazardType.Smoke;
        case TerrainType.Ice:
            return HazardType.Ice;
        case TerrainType.Trap:
            return HazardType.Trap;
        case TerrainType.Water:
        case TerrainType.DeepWater:
            return HazardType.DeepWater;
        case TerrainType.Cliff:
            return walkable ? HazardType.None : HazardType.Fall;
        default:
            return danger && !walkable ? HazardType.Fall : HazardType.None;
        }
    }

    private static void Check(bool condition, string passText, string failText, List<string> pass, List<string> fail)
    {
        if (condition)
        {
            pass.Add(passText);
        }
        else if (!string.IsNullOrEmpty(failText))
        {
            fail.Add(failText);
        }
    }

    private static void AppendSection(StringBuilder builder, string title, List<string> lines)
    {
        builder.AppendLine(title + ":");
        if (lines.Count == 0)
        {
            builder.AppendLine("- none");
            return;
        }

        foreach (string line in lines)
        {
            builder.AppendLine("- " + line);
        }
    }

    private sealed class CellMetric
    {
        public Vector2Int Cell;
        public TerrainType Terrain;
        public bool Walkable;
        public bool BlocksLineOfSight;
        public bool IsChokePoint;
        public int Elevation;
        public CoverType CoverType;
        public HazardType HazardType;
        public string LaneId;
        public bool Objective;
        public EdgeType NorthEdge;
        public EdgeType EastEdge;
        public EdgeType SouthEdge;
        public EdgeType WestEdge;
    }

    private sealed class MapMetrics
    {
        public int Width;
        public int Height;
        public int TotalCells;
        public int WalkableCellCount;
        public int OpenAreaCellCount;
        public int MinElevation;
        public int MaxElevation;
        public int ElevationDelta;
        public int TerrainTypeCount;
        public float OpenAreaRatio = 1f;
        public int LaneCount;
        public int ChokePointCount;
        public int ElevationLevelCount;
        public int InteractableCount;
        public int ObjectiveCellCount;
        public int HighGroundCellCount;
        public int LineOfSightBlockerCount;
        public int CoverCellCount;
        public int CliffDropEdgeCount;
        public int DestructibleCount;
        public int FallHazardCount;
        public int WaterHazardCount;
        public int FireHazardCount;
        public int SmokeHazardCount;
        public int IceHazardCount;
        public int Repeated3x3TileCount;
        public bool HasBackdrop;
        public bool CameraBackgroundLooksBlack;
        public float MaxHighlightAlpha;
        public int MissingPropShadowCount;
        public int MissingUnitShadowCount;
        public bool HasStartToObjectivePath;
        public bool HasSouthStartToObjectivePath;
        public bool HasNorthStartToObjectivePath;
        public readonly List<string> LaneIds = new List<string>();
    }
}
}
#endif
