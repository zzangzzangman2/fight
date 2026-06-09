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
        controller.unitDefinitions = new[] {
            Unit("park_sungjun", "박성준", Faction.Ally, "park_sungjun_visual.asset", new Vector2Int(1, 2),
                 "백두산 백두검문", 20, "ENTP", "빛", "검", "능청스럽지만 결정적 순간에는 단호함", 36, 5, 15, 16, 4, 1,
                 7, 15, 6, 10, "백두광검", 1, 0, 1, 5, 2, BattleSpecialEffect.Mark),
            Unit("baek_ryeon", "백련", Faction.Ally, "baek_ryeon_visual.asset", new Vector2Int(1, 4), "강원 설악창문",
                 17, "INFJ", "얼음/서리", "창", "차분한 존댓말", 30, 4, 12, 13, 4, 2, 5, 14, 5, 8, "설악빙창", 2, 1, 2,
                 4, 1, BattleSpecialEffect.Freeze),
            Unit("do_arin", "도아린", Faction.Ally, "do_arin_visual.asset", new Vector2Int(2, 5), "경상 화왕도문", 16,
                 "ESTP", "불", "도", "짧고 거친 직설", 34, 3, 14, 15, 4, 1, 7, 14, 6, 11, "화왕참", 1, 1, 2, 6, 2,
                 BattleSpecialEffect.BreakGuard),
            Unit("seo_a", "서아", Faction.Ally, "SchoolCombat/school_combat_03_visual.asset", new Vector2Int(2, 1),
                 "경성 천뢰봉문", 13, "ENFP", "전기", "봉", "밝고 빠른 질문형 말투", 24, 4, 18, 19, 5, 2, 6, 12, 4, 7,
                 "천뢰봉무", 2, 1, 2, 4, 3, BattleSpecialEffect.Strike),
            Unit("mae_hwaryeong", "매화령", Faction.Ally, "strategist_visual.asset", new Vector2Int(0, 5),
                 "전라 풍매문", 18, "ENFJ", "바람/꽃", "부채", "부드럽고 사교적인 말투", 28, 5, 13, 14, 5, 3, 5, 13, 4,
                 7, "풍매선", 3, 1, 2, 0, 0, BattleSpecialEffect.Mark),
            Unit("han_biyeon", "한비연", Faction.Ally, "han_biyeon_visual.asset", new Vector2Int(0, 1), "경성 흑연문",
                 17, "ISTP", "어둠/독", "단검·암기", "짧고 비꼬는 듯한 말투", 27, 4, 16, 17, 5, 3, 6, 13, 4, 8,
                 "흑연독침", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison),
            Unit("zhongyuan_guard_1", "중원 호위검수", Faction.Enemy, "SchoolCombat/school_combat_04_visual.asset",
                 new Vector2Int(9, 1), "중원무림맹", 24, "ISTJ", "철", "검", "딱딱한 명령조", 30, 3, 12, 12, 4, 1, 5,
                 14, 5, 8, "정도검", 1, 1, 2, 4, 1, BattleSpecialEffect.Strike),
            Unit("zhongyuan_guard_2", "중원 장창수", Faction.Enemy, "SchoolCombat/school_combat_05_visual.asset",
                 new Vector2Int(9, 6), "중원무림맹", 25, "ESTJ", "철", "장창", "위압적인 말투", 32, 3, 11, 12, 4, 2, 5,
                 15, 5, 9, "쇄진창", 2, 1, 2, 4, 1, BattleSpecialEffect.BreakGuard),
            Unit("wijigang", "감찰사 위지강", Faction.Enemy, "SchoolCombat/school_combat_06_visual.asset",
                 new Vector2Int(10, 4), "중원무림맹 감찰단", 31, "ENTJ", "압박", "검", "권위적인 관화", 38, 4, 13, 13,
                 4, 1, 7, 16, 6, 11, "현판압령", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark)
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
