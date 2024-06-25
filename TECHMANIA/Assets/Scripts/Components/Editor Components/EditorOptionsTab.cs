using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class EditorOptionsTab : MonoBehaviour
{
    [Header("Appearance")]
    public Toggle showKeysoundsToggle;
    public Toggle keepScanlineInViewToggle;

    [Header("Editing")]
    public Toggle applyKeysoundToSelectionToggle;
    public Toggle applyNoteTypeToSelectionToggle;
    public Toggle lockNotesInTimeToggle;
    public Toggle lockDragAnchorsInTimeToggle;
    public Toggle snapDragAnchorsToggle;
    public Toggle autoSaveToggle;

    [Header("Playback")]
    public Toggle metronomeToggle;
    public Toggle assistTickOnSilentNotesToggle;
    public Toggle returnScanlineAfterPlaybackToggle;

    public static event UnityAction Opened;
    public static event UnityAction Closed;

    private void OnEnable()
    {
        Opened?.Invoke();
        MemoryToUI();
    }

    private void OnDisable()
    {
        Closed?.Invoke();
    }

    private void MemoryToUI()
    {
        showKeysoundsToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.showKeysounds);
        keepScanlineInViewToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.keepScanlineInView);

        applyKeysoundToSelectionToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.applyKeysoundToSelection);
        applyNoteTypeToSelectionToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.applyNoteTypeToSelection);
        lockNotesInTimeToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.lockNotesInTime);
        lockDragAnchorsInTimeToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.lockDragAnchorsInTime);
        snapDragAnchorsToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.snapDragAnchors);
        autoSaveToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.autoSave);

        metronomeToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.metronome);
        assistTickOnSilentNotesToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions.assistTickOnSilentNotes);
        returnScanlineAfterPlaybackToggle.SetIsOnWithoutNotify(
            Options.instance.editorOptions
            .returnScanlineAfterPlayback);
    }

    public void UIToMemory()
    {
        Options.instance.editorOptions.showKeysounds =
            showKeysoundsToggle.isOn;
        Options.instance.editorOptions.keepScanlineInView =
            keepScanlineInViewToggle.isOn;

        Options.instance.editorOptions.applyKeysoundToSelection =
            applyKeysoundToSelectionToggle.isOn;
        Options.instance.editorOptions.applyNoteTypeToSelection =
            applyNoteTypeToSelectionToggle.isOn;
        Options.instance.editorOptions.lockNotesInTime =
            lockNotesInTimeToggle.isOn;
        Options.instance.editorOptions.lockDragAnchorsInTime =
            lockDragAnchorsInTimeToggle.isOn;
        Options.instance.editorOptions.snapDragAnchors =
            snapDragAnchorsToggle.isOn;
        Options.instance.editorOptions.autoSave =
            autoSaveToggle.isOn;

        Options.instance.editorOptions.metronome =
            metronomeToggle.isOn;
        Options.instance.editorOptions.assistTickOnSilentNotes =
            assistTickOnSilentNotesToggle.isOn;
        Options.instance.editorOptions.returnScanlineAfterPlayback =
            returnScanlineAfterPlaybackToggle.isOn;
    }
}
