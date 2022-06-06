using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;
using FantomLib;
using UnityEngine.Networking;

public class OptionsPanel : MonoBehaviour
{
    public MessageDialog messageDialog;

    [Header("Graphics")]
    public TMP_Dropdown resolutionDropdown;
    public TMP_Dropdown fullscreenDropdown;
    public Toggle vSyncToggle;

    [Header("Audio")]
    public AudioSliders audioSliders;
    public TMP_Dropdown audioBufferDropdown;

    [Header("Appearance")]
    public TMP_Dropdown languageDropdown;
    public TextAsset stringTable;
    public Toggle showLoadingBarToggle;
    public Toggle showFpsToggle;
    public Toggle showJudgementTallyToggle;
    public Toggle showLaneDividersToggle;
    public TMP_Dropdown beatMarkersDropdown;
    public TMP_Dropdown backgroundScalingDropdown;

    [Header("Miscellaneous")]
    public TMP_Dropdown rulesetDropdown;
    public Toggle customDataLocation;
    public GameObject tracksFolder;
    public ScrollingText tracksFolderDisplay;
    public GameObject skinsFolder;
    public ScrollingText skinsFolderDisplay;
    public TextMeshProUGUI latencyDisplay;
    public Toggle pauseWhenGameLosesFocusToggle;
    public Toggle discordRichPresenceToggle;

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

        Locale.Initialize(instance.stringTable);
        Locale.SetLocale(Options.instance.locale);
        Options.instance.ApplyGraphicSettings();
        instance.ApplyAudioBufferSize();
        instance.audioSliders.ApplyVolume();
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

        if (resolutionIndex == -1 && resolutions.Count > 0)
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

