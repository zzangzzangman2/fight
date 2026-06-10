using System;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class FactionConquestState
    {
        public string factionId;
        public string displayName;
        public int stage;
        public int influence;
        public int hostility;
        public int control;
        public bool discovered;
        public bool defeated;
    }

    [Serializable]
    public sealed class FactionConquestInfo
    {
        public string factionId;
        public string displayName;
        public string group;
        public int recommendedLevel;
        public string arcId;
        public string finalBattleId;
    }

    public static class FactionConquestCatalog
    {
        public static readonly FactionConquestInfo[] Factions =
        {
            new FactionConquestInfo { factionId = "nine:shaolin", displayName = "소림", group = "구파일방", recommendedLevel = 28, arcId = CampaignArcCatalog.Arc04NineSectsAndFiveHouses, finalBattleId = "BATTLE_NINE_SHAOLIN_MASTER" },
            new FactionConquestInfo { factionId = "nine:wudang", displayName = "무당", group = "구파일방", recommendedLevel = 29, arcId = CampaignArcCatalog.Arc04NineSectsAndFiveHouses, finalBattleId = "BATTLE_NINE_WUDANG_LEADER" },
            new FactionConquestInfo { factionId = "nine:emei", displayName = "아미", group = "구파일방", recommendedLevel = 30, arcId = CampaignArcCatalog.Arc04NineSectsAndFiveHouses, finalBattleId = "BATTLE_NINE_EMEI_ABBESS" },
            new FactionConquestInfo { factionId = "nine:huashan", displayName = "화산", group = "구파일방", recommendedLevel = 31, arcId = CampaignArcCatalog.Arc04NineSectsAndFiveHouses, finalBattleId = "BATTLE_NINE_HUASHAN_MASTER" },
            new FactionConquestInfo { factionId = "nine:kunlun", displayName = "곤륜", group = "구파일방", recommendedLevel = 32, arcId = CampaignArcCatalog.Arc04NineSectsAndFiveHouses, finalBattleId = "BATTLE_NINE_KUNLUN_MASTER" },
            new FactionConquestInfo { factionId = "nine:gaibang", displayName = "개방", group = "구파일방", recommendedLevel = 32, arcId = CampaignArcCatalog.Arc04NineSectsAndFiveHouses, finalBattleId = "BATTLE_NINE_GAIBANG_LEADER" },
            new FactionConquestInfo { factionId = "nine:qingcheng", displayName = "청성", group = "구파일방", recommendedLevel = 33, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_NINE_QINGCHENG_MASTER" },
            new FactionConquestInfo { factionId = "nine:diancang", displayName = "점창", group = "구파일방", recommendedLevel = 34, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_NINE_DIANCANG_MASTER" },
            new FactionConquestInfo { factionId = "nine:zhongnan", displayName = "종남", group = "구파일방", recommendedLevel = 35, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_NINE_ZHONGNAN_MASTER" },
            new FactionConquestInfo { factionId = "house:namgung", displayName = "남궁세가", group = "오대세가", recommendedLevel = 34, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_HOUSE_NAMGUNG_HEAD" },
            new FactionConquestInfo { factionId = "house:tang", displayName = "사천당문", group = "오대세가", recommendedLevel = 35, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_HOUSE_TANG_HEAD" },
            new FactionConquestInfo { factionId = "house:peng", displayName = "하북팽가", group = "오대세가", recommendedLevel = 36, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_HOUSE_PENG_HEAD" },
            new FactionConquestInfo { factionId = "house:zhuge", displayName = "제갈세가", group = "오대세가", recommendedLevel = 37, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_HOUSE_ZHUGE_HEAD" },
            new FactionConquestInfo { factionId = "house:moyong", displayName = "모용세가", group = "오대세가", recommendedLevel = 38, arcId = CampaignArcCatalog.Arc05SectHeads, finalBattleId = "BATTLE_HOUSE_MOYONG_HEAD" },
            new FactionConquestInfo { factionId = "demon:cult", displayName = "마교", group = "마교", recommendedLevel = 42, arcId = CampaignArcCatalog.Arc06DemonCultWar, finalBattleId = "BATTLE_DEMON_CULT_LEADER" }
        };

        public static FactionConquestInfo Get(string factionId)
        {
            for (int i = 0; i < Factions.Length; i++)
            {
                if (Factions[i].factionId == factionId)
                {
                    return Factions[i];
                }
            }

            return new FactionConquestInfo { factionId = factionId, displayName = factionId, group = "기타", recommendedLevel = 1, arcId = CampaignArcCatalog.Arc00Prologue, finalBattleId = string.Empty };
        }
    }

    public sealed class FactionConquestService
    {
        private readonly GameSession session;

        public FactionConquestService(GameSession session)
        {
            this.session = session;
        }

        public FactionConquestState GetState(string factionId)
        {
            FactionConquestInfo info = FactionConquestCatalog.Get(factionId);
            FactionConquestState state = new FactionConquestState();
            state.factionId = factionId;
            state.displayName = info.displayName;
            state.stage = ProgressionKeys.GetInt(session, ProgressionKeys.ConquestStage(factionId), 0);
            state.influence = ProgressionKeys.GetInt(session, ProgressionKeys.ConquestInfluence(factionId), 0);
            state.hostility = ProgressionKeys.GetInt(session, ProgressionKeys.ConquestHostility(factionId), 0);
            state.control = ProgressionKeys.GetInt(session, ProgressionKeys.ConquestControl(factionId), 0);
            state.discovered = ProgressionKeys.HasFlag(session, ProgressionKeys.ConquestDiscoveredFlag(factionId));
            state.defeated = ProgressionKeys.HasFlag(session, ProgressionKeys.ConquestDefeatedFlag(factionId));
            return state;
        }

        public FactionConquestState ApplyBattleReward(string factionId, int stageDelta, int controlDelta, int hostilityDelta, bool markDiscovered)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                return null;
            }

            if (markDiscovered)
            {
                ProgressionKeys.SetFlag(session, ProgressionKeys.ConquestDiscoveredFlag(factionId));
            }

            int stage = ProgressionKeys.AddInt(session, ProgressionKeys.ConquestStage(factionId), stageDelta);
            ProgressionKeys.AddInt(session, ProgressionKeys.ConquestControl(factionId), controlDelta);
            ProgressionKeys.AddInt(session, ProgressionKeys.ConquestHostility(factionId), hostilityDelta);

            if (stage >= 100)
            {
                ProgressionKeys.SetFlag(session, ProgressionKeys.ConquestDefeatedFlag(factionId));
                ProgressionKeys.SetInt(session, ProgressionKeys.ConquestStage(factionId), 100);
            }

            return GetState(factionId);
        }
    }
}
