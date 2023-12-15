using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static TreeEditor.TreeEditorHelper;

// Handles all UI inside the workspace.
// Does not hold the notes or selection; these remain in
// PatternPanel.
public class PatternPanelWorkspace : MonoBehaviour
{
    public PatternPanel panel;

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
    public NoteObject noteCursor;
    public Transform noteContainer;
    public Transform noteCemetary;
    public GameObject basicNotePrefab;
    public GameObject chainHeadPrefab;
    public GameObject chainNodePrefab;
    public GameObject repeatHeadPrefab;
    public GameObject repeatHeadHoldPrefab;
    public GameObject repeatNotePrefab;
    public GameObject repeatHoldPrefab;
    public GameObject holdNotePrefab;
    public GameObject dragNotePrefab;
    public RectTransform selectionRectangle;

    #region Internal data structures
    // Each NoteObject contains a reference to a Note, and this
    // dictionary is the reverse of that. Only contains the notes
    // close to the viewport.
    private Dictionary<Note, NoteObject> noteToNoteObject;

    // Maintain a list of all drag notes, so when the workspace
    // receives a click, it can check if it should land on any
    // drag note.
    private HashSet<NoteInEditor> dragNotes;

    private Note lastSelectedNoteWithoutShift;
    public Note lastClickedNote { get; private set; }

    private float unsnappedCursorPulse;
    private float unsnappedCursorLane;

    public GameObject GetGameObjectFromNote(Note n)
    {
        if (!noteToNoteObject.ContainsKey(n)) return null;
        return noteToNoteObject[n].gameObject;
    }

    private Note GetNoteFromGameObject(GameObject o)
    {
        return o.GetComponent<NoteObject>().note;
    }
    #endregion

    #region Vertical spacing
    public static int TotalLanes => 64;
    public static int VisibleLanes =>
        Options.instance.editorOptions.visibleLanes;
    private static float WorkspaceViewportHeight;
    private static float WorkspaceContentHeight =>
        LaneHeight * TotalLanes;
    public static float LaneHeight =>
        WorkspaceViewportHeight / VisibleLanes;
    #endregion

    #region Horizontal spacing
    private static int zoom = 0;
    private const int kMinZoom = 10;
    private const int kMaxZoom = 500;
    public static float ScanWidth => 10f * zoom;
    public static float PulseWidth => ScanWidth /
        EditorContext.Pattern.patternMetadata.bps /
        Pattern.pulsesPerBeat;
    public int numScans { get; private set; }  // display only
    private float WorkspaceContentWidth => numScans * ScanWidth;
    #endregion

    #region Outward events
    public static event UnityAction RepositionNeeded;
    #endregion

    #region MonoBehavior APIs
    public void InternalOnEnable()
    {
        // Hidden lanes
        hiddenLaneBackground.anchorMin = Vector2.zero;
        hiddenLaneBackground.anchorMax = new Vector2(
            1f, 1f - (float)PatternPanel.PlayableLanes
                / TotalLanes);

        // Vertical spacing
        Canvas.ForceUpdateCanvases();
        WorkspaceViewportHeight = workspaceViewport.rect.height;

        // Horizontal spacing
        numScans = 0;  // Will be updated in Refresh()
        if (zoom == 0) zoom = 100;  // Preserved through preview

        // Scanline
        scanlineFloatPulse = 0f;
        workspaceScrollRect.horizontalNormalizedPosition = 0f;
        headerScrollRect.horizontalNormalizedPosition = 0f;

        // Event handlers
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
    }

    public void InternalOnDisable()
    {
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
    }

    // Update is called once per frame
    void Update()
    {
        bool mouseInWorkspace = RectTransformUtility
            .RectangleContainsScreenPoint(
                workspaceScrollRect.GetComponent<RectTransform>(),
                Input.mousePosition);
        bool mouseInHeader = RectTransformUtility
            .RectangleContainsScreenPoint(
                headerScrollRect.GetComponent<RectTransform>(),
                Input.mousePosition);

        if (Input.mouseScrollDelta.y != 0 &&
            (mouseInWorkspace || mouseInHeader))
        {
            HandleMouseScroll(Input.mouseScrollDelta.y);
        }

        if (mouseInWorkspace &&
            PatternPanel.tool == PatternPanel.Tool.Note)
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
            !panel.isPlaying)
        {
            MoveScanlineToPointer(Input.mousePosition);
        }

        HandleKeyboardShortcuts();

        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            bool touchInHeader = RectTransformUtility
                .RectangleContainsScreenPoint(
                    headerScrollRect.GetComponent<RectTransform>(),
                    touch.position);

