using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FpsCounter : MonoBehaviour
{
    public TMPro.TextMeshProUGUI text;

    private void Start()
    {
        text.text = "";
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.frameCount % 20 == 0)
        {
            float fps = 1f / Time.smoothDeltaTime;
            text.text = fps.ToString("F2") + " FPS";
        }
    }
}
