using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioSourceManager : MonoBehaviour
{
    public AudioSource backingTrack;
    public Transform playableLanesContainer;
    public Transform hiddenLanesContainer;

    private AudioSource[] playableLanes;
    private AudioSource[] hiddenLanes;

    // Start is called before the first frame update
    void Start()
    {
        playableLanes = playableLanesContainer
            .GetComponentsInChildren<AudioSource>();
        hiddenLanes = hiddenLanesContainer
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

    // Returns the AudioSource chosen to play the clip, if not null.
    public AudioSource PlayKeysound(AudioClip clip, bool hiddenLane,
        float startTime = 0f,
        float volume = 1f, float pan = 0f)
    {
        if (clip == null) return null;

        AudioSource[] sources = hiddenLane ?
            hiddenLanes : playableLanes;

        AudioSource sourceWithLeastRemainingTime = null;
        float leastRemainingTime = float.MaxValue;
        foreach (AudioSource s in sources)
        {
            if (!s.isPlaying)
            {
                PlaySound(s, clip, startTime, volume, pan);
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

        // If no source is available, cut off the source with least
        // remaining time.
        Debug.LogWarning($"Out of available audio sources, cutting one off. hiddenLane={hiddenLane}");
        PlaySound(sourceWithLeastRemainingTime, clip, startTime,
            volume, pan);
        return sourceWithLeastRemainingTime;
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
}
