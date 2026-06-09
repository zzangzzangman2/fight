using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace JoseonMurimTactics
{
public sealed class SettingsPopup : UIScreenBase
{
    [SerializeField]
    private Slider bgmSlider;
    [SerializeField]
    private Slider sfxSlider;
    [SerializeField]
    private Slider textSpeedSlider;
    [SerializeField]
    private Slider autoTextSpeedSlider;
    [SerializeField]
    private Toggle fullscreenToggle;
    [SerializeField]
    private Toggle choicePreviewToggle;
    [SerializeField]
    private TMP_Dropdown resolutionDropdown;

    public void Bind(SettingsService service)
    {
        if (service == null || service.Data == null)
        {
            return;
        }

        SettingsData data = service.Data;
        SetValue(bgmSlider, data.bgmVolume);
        SetValue(sfxSlider, data.sfxVolume);
        SetValue(textSpeedSlider, data.textSpeed);
        SetValue(autoTextSpeedSlider, data.autoTextSpeed);
        SetValue(fullscreenToggle, data.fullscreen);
        SetValue(choicePreviewToggle, data.choiceEffectPreview);
        if (resolutionDropdown != null && resolutionDropdown.options.Count > 0)
        {
            resolutionDropdown.value = Mathf.Clamp(data.resolutionIndex, 0, resolutionDropdown.options.Count - 1);
        }
    }

    public SettingsData Read(SettingsService service)
    {
        SettingsData data = service != null && service.Data != null ? service.Data : new SettingsData();
        data.bgmVolume = ValueOf(bgmSlider, data.bgmVolume);
        data.sfxVolume = ValueOf(sfxSlider, data.sfxVolume);
        data.textSpeed = ValueOf(textSpeedSlider, data.textSpeed);
        data.autoTextSpeed = ValueOf(autoTextSpeedSlider, data.autoTextSpeed);
        data.fullscreen = ValueOf(fullscreenToggle, data.fullscreen);
        data.choiceEffectPreview = ValueOf(choicePreviewToggle, data.choiceEffectPreview);
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
