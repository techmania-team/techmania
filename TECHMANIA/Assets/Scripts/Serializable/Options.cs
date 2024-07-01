using JetBrains.Annotations;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;

// Each format version is a derived class of OptionsBase.

[Serializable]
[FormatVersion(OptionsV1.kVersion, typeof(OptionsV1), isLatest: false)]
[FormatVersion(OptionsV2.kVersion, typeof(OptionsV2), isLatest: false)]
[FormatVersion(Options.kVersion, typeof(Options), isLatest: true)]
public class OptionsBase : SerializableClass<OptionsBase>
{
    public void SaveToFile()
    {
        SaveToFile(Paths.GetOptionsFilePath());
    }
}

// Deserialization will call the constructor, so we can set whatever
// weird default values in the constructor, and they will naturally
// apply to options from earlier versions.
//
// Updates in version 2:
// - Now defines volumes in integer percents, so there's less
//   float fuckery.
//
// Updates in version 3:
// - Most appearance options are moved to theme-specific options.
// - Replaced refreshRate with refreshRateNumerator and
//   refreshRateDenominator, in line with Unity 2022.2 deprecating
//   Resolution.refreshRate.
// - Per-track options are now serialized as a dictionary.
[MoonSharp.Interpreter.MoonSharpUserData]
[Serializable]
public class Options : OptionsBase
{
    public const string kVersion = "3";

    // Graphics

    public int width; 
    public int height;
    public uint refreshRateNumerator;
    public uint refreshRateDenominator;
    public FullScreenMode fullScreenMode;
    public bool vSync;

    // Audio

    public int masterVolumePercent;
    public int musicVolumePercent;
    public int keysoundVolumePercent;
    public int sfxVolumePercent;
    public int audioBufferSize;
    public int numAudioBuffers;
    public bool useAsio;

    // Appearance

    public string locale;
    public string noteSkin;
    public string vfxSkin;
    public string comboSkin;
    public string gameUiSkin;
    public const string kDefaultSkin = "Default";
    public bool reloadSkinsWhenLoadingPattern;
    public string theme;
    public const string kDefaultTheme = "Default";

    // Timing

    public int touchOffsetMs;
    public int touchLatencyMs;
    public int keyboardMouseOffsetMs;
    public int keyboardMouseLatencyMs;

    // Miscellaneous

    public enum Ruleset
    {
        Standard,
        Legacy,
        Custom
    }
    public Ruleset ruleset;
    // Remember to call Paths.ApplyCustomDataLocation after modifying
    // these paths.
    public bool customDataLocation;
    public string tracksFolderLocation;
    public string setlistsFolderLocation;
    public string skinsFolderLocation;
    public string themesFolderLocation;
    // Call TurnOn/OffDiscordRichPresence instead of setting this
    // directly.
    public bool discordRichPresence
    {
        get;
        [MoonSharp.Interpreter.MoonSharpHidden]
        set;
    }

    // Editor options

    public EditorOptions editorOptions;

    // Modifiers

    public Modifiers modifiers;

    // Per-track options. Dictionary is keyed by GUID.
    // Themes should access this with GetPerTrackOptions.
    //
    // There is a non-serialized, in-memory version and a
    // serialized version of the same dictionary. This is because
    // most per-track options will be the same as default, and
    // we don't want to save those to disk. But we still want to
    // keep them in memory so references to them can be retained.
    // Therefore:
    //
    // - GetPerTrackOptions serves from the in-memory version, and
    //   all callers operate on it without caring about the
    //   serialized version
    // - On serialization, non-default options are copied from
    //   in-memory to serialized
    // - On deserialization, all options are copied from serialized
    //   to in-memory

    [NonSerialized]
    [MoonSharp.Interpreter.MoonSharpHidden]
    public Dictionary<string, PerTrackOptions> 
        inMemoryPerTrackOptions;
    [MoonSharp.Interpreter.MoonSharpHidden]
    public Dictionary<string, PerTrackOptions> perTrackOptions;

    // Per-theme options. Dictionary is keyed by themes'
    // self-declared names when calling GetThemeOptions.
    // Themes should access this with GetThemeOptions.

