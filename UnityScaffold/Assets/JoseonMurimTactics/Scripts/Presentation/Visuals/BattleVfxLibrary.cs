using UnityEngine;

namespace JoseonMurimTactics
{
[CreateAssetMenu(fileName = "BattleVfxLibrary", menuName = "Joseon Murim Tactics/Visual/Battle VFX Library")]
public sealed class BattleVfxLibrary : ScriptableObject
{
    public AnimationClip swordSlash;
    public AnimationClip frostSpear;
    public AnimationClip hitSpark;
    public AnimationClip healPulse;
    public AnimationClip snowStep;
    public AnimationClip counterFlash;
    public AnimationClip dangerAura;
    public AnimationClip phaseSnowSwirl;
}
}
