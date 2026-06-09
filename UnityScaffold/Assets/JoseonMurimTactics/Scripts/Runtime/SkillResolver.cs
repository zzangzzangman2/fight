using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class SkillResolver
{
    private readonly DiceRoller dice;
    private readonly MovementResolver movement;
    private readonly LineOfSightResolver lineOfSight;
    private readonly CombatLog log;

    public SkillResolver(DiceRoller dice, MovementResolver movement, LineOfSightResolver lineOfSight, CombatLog log)
    {
        this.dice = dice;
        this.movement = movement;
        this.lineOfSight = lineOfSight;
        this.log = log;
    }

    public bool CanUseSkill(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        return string.IsNullOrEmpty(UseSkillFailureReason(actor, skill, target));
    }

    public BattleForecastData GetSkillPreview(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        BattleForecastData preview = new BattleForecastData();
        preview.failureReason = UseSkillFailureReason(actor, skill, target);
        preview.canUseSkill = string.IsNullOrEmpty(preview.failureReason);

        if (actor == null || skill == null || target == null || !IsAttackSkill(skill))
        {
            return preview;
        }

        FillAttackForecast(preview, actor, skill, target, false);

        Vector2Int counterCell = target.currentCell;
        bool targetCanCounterAfterHit = true;
        if (skill.pushDistance > 0)
        {
            preview.willPushOnHit = true;
            preview.pushDistance = skill.pushDistance;
            Vector2Int direction = movement.DirectionFromTo(actor.currentCell, target.currentCell);
            movement.TryProjectPush(target.currentCell, direction, skill.pushDistance, out preview.pushDestination,
                                    out preview.pushCausesFall, out preview.pushBlocked);
            counterCell = preview.pushDestination;
            targetCanCounterAfterHit = !preview.pushCausesFall;
        }

        if (targetCanCounterAfterHit)
        {
            SkillData counterSkill = FindCounterSkillAtCell(target, actor, counterCell);
            if (counterSkill != null)
            {
                preview.willCounter = true;
                preview.counterSkill = counterSkill;
                FillAttackForecast(preview, target, counterCell, counterSkill, actor, actor.currentCell, true);
            }
        }

        return preview;
    }

    public bool CheckIfTargetCanCounter(CombatantRuntime target, CombatantRuntime actor)
    {
        return FindCounterSkill(target, actor) != null;
    }

    public SkillData FindCounterSkill(CombatantRuntime defender, CombatantRuntime attacker)
    {
        return FindCounterSkillAtCell(defender, attacker, defender == null ? Vector2Int.zero : defender.currentCell);
    }

    private SkillData FindCounterSkillAtCell(CombatantRuntime defender, CombatantRuntime attacker,
                                             Vector2Int defenderCell)
    {
        SkillData counterSkill;
        return TryFindCounterSkill(defender, attacker, defenderCell, out counterSkill) ? counterSkill : null;
    }

    private string UseSkillFailureReason(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        if (actor == null)
        {
            return "사용자 없음";
        }

        if (skill == null)
        {
            return "무공 없음";
        }

        if (actor.defeated || actor.surrendered)
        {
            return "행동 불가";
        }

        if (!actor.actions.CanSpend(skill.actionSlot))
        {
            return "행동 자원 부족";
        }

        if (actor.inner < skill.innerCost)
        {
            return "내공 부족";
        }

        if (actor.cooldowns.ContainsKey(skill.id) && actor.cooldowns[skill.id] > 0)
        {
            return "재사용 대기";
        }

        if (actor.usesLeft.ContainsKey(skill.id) && actor.usesLeft[skill.id] <= 0)
        {
            return "사용 횟수 소진";
        }

        if (target != null && skill.targetType != TargetType.Self)
        {
            int distance = movement.GridDistance(actor.currentCell, target.currentCell);
            if (distance > skill.range)
            {
                return "사거리 밖";
            }

            if (!lineOfSight.HasLineOfSight(actor.currentCell, target.currentCell))
            {
                return "시야 차단";
            }
        }
        else if (target == null && IsAttackSkill(skill))
        {
            return "대상 없음";
        }

        return null;
    }

    public void UseSkill(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        string failureReason = UseSkillFailureReason(actor, skill, target);
        if (!string.IsNullOrEmpty(failureReason))
        {
            string actorName = actor == null ? "알 수 없음" : actor.DisplayName;
            string skillName = skill == null ? "무공" : skill.displayName;
            log.Add("Skill", actorName + " " + skillName + " 사용 불가: " + failureReason + ".");
            return;
        }

        SpendCost(actor, skill);

        if (skill.tags.Contains(SkillTag.Heal))
        {
            ResolveHeal(actor, skill, target ?? actor);
            return;
        }

        if (skill.targetType == TargetType.Self || skill.tags.Contains(SkillTag.Movement) ||
            skill.tags.Contains(SkillTag.Stance))
        {
            ResolveSelfSkill(actor, skill);
            return;
        }

        if (target == null)
        {
            log.Add("Skill", skill.displayName + " 대상 없음.");
            return;
        }

        ResolveAttack(actor, skill, target, false);
    }

    private void ResolveAttack(CombatantRuntime actor, SkillData skill, CombatantRuntime target, bool isCounter)
    {
        RollMode mode = ResolveRollMode(actor, skill, target);
        DiceRoll d20 = dice.RollD20(mode);
        int distance = movement.GridDistance(actor.currentCell, target.currentCell);
        int statMod = actor.StatModifier(skill.stat);
        int terrainBonus = TerrainAttackBonus(actor, target, skill);
        int total = d20.total + statMod + actor.Proficiency + skill.attackBonus + terrainBonus;
        int defense = target.data.armorClass + lineOfSight.CoverBonus(target.currentCell, distance);
        bool critical = d20.natural == 20;
        bool fumble = d20.natural == 1;
        bool hit = critical || (!fumble && total >= defense);

        log.Add("Dice", actor.DisplayName + " " + skill.displayName + ": d20 " + d20.detail + " + 스탯 " + statMod +
                            " + 숙련 " + actor.Proficiency + " + 무공 " + skill.attackBonus + " + 지형 " +
                            terrainBonus + " = " + total + " vs 방어 " + defense);

        if (fumble)
        {
            actor.morale = Mathf.Max(0, actor.morale - 8);
            actor.AddStatus("초식흐트러짐");
            log.Add("Fumble", actor.DisplayName + " 대실패. 기세 -8.");
            ResolveCounterIfPossible(target, actor, isCounter);
            return;
        }

        if (!hit)
        {
            log.Add("Miss", target.DisplayName + " 방어 성공.");
            ResolveCounterIfPossible(target, actor, isCounter);
            return;
        }

        DiceRoll damageDice = dice.RollDice(skill.damageDice, critical ? 2 : 1);
        int damage = Mathf.Max(1, damageDice.total + statMod);
        target.ApplyDamage(damage);
        target.breakGauge = Mathf.Min(100, target.breakGauge + skill.breakGain + (critical ? 12 : 0));
        target.morale = Mathf.Max(0, target.morale - Mathf.Max(3, skill.moraleDamage));

        if (skill.tags.Contains(SkillTag.Poison))
        {
            target.AddStatus("중독");
        }

        if (skill.tags.Contains(SkillTag.Ice))
        {
            target.AddStatus("둔화");
        }

        if (skill.breakGain > 0 && target.breakGauge >= 100)
        {
            target.AddStatus("파훼");
        }

        log.Add("Hit", target.DisplayName + " 피해 " + damage + ", 파훼 " + target.breakGauge + "/100.");

        if (target.hp > 0 && skill.pushDistance > 0)
        {
            ResolvePush(actor, skill, target);
        }

        ResolveCounterIfPossible(target, actor, isCounter);
    }

    private void ResolveHeal(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        DiceRoll healDice = dice.RollDice(string.IsNullOrEmpty(skill.healDice) ? "1d6" : skill.healDice);
        int amount = Mathf.Max(1, healDice.total + actor.StatModifier(skill.stat));
        target.Heal(amount);
        target.statuses.Remove("중독");
        target.statuses.Remove("화상");
        log.Add("Heal",
                actor.DisplayName + " " + skill.displayName + ": " + target.DisplayName + " 회복 " + amount + ".");
    }

    private void ResolveSelfSkill(CombatantRuntime actor, SkillData skill)
    {
        if (skill.tags.Contains(SkillTag.Movement))
        {
            actor.actions.movementLeft += 2;
        }

        if (skill.tags.Contains(SkillTag.Stance))
        {
            actor.AddStatus("반격예약");
        }

        if (skill.tags.Contains(SkillTag.Stealth))
        {
            actor.AddStatus("은신");
        }

        if (skill.tags.Contains(SkillTag.Formation))
        {
            actor.morale = Mathf.Min(100, actor.morale + 10);
        }

        log.Add("Skill", actor.DisplayName + " " + skill.displayName + " 발동.");
    }

    private void SpendCost(CombatantRuntime actor, SkillData skill)
    {
        actor.actions.Spend(skill.actionSlot);
        actor.inner = Mathf.Max(0, actor.inner - skill.innerCost);
        if (skill.cooldown > 0)
        {
            actor.cooldowns[skill.id] = skill.cooldown;
        }

        if (actor.usesLeft.ContainsKey(skill.id))
        {
            actor.usesLeft[skill.id]--;
        }
    }

    private RollMode ResolveRollMode(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        return ResolveRollMode(actor, actor.currentCell, skill, target, target.currentCell);
    }

    private RollMode ResolveRollMode(CombatantRuntime actor, Vector2Int actorCell, SkillData skill,
                                     CombatantRuntime target, Vector2Int targetCell)
    {
        int score = 0;
        int fromElevation = movement.GetElevation(actorCell);
        int toElevation = movement.GetElevation(targetCell);

        if (fromElevation > toElevation)
        {
            score++;
        }

        if (actor.HasStatus("은신") && skill.tags.Contains(SkillTag.Poison))
        {
            score++;
        }

        if (target.HasStatus("파훼") || target.HasStatus("간파"))
        {
            score++;
        }

        if (movement.GetTerrainType(targetCell) == TerrainType.Water && skill.tags.Contains(SkillTag.Ice))
        {
            score++;
        }

        if (movement.GetCoverType(targetCell) == CoverType.Heavy && skill.range > 1)
        {
            score--;
        }

        if (actor.HasStatus("넘어짐") || actor.HasStatus("중독") || actor.HasStatus("연막불리"))
        {
            score--;
        }

        if (score > 0)
        {
            return RollMode.Advantage;
        }

        return score < 0 ? RollMode.Disadvantage : RollMode.Normal;
    }

    private int TerrainAttackBonus(CombatantRuntime actor, CombatantRuntime target, SkillData skill)
    {
        return TerrainAttackBonus(actor, actor.currentCell, target, target.currentCell, skill);
    }

    private int TerrainAttackBonus(CombatantRuntime actor, Vector2Int actorCell, CombatantRuntime target,
                                   Vector2Int targetCell, SkillData skill)
    {
        int bonus = 0;

        if (movement.GetElevation(actorCell) > movement.GetElevation(targetCell))
        {
            bonus += 2;
        }

        if (movement.GetTerrainType(targetCell) == TerrainType.Water && skill.tags.Contains(SkillTag.Ice))
        {
            bonus += 2;
        }

        if (target.HasStatus("파훼") || target.HasStatus("간파"))
        {
            bonus += 2;
        }

        return bonus;
    }

    private void ResolvePush(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
    {
        Vector2Int direction = movement.DirectionFromTo(actor.currentCell, target.currentCell);
        Vector2Int destination;
        bool fell;
        bool blocked;
        bool moved = movement.TryProjectPush(target.currentCell, direction, skill.pushDistance, out destination,
                                             out fell, out blocked);

        if (!moved && blocked)
        {
            ApplyPushImpact(target, skill.pushDistance);
            return;
        }

        if (!moved)
        {
            log.Add("Push", target.DisplayName + " 밀치기 실패: 방향 없음.");
            return;
        }

        target.currentCell = destination;
        log.Add("Push", target.DisplayName + "이(가) " + skill.pushDistance + "칸 밀려났습니다.");

        if (fell)
        {
            target.ApplyDamage(target.hp);
            log.Add("Fall", target.DisplayName + " 낙하. 전투 불능.");
            return;
        }

        if (blocked)
        {
            ApplyPushImpact(target, skill.pushDistance);
        }
    }

    private void ApplyPushImpact(CombatantRuntime target, int pushDistance)
    {
        int impactDamage = Mathf.Max(1, pushDistance);
        target.ApplyDamage(impactDamage);
        target.AddStatus("넘어짐");
        log.Add("Impact", target.DisplayName + " 장애물 충돌. 피해 " + impactDamage + ", 넘어짐.");
    }

    private void ResolveCounterIfPossible(CombatantRuntime defender, CombatantRuntime attacker, bool sourceWasCounter)
    {
        if (sourceWasCounter)
        {
            return;
        }

        SkillData counterSkill = FindCounterSkill(defender, attacker);
        if (counterSkill == null)
        {
            return;
        }

        log.Add("Counter", defender.DisplayName + "의 반격!");
        SpendCounterCost(defender, counterSkill);
        ResolveAttack(defender, counterSkill, attacker, true);
    }

    private bool TryFindCounterSkill(CombatantRuntime defender, CombatantRuntime attacker, Vector2Int defenderCell,
                                     out SkillData counterSkill)
    {
        counterSkill = null;
        if (defender == null || attacker == null || defender.defeated || defender.surrendered ||
            !defender.actions.CanSpend(ActionSlot.Reaction))
        {
            return false;
        }

        SkillData fallback = null;
        foreach (SkillData skill in defender.data.skills)
        {
            if (!IsAttackSkill(skill) || !CanPayCounterCost(defender, skill) ||
                !CanReachWithSkill(defender, defenderCell, skill, attacker, attacker.currentCell))
            {
                continue;
            }

            if (skill.actionSlot == ActionSlot.Reaction)
            {
                counterSkill = skill;
                return true;
            }

            if (fallback == null)
            {
                fallback = skill;
            }
        }

        counterSkill = fallback;
        return counterSkill != null;
    }

    private bool CanPayCounterCost(CombatantRuntime defender, SkillData skill)
    {
        if (defender.inner < skill.innerCost)
        {
            return false;
        }

        if (defender.cooldowns.ContainsKey(skill.id) && defender.cooldowns[skill.id] > 0)
        {
            return false;
        }

        return !defender.usesLeft.ContainsKey(skill.id) || defender.usesLeft[skill.id] > 0;
    }

    private bool CanReachWithSkill(CombatantRuntime actor, Vector2Int actorCell, SkillData skill,
                                   CombatantRuntime target, Vector2Int targetCell)
    {
        int distance = movement.GridDistance(actorCell, targetCell);
        return distance <= skill.range && lineOfSight.HasLineOfSight(actorCell, targetCell);
    }

    private void SpendCounterCost(CombatantRuntime actor, SkillData skill)
    {
        actor.actions.Spend(ActionSlot.Reaction);
        actor.inner = Mathf.Max(0, actor.inner - skill.innerCost);
        if (skill.cooldown > 0)
        {
            actor.cooldowns[skill.id] = skill.cooldown;
        }

        if (actor.usesLeft.ContainsKey(skill.id))
        {
            actor.usesLeft[skill.id]--;
        }
    }

    private bool IsAttackSkill(SkillData skill)
    {
        return skill != null && skill.targetType != TargetType.Self && !skill.tags.Contains(SkillTag.Heal) &&
               !skill.tags.Contains(SkillTag.Movement) && !skill.tags.Contains(SkillTag.Stance) &&
               !skill.tags.Contains(SkillTag.Formation) && !skill.tags.Contains(SkillTag.Social);
    }

    private void FillAttackForecast(BattleForecastData preview, CombatantRuntime actor, SkillData skill,
                                    CombatantRuntime target, bool counter)
    {
        FillAttackForecast(preview, actor, actor.currentCell, skill, target, target.currentCell, counter);
    }

    private void FillAttackForecast(BattleForecastData preview, CombatantRuntime actor, Vector2Int actorCell,
                                    SkillData skill, CombatantRuntime target, Vector2Int targetCell, bool counter)
    {
        int distance = movement.GridDistance(actorCell, targetCell);
        int statMod = actor.StatModifier(skill.stat);
        int terrainBonus = TerrainAttackBonus(actor, actorCell, target, targetCell, skill);
        int attackBonus = statMod + actor.Proficiency + skill.attackBonus + terrainBonus;
        int defense = target.data.armorClass + lineOfSight.CoverBonus(targetCell, distance);
        int requiredRoll = defense - attackBonus;
        RollMode rollMode = ResolveRollMode(actor, actorCell, skill, target, targetCell);
        int hitChancePercent = CalculateHitChancePercent(requiredRoll, rollMode);
        int minDamage;
        int maxDamage;
        int criticalMinDamage;
        int criticalMaxDamage;
        GetDamageRange(skill.damageDice, statMod, 1, out minDamage, out maxDamage);
        GetDamageRange(skill.damageDice, statMod, 2, out criticalMinDamage, out criticalMaxDamage);

        if (counter)
        {
            preview.counterDistance = distance;
            preview.counterHitChancePercent = hitChancePercent;
            preview.counterMinDamage = minDamage;
            preview.counterMaxDamage = maxDamage;
            return;
        }

        preview.distance = distance;
        preview.rollMode = rollMode;
        preview.attackBonus = attackBonus;
        preview.targetDefense = defense;
        preview.requiredRoll = requiredRoll;
        preview.hitChancePercent = hitChancePercent;
        preview.minDamage = minDamage;
        preview.maxDamage = maxDamage;
        preview.criticalMinDamage = criticalMinDamage;
        preview.criticalMaxDamage = criticalMaxDamage;
    }

    private int CalculateHitChancePercent(int requiredRoll, RollMode rollMode)
    {
        int successes = 0;
        int totalRolls = 0;

        if (rollMode == RollMode.Normal)
        {
            for (int roll = 1; roll <= 20; roll++)
            {
                totalRolls++;
                if (IsD20Hit(roll, requiredRoll))
                {
                    successes++;
                }
            }
        }
        else
        {
            for (int first = 1; first <= 20; first++)
            {
                for (int second = 1; second <= 20; second++)
                {
                    totalRolls++;
                    int roll = rollMode == RollMode.Advantage ? Mathf.Max(first, second) : Mathf.Min(first, second);
                    if (IsD20Hit(roll, requiredRoll))
                    {
                        successes++;
                    }
                }
            }
        }

        return Mathf.RoundToInt(successes * 100f / totalRolls);
    }

    private bool IsD20Hit(int naturalRoll, int requiredRoll)
    {
        if (naturalRoll == 1)
        {
            return false;
        }

        if (naturalRoll == 20)
        {
            return true;
        }

        return naturalRoll >= requiredRoll;
    }

    private void GetDamageRange(string expression, int statMod, int multiplier, out int minDamage, out int maxDamage)
    {
        int count;
        int sides;
        if (!TryParseDice(expression, out count, out sides))
        {
            minDamage = Mathf.Max(1, statMod);
            maxDamage = Mathf.Max(1, statMod);
            return;
        }

        count *= Mathf.Max(1, multiplier);
        minDamage = Mathf.Max(1, count + statMod);
        maxDamage = Mathf.Max(1, count * sides + statMod);
    }

    private bool TryParseDice(string expression, out int count, out int sides)
    {
        count = 0;
        sides = 0;
        if (string.IsNullOrEmpty(expression))
        {
            return false;
        }

        int separator = expression.IndexOf('d');
        if (separator < 1 || separator >= expression.Length - 1)
        {
            return false;
        }

        if (!int.TryParse(expression.Substring(0, separator), out count))
        {
            return false;
        }

        if (!int.TryParse(expression.Substring(separator + 1), out sides))
        {
            return false;
        }

        return count > 0 && sides > 0;
    }
}
}