    [MoonSharp.Interpreter.MoonSharpHidden]
    public Dictionary<string, Dictionary<string, string>> themeOptions;

    /* Default theme options
     * 
     * Key                          Value
     * ---------------------------------------------------------
     * showLoadingBar               True/False
     * showFps                      True/False
     * showJudgementTally           True/False
     * showLaneDividers             True/False
     * beatMarkers                  Hidden
     *                              ShowBeatMarkers
     *                              ShowHalfBeatMarkers
     * backgroundScalingMode        FillEntireScreen
     *                              FillGameArea
     * pauseWhenGameLosesFocus      True/False
     * pauseButtonInteraction       SingleTap/DoubleTap/Hold
     */

    public Options()
    {
        version = kVersion;

        width = 0;
        height = 0;
        refreshRateNumerator = 0;
        refreshRateDenominator = 0;
        fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        vSync = false;

        masterVolumePercent = 100;
        musicVolumePercent = 80;
        keysoundVolumePercent = 100;
        sfxVolumePercent = 100;
        audioBufferSize = 1024;
        numAudioBuffers = 4;
        useAsio = false;

        locale = L10n.kDefaultLocale;
        noteSkin = "Default";
        vfxSkin = "Default";
        comboSkin = "Default";
        gameUiSkin = "Default";
        reloadSkinsWhenLoadingPattern = false;
        theme = kDefaultTheme;

        touchOffsetMs = 0;
        touchLatencyMs = 0;
        keyboardMouseOffsetMs = 0;
        keyboardMouseLatencyMs = 0;

        ruleset = Ruleset.Standard;
        customDataLocation = false;
        tracksFolderLocation = "";
        skinsFolderLocation = "";
        themesFolderLocation = "";
        discordRichPresence = true;

        editorOptions = new EditorOptions();
        modifiers = new Modifiers();
        inMemoryPerTrackOptions = new Dictionary<
            string, PerTrackOptions>();
        perTrackOptions = new Dictionary<string, PerTrackOptions>();
        themeOptions = new Dictionary<string,
            Dictionary<string, string>>();

        Dictionary<string, string> defaultThemeOptions =
            new Dictionary<string, string>
            {
                { "showLoadingBar", true.ToString() },
                { "showFps", false.ToString() },
                { "showJudgementTally", false.ToString() },
                { "showLaneDividers", false.ToString() },
                { "beatMarkers", "Hidden" },
                {
                    "backgroundScalingMode",
                    "FillEntireScreen"
                },
                { "pauseWhenGameLosesFocus", true.ToString() },
                { "pauseButtonInteraction", "SingleTap" }
            };
        themeOptions.Add(kDefaultTheme, defaultThemeOptions);
    }

    protected override void PrepareToSerialize()
    {
        PreparePerTrackOptionsToSerialize();
    }

    protected override void InitAfterDeserialize()
    {
        InitPerTrackOptionsAfterDeserialize();
    }

    public void ResetCustomDataLocation ()
    {
        Debug.Log("Resetting custom data location.");
        customDataLocation = false;
        noteSkin = kDefaultSkin;
        vfxSkin = kDefaultSkin;
        comboSkin = kDefaultSkin;
        gameUiSkin = kDefaultSkin;
        tracksFolderLocation = "";
        skinsFolderLocation = "";
        themesFolderLocation = "";
        SaveToFile(Paths.GetOptionsFilePath());
    }

    #region Offset & latency
    public int GetOffsetForControlScheme(ControlScheme scheme)
    {
        switch (scheme)
        {
            case ControlScheme.Touch:
                return touchOffsetMs;
            default:
                return keyboardMouseOffsetMs;
        }
    }

    public int GetLatencyForDevice(InputDevice device)
    {
        switch (device)
        {
            case InputDevice.Touchscreen:
                return touchLatencyMs;
            default:
                return keyboardMouseLatencyMs;
        }
    }
    #endregion

