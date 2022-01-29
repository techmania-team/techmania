using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RadarDialog : MonoBehaviour
{
    public Radar radarDisplay;

    public TextMeshProUGUI densityRaw;
    public TextMeshProUGUI densityNormalized;
    public TextMeshProUGUI peakRaw;
    public TextMeshProUGUI peakNormalized;
    public TextMeshProUGUI speedRaw;
    public TextMeshProUGUI speedNormalized;
    public TextMeshProUGUI chaosRaw;
    public TextMeshProUGUI chaosNormalized;
    public TextMeshProUGUI asyncRaw;
    public TextMeshProUGUI asyncNormalized;
    // public TextMeshProUGUI shiftRaw;

    public TextMeshProUGUI suggestedLevelRounded;
    public TextMeshProUGUI suggestedLevel;

    public void Show()
    {
        Pattern.Radar radar = EditorContext.Pattern.CalculateRadar();
        radarDisplay.SetRadar(radar);

        densityRaw.text = radar.density.raw.ToString("F2");
        densityNormalized.text = radar.density.normalized.ToString();
        peakRaw.text = radar.peak.raw.ToString("F2");
        peakNormalized.text = radar.peak.normalized.ToString();
        speedRaw.text = radar.speed.raw.ToString("F2");
        speedNormalized.text = radar.speed.normalized.ToString();
        chaosRaw.text = radar.chaos.raw.ToString("F2");
        chaosNormalized.text = radar.chaos.normalized.ToString();
        asyncRaw.text = radar.async.raw.ToString("F2");
        asyncNormalized.text = radar.async.normalized.ToString();
        // shiftRaw.text = radar.shift.raw.ToString("F0");

        suggestedLevel.text = "(" +
            radar.suggestedLevel.ToString("F2") + ")";
        suggestedLevelRounded.text = $"{radar.suggestedLevelRounded - 1} - {radar.suggestedLevelRounded + 1}";

        GetComponent<Dialog>().FadeIn();
    }
}
