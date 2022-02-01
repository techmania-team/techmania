using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
[FormatVersion(Records.kVersion, typeof(Records), isLatest: true)]
public class RecordsBase : SerializableClass<RecordsBase> { }

[Serializable]
public class Record
{
    public string guid;  // For pattern
    public string fingerprint;  // SHA256
    public Options.Ruleset ruleset;
    public int score;
    public PerformanceMedal medal;
    public string gameVersion;

    public override string ToString()
    {
        return $"{score}   {Score.ScoreToRank(score)}";
    }

    public static string EmptyRecordString()
    {
        return "---";
    }

    public static string MedalToString(PerformanceMedal medal)
    {
        switch (medal)
        {
            case PerformanceMedal.NoMedal:
                return "";
            case PerformanceMedal.AllCombo:
                return Locale.GetString(
                    "result_panel_full_combo_medal");
            case PerformanceMedal.PerfectPlay:
                return Locale.GetString(
                    "result_panel_perfect_play_medal");
            case PerformanceMedal.AbsolutePerfect:
                return Locale.GetString(
                    "result_panel_absolute_perfect_medal");
            default:
                return "";
        }
    }

    public Record Clone()
    {
        return new Record()
        {
            guid = this.guid,
            fingerprint = this.fingerprint,
            ruleset = this.ruleset,
            score = this.score,
            medal = this.medal,
            gameVersion = this.gameVersion
        };
    }
}

[Serializable]
public class Records : RecordsBase
{
    public const string kVersion = "1";

    // The format stored on disk.
    public List<Record> records;

    // The format stored in memory.
    [NonSerialized]
    public Dictionary<Options.Ruleset, Dictionary<string, Record>> 
        recordDict;

    public Records()
    {
        version = kVersion;

        records = new List<Record>();
        recordDict = new Dictionary<Options.Ruleset,
            Dictionary<string, Record>>();
        recordDict[Options.Ruleset.Standard] =
            new Dictionary<string, Record>();
        recordDict[Options.Ruleset.Legacy] =
            new Dictionary<string, Record>();
    }

    // Requires fingerprints to have been calculated.
    public Record GetRecord(Pattern p)
    {
        if (Options.instance.ruleset == Options.Ruleset.Custom)
        {
            return null;
        }
        p.CheckFingerprintCalculated();
        Dictionary<string, Record> dict = recordDict[
            Options.instance.ruleset];
        if (!dict.ContainsKey(p.patternMetadata.guid))
        {
            return null;
        }

        Record r = dict[p.patternMetadata.guid];

        bool checkFingerprint = true;
        switch (r.gameVersion)
        {
            case "1.0 beta":
                // There were ruleset changes between 1.0 beta and
                // 1.0.
                return null;
            case "1.0":
            case "1.0.1":
                // Records on these versions were not calculated
                // with MinimizedPattern. They will be fixed by
                // UpdateRecord.
                checkFingerprint = false;
                break;
        }

        if (checkFingerprint && r.fingerprint != p.fingerprint)
        {
            return null;
        }
        return r;
    }

    // If the score is invalid for any reason (modifiers,
    // stage failed, etc.), pass in null. currentRecord can also
    // be null.
    public void UpdateRecord(Pattern p, Score s,
        Record currentRecord, out bool newRecord)
    {
        p.CheckFingerprintCalculated();
        Record updatedRecord = null;
        if (currentRecord != null)
        {
            updatedRecord = currentRecord.Clone();
        }

        // First, fix outdated records if applicable.
        if (currentRecord != null &&
            (currentRecord.gameVersion == "1.0" ||
            currentRecord.gameVersion == "1.0.1"))
        {
            updatedRecord.fingerprint = p.fingerprint;
            updatedRecord.gameVersion = Application.version;
        }

        // Then, update medal if applicable.
        if (currentRecord != null &&
            s != null &&
            s.Medal() > currentRecord.medal)
        {
            updatedRecord.medal = s.Medal();
            updatedRecord.gameVersion = Application.version;
        }

        // Finally, update score if applicable.
        newRecord = false;
        if (s != null)
        {
            int totalScore = s.CurrentScore() +
                s.totalFeverBonus + s.comboBonus;
            if (currentRecord == null ||
                totalScore > currentRecord.score)
            {
                newRecord = true;
                updatedRecord = new Record()
                {
                    guid = p.patternMetadata.guid,
                    fingerprint = p.fingerprint,
                    ruleset = Options.instance.ruleset,
                    score = totalScore,
                    medal = s.Medal(),
                    gameVersion = Application.version
                };
            }    
        }

        if (updatedRecord != null)
        {
            recordDict[updatedRecord.ruleset][updatedRecord.guid]
                = updatedRecord;
        }
    }

    // Requires fingerprints to have been calculated.
    public void SetRecord(Pattern p, Score s)
    {
        p.CheckFingerprintCalculated();
        int totalScore = s.CurrentScore() +
            s.totalFeverBonus + s.comboBonus;
        Record r = new Record()
        {
            guid = p.patternMetadata.guid,
            fingerprint = p.fingerprint,
            ruleset = Options.instance.ruleset,
            score = totalScore,
            medal = s.Medal(),
            gameVersion = Application.version
        };
        recordDict[r.ruleset][r.guid] = r;
    }

    protected override void PrepareToSerialize()
    {
        records.Clear();
        foreach (Dictionary<string, Record> dict in
            recordDict.Values)
        {
            foreach (Record r in dict.Values)
            {
                records.Add(r);
            }
        }
    }

    protected override void InitAfterDeserialize()
    {
        recordDict[Options.Ruleset.Standard].Clear();
        recordDict[Options.Ruleset.Legacy].Clear();
        foreach (Record r in records)
        {
            if (r.ruleset == Options.Ruleset.Custom) continue;
            recordDict[r.ruleset].Add(r.guid, r);
        }
    }

    #region Instance
    public static Records instance { get; private set; }
    public static void RefreshInstance()
    {
        try
        {
            instance = LoadFromFile(
                Paths.GetRecordsFilePath()) as Records;
        }
        catch (IOException)
        {
            instance = new Records();
        }
    }
    #endregion
}
