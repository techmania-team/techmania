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

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject o in laneDividers)
        {
            o.SetActive(false);
        }
    }

    private void SpawnMarker(GameObject template,
        float relativePosition,
        Scan.Direction scanDirection)
    {
        float x = Mathf.Lerp(
            Scan.kSpaceBeforeScan, 1f - Scan.kSpaceAfterScan,
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
    }

    public void Initialize(Scan.Direction scanDirection)
    {
        // Lanes
        float laneHeightRelative =
            (1f - Ruleset.instance.scanMargin * 2f) * 0.25f;
        lanes[3].anchorMin = new Vector2(
            0f, 0f);
        lanes[3].anchorMax = new Vector2(
            1f, 0.5f - laneHeightRelative);
        lanes[2].anchorMin = new Vector2(
            0f, 0.5f - laneHeightRelative);
        lanes[2].anchorMax = new Vector2(
            1f, 0.5f);
        lanes[1].anchorMin = new Vector2(
            0f, 0.5f);
        lanes[1].anchorMax = new Vector2(
            1f, 0.5f + laneHeightRelative);
        lanes[0].anchorMin = new Vector2(
            0f, 0.5f + laneHeightRelative);
        lanes[0].anchorMax = new Vector2(
            1f, 1f);

        // Lane dividers
        foreach (GameObject o in laneDividers)
        {
            o.SetActive(Options.instance.showLaneDividers);
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
}
