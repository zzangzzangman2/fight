using UnityEngine;

namespace JoseonMurimTactics
{
    /// <summary>세력 정보(설계 §7-8, §8).</summary>
    [CreateAssetMenu(fileName = "Faction", menuName = "JoseonMurim/Faction Data")]
    public sealed class FactionData : ScriptableObject
    {
        public string factionId;
        public string displayName;
        public int initialReputation;
        [TextArea] public string summary;
    }
}
