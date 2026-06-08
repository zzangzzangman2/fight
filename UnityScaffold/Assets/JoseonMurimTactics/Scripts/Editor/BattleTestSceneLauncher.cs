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
                Unit("yeon_sowol", "Yeon Sowol", Faction.Ally, "park_sungjun_visual.asset", new Vector2Int(1, 1), 34, 4, 14, 4, 1, 6, 15, 6, 10, "Moonlit Sword", 1, 1, 2, 5, 2, BattleSpecialEffect.Strike),
                Unit("seo_arin", "Seo Arin", Faction.Ally, "yun_seohwa_visual.asset", new Vector2Int(1, 3), 28, 4, 11, 5, 1, 5, 13, 4, 7, "Pure Remedy", 3, 1, 2, 10, 0, BattleSpecialEffect.Heal),
                Unit("nam_soyu", "Nam Soyu", Faction.Ally, "baek_ryeon_visual.asset", new Vector2Int(2, 5), 26, 3, 17, 5, 1, 7, 13, 5, 9, "Bright Rush", 1, 1, 2, 4, 3, BattleSpecialEffect.BreakGuard),
                Unit("mok_hyang", "Mok Hyang", Faction.Enemy, "han_biyeon_visual.asset", new Vector2Int(9, 1), 28, 4, 16, 5, 3, 6, 14, 4, 8, "Silent Needle", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
                Unit("han_jiyu", "Han Jiyu", Faction.Enemy, "strategist_visual.asset", new Vector2Int(9, 6), 24, 4, 13, 4, 4, 5, 13, 4, 7, "Cold Reading", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark),
                Unit("kang_hana", "Kang Hana", Faction.Enemy, "do_arin_visual.asset", new Vector2Int(10, 4), 36, 3, 12, 4, 1, 6, 16, 6, 11, "Iron Fist", 1, 1, 2, 6, 2, BattleSpecialEffect.BreakGuard)
            };
        }

        private static BattleTestUnitDefinition Unit(
            string id,
            string displayName,
            Faction faction,
            string visualFile,
            Vector2Int startCell,
            int maxHp,
            int maxInner,
            int initiative,
            int moveRange,
            int attackRange,
            int attackBonus,
            int defense,
            int damageMin,
            int damageMax,
            string specialName,
            int specialRange,
            int specialCost,
            int specialCooldown,
            int specialPower,
            int specialAttackBonus,
            BattleSpecialEffect specialEffect)
        {
            return new BattleTestUnitDefinition
            {
                id = id,
                displayName = displayName,
                faction = faction,
                visual = LoadVisual(visualFile),
                startCell = startCell,
                maxHp = maxHp,
                maxInner = maxInner,
                initiative = initiative,
                moveRange = moveRange,
                attackRange = attackRange,
                attackBonus = attackBonus,
                defense = defense,
                damageMin = damageMin,
                damageMax = damageMax,
                specialName = specialName,
                specialRange = specialRange,
                specialCost = specialCost,
                specialCooldown = specialCooldown,
                specialPower = specialPower,
                specialAttackBonus = specialAttackBonus,
                specialEffect = specialEffect
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
