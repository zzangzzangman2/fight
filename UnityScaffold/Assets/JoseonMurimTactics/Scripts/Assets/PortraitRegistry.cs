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
            { "park_mugyeom", "Portraits/NPC/park_mugyeom/park_mugyeom_standing" },
            { "yeon_ok", "Portraits/NPC/yeon_ok/yeon_ok_standing" },
            { "cho_hui", "Portraits/NPC/chohui/chohui_standing" },
            { "chohui", "Portraits/NPC/chohui/chohui_standing" },
            { "infirmary_doctor", "Portraits/NPC/infirmary_doctor/infirmary_doctor_face" },
            { "library_keeper", "Portraits/NPC/library_keeper/library_keeper_face" },
            { "market_merchant", "Portraits/NPC/market_merchant/market_merchant_face" },
            { "mission_board_clerk", "Portraits/NPC/mission_board_clerk/mission_board_clerk_face" },
            { "sect_blacksmith", "Portraits/NPC/sect_blacksmith/sect_blacksmith_face" },
            { "sobaek_village_chief", "Portraits/NPC/sobaek_village_chief/sobaek_village_chief_face" },
            { "suspicious_messenger", "Portraits/NPC/suspicious_messenger/suspicious_messenger_face" },
            { "tavern_owner", "Portraits/NPC/tavern_owner/tavern_owner_face" },
            { "baek_ryeon", "Portraits/Companions/baek_ryeon/baek_ryeon_fullbody" }
        };

    private static readonly Dictionary<string, string> PortraitIdPaths =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "park_mugyeom_face", "Portraits/NPC/park_mugyeom/park_mugyeom_face" },
            { "park_mugyeom_standing", "Portraits/NPC/park_mugyeom/park_mugyeom_standing" },
            { "park_mugyeom_calm", "Portraits/NPC/park_mugyeom/park_mugyeom_calm" },
            { "park_mugyeom_stern", "Portraits/NPC/park_mugyeom/park_mugyeom_stern" },
            { "park_mugyeom_smile", "Portraits/NPC/park_mugyeom/park_mugyeom_smile" },
            { "park_mugyeom_proud", "Portraits/NPC/park_mugyeom/park_mugyeom_proud" },
            { "park_mugyeom_surprised", "Portraits/NPC/park_mugyeom/park_mugyeom_surprised" },
            { "park_mugyeom_sad", "Portraits/NPC/park_mugyeom/park_mugyeom_sad" },
            { "park_mugyeom_sick", "Portraits/NPC/park_mugyeom/park_mugyeom_sick" },
            { "park_mugyeom_worried", "Portraits/NPC/park_mugyeom/park_mugyeom_worried" },
            { "yeon_ok_face", "Portraits/NPC/yeon_ok/yeon_ok_face" },
            { "yeon_ok_standing", "Portraits/NPC/yeon_ok/yeon_ok_standing" },
            { "yeon_ok_calm", "Portraits/NPC/yeon_ok/yeon_ok_calm" },
            { "yeon_ok_stern", "Portraits/NPC/yeon_ok/yeon_ok_stern" },
            { "yeon_ok_smile", "Portraits/NPC/yeon_ok/yeon_ok_smile" },
            { "yeon_ok_approving", "Portraits/NPC/yeon_ok/yeon_ok_approving" },
            { "yeon_ok_surprised", "Portraits/NPC/yeon_ok/yeon_ok_surprised" },
            { "yeon_ok_sad", "Portraits/NPC/yeon_ok/yeon_ok_sad" },
            { "yeon_ok_angry", "Portraits/NPC/yeon_ok/yeon_ok_angry" },
            { "yeon_ok_worried", "Portraits/NPC/yeon_ok/yeon_ok_worried" },
            { "chohui_face", "Portraits/NPC/chohui/chohui_face" },
            { "cho_hui_face", "Portraits/NPC/chohui/chohui_face" },
            { "chohui_standing", "Portraits/NPC/chohui/chohui_standing" },
            { "cho_hui_standing", "Portraits/NPC/chohui/chohui_standing" },
            { "chohui_calm", "Portraits/NPC/chohui/chohui_calm" },
            { "cho_hui_calm", "Portraits/NPC/chohui/chohui_calm" },
            { "chohui_gentle", "Portraits/NPC/chohui/chohui_gentle" },
            { "cho_hui_gentle", "Portraits/NPC/chohui/chohui_gentle" },
            { "chohui_teasing", "Portraits/NPC/chohui/chohui_teasing" },
            { "cho_hui_teasing", "Portraits/NPC/chohui/chohui_teasing" },
            { "chohui_serious", "Portraits/NPC/chohui/chohui_serious" },
            { "cho_hui_serious", "Portraits/NPC/chohui/chohui_serious" },
            { "chohui_smile", "Portraits/NPC/chohui/chohui_smile" },
            { "cho_hui_smile", "Portraits/NPC/chohui/chohui_smile" },
            { "chohui_surprised", "Portraits/NPC/chohui/chohui_surprised" },
            { "cho_hui_surprised", "Portraits/NPC/chohui/chohui_surprised" },
            { "chohui_worried", "Portraits/NPC/chohui/chohui_worried" },
            { "cho_hui_worried", "Portraits/NPC/chohui/chohui_worried" },
            { "chohui_sad", "Portraits/NPC/chohui/chohui_sad" },
            { "cho_hui_sad", "Portraits/NPC/chohui/chohui_sad" },
            { "infirmary_doctor_face", "Portraits/NPC/infirmary_doctor/infirmary_doctor_face" },
            { "library_keeper_face", "Portraits/NPC/library_keeper/library_keeper_face" },
            { "market_merchant_face", "Portraits/NPC/market_merchant/market_merchant_face" },
            { "mission_board_clerk_face", "Portraits/NPC/mission_board_clerk/mission_board_clerk_face" },
            { "sect_blacksmith_face", "Portraits/NPC/sect_blacksmith/sect_blacksmith_face" },
            { "sobaek_village_chief_face", "Portraits/NPC/sobaek_village_chief/sobaek_village_chief_face" },
            { "suspicious_messenger_face", "Portraits/NPC/suspicious_messenger/suspicious_messenger_face" },
            { "tavern_owner_face", "Portraits/NPC/tavern_owner/tavern_owner_face" },
            { "baek_ryeon_fullbody", "Portraits/Companions/baek_ryeon/baek_ryeon_fullbody" },
            { "baek_ryeon_face", "Portraits/Companions/baek_ryeon/baek_ryeon_fullbody" }
        };

    private static readonly Dictionary<string, string> StandingPortraitOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Portraits/NPC/park_mugyeom/park_mugyeom_face", "Portraits/NPC/park_mugyeom/park_mugyeom_standing" },
            { "Portraits/NPC/yeon_ok/yeon_ok_face", "Portraits/NPC/yeon_ok/yeon_ok_standing" },
            { "Portraits/NPC/chohui/chohui_face", "Portraits/NPC/chohui/chohui_standing" }
        };

    private static readonly Dictionary<string, Texture2D> TextureCache =
        new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> MissingWarnings =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    public static string ResolvePortraitResource(string speakerId, string portraitId = null)
    {
        return ResolvePortraitResource(speakerId, portraitId, string.Empty);
    }

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
                   : string.Empty;
    }

    public static string ResolveStandingPortraitResource(string resourcePath)
    {
        if (string.IsNullOrEmpty(resourcePath))
        {
            return string.Empty;
        }

        string normalizedResource = resourcePath.Replace('\\', '/');
        return StandingPortraitOverrides.TryGetValue(normalizedResource, out string standingResource)
                   ? standingResource
                   : normalizedResource;
    }

    public static string ResolveMoodPortraitResource(string speakerId, string mood, string fallbackResource)
    {
        string normalizedFallback = string.IsNullOrEmpty(fallbackResource) ? string.Empty : fallbackResource.Replace('\\', '/');
        if (!CanMoodOverride(normalizedFallback))
        {
            return normalizedFallback;
        }

        string normalizedSpeakerId = AssetAliasResolver.NormalizeCharacterId(speakerId);
        if (TryResolveMoodPortrait(normalizedSpeakerId, mood, out string moodResource))
        {
            return moodResource;
        }

        return ResolveStandingPortraitResource(normalizedFallback);
    }

    private static bool CanMoodOverride(string resourcePath)
    {
        return string.IsNullOrEmpty(resourcePath) ||
               StandingPortraitOverrides.ContainsKey(resourcePath) ||
               resourcePath.EndsWith("_standing", StringComparison.OrdinalIgnoreCase);
    }

    private static bool TryResolveMoodPortrait(string speakerId, string mood, out string resourcePath)
    {
        resourcePath = string.Empty;
        if (string.IsNullOrEmpty(speakerId) || string.IsNullOrEmpty(mood))
        {
            return false;
        }

        if (speakerId == "yeon_ok")
        {
            if (MoodHas(mood, "놀람"))
                resourcePath = "Portraits/NPC/yeon_ok/yeon_ok_surprised";
            else if (MoodHas(mood, "슬픔") || MoodHas(mood, "비통"))
                resourcePath = "Portraits/NPC/yeon_ok/yeon_ok_sad";
            else if (MoodHas(mood, "걱정"))
                resourcePath = "Portraits/NPC/yeon_ok/yeon_ok_worried";
            else if (MoodHas(mood, "꾸짖") || MoodHas(mood, "분노"))
                resourcePath = "Portraits/NPC/yeon_ok/yeon_ok_angry";
            else if (MoodHas(mood, "온화") || MoodHas(mood, "웃") || MoodHas(mood, "승인"))
                resourcePath = "Portraits/NPC/yeon_ok/yeon_ok_smile";
            else if (MoodHas(mood, "엄격") || MoodHas(mood, "단호") || MoodHas(mood, "차갑") || MoodHas(mood, "냉정"))
                resourcePath = "Portraits/NPC/yeon_ok/yeon_ok_stern";
        }
        else if (speakerId == "park_mugyeom")
        {
            if (MoodHas(mood, "놀람"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_surprised";
            else if (MoodHas(mood, "병") || MoodHas(mood, "쇠약"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_sick";
            else if (MoodHas(mood, "슬픔") || MoodHas(mood, "비통"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_sad";
            else if (MoodHas(mood, "걱정"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_worried";
            else if (MoodHas(mood, "당부") || MoodHas(mood, "자랑") || MoodHas(mood, "긍지"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_proud";
            else if (MoodHas(mood, "온화") || MoodHas(mood, "웃"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_smile";
            else if (MoodHas(mood, "엄격") || MoodHas(mood, "단호"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_stern";
            else if (MoodHas(mood, "조용") || MoodHas(mood, "담담"))
                resourcePath = "Portraits/NPC/park_mugyeom/park_mugyeom_calm";
        }
        else if (speakerId == "cho_hui")
        {
            if (MoodHas(mood, "놀람"))
                resourcePath = "Portraits/NPC/chohui/chohui_surprised";
            else if (MoodHas(mood, "슬픔") || MoodHas(mood, "비통"))
                resourcePath = "Portraits/NPC/chohui/chohui_sad";
            else if (MoodHas(mood, "걱정"))
                resourcePath = "Portraits/NPC/chohui/chohui_worried";
            else if (MoodHas(mood, "핀잔") || MoodHas(mood, "타박") || MoodHas(mood, "놀림"))
                resourcePath = "Portraits/NPC/chohui/chohui_teasing";
            else if (MoodHas(mood, "현실") || MoodHas(mood, "단호") || MoodHas(mood, "엄격") || MoodHas(mood, "냉정"))
                resourcePath = "Portraits/NPC/chohui/chohui_serious";
            else if (MoodHas(mood, "온화") || MoodHas(mood, "웃") || MoodHas(mood, "기쁨"))
                resourcePath = "Portraits/NPC/chohui/chohui_smile";
            else if (MoodHas(mood, "부드") || MoodHas(mood, "다정"))
                resourcePath = "Portraits/NPC/chohui/chohui_gentle";
        }

        return !string.IsNullOrEmpty(resourcePath);
    }

    private static bool MoodHas(string mood, string needle)
    {
        return mood.IndexOf(needle, StringComparison.OrdinalIgnoreCase) >= 0;
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
