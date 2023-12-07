using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
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
    public NoteObject noteCursor;

    #region Internal data structures
    private float unsnappedCursorPulse;
    private float unsnappedCursorLane;
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
    private int numScans;
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
            GameObject o = panel.GetGameObjectFromNote(n);
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
                        panel.GetGameObjectFromNote(prevChain)
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
