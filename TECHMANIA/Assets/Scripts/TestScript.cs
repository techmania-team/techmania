using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TestScript : MonoBehaviour
{
    Text text;
    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<Text>();
        Input.simulateMouseWithTouches = false;
    }

    // Update is called once per frame
    void Update()
    {
        string report = "";
        report += $"Input.mousePresent: {Input.mousePresent}\n";
        report += $"Input.mousePosition: {Input.mousePosition}\n";
        report += $"Input.mouseScrollDelta: {Input.mouseScrollDelta}\n";
        report += $"Mouse button held: left={Input.GetMouseButton(0)}, middle={Input.GetMouseButton(2)}, right={Input.GetMouseButton(1)}\n";
        report += "\n";
        report += $"Input.touchSupported: {Input.touchSupported}\n";
        report += $"Input.touchPressureSupported: {Input.touchPressureSupported}\n";
        report += $"Input.multiTouchEnabled: {Input.multiTouchEnabled}\n";
        report += $"Input.stylusTouchSupported: {Input.stylusTouchSupported}\n";
        report += "\n";
        report += $"Total touches: {Input.touchCount}\n";
        report += "\n";
        for (int i = 0; i < Input.touchCount; i++)
        {
            report += $"Touch #{i}:\n";
            Touch touch = Input.GetTouch(i);
            report += $"Phase: {touch.phase} Position: {touch.position} Finger ID: {touch.fingerId}\n";
            report += "\n";
        }
        text.text = report;
    }
}
