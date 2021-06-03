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
    private CanvasGroup canvasGroup;

    public void Initialize(Scan scanRef, float height)
    {
        this.scanRef = scanRef;
        canvasGroup = GetComponent<CanvasGroup>();

        RectTransform rect = GetComponent<RectTransform>();
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.anchoredPosition = new Vector2(-height, 0f);
        rect.sizeDelta = new Vector2(height, height);

        autoPlayIndicator.SetActive(
            Modifiers.instance.mode ==
            Modifiers.Mode.AutoPlay);
    }

    private void Update()
    {
        float x = scanRef.FloatPulseToXPosition(Game.FloatPulse);
        GetComponent<RectTransform>().anchoredPosition =
            new Vector2(x, 0f);

        float alpha;
        switch (Modifiers.instance.scanlineOpacity)
        {
            case Modifiers.ScanlineOpacity.Normal:
                alpha = 1f;
                break;
            case Modifiers.ScanlineOpacity.Blind:
                alpha = 0f;
                break;
            default:
                float scan = Game.FloatPulse / Game.PulsesPerScan;
                if (Modifiers.instance.scanlineOpacity ==
                    Modifiers.ScanlineOpacity.Blink)
                {
                    // 4 periods per scan.
                    scan *= 4f;
                    scan -= Mathf.Floor(scan);
                    if (scan < 0.25f)
                    {
                        alpha = Mathf.InverseLerp(0f, 0.25f, scan);
                    }
                    else if (scan < 0.5f)
                    {
                        alpha = Mathf.InverseLerp(0.5f, 0.25f, scan);
                    }
                    else
                    {
                        alpha = 0f;
                    }
                }
                else
                {
                    // 2 periods per scan.
                    scan *= 2f;
                    scan -= Mathf.Floor(scan);
                    if (scan < 0.125f)
                    {
                        alpha = Mathf.InverseLerp(0f, 0.125f, scan);
                    }
                    else if (scan < 0.25f)
                    {
                        alpha = Mathf.InverseLerp(
                            0.25f, 0.125f, scan);
                    }
                    else
                    {
                        alpha = 0f;
                    }
                }
                break;
        }
        canvasGroup.alpha = Mathf.SmoothStep(0f, 1f, alpha);
    }
}
