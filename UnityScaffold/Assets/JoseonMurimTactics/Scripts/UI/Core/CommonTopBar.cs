using TMPro;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class CommonTopBar : MonoBehaviour
{
    [SerializeField]
    private TMP_Text titleText;
    [SerializeField]
    private TMP_Text chapterText;
    [SerializeField]
    private TMP_Text resourceText;

    public void Bind(GameRoot root, string title)
    {
        if (root == null)
        {
            return;
        }

        SetText(titleText, title);
        SetText(chapterText, $"{root.Session.sectName} · {root.Session.currentChapterId}");
        SetText(
            resourceText,
            $"은냥 {root.Flags.GetInt("silver")} · 동료 {root.Session.recruitedCompanionIds.Count} · 행동 {root.Session.actionsTaken}");
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
