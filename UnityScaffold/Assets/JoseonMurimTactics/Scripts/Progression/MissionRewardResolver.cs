using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class ProgressionMissionTuning
    {
        public string battleId;
        public string questId;
        public string displayName;
        public string campaignArcId;
        public int recommendedLevel = 1;
        public int baseParticipationXp = 80;
        public int contributionXpCap = 40;
        public int optionalObjectiveXp = 25;
        public int mvpXp = 25;
        public int masteryAmount = 16;
        public int bondDeltaOnWin = 1;
        public bool repeatable;
        public bool consumesFreeTime;
        public bool isStory = true;
        public string factionId;
        public int factionConquestStageDelta;
        public int factionControlDelta;
        public int factionHostilityDelta;
        public List<string> masteryTags = new List<string>();
        public List<RewardDelta> materialRewards = new List<RewardDelta>();
    }

    /// <summary>
    /// 전투 결과를 성장 보상으로 번역한다.
    /// 기존 BattleResultApplyService가 처리하는 은전/아이템/평판은 여기서 중복 지급하지 않는다.
    /// </summary>
    public static class MissionRewardResolver
    {
        public static RewardBundle Resolve(GameSession session, BattleResultData result, BattleDefinition definition, BattleContributionSummary contribution)
        {
            RewardBundle bundle = new RewardBundle();
            if (result == null)
            {
                return bundle;
            }

            ProgressionMissionTuning tuning = GetTuning(session, definition, result);
            CampaignArcInfo arc = CampaignArcCatalog.GetById(tuning.campaignArcId);
            ProgressionService progression = new ProgressionService(session);

            List<string> participants = BattleDeploymentService.ResolveRewardParticipants(session, definition);

            int partyAverage = AverageLevel(progression, participants);
            float repeatMultiplier = tuning.repeatable ? GetRepeatMultiplier(session, tuning) : 1f;
            float overlevelMultiplier = GetOverlevelMultiplier(partyAverage, tuning.recommendedLevel);
            float resultMultiplier = result.Won ? 1f : 0.35f;

            bundle.battleId = !string.IsNullOrEmpty(tuning.battleId) ? tuning.battleId : result.battleId;
            bundle.questId = tuning.questId;
            bundle.campaignArcId = arc.id;
            bundle.factionId = tuning.factionId;
            bundle.recommendedLevel = tuning.recommendedLevel;
            bundle.partyAverageLevel = partyAverage;
            bundle.deployedMemberCount = participants.Count;
            bundle.usedExplicitDeployment = BattleDeploymentService.HasExplicitDeployment(session, definition);
            bundle.deploymentBattleId = BattleDeploymentService.ActiveBattleId(session);
            bundle.won = result.Won;
            bundle.repeatMultiplier = repeatMultiplier;
            bundle.overlevelMultiplier = overlevelMultiplier;
            bundle.resultMultiplier = resultMultiplier;
            bundle.overleveled = overlevelMultiplier < 1f;
            bundle.factionConquestStageDelta = result.Won ? tuning.factionConquestStageDelta : 0;
            bundle.factionControlDelta = result.Won ? tuning.factionControlDelta : 0;
            bundle.factionHostilityDelta = tuning.factionHostilityDelta;

            for (int i = 0; i < tuning.materialRewards.Count; i++)
            {
                RewardDelta material = tuning.materialRewards[i];
                int scaled = result.Won ? material.delta : Math.Max(0, material.delta / 3);
                if (scaled > 0)
                {
                    bundle.materialRewards.Add(new RewardDelta(material.id, scaled));
                }
            }

            string mvpId = contribution != null ? contribution.MvpCharacterId() : null;
            int optionalCount = CountCompletedOptionalObjectives(result, definition);

            for (int i = 0; i < participants.Count; i++)
            {
                string characterId = CharacterGrowthCatalog.NormalizeCharacterId(participants[i]);
                CharacterGrowthProfile profile = CharacterGrowthCatalog.Get(characterId);
                CharacterProgressState state = progression.GetSnapshot(characterId);

                int contributionXp = CalculateContributionXp(characterId, contribution, tuning.contributionXpCap, participants.Count);
                int optionalXp = optionalCount * tuning.optionalObjectiveXp;
                int mvpXp = characterId == mvpId ? tuning.mvpXp : 0;
                int baseXp = tuning.baseParticipationXp + contributionXp + optionalXp + mvpXp;

                float personalLevelMultiplier = GetPersonalLevelMultiplier(state.level, tuning.recommendedLevel);
                int finalXp = Math.Max(0, (int)Math.Round(baseXp * repeatMultiplier * overlevelMultiplier * resultMultiplier * personalLevelMultiplier));

                CharacterReward reward = new CharacterReward();
                reward.characterId = characterId;
                reward.displayName = CharacterGrowthCatalog.DisplayName(characterId);
                reward.deployed = true;
                reward.baseXp = baseXp;
                reward.finalXp = finalXp;
                reward.masteryAmount = Math.Max(0, (int)Math.Round(tuning.masteryAmount * repeatMultiplier * resultMultiplier));
                reward.bondDelta = result.Won ? tuning.bondDeltaOnWin : 0;

                if (tuning.masteryTags.Count > 0)
                {
                    reward.masteryTags.AddRange(tuning.masteryTags);
                }
                else
                {
                    reward.masteryTags.Add(profile.defaultMasteryTag);
                }

                if (!reward.masteryTags.Contains(profile.defaultMasteryTag))
                {
                    reward.masteryTags.Add(profile.defaultMasteryTag);
                }

                bundle.characterRewards.Add(reward);
            }

            string title = definition != null && !string.IsNullOrEmpty(definition.title) ? definition.title : bundle.battleId;
            bundle.summaryLines.Add(title + " 성장 보상: 출진 " + participants.Count + "명만 적용, 평균 Lv." + partyAverage + ", 권장 Lv." + tuning.recommendedLevel + ", 반복 x" + repeatMultiplier.ToString("0.##") + ", 오버레벨 x" + overlevelMultiplier.ToString("0.##"));
            return bundle;
        }

        public static RewardBundle Resolve(GameSession session, BattleResultData result, BattleDefinition definition)
        {
            return Resolve(session, result, definition, null);
        }

        public static void RecordDailyAttempt(GameSession session, RewardBundle bundle)
        {
            if (session == null || bundle == null)
            {
                return;
            }

            string id = !string.IsNullOrEmpty(bundle.questId) ? bundle.questId : bundle.battleId;
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            int day = ProgressionKeys.GetInt(session, ProgressionKeys.CalendarDay, 0);
            ProgressionKeys.AddInt(session, ProgressionKeys.DailyMissionAttempt(day, id), 1);
        }

        private static ProgressionMissionTuning GetTuning(GameSession session, BattleDefinition definition, BattleResultData result)
        {
            string battleId = definition != null ? definition.id : result.battleId;
            string questId = definition != null ? definition.questId : string.Empty;

            ProgressionMissionTuning known = KnownTuning(battleId, questId);
            if (known != null)
            {
                return known;
            }

            CampaignArcInfo arc = CampaignArcCatalog.GetCurrent(session);
            if (definition != null && definition.repeatable)
            {
                arc = CampaignArcCatalog.Arcs[0];
            }

            ProgressionMissionTuning fallback = new ProgressionMissionTuning();
            fallback.battleId = battleId;
            fallback.questId = questId;
            fallback.displayName = definition != null ? definition.title : battleId;
            fallback.campaignArcId = arc.id;
            fallback.recommendedLevel = Math.Max(1, arc.recommendedMinLevel);
            fallback.baseParticipationXp = definition != null && definition.repeatable ? arc.freeBattleParticipationXp : arc.storyParticipationXp;
            fallback.contributionXpCap = arc.contributionXpCap;
            fallback.optionalObjectiveXp = arc.optionalObjectiveXp;
            fallback.mvpXp = arc.mvpXp;
            fallback.masteryAmount = definition != null && definition.repeatable ? 14 : 22;
            fallback.bondDeltaOnWin = definition != null && definition.repeatable ? 1 : 2;
            fallback.repeatable = definition != null && definition.repeatable;
            fallback.consumesFreeTime = fallback.repeatable;
            fallback.isStory = !fallback.repeatable;
            fallback.masteryTags.Add("mastery:sword");
            fallback.masteryTags.Add("mastery:formation");
            return fallback;
        }

        private static ProgressionMissionTuning KnownTuning(string battleId, string questId)
        {
            string id = !string.IsNullOrEmpty(questId) ? questId : battleId;
            if (id == "MISSION_CH01_BLACK_MARK")
            {
                ProgressionMissionTuning t = BaseStory(battleId, questId, "폐사당 고개 방어전", CampaignArcCatalog.Arc00Prologue, 3);
                t.materialRewards.Add(new RewardDelta("supply:medicine", 2));
                t.masteryTags.Add("mastery:formation");
                t.masteryTags.Add("mastery:terrain");
                return t;
            }

            if (id == "MISSION_CH01_SEORAK_REQUEST" || battleId == HubController.SeorakPassRescueBattleId)
            {
                ProgressionMissionTuning t = BaseStory(battleId, questId, "설운령 약초 수레 호위전", CampaignArcCatalog.Arc00Prologue, 4);
                t.materialRewards.Add(new RewardDelta("supply:herb", 2));
                t.masteryTags.Add("mastery:spear");
                t.masteryTags.Add("mastery:formation");
                t.masteryTags.Add("mastery:medicine");
                return t;
            }

            if (id == "MISSION_FREE_SOBAEK_BANDIT_LAIR" || battleId == HubController.BanditLairBattleId)
            {
                ProgressionMissionTuning t = BaseFree(battleId, questId, "소백촌 도적 소굴 토벌", 2, 45);
                t.factionId = "local:black_hat_guild";
                t.factionConquestStageDelta = 6;
                t.factionControlDelta = 2;
                t.factionHostilityDelta = -2;
                t.materialRewards.Add(new RewardDelta("supply:medicine", 1));
                t.materialRewards.Add(new RewardDelta("supply:wood", 1));
                t.masteryTags.Add("mastery:sword");
                t.masteryTags.Add("mastery:stealth");
                t.masteryTags.Add("mastery:terrain");
                return t;
            }

            if (id == "MISSION_FREE_SOBAEK_WOLF_PASS" || battleId == HubController.WolfPassBattleId)
            {
                ProgressionMissionTuning t = BaseFree(battleId, questId, "소백촌 늑대 고개 방어", 3, 42);
                t.materialRewards.Add(new RewardDelta("supply:leather", 1));
                t.masteryTags.Add("mastery:formation");
                t.masteryTags.Add("mastery:terrain");
                return t;
            }

            if (id == "MISSION_FREE_SOBAEK_TIGER_RAVINE" || battleId == HubController.TigerRavineBattleId)
            {
                ProgressionMissionTuning t = BaseFree(battleId, questId, "백호 바위골 주민 구조", 5, 62);
                t.materialRewards.Add(new RewardDelta("supply:medicine", 1));
                t.materialRewards.Add(new RewardDelta("material:tiger_pelt", 1));
                t.masteryTags.Add("mastery:spear");
                t.masteryTags.Add("mastery:terrain");
                return t;
            }

            if (id == "MISSION_FREE_SOBAEK_LEOPARD_CLIFF" || battleId == HubController.LeopardCliffBattleId)
            {
                ProgressionMissionTuning t = BaseFree(battleId, questId, "표범 절벽길 약초꾼 호송", 6, 66);
                t.materialRewards.Add(new RewardDelta("supply:herb", 2));
                t.masteryTags.Add("mastery:agility");
                t.masteryTags.Add("mastery:medicine");
                return t;
            }

            if (battleId == "BATTLE_NINE_HUASHAN_OUTER")
            {
                ProgressionMissionTuning t = BaseStory(battleId, questId, "화산파 외당 공략", CampaignArcCatalog.Arc04NineSectsAndFiveHouses, 28);
                t.factionId = "nine:huashan";
                t.factionConquestStageDelta = 12;
                t.factionControlDelta = 4;
                t.factionHostilityDelta = 5;
                t.masteryTags.Add("mastery:sword");
                return t;
            }

            if (battleId == "BATTLE_NINE_WUDANG_LEADER")
            {
                ProgressionMissionTuning t = BaseStory(battleId, questId, "무당 장문 결전", CampaignArcCatalog.Arc05SectHeads, 35);
                t.factionId = "nine:wudang";
                t.factionConquestStageDelta = 25;
                t.factionControlDelta = 8;
                t.factionHostilityDelta = 8;
                t.masteryTags.Add("mastery:qigong");
                return t;
            }

            if (battleId == "BATTLE_DEMON_BLACK_ALTAR")
            {
                ProgressionMissionTuning t = BaseStory(battleId, questId, "마교 흑단 제단 침공", CampaignArcCatalog.Arc06DemonCultWar, 42);
                t.factionId = "demon:cult";
                t.factionConquestStageDelta = 10;
                t.factionControlDelta = 3;
                t.factionHostilityDelta = 10;
                t.masteryTags.Add("mastery:demon_slaying");
                return t;
            }

            if (battleId == "BATTLE_DEMON_CULT_LEADER")
            {
                ProgressionMissionTuning t = BaseStory(battleId, questId, "마교 교주 결전", CampaignArcCatalog.Arc07FinalConquest, 48);
                t.factionId = "demon:cult";
                t.factionConquestStageDelta = 35;
                t.factionControlDelta = 15;
                t.factionHostilityDelta = 15;
                t.masteryTags.Add("mastery:demon_slaying");
                t.masteryTags.Add("mastery:formation");
                return t;
            }

            return null;
        }

        private static ProgressionMissionTuning BaseFree(string battleId, string questId, string title, int recommendedLevel, int participationXp)
        {
            CampaignArcInfo arc = CampaignArcCatalog.EstimateByRecommendedLevel(recommendedLevel);
            ProgressionMissionTuning t = new ProgressionMissionTuning();
            t.battleId = battleId;
            t.questId = questId;
            t.displayName = title;
            t.campaignArcId = arc.id;
            t.recommendedLevel = recommendedLevel;
            t.baseParticipationXp = participationXp;
            t.contributionXpCap = Math.Max(20, arc.contributionXpCap / 2);
            t.optionalObjectiveXp = arc.optionalObjectiveXp;
            t.mvpXp = Math.Max(15, arc.mvpXp / 2);
            t.masteryAmount = 14;
            t.bondDeltaOnWin = 1;
            t.repeatable = true;
            t.consumesFreeTime = true;
            t.isStory = false;
            return t;
        }

        private static ProgressionMissionTuning BaseStory(string battleId, string questId, string title, string arcId, int recommendedLevel)
        {
            CampaignArcInfo arc = CampaignArcCatalog.GetById(arcId);
            ProgressionMissionTuning t = new ProgressionMissionTuning();
            t.battleId = battleId;
            t.questId = questId;
            t.displayName = title;
            t.campaignArcId = arc.id;
            t.recommendedLevel = recommendedLevel;
            t.baseParticipationXp = arc.storyParticipationXp;
            t.contributionXpCap = arc.contributionXpCap;
            t.optionalObjectiveXp = arc.optionalObjectiveXp;
            t.mvpXp = arc.mvpXp;
            t.masteryAmount = 24;
            t.bondDeltaOnWin = 2;
            t.repeatable = false;
            t.consumesFreeTime = false;
            t.isStory = true;
            return t;
        }

        // v1.3부터 실제 보상 참여자는 BattleDeploymentService.ResolveRewardParticipants를 사용한다.
        // 이 함수는 예전 저장/외부 도구 호환용으로만 남긴다.
        private static List<string> ResolveParticipants(BattleDefinition definition)
        {
            List<string> participants = new List<string>();
            if (definition == null || definition.roster == null)
            {
                return participants;
            }

            for (int i = 0; i < definition.roster.Count; i++)
            {
                string id = CharacterGrowthCatalog.NormalizeCharacterId(definition.roster[i]);
                if (!participants.Contains(id))
                {
                    participants.Add(id);
                }
            }

            return participants;
        }

        private static int AverageLevel(ProgressionService progression, List<string> participants)
        {
            if (participants == null || participants.Count == 0)
            {
                return 1;
            }

            int total = 0;
            for (int i = 0; i < participants.Count; i++)
            {
                total += progression.GetSnapshot(participants[i]).level;
            }

            return Math.Max(1, (int)Math.Round(total / (float)participants.Count));
        }

        private static int CountCompletedOptionalObjectives(BattleResultData result, BattleDefinition definition)
        {
            if (result == null || definition == null || result.completedObjectives == null || definition.objectives == null)
            {
                return 0;
            }

            int count = 0;
            for (int i = 0; i < definition.objectives.Count; i++)
            {
                BattleObjective objective = definition.objectives[i];
                if (objective != null && objective.optional && result.completedObjectives.Contains(objective.id))
                {
                    count++;
                }
            }

            return count;
        }

        private static int CalculateContributionXp(string characterId, BattleContributionSummary contribution, int cap, int participantCount)
        {
            if (contribution == null)
            {
                return Math.Max(0, cap / Math.Max(2, participantCount));
            }

            int score = contribution.ScoreFor(characterId);
            if (score <= 0)
            {
                return Math.Max(0, cap / 4);
            }

            return Math.Min(cap, Math.Max(5, score / 30));
        }

        private static float GetRepeatMultiplier(GameSession session, ProgressionMissionTuning tuning)
        {
            if (session == null || tuning == null)
            {
                return 1f;
            }

            string id = !string.IsNullOrEmpty(tuning.questId) ? tuning.questId : tuning.battleId;
            int day = ProgressionKeys.GetInt(session, ProgressionKeys.CalendarDay, 0);
            int todayAttempts = ProgressionKeys.GetInt(session, ProgressionKeys.DailyMissionAttempt(day, id), 0);

            if (todayAttempts <= 0) return 1f;
            if (todayAttempts == 1) return 0.50f;
            if (todayAttempts == 2) return 0.25f;
            return 0.10f;
        }

        private static float GetOverlevelMultiplier(int partyAverage, int recommendedLevel)
        {
            int diff = partyAverage - recommendedLevel;
            if (diff >= 8) return 0f;
            if (diff >= 5) return 0.15f;
            if (diff >= 3) return 0.50f;
            return 1f;
        }

        private static float GetPersonalLevelMultiplier(int characterLevel, int recommendedLevel)
        {
            int diff = recommendedLevel - characterLevel;
            float multiplier = 1f + diff * 0.10f;
            if (multiplier < 0.15f) multiplier = 0.15f;
            if (multiplier > 1.75f) multiplier = 1.75f;
            return multiplier;
        }
    }
}
