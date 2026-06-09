using System.Collections.Generic;

namespace JoseonMurimTactics
{
    public sealed class CompanionInfo
    {
        public string id;
        public string name;
        public string title;
        public string role;
        public string profile;
        public string personalQuestId;

        public CompanionInfo(string id, string name, string title, string role, string profile, string personalQuestId)
        {
            this.id = id;
            this.name = name;
            this.title = title;
            this.role = role;
            this.profile = profile;
            this.personalQuestId = personalQuestId;
        }
    }

    /// <summary>
    /// 동료 기본 표시 정보(설계 §3). v0.8에서는 코드 카탈로그로 제공하고,
    /// 이후 CompanionData ScriptableObject 에셋으로 옮길 수 있다.
    /// </summary>
    public static class CompanionCatalog
    {
        public static readonly string YunSeohwa = "yun_seohwa";
        public static readonly string BaekRyeon = "baek_ryeon";
        public static readonly string HanBiyeon = "han_biyeon";
        public static readonly string DoArin = "do_arin";
        public static readonly string MaeHwaryeong = "mae_hwaryeong";
        public static readonly string KangChohui = "kang_chohui";

        private static readonly Dictionary<string, CompanionInfo> Map = Build();

        private static Dictionary<string, CompanionInfo> Build()
        {
            Dictionary<string, CompanionInfo> map = new Dictionary<string, CompanionInfo>();
            void Add(CompanionInfo c) => map[c.id] = c;

            Add(new CompanionInfo("yun_seohwa", "윤서화", "몰락 검가의 후예", "반격형 검객",
                "중원 감찰단에게 문파 현판을 빼앗긴 조선 검가의 생존자. 절제와 명예를 중시하며, 가벼운 농담에는 차갑다.",
                "PQ_YUN_SEOHWA_HONOR"));
            Add(new CompanionInfo("baek_ryeon", "백련", "설화의 의원", "빙공·회복·제어",
                "온화하지만 단호한 의원. 감찰단이 빼앗아 간 의서를 되찾으려 한다. 무고한 이를 구하면 마음을 연다.",
                "PQ_BAEK_RYEON_MANUAL"));
            Add(new CompanionInfo("han_biyeon", "한비연", "흑립방의 그림자", "암기·잠행·정찰",
                "장난기와 현실주의를 함께 지닌 추적자. 흑립방 내부 배신자를 쫓는다. 무능한 허세를 싫어한다.",
                "PQ_HAN_BIYEON_TRAITOR"));
            Add(new CompanionInfo("do_arin", "도아린", "파산권의 무인", "권법·돌파",
                "직선적이고 의리 있는 호전파. 중원 장창수에게 패한 사형의 복수를 꿈꾼다. 강적과의 정면승부에 끓어오른다.",
                "PQ_DO_ARIN_REVENGE"));
            Add(new CompanionInfo("mae_hwaryeong", "매화령", "취월곡의 음공인", "음공·버프·사기",
                "사교적이고 정보력이 강하다. 문례원에 끌려간 예인 동료를 구하려 한다. 풍류와 궁합이 좋으나 선을 넘으면 냉정하다.",
                "PQ_MAE_HWARYEONG_RESCUE"));
            Add(new CompanionInfo("kang_chohui", "강초희", "백호창의 군관", "창술·지휘·방진",
                "군영 출신의 원칙주의자. 조선 군영과 무림의 갈등을 풀려 한다. 왕도·질서 선택에 호응한다.",
                "PQ_KANG_CHOHUI_ARMY"));
            return map;
        }

        public static CompanionInfo Info(string id)
        {
            if (!string.IsNullOrEmpty(id) && Map.TryGetValue(id, out CompanionInfo info))
            {
                return info;
            }

            return null;
        }

        public static string Name(string id)
        {
            CompanionInfo info = Info(id);
            return info != null ? info.name : id;
        }

        public static IEnumerable<CompanionInfo> All => Map.Values;
    }
}
