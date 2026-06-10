using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
public static class AssetAliasResolver
{
    private static readonly Dictionary<string, string> CharacterAliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "park_sungjun", "park_sungjun" },
            { "protagonist", "park_sungjun" },
            { "park", "park_sungjun" },
            { "seo_a", "seo_a" },
            { "shin_seoa", "seo_a" },
            { "cho_hui", "cho_hui" },
            { "chohui", "cho_hui" },
            { "do_arin", "do_arin" },
            { "arin", "do_arin" },
            { "doarin", "do_arin" },
            { "baek_ryeon", "baek_ryeon" },
            { "baek", "baek_ryeon" },
            { "baekryeon", "baek_ryeon" },
            { "han_biyeon", "han_biyeon" },
            { "han", "han_biyeon" },
            { "hanbiyeon", "han_biyeon" },
            { "jin_seoyul", "jin_seoyul" },
            { "jin", "jin_seoyul" },
            { "jinseoyul", "jin_seoyul" }
        };

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
        return Normalize(id, CharacterAliases);
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
