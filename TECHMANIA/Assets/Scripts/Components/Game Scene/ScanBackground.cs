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
    public KeystrokeFeedback kmKeystrokeFeedback;

    private List<int> numKeysHeldOnLane;
    private int totalKeysHeld;

    // Start is called before the first frame update
    void Start()
    {
        foreach (GameObject o in laneDividers)
        {
            o.SetActive(false);
        }

        numKeysHeldOnLane = new List<int>();
        foreach (var l in lanes) numKeysHeldOnLane.Add(0);
        totalKeysHeld = 0;
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
            (1f - Ruleset.instance.scanMargin * 2f) /
            Game.playableLanes;
        List<float> anchors = new List<float>();
        anchors.Add(1f);
        switch (Game.playableLanes)
        {
            case 4:
                anchors.Add(0.5f + laneHeightRelative);
                anchors.Add(0.5f);
                anchors.Add(0.5f - laneHeightRelative);
                break;
            case 3:
                anchors.Add(1f - Ruleset.instance.scanMargin -
                    laneHeightRelative);
                anchors.Add(Ruleset.instance.scanMargin +
                    laneHeightRelative);
                break;
            case 2:
                anchors.Add(0.5f);
                break;
        }
        anchors.Add(0f);
        for (int i = 0; i < Game.playableLanes; i++)
        {
            lanes[i].anchorMin = new Vector2(0f, anchors[i + 1]);
            lanes[i].anchorMax = new Vector2(1f, anchors[i]);
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

        // Keystroke feedback
        if (scanDirection == Scan.Direction.Left)
        {
            kmKeystrokeFeedback.GetComponent<RectTransform>()
                .localScale = new Vector3(-1f, 1f, 1f);
            foreach (RectTransform l in lanes)
            {
                l.localScale = new Vector3(-1f, 1f, 1f);
            }
        }
    }

    private void Update()
    {
        ControlScheme scheme = GameSetup.pattern.patternMetadata
            .controlScheme;
        if (Game.keysForLane == null) return;

        switch (scheme)
        {
            case ControlScheme.Touch: return;
            case ControlScheme.Keys:
                for (int i = 0; i < Game.playableLanes; i++)
                {
                    foreach (KeyCode c in Game.keysForLane[i])
                    {
                        if (Input.GetKeyDown(c))
                        {
                            numKeysHeldOnLane[i]++;
                            if (numKeysHeldOnLane[i] == 1)
                            {
                                lanes[i]
                                    .GetComponent<KeystrokeFeedback>()
                                    .Play();
                            }
                        }
                        if (Input.GetKeyUp(c))
                        {
                            numKeysHeldOnLane[i]--;
                            if (numKeysHeldOnLane[i] <= 0)
                            {
                                lanes[i]
                                    .GetComponent<KeystrokeFeedback>()
                                    .Stop();
                            }
                        }
                    }
                }
                break;
            case ControlScheme.KM:
                for (int i = 0; i < Game.playableLanes; i++)
                {
                    foreach (KeyCode c in Game.keysForLane[i])
                    {
                        if (Input.GetKeyDown(c))
                        {
                            totalKeysHeld++;
                            if (totalKeysHeld == 1)
                            {
                                kmKeystrokeFeedback.Play();
                            }
                        }
                        if (Input.GetKeyUp(c))
                        {
                            totalKeysHeld--;
                            if (totalKeysHeld <= 0)
                            {
                                kmKeystrokeFeedback.Stop();
                            }
                        }
                    }
                }
                break;
        }
    }
}
