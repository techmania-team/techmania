using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectMask2D))]
public class ScrollingText : MonoBehaviour
{
    public enum Direction
    {
        Horizontal,
        Vertical
    }
    public Direction direction;
    [Range(0f, 1f)]
    public float restingPosition;

    private RectTransform rect;
    private RectTransform innerRect;
    private float maskSize;
    private float contentSize;

    void OnEnable()
    {
        SetUp();
    }

    public void SetUp()
    {
        rect = GetComponent<RectTransform>();
        innerRect = rect.GetChild(0).GetComponent<RectTransform>();
        TextMeshProUGUI[] allTexts =
            GetComponentsInChildren<TextMeshProUGUI>();

        contentSize = 0f;
        switch (direction)
        {
            case Direction.Horizontal:
                maskSize = rect.rect.width;
                foreach (TextMeshProUGUI t in allTexts)
                {
                    contentSize += t.preferredWidth;
                }
                break;
            case Direction.Vertical:
                maskSize = rect.rect.height;
                foreach (TextMeshProUGUI t in allTexts)
                {
                    contentSize += t.preferredHeight;
                }
                break;
        }

        StopAllCoroutines();
        if (contentSize > maskSize)
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(Scroll());
            }
        }
        else
        {
            ScrollTo(restingPosition);
        }
    }

    // Convenience method to reset the text within, and then
    // call SetUp.
    public void SetUp(string text)
    {
        GetComponentInChildren<TextMeshProUGUI>().text = text;
        SetUp();
    }

    private void ScrollTo(float t)
    {
        switch (direction)
        {
            case Direction.Horizontal:
                innerRect.anchorMin = new Vector2(t, 0f);
                innerRect.anchorMax = new Vector2(t, 1f);
                innerRect.pivot = new Vector2(t, 0.5f);
                break;
            case Direction.Vertical:
                innerRect.anchorMin = new Vector2(0f, 1f - t);
                innerRect.anchorMax = new Vector2(1f, 1f - t);
                innerRect.pivot = new Vector2(0.5f, 1f - t);
                break;
        }
    }

    private IEnumerator Scroll()
    {
        const float kScrollTime = 2f;
        const float kWaitTime = 2f;

        while (true)
        {
            ScrollTo(0f);
            yield return new WaitForSeconds(kWaitTime);
            for (float time = 0;
                time < kScrollTime; time += Time.deltaTime)
            {
                float progress = time / kScrollTime;
                ScrollTo(progress);
                yield return null;
            }
            ScrollTo(1f);
            yield return new WaitForSeconds(kWaitTime);
            for (float time = 0;
                time < kScrollTime; time += Time.deltaTime)
            {
                float progress = 1f - time / kScrollTime;
                ScrollTo(progress);
                yield return null;
            }
        }
    }
}
