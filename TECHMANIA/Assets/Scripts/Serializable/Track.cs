using System;
using System.Collections.Generic;

// Track is the container of all patterns in a musical track.
// In anticipation of format updates, each format version is
// a derived class of TrackBase.
//
// Because class names are not serialized, we can change class
// names however we want without breaking old files, so the
// current version class will always be called "Track", and
// deprecated versions will be renamed to "TrackV1" or such.

// The current version ("2") introduces "packing", which basically
// compresses notes to concise strings before serialization,
// so we don't output field names and hundreds of spaces for
// each note. This should bring the serialized tracks to a
// reasonable size.
//
// Notes contain optional parameters. A note with non-default values
// on any such parameter is considered an "extended" note, and
// is packed differently from normal notes.

[Serializable]
[FormatVersion(TrackV1.kVersion, typeof(TrackV1), isLatest: false)]
[FormatVersion(Track.kVersion, typeof(Track), isLatest: true)]
public class TrackBase : SerializableClass<TrackBase> {}

#region Enums
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
public enum CurveType
{
    Bezier = 0,
    BSpline = 1
}
#endregion

public class TimeEvent
{
    public int pulse;
#if UNITY_2020
    [NonSerialized]
#else
    [System.Text.Json.Serialization.JsonIgnore]
#endif
    public float time;
}

[Serializable]
public class BpmEvent : TimeEvent
{
    public double bpm;

    public BpmEvent Clone()
    {
        return new BpmEvent()
        {
            pulse = pulse,
            bpm = bpm
        };
    }
}

[Serializable]
public class TimeStop : TimeEvent
{
    public int duration;  // In beats

#if UNITY_2020
    [NonSerialized]
#else
    [System.Text.Json.Serialization.JsonIgnore]
#endif
    public float endTime;

#if UNITY_2020
    [NonSerialized]
#else
    [System.Text.Json.Serialization.JsonIgnore]
#endif
    // The BPM at the time of this event, purely meant for
    // simplifying time calculation.
    public double bpmAtStart;

    public TimeStop Clone()
    {
        return new TimeStop()
        {
            pulse = pulse,
            duration = duration
        };
    }
}

[Serializable]
public class Track : TrackBase
{
    public const string kVersion = "2";

    public Track(string title, string artist)
    {
        version = kVersion;
        trackMetadata = new TrackMetadata();
        trackMetadata.guid = Guid.NewGuid().ToString();
        trackMetadata.title = title;
        trackMetadata.artist = artist;
        patterns = new List<Pattern>();
    }

    public TrackMetadata trackMetadata;
    public List<Pattern> patterns;

    public void SortPatterns()
    {
        patterns.Sort((Pattern p1, Pattern p2) =>
        {
            if (p1.patternMetadata.controlScheme !=
                p2.patternMetadata.controlScheme)
            {
                return (int)p1.patternMetadata.controlScheme -
                    (int)p2.patternMetadata.controlScheme;
            }
            else
            {
                return p1.patternMetadata.level -
                    p2.patternMetadata.level;
            }
        });
    }

    // Returns -1 if not found.
    public int FindPatternIndexByGuid(string guid)
    {
        for (int i = 0; i < patterns.Count; i++)
        {
            if (patterns[i].patternMetadata.guid == guid) return i;
        }
        return -1;
    }

    public Pattern FindPatternByGuid(string guid)
    {
        int index = FindPatternIndexByGuid(guid);
        if (index < 0) return null;
        return patterns[index];
    }

    protected override void PrepareToSerialize()
    {
        patterns.ForEach(p => p.PackAllNotes());
    }

    protected override void InitAfterDeserialize()
    {
        patterns.ForEach(p => p.UnpackAllNotes());
    }
}

[Serializable]
public class TrackMetadata
{
    public string guid;

    // Text stuff.

    public string title;
    public string artist;
    public string genre;
    public string additionalCredits;  // Multiple lines allowed

    // In track select screen.

    // Filename of eyecatch image.
    public string eyecatchImage;
    // Filename of preview music.
    public string previewTrack;
    // In seconds.
    public double previewStartTime;
    public double previewEndTime;
}

