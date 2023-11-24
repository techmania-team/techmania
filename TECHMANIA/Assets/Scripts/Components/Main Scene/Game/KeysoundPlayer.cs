using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeysoundPlayer
{
    // Records which AudioSource is playing the keysounds of which
    // note, so they can be stopped later. This is meant for long
    // notes, and do not care about assist ticks.
    private Dictionary<Note, FmodChannelWrap> fmodChannelOfNote;
    private AudioSourceManager sourceManager;
    private AudioClip assistTick;

    public KeysoundPlayer(AudioClip assistTick)
    {
        sourceManager = AudioSourceManager.instance;
        this.assistTick = assistTick;
    }

    public void Prepare()
    {
        fmodChannelOfNote = new Dictionary<Note, FmodChannelWrap>();
    }

    public void Pause()
    {
        foreach (FmodChannelWrap channel in fmodChannelOfNote.Values)
        {
            channel.Pause();
        }
    }

    public void Unpause()
    {
        foreach (FmodChannelWrap channel in fmodChannelOfNote.Values)
        {
            channel.UnPause();
        }
    }

    public void Dispose()
    {
        foreach (FmodChannelWrap channel in fmodChannelOfNote.Values)
        {
            channel.Stop();
        }
        fmodChannelOfNote.Clear();
    }

    public void Play(Note n, bool hidden, bool emptyHit)
    {
        if (emptyHit && (
            n.type == NoteType.Hold ||
            n.type == NoteType.RepeatHeadHold ||
            n.type == NoteType.RepeatHold ||
            n.type == NoteType.Drag))
        {
            // Don't play keysounds for empty hits when the upcoming
            // note is a long one.
            return;
        }
        if (GameController.instance.modifiers.assistTick == 
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
        fmodChannelOfNote[n] = source;
    }

    // Only play if the note's keysound starts before baseTime
    // but ends after baseTime.
    public void PlayFromHalfway(Note n, bool hidden, float baseTime)
    {
        if (string.IsNullOrEmpty(n.sound)) return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.sound);
        if (n.time + clip.length <= baseTime) return;

        float startTime = baseTime - n.time;
        AudioSource source = sourceManager.PlayKeysound(clip,
            hidden, startTime, n.volumePercent, n.panPercent);
        fmodChannelOfNote[n] = source;
    }

    public void StopIfPlaying(Note n)
    {
        if (string.IsNullOrEmpty(n.sound)) return;

        AudioClip clip = ResourceLoader.GetCachedClip(n.sound);
        if (fmodChannelOfNote[n].clip == clip)
        {
            fmodChannelOfNote[n].Stop();
            fmodChannelOfNote.Remove(n);
        }
    }

    public void StopAll()
    {
        Dispose();
    }
}
