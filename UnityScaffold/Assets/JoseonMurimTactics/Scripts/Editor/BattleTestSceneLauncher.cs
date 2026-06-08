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
                Unit("park_sungjun", "박성준", Faction.Ally, "park_sungjun_visual.asset", new Vector2Int(2, 2), SkillStyle.Sword, 42, 5, 4, 1, "사군자검", 7, 15, 7, 11, true, 1, 0, "백두일섬", 1, 1, 2, 7, 2, BattleSpecialEffect.Strike),
                Unit("yun_seohwa", "윤서화", Faction.Ally, "yun_seohwa_visual.asset", new Vector2Int(3, 3), SkillStyle.Sword, 32, 4, 5, 1, "월하반조검", 8, 14, 5, 9, true, 1, 0, "검로재기", 1, 1, 2, 5, 3, BattleSpecialEffect.BreakGuard),
                Unit("baek_ryeon", "백련", Faction.Ally, "baek_ryeon_visual.asset", new Vector2Int(2, 6), SkillStyle.Ice, 30, 5, 4, 2, "빙백장", 6, 13, 4, 8, true, 2, 1, "한설빙로", 3, 1, 2, 4, 2, BattleSpecialEffect.Freeze),
                Unit("han_biyeon", "한비연", Faction.Ally, "han_biyeon_visual.asset", new Vector2Int(3, 8), SkillStyle.Poison, 29, 4, 5, 3, "비화독침", 6, 14, 4, 8, false, 1, 0, "독무살포", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
                Unit("do_arin", "도아린", Faction.Ally, "do_arin_visual.asset", new Vector2Int(4, 7), SkillStyle.Blade, 38, 3, 4, 1, "파산권", 6, 16, 6, 11, true, 1, 0, "철산고", 1, 1, 2, 6, 2, BattleSpecialEffect.BreakGuard),
                Unit("central_swordsman_a", "정파 검수", Faction.Enemy, "strategist_visual.asset", new Vector2Int(12, 3), SkillStyle.Sword, 30, 3, 4, 1, "정파검", 5, 14, 4, 8, true, 1, 0, "수비 베기", 1, 1, 2, 3, 1, BattleSpecialEffect.Strike),
                Unit("central_swordsman_b", "정파 검수", Faction.Enemy, "park_sungjun_visual.asset", new Vector2Int(13, 5), SkillStyle.Sword, 30, 3, 4, 1, "정파검", 5, 14, 4, 8, true, 1, 0, "수비 베기", 1, 1, 2, 3, 1, BattleSpecialEffect.Strike),
                Unit("qingcheng_spearman", "청성 장창수", Faction.Enemy, "yun_seohwa_visual.asset", new Vector2Int(12, 8), SkillStyle.Spear, 34, 3, 4, 2, "장창 찌르기", 6, 14, 5, 9, true, 2, 0, "창로 봉쇄", 2, 1, 2, 4, 2, BattleSpecialEffect.BreakGuard),
                Unit("sichuan_poisoner", "사천 독공술사", Faction.Enemy, "han_biyeon_visual.asset", new Vector2Int(14, 9), SkillStyle.Poison, 26, 4, 4, 3, "독침", 5, 13, 3, 7, false, 1, 0, "독무침", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
                Unit("law_record_keeper", "감찰 기록관", Faction.Enemy, "baek_ryeon_visual.asset", new Vector2Int(15, 4), SkillStyle.Mind, 24, 4, 4, 3, "칙령필", 5, 13, 3, 6, false, 1, 0, "감찰 인장", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark),
                Unit("bodyguard_expert", "감찰 호위병", Faction.Enemy, "do_arin_visual.asset", new Vector2Int(15, 8), SkillStyle.Blade, 36, 3, 4, 1, "중도 베기", 6, 16, 6, 10, true, 1, 0, "방패 밀기", 1, 1, 2, 5, 2, BattleSpecialEffect.BreakGuard),
                Unit("central_inspector", "중원 감찰사", Faction.Enemy, "strategist_visual.asset", new Vector2Int(16, 6), SkillStyle.Mind, 38, 4, 4, 3, "판결 인장", 6, 15, 5, 9, true, 2, 1, "감찰령", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark)
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
