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

    [Header("Workspace")]
    public ScrollRect workspaceScrollRect;
    public ScrollRect headerScrollRect;
    public RectTransform workspaceViewport;
    public RectTransform workspaceContent;
    public RectTransform headerContent;
    public ScanlineInEditor scanline;

    [Header("Lanes")]
    public RectTransform hiddenLaneBackground;
    public RectTransform header;

    [Header("Markers")]
    public Transform markerInHeaderContainer;
    public GameObject scanMarkerInHeaderTemplate;
    public GameObject beatMarkerInHeaderTemplate;
    public GameObject bpmMarkerTemplate;
    public GameObject timeStopMarkerTemplate;
    public Transform markerContainer;
    public GameObject scanMarkerTemplate;
    public GameObject beatMarkerTemplate;

    [Header("Notes")]
    public Transform noteContainer;
    public Transform noteCemetary;
    public NoteObject noteCursor;
    public RectTransform selectionRectangle;
    public GameObject basicNotePrefab;
    public GameObject chainHeadPrefab;
    public GameObject chainNodePrefab;
    public GameObject repeatHeadPrefab;
    public GameObject repeatHeadHoldPrefab;
    public GameObject repeatNotePrefab;
    public GameObject repeatHoldPrefab;
    public GameObject holdNotePrefab;
    public GameObject dragNotePrefab;

    [Header("Audio")]
    public AudioSourceManager audioSourceManager;
    public AudioClip metronome1;
    public AudioClip metronome2;
    public AudioClip assistTick;

    [Header("Options")]
    public TextMeshProUGUI beatSnapDividerDisplay;
    public TMP_Dropdown visibleLanesDropdown;
    public GameObject optionsTab;

    [Header("UI")]
    public MaterialToggleButton rectangleToolButton;
    public List<NoteTypeButton> noteTypeButtons;
    public KeysoundSideSheet keysoundSheet;
    public GameObject playButton;
    public GameObject stopButton;
    public GameObject audioLoadingIndicator;
    public TextMeshProUGUI timeDisplay;
    public Slider scanlinePositionSlider;
    public Snackbar snackbar;
    public MessageDialog messageDialog;
    public TimeEventDialog timeEventDialog;
    public RadarDialog radarDialog;

    [Header("Preview")]
    public Button previewButton;
    private float? scanlinePulseBeforePreview;

    #region Internal Data Structures
    // Each NoteObject contains a reference to a Note, and this
    // dictionary is the reverse of that. Only contains the notes
    // close to the viewport.
    private Dictionary<Note, NoteObject> noteToNoteObject;
    // Maintain a list of all drag notes, so when the workspace
    // receives a click, it can check if it should land on any
    // drag note.
    private HashSet<NoteInEditor> dragNotes;

    private Note lastSelectedNoteWithoutShift;
    private Note lastClickedNote;
    public HashSet<Note> selectedNotes { get; private set; }

    private Note GetNoteFromGameObject(GameObject o)
    {
        return o.GetComponent<NoteObject>().note;
    }
        
    public GameObject GetGameObjectFromNote(Note n)
    {
        if (!noteToNoteObject.ContainsKey(n)) return null;
        return noteToNoteObject[n].gameObject;
    }

    // Clipboard stores notes instead of GameObjects,
    // so we are free of Unity stuff such as MonoBehaviors and
    // Instantiating.
    //
    // The clipboard is intentionally not initialized in OnEnabled,
    // so it is preserved between editing sessions, and across
    // patterns.
    private List<Note> clipboard;
    private int minPulseInClipboard;

    private NoteType noteType;

    private float unsnappedCursorPulse;
    private float unsnappedCursorLane;

    public enum Tool
    {
        Rectangle,
        Note
    }
    public static Tool tool { get; private set; }
    #endregion

    #region Vertical Spacing
    private static int PlayableLanes => 
        EditorContext.Pattern.patternMetadata.playableLanes;
    private static int TotalLanes => 64;

    private static float WorkspaceViewportHeight;
    private static int VisibleLanes =>
        Options.instance.editorOptions.visibleLanes;
    public static float LaneHeight =>
        WorkspaceViewportHeight / VisibleLanes;
    public static float WorkspaceContentHeight => LaneHeight *
        TotalLanes;
    #endregion

    #region Horizontal Spacing
    private int numScans;
    private static int zoom;
    public static float ScanWidth => 10f * zoom;
    public static float PulseWidth
    {
        get
        {
            return ScanWidth /
                EditorContext.Pattern.patternMetadata.bps /
                Pattern.pulsesPerBeat;
        }
    }
    private float WorkspaceContentWidth => numScans * ScanWidth;
    #endregion

    #region Outward Events
    public static event UnityAction RepositionNeeded;
    public static event UnityAction<HashSet<Note>> 
        SelectionChanged;
    public static event UnityAction KeysoundVisibilityChanged;
    public static event UnityAction PlaybackStarted;
    public static event UnityAction PlaybackStopped;
    #endregion

    #region MonoBehavior APIs
    private void OnEnable()
    {
        Options.RefreshInstance();

        // Hidden lanes
        hiddenLaneBackground.anchorMin = Vector2.zero;
        hiddenLaneBackground.anchorMax = new Vector2(
            1f, 1f - (float)PlayableLanes / TotalLanes);

        // Vertical spacing
        Canvas.ForceUpdateCanvases();
        WorkspaceViewportHeight = workspaceViewport.rect.height;

        // Horizontal spacing
        numScans = 0;  // Will be updated in Refresh()
        zoom = 100;

        // Scanline
        scanline.floatPulse = 0f;
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        workspaceScrollRect.horizontalNormalizedPosition = 0f;
        headerScrollRect.horizontalNormalizedPosition = 0f;
        scanlinePositionSlider.SetValueWithoutNotify(0f);

        // UI and options
        tool = Tool.Note;
        noteType = NoteType.Basic;
        UpdateToolAndNoteTypeButtons();
        UpdateBeatSnapDivisorDisplay();
        UpdateVisibleLaneDisplay();
        keysoundSheet.Initialize();

        // Playback
        audioLoaded = false;
        isPlaying = false;
        UpdatePlaybackUI();
        ResourceLoader.CacheAudioResources(
            EditorContext.trackFolder,
            cacheAudioCompleteCallback: OnResourceLoadComplete);

        Refresh();
        SelectionChanged += RefreshNotesInViewportWhenSelectionChanged;
        EditorContext.UndoInvoked += OnUndo;
        EditorContext.RedoInvoked += OnRedo;
        NoteInEditor.LeftClicked += OnNoteObjectLeftClick;
        NoteInEditor.RightClicked += OnNoteObjectRightClick;
        NoteInEditor.BeginDrag += OnNoteObjectBeginDrag;
        NoteInEditor.Drag += OnNoteObjectDrag;
        NoteInEditor.EndDrag += OnNoteObjectEndDrag;
        NoteInEditor.DurationHandleBeginDrag += 
            OnDurationHandleBeginDrag;
        NoteInEditor.DurationHandleDrag += OnDurationHandleDrag;
        NoteInEditor.DurationHandleEndDrag += OnDurationHandleEndDrag;
        NoteInEditor.AnchorReceiverClicked += OnAnchorReceiverClick;
        NoteInEditor.AnchorClicked += OnAnchorClick;
        NoteInEditor.AnchorBeginDrag += OnAnchorBeginDrag;
        NoteInEditor.AnchorDrag += OnAnchorDrag;
        NoteInEditor.AnchorEndDrag += OnAnchorEndDrag;
        NoteInEditor.ControlPointClicked += 
            OnControlPointClick;
        NoteInEditor.ControlPointBeginDrag += OnControlPointBeginDrag;
        NoteInEditor.ControlPointDrag += OnControlPointDrag;
        NoteInEditor.ControlPointEndDrag += OnControlPointEndDrag;
        KeysoundSideSheet.selectedKeysoundsUpdated += 
            OnSelectedKeysoundsUpdated;
        PatternTimingTab.TimingUpdated += OnPatternTimingUpdated;
        EditorOptionsTab.Opened += OnOptionsTabOpened;
        EditorOptionsTab.Closed += OnOptionsTabClosed;

        // Restore editing session
        if (scanlinePulseBeforePreview.HasValue)
        {
            scanline.floatPulse = scanlinePulseBeforePreview.Value;
            scanline.GetComponent<SelfPositionerInEditor>()
                .Reposition();
            scanlinePulseBeforePreview = null;
            ScrollScanlineIntoView();
            RefreshPlaybackBar();
        }

        DiscordController.SetActivity(DiscordActivityType.EditorPattern);
    }

    private void OnDisable()
    {
        StopPlayback();
        SelectionChanged -= RefreshNotesInViewportWhenSelectionChanged;
        EditorContext.UndoInvoked -= OnUndo;
        EditorContext.RedoInvoked -= OnRedo;
        NoteInEditor.LeftClicked -= OnNoteObjectLeftClick;
        NoteInEditor.RightClicked -= OnNoteObjectRightClick;
        NoteInEditor.BeginDrag -= OnNoteObjectBeginDrag;
        NoteInEditor.Drag -= OnNoteObjectDrag;
        NoteInEditor.EndDrag -= OnNoteObjectEndDrag;
        NoteInEditor.DurationHandleBeginDrag -= 
            OnDurationHandleBeginDrag;
        NoteInEditor.DurationHandleDrag -= OnDurationHandleDrag;
        NoteInEditor.DurationHandleEndDrag -= OnDurationHandleEndDrag;
        NoteInEditor.AnchorReceiverClicked -= OnAnchorReceiverClick;
        NoteInEditor.AnchorClicked -= OnAnchorClick;
        NoteInEditor.AnchorBeginDrag -= OnAnchorBeginDrag;
        NoteInEditor.AnchorDrag -= OnAnchorDrag;
        NoteInEditor.AnchorEndDrag -= OnAnchorEndDrag;
        NoteInEditor.ControlPointClicked -=
            OnControlPointClick;
        NoteInEditor.ControlPointBeginDrag -= OnControlPointBeginDrag;
        NoteInEditor.ControlPointDrag -= OnControlPointDrag;
        NoteInEditor.ControlPointEndDrag -= OnControlPointEndDrag;
        KeysoundSideSheet.selectedKeysoundsUpdated -= 
            OnSelectedKeysoundsUpdated;
        PatternTimingTab.TimingUpdated -= OnPatternTimingUpdated;
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
            timeEventDialog.gameObject.activeSelf ||
            optionsTab.activeSelf)
        {
            return;
        }

        bool mouseInWorkspace = RectTransformUtility
            .RectangleContainsScreenPoint(
                workspaceScrollRect.GetComponent<RectTransform>(),
                Input.mousePosition);
        bool mouseInHeader = RectTransformUtility
            .RectangleContainsScreenPoint(
                headerScrollRect.GetComponent<RectTransform>(), 
                Input.mousePosition);
        if (Input.mouseScrollDelta.y != 0)
        {
            HandleMouseScroll(Input.mouseScrollDelta.y,
                mouseInWorkspace || mouseInHeader);
        }

        if (mouseInWorkspace && tool == Tool.Note)
        {
            noteCursor.gameObject.SetActive(true);
            SnapNoteCursor();
        }
        else
        {
            noteCursor.gameObject.SetActive(false);
        }

        if (Input.GetMouseButton(0) &&
            mouseInHeader &&
            !isPlaying)
        {
            MoveScanlineToMouse();
        }

        HandleKeyboardShortcuts();
    }
    #endregion

    #region Undo and Redo
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
                case EditOperation.Type.Metadata:
                    OnPatternTimingUpdated();
                    break;
                case EditOperation.Type.TimeEvent:
                    DestroyAndRespawnAllMarkers();
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
                        GenericAddNote(n);
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
                        RefreshNoteInEditor(n);
                    }
                    break;
            }
        }
        // To update note detail sheet.
        SelectionChanged?.Invoke(selectedNotes);
    }

    private void OnRedo(EditTransaction transaction)
    {
        foreach (EditOperation op in transaction.ops)
        {
            switch (op.type)
            {
                case EditOperation.Type.Metadata:
                    OnPatternTimingUpdated();
                    break;
                case EditOperation.Type.TimeEvent:
                    DestroyAndRespawnAllMarkers();
                    break;
                case EditOperation.Type.AddNote:
                    {
                        Note n = op.addedNote;
                        GenericAddNote(n);
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
                        RefreshNoteInEditor(n);
                    }
                    break;
            }
        }
        // To update note detail sheet.
        SelectionChanged?.Invoke(selectedNotes);
    }

    // Calls one of AddNote, AddHoldNote and AddDragNote.
    private void GenericAddNote(Note n)
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

    private void RefreshNoteInEditor(Note n)
    {
        GameObject o = GetGameObjectFromNote(n);
        if (o == null) return;

        NoteInEditor e = o.GetComponent<NoteInEditor>();
        e.SetKeysoundText();
        e.UpdateEndOfScanIndicator();
        switch (n.type)
        {
            case NoteType.Hold:
            case NoteType.RepeatHold:
            case NoteType.RepeatHeadHold:
                e.ResetTrail();
                break;
            case NoteType.Drag:
                e.ResetCurve();
                e.ResetAllAnchorsAndControlPoints();
                break;
        }
    }
    #endregion

    #region Mouse and Keyboard Update
    private void HandleMouseScroll(float y,
        bool mouseInWorkspaceOrHeader)
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);
        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);

        if (mouseInWorkspaceOrHeader && ctrl)
        {
            // Adjust zoom.
            zoom += Mathf.FloorToInt(y * 5f);
            zoom = Mathf.Clamp(zoom, 10, 500);
            float horizontal = workspaceScrollRect
                .horizontalNormalizedPosition;
            ResizeWorkspace();
            RepositionNeeded?.Invoke();
            AdjustAllPathsAndTrails();
            workspaceScrollRect.horizontalNormalizedPosition =
                horizontal;
        }
        else if (alt)
        {
            // Change beat snap divisor.
            OnBeatSnapDivisorChanged(y < 0f ? -1 : 1);
        }
        else if (mouseInWorkspaceOrHeader)
        {
            if (shift)
            {
                // Vertical scroll.
                workspaceScrollRect.verticalNormalizedPosition +=
                    y * 100f / WorkspaceContentHeight;
                workspaceScrollRect.verticalNormalizedPosition =
                    Mathf.Clamp01(
                    workspaceScrollRect.verticalNormalizedPosition);
            }
            else
            {
                // Horizontal scroll.
                workspaceScrollRect.horizontalNormalizedPosition +=
                    y * 100f / WorkspaceContentWidth;
                workspaceScrollRect.horizontalNormalizedPosition =
                    Mathf.Clamp01(
                    workspaceScrollRect.horizontalNormalizedPosition);
                SynchronizeScrollRects();
            }
        }
    }

    private void CalculateCursorPulseAndLane(
        Vector2 mousePosition,
        out float unsnappedPulse, out float unsnappedLane)
    {
        Vector2 pointInContainer = ScreenPointToPointInNoteContainer(
            mousePosition);
        PointInNoteContainerToPulseAndLane(pointInContainer,
            out unsnappedPulse, out unsnappedLane);
    }

    private void SnapNoteCursor(Vector2 mousePositionOverride)
    {
        CalculateCursorPulseAndLane(mousePositionOverride,
            out unsnappedCursorPulse, out unsnappedCursorLane);
        
        int snappedCursorPulse = SnapPulse(unsnappedCursorPulse);
        int snappedCursorLane =
            Mathf.FloorToInt(unsnappedCursorLane + 0.5f);

        noteCursor.note = new Note();
        noteCursor.note.pulse = snappedCursorPulse;
        noteCursor.note.lane = snappedCursorLane;
        noteCursor.GetComponent<SelfPositionerInEditor>().Reposition();
    }

    private void SnapNoteCursor()
    {
        SnapNoteCursor(Input.mousePosition);
    }

    private void MoveScanlineToMouse()
    {
        Vector2 pointInHeader;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            header, Input.mousePosition,
            cam: null, out pointInHeader);

        int bps = EditorContext.Pattern.patternMetadata.bps;
        float cursorScan = pointInHeader.x / ScanWidth;
        float cursorPulse = cursorScan * bps * Pattern.pulsesPerBeat;
        int snappedCursorPulse = SnapPulse(cursorPulse);

        scanline.floatPulse = snappedCursorPulse;
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        RefreshPlaybackBar();
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
            OnRectangleToolButtonClick();
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
        if (Input.GetKeyDown(KeyCode.F) && selectedNotes.Count > 0)
        {
            foreach (Note n in selectedNotes)
            {
                ScrollNoteIntoView(n);
                break;
            }
        }

        UnityAction<float> moveScanlineTo = (float pulse) =>
        {
            scanline.floatPulse = pulse;
            scanline.GetComponent<SelfPositionerInEditor>()
                .Reposition();
            RefreshPlaybackBar();
            ScrollScanlineIntoView();
        };
        float pulsesPerScan =
            EditorContext.Pattern.patternMetadata.bps *
            Pattern.pulsesPerBeat;
        if (Input.GetKeyDown(KeyCode.Home))
        {
            moveScanlineTo(0f);
        }
        if (Input.GetKeyDown(KeyCode.End))
        {
            moveScanlineTo(numScans * pulsesPerScan);
        }
        if (Input.GetKeyDown(KeyCode.PageUp))
        {
            moveScanlineTo(Mathf.Max(
                scanline.floatPulse - pulsesPerScan,
                0f));
        }
        if (Input.GetKeyDown(KeyCode.PageDown))
        {
            float maxPulse = numScans * pulsesPerScan;
            moveScanlineTo(Mathf.Min(
                scanline.floatPulse + pulsesPerScan,
                maxPulse));
        }
    }
    #endregion

    #region Events From Workspace and NoteObjects
    public void OnWorkspaceScrollRectValueChanged()
    {
        RefreshNotesInViewport();
        SynchronizeScrollRects();
    }

    private void RefreshNotesInViewport()
    {
        // Calculate the pulse and lane range visible through the
        // viewport.
        Vector2 topLeftOfViewport = new Vector2(
            workspaceScrollRect.horizontalNormalizedPosition *
            (WorkspaceContentWidth - workspaceViewport.rect.width),
            (1f - workspaceScrollRect.verticalNormalizedPosition) *
            (workspaceViewport.rect.height - WorkspaceContentHeight));
        if (workspaceViewport.rect.width > WorkspaceContentWidth)
        {
            topLeftOfViewport.x = 0f;
        }
        Vector2 bottomRightOfViewport = new Vector2(
            topLeftOfViewport.x + workspaceViewport.rect.width,
            topLeftOfViewport.y - workspaceViewport.rect.height);
        float minPulse, maxPulse, minLane, maxLane;
        PointInNoteContainerToPulseAndLane(
            topLeftOfViewport, out minPulse, out minLane);
        PointInNoteContainerToPulseAndLane(
            bottomRightOfViewport, out maxPulse, out maxLane);

        // Expand the range to compensate for paths, trails and curves.
        minPulse -= Pattern.pulsesPerBeat *
            EditorContext.Pattern.patternMetadata.bps * 2;
        maxPulse += Pattern.pulsesPerBeat *
            EditorContext.Pattern.patternMetadata.bps * 2;
        minLane -= EditorContext.Pattern.patternMetadata.playableLanes;
        maxLane += EditorContext.Pattern.patternMetadata.playableLanes;

        // Find all notes that should spawn.
        Note topLeftNote = new Note()
        {
            pulse = Mathf.FloorToInt(minPulse),
            lane = Mathf.FloorToInt(minLane)
        };
        Note bottomRightNote = new Note()
        {
            pulse = Mathf.CeilToInt(maxPulse),
            lane = Mathf.CeilToInt(maxLane)
        };
        List<Note> visibleNotes = EditorContext.Pattern
            .GetRangeBetween(topLeftNote, bottomRightNote);
        HashSet<Note> visibleNotesAsSet = new HashSet<Note>(
            visibleNotes);
        // All selected notes should remain visible.
        visibleNotesAsSet.UnionWith(selectedNotes);

        // Make a copy of noteToNoteObject because the code below
        // will modify it.
        Dictionary<Note, NoteObject> noteToNoteObjectClone
            = new Dictionary<Note, NoteObject>(noteToNoteObject);

        // Destroy notes that go out of view.
        foreach (KeyValuePair<Note, NoteObject> pair in
            noteToNoteObjectClone)
        {
            if (!visibleNotesAsSet.Contains(pair.Key))
            {
                DeleteNoteObject(pair.Key, pair.Value.gameObject,
                    intendToDeleteNote: false);
            }
        }

        // Spawn notes that come into view.
        foreach (Note n in visibleNotesAsSet)
        {
            if (!noteToNoteObjectClone.ContainsKey(n))
            {
                GameObject o = SpawnNoteObject(n);
                AdjustPathOrTrailAround(o);
            }
        }
    }

    private void RefreshNotesInViewportWhenSelectionChanged(
        HashSet<Note> _)
    {
        RefreshNotesInViewport();
    }

    // If no drag note should receive this event, returns null.
    private NoteInEditor FindDragNoteToReceiveEvent(
        PointerEventData eventData)
    {
        foreach (NoteInEditor dragNote in dragNotes)
        {
            if (dragNote.ClickLandsOnCurve(eventData.position))
            {
                return dragNote;
            }
        }
        return null;
    }

    public void OnNoteContainerClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerEventData =
            eventData as PointerEventData;
        if (pointerEventData.dragging) return;

        // Special case: check drag notes because the curves do not
        // receive clicks.
        NoteInEditor dragNoteToReceiveEvent = 
            FindDragNoteToReceiveEvent(pointerEventData);
        if (dragNoteToReceiveEvent != null)
        {
            if (pointerEventData.button ==
                    PointerEventData.InputButton.Left)
            {
                OnNoteObjectLeftClick(
                    dragNoteToReceiveEvent.gameObject);
            }
            if (pointerEventData.button ==
                PointerEventData.InputButton.Right)
            {
                OnNoteObjectRightClick(
                    dragNoteToReceiveEvent.gameObject);
            }
            return;
        }

        // Special case: if rectangle tool, deselect all.
        if (tool == Tool.Rectangle)
        {
            selectedNotes.Clear();
            SelectionChanged?.Invoke(selectedNotes);
            return;
        }

        if (pointerEventData.button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }
        if (!noteCursor.gameObject.activeSelf) return;
        if (EditorContext.Pattern.HasNoteAt(
            noteCursor.note.pulse, noteCursor.note.lane))
        {
            return;
        }
        if (isPlaying) return;

        string invalidReason;
        // No need to call CanAddHoldNote or CanAddDragNote here
        // because durations and curves are flexible.
        if (!CanAddNote(noteType, noteCursor.note.pulse,
            noteCursor.note.lane, out invalidReason))
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
                newNote = AddNote(noteType, noteCursor.note.pulse,
                    noteCursor.note.lane, sound);
                break;
            case NoteType.Hold:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                newNote = AddHoldNote(noteType, noteCursor.note.pulse,
                    noteCursor.note.lane, duration: null, sound);
                break;
            case NoteType.Drag:
                newNote = AddDragNote(noteCursor.note.pulse,
                    noteCursor.note.lane,
                    nodes: null,
                    sound);
                break;
        }

        EditorContext.RecordAddedNote(GetNoteFromGameObject(newNote));
        EditorContext.EndTransaction();
    }

    private bool draggingDragCurve;
    public void OnNoteContainerBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerEventData =
            eventData as PointerEventData;
        if (tool == Tool.Rectangle &&
            pointerEventData.button == 
                PointerEventData.InputButton.Left)
        {
            OnBeginDragWhenRectangleToolActive();
            return;
        }
        draggingDragCurve = false;

        // Special case for drag notes.
        NoteInEditor dragNoteToReceiveEvent =
            FindDragNoteToReceiveEvent(pointerEventData);
        if (dragNoteToReceiveEvent != null &&
            pointerEventData.button == 
                PointerEventData.InputButton.Left)
        {
            OnNoteObjectBeginDrag(pointerEventData,
                dragNoteToReceiveEvent.gameObject);
            draggingDragCurve = true;
            return;
        }
    }

    public void OnNoteContainerDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData p = eventData as PointerEventData;
        if (tool == Tool.Rectangle &&
            p.button ==
                PointerEventData.InputButton.Left)
        {
            OnDragWhenRectangleToolActive(p.delta);
            return;
        }

        // Special case for drag notes.
        if (draggingDragCurve && p.button == 
            PointerEventData.InputButton.Left)
        {
            OnNoteObjectDrag(p);
            return;
        }

        if (p.button == PointerEventData.InputButton.Middle)
        {
            OnMiddleMouseButtonDrag(p.delta);
        }
    }

    private void OnMiddleMouseButtonDrag(Vector2 unscaledDelta)
    {
        float outOfViewWidth = WorkspaceContentWidth -
            workspaceViewport.rect.width;
        float outOfViewHeight = WorkspaceContentHeight -
            workspaceViewport.rect.height;
        if (outOfViewWidth < 0f) outOfViewWidth = 0f;
        if (outOfViewHeight < 0f) outOfViewHeight = 0f;

        float horizontal =
            workspaceScrollRect.horizontalNormalizedPosition *
            outOfViewWidth;
        horizontal -= unscaledDelta.x / rootCanvas.localScale.x;
        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(horizontal / outOfViewWidth);

        float vertical =
            workspaceScrollRect.verticalNormalizedPosition *
            outOfViewHeight;
        vertical -= unscaledDelta.y / rootCanvas.localScale.x;
        workspaceScrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(vertical / outOfViewHeight);

        SynchronizeScrollRects();
    }

    public void OnNoteContainerEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerEventData =
            eventData as PointerEventData;
        if (tool == Tool.Rectangle &&
            pointerEventData.button ==
                PointerEventData.InputButton.Left)
        {
            OnEndDragWhenRectangleToolActive();
            return;
        }

        // Special case for drag notes.
        if (draggingDragCurve && pointerEventData.button == 
            PointerEventData.InputButton.Left)
        { 
            OnNoteObjectEndDrag(pointerEventData);
            return;
        }
    }

    public void OnNoteObjectLeftClick(GameObject o)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        Note clickedNote = GetNoteFromGameObject(o);
        lastClickedNote = clickedNote;
        if (shift)
        {
            if (lastSelectedNoteWithoutShift == null)
            {
                lastSelectedNoteWithoutShift =
                    EditorContext.Pattern.notes.Min;
            }
            // At this point lastSelectedNoteObjectWithoutShift
            // might still be null.
            List<Note> range = EditorContext.Pattern
                .GetRangeBetween(
                lastSelectedNoteWithoutShift,
                clickedNote);
            if (ctrl)
            {
                // Add [prev, o] to current selection.
                foreach (Note noteInRange in range)
                {
                    selectedNotes.Add(noteInRange);
                }
            }
            else  // !ctrl
            {
                // Overwrite current selection with [prev, o].
                selectedNotes.Clear();
                foreach (Note noteInRange in range)
                {
                    selectedNotes.Add(noteInRange);
                }
            }
        }
        else  // !shift
        {
            lastSelectedNoteWithoutShift = clickedNote;
            if (ctrl)
            {
                // Toggle o in current selection.
                ToggleSelection(clickedNote);
            }
            else  // !ctrl
            {
                if (selectedNotes.Count > 1)
                {
                    selectedNotes.Clear();
                    selectedNotes.Add(clickedNote);
                }
                else if (selectedNotes.Count == 1)
                {
                    if (selectedNotes.Contains(clickedNote))
                    {
                        selectedNotes.Remove(clickedNote);
                    }
                    else
                    {
                        selectedNotes.Clear();
                        selectedNotes.Add(clickedNote);
                    }
                }
                else  // Count == 0
                {
                    selectedNotes.Add(clickedNote);
                }
            }
        }

        SelectionChanged?.Invoke(selectedNotes);
    }

    private void ToggleSelection(Note n)
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

    public void OnNoteObjectRightClick(GameObject o)
    {
        if (isPlaying) return;
        Note n = GetNoteFromGameObject(o);

        EditorContext.BeginTransaction();
        EditorContext.RecordDeletedNote(n);
        selectedNotes.Remove(n);
        DeleteNote(n);
        EditorContext.EndTransaction();

        SelectionChanged?.Invoke(selectedNotes);
    }
    #endregion

    #region UI Events And Updates
    private void OnPatternTimingUpdated()
    {
        DestroyAndRespawnAllMarkers();
        RepositionNeeded?.Invoke();
        UpdateNumScansAndRelatedUI();
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
        UpdateBeatSnapDivisorDisplay();
    }

    private void UpdateBeatSnapDivisorDisplay()
    {
        beatSnapDividerDisplay.text =
            Options.instance.editorOptions.beatSnapDivisor.ToString();
    }

    public void OnTimeEventButtonClick()
    {
        int scanlineIntPulse = (int)scanline.floatPulse;
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

            EditorContext.PrepareToModifyTimeEvent();
            // Delete event.
            EditorContext.Pattern.bpmEvents.RemoveAll((BpmEvent e) =>
            {
                return e.pulse == scanlineIntPulse;
            });
            EditorContext.Pattern.timeStops.RemoveAll((TimeStop t) =>
            {
                return t.pulse == scanlineIntPulse;
            });
            // Add event if there is one.
            if (newBpm.HasValue)
            {
                EditorContext.Pattern.bpmEvents.Add(new BpmEvent()
                {
                    pulse = scanlineIntPulse,
                    bpm = newBpm.Value
                });
            }
            if (newTimeStopPulses.HasValue)
            {
                EditorContext.Pattern.timeStops.Add(new TimeStop()
                {
                    pulse = scanlineIntPulse,
                    duration = newTimeStopPulses.Value
                });
            }

            DestroyAndRespawnAllMarkers();
        });
    }

    public void OnVisibleLaneNumberChanged(int newValue)
    {
        Options.instance.editorOptions.visibleLanes = int.Parse(
            visibleLanesDropdown.options
            [newValue].text);

        ResizeWorkspace();
        RepositionNeeded?.Invoke();
        AdjustAllPathsAndTrails();
    }

    private void UpdateVisibleLaneDisplay()
    {
        UIUtils.MemoryToDropdown(visibleLanesDropdown,
            VisibleLanes.ToString());
    }

    public void OnRectangleToolButtonClick()
    {
        tool = Tool.Rectangle;
        UpdateToolAndNoteTypeButtons();
    }

    public void OnNoteTypeButtonClick(NoteTypeButton clickedButton)
    {
        ChangeNoteType(clickedButton.type);
    }

    private void ChangeNoteType(NoteType newType)
    {
        tool = Tool.Note;
        noteType = newType;
        UpdateToolAndNoteTypeButtons();

        // Apply to selection if asked to.
        if (!Options.instance.editorOptions
            .applyNoteTypeToSelection)
        {
            return;
        }
        if (isPlaying) return;
        if (selectedNotes.Count == 0) return;

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
        SelectionChanged?.Invoke(selectedNotes);
    }

    private void UpdateToolAndNoteTypeButtons()
    {
        rectangleToolButton.SetIsOn(tool == Tool.Rectangle);
        foreach (NoteTypeButton b in noteTypeButtons)
        {
            b.GetComponent<MaterialToggleButton>().SetIsOn(
                tool == Tool.Note && b.type == noteType);
        }
    }

    public void OnScanlinePositionSliderValueChanged(float newValue)
    {
        if (isPlaying) return;

        int totalPulses = numScans
            * EditorContext.Pattern.patternMetadata.bps
            * Pattern.pulsesPerBeat;
        float scanlineRawPulse = totalPulses * newValue;
        scanline.floatPulse = SnapPulse(scanlineRawPulse);
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        ScrollScanlineIntoView();
        RefreshScanlineTimeDisplay();
    }

    private void OnSelectedKeysoundsUpdated(List<string> keysounds)
    {
        if (selectedNotes == null ||
            selectedNotes.Count == 0) return;
        if (!Options.instance.editorOptions
            .applyKeysoundToSelection)
        {
            return;
        }
        if (isPlaying) return;
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
            GameObject o = GetGameObjectFromNote(n);
            if (o == null) continue;
            o.GetComponent<NoteInEditor>().SetKeysoundText();
            o.GetComponent<NoteInEditor>().UpdateKeysoundVisibility();
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

    public void OnPreviewButtonClicked()
    {
        GameSetup.track = EditorContext.track;
        GameSetup.trackPath = EditorContext.trackPath;
        GameSetup.pattern = EditorContext.Pattern;
        GameSetup.beginningScanInEditorPreview =
            Mathf.FloorToInt(
                scanline.floatPulse / 
                Pattern.pulsesPerBeat /
                GameSetup.pattern.patternMetadata.bps);
        scanlinePulseBeforePreview = scanline.floatPulse;
        previewButton.GetComponent<TransitionToPanel>().Invoke();
    }

    public void OnInspectButtonClick()
    {
        List<Note> notesWithIssue = new List<Note>();
        string issue = EditorContext.Pattern.Inspect(notesWithIssue);
        if (issue == null)
        {
            snackbar.Show(Locale.GetString(
                "pattern_inspection_no_issue"));
        }
        else
        {
            snackbar.Show(issue);
            selectedNotes.Clear();
            foreach (Note n in notesWithIssue)
            {
                selectedNotes.Add(n);
            }
            // Scroll the first selected note into view.
            if (selectedNotes.Count > 0)
            {
                ScrollNoteIntoView(notesWithIssue[0]);
            }
            SelectionChanged?.Invoke(selectedNotes);
        }
    }

    public void OnRadarButtonClick()
    {
        radarDialog.Show();
    }
    #endregion

    #region Dragging And Dropping Notes
    private GameObject draggedNoteObject;
    private void OnNoteObjectBeginDrag(PointerEventData eventData,
        GameObject o)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerBeginDrag(eventData);
            return;
        }

        OnBeginDraggingNotes(o);
    }

    private void OnNoteObjectDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerDrag(eventData);
            return;
        }

        OnDraggingNotes(eventData.delta);
    }

    private void OnNoteObjectEndDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerEndDrag(eventData);
            return;
        }

        OnEndDraggingNotes();
    }

    // The following can be called in 2 ways:
    // - from NoteObject's drag events, when any note type is active
    // - from ctrl+drag on anything, when the rectangle tool is active
    private void OnBeginDraggingNotes(GameObject o)
    {
        draggedNoteObject = o;
        lastSelectedNoteWithoutShift = GetNoteFromGameObject(o);
        if (!selectedNotes.Contains(lastSelectedNoteWithoutShift))
        {
            selectedNotes.Clear();
            selectedNotes.Add(lastSelectedNoteWithoutShift);

            SelectionChanged?.Invoke(selectedNotes);
        }
    }

    private void OnDraggingNotes(Vector2 delta)
    {
        delta /= rootCanvas.localScale.x;
        if (Options.instance.editorOptions.lockNotesInTime)
        {
            delta.x = 0f;
        }

        foreach (Note n in selectedNotes)
        {
            GameObject o = GetGameObjectFromNote(n);
            if (o == null) continue;

            // This is only visual. Notes are only really moved
            // in OnNoteObjectEndDrag.
            o.GetComponent<RectTransform>().anchoredPosition += delta;
            o.GetComponent<NoteInEditor>()
                .KeepPathInPlaceWhileNoteBeingDragged(delta);
        }

        ScrollWorkspaceWhenMouseIsCloseToEdge();
    }

    private void OnEndDraggingNotes()
    {
        // Calculate and snap where the note image lands at.
        Note draggedNote = GetNoteFromGameObject(draggedNoteObject);
        Vector3 noteImagePosition =
            draggedNoteObject.GetComponent<NoteInEditor>().noteImage
            .position;
        SnapNoteCursor(noteImagePosition);  // hackity hack
        int newPulse = noteCursor.note.pulse;
        int newLane = noteCursor.note.lane;
        SnapNoteCursor();  // unhack

        // Calculate delta pulse and delta lane.
        int oldPulse = draggedNote.pulse;
        int oldLane = draggedNote.lane;
        int deltaPulse = newPulse - oldPulse;
        int deltaLane = newLane - oldLane;
        if (Options.instance.editorOptions.lockNotesInTime)
        {
            deltaPulse = 0;
        }

        // Is the move valid?
        bool movable = true;
        string invalidReason = "";
        foreach (Note n in selectedNotes)
        {
            newPulse = n.pulse + deltaPulse;
            newLane = n.lane + deltaLane;

            switch (n.type)
            {
                case NoteType.Basic:
                case NoteType.ChainHead:
                case NoteType.ChainNode:
                case NoteType.RepeatHead:
                case NoteType.Repeat:
                    movable = movable && CanAddNote(n.type,
                        newPulse, newLane,
                        ignoredExistingNotes: selectedNotes,
                        out invalidReason);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    movable = movable && CanAddHoldNote(n.type,
                        newPulse, newLane, (n as HoldNote).duration,
                        ignoredExistingNotes: selectedNotes,
                        out invalidReason);
                    break;
                case NoteType.Drag:
                    movable = movable && CanAddDragNote(
                        newPulse, newLane, (n as DragNote).nodes,
                        ignoredExistingNotes: selectedNotes,
                        out invalidReason);
                    break;
            }

            if (!movable)
            {
                snackbar.Show(invalidReason);
                break;
            }
        }

        if (movable)
        {
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
                if (movedNote.pulse == oldPulse + deltaPulse &&
                    movedNote.lane == oldLane + deltaLane)
                {
                    lastSelectedNoteWithoutShift =
                        GetNoteFromGameObject(o);
                }
            }
            EditorContext.EndTransaction();
            selectedNotes = replacedSelection;
            SelectionChanged?.Invoke(selectedNotes);
        }

        foreach (Note n in selectedNotes)
        {
            GameObject o = GetGameObjectFromNote(n);
            if (o == null) continue;

            o.GetComponent<SelfPositionerInEditor>().Reposition();
            o.GetComponent<NoteInEditor>().ResetPathPosition();
        }

        if (selectedNotes.Count == 1)
        {
            foreach (Note n in selectedNotes)
            {
                lastClickedNote = n;
            }
        }
    }

    private void ScrollWorkspaceWhenMouseIsCloseToEdge()
    {
        // There was an attempt to scroll the workspace when
        // dragging anything. However there are 2 problems:
        //
        // - Unity doesn't fire drag events when the mouse is
        //   not moving, so the user has to wiggle the mouse
        //   to keep the scrolling going. We can work around that
        //   by calling this from Update but it will take too much
        //   work to figure out when to call this and when not to.
        //
        // - When the workspace scrolls, the thing being dragged
        //   moves a lot in screen space, but the mouse moves little,
        //   so the delta passed to drag events is also little.
        //   This results in the dragged thing moving away from
        //   the mouse.
        //
        // Until we have a solution, it's better to not support
        // drag-induced scrolling for now.

        /*
        const float kEdgeWidthInside = 10f;
        const float kEdgeWidthOutside = 50f;
        const float kHorizontalScrollSpeed = 0.01f;
        const float kVerticalScrollSpeed = 0.01f;

        Vector2 mousePosInViewport;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            workspaceViewport,
            screenPoint: Input.mousePosition,
            cam: null,
            out mousePosInViewport);
        float xScroll, yScroll;
        if (mousePosInViewport.x < workspaceViewport.rect.center.x)
        {
            xScroll = Mathf.InverseLerp(
                workspaceViewport.rect.xMin - kEdgeWidthOutside,
                workspaceViewport.rect.xMin + kEdgeWidthInside,
                mousePosInViewport.x) - 1f;
        }
        else
        {
            xScroll = Mathf.InverseLerp(
                workspaceViewport.rect.xMax - kEdgeWidthInside,
                workspaceViewport.rect.xMax + kEdgeWidthOutside,
                mousePosInViewport.x);
        }
        if (mousePosInViewport.y < workspaceViewport.rect.center.y)
        {
            yScroll = Mathf.InverseLerp(
                workspaceViewport.rect.yMin - kEdgeWidthOutside,
                workspaceViewport.rect.yMin + kEdgeWidthInside,
                mousePosInViewport.y) - 1f;
        }
        else
        {
            yScroll = Mathf.InverseLerp(
                workspaceViewport.rect.yMax - kEdgeWidthInside,
                workspaceViewport.rect.yMax + kEdgeWidthOutside,
                mousePosInViewport.y);
        }

        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(
                workspaceScrollRect.horizontalNormalizedPosition +
                xScroll * kHorizontalScrollSpeed);
        workspaceScrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(
                workspaceScrollRect.verticalNormalizedPosition +
                yScroll * kVerticalScrollSpeed);
        SynchronizeScrollRects();
        */
    }
    #endregion

    #region Hold Note Duration Adjustment
    private List<Note> holdNotesBeingAdjusted;
    private GameObject initialHoldNoteBeingAdjusted;
    private void OnDurationHandleBeginDrag(
        PointerEventData eventData, GameObject noteObject)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerBeginDrag(eventData);
            return;
        }

        Note n = GetNoteFromGameObject(noteObject);
        holdNotesBeingAdjusted = new List<Note>();
        if (selectedNotes.Contains(n))
        {
            // Adjust all hold notes in the selection.
            foreach (Note selectedNote in selectedNotes)
            {
                NoteType noteType = selectedNote.type;
                if (noteType == NoteType.Hold ||
                    noteType == NoteType.RepeatHeadHold ||
                    noteType == NoteType.RepeatHold)
                {
                    holdNotesBeingAdjusted.Add(selectedNote);
                }
            }
        }
        else
        {
            // Adjust only the dragged note and ignore selection.
            holdNotesBeingAdjusted.Add(n);
        }
        initialHoldNoteBeingAdjusted = noteObject;

        foreach (Note holdNote in holdNotesBeingAdjusted)
        {
            GameObject o = GetGameObjectFromNote(holdNote);
            if (o == null) continue;
            o.GetComponent<NoteInEditor>().RecordTrailActualLength();
        }
    }

    private void OnDurationHandleDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerDrag(eventData);
            return;
        }
        Vector2 delta = eventData.delta;
        delta /= rootCanvas.localScale.x;

        foreach (Note n in holdNotesBeingAdjusted)
        {
            GameObject o = GetGameObjectFromNote(n);
            if (o == null) continue;
            // This is only visual; duration is only really changed
            // in OnDurationHandleEndDrag.
            o.GetComponent<NoteInEditor>().AdjustTrailLength(
                delta.x);
        }

        ScrollWorkspaceWhenMouseIsCloseToEdge();
    }

    private void OnDurationHandleEndDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerEndDrag(eventData);
            return;
        }

        int oldDuration = (initialHoldNoteBeingAdjusted.
            GetComponent<NoteObject>().note as HoldNote).duration;
        int newDuration = noteCursor.note.pulse -
            initialHoldNoteBeingAdjusted.GetComponent<NoteObject>().
            note.pulse;
        int deltaDuration = newDuration - oldDuration;

        // Is the adjustment valid?
        bool adjustable = true;
        foreach (Note n in holdNotesBeingAdjusted)
        {
            HoldNote holdNote = n as HoldNote;
            oldDuration = holdNote.duration;
            newDuration = oldDuration + deltaDuration;
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
            foreach (Note n in holdNotesBeingAdjusted)
            {
                EditOperation op = EditorContext
                    .BeginModifyNoteOperation();
                HoldNote holdNote = n as HoldNote;
                op.noteBeforeOp = holdNote.Clone();
                holdNote.duration += deltaDuration;
                op.noteAfterOp = holdNote.Clone();
            }
            EditorContext.EndTransaction();
            UpdateNumScansAndRelatedUI();
        }

        foreach (Note n in holdNotesBeingAdjusted)
        {
            GameObject o = GetGameObjectFromNote(n);
            if (o == null) continue;
            o.GetComponent<NoteInEditor>().ResetTrail();
        }
    }
    #endregion

    #region Drag Notes
    // These may be snapped or unsnapped depending on options.
    private void GetCursorPositionForAnchor(out float pulse,
        out float lane)
    {
        if (Options.instance.editorOptions.snapDragAnchors)
        {
            pulse = noteCursor.note.pulse;
            lane = noteCursor.note.lane;
        }
        else
        {
            pulse = unsnappedCursorPulse;
            lane = unsnappedCursorLane;
        }
    }

    private void OnAnchorReceiverClick(PointerEventData eventData,
        GameObject note)
    {
        if (tool == Tool.Rectangle)
        {
            // Event passes through.
            OnNoteContainerClick(eventData);
            return;
        }

        if (eventData.button != PointerEventData.InputButton.Left)
        {
            // Do nothing.
            return;
        }

        DragNote dragNote = note.GetComponent<NoteObject>()
            .note as DragNote;
        float cursorPulse, cursorLane;
        GetCursorPositionForAnchor(out cursorPulse, out cursorLane);
        FloatPoint newAnchor = new FloatPoint(
            cursorPulse - dragNote.pulse,
            cursorLane - dragNote.lane);

        // Is there an existing anchor at the same pulse?
        string reason;
        if (!EditorContext.Pattern.CanAddDragAnchor(
            dragNote, newAnchor.pulse, out reason))
        {
            snackbar.Show(reason);
            return;
        }

        DragNode newNode = new DragNode()
        {
            anchor = newAnchor,
            controlLeft = new FloatPoint(0f, 0f),
            controlRight = new FloatPoint(0f, 0f)
        };
        EditorContext.BeginTransaction();
        EditOperation op = EditorContext.BeginModifyNoteOperation();
        op.noteBeforeOp = dragNote.Clone();
        dragNote.nodes.Add(newNode);
        dragNote.nodes.Sort((DragNode node1, DragNode node2) =>
        {
            return (int)Mathf.Sign(
                node1.anchor.pulse - node2.anchor.pulse);
        });
        op.noteAfterOp = dragNote.Clone();
        EditorContext.EndTransaction();
        UpdateNumScansAndRelatedUI();

        NoteInEditor noteInEditor = note
            .GetComponent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetAllAnchorsAndControlPoints();
    }

    private GameObject draggedAnchor;
    private DragNode draggedDragNode;
    private DragNode draggedDragNodeBeforeDrag;
    private bool ctrlHeldOnAnchorBeginDrag;
    private bool dragCurveIsBSpline;
    private Vector2 mousePositionRelativeToDraggedAnchor;
    private void OnAnchorClick(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Right)
        {
            // Event passes through.
            OnNoteContainerClick(eventData);
            return;
        }

        GameObject anchor = eventData.pointerDrag;
        int anchorIndex = anchor
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        DragNote dragNote = anchor
            .GetComponentInParent<NoteObject>().note as DragNote;

        string reason;
        if (!EditorContext.Pattern.CanDeleteDragAnchor(
            dragNote, anchorIndex, out reason))
        {
            snackbar.Show(reason);
            return;
        }

        EditorContext.BeginTransaction();
        EditOperation op = EditorContext.BeginModifyNoteOperation();
        op.noteBeforeOp = dragNote.Clone();
        dragNote.nodes.RemoveAt(anchorIndex);
        op.noteAfterOp = dragNote.Clone();
        EditorContext.EndTransaction();
        UpdateNumScansAndRelatedUI();

        NoteInEditor noteInEditor = anchor
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetAllAnchorsAndControlPoints();
    }

    private void OnAnchorBeginDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerBeginDrag(eventData);
            return;
        }

        GameObject anchor = eventData.pointerDrag;
        draggedAnchor = anchor
            .GetComponentInParent<DragNoteAnchor>().gameObject;

        DragNote dragNote = anchor
            .GetComponentInParent<NoteObject>().note as DragNote;
        int anchorIndex = anchor
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        draggedDragNode = dragNote.nodes[anchorIndex];
        draggedDragNodeBeforeDrag = draggedDragNode.Clone();

        ctrlHeldOnAnchorBeginDrag = Input.GetKey(KeyCode.LeftControl)
            || Input.GetKey(KeyCode.RightControl);
        dragCurveIsBSpline = dragNote.curveType == CurveType.BSpline;
        if (ctrlHeldOnAnchorBeginDrag && !dragCurveIsBSpline)
        {
            // Reset control points.
            mousePositionRelativeToDraggedAnchor = new Vector2();
            draggedDragNode.controlLeft = new FloatPoint(0f, 0f);
            draggedDragNode.controlRight = new FloatPoint(0f, 0f);

            NoteInEditor noteInEditor = draggedAnchor
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetControlPointPosition(draggedDragNode,
                draggedAnchor, 0);
            noteInEditor.ResetControlPointPosition(draggedDragNode,
                draggedAnchor, 1);
            noteInEditor.ResetPathsToControlPoints(
                draggedAnchor.GetComponent<DragNoteAnchor>());
        }
    }

    private void MoveDraggedAnchor()
    {
        Note noteHead = draggedAnchor
            .GetComponentInParent<NoteObject>().note;
        float cursorPulse, cursorLane;
        GetCursorPositionForAnchor(out cursorPulse, out cursorLane);
        if (!Options.instance.editorOptions.lockDragAnchorsInTime)
        {
            draggedDragNode.anchor.pulse = cursorPulse
                - noteHead.pulse;
        }
        draggedDragNode.anchor.lane = cursorLane
            - noteHead.lane;
        draggedAnchor.GetComponent<RectTransform>().anchoredPosition
            = new Vector2(
                draggedDragNode.anchor.pulse * PulseWidth,
                -draggedDragNode.anchor.lane * LaneHeight);

        NoteInEditor noteInEditor = draggedAnchor
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
    }

    private void MoveControlPointsBeingReset(Vector2 delta)
    {
        mousePositionRelativeToDraggedAnchor += delta;

        Vector2 pointLeft, pointRight;
        if (mousePositionRelativeToDraggedAnchor.x < 0f)
        {
            pointLeft = mousePositionRelativeToDraggedAnchor;
        }
        else
        {
            pointLeft = -mousePositionRelativeToDraggedAnchor;
        }
        pointRight = -pointLeft;

        draggedDragNode.controlLeft = new FloatPoint(
            pulse: pointLeft.x / PulseWidth,
            lane: -pointLeft.y / LaneHeight);
        draggedDragNode.controlRight = new FloatPoint(
           pulse: pointRight.x / PulseWidth,
           lane: -pointRight.y / LaneHeight);

        NoteInEditor noteInEditor = draggedAnchor
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetControlPointPosition(draggedDragNode,
            draggedAnchor, 0);
        noteInEditor.ResetControlPointPosition(draggedDragNode,
            draggedAnchor, 1);
        noteInEditor.ResetPathsToControlPoints(
            draggedAnchor.GetComponent<DragNoteAnchor>());
    }

    private void OnAnchorDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerDrag(eventData);
            return;
        }

        Vector2 delta = eventData.delta;
        delta /= rootCanvas.localScale.x;

        if (ctrlHeldOnAnchorBeginDrag)
        {
            if (!dragCurveIsBSpline)
            {
                MoveControlPointsBeingReset(delta);
            }
        }
        else
        {
            if (draggedAnchor
                .GetComponentInParent<DragNoteAnchor>()
                .anchorIndex == 0)
            {
                return;
            }
            MoveDraggedAnchor();
        }

        ScrollWorkspaceWhenMouseIsCloseToEdge();
    }

    private void OnAnchorEndDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerEndDrag(eventData);
            return;
        }
        if (!ctrlHeldOnAnchorBeginDrag &&
            draggedAnchor.GetComponentInParent<DragNoteAnchor>()
                .anchorIndex == 0)
        {
            return;
        }
        if (ctrlHeldOnAnchorBeginDrag && dragCurveIsBSpline)
        {
            return;
        }

        DragNote dragNote = draggedAnchor
            .GetComponentInParent<NoteObject>().note as DragNote;
        string reason;
        if (!EditorContext.Pattern.CanEditDragNote(dragNote,
            out reason))
        {
            snackbar.Show(reason);
            // Restore note to pre-drag state.
            draggedDragNode.CopyFrom(draggedDragNodeBeforeDrag);
            NoteInEditor noteInEditor = draggedAnchor
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetAllAnchorsAndControlPoints();
            return;
        }

        EditorContext.BeginTransaction();
        EditOperation op = EditorContext.BeginModifyNoteOperation();
        op.noteAfterOp = dragNote.Clone();
        DragNode draggedDragNodeAfterDrag = draggedDragNode.Clone();
        draggedDragNode.CopyFrom(draggedDragNodeBeforeDrag);
        op.noteBeforeOp = dragNote.Clone();
        draggedDragNode.CopyFrom(draggedDragNodeAfterDrag);
        EditorContext.EndTransaction();
        
        UpdateNumScansAndRelatedUI();
    }

    private void OnControlPointClick(PointerEventData eventData,
        int controlPointIndex)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Right)
        {
            // Event passes through.
            OnNoteContainerClick(eventData);
            return;
        }

        GameObject controlPoint = eventData.pointerPress;
        int anchorIndex = controlPoint
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        DragNote note = controlPoint
            .GetComponentInParent<NoteObject>().note as DragNote;
        DragNode node = note.nodes[anchorIndex];

        EditorContext.BeginTransaction();
        EditOperation op = EditorContext.BeginModifyNoteOperation();
        op.noteBeforeOp = note.Clone();
        node.SetControlPoint(controlPointIndex,
            new FloatPoint(0f, 0f));
        op.noteAfterOp = note.Clone();
        EditorContext.EndTransaction();

        NoteInEditor noteInEditor = controlPoint
            .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetCurve();
        noteInEditor.ResetAllAnchorsAndControlPoints();
    }

    private GameObject draggedControlPoint;
    private int draggedControlPointIndex;
    private void OnControlPointBeginDrag(PointerEventData eventData,
        int controlPointIndex)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerBeginDrag(eventData);
            return;
        }
        draggedControlPoint = eventData.pointerDrag;
        draggedControlPointIndex = controlPointIndex;

        int anchorIndex = draggedControlPoint
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        draggedDragNode = (draggedControlPoint
            .GetComponentInParent<NoteObject>().note as DragNote)
            .nodes[anchorIndex];
        draggedDragNodeBeforeDrag = draggedDragNode.Clone();
    }

    private void OnControlPointDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerDrag(eventData);
            return;
        }

        Vector2 delta = eventData.delta;
        delta /= rootCanvas.localScale.x;
        draggedControlPoint.GetComponent<RectTransform>()
            .anchoredPosition += delta;

        Vector2 newPosition = draggedControlPoint
            .GetComponent<RectTransform>()
            .anchoredPosition;
        FloatPoint newPoint = new FloatPoint(
            pulse: newPosition.x / PulseWidth,
            lane: -newPosition.y / LaneHeight);
        draggedDragNode.SetControlPoint(draggedControlPointIndex,
            newPoint);

        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);
        if (!alt && newPosition.sqrMagnitude > 0f)
        {
            // Rotate opposing control point.
            int otherIndex = 1 - draggedControlPointIndex;
            RectTransform otherTransform = draggedControlPoint
                .GetComponentInParent<DragNoteAnchor>()
                .GetControlPoint(otherIndex)
                .GetComponent<RectTransform>();
            Vector2 otherPosition = otherTransform
                .anchoredPosition;

            float angle = Mathf.Atan2(newPosition.y, newPosition.x);
            angle += Mathf.PI;
            otherPosition = new Vector2(
                otherPosition.magnitude * Mathf.Cos(angle),
                otherPosition.magnitude * Mathf.Sin(angle));
            FloatPoint otherPoint = new FloatPoint(
                pulse: otherPosition.x / PulseWidth,
                lane: -otherPosition.y / LaneHeight);

            otherTransform.anchoredPosition = otherPosition;
            draggedDragNode.SetControlPoint(otherIndex,
                otherPoint);
        }

        NoteInEditor noteInEditor = draggedControlPoint
                .GetComponentInParent<NoteInEditor>();
        noteInEditor.ResetPathsToControlPoints(
            draggedControlPoint
            .GetComponentInParent<DragNoteAnchor>());
        noteInEditor.ResetCurve();

        ScrollWorkspaceWhenMouseIsCloseToEdge();
    }

    private void OnControlPointEndDrag(PointerEventData eventData)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerEndDrag(eventData);
            return;
        }

        DragNote dragNote = draggedControlPoint
            .GetComponentInParent<NoteObject>().note as DragNote;
        string reason;
        if (!EditorContext.Pattern.CanEditDragNote(
            dragNote, out reason))
        {
            snackbar.Show(reason);
            // Restore note to pre-drag state.
            draggedDragNode.CopyFrom(draggedDragNodeBeforeDrag);
            NoteInEditor noteInEditor = draggedControlPoint
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetAllAnchorsAndControlPoints();
            return;
        }

        EditorContext.BeginTransaction();
        EditOperation op = EditorContext.BeginModifyNoteOperation();
        op.noteAfterOp = dragNote.Clone();
        DragNode draggedDragNodeAfterDrag = draggedDragNode.Clone();
        draggedDragNode.CopyFrom(draggedDragNodeBeforeDrag);
        op.noteBeforeOp = dragNote.Clone();
        draggedDragNode.CopyFrom(draggedDragNodeAfterDrag);
        EditorContext.EndTransaction();
    }
    #endregion

    #region Refreshing
    // This deletes and respawns everything, therefore is extremely
    // slow.
    private void Refresh()
    {
        DestroyAndSpawnExistingNotes();
        UpdateNumScans();
        DestroyAndRespawnAllMarkers();
        ResizeWorkspace();
        RefreshPlaybackBar();
    }

    // Returns whether the number changed.
    // During an editing session the number of scans will never
    // decrease. This prevents unintended scrolling when deleting
    // the last notes.
    private bool UpdateNumScans()
    {
        int numScansBackup = numScans;

        int lastPulse = 0;
        if (EditorContext.Pattern.notes.Count > 0)
        {
            lastPulse = EditorContext.Pattern.notes.Max.pulse;
        }
        int pulsesPerScan = Pattern.pulsesPerBeat *
            EditorContext.Pattern.patternMetadata.bps;

        // Look at all hold and drag notes in the last few scans
        // in case their duration outlasts the currently considered
        // last scan.
        foreach (Note n in EditorContext.Pattern.GetViewBetween(
            minPulseInclusive: lastPulse - pulsesPerScan * 2,
            maxPulseInclusive: lastPulse))
        {
            int endingPulse;
            if (n is HoldNote)
            {
                endingPulse = n.pulse + (n as HoldNote).duration;
            }
            else if (n is DragNote)
            {
                endingPulse = n.pulse + (n as DragNote).Duration();
            }
            else
            {
                continue;
            }

            if (endingPulse > lastPulse)
            {
                lastPulse = endingPulse;
            }
        }

        int lastScan = lastPulse / pulsesPerScan;
        // 1 empty scan at the end
        numScans = Mathf.Max(numScansBackup, lastScan + 2);
        // Minimal 16 scans
        numScans = Mathf.Max(numScans, 16);

        return numScans != numScansBackup;
    }

    private void UpdateNumScansAndRelatedUI()
    {
        if (UpdateNumScans())
        {
            DestroyAndRespawnAllMarkers();
            ResizeWorkspace();
        }
    }

    private void ResizeWorkspace()
    {
        workspaceContent.sizeDelta = new Vector2(
            WorkspaceContentWidth,
            WorkspaceContentHeight);
        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(
                workspaceScrollRect.horizontalNormalizedPosition);
        workspaceScrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(
                workspaceScrollRect.verticalNormalizedPosition);

        SynchronizeScrollRects();
    }

    private void RefreshScanlineTimeDisplay()
    {
        float scanlineTime = EditorContext.Pattern.PulseToTime(
            (int)scanline.floatPulse);
        timeDisplay.text = UIUtils.FormatTime(scanlineTime,
            includeMillisecond: true);
    }

    // This includes both the time display and slider.
    private void RefreshPlaybackBar()
    {
        RefreshScanlineTimeDisplay();

        int bps = EditorContext.Pattern.patternMetadata.bps;
        float scanlineNormalizedPosition = scanline.floatPulse /
            (numScans * bps * Pattern.pulsesPerBeat);
       
        scanlinePositionSlider.SetValueWithoutNotify(scanlineNormalizedPosition);
    }
    #endregion

    #region Spawning
    private void DestroyAndRespawnAllMarkers()
    {
        for (int i = 0; i < markerInHeaderContainer.childCount; i++)
        {
            GameObject child = markerInHeaderContainer.GetChild(i)
                .gameObject;
            if (child == scanMarkerInHeaderTemplate) continue;
            if (child == beatMarkerInHeaderTemplate) continue;
            if (child == bpmMarkerTemplate) continue;
            if (child == timeStopMarkerTemplate) continue;
            Destroy(child);
        }
        for (int i = 0; i < markerContainer.childCount; i++)
        {
            GameObject child = markerContainer.GetChild(i)
                .gameObject;
            if (child == scanMarkerTemplate) continue;
            if (child == beatMarkerTemplate) continue;
            Destroy(child);
        }

        EditorContext.Pattern.PrepareForTimeCalculation();
        int bps = EditorContext.Pattern.patternMetadata.bps;

        for (int scan = 0; scan < numScans; scan++)
        {
            GameObject markerInHeader = Instantiate(
                scanMarkerInHeaderTemplate, markerInHeaderContainer);
            GameObject marker = Instantiate(
                scanMarkerTemplate, markerContainer);
            markerInHeader.SetActive(true);  // This calls OnEnabled
            marker.SetActive(true);

            int pulse = scan * bps * Pattern.pulsesPerBeat;
            Marker m = markerInHeader.GetComponent<Marker>();
            m.pulse = pulse;
            m.SetTimeDisplay();
            m.GetComponent<SelfPositionerInEditor>().Reposition();
            m = marker.GetComponent<Marker>();
            m.pulse = pulse;
            m.GetComponent<SelfPositionerInEditor>().Reposition();

            for (int beat = 1; beat < bps; beat++)
            {
                markerInHeader = Instantiate(
                    beatMarkerInHeaderTemplate, 
                    markerInHeaderContainer);
                marker = Instantiate(
                    beatMarkerTemplate,
                    markerContainer);
                markerInHeader.SetActive(true);
                marker.SetActive(true);

                pulse = (scan * bps + beat) *
                    Pattern.pulsesPerBeat;
                m = markerInHeader.GetComponent<Marker>();
                m.pulse = pulse;
                m.SetTimeDisplay();
                m.GetComponent<SelfPositionerInEditor>()
                    .Reposition();
                m = marker.GetComponent<Marker>();
                m.pulse = pulse;
                m.GetComponent<SelfPositionerInEditor>()
                    .Reposition();
            }
        }

        foreach (BpmEvent e in EditorContext.Pattern.bpmEvents)
        {
            GameObject marker = Instantiate(
                bpmMarkerTemplate, markerInHeaderContainer);
            marker.SetActive(true);
            Marker m = marker.GetComponent<Marker>();
            m.pulse = e.pulse;
            m.SetBpmText(e.bpm);
            m.GetComponent<SelfPositionerInEditor>().Reposition();
        }

        foreach (TimeStop t in EditorContext.Pattern.timeStops)
        {
            GameObject marker = Instantiate(
                timeStopMarkerTemplate, markerInHeaderContainer);
            marker.SetActive(true);
            Marker m = marker.GetComponent<Marker>();
            m.pulse = t.pulse;
            m.SetTimeStopText(t.duration);
            m.GetComponent<SelfPositionerInEditor>().Reposition();
        }
    }

    // This will call Reposition on the new object.
    private GameObject SpawnNoteObject(Note n)
    {
        GameObject prefab;
        switch (n.type)
        {
            case NoteType.Basic:
                prefab = basicNotePrefab;
                break;
            case NoteType.ChainHead:
                prefab = chainHeadPrefab;
                break;
            case NoteType.ChainNode:
                prefab = chainNodePrefab;
                break;
            case NoteType.RepeatHead:
                prefab = repeatHeadPrefab;
                break;
            case NoteType.RepeatHeadHold:
                prefab = repeatHeadHoldPrefab;
                break;
            case NoteType.Repeat:
                prefab = repeatNotePrefab;
                break;
            case NoteType.RepeatHold:
                prefab = repeatHoldPrefab;
                break;
            case NoteType.Hold:
                prefab = holdNotePrefab;
                break;
            case NoteType.Drag:
                prefab = dragNotePrefab;
                break;
            default:
                Debug.LogError("Unknown note type: " + n.type);
                prefab = basicNotePrefab;
                break;
        }

        NoteObject noteObject = 
            Instantiate(prefab, noteContainer)
            .GetComponent<NoteObject>();
        noteObject.note = n;
        NoteInEditor noteInEditor = noteObject
            .GetComponent<NoteInEditor>();
        noteInEditor.SetKeysoundText();
        noteInEditor.UpdateKeysoundVisibility();
        noteInEditor.UpdateEndOfScanIndicator();
        noteInEditor.SetSprite(hidden: n.lane >= PlayableLanes);
        noteInEditor.UpdateSelection(selectedNotes);
        noteObject.GetComponent<SelfPositionerInEditor>()
            .Reposition();

        noteToNoteObject.Add(n, noteObject);
        if (n.type == NoteType.Drag)
        {
            dragNotes.Add(noteInEditor);
        }

        // Binary search the appropriate sibling index of
        // new note, so all notes are drawn from right to left.
        //
        // More specifically, we are looking for the smallest-index
        // sibling that's located on the left of the new note.
        if (noteContainer.childCount == 1)
        {
            return noteObject.gameObject;
        }
        float targetX = noteObject.transform.position.x;
        int first = 0;
        int last = noteContainer.childCount - 2;
        while (true)
        {
            float firstX = noteContainer.GetChild(first).position.x;
            float lastX = noteContainer.GetChild(last).position.x;
            if (firstX <= targetX)
            {
                noteObject.transform.SetSiblingIndex(first);
                break;
            }
            if (lastX >= targetX)
            {
                noteObject.transform.SetSiblingIndex(last + 1);
                break;
            }
            // Now we know for sure that lastX < targetX < firstX.
            int middle = (first + last) / 2;
            float middleX = noteContainer.GetChild(middle)
                .position.x;
            if (middleX == targetX)
            {
                noteObject.transform.SetSiblingIndex(middle);
                break;
            }
            if (middleX < targetX)
            {
                last = middle - 1;
            }
            else  // middleX > targetX
            {
                first = middle + 1;
            }
        }

        return noteObject.gameObject;
    }

    private void DestroyAndSpawnExistingNotes()
    {
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            Destroy(noteContainer.GetChild(i).gameObject);
        }
        noteToNoteObject = new Dictionary<Note, NoteObject>();
        dragNotes = new HashSet<NoteInEditor>();
        lastSelectedNoteWithoutShift = null;
        lastClickedNote = null;
        selectedNotes = new HashSet<Note>();
        SelectionChanged?.Invoke(selectedNotes);

        RefreshNotesInViewport();
        AdjustAllPathsAndTrails();
    }

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

    private void GetPreviousAndNextChainNotes(Note n,
        out Note prev, out Note next)
    {
        GetPreviousAndNextNotes( n,
            new HashSet<NoteType>()
                { NoteType.ChainHead, NoteType.ChainNode },
            minLaneInclusive: 0,
            maxLaneInclusive: PlayableLanes - 1,
            out prev, out next);
    }

    private void GetPreviousAndNextRepeatNotes(Note n,
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

    // This may modify o, the same-type note before o, and/or
    // the same-type note after o.
    private void AdjustPathOrTrailAround(GameObject o)
    {
        Note n = o.GetComponent<NoteObject>().note;
        Note prev, next;

        switch (n.type)
        {
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                if (n.lane >= PlayableLanes) break;
                GetPreviousAndNextChainNotes(n,
                    out prev, out next);

                if (n.type == NoteType.ChainNode)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(prev);
                    if (prev != null &&
                        prev.type == NoteType.ChainHead)
                    {
                        GetGameObjectFromNote(prev)
                            ?.GetComponent<NoteInEditor>()
                            .RotateNoteHeadToward(n);
                    }
                }
                if (next != null &&
                    next.type == NoteType.ChainNode)
                {
                    GetGameObjectFromNote(next)
                        ?.GetComponent<NoteInEditor>()
                        .PointPathToward(n);
                    if (n.type == NoteType.ChainHead)
                    {
                        o.GetComponent<NoteInEditor>()
                            .RotateNoteHeadToward(next);
                    }
                }
                break;
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                if (n.lane >= PlayableLanes) break;
                GetPreviousAndNextRepeatNotes(n,
                    out prev, out next);

                if (n.type == NoteType.Repeat ||
                    n.type == NoteType.RepeatHold)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(prev);
                }

                if (next != null)
                {
                    NoteType nextType = next.type;
                    if (nextType == NoteType.Repeat ||
                        nextType == NoteType.RepeatHold)
                    {
                        GetGameObjectFromNote(next)
                            ?.GetComponent<NoteInEditor>()
                            .PointPathToward(n);
                    }
                }
                break;
            case NoteType.Drag:
                o.GetComponent<NoteInEditor>().ResetCurve();
                o.GetComponent<NoteInEditor>()
                    .ResetAllAnchorsAndControlPoints();
                break;
        }

        if (n.type == NoteType.Hold ||
            n.type == NoteType.RepeatHeadHold ||
            n.type == NoteType.RepeatHold)
        {
            o.GetComponent<NoteInEditor>().ResetTrail();
        }
    }

    // This may modify o, the same-type note before o, and/or
    // the same-type note after o.
    private void AdjustPathBeforeDeleting(GameObject o)
    {
        Note n = o.GetComponent<NoteObject>().note;
        if (n.lane < 0 || n.lane >= PlayableLanes) return;
        Note prev, next;

        switch (n.type)
        {
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                GetPreviousAndNextChainNotes(n,
                    out prev, out next);

                if (next != null &&
                    next.type == NoteType.ChainNode)
                {
                    GetGameObjectFromNote(next)
                        ?.GetComponent<NoteInEditor>()
                        .PointPathToward(prev);
                    if (prev != null &&
                        prev.type == NoteType.ChainHead)
                    {
                        GetGameObjectFromNote(prev)
                            ?.GetComponent<NoteInEditor>()
                            .RotateNoteHeadToward(next);
                    }
                }
                else if (prev != null &&
                    prev.type == NoteType.ChainHead)
                {
                    GetGameObjectFromNote(prev)
                        ?.GetComponent<NoteInEditor>()
                        .ResetNoteImageRotation();
                }
                break;
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                GetPreviousAndNextRepeatNotes(n,
                    out prev, out next);

                if (next != null)
                {
                    NoteType nextType = next.type;
                    if (nextType == NoteType.Repeat ||
                        nextType == NoteType.RepeatHold)
                    {
                        GetGameObjectFromNote(next)
                            ?.GetComponent<NoteInEditor>()
                            .PointPathToward(prev);
                    }
                }
                break;
            default:
                break;
        }
    }

    private void AdjustAllPathsAndTrails()
    {
        Note prevChain = null;
        // Indexed by lane
        List<Note> previousRepeat = new List<Note>();
        for (int i = 0; i < PlayableLanes; i++)
        {
            previousRepeat.Add(null);
        }

        foreach (Note n in EditorContext.Pattern.notes)
        {
            GameObject o = GetGameObjectFromNote(n);
            if (n.type == NoteType.Hold ||
                n.type == NoteType.RepeatHeadHold ||
                n.type == NoteType.RepeatHold)
            {
                // Adjust the trails of hold notes.
                o?.GetComponent<NoteInEditor>().ResetTrail();
            }
            if (n.type == NoteType.Drag)
            {
                // Draw curves of drag notes.
                o?.GetComponent<NoteInEditor>().ResetCurve();
                o?.GetComponent<NoteInEditor>()
                    .ResetAllAnchorsAndControlPoints();
            }

            // For chain paths and repeat paths, ignore hidden notes.
            if (n.lane >= PlayableLanes) continue;

            if (n.type == NoteType.ChainHead ||
                n.type == NoteType.ChainNode)
            {
                // Adjust the paths of chain nodes.
                if (n.type == NoteType.ChainNode)
                {
                    o?.GetComponent<NoteInEditor>()
                        .PointPathToward(prevChain);
                    if (prevChain != null &&
                        prevChain.type == NoteType.ChainHead)
                    {
                        GetGameObjectFromNote(prevChain)
                            ?.GetComponent<NoteInEditor>()
                            .RotateNoteHeadToward(n);
                    }
                }
                prevChain = n;
            }
            if (n.type == NoteType.RepeatHead ||
                n.type == NoteType.Repeat ||
                n.type == NoteType.RepeatHeadHold ||
                n.type == NoteType.RepeatHold)
            {
                // Adjust the paths of repeat notes.
                if (n.type == NoteType.Repeat ||
                    n.type == NoteType.RepeatHold)
                {
                    o?.GetComponent<NoteInEditor>()
                        .PointPathToward(previousRepeat[n.lane]);
                }
                previousRepeat[n.lane] = n;
            }
        }
    }
    #endregion

    #region Pattern Modification
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
            type, pulse, lane, TotalLanes,
            ignoredExistingNotes, out reason);
    }

    private bool CanAddHoldNote(NoteType type, int pulse, int lane,
        int duration, HashSet<Note> ignoredExistingNotes,
        out string reason)
    {
        return EditorContext.Pattern.CanAddHoldNote(
            type, pulse, lane, TotalLanes, duration,
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

    private GameObject FinishAddNote(Note n)
    {
        // Add to pattern.
        EditorContext.Pattern.notes.Add(n);

        // Add to UI. SpawnNoteObject will add n to
        // noteToNoteObject.
        GameObject newNote = SpawnNoteObject(n);
        AdjustPathOrTrailAround(newNote);
        UpdateNumScansAndRelatedUI();
        return newNote;
    }

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
            if (lastClickedNote != null &&
                lastClickedNote.type == type)
            {
                // Attempt to inherit duration of last clicked note.
                if (CanAddHoldNote(type, pulse, lane,
                    (lastClickedNote as HoldNote).duration,
                    null, out _))
                {
                    duration = (lastClickedNote as HoldNote).duration;
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
            if (lastClickedNote != null &&
                lastClickedNote.type == NoteType.Drag)
            {
                // Inherit nodes from the last clicked note.
                foreach (DragNode node in
                    (lastClickedNote as DragNote).nodes)
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

    // Cannot remove n from selectedNotes because the
    // caller may be enumerating that list.
    private void DeleteNote(Note n)
    {
        // Delete from UI, if it's there.
        GameObject o = GetGameObjectFromNote(n);
        if (o == null) return;
        DeleteNoteObject(n, o,
            intendToDeleteNote: true);

        // Delete from pattern.
        EditorContext.Pattern.notes.Remove(n);

        if (lastSelectedNoteWithoutShift == n)
        {
            lastSelectedNoteWithoutShift = null;
        }
        if (lastClickedNote == n)
        {
            lastClickedNote = null;
        }
    }

    private void DeleteNoteObject(Note n, GameObject o,
        bool intendToDeleteNote)
    {
        if (intendToDeleteNote)
        {
            AdjustPathBeforeDeleting(o);
        }
        else
        {
            AdjustPathOrTrailAround(o);
        }
        noteToNoteObject.Remove(n);
        // No exception if the elements don't exist.
        dragNotes.Remove(o.GetComponent<NoteInEditor>());
        
        // Destroy doesn't immediately destroy, so we move the note
        // to the cemetary so as to not interfere with the
        // binary searches when spawning new notes on the same frame.
        o.transform.SetParent(noteCemetary);
        Destroy(o);
        UpdateNumScansAndRelatedUI();
    }

    private void ToggleEndOfScanOnSelectedNotes()
    {
        if (selectedNotes.Count == 0) return;
        bool currentValue = false;
        foreach (Note n in selectedNotes)
        {
            currentValue = n.endOfScan;
            break;
        }
        SetEndOfScanOnSelectedNotes(!currentValue);

        // Force the side sheet to update.
        SelectionChanged?.Invoke(selectedNotes);
    }

    public void SetEndOfScanOnSelectedNotes(bool newValue)
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
            GameObject o = GetGameObjectFromNote(n);
            if (o == null) continue;
            o.GetComponent<NoteInEditor>().UpdateEndOfScanIndicator();
        }
    }
    #endregion

    #region Rectangle Tool
    private Vector2 rectangleStart;
    private Vector2 rectangleEnd;
    private bool movingNotesWhenRectangleToolActive;

    private void OnBeginDragWhenRectangleToolActive()
    {
        movingNotesWhenRectangleToolActive =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        if (movingNotesWhenRectangleToolActive)
        {
            if (selectedNotes.Count == 0) return;
            foreach (Note n in selectedNotes)
            {
                GameObject o = GetGameObjectFromNote(n);
                if (o == null) continue;

                OnBeginDraggingNotes(o);
                break;
            }
        }
        else
        {
            StartRectangle();
        }
    }

    private void OnDragWhenRectangleToolActive(Vector2 delta)
    {
        if (movingNotesWhenRectangleToolActive)
        {
            if (selectedNotes.Count == 0) return;
            OnDraggingNotes(delta);
        }
        else
        {
            UpdateRectangle();
            ScrollWorkspaceWhenMouseIsCloseToEdge();
        }
    }

    private void OnEndDragWhenRectangleToolActive()
    {
        if (movingNotesWhenRectangleToolActive)
        {
            if (selectedNotes.Count == 0) return;
            OnEndDraggingNotes();
        }
        else
        {
            ProcessRectangle();
        }
    }

    private void StartRectangle()
    {
        rectangleStart = ScreenPointToPointInNoteContainer(
            Input.mousePosition);
        selectionRectangle.gameObject.SetActive(true);

        DrawRectangle();
    }

    private void UpdateRectangle()
    {
        rectangleEnd = ScreenPointToPointInNoteContainer(
            Input.mousePosition);

        DrawRectangle();
    }

    private void ProcessRectangle()
    {
        selectionRectangle.gameObject.SetActive(false);

        float startPulse, startLane, endPulse, endLane;
        PointInNoteContainerToPulseAndLane(rectangleStart,
            out startPulse, out startLane);
        PointInNoteContainerToPulseAndLane(rectangleEnd,
            out endPulse, out endLane);

        int minPulse = Mathf.RoundToInt(
            Mathf.Min(startPulse, endPulse));
        int maxPulse = Mathf.RoundToInt(
            Mathf.Max(startPulse, endPulse));
        float minLane = Mathf.Min(startLane, endLane);
        float maxLane = Mathf.Max(startLane, endLane);
        HashSet<Note> notesInRectangle =
            new HashSet<Note>();
        foreach (Note n in EditorContext.Pattern
            .GetViewBetween(minPulse, maxPulse))
        {
            if (n.lane >= minLane && n.lane <= maxLane)
            {
                notesInRectangle.Add(n);
            }
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);
        if (shift)
        {
            // Append rectangle to selection.
            foreach (Note n in notesInRectangle)
            {
                selectedNotes.Add(n);
            }
        }
        else if (alt)
        {
            // Subtract rectangle from selection.
            foreach (Note n in notesInRectangle)
            {
                selectedNotes.Remove(n);
            }
        }
        else
        {
            // Replace selection with rectangle.
            selectedNotes = notesInRectangle;
        }
        SelectionChanged?.Invoke(selectedNotes);
    }

    private void DrawRectangle()
    {
        selectionRectangle.anchoredPosition = new Vector2(
            Mathf.Min(rectangleStart.x, rectangleEnd.x),
            Mathf.Max(rectangleStart.y, rectangleEnd.y));
        selectionRectangle.sizeDelta = new Vector2(
            Mathf.Abs(rectangleStart.x - rectangleEnd.x),
            Mathf.Abs(rectangleStart.y - rectangleEnd.y));
    }
    #endregion

    #region Selection And Clipboard
    public void SelectAll()
    {
        selectedNotes.Clear();
        foreach (Note n in EditorContext.Pattern.notes)
        {
            selectedNotes.Add(n);
        }
        SelectionChanged?.Invoke(selectedNotes);

        // SelectAll is the only way of selection that can affect
        // notes outside the view port. So:
        RefreshNotesInViewport();
    }

    public void SelectNone()
    {
        selectedNotes.Clear();
        SelectionChanged?.Invoke(selectedNotes);
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

        int scanlinePulse = (int)scanline.floatPulse;
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
        SelectionChanged?.Invoke(selectedNotes);
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
    private bool audioLoaded;
    private bool isPlaying;
    private float playbackStartingPulse;
    private float playbackStartingTime;
    private bool backingTrackPlaying;
    private DateTime systemTimeOnPlaybackStart;
    private float playbackBeatOnPreviousFrame;  // For metronome

    // When playing, sort all notes by pulse so it's easy to tell if
    // it's time to play the next note in the queue. Once played,
    // a note is removed from the queue.
    private Queue<Note> sortedNotesForPlayback;

    // Keep a reference to the audio source playing a keysound preview
    // so we can stop a preview before starting the next one.
    private AudioSource keysoundPreviewSource;

    private void OnResourceLoadComplete(string error)
    {
        if (error != null)
        {
            messageDialog.Show(Locale.GetStringAndFormatIncludingPaths(
                "pattern_panel_resource_loading_error_format",
                error));
        }
        audioLoaded = true;
        playButton.GetComponent<Button>().interactable =
            error == null;
        UpdatePlaybackUI();
    }

    private void UpdatePlaybackUI()
    {
        if (audioLoaded)
        {
            playButton.SetActive(!isPlaying);
            stopButton.SetActive(isPlaying);
        }
        else
        {
            playButton.SetActive(false);
            stopButton.SetActive(false);
        }
        audioLoadingIndicator.SetActive(!audioLoaded);
        scanlinePositionSlider.interactable = !isPlaying;
        previewButton.interactable = audioLoaded;
    }

    public void StartPlayback()
    {
        if (!audioLoaded) return;
        if (isPlaying) return;
        if (!playButton.GetComponent<Button>().interactable)
            return;
        isPlaying = true;
        UpdatePlaybackUI();

        Pattern pattern = EditorContext.Pattern;
        pattern.PrepareForTimeCalculation();
        pattern.CalculateTimeOfAllNotes(
            calculateTimeWindows: false);
        playbackStartingPulse = scanline.floatPulse;
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
                AudioClip clip = ResourceLoader.GetCachedClip(
                    n.sound);
                if (clip == null) continue;
                if (n.time + clip.length > playbackStartingTime)
                {
                    audioSourceManager.PlayKeysound(clip,
                        EditorContext.Pattern.IsHiddenNote(n.lane),
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
        audioSourceManager.StopAll();

        if (!isPlaying) return;
        isPlaying = false;
        UpdatePlaybackUI();

        if (Options.instance.editorOptions.returnScanlineAfterPlayback)
        {
            scanline.floatPulse = playbackStartingPulse;
        }
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        ScrollScanlineIntoView();
        RefreshPlaybackBar();

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
                AudioClip clip = wholeScan ? metronome2 : metronome1;

                MenuSfx.instance.PlaySound(clip);
            }
            playbackBeatOnPreviousFrame = beat;
        }

        // Start playing backing track if applicable.
        if (!backingTrackPlaying &&
            playbackCurrentTime >= 0f)
        {
            backingTrackPlaying = true;
            audioSourceManager.PlayBackingTrack(
                ResourceLoader.GetCachedClip(
                    EditorContext.Pattern.patternMetadata
                    .backingTrack),
                playbackCurrentTime);
        }

        // Stop playback after the last scan.
        int totalPulses = numScans * 
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
            AudioClip clip = ResourceLoader.GetCachedClip(
                nextNote.sound);
            if (clip == null && Options.instance.editorOptions
                .assistTickOnSilentNotes)
            {
                clip = assistTick;
            }
            audioSourceManager.PlayKeysound(clip,
                EditorContext.Pattern.IsHiddenNote(nextNote.lane),
                startTime: 0f,
                nextNote.volumePercent, nextNote.panPercent);
        }

        // Move scanline.
        scanline.floatPulse = playbackCurrentPulse;
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        ScrollScanlineIntoView();
        RefreshPlaybackBar();
    }

    public void PreviewKeysound(Note n)
    {
        if (keysoundPreviewSource != null)
        {
            keysoundPreviewSource.Stop();
        }
        AudioClip clip = ResourceLoader.GetCachedClip(
            n.sound);
        keysoundPreviewSource = audioSourceManager.PlayKeysound(
            clip,
            EditorContext.Pattern.IsHiddenNote(n.lane),
            0f,
            n.volumePercent, n.panPercent);
    }
    #endregion

    #region Utilities
    private int SnapPulse(float rawPulse)
    {
        int pulsesPerDivision = Pattern.pulsesPerBeat
            / Options.instance.editorOptions.beatSnapDivisor;
        int snappedPulse = Mathf.RoundToInt(
            rawPulse / pulsesPerDivision)
            * pulsesPerDivision;
        return snappedPulse;
    }

    private Vector2 ScreenPointToPointInNoteContainer(
        Vector2 screenPoint)
    {
        Vector2 pointInContainer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            noteContainer.GetComponent<RectTransform>(),
            screenPoint,
            cam: null,
            out pointInContainer);
        return pointInContainer;
    }

    private void PointInNoteContainerToPulseAndLane(
        Vector2 pointInContainer,
        out float pulse, out float lane)
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;
        float scan = pointInContainer.x / ScanWidth;
        pulse = scan * bps * Pattern.pulsesPerBeat;
        lane = -pointInContainer.y / LaneHeight - 0.5f;
    }

    private void ScrollScanlineIntoView()
    {
        if (!Options.instance.editorOptions.keepScanlineInView) return;

        UIUtils.ScrollIntoView(
            scanline.GetComponent<RectTransform>(),
            workspaceScrollRect,
            normalizedMargin: 0.8f,
            viewRectAsPoint: true,
            horizontal: true,
            vertical: false);
        SynchronizeScrollRects();
    }

    private void ScrollNoteIntoView(Note n)
    {
        Vector2 position = SelfPositionerInEditor.PositionOf(n);
        Vector3 worldPosition = workspaceContent.TransformPoint(
            position);
        UIUtils.ScrollIntoView(
            worldPosition, 
            workspaceScrollRect,
            normalizedMargin: 0.1f,
            viewRectAsPoint: true,
            horizontal: true,
            vertical: true);
    }

    // This was used to implement continuous scrolling during playback,
    // but the frame rate is too low, so it's abandoned.
    private void KeepScanlineAtLeftOfView()
    {
        float viewPortWidth = workspaceScrollRect
            .GetComponent<RectTransform>().rect.width;
        if (WorkspaceContentWidth <= viewPortWidth) return;

        float scanlinePosition = scanline
            .GetComponent<RectTransform>().anchoredPosition.x;

        float desiredXAtLeft = scanlinePosition - viewPortWidth * 0.1f;
        float normalizedPosition =
            desiredXAtLeft / (WorkspaceContentWidth - viewPortWidth);
        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(normalizedPosition);
        SynchronizeScrollRects();
    }

    private void SynchronizeScrollRects()
    {
        headerContent.sizeDelta = new Vector2(
            workspaceContent.sizeDelta.x,
            headerContent.sizeDelta.y);
        headerScrollRect.horizontalNormalizedPosition =
            workspaceScrollRect.horizontalNormalizedPosition;
    }
    #endregion
}
