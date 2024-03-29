using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KeysoundPlayer
{
    // Records which FmodChannelWrap is playing the keysounds of which
    // note, so they can be stopped later. This is meant for long
    // notes, and do not care about assist ticks.
    private Dictionary<Note, FmodChannelWrap> fmodChannelOfNote;
    private AudioManager sourceManager;
    private FmodSoundWrap assistTick;

    public KeysoundPlayer(FmodSoundWrap assistTick)
    {
        sourceManager = AudioManager.instance;
        this.assistTick = assistTick;
    }

    public void Prepare()
    {
        fmodChannelOfNote = new Dictionary<Note, FmodChannelWrap>();
        removeChannelFromMapOnSoundEnd = true;
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
        StopAll();
    }

    private bool removeChannelFromMapOnSoundEnd;
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

        FmodSoundWrap sound = ResourceLoader.GetCachedSound(n.sound);
        FmodChannelWrap channel = sourceManager.PlayKeysound(sound,
            hidden,
            startTime: 0f,
            n.volumePercent, n.panPercent);
        channel.SetSoundEndCallback(() =>
        {
            if (removeChannelFromMapOnSoundEnd)
            {
                fmodChannelOfNote.Remove(n);
            }
        });
        fmodChannelOfNote[n] = channel;
    }

    // Only play if the note's keysound starts before baseTime
    // but ends after baseTime.
    public void PlayFromHalfway(Note n, bool hidden, float baseTime)
    {
        if (string.IsNullOrEmpty(n.sound)) return;

        FmodSoundWrap sound = ResourceLoader.GetCachedSound(n.sound);
        if (n.time + sound.length <= baseTime) return;

        float startTime = baseTime - n.time;
        FmodChannelWrap channel = sourceManager.PlayKeysound(sound,
            hidden, startTime, n.volumePercent, n.panPercent);
        fmodChannelOfNote[n] = channel;
    }

    public void StopIfPlaying(Note n)
    {
        if (string.IsNullOrEmpty(n.sound)) return;

        // It's possible that the keysound was short and has
        // already stopped and erased itself.
        if (!fmodChannelOfNote.ContainsKey(n)) return;

        FmodSoundWrap sound = ResourceLoader.GetCachedSound(n.sound);
        if (fmodChannelOfNote[n].sound.Equals(sound))
        {
            // Sound end callback will delete the note
            // from fmodChannelOfNote.
            fmodChannelOfNote[n].Stop();
        }
    }

    public void StopAll()
    {
        // We need to prevent the sound end callback from modifying
        // fmodChannelOfNote.
        removeChannelFromMapOnSoundEnd = false;
        foreach (FmodChannelWrap channel in fmodChannelOfNote.Values)
        {
            channel.Stop();
        }
        removeChannelFromMapOnSoundEnd = true;

        fmodChannelOfNote.Clear();
    }
}
