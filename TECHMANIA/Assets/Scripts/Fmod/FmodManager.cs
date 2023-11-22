using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class FmodManager : MonoBehaviour
{
    public static FmodManager instance { get; private set; }
    static FmodManager()
    {
        instance = new FmodManager();
    }

    public static FMOD.System system { get; private set; }

    #region Channel groups
    private FMOD.ChannelGroup masterGroup;
    private FMOD.ChannelGroup musicGroup;
    private FMOD.ChannelGroup keysoundGroup;
    private FMOD.ChannelGroup sfxGroup;
    #endregion

    public void Initialize()
    {
        system = FMODUnity.RuntimeManager.CoreSystem;

        // Create channel groups.
        EnsureOk(system.getMasterChannelGroup(out masterGroup));
        EnsureOk(system.createChannelGroup("Music", out musicGroup));
        EnsureOk(system.createChannelGroup("Keysound",
            out keysoundGroup));
        EnsureOk(system.createChannelGroup("SFX", out sfxGroup));
    }

    #region Utilities
    public static void EnsureOk(FMOD.RESULT result)
    {
        if (result != FMOD.RESULT.OK)
        {
            throw new Exception(result.ToString());
        }
    }

    // https://qa.fmod.com/t/load-an-audioclip-as-fmod-sound/11741/2
    private static FMOD.Sound CreateSoundFromAudioClip(
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
