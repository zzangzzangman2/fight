using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
public static class CharacterIdAliasResolver
{
    public const string LegacySeoAId = "seo_a";
    public const string ShinSeoaId = "shin_seoa";

    private static readonly Dictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "park_sungjun", "park_sungjun" },
            { "protagonist", "park_sungjun" },
            { "hero", "park_sungjun" },
            { "park", "park_sungjun" },
            { "박성준", "park_sungjun" },
            { "성준", "park_sungjun" },
            { "baek_ryeon", "baek_ryeon" },
            { "baekryeon", "baek_ryeon" },
            { "baek", "baek_ryeon" },
            { "백련", "baek_ryeon" },
            { "do_arin", "do_arin" },
            { "doarin", "do_arin" },
            { "arin", "do_arin" },
            { "도아린", "do_arin" },
            { "jin_seoyul", "jin_seoyul" },
            { "jinseoyul", "jin_seoyul" },
            { "jin", "jin_seoyul" },
            { "진서율", "jin_seoyul" },
            { LegacySeoAId, ShinSeoaId },
            { ShinSeoaId, ShinSeoaId },
            { "서아", ShinSeoaId },
            { "신서아", ShinSeoaId },
            { "han_biyeon", "han_biyeon" },
            { "hanbiyeon", "han_biyeon" },
            { "han", "han_biyeon" },
            { "한비연", "han_biyeon" },
            { "yudalgeun", "yudalgeun" },
            { "yu_dalgeun", "yudalgeun" },
            { "yoo_dal_geun", "yudalgeun" },
            { "유달근", "yudalgeun" },
            { "cho_hui", "cho_hui" },
            { "chohui", "cho_hui" },
            { "초희", "cho_hui" }
        };

    public static string Normalize(string raw)
    {
        if (string.IsNullOrEmpty(raw))
        {
            return string.Empty;
        }

        string id = raw.Trim();
        return Aliases.TryGetValue(id, out string canonical) ? canonical : ProgressionKeys.SafeId(id);
    }

    public static bool IsLegacyShinSeoa(string raw)
    {
        return string.Equals(raw, LegacySeoAId, StringComparison.OrdinalIgnoreCase);
    }
}
}
