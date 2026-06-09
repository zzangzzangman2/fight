namespace JoseonMurimTactics
{
/// <summary>
/// 나중에 Firebase AI Logic + Gemini를 붙일 자리. v0.8에서는 MockAINarrationService가
/// 고정 문장을 반환한다. 메인 퀘스트 진행/승패/수치 판정에는 절대 쓰지 않는다 —
/// 무림 소문, NPC 잡담, 동료 반응 보조 문장처럼 곁가지 묘사에만 쓴다.
/// </summary>
public interface IAINarrationService
{
    /// <summary>전투 후 무림 소문 문장.</summary>
    string GenerateRumor(BattleResultData result);

    /// <summary>허브 NPC 잡담 한 줄.</summary>
    string GenerateNpcLine(string npcId, GameSession session);

    /// <summary>동료 반응 보조 문장.</summary>
    string GenerateCompanionReaction(string companionId, string eventId);
}
}
