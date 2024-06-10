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

[Serializable]
[MoonSharpUserData]
public class Statistics : StatisticsBase
{
    public const string kVersion = "1";

    public TimeSpan totalPlayTime;
    public TimeSpan timeInGame;
    public TimeSpan timeInEditor;

    public int timesAppLaunched;
    public int totalPatternsPlayed;
    public int totalNotesHit;

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
