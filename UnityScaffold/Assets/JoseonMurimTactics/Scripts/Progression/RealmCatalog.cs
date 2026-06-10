using System;

namespace JoseonMurimTactics
{
    [Serializable]
    public sealed class RealmInfo
    {
        public int tier;
        public string id;
        public string displayName;
        public int minLevel;
        public int maxLevel;
        public bool requiresBreakthrough;
        public string breakthroughFlag;
        public string description;

        public bool ContainsLevel(int level)
        {
            return level >= minLevel && level <= maxLevel;
        }
    }

    /// <summary>
    /// 무공 경지: 삼류 → 이류 → 일류 → 절정 → 초절정 → 화경 → 현경 → 생사경.
    /// </summary>
    public static class RealmCatalog
    {
        public static readonly RealmInfo[] Realms =
        {
            new RealmInfo { tier = 0, id = "realm:samryu", displayName = "삼류", minLevel = 1, maxLevel = 6, requiresBreakthrough = false, breakthroughFlag = string.Empty, description = "입문 무인. 기본기와 생존을 배운다." },
            new RealmInfo { tier = 1, id = "realm:iryu", displayName = "이류", minLevel = 7, maxLevel = 12, requiresBreakthrough = false, breakthroughFlag = string.Empty, description = "실전 무인. 하급 토벌을 안정적으로 수행한다." },
            new RealmInfo { tier = 2, id = "realm:ilryu", displayName = "일류", minLevel = 13, maxLevel = 18, requiresBreakthrough = false, breakthroughFlag = string.Empty, description = "지역 명성권. 문파 재건의 중심 전력이 된다." },
            new RealmInfo { tier = 3, id = "realm:jeoljeong", displayName = "절정", minLevel = 19, maxLevel = 25, requiresBreakthrough = true, breakthroughFlag = "breakthrough:jeoljeong", description = "진짜 고수의 문턱. 구파일방 외당과 겨룰 수 있다." },
            new RealmInfo { tier = 4, id = "realm:chojeoljeong", displayName = "초절정", minLevel = 26, maxLevel = 32, requiresBreakthrough = true, breakthroughFlag = "breakthrough:chojeoljeong", description = "대문파 핵심 전력. 오대세가 장로와 맞선다." },
            new RealmInfo { tier = 5, id = "realm:hwa", displayName = "화경", minLevel = 33, maxLevel = 39, requiresBreakthrough = true, breakthroughFlag = "breakthrough:hwa", description = "무공을 몸에 녹여낸 경지. 장문인급 전투가 가능하다." },
            new RealmInfo { tier = 6, id = "realm:hyeon", displayName = "현경", minLevel = 40, maxLevel = 45, requiresBreakthrough = true, breakthroughFlag = "breakthrough:hyeon", description = "무림 판도를 움직이는 초월급 고수." },
            new RealmInfo { tier = 7, id = "realm:saengsa", displayName = "생사경", minLevel = 46, maxLevel = 50, requiresBreakthrough = true, breakthroughFlag = "breakthrough:saengsa", description = "생사관을 넘어선 결전 경지. 마교 최종전용." }
        };

        public static RealmInfo GetByLevel(int level)
        {
            int safe = Math.Max(1, Math.Min(XpTable.MaxLevel, level));
            for (int i = 0; i < Realms.Length; i++)
            {
                if (Realms[i].ContainsLevel(safe))
                {
                    return Realms[i];
                }
            }

            return Realms[0];
        }

        public static RealmInfo GetByTier(int tier)
        {
            for (int i = 0; i < Realms.Length; i++)
            {
                if (Realms[i].tier == tier)
                {
                    return Realms[i];
                }
            }

            return Realms[0];
        }

        public static bool IsRealmEntryLevel(int level)
        {
            RealmInfo realm = GetByLevel(level);
            return realm != null && realm.minLevel == level;
        }
    }
}
