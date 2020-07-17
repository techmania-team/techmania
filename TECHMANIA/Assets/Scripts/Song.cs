using System;
using System.Collections;
using System.Collections.Generic;

// Song is the container of all patterns in a song. In anticipation of
// format updates, each format version is a derived class of SongBase.
//
// Because class names are not serialized, we can change class names
// however we want without breaking old files, so the current version
// class will always be called "Song", and deprecated versions will be
// called "SongVersion1" or such.

[Serializable]
public class SongBase
{
    public string version;

    public string Serialize()
    {
        return UnityEngine.JsonUtility.ToJson(this, prettyPrint: true);
    }
    public static SongBase Deserialize(string json)
    {
        string version = UnityEngine.JsonUtility.FromJson<SongBase>(json).version;
        switch (version)
        {
            case Song.kVersion:
                return UnityEngine.JsonUtility.FromJson<Song>(json);
                // For non-current versions, maybe attempt conversion?
            default:
                throw new Exception($"Unknown version: {version}");
        }
    }
}

// Heavily inspired by bmson:
// https://bmson-spec.readthedocs.io/en/master/doc/index.html#format-overview
[Serializable]
public class Song : SongBase
{
    public const string kVersion = "1";
    public Song() { version = kVersion; }

    public SongMetadata song_metadata;
    public List<Pattern> patterns;
}

[Serializable]
public class SongMetadata
{
    // Text stuff.

    public string title;
    public string subtitle;
    public string artist;
    public List<string> sub_artists;
    public string genre;

    // In song select screen.

    // Filename of eyecatch image.
    public string eyecatch_image;
    // Filename of preview music.
    public string preview_music;
    // In seconds.
    public double preview_start_time;
    public double preview_end_time;

    // In gameplay.

    // Filename of background image, used in loading screen
    public string back_image;
    // Filename of background animation (BGA)
    // If empty, will show background image
    public string bga;
    // Play BGA from this time.
    public double bga_start_time;
}

[Serializable]
public class Pattern
{
    public PatternMetadata pattern_metadata;
    public List<BpmEvent> bpm_events;
    public List<BpcEvent> bpc_events;
    public List<SoundChannel> sound_channels;
}

[Serializable]
public enum ControlScheme
{
    Touch,
    Keys,
    KM
}

[Serializable]
public class PatternMetadata
{
    public string pattern_name;
    public int level;
    public ControlScheme control_scheme;

    // The backing track played in game.
    // This always plays from the beginning.
    // If no keysounds, this should be the entire song.
    public string base_music;
    // Beat 0 starts at this time.
    public double first_beat_offset;

    // These can be changed by events.
    public double init_bpm;
    // BPC: beats per scan.
    public int init_bpc;
}

[Serializable]
public class PatternEventBase
{
    public long pulse;
}

[Serializable]
public class BpmEvent : PatternEventBase
{
    public double bpm;
}

[Serializable]
public class BpcEvent : PatternEventBase
{
    public int bpc;
}

[Serializable]
public class SoundChannel
{
    // Sound file name.
    public string name;
    // Notes using this sound.
    public List<Note> notes;
    public List<HoldNote> hold_notes;
    public List<DragNote> drag_notes;
}

[Serializable]
public enum NoteType
{
    Basic,
    ChainHead,
    Chain,
    Hold,
    Drag,
    ComboHead,
    Combo,
    ComboHold
}

[Serializable]
public class Note
{
    public int lane;
    public long pulse;
    public NoteType type;
}

// For Hold and ComboHold notes.
[Serializable]
public class HoldNote : Note
{
    public long length;
}

[Serializable]
public class DragNotePath
{
    public int lane;
    public long pulse;
}

[Serializable]
public class DragNote : Note
{
    public List<DragNotePath> path;
}