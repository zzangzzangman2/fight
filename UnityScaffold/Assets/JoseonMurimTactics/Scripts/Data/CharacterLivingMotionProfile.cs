using UnityEngine;

namespace JoseonMurimTactics
{
public enum CharacterEmotion
{
    Neutral,
    Blink,
    Smile,
    Serious,
    Angry,
    Pain,
    Victory,
    LowHp
}

[CreateAssetMenu(menuName = "Joseon Murim Tactics/Character Living Motion Profile")]
public sealed class CharacterLivingMotionProfile : ScriptableObject
{
    [Header("Idle")]
    public float idleBreathAmount = 0.015f;
    public float idleBreathSpeed = 1f;
    public float idleBobAmount = 0.035f;
    public float idleBobSpeed = 1f;
    public float blinkMinInterval = 3f;
    public float blinkMaxInterval = 7f;

    [Header("Selection / Turn")]
    public float selectedPopScale = 1.06f;
    public float selectedPopDuration = 0.15f;
    public float turnStartHop = 0.085f;
    public float turnStartDuration = 0.34f;
    public float waitSlouchAmount = 0.45f;
    public float lowHpShakeAmount = 0.012f;

    [Header("Move")]
    public float moveHopAmount = 0.040f;
    public float moveLeanAmount = 1f;
    public float footDustAmount = 1f;

    [Header("Attack")]
    public float attackAnticipationDistance = 0.045f;
    public float attackLungeMultiplier = 1f;
    public float impactFreezeSeconds = 0.055f;
    public float hitShakeAmount = 0.025f;
    public float victoryHopAmount = 0.060f;

    [Header("Layer Sway")]
    public float hairSwayAmount = 1f;
    public float weaponSwayAmount = 1f;
    public float accessorySwayAmount = 1f;
}
}
