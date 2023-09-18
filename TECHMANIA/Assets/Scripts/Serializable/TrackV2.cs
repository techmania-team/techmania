using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Track version 2 introduces "packing", which basically
// compresses notes to concise strings before serialization,
// so we don't output field names and hundreds of spaces for
// each note. This should bring the serialized tracks to a
// reasonable size.
//
// Notes contain optional parameters. A note with non-default values
// on any such parameter is considered an "extended" note, and
// is packed differently from normal notes.

[Serializable]
public class TrackV2 : TrackBase
{
    public const string kVersion = "2";

    public TrackMetadataV2 trackMetadata;
    public List<PatternV2> patterns;

    public TrackV2(string title, string artist)
    {
        version = kVersion;
        trackMetadata = new TrackMetadataV2();
        trackMetadata.guid = Guid.NewGuid().ToString();
        trackMetadata.title = title;
        trackMetadata.artist = artist;
        patterns = new List<PatternV2>();
    }

    protected override TrackBase Upgrade()
    {
        // Deserialization calls Upgrade before InitAfterDeserialize.
        // Therefore, we should upgrade notes to version 3, then
        // pack up as version 3, so InitAfterDeserialize can unpack
        // them properly.
        Track t = new Track()
        {
            trackMetadata = trackMetadata.Upgrade(),
            patterns = new List<Pattern>()
        };
        if (patterns != null)
        {
            foreach (PatternV2 p in patterns)
            {
                t.patterns.Add(p.Upgrade());
            }
        }
        return t;
    }
}

[Serializable]
public class TrackMetadataV2
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

    public TrackMetadata Upgrade()
    {
        return new TrackMetadata()
        {
            guid = guid,

            title = title,
            artist = artist,
            genre = genre,
            additionalCredits = additionalCredits,

            eyecatchImage = eyecatchImage,
            previewTrack = previewTrack,
            previewStartTime = previewStartTime,
            previewEndTime = previewEndTime
        };
    }
}

[Serializable]
public class PatternV2
{
    public PatternMetadataV2 patternMetadata;
    public List<BpmEvent> bpmEvents;
    public List<TimeStop> timeStops;

    public List<string> packedNotes;
    public List<string> packedHoldNotes;
    public List<PackedDragNote> packedDragNotes;

    public Pattern Upgrade()
    {
        Pattern p = new Pattern()
        {
            patternMetadata = patternMetadata.Upgrade(),
            packedNotes = new List<string>(),
            packedHoldNotes = new List<string>(),
            packedDragNotes = new List<PackedDragNote>()
        };
        if (bpmEvents != null)
        {
            foreach (BpmEvent e in bpmEvents)
            {
                p.bpmEvents.Add(e.Clone());
            }
        }
        if (timeStops != null)
        {
            foreach (TimeStop t in timeStops)
            {
                p.timeStops.Add(t.Clone());
            }
        }

        // Unpack notes as V2, then re-pack as v3.
        if (packedNotes != null)
        {
            foreach (string s in packedNotes)
            {
                NoteV2 n = NoteV2.Unpack(s);
                Note upgraded = n.Upgrade();
                p.packedNotes.Add(upgraded.Pack());
            }
        }
        if (packedHoldNotes != null)
        {
            foreach (string s in packedHoldNotes)
            {
                HoldNoteV2 n = HoldNoteV2.Unpack(s);
                HoldNote upgraded = n.Upgrade() as HoldNote;
                p.packedHoldNotes.Add(upgraded.Pack());
            }
        }
        if (packedDragNotes != null)
        {
            foreach (PackedDragNote s in packedDragNotes)
            {
                DragNoteV2 n = DragNoteV2.Unpack(s);
                DragNote upgraded = n.Upgrade() as DragNote;
                p.packedDragNotes.Add(upgraded.Pack());
            }
        }

        return p;
    }
}

[Serializable]
public class PatternMetadataV2
{
    public string guid;

