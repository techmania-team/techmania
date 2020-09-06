using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSfx : MonoBehaviour
{
    public AudioClip select;
    public AudioClip click;
    public AudioClip back;
    public AudioClip gameStart;
    public AudioClip pause;

    public static MenuSfx instance { get; private set; }

    private AudioSource source;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        source = GetComponent<AudioSource>();
    }

    public void PlaySound(AudioClip clip)
    {
        if (clip == null) return;
        source.clip = clip;
        source.Play();
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
