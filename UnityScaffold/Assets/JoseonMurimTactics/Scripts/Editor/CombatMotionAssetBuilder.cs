using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JoseonMurimTactics.Editor
{
public static class CombatMotionAssetBuilder
{
    private const string Root = "Assets/JoseonMurimTactics";
    private const string AnimationFolder = Root + "/Animations/Characters/TestSwordsman";
    private const string CharacterAssetFolder = Root + "/ScriptableObjects/Characters";
    private const string WeaponAssetFolder = Root + "/ScriptableObjects/Weapons";
    private const string UnitPrefabFolder = Root + "/Prefabs/Units";
    private const string VfxPrefabFolder = Root + "/Prefabs/VFX";
    private const string VisualDataPath = Root + "/Art/Characters/TestSwordsman/VisualData/test_swordsman_visual.asset";
    private const string CombatVisualDataPath = CharacterAssetFolder + "/test_swordsman_combat_visual.asset";
    private const string WeaponSetPath = WeaponAssetFolder + "/sword_motion_set.asset";
    private const string UnitPrefabPath = UnitPrefabFolder + "/test_swordsman_unit.prefab";

    [MenuItem("Joseon Murim Tactics/Combat/Rebuild Test Swordsman Motion Assets")]
    public static void RebuildTestSwordsmanAssets()
    {
        EnsureFolders();
        ConfigureSpriteImporters();

        AnimationClip idle = SaveClip("Idle", 1.2f, true, 0.020f, 0f);
        AnimationClip selectedIdle = SaveClip("SelectedIdle", 1.0f, true, 0.035f, 0f);
        AnimationClip walk = SaveClip("Walk", 0.6f, true, 0.055f, 4f);
        AnimationClip attack = SaveClip("Attack_Sword_01", 0.72f, false, 0.050f, -8f);
        AnimationClip skill = SaveClip("Skill_Sword_01", 1.12f, false, 0.080f, 5f);
        AnimationClip hit = SaveClip("Hit", 0.30f, false, 0.030f, 10f);
        AnimationClip guard = SaveClip("Guard", 0.45f, false, 0.020f, -4f);
        AnimationClip defeat = SaveClip("Defeat", 0.80f, false, -0.090f, 70f);
        AnimationClip victory = SaveClip("Victory", 0.55f, false, 0.070f, 5f);
        AnimationClip acted = SaveClip("Acted", 0.8f, true, 0.010f, 0f);

        GameObject slashVfx = SaveVfxPrefab("test_swordsman_slash_vfx", Root + "/Art/Effects/TestSwordsman/test_swordsman_slash_arc.png", new Color(0.55f, 0.98f, 1f, 0.90f), new Vector3(0.72f, 0.52f, 1f));
        GameObject skillVfx = SaveVfxPrefab("test_swordsman_skill_vfx", Root + "/Art/Effects/TestSwordsman/test_swordsman_skill_burst.png", new Color(0.42f, 0.94f, 1f, 0.70f), Vector3.one);
        GameObject guardVfx = SaveVfxPrefab("test_swordsman_guard_vfx", Root + "/Art/Effects/TestSwordsman/test_swordsman_guard_ring.png", new Color(0.52f, 0.94f, 1f, 0.64f), Vector3.one * 0.72f);

        WeaponAnimationSet weaponSet = LoadOrCreate<WeaponAnimationSet>(WeaponSetPath);
        weaponSet.weaponType = WeaponType.Sword;
        weaponSet.idleClip = idle;
        weaponSet.selectedIdleClip = selectedIdle;
        weaponSet.walkClip = walk;
        weaponSet.attackClip = attack;
        weaponSet.skillClip = skill;
        weaponSet.guardClip = guard;
        weaponSet.hitClip = hit;
        weaponSet.defeatClip = defeat;
        weaponSet.victoryClip = victory;
        weaponSet.actedClip = acted;
        weaponSet.walkSecondsPerTile = 0.24f;
        weaponSet.moveSettleTime = 0.10f;
        weaponSet.attackDuration = 0.72f;
        weaponSet.skillDuration = 1.12f;
        weaponSet.attackVfxTime = 0.30f;
        weaponSet.attackHitTime = 0.40f;
        weaponSet.skillVfxTime = 0.44f;
        weaponSet.skillHitTime = 0.58f;
        weaponSet.recoveryTime = 0.18f;
        weaponSet.attackMoveForwardDistance = 0.13f;
        weaponSet.skillMoveForwardDistance = 0.20f;
        weaponSet.cameraShakeStrength = 0.055f;
        weaponSet.cameraShakeDuration = 0.11f;
        weaponSet.attackVfxPrefab = slashVfx;
        weaponSet.skillVfxPrefab = skillVfx;
        weaponSet.guardVfxPrefab = guardVfx;
        EditorUtility.SetDirty(weaponSet);

        CharacterVisualData visualData = LoadOrCreate<CharacterVisualData>(VisualDataPath);
        visualData.visualId = "test_swordsman";
        visualData.fullBodySprite = EnsureSprite(Root + "/Art/Characters/TestSwordsman/Sprites/test_swordsman_fullbody.png", 420f);
        visualData.bustSprite = EnsureSprite(Root + "/Art/Characters/TestSwordsman/Portraits/test_swordsman_bust.png", 420f);
        visualData.portraitSprite = visualData.bustSprite;
        visualData.faceIconSprite = EnsureSprite(Root + "/Art/Characters/TestSwordsman/Portraits/test_swordsman_icon.png", 220f);
        visualData.defaultWeaponType = WeaponType.Sword;
        visualData.weaponAnimationSet = weaponSet;
        visualData.heightInTiles = 1.18f;
        visualData.spriteOffset = new Vector2(0f, 0.12f);
        visualData.moveSecondsPerTile = weaponSet.walkSecondsPerTile;
        visualData.moveSettleTime = weaponSet.moveSettleTime;
        visualData.attackLunge = weaponSet.attackMoveForwardDistance;
        visualData.skillPulseScale = 0.09f;
        visualData.hitRecoil = 0.10f;
        EditorUtility.SetDirty(visualData);

        GameObject unitPrefab = SaveUnitPrefab(visualData);
        CharacterCombatVisualData combatVisual = LoadOrCreate<CharacterCombatVisualData>(CombatVisualDataPath);
        combatVisual.characterId = "test_swordsman";
        combatVisual.displayName = "Test Swordsman";
        combatVisual.unitSpritePrefab = unitPrefab;
        combatVisual.bustPortrait = visualData.bustSprite;
        combatVisual.faceIcon = visualData.faceIconSprite;
        combatVisual.defaultWeaponType = WeaponType.Sword;
        combatVisual.weaponAnimationSet = weaponSet;
        combatVisual.boardVisual = visualData;
        combatVisual.actedTint = visualData.actedTint;
        combatVisual.defeatedTint = visualData.defeatedTint;
        EditorUtility.SetDirty(combatVisual);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[CombatMotionAssetBuilder] Test swordsman motion assets rebuilt.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder(Root, "Animations");
        EnsureFolder(Root + "/Animations", "Characters");
        EnsureFolder(Root + "/Animations/Characters", "TestSwordsman");
        EnsureFolder(Root, "ScriptableObjects");
        EnsureFolder(Root + "/ScriptableObjects", "Characters");
        EnsureFolder(Root + "/ScriptableObjects", "Weapons");
        EnsureFolder(Root, "Prefabs");
        EnsureFolder(Root + "/Prefabs", "Units");
        EnsureFolder(Root + "/Prefabs", "VFX");
        EnsureFolder(Root, "Docs");
    }

    private static void EnsureFolder(string parent, string child)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + child))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }

    private static void ConfigureSpriteImporters()
    {
        EnsureSprite(Root + "/Art/Characters/TestSwordsman/Sprites/test_swordsman_fullbody.png", 420f);
        EnsureSprite(Root + "/Art/Characters/TestSwordsman/Portraits/test_swordsman_bust.png", 420f);
        EnsureSprite(Root + "/Art/Characters/TestSwordsman/Portraits/test_swordsman_icon.png", 220f);
        EnsureSprite(Root + "/Art/Effects/TestSwordsman/test_swordsman_slash_arc.png", 120f);
        EnsureSprite(Root + "/Art/Effects/TestSwordsman/test_swordsman_skill_burst.png", 96f);
        EnsureSprite(Root + "/Art/Effects/TestSwordsman/test_swordsman_guard_ring.png", 96f);
    }

    private static Sprite EnsureSprite(string path, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            bool dirty = false;
            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                dirty = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                dirty = true;
            }

            if (dirty)
            {
                importer.SaveAndReimport();
            }
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
        }

        if (sprite == null)
        {
            Debug.LogWarning("[CombatMotionAssetBuilder] Sprite could not be loaded: " + path);
        }

        return sprite;
    }

    private static AnimationClip SaveClip(string name, float duration, bool loop, float yAmplitude, float zRotation)
    {
        string assetName = "test_swordsman_" + name.ToLowerInvariant();
        string path = AnimationFolder + "/" + assetName + ".anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }

        clip.name = assetName;
        clip.frameRate = 12f;
        clip.wrapMode = loop ? WrapMode.Loop : WrapMode.Once;
        clip.ClearCurves();
        AnimationCurve yCurve = AnimationCurve.EaseInOut(0f, 0f, duration * 0.5f, yAmplitude);
        yCurve.AddKey(duration, 0f);
        AnimationCurve rotCurve = AnimationCurve.EaseInOut(0f, 0f, duration, zRotation);
        clip.SetCurve("FullBody", typeof(Transform), "localPosition.y", yCurve);
        clip.SetCurve("FullBody", typeof(Transform), "localEulerAngles.z", rotCurve);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    private static GameObject SaveVfxPrefab(string prefabName, string spritePath, Color color, Vector3 scale)
    {
        GameObject root = new GameObject(prefabName);
        SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = EnsureSprite(spritePath, 96f);
        renderer.color = color;
        root.transform.localScale = scale;
        string path = VfxPrefabFolder + "/" + prefabName + ".prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject SaveUnitPrefab(CharacterVisualData visualData)
    {
        GameObject root = new GameObject("test_swordsman_unit");
        CharacterVisualController controller = root.AddComponent<CharacterVisualController>();
        controller.visual = visualData;
        controller.sortingLayerName = "Characters";
        root.AddComponent<BattleTestUnitView>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, UnitPrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static T LoadOrCreate<T>(string path) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            return asset;
        }

        asset = ScriptableObject.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }
}
}
