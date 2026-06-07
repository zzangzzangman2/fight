using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    [CreateAssetMenu(menuName = "Joseon Murim Tactics/Combatant Data")]
    public sealed class CombatantData : ScriptableObject
    {
        public string id;
        public string displayName;
        public Faction faction;
        public string role;
        public int maxHp = 30;
        public int maxInner = 4;
        public int armorClass = 14;
        public int movement = 4;
        public SixStats stats;
        public List<SkillData> skills = new List<SkillData>();
        public string portraitPlaceholderName;
    }
}
