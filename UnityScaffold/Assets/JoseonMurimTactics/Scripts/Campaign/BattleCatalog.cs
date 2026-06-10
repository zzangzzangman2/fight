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
        public bool repeatable;
    }

    /// <summary>Battle catalog for the currently playable early-story encounter.</summary>
    public static class BattleCatalog
    {
        public static BattleDefinition Get(string battleId)
        {
            if (battleId == HubController.BanditLairBattleId)
            {
                return CreateBanditLairBattle();
            }

            return CreatePyesadangDefenseBattle(battleId);
        }

        private static BattleDefinition CreatePyesadangDefenseBattle(string battleId)
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

        private static BattleDefinition CreateBanditLairBattle()
        {
            BattleDefinition d = new BattleDefinition
            {
                id = HubController.BanditLairBattleId,
                title = "소백촌 도적 소굴 토벌",
                location = "소백촌 서쪽 벌목길 폐광 입구",
                bossName = "흑립방 두목 곽칠",
                victoryCondition = "도적 두목 제압과 빼앗긴 보급 회수",
                mapHint = "남쪽 벌목길에서 진입한다. 중앙 진흙길은 빠르지만 덫이 많고, 좌측 숲길은 엄폐가 좋으며, 우측 망루는 고지라 원거리 도적을 먼저 끊어야 한다.",
                silverReward = 45,
                joseonRenownOnWin = 1,
                questId = "MISSION_FREE_SOBAEK_BANDIT_LAIR",
                repeatable = true
            };

            d.roster.Add("박성준");
            d.roster.Add("백련");
            d.roster.Add("도아린");
            d.roster.Add("진서율");
            d.roster.Add("신서아");
            d.roster.Add("한비연");

            d.defeatConditions.Add("아군 전멸");
            d.defeatConditions.Add("12턴 초과");

            d.objectives.Add(new BattleObjective("OBJ_CLEAR_BANDIT_LAIR", "흑립방 두목 곽칠 제압", false));
            d.objectives.Add(new BattleObjective("OBJ_RECOVER_SUPPLIES", "빼앗긴 약재·목재 회수", false));
            d.objectives.Add(new BattleObjective("OBJ_AVOID_TRAPS", "덫 피해 최소화", true));

            d.rewardItems.Add("약재 꾸러미");
            d.rewardItems.Add("목재 묶음");
            d.rewardItems.Add("마을 감사패");

            d.factionOnWin.Add(new IdDelta(FactionIds.JoseonSects, +1));
            d.factionOnWin.Add(new IdDelta(FactionIds.BlackHatGuild, -2));

            return d;
        }
    }
}
