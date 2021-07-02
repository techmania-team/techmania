using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TouchClickFeedback : MonoBehaviour
{
    private float spawnTime;
    private Image image;

    // Start is called before the first frame update
    void Start()
    {
        spawnTime = Time.time;
        image = GetComponent<Image>();
        image.sprite = GlobalResource.gameUiSkin.touchClickFeedback
            .sprites[0];
    }

    // Update is called once per frame
    void Update()
    {
        image.sprite = GlobalResource.gameUiSkin.touchClickFeedback
            .GetSpriteForTime(Time.time - spawnTime,
            loop: true);
    }
}
