using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    /// <summary>
    /// BattleResultApplyService에 한 줄로 연결하는 성장 보상 브리지.
    /// 기존 은전/아이템/평판 지급은 BattleResultApplyService가 이미 처리하므로 여기서 중복하지 않는다.
    /// </summary>
    public static class ProgressionBattleRewardBridge
    {
        public static RewardBundle Apply(GameRoot root, BattleResultData result, BattleDefinition definition, bool baseReplayRewardsReduced)
        {
            if (root == null || root.Session == null || result == null)
            {
                return new RewardBundle();
            }

            RewardBundle bundle = MissionRewardResolver.Resolve(root.Session, result, definition, null);
            bundle.repeatRewardsReduced = baseReplayRewardsReduced;

            ProgressionService progression = new ProgressionService(root.Session);
            progression.EnsureCorePartyInitialized();
            SupportBondService bonds = new SupportBondService(root.Session);
            FactionConquestService conquests = new FactionConquestService(root.Session);

            float replayMultiplier = baseReplayRewardsReduced ? 0.25f : 1f;
            ProgressionXpSourceType sourceType = IsFreeBattle(definition) ? ProgressionXpSourceType.FreeBattle : ProgressionXpSourceType.Story;

            for (int i = 0; i < bundle.characterRewards.Count; i++)
            {
                CharacterReward characterReward = bundle.characterRewards[i];
                int xp = Math.Max(0, (int)Math.Round(characterReward.finalXp * replayMultiplier));
                characterReward.finalXp = xp;

                List<LevelUpResult> levelUps = progression.AddXp(characterReward.characterId, xp, sourceType, true);
                characterReward.levelUps.AddRange(levelUps);

                int masteryAmount = Math.Max(0, (int)Math.Round(characterReward.masteryAmount * replayMultiplier));
                for (int tagIndex = 0; tagIndex < characterReward.masteryTags.Count; tagIndex++)
                {
                    MasteryGainResult mastery = progression.AddMastery(characterReward.characterId, characterReward.masteryTags[tagIndex], masteryAmount);
                    if (mastery.appliedAmount > 0 || mastery.crossedMilestones.Count > 0)
                    {
                        characterReward.masteryGains.Add(mastery);
                    }
                }

                if (characterReward.bondDelta != 0 && result.Won)
                {
                    bonds.AddSafeBattleBond(characterReward.characterId, characterReward.bondDelta);
                }
            }

            ApplyExtraMaterials(root.Session, bundle);

            if (!string.IsNullOrEmpty(bundle.factionId) && (bundle.factionConquestStageDelta != 0 || bundle.factionControlDelta != 0 || bundle.factionHostilityDelta != 0))
            {
                FactionConquestState state = conquests.ApplyBattleReward(bundle.factionId, bundle.factionConquestStageDelta, bundle.factionControlDelta, bundle.factionHostilityDelta, true);
                if (state != null)
                {
                    bundle.summaryLines.Add(state.displayName + " 정복도 " + state.stage + "/100");
                }
            }

            MissionRewardResolver.RecordDailyAttempt(root.Session, bundle);
            AppendSummaryFlags(result, bundle);
            return bundle;
        }

        private static bool IsFreeBattle(BattleDefinition definition)
        {
            return definition != null && definition.repeatable;
        }

        private static void ApplyExtraMaterials(GameSession session, RewardBundle bundle)
        {
            if (session == null || session.inventory == null || bundle == null || bundle.materialRewards == null)
            {
                return;
            }

            for (int i = 0; i < bundle.materialRewards.Count; i++)
            {
                RewardDelta material = bundle.materialRewards[i];
                if (material == null || string.IsNullOrEmpty(material.id) || material.delta == 0)
                {
                    continue;
                }

                int oldValue;
                session.inventory.TryGetValue(material.id, out oldValue);
                session.inventory[material.id] = oldValue + material.delta;
            }
        }

        private static void AppendSummaryFlags(BattleResultData result, RewardBundle bundle)
        {
            if (result == null || result.specialFlags == null || bundle == null)
            {
                return;
            }

            AddFlag(result, "progression:경험치 보상 적용 / 평균 Lv." + bundle.partyAverageLevel + " / 권장 Lv." + bundle.recommendedLevel);
            if (bundle.repeatMultiplier < 1f)
            {
                AddFlag(result, "progression:반복 토벌 보상 감쇠 x" + bundle.repeatMultiplier.ToString("0.##"));
            }

            if (bundle.overlevelMultiplier < 1f)
            {
                AddFlag(result, "progression:오버레벨 보상 감쇠 x" + bundle.overlevelMultiplier.ToString("0.##"));
            }

            for (int i = 0; i < bundle.summaryLines.Count; i++)
            {
                AddFlag(result, "progression:" + bundle.summaryLines[i]);
            }

            int levelUpCount = 0;
            int blockedCount = 0;
            for (int i = 0; i < bundle.characterRewards.Count; i++)
            {
                CharacterReward reward = bundle.characterRewards[i];
                for (int j = 0; j < reward.levelUps.Count; j++)
                {
                    LevelUpResult levelUp = reward.levelUps[j];
                    if (levelUp.blocked)
                    {
                        blockedCount++;
                        AddFlag(result, "progression:" + levelUp.ToSummaryString());
                    }
                    else
                    {
                        levelUpCount++;
                        AddFlag(result, "progression:" + levelUp.ToSummaryString());
                    }
                }

                for (int j = 0; j < reward.masteryGains.Count; j++)
                {
                    MasteryGainResult mastery = reward.masteryGains[j];
                    if (mastery.crossedMilestones.Count > 0)
                    {
                        AddFlag(result, "progression:" + CharacterGrowthCatalog.DisplayName(mastery.characterId) + " " + mastery.masteryTag + " 숙련 " + mastery.newValue + "/1000");
                    }
                }
            }

            if (levelUpCount <= 0 && blockedCount <= 0)
            {
                AddFlag(result, "progression:레벨업 없음, XP/숙련도 누적");
            }
        }

        private static void AddFlag(BattleResultData result, string flag)
        {
            if (result.specialFlags.Contains(flag))
            {
                return;
            }

            result.specialFlags.Add(flag);
        }
    }
}
