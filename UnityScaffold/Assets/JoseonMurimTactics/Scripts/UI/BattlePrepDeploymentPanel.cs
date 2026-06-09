using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class BattlePrepDeploymentPanel : MonoBehaviour
{
    public const int MaxSlots = 5;

    [SerializeField]
    private TMP_Text slotsText;

    private readonly List<string> selectedCompanionIds = new List<string>(MaxSlots);

    public IReadOnlyList<string> SelectedCompanionIds => selectedCompanionIds;

    public bool TryToggle(string companionId)
    {
        if (string.IsNullOrEmpty(companionId))
        {
            return false;
        }

        if (selectedCompanionIds.Contains(companionId))
        {
            selectedCompanionIds.Remove(companionId);
            Refresh();
            return true;
        }

        if (selectedCompanionIds.Count >= MaxSlots)
        {
            return false;
        }

        selectedCompanionIds.Add(companionId);
        Refresh();
        return true;
    }

    public void BindRoster(IEnumerable<string> companionIds)
    {
        selectedCompanionIds.Clear();
        if (companionIds != null)
        {
            foreach (string id in companionIds)
            {
                if (selectedCompanionIds.Count >= MaxSlots)
                {
                    break;
                }

                selectedCompanionIds.Add(id);
            }
        }

        Refresh();
    }

    private void Refresh()
    {
        if (slotsText == null)
        {
            return;
        }

        List<string> lines = new List<string>();
        for (int i = 0; i < MaxSlots; i++)
        {
            string id = i < selectedCompanionIds.Count ? selectedCompanionIds[i] : null;
            lines.Add(string.IsNullOrEmpty(id) ? $"출격 슬롯 {i + 1}: 비어 있음"
                                               : $"출격 슬롯 {i + 1}: {CompanionCatalog.Name(id)}");
        }

        slotsText.text = string.Join("\n", lines);
    }
}
}
