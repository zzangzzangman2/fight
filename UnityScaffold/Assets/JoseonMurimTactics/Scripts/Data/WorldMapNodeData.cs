using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(fileName = "WorldMapNode", menuName = "JoseonMurim/World Map Node")]
public sealed class WorldMapNodeData : ScriptableObject
{
    public string nodeId;
    public string displayName;
    public string subtitle;
    public Vector2 normalizedPosition = new Vector2(0.5f, 0.5f);
    public WorldMapNodeState state = WorldMapNodeState.Available;
    public string unlockFlag;
    public string completedFlag;
    public int recommendedLevel = 1;
    public int factionPressure;
    public string linkedMissionId;
    [TextArea]
    public string description;
    public Color tint = Color.white;

    public bool IsUnlocked(StoryFlagService flags)
    {
        return state != WorldMapNodeState.Locked || string.IsNullOrEmpty(unlockFlag) ||
               (flags != null && flags.HasFlag(unlockFlag));
    }

    public bool IsCompleted(StoryFlagService flags)
    {
        return !string.IsNullOrEmpty(completedFlag) && flags != null && flags.HasFlag(completedFlag);
    }
}
}
