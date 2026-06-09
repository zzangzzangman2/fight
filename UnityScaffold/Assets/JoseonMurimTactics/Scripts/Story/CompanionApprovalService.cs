using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>
    /// 대화/전투/선택에 따른 동료 승인도 변경과 단계 판정. 0~100 척도, 기본 50(보통).
    /// 단계 경계에서 개인 이벤트 해금 플래그를 세울 수 있다.
    /// </summary>
    public sealed class CompanionApprovalService
    {
        public const int Min = 0;
        public const int Max = 100;
        public const int Default = 50;

        private readonly GameSession session;

        public CompanionApprovalService(GameSession session)
        {
            this.session = session;
        }

        public int Get(string companionId)
        {
            if (string.IsNullOrEmpty(companionId))
            {
                return Default;
            }

            return session.companionApproval.TryGetValue(companionId, out int value) ? value : Default;
        }

        /// <summary>승인도를 delta만큼 변경하고 변화 후 값을 반환.</summary>
        public int Add(string companionId, int delta)
        {
            if (string.IsNullOrEmpty(companionId))
            {
                return Default;
            }

            int next = Mathf.Clamp(Get(companionId) + delta, Min, Max);
            session.companionApproval[companionId] = next;
            return next;
        }

        public ApprovalStage GetStage(string companionId)
        {
            return StageOf(Get(companionId));
        }

        public static ApprovalStage StageOf(int value)
        {
            if (value < 20) return ApprovalStage.Distrust;
            if (value < 40) return ApprovalStage.Wary;
            if (value < 60) return ApprovalStage.Neutral;
            if (value < 80) return ApprovalStage.Trust;
            return ApprovalStage.Comrade;
        }

        public string GetStageLabel(string companionId)
        {
            return StoryEnumLabels.Label(GetStage(companionId));
        }
    }
}
