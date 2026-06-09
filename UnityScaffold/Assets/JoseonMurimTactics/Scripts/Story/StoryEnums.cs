namespace JoseonMurimTactics
{
/// <summary>박성준 문파 성향. 새 게임에서 1개 선택.</summary>
public enum HeroDisposition
{
    Chivalrous, // 협도
    Royal,      // 왕도
    Conqueror,  // 패도
    Romantic    // 풍류
}

/// <summary>난이도. 이야기 / 무림 / 혈로.</summary>
public enum GameDifficulty
{
    Story,    // 이야기 - 적 약화, 되돌리기 많음
    Murim,    // 무림 - 기본
    BloodPath // 혈로 - 적 강화, 되돌리기 제한
}

/// <summary>초기 무공 선택.</summary>
public enum StartingArt
{
    Sword,        // 검법
    Fist,         // 권법
    HiddenWeapon, // 암기
    InnerArt,     // 심법
    Ice           // 빙공
}

/// <summary>동료 승인도 단계 (0~100 척도에서 파생).</summary>
public enum ApprovalStage
{
    Distrust, // 불신
    Wary,     // 경계
    Neutral,  // 보통
    Trust,    // 신뢰
    Comrade   // 동지
}

public static class StoryEnumLabels
{
    public static string Label(HeroDisposition d)
    {
        switch (d)
        {
        case HeroDisposition.Chivalrous:
            return "협도";
        case HeroDisposition.Royal:
            return "왕도";
        case HeroDisposition.Conqueror:
            return "패도";
        case HeroDisposition.Romantic:
            return "풍류";
        default:
            return d.ToString();
        }
    }

    public static string Blurb(HeroDisposition d)
    {
        switch (d)
        {
        case HeroDisposition.Chivalrous:
            return "백성 보호와 명예. 정파적 선택과 동료 신뢰가 쉽다.";
        case HeroDisposition.Royal:
            return "조정과 질서, 외교. 문파 정치와 협상에 강하다.";
        case HeroDisposition.Conqueror:
            return "힘과 위압. 적의 굴복에 강하나 선한 동료가 경계한다.";
        case HeroDisposition.Romantic:
            return "대화와 도발, 설득. 박성준의 본색. 실패 시 승인도 하락.";
        default:
            return string.Empty;
        }
    }

    public static string Label(GameDifficulty d)
    {
        switch (d)
        {
        case GameDifficulty.Story:
            return "이야기";
        case GameDifficulty.Murim:
            return "무림";
        case GameDifficulty.BloodPath:
            return "혈로";
        default:
            return d.ToString();
        }
    }

    public static string Blurb(GameDifficulty d)
    {
        switch (d)
        {
        case GameDifficulty.Story:
            return "적이 약하고 되돌리기가 넉넉하다. 이야기를 즐기는 분께.";
        case GameDifficulty.Murim:
            return "표준 난이도. 무림의 균형을 그대로 맛본다.";
        case GameDifficulty.BloodPath:
            return "적이 강하고 되돌리기가 제한된다. 진검승부.";
        default:
            return string.Empty;
        }
    }

    public static string Label(StartingArt a)
    {
        switch (a)
        {
        case StartingArt.Sword:
            return "검법";
        case StartingArt.Fist:
            return "권법";
        case StartingArt.HiddenWeapon:
            return "암기";
        case StartingArt.InnerArt:
            return "심법";
        case StartingArt.Ice:
            return "빙공";
        default:
            return a.ToString();
        }
    }

    public static string Blurb(StartingArt a)
    {
        switch (a)
        {
        case StartingArt.Sword:
            return "정공법의 검로. 반격과 베기에 능하다.";
        case StartingArt.Fist:
            return "근접 권각. 밀치기와 돌파에 능하다.";
        case StartingArt.HiddenWeapon:
            return "암기와 잠행. 원거리 제압과 정찰.";
        case StartingArt.InnerArt:
            return "내공 심법. 회복과 기세 운용.";
        case StartingArt.Ice:
            return "빙공. 방어와 둔화로 전장을 늦춘다.";
        default:
            return string.Empty;
        }
    }

    public static string Label(ApprovalStage s)
    {
        switch (s)
        {
        case ApprovalStage.Distrust:
            return "불신";
        case ApprovalStage.Wary:
            return "경계";
        case ApprovalStage.Neutral:
            return "동행";
        case ApprovalStage.Trust:
            return "신뢰";
        case ApprovalStage.Comrade:
            return "맹약";
        default:
            return s.ToString();
        }
    }
}
}
