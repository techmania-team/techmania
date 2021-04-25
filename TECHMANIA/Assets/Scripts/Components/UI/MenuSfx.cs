using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSfx : MonoBehaviour
{
    public AudioSourceManager audioSourceManager;
    public AudioClip select;
    public AudioClip click;
    public AudioClip back;
    public AudioClip gameStart;
    public AudioClip pause;

    public static MenuSfx instance { get; private set; }

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        audioSourceManager.PlaySfx(clip);
    }

    public void PlaySelectSound()
    {
        PlaySound(select);
    }

    public void PlayClickSound()
    {
        PlaySound(click);
    }

    public void PlayBackSound()
    {
        PlaySound(back);
    }

    public void PlayGameStartSound()
    {
        PlaySound(gameStart);
    }

    public void PlayPauseSound()
    {
        PlaySound(pause);
    }
}