[Serializable]
public partial class Pattern
{
    public PatternMetadata patternMetadata;
    public List<BpmEvent> bpmEvents;
    public List<TimeStop> timeStops;

#if UNITY_2020
    [NonSerialized]
#else
    [System.Text.Json.Serialization.JsonIgnore]
#endif
    public SortedSet<Note> notes;

#if UNITY_2020
    [NonSerialized]
#else
    [System.Text.Json.Serialization.JsonIgnore]
#endif
    public List<TimeEvent> timeEvents;  // bpmEvents + timeStops

    // Only used in serialization and deserialization.
    public List<string> packedNotes;
    public List<string> packedHoldNotes;
    public List<PackedDragNote> packedDragNotes;

    public const int pulsesPerBeat = 240;
    public const int minLevel = 1;
    public const int defaultLevel = 1;
    public const double minBpm = 1;
    public const double defaultBpm = 60;
    public const int minBps = 1;
    public const int defaultBps = 4;

    public Pattern()
    {
        patternMetadata = new PatternMetadata();
        bpmEvents = new List<BpmEvent>();
        timeStops = new List<TimeStop>();
        notes = new SortedSet<Note>(new NoteComparer());
    }

    public Pattern CloneWithDifferentGuid()
    {
#if UNITY_2020
        PackAllNotes();
        string json = UnityEngine.JsonUtility.ToJson(
            this, prettyPrint: false);
        Pattern clone = UnityEngine.JsonUtility
            .FromJson<Pattern>(json);
        clone.patternMetadata.guid = Guid.NewGuid().ToString();
        clone.UnpackAllNotes();
        return clone;
#else
        return null;
#endif
    }

    public void PackAllNotes()
    {
        packedNotes = new List<string>();
        packedHoldNotes = new List<string>();
        packedDragNotes = new List<PackedDragNote>();
        foreach (Note n in notes)
        {
            if (n is HoldNote)
            {
                packedHoldNotes.Add(n.Pack());
            }
            else if (n is DragNote)
            {
                packedDragNotes.Add((n as DragNote).Pack());
            }
            else
            {
                packedNotes.Add(n.Pack());
            }
        }
    }

    public void UnpackAllNotes()
    {
        notes = new SortedSet<Note>(new NoteComparer());
        foreach (string s in packedNotes)
        {
            notes.Add(Note.Unpack(s));
        }
        foreach (string s in packedHoldNotes)
        {
            notes.Add(HoldNote.Unpack(s));
        }
        foreach (PackedDragNote n in packedDragNotes)
        {
            notes.Add(DragNote.Unpack(n));
        }
    }
}

[Serializable]
public class PatternMetadata
{
    public string guid;

    // Basics.

    public string patternName;
    public int level;
    public ControlScheme controlScheme;
    public int lanes;  // Currently unused
    public string author;

    // Background AV.

    // The backing track played in game.
    // This always plays from the beginning.
    // If no keysounds, this should be the entire track.
    public string backingTrack;
    // Filename of background image, used in loading screen.
    public string backImage;
    // Filename of background animation (BGA).
    // If empty, will show background image.
    public string bga;
    // Play BGA this many seconds after the backing track begins.
    public double bgaOffset;
    // Take BGA into account when calculating pattern length.
    public bool waitForEndOfBga;
    // If true, game will not wait for BGA regardless of
    // waitForEndOfBga's value.
    public bool playBgaOnLoop;

    // Timing.

    // Beat 0 starts at this time.
    public double firstBeatOffset;
    // These can be changed by events.
    public double initBpm;
    // BPS: beats per scan.
    public int bps;

    public PatternMetadata()
    {
        guid = Guid.NewGuid().ToString();
#if UNITY_2020
        patternName = Locale.GetString(
            "track_setup_patterns_tab_new_pattern_name");
#else
        patternName = "New pattern";
#endif
        level = Pattern.defaultLevel;

        waitForEndOfBga = true;
        playBgaOnLoop = false;

        initBpm = Pattern.defaultBpm;
        bps = Pattern.defaultBps;
    }
}

public class Note
{
    // Calculated at unpack time:

    public NoteType type;
    public int pulse;
    public int lane;
    public string sound;  // Filename with extension, no folder

    // Calculated at runtime:

    public float time;

    // Optional parameters:

    public float volume;
    public float pan;
    public bool endOfScan;
    protected string endOfScanString
    {
        get { return endOfScan ? "1" : "0"; }
        set { endOfScan = value == "1"; }
    }
    public const float minVolume = 0f;
    public const float defaultVolume = 1f;
    public const float maxVolume = 1f;
    public const float minPan = -1f;
    public const float defaultPan = 0f;
    public const float maxPan = 1f;

