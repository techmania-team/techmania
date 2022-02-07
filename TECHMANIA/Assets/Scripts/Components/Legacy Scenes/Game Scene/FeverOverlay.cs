using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class FeverOverlay : MonoBehaviour
{
    private RectTransform rect;
    private Image image;  // May be enabled/disabled by NoteAppearance.
    private float noteAlpha;

    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.color = Color.clear;
    }

    public void SetNoteAlpha(float bound)
    {
        noteAlpha = bound;
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
            alpha *= noteAlpha;
            image.color = new Color(1f, 1f, 1f, alpha);
        }
        else
        {
            image.color = Color.clear;
        }
    }
}
