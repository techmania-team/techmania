using MoonSharp.Interpreter;
using MoonSharp.VsCodeDebugger.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO: merge this with FmodManager.
[MoonSharpUserData]
public class AudioSourceManager : MonoBehaviour
{
    [MoonSharpHidden]
    public static AudioSourceManager instance { get; private set; }

    // Start is called before the first frame update
    private void Start()
    {
        instance = this;
    }

    private FmodChannelWrap PlaySound(FmodManager.ChannelGroupType group,
        FmodSoundWrap sound,
        float startTime, int volumePercent, int panPercent)
    {
        if (sound == null)
        {
            return null;
        }
        
        int startSample = Mathf.FloorToInt(
            startTime * sound.frequency);
        FmodChannelWrap channel = FmodManager.instance.Play(
            sound, group, paused: true);
        channel.timeSamples = Mathf.Min(sound.samples, startSample);
        channel.volume = volumePercent * 0.01f;
        channel.panStereo = panPercent * 0.01f;
        channel.Play();
        return channel;
    }

    #region Play API
    public FmodChannelWrap PlayMusic(FmodSoundWrap sound,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        return PlaySound(FmodManager.ChannelGroupType.Music,
            sound, startTime, volumePercent, panPercent);
    }

    public FmodChannelWrap PlayKeysound(FmodSoundWrap sound,
        bool hiddenLane,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        if (sound == null) return null;

        FmodManager.ChannelGroupType group = hiddenLane ? 
            FmodManager.ChannelGroupType.Music :
            FmodManager.ChannelGroupType.Keysound;
        return PlaySound(group, sound, startTime,
            volumePercent, panPercent);
    }

    public FmodChannelWrap PlaySfx(FmodSoundWrap sound,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        return PlaySound(FmodManager.ChannelGroupType.SFX,
            sound, startTime: 0f, volumePercent, panPercent);
    }
    #endregion

    #region Batch control
    public void PauseAll()
    {
        FmodManager.instance.PauseAll();
    }

    public void UnpauseAll()
    {
        FmodManager.instance.UnpauseAll();
    }

    public void StopAll()
    {
        FmodManager.instance.StopAll();
    }

    public void SetSpeed(float speed)
    {
        FmodManager.instance.SetSpeed(speed);
    }
    #endregion

    public bool IsAnySoundPlaying()
    {
        return FmodManager.instance.AnySoundPlaying();
    }

    // For backwards compatibility with API version 1.
    public bool IsAnySourcePlaying()
    {
        return IsAnySoundPlaying();
    }

    public void ApplyVolume()
    {
        FmodManager.instance.SetVolume(
            FmodManager.ChannelGroupType.Master,
            Options.instance.masterVolumePercent * 0.01f);
        FmodManager.instance.SetVolume(
            FmodManager.ChannelGroupType.Music,
            Options.instance.musicVolumePercent * 0.01f);
        FmodManager.instance.SetVolume(
            FmodManager.ChannelGroupType.Keysound,
            Options.instance.keysoundVolumePercent * 0.01f);
        FmodManager.instance.SetVolume(
            FmodManager.ChannelGroupType.SFX,
            Options.instance.sfxVolumePercent * 0.01f);
    }
}