    public Note()
    {
        // These will apply to HoldNote and DragNote.
        volume = defaultVolume;
        pan = defaultPan;
        endOfScan = false;
    }

    public virtual bool IsExtended()
    {
        if (volume != defaultVolume) return true;
        if (pan != defaultPan) return true;
        if (endOfScan) return true;
        return false;
    }

    public virtual string Pack()
    {
        if (IsExtended())
        {
            // Enums will be formatted as strings.
            return $"E|{type}|{pulse}|{lane}|{volume}|{pan}|{endOfScanString}|{sound}";
        }
        else
        {
            return $"{type}|{pulse}|{lane}|{sound}";
        }
    }

    public static Note Unpack(string packed)
    {
        char[] delim = new char[] { '|' };
        // Beware that the "sound" portion may contain |.
        string[] splits = packed.Split(delim, 2);
        // Extended?
        if (splits[0] == "E")
        {
            splits = packed.Split(delim, 8);
            return new Note()
            {
                type = (NoteType)Enum.Parse(
                    typeof(NoteType), splits[1]),
                pulse = int.Parse(splits[2]),
                lane = int.Parse(splits[3]),
                volume = float.Parse(splits[4]),
                pan = float.Parse(splits[5]),
                endOfScanString = splits[6],
                sound = splits[7]
            };
        }
        else
        {
            splits = packed.Split(delim, 4);
            return new Note()
            {
                type = (NoteType)Enum.Parse(
                    typeof(NoteType), splits[0]),
                pulse = int.Parse(splits[1]),
                lane = int.Parse(splits[2]),
                sound = splits[3]
            };
        }
    }

    public Note Clone()
    {
        // If performance is necessary, then do it type-by-type and
        // field-by-field, as in NoteV1.Clone.
        if (this is HoldNote)
        {
            return HoldNote.Unpack(Pack());
        }
        else if (this is DragNote)
        {
            return DragNote.Unpack((this as DragNote).Pack());
        }
        else
        {
            return Note.Unpack(Pack());
        }
    }

    // This does not modify type, pulse and lane.
    public void CopyFrom(Note other)
    {
        sound = other.sound;
        volume = other.volume;
        pan = other.pan;
        endOfScan = other.endOfScan;
        if (this is HoldNote && other is HoldNote)
        {
            (this as HoldNote).duration = (other as HoldNote).duration;
        }
        if (this is DragNote && other is DragNote)
        {
            DragNote d = this as DragNote;
            d.nodes = new List<DragNode>();
            foreach (DragNode node in (other as DragNote).nodes)
            {
                d.nodes.Add(node.Clone());
            }
            d.curveType = (other as DragNote).curveType;
        }
    }

    public int GetScanNumber(int bps)
    {
        int pulsesPerScan = Pattern.pulsesPerBeat * bps;
        int scan = pulse / pulsesPerScan;
        if (pulse % pulsesPerScan == 0 &&
            endOfScan && scan > 0 &&
            type != NoteType.Drag)
        {
            scan--;
        }
        return scan;
    }
}

// There has been a bug with HoldNote since 0.1 but only found
// in 0.3: the order of pulse and lane are swapped when packing.
public class HoldNote : Note
{
    // Calculated at unpack time:

    public int duration;  // In pulses.

    // Calculated at runtime:

    public float gracePeriodStart;
    public float endTime;

    public override string Pack()
    {
        if (IsExtended())
        {
            // Enums will be formatted as strings.
            return $"E|{type}|{lane}|{pulse}|{duration}|{volume}|{pan}|{endOfScanString}|{sound}";
        }
        else
        {
            return $"{type}|{lane}|{pulse}|{duration}|{sound}";
        }
    }

