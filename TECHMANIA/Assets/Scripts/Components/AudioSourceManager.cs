using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSourceManager : MonoBehaviour
{
    public AudioSource backingTrack;
    public Transform playableLanesContainer;
    public Transform hiddenLanesContainer;
    public Transform sfxContainer;

    private AudioSource[] playableLanes;
    private AudioSource[] hiddenLanes;
    private AudioSource[] sfxSources;

    // Start is called before the first frame update
    void Start()
    {
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
            PrintReportOnAudioSource("backing track", backingTrack);
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

    public void PlayBackingTrack(AudioClip clip,
        float startTime = 0f)
    {
        PlaySound(backingTrack, clip, startTime,
            volumePercent: Note.defaultVolume,
            panPercent: Note.defaultPan);
    }

    private AudioSource FindSource(AudioSource[] sources,
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

    // Returns the AudioSource chosen to play the clip, if not null.
    public AudioSource PlayKeysound(AudioClip clip, bool hiddenLane,
        float startTime = 0f,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan,
        bool fromAutoKeysound = false)
    {
        if (clip == null) return null;

        AudioSource source;
        if (hiddenLane && !fromAutoKeysound)
        {
            source = FindSource(hiddenLanes,
                "keysound in hidden lane");
        }
        else
        {
            source = FindSource(playableLanes,
                "keysound in playable lane");
        }

        PlaySound(source, clip, startTime, volumePercent, panPercent);
        return source;
    }

    public void PlaySfx(AudioClip clip)
    {
        AudioSource source = FindSource(sfxSources,
            "SFX");
        PlaySound(source, clip, 0f,
            volumePercent: 100, panPercent: 0);
    }

    public void PauseAll()
    {
        backingTrack.Pause();
        foreach (AudioSource s in playableLanes) s.Pause();
        foreach (AudioSource s in hiddenLanes) s.Pause();
    }

    public void UnpauseAll()
    {
        backingTrack.UnPause();
        foreach (AudioSource s in playableLanes) s.UnPause();
        foreach (AudioSource s in hiddenLanes) s.UnPause();
    }

    public void StopAll()
    {
        backingTrack.Stop();
        foreach (AudioSource s in playableLanes) s.Stop();
        foreach (AudioSource s in hiddenLanes) s.Stop();
    }

    public void SetSpeed(float speed)
    {
        backingTrack.pitch = speed;
        foreach (AudioSource s in playableLanes) s.pitch = speed;
        foreach (AudioSource s in hiddenLanes) s.pitch = speed;
    }

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
        if (SourceIsPlaying(backingTrack)) return true;
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
}
