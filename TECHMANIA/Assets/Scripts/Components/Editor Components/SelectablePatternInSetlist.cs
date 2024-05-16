using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SelectablePatternInSetlist : MonoBehaviour
{
    public Button moveUpButton;
    public Button moveDownButton;
    public TextMeshProUGUI trackTitle;
    public PatternBanner banner;
    public UnityEngine.Color nonexistantPatternTitleColor;

    private EditSetlistPanel panel;
    private int index;

    public void SetUp(EditSetlistPanel panel, string trackTitle,
        PatternMetadata patternMetadata, int index, int numPatterns)
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
    }

    public void SetUpNonExistant(EditSetlistPanel panel,
        int index, int numPatterns)
    {
        moveUpButton.interactable = index > 0;
        moveDownButton.interactable = index < numPatterns - 1;
        this.trackTitle.text = $"<color=#{ColorUtility.ToHtmlStringRGB(nonexistantPatternTitleColor)}>{L10n.GetString("edit_setlist_panel_reference_not_found")}</color>";
        banner.InitializeNonExistant();
        this.index = index;
    }

    public void OnDeleteButtonClick()
    {
        panel.DeleteSelectablePattern(index);
    }

    public void OnMoveUpButtonClick()
    {
        panel.MoveSelectablePattern(index, -1);
    }

    public void OnMoveDownButtonClick()
    {
        panel.MoveSelectablePattern(index, 1);
    }

    public void OnTrackButtonClick()
    {
        panel.ReplaceSelectablePattern(index, changeTrack: true);
    }

    public void OnPatternButtonClick()
    {
        panel.ReplaceSelectablePattern(index, changeTrack: false);
    }
}
