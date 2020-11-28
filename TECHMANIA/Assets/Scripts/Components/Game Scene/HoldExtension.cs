using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoldExtension : MonoBehaviour
{
    public RectTransform durationTrail;
    public RectTransform durationTrailEnd;
    public RectTransform ongoingTrail;
    public RectTransform ongoingTrailEnd;

    private Scan scanRef;
    private Scanline scanlineRef;

    public void Initialize(Scan scanRef, Scanline scanlineRef, 
        HoldNote holdNote)
    {
        this.scanRef = scanRef;
        this.scanlineRef = scanlineRef;

        float startX = GetComponent<RectTransform>()
            .anchoredPosition.x;
        float endX = scanRef.FloatPulseToXPosition(
            holdNote.pulse + holdNote.duration,
            extendOutOfBoundPosition: true);
        float width = Mathf.Abs(startX - endX);

        durationTrail.sizeDelta = new Vector2(width,
            durationTrail.sizeDelta.y);
        if (endX < startX)
        {
            durationTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
            ongoingTrail.localRotation =
                Quaternion.Euler(0f, 0f, 180f);
        }
        ongoingTrail.sizeDelta = new Vector2(0f,
            ongoingTrail.sizeDelta.y);
    }

    public void Activate()
    {
        // TODO: become 60% transparent
    }

    public void Prepare()
    {
        // TODO: become opaque
    }
}
