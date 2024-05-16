using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HiddenPatternInSetlist : MonoBehaviour
{
    public Button moveUpButton;
    public Button moveDownButton;
    public TextMeshProUGUI trackTitle;
    public PatternBanner banner;
    public UnityEngine.Color nonexistantPatternTitleColor;

    [Header("Criteria")]
    public GameObject criteriaContainer;
    public TMP_Dropdown criteriaType;
    public TMP_Dropdown criteriaDirection;
    public TMP_InputField criteriaValue;
    public GameObject noCriteriaLabel;

    private EditSetlistPanel panel;
    private int index;

    public void SetUp(EditSetlistPanel panel, string trackTitle,
        PatternMetadata patternMetadata,
        Setlist.HiddenPattern hiddenPattern,
        int index, int numPatterns)
    {
        moveUpButton.interactable = index > 0;
        moveDownButton.interactable = index < numPatterns - 1;
        this.panel = panel;
        this.trackTitle.text = trackTitle;
        banner.Initialize(patternMetadata);
        if (patternMetadata.controlScheme !=
            EditorContext.setlist.setlistMetadata.controlScheme)
        {
            banner.MakeControlIconRed();
        }
        this.index = index;

        SetUpCriteria(
            lastHiddenPattern: index == numPatterns - 1,
            hiddenPattern);
    }

    public void SetUpNonExistant(EditSetlistPanel panel,
        Setlist.HiddenPattern hiddenPattern,
        int index, int numPatterns)
    {
        moveUpButton.interactable = index > 0;
        moveDownButton.interactable = index < numPatterns - 1;
        this.trackTitle.text = $"<color={ColorUtility.ToHtmlStringRGB(nonexistantPatternTitleColor)}>{L10n.GetString("edit_setlist_panel_reference_not_found")}</color>";
        banner.InitializeNonExistant();
        this.index = index;

        SetUpCriteria(
            lastHiddenPattern: index == numPatterns - 1,
            hiddenPattern);
    }

    private void SetUpCriteria(bool lastHiddenPattern,
        Setlist.HiddenPattern hiddenPattern)
    {
        if (lastHiddenPattern)
        {
            criteriaContainer.SetActive(false);
            noCriteriaLabel.SetActive(true);
            return;
        }

        criteriaContainer.SetActive(true);
        noCriteriaLabel.SetActive(false);

        criteriaType.options.Clear();
        criteriaType.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString(
            "edit_setlist_panel_hidden_pattern_criteria_type_index")));
        criteriaType.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString(
            "edit_setlist_panel_hidden_pattern_criteria_type_level")));
        criteriaType.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString(
            "edit_setlist_panel_hidden_pattern_criteria_type_hp")));
        criteriaType.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString(
            "edit_setlist_panel_hidden_pattern_criteria_type_score")));
        criteriaType.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString(
            "edit_setlist_panel_hidden_pattern_criteria_type_combo")));
        criteriaType.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString(
            "edit_setlist_panel_hidden_pattern_criteria_type_max_combo")));
        criteriaType.options.Add(new TMP_Dropdown.OptionData(
            L10n.GetString(
            "edit_setlist_panel_hidden_pattern_criteria_type_d100")));
        criteriaType.SetValueWithoutNotify(
            (int)hiddenPattern.criteriaType);

        criteriaDirection.options.Clear();
        criteriaDirection.options.Add(
            new TMP_Dropdown.OptionData("<"));
        criteriaDirection.options.Add(
            new TMP_Dropdown.OptionData(">"));
        criteriaDirection.SetValueWithoutNotify(
            (int)hiddenPattern.criteriaDirection);

        criteriaValue.SetTextWithoutNotify(
            hiddenPattern.criteriaValue.ToString());
    }

    public void OnDeleteButtonClick()
    {
        panel.DeleteHiddenPattern(index);
    }

    public void OnMoveUpButtonClick()
    {
        panel.MoveHiddenPattern(index, -1);
    }

    public void OnMoveDownButtonClick()
    {
        panel.MoveHiddenPattern(index, 1);
    }

    public void OnTrackButtonClick()
    {
        panel.ReplaceHiddenPattern(index, changeTrack: true);
    }

    public void OnPatternButtonClick()
    {
        panel.ReplaceHiddenPattern(index, changeTrack: false);
    }

    public void OnCriteriaTypeChanged(int newValue)
    {
        panel.ChangeCriteriaType(index,
            (Setlist.HiddenPatternCriteriaType)newValue);
    }

    public void OnCriteriaDirectionChanged(int newValue)
    {
        panel.ChangeCriteriaDirection(index,
            (Setlist.HiddenPatternCriteriaDirection)newValue);
    }

    public void OnCriteriaValueChanged(string newValueString)
    {
        int newValue = 0;
        if (int.TryParse(newValueString, out newValue))
        {
            panel.ChangeCriteriaValue(index, newValue);
        }
    }
}
