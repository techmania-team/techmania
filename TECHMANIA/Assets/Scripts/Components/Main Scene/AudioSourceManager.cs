using MoonSharp.Interpreter;
using MoonSharp.VsCodeDebugger.SDK;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
        AudioClip clip,
        float startTime, int volumePercent, int panPercent)
    {
        if (clip == null)
        {
            return null;
        }
        
        int startSample = Mathf.FloorToInt(
            startTime * clip.frequency);
        FMOD.Channel internalChannel = FmodManager.instance.Play(
            clip, group, paused: true);
        FmodChannelWrap channel = new FmodChannelWrap(internalChannel);
        channel.timeSamples = Mathf.Min(clip.samples, startSample);
        channel.volume = volumePercent * 0.01f;
        channel.panStereo = panPercent * 0.01f;
        channel.Play();
        return channel;
    }

    #region Play API
    public FmodChannelWrap PlayMusic(AudioClip clip,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        return PlaySound(FmodManager.ChannelGroupType.Music,
            clip, startTime, volumePercent, panPercent);
    }

    public FmodChannelWrap PlayKeysound(AudioClip clip, bool hiddenLane,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        if (clip == null) return null;

        FmodManager.ChannelGroupType group = hiddenLane ? 
            FmodManager.ChannelGroupType.Music :
            FmodManager.ChannelGroupType.Keysound;
        return PlaySound(group, clip, startTime,
            volumePercent, panPercent);
    }

    public FmodChannelWrap PlaySfx(AudioClip clip,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        return PlaySound(FmodManager.ChannelGroupType.SFX,
            clip, startTime: 0f, volumePercent, panPercent);
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

    public bool IsAnySourcePlaying()
    {
        return FmodManager.instance.AnySoundPlaying();
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
