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
            return GenerateRumorData(npcId, session).rumorText;
        }

        public RumorData GenerateRumorData(string npcId, GameSession session)
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

            return new RumorData
            {
                rumorText = lines[index],
                relatedFaction = index == 0 ? FactionIds.MurimInspectors : FactionIds.JoseonSects,
                missionHintId = index == 2 ? "MISSION_UIJU_TAVERN_LEAD" : string.Empty,
                dangerLevel = index == 0 ? 2 : 1,
                unlockFlag = index == 2 ? "FLAG_RUMOR_PYESADANG_SPREAD" : string.Empty
            };
        }

        public string GenerateCompanionReaction(string companionId, string eventId)
        {
            switch (companionId)
            {
                case "baek_ryeon":
                    return "백련이 창대를 조용히 세우고 주변의 부상자를 먼저 살핀다.";
                case "do_arin":
                    return "도아린이 도집을 툭 치며 “먼저 치면 되지?”라고 웃는다.";
                case "seo_a":
                    return "서아가 봉끝에 튄 잔전기를 보고 눈을 반짝인다. “봤죠? 방금 봤죠?”";
                case "mae_hwaryeong":
                    return "매화령이 부채를 접으며 꽃잎처럼 가볍게 미소 짓는다.";
                case "han_biyeon":
                    return "한비연이 그림자 쪽으로 한 걸음 물러나며 “제법인데?”라고 흘린다.";
                default:
                    return "동료가 말없이 당신의 결정을 가늠한다.";
            }
        }
    }
}
