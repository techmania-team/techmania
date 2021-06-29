using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ApproachOverlay : MonoBehaviour
{
    private RectTransform rect;
    private Image image;  // May be enabled/disabled by NoteAppearance.
    private float noteAlpha;
    private float correctScan;

    private const float kOverlayStart = -0.5f;
    private const float kOverlayEnd = 0f;

    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        image = GetComponent<Image>();
        image.color = Color.clear;

        correctScan = (float)GetComponentInParent<NoteObject>()
            .note.pulse / Game.PulsesPerScan;
    }

    public void SetNoteAlpha(float bound)
    {
        noteAlpha = bound;
    }

    // Update is called once per frame
    void Update()
    {
        float distance = Game.FloatScan - correctScan;
        if (distance < kOverlayStart || distance > kOverlayEnd)
        {
            image.color = Color.clear;
        }
        else
        {
            float t = Mathf.InverseLerp(
                kOverlayStart, kOverlayEnd, distance);
            image.sprite = GlobalResource.gameUiSkin.approachOverlay
                .GetSpriteAtFloatIndex(t);
            image.color = new Color(1f, 1f, 1f, noteAlpha);
        }
    }
}
