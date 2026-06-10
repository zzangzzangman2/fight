using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class ProgressionIntPair
    {
        public string key;
        public int value;

        public ProgressionIntPair() { }

        public ProgressionIntPair(string key, int value)
        {
            this.key = key;
            this.value = value;
        }
    }

    /// <summary>
    /// 저장 DTO가 아니라 UI/디버그용 스냅샷이다.
    /// 실제 저장은 GameSession.intVars/storyFlags에 들어간다.
    /// </summary>
    [Serializable]
    public sealed class CharacterProgressState
    {
        public string characterId;
        public string displayName;
        public int level = 1;
        public int xp;
        public int xpToNext;
        public int totalXp;
        public int realmTier;
        public string realmId;
        public string realmName;
        public int martialPoints;
        public int hpBonus;
        public int innerBonus;
        public SixStats statBonuses;
        public List<ProgressionIntPair> growthMeters = new List<ProgressionIntPair>();
        public List<ProgressionIntPair> mastery = new List<ProgressionIntPair>();

        public bool IsMaxLevel
        {
            get { return level >= XpTable.MaxLevel; }
        }

        public float XpProgress01
        {
            get { return XpTable.GetProgress01(level, xp); }
        }
    }
}
