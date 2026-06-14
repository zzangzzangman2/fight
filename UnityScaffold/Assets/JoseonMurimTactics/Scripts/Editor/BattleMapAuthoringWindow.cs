using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
    public sealed class BattleMapAuthoringWindow : EditorWindow
    {
        private BattleMapData mapData;
        private Vector2 scroll;
        private TerrainType brushTerrain = TerrainType.Road;
        private bool brushWalkable = true;
        private bool brushBlocksLineOfSight;
        private bool brushBlocksProjectiles;
        private bool brushOccupyAllowed = true;
        private int brushElevation;
        private int brushMoveCost = 1;
        private int brushCoverBonus;
        private int brushDeployZone;
        private string brushTags = string.Empty;
        private Vector2Int selectedCell = new Vector2Int(-1, -1);

        [MenuItem("Joseon Murim Tactics/Battle Maps/Battle Map Authoring Window")]
        public static void Open()
        {
            GetWindow<BattleMapAuthoringWindow>("Battle Map Authoring");
        }

        private void OnGUI()
        {
            mapData = (BattleMapData)
                EditorGUILayout.ObjectField("BattleMapData", mapData, typeof(BattleMapData), false);
            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("New 16x12"))
                {
                    CreateNewMapData();
                }

                if (GUILayout.Button("Normalize Cells") && mapData != null)
                {
                    NormalizeCells();
                }

                if (GUILayout.Button("Save") && mapData != null)
                {
                    EditorUtility.SetDirty(mapData);
                    AssetDatabase.SaveAssets();
                }
            }

            if (mapData == null)
            {
                EditorGUILayout.HelpBox(
                    "BattleMapData asset을 선택하거나 New 16x12를 누르세요.",
                    MessageType.Info
                );
                return;
            }

            DrawBrush();
            EditorGUILayout.Space(8f);
            DrawGrid();
            DrawSelectedCellInfo();
        }

        private void DrawBrush()
        {
            EditorGUILayout.LabelField("Brush", EditorStyles.boldLabel);
            brushTerrain = (TerrainType)EditorGUILayout.EnumPopup("Terrain", brushTerrain);
            brushWalkable = EditorGUILayout.Toggle("Walkable", brushWalkable);
            brushOccupyAllowed = EditorGUILayout.Toggle("Occupy Allowed", brushOccupyAllowed);
            brushBlocksLineOfSight = EditorGUILayout.Toggle("Blocks LOS", brushBlocksLineOfSight);
            brushBlocksProjectiles = EditorGUILayout.Toggle(
                "Blocks Projectiles",
                brushBlocksProjectiles
            );
            brushElevation = EditorGUILayout.IntSlider("Elevation", brushElevation, 0, 5);
            brushMoveCost = EditorGUILayout.IntSlider("Move Cost", brushMoveCost, 1, 99);
            brushCoverBonus = EditorGUILayout.IntSlider("Cover Bonus", brushCoverBonus, 0, 4);
            brushDeployZone = EditorGUILayout.IntSlider("Deploy Zone", brushDeployZone, 0, 4);
            brushTags = EditorGUILayout.TextField("Tags CSV", brushTags);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Preset: Stairs"))
                {
                    brushTerrain = TerrainType.Stone;
                    brushWalkable = true;
                    brushOccupyAllowed = true;
                    brushMoveCost = 1;
                    brushTags = "stairs,ramp,visible_path";
                }

                if (GUILayout.Button("Preset: Blocker"))
                {
                    brushTerrain = TerrainType.Wall;
                    brushWalkable = false;
                    brushOccupyAllowed = false;
                    brushMoveCost = 99;
                    brushBlocksLineOfSight = true;
                    brushBlocksProjectiles = true;
                    brushTags = "wall,decorativeBlocker";
                }
            }
        }

        private void DrawGrid()
        {
            Vector2Int size = mapData.size;
            if (size.x <= 0 || size.y <= 0)
            {
                size = new Vector2Int(16, 12);
                mapData.size = size;
            }

            EditorGUILayout.LabelField("Grid", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.Height(390f));
            for (int y = size.y - 1; y >= 0; y--)
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    for (int x = 0; x < size.x; x++)
                    {
                        BattleCellData cell = GetOrCreateCell(new Vector2Int(x, y));
                        GUI.backgroundColor = CellColor(cell);
                        string label =
                            $"{x},{y}\n{(cell.walkable && cell.occupyAllowed ? "W" : "B")} E{cell.elevation}";
                        if (GUILayout.Button(label, GUILayout.Width(58f), GUILayout.Height(42f)))
                        {
                            selectedCell = cell.cell;
                            ApplyBrush(cell);
                        }
                    }
                }
            }

            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndScrollView();
        }

        private void DrawSelectedCellInfo()
        {
            if (selectedCell.x < 0)
            {
                return;
            }

            BattleCellData cell = FindCell(selectedCell);
            if (cell == null)
            {
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Selected", EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                $"{cell.cell} {cell.terrainType} W:{cell.walkable} Occupy:{cell.occupyAllowed} E:{cell.elevation} M:{cell.moveCost} LOS:{cell.blocksLineOfSight} C:{cell.coverBonus} D:{cell.deployZone} Tags:{string.Join(",", cell.tags)}"
            );
        }

        private void ApplyBrush(BattleCellData cell)
        {
            Undo.RecordObject(mapData, "Paint Battle Map Cell");
            cell.terrainType = brushTerrain;
            cell.walkable = brushWalkable;
            cell.blocksMovement = !brushWalkable;
            cell.occupyAllowed = brushOccupyAllowed;
            cell.blocksLineOfSight = brushBlocksLineOfSight;
            cell.blocksProjectiles = brushBlocksProjectiles;
            cell.elevation = brushElevation;
            cell.moveCost = brushWalkable ? Mathf.Max(1, brushMoveCost) : 99;
            cell.coverBonus = brushCoverBonus;
            cell.coverType = CoverFromBonus(brushCoverBonus);
            cell.deployZone = brushDeployZone;
            cell.tags = ParseTags(brushTags);
            EditorUtility.SetDirty(mapData);
        }

        private void CreateNewMapData()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "Create BattleMapData",
                "baekdu_snow_gate_data",
                "asset",
                "Create BattleMapData asset"
            );
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            mapData = CreateInstance<BattleMapData>();
            mapData.mapId = BattleMapRuntimeCatalog.BaekduSnowGateMapId;
            mapData.displayName = "백두산 설문 관문전";
            mapData.size = new Vector2Int(16, 12);
            NormalizeCells();
            AssetDatabase.CreateAsset(mapData, path);
            AssetDatabase.SaveAssets();
        }

        private void NormalizeCells()
        {
            Undo.RecordObject(mapData, "Normalize Battle Map Cells");
            Vector2Int size =
                mapData.size.x <= 0 || mapData.size.y <= 0 ? new Vector2Int(16, 12) : mapData.size;
            for (int y = 0; y < size.y; y++)
            {
                for (int x = 0; x < size.x; x++)
                {
                    GetOrCreateCell(new Vector2Int(x, y));
                }
            }

            EditorUtility.SetDirty(mapData);
        }

        private BattleCellData GetOrCreateCell(Vector2Int cell)
        {
            BattleCellData existing = FindCell(cell);
            if (existing != null)
            {
                return existing;
            }

            BattleCellData created = new BattleCellData
            {
                cell = cell,
                displayName = $"Cell {cell.x},{cell.y}",
            };
            if (
                BattleMapRuntimeCatalog.TryGetCell(
                    BattleTestMapVariant.BaekduSnowGate,
                    cell,
                    out BattleMapRuntimeCell runtime
                )
            )
            {
                CopyRuntimeCell(runtime, created);
            }

            mapData.cells.Add(created);
            return created;
        }

        private BattleCellData FindCell(Vector2Int cell)
        {
            if (mapData == null || mapData.cells == null)
            {
                return null;
            }

            for (int i = 0; i < mapData.cells.Count; i++)
            {
                if (mapData.cells[i] != null && mapData.cells[i].cell == cell)
                {
                    return mapData.cells[i];
                }
            }

            return null;
        }

        private static void CopyRuntimeCell(BattleMapRuntimeCell source, BattleCellData target)
        {
            target.terrainType = source.terrainType;
            target.elevation = source.elevation;
            target.moveCost = source.moveCost;
            target.walkable = source.walkable;
            target.blocksMovement = !source.walkable;
            target.blocksLineOfSight = source.blocksLineOfSight;
            target.blocksProjectiles = source.blocksProjectiles;
            target.occupyAllowed = source.occupyAllowed;
            target.isChokePoint = source.isChokePoint;
            target.coverBonus = source.coverBonus;
            target.coverType = CoverFromBonus(source.coverBonus);
            target.hazardType = source.hazardType;
            target.deployZone = source.deployZone;
            target.zoneId = source.zoneId;
            target.laneId = source.laneId;
            target.decorSetKey = source.tacticalNote;
            target.tags = new List<string>(source.tags);
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

        private static List<string> ParseTags(string csv)
        {
            List<string> tags = new List<string>();
            if (string.IsNullOrEmpty(csv))
            {
                return tags;
            }

            string[] parts = csv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                string tag = parts[i].Trim();
                if (!string.IsNullOrEmpty(tag))
                {
                    tags.Add(tag);
                }
            }

            return tags;
        }

        private static Color CellColor(BattleCellData cell)
        {
            if (cell == null || !cell.walkable || !cell.occupyAllowed)
            {
                return new Color(0.52f, 0.20f, 0.18f, 1f);
            }

            if (cell.deployZone > 0)
            {
                return new Color(0.16f, 0.58f, 0.68f, 1f);
            }

            if (
                cell.tags != null
                && cell.tags.Exists(tag =>
                    string.Equals(tag, "stairs", StringComparison.OrdinalIgnoreCase)
                )
            )
            {
                return new Color(0.70f, 0.58f, 0.34f, 1f);
            }

            return new Color(0.24f, 0.42f, 0.56f, 1f);
        }
    }
}
