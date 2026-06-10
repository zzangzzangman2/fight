using System;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class CampaignArcInfo
    {
        public int index;
        public string id;
        public string displayName;
        public int levelCap;
        public int recommendedMinLevel;
        public int recommendedMaxLevel;
        public int storyParticipationXp;
        public int freeBattleParticipationXp;
        public int contributionXpCap;
        public int optionalObjectiveXp;
        public int mvpXp;
    }

    public static class CampaignArcCatalog
    {
        public const string Arc00Prologue = "ARC_00_PROLOGUE";
        public const string Arc01LocalThreats = "ARC_01_LOCAL_THREATS";
        public const string Arc02RegionalSects = "ARC_02_REGIONAL_SECTS";
        public const string Arc03FamousSectOuter = "ARC_03_FAMOUS_SECT_OUTER";
        public const string Arc04NineSectsAndFiveHouses = "ARC_04_NINE_SECTS_AND_FIVE_HOUSES";
        public const string Arc05SectHeads = "ARC_05_SECT_HEADS";
        public const string Arc06DemonCultWar = "ARC_06_DEMON_CULT_WAR";
        public const string Arc07FinalConquest = "ARC_07_FINAL_CONQUEST";

        public static readonly CampaignArcInfo[] Arcs =
        {
            new CampaignArcInfo { index = 0, id = Arc00Prologue, displayName = "초반: 폐사당 재건", levelCap = 6, recommendedMinLevel = 1, recommendedMaxLevel = 6, storyParticipationXp = 80, freeBattleParticipationXp = 45, contributionXpCap = 40, optionalObjectiveXp = 25, mvpXp = 25 },
            new CampaignArcInfo { index = 1, id = Arc01LocalThreats, displayName = "지역 위협 정리", levelCap = 12, recommendedMinLevel = 7, recommendedMaxLevel = 12, storyParticipationXp = 130, freeBattleParticipationXp = 70, contributionXpCap = 65, optionalObjectiveXp = 35, mvpXp = 35 },
            new CampaignArcInfo { index = 2, id = Arc02RegionalSects, displayName = "지방 문파전", levelCap = 18, recommendedMinLevel = 13, recommendedMaxLevel = 18, storyParticipationXp = 190, freeBattleParticipationXp = 95, contributionXpCap = 90, optionalObjectiveXp = 50, mvpXp = 50 },
            new CampaignArcInfo { index = 3, id = Arc03FamousSectOuter, displayName = "명문 외당 공략", levelCap = 25, recommendedMinLevel = 19, recommendedMaxLevel = 25, storyParticipationXp = 270, freeBattleParticipationXp = 125, contributionXpCap = 125, optionalObjectiveXp = 70, mvpXp = 70 },
            new CampaignArcInfo { index = 4, id = Arc04NineSectsAndFiveHouses, displayName = "구파일방·오대세가 전쟁", levelCap = 32, recommendedMinLevel = 26, recommendedMaxLevel = 32, storyParticipationXp = 380, freeBattleParticipationXp = 165, contributionXpCap = 175, optionalObjectiveXp = 95, mvpXp = 95 },
            new CampaignArcInfo { index = 5, id = Arc05SectHeads, displayName = "장문인·가주 결전", levelCap = 39, recommendedMinLevel = 33, recommendedMaxLevel = 39, storyParticipationXp = 540, freeBattleParticipationXp = 220, contributionXpCap = 245, optionalObjectiveXp = 135, mvpXp = 135 },
            new CampaignArcInfo { index = 6, id = Arc06DemonCultWar, displayName = "마교 대전", levelCap = 45, recommendedMinLevel = 40, recommendedMaxLevel = 45, storyParticipationXp = 760, freeBattleParticipationXp = 290, contributionXpCap = 340, optionalObjectiveXp = 190, mvpXp = 190 },
            new CampaignArcInfo { index = 7, id = Arc07FinalConquest, displayName = "천하 점령전", levelCap = 50, recommendedMinLevel = 46, recommendedMaxLevel = 50, storyParticipationXp = 1050, freeBattleParticipationXp = 370, contributionXpCap = 470, optionalObjectiveXp = 260, mvpXp = 260 }
        };

        public static CampaignArcInfo GetByIndex(int index)
        {
            int safe = ProgressionKeys.Clamp(index, 0, Arcs.Length - 1);
            return Arcs[safe];
        }

        public static CampaignArcInfo GetById(string id)
        {
            if (!string.IsNullOrEmpty(id))
            {
                for (int i = 0; i < Arcs.Length; i++)
                {
                    if (Arcs[i].id == id)
                    {
                        return Arcs[i];
                    }
                }
            }

            return Arcs[0];
        }

        public static CampaignArcInfo GetCurrent(GameSession session)
        {
            int index = ProgressionKeys.GetInt(session, ProgressionKeys.CampaignArcIndex, 0);
            return GetByIndex(index);
        }

        public static void SetCurrent(GameSession session, string arcId)
        {
            CampaignArcInfo info = GetById(arcId);
            ProgressionKeys.SetInt(session, ProgressionKeys.CampaignArcIndex, info.index);
        }

        public static void AdvanceToAtLeast(GameSession session, string arcId)
        {
            CampaignArcInfo info = GetById(arcId);
            int current = ProgressionKeys.GetInt(session, ProgressionKeys.CampaignArcIndex, 0);
            if (info.index > current)
            {
                ProgressionKeys.SetInt(session, ProgressionKeys.CampaignArcIndex, info.index);
            }
        }

        public static CampaignArcInfo EstimateByRecommendedLevel(int recommendedLevel)
        {
            int safe = Math.Max(1, Math.Min(XpTable.MaxLevel, recommendedLevel));
            for (int i = 0; i < Arcs.Length; i++)
            {
                if (safe >= Arcs[i].recommendedMinLevel && safe <= Arcs[i].recommendedMaxLevel)
                {
                    return Arcs[i];
                }
            }

            return safe >= 46 ? Arcs[7] : Arcs[0];
        }
    }
}
