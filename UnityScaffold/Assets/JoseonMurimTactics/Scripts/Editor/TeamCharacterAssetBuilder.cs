using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace JoseonMurimTactics.Editor
{
public static class TeamCharacterAssetBuilder
{
    private const string Root = "Assets/JoseonMurimTactics";
    private const string CharacterRoot = Root + "/Art/Characters";
    private const string AnimationRoot = Root + "/Animations/Characters";
    private const string CharacterAssetFolder = Root + "/ScriptableObjects/Characters";
    private const string WeaponAssetFolder = Root + "/ScriptableObjects/Weapons";
    private const string UnitPrefabFolder = Root + "/Prefabs/Units";
    private const float PosePixelsPerUnit = 420f;
    private const float PortraitPixelsPerUnit = 420f;
    private const float IconPixelsPerUnit = 220f;
    private static readonly Vector2 BattlePosePivot = new Vector2(0.5f, 32f / 384f);

    private static readonly Spec[] Team = {
        new Spec("park_sungjun", "박성준", "백두 루멘오더", 17, "ENFJ", "빛", "검",
                 "침착하고 다정한 주인공 말투", WeaponType.Sword, CombatElementType.Light,
                 new Color(1f, 0.82f, 0.30f, 1f), new Color(0.96f, 0.98f, 1f, 1f),
                 new Vector2Int(5, 8), 36, 5, 15, 16, 4, 1, 7, 15, 6, 10, "백두광검", 1, 1, 2,
                 5, 2, BattleSpecialEffect.Mark),
        new Spec("baek_ryeon", "백련", "강원 설악창문", 17, "INFJ", "얼음/서리", "창", "차분한 존댓말",
                 WeaponType.Spear, CombatElementType.Ice, new Color(0.56f, 0.92f, 1f, 1f),
                 new Color(0.92f, 0.98f, 1f, 1f), new Vector2Int(7, 8), 30, 4, 12, 13, 4, 2, 5, 14, 5,
                 8, "설악빙창", 2, 1, 2, 4, 1, BattleSpecialEffect.Freeze),
        new Spec("do_arin", "도아린", "경상 화왕도문", 16, "ESTP", "불", "대도", "짧고 거친 직설",
                 WeaponType.Dao, CombatElementType.Fire, new Color(1f, 0.38f, 0.12f, 1f),
                 new Color(1f, 0.82f, 0.30f, 1f), new Vector2Int(9, 8), 34, 3, 14, 15, 4, 1, 7, 14, 6,
                 11, "화왕참", 1, 1, 2, 6, 2, BattleSpecialEffect.BreakGuard),
        new Spec("jin_seoyul", "진서율", "경성 천뢰봉문", 15, "ENTP", "전기", "봉",
                 "빠르고 장난기 있는 추리형 말투", WeaponType.Staff, CombatElementType.Lightning,
                 new Color(0.36f, 0.72f, 1f, 1f), new Color(1f, 0.88f, 0.24f, 1f), new Vector2Int(10, 8),
                 24, 4, 18, 19, 5, 2, 6, 12, 4, 7, "천뢰봉무", 2, 1, 2, 4, 3,
                 BattleSpecialEffect.Strike),
        new Spec("shin_seoa", "신서아", "전라도 남원 화접풍류문", 13, "ENFP", "바람/꽃", "부채",
                 "밝고 씩씩한 막내 말투", WeaponType.Fan, CombatElementType.WindFlower,
                 new Color(0.54f, 0.98f, 0.84f, 1f), new Color(1f, 0.58f, 0.76f, 1f), new Vector2Int(3, 8),
                 24, 5, 13, 14, 5, 3, 5, 13, 4, 7, "꽃바람", 3, 1, 2, 0, 0,
                 BattleSpecialEffect.Mark),
        new Spec("han_biyeon", "한비연", "황해도 구월산 흑련암문", 17, "ISTP", "어둠/독", "단검·암기",
                 "짧고 비꼬는 듯한 말투", WeaponType.Dagger, CombatElementType.DarkPoison,
                 new Color(0.58f, 0.30f, 0.92f, 1f), new Color(0.46f, 1f, 0.22f, 1f),
                 new Vector2Int(13, 8), 27, 4, 16, 17, 5, 3, 6, 13, 4, 8, "흑련독침", 3, 1, 2, 3, 2,
                 BattleSpecialEffect.Poison)
    };

    [MenuItem("Joseon Murim Tactics/Combat/Rebuild Six Character Team Assets")]
    public static void RebuildTeamCharacterAssets()
    {
        AssetDatabase.Refresh();
        EnsureFolders();

        foreach (Spec spec in Team)
        {
            EnsureCharacterFolders(spec);
            ConfigureSpriteImporters(spec);

            AnimationClip idle = SaveClip(spec, "Idle", 1.2f, true, 0.018f, 0f);
            AnimationClip selectedIdle = SaveClip(spec, "SelectedIdle", 1.0f, true, 0.032f, 0f);
            AnimationClip walk = SaveClip(spec, "Walk", 0.56f, true, 0.050f, spec.weaponType == WeaponType.Staff ? 5f : 3f);
            AnimationClip attack = SaveClip(spec, "Attack", 0.70f, false, 0.045f, -8f);
            AnimationClip skill = SaveClip(spec, "Skill", 1.10f, false, 0.075f, 6f);
            AnimationClip hit = SaveClip(spec, "Hit", 0.30f, false, 0.030f, 10f);
            AnimationClip guard = SaveClip(spec, "Guard", 0.44f, false, 0.020f, -4f);
            AnimationClip defeat = SaveClip(spec, "Defeat", 0.82f, false, -0.090f, 62f);
            AnimationClip victory = SaveClip(spec, "Victory", 0.58f, false, 0.060f, 5f);
            AnimationClip acted = SaveClip(spec, "Acted", 0.80f, true, 0.010f, 0f);

            WeaponAnimationSet weaponSet = LoadOrCreate<WeaponAnimationSet>(WeaponSetPath(spec));
            weaponSet.weaponType = spec.weaponType;
            weaponSet.elementType = spec.elementType;
            weaponSet.primaryEffectColor = spec.primary;
            weaponSet.secondaryEffectColor = spec.secondary;
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
            ApplyWeaponTiming(spec, weaponSet);
            EditorUtility.SetDirty(weaponSet);

            CharacterVisualData visualData = LoadOrCreate<CharacterVisualData>(VisualDataPath(spec));
            Sprite idleSprite = EnsureSprite(PosePath(spec, "idle"), PosePixelsPerUnit);
            CharacterOutfitData defaultOutfit = LoadOrCreate<CharacterOutfitData>(OutfitPath(spec));
            defaultOutfit.outfitId = spec.id + "_academy_default";
            defaultOutfit.displayName = spec.displayName + " 기본 전투복";
            defaultOutfit.fullBodySprite = idleSprite;
            defaultOutfit.idlePoseSprite = idleSprite;
            defaultOutfit.movePoseSprite = EnsureSprite(PosePath(spec, "move"), PosePixelsPerUnit);
            defaultOutfit.attackPoseSprite = EnsureSprite(PosePath(spec, "attack"), PosePixelsPerUnit);
            defaultOutfit.skillPoseSprite = EnsureSprite(PosePath(spec, "skill"), PosePixelsPerUnit);
            defaultOutfit.hitPoseSprite = EnsureSprite(PosePath(spec, "hit"), PosePixelsPerUnit);
            defaultOutfit.defeatedPoseSprite = EnsureSprite(PosePath(spec, "defeated"), PosePixelsPerUnit);
            defaultOutfit.actedPoseSprite = EnsureSprite(PosePath(spec, "acted"), PosePixelsPerUnit);
            defaultOutfit.idleFrames = CollectPoseFrames(spec, "idle", defaultOutfit.idlePoseSprite);
            defaultOutfit.moveFrames = CollectPoseFrames(spec, "move", defaultOutfit.movePoseSprite);
            defaultOutfit.attackFrames = CollectPoseFrames(spec, "attack", defaultOutfit.attackPoseSprite);
            defaultOutfit.skillFrames = CollectPoseFrames(spec, "skill", defaultOutfit.skillPoseSprite);
            defaultOutfit.hitFrames = CollectPoseFrames(spec, "hit", defaultOutfit.hitPoseSprite);
            defaultOutfit.bustSprite = EnsureSprite(CharacterRoot + "/" + spec.id + "/Portraits/" + spec.id + "_portrait.png", PortraitPixelsPerUnit);
            defaultOutfit.portraitSprite = defaultOutfit.bustSprite;
            defaultOutfit.faceIconSprite = EnsureSprite(CharacterRoot + "/" + spec.id + "/Portraits/" + spec.id + "_icon.png", IconPixelsPerUnit);
            defaultOutfit.useLayeredSprites = false;
            EditorUtility.SetDirty(defaultOutfit);

            visualData.visualId = spec.id;
            visualData.fullBodySprite = idleSprite;
            visualData.idlePoseSprite = idleSprite;
            visualData.movePoseSprite = defaultOutfit.movePoseSprite;
            visualData.attackPoseSprite = defaultOutfit.attackPoseSprite;
            visualData.skillPoseSprite = defaultOutfit.skillPoseSprite;
            visualData.hitPoseSprite = defaultOutfit.hitPoseSprite;
            visualData.defeatedPoseSprite = defaultOutfit.defeatedPoseSprite;
            visualData.actedPoseSprite = defaultOutfit.actedPoseSprite;
            visualData.idleFrames = defaultOutfit.idleFrames;
            visualData.moveFrames = defaultOutfit.moveFrames;
            visualData.attackFrames = defaultOutfit.attackFrames;
            visualData.skillFrames = defaultOutfit.skillFrames;
            visualData.hitFrames = defaultOutfit.hitFrames;
            visualData.bustSprite = defaultOutfit.bustSprite;
            visualData.portraitSprite = visualData.bustSprite;
            visualData.faceIconSprite = defaultOutfit.faceIconSprite;
            visualData.defaultOutfit = defaultOutfit;
            visualData.outfitOptions = new[] { defaultOutfit };
            visualData.defaultWeaponType = spec.weaponType;
            visualData.weaponAnimationSet = weaponSet;
            visualData.heightInTiles = 1.10f;
            visualData.spriteOffset = new Vector2(0f, 0.02f);
            visualData.moveSecondsPerTile = weaponSet.walkSecondsPerTile;
            visualData.moveSettleTime = weaponSet.moveSettleTime;
            visualData.moveLeanDegrees = 5f;
            visualData.attackLunge = weaponSet.attackMoveForwardDistance;
            visualData.skillPulseScale = 0.10f;
            visualData.hitRecoil = 0.11f;
            visualData.shadowWidth = 0.76f;
            visualData.shadowHeight = 0.19f;
            visualData.selectedTint = Color.Lerp(Color.white, spec.primary, 0.28f);
            visualData.hitTint = Color.Lerp(new Color(1f, 0.62f, 0.58f, 1f), spec.secondary, 0.18f);
            visualData.guardTint = Color.Lerp(Color.white, spec.primary, 0.35f);
            EditorUtility.SetDirty(visualData);

            GameObject unitPrefab = SaveUnitPrefab(spec, visualData);
            CharacterCombatVisualData combatVisual = LoadOrCreate<CharacterCombatVisualData>(CombatVisualPath(spec));
            combatVisual.characterId = spec.id;
            combatVisual.displayName = spec.displayName;
            combatVisual.unitSpritePrefab = unitPrefab;
            combatVisual.bustPortrait = visualData.bustSprite;
            combatVisual.faceIcon = visualData.faceIconSprite;
            combatVisual.defaultWeaponType = spec.weaponType;
            combatVisual.weaponAnimationSet = weaponSet;
            combatVisual.boardVisual = visualData;
            combatVisual.actedTint = visualData.actedTint;
            combatVisual.defeatedTint = visualData.defeatedTint;
            EditorUtility.SetDirty(combatVisual);
        }

        RebuildBattleTestSceneUnits();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[TeamCharacterAssetBuilder] Six character combat team assets rebuilt.");
    }

    public static BattleTestUnitDefinition[] BuildSceneUnitDefinitions()
    {
        List<BattleTestUnitDefinition> units = new List<BattleTestUnitDefinition>();
        foreach (Spec spec in Team)
        {
            units.Add(Unit(spec, Faction.Ally, spec.startCell, GetVisual(spec.id)));
        }

        units.Add(Unit("bandit_guard_1", "흑립방 칼잡이", Faction.Enemy, GetVisual("do_arin"), new Vector2Int(6, 2),
                       "흑립방", 24, "ISTJ", "화약", "도", "거칠고 위협적인 말투", 30, 3, 12, 12, 4, 1, 5, 14, 5,
                       8, "난도질", 1, 1, 2, 4, 1, BattleSpecialEffect.Strike));
        units.Add(Unit("bandit_scout_1", "흑립방 암수", Faction.Enemy, GetVisual("han_biyeon"), new Vector2Int(9, 2),
                       "흑립방", 22, "ISTP", "독", "암기", "낮게 비웃는 말투", 26, 3, 16, 16, 5, 3, 5, 13, 4,
                       7, "비침", 3, 1, 2, 3, 2, BattleSpecialEffect.Poison));
        units.Add(Unit("bandit_captain", "흑립방 두목 곽칠", Faction.Enemy, GetVisual("jin_seoyul"), new Vector2Int(12, 3),
                       "흑립방", 31, "ENTJ", "압박", "철봉", "거칠고 얕보는 말투", 38, 4, 13, 13, 4, 2, 7, 16, 6,
                       11, "흑랑표식", 4, 1, 2, 0, 0, BattleSpecialEffect.Mark));
        return units.ToArray();
    }

    private static void ApplyWeaponTiming(Spec spec, WeaponAnimationSet set)
    {
        set.walkSecondsPerTile = 0.20f + (spec.weaponType == WeaponType.Spear ? 0.03f : 0f);
        set.moveSettleTime = 0.08f;
        set.attackDuration = 0.70f;
        set.skillDuration = 1.10f;
        set.attackVfxTime = 0.28f;
        set.attackHitTime = 0.38f;
        set.skillVfxTime = 0.42f;
        set.skillHitTime = 0.56f;
        set.recoveryTime = 0.16f;
        set.attackMoveForwardDistance = spec.weaponType == WeaponType.Staff ? 0.11f : 0.14f;
        set.skillMoveForwardDistance = spec.weaponType == WeaponType.Fan ? 0.08f : 0.20f;
        set.cameraShakeStrength = spec.weaponType == WeaponType.Dao ? 0.075f : 0.055f;
        set.cameraShakeDuration = 0.10f;
    }

    private static void RebuildBattleTestSceneUnits()
    {
        if (!AssetDatabase.LoadAssetAtPath<SceneAsset>(BattleTestSceneLauncher.ScenePath))
        {
            BattleTestSceneLauncher.RebuildBattleTestScene();
        }

        Scene scene = EditorSceneManager.OpenScene(BattleTestSceneLauncher.ScenePath, OpenSceneMode.Single);
        BattleTestController controller = Object.FindObjectOfType<BattleTestController>();
        if (controller == null)
        {
            GameObject controllerObject = new GameObject("Battle Test Controller");
            controller = controllerObject.AddComponent<BattleTestController>();
        }

        controller.width = 16;
        controller.height = 12;
        controller.tileWidth = 1.16f;
        controller.tileHeight = 0.62f;
        controller.mapVariant = BattleTestMapVariant.BaekduMountainSnowfield;
        controller.useAuthoredSceneMap = true;
        controller.useTilemapBattlefield = true;
        controller.unitDefinitions = BuildSceneUnitDefinitions();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, BattleTestSceneLauncher.ScenePath);
    }

    private static void EnsureFolders()
    {
        EnsureFolder(Root + "/Animations");
        EnsureFolder(AnimationRoot);
        EnsureFolder(Root + "/ScriptableObjects");
        EnsureFolder(CharacterAssetFolder);
        EnsureFolder(WeaponAssetFolder);
        EnsureFolder(Root + "/Prefabs");
        EnsureFolder(UnitPrefabFolder);
        EnsureFolder(Root + "/Docs");
    }

    private static void EnsureCharacterFolders(Spec spec)
    {
        EnsureFolder(CharacterRoot + "/" + spec.id);
        EnsureFolder(CharacterRoot + "/" + spec.id + "/Source");
        EnsureFolder(CharacterRoot + "/" + spec.id + "/Sprites");
        EnsureFolder(CharacterRoot + "/" + spec.id + "/Portraits");
        EnsureFolder(CharacterRoot + "/" + spec.id + "/VisualData");
        EnsureFolder(CharacterRoot + "/" + spec.id + "/Outfits");
        EnsureFolder(AnimationRoot + "/" + spec.id);
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

    private static void ConfigureSpriteImporters(Spec spec)
    {
        EnsureSprite(PosePath(spec, "idle"), PosePixelsPerUnit);
        EnsureSprite(PosePath(spec, "move"), PosePixelsPerUnit);
        EnsureSprite(PosePath(spec, "attack"), PosePixelsPerUnit);
        EnsureSprite(PosePath(spec, "skill"), PosePixelsPerUnit);
        EnsureSprite(PosePath(spec, "hit"), PosePixelsPerUnit);
        EnsureSprite(PosePath(spec, "defeated"), PosePixelsPerUnit);
        EnsureSprite(PosePath(spec, "acted"), PosePixelsPerUnit);
        EnsureSprite(CharacterRoot + "/" + spec.id + "/Portraits/" + spec.id + "_portrait.png", PortraitPixelsPerUnit);
        EnsureSprite(CharacterRoot + "/" + spec.id + "/Portraits/" + spec.id + "_icon.png", IconPixelsPerUnit);
        ConfigurePoseFrameImporters(spec, "idle");
        ConfigurePoseFrameImporters(spec, "move");
        ConfigurePoseFrameImporters(spec, "attack");
        ConfigurePoseFrameImporters(spec, "skill");
        ConfigurePoseFrameImporters(spec, "hit");
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

            if (importer.spriteImportMode != SpriteImportMode.Single)
            {
                importer.spriteImportMode = SpriteImportMode.Single;
                dirty = true;
            }

            if (!Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerUnit))
            {
                importer.spritePixelsPerUnit = pixelsPerUnit;
                dirty = true;
            }

            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }

            if (!importer.alphaIsTransparency)
            {
                importer.alphaIsTransparency = true;
                dirty = true;
            }

            if (path.Contains("/Sprites/"))
            {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteAlignment != (int)SpriteAlignment.Custom)
                {
                    settings.spriteAlignment = (int)SpriteAlignment.Custom;
                    importer.SetTextureSettings(settings);
                    dirty = true;
                }

                if (importer.spritePivot != BattlePosePivot)
                {
                    importer.spritePivot = BattlePosePivot;
                    dirty = true;
                }
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
            Debug.LogWarning("[TeamCharacterAssetBuilder] Sprite could not be loaded: " + path);
        }

        return sprite;
    }

    private static AnimationClip SaveClip(Spec spec, string name, float duration, bool loop, float yAmplitude,
                                          float zRotation)
    {
        string folder = AnimationRoot + "/" + spec.id;
        EnsureFolder(folder);
        string assetName = spec.id + "_" + name.ToLowerInvariant();
        string path = folder + "/" + assetName + ".anim";
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

    private static GameObject SaveUnitPrefab(Spec spec, CharacterVisualData visualData)
    {
        GameObject root = new GameObject(spec.id + "_unit");
        CharacterVisualController controller = root.AddComponent<CharacterVisualController>();
        controller.visual = visualData;
        controller.sortingLayerName = "Characters";
        root.AddComponent<BattleTestUnitView>();
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, UnitPrefabFolder + "/" + spec.id + "_unit.prefab");
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

    private static CharacterVisualData GetVisual(string id)
    {
        return AssetDatabase.LoadAssetAtPath<CharacterVisualData>(CharacterRoot + "/" + id + "/VisualData/" + id + "_visual.asset");
    }

    private static BattleTestUnitDefinition Unit(Spec spec, Faction faction, Vector2Int startCell,
                                                 CharacterVisualData visual)
    {
        return Unit(spec.id, spec.displayName, faction, visual, startCell, spec.sectName, spec.age, spec.mbti,
                    spec.elementName, spec.weaponName, spec.speechTone, spec.maxHp, spec.maxInner, spec.initiative,
                    spec.agility, spec.moveRange, spec.attackRange, spec.attackBonus, spec.defense, spec.damageMin,
                    spec.damageMax, spec.specialName, spec.specialRange, spec.specialCost, spec.specialCooldown,
                    spec.specialPower, spec.specialAttackBonus, spec.specialEffect);
    }

    private static BattleTestUnitDefinition Unit(string id, string displayName, Faction faction, CharacterVisualData visual,
                                                 Vector2Int startCell, string sectName, int age, string mbti,
                                                 string elementName, string weaponName, string speechTone, int maxHp,
                                                 int maxInner, int initiative, int agility, int moveRange,
                                                 int attackRange, int attackBonus, int defense, int damageMin,
                                                 int damageMax, string specialName, int specialRange, int specialCost,
                                                 int specialCooldown, int specialPower, int specialAttackBonus,
                                                 BattleSpecialEffect specialEffect)
    {
        return new BattleTestUnitDefinition {
            id = id,
            displayName = displayName,
            faction = faction,
            visual = visual,
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
            specialEffect = specialEffect
        };
    }

    private static string PosePath(Spec spec, string pose)
    {
        return CharacterRoot + "/" + spec.id + "/Sprites/" + spec.id + "_" + pose + ".png";
    }

    private const int MaxPoseFrames = 8;

    private static string PoseFramePath(Spec spec, string pose, int frame)
    {
        return CharacterRoot + "/" + spec.id + "/Sprites/" + spec.id + "_" + pose + "_" + frame + ".png";
    }

    private static void ConfigurePoseFrameImporters(Spec spec, string pose)
    {
        for (int frame = 2; frame <= MaxPoseFrames; frame++)
        {
            string path = PoseFramePath(spec, pose, frame);
            if (AssetImporter.GetAtPath(path) == null)
            {
                break;
            }

            EnsureSprite(path, PosePixelsPerUnit);
        }
    }

    /// <summary>{id}_{pose}.png에 더해 {id}_{pose}_2..8.png 연속 파일을 프레임 배열로 모은다.
    /// 추가 프레임 파일이 없으면 null을 돌려 단일 포즈 경로(현행 동작)를 유지한다.</summary>
    private static Sprite[] CollectPoseFrames(Spec spec, string pose, Sprite baseSprite)
    {
        if (baseSprite == null)
        {
            return null;
        }

        List<Sprite> frames = new List<Sprite> { baseSprite };
        for (int frame = 2; frame <= MaxPoseFrames; frame++)
        {
            string path = PoseFramePath(spec, pose, frame);
            if (AssetImporter.GetAtPath(path) == null)
            {
                break;
            }

            Sprite sprite = EnsureSprite(path, PosePixelsPerUnit);
            if (sprite == null)
            {
                break;
            }

            frames.Add(sprite);
        }

        return frames.Count >= 2 ? frames.ToArray() : null;
    }

    private static string VisualDataPath(Spec spec)
    {
        return CharacterRoot + "/" + spec.id + "/VisualData/" + spec.id + "_visual.asset";
    }

    private static string OutfitPath(Spec spec)
    {
        return CharacterRoot + "/" + spec.id + "/Outfits/" + spec.id + "_academy_default.asset";
    }

    private static string CombatVisualPath(Spec spec)
    {
        return CharacterAssetFolder + "/" + spec.id + "_combat_visual.asset";
    }

    private static string WeaponSetPath(Spec spec)
    {
        return WeaponAssetFolder + "/" + spec.id + "_" + spec.weaponType.ToString().ToLowerInvariant() + "_motion_set.asset";
    }

    private sealed class Spec
    {
        public readonly string id;
        public readonly string displayName;
        public readonly string sectName;
        public readonly int age;
        public readonly string mbti;
        public readonly string elementName;
        public readonly string weaponName;
        public readonly string speechTone;
        public readonly WeaponType weaponType;
        public readonly CombatElementType elementType;
        public readonly Color primary;
        public readonly Color secondary;
        public readonly Vector2Int startCell;
        public readonly int maxHp;
        public readonly int maxInner;
        public readonly int initiative;
        public readonly int agility;
        public readonly int moveRange;
        public readonly int attackRange;
        public readonly int attackBonus;
        public readonly int defense;
        public readonly int damageMin;
        public readonly int damageMax;
        public readonly string specialName;
        public readonly int specialRange;
        public readonly int specialCost;
        public readonly int specialCooldown;
        public readonly int specialPower;
        public readonly int specialAttackBonus;
        public readonly BattleSpecialEffect specialEffect;

        public Spec(string id, string displayName, string sectName, int age, string mbti, string elementName,
                    string weaponName, string speechTone, WeaponType weaponType, CombatElementType elementType,
                    Color primary, Color secondary, Vector2Int startCell, int maxHp, int maxInner, int initiative,
                    int agility, int moveRange, int attackRange, int attackBonus, int defense, int damageMin,
                    int damageMax, string specialName, int specialRange, int specialCost, int specialCooldown,
                    int specialPower, int specialAttackBonus, BattleSpecialEffect specialEffect)
        {
            this.id = id;
            this.displayName = displayName;
            this.sectName = sectName;
            this.age = age;
            this.mbti = mbti;
            this.elementName = elementName;
            this.weaponName = weaponName;
            this.speechTone = speechTone;
            this.weaponType = weaponType;
            this.elementType = elementType;
            this.primary = primary;
            this.secondary = secondary;
            this.startCell = startCell;
            this.maxHp = maxHp;
            this.maxInner = maxInner;
            this.initiative = initiative;
            this.agility = agility;
            this.moveRange = moveRange;
            this.attackRange = attackRange;
            this.attackBonus = attackBonus;
            this.defense = defense;
            this.damageMin = damageMin;
            this.damageMax = damageMax;
            this.specialName = specialName;
            this.specialRange = specialRange;
            this.specialCost = specialCost;
            this.specialCooldown = specialCooldown;
            this.specialPower = specialPower;
            this.specialAttackBonus = specialAttackBonus;
            this.specialEffect = specialEffect;
        }
    }
}
}
