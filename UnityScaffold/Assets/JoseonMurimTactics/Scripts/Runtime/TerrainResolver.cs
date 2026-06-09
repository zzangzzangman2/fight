using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class TerrainResolver
{
    private readonly BattleMapData map;
    private readonly DiceRoller dice;
    private readonly MovementResolver movement;
    private readonly CombatLog log;

    public TerrainResolver(BattleMapData map, DiceRoller dice, MovementResolver movement, CombatLog log)
    {
        this.map = map;
        this.dice = dice;
        this.movement = movement;
        this.log = log;
    }

    public bool TryUseObject(CombatantRuntime actor, InteractableObjectData interactable)
    {
        if (interactable == null || !actor.actions.CanSpend(interactable.requiredActionSlot))
        {
            return false;
        }

        if (movement.GridDistance(actor.currentCell, interactable.cell) > 1)
        {
            log.Add("Terrain", actor.DisplayName + " 상호작용 실패: 거리 밖.");
            return false;
        }

        DiceRoll roll = dice.RollD20();
        int total = roll.total + actor.StatModifier(interactable.stat) + actor.Proficiency;
        bool success = roll.natural == 20 || total >= interactable.dc;
        actor.actions.Spend(interactable.requiredActionSlot);

        log.Add("Terrain", actor.DisplayName + " " + interactable.displayName + ": d20 " + roll.detail + " = " + total +
                               " vs DC " + interactable.dc);

        if (!success)
        {
            return false;
        }

        ApplyEffect(interactable);
        return true;
    }

    private void ApplyEffect(InteractableObjectData interactable)
    {
        BattleCellData cell = movement.FindCell(interactable.cell);
        if (cell == null)
        {
            return;
        }

        switch (interactable.effectType)
        {
        case InteractableEffectType.CreateCover:
            cell.coverType = CoverType.Heavy;
            break;
        case InteractableEffectType.CreateSmoke:
            cell.coverType = CoverType.Heavy;
            cell.hazardType = HazardType.Smoke;
            break;
        case InteractableEffectType.CreateFire:
            cell.hazardType = HazardType.Fire;
            break;
        case InteractableEffectType.CreateIce:
            cell.hazardType = HazardType.Ice;
            break;
        case InteractableEffectType.CollapseBridge:
            cell.hazardType = HazardType.Fall;
            cell.coverType = CoverType.None;
            cell.walkable = false;
            break;
        case InteractableEffectType.ShatterAltar:
            cell.hazardType = HazardType.Fall;
            cell.walkable = false;
            break;
        case InteractableEffectType.BlockSight:
            cell.coverType = CoverType.Heavy;
            break;
        }

        log.Add("Terrain", interactable.displayName + " 효과 적용: " + interactable.effectType + ".");
    }
}
}
