using System.Collections.Generic;

namespace JoseonMurimTactics
{
public enum BattlePhase
{
    PlayerPhase,
    EnemyPhase,
    NeutralPhase
}

public sealed class PhaseTurnController
{
    public BattlePhase CurrentPhase { get; private set; } = BattlePhase.PlayerPhase;
    public int Round { get; private set; } = 1;

    public void Reset()
    {
        Round = 1;
        CurrentPhase = BattlePhase.PlayerPhase;
    }

    public void BeginPlayerPhase()
    {
        CurrentPhase = BattlePhase.PlayerPhase;
    }

    public void BeginEnemyPhase()
    {
        CurrentPhase = BattlePhase.EnemyPhase;
    }

    public void BeginNeutralPhase()
    {
        CurrentPhase = BattlePhase.NeutralPhase;
    }

    public void CompleteEnemyPhase()
    {
        Round++;
        CurrentPhase = BattlePhase.PlayerPhase;
    }

    public bool IsPlayerPhase => CurrentPhase == BattlePhase.PlayerPhase;
    public bool IsEnemyPhase => CurrentPhase == BattlePhase.EnemyPhase;

    public bool CanPlayerControl(BattleTestUnit unit)
    {
        return IsPlayerPhase && unit != null && !unit.defeated && unit.definition.faction == Faction.Ally &&
               !unit.acted;
    }

    public bool AllFactionUnitsActed(IEnumerable<BattleTestUnit> units, Faction faction)
    {
        bool any = false;
        foreach (BattleTestUnit unit in units)
        {
            if (unit == null || unit.defeated || unit.definition.faction != faction)
            {
                continue;
            }

            any = true;
            if (!unit.acted)
            {
                return false;
            }
        }

        return any;
    }
}
}
