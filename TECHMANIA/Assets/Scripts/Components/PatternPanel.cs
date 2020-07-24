using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Note to self: (0, 0) is bottom-left.
public class PatternPanel : MonoBehaviour
{
    public ScrollRect scrollRect;
    public Transform patternContainer;
    public GameObject markerTemplate;
    public GameObject lineTemplate;
    public GameObject dottedLineTemplate;
    public GameObject laneDividers;

    private int numScans;
    private int zoom;

    private void SpawnLine(float x, GameObject template)
    {
        GameObject line = Instantiate(template, patternContainer);
        line.SetActive(true);
        RectTransform rect = line.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, 0.5f);
    }

    private void SpawnMarker(float x, float y, string text)
    {
        GameObject marker = Instantiate(markerTemplate, patternContainer);
        marker.SetActive(true);
        marker.GetComponentInChildren<Text>().text = text;
        RectTransform rect = marker.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, y);
    }

    private void DrawMarkersAndLines()
    {
        for (int i = 0; i < patternContainer.childCount; i++)
        {
            Transform t = patternContainer.GetChild(i);
            if (t == markerTemplate.transform) continue;
            if (t == lineTemplate.transform) continue;
            if (t == dottedLineTemplate.transform) continue;
            if (t == laneDividers.transform) continue;
            Destroy(t.gameObject);
        }

        Pattern pattern = Navigation.GetCurrentPattern();
        float scanLength = 1500f * 100 / zoom;
        RectTransform containerRect = patternContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector3(
            scanLength * numScans,
            containerRect.sizeDelta.y);

        const float scanMarkerY = 0f;
        const float beatMarkerY = -20f;
        const float timeMarkerY = -40f;

        // Draw scan dividers
        for (int i = 0; i <= numScans; i++)
        {
            float x = scanLength * i;

            SpawnLine(x, lineTemplate);

            SpawnMarker(x, scanMarkerY, $"Scan {i}");
        }

        // Draw beat dividers
        int bps = pattern.patternMetadata.bps;
        double secondsPerBeat = 60.0 / pattern.patternMetadata.initBpm;
        for (int i = 0; i < numScans * bps ; i++)
        {
            float x = scanLength * i / bps;

            float time = (float)(secondsPerBeat * i +
                pattern.patternMetadata.firstBeatOffset);
            int minute = Mathf.FloorToInt(time / 60f);
            time -= minute * 60f;
            int second = Mathf.FloorToInt(time);
            time -= second;
            int milliSecond = Mathf.FloorToInt(time * 1000f);

            SpawnLine(x, dottedLineTemplate);

            SpawnMarker(x, beatMarkerY, $"Beat {i}");
            SpawnMarker(x, timeMarkerY,
                $"{minute}:{second:D2}.{milliSecond:D3}");
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        numScans = 4;
        zoom = 100;
        DrawMarkersAndLines();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.mouseScrollDelta.y != 0)
        {
            if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
            {
                // Adjust zoom
                zoom += Mathf.FloorToInt(Input.mouseScrollDelta.y * -10f);
                zoom = Mathf.Clamp(zoom, 10, 200);
                float horizontal = scrollRect.horizontalNormalizedPosition;

                DrawMarkersAndLines();
                scrollRect.horizontalNormalizedPosition = horizontal;
            }
            else
            {
                // Scroll
                scrollRect.horizontalNormalizedPosition +=
                    Input.mouseScrollDelta.y * 0.05f;
            }
        }
    }

    public void ScrollPositionChanged()
    {
        // Debug.Log("Horizontal: " + scrollRect.horizontalNormalizedPosition);
    }
}
