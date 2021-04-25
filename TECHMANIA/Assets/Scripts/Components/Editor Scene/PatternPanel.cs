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
    public Slider scanlinePositionSlider;
    public Snackbar snackbar;
    public MessageDialog messageDialog;
    public TimeEventDialog timeEventDialog;

    #region Internal Data Structures
    // Each NoteObject contains a reference to a Note, and this
    // dictionary is the reverse of that. Must be updated alongside
    // EditorContext.Pattern at all times.
    private Dictionary<Note, NoteObject> noteToNoteObject;
    // Maintain a list of all drag notes, so when the workspace
    // receives a click, it can check if it should have landed on
    // any drag note.
    private HashSet<NoteInEditor> dragNotes;

    private Note lastSelectedNoteWithoutShift;
    public HashSet<GameObject> selectedNoteObjects
    { get; private set; }

    private Note GetNoteFromGameObject(GameObject o)
    {
        return o.GetComponent<NoteObject>().note;
    }
    
    private GameObject GetGameObjectFromNote(Note n)
    {
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
    private static int PlayableLanes => 4;
    private static int TotalLanes => 64;

    private static float WorkspaceViewportHeight;
    private static int VisibleLanes;
    public static float LaneHeight =>
        WorkspaceViewportHeight / VisibleLanes;
    public static float WorkspaceContentHeight => LaneHeight *
        TotalLanes;
    #endregion

    #region Horizontal Spacing
    private int numScans;
    private static int zoom;
    private int beatSnapDivisor;
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
    public static event UnityAction<HashSet<GameObject>> 
        SelectionChanged;
    public static event UnityAction KeysoundVisibilityChanged;
    #endregion

    #region MonoBehavior APIs
    private void OnEnable()
    {
        // Vertical spacing
        VisibleLanes = int.Parse(
            visibleLanesDropdown.options
            [visibleLanesDropdown.value].text);
        Canvas.ForceUpdateCanvases();
        WorkspaceViewportHeight = workspaceViewport.rect.height;

        // Horizontal spacing
        numScans = 0;  // Will be updated in Refresh()
        zoom = 100;
        beatSnapDivisor = 2;

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
        keysoundSheet.Initialize();
        Options.RefreshInstance();

        // Playback
        audioLoaded = false;
        isPlaying = false;
        UpdatePlaybackUI();
        ResourceLoader.CacheAudioResources(
            EditorContext.trackFolder,
            cacheAudioCompleteCallback: OnResourceLoadComplete);

        Refresh();
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
        EditorOptionsTab.Opened += OnOptionsTabOpened;
        EditorOptionsTab.Closed += OnOptionsTabClosed;
    }

    private void OnDisable()
    {
        StopPlayback();
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
        EditorOptionsTab.Opened -= OnOptionsTabOpened;
        EditorOptionsTab.Closed -= OnOptionsTabClosed;
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
                    // Do nothing.
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
                        GameObject o = GetGameObjectFromNote(n);
                        selectedNoteObjects.Remove(o);
                        DeleteNote(o);
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
        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    private void OnRedo(EditTransaction transaction)
    {
        foreach (EditOperation op in transaction.ops)
        {
            switch (op.type)
            {
                case EditOperation.Type.Metadata:
                    // Do nothing.
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
                        GameObject o = GetGameObjectFromNote(n);
                        selectedNoteObjects.Remove(o);
                        DeleteNote(o);
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
        SelectionChanged?.Invoke(selectedNoteObjects);
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
                    n.volume, n.pan, n.endOfScan);
                break;
            case NoteType.Hold:
            case NoteType.RepeatHeadHold:
            case NoteType.RepeatHold:
                AddHoldNote(n.type, n.pulse, n.lane,
                    (n as HoldNote).duration, n.sound,
                    n.volume, n.pan, n.endOfScan);
                break;
            case NoteType.Drag:
                AddDragNote(n.pulse, n.lane,
                    (n as DragNote).nodes, n.sound,
                    n.volume, n.pan, (n as DragNote).curveType);
                break;
        }
    }

    private void RefreshNoteInEditor(Note n)
    {
        NoteInEditor e = GetGameObjectFromNote(n)
            .GetComponent<NoteInEditor>();
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
        RefreshScanlinePositionSlider();
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
            if (selectedNoteObjects.Count == 1)
            {
                Note n = null;
                foreach (GameObject o in selectedNoteObjects)
                {
                    n = GetNoteFromGameObject(o);
                }
                PlayKeysound(n);
            }
        }
    }
    #endregion

    #region Events From Workspace and NoteObjects
    public void OnWorkspaceScrollRectValueChanged(
        Vector2 value)
    {
        SynchronizeScrollRects();
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
            selectedNoteObjects.Clear();
            SelectionChanged?.Invoke(selectedNoteObjects);
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
                foreach (Note oInRange in range)
                {
                    selectedNoteObjects.Add(
                        GetGameObjectFromNote(oInRange));
                }
            }
            else  // !ctrl
            {
                // Overwrite current selection with [prev, o].
                selectedNoteObjects.Clear();
                foreach (Note oInRange in range)
                {
                    selectedNoteObjects.Add(
                        GetGameObjectFromNote(oInRange));
                }
            }
        }
        else  // !shift
        {
            lastSelectedNoteWithoutShift = clickedNote;
            if (ctrl)
            {
                // Toggle o in current selection.
                ToggleSelection(o);
            }
            else  // !ctrl
            {
                if (selectedNoteObjects.Count > 1)
                {
                    selectedNoteObjects.Clear();
                    selectedNoteObjects.Add(o);
                }
                else if (selectedNoteObjects.Count == 1)
                {
                    if (selectedNoteObjects.Contains(o))
                    {
                        selectedNoteObjects.Remove(o);
                    }
                    else
                    {
                        selectedNoteObjects.Clear();
                        selectedNoteObjects.Add(o);
                    }
                }
                else  // Count == 0
                {
                    selectedNoteObjects.Add(o);
                }
            }
        }

        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    private void ToggleSelection(GameObject o)
    {
        if (selectedNoteObjects.Contains(o))
        {
            selectedNoteObjects.Remove(o);
        }
        else
        {
            selectedNoteObjects.Add(o);
        }
    }

    public void OnNoteObjectRightClick(GameObject o)
    {
        if (isPlaying) return;

        EditorContext.BeginTransaction();
        EditorContext.RecordDeletedNote(GetNoteFromGameObject(o));
        selectedNoteObjects.Remove(o);
        DeleteNote(o);
        EditorContext.EndTransaction();

        SelectionChanged?.Invoke(selectedNoteObjects);
    }
    #endregion

    #region UI Events And Updates
    public void OnBeatSnapDivisorChanged(int direction)
    {
        do
        {
            beatSnapDivisor += direction;
            if (beatSnapDivisor <= 0 && direction < 0)
            {
                beatSnapDivisor = Pattern.pulsesPerBeat;
            }
            if (beatSnapDivisor > Pattern.pulsesPerBeat && direction > 0)
            {
                beatSnapDivisor = 1;
            }
        }
        while (Pattern.pulsesPerBeat % beatSnapDivisor != 0);
        UpdateBeatSnapDivisorDisplay();
    }

    private void UpdateBeatSnapDivisorDisplay()
    {
        beatSnapDividerDisplay.text = beatSnapDivisor.ToString();
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
        VisibleLanes = int.Parse(
            visibleLanesDropdown.options
            [newValue].text);

        ResizeWorkspace();
        RepositionNeeded?.Invoke();
        AdjustAllPathsAndTrails();
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
        if (selectedNoteObjects.Count == 0) return;

        HashSet<GameObject> newSelection = new HashSet<GameObject>();
        EditorContext.BeginTransaction();
        foreach (GameObject o in selectedNoteObjects)
        {
            NoteObject n = o.GetComponent<NoteObject>();
            int pulse = n.note.pulse;
            int lane = n.note.lane;
            string sound = n.note.sound;
            float volume = n.note.volume;
            float pan = n.note.pan;
            bool endOfScan = n.note.endOfScan;

            int currentDuration = 0;
            if (n.note is HoldNote)
            {
                currentDuration = (n.note as HoldNote).duration;
            }
            if (n.note is DragNote)
            {
                currentDuration = (n.note as DragNote).Duration();
            }

            GameObject newObject = null;
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
                        new HashSet<GameObject>() { o },
                        out invalidReason))
                    {
                        snackbar.Show(invalidReason);
                        break;
                    }
                    EditorContext.RecordDeletedNote(n.note.Clone());
                    DeleteNote(o);
                    newObject = AddNote(noteType, pulse, lane, sound,
                        volume, pan, endOfScan);
                    EditorContext.RecordAddedNote(
                        GetNoteFromGameObject(newObject));
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    if (currentDuration == 0)
                    {
                        currentDuration = HoldNoteDefaultDuration(
                            pulse, lane);
                    }
                    EditorContext.RecordDeletedNote(n.note.Clone());
                    DeleteNote(o);
                    newObject = AddHoldNote(noteType, pulse, lane,
                        duration: currentDuration, sound,
                        volume, pan, endOfScan);
                    EditorContext.RecordAddedNote(
                        GetNoteFromGameObject(newObject));
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
                    EditorContext.RecordDeletedNote(n.note.Clone());
                    DeleteNote(o);
                    newObject = AddDragNote(pulse, lane,
                        nodes, sound, volume, pan);
                    EditorContext.RecordAddedNote(
                        GetNoteFromGameObject(newObject));
                    break;
            }

            if (newObject != null)
            {
                newSelection.Add(newObject);
            }
            else
            {
                newSelection.Add(o);
            }
        }
        EditorContext.EndTransaction();

        selectedNoteObjects = newSelection;
        SelectionChanged?.Invoke(selectedNoteObjects);
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
    }

    private void OnSelectedKeysoundsUpdated(List<string> keysounds)
    {
        if (selectedNoteObjects == null ||
            selectedNoteObjects.Count == 0) return;
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

        // Sort selected note objects, first by pulse, then by lane.
        List<NoteObject> sortedSelection = new List<NoteObject>();
        foreach (GameObject o in selectedNoteObjects)
        {
            sortedSelection.Add(o.GetComponent<NoteObject>());
        }
        sortedSelection.Sort((NoteObject e1, NoteObject e2) =>
        {
            Note n1 = e1.note;
            Note n2 = e2.note;
            if (n1.pulse != n2.pulse) return n1.pulse - n2.pulse;
            return n1.lane - n2.lane;
        });

        // Apply keysound.
        EditorContext.BeginTransaction();
        int keysoundIndex = 0;
        foreach (NoteObject n in sortedSelection)
        {
            EditOperation op = EditorContext
                .BeginModifyNoteOperation();
            op.noteBeforeOp = n.note.Clone();
            n.note.sound = keysounds[keysoundIndex];
            op.noteAfterOp = n.note.Clone();

            n.GetComponent<NoteInEditor>().SetKeysoundText();
            n.GetComponent<NoteInEditor>().UpdateKeysoundVisibility();
            keysoundIndex = (keysoundIndex + 1) % keysounds.Count;
        }
        EditorContext.EndTransaction();
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
    // - from ctrl+drag on anything, when the select tool is active
    private void OnBeginDraggingNotes(GameObject o)
    {
        draggedNoteObject = o;
        lastSelectedNoteWithoutShift = GetNoteFromGameObject(o);
        if (!selectedNoteObjects.Contains(o))
        {
            selectedNoteObjects.Clear();
            selectedNoteObjects.Add(o);

            SelectionChanged?.Invoke(selectedNoteObjects);
        }
    }

    private void OnDraggingNotes(Vector2 delta)
    {
        delta /= rootCanvas.localScale.x;
        if (Options.instance.editorOptions.lockNotesInTime)
        {
            delta.x = 0f;
        }

        foreach (GameObject o in selectedNoteObjects)
        {
            // This is only visual. Notes are only really moved
            // in OnNoteObjectEndDrag.
            o.GetComponent<RectTransform>().anchoredPosition += delta;
            o.GetComponent<NoteInEditor>()
                .KeepPathInPlaceWhileNoteBeingDragged(delta);
        }
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
        HashSet<GameObject> selectionAsSet =
            new HashSet<GameObject>(selectedNoteObjects);
        foreach (GameObject o in selectedNoteObjects)
        {
            Note n = o.GetComponent<NoteObject>().note;
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
                        ignoredExistingNotes: selectionAsSet,
                        out invalidReason);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    movable = movable && CanAddHoldNote(n.type,
                        newPulse, newLane, (n as HoldNote).duration,
                        ignoredExistingNotes: selectionAsSet,
                        out invalidReason);
                    break;
                case NoteType.Drag:
                    movable = movable && CanAddDragNote(
                        newPulse, newLane, (n as DragNote).nodes,
                        ignoredExistingNotes: selectionAsSet,
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
            HashSet<GameObject> replacedSelection =
                new HashSet<GameObject>();
            // These notes are not the ones added to the pattern.
            // They are created only to pass information to AddNote
            // methods.
            List<Note> movedNotes = new List<Note>();
            foreach (GameObject o in selectedNoteObjects)
            {
                NoteObject n = o.GetComponent<NoteObject>();

                Note movedNote = n.note.Clone();
                movedNote.pulse += deltaPulse;
                movedNote.lane += deltaLane;
                movedNotes.Add(movedNote);

                EditorContext.RecordDeletedNote(n.note.Clone());
                DeleteNote(o);
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
                            movedNote.volume,
                            movedNote.pan,
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
                            movedNote.volume,
                            movedNote.pan,
                            movedNote.endOfScan);
                        break;
                    case NoteType.Drag:
                        o = AddDragNote(
                            movedNote.pulse,
                            movedNote.lane,
                            (movedNote as DragNote).nodes,
                            movedNote.sound,
                            movedNote.volume,
                            movedNote.pan,
                            (movedNote as DragNote).curveType);
                        break;
                }
                EditorContext.RecordAddedNote(
                    GetNoteFromGameObject(o));
                replacedSelection.Add(o);
                if (movedNote.pulse == oldPulse + deltaPulse &&
                    movedNote.lane == oldLane + deltaLane)
                {
                    lastSelectedNoteWithoutShift =
                        GetNoteFromGameObject(o);
                }
            }
            EditorContext.EndTransaction();
            selectedNoteObjects = replacedSelection;
            SelectionChanged?.Invoke(selectedNoteObjects);
        }

        foreach (GameObject o in selectedNoteObjects)
        {
            o.GetComponent<SelfPositionerInEditor>().Reposition();
            o.GetComponent<NoteInEditor>().ResetPathPosition();
        }
    }
    #endregion

    #region Hold Note Duration Adjustment
    private List<GameObject> holdNotesBeingAdjusted;
    private GameObject initialHoldNoteBeingAdjusted;
    private void OnDurationHandleBeginDrag(
        PointerEventData eventData, GameObject note)
    {
        if (isPlaying) return;
        if (tool == Tool.Rectangle ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerBeginDrag(eventData);
            return;
        }

        holdNotesBeingAdjusted = new List<GameObject>();
        if (selectedNoteObjects.Contains(note))
        {
            // Adjust all hold notes in the selection.
            foreach (GameObject o in selectedNoteObjects)
            {
                NoteType noteType = o.GetComponent<NoteObject>().note.type;
                if (noteType == NoteType.Hold ||
                    noteType == NoteType.RepeatHeadHold ||
                    noteType == NoteType.RepeatHold)
                {
                    holdNotesBeingAdjusted.Add(o);
                }
            }
        }
        else
        {
            // Adjust only the dragged note and ignore selection.
            holdNotesBeingAdjusted.Add(note);
        }
        initialHoldNoteBeingAdjusted = note;

        foreach (GameObject o in holdNotesBeingAdjusted)
        {
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

        foreach (GameObject o in holdNotesBeingAdjusted)
        {
            // This is only visual; duration is only really changed
            // in OnDurationHandleEndDrag.
            o.GetComponent<NoteInEditor>().AdjustTrailLength(
                delta.x);
        }
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
        foreach (GameObject o in holdNotesBeingAdjusted)
        {
            HoldNote holdNote = o.GetComponent<NoteObject>().note as HoldNote;
            oldDuration = holdNote.duration;
            newDuration = oldDuration + deltaDuration;
            if (newDuration <= 0)
            {
                snackbar.Show(Locale.GetString(
                    "pattern_panel_snackbar_hold_note_zero_length"));
                adjustable = false;
                break;
            }
            if (HoldNoteCoversAnotherNote(holdNote.pulse, holdNote.lane,
                newDuration, ignoredExistingNotes: null))
            {
                snackbar.Show(Locale.GetString(
                    "pattern_panel_snackbar_hold_note_covers_other_notes"));
                adjustable = false;
                break;
            }
        }

        if (adjustable)
        {
            // Apply adjustment. No need to delete and respawn notes
            // this time.
            EditorContext.BeginTransaction();
            foreach (GameObject o in holdNotesBeingAdjusted)
            {
                EditOperation op = EditorContext
                    .BeginModifyNoteOperation();
                HoldNote holdNote = o.GetComponent<NoteObject>()
                    .note as HoldNote;
                op.noteBeforeOp = holdNote.Clone();
                holdNote.duration += deltaDuration;
                op.noteAfterOp = holdNote.Clone();
            }
            EditorContext.EndTransaction();
            UpdateNumScansAndRelatedUI();
        }

        foreach (GameObject o in holdNotesBeingAdjusted)
        {
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
        if (dragNote.nodes.Find((DragNode node) =>
        {
            return node.anchor.pulse == newAnchor.pulse;
        }) != null)
        {
            snackbar.Show(Locale.GetString(
                "pattern_panel_snackbar_anchor_too_close_to_existing"));
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
    private DragNode draggedDragNodeClone;
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

        if (anchorIndex == 0)
        {
            snackbar.Show(Locale.GetString(
                "pattern_panel_snackbar_cannot_delete_first_anchor"));
            return;
        }

        DragNote dragNote = anchor
            .GetComponentInParent<NoteObject>().note as DragNote;
        if (dragNote.nodes.Count == 2)
        {
            snackbar.Show(Locale.GetString(
                "pattern_panel_snackbar_at_least_two_anchors"));
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
        draggedDragNodeClone = draggedDragNode.Clone();

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
    }

    private bool ValidateMovedAnchor(DragNode movedNode,
        out string invalidReason)
    {
        DragNote dragNote = draggedAnchor
            .GetComponentInParent<NoteObject>().note as DragNote;
        int anchorIndex = draggedAnchor
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;

        // Check 1: anchors are still in order.
        bool anchorsInOrder = movedNode.anchor.pulse >
            dragNote.nodes[anchorIndex - 1].anchor.pulse;
        if (anchorIndex < dragNote.nodes.Count - 1)
        {
            anchorsInOrder = anchorsInOrder &&
                movedNode.anchor.pulse <
                dragNote.nodes[anchorIndex + 1].anchor.pulse;
        }
        if (!anchorsInOrder)
        {
            invalidReason = Locale.GetString(
                "pattern_panel_snackbar_anchors_flow_left_ro_right");
            return false;
        }

        invalidReason = null;
        return true;
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

        DragNode cloneAtEndDrag = draggedDragNode.Clone();
        // Temporarily restore pre-drag data for snapshotting.
        draggedDragNode.CopyFrom(draggedDragNodeClone);
        
        bool valid;
        string invalidReason = "";
        if (ctrlHeldOnAnchorBeginDrag)
        {
            valid = true;
        }
        else
        {
            valid = ValidateMovedAnchor(cloneAtEndDrag,
                out invalidReason);
        }
        if (!valid)
        {
            snackbar.Show(invalidReason);
            NoteInEditor noteInEditor = draggedAnchor
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetAllAnchorsAndControlPoints();
            return;
        }

        DragNote dragNote = draggedAnchor
            .GetComponentInParent<NoteObject>().note as DragNote;
        EditorContext.BeginTransaction();
        EditOperation op = EditorContext.BeginModifyNoteOperation();
        op.noteBeforeOp = dragNote.Clone();
        draggedDragNode.CopyFrom(cloneAtEndDrag);
        op.noteAfterOp = dragNote.Clone();
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
        draggedDragNodeClone = draggedDragNode.Clone();
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

        DragNode cloneAtEndDrag = draggedDragNode.Clone();
        // Temporarily restore pre-drag data for snapshotting.
        draggedDragNode.CopyFrom(draggedDragNodeClone);

        // Are the control points' current position valid?
        bool valid = cloneAtEndDrag.GetControlPoint(0).pulse <= 0f
            && cloneAtEndDrag.GetControlPoint(1).pulse >= 0f;
        if (!valid)
        {
            snackbar.Show(Locale.GetString(
                "pattern_panel_snackbar_control_point_on_correct_sides"));
            NoteInEditor noteInEditor = draggedControlPoint
                .GetComponentInParent<NoteInEditor>();
            noteInEditor.ResetCurve();
            noteInEditor.ResetAllAnchorsAndControlPoints();
            return;
        }

        DragNote dragNote = draggedControlPoint
            .GetComponentInParent<NoteObject>().note as DragNote;
        EditorContext.BeginTransaction();
        EditOperation op = EditorContext.BeginModifyNoteOperation();
        op.noteBeforeOp = dragNote.Clone();
        draggedDragNode.CopyFrom(cloneAtEndDrag);
        op.noteAfterOp = dragNote.Clone();
        EditorContext.EndTransaction();
    }
    #endregion

    #region Refreshing
    // This deletes and respawns everything, therefore is extremely
    // slow.
    private void Refresh()
    {
        DestroyAndRespawnExistingNotes();
        UpdateNumScans();
        DestroyAndRespawnAllMarkers();
        ResizeWorkspace();
        RefreshScanlinePositionSlider();
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

    private void RefreshScanlinePositionSlider()
    {
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

        NoteObject noteObject = Instantiate(prefab,
            noteContainer).GetComponent<NoteObject>();
        noteObject.note = n;
        NoteInEditor noteInEditor = noteObject
            .GetComponent<NoteInEditor>();
        noteInEditor.SetKeysoundText();
        noteInEditor.UpdateKeysoundVisibility();
        noteInEditor.UpdateEndOfScanIndicator();
        if (n.lane >= PlayableLanes) noteInEditor.UseHiddenSprite();
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

    private void DestroyAndRespawnExistingNotes()
    {
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            Destroy(noteContainer.GetChild(i).gameObject);
        }
        noteToNoteObject = new Dictionary<Note, NoteObject>();
        dragNotes = new HashSet<NoteInEditor>();
        lastSelectedNoteWithoutShift = null;
        selectedNoteObjects = new HashSet<GameObject>();
        SelectionChanged?.Invoke(selectedNoteObjects);

        foreach (Note n in EditorContext.Pattern.notes)
        {
            SpawnNoteObject(n);
        }

        AdjustAllPathsAndTrails();
    }

    private void GetPreviousAndNextNoteObjects(
        Note n, HashSet<NoteType> types,
        int minLaneInclusive, int maxLaneInclusive,
        out GameObject prev, out GameObject next)
    {
        Note prevNote = EditorContext.Pattern
            .GetClosestNoteBefore(n.pulse, types,
            minLaneInclusive,
            maxLaneInclusive);
        Note nextNote = EditorContext.Pattern
            .GetClosestNoteAfter(n.pulse, types,
            minLaneInclusive,
            maxLaneInclusive);
        prev = null;
        if (prevNote != null)
        {
            prev = GetGameObjectFromNote(prevNote);
        }
        next = null;
        if (nextNote != null)
        {
            next = GetGameObjectFromNote(nextNote);
        }
    }

    private void GetPreviousAndNextChainNotes(Note n,
        out GameObject prev, out GameObject next)
    {
        GetPreviousAndNextNoteObjects(n,
            new HashSet<NoteType>()
                { NoteType.ChainHead, NoteType.ChainNode },
            minLaneInclusive: 0,
            maxLaneInclusive: PlayableLanes - 1,
            out prev, out next);
    }

    private void GetPreviousAndNextRepeatNotes(Note n,
        out GameObject prev, out GameObject next)
    {
        GetPreviousAndNextNoteObjects(n,
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
        NoteObject n = o.GetComponent<NoteObject>();

        if (n.note.type == NoteType.ChainHead ||
            n.note.type == NoteType.ChainNode)
        {
            if (n.note.lane >= 0 && n.note.lane < PlayableLanes)
            {
                GameObject prev, next;
                GetPreviousAndNextChainNotes(n.note,
                    out prev, out next);

                if (n.note.type == NoteType.ChainNode)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(prev);
                }
                if (next != null &&
                    next.GetComponent<NoteObject>().note.type
                    == NoteType.ChainNode)
                {
                    next.GetComponent<NoteInEditor>()
                        .PointPathToward(o);
                }
            }
        }

        if (n.note.type == NoteType.RepeatHead ||
            n.note.type == NoteType.RepeatHeadHold ||
            n.note.type == NoteType.Repeat ||
            n.note.type == NoteType.RepeatHold)
        {
            if (n.note.lane >= 0 && n.note.lane < PlayableLanes)
            {
                GameObject prev, next;
                GetPreviousAndNextRepeatNotes(n.note,
                    out prev, out next);

                if (n.note.type == NoteType.Repeat ||
                    n.note.type == NoteType.RepeatHold)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(prev);
                }
                
                if (next != null)
                {
                    NoteType nextType = next
                        .GetComponent<NoteObject>().note.type;
                    if (nextType == NoteType.Repeat ||
                        nextType == NoteType.RepeatHold)
                    {
                        next.GetComponent<NoteInEditor>()
                            .PointPathToward(o);
                    }
                }
            }
        }

        if (n.note.type == NoteType.Hold ||
            n.note.type == NoteType.RepeatHeadHold ||
            n.note.type == NoteType.RepeatHold)
        {
            o.GetComponent<NoteInEditor>().ResetTrail();
        }

        if (n.note.type == NoteType.Drag)
        {
            o.GetComponent<NoteInEditor>().ResetCurve();
            o.GetComponent<NoteInEditor>()
                .ResetAllAnchorsAndControlPoints();
        }
    }

    // This may modify o, the same-type note before o, and/or
    // the same-type note after o.
    private void AdjustPathBeforeDeleting(GameObject o)
    {
        NoteObject n = o.GetComponent<NoteObject>();
        if (n.note.lane < 0 || n.note.lane >= PlayableLanes) return;
        switch (n.note.type)
        {
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                {
                    GameObject prev, next;
                    GetPreviousAndNextChainNotes(n.note,
                        out prev, out next);

                    if (next != null &&
                        next.GetComponent<NoteObject>().note.type
                        == NoteType.ChainNode)
                    {
                        next.GetComponent<NoteInEditor>()
                            .PointPathToward(prev);
                    }
                    else if (prev != null &&
                        prev.GetComponent<NoteObject>().note.type
                        == NoteType.ChainHead)
                    {
                        prev.GetComponent<NoteInEditor>()
                            .ResetNoteImageRotation();
                    }
                }
                break;
            case NoteType.RepeatHead:
            case NoteType.RepeatHeadHold:
            case NoteType.Repeat:
            case NoteType.RepeatHold:
                {
                    GameObject prev, next;
                    GetPreviousAndNextRepeatNotes(n.note,
                        out prev, out next);

                    if (next != null)
                    {
                        NoteType nextType = next
                            .GetComponent<NoteObject>().note.type;
                        if (nextType == NoteType.Repeat ||
                            nextType == NoteType.RepeatHold)
                        {
                            next.GetComponent<NoteInEditor>()
                                .PointPathToward(prev);
                        }
                    }
                }
                break;
            default:
                break;
        }
    }

    private void AdjustAllPathsAndTrails()
    {
        GameObject previousChain = null;
        List<GameObject> previousRepeat = new List<GameObject>();
        for (int i = 0; i < PlayableLanes; i++)
        {
            previousRepeat.Add(null);
        }

        foreach (Note n in EditorContext.Pattern.notes)
        {
            GameObject o = GetGameObjectFromNote(n);
            if (n.type == NoteType.ChainHead ||
                n.type == NoteType.ChainNode)
            {
                // Adjust the paths of chain nodes.
                if (n.lane >= PlayableLanes) continue;
                if (n.type == NoteType.ChainNode)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(previousChain);
                }
                previousChain = o;
            }
            if (n.type == NoteType.RepeatHead ||
                n.type == NoteType.Repeat ||
                n.type == NoteType.RepeatHeadHold ||
                n.type == NoteType.RepeatHold)
            {
                // Adjust the paths of repeat notes.
                if (n.lane >= PlayableLanes) continue;
                if (n.type == NoteType.Repeat ||
                    n.type == NoteType.RepeatHold)
                {
                    o.GetComponent<NoteInEditor>()
                        .PointPathToward(previousRepeat[n.lane]);
                }
                previousRepeat[n.lane] = o;
            }
            if (n.type == NoteType.Hold ||
                n.type == NoteType.RepeatHeadHold ||
                n.type == NoteType.RepeatHold)
            {
                // Adjust the trails of hold notes.
                o.GetComponent<NoteInEditor>().ResetTrail();
            }
            if (n.type == NoteType.Drag)
            {
                // Draw curves of drag notes.
                o.GetComponent<NoteInEditor>().ResetCurve();
                o.GetComponent<NoteInEditor>()
                    .ResetAllAnchorsAndControlPoints();
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
        HashSet<GameObject> ignoredExistingNotes,
        out string reason)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }

        // Boundary check.
        if (pulse < 0)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_before_scan_0");
            return false;
        }
        if (lane < 0)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_above_topmost_lane");
            return false;
        }
        if (lane >= TotalLanes)
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_below_bottommost_lane");
            return false;
        }

        // Overlap check.
        Note noteAtSamePulseAndLane = EditorContext.Pattern
            .GetNoteAt(pulse, lane);
        if (noteAtSamePulseAndLane != null &&
            !ignoredExistingNotes.Contains(
                GetGameObjectFromNote(noteAtSamePulseAndLane)))
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_on_top_of_existing_note");
            return false;
        }

        // Chain check.
        if (type == NoteType.ChainHead || type == NoteType.ChainNode)
        {
            foreach (Note noteAtPulse in EditorContext.Pattern
                .GetViewBetween(pulse, pulse))
            {
                if (ignoredExistingNotes.Contains(
                    GetGameObjectFromNote(noteAtPulse)))
                {
                    continue;
                }
                
                if (noteAtPulse.type == NoteType.ChainHead ||
                    noteAtPulse.type == NoteType.ChainNode)
                {
                    reason = Locale.GetString(
                        "pattern_panel_snackbar_chain_note_at_same_pulse");
                    return false;
                }
            }
        }

        // Hold check.
        Note holdNoteBeforePivot =
            EditorContext.Pattern.GetClosestNoteBefore(pulse,
            new HashSet<NoteType>()
            {
                NoteType.Hold,
                NoteType.RepeatHeadHold,
                NoteType.RepeatHold
            }, minLaneInclusive: lane, maxLaneInclusive: lane);
        if (holdNoteBeforePivot != null &&
            !ignoredExistingNotes.Contains(
                GetGameObjectFromNote(holdNoteBeforePivot)))
        {
            HoldNote holdNote = holdNoteBeforePivot as HoldNote;
            if (holdNote.pulse + holdNote.duration >= pulse)
            {
                reason = Locale.GetString(
                    "pattern_panel_snackbar_covered_by_hold_note");
                return false;
            }
        }
            
        reason = null;
        return true;
    }

    private bool CanAddHoldNote(NoteType type, int pulse, int lane,
        int duration, HashSet<GameObject> ignoredExistingNotes,
        out string reason)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }
        if (!CanAddNote(type, pulse, lane, ignoredExistingNotes,
            out reason))
        {
            return false;
        }

        // Additional check for hold notes.
        if (HoldNoteCoversAnotherNote(pulse, lane, duration,
            ignoredExistingNotes))
        {
            reason = Locale.GetString(
                "pattern_panel_snackbar_hold_note_covers_other_notes");
            return false;
        }

        return true;
    }

    private bool CanAddDragNote(int pulse, int lane,
        List<DragNode> nodes,
        HashSet<GameObject> ignoredExistingNotes,
        out string reason)
    {
        return CanAddNote(NoteType.Drag, pulse, lane,
            ignoredExistingNotes,
            out reason);
    }

    // Ignores notes at (pulse, lane), if any.
    private bool HoldNoteCoversAnotherNote(int pulse, int lane,
        int duration,
        HashSet<GameObject> ignoredExistingNotes)
    {
        if (ignoredExistingNotes == null)
        {
            ignoredExistingNotes = new HashSet<GameObject>();
        }

        Note noteAfterPivot = EditorContext.Pattern
            .GetClosestNoteAfter(
            pulse, types: null,
            minLaneInclusive: lane,
            maxLaneInclusive: lane);
        if (noteAfterPivot != null &&
            !ignoredExistingNotes.Contains(
                GetGameObjectFromNote(noteAfterPivot)))
        {
            if (pulse + duration >= noteAfterPivot.pulse)
            {
                return true;
            }
        }

        return false;
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
        float volume = Note.defaultVolume,
        float pan = Note.defaultPan,
        bool endOfScan = false)
    {
        Note n = new Note()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,
            volume = volume,
            pan = pan,
            endOfScan = endOfScan
        };
        return FinishAddNote(n);
    }

    private GameObject AddHoldNote(NoteType type,
        int pulse, int lane, int? duration, string sound,
        float volume = Note.defaultVolume,
        float pan = Note.defaultPan,
        bool endOfScan = false)
    {
        if (!duration.HasValue)
        {
            duration = HoldNoteDefaultDuration(pulse, lane);
        }
        HoldNote n = new HoldNote()
        {
            type = type,
            pulse = pulse,
            lane = lane,
            sound = sound,
            duration = duration.Value,
            volume = volume,
            pan = pan,
            endOfScan = endOfScan
        };
        return FinishAddNote(n);
    }

    private GameObject AddDragNote(int pulse, int lane,
        List<DragNode> nodes, string sound,
        float volume = Note.defaultVolume,
        float pan = Note.defaultPan,
        CurveType curveType = CurveType.Bezier)
    {
        if (nodes == null)
        {
            nodes = new List<DragNode>();
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
        DragNote n = new DragNote()
        {
            type = NoteType.Drag,
            pulse = pulse,
            lane = lane,
            sound = sound,
            nodes = nodes,
            volume = volume,
            pan = pan,
            curveType = curveType
        };
        return FinishAddNote(n);
    }

    // Cannot remove o from selectedNoteObjects because the
    // caller may be enumerating that list.
    private void DeleteNote(GameObject o)
    {
        // Delete from pattern.
        Note n = GetNoteFromGameObject(o);
        EditorContext.Pattern.notes.Remove(n);

        // Delete from UI.
        AdjustPathBeforeDeleting(o);
        noteToNoteObject.Remove(n);
        if (n.type == NoteType.Drag)
        {
            dragNotes.Remove(o.GetComponent<NoteInEditor>());
        }
        if (lastSelectedNoteWithoutShift == n)
        {
            lastSelectedNoteWithoutShift = null;
        }
        // Destroy doesn't immedialy destroy, so we move the note
        // to the cemetary so as to not interfere with the
        // binary searches when spawning new notes on the same frame.
        o.transform.SetParent(noteCemetary);
        Destroy(o);
        UpdateNumScansAndRelatedUI();
    }

    private void ToggleEndOfScanOnSelectedNotes()
    {
        if (selectedNoteObjects.Count == 0) return;
        bool currentValue = false;
        foreach (GameObject o in selectedNoteObjects)
        {
            currentValue = GetNoteFromGameObject(o).endOfScan;
            break;
        }
        SetEndOfScanOnSelectedNotes(!currentValue);

        // Force the side sheet to update.
        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    public void SetEndOfScanOnSelectedNotes(bool newValue)
    {
        EditorContext.BeginTransaction();
        foreach (GameObject o in selectedNoteObjects)
        {
            Note n = o.GetComponent<NoteObject>().note;
            EditOperation op = EditorContext
                .BeginModifyNoteOperation();
            op.noteBeforeOp = n.Clone();
            n.endOfScan = newValue;
            op.noteAfterOp = n.Clone();
        }
        EditorContext.EndTransaction();

        foreach (GameObject o in selectedNoteObjects)
        {
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
            if (selectedNoteObjects.Count == 0) return;
            foreach (GameObject o in selectedNoteObjects)
            {
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
            if (selectedNoteObjects.Count == 0) return;
            OnDraggingNotes(delta);
        }
        else
        {
            UpdateRectangle();
        }
    }

    private void OnEndDragWhenRectangleToolActive()
    {
        if (movingNotesWhenRectangleToolActive)
        {
            if (selectedNoteObjects.Count == 0) return;
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
        HashSet<GameObject> notesInRectangle =
            new HashSet<GameObject>();
        foreach (Note n in EditorContext.Pattern
            .GetViewBetween(minPulse, maxPulse))
        {
            if (n.lane >= minLane && n.lane <= maxLane)
            {
                notesInRectangle.Add(GetGameObjectFromNote(n));
            }
        }

        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);
        if (shift)
        {
            // Append rectangle to selection.
            foreach (GameObject o in notesInRectangle)
            {
                selectedNoteObjects.Add(o);
            }
        }
        else if (alt)
        {
            // Subtract rectangle from selection.
            foreach (GameObject o in notesInRectangle)
            {
                selectedNoteObjects.Remove(o);
            }
        }
        else
        {
            // Replace selection with rectangle.
            selectedNoteObjects = notesInRectangle;
        }
        SelectionChanged?.Invoke(selectedNoteObjects);
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
        selectedNoteObjects.Clear();
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            selectedNoteObjects.Add(noteContainer.GetChild(i).gameObject);
        }
        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    public void SelectNone()
    {
        selectedNoteObjects.Clear();
        SelectionChanged?.Invoke(selectedNoteObjects);
    }

    public void CutSelection()
    {
        if (selectedNoteObjects.Count == 0) return;

        CopySelection();
        DeleteSelection();
    }

    public void CopySelection()
    {
        if (selectedNoteObjects.Count == 0) return;

        if (clipboard == null)
        {
            clipboard = new List<Note>();
        }
        clipboard.Clear();
        minPulseInClipboard = int.MaxValue;
        foreach (GameObject o in selectedNoteObjects)
        {
            Note n = GetNoteFromGameObject(o);
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
                        n.volume, n.pan, n.endOfScan);
                    break;
                case NoteType.Hold:
                case NoteType.RepeatHeadHold:
                case NoteType.RepeatHold:
                    newObject = AddHoldNote(n.type, newPulse,
                        n.lane, (n as HoldNote).duration,
                        n.sound,
                        n.volume, n.pan, n.endOfScan);
                    break;
                case NoteType.Drag:
                    newObject = AddDragNote(newPulse, n.lane,
                        (n as DragNote).nodes,
                        n.sound,
                        n.volume, n.pan, (n as DragNote).curveType);
                    break;
            }
            EditorContext.RecordAddedNote(
                GetNoteFromGameObject(newObject));
        }
        EditorContext.EndTransaction();
    }

    public void DeleteSelection()
    {
        if (selectedNoteObjects.Count == 0) return;
        if (isPlaying) return;

        // Delete notes from pattern.
        EditorContext.BeginTransaction();
        foreach (GameObject o in selectedNoteObjects)
        {
            EditorContext.RecordDeletedNote(
                GetNoteFromGameObject(o));
            DeleteNote(o);
        }
        EditorContext.EndTransaction();

        selectedNoteObjects.Clear();
        SelectionChanged?.Invoke(selectedNoteObjects);
    }
    #endregion

    #region Playback
    // During playback, the following features are disabled:
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

    private void OnResourceLoadComplete(string error)
    {
        if (error != null)
        {
            messageDialog.Show(Locale.GetStringAndFormat(
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
        pattern.CalculateTimeOfAllNotes();
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
                        n.lane > PlayableLanes,
                        startTime: playbackStartingTime - n.time,
                        n.volume, n.pan);
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
    }

    public void StopPlayback()
    {
        if (!isPlaying) return;
        isPlaying = false;
        UpdatePlaybackUI();

        audioSourceManager.StopAll();
        if (Options.instance.editorOptions.returnScanlineAfterPlayback)
        {
            scanline.floatPulse = playbackStartingPulse;
        }
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        ScrollScanlineIntoView();
        RefreshScanlinePositionSlider();
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
                nextNote.lane > PlayableLanes,
                startTime: 0f,
                nextNote.volume, nextNote.pan);
        }

        // Move scanline.
        scanline.floatPulse = playbackCurrentPulse;
        scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        ScrollScanlineIntoView();
        RefreshScanlinePositionSlider();
    }

    public void PlayKeysound(Note n)
    {
        AudioClip clip = ResourceLoader.GetCachedClip(
            n.sound);
        audioSourceManager.PlayKeysound(clip,
            n.lane > PlayableLanes, 0f,
            n.volume, n.pan);
    }
    #endregion

    #region Utilities
    private int SnapPulse(float rawPulse)
    {
        int pulsesPerDivision = Pattern.pulsesPerBeat / beatSnapDivisor;
        int snappedPulse = Mathf.RoundToInt(rawPulse / pulsesPerDivision)
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

    private Vector2 PulseAndLaneToPointInNoteContainer(
        float pulse, float lane)
    {
        int bps = EditorContext.Pattern.patternMetadata.bps;
        float scan = pulse / Pattern.pulsesPerBeat / bps;
        return new Vector2(
            scan * ScanWidth,
            -(lane + 0.5f) * LaneHeight);
    }

    private void ScrollScanlineIntoView()
    {
        if (!Options.instance.editorOptions.keepScanlineInView) return;

        float viewPortWidth = workspaceScrollRect
            .GetComponent<RectTransform>().rect.width;
        if (WorkspaceContentWidth <= viewPortWidth) return;

        float scanlinePosition = scanline
            .GetComponent<RectTransform>().anchoredPosition.x;

        float xAtViewPortLeft = (WorkspaceContentWidth - viewPortWidth)
            * workspaceScrollRect.horizontalNormalizedPosition;
        float xAtViewPortRight = xAtViewPortLeft + viewPortWidth;

        if (scanlinePosition >= xAtViewPortLeft &&
            scanlinePosition < xAtViewPortRight)
        {
            // No need to scroll.
            return;
        }

        float desiredXAtLeft;
        if (scanlinePosition < xAtViewPortLeft)
        {
            // Scrolling left: put scanline at right side.
            desiredXAtLeft = scanlinePosition - viewPortWidth * 0.8f;
        }
        else
        {
            // Scrolling right: put scanline at left side.
            desiredXAtLeft = scanlinePosition - viewPortWidth * 0.2f;
        }
        float normalizedPosition =
            desiredXAtLeft / (WorkspaceContentWidth - viewPortWidth);
        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(normalizedPosition);
        SynchronizeScrollRects();
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
