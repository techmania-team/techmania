using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Anything that will be serialized to disk (tracks, options, rulesets,
// skins, etc.) should be defined as a subclass to Serializable<T> and
// take advantage of its methods, so it can be ready for future format
// updates. Refer to SerializableDemoBase for an example.

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
public abstract class Serializable<T> where T : Serializable<T>
{
    public string version;

    public string Serialize()
    {
        return JsonUtility.ToJson(this, prettyPrint: true);
    }

    public static T Deserialize(string json)
    {
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

        // Deserialize, and upgrade if necessary.
        T t = JsonUtility.FromJson(json, subclassType) as T;
        while (t.version != latestVersion)
        {
            t = t.Upgrade();
        }
        return t;
    }

    public T Clone()
    {
        return Deserialize(Serialize());
    }

    public void SaveToFile(string path)
    {
        System.IO.File.WriteAllText(path, Serialize());
    }

    public static T LoadFromFile(string path)
    {
        string fileContent = System.IO.File.ReadAllText(path);
        return Deserialize(fileContent);
    }

    protected abstract T Upgrade();
}

[Serializable]
[FormatVersion(SerializableDemoV1.kVersion,
    typeof(SerializableDemoV1), isLatest: false)]
[FormatVersion(SerializableDemoV2.kVersion,
    typeof(SerializableDemoV2), isLatest: true)]
public class SerializableDemoBase :
    Serializable<SerializableDemoBase>
{
    protected override SerializableDemoBase Upgrade()
    {
        throw new NotImplementedException();
    }
}

[Serializable]
public class SerializableDemoV1 : SerializableDemoBase
{
    public const string kVersion = "1";
    public string v1field;

    public SerializableDemoV1(string field)
    {
        version = kVersion;
        v1field = field;
    }

    protected override SerializableDemoBase Upgrade()
    {
        return new SerializableDemoV2(v1field);
    }
}

[Serializable]
public class SerializableDemoV2 : SerializableDemoBase
{
    public const string kVersion = "2";
    public string v2field;

    public SerializableDemoV2(string field)
    {
        version = kVersion;
        v2field = field;
    }

    protected override SerializableDemoBase Upgrade()
    {
        throw new NotImplementedException();
    }
}