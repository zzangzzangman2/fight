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
            m1.rewardPreview.Add("은전 80");
            m1.rewardPreview.Add("목재와 약초");
            m1.rewardPreview.Add("무공 단서: 새벽일섬");
            list.Add(m1);

            MissionInfo m2 = new MissionInfo
            {
                id = "MISSION_CH02_IRON_WOLF_CLAW",
                title = "철랑문의 발톱",
                location = "백두산 소백촌 장터",
                battleId = "",
                recommendedLevel = 2,
                enemyFaction = "철랑문",
                difficulty = "보통",
                summary = "철랑문 무인들이 소백촌에 통행세를 요구하고 식량과 은전을 빼앗기 시작한다. 박성준은 처음으로 문파와 마을을 함께 지키는 싸움에 나선다.",
                victoryConditionShort = "주민 보호와 철랑문 격퇴 (준비 중)",
                requiredFlag = StoryFlags.FirstBattleWon,
                completeFlag = "CH2_IRON_WOLF_APPEARED",
                dangerNotes = "주민 보호, 야간 전투, 불붙은 창고, 인질 구출 튜토리얼 예정.",
                isStory = true
            };
            m2.rewardPreview.Add("광명호신기 해금 (예정)");
            m2.rewardPreview.Add("백두천광검문 평판 상승");
            list.Add(m2);

            MissionInfo m3 = new MissionInfo
            {
                id = "MISSION_CH03_SEORAK_FROST",
                title = "설악의 서리창",
                location = "강원도 설악창문",
                battleId = "",
                recommendedLevel = 3,
                enemyFaction = "모용세가 사절단 · 빙계곡 술사",
                difficulty = "어려움",
                summary = "더 큰 세력인 모용세가가 백두산의 후견을 제안한다. 거절 뒤 설악창문 빙계곡이 흔들리고, 박성준은 첫 동료 백련과 만난다.",
                victoryConditionShort = "빙계곡 정화와 백련 생존 (준비 중)",
                requiredFlag = StoryFlags.FirstBattleWon,
                completeFlag = StoryFlags.BaekRyeonRecruited,
                dangerNotes = "빙판 지형, 빙맥 파괴, 협공 튜토리얼 예정.",
                isStory = true
            };
            m3.rewardPreview.Add("백련 정식 합류 (예정)");
            m3.rewardPreview.Add("동료 시스템 해금");
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
