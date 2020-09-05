using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScrollingText : MonoBehaviour
{
    private RectTransform rect;
    private RectTransform innerRect;
    private TextMeshProUGUI text;
    private float maskWidth;
    private float textWidth;

    // Start is called before the first frame update
    void Start()
    {
        rect = GetComponent<RectTransform>();
        text = GetComponentInChildren<TextMeshProUGUI>();
        innerRect = text.GetComponent<RectTransform>();

        // The RectTransform must have a static width for this
        // line to work.
        maskWidth = rect.sizeDelta.x;
        textWidth = text.preferredWidth;

        if (textWidth > maskWidth)
        {
            StartCoroutine(Scroll());
        }
    }

    private void ScrollTo(float t)
    {
        innerRect.anchorMin = new Vector2(t, 0f);
        innerRect.anchorMax = new Vector2(t, 1f);
        innerRect.pivot = new Vector2(t, 0.5f);
    }

    private IEnumerator Scroll()
    {
        const float kScrollTime = 2f;
        const float kWaitTime = 2f;

        while (true)
        {
            ScrollTo(0f);
            yield return new WaitForSeconds(kWaitTime);
            for (float time = 0; time < kScrollTime; time += Time.deltaTime)
            {
                float progress = time / kScrollTime;
                ScrollTo(progress);
                yield return null;
            }
            ScrollTo(1f);
            yield return new WaitForSeconds(kWaitTime);
            for (float time = 0; time < kScrollTime; time += Time.deltaTime)
            {
                float progress = 1f - time / kScrollTime;
                ScrollTo(progress);
                yield return null;
            }
        }
    }
}
