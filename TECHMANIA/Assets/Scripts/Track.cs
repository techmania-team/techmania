using System;
using System.Collections.Generic;

// Track is the container of all patterns in a musical track.
// In anticipation of format updates, each format version is
// a derived class of TrackBase.
//
// Because class names are not serialized, we can change class
// names however we want without breaking old files, so the
// current version class will always be called "Track", and
// deprecated versions will be called "TrackV1" or such.

[Serializable]
public class TrackBase
{
    public string version;

    private string Serialize()
    {
#if UNITY_2019
        return UnityEngine.JsonUtility.ToJson(this,
            prettyPrint: true);
#else
        return System.Text.Json.JsonSerializer.Serialize(this,
            typeof(Track),
            new System.Text.Json.JsonSerializerOptions()
            {
                IncludeFields = true,
                WriteIndented = true
            });
#endif
    }

    private static TrackBase Deserialize(string json)
    {
#if UNITY_2019
        string version = UnityEngine.JsonUtility
            .FromJson<TrackBase>(json).version;
        switch (version)
        {
            case TrackV1.kVersion:
                return UnityEngine.JsonUtility
                    .FromJson<TrackV1>(json);
            // For non-current versions, maybe attempt conversion?
            default:
                throw new Exception($"Unknown version: {version}");
        }
#else
        return null;
#endif
    }

    // The clone will retain the same Guid.
    public TrackBase Clone()
    {
        return Deserialize(Serialize());
    }

    public void SaveToFile(string path)
    {
        string serialized = Serialize();
        System.IO.File.WriteAllText(path, serialized);
    }

    public static TrackBase LoadFromFile(string path)
    {
        string fileContent = System.IO.File.ReadAllText(path);
        return Deserialize(fileContent);
    }
}

[Serializable]
public enum ControlScheme
{
    Touch = 0,
    Keys = 1,
    KM = 2
}

[Serializable]
public enum NoteType
{
    Basic,
    ChainHead,
    ChainNode,
    Hold,
    Drag,
    RepeatHead,
    RepeatHeadHold,
    Repeat,
    RepeatHold
}

[Serializable]
public class BpmEvent
{
    public int pulse;
    public double bpm;
    [NonSerialized]
    public float time;
}

[Serializable]
public class IntPoint
{
    public int lane;
    public int pulse;

    public IntPoint(int pulse, int lane)
    {
        this.pulse = pulse;
        this.lane = lane;
    }

    public IntPoint Clone()
    {
        return new IntPoint(pulse, lane);
    }

    public FloatPoint ToFloatPoint()
    {
        return new FloatPoint(pulse, lane);
    }
}

[Serializable]
public class FloatPoint
{
    public float lane;
    public float pulse;

    public FloatPoint(float pulse, float lane)
    {
        this.pulse = pulse;
        this.lane = lane;
    }

    public FloatPoint Clone()
    {
        return new FloatPoint(pulse, lane);
    }

    public static FloatPoint operator +(
        FloatPoint left, FloatPoint right)
    {
        return new FloatPoint(left.pulse + right.pulse,
            left.lane + right.lane);
    }

    public static FloatPoint operator *(float coeff,
        FloatPoint point)
    {
        return new FloatPoint(coeff * point.pulse,
            coeff * point.lane);
    }
}

[Serializable]
public class DragNode
{
    // Relative to DragNote
    public IntPoint anchor;
    // Relative to anchor
    public FloatPoint controlLeft;
    // Relative to anchor
    public FloatPoint controlRight;

    public FloatPoint GetControlPoint(int index)
    {
        if (index == 0)
            return controlLeft;
        else
            return controlRight;
    }

    public void SetControlPoint(int index, FloatPoint p)
    {
        if (index == 0)
            controlLeft = p;
        else
            controlRight = p;
    }

    public DragNode Clone()
    {
        return new DragNode()
        {
            anchor = anchor.Clone(),
            controlLeft = controlLeft.Clone(),
            controlRight = controlRight.Clone()
        };
    }

    public void CopyFrom(DragNode other)
    {
        anchor = other.anchor;
        controlLeft = other.controlLeft;
        controlRight = other.controlRight;
    }
}