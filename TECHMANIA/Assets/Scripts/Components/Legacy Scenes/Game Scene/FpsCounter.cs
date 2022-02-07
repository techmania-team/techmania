using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsCounter : MonoBehaviour
{
    public TMPro.TextMeshProUGUI text;
    private float timeAtLastReport;
    private const float reportInterval = 0.2f;

    private void Start()
    {
        text.text = "";
        timeAtLastReport = 0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mathf.Floor(timeAtLastReport / reportInterval) !=
            Mathf.Floor(Time.timeSinceLevelLoad / reportInterval))
        {
            float fps = 1f / Time.smoothDeltaTime;
            text.text = fps.ToString("F2") + " FPS";
            timeAtLastReport = Time.timeSinceLevelLoad;
        }
    }
}