    #region Graphics
    // This only changes options and does not apply the modified
    // resolution or save to file.
    public void SetDefaultResolutionIfInvalid()
    {
#if UNITY_ANDROID || UNITY_IOS
        return;
#else
        foreach (Resolution r in Screen.resolutions)
        {
            if (r.width == width && r.height == height &&
                r.refreshRateRatio.numerator == refreshRateNumerator &&
                r.refreshRateRatio.denominator == refreshRateDenominator)
            {
                // Current resolution is valid; do nothing.
                return;
            }
        }

        Resolution def = Screen.resolutions[^1];
        width = def.width;
        height = def.height;
        refreshRateNumerator = def.refreshRateRatio.numerator;
        refreshRateDenominator = def.refreshRateRatio.denominator;
#endif
    }

    public Resolution GetCurrentResolutionAsObject()
    {
        return new Resolution()
        {
            width = this.width,
            height = this.height,
            refreshRateRatio = new RefreshRate()
            {
                numerator = refreshRateNumerator,
                denominator = refreshRateDenominator
            }
        };
    }

    public void ApplyGraphicSettings()
    {
#if UNITY_IOS || UNITY_ANDROID
        // Ignore resolution and VSync as they don't make sense
        // on mobile platforms.
        // However, these platforms cap FPS at 30 by default, so we
        // need to unlock it.
        Application.targetFrameRate =   
            Screen.currentResolution.refreshRate;
        QualitySettings.vSyncCount = 0;
#else
        Screen.SetResolution(width, height, fullScreenMode,
            new RefreshRate()
            {
                numerator = refreshRateNumerator,
                denominator = refreshRateDenominator
            });
        QualitySettings.vSyncCount = vSync ? 1 : 0;
#endif
    }

    // Used for loading stuff when limited to 1 frame per asset.
    public static void TemporarilyDisableVSync()
    {
        QualitySettings.vSyncCount = 0;
    }

    public static void RestoreVSync()
    {
        QualitySettings.vSyncCount = instance.vSync ? 1 : 0;
    }
    #endregion

    #region Audio
    public void ApplyVolumeSettings()
    {
        AudioManager.instance.ApplyVolume();
    }

    public static int GetDefaultAudioBufferSize()
    {
        Debug.LogWarning("GetDefaultAudioBufferSize() is deprecated, and hardcoded to 1024.");
        return 1024;
    }

    public void ApplyAudioBufferSize()
    {
        Debug.LogWarning("TECHMANIA no longer allows setting audio buffer size at runtime.");
    }

    public void ApplyAsio()
    {
        FmodManager.instance.useASIO = useAsio;
    }

    public static float VolumeValueToDb(int volumePercent)
    {
        float volume = volumePercent * 0.01f;
        return (Mathf.Pow(volume, 0.25f) - 1f) * 80f;
    }
    #endregion

    #region Discord
    // Call this instead of setting discordRichPresence directly,
    // or rich presence will not immediately appear in Discord.
    public void TurnOnDiscordRichPresence()
    {
        discordRichPresence = true;
        DiscordController.Start();
    }

    // Call this instead of setting discordRichPresence directly,
    // or rich presence will not immediately disappear in Discord.
    public void TurnOffDiscordRichPresence()
    {
        discordRichPresence = false;
        DiscordController.Dispose();
    }
    #endregion

    #region Instance
    public static Options instance { get; private set; }
    private static Options backupInstance;
    public static void RefreshInstance()
    {
        try
        {
            instance = LoadFromFile(
                Paths.GetOptionsFilePath()) as Options;
        }
        catch (Exception ex)
        {
            Debug.LogError("An error occurred when loading options. All options will be reverted to default. See next error for details.");
            Debug.LogException(ex);
            instance = new Options();
        }
    }

    public static void MakeBackup()
    {
        backupInstance = instance.Clone() as Options;
    }

    public static void RestoreBackup()
    {
        instance = backupInstance;
    }
    #endregion

