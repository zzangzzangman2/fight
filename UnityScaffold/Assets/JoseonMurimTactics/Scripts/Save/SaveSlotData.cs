using System;

namespace JoseonMurimTactics
{
[Serializable]
public sealed class SaveSlotData
{
    public string slotId;
    public bool exists;
    public string chapterTitle;
    public string location;
    public string playTimeText;
    public string savedAtText;
    public int silver;
    public int companionCount;
    public string recentMissionId;

    public static SaveSlotData FromSummary(SaveSlotSummary summary)
    {
        if (summary == null)
        {
            return new SaveSlotData();
        }

        return new SaveSlotData { slotId = summary.slotId,
                                  exists = summary.exists,
                                  chapterTitle = summary.chapterTitle,
                                  location = summary.location,
                                  playTimeText = summary.playTimeText,
                                  savedAtText = summary.savedAtText,
                                  silver = summary.silver,
                                  companionCount = summary.companionCount,
                                  recentMissionId = summary.recentMissionId };
    }
}
}
