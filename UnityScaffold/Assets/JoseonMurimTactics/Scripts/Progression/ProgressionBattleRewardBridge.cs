using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    /// <summary>
    /// BattleResultApplyService에 한 줄로 연결하는 성장 보상 브리지.
    /// 기존 은전/아이템/평판 지급은 BattleResultApplyService가 이미 처리하므로 여기서 중복하지 않는다.
    /// v1.3: 실제 출진 파티만 성장시키고, 결과 화면 성장 그래프용 before/after 값을 RewardBundle에 기록한다.
    /// </summary>
    public static class ProgressionBattleRewardBridge
    {
        public static RewardBundle Apply(GameRoot root, BattleResultData result, BattleDefinition definition, bool baseReplayRewardsReduced)
        {
            if (root == null || root.Session == null || result == null)
            {
                return new RewardBundle();
            }

            BattleDeploymentService.EnsureDefaultStored(root.Session, definition);

            RewardBundle bundle = MissionRewardResolver.Resolve(root.Session, result, definition, null);
            bundle.repeatRewardsReduced = baseReplayRewardsReduced;
            bundle.replayMultiplier = baseReplayRewardsReduced ? 0.25f : 1f;

            ProgressionService progression = new ProgressionService(root.Session);
            progression.EnsureCorePartyInitialized();
            SupportBondService bonds = new SupportBondService(root.Session);
            FactionConquestService conquests = new FactionConquestService(root.Session);

            float replayMultiplier = bundle.replayMultiplier;
            ProgressionXpSourceType sourceType = IsFreeBattle(definition) ? ProgressionXpSourceType.FreeBattle : ProgressionXpSourceType.Story;

            for (int i = 0; i < bundle.characterRewards.Count; i++)
            {
                CharacterReward characterReward = bundle.characterRewards[i];
                characterReward.deployed = true;

                CharacterProgressState before = progression.GetSnapshot(characterReward.characterId);
                characterReward.CaptureBefore(before);

                int xp = Math.Max(0, (int)Math.Round(characterReward.finalXp * replayMultiplier));
                characterReward.finalXp = xp;
                characterReward.appliedXp = xp;

                List<LevelUpResult> levelUps = progression.AddXp(characterReward.characterId, xp, sourceType, true);
                characterReward.levelUps.AddRange(levelUps);
                for (int levelIndex = 0; levelIndex < levelUps.Count; levelIndex++)
                {
                    characterReward.AbsorbLevelUp(levelUps[levelIndex]);
                }

                int masteryAmount = Math.Max(0, (int)Math.Round(characterReward.masteryAmount * replayMultiplier));
                characterReward.masteryAmount = masteryAmount;
                for (int tagIndex = 0; tagIndex < characterReward.masteryTags.Count; tagIndex++)
                {
                    MasteryGainResult mastery = progression.AddMastery(characterReward.characterId, characterReward.masteryTags[tagIndex], masteryAmount);
                    if (mastery.appliedAmount > 0 || mastery.crossedMilestones.Count > 0)
                    {
                        characterReward.masteryGains.Add(mastery);
                        characterReward.totalMasteryApplied += Math.Max(0, mastery.appliedAmount);
                    }
                }

                if (characterReward.bondDelta != 0 && result.Won)
                {
                    bonds.AddSafeBattleBond(characterReward.characterId, characterReward.bondDelta);
                }

                CharacterProgressState after = progression.GetSnapshot(characterReward.characterId);
                characterReward.CaptureAfter(after);
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
            ProgressionRewardMemory.Store(result, bundle);
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

                string itemId = InventoryService.NormalizeItemId(material.id);
                if (string.IsNullOrEmpty(itemId))
                {
                    continue;
                }

                int oldValue;
                session.inventory.TryGetValue(itemId, out oldValue);
                session.inventory[itemId] = oldValue + material.delta;
            }
        }

        private static void AppendSummaryFlags(BattleResultData result, RewardBundle bundle)
        {
            if (result == null || result.specialFlags == null || bundle == null)
            {
                return;
            }

            AddFlag(result, "progression:출진 " + bundle.deployedMemberCount + "명만 성장 적용 / 비출진 동료 XP 없음");
            AddFlag(result, "progression:경험치 보상 적용 / 평균 Lv." + bundle.partyAverageLevel + " / 권장 Lv." + bundle.recommendedLevel);

            if (bundle.repeatMultiplier < 1f)
            {
                AddFlag(result, "progression:반복 토벌 보상 감쇠 x" + bundle.repeatMultiplier.ToString("0.##"));
            }

            if (bundle.overlevelMultiplier < 1f)
            {
                AddFlag(result, "progression:오버레벨 보상 감쇠 x" + bundle.overlevelMultiplier.ToString("0.##"));
            }

            if (bundle.replayMultiplier < 1f)
            {
                AddFlag(result, "progression:재전투 성장 보상 x" + bundle.replayMultiplier.ToString("0.##"));
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
                AddFlag(result, "progression:" + BuildXpSummary(reward));

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

        private static string BuildXpSummary(CharacterReward reward)
        {
            if (reward == null)
            {
                return string.Empty;
            }

            string before = reward.beforeXpToNext <= 0 ? "MAX" : reward.beforeXp + "/" + reward.beforeXpToNext;
            string after = reward.afterXpToNext <= 0 ? "MAX" : reward.afterXp + "/" + reward.afterXpToNext;
            return reward.displayName + " XP +" + reward.appliedXp + " · Lv." + reward.beforeLevel + " " + before + " → Lv." + reward.afterLevel + " " + after;
        }

        private static void AddFlag(BattleResultData result, string flag)
        {
            if (string.IsNullOrEmpty(flag) || result.specialFlags.Contains(flag))
            {
                return;
            }

            result.specialFlags.Add(flag);
        }
    }
}