            if (Input.touchCount == 1 && touchInHeader &&
                !panel.isPlaying)
            {
                MoveScanlineToPointer(touch.position);
            }
            else if (Input.touchCount == 2)
            {
                HandleTouchResize();
            }
        }
    }
    #endregion

    public float scanlineFloatPulse
    {
        get { return scanline.floatPulse; }
        set
        {
            scanline.floatPulse = value;
            scanline.GetComponent<SelfPositionerInEditor>().Reposition();
        }
    }

    #region Undo and redo
    public void RefreshNoteInEditor(Note n)
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
    private void HandleMouseScroll(float y)
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);
        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);

        if (ctrl)
        {
            // Adjust zoom.
            int value = zoom + Mathf.FloorToInt(y * 5f);
            AdjustZoom(value);
        }
        else if (shift)
        {
            // Vertical scroll.
            workspaceScrollRect.verticalNormalizedPosition +=
                y * 100f / WorkspaceContentHeight;
            workspaceScrollRect.verticalNormalizedPosition =
                Mathf.Clamp01(
                workspaceScrollRect.verticalNormalizedPosition);
        }
        else if (!alt)
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

    private void HandleKeyboardShortcuts()
    {
        if (Input.GetKeyDown(KeyCode.F) && panel.selectedNotes.Count > 0)
        {
            foreach (Note n in panel.selectedNotes)
            {
                ScrollNoteIntoView(n);
                break;
            }
        }

        UnityAction<float> moveScanlineTo = (float pulse) =>
        {
            scanlineFloatPulse = pulse;
            ScrollScanlineIntoView();
            panel.playbackBar.Refresh();
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

        int snappedCursorPulse = panel.SnapPulse(unsnappedCursorPulse);
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

    private void DragWorkSpace(Vector2 deltaPosition)
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
        horizontal -= deltaPosition.x / panel.rootCanvas.localScale.x;
        workspaceScrollRect.horizontalNormalizedPosition =
            Mathf.Clamp01(horizontal / outOfViewWidth);

        float vertical =
            workspaceScrollRect.verticalNormalizedPosition *
            outOfViewHeight;
        vertical -= deltaPosition.y / panel.rootCanvas.localScale.x;
        workspaceScrollRect.verticalNormalizedPosition =
            Mathf.Clamp01(vertical / outOfViewHeight);

        SynchronizeScrollRects();
    }
    #endregion

    #region Events From Workspace and NoteObjects
    public void OnWorkspaceScrollRectValueChanged()
    {
        RefreshNotesInViewport();
        SynchronizeScrollRects();
    }

    public void RefreshNotesInViewport()
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
        visibleNotesAsSet.UnionWith(panel.selectedNotes);

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
        if (panel.UsingRectangleTool())
        {
            panel.selectedNotes.Clear();
            panel.NotifySelectionChanged();
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
        if (panel.isPlaying) return;

        panel.AddNoteAsTransaction(
            noteCursor.note.pulse, noteCursor.note.lane);
    }

    private bool draggingDragCurve;
    public void OnNoteContainerBeginDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerEventData =
            eventData as PointerEventData;
        if (panel.UsingRectangleTool() &&
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
        if (panel.UsingRectangleTool() &&
            p.button == PointerEventData.InputButton.Left)
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

        if (p.button == PointerEventData.InputButton.Middle
            || PatternPanel.tool == PatternPanel.Tool.Pan)
        {
            DragWorkSpace(p.delta);
        }
    }

    public void OnNoteContainerEndDrag(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        PointerEventData pointerEventData =
            eventData as PointerEventData;
        if (panel.UsingRectangleTool() &&
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
                    panel.selectedNotes.Add(noteInRange);
                }
            }
            else  // !ctrl
            {
                // Overwrite current selection with [prev, o].
                panel.selectedNotes.Clear();
                foreach (Note noteInRange in range)
                {
                    panel.selectedNotes.Add(noteInRange);
                }
            }
        }
        else  // !shift
        {
            lastSelectedNoteWithoutShift = clickedNote;
            if (ctrl)
            {
                // Toggle o in current selection.
                panel.ToggleSelection(clickedNote);
            }
            else if (PatternPanel.tool == 
                PatternPanel.Tool.RectangleAppend)
            {
                panel.selectedNotes.Add(clickedNote);
            }
            else if (PatternPanel.tool == 
                PatternPanel.Tool.RectangleSubtract)
            {
                panel.selectedNotes.Remove(clickedNote);
            }
            else  // !ctrl
            {
                if (panel.selectedNotes.Count > 1)
                {
                    panel.selectedNotes.Clear();
                    panel.selectedNotes.Add(clickedNote);
                }
                else if (panel.selectedNotes.Count == 1)
                {
                    if (panel.selectedNotes.Contains(clickedNote))
                    {
                        panel.selectedNotes.Remove(clickedNote);
                    }
                    else
                    {
                        panel.selectedNotes.Clear();
                        panel.selectedNotes.Add(clickedNote);
                    }
                }
                else  // Count == 0
                {
                    panel.selectedNotes.Add(clickedNote);
                }
            }
        }

        panel.NotifySelectionChanged();
    }

    public void OnNoteObjectRightClick(GameObject o)
    {
        if (panel.isPlaying) return;
        Note n = GetNoteFromGameObject(o);
        panel.DeleteNoteAsTransaction(n);
    }
    #endregion

    #region Touch
    private void HandleTouchResize()
    {
        Touch touchZero = Input.GetTouch(0);
        Touch touchOne = Input.GetTouch(1);

        Vector2 touchZeroPrevPos = touchZero.position
            - touchZero.deltaPosition;
        Vector2 touchOnePrevPos = touchOne.position
            - touchOne.deltaPosition;

        float prevMagnitude = (touchZeroPrevPos
            - touchOnePrevPos).magnitude;
        float currentMagnitude = (touchZero.position
            - touchOne.position).magnitude;

        float difference = currentMagnitude - prevMagnitude;

        AdjustZoom(zoom + (int)Mathf.Round(difference * 0.02f));
    }
    #endregion

    #region UI events and updates
    private void MoveScanlineToPointer(Vector2 position)
    {
        Vector2 pointInHeader;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            header, position,
            cam: null, out pointInHeader);

        int bps = EditorContext.Pattern.patternMetadata.bps;
        float cursorScan = pointInHeader.x / ScanWidth;
        float cursorPulse = cursorScan * bps * Pattern.pulsesPerBeat;
        int snappedCursorPulse = panel.SnapPulse(cursorPulse);

        scanlineFloatPulse = snappedCursorPulse;
        panel.playbackBar.Refresh();
    }

    public void RefreshKeysoundDisplay(Note n)
    {
        GameObject o = GetGameObjectFromNote(n);
        if (o == null) return;
        o.GetComponent<NoteInEditor>().SetKeysoundText();
        o.GetComponent<NoteInEditor>().UpdateKeysoundVisibility();
    }

    public void RefreshEndOfScanIndicator(Note n)
    {
        GameObject o = GetGameObjectFromNote(n);
        if (o == null) return;
        o.GetComponent<NoteInEditor>().UpdateEndOfScanIndicator();
    }

    public void RefreshDragNoteCurve(Note n)
    {
        GameObject o = GetGameObjectFromNote(n);
        if (o == null) return;

        o.GetComponent<NoteInEditor>().ResetCurve();
        o.GetComponent<NoteInEditor>().
            ResetAllAnchorsAndControlPoints();
    }
    #endregion

    #region Hold Note Duration Adjustment
    private List<Note> holdNotesBeingAdjusted;
    private GameObject initialHoldNoteBeingAdjusted;
    private void OnDurationHandleBeginDrag(
        PointerEventData eventData, GameObject noteObject)
    {
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerBeginDrag(eventData);
            return;
        }

        Note n = GetNoteFromGameObject(noteObject);
        holdNotesBeingAdjusted = new List<Note>();
        if (panel.selectedNotes.Contains(n))
        {
            // Adjust all hold notes in the selection.
            foreach (Note selectedNote in panel.selectedNotes)
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerDrag(eventData);
            return;
        }
        Vector2 delta = eventData.delta;
        delta /= panel.rootCanvas.localScale.x;

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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
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
                panel.snackbar.Show(reason);
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
        if (PatternPanel.tool != PatternPanel.Tool.Note)
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
            panel.snackbar.Show(reason);
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
        if (panel.isPlaying) return;
        if (eventData.dragging) return;
        if (panel.UsingRectangleTool())
        {
            // Event passes through.
            OnNoteContainerClick(eventData);
            return;
        }
        if (PatternPanel.tool != PatternPanel.Tool.Anchor &&
            eventData.button != PointerEventData.InputButton.Right)
        {
            // Event passes through.
            OnNoteContainerClick(eventData);
            return;
        }

        // Attempt to delete anchor.
        GameObject anchor = eventData.pointerDrag;
        int anchorIndex = anchor
            .GetComponentInParent<DragNoteAnchor>().anchorIndex;
        DragNote dragNote = anchor
            .GetComponentInParent<NoteObject>().note as DragNote;

        string reason;
        if (!EditorContext.Pattern.CanDeleteDragAnchor(
            dragNote, anchorIndex, out reason))
        {
            panel.snackbar.Show(reason);
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
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
        if ((ctrlHeldOnAnchorBeginDrag
            || PatternPanel.tool == PatternPanel.Tool.Anchor)
            && !dragCurveIsBSpline)
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerDrag(eventData);
            return;
        }

        Vector2 delta = eventData.delta;
        delta /= panel.rootCanvas.localScale.x;

        if (ctrlHeldOnAnchorBeginDrag ||
            PatternPanel.tool == PatternPanel.Tool.Anchor)
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
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
            panel.snackbar.Show(reason);
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
        if (panel.isPlaying) return;
        if (eventData.dragging) return;
        if (panel.UsingRectangleTool())
        {
            // Event passes through.
            OnNoteContainerClick(eventData);
            return;
        }
        if (PatternPanel.tool != PatternPanel.Tool.Anchor &&
            eventData.button != PointerEventData.InputButton.Right)
        {
            // Event passes through.
            OnNoteContainerClick(eventData);
            return;
        }

        // Delete control point.
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerDrag(eventData);
            return;
        }

        Vector2 delta = eventData.delta;
        delta /= panel.rootCanvas.localScale.x;
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
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
            panel.snackbar.Show(reason);
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

    #region Dragging and dropping notes
    private void OnNoteObjectBeginDrag(PointerEventData eventData,
        GameObject o)
    {
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
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
        if (panel.isPlaying) return;
        if (panel.UsingRectangleTool() ||
            eventData.button != PointerEventData.InputButton.Left)
        {
            // Event passes through.
            OnNoteContainerEndDrag(eventData);
            return;
        }

        OnEndDraggingNotes();
    }

    private GameObject draggedNoteObject;
    // The following can be called in 2 ways:
    // - from NoteObject's drag events, when any note type is active
    // - from ctrl+drag on anything, when the rectangle tool is active
    private void OnBeginDraggingNotes(GameObject o)
    {
        draggedNoteObject = o;
        lastSelectedNoteWithoutShift = GetNoteFromGameObject(o);
        if (!panel.selectedNotes.Contains(lastSelectedNoteWithoutShift))
        {
            panel.selectedNotes.Clear();
            panel.selectedNotes.Add(lastSelectedNoteWithoutShift);

            panel.NotifySelectionChanged();
        }
    }

    private void OnDraggingNotes(Vector2 delta)
    {
        delta /= panel.rootCanvas.localScale.x;
        if (Options.instance.editorOptions.lockNotesInTime)
        {
            delta.x = 0f;
        }

        foreach (Note n in panel.selectedNotes)
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

        panel.MoveSelectedNotesAsTransaction(deltaPulse, deltaLane);

        foreach (Note n in panel.selectedNotes)
        {
            if (n.pulse == oldPulse + deltaPulse &&
                n.lane == oldLane + deltaLane)
            {
                lastSelectedNoteWithoutShift = n;
            }

            GameObject o = GetGameObjectFromNote(n);
            if (o == null) continue;

            o.GetComponent<SelfPositionerInEditor>().Reposition();
            o.GetComponent<NoteInEditor>().ResetPathPosition();
        }

        if (panel.selectedNotes.Count == 1)
        {
            foreach (Note n in panel.selectedNotes)
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

    #region Refreshing
    public void DestroyAndSpawnExistingNotes()
    {
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            Destroy(noteContainer.GetChild(i).gameObject);
        }
        noteToNoteObject = new Dictionary<Note, NoteObject>();
        dragNotes = new HashSet<NoteInEditor>();
        lastSelectedNoteWithoutShift = null;
        lastClickedNote = null;
        panel.selectedNotes = new HashSet<Note>();
        panel.NotifySelectionChanged();

        RefreshNotesInViewport();
        AdjustAllPathsAndTrails();
    }

    public void ResizeWorkspace()
    {
        Debug.Log($"Resizing workspace content to: {WorkspaceContentWidth}, {WorkspaceContentHeight}");
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

    public void RepositionNotes()
    {
        RepositionNeeded?.Invoke();
    }

    // Returns whether the number changed.
    // During an editing session the number of scans will never
    // decrease. This prevents unintended scrolling when deleting
    // the last notes.
    public bool UpdateNumScans()
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

    public void UpdateNumScansAndRelatedUI()
    {
        if (UpdateNumScans())
        {
            DestroyAndRespawnAllMarkers();
            ResizeWorkspace();
        }
    }

    public GameObject CreateNewNoteObject(Note n)
    {
        GameObject newNote = SpawnNoteObject(n);
        AdjustPathOrTrailAround(newNote);
        UpdateNumScansAndRelatedUI();
        return newNote;
    }

    public void DeleteNoteObject(Note n)
    {
        GameObject o = GetGameObjectFromNote(n);
        if (o == null) return;
        DeleteNoteObject(n, o,
            intendToDeleteNote: true);

        if (lastSelectedNoteWithoutShift == n)
        {
            lastSelectedNoteWithoutShift = null;
        }
        if (lastClickedNote == n)
        {
            lastClickedNote = null;
        }
    }
    #endregion

    #region Spawning
    public void AdjustAllPathsAndTrails()
    {
        Note prevChain = null;
        // Indexed by lane
        List<Note> previousRepeat = new List<Note>();
        for (int i = 0; i < PatternPanel.PlayableLanes; i++)
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
            if (n.lane >= PatternPanel.PlayableLanes) continue;

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
        noteInEditor.SetSprite(hidden: n.lane >=
            PatternPanel.PlayableLanes);
        noteInEditor.UpdateSelection(panel.selectedNotes);
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
                if (n.lane >= PatternPanel.PlayableLanes) break;
                panel.GetPreviousAndNextChainNotes(n,
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
                if (n.lane >= PatternPanel.PlayableLanes) break;
                panel.GetPreviousAndNextRepeatNotes(n,
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
        if (n.lane < 0 || n.lane >= PatternPanel.PlayableLanes) return;
        Note prev, next;

        switch (n.type)
        {
            case NoteType.ChainHead:
            case NoteType.ChainNode:
                panel.GetPreviousAndNextChainNotes(n,
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
                panel.GetPreviousAndNextRepeatNotes(n,
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

    public void DestroyAndRespawnAllMarkers()
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
    #endregion

    #region Pattern modification
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
    #endregion

    #region Rectangle tool
    private bool movingNotesWhenRectangleToolActive;
    private Vector2 rectangleStart;
    private Vector2 rectangleEnd;

    private void OnBeginDragWhenRectangleToolActive()
    {
        movingNotesWhenRectangleToolActive =
            Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        if (movingNotesWhenRectangleToolActive)
        {
            if (panel.selectedNotes.Count == 0) return;
            foreach (Note n in panel.selectedNotes)
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
            if (panel.selectedNotes.Count == 0) return;
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
            if (panel.selectedNotes.Count == 0) return;
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
        if (shift || PatternPanel.tool == 
            PatternPanel.Tool.RectangleAppend)
        {
            // Append rectangle to selection.
            foreach (Note n in notesInRectangle)
            {
                panel.selectedNotes.Add(n);
            }
        }
        else if (alt || PatternPanel.tool == 
            PatternPanel.Tool.RectangleSubtract)
        {
            // Subtract rectangle from selection.
            foreach (Note n in notesInRectangle)
            {
                panel.selectedNotes.Remove(n);
            }
        }
        else
        {
            // Replace selection with rectangle.
            panel.selectedNotes = notesInRectangle;
        }
        panel.NotifySelectionChanged();
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

    #region Zoom
    public void HorizontalZoomIn()
    {
        AdjustZoom(zoom + 10);
    }

    public void HorizontalZoomOut()
    {
        AdjustZoom(zoom - 10);
    }

    private void AdjustZoom(int value)
    {
        zoom = Mathf.Clamp(value, kMinZoom, kMaxZoom);
        float horizontal = workspaceScrollRect
            .horizontalNormalizedPosition;
        ResizeWorkspace();
        RepositionNeeded?.Invoke();
        AdjustAllPathsAndTrails();
        workspaceScrollRect.horizontalNormalizedPosition =
            horizontal;
    }

    public void VerticalZoomIn()
    {
        SetVisibleLaneNumber(Options.instance.editorOptions
            .visibleLanes - 2);
    }

    public void VerticalZoomOut()
    {
        SetVisibleLaneNumber(Options.instance.editorOptions
            .visibleLanes + 2);
    }

    private void SetVisibleLaneNumber(int newValue)
    {
        Options.instance.editorOptions.visibleLanes =
            Mathf.Clamp(newValue, 8, 16);

        ResizeWorkspace();
        RepositionNotes();
        AdjustAllPathsAndTrails();
    }
    #endregion

    #region Utilities
    public void ScrollScanlineIntoView()
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

    public void ScrollNoteIntoView(Note n)
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

    private void SynchronizeScrollRects()
    {
        headerContent.sizeDelta = new Vector2(
            workspaceContent.sizeDelta.x,
            headerContent.sizeDelta.y);
        headerScrollRect.horizontalNormalizedPosition =
            workspaceScrollRect.horizontalNormalizedPosition;
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
    #endregion
}
