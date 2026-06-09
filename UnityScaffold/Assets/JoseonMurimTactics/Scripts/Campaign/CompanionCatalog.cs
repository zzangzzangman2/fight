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
    public string region;
    public string sectName;
    public int age;
    public string mbti;
    public string element;
    public string weapon;
    public string speechTone;

    public CompanionInfo(string id, string name, string title, string role, string profile, string personalQuestId,
                         string region, string sectName, int age, string mbti, string element, string weapon,
                         string speechTone)
    {
        this.id = id;
        this.name = name;
        this.title = title;
        this.role = role;
        this.profile = profile;
        this.personalQuestId = personalQuestId;
        this.region = region;
        this.sectName = sectName;
        this.age = age;
        this.mbti = mbti;
        this.element = element;
        this.weapon = weapon;
        this.speechTone = speechTone;
    }
}

/// <summary>
/// 동료 기본 표시 정보(설계 §3). v0.8에서는 코드 카탈로그로 제공하고,
/// 이후 CompanionData ScriptableObject 에셋으로 옮길 수 있다.
/// </summary>
public static class CompanionCatalog
{
    public static readonly string BaekRyeon = "baek_ryeon";
    public static readonly string HanBiyeon = "han_biyeon";
    public static readonly string DoArin = "do_arin";
    public static readonly string MaeHwaryeong = "mae_hwaryeong";
    public static readonly string SeoA = "seo_a";
    public static readonly string KangChohui = SeoA; // legacy alias

    private static readonly Dictionary<string, CompanionInfo> Map = Build();

    private static Dictionary<string, CompanionInfo> Build()
    {
        Dictionary<string, CompanionInfo> map = new Dictionary<string, CompanionInfo>();
        void Add(CompanionInfo c) => map[c.id] = c;

        Add(new CompanionInfo("baek_ryeon", "백련", "강원 설악창문의 장녀", "서리·창·제어",
                              "설악산 자락에서 이름난 창문 출신. 차분하고 신중하지만, 약한 이를 건드리는 순간 말수가 " +
                              "줄고 창끝이 매서워진다.",
                              "PQ_BAEK_RYEON_FROST_SPEAR", "강원도", "설악창문", 17, "INFJ", "얼음/서리", "창",
                              "낮고 조심스러운 존댓말. 화나면 짧게 끊어 말한다."));
        Add(new CompanionInfo(
            "do_arin", "도아린", "경상 화왕도문의 외동딸", "불·도·돌파",
            "불같은 승부욕을 지닌 도문 유망주. 생각보다 먼저 몸이 나가지만, 한번 동료라 여긴 사람은 끝까지 지킨다.",
            "PQ_DO_ARIN_FIRE_BLADE", "경상도", "화왕도문", 16, "ESTP", "불", "도",
            "거침없는 반말과 짧은 감탄사. 정면승부를 좋아한다."));
        Add(new CompanionInfo("seo_a", "서아", "경성 천뢰봉문의 막내", "전기·봉·기동",
                              "열세 살에 봉술과 뇌기를 동시에 깨친 천재. 키는 가장 작지만 기세는 누구보다 크고, " +
                              "긴장한 사람을 웃게 만든다.",
                              "PQ_SEO_A_THUNDER_STAFF", "경성", "천뢰봉문", 13, "ENFP", "전기", "봉",
                              "밝고 빠른 말투. 호기심이 많아 질문을 연달아 던진다."));
        Add(new CompanionInfo(
            "mae_hwaryeong", "매화령", "전라 풍매문의 소문주", "바람·꽃·부채·지원",
            "남원 풍매문의 딸. 웃는 얼굴로 분위기를 읽고, 바람결과 꽃잎으로 적의 균형을 흐트러뜨린다.",
            "PQ_MAE_HWARYEONG_WIND_FAN", "전라도", "풍매문", 18, "ENFJ", "바람/꽃", "부채",
            "부드럽고 사교적인 말투. 핵심을 찌를 때도 웃음을 잃지 않는다."));
        Add(new CompanionInfo(
            "han_biyeon", "한비연", "경성 흑연문의 그림자", "어둠·독·단검·암기",
            "경성 뒷골목과 궁궐 담장을 모두 아는 흑연문 유망주. 장난처럼 말하지만 관찰은 차갑고 정확하다.",
            "PQ_HAN_BIYEON_SHADOW_POISON", "경성", "흑연문", 17, "ISTP", "어둠/독", "단검·암기",
            "비꼬는 듯한 짧은 말투. 믿는 사람에게만 속내를 보인다."));
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
