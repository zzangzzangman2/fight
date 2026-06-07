using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    [CreateAssetMenu(menuName = "Joseon Murim Tactics/Skill Data")]
    public sealed class SkillData : ScriptableObject
    {
        public string id;
        public string displayName;
        public ActionSlot actionSlot = ActionSlot.Main;
        public TargetType targetType = TargetType.Enemy;
        public int range = 1;
        public StatType stat = StatType.Agility;
        public int attackBonus;
        public string damageDice = "1d6";
        public string healDice;
        public int innerCost;
        public int cooldown;
        public int usesPerBattle;
        public int breakGain;
        public int moraleDamage;
        public int pushDistance;
        public List<SkillTag> tags = new List<SkillTag>();
        public TimelineCue timelineCue = TimelineCue.None;
    }
}
