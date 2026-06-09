using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>세력 ID 상수.</summary>
    public static class FactionIds
    {
        public const string JoseonSects = "JOSEON_SECTS";          // 조선문파연합
        public const string ZhongyuanAlliance = "ZHONGYUAN_ALLIANCE"; // 중원무림맹 강경파
        public const string MurimInspectors = "MURIM_INSPECTORS";   // 무림맹 감찰단
        public const string RoyalCourt = "ROYAL_COURT";             // 조정
        public const string DemonicCult = "DEMONIC_CULT";           // 마교
        public const string BlackHatGuild = "BLACK_HAT_GUILD";      // 흑립방(사파)

        public static string Label(string factionId)
        {
            switch (factionId)
            {
                case JoseonSects: return "조선문파연합";
                case ZhongyuanAlliance: return "중원무림맹";
                case MurimInspectors: return "무림맹 감찰단";
                case RoyalCourt: return "조정";
                case DemonicCult: return "마교";
                case BlackHatGuild: return "흑립방";
                default: return factionId;
            }
        }
    }

    /// <summary>중원무림맹/조선문파연합/마교/조정/사파 등 세력 평판 관리.</summary>
    public sealed class FactionReputationService
    {
        public const int Min = -100;
        public const int Max = 100;

        private readonly GameSession session;

        public FactionReputationService(GameSession session)
        {
            this.session = session;
        }

        public int Get(string factionId)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                return 0;
            }

            return session.factionReputation.TryGetValue(factionId, out int value) ? value : 0;
        }

        public int Add(string factionId, int delta)
        {
            if (string.IsNullOrEmpty(factionId))
            {
                return 0;
            }

            int next = Mathf.Clamp(Get(factionId) + delta, Min, Max);
            session.factionReputation[factionId] = next;
            return next;
        }

        public void Set(string factionId, int value)
        {
            if (!string.IsNullOrEmpty(factionId))
            {
                session.factionReputation[factionId] = Mathf.Clamp(value, Min, Max);
            }
        }
    }
}
