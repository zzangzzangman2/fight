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
        public int baseXp;
        public int finalXp;
        public int masteryAmount;
        public int bondDelta;
        public List<string> masteryTags = new List<string>();
        public List<LevelUpResult> levelUps = new List<LevelUpResult>();
        public List<MasteryGainResult> masteryGains = new List<MasteryGainResult>();
    }

    [Serializable]
    public sealed class RewardBundle
    {
        public string battleId;
        public string questId;
        public string campaignArcId;
        public string factionId;
        public int recommendedLevel;
        public int partyAverageLevel;
        public bool won;
        public bool repeatRewardsReduced;
        public bool overleveled;
        public float repeatMultiplier = 1f;
        public float overlevelMultiplier = 1f;
        public float resultMultiplier = 1f;
        public int trainingCreditBonus;
        public int factionConquestStageDelta;
        public int factionControlDelta;
        public int factionHostilityDelta;
        public List<RewardDelta> materialRewards = new List<RewardDelta>();
        public List<CharacterReward> characterRewards = new List<CharacterReward>();
        public List<string> summaryLines = new List<string>();
    }
}
