using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public static class DialogueBackgroundRegistry
{
    public const string DefaultBackgroundId = "bg_baekdu_light_sword_gate_day";

    private static readonly Dictionary<string, string> ResourcePaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "bg_baekdu_light_sword_gate_day", "Backgrounds/Dialogue/bg_baekdu_light_sword_gate_day" },
            { "bg_baekdu_training_yard_snow", "Backgrounds/Dialogue/bg_baekdu_training_yard_snow" },
            { "bg_baekdu_rooftop_day", "Backgrounds/Dialogue/bg_baekdu_rooftop_day" },
            { "bg_sect_master_room_sickbed", "Backgrounds/Dialogue/bg_sect_master_room_sickbed" },
            { "bg_sobaek_village_street_day", "Backgrounds/Dialogue/bg_sobaek_village_street_day" },
            { "bg_chohui_apothecary_shop", "Backgrounds/Dialogue/bg_chohui_apothecary_shop" },
            { "bg_baekdu_broken_flag_night", "Backgrounds/Dialogue/bg_baekdu_broken_flag_night" },
            { "bg_wood_chopping_yard", "Backgrounds/Dialogue/bg_wood_chopping_yard" },
            { "bg_mountain_herb_path", "Backgrounds/Dialogue/bg_mountain_herb_path" },
            { "bg_leaking_roof_repair_scene", "Backgrounds/Dialogue/bg_leaking_roof_repair_scene" },
            { "bg_seorak_spear_hall_frost", "Backgrounds/Dialogue/bg_seorak_spear_hall_frost" },
            { "bg_hwawang_blade_training_ground", "Backgrounds/Dialogue/bg_hwawang_blade_training_ground" },
            { "bg_cheonroe_staff_dojo_gyeongseong", "Backgrounds/Dialogue/bg_cheonroe_staff_dojo_gyeongseong" },
            { "bg_hwajeop_flower_fan_courtyard", "Backgrounds/Dialogue/bg_hwajeop_flower_fan_courtyard" },
            { "bg_heukryeon_shadow_cliff_temple", "Backgrounds/Dialogue/bg_heukryeon_shadow_cliff_temple" }
        };

    private static readonly Dictionary<string, Texture2D> TextureCache =
        new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MissingWarnings =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static string ResolveBackgroundId(string id, string fallbackId = null)
    {
        string selected = !string.IsNullOrEmpty(id) ? id : fallbackId;
        selected = AssetAliasResolver.NormalizeBackgroundId(selected);
        return string.IsNullOrEmpty(selected) ? DefaultBackgroundId : selected;
    }

    public static string ResolveResourcePath(string id, string fallbackId = null)
    {
        string resolvedId = ResolveBackgroundId(id, fallbackId);
        return ResourcePaths.TryGetValue(resolvedId, out string resourcePath)
                   ? resourcePath
                   : ResourcePaths[DefaultBackgroundId];
    }

    public static Texture2D LoadBackgroundTexture(string resourcePathOrId)
    {
        string resourcePath = !string.IsNullOrEmpty(resourcePathOrId) && ResourcePaths.ContainsKey(resourcePathOrId)
                                  ? ResourcePaths[resourcePathOrId]
                                  : resourcePathOrId;
        if (string.IsNullOrEmpty(resourcePath))
        {
            resourcePath = ResourcePaths[DefaultBackgroundId];
        }

        if (TextureCache.TryGetValue(resourcePath, out Texture2D cached))
        {
            return cached;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            texture = sprite != null ? sprite.texture : null;
        }

        if (texture == null && MissingWarnings.Add(resourcePath))
        {
            Debug.LogWarning("[DialogueBackgroundRegistry] Missing dialogue background Resources asset: " + resourcePath);
        }

        TextureCache[resourcePath] = texture;
        return texture;
    }
}
}
