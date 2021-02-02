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
    public Button previewButton;
    public GameObject endOfScanOptions;
    public Toggle endOfScanToggle;
    public PatternPanel patternPanel;

    private List<Note> notes;

    private void OnEnable()
    {
        PatternPanel.SelectionChanged += OnSelectionChanged;
        // TODO: don't do this when the user hides, and then shows
        // this sheet.
        OnSelectionChanged(new HashSet<GameObject>());
    }

    private void OnDisable()
    {
        PatternPanel.SelectionChanged -= OnSelectionChanged;
    }

    private void OnSelectionChanged(HashSet<GameObject> newSelection)
    {
        if (newSelection.Count == 0)
        {
            noSelectionNotice.SetActive(true);
            contents.SetActive(false);
            return;
        }
        noSelectionNotice.SetActive(false);
        contents.SetActive(true);

        bool multiple = newSelection.Count > 1;
        notes = new List<Note>();
        foreach (GameObject o in newSelection)
        {
            notes.Add(o.GetComponent<NoteObject>().note);
        }

        if (!multiple)
        {
            volumeSlider.SetValueWithoutNotify(notes[0].volume * 100f);
            panSlider.SetValueWithoutNotify(notes[0].pan * 100f);
        }
        RefreshDisplays();
        previewButton.interactable = !multiple;

        bool allNotesOnScanDividers = true;
        int pulsesPerScan = Pattern.pulsesPerBeat *
            EditorContext.Pattern.patternMetadata.bps;
        foreach (Note n in notes)
        {
            if (n.pulse % pulsesPerScan != 0)
            {
                allNotesOnScanDividers = false;
                break;
            }
        }
        endOfScanOptions.SetActive(allNotesOnScanDividers);
        if (allNotesOnScanDividers)
        {
            endOfScanToggle.SetIsOnWithoutNotify(notes[0].endOfScan);
        }
    }

    // Assumes at least 1 note selected.
    private void RefreshDisplays()
    {
        bool sameValue = true;
        float volume = notes[0].volume;
        for (int i = 1; i < notes.Count; i++)
        {
            if (notes[i].volume != volume)
            {
                sameValue = false;
            }
        }
        if (sameValue)
        {
            volumeDisplay.text = Mathf.FloorToInt(volume * 100f) + "%";
        }
        else
        {
            volumeDisplay.text = "---";
        }

        sameValue = true;
        float pan = notes[0].pan;
        for (int i = 1; i < notes.Count; i++)
        {
            if (notes[i].pan != pan)
            {
                sameValue = false;
            }
        }
        if (sameValue)
        {
            panDisplay.text = Mathf.FloorToInt(pan * 100f) + "%";
        }
        else
        {
            panDisplay.text = "---";
        }
    }

    public void OnVolumeSliderValueChanged(float newValue)
    {
        EditorContext.PrepareForChange();
        notes.ForEach(n => n.volume = newValue * 0.01f);
        EditorContext.DoneWithChange();

        RefreshDisplays();
    }

    public void OnPanSliderValueChanged(float newValue)
    {
        EditorContext.PrepareForChange();
        notes.ForEach(n => n.pan = newValue * 0.01f);
        EditorContext.DoneWithChange();

        RefreshDisplays();
    }

    public void OnPreviewButtonClick()
    {
        patternPanel.PlayKeysound(notes[0]);
    }

    public void OnEndOfScanToggleValueChanged(bool newValue)
    {
        EditorContext.PrepareForChange();
        notes.ForEach(n => n.endOfScan = newValue);
        EditorContext.DoneWithChange();
    }
}
