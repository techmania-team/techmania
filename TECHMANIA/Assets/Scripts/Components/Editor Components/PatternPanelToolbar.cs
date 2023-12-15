using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class PatternPanelToolbar : MonoBehaviour
{
    public PatternPanel panel;
    public TextMeshProUGUI beatSnapDividerDisplay;

    [Header("Tools and note types")]
    public MaterialToggleButton panButton;
    public MaterialToggleButton noteToolButton;
    public MaterialToggleButton rectangleToolButton;
    public MaterialToggleButton rectangleAppendButton;
    public MaterialToggleButton rectangleSubtractButton;
    public MaterialToggleButton anchorButton;
    public List<NoteTypeButton> noteTypeButtons;

    [Header("Dialogs")]
    public TimeEventDialog timeEventDialog;
    public RadarDialog radarDialog;

    #region Tools and note types
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
    #endregion

    #region Dialogs
    public void OnTimeEventButtonClick()
    {
        int scanlineIntPulse = (int)panel.workspace.scanlineFloatPulse;
        BpmEvent currentBpmEvent = EditorContext.Pattern.bpmEvents.
            Find((BpmEvent e) =>
            {
                return e.pulse == scanlineIntPulse;
            });
        TimeStop currentTimeStop = EditorContext.Pattern.timeStops.
            Find((TimeStop t) =>
            {
                return t.pulse == scanlineIntPulse;
            });

        timeEventDialog.Show(currentBpmEvent, currentTimeStop,
            (double? newBpm, int? newTimeStopPulses) =>
            {
                bool bpmEventChanged = true, timeStopChanged = true;
                if (currentBpmEvent == null && newBpm == null)
                {
                    bpmEventChanged = false;
                }
                if (currentBpmEvent != null && newBpm != null &&
                    currentBpmEvent.bpm == newBpm.Value)
                {
                    bpmEventChanged = false;
                }
                if (newTimeStopPulses.HasValue &&
                    newTimeStopPulses.Value == 0)
                {
                    newTimeStopPulses = null;
                }
                if (currentTimeStop == null && newTimeStopPulses == null)
                {
                    timeStopChanged = false;
                }
                if (currentTimeStop != null && newTimeStopPulses != null
                    && currentTimeStop.duration == newTimeStopPulses.Value)
                {
                    timeStopChanged = false;
                }
                bool anyChange = bpmEventChanged || timeStopChanged;
                if (!anyChange)
                {
                    return;
                }

                panel.ChangeTimeEvent(scanlineIntPulse,
                    newBpm, newTimeStopPulses);
                panel.workspace.DestroyAndRespawnAllMarkers();
            });
    }

    public void OnInspectButtonClick()
    {
        List<Note> notesWithIssue = new List<Note>();
        string issue = EditorContext.Pattern.Inspect(notesWithIssue);
        if (issue == null)
        {
            panel.snackbar.Show(L10n.GetString(
                "pattern_inspection_no_issue"));
        }
        else
        {
            panel.snackbar.Show(issue);
            panel.selectedNotes.Clear();
            foreach (Note n in notesWithIssue)
            {
                panel.selectedNotes.Add(n);
            }
            // Scroll the first selected note into view.
            if (panel.selectedNotes.Count > 0)
            {
                panel.workspace.ScrollNoteIntoView(notesWithIssue[0]);
            }
            panel.NotifySelectionChanged();
        }
    }

    public void OnRadarButtonClick()
    {
        radarDialog.Show();
    }
    #endregion

    #region Beat snap divisor
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