        if (Options.instance.ruleset == Options.Ruleset.Custom)
        {
            try
            {
                Ruleset.LoadCustomRuleset();
            }
            catch (System.Exception ex)
            {
                Debug.LogError("An error occurred when loading custom ruleset, reverting to standard ruleset: " + ex.ToString());
                // Silently ignore errors.
                Options.instance.ruleset = Options.Ruleset.Standard;
            }
        }
    }

    private void OnEnable()
    {
        LoadOrCreateOptions();
        MemoryToUI();

        DiscordController.SetActivity(DiscordActivityType.Options);
    }

    private void OnDisable()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
    }

    private void MemoryToUI()
    {
        MemoryToLocalizedUI();

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
        resolutionDropdown.RefreshShownValue();

        vSyncToggle.SetIsOnWithoutNotify(Options.instance.vSync);

        // Audio

        UIUtils.MemoryToDropdown(audioBufferDropdown,
            Options.instance.audioBufferSize.ToString(),
            defaultValue: 0);

        // Appearance

        languageDropdown.ClearOptions();
        foreach (KeyValuePair<string, string> pair in
            Locale.GetLocaleToLanguageName())
        {
            languageDropdown.options.Add(new TMP_Dropdown.OptionData(
                pair.Value));
            if (pair.Key == Options.instance.locale)
            {
                languageDropdown.SetValueWithoutNotify(
                    languageDropdown.options.Count - 1);
                languageDropdown.RefreshShownValue();
            }
        }
        showLoadingBarToggle.SetIsOnWithoutNotify(
            Options.instance.showLoadingBar);
        showFpsToggle.SetIsOnWithoutNotify(
            Options.instance.showFps);
        showJudgementTallyToggle.SetIsOnWithoutNotify(
            Options.instance.showJudgementTally);
        showLaneDividersToggle.SetIsOnWithoutNotify(
            Options.instance.showLaneDividers);

        // Miscellaneous

        customDataLocation.SetIsOnWithoutNotify(
            Options.instance.customDataLocation);
        tracksFolder.SetActive(Options.instance.customDataLocation);
        tracksFolderDisplay.SetUp(Options.instance
            .tracksFolderLocation);
        skinsFolder.SetActive(Options.instance.customDataLocation);
        skinsFolderDisplay.SetUp(Options.instance
            .skinsFolderLocation);
        latencyDisplay.text = $"{Options.instance.touchOffsetMs}/{Options.instance.touchLatencyMs}/{Options.instance.keyboardMouseOffsetMs}/{Options.instance.keyboardMouseLatencyMs} ms";
        pauseWhenGameLosesFocusToggle.SetIsOnWithoutNotify(
            Options.instance.pauseWhenGameLosesFocus);
        discordRichPresenceToggle.SetIsOnWithoutNotify(
            Options.instance.discordRichPresence);
    }

    // The portion of MemoryToUI that should respond to
    // locale change.
    private void MemoryToLocalizedUI()
    {
        UIUtils.InitializeDropdownWithLocalizedOptions(
            fullscreenDropdown,
            "options_fullscreen_mode_exclusive_fullscreen",
            "options_fullscreen_mode_fullscreen_window",
            "options_fullscreen_mode_maximized_window",
            "options_fullscreen_mode_windowed");
        fullscreenDropdown.SetValueWithoutNotify(
            (int)Options.instance.fullScreenMode);
        fullscreenDropdown.RefreshShownValue();

        UIUtils.InitializeDropdownWithLocalizedOptions(
            beatMarkersDropdown,
            "options_beat_markers_hidden",
            "options_beat_markers_show_beat_markers",
            "options_beat_markers_show_half_beat_markers");
        beatMarkersDropdown.SetValueWithoutNotify(
           (int)Options.instance.beatMarkers);
        beatMarkersDropdown.RefreshShownValue();

        UIUtils.InitializeDropdownWithLocalizedOptions(
            backgroundScalingDropdown,
            "options_bg_scaling_fill_entire_screen",
            "options_bg_scaling_fill_game_area");
        backgroundScalingDropdown.SetValueWithoutNotify(
            (int)Options.instance.backgroundScalingMode);
        backgroundScalingDropdown.RefreshShownValue();

        UIUtils.InitializeDropdownWithLocalizedOptions(
            rulesetDropdown,
            "options_ruleset_standard",
            "options_ruleset_legacy",
            "options_ruleset_custom");
        rulesetDropdown.SetValueWithoutNotify(
            (int)Options.instance.ruleset);
        rulesetDropdown.RefreshShownValue();

        DiscordController.SetActivity(DiscordActivityType.Options);
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
    public void OnAudioBufferSizeChanged()
    {
        Options.instance.audioBufferSize = int.Parse(
            audioBufferDropdown.options[
            audioBufferDropdown.value].text);

        ApplyAudioBufferSize();
        audioSliders.ApplyVolume();
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
    public void OnLanguageChanged(int value)
    {
        foreach (KeyValuePair<string, string> pair in
            Locale.GetLocaleToLanguageName())
        {
            if (pair.Value == languageDropdown.options[value].text)
            {
                Options.instance.locale = pair.Key;
                Locale.SetLocale(Options.instance.locale);
                break;
            }
        }

        MemoryToLocalizedUI();
    }

    public void OnAppearanceOptionsChanged()
    {
        Options.instance.showLoadingBar = showLoadingBarToggle.isOn;
        Options.instance.showFps = showFpsToggle.isOn;
        Options.instance.showJudgementTally =
            showJudgementTallyToggle.isOn;
        Options.instance.showLaneDividers =
            showLaneDividersToggle.isOn;
        Options.instance.beatMarkers = (Options.BeatMarkerVisibility)
            beatMarkersDropdown.value;
        Options.instance.backgroundScalingMode =
            (Options.BackgroundScalingMode)
            backgroundScalingDropdown.value;
    }
    #endregion

    #region Miscellaneous
    public void OnRulesetChanged()
    {
        Options.instance.ruleset = (Options.Ruleset)
            rulesetDropdown.value;
        if (Options.instance.ruleset == Options.Ruleset.Custom)
        {
            // Attempt to load custom ruleset.
            try
            {
                Ruleset.LoadCustomRuleset();
            }
            catch (System.IO.FileNotFoundException)
            {
                messageDialog.Show(Locale.GetStringAndFormatIncludingPaths(
                    "custom_ruleset_not_found_error_format",
                    Paths.GetRulesetFilePath()));
                Options.instance.ruleset = Options.Ruleset.Standard;
                MemoryToUI();
            }
            catch (System.Exception ex)
            {
                messageDialog.Show(Locale.GetStringAndFormatIncludingPaths(
                    "custom_ruleset_load_error_format",
                    ex.Message));
                Options.instance.ruleset = Options.Ruleset.Standard;
                MemoryToUI();
            }
        }
    }

    public void OnCustomDataLocationChanged()
    {
        Options.instance.customDataLocation =
            customDataLocation.isOn;
        if (Options.instance.customDataLocation)
        {
            if (string.IsNullOrEmpty(
                Options.instance.tracksFolderLocation))
            {
                Options.instance.tracksFolderLocation =
                    Paths.GetTrackRootFolder();
            }
            if (string.IsNullOrEmpty(
                Options.instance.skinsFolderLocation))
            {
                Options.instance.skinsFolderLocation =
                    Paths.GetSkinFolder();
            }
        }
        SelectTrackPanel.RemoveCachedLists();
        SelectTrackPanel.ResetLocation();
        MemoryToUI();
        Paths.ApplyCustomDataLocation();
    }

    public void OnTracksFolderBrowseButtonClick()
    {
#if UNITY_ANDROID
        AndroidPlugin.OpenStorageFolder(gameObject.name, "OnAndroidTracksFolderSelected", "", true);
#else
        string[] folders = SFB.StandaloneFileBrowser
            .OpenFolderPanel("",
            Options.instance.tracksFolderLocation,
            multiselect: false);
        if (folders.Length == 1)
        {
            OnTracksFolderSelected(folders[0]);
        }
#endif
    }

    public void OnSkinsFolderBrowseButtonClick()
    {
#if UNITY_ANDROID
        AndroidPlugin.OpenStorageFolder(gameObject.name, "OnAndroidSkinsFolderSelected", "", true);
#else
        string[] folders = SFB.StandaloneFileBrowser
            .OpenFolderPanel("",
            Options.instance.skinsFolderLocation,
            multiselect: false);
        if (folders.Length == 1)
        {
            OnSkinsFolderSelected(folders[0]);
        }
#endif
    }

    private void OnAndroidTracksFolderSelected(string result)
    {
        if (result[0] == '{')
        {
            ContentInfo info = JsonUtility.FromJson<ContentInfo>(result);
            OnTracksFolderSelected(info.path);
        }
    }

    private void OnAndroidSkinsFolderSelected(string result)
    {
        if (result[0] == '{')
        {
            ContentInfo info = JsonUtility.FromJson<ContentInfo>(result);
            OnSkinsFolderSelected(info.path);
        }
    }

    private void OnTracksFolderSelected(string fullPath)
    {
        Options.instance.tracksFolderLocation = fullPath;
        SelectTrackPanel.RemoveCachedLists();
        SelectTrackPanel.ResetLocation();
        MemoryToUI();
        Paths.ApplyCustomDataLocation();
    }

    private void OnSkinsFolderSelected(string fullPath)
    {
        Options.instance.skinsFolderLocation = fullPath;
        MemoryToUI();
        Paths.ApplyCustomDataLocation();
    }

    public void OnPauseWhenGameLosesFocusChanged()
    {
        Options.instance.pauseWhenGameLosesFocus =
            pauseWhenGameLosesFocusToggle.isOn;
    }

    public void OnDiscordRichPresenceChanged()
    {
        Options.instance.discordRichPresence =
            discordRichPresenceToggle.isOn;
        if (Options.instance.discordRichPresence)
        {
            DiscordController.Start();
            DiscordController.SetActivity(DiscordActivityType.Options);
        }
        else
        {
            DiscordController.Dispose();
        }
    }

    public void OnDiscordRichPresenceReconnectButtonClick()
    {
        DiscordController.Start();
        DiscordController.SetActivity(DiscordActivityType.Options);
    } 
    #endregion
}
