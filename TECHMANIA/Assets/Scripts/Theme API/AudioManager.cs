using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;
using UnityEngine.Audio;

namespace ThemeApi
{
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
        public AudioSource Play(AudioClip clip, string channel,
            float startTime = 0f,
            int volumePercent = 100, int panPercent = 0)
        {
            if (clip == null)
            {
                Debug.LogError("Attempting to play a null audio clip.");
                return null;
            }
            switch (System.Enum.Parse<Channel>(channel))
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