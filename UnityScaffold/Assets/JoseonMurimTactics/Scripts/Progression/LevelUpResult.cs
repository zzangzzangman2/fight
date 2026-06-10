using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    public enum ProgressionXpSourceType
    {
        Story,
        FreeBattle,
        Training,
        CatchUp,
        Debug
    }

    [Serializable]
    public sealed class MasteryGainResult
    {
        public string characterId;
        public string masteryTag;
        public int oldValue;
        public int newValue;
        public int appliedAmount;
        public List<int> crossedMilestones = new List<int>();
    }

    [Serializable]
    public sealed class LevelUpResult
    {
        public string characterId;
        public string displayName;
        public int oldLevel;
        public int newLevel;
        public string oldRealmName;
        public string newRealmName;
        public bool realmChanged;
        public bool blocked;
        public string blockedReason;
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
        public List<string> unlockedPassiveIds = new List<string>();
        public List<string> notes = new List<string>();

        public int TotalStatGain
        {
            get
            {
                return hpBonus + innerBonus + strengthBonus + agilityBonus + innerPowerBonus + spiritBonus + insightBonus + charmBonus + martialPointBonus;
            }
        }

        public string ToSummaryString()
        {
            if (blocked)
            {
                return displayName + " 성장 정체: " + blockedReason + " / 수련치 +" + convertedTrainingCredit;
            }

            string realm = realmChanged ? " / " + oldRealmName + "→" + newRealmName : string.Empty;
            return displayName + " Lv." + oldLevel + "→" + newLevel + realm;
        }
    }
}
