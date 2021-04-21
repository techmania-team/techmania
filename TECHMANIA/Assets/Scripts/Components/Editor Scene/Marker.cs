using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Marker : MonoBehaviour
{
    public TextMeshProUGUI scanBeatText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI bpmOrTimeStopText;

    [HideInInspector]
    public int pulse;

    public void SetTimeDisplay()
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;
        int beat = pulse / Pattern.pulsesPerBeat;
        int scan = beat / bps;
        int beatInScan = beat % bps;

        float time = EditorContext.Pattern.PulseToTime(pulse);
        bool negative = time < 0f;
        time = Mathf.Abs(time);
        int minute = Mathf.FloorToInt(time / 60f);
        time -= minute * 60f;
        int second = Mathf.FloorToInt(time);
        time -= second;
        int milliSecond = Mathf.FloorToInt(time * 1000f);

        string sign = negative ? "-" : "";
        scanBeatText.text = $"{scan}-{beatInScan}";
        timeText.text = $"{sign}{minute}:{second:D2}.{milliSecond:D3}";
    }

    public void SetBpmText(double bpm)
    {
        bpmOrTimeStopText.text = bpm.ToString();
    }

    public void SetTimeStopText(double beats)
    {
        bpmOrTimeStopText.text = beats.ToString();
    }
}
