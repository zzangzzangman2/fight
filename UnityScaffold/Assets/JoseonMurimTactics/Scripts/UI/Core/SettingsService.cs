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
        Data = new SettingsData { bgmVolume = legacy.bgmVolume,
                                  sfxVolume = legacy.sfxVolume,
                                  textSpeed = legacy.textSpeed,
                                  autoTextSpeed = legacy.autoTextSpeed,
                                  diceAnimationSpeed = legacy.diceAnimationSpeed,
                                  fullscreen = legacy.fullscreen,
                                  resolutionIndex = legacy.resolutionIndex,
                                  damageNumbers = legacy.damageNumbers,
                                  choiceEffectPreview = legacy.choiceEffectPreview };
    }

    public void Save()
    {
        GameSettings settings = GameSettings.Load();
        settings.bgmVolume = Data.bgmVolume;
        settings.sfxVolume = Data.sfxVolume;
        settings.textSpeed = Data.textSpeed;
        settings.autoTextSpeed = Data.autoTextSpeed;
        settings.diceAnimationSpeed = Data.diceAnimationSpeed;
        settings.fullscreen = Data.fullscreen;
        settings.resolutionIndex = Data.resolutionIndex;
        settings.damageNumbers = Data.damageNumbers;
        settings.choiceEffectPreview = Data.choiceEffectPreview;
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
