using System;

namespace JoseonMurimTactics
{
    /// <summary>
    /// BattleResultController가 결과를 정산한 직후 같은 BattleResult 씬에서 성장 그래프가 읽는 런타임 캐시.
    /// 저장 데이터가 아니며, 실제 성장 저장은 ProgressionService가 GameSession.intVars에 반영한다.
    /// </summary>
    public static class ProgressionRewardMemory
    {
        private static string lastResultId;
        private static string lastBattleId;
        private static RewardBundle lastBundle;
        private static float storedAtRealtime;

        public static void Store(BattleResultData result, RewardBundle bundle)
        {
            if (result == null || bundle == null)
            {
                return;
            }

            lastResultId = result.EnsureResultId();
            lastBattleId = result.battleId;
            lastBundle = bundle;
            storedAtRealtime = UnityEngine.Time.realtimeSinceStartup;
        }

        public static bool TryGet(BattleResultData result, out RewardBundle bundle)
        {
            bundle = null;
            if (result == null || lastBundle == null)
            {
                return false;
            }

            string id = result.EnsureResultId();
            if (string.Equals(id, lastResultId, StringComparison.Ordinal))
            {
                bundle = lastBundle;
                return true;
            }

            return false;
        }

        public static bool TryGetLatest(out RewardBundle bundle)
        {
            bundle = lastBundle;
            return bundle != null;
        }

        public static bool IsFresh(float seconds)
        {
            if (lastBundle == null)
            {
                return false;
            }

            return UnityEngine.Time.realtimeSinceStartup - storedAtRealtime <= seconds;
        }

        public static string LastResultId
        {
            get { return lastResultId; }
        }

        public static string LastBattleId
        {
            get { return lastBattleId; }
        }
    }
}
