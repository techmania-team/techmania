using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeverOverlay : MonoBehaviour
{
    private RectTransform rect;
    private Image image;  // May be enabled/disabled by NoteAppearance.

    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();

        float scale = GlobalResource.vfxSkin.feverOverlay.scale;
        rect.localScale = new Vector3(scale, scale, 1f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Game.feverState == Game.FeverState.Active)
        {
            Sprite sprite = GlobalResource.vfxSkin.feverOverlay
                .GetSpriteForTime(Game.Time, loop: true);
            image.sprite = sprite;

            float alpha = 1f;
            if (Game.feverAmount * 6f < 1f)
            {
                alpha = Game.feverAmount * 6f;
            }
            image.color = new Color(1f, 1f, 1f, alpha);
        }
        else
        {
            image.color = Color.clear;
        }
    }
}
