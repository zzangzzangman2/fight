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
                Unit("park_sungjun", "Park Sungjun", Faction.Ally, "park_sungjun_visual.asset", new Vector2Int(1, 1), SkillStyle.Sword, 38, 4, 4, 1, "Sagunjageom", 6, 15, 6, 10, true, 1, 0, "Baekdu Flash", 1, 1, 2, 6, 2, BattleSpecialEffect.Strike),
                Unit("yun_seohwa", "Yun Seohwa", Faction.Ally, "yun_seohwa_visual.asset", new Vector2Int(2, 2), SkillStyle.Sword, 30, 4, 5, 1, "Moon Reflection", 7, 14, 5, 9, true, 1, 0, "Read Sword Path", 1, 1, 2, 5, 3, BattleSpecialEffect.BreakGuard),
                Unit("baek_ryeon", "Baek Ryeon", Faction.Ally, "baek_ryeon_visual.asset", new Vector2Int(1, 4), SkillStyle.Ice, 28, 5, 4, 2, "Ice Palm", 6, 13, 4, 8, true, 2, 1, "Frost Seal", 3, 1, 2, 4, 2, BattleSpecialEffect.Freeze),
                Unit("han_biyeon", "Han Biyeon", Faction.Ally, "han_biyeon_visual.asset", new Vector2Int(2, 5), SkillStyle.Poison, 27, 4, 5, 3, "Hidden Needle", 6, 14, 4, 8, false, 1, 0, "Poison Needle", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
                Unit("do_arin", "Do Arin", Faction.Ally, "do_arin_visual.asset", new Vector2Int(2, 6), SkillStyle.Blade, 36, 3, 4, 1, "Mountain Palm", 6, 16, 6, 11, true, 1, 0, "Iron Shoulder", 1, 1, 2, 6, 2, BattleSpecialEffect.BreakGuard),
                Unit("central_swordsman_a", "Central Swordsman", Faction.Enemy, "strategist_visual.asset", new Vector2Int(8, 1), SkillStyle.Sword, 28, 3, 4, 1, "Orthodox Sword", 5, 14, 4, 8, true, 1, 0, "Guard Cut", 1, 1, 2, 3, 1, BattleSpecialEffect.Strike),
                Unit("central_swordsman_b", "Central Swordsman", Faction.Enemy, "park_sungjun_visual.asset", new Vector2Int(9, 2), SkillStyle.Sword, 28, 3, 4, 1, "Orthodox Sword", 5, 14, 4, 8, true, 1, 0, "Guard Cut", 1, 1, 2, 3, 1, BattleSpecialEffect.Strike),
                Unit("qingcheng_spearman", "Qingcheng Spearman", Faction.Enemy, "yun_seohwa_visual.asset", new Vector2Int(8, 5), SkillStyle.Spear, 32, 3, 4, 2, "Long Spear", 6, 14, 5, 9, true, 2, 0, "Spear Lock", 2, 1, 2, 4, 2, BattleSpecialEffect.BreakGuard),
                Unit("sichuan_poisoner", "Sichuan Poisoner", Faction.Enemy, "han_biyeon_visual.asset", new Vector2Int(9, 6), SkillStyle.Poison, 24, 4, 4, 3, "Poison Dart", 5, 13, 3, 7, false, 1, 0, "Miasma Needle", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
                Unit("central_inspector", "Central Inspector", Faction.Enemy, "strategist_visual.asset", new Vector2Int(10, 5), SkillStyle.Mind, 34, 4, 4, 3, "Judgment Seal", 6, 15, 5, 9, true, 2, 1, "Mandate Seal", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark)
            };
        }

        private static BattleTestUnitDefinition Unit(
            string id,
            string displayName,
            Faction faction,
            string visualFile,
            Vector2Int startCell,
            SkillStyle style,
            int maxHp,
            int maxInner,
            int moveRange,
            int attackRange,
            string basicAttackName,
            int attackBonus,
            int defense,
            int damageMin,
            int damageMax,
            bool canCounter,
            int counterRange,
            int counterInnerCost,
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
                style = style,
                maxHp = maxHp,
                maxInner = maxInner,
                moveRange = moveRange,
                attackRange = attackRange,
                basicAttackName = basicAttackName,
                attackBonus = attackBonus,
                defense = defense,
                damageMin = damageMin,
                damageMax = damageMax,
                canCounter = canCounter,
                counterRange = counterRange,
                counterInnerCost = counterInnerCost,
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
