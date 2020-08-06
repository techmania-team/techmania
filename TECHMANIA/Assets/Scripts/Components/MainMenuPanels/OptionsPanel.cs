using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsPanel : MonoBehaviour
{
    public Text resolutionDisplay;
    public Text fullscreenModeDisplay;
    public Text vsyncDisplay;
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider keysoundVolumeSlider;
    public AudioMixer audioMixer;

    private Options options;
    private int resolutionIndex;

    public static void ApplyOptionsOnStartUp()
    {
        OptionsPanel instance = FindObjectOfType<Canvas>()
            .GetComponentInChildren<OptionsPanel>(includeInactive: true);
        instance.LoadOrCreateOptions();
        instance.options.ApplyGraphicSettings();
        instance.ApplyVolume();
    }

    private void LoadOrCreateOptions()
    {
        options = null;
        try
        {
            options = OptionsBase.LoadFromFile(
                Paths.GetOptionsFilePath()) as Options;
        }
        catch (IOException)
        {
            options = new Options();
        }

        // Find resolutionIndex.
        resolutionIndex = -1;
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            Resolution r = Screen.resolutions[i];
            if (r.width == options.width &&
                r.height == options.height &&
                r.refreshRate == options.refreshRate)
            {
                resolutionIndex = i;
                break;
            }
        }

        if (resolutionIndex == -1)
        {
            // Restore default resolution.
            resolutionIndex = Screen.resolutions.Length - 1;
            options.width = Screen.resolutions[resolutionIndex].width;
            options.height = Screen.resolutions[resolutionIndex].height;
            options.refreshRate = Screen.resolutions[resolutionIndex].refreshRate;
        }
    }

    private void OnEnable()
    {
        LoadOrCreateOptions();
        MemoryToUI();
    }

    private void MemoryToUI()
    {
        resolutionDisplay.text = Screen.resolutions[resolutionIndex].ToString();
        fullscreenModeDisplay.text = options.fullScreenMode.ToString();
        vsyncDisplay.text = options.vSync ? "Yes" : "No";
        masterVolumeSlider.SetValueWithoutNotify(options.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(options.musicVolume);
        keysoundVolumeSlider.SetValueWithoutNotify(options.keysoundVolume);
    }

    public void UIToMemory()
    {
        options.masterVolume = masterVolumeSlider.value;
        options.musicVolume = musicVolumeSlider.value;
        options.keysoundVolume = keysoundVolumeSlider.value;

        ApplyVolume();
    }

    private float VolumeValueToDb(float volume)
    {
        // TODO: find better equation
        return (Mathf.Pow(volume, 0.25f) - 1f) * 80f;
    }

    public void ApplyVolume()
    {
        audioMixer.SetFloat("MasterVolume", VolumeValueToDb(options.masterVolume));
        audioMixer.SetFloat("MusicVolume", VolumeValueToDb(options.musicVolume));
        audioMixer.SetFloat("KeysoundVolume", VolumeValueToDb(options.keysoundVolume));
    }

    public void OnApplyButtonClick()
    {
        options.width = Screen.resolutions[resolutionIndex].width;
        options.height = Screen.resolutions[resolutionIndex].height;
        options.refreshRate = Screen.resolutions[resolutionIndex].refreshRate;
        options.ApplyGraphicSettings();
        options.SaveToFile(Paths.GetOptionsFilePath());
    }

    public void OnTouchscreenTestButtonClick()
    {
        Navigation.GoTo(Navigation.Location.TouchscreenTest);
    }

    public void ModifyResolution(int direction)
    {
        resolutionIndex += direction;
        if (resolutionIndex < 0) resolutionIndex += Screen.resolutions.Length;
        if (resolutionIndex >= Screen.resolutions.Length)
        {
            resolutionIndex -= Screen.resolutions.Length;
        }
        MemoryToUI();
    }

    public void ModifyFullscreenMode(int direction)
    {
        int modeAsInt = (int)options.fullScreenMode;
        int numberOfModes = System.Enum.GetValues(typeof(FullScreenMode)).Length;
        modeAsInt += direction;
        if (modeAsInt < 0) modeAsInt += numberOfModes;
        if (modeAsInt >= numberOfModes) modeAsInt -= numberOfModes;

        options.fullScreenMode = (FullScreenMode)modeAsInt;
        MemoryToUI();
    }

    public void ToggleVsync()
    {
        options.vSync = !options.vSync;
        MemoryToUI();
    }
}
