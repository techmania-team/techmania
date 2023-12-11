using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NoteDetailSideSheet : MonoBehaviour
{
    public GameObject noSelectionNotice;
    public GameObject contents;
    public Slider volumeSlider;
    public TextMeshProUGUI volumeDisplay;
    public Slider panSlider;
    public TextMeshProUGUI panDisplay;
    public GameObject endOfScanOptions;
    public Toggle endOfScanToggle;
    public GameObject bSplineOptions;
    public Toggle bSplineToggle;
    public PatternPanel patternPanel;

    private HashSet<Note> selection;
    private List<Note> notes;

    private void OnEnable()
    {
        PatternPanel.SelectionChanged += OnSelectionChanged;
        OnSelectionChanged(patternPanel.selectedNotes);
    }

    private void OnDisable()
    {
        PatternPanel.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(HashSet<Note> newSelection)
    {
        if (newSelection == null) return;
        if (newSelection.Count == 0)
        {
            noSelectionNotice.SetActive(true);
            contents.SetActive(false);
            return;
        }
        selection = newSelection;
        noSelectionNotice.SetActive(false);
        contents.SetActive(true);

        bool multiple = newSelection.Count > 1;
        notes = new List<Note>();
        foreach (Note n in newSelection)
        {
            notes.Add(n);
        }

        if (!multiple)
        {
            volumeSlider.SetValueWithoutNotify(
                notes[0].volumePercent);
            panSlider.SetValueWithoutNotify(notes[0].panPercent);
        }
        RefreshVolumeAndPanDisplay();

        bool allNotesOnScanDividers = true;
        bool allNotesAreDragNotes = true;
        int pulsesPerScan = Pattern.pulsesPerBeat *
            EditorContext.Pattern.patternMetadata.bps;
        foreach (Note n in notes)
        {
            if (n.pulse % pulsesPerScan != 0)
            {
                allNotesOnScanDividers = false;
            }
            if (n.type != NoteType.Drag)
            {
                allNotesAreDragNotes = false;
            }
        }

        if (allNotesAreDragNotes)
        {
            bSplineOptions.SetActive(true);
            bSplineToggle.SetIsOnWithoutNotify(
                (notes[0] as DragNote).curveType == CurveType.BSpline);
            endOfScanOptions.SetActive(false);
        }
        else if (allNotesOnScanDividers)
        {
            bSplineOptions.SetActive(false);
            endOfScanOptions.SetActive(true);
            endOfScanToggle.SetIsOnWithoutNotify(notes[0].endOfScan);
        }
        else
        {
            bSplineOptions.SetActive(false);
            endOfScanOptions.SetActive(false);
        }
    }

    // Assumes at least 1 note selected.
    private void RefreshVolumeAndPanDisplay()
    {
        bool sameValue = true;
        int volumePercent = notes[0].volumePercent;
        for (int i = 1; i < notes.Count; i++)
        {
            if (notes[i].volumePercent != volumePercent)
            {
                sameValue = false;
            }
        }
        if (sameValue)
        {
            volumeDisplay.text = volumePercent + "%";
        }
        else
        {
            volumeDisplay.text = "---";
        }

        sameValue = true;
        int panPercent = notes[0].panPercent;
        for (int i = 1; i < notes.Count; i++)
        {
            if (notes[i].panPercent != panPercent)
            {
                sameValue = false;
            }
        }
        if (sameValue)
        {
            panDisplay.text = panPercent + "%";
        }
        else
        {
            panDisplay.text = "---";
        }
    }

    public void OnSliderValueChanged()
    {
        volumeDisplay.text = volumeSlider.value + "%";
        panDisplay.text = panSlider.value + "%";
    }

    public void OnVolumeSliderEndEdit(float newValue)
    {
        EditorContext.BeginTransaction();
        foreach (Note n in notes)
        {
            EditOperation op = EditorContext
                .BeginModifyNoteOperation();
            op.noteBeforeOp = n.Clone();
            n.volumePercent = Mathf.FloorToInt(newValue);
            op.noteAfterOp = n.Clone();
        }
        EditorContext.EndTransaction();

        RefreshVolumeAndPanDisplay();
    }

    public void OnPanSliderEndEdit(float newValue)
    {
        EditorContext.BeginTransaction();
        foreach (Note n in notes)
        {
            EditOperation op = EditorContext
                .BeginModifyNoteOperation();
            op.noteBeforeOp = n.Clone();
            n.panPercent = Mathf.FloorToInt(newValue);
            op.noteAfterOp = n.Clone();
        }
        EditorContext.EndTransaction();

        RefreshVolumeAndPanDisplay();
    }

    public void OnEndOfScanToggleValueChanged(bool newValue)
    {
        patternPanel.SetEndOfScanOnSelectedNotes(newValue);
    }

    public void OnBSplineToggleValueChanged(bool newValue)
    {
        EditorContext.BeginTransaction();
        foreach (Note n in notes)
        {
            EditOperation op = EditorContext
                .BeginModifyNoteOperation();
            op.noteBeforeOp = n.Clone();
            (n as DragNote).curveType = newValue ?
                CurveType.BSpline : CurveType.Bezier;
            op.noteAfterOp = n.Clone();
        }
        EditorContext.EndTransaction();

        foreach (Note n in selection)
        {
            patternPanel.workspace.RefreshDragNoteCurve(n);
        }
    }
}
