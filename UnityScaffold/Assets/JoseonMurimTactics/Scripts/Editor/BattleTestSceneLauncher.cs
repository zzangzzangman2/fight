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
        CreateLighting();
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

    private static void CreateLighting()
    {
        GameObject lightObject = new GameObject("Battle Character Key Light");
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Directional;
        light.color = new Color(1f, 0.96f, 0.88f, 1f);
        light.intensity = 1.15f;
        lightObject.transform.rotation = Quaternion.Euler(42f, -32f, 0f);
    }

    private static void CreateController()
    {
        GameObject controllerObject = new GameObject("Battle Test Controller");
        BattleTestController controller = controllerObject.AddComponent<BattleTestController>();
        controller.width = 16;
        controller.height = 12;
        controller.tileWidth = 1.16f;
        controller.tileHeight = 0.62f;
        controller.unitDefinitions = TeamCharacterAssetBuilder.BuildSceneUnitDefinitions();
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
