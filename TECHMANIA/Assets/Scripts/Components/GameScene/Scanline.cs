using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanline : MonoBehaviour
{
    [HideInInspector]
    public int scanNumber;

    private float screenWidth;
    private int pulsesPerScan;
    

    public void Initialize(float screenWidth, float height)
    {
        this.screenWidth = screenWidth;

        RectTransform rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(
            (scanNumber % 2 == 0) ? -100f : screenWidth + 100f,
            0f);
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
        float relativeNormalizedScan = pulse / pulsesPerScan - scanNumber;
        float normalizedX = Mathf.LerpUnclamped(
            Scan.kSpaceBeforeScan, 1f - Scan.kSpaceAfterScan,
            relativeNormalizedScan);
        if (scanNumber % 2 != 0)
        {
            normalizedX = 1f - normalizedX;
        }
        float x = normalizedX * screenWidth;

        GetComponent<RectTransform>().anchoredPosition = new Vector2(x, 0f);
    }
}
