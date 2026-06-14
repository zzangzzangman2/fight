using UnityEngine;

namespace JoseonMurimTactics
{
public enum MurimDerivedStatType
{
    Vitality,
    Inner,
    OuterPower,
    OuterGuard,
    InnerPower,
    MindGuard,
    Speed,
    Movement,
    Accuracy,
    Evasion,
    Critical
}

public struct MurimDerivedStats
{
    public int vitality;
    public int inner;
    public int outerPower;
    public int outerGuard;
    public int innerPower;
    public int mindGuard;
    public int speed;
    public int movement;
    public int accuracy;
    public int evasion;
    public int critical;

    public int Get(MurimDerivedStatType type)
    {
        switch (type)
        {
            case MurimDerivedStatType.Vitality: return vitality;
            case MurimDerivedStatType.Inner: return inner;
            case MurimDerivedStatType.OuterPower: return outerPower;
            case MurimDerivedStatType.OuterGuard: return outerGuard;
            case MurimDerivedStatType.InnerPower: return innerPower;
            case MurimDerivedStatType.MindGuard: return mindGuard;
            case MurimDerivedStatType.Speed: return speed;
            case MurimDerivedStatType.Movement: return movement;
            case MurimDerivedStatType.Accuracy: return accuracy;
            case MurimDerivedStatType.Evasion: return evasion;
            case MurimDerivedStatType.Critical: return critical;
            default: return 0;
        }
    }
}

public static class MurimStatFormula
{
    public const string HpKey = "hp";
    public const string InnerKey = "inner";
    public const string StrengthKey = "strength";
    public const string AgilityKey = "agility";
    public const string InnerPowerKey = "innerPower";
    public const string SpiritKey = "spirit";
    public const string InsightKey = "insight";
    public const string CharmKey = "charm";

    public static SixStats EffectiveStats(CombatantData data, CharacterProgressState progress)
    {
        SixStats baseStats = data == null ? new SixStats() : data.stats;
        SixStats bonus = progress == null ? new SixStats() : progress.statBonuses;
        return new SixStats
        {
            strength = baseStats.strength + bonus.strength,
            agility = baseStats.agility + bonus.agility,
            innerPower = baseStats.innerPower + bonus.innerPower,
            spirit = baseStats.spirit + bonus.spirit,
            insight = baseStats.insight + bonus.insight,
            charm = baseStats.charm + bonus.charm
        };
    }

    public static MurimDerivedStats Build(CombatantData data, CharacterProgressState progress,
                                          EquipmentBonus equipment)
    {
        SixStats stats = EffectiveStats(data, progress);
        int hpBase = data == null ? 1 : data.maxHp;
        int innerBase = data == null ? 0 : data.maxInner;
        int guardBase = data == null ? 10 : data.armorClass;
        int moveBase = data == null ? 4 : data.movement;
        int hpBonus = progress == null ? 0 : progress.hpBonus;
        int innerBonus = progress == null ? 0 : progress.innerBonus;

        MurimDerivedStats result = new MurimDerivedStats();
        result.vitality = Mathf.Max(1, hpBase + hpBonus + equipment.hp + Positive(stats.strength) * 2 +
                                    Positive(stats.spirit));
        result.inner = Mathf.Max(0, innerBase + innerBonus + equipment.inner + Positive(stats.innerPower) / 3);
        result.outerPower = Mathf.Max(1, stats.strength + stats.agility / 2 + equipment.atk * 2);
        result.outerGuard = Mathf.Max(1, guardBase + stats.spirit / 2 + stats.strength / 3 + equipment.guard * 2);
        result.innerPower = Mathf.Max(1, stats.innerPower + stats.insight / 2 + equipment.inner);
        result.mindGuard = Mathf.Max(1, stats.spirit + stats.innerPower / 2 + stats.insight / 3 + equipment.guard);
        result.speed = Mathf.Max(1, stats.agility + stats.insight / 2);
        result.movement = Mathf.Max(1, moveBase + equipment.move + ThresholdBonus(stats.agility, 16, 22));
        result.accuracy = Mathf.Max(1, stats.insight + stats.agility / 2 + equipment.acc * 2);
        result.evasion = Mathf.Max(1, stats.agility + stats.spirit / 2);
        result.critical = Mathf.Clamp(stats.insight / 3 + stats.agility / 4 + equipment.acc, 0, 18);
        return result;
    }

