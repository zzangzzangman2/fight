using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
public static class AssetAliasResolver
{
    private static readonly Dictionary<string, string> BackgroundAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "joseon_murim_game_map", "bg_baekdu_light_sword_gate_day" }
        };

    private static readonly Dictionary<string, string> PortraitAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "cho_hui_face", "chohui_face" },
            { "chohui_face", "chohui_face" },
            { "cho_hui", "chohui" },
            { "chohui", "chohui" }
        };

    public static string NormalizeCharacterId(string id)
    {
        return CharacterIdAliasResolver.Normalize(id);
    }

    public static string NormalizeBackgroundId(string id)
    {
        return Normalize(id, BackgroundAliases);
    }

    public static string NormalizePortraitId(string id)
    {
        return Normalize(id, PortraitAliases);
    }

    private static string Normalize(string id, Dictionary<string, string> aliases)
    {
        if (string.IsNullOrEmpty(id))
        {
            return id;
        }

        return aliases.TryGetValue(id, out string canonical) ? canonical : id;
    }
}
}
