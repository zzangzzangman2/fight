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
                title = "폐사당 고개 방어전",
                location = "소백촌 북쪽 폐사당 고개",
                bossName = "철랑문 정찰조장",
                victoryCondition = "철랑문 정찰조 격퇴와 백두천광 현판 보호",
                mapHint = "중앙 돌계단은 1칸 병목이다. 좌측 대나무숲, 우측 낡은 다리, 상단 사당 고지를 나눠 막아라.",
                silverReward = 80,
                joseonRenownOnWin = 3,
                questId = "MISSION_CH01_BLACK_MARK"
            };

            d.roster.Add("박성준");
            d.roster.Add("백련");
            d.roster.Add("도아린");
            d.roster.Add("진서율");
            d.roster.Add("신서아");
            d.roster.Add("한비연");

            d.defeatConditions.Add("박성준 전투불능");
            d.defeatConditions.Add("백두천광 현판 파괴");
            d.defeatConditions.Add("12턴 초과");

            d.objectives.Add(new BattleObjective("OBJ_DEFEAT_SCOUTS", "철랑문 정찰조 격퇴", false));
            d.objectives.Add(new BattleObjective("OBJ_PROTECT_SIGNBOARD", "백두천광 현판 보호", false));
            d.objectives.Add(new BattleObjective("OBJ_USE_TERRAIN", "향로·등불·다리·석등 중 1개 이상 활용", true));

            d.rewardItems.Add("사당 현판 보존 명성");
            d.rewardItems.Add("약초 꾸러미");
            d.rewardItems.Add("무공 단서: 돌계단 방진");

            d.factionOnWin.Add(new IdDelta(FactionIds.ZhongyuanAlliance, -3));
            d.factionOnWin.Add(new IdDelta(FactionIds.JoseonSects, +3));

            return d;
        }
    }
}