    public static bool IsInnerSkill(SkillData skill)
    {
        return skill != null &&
               skill.tags != null &&
               (skill.stat == StatType.InnerPower ||
                skill.tags.Contains(SkillTag.Light) ||
                skill.tags.Contains(SkillTag.Fire) ||
                skill.tags.Contains(SkillTag.Ice) ||
                skill.tags.Contains(SkillTag.Lightning) ||
                skill.tags.Contains(SkillTag.Wind) ||
                skill.tags.Contains(SkillTag.Dark) ||
                skill.tags.Contains(SkillTag.Heal) ||
                skill.tags.Contains(SkillTag.Support) ||
                skill.tags.Contains(SkillTag.Debuff));
    }

    public static int AttackRollBonus(MurimDerivedStats stats)
    {
        return Mathf.Max(0, stats.accuracy / 10);
    }

    public static int DefenseRollBonus(MurimDerivedStats stats, SkillData incomingSkill)
    {
        int guard = IsInnerSkill(incomingSkill) ? stats.mindGuard : stats.outerGuard;
        return Mathf.Max(0, guard / 12 + stats.evasion / 18);
    }

    public static int SkillPowerBonus(MurimDerivedStats stats, SkillData skill)
    {
        int power = IsInnerSkill(skill) ? stats.innerPower : stats.outerPower;
        return Mathf.Max(0, power / 14);
    }

    public static int HealBonus(MurimDerivedStats stats)
    {
        return Mathf.Max(0, (stats.innerPower + stats.mindGuard) / 18);
    }

    public static int InitiativeBonus(MurimDerivedStats stats)
    {
        return Mathf.Max(0, stats.speed / 6);
    }

    public static int CriticalThreshold(MurimDerivedStats stats)
    {
        return Mathf.Clamp(20 - stats.critical / 7, 18, 20);
    }

    public static string DerivedLabel(MurimDerivedStatType type)
    {
        switch (type)
        {
            case MurimDerivedStatType.Vitality: return "기혈";
            case MurimDerivedStatType.Inner: return "내력";
            case MurimDerivedStatType.OuterPower: return "외공";
            case MurimDerivedStatType.OuterGuard: return "호체";
            case MurimDerivedStatType.InnerPower: return "내공";
            case MurimDerivedStatType.MindGuard: return "심법";
            case MurimDerivedStatType.Speed: return "신법";
            case MurimDerivedStatType.Movement: return "보법";
            case MurimDerivedStatType.Accuracy: return "안법";
            case MurimDerivedStatType.Evasion: return "회피";
            case MurimDerivedStatType.Critical: return "절초";
            default: return "-";
        }
    }

    public static string TrainingLabel(string key)
    {
        switch (key)
        {
            case HpKey: return "기혈";
            case InnerKey: return "내력";
            case StrengthKey: return "외공";
            case AgilityKey: return "신법";
            case InnerPowerKey: return "내공";
            case SpiritKey: return "심법";
            case InsightKey: return "안법";
            case CharmKey: return "풍류";
            default: return key;
        }
    }

    public static string TrainingDescription(string key)
    {
        switch (key)
        {
            case HpKey: return "맞고 버티는 근골과 회복력을 올린다.";
            case InnerKey: return "무공 사용에 필요한 내력 최대치를 올린다.";
            case StrengthKey: return "검격, 권각, 창격 같은 외문 피해를 올린다.";
            case AgilityKey: return "행동 순서, 이동, 회피 기반을 올린다.";
            case InnerPowerKey: return "기공, 속성술, 회복 무공의 위력을 올린다.";
            case SpiritKey: return "호체와 심법 저항, 사기 유지력을 올린다.";
            case InsightKey: return "명중, 치명, 전장 판독력을 올린다.";
            case CharmKey: return "동료 호응과 문파 운용의 설득력을 올린다.";
            default: return string.Empty;
        }
    }

    public static int ProgressBonus(CharacterProgressState progress, string key)
    {
        if (progress == null)
        {
            return 0;
        }

        switch (key)
        {
            case HpKey: return progress.hpBonus;
            case InnerKey: return progress.innerBonus;
            case StrengthKey: return progress.statBonuses.strength;
            case AgilityKey: return progress.statBonuses.agility;
            case InnerPowerKey: return progress.statBonuses.innerPower;
            case SpiritKey: return progress.statBonuses.spirit;
            case InsightKey: return progress.statBonuses.insight;
            case CharmKey: return progress.statBonuses.charm;
            default: return 0;
        }
    }

    private static int Positive(int stat)
    {
        return Mathf.Max(0, stat - 10);
    }

    private static int ThresholdBonus(int value, int first, int second)
    {
        int bonus = value >= first ? 1 : 0;
        if (value >= second)
        {
            bonus++;
        }

        return bonus;
    }
}
}
