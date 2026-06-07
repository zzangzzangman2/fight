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
            if (actor.defeated || actor.surrendered || skill == null)
            {
                return false;
            }

            if (!actor.actions.CanSpend(skill.actionSlot))
            {
                return false;
            }

            if (actor.inner < skill.innerCost)
            {
                return false;
            }

            if (actor.cooldowns.ContainsKey(skill.id) && actor.cooldowns[skill.id] > 0)
            {
                return false;
            }

            if (actor.usesLeft.ContainsKey(skill.id) && actor.usesLeft[skill.id] <= 0)
            {
                return false;
            }

            if (target != null && skill.targetType != TargetType.Self)
            {
                int distance = movement.Distance(actor.currentNodeId, target.currentNodeId);
                if (distance == int.MaxValue || distance > skill.range)
                {
                    return false;
                }

                if (!lineOfSight.HasLineOfSight(actor.currentNodeId, target.currentNodeId))
                {
                    return false;
                }
            }

            return true;
        }

        public void UseSkill(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
        {
            if (!CanUseSkill(actor, skill, target))
            {
                log.Add("Skill", actor.DisplayName + " " + skill.displayName + " 사용 불가.");
                return;
            }

            SpendCost(actor, skill);

            if (skill.tags.Contains(SkillTag.Heal))
            {
                ResolveHeal(actor, skill, target ?? actor);
                return;
            }

            if (skill.targetType == TargetType.Self || skill.tags.Contains(SkillTag.Movement) || skill.tags.Contains(SkillTag.Stance))
            {
                ResolveSelfSkill(actor, skill);
                return;
            }

            if (target == null)
            {
                log.Add("Skill", skill.displayName + " 대상 없음.");
                return;
            }

            ResolveAttack(actor, skill, target);
        }

        private void ResolveAttack(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
        {
            RollMode mode = ResolveRollMode(actor, skill, target);
            DiceRoll d20 = dice.RollD20(mode);
            int distance = movement.Distance(actor.currentNodeId, target.currentNodeId);
            int statMod = actor.StatModifier(skill.stat);
            int terrainBonus = TerrainAttackBonus(actor, target, skill);
            int total = d20.total + statMod + actor.Proficiency + skill.attackBonus + terrainBonus;
            int defense = target.data.armorClass + lineOfSight.CoverBonus(target.currentNodeId, distance);
            bool critical = d20.natural == 20;
            bool fumble = d20.natural == 1;
            bool hit = critical || (!fumble && total >= defense);

            log.Add("Dice", actor.DisplayName + " " + skill.displayName + ": d20 " + d20.detail + " + 스탯 " + statMod + " + 숙련 " + actor.Proficiency + " + 무공 " + skill.attackBonus + " + 지형 " + terrainBonus + " = " + total + " vs 방어 " + defense);

            if (fumble)
            {
                actor.morale = Mathf.Max(0, actor.morale - 8);
                actor.AddStatus("초식흐트러짐");
                log.Add("Fumble", actor.DisplayName + " 대실패. 기세 -8.");
                return;
            }

            if (!hit)
            {
                log.Add("Miss", target.DisplayName + " 방어 성공.");
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
        }

        private void ResolveHeal(CombatantRuntime actor, SkillData skill, CombatantRuntime target)
        {
            DiceRoll healDice = dice.RollDice(string.IsNullOrEmpty(skill.healDice) ? "1d6" : skill.healDice);
            int amount = Mathf.Max(1, healDice.total + actor.StatModifier(skill.stat));
            target.Heal(amount);
            target.statuses.Remove("중독");
            target.statuses.Remove("화상");
            log.Add("Heal", actor.DisplayName + " " + skill.displayName + ": " + target.DisplayName + " 회복 " + amount + ".");
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
            int score = 0;
            BattleNodeData from = movement.FindNode(actor.currentNodeId);
            BattleNodeData to = movement.FindNode(target.currentNodeId);

            if (from != null && to != null && from.elevation > to.elevation)
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

            if (to != null && to.terrainType == TerrainType.Water && skill.tags.Contains(SkillTag.Ice))
            {
                score++;
            }

            if (to != null && to.coverType == CoverType.Heavy && skill.range > 1)
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
            BattleNodeData from = movement.FindNode(actor.currentNodeId);
            BattleNodeData to = movement.FindNode(target.currentNodeId);
            int bonus = 0;

            if (from != null && to != null && from.elevation > to.elevation)
            {
                bonus += 2;
            }

            if (to != null && to.terrainType == TerrainType.Water && skill.tags.Contains(SkillTag.Ice))
            {
                bonus += 2;
            }

            if (target.HasStatus("파훼") || target.HasStatus("간파"))
            {
                bonus += 2;
            }

            return bonus;
        }
    }
}
