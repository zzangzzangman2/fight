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
            string boss = string.IsNullOrEmpty(result.defeatedBoss) ? "철랑문 정찰조장" : result.defeatedBoss;
            return $"백두산 소백촌 길목의 검은 표식이 드러났다. 박성준이 {boss}을(를) 물리치고 백두천광검문의 이름을 다시 세웠다 한다.";
        }

        return "백두산 길목의 불빛이 흐려졌다. 철랑문이 다시 소백촌을 노린다는 소문이 산 아래로 번진다.";
    }

    public string GenerateNpcLine(string npcId, GameSession session)
    {
        return GenerateRumorData(npcId, session).rumorText;
    }

    public RumorData GenerateRumorData(string npcId, GameSession session)
    {
        List<string> lines = new List<string> { "“요즘 백두산 북쪽 길목에 낯선 늑대 문양이 찍힌다더군.”",
                                                "“성준 도련님이 또 농담만 하다 끝날 줄 알았는데, 이번엔 눈빛이 다르다더이다.”",
                                                "“백두천광검문이 다시 일어서면 소백촌도 숨통이 트이겠지요.”" };

        int index = 0;
        if (!string.IsNullOrEmpty(npcId))
        {
            index = (npcId.Length + (session != null ? session.actionsTaken : 0)) % lines.Count;
        }

        return new RumorData { rumorText = lines[index],
                               relatedFaction = index == 0 ? FactionIds.MurimInspectors : FactionIds.JoseonSects,
                               missionHintId = index == 2 ? "MISSION_CH01_BLACK_MARK" : string.Empty,
                               dangerLevel = index == 0 ? 2 : 1,
                               unlockFlag = index == 2 ? "FLAG_RUMOR_PYESADANG_SPREAD" : string.Empty };
    }

    public string GenerateCompanionReaction(string companionId, string eventId)
    {
        switch (companionId)
        {
        case "baek_ryeon":
            return "백련이 창대를 조용히 세우고 주변의 부상자를 먼저 살핀다.";
        case "do_arin":
            return "도아린이 도집을 툭 치며 “먼저 치면 되지?”라고 웃는다.";
        case "jin_seoyul":
            return "진서율이 봉끝의 잔전기를 털며 “방금 길, 보였죠?”라고 빠르게 말한다.";
        case "seo_a":
            return "신서아가 부채를 꼭 쥐고 “작아도 바람길은 만들 수 있어요!”라고 씩 웃는다.";
        case "han_biyeon":
            return "한비연이 구월산 그림자 쪽으로 한 걸음 물러나며 “제법인데?”라고 흘린다.";
        default:
            return "동료가 말없이 당신의 결정을 가늠한다.";
        }
    }
}
}
