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
            InternalGameSetup.trackOptions.backgroundBrightness);
        RefreshBrightnessDisplay();
    }

    private void RefreshBrightnessDisplay()
    {
        display.text = InternalGameSetup.trackOptions
            .backgroundBrightness.ToString();
    }

    public void UIToMemory()
    {
        InternalGameSetup.trackOptions.backgroundBrightness =
            Mathf.FloorToInt(slider.value);
        RefreshBrightnessDisplay();
    }
}
