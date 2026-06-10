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
    public bool romanceEligible;

    public CompanionInfo(string id, string name, string title, string role, string profile, string personalQuestId,
                         string region, string sectName, int age, string mbti, string element, string weapon,
                         string speechTone, bool romanceEligible = false)
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
        this.romanceEligible = romanceEligible;
    }

    public bool CanReceiveRomanticEffects => romanceEligible;
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
    public static readonly string JinSeoyul = "jin_seoyul";
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
        Add(new CompanionInfo("jin_seoyul", "진서율", "경성 천뢰봉문의 천재 봉술가", "전기·봉·기동",
                              "열다섯에 봉술과 뇌기를 동시에 깨친 경성의 천재. 말이 빠르고 장난이 많지만, 전장에서는 " +
                                  "피뢰침처럼 빈틈을 잡아낸다.",
                              "PQ_JIN_SEOYUL_LIGHTNING_STAFF", "경성", "천뢰봉문", 15, "ENTP", "전기", "봉",
                              "빠르고 장난기 있는 말투. 농담과 추리를 번개처럼 이어 간다."));
        Add(new CompanionInfo("seo_a", "신서아", "전라 남원 화접풍류문의 막내", "바람·꽃·부채·지원",
                              "작은 키와 밝은 웃음으로 방심을 부르지만, 꽃바람과 부채술로 아군의 길을 열고 적의 " +
                                  "균형을 흩뜨리는 막내 동료.",
                              "PQ_SHIN_SEOA_FLOWER_WIND_FAN", "전라도 남원", "화접풍류문", 13, "ENFP", "바람/꽃",
                              "부채", "밝고 씩씩한 막내 말투. 작다고 얕보면 바로 받아친다."));
        Add(new CompanionInfo("han_biyeon", "한비연", "황해도 구월산 흑련암문의 그림자", "어둠·독·단검·암기",
                              "구월산 흑련암문 출신의 암기 고수. 장난처럼 말하지만 관찰은 차갑고 정확하며, 독과 " +
                                  "그림자로 누명을 벗겨낼 단서를 찾는다.",
                              "PQ_HAN_BIYEON_SHADOW_POISON", "황해도 구월산", "흑련암문", 17, "ISTP", "어둠/독",
                              "단검·암기", "비꼬는 듯한 짧은 말투. 믿는 사람에게만 속내를 보인다."));
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
