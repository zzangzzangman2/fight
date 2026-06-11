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
            if (battleId == HubController.WolfPassBattleId)
            {
                return CreateWolfPassBattle();
            }
            if (battleId == HubController.TigerRavineBattleId)
            {
                return CreateTigerRavineBattle();
            }
            if (battleId == HubController.LeopardCliffBattleId)
            {
                return CreateLeopardCliffBattle();
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
            d.rewardItems.Add("철광석"); // 폐광 입구 — 무기 강화 재료 수급처(후속 지시 §6)
            d.rewardItems.Add("마을 감사패");

            d.factionOnWin.Add(new IdDelta(FactionIds.JoseonSects, +1));
            d.factionOnWin.Add(new IdDelta(FactionIds.BlackHatGuild, -2));

            return d;
        }

        private static BattleDefinition CreateWolfPassBattle()
        {
            return CreateWildlifeBattle(
                HubController.WolfPassBattleId,
                "MISSION_FREE_SOBAEK_WOLF_PASS",
                "소백촌 늑대 고개 방어",
                "소백촌 북동쪽 자작나무 고개",
                "굶주린 늑대 우두머리",
                "늑대 우두머리 제압과 방목민 피난로 확보",
                "남쪽 방목길에서 진입한다. 중앙 개울은 다리와 얕은 여울만 건널 수 있고, 동쪽 능선은 고저차가 크며, 북쪽 굴 주변 나무와 바위는 통과할 수 없다.",
                38,
                "OBJ_REPEL_WOLF_PACK",
                "굶주린 늑대 무리 격퇴",
                "OBJ_PROTECT_HERDERS",
                "방목민 피난로 확보",
                "OBJ_SECURE_WOLF_DEN",
                "북쪽 늑대 굴 봉쇄",
                "산양 젖",
                "질긴 가죽",
                "질긴 천", // 방어구 강화 재료 수급처(후속 지시 §6)
                "마을 감사패");
        }

        private static BattleDefinition CreateTigerRavineBattle()
        {
            return CreateWildlifeBattle(
                HubController.TigerRavineBattleId,
                "MISSION_FREE_SOBAEK_TIGER_RAVINE",
                "백호 바위골 주민 구조",
                "소백촌 남쪽 바위골",
                "산군 호랑이",
                "산군 호랑이 제압과 갇힌 주민 구조",
                "바위골은 가운데 절벽과 낙석이 길을 끊는다. 서쪽 억새밭은 느리지만 엄폐가 좋고, 동쪽 바위 선반은 H3 고지라 한 칸씩 올라야 한다.",
                55,
                "OBJ_SUBDUE_TIGER",
                "산군 호랑이 제압",
                "OBJ_RESCUE_VILLAGERS",
                "갇힌 주민 구조",
                "OBJ_CONTROL_RAVINE_HIGHGROUND",
                "동쪽 바위 선반 확보",
                "호피 조각",
                "응급 약재",
                "옥 조각", // 장신구 강화 재료 수급처(후속 지시 §6)
                "마을 감사패");
        }

        private static BattleDefinition CreateLeopardCliffBattle()
        {
            return CreateWildlifeBattle(
                HubController.LeopardCliffBattleId,
                "MISSION_FREE_SOBAEK_LEOPARD_CLIFF",
                "표범 절벽길 약초꾼 호송",
                "소백촌 동쪽 절벽 약초길",
                "그림자 표범",
                "그림자 표범 격퇴와 약초꾼 호송로 개방",
                "절벽길은 하단 낭떠러지와 대나무 덤불이 길을 잘라낸다. 밧줄다리만 계곡을 넘고, 북동쪽 약초 선반은 표범이 먼저 차지한 고지다.",
                50,
                "OBJ_DRIVE_OFF_LEOPARD",
                "그림자 표범 격퇴",
                "OBJ_ESCORT_HERBALISTS",
                "약초꾼 호송로 개방",
                "OBJ_AVOID_CLIFF_AMBUSH",
                "절벽 매복 피해 최소화",
                "희귀 약초",
                "표범 무늬 가죽",
                "철광석", // 무기 강화 재료 보조 수급처(후속 지시 §6)
                "마을 감사패");
        }

        private static BattleDefinition CreateWildlifeBattle(string battleId, string questId, string title,
                                                             string location, string bossName, string victory,
                                                             string mapHint, int silver, string primaryObjectiveId,
                                                             string primaryObjective, string supportObjectiveId,
                                                             string supportObjective, string optionalObjectiveId,
                                                             string optionalObjective, params string[] rewards)
        {
            BattleDefinition d = new BattleDefinition
            {
                id = battleId,
                title = title,
                location = location,
                bossName = bossName,
                victoryCondition = victory,
                mapHint = mapHint,
                silverReward = silver,
                joseonRenownOnWin = 1,
                questId = questId,
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

            d.objectives.Add(new BattleObjective(primaryObjectiveId, primaryObjective, false));
            d.objectives.Add(new BattleObjective(supportObjectiveId, supportObjective, false));
            d.objectives.Add(new BattleObjective(optionalObjectiveId, optionalObjective, true));

            foreach (string reward in rewards)
            {
                d.rewardItems.Add(reward);
            }

            d.factionOnWin.Add(new IdDelta(FactionIds.JoseonSects, +1));

            return d;
        }
    }
}
