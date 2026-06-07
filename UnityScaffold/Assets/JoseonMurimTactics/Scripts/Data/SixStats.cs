using System;

namespace JoseonMurimTactics
{
    [Serializable]
    public struct SixStats
    {
        public int strength;
        public int agility;
        public int innerPower;
        public int spirit;
        public int insight;
        public int charm;

        public int Get(StatType stat)
        {
            switch (stat)
            {
                case StatType.Strength:
                    return strength;
                case StatType.Agility:
                    return agility;
                case StatType.InnerPower:
                    return innerPower;
                case StatType.Spirit:
                    return spirit;
                case StatType.Insight:
                    return insight;
                case StatType.Charm:
                    return charm;
                default:
                    return 10;
            }
        }

        public int Modifier(StatType stat)
        {
            return (Get(stat) - 10) / 2;
        }
    }
}