    public static new HoldNote Unpack(string packed)
    {
        char[] delim = new char[] { '|' };
        // Beware that the "sound" portion may contain |.
        string[] splits = packed.Split(delim, 2);
        // Extended?
        if (splits[0] == "E")
        {
            splits = packed.Split(delim, 9);
            return new HoldNote()
            {
                type = (NoteType)Enum.Parse(
                    typeof(NoteType), splits[1]),
                lane = int.Parse(splits[2]),
                pulse = int.Parse(splits[3]),
                duration = int.Parse(splits[4]),
                volume = float.Parse(splits[5]),
                pan = float.Parse(splits[6]),
                endOfScanString = splits[7],
                sound = splits[8]
            };
        }
        else
        {
            splits = packed.Split(delim, 5);
            return new HoldNote()
            {
                type = (NoteType)Enum.Parse(
                    typeof(NoteType), splits[0]),
                lane = int.Parse(splits[1]),
                pulse = int.Parse(splits[2]),
                duration = int.Parse(splits[3]),
                sound = splits[4]
            };
        }
    }
}

public class DragNote : Note
{
    public CurveType curveType;

    // There must be at least 2 nodes, with nodes[0]
    // describing the note head.
    // controlBefore of the first node and controlAfter
    // of the last node are ignored.
    public List<DragNode> nodes;

    // Calculated at runtime:

    public float gracePeriodStart;
    public float endTime;

    public DragNote()
    {
        curveType = CurveType.Bezier;
        nodes = new List<DragNode>();
    }

    public int Duration()
    {
        if (curveType == CurveType.Bezier ||
            nodes.Count == 2)
        {
            return (int)nodes[nodes.Count - 1].anchor.pulse;
        }

        // B-spline removes the last segment so we need a bit of
        // interpolation here.
        float p1 = nodes[nodes.Count - 2].anchor.pulse;
        float p2 = nodes[nodes.Count - 1].anchor.pulse;
        return (int)((p1 + p2 * 5f) / 6f);
    }

    #region Interpolation
    // Returns a list of points on the curve defined by
    // this note. All points are relative to the note head.
    public List<FloatPoint> Interpolate()
    {
        List<FloatPoint> result = new List<FloatPoint>();
        switch (curveType)
        {
            case CurveType.Bezier:
                InterpolateAsBezierCurve(result);
                break;
            case CurveType.BSpline:
                InterpolateAsBSpline(result);
                break;
        }
        return result;
    }

    private void InterpolateAsLine(List<FloatPoint> result)
    {
        const int numSteps = 30;
        for (int step = 0; step <= numSteps; step++)
        {
            float t = (float)step / numSteps;
            result.Add((1f - t) * nodes[0].anchor +
                t * nodes[1].anchor);
        }
    }

