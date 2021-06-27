using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Each format version is a derived class of OptionsBase.

[Serializable]
[FormatVersion(Options.kVersion, typeof(Options), isLatest: true)]
public class OptionsBase : SerializableClass<OptionsBase> {}

// Deserialization will call the constructor, so we can set whatever
// weird default values in the constructor, and they will naturally
// apply to options from earlier versions.
[Serializable]
public class Options : OptionsBase
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

    public string locale;
    public bool showLoadingBar;
    public bool showFps;
    public bool showJudgementTally;
    public bool showLaneDividers;
    public BeatMarkerVisibility beatMarkers;
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

    // Per-track options.
    // This should be a dictionary, but dictionaries are not
    // directly serializable, and we don't expect more than a
    // few hundred elements anyway.
    public List<PerTrackOptions> perTrackOptions;

    // Track list options.

    public TrackListOptions trackListOptions;

    public Options()
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

        locale = Locale.kDefaultLocale;
        showLoadingBar = true;
        showFps = false;
        showJudgementTally = false;
        showLaneDividers = false;
        beatMarkers = BeatMarkerVisibility.Hidden;
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
        perTrackOptions = new List<PerTrackOptions>();
        trackListOptions = new TrackListOptions();
    }

    public static int GetDefaultAudioBufferSize()
    {
        return AudioSettings.GetConfiguration().dspBufferSize;
    }

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

    public void ApplyGraphicSettings()
    {
#if !UNITY_ANDROID && !UNITY_IOS
        // Setting resolution on Android causes the graphics to
        // be stretched in the wrong direction.
        //
        // Resolution is not supported at all on iOS.
        Screen.SetResolution(width, height, fullScreenMode, refreshRate);
#endif

#if UNITY_IOS
        // iOS ignores VSync, and caps the FPS at 30 by default.
        Application.targetFrameRate =   
            Screen.currentResolution.refreshRate;
#else
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
        catch (IOException)
        {
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
    public PerTrackOptions GetPerTrackOptions(Track t)
    {
        string guid = t.trackMetadata.guid;
        foreach (PerTrackOptions options in perTrackOptions)
        {
            if (options.trackGuid == guid) return options;
        }

        PerTrackOptions newOptions = new PerTrackOptions(guid);
        perTrackOptions.Add(newOptions);  // Not written to disk yet.
        return newOptions;
    }
#endregion
}

[Serializable]
public class EditorOptions
{
    // Appearance

    public bool showKeysounds;
    public bool keepScanlineInView;

    // Editing

    public bool applyKeysoundToSelection;
    public bool applyNoteTypeToSelection;
    public bool lockNotesInTime;
    public bool lockDragAnchorsInTime;
    public bool snapDragAnchors;

    // Playback

    public bool metronome;
    public bool assistTickOnSilentNotes;
    public bool returnScanlineAfterPlayback;

    public EditorOptions()
    {
        showKeysounds = true;
        keepScanlineInView = true;

        applyKeysoundToSelection = false;
        applyNoteTypeToSelection = false;
        lockNotesInTime = false;
        lockDragAnchorsInTime = false;
        snapDragAnchors = true;

        metronome = false;
        assistTickOnSilentNotes = false;
        returnScanlineAfterPlayback = true;
    }
}

// All enums reserve the first option as the "normal" one.
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
    public static readonly string[] noteOpacityDisplayKeys =
    {
        "modifier_normal",
        "modifier_fade_out",
        "modifier_fade_out_2",
        "modifier_fade_in",
        "modifier_fade_in_2"
    };

    public enum ScanlineOpacity
    {
        Normal,
        Blink,
        Blink2,
        Blind
    }
    public ScanlineOpacity scanlineOpacity;
    public static readonly string[] scanlineOpacityDisplayKeys =
    {
        "modifier_normal",
        "modifier_blink",
        "modifier_blink_2",
        "modifier_blind"
    };

    public enum ScanDirection
    {
        Normal,  // RL
        RR,
        LR,
        LL
    }
    public ScanDirection scanDirection;
    public static readonly string[] scanDirectionDisplayKeys =
    {
        "modifier_normal",
        "modifier_right_right",
        "modifier_left_right",
        "modifier_left_left"
    };

    public enum NotePosition
    {
        Normal,
        Mirror
    }
    public NotePosition notePosition;
    public static readonly string[] notePositionDisplayKeys =
    {
        "modifier_normal",
        "modifier_mirror"
    };

    public enum ScanPosition
    {
        Normal,
        Swap
    }
    public ScanPosition scanPosition;
    public static readonly string[] scanPositionDisplayKeys =
    {
        "modifier_normal",
        "modifier_swap"
    };

    public enum Fever
    {
        Normal,
        FeverOff,
        AutoFever
    }
    public Fever fever;
    public static readonly string[] feverDisplayKeys =
    {
        "modifier_normal",
        "modifier_fever_off",
        "modifier_auto_fever"
    };

    public enum Keysound
    {
        Normal,
        AutoKeysound
    }
    public Keysound keysound;
    public static readonly string[] keysoundDisplayKeys =
    {
        "modifier_normal",
        "modifier_auto_keysound"
    };

    public enum AssistTick
    {
        None,
        AssistTick,
        AutoAssistTick
    }
    public AssistTick assistTick;
    public static readonly string[] assistTickDisplayKeys =
    {
        "modifier_none",
        "modifier_assist_tick",
        "modifier_auto_assist_tick"
    };

    // Special modifiers

    public enum Mode
    {
        Normal,
        NoFail,
        AutoPlay,
        Practice
    }
    public Mode mode;
    public static readonly string[] modeDisplayKeys =
    {
        "modifier_normal",
        "modifier_no_fail",
        "modifier_auto_play",
        "modifier_practice"
    };

    public enum ControlOverride
    {
        None,
        OverrideToTouch,
        OverrideToKeys,
        OverrideToKM
    }
    public ControlOverride controlOverride;
    public static readonly string[] controlOverrideDisplayKeys =
    {
        "modifier_none",
        "modifier_override_to_touch",
        "modifier_override_to_keys",
        "modifier_override_to_km"
    };

    public enum ScrollSpeed
    {
        Normal,
        HalfSpeed
    }
    public ScrollSpeed scrollSpeed;
    public static readonly string[] scrollSpeedDisplayKeys =
    {
        "modifier_normal",
        "modifier_half_speed"
    };

    // Utilities

    // Does not add "none" segments when all options are 0; does not
    // add per-track options.
    public void ToDisplaySegments(List<string> regularSegments,
        List<string> specialSegments)
    {
        if (noteOpacity != 0)
        {
            regularSegments.Add(Locale.GetString(
                noteOpacityDisplayKeys[(int)noteOpacity]));
        }
        if (scanlineOpacity != 0)
        {
            regularSegments.Add(Locale.GetString(
                scanlineOpacityDisplayKeys[(int)scanlineOpacity]));
        }
        if (scanDirection != 0)
        {
            regularSegments.Add(Locale.GetString(
                scanDirectionDisplayKeys[(int)scanDirection]));
        }
        if (notePosition != 0)
        {
            regularSegments.Add(Locale.GetString(
                notePositionDisplayKeys[(int)notePosition]));
        }
        if (scanPosition != 0)
        {
            regularSegments.Add(Locale.GetString(
                scanPositionDisplayKeys[(int)scanPosition]));
        }
        if (fever != 0)
        {
            regularSegments.Add(Locale.GetString(
                feverDisplayKeys[(int)fever]));
        }
        if (keysound != 0)
        {
            regularSegments.Add(Locale.GetString(
                keysoundDisplayKeys[(int)keysound]));
        }
        if (assistTick != 0)
        {
            regularSegments.Add(Locale.GetString(
                assistTickDisplayKeys[(int)assistTick]));
        }

        if (mode != 0)
        {
            specialSegments.Add(Locale.GetString(
                modeDisplayKeys[(int)mode]));
        }
        if (controlOverride != 0)
        {
            specialSegments.Add(Locale.GetString(
                controlOverrideDisplayKeys[(int)controlOverride]));
        }
        if (scrollSpeed != 0)
        {
            specialSegments.Add(Locale.GetString(
                scrollSpeedDisplayKeys[(int)scrollSpeed]));
        }
    }

    public Scan.Direction GetTopScanDirection()
    {
        switch (scanDirection)
        {
            case ScanDirection.Normal:
            case ScanDirection.RR:
                return Scan.Direction.Right;
            case ScanDirection.LR:
            case ScanDirection.LL:
                return Scan.Direction.Left;
            default:
                throw new Exception();
        }
    }

    public Scan.Direction GetBottomScanDirection()
    {
        switch (scanDirection)
        {
            case ScanDirection.Normal:
            case ScanDirection.LL:
                return Scan.Direction.Left;
            case ScanDirection.LR:
            case ScanDirection.RR:
                return Scan.Direction.Right;
            default:
                throw new Exception();
        }
    }
}

[Serializable]
public class PerTrackOptions
{
    public string trackGuid;
    public bool noVideo;
    public int backgroundBrightness;  // 0-10

    public PerTrackOptions(string trackGuid)
    {
        this.trackGuid = trackGuid;
        noVideo = false;
        backgroundBrightness = 10;
    }
}

[Serializable]
public class TrackListOptions
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

    public TrackListOptions()
    {
        showTracksInAllFolders = false;
        sortBasis = SortBasis.Title;
        sortOrder = SortOrder.Ascending;
    }
}