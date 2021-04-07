using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsPanel : MonoBehaviour
{
    [Header("Graphics")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown fullscreenDropdown;
    public Toggle vSyncToggle;

    [Header("Audio")]
    public Slider masterVolumeSlider;
    public TMP_Text masterVolumeDisplay;
    public Slider musicVolumeSlider;
    public TMP_Text musicVolumeDisplay;
    public Slider keysoundVolumeSlider;
    public TMP_Text keysoundVolumeDisplay;
    public Slider sfxVolumeSlider;
    public TMP_Text sfxVolumeDisplay;
    public TMP_Dropdown audioBufferDropdown;
    public AudioMixer audioMixer;

    [Header("Appearance")]
    public TMP_Dropdown languageDropdown;
    public TextAsset stringTable;
    public Toggle showLoadingBarToggle;
    public Toggle showFpsToggle;

    [Header("Miscellaneous")]
    public TMP_Text latencyDisplay;

    // Make a backup of all available resolutions at startup, because
    // Screen.resolutions may change at runtime. I have no idea why.
    private List<Resolution> resolutions;
    private int resolutionIndex;

    public static void ApplyOptionsOnStartUp()
    {
        OptionsPanel instance = FindObjectOfType<Canvas>()
            .GetComponentInChildren<OptionsPanel>(
            includeInactive: true);
        instance.LoadOrCreateOptions();

        Locale.Load(instance.stringTable, Options.instance.locale);
        Options.instance.ApplyGraphicSettings();
        instance.ApplyAudioBufferSize();
        instance.ApplyVolume();
    }

    private void LoadOrCreateOptions()
    {
        Options.RefreshInstance();

        // Find all resolutions, as well as resolutionIndex.
        resolutions = new List<Resolution>();
        resolutionIndex = -1;
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            Resolution r = Screen.resolutions[i];
            resolutions.Add(r);
            if (r.width == Options.instance.width &&
                r.height == Options.instance.height &&
                r.refreshRate == Options.instance.refreshRate)
            {
                resolutionIndex = i;
            }
        }

        if (resolutionIndex == -1)
        {
            // Restore default resolution.
            resolutionIndex = resolutions.Count - 1;
            Options.instance.width =
                resolutions[resolutionIndex].width;
            Options.instance.height =
                resolutions[resolutionIndex].height;
            Options.instance.refreshRate =
                resolutions[resolutionIndex].refreshRate;
        }
    }

    private void OnEnable()
    {
        LoadOrCreateOptions();
        MemoryToUI();
    }

    private void OnDisable()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
    }

    private void MemoryToUI()
    {
        // Graphics

        resolutionDropdown.ClearOptions();
        foreach (Resolution r in resolutions)
        {
            resolutionDropdown.options.Add(
                new TMP_Dropdown.OptionData(r.ToString()));
        }
        if (resolutions.Count > 1)
        {
            // I believe there's a bug in TMP_Dropdown 2.1.1:
            // SetValueWithoutNotify(0) clears the label, even if the
            // 0-st option is not empty.
            //
            // Setting a non-0 value works around it.
            resolutionDropdown.SetValueWithoutNotify(1);
        }
        resolutionDropdown.SetValueWithoutNotify(resolutionIndex);

        fullscreenDropdown.ClearOptions();
        foreach (FullScreenMode m in
            System.Enum.GetValues(typeof(FullScreenMode)))
        {
            fullscreenDropdown.options.Add(
                new TMP_Dropdown.OptionData(m.ToString()));
        }
        fullscreenDropdown.SetValueWithoutNotify(1);
        fullscreenDropdown.SetValueWithoutNotify(
            (int)Options.instance.fullScreenMode);

        vSyncToggle.SetIsOnWithoutNotify(Options.instance.vSync);

        // Audio

        masterVolumeSlider.SetValueWithoutNotify(
            Options.instance.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(
            Options.instance.musicVolume);
        keysoundVolumeSlider.SetValueWithoutNotify(
            Options.instance.keysoundVolume);
        sfxVolumeSlider.SetValueWithoutNotify(
            Options.instance.sfxVolume);
        UpdateVolumeDisplay();

        UIUtils.MemoryToDropdown(audioBufferDropdown,
            Options.instance.audioBufferSize.ToString(),
            defaultValue: 0);

        // Appearance

        showLoadingBarToggle.SetIsOnWithoutNotify(
            Options.instance.showLoadingBar);
        showFpsToggle.SetIsOnWithoutNotify(
            Options.instance.showFps);

        // Miscellaneous

        latencyDisplay.text = $"{Options.instance.touchOffsetMs}/{Options.instance.touchLatencyMs}/{Options.instance.keyboardMouseOffsetMs}/{Options.instance.keyboardMouseLatencyMs} ms";
    }

    #region Graphics
    public void DumpResolutions()
    {
        string report = $"Resolutions: (total {resolutions.Count})\n";
        foreach (Resolution r in resolutions)
        {
            report += r.ToString() + "\n";
        }
        Debug.Log(report);
    }

    public void OnGraphicsOptionsUpdated()
    {
        resolutionIndex = resolutionDropdown.value;
        Options.instance.width = resolutions[resolutionIndex].width;
        Options.instance.height = resolutions[resolutionIndex].height;
        Options.instance.refreshRate =
            resolutions[resolutionIndex].refreshRate;

        Options.instance.fullScreenMode =
            (FullScreenMode)fullscreenDropdown.value;
        Options.instance.vSync = vSyncToggle.isOn;

        Options.instance.ApplyGraphicSettings();
    }
    #endregion

    #region Audio
    public void OnVolumeChanged()
    {
        Options.instance.masterVolume = masterVolumeSlider.value;
        Options.instance.musicVolume = musicVolumeSlider.value;
        Options.instance.keysoundVolume = keysoundVolumeSlider.value;
        Options.instance.sfxVolume = sfxVolumeSlider.value;
        
        UpdateVolumeDisplay();
        ApplyVolume();
    }

    public void OnAudioBufferSizeChanged()
    {
        Options.instance.audioBufferSize = int.Parse(
            audioBufferDropdown.options[
            audioBufferDropdown.value].text);

        ApplyAudioBufferSize();
        ApplyVolume();
    }

    private float VolumeValueToDb(float volume)
    {
        return (Mathf.Pow(volume, 0.25f) - 1f) * 80f;
    }

    private string VolumeValueToDisplay(float volume)
    {
        return Mathf.RoundToInt(volume * 100f).ToString();
    }

    private void UpdateVolumeDisplay()
    {
        masterVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.masterVolume);
        musicVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.musicVolume);
        keysoundVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.keysoundVolume);
        sfxVolumeDisplay.text = VolumeValueToDisplay(
            Options.instance.sfxVolume);
    }

    private void ApplyVolume()
    {
        audioMixer.SetFloat("MasterVolume", VolumeValueToDb(
            Options.instance.masterVolume));
        audioMixer.SetFloat("MusicVolume", VolumeValueToDb(
            Options.instance.musicVolume));
        audioMixer.SetFloat("KeysoundVolume", VolumeValueToDb(
            Options.instance.keysoundVolume));
        audioMixer.SetFloat("SfxVolume", VolumeValueToDb(
            Options.instance.sfxVolume));
    }

    // This resets the audio mixer, AND it only happens in
    // the standalone player. What the heck? Anyway always reset
    // the audio mixer after calling this.
    private void ApplyAudioBufferSize()
    {
        AudioConfiguration config = AudioSettings.GetConfiguration();
        if (config.dspBufferSize != Options.instance.audioBufferSize)
        {
            config.dspBufferSize = Options.instance.audioBufferSize;
            AudioSettings.Reset(config);
            ResourceLoader.forceReload = true;
        }
    }
    #endregion

    #region Appearance
    public void OnAppearanceOptionsChanged()
    {
        Options.instance.showLoadingBar = showLoadingBarToggle.isOn;
        Options.instance.showFps = showFpsToggle.isOn;
    }
    #endregion
}