    #region Per-track options
    public PerTrackOptions GetPerTrackOptions(string guid)
    {
        if (inMemoryPerTrackOptions == null)
        {
            inMemoryPerTrackOptions = new Dictionary<string, PerTrackOptions>();
        }

        if (!inMemoryPerTrackOptions.ContainsKey(guid))
        {
            PerTrackOptions newOptions = new PerTrackOptions();
            inMemoryPerTrackOptions.Add(guid, newOptions);
            // At this time the new options are not written to disk.
        }

        return inMemoryPerTrackOptions[guid];
    }

    private void PreparePerTrackOptionsToSerialize()
    {
        if (inMemoryPerTrackOptions == null)
        {
            inMemoryPerTrackOptions = new Dictionary<string, PerTrackOptions>();
        }

        perTrackOptions = new Dictionary<string, PerTrackOptions>();
        foreach (KeyValuePair<string, PerTrackOptions> pair in
            inMemoryPerTrackOptions)
        {
            if (pair.Value.SameAsDefault()) continue;
            perTrackOptions.Add(pair.Key, pair.Value);
        }
    }

    private void InitPerTrackOptionsAfterDeserialize()
    {
        inMemoryPerTrackOptions = new
            Dictionary<string, PerTrackOptions>();
        foreach (KeyValuePair<string, PerTrackOptions> pair in
            perTrackOptions)
        {
            inMemoryPerTrackOptions.Add(pair.Key, pair.Value);
        }
    }
    #endregion

    #region Per-theme options
    public Dictionary<string, string> GetThemeOptions(string themeName)
    {
        if (!themeOptions.ContainsKey(themeName))
        {
            themeOptions.Add(themeName,
                new Dictionary<string, string>());
        }
        return themeOptions[themeName];
    }
    #endregion
}

[MoonSharp.Interpreter.MoonSharpUserData]
[Serializable]
public class EditorOptions
{
    // Edit tab

    public int beatSnapDivisor;
    public int visibleLanes;

    // Appearance

    public bool showKeysounds;
    public bool keepScanlineInView;

    // Editing

    public bool applyKeysoundToSelection;
    public bool applyNoteTypeToSelection;
    public bool lockNotesInTime;
    public bool lockDragAnchorsInTime;
    public bool snapDragAnchors;
    public bool autoSave;

    // Playback

    public bool metronome;
    public bool assistTickOnSilentNotes;
    public bool returnScanlineAfterPlayback;

    public EditorOptions()
    {
        beatSnapDivisor = 2;
        visibleLanes = 12;

        showKeysounds = true;
        keepScanlineInView = true;

        applyKeysoundToSelection = false;
        applyNoteTypeToSelection = false;
        lockNotesInTime = false;
        lockDragAnchorsInTime = false;
        snapDragAnchors = true;
        autoSave = false;

        metronome = false;
        assistTickOnSilentNotes = false;
        returnScanlineAfterPlayback = true;
    }

    public EditorOptions Clone()
    {
        return new EditorOptions()
        {
            showKeysounds = showKeysounds,
            keepScanlineInView = keepScanlineInView,

            applyKeysoundToSelection = applyKeysoundToSelection,
            applyNoteTypeToSelection = applyNoteTypeToSelection,
            lockNotesInTime = lockNotesInTime,
            lockDragAnchorsInTime = lockDragAnchorsInTime,
            snapDragAnchors = snapDragAnchors,

            metronome = metronome,
            assistTickOnSilentNotes = assistTickOnSilentNotes,
            returnScanlineAfterPlayback = returnScanlineAfterPlayback,

            autoSave = autoSave
        };
    }
}

// All enums reserve the first option as the "normal" one.
[MoonSharp.Interpreter.MoonSharpUserData]
[Serializable]
public class Modifiers
{
    public static Modifiers instance 
    { 
        get { return Options.instance.modifiers; }
    }

    // Regular modifiers

    public enum NoteOpacity
    {
        Normal,
        FadeOut,
        FadeOut2,
        FadeIn,
        FadeIn2
    }
    public NoteOpacity noteOpacity;

    public enum ScanlineOpacity
    {
        Normal,
        Blink,
        Blink2,
        Blind
    }
    public ScanlineOpacity scanlineOpacity;

    public enum ScanDirection
    {
        Normal,  // RL
        RR,
        LR,
        LL
    }
    public ScanDirection scanDirection;

