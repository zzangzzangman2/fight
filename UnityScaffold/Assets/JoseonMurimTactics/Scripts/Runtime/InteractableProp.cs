using UnityEngine;

namespace JoseonMurimTactics
{
[DisallowMultipleComponent]
public sealed class InteractableProp : MonoBehaviour
{
    public ActionSlot requiredActionSlot = ActionSlot.Main;
    public StatType checkStat = StatType.Strength;
    public int dc = 12;
    public int radius = 1;
    public bool consumedOnUse = true;
    public InteractableEffectType effectType = InteractableEffectType.CreateCover;

    public void Configure(ActionSlot actionSlot, StatType stat, int checkDc, int effectRadius,
                          InteractableEffectType effect, bool consumed)
    {
        requiredActionSlot = actionSlot;
        checkStat = stat;
        dc = Mathf.Max(0, checkDc);
        radius = Mathf.Max(0, effectRadius);
        effectType = effect;
        consumedOnUse = consumed;
    }
}
}
