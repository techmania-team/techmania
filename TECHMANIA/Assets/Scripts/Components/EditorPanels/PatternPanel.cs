using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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

    public static float ScanLength
    {
        get
        {
            return 1500f * zoom * 0.01f;
        }
    }

    private int numScans;
    private static int zoom;

    public static event UnityAction RepositionNeeded;

    private void SpawnLine(int scan, GameObject template)
    {
        GameObject line = Instantiate(template, patternContainer);
        line.SetActive(true);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.Line;
        element.scan = scan;
    }

    private void SpawnDottedLine(int beat, GameObject template)
    {
        GameObject line = Instantiate(template, patternContainer);
        line.SetActive(true);

        EditorElement element = line.GetComponent<EditorElement>();
        element.type = EditorElement.Type.DottedLine;
        element.beat = beat;
    }
    
    private void SpawnScanBasedMarker(EditorElement.Type type, int scan, string text)
    {
        GameObject marker = Instantiate(markerTemplate, patternContainer);
        marker.SetActive(true);
        marker.GetComponentInChildren<Text>().text = text;
        marker.GetComponent<EditorElement>().type = type;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.scan = scan;
    }

    private void SpawnBeatBasedMarker(EditorElement.Type type, int beat, string text)
    {
        GameObject marker = Instantiate(markerTemplate, patternContainer);
        marker.SetActive(true);
        marker.GetComponentInChildren<Text>().text = text;
        marker.GetComponent<EditorElement>().type = type;

        EditorElement element = marker.GetComponent<EditorElement>();
        element.type = type;
        element.beat = beat;
    }

    private void ResizeContainer()
    {
        RectTransform containerRect = patternContainer.GetComponent<RectTransform>();
        containerRect.sizeDelta = new Vector3(
            ScanLength * numScans,
            containerRect.sizeDelta.y);
    }

    private void SpawnMarkersAndLines()
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

        // Scan based stuff
        for (int scan = 0; scan <= numScans; scan++)
        {
            SpawnLine(scan, lineTemplate);
            SpawnScanBasedMarker(EditorElement.Type.ScanMarker,
                scan, $"Scan {scan}");
        }

        // Beat based stuff
        int bps = pattern.patternMetadata.bps;
        double secondsPerBeat = 60.0 / pattern.patternMetadata.initBpm;
        for (int beat = 0; beat < numScans * bps ; beat++)
        {
            float time = (float)(secondsPerBeat * beat +
                pattern.patternMetadata.firstBeatOffset);
            int minute = Mathf.FloorToInt(time / 60f);
            time -= minute * 60f;
            int second = Mathf.FloorToInt(time);
            time -= second;
            int milliSecond = Mathf.FloorToInt(time * 1000f);

            SpawnDottedLine(beat, dottedLineTemplate);
            SpawnBeatBasedMarker(EditorElement.Type.BeatMarker,
                beat, $"Beat {beat}");
            SpawnBeatBasedMarker(EditorElement.Type.TimeMarker,
                beat, $"{minute}:{second:D2}.{milliSecond:D3}");
        }
    }

    private void OnEnable()
    {
        numScans = 4;
        zoom = 100;
        ResizeContainer();
        SpawnMarkersAndLines();
        RepositionNeeded?.Invoke();
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
                zoom += Mathf.FloorToInt(Input.mouseScrollDelta.y * 10f);
                zoom = Mathf.Clamp(zoom, 10, 500);
                // Debug.Log($"zoom={zoom} ScanLength={ScanLength}");
                float horizontal = scrollRect.horizontalNormalizedPosition;

                ResizeContainer();
                RepositionNeeded?.Invoke();

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
