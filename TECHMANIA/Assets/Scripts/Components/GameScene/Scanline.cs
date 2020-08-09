using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanline : MonoBehaviour
{
    [HideInInspector]
    public int scanNumber;

    private int pulsesPerScan;
    private Scan scanRef;

    public void Initialize(Scan scanRef, float height)
    {
        this.scanRef = scanRef;

        RectTransform rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(-height, 0f);
        rect.sizeDelta = new Vector2(height, height);

        pulsesPerScan = Pattern.pulsesPerBeat *
            GameSetup.pattern.patternMetadata.bps;

        Game.FloatPulseChanged += OnFloatPulseChanged;
    }

    private void OnDestroy()
    {
        Game.FloatPulseChanged -= OnFloatPulseChanged;
    }

    private void OnFloatPulseChanged(float pulse)
    {
        float x = scanRef.FloatPulseToXPosition(pulse);
        GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0f);
    }
}
