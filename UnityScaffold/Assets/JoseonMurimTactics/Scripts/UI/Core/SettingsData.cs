using System;

namespace JoseonMurimTactics
{
[Serializable]
public sealed class SettingsData
{
    public float bgmVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public float textSpeed = 0.55f;
    public float autoTextSpeed = 0.55f;
    public float diceAnimationSpeed = 0.55f;
    public bool fullscreen;
    public int resolutionIndex;
    public bool damageNumbers = true;
    public bool choiceEffectPreview = true;
}
}
