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
            controller.width = 18;
            controller.height = 12;
            controller.tileWidth = 1.16f;
            controller.tileHeight = 0.62f;
            controller.unitDefinitions = new[]
            {
                Unit("park_sungjun", "Park Sungjun", Faction.Ally, "park_sungjun_visual.asset", new Vector2Int(2, 2), SkillStyle.Sword, 42, 5, 4, 1, "Sagunjageom", 7, 15, 7, 11, true, 1, 0, "Baekdu Flash", 1, 1, 2, 7, 2, BattleSpecialEffect.Strike),
                Unit("yun_seohwa", "Yun Seohwa", Faction.Ally, "yun_seohwa_visual.asset", new Vector2Int(3, 3), SkillStyle.Sword, 32, 4, 5, 1, "Moon Reflection", 8, 14, 5, 9, true, 1, 0, "Read Sword Path", 1, 1, 2, 5, 3, BattleSpecialEffect.BreakGuard),
                Unit("baek_ryeon", "Baek Ryeon", Faction.Ally, "baek_ryeon_visual.asset", new Vector2Int(2, 6), SkillStyle.Ice, 30, 5, 4, 2, "Ice Palm", 6, 13, 4, 8, true, 2, 1, "Frost Seal", 3, 1, 2, 4, 2, BattleSpecialEffect.Freeze),
                Unit("han_biyeon", "Han Biyeon", Faction.Ally, "han_biyeon_visual.asset", new Vector2Int(3, 8), SkillStyle.Poison, 29, 4, 5, 3, "Hidden Needle", 6, 14, 4, 8, false, 1, 0, "Poison Needle", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
                Unit("do_arin", "Do Arin", Faction.Ally, "do_arin_visual.asset", new Vector2Int(4, 7), SkillStyle.Blade, 38, 3, 4, 1, "Mountain Palm", 6, 16, 6, 11, true, 1, 0, "Iron Shoulder", 1, 1, 2, 6, 2, BattleSpecialEffect.BreakGuard),
                Unit("central_swordsman_a", "Central Swordsman", Faction.Enemy, "strategist_visual.asset", new Vector2Int(12, 3), SkillStyle.Sword, 30, 3, 4, 1, "Orthodox Sword", 5, 14, 4, 8, true, 1, 0, "Guard Cut", 1, 1, 2, 3, 1, BattleSpecialEffect.Strike),
                Unit("central_swordsman_b", "Central Swordsman", Faction.Enemy, "park_sungjun_visual.asset", new Vector2Int(13, 5), SkillStyle.Sword, 30, 3, 4, 1, "Orthodox Sword", 5, 14, 4, 8, true, 1, 0, "Guard Cut", 1, 1, 2, 3, 1, BattleSpecialEffect.Strike),
                Unit("qingcheng_spearman", "Qingcheng Spearman", Faction.Enemy, "yun_seohwa_visual.asset", new Vector2Int(12, 8), SkillStyle.Spear, 34, 3, 4, 2, "Long Spear", 6, 14, 5, 9, true, 2, 0, "Spear Lock", 2, 1, 2, 4, 2, BattleSpecialEffect.BreakGuard),
                Unit("sichuan_poisoner", "Sichuan Poisoner", Faction.Enemy, "han_biyeon_visual.asset", new Vector2Int(14, 9), SkillStyle.Poison, 26, 4, 4, 3, "Poison Dart", 5, 13, 3, 7, false, 1, 0, "Miasma Needle", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
                Unit("law_record_keeper", "Law Record Keeper", Faction.Enemy, "baek_ryeon_visual.asset", new Vector2Int(15, 4), SkillStyle.Mind, 24, 4, 4, 3, "Edict Brush", 5, 13, 3, 6, false, 1, 0, "Confucian Seal", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark),
                Unit("bodyguard_expert", "Bodyguard Expert", Faction.Enemy, "do_arin_visual.asset", new Vector2Int(15, 8), SkillStyle.Blade, 36, 3, 4, 1, "Heavy Blade", 6, 16, 6, 10, true, 1, 0, "Shield Bash", 1, 1, 2, 5, 2, BattleSpecialEffect.BreakGuard),
                Unit("central_inspector", "Central Inspector", Faction.Enemy, "strategist_visual.asset", new Vector2Int(16, 6), SkillStyle.Mind, 38, 4, 4, 3, "Judgment Seal", 6, 15, 5, 9, true, 2, 1, "Mandate Seal", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark)
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
                agility = 8 + moveRange + attackBonus - (attackRange > 1 ? 1 : 0),
                moveRange = moveRange,
                attackMinRange = 1,
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
                specialMinRange = 1,
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
