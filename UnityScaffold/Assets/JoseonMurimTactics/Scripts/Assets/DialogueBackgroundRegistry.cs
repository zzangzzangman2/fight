using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public static class DialogueBackgroundRegistry
{
    public const string DefaultBackgroundId = "bg_baekdu_training_yard_snow";

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
            { "bg_pyesadang_main_hall_day", "Backgrounds/Dialogue/bg_pyesadang_main_hall_day" },
            { "bg_pyesadang_main_hall_night", "Backgrounds/Dialogue/bg_pyesadang_main_hall_night" },
            { "bg_pyesadang_courtyard_dawn", "Backgrounds/Dialogue/bg_pyesadang_courtyard_dawn" },
            { "bg_pyesadang_training_ground_evening", "Backgrounds/Dialogue/bg_pyesadang_training_ground_evening" },
            { "bg_pyesadang_infirmary", "Backgrounds/Dialogue/bg_pyesadang_infirmary" },
            { "bg_pyesadang_market_stall", "Backgrounds/Dialogue/bg_pyesadang_market_stall" },
            { "bg_pyesadang_library", "Backgrounds/Dialogue/bg_pyesadang_library" },
            { "bg_pyesadang_tavern_corner", "Backgrounds/Dialogue/bg_pyesadang_tavern_corner" },
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
    private static Dictionary<string, string> manifestResourcePaths;

    public static string ResolveBackgroundId(string id, string fallbackId = null)
    {
        string selected = !string.IsNullOrEmpty(id) ? id : fallbackId;
        selected = AssetAliasResolver.NormalizeBackgroundId(selected);
        return string.IsNullOrEmpty(selected) ? DefaultBackgroundId : selected;
    }

    public static string ResolveResourcePath(string id, string fallbackId = null)
    {
        string resolvedId = ResolveBackgroundId(id, fallbackId);
        if (TryResolveResourcePath(resolvedId, out string resourcePath))
        {
            return resourcePath;
        }

        if (!string.IsNullOrEmpty(fallbackId) && TryResolveResourcePath(fallbackId, out resourcePath))
        {
            return resourcePath;
        }

        return ResourcePaths[DefaultBackgroundId];
    }

    public static Texture2D LoadBackgroundTexture(string resourcePathOrId)
    {
        string resourcePath = !string.IsNullOrEmpty(resourcePathOrId) &&
                              TryResolveResourcePath(resourcePathOrId, out string resolvedPath)
                                  ? resolvedPath
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

    public static bool TryResolveResourcePath(string id, out string resourcePath)
    {
        string normalizedId = AssetAliasResolver.NormalizeBackgroundId(id);
        if (!string.IsNullOrEmpty(normalizedId))
        {
            Dictionary<string, string> manifestPaths = GetManifestResourcePaths();
            if (manifestPaths.TryGetValue(normalizedId, out resourcePath))
            {
                return true;
            }

            if (ResourcePaths.TryGetValue(normalizedId, out resourcePath))
            {
                return true;
            }
        }

        resourcePath = string.Empty;
        return false;
    }

    private static Dictionary<string, string> GetManifestResourcePaths()
    {
        if (manifestResourcePaths != null)
        {
            return manifestResourcePaths;
        }

        manifestResourcePaths = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        AuthoringContentManifest manifest = AuthoringContentManifest.LoadFromResources();
        foreach (AuthoringMediaItem background in manifest.backgrounds)
        {
            if (background == null || string.IsNullOrEmpty(background.id) ||
                string.IsNullOrEmpty(background.resourcePath))
            {
                continue;
            }

            string normalizedId = AssetAliasResolver.NormalizeBackgroundId(background.id);
            if (!string.IsNullOrEmpty(normalizedId))
            {
                manifestResourcePaths[normalizedId] = background.resourcePath;
            }
        }

        return manifestResourcePaths;
    }
}
}
