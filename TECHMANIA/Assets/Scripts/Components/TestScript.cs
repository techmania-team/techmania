using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        // To reset audio buffer size:
        // (lower value = less latency, less performance;
        //  higher value = more latency, more performance)
        AudioConfiguration config = AudioSettings.GetConfiguration();
        // config.dspBufferSize = 512;
        // AudioSettings.Reset(config);
    }
}
