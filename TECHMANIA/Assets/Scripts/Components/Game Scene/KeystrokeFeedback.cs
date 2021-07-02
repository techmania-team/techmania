using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class KeystrokeFeedback : MonoBehaviour
{
    private bool isPlaying;
    private float startTime;
    private Image image;

    // Start is called before the first frame update
    void Start()
    {
        isPlaying = false;
        image = GetComponent<Image>();
        image.color = Color.clear;
    }

    // Update is called once per frame
    void Update()
    {
        if (!isPlaying) return;

        image.sprite = GlobalResource.gameUiSkin.keystrokeFeedback
            .GetSpriteForTime(
            Game.Time - startTime, loop: true);
    }

    public void Play()
    {
        isPlaying = true;
        startTime = Game.Time;
        image.sprite = GlobalResource.gameUiSkin.keystrokeFeedback
            .sprites[0];
        image.color = Color.white;
    }

    public void Stop()
    {
        isPlaying = false;
        image.color = Color.clear;
    }
}
