using System;

namespace JoseonMurimTactics
{
    /// <summary>
    /// Lv 1~50 장기 캠페인용 경험치 테이블.
    /// 값은 현재 레벨에서 다음 레벨까지 필요한 XP다.
    /// Lv 50은 최종 레벨이므로 다음 필요 XP가 없다.
    /// </summary>
    public static class XpTable
    {
        public const int MaxLevel = 50;
        public const int TotalXpToMax = 61735;

        private static readonly int[] xpToNext =
        {
            0,
            90, 100, 110, 120, 130, 145,
            160, 175, 190, 210, 230, 250,
            275, 300, 325, 350, 380, 410,
            445, 480, 520, 560, 600, 650, 700,
            760, 820, 890, 960, 1040, 1120, 1210,
            1310, 1420, 1540, 1670, 1810, 1960, 2120,
            2300, 2500, 2720, 2960, 3220, 3500,
            3850, 4250, 4700, 5200
        };

        public static int GetXpToNext(int level)
        {
            if (level < 1)
            {
                return xpToNext[1];
            }

            if (level >= MaxLevel)
            {
                return 0;
            }

            return xpToNext[level];
        }

        public static int GetTotalXpRequiredForLevel(int targetLevel)
        {
            int clamped = Math.Max(1, Math.Min(MaxLevel, targetLevel));
            int total = 0;
            for (int level = 1; level < clamped; level++)
            {
                total += GetXpToNext(level);
            }

            return total;
        }

        public static int GetLevelFromTotalXp(int totalXp)
        {
            int safe = Math.Max(0, totalXp);
            int running = 0;
            for (int level = 1; level < MaxLevel; level++)
            {
                running += GetXpToNext(level);
                if (safe < running)
                {
                    return level;
                }
            }

            return MaxLevel;
        }

        public static float GetProgress01(int level, int currentXp)
        {
            int needed = GetXpToNext(level);
            if (needed <= 0)
            {
                return 1f;
            }

            return Math.Max(0f, Math.Min(1f, currentXp / (float)needed));
        }
    }
}
