#if UNITY_EDITOR
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace JoseonMurimTactics.Editor
{
public static class VisualUpgradeV1Validator
{
    private const string Root = "Assets/JoseonMurimTactics";
    private const string ArtRoot = Root + "/Art/VisualUpgradeV1";
    private const string VisualAssetRoot = Root + "/ScriptableObjects/Visuals";
    private const string BattleTestScenePath = Root + "/Scenes/BattleTest.unity";
    private static readonly string[] RequiredImageAssets =
    {
        "Concepts/baekdusan_gate_battle_concept_16x9.png",
        "Concepts/baek_ryeon_bandit_rescue_concept_16x9.png",
        "Tiles/snow_ground_01.png",
        "Tiles/snow_ground_02.png",
        "Tiles/snow_ground_03.png",
        "Tiles/packed_snow_road_01.png",
        "Tiles/stone_stair_snow_01.png",
        "Tiles/frozen_stream_01.png",
        "Tiles/ice_crack_01.png",
        "Tiles/cliff_top_snow_01.png",
        "Tiles/cliff_side_ice_01.png",
        "Tiles/shrine_floor_ruined_01.png",
        "Tiles/burned_ground_01.png",
        "Tiles/smoke_ground_01.png",
        "Props/broken_sect_gate.png",
        "Props/broken_signboard_haedongmun.png",
        "Props/torn_banner_blue.png",
        "Props/torn_banner_red_enemy.png",
        "Props/torch_stand_lit.png",
        "Props/stone_lantern_snow.png",
        "Props/frozen_pine_small.png",
        "Props/frozen_pine_large.png",
        "Props/snow_rock_cover_01.png",
        "Props/snow_rock_cover_02.png",
        "Props/wooden_palisade_broken.png",
        "Props/shrine_bell_snow.png",
        "Props/incense_burner_frozen.png",
        "Props/bamboo_trap_snow.png",
        "Props/bandit_supply_cart.png",
        "Props/ice_bridge_rope.png",
        "VFX/vfx_sword_slash_silver_4f.png",
        "VFX/vfx_frost_spear_4f.png",
        "VFX/vfx_hit_spark_red_4f.png",
        "VFX/vfx_heal_inner_light_4f.png",
        "VFX/vfx_snow_step_puff_4f.png",
        "VFX/vfx_counter_flash_4f.png",
        "VFX/vfx_danger_aura_loop_4f.png",
        "VFX/vfx_phase_snow_swirl_4f.png",
        "UI/panel_ink_dark_9slice.png",
        "UI/panel_snow_light_9slice.png",
        "UI/command_button_normal.png",
        "UI/command_button_hover.png",
        "UI/forecast_panel_frame.png",
        "UI/phase_banner_player.png",
        "UI/phase_banner_enemy.png",
        "UI/hp_bar_frame.png",
        "UI/inner_bar_frame.png",
        "UI/morale_icon.png",
        "UI/break_icon.png",
        "UI/counter_icon.png",
        "Characters/park_sungjun_fullbody_visual_v1.png",
        "Characters/park_sungjun_fullbody_visual_v2.png",
        "Characters/yoon_seohwa_fullbody_visual_v1.png",
        "Characters/baek_ryeon_fullbody_visual_v1.png",
        "Characters/han_biyeon_fullbody_visual_v1.png",
        "Characters/do_arin_fullbody_visual_v1.png",
        "Characters/enemy_inspector_swordsman_visual_v1.png",
        "Characters/enemy_bandit_raider_visual_v1.png",
        "Characters/enemy_bandit_archer_visual_v1.png",
        "Portraits/park_sungjun_portrait_v1.png",
        "Portraits/yoon_seohwa_portrait_v1.png",
        "Portraits/baek_ryeon_portrait_v1.png",
        "Portraits/han_biyeon_portrait_v1.png",
        "Portraits/do_arin_portrait_v1.png"
    };

    [MenuItem("Joseon Murim Tactics/Visual Upgrade V1/Validate Visual Setup")]
    public static void ValidateVisualSetup()
    {
        List<string> pass = new List<string>();
        List<string> fail = new List<string>();

        RequireFolder(ArtRoot, pass, fail);
        foreach (string folder in new[] { "Concepts", "Tiles", "Props", "VFX", "UI", "Characters", "Portraits", "Materials" })
        {
            RequireFolder(ArtRoot + "/" + folder, pass, fail);
        }

        RequireAsset<BattleVisualProfile>(VisualAssetRoot + "/baekdusan_gate_battle_visual_profile.asset", pass, fail);
        RequireAsset<BattleVfxLibrary>(VisualAssetRoot + "/battle_vfx_library_v1.asset", pass, fail);
        RequireAsset<BattleUiSkinData>(VisualAssetRoot + "/battle_ui_skin_v1.asset", pass, fail);

        RequireTextAsset(Root + "/Docs/visual_quality_v1_0_art_bible.md", pass, fail);
        RequireTextAsset(Root + "/Docs/visual_quality_v1_0_implementation_notes.md", pass, fail);
        RequireTextAsset(Root + "/Docs/VisualUpgradeV1/imagegen_prompts.md", pass, fail);
        RequireTextAsset(Root + "/Docs/chapter1_baek_ryeon_rescue_visual_notes.md", pass, fail);
        foreach (string relativePath in RequiredImageAssets)
        {
            RequireSprite(ArtRoot + "/" + relativePath, pass, fail);
        }

        CheckVisualProfile(pass, fail);
        CheckVfxLibrary(pass, fail);
        CheckUiSkin(pass, fail);

        EditorSceneManager.OpenScene(BattleTestScenePath, OpenSceneMode.Single);
        BattleTestController controller = Object.FindAnyObjectByType<BattleTestController>();
        if (controller == null)
        {
            fail.Add("BattleTestController not found in the open scene.");
        }
        else
        {
            pass.Add("BattleTestController found in the open scene.");
            CheckComponent<BattleCameraFx>(controller.gameObject, pass, fail);
            CheckComponent<DamagePopupPresenter>(controller.gameObject, pass, fail);
            CheckComponent<BattleImpactPresenter>(controller.gameObject, pass, fail);
        }

        int generatedSpriteCount = AssetDatabase.FindAssets("t:Sprite", new[] { ArtRoot }).Length;
        if (generatedSpriteCount > 0)
        {
            pass.Add($"Generated VisualUpgradeV1 sprites found: {generatedSpriteCount}.");
        }
        else
        {
            pass.Add("Generated VisualUpgradeV1 sprites not required yet; prompt document is present.");
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("[VisualUpgradeV1] Validation report");
        foreach (string item in pass)
        {
            builder.AppendLine("PASS: " + item);
        }

        foreach (string item in fail)
        {
            builder.AppendLine("FAIL: " + item);
        }

        if (fail.Count > 0)
        {
            Debug.LogError(builder.ToString());
        }
        else
        {
            Debug.Log(builder.ToString());
        }
    }

    private static void RequireFolder(string path, List<string> pass, List<string> fail)
    {
        if (AssetDatabase.IsValidFolder(path))
        {
            pass.Add("Folder exists: " + path);
        }
        else
        {
            fail.Add("Missing folder: " + path);
        }
    }

    private static void RequireAsset<T>(string path, List<string> pass, List<string> fail) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null)
        {
            pass.Add("Asset exists: " + path);
        }
        else
        {
            fail.Add("Missing asset: " + path);
        }
    }

    private static void RequireTextAsset(string path, List<string> pass, List<string> fail)
    {
        TextAsset asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        if (asset != null)
        {
            pass.Add("Doc exists: " + path);
        }
        else
        {
            fail.Add("Missing doc: " + path);
        }
    }

    private static void RequireSprite(string path, List<string> pass, List<string> fail)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite != null)
        {
            pass.Add("Sprite exists: " + path);
        }
        else
        {
            fail.Add("Missing sprite: " + path);
        }
    }

    private static void CheckVisualProfile(List<string> pass, List<string> fail)
    {
        BattleVisualProfile profile = AssetDatabase.LoadAssetAtPath<BattleVisualProfile>(
            VisualAssetRoot + "/baekdusan_gate_battle_visual_profile.asset");
        if (profile == null)
        {
            fail.Add("Visual profile could not be loaded for link validation.");
            return;
        }

        CheckArray(profile.groundTiles, 5, "profile ground/road tiles", pass, fail);
        CheckArray(profile.waterTiles, 2, "profile water tiles", pass, fail);
        CheckArray(profile.cliffTiles, 2, "profile cliff tiles", pass, fail);
        CheckArray(profile.decorTiles, 2, "profile decor tiles", pass, fail);
        CheckArray(profile.propSprites, 16, "profile prop sprites", pass, fail);
    }

    private static void CheckVfxLibrary(List<string> pass, List<string> fail)
    {
        BattleVfxLibrary library = AssetDatabase.LoadAssetAtPath<BattleVfxLibrary>(
            VisualAssetRoot + "/battle_vfx_library_v1.asset");
        if (library == null)
        {
            fail.Add("VFX library could not be loaded for link validation.");
            return;
        }

        CheckObject(library.swordSlash, "VFX clip swordSlash", pass, fail);
        CheckObject(library.frostSpear, "VFX clip frostSpear", pass, fail);
        CheckObject(library.hitSpark, "VFX clip hitSpark", pass, fail);
        CheckObject(library.healPulse, "VFX clip healPulse", pass, fail);
        CheckObject(library.snowStep, "VFX clip snowStep", pass, fail);
        CheckObject(library.counterFlash, "VFX clip counterFlash", pass, fail);
        CheckObject(library.dangerAura, "VFX clip dangerAura", pass, fail);
        CheckObject(library.phaseSnowSwirl, "VFX clip phaseSnowSwirl", pass, fail);
    }

    private static void CheckUiSkin(List<string> pass, List<string> fail)
    {
        BattleUiSkinData skin = AssetDatabase.LoadAssetAtPath<BattleUiSkinData>(
            VisualAssetRoot + "/battle_ui_skin_v1.asset");
        if (skin == null)
        {
            fail.Add("UI skin could not be loaded for link validation.");
            return;
        }

        CheckObject(skin.panelDark, "UI skin panelDark", pass, fail);
        CheckObject(skin.panelLight, "UI skin panelLight", pass, fail);
        CheckObject(skin.commandButtonNormal, "UI skin commandButtonNormal", pass, fail);
        CheckObject(skin.commandButtonHover, "UI skin commandButtonHover", pass, fail);
        CheckObject(skin.forecastPanelFrame, "UI skin forecastPanelFrame", pass, fail);
        CheckObject(skin.phaseBannerPlayer, "UI skin phaseBannerPlayer", pass, fail);
        CheckObject(skin.phaseBannerEnemy, "UI skin phaseBannerEnemy", pass, fail);
        CheckObject(skin.hpBarFrame, "UI skin hpBarFrame", pass, fail);
        CheckObject(skin.innerBarFrame, "UI skin innerBarFrame", pass, fail);
        CheckObject(skin.moraleIcon, "UI skin moraleIcon", pass, fail);
        CheckObject(skin.breakIcon, "UI skin breakIcon", pass, fail);
        CheckObject(skin.counterIcon, "UI skin counterIcon", pass, fail);
    }

    private static void CheckArray(Object[] values, int minimum, string label, List<string> pass, List<string> fail)
    {
        int count = 0;
        if (values != null)
        {
            for (int i = 0; i < values.Length; i++)
            {
                if (values[i] != null)
                {
                    count++;
                }
            }
        }

        if (count >= minimum)
        {
            pass.Add($"{label}: {count} linked.");
        }
        else
        {
            fail.Add($"{label}: expected at least {minimum}, found {count}.");
        }
    }

    private static void CheckObject(Object value, string label, List<string> pass, List<string> fail)
    {
        if (value != null)
        {
            pass.Add(label + " linked.");
        }
        else
        {
            fail.Add(label + " is missing.");
        }
    }

    private static void CheckComponent<T>(GameObject target, List<string> pass, List<string> fail) where T : Component
    {
        if (target.GetComponent<T>() != null)
        {
            pass.Add("Scene hook exists: " + typeof(T).Name);
        }
        else
        {
            fail.Add("Missing scene hook: " + typeof(T).Name);
        }
    }
}
}
#endif
