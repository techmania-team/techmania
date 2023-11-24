using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

// A basic wrapper around FMOD.
public class FmodManager
{
    public static FmodManager instance { get; private set; }
    static FmodManager()
    {
        instance = new FmodManager();
    }

    public static FMOD.System system { get; private set; }

    private Dictionary<AudioClip, FMOD.Sound> clipToSound;

    #region Channel groups
    private FMOD.ChannelGroup masterGroup;
    private FMOD.ChannelGroup musicGroup;
    private FMOD.ChannelGroup keysoundGroup;
    private FMOD.ChannelGroup sfxGroup;

    public enum ChannelGroupType
    {
        Master,
        Music,
        Keysound,
        SFX
    }

    private FMOD.ChannelGroup GetGroup(ChannelGroupType type)
    {
        return type switch
        {
            ChannelGroupType.Master => masterGroup,
            ChannelGroupType.Music => musicGroup,
            ChannelGroupType.Keysound => keysoundGroup,
            ChannelGroupType.SFX => sfxGroup,
            _ => throw new NotImplementedException()
        };
    }
    #endregion

    public void Initialize()
    {
        system = FMODUnity.RuntimeManager.CoreSystem;
        clipToSound = new Dictionary<AudioClip, FMOD.Sound>();

        // Create channel groups.
        EnsureOk(system.getMasterChannelGroup(out masterGroup));
        EnsureOk(system.createChannelGroup("Music", out musicGroup));
        EnsureOk(system.createChannelGroup("Keysound",
            out keysoundGroup));
        EnsureOk(system.createChannelGroup("SFX", out sfxGroup));
    }

    #region Prepare and play sounds
    public void ConvertAndCacheAudioClip(AudioClip clip)
    {
        FMOD.Sound sound = CreateSoundFromAudioClip(clip);
        clipToSound.Add(clip, sound);
    }

    public FMOD.Sound GetSoundFromAudioClip(AudioClip clip)
    {
        // Intentionally let it throw exceptions if the clip is
        // not cached earlier.
        return clipToSound[clip];
    }

    public FMOD.Channel Play(AudioClip clip, ChannelGroupType group,
        bool paused = true)
    {
        return Play(GetSoundFromAudioClip(clip), group, true);
    }

    public FMOD.Channel Play(FMOD.Sound sound, ChannelGroupType group,
        bool paused = true)
    {
        FMOD.Channel channel;
        EnsureOk(system.playSound(sound, GetGroup(group),
            paused, out channel));
        return channel;
    }
    #endregion

    #region Group-level control
    public void PauseAll()
    {
        EnsureOk(GetGroup(ChannelGroupType.Music).setPaused(true));
        EnsureOk(GetGroup(ChannelGroupType.Keysound).setPaused(true));
    }

    public void UnpauseAll()
    {
        EnsureOk(GetGroup(ChannelGroupType.Music).setPaused(false));
        EnsureOk(GetGroup(ChannelGroupType.Keysound).setPaused(false));
    }

    public void StopAll()
    {
        EnsureOk(GetGroup(ChannelGroupType.Music).stop());
        EnsureOk(GetGroup(ChannelGroupType.Keysound).stop());
    }

    public void SetSpeed(float speed)
    {
        EnsureOk(GetGroup(ChannelGroupType.Music).setPitch(speed));
        EnsureOk(GetGroup(ChannelGroupType.Keysound).setPitch(speed));
    }

    public bool AnySoundPlaying()
    {
        int numChannels;
        EnsureOk(masterGroup.getNumChannels(out numChannels));

        for (int i = 0; i < numChannels; i++)
        {
            FMOD.Channel channel;
            EnsureOk(masterGroup.getChannel(i, out channel));
            bool paused;
            EnsureOk(channel.getPaused(out paused));
            if (!paused) return true;
        }

        return false;
    }

    public void SetVolume(ChannelGroupType type, float volume)
    {
        FMOD.ChannelGroup group = GetGroup(type);
        EnsureOk(group.setVolume(volume));
    }
    #endregion

    #region Utilities
    public static void EnsureOk(FMOD.RESULT result)
    {
        if (result != FMOD.RESULT.OK)
        {
            throw new Exception(result.ToString());
        }
    }

    // https://qa.fmod.com/t/load-an-audioclip-as-fmod-sound/11741/2
    public static FMOD.Sound CreateSoundFromAudioClip(
        AudioClip audioClip)
    {
        // Load samples from audio clip.
        // If Unity Audio is disabled in project settings,
        // the samples returned from GetData will be all 0, so we
        // can't disable it.
        var samplesSize = audioClip.samples * audioClip.channels;
        var samples = new float[samplesSize];
        audioClip.GetData(samples, 0);  
        var bytesLength = (uint)(samplesSize * sizeof(float));

        // Some extra information when creating a sound.
        var soundInfo = new FMOD.CREATESOUNDEXINFO();
        soundInfo.cbsize = Marshal.SizeOf(typeof(
            FMOD.CREATESOUNDEXINFO));
        soundInfo.length = bytesLength;
        soundInfo.format = FMOD.SOUND_FORMAT.PCMFLOAT;
        soundInfo.defaultfrequency = audioClip.frequency;
        soundInfo.numchannels = audioClip.channels;

        // Open a user-created static sample.
        FMOD.Sound sound;
        EnsureOk(FMODUnity.RuntimeManager.CoreSystem.createSound(
            "", FMOD.MODE.OPENUSER, ref soundInfo, out sound));

        // `lock` gives access to the sample data for direct
        // manipulation.
        // `ptr2` and `len2` are for when bytesLength exceeds the
        // sample buffer, which shouldn't be the case, but we handle
        // it anyway.
        IntPtr ptr1, ptr2;
        uint len1, len2;
        EnsureOk(sound.@lock(0, bytesLength,
            out ptr1, out ptr2, out len1, out len2));
        var samplesLength = (int)(len1 / sizeof(float));
        Marshal.Copy(samples, 0, ptr1, samplesLength);
        if (len2 > 0)
        {
            Marshal.Copy(samples, samplesLength,
                ptr2, (int)(len2 / sizeof(float)));
        }

        // Submit the sample data back to the sound object.
        EnsureOk(sound.unlock(ptr1, ptr2, len1, len2));

        // Return sound.
        EnsureOk(sound.setMode(FMOD.MODE.LOOP_OFF | FMOD.MODE._2D));
        return sound;
    }
    #endregion
}