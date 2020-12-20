using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Shared by notes and markers, this component positions
// the GameObject at the appropriate position in the workspace.
public class SelfPositionerInEditor : MonoBehaviour
{
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
        int bps = EditorContext.Pattern.patternMetadata.bps;

        float scan = 0f;
        Marker marker = GetComponent<Marker>();
        ScanlineInEditor scanline = GetComponent<ScanlineInEditor>();
        NoteObject noteObject = GetComponent<NoteObject>();
        if (marker != null)
        {
            float beat = (float)marker.pulse / PatternV1.pulsesPerBeat;
            scan = beat / bps;
        }
        else if (scanline != null)
        {
            float beat = scanline.floatPulse / PatternV1.pulsesPerBeat;
            scan = beat / bps;
        }
        else
        {
            float beat = (float)noteObject.note.pulse / PatternV1.pulsesPerBeat;
            scan = beat / bps;
        }
        float x = PatternPanel.ScanWidth * scan;

        float y = 0;
        if (noteObject != null)
        {
            y = -PatternPanel.LaneHeight * (noteObject.note.lane + 0.5f);
        }

        RectTransform rect = GetComponent<RectTransform>();
        if (noteObject != null)
        {
            rect.anchoredPosition = new Vector2(x, y);
            rect.sizeDelta = new Vector2(
                PatternPanel.LaneHeight, PatternPanel.LaneHeight);
        }
        else
        {
            rect.anchoredPosition = new Vector2(x, 0f);
        }
    }
}
