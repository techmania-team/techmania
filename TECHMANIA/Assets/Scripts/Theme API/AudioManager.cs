using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MoonSharp.Interpreter;

namespace ThemeApi
{
    [MoonSharpUserData]
    public class AudioManager
    {
        private AudioSourceManager manager;
        [MoonSharpHidden]
        public AudioManager()
        {
            manager = Object.FindObjectOfType<AudioSourceManager>();
        }

        public enum Channel
        {
            Music,
            Keysound,
            HiddenKeysound,
            SFX
        }

        // name: file name of audio clip in theme
        // channel: one of Channel enums
        // volumePercent: [0, 100]
        // panPercent: [-100, 100]
        public AudioSource Play(string name, string channel,
            int volumePercent = 100, int panPercent = 0)
        {
            AudioClip clip = GlobalResource.GetThemeContent
                <AudioClip>(name);
            if (clip == null)
            {
                throw new System.Exception($"Audio clip {name} is not found.");
            }

            switch (System.Enum.Parse<Channel>(channel))
            {
                case Channel.Music:
                    return manager.PlayBackingTrack(clip,
                        volumePercent, panPercent);
                case Channel.Keysound:
                    return manager.PlayKeysound(clip,
                        hiddenLane: false,
                        startTime: 0f, volumePercent, panPercent);
                case Channel.HiddenKeysound:
                    return manager.PlayKeysound(clip,
                        hiddenLane: true,
                        startTime: 0f, volumePercent, panPercent);
                case Channel.SFX:
                    return manager.PlaySfx(clip,
                        volumePercent, panPercent);
                default:
                    throw new System.Exception($"Unknown audio channel: {channel}.");
            }
        }
    }
}