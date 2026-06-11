using System;
using System.Collections.Generic;

namespace JoseonMurimTactics
{
    public static class SupportBondRanks
    {
        public const int C = 20;
        public const int B = 45;
        public const int A = 75;
        public const int S = 100;

        public static string RankFor(int value)
        {
            if (value >= S) return "S";
            if (value >= A) return "A";
            if (value >= B) return "B";
            if (value >= C) return "C";
            return "-";
        }
    }

    /// <summary>
    /// 동료 보상은 호감도/신뢰/지원 효과로 처리한다.
    /// 로맨스 연출은 CanReceiveRomanticEffects로 연애 가능 여부를 확인한 뒤에만 적용한다.
    /// (기획 결정: 연애 연출은 나이 데이터와 무관하게 항상 허용 — CanReceiveRomanticEffects는 상시 true.)
    /// </summary>
    public sealed class SupportBondService
    {
        private readonly GameSession session;

        public SupportBondService(GameSession session)
        {
            this.session = session;
        }

        public int AddBond(string companionId, int delta)
        {
            if (session == null || session.companionApproval == null || string.IsNullOrEmpty(companionId) || delta == 0)
            {
                return GetBond(companionId);
            }

            int current = GetBond(companionId);
            int next = ProgressionKeys.Clamp(current + delta, -100, 100);
            session.companionApproval[companionId] = next;
            return next;
        }

        public int GetBond(string companionId)
        {
            if (session == null || session.companionApproval == null || string.IsNullOrEmpty(companionId))
            {
                return 0;
            }

            int value;
            return session.companionApproval.TryGetValue(companionId, out value) ? value : 0;
        }

        public string GetRank(string companionId)
        {
            return SupportBondRanks.RankFor(GetBond(companionId));
        }

        public bool CanReceiveRomanticEffects(string companionId)
        {
            CompanionInfo info = CompanionCatalog.Info(companionId);
            return info != null && info.CanReceiveRomanticEffects;
        }

        public void AddSafeBattleBond(string characterId, int delta)
        {
            string id = CharacterGrowthCatalog.NormalizeCharacterId(characterId);
            if (id == CharacterGrowthCatalog.ProtagonistId)
            {
                return;
            }

            AddBond(id, delta);
        }

        public string BuildSupportSummary(string companionId)
        {
            int value = GetBond(companionId);
            return CharacterGrowthCatalog.DisplayName(companionId) + " 유대 " + value + " / 등급 " + SupportBondRanks.RankFor(value);
        }
    }
}
