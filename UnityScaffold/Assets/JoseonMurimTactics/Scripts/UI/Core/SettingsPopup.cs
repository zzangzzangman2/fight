using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
public sealed class SettingsPopup : UIScreenBase
{
    [SerializeField]
    private Slider masterSlider;
    [SerializeField]
    private Slider bgmSlider;
    [SerializeField]
    private Slider sfxSlider;
    [SerializeField]
    private Slider uiSlider;
    [SerializeField]
    private Slider textSpeedSlider;
    [SerializeField]
    private Slider autoTextSpeedSlider;
    [SerializeField]
    private Slider enemyPhaseSpeedSlider;
    [SerializeField]
    private Slider uiScaleSlider;
    [SerializeField]
    private Toggle fullscreenToggle;
    [SerializeField]
    private Toggle vsyncToggle;
    [SerializeField]
    private Toggle choicePreviewToggle;
    [SerializeField]
    private Toggle confirmPopupToggle;
    [SerializeField]
    private Toggle largeTextToggle;
    [SerializeField]
    private Toggle highContrastToggle;
    [SerializeField]
    private Toggle reduceMotionToggle;
    [SerializeField]
    private TMP_Dropdown resolutionDropdown;

    public void Bind(SettingsService service)
    {
        if (service == null || service.Data == null)
        {
            return;
        }

        SettingsData data = service.Data;
        SetValue(masterSlider, data.masterVolume);
        SetValue(bgmSlider, data.bgmVolume);
        SetValue(sfxSlider, data.sfxVolume);
        SetValue(uiSlider, data.uiVolume);
        SetValue(textSpeedSlider, data.textSpeed);
        SetValue(autoTextSpeedSlider, data.autoTextSpeed);
        SetValue(enemyPhaseSpeedSlider, data.enemyPhaseSpeed);
        SetValue(uiScaleSlider, data.uiScale);
        SetValue(fullscreenToggle, data.fullscreen);
        SetValue(vsyncToggle, data.vsync);
        SetValue(choicePreviewToggle, data.choiceEffectPreview);
        SetValue(confirmPopupToggle, data.confirmPopups);
        SetValue(largeTextToggle, data.largeText);
        SetValue(highContrastToggle, data.highContrast);
        SetValue(reduceMotionToggle, data.reduceMotion);
        if (resolutionDropdown != null && resolutionDropdown.options.Count > 0)
        {
            resolutionDropdown.value = Mathf.Clamp(data.resolutionIndex, 0, resolutionDropdown.options.Count - 1);
        }
    }

    public SettingsData Read(SettingsService service)
    {
        SettingsData data = service != null && service.Data != null ? service.Data : new SettingsData();
        data.masterVolume = ValueOf(masterSlider, data.masterVolume);
        data.bgmVolume = ValueOf(bgmSlider, data.bgmVolume);
        data.sfxVolume = ValueOf(sfxSlider, data.sfxVolume);
        data.uiVolume = ValueOf(uiSlider, data.uiVolume);
        data.textSpeed = ValueOf(textSpeedSlider, data.textSpeed);
        data.autoTextSpeed = ValueOf(autoTextSpeedSlider, data.autoTextSpeed);
        data.enemyPhaseSpeed = ValueOf(enemyPhaseSpeedSlider, data.enemyPhaseSpeed);
        data.uiScale = ValueOf(uiScaleSlider, data.uiScale);
        data.fullscreen = ValueOf(fullscreenToggle, data.fullscreen);
        data.vsync = ValueOf(vsyncToggle, data.vsync);
        data.choiceEffectPreview = ValueOf(choicePreviewToggle, data.choiceEffectPreview);
        data.confirmPopups = ValueOf(confirmPopupToggle, data.confirmPopups);
        data.largeText = ValueOf(largeTextToggle, data.largeText);
        data.highContrast = ValueOf(highContrastToggle, data.highContrast);
        data.reduceMotion = ValueOf(reduceMotionToggle, data.reduceMotion);
        data.resolutionIndex = resolutionDropdown != null ? resolutionDropdown.value : data.resolutionIndex;
        return data;
    }

    private static void SetValue(Slider slider, float value)
    {
        if (slider != null)
        {
            slider.SetValueWithoutNotify(value);
        }
    }

    private static void SetValue(Toggle toggle, bool value)
    {
        if (toggle != null)
        {
            toggle.SetIsOnWithoutNotify(value);
        }
    }

    private static float ValueOf(Slider slider, float fallback)
    {
        return slider != null ? slider.value : fallback;
    }

    private static bool ValueOf(Toggle toggle, bool fallback)
    {
        return toggle != null ? toggle.isOn : fallback;
    }
}
}
