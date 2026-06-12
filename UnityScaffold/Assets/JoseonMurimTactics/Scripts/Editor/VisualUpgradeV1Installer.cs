#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class VisualUpgradeV1Installer
{
    private const string Root = "Assets/JoseonMurimTactics";
    private const string ArtRoot = Root + "/Art/VisualUpgradeV1";
    private const string VisualAssetRoot = Root + "/ScriptableObjects/Visuals";
    private const string BattleTestScenePath = Root + "/Scenes/BattleTest.unity";
    private const string BattleVisualProfilePath = VisualAssetRoot + "/baekdusan_gate_battle_visual_profile.asset";
    private const string BattleVfxLibraryPath = VisualAssetRoot + "/battle_vfx_library_v1.asset";
    private const string BattleUiSkinPath = VisualAssetRoot + "/battle_ui_skin_v1.asset";
    private const string VfxClipRoot = VisualAssetRoot + "/VfxClips";

    [MenuItem("Joseon Murim Tactics/Visual Upgrade V1/Import Generated Sprites")]
    public static void ImportGeneratedSprites()
    {
        EnsureFolders();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtRoot });
        int changed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (!path.EndsWith(".png"))
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                continue;
            }

            bool before = importer.textureType == TextureImporterType.Sprite;
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.spritePixelsPerUnit = PixelsPerUnitForPath(path);
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
            changed += before ? 0 : 1;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[VisualUpgradeV1] Imported generated sprites. Updated new sprite importers: {changed}");
    }

    [MenuItem("Joseon Murim Tactics/Visual Upgrade V1/Apply To Current BattleTest")]
    public static void ApplyToCurrentBattleTest()
    {
        EnsureFolders();
        CreateBaselineAssets();
        EditorSceneManager.OpenScene(BattleTestScenePath, OpenSceneMode.Single);
        BattleTestController controller = Object.FindAnyObjectByType<BattleTestController>();
        if (controller != null)
        {
            EnsureComponent<BattleCameraFx>(controller.gameObject);
            EnsureComponent<DamagePopupPresenter>(controller.gameObject);
            EnsureComponent<BattleImpactPresenter>(controller.gameObject);
            EditorUtility.SetDirty(controller.gameObject);
            EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
            EditorSceneManager.SaveScene(controller.gameObject.scene);
        }

        AssetDatabase.SaveAssets();
        Debug.Log("[VisualUpgradeV1] Applied baseline visual assets and presentation hooks to current BattleTest.");
    }

    public static void EnsureFolders()
    {
        EnsureFolder("Assets", "JoseonMurimTactics");
        EnsureFolder(Root, "Art");
        EnsureFolder(Root + "/Art", "VisualUpgradeV1");
        foreach (string child in new[] { "Concepts", "Tiles", "Props", "VFX", "UI", "Characters", "Portraits", "Materials" })
        {
            EnsureFolder(ArtRoot, child);
        }

        EnsureFolder(Root, "ScriptableObjects");
        EnsureFolder(Root + "/ScriptableObjects", "Visuals");
        EnsureFolder(VisualAssetRoot, "VfxClips");
        EnsureFolder(Root, "Docs");
        EnsureFolder(Root + "/Docs", "VisualUpgradeV1");
    }

    public static void CreateBaselineAssets()
    {
        EnsureFolders();
        BattleVisualProfile profile = LoadOrCreate<BattleVisualProfile>(BattleVisualProfilePath);
        profile.id = "baekdusan_gate_v1";
        profile.globalTint = new Color(0.78f, 0.82f, 0.90f, 1f);
        profile.groundTiles = LoadSprites("Tiles", "snow_ground", "packed_snow", "stone_stair", "shrine_floor");
        profile.roadTiles = LoadSprites("Tiles", "packed_snow", "stone_stair");
        profile.waterTiles = LoadSprites("Tiles", "frozen_stream", "ice_crack");
        profile.cliffTiles = LoadSprites("Tiles", "cliff_top", "cliff_side");
        profile.decorTiles = LoadSprites("Tiles", "burned_ground", "smoke_ground");
        profile.propSprites = LoadSprites("Props");
        EditorUtility.SetDirty(profile);

        BattleVfxLibrary vfx = LoadOrCreate<BattleVfxLibrary>(BattleVfxLibraryPath);
        vfx.swordSlash = LoadOrCreateVfxClip("vfx_sword_slash_silver_4f");
        vfx.frostSpear = LoadOrCreateVfxClip("vfx_frost_spear_4f");
        vfx.hitSpark = LoadOrCreateVfxClip("vfx_hit_spark_red_4f");
        vfx.healPulse = LoadOrCreateVfxClip("vfx_heal_inner_light_4f");
        vfx.snowStep = LoadOrCreateVfxClip("vfx_snow_step_puff_4f");
        vfx.counterFlash = LoadOrCreateVfxClip("vfx_counter_flash_4f");
        vfx.dangerAura = LoadOrCreateVfxClip("vfx_danger_aura_loop_4f");
        vfx.phaseSnowSwirl = LoadOrCreateVfxClip("vfx_phase_snow_swirl_4f");
        EditorUtility.SetDirty(vfx);

        BattleUiSkinData skin = LoadOrCreate<BattleUiSkinData>(BattleUiSkinPath);
        skin.panelDark = LoadSprite("UI/panel_ink_dark_9slice");
        skin.panelLight = LoadSprite("UI/panel_snow_light_9slice");
        skin.commandButtonNormal = LoadSprite("UI/command_button_normal");
        skin.commandButtonHover = LoadSprite("UI/command_button_hover");
        skin.forecastPanelFrame = LoadSprite("UI/forecast_panel_frame");
        skin.phaseBannerPlayer = LoadSprite("UI/phase_banner_player");
        skin.phaseBannerEnemy = LoadSprite("UI/phase_banner_enemy");
        skin.hpBarFrame = LoadSprite("UI/hp_bar_frame");
        skin.innerBarFrame = LoadSprite("UI/inner_bar_frame");
        skin.moraleIcon = LoadSprite("UI/morale_icon");
        skin.breakIcon = LoadSprite("UI/break_icon");
        skin.counterIcon = LoadSprite("UI/counter_icon");
        skin.allyAccent = new Color(0.35f, 0.72f, 1f, 1f);
        skin.enemyAccent = new Color(0.90f, 0.28f, 0.22f, 1f);
        skin.dangerAccent = new Color(0.95f, 0.32f, 0.16f, 1f);
        EditorUtility.SetDirty(skin);
        AssetDatabase.SaveAssets();
    }

    private static float PixelsPerUnitForPath(string path)
    {
        if (path.Contains("/Tiles/"))
        {
            return 64f;
        }

        if (path.Contains("/UI/"))
        {
            return 100f;
        }

        if (path.Contains("/Characters/") || path.Contains("/Portraits/"))
        {
            return 420f;
        }

        return 128f;
    }

    private static Sprite LoadSprite(string relativeWithoutExtension)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{relativeWithoutExtension}.png");
    }

    private static Sprite[] LoadSprites(string folder, params string[] prefixes)
    {
        string folderPath = ArtRoot + "/" + folder;
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            return new Sprite[0];
        }

        List<Sprite> sprites = new List<Sprite>();
        string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string file = Path.GetFileNameWithoutExtension(path);
            if (prefixes != null && prefixes.Length > 0)
            {
                bool matched = false;
                foreach (string prefix in prefixes)
                {
                    if (file.StartsWith(prefix))
                    {
                        matched = true;
                        break;
                    }
                }

                if (!matched)
                {
                    continue;
                }
            }

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                sprites.Add(sprite);
            }
        }

        return sprites.ToArray();
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

    private static AnimationClip LoadOrCreateVfxClip(string effectId)
    {
        string path = $"{VfxClipRoot}/{effectId}.anim";
        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { frameRate = 12f };
            AssetDatabase.CreateAsset(clip, path);
        }

        ObjectReferenceKeyframe[] frames = LoadVfxFrames(effectId);
        if (frames.Length > 0)
        {
            EditorCurveBinding binding = new EditorCurveBinding
            {
                type = typeof(SpriteRenderer),
                path = string.Empty,
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
            EditorUtility.SetDirty(clip);
        }

        return clip;
    }

    private static ObjectReferenceKeyframe[] LoadVfxFrames(string effectId)
    {
        List<ObjectReferenceKeyframe> frames = new List<ObjectReferenceKeyframe>();
        for (int i = 0; i < 4; i++)
        {
            Sprite sprite = LoadSprite($"VFX/Frames/{effectId}_f{i}");
            if (sprite == null)
            {
                continue;
            }

            frames.Add(new ObjectReferenceKeyframe
            {
                time = i / 12f,
                value = sprite
            });
        }

        return frames.ToArray();
    }

    private static T EnsureComponent<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : target.AddComponent<T>();
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder(parent, child);
        }
    }
}
}
#endif