    public enum NotePosition
    {
        Normal,
        Mirror
    }
    public NotePosition notePosition;

    public enum ScanPosition
    {
        Normal,
        Swap
    }
    public ScanPosition scanPosition;

    public enum Fever
    {
        Normal,
        FeverOff,
        AutoFever
    }
    public Fever fever;

    public enum Keysound
    {
        Normal,
        AutoKeysound
    }
    public Keysound keysound;

    public enum AssistTick
    {
        None,
        AssistTick,
        AutoAssistTick
    }
    public AssistTick assistTick;

    public enum SuddenDeath
    {
        Normal,
        SuddenDeath
    }
    public SuddenDeath suddenDeath;

    // Special modifiers

    public enum Mode
    {
        Normal,
        NoFail,
        AutoPlay,
        Practice
    }
    public Mode mode;

    public enum ControlOverride
    {
        None,
        OverrideToTouch,
        OverrideToKeys,
        OverrideToKM
    }
    public ControlOverride controlOverride;

    public enum ScrollSpeed
    {
        Normal,
        HalfSpeed,
        ShiftedHalfSpeed
    }
    public ScrollSpeed scrollSpeed;

    // Utilities

    public bool HasAnySpecialModifier()
    {
        if (mode != Mode.Normal) return true;
        if (controlOverride != ControlOverride.None) return true;
        if (scrollSpeed != ScrollSpeed.Normal) return true;
        return false;
    }

    public Modifiers Clone()
    {
        return new Modifiers()
        {
            noteOpacity = noteOpacity,
            scanlineOpacity = scanlineOpacity,
            scanDirection = scanDirection,
            notePosition = notePosition,
            scanPosition = scanPosition,
            fever = fever,
            keysound = keysound,
            assistTick = assistTick,
            mode = mode,
            controlOverride = controlOverride,
            scrollSpeed = scrollSpeed
        };
    }
}

[MoonSharp.Interpreter.MoonSharpUserData]
[Serializable]
public class PerTrackOptions
{
    public bool noVideo;
    public int backgroundBrightness;  // 0-10
    public const int kMaxBrightness = 10;

    public PerTrackOptions()
    {
        noVideo = false;
        backgroundBrightness = kMaxBrightness;
    }

    public PerTrackOptions Clone()
    {
        return new PerTrackOptions()
        {
            noVideo = noVideo,
            backgroundBrightness = backgroundBrightness
        };
    }

    public bool SameAsDefault()
    {
        return !noVideo && backgroundBrightness == kMaxBrightness;
    }
}

// Only used in OptionsV1 and OptionsV2.
[Serializable]
public class TrackFilter
{
    public bool showTracksInAllFolders;

    public enum SortBasis
    {
        Title,
        Artist,
        Genre,
        TouchLevel,
        KeysLevel,
        KMLevel
    };
    public enum SortOrder
    { 
        Ascending,
        Descending
    };

    public SortBasis sortBasis;
    public SortOrder sortOrder;

    public TrackFilter()
    {
        showTracksInAllFolders = false;
        sortBasis = SortBasis.Title;
        sortOrder = SortOrder.Ascending;
    }

    public static TrackFilter instance
    {
        get 
        {
            // return Options.instance.trackFilter;
            return null;
        }
    }

    public static readonly string[] sortBasisDisplayKeys =
    {
        "track_filter_sidesheet_sort_basis_title",
        "track_filter_sidesheet_sort_basis_artist",
        "track_filter_sidesheet_sort_basis_genre",
        "track_filter_sidesheet_sort_basis_touch_level",
        "track_filter_sidesheet_sort_basis_keys_level",
        "track_filter_sidesheet_sort_basis_km_level"
    };

    public TrackFilter Clone()
    {
        return new TrackFilter()
        {
            showTracksInAllFolders = showTracksInAllFolders,
            sortBasis = sortBasis,
            sortOrder = sortOrder
        };
    }
}

