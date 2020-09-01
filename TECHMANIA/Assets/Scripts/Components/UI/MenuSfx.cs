using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuSfx : MonoBehaviour
{
    public AudioClip select;
    public AudioClip click;
    public AudioClip back;
    public AudioClip gameStart;

    public static MenuSfx instance { get; private set; }

    private AudioSource source;

    // Start is called before the first frame update
    void Start()
    {
        instance = this;
        source = GetComponent<AudioSource>();
    }

    public void PlaySelectSound()
    {
        source.clip = select;
        source.Play();
    }

    public void PlayClickSound()
    {
        source.clip = click;
        source.Play();
    }

    public void PlayBackSound()
    {
        source.clip = back;
        source.Play();
    }

    public void PlayGameStartSound()
    {
        source.clip = gameStart;
        source.Play();
    }
}
