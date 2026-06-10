namespace JoseonMurimTactics
{
public interface ICombatAnimationEventReceiver
{
    void OnAttackHitFrame();
    void OnSkillHitFrame();
    void OnProjectileSpawnFrame();
    void OnFootstepFrame();
    void OnAnimationComplete();
}
}