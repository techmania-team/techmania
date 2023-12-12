using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PatternPanelToolbar : MonoBehaviour
{
    public PatternPanel panel;

    public TextMeshProUGUI beatSnapDividerDisplay;
    public MaterialToggleButton panButton;
    public MaterialToggleButton noteToolButton;
    public MaterialToggleButton rectangleToolButton;
    public MaterialToggleButton rectangleAppendButton;
    public MaterialToggleButton rectangleSubtractButton;
    public MaterialToggleButton anchorButton;
    public List<NoteTypeButton> noteTypeButtons;

    #region Event handler
    private void ChangeTool(PatternPanel.Tool newTool)
    {
        PatternPanel.tool = newTool;
        RefreshToolAndNoteTypeButtons();
    }

    public void OnPanToolButtonClick()
    {
        ChangeTool(PatternPanel.Tool.Pan);
    }

    public void OnNoteToolButtonClick()
    {
        ChangeTool(PatternPanel.Tool.Note);
    }

    public void OnRectangleToolButtonClick()
    {
        ChangeTool(PatternPanel.Tool.Rectangle);
    }

    public void OnRectangleAppendButtonClick()
    {
        ChangeTool(PatternPanel.Tool.RectangleAppend);
    }

    public void OnRectangleSubtractButtonClick()
    {
        ChangeTool(PatternPanel.Tool.RectangleSubtract);
    }

    public void OnAnchorButtonClick()
    {
        ChangeTool(PatternPanel.Tool.Anchor);
    }

    public void OnNoteTypeButtonClick(NoteTypeButton clickedButton)
    {
        panel.ChangeNoteType(clickedButton.type);
        RefreshToolAndNoteTypeButtons();
    }

    public void OnBeatSnapDivisorChanged(int direction)
    {
        int divisor = Options.instance.editorOptions.beatSnapDivisor;
        do
        {
            divisor += direction;
            if (divisor <= 0 && direction < 0)
            {
                divisor = Pattern.pulsesPerBeat;
            }
            if (divisor > Pattern.pulsesPerBeat &&
                direction > 0)
            {
                divisor = 1;
            }
        }
        while (Pattern.pulsesPerBeat % divisor != 0);
        Options.instance.editorOptions.beatSnapDivisor =
            divisor;
        RefreshBeatSnapDivisorDisplay();
    }
    #endregion

    #region UI refreshing
    public void RefreshToolAndNoteTypeButtons()
    {
        panButton.SetIsOn(
            PatternPanel.tool == PatternPanel.Tool.Pan);
        noteToolButton.SetIsOn(
            PatternPanel.tool == PatternPanel.Tool.Note);
        rectangleToolButton.SetIsOn(
            PatternPanel.tool == PatternPanel.Tool.Rectangle);
        rectangleAppendButton.SetIsOn(
            PatternPanel.tool == PatternPanel.Tool.RectangleAppend);
        rectangleSubtractButton.SetIsOn(
            PatternPanel.tool == PatternPanel.Tool.RectangleSubtract);
        anchorButton.SetIsOn(
            PatternPanel.tool == PatternPanel.Tool.Anchor);
        foreach (NoteTypeButton b in noteTypeButtons)
        {
            b.GetComponent<MaterialToggleButton>().SetIsOn(
                PatternPanel.tool == PatternPanel.Tool.Note &&
                b.type == panel.noteType);
        }
    }

    public void RefreshBeatSnapDivisorDisplay()
    {
        beatSnapDividerDisplay.text =
            Options.instance.editorOptions.beatSnapDivisor.ToString();
    }
    #endregion
}
