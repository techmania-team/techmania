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

    [Header("Miscellaneous")]
    public TMP_Text latencyDisplay;

    private Options options;
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
        instance.options.ApplyGraphicSettings();
        instance.ApplyAudioOptions();
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

        // Find all resolutions, as well as resolutionIndex.
        resolutions = new List<Resolution>();
        resolutionIndex = -1;
        for (int i = 0; i < Screen.resolutions.Length; i++)
        {
            Resolution r = Screen.resolutions[i];
            resolutions.Add(r);
            if (r.width == options.width &&
                r.height == options.height &&
                r.refreshRate == options.refreshRate)
            {
                resolutionIndex = i;
            }
        }

        if (resolutionIndex == -1)
        {
            // Restore default resolution.
            resolutionIndex = resolutions.Count - 1;
            options.width = resolutions[resolutionIndex].width;
            options.height = resolutions[resolutionIndex].height;
            options.refreshRate = resolutions[resolutionIndex]
                .refreshRate;
        }
    }

    private void OnEnable()
    {
        LoadOrCreateOptions();
        MemoryToUI();
    }

    private void OnDisable()
    {
        options.SaveToFile(Paths.GetOptionsFilePath());
    }

    private void MemoryToUI()
    {
        // Graphics

        resolutionDropdown.ClearOptions();
        foreach (Resolution r in resolutions)
        {
            resolutionDropdown.options.Add(new TMP_Dropdown.OptionData(r.ToString()));
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
        foreach (FullScreenMode m in System.Enum.GetValues(typeof(FullScreenMode)))
        {
            fullscreenDropdown.options.Add(new TMP_Dropdown.OptionData(m.ToString()));
        }
        fullscreenDropdown.SetValueWithoutNotify(1);
        fullscreenDropdown.SetValueWithoutNotify((int)options.fullScreenMode);

        vSyncToggle.SetIsOnWithoutNotify(options.vSync);

        // Audio

        masterVolumeSlider.SetValueWithoutNotify(options.masterVolume);
        musicVolumeSlider.SetValueWithoutNotify(options.musicVolume);
        keysoundVolumeSlider.SetValueWithoutNotify(options.keysoundVolume);
        sfxVolumeSlider.SetValueWithoutNotify(options.sfxVolume);
        UpdateVolumeDisplay();

        UIUtils.MemoryToDropdown(audioBufferDropdown,
            options.audioBufferSize.ToString(),
            defaultValue: 0);

        // Miscellaneous

        latencyDisplay.text = $"{options.touchLatencyMs}/{options.keyboardLatencyMs}/{options.mouseLatencyMs} ms";
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
        options.width = resolutions[resolutionIndex].width;
        options.height = resolutions[resolutionIndex].height;
        options.refreshRate = resolutions[resolutionIndex].refreshRate;

        options.fullScreenMode = (FullScreenMode)fullscreenDropdown.value;
        options.vSync = vSyncToggle.isOn;

        options.ApplyGraphicSettings();
    }
    #endregion

    #region Audio
    public void OnAudioOptionsUpdated()
    {
        options.masterVolume = masterVolumeSlider.value;
        options.musicVolume = musicVolumeSlider.value;
        options.keysoundVolume = keysoundVolumeSlider.value;
        options.sfxVolume = sfxVolumeSlider.value;
        options.audioBufferSize = int.Parse(
            audioBufferDropdown.options[
            audioBufferDropdown.value].text);

        UpdateVolumeDisplay();
        ApplyAudioOptions();
    }

    private float VolumeValueToDb(float volume)
    {
        return (Mathf.Pow(volume, 0.25f) - 1f) * 80f;
    }

    private string VolumeValueToDisplay(float volume)
    {
        return Mathf.FloorToInt(volume * 100f).ToString();
    }

    private void UpdateVolumeDisplay()
    {
        masterVolumeDisplay.text = VolumeValueToDisplay(options.masterVolume);
        musicVolumeDisplay.text = VolumeValueToDisplay(options.musicVolume);
        keysoundVolumeDisplay.text = VolumeValueToDisplay(options.keysoundVolume);
        sfxVolumeDisplay.text = VolumeValueToDisplay(options.sfxVolume);
    }

    private void ApplyAudioOptions()
    {
        audioMixer.SetFloat("MasterVolume", VolumeValueToDb(options.masterVolume));
        audioMixer.SetFloat("MusicVolume", VolumeValueToDb(options.musicVolume));
        audioMixer.SetFloat("KeysoundVolume", VolumeValueToDb(options.keysoundVolume));
        audioMixer.SetFloat("SfxVolume", VolumeValueToDb(options.sfxVolume));

        AudioConfiguration config = AudioSettings.GetConfiguration();
        config.dspBufferSize = options.audioBufferSize;
        AudioSettings.Reset(config);
    }
    #endregion
}
