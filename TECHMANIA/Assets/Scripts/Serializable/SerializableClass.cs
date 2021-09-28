using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
#if UNITY_2020
using UnityEngine;
#endif

// Anything that will be serialized to disk (tracks, options, rulesets,
// skins, etc.) should be defined as a subclass to SerializableClass<T>
// and take advantage of its methods, so it can be ready for future
// format updates. Refer to SerializableDemoBase for an example.
//
// Each base class can implement:
// - (optional) PrepareToSerialize
// - (optional) InitAfterDeserialize
//
// Each version class should implement:
// - Parameter-less constructor that at least sets version
// - Upgrade, if not latest version

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class FormatVersionAttribute : Attribute
{
    public string version { get; private set; }
    public Type subclassType { get; private set; }
    public bool isLatest { get; private set; }

    public FormatVersionAttribute(string version,
        Type subclassType, bool isLatest)
    {
        this.version = version;
        this.subclassType = subclassType;
        this.isLatest = isLatest;
    }

}

[Serializable]
public abstract class SerializableClass<T> where T : SerializableClass<T>
{
    public string version;

    public string Serialize(bool optimizeForSaving)
    {
        PrepareToSerialize();
#if UNITY_2020
        if (optimizeForSaving)
        {
            return JsonUtility.ToJson(this, prettyPrint: true)
                .Replace("    ", "\t");
        }
        else
        {
            return JsonUtility.ToJson(this, prettyPrint: false);
        }
#else
        return System.Text.Json.JsonSerializer.Serialize(this,
            GetType(),
            new System.Text.Json.JsonSerializerOptions()
            {
                IncludeFields = true,
                WriteIndented = true
            });
#endif
    }

    public static T Deserialize(string json, out bool upgraded)
    {
#if UNITY_2020
        string version = JsonUtility.FromJson<T>(json).version;
        Type subclassType = null;
        string latestVersion = null;

        // Find which subclass to deserialize as, and what's the
        // latest format version.
        foreach (Attribute a in Attribute.GetCustomAttributes(
            typeof(T)))
        {
            if (!(a is FormatVersionAttribute)) continue;
            FormatVersionAttribute formatVersion = a as
                FormatVersionAttribute;

            if (formatVersion.version == version)
            {
                subclassType = formatVersion.subclassType;
            }
            if (formatVersion.isLatest)
            {
                latestVersion = formatVersion.version;
            }
        }
        if (subclassType == null)
        {
            throw new Exception($"Unknown version: {version}");
        }
        if (latestVersion == null)
        {
            throw new Exception($"Latest version not defined.");
        }

        // Deserialize, upgrade if necessary, initialize if necessary.
        T t = JsonUtility.FromJson(json, subclassType) as T;
        upgraded = false;
        while (t.version != latestVersion)
        {
            t = t.Upgrade();
            upgraded = true;
        }
        t.InitAfterDeserialize();
        return t;
#else
        upgraded = false;
        return null;
#endif
    }

    public static T Deserialize(string json)
    {
        return Deserialize(json, out _);
    }

    public T Clone()
    {
        return Deserialize(Serialize(optimizeForSaving: false));
    }

    public void SaveToFile(string path)
    {
        System.IO.File.WriteAllText(path, Serialize(
            optimizeForSaving: true));
    }

    public static T LoadFromFile(string path, out bool upgraded)
    {
#if UNITY_2020
        string fileContent = UniversalIO.ReadAllText(path);
        return Deserialize(fileContent, out upgraded);
#else
        upgraded = false;
        return null;
#endif
    }

    public static T LoadFromFile(string path)
    {
        return LoadFromFile(path, out _);
    }

    protected virtual T Upgrade()
    {
        throw new NotImplementedException();
    }
    protected virtual void PrepareToSerialize() { }
    protected virtual void InitAfterDeserialize() { }

    #region Culture
    protected static CultureInfo cultureInfoBackup;
    protected void SwitchToInvariantCulture()
    {
        cultureInfoBackup = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
    }

    protected void RestoreToSystemCulture()
    {
        CultureInfo.CurrentCulture = cultureInfoBackup;
    }
    #endregion
}

[Serializable]
[FormatVersion(SerializableDemoV1.kVersion,
    typeof(SerializableDemoV1), isLatest: false)]
[FormatVersion(SerializableDemoV2.kVersion,
    typeof(SerializableDemoV2), isLatest: true)]
public class SerializableDemoBase :
    SerializableClass<SerializableDemoBase>
{ }

[Serializable]
public class SerializableDemoV1 : SerializableDemoBase
{
    public const string kVersion = "1";
    public string v1field;

    public SerializableDemoV1()
    {
#if UNITY_2020
        Debug.Log("V1 constructor called");
#endif
        version = kVersion;
    }

    protected override SerializableDemoBase Upgrade()
    {
        return new SerializableDemoV2()
        {
            v2field = v1field
        };
    }
}

[Serializable]
public class SerializableDemoV2 : SerializableDemoBase
{
    public const string kVersion = "2";
    public string v2field;

    public SerializableDemoV2()
    {
#if UNITY_2020
        Debug.Log("V2 constructor called");
#endif
        version = kVersion;
    }
}