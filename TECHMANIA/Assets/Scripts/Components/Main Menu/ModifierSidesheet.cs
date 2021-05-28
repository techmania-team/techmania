using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ModifierSidesheet : MonoBehaviour
{
    [Header("Modifiers")]
    public TMP_Dropdown noteOpacity;
    public TMP_Dropdown scanlineOpacity;
    public TMP_Dropdown scanDirection;
    public TMP_Dropdown notePosition;
    public TMP_Dropdown scanPosition;
    public TMP_Dropdown fever;
    public TMP_Dropdown keysound;

    [Header("Appearance")]
    public Toggle judgementTallies;
    public Slider backgroundBrightnessSlider;
    public TextMeshProUGUI backgroundBrightnessDisplay;
    public Toggle noVideo;

    [Header("Special modifiers")]
    public TMP_Dropdown mode;
    public TMP_Dropdown controlOverride;
    public TMP_Dropdown scrollSpeed;

    private void OnEnable()
    {
        MemoryToUI();
    }

    private void MemoryToUI()
    {
        UIUtils.InitializeDropdownWithLocalizedOptions(
            noteOpacity,
            "modifier_normal",
            "modifier_fade_in",
            "modifier_fade_in_2",
            "modifier_fade_out",
            "modifier_fade_out_2");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scanlineOpacity,
            "modifier_normal",
            "modifier_blink",
            "modifier_blink_2",
            "modifier_blind");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scanDirection,
            "modifier_normal",
            "modifier_left_left",
            "modifier_right_right",
            "modifier_right_left");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            notePosition,
            "modifier_normal",
            "modifier_mirror");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scanPosition,
            "modifier_normal",
            "modifier_swap");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            fever,
            "modifier_normal",
            "modifier_fever_off",
            "modifier_auto_fever");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            keysound,
            "modifier_normal",
            "modifier_auto_keysound",
            "modifier_auto_keysound_plus_ticks",
            "modifier_auto_keysound_plus_auto_ticks");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            mode,
            "modifier_normal",
            "modifier_no_fail",
            "modifier_auto_play",
            "modifier_practice");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            controlOverride,
            "modifier_normal",
            "modifier_override_to_touch",
            "modifier_override_to_keys",
            "modifier_override_to_km");
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scrollSpeed,
            "modifier_normal",
            "modifier_half_speed");
    }
}
