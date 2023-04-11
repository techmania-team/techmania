using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[MoonSharpUserData]
public class AudioSourceManager : MonoBehaviour
{
    [MoonSharpHidden]
    public static AudioSourceManager instance { get; private set; }

    [MoonSharpHidden]
    public UnityEngine.Audio.AudioMixer audioMixer;

    [MoonSharpHidden]
    public AudioSource musicSource;
    [MoonSharpHidden]
    public Transform playableLanesContainer;
    [MoonSharpHidden]
    public Transform hiddenLanesContainer;
    [MoonSharpHidden]
    public Transform sfxContainer;

    private AudioSource[] playableLanes;
    private AudioSource[] hiddenLanes;
    private AudioSource[] sfxSources;

    // Start is called before the first frame update
    private void Start()
    {
        instance = this;

        playableLanes = playableLanesContainer
            .GetComponentsInChildren<AudioSource>();
        hiddenLanes = hiddenLanesContainer
            .GetComponentsInChildren<AudioSource>();
        sfxSources = sfxContainer
            .GetComponentsInChildren<AudioSource>();
    }

    private void PrintReportOnAudioSource(string name,
        AudioSource s)
    {
        if (s.clip == null)
        {
            Debug.Log($"name:{name} isPlaying:{s.isPlaying} time:{s.time} timeSamples:{s.timeSamples} volume:{s.volume} clip:null");
        }
        else
        {
            Debug.Log($"name:{name} isPlaying:{s.isPlaying} time:{s.time} timeSamples:{s.timeSamples} volume:{s.volume} clip.length:{s.clip?.length} clip.samples:{s.clip?.samples}");
        }
    }

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftControl) &&
            Input.GetKeyDown(KeyCode.R))
        {
            Debug.Log("===== Beginning of AudioSourceManager report =====");
            PrintReportOnAudioSource("music", musicSource);
            for (int i = 0; i < playableLanes.Length; i++)
            {
                PrintReportOnAudioSource($"playable lane #{i}", playableLanes[i]);
            }
            for (int i = 0; i < hiddenLanes.Length; i++)
            {
                PrintReportOnAudioSource($"hidden lane #{i}", hiddenLanes[i]);
            }
            for (int i = 0; i < sfxSources.Length; i++)
            {
                PrintReportOnAudioSource($"sfx #{i}", sfxSources[i]);
            }
            Debug.Log("===== End of AudioSourceManager report =====");
        }
    }

    private void PlaySound(AudioSource source, AudioClip clip,
        float startTime, int volumePercent, int panPercent)
    {
        if (clip == null)
        {
            return;
        }
        
        int startSample = Mathf.FloorToInt(
            startTime * clip.frequency);
        source.clip = clip;
        source.timeSamples = Mathf.Min(clip.samples, startSample);
        source.volume = volumePercent * 0.01f;
        source.panStereo = panPercent * 0.01f;
        source.Play();
    }

    private AudioSource FindAvailableSource(AudioSource[] sources,
        string clipTypeForLogging)
    {
        AudioSource sourceWithLeastRemainingTime = null;
        float leastRemainingTime = float.MaxValue;
        foreach (AudioSource s in sources)
        {
            if (!s.isPlaying)
            {
                return s;
            }

            // Calculate the remaining time of this source.
            float remainingTime = s.clip.length - s.time;
            if (remainingTime < leastRemainingTime)
            {
                leastRemainingTime = remainingTime;
                sourceWithLeastRemainingTime = s;
            }
        }
        Debug.Log($"Out of available audio sources to play {clipTypeForLogging}; cutting one off.");
        return sourceWithLeastRemainingTime;
    }

    #region Play API
    public AudioSource PlayMusic(AudioClip clip,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        PlaySound(musicSource, clip, startTime,
            volumePercent, panPercent);
        return musicSource;
    }

    // Returns the AudioSource chosen to play the clip, if not null.
    public AudioSource PlayKeysound(AudioClip clip, bool hiddenLane,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        if (clip == null) return null;

        AudioSource source;
        if (hiddenLane)
        {
            source = FindAvailableSource(hiddenLanes,
                "keysound in hidden lane");
        }
        else
        {
            source = FindAvailableSource(playableLanes,
                "keysound in playable lane");
        }

        PlaySound(source, clip, startTime, volumePercent, panPercent);
        return source;
    }

    public AudioSource PlaySfx(AudioClip clip,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan)
    {
        AudioSource source = FindAvailableSource(sfxSources,
            "SFX");
        PlaySound(source, clip, 0f, volumePercent, panPercent);
        return source;
    }
    #endregion

    #region Batch control
    public void PauseAll()
    {
        musicSource.Pause();
        foreach (AudioSource s in playableLanes) s.Pause();
        foreach (AudioSource s in hiddenLanes) s.Pause();
    }

    public void UnpauseAll()
    {
        musicSource.UnPause();
        foreach (AudioSource s in playableLanes) s.UnPause();
        foreach (AudioSource s in hiddenLanes) s.UnPause();
    }

    public void StopAll()
    {
        musicSource.Stop();
        foreach (AudioSource s in playableLanes) s.Stop();
        foreach (AudioSource s in hiddenLanes) s.Stop();
    }

    public void SetSpeed(float speed)
    {
        musicSource.pitch = speed;
        foreach (AudioSource s in playableLanes) s.pitch = speed;
        foreach (AudioSource s in hiddenLanes) s.pitch = speed;
    }
    #endregion

    public bool IsAnySourcePlaying()
    {
        Func<AudioSource, bool> SourceIsPlaying = (AudioSource s) =>
        {
            if (!s.isPlaying) return false;
            // It's still unknown why but sometimes an audio source
            // reports that it's playing when in fact it's not.
            if (s.timeSamples == 0) return false;
            return true;
        };
        if (SourceIsPlaying(musicSource)) return true;
        foreach (AudioSource s in playableLanes)
        {
            if (SourceIsPlaying(s)) return true;
        }
        foreach (AudioSource s in hiddenLanes)
        {
            if (SourceIsPlaying(s)) return true;
        }
        foreach (AudioSource s in sfxSources)
        {
            if (SourceIsPlaying(s)) return true;
        }
        return false;
    }

    public void ApplyVolume()
    {
        audioMixer.SetFloat("MasterVolume",
            Options.VolumeValueToDb(
            Options.instance.masterVolumePercent));
        audioMixer.SetFloat("MusicVolume",
            Options.VolumeValueToDb(
            Options.instance.musicVolumePercent));
        audioMixer.SetFloat("KeysoundVolume",
            Options.VolumeValueToDb(
            Options.instance.keysoundVolumePercent));
        audioMixer.SetFloat("SfxVolume",
            Options.VolumeValueToDb(
            Options.instance.sfxVolumePercent));
    }
}
