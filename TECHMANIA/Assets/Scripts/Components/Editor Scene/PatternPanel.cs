using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class PatternPanel : MonoBehaviour
{
    [Header("Workspace")]
    public ScrollRect workspace;
    public RectTransform workspaceContent;
    public EditorElement scanline;

    [Header("Lanes")]
    public RectTransform hiddenLaneBackground;
    public RectTransform header;
    public RectTransform laneDividerParent;

    [Header("Markers")]
    public Transform markerContainer;
    public GameObject scanMarkerTemplate;
    public GameObject beatMarkerTemplate;
    public GameObject bpmMarkerTemplate;

    [Header("Notes")]
    public Transform noteContainer;
    public EditorElement noteCursor;
    public GameObject basicNotePrefab;
    public GameObject hiddenNotePrefab;

    [Header("UI And Options")]
    public TextMeshProUGUI beatSnapDividerDisplay;
    public KeysoundSideSheet keysoundSheet;

    #region Internal Data Structures
    // All note objects sorted by pulse. This allows fast lookups
    // of whether any location is occupied when moving notes.
    //
    // This data structure must be updated alongside
    // EditorContext.Pattern at all times.
    private SortedNoteObjects sortedNoteObjects;

    private GameObject lastSelectedNoteObjectWithoutShift;
    private HashSet<GameObject> selectedNoteObjects;
    #endregion

    #region Vertical Spacing
    public static int PlayableLanes
    {
        get
        {
            return 4;
        }
    }
    public static int HiddenLanes { get; private set; }
    public static int TotalLanes
    {
        get
        {
            return PlayableLanes + HiddenLanes;
        }
    }
    public static float AllLaneTotalHeight { get; private set; }
    public static float LaneHeight
    {
        get
        {
            return AllLaneTotalHeight / TotalLanes;
        }
    }
    #endregion

    #region Horizontal Spacing
    private int numScans;
    private static int zoom;
    private int beatSnapDivisor;
    public static float ScanWidth
    {
        get
        {
            return 10f * zoom;
        }
    }
    private float WorkspaceContentWidth
    {
        get
        {
            return numScans * ScanWidth;
        }
    }
    #endregion

    #region Outward Events
    public static event UnityAction RepositionNeeded;
    public static event UnityAction<HashSet<GameObject>> SelectionChanged;
    #endregion

    #region MonoBehavior APIs
    private void OnEnable()
    {
        // TODO: save this to game options and load from disk

        // Vertical spacing
        HiddenLanes = 8;
        Canvas.ForceUpdateCanvases();
        AllLaneTotalHeight = laneDividerParent.rect.height;

        // Horizontal spacing
        zoom = 100;
        beatSnapDivisor = 2;

        // Scanline
        scanline.floatPulse = 0f;
        scanline.Reposition();

        // UI
        UpdateBeatSnapDivisorDisplay();

        Refresh();
        EditorContext.UndoneOrRedone += Refresh;
        EditorElement.LeftClicked += OnNoteObjectLeftClick;
        EditorElement.RightClicked += OnNoteObjectRightClick;
        // EditorElement.BeginDrag += OnNoteObjectBeginDrag;
        // EditorElement.Drag += OnNoteObjectDrag;
        // EditorElement.EndDrag += OnNoteObjectEndDrag;
    }

    private void OnDisable()
    {
        EditorContext.UndoneOrRedone -= Refresh;
        EditorElement.LeftClicked -= OnNoteObjectLeftClick;
        EditorElement.RightClicked -= OnNoteObjectRightClick;
        // EditorElement.BeginDrag -= OnNoteObjectBeginDrag;
        // EditorElement.Drag -= OnNoteObjectDrag;
        // EditorElement.EndDrag -= OnNoteObjectEndDrag;
    }

    // Update is called once per frame
    void Update()
    {
        bool mouseInWorkspace = RectTransformUtility.RectangleContainsScreenPoint(
            workspace.GetComponent<RectTransform>(),
            Input.mousePosition);
        bool mouseInHeader = RectTransformUtility.RectangleContainsScreenPoint(
            header, Input.mousePosition);
        if (Input.mouseScrollDelta.y != 0)
        {
            HandleMouseScroll(Input.mouseScrollDelta.y,
                mouseInWorkspace);
        }

        if (mouseInWorkspace && !mouseInHeader)
        {
            noteCursor.gameObject.SetActive(true);
            SnapNoteCursor();
        }
        else
        {
            noteCursor.gameObject.SetActive(false);
        }

        if (Input.GetMouseButton(0) && mouseInWorkspace && mouseInHeader)
        {
            MoveScanlineToMouse();
        }
    }
    #endregion

    #region Mouse and Keyboard Update
    private void HandleMouseScroll(float y, bool mouseInWorkspace)
    {
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
                Input.GetKey(KeyCode.RightControl);
        bool alt = Input.GetKey(KeyCode.LeftAlt) ||
            Input.GetKey(KeyCode.RightAlt);

        // Is the cursor inside the workspace?
        if (mouseInWorkspace && !alt)
        {
            if (ctrl)
            {
                // Adjust zoom
                zoom += Mathf.FloorToInt(y * 5f);
                zoom = Mathf.Clamp(zoom, 10, 500);
                float horizontal = workspace.horizontalNormalizedPosition;
                ResizeWorkspace();
                RepositionNeeded?.Invoke();
                workspace.horizontalNormalizedPosition = horizontal;
            }
            else
            {
                // Scroll workspace
                workspace.horizontalNormalizedPosition += y * 5f / zoom;
                workspace.horizontalNormalizedPosition =
                    Mathf.Clamp01(workspace.horizontalNormalizedPosition);
            }
        }

        // Alt+scroll to change beat snap divisor
        if (alt)
        {
            OnBeatSnapDivisorChanged(y < 0f ? -1 : 1);
        }
    }

    private void SnapNoteCursor()
    {
        Vector2 pointInContainer;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            noteContainer.GetComponent<RectTransform>(),
            Input.mousePosition,
            cam: null,
            out pointInContainer);

        int bps = EditorContext.Pattern.patternMetadata.bps;
        float cursorScan = pointInContainer.x / ScanWidth;
        float cursorPulse = cursorScan * bps * Pattern.pulsesPerBeat;
        int pulsesPerDivision = Pattern.pulsesPerBeat / beatSnapDivisor;
        int snappedCursorPulse = Mathf.RoundToInt(cursorPulse / pulsesPerDivision)
            * pulsesPerDivision;

        int snappedLane = Mathf.FloorToInt(-pointInContainer.y / LaneHeight);

        noteCursor.note = new Note();
        noteCursor.note.pulse = snappedCursorPulse;
        noteCursor.note.lane = snappedLane;
        noteCursor.Reposition();
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
        int pulsesPerDivision = Pattern.pulsesPerBeat / beatSnapDivisor;
        int snappedCursorPulse = Mathf.RoundToInt(cursorPulse / pulsesPerDivision)
            * pulsesPerDivision;

        scanline.floatPulse = snappedCursorPulse;
        scanline.Reposition();
    }
    #endregion

    #region Events From Workspace
    public void OnNoteContainerClick(BaseEventData eventData)
    {
        if (!(eventData is PointerEventData)) return;
        if ((eventData as PointerEventData).button !=
            PointerEventData.InputButton.Left)
        {
            return;
        }
        if (!noteCursor.gameObject.activeSelf) return;
        if (sortedNoteObjects.HasAt(
            noteCursor.note.pulse, noteCursor.note.lane))
        {
            return;
        }

        // Add note to pattern
        string sound = keysoundSheet.UpcomingKeysound();
        keysoundSheet.AdvanceUpcoming();
        Note n = new Note();
        n.pulse = noteCursor.note.pulse;
        n.lane = noteCursor.note.lane;
        n.type = NoteType.Basic;
        EditorContext.PrepareForChange();
        EditorContext.Pattern.AddNote(n, sound);
        EditorContext.DoneWithChange();

        // Add note to UI
        SpawnNoteObject(n, sound);
    }

    public void OnNoteObjectLeftClick(GameObject o)
    {
        bool shift = Input.GetKey(KeyCode.LeftShift) ||
            Input.GetKey(KeyCode.RightShift);
        bool ctrl = Input.GetKey(KeyCode.LeftControl) ||
            Input.GetKey(KeyCode.RightControl);
        if (shift)
        {
            if (lastSelectedNoteObjectWithoutShift == null)
            {
                lastSelectedNoteObjectWithoutShift = sortedNoteObjects.GetFirst();
            }
            List<GameObject> range = sortedNoteObjects.GetRange(
                    lastSelectedNoteObjectWithoutShift, o);
            if (ctrl)
            {
                // Add [prev, o] to current selection.
                foreach (GameObject oInRange in range)
                {
                    selectedNoteObjects.Add(oInRange);
                }
            }
            else  // !ctrl
            {
                // Overwrite current selection with [prev, o].
                selectedNoteObjects.Clear();
                foreach (GameObject oInRange in range)
                {
                    selectedNoteObjects.Add(oInRange);
                }
            }
        }
        else  // !shift
        {
            lastSelectedNoteObjectWithoutShift = o;
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
        // Delete note from pattern
        EditorElement e = o.GetComponent<EditorElement>();
        EditorContext.PrepareForChange();
        EditorContext.Pattern.DeleteNote(e.note, e.sound);
        EditorContext.DoneWithChange();

        // Delete note from UI
        sortedNoteObjects.Delete(o);
        if (lastSelectedNoteObjectWithoutShift == o)
        {
            lastSelectedNoteObjectWithoutShift = null;
        }
        selectedNoteObjects.Remove(o);
        Destroy(o);
    }
    #endregion

    #region Events From UI
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
    #endregion

    #region Refreshing
    private void Refresh()
    {
        DestroyAndRespawnExistingNotes();
        UpdateNumScans();
        DestroyAndRespawnAllMarkers();
        ResizeWorkspace();
    }

    private void UpdateNumScans()
    {
        GameObject o = sortedNoteObjects.GetLast();
        if (o == null)
        {
            numScans = 1;
            return;
        }

        int lastPulse = o.GetComponent<EditorElement>().note.pulse;
        int lastScan = lastPulse / Pattern.pulsesPerBeat
            / EditorContext.Pattern.patternMetadata.bps;
        numScans = lastScan + 2;  // 1 empty scan at the end
    }

    private void ResizeWorkspace()
    {
        workspaceContent.sizeDelta = new Vector2(
            WorkspaceContentWidth,
            workspaceContent.sizeDelta.y);
    }
    #endregion

    #region Spawning
    private void DestroyAndRespawnAllMarkers()
    {
        for (int i = 0; i < markerContainer.childCount; i++)
        {
            GameObject child = markerContainer.GetChild(i).gameObject;
            if (child == scanMarkerTemplate) continue;
            if (child == beatMarkerTemplate) continue;
            if (child == bpmMarkerTemplate) continue;
            Destroy(child.gameObject);
        }

        EditorContext.Pattern.PrepareForTimeCalculation();
        int bps = EditorContext.Pattern.patternMetadata.bps;
        for (int scan = 0; scan < numScans; scan++)
        {
            GameObject marker = Instantiate(scanMarkerTemplate, markerContainer);
            marker.SetActive(true);  // This calls OnEnabled
            EditorElement element = marker.GetComponent<EditorElement>();
            element.beat = scan * bps;
            element.SetTimeDisplay();
            element.Reposition();

            for (int beat = 1; beat < bps; beat++)
            {
                marker = Instantiate(beatMarkerTemplate, markerContainer);
                marker.SetActive(true);
                element = marker.GetComponent<EditorElement>();
                element.beat = scan * bps + beat;
                element.SetTimeDisplay();
                element.Reposition();
            }
        }

        foreach (BpmEvent e in EditorContext.Pattern.bpmEvents)
        {
            GameObject marker = Instantiate(bpmMarkerTemplate, markerContainer);
            marker.SetActive(true);
            EditorElement element = marker.GetComponent<EditorElement>();
            element.pulse = e.pulse;
            element.SetBpmText(e.bpm);
            element.Reposition();
        }
    }

    private void SpawnNoteObject(Note n, string sound)
    {
        EditorElement noteObject = Instantiate(basicNotePrefab,
            noteContainer).GetComponent<EditorElement>();
        noteObject.note = n;
        noteObject.sound = sound;
        noteObject.SetKeysoundText();
        noteObject.Reposition();

        sortedNoteObjects.Add(noteObject.gameObject);
    }

    private void DestroyAndRespawnExistingNotes()
    {
        for (int i = 0; i < noteContainer.childCount; i++)
        {
            Destroy(noteContainer.GetChild(i).gameObject);
        }
        sortedNoteObjects = new SortedNoteObjects();
        lastSelectedNoteObjectWithoutShift = null;
        selectedNoteObjects = new HashSet<GameObject>();

        // For newly created patterns, there's no sound channel yet.
        EditorContext.Pattern.CreateListsIfNull();
        foreach (SoundChannel channel in EditorContext.Pattern.soundChannels)
        {
            foreach (Note n in channel.notes)
            {
                SpawnNoteObject(n, channel.name);
            }
        }
    }
    #endregion
}
