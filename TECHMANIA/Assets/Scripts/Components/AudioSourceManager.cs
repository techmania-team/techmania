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
        float startTime)
    {
        if (clip == null) return;

        int startSample = Mathf.FloorToInt(
            startTime * clip.frequency);
        source.clip = clip;
        source.timeSamples = Mathf.Min(clip.samples, startSample);
        source.Play();
    }

    public void PlayBackingTrack(AudioClip clip,
        float startTime = 0f)
    {
        PlaySound(backingTrack, clip, startTime);
    }

    public void PlayKeysound(AudioClip clip, bool hiddenLane,
        float startTime = 0f)
    {
        AudioSource[] sources = hiddenLane ?
            hiddenLanes : playableLanes;

        AudioSource sourceWithLeastRemainingTime = null;
        float leastRemainingTime = float.MaxValue;
        foreach (AudioSource s in sources)
        {
            if (!s.isPlaying)
            {
                PlaySound(s, clip, startTime);
                return;
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
        PlaySound(sourceWithLeastRemainingTime, clip, startTime);
    }

    public void StopAll()
    {
        backingTrack.Stop();
        foreach (AudioSource s in playableLanes) s.Stop();
        foreach (AudioSource s in hiddenLanes) s.Stop();
    }
}
