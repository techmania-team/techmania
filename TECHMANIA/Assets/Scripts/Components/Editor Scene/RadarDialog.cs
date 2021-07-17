using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class RadarDialog : MonoBehaviour
{
    public TextMeshProUGUI densityRaw;
    public TextMeshProUGUI densityNormalized;
    public TextMeshProUGUI voltageRaw;
    public TextMeshProUGUI voltageNormalized;
    public TextMeshProUGUI speedRaw;
    public TextMeshProUGUI speedNormalized;
    public TextMeshProUGUI chaosRaw;
    public TextMeshProUGUI chaosNormalized;
    public TextMeshProUGUI asyncRaw;
    public TextMeshProUGUI asyncNormalized;
    public TextMeshProUGUI shiftRaw;

    public TextMeshProUGUI suggestedLevelRounded;
    public TextMeshProUGUI suggestedLevel;

    public void Show()
    {
        Pattern.Radar radar = EditorContext.Pattern.CalculateRadar();

        densityRaw.text = radar.density.raw.ToString("F2");
        voltageRaw.text = radar.voltage.raw.ToString("F2");
        speedRaw.text = radar.speed.raw.ToString("F2");
        chaosRaw.text = radar.chaos.raw.ToString("F2");
        asyncRaw.text = radar.async.raw.ToString("F2");
        shiftRaw.text = radar.shift.raw.ToString("F2");

        GetComponent<Dialog>().FadeIn();
    }
}
