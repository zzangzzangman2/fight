using UnityEngine;

namespace JoseonMurimTactics
{
public sealed class SettingsService
{
    public SettingsData Data { get; private set; }

    public SettingsService()
    {
        Reload();
    }

    public void Reload()
    {
        GameSettings legacy = GameSettings.Load();
        Data = new SettingsData { masterVolume = legacy.masterVolume,
                                  bgmVolume = legacy.bgmVolume,
                                  sfxVolume = legacy.sfxVolume,
                                  uiVolume = legacy.uiVolume,
                                  textSpeed = legacy.textSpeed,
                                  autoTextSpeed = legacy.autoTextSpeed,
                                  diceAnimationSpeed = legacy.diceAnimationSpeed,
                                  enemyPhaseSpeed = legacy.enemyPhaseSpeed,
                                  uiScale = legacy.uiScale,
                                  fullscreen = legacy.fullscreen,
                                  vsync = legacy.vsync,
                                  resolutionIndex = legacy.resolutionIndex,
                                  screenShake = legacy.screenShake,
                                  autoDialogue = legacy.autoDialogue,
                                  detailedCombatMath = legacy.detailedCombatMath,
                                  damageNumbers = legacy.damageNumbers,
                                  choiceEffectPreview = legacy.choiceEffectPreview,
                                  confirmPopups = legacy.confirmPopups,
                                  confirmMoveAttack = legacy.confirmMoveAttack,
                                  largeText = legacy.largeText,
                                  highContrast = legacy.highContrast,
                                  reduceMotion = legacy.reduceMotion,
                                  colorBlindAssist = legacy.colorBlindAssist };
    }

    public void Save()
    {
        GameSettings settings = GameSettings.Load();
        settings.masterVolume = Data.masterVolume;
        settings.bgmVolume = Data.bgmVolume;
        settings.sfxVolume = Data.sfxVolume;
        settings.uiVolume = Data.uiVolume;
        settings.textSpeed = Data.textSpeed;
        settings.autoTextSpeed = Data.autoTextSpeed;
        settings.diceAnimationSpeed = Data.diceAnimationSpeed;
        settings.enemyPhaseSpeed = Data.enemyPhaseSpeed;
        settings.uiScale = Data.uiScale;
        settings.fullscreen = Data.fullscreen;
        settings.vsync = Data.vsync;
        settings.resolutionIndex = Data.resolutionIndex;
        settings.screenShake = Data.screenShake;
        settings.autoDialogue = Data.autoDialogue;
        settings.detailedCombatMath = Data.detailedCombatMath;
        settings.damageNumbers = Data.damageNumbers;
        settings.choiceEffectPreview = Data.choiceEffectPreview;
        settings.confirmPopups = Data.confirmPopups;
        settings.confirmMoveAttack = Data.confirmMoveAttack;
        settings.largeText = Data.largeText;
        settings.highContrast = Data.highContrast;
        settings.reduceMotion = Data.reduceMotion;
        settings.colorBlindAssist = Data.colorBlindAssist;
        settings.Save();
    }

    public void ApplyFrom(SettingsData data)
    {
        if (data == null)
        {
            return;
        }

        Data = data;
        Save();
    }
}
}
