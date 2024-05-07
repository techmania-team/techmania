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
            guid = guid,
            fingerprint = fingerprint,
            ruleset = ruleset,
            score = score,
            medal = medal,
            gameVersion = gameVersion
        };
    }
}

[Serializable]
[MoonSharpUserData]
public class SetlistRecord
{
    // All fields are read-only for Lua.
    public string setlistGuid
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public List<string> patternGuids  // 4 elements
    {
        get;
        [MoonSharpHidden]
        set;
    }
    public List<string> patternFingerprints  // 4 elements
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
        return ScoreKeeper.ScoreToRankAssumingStageClear(score / 4);
    }

    public override string ToString()
    {
        return $"{score}   {Rank()}";
    }

    public static string EmptyRecordString()
    {
        return "---";
    }

    public SetlistRecord Clone()
    {
        return new SetlistRecord()
        {
            setlistGuid = setlistGuid,
            patternGuids = new List<string>(patternGuids),
            patternFingerprints = new List<string>(patternFingerprints),
            ruleset = ruleset,
            score = score,
            medal = medal,
            gameVersion = gameVersion
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
    [MoonSharpHidden]
    public List<SetlistRecord> setlistRecords;

    // The format stored in memory. We don't serialize this dictionary
    // directly because it's 2 levels deep.
    // string is guid.
    [NonSerialized]
    [MoonSharpHidden]
    public Dictionary<Options.Ruleset, Dictionary<string, Record>> 
        recordDict;
    [NonSerialized]
    [MoonSharpHidden]
    public Dictionary<Options.Ruleset, Dictionary<string, SetlistRecord>>
        setlistRecordDict;

    [MoonSharpHidden]
    public Records()
    {
        version = kVersion;

        records = new List<Record>();
        setlistRecords = new List<SetlistRecord>();
        recordDict = new Dictionary<Options.Ruleset,
            Dictionary<string, Record>>();
        recordDict[Options.Ruleset.Standard] =
            new Dictionary<string, Record>();
        recordDict[Options.Ruleset.Legacy] =
            new Dictionary<string, Record>();
        setlistRecordDict = new Dictionary<Options.Ruleset, 
            Dictionary<string, SetlistRecord>>();
        setlistRecordDict[Options.Ruleset.Standard] =
            new Dictionary<string, SetlistRecord>();
        setlistRecordDict[Options.Ruleset.Legacy] =
            new Dictionary<string, SetlistRecord>();

        setlist = new SetlistMethods();
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

    [MoonSharpUserData]
    public class SetlistMethods
    {
        [MoonSharpHidden]
        public Records parent;

        private string GetFingerprintFromReference(
            Setlist.PatternReference r)
        {
            GlobalResource.TrackInFolder trackInFolder;
            Pattern minimizedPattern;
            Status status = GlobalResource.SearchForPatternReference(
                r, out trackInFolder, out minimizedPattern);
            if (!status.Ok()) return null;

            Track fullTrack = null;
            try
            {
                fullTrack = Track.LoadFromFile(
                    Path.Combine(trackInFolder.folder,
                        Paths.kTrackFilename)) as Track;
            }
            catch (Exception) { return null; }
            if (fullTrack == null) return null;

            Pattern fullPattern = null;
            foreach (Pattern p in fullTrack.patterns)
            {
                if (p.patternMetadata.guid ==
                    minimizedPattern.patternMetadata.guid)
                {
                    fullPattern = p;
                    break;
                }
            }
            if (fullPattern == null) return null;

            fullPattern.CalculateFingerprint();
            return fullPattern.fingerprint;
        }

        // Returns null if a record doesn't exist or has any issue.
        public SetlistRecord GetRecord(Setlist s,
            Options.Ruleset ruleset)
        {
            if (ruleset == Options.Ruleset.Custom)
            {
                return null;
            }
            Dictionary<string, SetlistRecord> dict =
                parent.setlistRecordDict[ruleset];
            if (!dict.ContainsKey(s.setlistMetadata.guid))
            {
                return null;
            }
            SetlistRecord r = dict[s.setlistMetadata.guid];
            if (r.patternGuids == null || r.patternGuids.Count != 4 ||
                r.patternFingerprints == null ||
                r.patternFingerprints.Count != 4)
            {
                return null;
            }

            // Load patterns from disk and compare fingerprints
            for (int i = 0; i < 3; i++)
            {
                string guid = r.patternGuids[i];
                string expectedFingerprint = r.patternFingerprints[i];
                bool foundReference = false;
                foreach (Setlist.PatternReference reference in
                    s.selectablePatterns)
                {
                    if (reference.patternGuid == guid)
                    {
                        foundReference = true;
                        string actualFingerprint =
                            GetFingerprintFromReference(reference);
                        if (actualFingerprint != expectedFingerprint)
                        {
                            return null;
                        }
                        break;
                    }
                }
                if (!foundReference) return null;
            }
            string hiddenPatternGuid = r.patternGuids[3];
            string expectedHiddenPatternFingerprint = 
                r.patternFingerprints[3];
            bool foundHiddenPattern = false;
            foreach (Setlist.HiddenPattern hiddenPattern in 
                s.hiddenPatterns)
            {
                if (hiddenPattern.reference.patternGuid ==
                    hiddenPatternGuid)
                {
                    foundHiddenPattern = true;
                    string actualHiddenPatternFingerprint =
                        GetFingerprintFromReference(
                            hiddenPattern.reference);
                    if (actualHiddenPatternFingerprint != 
                        expectedHiddenPatternFingerprint)
                    {
                        return null;
                    }
                    break;
                }
            }
            if (!foundHiddenPattern) return null;

            return r;
        }

        // Requires fingerprints to have been calculated.
        public SetlistRecord GetRecord(Setlist s)
        {
            return GetRecord(s, Options.instance.ruleset);
        }

        [MoonSharpHidden]
        public void UpdateRecord(Setlist s,
            List<int> selectedPatternIndices,
            int hiddenPatternIndex,
            Options.Ruleset ruleset,
            int totalScore, PerformanceMedal medal)
        {
            if (ruleset == Options.Ruleset.Custom)
            {
                return;
            }

            // Calculate the guid and fingerprint of patterns.
            List<string> patternGuids = new List<string>();
            List<string> patternFingerprints = new List<string>();
            foreach (int i in selectedPatternIndices)
            {
                Setlist.PatternReference r = s.selectablePatterns[i];
                patternGuids.Add(r.patternGuid);
                patternFingerprints.Add(GetFingerprintFromReference(r));
            }
            Setlist.PatternReference hiddenPatternReference =
                s.hiddenPatterns[hiddenPatternIndex].reference;
            patternGuids.Add(hiddenPatternReference.patternGuid);
            patternFingerprints.Add(GetFingerprintFromReference(
                hiddenPatternReference));

            string setlistGuid = s.setlistMetadata.guid;
            SetlistRecord record = GetRecord(s, ruleset);
            if (record == null)
            {
                // Create a new record.
                record = new SetlistRecord()
                {
                    setlistGuid = setlistGuid,
                    patternGuids = patternGuids,
                    patternFingerprints = patternFingerprints,
                    ruleset = ruleset,
                    score = totalScore,
                    medal = medal,
                    gameVersion = Application.version
                };
                parent.setlistRecordDict[ruleset].Add(
                    setlistGuid, record);
            }
            else
            {
                // If there is an existing record, we update its score
                // and/or medal if necessary.
                if (totalScore > record.score)
                {
                    record.score = totalScore;
                    // Also set guids and fingerprints
                    record.patternGuids.Clear();
                    record.patternGuids.AddRange(patternGuids);
                    record.patternFingerprints.Clear();
                    record.patternFingerprints.AddRange(
                        patternFingerprints);
                }
                if (medal > record.medal)
                {
                    record.medal = medal;
                }
            }
        }
    }
    public SetlistMethods setlist;

    protected override void PrepareToSerialize()
    {
        records.Clear();
        setlistRecords.Clear();
        foreach (Dictionary<string, Record> dict in
            recordDict.Values)
        {
            foreach (Record r in dict.Values)
            {
                records.Add(r);
            }
        }
        foreach (Dictionary<string, SetlistRecord> dict in
            setlistRecordDict.Values)
        {
            foreach (SetlistRecord r in dict.Values)
            {
                setlistRecords.Add(r);
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
        setlistRecordDict[Options.Ruleset.Standard].Clear();
        setlistRecordDict[Options.Ruleset.Legacy].Clear();
        foreach (SetlistRecord r in setlistRecords)
        {
            if (r.ruleset == Options.Ruleset.Custom) continue;
            setlistRecordDict[r.ruleset].Add(r.setlistGuid, r);
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
