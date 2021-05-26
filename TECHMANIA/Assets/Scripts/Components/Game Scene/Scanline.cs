using System.CodeDom;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Scanline : MonoBehaviour
{
    [HideInInspector]
    public int scanNumber;
    public GameObject autoPlayIndicator;

    private Scan scanRef;

    public void Initialize(Scan scanRef, float height)
    {
        this.scanRef = scanRef;

        RectTransform rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(-height, 0f);
        rect.sizeDelta = new Vector2(height, height);

        autoPlayIndicator.SetActive(
            Options.instance.modifiers.mode ==
            Modifiers.Mode.AutoPlay);
    }

    private void Update()
    {
        float x = scanRef.FloatPulseToXPosition(Game.FloatPulse);
        GetComponent<RectTransform>().anchoredPosition =
            new Vector2(x, 0f);
    }
}