// For deserialization with OptionsV2 and OptionsV1.
[Serializable]
public class PerTrackOptionsWithGuid
{
    public string trackGuid;
    public bool noVideo;
    public int backgroundBrightness;  // 0-10
    public const int kMaxBrightness = 10;

    public PerTrackOptionsWithGuid(string trackGuid)
    {
        this.trackGuid = trackGuid;
        noVideo = false;
        backgroundBrightness = kMaxBrightness;
    }

    public PerTrackOptionsWithGuid Clone()
    {
        return new PerTrackOptionsWithGuid(trackGuid)
        {
            noVideo = noVideo,
            backgroundBrightness = backgroundBrightness
        };
    }

    public PerTrackOptions DropGuid()
    {
        return new PerTrackOptions()
        {
            noVideo = noVideo,
            backgroundBrightness = backgroundBrightness
        };
    }
}

[Serializable]
public class OptionsV2 : OptionsBase
{
    public const string kVersion = "2";

    public const string kDefaultTheme = "Default";

    // Graphics

    public int width;
    public int height;
    public int refreshRate;
    public FullScreenMode fullScreenMode;
    public bool vSync;

    // Audio

    public int masterVolumePercent;
    public int musicVolumePercent;
    public int keysoundVolumePercent;
    public int sfxVolumePercent;
    public int audioBufferSize;

    // Appearance

    public enum BeatMarkerVisibility
    {
        Hidden,
        ShowBeatMarkers,
        ShowHalfBeatMarkers
    }
    public enum BackgroundScalingMode
    {
        FillEntireScreen,
        // Fill the area under the top bar.
        FillGameArea
    }

    public string locale;
    public bool showLoadingBar;
    public bool showFps;
    public bool showJudgementTally;
    public bool showLaneDividers;
    public BeatMarkerVisibility beatMarkers;
    public BackgroundScalingMode backgroundScalingMode;
    public string noteSkin;
    public string vfxSkin;
    public string comboSkin;
    public string gameUiSkin;
    public bool reloadSkinsWhenLoadingPattern;
    public string theme;

    // Timing

    public int touchOffsetMs;
    public int touchLatencyMs;
    public int keyboardMouseOffsetMs;
    public int keyboardMouseLatencyMs;

    // Miscellaneous

    public enum Ruleset
    {
        Standard,
        Legacy,
        Custom
    }
    public Ruleset ruleset;
    public bool customDataLocation;
    public string tracksFolderLocation;
    public string skinsFolderLocation;
    public bool pauseWhenGameLosesFocus;

    // Editor options

    public EditorOptions editorOptions;

    // Modifiers

    public Modifiers modifiers;

    // Track list options.

    public TrackFilter trackFilter;

    // Per-track options.
    // This should be a dictionary, but dictionaries are not
    // directly serializable, and we don't expect more than a
    // few hundred elements anyway.
    public List<PerTrackOptionsWithGuid> perTrackOptions;

    public OptionsV2()
    {
        version = kVersion;

        width = 0;
        height = 0;
        refreshRate = 0;
        fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        vSync = false;

        masterVolumePercent = 100;
        musicVolumePercent = 80;
        keysoundVolumePercent = 100;
        sfxVolumePercent = 100;
        // Cannot call GetDefaultAudioBufferSize() here, because
        // somehow Unity calls this constructor during serialization,
        // and calling AudioSettings.GetConfiguration() at that time
        // causes an exception.
        audioBufferSize = 512;

        locale = L10n.kDefaultLocale;
        showLoadingBar = true;
        showFps = false;
        showJudgementTally = false;
        showLaneDividers = false;
        beatMarkers = BeatMarkerVisibility.Hidden;
        backgroundScalingMode = BackgroundScalingMode
            .FillEntireScreen;
        noteSkin = "Default";
        vfxSkin = "Default";
        comboSkin = "Default";
        gameUiSkin = "Default";
        reloadSkinsWhenLoadingPattern = false;
        theme = kDefaultTheme;

        touchOffsetMs = 0;
        touchLatencyMs = 0;
        keyboardMouseOffsetMs = 0;
        keyboardMouseLatencyMs = 0;

        ruleset = Ruleset.Standard;
        customDataLocation = false;
        tracksFolderLocation = "";
        skinsFolderLocation = "";
        pauseWhenGameLosesFocus = true;

        editorOptions = new EditorOptions();
        modifiers = new Modifiers();
        perTrackOptions = new List<PerTrackOptionsWithGuid>();
        trackFilter = new TrackFilter();
    }

