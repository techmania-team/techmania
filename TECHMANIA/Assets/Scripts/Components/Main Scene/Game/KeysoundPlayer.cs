using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeysoundPlayer
{
    // Records which AudioSource is playing the keysounds of which
    // note, so they can be stopped later. This is meant for long
    // notes, and do not care about assist ticks.
    private Dictionary<Note, AudioSource> audioSourceOfNote;
    private AudioSourceManager sourceManager;
    private AudioClip assistTick;

    public KeysoundPlayer(AudioClip assistTick)
    {
        sourceManager = AudioSourceManager.instance;
        this.assistTick = assistTick;
    }

    public void Prepare()
    {
        audioSourceOfNote = new Dictionary<Note, AudioSource>();
    }

    public void Dispose()
    {
        foreach (AudioSource source in audioSourceOfNote.Values)
        {
            source.Stop();
        }
        audioSourceOfNote.Clear();
    }

    public void Play(Note n, bool hidden, bool emptyHit)
    {
        if (emptyHit && (
            n.type == NoteType.Hold ||
            n.type == NoteType.Hold ||
            n.type == NoteType.Hold ||
            n.type == NoteType.Hold))
        {
            // Don't play keysounds for empty hits when the upcoming
            // note is a long one.
            return;
        }
        if (Modifiers.instance.assistTick == 
            Modifiers.AssistTick.AssistTick && !hidden && !emptyHit)
        {
            sourceManager.PlaySfx(assistTick);
        }
        if (n is AssistTickNote)  // Auto assist tick
        {
            sourceManager.PlaySfx(assistTick);
        }

        if (string.IsNullOrEmpty(n.sound)) return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.sound);
        AudioSource source = sourceManager.PlayKeysound(clip,
            hidden,
            startTime: 0f,
            n.volumePercent, n.panPercent);
        audioSourceOfNote[n] = source;
    }

    public void StopIfPlaying(Note n)
    {
        if (string.IsNullOrEmpty(n.sound)) return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.sound);
        if (audioSourceOfNote[n].clip == clip)
        {
            audioSourceOfNote[n].Stop();
            audioSourceOfNote.Remove(n);
        }
    }
}
