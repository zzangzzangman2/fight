using System.Collections.Generic;

namespace JoseonMurimTactics
{
    public sealed class BattleObjective
    {
        public string id;
        public string description;
        public bool optional;

        public BattleObjective(string id, string description, bool optional)
        {
            this.id = id;
            this.description = description;
            this.optional = optional;
        }
    }

    /// <summary>한 전투의 메타데이터. BattlePrep/정찰/BattleResult가 공유한다.</summary>
    public sealed class BattleDefinition
    {
        public string id;
        public string title;
        public string location;
        public string bossName;
        public List<string> roster = new List<string>();        // 출격 인원(표시명)
        public string victoryCondition;
        public List<string> defeatConditions = new List<string>();
        public List<BattleObjective> objectives = new List<BattleObjective>();
        public string mapHint;

        // 승리 보상/정산
        public int silverReward;
        public List<string> rewardItems = new List<string>();
        public int joseonRenownOnWin;
        public List<IdDelta> factionOnWin = new List<IdDelta>();
        public List<IdDelta> approvalOnWin = new List<IdDelta>();
        public string questId;
    }

    /// <summary>
    /// v0.8 전투 정의 코드 카탈로그. 이후 BattleEntryData/QuestData ScriptableObject로 옮길 수 있다.
    /// </summary>
    public static class BattleCatalog
    {
        public static BattleDefinition Get(string battleId)
        {
            // v0.8에는 폐사당 방어전 하나. 미지정/미상은 동일 정의로 대체.
            BattleDefinition d = new BattleDefinition
            {
                id = string.IsNullOrEmpty(battleId) ? HubController.FirstBattleId : battleId,
                title = "압록강 폐사당 방어전",
                location = "의주 근처 압록강 폐사당",
                bossName = "감찰사 위지강",
                victoryCondition = "중원 감찰사 위지강 제압",
                mapHint = "무너진 다리와 물가로 도하 지점이 좁다. 제단 주변은 엄폐가 좋다.",
                silverReward = 120,
                joseonRenownOnWin = 5,
                questId = "MQ_CH00_DEFEND_PYESADANG"
            };

            d.roster.Add("박성준");
            d.roster.Add("백련");
            d.roster.Add("도아린");

            d.defeatConditions.Add("박성준 전투불능");
            d.defeatConditions.Add("백련 전투불능");
            d.defeatConditions.Add("10턴 초과");

            d.objectives.Add(new BattleObjective("OBJ_DEFEAT_WIJIGANG", "위지강을 죽이지 않고 제압", false));
            d.objectives.Add(new BattleObjective("OBJ_SAVE_DISCIPLE", "다친 조선 제자 구출", true));
            d.objectives.Add(new BattleObjective("OBJ_KEEP_ALTAR", "제단을 부수지 않는다", true));

            d.rewardItems.Add("약재 꾸러미");
            d.rewardItems.Add("무공 단서: 설악창결");

            d.factionOnWin.Add(new IdDelta(FactionIds.ZhongyuanAlliance, -10)); // 적대도 +10 = 평판 -10
            d.factionOnWin.Add(new IdDelta(FactionIds.JoseonSects, +5));
            d.approvalOnWin.Add(new IdDelta(CompanionCatalog.BaekRyeon, +4));
            d.approvalOnWin.Add(new IdDelta(CompanionCatalog.DoArin, +3));

            return d;
        }
    }
}
