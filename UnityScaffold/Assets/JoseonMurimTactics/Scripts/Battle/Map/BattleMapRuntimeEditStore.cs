using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace JoseonMurimTactics
{
    public sealed class BattleMapRuntimeCellEdit
    {
        public Vector2Int cell;
        public TerrainType terrainType = TerrainType.Stone;
        public bool walkable = true;
        public bool occupyAllowed = true;
        public int moveCost = 1;
        public int elevation;
        public bool blocksLineOfSight;
        public bool blocksProjectiles;
        public int coverBonus;
        public int deployZone;
        public HazardType hazardType = HazardType.None;
        public string laneId = string.Empty;
        public readonly List<string> tags = new List<string>();

        public static BattleMapRuntimeCellEdit FromTile(BattleTestTile tile)
        {
            BattleMapRuntimeCellEdit edit = new BattleMapRuntimeCellEdit();
            if (tile == null)
            {
                return edit;
            }

            edit.cell = tile.cell;
            edit.terrainType = tile.terrain;
            edit.walkable = tile.walkable;
            edit.occupyAllowed = tile.occupyAllowed;
            edit.moveCost = tile.walkable && tile.occupyAllowed ? Mathf.Max(1, tile.moveCost) : 99;
            edit.elevation = tile.elevation;
            edit.blocksLineOfSight = tile.blocksLineOfSight;
            edit.blocksProjectiles = tile.blocksProjectiles;
            edit.coverBonus = tile.coverBonus;
            edit.deployZone = tile.deployZone;
            edit.hazardType = tile.hazardType;
            edit.laneId = string.IsNullOrEmpty(tile.laneId) ? string.Empty : tile.laneId;
            if (tile.tags != null)
            {
                edit.tags.AddRange(tile.tags);
            }

            return edit;
        }

        public void ApplyTo(BattleMapRuntimeCell cellData)
        {
            if (cellData == null)
            {
                return;
            }

            cellData.terrainType = terrainType;
            cellData.walkable = walkable;
            cellData.occupyAllowed = occupyAllowed;
            cellData.moveCost = walkable && occupyAllowed ? Mathf.Max(1, moveCost) : 99;
            cellData.elevation = elevation;
            cellData.blocksLineOfSight = blocksLineOfSight;
            cellData.blocksProjectiles = blocksProjectiles;
            cellData.coverBonus = coverBonus;
            cellData.deployZone = deployZone;
            cellData.hazardType = hazardType;
            cellData.danger = hazardType != HazardType.None;
            cellData.laneId = string.IsNullOrEmpty(laneId) ? cellData.laneId : laneId;
            cellData.tags.Clear();
            cellData.tags.AddRange(tags);
        }

        public void ApplyTo(BattleTestTile tile)
        {
            if (tile == null)
            {
                return;
            }

            tile.terrain = terrainType;
            tile.walkable = walkable;
            tile.occupyAllowed = occupyAllowed;
            tile.moveCost = walkable && occupyAllowed ? Mathf.Max(1, moveCost) : 99;
            tile.elevation = elevation;
            tile.blocksLineOfSight = blocksLineOfSight;
            tile.blocksProjectiles = blocksProjectiles;
            tile.coverBonus = coverBonus;
            tile.baseCoverBonus = coverBonus;
            tile.deployZone = deployZone;
            tile.hazardType = hazardType;
            tile.danger = hazardType != HazardType.None;
            if (!string.IsNullOrEmpty(laneId))
            {
                tile.laneId = laneId;
            }

            tile.tags.Clear();
            tile.tags.AddRange(tags);
        }
    }

    public static class BattleMapRuntimeEditStore
    {
        public const string RuntimeOverrideFileName =
            "2026-06-14_baekdu_snow_gate_runtime_edits.csv";
        public const string EnemySpawnTag = "enemy_spawn";
        public const string RuntimeEditedTag = "runtime_edit";

        private const string Header =
            "x,y,terrainType,walkable,occupyAllowed,moveCost,elevation,blocksLineOfSight,blocksProjectiles,coverBonus,deployZone,hazardType,tags,laneId";

        public static string PersistentOverridePath
        {
            get { return Path.Combine(Application.persistentDataPath, RuntimeOverrideFileName); }
        }

        public static string RepoOverridePath
        {
            get { return Path.Combine(RepoRequestFolder(), RuntimeOverrideFileName); }
        }

        public static string PersistentOverridePathFor(BattleTestMapVariant variant)
        {
            return Path.Combine(
                Application.persistentDataPath,
                RuntimeOverrideFileNameFor(variant)
            );
        }

        public static string RepoOverridePathFor(BattleTestMapVariant variant)
        {
            return Path.Combine(RepoRequestFolder(), RuntimeOverrideFileNameFor(variant));
        }

        public static string RuntimeOverrideFileNameFor(BattleTestMapVariant variant)
        {
            if (variant == BattleTestMapVariant.BaekduSnowGate)
            {
                return RuntimeOverrideFileName;
            }

            return $"2026-06-14_{VariantSlug(variant)}_runtime_edits.csv";
        }

        public static string VariantSlug(BattleTestMapVariant variant)
        {
            switch (variant)
            {
                case BattleTestMapVariant.BaekduSnowGate:
                    return "baekdu_snow_gate";
                case BattleTestMapVariant.BaekduMountainSnowfield:
                    return "baekdu_mountain_snowfield";
                case BattleTestMapVariant.BanditLair:
                    return "bandit_lair";
                case BattleTestMapVariant.WolfPass:
                    return "wolf_pass";
                case BattleTestMapVariant.TigerRavine:
                    return "tiger_ravine";
                case BattleTestMapVariant.LeopardCliff:
                    return "leopard_cliff";
                case BattleTestMapVariant.SeorakPassRescue:
                    return "seorak_pass_rescue";
                default:
                    return variant.ToString().ToLowerInvariant();
            }
        }

        public static string RepoRequestFolder()
        {
            string repoRoot = FindRepoRoot();
            if (!string.IsNullOrEmpty(repoRoot))
            {
                return Path.Combine(repoRoot, "codex-requests");
            }

            return Path.Combine(Directory.GetCurrentDirectory(), "codex-requests");
        }

        public static bool TryLoadBestOverride(
            out List<BattleMapRuntimeCellEdit> edits,
            out string path,
            out string message
        )
        {
            return TryLoadBestOverride(
                BattleTestMapVariant.BaekduSnowGate,
                out edits,
                out path,
                out message
            );
        }

        public static bool TryLoadBestOverride(
            BattleTestMapVariant variant,
            out List<BattleMapRuntimeCellEdit> edits,
            out string path,
            out string message
        )
        {
            string[] candidates = new[]
            {
                RepoOverridePathFor(variant),
                PersistentOverridePathFor(variant),
            };
            for (int i = 0; i < candidates.Length; i++)
            {
                string candidate = candidates[i];
                if (!File.Exists(candidate))
                {
                    continue;
                }

                if (TryLoadOverridesFromPath(candidate, out edits, out message))
                {
                    path = candidate;
                    return true;
                }
            }

            edits = new List<BattleMapRuntimeCellEdit>();
            path = string.Empty;
            message = "No runtime edit CSV found.";
            return false;
        }

        public static bool HasSavedOverride()
        {
            return HasSavedOverride(BattleTestMapVariant.BaekduSnowGate);
        }

        public static bool HasSavedOverride(BattleTestMapVariant variant)
        {
            return File.Exists(RepoOverridePathFor(variant))
                || File.Exists(PersistentOverridePathFor(variant));
        }

        public static bool TryLoadOverridesFromPath(
            string path,
            out List<BattleMapRuntimeCellEdit> edits,
            out string message
        )
        {
            edits = new List<BattleMapRuntimeCellEdit>();
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
            {
                message = "Runtime edit CSV does not exist.";
                return false;
            }

            string[] lines = File.ReadAllLines(path, Encoding.UTF8);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (
                    string.IsNullOrWhiteSpace(line)
                    || line.StartsWith("x,", StringComparison.Ordinal)
                )
                {
                    continue;
                }

                string[] parts = SplitCsvLine(line);
                if (parts.Length < 12)
                {
                    message = $"Runtime edit CSV line {i + 1} has too few columns.";
                    return false;
                }

                BattleMapRuntimeCellEdit edit = new BattleMapRuntimeCellEdit
                {
                    cell = new Vector2Int(ParseInt(parts[0]), ParseInt(parts[1])),
                    terrainType = ParseEnum(parts[2], TerrainType.Stone),
                    walkable = ParseBool(parts[3]),
                    occupyAllowed = ParseBool(parts[4]),
                    moveCost = ParseInt(parts[5]),
                    elevation = ParseInt(parts[6]),
                    blocksLineOfSight = ParseBool(parts[7]),
                    blocksProjectiles = ParseBool(parts[8]),
                    coverBonus = ParseInt(parts[9]),
                    deployZone = ParseInt(parts[10]),
                    hazardType = ParseEnum(parts[11], HazardType.None),
                    laneId = parts.Length > 13 ? parts[13] : string.Empty,
                };

                if (parts.Length > 12 && !string.IsNullOrWhiteSpace(parts[12]))
                {
                    string[] tags = parts[12].Split('|');
                    for (int tagIndex = 0; tagIndex < tags.Length; tagIndex++)
                    {
                        string tag = tags[tagIndex].Trim();
                        if (!string.IsNullOrEmpty(tag))
                        {
                            edit.tags.Add(tag);
                        }
                    }
                }

                edits.Add(edit);
            }

            message = $"Loaded {edits.Count} runtime edit cells.";
            return edits.Count > 0;
        }

        public static void SaveOverrides(
            IEnumerable<BattleTestTile> tiles,
            out string repoPath,
            out string persistentPath
        )
        {
            SaveOverrides(
                BattleTestMapVariant.BaekduSnowGate,
                tiles,
                out repoPath,
                out persistentPath
            );
        }

        public static void SaveOverrides(
            BattleTestMapVariant variant,
            IEnumerable<BattleTestTile> tiles,
            out string repoPath,
            out string persistentPath
        )
        {
            List<BattleTestTile> ordered = new List<BattleTestTile>();
            if (tiles != null)
            {
                foreach (BattleTestTile tile in tiles)
                {
                    if (tile != null)
                    {
                        ordered.Add(tile);
                    }
                }
            }

            ordered.Sort(
                (left, right) =>
                {
                    int yCompare = left.cell.y.CompareTo(right.cell.y);
                    return yCompare != 0 ? yCompare : left.cell.x.CompareTo(right.cell.x);
                }
            );

            string csv = BuildCsv(ordered);
            repoPath = RepoOverridePathFor(variant);
            persistentPath = PersistentOverridePathFor(variant);
            WriteCsv(repoPath, csv);
            WriteCsv(persistentPath, csv);
        }

        private static string BuildCsv(List<BattleTestTile> tiles)
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine(Header);
            for (int i = 0; i < tiles.Count; i++)
            {
                BattleMapRuntimeCellEdit edit = BattleMapRuntimeCellEdit.FromTile(tiles[i]);
                builder.Append(edit.cell.x.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(edit.cell.y.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(Escape(edit.terrainType.ToString())).Append(',');
                builder.Append(edit.walkable).Append(',');
                builder.Append(edit.occupyAllowed).Append(',');
                builder.Append(edit.moveCost.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(edit.elevation.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(edit.blocksLineOfSight).Append(',');
                builder.Append(edit.blocksProjectiles).Append(',');
                builder.Append(edit.coverBonus.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(edit.deployZone.ToString(CultureInfo.InvariantCulture)).Append(',');
                builder.Append(Escape(edit.hazardType.ToString())).Append(',');
                builder.Append(Escape(string.Join("|", edit.tags))).Append(',');
                builder.Append(Escape(edit.laneId));
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static void WriteCsv(string path, string csv)
        {
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(path, csv, new UTF8Encoding(false));
        }

        private static string FindRepoRoot()
        {
            string[] starts = new[] { Application.dataPath, Directory.GetCurrentDirectory() };
            for (int i = 0; i < starts.Length; i++)
            {
                string start = starts[i];
                if (string.IsNullOrEmpty(start))
                {
                    continue;
                }

                DirectoryInfo directory = new DirectoryInfo(start);
                for (int depth = 0; directory != null && depth < 12; depth++)
                {
                    if (
                        string.Equals(
                            directory.Name,
                            "UnityScaffold",
                            StringComparison.OrdinalIgnoreCase
                        )
                        && directory.Parent != null
                    )
                    {
                        return directory.Parent.FullName;
                    }

                    if (
                        Directory.Exists(
                            Path.Combine(directory.FullName, "UnityScaffold", "Assets")
                        )
                    )
                    {
                        return directory.FullName;
                    }

                    directory = directory.Parent;
                }
            }

            return string.Empty;
        }

        private static string[] SplitCsvLine(string line)
        {
            List<string> parts = new List<string>();
            StringBuilder current = new StringBuilder();
            bool quoted = false;
            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '"')
                {
                    if (quoted && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        quoted = !quoted;
                    }
                }
                else if (c == ',' && !quoted)
                {
                    parts.Add(current.ToString());
                    current.Length = 0;
                }
                else
                {
                    current.Append(c);
                }
            }

            parts.Add(current.ToString());
            return parts.ToArray();
        }

        private static bool ParseBool(string value)
        {
            if (bool.TryParse(value, out bool parsed))
            {
                return parsed;
            }

            return string.Equals(value, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(value, "yes", StringComparison.OrdinalIgnoreCase);
        }

        private static int ParseInt(string value)
        {
            return int.TryParse(
                value,
                NumberStyles.Integer,
                CultureInfo.InvariantCulture,
                out int parsed
            )
                ? parsed
                : 0;
        }

        private static T ParseEnum<T>(string value, T fallback)
            where T : struct
        {
            return Enum.TryParse(value, true, out T parsed) ? parsed : fallback;
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
    }
}
