using System;
using System.Collections.Generic;

// Track is the container of all patterns in a musical track. In anticipation of
// format updates, each format version is a derived class of TrackBase.
//
// Because class names are not serialized, we can change class names
// however we want without breaking old files, so the current version
// class will always be called "Track", and deprecated versions will be
// called "TrackVersion1" or such.

[Serializable]
public class TrackBase
{
    public string version;

    private string Serialize()
    {
        return UnityEngine.JsonUtility.ToJson(this, prettyPrint: true);
    }
    private static TrackBase Deserialize(string json)
    {
        string version = UnityEngine.JsonUtility.FromJson<TrackBase>(json).version;
        switch (version)
        {
            case Track.kVersion:
                return UnityEngine.JsonUtility.FromJson<Track>(json);
                // For non-current versions, maybe attempt conversion?
            default:
                throw new Exception($"Unknown version: {version}");
        }
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

// Heavily inspired by bmson:
// https://bmson-spec.readthedocs.io/en/master/doc/index.html#format-overview
[Serializable]
public class Track : TrackBase
{
    public const string kVersion = "1";
    public Track() { version = kVersion; }
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
            if (p1.patternMetadata.controlScheme != p2.patternMetadata.controlScheme)
            {
                return (int)p1.patternMetadata.controlScheme -
                    (int)p2.patternMetadata.controlScheme;
            }
            else
            {
                return p1.patternMetadata.level - p2.patternMetadata.level;
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
}

[Serializable]
public class TrackMetadata
{
    public string guid;

    // Text stuff.

    public string title;
    public string artist;
    public string genre;

    // In track select screen.

    // Filename of eyecatch image.
    public string eyecatchImage;
    // Filename of preview music.
    public string previewTrack;
    // In seconds.
    public double previewStartTime;
    public double previewEndTime;

    // In gameplay.

    // Filename of background image, used in loading screen
    public string backImage;
    // Filename of background animation (BGA)
    // If empty, will show background image
    public string bga;
    // Play BGA from this time.
    public double bgaOffset;
}

[Serializable]
public class Pattern
{
    public PatternMetadata patternMetadata;
    public List<BpmEvent> bpmEvents;
    public List<SoundChannel> soundChannels;

    public const int pulsesPerBeat = 240;
    public const int minLevel = 1;
    public const int maxLevel = 12;
    public const double minBpm = 1.0;
    public const double maxBpm = 1000.0;
    public const int minBps = 1;
    public const int maxBps = 128;

    public Pattern()
    {
        patternMetadata = new PatternMetadata();
        bpmEvents = new List<BpmEvent>();
        soundChannels = new List<SoundChannel>();
    }

    public Pattern CloneWithDifferentGuid()
    {
        string json = UnityEngine.JsonUtility.ToJson(this, prettyPrint: false);
        Pattern clone = UnityEngine.JsonUtility.FromJson<Pattern>(json);
        clone.patternMetadata.guid = Guid.NewGuid().ToString();
        return clone;
    }

    public void CreateListsIfNull()
    {
        if (bpmEvents == null)
        {
            bpmEvents = new List<BpmEvent>();
        }
        if (soundChannels == null)
        {
            soundChannels = new List<SoundChannel>();
        }
    }

    // Assumes no note exists at the same location.
    public void AddNote(Note n, string sound)
    {
        if (soundChannels == null)
        {
            soundChannels = new List<SoundChannel>();
        }
        SoundChannel channel = soundChannels.Find(
            (SoundChannel c) => { return c.name == sound; });
        if (channel == null)
        {
            channel = new SoundChannel();
            channel.name = sound;
            soundChannels.Add(channel);
        }
        if (n is HoldNote)
        {
            channel.holdNotes.Add(n as HoldNote);
        }
        else if (n is DragNote)
        {
            channel.dragNotes.Add(n as DragNote);
        }
        else
        {
            channel.notes.Add(n);
        }
    }

    public void ModifyNoteKeysound(Note n, string oldSound, string newSound)
    {
        SoundChannel oldChannel = soundChannels.Find(
            (SoundChannel c) => { return c.name == oldSound; });
        if (oldChannel == null)
        {
            throw new Exception(
                $"Sound channel {oldSound} not found in pattern when modifying keysound.");
        }
        SoundChannel newChannel = soundChannels.Find(
            (SoundChannel c) => { return c.name == newSound; });
        if (newChannel == null)
        {
            newChannel = new SoundChannel();
            newChannel.name = newSound;
            soundChannels.Add(newChannel);
        }

        if (n is HoldNote)
        {
            oldChannel.holdNotes.Remove(n as HoldNote);
            newChannel.holdNotes.Add(n as HoldNote);
        }
        else if (n is DragNote)
        {
            oldChannel.dragNotes.Remove(n as DragNote);
            newChannel.dragNotes.Add(n as DragNote);
        }
        else
        {
            oldChannel.notes.Remove(n);
            newChannel.notes.Add(n);
        }
        if (oldChannel.notes.Count +
            oldChannel.holdNotes.Count +
            oldChannel.dragNotes.Count == 0)
        {
            soundChannels.Remove(oldChannel);
        }
    }

    public void DeleteNote(Note n, string sound)
    {
        SoundChannel channel = soundChannels.Find(
            (SoundChannel c) => { return c.name == sound; });
        if (channel == null)
        {
            throw new Exception(
                $"Sound channel {sound} not found in pattern when deleting.");
        }

        if (n is HoldNote)
        {
            channel.holdNotes.Remove(n as HoldNote);
        }
        else if (n is DragNote)
        {
            channel.dragNotes.Remove(n as DragNote);
        }
        else
        {
            channel.notes.Remove(n);
        }

        if (channel.notes.Count +
            channel.holdNotes.Count +
            channel.dragNotes.Count == 0)
        {
            soundChannels.Remove(channel);
        }
    }

    // Sort BPM events by pulse, then fill their time fields.
    // Enables CalculateTimeOfAllNotes, TimeToPulse and PulseToTime.
    public void PrepareForTimeCalculation()
    {
        bpmEvents.Sort((BpmEvent e1, BpmEvent e2) =>
        {
            return e1.pulse - e2.pulse;
        });

        float currentBpm = (float)patternMetadata.initBpm;
        float currentTime = (float)patternMetadata.firstBeatOffset;
        int currentPulse = 0;
        // beat / minute = currentBpm
        // pulse / beat = pulsesPerBeat
        // ==>
        // pulse / minute = pulsesPerBeat * currentBpm
        // ==>
        // minute / pulse = 1f / (pulsesPerBeat * currentBpm)
        // ==>
        // second / pulse = 60f / (pulsesPerBeat * currentBpm)
        float secondsPerPulse = 60f / (pulsesPerBeat * currentBpm);

        foreach (BpmEvent e in bpmEvents)
        {
            e.time = currentTime + secondsPerPulse * (e.pulse - currentPulse);

            currentBpm = (float)e.bpm;
            currentTime = e.time;
            currentPulse = e.pulse;
            secondsPerPulse = 60f / (pulsesPerBeat * currentBpm);
        }
    }

    public void CalculateTimeOfAllNotes()
    {
        foreach (SoundChannel c in soundChannels)
        {
            foreach (Note n in c.notes)
            {
                n.time = PulseToTime(n.pulse);
            }
            foreach (Note n in c.holdNotes)
            {
                n.time = PulseToTime(n.pulse);
            }
            foreach (Note n in c.dragNotes)
            {
                n.time = PulseToTime(n.pulse);
            }
        }
    }

    // Works for negative times too.
    public float TimeToPulse(float time)
    {
        float referenceBpm = (float)patternMetadata.initBpm;
        float referenceTime = (float)patternMetadata.firstBeatOffset;
        int referencePulse = 0;

        // Find the immediate BpmEvent before specified pulse.
        for (int i = bpmEvents.Count - 1; i >= 0; i--)
        {
            BpmEvent e = bpmEvents[i];
            if (e.time <= time)
            {
                referenceBpm = (float)e.bpm;
                referenceTime = e.time;
                referencePulse = e.pulse;
                break;
            }
        }

        float secondsPerPulse = 60f / (pulsesPerBeat * referenceBpm);

        return referencePulse + (time - referenceTime) / secondsPerPulse;
    }

    // Works for negative pulses too.
    public float PulseToTime(int pulse)
    {
        float referenceBpm = (float)patternMetadata.initBpm;
        float referenceTime = (float)patternMetadata.firstBeatOffset;
        int referencePulse = 0;

        // Find the immediate BpmEvent before specified pulse.
        for (int i = bpmEvents.Count - 1; i >= 0; i--)
        {
            BpmEvent e = bpmEvents[i];
            if (e.pulse <= pulse)
            {
                referenceBpm = (float)e.bpm;
                referenceTime = e.time;
                referencePulse = e.pulse;
                break;
            }
        }

        float secondsPerPulse = 60f / (pulsesPerBeat * referenceBpm);

        return referenceTime + secondsPerPulse * (pulse - referencePulse);
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
public class PatternMetadata
{
    public string guid;

    public string patternName;
    public int level;
    public ControlScheme controlScheme;
    public int lanes;  // Currently unused
    public string author;

    // The backing track played in game.
    // This always plays from the beginning.
    // If no keysounds, this should be the entire track.
    public string backingTrack;
    // Beat 0 starts at this time.
    public double firstBeatOffset;

    // These can be changed by events.
    public double initBpm;
    // BPS: beats per scan.
    public int bps;

    public PatternMetadata()
    {
        guid = Guid.NewGuid().ToString();
        patternName = "New pattern";
        level = Pattern.minLevel;
        initBpm = Pattern.minBpm;
        bps = Pattern.minBps;
    }
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
public class SoundChannel
{
    // Sound file name.
    public string name;
    // Notes using this sound.
    public List<Note> notes;
    public List<HoldNote> holdNotes;
    public List<DragNote> dragNotes;

    public SoundChannel()
    {
        notes = new List<Note>();
        holdNotes = new List<HoldNote>();
        dragNotes = new List<DragNote>();
    }
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
public class Note
{
    public int lane;
    public int pulse;
    public NoteType type;
    [NonSerialized]
    public float time;

    public Note Clone()
    {
        if (this is HoldNote)
        {
            return new HoldNote()
            {
                type = type,
                lane = lane,
                pulse = pulse,
                duration = (this as HoldNote).duration
            };
        }
        if (this is DragNote)
        {
            DragNote clone = new DragNote()
            {
                type = type,
                lane = lane,
                pulse = pulse,
                nodes = new List<DragNode>()
            };
            foreach (DragNode node in (this as DragNote).nodes)
            {
                clone.nodes.Add(node.Clone());
            }
            return clone;
        }
        return new Note()
        {
            lane = lane,
            pulse = pulse,
            type = type
        };
    }
}

[Serializable]
public class HoldNote : Note
{
    public int duration;  // in pulses
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

    public static FloatPoint operator+(
        FloatPoint left, FloatPoint right)
    {
        return new FloatPoint(left.pulse + right.pulse,
            left.lane + right.lane);
    }

    public static FloatPoint operator*(float coeff,
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
    public FloatPoint controlBefore;
    // Relative to anchor
    public FloatPoint controlAfter;

    public DragNode Clone()
    {
        return new DragNode()
        {
            anchor = anchor.Clone(),
            controlBefore = controlBefore.Clone(),
            controlAfter = controlAfter.Clone()
        };
    }

    public void CopyFrom(DragNode other)
    {
        anchor = other.anchor;
        controlBefore = other.controlBefore;
        controlAfter = other.controlAfter;
    }
}

[Serializable]
public class DragNote : Note
{
    // There must be at least 2 nodes, with nodes[0]
    // describing the note head.
    // controlBefore of the first node and controlAfter
    // of the last node are ignored.
    public List<DragNode> nodes;

    public DragNote()
    {
        nodes = new List<DragNode>();
    }

    // Returns a list of points on the bezier curve defined by
    // this note. All points are relative to the note head.
    public List<FloatPoint> Interpolate()
    {
        List<FloatPoint> result = new List<FloatPoint>();
        result.Add(nodes[0].anchor.ToFloatPoint());
        const int numSteps = 50;
        for (int i = 0; i < nodes.Count - 1; i++)
        {
            FloatPoint p0 = nodes[i].anchor.ToFloatPoint();
            FloatPoint p1 = p0 + nodes[i].controlAfter;
            FloatPoint p3 = nodes[i + 1].anchor.ToFloatPoint();
            FloatPoint p2 = p3 + nodes[i + 1].controlBefore;
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

        return result;
    }
}

// Not intended to be serialized. Different from NoteObject,
// this class does not derive from MonoBehavior, so it's much
// lighter.
public class NoteWithSound
{
    public Note note;
    public string sound;
}