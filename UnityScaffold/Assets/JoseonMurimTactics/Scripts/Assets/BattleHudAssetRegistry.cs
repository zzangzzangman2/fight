using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public static class BattleHudAssetRegistry
{
    public const string ResourceRoot = "UI/BattleHUD/";

    public static readonly string[] RequiredRuntimeAssetIds =
    {
        "ui_panel_dark_ink",
        "ui_panel_gold_frame",
        "ui_scroll_frame",
        "ui_battle_command_panel",
        "ui_battle_forecast_panel",
        "ui_unit_status_card",
        "ui_turn_order_card",
        "ui_button_normal",
        "ui_button_hover",
        "ui_button_pressed",
        "ui_button_disabled",
        "ui_btn_move",
        "ui_btn_attack",
        "ui_btn_skill",
        "ui_btn_guard",
        "ui_btn_terrain_action",
        "ui_btn_wait",
        "SkillIcons/ui_command_active_ring",
        "SkillIcons/ui_command_disabled_mask",
        "SkillIcons/ui_action_move",
        "SkillIcons/ui_action_attack",
        "SkillIcons/ui_action_guard",
        "SkillIcons/ui_action_terrain",
        "SkillIcons/ui_action_wait",
        "SkillIcons/skill_default_wuxia",
        "SkillIcons/skill_park_sungjun_baekdu_light_sword",
        "SkillIcons/skill_baek_ryeon_snow_spear",
        "SkillIcons/skill_do_arin_fire_dao",
        "SkillIcons/skill_jin_seoyul_thunder_staff",
        "SkillIcons/skill_shin_seoa_flower_wind_fan",
        "SkillIcons/skill_han_biyeon_shadow_poison_needle",
        "ui_tooltip_frame",
        "ui_toast_frame",
        "ui_hp_bar_bg",
        "ui_hp_bar_fill",
        "ui_inner_bar_bg",
        "ui_inner_bar_fill",
        "ui_morale_bar_bg",
        "ui_morale_bar_fill",
        "ui_red_seal_stamp",
        "ui_ink_brush_corner"
    };

    private static readonly Dictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "ui_battle_panel_phase_ribbon_9slice", "ui_panel_gold_frame" },
            { "ui_battle_panel_ink_glass_9slice", "ui_panel_dark_ink" },
            { "ui_battle_panel_log_9slice", "ui_scroll_frame" },
            { "ui_battle_panel_forecast_9slice", "ui_battle_forecast_panel" },
            { "ui_battle_button_normal_9slice", "ui_button_normal" },
            { "ui_button_primary", "ui_button_normal" },
            { "ui_button_secondary", "ui_button_hover" },
            { "ui_neigong_bar_fill", "ui_inner_bar_fill" },
            { "ui_action_bar_fill", "ui_morale_bar_fill" }
        };

    private static readonly Dictionary<string, Sprite> SpriteCache =
        new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, Texture2D> TextureCache =
        new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MissingWarnings =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static string ResolveId(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return string.Empty;
        }

        return Aliases.TryGetValue(id, out string mapped) ? mapped : id;
    }

    public static string ResolveResourcePath(string id)
    {
        string resolved = ResolveId(id);
        return string.IsNullOrEmpty(resolved) ? string.Empty : ResourceRoot + resolved;
    }

    public static Sprite LoadSprite(string id)
    {
        string resourcePath = ResolveResourcePath(id);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        if (SpriteCache.TryGetValue(resourcePath, out Sprite cached))
        {
            return cached;
        }

        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null && MissingWarnings.Add(resourcePath))
        {
            Debug.LogWarning("[BattleHudAssetRegistry] Missing Battle HUD sprite: " + resourcePath);
        }

        SpriteCache[resourcePath] = sprite;
        return sprite;
    }

    public static Texture2D LoadTexture(string id)
    {
        string resourcePath = ResolveResourcePath(id);
        if (string.IsNullOrEmpty(resourcePath))
        {
            return null;
        }

        if (TextureCache.TryGetValue(resourcePath, out Texture2D cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Sprite sprite = LoadSprite(id);
            texture = sprite != null ? sprite.texture : null;
        }

        TextureCache[resourcePath] = texture;
        return texture;
    }
}
}
