using System.Collections.Generic;

namespace JoseonMurimTactics
{
/// <summary>임무 한 건의 표시/진행 정보(런타임 코드 카탈로그).</summary>
public sealed class MissionInfo
{
    public string id;
    public string title;
    public string location;
    public string battleId; // 비어있으면 아직 전투 미구현(예고용)
    public int recommendedLevel;
    public string enemyFaction;
    public string difficulty;
    public string summary;
    public string victoryConditionShort;
    public readonly List<string> rewardPreview = new List<string>();
    public string requiredFlag; // 비어있으면 항상 개방
    public string completeFlag;
    public string dangerNotes;
    public bool isStory = true;

    public bool IsUnlocked(StoryFlagService flags)
    {
        return string.IsNullOrEmpty(requiredFlag) || (flags != null && flags.HasFlag(requiredFlag));
    }

    public bool IsCompleted(StoryFlagService flags)
    {
        return !string.IsNullOrEmpty(completeFlag) && flags != null && flags.HasFlag(completeFlag);
    }

    public bool IsPlayable => !string.IsNullOrEmpty(battleId);
}

/// <summary>
/// v0.9 임무 카탈로그. 첫 임무(폐사당 방어전)는 플레이 가능, 다음 장은 예고(잠김)로 둔다.
/// </summary>
public static class MissionCatalog
{
    private static readonly List<MissionInfo> Missions = Build();

    private static List<MissionInfo> Build()
    {
        List<MissionInfo> list = new List<MissionInfo>();

        MissionInfo m1 = new MissionInfo {
            id = "MISSION_CH00_PYESADANG",
            title = "압록강 폐사당 방어전",
            location = "의주 근처 압록강 폐사당",
            battleId = HubController.FirstBattleId,
            recommendedLevel = 1,
            enemyFaction = "중원무림맹 감찰단",
            difficulty = "평이",
            summary = "중원 감찰단의 현판령에 맞서 폐사당의 조선 제자들을 지키고, 감찰사 위지강을 제압한다. 해동문 " +
                      "연합의 첫 싸움.",
            victoryConditionShort = "위지강 제압",
            requiredFlag = "",
            completeFlag = StoryFlags.FirstBattleWon,
            dangerNotes = "무너진 다리와 물가로 도하 지점이 좁다. 제단 주변은 엄폐가 좋으니 활용하되 부수지 말 것.",
            isStory = true
        };
        m1.rewardPreview.Add("은전 120");
        m1.rewardPreview.Add("약재 꾸러미");
        m1.rewardPreview.Add("무공 단서: 설악창결");
        list.Add(m1);

        MissionInfo m2 = new MissionInfo { id = "MISSION_CH01_UIJU_TAVERN",
                                           title = "의주 객잔의 회합",
                                           location = "의주 객잔",
                                           battleId = "", // v1.0 예정
                                           recommendedLevel = 2,
                                           enemyFaction = "흑립방 정탐 · 친중원파",
                                           difficulty = "보통",
                                           summary = "흩어진 조선 문파 대표들이 의주 객잔에 모인다. 암기의 고수 " +
                                                     "한비연이 처음 등장하고, 회합을 노리는 이들이 있다.",
                                           victoryConditionShort = "회합 보호 (준비 중)",
                                           requiredFlag = StoryFlags.FirstBattleWon,
                                           completeFlag = "ch01_cleared",
                                           dangerNotes = "좁은 객잔 실내전. 탁자·등불·술독 상호작용이 핵심이 될 예정.",
                                           isStory = true };
        m2.rewardPreview.Add("한비연 영입 (예정)");
        list.Add(m2);

        return list;
    }

    public static IReadOnlyList<MissionInfo> All => Missions;

    public static MissionInfo Get(string missionId)
    {
        foreach (MissionInfo m in Missions)
        {
            if (m.id == missionId)
                return m;
        }

        return null;
    }
}
}
