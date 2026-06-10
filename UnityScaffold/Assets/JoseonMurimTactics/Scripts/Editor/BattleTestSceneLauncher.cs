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
        camera.orthographicSize = 5.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.08f, 0.075f, 0.065f, 1f);
        cameraObject.transform.position = new Vector3(1.1f, 4.2f, -10f);
        cameraObject.tag = "MainCamera";
        cameraObject.AddComponent<AudioListener>();
    }

    private static void CreateController()
    {
        GameObject controllerObject = new GameObject("Battle Test Controller");
        BattleTestController controller = controllerObject.AddComponent<BattleTestController>();
        controller.width = 16;
        controller.height = 12;
        controller.tileWidth = 1.16f;
        controller.tileHeight = 0.62f;
        controller.unitDefinitions = new[] {
            Unit("test_swordsman", "청월검 소윤", Faction.Ally,
                 "TestSwordsman/VisualData/test_swordsman_visual.asset", new Vector2Int(7, 8),
                 "청월검문", 16, "ENFP", "청광", "검", "밝고 침착한 실전형 말투", 36, 5, 15, 16, 4, 1,
                 7, 15, 6, 10, "청월섬", 1, 0, 1, 5, 2, BattleSpecialEffect.Mark),
            Unit("baek_ryeon", "백련", Faction.Ally, "SchoolCombat/school_combat_02_visual.asset", new Vector2Int(6, 9), "강원 설악창문",
                 17, "INFJ", "얼음/서리", "창", "차분한 존댓말", 30, 4, 12, 13, 4, 2, 5, 14, 5, 8, "설악빙창", 2, 1, 2,
                 4, 1, BattleSpecialEffect.Freeze),
            Unit("do_arin", "도아린", Faction.Ally, "SchoolCombat/school_combat_03_visual.asset", new Vector2Int(8, 8), "경상 화왕도문", 16,
                 "ESTP", "불", "도", "짧고 거친 직설", 34, 3, 14, 15, 4, 1, 7, 14, 6, 11, "화왕참", 1, 1, 2, 6, 2,
                 BattleSpecialEffect.BreakGuard),
            Unit("jin_seoyul", "진서율", Faction.Ally, "SchoolCombat/school_combat_04_visual.asset", new Vector2Int(10, 8), "경성 천뢰봉문",
                 15, "ENTP", "전기", "봉", "빠르고 장난기 있는 추리형 말투", 24, 4, 18, 19, 5, 2, 6, 12, 4, 7,
                 "천뢰봉무", 2, 1, 2, 4, 3, BattleSpecialEffect.Strike),
            Unit("seo_a", "신서아", Faction.Ally, "SchoolCombat/school_combat_05_visual.asset", new Vector2Int(4, 8), "전라도 남원 화접풍류문",
                 13, "ENFP", "바람/꽃", "부채", "밝고 씩씩한 막내 말투", 24, 5, 13, 14, 5, 3, 5, 13, 4, 7, "꽃바람", 3,
                 1, 2, 0, 0, BattleSpecialEffect.Mark),
            Unit("han_biyeon", "한비연", Faction.Ally, "SchoolCombat/school_combat_06_visual.asset", new Vector2Int(2, 7),
                 "황해도 구월산 흑련암문", 17, "ISTP", "어둠/독", "단검·암기", "짧고 비꼬는 듯한 말투", 27, 4, 16, 17,
                 5, 3, 6, 13, 4, 8, "흑련독침", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
            Unit("iron_wolf_guard_1", "철랑문 검수", Faction.Enemy, "SchoolCombat/school_combat_04_visual.asset",
                 new Vector2Int(7, 1), "철랑문", 24, "ISTJ", "철", "검", "딱딱한 명령조", 30, 3, 12, 12, 4, 1, 5,
                 14, 5, 8, "철랑검", 1, 1, 2, 4, 1, BattleSpecialEffect.Strike),
            Unit("iron_wolf_spear_1", "철랑문 장창수", Faction.Enemy, "SchoolCombat/school_combat_05_visual.asset",
                 new Vector2Int(5, 1), "철랑문", 25, "ESTJ", "철", "장창", "위압적인 말투", 32, 3, 11, 12, 4, 2, 5,
                 15, 5, 9, "쇄랑창", 2, 1, 2, 4, 1, BattleSpecialEffect.BreakGuard),
            Unit("iron_wolf_captain", "철랑문 정찰조장", Faction.Enemy, "SchoolCombat/school_combat_06_visual.asset",
                 new Vector2Int(12, 2), "철랑문", 31, "ENTJ", "압박", "검", "거칠고 얕보는 말투", 38, 4, 13, 13,
                 4, 1, 7, 16, 6, 11, "흑랑표식", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark)
        };
    }

    private static BattleTestUnitDefinition Unit(string id, string displayName, Faction faction, string visualFile,
                                                 Vector2Int startCell, string sectName, int age, string mbti,
                                                 string elementName, string weaponName, string speechTone, int maxHp,
                                                 int maxInner, int initiative, int agility, int moveRange,
                                                 int attackRange, int attackBonus, int defense, int damageMin,
                                                 int damageMax, string specialName, int specialRange, int specialCost,
                                                 int specialCooldown, int specialPower, int specialAttackBonus,
                                                 BattleSpecialEffect specialEffect)
    {
        return new BattleTestUnitDefinition { id = id,
                                              displayName = displayName,
                                              faction = faction,
                                              visual = LoadVisual(visualFile),
                                              startCell = startCell,
                                              sectName = sectName,
                                              age = age,
                                              mbti = mbti,
                                              elementName = elementName,
                                              weaponName = weaponName,
                                              speechTone = speechTone,
                                              maxHp = maxHp,
                                              maxInner = maxInner,
                                              initiative = initiative,
                                              agility = agility,
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
                                              specialEffect = specialEffect };
    }

    private static CharacterVisualData LoadVisual(string fileName)
    {
        string directPath = "Assets/JoseonMurimTactics/Art/Characters/" + fileName;
        CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(directPath);
        if (visual != null)
        {
            return visual;
        }

        string legacyPath = "Assets/JoseonMurimTactics/Art/Characters/VisualData/" + fileName;
        return AssetDatabase.LoadAssetAtPath<CharacterVisualData>(legacyPath);
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
