using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class RewardDelta
    {
        public string id;
        public int delta;

        public RewardDelta() { }

        public RewardDelta(string id, int delta)
        {
            this.id = id;
            this.delta = delta;
        }
    }

    [Serializable]
    public sealed class CharacterReward
    {
        public string characterId;
        public string displayName;
        public bool deployed;
        public int baseXp;
        public int finalXp;
        public int appliedXp;
        public int masteryAmount;
        public int totalMasteryApplied;
        public int bondDelta;

        public int beforeLevel;
        public int afterLevel;
        public int beforeXp;
        public int afterXp;
        public int beforeXpToNext;
        public int afterXpToNext;
        public string beforeRealmName;
        public string afterRealmName;
        public bool realmChanged;
        public bool blockedByCap;
        public int convertedTrainingCredit;

        public int hpBonus;
        public int innerBonus;
        public int strengthBonus;
        public int agilityBonus;
        public int innerPowerBonus;
        public int spiritBonus;
        public int insightBonus;
        public int charmBonus;
        public int martialPointBonus;

        public List<string> masteryTags = new List<string>();
        public List<LevelUpResult> levelUps = new List<LevelUpResult>();
        public List<MasteryGainResult> masteryGains = new List<MasteryGainResult>();
        public List<string> growthNotes = new List<string>();

        public int LevelGain
        {
            get { return Math.Max(0, afterLevel - beforeLevel); }
        }

        public bool LeveledUp
        {
            get { return LevelGain > 0; }
        }

        public bool HasVisibleGrowth
        {
            get
            {
                return appliedXp > 0 || LeveledUp || TotalStatGain > 0 || totalMasteryApplied > 0 || bondDelta != 0 || blockedByCap;
            }
        }

        public int TotalStatGain
        {
            get
            {
                return hpBonus + innerBonus + strengthBonus + agilityBonus + innerPowerBonus + spiritBonus + insightBonus + charmBonus + martialPointBonus;
            }
        }

        public float BeforeProgress01
        {
            get
            {
                if (beforeXpToNext <= 0) return 1f;
                return Math.Max(0f, Math.Min(1f, beforeXp / (float)beforeXpToNext));
            }
        }

        public float AfterProgress01
        {
            get
            {
                if (afterXpToNext <= 0) return 1f;
                return Math.Max(0f, Math.Min(1f, afterXp / (float)afterXpToNext));
            }
        }

        public void CaptureBefore(CharacterProgressState state)
        {
            if (state == null)
            {
                return;
            }

            beforeLevel = state.level;
            beforeXp = state.xp;
            beforeXpToNext = state.xpToNext;
            beforeRealmName = state.realmName;
        }

        public void CaptureAfter(CharacterProgressState state)
        {
            if (state == null)
            {
                return;
            }

            afterLevel = state.level;
            afterXp = state.xp;
            afterXpToNext = state.xpToNext;
            afterRealmName = state.realmName;
            realmChanged = !string.Equals(beforeRealmName, afterRealmName, StringComparison.Ordinal);
        }

        public void AbsorbLevelUp(LevelUpResult result)
        {
            if (result == null)
            {
                return;
            }

            if (result.blocked)
            {
                blockedByCap = true;
                convertedTrainingCredit += Math.Max(0, result.convertedTrainingCredit);
                if (!string.IsNullOrEmpty(result.blockedReason))
                {
                    growthNotes.Add(result.blockedReason);
                }
                return;
            }

            hpBonus += Math.Max(0, result.hpBonus);
            innerBonus += Math.Max(0, result.innerBonus);
            strengthBonus += Math.Max(0, result.strengthBonus);
            agilityBonus += Math.Max(0, result.agilityBonus);
            innerPowerBonus += Math.Max(0, result.innerPowerBonus);
            spiritBonus += Math.Max(0, result.spiritBonus);
            insightBonus += Math.Max(0, result.insightBonus);
            charmBonus += Math.Max(0, result.charmBonus);
            martialPointBonus += Math.Max(0, result.martialPointBonus);

            if (result.realmChanged)
            {
                realmChanged = true;
            }

            for (int i = 0; i < result.notes.Count; i++)
            {
                if (!string.IsNullOrEmpty(result.notes[i]))
                {
                    growthNotes.Add(result.notes[i]);
                }
            }
        }
    }

    [Serializable]
    public sealed class RewardBundle
    {
        public string battleId;
        public string questId;
        public string campaignArcId;
        public string factionId;
        public string deploymentBattleId;
        public int recommendedLevel;
        public int partyAverageLevel;
        public int deployedMemberCount;
        public bool usedExplicitDeployment;
        public bool won;
        public bool repeatRewardsReduced;
        public bool overleveled;
        public float repeatMultiplier = 1f;
        public float overlevelMultiplier = 1f;
        public float resultMultiplier = 1f;
        public float replayMultiplier = 1f;
        public int trainingCreditBonus;
        public int factionConquestStageDelta;
        public int factionControlDelta;
        public int factionHostilityDelta;
        public List<RewardDelta> materialRewards = new List<RewardDelta>();
        public List<CharacterReward> characterRewards = new List<CharacterReward>();
        public List<string> summaryLines = new List<string>();
    }
}
