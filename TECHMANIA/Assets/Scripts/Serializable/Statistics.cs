using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
[FormatVersion(Statistics.kVersion, typeof(Statistics), isLatest: true)]
public class StatisticsBase : SerializableClass<StatisticsBase>
{
    public void SaveToFile()
    {
        SaveToFile(Paths.GetStatisticsFilePath());
    }
}

// All fields are read-only to Lua.
[Serializable]
[MoonSharpUserData]
public class Statistics : StatisticsBase
{
    public const string kVersion = "1";

    public TimeSpan totalPlayTime
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public TimeSpan timeInGame
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public TimeSpan timeInEditor
    {
        get;
        [MoonSharpHidden]
        set;
    }

    public long timesAppLaunched
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public long totalPatternsPlayed
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public long totalNotesHit
    {
        get;
        [MoonSharpHidden]
        set;
    }

    public Statistics()
    {
        version = kVersion;
    }

    #region Instance
    public static Statistics instance { get; private set; }

    public static void RefreshInstance()
    {
        try
        {
            instance = LoadFromFile(
                Paths.GetStatisticsFilePath()) as Statistics;
        }
        catch (Exception ex)
        {
            Debug.LogError("An error occurred when loading statistics. All stats will be reset. See next error for details.");
            Debug.LogException(ex);
            instance = new Statistics();
        }
    }
    #endregion
}
