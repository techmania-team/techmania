using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PatternPanel : MonoBehaviour
{
    public RectTransform rootCanvas;
    public CanvasGroup canvasGroup;

    [Header("Audio")]
    public AudioManager audioManager;
    public AudioClip metronome1;
    public AudioClip metronome2;
    public AudioClip assistTick;
    private FmodSoundWrap metronome1Sound;
    private FmodSoundWrap metronome2Sound;
    private FmodSoundWrap assistTickSound;

    [Header("UI")]
    public GameObject optionsTab;
    public PatternPanelToolbar toolbar;
    public PatternPanelWorkspace workspace;
    public PatternPanelPlaybackBar playbackBar;
    public KeysoundSideSheet keysoundSheet;
    public Snackbar snackbar;
    public MessageDialog messageDialog;

    private float? scanlinePulseBeforePreview;

    #region Internal data structures
    public HashSet<Note> selectedNotes;

    // Clipboard stores notes instead of GameObjects,
    // so we are free of Unity stuff such as MonoBehaviors and
    // Instantiating.
    //
    // The clipboard is intentionally not initialized in OnEnabled,
    // so it is preserved between editing sessions, and across
    // patterns.
    private List<Note> clipboard;
    private int minPulseInClipboard;

    public NoteType noteType { get; private set; }

    public enum Tool
    {
        Pan,
        Note,
        Rectangle,
        RectangleAppend,
        RectangleSubtract,
        Anchor
    }
    public static Tool tool;

    public bool UsingRectangleTool()
    {
        return tool == Tool.Rectangle ||
            tool == Tool.RectangleAppend ||
            tool == Tool.RectangleSubtract;
    }

    private Note GetNoteFromGameObject(GameObject o)
    {
        return o.GetComponent<NoteObject>().note;
    }
    #endregion

    #region Vertical spacing
    public static int PlayableLanes => 
        EditorContext.Pattern.patternMetadata.playableLanes;
    #endregion

    #region Outward events
    public static event UnityAction<HashSet<Note>> 
        SelectionChanged;
    public static event UnityAction KeysoundVisibilityChanged;
    public static event UnityAction PlaybackStarted;
    public static event UnityAction PlaybackStopped;
    #endregion

    #region MonoBehavior APIs
    private void Start()
    {
        metronome1Sound = FmodManager.CreateSoundFromAudioClip(
            metronome1);
        metronome2Sound = FmodManager.CreateSoundFromAudioClip(
            metronome2);
        assistTickSound = FmodManager.CreateSoundFromAudioClip(
            assistTick);
    }

    // This deletes and respawns everything, therefore is extremely
    // slow.
    private void Refresh()
    {
        workspace.DestroyAndSpawnExistingNotes();
        workspace.UpdateNumScansAndRelatedUI();
        playbackBar.Refresh();
    }

    private void OnEnable()
    {
        Options.RefreshInstance();

        // UI and options
        tool = Tool.Note;
        noteType = NoteType.Basic;
        toolbar.RefreshToolAndNoteTypeButtons();
        toolbar.RefreshBeatSnapDivisorDisplay();
        keysoundSheet.Initialize();

        // Playback
        audioLoaded = false;
        isPlaying = false;
        playbackBar.UpdatePlaybackUI();
        ResourceLoader.CacheAudioResources(
            EditorContext.trackFolder,
            cacheAudioCompleteCallback: OnResourceLoadComplete);

        // UI parts
        toolbar.InternalOnEnable();
        workspace.InternalOnEnable();
        playbackBar.InternalOnEnable();

        Refresh();
        EditorContext.UndoInvoked += OnUndo;
        EditorContext.RedoInvoked += OnRedo;
        KeysoundSideSheet.selectedKeysoundsUpdated += 
            OnSelectedKeysoundsUpdated;
        EditorOptionsTab.Opened += OnOptionsTabOpened;
        EditorOptionsTab.Closed += OnOptionsTabClosed;

        // Restore editing session if returning from preview
        if (scanlinePulseBeforePreview.HasValue)
        {
            workspace.scanlineFloatPulse = 
                scanlinePulseBeforePreview.Value;
            workspace.ScrollScanlineIntoView();
            scanlinePulseBeforePreview = null;
            playbackBar.Refresh();
        }

        DiscordController.SetActivity(
            DiscordActivityType.EditorPattern);
    }

    private void OnDisable()
    {
        // UI parts
        toolbar.InternalOnDisable();
        workspace.InternalOnDisable();
        playbackBar.InternalOnDisable();

        StopPlayback();
        EditorContext.UndoInvoked -= OnUndo;
        EditorContext.RedoInvoked -= OnRedo;
        KeysoundSideSheet.selectedKeysoundsUpdated -= 
            OnSelectedKeysoundsUpdated;
        EditorOptionsTab.Opened -= OnOptionsTabOpened;
        EditorOptionsTab.Closed -= OnOptionsTabClosed;

        Options.instance.SaveToFile(Paths.GetOptionsFilePath());
    }

    // Update is called once per frame
    void Update()
    {
        if (isPlaying)
        {
            UpdatePlayback();
        }
        if (messageDialog.gameObject.activeSelf ||
            toolbar.timeEventDialog.gameObject.activeSelf ||
            optionsTab.activeSelf)
        {
            return;
        }

        if (Input.mouseScrollDelta.y != 0)
        {
            HandleMouseScroll(Input.mouseScrollDelta.y);
        }

        HandleKeyboardShortcuts();
    }
    #endregion

    #region Undo and redo
    private void OnUndo(EditTransaction transaction)
    {
        // Undo operations in reverse order.
        for (int opIndex = transaction.ops.Count - 1;
            opIndex >= 0;
            opIndex--)
        {
            EditOperation op = transaction.ops[opIndex];
            switch (op.type)
            {
                case EditOperation.Type.TrackOrPatternMetadata:
                    workspace.OnPatternTimingUpdated();
                    break;
                case EditOperation.Type.TimeEvent:
                    workspace.DestroyAndRespawnAllMarkers();
                    break;
                case EditOperation.Type.AddNote:
                    {
                        Note n = EditorContext.Pattern.GetNoteAt(
                            op.addedNote.pulse,
                            op.addedNote.lane);
                        if (n == null)
                        {
                            Debug.LogError("Note not found when trying to undo AddNote.");
                            break;
                        }
                        selectedNotes.Remove(n);
                        DeleteNote(n);
                    }
                    break;
                case EditOperation.Type.DeleteNote:
                    {
                        Note n = op.deletedNote;
                        AddExistingNote(n);
                    }
                    break;
                case EditOperation.Type.ModifyNote:
                    {
                        Note n = EditorContext.Pattern.GetNoteAt(
                            op.noteAfterOp.pulse,
                            op.noteAfterOp.lane);
                        if (n == null)
                        {
                            Debug.LogError("Note not found when trying to undo ModifyNote.");
                            break;
                        }
                        n.CopyFrom(op.noteBeforeOp);
                        workspace.RefreshNoteInEditor(n);
                    }
                    break;
            }
        }
        // To update note detail sheet.
        NotifySelectionChanged();
    }

    private void OnRedo(EditTransaction transaction)
    {
        foreach (EditOperation op in transaction.ops)
        {
            switch (op.type)
            {
                case EditOperation.Type.TrackOrPatternMetadata:
                    workspace.OnPatternTimingUpdated();
                    break;
                case EditOperation.Type.TimeEvent:
                    workspace.DestroyAndRespawnAllMarkers();
                    break;
                case EditOperation.Type.AddNote:
                    {
                        Note n = op.addedNote;
                        AddExistingNote(n);
                    }
                    break;
                case EditOperation.Type.DeleteNote:
                    {
                        Note n = EditorContext.Pattern.GetNoteAt(
                            op.deletedNote.pulse,
                            op.deletedNote.lane);
                        if (n == null)
                        {
                            Debug.LogError("Note not found when trying to redo DeleteNote.");
                            break;
                        }
                        selectedNotes.Remove(n);
                        DeleteNote(n);
                    }
                    break;
                case EditOperation.Type.ModifyNote:
                    {
                        Note n = EditorContext.Pattern.GetNoteAt(
                            op.noteBeforeOp.pulse,
                            op.noteBeforeOp.lane);
                        if (n == null)
                        {
                            Debug.LogError("Note not found when trying to redo ModifyNote.");
                            break;
                        }
                        n.CopyFrom(op.noteAfterOp);
                        workspace.RefreshNoteInEditor(n);
                    }
                    break;
            }
        }
        // To update note detail sheet.
        NotifySelectionChanged();
    }
    #endregion

    #region Mouse and keyboard update
    private void HandleMouseScroll(float y)
    {
        if (Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt))
        {
            // Change beat snap divisor.
            toolbar.OnBeatSnapDivisorChanged(y < 0f ? -1 : 1);
        }
    }

    private void HandleKeyboardShortcuts()
    {
        if (Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl))
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                SelectAll();
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                SelectNone();
            }
            if (Input.GetKeyDown(KeyCode.X))
            {
                CutSelection();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                CopySelection();
            }
            if (Input.GetKeyDown(KeyCode.V))
            {
                PasteAtScanline();
            }
            if (Input.GetKeyDown(KeyCode.E))
            {
                ToggleEndOfScanOnSelectedNotes();
            }
        }
        if (Input.GetKeyDown(KeyCode.Delete))
        {
            DeleteSelection();
        }
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (isPlaying)
            {
                StopPlayback();
            }
            else
            {
                StartPlayback();
            }
        }
        if (Input.GetKeyDown(KeyCode.BackQuote))
        {
            toolbar.OnRectangleToolButtonClick();
        }
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ChangeNoteType(NoteType.Basic);
        if (Input.GetKeyDown(KeyCode.Alpha2))
            ChangeNoteType(NoteType.ChainHead);
        if (Input.GetKeyDown(KeyCode.Alpha3))
            ChangeNoteType(NoteType.ChainNode);
        if (Input.GetKeyDown(KeyCode.Alpha4))
            ChangeNoteType(NoteType.Drag);
        if (Input.GetKeyDown(KeyCode.Alpha5))
            ChangeNoteType(NoteType.Hold);
        if (Input.GetKeyDown(KeyCode.Alpha6))
            ChangeNoteType(NoteType.RepeatHead);
        if (Input.GetKeyDown(KeyCode.Alpha7))
            ChangeNoteType(NoteType.Repeat);
        if (Input.GetKeyDown(KeyCode.Alpha8))
            ChangeNoteType(NoteType.RepeatHeadHold);
        if (Input.GetKeyDown(KeyCode.Alpha9))
            ChangeNoteType(NoteType.RepeatHold);

        if (Input.GetKeyDown(KeyCode.P))
        {
            if (selectedNotes.Count == 1 && !isPlaying)
            {
                foreach (Note n in selectedNotes)
                {
                    PreviewKeysound(n);
                }
            }
        }
    }
    #endregion
    
    #region UI events
    public void ChangeNoteType(NoteType newType)
    {
        tool = Tool.Note;
        noteType = newType;

        // Apply to selection if asked to.
        if (Options.instance.editorOptions
            .applyNoteTypeToSelection &&
            !isPlaying &&
            selectedNotes.Count > 0)
        {
            ApplyNoteTypeToSelection(newType);
        }
    }

    public void ChangeTimeEvent(int pulse,
        double? newBpm, int? newTimeStopPulses)
    {
        EditorContext.PrepareToModifyTimeEvent();
        // Delete event.
        EditorContext.Pattern.bpmEvents.RemoveAll((BpmEvent e) =>
        {
            return e.pulse == pulse;
        });
        EditorContext.Pattern.timeStops.RemoveAll((TimeStop t) =>
        {
            return t.pulse == pulse;
        });
        // Add event if there is one.
        if (newBpm.HasValue)
        {
            EditorContext.Pattern.bpmEvents.Add(new BpmEvent()
            {
                pulse = pulse,
                bpm = newBpm.Value
            });
        }
        if (newTimeStopPulses.HasValue)
        {
            EditorContext.Pattern.timeStops.Add(new TimeStop()
            {
                pulse = pulse,
                duration = newTimeStopPulses.Value
            });
        }
    }

    private void OnSelectedKeysoundsUpdated(List<string> keysounds)
    {
        if (selectedNotes == null) return;
        if (Options.instance.editorOptions
            .applyKeysoundToSelection &&
            !isPlaying &&
            selectedNotes.Count > 0)
        {
            ApplyKeysoundsToSelection(keysounds);
        }
    }

    private void OnOptionsTabOpened()
    {
        canvasGroup.alpha = 0f;
    }

    private void OnOptionsTabClosed()
    {
        canvasGroup.alpha = 1f;
        KeysoundVisibilityChanged?.Invoke();
    }

    public void RecordScanlinePulseBeforePreview()
    {
        scanlinePulseBeforePreview = workspace.scanlineFloatPulse;
    }
    #endregion

    #region Note utilities
    private void GetPreviousAndNextNotes(
        Note n, HashSet<NoteType> types,
        int minLaneInclusive, int maxLaneInclusive,
        out Note prev, out Note next)
    {
        prev = EditorContext.Pattern
            .GetClosestNoteBefore(n.pulse, types,
            minLaneInclusive,
            maxLaneInclusive);
        next = EditorContext.Pattern
            .GetClosestNoteAfter(n.pulse, types,
            minLaneInclusive,
            maxLaneInclusive);
    }

    public void GetPreviousAndNextChainNotes(Note n,
        out Note prev, out Note next)
    {
        GetPreviousAndNextNotes( n,
            new HashSet<NoteType>()
                { NoteType.ChainHead, NoteType.ChainNode },
            minLaneInclusive: 0,
            maxLaneInclusive: PlayableLanes - 1,
            out prev, out next);
    }

    public void GetPreviousAndNextRepeatNotes(Note n,
        out Note prev, out Note next)
    {
        GetPreviousAndNextNotes(n,
            new HashSet<NoteType>()
                { NoteType.RepeatHead,
                NoteType.RepeatHeadHold,
                NoteType.Repeat,
                NoteType.RepeatHold},
            minLaneInclusive: n.lane,
            maxLaneInclusive: n.lane,
            out prev, out next);
    }

    private int HoldNoteDefaultDuration(int pulse, int lane)
    {
        Note noteAfterPivot = EditorContext.Pattern
            .GetClosestNoteAfter(
                pulse, types: null,
                minLaneInclusive: lane,
                maxLaneInclusive: lane);
        if (noteAfterPivot != null)
        {
            int nextPulse = noteAfterPivot.pulse;
            if (nextPulse - pulse <= Pattern.pulsesPerBeat)
            {
                return nextPulse - pulse - 1;
            }
        }
        return Pattern.pulsesPerBeat;
    }
    #endregion

    #region Can we add a note here?
    private bool CanAddNote(NoteType type, int pulse, int lane,
        out string reason)
    {
        return CanAddNote(type, pulse, lane, null, out reason);
    }

    private bool CanAddNote(NoteType type, int pulse, int lane,
        HashSet<Note> ignoredExistingNotes,
        out string reason)
    {
        return EditorContext.Pattern.CanAddNote(
            type, pulse, lane, PatternPanelWorkspace.TotalLanes,
            ignoredExistingNotes, out reason);
    }

    private bool CanAddHoldNote(NoteType type, int pulse, int lane,
        int duration, HashSet<Note> ignoredExistingNotes,
        out string reason)
    {
        return EditorContext.Pattern.CanAddHoldNote(
            type, pulse, lane, PatternPanelWorkspace.TotalLanes, duration,
            ignoredExistingNotes, out reason);
    }

    private bool CanAddDragNote(int pulse, int lane,
        List<DragNode> nodes,
        HashSet<Note> ignoredExistingNotes,
        out string reason)
    {
        return CanAddNote(NoteType.Drag, pulse, lane,
            ignoredExistingNotes, out reason);
    }
    #endregion

    #region Adding notes
    // Called at the end of Add(|Hold|Drag)Note.
    private GameObject FinishAddNote(Note n)
    {
        // Add to pattern.
        EditorContext.Pattern.notes.Add(n);

        // Add to UI. SpawnNoteObject will add n to
        // noteToNoteObject.
        return workspace.CreateNewNoteObject(n);
    }

    // The Add(|Hold|Drag)Note methods add the note to the pattern
    // and UI, but do NOT create a transaction. That's up to the caller.
    private GameObject AddNote(NoteType type, int pulse, int lane,
        string sound,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan,
        bool endOfScan = false)
    {
        Note n = new Note()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,
            volumePercent = volumePercent,
            panPercent = panPercent,
            endOfScan = endOfScan
        };
        return FinishAddNote(n);
    }

    private GameObject AddHoldNote(NoteType type,
        int pulse, int lane, int? duration, string sound,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan,
        bool endOfScan = false)
    {
        if (!duration.HasValue)
        {
            if (workspace.lastClickedNote != null &&
                workspace.lastClickedNote.type == type)
            {
                // Attempt to inherit duration of last clicked note.
                if (CanAddHoldNote(type, pulse, lane,
                    (workspace.lastClickedNote as HoldNote).duration,
                    null, out _))
                {
                    duration = (workspace.lastClickedNote
                        as HoldNote).duration;
                }
            }

            if (!duration.HasValue)
            {
                // If the above failed, then use default duration.
                duration = HoldNoteDefaultDuration(pulse, lane);
            }
        }
        HoldNote n = new HoldNote()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,
            duration = duration.Value,
            volumePercent = volumePercent,
            panPercent = panPercent,
            endOfScan = endOfScan
        };
        return FinishAddNote(n);
    }

    private GameObject AddDragNote(int pulse, int lane,
        List<DragNode> nodes, string sound,
        int volumePercent = Note.defaultVolume,
        int panPercent = Note.defaultPan,
        CurveType curveType = CurveType.Bezier)
    {
        if (nodes == null)
        {
            nodes = new List<DragNode>();
            if (workspace.lastClickedNote != null &&
                workspace.lastClickedNote.type == NoteType.Drag)
            {
                // Inherit nodes from the last clicked note.
                foreach (DragNode node in
                    (workspace.lastClickedNote as DragNote).nodes)
                {
                    nodes.Add(node.Clone());
                }
            }
            else
            {
                // Calculate default duration as hold note and
                // use that to create node #1.
                int relativePulseOfLastNode =
                    HoldNoteDefaultDuration(pulse, lane);
                nodes.Add(new DragNode()
                {
                    anchor = new FloatPoint(0f, 0f),
                    controlLeft = new FloatPoint(0f, 0f),
                    controlRight = new FloatPoint(0f, 0f)
                });
                nodes.Add(new DragNode()
                {
                    anchor = new FloatPoint(relativePulseOfLastNode, 0f),
                    controlLeft = new FloatPoint(0f, 0f),
                    controlRight = new FloatPoint(0f, 0f)
                });
            }
        }
        DragNote n = new DragNote()
        {
            type = NoteType.Drag,
            pulse = pulse,
            lane = lane,
            sound = sound,
            nodes = nodes,
            volumePercent = volumePercent,
            panPercent = panPercent,
            curveType = curveType
        };
        return FinishAddNote(n);
    }

    // Called from undo and redo.
    // Calls one of AddNote, AddHoldNote and AddDragNote, without
    // validation.
    private void AddExistingNote(Note n)
    {
        switch (n.type)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.RepeatHead:
            case NoteType.Repeat:
                AddNote(n.type, n.pulse, n.lane, n.sound,
                    n.volumePercent, n.panPercent, n.endOfScan);
                break;
            case NoteType.Hold:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                AddHoldNote(n.type, n.pulse, n.lane,
                    (n as HoldNote).duration, n.sound,
                    n.volumePercent, n.panPercent, n.endOfScan);
                break;
            case NoteType.Drag:
                AddDragNote(n.pulse, n.lane,
                    (n as DragNote).nodes, n.sound,
                    n.volumePercent, n.panPercent,
                    (n as DragNote).curveType);
                break;
        }
    }

    // The only public method to add a note.
    // Called from workspace.
    // Calls one of AddNote, AddHoldNote and AddDragNote.
    // Will create a transaction.
    // Will show snack bar if the added note is invalid.
    public void AddNoteOfCurrentTypeAsTransaction(int pulse, int lane)
    {
        string invalidReason;
        // No need to call CanAddHoldNote or CanAddDragNote here
        // because durations and curves are flexible.
        if (!CanAddNote(noteType, pulse, lane, out invalidReason))
        {
            snackbar.Show(invalidReason);
            return;
        }

        string sound = keysoundSheet.UpcomingKeysound();
        keysoundSheet.AdvanceUpcoming();
        EditorContext.BeginTransaction();
        GameObject newNote = null;
        switch (noteType)
        {
            case NoteType.Basic:
            case NoteType.ChainHead:
            case NoteType.ChainNode:
            case NoteType.RepeatHead:
            case NoteType.Repeat:
                newNote = AddNote(noteType, pulse, lane, sound);
                break;
            case NoteType.Hold:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                newNote = AddHoldNote(noteType, pulse, lane,
                    duration: null, sound);
                break;
            case NoteType.Drag:
                newNote = AddDragNote(pulse, lane,
                    nodes: null, sound);
                break;
        }

        EditorContext.RecordAddedNote(GetNoteFromGameObject(newNote));
        EditorContext.EndTransaction();
    }
    #endregion

    #region Editing notes
    // Intended to be called from workspace.
    // Will show snack bar if the move is invalid.
    public void ChangeHoldNoteDurationAsTransaction(
        List<Note> holdNotes, int deltaDuration)
    {
        // Is the adjustment valid?
        bool adjustable = true;
        foreach (Note n in holdNotes)
        {
            HoldNote holdNote = n as HoldNote;
            int oldDuration = holdNote.duration;
            int newDuration = oldDuration + deltaDuration;
            string reason;
            if (!EditorContext.Pattern.CanAdjustHoldNoteDuration(
                holdNote, newDuration, out reason))
            {
                snackbar.Show(reason);
                adjustable = false;
                break;
            }
        }

        if (adjustable)
        {
            // Apply adjustment. No need to delete and respawn notes
            // this time.
            EditorContext.BeginTransaction();
            foreach (Note n in holdNotes)
            {
                EditOperation op = EditorContext
                    .BeginModifyNoteOperation();
                HoldNote holdNote = n as HoldNote;
                op.noteBeforeOp = holdNote.Clone();
                holdNote.duration += deltaDuration;
                op.noteAfterOp = holdNote.Clone();
            }
            EditorContext.EndTransaction();
        }
    }

    // Will create transaction, add and delete notes, and reset selection.
    private void ApplyNoteTypeToSelection(NoteType newType)
    {
        HashSet<Note> newSelection = new HashSet<Note>();
        EditorContext.BeginTransaction();
        foreach (Note n in selectedNotes)
        {
            int pulse = n.pulse;
            int lane = n.lane;
            string sound = n.sound;
            int volumePercent = n.volumePercent;
            int panPercent = n.panPercent;
            bool endOfScan = n.endOfScan;

            // Inherit the previous duration if applicable.
            int currentDuration = 0;
            if (n is HoldNote)
            {
                currentDuration = (n as HoldNote).duration;
            }
            if (n is DragNote)
            {
                currentDuration = (n as DragNote).Duration();
            }

            GameObject newObject = null;
            Note newNote = null;
            string invalidReason = "";
            switch (noteType)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    if (!CanAddNote(noteType, pulse, lane,
                        ignoredExistingNotes:
                        new HashSet<Note>() { n },
                        out invalidReason))
                    {
                        snackbar.Show(invalidReason);
                        break;
                    }
                    EditorContext.RecordDeletedNote(n.Clone());
                    DeleteNote(n);
                    newObject = AddNote(noteType, pulse, lane, sound,
                        volumePercent, panPercent, endOfScan);
                    newNote = GetNoteFromGameObject(newObject);
                    EditorContext.RecordAddedNote(newNote);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    if (currentDuration == 0)
                    {
                        currentDuration = HoldNoteDefaultDuration(
                            pulse, lane);
                    }
                    EditorContext.RecordDeletedNote(n.Clone());
                    DeleteNote(n);
                    newObject = AddHoldNote(noteType, pulse, lane,
                        duration: currentDuration, sound,
                        volumePercent, panPercent, endOfScan);
                    newNote = GetNoteFromGameObject(newObject);
                    EditorContext.RecordAddedNote(newNote);
                    break;
                case NoteType.Drag:
                    if (currentDuration == 0)
                    {
                        currentDuration = HoldNoteDefaultDuration(
                            pulse, lane);
                    }
                    List<DragNode> nodes = new List<DragNode>();
                    nodes.Add(new DragNode());
                    nodes.Add(new DragNode());
                    nodes[1].anchor.pulse = currentDuration;
                    EditorContext.RecordDeletedNote(n.Clone());
                    DeleteNote(n);
                    newObject = AddDragNote(pulse, lane,
                        nodes, sound, volumePercent, panPercent);
                    newNote = GetNoteFromGameObject(newObject);
                    EditorContext.RecordAddedNote(newNote);
                    break;
            }

            if (newNote != null)
            {
                newSelection.Add(newNote);
            }
            else
            {
                newSelection.Add(n);
            }
        }
        EditorContext.EndTransaction();

        selectedNotes = newSelection;
        NotifySelectionChanged();
    }

    // Will create transaction, modify notes, and refresh keysound display.
    private void ApplyKeysoundsToSelection(List<string> keysounds)
    {
        if (keysounds.Count == 0)
        {
            keysounds.Add("");
        }

        // Sort selected notes, first by pulse, then by lane.
        List<Note> sortedSelection = new List<Note>();
        foreach (Note n in selectedNotes)
        {
            sortedSelection.Add(n);
        }
        sortedSelection.Sort((Note n1, Note n2) =>
        {
            if (n1.pulse != n2.pulse) return n1.pulse - n2.pulse;
            return n1.lane - n2.lane;
        });

        // Apply keysound.
        EditorContext.BeginTransaction();
        int keysoundIndex = 0;
        foreach (Note n in sortedSelection)
        {
            EditOperation op = EditorContext
                .BeginModifyNoteOperation();
            op.noteBeforeOp = n.Clone();
            n.sound = keysounds[keysoundIndex];
            op.noteAfterOp = n.Clone();

            keysoundIndex = (keysoundIndex + 1) % keysounds.Count;
        }
        EditorContext.EndTransaction();

        // Refresh UI.
        foreach (Note n in sortedSelection)
        {
            workspace.RefreshKeysoundDisplay(n);
        }
    }

    // This one is public, as it can be called from NoteDetailSideSheet.
    // Will create transaction, modify notes and refresh end-of-scan
    // display.
    public void ApplyEndOfScanToSelection(bool newValue)
    {
        EditorContext.BeginTransaction();
        foreach (Note n in selectedNotes)
        {
            EditOperation op = EditorContext
                .BeginModifyNoteOperation();
            op.noteBeforeOp = n.Clone();
            n.endOfScan = newValue;
            op.noteAfterOp = n.Clone();
        }
        EditorContext.EndTransaction();

        foreach (Note n in selectedNotes)
        {
            workspace.RefreshEndOfScanIndicator(n);
        }
    }

    // Called from keyboard shortcut
    private void ToggleEndOfScanOnSelectedNotes()
    {
        if (selectedNotes.Count == 0) return;
        bool currentValue = false;
        foreach (Note n in selectedNotes)
        {
            currentValue = n.endOfScan;
            break;
        }
        ApplyEndOfScanToSelection(!currentValue);

        // Force the side sheet to update.
        NotifySelectionChanged();
    }

    // Intended to be called from workspace.
    // Will show snack bar if the move is invalid.
    public void MoveSelectedNotesAsTransaction(
        int deltaPulse, int deltaLane)
    {
        // Is the move valid?
        string invalidReason = "";
        foreach (Note n in selectedNotes)
        {
            int newPulse = n.pulse + deltaPulse;
            int newLane = n.lane + deltaLane;
            bool movable = true;

            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    movable = CanAddNote(n.type,
                        newPulse, newLane,
                        ignoredExistingNotes: selectedNotes,
                        out invalidReason);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    movable = CanAddHoldNote(n.type,
                        newPulse, newLane, (n as HoldNote).duration,
                        ignoredExistingNotes: selectedNotes,
                        out invalidReason);
                    break;
                case NoteType.Drag:
                    movable = CanAddDragNote(
                        newPulse, newLane, (n as DragNote).nodes,
                        ignoredExistingNotes: selectedNotes,
                        out invalidReason);
                    break;
            }

            if (!movable)
            {
                snackbar.Show(invalidReason);
                return;
            }
        }

        // Apply move. We need to delete and respawn note
        // objects, because they may have been moved between
        // playable and hidden lanes.
        EditorContext.BeginTransaction();
        HashSet<Note> replacedSelection =
            new HashSet<Note>();
        // These notes are not the ones added to the pattern.
        // They are created only to pass information to AddNote
        // methods.
        List<Note> movedNotes = new List<Note>();
        foreach (Note n in selectedNotes)
        {
            Note movedNote = n.Clone();
            movedNote.pulse += deltaPulse;
            movedNote.lane += deltaLane;
            movedNotes.Add(movedNote);

            EditorContext.RecordDeletedNote(n.Clone());
            DeleteNote(n);
        }
        foreach (Note movedNote in movedNotes)
        {
            GameObject o = null;
            switch (movedNote.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    o = AddNote(movedNote.type,
                        movedNote.pulse,
                        movedNote.lane,
                        movedNote.sound,
                        movedNote.volumePercent,
                        movedNote.panPercent,
                        movedNote.endOfScan);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    o = AddHoldNote(movedNote.type,
                        movedNote.pulse,
                        movedNote.lane,
                        (movedNote as HoldNote).duration,
                        movedNote.sound,
                        movedNote.volumePercent,
                        movedNote.panPercent,
                        movedNote.endOfScan);
                    break;
                case NoteType.Drag:
                    o = AddDragNote(
                        movedNote.pulse,
                        movedNote.lane,
                        (movedNote as DragNote).nodes,
                        movedNote.sound,
                        movedNote.volumePercent,
                        movedNote.panPercent,
                        (movedNote as DragNote).curveType);
                    break;
            }
            Note newNote = GetNoteFromGameObject(o);
            EditorContext.RecordAddedNote(newNote);
            replacedSelection.Add(newNote);
        }
        EditorContext.EndTransaction();
        selectedNotes = replacedSelection;
        NotifySelectionChanged();
    }
    #endregion

    #region Deleting notes
    // Does not create a transaction.
    // Cannot remove n from selectedNotes because the
    // caller may be enumerating that list.
    private void DeleteNote(Note n)
    {
        // Delete from UI, if it's there.
        workspace.DeleteNoteObject(n);

        // Delete from pattern.
        EditorContext.Pattern.notes.Remove(n);
    }

    // The only public method to delete a note.
    // Called from workspace.
    // Creates a transaction.
    public void DeleteNoteAsTransaction(Note n)
    {
        EditorContext.BeginTransaction();
        EditorContext.RecordDeletedNote(n);
        selectedNotes.Remove(n);
        DeleteNote(n);
        EditorContext.EndTransaction();
        NotifySelectionChanged();
    }
    #endregion

    #region Selection and clipboard
    public void NotifySelectionChanged()
    {
        SelectionChanged?.Invoke(selectedNotes);
    }

    public void ToggleSelection(Note n)
    {
        if (selectedNotes.Contains(n))
        {
            selectedNotes.Remove(n);
        }
        else
        {
            selectedNotes.Add(n);
        }
    }

    public void SelectAll()
    {
        selectedNotes.Clear();
        foreach (Note n in EditorContext.Pattern.notes)
        {
            selectedNotes.Add(n);
        }
        NotifySelectionChanged();

        // SelectAll is the only way of selection that can affect
        // notes outside the view port. So:
        workspace.RefreshNotesInViewport();
    }

    public void SelectNone()
    {
        selectedNotes.Clear();
        NotifySelectionChanged();
    }

    public void CutSelection()
    {
        if (selectedNotes.Count == 0) return;

        CopySelection();
        DeleteSelection();
    }

    public void CopySelection()
    {
        if (selectedNotes.Count == 0) return;

        if (clipboard == null)
        {
            clipboard = new List<Note>();
        }
        clipboard.Clear();
        minPulseInClipboard = int.MaxValue;
        foreach (Note n in selectedNotes)
        {
            if (n.pulse < minPulseInClipboard)
            {
                minPulseInClipboard = n.pulse;
            }
            clipboard.Add(n.Clone());
        }
    }

    public void PasteAtScanline()
    {
        if (clipboard == null) return;
        if (clipboard.Count == 0) return;
        if (isPlaying) return;

        int scanlinePulse = (int)workspace.scanlineFloatPulse;
        int deltaPulse = scanlinePulse - minPulseInClipboard;

        // Can we paste here?
        bool pastable = true;
        string invalidReason = "";
        foreach (Note n in clipboard)
        {
            int newPulse = n.pulse + deltaPulse;
            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    pastable = CanAddNote(n.type, newPulse,
                        n.lane, out invalidReason);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    pastable = CanAddHoldNote(n.type,
                        newPulse, n.lane,
                        (n as HoldNote).duration,
                        ignoredExistingNotes: null,
                        out invalidReason);
                    break;
                case NoteType.Drag:
                    pastable = CanAddDragNote(newPulse, n.lane,
                        (n as DragNote).nodes,
                        ignoredExistingNotes: null,
                        out invalidReason);
                    break;
            }
            if (!pastable)
            {
                snackbar.Show(invalidReason);
                return;
            }
        }

        // Paste.
        EditorContext.BeginTransaction();
        foreach (Note n in clipboard)
        {
            int newPulse = n.pulse + deltaPulse;
            GameObject newObject = null;
            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    newObject = AddNote(n.type, newPulse,
                        n.lane, n.sound,
                        n.volumePercent, n.panPercent, n.endOfScan);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    newObject = AddHoldNote(n.type, newPulse,
                        n.lane, (n as HoldNote).duration,
                        n.sound,
                        n.volumePercent, n.panPercent, n.endOfScan);
                    break;
                case NoteType.Drag:
                    newObject = AddDragNote(newPulse, n.lane,
                        (n as DragNote).nodes,
                        n.sound,
                        n.volumePercent, n.panPercent,
                        (n as DragNote).curveType);
                    break;
            }
            EditorContext.RecordAddedNote(
                GetNoteFromGameObject(newObject));
        }
        EditorContext.EndTransaction();
    }

    public void DeleteSelection()
    {
        if (selectedNotes.Count == 0) return;
        if (isPlaying) return;

        // Delete notes from pattern.
        EditorContext.BeginTransaction();
        foreach (Note n in selectedNotes)
        {
            EditorContext.RecordDeletedNote(n);
            DeleteNote(n);
        }
        EditorContext.EndTransaction();

        selectedNotes.Clear();
        NotifySelectionChanged();
    }
    #endregion

    #region Playback
    // During playback, the following features are disabled:
    // - All timing options
    // - Adding or deleting notes, including by clicking, dragging
    //   and cut/copy/paste
    // - Applying note types and/or keysounds to selection, if
    //   specified in options
    // - Moving the scanline, including by clicking the header
    //   and dragging the scanline position slider.
    public bool audioLoaded { get; private set; }
    public bool isPlaying { get; private set; }
    private float playbackStartingPulse;
    private float playbackStartingTime;
    private bool backingTrackPlaying;
    private DateTime systemTimeOnPlaybackStart;
    private float playbackBeatOnPreviousFrame;  // For metronome

    // When playing, sort all notes by pulse so it's easy to tell if
    // it's time to play the next note in the queue. Once played,
    // a note is removed from the queue.
    private Queue<Note> sortedNotesForPlayback;

    // Keep a reference to the FMOD channel playing a keysound preview
    // so we can stop a preview before starting the next one.
    private FmodChannelWrap keysoundPreviewChannel;

    private void OnResourceLoadComplete(Status status)
    {
        if (!status.Ok())
        {
            messageDialog.Show(L10n.GetStringAndFormatIncludingPaths(
                "pattern_panel_resource_loading_error_format",
                status.errorMessage));
        }
        audioLoaded = true;
        playbackBar.EnablePlayButton(status.Ok());
        playbackBar.UpdatePlaybackUI();
    }

    public void StartPlayback()
    {
        if (!audioLoaded) return;
        if (isPlaying) return;
        if (!playbackBar.playButtonEnabled) return;
        isPlaying = true;
        playbackBar.UpdatePlaybackUI();

        Pattern pattern = EditorContext.Pattern;
        pattern.PrepareForTimeCalculation();
        pattern.CalculateTimeOfAllNotes(
            calculateTimeWindows: false);
        playbackStartingPulse = workspace.scanlineFloatPulse;
        playbackStartingTime = pattern.PulseToTime(
            (int)playbackStartingPulse);

        // Go through all notes.
        // For notes before playbackStartingTime, play their keysounds
        // immediately if they last long enough.
        // For notes after playbackStartingTime, put them into queue,
        // and they will be played during UpdatePlayback.
        sortedNotesForPlayback = new Queue<Note>();
        foreach (Note n in EditorContext.Pattern.notes)
        {
            if (n.time < playbackStartingTime)
            {
                FmodSoundWrap sound = ResourceLoader.GetCachedSound(
                    n.sound);
                if (sound == null) continue;
                if (n.time + sound.length > playbackStartingTime)
                {
                    audioManager.PlayKeysound(sound,
                        EditorContext.Pattern.ShouldPlayInMusicChannel(n.lane),
                        startTime: playbackStartingTime - n.time,
                        n.volumePercent, n.panPercent);
                }
            }
            else
            {
                sortedNotesForPlayback.Enqueue(n);
            }
        }

        systemTimeOnPlaybackStart = DateTime.Now;
        playbackBeatOnPreviousFrame = -1f;
        backingTrackPlaying = false;

        PlaybackStarted?.Invoke();
    }

    public void StopPlayback()
    {
        // This method is called from OnDisable, so we need to
        // stop all sound even not during playback.
        audioManager.StopAll();

        if (!isPlaying) return;
        isPlaying = false;
        playbackBar.UpdatePlaybackUI();

        if (Options.instance.editorOptions.returnScanlineAfterPlayback)
        {
            workspace.scanlineFloatPulse = playbackStartingPulse;
        }
        workspace.ScrollScanlineIntoView();
        playbackBar.Refresh();

        PlaybackStopped?.Invoke();
    }

    public void UpdatePlayback()
    {
        // Calculate time.
        float elapsedTime = (float)(DateTime.Now - 
            systemTimeOnPlaybackStart).TotalSeconds;
        float playbackCurrentTime = playbackStartingTime + 
            elapsedTime;
        float playbackCurrentPulse = EditorContext.Pattern
            .TimeToPulse(playbackCurrentTime);
        
        // Play metronome sound if necessary.
        if (Options.instance.editorOptions.metronome)
        {
            float beat = playbackCurrentPulse / Pattern.pulsesPerBeat;
            if (Mathf.FloorToInt(beat) >
                Mathf.FloorToInt(playbackBeatOnPreviousFrame))
            {
                int wholeBeat = Mathf.FloorToInt(beat);
                bool wholeScan = wholeBeat % 
                    EditorContext.Pattern.patternMetadata.bps == 0;
                FmodSoundWrap sound = wholeScan ? 
                    metronome2Sound : metronome1Sound;

                MenuSfx.instance.PlaySound(sound);
            }
            playbackBeatOnPreviousFrame = beat;
        }

        // Start playing backing track if applicable.
        if (!backingTrackPlaying &&
            playbackCurrentTime >= 0f)
        {
            backingTrackPlaying = true;
            audioManager.PlayMusic(
                ResourceLoader.GetCachedSound(
                    EditorContext.Pattern.patternMetadata
                    .backingTrack),
                playbackCurrentTime);
        }

        // Stop playback after the last scan.
        int totalPulses = workspace.numScans * 
            EditorContext.Pattern.patternMetadata.bps * 
            Pattern.pulsesPerBeat;
        if (playbackCurrentPulse > totalPulses)
        {
            StopPlayback();
            return;
        }

        // Play keysounds if it's their time.
        while (sortedNotesForPlayback.Count > 0)
        {
            Note nextNote = sortedNotesForPlayback.Peek();
            if (playbackCurrentTime < nextNote.time) break;

            sortedNotesForPlayback.Dequeue();
            FmodSoundWrap sound = ResourceLoader.GetCachedSound(
                nextNote.sound);
            if (sound == null && Options.instance.editorOptions
                .assistTickOnSilentNotes)
            {
                sound = assistTickSound;
            }
            audioManager.PlayKeysound(sound,
                EditorContext.Pattern.ShouldPlayInMusicChannel(
                    nextNote.lane),
                startTime: 0f,
                nextNote.volumePercent, nextNote.panPercent);
        }

        // Move scanline.
        workspace.scanlineFloatPulse = playbackCurrentPulse;
        workspace.ScrollScanlineIntoView();
        playbackBar.Refresh();
    }

    private void PreviewKeysound(Note n)
    {
        if (keysoundPreviewChannel != null)
        {
            keysoundPreviewChannel.Stop();
        }
        FmodSoundWrap sound = ResourceLoader.GetCachedSound(
            n.sound);
        keysoundPreviewChannel = audioManager.PlayKeysound(
            sound,
            EditorContext.Pattern.ShouldPlayInMusicChannel(n.lane),
            0f,
            n.volumePercent, n.panPercent);
    }
    #endregion
}
