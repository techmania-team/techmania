using System;
using System.Collections;
using System.Collections.Generic;
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

[Serializable]
public class Options : OptionsBase
{
    public const string kVersion = "1";

    public int width; 
    public int height;
    public int refreshRate;
    public FullScreenMode fullScreenMode;
    public bool vSync;

    public float masterVolume;
    public float musicVolume;
    public float keysoundVolume;

    public void ApplyGraphicSettings()
    {
        Screen.SetResolution(width, height, fullScreenMode, refreshRate);
        QualitySettings.vSyncCount = vSync ? 1 : 0;
    }
}
