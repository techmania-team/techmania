using MoonSharp.Interpreter;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

// When playing a sound via FMOD, it returns a channel. We wrap
// around that channel in attempt to imitate a Unity AudioSource.
[MoonSharpUserData]
public class FmodChannelWrap
{
    public FMOD.Channel channel;  // internal handle, copyable
    public FmodChannelWrap(FMOD.Channel channel)
    {
        this.channel = channel;
    }

    // This field retains the name "clip" for backwards compatibility.
    // "sound" is the more proper name.
    public FmodSoundWrap clip
    {
        get
        {
            FMOD.Sound sound;
            FmodManager.EnsureOk(channel.getCurrentSound(out sound));
            return new FmodSoundWrap(sound);
        }
        set
        {
            // We can't overwrite the sound in a channel, so
            // create a new one.
            FMOD.ChannelGroup channelGroup;
            FmodManager.EnsureOk(channel.getChannelGroup(
                out channelGroup));
            FmodManager.EnsureOk(FmodManager.system.playSound(
                value.sound, channelGroup, isPlaying, out channel));
        }
    }
    public FmodSoundWrap sound => clip;

    public bool isPlaying
    {
        get
        {
            bool value;
            FmodManager.EnsureOk(channel.isPlaying(out value));
            return value;
        }
    }

    public bool loop
    {
        get
        {
            FMOD.MODE mode;
            FmodManager.EnsureOk(channel.getMode(out mode));
            return (mode & FMOD.MODE.LOOP_NORMAL) > 0;
        }
        set
        {
            FmodManager.EnsureOk(channel.setMode(
                value ? FMOD.MODE.LOOP_NORMAL : FMOD.MODE.LOOP_OFF));
        }
    }

    // It's too much work to calculate pan from a mix matrix,
    // so we simply cache the value for getter.
    private float cachedPanStereo = 0f;
    public float panStereo
    {
        get => cachedPanStereo;
        set
        {
            cachedPanStereo = value;
            FmodManager.EnsureOk(channel.setPan(value));
        }
    }

    public float pitch
    {
        get
        {
            float value;
            FmodManager.EnsureOk(channel.getPitch(out value));
            return value;
        }
        set
        {
            FmodManager.EnsureOk(channel.setPitch(value));
        }
    }

    public float time
    {
        get
        {
            uint position;
            FmodManager.EnsureOk(channel.getPosition(out position,
                FMOD.TIMEUNIT.MS));
            return position * 0.001f;
        }
        set
        {
            FmodManager.EnsureOk(channel.setPosition(
                (uint)(value * 1000f), FMOD.TIMEUNIT.MS));
        }
    }

    public int timeSamples
    {
        get
        {
            uint position;
            FmodManager.EnsureOk(channel.getPosition(out position,
                FMOD.TIMEUNIT.PCM));
            return (int)position;
        }
        set
        {
            FmodManager.EnsureOk(channel.setPosition(
                (uint)value, FMOD.TIMEUNIT.PCM));
        }
    }

    public float volume
    {
        get
        {
            float value;
            FmodManager.EnsureOk(channel.getVolume(out value));
            return value;
        }
        set
        {
            FmodManager.EnsureOk(channel.setVolume(value));
        }
    }

    public void Pause()
    {
        // TODO: does this unpause on a repeated call?
        FmodManager.EnsureOk(channel.setPaused(true));
    }

    public void Play()
    {
        FmodManager.EnsureOk(channel.setPaused(false));
    }

    public void Stop()
    {
        FmodManager.EnsureOk(channel.stop());
    }

    public void UnPause()
    {
        FmodManager.EnsureOk(channel.setPaused(true));
    }

    // This callback must be static or FMOD will crash Unity hard.
    private static FMOD.RESULT InternalCallback(
        IntPtr channelControl,
        FMOD.CHANNELCONTROL_TYPE channelControlType,
        FMOD.CHANNELCONTROL_CALLBACK_TYPE callbackType,
        IntPtr commandData1, IntPtr commandData2)
    {
        if (channelControlType != FMOD.CHANNELCONTROL_TYPE.CHANNEL ||
            callbackType != FMOD.CHANNELCONTROL_CALLBACK_TYPE.END)
        {
            return FMOD.RESULT.OK;
        }

        // Retrieve the FmodChannelWrap object from userdata.
        FMOD.Channel internalChannel = new FMOD.Channel(channelControl);
        IntPtr pointer;
        FmodManager.EnsureOk(internalChannel.getUserData(out pointer));
        GCHandle handle = GCHandle.FromIntPtr(pointer);
        FmodChannelWrap channel = handle.Target as FmodChannelWrap;

        // Now we can finally call the callback.
        channel.soundEndCallback();
        return FMOD.RESULT.OK;
    }

    private Action soundEndCallback;
    public void SetSoundEndCallback(Action callback)
    {
        // Store the FmodChannelWrap's pointer inside the channel,
        // so we can retrieve it later from the static callback.
        GCHandle handle = GCHandle.Alloc(this);
        IntPtr pointer = GCHandle.ToIntPtr(handle);
        FmodManager.EnsureOk(channel.setUserData(pointer));

        // Store and set callback.
        this.soundEndCallback = callback;
        FmodManager.EnsureOk(channel.setCallback(InternalCallback));
    }
}
