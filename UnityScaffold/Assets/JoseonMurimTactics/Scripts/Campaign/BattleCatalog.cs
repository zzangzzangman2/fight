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

    /// <summary>Battle metadata shared by prep/scout/result screens.</summary>
    public sealed class BattleDefinition
    {
        public string id;
        public string title;
        public string location;
        public string bossName;
        public List<string> roster = new List<string>();
        public string victoryCondition;
        public List<string> defeatConditions = new List<string>();
        public List<BattleObjective> objectives = new List<BattleObjective>();
        public string mapHint;

        public int silverReward;
        public List<string> rewardItems = new List<string>();
        public int joseonRenownOnWin;
        public List<IdDelta> factionOnWin = new List<IdDelta>();
        public List<IdDelta> approvalOnWin = new List<IdDelta>();
        public string questId;
    }

    /// <summary>Battle catalog for the currently playable early-story encounter.</summary>
    public static class BattleCatalog
    {
        public static BattleDefinition Get(string battleId)
        {
            BattleDefinition d = new BattleDefinition
            {
                id = string.IsNullOrEmpty(battleId) ? HubController.FirstBattleId : battleId,
                title = "백두산 길목의 검은 표식",
                location = "백두산 소백촌 북쪽 길목",
                bossName = "철랑문 정찰조장",
                victoryCondition = "철랑문 정찰조 격퇴와 검은 표식 조사",
                mapHint = "좁은 산길과 짐수레가 전장을 가른다. 나무와 바위를 엄폐로 삼고 주민 피해를 줄여라.",
                silverReward = 80,
                joseonRenownOnWin = 3,
                questId = "MISSION_CH01_BLACK_MARK"
            };

            d.roster.Add("박성준");
            d.roster.Add("연옥");

            d.defeatConditions.Add("박성준 전투불능");
            d.defeatConditions.Add("마을 주민 3명 이상 피해");
            d.defeatConditions.Add("12턴 초과");

            d.objectives.Add(new BattleObjective("OBJ_DEFEAT_SCOUTS", "철랑문 정찰조 격퇴", false));
            d.objectives.Add(new BattleObjective("OBJ_SAVE_PORTERS", "마을 짐꾼 2명 이상 생존", true));
            d.objectives.Add(new BattleObjective("OBJ_INSPECT_MARK", "검은 표식 조사", true));

            d.rewardItems.Add("목재 묶음");
            d.rewardItems.Add("약초 꾸러미");
            d.rewardItems.Add("무공 단서: 새벽일섬");

            d.factionOnWin.Add(new IdDelta(FactionIds.ZhongyuanAlliance, -3));
            d.factionOnWin.Add(new IdDelta(FactionIds.JoseonSects, +3));

            return d;
        }
    }
}
