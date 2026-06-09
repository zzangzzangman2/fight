using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Minimum settings persisted through PlayerPrefs for v1.0 noncombat UI.</summary>
public struct GameSettings
{
    private const string Prefix = "joseon.settings.";

    public float bgmVolume;
    public float sfxVolume;
    public float textSpeed;
    public float autoTextSpeed;
    public float uiScale;
    public float diceAnimationSpeed;
    public bool fullscreen;
    public bool screenShake;
    public bool autoDialogue;
    public bool detailedCombatMath;
    public bool damageNumbers;
    public bool choiceEffectPreview;
    public int resolutionIndex;

    public static GameSettings Load()
    {
        return new GameSettings { bgmVolume = PlayerPrefs.GetFloat(Prefix + "bgmVolume", 0.8f),
                                  sfxVolume = PlayerPrefs.GetFloat(Prefix + "sfxVolume", 0.8f),
                                  textSpeed = PlayerPrefs.GetFloat(Prefix + "textSpeed", 0.55f),
                                  autoTextSpeed = PlayerPrefs.GetFloat(Prefix + "autoTextSpeed", 0.55f),
                                  uiScale = PlayerPrefs.GetFloat(Prefix + "uiScale", 1.0f),
                                  diceAnimationSpeed = PlayerPrefs.GetFloat(Prefix + "diceAnimationSpeed", 0.55f),
                                  fullscreen =
                                      PlayerPrefs.GetInt(Prefix + "fullscreen", Screen.fullScreen ? 1 : 0) == 1,
                                  screenShake = PlayerPrefs.GetInt(Prefix + "screenShake", 1) == 1,
                                  autoDialogue = PlayerPrefs.GetInt(Prefix + "autoDialogue", 0) == 1,
                                  detailedCombatMath = PlayerPrefs.GetInt(Prefix + "detailedCombatMath", 0) == 1,
                                  damageNumbers = PlayerPrefs.GetInt(Prefix + "damageNumbers", 1) == 1,
                                  choiceEffectPreview = PlayerPrefs.GetInt(Prefix + "choiceEffectPreview", 1) == 1,
                                  resolutionIndex = PlayerPrefs.GetInt(Prefix + "resolutionIndex", 0) };
    }

    public void Save()
    {
        bgmVolume = Mathf.Clamp01(bgmVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        textSpeed = Mathf.Clamp01(textSpeed);
        autoTextSpeed = Mathf.Clamp01(autoTextSpeed);
        uiScale = Mathf.Clamp(uiScale, 0.8f, 1.4f);
        diceAnimationSpeed = Mathf.Clamp01(diceAnimationSpeed);

        PlayerPrefs.SetFloat(Prefix + "bgmVolume", bgmVolume);
        PlayerPrefs.SetFloat(Prefix + "sfxVolume", sfxVolume);
        PlayerPrefs.SetFloat(Prefix + "textSpeed", textSpeed);
        PlayerPrefs.SetFloat(Prefix + "autoTextSpeed", autoTextSpeed);
        PlayerPrefs.SetFloat(Prefix + "uiScale", uiScale);
        PlayerPrefs.SetFloat(Prefix + "diceAnimationSpeed", diceAnimationSpeed);
        PlayerPrefs.SetInt(Prefix + "fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "screenShake", screenShake ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "autoDialogue", autoDialogue ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "detailedCombatMath", detailedCombatMath ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "damageNumbers", damageNumbers ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "choiceEffectPreview", choiceEffectPreview ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "resolutionIndex", Mathf.Max(0, resolutionIndex));
        PlayerPrefs.Save();
        Screen.fullScreen = fullscreen;
        ApplyResolution();
    }

    private void ApplyResolution()
    {
        switch (Mathf.Clamp(resolutionIndex, 0, 2))
        {
        case 0:
            Screen.SetResolution(1280, 720, fullscreen);
            break;
        case 1:
            Screen.SetResolution(1600, 900, fullscreen);
            break;
        default:
            Screen.SetResolution(1920, 1080, fullscreen);
            break;
        }
    }
}
}
