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
    public EquipmentBonus equipmentBonus;
    public readonly ActionEconomy actions = new ActionEconomy();
    public readonly Dictionary<string, int> cooldowns = new Dictionary<string, int>();
    public readonly Dictionary<string, int> usesLeft = new Dictionary<string, int>();
    public readonly List<string> statuses = new List<string>();

    public string Id
    {
        get {
            return data.id;
        }
    }
    public string DisplayName
    {
        get {
            return data.displayName;
        }
    }
    public Faction Faction
    {
        get {
            return data.faction;
        }
    }
    public int Proficiency
    {
        get {
            return data.maxHp >= 36 ? 2 : 1;
        }
    }
    public int MaxHp
    {
        get {
            return Mathf.Max(1, data.maxHp + equipmentBonus.hp);
        }
    }
    public int MaxInner
    {
        get {
            return Mathf.Max(0, data.maxInner + equipmentBonus.inner);
        }
    }
    public int ArmorClass
    {
        get {
            return Mathf.Max(0, data.armorClass + equipmentBonus.guard);
        }
    }
    public int Movement
    {
        get {
            return Mathf.Max(0, data.movement + equipmentBonus.move);
        }
    }
    public int AttackBonus
    {
        get {
            return equipmentBonus.acc;
        }
    }
    public int DamageBonus
    {
        get {
            return equipmentBonus.atk;
        }
    }

    public CombatantRuntime(CombatantData source, Vector2Int startCell)
        : this(source, startCell, new EquipmentBonus())
    {
    }

    public CombatantRuntime(CombatantData source, Vector2Int startCell, EquipmentBonus equipmentBonus)
    {
        data = source;
        currentCell = startCell;
        this.equipmentBonus = equipmentBonus;
        hp = MaxHp;
        inner = MaxInner;
        morale = 60;
        breakGauge = 0;
        actions.ResetForTurn(Movement);

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
        hp = Mathf.Min(MaxHp, hp + Mathf.Max(0, amount));
    }

    public void StartTurn()
    {
        actions.ResetForTurn(Movement);
        List<string> keys = new List<string>(cooldowns.Keys);
        foreach (string key in keys)
        {
            cooldowns[key] = Mathf.Max(0, cooldowns[key] - 1);
        }
    }
}
}
