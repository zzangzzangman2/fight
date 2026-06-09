using System;

namespace JoseonMurimTactics
{
[Serializable]
public sealed class SettingsData
{
    public float masterVolume = 1f;
    public float bgmVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public float uiVolume = 0.8f;
    public float textSpeed = 0.55f;
    public float autoTextSpeed = 0.55f;
    public float diceAnimationSpeed = 0.55f;
    public float enemyPhaseSpeed = 0.55f;
    public float uiScale = 1f;
    public bool fullscreen;
    public bool vsync = true;
    public int resolutionIndex;
    public bool screenShake = true;
    public bool autoDialogue;
    public bool detailedCombatMath;
    public bool damageNumbers = true;
    public bool choiceEffectPreview = true;
    public bool confirmPopups = true;
    public bool confirmMoveAttack = true;
    public bool largeText;
    public bool highContrast;
    public bool reduceMotion;
    public bool colorBlindAssist;
}
}
