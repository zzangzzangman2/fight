using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    public sealed class CombatantRuntime
    {
        public CombatantData data;
        public Vector2Int currentCell;
        public int hp;
        public int inner;
        public int morale;
        public int breakGauge;
        public bool defeated;
        public bool surrendered;
        public readonly ActionEconomy actions = new ActionEconomy();
        public readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>();
        public readonly Dictionary<string, int> usesLeft = new Dictionary<string, int>();
        public readonly List<string> statuses = new List<string>();

        public string Id { get { return data.id; } }
        public string DisplayName { get { return data.displayName; } }
        public Faction Faction { get { return data.faction; } }
        public int Proficiency { get { return data.maxHp >= 36 ? 2 : 1; } }

        public CombatantRuntime(CombatantData source, Vector2Int startCell)
        {
            data = source;
            currentCell = startCell;
            hp = source.maxHp;
            inner = source.maxInner;
            morale = 60;
            breakGauge = 0;
            actions.ResetForTurn(source.movement);

            foreach (SkillData skill in source.skills)
            {
                if (skill == null)
                {
                    continue;
                }

                cooldowns[skill.id] = 0;
                if (skill.usesPerBattle > 0)
                {
                    usesLeft[skill.id] = skill.usesPerBattle;
                }
            }
        }

        public int StatModifier(StatType stat)
        {
            return data.stats.Modifier(stat);
        }

        public bool HasStatus(string status)
        {
            return statuses.Contains(status);
        }

        public void AddStatus(string status)
        {
            if (!statuses.Contains(status))
            {
                statuses.Add(status);
            }
        }

        public void ApplyDamage(int amount)
        {
            hp = Mathf.Max(0, hp - Mathf.Max(0, amount));
            if (hp == 0)
            {
                defeated = true;
                morale = 0;
                breakGauge = 100;
            }
        }

        public void Heal(int amount)
        {
            hp = Mathf.Min(data.maxHp, hp + Mathf.Max(0, amount));
        }

        public void StartTurn()
        {
            actions.ResetForTurn(data.movement);
            List<string> keys = new List<string>(cooldowns.Keys);
            foreach (string key in keys)
            {
                cooldowns[key] = Mathf.Max(0, cooldowns[key] - 1);
            }
        }
    }
}
