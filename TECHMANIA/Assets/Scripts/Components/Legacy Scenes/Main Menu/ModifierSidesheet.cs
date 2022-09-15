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

    private void InitializeDropdowns()
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

    public void MemoryToUI()
    {
        InitializeDropdowns();

        // Modifiers
        
        noteOpacity.SetValueWithoutNotify(
            (int)Modifiers.instance.noteOpacityEnum);
        noteOpacity.RefreshShownValue();
        
        scanlineOpacity.SetValueWithoutNotify(
            (int)Modifiers.instance.scanlineOpacityEnum);
        scanlineOpacity.RefreshShownValue();
        
        scanDirection.SetValueWithoutNotify(
            (int)Modifiers.instance.scanDirectionEnum);
        scanDirection.RefreshShownValue();
        
        notePosition.SetValueWithoutNotify(
            (int)Modifiers.instance.notePositionEnum);
        notePosition.RefreshShownValue();
        
        scanPosition.SetValueWithoutNotify(
             (int)Modifiers.instance.scanPositionEnum);
        scanPosition.RefreshShownValue();
        
        fever.SetValueWithoutNotify(
            (int)Modifiers.instance.feverEnum);
        fever.RefreshShownValue();
        
        keysound.SetValueWithoutNotify(
            (int)Modifiers.instance.keysoundEnum);
        keysound.RefreshShownValue();

        assistTick.SetValueWithoutNotify(
            (int)Modifiers.instance.assistTickEnum);
        assistTick.RefreshShownValue();

        // Appearance

        //showJudgementTally.SetIsOnWithoutNotify(
        //    Options.instance.showJudgementTally);
        noVideo.SetIsOnWithoutNotify(
            InternalGameSetup.trackOptions.noVideo);

        // Special modifiers

        mode.SetValueWithoutNotify(
            (int)Modifiers.instance.modeEnum);
        mode.RefreshShownValue();
        
        controlOverride.SetValueWithoutNotify(
            (int)Modifiers.instance.controlOverrideEnum);
        controlOverride.RefreshShownValue();
        
        scrollSpeed.SetValueWithoutNotify(
            (int)Modifiers.instance.scrollSpeedEnum);
        scrollSpeed.RefreshShownValue();
    }

    public void UIToMemory()
    {
        // Modifiers

        Modifiers.instance.noteOpacityEnum =
            (Modifiers.NoteOpacity)noteOpacity.value;
        Modifiers.instance.scanlineOpacityEnum = 
            (Modifiers.ScanlineOpacity)scanlineOpacity.value;
        Modifiers.instance.scanDirectionEnum =
            (Modifiers.ScanDirection)scanDirection.value;
        Modifiers.instance.notePositionEnum =
            (Modifiers.NotePosition)notePosition.value;
        Modifiers.instance.scanPositionEnum =
            (Modifiers.ScanPosition)scanPosition.value;
        Modifiers.instance.feverEnum =
            (Modifiers.Fever)fever.value;
        Modifiers.instance.keysoundEnum =
            (Modifiers.Keysound)keysound.value;
        Modifiers.instance.assistTickEnum =
            (Modifiers.AssistTick)assistTick.value;

        // Appearance

        //Options.instance.showJudgementTally =
        //    showJudgementTally.isOn;
        InternalGameSetup.trackOptions.noVideo =
            noVideo.isOn;

        // Special modifiers

        Modifiers.instance.modeEnum =
            (Modifiers.Mode)mode.value;
        Modifiers.instance.controlOverrideEnum =
            (Modifiers.ControlOverride)controlOverride.value;
        Modifiers.instance.scrollSpeedEnum =
            (Modifiers.ScrollSpeed)scrollSpeed.value;

        ModifierChanged?.Invoke();
    }

    public static string GetDisplayString(bool noVideo,
        Color specialModifierColor)
    {
        List<string> regularSegments = new List<string>();
        List<string> specialSegments = new List<string>();
        Modifiers.instance.ToDisplaySegments(
            regularSegments, specialSegments);
        if (noVideo)
        {
            regularSegments.Add(L10n.GetString(
                "modifier_sidesheet_no_video_label"));
        }

        List<string> allSegments = new List<string>();
        if (regularSegments.Count + specialSegments.Count == 0)
        {
            allSegments.Add(L10n.GetString(
                "select_pattern_modifier_none"));
        }
        for (int i = 0; i < regularSegments.Count; i++)
        {
            allSegments.Add(regularSegments[i]);
        }
        for (int i = 0; i < specialSegments.Count; i++)
        {
            allSegments.Add(
                $"<color=#{ColorUtility.ToHtmlStringRGB(specialModifierColor)}>{specialSegments[i]}</color>");
        }

        return string.Join(" / ", allSegments);
    }
}
