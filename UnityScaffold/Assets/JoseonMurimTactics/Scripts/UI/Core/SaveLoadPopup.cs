using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class SaveLoadPopup : UIScreenBase
{
    [SerializeField]
    private TMP_Text slotsText;

    public string LastMessage { get; private set; }

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
            string warning = slot.versionMismatch ? " · " + slot.versionWarning : string.Empty;
            lines.Add(
                slot.exists
                    ? $"{label} · {slot.chapterTitle} · {slot.location} · 은냥 {slot.silver} · 동료 {slot.companionCount} · {slot.savedAtText}{warning}"
                    : $"{label} · 비어 있음");
        }

        slotsText.text = string.Join("\n", lines);
    }

    public bool TrySave(GameRoot root, string slot, bool overwriteConfirmed)
    {
        if (root == null || root.Save == null || root.Session == null || string.IsNullOrEmpty(slot))
        {
            LastMessage = "저장할 세션이 없습니다.";
            return false;
        }

        if (root.Save.HasSave(slot) && !overwriteConfirmed)
        {
            LastMessage = "기존 저장을 덮어쓸지 확인이 필요합니다.";
            return false;
        }

        bool ok = root.Save.Save(root.Session, slot);
        LastMessage = ok ? "저장되었습니다." : "저장에 실패했습니다.";
        if (root.Notifications != null)
        {
            root.Notifications.Push(LastMessage, ok ? NotificationKind.Success : NotificationKind.Error);
        }

        Refresh(root);
        return ok;
    }

    public bool TryLoad(GameRoot root, string slot)
    {
        if (root == null || root.Save == null || string.IsNullOrEmpty(slot))
        {
            LastMessage = "불러올 슬롯이 없습니다.";
            return false;
        }

        GameSession loaded = root.Save.Load(slot);
        if (loaded == null)
        {
            LastMessage = "불러오기에 실패했습니다.";
            return false;
        }

        root.LoadExistingSession(loaded);
        LastMessage = "불러왔습니다.";
        if (root.Notifications != null)
        {
            root.Notifications.Push(LastMessage, NotificationKind.Success);
        }

        return true;
    }

    public bool TryDelete(GameRoot root, string slot, bool deleteConfirmed)
    {
        if (root == null || root.Save == null || string.IsNullOrEmpty(slot))
        {
            LastMessage = "삭제할 슬롯이 없습니다.";
            return false;
        }

        if (!deleteConfirmed)
        {
            LastMessage = "삭제 확인이 필요합니다.";
            return false;
        }

        bool ok = root.Save.Delete(slot);
        LastMessage = ok ? "저장을 삭제했습니다." : "저장 삭제에 실패했습니다.";
        if (root.Notifications != null)
        {
            root.Notifications.Push(LastMessage, ok ? NotificationKind.Warning : NotificationKind.Error);
        }

        Refresh(root);
        return ok;
    }
}
}
