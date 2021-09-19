using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Marker : MonoBehaviour
{
    public TextMeshProUGUI scanBeatText;
    public TextMeshProUGUI timeText;
    public TextMeshProUGUI timeEventText;

    [HideInInspector]
    public int pulse;

    public void SetTimeDisplay()
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;
        int beat = pulse / Pattern.pulsesPerBeat;
        int scan = beat / bps;
        int beatInScan = beat % bps;

        float time = EditorContext.Pattern.PulseToTime(pulse);

        string scanPosition = "";
        if (beatInScan == 0)
        {
            scanPosition = (scan % 2 == 0) ?
                "↓" : "↑";
        }

        scanBeatText.text = $"{scanPosition}{scan}-{beatInScan}";
        timeText.text = UIUtils.FormatTime(time,
            includeMillisecond: true);
    }

    public void SetBpmText(double bpm)
    {
        timeEventText.text = bpm.ToString();
    }

    public void SetTimeStopText(int pulses)
    {
        float beats = (float)pulses / Pattern.pulsesPerBeat;
        timeEventText.text = beats.ToString("G5");
    }
}
