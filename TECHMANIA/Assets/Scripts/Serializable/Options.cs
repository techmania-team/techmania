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

    public string locale;
    public bool showLoadingBar;
    public bool showFps;
    public bool showJudgementTally;
    public string noteSkin;
    public string vfxSkin;
    public string comboSkin;
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
        noteSkin = "Default";
        vfxSkin = "Default";
        comboSkin = "Default";
        reloadSkinsWhenLoadingPattern = false;

        touchOffsetMs = 0;
        touchLatencyMs = 0;
        keyboardMouseOffsetMs = 0;
        keyboardMouseLatencyMs = 0;

        editorOptions = new EditorOptions();
        modifiers = new Modifiers();
        perTrackOptions = new List<PerTrackOptions>();
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
#if !UNITY_ANDROID
        // Setting resolution on Android causes the graphics to
        // be stretched in the wrong direction.
        Screen.SetResolution(width, height, fullScreenMode, refreshRate);
#endif
        QualitySettings.vSyncCount = vSync ? 1 : 0;
    }

    #region Instance
    public static Options instance { get; private set; }
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
        Normal,
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
        AutoKeysound,
        AutoKeysoundAndTicks,
        AutoKeysoundAndAutoTicks
    }
    public Keysound keysound;

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
        Normal,
        OverrideToTouch,
        OverrideToKeys,
        OverrideToKM
    }
    public ControlOverride controlOverride;

    public enum ScrollSpeed
    {
        Normal,
        HalfSpeed
    }
    public ScrollSpeed scrollSpeed;
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