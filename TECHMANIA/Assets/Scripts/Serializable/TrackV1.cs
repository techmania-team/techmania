using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// The deprecated, version 1 of Track definition, retained so
// the game can automatically convert outdated tracks to
// future versions.
//
// Some fields reference not-yet-deprecated classes:
// - ControlScheme, NoteType
// - BpmEvent
// - IntPoint, FloatPoint, DragNode
//
// Reasons for deprecation:
// - Background image and video should belong to pattern metadata,
//   not track metadata. This way each pattern can use different
//   backgrounds, if needed.
// - Notes do not contain their own keysound, or a reference to
//   the sound channel they belong to. This creates various hurdles
//   in code. V2 removes the notion of sound channels entirely.
// - Notes are not packed, and instead are serialized in full. This
//   causes patterns to be megabytes in size, which is ridiculous.

// Heavily inspired by bmson:
// https://bmson-spec.readthedocs.io/en/master/doc/index.html#format-overview
[Serializable]
public class TrackV1 : TrackBase
{
    public const string kVersion = "1";

    public TrackV1(string title, string artist)
    {
        version = kVersion;
        trackMetadata = new TrackMetadataV1();
        trackMetadata.guid = Guid.NewGuid().ToString();
        trackMetadata.title = title;
        trackMetadata.artist = artist;
        patterns = new List<PatternV1>();
    }

    public TrackMetadataV1 trackMetadata;
    public List<PatternV1> patterns;

    public void SortPatterns()
    {
        patterns.Sort((PatternV1 p1, PatternV1 p2) =>
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

    public PatternV1 FindPatternByGuid(string guid)
    {
        int index = FindPatternIndexByGuid(guid);
        if (index < 0) return null;
        return patterns[index];
    }

    protected override TrackBase Upgrade()
    {
        Debug.Log("Upgrading a version 1 track to version 2.");
        TrackV2 track = new TrackV2(trackMetadata.title,
            trackMetadata.artist);
        trackMetadata.UpgradeTo(track.trackMetadata);
        foreach (PatternV1 p in patterns)
        {
            track.patterns.Add(p.Upgrade(
                oldTrackMetadata: trackMetadata));
        }
        return track;
    }
}

[Serializable]
public class TrackMetadataV1
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

    public void UpgradeTo(TrackMetadataV2 metadata)
    {
        // GUID should persist through upgrades.
        metadata.guid = guid;

        // Title and artist should be already set.
        metadata.genre = genre;
        metadata.additionalCredits = "";
        metadata.eyecatchImage = eyecatchImage;
        metadata.previewTrack = previewTrack;
        metadata.previewStartTime = previewStartTime;
        metadata.previewEndTime = previewEndTime;
    }
}

[Serializable]
public class PatternV1
{
    public PatternMetadataV1 patternMetadata;
    public List<BpmEvent> bpmEvents;
    public List<SoundChannel> soundChannels;

    public const int pulsesPerBeat = 240;
    public const int minLevel = 1;
    public const int maxLevel = 12;
    public const double minBpm = 1.0;
    public const double maxBpm = 1000.0;
    public const int minBps = 1;
    public const int maxBps = 128;

    public PatternV1()
    {
        patternMetadata = new PatternMetadataV1();
        bpmEvents = new List<BpmEvent>();
        soundChannels = new List<SoundChannel>();
    }

    public PatternV1 CloneWithDifferentGuid()
    {
#if UNITY_2021
        string json = UnityEngine.JsonUtility.ToJson(
            this, prettyPrint: false);
        PatternV1 clone = UnityEngine.JsonUtility
            .FromJson<PatternV1>(json);
        clone.patternMetadata.guid = Guid.NewGuid().ToString();
        return clone;
#else
        return null;
#endif
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
    public void AddNote(NoteV1 n, string sound)
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
        if (n is HoldNoteV1)
        {
            channel.holdNotes.Add(n as HoldNoteV1);
        }
        else if (n is DragNoteV1)
        {
            channel.dragNotes.Add(n as DragNoteV1);
        }
        else
        {
            channel.notes.Add(n);
        }
    }

    public void ModifyNoteKeysound(NoteV1 n,
        string oldSound, string newSound)
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

