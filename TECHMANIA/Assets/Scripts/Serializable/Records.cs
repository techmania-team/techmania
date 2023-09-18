using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using MoonSharp.Interpreter;

[Serializable]
[FormatVersion(Records.kVersion, typeof(Records), isLatest: true)]
public class RecordsBase : SerializableClass<RecordsBase>
{
    public void SaveToFile()
    {
        SaveToFile(Paths.GetRecordsFilePath());
    }
}

[Serializable]
[MoonSharpUserData]
public class Record
{
    // All fields are read-only for Lua.
    public string guid  // For pattern
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public string fingerprint // SHA256
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public Options.Ruleset ruleset
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public int score
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public PerformanceMedal medal
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public string gameVersion
    {
        get;
        [MoonSharpHidden]
        set;
    }

    public string Rank()
    {
        return ScoreKeeper.ScoreToRankAssumingStageClear(score);
    }

    public override string ToString()
    {
        return $"{score}   {Rank()}";
    }

    public static string EmptyRecordString()
    {
        return "---";
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
[MoonSharpUserData]
public class Records : RecordsBase
{
    public const string kVersion = "1";

    // The format stored on disk.
    [MoonSharpHidden]
    public List<Record> records;

    // The format stored in memory. We don't serialize this dictionary
    // directly because it's 2 levels deep.
    // string is guid.
    [NonSerialized]
    [MoonSharpHidden]
    public Dictionary<Options.Ruleset, Dictionary<string, Record>> 
        recordDict;

    [MoonSharpHidden]
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

    // Returns null if a record doesn't exist.
    public Record GetRecord(Pattern p, Options.Ruleset ruleset)
    {
        if (ruleset == Options.Ruleset.Custom)
        {
            return null;
        }
        Dictionary<string, Record> dict = recordDict[ruleset];
        if (!dict.ContainsKey(p.patternMetadata.guid))
        {
            return null;
        }

        Record r = dict[p.patternMetadata.guid];
        if (string.IsNullOrEmpty(p.fingerprint))
        {
            p.CalculateFingerprint();
        }
        if (r.fingerprint != p.fingerprint)
        {
            return null;
        }
        return r;
    }

    // Requires fingerprints to have been calculated.
    public Record GetRecord(Pattern p)
    {
        return GetRecord(p, Options.instance.ruleset);
    }

    [MoonSharpHidden]
    public void UpdateRecord(Pattern p, Options.Ruleset ruleset,
        int totalScore, PerformanceMedal medal)
    {
        p.CheckFingerprintCalculated();

        if (ruleset == Options.Ruleset.Custom)
        {
            return;
        }

        string guid = p.patternMetadata.guid;
        Record record = GetRecord(p, ruleset);
        if (record == null)
        {
            // If no record, it may be due to wrong fingerprint.
            // To handle that, we delete the existing record on the
            // guid, if any.
            Dictionary<string, Record> dict = recordDict[ruleset];
            
            if (dict.ContainsKey(guid))
            {
                dict.Remove(guid);
            }

            // Now we create a new record.
            record = new Record()
            {
                guid = guid,
                fingerprint = p.fingerprint,
                ruleset = Options.instance.ruleset,
                score = totalScore,
                medal = medal,
                gameVersion = Application.version
            };
            dict.Add(guid, record);
        }
        else
        {
            // If there is an existing record, we update its score
            // and/or medal if necessary.
            if (totalScore > record.score)
            {
                record.score = totalScore;
            }
            if (medal > record.medal)
            {
                record.medal = medal;
            }
        }
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
    [MoonSharpHidden]
    public static Records instance { get; private set; }

    [MoonSharpHidden]
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
