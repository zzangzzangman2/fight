using System;

namespace JoseonMurimTactics
{
[Serializable]
public sealed class ActionEconomy
{
    public bool mainAction = true;
    public bool bonusAction = true;
    public bool reaction = true;
    public int movementLeft;

    public void ResetForTurn(int movement)
    {
        mainAction = true;
        bonusAction = true;
        reaction = true;
        movementLeft = movement;
    }

    public bool CanSpend(ActionSlot slot)
    {
        switch (slot)
        {
        case ActionSlot.Main:
            return mainAction;
        case ActionSlot.Bonus:
            return bonusAction;
        case ActionSlot.Reaction:
            return reaction;
        case ActionSlot.Free:
            return true;
        default:
            return false;
        }
    }

    public void Spend(ActionSlot slot)
    {
        switch (slot)
        {
        case ActionSlot.Main:
            mainAction = false;
            break;
        case ActionSlot.Bonus:
            bonusAction = false;
            break;
        case ActionSlot.Reaction:
            reaction = false;
            break;
        }
    }
}
}