    protected override OptionsBase Upgrade()
    {
        Options upgraded = new Options()
        {
            width = width,
            height = height,
            refreshRateNumerator = (uint)refreshRate * 1000,
            refreshRateDenominator = 1000,
            fullScreenMode = fullScreenMode,
            vSync = vSync,

            masterVolumePercent = masterVolumePercent,
            musicVolumePercent = musicVolumePercent,
            keysoundVolumePercent = keysoundVolumePercent,
            sfxVolumePercent = sfxVolumePercent,
            audioBufferSize = audioBufferSize,

            locale = locale,
            noteSkin = noteSkin,
            vfxSkin = vfxSkin,
            comboSkin = comboSkin,
            gameUiSkin = gameUiSkin,
            reloadSkinsWhenLoadingPattern =
                reloadSkinsWhenLoadingPattern,
            theme = theme,

            touchOffsetMs = touchOffsetMs,
            touchLatencyMs = touchLatencyMs,
            keyboardMouseOffsetMs = keyboardMouseOffsetMs,
            keyboardMouseLatencyMs = keyboardMouseLatencyMs,

            ruleset = (Options.Ruleset)ruleset,
            customDataLocation = customDataLocation,
            tracksFolderLocation = tracksFolderLocation,
            skinsFolderLocation = skinsFolderLocation,

            editorOptions = editorOptions.Clone(),
            modifiers = modifiers.Clone(),
            inMemoryPerTrackOptions = new Dictionary<
                string, PerTrackOptions>(),
            perTrackOptions = new Dictionary<string, PerTrackOptions>(),
            themeOptions = new Dictionary<string,
                Dictionary<string, string>>()
        };

        foreach (PerTrackOptionsWithGuid o in perTrackOptions)
        {
            upgraded.inMemoryPerTrackOptions.Add(o.trackGuid, 
                o.DropGuid());
            upgraded.perTrackOptions.Add(o.trackGuid, o.DropGuid());
        }

        Dictionary<string, string> defaultThemeOptions =
            new Dictionary<string, string>
            {
                {
                    "showLoadingBar",
                    showLoadingBar.ToString()
                },
                { "showFps", showFps.ToString() },
                {
                    "showJudgementTally",
                    showJudgementTally.ToString()
                },
                {
                    "showLaneDividers",
                    showLaneDividers.ToString()
                },
                {
                    "beatMarkers",
                    beatMarkers.ToString()
                },
                {
                    "backgroundScalingMode",
                    backgroundScalingMode.ToString()
                },
                {
                    "pauseWhenGameLosesFocus",
                    pauseWhenGameLosesFocus.ToString()
                }
            };
        upgraded.themeOptions.Add(kDefaultTheme, defaultThemeOptions);

        return upgraded;
    }
}

[Serializable]
public class OptionsV1 : OptionsBase
{
    public const string kVersion = "1";

    // Graphics

    public int width;
    public int height;
    public int refreshRate;
    public FullScreenMode fullScreenMode;
    public bool vSync;

    // Audio

    public float masterVolume;
    public float musicVolume;
    public float keysoundVolume;
    public float sfxVolume;
    public int audioBufferSize;

    // Appearance
    public enum BeatMarkerVisibility
    {
        Hidden,
        ShowBeatMarkers,
        ShowHalfBeatMarkers
    }
    public enum BackgroundScalingMode
    {
        FillEntireScreen,
        // Fill the area under the top bar.
        FillGameArea
    }

    public string locale;
    public bool showLoadingBar;
    public bool showFps;
    public bool showJudgementTally;
    public bool showLaneDividers;
    public BeatMarkerVisibility beatMarkers;
    public BackgroundScalingMode backgroundScalingMode;
    public string noteSkin;
    public string vfxSkin;
    public string comboSkin;
    public string gameUiSkin;
    public bool reloadSkinsWhenLoadingPattern;

