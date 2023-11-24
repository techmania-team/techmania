using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSfx : MonoBehaviour
{
    public AudioSourceManager audioSourceManager;
    public AudioClip select;
    public AudioClip click;
    public AudioClip back;

    private FmodSoundWrap selectSound;
    private FmodSoundWrap clickSound;
    private FmodSoundWrap backSound;

    public static MenuSfx instance { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;

        selectSound = FmodManager.CreateSoundFromAudioClip(select);
        clickSound = FmodManager.CreateSoundFromAudioClip(click);
        backSound = FmodManager.CreateSoundFromAudioClip(back);
    }

    public void PlaySound(FmodSoundWrap sound)
    {
        if (sound == null) return;
        audioSourceManager.PlaySfx(sound);
    }

    public void PlaySelectSound()
    {
        PlaySound(selectSound);
    }

    public void PlayClickSound()
    {
        PlaySound(clickSound);
    }

    public void PlayBackSound()
    {
        PlaySound(backSound);
    }
}
