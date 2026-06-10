using System;
using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
public static class PortraitRegistry
{
    private static readonly Dictionary<string, string> CharacterPortraits =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "park_mugyeom", "Portraits/NPC/park_mugyeom/park_mugyeom_face" },
            { "yeon_ok", "Portraits/NPC/yeon_ok/yeon_ok_face" },
            { "cho_hui", "Portraits/NPC/chohui/chohui_face" },
            { "chohui", "Portraits/NPC/chohui/chohui_face" },
            { "infirmary_doctor", "Portraits/NPC/infirmary_doctor/infirmary_doctor_face" },
            { "library_keeper", "Portraits/NPC/library_keeper/library_keeper_face" },
            { "market_merchant", "Portraits/NPC/market_merchant/market_merchant_face" },
            { "mission_board_clerk", "Portraits/NPC/mission_board_clerk/mission_board_clerk_face" },
            { "sect_blacksmith", "Portraits/NPC/sect_blacksmith/sect_blacksmith_face" },
            { "sobaek_village_chief", "Portraits/NPC/sobaek_village_chief/sobaek_village_chief_face" },
            { "suspicious_messenger", "Portraits/NPC/suspicious_messenger/suspicious_messenger_face" },
            { "tavern_owner", "Portraits/NPC/tavern_owner/tavern_owner_face" }
        };

    private static readonly Dictionary<string, string> PortraitIdPaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "park_mugyeom_face", "Portraits/NPC/park_mugyeom/park_mugyeom_face" },
            { "yeon_ok_face", "Portraits/NPC/yeon_ok/yeon_ok_face" },
            { "chohui_face", "Portraits/NPC/chohui/chohui_face" },
            { "cho_hui_face", "Portraits/NPC/chohui/chohui_face" },
            { "infirmary_doctor_face", "Portraits/NPC/infirmary_doctor/infirmary_doctor_face" },
            { "library_keeper_face", "Portraits/NPC/library_keeper/library_keeper_face" },
            { "market_merchant_face", "Portraits/NPC/market_merchant/market_merchant_face" },
            { "mission_board_clerk_face", "Portraits/NPC/mission_board_clerk/mission_board_clerk_face" },
            { "sect_blacksmith_face", "Portraits/NPC/sect_blacksmith/sect_blacksmith_face" },
            { "sobaek_village_chief_face", "Portraits/NPC/sobaek_village_chief/sobaek_village_chief_face" },
            { "suspicious_messenger_face", "Portraits/NPC/suspicious_messenger/suspicious_messenger_face" },
            { "tavern_owner_face", "Portraits/NPC/tavern_owner/tavern_owner_face" }
        };

    private static readonly Dictionary<string, Texture2D> TextureCache =
        new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MissingWarnings =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static string ResolvePortraitResource(string speakerId, string portraitId, string explicitResource)
    {
        if (!string.IsNullOrEmpty(explicitResource))
        {
            return explicitResource;
        }

        string normalizedPortraitId = AssetAliasResolver.NormalizePortraitId(portraitId);
        if (!string.IsNullOrEmpty(normalizedPortraitId) &&
            PortraitIdPaths.TryGetValue(normalizedPortraitId, out string portraitResource))
        {
            return portraitResource;
        }

        string normalizedSpeakerId = AssetAliasResolver.NormalizeCharacterId(speakerId);
        return !string.IsNullOrEmpty(normalizedSpeakerId) &&
               CharacterPortraits.TryGetValue(normalizedSpeakerId, out string characterResource)
                   ? characterResource
                   : null;
    }

    public static Texture2D LoadPortraitTexture(string resourcePath)
    {
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
            Sprite sprite = Resources.Load<Sprite>(resourcePath);
            texture = sprite != null ? sprite.texture : null;
        }

        if (texture == null && MissingWarnings.Add(resourcePath))
        {
            Debug.LogWarning("[PortraitRegistry] Missing portrait Resources asset: " + resourcePath);
        }

        TextureCache[resourcePath] = texture;
        return texture;
    }
}
}
