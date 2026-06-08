using System.Collections.Generic;
using UnityEngine;

namespace JoseonMurimTactics
{
    public enum BattlePhase
    {
        PlayerPhase,
        EnemyPhase,
        Victory,
        Defeat
    }

    public enum SkillStyle
    {
        Sword,
        Blade,
        Spear,
        Palm,
        HiddenWeapon,
        Poison,
        Ice,
        Mind
    }

    public enum StyleAdvantage
    {
        Disadvantage,
        Neutral,
        Advantage
    }

    public enum BattleOutcome
    {
        Ongoing,
        Victory,
        Defeat
    }

    public readonly struct BattleForecast
    {
        public readonly bool valid;
        public readonly string actorName;
        public readonly string targetName;
        public readonly string actionName;
        public readonly int requiredD20;
        public readonly int hitPercent;
        public readonly int damageMin;
        public readonly int damageMax;
        public readonly int critPercent;
        public readonly int breakGain;
        public readonly StyleAdvantage styleAdvantage;
        public readonly int terrainBonus;
        public readonly bool counterPossible;
        public readonly int counterDamageMin;
        public readonly int counterDamageMax;
        public readonly string reason;

        public BattleForecast(
            bool valid,
            string actorName,
            string targetName,
            string actionName,
            int requiredD20,
            int hitPercent,
            int damageMin,
            int damageMax,
            int critPercent,
            int breakGain,
            StyleAdvantage styleAdvantage,
            int terrainBonus,
            bool counterPossible,
            int counterDamageMin,
            int counterDamageMax,
            string reason)
        {
            this.valid = valid;
            this.actorName = actorName;
            this.targetName = targetName;
            this.actionName = actionName;
            this.requiredD20 = requiredD20;
            this.hitPercent = hitPercent;
            this.damageMin = damageMin;
            this.damageMax = damageMax;
            this.critPercent = critPercent;
            this.breakGain = breakGain;
            this.styleAdvantage = styleAdvantage;
            this.terrainBonus = terrainBonus;
            this.counterPossible = counterPossible;
            this.counterDamageMin = counterDamageMin;
            this.counterDamageMax = counterDamageMax;
            this.reason = reason;
        }
    }

    public sealed class PhaseTurnController
    {
        public BattlePhase Phase { get; private set; } = BattlePhase.PlayerPhase;
        public int Round { get; private set; } = 1;

        public void StartBattle(IReadOnlyList<BattleTestUnit> units)
        {
            Round = 1;
            Phase = BattlePhase.PlayerPhase;
            BeginPlayerPhase(units);
        }

        public void BeginPlayerPhase(IReadOnlyList<BattleTestUnit> units)
        {
            Phase = BattlePhase.PlayerPhase;
            ResetFaction(units, Faction.Ally);
        }

        public void BeginEnemyPhase(IReadOnlyList<BattleTestUnit> units)
        {
            Phase = BattlePhase.EnemyPhase;
            ResetFaction(units, Faction.Enemy);
        }

        public void FinishEnemyPhase(IReadOnlyList<BattleTestUnit> units)
        {
            Round++;
            BeginPlayerPhase(units);
        }

        public void MarkUnitFinished(BattleTestUnit unit)
        {
            if (unit == null)
            {
                return;
            }

            unit.turnEnded = true;
            unit.moved = true;
            unit.acted = true;
        }

        public bool AllFactionUnitsFinished(IReadOnlyList<BattleTestUnit> units, Faction faction)
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (!unit.defeated && unit.definition.faction == faction && !unit.turnEnded)
                {
                    return false;
                }
            }

