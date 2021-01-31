using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EditorOptionsTab : MonoBehaviour
{
    [Header("Appearance")]
    public Toggle showKeysoundsToggle;

    [Header("Editing")]
    public Toggle applyKeysoundToSelectionToggle;
    public Toggle applyNoteTypeToSelectionToggle;
    public Toggle lockNotesInTimeToggle;
    public Toggle snapDragAnchorsToggle;

    [Header("Playback")]
    public Toggle metronomeToggle;
    public Toggle assistTickOnSilentNotesToggle;
    public Toggle returnScanlineAfterPlaybackToggle;
    public Toggle continousScrollDuringPlaybackToggle;

    public static event UnityAction Opened;
    public static event UnityAction Closed;

    private void OnEnable()
    {
        Opened?.Invoke();
        MemoryToUI();
    }

    private void OnDisable()
    {
        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
        Closed?.Invoke();
    }

    private void MemoryToUI()
    {
        showKeysoundsToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.showKeysounds);

        applyKeysoundToSelectionToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.applyKeysoundToSelection);
        applyNoteTypeToSelectionToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.applyNoteTypeToSelection);
        lockNotesInTimeToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.lockNotesInTime);
        snapDragAnchorsToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.snapDragAnchors);

        metronomeToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.metronome);
        assistTickOnSilentNotesToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.assistTickOnSilentNotes);
        returnScanlineAfterPlaybackToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions
            .returnScanlineAfterPlayback);
        continousScrollDuringPlaybackToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions
            .continousScrollDuringPlayback);
    }

    public void UIToMemory()
    {
        Options.instance.editorOptions.showKeysounds =
            showKeysoundsToggle.isOn;

        Options.instance.editorOptions.applyKeysoundToSelection =
            applyKeysoundToSelectionToggle.isOn;
        Options.instance.editorOptions.applyNoteTypeToSelection =
            applyNoteTypeToSelectionToggle.isOn;
        Options.instance.editorOptions.lockNotesInTime =
            lockNotesInTimeToggle.isOn;
        Options.instance.editorOptions.snapDragAnchors =
            snapDragAnchorsToggle.isOn;

        Options.instance.editorOptions.metronome =
            metronomeToggle.isOn;
        Options.instance.editorOptions.assistTickOnSilentNotes =
            assistTickOnSilentNotesToggle.isOn;
        Options.instance.editorOptions.returnScanlineAfterPlayback =
            returnScanlineAfterPlaybackToggle.isOn;
        Options.instance.editorOptions.continousScrollDuringPlayback =
            continousScrollDuringPlaybackToggle.isOn;
    }
}
