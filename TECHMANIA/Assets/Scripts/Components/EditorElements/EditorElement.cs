using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// This base class corresponds to the un-interactable
// elements in the editor: lines and markers.
//
// A derived class will handle writing note positions
// back to the pattern.
public class EditorElement : MonoBehaviour
{
    public enum Type
    {
        // Position expressed in scans
        Line,
        ScanMarker,

        // Position expressed in beats
        DottedLine,
        BeatMarker,
        TimeMarker,

        // Position expressed in pulses
        BpmMarker,

        // Position expressed in pulses and lane
        Note
    }
    public Type type;
    [HideInInspector]
    public int scan;
    [HideInInspector]
    public int beat;
    [HideInInspector]
    public int pulse;
    [HideInInspector]
    public int lane;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnEnable()
    {
        PatternPanel.RepositionNeeded += Reposition;
    }

    private void OnDisable()
    {
        PatternPanel.RepositionNeeded -= Reposition;
    }

    public void Reposition()
    {
        int bps = Navigation.GetCurrentPattern().patternMetadata.bps;

        float x = 0;
        switch (type)
        {
            case Type.Line:
            case Type.ScanMarker:
                x = PatternPanel.ScanLength * scan;
                break;
            case Type.DottedLine:
            case Type.BeatMarker:
            case Type.TimeMarker:
                {
                    float scan = (float)beat / bps;
                    x = PatternPanel.ScanLength * scan;
                }
                break;
            case Type.BpmMarker:
            case Type.Note:
                {
                    float beat = (float)pulse / Pattern.pulsesPerBeat;
                    float scan = beat / bps;
                    x = PatternPanel.ScanLength * scan;
                }
                break;
        }

        float y = 0;
        switch (type)
        {
            case Type.ScanMarker:
                y = 0f;
                break;
            case Type.BeatMarker:
                y = -20f;
                break;
            case Type.TimeMarker:
                y = -40f;
                break;
            case Type.BpmMarker:
                y = -60f;
                break;
            default:
                // Not supported yet
                break;
        }

        RectTransform rect = GetComponent<RectTransform>();
        switch (type)
        {
            case Type.Line:
            case Type.DottedLine:
                rect.anchorMin = new Vector2(0f, 0f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, 0.5f);
                break;
            case Type.ScanMarker:
            case Type.BeatMarker:
            case Type.TimeMarker:
            case Type.BpmMarker:
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(x, y);
                break;
            default:
                // Not supported yet
                break;
        }
    }
}
