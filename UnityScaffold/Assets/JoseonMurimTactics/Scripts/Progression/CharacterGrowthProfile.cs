using System;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class CharacterGrowthProfile
    {
        public string characterId;
        public string displayName;
        public int hpGrowth;
        public int innerGrowth;
        public int strengthGrowth;
        public int agilityGrowth;
        public int innerPowerGrowth;
        public int spiritGrowth;
        public int insightGrowth;
        public int charmGrowth;
        public string primaryStatKey;
        public string defaultMasteryTag;

        public int GetGrowth(string statKey)
        {
            switch (statKey)
            {
                case "hp": return hpGrowth;
                case "inner": return innerGrowth;
                case "strength": return strengthGrowth;
                case "agility": return agilityGrowth;
                case "innerPower": return innerPowerGrowth;
                case "spirit": return spiritGrowth;
                case "insight": return insightGrowth;
                case "charm": return charmGrowth;
                default: return 0;
            }
        }
    }

    public static class CharacterGrowthCatalog
    {
        // 프로젝트 전반(전투 에셋/씬 유닛)과 동일한 표기를 쓴다 — park_sungjun.
        public const string ProtagonistId = "park_sungjun";
        public const string BaekRyeonId = "baek_ryeon";
        public const string HanBiyeonId = "han_biyeon";
        public const string DoArinId = "do_arin";
        public const string JinSeoyulId = "jin_seoyul";
        public const string SeoAId = CharacterIdAliasResolver.ShinSeoaId;

        public static readonly string[] CorePartyIds =
        {
            ProtagonistId,
            BaekRyeonId,
            DoArinId,
            JinSeoyulId,
            SeoAId,
            HanBiyeonId
        };

        public static readonly string[] StatKeys =
        {
            "strength",
            "agility",
            "innerPower",
            "spirit",
            "insight",
            "charm"
        };

        public static readonly string[] GrowthMeterKeys =
        {
            "hp",
            "inner",
            "strength",
            "agility",
            "innerPower",
            "spirit",
            "insight",
            "charm"
        };

        public static readonly string[] CommonMasteryTags =
        {
            "mastery:sword",
            "mastery:qigong",
            "mastery:spear",
            "mastery:dagger",
            "mastery:medicine",
            "mastery:stealth",
            "mastery:terrain",
            "mastery:formation",
            "mastery:demon_slaying"
        };

        public static string NormalizeCharacterId(string raw)
        {
            if (string.IsNullOrEmpty(raw))
            {
                return ProtagonistId;
            }

            string id = CharacterIdAliasResolver.Normalize(raw);
            return string.IsNullOrEmpty(id) ? ProtagonistId : id;
        }

        public static string DisplayName(string characterId)
        {
            switch (NormalizeCharacterId(characterId))
            {
                case ProtagonistId: return "박성준";
                case BaekRyeonId: return "백련";
                case HanBiyeonId: return "한비연";
                case DoArinId: return "도아린";
                case JinSeoyulId: return "진서율";
                case SeoAId: return "신서아";
                default: return string.IsNullOrEmpty(characterId) ? "미상" : characterId;
            }
        }

        public static CharacterGrowthProfile Get(string characterId)
        {
            string id = NormalizeCharacterId(characterId);
            switch (id)
            {
                case BaekRyeonId:
                    return new CharacterGrowthProfile { characterId = id, displayName = "백련", hpGrowth = 50, innerGrowth = 75, strengthGrowth = 30, agilityGrowth = 40, innerPowerGrowth = 75, spiritGrowth = 65, insightGrowth = 55, charmGrowth = 40, primaryStatKey = "innerPower", defaultMasteryTag = "mastery:qigong" };
                case DoArinId:
                    return new CharacterGrowthProfile { characterId = id, displayName = "도아린", hpGrowth = 85, innerGrowth = 40, strengthGrowth = 75, agilityGrowth = 45, innerPowerGrowth = 40, spiritGrowth = 60, insightGrowth = 30, charmGrowth = 45, primaryStatKey = "strength", defaultMasteryTag = "mastery:spear" };
                case JinSeoyulId:
                    return new CharacterGrowthProfile { characterId = id, displayName = "진서율", hpGrowth = 50, innerGrowth = 65, strengthGrowth = 40, agilityGrowth = 75, innerPowerGrowth = 65, spiritGrowth = 40, insightGrowth = 60, charmGrowth = 50, primaryStatKey = "agility", defaultMasteryTag = "mastery:sword" };
                case SeoAId:
                    return new CharacterGrowthProfile { characterId = id, displayName = "신서아", hpGrowth = 45, innerGrowth = 65, strengthGrowth = 30, agilityGrowth = 65, innerPowerGrowth = 55, spiritGrowth = 60, insightGrowth = 50, charmGrowth = 75, primaryStatKey = "charm", defaultMasteryTag = "mastery:medicine" };
                case HanBiyeonId:
                    return new CharacterGrowthProfile { characterId = id, displayName = "한비연", hpGrowth = 50, innerGrowth = 55, strengthGrowth = 40, agilityGrowth = 75, innerPowerGrowth = 45, spiritGrowth = 40, insightGrowth = 75, charmGrowth = 45, primaryStatKey = "insight", defaultMasteryTag = "mastery:stealth" };
                case ProtagonistId:
                default:
                    return new CharacterGrowthProfile { characterId = id, displayName = DisplayName(id), hpGrowth = 65, innerGrowth = 50, strengthGrowth = 50, agilityGrowth = 50, innerPowerGrowth = 45, spiritGrowth = 50, insightGrowth = 45, charmGrowth = 65, primaryStatKey = "charm", defaultMasteryTag = "mastery:sword" };
            }
        }
    }
}
