using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.Audio;

namespace ThemeApi
{
    // TODO: merge AudioManager and AudioSourceManager into one.
    // There's really no need for 2 layers of wrappers.
    [MoonSharpUserData]
    public class AudioManager
    {
        private AudioSourceManager manager;
        [MoonSharpHidden]
        public AudioMixer mixer { get; private set; }

        [MoonSharpHidden]
        public AudioManager()
        {
            manager = Object.FindObjectOfType<AudioSourceManager>();
            mixer = manager.audioMixer;
        }

        public enum Channel
        {
            Music,
            Keysound,
            HiddenKeysound,
            SFX
        }

        // channel: one of Channel enums
        // volumePercent: [0, 100]
        // panPercent: [-100, 100]
        public AudioSource Play(AudioClip clip, Channel channel,
            float startTime = 0f,
            int volumePercent = 100, int panPercent = 0)
        {
            if (clip == null)
            {
                Debug.LogError("Attempting to play a null audio clip.");
                return null;
            }
            switch (channel)
            {
                case Channel.Music:
                    return manager.PlayBackingTrack(clip, startTime,
                        volumePercent, panPercent);
                case Channel.Keysound:
                    return manager.PlayKeysound(clip,
                        hiddenLane: false,
                        startTime, volumePercent, panPercent);
                case Channel.HiddenKeysound:
                    return manager.PlayKeysound(clip,
                        hiddenLane: true,
                        startTime, volumePercent, panPercent);
                case Channel.SFX:
                    return manager.PlaySfx(clip,
                        volumePercent, panPercent);
                default:
                    throw new System.Exception($"Unknown audio channel: {channel}.");
            }
        }
    }
}