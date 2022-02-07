using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BgBrightnessSlider : MonoBehaviour
{
    public Slider slider;
    public TextMeshProUGUI display;

    private void OnEnable()
    {
        MemoryToUI();
    }

    private void MemoryToUI()
    {
        slider.SetValueWithoutNotify(
            GameSetup.trackOptions.backgroundBrightness);
        RefreshBrightnessDisplay();
    }

    private void RefreshBrightnessDisplay()
    {
        display.text = GameSetup.trackOptions
            .backgroundBrightness.ToString();
    }

    public void UIToMemory()
    {
        GameSetup.trackOptions.backgroundBrightness =
            Mathf.FloorToInt(slider.value);
        RefreshBrightnessDisplay();
    }
}
