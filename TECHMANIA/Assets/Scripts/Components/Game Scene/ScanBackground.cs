using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScanBackground : MonoBehaviour
{
    public Transform beatMarkerContainer;
    public GameObject beatMarkerTemplate;
    public GameObject halfBeatMarkerTemplate;
    public List<RectTransform> lanes;
    public List<GameObject> laneDividers;

    private List<GameObject> beatMarkers;

    // Start is called before the first frame update
    void Start()
    {
        HideAllMarkers();
    }

    public void HideAllMarkers()
    {
        foreach (GameObject o in laneDividers)
        {
            o.SetActive(false);
        }
        if (beatMarkers != null)
        {
            foreach (GameObject o in beatMarkers)
            {
                Destroy(o);
            }
            beatMarkers.Clear();
        }
    }

    private void SpawnMarker(GameObject template,
        float relativePosition,
        Scan.Direction scanDirection)
    {
        float x = Mathf.Lerp(
            Ruleset.instance.scanMarginBeforeFirstBeat,
            1f - Ruleset.instance.scanMarginAfterLastBeat,
            relativePosition);
        if (scanDirection == Scan.Direction.Left)
        {
            x = 1f - x;
        }

        RectTransform marker = Instantiate(template,
            beatMarkerContainer).GetComponent<RectTransform>();
        marker.anchorMin = new Vector2(x, 0f);
        marker.anchorMax = new Vector2(x, 1f);
        marker.gameObject.SetActive(true);

        if (beatMarkers == null)
        {
            beatMarkers = new List<GameObject>();
        }
        beatMarkers.Add(marker.gameObject);
    }

    public void Initialize(Scan.Direction scanDirection,
        Scan.Position position)
    {
        float marginAbove, marginBelow;
        Ruleset.instance.GetScanMargin(
            GameSetup.pattern.patternMetadata.playableLanes,
            position, out marginAbove, out marginBelow);

        // Lanes
        float laneHeightRelative =
            (1f - marginAbove - marginBelow) /
            Game.playableLanes;
        List<float> anchors = new List<float>();
        for (int i = 0; i <= Game.playableLanes; i++)
        {
            anchors.Add(1f - marginAbove - laneHeightRelative * i);
        }
        anchors[0] = 1f;
        anchors[anchors.Count - 1] = 0f;
        for (int i = 0; i < lanes.Count; i++)
        {
            lanes[i].anchorMin = Vector2.zero;
            lanes[i].anchorMax = Vector2.zero;
        }
        for (int i = 0; i < Game.playableLanes; i++)
        {
            lanes[i].anchorMin = new Vector2(0f, anchors[i + 1]);
            lanes[i].anchorMax = new Vector2(1f, anchors[i]);
        }

        // Empty touch receivers
        foreach(EmptyTouchReceiver receiver
            in GetComponentsInChildren<EmptyTouchReceiver>())
        {
            Game.gameObjectToEmptyTouchReceiver.Add(
                receiver.gameObject,
                receiver);
        }

        // Lane dividers
        for (int i = 0; i < laneDividers.Count; i++)
        {
            if (!Options.instance.showLaneDividers)
            {
                laneDividers[i].SetActive(false);
                continue;
            }
            laneDividers[i].SetActive(i + 1 < Game.playableLanes);
        }

        // Beat markers
        int bps = GameSetup.pattern.patternMetadata.bps;
        switch (Options.instance.beatMarkers)
        {
            case Options.BeatMarkerVisibility.Hidden:
                break;
            case Options.BeatMarkerVisibility.ShowBeatMarkers:
                for (int i = 0; i <= bps; i++)
                {
                    SpawnMarker(beatMarkerTemplate,
                        (float)i / bps,
                        scanDirection);
                }
                break;
            case Options.BeatMarkerVisibility.ShowHalfBeatMarkers:
                for (int i = 0; i <= bps; i++)
                {
                    SpawnMarker(beatMarkerTemplate,
                        (float)i / bps,
                        scanDirection);
                    if (i < bps)
                    {
                        SpawnMarker(halfBeatMarkerTemplate,
                            (i + 0.5f) / bps,
                            scanDirection);
                    }
                }
                break;
        }
    }

    private void Update()
    {
        ControlScheme scheme = GameSetup.pattern.patternMetadata
            .controlScheme;
        if (scheme != ControlScheme.Keys) return;
        if (Game.keysForLane == null) return;
    }

    public float GetMiddleYOfLaneInWorldSpace(int lane)
    {
        Vector3[] corners = new Vector3[4];
        lanes[lane].GetWorldCorners(corners);
        return (corners[0].y + corners[1].y +
            corners[2].y + corners[3].y) * 0.25f;
    }
}