    // Basics.

    public string patternName;
    public int level;
    public ControlScheme controlScheme;
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

    public PatternMetadata Upgrade()
    {
        return new PatternMetadata()
        {
            guid = guid,

            patternName = patternName,
            level = level,
            controlScheme = controlScheme,
            author = author,

            backingTrack = backingTrack,
            backImage = backImage,
            bga = bga,
            bgaOffset = bgaOffset,
            waitForEndOfBga = waitForEndOfBga,
            playBgaOnLoop = playBgaOnLoop,

            firstBeatOffset = firstBeatOffset,
            initBpm = initBpm,
            bps = bps
        };
    }
}

public class NoteV2
{
    // Calculated at unpack time:

    public NoteType type;
    public int pulse;
    public int lane;
    public string sound;  // Filename with extension, no folder

    // Optional parameters:

    public float volume;
    public float pan;
    public bool endOfScan;
    protected string endOfScanString
    {
        get { return endOfScan ? "1" : "0"; }
        set { endOfScan = value == "1"; }
    }

    public const float defaultVolume = 1f;
    public const float defaultPan = 0f;

    public NoteV2()
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

    public static NoteV2 Unpack(string packed)
    {
        char[] delim = new char[] { '|' };
        // Beware that the "sound" portion may contain |.
        string[] splits = packed.Split(delim, 2);
        // Extended?
        if (splits[0] == "E")
        {
            splits = packed.Split(delim, 8);
            return new NoteV2()
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
            return new NoteV2()
            {
                type = (NoteType)Enum.Parse(
                    typeof(NoteType), splits[0]),
                pulse = int.Parse(splits[1]),
                lane = int.Parse(splits[2]),
                sound = splits[3]
            };
        }
    }

    public virtual Note Upgrade()
    {
        return new Note()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,

            volumePercent = Mathf.FloorToInt(volume * 100f),
            panPercent = Mathf.FloorToInt(pan * 100f),
            endOfScan = endOfScan
        };
    }
}

public class HoldNoteV2 : NoteV2
{
    public int duration;  // In pulses.

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

    public static new HoldNoteV2 Unpack(string packed)
    {
        char[] delim = new char[] { '|' };
        // Beware that the "sound" portion may contain |.
        string[] splits = packed.Split(delim, 2);
        // Extended?
        if (splits[0] == "E")
        {
            splits = packed.Split(delim, 9);
            return new HoldNoteV2()
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
            return new HoldNoteV2()
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

    public override Note Upgrade()
    {
        return new HoldNote()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,

            volumePercent = Mathf.FloorToInt(volume * 100f),
            panPercent = Mathf.FloorToInt(pan * 100f),
            endOfScan = endOfScan,

            duration = duration
        };
    }
}

public class DragNoteV2 : NoteV2
{
    public CurveType curveType;

    // There must be at least 2 nodes, with nodes[0]
    // describing the note head.
    // controlBefore of the first node and controlAfter
    // of the last node are ignored.
    public List<DragNode> nodes;

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

    public static DragNoteV2 Unpack(PackedDragNote packed)
    {
        char[] delim = new char[] { '|' };
        // Beware that the "sound" portion may contain |.
        string[] splits = packed.packedNote.Split(delim, 2);
        DragNoteV2 dragNote;
        // Extended?
        if (splits[0] == "E")
        {
            splits = packed.packedNote.Split(delim, 8);
            dragNote = new DragNoteV2()
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
            dragNote = new DragNoteV2()
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

    public override Note Upgrade()
    {
        DragNote n = new DragNote()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,

            volumePercent = Mathf.FloorToInt(volume * 100f),
            panPercent = Mathf.FloorToInt(pan * 100f),
            endOfScan = endOfScan,

            curveType = curveType,
            nodes = new List<DragNode>()
        };
        if (nodes != null)
        {
            foreach (DragNode node in nodes) n.nodes.Add(node.Clone());
        }
        return n;
    }
}