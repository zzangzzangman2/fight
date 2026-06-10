using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    /// <summary>
    /// GameSession을 직접 수정하지 않기 위해 성장 저장값은 intVars/storyFlags에 보관한다.
    /// 키를 한 곳에 모아 충돌을 줄인다.
    /// </summary>
    public static class ProgressionKeys
    {
        public const string CampaignArcIndex = "campaign:arcIndex";
        public const string CalendarDay = "calendar:day";
        public const string TrainingCredit = "growth:training_credit";

        public static string Level(string characterId) { return "growth:" + characterId + ":level"; }
        public static string Xp(string characterId) { return "growth:" + characterId + ":xp"; }
        public static string TotalXp(string characterId) { return "growth:" + characterId + ":totalXp"; }
        public static string RealmTier(string characterId) { return "growth:" + characterId + ":realmTier"; }
        public static string MartialPoints(string characterId) { return "growth:" + characterId + ":mp"; }
        public static string HpBonus(string characterId) { return "growth:" + characterId + ":hpBonus"; }
        public static string InnerBonus(string characterId) { return "growth:" + characterId + ":innerBonus"; }
        public static string StatBonus(string characterId, string statKey) { return "growth:" + characterId + ":stat:" + statKey; }
        public static string GrowthMeter(string characterId, string statKey) { return "growth:" + characterId + ":meter:" + statKey; }
        public static string Mastery(string characterId, string masteryTag) { return "growth:" + characterId + ":mastery:" + masteryTag; }
        public static string MasteryMilestoneFlag(string characterId, string masteryTag, int threshold) { return "growth:" + characterId + ":mastery:" + masteryTag + ":milestone:" + threshold; }
        public static string CharacterBreakthroughFlag(string characterId, string breakthroughFlag) { return "growth:" + characterId + ":" + breakthroughFlag; }
        public static string DailyMissionAttempt(int day, string missionOrBattleId) { return "daily:" + day + ":mission:" + missionOrBattleId; }
        public static string ConquestStage(string factionId) { return "conquest:" + factionId + ":stage"; }
        public static string ConquestInfluence(string factionId) { return "conquest:" + factionId + ":influence"; }
        public static string ConquestHostility(string factionId) { return "conquest:" + factionId + ":hostility"; }
        public static string ConquestControl(string factionId) { return "conquest:" + factionId + ":control"; }
        public static string ConquestDiscoveredFlag(string factionId) { return "conquest:" + factionId + ":discovered"; }
        public static string ConquestDefeatedFlag(string factionId) { return "conquest:" + factionId + ":defeated"; }

        public static int GetInt(GameSession session, string key, int fallback)
        {
            if (session == null || session.intVars == null || string.IsNullOrEmpty(key))
            {
                return fallback;
            }

            int value;
            return session.intVars.TryGetValue(key, out value) ? value : fallback;
        }

        public static void SetInt(GameSession session, string key, int value)
        {
            if (session == null || string.IsNullOrEmpty(key))
            {
                return;
            }

            if (session.intVars == null)
            {
                return;
            }

            session.intVars[key] = value;
        }

        public static int AddInt(GameSession session, string key, int delta)
        {
            int next = GetInt(session, key, 0) + delta;
            SetInt(session, key, next);
            return next;
        }

        public static bool HasFlag(GameSession session, string flag)
        {
            return session != null && session.storyFlags != null && !string.IsNullOrEmpty(flag) && session.storyFlags.Contains(flag);
        }

        public static void SetFlag(GameSession session, string flag)
        {
            if (session == null || string.IsNullOrEmpty(flag))
            {
                return;
            }

            if (session.storyFlags == null)
            {
                return;
            }

            session.storyFlags.Add(flag);
        }

        public static string SafeId(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return "unknown";
            }

            return value.Trim().Replace(" ", "_").Replace("/", "_").Replace("\\", "_");
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }
}
