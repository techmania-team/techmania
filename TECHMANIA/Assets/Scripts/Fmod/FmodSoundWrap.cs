using MoonSharp.Interpreter;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Imitates a Unity AudioClip.
[MoonSharpUserData]
public class FmodSoundWrap
{
    [MoonSharpHidden]
    public FMOD.Sound sound;  // internal handle, copyable
    [MoonSharpHidden]
    public FmodSoundWrap(FMOD.Sound sound)
    {
        this.sound = sound;
    }

    public int channels
    {
        get
        {
            FMOD.SOUND_TYPE type;
            FMOD.SOUND_FORMAT format;
            int numChannels;
            int bits;
            FmodManager.EnsureOk(sound.getFormat(out type, out format,
                out numChannels, out bits));
            return numChannels;
        }
    }

    public int frequency
    {
        get
        {
            float defaultFrequency;
            int priority;
            FmodManager.EnsureOk(sound.getDefaults(out defaultFrequency,
                out priority));
            return (int)defaultFrequency;
        }
    }

    public float length
    {
        get
        {
            uint value;
            FmodManager.EnsureOk(sound.getLength(out value,
                FMOD.TIMEUNIT.MS));
            return value * 0.001f;
        }
    }

    public int samples
    {
        get
        {
            uint value;
            FmodManager.EnsureOk(sound.getLength(out value,
                FMOD.TIMEUNIT.PCM));
            return (int)value;
        }
    }

    [MoonSharpHidden]
    // Themes may want to use this on preview tracks.
    public void Release()
    {
        sound.release();
    }

    public bool Equals(FmodSoundWrap other)
    {
        return sound.handle == other.sound.handle;
    }
}
