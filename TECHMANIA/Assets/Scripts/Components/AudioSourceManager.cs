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

    private void PlaySound(AudioSource source, AudioClip clip,
        float startTime, float volume, float pan)
    {
        if (clip == null)
        {
            return;
        }
        
        int startSample = Mathf.FloorToInt(
            startTime * clip.frequency);
        source.clip = clip;
        source.timeSamples = Mathf.Min(clip.samples, startSample);
        source.volume = volume;
        source.panStereo = pan;
        source.Play();
    }

    public void PlayBackingTrack(AudioClip clip,
        float startTime = 0f)
    {
        PlaySound(backingTrack, clip, startTime,
            volume: 1f, pan: 0f);
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
        float volume = Note.defaultVolume,
        float pan = Note.defaultPan)
    {
        if (clip == null) return null;

        AudioSource source;
        if (hiddenLane)
        {
            source = FindSource(hiddenLanes,
                "keysound in hidden lane");
        }
        else
        {
            source = FindSource(playableLanes,
                "keysound in playable lane");
        }

        PlaySound(source, clip, startTime, volume, pan);
        return source;
    }

    public void PlaySfx(AudioClip clip)
    {
        AudioSource source = FindSource(sfxSources,
            "SFX");
        PlaySound(source, clip, 0f, 1f, 0f);
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
        foreach (AudioSource s in playableLanes)
        {
            if (s.isPlaying) return true;
        }
        foreach (AudioSource s in hiddenLanes)
        {
            if (s.isPlaying) return true;
        }
        foreach (AudioSource s in sfxSources)
        {
            if (s.isPlaying) return true;
        }
        return false;
    }
}
