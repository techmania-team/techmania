using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Marker : MonoBehaviour
{
    [HideInInspector]
    public int pulse;

    public void SetTimeDisplay()
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;
        int beat = pulse / Pattern.pulsesPerBeat;
        int scan = beat / bps;
        int beatInScan = beat % bps;

        float time = EditorContext.Pattern.PulseToTime(pulse);
        int minute = Mathf.FloorToInt(time / 60f);
        time -= minute * 60f;
        int second = Mathf.FloorToInt(time);
        time -= second;
        int milliSecond = Mathf.FloorToInt(time * 1000f);

        SetText($"{scan}-{beatInScan}\n{minute}:{second:D2}.{milliSecond:D3}");
    }

    public void SetBpmText(double bpm)
    {
        SetText($"BPM={bpm}");
    }

    private void SetText(string s)
    {
        GetComponentInChildren<TextMeshProUGUI>(includeInactive: true)
            .text = s;
    }
}
