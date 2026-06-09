using UnityEngine;

namespace JoseonMurimTactics
{
/// <summary>Minimum settings persisted through PlayerPrefs for v1.0 noncombat UI.</summary>
public struct GameSettings
{
    private const string Prefix = "joseon.settings.";

    public float masterVolume;
    public float bgmVolume;
    public float sfxVolume;
    public float uiVolume;
    public float textSpeed;
    public float autoTextSpeed;
    public float uiScale;
    public float diceAnimationSpeed;
    public float enemyPhaseSpeed;
    public bool fullscreen;
    public bool vsync;
    public bool screenShake;
    public bool autoDialogue;
    public bool detailedCombatMath;
    public bool damageNumbers;
    public bool choiceEffectPreview;
    public bool confirmPopups;
    public bool confirmMoveAttack;
    public bool largeText;
    public bool highContrast;
    public bool reduceMotion;
    public bool colorBlindAssist;
    public int resolutionIndex;

    public static GameSettings Load()
    {
        return new GameSettings { masterVolume = PlayerPrefs.GetFloat(Prefix + "masterVolume", 1f),
                                  bgmVolume = PlayerPrefs.GetFloat(Prefix + "bgmVolume", 0.8f),
                                  sfxVolume = PlayerPrefs.GetFloat(Prefix + "sfxVolume", 0.8f),
                                  uiVolume = PlayerPrefs.GetFloat(Prefix + "uiVolume", 0.8f),
                                  textSpeed = PlayerPrefs.GetFloat(Prefix + "textSpeed", 0.55f),
                                  autoTextSpeed = PlayerPrefs.GetFloat(Prefix + "autoTextSpeed", 0.55f),
                                  uiScale = PlayerPrefs.GetFloat(Prefix + "uiScale", 1.0f),
                                  diceAnimationSpeed = PlayerPrefs.GetFloat(Prefix + "diceAnimationSpeed", 0.55f),
                                  enemyPhaseSpeed = PlayerPrefs.GetFloat(Prefix + "enemyPhaseSpeed", 0.55f),
                                  fullscreen =
                                      PlayerPrefs.GetInt(Prefix + "fullscreen", Screen.fullScreen ? 1 : 0) == 1,
                                  vsync = PlayerPrefs.GetInt(Prefix + "vsync", QualitySettings.vSyncCount > 0 ? 1 : 0) ==
                                          1,
                                  screenShake = PlayerPrefs.GetInt(Prefix + "screenShake", 1) == 1,
                                  autoDialogue = PlayerPrefs.GetInt(Prefix + "autoDialogue", 0) == 1,
                                  detailedCombatMath = PlayerPrefs.GetInt(Prefix + "detailedCombatMath", 0) == 1,
                                  damageNumbers = PlayerPrefs.GetInt(Prefix + "damageNumbers", 1) == 1,
                                  choiceEffectPreview = PlayerPrefs.GetInt(Prefix + "choiceEffectPreview", 1) == 1,
                                  confirmPopups = PlayerPrefs.GetInt(Prefix + "confirmPopups", 1) == 1,
                                  confirmMoveAttack = PlayerPrefs.GetInt(Prefix + "confirmMoveAttack", 1) == 1,
                                  largeText = PlayerPrefs.GetInt(Prefix + "largeText", 0) == 1,
                                  highContrast = PlayerPrefs.GetInt(Prefix + "highContrast", 0) == 1,
                                  reduceMotion = PlayerPrefs.GetInt(Prefix + "reduceMotion", 0) == 1,
                                  colorBlindAssist = PlayerPrefs.GetInt(Prefix + "colorBlindAssist", 0) == 1,
                                  resolutionIndex = PlayerPrefs.GetInt(Prefix + "resolutionIndex", 0) };
    }

    public void Save()
    {
        masterVolume = Mathf.Clamp01(masterVolume);
        bgmVolume = Mathf.Clamp01(bgmVolume);
        sfxVolume = Mathf.Clamp01(sfxVolume);
        uiVolume = Mathf.Clamp01(uiVolume);
        textSpeed = Mathf.Clamp01(textSpeed);
        autoTextSpeed = Mathf.Clamp01(autoTextSpeed);
        uiScale = Mathf.Clamp(uiScale, 0.8f, 1.4f);
        diceAnimationSpeed = Mathf.Clamp01(diceAnimationSpeed);
        enemyPhaseSpeed = Mathf.Clamp01(enemyPhaseSpeed);

        PlayerPrefs.SetFloat(Prefix + "masterVolume", masterVolume);
        PlayerPrefs.SetFloat(Prefix + "bgmVolume", bgmVolume);
        PlayerPrefs.SetFloat(Prefix + "sfxVolume", sfxVolume);
        PlayerPrefs.SetFloat(Prefix + "uiVolume", uiVolume);
        PlayerPrefs.SetFloat(Prefix + "textSpeed", textSpeed);
        PlayerPrefs.SetFloat(Prefix + "autoTextSpeed", autoTextSpeed);
        PlayerPrefs.SetFloat(Prefix + "uiScale", uiScale);
        PlayerPrefs.SetFloat(Prefix + "diceAnimationSpeed", diceAnimationSpeed);
        PlayerPrefs.SetFloat(Prefix + "enemyPhaseSpeed", enemyPhaseSpeed);
        PlayerPrefs.SetInt(Prefix + "fullscreen", fullscreen ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "vsync", vsync ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "screenShake", screenShake ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "autoDialogue", autoDialogue ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "detailedCombatMath", detailedCombatMath ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "damageNumbers", damageNumbers ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "choiceEffectPreview", choiceEffectPreview ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "confirmPopups", confirmPopups ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "confirmMoveAttack", confirmMoveAttack ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "largeText", largeText ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "highContrast", highContrast ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "reduceMotion", reduceMotion ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "colorBlindAssist", colorBlindAssist ? 1 : 0);
        PlayerPrefs.SetInt(Prefix + "resolutionIndex", Mathf.Max(0, resolutionIndex));
        PlayerPrefs.Save();
        Screen.fullScreen = fullscreen;
        QualitySettings.vSyncCount = vsync ? 1 : 0;
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