    private void InterpolateAsBezierCurve(List<FloatPoint> result)
    {
        if (nodes.Count == 2)
        {
            InterpolateAsLine(result);
            return;
        }

        result.Add(nodes[0].anchor);
        const int numSteps = 50;
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            FloatPoint p0 = nodes[i].anchor;
            FloatPoint p1 = p0 + nodes[i].controlRight;
            FloatPoint p3 = nodes[i + 1].anchor;
            FloatPoint p2 = p3 + nodes[i + 1].controlLeft;
            for (int step = 1; step <= numSteps; step++)
            {
                float t = (float)step / numSteps;

                float coeff0 = (1f - t) * (1f - t) * (1f - t);
                float coeff1 = 3f * (1f - t) * (1f - t) * t;
                float coeff2 = 3f * (1f - t) * t * t;
                float coeff3 = t * t * t;

                result.Add(coeff0 * p0 +
                    coeff1 * p1 +
                    coeff2 * p2 +
                    coeff3 * p3);
            }
        }
    }

    private void InterpolateAsBSpline(List<FloatPoint> result)
    {
        result.Add(nodes[0].anchor);
        const int numSteps = 10;
        Func<int, int> clampIndex = (int index) =>
        {
            if (index <= 0) return 0;
            if (index >= nodes.Count - 1) return nodes.Count - 1;
            return index;
        };
        for (int i = -2; i < nodes.Count - 2; i++)
        {
            int index0 = clampIndex(i);
            int index1 = clampIndex(i + 1);
            int index2 = clampIndex(i + 2);
            int index3 = clampIndex(i + 3);
            FloatPoint p0 = nodes[index0].anchor;
            FloatPoint p1 = nodes[index1].anchor;
            FloatPoint p2 = nodes[index2].anchor;
            FloatPoint p3 = nodes[index3].anchor;
            for (int step = 1; step <= numSteps; step++)
            {
                float t = (float)step / numSteps;
                float tSquared = t * t;
                float tCubed = tSquared * t;

                float coeff0 = -tCubed + 3f * tSquared - 3f * t + 1f;
                float coeff1 = 3f * tCubed - 6f * tSquared + 4f;
                float coeff2 = -3f * tCubed + 3f * tSquared + 3f * t + 1f;
                float coeff3 = tCubed;

                result.Add((coeff0 * p0 +
                    coeff1 * p1 +
                    coeff2 * p2 +
                    coeff3 * p3) / 6f);
            }
        }
    }
    #endregion

    public override bool IsExtended()
    {
        if (volume != defaultVolume) return true;
        if (pan != defaultPan) return true;
        if (curveType != CurveType.Bezier) return true;
        return false;
    }

    public new PackedDragNote Pack()
    {
        PackedDragNote packed = new PackedDragNote();
        if (IsExtended())
        {
            // Enums will be formatted as strings.
            packed.packedNote = $"E|{type}|{pulse}|{lane}|{volume}|{pan}|{(int)curveType}|{sound}";
        }
        else
        {
            packed.packedNote = $"{type}|{pulse}|{lane}|{sound}";
        }
        foreach (DragNode node in nodes)
        {
            packed.packedNodes.Add(node.Pack());
        }
        return packed;
    }

    public static DragNote Unpack(PackedDragNote packed)
    {
        char[] delim = new char[] { '|' };
        // Beware that the "sound" portion may contain |.
        string[] splits = packed.packedNote.Split(delim, 2);
        DragNote dragNote;
        // Extended?
        if (splits[0] == "E")
        {
            splits = packed.packedNote.Split(delim, 8);
            dragNote = new DragNote()
            {
                pulse = int.Parse(splits[2]),
                lane = int.Parse(splits[3]),
                volume = float.Parse(splits[4]),
                pan = float.Parse(splits[5]),
                curveType = (CurveType)int.Parse(splits[6]),
                sound = splits[7]
            };
        }
        else
        {
            splits = packed.packedNote.Split(delim, 4);
            dragNote = new DragNote()
            {
                pulse = int.Parse(splits[1]),
                lane = int.Parse(splits[2]),
                sound = splits[3]
            };
        }

        dragNote.type = NoteType.Drag;
        dragNote.endOfScan = false;
        dragNote.nodes = new List<DragNode>();
        foreach (string packedNode in packed.packedNodes)
        {
            dragNote.nodes.Add(DragNode.Unpack(packedNode));
        }
        return dragNote;
    }
}

public class NoteComparer : IComparer<Note>
{
    public int Compare(Note x, Note y)
    {
        if (x.pulse < y.pulse) return -1;
        if (x.pulse > y.pulse) return 1;
        if (x.lane < y.lane) return -1;
        if (x.lane > y.lane) return 1;
        return 0;
    }
}

#region Drag note dependencies
// Version 2 does not serialize the following classes, but
// serialization is required for the loading of version 1 tracks
// to complete.

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

    public static FloatPoint operator /(FloatPoint point, float coeff)
    {
        return new FloatPoint(point.pulse / coeff,
            point.lane / coeff);
    }
}

[Serializable]
public class DragNode
{
    // Relative to DragNote
    public FloatPoint anchor;
    // Relative to anchor
    public FloatPoint controlLeft;
    // Relative to anchor
    public FloatPoint controlRight;

    public DragNode()
    {
        anchor = new FloatPoint(0f, 0f);
        controlLeft = new FloatPoint(0f, 0f);
        controlRight = new FloatPoint(0f, 0f);
    }

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

    public string Pack()
    {
        return $"{anchor.pulse}|{anchor.lane}|{controlLeft.pulse}|{controlLeft.lane}|{controlRight.pulse}|{controlRight.lane}";
    }

    public static DragNode Unpack(string packed)
    {
        string[] splits = packed.Split('|');
        return new DragNode()
        {
            anchor = new FloatPoint(
                float.Parse(splits[0]),
                float.Parse(splits[1])),
            controlLeft = new FloatPoint(
                float.Parse(splits[2]),
                float.Parse(splits[3])),
            controlRight = new FloatPoint(
                float.Parse(splits[4]),
                float.Parse(splits[5]))
        };
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

[Serializable]
public class PackedDragNote
{
    public string packedNote;
    public List<string> packedNodes;

    public PackedDragNote()
    {
        packedNodes = new List<string>();
    }
}
#endregion