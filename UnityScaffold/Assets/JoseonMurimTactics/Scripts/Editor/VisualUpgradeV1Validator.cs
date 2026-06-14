#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;

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
        CheckSeorakRecruitmentFlow(pass, fail);

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
            CheckObject(controller.battleVisualProfile, "BattleTest visual profile reference", pass, fail);
            CheckObject(controller.battleVfxLibrary, "BattleTest VFX library reference", pass, fail);
            CheckObject(controller.battleUiSkin, "BattleTest UI skin reference", pass, fail);
            CheckPaintedMapBackdropBinding(controller, pass, fail);
            CheckBattleAdjacencyMath(controller, pass, fail);
            CheckBattleTileClickTargetResolution(controller, pass, fail);
            CheckBattleHudForecastPolicy(pass, fail);
            CheckDeploymentDragAndFacingPolicy(pass, fail);
            CheckDirectionalWalkAnimationPolicy(pass, fail);
            CheckBattleTerrainPlacementPolicy(controller, pass, fail);
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

    private static void RequireTexture(string path, List<string> pass, List<string> fail)
    {
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture != null)
        {
            pass.Add("Texture exists: " + path);
        }
        else
        {
            fail.Add("Missing texture: " + path);
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

    private static void CheckSeorakRecruitmentFlow(List<string> pass, List<string> fail)
    {
        MissionInfo mission = FindMission("MISSION_CH01_SEORAK_REQUEST");
        CheckCondition(mission != null, "Seorak rescue mission exists in MissionCatalog.", "Missing Seorak rescue mission.", pass, fail);
        if (mission != null)
        {
            CheckCondition(mission.battleId == HubController.SeorakPassRescueBattleId,
                           "Seorak rescue mission points to the Seorak battle id.",
                           "Seorak rescue mission battle id is not " + HubController.SeorakPassRescueBattleId + ".",
                           pass, fail);
            CheckCondition(mission.requiredFlag == StoryFlags.FirstBattleWon,
                           "Seorak rescue unlocks after the first battle win flag.",
                           "Seorak rescue mission should require " + StoryFlags.FirstBattleWon + ".",
                           pass, fail);
            CheckCondition(mission.completeFlag == StoryFlags.BaekRyeonRecruited,
                           "Seorak rescue completes when Baek Ryeon is recruited.",
                           "Seorak rescue mission should complete on " + StoryFlags.BaekRyeonRecruited + ".",
                           pass, fail);
            CheckCondition(ContainsFragment(mission.rewardPreview, "백련"),
                           "Seorak rescue reward preview names Baek Ryeon's join.",
                           "Seorak rescue reward preview does not mention Baek Ryeon's join.",
                           pass, fail);
        }

        BattleDefinition battle = BattleCatalog.Get(HubController.SeorakPassRescueBattleId);
        CheckCondition(battle != null, "Seorak rescue BattleDefinition exists.", "Missing Seorak rescue BattleDefinition.", pass, fail);
        if (battle != null)
        {
            CheckCondition(battle.id == HubController.SeorakPassRescueBattleId,
                           "Seorak battle definition keeps the expected id.",
                           "Seorak battle definition id mismatch.",
                           pass, fail);
            CheckCondition(ContainsExact(battle.roster, "백련"),
                           "Seorak battle roster includes Baek Ryeon as the guest ally.",
                           "Seorak battle roster does not include Baek Ryeon.",
                           pass, fail);
            CheckCondition(ContainsObjective(battle.objectives, "OBJ_DEFEAT_YUDALGEUN") &&
                           ContainsObjective(battle.objectives, "OBJ_PROTECT_HERB_CART"),
                           "Seorak battle has boss defeat and rescue-protection objectives.",
                           "Seorak battle is missing required rescue objectives.",
                           pass, fail);
            CheckCondition(ContainsDeltaAtLeast(battle.factionOnWin, FactionIds.SeorakSpear, 5),
                           "Seorak battle rewards Seorak Spear faction support.",
                           "Seorak battle does not reward Seorak Spear faction support.",
                           pass, fail);
            CheckCondition(ContainsDeltaAtLeast(battle.approvalOnWin, CompanionCatalog.BaekRyeon, 4),
                           "Seorak battle rewards Baek Ryeon approval on victory.",
                           "Seorak battle does not reward Baek Ryeon approval on victory.",
                           pass, fail);
        }

        CompanionInfo baekRyeon = CompanionCatalog.Info(CompanionCatalog.BaekRyeon);
        CheckCondition(baekRyeon != null, "Baek Ryeon exists in CompanionCatalog.", "Baek Ryeon missing from CompanionCatalog.", pass, fail);
        CheckCondition(baekRyeon != null && baekRyeon.CanReceiveRomanticEffects,
                       "Baek Ryeon allows romantic presentation effects.",
                       "Baek Ryeon should allow romantic presentation effects.",
                       pass, fail);

        AuthoringContentManifest manifest = AuthoringContentManifest.LoadFromResources();
        CheckDialogueScene(manifest, "chapter1_baek_ryeon_join_before_battle", true, pass, fail);
        CheckDialogueScene(manifest, "chapter1_baek_ryeon_join_after_battle", false, pass, fail);

        RequireTexture(Root + "/Resources/Backgrounds/Dialogue/bg_seorak_pass_rescue_meeting.png", pass, fail);
        RequireTexture(Root + "/Resources/Backgrounds/Dialogue/bg_seorak_spear_gate_winter.png", pass, fail);
        RequireTexture(Root + "/Resources/Backgrounds/Dialogue/bg_seorak_spear_council_hall.png", pass, fail);
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

    private static void CheckDialogueScene(AuthoringContentManifest manifest, string sceneId, bool requireChoice,
                                           List<string> pass, List<string> fail)
    {
        DialogueScript script = AuthoringDialogueAdapter.ToDialogueScript(manifest, sceneId);
        CheckCondition(script.Nodes.Count > 0,
                       "Dialogue scene exists: " + sceneId,
                       "Missing or empty dialogue scene: " + sceneId,
                       pass, fail);
        if (script.Nodes.Count == 0)
        {
            return;
        }

        bool hasBaekRyeon = false;
        bool hasBackground = false;
        bool hasPortrait = false;
        bool hasChoice = false;
        bool hasApprovalChange = false;
        for (int i = 0; i < script.Nodes.Count; i++)
        {
            DialogueNode node = script.Nodes[i];
            if (node == null)
            {
                continue;
            }

            hasBaekRyeon |= node.speakerId == CompanionCatalog.BaekRyeon || node.speakerName == "백련";
            hasBackground |= !string.IsNullOrEmpty(node.backgroundResource) ||
                             (!string.IsNullOrEmpty(node.backgroundId) && node.backgroundId.Contains("seorak"));
            hasPortrait |= !string.IsNullOrEmpty(node.portraitResource);
            if (!node.HasChoices)
            {
                continue;
            }

            hasChoice = true;
            for (int c = 0; c < node.choices.Count; c++)
            {
                DialogueChoice choice = node.choices[c];
                if (choice == null)
                {
                    continue;
                }

                hasApprovalChange |= ContainsDeltaAny(choice.approvalChanges, CompanionCatalog.BaekRyeon);
            }
        }

        CheckCondition(hasBaekRyeon, "Dialogue scene includes Baek Ryeon: " + sceneId,
                       "Dialogue scene does not include Baek Ryeon: " + sceneId, pass, fail);
        CheckCondition(hasBackground, "Dialogue scene has Seorak background binding: " + sceneId,
                       "Dialogue scene lacks Seorak background binding: " + sceneId, pass, fail);
        CheckCondition(hasPortrait, "Dialogue scene has standing/portrait binding: " + sceneId,
                       "Dialogue scene lacks standing/portrait binding: " + sceneId, pass, fail);
        if (requireChoice)
        {
            CheckCondition(hasChoice && hasApprovalChange,
                           "Before-battle dialogue choices can adjust Baek Ryeon approval.",
                           "Before-battle dialogue should include a Baek Ryeon approval choice.",
                           pass, fail);
        }
    }

    private static MissionInfo FindMission(string missionId)
    {
        foreach (MissionInfo mission in MissionCatalog.All)
        {
            if (mission != null && mission.id == missionId)
            {
                return mission;
            }
        }

        return null;
    }

    private static bool ContainsExact(List<string> values, string expected)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i] == expected)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsFragment(List<string> values, string fragment)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (!string.IsNullOrEmpty(values[i]) && values[i].Contains(fragment))
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsObjective(List<BattleObjective> objectives, string objectiveId)
    {
        if (objectives == null)
        {
            return false;
        }

        for (int i = 0; i < objectives.Count; i++)
        {
            if (objectives[i] != null && objectives[i].id == objectiveId)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsDeltaAtLeast(List<IdDelta> values, string id, int minimumDelta)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].id == id && values[i].delta >= minimumDelta)
            {
                return true;
            }
        }

        return false;
    }

    private static bool ContainsDeltaAny(List<IdDelta> values, string id)
    {
        if (values == null)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (values[i].id == id && values[i].delta != 0)
            {
                return true;
            }
        }

        return false;
    }

    private static void CheckCondition(bool condition, string passMessage, string failMessage, List<string> pass,
                                       List<string> fail)
    {
        if (condition)
        {
            pass.Add(passMessage);
        }
        else
        {
            fail.Add(failMessage);
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

    private static void CheckPaintedMapBackdropBinding(BattleTestController controller, List<string> pass,
                                                       List<string> fail)
    {
        if (controller == null)
        {
            return;
        }

        MethodInfo ensureSprites = typeof(BattleTestController).GetMethod("EnsureMapVisualSprites",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (ensureSprites == null)
        {
            fail.Add("Could not inspect BattleTest painted map backdrop binding.");
            return;
        }

        try
        {
            ensureSprites.Invoke(controller, null);
        }
        catch (System.Exception ex)
        {
            fail.Add("Painted map backdrop binding threw during validation: " + ex.GetType().Name);
            return;
        }

        Sprite backdrop = GetPrivate<Sprite>(controller, "paintedMapBackdropSprite");
        if (controller.mapVariant == BattleTestMapVariant.BaekduSnowGate && backdrop != null)
        {
            pass.Add("BattleTest keeps the painted Baekdu map backdrop: " + backdrop.name + ".");
        }
        else if (controller.mapVariant == BattleTestMapVariant.BaekduSnowGate)
        {
            fail.Add("BattleTest painted Baekdu map backdrop is missing.");
        }
    }

    private static void CheckBattleAdjacencyMath(BattleTestController controller, List<string> pass, List<string> fail)
    {
        MethodInfo gridDistance = typeof(BattleTestController).GetMethod("GridDistance",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (gridDistance == null)
        {
            fail.Add("Could not inspect BattleTest attack distance math.");
            return;
        }

        Vector2Int center = new Vector2Int(8, 5);
        Vector2Int[] directNeighbors =
        {
            new Vector2Int(9, 5),
            new Vector2Int(7, 5),
            new Vector2Int(8, 6),
            new Vector2Int(8, 4)
        };

        bool directNeighborsAreAdjacent = true;
        for (int i = 0; i < directNeighbors.Length; i++)
        {
            int distance = (int)gridDistance.Invoke(controller, new object[] { center, directNeighbors[i] });
            directNeighborsAreAdjacent &= distance == 1;
        }

        int diagonalDistance = (int)gridDistance.Invoke(controller, new object[] { center, new Vector2Int(9, 6) });
        bool hasBasicMeleeRange = false;
        if (controller.unitDefinitions != null)
        {
            for (int i = 0; i < controller.unitDefinitions.Length; i++)
            {
                BattleTestUnitDefinition definition = controller.unitDefinitions[i];
                if (definition != null && definition.attackRange >= 1)
                {
                    hasBasicMeleeRange = true;
                    break;
                }
            }
        }

        CheckCondition(directNeighborsAreAdjacent && diagonalDistance == 2,
                       "BattleTest basic attack distance treats four direct neighbor tiles as range 1.",
                       "BattleTest basic attack distance no longer treats direct neighbor tiles as range 1.",
                       pass, fail);
        CheckCondition(hasBasicMeleeRange,
                       "BattleTest has units with basic attack range 1 or higher.",
                       "BattleTest has no unit definition with basic attack range 1 or higher.",
                       pass, fail);
    }

    private static void CheckBattleTileClickTargetResolution(BattleTestController controller, List<string> pass, List<string> fail)
    {
        MethodInfo resolveClickedUnit = typeof(BattleTestController).GetMethod("ResolveClickedUnit",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo resolveAttackClickTarget = typeof(BattleTestController).GetMethod("ResolveAttackClickTarget",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo canBasicAttackTarget = typeof(BattleTestController).GetMethod("CanBasicAttackTarget",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo gridDistance = typeof(BattleTestController).GetMethod("GridDistance",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo buildBattle = typeof(BattleTestController).GetMethod("BuildBattle",
            BindingFlags.Instance | BindingFlags.NonPublic);

        if (resolveClickedUnit == null || resolveAttackClickTarget == null || canBasicAttackTarget == null ||
            gridDistance == null || buildBattle == null)
        {
            fail.Add("Could not inspect BattleTest tile click target resolution.");
            return;
        }

        BattleTestUnitDefinition attackerDefinition = null;
        BattleTestUnitDefinition enemyDefinition = null;
        if (controller.unitDefinitions != null)
        {
            for (int i = 0; i < controller.unitDefinitions.Length; i++)
            {
                BattleTestUnitDefinition definition = controller.unitDefinitions[i];
                if (definition == null)
                {
                    continue;
                }

                if (attackerDefinition == null &&
                    definition.faction == Faction.Ally &&
                    definition.attackRange >= 1)
                {
                    attackerDefinition = definition;
                }

                if (enemyDefinition == null && definition.faction == Faction.Enemy)
                {
                    enemyDefinition = definition;
                }
            }
        }

        if (attackerDefinition == null || enemyDefinition == null)
        {
            fail.Add("BattleTest tile click targeting validation could not find an adjacent-capable ally and enemy definition.");
            return;
        }

        buildBattle.Invoke(controller, null);
        List<BattleTestUnit> runtimeUnits = GetPrivate<List<BattleTestUnit>>(controller, "units");
        BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
        if (runtimeUnits == null || tiles == null)
        {
            fail.Add("BattleTest tile click targeting validation could not inspect runtime units.");
            return;
        }

        List<BattleTestUnit> originalUnits = new List<BattleTestUnit>(runtimeUnits);

        try
        {
            BattleTestUnit attacker = new BattleTestUnit(attackerDefinition, null);
            attacker.cell = new Vector2Int(6, 1);

            BattleTestUnit enemy = new BattleTestUnit(enemyDefinition, null);
            enemy.cell = new Vector2Int(7, 1);

            BattleTestTile enemyTile = tiles[enemy.cell.x, enemy.cell.y];
            BattleTestTile nearbyTile = tiles[7, 2];
            if (enemyTile == null || nearbyTile == null)
            {
                fail.Add("BattleTest tile click targeting validation could not find lower approach test tiles.");
                return;
            }

            runtimeUnits.Clear();
            runtimeUnits.Add(attacker);
            runtimeUnits.Add(enemy);

            object resolved = resolveClickedUnit.Invoke(controller, new object[] { null, enemyTile });
            object resolvedNearby = resolveAttackClickTarget.Invoke(controller, new object[] { attacker, null, nearbyTile });
            bool canAttackAdjacent = (bool)canBasicAttackTarget.Invoke(controller, new object[] { attacker, enemy });
            int distance = (int)gridDistance.Invoke(controller, new object[] { attacker.cell, enemy.cell });

            CheckCondition(ReferenceEquals(resolved, enemy) && distance == 1 && distance <= attacker.definition.attackRange,
                           "BattleTest tile click targeting resolves an adjacent enemy-occupied tile to that enemy.",
                           "BattleTest tile click targeting did not resolve an adjacent enemy-occupied tile to that enemy.",
                           pass, fail);
            CheckCondition(canAttackAdjacent,
                           "BattleTest basic attack permits a valid enemy on the direct front tile.",
                           "BattleTest basic attack rejected a valid enemy on the direct front tile.",
                           pass, fail);
            CheckCondition(ReferenceEquals(resolvedNearby, enemy),
                           "BattleTest attack mode tolerates a one-tile visual click offset around a unique adjacent enemy.",
                           "BattleTest attack mode did not tolerate a one-tile visual click offset around a unique adjacent enemy.",
                           pass, fail);
        }
        catch (System.Exception ex)
        {
            fail.Add("BattleTest tile click targeting validation threw: " + ex.GetType().Name);
        }
        finally
        {
            runtimeUnits.Clear();
            runtimeUnits.AddRange(originalUnits);
        }
    }

    private static void CheckBattleHudForecastPolicy(List<string> pass, List<string> fail)
    {
        MethodInfo shouldShowForecast = typeof(BattleHUDController).GetMethod("ShouldShowForecast",
            BindingFlags.Static | BindingFlags.NonPublic);
        if (shouldShowForecast == null)
        {
            fail.Add("Could not inspect Battle HUD forecast display policy.");
            return;
        }

        BattleHudSnapshot moveModeEnemyHover = new BattleHudSnapshot
        {
            commandMode = BattleCommandMode.Move,
            canAttack = true,
            hasForecast = true,
            forecastLeft = "Jin Seoyul\nBasic Attack",
            forecastRight = "Enemy\nHP 30 -> 22",
            battleOver = false
        };

        BattleHudSnapshot emptyMoveHover = new BattleHudSnapshot
        {
            commandMode = BattleCommandMode.Move,
            canAttack = true,
            hasForecast = true,
            forecastLeft = string.Empty,
            forecastRight = string.Empty,
            battleOver = false
        };

        BattleHudSnapshot attackModeEnemyHover = new BattleHudSnapshot
        {
            commandMode = BattleCommandMode.Attack,
            canAttack = false,
            hasForecast = true,
            forecastLeft = "Jin Seoyul\nBasic Attack",
            forecastRight = "Enemy\nHP 30 -> 22",
            battleOver = false
        };

        bool moveModeShows = (bool)shouldShowForecast.Invoke(null, new object[] { moveModeEnemyHover });
        bool emptyMoveHides = !(bool)shouldShowForecast.Invoke(null, new object[] { emptyMoveHover });
        bool attackModeShows = (bool)shouldShowForecast.Invoke(null, new object[] { attackModeEnemyHover });

        CheckCondition(moveModeShows,
                       "Battle HUD shows combat forecast while hovering an enemy in move mode.",
                       "Battle HUD hides combat forecast while hovering an enemy in move mode.",
                       pass, fail);
        CheckCondition(emptyMoveHides,
                       "Battle HUD keeps forecast hidden for move mode without target context.",
                       "Battle HUD shows an empty forecast panel in move mode.",
                       pass, fail);
        CheckCondition(attackModeShows,
                       "Battle HUD keeps forecast visible in explicit attack mode.",
                       "Battle HUD no longer shows forecast in explicit attack mode.",
                       pass, fail);
    }

    private static void CheckDeploymentDragAndFacingPolicy(List<string> pass, List<string> fail)
    {
        MethodInfo beginDrag = typeof(BattleTestController).GetMethod("HudBeginDeploymentDrag",
            BindingFlags.Instance | BindingFlags.Public);
        MethodInfo dropDrag = typeof(BattleTestController).GetMethod("HudDropDeploymentUnit",
            BindingFlags.Instance | BindingFlags.Public);
        MethodInfo faceDeployment = typeof(BattleTestController).GetMethod("FaceUnitsForDeployment",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo deploymentCamera = typeof(BattleTestController).GetMethod("FocusCameraOnDeploymentOverview",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo deploymentCameraSize = typeof(BattleTestController).GetMethod("CalculateDeploymentCameraSize",
            BindingFlags.Instance | BindingFlags.NonPublic);

        CheckCondition(beginDrag != null && dropDrag != null && faceDeployment != null,
                       "Battle deployment supports dragging roster characters onto starting cells.",
                       "Battle deployment drag/drop API is missing.",
                       pass, fail);
        CheckCondition(deploymentCamera != null && deploymentCameraSize != null,
                       "Battle deployment uses an overview camera that keeps allies and enemies in frame.",
                       "Battle deployment can still zoom to one ally and push enemies out of frame.",
                       pass, fail);

        System.Type dragHandler = typeof(BattleHUDController).GetNestedType("DeploymentDragHandler",
            BindingFlags.NonPublic);
        bool hasDragHandler = dragHandler != null &&
                              typeof(IBeginDragHandler).IsAssignableFrom(dragHandler) &&
                              typeof(IDragHandler).IsAssignableFrom(dragHandler) &&
                              typeof(IEndDragHandler).IsAssignableFrom(dragHandler);
        CheckCondition(hasDragHandler,
                       "Battle deployment roster slots have begin/drag/end handlers.",
                       "Battle deployment roster slots are missing drag handlers.",
                       pass, fail);

        FieldInfo facingField = typeof(CharacterVisualController).GetField("facingVector",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (facingField == null)
        {
            fail.Add("Could not inspect CharacterVisualController default facing.");
            return;
        }

        GameObject visualObject = new GameObject("VisualUpgradeValidator_DefaultFacing");
        visualObject.hideFlags = HideFlags.HideAndDontSave;
        try
        {
            CharacterVisualController visual = visualObject.AddComponent<CharacterVisualController>();
            Vector2 facing = (Vector2)facingField.GetValue(visual);
            CheckCondition(Vector2.Dot(facing.normalized, Vector2.down) > 0.99f,
                           "Battle characters default to front-facing idle before movement.",
                           "Battle characters still default to side-facing idle before movement.",
                           pass, fail);
        }
        finally
        {
            Object.DestroyImmediate(visualObject);
        }
    }

    private static void CheckDirectionalWalkAnimationPolicy(List<string> pass, List<string> fail)
    {
        string[] heroIds =
        {
            "jin_seoyul",
            "han_biyeon",
            "park_sungjun",
            "do_arin",
            "shin_seoa",
            "baek_ryeon"
        };

        bool allHeroesHaveDirectionalWalkFrames = true;
        bool allHeroesUseReadableWalkTiming = true;
        foreach (string heroId in heroIds)
        {
            string path = Root + "/Art/Characters/" + heroId + "/VisualData/" + heroId + "_visual.asset";
            CharacterVisualData visual = AssetDatabase.LoadAssetAtPath<CharacterVisualData>(path);
            if (visual == null)
            {
                allHeroesHaveDirectionalWalkFrames = false;
                allHeroesUseReadableWalkTiming = false;
                continue;
            }

            int frontFrames = visual.moveFrames == null ? 0 : visual.moveFrames.Length;
            int sideFrames = visual.moveSideFrames == null ? 0 : visual.moveSideFrames.Length;
            int backFrames = visual.moveBackFrames == null ? 0 : visual.moveBackFrames.Length;
            if (frontFrames < 4 || sideFrames < 4 || backFrames < 4)
            {
                allHeroesHaveDirectionalWalkFrames = false;
            }

            GameObject visualObject = new GameObject("VisualUpgradeValidator_Walk_" + heroId);
            visualObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                CharacterVisualController controller = visualObject.AddComponent<CharacterVisualController>();
                controller.visual = visual;
                if (controller.WalkSecondsPerTile() < 0.44f)
                {
                    allHeroesUseReadableWalkTiming = false;
                }
            }
            finally
            {
                Object.DestroyImmediate(visualObject);
            }
        }

        FieldInfo leftFoot = typeof(CharacterVisualController).GetField("leftFootRenderer",
            BindingFlags.Instance | BindingFlags.Public);
        FieldInfo rightFoot = typeof(CharacterVisualController).GetField("rightFootRenderer",
            BindingFlags.Instance | BindingFlags.Public);
        MethodInfo setStridePhase = typeof(CharacterVisualController).GetMethod("SetMoveStridePhase",
            BindingFlags.Instance | BindingFlags.Public);
        bool hasFootContactRig = leftFoot != null && rightFoot != null && setStridePhase != null;
        bool isoDiagonalFacingIsNotSide = false;
        MethodInfo faceDirection = typeof(CharacterVisualController).GetMethod("FaceDirection",
            BindingFlags.Instance | BindingFlags.Public);
        MethodInfo sideAmount = typeof(CharacterVisualController).GetMethod("FacingSideAmount",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo backAmount = typeof(CharacterVisualController).GetMethod("FacingBackAmount",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo frontAmount = typeof(CharacterVisualController).GetMethod("FacingFrontAmount",
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (faceDirection != null && sideAmount != null && backAmount != null && frontAmount != null)
        {
            GameObject visualObject = new GameObject("VisualUpgradeValidator_IsoFacing");
            visualObject.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                CharacterVisualController controller = visualObject.AddComponent<CharacterVisualController>();
                faceDirection.Invoke(controller, new object[] { new Vector2(0.58f, 0.31f) });
                float backDiagonalSide = (float)sideAmount.Invoke(controller, null);
                float backDiagonalBack = (float)backAmount.Invoke(controller, null);
                faceDirection.Invoke(controller, new object[] { new Vector2(-0.58f, -0.31f) });
                float frontDiagonalSide = (float)sideAmount.Invoke(controller, null);
                float frontDiagonalFront = (float)frontAmount.Invoke(controller, null);
                faceDirection.Invoke(controller, new object[] { Vector2.right });
                float pureSide = (float)sideAmount.Invoke(controller, null);
                isoDiagonalFacingIsNotSide = backDiagonalSide <= 0.05f && frontDiagonalSide <= 0.05f &&
                                             backDiagonalBack >= 0.40f && frontDiagonalFront >= 0.40f &&
                                             pureSide >= 0.90f;
            }
            finally
            {
                Object.DestroyImmediate(visualObject);
            }
        }

        CheckCondition(allHeroesHaveDirectionalWalkFrames,
                       "All six BattleTest heroes have 4-frame front/side/back walk cycles.",
                       "BattleTest hero walk cycles are missing front/side/back 4-frame coverage.",
                       pass, fail);
        CheckCondition(allHeroesUseReadableWalkTiming,
                       "BattleTest hero walk timing is clamped to readable two-foot movement.",
                       "BattleTest hero walk timing can still look like sliding or one-foot movement.",
                       pass, fail);
        CheckCondition(hasFootContactRig,
                       "CharacterVisualController exposes left/right foot contact renderers and stride phase control.",
                       "CharacterVisualController is missing the two-foot contact rig or stride phase control.",
                       pass, fail);
        CheckCondition(isoDiagonalFacingIsNotSide,
                       "Isometric diagonal movement selects front/back facing instead of side-walk leakage.",
                       "Isometric diagonal movement can still leak into side-walk facing.",
                       pass, fail);
    }

    private static void CheckBattleTerrainPlacementPolicy(BattleTestController controller, List<string> pass,
                                                          List<string> fail)
    {
        MethodInfo buildBattle = typeof(BattleTestController).GetMethod("BuildBattle",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo canStandOnTile = typeof(BattleTestController).GetMethod("CanStandOnTile",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo isDeploymentCell = typeof(BattleTestController).GetMethod("IsDeploymentCell",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo isBlockedByInteractable = typeof(BattleTestController).GetMethod("IsCellBlockedByInteractable",
            BindingFlags.Instance | BindingFlags.NonPublic);
        MethodInfo noStandMask = typeof(BattleTestController).GetMethod("IsBaekduSnowGatePaintedNoStandCell",
            BindingFlags.Static | BindingFlags.NonPublic);

        if (buildBattle == null || canStandOnTile == null || isDeploymentCell == null ||
            isBlockedByInteractable == null || noStandMask == null)
        {
            fail.Add("Could not inspect BattleTest terrain placement safety policy.");
            return;
        }

        try
        {
            bool masksLeftWater = (bool)noStandMask.Invoke(null, new object[] { new Vector2Int(1, 5) });
            bool masksGateWall = (bool)noStandMask.Invoke(null, new object[] { new Vector2Int(7, 10) });
            bool masksUpperFence = (bool)noStandMask.Invoke(null, new object[] { new Vector2Int(13, 8) });
            bool allowsGateStairs = !(bool)noStandMask.Invoke(null, new object[] { new Vector2Int(10, 5) });
            bool allowsRightFlankSnow = !(bool)noStandMask.Invoke(null, new object[] { new Vector2Int(12, 7) });
            bool allowsCentralApproach = !(bool)noStandMask.Invoke(null, new object[] { new Vector2Int(8, 3) });
            CheckCondition(masksLeftWater && masksGateWall && masksUpperFence && allowsGateStairs &&
                           allowsRightFlankSnow && allowsCentralApproach,
                           "Baekdu painted-map mask blocks decorative backdrop art while keeping visible approach routes usable.",
                           "Baekdu painted-map mask still mismatches decorative blockers or visible approach routes.",
                           pass, fail);

            buildBattle.Invoke(controller, null);
            BattleTestTile[,] tiles = GetPrivate<BattleTestTile[,]>(controller, "tiles");
            List<BattleTestUnit> units = GetPrivate<List<BattleTestUnit>>(controller, "units");
            bool battleOver = GetPrivate<bool>(controller, "battleOver");
            if (tiles == null || units == null)
            {
                fail.Add("BattleTest terrain placement validation could not inspect runtime tiles or units.");
                return;
            }

            bool approachLaneSafe = false;
            BattleTestTile centralApproach = tiles[8, 3];
            if (centralApproach != null)
            {
                approachLaneSafe = centralApproach.terrain != TerrainType.DeepWater &&
                                   (bool)canStandOnTile.Invoke(controller, new object[] { centralApproach });
            }

            bool backdropBlockersBlocked = true;
            Vector2Int[] backdropBlockerCells =
            {
                new Vector2Int(8, 0),
                new Vector2Int(9, 1),
                new Vector2Int(10, 2),
                new Vector2Int(14, 3),
                new Vector2Int(5, 4),
                new Vector2Int(5, 5),
                new Vector2Int(4, 6),
                new Vector2Int(12, 8),
                new Vector2Int(13, 8),
                new Vector2Int(1, 5),
                new Vector2Int(7, 10)
            };
            foreach (Vector2Int cell in backdropBlockerCells)
            {
                BattleTestTile tile = tiles[cell.x, cell.y];
                if (tile != null && (bool)canStandOnTile.Invoke(controller, new object[] { tile }))
                {
                    backdropBlockersBlocked = false;
                    break;
                }
            }

            bool allUnitsSafe = true;
            bool enemiesAvoidStartObjective = true;
            bool allEnemyBodiesRenderable = true;
            bool enemiesStartOnVisibleApproach = true;
            foreach (BattleTestUnit unit in units)
            {
                if (unit == null || unit.defeated || unit.cell.x < 0 || unit.cell.y < 0 ||
                    unit.cell.x >= tiles.GetLength(0) || unit.cell.y >= tiles.GetLength(1))
                {
                    allUnitsSafe = false;
                    continue;
                }

                BattleTestTile tile = tiles[unit.cell.x, unit.cell.y];
                bool canStand = tile != null && (bool)canStandOnTile.Invoke(controller, new object[] { tile });
                bool blocked = (bool)isBlockedByInteractable.Invoke(controller, new object[] { unit.cell });
                if (!canStand || blocked)
                {
                    allUnitsSafe = false;
                    break;
                }

                if (unit.definition.faction == Faction.Enemy && tile != null && tile.objective)
                {
                    enemiesAvoidStartObjective = false;
                    break;
                }

                if (unit.definition.faction == Faction.Enemy)
                {
                    if (unit.cell.x < 7 || unit.cell.x > 9 || unit.cell.y < 2 || unit.cell.y > 3)
                    {
                        enemiesStartOnVisibleApproach = false;
                        break;
                    }

                    CharacterVisualController visual = unit.view == null
                                                           ? null
                                                           : unit.view.GetComponent<CharacterVisualController>();
                    SpriteRenderer body = visual == null ? null : visual.bodyRenderer;
                    if (visual == null || body == null || !body.enabled || body.sprite == null ||
                        body.sortingOrder < 2300)
                    {
                        allEnemyBodiesRenderable = false;
                        break;
                    }
                }
            }

            bool allDeploymentCellsSafe = true;
            for (int y = 0; y < tiles.GetLength(1); y++)
            {
                for (int x = 0; x < tiles.GetLength(0); x++)
                {
                    BattleTestTile tile = tiles[x, y];
                    if (tile == null)
                    {
                        continue;
                    }

                    bool deployment = (bool)isDeploymentCell.Invoke(controller, new object[] { tile.cell });
                    if (!deployment)
                    {
                        continue;
                    }

                    bool canStand = (bool)canStandOnTile.Invoke(controller, new object[] { tile });
                    bool blocked = (bool)isBlockedByInteractable.Invoke(controller, new object[] { tile.cell });
                    if (!canStand || blocked)
                    {
                        allDeploymentCellsSafe = false;
                        break;
                    }
                }
            }

            CheckCondition(approachLaneSafe,
                           "Baekdu tile (8,3) is lower painted stone approach, not deep water.",
                           "Baekdu tile (8,3) is still treated as water or non-standing terrain.",
                           pass, fail);
            CheckCondition(backdropBlockersBlocked,
                           "Baekdu decorative wall/fence/backdrop cells are blocked while visible routes remain usable.",
                           "Baekdu decorative wall/fence/backdrop cells still include standable blockers.",
                           pass, fail);
            CheckCondition(allUnitsSafe,
                           "BattleTest runtime units spawn only on standable painted-map cells.",
                           "BattleTest runtime units can spawn on blocked painted-map cells.",
                           pass, fail);
            CheckCondition(!battleOver && enemiesAvoidStartObjective,
                           "BattleTest starts active; enemies do not spawn on breach objectives.",
                           "BattleTest can end immediately because an enemy starts on an objective.",
                           pass, fail);
            CheckCondition(enemiesStartOnVisibleApproach,
                           "BattleTest enemies start on the lower-right visible approach, away from ally silhouettes and gate UI.",
                           "BattleTest enemies still start where ally silhouettes, gate foreground, or UI can hide their bodies.",
                           pass, fail);
            CheckCondition(allEnemyBodiesRenderable,
                           "BattleTest enemies render full bodies above the painted foreground, not just HP bars.",
                           "BattleTest enemies can hide behind the painted map while only HP bars remain visible.",
                           pass, fail);
            CheckCondition(allDeploymentCellsSafe,
                           "Battle deployment cells are restricted to standable, unblocked starting cells.",
                           "Battle deployment cells still include blocked backdrop or prop cells.",
                           pass, fail);
        }
        catch (System.Exception ex)
        {
            fail.Add("BattleTest terrain placement validation threw: " + ex.GetType().Name);
        }
    }

    private static T GetPrivate<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        return field == null ? default(T) : (T)field.GetValue(target);
    }
}
}
#endif
