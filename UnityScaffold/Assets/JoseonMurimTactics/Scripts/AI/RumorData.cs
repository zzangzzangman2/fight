using System;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class RumorData
    {
        public string rumorText;
        public string relatedFaction;
        public string missionHintId;
        public int dangerLevel;
        public string unlockFlag;
    }
}