    // Timing

    public int touchOffsetMs;
    public int touchLatencyMs;
    public int keyboardMouseOffsetMs;
    public int keyboardMouseLatencyMs;

    // Editor options

    public EditorOptions editorOptions;

    // Modifiers

    public Modifiers modifiers;

    // Track list options.

    public TrackFilter trackFilter;

    // Per-track options.
    // This should be a dictionary, but dictionaries are not
    // directly serializable, and we don't expect more than a
    // few hundred elements anyway.
    public List<PerTrackOptionsWithGuid> perTrackOptions;

    public OptionsV1()
    {
        version = kVersion;

        width = 0;
        height = 0;
        refreshRate = 0;
        fullScreenMode = FullScreenMode.ExclusiveFullScreen;
        vSync = false;

        masterVolume = 1f;
        musicVolume = 0.8f;
        keysoundVolume = 1f;
        sfxVolume = 1f;
        // Cannot call GetDefaultAudioBufferSize() here, because
        // somehow Unity calls this constructor during serialization,
        // and calling AudioSettings.GetConfiguration() at that time
        // causes an exception.
        audioBufferSize = 512;

        locale = L10n.kDefaultLocale;
        showLoadingBar = true;
        showFps = false;
        showJudgementTally = false;
        showLaneDividers = false;
        beatMarkers = BeatMarkerVisibility.Hidden;
        backgroundScalingMode = BackgroundScalingMode
            .FillEntireScreen;
        noteSkin = "Default";
        vfxSkin = "Default";
        comboSkin = "Default";
        gameUiSkin = "Default";
        reloadSkinsWhenLoadingPattern = false;

        touchOffsetMs = 0;
        touchLatencyMs = 0;
        keyboardMouseOffsetMs = 0;
        keyboardMouseLatencyMs = 0;

        editorOptions = new EditorOptions();
        modifiers = new Modifiers();
        perTrackOptions = new List<PerTrackOptionsWithGuid>();
        trackFilter = new TrackFilter();
    }

    protected override OptionsBase Upgrade()
    {
        OptionsV2 upgraded = new OptionsV2()
        {
            width = width,
            height = height,
            refreshRate = refreshRate,
            fullScreenMode = fullScreenMode,
            vSync = vSync,

            masterVolumePercent = Mathf.FloorToInt(
                masterVolume * 100f),
            musicVolumePercent = Mathf.FloorToInt(
                musicVolume * 100f),
            keysoundVolumePercent = Mathf.FloorToInt(
                keysoundVolume * 100f),
            sfxVolumePercent = Mathf.FloorToInt(
                sfxVolume * 100f),
            audioBufferSize = audioBufferSize,

            locale = locale,
            showLoadingBar = showLoadingBar,
            showFps = showFps,
            showJudgementTally = showJudgementTally,
            showLaneDividers = showLaneDividers,
            beatMarkers = (OptionsV2.BeatMarkerVisibility)beatMarkers,
            backgroundScalingMode = (OptionsV2.BackgroundScalingMode)
                backgroundScalingMode,
            noteSkin = noteSkin,
            vfxSkin = vfxSkin,
            comboSkin = comboSkin,
            gameUiSkin = gameUiSkin,
            reloadSkinsWhenLoadingPattern =
                reloadSkinsWhenLoadingPattern,

            touchOffsetMs = touchOffsetMs,
            touchLatencyMs = touchLatencyMs,
            keyboardMouseOffsetMs = keyboardMouseOffsetMs,
            keyboardMouseLatencyMs = keyboardMouseLatencyMs,

            editorOptions = editorOptions.Clone(),

            modifiers = modifiers.Clone(),

            trackFilter = trackFilter.Clone(),

            perTrackOptions = new List<PerTrackOptionsWithGuid>()
        };
        foreach (PerTrackOptionsWithGuid o in perTrackOptions)
        {
            upgraded.perTrackOptions.Add(o.Clone());
        }
        return upgraded;
    }
}
