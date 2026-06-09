using TMPro;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class HubLocationPanel : MonoBehaviour
{
    [SerializeField]
    private TMP_Text titleText;
    [SerializeField]
    private TMP_Text bodyText;
    [SerializeField]
    private TMP_Text actionText;

    public void Bind(GameRoot root, HubLocation location)
    {
        SetText(titleText, Label(location));
        SetText(bodyText, Description(root, location));
        SetText(actionText, ActionHint(location));
    }

    public static string Label(HubLocation location)
    {
        switch (location)
        {
        case HubLocation.TrainingYard:
            return "연무장";
        case HubLocation.Tavern:
            return "객잔";
        case HubLocation.Infirmary:
            return "의원";
        case HubLocation.Market:
            return "장터";
        case HubLocation.Library:
            return "서고";
        case HubLocation.SectHall:
            return "문파 회의";
        case HubLocation.MissionGate:
            return "출정 깃발";
        case HubLocation.CompanionDeck:
            return "검각 마루";
        default:
            return "백두산 검각";
        }
    }

    private static string Description(GameRoot root, HubLocation location)
    {
        int actions = root != null ? root.Flags.GetInt("hub:daily_actions_remaining") : 0;
        switch (location)
        {
        case HubLocation.TrainingYard:
            return $"목검과 낡은 과녁이 놓인 마당. 남은 행동 {actions}.";
        case HubLocation.Tavern:
            return "상인과 무인들이 소문을 흘리는 천막 객잔.";
        case HubLocation.Infirmary:
            return "부상과 약재를 관리하는 의원 자리.";
        case HubLocation.Market:
            return "약재, 소모품, 수리 자재를 사고파는 작은 장터.";
        case HubLocation.Library:
            return "백두산 영맥, 문파 계보, 천광검문 단서를 모아두는 서고.";
        case HubLocation.SectHall:
            return "문파 기조와 세력 평판을 정리하는 회의 자리.";
        case HubLocation.MissionGate:
            return "출정 깃발 아래에서 임무 게시판과 전장 지도를 확인한다.";
        case HubLocation.CompanionDeck:
            return "동료가 머무는 마루. 대화, 선물, 상태 확인이 붙을 자리.";
        default:
            return "낡은 백두산 검각. 찢어진 천광검문 깃발 사이로 새벽바람이 스민다.";
        }
    }

    private static string ActionHint(HubLocation location)
    {
        switch (location)
        {
        case HubLocation.MissionGate:
            return "임무 / 월드맵 / 출격 준비";
        case HubLocation.CompanionDeck:
            return "대화 / 선물 / 지원 단계";
        case HubLocation.Market:
            return "구매 / 판매 / 인벤토리";
        default:
            return "장소 행동 패널";
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }
}
}
