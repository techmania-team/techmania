using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Audio;

// Each format version is a derived class of OptionsBase.

[Serializable]
public class OptionsBase
{
    public string version;

    private string Serialize()
    {
        return UnityEngine.JsonUtility.ToJson(this, prettyPrint: true);
    }
    private static OptionsBase Deserialize(string json)
    {
        string version = UnityEngine.JsonUtility.FromJson<OptionsBase>(json).version;
        switch (version)
        {
            case Options.kVersion:
                return UnityEngine.JsonUtility.FromJson<Options>(json);
            // For non-current versions, maybe attempt conversion?
            default:
                throw new Exception($"Unknown version: {version}");
        }
    }

    public OptionsBase Clone()
    {
        return Deserialize(Serialize());
    }

    public void SaveToFile(string path)
    {
        System.IO.File.WriteAllText(path, Serialize());
    }

    public static OptionsBase LoadFromFile(string path)
    {
        string fileContent = System.IO.File.ReadAllText(path);
        return Deserialize(fileContent);
    }
}

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

    // Timing

    public int touchOffsetMs;
    public int touchLatencyMs;
    public int keyboardMouseOffsetMs;
    public int keyboardMouseLatencyMs;

    // Editor options

    public EditorOptions editorOptions;

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

        touchOffsetMs = 0;
        touchLatencyMs = 0;
        keyboardMouseOffsetMs = 0;
        keyboardMouseLatencyMs = 0;

        editorOptions = new EditorOptions();
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
        Screen.SetResolution(width, height, fullScreenMode, refreshRate);
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
}

[Serializable]
public class EditorOptions
{
    // Appearance

    public bool showKeysounds;

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