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
    public TMP_Dropdown assistTick;

    [Header("Appearance")]
    public Toggle showJudgementTally;
    public Slider backgroundBrightnessSlider;
    public TextMeshProUGUI backgroundBrightnessDisplay;
    public Toggle noVideo;

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
            Modifiers.noteOpacityDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scanlineOpacity,
            Modifiers.scanlineOpacityDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scanDirection,
            Modifiers.scanDirectionDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            notePosition,
            Modifiers.notePositionDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scanPosition,
            Modifiers.scanPositionDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            fever,
            Modifiers.feverDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            keysound,
            Modifiers.keysoundDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            assistTick,
            Modifiers.assistTickDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            mode,
            Modifiers.modeDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            controlOverride,
            Modifiers.controlOverrideDisplayKeys);
        UIUtils.InitializeDropdownWithLocalizedOptions(
            scrollSpeed,
            Modifiers.scrollSpeedDisplayKeys);
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

        assistTick.SetValueWithoutNotify(
            (int)Modifiers.instance.assistTick);
        assistTick.RefreshShownValue();

        // Appearance

        showJudgementTally.SetIsOnWithoutNotify(
            Options.instance.showJudgementTally);
        backgroundBrightnessSlider.SetValueWithoutNotify(
            GameSetup.trackOptions.backgroundBrightness);
        RefreshBrightnessDisplay();
        noVideo.SetIsOnWithoutNotify(
            GameSetup.trackOptions.noVideo);

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
            GameSetup.trackOptions.backgroundBrightness.ToString();
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
        Modifiers.instance.assistTick =
            (Modifiers.AssistTick)assistTick.value;

        // Appearance

        Options.instance.showJudgementTally =
            showJudgementTally.isOn;
        GameSetup.trackOptions.backgroundBrightness =
            Mathf.FloorToInt(backgroundBrightnessSlider.value);
        GameSetup.trackOptions.noVideo =
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
        List<string> regularSegments = new List<string>();
        List<string> specialSegments = new List<string>();
        Modifiers.instance.ToDisplaySegments(
            regularSegments, specialSegments);

        if (GameSetup.trackOptions.noVideo)
        {
            regularSegments.Add(Locale.GetString(
                "modifier_sidesheet_no_video_label"));
        }
        if (regularSegments.Count == 0)
        {
            regularSegments.Add(Locale.GetString(
                "select_pattern_modifier_none"));
        }

        line1 = string.Join(" / ", regularSegments);
        line2 = string.Join(" / ", specialSegments);
    }
}
