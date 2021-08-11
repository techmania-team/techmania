using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class RecordsBase : SerializableClass<RecordsBase> { }

[Serializable]
public class Record
{
    public string guid;  // For pattern
    public string fingerprint;
    public int score;
    public PerformanceMedal medal;
}

[Serializable]
public class Records : RecordsBase
{
    public const string kVersion = "1";

    public List<Record> records;

    [NonSerialized]
    public Dictionary<string, Record> guidToRecord;

    public Records()
    {
        records = new List<Record>();
        guidToRecord = new Dictionary<string, Record>();
    }

    public Record GetRecord(string guid)
    {
        if (guidToRecord.ContainsKey(guid))
        {
            return guidToRecord[guid];
        }
        else
        {
            return null;
        }
    }

    public void SetRecord(Record r)
    {
        guidToRecord[r.guid] = r;
    }

    protected override void PrepareToSerialize()
    {
        records.Clear();
        foreach (Record r in guidToRecord.Values)
        {
            records.Add(r);
        }
    }

    protected override void InitAfterDeserialize()
    {
        guidToRecord.Clear();
        foreach (Record r in records)
        {
            guidToRecord.Add(r.guid, r);
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
