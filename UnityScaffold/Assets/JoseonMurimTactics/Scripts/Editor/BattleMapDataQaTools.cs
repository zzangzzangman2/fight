using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
    public static class BattleMapDataQaTools
    {
        private const string CsvFileName = "2026-06-14_baekdu_snow_gate_cells.csv";
        private const string ValidatorFileName = "2026-06-14_battle-map-rebuild-validator.md";

        private static readonly Vector2Int[] AllySpawnCells =
        {
            new Vector2Int(4, 0),
            new Vector2Int(5, 0),
            new Vector2Int(6, 0),
            new Vector2Int(7, 0),
            new Vector2Int(4, 1),
            new Vector2Int(5, 1),
        };

        private static readonly Vector2Int[] EnemySpawnCells =
        {
            new Vector2Int(7, 2),
            new Vector2Int(8, 2),
            new Vector2Int(9, 2),
            new Vector2Int(7, 3),
            new Vector2Int(8, 3),
            new Vector2Int(9, 3),
        };

        [MenuItem("Joseon Murim Tactics/Battle Maps/Export Current BattleMap CSV")]
        public static void ExportCurrentBattleMapCsv()
        {
            BattleMapData mapData = LoadCurrentBattleMapData();
            string path = Path.Combine(RepoRequestFolder(), CsvFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(path, BuildCsv(mapData), new UTF8Encoding(false));
            Debug.Log($"[BattleMapDataQaTools] Exported {path}");
        }

        [MenuItem("Joseon Murim Tactics/Battle Maps/Validate Current BattleMap")]
        public static void ValidateCurrentBattleMap()
        {
            BattleMapData mapData = LoadCurrentBattleMapData();
            List<string> failures = Validate(mapData);
            string path = Path.Combine(RepoRequestFolder(), ValidatorFileName);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.WriteAllText(
                path,
                BuildValidationReport(mapData, failures),
                new UTF8Encoding(false)
            );

            if (failures.Count > 0)
            {
                throw new InvalidOperationException(
                    $"BattleMapData validation failed with {failures.Count} issue(s). See {path}"
                );
            }

            Debug.Log($"[BattleMapDataQaTools] BattleMapData validation passed. Report: {path}");
        }

        [MenuItem("Joseon Murim Tactics/Battle Maps/Import Runtime Edit CSV")]
        public static void ImportRuntimeEditCsv()
        {
            BattleMapData mapData = LoadCurrentBattleMapData();
            string path = BattleMapRuntimeEditStore.RepoOverridePath;
            if (
                !BattleMapRuntimeEditStore.TryLoadOverridesFromPath(
                    path,
                    out List<BattleMapRuntimeCellEdit> edits,
                    out string message
                )
            )
            {
                throw new InvalidOperationException($"{message} Path: {path}");
            }

            Dictionary<Vector2Int, BattleCellData> cells = CellsByPosition(mapData);
            int applied = 0;
            for (int i = 0; i < edits.Count; i++)
            {
                BattleMapRuntimeCellEdit edit = edits[i];
                if (!cells.TryGetValue(edit.cell, out BattleCellData cell))
                {
                    continue;
                }

                ApplyRuntimeEdit(cell, edit);
                applied++;
            }

            EditorUtility.SetDirty(mapData);
            AssetDatabase.SaveAssets();
            BattleMapRuntimeCatalog.ClearCache();
            Debug.Log(
                $"[BattleMapDataQaTools] Imported {applied} runtime edit cells into {mapData.name}. Source: {path}"
            );
        }

        private static BattleMapData LoadCurrentBattleMapData()
        {
            BattleMapData mapData = Resources.Load<BattleMapData>(
                BattleMapRuntimeCatalog.BaekduSnowGateResourcePath
            );
            if (mapData == null)
            {
                throw new InvalidOperationException(
                    $"BattleMapData not found at Resources/{BattleMapRuntimeCatalog.BaekduSnowGateResourcePath}."
                );
            }

            return mapData;
        }

        private static string BuildCsv(BattleMapData mapData)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(
                "x,y,terrainType,walkable,occupyAllowed,moveCost,elevation,blocksLineOfSight,blocksProjectiles,coverBonus,deployZone,tags,zoneId,laneId"
            );

            Dictionary<Vector2Int, BattleCellData> cells = CellsByPosition(mapData);
            for (int y = 0; y < mapData.size.y; y++)
            {
                for (int x = 0; x < mapData.size.x; x++)
                {
                    Vector2Int cell = new Vector2Int(x, y);
                    if (!cells.TryGetValue(cell, out BattleCellData data))
                    {
                        continue;
                    }

                    builder.Append(x.ToString(CultureInfo.InvariantCulture)).Append(',');
                    builder.Append(y.ToString(CultureInfo.InvariantCulture)).Append(',');
                    builder.Append(Escape(data.terrainType.ToString())).Append(',');
                    builder.Append(data.walkable).Append(',');
                    builder.Append(data.occupyAllowed).Append(',');
                    builder
                        .Append(data.moveCost.ToString(CultureInfo.InvariantCulture))
                        .Append(',');
                    builder
                        .Append(data.elevation.ToString(CultureInfo.InvariantCulture))
                        .Append(',');
                    builder.Append(data.blocksLineOfSight).Append(',');
                    builder.Append(data.blocksProjectiles).Append(',');
                    builder
                        .Append(data.coverBonus.ToString(CultureInfo.InvariantCulture))
                        .Append(',');
                    builder
                        .Append(data.deployZone.ToString(CultureInfo.InvariantCulture))
                        .Append(',');
                    builder
                        .Append(
                            Escape(data.tags == null ? string.Empty : string.Join("|", data.tags))
                        )
                        .Append(',');
                    builder.Append(Escape(data.zoneId)).Append(',');
                    builder.Append(Escape(data.laneId));
                    builder.AppendLine();
                }
            }

            return builder.ToString();
        }

        private static List<string> Validate(BattleMapData mapData)
        {
            List<string> failures = new List<string>();
            if (mapData.size != new Vector2Int(16, 12))
            {
                failures.Add($"Expected size 16x12, got {mapData.size.x}x{mapData.size.y}.");
            }

            Dictionary<Vector2Int, BattleCellData> cells = CellsByPosition(mapData, failures);
            if (cells.Count != 192)
            {
                failures.Add($"Expected 192 unique cells, got {cells.Count}.");
            }

            for (int y = 0; y < mapData.size.y; y++)
            {
                for (int x = 0; x < mapData.size.x; x++)
                {
                    Vector2Int position = new Vector2Int(x, y);
                    if (!cells.TryGetValue(position, out BattleCellData cell))
                    {
                        failures.Add($"Missing cell {position}.");
                        continue;
                    }

                    ValidateCell(cell, failures);
                }
            }

            ValidateTaggedContinuity(cells, failures);
            ValidateSpawnCells(cells, AllySpawnCells, "ally", failures);
            ValidateSpawnCells(cells, EnemySpawnCells, "enemy", failures);

            return failures;
        }

        private static void ApplyRuntimeEdit(BattleCellData cell, BattleMapRuntimeCellEdit edit)
        {
            cell.terrainType = edit.terrainType;
            cell.walkable = edit.walkable;
            cell.occupyAllowed = edit.occupyAllowed;
            cell.blocksMovement = !edit.walkable || !edit.occupyAllowed || edit.moveCost >= 90;
            cell.moveCost = cell.blocksMovement ? 99 : Mathf.Max(1, edit.moveCost);
            cell.elevation = edit.elevation;
            cell.blocksLineOfSight = edit.blocksLineOfSight;
            cell.blocksProjectiles = edit.blocksProjectiles;
            cell.coverBonus = edit.coverBonus;
            cell.coverType = CoverFromBonus(edit.coverBonus);
            cell.deployZone = edit.deployZone;
            cell.hazardType = edit.hazardType;
            cell.laneId = edit.laneId;
            cell.tags = new List<string>(edit.tags);
        }

        private static void ValidateCell(BattleCellData cell, List<string> failures)
        {
            if (!cell.walkable && cell.moveCost < 90 && !cell.blocksMovement)
            {
                failures.Add(
                    $"{cell.cell}: blocked cell should use moveCost>=90 or blocksMovement=true."
                );
            }

            using (BattleTestTileProbe probe = new BattleTestTileProbe(cell))
            {
                bool canStand = BattlePathService.CanStandOnTile(probe.Tile);
                if (cell.moveCost >= 90 && canStand)
                {
                    failures.Add(
                        $"{cell.cell}: moveCost>=90 but BattlePathService allows standing."
                    );
                }

                if (cell.deployZone > 0 && !canStand)
                {
                    failures.Add($"{cell.cell}: deploy zone must be standable.");
                }
            }
        }

        private static void ValidateTaggedContinuity(
            Dictionary<Vector2Int, BattleCellData> cells,
            List<string> failures
        )
        {
            foreach (BattleCellData cell in cells.Values)
            {
                if (!HasTag(cell, "stairs") && !HasTag(cell, "ramp"))
                {
                    continue;
                }

                int linked = 0;
                foreach (Vector2Int neighbor in OrthogonalNeighbors(cell.cell))
                {
                    if (
                        cells.TryGetValue(neighbor, out BattleCellData neighborCell)
                        && (HasTag(neighborCell, "stairs") || HasTag(neighborCell, "ramp"))
                    )
                    {
                        linked++;
                    }
                }

                if (linked == 0)
                {
                    failures.Add($"{cell.cell}: stairs/ramp tag is isolated.");
                }
            }
        }

        private static void ValidateSpawnCells(
            Dictionary<Vector2Int, BattleCellData> cells,
            IReadOnlyList<Vector2Int> spawnCells,
            string label,
            List<string> failures
        )
        {
            for (int i = 0; i < spawnCells.Count; i++)
            {
                Vector2Int position = spawnCells[i];
                if (!cells.TryGetValue(position, out BattleCellData cell))
                {
                    failures.Add($"{label} spawn {position}: missing cell.");
                    continue;
                }

                using (BattleTestTileProbe probe = new BattleTestTileProbe(cell))
                {
                    if (!BattlePathService.CanStandOnTile(probe.Tile))
                    {
                        failures.Add($"{label} spawn {position}: not standable.");
                    }
                }
            }
        }

        private static Dictionary<Vector2Int, BattleCellData> CellsByPosition(
            BattleMapData mapData,
            List<string> failures = null
        )
        {
            Dictionary<Vector2Int, BattleCellData> result =
                new Dictionary<Vector2Int, BattleCellData>();
            if (mapData.cells == null)
            {
                failures?.Add("Map data has null cells list.");
                return result;
            }

            for (int i = 0; i < mapData.cells.Count; i++)
            {
                BattleCellData cell = mapData.cells[i];
                if (cell == null)
                {
                    failures?.Add($"Null cell at index {i}.");
                    continue;
                }

                if (result.ContainsKey(cell.cell))
                {
                    failures?.Add($"Duplicate cell {cell.cell}.");
                    continue;
                }

                result.Add(cell.cell, cell);
            }

            return result;
        }

        private static string BuildValidationReport(BattleMapData mapData, List<string> failures)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("# BattleMapData Validator");
            builder.AppendLine();
            builder.AppendLine($"- mapId: `{mapData.mapId}`");
            builder.AppendLine(
                $"- source: `Resources/{BattleMapRuntimeCatalog.BaekduSnowGateResourcePath}`"
            );
            builder.AppendLine($"- size: `{mapData.size.x}x{mapData.size.y}`");
            builder.AppendLine($"- cells: `{(mapData.cells == null ? 0 : mapData.cells.Count)}`");
            builder.AppendLine($"- result: `{(failures.Count == 0 ? "PASS" : "FAIL")}`");
            builder.AppendLine();
            builder.AppendLine("## Checks");
            builder.AppendLine();
            builder.AppendLine("- 16x12 = 192 cells");
            builder.AppendLine("- no missing or duplicate coordinates");
            builder.AppendLine("- blocked cells use impassable movement cost or blocksMovement");
            builder.AppendLine("- moveCost>=90 is rejected by BattlePathService.CanStandOnTile");
            builder.AppendLine("- stairs/ramp tags form a connected route");
            builder.AppendLine("- deploy and spawn cells are standable");
            builder.AppendLine(
                "- BattleMapData asset is allowed to diverge from generated fallback after authoring"
            );

            if (failures.Count > 0)
            {
                builder.AppendLine();
                builder.AppendLine("## Failures");
                builder.AppendLine();
                for (int i = 0; i < failures.Count; i++)
                {
                    builder.AppendLine($"- {failures[i]}");
                }
            }

            return builder.ToString();
        }

        private static IEnumerable<Vector2Int> OrthogonalNeighbors(Vector2Int cell)
        {
            yield return cell + Vector2Int.up;
            yield return cell + Vector2Int.right;
            yield return cell + Vector2Int.down;
            yield return cell + Vector2Int.left;
        }

        private static bool HasTag(BattleCellData cell, string tag)
        {
            if (cell.tags == null)
            {
                return false;
            }

            for (int i = 0; i < cell.tags.Count; i++)
            {
                if (string.Equals(cell.tags[i], tag, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static string RepoRequestFolder()
        {
            return BattleMapRuntimeEditStore.RepoRequestFolder();
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

        private static string Escape(string value)
        {
            value = value ?? string.Empty;
            if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return value;
        }

        private sealed class BattleTestTileProbe : IDisposable
        {
            private readonly GameObject gameObject;

            public BattleTestTileProbe(BattleCellData cell)
            {
                gameObject = new GameObject("BattleMapValidatorTile")
                {
                    hideFlags = HideFlags.HideAndDontSave,
                };
                Tile = gameObject.AddComponent<BattleTestTile>();
                Tile.cell = cell.cell;
                Tile.terrain = cell.terrainType;
                Tile.walkable = cell.walkable && !cell.blocksMovement;
                Tile.occupyAllowed = cell.occupyAllowed;
                Tile.moveCost = cell.blocksMovement ? 99 : cell.moveCost;
                Tile.elevation = cell.elevation;
                Tile.blocksLineOfSight = cell.blocksLineOfSight;
                Tile.blocksProjectiles = cell.blocksProjectiles;
                Tile.coverBonus = cell.coverBonus;
                Tile.deployZone = cell.deployZone;
                Tile.hazardType = cell.hazardType;
                if (cell.tags != null)
                {
                    Tile.tags.AddRange(cell.tags);
                }
            }

            public BattleTestTile Tile { get; }

            public void Dispose()
            {
                UnityEngine.Object.DestroyImmediate(gameObject);
            }
        }
    }
}
