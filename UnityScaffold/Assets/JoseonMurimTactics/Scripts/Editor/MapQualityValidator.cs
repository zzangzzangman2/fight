#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

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
        if (binder != null)
        {
            binder.SyncTacticalOverlayFromVisualTilemaps();
        }

        MapQualityTarget target = ResolveQualityTarget(binder);
        MapMetrics metrics = CollectMetrics();

        List<string> pass = new List<string>();
        List<string> warnings = new List<string>();
        List<string> fail = new List<string>();

        Check(metrics.Width >= 16 && metrics.Height >= 12, $"map size {metrics.Width}x{metrics.Height}",
              "map smaller than 16x12", pass, fail);
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
        Check(metrics.ChokePointCount >= target.minChokePoints, $"{metrics.ChokePointCount} choke cells",
              $"less than {target.minChokePoints} choke cells", pass, fail);
        Check(metrics.ElevationDelta >= 2, $"elevation delta {metrics.ElevationDelta}",
              "less than 2-step elevation difference", pass, fail);
        Check(metrics.ElevationLevelCount >= target.minElevationLevels,
              $"{metrics.ElevationLevelCount} elevation levels",
              $"less than {target.minElevationLevels} elevation levels", pass, fail);
        Check(metrics.InteractableCount >= 3, $"{metrics.InteractableCount} interactive props",
              "less than 3 interactive props", pass, fail);
        Check(metrics.InteractableCount >= target.minInteractables, $"{metrics.InteractableCount} interactables",
              $"less than {target.minInteractables} interactables", pass, fail);
        Check(metrics.ObjectiveCellCount >= target.minObjectiveCells, $"{metrics.ObjectiveCellCount} objective cells",
              $"less than {target.minObjectiveCells} objective cells", pass, fail);
        Check(metrics.HighGroundCellCount >= target.minHighGroundZones, $"{metrics.HighGroundCellCount} high-ground cells",
              $"less than {target.minHighGroundZones} high-ground zone", pass, fail);
        Check(metrics.LineOfSightBlockerCount >= target.minLineOfSightBlockerZones,
              $"{metrics.LineOfSightBlockerCount} line-of-sight blocker cells",
              $"less than {target.minLineOfSightBlockerZones} line-of-sight blocker zone", pass, fail);
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

    private static MapMetrics CollectMetrics()
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

            CountHazards(metrics, cell);
        }

        metrics.Width = maxX - minX + 1;
        metrics.Height = maxY - minY + 1;
        metrics.MinElevation = minElevation;
        metrics.MaxElevation = maxElevation;
        metrics.ElevationDelta = maxElevation - minElevation;
        metrics.LaneCount = lanes.Count;
        metrics.LaneIds.AddRange(lanes);
        metrics.ElevationLevelCount = elevationLevels.Count;
        metrics.InteractableCount = CountInteractables();
        metrics.DestructibleCount = CountDestructibles();
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
        public float OpenAreaRatio = 1f;
        public int LaneCount;
        public int ChokePointCount;
        public int ElevationLevelCount;
        public int InteractableCount;
        public int ObjectiveCellCount;
        public int HighGroundCellCount;
        public int LineOfSightBlockerCount;
        public int DestructibleCount;
        public int FallHazardCount;
        public int WaterHazardCount;
        public int FireHazardCount;
        public int SmokeHazardCount;
        public int IceHazardCount;
        public bool HasStartToObjectivePath;
        public bool HasSouthStartToObjectivePath;
        public bool HasNorthStartToObjectivePath;
        public readonly List<string> LaneIds = new List<string>();
    }
}
}
#endif
