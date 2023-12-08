using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Handles all UI inside the workspace.
// Does not hold the notes or selection; these remain in
// PatternPanel.
public class PatternPanelWorkspace : MonoBehaviour
{
    private PatternPanel panel;

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
    public GameObject basicNotePrefab;
    public GameObject chainHeadPrefab;
    public GameObject chainNodePrefab;
    public GameObject repeatHeadPrefab;
    public GameObject repeatHeadHoldPrefab;
    public GameObject repeatNotePrefab;
    public GameObject repeatHoldPrefab;
    public GameObject holdNotePrefab;
    public GameObject dragNotePrefab;

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
    private Note lastClickedNote;

    private float unsnappedCursorPulse;
    private float unsnappedCursorLane;

    public GameObject GetGameObjectFromNote(Note n)
    {
        if (!noteToNoteObject.ContainsKey(n)) return null;
        return noteToNoteObject[n].gameObject;
    }
    #endregion

    #region Vertical spacing
    public static int TotalLanes => 64;
    public static int VisibleLanes =>
        Options.instance.editorOptions.visibleLanes;
    private static float WorkspaceViewportHeight;
    private static float WorkspaceContentHeight =>
        LaneHeight * TotalLanes;
    private static float LaneHeight =>
        WorkspaceViewportHeight / VisibleLanes;
    #endregion

    #region Horizontal spacing
    private static int zoom = 0;
    private const int kMinZoom = 10;
    private const int kMaxZoom = 500;
    private static float ScanWidth => 10f * zoom;
    private static float PulseWidth
    {
        get
        {
            return ScanWidth /
                EditorContext.Pattern.patternMetadata.bps /
                Pattern.pulsesPerBeat;
        }
    }
    private int numScans;  // display only
    private float WorkspaceContentWidth => numScans * ScanWidth;
    #endregion

    #region Outward events
    public static event UnityAction RepositionNeeded;
    #endregion

    #region MonoBehavior APIs
    // Start is called before the first frame update
    void Start()
    {
        panel = GetComponent<PatternPanel>();
    }

    private void OnEnable()
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
            // TODO: make this an event?
            panel.RefreshPlaybackBar();
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
        // TODO: make this an event?
        panel.RefreshPlaybackBar();
    }
    #endregion

    #region Refreshing
    public void ResizeWorkspace()
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

    private void UpdateNumScansAndRelatedUI()
    {
        if (UpdateNumScans())
        {
            DestroyAndRespawnAllMarkers();
            ResizeWorkspace();
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

    #region Zoom
    // TODO: remap UI event
    public void HorizontalZoomIn()
    {
        AdjustZoom(zoom + 10);
    }

    // TODO: remap UI event
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