            return true;
        }

        public void SetOutcome(BattleOutcome outcome)
        {
            if (outcome == BattleOutcome.Victory)
            {
                Phase = BattlePhase.Victory;
            }
            else if (outcome == BattleOutcome.Defeat)
            {
                Phase = BattlePhase.Defeat;
            }
        }

        private void ResetFaction(IReadOnlyList<BattleTestUnit> units, Faction faction)
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (!unit.defeated && unit.definition.faction == faction)
                {
                    unit.turnEnded = false;
                    unit.moved = false;
                    unit.acted = false;
                    unit.guarded = false;
                }
            }
        }
    }

    public sealed class UnitSelectionController
    {
        public BattleTestUnit SelectedUnit { get; private set; }

        public bool TrySelect(BattleTestUnit unit)
        {
            if (unit == null || unit.defeated || unit.turnEnded || unit.definition.faction != Faction.Ally)
            {
                return false;
            }

            SelectedUnit = unit;
            return true;
        }

        public BattleTestUnit SelectFirstReadyAlly(IReadOnlyList<BattleTestUnit> units)
        {
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (!unit.defeated && !unit.turnEnded && unit.definition.faction == Faction.Ally)
                {
                    SelectedUnit = unit;
                    return unit;
                }
            }

            SelectedUnit = null;
            return null;
        }
    }

    public sealed class BreakResolver
    {
        public StyleAdvantage Resolve(SkillStyle attacker, SkillStyle defender)
        {
            if (attacker == SkillStyle.Sword && defender == SkillStyle.Blade)
            {
                return StyleAdvantage.Advantage;
            }

            if (attacker == SkillStyle.Blade && defender == SkillStyle.Spear)
            {
                return StyleAdvantage.Advantage;
            }

            if (attacker == SkillStyle.Spear && defender == SkillStyle.Sword)
            {
                return StyleAdvantage.Advantage;
            }

            if (defender == SkillStyle.Sword && attacker == SkillStyle.Blade)
            {
                return StyleAdvantage.Disadvantage;
            }

            if (defender == SkillStyle.Blade && attacker == SkillStyle.Spear)
            {
                return StyleAdvantage.Disadvantage;
            }

            if (defender == SkillStyle.Spear && attacker == SkillStyle.Sword)
            {
                return StyleAdvantage.Disadvantage;
            }

            return StyleAdvantage.Neutral;
        }

        public int AttackModifier(SkillStyle attacker, SkillStyle defender)
        {
            StyleAdvantage advantage = Resolve(attacker, defender);
            if (advantage == StyleAdvantage.Advantage)
            {
                return 2;
            }

            return advantage == StyleAdvantage.Disadvantage ? -2 : 0;
        }

        public int BreakGain(SkillStyle attacker, SkillStyle defender, bool hit)
        {
            if (!hit)
            {
                return 0;
            }

            StyleAdvantage advantage = Resolve(attacker, defender);
            if (advantage == StyleAdvantage.Advantage)
            {
                return 25;
            }

            return advantage == StyleAdvantage.Disadvantage ? 5 : 12;
        }

        public void ApplyBreak(BattleTestUnit target, int amount)
        {
            if (target == null || target.defeated)
            {
                return;
            }

            target.breakGauge = Mathf.Min(100, target.breakGauge + Mathf.Max(0, amount));
            if (target.breakGauge >= 100)
            {
                target.broken = true;
            }
        }
    }

    public sealed class CounterattackService
    {
        public bool CanCounter(BattleTestUnit defender, BattleTestUnit attacker, int distance)
        {
            if (defender == null || attacker == null || defender.defeated)
            {
                return false;
            }

            if (!defender.definition.canCounter || defender.broken || defender.disarmed || defender.prone)
            {
                return false;
            }

            if (defender.counterSpent || distance > defender.definition.counterRange)
            {
                return false;
            }

            return defender.inner >= defender.definition.counterInnerCost;
        }
    }

    public sealed class BattleForecastService
    {
        private readonly BreakResolver breakResolver;
        private readonly CounterattackService counterattackService;

        public BattleForecastService(BreakResolver breakResolver, CounterattackService counterattackService)
        {
            this.breakResolver = breakResolver;
            this.counterattackService = counterattackService;
        }

        public BattleForecast Create(BattleTestUnit actor, BattleTestUnit target, BattleTestTile from, BattleTestTile to, bool special)
        {
            if (actor == null || target == null || actor.defeated || target.defeated)
            {
                return new BattleForecast(false, string.Empty, string.Empty, string.Empty, 0, 0, 0, 0, 0, 0, StyleAdvantage.Neutral, 0, false, 0, 0, "No valid target");
            }

            int distance = Mathf.Abs(actor.cell.x - target.cell.x) + Mathf.Abs(actor.cell.y - target.cell.y);
            int range = special ? actor.definition.specialRange : actor.definition.attackRange;
            if (distance > range)
            {
                return new BattleForecast(false, actor.definition.displayName, target.definition.displayName, ActionName(actor, special), 0, 0, 0, 0, 0, 0, StyleAdvantage.Neutral, 0, false, 0, 0, "Out of range");
            }

            int terrainBonus = from != null && to != null && from.elevation > to.elevation ? 2 : 0;
            if (to != null && (to.hazard == HazardType.Ice || to.hazard == HazardType.Slippery) && actor.definition.style == SkillStyle.Ice)
            {
                terrainBonus += 2;
            }
            int styleModifier = breakResolver.AttackModifier(actor.definition.style, target.definition.style);
            int attackBonus = actor.definition.attackBonus + terrainBonus + styleModifier + (special ? actor.definition.specialAttackBonus : 0);
            int defense = DefenseValue(target, to);
            int required = Mathf.Clamp(defense - attackBonus, 2, 20);
            int hitPercent = Mathf.Clamp((21 - required) * 5, 5, 95);
            int damageMin = actor.definition.damageMin + terrainBonus + (special ? actor.definition.specialPower : 0);
            int damageMax = actor.definition.damageMax + terrainBonus + (special ? actor.definition.specialPower : 0);
            int breakGain = breakResolver.BreakGain(actor.definition.style, target.definition.style, true);
            bool canCounter = counterattackService.CanCounter(target, actor, distance);

            return new BattleForecast(
                true,
                actor.definition.displayName,
                target.definition.displayName,
                ActionName(actor, special),
                required,
                hitPercent,
                Mathf.Max(1, damageMin),
                Mathf.Max(1, damageMax),
                5,
                breakGain,
                breakResolver.Resolve(actor.definition.style, target.definition.style),
                terrainBonus,
                canCounter,
                target.definition.damageMin,
                target.definition.damageMax,
                string.Empty);
        }

        private string ActionName(BattleTestUnit actor, bool special)
        {
            return special ? actor.definition.specialName : actor.definition.basicAttackName;
        }

        private int DefenseValue(BattleTestUnit unit, BattleTestTile tile)
        {
            int defense = unit.definition.defense + (tile != null ? tile.coverBonus : 0);
            if (tile != null && tile.hazard == HazardType.Smoke)
            {
                defense += 2;
            }
            if (unit.guarded)
            {
                defense += 2;
            }

            if (unit.marked || unit.broken)
            {
                defense -= 2;
            }

            return defense;
        }
    }

    public sealed class ThreatRangeService
    {
        public HashSet<Vector2Int> BuildThreatCells(IReadOnlyList<BattleTestUnit> units, Faction sourceFaction, int width, int height)
        {
            HashSet<Vector2Int> cells = new HashSet<Vector2Int>();
            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (unit.defeated || unit.definition.faction != sourceFaction)
                {
                    continue;
                }

                int range = Mathf.Max(unit.definition.attackRange, unit.definition.specialRange);
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        Vector2Int cell = new Vector2Int(x, y);
                        int distance = Mathf.Abs(unit.cell.x - cell.x) + Mathf.Abs(unit.cell.y - cell.y);
                        if (distance <= range)
                        {
                            cells.Add(cell);
                        }
                    }
                }
            }

            return cells;
        }
    }

    public sealed class ObjectiveManager
    {
        public string ScenarioTitle { get; private set; } = "압록강 폐사당 탈환전";
        public string PrimaryObjective { get; private set; } = "중원 감찰사 제압";
        public string BonusObjective { get; private set; } = "8턴 안에 승리 / 제단 보존 / 독공술사 생포";
        public string DefeatCondition { get; private set; } = "박성준 또는 백련 전투불능 / 12턴 초과";
        public bool altarPreserved = true;
        public bool poisonerSubdued = true;

        public BattleOutcome Evaluate(IReadOnlyList<BattleTestUnit> units, int round)
        {
            bool inspectorDown = false;
            bool parkDown = false;
            bool baekDown = false;

            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit unit = units[i];
                if (unit.definition.id == "park_sungjun" && unit.defeated)
                {
                    parkDown = true;
                }

                if (unit.definition.id == "baek_ryeon" && unit.defeated)
                {
                    baekDown = true;
                }

                if (unit.definition.id == "central_inspector" && unit.defeated)
                {
                    inspectorDown = true;
                }

                if (unit.definition.id == "sichuan_poisoner" && unit.defeated)
                {
                    poisonerSubdued = false;
                }
            }

            if (parkDown || baekDown || round > 12)
            {
                return BattleOutcome.Defeat;
            }

            return inspectorDown ? BattleOutcome.Victory : BattleOutcome.Ongoing;
        }
    }

    public sealed class EnemyTacticsAI
    {
        private readonly BreakResolver breakResolver;
        private readonly BattleForecastService forecastService;

        public EnemyTacticsAI(BreakResolver breakResolver, BattleForecastService forecastService)
        {
            this.breakResolver = breakResolver;
            this.forecastService = forecastService;
        }

        public BattleTestUnit ChooseTarget(BattleTestUnit actor, IReadOnlyList<BattleTestUnit> units, System.Func<Vector2Int, BattleTestTile> tileAt)
        {
            BattleTestUnit best = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < units.Count; i++)
            {
                BattleTestUnit target = units[i];
                if (target.defeated || target.definition.faction == actor.definition.faction)
                {
                    continue;
                }

                int distance = Mathf.Abs(actor.cell.x - target.cell.x) + Mathf.Abs(actor.cell.y - target.cell.y);
                int score = 100 - (distance * 8);
                score += target.definition.maxHp - target.hp;
                if (breakResolver.Resolve(actor.definition.style, target.definition.style) == StyleAdvantage.Advantage)
                {
                    score += 22;
                }

                BattleForecast forecast = forecastService.Create(actor, target, tileAt(actor.cell), tileAt(target.cell), false);
                if (forecast.counterPossible)
                {
                    score -= 12;
                }

                if (target.hp <= forecast.damageMax)
                {
                    score += 35;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    best = target;
                }
            }

            return best;
        }
    }

    public sealed class BattleForecastPanel
    {
        public void Draw(Rect rect, BattleForecast forecast, GUIStyle panelStyle, GUIStyle titleStyle, GUIStyle smallStyle)
        {
            GUI.Box(rect, GUIContent.none, panelStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 12f, rect.width - 32f, 24f), "Battle Forecast", titleStyle);

            if (!forecast.valid)
            {
                GUI.Label(new Rect(rect.x + 16f, rect.y + 44f, rect.width - 32f, 24f), forecast.reason, smallStyle);
                return;
            }

            GUI.Label(new Rect(rect.x + 16f, rect.y + 42f, rect.width - 32f, 22f), $"{forecast.actorName} -> {forecast.targetName}", smallStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 64f, rect.width - 32f, 22f), $"Action: {forecast.actionName}", smallStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 86f, rect.width - 32f, 22f), $"Hit: d20 {forecast.requiredD20}+  ({forecast.hitPercent}%)", smallStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 108f, rect.width - 32f, 22f), $"Damage: {forecast.damageMin}-{forecast.damageMax}   Crit: {forecast.critPercent}%", smallStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 130f, rect.width - 32f, 22f), $"Break: +{forecast.breakGain}   Matchup: {forecast.styleAdvantage}", smallStyle);
            GUI.Label(new Rect(rect.x + 16f, rect.y + 152f, rect.width - 32f, 22f), $"Terrain: +{forecast.terrainBonus}   Counter: {(forecast.counterPossible ? forecast.counterDamageMin + "-" + forecast.counterDamageMax : "No")}", smallStyle);
        }
    }
}
