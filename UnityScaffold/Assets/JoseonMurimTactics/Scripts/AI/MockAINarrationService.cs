using System.Collections.Generic;

namespace JoseonMurimTactics
{
    /// <summary>
    /// v0.8용 고정 문장 반환 구현. 입력에 따라 약간 변주하지만 외부 호출은 없다.
    /// </summary>
    public sealed class MockAINarrationService : IAINarrationService
    {
        public string GenerateRumor(BattleResultData result)
        {
            if (result == null)
            {
                return "변방에서 작은 소동이 있었다 하나, 자세한 것은 알 수 없다.";
            }

            if (result.Won)
            {
                string boss = string.IsNullOrEmpty(result.defeatedBoss) ? "중원 감찰사" : result.defeatedBoss;
                return $"의주 폐사당에서 감찰단의 현판령이 깨졌다. 조선 변방의 신생 문주 박성준이 {boss}에게 검을 겨누었다 한다.";
            }

            return "폐사당의 불빛이 꺼졌다. 신생 조선 문파가 첫 싸움에서 물러섰다는 소문이 강을 따라 흐른다.";
        }

        public string GenerateNpcLine(string npcId, GameSession session)
        {
            List<string> lines = new List<string>
            {
                "“요즘 강 건너에서 감찰단 깃발이 부쩍 늘었습디다.”",
                "“문주께서 여인 고수만 거두신다는 소문이 벌써 의주까지 닿았소.”",
                "“현판을 지킨 문파가 있다더군. 사실이라면 큰일이오.”"
            };

            int index = 0;
            if (!string.IsNullOrEmpty(npcId))
            {
                index = (npcId.Length + (session != null ? session.actionsTaken : 0)) % lines.Count;
            }

            return lines[index];
        }

        public string GenerateCompanionReaction(string companionId, string eventId)
        {
            switch (companionId)
            {
                case "yun_seohwa":
                    return "윤서화가 검집을 매만지며 짧게 고개를 끄덕인다.";
                case "baek_ryeon":
                    return "백련이 다친 이의 곁을 떠나지 않은 채 당신을 바라본다.";
                case "han_biyeon":
                    return "한비연이 입꼬리를 올리며 “제법인데?”라고 흘린다.";
                default:
                    return "동료가 말없이 당신의 결정을 가늠한다.";
            }
        }
    }
}
