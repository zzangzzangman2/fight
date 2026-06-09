using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class SaveLoadPopup : UIScreenBase
{
    [SerializeField]
    private TMP_Text slotsText;

    public List<SaveSlotData> BuildSlotData(GameRoot root)
    {
        List<SaveSlotData> slots = new List<SaveSlotData>();
        if (root == null || root.Save == null)
        {
            return slots;
        }

        foreach (string slot in SaveManager.AllSlots)
        {
            slots.Add(SaveSlotData.FromSummary(root.Save.Peek(slot)));
        }

        return slots;
    }

    public void Refresh(GameRoot root)
    {
        if (slotsText == null)
        {
            return;
        }

        List<SaveSlotData> slots = BuildSlotData(root);
        List<string> lines = new List<string>();
        foreach (SaveSlotData slot in slots)
        {
            string label = slot.slotId == SaveManager.AutoSlot ? "자동 저장" : "수동 " + slot.slotId;
            lines.Add(
                slot.exists
                    ? $"{label} · {slot.chapterTitle} · {slot.location} · 은전 {slot.silver} · 동료 {slot.companionCount} · {slot.savedAtText}"
                    : $"{label} · 비어 있음");
        }

        slotsText.text = string.Join("\n", lines);
    }
}
}