        if (n is HoldNoteV1)
        {
            oldChannel.holdNotes.Remove(n as HoldNoteV1);
            newChannel.holdNotes.Add(n as HoldNoteV1);
        }
        else if (n is DragNoteV1)
        {
            oldChannel.dragNotes.Remove(n as DragNoteV1);
            newChannel.dragNotes.Add(n as DragNoteV1);
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

    public void DeleteNote(NoteV1 n, string sound)
    {
        SoundChannel channel = soundChannels.Find(
            (SoundChannel c) => { return c.name == sound; });
        if (channel == null)
        {
            throw new Exception(
                $"Sound channel {sound} not found in pattern when deleting.");
        }

        if (n is HoldNoteV1)
        {
            channel.holdNotes.Remove(n as HoldNoteV1);
        }
        else if (n is DragNoteV1)
        {
            channel.dragNotes.Remove(n as DragNoteV1);
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
            e.time = currentTime +
                secondsPerPulse * (e.pulse - currentPulse);

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
            foreach (NoteV1 n in c.notes)
            {
                n.time = PulseToTime(n.pulse);
            }
            foreach (NoteV1 n in c.holdNotes)
            {
                n.time = PulseToTime(n.pulse);
            }
            foreach (NoteV1 n in c.dragNotes)
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

        return referencePulse +
            (time - referenceTime) / secondsPerPulse;
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

        return referenceTime +
            secondsPerPulse * (pulse - referencePulse);
    }

    public PatternV2 Upgrade(TrackMetadataV1 oldTrackMetadata)
    {
        PatternV2 pattern = new PatternV2();
        patternMetadata.UpgradeTo(pattern.patternMetadata,
            oldTrackMetadata);
        foreach (BpmEvent e in bpmEvents)
        {
            pattern.bpmEvents.Add(e.Clone());
        }
        foreach (SoundChannel channel in soundChannels)
        {
            string sound = channel.name;
            foreach (NoteV1 n in channel.notes)
            {
                pattern.packedNotes.Add(n.Upgrade(sound).Pack());
            }
            foreach (HoldNoteV1 n in channel.holdNotes)
            {
                pattern.packedHoldNotes.Add(n.Upgrade(sound).Pack());
            }
            foreach (DragNoteV1 n in channel.dragNotes)
            {
                pattern.packedDragNotes.Add(n.Upgrade(sound).Pack());
            }
        }
        return pattern;
    }
}

[Serializable]
public class PatternMetadataV1
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

    public PatternMetadataV1()
    {
        guid = Guid.NewGuid().ToString();
        patternName = "New pattern";
        level = PatternV1.minLevel;
        initBpm = PatternV1.minBpm;
        bps = PatternV1.minBps;
    }

    public void UpgradeTo(PatternMetadataV2 metadata,
        TrackMetadataV1 oldTrackMetadata)
    {
        metadata.guid = guid;

        metadata.patternName = patternName;
        metadata.level = level;
        metadata.controlScheme = controlScheme;
        metadata.author = author;

        metadata.backingTrack = backingTrack;
        metadata.backImage = oldTrackMetadata.backImage;
        metadata.bga = oldTrackMetadata.bga;
        metadata.bgaOffset = oldTrackMetadata.bgaOffset;

        metadata.firstBeatOffset = firstBeatOffset;
        metadata.initBpm = initBpm;
        metadata.bps = bps;
    }
}

[Serializable]
public class SoundChannel
{
    // Sound file name.
    public string name;
    // Notes using this sound.
    public List<NoteV1> notes;
    public List<HoldNoteV1> holdNotes;
    public List<DragNoteV1> dragNotes;

    public SoundChannel()
    {
        notes = new List<NoteV1>();
        holdNotes = new List<HoldNoteV1>();
        dragNotes = new List<DragNoteV1>();
    }
}

[Serializable]
public class NoteV1
{
    public int lane;
    public int pulse;
    public NoteType type;
#if UNITY_2021
    [NonSerialized]
#else
    [System.Text.Json.Serialization.JsonIgnore]
#endif
    public float time;

    public NoteV1 Clone()
    {
        if (this is HoldNoteV1)
        {
            return new HoldNoteV1()
            {
                type = type,
                lane = lane,
                pulse = pulse,
                duration = (this as HoldNoteV1).duration
            };
        }
        if (this is DragNoteV1)
        {
            DragNoteV1 clone = new DragNoteV1()
            {
                type = type,
                lane = lane,
                pulse = pulse,
                nodes = new List<DragNode>()
            };
            foreach (DragNode node in (this as DragNoteV1).nodes)
            {
                clone.nodes.Add(node.Clone());
            }
            return clone;
        }
        return new NoteV1()
        {
            lane = lane,
            pulse = pulse,
            type = type
        };
    }

    public NoteV2 Upgrade(string sound)
    {
        return new NoteV2()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound
        };
    }
}

[Serializable]
public class HoldNoteV1 : NoteV1
{
    public int duration;  // in pulses

    public new HoldNoteV2 Upgrade(string sound)
    {
        return new HoldNoteV2()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,
            duration = duration
        };
    }
}

[Serializable]
public class DragNoteV1 : NoteV1
{
    // There must be at least 2 nodes, with nodes[0]
    // describing the note head.
    // controlBefore of the first node and controlAfter
    // of the last node are ignored.
    public List<DragNode> nodes;

    public DragNoteV1()
    {
        nodes = new List<DragNode>();
    }

    public int Duration()
    {
        return (int)nodes[nodes.Count - 1].anchor.pulse;
    }

    // Returns a list of points on the bezier curve defined by
    // this note. All points are relative to the note head.
    public List<FloatPoint> Interpolate()
    {
        List<FloatPoint> result = new List<FloatPoint>();
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

        return result;
    }

    public new DragNoteV2 Upgrade(string sound)
    {
        DragNoteV2 newNote = new DragNoteV2()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound
        };
        foreach (DragNode node in nodes)
        {
            newNote.nodes.Add(node.Clone());
        }
        return newNote;
    }
}