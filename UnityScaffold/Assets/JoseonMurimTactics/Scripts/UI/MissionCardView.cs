using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
public sealed class MissionCardView : MonoBehaviour
{
    [SerializeField]
    private TMP_Text titleText;
    [SerializeField]
    private TMP_Text metaText;
    [SerializeField]
    private TMP_Text rewardText;
    [SerializeField]
    private Image difficultyStripe;

    public void Bind(MissionInfo mission)
    {
        if (mission == null)
        {
            return;
        }

        SetText(titleText, mission.title);
        SetText(
            metaText,
            $"{CategoryLabel(mission)} · 권장 {mission.recommendedLevel} · {mission.difficulty} · {mission.location}");
        SetText(rewardText, mission.rewardPreview != null ? string.Join(" / ", mission.rewardPreview) : string.Empty);
        if (difficultyStripe != null)
        {
            difficultyStripe.color = DifficultyColor(mission.difficulty);
        }
    }

    private static void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value ?? string.Empty;
        }
    }

    private static string CategoryLabel(MissionInfo mission)
    {
        if (mission.isStory)
        {
            return "주요 임무";
        }

        return string.IsNullOrEmpty(mission.battleId) ? "이벤트" : "의뢰";
    }

    private static Color DifficultyColor(string difficulty)
    {
        if (difficulty == "위험" || difficulty == "어려움")
        {
            return UiTheme.SealRed;
        }

        if (difficulty == "보통")
        {
            return UiTheme.Gold;
        }

        return UiTheme.Teal;
    }
}
}
