using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace JoseonMurimTactics.Editor
{
public static class NoMapAssetChecklistBuilder
{
    private const string Root = "Assets/JoseonMurimTactics";
    private const string SourceChecklistReference = "codex_no_map_asset_checklist.txt";
    private const string ManifestFolder = Root + "/Resources/AssetManifest";
    private const string GeneratedManifestPath = ManifestFolder + "/generated_asset_manifest.json";
    private const string IconMappingPath = ManifestFolder + "/icon_mapping.json";
    private const string VfxMappingPath = ManifestFolder + "/vfx_mapping.json";
    private const string AudioCueManifestPath = ManifestFolder + "/audio_cue_manifest.json";
    private const string DocPath = Root + "/Docs/ASSET_TODO_NO_MAP.md";

    private static readonly Vector2 CenterPivot = new Vector2(0.5f, 0.5f);
    private static readonly Vector2 BottomCenterPivot = new Vector2(0.5f, 0.03f);

    [MenuItem("Joseon Murim Tactics/Assets/Rebuild No-Map Placeholder Assets (Dangerous)")]
    public static void Rebuild()
    {
        bool forceOverwrite = HasCommandLineFlag("-forceNoMapAssetOverwrite");
        if (!Application.isBatchMode)
        {
            forceOverwrite = EditorUtility.DisplayDialog(
                "Rebuild No-Map Placeholder Assets",
                "This can overwrite placeholder PNGs. Use Repair Import Settings Only unless you intentionally want to regenerate art.",
                "Overwrite/Rebuild",
                "Cancel");
            if (!forceOverwrite)
            {
                Debug.Log("[NoMapAssetChecklistBuilder] Rebuild canceled.");
                return;
            }
        }

        RebuildInternal(forceOverwrite);
    }

    [MenuItem("Joseon Murim Tactics/Assets/Repair No-Map Placeholder Import Settings Only")]
    public static void RepairImportSettingsOnly()
    {
        EnsureFolders();

        List<TextureSpec> specs = BuildTextureSpecs();
        AssetDatabase.Refresh();

        foreach (TextureSpec spec in specs)
        {
            if (File.Exists(AssetPathToAbsolutePath(spec.Path)))
            {
                ConfigureSpriteImporter(spec);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[NoMapAssetChecklistBuilder] Repaired import settings without regenerating PNGs.");
    }

    private static void RebuildInternal(bool forceOverwrite)
    {
        EnsureFolders();

        List<TextureSpec> specs = BuildTextureSpecs();
        Dictionary<string, VerificationRecord> verification = new Dictionary<string, VerificationRecord>();

        foreach (TextureSpec spec in specs)
        {
            WriteTexture(spec, verification, forceOverwrite);
        }

        AssetDatabase.Refresh();

        foreach (TextureSpec spec in specs)
        {
            ConfigureSpriteImporter(spec);
        }

        AssetDatabase.Refresh();

        Dictionary<string, GameObject> vfxPrefabs = CreateVfxPrefabs(specs);
        Dictionary<string, EnemyPlaceholderVisualData> enemyVisuals = CreateEnemyVisualData(specs);
        Dictionary<string, GameObject> enemyPrefabs = CreateEnemyPrefabs(specs, enemyVisuals);
        List<WeaponLinkRecord> weaponLinks = ConnectWeaponAnimationSets(vfxPrefabs);

        WriteGeneratedManifest(specs, verification);
        WriteIconMapping(specs);
        WriteVfxMapping(weaponLinks);
        WriteAudioCueManifest();
        WriteNoMapDoc(specs, vfxPrefabs.Count, enemyPrefabs.Count, weaponLinks);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        int transparentAssets = verification.Values.Count(record => record.TransparentPixelCount > 0);
        Debug.Log(
            "[NoMapAssetChecklistBuilder] Generated " + specs.Count.ToString(CultureInfo.InvariantCulture) +
            " no-map placeholder sprite records, " + vfxPrefabs.Count.ToString(CultureInfo.InvariantCulture) +
            " VFX prefabs, " + enemyPrefabs.Count.ToString(CultureInfo.InvariantCulture) +
            " enemy prefabs. Transparent-pixel sprites: " +
            transparentAssets.ToString(CultureInfo.InvariantCulture) + ".");
    }

    private static void EnsureFolders()
    {
        string[] folders =
        {
            Root + "/Art/VFX/Common",
            Root + "/Art/VFX/Characters/ParkSungjun",
            Root + "/Art/VFX/Characters/BaekRyeon",
            Root + "/Art/VFX/Characters/DoArin",
            Root + "/Art/VFX/Characters/JinSeoyul",
            Root + "/Art/VFX/Characters/SeoA",
            Root + "/Art/VFX/Characters/HanBiyeon",
            Root + "/Prefabs/VFX",
            Root + "/ScriptableObjects/Enemies/Visuals",
            Root + "/Art/UI/Icons/Elements",
            Root + "/Art/UI/Icons/Weapons",
            Root + "/Art/UI/Icons/Statuses",
            Root + "/Art/UI/Icons/Combat",
            Root + "/Art/UI/Icons/Items",
            Root + "/Art/UI/Icons/HubActions",
            Root + "/Art/UI/BattleHUD",
            Root + "/Prefabs/UI/BattleHUD",
            Root + "/Art/UI/Hub",
            Root + "/Prefabs/UI/Hub",
            Root + "/Art/UI/Emblems",
            Root + "/Art/UI/Result",
            Root + "/Art/UI/SaveLoad",
            Root + "/Art/UI/Settings",
            Root + "/Art/Portraits/NPC",
            Root + "/Resources/Portraits/NPC",
            Root + "/Art/Characters/Enemies",
            Root + "/Art/Portraits/Enemies",
            Root + "/Prefabs/Units/Enemies",
            Root + "/Art/Backgrounds/Dialogue",
            Root + "/Audio/SFX",
            Root + "/Audio/BGM",
            ManifestFolder,
            Root + "/Docs"
        };

        foreach (string folder in folders)
        {
            EnsureAssetFolder(folder);
        }
    }

    private static List<TextureSpec> BuildTextureSpecs()
    {
        List<TextureSpec> specs = new List<TextureSpec>();

        AddVfxSpecs(specs);
        AddIconSpecs(specs);
        AddUiSpecs(specs);
        AddNpcPortraitSpecs(specs);
        AddEnemySpecs(specs);
        AddEmblemSpecs(specs);
        AddDialogueBackgroundSpecs(specs);

        return specs;
    }

    private static void AddVfxSpecs(List<TextureSpec> specs)
    {
        CharacterStyle park = new CharacterStyle("park_sungjun", "ParkSungjun", "Light", "Sword", new Color32(249, 232, 123, 255), new Color32(105, 232, 255, 255), new[] { "protagonist", "park" });
        CharacterStyle baek = new CharacterStyle("baek_ryeon", "BaekRyeon", "Ice", "Spear", new Color32(142, 222, 255, 255), new Color32(240, 255, 255, 255), new[] { "baek", "baekryeon" });
        CharacterStyle arin = new CharacterStyle("do_arin", "DoArin", "Fire", "Dao", new Color32(255, 103, 58, 255), new Color32(255, 209, 91, 255), new[] { "arin", "doarin" });
        CharacterStyle jin = new CharacterStyle("jin_seoyul", "JinSeoyul", "Lightning", "Staff", new Color32(120, 204, 255, 255), new Color32(255, 247, 118, 255), new[] { "jin", "jinseoyul" });
        CharacterStyle seoa = new CharacterStyle("seo_a", "SeoA", "WindFlower", "Fan", new Color32(246, 158, 214, 255), new Color32(133, 230, 184, 255), new[] { "shin_seoa" });
        CharacterStyle han = new CharacterStyle("han_biyeon", "HanBiyeon", "DarkPoison", "Dagger", new Color32(139, 94, 210, 255), new Color32(108, 226, 99, 255), new[] { "han", "hanbiyeon" });

        AddCharacterVfx(specs, park, new[]
        {
            "light_sword_slash",
            "radiant_cross_slash",
            "light_pillar",
            "star_flare_impact",
            "holy_guard_spark",
            "ultimate_divine_impact"
        });

        AddCharacterVfx(specs, baek, new[]
        {
            "frost_spear_thrust",
            "ice_spear_slash_arc",
            "ice_crystal_impact",
            "freeze_circle",
            "snowflake_bind",
            "ice_wall_burst"
        });

        AddCharacterVfx(specs, arin, new[]
        {
            "fire_dao_slash",
            "blazing_charge_trail",
            "ground_fire_burst",
            "burn_impact",
            "flame_ring",
            "meteor_dao_crash"
        });

        AddCharacterVfx(specs, jin, new[]
        {
            "lightning_staff_spin",
            "electric_dash_trail",
            "thunder_impact",
            "shock_spark",
            "lightning_ring",
            "staff_thunderfall"
        });

        AddCharacterVfx(specs, seoa, new[]
        {
            "flower_wind_arc",
            "fan_gust_slash",
            "petal_buff_aura",
            "confusion_pollen",
            "healing_breeze",
            "blossom_field"
        });

        AddCharacterVfx(specs, han, new[]
        {
            "shadow_dagger_slash",
            "poison_needle_projectile",
            "dark_poison_mist",
            "shadow_afterimage",
            "toxic_impact_splash",
            "assassin_mark"
        });

        string[] common =
        {
            "common_hit_spark",
            "common_guard_parry_spark",
            "common_critical_flash",
            "common_miss_smoke",
            "common_footstep_dust",
            "common_selection_ring",
            "common_target_marker",
            "common_acted_overlay",
            "common_defeated_shadow",
            "common_turn_start_aura",
            "common_damage_popup_burst",
            "common_heal_popup_burst",
            "common_buff_up_glow",
            "common_debuff_down_smoke"
        };

        foreach (string id in common)
        {
            specs.Add(TextureSpec.Vfx(
                id,
                Root + "/Art/VFX/Common/" + id + ".png",
                "Common combat VFX placeholder",
                "Common",
                string.Empty,
                string.Empty,
                new Color32(235, 238, 220, 255),
                id.Contains("debuff") || id.Contains("smoke") || id.Contains("defeated") ? new Color32(65, 66, 75, 255) : new Color32(255, 214, 109, 255),
                Array.Empty<string>()));
        }
    }

    private static void AddCharacterVfx(List<TextureSpec> specs, CharacterStyle style, string[] suffixes)
    {
        foreach (string suffix in suffixes)
        {
            string id = style.Id + "_" + suffix;
            specs.Add(TextureSpec.Vfx(
                id,
                Root + "/Art/VFX/Characters/" + style.FolderName + "/" + id + ".png",
                style.Id + " " + suffix.Replace('_', ' ') + " VFX placeholder",
                style.Element,
                style.Weapon,
                style.Id,
                style.Primary,
                style.Secondary,
                style.Aliases));
        }
    }

    private static void AddIconSpecs(List<TextureSpec> specs)
    {
        AddIconGroup(specs, "Elements", new[]
        {
            "icon_element_light",
            "icon_element_ice_frost",
            "icon_element_fire",
            "icon_element_lightning",
            "icon_element_wind_flower",
            "icon_element_dark_poison"
        });

        AddIconGroup(specs, "Weapons", new[]
        {
            "icon_weapon_sword",
            "icon_weapon_spear",
            "icon_weapon_dao",
            "icon_weapon_staff",
            "icon_weapon_fan",
            "icon_weapon_dagger",
            "icon_weapon_hidden_weapon",
            "icon_weapon_needle",
            "icon_weapon_bow",
            "icon_weapon_fist"
        });

        AddIconGroup(specs, "Statuses", new[]
        {
            "icon_status_guard",
            "icon_status_evade",
            "icon_status_acted",
            "icon_status_defeated",
            "icon_status_poison",
            "icon_status_burn",
            "icon_status_chill",
            "icon_status_freeze",
            "icon_status_shock",
            "icon_status_stun",
            "icon_status_stealth",
            "icon_status_slow",
            "icon_status_bind",
            "icon_status_blind",
            "icon_status_marked",
            "icon_status_break",
            "icon_status_morale_down",
            "icon_status_morale_up",
            "icon_status_inner_power",
            "icon_status_cooldown",
            "icon_status_buff_attack",
            "icon_status_buff_defense",
            "icon_status_buff_move",
            "icon_status_buff_hit",
            "icon_status_buff_evasion",
            "icon_status_debuff_attack",
            "icon_status_debuff_defense",
            "icon_status_debuff_move",
            "icon_status_debuff_hit"
        });

        AddIconGroup(specs, "Combat", new[]
        {
            "icon_d20_normal",
            "icon_d20_critical_20",
            "icon_d20_fumble_1",
            "icon_hit_chance",
            "icon_damage",
            "icon_counter",
            "icon_follow_up",
            "icon_push",
            "icon_terrain_bonus",
            "icon_cover",
            "icon_height_bonus",
            "icon_range_min",
            "icon_range_max"
        });

        AddIconGroup(specs, "Items", new[]
        {
            "item_silver_coin",
            "item_silver_pouch",
            "item_herb_bundle",
            "item_wood_bundle",
            "item_medicine_pack",
            "item_supply_crate",
            "item_bandit_loot",
            "item_sect_repair_material",
            "item_martial_clue_scroll",
            "item_dawn_slash_scroll",
            "item_stone_step_formation_scroll",
            "item_old_manual",
            "item_secret_letter",
            "item_mission_contract",
            "item_wanted_poster",
            "item_village_gratitude_plaque",
            "item_reputation_medal",
            "item_approval_heart",
            "item_faction_reputation_badge",
            "item_joseon_alliance_token",
            "item_black_mark_token"
        });

        AddIconGroup(specs, "HubActions", new[]
        {
            "action_chop_wood",
            "action_gather_herbs",
            "action_repair_roof",
            "action_training_dummy",
            "action_read_manual",
            "action_market_trade",
            "action_infirmary_heal",
            "action_tavern_rumor",
            "action_library_research",
            "action_sect_management",
            "action_companion_talk",
            "action_patrol"
        });
    }

    private static void AddIconGroup(List<TextureSpec> specs, string folder, string[] ids)
    {
        foreach (string id in ids)
        {
            ColorPair colors = PaletteFor(id);
            specs.Add(TextureSpec.Icon(
                id,
                Root + "/Art/UI/Icons/" + folder + "/" + id + ".png",
                folder + " icon placeholder",
                colors.Primary,
                colors.Secondary));
        }
    }

    private static void AddUiSpecs(List<TextureSpec> specs)
    {
        AddUiGroup(specs, Root + "/Art/UI/BattleHUD", new[]
        {
            "ui_panel_hanji_base",
            "ui_panel_dark_ink",
            "ui_panel_gold_frame",
            "ui_button_normal",
            "ui_button_hover",
            "ui_button_pressed",
            "ui_button_disabled",
            "ui_divider_gold",
            "ui_red_seal_stamp",
            "ui_ink_brush_corner",
            "ui_tooltip_frame",
            "ui_toast_frame",
            "ui_modal_frame",
            "ui_scroll_frame",
            "ui_tab_button_normal",
            "ui_tab_button_selected",
            "ui_checkbox_on",
            "ui_checkbox_off",
            "ui_slider_bar",
            "ui_slider_handle",
            "ui_battle_command_panel",
            "ui_battle_forecast_panel",
            "ui_turn_order_card",
            "ui_unit_status_card",
            "ui_hp_bar_bg",
            "ui_hp_bar_fill",
            "ui_inner_bar_bg",
            "ui_inner_bar_fill",
            "ui_morale_bar_bg",
            "ui_morale_bar_fill",
            "ui_break_bar_bg",
            "ui_break_bar_fill",
            "ui_cooldown_badge",
            "ui_range_badge",
            "ui_terrain_bonus_badge",
            "ui_damage_number_burst",
            "ui_critical_popup",
            "ui_miss_popup",
            "ui_counter_popup",
            "ui_followup_popup",
            "ui_guard_popup",
            "ui_evade_popup",
            "ui_btn_move",
            "ui_btn_attack",
            "ui_btn_skill",
            "ui_btn_guard",
            "ui_btn_wait",
            "ui_btn_end_turn",
            "ui_btn_cancel",
            "ui_btn_confirm",
            "ui_btn_inventory",
            "ui_btn_terrain_action"
        });

        AddUiGroup(specs, Root + "/Art/UI/Hub", new[]
        {
            "hub_sortie",
            "hub_worldmap",
            "hub_training",
            "hub_companions",
            "hub_sect",
            "hub_tavern",
            "hub_infirmary",
            "hub_market",
            "hub_library",
            "hub_save",
            "hub_settings",
            "hub_daily_action_energy",
            "hub_silver",
            "hub_renown",
            "hub_village_trust",
            "hub_sect_repair",
            "hub_training_xp",
            "hub_research_xp",
            "hub_rumor",
            "hub_notification_success",
            "hub_notification_warning",
            "hub_notification_error",
            "ui_mission_card_frame",
            "ui_companion_card_frame",
            "ui_faction_meter_frame",
            "ui_save_slot_empty",
            "ui_save_slot_filled",
            "ui_new_game_card",
            "ui_settings_frame"
        });

        AddUiGroup(specs, Root + "/Art/UI/Result", new[]
        {
            "result_victory_banner",
            "result_defeat_banner",
            "result_grade_s",
            "result_grade_a",
            "result_grade_b",
            "result_grade_c",
            "result_reward_panel",
            "result_renown_up",
            "result_renown_down",
            "result_approval_up",
            "result_approval_down",
            "result_rumor_scroll",
            "result_battle_record_stamp"
        });

        AddUiGroup(specs, Root + "/Art/UI/SaveLoad", new[]
        {
            "save_slot_empty",
            "save_slot_filled",
            "save_slot_selected",
            "save_slot_locked",
            "save_thumbnail_frame"
        });

        AddUiGroup(specs, Root + "/Art/UI/Settings", new[]
        {
            "settings_gear",
            "settings_volume_icon",
            "settings_resolution_icon",
            "settings_controls_icon",
            "settings_accessibility_icon",
            "settings_language_icon",
            "settings_reset_icon"
        });
    }

    private static void AddUiGroup(List<TextureSpec> specs, string folder, string[] ids)
    {
        foreach (string id in ids)
        {
            ColorPair colors = PaletteFor(id);
            int width = 512;
            int height = 512;
            if (id.Contains("banner"))
            {
                width = 1024;
                height = 256;
            }
            else if (id.Contains("panel") || id.Contains("frame") || id.Contains("card") || id.Contains("slot"))
            {
                width = 1024;
                height = 512;
            }
            else if (id.Contains("button") || id.StartsWith("ui_btn_", StringComparison.Ordinal))
            {
                width = 512;
                height = 192;
            }

            specs.Add(TextureSpec.Ui(
                id,
                folder + "/" + id + ".png",
                "UI sprite placeholder",
                width,
                height,
                colors.Primary,
                colors.Secondary));
        }
    }

    private static void AddNpcPortraitSpecs(List<TextureSpec> specs)
    {
        NpcStyle[] priorityNpcs =
        {
            new NpcStyle("park_mugyeom", new Color32(113, 127, 143, 255), new Color32(228, 207, 166, 255), new[] { "calm", "sick", "stern", "proud", "worried" }),
            new NpcStyle("yeon_ok", new Color32(89, 111, 128, 255), new Color32(218, 182, 132, 255), new[] { "stern", "angry", "approving", "worried", "calm" }),
            new NpcStyle("chohui", new Color32(117, 160, 136, 255), new Color32(238, 193, 154, 255), new[] { "gentle", "worried", "smile", "serious", "surprised" })
        };

        foreach (NpcStyle npc in priorityNpcs)
        {
            AddNpcSet(specs, npc, true);
        }

        NpcStyle[] candidates =
        {
            new NpcStyle("sobaek_village_chief", new Color32(123, 139, 118, 255), new Color32(215, 189, 151, 255), Array.Empty<string>()),
            new NpcStyle("mission_board_clerk", new Color32(112, 96, 74, 255), new Color32(229, 199, 150, 255), Array.Empty<string>()),
            new NpcStyle("market_merchant", new Color32(157, 112, 79, 255), new Color32(235, 196, 138, 255), Array.Empty<string>()),
            new NpcStyle("tavern_owner", new Color32(111, 75, 73, 255), new Color32(226, 184, 133, 255), Array.Empty<string>()),
            new NpcStyle("infirmary_doctor", new Color32(113, 154, 151, 255), new Color32(231, 207, 163, 255), Array.Empty<string>()),
            new NpcStyle("library_keeper", new Color32(92, 93, 126, 255), new Color32(213, 190, 146, 255), Array.Empty<string>()),
            new NpcStyle("sect_blacksmith", new Color32(120, 92, 79, 255), new Color32(220, 177, 129, 255), Array.Empty<string>()),
            new NpcStyle("suspicious_messenger", new Color32(78, 82, 89, 255), new Color32(210, 178, 132, 255), Array.Empty<string>())
        };

        foreach (NpcStyle npc in candidates)
        {
            AddNpcSet(specs, npc, false);
        }
    }

    private static void AddNpcSet(List<TextureSpec> specs, NpcStyle npc, bool includeExpressions)
    {
        string folder = Root + "/Art/Portraits/NPC/" + npc.Id;
        specs.Add(TextureSpec.Portrait(npc.Id + "_bust", folder + "/" + npc.Id + "_bust.png", "NPC bust placeholder", 1024, 1024, npc.Primary, npc.Secondary));
        specs.Add(TextureSpec.Portrait(npc.Id + "_face", folder + "/" + npc.Id + "_face.png", "NPC face icon placeholder", 512, 512, npc.Primary, npc.Secondary));

        string resourceFolder = Root + "/Resources/Portraits/NPC/" + npc.Id;
        specs.Add(TextureSpec.Portrait(npc.Id + "_face_runtime", resourceFolder + "/" + npc.Id + "_face.png", "NPC runtime face icon placeholder", 512, 512, npc.Primary, npc.Secondary));

        if (!includeExpressions)
        {
            return;
        }

        foreach (string expression in npc.Expressions)
        {
            specs.Add(TextureSpec.Portrait(
                npc.Id + "_" + expression,
                folder + "/" + npc.Id + "_" + expression + ".png",
                "NPC expression placeholder",
                512,
                512,
                npc.Primary,
                npc.Secondary));
        }
    }

    private static void AddEnemySpecs(List<TextureSpec> specs)
    {
        string[] enemies =
        {
            "enemy_iron_wolf_scout_leader",
            "enemy_iron_wolf_swordsman",
            "enemy_iron_wolf_spearman",
            "enemy_iron_wolf_archer",
            "enemy_iron_wolf_banner_guard",
            "enemy_iron_wolf_heavy_guard",
            "enemy_black_hat_boss_gwakchil",
            "enemy_black_hat_swordsman",
            "enemy_black_hat_archer",
            "enemy_black_hat_trapper",
            "enemy_black_hat_watchtower_shooter",
            "enemy_black_hat_poison_thug",
            "enemy_moyong_envoy",
            "enemy_ice_valley_sorcerer",
            "enemy_zhongyuan_spy",
            "enemy_lightning_device_bandit",
            "enemy_pro_zhongyuan_ronin",
            "enemy_incense_sorcerer",
            "enemy_poison_fog_agent",
            "enemy_shadow_frame_assassin"
        };

        string[] poses =
        {
            "idle",
            "move",
            "attack",
            "skill",
            "hit",
            "defeated",
            "acted"
        };

        foreach (string enemy in enemies)
        {
            ColorPair colors = PaletteFor(enemy);
            foreach (string pose in poses)
            {
                specs.Add(TextureSpec.EnemyPose(
                    enemy + "_" + pose,
                    Root + "/Art/Characters/Enemies/" + enemy + "/" + enemy + "_" + pose + ".png",
                    enemy,
                    pose,
                    colors.Primary,
                    colors.Secondary));
            }

            if (enemy.Contains("leader") || enemy.Contains("boss") || enemy.Contains("sorcerer") || enemy.Contains("assassin"))
            {
                specs.Add(TextureSpec.Portrait(
                    enemy + "_portrait",
                    Root + "/Art/Portraits/Enemies/" + enemy + "_portrait.png",
                    "Enemy boss portrait placeholder",
                    768,
                    768,
                    colors.Primary,
                    colors.Secondary));
            }
        }
    }

    private static void AddEmblemSpecs(List<TextureSpec> specs)
    {
        string[] emblems =
        {
            "emblem_baekdu_light_sword",
            "emblem_seorak_spear",
            "emblem_hwawang_blade",
            "emblem_cheonroe_staff",
            "emblem_hwajeop_fan",
            "emblem_heukryeon_shadow",
            "emblem_joseon_sects",
            "emblem_zhongyuan_alliance",
            "emblem_iron_wolf_gate",
            "emblem_black_hat_guild",
            "emblem_moyong_clan",
            "emblem_sobaek_village"
        };

        foreach (string id in emblems)
        {
            ColorPair colors = PaletteFor(id);
            specs.Add(TextureSpec.Emblem(
                id,
                Root + "/Art/UI/Emblems/" + id + ".png",
                "Faction emblem placeholder",
                colors.Primary,
                colors.Secondary));
        }
    }

    private static void AddDialogueBackgroundSpecs(List<TextureSpec> specs)
    {
        string[] backgrounds =
        {
            "bg_baekdu_light_sword_gate_day",
            "bg_baekdu_training_yard_snow",
            "bg_baekdu_rooftop_day",
            "bg_sect_master_room_sickbed",
            "bg_sobaek_village_street_day",
            "bg_chohui_apothecary_shop",
            "bg_baekdu_broken_flag_night",
            "bg_wood_chopping_yard",
            "bg_mountain_herb_path",
            "bg_leaking_roof_repair_scene",
            "bg_seorak_spear_hall_frost",
            "bg_hwawang_blade_training_ground",
            "bg_cheonroe_staff_dojo_gyeongseong",
            "bg_hwajeop_flower_fan_courtyard",
            "bg_heukryeon_shadow_cliff_temple"
        };

        foreach (string id in backgrounds)
        {
            ColorPair colors = PaletteFor(id);
            specs.Add(TextureSpec.DialogueBackground(
                id,
                Root + "/Art/Backgrounds/Dialogue/" + id + ".png",
                "Dialogue background placeholder",
                colors.Primary,
                colors.Secondary));
        }
    }

    private static void WriteTexture(TextureSpec spec, Dictionary<string, VerificationRecord> verification, bool forceOverwrite)
    {
        string absolutePath = AssetPathToAbsolutePath(spec.Path);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        if (File.Exists(absolutePath) && !forceOverwrite)
        {
            RecordExistingTexture(spec, absolutePath, verification);
            return;
        }

        if (File.Exists(absolutePath))
        {
            BackupExistingAsset(spec.Path);
        }

        Texture2D texture = new Texture2D(spec.Width, spec.Height, TextureFormat.RGBA32, false);
        Color32[] pixels = new Color32[spec.Width * spec.Height];
        Color32 clear = new Color32(0, 0, 0, 0);
        Color32 baseColor = spec.Transparent ? clear : WithAlpha(spec.Secondary, 255);

        for (int i = 0; i < pixels.Length; i++)
        {
            pixels[i] = baseColor;
        }

        if (spec.Kind == AssetKind.DialogueBackground)
        {
            DrawDialogueBackground(pixels, spec);
        }
        else if (spec.Kind == AssetKind.EnemyPose)
        {
            DrawEnemyPose(pixels, spec);
        }
        else if (spec.Kind == AssetKind.Portrait)
        {
            DrawPortrait(pixels, spec);
        }
        else if (spec.Kind == AssetKind.Ui)
        {
            DrawUiSprite(pixels, spec);
        }
        else if (spec.Kind == AssetKind.Icon)
        {
            DrawIcon(pixels, spec);
        }
        else if (spec.Kind == AssetKind.Emblem)
        {
            DrawEmblem(pixels, spec);
        }
        else
        {
            DrawVfx(pixels, spec);
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        int transparentPixelCount = pixels.Count(pixel => pixel.a < 8);
        verification[spec.Path] = new VerificationRecord(spec.Path, spec.Width, spec.Height, transparentPixelCount, pixels.Length);
    }

    private static void RecordExistingTexture(TextureSpec spec, string absolutePath, Dictionary<string, VerificationRecord> verification)
    {
        Texture2D existing = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        try
        {
            byte[] bytes = File.ReadAllBytes(absolutePath);
            if (existing.LoadImage(bytes))
            {
                Color32[] pixels = existing.GetPixels32();
                int transparentPixelCount = pixels.Count(pixel => pixel.a < 8);
                verification[spec.Path] = new VerificationRecord(spec.Path, existing.width, existing.height, transparentPixelCount, pixels.Length);
                return;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[NoMapAssetChecklistBuilder] Could not inspect existing texture " + spec.Path + ": " + exception.Message);
        }
        finally
        {
            Object.DestroyImmediate(existing);
        }

        verification[spec.Path] = new VerificationRecord(spec.Path, spec.Width, spec.Height, 0, spec.Width * spec.Height);
    }

    private static void BackupExistingAsset(string assetPath)
    {
        string sourceAbsolutePath = AssetPathToAbsolutePath(assetPath);
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string backupRoot = Path.Combine(projectRoot, "Library", "CodexBackups", "NoMapPlaceholder",
            DateTime.UtcNow.ToString("yyyyMMddTHHmmssZ", CultureInfo.InvariantCulture));
        string targetAbsolutePath = Path.Combine(backupRoot, assetPath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(targetAbsolutePath));
        File.Copy(sourceAbsolutePath, targetAbsolutePath, true);
    }

    private static void DrawDialogueBackground(Color32[] pixels, TextureSpec spec)
    {
        int width = spec.Width;
        int height = spec.Height;
        Color32 sky = Lerp(spec.Secondary, new Color32(232, 238, 229, 255), 0.45f);
        Color32 ground = Lerp(spec.Primary, new Color32(77, 88, 74, 255), 0.35f);
        Color32 mist = new Color32(240, 244, 236, 255);

        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color32 row = Lerp(ground, sky, Mathf.SmoothStep(0f, 1f, t));
            for (int x = 0; x < width; x++)
            {
                float noise = Hash01(spec.Id + x / 80 + ":" + y / 80) * 0.08f;
                Color32 final = Lerp(row, mist, noise);
                SetPixel(pixels, width, height, x, y, final);
            }
        }

        int horizon = Mathf.RoundToInt(height * 0.46f);
        for (int i = 0; i < 5; i++)
        {
            int cx = Mathf.RoundToInt(width * (0.12f + i * 0.21f));
            int cy = horizon + Mathf.RoundToInt(Mathf.Sin(i * 1.7f + Hash01(spec.Id) * 4f) * height * 0.06f);
            int radiusX = Mathf.RoundToInt(width * (0.18f + Hash01(spec.Id + i) * 0.08f));
            int radiusY = Mathf.RoundToInt(height * (0.16f + Hash01(spec.Id + ":h" + i) * 0.08f));
            DrawFilledEllipse(pixels, width, height, cx, cy, radiusX, radiusY, new Color32(71, 87, 80, 120));
        }

        DrawFilledRect(pixels, width, height, 0, 0, width, Mathf.RoundToInt(height * 0.26f), new Color32(78, 84, 69, 130));
        DrawPolyline(pixels, width, height, new[]
        {
            new Vector2(width * 0.07f, height * 0.20f),
            new Vector2(width * 0.24f, height * 0.27f),
            new Vector2(width * 0.48f, height * 0.23f),
            new Vector2(width * 0.74f, height * 0.30f),
            new Vector2(width * 0.95f, height * 0.22f)
        }, new Color32(236, 228, 188, 150), Mathf.RoundToInt(height * 0.018f));

        if (spec.Id.Contains("snow") || spec.Id.Contains("frost") || spec.Id.Contains("baekdu"))
        {
            for (int i = 0; i < 180; i++)
            {
                int x = Mathf.RoundToInt(Hash01(spec.Id + ":snowx" + i) * (width - 1));
                int y = Mathf.RoundToInt(Hash01(spec.Id + ":snowy" + i) * (height - 1));
                int r = 1 + Mathf.RoundToInt(Hash01(spec.Id + ":snowr" + i) * 3f);
                DrawFilledCircle(pixels, width, height, x, y, r, new Color32(250, 252, 255, 150));
            }
        }

        DrawVignette(pixels, width, height, new Color32(23, 28, 35, 90));
    }

    private static void DrawEnemyPose(Color32[] pixels, TextureSpec spec)
    {
        int width = spec.Width;
        int height = spec.Height;
        int groundY = Mathf.RoundToInt(height * 0.12f);
        int cx = width / 2;
        int bodyY = Mathf.RoundToInt(height * 0.47f);
        int lean = spec.Pose == "hit" ? -20 : spec.Pose == "attack" || spec.Pose == "skill" ? 26 : 0;
        int step = spec.Pose == "move" ? 34 : 18;

        Color32 shadow = new Color32(10, 13, 18, 90);
        Color32 body = WithAlpha(spec.Primary, 235);
        Color32 trim = WithAlpha(spec.Secondary, 220);
        Color32 dark = Lerp(spec.Primary, new Color32(21, 24, 31, 255), 0.45f);

        DrawFilledEllipse(pixels, width, height, cx, groundY, 128, 30, shadow);

        if (spec.Pose == "defeated")
        {
            DrawCapsule(pixels, width, height, cx - 60, groundY + 52, cx + 86, groundY + 86, 26, body);
            DrawFilledCircle(pixels, width, height, cx + 112, groundY + 86, 36, trim);
            DrawLine(pixels, width, height, cx - 80, groundY + 90, cx + 140, groundY + 42, dark, 10);
            return;
        }

        DrawLine(pixels, width, height, cx - 28, groundY + 34, cx - step, groundY, dark, 18);
        DrawLine(pixels, width, height, cx + 24, groundY + 36, cx + step, groundY, dark, 18);
        DrawFilledEllipse(pixels, width, height, cx + lean / 3, bodyY, 70, 130, body);
        DrawFilledEllipse(pixels, width, height, cx + lean / 3, bodyY + 78, 58, 48, trim);
        DrawFilledCircle(pixels, width, height, cx + lean / 2, bodyY + 160, 52, trim);
        DrawFilledRect(pixels, width, height, cx - 54 + lean / 3, bodyY + 82, 108, 18, dark);

        int leftArmX = cx - 66 + lean / 2;
        int rightArmX = cx + 70 + lean / 2;
        int armY = bodyY + 78;
        if (spec.Pose == "attack" || spec.Pose == "skill")
        {
            DrawLine(pixels, width, height, leftArmX, armY, leftArmX - 72, armY + 54, body, 16);
            DrawLine(pixels, width, height, rightArmX, armY, rightArmX + 114, armY + 88, body, 16);
            DrawLine(pixels, width, height, rightArmX + 42, armY + 44, rightArmX + 178, armY + 144, trim, 10);
            DrawFilledCircle(pixels, width, height, rightArmX + 190, armY + 154, 13, trim);
        }
        else
        {
            DrawLine(pixels, width, height, leftArmX, armY, leftArmX - 66, armY - 64, body, 16);
            DrawLine(pixels, width, height, rightArmX, armY, rightArmX + 58, armY - 68, body, 16);
        }

        if (spec.Id.Contains("archer") || spec.Id.Contains("shooter"))
        {
            DrawEllipseOutline(pixels, width, height, cx + 104, bodyY + 74, 18, 94, trim, 7);
            DrawLine(pixels, width, height, cx + 99, bodyY - 10, cx + 99, bodyY + 164, trim, 3);
        }
        else if (spec.Id.Contains("spear"))
        {
            DrawLine(pixels, width, height, cx + 110, bodyY - 96, cx + 144, bodyY + 206, trim, 8);
        }
        else if (spec.Id.Contains("banner"))
        {
            DrawLine(pixels, width, height, cx + 105, bodyY - 95, cx + 105, bodyY + 190, trim, 8);
            DrawFilledRect(pixels, width, height, cx + 105, bodyY + 100, 90, 78, WithAlpha(spec.Secondary, 160));
        }
        else
        {
            DrawLine(pixels, width, height, cx + 98, bodyY - 62, cx + 156, bodyY + 106, trim, 9);
        }

        if (spec.Pose == "acted")
        {
            DrawFilledEllipse(pixels, width, height, cx, bodyY + 70, 132, 170, new Color32(70, 70, 78, 96));
        }
    }

    private static void DrawPortrait(Color32[] pixels, TextureSpec spec)
    {
        int width = spec.Width;
        int height = spec.Height;
        int cx = width / 2;
        int bustY = Mathf.RoundToInt(height * 0.27f);
        int faceY = Mathf.RoundToInt(height * 0.56f);
        int scale = Mathf.Min(width, height);

        Color32 shadow = new Color32(19, 22, 27, 70);
        Color32 robe = WithAlpha(spec.Primary, 238);
        Color32 skin = WithAlpha(spec.Secondary, 240);
        Color32 hair = Lerp(spec.Primary, new Color32(21, 22, 28, 255), 0.58f);

        DrawFilledEllipse(pixels, width, height, cx, bustY, Mathf.RoundToInt(scale * 0.30f), Mathf.RoundToInt(scale * 0.10f), shadow);
        DrawFilledEllipse(pixels, width, height, cx, bustY + Mathf.RoundToInt(scale * 0.09f), Mathf.RoundToInt(scale * 0.30f), Mathf.RoundToInt(scale * 0.26f), robe);
        DrawFilledEllipse(pixels, width, height, cx, faceY + Mathf.RoundToInt(scale * 0.07f), Mathf.RoundToInt(scale * 0.16f), Mathf.RoundToInt(scale * 0.19f), skin);
        DrawFilledEllipse(pixels, width, height, cx, faceY + Mathf.RoundToInt(scale * 0.19f), Mathf.RoundToInt(scale * 0.19f), Mathf.RoundToInt(scale * 0.09f), hair);
        DrawFilledRect(pixels, width, height, cx - Mathf.RoundToInt(scale * 0.16f), faceY + Mathf.RoundToInt(scale * 0.12f), Mathf.RoundToInt(scale * 0.32f), Mathf.RoundToInt(scale * 0.07f), hair);

        Color32 accent = Lerp(spec.Primary, spec.Secondary, 0.38f);
        DrawLine(pixels, width, height, cx - Mathf.RoundToInt(scale * 0.21f), bustY + Mathf.RoundToInt(scale * 0.13f), cx + Mathf.RoundToInt(scale * 0.20f), bustY + Mathf.RoundToInt(scale * 0.32f), accent, Mathf.Max(8, scale / 38));
        DrawLine(pixels, width, height, cx + Mathf.RoundToInt(scale * 0.18f), bustY + Mathf.RoundToInt(scale * 0.13f), cx - Mathf.RoundToInt(scale * 0.19f), bustY + Mathf.RoundToInt(scale * 0.30f), new Color32(246, 231, 177, 170), Mathf.Max(6, scale / 52));
    }

    private static void DrawUiSprite(Color32[] pixels, TextureSpec spec)
    {
        int width = spec.Width;
        int height = spec.Height;
        Color32 frame = WithAlpha(spec.Primary, 224);
        Color32 fill = WithAlpha(spec.Secondary, 118);
        Color32 highlight = new Color32(244, 225, 166, 210);

        int pad = Mathf.Max(10, Mathf.RoundToInt(Mathf.Min(width, height) * 0.06f));
        DrawFilledRect(pixels, width, height, pad, pad, width - pad * 2, height - pad * 2, fill);
        DrawRectOutline(pixels, width, height, pad, pad, width - pad * 2, height - pad * 2, frame, Mathf.Max(4, pad / 4));

        if (spec.Id.Contains("bar"))
        {
            DrawFilledRect(pixels, width, height, pad * 2, height / 2 - pad / 2, width - pad * 4, pad, frame);
            DrawFilledRect(pixels, width, height, pad * 2, height / 2 - pad / 2, Mathf.RoundToInt((width - pad * 4) * 0.68f), pad, highlight);
        }
        else if (spec.Id.Contains("checkbox"))
        {
            int box = Mathf.Min(width, height) / 2;
            DrawRectOutline(pixels, width, height, width / 2 - box / 2, height / 2 - box / 2, box, box, frame, Mathf.Max(6, box / 12));
            if (spec.Id.Contains("_on"))
            {
                DrawLine(pixels, width, height, width / 2 - box / 4, height / 2, width / 2 - box / 18, height / 2 - box / 5, highlight, Mathf.Max(6, box / 14));
                DrawLine(pixels, width, height, width / 2 - box / 18, height / 2 - box / 5, width / 2 + box / 3, height / 2 + box / 4, highlight, Mathf.Max(6, box / 14));
            }
        }
        else if (spec.Id.Contains("seal") || spec.Id.Contains("stamp"))
        {
            DrawFilledCircle(pixels, width, height, width / 2, height / 2, Mathf.Min(width, height) / 3, new Color32(164, 36, 42, 185));
            DrawCircleOutline(pixels, width, height, width / 2, height / 2, Mathf.Min(width, height) / 3, new Color32(243, 202, 169, 180), Mathf.Max(5, pad / 3));
        }
        else
        {
            DrawFilledEllipse(pixels, width, height, width / 2, height / 2, Mathf.RoundToInt(width * 0.26f), Mathf.RoundToInt(height * 0.22f), new Color32(highlight.r, highlight.g, highlight.b, 72));
        }
    }

    private static void DrawIcon(Color32[] pixels, TextureSpec spec)
    {
        int width = spec.Width;
        int height = spec.Height;
        int cx = width / 2;
        int cy = height / 2;
        Color32 primary = WithAlpha(spec.Primary, 236);
        Color32 secondary = WithAlpha(spec.Secondary, 228);
        Color32 shadow = new Color32(10, 13, 17, 82);

        DrawFilledCircle(pixels, width, height, cx + 8, cy - 10, 78, shadow);
        if (spec.Id.Contains("weapon") || spec.Id.Contains("attack") || spec.Id.Contains("damage"))
        {
            DrawLine(pixels, width, height, 70, 58, 190, 198, primary, 18);
            DrawLine(pixels, width, height, 96, 46, 208, 160, secondary, 8);
            DrawFilledCircle(pixels, width, height, 68, 56, 18, secondary);
        }
        else if (spec.Id.Contains("status") || spec.Id.Contains("buff") || spec.Id.Contains("debuff"))
        {
            DrawFilledDiamond(pixels, width, height, cx, cy, 82, primary);
            DrawCircleOutline(pixels, width, height, cx, cy, 56, secondary, 10);
        }
        else if (spec.Id.Contains("item") || spec.Id.Contains("scroll") || spec.Id.Contains("poster") || spec.Id.Contains("contract"))
        {
            DrawFilledRect(pixels, width, height, 78, 58, 104, 140, new Color32(232, 214, 166, 225));
            DrawRectOutline(pixels, width, height, 78, 58, 104, 140, primary, 8);
            DrawFilledCircle(pixels, width, height, 132, 58, 18, secondary);
            DrawFilledCircle(pixels, width, height, 132, 198, 18, secondary);
        }
        else if (spec.Id.Contains("hub") || spec.Id.Contains("action"))
        {
            DrawFilledEllipse(pixels, width, height, cx, cy, 86, 66, primary);
            DrawLine(pixels, width, height, cx - 58, cy - 32, cx + 64, cy + 42, secondary, 16);
            DrawFilledCircle(pixels, width, height, cx - 58, cy - 32, 20, secondary);
        }
        else if (spec.Id.Contains("element"))
        {
            DrawFilledCircle(pixels, width, height, cx, cy, 86, primary);
            DrawCircleOutline(pixels, width, height, cx, cy, 64, secondary, 14);
            DrawLine(pixels, width, height, cx - 62, cy, cx + 62, cy, new Color32(255, 255, 255, 145), 10);
            DrawLine(pixels, width, height, cx, cy - 62, cx, cy + 62, new Color32(255, 255, 255, 145), 10);
        }
        else
        {
            DrawFilledDiamond(pixels, width, height, cx, cy, 96, primary);
            DrawFilledCircle(pixels, width, height, cx, cy, 42, secondary);
        }
    }

    private static void DrawEmblem(Color32[] pixels, TextureSpec spec)
    {
        int width = spec.Width;
        int height = spec.Height;
        int cx = width / 2;
        int cy = height / 2;
        Color32 primary = WithAlpha(spec.Primary, 235);
        Color32 secondary = WithAlpha(spec.Secondary, 225);

        DrawFilledDiamond(pixels, width, height, cx, cy, 178, primary);
        DrawCircleOutline(pixels, width, height, cx, cy, 154, secondary, 20);
        DrawLine(pixels, width, height, cx - 98, cy - 98, cx + 98, cy + 98, secondary, 18);
        DrawLine(pixels, width, height, cx + 98, cy - 98, cx - 98, cy + 98, secondary, 18);
        DrawFilledCircle(pixels, width, height, cx, cy, 52, new Color32(250, 235, 178, 220));
    }

    private static void DrawVfx(Color32[] pixels, TextureSpec spec)
    {
        int width = spec.Width;
        int height = spec.Height;
        int cx = width / 2;
        int cy = height / 2;
        Color32 primary = WithAlpha(spec.Primary, 210);
        Color32 secondary = WithAlpha(spec.Secondary, 180);
        Color32 white = new Color32(255, 255, 255, 180);

        DrawFilledEllipse(pixels, width, height, cx, cy, 172, 112, new Color32(primary.r, primary.g, primary.b, 44));
        DrawCircleOutline(pixels, width, height, cx, cy, 158, secondary, 14);

        if (spec.Id.Contains("slash") || spec.Id.Contains("thrust") || spec.Id.Contains("arc"))
        {
            DrawLine(pixels, width, height, 134, 176, 824, 704, primary, 44);
            DrawLine(pixels, width, height, 172, 226, 790, 674, white, 18);
        }
        else if (spec.Id.Contains("projectile") || spec.Id.Contains("needle"))
        {
            DrawLine(pixels, width, height, 122, cy, 870, cy + 22, primary, 30);
            DrawFilledDiamond(pixels, width, height, 840, cy + 22, 58, secondary);
            DrawFilledEllipse(pixels, width, height, 342, cy - 20, 170, 42, new Color32(primary.r, primary.g, primary.b, 74));
        }
        else if (spec.Id.Contains("guard") || spec.Id.Contains("ring") || spec.Id.Contains("circle"))
        {
            DrawCircleOutline(pixels, width, height, cx, cy, 210, primary, 26);
            DrawCircleOutline(pixels, width, height, cx, cy, 128, white, 10);
        }
        else if (spec.Id.Contains("mist") || spec.Id.Contains("smoke") || spec.Id.Contains("shadow"))
        {
            for (int i = 0; i < 9; i++)
            {
                int ox = Mathf.RoundToInt((Hash01(spec.Id + ":mx" + i) - 0.5f) * 360f);
                int oy = Mathf.RoundToInt((Hash01(spec.Id + ":my" + i) - 0.5f) * 210f);
                int rx = 92 + Mathf.RoundToInt(Hash01(spec.Id + ":mr" + i) * 92f);
                int ry = 54 + Mathf.RoundToInt(Hash01(spec.Id + ":ms" + i) * 54f);
                DrawFilledEllipse(pixels, width, height, cx + ox, cy + oy, rx, ry, new Color32(primary.r, primary.g, primary.b, 72));
            }
        }
        else
        {
            for (int i = 0; i < 10; i++)
            {
                float angle = (Mathf.PI * 2f * i / 10f) + Hash01(spec.Id) * 0.7f;
                int x2 = cx + Mathf.RoundToInt(Mathf.Cos(angle) * 272f);
                int y2 = cy + Mathf.RoundToInt(Mathf.Sin(angle) * 272f);
                DrawLine(pixels, width, height, cx, cy, x2, y2, primary, 18);
                DrawFilledCircle(pixels, width, height, x2, y2, 26, secondary);
            }

            DrawFilledCircle(pixels, width, height, cx, cy, 74, white);
        }
    }

    private static void ConfigureSpriteImporter(TextureSpec spec)
    {
        TextureImporter importer = AssetImporter.GetAtPath(spec.Path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogWarning("[NoMapAssetChecklistBuilder] Missing importer for " + spec.Path);
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePixelsPerUnit = spec.PixelsPerUnit;
        ConfigurePlatformSettings(importer, spec, "Standalone");
        ConfigurePlatformSettings(importer, spec, "WebGL");

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = spec.Pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static void ConfigurePlatformSettings(TextureImporter importer, TextureSpec spec, string platformName)
    {
        TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platformName);
        settings.name = platformName;
        settings.overridden = true;
        settings.maxTextureSize = Mathf.NextPowerOfTwo(Mathf.Max(spec.Width, spec.Height));
        settings.format = TextureImporterFormat.RGBA32;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SetPlatformTextureSettings(settings);
    }

    private static Dictionary<string, GameObject> CreateVfxPrefabs(List<TextureSpec> specs)
    {
        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        foreach (TextureSpec spec in specs.Where(s => s.Kind == AssetKind.Vfx))
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.Path);
            if (sprite == null)
            {
                Debug.LogWarning("[NoMapAssetChecklistBuilder] VFX sprite not found: " + spec.Path);
                continue;
            }

            GameObject root = new GameObject(spec.Id);
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = Color.white;
            renderer.sortingOrder = 1200;
            root.transform.localScale = Vector3.one * 0.72f;
            string prefabPath = Root + "/Prefabs/VFX/" + spec.Id + ".prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            spec.PrefabPath = prefabPath;
            prefabs[spec.Id] = prefab;
        }

        return prefabs;
    }

    private static Dictionary<string, EnemyPlaceholderVisualData> CreateEnemyVisualData(List<TextureSpec> specs)
    {
        Dictionary<string, List<TextureSpec>> specsByEnemy = specs
            .Where(s => s.Kind == AssetKind.EnemyPose)
            .GroupBy(s => s.EnemyId, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        Dictionary<string, EnemyPlaceholderVisualData> visuals = new Dictionary<string, EnemyPlaceholderVisualData>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, List<TextureSpec>> pair in specsByEnemy)
        {
            string assetPath = Root + "/ScriptableObjects/Enemies/Visuals/" + pair.Key + "_placeholder_visual.asset";
            EnemyPlaceholderVisualData data = AssetDatabase.LoadAssetAtPath<EnemyPlaceholderVisualData>(assetPath);
            if (data == null)
            {
                data = ScriptableObject.CreateInstance<EnemyPlaceholderVisualData>();
                AssetDatabase.CreateAsset(data, assetPath);
            }

            data.enemyId = pair.Key;
            foreach (TextureSpec spec in pair.Value)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.Path);
                if (sprite == null)
                {
                    continue;
                }

                SetEnemyPoseSprite(data, spec.Pose, sprite);
            }

            EditorUtility.SetDirty(data);
            visuals[pair.Key] = data;
        }

        return visuals;
    }

    private static void SetEnemyPoseSprite(EnemyPlaceholderVisualData data, string pose, Sprite sprite)
    {
        switch (pose)
        {
            case "move":
                data.moveSprite = sprite;
                break;
            case "attack":
                data.attackSprite = sprite;
                break;
            case "skill":
                data.skillSprite = sprite;
                break;
            case "hit":
                data.hitSprite = sprite;
                break;
            case "defeated":
                data.defeatedSprite = sprite;
                break;
            case "acted":
                data.actedSprite = sprite;
                break;
            default:
                data.idleSprite = sprite;
                break;
        }
    }

    private static Dictionary<string, GameObject> CreateEnemyPrefabs(List<TextureSpec> specs, Dictionary<string, EnemyPlaceholderVisualData> enemyVisuals)
    {
        Dictionary<string, TextureSpec> idleSpecs = specs
            .Where(s => s.Kind == AssetKind.EnemyPose && s.Pose == "idle")
            .ToDictionary(s => s.EnemyId, StringComparer.Ordinal);

        Dictionary<string, GameObject> prefabs = new Dictionary<string, GameObject>(StringComparer.Ordinal);
        foreach (KeyValuePair<string, TextureSpec> pair in idleSpecs)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(pair.Value.Path);
            if (sprite == null)
            {
                Debug.LogWarning("[NoMapAssetChecklistBuilder] Enemy idle sprite not found: " + pair.Value.Path);
                continue;
            }

            GameObject root = new GameObject(pair.Key + "_unit");
            SpriteRenderer renderer = root.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = 600;
            EnemyPlaceholderVisualController controller = root.AddComponent<EnemyPlaceholderVisualController>();
            if (enemyVisuals.TryGetValue(pair.Key, out EnemyPlaceholderVisualData visualData))
            {
                controller.VisualData = visualData;
            }

            string prefabPath = Root + "/Prefabs/Units/Enemies/" + pair.Key + "_unit.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            pair.Value.PrefabPath = prefabPath;
            prefabs[pair.Key] = prefab;
        }

        return prefabs;
    }

    private static List<WeaponLinkRecord> ConnectWeaponAnimationSets(Dictionary<string, GameObject> vfxPrefabs)
    {
        List<WeaponLinkRecord> records = new List<WeaponLinkRecord>();

        LinkWeapon(records, vfxPrefabs, "park_sungjun", "park_sungjun_sword_motion_set.asset",
            "park_sungjun_light_sword_slash", "park_sungjun_radiant_cross_slash", "park_sungjun_light_pillar",
            "park_sungjun_light_sword_slash", "park_sungjun_star_flare_impact", "park_sungjun_holy_guard_spark", "common_footstep_dust",
            new[] { "protagonist", "park" });

        LinkWeapon(records, vfxPrefabs, "baek_ryeon", "baek_ryeon_spear_motion_set.asset",
            "baek_ryeon_frost_spear_thrust", "baek_ryeon_ice_wall_burst", "baek_ryeon_frost_spear_thrust",
            "baek_ryeon_ice_spear_slash_arc", "baek_ryeon_ice_crystal_impact", "common_guard_parry_spark", "common_footstep_dust",
            new[] { "baek", "baekryeon" });

        LinkWeapon(records, vfxPrefabs, "do_arin", "do_arin_dao_motion_set.asset",
            "do_arin_fire_dao_slash", "do_arin_meteor_dao_crash", "do_arin_blazing_charge_trail",
            "do_arin_fire_dao_slash", "do_arin_burn_impact", "common_guard_parry_spark", "common_footstep_dust",
            new[] { "arin", "doarin" });

        LinkWeapon(records, vfxPrefabs, "jin_seoyul", "jin_seoyul_staff_motion_set.asset",
            "jin_seoyul_lightning_staff_spin", "jin_seoyul_staff_thunderfall", "jin_seoyul_electric_dash_trail",
            "jin_seoyul_lightning_staff_spin", "jin_seoyul_thunder_impact", "common_guard_parry_spark", "common_footstep_dust",
            new[] { "jin", "jinseoyul" });

        LinkWeapon(records, vfxPrefabs, "seo_a", "shin_seoa_fan_motion_set.asset",
            "seo_a_fan_gust_slash", "seo_a_blossom_field", "seo_a_flower_wind_arc",
            "seo_a_flower_wind_arc", "seo_a_petal_buff_aura", "common_guard_parry_spark", "common_footstep_dust",
            new[] { "shin_seoa" });

        LinkWeapon(records, vfxPrefabs, "han_biyeon", "han_biyeon_dagger_motion_set.asset",
            "han_biyeon_shadow_dagger_slash", "han_biyeon_dark_poison_mist", "han_biyeon_poison_needle_projectile",
            "han_biyeon_shadow_afterimage", "han_biyeon_toxic_impact_splash", "common_guard_parry_spark", "common_footstep_dust",
            new[] { "han", "hanbiyeon" });

        return records;
    }

    private static void LinkWeapon(
        List<WeaponLinkRecord> records,
        Dictionary<string, GameObject> vfxPrefabs,
        string characterId,
        string weaponSetFile,
        string attackId,
        string skillId,
        string projectileId,
        string trailId,
        string impactId,
        string guardId,
        string footstepId,
        string[] aliases)
    {
        string weaponSetPath = Root + "/ScriptableObjects/Weapons/" + weaponSetFile;
        WeaponAnimationSet set = AssetDatabase.LoadAssetAtPath<WeaponAnimationSet>(weaponSetPath);
        if (set == null)
        {
            Debug.LogWarning("[NoMapAssetChecklistBuilder] WeaponAnimationSet not found: " + weaponSetPath);
            return;
        }

        set.attackVfxPrefab = GetPrefab(vfxPrefabs, attackId);
        set.skillVfxPrefab = GetPrefab(vfxPrefabs, skillId);
        set.projectilePrefab = GetPrefab(vfxPrefabs, projectileId);
        set.weaponTrailPrefab = GetPrefab(vfxPrefabs, trailId);
        set.impactVfxPrefab = GetPrefab(vfxPrefabs, impactId);
        set.guardVfxPrefab = GetPrefab(vfxPrefabs, guardId);
        set.footstepVfxPrefab = GetPrefab(vfxPrefabs, footstepId);
        EditorUtility.SetDirty(set);

        records.Add(new WeaponLinkRecord(
            characterId,
            aliases,
            weaponSetPath,
            PrefabPath(set.attackVfxPrefab),
            PrefabPath(set.skillVfxPrefab),
            PrefabPath(set.projectilePrefab),
            PrefabPath(set.weaponTrailPrefab),
            PrefabPath(set.impactVfxPrefab),
            PrefabPath(set.guardVfxPrefab),
            PrefabPath(set.footstepVfxPrefab)));
    }

    private static GameObject GetPrefab(Dictionary<string, GameObject> prefabs, string id)
    {
        return prefabs.TryGetValue(id, out GameObject prefab) ? prefab : null;
    }

    private static string PrefabPath(GameObject prefab)
    {
        return prefab == null ? string.Empty : AssetDatabase.GetAssetPath(prefab);
    }

    private static void WriteGeneratedManifest(List<TextureSpec> specs, Dictionary<string, VerificationRecord> verification)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"schemaVersion\": 1,");
        builder.AppendLine("  \"sourceChecklist\": " + JsonString(SourceChecklistReference) + ",");
        builder.AppendLine("  \"scope\": \"No MAP, no Tilemap, no world-map background assets were generated by this builder.\",");
        builder.AppendLine("  \"generatedUtc\": \"stable\",");
        builder.AppendLine("  \"assets\": [");
        for (int i = 0; i < specs.Count; i++)
        {
            TextureSpec spec = specs[i];
            verification.TryGetValue(spec.Path, out VerificationRecord record);
            builder.AppendLine("    {");
            builder.AppendLine("      \"id\": " + JsonString(spec.Id) + ",");
            builder.AppendLine("      \"category\": " + JsonString(spec.Category) + ",");
            builder.AppendLine("      \"displayName\": " + JsonString(spec.DisplayName) + ",");
            builder.AppendLine("      \"path\": " + JsonString(spec.Path) + ",");
            builder.AppendLine("      \"addressablePath\": " + JsonString(spec.Path) + ",");
            builder.AppendLine("      \"characterId\": " + JsonString(spec.CharacterId) + ",");
            builder.AppendLine("      \"element\": " + JsonString(spec.Element) + ",");
            builder.AppendLine("      \"weaponType\": " + JsonString(spec.WeaponType) + ",");
            builder.AppendLine("      \"skillId\": " + JsonString(spec.SkillId) + ",");
            builder.AppendLine("      \"statusId\": " + JsonString(spec.StatusId) + ",");
            builder.AppendLine("      \"enemyId\": " + JsonString(spec.EnemyId) + ",");
            builder.AppendLine("      \"pose\": " + JsonString(spec.Pose) + ",");
            builder.AppendLine("      \"prefabPath\": " + JsonString(spec.PrefabPath) + ",");
            builder.AppendLine("      \"isPlaceholder\": true,");
            builder.AppendLine("      \"width\": " + spec.Width.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"height\": " + spec.Height.ToString(CultureInfo.InvariantCulture) + ",");
            builder.AppendLine("      \"transparentPixelCount\": " + (record == null ? "0" : record.TransparentPixelCount.ToString(CultureInfo.InvariantCulture)) + ",");
            builder.AppendLine("      \"totalPixelCount\": " + (record == null ? "0" : record.TotalPixelCount.ToString(CultureInfo.InvariantCulture)) + ",");
            builder.AppendLine("      \"aliases\": " + JsonArray(spec.Aliases) + ",");
            builder.AppendLine("      \"importNotes\": " + JsonString(spec.ImportNotes));
            builder.Append("    }");
            if (i < specs.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        WriteTextAsset(GeneratedManifestPath, builder.ToString());
    }

    private static void WriteIconMapping(List<TextureSpec> specs)
    {
        Dictionary<string, string> entries = specs
            .Where(s => s.Kind == AssetKind.Icon || s.Kind == AssetKind.Ui || s.Kind == AssetKind.Emblem)
            .ToDictionary(s => s.Id, s => s.Path, StringComparer.Ordinal);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"schemaVersion\": 1,");
        builder.AppendLine("  \"note\": \"SkillData has no direct icon field yet, so this manifest maps elements, weapons, statuses, tags, command buttons, hub actions, rewards, and UI frames to stable sprite paths.\",");
        builder.AppendLine("  \"aliases\": {");
        builder.AppendLine("    \"seo_a\": [\"shin_seoa\"],");
        builder.AppendLine("    \"park_sungjun\": [\"protagonist\", \"park\"],");
        builder.AppendLine("    \"cho_hui\": [\"chohui\"],");
        builder.AppendLine("    \"do_arin\": [\"arin\", \"doarin\"],");
        builder.AppendLine("    \"baek_ryeon\": [\"baek\", \"baekryeon\"],");
        builder.AppendLine("    \"han_biyeon\": [\"han\", \"hanbiyeon\"],");
        builder.AppendLine("    \"jin_seoyul\": [\"jin\", \"jinseoyul\"]");
        builder.AppendLine("  },");
        builder.AppendLine("  \"elementIcons\": {");
        AppendMapping(builder, "Light", entries, "icon_element_light", true);
        AppendMapping(builder, "Ice", entries, "icon_element_ice_frost", true);
        AppendMapping(builder, "Fire", entries, "icon_element_fire", true);
        AppendMapping(builder, "Lightning", entries, "icon_element_lightning", true);
        AppendMapping(builder, "WindFlower", entries, "icon_element_wind_flower", true);
        AppendMapping(builder, "DarkPoison", entries, "icon_element_dark_poison", false);
        builder.AppendLine("  },");
        builder.AppendLine("  \"weaponIcons\": {");
        AppendMapping(builder, "Sword", entries, "icon_weapon_sword", true);
        AppendMapping(builder, "Spear", entries, "icon_weapon_spear", true);
        AppendMapping(builder, "Dao", entries, "icon_weapon_dao", true);
        AppendMapping(builder, "Staff", entries, "icon_weapon_staff", true);
        AppendMapping(builder, "Fan", entries, "icon_weapon_fan", true);
        AppendMapping(builder, "Dagger", entries, "icon_weapon_dagger", true);
        AppendMapping(builder, "HiddenWeapon", entries, "icon_weapon_hidden_weapon", true);
        AppendMapping(builder, "Bow", entries, "icon_weapon_bow", true);
        AppendMapping(builder, "Fist", entries, "icon_weapon_fist", false);
        builder.AppendLine("  },");
        builder.AppendLine("  \"statusIcons\": {");
        string[] statusIds =
        {
            "guard", "evade", "acted", "defeated", "poison", "burn", "chill", "freeze", "shock", "stun",
            "stealth", "slow", "bind", "blind", "marked", "break", "morale_down", "morale_up",
            "inner_power", "cooldown", "buff_attack", "buff_defense", "buff_move", "buff_hit",
            "buff_evasion", "debuff_attack", "debuff_defense", "debuff_move", "debuff_hit"
        };

        for (int i = 0; i < statusIds.Length; i++)
        {
            AppendMapping(builder, statusIds[i], entries, "icon_status_" + statusIds[i], i < statusIds.Length - 1);
        }
        builder.AppendLine("  },");
        builder.AppendLine("  \"combatIcons\": {");
        string[] combatIds =
        {
            "icon_d20_normal", "icon_d20_critical_20", "icon_d20_fumble_1", "icon_hit_chance",
            "icon_damage", "icon_counter", "icon_follow_up", "icon_push", "icon_terrain_bonus",
            "icon_cover", "icon_height_bonus", "icon_range_min", "icon_range_max"
        };
        for (int i = 0; i < combatIds.Length; i++)
        {
            AppendMapping(builder, combatIds[i], entries, combatIds[i], i < combatIds.Length - 1);
        }
        builder.AppendLine("  },");
        builder.AppendLine("  \"allUiSpritePaths\": {");
        int index = 0;
        foreach (KeyValuePair<string, string> entry in entries.OrderBy(e => e.Key, StringComparer.Ordinal))
        {
            builder.Append("    " + JsonString(entry.Key) + ": " + JsonString(entry.Value));
            if (index < entries.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
            index++;
        }
        builder.AppendLine("  }");
        builder.AppendLine("}");
        WriteTextAsset(IconMappingPath, builder.ToString());
    }

    private static void WriteVfxMapping(List<WeaponLinkRecord> weaponLinks)
    {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"schemaVersion\": 1,");
        builder.AppendLine("  \"note\": \"VFX prefabs are linked into WeaponAnimationSet visual impact slots. Character body SD sprites are not included in this manifest.\",");
        builder.AppendLine("  \"weaponAnimationSets\": [");
        for (int i = 0; i < weaponLinks.Count; i++)
        {
            WeaponLinkRecord link = weaponLinks[i];
            builder.AppendLine("    {");
            builder.AppendLine("      \"characterId\": " + JsonString(link.CharacterId) + ",");
            builder.AppendLine("      \"aliases\": " + JsonArray(link.Aliases) + ",");
            builder.AppendLine("      \"weaponSetPath\": " + JsonString(link.WeaponSetPath) + ",");
            builder.AppendLine("      \"attackVfxPrefab\": " + JsonString(link.AttackVfxPrefab) + ",");
            builder.AppendLine("      \"skillVfxPrefab\": " + JsonString(link.SkillVfxPrefab) + ",");
            builder.AppendLine("      \"projectilePrefab\": " + JsonString(link.ProjectilePrefab) + ",");
            builder.AppendLine("      \"weaponTrailPrefab\": " + JsonString(link.WeaponTrailPrefab) + ",");
            builder.AppendLine("      \"impactVfxPrefab\": " + JsonString(link.ImpactVfxPrefab) + ",");
            builder.AppendLine("      \"guardVfxPrefab\": " + JsonString(link.GuardVfxPrefab) + ",");
            builder.AppendLine("      \"footstepVfxPrefab\": " + JsonString(link.FootstepVfxPrefab));
            builder.Append("    }");
            if (i < weaponLinks.Count - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        WriteTextAsset(VfxMappingPath, builder.ToString());
    }

    private static void WriteAudioCueManifest()
    {
        string[] sfx =
        {
            "sfx_ui_confirm",
            "sfx_ui_cancel",
            "sfx_ui_hover",
            "sfx_ui_error",
            "sfx_turn_start",
            "sfx_unit_select",
            "sfx_unit_move_step",
            "sfx_attack_whoosh",
            "sfx_hit_light",
            "sfx_hit_heavy",
            "sfx_guard_clang",
            "sfx_evade",
            "sfx_defeat",
            "sfx_critical",
            "sfx_miss",
            "sfx_heal",
            "sfx_buff",
            "sfx_debuff",
            "sfx_light_slash",
            "sfx_light_pillar",
            "sfx_ice_spear",
            "sfx_ice_freeze",
            "sfx_fire_dao_slash",
            "sfx_fire_burst",
            "sfx_lightning_staff",
            "sfx_thunder_impact",
            "sfx_flower_wind",
            "sfx_fan_gust",
            "sfx_poison_needle",
            "sfx_shadow_dash"
        };

        string[] bgm =
        {
            "bgm_title_baekdu_light",
            "bgm_hub_pyesadang",
            "bgm_battle_normal",
            "bgm_battle_boss",
            "bgm_battle_result_victory",
            "bgm_battle_result_defeat",
            "bgm_dialogue_calm",
            "bgm_dialogue_tension"
        };

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("{");
        builder.AppendLine("  \"schemaVersion\": 1,");
        builder.AppendLine("  \"note\": \"Audio files are intentionally not generated. This manifest reserves cue ids and target folders for later sound replacement.\",");
        builder.AppendLine("  \"sfx\": [");
        for (int i = 0; i < sfx.Length; i++)
        {
            builder.Append("    { \"id\": " + JsonString(sfx[i]) + ", \"targetFolder\": " + JsonString(Root + "/Audio/SFX") + ", \"assetPending\": true }");
            if (i < sfx.Length - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("  ],");
        builder.AppendLine("  \"bgm\": [");
        for (int i = 0; i < bgm.Length; i++)
        {
            builder.Append("    { \"id\": " + JsonString(bgm[i]) + ", \"targetFolder\": " + JsonString(Root + "/Audio/BGM") + ", \"assetPending\": true }");
            if (i < bgm.Length - 1)
            {
                builder.Append(",");
            }
            builder.AppendLine();
        }
        builder.AppendLine("  ]");
        builder.AppendLine("}");
        WriteTextAsset(AudioCueManifestPath, builder.ToString());
    }

    private static void WriteNoMapDoc(List<TextureSpec> specs, int vfxPrefabCount, int enemyPrefabCount, List<WeaponLinkRecord> weaponLinks)
    {
        int vfxCount = specs.Count(s => s.Kind == AssetKind.Vfx);
        int iconCount = specs.Count(s => s.Kind == AssetKind.Icon);
        int uiCount = specs.Count(s => s.Kind == AssetKind.Ui);
        int npcCount = specs.Count(s => s.Kind == AssetKind.Portrait && s.Path.Contains("/Portraits/NPC/"));
        int enemySpriteCount = specs.Count(s => s.Kind == AssetKind.EnemyPose);
        int enemyPortraitCount = specs.Count(s => s.Kind == AssetKind.Portrait && s.Path.Contains("/Portraits/Enemies/"));
        int emblemCount = specs.Count(s => s.Kind == AssetKind.Emblem);
        int backgroundCount = specs.Count(s => s.Kind == AssetKind.DialogueBackground);

        StringBuilder builder = new StringBuilder();
        builder.AppendLine("# No-Map Asset Checklist Build");
        builder.AppendLine();
        builder.AppendLine("Generated from:");
        builder.AppendLine();
        builder.AppendLine("- `" + SourceChecklistReference + "`");
        builder.AppendLine();
        builder.AppendLine("## Scope");
        builder.AppendLine();
        builder.AppendLine("This pass intentionally excludes MAP, Tilemap ground/height/props/collision art, battle map layout, and world-map backgrounds.");
        builder.AppendLine("Character body SD combat sprites are also treated as a separate workstream. This pass only creates replaceable no-map placeholders and mapping data.");
        builder.AppendLine();
        builder.AppendLine("## Generated Counts");
        builder.AppendLine();
        builder.AppendLine("- VFX sprites: " + vfxCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- VFX prefabs: " + vfxPrefabCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- UI/icon sprites: " + (iconCount + uiCount + emblemCount).ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- NPC portrait sprites: " + npcCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- Enemy pose sprites: " + enemySpriteCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- Enemy portrait sprites: " + enemyPortraitCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- Enemy visual prefabs: " + enemyPrefabCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine("- Dialogue background PNGs: " + backgroundCount.ToString(CultureInfo.InvariantCulture));
        builder.AppendLine();
        builder.AppendLine("## Runtime / Data Links");
        builder.AppendLine();
        builder.AppendLine("- `generated_asset_manifest.json` lists every placeholder with category, path, alias, size, prefab path, and transparency verification counts.");
        builder.AppendLine("- `icon_mapping.json` maps SkillData-adjacent element, weapon, status, combat, hub, reward, and UI ids to stable sprite paths.");
        builder.AppendLine("- `vfx_mapping.json` records the WeaponAnimationSet links written by this pass.");
        builder.AppendLine("- `audio_cue_manifest.json` reserves SFX/BGM cue names without generating fake audio files.");
        builder.AppendLine();
        builder.AppendLine("## WeaponAnimationSet Links");
        builder.AppendLine();
        foreach (WeaponLinkRecord link in weaponLinks)
        {
            builder.AppendLine("- `" + link.CharacterId + "` -> `" + link.WeaponSetPath + "`");
        }
        builder.AppendLine();
        builder.AppendLine("## Alias Rules");
        builder.AppendLine();
        builder.AppendLine("- `seo_a` = `shin_seoa`");
        builder.AppendLine("- `park_sungjun` = `protagonist` = `park`");
        builder.AppendLine("- `cho_hui` = `chohui`");
        builder.AppendLine("- `do_arin` = `arin` = `doarin`");
        builder.AppendLine("- `baek_ryeon` = `baek` = `baekryeon`");
        builder.AppendLine("- `han_biyeon` = `han` = `hanbiyeon`");
        builder.AppendLine("- `jin_seoyul` = `jin` = `jinseoyul`");
        builder.AppendLine();
        builder.AppendLine("## Replacement Rules");
        builder.AppendLine();
        builder.AppendLine("- Replace placeholder PNGs in place with the same filenames whenever possible.");
        builder.AppendLine("- Keep PNG alpha; do not bake UI labels into images. Use TextMeshPro for text.");
        builder.AppendLine("- VFX and character SD body assets stay separate.");
        builder.AppendLine("- Do not add MAP or Tilemap art to this checklist pipeline.");
        WriteTextAsset(DocPath, builder.ToString());
    }

    private static void AppendMapping(StringBuilder builder, string key, Dictionary<string, string> entries, string id, bool trailingComma)
    {
        entries.TryGetValue(id, out string path);
        builder.Append("    " + JsonString(key) + ": " + JsonString(path ?? string.Empty));
        if (trailingComma)
        {
            builder.Append(",");
        }
        builder.AppendLine();
    }

    private static void WriteTextAsset(string assetPath, string text)
    {
        string absolutePath = AssetPathToAbsolutePath(assetPath);
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        File.WriteAllText(absolutePath, text, new UTF8Encoding(false));
    }

    private static void EnsureAssetFolder(string assetFolder)
    {
        string absolute = AssetPathToAbsolutePath(assetFolder);
        Directory.CreateDirectory(absolute);
    }

    private static bool HasCommandLineFlag(string flag)
    {
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (string.Equals(argument, flag, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string AssetPathToAbsolutePath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string normalized = assetPath.Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(projectRoot, normalized);
    }

    private static ColorPair PaletteFor(string id)
    {
        if (id.Contains("ice") || id.Contains("frost") || id.Contains("snow") || id.Contains("baekdu"))
        {
            return new ColorPair(new Color32(122, 204, 240, 255), new Color32(232, 249, 255, 255));
        }

        if (id.Contains("fire") || id.Contains("burn") || id.Contains("hwawang") || id.Contains("victory"))
        {
            return new ColorPair(new Color32(230, 82, 50, 255), new Color32(255, 205, 100, 255));
        }

        if (id.Contains("lightning") || id.Contains("thunder") || id.Contains("shock") || id.Contains("cheonroe"))
        {
            return new ColorPair(new Color32(103, 190, 246, 255), new Color32(255, 242, 106, 255));
        }

        if (id.Contains("poison") || id.Contains("dark") || id.Contains("shadow") || id.Contains("black_hat") || id.Contains("heukryeon"))
        {
            return new ColorPair(new Color32(110, 73, 170, 255), new Color32(95, 214, 94, 255));
        }

        if (id.Contains("flower") || id.Contains("wind") || id.Contains("fan") || id.Contains("hwajeop"))
        {
            return new ColorPair(new Color32(231, 129, 190, 255), new Color32(126, 223, 178, 255));
        }

        if (id.Contains("defeat") || id.Contains("locked") || id.Contains("disabled") || id.Contains("defeated"))
        {
            return new ColorPair(new Color32(80, 84, 92, 255), new Color32(166, 168, 158, 255));
        }

        if (id.Contains("iron_wolf"))
        {
            return new ColorPair(new Color32(105, 119, 125, 255), new Color32(192, 205, 193, 255));
        }

        float h = Hash01(id);
        Color primary = Color.HSVToRGB(h, 0.48f, 0.78f);
        Color secondary = Color.HSVToRGB((h + 0.11f) % 1f, 0.32f, 0.92f);
        return new ColorPair(ToColor32(primary), ToColor32(secondary));
    }

    private static Color32 ToColor32(Color color)
    {
        return new Color32(
            (byte)Mathf.RoundToInt(color.r * 255f),
            (byte)Mathf.RoundToInt(color.g * 255f),
            (byte)Mathf.RoundToInt(color.b * 255f),
            255);
    }

    private static Color32 WithAlpha(Color32 color, byte alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Color32 Lerp(Color32 a, Color32 b, float t)
    {
        t = Mathf.Clamp01(t);
        return new Color32(
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.r, b.r, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.g, b.g, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.b, b.b, t)),
            (byte)Mathf.RoundToInt(Mathf.Lerp(a.a, b.a, t)));
    }

    private static float Hash01(string value)
    {
        unchecked
        {
            uint hash = 2166136261;
            for (int i = 0; i < value.Length; i++)
            {
                hash ^= value[i];
                hash *= 16777619;
            }

            return (hash & 0x00FFFFFF) / 16777215f;
        }
    }

    private static void SetPixel(Color32[] pixels, int width, int height, int x, int y, Color32 color)
    {
        if (x < 0 || y < 0 || x >= width || y >= height)
        {
            return;
        }

        int index = y * width + x;
        pixels[index] = AlphaBlend(pixels[index], color);
    }

    private static Color32 AlphaBlend(Color32 dst, Color32 src)
    {
        float sa = src.a / 255f;
        if (sa <= 0f)
        {
            return dst;
        }

        float da = dst.a / 255f;
        float outA = sa + da * (1f - sa);
        if (outA <= 0f)
        {
            return new Color32(0, 0, 0, 0);
        }

        float r = (src.r * sa + dst.r * da * (1f - sa)) / outA;
        float g = (src.g * sa + dst.g * da * (1f - sa)) / outA;
        float b = (src.b * sa + dst.b * da * (1f - sa)) / outA;
        return new Color32(
            (byte)Mathf.Clamp(Mathf.RoundToInt(r), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(g), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(b), 0, 255),
            (byte)Mathf.Clamp(Mathf.RoundToInt(outA * 255f), 0, 255));
    }

    private static void DrawFilledRect(Color32[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color32 color)
    {
        int xMin = Mathf.Max(0, x);
        int yMin = Mathf.Max(0, y);
        int xMax = Mathf.Min(width - 1, x + rectWidth);
        int yMax = Mathf.Min(height - 1, y + rectHeight);
        for (int py = yMin; py <= yMax; py++)
        {
            for (int px = xMin; px <= xMax; px++)
            {
                SetPixel(pixels, width, height, px, py, color);
            }
        }
    }

    private static void DrawRectOutline(Color32[] pixels, int width, int height, int x, int y, int rectWidth, int rectHeight, Color32 color, int thickness)
    {
        DrawFilledRect(pixels, width, height, x, y, rectWidth, thickness, color);
        DrawFilledRect(pixels, width, height, x, y + rectHeight - thickness, rectWidth, thickness, color);
        DrawFilledRect(pixels, width, height, x, y, thickness, rectHeight, color);
        DrawFilledRect(pixels, width, height, x + rectWidth - thickness, y, thickness, rectHeight, color);
    }

    private static void DrawFilledCircle(Color32[] pixels, int width, int height, int cx, int cy, int radius, Color32 color)
    {
        DrawFilledEllipse(pixels, width, height, cx, cy, radius, radius, color);
    }

    private static void DrawFilledEllipse(Color32[] pixels, int width, int height, int cx, int cy, int radiusX, int radiusY, Color32 color)
    {
        radiusX = Mathf.Max(1, radiusX);
        radiusY = Mathf.Max(1, radiusY);
        int xMin = Mathf.Max(0, cx - radiusX);
        int xMax = Mathf.Min(width - 1, cx + radiusX);
        int yMin = Mathf.Max(0, cy - radiusY);
        int yMax = Mathf.Min(height - 1, cy + radiusY);
        float rx2 = radiusX * radiusX;
        float ry2 = radiusY * radiusY;

        for (int y = yMin; y <= yMax; y++)
        {
            float dy = y - cy;
            for (int x = xMin; x <= xMax; x++)
            {
                float dx = x - cx;
                if ((dx * dx) / rx2 + (dy * dy) / ry2 <= 1f)
                {
                    SetPixel(pixels, width, height, x, y, color);
                }
            }
        }
    }

    private static void DrawCircleOutline(Color32[] pixels, int width, int height, int cx, int cy, int radius, Color32 color, int thickness)
    {
        DrawEllipseOutline(pixels, width, height, cx, cy, radius, radius, color, thickness);
    }

    private static void DrawEllipseOutline(Color32[] pixels, int width, int height, int cx, int cy, int radiusX, int radiusY, Color32 color, int thickness)
    {
        radiusX = Mathf.Max(1, radiusX);
        radiusY = Mathf.Max(1, radiusY);
        float outerRx2 = radiusX * radiusX;
        float outerRy2 = radiusY * radiusY;
        float innerRx = Mathf.Max(1, radiusX - thickness);
        float innerRy = Mathf.Max(1, radiusY - thickness);
        float innerRx2 = innerRx * innerRx;
        float innerRy2 = innerRy * innerRy;

        int xMin = Mathf.Max(0, cx - radiusX);
        int xMax = Mathf.Min(width - 1, cx + radiusX);
        int yMin = Mathf.Max(0, cy - radiusY);
        int yMax = Mathf.Min(height - 1, cy + radiusY);
        for (int y = yMin; y <= yMax; y++)
        {
            float dy = y - cy;
            for (int x = xMin; x <= xMax; x++)
            {
                float dx = x - cx;
                float outer = (dx * dx) / outerRx2 + (dy * dy) / outerRy2;
                float inner = (dx * dx) / innerRx2 + (dy * dy) / innerRy2;
                if (outer <= 1f && inner >= 1f)
                {
                    SetPixel(pixels, width, height, x, y, color);
                }
            }
        }
    }

    private static void DrawFilledDiamond(Color32[] pixels, int width, int height, int cx, int cy, int radius, Color32 color)
    {
        int xMin = Mathf.Max(0, cx - radius);
        int xMax = Mathf.Min(width - 1, cx + radius);
        int yMin = Mathf.Max(0, cy - radius);
        int yMax = Mathf.Min(height - 1, cy + radius);
        for (int y = yMin; y <= yMax; y++)
        {
            for (int x = xMin; x <= xMax; x++)
            {
                if (Mathf.Abs(x - cx) + Mathf.Abs(y - cy) <= radius)
                {
                    SetPixel(pixels, width, height, x, y, color);
                }
            }
        }
    }

    private static void DrawLine(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, Color32 color, int thickness)
    {
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1;
        int sy = y0 < y1 ? 1 : -1;
        int err = dx - dy;
        int radius = Mathf.Max(1, thickness / 2);

        while (true)
        {
            DrawFilledCircle(pixels, width, height, x0, y0, radius, color);
            if (x0 == x1 && y0 == y1)
            {
                break;
            }

            int e2 = 2 * err;
            if (e2 > -dy)
            {
                err -= dy;
                x0 += sx;
            }

            if (e2 < dx)
            {
                err += dx;
                y0 += sy;
            }
        }
    }

    private static void DrawPolyline(Color32[] pixels, int width, int height, Vector2[] points, Color32 color, int thickness)
    {
        for (int i = 0; i < points.Length - 1; i++)
        {
            DrawLine(
                pixels,
                width,
                height,
                Mathf.RoundToInt(points[i].x),
                Mathf.RoundToInt(points[i].y),
                Mathf.RoundToInt(points[i + 1].x),
                Mathf.RoundToInt(points[i + 1].y),
                color,
                thickness);
        }
    }

    private static void DrawCapsule(Color32[] pixels, int width, int height, int x0, int y0, int x1, int y1, int radius, Color32 color)
    {
        DrawLine(pixels, width, height, x0, y0, x1, y1, color, radius * 2);
        DrawFilledCircle(pixels, width, height, x0, y0, radius, color);
        DrawFilledCircle(pixels, width, height, x1, y1, radius, color);
    }

    private static void DrawVignette(Color32[] pixels, int width, int height, Color32 color)
    {
        Vector2 center = new Vector2(width * 0.5f, height * 0.5f);
        float maxDistance = center.magnitude;
        for (int y = 0; y < height; y += 2)
        {
            for (int x = 0; x < width; x += 2)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / maxDistance;
                if (distance < 0.60f)
                {
                    continue;
                }

                Color32 vignette = color;
                vignette.a = (byte)Mathf.RoundToInt(color.a * Mathf.InverseLerp(0.60f, 1f, distance));
                SetPixel(pixels, width, height, x, y, vignette);
                SetPixel(pixels, width, height, x + 1, y, vignette);
                SetPixel(pixels, width, height, x, y + 1, vignette);
                SetPixel(pixels, width, height, x + 1, y + 1, vignette);
            }
        }
    }

    private static string JsonString(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        return "\"" + value.Replace("\\", "\\\\").Replace("\"", "\\\"") + "\"";
    }

    private static string JsonArray(IEnumerable<string> values)
    {
        return "[" + string.Join(", ", values.Select(JsonString)) + "]";
    }

    private enum AssetKind
    {
        Vfx,
        Icon,
        Ui,
        Portrait,
        EnemyPose,
        Emblem,
        DialogueBackground
    }

    private sealed class TextureSpec
    {
        public string Id;
        public string Category;
        public string DisplayName;
        public string Path;
        public string CharacterId = string.Empty;
        public string Element = string.Empty;
        public string WeaponType = string.Empty;
        public string SkillId = string.Empty;
        public string StatusId = string.Empty;
        public string EnemyId = string.Empty;
        public string Pose = string.Empty;
        public string PrefabPath = string.Empty;
        public string ImportNotes;
        public string[] Aliases = Array.Empty<string>();
        public int Width;
        public int Height;
        public bool Transparent;
        public float PixelsPerUnit;
        public Vector2 Pivot;
        public Color32 Primary;
        public Color32 Secondary;
        public AssetKind Kind;

        public static TextureSpec Vfx(string id, string path, string displayName, string element, string weapon, string characterId, Color32 primary, Color32 secondary, string[] aliases)
        {
            return new TextureSpec
            {
                Id = id,
                Category = "VFX",
                DisplayName = displayName,
                Path = path,
                CharacterId = characterId,
                Element = element,
                WeaponType = weapon,
                SkillId = id,
                Width = 1024,
                Height = 1024,
                Transparent = true,
                PixelsPerUnit = 128f,
                Pivot = CenterPivot,
                Primary = primary,
                Secondary = secondary,
                Aliases = aliases ?? Array.Empty<string>(),
                Kind = AssetKind.Vfx,
                ImportNotes = "Sprite Single, Full Rect, Compression None, Bilinear, center pivot."
            };
        }

        public static TextureSpec Icon(string id, string path, string displayName, Color32 primary, Color32 secondary)
        {
            return new TextureSpec
            {
                Id = id,
                Category = "UI/Icon",
                DisplayName = displayName,
                Path = path,
                StatusId = id.StartsWith("icon_status_", StringComparison.Ordinal) ? id.Substring("icon_status_".Length) : string.Empty,
                Width = 256,
                Height = 256,
                Transparent = true,
                PixelsPerUnit = 100f,
                Pivot = CenterPivot,
                Primary = primary,
                Secondary = secondary,
                Kind = AssetKind.Icon,
                ImportNotes = "Sprite Single, Full Rect, Compression None, Bilinear, center pivot. No baked text."
            };
        }

        public static TextureSpec Ui(string id, string path, string displayName, int width, int height, Color32 primary, Color32 secondary)
        {
            return new TextureSpec
            {
                Id = id,
                Category = "UI",
                DisplayName = displayName,
                Path = path,
                Width = width,
                Height = height,
                Transparent = true,
                PixelsPerUnit = 100f,
                Pivot = CenterPivot,
                Primary = primary,
                Secondary = secondary,
                Kind = AssetKind.Ui,
                ImportNotes = "Sprite Single, Full Rect, Compression None, Bilinear, center pivot. Text must be TextMeshPro."
            };
        }

        public static TextureSpec Portrait(string id, string path, string displayName, int width, int height, Color32 primary, Color32 secondary)
        {
            return new TextureSpec
            {
                Id = id,
                Category = path.Contains("/Enemies/") ? "Enemy/Portrait" : "NPC/Portrait",
                DisplayName = displayName,
                Path = path,
                Width = width,
                Height = height,
                Transparent = true,
                PixelsPerUnit = 100f,
                Pivot = CenterPivot,
                Primary = primary,
                Secondary = secondary,
                Kind = AssetKind.Portrait,
                ImportNotes = "Sprite Single, Full Rect, Compression None, Bilinear, center pivot. No text."
            };
        }

        public static TextureSpec EnemyPose(string id, string path, string enemyId, string pose, Color32 primary, Color32 secondary)
        {
            return new TextureSpec
            {
                Id = id,
                Category = "Enemy/Pose",
                DisplayName = enemyId + " " + pose + " pose placeholder",
                Path = path,
                EnemyId = enemyId,
                Pose = pose,
                Width = 768,
                Height = 768,
                Transparent = true,
                PixelsPerUnit = 384f,
                Pivot = BottomCenterPivot,
                Primary = primary,
                Secondary = secondary,
                Kind = AssetKind.EnemyPose,
                ImportNotes = "Sprite Single, Full Rect, Compression None, Bilinear, bottom-center pivot."
            };
        }

        public static TextureSpec Emblem(string id, string path, string displayName, Color32 primary, Color32 secondary)
        {
            return new TextureSpec
            {
                Id = id,
                Category = "UI/Emblem",
                DisplayName = displayName,
                Path = path,
                Width = 512,
                Height = 512,
                Transparent = true,
                PixelsPerUnit = 100f,
                Pivot = CenterPivot,
                Primary = primary,
                Secondary = secondary,
                Kind = AssetKind.Emblem,
                ImportNotes = "Sprite Single, Full Rect, Compression None, Bilinear, center pivot. No baked letters."
            };
        }

        public static TextureSpec DialogueBackground(string id, string path, string displayName, Color32 primary, Color32 secondary)
        {
            return new TextureSpec
            {
                Id = id,
                Category = "Dialogue/Background",
                DisplayName = displayName,
                Path = path,
                Width = 1920,
                Height = 1080,
                Transparent = false,
                PixelsPerUnit = 100f,
                Pivot = CenterPivot,
                Primary = primary,
                Secondary = secondary,
                Kind = AssetKind.DialogueBackground,
                ImportNotes = "Sprite Single, Full Rect, Compression None, Bilinear, center pivot. RGBA PNG, opaque alpha allowed for VN backgrounds."
            };
        }
    }

    private sealed class CharacterStyle
    {
        public readonly string Id;
        public readonly string FolderName;
        public readonly string Element;
        public readonly string Weapon;
        public readonly Color32 Primary;
        public readonly Color32 Secondary;
        public readonly string[] Aliases;

        public CharacterStyle(string id, string folderName, string element, string weapon, Color32 primary, Color32 secondary, string[] aliases)
        {
            Id = id;
            FolderName = folderName;
            Element = element;
            Weapon = weapon;
            Primary = primary;
            Secondary = secondary;
            Aliases = aliases;
        }
    }

    private sealed class NpcStyle
    {
        public readonly string Id;
        public readonly Color32 Primary;
        public readonly Color32 Secondary;
        public readonly string[] Expressions;

        public NpcStyle(string id, Color32 primary, Color32 secondary, string[] expressions)
        {
            Id = id;
            Primary = primary;
            Secondary = secondary;
            Expressions = expressions;
        }
    }

    private sealed class ColorPair
    {
        public readonly Color32 Primary;
        public readonly Color32 Secondary;

        public ColorPair(Color32 primary, Color32 secondary)
        {
            Primary = primary;
            Secondary = secondary;
        }
    }

    private sealed class VerificationRecord
    {
        public readonly string Path;
        public readonly int Width;
        public readonly int Height;
        public readonly int TransparentPixelCount;
        public readonly int TotalPixelCount;

        public VerificationRecord(string path, int width, int height, int transparentPixelCount, int totalPixelCount)
        {
            Path = path;
            Width = width;
            Height = height;
            TransparentPixelCount = transparentPixelCount;
            TotalPixelCount = totalPixelCount;
        }
    }

    private sealed class WeaponLinkRecord
    {
        public readonly string CharacterId;
        public readonly string[] Aliases;
        public readonly string WeaponSetPath;
        public readonly string AttackVfxPrefab;
        public readonly string SkillVfxPrefab;
        public readonly string ProjectilePrefab;
        public readonly string WeaponTrailPrefab;
        public readonly string ImpactVfxPrefab;
        public readonly string GuardVfxPrefab;
        public readonly string FootstepVfxPrefab;

        public WeaponLinkRecord(
            string characterId,
            string[] aliases,
            string weaponSetPath,
            string attackVfxPrefab,
            string skillVfxPrefab,
            string projectilePrefab,
            string weaponTrailPrefab,
            string impactVfxPrefab,
            string guardVfxPrefab,
            string footstepVfxPrefab)
        {
            CharacterId = characterId;
            Aliases = aliases ?? Array.Empty<string>();
            WeaponSetPath = weaponSetPath;
            AttackVfxPrefab = attackVfxPrefab;
            SkillVfxPrefab = skillVfxPrefab;
            ProjectilePrefab = projectilePrefab;
            WeaponTrailPrefab = weaponTrailPrefab;
            ImpactVfxPrefab = impactVfxPrefab;
            GuardVfxPrefab = guardVfxPrefab;
            FootstepVfxPrefab = footstepVfxPrefab;
        }
    }
}
}
