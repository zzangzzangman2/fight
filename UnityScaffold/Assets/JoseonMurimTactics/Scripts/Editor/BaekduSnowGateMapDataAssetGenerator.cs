using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
    public static class BaekduSnowGateMapDataAssetGenerator
    {
        private const string OutputFolder =
            "Assets/JoseonMurimTactics/ScriptableObjects/BattleMaps";
        private const string OutputPath = OutputFolder + "/baekdu_snow_gate_data.asset";

        [MenuItem("Joseon Murim Tactics/Battle Maps/Generate Baekdu Snow Gate BattleMapData")]
        public static void Generate()
        {
            EnsureFolder(OutputFolder);
            BattleMapData mapData = AssetDatabase.LoadAssetAtPath<BattleMapData>(OutputPath);
            if (mapData == null)
            {
                mapData = ScriptableObject.CreateInstance<BattleMapData>();
                AssetDatabase.CreateAsset(mapData, OutputPath);
            }

            mapData.mapId = BattleMapRuntimeCatalog.BaekduSnowGateMapId;
            mapData.displayName = "백두산 설문 관문전";
            mapData.oneLineConcept = "눈 덮인 관문 계단을 따라 오르는 첫 SRPG 기준 전장";
            mapData.briefingText =
                "보이는 석도와 계단은 이동 가능, 벽/펜스/절벽/빙판 외곽은 명시 차단된다.";
            mapData.origin = Vector2Int.zero;
            mapData.size = new Vector2Int(
                BaekduSnowGateBattleMapData.Width,
                BaekduSnowGateBattleMapData.Height
            );
            mapData.tileWidth = 1.16f;
            mapData.tileHeight = 0.62f;
            mapData.isIsometric = true;
            mapData.cells.Clear();

            foreach (BattleMapRuntimeCell runtime in BaekduSnowGateBattleMapData.Cells())
            {
                mapData.cells.Add(ToBattleCell(runtime));
            }

            EditorUtility.SetDirty(mapData);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[BaekduSnowGateMapDataAssetGenerator] Generated " + OutputPath);
        }

        private static BattleCellData ToBattleCell(BattleMapRuntimeCell runtime)
        {
            return new BattleCellData
            {
                cell = runtime.cell,
                displayName = $"{runtime.terrainType} {runtime.cell.x},{runtime.cell.y}",
                worldPosition = GridToWorld(runtime.cell),
                terrainType = runtime.terrainType,
                moveCost = runtime.moveCost,
                walkable = runtime.walkable,
                blocksMovement = !runtime.walkable,
                blocksLineOfSight = runtime.blocksLineOfSight,
                blocksProjectiles = runtime.blocksProjectiles,
                isChokePoint = runtime.isChokePoint,
                capacity = runtime.isChokePoint ? 1 : 2,
                elevation = runtime.elevation,
                coverBonus = runtime.coverBonus,
                coverType = CoverFromBonus(runtime.coverBonus),
                hazardType = runtime.hazardType,
                zoneId = runtime.zoneId,
                laneId = runtime.laneId,
                deployZone = runtime.deployZone,
                occupyAllowed = runtime.occupyAllowed,
                visualTileKey = runtime.terrainType.ToString(),
                decorSetKey = runtime.tacticalNote,
                tags = new List<string>(runtime.tags),
            };
        }

        private static Vector3 GridToWorld(Vector2Int cell)
        {
            const float tileWidth = 1.16f;
            const float tileHeight = 0.62f;
            return new Vector3(
                (cell.x - cell.y) * tileWidth * 0.5f,
                (cell.x + cell.y) * tileHeight * 0.5f,
                0f
            );
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

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

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
    }
}
