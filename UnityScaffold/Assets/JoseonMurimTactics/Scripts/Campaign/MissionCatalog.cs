using System.Collections.Generic;

namespace JoseonMurimTactics
{
    /// <summary>Runtime mission display/progression information.</summary>
    public sealed class MissionInfo
    {
        public string id;
        public string title;
        public string location;
        public string battleId;
        public int recommendedLevel;
        public string enemyFaction;
        public string difficulty;
        public string summary;
        public string victoryConditionShort;
        public readonly List<string> rewardPreview = new List<string>();
        public string requiredFlag;
        public string completeFlag;
        public string dangerNotes;
        public bool isStory = true;
        public bool repeatable;
        public bool consumesFreeTime;

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

    /// <summary>Mission catalog for the early Baekdu Cheongwang story arc.</summary>
    public static class MissionCatalog
    {
        private static readonly List<MissionInfo> Missions = Build();

        private static List<MissionInfo> Build()
        {
            List<MissionInfo> list = new List<MissionInfo>();

            MissionInfo m1 = new MissionInfo
            {
                id = "MISSION_CH01_BLACK_MARK",
                title = "백두산 길목의 검은 표식",
                location = "백두산 소백촌 북쪽 길목",
                battleId = HubController.FirstBattleId,
                recommendedLevel = 1,
                enemyFaction = "철랑문 정찰조",
                difficulty = "평이",
                summary = "백두산 영맥을 표시한 검은 표식이 소백촌 길목에서 발견된다. 박성준은 마을 짐꾼들을 지키며 철랑문 정찰조를 몰아내야 한다.",
                victoryConditionShort = "정찰조 격퇴와 표식 조사",
                requiredFlag = "",
                completeFlag = StoryFlags.FirstBattleWon,
                dangerNotes = "좁은 산길, 나무 엄폐, 짐수레 보호. 빛 속성 검기와 기본 이동 튜토리얼에 적합.",
                isStory = true
            };
            m1.rewardPreview.Add("은냥 80");
            m1.rewardPreview.Add("목재와 약초");
            m1.rewardPreview.Add("무공 단서: 새벽일섬");
            list.Add(m1);

            MissionInfo m2 = new MissionInfo
            {
                id = "MISSION_CH01_SEORAK_REQUEST",
                title = "설악으로 가는 서리길",
                location = "백두산 동남쪽 설운령 산길",
                battleId = HubController.SeorakPassRescueBattleId,
                recommendedLevel = 3,
                enemyFaction = "철비채 산적 · 중원 감찰단 하수인",
                difficulty = "보통",
                summary = "첫 공세를 막아낸 백두천광검문은 단독 방어의 한계를 마주한다. 박성준은 설악창문에 원군을 청하러 가던 길에, 약초 수레와 피난민을 지키는 창수 백련과 만난다.",
                victoryConditionShort = "유달근 격파와 약초 수레·피난민 보호",
                requiredFlag = StoryFlags.FirstBattleWon,
                completeFlag = StoryFlags.BaekRyeonRecruited,
                dangerNotes = "좁은 절벽길, 약초 수레 보호, 백련 게스트 합류, 빛/서리 협공 튜토리얼에 적합.",
                isStory = true
            };
            m2.rewardPreview.Add("백련 정식 합류");
            m2.rewardPreview.Add("설악창문 평판 +5");
            m2.rewardPreview.Add("약초 꾸러미 2개");
            m2.rewardPreview.Add("북방 문파 연합 단서");
            list.Add(m2);

            MissionInfo m3 = new MissionInfo
            {
                id = "MISSION_CH02_IRON_WOLF_CLAW",
                title = "철랑문의 발톱",
                location = "백두산 소백촌 장터",
                battleId = "",
                recommendedLevel = 4,
                enemyFaction = "철랑문",
                difficulty = "보통",
                summary = "철랑문 무인들이 소백촌에 통행세를 요구하고 식량과 은전을 빼앗기 시작한다. 설악창문의 첫 창을 얻은 박성준은 문파와 마을을 함께 지키는 싸움을 준비한다.",
                victoryConditionShort = "주민 보호와 철랑문 격퇴 (준비 중)",
                requiredFlag = StoryFlags.BaekRyeonRecruited,
                completeFlag = "CH2_IRON_WOLF_APPEARED",
                dangerNotes = "주민 보호, 야간 전투, 불붙은 창고, 인질 구출 튜토리얼 예정.",
                isStory = true
            };
            m3.rewardPreview.Add("광명호신기 해금 (예정)");
            m3.rewardPreview.Add("백두천광검문 평판 상승");
            list.Add(m3);

            MissionInfo m4 = new MissionInfo
            {
                id = "MISSION_CH04_CHEONNOE_STAFF",
                title = "경성 천뢰봉문의 감전 사건",
                location = "경성 천뢰봉문",
                battleId = "",
                recommendedLevel = 4,
                enemyFaction = "중원 밀정 · 전기 장치 도적",
                difficulty = "보통",
                summary = "경성 무관 수련생들이 원인 모를 감전 증세로 쓰러진다. 천뢰봉문의 천재 봉술가 진서율이 사건의 전기 흐름을 추적한다.",
                victoryConditionShort = "피뢰 장치 조사와 진서율 보호 (준비 중)",
                requiredFlag = StoryFlags.BaekRyeonRecruited,
                completeFlag = StoryFlags.JinSeoyulRecruited,
                dangerNotes = "지붕, 피뢰침, 물웅덩이 감전. 기절과 장치 상호작용 튜토리얼 예정.",
                isStory = true
            };
            m4.rewardPreview.Add("진서율 영입 (예정)");
            m4.rewardPreview.Add("무공 단서: 천뢰봉무");
            list.Add(m4);

            MissionInfo m5 = new MissionInfo
            {
                id = "MISSION_CH04_NAMWON_FLOWER_WIND",
                title = "남원 화접풍류문의 꽃바람",
                location = "전라도 남원 화접풍류문",
                battleId = "",
                recommendedLevel = 4,
                enemyFaction = "친중원파 낭인 · 향로 술사",
                difficulty = "보통",
                summary = "꽃축제 중 기억을 흐리는 향과 독안개가 퍼진다. 작지만 씩씩한 막내 신서아가 부채와 꽃바람으로 사람들을 대피시킨다.",
                victoryConditionShort = "군중 보호와 향로 봉인 (준비 중)",
                requiredFlag = StoryFlags.BaekRyeonRecruited,
                completeFlag = StoryFlags.SeoARecruited,
                dangerNotes = "꽃밭, 바람길, 향로, 군중 보호. 회피 버프와 이동 보조 튜토리얼 예정.",
                isStory = true
            };
            m5.rewardPreview.Add("신서아 영입 (예정)");
            m5.rewardPreview.Add("무공 단서: 화접풍류선");
            list.Add(m5);

            MissionInfo m6 = new MissionInfo
            {
                id = "MISSION_CH04_GUWOLSAN_SHADOW_POISON",
                title = "구월산 흑련암문의 독살 누명",
                location = "황해도 구월산 흑련암문",
                battleId = "",
                recommendedLevel = 4,
                enemyFaction = "암살 누명 조작 세력 · 중원 밀정",
                difficulty = "어려움",
                summary = "독과 암기를 쓰는 문파라는 편견을 이용해 흑련암문이 누명을 쓴다. 한비연은 진범의 흔적을 따라 그림자길을 연다.",
                victoryConditionShort = "진범 추적과 독안개 돌파 (준비 중)",
                requiredFlag = StoryFlags.BaekRyeonRecruited,
                completeFlag = StoryFlags.HanBiyeonRecruited,
                dangerNotes = "독안개, 은신 적, 암기 함정, 시야 제한. 독과 표식 튜토리얼 예정.",
                isStory = true
            };
            m6.rewardPreview.Add("한비연 영입 (예정)");
            m6.rewardPreview.Add("무공 단서: 흑련독침");
            list.Add(m6);

            MissionInfo banditLair = new MissionInfo
            {
                id = "MISSION_FREE_SOBAEK_BANDIT_LAIR",
                title = "소백촌 뒷산 도적 소굴",
                location = "소백촌 서쪽 벌목길 폐광 입구",
                battleId = HubController.BanditLairBattleId,
                recommendedLevel = 1,
                enemyFaction = "흑립방 산도적",
                difficulty = "평이",
                summary = "자유시간에 소백촌 주민들이 맡긴 의뢰를 처리한다. 폐광을 차지한 도적떼가 약재와 목재를 빼앗아 숨겨두었으니, 좁은 벌목길과 망루를 피해 소굴을 정리해야 한다.",
                victoryConditionShort = "도적 두목 제압과 빼앗긴 보급 회수",
                requiredFlag = "",
                completeFlag = "",
                dangerNotes = "벌목길 병목, 통나무 장애물, 진흙 웅덩이, 덫, 망루 고지. 자유시간/기력 1을 소모하는 반복 의뢰.",
                isStory = false,
                repeatable = true,
                consumesFreeTime = true
            };
            banditLair.rewardPreview.Add("은냥 45");
            banditLair.rewardPreview.Add("약재 꾸러미");
            banditLair.rewardPreview.Add("목재 묶음");
            banditLair.rewardPreview.Add("마을 신뢰 +1");
            list.Add(banditLair);

            MissionInfo wolfPass = new MissionInfo
            {
                id = "MISSION_FREE_SOBAEK_WOLF_PASS",
                title = "소백촌 늑대 고개 방어",
                location = "소백촌 북동쪽 자작나무 고개",
                battleId = HubController.WolfPassBattleId,
                recommendedLevel = 1,
                enemyFaction = "굶주린 늑대 무리",
                difficulty = "평이",
                summary = "방목민과 나무꾼들이 늑대에게 길을 빼앗겼다. 얕은 개울과 쓰러진 통나무, 동쪽 능선을 이용해 늑대 무리를 몰아내고 북쪽 굴을 봉쇄해야 한다.",
                victoryConditionShort = "늑대 우두머리 제압과 피난로 확보",
                requiredFlag = "",
                completeFlag = "",
                dangerNotes = "개울 병목, 쓰러진 통나무 장애물, H2 능선, 늑대 굴 바위. 자유시간/기력 1을 소모하는 반복 의뢰.",
                isStory = false,
                repeatable = true,
                consumesFreeTime = true
            };
            wolfPass.rewardPreview.Add("은냥 38");
            wolfPass.rewardPreview.Add("산양 젖");
            wolfPass.rewardPreview.Add("질긴 가죽");
            wolfPass.rewardPreview.Add("마을 신뢰 +1");
            list.Add(wolfPass);

            MissionInfo tigerRavine = new MissionInfo
            {
                id = "MISSION_FREE_SOBAEK_TIGER_RAVINE",
                title = "백호 바위골 주민 구조",
                location = "소백촌 남쪽 바위골",
                battleId = HubController.TigerRavineBattleId,
                recommendedLevel = 2,
                enemyFaction = "산군 호랑이",
                difficulty = "보통",
                summary = "바위골을 지나던 주민들이 산군에게 막혀 움직이지 못한다. 억새밭과 낙석 지대를 돌아가며 동쪽 바위 선반을 제압하고 주민들을 빼내야 한다.",
                victoryConditionShort = "산군 호랑이 제압과 갇힌 주민 구조",
                requiredFlag = "",
                completeFlag = "",
                dangerNotes = "절벽 낙차, 막힌 바위벽, 억새 엄폐, H3 바위 선반. 자유시간/기력 1을 소모하는 반복 의뢰.",
                isStory = false,
                repeatable = true,
                consumesFreeTime = true
            };
            tigerRavine.rewardPreview.Add("은냥 55");
            tigerRavine.rewardPreview.Add("호피 조각");
            tigerRavine.rewardPreview.Add("응급 약재");
            tigerRavine.rewardPreview.Add("마을 신뢰 +1");
            list.Add(tigerRavine);

            MissionInfo leopardCliff = new MissionInfo
            {
                id = "MISSION_FREE_SOBAEK_LEOPARD_CLIFF",
                title = "표범 절벽길 약초꾼 호송",
                location = "소백촌 동쪽 절벽 약초길",
                battleId = HubController.LeopardCliffBattleId,
                recommendedLevel = 2,
                enemyFaction = "그림자 표범",
                difficulty = "보통",
                summary = "약초꾼들이 표범 매복 때문에 절벽길을 지나지 못한다. 대나무 덤불과 밧줄다리, 북동쪽 약초 선반의 고저차를 읽고 호송로를 열어야 한다.",
                victoryConditionShort = "그림자 표범 격퇴와 약초꾼 호송로 개방",
                requiredFlag = "",
                completeFlag = "",
                dangerNotes = "낭떠러지 장애물, 밧줄다리 병목, 대나무 덤불 시야 차단, H3 약초 선반. 자유시간/기력 1을 소모하는 반복 의뢰.",
                isStory = false,
                repeatable = true,
                consumesFreeTime = true
            };
            leopardCliff.rewardPreview.Add("은냥 50");
            leopardCliff.rewardPreview.Add("희귀 약초");
            leopardCliff.rewardPreview.Add("표범 무늬 가죽");
            leopardCliff.rewardPreview.Add("마을 신뢰 +1");
            list.Add(leopardCliff);

            return list;
        }

        public static IReadOnlyList<MissionInfo> All => Missions;

        public static MissionInfo Get(string missionId)
        {
            foreach (MissionInfo m in Missions)
            {
                if (m.id == missionId)
                {
                    return m;
                }
            }

            return null;
        }
    }
}
