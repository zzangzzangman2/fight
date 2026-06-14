using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JoseonMurimTactics.Editor
{
public static class AndroidPixelBattleTestSampleBuilder
{
    private const string CharacterIconRoot =
        "Assets/JoseonMurimTactics/Art/Characters/_Imported/AndroidCharacterIcons/Sprites/Pixel/character/icon_character1";
    private const string SampleRoot =
        "Assets/JoseonMurimTactics/Art/Characters/_Imported/AndroidBattleTestSamples";
    private const string VisualRoot = SampleRoot + "/VisualData";
    private const string UnitIdPrefix = "android_pixel_test_";

    private static readonly SampleSpec[] Samples = {
        new SampleSpec(
            UnitIdPrefix + "paladin",
            "Pixel Paladin",
            "character_hknightspaladinfset01.png",
            WeaponType.Sword,
            CombatElementType.Light,
            BattleSpecialEffect.Mark,
            17,
            36,
            15,
            5,
            8,
            12
        ),
        new SampleSpec(
            UnitIdPrefix + "camelot",
            "Pixel King",
            "character_hknightskingcamelotmset01.png",
            WeaponType.Dao,
            CombatElementType.Fire,
            BattleSpecialEffect.BreakGuard,
            15,
            40,
            16,
            5,
            9,
            14
        ),
        new SampleSpec(
            UnitIdPrefix + "prince",
            "Pixel Prince",
            "character_hpapalprincemset01.png",
            WeaponType.Spear,
            CombatElementType.Ice,
            BattleSpecialEffect.Freeze,
            14,
            34,
            15,
            5,
            7,
            11
        ),
        new SampleSpec(
            UnitIdPrefix + "sophia",
            "Pixel Sophia",
            "character_hcrimsonsophiaf3set01.png",
            WeaponType.Fan,
            CombatElementType.WindFlower,
            BattleSpecialEffect.Heal,
            13,
            30,
            14,
            5,
            6,
            10
        )
    };

    [MenuItem("Joseon Murim Tactics/Combat/Install Android Pixel BattleTest Samples")]
    public static void InstallSamples()
    {
        AssetDatabase.Refresh();
        EnsureFolder(SampleRoot);
        EnsureFolder(VisualRoot);

        List<BattleTestUnitDefinition> sampleUnits = new List<BattleTestUnitDefinition>();
        for (int i = 0; i < Samples.Length; i++)
        {
            SampleSpec sample = Samples[i];
            CharacterVisualData visual = BuildVisual(sample);
            sampleUnits.Add(BuildUnit(sample, visual, i));
        }

        Scene scene = OpenBattleTestScene();
        BattleTestController controller = Object.FindAnyObjectByType<BattleTestController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("Battle Test Controller");
            controller = controllerObject.AddComponent<BattleTestController>();
        }

        List<BattleTestUnitDefinition> merged = new List<BattleTestUnitDefinition>();
        merged.AddRange(sampleUnits);

        BattleTestUnitDefinition[] existing = controller.unitDefinitions;
        if (existing == null || existing.Length == 0)
        {
            existing = TeamCharacterAssetBuilder.BuildSceneUnitDefinitions();
        }

        foreach (BattleTestUnitDefinition definition in existing)
        {
            if (definition == null || string.IsNullOrEmpty(definition.id) ||
                definition.id.StartsWith(UnitIdPrefix, System.StringComparison.Ordinal))
            {
                continue;
            }

            merged.Add(definition);
        }

        controller.width = 16;
        controller.height = 12;
        controller.tileWidth = 1.16f;
        controller.tileHeight = 0.62f;
        controller.useAuthoredSceneMap = false;
        controller.useTilemapBattlefield = true;
        controller.mapVariant = BattleTestMapVariant.BaekduSnowGate;
        controller.unitDefinitions = merged.ToArray();

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BattleTestSceneLauncher.ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[AndroidPixelBattleTestSampleBuilder] Installed " + Samples.Length +
                  " Android pixel samples into BattleTest.");
    }

    private static CharacterVisualData BuildVisual(SampleSpec sample)
    {
        string spritePath = CharacterIconRoot + "/" + sample.spriteFileName;
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (sprite == null)
        {
            throw new System.InvalidOperationException("Missing Android pixel character sprite: " + spritePath);
        }

        CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(VisualPath(sample));
        if (visual == null)
        {
            visual = ScriptableObject.CreateInstance<CharacterVisualData>();
            AssetDatabase.CreateAsset(visual, VisualPath(sample));
        }

        Sprite[] singleFrame = { sprite };
        visual.visualId = sample.id;
        visual.fullBodySprite = sprite;
        visual.bustSprite = sprite;
        visual.portraitSprite = sprite;
        visual.faceIconSprite = sprite;
        visual.animatorController = null;
        visual.idlePoseSprite = sprite;
        visual.movePoseSprite = sprite;
        visual.attackPoseSprite = sprite;
        visual.skillPoseSprite = sprite;
        visual.hitPoseSprite = sprite;
        visual.defeatedPoseSprite = sprite;
        visual.actedPoseSprite = sprite;
        visual.idleSidePoseSprite = sprite;
        visual.idleBackPoseSprite = sprite;
        visual.moveSidePoseSprite = sprite;
        visual.moveBackPoseSprite = sprite;
        visual.idleFrames = singleFrame;
        visual.moveFrames = singleFrame;
        visual.moveSideFrames = singleFrame;
        visual.moveBackFrames = singleFrame;
        visual.attackFrames = singleFrame;
        visual.skillFrames = singleFrame;
        visual.hitFrames = singleFrame;
        visual.idleFrameRate = 4f;
        visual.pixelSpriteMode = true;
        visual.livingMotion = null;
        visual.idleClip = null;
        visual.selectedIdleClip = null;
        visual.moveClip = null;
        visual.attackClip = null;
        visual.skillClip = null;
        visual.hitClip = null;
        visual.guardClip = null;
        visual.waitClip = null;
        visual.defeatClip = null;
        visual.victoryClip = null;
        visual.turnStartClip = null;
        visual.lowHpClip = null;
        visual.blinkFaceSprite = null;
        visual.happyFaceSprite = null;
        visual.angryFaceSprite = null;
        visual.painFaceSprite = null;
        visual.seriousFaceSprite = null;
        visual.enableBlink = false;
        visual.enableLayerSway = false;
        visual.enableFootDust = false;
        visual.enableSelectionPop = true;
        visual.enableImpactFreeze = true;
        visual.defaultOutfit = null;
        visual.outfitOptions = new CharacterOutfitData[0];
        visual.defaultWeaponType = sample.weaponType;
        visual.weaponAnimationSet = null;
        visual.heightInTiles = 1.12f;
        visual.spriteOffset = new Vector2(0f, 0.015f);
        visual.sortingOffset = 0;
        visual.idleAmplitude = 0.012f;
        visual.idleSpeed = 0.75f;
        visual.breathingScale = 0.004f;
        visual.shadowWidth = 0.58f;
        visual.shadowHeight = 0.15f;
        visual.normalTint = Color.white;
        visual.selectedTint = new Color(1f, 0.95f, 0.74f, 1f);
        visual.actedTint = new Color(0.72f, 0.78f, 0.86f, 1f);
        visual.hitTint = new Color(1f, 0.62f, 0.58f, 1f);
        visual.guardTint = new Color(0.72f, 0.96f, 1f, 1f);
        visual.defeatedTint = new Color(0.55f, 0.55f, 0.55f, 1f);
        visual.moveSecondsPerTile = 0.20f;
        visual.moveSettleTime = 0.08f;
        visual.moveLeanDegrees = 0f;
        visual.attackLunge = 0.06f;
        visual.skillPulseScale = 0.04f;
        visual.hitRecoil = 0.06f;

        EditorUtility.SetDirty(visual);
        return visual;
    }

    private static BattleTestUnitDefinition BuildUnit(SampleSpec sample, CharacterVisualData visual, int index)
    {
        return new BattleTestUnitDefinition {
            id = sample.id,
            displayName = sample.displayName,
            faction = Faction.Ally,
            visual = visual,
            startCell = new Vector2Int(4 + index, 0),
            sectName = "Android Pixel",
            age = 18,
            mbti = "TEST",
            elementName = sample.elementType.ToString(),
            weaponName = sample.weaponType.ToString(),
            speechTone = "Imported pixel BattleTest sample",
            maxHp = sample.maxHp,
            maxInner = 4,
            initiative = sample.initiative,
            agility = -1,
            moveRange = 4,
            attackRange = 1,
            attackBonus = sample.attackBonus,
            defense = sample.defense,
            damageMin = sample.damageMin,
            damageMax = sample.damageMax,
            specialName = "Pixel Test",
            specialRange = 2,
            specialCost = 1,
            specialCooldown = 2,
            specialPower = 4,
            specialAttackBonus = 1,
            specialEffect = sample.specialEffect
        };
    }

    private static Scene OpenBattleTestScene()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleTestSceneLauncher.ScenePath) == null)
        {
            BattleTestSceneLauncher.RebuildBattleTestScene();
        }

        return EditorSceneManager.OpenScene(BattleTestSceneLauncher.ScenePath, OpenSceneMode.Single);
    }

    private static string VisualPath(SampleSpec sample)
    {
        return VisualRoot + "/" + sample.id + "_visual.asset";
    }

    private static void EnsureFolder(string folder)
    {
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

    private sealed class SampleSpec
    {
        public readonly string id;
        public readonly string displayName;
        public readonly string spriteFileName;
        public readonly WeaponType weaponType;
        public readonly CombatElementType elementType;
        public readonly BattleSpecialEffect specialEffect;
        public readonly int initiative;
        public readonly int maxHp;
        public readonly int defense;
        public readonly int attackBonus;
        public readonly int damageMin;
        public readonly int damageMax;

        public SampleSpec(string id, string displayName, string spriteFileName, WeaponType weaponType,
                          CombatElementType elementType, BattleSpecialEffect specialEffect, int initiative,
                          int maxHp, int defense, int attackBonus, int damageMin, int damageMax)
        {
            this.id = id;
            this.displayName = displayName;
            this.spriteFileName = spriteFileName;
            this.weaponType = weaponType;
            this.elementType = elementType;
            this.specialEffect = specialEffect;
            this.initiative = initiative;
            this.maxHp = maxHp;
            this.defense = defense;
            this.attackBonus = attackBonus;
            this.damageMin = damageMin;
            this.damageMax = damageMax;
        }
    }
}
}
