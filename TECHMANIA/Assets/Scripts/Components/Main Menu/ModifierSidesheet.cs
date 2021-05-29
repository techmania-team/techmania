using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
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
    public Toggle showJudgementTally;
    public Slider backgroundBrightnessSlider;
    public TextMeshProUGUI backgroundBrightnessDisplay;
    public Toggle noVideo;
    private PerTrackOptions perTrackOptions;

    [Header("Special modifiers")]
    public TMP_Dropdown mode;
    public TMP_Dropdown controlOverride;
    public TMP_Dropdown scrollSpeed;

    public static event UnityAction ModifierChanged;

    private void OnEnable()
    {
        MemoryToUI();
    }

    // To be called by SelectPatternPanel. Prepares the
    // dropdowns and per-track options.
    public void Prepare()
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

        perTrackOptions = Options.instance.GetPerTrackOptions(
            GameSetup.track);
    }

    private void MemoryToUI()
    {
        // Modifiers
        
        noteOpacity.SetValueWithoutNotify(
            (int)Modifiers.instance.noteOpacity);
        noteOpacity.RefreshShownValue();
        
        scanlineOpacity.SetValueWithoutNotify(
            (int)Modifiers.instance.scanlineOpacity);
        scanlineOpacity.RefreshShownValue();
        
        scanDirection.SetValueWithoutNotify(
            (int)Modifiers.instance.scanDirection);
        scanDirection.RefreshShownValue();
        
        notePosition.SetValueWithoutNotify(
            (int)Modifiers.instance.notePosition);
        notePosition.RefreshShownValue();
        
        scanPosition.SetValueWithoutNotify(
             (int)Modifiers.instance.scanPosition);
        scanPosition.RefreshShownValue();
        
        fever.SetValueWithoutNotify(
            (int)Modifiers.instance.fever);
        fever.RefreshShownValue();
        
        keysound.SetValueWithoutNotify(
            (int)Modifiers.instance.keysound);
        keysound.RefreshShownValue();

        // Appearance

        showJudgementTally.SetIsOnWithoutNotify(
            Options.instance.showJudgementTally);
        backgroundBrightnessSlider.SetValueWithoutNotify(
            perTrackOptions.backgroundBrightness);
        RefreshBrightnessDisplay();
        noVideo.SetIsOnWithoutNotify(
            perTrackOptions.noVideo);

        // Special modifiers

        mode.SetValueWithoutNotify(
            (int)Modifiers.instance.mode);
        mode.RefreshShownValue();
        
        controlOverride.SetValueWithoutNotify(
            (int)Modifiers.instance.controlOverride);
        controlOverride.RefreshShownValue();
        
        scrollSpeed.SetValueWithoutNotify(
            (int)Modifiers.instance.scrollSpeed);
        scrollSpeed.RefreshShownValue();
    }
    
    public void RefreshBrightnessDisplay()
    {
        backgroundBrightnessDisplay.text =
            perTrackOptions.backgroundBrightness.ToString();
    }

    public void UIToMemory()
    {
        // Modifiers

        Modifiers.instance.noteOpacity =
            (Modifiers.NoteOpacity)noteOpacity.value;
        Modifiers.instance.scanlineOpacity = 
            (Modifiers.ScanlineOpacity)scanlineOpacity.value;
        Modifiers.instance.scanDirection =
            (Modifiers.ScanDirection)scanDirection.value;
        Modifiers.instance.notePosition =
            (Modifiers.NotePosition)notePosition.value;
        Modifiers.instance.scanPosition =
            (Modifiers.ScanPosition)scanPosition.value;
        Modifiers.instance.fever =
            (Modifiers.Fever)fever.value;
        Modifiers.instance.keysound =
            (Modifiers.Keysound)keysound.value;

        // Appearance

        Options.instance.showJudgementTally =
            showJudgementTally.isOn;
        perTrackOptions.backgroundBrightness =
            Mathf.FloorToInt(backgroundBrightnessSlider.value);
        perTrackOptions.noVideo =
            noVideo.isOn;

        // Special modifiers

        Modifiers.instance.mode =
            (Modifiers.Mode)mode.value;
        Modifiers.instance.controlOverride =
            (Modifiers.ControlOverride)controlOverride.value;
        Modifiers.instance.scrollSpeed =
            (Modifiers.ScrollSpeed)scrollSpeed.value;

        ModifierChanged?.Invoke();
    }

    // Line 1 contains modifiers and appearance options;
    // Line 2 contains special modifiers.
    public void GetModifierDisplay(
        out string line1, out string line2)
    {
        List<string> segments = new List<string>();

        if (Modifiers.instance.noteOpacity != 0)
        {
            segments.Add(noteOpacity.options[
                (int)Modifiers.instance.noteOpacity].text);
        }
        if (Modifiers.instance.scanlineOpacity != 0)
        {
            segments.Add(scanlineOpacity.options[
                (int)Modifiers.instance.scanlineOpacity].text);
        }
        if (Modifiers.instance.scanDirection != 0)
        {
            segments.Add(scanDirection.options[
                (int)Modifiers.instance.scanDirection].text);
        }
        if (Modifiers.instance.notePosition != 0)
        {
            segments.Add(notePosition.options[
                (int)Modifiers.instance.notePosition].text);
        }
        if (Modifiers.instance.scanPosition != 0)
        {
            segments.Add(scanPosition.options[
                (int)Modifiers.instance.scanPosition].text);
        }
        if (Modifiers.instance.fever != 0)
        {
            segments.Add(fever.options[
                (int)Modifiers.instance.fever].text);
        }
        if (Modifiers.instance.keysound != 0)
        {
            segments.Add(keysound.options[
                (int)Modifiers.instance.keysound].text);
        }
        if (perTrackOptions.noVideo)
        {
            segments.Add(Locale.GetString(
                "modifier_sidesheet_no_video_label"));
        }
        if (segments.Count == 0)
        {
            segments.Add(Locale.GetString(
                "select_pattern_modifier_none"));
        }

        line1 = string.Join(" / ", segments);

        segments.Clear();
        if (Modifiers.instance.mode != 0)
        {
            segments.Add(mode.options[
                (int)Modifiers.instance.mode].text);
        }
        if (Modifiers.instance.controlOverride != 0)
        {
            segments.Add(controlOverride.options[
                (int)Modifiers.instance.controlOverride].text);
        }
        if (Modifiers.instance.scrollSpeed != 0)
        {
            segments.Add(scrollSpeed.options[
                (int)Modifiers.instance.scrollSpeed].text);
        }

        line2 = string.Join(" / ", segments);
    }
}
