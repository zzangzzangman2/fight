using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JoseonMurimTactics.Editor
{
    public static class BattleTestSceneLauncher
    {
        public const string ScenePath = "Assets/JoseonMurimTactics/Scenes/BattleTest.unity";

        [MenuItem("Joseon Murim Tactics/Open Battle Test Scene")]
        public static void OpenBattleTestScene()
        {
            RebuildBattleTestScene();
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        public static void RebuildBattleTestScene()
        {
            EnsureFolder("Assets/JoseonMurimTactics/Scenes");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "BattleTest";

            CreateCamera();
            CreateController();

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void CreateCamera()
        {
            GameObject cameraObject = new GameObject("Battle Camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = 4.2f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.075f, 0.065f, 1f);
            cameraObject.transform.position = new Vector3(0f, 2.6f, -10f);
            cameraObject.tag = "MainCamera";
            cameraObject.AddComponent<AudioListener>();
        }

        private static void CreateController()
        {
            GameObject controllerObject = new GameObject("Battle Test Controller");
            BattleTestController controller = controllerObject.AddComponent<BattleTestController>();
            controller.width = 12;
            controller.height = 8;
            controller.tileWidth = 1.16f;
            controller.tileHeight = 0.62f;
            controller.unitDefinitions = new[]
            {
                Unit("park_sungjun", "Park Sungjun", Faction.Ally, "park_sungjun_visual.asset", new Vector2Int(1, 1), 36, 4, 1, 6, 15, 6, 10),
                Unit("yun_seohwa", "Yun Seohwa", Faction.Ally, "yun_seohwa_visual.asset", new Vector2Int(1, 3), 28, 5, 1, 7, 14, 5, 9),
                Unit("baek_ryeon", "Baek Ryeon", Faction.Ally, "baek_ryeon_visual.asset", new Vector2Int(2, 5), 26, 4, 2, 6, 13, 4, 8),
                Unit("han_biyeon", "Han Biyeon", Faction.Enemy, "han_biyeon_visual.asset", new Vector2Int(9, 1), 28, 5, 2, 6, 14, 4, 9),
                Unit("do_arin", "Do Arin", Faction.Enemy, "do_arin_visual.asset", new Vector2Int(10, 4), 34, 4, 1, 6, 15, 6, 10),
                Unit("strategist", "Strategist", Faction.Enemy, "strategist_visual.asset", new Vector2Int(9, 6), 24, 4, 3, 5, 13, 4, 7)
            };
        }

        private static BattleTestUnitDefinition Unit(
            string id,
            string displayName,
            Faction faction,
            string visualFile,
            Vector2Int startCell,
            int maxHp,
            int moveRange,
            int attackRange,
            int attackBonus,
            int defense,
            int damageMin,
            int damageMax)
        {
            return new BattleTestUnitDefinition
            {
                id = id,
                displayName = displayName,
                faction = faction,
                visual = LoadVisual(visualFile),
                startCell = startCell,
                maxHp = maxHp,
                moveRange = moveRange,
                attackRange = attackRange,
                attackBonus = attackBonus,
                defense = defense,
                damageMin = damageMin,
                damageMax = damageMax
            };
        }

        private static CharacterVisualData LoadVisual(string fileName)
        {
            string path = "Assets/JoseonMurimTactics/Art/Characters/VisualData/" + fileName;
            return AssetDatabase.LoadAssetAtPath<CharacterVisualData>(path);
        }

        private static void EnsureFolder(string folder)
        {
            string[] parts = folder.Split('/');
            string current = parts[0];

            for (int i = 1; i < parts.Length; i++)
            {
                string next = $"{current}/{parts[i]}";
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}
